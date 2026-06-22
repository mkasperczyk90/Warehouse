# Exceptions & unhappy paths

> The [flows](01-flows.md) and [use cases](../03-use-cases.md) lead with the happy path — that's
> deliberate, it's how you learn a system. This doc is the counterweight: **what goes wrong, and
> what the system does about it.** Three classes:
>
> 1. **Business exceptions** — expected, modeled, shown on a screen (§1).
> 2. **System & operational failures** — the distributed-system realities from the
>    [NFRs](../blog/11-design-nfr-adr-and-design-system.md) and [ADRs](../adr/README.md) (§2).
> 3. **Invariants** — the things the system *refuses* to do, ever (§3), and the
>    [open questions](#4-open-questions) still unanswered (§4).
>
> Legend: ✅ already shown in a screen · 🟡 partially · ⬜ not yet designed.

---

## 1. Business exceptions, by phase

### Inbound

| Scenario | Trigger | How it's handled | UI | Backed by |
|---|---|---|---|---|
| Unknown SKU on ASN | SKU not in catalog | Line flagged *for clarification*; ASN can't be fully validated | ✅ red flag row on [Inbound (ASN)](prototypes/admin-2-asn.html) | UC-01 |
| Unknown SKU — resolution | a flagged line needs fixing | no action yet to **create the product** or **map to an existing SKU** — only the flag is shown | ⬜ | UC-01 / UC-13 |
| No free dock slot | Requested window taken | System proposes an alternative window | ⬜ | UC-01 |
| Unannounced delivery | Truck arrives with no ASN | **Blocked** — an ad-hoc ASN must be created first | 🟡 (rule stated, no screen) | UC-02 rule |
| Shortage / overage | Counted ≠ expected | Counted via a **numeric keypad** (one-tap "= expected" default), so a discrepancy is a deliberate entry, not a stepper slog; recorded per line, receipt still proceeds | ✅ keypad + *Report discrepancy* on [Goods receipt](prototypes/terminal-2-receive.html) | UC-02 |
| Damaged goods | Operator marks damage | Discrepancy + may route to QC quarantine | ✅ same action | UC-02 / UC-03 |
| Missing batch/BBE | Batch-tracked product, no data entered | Cannot confirm the line until batch + expiry given | 🟡 (fields exist, no hard validation shown) | UC-02 |
| Batch rejected by QC | Inspector rejects | `Rejected` → return / disposal; never enters stock | ✅ *Reject* on [QC worklist](prototypes/admin-8-qc.html) | UC-03 |
| QC decision — reason / audit | inspector releases or rejects | a confirm dialog requires a **reason** (+ optional note) before the command posts; the decision is audited | ✅ built in the admin app ([plan §11](03-admin-frontend-plan.md#11-post-build-review--what-the-gap-closing-pass-delivered)) | UC-03 |
| No compatible location | Nothing matches temperature need | **Hard stop** — operator must escalate; system won't suggest an illegal spot | ✅ check rows + *propose another* on [Put-away](prototypes/terminal-3-putaway.html) | Invariant #1 |
| Location full / over capacity | Volume/weight would exceed limit | Operator scans another; capacity is validated | ✅ *Location full → propose another* | Invariant #2, UC-04 |

### Stock

| Scenario | Trigger | How it's handled | UI | Backed by |
|---|---|---|---|---|
| Move to incompatible room | Dairy → ambient, etc. | **Hard stop** (same invariant as put-away) | ✅ check rows on [Move stock](prototypes/terminal-5-move.html) | Invariant #1 |
| Stocktake discrepancy | Blind count ≠ system | Variance surfaced; **reason required** before posting | ✅ [Stocktake review](prototypes/admin-3-stocktake.html) | UC-07 |
| Adjustment below zero | Correction would make stock negative | **Refused** — quantity never drops below zero | 🟡 (screen exists, guard not visualized) | Invariant #3 |
| Recount needed | Variance disputed | Manager re-issues a blind count | 🟡 (*Recount selected* button) | UC-07 |
| In-transit transfer mismatch | Inter-warehouse: received ≠ issued | Discrepancy at destination receipt; goods visible as `InTransit` meanwhile | ⬜ | UC-06 |

### Outbound

| Scenario | Trigger | How it's handled | UI | Backed by |
|---|---|---|---|---|
| Insufficient availability | Order qty > ATP | **Partial** or **waiting** order — coordinator decides | ✅ note on [Outbound](prototypes/admin-5-outbound.html) | UC-09 |
| Reserve > available | Attempt to over-commit | **Refused** — reservation ≤ availability | 🟡 | Invariant #4 |
| FEFO batch blocked at pick | Chosen batch went to QC hold / expired between order & wave | Re-allocate to next FEFO batch; quality re-checked at allocation | 🟡 (logic stated, not a distinct screen) | UC-10, Invariant #6/#7 |
| Pick shortage | Location holds less than expected | **Replan** from another location/batch | ✅ neutral *Short pick* (replan) on [Picking](prototypes/terminal-4-pick.html) | UC-10 |
| Order cancelled | Customer/coordinator cancels | Reservations released back to ATP | 🟡 (lifecycle state, no screen) | UC-09 state machine |
| Wrong item at packing | Scan ≠ expected item | Rejected; item must match the pick | 🟡 (scan-into-package implies it) | UC-11 |
| Carrier no-show / missed pickup | Pickup window passes | Shipment stays *awaiting collection*; re-notify | 🟡 (board column exists) | UC-12 |
| Collection not confirmed | No signature/confirmation | Stock **not** deducted; no `ShipmentDispatched` | 🟡 | UC-12 |

---

## 2. System & operational failures (cross-cutting)

These don't belong to one screen — they're the distributed-system realities. The UI's job is to
**degrade honestly**, not to pretend.

| Failure | What happens | Design response |
|---|---|---|
| **Stale replica** (eventual consistency) | A product/price just changed in masterdata; Inventory's snapshot is seconds behind ([ADR-0003](../adr/0003-replicas-over-cross-service-queries.md)) | Accept staleness on read paths; never block the operator on a cross-service call. ATP shown is "best known". |
| **masterdata-service down** during put-away | We still must validate temperature/capacity | The invariant holds off the **local replica**, not a live call ([NFR](../blog/11-design-nfr-adr-and-design-system.md)) — the floor keeps working. |
| **Product not yet replicated** | Operator scans a SKU Inventory hasn't heard of | Surface a clear "unknown here yet" state, not a crash; retry as the event arrives. |
| **Concurrency conflict** | Two actors touch the same `StockItem` (e.g. two pickers) | Optimistic concurrency via Postgres `xmin`; the loser retries on a fresh read. The non-negative & reservation invariants are never bypassed. |
| **Broker (RabbitMQ) down** | Integration event can't publish | Transactional **outbox** holds it; it publishes when the broker returns — no lost events, no "publish-after-save" bug. |
| **Poison / repeated message** | A consumer keeps failing, or an event arrives twice | **Idempotent** consumers (inbox); a dead-letter queue isolates poison messages for inspection. |
| **Double scan / double submit** | Operator taps confirm twice | Idempotent commands; a second identical confirm is a no-op, not a double movement. |
| **Network drop on the handheld** | WiFi flaps on the floor | Terminal designed **offline-first**: confirmations queue on-device, the bar shows an **Offline · N queued** chip, and they sync when signal returns. ✅ state on [Task hub](prototypes/terminal-1-hub.html); the *policy* (offline-first vs hard-require connectivity) is still a PO call (§4, [blog #11 Q6](../blog/11-design-nfr-adr-and-design-system.md)). |
| **Unauthorized action** | Actor lacks permission | AuthN/Z on every call; refused with an audit entry — no silent failure. |
| **Domain error** | Any invariant violation | Surfaced **by stable error code** (`DomainException`) → a toast keyed to the code, the same language the API returns. |

---

## 3. The invariants — what the system refuses to do

The hard "never" list. These are not warnings; they are **enforced** ([domain overview](../01-domain-overview.md#5-key-business-rules-invariants)):

1. Store a product in a room with an incompatible temperature range.
2. Exceed a location's volume capacity or load limit.
3. Drive stock at a location below zero.
4. Reserve more than is available.
5. Lose a movement — every change is an immutable ledger entry.
6. Pick out of FEFO order for expiring batches.
7. Reserve or pick a QC-blocked batch.

> **The one we never tolerate:** selling the same stock twice. Everything about the two-stage
> allocation (soft reservation → hard FEFO allocation) and strong in-aggregate consistency exists
> to make that impossible, even while everything *across* services is eventually consistent.

---

## 4. Open questions

Genuinely undecided — guessing silently would be worse than asking
([blog #11](../blog/11-design-nfr-adr-and-design-system.md), [PLAN](../PLAN.md#open-questions-for-po--clients)):

- **Offline operation** on the floor if WiFi drops — work offline & sync, or hard-require connectivity? *(The terminal now prototypes the offline-first direction — queued confirmations + sync — pending this decision.)*
- **Cross-docking** — receipt straight to dispatch, skipping put-away — in scope?
- **Serial-number tracking** beyond batches?
- **Partial-collection** and **backorder** policy on dispatch/outbound.
- **Audit/ledger retention** horizon for the auditor.

---

*Coverage note:* the ✅ rows are designed; 🟡 are stated in rules or implied by a control but lack a
dedicated screen/state; ⬜ are not yet designed. The 🟡/⬜ items are the honest backlog for the next
design pass — see [README scope](README.md#scope). The broader **admin-panel UX gaps** found after the
first build (the missing list / create front-doors, global search, a work-queue landing — since built) are framed in the
[admin frontend plan §11](03-admin-frontend-plan.md#11-post-build-review--what-the-gap-closing-pass-delivered) and
tracked in the app [`TODO.md`](../../src/web/admin/TODO.md).
