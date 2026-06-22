---
name: add-terminal-feature
description: Scaffold or extend a screen, task flow, or write action in the Warehouse operator terminal (src/web/terminal) — a React Native (Expo) handheld app. Follows its conventions: a feature slice (model + screen + index), async data through the api seam with useResource/ResourceView, stateful MSW GET+POST handlers, ActionSheet for exception reasons, EN/PL i18n, and an expo-router route. Use when adding or extending a terminal task screen, scan/confirm flow, list/lookup, or a confirm/exception action (put-away, pick, move, pack, receive…).
---

# Add a terminal feature

Recipe for `src/web/terminal`. Read [`src/web/terminal/README.md`](../../../src/web/terminal/README.md)
first — it holds the folder layout and conventions. There is no `CLAUDE.md` here; the README + this
recipe are the spec. Mirror an existing feature of the same shape rather than inventing structure:

- **scan + confirm + exception** task screen → `Receiving`, `PutAway`, `Movement`
- **multi-step scan-gated** screen → `Picking` (location → product → confirm)
- **top-level tab** (search / dispatcher) → `Lookup`, `Scan`
- **landing hub** with task piles → `Tasks`

The terminal is scanner-first: one hand, gloves, cold room, glare. One task at a time, huge tap targets
(≥ `TAP` = 56px), zero prose. It shares **design tokens + domain language** with the admin, not runtime.

## A new screen (feature slice)

1. **`src/features/<Name>/<name>.model.ts`** — the wire types (interfaces). Domain status fields are typed
   `StatusKey` from `@/shared/theme/tokens`. Data getter: `getX = (): Promise<T> => api.get<T>('resource')`
   (the single seam — never `fetch` in a component). View config (icon / colour / route / `IconName`) is
   composed **client-side** over the fetched domain data (see `Tasks`/`Scan`), never sent by the server.
2. **`src/features/<Name>/<Name>Screen.tsx`** — split for clean async + the hooks rules:
   - outer `XScreen` calls `useResource(getX)` and returns
     `<ResourceView resource={r}>{(data) => <XView … />}</ResourceView>`;
   - inner `XView` holds every data-dependent hook (`useState` seeded from `data`). For an in-place
     refetch (an exception that replans), pass `reload={r.reload}` into `XView`.
   - Shell: `ScreenScaffold` (task screens — `title`/`subtitle`/optional `accent` + 2–3 `BigActionButton`
     in `actions`) or `TabScaffold` (top-level tabs). Primitives from `@/shared/ui`: `ScanField`
     (always-focused; `onScan` fires on Enter with a non-empty code), `StatusBadge`, `QuantityWithUnit`
     (never a bare number), `Chip`, `CheckRow` (the green env/capacity invariant rows), `Card`, `Stepper`,
     `Keypad`, `ActionSheet`.
3. **`src/features/<Name>/index.ts`** — re-export the screen + types + getters/mutations (and any pure
   helper like `resolveScan`). Cross-feature imports go through `index.ts`; `@/*` → `src/*`.
4. **i18n** — add keys to **both** `src/shared/i18n/en.ts` and `pl.ts`. These are **flat dotted** keys
   (`'pick.confirm': '…'`), not nested objects like the admin. No literal user-facing text in components;
   data strings (SKUs, addresses, lot numbers) stay in the fixtures, untranslated.
5. **MSW** — add fixtures + `http.get('/api/resource', …)` to `src/core/mocks/handlers.ts`; import the
   response types from `@/features/<Name>`. The fixtures are the contract; the `x.model.ts` types the spec.
6. **Route** — expo-router is file-based. Add `ROUTES.<name>` to `src/navigation/routes.ts`; add a thin
   `src/app/<name>.tsx` that re-exports the screen; register `<Stack.Screen name="<name>" />` in
   `src/app/_layout.tsx`. For a top-level tab, also wire `BottomNav`.

## A write action (mutation) + its exceptions

1. **Model:** `confirmX = (body?): Promise<void> => api.post<void>('resource/verb', body)`. Add exception
   variants — `proposeAnotherX()` (cycle a candidate), `shortX(reason)` / discrepancy (replan / record),
   `transferX()` (alternate destination). Type reason unions (e.g. `DiscrepancyReason`, `ShortReason`).
2. **MSW:** add `http.post('/api/resource/verb', …)` that **mutates in-memory state** (drop a hub count
   via the `drop()` helper, cycle a candidate index, bump a counter) and returns `204` — so the next GET
   reflects it. That is how a confirm actually does something.
3. **Screen:** a `pending` state disables the buttons during the POST (idempotent / no double-submit — §2
   of the exceptions doc). Confirm → `await` the mutation → `router.back()` / `router.push(next)` /
   `router.dismissAll()`. In-place exception → `await` → `reload()`. A **reason-bearing** exception opens
   an `ActionSheet` (the terminal's reason picker; a hard / QC-bound choice gets `danger: true`).
4. **Hub reflects it:** counts are stateful in MSW and the hub refetches on focus
   (`useFocusEffect(useCallback(() => reload(), [reload]))`) — the terminal's analog of query invalidation.
5. **i18n** (EN+PL) for button labels, sheet titles and reason options.

## Invariants (don't break)

- Status colour **encodes domain stock status, never decoration** — `StatusBadge` + `StatusKey`
  (`available | reserved | blocked | expired | transit`). Quantities → `QuantityWithUnit`.
- Hard environment / capacity stops are shown, never bypassed (Invariants #1/#2) — `CheckRow` rows +
  "propose another". See [`docs/design/02-exceptions.md`](../../../docs/design/02-exceptions.md) for the
  modelled unhappy paths and [`docs/03-use-cases.md`](../../../docs/03-use-cases.md) for the use case.

## Finish

Run `npm run typecheck` from `src/web/terminal` (there is no unit-test runner here — coverage is e2e). If
the MSW worker is missing, `npm run mock:init`. Keep the screen faithful to
`docs/design/prototypes/terminal-*.html`. Then add matching e2e with the **add-terminal-e2e-test** skill.
