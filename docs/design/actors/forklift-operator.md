# Forklift Operator

> **Who:** moves pallets — put-away tasks and location-to-location transfers. Same rugged terminal
> as the [Warehouse Operator](warehouse-operator.md), same constraints (gloves, cold, glare), but a
> narrower task set centred on whole pallets / handling units (LPNs).
> **Front end:** Operator terminal.
> **Use cases:** [UC-04 Put away](../03-use-cases.md#uc-04-put-away-goods),
> [UC-06 Move stock](../03-use-cases.md#uc-06-move-stock).

---

## Journey A — Put away pallets (UC-04)

Identical to the [Warehouse Operator's put-away journey](warehouse-operator.md#journey-b--put-away-received-goods-uc-04),
but the unit of work is a **pallet / LPN** rather than a scanned eaches count:

1. Task hub → **Put away**; tasks are keyed by pallet LPN.
2. System proposes a location honouring temperature ↔ room, capacity and consolidation.
3. The **cold-chain chip** and the two **green check rows** (temperature, capacity) are the whole
   point for this actor — they confirm the pallet may legally go where it's headed.
4. Scan location → confirm → `PutAway` in the ledger.

**Screen**
- **Put-away** — [prototype](../prototypes/terminal-3-putaway.html) · Figma frame pending re-capture ⏳.
  Shows the pallet's LPN, quantity, batch/BBE and the proposed location in large type.

---

## Journey B — Move / replenish stock (UC-06)

Move between locations within a warehouse, or an **inter-warehouse transfer** (issue from A →
in-transit → receipt at B; goods visible as `InTransit`). Same validations as put-away: the screen
shows a **From** leg and a **To** leg, the item with its cold-chain chip, a quantity stepper, and
the identical environment/capacity **check rows** before the confirm scan.

**Screen**
- **Move stock** — [prototype](../prototypes/terminal-5-move.html) · Figma frame pending re-capture ⏳.
  A second action switches the move into an **inter-warehouse transfer → in-transit**.

## Design notes specific to this actor

- Works in **whole handling units (LPNs)** — the screen leads with the LPN, not a SKU count.
- The hard **environment-compatibility stop** matters most here: a forklift driver can't be
  trusted to remember that dairy must go to a cold room — the system refuses the move.
- Otherwise inherits every terminal convention from the
  [Warehouse Operator](warehouse-operator.md#design-notes-specific-to-this-actor).
