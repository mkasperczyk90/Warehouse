// Generates docs/diagrams/design-stockitem.excalidraw — a DESIGN-LEVEL event storming board:
// a zoom into ONE aggregate (StockItem, the core). It shows the aggregate boundary, the
// commands that come in, the events that go out, the invariants enforced *inside*, the
// append-only ledger the state projects from, and the domain services that enforce the rules
// which span MORE than one StockItem (capacity) or another aggregate (Batch) — and therefore
// live outside it. Design Level is the last stop before code.
// Run: node docs/diagrams/generate-design-stockitem.mjs
import { writeFileSync } from "node:fs";

let seedCounter = 1;
const rnd = () => Math.floor(Math.random() * 2 ** 31);
const elements = [];

const PALETTE = {
  event:     { bg: "#ffa94d", stroke: "#d9480f" },
  command:   { bg: "#74c0fc", stroke: "#1971c2" },
  aggregate: { bg: "#ffe066", stroke: "#f08c00" },
  policy:    { bg: "#d0bfff", stroke: "#6741d9" },
  readmodel: { bg: "#b2f2bb", stroke: "#2f9e44" },
};

function base(extra) {
  return {
    angle: 0, strokeWidth: 1.5, strokeStyle: "solid", roughness: 1, opacity: 100,
    groupIds: [], frameId: null, roundness: null, seed: rnd(),
    version: seedCounter++, versionNonce: rnd(), isDeleted: false,
    boundElements: [], updated: 1, link: null, locked: false, ...extra,
  };
}

function sticky(kind, x, y, text, opts = {}) {
  const w = opts.w ?? 240;
  const h = opts.h ?? 84;
  const fontSize = opts.fontSize ?? 15;
  const { bg, stroke } = PALETTE[kind];
  const rectId = `r${seedCounter}`;
  const textId = `t${seedCounter}`;
  elements.push(base({
    id: rectId, type: "rectangle", x, y, width: w, height: h,
    strokeColor: stroke, backgroundColor: bg, fillStyle: "solid",
    roundness: { type: 3 }, boundElements: [{ type: "text", id: textId }],
  }));
  elements.push(base({
    id: textId, type: "text", x: x + 6, y: y + h / 2 - fontSize, width: w - 12, height: fontSize * 2.4,
    strokeColor: "#1e1e1e", backgroundColor: "transparent", fillStyle: "solid",
    fontSize, fontFamily: 1, text, textAlign: "center", verticalAlign: "middle",
    containerId: rectId, originalText: text, lineHeight: 1.25,
  }));
  return { rectId, cx: x + w / 2, cy: y + h / 2, x, y, w, h };
}

function label(x, y, text, fontSize = 28, color = "#343a40", align = "left") {
  elements.push(base({
    id: `l${seedCounter}`, type: "text", x, y, width: text.length * fontSize * 0.6, height: fontSize * 1.3,
    strokeColor: color, backgroundColor: "transparent", fillStyle: "solid",
    fontSize, fontFamily: 1, text, textAlign: align, verticalAlign: "top",
    containerId: null, originalText: text, lineHeight: 1.25,
  }));
}

// Left/top-aligned multi-line panel (state / invariants / explanatory notes).
function note(x, y, w, h, text, opts = {}) {
  const fontSize = opts.fontSize ?? 14;
  elements.push(base({
    id: `r${seedCounter}`, type: "rectangle", x, y, width: w, height: h,
    strokeColor: opts.stroke ?? "#adb5bd", backgroundColor: opts.bg ?? "#f8f9fa",
    fillStyle: "solid", roundness: { type: 3 }, strokeWidth: 1.5,
  }));
  elements.push(base({
    id: `t${seedCounter}`, type: "text", x: x + 14, y: y + 12, width: w - 28, height: h - 24,
    strokeColor: opts.color ?? "#1e1e1e", backgroundColor: "transparent", fillStyle: "solid",
    fontSize, fontFamily: 1, text, textAlign: "left", verticalAlign: "top",
    containerId: null, originalText: text, lineHeight: 1.35,
  }));
  return { x, y, w, h, cx: x + w / 2, cy: y + h / 2 };
}

