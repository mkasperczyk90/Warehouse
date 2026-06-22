# Event Storming — Session #1 (Big Picture)

> **A reconstructed transcript.** This is the kickoff discovery workshop that produced the
> board in [`/docs/diagrams/warehouse.excalidraw`](../diagrams/warehouse.excalidraw) and the
> ubiquitous language used everywhere else in this repo. It is lightly edited for reading —
> the dead ends, the corrections and the arguments are kept on purpose, because *they are the
> deliverable*. If you only read the clean summary at the bottom, you'll miss where the model
> actually came from.

**Date:** 2026-05-14, 09:30–13:00
**Place:** WAW1 site canteen (whiteboard wall + a very long roll of butcher paper)
**Facilitator:** Ania (DDD / event storming)

**In the room:**

| Tag | Person | Role |
|---|---|---|
| **ANIA** | Ania | Facilitator |
| **TOMEK** | Tomek | Product Owner |
| **MAREK** | Marek | Warehouse Manager (client SME) |
| **KASIA** | Kasia | Logistics Coordinator (client SME) |
| **EWA** | Ewa | QC Lead (client SME) |
| **PIOTR** | Piotr | Developer — domain |
| **ŁUKASZ** | Łukasz | Developer — infrastructure |
| **HANNA** | Hanna | Internal Auditor (client, joins after the break) |

Colour code: 🟧 event · 🟦 command · 🟨 aggregate · 🟪 policy · 🟩 read model · 🟥 actor · 🩷 hotspot.

---

## 0 · Warm-up — "don't model yet, just tell me what happens"

**ANIA:** Rule for the morning: no databases, no microservices, no "we'll just add a flag".
We put **orange stickies** on the wall for things that *already happened* — past tense,
business facts. We argue later. Marek, a truck shows up. Go.

**MAREK:** A truck shows up at the gate. Usually we already know it's coming —

**ANIA:** Stop. "We already know it's coming." How?

**MAREK:** The supplier sends us a heads-up the day before. What's on it, how much, roughly
when. We call it *awizacja*.

**KASIA:** Advance shipping notice. ASN. Without it the truck just… stands in the yard.

**ANIA:** *(writes)* 🟧 `DeliveryAnnounced`. And the thing that caused it?

**PIOTR:** A command — someone announces the delivery. 🟦 `Announce delivery (ASN)`.

**ANIA:** Who issues it? Orange and blue stickies are useless without a 🟥 actor.

**KASIA:** The supplier, mostly. Sometimes I key it in for them when they phone it through.

> 📌 Wall so far: 🟥 Supplier → 🟦 Announce delivery (ASN) → 🟧 DeliveryAnnounced.
> 🟥 Logistics Coordinator can issue the same command.

---

## 1 · Inbound — receiving the truck

**PIOTR:** So a delivery is basically a row with some lines. Supplier, date, list of products
and quantities. I'd make a `Delivery` table —

**MAREK:** It's not a row, it's a *thing that has a life*. It's announced, then the truck
arrives, then we receive it, then it's done. Things go wrong at every step.

**ANIA:** That "thing that has a life" is a 🟨 **aggregate**. *(big yellow sticky)*
`InboundDelivery`. The orange stickies are its diary. Keep going — truck arrives.

**KASIA:** Truck arrives at a **dock** (*rampa*). And here's the first thing you'll get wrong:
the dock isn't free by magic. Ramps are scarce. When I accept the ASN I book a **dock slot** —
a dock plus a time window.

**ANIA:** 🟪 **Policy** — "whenever a delivery is announced, *then* book a dock slot."
*(purple sticky under DeliveryAnnounced)*

**LUKASZ:** What if a truck shows up that nobody announced?

**MAREK:** *(flat)* Then it waits. We don't receive an unannounced truck.

**PIOTR:** Never? That feels harsh. Why not just receive it and sort the paperwork after?

**MAREK:** Because an unplanned 18-wheeler at a ramp blocks four planned ones. And because
"sort the paperwork after" is exactly how a pallet ends up on stock that nobody can explain.
If a truck turns up cold, the coordinator creates an **ad-hoc ASN first**, *then* we receive.

