# ADR-0010 — BI Bronze export reuses the transactional outbox, per-event

- **Status:** Accepted
- **Date:** 2026-07-03
- **Deciders:** dev team
- **Related:** [ADR-0001](0001-microservices-from-day-one.md) (transactional outbox as the messaging
  backbone); [docs/BI_Plan.md](../BI_Plan.md) §11

## Context

Power BI needs the ~11 published integration events landed in an S3 Bronze zone so Databricks Auto
Loader can pick them up (`docs/BI_Plan.md` §1, §3). Each of the 3 backend services already
publishes these events transactionally via the outbox (ADR-0001) onto RabbitMQ. The question is
*how* those same events also reach S3, without turning "add BI reporting" into "add a second
integration mechanism" that every future event has to remember to wire up twice.

## Decision

**One more consumer per service, on its own dedicated `<service>.bi-export` queue, bound to the
exchange(s) the service already publishes on.** Each consumer maps its event to a flat
`BronzeEvent` envelope and writes it through a shared `IBronzeExportWriter`
(`Warehouse.ServiceDefaults.BiExport`) — one Parquet file per event, keyed by the outbox message id
so redelivery overwrites rather than duplicates. A service opts in with one line
(`AddBiExport()`), the same shape as `AddServiceDefaults()`/`AddWarehouseMessaging()`. The
`bi-export` queue is intentionally separate from the business-critical replica/consumer queues, so
a slow or backed-up S3 write can never apply backpressure to the domain flow.

## Alternatives considered

| Option | Why not |
|---|---|
| Databricks reads each service's OLTP database directly (JDBC/federated query) | Couples an analytics workload's query pattern and schedule to the live transactional schema; a heavy BI query competing with production traffic on the same database is the classic anti-pattern this design exists to avoid |
| A dedicated "BI outbox" table per service, separate from the domain outbox | Duplicate transactional-write infrastructure for data that's already committed atomically in the real outbox; doubles the write amplification per business transaction for no consistency gain |
| Batch-export a nightly dump of each service's event log table | Loses the near-real-time cadence §11 already targets (micro-batch via Auto Loader) for no simplicity gain — you still have to write an exporter, just a slower one |
| CDC on the outbox table itself (log-based capture of the outbox insert) | Would work, but reintroduces exactly the "reconstruct intent from a raw row diff" problem ADR's own CDC section (see ADR-0011) argues against — the outbox row *is* the event, so a typed consumer is strictly simpler than decoding it from a WAL record |

## Consequences

- **Positive:** zero new integration mechanism — a team adding event #12 already knows the pattern
  (`add-bi-export` skill). Idempotent by construction (outbox message id = S3 object key). BI load
  can never slow down or fail a business transaction — it's a downstream, dedicated queue.
- **Negative / the price:** BI export is **opt-in per handler** — an event that exists but has no
  `BiExportConsumer` silently never reaches Bronze. Nothing fails loudly if a new event ships
  without its export sibling; it's a code-review discipline, not an enforced invariant.
- **Revisit if:** the number of published events grows large enough that "did every event get its
  export consumer" needs an automated check (e.g. a build-time assertion that every `IEvent` type
  has a matching consumer) rather than relying on the skill/checklist.
