# Warehouse Manager

> **Who:** runs a single warehouse from a desk — mouse + keyboard, big screen, data-dense. Lives in
> analysis and exceptions: stock reports, stocktakes, adjustments, and master data.
> **Front end:** [Admin panel](../00-design-system.md#2-two-type-scales-one-base-two-ergonomics).
> **Use cases:** [UC-05](../03-use-cases.md#uc-05-view-stock),
> [UC-07](../03-use-cases.md#uc-07-stocktake),
> [UC-08](../03-use-cases.md#uc-08-stock-adjustment),
> [UC-13](../03-use-cases.md#uc-13-manage-products),
> [UC-14](../03-use-cases.md#uc-14-manage-topology).

The manager never scans anything. They want tables, filters, KPIs, and the ability to act on
exceptions in bulk.

---

## Journey A — View stock (UC-05)

1. Opens **Stock view**. Four KPIs up top: **On hand**, **Available-to-promise**,
   **Reserved/allocated**, and **Blocked + expiring** (the exceptions, in red).
2. Filters by room (cold room), status (blocked, expiring), or searches SKU / batch / location.
3. The table distinguishes `OnHand`, `ATP`, and per-row **status badges** — `Available`,
   `Reserved`, `Expiring`, `In transit`, `Blocked · QC` — so the state of every lot is legible at a
   glance.

**Screen**
- **Stock view** — [prototype](../prototypes/admin-1-stock.html) · Figma frame pending re-capture ⏳.
  This is the canonical use of every [status token](../00-design-system.md#1-status-colours--the-load-bearing-tokens)
  in one place, plus `QuantityWithUnit` and tabular numerals.

---

## Journey B — Manage products & topology (UC-13 / UC-14)

1. Under **Master data → Products**, opens a `ProductType`: identity (SKU/EAN/category/unit),
   dimensions & weight, pack/unit conversions, and **storage requirements**.
2. Storage requirements are the sharp part: temperature range (→ *Cold room only* badge), ADR,
   batch-tracked, expiry-tracked (FEFO). A warning makes the rule explicit: **changing storage
   requirements does not move existing stock** — the system reports incompatibilities to resolve.
3. Topology (UC-14) uses a **warehouse → room → location tree**: pick a room, set its type and
   temperature range (which *drives* the put-away environment invariant), and manage its locations
   (capacity m³, load limit kg). Rooms/locations holding stock can't be deleted until emptied.

**Screens**
- **Product master data** — [prototype](../prototypes/admin-4-product.html) ·
  [Figma](https://www.figma.com/design/xAzdWqmAOd3b2ZKWlU0TgR?node-id=6-2) ✅
  Sectioned form (Identity / Dimensions / Storage requirements) with toggles, a temperature-range
  control, and the “does not move existing stock” caution echoing
  [UC-13](../03-use-cases.md#uc-13-manage-products).
- **Warehouse topology** — [prototype](../prototypes/admin-7-topology.html) · Figma frame pending
  re-capture ⏳. Tree on the left (warehouse, rooms with their temp badges, locations, docks),
  room/location detail on the right, and the “can't delete with stock” guard (UC-14).

---

## Journey C — Stocktake review (UC-07)

1. Manager orders a **blind count** of selected locations (operators count; expected quantities
   hidden during counting).
2. The system surfaces only the **discrepancies** for approval — a summary (locations counted,
   matches, discrepancies, net variance) and a table of each difference (System vs Counted vs Δ)
   with a **required reason** per line (damage, loss, pick error, count correction).
3. Approving posts ledger adjustments with reason `StocktakeDifference` — every entry audited.

**Screens**
- **Stocktake review** — [prototype](../prototypes/admin-3-stocktake.html) ·
  Figma frame pending re-capture ⏳.
  Variances colour-coded (green positive / red negative), reason required before *Approve →
  ledger*.
- **Stock adjustment** (UC-08) — [prototype](../prototypes/admin-9-adjustment.html) · Figma frame
  pending re-capture ⏳. A single-item, reason-bearing correction (damage/loss/count) that posts a
  movement to the immutable ledger with a who/when **audit** line — the manual sibling of the
  stocktake.

---

## Design notes specific to this actor

- **Density over chrome** — the admin type scale is deliberately small; the table is the product.
- **Exceptions first** — KPIs and filters are built to pull blocked/expiring/discrepant stock to
  the surface, because that's what a manager spends their day on.
- **Every write is audited and lands in the immutable ledger** — stocktakes and adjustments are
  reason-bearing by construction (the
  [movement-ledger invariant](../01-domain-overview.md#5-key-business-rules-invariants)).
