import { Application, Container, Graphics, Sprite, Texture } from './vendor/pixi.mjs';

const CELL_WIDTH = 12;
const CELL_HEIGHT = 8;
const IMAGE_SCALE = 0.25;
const ANIMATION_FRAME_MASK = 0x0f;
const ANIMATION_BLEND_BIT = 0x80;
const MAX_CONCURRENT_TEXTURE_LOADS = 20;
const MAX_SHARED_TEXTURES = 4096;
const DEFAULT_MIN_ZOOM = 0.25;
const ABSOLUTE_MIN_ZOOM = 0.01;
const MAX_ZOOM = 8;

// Keep successfully decoded textures between map editor instances. Most maps use
// the same libraries, so a map switch should only load assets that are genuinely
// new. Active editors retain their textures; idle least-recently-used entries are
// discarded once the cache reaches its bound.
const sharedTextures = new Map();
let sharedTextureClock = 0;

function evictSharedTextures() {
    if (sharedTextures.size <= MAX_SHARED_TEXTURES) return;
    const candidates = [...sharedTextures.entries()]
        .filter(([, entry]) => entry.references === 0)
        .sort((left, right) => left[1].lastUsed - right[1].lastUsed);
    for (const [key, entry] of candidates) {
        if (sharedTextures.size <= MAX_SHARED_TEXTURES) break;
        sharedTextures.delete(key);
        entry.texture.destroy(true);
    }
}

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
    let backgroundLayer = new Container();
    let objectLayer = new Container();
    const overlayLayer = new Graphics();
    objectLayer.sortableChildren = true;
    world.addChild(backgroundLayer, objectLayer, overlayLayer);
    app.stage.addChild(world);

    const selection = new Set(initialPoints.map(point => `${point.x},${point.y}`));
    const texturePromises = new Map();
    const retainedTextureKeys = new Set();
    const missingFiles = new Set();
    const textureLoadQueue = [];
    const priorityTextureLoadQueue = [];
    const textureAbortController = new AbortController();
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
    let animatedSprites = [];
    let animationUpdatePending = false;
    let lastStatusUpdate = 0;
    let autoFit = true;
    let resizeFrame = 0;
    let hasRenderedTiles = false;
    let rebuildTilesQueued = false;

    const canvasPoint = event => {
        const rect = app.canvas.getBoundingClientRect();
        return {
            x: (event.clientX - rect.left) * (rect.width > 0 ? app.screen.width / rect.width : 1),
            y: (event.clientY - rect.top) * (rect.height > 0 ? app.screen.height / rect.height : 1)
        };
    };

    const cellAtPoint = point => {
        const localX = (point.x - world.x) / world.scale.x;
        const localY = (point.y - world.y) / world.scale.y;
        return { x: Math.floor(localX / CELL_WIDTH), y: Math.floor(localY / CELL_HEIGHT) };
    };

    const valid = (x, y) => x >= 0 && x < map.width && y >= 0 && y < map.height;
    const isBlocked = (x, y) => map.cells[y * map.width + x]?.blocked === true;
    const selectable = (x, y) => valid(x, y) && isBlocked(x, y) === blockedMode;

    function runTextureLoadQueue() {
        while (!disposed && activeTextureLoads < MAX_CONCURRENT_TEXTURE_LOADS &&
            (priorityTextureLoadQueue.length > 0 || textureLoadQueue.length > 0)) {
            const job = priorityTextureLoadQueue.shift() ?? textureLoadQueue.shift();
            activeTextureLoads++;
            job.load().then(job.resolve, job.reject).finally(() => {
                activeTextureLoads--;
                runTextureLoadQueue();
            });
        }
    }

    function enqueueTextureLoad(load, priority) {
        return new Promise((resolve, reject) => {
            (priority ? priorityTextureLoadQueue : textureLoadQueue).push({ load, resolve, reject });
            runTextureLoadQueue();
        });
    }

    async function loadTexture(url) {
        const response = await fetch(url, { signal: textureAbortController.signal });
        // Existing ZL libraries can intentionally contain empty image slots. The
        // server represents those as 204 so they stay transparent without being
        // reported as a missing/corrupt library.
        if (response.status === 204) return null;
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const objectUrl = URL.createObjectURL(await response.blob());
        const image = new Image();
        image.decoding = 'async';
        image.src = objectUrl;
        try {
            await image.decode();
        } finally {
            URL.revokeObjectURL(objectUrl);
        }

        // HTMLImageElement lets WebGL perform premultiplication exactly once during
        // upload. createImageBitmap() may already premultiply transparent pixels,
        // which caused dark/rotated-looking map objects when Pixi uploaded them again.
        const texture = Texture.from({ resource: image, alphaMode: 'premultiply-alpha-on-upload' });
        texture.source.style.scaleMode = 'nearest';
        return texture;
    }

    function updateStatus(x, y, force = false) {
        const now = performance.now();
        if (!force && now - lastStatusUpdate < 100) return;
        lastStatusUpdate = now;
        void dotnet.invokeMethodAsync('UpdateStatus', x, y, selection.size).catch(() => { });
    }

    function retainSharedTexture(key, entry) {
        entry.lastUsed = ++sharedTextureClock;
        if (retainedTextureKeys.has(key)) return;
        retainedTextureKeys.add(key);
        entry.references++;
    }

    async function textureFor(file, image, priority = false) {
        const key = `${assetVersion}:${file}:${image}`;
        if (texturePromises.has(key)) return texturePromises.get(key);

        const cached = sharedTextures.get(key);
        if (cached) {
            retainSharedTexture(key, cached);
            const promise = Promise.resolve(cached.texture);
            texturePromises.set(key, promise);
            return promise;
        }

        const promise = enqueueTextureLoad(async () => {
            try {
                const texture = await loadTexture(`/api/map-assets/${file}/${image}?v=${assetVersion}`);
                if (texture === null) return null;
                if (disposed) {
                    texture.destroy(true);
                    return null;
                }

                // A second editor is not normally active, but prefer an existing
                // cache entry if two loads happen to finish at the same time.
                let entry = sharedTextures.get(key);
                if (entry) {
                    texture.destroy(true);
                } else {
                    entry = { texture, references: 0, lastUsed: ++sharedTextureClock };
                    sharedTextures.set(key, entry);
                }
                retainSharedTexture(key, entry);
                return entry.texture;
            } catch {
                if (!disposed && !missingFiles.has(file)) {
                    missingFiles.add(file);
                    await dotnet.invokeMethodAsync('ReportMapWarning',
                        `地图贴图资源 ${file} 缺失或无法解码，请检查 ClientPath 和客户端 Data/Map Data 目录。`);
                }
                return null;
            }
        }, priority);
        texturePromises.set(key, promise);
        return promise;
    }

    function clearLayer(container) {
        for (const child of container.removeChildren()) child.destroy();
    }

    async function placeTile(container, file, image, x, y, generation, objectLayer = false, blend = false, order = 0,
        animation = null) {
        const texture = await textureFor(file, image, objectLayer);
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
        if (animation) animation.target.push({
            sprite,
            file,
            baseImage: animation.baseImage,
            frameCount: animation.frameCount
        });
    }

    async function renderTiles() {
        const generation = ++renderGeneration;
        const progressive = !hasRenderedTiles;
        const nextBackgroundLayer = progressive ? backgroundLayer : new Container();
        const nextObjectLayer = progressive ? objectLayer : new Container();
        if (progressive) hasRenderedTiles = true;
        nextObjectLayer.sortableChildren = true;
        const placements = [];
        const nextAnimatedSprites = [];
        if (progressive) animatedSprites = nextAnimatedSprites;

        if (visibleLayers.background) {
            const startX = 0;
            const startY = 0;
            for (let y = startY; y < map.height; y += 2) {
                for (let x = startX; x < map.width; x += 2) {
                    const cell = map.cells[y * map.width + x];
                    if (cell) placements.push(
                        placeTile(nextBackgroundLayer, cell.backFile, cell.backImage, x, y, generation));
                }
            }
        }

        for (let y = 0; y < map.height; y++) {
            for (let x = 0; x < map.width; x++) {
                const cell = map.cells[y * map.width + x];
                if (!cell) continue;

                if (visibleLayers.middle && cell.middleFile !== 0 && cell.middleImage > 0) {
                    const count = cell.middleAnimationFrame & ANIMATION_FRAME_MASK;
                    const animated = count > 1 && cell.middleAnimationFrame < 255;
                    const baseImage = cell.middleImage - 1;
                    const image = baseImage + (animated ? animationPhase % count : 0);
                    placements.push(placeTile(nextObjectLayer, cell.middleFile, image, x, y, generation, true,
                        (cell.middleAnimationFrame & ANIMATION_BLEND_BIT) !== 0, 0.1,
                        animated ? { target: nextAnimatedSprites, baseImage, frameCount: count } : null));
                }
                if (visibleLayers.front && cell.frontFile !== 0 && cell.frontImage > 0) {
                    const count = cell.frontAnimationFrame & ANIMATION_FRAME_MASK;
                    const animated = count > 1 && cell.frontAnimationFrame < 255;
                    const baseImage = cell.frontImage - 1;
                    const image = baseImage + (animated ? animationPhase % count : 0);
                    placements.push(placeTile(nextObjectLayer, cell.frontFile, image, x, y, generation, true,
                        (cell.frontAnimationFrame & ANIMATION_BLEND_BIT) !== 0, 0.2,
                        animated ? { target: nextAnimatedSprites, baseImage, frameCount: count } : null));
                }
            }
        }

        await Promise.all(placements);
        if (progressive) {
            if (!disposed && generation === renderGeneration)
                hasVisibleAnimation = nextAnimatedSprites.length > 0;
            return;
        }
        if (disposed || generation !== renderGeneration) {
            clearLayer(nextBackgroundLayer);
            clearLayer(nextObjectLayer);
            nextBackgroundLayer.destroy();
            nextObjectLayer.destroy();
            return;
        }

        // Keep the previous scene visible while a layer change is being prepared.
        // Zooming and panning only transform this complete scene and never rebuild it.
        const previousBackgroundLayer = backgroundLayer;
        const previousObjectLayer = objectLayer;
        world.removeChild(previousBackgroundLayer);
        world.removeChild(previousObjectLayer);
        world.addChildAt(nextBackgroundLayer, 0);
        world.addChildAt(nextObjectLayer, 1);
        backgroundLayer = nextBackgroundLayer;
        objectLayer = nextObjectLayer;
        animatedSprites = nextAnimatedSprites;
        hasVisibleAnimation = nextAnimatedSprites.length > 0;
        clearLayer(previousBackgroundLayer);
        clearLayer(previousObjectLayer);
        previousBackgroundLayer.destroy();
        previousObjectLayer.destroy();
    }

    function renderOverlay() {
        overlayLayer.clear();
        overlayLayer.rect(0, 0, map.width * CELL_WIDTH, map.height * CELL_HEIGHT)
            .stroke({ color: 0x273238, width: 1 });
        for (let y = 0; y < map.height; y++) {
            for (let x = 0; x < map.width; x++) {
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

    function redraw(rebuildTiles = false) {
        rebuildTilesQueued ||= rebuildTiles;
        if (redrawQueued || disposed) return;
        redrawQueued = true;
        requestAnimationFrame(() => {
            redrawQueued = false;
            const rebuild = rebuildTilesQueued;
            rebuildTilesQueued = false;
            if (rebuild) void renderTiles();
            renderOverlay();
        });
    }

    async function updateAnimatedSprites() {
        if (disposed || animationUpdatePending || animatedSprites.length === 0) return;
        animationUpdatePending = true;
        animationPhase++;
        const targets = animatedSprites;
        try {
            await Promise.all(targets.map(async item => {
                const texture = await textureFor(
                    item.file,
                    item.baseImage + animationPhase % item.frameCount,
                    true);
                if (!disposed && animatedSprites === targets && texture !== null)
                    item.sprite.texture = texture;
            }));
        } finally {
            animationUpdatePending = false;
        }
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
        const position = canvasPoint(event);
        lastPointer = position;
        panning = event.shiftKey;
        if (panning) autoFit = false;
        const point = cellAtPoint(position);
        if (!panning && event.button === 0) brush(point, true);
        if (!panning && event.button === 2) brush(point, false);
        if (!panning && event.button === 1) flood(point);
        app.canvas.setPointerCapture(event.pointerId);
    });

    app.canvas.addEventListener('pointermove', event => {
        const position = canvasPoint(event);
        const point = cellAtPoint(position);
        updateStatus(point.x, point.y);
        if (!pointerDown) return;
        if (panning) {
            world.x += position.x - lastPointer.x;
            world.y += position.y - lastPointer.y;
            lastPointer = position;
        } else if (pointerButton === 0) brush(point, true);
        else if (pointerButton === 2) brush(point, false);
    });
    app.canvas.addEventListener('pointerup', () => { pointerDown = false; panning = false; });
    app.canvas.addEventListener('wheel', event => {
        event.preventDefault();
        autoFit = false;
        const factor = event.deltaY < 0 ? 1.25 : 0.8;
        const anchor = canvasPoint(event);
        setZoom(zoom * factor, anchor.x, anchor.y);
    }, { passive: false });

    function setZoom(value, anchorX = app.screen.width / 2, anchorY = app.screen.height / 2) {
        const mapWidth = Math.max(1, map.width * CELL_WIDTH);
        const mapHeight = Math.max(1, map.height * CELL_HEIGHT);
        const fittedZoom = Math.min(app.screen.width / mapWidth, app.screen.height / mapHeight);
        const minimum = Math.max(ABSOLUTE_MIN_ZOOM, Math.min(DEFAULT_MIN_ZOOM, fittedZoom));
        const next = Math.max(minimum, Math.min(MAX_ZOOM, value));
        const worldX = (anchorX - world.x) / world.scale.x;
        const worldY = (anchorY - world.y) / world.scale.y;
        zoom = next;
        world.scale.set(next);
        world.x = anchorX - worldX * next;
        world.y = anchorY - worldY * next;
    }

    function fitMapToCanvas() {
        const mapWidth = Math.max(1, map.width * CELL_WIDTH);
        const mapHeight = Math.max(1, map.height * CELL_HEIGHT);
        if (app.screen.width <= 0 || app.screen.height <= 0) return;
        zoom = Math.max(ABSOLUTE_MIN_ZOOM,
            Math.min(MAX_ZOOM, app.screen.width / mapWidth, app.screen.height / mapHeight));
        world.scale.set(zoom);
        world.x = (app.screen.width - mapWidth * zoom) / 2;
        world.y = (app.screen.height - mapHeight * zoom) / 2;
        autoFit = true;
        if (!hasRenderedTiles) redraw(true);
    }

    const resizeObserver = new ResizeObserver(() => {
        cancelAnimationFrame(resizeFrame);
        resizeFrame = requestAnimationFrame(() => {
            resizeFrame = 0;
            const width = Math.max(1, host.clientWidth);
            const height = Math.max(1, host.clientHeight);
            if (app.screen.width === width && app.screen.height === height) return;
            app.renderer.resize(width, height);
            if (autoFit) fitMapToCanvas();
        });
    });
    resizeObserver.observe(host);
    let animationElapsed = 0;
    app.ticker.add(ticker => {
        animationElapsed += ticker.deltaMS;
        if (animationElapsed >= 180 && hasVisibleAnimation) {
            animationElapsed = 0;
            void updateAnimatedSprites();
        }
    });
    fitMapToCanvas();
    updateStatus(-1, -1, true);

    return {
        zoomIn: () => { autoFit = false; setZoom(zoom * 2); },
        zoomOut: () => { autoFit = false; setZoom(zoom / 2); },
        resetView: fitMapToCanvas,
        toggleAttributes: () => { showAttributes = !showAttributes; renderOverlay(); },
        toggleLayer: name => {
            if (!(name in visibleLayers)) return;
            visibleLayers[name] = !visibleLayers[name];
            backgroundLayer.visible = visibleLayers.background;
            if (name === 'middle' || name === 'front') redraw(true);
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
            textureAbortController.abort();
            for (const job of textureLoadQueue.splice(0)) job.resolve(null);
            for (const job of priorityTextureLoadQueue.splice(0)) job.resolve(null);
            cancelAnimationFrame(resizeFrame);
            resizeObserver.disconnect();
            for (const key of retainedTextureKeys) {
                const entry = sharedTextures.get(key);
                if (entry) {
                    entry.references = Math.max(0, entry.references - 1);
                    entry.lastUsed = ++sharedTextureClock;
                }
            }
            retainedTextureKeys.clear();
            evictSharedTextures();
            app.destroy(true);
        }
    };
}
