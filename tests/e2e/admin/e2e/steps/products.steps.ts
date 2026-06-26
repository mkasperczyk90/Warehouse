import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the product catalogue', async ({ products }) => {
  await products.openCatalog();
});

Given('the manager opens the product {string}', async ({ products }, sku: string) => {
  await products.openDetail(sku);
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

// --- Detail + rename -------------------------------------------------------
Then('the product detail shows {string}', async ({ products }, name: string) => {
  await products.expectDetailName(name);
});

When('the manager renames the product to {string}', async ({ products }, name: string) => {
  await products.rename(name);
});

// --- Define (create) -------------------------------------------------------
When('the manager starts a new product', async ({ products }) => {
  await products.openNew();
});

Then('the new-product form is shown', async ({ products }) => {
  await products.expectCreateShown();
});

When('the manager fills in a valid SKU and name', async ({ products }) => {
  await products.fillValidSkuAndName();
});

When(
  'the manager sets cold-chain storage with min {string} and max {string}',
  async ({ products }, min: string, max: string) => {
    await products.setColdChainRange(min, max);
  },
);

When('the manager creates the product', async ({ products }) => {
  await products.createProduct();
});

Then('the temperature-range error is shown', async ({ products }) => {
  await products.expectTempError();
});

Then('the SKU-length error is shown', async ({ products }) => {
  await products.expectSkuError();
});
