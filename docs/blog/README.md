# Blog series: Building a real microservices application, brick by brick

**Series title (used in every post):** *Building a real microservices application, brick by brick*
**Subtitle:** *a warehouse system, from a whiteboard to production*

**Audience:** developers who are curious what building an application looks like in a
professional team — the whole process, end to end: talking to the business, carving the
domain, making (and paying for) architectural decisions, then adding infrastructure one
brick at a time.

**Premise of the series:** most microservices tutorials start with `docker-compose up` and
a to-do list. Real projects start with a conversation about the business. So does this
series — the first five posts contain **no infrastructure at all**. We earn the right to
talk about RabbitMQ by first understanding what a pallet is.

**The repo is the single source of truth.** Every post links to code and docs in this
repository; diagrams live next to the text as Mermaid (versionable, reviewable like code).

---

## Part I — The domain (this batch)

| # | File | Topic | Key takeaway |
|---|---|---|---|
| 0 | [00-series-intro.md](00-series-intro.md) | Series opener: what we're building, for whom, the end-to-end process (discovery → design → build → pipelines/security/deploy) and the rules of the series | Real projects start with a conversation, not `docker compose up` |
| 1 | [01-what-is-event-storming.md](01-what-is-event-storming.md) | What Event Storming is: the colour grammar, the three zoom levels (Big Picture → Process → Design), how a session runs, and why a wall beats a doc | The deliverable isn't the board — it's the corrections |
| 2 | [02-why-we-start-with-the-domain.md](02-why-we-start-with-the-domain.md) | Why a warehouse, why domain-first, the big picture (event-storming board), strategic decisions and their trade-offs, plus a one-page DDD building-blocks primer (entity, value object, aggregate, shared kernel…) | Architecture is a set of paid-for decisions, not a diagram |
| 3 | [03-bounded-contexts-and-use-cases.md](03-bounded-contexts-and-use-cases.md) | Each bounded context in detail, use cases, how contexts talk to each other | Contexts share language (IDs, events), never types |
| 4 | [04-the-aggregate-where-to-draw-the-lines.md](04-the-aggregate-where-to-draw-the-lines.md) | A deep dive on the aggregate root: the four rules, how to find the boundary, why `StockItem` is tiny and `WarehouseSite` is big, and the common smells | The aggregate is a decision about what must be true at the same time |
| 5 | [05-archetypes-in-practice.md](05-archetypes-in-practice.md) | The archetype patterns we used (Party/Role, Description, Quantity, Moment-Interval, Rule) and what each one cost us | Archetypes are pre-paid modeling decisions — know their price |
| 6 | [06-the-shared-kernel.md](06-the-shared-kernel.md) | What made it into the SharedKernel, what was deliberately kept out, and why duplication can be a feature | The smaller the shared kernel, the safer the boundaries |
| 7 | [07-the-price-tag.md](07-the-price-tag.md) | A cold look at the "receipt" for Part I's decisions — where the model might creak under 10k pallets | Every decision shows its price, including ours |
| 8 | [08-wrapping-up-part-i.md](08-wrapping-up-part-i.md) | Part I retrospective: language, boundaries, patterns, and the trade-offs we accepted knowingly | We earned the right to talk about technology |

## Part II — From understanding to delivery (this batch)

| # | File | Topic | Key takeaway |
|---|---|---|---|
| 9 | [09-story-mapping-epics-and-tasks.md](09-story-mapping-epics-and-tasks.md) | User story mapping: backbone, slices, epics → stories → tasks, the walking skeleton, and how the map sets the build order | A backlog is one-dimensional; a map shows shape |
| 10 | [10-architectural-drivers.md](10-architectural-drivers.md) | The architectural drivers: the primary use cases, the quality-attribute scenarios, the constraints and the guiding principles — the small set of forces that actually shape the architecture, and the bridge from the story map to the ADRs | Decisions need drivers to answer to |
| 11 | [11-design-nfr-adr-and-design-system.md](11-design-nfr-adr-and-design-system.md) | Functional vs non-functional requirements, ADRs, Clean Architecture, the diagrams worth keeping (C4 + deployment), and a first design system for the two frontends | Write the design down before you cut code |
| 12 | [12-from-design-system-to-screens.md](12-from-design-system-to-screens.md) | Turning the design system + use cases into concrete screens, organised actor → journey → screen; two front ends; code→design prototyping into Figma | Designing the screens is a fitting for the domain model |

### Foundations — opening the IDE (this batch)

The plan and the design done, Part II keeps going into the code — the foundations that every feature
will stand on.

