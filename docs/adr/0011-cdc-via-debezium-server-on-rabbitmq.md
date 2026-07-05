# ADR-0011 — CDC for state without events: Debezium Server on RabbitMQ, not Kafka Connect

- **Status:** Accepted
- **Date:** 2026-07-03
- **Deciders:** dev team
- **Related:** [ADR-0010](0010-bi-export-reuses-transactional-outbox.md); [docs/BI_Plan.md](../BI_Plan.md) §11 CDC section

## Context

Some state never becomes an integration event, because no *other service* needs to react to it —
the `stock_movements` ledger, stocktake counts, current on-hand, and the entirety of `partners`
(that module publishes no events at all). ADR-0010's mechanism can't reach any of it: there's no
event to hook a consumer onto. BI still needs it (§8's inventory-accuracy, turnover, and
partner-relationship metrics all read from these tables).

The explicit constraint is **cloud-agnostic**: this stack targets AWS today but nothing about the
design should be AWS-only, which already rules out AWS DMS despite it needing the least operational
effort. The existing broker is RabbitMQ, not Kafka (`02-bounded-contexts.md` §Messaging) — there is
no Kafka cluster anywhere in this stack today.

## Decision

**Debezium Server** (the standalone, Kafka-Connect-free distribution), one instance per source
database, reading Postgres logical replication (`pgoutput`) directly and publishing onto the
**existing RabbitMQ broker** via Debezium's own `debezium-server-rabbitmq` sink — plain AMQP
`basicPublish`, no new protocol. Debezium's native `op`/`before`/`after`/`source` envelope is left
untouched (Kafka Connect's schema-wrapper is disabled), so it plugs into Databricks' `APPLY CHANGES
INTO` later without a custom transform. A small custom worker, `Warehouse.CdcBridge`, is a raw AMQP
consumer that lands each record byte-for-byte in S3, keyed by Postgres LSN for idempotent
redelivery. Table scope is narrow and deliberate (see `docs/BI_Plan.md`'s CDC section) — current-
state/ledger tables only, never a downstream replica of another service's own data.

## Alternatives considered

| Option | Why not (for this stack, today) |
|---|---|
| **Kafka Connect + Debezium connectors + MSK/self-hosted Kafka** | The textbook, most battle-tested CDC-to-lake reference architecture — distributed workers, task rebalancing, Kafka-backed offset storage (durable across restarts by construction), Schema Registry integration. Rejected here purely on operational cost: it means running and paying for **a second message bus** alongside RabbitMQ, solely to move CDC data, when RabbitMQ already exists and micro-batch latency is sufficient (§11) |
| **AWS DMS** | Fully managed, least ops of any option. Disqualified by the explicit cloud-agnostic requirement — a service that only runs on AWS, for a pipeline that's supposed to be portable |
| **Debezium Server's own S3 sink** (`debezium-server-s3`), skipping RabbitMQ and `CdcBridge` entirely | A real, simpler alternative — one fewer hop, no custom worker to own. Not chosen because it would mean CDC data takes a *different* delivery path than BI-export data (ADR-0010, which already flows through RabbitMQ), losing the "one broker, one delivery story" property; also less mature/less documented than the RabbitMQ sink for this stack's needs. Worth revisiting if `Warehouse.CdcBridge`'s bespoke consumer becomes a maintenance burden |
| **Managed ELT/CDC SaaS (Fivetran, Airbyte Cloud, Estuary Flow, Databricks Lakeflow Connect)** | Genuinely the most "modern," least-code option in 2026 — zero infrastructure to run. Rejected on cost (per-connector/per-row pricing) and vendor lock-in for a project that otherwise self-hosts everything; also most of these still use Debezium under the hood for Postgres, so the underlying CDC technology choice doesn't actually change |
| **Debezium embedded engine inside a custom .NET/JVM host process** | Avoids running the separate Debezium Server JVM as its own container. Rejected — reimplements what Debezium Server already packages (config-driven connector lifecycle, offset management, REST health endpoint) as bespoke hosting code, for no operational win |
| **Roll-your-own logical-replication client** (raw `pgoutput` decoding, no Debezium) | Would avoid the JVM/Debezium dependency entirely. Rejected — reinvents schema/type decoding, snapshotting, and WAL-position bookkeeping that Debezium already solves and has solved for years; high risk, no upside |

## Is this "best practice" / state of the art?

**Mixed — and worth being explicit about which parts are and aren't.**

- **Log-based CDC via Debezium reading Postgres WAL directly is the industry-standard approach** —
  low source impact (no polling queries against a live OLTP table), captures every intermediate
  state, and is the same engine underneath most managed CDC products on the market today. This part
  is not a compromise; it's the right tool.
- **Debezium *Server* instead of Kafka Connect, sinking to RabbitMQ instead of Kafka, is a pragmatic
  simplification for this stack's size — not the pattern a large, always-on production data platform
  would reach for.** The RabbitMQ sink is a comparatively thin, less-traveled path (the smoke test
  already surfaced one config quirk — a routing-key env var that silently doesn't take effect,
  `docs/BI_Plan.md`'s confidence note) compared to Kafka Connect's much larger install base.
- **The offset-storage gap is the sharpest edge.** Each Debezium Server instance persists its
  replication position to a local file (`/debezium/data/offsets.dat`) inside the container, with
  **no volume mounted** — explicitly flagged as unexercised in the smoke test
  (`docs/BI_Plan.md`'s confidence note: "container restart / offset-file persistence across restarts
  (no volume mounted in the smoke test)"). A container restart today likely means Debezium loses its
  WAL position. Kafka Connect avoids this entirely by storing offsets in a Kafka topic, decoupled
  from any one worker's filesystem. This is the single biggest thing standing between "works in a
  smoke test" and "production-grade."
- **An unbounded/unmonitored replication slot is a classic footgun this design doesn't yet guard
  against.** `CREATE PUBLICATION ... FOR ALL TABLES` plus a logical replication slot means Postgres
  retains WAL until Debezium consumes it. If a Debezium Server instance is down or misconfigured for
  an extended period, WAL accumulates on the primary — in the worst case, filling disk. Nothing in
  the current design monitors replication-slot lag or alerts on it.

## Consequences

- **Positive:** cloud-agnostic and reuses the existing broker — no second message bus, no
  AWS-only managed service. Debezium's native envelope requires no custom transform for the
  eventual Databricks `APPLY CHANGES INTO`. Table scope evolves via `table.include.list` alone, no
  migration needed to add a table later. Verified end-to-end for one table via a real
  Postgres→Debezium→RabbitMQ→CdcBridge→S3 smoke test.
- **Negative / the price:** three extra JVM processes running continuously once enabled (real
  Fargate cost, see the task-sizing note in `variables.tf`); no offset durability across container
  restarts today; no replication-slot-lag monitoring/alerting; the RabbitMQ sink is a thinner-tested
  path than Kafka Connect's, evidenced by the routing-key quirk found in testing; 24 of 25 configured
  tables and 2 of 3 Debezium instances have never actually been run, only the single-table/
  single-instance path is proven.
- **Revisit if:** (a) a Kafka broker becomes justified for other reasons (e.g. sub-minute streaming
  needs elsewhere) — at that point Kafka Connect becomes strictly better with no new infra cost; (b)
  the offset-durability gap causes a real incident (lost WAL position forcing a full re-snapshot) —
  mount a persistent volume or move offset storage to something durable (e.g. the existing Postgres
  itself, or S3) before that happens rather than after; (c) nobody is watching replication-slot lag
  by the time this runs anywhere non-ephemeral — add a CloudWatch alarm on slot retained-WAL size
  before that.
