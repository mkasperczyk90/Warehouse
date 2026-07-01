# #1 — Event Storming: what it is and why we start here

*Series: Building a real microservices application, brick by brick.
Previous: [#0 Series intro](00-series-intro.md).
The session this post describes: [/docs/meeting](../meeting/event-storming-session-01.md).
The boards: [/docs/diagrams](../diagrams/README.md).*

---

The intro promised we'd start where real projects start — not with `docker compose up`, but
with a conversation about the business. This is that conversation, and it has a name and a
method: **Event Storming**. Before we model a single aggregate or argue about service
boundaries, we spend a morning at a wall covered in sticky notes, with the people who actually
run the warehouse. It is the single highest-leverage thing we do on this whole project — and
the cheapest to copy — so it earns the first post.

Everything in the next few posts is downstream of this one. The domain we'll carve up (post
#2), the bounded contexts we'll draw (post #3), the archetypes and the ledger (post #5) — all
of it came off a wall, in business language, before any code existed. So let's understand the
technique first, and then watch it pay off.

It's called **Event Storming**, invented by Alberto Brandolini around 2013. The pitch is
almost insultingly simple: *get the people who know the business and the people who'll build
the software in one room, give them a wall and a lot of orange sticky notes, and model what
happens — as a flow of events — before anyone opens an IDE.* No UML, no boxes-and-lines, no
database. Just facts on a wall, argued over until they're true.

That's it. The rest of this post is *why that works*, the grammar that turns a messy wall
into a model, and the three zoom levels we used (you've already seen all three boards in this
repo — now you'll know what you were looking at).

## Why a wall of stickies beats a meeting

A normal "requirements meeting" produces a document nobody reads and a shared illusion of
agreement. Event Storming produces disagreement *early and visibly*, which is the point.
Three properties make it work:

- **Everyone models at once.** Not a business analyst interviewing one expert while eight
  people wait. Eight hands put up stickies in parallel; the wall fills in minutes, and the
  *gaps and collisions* are where the learning is.
- **The unit is an event, in the past tense.** "GoodsReceiptConfirmed", not "the receiving
  module". Past-tense facts are unambiguous — a domain expert can confirm or reject one
  without knowing what an aggregate is. You can't hide a fuzzy process behind a noun.
- **The model is disposable.** It's paper on a wall (or shapes in Excalidraw). Nobody is
  precious about it, so nobody defends a bad idea to save face. You re-stick, you don't
  refactor.

> The deliverable is **not the board**. The board is a by-product. The deliverable is the
> *shared understanding* and, above all, the **corrections** — the moments a domain expert
> says "no, that's not how it works". We caught nine of those in one morning
> ([the transcript](../meeting/event-storming-session-01.md) keeps every one). Each killed a
> wrong model before it cost a line of code.

## The grammar: six colours and one direction

The genius of the method is a tiny visual grammar. Each colour is a different *kind* of thing,
and they snap together in a predictable way. This is the exact legend used on every board and
diagram in this series:

| Colour | Means | In our domain |
|---|---|---|
| 🟧 orange | **Domain event** — a fact, past tense | `GoodsReceiptConfirmed`, `StockReserved`, `ShipmentDispatched` |
| 🟦 blue | **Command** — an intent that causes an event | `Confirm goods receipt`, `Release wave to floor` |
| 🟨 yellow | **Aggregate** — the thing that decides whether a command is allowed | `StockItem`, `Batch`, `OutboundOrder` |
| 🟪 purple | **Policy** — "whenever *this event*, then *that command*" | "blocked batch → quarantine its stock everywhere" |
| 🟩 green | **Read model** — the view someone looks at before deciding | "Available-to-Promise view", "Pick list (routed)" |
| 🟥 pink | **Actor / external system** — who issues the command | Supplier, Picker, Carrier, ERP |
| 🩷 hot pink | **Hotspot** — a question, a conflict, an insight | "FIFO? No — FEFO!" |

They assemble into one repeating sentence, left to right in **business time**:

```mermaid
flowchart LR
    A["🟥 Actor"] --> C["🟦 Command"]
    C --> AG["🟨 Aggregate"]
    AG --> E["🟧 Event"]
    R["🟩 Read model"] -.->|"informs"| C
    E --> P["🟪 Policy"]
    P -->|"triggers"| C2["🟦 next Command"]
    H["🩷 Hotspot"] -.->|"the argument"| AG
    classDef e fill:#ffa94d,stroke:#d9480f; classDef c fill:#74c0fc,stroke:#1971c2;
    classDef a fill:#ffe066,stroke:#f08c00; classDef p fill:#d0bfff,stroke:#6741d9;
    classDef r fill:#b2f2bb,stroke:#2f9e44; classDef x fill:#ffafcc,stroke:#c2255c;
    class A,H x; class C,C2 c; class AG a; class E e; class P p; class R r;
```

Read it aloud and it's plain language: *"a **Picker** issues **Confirm pick**; the **StockItem**
decides it's allowed and emits **StockPicked**; **whenever** a short pick happens, the
**short-pick policy** triggers a **replan**."* When you can narrate the wall like a story,
you have a model. When you can't, you've found a hotspot.

### Two kinds of sticky are worth more than the rest

- **Pivotal events.** A few orange stickies are clearly more important than the others —
  the ones every process funnels through. On our board they were `GoodsReceiptConfirmed`,
  `StockReserved` and `ShipmentDispatched`. These are gold: they're the natural **seams
  between services** (the three services we carve in post #3 meet exactly there) and the natural
  **integration events** later. We mark them ⚡ on the boards.
- **Hotspots.** The hot-pink stickies are not noise to clean up — they're the most valuable
  thing on the wall. "QC blocks the *batch*, not the pallet." "Never edit stock — add a
  correction." Every one is a place the naive model was wrong. We kept them *on* the board
  on purpose; sanding them off would throw away the reason the model looks the way it does.

## Zoom in one level at a time

Brandolini's method runs at three magnifications. We used all three, and they're all in
[`/docs/diagrams`](../diagrams/README.md) — go open them next to this text.

### 1 · Big Picture — the whole business in one glance

The kickoff. Wide, chaotic, the entire flow of goods left to right in business time, every
actor and external system, every hotspot. The goal isn't precision — it's *scope and shared
language*. You're answering **"what happens here, and where are the surprises?"**

→ [`warehouse.excalidraw`](../diagrams/warehouse.excalidraw): five phases (inbound → quality
→ storage → outbound → stocktake), the deployment band that groups five contexts into three
services, and all nine corrections pinned where they happened.

### 2 · Process Level — one flow, with its unhappy paths

Now zoom into a single process and use the *full* grammar: commands, the aggregates that
decide, the policies that chain steps, the read models people consult — **and the exception
paths**, which is where real systems are won or lost. You're answering **"how does this one
flow actually work when things go sideways?"**

→ [`process-outbound.excalidraw`](../diagrams/process-outbound.excalidraw): order fulfilment,
end to end. It's the outbound story (post #3) made mechanical — the two-stage *soft reserve → hard FEFO
allocation*, plus the branches we'd otherwise forget: ATP-insufficient backorders, a batch
that gets quarantined *between* reservation and allocation, and the short-pick replan loop.

### 3 · Design Level — inside one aggregate, the last stop before code

The tightest zoom: a single aggregate, its commands in, its events out, and — the part that
becomes code almost verbatim — its **invariants**. You're answering **"what is this thing
allowed to do, and what must always be true?"**

→ [`design-stockitem.excalidraw`](../diagrams/design-stockitem.excalidraw): the `StockItem`
boundary. Commands on the left, events on the right, the invariants in the middle (`OnHand ≥ 0`,
allocate ≤ available, unit-safe quantities, "every behaviour returns a ledger entry"), the
append-only `StockMovement` it projects from, and the policies that *don't* fit inside it
(capacity spans many items; FEFO spans `StockItem` *and* `Batch`) standing deliberately
outside the boundary. That last distinction is a whole archetype — which is exactly where
post #5 is going.

## What a session actually looks like

Less mystical than it sounds. Ours ran one morning, roughly:

1. **Unstructured exploration** (the longest part) — everyone throws orange events at the wall,
   no order, no filtering. Duplicates and contradictions are *fine* right now.
2. **Enforce the timeline** — sort events left to right in business time. Gaps appear:
   "wait, what happens between *DeliveryArrived* and *StockReceived*?" (Answer: the dock
   buffer. We'd have missed it.)
3. **Add the grammar** — who triggers each event (actors/commands), what decides
   (aggregates), what chains steps (policies), what people look at (read models).
4. **Hunt hotspots** — wherever the room argues, slap a pink sticky. Don't resolve it on the
   spot; mark it and move on, or you'll rat-hole for an hour.

The other non-negotiable is **who's in the room**. A session without the people who actually
do the work is just developers guessing in colour. Ours had a warehouse manager, a logistics
coordinator, a QC lead and an auditor next to the developers and the product owner — and every
single correction came from *their* side of the table, not ours. The
[reconstructed transcript](../meeting/event-storming-session-01.md) shows the developer
proposing a `Suppliers` table and getting it demolished in real time. That demolition is the
ROI.

> **Trade-off — it's a workshop, not a document generator.** Event Storming buys you shared
> understanding and early correction, but it *costs* the calendars of your busiest domain
> experts for half a day, a facilitator who can keep eight opinions moving without
> bulldozing them, and the discipline to treat the board as disposable. Skip the real experts
> and you get a confident, beautifully-coloured wall that is wrong. The board is also **not**
> your documentation — it's a snapshot of a conversation; the durable artifacts are the
> ubiquitous language, the model, and (for us) the committed diagrams and the transcript.

> **Trade-off — remote dulls it.** The method’s superpower is eight people standing at a wall
> moving paper at once. On a digital canvas you keep the artifact (and versioning — that's why
> ours are generated and committed), but you lose some of the parallel, physical, interrupt-driven
> energy that surfaces the best arguments. We accept it because committed, diffable boards are
> worth more to a distributed team than the extra few hotspots a physical wall might have caught.

## What our morning actually produced

Concretely, by lunch we had:

- a **ubiquitous language** the whole team now shares (ASN/*awizacja*, dock buffer, FEFO, LPN,
  blind count, available-to-promise) — the glossary in post #2 is just this, written down;
- the **aggregates** that became our code (`StockItem`, `Batch`, `InboundDelivery`,
  `OutboundOrder`, `StockMovement`, …);
- the **service seams**, handed to us for free by the pivotal events;
- and **nine corrections** that each deleted a plausible-but-wrong design before it was built.

Not bad for a wall, some paper, and the discipline to ask the people who drive the forklifts.

## What's next

[Post #2](02-why-we-start-with-the-domain.md): now we point the technique at a real domain.
Why a warehouse, why domain-first instead of `docker compose up`, and the big-picture board in
full — the strategic decisions it surfaced (microservices from day one, stock as an append-only
ledger) and the price tag on each. The method you just met, applied — and corrected — in anger.
