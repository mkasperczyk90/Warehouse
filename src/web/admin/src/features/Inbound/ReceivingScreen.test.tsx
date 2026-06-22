import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';

import { renderWithProviders } from '@/test/render';
import { ReceivingScreen } from './ReceivingScreen';

vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('ReceivingScreen', () => {
  it('shows receiving progress for an arrived ASN', async () => {
    renderWithProviders(<ReceivingScreen id="ASN-2206" />);

    expect(await screen.findByText('Receiving — ASN-2206')).toBeInTheDocument();
    expect(screen.getByText(/lines received/)).toBeInTheDocument();
    // The second line is mid-receipt.
    expect(screen.getByText('Receiving')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Back to inbound/ })).toBeInTheDocument();
  });
});
