import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';

import { renderWithProviders } from '@/test/render';
import { TodayScreen } from './TodayScreen';

// Cards and rows navigate; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('TodayScreen', () => {
  it('renders the worklist heading and the attention cards', async () => {
    renderWithProviders(<TodayScreen />);

    expect(await screen.findByText('What needs you now')).toBeInTheDocument();
    // "Quality holds" appears as both the card and the queue panel title.
    expect(screen.getAllByText('Quality holds').length).toBeGreaterThan(0);
    expect(screen.getByText('Expiring ≤ 7 d')).toBeInTheDocument();
    expect(screen.getByText('Stocktake to approve')).toBeInTheDocument();
  });

  it('renders the queue panels with their items', async () => {
    renderWithProviders(<TodayScreen />);

    expect(await screen.findByText('Orders needing a decision')).toBeInTheDocument();
    // A QC item from the live quarantine list (exact label — "Cheese wheel"
    // also appears in the expiring queue, so match the QC item's own label).
    expect(screen.getByText('LOT-0402 · Cheese wheel 5 kg')).toBeInTheDocument();
    // An inbound item.
    expect(screen.getByText('ASN-2206 · Dairy Farms Ltd')).toBeInTheDocument();
  });
});
