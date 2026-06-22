# External actors — Supplier · Customer · Carrier · ERP/e-commerce

> These actors live **outside** the warehouse. In the current design they interact through
> **APIs and integration events**, not screens — an eventual self-service portal is a later
> decision. This doc records their touchpoints so the API/portal design has a home.
> **Use cases:** [UC-01](../03-use-cases.md#uc-01-announce-delivery-asn),
> [UC-09](../03-use-cases.md#uc-09-outbound-order),
> [UC-12](../03-use-cases.md#uc-12-dispatch-to-carrier).
> All external integration sits behind an **ACL** ([PLAN.md](../PLAN.md), ADR direction).

| Actor | What they do | Touchpoint (pass 1) | Future UI |
|---|---|---|---|
| **Supplier** | Announces deliveries (ASN), checks receipt status | API / EDI → creates an ASN ([UC-01](../03-use-cases.md#uc-01-announce-delivery-asn)); also done by the [Coordinator](logistics-coordinator.md) | Supplier portal: submit ASN, see receipt status |
| **Customer / Consignee** | Places outbound orders, tracks shipments | API / e-commerce → outbound order ([UC-09](../03-use-cases.md#uc-09-outbound-order)); `ShipmentDispatched` + tracking back | Order/tracking portal |
| **Carrier** | Picks up shipments, confirms collection | API + pickup notice; collection confirmation → `Dispatched` ([UC-12](../03-use-cases.md#uc-12-dispatch-to-carrier)) | Carrier confirmation screen / driver app |
| **External system (ERP / e-commerce)** | Integration: stock levels, orders, confirmations | API + integration events on RabbitMQ, behind an ACL | n/a (system-to-system) |

## Why no screens yet

The [domain overview](../01-domain-overview.md#3-actors-our-clients--users) lists these as
**external** actors, and the [roadmap](../PLAN.md#roadmap) puts ACL / outgoing webhooks & events in
**Phase 5**. Designing portals now would gold-plate a guess (blog #9's warning). What they *do*
need from design today is consistency: any future portal must reuse the shared
[status tokens](../00-design-system.md#1-status-colours--the-load-bearing-tokens) and
`QuantityWithUnit` so a supplier sees the same "Received / Discrepancy" language the
[operator](warehouse-operator.md) and [coordinator](logistics-coordinator.md) use.

## When a portal is on the table

- **Supplier portal** ≈ a slimmed-down [Inbound (ASN)](logistics-coordinator.md#journey-a--announce--schedule-a-delivery-uc-01)
  detail view, scoped to that supplier's own deliveries.
- **Customer tracking** ≈ an outbound order + shipment status timeline using the
  `Reserved → Picking → Packed → Dispatched` lifecycle from
  [UC-09's state machine](../03-use-cases.md#6-process-lifecycles).

Until then, the [API gateway + Contracts package](../PLAN.md) *is* their interface.