> 🩷 **HOTSPOT** *(Ania slaps a pink sticky on the wall)*: "Only ANNOUNCED trucks get
> received — else ad-hoc ASN first." — *this is a rule, not a nice-to-have.*

**ANIA:** 🟧 `DeliveryArrived`. Now — receiving. Walk me through the scan gun.

**MAREK:** Marek-on-the-floor — different Marek — opens the ASN on the terminal and scans
pallet by pallet. The system compares what he scans against what was announced.

**EWA:** And for food he types the **batch** number and the best-before date. That's not
optional. For yogurt, the batch number is the difference between "recall two pallets" and
"recall the whole warehouse".

**PIOTR:** Hold on — batch. Is that a property of the product?

**EWA:** No. Same product, many batches. A batch is *this production run, this expiry*.
We'll come back to it, because batches are where my whole job lives.

**ANIA:** Parking it. 🟨 `Batch` — yellow, we'll fill it in during the quality act.
Marek, what about the pallet of butter that isn't there?

**MAREK:** Driver shrugs, says it didn't make the truck. I record a **shortage** on that line
and sign off anyway. The goods that *did* arrive are now officially **on stock**.

**KASIA:** And — write this down — any line nobody scanned at all becomes a **full shortage
automatically** when he confirms. The driver who "forgot" a pallet doesn't get a fuzzy paper
trail out of it.

**ANIA:** 🟦 `Confirm goods receipt` → 🟧 `GoodsReceiptConfirmed`.

**PIOTR:** Great, so on receipt confirmation we set the stock location to wherever it'll live —

**MAREK:** No. It's not put away yet. It's standing on a square of floor next to the ramp.

**PIOTR:** …so it's not on stock?

**MAREK:** It *is* on stock. It's just not in a rack. That square has a name — the **dock
buffer** (*bufor przyjęć*). It's a real location with a barcode. Goods legally exist there
between receipt and put-away.

**HANNA:** *(later, on hearing the recording)* That dock buffer is the only reason I can ever
answer "the truck left at 7:00, put-away finished at 9:00 — where was the butter at 8:00?"

**ANIA:** So receipt produces stock *in the dock buffer*. 🟧 `StockReceived (dock buffer)`.
Notice something, everyone — the delivery is a **Logistics** thing, but "stock now exists"
is an **Inventory** thing. That seam is going to matter.

> 📌 Inbound spine: 🟧 DeliveryAnnounced → DeliveryArrived → **GoodsReceiptConfirmed**
> → StockReceived (dock buffer).
> 🩷 HOTSPOT: "Pallet count depends on the DELIVERY, not just the catalog!" (the same SKU
> arrives 48-to-a-pallet one week, 36 the next — Kasia insisted the *line* can carry its own pack).

---

## 2 · Quality — the batch hold

**ANIA:** Ewa, you've been waiting. The QC inspector doesn't like a batch. What happens?

**EWA:** The temperature logger in the truck showed a spike on the butter. I don't trust that
batch. I put it on **hold** — quarantine. And the instant I do, *every pallet of that batch,
in every location, in every warehouse* is frozen for orders.

**PIOTR:** OK, so I flag the stock at that location as blocked —

**EWA:** *(sharp)* No. Not the location. Not the pallet. The **batch**. You're about to make
the exact mistake everyone makes.

**PIOTR:** What's the difference? The suspect goods are at a location.

**EWA:** The suspect goods are *that batch*, and that batch might be sitting in five locations
across two warehouses. If I block "the pallet at A-03-2", the same poison is still sellable in
room CHLD2 and in the Kraków site. A suspicious batch is suspicious **everywhere at once**.

**ANIA:** *(moves the quality flag off the stock sticky and onto)* 🟨 `Batch`. So the command
is 🟦 `Quarantine / block batch` on the **Batch** aggregate, and it raises 🟧 `BatchBlocked`.

