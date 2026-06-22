import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { DispatchScreen } from './DispatchScreen';

describe('DispatchScreen', () => {
  it('renders the four status columns of the board', async () => {
    renderWithProviders(<DispatchScreen />);

    expect(await screen.findByText('Packed — awaiting carrier')).toBeInTheDocument();
    expect(screen.getByText('Carrier assigned')).toBeInTheDocument();
    expect(screen.getByText('Pickup notice sent')).toBeInTheDocument();
    expect(screen.getByText('Dispatched')).toBeInTheDocument();
  });

  it('shows an assign-carrier affordance only on packed shipments', async () => {
    renderWithProviders(<DispatchScreen />);

    // Two packed shipments → two assign buttons.
    const assign = await screen.findAllByRole('button', { name: /Assign carrier/ });
    expect(assign).toHaveLength(2);
  });

  it('shows tracking on dispatched shipments', async () => {
    renderWithProviders(<DispatchScreen />);

    expect(
      await screen.findByText(/Tracking GLS-PL-99213 · waybill issued/),
    ).toBeInTheDocument();
  });

  it('assigns a carrier and moves the shipment off the packed queue', async () => {
    const user = userEvent.setup();
    renderWithProviders(<DispatchScreen />);
    await screen.findByText('Packed — awaiting carrier');

    // Two packed shipments → two assign buttons; open the first.
    expect(screen.getAllByRole('button', { name: /Assign carrier/ })).toHaveLength(2);
    await user.click(screen.getAllByRole('button', { name: /Assign carrier/ })[0]);

    const submit = screen.getByRole('button', { name: 'Assign' });
    expect(submit).toBeDisabled(); // no carrier chosen
    await user.selectOptions(screen.getByLabelText('Carrier'), 'DH');
    await user.click(submit);

    // The shipment leaves the packed queue → one assign button remains.
    await waitFor(() =>
      expect(screen.getAllByRole('button', { name: /Assign carrier/ })).toHaveLength(1),
    );
  });

  it('advances an assigned shipment to the next column', async () => {
    const user = userEvent.setup();
    renderWithProviders(<DispatchScreen />);
    await screen.findByText('Carrier assigned');

    const before = screen.getAllByRole('button', { name: /Send pickup notice/ }).length;
    expect(before).toBeGreaterThan(0);

    await user.click(screen.getAllByRole('button', { name: /Send pickup notice/ })[0]);

    await waitFor(() =>
      expect(screen.getAllByRole('button', { name: /Send pickup notice/ }).length).toBe(before - 1),
    );
  });

  it('offers a print-waybill action on dispatched shipments', async () => {
    renderWithProviders(<DispatchScreen />);
    await screen.findByText('Dispatched');

    expect((await screen.findAllByRole('button', { name: /Print waybill/ })).length).toBeGreaterThan(0);
  });

  it('filters the board by carrier', async () => {
    const user = userEvent.setup();
    renderWithProviders(<DispatchScreen />);
    await screen.findByText('SHP-3302');

    await user.click(screen.getByRole('button', { name: 'GLS' }));

    expect(screen.getByText('SHP-3302')).toBeInTheDocument(); // GLS
    expect(screen.queryByText('SHP-3301')).not.toBeInTheDocument(); // DHL — hidden
  });
});
