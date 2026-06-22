import { http, HttpResponse } from 'msw';

import type { AdjustmentDraft } from '@/features/Adjustment';
import type { DispatchColumn } from '@/features/Dispatch';
import type { AsnDetail, AsnSummary } from '@/features/Inbound';
import type { SoDetail, SoSummary } from '@/features/Outbound';
import type { MovementRow } from '@/features/Movements';
import type { ProductDraft, ProductForm, ProductSummary } from '@/features/Products';
import type { QcBatch } from '@/features/Quality';
import type { Stocktake, StocktakeListItem } from '@/features/Stocktake';
import type { RoomDetail, TopologyNode } from '@/features/Topology';
import type { SearchResult } from '@/features/Search';
import type { Worklist } from '@/features/Today';
import type { StockItemDetail, StockKpis, StockRow } from '@/features/Stock';
import type { Warehouse } from '@/features/Warehouses';
import type { CurrentUser } from '@/features/Auth';
import type { UserProfile } from '@/features/Profile';

/**
 * The "server" side of the seam (ADR-0006). These fixtures mirror the
 * admin-1-stock prototype and are returned by the same routes the real Gateway
 * will expose, so turning the worker off is the only step to go live.
 */
// --- Warehouse scoping ------------------------------------------------------
// Every operational GET is scoped to the warehouse the desk has selected, which
// the api client sends as `X-Warehouse-Id`. Master data (products, topology) is
// cross-warehouse by design and stays global.
const warehouses: Warehouse[] = [
  { id: 'WH-01', code: 'WH-01', name: 'Wrocław' },
  { id: 'WH-02', code: 'WH-02', name: 'Poznań' },
];

/** The active warehouse for a request (defaults to WH-01 when the header is absent). */
const wh = (request: Request) => request.headers.get('X-Warehouse-Id') ?? 'WH-01';

/**
 * Records that live in WH-02 (everything else is WH-01). Fixture ids are unique
 * across entities, so one set keeps the scoping declarative and low-touch.
 */
const WH02 = new Set([
  '6', '7', // stock rows
  'm11', 'm12', // movements
  'ASN-3001', // inbound
  'SO-5001', // outbound
  'B-0500', // qc
  'ST-201', // stocktake
  'SHP-3400', // dispatch
]);
const whOf = (id: string) => (WH02.has(id) ? 'WH-02' : 'WH-01');

/** On-hand KPIs computed from one warehouse's rows. */
function kpisFor(warehouse: string): StockKpis {
  const scoped = rows.filter((r) => whOf(r.id) === warehouse);
  const sum = (pick: (r: StockRow) => number) => scoped.reduce((acc, r) => acc + pick(r), 0);
  return {
    onHand: sum((r) => r.onHand),
    atp: sum((r) => r.atp),
    reserved: sum((r) => r.onHand - r.atp),
    blockedExpiring: scoped
      .filter((r) => r.status === 'blocked' || r.status === 'expired')
      .reduce((acc, r) => acc + r.onHand, 0),
    unit: 'units',
  };
}

// --- Desk users (badge → user, resolved at sign-in) ------------------------
const users: Record<string, UserProfile> = {
  'U-1': {
    id: 'U-1', badge: '1001', name: 'K. Manager', role: 'manager',
    email: 'k.manager@warehouse.example', phone: '+48 600 100 100',
    defaultWarehouseId: 'WH-01', language: 'en', lastLogin: '2026-06-22 07:02',
    recentSessions: [
      { id: 's1', when: '2026-06-22 07:02', device: 'Desk · Chrome / Windows' },
      { id: 's2', when: '2026-06-21 06:58', device: 'Desk · Chrome / Windows' },
    ],
  },
  'U-2': {
    id: 'U-2', badge: '1002', name: 'A. Coordinator', role: 'coordinator',
    email: 'a.coordinator@warehouse.example', phone: '+48 600 100 200',
    defaultWarehouseId: 'WH-01', language: 'pl', lastLogin: '2026-06-22 06:40',
    recentSessions: [{ id: 's1', when: '2026-06-22 06:40', device: 'Desk · Edge / Windows' }],
  },
  'U-3': {
    id: 'U-3', badge: '1003', name: 'M. Inspector', role: 'inspector',
    email: 'm.inspector@warehouse.example', phone: '+48 600 100 300',
    defaultWarehouseId: 'WH-02', language: 'en', lastLogin: '2026-06-21 14:15',
    recentSessions: [{ id: 's1', when: '2026-06-21 14:15', device: 'Desk · Firefox / Linux' }],
  },
};

const toCurrentUser = (p: UserProfile): CurrentUser => ({
  id: p.id, badge: p.badge, name: p.name, role: p.role,
  email: p.email, defaultWarehouseId: p.defaultWarehouseId, language: p.language,
});

const rows: StockRow[] = [
  {
    id: '1',
    product: 'Whole milk 3.2% 1 L',
    sku: '4006381333931',
    batch: 'LOT-0425-A',
    bestBefore: '2026-07-02',
    location: 'CR1-A03-R2-S1',
    room: 'Cold room',
    onHand: 240,
    atp: 216,
    unit: 'ea',
    status: 'available',
    statusLabel: 'Available',
  },
  {
    id: '2',
    product: 'Greek yoghurt 400 g',
    sku: '5901234123457',
    batch: 'LOT-0419',
    bestBefore: '2026-06-28',
    location: 'A2-A07-R3-S2',
    room: 'Standard',
    onHand: 1_440,
    atp: 960,
    unit: 'ea',
    status: 'reserved',
    statusLabel: 'Reserved',
  },
  {
    id: '3',
    product: 'Butter block 250 g',
    sku: '5900512331027',
    batch: 'LOT-0331',
    bestBefore: '2026-06-15',
    location: 'CR1-A01-R1-S4',
    room: 'Cold room',
    onHand: 600,
    atp: 0,
    unit: 'ea',
    status: 'expired',
    statusLabel: 'Expiring 1d',
  },
  {
    id: '4',
    product: 'Frozen berries 1 kg',
    sku: '5601012009873',
    batch: 'LOT-0288',
    bestBefore: '2027-02-10',
    location: 'FZ1-B02-R4-S1',
    room: 'Freezer',
    onHand: 320,
    atp: 320,
    unit: 'ea',
    status: 'transit',
    statusLabel: 'In transit',
  },
  {
    id: '5',
    product: 'Cheese wheel 5 kg',
    sku: '5902860004417',
    batch: 'LOT-0402',
    bestBefore: '2026-09-01',
    location: 'QC-HOLD-02',
    room: 'Quarantine',
    onHand: 48,
    atp: 0,
    unit: 'ea',
    status: 'blocked',
    statusLabel: 'Blocked · QC',
  },
  {
    id: '6',
    product: 'Whole milk 3.2% 1 L',
    sku: '4006381333931',
    batch: 'LOT-0512-PZ',
    bestBefore: '2026-07-20',
    location: 'PZ-CR1-A01-R1-S1',
    room: 'Cold room',
    onHand: 480,
    atp: 480,
    unit: 'ea',
    status: 'available',
    statusLabel: 'Available',
  },
  {
    id: '7',
    product: 'Frozen peas 1 kg',
    sku: '5601012009880',
    batch: 'LOT-0490-PZ',
    bestBefore: '2027-03-01',
    location: 'PZ-FZ1-B01-R2-S3',
    room: 'Freezer',
    onHand: 150,
    atp: 90,
    unit: 'ea',
    status: 'reserved',
    statusLabel: 'Reserved',
  },
];

