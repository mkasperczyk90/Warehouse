import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { QualityScreen } from './QualityScreen';

describe('QualityScreen', () => {
  it('lists quarantined batches awaiting a decision', async () => {
    renderWithProviders(<QualityScreen />);

    expect(await screen.findByText(/Cheese wheel 5 kg/)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Release' })).toHaveLength(4);
  });

  it('requires a reason before a decision can be confirmed (UC-03 audit)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<QualityScreen />);
    await screen.findByText(/Cheese wheel 5 kg/);

    await user.click(screen.getAllByRole('button', { name: 'Reject' })[0]);

    const confirm = screen.getByRole('button', { name: 'Confirm reject' });
    expect(confirm).toBeDisabled();

    await user.selectOptions(screen.getByLabelText('Reason'), 'damaged');
    expect(confirm).toBeEnabled();
  });

  it('removes a batch once a reason is given and the decision confirmed', async () => {
    const user = userEvent.setup();
    renderWithProviders(<QualityScreen />);
    await screen.findByText(/Cheese wheel 5 kg/);

    await user.click(screen.getAllByRole('button', { name: 'Release' })[0]);
    await user.selectOptions(screen.getByLabelText('Reason'), 'passed');
    await user.click(screen.getByRole('button', { name: 'Confirm release' }));

    await waitFor(() => expect(screen.queryByText(/Cheese wheel 5 kg/)).not.toBeInTheDocument());
    expect(screen.getAllByRole('button', { name: 'Release' })).toHaveLength(3);
  });
});
