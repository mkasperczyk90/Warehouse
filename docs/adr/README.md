# Architecture Decision Records (ADRs)

Short, immutable records of the **architecturally significant decisions** on this project —
one decision per file, in [Michael Nygard's format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).
The value isn't the choice; it's the **context and the "why"**, captured at the moment we had
it, so a future maintainer (or a future us) doesn't relitigate a settled question from scratch.

Rules we hold ourselves to:

- **One decision per file**, numbered, named, dated.
- **Immutable.** An ADR is never edited after it's `Accepted`. If we change our minds, we add a
  new ADR that **supersedes** the old one, and mark the old one `Superseded by ADR-XXXX`.
- **Significant only.** Library bumps and naming don't get an ADR; anything that's expensive to
  reverse, or that someone will later ask "why on earth did they…" about, does.
- Status is one of `Proposed · Accepted · Deprecated · Superseded`.

| # | Decision | Status | Blog |
|---|---|---|---|
| [0001](0001-microservices-from-day-one.md) | Microservices from day one (3 services, 5 contexts) | Accepted | [#2](../blog/02-why-we-start-with-the-domain.md), [#6](../blog/06-the-price-tag.md) |
| [0002](0002-stock-as-append-only-ledger.md) | Stock is a projection of an append-only ledger | Accepted | [#2](../blog/02-why-we-start-with-the-domain.md), [#4](../blog/04-archetypes-in-practice.md) |
| [0003](0003-replicas-over-cross-service-queries.md) | Local replicas instead of cross-service queries | Accepted | [#2](../blog/02-why-we-start-with-the-domain.md), [#3](../blog/03-bounded-contexts-and-use-cases.md) |
| [0004](0004-admin-panel-separate-spa.md) | Admin panel as a separate browser-native React SPA | Accepted | [#12](../blog/12-from-design-system-to-screens.md), [#18](../blog/18-the-admin-panel-architecture.md) |
| [0005](0005-shared-design-tokens.md) | One design-token layer (`tokens.css`) across both front ends | Accepted | [#11](../blog/11-design-nfr-adr-and-design-system.md), [#18](../blog/18-the-admin-panel-architecture.md) |
| [0006](0006-mock-at-the-network-boundary.md) | Mock the admin's backend at the network boundary (MSW) | Accepted | [#18](../blog/18-the-admin-panel-architecture.md) |
| [0007](0007-vertical-slices-in-application-layer.md) | Vertical slices inside the Application layer (one folder per use-case) | Accepted | [#13](../blog/13-repository-unit-of-work-and-events.md), [#18](../blog/18-the-admin-panel-architecture.md) |
| [0008](0008-automated-per-component-releases.md) | Automated, per-component releases (release-please + SemVer) | Accepted | — |
| [0009](0009-monorepo-ci-and-signed-images.md) | Monorepo CI — affected-only builds, aggregate gates, signed images | Accepted | — |

The remaining Part I decisions (the `StockItem` aggregate over `Warehouse`, one-aggregate-per-site
Topology, the small SharedKernel) are being captured the same way as the series continues.
