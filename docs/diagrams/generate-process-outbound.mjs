// Generates docs/diagrams/process-outbound.excalidraw — a PROCESS-LEVEL event storming
// board: a zoom into ONE business process (outbound order fulfillment), modelled with the
// full grammar — actor → command → aggregate → event → policy → next command — plus the
// read models people consult and the exception paths (partial reserve, allocation reject,
// short pick). Big Picture answers "what happens?"; Process Level answers "how does this one
// flow actually work, with its decisions and its unhappy paths?".
// Run: node docs/diagrams/generate-process-outbound.mjs
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
  actor:     { bg: "#ffec99", stroke: "#f08c00" },
  hotspot:   { bg: "#fcc2d7", stroke: "#c2255c" },
  external:  { bg: "#eebefa", stroke: "#9c36b5" },
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
  const w = opts.w ?? 188;
  const h = opts.h ?? 96;
  const angle = opts.angle ?? 0;
  const fontSize = opts.fontSize ?? 15;
  const { bg, stroke } = PALETTE[kind];
  const rectId = `r${seedCounter}`;
  const textId = `t${seedCounter}`;
  elements.push(base({
    id: rectId, type: "rectangle", x, y, width: w, height: h, angle,
    strokeColor: stroke, backgroundColor: bg, fillStyle: "solid",
    roundness: { type: 3 }, boundElements: [{ type: "text", id: textId }],
  }));
  elements.push(base({
    id: textId, type: "text", x: x + 6, y: y + h / 2 - fontSize, width: w - 12, height: fontSize * 2.2,
    angle, strokeColor: "#1e1e1e", backgroundColor: "transparent", fillStyle: "solid",
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

function zone(x, y, w, h, title, color) {
  elements.push(base({
    id: `z${seedCounter}`, type: "rectangle", x, y, width: w, height: h,
    strokeColor: color, backgroundColor: "transparent", fillStyle: "solid",
    strokeStyle: "dashed", strokeWidth: 2, roundness: { type: 3 }, opacity: 60,
  }));
  label(x + 16, y + 12, title, 26, color);
}

function arrow(x1, y1, x2, y2, opts = {}) {
  elements.push(base({
    id: `a${seedCounter}`, type: "arrow", x: x1, y: y1,
    width: Math.abs(x2 - x1), height: Math.abs(y2 - y1), angle: 0,
    strokeColor: opts.color ?? "#868e96", backgroundColor: "transparent", fillStyle: "solid",
    strokeWidth: opts.width ?? 2, strokeStyle: opts.style ?? "solid", roughness: 1,
    points: [[0, 0], [x2 - x1, y2 - y1]], lastCommittedPoint: null,
    startBinding: null, endBinding: null,
    startArrowhead: null, endArrowhead: opts.head ?? "arrow",
  }));
}

const aDown  = (a, b, o = {}) => arrow(a.cx, a.y + a.h, b.cx, b.y, o);
const aUp    = (a, b, o = {}) => arrow(a.cx, a.y, b.cx, b.y + b.h, o);
const aSpine = (a, b) => arrow(a.x + a.w, a.cy, b.x, b.cy, { color: "#ced4da", width: 3 });

const FLOW = { color: "#868e96", width: 1.8 };
const EMIT = { color: "#f08c00", width: 1.8 };
const POL  = { color: "#6741d9", width: 1.8, style: "dashed" };
const READ = { color: "#2f9e44", width: 1.6, style: "dashed" };
const ALT  = { color: "#c2255c", width: 1.8, style: "dashed" };

function integration(s, text) { label(s.x, s.y - 24, "⚡ " + text, 13, "#0b7285"); }

// ----- TITLE + LEGEND ----------------------------------------------------
label(60, -200, "Process-Level Event Storming — Outbound Order Fulfillment", 40, "#212529");
label(60, -148, "one process, end to end · Logistics drives the flow · Inventory owns the stock truth (StockItem / Batch / ledger)", 20, "#868e96");

const legendItems = [
  ["actor", "Actor"], ["command", "Command"], ["aggregate", "Aggregate"],
  ["event", "Event"], ["policy", "Policy"], ["readmodel", "Read model"], ["hotspot", "Decision / exception"],
];
legendItems.forEach(([k, t], i) => {
  const lx = 60 + (i % 7) * 250;
  elements.push(base({
    id: `lg${seedCounter}`, type: "rectangle", x: lx, y: -100, width: 30, height: 24,
    strokeColor: PALETTE[k].stroke, backgroundColor: PALETTE[k].bg, fillStyle: "solid", roundness: { type: 3 },
  }));
  label(lx + 38, -100, t, 16, "#495057");
});
label(60, -64, "— spine = business time · purple dashed = policy (whenever… then…) · green dashed = reads a view · pink dashed = exception / loop · ⚡ = crosses into another service", 15, "#868e96");

// ----- FRAME -------------------------------------------------------------
zone(40, 60, 2900, 900, "OUTBOUND — fulfilment process", "#1971c2");

// Rows.
const R = { top: 120, cmd: 270, agg: 415, ev: 565, pol: 730, branch: 880 };
// Step columns (centres of each step band).
const C = { order: 80, soft: 480, alloc: 900, list: 1320, pick: 1740, pack: 2170, ship: 2600 };

// === 1 · ORDER ===========================================================
const aCustomer = sticky("actor",     C.order, R.top, "Customer / ERP");
const cPlace    = sticky("command",   C.order, R.cmd, "Place outbound order");
const agOrder   = sticky("aggregate", C.order, R.agg, "OutboundOrder");
const eCreated  = sticky("event",     C.order, R.ev,  "OutboundOrderCreated");
aDown(aCustomer, cPlace, FLOW);
aDown(cPlace, agOrder, FLOW);
// aggregate sits above the event in this layout, so it emits downward
arrow(agOrder.cx, agOrder.y + agOrder.h, eCreated.cx, eCreated.y, EMIT);

// === 2 · SOFT RESERVE ====================================================
const rAtp      = sticky("readmodel", C.soft, R.top, "Available-to-Promise view (on-hand − allocated − reserved)", { fontSize: 13 });
const cReserve  = sticky("command",   C.soft, R.cmd, "Reserve stock (soft)");
const agResv    = sticky("aggregate", C.soft, R.agg, "StockReservation");
const eReserved = sticky("event",     C.soft, R.ev,  "StockReserved (soft)");
const pReserve  = sticky("policy",    (C.order + C.soft) / 2 + 30, R.pol, "Policy: reserve within ATP (SKU-level, no pallet pinned)", { fontSize: 13 });
const hPartial  = sticky("hotspot",   C.soft, R.branch, "ATP insufficient → OrderPartiallyReserved / backorder — coordinator decides", { fontSize: 12, h: 92, angle: -0.03 });
aSpine(eCreated, eReserved);
aDown(cReserve, agResv, FLOW);
arrow(agResv.cx, agResv.y + agResv.h, eReserved.cx, eReserved.y, EMIT);
arrow(eCreated.cx, eCreated.y + eCreated.h, pReserve.x, pReserve.cy, POL);   // event → policy
aUp(pReserve, cReserve, POL);                                               // policy → command
arrow(rAtp.cx, rAtp.y + rAtp.h, cReserve.cx, cReserve.y, READ);             // command reads ATP
arrow(eReserved.cx, eReserved.y + eReserved.h, hPartial.cx, hPartial.y, ALT);
integration(eReserved, "Logistics ↔ Inventory");

// === 3 · WAVE / HARD ALLOCATE (FEFO) =====================================
const aCoord    = sticky("actor",     C.alloc, R.top, "Logistics Coordinator");
const rBatchExp = sticky("readmodel", C.alloc + 210, R.top, "Stock by batch + expiry + QC status", { fontSize: 13, w: 200 });
const cWave     = sticky("command",   C.alloc, R.cmd, "Release wave to floor");
const agStock1  = sticky("aggregate", C.alloc, R.agg, "StockItem  (marks Allocated)", { fontSize: 13 });
const eAlloc    = sticky("event",     C.alloc, R.ev,  "StockAllocated (hard)");
const pFefo     = sticky("policy",    C.alloc, R.pol, "Policy: FEFO allocation + re-check batch quality (at commit!)", { fontSize: 13 });
const hReject   = sticky("hotspot",   C.alloc + 210, R.pol, "Batch quarantined / expired NOW → AllocationRejected → re-run FEFO", { fontSize: 12, h: 92, angle: 0.03, w: 200 });
aSpine(eReserved, eAlloc);
aDown(aCoord, cWave, FLOW);
aDown(cWave, agStock1, FLOW);
arrow(agStock1.cx, agStock1.y + agStock1.h, eAlloc.cx, eAlloc.y, EMIT);
aUp(pFefo, eAlloc, POL);
arrow(rBatchExp.cx, rBatchExp.y + rBatchExp.h, pFefo.x + pFefo.w, pFefo.cy, READ); // FEFO reads expiry+QC
arrow(hReject.cx, hReject.y, pFefo.x + pFefo.w, pFefo.cy - 10, ALT);               // reject loops back to FEFO
integration(eAlloc, "Inventory");

// === 4 · PICK LIST =======================================================
const pList     = sticky("policy",    C.list, R.pol, "Policy: build routed pick list (shortest path)", { fontSize: 13 });
const agPickL   = sticky("aggregate", C.list, R.agg, "PickList");
const eListed   = sticky("event",     C.list, R.ev,  "PickListReleased");
const rPickList = sticky("readmodel", C.list, R.top, "Pick list (routed) — the picker's screen", { fontSize: 13 });
aSpine(eAlloc, eListed);
arrow(eAlloc.cx, eAlloc.y + eAlloc.h, pList.x, pList.cy, POL);   // allocation → build pick list
aUp(pList, agPickL, POL);
arrow(agPickL.cx, agPickL.y + agPickL.h, eListed.cx, eListed.y, EMIT);
arrow(eListed.cx, eListed.y, rPickList.cx, rPickList.y + rPickList.h, READ);  // event → read model

// === 5 · PICK ============================================================
const aPicker   = sticky("actor",     C.pick, R.top, "Picker");
const cPick     = sticky("command",   C.pick, R.cmd, "Confirm pick (scan loc → SKU → qty)", { fontSize: 13 });
const agStock2  = sticky("aggregate", C.pick, R.agg, "StockItem  (decrements + ledger)", { fontSize: 13 });
const ePicked   = sticky("event",     C.pick, R.ev,  "StockPicked");
const pShort    = sticky("policy",    C.pick, R.pol, "Policy: short pick → replan from another location", { fontSize: 13 });
const eShort    = sticky("event",     C.pick + 210, R.pol, "ShortPickReported", { fontSize: 13, w: 175 });
const hShort    = sticky("hotspot",   C.pick, R.branch, "Short pick = STOCKTAKE problem, not a picking error — never edit the number", { fontSize: 12, h: 92, angle: -0.03 });
aSpine(eListed, ePicked);
aDown(aPicker, cPick, FLOW);
aDown(cPick, agStock2, FLOW);
arrow(rPickList.cx, rPickList.y + rPickList.h, cPick.cx, cPick.y, READ);  // picker reads pick list
arrow(agStock2.cx, agStock2.y + agStock2.h, ePicked.cx, ePicked.y, EMIT);
arrow(ePicked.cx, ePicked.y + ePicked.h, pShort.cx, pShort.y, POL);
arrow(pShort.x + pShort.w, pShort.cy, eShort.x, eShort.cy, POL);
arrow(eShort.cx, eShort.y, rPickList.x + rPickList.w, rPickList.cy, ALT);  // replan loop → pick list
arrow(ePicked.cx, ePicked.y + ePicked.h + 40, hShort.cx, hShort.y, ALT);
integration(ePicked, "Inventory ledger");

// === 6 · PACK ============================================================
const aPackOp   = sticky("actor",     C.pack, R.top, "Operator (packing)");
const cPack     = sticky("command",   C.pack, R.cmd, "Pack & label parcels");
const agShip1   = sticky("aggregate", C.pack, R.agg, "Shipment");
const ePacked   = sticky("event",     C.pack, R.ev,  "OrderPacked");
aSpine(ePicked, ePacked);
aDown(aPackOp, cPack, FLOW);
aDown(cPack, agShip1, FLOW);
arrow(agShip1.cx, agShip1.y + agShip1.h, ePacked.cx, ePacked.y, EMIT);

// === 7 · DISPATCH ========================================================
const aCarrier  = sticky("actor",     C.ship, R.top, "Carrier");
const cDispatch = sticky("command",   C.ship, R.cmd, "Confirm dispatch (handover + signature)", { fontSize: 13 });
const agShip2   = sticky("aggregate", C.ship, R.agg, "Shipment");
const eDispatch = sticky("event",     C.ship, R.ev,  "ShipmentDispatched");
const pDrop     = sticky("policy",    C.ship, R.pol, "Policy: on dispatch → stock drops (ledger) + notify customer", { fontSize: 13 });
const rTrack    = sticky("readmodel", C.ship, R.branch, "Tracking # → customer", { fontSize: 13, h: 70 });
aSpine(ePacked, eDispatch);
aDown(aCarrier, cDispatch, FLOW);
aDown(cDispatch, agShip2, FLOW);
arrow(agShip2.cx, agShip2.y + agShip2.h, eDispatch.cx, eDispatch.y, EMIT);
aDown(eDispatch, pDrop, POL);
arrow(pDrop.cx, pDrop.y + pDrop.h, rTrack.cx, rTrack.y, READ);
integration(eDispatch, "Logistics → Customer / ERP");

// ----- WRITE -------------------------------------------------------------
const doc = {
  type: "excalidraw", version: 2, source: "https://github.com/warehouse-wms/docs",
  elements, appState: { gridSize: null, viewBackgroundColor: "#fffdf5" }, files: {},
};
writeFileSync(new URL("./process-outbound.excalidraw", import.meta.url), JSON.stringify(doc, null, 2));
console.log(`Wrote process-outbound.excalidraw with ${elements.length} elements.`);
