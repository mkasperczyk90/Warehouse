# #18 — The admin panel: architecting the second front end

*Series: Building a real microservices application, brick by brick.
Previous: [#17 Choosing a messaging library](17-choosing-a-messaging-library.md).
Plan & specs: [/docs/design/03-admin-frontend-plan.md](../design/03-admin-frontend-plan.md). Decisions
recorded in [ADR-0004](../adr/0004-admin-panel-separate-spa.md),
[ADR-0005](../adr/0005-shared-design-tokens.md), [ADR-0006](../adr/0006-mock-at-the-network-boundary.md).*

---

Post [#12](12-from-design-system-to-screens.md) drew the screens — nine `admin-*` prototypes plus the
terminal — and promised the React build would come "in Part III, the admin panel around post #18". This
is that post, but it stops short of code on purpose: before opening the IDE on a *second* front end, the
honest move is to write down the architecture and the decisions behind it, the same way we wrote down
the domain before touching EF Core. This is the structure of `src/web/admin`, and — more usefully — *why*
it's shaped that way.

The terminal already gave us a template. So the interesting questions aren't "React or not" — they're the
three places the admin panel **deviates** from the terminal, and why each deviation is deliberate.

## The one decision that frames the rest: a second app, not a bigger one

We already have a React Native (Expo) terminal that runs on the web via React Native Web. The DRY instinct
says: extend it. Build the admin panel out of RN-Web too, share the components, share the tooling, one
codebase.

We didn't. The admin panel is a **separate, browser-native React SPA** ([ADR-0004](../adr/0004-admin-panel-separate-spa.md))
that shares the *design tokens* and the *ubiquitous language* with the terminal — and nothing else at
runtime.

The reason is the same one that justified two front ends in the first place (post #11): these are **two
products for two users**. The terminal is one task at a time, a scan field that's always focused, three
56 px buttons, and a high-contrast theme for glare on a cold-store floor. The admin panel is the opposite
— a 10,000-row stock table, filter pills, bulk actions, master/detail forms, and a high-contrast theme
would be meaningless at a desk. Pretending one component tree serves both is the classic enterprise
mistake. And at the *technology* level the split pays off concretely: the admin wants real HTML `<table>`
semantics, CSS Modules, and the mature browser-data stack (TanStack Table, TanStack Query, React Hook
Form) — none of which sit comfortably on React Native Web's primitives.

> **Trade-off — two front ends mean two of everything.** Two build pipelines, two dependency sets, and a
> shared primitive like `StatusBadge` implemented twice. We pay it knowingly. The mitigation is the same
> as the design's: a thin **shared token layer** keeps the two from drifting into different products, and
> the duplicated primitives are small and dumb. If that duplication ever grows past a handful of trivial
> components, we extract a platform-agnostic package — but not before we feel the pain.

What the admin panel *does* borrow from the terminal is its **conventions**, not its code: feature-sliced
folders (`features/Stock/`, `features/Inbound/`…), a single API seam, a typed route map. A developer who
knows one front end can read the other.

## Deviation #1: tokens are consumed, not ported

Colour in this system is never decoration — it encodes domain stock status (`available`, `reserved`,
`blocked`/QC, `expired`, `in-transit`), and `status.blocked` red has to mean the same red on the
event-storming board, in the terminal, and in the admin panel. The tokens already live as the single
source of truth in [`tokens.css`](../design/prototypes/tokens.css).

The terminal had to **copy** them — it ported `tokens.css` into a `tokens.ts` because React Native has no
CSS. Every copy is a chance to drift, and drift here breaks the one rule the whole design rests on.

The admin panel doesn't copy in that sense. It runs in the browser, so `tokens.css` *is* the consumable
format — it's vendored in and imported directly, never re-authored ([ADR-0005](../adr/0005-shared-design-tokens.md)).
Both the terminal's `tokens.ts` and the admin's vendored CSS are **derived artifacts** of the one source
file. The admin pays *zero* porting cost.

> **Trade-off — a vendored copy needs a sync rule.** Keeping the app self-contained (buildable in
> isolation, no import across the repo boundary) means it carries a copy of `tokens.css`, and a stale
> copy is a real failure mode. We accept it because the sync is mechanical and CI can check it; the prize
> is that the load-bearing status colours physically cannot drift between the two apps.

This is also why we reuse the prototypes as a *spec*, not just inspiration. Each `admin-N-*.html` is a
finished layout — same DOM structure, same class names (`.side`, `.kpi`, `.panel`, `.filters`,
`.badge--*`) — translated 1:1 into a React component. The design isn't reinterpreted in code; it's
implemented.

## Deviation #2: async from the first line, mocked at the network

The terminal mocks its backend with an in-app synchronous helper: `read<T>(resource, fixture)` returns a
fixture object directly, to become a real `fetch` later. That's the right call for the terminal — its
screens are single-task and barely data-driven.

It would be the wrong call for the admin panel. Here the loading and error states aren't edge cases —
they *are* the product: loading a big stock table, a reason-bearing write that the ledger rejects, an
optimistic adjustment that has to roll back. A synchronous always-succeeds mock makes those states
**absent by construction**, to be retrofitted across nine screens when the Gateway arrives.

So the admin calls `fetch` from day one, through a single `core/api/client.ts` seam, with **Mock Service
Worker (MSW)** intercepting at the network layer and returning fixtures in dev and tests
([ADR-0006](../adr/0006-mock-at-the-network-boundary.md)). TanStack Query sits on top, so components
consume real `loading` / `error` / `data` whether MSW or the real `.NET` Gateway answers.

```
component → Query hook (x.model.ts) → core/api/client.ts (fetch /api/…)
                                          │ (dev / test)   → MSW intercepts → fixture
                                          │ (prod, MSW off) → real Gateway (YARP)
```

Going live is **turning MSW off**, not rewriting components. The same fixtures back the dev server and the
Vitest suite, and we can test idempotency and optimistic updates against *simulated* failures.

> **Trade-off — more setup than a one-liner.** A service worker, request handlers, fixtures per resource,
> an extra dev/test dependency. We pay it once, at the start, instead of paying it nine times in retrofit.
> The admin's whole value is correct behaviour under real network conditions, so we buy that behaviour up
> front.

## Deviation #3: the library shelf the browser earns

The terminal's stack is dictated by Expo. The admin gets to pick the browser's best-in-class data tools,
and the choices all point the same way — **headless libraries that respect the tokens we already have**:

| Concern | Choice | The reason in one line |
|---|---|---|
| Build | **Vite + TS**, React 19 | Instant HMR; React 19 matches the terminal. |
| Routing | **TanStack Router** | The admin is URL-driven (filters, selection in query params) — typed search params fit exactly. |
| Server-state | **TanStack Query** | Cache, loading/error, optimistic writes — slots onto the `fetch` seam. |
| Tables | **TanStack Table** (headless) | We render our own `<table>` with the existing token classes; no imposed look. |
| Forms | **React Hook Form + Zod** | The Zod schema is the single source of validation rules that mirror domain invariants (qty ≥ 0, reserve ≤ available). |
| Styling | **CSS Modules + `tokens.css`** | Component scope, no CSS-in-JS runtime, faithful to the prototypes. |
| Icons | **lucide-react** | Inline-SVG `currentColor` — the same model as the terminal's `Icon`; retires the sidebar's placeholder glyphs. |

Notice what's **not** here: none of these is an ADR. Per our own [ADR rules](../adr/README.md), "library
bumps and naming don't get an ADR; anything that's expensive to reverse does." Swapping TanStack Router
for React Router 7 is a contained change behind the route map; swapping a separate-SPA decision is not.
The three things that *are* recorded — separate SPA, shared tokens, network-boundary mock — are the ones
a future maintainer would otherwise relitigate from scratch.

> **Trade-off — headless means we build the chrome.** TanStack Table gives us sorting and selection logic
> but no pixels; we render the markup. That's the point — it's how the table inherits `tokens.css` instead
> of fighting a library's opinions — but it is more code than dropping in a styled grid. For a design
> where colour carries domain meaning, owning the markup is the cheaper long-run bet.

## The shape, and the build order

The structure mirrors the terminal so the two read alike — feature-sliced, one seam, a typed route map.
The [full plan](../design/03-admin-frontend-plan.md) has the tree; the spine is `features/*` (one folder
per screen, each `XScreen.tsx` + `x.model.ts` + `index.ts`), `shared/ui` (the primitives + `DataTable`,
`FilterBar`, `KpiCard`), `shared/layout` (the sidebar + top bar shell), and `core/api` (the seam).

The build goes in slices, not all-at-once:

0. **Foundation** — the shell, providers, routing, MSW, the seam.
1. **Stock view (UC-05)** — the manager's landing, and the screen that *builds almost all of `shared/ui`*:
   `DataTable`, `FilterBar`, `KpiCard`, `StatusBadge`, `QuantityWithUnit`. The reference screen.
2. **Coordinator logistics** — Inbound (ASN) → Outbound → Dispatch board.
3. **Exceptions & ledger writes** — Stocktake → Adjustment → QC worklist (reason-bearing forms,
   optimistic mutations, the qty ≥ 0 guard).
4. **Master data** — Products (master/detail) → Topology (the location tree).

One screen, `Stock`, pays for most of the component kit. That's deliberate — it's the same "walking
skeleton first" instinct from the story map (post #9), applied to the front end.

## What's next

We have the plan and the decisions for the second front end; the next brick is to lay the foundation and
the first vertical slice — Phase 0 and the Stock view — against the mocked Gateway. And on the backend
side, Part III opens the feature work the admin panel will eventually talk to: **master data — the first
vertical slices** — and the first integration event, `ProductDefined`, flowing into Inventory's
`ProductSnapshot`. The two ends will meet at the seam this post just specified.

**Part III: Master data — first vertical slices, and the admin panel meets the Gateway →**

---

*Update (post-build): the foundation and all nine screens are now built. See
[#19 Building the admin panel](19-building-the-admin-panel.md) — it pays off this post's plan and
reviews where the build (and the deliberately thin design pass) stopped short of a working tool.*
