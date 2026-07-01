import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Human-like interaction helpers. The point of the recording is that it looks
 * like a person using the app, so every click is preceded by a smooth cursor
 * glide to the target and typing happens character by character.
 *
 * `DEMO_PACE` (default 1) scales every pause — bump it to 1.5 for a calmer clip,
 * drop it to 0.5 for a quick one.
 */
const PACE = Number(process.env.DEMO_PACE ?? '1');

export async function beat(page: Page, ms = 500): Promise<void> {
  await page.waitForTimeout(Math.round(ms * PACE));
}

/** Set the on-screen caption banner (scene narration). */
export async function caption(page: Page, title: string, sub = ''): Promise<void> {
  await page
    .evaluate(([t, s]) => (window as Window & { __demoCaption?: (t: string, s?: string) => void }).__demoCaption?.(t, s), [title, sub] as const)
    .catch(() => {});
}

/** Glide the synthetic cursor to a locator's centre (so the overlay follows). */
export async function moveTo(page: Page, target: Locator): Promise<void> {
  await target.scrollIntoViewIfNeeded().catch(() => {});
  const box = await target.boundingBox();
  if (!box) return;
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps: 22 });
  await beat(page, 220);
}

/** Move to a target, then click it — the cursor is already there, so no jump. */
export async function humanClick(page: Page, target: Locator): Promise<void> {
  const el = target.first();
  await expect(el).toBeVisible();
  await moveTo(page, el);
  await el.click();
  await beat(page, 320);
}

/** Focus a field (with a glide) and type the value one key at a time. */
export async function humanType(page: Page, target: Locator, text: string): Promise<void> {
  const el = target.first();
  await expect(el).toBeVisible();
  await moveTo(page, el);
  await el.click();
  await el.fill('');
  await el.pressSequentially(text, { delay: Math.round(60 * PACE) });
  await beat(page, 250);
}

/** Type into a field and submit with Enter — emulates a badge / barcode scan. */
export async function scan(page: Page, target: Locator, payload: string): Promise<void> {
  const el = target.first();
  await expect(el).toBeVisible();
  await moveTo(page, el);
  await el.click();
  await el.fill('');
  await el.pressSequentially(payload, { delay: Math.round(45 * PACE) });
  await beat(page, 200);
  await el.press('Enter');
  await beat(page, 400);
}

/** Announce a scene: set the caption and hold a beat so it reads on screen. */
export async function scene(page: Page, title: string, sub = ''): Promise<void> {
  await caption(page, title, sub);
  await beat(page, 900);
}
