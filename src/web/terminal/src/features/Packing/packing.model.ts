import { api } from '@/core/api/client';
import { humanRef } from '@/shared/format/ref';

/*
 * UC-11 — Packing, composed from the Logistics service: the order released to the floor (status Picking)
 * plus its pick list — the picked lines are the package contents. Closing the package marks the order
 * packed (`logistics/orders/{id}/packed`). The backend deals in product codes (no names) and does not
 * model package weight/dimensions yet, so those are omitted; "open another package" is a terminal-side
 * affordance (one order, one shipment for the demo).
 */

interface OrderSummaryDto {
  id: string;
  status: string;
}

interface OrderDto {
  id: string;
  customerRoleId: string;
}

interface PickTaskDto {
  sequence: number;
  productCode: string;
  batchNumber: string | null;
  quantity: number;
  unit: string;
  status: string;
}

interface PickListDto {
  orderId: string;
  picked: number;
  total: number;
  tasks: PickTaskDto[];
}

export interface PackLine {
  name: string;
  lot: string;
  qty: string;
  done: boolean;
  remaining?: number;
}

/** UC-11 — Packing. */
export interface PackJob {
  order: string;
  customer: string;
  lines: PackLine[];
}

/** The order being packed (set by getPackJob, used by closePackage). */
let currentOrderId: string | null = null;

export const getPackJob = async (): Promise<PackJob> => {
  const orders = await api.get<OrderSummaryDto[]>('logistics/orders?status=Picking');
  currentOrderId = orders[0]?.id ?? null;
  if (!currentOrderId) {
    return { order: '—', customer: '—', lines: [] };
  }
  const [order, pl] = await Promise.all([
    api.get<OrderDto>(`logistics/orders/${currentOrderId}`),
    api.get<PickListDto>(`logistics/orders/${currentOrderId}/pick-list`),
  ]);
  const lines: PackLine[] = pl.tasks.map((tk) => {
    const done = tk.status === 'Picked';
    return {
      name: tk.productCode,
      lot: tk.batchNumber ?? '—',
      qty: `${tk.quantity} ${tk.unit}`,
      done,
      remaining: done ? undefined : tk.quantity,
    };
  });
  return { order: humanRef('SO', order.id), customer: order.customerRoleId, lines };
};

/** Close & label the current package — the order is marked packed (UC-11). */
export const closePackage = async (): Promise<void> => {
  if (!currentOrderId) return;
  await api.post<void>(`logistics/orders/${currentOrderId}/packed`);
};
