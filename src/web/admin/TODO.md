# Admin panel — remaining work

The nine designed screens are built (phases 0–4 of the
[plan](../../../docs/design/03-admin-frontend-plan.md)); this is the backlog of what's left. Status as
of this build: 9/9 screens, 27 tests passing, `tsc` clean, `vite build` clean.

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

- [x] **Product catalog list** — *done.* `/products` is now a searchable, category-filterable catalogue
      (`ProductCatalogScreen`); rows open `/products/$sku` (edit), `+ New product` → `/products/new`
      (create, stateful in the mock). Remaining: **CSV bulk import** of master data.
- [x] **Front door for Adjustment & Stocktake.** *Done.* Adjustment is reachable from a Stock row →
      `/stock/$id` drill → **Adjust stock** → `/adjustment/$itemId`. Stocktake is now a **list**
      (`/stocktake`) with a **Start count** dialog that creates a counting stocktake (stateful mock) and
      opens it; rows open `/stocktake/$id` (review or counting-in-progress). Minor follow-up: a standalone
      SKU/location picker for ad-hoc adjustments not started from a Stock row.
- [x] **Clickable rows + row actions.** *Done.* `DataTable` supports `onRowClick`; the product catalogue
      (row → edit), Stock (row → `/stock/$id` drill) and the Stocktake list (row → review) use it. The
      stock drill carries the row actions: **Adjust** (→ ledger), **Move** (with the environment-
      compatibility invariant enforced — a cold item can't move to ambient) and **Block** (reason-bearing
      → quarantine), all via `shared/ui/Modal`.