// --- Inbound / ASN (UC-01, admin-2-asn) ------------------------------------
const asnList: AsnSummary[] = [
  {
    id: 'ASN-2206',
    supplier: 'Dairy Farms Ltd',
    status: 'transit',
    statusLabel: 'Arrived',
    meta: 'Today 09:30 · Dock D-3 · 8 lines',
  },
  {
    id: 'ASN-2207',
    supplier: 'Nordic Frozen AS',
    status: 'reserved',
    statusLabel: 'Announced',
    meta: 'Today 14:00 · Dock D-5 · 12 lines',
  },
  {
    id: 'ASN-2208',
    supplier: 'ACME Packaging',
    status: 'reserved',
    statusLabel: 'Announced',
    meta: 'Tomorrow 08:00 · slot pending · 3 lines',
  },
  {
    id: 'ASN-2205',
    supplier: 'Dairy Farms Ltd',
    status: 'available',
    statusLabel: 'Completed',
    meta: 'Yesterday · received & put away',
  },
  {
    id: 'ASN-3001',
    supplier: 'Wielkopolska Mleczarnia',
    status: 'reserved',
    statusLabel: 'Announced',
    meta: 'Today 11:00 · Dock PZ-1 · 4 lines',
  },
];

const asnDetails: Record<string, AsnDetail> = {
  'ASN-2206': {
    id: 'ASN-2206',
    supplier: 'Dairy Farms Ltd',
    warehouse: 'WH-01 Wrocław',
    dockSlot: 'D-3 · 09:30–10:30',
    createdBy: 'Created by K. Coordinator · validated against catalog',
    status: 'transit',
    statusLabel: 'Arrived · receiving',
    lines: [
      { id: '1', sku: '4006381333931', product: 'Whole milk 3.2% 1 L', planned: 240, unit: 'ea', tracking: 'Batch + BBE' },
      { id: '2', sku: '5901234123457', product: 'Greek yoghurt 400 g', planned: 1_440, unit: 'ea', tracking: 'Batch + BBE' },
      { id: '3', sku: '5900512331027', product: 'Butter block 250 g', planned: 600, unit: 'ea', tracking: 'Batch + BBE' },
      {
        id: '4',
        sku: '—',
        product: 'Unknown SKU 9900001 — flagged for clarification',
        planned: 120,
        unit: 'ea',
        tracking: '—',
        flagged: true,
      },
    ],
  },
  'ASN-2207': {
    id: 'ASN-2207',
    supplier: 'Nordic Frozen AS',
    warehouse: 'WH-01 Wrocław',
    dockSlot: 'D-5 · 14:00–15:00',
    createdBy: 'Created by K. Coordinator · validated against catalog',
    status: 'reserved',
    statusLabel: 'Announced',
    lines: [
      { id: '1', sku: '5601012009873', product: 'Frozen berries 1 kg', planned: 320, unit: 'ea', tracking: 'Batch + BBE' },
      { id: '2', sku: '5601012009880', product: 'Frozen peas 1 kg', planned: 480, unit: 'ea', tracking: 'Batch + BBE' },
    ],
  },
  'ASN-2208': {
    id: 'ASN-2208',
    supplier: 'ACME Packaging',
    warehouse: 'WH-01 Wrocław',
    dockSlot: 'slot pending',
    createdBy: 'Created by K. Coordinator · validated against catalog',
    status: 'reserved',
    statusLabel: 'Announced',
    lines: [
      { id: '1', sku: '5901111000017', product: 'Cardboard box L', planned: 200, unit: 'ea', tracking: 'None' },
      { id: '2', sku: '5901111000024', product: 'Pallet wrap film', planned: 50, unit: 'roll', tracking: 'None' },
      { id: '3', sku: '5901111000031', product: 'Shipping label roll', planned: 30, unit: 'roll', tracking: 'None' },
    ],
  },
  'ASN-2205': {
    id: 'ASN-2205',
    supplier: 'Dairy Farms Ltd',
    warehouse: 'WH-01 Wrocław',
    dockSlot: 'D-3 · (closed)',
    createdBy: 'Created by K. Coordinator · received & put away',
    status: 'available',
    statusLabel: 'Completed',
    lines: [
      { id: '1', sku: '5900512331027', product: 'Butter block 250 g', planned: 600, unit: 'ea', tracking: 'Received' },
      { id: '2', sku: '4006381333931', product: 'Whole milk 3.2% 1 L', planned: 240, unit: 'ea', tracking: 'Received' },
    ],
  },
  'ASN-3001': {
    id: 'ASN-3001',
    supplier: 'Wielkopolska Mleczarnia',
    warehouse: 'WH-02 Poznań',
    dockSlot: 'PZ-1 · 11:00–12:00',
    createdBy: 'Created by A. Coordinator · validated against catalog',
    status: 'reserved',
    statusLabel: 'Announced',
    lines: [
      { id: '1', sku: '4006381333931', product: 'Whole milk 3.2% 1 L', planned: 480, unit: 'ea', tracking: 'Batch + BBE' },
      { id: '2', sku: '5601012009880', product: 'Frozen peas 1 kg', planned: 150, unit: 'ea', tracking: 'Batch + BBE' },
    ],
  },
};

// --- Outbound orders (UC-09, admin-5-outbound) -----------------------------
const orderList: SoSummary[] = [
  { id: 'SO-4471', customer: 'Fresh Market sp. z o.o.', status: 'reserved', statusLabel: 'Reserved', meta: 'Required 2026-06-16 · 3 lines' },
  { id: 'SO-4472', customer: 'Bistro 24', status: 'transit', statusLabel: 'Partially reserved', meta: 'Required 2026-06-16 · 5 lines' },
  { id: 'SO-4470', customer: 'Hotel Vega', status: 'available', statusLabel: 'Picking', meta: 'Wave W-2206 · 4 lines' },
  { id: 'SO-4469', customer: 'Fresh Market sp. z o.o.', status: 'reserved', statusLabel: 'Created', meta: 'Required 2026-06-18 · 2 lines' },
  { id: 'SO-5001', customer: 'Poznań Catering sp. z o.o.', status: 'reserved', statusLabel: 'Reserved', meta: 'Required 2026-06-17 · 2 lines' },
];

const orderDetails: Record<string, SoDetail> = {
  'SO-4471': {
    id: 'SO-4471',
    customer: 'Fresh Market sp. z o.o.',
    status: 'reserved',
    statusLabel: 'Reserved',
    subtitle: 'Soft reservation against ATP · batch+location pinned later by FEFO at wave release',
    linesReserved: '3 / 3',
    reservedUnits: 42,
    shipTo: 'Wrocław, Powstańców 12',
    lines: [
      { id: '1', sku: '5901234123457', product: 'Greek yoghurt 400 g', ordered: 24, atpAtOrder: 960, reserved: 24, status: 'reserved', statusLabel: 'Reserved' },
      { id: '2', sku: '4006381333931', product: 'Whole milk 3.2% 1 L', ordered: 12, atpAtOrder: 216, reserved: 12, status: 'reserved', statusLabel: 'Reserved' },
      { id: '3', sku: '5900512331027', product: 'Butter block 250 g', ordered: 6, atpAtOrder: 8, reserved: 6, status: 'reserved', statusLabel: 'Reserved' },
    ],
  },
  'SO-4472': {
    id: 'SO-4472',
    customer: 'Bistro 24',
    status: 'transit',
    statusLabel: 'Partially reserved',
    subtitle: 'One line short of ATP — partial / waiting decision pending',
    linesReserved: '4 / 5',
    reservedUnits: 68,
    shipTo: 'Wrocław, Rynek 7',
    lines: [
      { id: '1', sku: '5901234123457', product: 'Greek yoghurt 400 g', ordered: 36, atpAtOrder: 936, reserved: 36, status: 'reserved', statusLabel: 'Reserved' },
      { id: '2', sku: '5900512331027', product: 'Butter block 250 g', ordered: 20, atpAtOrder: 8, reserved: 8, status: 'transit', statusLabel: 'Partial' },
    ],
  },
  'SO-4470': {
    id: 'SO-4470',
    customer: 'Hotel Vega',
    status: 'available',
    statusLabel: 'Picking',
    subtitle: 'Released to wave W-2206 · FEFO allocation in progress',
    linesReserved: '4 / 4',
    reservedUnits: 96,
    shipTo: 'Wrocław, Kazimierza 3',
    lines: [
      { id: '1', sku: '5601012009873', product: 'Frozen berries 1 kg', ordered: 48, atpAtOrder: 320, reserved: 48, status: 'available', statusLabel: 'Allocated' },
      { id: '2', sku: '4006381333931', product: 'Whole milk 3.2% 1 L', ordered: 48, atpAtOrder: 216, reserved: 48, status: 'available', statusLabel: 'Allocated' },
    ],
  },
  'SO-4469': {
    id: 'SO-4469',
    customer: 'Fresh Market sp. z o.o.',
    status: 'reserved',
    statusLabel: 'Created',
    subtitle: 'Created · not yet reserved against ATP',
    linesReserved: '0 / 2',
    reservedUnits: 0,
    shipTo: 'Wrocław, Powstańców 12',
    lines: [
      { id: '1', sku: '5901234123457', product: 'Greek yoghurt 400 g', ordered: 60, atpAtOrder: 960, reserved: 0, status: 'reserved', statusLabel: 'Created' },
      { id: '2', sku: '5900512331027', product: 'Butter block 250 g', ordered: 10, atpAtOrder: 8, reserved: 0, status: 'reserved', statusLabel: 'Created' },
    ],
  },
  'SO-5001': {
    id: 'SO-5001',
    customer: 'Poznań Catering sp. z o.o.',
    status: 'reserved',
    statusLabel: 'Reserved',
    subtitle: 'Soft reservation against ATP · Poznań stock',
    linesReserved: '2 / 2',
    reservedUnits: 36,
    shipTo: 'Poznań, Półwiejska 4',
    lines: [
      { id: '1', sku: '4006381333931', product: 'Whole milk 3.2% 1 L', ordered: 24, atpAtOrder: 480, reserved: 24, status: 'reserved', statusLabel: 'Reserved' },
      { id: '2', sku: '5601012009880', product: 'Frozen peas 1 kg', ordered: 12, atpAtOrder: 90, reserved: 12, status: 'reserved', statusLabel: 'Reserved' },
    ],
  },
};

