# ADR-0007 — Vertical slices inside the Application layer (one folder per use-case)

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** dev team
- **Related:** blog [#13](../blog/13-repository-unit-of-work-and-events.md), [#18](../blog/18-the-admin-panel-architecture.md); [03-use-cases.md](../03-use-cases.md); [ADR-0001](0001-microservices-from-day-one.md); design [plan](../design/03-admin-frontend-plan.md) (the front end's `features/` convention)

## Context

Each service is split into **modules = bounded contexts** (Catalog, Partners, Inventory, Topology,
Logistics), and each module follows Clean Architecture layers — `Domain/`, `Application/`,
`Infrastructure/`. The module boundary already answers "where does this feature live?" at the macro
level. The open question is the *internal* shape of `Application/`, which today holds only
`Abstractions/` (repository interfaces); the use-case handlers don't exist yet.

There are two ways to organise the handlers as they arrive:

- **By technical type** — `Application/Commands/`, `Application/Handlers/`, `Application/Validators/`.
  A single use-case (`DefineProduct`) is then spread across three sibling folders.
- **By use-case (vertical slice)** — `Application/DefineProduct/` holding that use-case's command,
  handler, and validator together.

The front end deliberately organises by `features/` (one folder per screen) precisely so a feature is
found in seconds (design [plan](../design/03-admin-frontend-plan.md)). We want the same ergonomics on
the backend where we have the freedom to choose it.

## Decision

Inside `Application/`, organise **by use-case — one folder per slice** — not by technical type. A
use-case folder co-locates its command/query, handler, and validator (e.g.
`Application/DefineProduct/{DefineProductCommand, DefineProductHandler, DefineProductValidator}.cs`).

This applies **only within a module**. The slice does *not* cross the module boundary — a use-case
lives in exactly one bounded context. Anything shared across slices stays where it belongs:
cross-cutting **domain policies** (`PutAwayPolicy`, `AllocationPolicy`, …) remain in `Domain/Services`,
and repository ports remain in `Application/Abstractions`. This is the modular-monolith counterpart of
the front end's `features/`: same "find a feature fast" intent, applied per bounded context.

## Consequences

- **Positive:** a use-case is one folder — open it and everything that implements it is in front of
  you; adding a use-case is adding a folder, not touching three; the slices line up with the catalogued
  use cases (UC-01…UC-14) and read the same way as the front end's `features/`. Deleting a feature is
  deleting a folder.
- **Negative / the price:** the symmetry with the front end is *partial and intentional*, and that can
  mislead. The two tiers slice on **different axes** — the front end by screen/actor, the backend by
  bounded context + use-case — so one admin screen routinely fans out to several backend slices across
  modules (Stock view reads Inventory; Products writes Catalog; Inbound touches Logistics + Catalog).
  We do **not** force a 1:1 mapping; the Gateway/BFF composes across them. Pretending front-feature =
  backend-feature would breach the context boundaries Part I established.
- **Revisit if:** a module's `Application/` stays trivial (a handful of CRUD handlers with no behaviour),
  where the slice folders add ceremony without payoff — then a flat `Application/` is fine for that
  module; the convention is a default, not a mandate.
