import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { MovementsScreen } from './MovementsScreen';

describe('MovementsScreen', () => {
  it('lists ledger movements', async () => {
    renderWithProviders(<MovementsScreen />);

    expect(screen.getByText('Stock movements')).toBeInTheDocument();
    expect(await screen.findByText('GR-2206')).toBeInTheDocument(); // a receipt
    expect(screen.getByText('SO-4470')).toBeInTheDocument(); // a pick
  });

  it('filters by movement type', async () => {
    const user = userEvent.setup();
    renderWithProviders(<MovementsScreen />);
    await screen.findByText('GR-2206');

    await user.click(screen.getByRole('button', { name: 'Pick' }));

    expect(screen.getByText('SO-4470')).toBeInTheDocument();
    expect(screen.queryByText('GR-2206')).not.toBeInTheDocument(); // receipt hidden
  });

  it('filters by search text', async () => {
    const user = userEvent.setup();
    renderWithProviders(<MovementsScreen />);
    await screen.findByText('GR-2206');

    await user.type(screen.getByRole('textbox'), 'FZ1');

    expect(screen.getByText('PA-1176')).toBeInTheDocument(); // put-away at FZ1
    expect(screen.queryByText('GR-2206')).not.toBeInTheDocument();
  });
});
