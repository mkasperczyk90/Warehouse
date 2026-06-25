import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';

import { renderWithProviders } from '@/test/render';
import { useWarehouses, warehouseLabel } from './warehouse.model';

// Renders the switcher's data so the test exercises the real seam: GET topology/warehouses (MSW),
// mapped to { id: code, name: city } and labelled "code city".
function Probe() {
  const { data = [] } = useWarehouses();
  return (
    <ul>
      {data.map((w) => (
        <li key={w.id} data-id={w.id}>
          {warehouseLabel(w)}
        </li>
      ))}
    </ul>
  );
}

describe('useWarehouses', () => {
  it('lists sites from topology/warehouses, labelled "code city"', async () => {
    renderWithProviders(<Probe />);

    const first = await screen.findByText('WH-01 Wrocław');
    expect(first).toBeInTheDocument();
    expect(first).toHaveAttribute('data-id', 'WH-01'); // identity is the warehouse code (X-Warehouse-Id)
    expect(await screen.findByText('WH-02 Poznań')).toBeInTheDocument();
  });
});
