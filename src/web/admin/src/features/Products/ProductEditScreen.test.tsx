import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '@/test/render';
import { ProductEditScreen } from './ProductEditScreen';

describe('ProductEditScreen', () => {
  it('seeds the form from the product (edit mode)', async () => {
    renderWithProviders(<ProductEditScreen sku="4006381333931" />);

    const name = (await screen.findByLabelText('Name')) as HTMLInputElement;
    expect(name.value).toBe('Whole milk 3.2% — 1 L carton');
  });

  it('toggles a storage flag', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductEditScreen sku="4006381333931" />);

    const hazardous = (await screen.findByLabelText('Hazardous (ADR)')) as HTMLInputElement;
    expect(hazardous.checked).toBe(false);
    await user.click(hazardous);
    expect(hazardous.checked).toBe(true);
  });

  it('rejects an inverted temperature range on save', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductEditScreen sku="4006381333931" />);

    const min = await screen.findByLabelText('Temperature min');
    await user.clear(min);
    await user.type(min, '10'); // min 10 > max 6
    await user.click(screen.getByRole('button', { name: 'Save product' }));

    expect(await screen.findByText(/Max temperature must be ≥ min/)).toBeInTheDocument();
    expect(screen.queryByText('Product saved ✓')).not.toBeInTheDocument();
  });

  it('saves a valid product', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductEditScreen sku="4006381333931" />);
    await screen.findByLabelText('Name');

    await user.click(screen.getByRole('button', { name: 'Save product' }));

    expect(await screen.findByText('Product saved ✓')).toBeInTheDocument();
  });

  it('starts blank in create mode and requires a SKU', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductEditScreen />);

    expect(screen.getByText('New product')).toBeInTheDocument();
    const sku = screen.getByLabelText('SKU') as HTMLInputElement;
    expect(sku.value).toBe('');
    expect(sku.disabled).toBe(false);

    await user.click(screen.getByRole('button', { name: 'Create product' }));
    expect(await screen.findByText(/SKU must be at least 8 characters/)).toBeInTheDocument();
  });
});
