// Generates docs/diagrams/warehouse.excalidraw — a Domain Discovery / Event Storming board.
// Run: node docs/diagrams/generate-event-storming.mjs
// Event Storming colour code (Brandolini):
//   orange = domain event · blue = command · big yellow = aggregate
//   purple = policy · green = read model · small yellow = actor · pink = hotspot/question
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

// A sticky note = rectangle + centred bound text.
function sticky(kind, x, y, text, opts = {}) {
  const w = opts.w ?? 168;
  const h = opts.h ?? 96;
  const angle = opts.angle ?? 0;
  const fontSize = opts.fontSize ?? 16;
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

// Free-floating label (titles, phase headers, time axis).
function label(x, y, text, fontSize = 28, color = "#343a40", align = "left") {
  elements.push(base({
    id: `l${seedCounter}`, type: "text", x, y, width: text.length * fontSize * 0.6, height: fontSize * 1.3,
    strokeColor: color, backgroundColor: "transparent", fillStyle: "solid",
    fontSize, fontFamily: 1, text, textAlign: align, verticalAlign: "top",
    containerId: null, originalText: text, lineHeight: 1.25,
  }));
}

// Dashed translucent zone behind a phase.
function zone(x, y, w, h, title, color) {
  elements.push(base({
    id: `z${seedCounter}`, type: "rectangle", x, y, width: w, height: h,
    strokeColor: color, backgroundColor: "transparent", fillStyle: "solid",
    strokeStyle: "dashed", strokeWidth: 2, roundness: { type: 3 }, opacity: 60,
  }));
  label(x + 16, y + 12, title, 30, color);
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

// Arrow wrappers that connect sticky handles edge-to-edge.
const aDown  = (a, b, o = {}) => arrow(a.cx, a.y + a.h, b.cx, b.y, o);        // top sticky → lower sticky
const aUp    = (a, b, o = {}) => arrow(a.cx, a.y, b.cx, b.y + b.h, o);        // lower sticky → upper sticky
const aRight = (a, b, o = {}) => arrow(a.x + a.w, a.cy, b.x, b.cy, o);        // left sticky → right sticky
const aSpine = (a, b) => arrow(a.x + a.w, a.cy, b.x, b.cy, { color: "#ced4da", width: 3 }); // business-time spine

// Arrow styles used to read the board as a flow.
const FLOW = { color: "#868e96", width: 1.8 };                          // actor→command, command→event
const EMIT = { color: "#f08c00", width: 1.8 };                          // aggregate→event ("decides / emits")
const POL  = { color: "#6741d9", width: 1.8, style: "dashed" };         // policy trigger / policy→command
const CONV = { color: "#c2255c", width: 2, style: "dashed" };           // cross-aggregate convergence
const HAND = { color: "#1971c2", width: 2, style: "dotted", head: "arrow" }; // two-stage / handover

// A small ⚡ tag marking a pivotal event = a natural service boundary.
function integration(s, text) {
  label(s.x, s.y - 24, "⚡ " + text, 13, "#0b7285");
}

// A participant standing at the board: head + body + role label.
function person(x, y, role) {
  const skin = "#ffd8a8";
  elements.push(base({
    id: `ph${seedCounter}`, type: "ellipse", x, y, width: 34, height: 34,
    strokeColor: "#495057", backgroundColor: skin, fillStyle: "solid", roundness: null,
  }));
  elements.push(base({
    id: `pb${seedCounter}`, type: "ellipse", x: x - 8, y: y + 36, width: 50, height: 40,
    strokeColor: "#495057", backgroundColor: "#dee2e6", fillStyle: "solid", roundness: { type: 2 },
  }));
  label(x - 60, y + 80, role, 15, "#495057", "center");
}

// ----- TITLE -------------------------------------------------------------
label(60, -260, "Domain Discovery — Event Storming", 44, "#212529");
label(60, -200, "Warehouse Management System (WMS)  ·  big-picture session: dev team + Product Owner + client SMEs", 22, "#868e96");

// ----- LEGEND ------------------------------------------------------------
const legendX = 60, legendY = -150;
const legendItems = [
  ["event", "Domain event (past tense)"],
  ["command", "Command (intent / decision)"],
  ["aggregate", "Aggregate (decides)"],
  ["policy", "Policy (whenever… then…)"],
  ["readmodel", "Read model / view"],
  ["actor", "Actor / role"],
  ["hotspot", "Hotspot — question / insight"],
];
legendItems.forEach(([k, t], i) => {
  const lx = legendX + (i % 4) * 360;
  const ly = legendY + Math.floor(i / 4) * 56;
  elements.push(base({
    id: `lg${seedCounter}`, type: "rectangle", x: lx, y: ly, width: 34, height: 28,
    strokeColor: PALETTE[k].stroke, backgroundColor: PALETTE[k].bg, fillStyle: "solid", roundness: { type: 3 },
  }));
  label(lx + 44, ly + 4, t, 17, "#495057");
});
// 8th legend slot: the integration marker.
label(legendX + 3 * 360, legendY + 56 + 4, "⚡  pivotal event → service boundary", 17, "#0b7285");

// ----- TIME AXIS ---------------------------------------------------------
arrow(60, 70, 4360, 70, { color: "#adb5bd", width: 3 });
label(60, 30, "business time  →", 20, "#adb5bd");

// ----- PHASE ZONES -------------------------------------------------------
const ZTOP = 100, ZH = 820;
zone(60,   ZTOP, 880,  ZH, "1 · INBOUND",   "#d9480f");
zone(965,  ZTOP, 520,  ZH, "2 · QUALITY",   "#c2255c");
zone(1510, ZTOP, 760,  ZH, "3 · STORAGE",   "#f08c00");
zone(2295, ZTOP, 1380, ZH, "4 · OUTBOUND",  "#1971c2");
zone(3700, ZTOP, 660,  ZH, "5 · STOCKTAKE", "#2f9e44");

// Rows (y) by element kind. Orange events form the central business-time spine.
const Y = { actor: 175, cmd: 300, event: 440, agg: 590, policy: 720, read: 720 };

// =========================================================================
// 1 · INBOUND  (Logistics: ASN → arrival → goods receipt → dock buffer)
// =========================================================================
const inSupplier = sticky("actor",     90,  Y.actor,  "Supplier");
const cAnnounce   = sticky("command",   90,  Y.cmd,    "Announce delivery (ASN)");
const eAnnounced  = sticky("event",     90,  Y.event,  "DeliveryAnnounced");
const pDockSlot   = sticky("policy",    90,  Y.policy, "Policy: book a dock slot");
const inOperator  = sticky("actor",     320, Y.actor,  "Warehouse Operator");
const cReceipt    = sticky("command",   320, Y.cmd,    "Confirm goods receipt");
const aInbound    = sticky("aggregate", 320, Y.agg,    "InboundDelivery", { fontSize: 18 });
const eArrived    = sticky("event",     320, Y.event,  "DeliveryArrived");
const eReceipt    = sticky("event",     540, Y.event,  "GoodsReceiptConfirmed");
const eReceived   = sticky("event",     760, Y.event,  "StockReceived (dock buffer)");

aDown(inSupplier, cAnnounce, FLOW);
aDown(cAnnounce, eAnnounced, FLOW);
aDown(eAnnounced, pDockSlot, POL);
aUp(aInbound, eArrived, EMIT);
aDown(inOperator, cReceipt, FLOW);
aDown(cReceipt, eReceipt, FLOW);
aSpine(eAnnounced, eArrived);
aSpine(eArrived, eReceipt);
aSpine(eReceipt, eReceived);
integration(eReceipt, "Logistics → Inventory");

// =========================================================================
// 2 · QUALITY  (Inventory: a QC hold on the Batch blocks stock everywhere)
// =========================================================================
const qcInspector = sticky("actor",     990,  Y.actor,  "QC Inspector");
const cBlockBatch = sticky("command",   990,  Y.cmd,    "Quarantine / block batch");
const aBatch      = sticky("aggregate", 990,  Y.agg,    "Batch", { fontSize: 18 });
const eBatchBlock = sticky("event",     1200, Y.event,  "BatchBlocked");
const pQuarantine = sticky("policy",    1200, Y.policy, "Policy: blocked batch → quarantine its stock everywhere");

aDown(qcInspector, cBlockBatch, FLOW);
arrow(cBlockBatch.x + cBlockBatch.w, cBlockBatch.cy, eBatchBlock.x, eBatchBlock.cy, FLOW);
aUp(aBatch, eBatchBlock, EMIT);
aDown(eBatchBlock, pQuarantine, POL);

// =========================================================================
// 3 · STORAGE  (Inventory core: put-away, the StockItem & the ledger)
// =========================================================================
const fkOperator  = sticky("actor",     1540, Y.actor,  "Forklift Operator");
const cPutAway    = sticky("command",   1540, Y.cmd,    "Confirm put-away (scan)");
const pPutAway    = sticky("policy",    1540, Y.policy, "PutAwayPolicy: temperature ∧ capacity ∧ load");
const aStockItem  = sticky("aggregate", 1770, Y.agg,    "StockItem", { fontSize: 18 });
const eStockMoved = sticky("event",     1770, Y.event,  "StockMoved (ledger entry)");
const eQuarantined= sticky("event",     2040, Y.event,  "StockItemQuarantined", { fontSize: 14 });
const aHandling   = sticky("aggregate", 2040, Y.agg,    "HandlingUnit (LPN)", { fontSize: 14 });
const rStockView  = sticky("readmodel", 2040, Y.read,   "Stock levels / ATP view");

aDown(fkOperator, cPutAway, FLOW);
aUp(pPutAway, cPutAway, POL);
arrow(cPutAway.x + cPutAway.w, cPutAway.cy, aStockItem.x, aStockItem.cy - 20, FLOW);
aUp(aStockItem, eStockMoved, EMIT);
arrow(eStockMoved.x + eStockMoved.w, eStockMoved.cy, rStockView.cx, rStockView.y, { color: "#2f9e44", width: 1.8 });
arrow(aHandling.x, aHandling.cy, aStockItem.x + aStockItem.w, aStockItem.cy, { color: "#868e96", width: 1.5, style: "dotted", head: null });
// The QC hold sweeps across stock items already on shelves (cross-aggregate convergence).
arrow(pQuarantine.x + pQuarantine.w, pQuarantine.cy, eQuarantined.cx, eQuarantined.y + eQuarantined.h, CONV);

// =========================================================================
// 4 · OUTBOUND  (order → soft reserve → hard FEFO allocation → pick → ship)
// =========================================================================
const oCustomer  = sticky("actor",     2325, Y.actor,  "Customer / ERP");
const cPlace     = sticky("command",   2325, Y.cmd,    "Place outbound order");
const aOrder     = sticky("aggregate", 2325, Y.agg,    "OutboundOrder", { fontSize: 17 });
const eOrder     = sticky("event",     2325, Y.event,  "OutboundOrderCreated", { fontSize: 14 });
const eSoft      = sticky("event",     2560, Y.event,  "StockReserved (soft)");
const aReserve   = sticky("aggregate", 2560, Y.agg,    "StockReservation", { fontSize: 15 });
const pSoft      = sticky("policy",    2560, Y.policy, "Policy: soft-reserve vs available-to-promise");
const cWave      = sticky("command",   2795, Y.cmd,    "Release wave to floor");
const eHard      = sticky("event",     2795, Y.event,  "StockAllocated (hard)");
const pFefo      = sticky("policy",    2795, Y.policy, "Policy: FEFO allocation + re-check batch quality");
const rPickList  = sticky("readmodel", 3030, Y.read,   "Pick list (routed)");
const oPicker    = sticky("actor",     3030, Y.actor,  "Picker");
const cPick      = sticky("command",   3030, Y.cmd,    "Confirm pick (scan)");
const ePicked    = sticky("event",     3030, Y.event,  "StockPicked");
const pShort     = sticky("policy",    3265, Y.policy, "Policy: short pick → replan + report");
const eShort     = sticky("event",     3265, Y.event,  "ShortPickReported", { fontSize: 14 });
const oCarrier   = sticky("actor",     3500, Y.actor,  "Carrier");
const cPack      = sticky("command",   3500, Y.cmd,    "Pack & confirm dispatch");
const aShipment  = sticky("aggregate", 3500, Y.agg,    "Shipment", { fontSize: 18 });
const eDispatch  = sticky("event",     3500, Y.event,  "ShipmentDispatched");

aDown(oCustomer, cPlace, FLOW);
aDown(cPlace, eOrder, FLOW);
aUp(aOrder, eOrder, EMIT);
aSpine(eOrder, eSoft);
aUp(aReserve, eSoft, EMIT);
arrow(eOrder.x + eOrder.w, eOrder.cy + 30, pSoft.cx, pSoft.y, POL);   // order → soft-reserve policy
aUp(pSoft, eSoft, POL);
// the two-stage allocation — the key teaching point of the board
arrow(eSoft.cx, eSoft.y + eSoft.h, eHard.cx, eHard.y + eHard.h + 4, HAND);
aDown(cWave, eHard, FLOW);
aUp(pFefo, eHard, POL);
arrow(eHard.x + eHard.w, eHard.cy, rPickList.cx - 20, rPickList.y, { color: "#2f9e44", width: 1.8 });
aDown(oPicker, cPick, FLOW);
aDown(cPick, ePicked, FLOW);
arrow(eHard.cx, eHard.cy, ePicked.x, ePicked.cy, HAND);              // allocation → pick
aSpine(ePicked, eDispatch);
// short pick is an inventory problem, not a picking error — it replans, it doesn't "fix"
arrow(ePicked.x + ePicked.w, ePicked.cy, pShort.cx, pShort.y, POL);
aUp(pShort, eShort, POL);
aDown(cPack, eDispatch, FLOW);
aUp(aShipment, eDispatch, EMIT);
aUp(eDispatch, oCarrier, HAND);                                      // hand-over to carrier
integration(eSoft, "Inventory ↔ Logistics");
integration(eDispatch, "Logistics → Customer / ERP");

// =========================================================================
// 5 · STOCKTAKE  (blind count → approved differences → ledger adjustment)
// =========================================================================
const stManager   = sticky("actor",     3730, Y.actor,  "Warehouse Manager");
const cStart       = sticky("command",   3730, Y.cmd,    "Start blind stocktake");
const aStocktake   = sticky("aggregate", 3730, Y.agg,    "Stocktake", { fontSize: 18 });
const eStarted     = sticky("event",     3730, Y.event,  "StocktakeStarted");
const stCounter    = sticky("actor",     3960, Y.actor,  "Counter / Operator");
const cCount       = sticky("command",   3960, Y.cmd,    "Record blind count");
const eCounted     = sticky("event",     3960, Y.event,  "CountRecorded");
const rVariance    = sticky("readmodel", 3960, Y.agg,    "Variance report", { fontSize: 14 });
const pBlind       = sticky("policy",    3960, Y.policy, "Policy: hide expected qty (blind count)");
const cApprove     = sticky("command",   4190, Y.cmd,    "Approve differences");
const eAdjusted    = sticky("event",     4190, Y.event,  "StockAdjusted (ledger)");
const pAdjust      = sticky("policy",    4190, Y.policy, "Policy: differences → ledger adjustments");

aDown(stManager, cStart, FLOW);
aDown(cStart, eStarted, FLOW);
aUp(aStocktake, eStarted, EMIT);
aSpine(eStarted, eCounted);
aSpine(eCounted, eAdjusted);
aDown(stCounter, cCount, FLOW);
aDown(cCount, eCounted, FLOW);
aUp(pBlind, cCount, POL);
arrow(eCounted.x + eCounted.w, eCounted.cy, cApprove.cx, cApprove.y, FLOW);
aUp(pAdjust, eAdjusted, POL);
aDown(cApprove, eAdjusted, FLOW);
arrow(eAdjusted.cx, eAdjusted.y, rVariance.x + rVariance.w, rVariance.cy, { color: "#2f9e44", width: 1.6, style: "dotted" });
// an adjustment is just another append-only ledger entry on the StockItem
label(eAdjusted.x - 4, eAdjusted.y + eAdjusted.h + 6, "→ append-only StockMovement (never an edit)", 12, "#2f9e44");

// ----- HOTSPOTS (the real debates of the session) ------------------------
sticky("hotspot", 540,  Y.policy + 20, "Only ANNOUNCED trucks get received — else ad-hoc ASN first", { angle: -0.04, fontSize: 13, h: 90 });
sticky("hotspot", 330,  Y.actor - 90,  "Pallet count depends on the DELIVERY, not just the catalog!", { angle: 0.05, fontSize: 13, h: 90 });
sticky("hotspot", 1000, Y.policy - 20, "QC blocks the BATCH, not the pallet — suspicious everywhere at once", { angle: 0.06, fontSize: 13, h: 100 });
sticky("hotspot", 1560, Y.event + 5, "Temperature = NON-negotiable.  Capacity = negotiable.", { angle: -0.05, fontSize: 13, h: 80 });
sticky("hotspot", 2330, 825,           "½ of our suppliers also BUY from us → Party + Roles", { angle: 0.05, fontSize: 13, h: 90 });
sticky("hotspot", 2560, 825,           "Don't pin a pallet at order time! Soft-reserve → allocate at the wave", { angle: -0.04, fontSize: 13, h: 90 });
sticky("hotspot", 2795, 825,           "FIFO? No — FEFO! We sell time-to-expiry, not boxes", { angle: 0.05, fontSize: 13, h: 90 });
sticky("hotspot", 3265, 580,           "Short pick = stocktake problem, NOT a picking error", { angle: 0.04, fontSize: 13, h: 90 });
sticky("hotspot", 3730, 825,           "Never EDIT stock — add a CORRECTION (the auditors…)", { angle: 0.04, fontSize: 13, h: 90 });
sticky("hotspot", 3960, 825,           "Blind count: hide the expected qty", { angle: -0.05, fontSize: 13, h: 80 });

// ----- DEPLOYMENT BAND — 5 contexts → 3 services -------------------------
// Phases don't map 1:1 to services: INBOUND/OUTBOUND are split because the
// Logistics process and the Inventory stock-truth live in different services.
const SVC = {
  warehouse:  { bg: "#c3fae8", stroke: "#0ca678", name: "warehouse-service" },
  logistics:  { bg: "#dbe4ff", stroke: "#3b5bdb", name: "logistics-service" },
  masterdata: { bg: "#f3d9fa", stroke: "#9c36b5", name: "masterdata-service" },
};
const BAND_Y = 958, BAND_H = 74;
function svcBand(svc, x, w, line2, y = BAND_Y, h = BAND_H) {
  const c = SVC[svc];
  elements.push(base({
    id: `sb${seedCounter}`, type: "rectangle", x, y, width: w, height: h,
    strokeColor: c.stroke, backgroundColor: c.bg, fillStyle: "solid",
    roundness: { type: 3 }, strokeWidth: 2,
  }));
  label(x + 14, y + 10, c.name, 18, c.stroke);
  label(x + 14, y + 38, line2, 13, "#495057");
}

label(60, 928, "Deployment view — 5 bounded contexts grouped into 3 services (each context stays a module + own DB schema)", 18, "#495057");

// Phase-aligned ownership (x ranges match the phase zones above).
svcBand("logistics", 60,   612, "Logistics (core) — ASN → goods receipt");
svcBand("warehouse", 685,  255, "Inventory (core) — dock buffer");
svcBand("warehouse", 965,  520, "Inventory (core) — Batch QC hold");
svcBand("warehouse", 1510, 760, "Inventory (core) + Topology (supporting) — put-away · ledger");
svcBand("logistics", 2295, 685, "Logistics (core) — order → pick list → shipment");
svcBand("warehouse", 2992, 683, "Inventory (core) — reserve · allocate · pick");
svcBand("warehouse", 3700, 660, "Inventory (core) — blind count → ledger");

// masterdata-service is upstream reference data, not a phase: it feeds local snapshots.
const MD_Y = 1062;
svcBand("masterdata", 965, 2710,
  "Catalog (supporting) + Partners (generic) — upstream: ProductDefined / LocationDefined / PartyRoleRef → local snapshots", MD_Y, 48);
[1225, 1890, 3333].forEach((x) =>
  arrow(x, MD_Y, x, BAND_Y + BAND_H + 2, { color: "#9c36b5", width: 1.6, style: "dashed" }));

// ----- PEOPLE AT THE BOARD ----------------------------------------------
const peopleY = 1180;
const roster = [
  [120,  "Facilitator"],
  [620,  "Product Owner"],
  [1150, "Warehouse Mgr (client)"],
  [1700, "QC Lead (client)"],
  [2450, "Logistics Coord (client)"],
  [3050, "Dev — domain"],
  [3550, "Dev — infra"],
  [4050, "Auditor (client)"],
];
roster.forEach(([x, role]) => person(x, peopleY, role));

// ----- WRITE -------------------------------------------------------------
const doc = {
  type: "excalidraw",
  version: 2,
  source: "https://github.com/warehouse-wms/docs",
  elements,
  appState: { gridSize: null, viewBackgroundColor: "#fffdf5" },
  files: {},
};
writeFileSync(new URL("./warehouse.excalidraw", import.meta.url), JSON.stringify(doc, null, 2));
console.log(`Wrote warehouse.excalidraw with ${elements.length} elements.`);
