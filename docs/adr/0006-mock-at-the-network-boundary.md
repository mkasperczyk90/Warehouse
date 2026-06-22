# ADR-0006 — Mock the admin's backend at the network boundary (MSW), not in-app

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** dev team
- **Related:** blog [#18](../blog/18-the-admin-panel-architecture.md); design [plan](../design/03-admin-frontend-plan.md); [ADR-0001](0001-microservices-from-day-one.md) (the Gateway); [ADR-0004](0004-admin-panel-separate-spa.md)

## Context

The admin panel is being built before its backend slices exist behind the Gateway, so it needs mock
data. The terminal solved this with an **in-app synchronous mock**: a `read<T>(resource, fixture)`
function that returns a fixture object directly, to be turned into an async `fetch` later. That keeps
the first screens simple, but it has a cost — every component is written against a synchronous, always-
succeeds data source, so loading states, error states and async edge cases are *absent by construction*
and have to be retrofitted when the real Gateway arrives.

The admin panel is a data-dense app where those states are not edge cases — they are the product
(loading a 10k-row stock table, a failed reason-bearing write, an optimistic adjustment that the ledger
rejects). We want them real from the first screen.

## Decision

The admin panel calls **`fetch` against the Gateway from day one**, through a single
`core/api/client.ts` seam, with **Mock Service Worker (MSW)** intercepting those requests *at the
network layer* in development and tests and returning fixtures. There is no synchronous in-app mock.
TanStack Query sits on top of the seam, so components consume real `loading` / `error` / `data` states
regardless of whether MSW or the real Gateway answers.

## Consequences

- **Positive:** there is **no mock→real rewrite** — going live is turning MSW off, not editing
  components; loading/error/async states are exercised from the first screen instead of retrofitted; the
  *same* fixtures back both the dev server and the Vitest suite; idempotency and optimistic-update
  behaviour can be tested against simulated failures.
- **Negative / the price:** more setup than a one-line synchronous mock — a service worker, request
  handlers, fixtures wired per resource — and the team carries an extra dev/test dependency. Accepted
  because the admin's whole value is correct behaviour under real network conditions, and paying that
  setup once is cheaper than retrofitting async states across nine screens. (The terminal's simpler
  in-app mock stays as-is; its screens are single-task and far less data-driven — the trade-off lands
  differently there.)
- **Revisit if:** the Gateway contracts stabilise enough that a generated client + contract tests would
  serve better than hand-written handlers — then generate the mock from the OpenAPI/contract (its own
  ADR).
