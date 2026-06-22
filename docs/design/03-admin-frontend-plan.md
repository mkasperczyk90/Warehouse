# Admin panel — front-end implementation plan

> The build plan for the **Admin panel** (`src/web/admin`): a browser-native React app for the
> manager/coordinator at a desk. It turns the nine `admin-*` [prototypes](prototypes/index.html)
> and the [design system](00-design-system.md) into a real React build, mirroring the conventions
> already set by the [Operator terminal](../../src/web/terminal/README.md). The decisions here are
> narrated in [blog #18](../blog/18-the-admin-panel-architecture.md) and recorded in
> [ADR-0004](../adr/0004-admin-panel-separate-spa.md) / [0005](../adr/0005-shared-design-tokens.md) /
> [0006](../adr/0006-mock-at-the-network-boundary.md).

## 1. Starting point — what the design already decided for us

The admin panel is a **deliberately different product** from the terminal, not the same UI re-skinned
(blog [#11](../blog/11-design-nfr-adr-and-design-system.md), [#12](../blog/12-from-design-system-to-screens.md)):

| | Terminal (`src/web/terminal`) | **Admin (to build)** |
|---|---|---|
| User | floor operator — gloves, cold, glare | manager / coordinator at a desk |
| Shape | one task per screen, scanner-first, tap ≥56 px | **dense tables, filters, bulk actions, master-data forms** |
| Theme | light **+** high-contrast (glare) | **light only** (HC does not apply) |
| Type scale | `.terminal` (body 20 px, tap 56 px) | `.admin` (body 14 px, tap 36 px) |
| Platform | React Native / Expo (web via RN-Web) | **plain React in the browser** |

What is **shared** is the ubiquitous language and the **status tokens** — colour encodes domain stock
status (`available / reserved / blocked / expired / in-transit`), never decoration. Those are the
load-bearing tokens and must mean the same thing in both front ends and on the event-storming board.

The nine admin screens are already designed as runnable HTML prototypes
(`docs/design/prototypes/admin-*.html`) — those are the **design source of truth**, exactly as the
`terminal-*.html` prototypes were for the terminal.

## 2. The governing principle — parity with the design system, not a new design

The terminal had to *port* `tokens.css` → `tokens.ts` because React Native has no CSS. The admin panel
runs **in the browser, so it uses [`tokens.css`](prototypes/tokens.css) directly** — no porting, no
drift. This is the single most important architectural decision and it gets its own record
([ADR-0005](../adr/0005-shared-design-tokens.md)): **one token layer for both front ends.**

```
docs/design/prototypes/tokens.css   ← source of truth (already exists)
   └── vendored into src/web/admin/src/shared/theme/tokens.css (synced, not forked)
```

Each `admin-N-*.html` prototype is treated as a finished layout spec and translated 1:1 into a React
component, preserving the DOM structure and class names (`.side`, `.kpi`, `.panel`, `.filters`,
`.badge--*`).

## 3. Technology stack

Recommendations with the reasoning, not a catalogue of options. The *significant* ones (separate SPA,
shared tokens, network-boundary mock) are recorded as ADRs; the library picks below are deliberately
**not** ADR-worthy (they are reversible per the ADR rules) but are justified here.

| Layer | Choice | Why |
|---|---|---|
| **Build** | **Vite + TypeScript**, React 19 | Standard for SPAs, instant HMR. React 19 matches the terminal (19.2). |
| **Routing** | **TanStack Router** | The terminal valued *typed routes* (`ROUTES.pack`). The admin is URL-driven (filters, selection live in query params) — TanStack's typed search params fit that exactly. Mainstream fallback: React Router 7. |
| **Server-state** | **TanStack Query** | The admin *is* a data app over the Gateway. Query gives cache, loading/error, refetch — and slots into "one seam, mock now → fetch later". |
| **Tables** | **TanStack Table** (headless) | Dense tables with sort / filter / pagination / bulk-select. Headless = we render our own `<table>` with the existing token classes; no imposed look. |
| **Forms** | **React Hook Form + Zod** | Master data (Products, Topology) and validations that mirror domain invariants (qty ≥ 0, reserve ≤ available). The Zod schema is the single source of the rules. |
| **Styling** | **CSS Modules + `tokens.css`** | Tokens already exist as CSS custom properties. CSS Modules scopes per-component with no CSS-in-JS runtime, and stays faithful to the prototypes. (Alternative: Tailwind v4 with `@theme` mapping the tokens.) |
| **Mock API** | **MSW (Mock Service Worker)** | Key move: the admin calls `fetch` from day one and MSW serves fixtures at the network layer. **No mock→real rewrite** — turn MSW off and the real Gateway is already there ([ADR-0006](../adr/0006-mock-at-the-network-boundary.md)). |
| **Icons** | **lucide-react** | The design doc flags that the admin sidebar still uses Unicode glyphs ("a next pass"). Lucide is inline-SVG `currentColor` — the same model as the terminal's `Icon`. |
| **i18n** | **react-i18next** (en / pl) | The domain is Polish (Wrocław). Larger surface than the terminal → react-i18next over a hand-rolled helper; keys kept consistent with the terminal. |
| **Client-state** | **Zustand** (sparingly) | Only where the URL is not the right home: the selected warehouse (WH-01), bulk selection. Most state = URL + Query. |
| **Tests** | **Vitest + React Testing Library**; **Playwright** (e2e, optional) | The same MSW handlers back dev *and* tests. |

## 4. Folder structure (mirrors the terminal's conventions)

```
src/web/admin/
├── index.html
├── package.json
├── vite.config.ts            # alias @/* → src/*
├── tsconfig.json
├── README.md
└── src/
    ├── main.tsx              # React root + providers (Query, Router, i18n)
    ├── core/
    │   ├── api/
    │   │   ├── client.ts     # THE single seam: a fetch wrapper on the Gateway
    │   │   └── queryClient.ts
    │   └── mocks/            # MSW: browser.ts + handlers/ + fixtures/
    ├── features/             # MAIN WORK AREA — feature-sliced
    │   ├── Stock/            # UC-05 · admin-1-stock   (manager landing)
    │   ├── Inbound/          # UC-01 · admin-2-asn
    │   ├── Stocktake/        # UC-07 · admin-3-stocktake
    │   ├── Products/         # UC-13 · admin-4-product
    │   ├── Outbound/         # UC-09 · admin-5-outbound
    │   ├── Dispatch/         # UC-12 · admin-6-dispatch
    │   ├── Topology/         # UC-14 · admin-7-topology
    │   ├── Quality/          # UC-03 · admin-8-qc
    │   └── Adjustment/       # UC-08 · admin-9-adjustment
    ├── shared/
    │   ├── theme/
    │   │   └── tokens.css    # vendored from the design source of truth
    │   ├── layout/           # AppShell, Sidebar, TopBar
    │   ├── ui/               # StatusBadge, QuantityWithUnit, DataTable, FilterBar, KpiCard, FormField…
    │   └── i18n/             # en.ts, pl.ts, i18n.ts
    └── navigation/
        └── routes.ts         # typed path map (same idea as the terminal)
```

Each feature is self-contained — the same convention as the terminal:

- `XScreen.tsx` — the view
- `x.model.ts` — types + fixtures + **Query hooks** (`useStockRows()`)
- `index.ts` — public API (cross-feature imports go only through it)

## 5. Data architecture — the same seam as the terminal, but on `fetch`

The terminal's `read<T>(resource, fixture)` returns a fixture synchronously. The admin goes one step
further — **async from the start**:

```
component → Query hook (x.model.ts) → core/api/client.ts (fetch /api/…)
                                          │ (dev / test)
                                          ▼
                                       MSW intercepts → returns fixture
                                          │ (prod, MSW off)
                                          ▼
                                       real Gateway (.NET YARP)
```

The payoff: real loading/error states, optimistic updates for writes (stocktake, adjustment — every
write is a reason-bearing entry in the immutable ledger), and idempotency — all real from day one,
because there is no magic synchronous mock to throw away later
([ADR-0006](../adr/0006-mock-at-the-network-boundary.md)).

## 6. Shared components — `shared/ui`

**Reused from the design (the same as the terminal, ported to the web):**

- `StatusBadge` — dot + label pill; status is never colour alone (a11y).
- `QuantityWithUnit` — never a bare number; always a unit, tabular numerals.

**Admin-specific (from [00-design-system.md](00-design-system.md) §3):**

- `DataTable` + `FilterBar` — the core of the app (TanStack Table + the `.panel / .filters / table` classes).
- `KpiCard` — the KPI cards across the top (`.kpi`, `.warn` variant for blocked / expiring).
- `Form` / `MasterDetail` — master-data forms (RHF + Zod).
- `Toast` keyed by error code — surfacing `DomainException` by its stable code (the same language the API returns).

**Layout shell (`shared/layout`):**

- `Sidebar` — a **Today** (worklist) landing item on top, then groups: Inventory / Logistics /
  Master data; the active item carries a status-coloured left border. Only **Partners** is dimmed (designed,
  not yet built); **Movements** is now built and live under Inventory.
- `TopBar` — breadcrumb + warehouse selector (WH-01 Wrocław ▾) + the user avatar.

## 7. Screen map and routing

| Route | Feature | UC | Prototype | Actor |
|---|---|---|---|---|
| `/` → `/today` | `Today/TodayScreen` | — | admin-10-worklist | All (landing) |
| `/stock` | `Stock/StockScreen` | UC-05 | admin-1-stock | Manager |
| `/movements` | `Movements/MovementsScreen` | UC-05 | *(no prototype — added since)* | Manager |
| `/inbound` | `Inbound/InboundScreen` | UC-01 | admin-2-asn | Coordinator |
| `/stocktake` | `Stocktake/StocktakeScreen` | UC-07 | admin-3-stocktake | Manager |
| `/products` | `Products/ProductsScreen` | UC-13 | admin-4-product | Manager |
| `/outbound` | `Outbound/OutboundScreen` | UC-09 | admin-5-outbound | Coordinator |
| `/dispatch` | `Dispatch/DispatchScreen` | UC-12 | admin-6-dispatch | Coordinator |
| `/topology` | `Topology/TopologyScreen` | UC-14 | admin-7-topology | Manager |
| `/quality` | `Quality/QualityScreen` | UC-03 | admin-8-qc | Inspector |
| `/adjustment` | `Adjustment/AdjustmentScreen` | UC-08 | admin-9-adjustment | Manager |

The landing changed from `/stock` to the **worklist** (`/today`) — the "open question" of §11 was resolved in
favour of the worklist as default, with the stock view one click away. Beyond these list screens, nested
**detail / create** routes carry the actions: `/stock/$id`, `/inbound/$id/receiving`, `/stocktake/$id`,
`/adjustment/$itemId`, `/products/new`, `/products/$sku`. Filter and selection state lives in typed search
params in the URL (deep-linkable, refresh-safe).

## 8. Build phases

**Phase 0 — Foundation (app skeleton).** Vite + TS + React, `@/*` alias, providers (Query / Router /
i18n), `tokens.css` wired in, MSW configured, `core/api/client.ts`, `AppShell` + `Sidebar` + `TopBar`,
routing with the nine-path map (placeholders).

**Phase 1 — Vertical slice: Stock view (UC-05).** The most important screen (the manager's landing and
where exceptions surface). It builds `DataTable`, `FilterBar`, `KpiCard`, `StatusBadge`,
`QuantityWithUnit` — i.e. **almost all of `shared/ui`** off the back of one screen. The reference screen
for the rest.

**Phase 2 — Coordinator logistics.** Inbound (ASN) → Outbound → Dispatch board. Dispatch introduces a
board/kanban view (status columns).

**Phase 3 — Exceptions & ledger writes.** Stocktake → Adjustment → QC worklist. Here come the
reason-bearing forms (RHF + Zod), optimistic mutations, and the invariant guard (qty ≥ 0).

**Phase 4 — Master data.** Products (master-detail form) → Topology (location / room tree).

Each phase is a working, clickable increment (it grows per slice, matching the [roadmap](../PLAN.md#roadmap)).

## 9. Deliberately out of scope (for now)

- The high-contrast theme (that's an operator driver, not a desk one — terminal only).
- Auth / identity (generic, off-the-shelf — out of pass 1, per blog #11).
- Analytics / BI dashboards.
- External-actor self-service portals (the API / events are their interface for now).
- Charts — KPIs are simple cards; when needed → recharts / visx.

## 10. Decisions to confirm

1. **Router** — recommended: TanStack Router (typed search params shine for filters); React Router 7 is
   the equally-fine mainstream fallback if the team already knows it.
2. **`tokens.css`** — vendor a copy into the app at build time (safer: the app does not depend on
   `docs/`) vs reference the file in `docs/design/prototypes/` directly. The copy needs a sync rule.
3. **Styling** — CSS Modules (recommended, minimal) vs Tailwind v4 mapping the tokens in `@theme`.

## 11. Post-build review — what the gap-closing pass delivered

A usability pass (from a warehouse-ops perspective) surfaced gaps where the build — and in places the
design itself — stopped short of the use cases. **Those gaps are now closed.** The full, prioritised
checklist lives in the app's [`TODO.md`](../../src/web/admin/TODO.md); the design-level framing is here.

**The missing "front doors" — now built.** The prototypes drew the *detail / edit* of these flows but not
the *list* or the *create*; each now has its front door:

- **UC-13** — a **product catalogue list** (browse / search / sort) and **+ New product** (`/products`,
  `/products/new`, `/products/$sku`). This is the gap that started the review.
- **UC-01** — **create an ASN** and **assign a dock slot** (the ASN screen now has both, plus
  `/inbound/$id/receiving`). The dock-slot calendar remains an open PO question in
  [PLAN](../PLAN.md#open-questions-for-po--clients).
- **UC-09** — **create an outbound order**; **UC-12** — **assign a carrier** (the board action is live, with
  split / hold).
- **UC-07** — **start / schedule a stocktake** plus a list of stocktakes (`/stocktake`, `/stocktake/$id`).
- **UC-14** — **add** a room / location (Topology now edits *and* adds).
- **UC-05** — a **Movements** ledger view (`/movements`) was added beyond the original nine screens.

**Cross-cutting UX — now in place** (see [`TODO.md`](../../src/web/admin/TODO.md)): clickable +
keyboard-accessible rows with row actions, sort + pagination on every `DataTable`, confirm-with-reason on
irreversible ledger / QC posts, and a **reason captured on the QC decision** (see
[exceptions](02-exceptions.md)). Still open: **CSV export / print**, an **undo window**, and a global
**Toast keyed by error code** — all listed in `TODO.md`.

**The work-queue landing is now the default.** The **Today / Worklist** ("what needs me now" — deliberately
*distinct* from the deferred BI dashboards in §9) is specced as `admin-10` and built at `/today`, where
`/` now redirects: actionable queues + sidebar counters, each linking to the screen that clears it.
**Global search** is built (`src/web/admin/src/features/Search`). The PO question of §7 — worklist vs. stock
as the landing — was resolved in favour of the worklist; per-role tailoring of which queues show remains
open. The only sidebar item still designed-but-unbuilt is **Partners** (master data).
