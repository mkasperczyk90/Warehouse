import { http, HttpResponse } from 'msw';

import type { CurrentOperator } from '@/features/Auth';
import type { LookupRow } from '@/features/Lookup';
import type { MoveTask } from '@/features/Movement';
import type { PackJob } from '@/features/Packing';
import type { PickStep } from '@/features/Picking';
import type { Receipt } from '@/features/Receiving';
import type { Operator, TaskKind } from '@/features/Tasks';

/**
 * The "server" side of the seam (ADR-0006). These fixtures mirror the terminal
 * prototypes and are returned by the same routes the real Gateway will expose,
 * so turning the worker off is the only step to go live. The `x.model.ts` types
 * are the spec; these fixtures are the contract.
 *
 * Writes are **stateful**: a confirm/exception mutates the in-memory state so the
 * next GET reflects it (the hub count drops, a put-away proposes another bay, a
 * short pick replans). That's the admin pattern — the change persists across the
 * refetch, which is how "confirm" actually does something.
 */

// --- Sign-in (terminal-0-login) --------------------------------------------
// Badge → operator. The reader types the id and emits Enter; an unknown badge
// is a 401 so the screen can show its inline "not recognised" error.
const operators: CurrentOperator[] = [
  { id: 'OP-1', badge: '7700', name: 'M. Operator', site: 'Cold-store · Wrocław WH-01', language: 'en' },
  { id: 'OP-2', badge: '7701', name: 'J. Forklift', site: 'Cold-store · Wrocław WH-01', language: 'pl' },
];

// --- Task hub (terminal-1-hub) ---------------------------------------------
const operator: Operator = {
  name: 'M. Operator',
  site: 'Cold-store · Wrocław WH-01',
  online: true,
};

/** The work piles. Mutable counts — each confirmed task drops its pile. */
const taskDetail: Record<TaskKind, string> = {
  receive: '2 trucks at dock D-3, D-5',
  putaway: '14 pallets in dock buffer',
  pick: 'Wave W-2206 released',
  move: 'Replenishment tasks',
};
const taskCounts: Record<TaskKind, number> = { receive: 2, putaway: 14, pick: 31, move: 5 };
const moveTotal = taskCounts.move;

const tasksList = () =>
  (['receive', 'putaway', 'pick', 'move'] as TaskKind[]).map((kind) => ({
    kind,
    detail: taskDetail[kind],
    count: taskCounts[kind],
  }));

const drop = (kind: TaskKind, by = 1) => {
  taskCounts[kind] = Math.max(0, taskCounts[kind] - by);
};

// --- Goods receipt (terminal-2-receive · UC-02) ----------------------------
const receipt: Receipt = {
  asn: 'ASN-2206',
  supplier: 'Dairy Farms Ltd',
  dock: 'Dock D-3',
  line: 3,
  ofLines: 8,
  sku: '4006381333931',
  product: 'Whole milk 3.2% — 1 L carton',
  expectedQty: 240,
  expectedNote: '(10 cases)',
  unit: 'ea',
  batch: 'LOT-0425-A',
  bestBefore: '2026-07-02',
};

// --- Picking (terminal-4-pick · UC-10) -------------------------------------
// A short pick replans onto the next FEFO-eligible batch/location (§ Outbound).
const pickPlans: Pick<PickStep, 'location' | 'fefo' | 'fefoStatus' | 'qty'>[] = [
  { location: 'WH01-A2-A07-R3-S2', fefo: 'FEFO · BBE 2026-06-28 · LOT-0419', fefoStatus: 'reserved', qty: 24 },
  { location: 'WH01-A2-A09-R1-S2', fefo: 'FEFO · BBE 2026-07-05 · LOT-0511', fefoStatus: 'available', qty: 24 },
];
let pickPlan = 0;

const pickStep = (): PickStep => ({
  wave: 'W-2206',
  order: 'SO-4471',
  picked: 12,
  total: 31,
  sku: '5901234123457',
  product: 'Greek yoghurt 400 g',
  unit: 'ea',
  ...pickPlans[pickPlan],
});

