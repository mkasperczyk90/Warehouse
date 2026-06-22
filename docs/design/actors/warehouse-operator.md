# Warehouse Operator

> **Who:** the operational worker on the floor with a rugged handheld scanner — *one hand, gloves,
> cold room, glare, noise.* Does one task at a time, as fast as possible, wants zero prose.
> **Front end:** [Operator terminal](../00-design-system.md#2-two-type-scales-one-base-two-ergonomics).
> **Use cases:** [UC-02](../03-use-cases.md#uc-02-receive-delivery-goods-receipt),
> [UC-04](../03-use-cases.md#uc-04-put-away-goods),
> [UC-06](../03-use-cases.md#uc-06-move-stock),
> [UC-10](../03-use-cases.md#uc-10-picking),
> [UC-11](../03-use-cases.md#uc-11-packing).

The operator is the reason the terminal exists. Every screen below assumes a thumb, a scanner
trigger, and no patience.

---

## Journey A — Receive an announced delivery (UC-02)

1. Truck checks in at the dock. Operator opens the terminal — the **Task hub** already lists the
   trucks waiting at their docks.
2. Taps **Receive** → opens the relevant ASN, lands on line 1.
3. Scans each item; the system compares the count against the ASN. The screen shows **expected vs
   counted** — the count defaults to expected and is corrected with a **numeric keypad** (tap the
   number), with the ±1 stepper kept for small fixes.
4. For batch-tracked products, enters **batch number + best-before** (FEFO depends on it).
5. Confirms each line, or reports a **discrepancy** (shortage / overage / damage).
6. On confirmation a goods-receipt document is created and stock lands in the dock buffer
   (`GoodsReceived`).

**Screens**
- **Task hub** — [prototype](../prototypes/terminal-1-hub.html) ·
  [Figma](https://www.figma.com/design/xAzdWqmAOd3b2ZKWlU0TgR?node-id=4-2) ✅
  The landing screen: an always-focused scan field + the day's task piles (Receive / Put away /
  Pick / Move) with live counts. Big coloured cards, status-coded. The bar's network chip flips to
  **Offline · N queued** when signal drops, with a sync banner — confirmations are saved on-device
  and sync when WiFi returns.
- **Goods receipt** — [prototype](../prototypes/terminal-2-receive.html) ·
  [Figma](https://www.figma.com/design/xAzdWqmAOd3b2ZKWlU0TgR?node-id=5-2) ✅
  Expected-on-ASN banner, a tap-to-keypad quantity (±1 stepper for small fixes), batch/BBE fields,
  and two big actions: *Confirm line* (green) vs *Report discrepancy* (red outline).

---

## Journey B — Put away received goods (UC-04)

1. From the Task hub, taps **Put away** (14 pallets in the dock buffer).
2. The system **proposes a target location** per pallet, respecting temperature ↔ room type,
   capacity/load limit, and consolidation with the same SKU/batch.
3. The screen shows the pallet (with a **cold-chain chip**), the proposed location in huge type,
   and explicit **green check rows**: *temperature compatible*, *capacity OK*.
4. Operator scans the location to confirm → `PutAway` movement in the ledger. If it's full, one
   tap asks the system to **propose another** — environment compatibility is a hard stop, always.

**Screen**
- **Put-away** — [prototype](../prototypes/terminal-3-putaway.html) ·
  Figma frame pending re-capture ⏳ (submitted; see [README](../README.md#re-capturing--adding-figma-frames)).
  Note how the **environment-compatibility invariant** is made visible, not hidden — the operator
  sees *why* the location was chosen and can't override the temperature rule.

---

## Journey C — Pick a wave (UC-10)

1. Wave released → soft reservations become **hard FEFO allocations** (concrete batch+location,
   quality re-checked at this moment).
2. Task hub shows **Pick** with the wave count. Tapping it opens a routed pick list.
3. Each step: a big **“Go to <location>”** card, the product, a **FEFO chip** (nearest BBE / lot),
   and the pick quantity in display type. A progress bar shows *12/31*.
4. Operator scans **location then product** — that scan *is* the commit (Confirm stays disabled
   until both land, so nothing is confirmed from memory). A shortage triggers **replan** from
   another location/batch.

**Screen**
- **Picking** — [prototype](../prototypes/terminal-4-pick.html) ·
  Figma frame pending re-capture ⏳.
  FEFO is surfaced as a batch/BBE badge so the operator sees the *why* of the chosen batch; blocked
  (QC) stock can never appear here. Confirm is gated behind the two scans; *Short pick* is a neutral
  secondary action, not a red alarm.

---

## Journey D — Packing (UC-11)

1. After picking, the operator moves to the packing zone and opens the order's packing task.
2. Scans each picked item **into the active package**; the screen ticks items off and tracks
   what's still outstanding (e.g. *0 / 6 ea*).
3. Records the package weight and dimensions, then **closes the package and prints the label**;
   can add further packages for the same order.

**Screen**
- **Packing** — [prototype](../prototypes/terminal-6-pack.html) · Figma frame pending re-capture ⏳.
  Reuses `QuantityWithUnit`, a done/todo checklist, weight/dimension fields, and a label-print action.

## Journey E — Move / replenish stock (UC-06)

Shared with the [Forklift Operator](forklift-operator.md#journey-b--move--replenish-stock-uc-06).
From → To location scan with the same environment/capacity check rows as put-away; an
inter-warehouse transfer routes goods through `InTransit`.

**Screen**
- **Move stock** — [prototype](../prototypes/terminal-5-move.html) · Figma frame pending re-capture ⏳.

## Design notes specific to this actor

- **ScanField is king** — it's focused on every screen; the keyboard is the exception, not the rule.
- **Three buttons max** — confirm, exception, alternative. Never a form.
- **Status by colour _and_ shape** — gloves + glare mean badges carry a dot + label, not hue alone.
- **Counts use a keypad, not just a stepper** — a real discrepancy is a deliberate keypad entry
  with a one-tap "= expected" default, never twenty taps on a `−`/`+`.
- **Offline-first** — the floor has RF dead spots, so confirmations queue on-device and sync on
  reconnect; the bar always shows the online/offline state and the pending count.
- **High-contrast for glare** — a ◐ toggle in the bar switches the terminal to a high-contrast
  theme (darker status colours that pass WCAG, no faint greys, solid lines), remembered per device.
- **The scan is the commit** — confirm is gated behind the required scans (location, then product);
  we never confirm from memory, which is the whole point of "believe scans, not memory".
- **Exceptions aren't alarms** — shortage / short-pick / discrepancy are neutral buttons; red is
  reserved for true hard-stops (QC-blocked), so operators report honestly instead of avoiding it.
- Cross-references: [Forklift Operator](forklift-operator.md) shares Put-away and Move;
  [Quality Inspector](quality-inspector.md) acts on the batches this actor receives.
