import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';

import { renderWithProviders } from '@/test/render';
import { setActiveWarehouse } from '@/core/api/client';
import { StockScreen } from '@/features/Stock';
import { WarehouseProvider } from './WarehouseContext';

// StockScreen rows navigate on click and its filters read the URL; stub the
// router hooks (no router here). `useSearch` seeds empty filters.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
  useSearch: () => ({}),
}));

beforeEach(() => localStorage.clear());
afterEach(() => {
  localStorage.clear();
  setActiveWarehouse(null); // don't leak the header into other tests
});

describe('warehouse scoping', () => {
  it('shows only the active warehouse’s stock (WH-01)', async () => {
    renderWithProviders(
      <WarehouseProvider initialWarehouseId="WH-01">
        <StockScreen />
      </WarehouseProvider>,
    );

    expect(await screen.findByText('Cheese wheel 5 kg')).toBeInTheDocument();
    expect(screen.queryByText('Frozen peas 1 kg')).not.toBeInTheDocument();
  });

  it('scopes the data to a different warehouse (WH-02)', async () => {
    renderWithProviders(
      <WarehouseProvider initialWarehouseId="WH-02">
        <StockScreen />
      </WarehouseProvider>,
    );

    expect(await screen.findByText('Frozen peas 1 kg')).toBeInTheDocument();
    expect(screen.queryByText('Cheese wheel 5 kg')).not.toBeInTheDocument();
  });
});
