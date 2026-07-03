# ADR-0012 — Identity Bronze via Keycloak Admin API polling, not CDC or events

- **Status:** Accepted
- **Date:** 2026-07-03
- **Deciders:** dev team
- **Related:** [ADR-0010](0010-bi-export-reuses-transactional-outbox.md); [ADR-0011](0011-cdc-via-debezium-server-on-rabbitmq.md); [docs/BI_Plan.md](../BI_Plan.md) §5.4, §11

## Context

`DimUser` needs Keycloak's users and roles in Bronze. Neither of the other two Bronze mechanisms
reach it: Keycloak publishes no domain/integration events (ADR-0010 doesn't apply), and this stack
runs Keycloak in `start-dev` mode with **embedded, non-persistent storage** — there is no Postgres
WAL to read, so Debezium/CDC (ADR-0011) has literally nothing to attach to.

## Decision

A third, narrower mechanism: `Warehouse.IdentityBridge` polls Keycloak's own Admin REST API on an
interval, lists users in the `warehouse` realm, fetches each user's realm-role mapping (a second
call — `UserRepresentation` doesn't carry roles), and writes one `BronzeEvent` per user through the
**same** `IBronzeExportWriter` ADR-0010 already established — no new S3/Parquet code. Snapshot
grain is one file per `user_id` per UTC day (latest-wins on same-day re-poll).

## Alternatives considered

| Option | Why not |
|---|---|
| Point Debezium at Keycloak's database anyway | There is no persistent database to point it at in this stack (`start-dev`, embedded H2/in-memory storage) — would require first switching Keycloak to an external Postgres, a much bigger infra change than this data need justifies today |
| Have each backend service publish a `UserAssigned`/`UserRoleChanged` event when it reads identity | Backend services only read tokens, they don't own or mutate identity — inventing an event for data they don't produce would be modeling something that isn't a domain fact of theirs |
| Export a static user/role list from realm JSON at deploy time | Point-in-time only — misses any change made after deploy (new user, role change) until the next deploy; defeats the purpose of a BI dimension that should reflect current org state |
| Keycloak's own event listener SPI (custom event listener plugin publishing to RabbitMQ) | Real option, but requires writing and maintaining a custom Java Keycloak plugin — meaningfully more code and a second language/runtime to own, for a dimension table (`DimUser`) that changes rarely; revisit if user/role changes need to be near-real-time rather than poll-interval-fresh |

## Consequences

- **Positive:** works regardless of Keycloak's storage mode; reuses ADR-0010's writer wholesale, so
  this is the smallest possible increment of new code for a third source shape. Verified for real
  against an actual Keycloak container with the actual realm import — the highest-confidence piece
  of the whole BI plan (`docs/BI_Plan.md` §11).
- **Negative / the price:** an extra Admin API call per user for role mappings means poll cost
  scales linearly with user count — fine at today's scale (a handful of operators), would need
  batching or a different Keycloak API if the user base grew by orders of magnitude. Poll-based
  means `DimUser` is only as fresh as the poll interval, not real-time.
- **Revisit if:** Keycloak moves to a persistent external database in this stack — CDC (ADR-0011)
  would then become the more consistent mechanism, unifying identity onto the same pipeline as
  everything else instead of a third bespoke path.
