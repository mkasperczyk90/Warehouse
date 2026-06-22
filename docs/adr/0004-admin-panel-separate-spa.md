# ADR-0004 — Admin panel as a separate browser-native React SPA

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** dev team
- **Related:** blog [#11](../blog/11-design-nfr-adr-and-design-system.md), [#12](../blog/12-from-design-system-to-screens.md), [#18](../blog/18-the-admin-panel-architecture.md); design [plan](../design/03-admin-frontend-plan.md); [ADR-0005](0005-shared-design-tokens.md)

## Context

The design work settled on **two front ends, one domain**: an Operator terminal for floor staff
(gloves, cold, glare — scanner-first, huge tap targets) and an Admin panel for the manager/coordinator
at a desk (dense tables, filters, bulk actions, master-data forms). The terminal already exists as a
React Native (Expo) app that also runs on the web via React Native Web.

The tempting "DRY" move is to extend that one codebase — build the admin panel out of React Native Web
too, sharing components and tooling. The alternative is a second, separate **browser-native React SPA**
that shares only the design tokens and the ubiquitous language, not the runtime or the component code.

## Decision

The Admin panel is a **separate React SPA in `src/web/admin`**, built for the browser with Vite +
TypeScript. It does **not** share React Native components with the terminal. It shares the design
**tokens** ([`tokens.css`](../design/prototypes/tokens.css), see [ADR-0005](0005-shared-design-tokens.md))
and the domain language, and it mirrors the terminal's *conventions* (feature-sliced folders, a single
API seam, a typed route map) without sharing its *code*.

## Consequences

- **Positive:** each front end uses the idioms its platform is best at — the admin gets real HTML
  `<table>` semantics, CSS Modules, and the mature browser-data ecosystem (TanStack Table/Query, React
  Hook Form), none of which fit RN-Web's primitives well. The two type scales, two interaction models
  and two themes (the terminal's high-contrast variant is meaningless at a desk) stop fighting each
  other inside one component tree.
- **Negative / the price:** two build pipelines, two dependency sets, and two places a shared primitive
  (`StatusBadge`, `QuantityWithUnit`) is implemented. We pay it knowingly — the shared token layer is
  what keeps the two from drifting into different products; the components are small and the duplication
  is bounded. This is the same trade-off blog #12 already accepted at the design level, now made real in
  code.
- **Revisit if:** the shared-primitive duplication grows past a handful of trivial components, or a
  genuine need appears to render the *same* screen on both platforms — then extract a platform-agnostic
  component package (its own ADR).
