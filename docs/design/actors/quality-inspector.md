# Quality Inspector

> **Who:** inspects deliveries and blocks/releases batches of goods. A small but high-stakes role:
> a batch on QC hold must be **invisible to reservations and picking** until released.
> **Front end:** Operator terminal (on the floor) + Admin panel (review/audit).
> **Use case:** [UC-03 Quality inspection](../03-use-cases.md#uc-03-quality-inspection).

---

## Journey — Quarantine, then release or reject (UC-03)

1. Selected batches from a goods receipt enter status **`Quarantine`** automatically.
2. The inspector reviews the batch (terminal at the dock, or admin from a desk) and either:
   - **Releases** it → `Released`, the batch becomes available; or
   - **Rejects** it → `Rejected` → return / disposal.
3. While blocked, the batch is **invisible to reservations** and can never appear on a pick list
   (the [quality-hold invariant](../01-domain-overview.md#5-key-business-rules-invariants)).

## Screens

- **QC worklist** — [prototype](../prototypes/admin-8-qc.html) · Figma frame pending re-capture ⏳.
  The inspector's home: a queue of batches in `Quarantine` (batch · product · originating receipt ·
  location · qty) with **Release** (green) and **Reject** (red outline) in one row each. Decisions
  are audited.
- On the **[Goods receipt](../prototypes/terminal-2-receive.html)** terminal screen, flagging a
  batch for inspection is the *“Report discrepancy”* path's sibling — the red-outline action.
- On the **[Admin stock view](../prototypes/admin-1-stock.html)**, QC-held stock renders with the
  unmissable **`Blocked · QC`** badge (red) and a `Quarantine` room, with ATP forced to `0`.

## Design notes specific to this actor

- **`status.blocked` (red) is the actor's whole world** — the design system makes it the loudest
  badge precisely so QC holds are never overlooked downstream.
- Release/reject is a **two-action confirm** (like the operator's confirm/exception pair): a
  release puts stock back into ATP; a rejection routes it out of inventory with a reason.
- Decisions are audited (who/when) — the same audit discipline as
  [stock adjustments](warehouse-manager.md#journey-c--stocktake-review-uc-07).
