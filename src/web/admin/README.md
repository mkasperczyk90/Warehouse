# Warehouse Admin panel

The **Admin panel** front end — a browser-native React SPA for the manager/coordinator at a desk:
data-dense tables, filters, bulk actions, master-data forms. The counterpart to the
[Operator terminal](../terminal/README.md); the two share the design language, not the runtime
([ADR-0004](../../../docs/adr/0004-admin-panel-separate-spa.md)).

It implements the admin screens from the design pass — the prototypes in
[`docs/design/prototypes/admin-*.html`](../../../docs/design/prototypes/) are the design source of
truth, and [`tokens.css`](src/shared/theme/tokens.css) is vendored from
[the design tokens](../../../docs/design/prototypes/tokens.css)
([ADR-0005](../../../docs/adr/0005-shared-design-tokens.md)). The full build plan lives in
[`docs/design/03-admin-frontend-plan.md`](../../../docs/design/03-admin-frontend-plan.md).

> **Backend is mocked at the network boundary.** The app calls `fetch` through the single
> [`src/core/api/client.ts`](src/core/api/client.ts) seam; in dev/test **MSW** serves fixtures, and
> in production the same calls hit the Gateway. Going live is turning the worker off — not a rewrite
> ([ADR-0006](../../../docs/adr/0006-mock-at-the-network-boundary.md)).

## Working with an LLM / agent

[`CLAUDE.md`](CLAUDE.md) captures the conventions an agent (or a new contributor) needs to extend this app
the way it's already built — the seam, feature slices, the reuse patterns, i18n, routing, mutations and
the test gotchas. The repo-level skill `add-admin-feature` (`.claude/skills/`) encodes the step-by-step
recipe for adding a screen or a write action. Read `CLAUDE.md` before making changes here.

## Folder structure

```
src/
├── main.tsx          # React root + providers (Query, Router); starts MSW
├── router.tsx        # TanStack Router tree (AppShell layout + screen routes)
├── core/             # infrastructure: the API seam + the MSW mock server
│   ├── api/          #   client.ts (fetch seam) + queryClient.ts
│   └── mocks/        #   browser.ts + handlers.ts (the fixtures)
├── features/         # business logic by screen — THE MAIN WORK AREA
│   └── Stock/        #   stock view · UC-05 (admin-1-stock)
├── shared/
│   ├── theme/        #   tokens.css (vendored) + global.css
│   ├── layout/       #   AppShell, Sidebar, TopBar
│   ├── ui/           #   StatusBadge, QuantityWithUnit, DataTable, FilterBar, KpiCard
│   └── i18n/         #   en / pl (react-i18next)
└── navigation/
    └── routes.ts     # typed path map + breadcrumb metadata
```

Each feature is self-contained: `XScreen.tsx` (view) + `x.model.ts` (types + Query hooks) +
`index.ts` (public API). Cross-feature imports go through `index.ts`; UI primitives through
`@/shared/ui`. The `@/*` alias maps to `src/*`.

## Stack

Vite · React 19 · TypeScript · TanStack Router (typed routes) · TanStack Query (server-state) ·
TanStack Table (headless tables) · React Hook Form + Zod (forms & validation) · CSS Modules ·
react-i18next · lucide-react · MSW.

## Running

```bash
npm install
npm run mock:init   # one-time: generates public/mockServiceWorker.js (gitignored)
npm run dev         # Vite dev server
```

`npm run build` type-checks and bundles; `npm run typecheck` runs `tsc` with no emit.

## Tests

```bash
npm test         # Vitest watch mode
npm run test:run # single run (CI)
```

Vitest + React Testing Library, jsdom environment. Tests run against the **same MSW handlers**
that back the dev server (`src/test/setup.ts` boots them via `msw/node`) — fixtures are defined
once and serve both dev and tests (ADR-0006). `src/test/render.tsx` wraps a screen in the providers
it needs. See [`features/Stock/StockScreen.test.tsx`](src/features/Stock/StockScreen.test.tsx) for
the pattern: render → await async rows → assert badges, KPIs, and filtering.

## Screens & build phases

| Route                                            | Feature                                              | UC             | Prototype          | Status                           |
| ------------------------------------------------ | ---------------------------------------------------- | -------------- | ------------------ | -------------------------------- |
| `/today` (the `/` landing)                       | `Today/TodayScreen`                                  | cross-cutting  | admin-10-worklist  | **built**                        |
| `/stock` · `/stock/$id`                          | `Stock/{StockScreen, StockItemScreen}`               | UC-05          | admin-1-stock      | **built (phase 1 + drill)**      |
| `/movements`                                     | `Movements/MovementsScreen`                          | UC-05 (ledger) | —                  | **built**                        |
| `/inbound` · `/inbound/$id/receiving`            | `Inbound/{InboundScreen, ReceivingScreen}`           | UC-01/02       | admin-2-asn        | **built (phase 2 + receiving)**  |
| `/outbound`                                      | `Outbound/OutboundScreen`                            | UC-09          | admin-5-outbound   | **built (phase 2)**              |
| `/dispatch`                                      | `Dispatch/DispatchScreen`                            | UC-12          | admin-6-dispatch   | **built (phase 2)**              |
| `/stocktake` · `/stocktake/$id`                  | `Stocktake/{StocktakeListScreen, StocktakeScreen}`   | UC-07          | admin-3-stocktake  | **built (phase 3 + list/start)** |
| `/adjustment`                                    | `Adjustment/AdjustmentScreen`                        | UC-08          | admin-9-adjustment | **built (phase 3)**              |
| `/quality`                                       | `Quality/QualityScreen`                              | UC-03          | admin-8-qc         | **built (phase 3)**              |
| `/products` · `/products/new` · `/products/$sku` | `Products/{ProductCatalogScreen, ProductEditScreen}` | UC-13          | admin-4-product    | **built (phase 4 + catalogue)**  |
| `/topology`                                      | `Topology/TopologyScreen`                            | UC-14          | admin-7-topology   | **built (phase 4)**              |

All nine designed admin screens are built. `/` redirects to `/stock` (the manager's landing).
Sidebar items with no screen yet (Movements, Partners) are dimmed and inert.

Remaining work (Gateway wiring, toasts, code-splitting, deferred screens, cleanup) is tracked in
[TODO.md](TODO.md).
