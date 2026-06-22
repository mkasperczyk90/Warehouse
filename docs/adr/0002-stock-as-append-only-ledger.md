# ADR-0002 — Stock is a projection of an append-only ledger

- **Status:** Accepted
- **Date:** 2026-05-21
- **Deciders:** dev team + Warehouse Manager (client SME)
- **Related:** blog [#2](../blog/02-why-we-start-with-the-domain.md) (Decision 2), [#4](../blog/04-archetypes-in-practice.md) (Moment-Interval archetype)

## Context

The warehouse's entire value proposition is "the numbers are right, and we can prove how they
got that way" — auditors visit twice a year. During event storming the warehouse manager was
emphatic: *"We never edit a stock number. Ever. We add a correction."* A naive design stores a
current quantity per `StockItem` and `UPDATE`s it on every move — fast, but it destroys history
and makes an off-by-one impossible to explain after the fact.

## Decision

Model every change of stock as an immutable **`StockMovement`** (who, what, from, to, when, why).
**Current quantities are a projection of movements**, not a stored truth. Corrections are
*reversing movements*, never edits. The ledger table rejects `UPDATE`/`DELETE` at the database
level. Domain behaviours **return** the movement to persist, so the application layer cannot
change stock without also writing the ledger entry (aggregate + movement in one transaction).

## Consequences

- **Positive:** a complete, tamper-evident audit trail; trivially debuggable history; movements
  double as natural integration events.
- **Negative / the price:** more rows; the projection must never drift from the ledger (made
  structurally hard, not merely policy). This is event-sourcing-*flavoured* thinking **without**
  full event sourcing — deliberately. We get auditability without snapshotting, event upcasting,
  or the steeper onboarding of full ES.
- **Revisit if:** we need temporal replay or projection rebuild at a scale where the "flavoured"
  approach stops paying — then a move to full event sourcing gets its own ADR.