function arrow(x1, y1, x2, y2, opts = {}) {
  elements.push(base({
    id: `a${seedCounter}`, type: "arrow", x: x1, y: y1,
    width: Math.abs(x2 - x1), height: Math.abs(y2 - y1), angle: 0,
    strokeColor: opts.color ?? "#868e96", backgroundColor: "transparent", fillStyle: "solid",
    strokeWidth: opts.width ?? 2, strokeStyle: opts.style ?? "solid", roughness: 1,
    points: [[0, 0], [x2 - x1, y2 - y1]], lastCommittedPoint: null,
    startBinding: null, endBinding: null, startArrowhead: null, endArrowhead: opts.head ?? "arrow",
  }));
}
const CMD  = { color: "#1971c2", width: 2 };
const EVT  = { color: "#d9480f", width: 2 };
const POL  = { color: "#6741d9", width: 1.8, style: "dashed" };
const LEDG = { color: "#f08c00", width: 2 };
const GRN  = { color: "#2f9e44", width: 2 };

// ----- TITLE + LEGEND ----------------------------------------------------
label(60, -210, "Design-Level Event Storming — the StockItem aggregate", 40, "#212529");
label(60, -158, "the consistency boundary: one SKU + Batch + Location · commands in → invariants → events out · state is a projection of the ledger", 19, "#868e96");

const legendItems = [
  ["command", "Command (in)"], ["aggregate", "Aggregate / Batch"], ["event", "Event (out)"],
  ["policy", "Domain service / policy"], ["readmodel", "Read model"],
];
legendItems.forEach(([k, t], i) => {
  const lx = 60 + i * 320;
  elements.push(base({
    id: `lg${seedCounter}`, type: "rectangle", x: lx, y: -108, width: 30, height: 24,
    strokeColor: PALETTE[k].stroke, backgroundColor: PALETTE[k].bg, fillStyle: "solid", roundness: { type: 3 },
  }));
  label(lx + 38, -108, t, 16, "#495057");
});

// ----- AGGREGATE BOUNDARY ------------------------------------------------
const BX = 840, BY = 300, BW = 600, BH = 700;
elements.push(base({
  id: `bnd${seedCounter}`, type: "rectangle", x: BX, y: BY, width: BW, height: BH,
  strokeColor: "#f08c00", backgroundColor: "#fff9db", fillStyle: "solid",
  strokeStyle: "dashed", strokeWidth: 3, roundness: { type: 3 }, opacity: 70,
}));
label(BX + 20, BY + 12, "« aggregate boundary — one short transaction per scan »", 16, "#b7791f");
sticky("aggregate", BX + 40, BY + 50, "StockItem", { w: BW - 80, h: 64, fontSize: 24 });
note(BX + 40, BY + 130, BW - 80, 150,
  "STATE\n• Sku · Batch · LocationCode   (identity)\n• OnHand    : Quantity\n• Allocated : Quantity\n• Status    : Available | Quarantined\n• allocations[] : (OrderRef, qty)",
  { fontSize: 14, bg: "#fffdf5", stroke: "#f0c000" });
note(BX + 40, BY + 296, BW - 80, 372,
  "INVARIANTS — checked INSIDE the aggregate\n\n• OnHand ≥ 0 — never edited, only moved\n• Allocate ≤ OnHand − already allocated\n• Pick ≤ allocated  (units must match)\n• Quantity is unit-safe  (kg ≠ pcs)\n• a quarantined batch ⇒ not allocatable\n\n• every mutating method RETURNS a\n   StockMovement — you cannot change\n   stock without writing the ledger",
  { fontSize: 14, bg: "#fff0f6", stroke: "#c2255c", color: "#1e1e1e" });

// ----- COMMANDS (in, left) ----------------------------------------------
const cmds = [
  ["Receive\n← GoodsReceiptConfirmed", 330],
  ["Put-away / Transfer\n← scan target location", 440],
  ["Allocate (hard)\n← Release wave", 550],
  ["Pick\n← scan", 660],
  ["Quarantine\n← BatchBlocked policy", 770],
  ["Adjust\n← Stocktake / manual", 880],
];
const CX = 340, CW = 270;
const cmdHandles = cmds.map(([t, y]) => {
  const s = sticky("command", CX, y, t, { w: CW, h: 84, fontSize: 13 });
  arrow(s.x + s.w, s.cy, BX, s.cy, CMD); // command → aggregate boundary
  return s;
});

