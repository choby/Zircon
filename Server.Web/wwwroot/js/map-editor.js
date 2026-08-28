import { Application, Container, Graphics } from './vendor/pixi.mjs';

const CELL_WIDTH = 12;
const CELL_HEIGHT = 8;

export async function createMapEditor(host, mapFileName, regionIndex, initialETag, dotnet) {
    const mapResponse = await fetch(`/api/maps/${encodeURIComponent(mapFileName)}`);
    if (!mapResponse.ok) throw new Error(await mapResponse.text());
    const map = await mapResponse.json();

    const correctedPointsResponse = await fetch(`/api/map-regions/${regionIndex}/points?width=${map.width}`);
    if (!correctedPointsResponse.ok) throw new Error(await correctedPointsResponse.text());
    const initialPoints = await correctedPointsResponse.json();

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
    const layer = new Graphics();
    world.addChild(layer);
    app.stage.addChild(world);

    const selection = new Set(initialPoints.map(point => `${point.x},${point.y}`));
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

    const cellAt = event => {
        const rect = app.canvas.getBoundingClientRect();
        const localX = (event.clientX - rect.left - world.x) / world.scale.x;
        const localY = (event.clientY - rect.top - world.y) / world.scale.y;
        return {
            x: Math.floor(localX / CELL_WIDTH),
            y: Math.floor(localY / CELL_HEIGHT)
        };
    };

    const valid = (x, y) => x >= 0 && x < map.width && y >= 0 && y < map.height;
    const isBlocked = (x, y) => map.cells[y * map.width + x]?.blocked === true;
    const selectable = (x, y) => valid(x, y) && isBlocked(x, y) === blockedMode;
    const colourFor = (file, image, phase = 0) => {
        const value = ((file * 2654435761) ^ (image * 2246822519) ^ phase) >>> 0;
        return ((value & 0x7f7f7f) | 0x303030) >>> 0;
    };

    function brush(point, add) {
        for (let y = point.y - radius; y <= point.y + radius; y++) {
            for (let x = point.x - radius; x <= point.x + radius; x++) {
                if (!selectable(x, y)) continue;
                const key = `${x},${y}`;
                if (add) selection.add(key); else selection.delete(key);
            }
        }
        redraw();
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
        redraw();
    }

    function redraw() {
        layer.clear();
        const left = Math.max(0, Math.floor((-world.x / world.scale.x) / CELL_WIDTH) - 2);
        const top = Math.max(0, Math.floor((-world.y / world.scale.y) / CELL_HEIGHT) - 2);
        const right = Math.min(map.width, left + Math.ceil(app.screen.width / world.scale.x / CELL_WIDTH) + 4);
        const bottom = Math.min(map.height, top + Math.ceil(app.screen.height / world.scale.y / CELL_HEIGHT) + 4);

        layer.rect(0, 0, map.width * CELL_WIDTH, map.height * CELL_HEIGHT).stroke({ color: 0x273238, width: 1 });
        const phase = Math.floor(performance.now() / 180);
        for (let y = top; y < bottom; y++) {
            for (let x = left; x < right; x++) {
                const key = `${x},${y}`;
                const cell = map.cells[y * map.width + x];
                const back = map.cells[(y & ~1) * map.width + (x & ~1)] ?? cell;
                if (visibleLayers.background && back.backImage > 0)
                    layer.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT)
                        .fill({ color: colourFor(back.backFile, back.backImage), alpha: 0.48 });
                if (visibleLayers.middle && cell.middleImage > 0)
                    layer.rect(x * CELL_WIDTH + 2, y * CELL_HEIGHT + 2, CELL_WIDTH - 4, CELL_HEIGHT - 4)
                        .fill({ color: colourFor(cell.middleFile, cell.middleImage, cell.middleAnimationFrame ? phase % cell.middleAnimationFrame : 0), alpha: 0.66 });
                if (visibleLayers.front && cell.frontImage > 0)
                    layer.circle(x * CELL_WIDTH + CELL_WIDTH / 2, y * CELL_HEIGHT + CELL_HEIGHT / 2, 2.2)
                        .fill({ color: colourFor(cell.frontFile, cell.frontImage, cell.frontAnimationFrame ? phase % cell.frontAnimationFrame : 0), alpha: 0.9 });
                if (visibleLayers.light && cell.light > 0)
                    layer.circle(x * CELL_WIDTH + CELL_WIDTH / 2, y * CELL_HEIGHT + CELL_HEIGHT / 2, Math.min(5, 1 + cell.light / 8))
                        .fill({ color: 0xffdc80, alpha: 0.16 });
                if (showAttributes && isBlocked(x, y))
                    layer.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT).fill({ color: 0xb53c32, alpha: 0.28 });
                if (selection.has(key))
                    layer.rect(x * CELL_WIDTH, y * CELL_HEIGHT, CELL_WIDTH, CELL_HEIGHT).fill({ color: 0xf1b84b, alpha: 0.72 });
            }
        }
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

    const resizeObserver = new ResizeObserver(() => redraw());
    resizeObserver.observe(host);
    let animationElapsed = 0;
    app.ticker.add(ticker => {
        animationElapsed += ticker.deltaMS;
        if (animationElapsed >= 180) { animationElapsed = 0; redraw(); }
    });
    redraw();
    dotnet.invokeMethodAsync('UpdateStatus', -1, -1, selection.size);

    return {
        zoomIn: () => setZoom(zoom * 2),
        zoomOut: () => setZoom(zoom / 2),
        resetView: () => { world.position.set(0, 0); setZoom(1, 0, 0); },
        toggleAttributes: () => { showAttributes = !showAttributes; redraw(); },
        toggleLayer: name => { if (name in visibleLayers) visibleLayers[name] = !visibleLayers[name]; redraw(); },
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
        dispose: () => { resizeObserver.disconnect(); app.destroy(true); }
    };
}
