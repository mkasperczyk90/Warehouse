# UX & Screen Design

> How the [actors](../01-domain-overview.md#3-actors-our-clients--users) actually *use* the
> system — walked through screen by screen. This folder turns the
> [use cases (UC-01…UC-14)](../03-use-cases.md) and the design baseline from
> [blog #11](../blog/11-design-nfr-adr-and-design-system.md) into concrete screens.

## The one decision that shapes everything: two front ends

We have **two products, one domain, two wildly different users** (blog #11). Pretending one UI
serves both is the classic enterprise mistake, so every actor below lands on exactly one of:

| Front end | For | Shape |
|---|---|---|
| **Operator terminal** | floor staff on a rugged handheld — *one hand, gloves, cold room, glare* | scanner-first, huge tap targets (≥48 px), one task at a time, zero prose |
| **Admin panel** | manager / coordinator at a desk | data-dense tables, filters, bulk actions, master-data forms |

Same [ubiquitous language](../01-domain-overview.md#4-ubiquitous-language-glossary); two skins.
The shared rule that drives every token: **colour is never decoration — it encodes stock
status** (`available`, `reserved`, `blocked`/QC, `expired`, `in-transit`).

## Actor → front end → screens

| Actor | Front end | Use cases | Designed screens |
|---|---|---|---|
| [Warehouse Operator](actors/warehouse-operator.md) | Terminal | UC-02, 04, 06, 10, 11 | Task hub · Goods receipt · Put-away · Picking · Packing · Move stock |
| [Forklift Operator](actors/forklift-operator.md) | Terminal | UC-04, 06 | Put-away · Move stock |
| [Quality Inspector](actors/quality-inspector.md) | Terminal + Admin | UC-03 | QC worklist (+ receipt & stock-view affordances) |
| [Warehouse Manager](actors/warehouse-manager.md) | Admin | UC-05, 07, 08, 13, 14 | Stock view · Stocktake · Adjustment · Product master data · Topology |
| [Logistics Coordinator](actors/logistics-coordinator.md) | Admin | UC-01, 09, 12 | Inbound (ASN) · Outbound orders · Dispatch board |
| [Supplier / Customer / Carrier / ERP](actors/external-actors.md) | API / future portal | UC-01, 09, 12 | Integration touchpoints (no UI in pass 1) |

All 14 use cases now have at least one screen. The external actors interact via API/events; their
self-service portals are deliberately deferred (see their doc).

**Plus one cross-cutting screen.** A [Today / Worklist](prototypes/admin-10-worklist.html) landing
(`admin-10`) was added after the first admin build surfaced the gap (see
[03-admin-frontend-plan.md §11](03-admin-frontend-plan.md#11-post-build-review--what-the-gap-closing-pass-delivered)).
It belongs to no single use case: it's the desk's *"what needs you now"* — actionable queues (QC holds,
expiring ≤7 d, partial orders, inbound today, stocktakes to approve) that each link to the screen that
clears them, with matching sidebar counters. It is deliberately **not** a BI dashboard (trends/metrics
stay deferred) — it's a worklist.

### Use-case → screen coverage

| UC | Screen | UC | Screen |
|---|---|---|---|
| UC-01 ASN | `admin-2-asn` | UC-08 Adjustment | `admin-9-adjustment` |
| UC-02 Receipt | `terminal-2-receive` | UC-09 Outbound | `admin-5-outbound` |
| UC-03 Quality | `admin-8-qc` | UC-10 Picking | `terminal-4-pick` |
| UC-04 Put-away | `terminal-3-putaway` | UC-11 Packing | `terminal-6-pack` |
| UC-05 View stock | `admin-1-stock` | UC-12 Dispatch | `admin-6-dispatch` |
| UC-06 Move | `terminal-5-move` | UC-13 Products | `admin-4-product` |
| UC-07 Stocktake | `admin-3-stocktake` | UC-14 Topology | `admin-7-topology` |
| — | `terminal-1-hub` (operator landing) | | |

## Artifacts

- **[01-flows.md](01-flows.md)** — the goods lifecycle across actors + a screen-by-screen
  clickpath diagram for every actor. Start here for the *flow*.
- **[02-exceptions.md](02-exceptions.md)** — the unhappy paths: business exceptions, system &
  operational failures, the invariants the system refuses to break, and open questions.
- **[00-design-system.md](00-design-system.md)** — tokens, type scales, component inventory.
- **[prototypes/](prototypes/)** — runnable HTML mockups (the source the Figma frames were
  captured from). They are the design *source of truth* and double as a token reference.
- **Figma file** — [Warehouse WMS — UX](https://www.figma.com/design/xAzdWqmAOd3b2ZKWlU0TgR)
  (frames captured from the prototypes). Per-screen deep links live in each actor doc.

### Viewing the prototypes locally

```bash
cd docs/design/prototypes
python -m http.server 8731
# then open http://localhost:8731/  → index.html links every screen
# terminal screens render at 390 px wide; admin screens at 1440 px
```

The screens are **walkable**: [`index.html`](prototypes/index.html) links them all, and within the
mockups the admin sidebar, the terminal task hub cards, the bottom nav and the back arrows all
navigate (wired by [`nav.js`](prototypes/nav.js)). Menu items with no screen yet (e.g. Movements,
Partners) are dimmed rather than dead. You can also just double-click any `.html` file — navigation
works over `file://` too.

The prototypes go beyond navigation in a few places and are **interactive**: [Goods receipt](prototypes/terminal-2-receive.html)
has a working **numeric keypad** (tap the counted number; "= expected" one-tap default), the
[Task hub](prototypes/terminal-1-hub.html) network chip **toggles online/offline** to show the
queued-sync state, and **every terminal screen** carries a ◐ **high-contrast (glare)** toggle in
the bar that's remembered per device.

### Re-capturing / adding Figma frames

Frames are produced with the Figma MCP `generate_figma_design` capture flow. To (re)capture a
screen: add `<script src="https://mcp.figma.com/mcp/html-to-design/capture.js" async></script>`
to the page's `<head>`, then open it with the capture hash the MCP returns. The capture toolbar
that appears also lets you re-capture other pages manually. *(The first pass was cut short by the
Figma Starter-plan MCP rate limit — see the per-actor docs for which frames are confirmed.)*

## Scope

All 14 use cases are now covered by at least one screen across the two front ends (15 screens +
the operator Task hub). Still deliberately **out of scope** for now: external-actor self-service
portals (API/events suffice — see their doc), authentication/identity (generic, off-the-shelf),
and analytics/BI dashboards. These grow per slice, matching the [roadmap](../PLAN.md#roadmap).
