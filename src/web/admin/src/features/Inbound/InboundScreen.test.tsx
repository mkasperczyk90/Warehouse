import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { InboundScreen } from './InboundScreen';

// The detail can navigate to the receiving view; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('InboundScreen', () => {
  it('lists ASNs and shows the first one selected by default', async () => {
    renderWithProviders(<InboundScreen />);

    expect(await screen.findByText('ASN-2206')).toBeInTheDocument();
    expect(screen.getByText('ASN-2207')).toBeInTheDocument();
    // Detail of the default (first) ASN loads — the backend deals in product codes, not names
    // (the code shows in both the SKU and Product columns).
    expect((await screen.findAllByText('4006381333931')).length).toBeGreaterThan(0);
  });

  it('swaps the detail when another ASN is selected', async () => {
    const user = userEvent.setup();
    renderWithProviders(<InboundScreen />);
    await screen.findAllByText('4006381333931');

    await user.click(screen.getByText('ASN-2207'));

    expect((await screen.findAllByText('5601012009873')).length).toBeGreaterThan(0);
    expect(screen.queryByText('4006381333931')).not.toBeInTheDocument();
  });

  it('creates a new ASN from the dialog (needs a supplier and a line)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<InboundScreen />);
    await screen.findByText('ASN-2206');

    await user.click(screen.getByRole('button', { name: /New ASN/ }));
    await user.type(screen.getByLabelText('Supplier'), 'New Supplier Co');

    const submit = screen.getByRole('button', { name: 'Create ASN' });
    expect(submit).toBeDisabled(); // no valid line yet

    await user.type(screen.getByLabelText('SKU 1'), '5900000000001');
    await user.type(screen.getByLabelText('Qty 1'), '100');
    expect(submit).toBeEnabled();

    await user.click(submit);

    // The new ASN is created and selected — its detail header shows the supplier.
    expect(await screen.findByText(/— New Supplier Co/)).toBeInTheDocument();
  });

  it('assigns a dock slot to an ASN (UC-01 step 3)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<InboundScreen />);
    await screen.findByText('ASN-2206');

    // ASN-2208 has "slot pending".
    await user.click(screen.getByText('ASN-2208'));
    await screen.findByText('ASN-2208 — ACME Packaging');

    await user.click(screen.getByRole('button', { name: /Assign dock slot/ }));
    await user.selectOptions(screen.getByLabelText('Dock'), 'D-2');
    await user.type(screen.getByLabelText('Time window'), '11:00–12:00');
    await user.click(screen.getByRole('button', { name: 'Assign slot' }));

    // The dock field reflects the assigned slot.
    expect(await screen.findByText('D-2 · 11:00–12:00')).toBeInTheDocument();
  });

  it('marks an announced ASN as arrived', async () => {
    const user = userEvent.setup();
    renderWithProviders(<InboundScreen />);
    await screen.findByText('ASN-2206');

    await user.click(screen.getByText('ASN-2207')); // Announced
    await screen.findByText('ASN-2207 — Nordic Frozen AS');

    await user.click(screen.getByRole('button', { name: 'Mark arrived' }));

    // Once arrived, the "Mark arrived" affordance is gone.
    await waitFor(() =>
      expect(screen.queryByRole('button', { name: 'Mark arrived' })).not.toBeInTheDocument(),
    );
  });
});
