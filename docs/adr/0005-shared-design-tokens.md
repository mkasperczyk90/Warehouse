# ADR-0005 — One design-token layer (`tokens.css`) shared across both front ends

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** dev team
- **Related:** blog [#11](../blog/11-design-nfr-adr-and-design-system.md), [#18](../blog/18-the-admin-panel-architecture.md); design [system](../design/00-design-system.md), [plan](../design/03-admin-frontend-plan.md); [ADR-0004](0004-admin-panel-separate-spa.md)

## Context

Colour in this system is **never decoration** — it encodes domain stock status (`available`,
`reserved`, `blocked`/QC, `expired`, `in-transit`) and must mean the same thing on the event-storming
board, the terminal, and the admin panel. The tokens already exist as the single source of truth in
[`docs/design/prototypes/tokens.css`](../design/prototypes/tokens.css): status colours, two type scales
(`.admin` / `.terminal`), spacing, radius.

With [ADR-0004](0004-admin-panel-separate-spa.md) the two front ends are *separate* codebases. The
danger of separate codebases is **token drift** — `status.blocked` becoming a slightly different red in
each app, which quietly breaks the one rule the whole design rests on. The terminal already had to copy
the tokens once (porting `tokens.css` → `tokens.ts` because React Native has no CSS). The question is
how the admin panel gets its tokens without forking a third, divergent copy.

## Decision

`tokens.css` stays the **single source of truth** and is **consumed, not re-authored**. Because the
admin panel runs in the browser, it uses `tokens.css` *directly* — the file is vendored into
`src/web/admin/src/shared/theme/tokens.css` by a sync step, never hand-edited there. The terminal's
`tokens.ts` and the admin's vendored copy are both **derived artifacts** of the one CSS file; any token
change is made in the source and flows out.

## Consequences

- **Positive:** the load-bearing status colours cannot drift between the two apps; the admin gets the
  real type scale and spacing for free; new tokens added per slice land in one place. The admin panel
  pays *zero* porting cost — for the browser, the source format *is* the consumable format.
- **Negative / the price:** the admin app carries a vendored copy rather than importing across the repo
  boundary, so a sync step (or a check in CI) is needed to keep the copy fresh; a stale copy is a real
  failure mode. Accepted because a vendored file keeps the app self-contained and buildable in isolation,
  and the sync is mechanical. The terminal's `tokens.ts` remains a hand-maintained port — the one place
  drift can still creep in, watched at review time.
- **Revisit if:** the token set grows enough that hand-syncing the terminal port becomes error-prone —
  then generate `tokens.ts` from `tokens.css` (a build step, its own ADR).