**LUKASZ:** But the stock already sitting in five locations — something has to react to
`BatchBlocked` and actually freeze those rows, right? Otherwise the "block" is just a note.

**EWA:** Right. Blocking the batch has to *reach out* and quarantine the stock that's already
on shelves.

**ANIA:** 🟪 **Policy**: "whenever a batch is blocked → quarantine its stock everywhere."
And that produces 🟧 `StockItemQuarantined` over on the inventory side. *(draws a long pink
arrow across the wall from the policy into the storage area)* This arrow is important — it
**crosses an aggregate boundary**. The batch decides; the stock items have to follow.

**PIOTR:** So the rule holds in two places — at the gate when we try to ship, and as a sweep
afterwards for stock already placed.

**EWA:** Exactly. Both. A batch can be fine on Monday and blocked on Wednesday after it's
already in racks.

> 🩷 HOTSPOT: "QC blocks the **BATCH**, not the pallet — suspicious everywhere at once."

---

## 3 · Storage — put-away, and the rule that has no override button

**ANIA:** The pallet's in the dock buffer. It needs a home. Marek?

**MAREK:** The system proposes a location. Forklift driver scans the pallet, drives there,
scans the rack label, done. Two scans. We believe scans, not memory.

**KASIA:** And the proposal isn't random. Yogurt needs 2–6 °C, so it proposes a spot in cold
room CHLD1 — *never* the ambient hall, even if the hall is half-empty.

**PIOTR:** So put-away has rules. Temperature, and… space, presumably. Let me make a
`putAway()` method on the warehouse that checks a few `if`s —

**MAREK:** Here's the thing those `if`s always miss, and you have to get it right: **temperature
is not negotiable, capacity is.**

**PIOTR:** Meaning?

**MAREK:** If a location is full, the driver scans the one next to it and life goes on. Nobody
calls me. But if a location is too *warm* for yogurt — there is no override button. And there
**must not be** one. A "force put-away anyway" button is how you poison a customer.

**ANIA:** That split is a model decision, not a UI detail. *(writes)* 🟪 **PutAwayPolicy:
temperature ∧ capacity ∧ load.** Temperature is a hard invariant — rejection, full stop.
Capacity and load limit are soft — the operator reroutes.

**LUKASZ:** Where does the policy even *get* the temperature rule and the room's environment?
The product card lives in another system, the room layout in another.

**PIOTR:** And if put-away has to phone two other services every time a forklift moves, the
forklift stops when either of them hiccups.

**ANIA:** Hold that — it's a real architecture question, but it's a *how*, not a *what*. Park
it on the side wall: *"Inventory needs product storage requirements + room environment to
validate put-away. Call out, or keep a local copy?"* We'll answer it in the design session.

> 🅿️ PARKING LOT: replicas vs cross-service queries for storage requirements & room environment.

**MAREK:** When the driver confirms the second scan, the pallet *moves*. Buffer → rack. And
that move is a line in the book. Who, what, from where, to where, when, why.

**ANIA:** 🟦 `Confirm put-away (scan)` → the stock changes. What's the aggregate that owns
"how much of this is here"?

**PIOTR:** A `Warehouse` with everything inside it?

**MAREK:** God no. Forty forklifts hammering one `Warehouse` row? We'd be deadlocked by 9 a.m.

**PIOTR:** Fair. Then the smallest thing that changes — the quantity of *one* product, *one*
batch, at *one* location.

**ANIA:** 🟨 `StockItem`. SKU + batch + location. Every scan is a tiny transaction on a tiny
row. Now — Marek said "a line in the book". Say more about the book.

**MAREK:** *(this is the part he leans in for)* We **never edit a stock number. Ever.** If
something's wrong, we add a **correction**. The number you see is the sum of every movement
that ever happened. I once spent a week explaining a missing pallet to an auditor. Never again.

**PIOTR:** So the current quantity is a *projection* of an append-only log of movements.

**MAREK:** I don't know what that means, but "you can't rub anything out, you can only add a
line that says why" — yes.

