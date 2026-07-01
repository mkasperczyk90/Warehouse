# Warehouse (WMS) — Project Plan

> Anchor file: come back here to recall the idea and check what's next.
> As of: 2026-06-26 (roadmap reconciled with the codebase — see notes per item)

## The idea in 3 sentences

We are building a WMS for multiple warehouses with rooms (including cold rooms) and storage
locations. The core domain is **Inventory** (an immutable movement ledger + stock as a projection)
and **Logistics** (inbound/outbound processes as sagas). We build **microservices from day one**
in .NET 10: 3 services hosting 5 bounded contexts as internal modules, integrated through events
on RabbitMQ behind an API gateway.

## Documentation

| File | Contents |
|---|---|
| [01-domain-overview.md](01-domain-overview.md) | Vision, subdomains, actors, ubiquitous language, invariants |
| [02-bounded-contexts.md](02-bounded-contexts.md) | 5 contexts, context map, 3-microservice split decision, tech stack & practices |
| [03-use-cases.md](03-use-cases.md) | UC-01…UC-14 + sequence and state diagrams |
| [04-domain-model.md](04-domain-model.md) | Archetypes (Arlow/Neustadt + Coad), class diagrams per context, events (design sketch) |
| [models/](models/README.md) | **As-built model reference** — one file per module, diagrams of how contexts connect; update together with code |
| [blog/](blog/README.md) | Blog series "brick by brick" — plan + posts #1-#4 (domain, contexts, archetypes, shared kernel) |

## Key decisions (ADRs in a nutshell)

1. **Microservices from day one: 3 services, not 5** — `warehouse-service` (Inventory + Topology:
   shared hard invariants), `logistics-service` (sagas, integrations), `masterdata-service`
   (Catalog + Partners: read-mostly). Contexts stay separate modules + schemas inside each service.
2. **Database per service, async-first** — three PostgreSQL databases, no cross-service table access;
   events on RabbitMQ; sync calls only where the use case demands it, always with resilience policies.
   Accepted trade-off: eventual consistency between services (snapshot replicas updated by events).
3. **`StockMovement` = append-only ledger** — stock is a projection of movements; a correction is a reversing movement.
4. **Aggregate = `StockItem`** (product + batch + location), not the whole warehouse — small
   transactions, no hot spots; optimistic concurrency via Postgres `xmin`.
5. **Archetypes**: `Quantity` always with a unit, `Party/PartyRole` for business parties,
   `ProductType` as Description, Moment-Interval for all documents/movements, `StorageRequirement` as Rule.
6. **Temperature compatibility is a hard invariant** — validated on every put-away/move.
7. **Two-stage allocation** — soft `StockReservation` at order time (protects available-to-promise),
   hard `Allocation` at wave/pick time (FEFO, batch quality re-checked then). `Sku` is owned per
   context (Catalog strict / Inventory light / Logistics loose `ProductCode`); unit conversions are
   a catalog default with per-delivery override.
8. External integrations (ERP/e-commerce) always behind an **ACL**.
9. **Stack:** .NET 10 LTS / C# 14, EF Core 10 + PostgreSQL, Minimal APIs, Aspire for local
   orchestration (all services + Postgres + RabbitMQ in one `dotnet run`), API gateway (YARP),
   transactional **outbox/inbox from day one**, versioned `Contracts` package, contract tests (PactNet),
   OpenTelemetry, Testcontainers + xUnit v3, React 19 + TypeScript + Vite on the front.
10. **Messaging library:** MediatR/MassTransit went commercial (2025) → spike **resolved: Wolverine**
    (OSS), for its built-in EF Core + PostgreSQL transactional outbox; MassTransit v8 kept as the OSS
    fallback (its saga maturity stays the strongest reason to revisit if orchestration grows).

## Roadmap

### Phase 0 — foundations (largely complete)
- [x] .NET 10 solution: `Warehouse.slnx` (new XML solution format), microservices layout
      (`src/Services/{Warehousing,Logistics,MasterData}` — each with internal context modules,
      `src/Gateway`, `src/SharedKernel`, `src/Contracts`, `src/AppHost`, `src/ServiceDefaults`),
      Central Package Management + `Directory.Build.props` (nullable, warnings as errors, analyzers)
- [x] SharedKernel: archetype value objects (`Quantity`, `UnitOfMeasure`, `Weight`, `Volume`,
      `TemperatureRange`, `Money`, `Address`) as records, base `Entity`, `AggregateRoot`,
      `IDomainEvent`, `DomainException` with error codes; strongly-typed IDs live in the modules.
      `Sku` moved **out** of the kernel into its owning contexts (Catalog strict / Inventory light /
      Logistics `ProductCode`) — see [REVIEW-FIXES.md](REVIEW-FIXES.md) Fix C (done)
