import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { ProductDetailScreen } from './ProductDetailScreen';

// The detail screen navigates from the back button; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('ProductDetailScreen', () => {
  it('renders the product card from the mocked gateway', async () => {
    renderWithProviders(<ProductDetailScreen sku="MILK-1L" />);

    expect(await screen.findByRole('heading', { name: 'Whole milk 3.2% — 1 L carton' })).toBeInTheDocument();
    expect(screen.getByText('Refrigerated')).toBeInTheDocument();
  });

  it('opens the change-storage dialog', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductDetailScreen sku="MILK-1L" />);
    await screen.findByRole('heading', { name: 'Whole milk 3.2% — 1 L carton' });

    await user.click(screen.getByRole('button', { name: 'Change storage' }));

    expect(await screen.findByText('Change storage requirement')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Apply' })).toBeInTheDocument();
  });

  // Mutates the in-memory fixture — keep last so earlier tests see the original name.
  it('renames the product and reflects it after the refetch', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductDetailScreen sku="MILK-1L" />);
    await screen.findByRole('heading', { name: 'Whole milk 3.2% — 1 L carton' });

    await user.click(screen.getByRole('button', { name: 'Rename' }));
    const input = await screen.findByLabelText('New name');
    await user.clear(input);
    await user.type(input, 'Skim milk 0.5% — 1 L carton');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('heading', { name: 'Skim milk 0.5% — 1 L carton' })).toBeInTheDocument();
  });
});
