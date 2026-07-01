# #20 — A worklist, not a dashboard: designing the admin landing

*Series: Building a real microservices application, brick by brick.
Previous: [#19 Building the admin panel](19-building-the-admin-panel.md).
Design: [Today / Worklist prototype](../design/prototypes/admin-10-worklist.html) ·
[/docs/design](../design/README.md).*

---

Post #19 built the nine admin screens and then reviewed them honestly: a solid data browser, but the
landing was the stock table and there was no answer to the desk's first question of the day — *what
needs me now?* This post designs that answer. The interesting part isn't the screen; it's resisting the
reflex. The reflex, when someone says "we need a landing page with the important numbers," is to build a
**dashboard**: charts, trends, a wall of metrics. For a warehouse desk that would be the wrong tool.

> **Key takeaway:** a dashboard answers *"how are we doing?"*; a worklist answers *"what do I do next?"*
> A warehouse desk runs on the second.

## The distinction that decides the whole screen

A dashboard is for **analysis** — it's passive, retrospective, and you *look* at it: throughput this
week, fill rate, shrinkage trend. Useful to a planner once a week. A worklist is for **action** — it's a
queue of specific items that each need a decision *today*, and from each one you *do* something. The
warehouse manager and the logistics coordinator don't start their shift wanting a line chart; they start
it wanting the list of things blocking the floor: which batches are stuck in QC, which orders can't be
filled, what's about to expire, what's arriving at the dock.

So the landing is a **worklist**, and we said so on the screen itself — the subtitle reads "this is a
worklist, not a dashboard." BI (trends, metrics) stays deliberately deferred, exactly where post #12's
scope put it. This isn't pedantry; it's the difference between a screen people act from and a screen
people glance at and close.

> **Trade-off — a worklist dates fast and must stay honest.** A queue that shows "4 QC holds" when there
> are really 7 is worse than no queue, because people stop trusting it and go back to spreadsheets. The
> price of a worklist is that its counts have to be *live and correct*, every time. We pay it by deriving
> every queue from the same endpoints the detail screens already use — the worklist is a new *lens* on
> existing data, never a second copy that can drift.

## The queues come from the domain, not from imagination

The temptation with a landing page is to invent "useful" widgets. We didn't add a single new concept.
Every queue is an exception or a state the domain already models and a screen already shows:

| Queue | What it is | Clears on | Token |
|---|---|---|---|
| **QC holds** | batches in quarantine awaiting a decision (UC-03) | [QC worklist](../design/prototypes/admin-8-qc.html) | blocked (red) |
| **Expiring ≤ 7 d** | FEFO risk — batches near best-before | [Stock view](../design/prototypes/admin-1-stock.html) | expired (dark red) |
| **Partial / waiting orders** | outbound lines short of ATP — split or hold (UC-09) | [Outbound](../design/prototypes/admin-5-outbound.html) | reserved/transit |
| **Inbound today** | ASNs due at the dock now (UC-01) | [Inbound](../design/prototypes/admin-2-asn.html) | reserved (blue) |
| **Stocktakes to approve** | blind counts whose differences need posting (UC-07) | [Stocktake](../design/prototypes/admin-3-stocktake.html) | available (green) |

The status tokens do real work here, same as everywhere in this system: colour *is* urgency because
colour encodes domain status. The QC queue is blocked-red and the expiring queue is expired-dark-red
because those are the loudest, and they should be — they're what bites first.

## The shape: cards that are jobs, and a way to the rest

The [prototype](../design/prototypes/admin-10-worklist.html) (`admin-10`) has three moving parts:

- **Attention cards** — a row of counts, each a *count + what it is + the action it needs* ("4 · QC holds
  · batches awaiting a decision"). A count with a verb, not a number on a tile.
- **Worklist panels** — under the cards, the actual top few items per queue (the specific batches, orders,
  expiring lots, ASNs), each row linking to the record, with a **"View all →"** to the full screen.
- **Sidebar counters** — the same numbers as badges next to QC, Outbound, Stocktakes in the nav, so the
  signal follows you off the landing page.

Every card and every row is a link to the screen that resolves it. That's the whole point: the worklist
is a *router for attention*, not a destination. It closes the "screens are islands" half of #19's
critique — now there's a hub that points at all of them in priority order.

> **Trade-off — one worklist for several roles.** The manager, the coordinator and the QC inspector have
> overlapping but not identical queues. We designed *one* shared worklist showing all of them rather than
> three role-tailored ones. That's the right first cut — it's simpler, and in a single-warehouse pass the
> overlap is large — but it means a coordinator sees a QC card that isn't theirs. Per-role tailoring is a
> deliberate later slice, flagged as an open question for the PO.

## What's deliberately still a question

Two calls we did **not** make unilaterally, and wrote down for the product owner
([PLAN open questions](../PLAN.md#open-questions-for-po--clients)):

1. **Does the worklist replace the stock view as the default landing?** Probably yes for the manager — but
   that's a workflow decision, not a designer's to make alone.
2. **Per-role queues.** Which queues each role sees by default once identity/roles exist.

Designing the screen surfaced these cleanly; guessing them silently would have been worse than asking.

## What's next

The design is a prototype and a spec; the build is small precisely because there's no new data — the
counts and the top-N lists come from the endpoints the screens already call, assembled into one read.
That slots straight onto the admin's existing seam. And then we get back to the backend foundations the
panel will lean on: the next post is **logging & observability** — what to log in a warehouse, and what
never to.

**Post #21: From a data browser to a tool — closing the review's backlog →**
