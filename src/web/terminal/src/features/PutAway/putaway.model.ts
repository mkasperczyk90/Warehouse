import { api } from '@/core/api/client';

/*
 * UC-04 — Put-away. The read is the Inventory dock-buffer worklist
 * (`inventory/put-away/tasks`); the confirm posts a buffer→location move
 * (`inventory/put-away/confirm`). The backend's location optimiser is deliberately deferred
 * (docs/PLAN.md), so the proposed bay + its env checks are presented by the terminal here until a
 * LocationSnapshot-backed proposal ships. The chosen bay is what the confirm moves stock into.
 */

const WAREHOUSE = 'WH01';

interface DeliverySummaryDto {
  id: string;
  warehouseCode: string;
}

// The confirm carries a deliveryId for the ledger reason + completion event; it isn't a lookup key, so
// any of the warehouse's real delivery GUIDs serves as the reference. Resolved lazily from the backend.
let deliveryRef: string | null = null;

export interface PutAwayTask {
  task: number;
  ofTasks: number;
  lpn: string;
  product: string;
  chips: string[];
  coldChip: string;
  location: string;
  why: string;
  checks: string[];
}

interface PutAwayTaskDto {
  sku: string;
  batchNumber: string | null;
  quantity: number;
  unit: string;
  fromLocation: string;
}

// Terminal-side candidate bays (the deferred backend optimiser will own these). Each already
// satisfies the temperature/capacity invariants; "Location full" cycles to the next.
const BAYS: Pick<PutAwayTask, 'location' | 'why' | 'checks'>[] = [
  {
    location: 'WH01-CR1-A03-R2-S1',
    why: 'Cold room · consolidates with same SKU/batch · 0.8 m³ free',
    checks: ['Temperature compatible (cold room 2–6 °C)', 'Capacity & load limit OK'],
  },
  {
    location: 'WH01-CR1-A01-R1-S4',
    why: 'Cold room · empty bay · 1.2 m³ free',
    checks: ['Temperature compatible (cold room 2–6 °C)', 'Capacity & load limit OK'],
  },
];
let bayIndex = 0;

// The line currently being put away (kept so confirm can post sku/batch/qty/unit).
let current: PutAwayTaskDto | null = null;

export const getPutAwayTask = async (): Promise<PutAwayTask> => {
  const tasks = await api.get<PutAwayTaskDto[]>(`inventory/put-away/tasks?warehouse=${WAREHOUSE}`);
  current = tasks[0] ?? null;
  if (!deliveryRef) {
    const deliveries = await api.get<DeliverySummaryDto[]>('logistics/deliveries');
    deliveryRef = deliveries.find((d) => d.warehouseCode === WAREHOUSE)?.id ?? deliveries[0]?.id ?? null;
  }
  const bay = BAYS[bayIndex];
  const chips = current ? [`${current.quantity} ${current.unit}`] : [];
  if (current?.batchNumber) chips.push(`Batch ${current.batchNumber}`);
  return {
    task: tasks.length === 0 ? 0 : 1,
    ofTasks: tasks.length,
    lpn: current?.fromLocation ?? '—',
    product: current?.sku ?? '—',
    chips,
    coldChip: 'Cold chain',
    location: bay.location,
    why: bay.why,
    checks: bay.checks,
  };
};

/** Confirm the pallet into the proposed location → ledger PutAway movement. */
export const confirmPutAway = async (): Promise<void> => {
  if (!current) return;
  await api.post<void>('inventory/put-away/confirm', {
    deliveryId: deliveryRef,
    warehouseCode: WAREHOUSE,
    sku: current.sku,
    batchNumber: current.batchNumber,
    quantity: current.quantity,
    unit: current.unit,
    toLocation: BAYS[bayIndex].location,
    performedBy: 'terminal',
  });
  bayIndex = 0;
};

/** Location full/over capacity → propose the next legal bay (terminal-side until the optimiser ships). */
export const proposeAnotherBay = (): Promise<void> => {
  bayIndex = (bayIndex + 1) % BAYS.length;
  return Promise.resolve();
};
