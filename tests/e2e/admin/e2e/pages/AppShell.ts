import { expect, type Locator, type Page } from '@playwright/test';

/**
 * The desk-app frame (sidebar + top bar). Owns navigation between sections, the
 * warehouse switcher and the user (profile) menu — which now also holds the
 * PL/EN language toggle and Sign out. Nav items are real `<a>` links and the
 * menu triggers `<button>`s, so locators lean on roles + visible label — no testids.
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

  // --- User (profile) menu ------------------------------------------------
  /** The TopBar trigger that opens the user menu (shows the signed-in name). */
  private userMenuTrigger(): Locator {
    return this.page.getByRole('button', { name: /K\. Manager|A\. Coordinator|M\. Inspector/ });
  }

  /** Open the user menu if it isn't already open (the Sign out item marks it open). */
  async openUserMenu() {
    const signOut = this.page.getByRole('menuitem', { name: /Sign out|Wyloguj/ });
    if (!(await signOut.isVisible().catch(() => false))) {
      await this.userMenuTrigger().click();
    }
  }

  /** The PL/EN toggle now lives inside the user menu. */
  async switchLanguage() {
    await this.openUserMenu();
    await this.page.getByRole('menuitem', { name: /Language|Język/ }).click();
  }

  async openProfileFromMenu() {
    await this.openUserMenu();
    await this.page.getByRole('menuitem', { name: /My profile|Mój profil/ }).click();
  }

  async signOut() {
    await this.openUserMenu();
    await this.page.getByRole('menuitem', { name: /Sign out|Wyloguj/ }).click();
  }

  // --- Warehouse switcher -------------------------------------------------
  /** The TopBar trigger that opens the warehouse list (shows the active site label). */
  private warehouseTrigger(): Locator {
    return this.page.getByRole('button', { name: /Wrocław|Poznań/ });
  }

  async switchWarehouse(label: string) {
    await this.warehouseTrigger().click();
    await this.page.getByRole('option', { name: label }).click();
  }

  async expectWarehouse(label: string) {
    await expect(this.warehouseTrigger()).toHaveText(new RegExp(label));
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }
}
