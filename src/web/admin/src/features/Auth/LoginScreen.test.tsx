import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { AuthProvider, useAuth } from '@/shared/auth/AuthContext';
import { LoginScreen } from './LoginScreen';

// Show the signed-in user once auth succeeds, so we can assert the badge flow
// end-to-end (LoginScreen → AuthContext → MSW → user).
function Harness() {
  const { user } = useAuth();
  return user ? <div>signed in: {user.name}</div> : <LoginScreen />;
}

beforeEach(() => localStorage.clear());
afterEach(() => localStorage.clear());

describe('LoginScreen', () => {
  it('signs in when a known badge is scanned', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <AuthProvider>
        <Harness />
      </AuthProvider>,
    );

    await user.type(screen.getByRole('textbox'), '1001');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByText('signed in: K. Manager')).toBeInTheDocument();
  });

  it('shows an error and stays on the screen for an unknown badge', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <AuthProvider>
        <Harness />
      </AuthProvider>,
    );

    await user.type(screen.getByRole('textbox'), '9999');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Badge not recognised');
    expect(screen.queryByText(/signed in:/)).not.toBeInTheDocument();
  });
});