- [x] **Work-queue landing + nav counters.** *Built* — `features/Today` (`/today`, now the `/` landing),
      [`admin-10` prototype](../../../docs/design/prototypes/admin-10-worklist.html) +
      [blog #20](../../../docs/blog/20-a-worklist-not-a-dashboard.md): attention cards + worklist panels
      (QC holds, expiring ≤7 d, partial orders, inbound today, stocktakes to approve), each linking to the
      screen that clears it; **sidebar count badges** mirror it; all from one `GET /api/worklist` over
      existing data (QC count is live). Open PO calls (flagged in `PLAN.md`): keep `/` → worklist as the
      default landing? per-role queues?
- [x] **Global search / command bar** — *done.* A search box in the top bar (`features/Search`,
      `GET /api/search`) matches across products, stock (SKU/batch/location), ASN, orders, shipments and
      locations, and jumps to the hit. Follow-ups: deep-select for ASN/order/shipment (today they land on
      the section, not the specific record — blocked on URL-driven selection, §2), LPN once modelled, and
      keyboard arrow navigation in the results.
- [~] **Confirm + undo on irreversible posts.** QC Release/Reject **and** the ledger adjustment now go
      through a confirm **dialog** (reusable `shared/ui/Modal`) that summarises the change. Remaining: a
      short **undo** window after an irreversible post.
- [x] **Reason (+ note) on QC Release/Reject.** *Done.* The decision dialog requires a reason (Release:
      passed / lab OK / temp OK; Reject: failed temp / damaged / expired / lab fail) plus an optional
      note, sent with the command — closes the audit gap flagged in
      [`02-exceptions.md`](../../../docs/design/02-exceptions.md).
- [x] **Pagination, sort, and a row count** — *done.* `DataTable` now sorts on header click (TanStack
      `getSortedRowModel`) and paginates with a `1–N of M` row count + Previous/Next (default page size 25);
      every table that uses it gets this for free. Follow-up: a page-size selector and **CSV export** (the
      export/print piece is still open under §2).

### P1 — actions & operational hygiene

- [ ] **Export CSV + print** — the warehouse runs on Excel and paper: export tables, print ASN/GRN, count
      sheets, waybills/labels, packing slips.
- [~] **Activate the inert prototype actions.** *Done:* **New ASN** and **New order** (create dialogs with
      a dynamic lines editor → stateful create → the record appears and is selected) and **Assign carrier**
      (carrier + pickup → the shipment moves from "awaiting carrier" to "assigned" on the board).
      Release-to-wave, print waybill, and **Recount selected** (re-issues a blind count) are now wired too.
      Topology location **Edit**, **Add location** and **Add room** (new node in the tree, grouped under
      its warehouse) are wired too. **No prototype actions remain inert.**
- [x] **Logistics — deeper flows.** *Done:* dock-slot scheduling, split/hold on partial orders, mark
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
- [x] **Multi-warehouse:** *done.* The TopBar warehouse switcher (`shared/warehouse/WarehouseContext`)
      is real — picking a site re-points the api client (`X-Warehouse-Id` header, `setActiveWarehouse`)
      and invalidates all queries so every screen refetches scoped to it; choice persists in
      `localStorage`. Operational data (stock+KPIs, movements, ASN, orders, dispatch, QC, stocktake,
      worklist, search) is filtered server-side in the MSW handlers (WH-01 + a WH-02 Poznań dataset);
      master data (products, topology) stays cross-warehouse by design. A user menu (My profile / language
      / **Sign out**) is in the TopBar too. Follow-up: scope `topology`/`locations` move-targets per site.
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
      endpoint as the real one ships.
- [ ] Auth: attach the bearer token / session to requests in the seam once Identity is in (out of scope
      in pass 1 per blog #11, but the seam is the single place to add it).

## 2. Cross-cutting UX the design system calls for

- [ ] **`Toast` keyed by error code** (design system §3, `00-design-system.md`). Today each form/list
      surfaces success/error inline. Replace with a global toast that renders `ApiError.code`
      (the seam already parses it) in the same language the API returns. Add a `ToastProvider` and a
      `useToast()` hook; wire mutations' `onError`/`onSuccess` to it.
- [ ] **URL-driven selection & filters.** Master-detail screens (Inbound, Outbound, Topology) and the
      Stock filters hold selection in local `useState`. Move to typed TanStack Router **search params**
      so views are deep-linkable and refresh-safe (this was an explicit reason we picked TanStack
      Router — plan §3).
- [ ] **Optimistic writes beyond QC.** Adjustment / Stocktake post and show a banner; consider optimistic
      cache updates + rollback (the QC pattern in `qc.model.ts`) where it improves feel.

## 3. Performance & build

- [ ] **Route-level code-splitting.** The bundle is ~590 kB (Vite advisory > 500 kB). Lazy-load screen
      components per route (`createRoute({ component: lazyRouteComponent(...) })`) so each screen is its
      own chunk; the initial load drops to the shell + stock view.
- [ ] Consider a `manualChunks` split for the vendor libs (TanStack, MSW out of prod) if needed.

## 4. Quality / tooling

- [ ] **ESLint + Prettier** — not yet configured for this app. Add a flat config consistent with the
      repo; wire `npm run lint`.
- [ ] **CI** — run `typecheck`, `test:run`, `build` for `src/web/admin` in the pipeline (mirrors the
      terminal). MSW handlers already back the tests, so no extra setup.
- [~] **a11y pass** — *done:* clickable `DataTable` rows are now keyboard-operable (`role="button"`,
      `tabIndex`, Enter/Space) with a focus ring. *Remaining:* `aria-current` on the active sidebar link,
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
- [ ] **E2E smoke** (optional) — a Playwright happy-path click-through (mirrors `tests/e2e/terminal`).
- [ ] More test depth where logic is thin today: Stock empty-state, error states per screen, i18n
      language switch.

## 5. Deferred / not-yet-designed screens

These have **no prototype yet** — they need design first (`docs/design`), then a slice here. Their
sidebar items are intentionally dimmed and inert.

- [x] **Movements** (Inventory) — *built* (no prototype existed): `/movements` is a read-only view of the
      immutable movement **ledger** (ADR-0002) — date / type / product / location / signed qty / reference,
      with type pills + search + pagination/sort. The sidebar item is enabled. **Inventory is complete.**
- [ ] **Partners** (Master data) — no prototype; external-actor portals are deferred (blog #12).
- [x] **Auth / identity UI** — *built* (badge-scan sign-in). `features/Auth` (`LoginScreen`) +
      `shared/auth/AuthContext` gate the app in `App.tsx`: an unauthenticated desk sees the badge screen;
      a scanned badge resolves to a user via `POST /api/auth/login` and opens the app on the user's
      default warehouse. `features/Profile` (`/profile`) adds identity + editable prefs (phone, default
      warehouse, language). Still mock-only — real token/session attaches at the seam (§1).
- [ ] Out of scope by design for now: analytics/BI dashboards, external self-service portals, the
      terminal's high-contrast theme (desk app is light-only).

## 6. Housekeeping

- [x] Remove dead code: `PlaceholderScreen.tsx` (+ `.module.css`) — *deleted* (all routes are real now).
- [ ] `tokens.css` sync rule: it's vendored from `docs/design/prototypes/tokens.css` (ADR-0005). Add a CI
      check (or a small script) that fails if the vendored copy drifts from the source.
- [x] Per-screen identity in `TopBar` is no longer hard-coded — it reads the signed-in user from
      `AuthContext` (name, initials, role); the demo badges cover manager / coordinator / inspector.
- [x] Every prototype button that was inert (New ASN / New order / Assign carrier / Recount selected /
      location Edit / Add location / **Add room** …) is now wired.

---

*Reference: build [plan](../../../docs/design/03-admin-frontend-plan.md) ·
[architecture blog #18](../../../docs/blog/18-the-admin-panel-architecture.md) · ADRs
[0004](../../../docs/adr/0004-admin-panel-separate-spa.md)–[0007](../../../docs/adr/0007-vertical-slices-in-application-layer.md)
· app [README](README.md).*