// --- Dispatch board (UC-12, admin-6-dispatch) ------------------------------
const dispatchBoard: DispatchColumn[] = [
  {
    key: 'awaitingCarrier',
    shipments: [
      { id: 'SHP-3310', customer: 'Hotel Vega', summary: '2 pkg · 28 kg', canAssign: true },
      { id: 'SHP-3311', customer: 'Bistro 24', summary: '1 pkg · 9 kg', canAssign: true },
      { id: 'SHP-3400', customer: 'Poznań Catering', summary: '3 pkg · 34 kg', canAssign: true },
    ],
  },
  {
    key: 'assigned',
    shipments: [
      { id: 'SHP-3308', customer: 'Fresh Market', summary: '3 pkg · 41 kg', carrier: { code: 'DH', name: 'DHL' }, pickup: 'pickup 14:00' },
      { id: 'SHP-3309', customer: 'Resto Group', summary: '4 pkg · 63 kg', carrier: { code: 'GL', name: 'GLS' }, pickup: 'pickup 15:30' },
    ],
  },
  {
    key: 'noticeSent',
    shipments: [
      {
        id: 'SHP-3305',
        customer: 'Fresh Market',
        summary: '2 pkg',
        carrier: { code: 'DH', name: 'DHL' },
        badge: { variant: 'transit', label: 'Awaiting collection' },
      },
    ],
  },
  {
    key: 'dispatched',
    shipments: [
      {
        id: 'SHP-3302',
        customer: 'Hotel Vega',
        summary: '1 pkg',
        carrier: { code: 'GL', name: 'GLS' },
        badge: { variant: 'available', label: 'Collected ✓' },
        tracking: 'Tracking GLS-PL-99213 · waybill issued',
      },
      {
        id: 'SHP-3301',
        customer: 'Bistro 24',
        summary: '1 pkg',
        carrier: { code: 'DH', name: 'DHL' },
        badge: { variant: 'available', label: 'Collected ✓' },
        tracking: 'Tracking DHL-PL-44188 · waybill issued',
      },
    ],
  },
];

// --- Adjustment (UC-08, admin-9-adjustment) --------------------------------
const adjustmentDraft: AdjustmentDraft = {
  itemId: 'SI-7781',
  product: 'Butter block 250 g',
  batch: 'LOT-0331',
  sku: '5900512331027',
  location: 'CR1-A01-R1-S4',
  room: 'Cold room 1',
  status: 'available',
  statusLabel: 'Available',
  systemOnHand: 600,
  unit: 'ea',
};

// --- Stocktake (UC-07, admin-3-stocktake) — stateful: start adds a count ----
const stocktakesById: Record<string, Stocktake> = {
  'ST-118': {
    summary: {
      id: 'ST-118',
      title: 'Stocktake ST-118 — Cold room 1, aisle A',
      sub: 'Blind count by 2 operators · counted 2026-06-14 07:10 · awaiting approval',
      state: 'review',
      locationsCounted: 42,
      totalLocations: 42,
      matches: 37,
      discrepancies: 5,
      netVariance: -86,
    },
    diffs: [
      { id: '1', location: 'CR1-A01-R1-S4', product: 'Butter block 250 g', batch: 'LOT-0331', system: 600, counted: 588, delta: -12, defaultReason: 'damage' },
      { id: '2', location: 'CR1-A02-R3-S2', product: 'Whole milk 3.2% 1 L', batch: 'LOT-0425-A', system: 240, counted: 216, delta: -24, defaultReason: 'loss' },
      { id: '3', location: 'CR1-A02-R4-S1', product: 'Greek yoghurt 400 g', batch: 'LOT-0419', system: 1_440, counted: 1_392, delta: -48, defaultReason: 'pickError' },
      { id: '4', location: 'CR1-A03-R2-S1', product: 'Cream 30% 0.5 L', batch: 'LOT-0410', system: 120, counted: 122, delta: 2, defaultReason: 'countCorrection' },
      { id: '5', location: 'CR1-A03-R2-S3', product: 'Kefir 1 L', batch: 'LOT-0388', system: 90, counted: 86, delta: -4 },
    ],
  },
  'ST-117': {
    summary: {
      id: 'ST-117',
      title: 'Stocktake ST-117 — Freezer 1',
      sub: 'Completed 2026-06-08 · posted to ledger',
      state: 'completed',
      locationsCounted: 48,
      totalLocations: 48,
      matches: 46,
      discrepancies: 2,
      netVariance: -10,
    },
    diffs: [],
  },
  'ST-119': {
    summary: {
      id: 'ST-119',
      title: 'Stocktake ST-119 — Standard hall A',
      sub: 'Scheduled for 2026-06-20',
      state: 'scheduled',
      locationsCounted: 0,
      totalLocations: 120,
      matches: 0,
      discrepancies: 0,
      netVariance: 0,
    },
    diffs: [],
  },
  'ST-201': {
    summary: {
      id: 'ST-201',
      title: 'Stocktake ST-201 — Poznań cold room',
      sub: 'Blind count by 1 operator · counted 2026-06-21 18:20 · awaiting approval',
      state: 'review',
      locationsCounted: 18,
      totalLocations: 18,
      matches: 16,
      discrepancies: 2,
      netVariance: -18,
    },
    diffs: [
      { id: '1', location: 'PZ-CR1-A01-R1-S1', product: 'Whole milk 3.2% 1 L', batch: 'LOT-0512-PZ', system: 480, counted: 468, delta: -12, defaultReason: 'loss' },
      { id: '2', location: 'PZ-FZ1-B01-R2-S3', product: 'Frozen peas 1 kg', batch: 'LOT-0490-PZ', system: 150, counted: 144, delta: -6, defaultReason: 'damage' },
    ],
  },
};
let stocktakeCounter = 119;

const stocktakeListItem = (s: Stocktake): StocktakeListItem => ({
  id: s.summary.id,
  scope: s.summary.title.split('— ')[1] ?? s.summary.title,
  state: s.summary.state,
  when: s.summary.sub,
  locationsCounted: s.summary.locationsCounted,
  totalLocations: s.summary.totalLocations,
  discrepancies: s.summary.discrepancies,
});

