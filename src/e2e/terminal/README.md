# Terminal — E2E tests (playwright-bdd)

End-to-end tests for the [Operator terminal](../../web/terminal/README.md), driven
through its **web** target with [playwright-bdd](https://vitalets.github.io/playwright-bdd/)
— real Gherkin `.feature` files executed by Playwright, backed by a Page Object Model.

## Layout

```
.
├── e2e/
│   ├── features/     # Gherkin .feature files — the business language
│   ├── steps/        # step definitions (TypeScript + Playwright)
│   └── pages/        # Page Object Model — UI locators & actions
├── fixtures/         # dependency injection — Page Objects as test fixtures
└── playwright.config.ts   # runner + playwright-bdd plugin config
```

How the pieces connect:

1. `.feature` files describe behaviour in `Given / When / Then` (the source of truth).
2. `bddgen` reads them plus the step definitions and generates Playwright specs into
   `.features-gen/` (git-ignored).
3. Step definitions (`createBdd(test)`) translate each Gherkin line into Playwright
   actions, delegating all UI detail to a **Page Object**.
4. Page Objects encapsulate locators/assertions; they're injected into steps as
   **fixtures** (`fixtures/index.ts`), so a step just asks for `{ taskHub }`.

| Feature | Steps | Page Objects |
|---|---|---|
| `login.feature` | `login.steps.ts`, `common.steps.ts` | `LoginPage`, `TaskHubPage` |
| `task-hub.feature` | `task-hub.steps.ts`, `common.steps.ts` | `TaskHubPage` |
| `goods-receipt.feature` | `goods-receipt.steps.ts` | `GoodsReceiptPage`, `TaskHubPage` |
| `put-away.feature` | `put-away.steps.ts` | `PutAwayPage` |
| `picking.feature` | `picking.steps.ts` | `PickingPage` |
| `packing.feature` | `packing.steps.ts` | `PackingPage` |
| `move.feature` | `move.steps.ts` | `MovePage` |
| `operator-flow.feature` | `put-away.steps.ts`, `move.steps.ts`, `picking.steps.ts`, `packing.steps.ts`, `task-hub.steps.ts`, `common.steps.ts` | `TaskHubPage`, `PutAwayPage`, `MovePage`, `PickingPage`, `PackingPage` |
| `scan-dispatch.feature` | `scan-dispatch.steps.ts` | `ScanPage`, `GoodsReceiptPage`, `LookupPage` |
| `lookup.feature` | `lookup.steps.ts` | `LookupPage` |
| `navigation.feature` | `navigation.steps.ts` | `TabBar`, `LookupPage` |
| `language.feature` | `language.steps.ts`, `task-hub.steps.ts` | `LanguagePage`, `TaskHubPage` |

## Use-case coverage ([docs/03-use-cases.md](../../../docs/03-use-cases.md))

The terminal owns the operator/forklift floor flows; the desk use cases live in the
[admin suite](../admin/README.md). Each terminal use case is exercised here, **happy
path and the modelled exceptions** ([02-exceptions.md](../../../docs/design/02-exceptions.md)).

| UC | Use case | Covered by |
|---|---|---|
| UC-02 | Receive delivery | `goods-receipt.feature` — ASN context, count via stepper, confirm → hub, **discrepancy reason** (damage → QC) still receives & drops the pile |
| UC-04 | Put away goods | `put-away.feature` — proposed location, temperature/capacity checks (Invariant #1/#2), **location full → propose another bay**; `operator-flow.feature` — confirm drops the pile |
| UC-06 | Move stock | `move.feature` — from/to legs + compatibility checks; `operator-flow.feature` — confirm & **inter-warehouse transfer** clear the move task |
| UC-10 | Picking | `picking.feature` — go-to location, FEFO batch, scan-gated confirm, **short pick → FEFO replan**; `operator-flow.feature` — confirm hands off to packing |
| UC-11 | Packing | `packing.feature` — active package, contents/weight/dimensions, **add another package**; `operator-flow.feature` — close package → hub |

Operator sign-in (badge scan), the universal scan dispatch, read-only look-up, tab
navigation and the PL/EN language toggle are covered by `login`, `scan-dispatch`,
`lookup`, `navigation` and `language` respectively. The terminal gates on a signed-in
operator, so the suite seeds a stored session in the `page` fixture; the `login` flow
opts out with the `@anonymous` tag to exercise the badge screen itself.

## Running

```bash
npm install
npx playwright install chromium    # one-time browser download
npm test                           # bddgen + playwright test (headless)
npm run test:headed                # watch it drive the UI
npm run test:ui                    # Playwright UI mode
npm run report                     # open the last HTML report
```

`npm test` runs `bddgen` first to (re)generate the specs, then `playwright test`. The
config boots the terminal's Expo web dev server automatically (`npm run web` in
`../../web/terminal`, port **8081**) and waits for it — you don't start the app yourself.
The first request triggers a Metro bundle (~10 s), hence the generous timeouts. The
browser is sized to the **390 px** handheld width the terminal is designed for.

## How elements are targeted

No `data-testid`s were added to the app — Page Objects lean on what the design already
makes accessible: `react-native-web` maps `accessibilityRole` → ARIA `role` and
`accessibilityLabel` → `aria-label`, so locators use `getByRole('button' | 'tab')`,
`getByPlaceholder(...)` and visible text. Two gotchas the POMs handle for you:

- expo-router appends `?__EXPO_ROUTER_key=…` to URLs → URL assertions allow a query.
- Tab navigation keeps prior screens **mounted-but-hidden**; role queries exclude hidden
  nodes, so "are we back on the hub?" checks a role (the *Receive* tile), not loose text.

## Locale & theme are pinned for determinism

The terminal defaults to **Polish** (floor staff) and remembers the high-contrast
choice — both read from `localStorage` at first render. So this English-authored suite
pins a stable baseline in the `page` fixture via `addInitScript` (`wms-locale = en`,
`wms-hc = 0`) *before* the app boots, keeping every copy assertion stable regardless of
the app's default. The Polish path isn't skipped: `language.feature` drives the in-app
**PL/EN** toggle and asserts the Polish copy (e.g. *Twoje zadania*, the *Przyjęcie /
Odkładanie / Kompletacja / Przesunięcie* piles), so both catalogues are exercised.

## Backend

The terminal's backend is mocked with MSW (ADR-0006, `src/core/mocks`), intercepting the
`fetch` calls the app makes from day one — so these tests are deterministic without any
server. Writes are **stateful** (a confirmed task drops its pile, a short pick replans),
and that state lives in the page, so the cross-screen journeys in `operator-flow.feature`
navigate via the UI (tap → confirm → back) rather than `page.goto`, which would reload and
reset it. When the real Gateway is wired up, turn the worker off (or stub with
`page.route(...)`).
