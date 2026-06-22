# ADR-0001 — Microservices from day one (3 services, 5 contexts)

- **Status:** Accepted
- **Date:** 2026-05-21
- **Deciders:** dev team + Product Owner
- **Related:** blog [#2](../blog/02-why-we-start-with-the-domain.md) (Decision 1), [#6](../blog/06-the-price-tag.md); [ADR-0003](0003-replicas-over-cross-service-queries.md)

## Context

Event storming produced five bounded contexts (Inventory, Logistics, Catalog, Topology,
Partners). The honest default advice for a fresh domain is a **modular monolith** — boundaries
are cheapest to fix when they're just folders, and you can extract services later once the
seams have proven themselves.

But this project has a second goal beyond shipping a warehouse system: it is a **teaching
vehicle** for the real mechanics of a distributed system (transactional outbox, sagas, contract
tests, independent deploys, eventual consistency). Those mechanics are hard to demonstrate
honestly if they're retrofitted in month six.

## Decision

Ship **microservices from day one** — but **three** services, not five, grouping contexts that
share hard invariants or change rhythm:

| Service | Contexts | Rationale |
|---|---|---|
| `warehouse-service` | Inventory + Topology | Share *hard* invariants (capacity, temperature) that must validate in one transaction |
| `logistics-service` | Logistics | Long-running processes, sagas, external integrations |
| `masterdata-service` | Catalog + Partners | Slow-changing, read-mostly reference data |

Inside each service, contexts stay **separate modules with separate DB schemas**. The logical
model is five contexts; only the deployment count is three.

## Consequences

- **Positive:** distributed-system constraints are present from the first commit, not bolted on;
  independent deploy/scale per service; the pivotal events (`GoodsReceiptConfirmed`,
  `StockReserved`, `ShipmentDispatched`) become natural integration points.
- **Negative / the price:** eventual consistency between services; moving a concept across a
  service boundary becomes a **data migration**, not a refactor. We accept this knowingly and
  mitigate by keeping contexts as modules — a wrong boundary moves a module, not a tangle.
- **Revisit if:** the boundaries prove wrong under load (see [#6](../blog/06-the-price-tag.md)),
  or the project's teaching goal changes. A superseding ADR would record the merge/split.
