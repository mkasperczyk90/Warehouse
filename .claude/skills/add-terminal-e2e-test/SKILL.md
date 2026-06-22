---
name: add-terminal-e2e-test
description: Add or extend playwright-bdd end-to-end coverage for the Warehouse operator terminal (src/e2e/terminal) — a Gherkin .feature + step definitions + a Page Object + fixture wiring, following the suite's conventions and use cases (docs/03-use-cases.md). Use when covering a terminal screen, flow, exception, or new mutation in e2e, or when a terminal change needs its tests updated.
---

# Add a terminal e2e test

Recipe for `src/e2e/terminal` (playwright-bdd driving the terminal's **web** target). Read
[`src/e2e/terminal/README.md`](../../../src/e2e/terminal/README.md) first. The pieces connect as:
`.feature` (Gherkin, the source of truth) → `e2e/steps/*.steps.ts` (`createBdd(test)`) → `e2e/pages/*.ts`
(Page Object Model — all locators/assertions) → `fixtures/index.ts` (each POM injected as a fixture).
`bddgen` generates Playwright specs into `.features-gen/` (gitignored); the npm scripts run it first.

Mirror an existing one of the same shape: task screen → `PutAwayPage`/`MovePage`; scan-gated →
`PickingPage`; tab/list → `LookupPage`/`ScanPage`; cross-screen journey → `operator-flow.feature`.

## Steps

1. **Page Object** `e2e/pages/<Name>Page.ts` — `constructor(page)`, `open()` → `page.goto('/route')`,
   `expectShown()` (assert URL `/route(\?|$)/` + a stable marker), then action/assert methods. **No
   test-ids** — lean on what the design already exposes (react-native-web maps `accessibilityRole` → ARIA
   `role`, `accessibilityLabel` → `aria-label`): `getByRole('button' | 'tab', { name })`,
   `getByPlaceholder(...)`, `getByText(..., { exact })`, `getByLabel(...)`. Scope a pile's count with the
   pile locator (`pile(name).getByText(count, { exact: true })`). To scan: `fill` the textbox then
   `press('Enter')` (ScanField needs a non-empty value + Enter).
2. **Fixture** — add the POM to `TerminalFixtures` and an `extend` entry in `fixtures/index.ts`.
3. **`.feature`** — `Feature` / `Background` / `Scenario` in business language; tag the use case in the
   title (e.g. `Picking (UC-10)`). Cover the **happy path and the modelled exceptions**
   ([02-exceptions.md](../../../docs/design/02-exceptions.md)): discrepancy reason, location-full, short
   pick, transfer, pile-drop after a confirm.
4. **Steps** — `createBdd(test)`; import `{ expect, test }` from `../../fixtures`; declare the POMs you
   need (`async ({ putAway }) => …`). Keep step phrases **unique across all step files** — playwright-bdd
   errors on a duplicate step definition. Use feature-specific wording, or put a genuinely shared step in
   `common.steps.ts` / `task-hub.steps.ts` (e.g. `the {string} pile shows {string}`).

## Gotchas (these bite — encode them)

- **MSW state lives in the page.** A confirm mutates in-memory fixtures (a pile drops, a short pick
  replans). For a cross-screen journey that must keep that state, navigate **via the UI** (tap pile →
  confirm → back), **never `page.goto`** mid-flow — a full reload resets the fixtures. `router.back()` /
  `dismissAll()` only reach the hub when the journey **started at the hub** (a real nav stack), so begin
  those scenarios with `Given the operator opens the terminal`. Screen-only checks (display, in-place
  replan, propose-another) can use a `goto` Background.
- **Locale + theme are pinned** for determinism in the `page` fixture via `addInitScript`
  (`wms-locale=en`, `wms-hc=0`) before first render. The Polish path is covered by `language.feature`, so
  this English-authored suite stays stable. Don't assert locale-formatted numbers.
- **expo-router** appends `?__EXPO_ROUTER_key=…` → URL assertions allow `(\?|$)`.
- **Tab nav keeps prior screens mounted-but-hidden** → role queries exclude hidden nodes; check a role (a
  tile) for "are we back on the hub?", not loose text.

## Finish

Run `npm test` from `src/e2e/terminal` (`bddgen` + `playwright test`; it boots Expo web on :8081
automatically). `npm run test:headed` to watch it drive, `npm run report` for the HTML report. Update the
README's **feature table** and **use-case coverage** section. If chromium is missing,
`npx playwright install chromium`.