// --- QC worklist (UC-03, admin-8-qc) — stateful: decisions remove the batch -
let qcBatches: QcBatch[] = [
  { id: 'B-0402', batch: 'LOT-0402', product: 'Cheese wheel 5 kg', sku: '5902860004417', fromReceipt: 'GR-2206 · Dairy Farms', location: 'QC-HOLD-02', qty: 48, unit: 'ea', status: 'blocked', statusLabel: 'Quarantine' },
  { id: 'B-0331', batch: 'LOT-0331', product: 'Butter block 250 g', sku: '5900512331027', fromReceipt: 'GR-2205 · Dairy Farms', location: 'QC-HOLD-01', qty: 120, unit: 'ea', status: 'blocked', statusLabel: 'Quarantine' },
  { id: 'B-0419', batch: 'LOT-0419', product: 'Greek yoghurt 400 g', sku: '5901234123457', fromReceipt: 'GR-2206 · Dairy Farms', location: 'QC-HOLD-02', qty: 48, unit: 'ea', status: 'blocked', statusLabel: 'Quarantine' },
  { id: 'B-0288', batch: 'LOT-0288', product: 'Frozen berries 1 kg', sku: '5601012009873', fromReceipt: 'GR-2204 · Nordic Frozen', location: 'QC-HOLD-03', qty: 20, unit: 'ea', status: 'expired', statusLabel: 'Damaged on receipt' },
  { id: 'B-0500', batch: 'LOT-0500-PZ', product: 'Frozen peas 1 kg', sku: '5601012009880', fromReceipt: 'GR-3100 · Wielkopolska', location: 'PZ-QC-HOLD-01', qty: 30, unit: 'ea', status: 'blocked', statusLabel: 'Quarantine' },
];

// --- Products (UC-13, admin-4-product) — stateful: create adds to the catalogue
const productsBySku: Record<string, ProductDraft> = {
  '4006381333931': { sku: '4006381333931', lastEdited: '2026-05-30', name: 'Whole milk 3.2% — 1 L carton', ean: '4006381333931', category: 'dairy', unit: 'ea', length: 70, width: 70, height: 200, weight: 1_030, packConversion: '1 case = 24 ea (catalog default)', tempMin: 2, tempMax: 6, hazardous: false, batchTracked: true, expiryTracked: true },
  '5901234123457': { sku: '5901234123457', lastEdited: '2026-05-22', name: 'Greek yoghurt 400 g', ean: '5901234123457', category: 'dairy', unit: 'ea', length: 95, width: 95, height: 60, weight: 410, packConversion: '1 case = 12 ea', tempMin: 2, tempMax: 6, hazardous: false, batchTracked: true, expiryTracked: true },
  '5900512331027': { sku: '5900512331027', lastEdited: '2026-05-18', name: 'Butter block 250 g', ean: '5900512331027', category: 'dairy', unit: 'ea', length: 110, width: 60, height: 40, weight: 250, packConversion: '1 case = 40 ea', tempMin: 2, tempMax: 6, hazardous: false, batchTracked: true, expiryTracked: true },
  '5601012009873': { sku: '5601012009873', lastEdited: '2026-04-30', name: 'Frozen berries 1 kg', ean: '5601012009873', category: 'frozen', unit: 'ea', length: 200, width: 140, height: 60, weight: 1_000, packConversion: '1 case = 10 ea', tempMin: -18, tempMax: -18, hazardous: false, batchTracked: true, expiryTracked: true },
  '5902860004417': { sku: '5902860004417', lastEdited: '2026-05-10', name: 'Cheese wheel 5 kg', ean: '5902860004417', category: 'dairy', unit: 'kg', length: 250, width: 250, height: 120, weight: 5_000, packConversion: '1 pallet = 24 ea', tempMin: 2, tempMax: 8, hazardous: false, batchTracked: true, expiryTracked: true },
  '5901111000017': { sku: '5901111000017', lastEdited: '2026-03-15', name: 'Cardboard box L', ean: '5901111000017', category: 'packaging', unit: 'ea', length: 600, width: 400, height: 400, weight: 320, packConversion: '1 bale = 25 ea', tempMin: 10, tempMax: 30, hazardous: false, batchTracked: false, expiryTracked: false },
};

const toSummary = (p: ProductDraft): ProductSummary => ({
  sku: p.sku,
  name: p.name,
  category: p.category,
  unit: p.unit,
  tempMin: p.tempMin,
  tempMax: p.tempMax,
  batchTracked: p.batchTracked,
  expiryTracked: p.expiryTracked,
});

// --- Topology (UC-14, admin-7-topology) ------------------------------------
const topologyTree: TopologyNode[] = [
  { id: 'WH-01', level: 1, label: 'WH-01 Wrocław', kind: 'warehouse', icon: 'warehouse' },
  { id: 'CR1', level: 2, label: 'Cold room 1', kind: 'room', icon: 'cold', tag: '2–6 °C' },
  { id: 'CR1-A01-R1-S4', level: 3, label: 'CR1-A01-R1-S4', kind: 'location', icon: 'location' },
  { id: 'CR1-A03-R2-S1', level: 3, label: 'CR1-A03-R2-S1', kind: 'location', icon: 'location' },
  { id: 'FZ1', level: 2, label: 'Freezer 1', kind: 'room', icon: 'freezer', tag: '−18 °C' },
  { id: 'STD', level: 2, label: 'Standard hall A', kind: 'room', icon: 'standard' },
  { id: 'HZ', level: 2, label: 'Hazmat zone', kind: 'room', icon: 'hazmat' },
  { id: 'DOCK', level: 2, label: 'Docks (D-1 … D-6)', kind: 'room', icon: 'dock' },
  { id: 'WH-02', level: 1, label: 'WH-02 Poznań', kind: 'warehouse', icon: 'warehouse' },
];

const topologyRooms: Record<string, RoomDetail> = {
  CR1: {
    id: 'CR1', name: 'Cold room 1', warehouse: 'WH-01', type: 'cold', tempMin: 2, tempMax: 6, shownCount: 2, totalCount: 96,
    locations: [
      { id: '1', address: 'CR1-A01-R1-S4', capacity: 1.2, loadLimit: 500, occupied: '85%' },
      { id: '2', address: 'CR1-A03-R2-S1', capacity: 1.2, loadLimit: 500, occupied: '33%' },
    ],
  },
  FZ1: {
    id: 'FZ1', name: 'Freezer 1', warehouse: 'WH-01', type: 'freezer', tempMin: -18, tempMax: -18, shownCount: 1, totalCount: 48,
    locations: [{ id: '1', address: 'FZ1-B02-R4-S1', capacity: 1.5, loadLimit: 600, occupied: '50%' }],
  },
  STD: {
    id: 'STD', name: 'Standard hall A', warehouse: 'WH-01', type: 'standard', tempMin: 15, tempMax: 25, shownCount: 2, totalCount: 240,
    locations: [
      { id: '1', address: 'A2-A07-R3-S2', capacity: 2.0, loadLimit: 800, occupied: '60%' },
      { id: '2', address: 'A2-A07-R3-S3', capacity: 2.0, loadLimit: 800, occupied: '12%' },
    ],
  },
  HZ: {
    id: 'HZ', name: 'Hazmat zone', warehouse: 'WH-01', type: 'hazmat', tempMin: 10, tempMax: 25, shownCount: 1, totalCount: 12,
    locations: [{ id: '1', address: 'HZ-A01-R1-S1', capacity: 1.0, loadLimit: 400, occupied: '20%' }],
  },
  DOCK: {
    id: 'DOCK', name: 'Docks', warehouse: 'WH-01', type: 'dock', tempMin: 0, tempMax: 30, shownCount: 1, totalCount: 6,
    locations: [{ id: '1', address: 'D-3', capacity: 0, loadLimit: 0, occupied: '—' }],
  },
};