// ----- EVENTS (out, right) ----------------------------------------------
const evs = [
  ["StockReceived", 330],
  ["StockMoved (ledger entry)", 440],
  ["StockAllocated", 550],
  ["StockPicked", 660],
  ["StockItemQuarantined", 770],
  ["StockAdjusted", 880],
];
const EX = 1660, EW = 280;
evs.forEach(([t, y]) => {
  const s = sticky("event", EX, y, t, { w: EW, h: 84, fontSize: 14 });
  arrow(BX + BW, s.cy, s.x, s.cy, EVT); // aggregate boundary → event
});

// ----- DOMAIN SERVICES (rules that DON'T fit in the aggregate) ----------
label(40, 250, "Rules that span more than one StockItem, or another aggregate →", 14, "#6741d9");
const sPutAway = sticky("policy", 40, 415, "PutAwayPolicy\ntemperature ∧ capacity ∧ load\n(capacity spans many items)", { w: 270, h: 110, fontSize: 12 });
const sAlloc   = sticky("policy", 40, 555, "AllocationPolicy\nFEFO + batch quality\n(spans StockItem + Batch)", { w: 270, h: 110, fontSize: 12 });
arrow(sPutAway.x + sPutAway.w, sPutAway.cy, cmdHandles[1].x, cmdHandles[1].cy, POL); // → Put-away
arrow(sAlloc.x + sAlloc.w, sAlloc.cy, cmdHandles[2].x, cmdHandles[2].cy, POL);       // → Allocate
note(40, 700, 270, 150,
  "Related services\n• StockTransferService\n   1 physical move = 1 ledger\n   entry across two StockItems\n• ReservationService\n   soft-reserve within ATP\n   (on StockReservation)",
  { fontSize: 12, bg: "#f3f0ff", stroke: "#6741d9" });

// ----- BATCH (the other aggregate the rules reach into) -----------------
const aBatch = sticky("aggregate", 1040, 150, "Batch  (QC hold · expiry)", { w: 240, h: 70, fontSize: 14 });
arrow(aBatch.cx, aBatch.y + aBatch.h, sAlloc.x + sAlloc.w / 2, sAlloc.y, { color: "#f08c00", width: 1.6, style: "dashed" }); // FEFO reads expiry/quality
arrow(aBatch.x, aBatch.cy, cmdHandles[4].cx, cmdHandles[4].y, { color: "#f08c00", width: 1.6, style: "dashed" });            // BatchBlocked → Quarantine

// ----- LEDGER + PROJECTION (the heart of the model) ---------------------
const aLedger = sticky("aggregate", 980, 1080, "StockMovement\n(append-only ledger)", { w: 320, h: 92, fontSize: 15 });
arrow(BX + BW / 2, BY + BH, aLedger.cx, aLedger.y, LEDG);
label(BX + BW / 2 + 12, BY + BH + 18, "every behavior appends one entry", 13, "#b7791f");
const rView = sticky("readmodel", 1420, 1080, "Stock levels / ATP view\n(OnHand − Allocated − Reserved)", { w: 300, h: 92, fontSize: 13 });
arrow(aLedger.x + aLedger.w, aLedger.cy, rView.x, rView.cy, GRN);
note(40, 1080, 880 - 40, 92,
  "OnHand is a PROJECTION of StockMovement, not a stored number. A correction is a reversing movement — the table rejects UPDATE / DELETE. This is event-sourcing-flavored thinking without full event sourcing (decision #2).",
  { fontSize: 13, bg: "#fff9db", stroke: "#f0c000" });

// ----- WRITE -------------------------------------------------------------
const doc = {
  type: "excalidraw", version: 2, source: "https://github.com/warehouse-wms/docs",
  elements, appState: { gridSize: null, viewBackgroundColor: "#fffdf5" }, files: {},
};
writeFileSync(new URL("./design-stockitem.excalidraw", import.meta.url), JSON.stringify(doc, null, 2));
console.log(`Wrote design-stockitem.excalidraw with ${elements.length} elements.`);
