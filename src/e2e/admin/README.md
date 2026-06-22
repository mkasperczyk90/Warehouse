# Admin — E2E tests (playwright-bdd)

End-to-end tests for the [admin panel](../../web/admin) (the desk app), driven
through its browser SPA with [playwright-bdd](https://vitalets.github.io/playwright-bdd/)
— real Gherkin `.feature` files executed by Playwright, backed by a Page Object Model.
Mirrors the [terminal suite](../terminal/README.md) in structure and conventions.

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
   **fixtures** (`fixtures/index.ts`), so a step just asks for `{ stock }`.

| Feature | Steps | Page Objects |
|---|---|---|
| `login.feature` | `auth.steps.ts`, `common.steps.ts` | `LoginPage`, `TodayPage` |
| `warehouse.feature` | `warehouse.steps.ts`, `stock.steps.ts`, `common.steps.ts` | `AppShell`, `StockPage` |
| `profile.feature` | `profile.steps.ts`, `common.steps.ts` | `ProfilePage`, `AppShell` |
| `navigation.feature` | `navigation.steps.ts`, `common.steps.ts` | `AppShell`, `TodayPage`, `StockPage` |
| `today.feature` | `today.steps.ts`, `common.steps.ts` | `TodayPage` |
| `stock.feature` | `stock.steps.ts`, `common.steps.ts` | `StockPage` |
| `stock-item.feature` | `stock-item.steps.ts`, `common.steps.ts` | `StockItemPage` |
| `search.feature` | `search.steps.ts`, `common.steps.ts` | `SearchPage` |
| `language.feature` | `language.steps.ts`, `common.steps.ts` | `AppShell` |
| `inbound.feature` | `inbound.steps.ts`, `common.steps.ts` | `InboundPage` |
| `quality.feature` | `quality.steps.ts`, `common.steps.ts` | `QualityPage` |
| `stocktake.feature` | `stocktake.steps.ts`, `common.steps.ts` | `StocktakePage` |
| `adjustment.feature` | `adjustment.steps.ts`, `common.steps.ts` | `AdjustmentPage` |
| `outbound.feature` | `outbound.steps.ts`, `common.steps.ts` | `OutboundPage` |
| `dispatch.feature` | `dispatch.steps.ts`, `common.steps.ts` | `DispatchPage` |
| `products.feature` | `products.steps.ts`, `common.steps.ts` | `ProductsPage` |
| `topology.feature` | `topology.steps.ts`, `common.steps.ts` | `TopologyPage` |
| `movements.feature` | `movements.steps.ts`, `common.steps.ts` | `MovementsPage` |

## Authentication & the seeded session

The admin gates on a badge sign-in (`features/Auth`). So that every feature except
`login.feature` opens straight into the panel, the `page` fixture seeds an authenticated
desk user (K. Manager · WH-01 · English) into `localStorage` before the SPA boots
(`fixtures/index.ts`). `LoginPage.open()` clears that seed to exercise the sign-in screen.
`warehouse.feature` then drives the TopBar switcher (WH-01 → WH-02) and asserts the stock
table re-scopes; `profile.feature` covers the profile screen and the user menu. The PL/EN
toggle now lives in that user menu (`AppShell.switchLanguage` opens it).

## Use-case coverage (docs/03-use-cases.md)

Each business use case the admin panel owns is exercised by a feature. Use cases
UC-04 (put-away), UC-10 (picking) and UC-11 (packing) are **operator** flows on
the handheld terminal, not the desk — they're covered by the [terminal
suite](../terminal/README.md), not here.

| UC | Use case | Covered by |
|---|---|---|
| UC-01 | Announce delivery (ASN) | `inbound.feature` — list, create, assign dock, mark arrived, resolve unknown SKU |
| UC-02 | Receive delivery | `inbound.feature` — view receiving progress |
| UC-03 | Quality inspection | `quality.feature` — release/reject with required reason; `stock-item.feature` — block → quarantine |
| UC-04 | Put away goods | _terminal (operator)_ |
| UC-05 | View stock | `stock.feature`, `stock-item.feature` — KPIs, filters, drill-down |
| UC-06 | Move stock | `stock-item.feature` — move dialog + environment invariant |
| UC-07 | Stocktake | `stocktake.feature` — start blind count, review, reason-gated approval, recount |
| UC-08 | Stock adjustment | `adjustment.feature` — delta, below-zero invariant, confirm-before-post |
| UC-09 | Outbound order | `outbound.feature` — create, split/hold, release, cancel |
| UC-10 | Picking | _terminal (operator)_ |
| UC-11 | Packing | _terminal (operator)_ |
| UC-12 | Dispatch to carrier | `dispatch.feature` — assign carrier, advance, filter, waybill/tracking |
| UC-13 | Manage products | `products.feature` — catalogue, edit, temp-range & SKU invariants |
| UC-14 | Manage topology | `topology.feature` — tree, room detail, add location, save room |

The immutable movement ledger (the projection source behind UC-05/06/08) is
exercised on its own by `movements.feature`.

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
config boots the admin's Vite dev server automatically (`npm run dev` in
`../../web/admin`, port **5179**) and waits for it — you don't start the app yourself.

## How elements are targeted

No `data-testid`s were added to the app — Page Objects lean on what the markup already
makes accessible: sidebar entries are `getByRole('link')`, KPI cards / table rows /
filter pills are `getByRole('button')`, inputs are reached by their placeholder, and
everything else by visible text. Two conventions the POMs encode:

- Clickable table rows render as `role="button"` (keyboard-accessible drill-down), so
  "is this row shown?" is a role query, not loose text.
- The Today landing is reached at `/` — the router redirects `/` → `/today`, so URL
  assertions check `/today`.

## Locale is English by default

The admin boots in **English** (`i18n` `lng: 'en'`, no persisted locale), so every copy
assertion is stable without any setup. The Polish path isn't skipped: `language.feature`
drives the in-app **EN/PL** toggle in the top bar and asserts the Polish navigation copy
(*Stany*, *Dziś*), so both catalogues are exercised.

## Backend

The admin's backend is mocked with MSW (ADR-0006, `src/core/mocks`), so these tests are
deterministic without any server. When the real Gateway is wired up, swap the worker for
network stubbing (`page.route(...)`) or a seeded test environment in the fixtures.