**ANIA:** 🟨 `StockMovement` — the **ledger**. Immutable. 🟧 `StockMoved (ledger entry)` is
raised on every change. A correction is just a *reversing* movement, never an `UPDATE`.

> 🩷 HOTSPOT: "Never EDIT stock — add a CORRECTION (the auditors…)."
> 🩷 HOTSPOT: "Temperature = NON-negotiable. Capacity = negotiable."

**KASIA:** One more storage thing. When we move a whole pallet, we don't re-scan every box on
it. The pallet has its own code — an **LPN**, license plate. Scan the LPN, the whole thing
moves as one.

**ANIA:** 🟨 `HandlingUnit (LPN)`. Good — that earns its own sticky. And the manager's
dashboard, the "how much can I actually sell" screen — that's a 🟩 **read model**, not an
aggregate. `Stock levels / ATP view`.

---

## 4 · Outbound — the correction that reshaped the whole order flow

**ANIA:** Money side. A customer orders. Kasia.

**KASIA:** A supermarket orders 300 yogurts for Thursday. We create the order, and we **set
aside** 300 of that SKU in this warehouse so we don't promise them twice.

**PIOTR:** Easy — on order, pick 300 from stock, mark those pallets as taken.

**KASIA:** No. *No.* That's the mistake that costs us every single day. Don't you dare pin a
pallet on Tuesday for a Thursday order.

**PIOTR:** Why not? The stock's there.

**KASIA:** Because in two days that *specific* pallet can be QC-blocked, clipped by a forklift,
or moved. If I pinned it Tuesday, I'm re-pinning it every time reality moves. What I protect on
Tuesday is the **promise** — "300 of this SKU, this warehouse, spoken for." Not a pallet.

**MAREK:** The pallet — which batch, which location — gets chosen at the *last* responsible
moment. When the order is released to the floor for picking.

**ANIA:** *(this is the big one — she splits one sticky into two)* So there are **two stages**,
not one.
> Stage one, at order time: 🟧 `StockReserved (soft)` — SKU-level, no pallet. Governed by
> 🟪 `Policy: soft-reserve vs available-to-promise`.
> Stage two, at wave release: 🟧 `StockAllocated (hard)` — concrete batch + location.

**PIOTR:** And the soft reservation is its own little thing with a life — created, maybe
partially filled, released…

**ANIA:** 🟨 `StockReservation`. Yes.

**TOMEK:** What's "available-to-promise"? Sales keeps asking me for one honest number.

**MAREK:** On the shelf there are 12. Eight are already spoken for. A new order can take **four**.
On-hand minus allocated minus the outstanding soft reservations. Selling the same yogurt twice
is the unforgivable sin, and that subtraction is the whole defense.

**ANIA:** That's the 🟩 `Stock levels / ATP view` doing real work. Now — wave release. Kasia,
who picks *which* batch?

**KASIA:** FEFO.

**PIOTR:** First-in-first-out, sure —

**KASIA & MAREK:** *(together)* **F-E-F-O.** First *Expired*, First Out.

**KASIA:** Monday's milk can have a *shorter* shelf life than the milk already on the rack —
different dairy, different line. FIFO would ship the older *delivery* and let the
sooner-expiring stock rot in row B. We ship whatever **dies first**.

**MAREK:** We don't sell milk. We sell *time until the date on the lid*.

**ANIA:** 🟪 `Policy: FEFO allocation + re-check batch quality`. And "re-check" because —

**EWA:** Because a batch fine at order time can be blocked by the time we allocate. The bouncer
checks again at the moment of commitment: quarantined, rejected, or expired → not allowed.

**ANIA:** 🟦 `Release wave to floor` → FEFO policy → 🟧 `StockAllocated (hard)` → out comes a
🟩 `Pick list (routed)` — locations in shortest-path order.

**MAREK:** Picker walks the route, scans location, product, quantity. Second stop: shelf says
11, system says 12.

**PIOTR:** So she corrects the system to 11?

