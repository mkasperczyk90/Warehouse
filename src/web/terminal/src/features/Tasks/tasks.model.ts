import type { Href } from 'expo-router';

import { api } from '@/core/api/client';
import { ROUTES } from '@/navigation/routes';
import type { IconName } from '@/shared/ui';

export interface Operator {
  name: string;
  site: string;
  online: boolean;
}

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

export const getOperator = (): Promise<Operator> => api.get<Operator>('operator/me');

export const getTasks = async (): Promise<TaskTile[]> => {
  const data = await api.get<TaskData[]>('tasks');
  return data.map((d) => ({ ...d, title: d.kind, ...TASK_VIEW[d.kind] }));
};