- [x] **Domain models (pulled forward from Phases 1-4):** Clean Architecture folders per module
      (`Domain` filled, `Application`/`Infrastructure` as placeholders) — Catalog (`ProductType`),
      Partners (`Party`/roles), Topology (`WarehouseSite`/`Room`/`Location`/`Dock`),
      Inventory (`StockItem`, `StockReservation`, `Batch`, `HandlingUnit`/LPN, `StockMovement`
      ledger, `Stocktake`, snapshots, domain services `PutAwayPolicy` / `StockTransferService` /
      `ReservationService` / `AllocationPolicy`),
      Logistics (`InboundDelivery`, `OutboundOrder`, `PickList`, `Shipment`);
      unit conversions: catalog default (`ProductType.UnitConversions`) + per-delivery override
      (`DeliveryLine.Pack`). Stock-changing behaviors **return the `StockMovement` to persist**
      — ledger and stock cannot diverge; batch QC holds enforced by `AllocationPolicy` + event
      convergence. **Two-stage allocation** (soft `StockReservation` at order time → hard
      `Allocation` at wave/pick time) and `Sku` ownership per context — see [REVIEW-FIXES.md](REVIEW-FIXES.md)
- [x] Contracts package: versioned integration events, additive-only policy — `Warehouse.Contracts`
      holds ~13 primitives-only events across Catalog/Topology/Logistics (incl. a `ProductDefinedV2`
      additive revision); additive-only policy stated on the types