/** Build a stock item's drill-down (breakdown + a small movement history) from a row. */
function stockItemDetail(r: StockRow): StockItemDetail {
  const picked = r.onHand - r.atp;
  return {
    id: r.id,
    product: r.product,
    sku: r.sku,
    batch: r.batch,
    bestBefore: r.bestBefore,
    location: r.location,
    room: r.room,
    onHand: r.onHand,
    atp: r.atp,
    reserved: r.onHand - r.atp,
    unit: r.unit,
    status: r.status,
    statusLabel: r.statusLabel,
    movements: [
      { id: 'm1', date: '2026-06-10', type: 'Goods receipt', qty: r.onHand, reference: 'GR-2206' },
      { id: 'm2', date: '2026-06-10', type: 'Put-away', qty: 0, reference: 'PA-1180' },
      ...(picked > 0
        ? [{ id: 'm3', date: '2026-06-14', type: 'Pick', qty: -picked, reference: 'SO-4470' }]
        : []),
    ],
  };
}

/** Global search across the whole admin — the desk's "where is X" (max 8 hits). */
function searchAll(query: string, warehouse: string): SearchResult[] {
  const q = query.toLowerCase();
  const hit = (s: string) => s.toLowerCase().includes(q);
  const out: SearchResult[] = [];

  for (const p of Object.values(productsBySku)) {
    if ([p.sku, p.name, p.ean].some(hit))
      out.push({ type: 'product', refId: p.sku, label: p.name, sublabel: `SKU ${p.sku}` });
  }
  for (const r of rows) {
    if (whOf(r.id) !== warehouse) continue;
    if ([r.sku, r.product, r.batch, r.location].some(hit))
      out.push({
        type: 'stock',
        refId: r.id,
        label: `${r.product} · ${r.batch}`,
        sublabel: `${r.location} · ${r.sku}`,
      });
  }
  for (const a of asnList) {
    if (whOf(a.id) !== warehouse) continue;
    if ([a.id, a.supplier].some(hit))
      out.push({ type: 'asn', refId: a.id, label: a.id, sublabel: a.supplier });
  }
  for (const o of orderList) {
    if (whOf(o.id) !== warehouse) continue;
    if ([o.id, o.customer].some(hit))
      out.push({ type: 'order', refId: o.id, label: o.id, sublabel: o.customer });
  }
  for (const col of dispatchBoard) {
    for (const sh of col.shipments) {
      if (whOf(sh.id) !== warehouse) continue;
      if ([sh.id, sh.customer].some(hit))
        out.push({ type: 'shipment', refId: sh.id, label: sh.id, sublabel: sh.customer });
    }
  }
  for (const room of Object.values(topologyRooms)) {
    for (const loc of room.locations) {
      if (hit(loc.address))
        out.push({ type: 'location', refId: loc.address, label: loc.address, sublabel: room.name });
    }
  }
  return out.slice(0, 8);
}

/** The work-queue landing (admin-10) — a new lens on existing data, not a new copy. */
function buildWorklist(warehouse: string): Worklist {
  const scopedQc = qcBatches.filter((b) => whOf(b.id) === warehouse);
  const scopedAsn = asnList.filter((a) => whOf(a.id) === warehouse);
  const partial = orderList
    .filter((o) => whOf(o.id) === warehouse)
    .filter((o) => ['Partially reserved', 'Created', 'Picking'].includes(o.statusLabel))
    .slice(0, 3);
  const expiringSpec = [
    { id: '3', days: 1 },
    { id: '1', days: 5 },
    { id: '2', days: 6 },
    { id: '5', days: 7 },
  ];
  const expiringItems = expiringSpec.flatMap(({ id, days }) => {
    const r = rows.find((x) => x.id === id);
    return r && whOf(r.id) === warehouse
      ? [
          {
            id,
            label: `${r.product} · ${r.batch}`,
            sublabel: `${r.location} · ${r.room}`,
            badge: { variant: 'expired' as const, label: `BBE ${days}d` },
            meta: r.bestBefore,
          },
        ]
      : [];
  });
  const reviewStocktakes = Object.values(stocktakesById).filter(
    (s) => s.summary.state === 'review' && whOf(s.summary.id) === warehouse,
  );

  return {
    counts: {
      qc: scopedQc.length,
      expiring: expiringItems.length,
      partial: partial.length,
      inbound: scopedAsn.length,
      stocktake: reviewStocktakes.length,
    },
    queues: [
      {
        key: 'qc',
        count: scopedQc.length,
        items: scopedQc.slice(0, 3).map((b) => ({
          id: b.id,
          label: `${b.batch} · ${b.product}`,
          sublabel: `${b.location} · ${b.fromReceipt}`,
          badge: { variant: b.status, label: b.statusLabel },
          meta: `${b.qty} ${b.unit}`,
        })),
      },
      {
        key: 'partial',
        count: partial.length,
        items: partial.map((o) => ({
          id: o.id,
          label: `${o.id} · ${o.customer}`,
          sublabel: o.meta,
          badge: { variant: o.status, label: o.statusLabel },
        })),
      },
      {
        key: 'expiring',
        count: expiringItems.length,
        items: expiringItems,
      },
      {
        key: 'inbound',
        count: scopedAsn.length,
        items: scopedAsn.slice(0, 2).map((a) => ({
          id: a.id,
          label: `${a.id} · ${a.supplier}`,
          sublabel: a.meta,
          badge: { variant: a.status, label: a.statusLabel },
        })),
      },
    ],
  };
}

let asnCounter = 2208;
let orderCounter = 4472;
let topoRoomCounter = 0;
const carrierNames: Record<string, string> = { DH: 'DHL', GL: 'GLS', DP: 'DPD' };

/** Flatten topology rooms into candidate move targets, with each room's type. */
function allLocations() {
  return Object.values(topologyRooms).flatMap((room) =>
    room.locations.map((l) => ({ address: l.address, room: room.name, roomType: room.type })),
  );
}

// --- Movements ledger (read-only projection source) ------------------------
const movements: MovementRow[] = [
  { id: 'm1', date: '2026-06-20 09:34', type: 'receipt', typeLabel: 'Goods receipt', product: 'Whole milk 3.2% 1 L', sku: '4006381333931', batch: 'LOT-0425-A', location: 'Dock D-3', qty: 240, unit: 'ea', reference: 'GR-2206' },
  { id: 'm2', date: '2026-06-20 10:12', type: 'putaway', typeLabel: 'Put-away', product: 'Whole milk 3.2% 1 L', sku: '4006381333931', batch: 'LOT-0425-A', location: 'CR1-A03-R2-S1', qty: 240, unit: 'ea', reference: 'PA-1180' },
  { id: 'm3', date: '2026-06-20 11:05', type: 'pick', typeLabel: 'Pick', product: 'Greek yoghurt 400 g', sku: '5901234123457', batch: 'LOT-0419', location: 'A2-A07-R3-S2', qty: -480, unit: 'ea', reference: 'SO-4470' },
  { id: 'm4', date: '2026-06-20 13:20', type: 'move', typeLabel: 'Move', product: 'Butter block 250 g', sku: '5900512331027', batch: 'LOT-0331', location: 'CR1-A01-R1-S4', qty: 600, unit: 'ea', reference: 'MV-0442' },
  { id: 'm5', date: '2026-06-20 14:02', type: 'adjustment', typeLabel: 'Adjustment', product: 'Butter block 250 g', sku: '5900512331027', batch: 'LOT-0331', location: 'CR1-A01-R1-S4', qty: -12, unit: 'ea', reference: 'ADJ-0091' },
  { id: 'm6', date: '2026-06-19 08:50', type: 'receipt', typeLabel: 'Goods receipt', product: 'Frozen berries 1 kg', sku: '5601012009873', batch: 'LOT-0288', location: 'Dock D-5', qty: 320, unit: 'ea', reference: 'GR-2204' },
  { id: 'm7', date: '2026-06-19 09:30', type: 'putaway', typeLabel: 'Put-away', product: 'Frozen berries 1 kg', sku: '5601012009873', batch: 'LOT-0288', location: 'FZ1-B02-R4-S1', qty: 320, unit: 'ea', reference: 'PA-1176' },
  { id: 'm8', date: '2026-06-19 15:40', type: 'pick', typeLabel: 'Pick', product: 'Whole milk 3.2% 1 L', sku: '4006381333931', batch: 'LOT-0425-A', location: 'CR1-A03-R2-S1', qty: -24, unit: 'ea', reference: 'SO-4471' },
  { id: 'm9', date: '2026-06-18 12:10', type: 'adjustment', typeLabel: 'Adjustment', product: 'Greek yoghurt 400 g', sku: '5901234123457', batch: 'LOT-0419', location: 'A2-A07-R3-S2', qty: -48, unit: 'ea', reference: 'ST-118' },
  { id: 'm10', date: '2026-06-18 16:25', type: 'move', typeLabel: 'Move', product: 'Cheese wheel 5 kg', sku: '5902860004417', batch: 'LOT-0402', location: 'QC-HOLD-02', qty: 48, unit: 'ea', reference: 'MV-0438' },
  { id: 'm11', date: '2026-06-20 08:15', type: 'receipt', typeLabel: 'Goods receipt', product: 'Whole milk 3.2% 1 L', sku: '4006381333931', batch: 'LOT-0512-PZ', location: 'Dock PZ-1', qty: 480, unit: 'ea', reference: 'GR-3101' },
  { id: 'm12', date: '2026-06-20 09:40', type: 'putaway', typeLabel: 'Put-away', product: 'Whole milk 3.2% 1 L', sku: '4006381333931', batch: 'LOT-0512-PZ', location: 'PZ-CR1-A01-R1-S1', qty: 480, unit: 'ea', reference: 'PA-3055' },
];

