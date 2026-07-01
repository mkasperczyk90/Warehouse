# #19 — Building the admin panel: nine screens, one seam, and an honest review

*Series: Building a real microservices application, brick by brick.
Previous: [#18 The admin panel: architecting the second front end](18-the-admin-panel-architecture.md).
Plan & specs: [/docs/design/03-admin-frontend-plan.md](../design/03-admin-frontend-plan.md). Backlog:
[admin `TODO.md`](../../src/web/admin/TODO.md).*

---

Post #18 made the decisions for the second front end and stopped there, on purpose — separate
browser-native SPA, shared tokens, mock at the network boundary, a headless library shelf. It ended
promising the next brick: *lay the foundation and the first slice — Phase 0 and the stock view.* This
post is what happened when we kept going past that brick and built **all nine designed screens**. Two
things came out of it, and the second is the more useful one: the architecture held up, **and** working
the screens revealed a gap that no prototype could have shown us.

> **Key takeaway:** a prototype proves the *flow*; only working the screens proves the *tool*.

## What got built

The full admin panel from the [design pass](../design/README.md) — nine screens across four slices:

| Slice | Screens | New pattern it bought |
|---|---|---|
| Stock view (UC-05) | stock | `DataTable` + `FilterBar` + `KpiCard` — most of `shared/ui` off one screen |
| Logistics | inbound (ASN), outbound, **dispatch** | master-detail, and a kanban `Board` |
| Exceptions & writes | stocktake, **adjustment**, QC | forms, bulk-approve, and an optimistic mutation |
| Master data | products, **topology** | a rich form, and a tree-nav |

`/` lands the manager on the stock view; the sidebar groups Inventory / Logistics / Master data;
Movements and Partners are dimmed because they have no screen yet. EN/PL throughout. 27 tests,
`tsc` clean, `vite build` clean.

## The architecture held up

The three recorded decisions from #18
([ADR-0004](../adr/0004-admin-panel-separate-spa.md)–[0006](../adr/0006-mock-at-the-network-boundary.md))
each paid off in the build, not just on paper:

- **One seam, no mock→real rewrite.** Every screen reads through `src/core/api/client.ts` and MSW
  answers at the network layer. Adding a screen meant adding a fixture handler, never touching the
  components' data path. Going live stays a switch, not a refactor.
- **Feature slices that read like the terminal.** Each screen is `XScreen.tsx` + `x.model.ts` (types +
  Query hooks) + `index.ts`. A new feature is a new folder; deleting one is deleting a folder. The
  fourth screen cost a fraction of the first.
- **Headless tables inheriting the tokens.** `DataTable` renders our own `<table>` with the design-
  system classes, so `status.blocked` red is the *same* red as the event-storming board — the rule the
  whole design rests on never had a chance to drift.
- **Validation that is the domain.** The adjustment form's rule is one line of Zod —
  `z.number().int().min(0, 'Quantity can never go below zero (invariant #3)')` — so invariant #3 is
  enforced on the glass before it can ever reach the ledger. The product form refuses an inverted
  temperature range the same way.

And one decision quietly closed a backlog item: the design's
[exceptions doc](../design/02-exceptions.md) had *"adjustment below zero — guard not visualized"* as a
🟡; the form now makes it a visible, blocking error.

> **Trade-off — building all nine against a mock risks gold-plating a guess.** Nine screens of UI for a
> backend that isn't wired yet is exactly the "designing for code that doesn't exist" risk #12 warned
> about. We bounded it the same way: the fixtures *are* the contract (the `x.model.ts` types), the seam
> is one file, and MSW means the async/loading/error behaviour is real from screen one — so when the
> Gateway arrives there's nothing to re-feel, only to point at.

A pattern worth keeping showed up too: **one optimistic mutation, done once, reused as a recipe.** QC
release/reject removes the batch from the list on click and rolls back on failure (`onMutate` /
`onError` / `onSettled` in `qc.model.ts`), against a *stateful* mock so the refetch agrees. That's the
template for every "act on a row" we'll add next.

## Then we worked the screens — and it's a data browser, not yet a tool

Here's the part a prototype can't tell you. Put on the hat of someone who has actually stood on a
warehouse floor, click around for ten minutes, and the panel stops feeling finished:

- There's **no product list.** `Products` is an edit form for *one* product — you can't browse, search,
  or create. That's the front door of master data, and it isn't there.
- The same shape repeats: **`Adjustment`** lands on a hard-coded item with no way to *find* one;
  **`Stocktake`** opens a single review with no list and no "start a count."
- **Tables are dead ends.** You can't click a stock row to see its batches, locations and reservations,
  or act on it (adjust / move / block).
- No **global search** ("where is SKU / batch / order X" — the desk's single most common act), no
  pagination on tables that show five rows, no export or print, no confirm/undo on irreversible posts,
  and **QC records a decision without a reason** even though the screen says every decision is audited.

None of that is a bug. Every screen matches its prototype faithfully. The gap is *between the prototypes
and a working tool* — and you only see it by working it.

## The lesson: a thin design pass hides the front doors

Post #12 built the screens as a deliberately **thin pass — "covering the real flows and nothing
speculative."** That was the right call; it's why we had screens to build at all. But thin has an edge,
and the build found it: the prototypes drew the *detail and the edit* of each flow and quietly skipped
the *list* and the *create* — the very things the use cases assume out loud.

- **UC-13** says *"Create/edit a ProductType."* We drew the edit; there's no catalogue and no create.
- **UC-01** says the coordinator *"creates an ASN"* and *"assigns a dock slot."* The ASN screen only
  lists and shows.
- **UC-09 / UC-12 / UC-07 / UC-14** — create order, assign carrier, start a stocktake, add a room —
  same story: the buttons render and do nothing, faithful to prototypes that never drew the flow behind
  them.

This is the honest counterweight to #18's optimism, and it's pure series DNA: *every decision shows its
price.* The price of a thin design pass is that the missing front doors are invisible until someone
builds the screens and tries to enter through them.

> **Trade-off — the review is cheap now and would be expensive later.** Surfacing "this is a browser,
> not a tool" after one build costs an afternoon and an honest paragraph. Surfacing it after a warehouse
> manager can't find a product costs trust. We pay the cheap version: we wrote the gaps down where the
> next slice will find them, rather than calling nine green screens "done."

## Where the gaps live now

Nothing here is swept under the rug; it's filed where the next person looks, sorted into three honest
buckets:

- **Already deferred by design** — BI dashboards, auth, external portals, Movements/Partners, the
  terminal's high-contrast theme ([design README scope](../design/README.md#scope)).
- **Under-covered use cases — the missing front doors** — the list/create slices above, framed in the
  [admin plan §11](../design/03-admin-frontend-plan.md#11-post-build-review--what-the-gap-closing-pass-delivered) and
  two new rows in the [exceptions doc](../design/02-exceptions.md).
- **Not yet in any doc — a PO call first** — a work-queue landing ("what needs me now", distinct from
  deferred BI) and global search, raised in
  [PLAN open questions](../PLAN.md#open-questions-for-po--clients).

The full prioritised checklist is the app's [`TODO.md`](../../src/web/admin/TODO.md); the docs hold the
framing and link down to it, so there's one backlog, not four.

## What's next

The admin panel is a complete, clickable spec of itself — running entirely on fixtures. The honest
backlog from this review is the next thread: the biggest gap — there's no *"what needs me now"* — gets
designed first. After that, back to the **backend foundations** the panel will lean on, and the moment
they meet: **turn MSW off** and the same `fetch` calls hit the real YARP Gateway, zero component changes,
exactly as [ADR-0006](../adr/0006-mock-at-the-network-boundary.md) promised.

**Post #20: A worklist, not a dashboard — designing the admin landing →**
