import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { StocktakeListScreen } from './StocktakeListScreen';

// Rows + start navigate; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('StocktakeListScreen', () => {
  it('lists stocktakes with a start-count affordance', async () => {
    renderWithProviders(<StocktakeListScreen />);

    expect(await screen.findByText('ST-118')).toBeInTheDocument();
    expect(screen.getByText('ST-117')).toBeInTheDocument();
    expect(screen.getByText('Cold room 1, aisle A')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Start count/ })).toBeInTheDocument();
  });

  it('opens the start-count dialog', async () => {
    const user = userEvent.setup();
    renderWithProviders(<StocktakeListScreen />);
    await screen.findByText('ST-118');

    await user.click(screen.getByRole('button', { name: /Start count/ }));

    expect(screen.getByText('Start a stocktake')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Start blind count' })).toBeInTheDocument();
  });
});