- [x] Aspire AppHost: 3 services + YARP gateway + Postgres (db per service) + RabbitMQ,
      service defaults (health checks, OTel, resilience). Local `dotnet run` startup + RabbitMQ/Keycloak
      health checks fixed (PR #24/#25)
- [x] Messaging spike (Wolverine vs MassTransit v8) + **transactional outbox/inbox** over RabbitMQ —
      spike → **Wolverine**; `IDbContextOutbox` producers + Wolverine `Handle()` consumers (inbox)
      rolled out across MasterData, Warehousing/Inventory and Logistics; RabbitMQ fanout wiring in
      `ServiceDefaults/Messaging.cs` (`AddWarehouseMessaging`)
- [x] EF Core 10: DbContext per context module, schema per module, migrations per module (Catalog,
      Partners, Topology, Inventory, Logistics all have an `Initial` + later migrations).
      **Remaining: CI model-drift check** (no pending-migration guard in `backend.yml` yet)
- [x] Architecture tests guarding module boundaries inside services (`tests/Warehouse.ArchitectureTests`)
- [ ] Contract tests (PactNet) skeleton — **not started** (no Pact tests in the suite yet)
- [x] CI/CD: build + test + container image per service — GitHub Actions workflows for `backend`,
      `frontend`, `docker`, `e2e`, `deploy-aws`, plus `codeql`/`gitleaks`/`dependency-review`; Dockerfile
      per service (Gateway + 3 APIs) and per web app

### Phase 1 — master data (mostly complete)
- [x] Catalog: `ProductType` + `StorageRequirement` CRUD (UC-13) — `Products` slices
      (define / rename / change-storage / list / get + CSV bulk import) over `catalog/products`
      (gateway `/api/catalog`); publishes `ProductDefinedV2` via the outbox; admin catalogue wired to
      the real endpoints (going live = MSW off)
- [x] Topology: `Warehouse/Room/Location/Dock` (UC-14) — vertical slices (establish / add room·location·dock /
      change environment / change-capacity / list·get + flat `tree`/`room`/`locations` reads) +
      `/topology/warehouses` endpoints (gateway `/api/topology`); publishes `LocationDefinedV1` /
      `RoomEnvironmentChangedV1` via the outbox → Inventory `LocationSnapshot`, read by put-away's
      `PutAwayPolicy`. Admin topology wired read+write to the real backend; e2e still mock-only (ADR-0006)
- [ ] Partners: `Party` + roles (supplier/customer/carrier) — **domain + EF persistence scaffolded only**
      (`Party`/`PartyRole`/`PartyRegistrationPolicy`, `PartyRepository`, migration); **no Application
      slices / endpoints / admin screen yet** (Logistics carries supplier/customer/carrier as opaque
      `PartyRoleRef` strings for now)
- [x] React: admin panel for master data — full admin SPA built and wired (see `src/web/admin/TODO.md`
      for the remaining UX/tooling backlog)

### Phase 2 — Inventory core (complete)
- [x] `StockItem`, `Batch`, `StockMovement` (ledger), stock projections (UC-05) — `StockOverview` +
      `MovementsLedger` read models over `inventory/stock/*` + `inventory/movements`; seeded WH01/WH02
- [x] Moves + environment/capacity validation (UC-06) — move with the temperature-compatibility
      invariant enforced; `PutAwayPolicy` reads the `LocationSnapshot`
- [x] Adjustments and stocktake (UC-07, UC-08) — `AdjustStock` (one signed ledger entry, never below
      zero/allocated) and the persisted `Stocktake` aggregate (start / review / approve→reconcile to ledger)

### Phase 3 — Inbound (complete for the modeled use cases)
- [x] ASNs + dock slots (UC-01) — `Deliveries` slices over `logistics/deliveries` (create / dock-slot /
      mark arrived / get / list)
- [x] Goods Receipt with discrepancies (UC-02) — `ReceiveGoodsReceipt` (Inventory consumer) +
      `ConfirmReceipt`; receiving-progress read for the coordinator
- [x] Put-away with location proposals (UC-04), QC holds (UC-03) — `ProposePutAway` query +
      `ConfirmPutAway`; batch-level `Quality` release/reject with reason+note
- [x] React: operator terminal view (large touch targets, scanner-first) — terminal SPA built
      (`src/web/terminal`) with playwright-bdd e2e (`tests/e2e/terminal`); mock-only at the seam (ADR-0006)

### Phase 4 — Outbound (complete for the modeled use cases)
- [x] Outbound orders + FEFO reservations (UC-09) — `Orders` slices (create / decision split-hold /
      release to picking / cancel) + soft `StockReservation` saga (`StockReservedV1`). Note: the split/hold
      path still needs a partially-reserved order from the reservation saga to exercise end-to-end
- [x] Picking with routing (UC-10), packing (UC-11), dispatch (UC-12) — `PickLists` (start / confirm /
      report-short-pick), terminal Packing, `Dispatch` board (`AssignCarrier`/`AdvanceShipment`,
      `Shipment` carrier lifecycle). The wave/pick **routing algorithm** stays deferred (lifecycle modeled,
      optimizer not — see closing note)

### Phase 5 — integrations and hardening
- [ ] ACL for ERP/e-commerce, outgoing webhooks/events
- [ ] Boundary review: do Inventory/Topology or Catalog/Partners deserve their own service?
      (moving a module between services, not a rewrite)
- [ ] Production hardening: per-service scaling, broker DLQs and retry policies, SLOs + alerting
      on OTel metrics

## Open questions (for PO / clients)

- [ ] Do we track individual units (serial numbers), or are batches enough? (assumption: batches)
- [ ] Do dock slots need a separate calendar/carrier booking flow? (assumption: simple time windows)
- [ ] Cross-docking (receipt straight to dispatch, skipping put-away) — MVP or later?
- [ ] Stock valuation (FIFO/weighted average) — our problem or the ERP's? (assumption: ERP)
- [ ] Multi-tenancy: single client or SaaS for many companies? (assumption: one company, many warehouses)
- [ ] Cold-room temperature monitoring (IoT, alarms) — out of MVP scope?

Admin-panel scope (raised by the post-build UX review — see the
[admin frontend plan §11](design/03-admin-frontend-plan.md#11-post-build-review--what-the-gap-closing-pass-delivered)
and the app [`TODO.md`](../src/web/admin/TODO.md)):

- [x] Admin landing: a **work-queue / worklist** ("what needs me now" — QC backlog, partial orders,
      expiring ≤7 d, ASN arriving today). *Built as the
      [Today / Worklist](design/prototypes/admin-10-worklist.html) (admin-10) at `/today`, where `/` now
      redirects — actionable queues + sidebar counters, distinct from the deferred BI dashboards.
      Decided: the worklist **replaces** the stock view as the default landing.* Still open: per-role
      tailoring of which queues show.
- [x] **Global search** across the admin (SKU / EAN / batch / LPN / location / ASN / SO / SHP). *Decided:
      first-class — built as a top-bar command bar (`src/web/admin/src/features/Search`).*
- [x] List + CRUD **front doors** — product catalogue + New product, create-ASN + dock slot, create
      outbound + assign carrier, start-stocktake, add room / location — **all built** (UC-01/07/09/12/13/14).
      A Movements ledger view was added beyond the original nine screens.

**Done after review** (was deferred, now modeled): **two-stage allocation** — soft
`StockReservation` (SKU-level, at order time) → hard `Allocation` (concrete batch+location,
FEFO, quality re-checked, at wave/pick time). See [REVIEW-FIXES.md](REVIEW-FIXES.md).

**Still deliberately deferred** (known simplifications, not omissions): the wave-planning
*algorithm* (pick routing, FEFO ordering across many stock items — the *lifecycle* is modeled,
the optimizer is not), serial-number tracking, slotting optimization, stock valuation.
