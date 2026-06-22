# Working in the Warehouse Admin panel (`src/web/admin`)

Conventions for this app. Follow them — the codebase is deliberately uniform, so new work should look
like existing work. See [`README.md`](README.md) for the tour and [`TODO.md`](TODO.md) for the backlog.

## What this is

A browser-native React SPA for the desk user (manager / coordinator / inspector) — data-dense tables,
filters, master-data forms. Separate from the React Native terminal; the two share **design tokens and
domain language, not runtime** (ADR-0004). The backend is mocked at the network boundary with MSW and the
app calls `fetch` from day one — going live is turning MSW off, never a rewrite (ADR-0006).

**Stack:** Vite · React 19 · TypeScript (strict) · TanStack Router (typed routes) · TanStack Query
(server state) · TanStack Table (headless) · React Hook Form + Zod (forms) · CSS Modules + vendored
`tokens.css` · react-i18next (EN/PL) · lucide-react · MSW · Vitest + React Testing Library.

## The golden rules

1. **Status colour encodes domain status, never decoration.** Use `StatusBadge` with a `StatusVariant`
   (`available | reserved | blocked | expired | transit`). Quantities use `QuantityWithUnit` (never a bare
   number where a unit is meaningful).
2. **One API seam.** All data goes through `src/core/api/client.ts` (`api.get/post/put`). Never `fetch`
   directly in a component. Fixtures live in `src/core/mocks/handlers.ts` and are the *contract*; the
   `x.model.ts` types are the spec.
3. **Feature slices.** Each screen is a folder under `src/features/<Name>/`: `x.model.ts` (types + Query/
   mutation hooks), `XScreen.tsx` (view), `XScreen.module.css`, `index.ts` (public API), `XScreen.test.tsx`.
   Cross-feature imports go through `index.ts`; UI primitives through `@/shared/ui`. `@/*` → `src/*`.
4. **Every string is translated, in EN *and* PL.** Add keys to both `src/shared/i18n/en.ts` and `pl.ts`
   (nested objects, same shape). No literal user-facing text in components.
5. **Definition of done:** `npm run typecheck` clean, `npm run test:run` green, `npm run build` green.
   Update EN+PL i18n and `TODO.md` for anything you finish or discover.

## Patterns to reuse (don't reinvent)

- **List → detail → action.** Lists use `DataTable` with `onRowClick` to navigate to a detail route, or
  master-detail with local `selected` state (Inbound/Outbound). Detail screens carry the actions.
- **`shared/ui/DataTable`** — headless, already sortable + paginated (row count + Prev/Next, default page
  size 25). Pass `columns` (TanStack `ColumnDef[]`), `data`, optional `onRowClick`. Right-align a column
  with `meta: { align: 'right' }`.
- **`shared/ui/Modal`** — every dialog (create / confirm / reason / assign). Open/close via local state.
- **Confirm + reason on irreversible writes.** Ledger posts, QC decisions, blocks → a `Modal` that
  captures a reason (required) and summarises the change before the mutation fires. See
  `Adjustment`, `Quality`, `Stock` (Block), `Inbound` (resolve), `Outbound` (split/hold).
- **`shared/ui/Board` / `BoardColumn`** — the kanban (Dispatch). `KpiCard`, `FilterBar` for dashboards/lists.
- **Forms:** big master-data forms use **React Hook Form + Zod** with the Zod schema mirroring domain
  invariants (e.g. `min(0)` = stock never negative; `refine` for temp ranges). Small/dynamic forms (create
  dialogs with a lines editor) use plain `useState`. Validation messages are short; render `errors.x.message`.

## Routing

`src/navigation/routes.ts` holds the typed `ROUTES` map + `ROUTE_META` (breadcrumb). `src/router.tsx`
registers them (code-based TanStack Router). For a **param route** (`/x/$id`), export a thin wrapper that
reads the param and renders the screen with a prop:

```tsx
export function FooRoute() {
  const params = useParams({ strict: false });
  return <FooScreen id={params.id} />;
}
```

Screens that navigate use `useNavigate()`; sidebar links use `<Link to={ROUTES.x}>`.

## Mutations (writes)

A write is a `useMutation` in `x.model.ts` calling `api.post(...)`. In the component: call `mutate(body, {
onSuccess })`, and in `onSuccess` **invalidate the relevant queries** (`queryClient.invalidateQueries({
queryKey: [...] })`) so the UI refetches. Make the **MSW handler stateful** (mutate the in-memory fixture
array/record) so the change persists across the refetch — that's how create/move/assign/advance "work".
QC uses an optimistic update (`onMutate`/`onError`/`onSettled`) as the reference for snappy list actions.

## Tests (Vitest + RTL)

- Render with `renderWithProviders` from `@/test/render` (gives Query + i18n). Tests hit the **same MSW
  handlers** via `msw/node` (`src/test/setup.ts`).
- If the screen calls `useNavigate`, **mock it** at the top of the test file:
  `vi.mock('@tanstack/react-router', async (o) => ({ ...(await o()), useNavigate: () => vi.fn() }))`.
- `getByLabelText` needs a real association: wrap the control in its `<label>`, or set `aria-label`. A bare
  text node next to a sibling isn't matched by `getByText` (wrap names in a `<span>`).
- Don't assert locale-formatted numbers (`toLocaleString` uses a space separator in Node, comma in the
  browser) — assert labels or unique text instead.
- **Stateful mocks persist across tests in a file** (module state, isolated per file). Order tests so a
  mutating test runs *after* tests that assert the pre-mutation state, or assert on records other tests
  don't touch.

## Commands

```
npm run dev        # Vite dev server (MSW serves fixtures)
npm run mock:init  # one-time: generate public/mockServiceWorker.js (gitignored)
npm run typecheck  # tsc --noEmit
npm run test:run   # vitest run
npm run build      # tsc --noEmit && vite build
```

## Out of scope (don't build without asking)

High-contrast theme (terminal-only), BI dashboards (the worklist landing is *not* BI), external-actor
portals. These are deferred in [`../../../docs/design/README.md`](../../../docs/design/README.md#scope).
(Auth/identity is now built — badge-scan sign-in in `features/Auth` + `shared/auth`, profile in
`features/Profile` — but it is **mock-only**: a real token/session still attaches at the api seam.)
