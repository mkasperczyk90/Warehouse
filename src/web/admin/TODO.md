# Admin panel — remaining work

The nine designed screens are built (phases 0–4 of the
[plan](../../../docs/design/03-admin-frontend-plan.md)) and the app has since grown past them
(Movements, Auth/badge sign-in, Profile, Today/Worklist, Search, Warehouses) and is now wired to the
**real backend** feature-by-feature (going live = MSW off — see §1). `tsc` clean, `vite build` clean,
unit tests green, plus a full `tests/e2e/admin` playwright-bdd suite — both run in CI
(`frontend.yml` + `e2e.yml`). This is the backlog of what's left.

Ordered roughly by value. Nothing here blocks demoing the panel against the MSW mock today.

## 0. Product & UX gaps (warehouse-ops review)

A usability pass from the perspective of someone who would actually work the floor/desk. The current
build is a solid **data browser**; these turn it into a **tool you can work a shift in**. Two systemic
gaps explain most of the individual items:

- **Screens are islands with no "front door."** Several screens assume you already know the subject and
  jump straight to a single item — there is no list to find it from, and tables don't lead anywhere.
- **No work queues.** The landing is the stock table, not "what needs me now" (QC backlog, partial
  orders, expiring stock, late dispatches).

### P0 — front doors & safety (highest value)

- [x] **Product catalog list** — _done._ `/products` is now a searchable, category-filterable catalogue
      (`ProductCatalogScreen`); rows open `/products/$sku` (edit), `+ New product` → `/products/new`
      (create, stateful in the mock). **CSV bulk import** is now wired too — _done_ (see §1 Products).
- [x] **Front door for Adjustment & Stocktake.** _Done._ Adjustment is reachable from a Stock row →
      `/stock/$id` drill → **Adjust stock** → `/adjustment/$itemId`. Stocktake is now a **list**
      (`/stocktake`) with a **Start count** dialog that creates a counting stocktake (stateful mock) and
      opens it; rows open `/stocktake/$id` (review or counting-in-progress). Minor follow-up: a standalone
      SKU/location picker for ad-hoc adjustments not started from a Stock row.
- [x] **Clickable rows + row actions.** _Done._ `DataTable` supports `onRowClick`; the product catalogue
      (row → edit), Stock (row → `/stock/$id` drill) and the Stocktake list (row → review) use it. The
      stock drill carries the row actions: **Adjust** (→ ledger), **Move** (with the environment-
      compatibility invariant enforced — a cold item can't move to ambient) and **Block** (reason-bearing
      → quarantine), all via `shared/ui/Modal`.