**MAREK:** She does **not** "fix" anything. She reports a **short pick**, takes the 11, and the
system replans the twelfth from somewhere else. The missing one is a **stocktake problem**, not
a picking problem. The day she starts editing numbers on the floor is the day I stop trusting
the system.

**ANIA:** 🟦 `Confirm pick (scan)` → 🟧 `StockPicked`; and 🟪 `Policy: short pick → replan +
report` → 🟧 `ShortPickReported`. *(pink sticky)* 🩷 "Short pick = stocktake problem, NOT a
picking error."

**KASIA:** Then it's packed, labeled, the carrier scans the handover and signs. Stock drops, a
tracking number goes to the customer.

**ANIA:** 🟨 `Shipment` → 🟦 `Pack & confirm dispatch` → 🟧 `ShipmentDispatched` → 🟥 Carrier.

> 📌 Outbound spine: OutboundOrderCreated → **StockReserved (soft)** ⇒ StockAllocated (hard)
> → StockPicked → **ShipmentDispatched**.
> 🩷 HOTSPOTS: "Don't pin a pallet at order time!" · "FIFO? No — FEFO!"

---

## 5 · Stocktake — and where the missing yogurt actually surfaces

*(Hanna, the auditor, has joined.)*

**HANNA:** So where does that twelfth yogurt come back?

**MAREK:** Stocktake. We count locations and reconcile. But the trick is *how* we count.

**ANIA:** Go on.

**MAREK:** **Blind.** The counter does **not** see what the system expects. If the screen says
12, people count "12" — they pencil-whip it. Show nothing, and they count what's actually there.

**EWA:** Same psychology as not telling someone the answer before the test.

**ANIA:** 🟦 `Start blind stocktake` → 🟨 `Stocktake` → 🟧 `StocktakeStarted`. Then 🟦 `Record
blind count` → 🟧 `CountRecorded`, with 🟪 `Policy: hide expected qty (blind count)`.

**HANNA:** And the differences?

**MAREK:** The manager approves them, and each difference becomes a movement in the ledger —
with a reason, a name, a timestamp. Just like everything else. We don't *edit* the count up or
down. We *post an adjustment*.

**ANIA:** 🟦 `Approve differences` → 🟧 `StockAdjusted (ledger)`, 🟪 `Policy: differences →
ledger adjustments`. And a 🟩 `Variance report` for Hanna's visits.

**HANNA:** *(satisfied)* That's the only kind of system I can sign off. Every number traceable
to a line that says who and why.

> 🩷 HOTSPOT: "Blind count: hide the expected qty."

---

## 6 · The sentence that killed a table (cross-cutting)

**ANIA:** Before we wrap — Tomek, you said something at coffee I want on the wall.

**TOMEK:** That half our suppliers also *buy* from us. The dairy delivers yogurt Monday morning
and buys back near-expiry stock for its outlet store on Friday.

**PIOTR:** I had a `Suppliers` table and a `Customers` table half-drawn already…

**TOMEK:** Then "Mlekpol Sp. z o.o." exists twice, with two addresses to keep in sync and two
tax IDs that had better match — and an accountant asking why the company owes itself money.

**ANIA:** One legal entity is a **Party**. "Supplier" and "Customer" are **roles** it plays.
🟥 `Party` with `SupplierRole`, `CustomerRole`, `CarrierRole`. The roles carry their own data —
the customer role has shipping addresses, the carrier role has "can you even haul refrigerated?".

**PIOTR:** So an inbound delivery points at a *supplier role*, not at a company.

**ANIA:** Right. And nobody gets two of the same role.

> 🩷 HOTSPOT: "½ of our suppliers also BUY from us → Party + Roles." — *the `Suppliers`
> table died here.*

---

## Closing readout — what the wall produced

**ANIA:** Reading left to right, in business time:

