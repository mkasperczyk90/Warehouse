import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { ProductCatalogScreen } from './ProductCatalogScreen';

// The catalogue navigates on row click / "New product"; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('ProductCatalogScreen', () => {
  it('lists products from the catalogue with a create affordance', async () => {
    renderWithProviders(<ProductCatalogScreen />);

    expect(await screen.findByText('Whole milk 3.2% — 1 L carton')).toBeInTheDocument();
    expect(screen.getByText('Cardboard box L')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /New product/ })).toBeInTheDocument();
  });

  it('filters by search text', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductCatalogScreen />);
    await screen.findByText('Whole milk 3.2% — 1 L carton');

    await user.type(screen.getByRole('textbox'), 'berries');

    expect(screen.getByText('Frozen berries 1 kg')).toBeInTheDocument();
    expect(screen.queryByText('Whole milk 3.2% — 1 L carton')).not.toBeInTheDocument();
  });

  it('filters by category pill', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductCatalogScreen />);
    await screen.findByText('Whole milk 3.2% — 1 L carton');

    await user.click(screen.getByRole('button', { name: 'Packaging' }));

    expect(screen.getByText('Cardboard box L')).toBeInTheDocument();
    expect(screen.queryByText('Whole milk 3.2% — 1 L carton')).not.toBeInTheDocument();
  });
});
