import { expect, type Locator, type Page } from '@playwright/test';

/**
 * The desk-app frame (sidebar + top bar). Owns navigation between sections and
 * the PL/EN language toggle. The admin's nav items are real `<a>` links and the
 * toggle a `<button>`, so locators lean on roles + visible label — no testids.
 */
export class AppShell {
  constructor(private readonly page: Page) {}

  async open(path = '/') {
    await this.page.goto(path);
  }

  /** Sidebar entries are role=link; `name` matches a substring so a badge ("Stocktakes 1") still hits. */
  navLink(name: string): Locator {
    return this.page.getByRole('link', { name });
  }

  async navigateTo(name: string) {
    await this.navLink(name).click();
  }

  /** A designed-but-unbuilt nav item (e.g. Partners) renders as inert text, not a link. */
  async expectNavDisabled(name: string) {
    await expect(this.navLink(name)).toHaveCount(0);
    await expect(this.page.getByText(name, { exact: true })).toBeVisible();
  }

  /** The PL/EN toggle in the top bar shows the active language code. */
  langToggle(): Locator {
    return this.page.getByRole('button', { name: /^(EN|PL)$/ });
  }

  async switchLanguage() {
    await this.langToggle().click();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }
}
