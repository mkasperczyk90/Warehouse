# ADR-0003 — Local replicas instead of cross-service queries

- **Status:** Accepted
- **Date:** 2026-05-21
- **Deciders:** dev team
- **Related:** blog [#2](../blog/02-why-we-start-with-the-domain.md) (Decision 4), [#3](../blog/03-bounded-contexts-and-use-cases.md); [ADR-0001](0001-microservices-from-day-one.md)

## Context

`PutAwayPolicy` must validate every put-away against the product's storage requirements (owned
by Catalog, in `masterdata-service`) and the room's environment (owned by Topology, in
`warehouse-service`). The obvious implementation is a synchronous call to masterdata on every
forklift scan. That creates **temporal coupling**: if `masterdata-service` is slow or down, the
forklift stops — and put-away is a hot path under dozens of concurrent operators.

## Decision

Inventory does **not** call other services on the invariant path. It keeps **local replicas** —
`ProductSnapshot` and `LocationSnapshot` — updated asynchronously by integration events
(`ProductDefined`, `ProductStorageChanged`, `LocationDefined`, `RoomEnvironmentChanged`).
`PutAwayPolicy.CanStore()` reads only local data.

## Consequences

- **Positive:** put-away works even when `masterdata-service` is down; no temporal coupling on a
  hot path; each context evolves its own copy of the data it needs.
- **Negative / the price:** the replica can be **seconds stale** — a window where Catalog has
  changed a storage requirement but Inventory hasn't caught up (the "yogurt gap", see
  [#6](../blog/06-the-price-tag.md)). Accepted because product master data changes rarely and a
  put-away validated against a 5-second-old temperature requirement is a non-problem; the cost of
  a stopped forklift is not.
- **Revisit if:** storage requirements start changing frequently enough that staleness causes
  real mis-stores — then add a compensating re-validation sweep (its own ADR).