// --- Inbound: adapt the view-model seed to the Logistics backend wire shapes -----------------
// The seed above is kept view-model shaped (it also feeds Search/Worklist); these mappers project
// it into the `logistics/deliveries` DTOs the real Warehouse.Logistics.Api returns.
const DELIVERY_STATUS: Record<string, string> = {
  Announced: 'Announced',
  Arrived: 'Arrived',
  'Arrived · receiving': 'Receiving',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};
const toDeliveryStatus = (statusLabel: string) => DELIVERY_STATUS[statusLabel] ?? 'Announced';
const asnPlannedAt: Record<string, string> = {};
const asnSlot: Record<string, { dockCode: string; from: string; to: string }> = {};
const plannedAtOf = (id: string) => asnPlannedAt[id] ?? '2026-06-22T09:30:00.000Z';

// Demo receiving progress encoded into the delivery's actual quantities, so the screen can derive it
// from GET delivery (the backend has no separate receiving endpoint).
const RECEIVING_STATES = new Set(['Receiving', 'Received', 'PutAwayInProgress']);
function actualFor(status: string, index: number, expected: number): number | null {
  if (status === 'Completed') return expected;
  if (!RECEIVING_STATES.has(status)) return null;
  if (index === 0) return expected;
  if (index === 1) return Math.floor(expected / 2);
  return null;
}

function deliverySummaryDto(a: AsnSummary) {
  const d = asnDetails[a.id];
  return {
    id: a.id,
    warehouseCode: d?.warehouse ?? '',
    plannedAt: plannedAtOf(a.id),
    status: toDeliveryStatus(a.statusLabel),
    lineCount: d?.lines.length ?? 0,
  };
}

function deliverySlotDto(id: string, dockSlot: string) {
  const stored = asnSlot[id];
  if (stored) return stored;
  if (!dockSlot || dockSlot === 'slot pending') return null;
  const dockCode = dockSlot.split('·')[0].trim();
  const m = dockSlot.match(/(\d{1,2}):(\d{2})\D+(\d{1,2}):(\d{2})/);
  const base = new Date('2026-06-22T00:00:00');
  if (m) {
    const from = new Date(base);
    from.setHours(Number(m[1]), Number(m[2]), 0, 0);
    const to = new Date(base);
    to.setHours(Number(m[3]), Number(m[4]), 0, 0);
    return { dockCode, from: from.toISOString(), to: to.toISOString() };
  }
  return { dockCode, from: base.toISOString(), to: new Date(base.getTime() + 3_600_000).toISOString() };
}

function deliveryDto(d: AsnDetail) {
  const status = toDeliveryStatus(d.statusLabel);
  return {
    id: d.id,
    supplierRoleId: d.supplier,
    warehouseCode: d.warehouse,
    plannedAt: plannedAtOf(d.id),
    status,
    slot: deliverySlotDto(d.id, d.dockSlot),
    lines: d.lines.map((l, i) => {
      const actual = actualFor(status, i, l.planned);
      return {
        lineNo: Number(l.id),
        productCode: l.sku,
        expectedQuantity: l.planned,
        expectedUnit: l.unit,
        actualQuantity: actual,
        actualUnit: actual != null ? l.unit : null,
        batchNumber: null,
        expiryDate: null,
        discrepancy: 'None',
        note: null,
      };
    }),
  };
}

// --- Outbound: adapt the view-model seed to the Logistics order DTOs ------------------------
const ORDER_STATUS: Record<string, string> = {
  Created: 'Created',
  'Partially reserved': 'PartiallyReserved',
  Waiting: 'PartiallyReserved',
  Reserved: 'Reserved',
  Picking: 'Picking',
  Packed: 'Packed',
  Dispatched: 'Dispatched',
  Cancelled: 'Cancelled',
};
const toOrderStatus = (statusLabel: string) => ORDER_STATUS[statusLabel] ?? 'Created';
const requiredAtOf = (meta: string) => {
  const m = meta.match(/(\d{4}-\d{2}-\d{2})/);
  return m ? `${m[1]}T00:00:00.000Z` : '2026-06-16T00:00:00.000Z';
};

function orderSummaryDto(s: SoSummary) {
  const d = orderDetails[s.id];
  return {
    id: s.id,
    warehouseCode: whOf(s.id),
    requiredAt: requiredAtOf(s.meta),
    status: toOrderStatus(s.statusLabel),
    lineCount: d?.lines.length ?? 0,
  };
}

function shipToDto(shipTo: string) {
  const parts = shipTo.split(',').map((p) => p.trim());
  return { street: parts.slice(1).join(', ') || parts[0] || '—', city: parts[0] || '—', postalCode: '00-000', countryCode: 'PL' };
}

function orderDto(d: SoDetail) {
  return {
    id: d.id,
    customerRoleId: d.customer,
    warehouseCode: whOf(d.id),
    requiredAt: '2026-06-16T00:00:00.000Z',
    status: toOrderStatus(d.statusLabel),
    shipTo: shipToDto(d.shipTo),
    lines: d.lines.map((l) => ({ lineNo: Number(l.id), productCode: l.sku, quantity: l.ordered, unit: 'ea' })),
  };
}

