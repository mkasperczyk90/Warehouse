# #21 — From a data browser to a tool: closing the review's backlog

*Series: Building a real microservices application, brick by brick.
Previous: [#20 A worklist, not a dashboard](20-a-worklist-not-a-dashboard.md).
Backlog: [admin `TODO.md`](../../src/web/admin/TODO.md) · screens: [/docs/design](../design/README.md).*

---

Post #19 built the admin panel and then said the uncomfortable thing: it was a solid **data browser, not
yet a tool**. The screens showed data beautifully and led nowhere — no way to find a product, no way to
act on a stock row, irreversible posts on a single click. Post #20 designed the biggest missing piece, the
worklist landing. This post is the rest of the backlog, closed — and what "making it a tool" actually
turned out to be.

> **Key takeaway:** a tool is a browser plus two thin layers — the **front doors** you enter through and
> the **actions** you take — over a domain that was already a tool.

## The shape of the gap

When you list what a data browser is missing, it looks like a long backlog. When you build it, it
collapses into two patterns repeated:

**Front doors — how you get *in*.** The prototypes drew the *detail* of each flow and skipped the *list*
and the *create*. So we added them: a [product catalogue](../design/prototypes/admin-4-product.html) with
search and "+ New product" in front of the edit form; a stocktake **list** with "Start count" in front of
the review; a stock-item **drill** behind every stock row. The use cases assumed these all along (UC-13
literally says "create/edit"); the thin design pass just hadn't drawn them.

**Actions — what you *do*.** A browser shows; a tool changes things. So the stock drill grew row actions —
**Adjust**, **Move**, **Block** — and the reason-bearing writes (QC decisions, ledger adjustments) grew a
**confirm** step instead of firing on one click.

One small shared change unlocked most of the front doors: a single `onRowClick` on the headless
`DataTable`. Rows in the catalogue open the editor, rows in stock open the drill, rows in the stocktake
list open the review — the same three lines, three places. Headless tables paid off exactly as #18
predicted: the behaviour lives in one primitive.

> **Trade-off — building actions against a mock.** The actions are real — optimistic cache updates with
> rollback (QC), a confirm dialog that summarises the change (adjustment), an invariant that disables the
> button (move) — even though the data source is still MSW fixtures. That's the point of the network-
> boundary seam ([ADR-0006](../adr/0006-mock-at-the-network-boundary.md)): everything *except* the data is
> production-shaped, so the only thing left when the Gateway arrives is to stop mocking. The risk we
> accept is that a stateful mock can drift from real backend semantics; we bound it by keeping the mock's
> writes trivial (flip a status, move a string) and the *contracts* — the `x.model.ts` types — the real
> spec.

## The invariant that showed up on the desk

The most satisfying moment wasn't a feature; it was a domain truth re-appearing where we didn't plan it.

On the operator terminal, put-away has a hard stop: you cannot store a dairy pallet in an ambient room
(the environment-compatibility invariant, blog [#12](12-from-design-system-to-screens.md)). When we built
**Move** on the admin's stock drill — a manager relocating stock — the *same* rule had to be there. Pick an
ambient location for a cold item and the confirm button goes dead with a red "✗ Incompatible — Cold room →
Standard hall A". We didn't decide to add that; the invariant *is* the domain, so it surfaces wherever
stock moves, on whichever front end. The terminal and the admin enforce the same "never" because it isn't
a UI rule — it's a domain rule wearing two different UIs.

> **Trade-off — the same check in two places is not the same authority.** The client disables the button;
> that's an *affordance*, fast feedback so the manager doesn't submit a doomed move. It is **not** the
> guard. The real guard is server-side in `PutAwayPolicy` / the move service, off the local replica
> ([ADR-0003](../adr/0003-replicas-over-cross-service-queries.md)). Duplicating the check on the client is
> a UX convenience we pay for with a second place the rule is written — accepted, because the server stays
> the authority and a stale client check can only ever be *more* cautious, never less.

## The rest, briefly

The remaining backlog items were each small once the patterns were there:

- **Global search** — a top-bar command bar over `GET /api/search`, matching products, stock, ASNs,
  orders, shipments and locations, and jumping to the hit. The desk's "where is X", finally answered in
  one place instead of per-table.
- **Pagination + sort + a row count** — lifted into the shared `DataTable` (TanStack's sorted/paginated row
  models), so every table scales past the five-row fixtures to a real 10k-SKU site, for free.
- **The worklist landing** (#20) — built: attention cards + queues + sidebar counters, all from one
  `GET /api/worklist` that's a *lens* on existing data, with a live QC count.

Nine screens became thirteen routes, four shared primitives gained an action or a mode, and the test
count went from 27 to 45 — all still on fixtures, `tsc` clean, one `vite build`.

## What a tool turned out to be

None of this added a domain concept. There's no new aggregate, no new event, no new rule. "Making it a
tool" was almost entirely **surfacing** what the domain already had: turning states into entry points
(a list per thing), turning behaviours into actions (a button per command), and adding a hub that routes
attention (the worklist). The domain was a tool the whole time; the first UI pass just hadn't opened the
doors.

That's the honest end of the admin arc that started at #18. The thin design pass (#12) got us screens fast
and hid the doors; building them (#19) found the gap; designing (#20) and closing it (here) turned the
browser into something you could work a shift in.

## What's next

The thin tail of the backlog stays thin and written down (an undo window, CSV export). The bigger thread
goes back to the backend the panel has been mocking all along: the next post is **logging &
observability** — what to log in a warehouse, and what never to — and then Part III, where the admin's
`fetch` calls finally meet the real Gateway and MSW gets switched off for good.

**Post #22: Logging & observability — what to log in a warehouse, and what never to →**
