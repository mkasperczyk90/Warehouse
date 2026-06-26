import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { StockItemScreen } from './StockItemScreen';

vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('StockItemScreen', () => {
  it('drills into a stock item: breakdown + movement history + adjust action', async () => {
    renderWithProviders(<StockItemScreen id="5" />);

    // Cheese wheel (id 5) — the QC-blocked item.
    expect(await screen.findByText(/Cheese wheel 5 kg/)).toBeInTheDocument();
    expect(screen.getByText('Blocked · QC')).toBeInTheDocument();
    // Movement history loads.
    expect(screen.getByText('Movement history')).toBeInTheDocument();
    expect(screen.getByText('Goods receipt')).toBeInTheDocument();
    // The front door to Adjustment.
    expect(screen.getByRole('button', { name: /Adjust stock/ })).toBeInTheDocument();
  });

  it('refuses a move to an incompatible room (environment invariant)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StockItemScreen id="1" />); // Whole milk · Cold room

    await screen.findByText(/Whole milk 3.2% 1 L/);
    await user.click(screen.getByRole('button', { name: 'Move' }));

    const confirm = screen.getByRole('button', { name: 'Confirm move' });
    expect(confirm).toBeDisabled();

    // A standard (ambient) location → incompatible with a cold item.
    await user.selectOptions(screen.getByLabelText('Target location'), 'A2-A07-R3-S2');
    expect(screen.getByText(/Incompatible/)).toBeInTheDocument();
    expect(confirm).toBeDisabled();

    // A cold-room location → compatible.
    await user.selectOptions(screen.getByLabelText('Target location'), 'CR1-A01-R1-S4');
    expect(confirm).toBeEnabled();
  });

  it('blocks a stock item with a required reason', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StockItemScreen id="2" />); // Greek yoghurt

    await screen.findByText(/Greek yoghurt 400 g/);
    await user.click(screen.getByRole('button', { name: 'Block' }));

    const confirm = screen.getByRole('button', {
      name: 'Block → quarantine',
    });
    expect(confirm).toBeDisabled();

    await user.selectOptions(screen.getByLabelText('Reason'), 'damage');
    await user.click(confirm);

    expect(await screen.findByText(/sent to quarantine/)).toBeInTheDocument();
  });
});