export const handlers = [
  http.get('/api/warehouses', () => HttpResponse.json(warehouses)),
  http.post('/api/auth/login', async ({ request }) => {
    const { badge } = (await request.json()) as { badge: string };
    const user = Object.values(users).find((u) => u.badge === badge.trim());
    if (!user)
      return HttpResponse.json(
        { code: 'unknown_badge', message: 'Badge not recognised' },
        { status: 401 },
      );
    return HttpResponse.json(toCurrentUser(user));
  }),
  http.get('/api/profile/:id', ({ params }) => {
    const user = users[params.id as string];
    return user ? HttpResponse.json(user) : new HttpResponse(null, { status: 404 });
  }),
  http.post('/api/profile/:id', async ({ params, request }) => {
    const user = users[params.id as string];
    if (!user) return new HttpResponse(null, { status: 404 });
    const body = (await request.json()) as {
      phone: string;
      defaultWarehouseId: string;
      language: 'en' | 'pl';
    };
    user.phone = body.phone;
    user.defaultWarehouseId = body.defaultWarehouseId;
    user.language = body.language;
    return HttpResponse.json(user);
  }),
  http.get('/api/worklist', ({ request }) => HttpResponse.json(buildWorklist(wh(request)))),
  http.get('/api/movements', ({ request }) =>
    HttpResponse.json(movements.filter((m) => whOf(m.id) === wh(request))),
  ),
  http.get('/api/locations', () => HttpResponse.json(allLocations())),
  http.get('/api/search', ({ request }) => {
    const q = new URL(request.url).searchParams.get('q') ?? '';
    return HttpResponse.json(q.trim() ? searchAll(q.trim(), wh(request)) : []);
  }),
  http.get('/api/stock/kpis', ({ request }) => HttpResponse.json(kpisFor(wh(request)))),
  http.get('/api/stock/rows', ({ request }) =>
    HttpResponse.json(rows.filter((r) => whOf(r.id) === wh(request))),
  ),
  http.get('/api/stock/item/:id', ({ params }) => {
    const r = rows.find((x) => x.id === params.id);
    return r ? HttpResponse.json(stockItemDetail(r)) : new HttpResponse(null, { status: 404 });
  }),
  http.get('/api/stock/by-sku/:sku', ({ params }) =>
    HttpResponse.json(
      rows
        .filter((r) => r.sku === params.sku)
        .map((r) => ({
          location: r.location,
          room: r.room,
          onHand: r.onHand,
          atp: r.atp,
          status: r.status,
          statusLabel: r.statusLabel,
        })),
    ),
  ),
  http.post('/api/stock/item/:id/move', async ({ params, request }) => {
    const { toLocation } = (await request.json()) as { toLocation: string };
    const r = rows.find((x) => x.id === params.id);
    if (!r) return new HttpResponse(null, { status: 404 });
    const loc = allLocations().find((l) => l.address === toLocation);
    r.location = toLocation;
    if (loc) r.room = loc.room;
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/stock/item/:id/block', ({ params }) => {
    const r = rows.find((x) => x.id === params.id);
    if (!r) return new HttpResponse(null, { status: 404 });
    r.status = 'blocked';
    r.statusLabel = 'Blocked · QC';
    return new HttpResponse(null, { status: 204 });
  }),
  // --- Inbound deliveries (Logistics service: logistics/deliveries/...) ---
  http.get('/api/logistics/deliveries', ({ request }) =>
    HttpResponse.json(
      asnList.filter((a) => whOf(a.id) === wh(request)).map(deliverySummaryDto),
    ),
  ),
  http.get('/api/logistics/deliveries/:id', ({ params }) => {
    const detail = asnDetails[params.id as string];
    return detail ? HttpResponse.json(deliveryDto(detail)) : new HttpResponse(null, { status: 404 });
  }),
  http.post('/api/logistics/deliveries', async ({ request }) => {
    const body = (await request.json()) as {
      supplierRoleId: string;
      warehouseCode: string;
      plannedAt: string;
      lines: { productCode: string; quantity: number; unit: string }[];
    };
    const id = `ASN-${++asnCounter}`;
    asnPlannedAt[id] = body.plannedAt;
    asnList.unshift({
      id,
      supplier: body.supplierRoleId,
      status: 'reserved',
      statusLabel: 'Announced',
      meta: `${new Date(body.plannedAt).toLocaleString()} · ${body.lines.length} lines`,
    });
    asnDetails[id] = {
      id,
      supplier: body.supplierRoleId,
      warehouse: body.warehouseCode,
      dockSlot: 'slot pending',
      createdBy: 'Validated against catalog',
      status: 'reserved',
      statusLabel: 'Announced',
      lines: body.lines.map((l, i) => ({
        id: String(i + 1),
        sku: l.productCode,
        product: l.productCode,
        planned: l.quantity,
        unit: l.unit,
        tracking: 'Batch + BBE',
      })),
    };
    return HttpResponse.json({ id }, { status: 201 });
  }),
  http.post('/api/logistics/deliveries/:id/dock-slot', async ({ params, request }) => {
    const { dockCode, from, to } = (await request.json()) as {
      dockCode: string;
      from: string;
      to: string;
    };
    const d = asnDetails[params.id as string];
    if (!d) return new HttpResponse(null, { status: 404 });
    asnSlot[params.id as string] = { dockCode, from, to };
    d.dockSlot = `${dockCode} · assigned`;
    const s = asnList.find((a) => a.id === params.id);
    if (s) s.meta = `${dockCode} · ${d.lines.length} lines`;
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/logistics/deliveries/:id/arrival', ({ params }) => {
    const d = asnDetails[params.id as string];
    if (!d) return new HttpResponse(null, { status: 404 });
    d.status = 'transit';
    d.statusLabel = 'Arrived · receiving';
    const s = asnList.find((a) => a.id === params.id);
    if (s) {
      s.status = 'transit';
      s.statusLabel = 'Arrived';
    }
    return new HttpResponse(null, { status: 204 });
  }),
  // Inert resolve stub — the backend rejects unknown SKUs at announce time, so lines are never flagged.
  http.post('/api/logistics/deliveries/:id/lines/:lineId/resolve', () => new HttpResponse(null, { status: 204 })),
  // --- Outbound orders (Logistics service: logistics/orders/...) ---
  http.get('/api/logistics/orders', ({ request }) =>
    HttpResponse.json(orderList.filter((o) => whOf(o.id) === wh(request)).map(orderSummaryDto)),
  ),
  http.get('/api/logistics/orders/:id', ({ params }) => {
    const detail = orderDetails[params.id as string];
    return detail ? HttpResponse.json(orderDto(detail)) : new HttpResponse(null, { status: 404 });
  }),
  http.post('/api/logistics/orders', async ({ request }) => {
    const body = (await request.json()) as {
      customerRoleId: string;
      shipTo: { street: string; city: string };
      warehouseCode: string;
      requiredAt: string;
      lines: { productCode: string; quantity: number; unit: string }[];
    };
    const id = `SO-${++orderCounter}`;
    orderList.unshift({
      id,
      customer: body.customerRoleId,
      status: 'reserved',
      statusLabel: 'Created',
      meta: `Required ${body.requiredAt.slice(0, 10)} · ${body.lines.length} lines`,
    });
    orderDetails[id] = {
      id,
      customer: body.customerRoleId,
      status: 'reserved',
      statusLabel: 'Created',
      subtitle: 'Created · not yet reserved against ATP',
      linesReserved: `0 / ${body.lines.length}`,
      reservedUnits: 0,
      shipTo: `${body.shipTo.city}, ${body.shipTo.street}`,
      lines: body.lines.map((l, i) => ({
        id: String(i + 1),
        sku: l.productCode,
        product: l.productCode,
        ordered: l.quantity,
        atpAtOrder: 0,
        reserved: 0,
        status: 'reserved',
        statusLabel: 'Created',
      })),
    };
    return HttpResponse.json({ id }, { status: 201 });
  }),
  http.post('/api/logistics/orders/:id/decision', async ({ params, request }) => {
    const { decision } = (await request.json()) as { decision: 'split' | 'hold' };
    const d = orderDetails[params.id as string];
    if (!d) return new HttpResponse(null, { status: 404 });
    if (decision === 'split') {
      d.status = 'reserved';
      d.statusLabel = 'Reserved';
      d.subtitle = 'Split — available portion reserved, remainder backordered';
    } else {
      d.status = 'transit';
      d.statusLabel = 'Waiting';
      d.subtitle = 'Held — waiting for stock to cover the order';
    }
    const s = orderList.find((o) => o.id === params.id);
    if (s) {
      s.status = d.status;
      s.statusLabel = d.statusLabel;
    }
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/logistics/orders/:id/picking', ({ params }) => {
    const d = orderDetails[params.id as string];
    if (!d) return new HttpResponse(null, { status: 404 });
    d.status = 'available';
    d.statusLabel = 'Picking';
    d.subtitle = 'Released to wave — FEFO allocation, picking in progress';
    const s = orderList.find((o) => o.id === params.id);
    if (s) {
      s.status = 'available';
      s.statusLabel = 'Picking';
    }
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/logistics/orders/:id/cancel', ({ params }) => {
    const d = orderDetails[params.id as string];
    if (!d) return new HttpResponse(null, { status: 404 });
    d.status = 'blocked';
    d.statusLabel = 'Cancelled';
    d.subtitle = 'Cancelled — reservations released back to ATP';
    const s = orderList.find((o) => o.id === params.id);
    if (s) {
      s.status = 'blocked';
      s.statusLabel = 'Cancelled';
    }
    return new HttpResponse(null, { status: 204 });
  }),
  http.get('/api/dispatch/board', ({ request }) =>
    HttpResponse.json(
      dispatchBoard.map((c) => ({
        ...c,
        shipments: c.shipments.filter((s) => whOf(s.id) === wh(request)),
      })),
    ),
  ),
  http.post('/api/dispatch/:id/assign', async ({ params, request }) => {
    const body = (await request.json()) as { carrierCode: string; pickup: string };
    const awaiting = dispatchBoard.find((c) => c.key === 'awaitingCarrier');
    const assigned = dispatchBoard.find((c) => c.key === 'assigned');
    if (!awaiting || !assigned) return new HttpResponse(null, { status: 404 });
    const idx = awaiting.shipments.findIndex((s) => s.id === params.id);
    if (idx === -1) return new HttpResponse(null, { status: 404 });
    const [ship] = awaiting.shipments.splice(idx, 1);
    assigned.shipments.push({
      ...ship,
      canAssign: false,
      badge: undefined,
      carrier: { code: body.carrierCode, name: carrierNames[body.carrierCode] ?? body.carrierCode },
      pickup: body.pickup,
    });
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/dispatch/:id/advance', ({ params }) => {
    const order = ['awaitingCarrier', 'assigned', 'noticeSent', 'dispatched'];
    const fromCol = dispatchBoard.find((c) => c.shipments.some((s) => s.id === params.id));
    if (!fromCol) return new HttpResponse(null, { status: 404 });
    const nextKey = order[order.indexOf(fromCol.key) + 1];
    const nextCol = nextKey && dispatchBoard.find((c) => c.key === nextKey);
    if (!nextCol) return new HttpResponse(null, { status: 204 });
    const idx = fromCol.shipments.findIndex((s) => s.id === params.id);
    const [ship] = fromCol.shipments.splice(idx, 1);
    if (nextKey === 'noticeSent') {
      ship.pickup = undefined;
      ship.badge = { variant: 'transit', label: 'Awaiting collection' };
    } else if (nextKey === 'dispatched') {
      ship.badge = { variant: 'available', label: 'Collected ✓' };
      ship.tracking = `Tracking ${ship.carrier?.name ?? 'CARRIER'}-PL-${10000 + Math.floor(Math.random() * 89999)} · waybill issued`;
    }
    nextCol.shipments.push(ship);
    return new HttpResponse(null, { status: 204 });
  }),

  http.get('/api/adjustments/draft', () => HttpResponse.json(adjustmentDraft)),
  http.get('/api/adjustments/draft/:id', ({ params }) => {
    const r = rows.find((x) => x.id === params.id);
    if (!r) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json({
      itemId: r.id,
      product: r.product,
      batch: r.batch,
      sku: r.sku,
      location: r.location,
      room: r.room,
      status: r.status,
      statusLabel: r.statusLabel,
      systemOnHand: r.onHand,
      unit: r.unit,
    });
  }),
  http.post('/api/adjustments', async ({ request }) => {
    const body = (await request.json()) as { newQuantity: number; reason: string };
    return HttpResponse.json({
      itemId: adjustmentDraft.itemId,
      newOnHand: body.newQuantity,
      delta: body.newQuantity - adjustmentDraft.systemOnHand,
      reason: body.reason,
      postedBy: 'K. Manager',
      postedAt: new Date().toISOString(),
    });
  }),

  http.get('/api/stocktake', ({ request }) =>
    HttpResponse.json(
      Object.values(stocktakesById)
        .filter((s) => whOf(s.summary.id) === wh(request))
        .map(stocktakeListItem),
    ),
  ),
  http.get('/api/stocktake/:id', ({ params }) => {
    const s = stocktakesById[params.id as string];
    return s ? HttpResponse.json(s) : new HttpResponse(null, { status: 404 });
  }),
  http.post('/api/stocktake', async ({ request }) => {
    const { scope } = (await request.json()) as { scope: string };
    const id = `ST-${++stocktakeCounter}`;
    stocktakesById[id] = {
      summary: {
        id,
        title: `Stocktake ${id} — ${scope}`,
        sub: 'Blind count started · counting in progress',
        state: 'counting',
        locationsCounted: 0,
        totalLocations: 24,
        matches: 0,
        discrepancies: 0,
        netVariance: 0,
      },
      diffs: [],
    };
    return HttpResponse.json({ id });
  }),
  http.post('/api/stocktake/:id/approve', () => HttpResponse.json({ posted: true })),
  http.post('/api/stocktake/:id/recount', () => new HttpResponse(null, { status: 204 })),

  http.get('/api/qc/batches', ({ request }) =>
    HttpResponse.json(qcBatches.filter((b) => whOf(b.id) === wh(request))),
  ),
  http.post('/api/qc/:id/:decision', ({ params }) => {
    qcBatches = qcBatches.filter((b) => b.id !== params.id);
    return new HttpResponse(null, { status: 204 });
  }),

  http.get('/api/products', () => HttpResponse.json(Object.values(productsBySku).map(toSummary))),
  http.get('/api/products/:sku', ({ params }) => {
    const product = productsBySku[params.sku as string];
    return product ? HttpResponse.json(product) : new HttpResponse(null, { status: 404 });
  }),
  http.post('/api/products', async ({ request }) => {
    const body = (await request.json()) as ProductForm;
    productsBySku[body.sku] = { ...body, lastEdited: new Date().toISOString().slice(0, 10) };
    return new HttpResponse(null, { status: 204 });
  }),

  http.get('/api/topology/tree', () => HttpResponse.json(topologyTree)),
  http.get('/api/topology/room/:id', ({ params }) => {
    const room = topologyRooms[params.id as string];
    return room ? HttpResponse.json(room) : new HttpResponse(null, { status: 404 });
  }),
  http.post('/api/topology/room/:id', () => new HttpResponse(null, { status: 204 })),
  http.post('/api/topology/room/:roomId/location/:id', async ({ params, request }) => {
    const { capacity, loadLimit } = (await request.json()) as { capacity: number; loadLimit: number };
    const room = topologyRooms[params.roomId as string];
    const loc = room?.locations.find((l) => l.id === params.id);
    if (!loc) return new HttpResponse(null, { status: 404 });
    loc.capacity = capacity;
    loc.loadLimit = loadLimit;
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/topology/room/:roomId/locations', async ({ params, request }) => {
    const { address, capacity, loadLimit } = (await request.json()) as {
      address: string;
      capacity: number;
      loadLimit: number;
    };
    const room = topologyRooms[params.roomId as string];
    if (!room) return new HttpResponse(null, { status: 404 });
    room.locations.push({ id: `L-${Date.now()}`, address, capacity, loadLimit, occupied: '0%' });
    room.shownCount = room.locations.length;
    room.totalCount += 1;
    return new HttpResponse(null, { status: 204 });
  }),
  http.post('/api/topology/rooms', async ({ request }) => {
    const { name, type, tempMin, tempMax, warehouse } = (await request.json()) as {
      name: string;
      type: RoomDetail['type'];
      tempMin: number;
      tempMax: number;
      warehouse: string;
    };
    const id = `R-${++topoRoomCounter}`;
    topologyRooms[id] = { id, name, warehouse, type, tempMin, tempMax, shownCount: 0, totalCount: 0, locations: [] };
    const node: TopologyNode = { id, level: 2, label: name, kind: 'room', icon: type, tag: `${tempMin}–${tempMax} °C` };
    const whIdx = topologyTree.findIndex((n) => n.id === warehouse);
    if (whIdx >= 0) {
      // Insert as the last room of this warehouse — before the next warehouse node.
      let insertAt = topologyTree.length;
      for (let i = whIdx + 1; i < topologyTree.length; i++) {
        if (topologyTree[i].kind === 'warehouse') {
          insertAt = i;
          break;
        }
      }
      topologyTree.splice(insertAt, 0, node);
    } else {
      topologyTree.push(node);
    }
    return HttpResponse.json({ id });
  }),
];
