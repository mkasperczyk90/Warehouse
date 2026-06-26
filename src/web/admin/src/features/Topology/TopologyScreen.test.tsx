import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { TopologyScreen } from './TopologyScreen';

// Selection (the chosen room) mirrors to the URL; stub the router hooks (no router
// in component tests). `useSearch` seeds an empty selection so the first room stays default.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
  useSearch: () => ({}),
}));

describe('TopologyScreen', () => {
  it('renders the tree and the first room detail by default', async () => {
    renderWithProviders(<TopologyScreen />);

    expect(await screen.findByText('Cold room 1 — WH-01')).toBeInTheDocument();
    expect(screen.getByText('WH-01 Wrocław')).toBeInTheDocument();
    // Cold room's locations are shown in the detail table.
    expect(screen.getByText('85%')).toBeInTheDocument();
  });

  it('switches the room detail when a tree node is selected', async () => {
    const user = userEvent.setup();
    renderWithProviders(<TopologyScreen />);
    await screen.findByText('Cold room 1 — WH-01');

    await user.click(screen.getByText('Freezer 1'));

    expect(await screen.findByText('Freezer 1 — WH-01')).toBeInTheDocument();
    expect(screen.getByText('FZ1-B02-R4-S1')).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('Cold room 1 — WH-01')).not.toBeInTheDocument());
  });

  it('adds a location to the room', async () => {
    const user = userEvent.setup();
    renderWithProviders(<TopologyScreen />);
    await screen.findByText('Cold room 1 — WH-01');

    await user.click(screen.getByRole('button', { name: 'Add location' }));
    await user.type(screen.getByLabelText('Address'), 'CR1-A05-R1-S1');
    await user.click(screen.getByRole('button', { name: 'Create location' }));

    expect(await screen.findByText('CR1-A05-R1-S1')).toBeInTheDocument();
  });

  it('edits a location capacity', async () => {
    const user = userEvent.setup();
    renderWithProviders(<TopologyScreen />);
    await screen.findByText('Cold room 1 — WH-01');

    await user.click(screen.getAllByRole('button', { name: 'Edit' })[0]);
    const cap = screen.getByLabelText('Capacity (m³)');
    await user.clear(cap);
    await user.type(cap, '2.5');
    await user.click(screen.getByRole('button', { name: 'Save location' }));

    await waitFor(() => expect(screen.getByText('2.5')).toBeInTheDocument());
  });

  it('adds a room to the topology tree', async () => {
    const user = userEvent.setup();
    renderWithProviders(<TopologyScreen />);
    await screen.findByText('Cold room 1 — WH-01');

    await user.click(screen.getByRole('button', { name: 'Add room' }));
    // Rooms are identified by a code; the detail name is derived from the type (default: cold room).
    await user.type(screen.getByLabelText('Room code'), 'CR2');
    await user.click(screen.getByRole('button', { name: 'Create room' }));

    // The new room is created and selected → its detail header shows the derived label.
    expect(await screen.findByText('Cold room CR2 — WH-01')).toBeInTheDocument();
  });

  it('saves a room', async () => {
    const user = userEvent.setup();
    renderWithProviders(<TopologyScreen />);
    await screen.findByText('Cold room 1 — WH-01');

    await user.click(screen.getByRole('button', { name: 'Save room' }));

    expect(await screen.findByText('Room saved ✓')).toBeInTheDocument();
  });
});
