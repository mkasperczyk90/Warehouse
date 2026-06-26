import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import '@/shared/i18n/i18n';
import { ApiError } from '@/core/api/client';
import { ToastProvider, useToast } from './ToastProvider';
import { formatApiError } from './formatApiError';

function Harness() {
  const toast = useToast();
  return (
    <div>
      <button type="button" onClick={() => toast.error('boom')}>
        raise
      </button>
      <button
        type="button"
        onClick={() => toast.apiError(new ApiError(409, 'put_away_incompatible', 'raw'))}
      >
        raise-api
      </button>
    </div>
  );
}

describe('ToastProvider', () => {
  it('shows a toast and dismisses it on click', async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <Harness />
      </ToastProvider>,
    );

    await user.click(screen.getByText('raise'));
    expect(await screen.findByText('boom')).toBeInTheDocument();

    await user.click(screen.getByLabelText('Dismiss'));
    expect(screen.queryByText('boom')).not.toBeInTheDocument();
  });

  it('renders an API error keyed by its domain code, not the raw message', async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <Harness />
      </ToastProvider>,
    );

    await user.click(screen.getByText('raise-api'));
    expect(await screen.findByText(/temperature does not match/i)).toBeInTheDocument();
    expect(screen.queryByText('raw')).not.toBeInTheDocument();
  });
});

describe('formatApiError', () => {
  it('maps a known domain code to its localized message', () => {
    expect(formatApiError(new ApiError(409, 'quantity_negative', 'x'))).toMatch(/below zero/i);
  });

  it('falls back to the API message for an unknown code', () => {
    expect(formatApiError(new ApiError(400, 'totally_unknown_code', 'fallback message'))).toBe(
      'fallback message',
    );
  });

  it('handles a plain Error', () => {
    expect(formatApiError(new Error('plain'))).toBe('plain');
  });
});
