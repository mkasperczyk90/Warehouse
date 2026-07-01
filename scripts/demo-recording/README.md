# Demo recorder — step-by-step GIF of the golden path

Drives the [`docs/demo-walkthrough.md`](../../docs/demo-walkthrough.md) golden path across **both** front
ends with Playwright and records it as a video → **GIF** (and MP4), with a **visible, animated mouse
cursor** and on-screen scene captions. It looks like a person clicking through the app.

- **Admin** take — scenes ① ② ④ ⑥ ⑦ ⑨ ⑩ (Manager / Coordinator / Inspector). Runs against the admin's
  own MSW-mocked backend (ADR-0006) — no services needed.
- **Terminal** take — scenes ③ ⑤ ⑧ + Move (Warehouse Operator). The handheld always calls the real
  Gateway, so we stub `/api` at the Playwright layer (a copy of the terminal e2e mocks) — also no backend.

The apps run **from the main checkout** (`src/web/admin`, `src/web/terminal`), so the recording always
exercises the current working code.

## Prerequisites

- Node + the apps' deps installed (`npm ci` in `src/web/admin` and `src/web/terminal`).
- `ffmpeg` on `PATH` (for the GIF/MP4 conversion).
- Playwright's Chromium: `npm i` here, then `npx playwright install chromium` if it isn't cached already.

## Run

```bash
cd scripts/demo-recording
npm install

# Both apps (Vite+MSW admin, Expo-web terminal). Boots the dev servers itself.
npm run record

# Or one app at a time (the Expo bundle is slow — admin alone is quick):
DEMO_ONLY=admin    npm run record:admin
DEMO_ONLY=terminal npm run record:terminal
```

Output lands in **`./output`**:

| File | What |
|---|---|
| `admin-golden-path.gif` / `.mp4` | the desk-app act |
| `terminal-golden-path.gif` / `.mp4` | the handheld act |
| `golden-path-full.gif` | admin then terminal, padded onto one canvas (only when both takes ran) |

The GIFs are produced automatically after the take by `globalTeardown`. To (re)convert existing
recordings without re-running the browser: `npm run gif`.

## Knobs (env vars)

| Var | Default | Effect |
|---|---|---|
| `DEMO_APP_ROOT` | `../..` | Repo root holding `src/web/*`. Set this when running from a git worktree. |
| `DEMO_ONLY` | _(both)_ | `admin` or `terminal` — boot/record just one app. |
| `DEMO_PACE` | `1` | Pause multiplier. `1.5` = calmer, `0.5` = snappier. |
| `DEMO_SLOWMO` | `120` | Playwright `slowMo` ms between input steps. |
| `DEMO_GIF_FPS` | `14` | GIF frame rate. |
| `DEMO_GIF_W` | _(native)_ | Force GIF width in px (height auto). |
| `DEMO_NO_GIF` | _(off)_ | Skip the GIF conversion (keep only the webm). |

## How it works

- **Cursor + captions** (`lib/overlay.ts`) are injected with `addInitScript`, so they survive SPA
  navigations and reloads. Playwright's `mouse.*` dispatch real DOM mouse events; a capturing
  `mousemove` listener moves the fake cursor.
- **Human-like input** (`lib/human.ts`): every click first glides the cursor to the target with
  `mouse.move(..., { steps })`; typing and scans go key-by-key.
- **Scenes** (`specs/*.spec.ts`) follow the walkthrough beat for beat. Admin write actions are "soft" —
  if a button isn't found the take narrates and continues rather than aborting.

## Notes

- This folder is standalone; it does **not** run in CI and is independent of `tests/e2e/*`.
- For a true cross-app data flow (a product defined in ① actually reaching ⑩), run the Aspire stack and
  point the apps at it — but for a clean, repeatable demo GIF the mocked path above is the intended use.
