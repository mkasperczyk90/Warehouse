import { describe, expect, it, vi } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { type ColumnDef } from '@tanstack/react-table';

import { renderWithProviders } from '@/test/render';
import { DataTable } from './DataTable';

interface Row {
  name: string;
  qty: number;
}

const data: Row[] = Array.from({ length: 7 }, (_, i) => ({
  name: `Item ${i + 1}`,
  qty: (i * 3) % 10, // [0,3,6,9,2,5,8]
}));

const columns: ColumnDef<Row, unknown>[] = [
  { header: () => 'Name', accessorKey: 'name' },
  { header: () => 'Qty', accessorKey: 'qty', meta: { align: 'right' } },
];

describe('DataTable', () => {
  it('paginates with a row count and page controls', async () => {
    const user = userEvent.setup();
    renderWithProviders(<DataTable columns={columns} data={data} pageSize={5} />);

    expect(screen.getByText(/of 7/)).toBeInTheDocument();
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.queryByText('Item 6')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next' }));

    expect(screen.getByText('Item 6')).toBeInTheDocument();
    expect(screen.queryByText('Item 1')).not.toBeInTheDocument();
  });

  it('sorts by a column when its header is clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders(<DataTable columns={columns} data={data} pageSize={25} />);

    // Numeric columns sort descending-first → the largest qty (9 = Item 4)
    // lands in the first body row.
    await user.click(screen.getByText('Qty'));

    const firstBodyRow = screen.getAllByRole('row')[1];
    expect(within(firstBodyRow).getByText('Item 4')).toBeInTheDocument();
  });

  it('fires onRowClick from the keyboard (Enter) for accessibility', async () => {
    const user = userEvent.setup();
    const onRowClick = vi.fn();
    renderWithProviders(<DataTable columns={columns} data={data} onRowClick={onRowClick} />);

    const row = screen.getByText('Item 1').closest('tr') as HTMLElement;
    row.focus();
    await user.keyboard('{Enter}');

    expect(onRowClick).toHaveBeenCalledWith(expect.objectContaining({ name: 'Item 1' }));
  });
});
