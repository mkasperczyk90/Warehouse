# Domain review fixes (2026-06)

Three modeling problems raised in expert review. All three are real; two are the
expensive-to-change-later kind, so we fix them in the model now.

## Fix A — Two-stage allocation (was: hard allocation at order time)

**Problem.** Order arrival immediately reserved a concrete batch+location via FEFO.
In a busy WMS that is *hard allocation* at the wrong moment: between order (day 0) and
pick (day +2) the pinned pallet can be QC-blocked or damaged → physical deadlock and
constant re-pinning. Real WMS does **soft reservation** at order time (logical, SKU-level,
protects available-to-promise), then **hard allocation** (concrete batch+location, FEFO,
quality re-checked) at wave/pick release.

**Change.**
- New aggregate `StockReservation` (soft): `Sku` + `WarehouseCode` + `OrderRef` + qty,
  status `Open → PartiallyAllocated → Allocated / Released / Cancelled`. Created at order time.
- New domain service `ReservationService.Reserve(...)` — gates against available-to-promise
  (ATP = Σ OnHand − Σ Allocated − Σ open soft reservations), passed in by the app layer.
- `StockItem`: `Reserved` → `Allocated`; child `Reservation` → `Allocation`
  (adds `StockReservationId`); `Reserve()` → `Allocate()`; `ReleaseReservation()` →
  `ReleaseAllocation()`.
- `ReservationPolicy` → `AllocationPolicy`: same FEFO + batch-quality gate, but now runs at
  **allocation time** — so a batch blocked *after* the order is caught when committing stock.
- Wave-planning *algorithm* (routing, batch FEFO ordering across items) stays deferred —
  what we fix is the *lifecycle*, not the optimizer.

## Fix B — Unit conversions: catalog default + per-delivery override

**Problem.** Conversions (1 pallet = 48 pcs) sat only in `ProductType` as fixed master data.
Reality: the same SKU is palletized differently per delivery (industrial vs euro-pallet);
"how many on a pallet" is often a fact of the *Inbound Delivery*, not encyclopedic master data.

**Change.**
- `ProductType.UnitConversions` stays as the **default/standard** pack (still used for
  outbound pack planning and as a fallback).
- `DeliveryLine` (Logistics) gains optional `DeliveryPack` (delivery-specific
  unit → base-unit factor). Receiving converts announced units → base using the delivery
  pack if present, else the catalog default.

## Fix C — `Sku` out of the SharedKernel

**Problem.** `Sku` (strict regex + EAN-linked) lived in SharedKernel. But "SKU" means
different things per context: Catalog enforces syntax + EAN; Logistics often holds *what the
scanner read* (an unknown code is valid input — UC-01 flags it for clarification). A shared
strict type invites universal validation that pollutes the kernel.

**Change.** Apply post #4's own thesis fully — identifiers belong to the owning context:
- **Catalog** owns the canonical, strict `Sku` (regex + normalization; EAN linkage).
- **Inventory** keeps its own lightweight `Sku` (normalized; stock only exists for
  cataloged products, so it's known-good — no strict regex).
- **Logistics** uses a loose `ProductCode` (normalized, non-empty) — "what was scanned /
  announced"; resolution to a catalog SKU is an integration concern, not a type rule.
- **SharedKernel** loses `Sku`. It now holds only physics/standards
  (`Quantity`, `Weight`, `Volume`, `TemperatureRange`, `Money`, `Address`).
- `PartyRoleRef` was already Logistics-local — no change, but blog wording clarified
  ("reference by value owned by the consumer", not "shared id").

## Execution order (keep build green at each step)
1. Fix C (mechanical type move) → build.
2. Fix B (additive) → build.
3. Fix A (structural) → build.
4. `docs/models/*`, `docs/04-domain-model.md`, `docs/PLAN.md`.
5. Blog posts #1–#4.
