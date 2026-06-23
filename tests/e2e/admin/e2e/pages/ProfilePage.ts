import { expect, type Locator, type Page } from '@playwright/test';

/** User profile screen (`features/Profile`, `/profile`) — identity + editable prefs. */
export class ProfilePage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/profile');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/profile(\?|$)/);
  }

  /** The phone input is wrapped by its <label> (text "Phone"). */
  phoneField(): Locator {
    return this.page.getByLabel('Phone');
  }

  async setPhone(value: string) {
    await this.phoneField().fill(value);
  }

  async save() {
    await this.page.getByRole('button', { name: /Save changes/ }).click();
  }

  async expectSaved() {
    await expect(this.page.getByText('Saved ✓')).toBeVisible();
  }
}
