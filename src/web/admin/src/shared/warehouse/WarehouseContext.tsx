import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';

import { setActiveWarehouse } from '@/core/api/client';
import { useWarehouses, type Warehouse } from '@/features/Warehouses';

const STORAGE_KEY = 'wh.activeWarehouse';

interface WarehouseContextValue {
  warehouseId: string;
  active: Warehouse | undefined;
  warehouses: Warehouse[];
  isLoading: boolean;
  setWarehouseId: (id: string) => void;
}

const WarehouseContext = createContext<WarehouseContextValue | null>(null);

/**
 * Holds the warehouse the desk is looking at. Switching it re-points the api
 * client (every request now carries the new `X-Warehouse-Id`) and invalidates
 * all queries so every screen refetches scoped to the new site.
 */
export function WarehouseProvider({
  initialWarehouseId,
  children,
}: {
  initialWarehouseId: string;
  children: ReactNode;
}) {
  const queryClient = useQueryClient();
  const { data: warehouses = [], isLoading } = useWarehouses();

  const [warehouseId, setId] = useState<string>(() => {
    let stored: string | null = null;
    try {
      stored = localStorage.getItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
    const id = stored ?? initialWarehouseId;
    setActiveWarehouse(id);
    return id;
  });

  const setWarehouseId = useCallback(
    (id: string) => {
      if (id === warehouseId) return;
      setActiveWarehouse(id);
      try {
        localStorage.setItem(STORAGE_KEY, id);
      } catch {
        /* ignore */
      }
      setId(id);
      // Every screen is scoped to the active warehouse — refetch it all.
      void queryClient.invalidateQueries();
    },
    [queryClient, warehouseId],
  );

  const value = useMemo<WarehouseContextValue>(
    () => ({
      warehouseId,
      active: warehouses.find((w) => w.id === warehouseId),
      warehouses,
      isLoading,
      setWarehouseId,
    }),
    [warehouseId, warehouses, isLoading, setWarehouseId],
  );

  return <WarehouseContext.Provider value={value}>{children}</WarehouseContext.Provider>;
}

export function useActiveWarehouse() {
  const ctx = useContext(WarehouseContext);
  if (!ctx) throw new Error('useActiveWarehouse must be used within a WarehouseProvider');
  return ctx;
}
