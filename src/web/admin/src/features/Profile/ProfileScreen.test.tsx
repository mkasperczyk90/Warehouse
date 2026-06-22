import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { AuthProvider } from '@/shared/auth/AuthContext';
import type { CurrentUser } from '@/features/Auth';
import { ProfileScreen } from './ProfileScreen';

const SEED: CurrentUser = {
  id: 'U-1',
  badge: '1001',
  name: 'K. Manager',
  role: 'manager',
  email: 'k.manager@warehouse.example',
  defaultWarehouseId: 'WH-01',
  language: 'en',
};

beforeEach(() => localStorage.setItem('wh.currentUser', JSON.stringify(SEED)));
afterEach(() => localStorage.clear());

describe('ProfileScreen', () => {
  it('renders the signed-in user from the mocked Gateway', async () => {
    renderWithProviders(
      <AuthProvider>
        <ProfileScreen />
      </AuthProvider>,
    );

    expect(await screen.findByText('k.manager@warehouse.example')).toBeInTheDocument();
    // Role is shown by its translated label, not the raw key.
    expect(screen.getAllByText('Warehouse manager').length).toBeGreaterThan(0);
  });

  it('saves an edited preference', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <AuthProvider>
        <ProfileScreen />
      </AuthProvider>,
    );
    await screen.findByText('k.manager@warehouse.example');

    const phone = screen.getByLabelText('Phone');
    await user.clear(phone);
    await user.type(phone, '+48 600 999 999');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(screen.getByText('Saved ✓')).toBeInTheDocument());
  });
});
