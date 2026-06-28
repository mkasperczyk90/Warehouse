import { api } from '@/core/api/client';

/*
 * UC-06 — Move / replenish, talking to the Inventory service directly: GET the replenishment worklist
 * (`inventory/moves`) and confirm a move (`inventory/moves/confirm`). The backend deals in product codes
 * (no names, which live in Catalog), so `product` falls back to the code. The terminal works one task at
 * a time. Inter-warehouse transfer (InTransit) is not modelled in the backend yet, so only the in-warehouse
 * move is offered here.
 */

const WAREHOUSE = 'WH01';

/** UC-06 — Move / replenish. */
export interface MoveTask {
  task: number;
  ofTasks: number;
  from: string;
  to: string;
  sku: string;
  batch: string;
  product: string;
  coldChip: string;
  bbeChip: string;
  qty: number;
  checks: string[];
}

interface MoveTaskDto {
  sourceItemId: string;
  sku: string;
  batchNumber: string | null;
  quantity: number;
  unit: string;
  fromLocation: string;
  toLocation: string;
  bestBefore: string | null;
  requiresColdChain: boolean;
  checks: string[];
}

/** The task currently on screen (so confirm knows the source item + target). */
let current: { sourceItemId: string; toLocation: string } | null = null;

const empty: MoveTask = {
  task: 0,
  ofTasks: 0,
  from: '—',
  to: '—',
  sku: '—',
  batch: '',
  product: '—',
  coldChip: '',
  bbeChip: '',
  qty: 0,
  checks: [],
};

export const getMoveTask = async (): Promise<MoveTask> => {
  const tasks = await api.get<MoveTaskDto[]>(`inventory/moves?warehouse=${WAREHOUSE}`);
  const task = tasks[0];
  if (!task) {
    current = null;
    return empty;
  }
  current = { sourceItemId: task.sourceItemId, toLocation: task.toLocation };
  return {
    task: 1,
    ofTasks: tasks.length,
    from: task.fromLocation,
    to: task.toLocation,
    sku: task.sku,
    batch: task.batchNumber ?? '',
    product: task.sku,
    coldChip: task.requiresColdChain ? 'Cold chain' : 'Ambient',
    bbeChip: task.bestBefore ? `BBE ${task.bestBefore}` : '',
    qty: task.quantity,
    checks: task.checks,
  };
};

/** Confirm the move within the warehouse → ledger Move movement. */
export const confirmMove = async (qty: number): Promise<void> => {
  if (!current) return;
  await api.post<void>('inventory/moves/confirm', {
    sourceItemId: current.sourceItemId,
    toLocation: current.toLocation,
    quantity: qty,
    performedBy: 'terminal',
  });
};
