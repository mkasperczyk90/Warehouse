# Demo walkthrough — the golden path, click by click

A guided, **happy-path** tour of the whole system across both front ends (**Admin** desk app and
**Terminal** handheld), one actor at a time, **from defining a product to dispatching it**. Use it to
present the app live or to record a single end-to-end GIF.

> The flows here mirror the design map in [design/01-flows.md](design/01-flows.md) and the use cases in
> [03-use-cases.md](03-use-cases.md) — this doc is the *operational script* (which screen, which button,
> what to type, what you should see).

![The full golden path — Admin and Terminal side by side, ① define → ⑩ ledger](media/golden-path-full.gif)

> The clip above is the canonical end-to-end run of this script on the Aspire stack (real backend).
> Per-app cuts and the file index are in [Recording the GIF / live demo](#recording-the-gif--live-demo--tips).

## Before you start

- **Run the apps:**
  - **Admin** still has a self-contained, MSW-mocked preview build (ADR-0006):
    `cd src/web/admin && npm ci && npm run mock:init && npm run dev` → opens at `localhost:5173`.
  - **Terminal** no longer mocks anything — it always calls the real Gateway. Run the whole stack with
    Aspire (`dotnet run --project src/AppHost/Warehouse.AppHost`) and open the **terminal** resource from
    the Aspire dashboard; nginx proxies its `/api` to the Gateway, and the operator badges in for a token.
  - For a true **end-to-end** across both front ends, run everything on Aspire (the AppHost builds the
    admin image with MSW off too), so a product defined in ① flows all the way through to ⑩.
- **Demo logins (badge scan — just type the number + Enter):**
  | App | Badge | Who |
  |---|---|---|
  | Admin | `1001` | Warehouse **Manager** |
  | Admin | `1002` | Logistics **Coordinator** |
  | Admin | `1003` | Quality **Inspector** |
  | Terminal | `7700` | Warehouse **Operator** |
  | Terminal | `7701` | **Forklift** operator (J. Forklift) |
- **Data note (important for a smooth demo).** On the **Aspire stack** the terminal — and the
  Aspire-built admin — read and write the **real, seeded backend**, so the inbound → stock → outbound
  handoffs are genuine end to end (the seeders fill products, deliveries, stock and the QC batches). The
  standalone **MSW** preview applies to the **admin** only; there, a created record is stateful within
  that browser session and won't appear elsewhere. Two things to expect on the terminal: it shows
  **catalog codes** (e.g. `MILK-1L`), not composed product names or EANs — names/EANs live in
  Catalog/Partners and aren't fetched on the handheld; and human refs (`ASN-…`, `SO-…`) are **derived**
  from the backend GUID, so the exact numbers in the terminal scenes below are illustrative.
- The Admin top bar has a **warehouse switcher** (WH-01 Wrocław / WH-02 Poznań) — stay on WH-01 for the demo.

## The cast (and where they work)

| Actor | App | Lands on | Owns |
|---|---|---|---|
| Warehouse Manager | Admin | Today / Stock | master data, stock, stocktake, adjustments |
| Logistics Coordinator | Admin | Inbound / Outbound / Dispatch | what comes in and goes out |
| Quality Inspector | Admin | Quality holds | release/reject quarantined batches |
| Warehouse Operator | Terminal | Task hub | receive · put-away · pick · pack · move |
| Forklift Operator | Terminal | Task hub | put-away & moves (whole pallets) |
| Supplier / Customer / Carrier | — (API) | — | ASN / order / pickup, behind an ACL |

---

# The golden path (10 scenes)

Each scene: **Actor · App · Screen → steps → what you should see → handoff.** Run them in order for the
full inbound → stock → outbound story.

## ① Master data — define a product · *Manager · Admin*

1. Sign in with badge **`1001`**. You land on **Today** (the work queue).
2. Sidebar → **Products**. The catalogue lists the six demo products (Whole milk, Greek yoghurt, Frozen
   berries, Cheese, Cleaning solvent, Cardboard box).
3. Click **Define product**. Fill: SKU `DEMO-1`, Name `Demo bar 50 g`, category `Dry goods`, base unit
   `piece`, dims/weight (defaults are fine). Click **Create product**.
4. **See:** the new product appears in the catalogue (search `Demo` to find it). Click a row to open its
   read-only detail; **Rename** and **Change storage** are the two write actions.

> *Capability shown: UC-13. From here we follow the pre-seeded **`Whole milk 3.2% — 1 L carton`**
> (`MILK-1L`) so the cross-app handoffs line up.*

## ② Announce a delivery (ASN) + dock slot · *Coordinator · Admin*

1. Sign out (top-bar user menu) and sign back in with badge **`1002`**.
2. Sidebar → **Inbound (ASN)**. The master-detail list shows announced deliveries (`ASN-2206…2208`).
3. *(Capability)* Click **New ASN** → pick a supplier, add a line (SKU + qty) → **Create ASN**.
4. Select **`ASN-2208`** (it shows "slot pending") → **Assign dock slot** → Dock `D-2`, window
   `11:00–12:00` → **Assign slot**. **See:** the dock field now reads `D-2 · 11:00–12:00`.
5. Select **`ASN-2206`** (Dairy Farms Ltd, the milk) → **Mark arrived**. **See:** status flips
   Announced → Arrived; "Mark arrived" disappears. → *handoff to the operator (truck is at the dock).*

## ③ Receive the delivery · *Operator · Terminal*

1. On the terminal, sign in with badge **`7700`**. You badge in for a real token and land on the **Task
   hub** (piles: Receive, Put away, Pick, Move stock — each with a live count aggregated from the backend).
2. Tap the **Receive** pile → the **Goods receipt** screen opens on the first WH01 delivery still to
   receive — its derived **`ASN-…`** ref + supplier, the line's product **code** (e.g. **`MILK-1L`**), and
   the counted qty pre-filled to the expected (e.g. **240**). *(The dock reads `Dock —` unless a slot was
   assigned in scene ②.)*
3. Use the **+ / –** stepper to match the physical count (leave it for a clean line). Enter batch/BBE
   if prompted.
4. Tap **Confirm line**. **See:** you return to the **Task hub** and the Receive pile drops by one.
   *(Try the discrepancy path too: **Report discrepancy → Damaged goods** still receives and routes the
   batch to QC.)* → *handoff: `GoodsReceived` → the batch lands in the dock buffer, and a **Put away** task
   now appears (proof the event reached Inventory).*

> Bonus: tap the **Scan** tab and scan any `ASN-…` code → the terminal recognises an *Inbound ASN* and
> offers **Open goods receipt** (the "scan anything, get routed" trick — resolved on-device by the code's
> shape). Each scan is remembered in this handheld's own recent list.

## ④ Quality decision · *Inspector · Admin*

1. Back on Admin, sign in with badge **`1003`**. Sidebar → **Quality holds** — batches in quarantine
   (e.g. a milk lot + cheese).
2. Pick a batch → **Release** → choose a reason (`Temperature within range`) + optional note →
   **Confirm release**. **See:** the batch leaves the worklist (it's now available to reservations).
   *(Or **Reject → Failed temperature** to send it to return/disposal — the loudest red path.)*
   → *handoff: `Released` → the stock is available; put-away can proceed.*

## ⑤ Put away the pallet · *Operator / Forklift · Terminal*

1. On the terminal Task hub, tap the **Put away** pile → the **Put-away** screen (populated by the
   goods-receipt from ③) proposes location **`WH01-CR1-A03-R2-S1`** for the pallet's product **code**.
2. Read the two **hard checks**: *Temperature compatible (cold room 2–6 °C)* ✓ and *Capacity & load limit
   OK* ✓ — the invariant in action.
3. *(Optional)* Tap **Location full** → the system proposes another legal bay (`WH01-CR1-A01-R1-S4`),
   still temperature-compatible — it never offers an incompatible one.
4. Scan the location to confirm. The backend re-checks compatibility, then posts a `PutAway` move. **See:**
   back to the hub, the Put away pile drops. → *handoff: `PutAway` movement hits the immutable ledger; the
   manager can now see the stock.*

## ⑥ Stock is live · *Manager · Admin*

1. Sign in as Manager (**`1001`**). Sidebar → **Stock view** — KPIs (On hand / ATP / Reserved /
   Blocked) + the table with **status badges** (colour = domain status, never decoration).
2. Click the milk row → the **stock item** drill: on-hand breakdown, **Movement history**, and the row
   actions **Adjust / Move / Block**.
3. Sidebar → **Movements** — the **immutable ledger**: the goods-receipt and put-away you just did appear
   as signed entries (a correction is a *new* reversing movement, never an edit). → *the goods are on
   stock and available-to-promise.*

## ⑦ Create an outbound order · *Coordinator · Admin*

1. Sign in as Coordinator (**`1002`**). Sidebar → **Outbound**. The list shows orders (`SO-4471…`).
2. *(Capability)* **New order** → customer + ship-to + a line (SKU + qty) → **Create order**. A **soft
   `StockReservation`** is taken against ATP (SKU-level — no batch/location pinned yet).
3. Select **`SO-4471`** → its lines + the reservation view. Drill a line into **ATP by location** ("why
   is this partial — what's available where"). Click **Release to wave**. **See:** status advances
   Reserved → Picking. → *handoff: `StockReserved` then wave release → a pick task for the operator.*

## ⑧ Pick → Pack · *Operator · Terminal*

1. Terminal Task hub → tap the **Pick** pile → the **Picking** screen (populated only once an order is
   released to a wave in ⑦): go-to location, product **code**, and the **FEFO batch** (the allocation
   picks the first-expiring batch; quality is re-checked here, not at order time).
2. **Confirm is blocked until both scans land** — scan the **location**, then the **product**. Now
   **Confirm pick** is enabled → tap it. *(Try **Short pick → Less stock here** to watch it replan onto
   the next FEFO batch.)*
3. The **Packing** screen opens automatically on package **`PKG 1`**, listing the **picked lines**
   (product code + lot + qty) for the order. *(Tap **Open another package** for `PKG 2` if the order
   spills over — package numbering is on-device.)*
4. **Close package** → the order is marked **packed**. **See:** back to the Task hub. → *handoff: packed
   parcels ready for the carrier.*

## ⑨ Dispatch to carrier · *Coordinator · Admin*

1. Admin as Coordinator (**`1002`**). Sidebar → **Dispatch** — a **kanban board**: *Awaiting carrier →
   Carrier assigned → Pickup notice sent → Dispatched*.
2. On a packed shipment card → **Assign carrier** (carrier + pickup time) → the card moves to *Carrier
   assigned*.
3. **Send pickup notice** → *Pickup notice sent*. **Mark collected** → *Dispatched* (with tracking).
   **Print waybill** on the dispatched card. → *handoff: `ShipmentDispatched` + tracking to the carrier /
   customer.*

## ⑩ Close the loop · *Manager · Admin*

1. Manager (**`1001`**) → **Movements**: the **Pick** and **Dispatch** entries are now in the ledger —
   the full lifecycle (receipt → put-away → pick → dispatch) reads as signed, immutable movements.
2. **Today** (the landing worklist): the queues you worked (QC holds, partial orders, inbound) have
   cleared — "what needed me now" is done. **End of the golden path.** 🎬

---

# Side scenes (optional, to round out a demo)

- **Move stock** *(Operator · Terminal)* — Task hub → **Move stock** pile → a replenishment task moves
  available stock from a reserve bay to its **pick face**, with the same temperature/capacity stop as
  put-away; confirm posts a `Move` to the ledger. *(Inter-warehouse `InTransit` transfers aren't modelled
  in the backend yet, so that variant is deferred.)*
- **Stocktake** *(Manager · Admin)* — **Stocktakes** → open a count awaiting approval → review the
  differences → **Approve differences → ledger** (each accepted diff posts a signed adjustment).
- **Adjustment** *(Manager · Admin)* — from a Stock row → **Adjust stock** → new counted qty + reason →
  confirm; posts one signed ledger entry (never below zero/allocated).
- **Global search** *(any · Admin)* — top-bar command bar: type a SKU / batch / location / ASN / order →
  jump straight to the record.
- **Lookup** *(Operator · Terminal)* — the **Lookup** tab → search/filter the real stock index
  (products, batches, locations) read from Inventory; status badges encode domain status.
- **Multi-warehouse** — switch the top-bar warehouse to **WH-02 Poznań**; every operational screen
  re-scopes (master data stays cross-warehouse by design).

---

# Recording the GIF / live demo — tips

- **One actor per "act"**, signing in/out between them, makes the handoffs legible. The order above is
  the natural narrative: ① define → ② announce → ③ receive → ④ QC → ⑤ put-away → ⑥ stock → ⑦ order →
  ⑧ pick/pack → ⑨ dispatch → ⑩ ledger.
- Put **Admin and Terminal side by side** (terminal at a narrow phone width) so the inbound/outbound
  handoffs read as one story.
- Keep scenes short: land on the screen, do the one action, show the result, cut. The whole path is ~10
  beats — aim for a 60–90 s clip.
- For a **true end-to-end**, run the Aspire stack: the terminal always hits the real Gateway, and the
  Aspire-built admin runs with MSW off too, so the product you define in ① actually flows through to ⑩.

## The recordings — which file is what

Already captured in [`media/`](media/) — the GIFs above and below are these files:

| File | Shows | Use for |
|---|---|---|
| [`golden-path-full.gif`](media/golden-path-full.gif) | **Canonical** full run, Admin + Terminal side by side (① → ⑩), Aspire / real backend | the one-clip story (embedded at the top) |
| [`admin-golden-path.gif`](media/admin-golden-path.gif) · [`.mp4`](media/admin-golden-path.mp4) | Admin desk app only — the Coordinator / Manager / Inspector acts | slides, README, a focused admin loop |
| [`terminal-golden-path.gif`](media/terminal-golden-path.gif) · [`.mp4`](media/terminal-golden-path.mp4) | Terminal handheld only — receive · put-away · pick · pack · move | the operator story on its own |
| [`admin-walkthrough-real-backend.gif`](media/admin-walkthrough-real-backend.gif) · [`.webm`](media/admin-walkthrough-real-backend.webm) | Admin driven against the **real seeded backend** (MSW off) | proof the admin runs live, not just mocked |
| [`frame-stock-view.png`](media/frame-stock-view.png) | A single Stock-view still (scene ⑥) | thumbnail / hero frame |

**Admin only** &nbsp;|&nbsp; **Terminal only**

| | |
|---|---|
| ![Admin golden path](media/admin-golden-path.gif) | ![Terminal golden path](media/terminal-golden-path.gif) |
