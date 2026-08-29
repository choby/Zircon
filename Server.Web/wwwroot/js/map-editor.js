import { Application, Container, Graphics, Sprite, Texture } from './vendor/pixi.mjs';

const CELL_WIDTH = 12;
const CELL_HEIGHT = 8;
const IMAGE_SCALE = 0.25;
const ANIMATION_FRAME_MASK = 0x0f;
const ANIMATION_BLEND_BIT = 0x80;
const MAX_CONCURRENT_TEXTURE_LOADS = 8;

export async function createMapEditor(host, mapFileName, regionIndex, initialETag, dotnet) {
    const [mapResponse, assetStatusResponse] = await Promise.all([
        fetch(`/api/maps/${encodeURIComponent(mapFileName)}`),
        fetch('/api/map-assets/status')
    ]);
    if (!mapResponse.ok) throw new Error(await mapResponse.text());
    const map = await mapResponse.json();
    const assetStatus = assetStatusResponse.ok ? await assetStatusResponse.json() : null;
    const assetVersion = encodeURIComponent(assetStatus?.version ?? 'uncached');

    const pointsResponse = await fetch(`/api/map-regions/${regionIndex}/points?width=${map.width}`);
    if (!pointsResponse.ok) throw new Error(await pointsResponse.text());
    const initialPoints = await pointsResponse.json();

    if (assetStatus && !assetStatus.available)
        await dotnet.invokeMethodAsync('ReportMapWarning', assetStatus.message);

    const app = new Application();
    await app.init({
        resizeTo: host,
        preference: 'webgl',
        background: '#090d0f',
        antialias: false,
        resolution: Math.min(window.devicePixelRatio || 1, 2),
        autoDensity: true
    });
    host.replaceChildren(app.canvas);
    app.canvas.classList.add('map-canvas');
    app.canvas.oncontextmenu = event => event.preventDefault();

    const world = new Container();
    const backgroundLayer = new Container();
    const objectLayer = new Container();
    const overlayLayer = new Graphics();
    objectLayer.sortableChildren = true;
    world.addChild(backgroundLayer, objectLayer, overlayLayer);
    app.stage.addChild(world);

    const selection = new Set(initialPoints.map(point => `${point.x},${point.y}`));
    const texturePromises = new Map();
    const missingFiles = new Set();
    const textureLoadQueue = [];
    let activeTextureLoads = 0;
    let etag = initialETag;
    let radius = 0;
    let blockedMode = false;
    let showAttributes = true;
    const visibleLayers = { background: true, middle: true, front: true, light: true };
    let zoom = 1;
    let pointerDown = false;
    let pointerButton = -1;
    let panning = false;
    let lastPointer = null;
    let renderGeneration = 0;
    let animationPhase = 0;
    let redrawQueued = false;
    let disposed = false;
    let hasVisibleAnimation = false;

    const cellAt = event => {
        const rect = app.canvas.getBoundingClientRect();
        const localX = (event.clientX - rect.left - world.x) / world.scale.x;
        const localY = (event.clientY - rect.top - world.y) / world.scale.y;
        return { x: Math.floor(localX / CELL_WIDTH), y: Math.floor(localY / CELL_HEIGHT) };
    };

    const valid = (x, y) => x >= 0 && x < map.width && y >= 0 && y < map.height;
    const isBlocked = (x, y) => map.cells[y * map.width + x]?.blocked === true;
    const selectable = (x, y) => valid(x, y) && isBlocked(x, y) === blockedMode;

    function runTextureLoadQueue() {
        while (!disposed && activeTextureLoads < MAX_CONCURRENT_TEXTURE_LOADS && textureLoadQueue.length > 0) {
            const job = textureLoadQueue.shift();
            activeTextureLoads++;
            job.load().then(job.resolve, job.reject).finally(() => {
                activeTextureLoads--;
                runTextureLoadQueue();
            });
        }
    }

    function enqueueTextureLoad(load) {
        return new Promise((resolve, reject) => {
            textureLoadQueue.push({ load, resolve, reject });
            runTextureLoadQueue();
        });
    }

    async function loadTexture(url) {
        const image = new Image();
        image.decoding = 'async';
        image.src = url;
        await image.decode();

        // HTMLImageElement lets WebGL perform premultiplication exactly once during
        // upload. createImageBitmap() may already premultiply transparent pixels,
        // which caused dark/rotated-looking map objects when Pixi uploaded them again.
        const texture = Texture.from({ resource: image, alphaMode: 'premultiply-alpha-on-upload' });
        texture.source.style.scaleMode = 'nearest';
        return texture;
    }

    async function textureFor(file, image) {
        const key = `${file}:${image}`;
        if (texturePromises.has(key)) return texturePromises.get(key);
        const promise = enqueueTextureLoad(async () => {
            try {
                return await loadTexture(`/api/map-assets/${file}/${image}?v=${assetVersion}`);
            } catch {
                if (!missingFiles.has(file)) {
                    missingFiles.add(file);
                    await dotnet.invokeMethodAsync('ReportMapWarning',
                        `地图贴图资源 ${file} 缺失或无法解码，请检查 ClientPath 和客户端 Data/Map Data 目录。`);
                }
                return null;
            }
        });
        texturePromises.set(key, promise);
        return promise;
    }

    function clearLayer(container) {
        for (const child of container.removeChildren()) child.destroy();
    }

    function visibleBounds(includeTallObjects = false) {
        const left = Math.max(0, Math.floor((-world.x / world.scale.x) / CELL_WIDTH) - 2);
        const top = Math.max(0, Math.floor((-world.y / world.scale.y) / CELL_HEIGHT) - 2);
        const right = Math.min(map.width, left + Math.ceil(app.screen.width / world.scale.x / CELL_WIDTH) + 4);
        const extraBottom = includeTallObjects ? 20 : 0;
        const bottom = Math.min(map.height, top + Math.ceil(app.screen.height / world.scale.y / CELL_HEIGHT) + 4 + extraBottom);
        return { left, top, right, bottom };
    }

    async function placeTile(container, file, image, x, y, generation, objectLayer = false, blend = false, order = 0) {
        const texture = await textureFor(file, image);
        if (disposed || generation !== renderGeneration || texture === null) return;
        const sprite = new Sprite(texture);
        sprite.roundPixels = true;
        sprite.scale.set(IMAGE_SCALE);
        sprite.x = x * CELL_WIDTH;
        if (!objectLayer) {
            sprite.y = y * CELL_HEIGHT;
        } else if ((texture.width === 48 && texture.height === 32) || (texture.width === 96 && texture.height === 64)) {
            sprite.y = y * CELL_HEIGHT;
        } else {
            sprite.y = (y + 1) * CELL_HEIGHT - texture.height * IMAGE_SCALE;
        }
        sprite.alpha = blend ? 0.5 : 1;
        sprite.zIndex = y * map.width + x + order;
        container.addChild(sprite);
    }

    function renderTiles() {
        const generation = ++renderGeneration;
        clearLayer(backgroundLayer);
        clearLayer(objectLayer);
        hasVisibleAnimation = false;

        if (visibleLayers.background) {
            const bounds = visibleBounds();
            const startX = bounds.left + (bounds.left & 1);
            const startY = bounds.top + (bounds.top & 1);
            for (let y = startY; y < bounds.bottom; y += 2) {
                for (let x = startX; x < bounds.right; x += 2) {
                    const cell = map.cells[y * map.width + x];
                    if (cell) void placeTile(backgroundLayer, cell.backFile, cell.backImage, x, y, generation);
                }
            }
        }

        const bounds = visibleBounds(true);
        for (let y = bounds.top; y < bounds.bottom; y++) {
            for (let x = bounds.left; x < bounds.right; x++) {
                const cell = map.cells[y * map.width + x];
                if (!cell) continue;

                if (visibleLayers.middle && cell.middleFile !== 0 && cell.middleImage > 0) {
                    const count = cell.middleAnimationFrame & ANIMATION_FRAME_MASK;
                    const animated = count > 1 && cell.middleAnimationFrame < 255;
                    const image = cell.middleImage - 1 + (animated ? animationPhase % count : 0);
                    hasVisibleAnimation ||= animated;
                    void placeTile(objectLayer, cell.middleFile, image, x, y, generation, true,
                        (cell.middleAnimationFrame & ANIMATION_BLEND_BIT) !== 0, 0.1);
                }
                if (visibleLayers.front && cell.frontFile !== 0 && cell.frontImage > 0) {
                    const count = cell.frontAnimationFrame & ANIMATION_FRAME_MASK;
                    const animated = count > 1 && cell.frontAnimationFrame < 255;
                    const image = cell.frontImage - 1 + (animated ? animationPhase % count : 0);
                    hasVisibleAnimation ||= animated;
                    void placeTile(objectLayer, cell.frontFile, image, x, y, generation, true,
                        (cell.frontAnimationFrame & ANIMATION_BLEND_BIT) !== 0, 0.2);
                }
            }
        }
    }

    function renderOverlay() {
        overlayLayer.clear();
        const { left, top, right, bottom } = visibleBounds();
        overlayLayer.rect(0, 0, map.width * CELL_WIDTH, map.height * CELL_HEIGHT)
            .stroke({ color: 0x273238, width: 1 });
        for (let y = top; y < bottom; y++) {
            for (let x = left; x < right; x++) {
                const key = `${x},${y}`;
                const cell = map.cells[y * map.width + x];
                if (visibleLayers.light && cell?.light > 0)
                    overlayLayer.circle(x * CELL_WIDTH + CELL_WIDTH / 2, y * CELL_HEIGHT + CELL_HEIGHT / 2,
                        Math.min(5, 1 + cell.light / 8)).fill({ color: 0xffdc80, alpha: 0.16 });
                if (showAttributes && isBlocked(x, y))
                    overlayLayer.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
                        .fill({ color: 0xb53c32, alpha: 0.28 });
                if (selection.has(key))
                    overlayLayer.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
                        .fill({ color: 0xf1b84b, alpha: 0.35 });
            }
        }
    }

    function redraw() {
        if (redrawQueued || disposed) return;
        redrawQueued = true;
        requestAnimationFrame(() => {
            redrawQueued = false;
            renderTiles();
            renderOverlay();
        });
    }

    function brush(point, add) {
        for (let y = point.y - radius; y <= point.y + radius; y++) {
            for (let x = point.x - radius; x <= point.x + radius; x++) {
                if (!selectable(x, y)) continue;
                const key = `${x},${y}`;
                if (add) selection.add(key); else selection.delete(key);
            }
        }
        renderOverlay();
    }

    function flood(point) {
        if (!selectable(point.x, point.y)) return;
        const removing = selection.has(`${point.x},${point.y}`);
        const queue = [point];
        const visited = new Set();
        while (queue.length) {
            const current = queue.shift();
            const key = `${current.x},${current.y}`;
            if (visited.has(key) || !selectable(current.x, current.y) || selection.has(key) !== removing) continue;
            visited.add(key);
            if (removing) selection.delete(key); else selection.add(key);
            queue.push({ x: current.x - 1, y: current.y }, { x: current.x + 1, y: current.y },
                { x: current.x, y: current.y - 1 }, { x: current.x, y: current.y + 1 });
        }
        renderOverlay();
    }

    app.canvas.addEventListener('pointerdown', event => {
        pointerDown = true;
        pointerButton = event.button;
        lastPointer = { x: event.clientX, y: event.clientY };
        panning = event.shiftKey;
        const point = cellAt(event);
        if (!panning && event.button === 0) brush(point, true);
        if (!panning && event.button === 2) brush(point, false);
        if (!panning && event.button === 1) flood(point);
        app.canvas.setPointerCapture(event.pointerId);
    });

    app.canvas.addEventListener('pointermove', event => {
        const point = cellAt(event);
        dotnet.invokeMethodAsync('UpdateStatus', point.x, point.y, selection.size);
        if (!pointerDown) return;
        if (panning) {
            world.x += event.clientX - lastPointer.x;
            world.y += event.clientY - lastPointer.y;
            lastPointer = { x: event.clientX, y: event.clientY };
            redraw();
        } else if (pointerButton === 0) brush(point, true);
        else if (pointerButton === 2) brush(point, false);
    });
    app.canvas.addEventListener('pointerup', () => { pointerDown = false; panning = false; });
    app.canvas.addEventListener('wheel', event => {
        event.preventDefault();
        const factor = event.deltaY < 0 ? 1.25 : 0.8;
        setZoom(zoom * factor, event.offsetX, event.offsetY);
    }, { passive: false });

    function setZoom(value, anchorX = app.screen.width / 2, anchorY = app.screen.height / 2) {
        const next = Math.max(0.25, Math.min(8, value));
        const worldX = (anchorX - world.x) / world.scale.x;
        const worldY = (anchorY - world.y) / world.scale.y;
        zoom = next;
        world.scale.set(next);
        world.x = anchorX - worldX * next;
        world.y = anchorY - worldY * next;
        redraw();
    }

    const resizeObserver = new ResizeObserver(redraw);
    resizeObserver.observe(host);
    let animationElapsed = 0;
    app.ticker.add(ticker => {
        animationElapsed += ticker.deltaMS;
        if (animationElapsed >= 180 && hasVisibleAnimation) {
            animationElapsed = 0;
            animationPhase++;
            renderTiles();
        }
    });
    redraw();
    dotnet.invokeMethodAsync('UpdateStatus', -1, -1, selection.size);

    return {
        zoomIn: () => setZoom(zoom * 2),
        zoomOut: () => setZoom(zoom / 2),
        resetView: () => { world.position.set(0, 0); setZoom(1, 0, 0); },
        toggleAttributes: () => { showAttributes = !showAttributes; renderOverlay(); },
        toggleLayer: name => {
            if (!(name in visibleLayers)) return;
            visibleLayers[name] = !visibleLayers[name];
            backgroundLayer.visible = visibleLayers.background;
            if (name === 'middle' || name === 'front') renderTiles();
            else if (name === 'light') renderOverlay();
        },
        toggleBlockedMode: () => { blockedMode = !blockedMode; },
        setRadius: value => { radius = Math.max(0, Math.min(12, Number(value) || 0)); },
        save: async () => {
            const points = [...selection].map(key => {
                const [x, y] = key.split(',').map(Number);
                return { x, y };
            });
            const response = await fetch(`/api/map-regions/${regionIndex}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ width: map.width, height: map.height, etag, points })
            });
            if (!response.ok) throw new Error(await response.text());
            etag = (await response.json()).etag;
        },
        dispose: () => {
            disposed = true;
            for (const job of textureLoadQueue.splice(0)) job.resolve(null);
            resizeObserver.disconnect();
            for (const texturePromise of texturePromises.values())
                texturePromise.then(texture => texture?.destroy(true));
            app.destroy(true);
        }
    };
}
