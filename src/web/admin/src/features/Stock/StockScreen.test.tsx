import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { StockScreen } from './StockScreen';

// Rows navigate to the drill-down on click; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('StockScreen', () => {
  it('renders stock rows and their status badges from the mocked Gateway', async () => {
    renderWithProviders(<StockScreen />);

    // Rows arrive async through the api seam → MSW.
    expect(await screen.findByText('Whole milk 3.2% 1 L')).toBeInTheDocument();
    expect(screen.getByText('Cheese wheel 5 kg')).toBeInTheDocument();
    // Status is never colour alone — the blocked label is present.
    expect(screen.getByText('Blocked · QC')).toBeInTheDocument();
  });

  it('shows the KPI strip', async () => {
    renderWithProviders(<StockScreen />);

    // Assert a unique KPI label (not the locale-formatted number, which varies
    // by environment) to confirm the strip rendered from the kpis query.
    expect(await screen.findByText('Available to promise')).toBeInTheDocument();
  });

  it('filters to blocked stock via the pill', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StockScreen />);
    await screen.findByText('Whole milk 3.2% 1 L');

    await user.click(screen.getByRole('button', { name: 'Blocked' }));

    expect(screen.getByText('Cheese wheel 5 kg')).toBeInTheDocument();
    expect(screen.queryByText('Whole milk 3.2% 1 L')).not.toBeInTheDocument();
  });

  it('filters by search text', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StockScreen />);
    await screen.findByText('Whole milk 3.2% 1 L');

    await user.type(screen.getByRole('textbox'), 'berries');

    expect(screen.getByText('Frozen berries 1 kg')).toBeInTheDocument();
    expect(screen.queryByText('Whole milk 3.2% 1 L')).not.toBeInTheDocument();
  });
});
