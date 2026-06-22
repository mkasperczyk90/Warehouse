# Design system (preliminary)

> The thin, durable layer the two front ends share. A first cut, not a 60-component library —
> it grows per slice (blog #9). Implemented in [`prototypes/tokens.css`](prototypes/tokens.css).

## 1. Status colours — the load-bearing tokens

Colour encodes **domain stock status** and must mean the same thing as the
[event-storming legend](../diagrams/README.md) and the
[invariants](../01-domain-overview.md#5-key-business-rules-invariants). It is never decoration.

| Token | Domain status | Colour | Where it must be unmissable |
|---|---|---|---|
| `status.available` | on-hand, sellable | green `#2f9e44` | stock view, ATP |
| `status.reserved` / `allocated` | spoken-for (soft / hard) | blue `#1971c2` | stock view, pick lists |
| `status.blocked` / `quarantined` | QC hold — **never shippable** | red `#e03131` | stock view, receipt, picking |
| `status.expired` | past best-before | dark red `#a61e1e` | stock view, FEFO prompts |
| `status.in-transit` | inter-warehouse move | amber `#f08c00` | stock view, transfers |

Rendered by the **`StatusBadge`** primitive (`.badge--*`): a dot + label pill, so status is never
conveyed by colour alone (accessibility).

## 2. Two type scales (one base, two ergonomics)

| | Admin panel (`.admin`) | Operator terminal (`.terminal`) |
|---|---|---|
| body (`--fs-md`) | 14 px | 20 px |
| display (`--fs-2xl`) | 30 px | 44 px |
| min tap target (`--tap`) | 36 px | **56 px** (≥48 px hard floor) |
| density | tables, multi-column | one thing per screen |

Spacing is a **4 px base** scale (`--s-1`…`--s-12`). Motion is minimal — *a freezer is no place
for a 400 ms ease.*

**Themes.** The terminal ships a light theme **and** a high-contrast (glare) variant — because
glare on a bright cold-store floor is a primary operator driver, not a nice-to-have. The `.hc`
theme darkens every status colour so it passes WCAG AA as a foreground (the default amber fails on
its light tint), removes the faint greys, and makes lines solid. It's toggled from the terminal bar
(◐) and remembered per device; the admin panel stays on the light theme.

## 3. Component inventory

**Shared primitives**
- `StatusBadge` — uses the status tokens above.
- `QuantityWithUnit` (`.qty`) — **never a bare number**; always a unit (echoes the domain's
  unit-safe [`Quantity`](../04-domain-model.md)). Tabular numerals.
- `Toast / error` — surfaced **by error code** (echoes `DomainException` stable codes).
- `Icon` — a small inline-SVG set (`data-icon="scan|receive|putaway|pick|move|tasks|search|more|print"`),
  `currentColor` and sized by font-size. Replaces the earlier bare Unicode glyphs, which render
  inconsistently and risk "tofu" boxes on rugged Android. *(Terminal is converted; the admin
  sidebar still uses glyphs — desktop-safe, a next pass.)*

**Admin panel**
- `DataTable + FilterBar`, `Form / MasterDetail`, `KPI cards`.

**Operator terminal**
- `ScanField` — always focused, **and the commit**: a pick is confirmed by scanning location then
  product, not by tapping from memory (Confirm stays disabled until both scans land).
- `BigActionButton` ×3 (confirm / exception / alternative). Only **hard-stops** (QC-blocked) are
  red; routine exceptions — a shortage, a short-pick — are **neutral**, so honest reporting isn't
  framed as a scary action.
- `Quantity stepper` for ±1, plus a **`NumericKeypad` sheet** for real counts — tap the number to
  open it; a one-tap "= expected" default makes a discrepancy a deliberate entry, not 20 taps.
- `TaskCard / RouteList`, `Confirm sheet`.
- **`OfflineBanner`** — the bar carries an online/offline chip; offline, confirmations queue
  on-device and a sync banner shows the **N queued** count until signal returns.

## 4. Why the UI mirrors the domain

The design system isn't separate from the domain work — it's the same ubiquitous language wearing
a UI:

- `QuantityWithUnit` exists because the domain refuses bare numbers.
- The **temperature badge + hard-stop** on put-away (terminal) is the *environment-compatibility
  invariant* made visible.
- **FEFO** appears as a batch/BBE chip on the pick screen, not as hidden logic.
- **Blocked (QC)** stock is styled to be impossible to miss because it must never be picked.

## 5. Brand

Neutral indigo (`--brand #364fc7`) as a placeholder — *“start neutral”* until a brand/visual
identity is chosen ([blog #11, open question 13](../blog/11-design-nfr-adr-and-design-system.md#questions-for-you-please-answer-before-we-cut-the-first-slice)).