// --- Move / replenish (terminal-5-move · UC-06) ----------------------------
const moveTask = (): MoveTask => ({
  task: moveTotal - taskCounts.move + 1,
  ofTasks: moveTotal,
  from: 'CR1-A03-R2-S1',
  to: 'CR1-PICKFACE-12',
  sku: '4006381333931',
  batch: 'LOT-0425-A',
  product: 'Whole milk 3.2% — 1 L',
  coldChip: '❄ 2–6 °C',
  bbeChip: 'BBE 2026-07-02',
  qty: 48,
  checks: [
    'Destination is a cold room (2–6 °C compatible)',
    'Capacity & load limit OK at destination',
  ],
});

// --- Packing (terminal-6-pack · UC-11) -------------------------------------
let packPkgNo = 1;

const packJob = (): PackJob => ({
  order: 'SO-4471',
  customer: 'Fresh Market sp. z o.o.',
  pkg: `PKG ${packPkgNo} · carton M`,
  weight: '14.8 kg',
  dimensions: '40×30×25 cm',
  lines: [
    { name: 'Greek yoghurt 400 g', lot: 'LOT-0419', qty: '24 ea', done: true },
    { name: 'Whole milk 3.2% 1 L', lot: 'LOT-0425-A', qty: '12 ea', done: true },
    { name: 'Butter block 250 g', lot: 'LOT-0331', qty: '0 / 6 ea', done: false, remaining: 3 },
  ],
});

// --- Look up (read-only inquiry) -------------------------------------------
const lookupIndex: LookupRow[] = [
  {
    id: 'p-milk',
    kind: 'product',
    icon: 'product',
    title: 'Whole milk 3.2% — 1 L carton',
    meta: 'SKU 4006381333931 · ATP across 3 locations',
    qty: { value: 612, unit: 'ea' },
    status: { kind: 'available' },
  },
  {
    id: 'p-yoghurt',
    kind: 'product',
    icon: 'product',
    title: 'Greek yoghurt 400 g',
    meta: 'SKU 5901234123457 · partly reserved for SO-4471',
    qty: { value: 148, unit: 'ea' },
    status: { kind: 'reserved' },
  },
  {
    id: 'p-butter',
    kind: 'product',
    icon: 'product',
    title: 'Butter block 250 g',
    meta: 'SKU 5901234555662 · on QC hold',
    qty: { value: 90, unit: 'ea' },
    status: { kind: 'blocked' },
  },
  {
    id: 'l-cr1',
    kind: 'location',
    icon: 'location',
    title: 'WH01-CR1-A03-R2-S1',
    meta: 'Cold room 2–6 °C · 0.8 m³ free · consolidates LOT-0425-A',
  },
  {
    id: 'l-pickface',
    kind: 'location',
    icon: 'location',
    title: 'CR1-PICKFACE-12',
    meta: 'Cold room pick face · 60% full',
  },
  {
    id: 'l-dock',
    kind: 'location',
    icon: 'location',
    title: 'WH01-DOCK-D3',
    meta: 'Inbound dock buffer · ambient · 14 pallets staged',
  },
  {
    id: 'b-0425',
    kind: 'batch',
    icon: 'batch',
    title: 'LOT-0425-A · Whole milk 3.2%',
    meta: 'BBE 2026-07-02 · FEFO-eligible',
    status: { kind: 'available' },
  },
  {
    id: 'b-0419',
    kind: 'batch',
    icon: 'batch',
    title: 'LOT-0419 · Greek yoghurt 400 g',
    meta: 'BBE 2026-06-28 · nearest expiry — picked first',
    status: { kind: 'reserved' },
  },
  {
    id: 'b-0210',
    kind: 'batch',
    icon: 'batch',
    title: 'LOT-0210 · Cream 30%',
    meta: 'BBE 2026-06-10 · past best-before',
    status: { kind: 'expired' },
  },
];

