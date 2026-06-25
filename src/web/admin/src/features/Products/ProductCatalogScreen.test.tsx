import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { ProductCatalogScreen } from './ProductCatalogScreen';

// The catalogue navigates on row click; stub the router hook.
vi.mock('@tanstack/react-router', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@tanstack/react-router')>()),
  useNavigate: () => vi.fn(),
}));

describe('ProductCatalogScreen', () => {
  it('lists products from the catalogue with a define affordance', async () => {
    renderWithProviders(<ProductCatalogScreen />);

    expect(await screen.findByText('Whole milk 3.2% — 1 L carton')).toBeInTheDocument();
    expect(screen.getByText('Cardboard box L')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Define product/ })).toBeInTheDocument();
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

    await user.click(screen.getByRole('button', { name: 'Dry goods' }));

    expect(screen.getByText('Cardboard box L')).toBeInTheDocument();
    expect(screen.queryByText('Whole milk 3.2% — 1 L carton')).not.toBeInTheDocument();
  });

  it('opens the define dialog', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductCatalogScreen />);
    await screen.findByText('Whole milk 3.2% — 1 L carton');

    await user.click(screen.getByRole('button', { name: /Define product/ }));

    expect(await screen.findByText('Define a product')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create product' })).toBeInTheDocument();
  });

  it('imports a CSV and reports the result, then shows the new product', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductCatalogScreen />);
    await screen.findByText('Whole milk 3.2% — 1 L carton');

    const csv = [
      'sku,name,ean,category,lengthCm,widthCm,heightCm,unitWeightKg,baseUnit,storage,minCelsius,maxCelsius,isBatchTracked,hasExpiryDate',
      'TEST-IMP-1,Imported widget,,DryGoods,10,10,10,1,pcs,Ambient,,,false,false',
    ].join('\n');
    const file = new File([csv], 'products.csv', { type: 'text/csv' });

    await user.upload(screen.getByLabelText('Import CSV'), file);

    expect(await screen.findByText('Import results')).toBeInTheDocument();
    expect(screen.getByText('Imported: 1')).toBeInTheDocument();
    // The list refetches and the new product appears.
    expect(await screen.findByText('Imported widget')).toBeInTheDocument();
  });

  it('rejects a row with a non-numeric field locally and reports it', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductCatalogScreen />);
    await screen.findByText('Whole milk 3.2% — 1 L carton');

    const csv = [
      'sku,name,ean,category,lengthCm,widthCm,heightCm,unitWeightKg,baseUnit,storage,minCelsius,maxCelsius,isBatchTracked,hasExpiryDate',
      'BAD-1,Broken row,,DryGoods,not-a-number,10,10,1,pcs,Ambient,,,false,false',
    ].join('\n');
    const file = new File([csv], 'bad.csv', { type: 'text/csv' });

    await user.upload(screen.getByLabelText('Import CSV'), file);

    expect(await screen.findByText('Import results')).toBeInTheDocument();
    expect(screen.getByText(/Failed: 1/)).toBeInTheDocument();
    expect(screen.getByText('BAD-1')).toBeInTheDocument();
  });
});
