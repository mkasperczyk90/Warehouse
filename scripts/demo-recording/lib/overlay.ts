import type { Page } from '@playwright/test';

/**
 * Browser-side overlay: a fake mouse cursor that follows Playwright's synthetic
 * pointer, a click ripple, and a bottom caption banner for scene narration.
 *
 * Playwright's `mouse.*` calls dispatch real DOM mouse events, so a capturing
 * `mousemove` listener can track the pointer the recording doesn't otherwise
 * show. Registered with `addInitScript`, this re-runs on every document load,
 * so it survives both SPA navigations and full reloads. It exposes
 * `window.__demoCaption(title, sub)` for the test to set the banner text.
 */
function installOverlay(): void {
  const w = window as unknown as { __demoOverlay?: boolean; __demoCaption?: (t: string, s?: string) => void };
  if (w.__demoOverlay) return;
  w.__demoOverlay = true;

  const CURSOR_ID = 'demo-cursor';
  const CAPTION_ID = 'demo-caption';

  const ensure = (): void => {
    if (!document.body) {
      requestAnimationFrame(ensure);
      return;
    }

    if (!document.getElementById(CURSOR_ID)) {
      const cursor = document.createElement('div');
      cursor.id = CURSOR_ID;
      Object.assign(cursor.style, {
        position: 'fixed',
        left: '0',
        top: '0',
        zIndex: '2147483647',
        pointerEvents: 'none',
        transform: `translate(${window.innerWidth / 2}px, ${window.innerHeight / 2}px)`,
        transition: 'transform 35ms linear',
        filter: 'drop-shadow(0 1px 2px rgba(0,0,0,.45))',
      } as CSSStyleDeclaration);
      cursor.innerHTML =
        '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">' +
        '<path d="M5 3l14 7-6 1.6L9.5 18 5 3z" fill="#fff" stroke="#111" stroke-width="1.4" stroke-linejoin="round"/></svg>';
      document.body.appendChild(cursor);
    }

    if (!document.getElementById(CAPTION_ID)) {
      const cap = document.createElement('div');
      cap.id = CAPTION_ID;
      Object.assign(cap.style, {
        position: 'fixed',
        left: '50%',
        bottom: '20px',
        transform: 'translateX(-50%)',
        maxWidth: 'min(86vw, 900px)',
        zIndex: '2147483646',
        pointerEvents: 'none',
        padding: '10px 16px',
        borderRadius: '12px',
        background: 'rgba(17,17,20,.86)',
        color: '#fff',
        font: '500 15px/1.35 system-ui, -apple-system, Segoe UI, Roboto, sans-serif',
        textAlign: 'center',
        boxShadow: '0 6px 24px rgba(0,0,0,.35)',
        opacity: '0',
        transition: 'opacity 220ms ease',
        backdropFilter: 'blur(2px)',
      } as CSSStyleDeclaration);
      document.body.appendChild(cap);
    }
  };

  ensure();

  const moveCursor = (e: MouseEvent): void => {
    const c = document.getElementById(CURSOR_ID);
    if (c) c.style.transform = `translate(${e.clientX}px, ${e.clientY}px)`;
  };

  const ripple = (e: MouseEvent): void => {
    if (!document.body) return;
    const r = document.createElement('div');
    Object.assign(r.style, {
      position: 'fixed',
      left: `${e.clientX}px`,
      top: `${e.clientY}px`,
      width: '8px',
      height: '8px',
      marginLeft: '-4px',
      marginTop: '-4px',
      borderRadius: '50%',
      border: '2px solid rgba(56,132,255,.9)',
      zIndex: '2147483646',
      pointerEvents: 'none',
      transform: 'scale(1)',
      opacity: '0.9',
      transition: 'transform 420ms ease-out, opacity 420ms ease-out',
    } as CSSStyleDeclaration);
    document.body.appendChild(r);
    requestAnimationFrame(() => {
      r.style.transform = 'scale(4.5)';
      r.style.opacity = '0';
    });
    setTimeout(() => r.remove(), 460);
  };

  window.addEventListener('mousemove', moveCursor, true);
  window.addEventListener('mousedown', ripple, true);

  w.__demoCaption = (title: string, sub?: string): void => {
    ensure();
    const cap = document.getElementById(CAPTION_ID);
    if (!cap) return;
    if (!title) {
      cap.style.opacity = '0';
      return;
    }
    cap.innerHTML = sub
      ? `<div>${title}</div><div style="opacity:.7;font-size:13px;margin-top:2px">${sub}</div>`
      : `<div>${title}</div>`;
    cap.style.opacity = '1';
  };
}

/** Register the overlay so it runs before every page load in this context. */
export async function registerOverlay(page: Page): Promise<void> {
  await page.addInitScript(installOverlay);
}
