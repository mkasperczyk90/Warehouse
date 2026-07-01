# #17 — Choosing a messaging library: the bake-off behind one line of code

*Series: Building a real microservices application, brick by brick.
Previous: [#16 The transactional outbox with Wolverine](16-the-transactional-outbox-with-wolverine.md).
Decision recorded in [PLAN.md](../PLAN.md) (key decision #10).*

---

Post #16 reached for Wolverine in a single line — `builder.UseWolverine(...)` — and moved on. That
line hides a decision that, a year ago, most .NET teams didn't have to make: *which* messaging
library. The answer used to be reflexive ("MassTransit"), and in 2025 it stopped being free. This
post is the bake-off behind that one line: the field, the criteria that actually mattered for a
warehouse, the two finalists head-to-head, and — most reusably — **a method you can run for your own
project** instead of copying ours.

> A note on freshness: library licensing and maturity move fast. Treat the specifics here as "as of
> the decision," not gospel — and re-run the *method*, not the *verdict*, when you choose.

## Why this is suddenly a real decision (the 2025 earthquake)

For a decade the .NET defaults were free and obvious: **MediatR** for in-process messaging,
**MassTransit** for a service bus, **AutoMapper**, **FluentAssertions** for tests. In 2025 their
maintainers — facing the classic open-source funding problem — moved to **commercial licensing** for
new versions. The last truly-OSS lines were frozen; new major versions now cost money per
seat/company.

That's not a scandal (maintaining this stuff for free for years was the anomaly), but it *is* a
forcing function: "just add MassTransit" is no longer a free default. So the choice reopened, and the
honest move is to treat it like any other architectural decision — candidates, criteria, a spike, a
price tag.

## The field

The candidates worth knowing, in one line each:

| Library | License | Sagas | Built-in outbox | Transports | Also a mediator? |
|---|---|---|---|---|---|
| **MassTransit** | v8 OSS (Apache) · **v9+ commercial** | **Excellent** (state machines) | Yes (EF/Mongo) | RabbitMQ, ASB, SQS/SNS, ActiveMQ, Kafka rider | Yes |
| **Wolverine** | OSS (MIT) | Good (simpler) | **Yes, EF Core + Postgres/Marten** | RabbitMQ, ASB, SQS, Kafka, MQTT, TCP, GCP | **Yes (bus + mediator in one)** |
| **NServiceBus** | **Commercial** (Particular) | Excellent | Yes | RabbitMQ, ASB, SQS, etc. | Via patterns |
| **Rebus** | OSS (MIT) | Yes (basic) | Limited | RabbitMQ, ASB, SQS, etc. | Lightweight |
| **CAP** (DotNetCore.CAP) | OSS (MIT) | No (eventing) | **Outbox-native** | RabbitMQ, Kafka, ASB, Redis | No |
| **Brighter / Darker** | OSS (MIT) | Workflow-ish | Yes (outbox) | RabbitMQ, Kafka, SQS, etc. | Yes (command processor) |
| **Dapr** pub/sub | OSS (CNCF) | No | Per-component | Broker-agnostic (sidecar) | No |
| **No framework** | — | DIY | DIY | One broker SDK (e.g. `Confluent.Kafka`) | No |

Two axes are hiding in that table. One is **library vs infrastructure**: Dapr (and "raw SDK") aren't
peers of MassTransit — they're a different bet, pushing messaging out of the process and into a
sidecar/broker you operate. The other is **bus vs mediator**: Wolverine and Brighter collapse the
in-process dispatcher and the out-of-process bus into one tool, which matters precisely because
MediatR — the usual in-process half — also went commercial.

## The criteria that mattered (for *this* warehouse)

A comparison with no weights is a feature list. Ours had hard must-haves and softer wants, derived
from Parts I–II:

**Must-haves (a "no" eliminates the candidate):**
- **OSS / no per-seat cost** — a portfolio/teaching project; a paid bus is a non-starter.
- **Transactional outbox on EF Core + PostgreSQL** — post #16's whole point; we wanted it *built-in*,
  not hand-rolled.
- **RabbitMQ transport** — the broker the Aspire AppHost already runs.
- **OpenTelemetry** — non-negotiable for a 24/7 system (post #18).

**Wants (these break ties):**
- **Sagas / process managers** — the inbound and outbound flows (ASN→put-away, order→dispatch) are
  long-running. This is where MassTransit shines.
- **Low ceremony / one tool** — fewer moving parts; ideally the mediator too, since MediatR is out.
- **Healthy community / hireability** and **maintainer governance** (the "bus factor").

Run the field through the must-haves and it thins fast: NServiceBus (cost) and MassTransit v9 (cost)
fall to the OSS rule; CAP and Dapr don't do **sagas** the way our process managers need; Rebus and
Brighter survive but trail on the EF-Postgres outbox ergonomics and ecosystem. Two finalists remain:
**MassTransit v8** (free, frozen) and **Wolverine** (free, current).

## The finalists, head-to-head

| | MassTransit v8 | Wolverine |
|---|---|---|
| Cost / future | Free, but **the last OSS line** — no new features | Free, **actively developed** (the "Critter Stack") |
| Sagas | **Best-in-class** state machines | Good, lighter; fine for our flows, less battle-tested |
| Outbox on EF+Postgres | Yes | **Yes, first-class** (what #15 used) |
| Mediator (replaces MediatR) | Separate concern | **Built in** |
| Ceremony | More abstraction, more "magic" | Source-generated handlers, less boilerplate |
| Ecosystem / hiring | **Huge**, years of Q&A | Smaller but growing fast since 2025 |
| Governance risk | Mature org, but OSS line is EOL-ish | Smaller team (JasperFx) — a real bus-factor note |

> **Trade-off — the one place MassTransit clearly wins.** MassTransit's saga maturity is genuinely
> ahead, and our domain is saga-heavy. We weighed that against it being a *frozen* OSS line and against
> Wolverine giving us the outbox **and** the mediator in one OSS package. For a project that values
> "current and free" over "deepest orchestration today," Wolverine wins — but if this were an
> enterprise with budget and heavy orchestration, NServiceBus or commercial MassTransit would be the
> rational buy, and I'd not argue.

## The verdict — and its tripwire

**Wolverine.** It clears every must-have, collapses bus + mediator into one OSS dependency, and its
EF-Core/Postgres outbox is exactly the mechanism post #16 needed. The price we're paying is recorded,
not hidden: a smaller community, a smaller maintainer team, and saga support that's good rather than
great.

So the decision ships with a **tripwire** (in [PLAN.md](../PLAN.md) #10): *if the inbound/outbound
orchestration grows hairy enough that Wolverine's sagas fight us, revisit MassTransit's state
machines.* A decision with a written "revisit when…" is a decision, not a bet.

## How to run your own bake-off (the reusable part)

Copy the *method*, not the verdict:

1. **Write the must-haves first, from your system** — not from feature lists. Ours fell out of "OSS,
   RabbitMQ, EF-Postgres outbox, OTel, sagas." Yours will differ; a Kafka-and-Azure shop eliminates
   different candidates.
2. **Eliminate on must-haves before comparing features.** Most candidates die here. Don't score what
   you've already ruled out.
3. **Spike the finalists on one representative slice — not a hello-world.** For us that's the
   `ProductDefined` outbox slice (post #16) or one inbound-saga step: implement it in each finalist,
   in a branch, and read the *real* ergonomics — error handling, testing, the failure modes.
4. **Score against weighted criteria**, must-haves as gates, wants as tie-breakers.
5. **Write an ADR with the price tag and a tripwire.** The artifact that ages well isn't "we chose X"
   — it's "we chose X over Y because Z, and here's when to reconsider."

> **Trade-off — a bake-off costs time you could spend building.** Two spikes plus an ADR is a few
> days. We'd skip it for a reversible, low-stakes pick — but a messaging library is sticky (it touches
> every service and the wire format), so it clears the bar where a real evaluation pays for itself.
> The cheap version, when you must: trust the must-have gate and skip the spike.

## What's next

We can persist a rich domain, publish facts through a transactional outbox, and we've justified the
library that carries them. What we still can't do is **see** the system run — and a warehouse runs
around the clock. When a put-away is rejected at 3 a.m., you need to know *why* without attaching a
debugger. [**Post #18 — Logging & observability**](18-logging-and-observability.md): structured logs,
OpenTelemetry traces that follow a message across the outbox and the broker, and the
warehouse-specific question of what you must log — and what (a customer's address, an operator's
badge) you must never.

**Post #18: Logging & observability — seeing the system run →**