```
INBOUND     DeliveryAnnounced → DeliveryArrived → GoodsReceiptConfirmed* → StockReceived(dock buffer)
QUALITY     BatchBlocked → (policy) → StockItemQuarantined            [batch hold sweeps everywhere]
STORAGE     StockMoved (ledger)                                       [StockItem · HandlingUnit · ledger]
OUTBOUND    OutboundOrderCreated → StockReserved(soft)* ⇒ StockAllocated(hard) → StockPicked → ShipmentDispatched*
STOCKTAKE   StocktakeStarted → CountRecorded → StockAdjusted (ledger)
            (* = pivotal events; these are the natural seams between future services)
```

### Aggregates discovered (🟨)
`InboundDelivery`, `Batch`, `StockItem`, `StockMovement` (ledger), `HandlingUnit` (LPN),
`StockReservation`, `OutboundOrder` / `PickList` / `Shipment`, `Stocktake`, `Party` (+ roles).

### Policies discovered (🟪)
book a dock slot · blocked batch → quarantine stock everywhere · **PutAwayPolicy**
(temperature ∧ capacity ∧ load) · soft-reserve vs available-to-promise · FEFO allocation +
re-check batch quality · short pick → replan + report · blind count · differences → ledger.

### The corrections (🩷) — *the most valuable thing we wrote down*
1. **Only announced trucks get received** — else an ad-hoc ASN first (dock slots are scarce).
2. **QC blocks the *batch*, not the pallet** — a suspect batch is suspect everywhere at once.
3. **Temperature is non-negotiable; capacity is negotiable** — hard invariant vs soft reroute.
4. **Never edit stock — append a correction** — the ledger is how the business already thinks.
5. **Don't pin a pallet at order time** — soft-reserve, then hard-allocate at the wave.
6. **FEFO, not FIFO** — we sell time-to-expiry, not boxes.
7. **A short pick is a stocktake problem, not a picking error.**
8. **Blind counts** — hide the expected quantity or people pencil-whip it.
9. **One company, two roles** — Party + Roles; the `Suppliers` table died.

### Decided in the room
- Stock truth is an **append-only ledger**; on-hand is a projection. Corrections are reversing
  movements, never edits.
- The aggregate is **`StockItem`** (SKU + batch + location), not `Warehouse` — small rows,
  no hot-row contention under many forklifts.
- The QC hold lives on **`Batch`** and converges onto stock items via a policy — it spans two
  aggregates, so neither owns it alone.
- Orders use **two stages**: soft reservation (promise) then hard allocation (pallet), the
  latter FEFO and re-checked for batch quality.

### Parking lot (for the strategic / design session)
- 🅿️ Storage-requirement & room-environment data for `PutAwayPolicy`: **local replicas vs
  cross-service queries?** (Piotr's "the forklift stops if either service hiccups" concern.)
- 🅿️ Where do the seams between services fall? The pivotal events (`GoodsReceiptConfirmed`,
  `StockReserved`, `ShipmentDispatched`) look like the natural integration points.
- 🅿️ Topology as one aggregate per site — fine for master data, but a 10k-location site is
  a big aggregate. Note it, don't pre-optimize.
- 🅿️ Batch vs serial number: we track **batches** (units interchangeable). Pharma/electronics
  would need per-unit instances — explicitly *deferred*, not forgotten.

### Action items
- **Ania** — clean up the photographed wall into [`warehouse.excalidraw`](../diagrams/warehouse.excalidraw); circulate for corrections.
- **Piotr** — first cut of the ubiquitous-language glossary from today's terms (ASN, dock
  buffer, FEFO, LPN, blind count, ATP, Party/Role).
- **Łukasz** — bring options for the replica-vs-query parking-lot item to the strategic session.
- **Tomek** — confirm with finance that one Party with multiple roles is acceptable for invoicing.
- **All** — next session: strategic design (bounded contexts, what becomes a service).

---

*Next: the strategic-design session turns this wall into bounded contexts and the
3-service split — see [`/docs/02-bounded-contexts.md`](../02-bounded-contexts.md) and the
blog write-ups [`#1 What is Event Storming`](../blog/01-what-is-event-storming.md) and
[`#2 Why we start with the domain`](../blog/02-why-we-start-with-the-domain.md).*
