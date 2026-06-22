import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { AdjustmentScreen } from './AdjustmentScreen';

describe('AdjustmentScreen', () => {
  it('seeds the form from the draft and computes the delta', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AdjustmentScreen />);

    const qty = (await screen.findByLabelText('New counted quantity')) as HTMLInputElement;
    expect(qty.value).toBe('600');

    await user.clear(qty);
    await user.type(qty, '588');
    // Delta shows in the field and in the result preview.
    expect(screen.getAllByText('-12').length).toBeGreaterThan(0);
  });

  it('refuses a negative quantity — invariant #3 (never below zero)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AdjustmentScreen />);

    const qty = await screen.findByLabelText('New counted quantity');
    await user.clear(qty);
    await user.type(qty, '-5');
    // pick a reason so only the quantity is invalid
    await user.selectOptions(screen.getByLabelText(/Reason/), 'damage');
    await user.click(screen.getByRole('button', { name: 'Post adjustment to ledger' }));

    expect(await screen.findByText(/can never go below zero/)).toBeInTheDocument();
    expect(screen.queryByText('Posted to the ledger ✓')).not.toBeInTheDocument();
  });

  it('confirms before posting a valid adjustment to the ledger', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AdjustmentScreen />);

    const qty = await screen.findByLabelText('New counted quantity');
    await user.clear(qty);
    await user.type(qty, '588');
    await user.selectOptions(screen.getByLabelText(/Reason/), 'damage');

    // First click opens the confirm dialog — nothing posted yet.
    await user.click(screen.getByRole('button', { name: 'Post adjustment to ledger' }));
    expect(screen.getByText('Post adjustment to the ledger?')).toBeInTheDocument();
    expect(screen.queryByText('Posted to the ledger ✓')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Confirm — post to ledger' }));
    expect(await screen.findByText('Posted to the ledger ✓')).toBeInTheDocument();
  });

  it('loads the specific item when arrived at from a stock row (front door)', async () => {
    // id 2 = Greek yoghurt 400 g, system on hand 1,440.
    renderWithProviders(<AdjustmentScreen itemId="2" />);

    const qty = (await screen.findByLabelText('New counted quantity')) as HTMLInputElement;
    expect(qty.value).toBe('1440');
    expect(screen.getByText(/Greek yoghurt 400 g/)).toBeInTheDocument();
  });
});
