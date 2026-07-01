# #0 — How microservices are really built: a series

*Series: Building a real microservices application, brick by brick — the opener.
Full plan: [README.md](README.md).*

---

I want to show you how microservices are built **in real life**.

Not the conference-talk version, where three perfectly named services exchange events and
nothing ever fails. And not the tutorial version, where the domain is a to-do list and the
hard decisions are someone else's problem. The real version: a business that wants
something, a team that has to understand it, decisions that cost money whichever way you
take them — and only *then* the code, the pipelines and the deploys.

## What this series is

Over the coming posts I will build a complete system, in public, one brick at a time:
a **warehouse management system (WMS)** — multiple warehouses, cold rooms, batches with
expiry dates, deliveries, picking, dispatch. A real domain that real people work in,
chosen precisely because it pushes back: it has hard physical rules ("frozen goods only
in freezer rooms"), auditors, forklifts, and a business language worth learning.

I will walk the **entire professional process, start to finish**:

1. **Domain discovery** — talking to the business, event storming, finding the language,
   being corrected (the most valuable part), and writing down *why* behind every decision.
2. **Strategic design** — bounded contexts, what becomes a microservice and what
   deliberately doesn't, and the trade-offs we pay for each choice.
3. **Tactical design** — aggregates, value objects, archetype patterns, invariants that
   live in code instead of wiki pages.
4. **Building the services** — .NET 10, Clean Architecture per module, EF Core with a
   rich domain, the transactional outbox, sagas, API gateway, React frontends for two very
   different users (a manager at a desk and an operator with a scanner).
5. **Engineering the delivery** — CI pipelines, tests that guard architecture, contract
   tests between services, **security scanning**, container builds, deploys, observability
   — everything that turns "works on my machine" into "runs in production".

Nothing will be hand-waved. When we add RabbitMQ, you will already know which business
fact travels through it and why it must not be lost. When we add a security scan to the
pipeline, it will scan code you watched being written.

## Who this is for

Developers who are curious **what it actually looks like in a professional team** — the
whole process, end to end. You've written services, maybe followed a microservices
tutorial or two, and you're left with the questions tutorials skip:

- How do you know where one service ends and another begins — *before* it's too late?
- What do senior engineers actually argue about, and how do those arguments get settled?
- What does "understanding the business" concretely produce, besides meetings?
- What surrounds the code in real delivery — reviews, pipelines, scans, deploys?

If that's you, welcome.

## The rules I'll hold myself to

- **Domain first.** The first several posts contain no infrastructure at all. We earn the
  right to talk about brokers by first understanding what a pallet is.
- **Every decision shows its price.** No choice is presented as free — each gets an
  explicit trade-off, including the ones where the "default advice" would say otherwise
  (yes, we go microservices from day one, and I'll tell you exactly what that costs).
- **The repository is the single source of truth.** Every post links to real code and
  docs in [this repo](../PLAN.md); diagrams are Mermaid committed next to the text.
  If the code changes, the post gets a changelog note.
- **The business stays in the room.** Expect yogurt, forklifts and auditors in every
  post — if a rule can't be explained with a pallet, it probably shouldn't be in the code.

## The roadmap

**Part I — The domain** (published):

| # | Post |
|---|---|
| 1 | [What is Event Storming](01-what-is-event-storming.md) — the colour grammar, the three zoom levels, and why a wall beats a requirements doc |
| 2 | [Why we start with the domain, not with Docker](02-why-we-start-with-the-domain.md) — applying the method, the big picture, strategic decisions, and a one-page DDD building-blocks primer |
| 3 | [Five contexts, three services](03-bounded-contexts-and-use-cases.md) — a guided tour of every bounded context |
| 4 | [The aggregate: where to draw the lines](04-the-aggregate-where-to-draw-the-lines.md) — the four rules, how to find the boundary, and what it costs to draw it wrong |
| 5 | [Archetypes: pre-paid modeling decisions](05-archetypes-in-practice.md) — Party/Role, Quantity, the ledger, and their price tags |
| 6 | [The SharedKernel: the most dangerous package](06-the-shared-kernel.md) — what we share, what we deliberately duplicate |
| 7 | [The "Price Tag" revisited](07-the-price-tag.md) — where the model might creak, admitted up front |
| 8 | [Wrapping up Part I](08-wrapping-up-part-i.md) — the retrospective before we touch technology |

**Part II — From understanding to delivery** (this batch): turning the whiteboard into a plan,
a design, and then running code. First the plan and design — user story mapping (epics, stories,
tasks, the walking skeleton), then the design pass (functional & non-functional requirements, ADRs,
Clean Architecture, the diagrams worth keeping, and a first design system for the two frontends).
*Then we open the IDE* and lay the foundations: the seam between the domain and the database — the
**Repository, the Unit of Work and Domain Events** — followed by persisting a rich domain in EF Core
10, Aspire wiring the system into one `dotnet run`, the transactional outbox, and observability.

**Part III — Brick by brick** (next): features end to end — master data with a React panel,
the first integration event, inbound and outbound sagas, handling units, stocktakes —
each brick adding one architectural concept. And around it all: CI/CD per service,
architecture & contract tests, security scans, deploys.

The full, evolving plan lives in [README.md](README.md).

---

Let's start where every real project starts — not with `docker compose up`, but with a
conversation at a wall of sticky notes. **[Post #1 →](01-what-is-event-storming.md)**
