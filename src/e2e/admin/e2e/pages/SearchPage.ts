import { expect, type Locator, type Page } from '@playwright/test';

/** The global command-bar in the top bar — search anything, jump to it. */
export class SearchPage {
  private readonly placeholder = 'Search SKU, batch, location, ASN, order…';

  constructor(private readonly page: Page) {}

  field(): Locator {
    return this.page.getByPlaceholder(this.placeholder);
  }

  /** The popover only opens once the query is ≥2 chars; focus then type. */
  async search(term: string) {
    const input = this.field();
    await input.click();
    await input.fill(term);
  }

  /**
   * Each hit is a role=button in the results popover. Scope to the search wrap
   * (the input's grandparent) so a matching row on the page behind the popover
   * — e.g. the same ASN in the Today worklist — can't shadow the result.
   */
  result(text: string): Locator {
    const escaped = text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const wrap = this.field().locator('xpath=ancestor::div[2]');
    return wrap.getByRole('button', { name: new RegExp(escaped) });
  }

  async expectResult(text: string) {
    await expect(this.result(text)).toBeVisible();
  }

  async openResult(text: string) {
    // mousedown (not click) fires before the input blur that closes the popover.
    await this.result(text).click();
  }
}