| # | File | Topic | Key takeaway |
|---|---|---|---|
| 13 | [13-repository-unit-of-work-and-events.md](13-repository-unit-of-work-and-events.md) | The Repository, Unit of Work and Domain Event patterns: what each is for and where it sits in Clean Architecture | The domain defines ports; infrastructure provides adapters |
| 14 | [14-persisting-an-aggregate-with-ef-core.md](14-persisting-an-aggregate-with-ef-core.md) | A first worked implementation — Topology's `WarehouseSite`: EF repository, `DbContext` as Unit of Work, value objects + owned collections, `xmin`, migrations, Aspire wiring | A rich aggregate maps to tables without flattening it |
| 15 | [15-the-hard-cases-ledger-and-owned-collections.md](15-the-hard-cases-ledger-and-owned-collections.md) | The hard cases — Inventory: the append-only ledger in one Unit of Work, owned allocations, a value-converted column you can't query, two contexts in one database | Let the consistency boundary decide the storage shape |
| 16 | [16-the-transactional-outbox-with-wolverine.md](16-the-transactional-outbox-with-wolverine.md) | The transactional outbox with **Wolverine**: why "publish after save" is a bug, one-transaction enqueue via `IDbContextOutbox`, versioned `Contracts` events, the at-least-once / idempotency trade-off (and the 2025 OSS-licensing story behind the library choice) | State and message commit together, or not at all |
| 17 | [17-choosing-a-messaging-library.md](17-choosing-a-messaging-library.md) | The messaging bake-off: MassTransit vs Wolverine vs NServiceBus / Rebus / CAP / Dapr / no-framework — the 2025 licensing earthquake, the criteria that mattered for a warehouse, and a reusable method for the decision | Copy the method, not the verdict |
| 18 | [18-the-admin-panel-architecture.md](18-the-admin-panel-architecture.md) | Architecting the second front end: the admin panel as a separate browser-native React SPA, shared tokens vs the terminal, mocking at the network boundary (MSW), and the headless library shelf — recorded as [ADR-0004](../adr/0004-admin-panel-separate-spa.md)/[0005](../adr/0005-shared-design-tokens.md)/[0006](../adr/0006-mock-at-the-network-boundary.md) | Two products for two users — share the tokens, not the runtime |
| 19 | [19-building-the-admin-panel.md](19-building-the-admin-panel.md) | Building all nine admin screens on the #18 architecture (one MSW seam, feature slices, headless tables, RHF+Zod encoding invariants, 27 tests), then an honest warehouse-ops review — it's a data browser, not yet a tool, and the deliberately thin design pass hid the missing list/create front doors | A prototype proves the flow; only working the screens proves the tool |
| 20 | [20-a-worklist-not-a-dashboard.md](20-a-worklist-not-a-dashboard.md) | Designing the admin landing from #19's biggest gap — a *worklist* ("what needs me now": QC holds, expiring ≤7 d, partial orders, inbound today), deliberately not a BI dashboard; queues derived from the domain, status tokens as urgency, sidebar counters; the `admin-10` prototype | A dashboard answers "how are we doing?"; a worklist answers "what do I do next?" |
| 21 | [21-from-browser-to-tool.md](21-from-browser-to-tool.md) | Closing #19's backlog: front doors (catalogue + create, list + start, row → drill) and actions (adjust/move/block, confirm on irreversible posts), global search, pagination/sort in the shared table, the worklist built — and the environment invariant reappearing on the admin's Move because it's a domain rule, not a UI one | A tool is a browser plus two thin layers — front doors and actions — over a domain that was already a tool |

Planned next, still in Part II:

22. **Logging & observability** — structured logs, OpenTelemetry, what to log in a warehouse
    (and what never to log); plus the cross-cutting solution anatomy (CPM, analyzers, `.slnx`)

## Part III — Features, brick by brick (planned)

23. Master data: first vertical slices + the React admin panel meets the Gateway
24. First integration event: `ProductDefined` → Inventory's `ProductSnapshot`
25. Goods receipt: the inbound saga
26. Put-away with `PutAwayPolicy` — a hard invariant under concurrency
27. Reservations & FEFO picking: the outbound saga
28. Stocktake, handling units (LPN), contract tests, CI/CD per service…

---

## Editorial rules (for consistency across the series)

- **Language:** English. Polish warehouse vocabulary may appear in parentheses where it
  helps (the domain was discovered in Polish).
- **Every decision gets a trade-off box** — "Trade-off:" callouts state what we paid,
  not just what we won. No decision is presented as free.
- **Diagrams:** Mermaid, committed next to the text. Event-storming colors:
  🟧 domain event, 🟦 command, 🟨 aggregate, 🟪 policy, 🟩 read model, 🟥 external system/actor.
- **Code samples** are copied from the repo, never invented; if the repo changes, the post
  gets a changelog footnote.
- Each post ends with **"What's next"** linking the series together.
