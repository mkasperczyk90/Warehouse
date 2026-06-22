---
name: add-admin-feature
description: Scaffold a new screen, list/detail, or write action in the Warehouse admin panel (src/web/admin) following its established conventions — a feature slice (model + screen + styles + index + test), MSW fixtures, EN/PL i18n, and a typed route. Use when adding or extending an admin screen, table, detail view, form, or row/detail action (create/move/assign/decision/etc.).
---

# Add an admin feature

Recipe for `src/web/admin`. Read [`src/web/admin/CLAUDE.md`](../../../src/web/admin/CLAUDE.md) first — it
holds the conventions this skill assumes. Mirror an existing feature of the same shape rather than
inventing structure: list+detail → `Inbound`/`Outbound`; table+filters → `Stock`; form → `Adjustment`/
`Products`; kanban → `Dispatch`; list+create → `Products` (catalogue) / `Stocktake`.

## A new screen (feature slice)

1. **`src/features/<Name>/<name>.model.ts`** — the wire types (interfaces) + Query hooks
   (`use<Name>List`, `use<Name>(id)`) calling `api.get<T>('resource')`. `id`-scoped queries pass
   `enabled: !!id`. Domain status fields are typed `StatusVariant` from `@/shared/ui`.
2. **`src/features/<Name>/<Name>Screen.tsx`** — the view. Loading/error/empty states first
   (`t('state.loading' | 'state.error' | 'state.empty')`). Tables → `DataTable` (+ `onRowClick` to
   navigate); badges → `StatusBadge`; quantities → `QuantityWithUnit`; cards → `KpiCard`; dialogs →
   `Modal`. For a `/x/$id` route also export a `XRoute` wrapper using `useParams({ strict: false })`.
3. **`src/features/<Name>/<Name>Screen.module.css`** — CSS Modules, only `tokens.css` vars (`var(--s-4)`,
   `var(--status-blocked)`, `var(--fs-md)`, …). No hard-coded colours/spacing except the dark sidebar.
4. **`src/features/<Name>/index.ts`** — re-export the screen(s) + hooks + types.
5. **i18n** — add a `<name>:` block to **both** `src/shared/i18n/en.ts` and `pl.ts` (same nested shape).
6. **MSW** — add fixtures + `http.get('/api/resource', …)` handlers to `src/core/mocks/handlers.ts`. Import
   the response types from `@/features/<Name>`.
7. **Route** — add `ROUTES.<name>` + `ROUTE_META` in `src/navigation/routes.ts`; register a `createRoute`
   in `src/router.tsx` and add it to `routeTree`. Add a sidebar entry in `src/shared/layout/Sidebar.tsx`
   if it's top-level.
8. **Test** — `src/features/<Name>/<Name>Screen.test.tsx` with `renderWithProviders`; assert it renders
   from the mocked Gateway and any filter/interaction. Mock `useNavigate` if the screen navigates.

## A write action (mutation) on an existing screen

1. **Model:** add `use<Verb>(id?)` → `useMutation({ mutationFn: (body) => api.post(\`resource/${id}/verb\`,
   body) })`.
2. **Mock:** add a `http.post('/api/resource/:id/verb', …)` handler that **mutates the in-memory fixture**
   (so the refetch reflects it) and returns `204`/JSON.
3. **Screen:** wire a button (and a `Modal` if it needs input/confirmation — required for irreversible
   posts, with a reason/summary). On `onSuccess`, `queryClient.invalidateQueries({ queryKey: [...] })`.
   For instant list feedback use the optimistic pattern in `features/Quality/qc.model.ts`.
4. **i18n** (EN+PL) for the button/dialog labels. **Test** the action (button → mutate → state changes;
   disabled until valid).

## Finish

Run `npm run typecheck`, `npm run test:run`, `npm run build` (all from `src/web/admin`). Update
`src/web/admin/TODO.md` for what you finished or newly discovered. If a screen has a design prototype,
keep it faithful to `docs/design/prototypes/admin-*.html`.