// --- Scan dispatcher (the Scan tab) ----------------------------------------
/** Seeded scan history (newest first) — the screen resolves each code locally. */
const recentScans: string[] = ['0078-2206-014', 'WH01-CR1-A03-R2-S1', 'ASN-2206'];

const noContent = () => new HttpResponse(null, { status: 204 });

export const handlers = [
  // Sign-in — resolve a scanned badge to an operator (401 on unknown badge).
  http.post('/api/auth/login', async ({ request }) => {
    const { badge } = (await request.json()) as { badge: string };
    const op = operators.find((o) => o.badge === badge.trim());
    return op
      ? HttpResponse.json(op)
      : HttpResponse.json({ code: 'unknown_badge', message: 'Badge not recognised' }, { status: 401 });
  }),

  http.get('/api/operator/me', () => HttpResponse.json(operator)),
  http.get('/api/tasks', () => HttpResponse.json(tasksList())),

  // Goods receipt — the delivery + its lines (Logistics service); confirm a line via /receipt.
  http.get('/api/logistics/deliveries/ASN-2206', () =>
    HttpResponse.json({
      id: receipt.asn,
      supplierRoleId: receipt.supplier,
      warehouseCode: 'WH01',
      status: 'Receiving',
      slot: { dockCode: receipt.dock.replace(/^Dock\s*/, '') },
      lines: Array.from({ length: receipt.ofLines }, (_, i) => {
        const lineNo = i + 1;
        const isTarget = lineNo === receipt.line;
        return {
          lineNo,
          productCode: isTarget ? receipt.sku : `SKU-${lineNo}`,
          expectedQuantity: isTarget ? receipt.expectedQty : 100,
          expectedUnit: receipt.unit,
          actualQuantity: null,
          actualUnit: null,
          batchNumber: isTarget ? receipt.batch : null,
          expiryDate: isTarget ? receipt.bestBefore : null,
          discrepancy: 'None',
          note: null,
        };
      }),
    }),
  ),
  http.post('/api/logistics/deliveries/ASN-2206/lines/3/receipt', async () => {
    drop('receive');
    return noContent();
  }),

  // Put-away — the dock-buffer worklist (Inventory service); confirm moves a line into a bay.
  http.get('/api/inventory/put-away/tasks', () =>
    HttpResponse.json([
      {
        sku: receipt.sku,
        batchNumber: receipt.batch,
        quantity: receipt.expectedQty,
        unit: receipt.unit,
        fromLocation: 'WH01-DOCK-BUFFER',
      },
    ]),
  ),
  http.post('/api/inventory/put-away/confirm', () => {
    drop('putaway');
    return noContent();
  }),

  // Picking — confirm picks the line; "short pick" replans onto the next FEFO batch.
  http.get('/api/wave/W-2206/next', () => HttpResponse.json(pickStep())),
  http.post('/api/wave/W-2206/next/confirm', () => {
    drop('pick', pickPlans[pickPlan].qty);
    pickPlan = 0;
    return noContent();
  }),
  http.post('/api/wave/W-2206/next/short', () => {
    pickPlan = (pickPlan + 1) % pickPlans.length;
    return noContent();
  }),

  // Move — confirm moves; "transfer" issues an inter-warehouse (in-transit) move.
  http.get('/api/move/next', () => HttpResponse.json(moveTask())),
  http.post('/api/move/next/confirm', () => {
    drop('move');
    return noContent();
  }),
  http.post('/api/move/next/transfer', () => {
    drop('move');
    return noContent();
  }),

  // Packing — close the package (done) or open another for the remainder.
  http.get('/api/order/SO-4471/packing', () => HttpResponse.json(packJob())),
  http.post('/api/order/SO-4471/packing/close', () => {
    packPkgNo = 1;
    return noContent();
  }),
  http.post('/api/order/SO-4471/packing/add', () => {
    packPkgNo += 1;
    return noContent();
  }),

  http.get('/api/lookup/index', () => HttpResponse.json(lookupIndex)),
  http.get('/api/scan/recent', () => HttpResponse.json(recentScans)),
];
