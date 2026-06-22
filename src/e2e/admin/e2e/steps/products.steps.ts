import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the product catalogue', async ({ products }) => {
  await products.openCatalog();
});

Given('the manager opens the product {string}', async ({ products }, sku: string) => {
  await products.openEdit(sku);
});

Then('the product catalogue is shown', async ({ products }) => {
  await products.expectCatalogShown();
});

Then('the product {string} is shown', async ({ products }, text: string) => {
  await products.expectRowShown(text);
});

Then('the product {string} is not shown', async ({ products }, text: string) => {
  await products.expectRowHidden(text);
});

When('the manager searches products for {string}', async ({ products }, term: string) => {
  await products.search(term);
});

When('the manager filters products by category {string}', async ({ products }, label: string) => {
  await products.filterByCategory(label);
});

// --- Edit / create ---------------------------------------------------------
Then('the product name field shows {string}', async ({ products }, value: string) => {
  await products.expectName(value);
});

When('the manager saves the product', async ({ products }) => {
  await products.save();
});

Then('the product is saved', async ({ products }) => {
  await products.expectSaved();
});

Then('the product is not saved', async ({ products }) => {
  await products.expectNotSaved();
});

When('the manager sets the minimum temperature to {string}', async ({ products }, value: string) => {
  await products.setTempMin(value);
});

Then('the temperature-range error is shown', async ({ products }) => {
  await products.expectTempError();
});

When('the manager starts a new product', async ({ products }) => {
  await products.openNew();
});

Then('the new-product form is shown', async ({ products }) => {
  await products.expectCreateShown();
});

When('the manager creates the product', async ({ products }) => {
  await products.createProduct();
});

Then('the SKU-length error is shown', async ({ products }) => {
  await products.expectSkuError();
});
