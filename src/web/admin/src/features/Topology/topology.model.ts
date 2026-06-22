import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';

/** UC-14 — Warehouse topology (admin-7-topology). */
export type NodeKind = 'warehouse' | 'room' | 'location';
export type RoomType = 'cold' | 'freezer' | 'standard' | 'hazmat' | 'dock';

export interface TopologyNode {
  id: string;
  level: 1 | 2 | 3;
  label: string;
  kind: NodeKind;
  /** Icon key, mapped to a lucide icon in the view. */
  icon: string;
  /** Optional environment tag, e.g. "2–6 °C". */
  tag?: string;
}

export interface LocationRow {
  id: string;
  address: string;
  capacity: number;
  loadLimit: number;
  occupied: string;
}

export interface RoomDetail {
  id: string;
  name: string;
  warehouse: string;
  type: RoomType;
  tempMin: number;
  tempMax: number;
  shownCount: number;
  totalCount: number;
  locations: LocationRow[];
}

export function useTopologyTree() {
  return useQuery({
    queryKey: ['topology', 'tree'],
    queryFn: () => api.get<TopologyNode[]>('topology/tree'),
  });
}

export function useRoom(id: string | null | undefined) {
  return useQuery({
    queryKey: ['topology', 'room', id],
    queryFn: () => api.get<RoomDetail>(`topology/room/${id}`),
    enabled: !!id,
  });
}

export function useSaveRoom(id: string | undefined) {
  return useMutation({
    mutationFn: (body: { type: RoomType; tempMin: number; tempMax: number }) =>
      api.post(`topology/room/${id}`, body),
  });
}

/** Edit a location's capacity / load limit (UC-14). */
export function useSaveLocation(roomId: string | undefined) {
  return useMutation({
    mutationFn: (body: { id: string; capacity: number; loadLimit: number }) =>
      api.post(`topology/room/${roomId}/location/${body.id}`, body),
  });
}

/** Add a location to a room (UC-14). */
export function useAddLocation(roomId: string | undefined) {
  return useMutation({
    mutationFn: (body: { address: string; capacity: number; loadLimit: number }) =>
      api.post(`topology/room/${roomId}/locations`, body),
  });
}

/** Add a room under a warehouse — a new node in the topology tree (UC-14). */
export function useAddRoom() {
  return useMutation({
    mutationFn: (body: {
      name: string;
      warehouse: string;
      type: RoomType;
      tempMin: number;
      tempMax: number;
    }) => api.post<{ id: string }>('topology/rooms', body),
  });
}
