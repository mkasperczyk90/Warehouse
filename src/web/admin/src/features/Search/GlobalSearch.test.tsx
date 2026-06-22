import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { GlobalSearch } from './GlobalSearch';

vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('GlobalSearch', () => {
  it('returns hits across entities for a query', async () => {
    const user = userEvent.setup();
    renderWithProviders(<GlobalSearch />);

    await user.type(screen.getByRole('textbox'), 'berries');

    expect(await screen.findByText('Frozen berries 1 kg')).toBeInTheDocument();
  });

  it('finds an ASN by id', async () => {
    const user = userEvent.setup();
    renderWithProviders(<GlobalSearch />);

    await user.type(screen.getByRole('textbox'), 'ASN-2206');

    expect(await screen.findByText('ASN-2206')).toBeInTheDocument();
  });

  it('shows a no-results state', async () => {
    const user = userEvent.setup();
    renderWithProviders(<GlobalSearch />);

    await user.type(screen.getByRole('textbox'), 'zzzzz');

    expect(await screen.findByText('No results')).toBeInTheDocument();
  });
});
