// Shared drawing kit for the static (architecture / planning) Excalidraw boards.
// Self-contained generators (the event-storming boards) don't use this; the newer
// box-and-arrow diagrams (story map, C4, deployment, clean architecture) do.
import { writeFileSync } from "node:fs";

let seed = 1;
const rnd = () => Math.floor(Math.random() * 2 ** 31);
const els = [];
export function reset() { els.length = 0; seed = 1; }

// [background, stroke] per semantic kind.
export const C = {
  event: ["#ffa94d", "#d9480f"], command: ["#74c0fc", "#1971c2"], aggregate: ["#ffe066", "#f08c00"],
  policy: ["#d0bfff", "#6741d9"], read: ["#b2f2bb", "#2f9e44"], actor: ["#ffec99", "#f08c00"],
  hot: ["#fcc2d7", "#c2255c"], ext: ["#eebefa", "#9c36b5"],
  svc: ["#b2f2bb", "#2f9e44"], ui: ["#ffe066", "#f08c00"], inf: ["#d0bfff", "#6741d9"],
  db: ["#74c0fc", "#1971c2"], note: ["#f1f3f5", "#868e96"], domain: ["#ffe066", "#f08c00"],
};

function base(extra) {
  return {
    angle: 0, strokeWidth: 1.5, strokeStyle: "solid", roughness: 1, opacity: 100,
    groupIds: [], frameId: null, roundness: null, seed: rnd(),
    version: seed++, versionNonce: rnd(), isDeleted: false,
    boundElements: [], updated: 1, link: null, locked: false, ...extra,
  };
}

// A labelled node. shape: rect | ellipse | diamond.
export function box(x, y, w, h, text, opts = {}) {
  const [bg, stroke] = C[opts.kind ?? "note"];
  const fontSize = opts.fontSize ?? 15;
  const shape = opts.shape ?? "rect";
  const type = shape === "ellipse" ? "ellipse" : shape === "diamond" ? "diamond" : "rectangle";
  const rectId = `r${seed}`, textId = `t${seed}`;
  els.push(base({
    id: rectId, type, x, y, width: w, height: h,
    strokeColor: opts.stroke ?? stroke, backgroundColor: opts.bg ?? bg, fillStyle: "solid",
    roundness: shape === "rect" ? { type: 3 } : null,
    strokeWidth: opts.strokeWidth ?? 1.5, strokeStyle: opts.strokeStyle ?? "solid",
    boundElements: [{ type: "text", id: textId }],
  }));
  els.push(base({
    id: textId, type: "text", x: x + 6, y: y + h / 2 - fontSize, width: w - 12, height: fontSize * 2.4,
    strokeColor: "#1e1e1e", backgroundColor: "transparent", fillStyle: "solid",
    fontSize, fontFamily: 1, text, textAlign: "center", verticalAlign: "middle",
    containerId: rectId, originalText: text, lineHeight: 1.2,
  }));
  return { x, y, w, h, cx: x + w / 2, cy: y + h / 2, id: rectId };
}

export function label(x, y, text, fontSize = 24, color = "#343a40", align = "left") {
  els.push(base({
    id: `l${seed}`, type: "text", x, y, width: text.length * fontSize * 0.6, height: fontSize * 1.3,
    strokeColor: color, backgroundColor: "transparent", fillStyle: "solid",
    fontSize, fontFamily: 1, text, textAlign: align, verticalAlign: "top",
    containerId: null, originalText: text, lineHeight: 1.25,
  }));
}

export function zone(x, y, w, h, title, color, opts = {}) {
  els.push(base({
    id: `z${seed}`, type: "rectangle", x, y, width: w, height: h,
    strokeColor: color, backgroundColor: opts.bg ?? "transparent", fillStyle: "solid",
    strokeStyle: opts.strokeStyle ?? "dashed", strokeWidth: 2, roundness: { type: 3 }, opacity: opts.opacity ?? 70,
  }));
  if (title) label(x + 14, y + 10, title, opts.titleSize ?? 18, color);
}

export function arrow(x1, y1, x2, y2, opts = {}) {
  els.push(base({
    id: `a${seed}`, type: "arrow", x: x1, y: y1, width: x2 - x1, height: y2 - y1,
    strokeColor: opts.color ?? "#868e96", backgroundColor: "transparent", fillStyle: "solid",
    strokeWidth: opts.width ?? 1.8, strokeStyle: opts.style ?? "solid", roughness: 1,
    points: [[0, 0], [x2 - x1, y2 - y1]], lastCommittedPoint: null,
    startBinding: null, endBinding: null,
    startArrowhead: opts.startHead ?? null,
    endArrowhead: opts.head === null ? null : opts.head ?? "arrow",
  }));
  if (opts.label) label((x1 + x2) / 2 - opts.label.length * 3.2, (y1 + y2) / 2 - 16, opts.label, 12, opts.color ?? "#868e96");
}

// Connect two boxes edge-to-edge, choosing anchors by their relative position.
export function connect(a, b, opts = {}) {
  const dx = b.cx - a.cx, dy = b.cy - a.cy;
  let x1, y1, x2, y2;
  if (Math.abs(dx) >= Math.abs(dy)) {
    if (dx >= 0) { x1 = a.x + a.w; y1 = a.cy; x2 = b.x; y2 = b.cy; }
    else { x1 = a.x; y1 = a.cy; x2 = b.x + b.w; y2 = b.cy; }
  } else {
    if (dy >= 0) { x1 = a.cx; y1 = a.y + a.h; x2 = b.cx; y2 = b.y; }
    else { x1 = a.cx; y1 = a.y; x2 = b.cx; y2 = b.y + b.h; }
  }
  arrow(x1, y1, x2, y2, opts);
}

export function legend(x, y, items) {
  items.forEach(([kind, t], i) => {
    const ly = y + i * 30;
    const [bg, stroke] = C[kind];
    els.push(base({
      id: `lg${seed}`, type: "rectangle", x, y: ly, width: 26, height: 20,
      strokeColor: stroke, backgroundColor: bg, fillStyle: "solid", roundness: { type: 3 },
    }));
    label(x + 34, ly + 1, t, 14, "#495057");
  });
}

export function write(url) {
  const doc = {
    type: "excalidraw", version: 2, source: "https://github.com/warehouse-wms/docs",
    elements: els, appState: { gridSize: null, viewBackgroundColor: "#fffdf5" }, files: {},
  };
  writeFileSync(url, JSON.stringify(doc, null, 2));
  console.log(`Wrote ${url.pathname ?? url} with ${els.length} elements.`);
}
