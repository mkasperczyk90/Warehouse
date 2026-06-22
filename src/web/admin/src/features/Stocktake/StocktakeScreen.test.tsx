import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { StocktakeScreen } from './StocktakeScreen';

describe('StocktakeScreen', () => {
  it('renders the summary and the differences awaiting approval', async () => {
    renderWithProviders(<StocktakeScreen id="ST-118" />);

    expect(await screen.findByText('Stocktake ST-118 — Cold room 1, aisle A')).toBeInTheDocument();
    expect(screen.getByText('Butter block 250 g · LOT-0331')).toBeInTheDocument();
    // Net variance card.
    expect(screen.getByText('-86')).toBeInTheDocument();
  });

  it('requires a reason on every selected row before approval', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StocktakeScreen id="ST-118" />);
    await screen.findByText('Butter block 250 g · LOT-0331');

    // The pre-filled rows (4) are valid → approve is enabled by default.
    const approve = screen.getByRole('button', { name: /Approve differences/ });
    expect(approve).toBeEnabled();

    // Select the 5th row (no reason) → approval blocked until a reason is set.
    await user.click(screen.getByLabelText('select CR1-A03-R2-S3'));
    expect(approve).toBeDisabled();

    await user.selectOptions(screen.getByLabelText('reason CR1-A03-R2-S3'), 'loss');
    expect(approve).toBeEnabled();
  });

  it('re-issues a blind count for the selected rows (recount)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StocktakeScreen id="ST-118" />);
    await screen.findByText('Butter block 250 g · LOT-0331');

    // The 4 pre-filled rows are selected by default → recount is enabled.
    await user.click(screen.getByRole('button', { name: 'Recount selected' }));

    expect(await screen.findByText(/Recount issued for/)).toBeInTheDocument();
  });

  it('approves the differences to the ledger', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StocktakeScreen id="ST-118" />);
    await screen.findByText('Butter block 250 g · LOT-0331');

    await user.click(screen.getByRole('button', { name: /Approve differences/ }));

    await waitFor(() =>
      expect(screen.getByRole('button', { name: /approved → ledger/ })).toBeInTheDocument(),
    );
  });
});
