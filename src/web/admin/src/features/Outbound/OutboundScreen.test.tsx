import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { OutboundScreen } from './OutboundScreen';

describe('OutboundScreen', () => {
  it('lists orders and shows the first one (with its lines) by default', async () => {
    renderWithProviders(<OutboundScreen />);

    expect(await screen.findByText('SO-4471')).toBeInTheDocument();
    expect(await screen.findByText('Greek yoghurt 400 g')).toBeInTheDocument();
    // The partial/waiting rule note is always surfaced.
    expect(screen.getByText(/partial \/ waiting order/)).toBeInTheDocument();
  });

  it('swaps to a partially-reserved order on selection', async () => {
    const user = userEvent.setup();
    renderWithProviders(<OutboundScreen />);
    await screen.findByText('Greek yoghurt 400 g');

    await user.click(screen.getByText('SO-4472'));

    // SO-4472's short line is marked Partial.
    expect(await screen.findByText('Partial')).toBeInTheDocument();
  });

  it('creates a new order from the dialog (needs a customer and a line)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<OutboundScreen />);
    await screen.findByText('SO-4471');

    await user.click(screen.getByRole('button', { name: /New order/ }));
    await user.type(screen.getByLabelText('Customer'), 'New Bistro');

    const submit = screen.getByRole('button', { name: 'Create order' });
    expect(submit).toBeDisabled();

    await user.type(screen.getByLabelText('SKU 1'), '5900000000002');
    await user.type(screen.getByLabelText('Qty 1'), '20');
    expect(submit).toBeEnabled();

    await user.click(submit);

    expect(await screen.findByText(/— New Bistro/)).toBeInTheDocument();
  });

  it('splits a partial order (UC-09 coordinator decision)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<OutboundScreen />);
    await screen.findByText('SO-4471');

    await user.click(screen.getByText('SO-4472')); // Partially reserved
    await screen.findByText('SO-4472 — Bistro 24');

    await user.click(screen.getByRole('button', { name: 'Split' }));

    // The order resolves: subtitle reflects the split decision.
    expect(await screen.findByText(/available portion reserved/)).toBeInTheDocument();
  });

  it('releases a reserved order to a picking wave', async () => {
    const user = userEvent.setup();
    renderWithProviders(<OutboundScreen />);
    await screen.findByText('SO-4471');

    await user.click(screen.getByText('SO-4471')); // Reserved
    await screen.findByText('SO-4471 — Fresh Market sp. z o.o.');

    await user.click(screen.getByRole('button', { name: 'Release to wave' }));

    expect(await screen.findByText(/Released to wave/)).toBeInTheDocument();
  });

  it('cancels an order, releasing reservations', async () => {
    const user = userEvent.setup();
    renderWithProviders(<OutboundScreen />);
    await screen.findByText('SO-4471');

    await user.click(screen.getByText('SO-4469')); // Created
    await screen.findByText('SO-4469 — Fresh Market sp. z o.o.');

    await user.click(screen.getByRole('button', { name: 'Cancel order' }));

    expect(await screen.findByText(/reservations released back to ATP/)).toBeInTheDocument();
  });

  it('drills into a line ATP by location', async () => {
    const user = userEvent.setup();
    renderWithProviders(<OutboundScreen />);
    await screen.findByText('SO-4471');

    await user.click(screen.getByText('SO-4471'));
    await screen.findByText('Greek yoghurt 400 g');

    // Click the line → ATP breakdown by location for that SKU.
    await user.click(screen.getByText('Greek yoghurt 400 g'));

    expect(await screen.findByText('A2-A07-R3-S2')).toBeInTheDocument();
  });
});