- [x] **Work-queue landing + nav counters.** _Built_ — `features/Today` (`/today`, now the `/` landing),
      [`admin-10` prototype](../../../docs/design/prototypes/admin-10-worklist.html) +
      [blog #20](../../../docs/blog/20-a-worklist-not-a-dashboard.md): attention cards + worklist panels
      (QC holds, expiring ≤7 d, partial orders, inbound today, stocktakes to approve), each linking to the
      screen that clears it; **sidebar count badges** mirror it; all from one `GET /api/worklist` over
      existing data (QC count is live). Open PO calls (flagged in `PLAN.md`): keep `/` → worklist as the
      default landing? per-role queues?
- [x] **Global search / command bar** — _done._ A search box in the top bar (`features/Search`,
      `GET /api/search`) matches across products, stock (SKU/batch/location), ASN, orders, shipments and
      locations, and jumps to the hit. Follow-ups: deep-select for ASN/order/shipment (today they land on
      the section, not the specific record — now **unblocked** by URL-driven selection §2: jump to
      `/inbound?selected=<id>` etc.), LPN once modelled, and keyboard arrow navigation in the results.
- [~] **Confirm + undo on irreversible posts.** QC Release/Reject **and** the ledger adjustment now go
  through a confirm **dialog** (reusable `shared/ui/Modal`) that summarises the change. Remaining: a
  short **undo** window after an irreversible post.
- [x] **Reason (+ note) on QC Release/Reject.** _Done._ The decision dialog requires a reason (Release:
      passed / lab OK / temp OK; Reject: failed temp / damaged / expired / lab fail) plus an optional
      note, sent with the command — closes the audit gap flagged in
      [`02-exceptions.md`](../../../docs/design/02-exceptions.md).
- [x] **Pagination, sort, and a row count** — _done._ `DataTable` now sorts on header click (TanStack
      `getSortedRowModel`) and paginates with a `1–N of M` row count + Previous/Next (default page size 25);
      every table that uses it gets this for free. Follow-up: a page-size selector and **CSV export** (the
      export/print piece is still open under §2).

### P1 — actions & operational hygiene

- [ ] **Export CSV + print** — the warehouse runs on Excel and paper: export tables, print ASN/GRN, count
      sheets, waybills/labels, packing slips.
- [~] **Activate the inert prototype actions.** _Done:_ **New ASN** and **New order** (create dialogs with
  a dynamic lines editor → stateful create → the record appears and is selected) and **Assign carrier**
  (carrier + pickup → the shipment moves from "awaiting carrier" to "assigned" on the board).
  Release-to-wave, print waybill, and **Recount selected** (re-issues a blind count) are now wired too.
  Topology location **Edit**, **Add location** and **Add room** (new node in the tree, grouped under
  its warehouse) are wired too. **No prototype actions remain inert.**
- [x] **Logistics — deeper flows.** _Done:_ dock-slot scheduling, split/hold on partial orders, mark
      arrived (Announced→Arrived), release to wave (Reserved→Picking), Dispatch board transitions (Send
      pickup notice / Mark collected — advance a shipment column to column with badge + tracking updates),
      **cancel order** (reservations released), **unknown-SKU resolution** (map a flagged ASN line to the
      correct SKU/product), **print waybill** (dispatched cards), and a **carrier filter** on the board.
      Both ends now walk their full happy-path lifecycle. Plus: unknown-SKU resolution can **create the
      product in the catalogue** (not just map to an existing one), Outbound order lines drill into **ATP
      by location** (the "why is this partial" view), and an Arrived ASN links to a read-only
      **receiving-progress** view (`/inbound/$id/receiving` — lines received vs expected + a progress bar,
      the coordinator monitoring UC-02). **Logistics is functionally complete for its use cases.**
- [ ] **Inbound:** dock-slot scheduling (there's "slot pending" but no calendar); resolve the unknown-SKU
      flag ("create product / map to existing"); link to receiving-in-progress status.
- [ ] **Outbound:** the split/hold decision UI the note promises ("coordinator decides"); ATP drill
      ("why is this line partial — what's available where").
- [ ] **Stock:** an expiry/FEFO list sorted by best-before with date windows (≤7/14/30 d) — FEFO is the
      whole point and today it's just a pill with no date; multi-UoM display (ea / case / pallet);
      LPN / handling-unit visibility (the domain has `HandlingUnit`).
- [ ] **Stocktake:** schedule counts (cycle vs full), assign operators, live progress ("28 of 42
      locations"), variance **valuation in money** (managers care about the value of shrinkage).
- [x] **Multi-warehouse:** _done._ The TopBar warehouse switcher (`shared/warehouse/WarehouseContext`)
      is real — picking a site re-points the api client (`X-Warehouse-Id` header, `setActiveWarehouse`)
      and invalidates all queries so every screen refetches scoped to it; choice persists in
      `localStorage`. Operational data (stock+KPIs, movements, ASN, orders, dispatch, QC, stocktake,
      worklist, search) is filtered server-side in the MSW handlers (WH-01 + a WH-02 Poznań dataset);
      master data (products, topology) stays cross-warehouse by design. A user menu (My profile / language
      / **Sign out**) is in the TopBar too. The switcher list now speaks the **real** backend contract —
      `useWarehouses()` calls `topology/warehouses` (Topology `WarehouseSummaryDto`, gateway `/api/topology`),
      so going live is turning MSW off. Follow-up: the whole topology screen (tree/room **reads + writes**)
      now speaks the real backend too (see §1 Topology); still open are `occupancy` (needs Inventory) and
      scoping `topology`/`locations` move-targets per site.
- [ ] **Audit / history views** — the adjustment banner says "audited" but there's nowhere to see the
      history (who adjusted, who released QC, when).

### P2 — depth & polish

- [ ] **Topology:** expand/collapse + search the tree (96 locations per room), capacity/occupancy heatmap.
- [ ] **Adjustment:** photo upload (prototype mentions "photo attached"); per-item adjustment history.
- [ ] **Keyboard shortcuts** for desk power users (the admin is keyboard-first by design — plan §1).
- [ ] **Saved filters/views**, loading skeletons, richer empty states.
- [ ] **Notifications/alerts** — expiring stock, QC backlog, missed pickup windows.

> Overlap note: global search, pagination/export, confirm/undo and the toast system also appear under
> §2–§4 below as the cross-cutting mechanisms; the items here frame them from the user's workflow.

## 1. Wire the real Gateway (the big one)

The app already calls `fetch` through the single `src/core/api/client.ts` seam; MSW serves fixtures
(ADR-0006). Going live is **turning the worker off**, not a rewrite.

- [ ] Gate `enableMocking()` in `src/main.tsx` on an env flag (e.g. `import.meta.env.VITE_API_MOCKING`)
      instead of always-on; default it off for production builds.
- [ ] Point `GATEWAY` (`src/core/api/client.ts`) at the real `.NET` YARP Gateway base path / configure a
      Vite dev proxy so `/api` reaches it locally.
- [ ] Reconcile each feature's wire contract with the real endpoints as backend slices land — keep the
      `x.model.ts` types as the contract; adjust fixtures/handlers to match, then remove handlers per
      endpoint as the real one ships. - [x] **Products / Catalog (UC-13)** — _wired._ The catalogue (list + category filter, product card,
      define, rename, change-storage) speaks the real MasterData endpoints `catalog/products[/{sku}…]`
      (Catalog `ListProducts`/`GetProduct`/`DefineProduct`/`Rename`/`ChangeStorage`, gateway `/api/catalog`)
      — the FE `product.model.ts` types are byte-for-byte the backend DTOs/commands, so MSW was already on
      the same paths. A new **`CatalogSeeder`** (dev-only, idempotent) makes the real list return the six
      demo cards, replaying them through the import slice so the resulting `ProductDefinedV2` events also
      seed the Inventory/Logistics replicas. **CSV bulk import** landed as a real backend slice:
      `POST catalog/products/import` (Catalog `ImportProductsHandler` → per-row `DefineProduct`, isolating
      bad rows) returns `{ created, failed[] }`; the catalogue's **Import CSV** button parses the file
      client-side (rejecting malformed-numeric rows before the wire) and shows a per-row result summary.
      Going live for Products is now just turning MSW off. - [x] **Stock / Inventory projection** — _wired._ The Stock view (rows, KPIs, item drill + movements,
      ATP-by-SKU) and its row actions (move/block) now speak the real Warehousing endpoints
      `inventory/stock/*` + `inventory/locations` (Inventory read model `StockOverviewHandler`, gateway
      `/api/inventory`). MSW handlers were repointed to the same paths so dev/tests stay green against an
      identical contract; the backend dev seed (`InventorySeeder` + `TopologySeeder`, WH01/WH02) makes the
      real endpoints return data. Going live for Stock is now just turning MSW off. - [x] **Movements (ledger)** — _wired._ `/api/inventory/movements` projects the immutable
      `stock_movements` ledger (Inventory `MovementsHandler`) into the admin Movements view — signed qty,
      UI category + label per movement type, warehouse-scoped. MSW repointed to the same path; the seed
      now routes stock in the production way (goods receipt → put-away), so the ledger shows two real
      categories. **Inventory read-side is now backend-real end to end.** - [x] **Adjustments (UC-08)** — _wired._ `inventory/adjustments/draft[/{id}]` (draft from a real stock
      item) + `POST inventory/adjustments` (Inventory `AdjustStockHandler` → `StockItem.AdjustTo` → one
      signed ledger entry). Domain invariants enforced (never below zero/allocated, no-op rejected) and
      surfaced as `DomainException` → HTTP. MSW repointed to the same paths. - [x] **Stocktake (UC-07)** — _wired._ `inventory/stocktake` (list), `/{id}` (review detail), start,
      approve, recount. The `Stocktake` aggregate is now persisted (new `stocktakes` +
      `stocktake_count_lines` tables, EF config + migration). **Approve reconciles to the ledger**: each
      accepted difference line posts a `StockItem.AdjustTo` adjustment, so a count's shortages become real
      movements (and show up in the Movements view). Seed ships a review-ready count (cold room, −12) and a
      completed one (freezer). Recount is a no-op ack until the terminal counting workflow lands; the start
      flow opens a blind count over the warehouse's locations (operators record the counts). - [x] **QC / Quality holds (UC-03)** — _wired._ `inventory/qc/batches` (quarantine worklist, grouped by
      batch from the held stock) + `POST inventory/qc/{batchId}/{release|reject}` with reason+note. The
      decision is batch-level (Inventory `QcDecisionHandler`): release lifts the `Batch` hold and frees its
      quarantined `StockItem`s (→ Available, visible in Stock); reject blocks the batch and its stock.
      Seed ships two held batches (cheese + a milk lot on QC hold). First move beyond the Inventory
      read-models into batch-quality writes. - [x] **Inbound deliveries (UC-01/UC-02)** — _wired._ The ASN list, detail (header + dock slot +
      lines) and read-only receiving-progress view speak the real Logistics endpoints
      `logistics/deliveries[/{id}…]` (`ListDeliveries`/`GetDelivery`, gateway `/api/logistics`) — the FE
      `inbound.model.ts` DTOs are byte-for-byte the backend `DeliverySummaryDto`/`DeliveryDto`/
      `DeliveryLineDto`, and MSW already serves the same paths. Create/dock-slot/arrival post the real
      commands. A new **`LogisticsSeeder`** (dev-only, idempotent) makes the real list return three
      announced deliveries (WH01/WH02) over seeded catalog SKUs. Note: the `lines/{id}/resolve`
      (unknown-SKU) path stays **MSW-only and inert** — the backend rejects unknown SKUs at announce, so
      no line is ever flagged; it never fires against the real backend. - [x] **Outbound orders (UC-09…UC-12)** — _wired._ The order list, detail (header + ship-to +
      lines, reservation view derived from status) and the actions (create, decision/split-hold, release
      to picking, cancel) speak the real `logistics/orders[/{id}…]` endpoints — FE `outbound.model.ts`
      DTOs equal the backend `OrderSummaryDto`/`OrderDto`/`OrderLineDto`. `LogisticsSeeder` ships two
      placed orders. **Party identity reconciled:** supplier/customer/carrier role refs are now carried
      as an opaque **string** (`PartyRoleRef(string)`, commands + `DeliveryDto.SupplierRoleId`/
      `OrderDto.CustomerRoleId` + the two integration events, EF columns `uuid → text` via the
      `PartyRoleRefAsString` migration), so the desk's free-text supplier/customer flows end-to-end and
      the create dialogs work against the real backend (a Partner picker yielding a real Party-role id is
      the eventual replacement). The split/hold decision still needs a partially-reserved order from the
      reservation saga to exercise end-to-end (the seed places orders in `Created`). - [x] **Topology tree + room (UC-14)** — _wired (read + write)._ A Warehousing read model serves the
      FE's flat-tree contract: `GET topology/tree` (`GetTopologyTreeHandler` → warehouse + room nodes) and
      `GET topology/room/{id}` (`GetRoomHandler` → room detail + locations), projecting the seeded
      `WarehouseSite` aggregates. The FE `topology.model.ts` `TopologyNode`/`RoomDetail`/`LocationRow`
      types equal the backend DTOs (room node id is `"{warehouseCode}:{roomCode}"`, round-tripped
      opaquely; MSW serves the same shape, so dev/tests stay green). **Writes wired too:** thin flat-path
      endpoints split the composite room id and delegate to the warehouse-scoped handlers — add-room
      (`POST topology/rooms`, FE room-type key → enum), save-room env (`POST topology/room/{id}` →
      `ChangeRoomEnvironment`; room type is fixed so the FE type field is display-only there), add-location
      (→ `AddLocation`) and a **new `ChangeLocationCapacity` slice** for edit-location (re-announces the
      same `LocationDefinedV1` upsert event, so Inventory's `LocationSnapshot` re-rates). The add-room
      dialog now collects a room **code** (rooms are coded in the domain) and sources its warehouse list
      from the live tree (real codes), so it posts a payload the backend accepts. `occupied` still reads
      **"—"**: occupancy is Inventory's stock, not Topology, so it stays blank until a stock-occupancy
      projection is composed in (the P2 heatmap). - [x] **Worklist + Search (cross-context BFF)** — _wired._ These two need data from several services
      at once, so they live as a **BFF in the Gateway** (the only place allowed to fan out): minimal-API
      endpoints `GET /api/worklist` and `GET /api/search` that resolve the service clients by Aspire
      service discovery (inheriting the standard resilience handler), forward `X-Warehouse-Id`, fetch in
      parallel **best-effort** (a failing source leaves its section empty), and project with pure,
      unit-tested mappers (`WorklistMapper`/`SearchMapper`, new `Warehouse.Gateway.Tests`). Worklist
      aggregates QC + expiring stock (≤7 d) + partial orders + inbound + stocktakes-to-approve; Search
      spans products, stock, ASN, orders and locations (a new flat `GET topology/locations` read backs the
      location hits) **and shipments** (off the dispatch board, below). The FE contract is unchanged
      (`useWorklist`/`useGlobalSearch` already hit these paths; MSW keeps serving them in dev), so going
      live is turning MSW off. - [x] **Dispatch board (UC-12)** — _wired, with a domain rework._ The board's four columns
      (awaiting carrier → carrier assigned → pickup notice sent → dispatched) are now real lifecycle
      states: the **`Shipment` aggregate** gained `AwaitingCarrier`/`CarrierAssigned` states, a nullable
      carrier + `Pickup`, and `CreateAwaitingCarrier`/`AssignCarrier`/`SendPickupNotice` transitions
      (migration `ShipmentCarrierLifecycle`). `MarkPacked` now **opens the shipment** (it lands on the
      board); the admin walks it column by column via `GET /dispatch/board` + `POST /dispatch/{id}/assign` + `/advance` (Logistics `GetDispatchBoard`/`AssignCarrier`/`AdvanceShipment`, gateway route
      `/api/dispatch` → logistics); the terminal's `ConfirmDispatch` fast-forwards the same states in one
      shot. `LogisticsSeeder` ships packed orders with shipments across the columns, and the doc model now
      carries the `Shipment.Status` state machine. FE/MSW contract unchanged — going live is turning MSW
      off. - [~] **Auth / Identity (Keycloak)** — _wired; needs a local run to verify the Keycloak path._ The
      deferred IdP decision is made: **self-hosted Keycloak** (a container in the AppHost) with a custom
      **badge Direct-Grant authenticator** (Java SPI, `src/Identity/keycloak-badge-authenticator`) so the
      desk's badge-scan issues real JWTs. The gateway **brokers** sign-in (`POST /api/auth/login` →
      Keycloak token endpoint, confidential client secret server-side → returns `{ accessToken, user }`)
      and **validates** every other call (`AddJwtBearer`, `RequireAuthorization` on the BFF + proxy);
      services trust the gateway (blog #11). The FE seam attaches `Authorization: Bearer` (token persisted
      for refresh-safe sessions); `AuthContext` stores token + user; MSW returns the same shape with a fake
      token, so **dev/tests are unchanged** and going live is turning MSW off. **Verified in-session:** the
      .NET (gateway broker/claims, AppHost wiring) builds + `AuthClaims` unit tests, and the FE
      (typecheck + tests). **Not verifiable in-session** (needs Maven + Docker): the Java jar build, the
      realm import, and the end-to-end token flow — see `src/Identity/README.md` for the local steps.
      Follow-up: pin a `ValidIssuer` once a stable public Keycloak URL is in front; add role-based
      authorization policies; per-service validation for zero-trust.

## 2. Cross-cutting UX the design system calls for

- [x] **`Toast` keyed by error code** (design system §3, `00-design-system.md`) — _done._
      `shared/toast` adds a `ToastProvider` + `useToast()` hook (`success`/`error`/`apiError`/`toast`).
      `formatApiError` maps `ApiError.code` → `errors.<code>` in the active language (EN+PL, seeded with
      the real `DomainException` codes), falling back to the API message then a generic string. **Every
      failed mutation auto-toasts** via the QueryClient `MutationCache.onError` (per-mutation `onError`,
      e.g. QC's optimistic rollback, still runs). Tested in `ToastProvider.test.tsx`. Follow-up: migrate
      the per-screen success/inline banners to `useToast().success(...)` screen by screen.
- [x] **URL-driven selection & filters** — _done._ Typed TanStack Router **search params** back the
      master-detail selection on Inbound / Outbound / Topology (`?selected=…`, shared
      `validateSelectionSearch` in `navigation/search.ts`) and the Stock filters (`?q=&pill=`,
      `stock.search.ts`), so those views are deep-linkable and refresh-safe. The URL seeds the state and
      every change mirrors back to it; local state still drives rendering between writes (keeps the
      router-free component tests green — they stub `useSearch`). Validators live in leaf modules so
      `router.tsx` wires `validateSearch` without pulling a lazy screen into the initial chunk.
      Follow-up: the global search can now deep-select a record (land on `/inbound?selected=<id>` etc.).
- [ ] **Optimistic writes beyond QC.** Adjustment / Stocktake post and show a banner; consider optimistic
      cache updates + rollback (the QC pattern in `qc.model.ts`) where it improves feel.

## 3. Performance & build

- [x] **Route-level code-splitting** — _done._ Every screen route in `router.tsx` now uses
      `lazyRouteComponent(() => import('@/features/X'), 'XScreen')`, so each screen is its own chunk and
      the initial JS dropped from **688 kB → 430 kB** (the >500 kB Vite advisory is gone). The shell stays
      eager; `defaultPreload: 'intent'` warms a screen's chunk on hover/focus so navigation still feels
      instant.
- [ ] Consider a `manualChunks` split for the vendor libs (TanStack, MSW out of prod) if needed.

## 4. Quality / tooling

- [x] **ESLint + Prettier** — _done._ Flat `eslint.config.js` (ESLint 9 + typescript-eslint 8 +
      react-hooks + react-refresh) and `.prettierrc.json` (2-space, single quotes, 100 cols). Scripts
      `lint` / `format` / `format:check`; the whole app is Prettier-clean (vendored `tokens.css` is
      ignored per ADR-0005). CI runs lint + format check for admin (`frontend.yml`). `npm run lint` is
      green (0 errors; 4 benign Fast-Refresh/exhaustive-deps warnings).
- [x] **CI** — _done._ `.github/workflows/frontend.yml` runs `typecheck` + `test:run` (JUnit + a
      per-test Check report) + `build` for `src/web/admin` (matrix with the terminal); MSW backs the tests.
- [~] **a11y pass** — _done:_ clickable `DataTable` rows are now keyboard-operable (`role="button"`,
  `tabIndex`, Enter/Space) with a focus ring. _Remaining:_ `aria-current` on the active sidebar link,
  focus management on master-detail selection, full color-contrast audit (status tokens already AA).

### UX polish (from the live design review)

- [x] **Search bar shifted with the breadcrumb** — the centred command bar jumped as the breadcrumb
      changed length. Fixed: the breadcrumb now has a fixed 260px width (ellipsis), so the search stays put.
- [x] **QC naming unified** — sidebar / worklist card / queue all say **"Quality holds"** (was "QC
      worklist" / "QC holds" / "Quality holds"). Page heading stays the descriptive "Batches in quarantine".
- [x] **Tables clipped at 1280** — `DataTable` now wraps its `<table>` in an `overflow-x:auto` scroll
      container, so wide tables scroll within their panel instead of clipping (Inbound/Outbound master-detail).
- [ ] **Kanban columns unequal height** (Dispatch) — make columns `min-height`/stretch so the grey panels align.
- [ ] **Pagination footer on tiny tables** — hide it when `total ≤ pageSize` (noise on embedded line tables).
- [x] **E2E** — _done, beyond a smoke._ A full playwright-bdd suite lives at `tests/e2e/admin` (19 `.feature`
      files: login, navigation, products, stock, stock-item, adjustment, stocktake, quality, inbound,
      outbound, dispatch, movements, search, topology, warehouse, today, profile, language) and runs in CI
      (`.github/workflows/e2e.yml`, admin + terminal matrix).
- [ ] More test depth where logic is thin today: Stock empty-state, error states per screen, i18n
      language switch.

## 5. Deferred / not-yet-designed screens

These have **no prototype yet** — they need design first (`docs/design`), then a slice here. Their
sidebar items are intentionally dimmed and inert.

- [x] **Movements** (Inventory) — _built_ (no prototype existed): `/movements` is a read-only view of the
      immutable movement **ledger** (ADR-0002) — date / type / product / location / signed qty / reference,
      with type pills + search + pagination/sort. The sidebar item is enabled. **Inventory is complete.**
- [ ] **Partners** (Master data) — no prototype; external-actor portals are deferred (blog #12).
- [x] **Auth / identity UI** — _built_ (badge-scan sign-in). `features/Auth` (`LoginScreen`) +
      `shared/auth/AuthContext` gate the app in `App.tsx`: an unauthenticated desk sees the badge screen;
      a scanned badge resolves to a user via `POST /api/auth/login` and opens the app on the user's
      default warehouse. `features/Profile` (`/profile`) adds identity + editable prefs (phone, default
      warehouse, language). Still mock-only — real token/session attaches at the seam (§1).
- [ ] Out of scope by design for now: analytics/BI dashboards, external self-service portals, the
      terminal's high-contrast theme (desk app is light-only).

## 6. Housekeeping

- [x] Remove dead code: `PlaceholderScreen.tsx` (+ `.module.css`) — _deleted_ (all routes are real now).
- [ ] `tokens.css` sync rule: it's vendored from `docs/design/prototypes/tokens.css` (ADR-0005). Add a CI
      check (or a small script) that fails if the vendored copy drifts from the source.
- [x] Per-screen identity in `TopBar` is no longer hard-coded — it reads the signed-in user from
      `AuthContext` (name, initials, role); the demo badges cover manager / coordinator / inspector.
- [x] Every prototype button that was inert (New ASN / New order / Assign carrier / Recount selected /
      location Edit / Add location / **Add room** …) is now wired.

---

_Reference: build [plan](../../../docs/design/03-admin-frontend-plan.md) ·
[architecture blog #18](../../../docs/blog/18-the-admin-panel-architecture.md) · ADRs
[0004](../../../docs/adr/0004-admin-panel-separate-spa.md)–[0007](../../../docs/adr/0007-vertical-slices-in-application-layer.md)
· app [README](README.md)._
