# Logistics Coordinator

> **Who:** plans deliveries and shipments — ASNs, dock slots, inbound/outbound statuses. A desk
> role focused on the *flow* of goods rather than the stock itself.
> **Front end:** [Admin panel](../00-design-system.md#2-two-type-scales-one-base-two-ergonomics).
> **Use cases:** [UC-01](../03-use-cases.md#uc-01-announce-delivery-asn),
> [UC-09](../03-use-cases.md#uc-09-outbound-order),
> [UC-12](../03-use-cases.md#uc-12-dispatch-to-carrier).

---

## Journey A — Announce & schedule a delivery (UC-01)

1. Opens **Logistics → Inbound (ASN)**: a master/detail list of announced deliveries, each with a
   status badge (`Announced`, `Arrived`, `Completed`).
2. Creates an ASN (supplier, planned date, target warehouse, lines = SKU + qty + unit). The system
   **validates SKUs against the catalog** — an unknown SKU is flagged **for clarification** inline.
3. Assigns a **dock slot** (dock + time window); no free slot → an alternative window is proposed.
4. The ASN reaches `Announced`, then `Arrived` when the truck checks in (handing off to the
   [Warehouse Operator's receipt journey](warehouse-operator.md#journey-a--receive-an-announced-delivery-uc-02)).

**Screen**
- **Inbound (ASN)** — [prototype](../prototypes/admin-2-asn.html) · Figma frame pending re-capture ⏳.
  Master/detail: the list on the left (supplier, slot, status), the selected ASN's header fields
  and **lines table** on the right — including a red **unknown-SKU flag** showing the catalog
  validation rule in action.

---

## Journey B — Outbound order (UC-09)

1. Creates an order (consignee, address, required date, lines = SKU + qty).
2. The system makes a **soft reservation** against ATP (SKU-level — no batch/location pinned yet);
   the detail shows ATP-at-order and reserved per line. Insufficient availability → **partial /
   waiting** order, the coordinator's call.
3. Concrete batch+location are pinned later by **FEFO at wave/pick release**, not here — so a pallet
   isn't committed days before it's picked.

**Screen**
- **Outbound orders** — [prototype](../prototypes/admin-5-outbound.html) · Figma frame pending
  re-capture ⏳. Master/detail like the ASN screen, plus an **ATP / reservation panel** and the
  `Created → Reserved → PartiallyReserved → Picking` status badges from the
  [order lifecycle](../03-use-cases.md#6-process-lifecycles).

## Journey C — Dispatch to carrier (UC-12)

1. Packed shipments queue up **awaiting a carrier**; the coordinator assigns one.
2. A **pickup notice** is sent; the shipment moves to *awaiting collection*.
3. On collection, a signature/confirmation deducts stock (`Dispatched`), emits
   `ShipmentDispatched`, and a **waybill + tracking** are issued.

**Screen**
- **Dispatch board** — [prototype](../prototypes/admin-6-dispatch.html) · Figma frame pending
  re-capture ⏳. A Kanban-style board (*Packed → Carrier assigned → Pickup notice sent →
  Dispatched*) with carrier chips and tracking numbers — status-coded throughout.

## Design notes specific to this actor

- **Master/detail is the pattern** — a list of documents (ASNs, orders, shipments) on the left,
  the working document on the right; the coordinator triages many, edits one.
- **Status badges drive triage** — the same [status tokens](../00-design-system.md#1-status-colours--the-load-bearing-tokens),
  reused on process documents, let the coordinator see at a glance what needs attention.
- The coordinator's outputs become the operators' inputs — these screens are the desk end of the
  flows the [terminal](warehouse-operator.md) executes, and the API surface the
  [external actors](external-actors.md) feed.
