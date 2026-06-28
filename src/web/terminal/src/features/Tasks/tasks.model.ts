import type { Href } from 'expo-router';

import { api } from '@/core/api/client';
import { ROUTES } from '@/navigation/routes';
import type { IconName } from '@/shared/ui';

export type TaskKind = 'receive' | 'putaway' | 'pick' | 'move';

export interface TaskTile {
  kind: TaskKind;
  icon: IconName;
  title: string;
  detail: string;
  count: number;
  /** Card background — task colours from the prototype hub. */
  color: string;
  route: Href;
}

/** Domain data for a task pile — what the Gateway returns (no view config). */
interface TaskData {
  kind: TaskKind;
  detail: string;
  count: number;
}

/** kind → icon / colour / route. View config; the server never sends this. */
const TASK_VIEW: Record<TaskKind, { icon: IconName; color: string; route: Href }> = {
  receive: { icon: 'receive', color: '#2f9e44', route: ROUTES.receive },
  putaway: { icon: 'putaway', color: '#1971c2', route: ROUTES.putaway },
  pick: { icon: 'pick', color: '#f08c00', route: ROUTES.pick },
  move: { icon: 'move', color: '#5f3dc4', route: ROUTES.move },
};

export const getTasks = async (): Promise<TaskTile[]> => {
  // The hub's work piles are aggregated across Inventory + Logistics by the gateway BFF (the only place
  // that may fan out across services), scoped to the operator's warehouse via the X-Warehouse-Id header.
  const data = await api.get<TaskData[]>('terminal/tasks');
  return data.map((d) => ({ ...d, title: d.kind, ...TASK_VIEW[d.kind] }));
};
