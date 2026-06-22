# Diagrams

Event-storming boards, kept as **code** — each `.excalidraw` is produced by a `.mjs`
generator next to it, so the diagrams are versionable and reviewable like everything else.
To regenerate after editing a generator:

```bash
# Event-storming boards
node docs/diagrams/generate-event-storming.mjs    # → warehouse.excalidraw
node docs/diagrams/generate-process-outbound.mjs  # → process-outbound.excalidraw
node docs/diagrams/generate-design-stockitem.mjs  # → design-stockitem.excalidraw
# Planning & architecture boards (share _kit.mjs)
node docs/diagrams/generate-story-map.mjs         # → story-map.excalidraw
node docs/diagrams/generate-c4-context.mjs        # → c4-context.excalidraw
node docs/diagrams/generate-c4-container.mjs      # → c4-container.excalidraw
node docs/diagrams/generate-deployment.mjs        # → deployment.excalidraw
node docs/diagrams/generate-clean-architecture.mjs # → clean-architecture.excalidraw
```

Open the `.excalidraw` files at [excalidraw.com](https://excalidraw.com) or with the
*Excalidraw* VS Code extension.

## The three levels of Event Storming (Brandolini)

We zoom in one level at a time — the wide picture first, then one process, then one aggregate.

| Level | Board | Generator | Scope |
|---|---|---|---|
| **Big Picture** | [warehouse.excalidraw](warehouse.excalidraw) | [generate-event-storming.mjs](generate-event-storming.mjs) | the whole domain in business time (5 phases) + the deployment band (5 contexts → 3 services) |
| **Process Level** | [process-outbound.excalidraw](process-outbound.excalidraw) | [generate-process-outbound.mjs](generate-process-outbound.mjs) | one process — outbound order fulfilment — with the full grammar and the unhappy paths (partial reserve, allocation reject, short pick) |
| **Design Level** | [design-stockitem.excalidraw](design-stockitem.excalidraw) | [generate-design-stockitem.mjs](generate-design-stockitem.mjs) | one aggregate — `StockItem` — its commands, events, invariants, the ledger it projects from, and the services that enforce cross-boundary rules |

The conversation these boards came from is reconstructed in
[/docs/meeting/event-storming-session-01.md](../meeting/event-storming-session-01.md).

## Planning & architecture boards

The diagrams from the Part II posts ([#8 Story Mapping](../blog/08-story-mapping-epics-and-tasks.md),
[#9 Design](../blog/09-design-nfr-adr-and-design-system.md)). These share [`_kit.mjs`](_kit.mjs),
a small box-and-arrow drawing helper.

| Board | Generator | From |
|---|---|---|
| [story-map.excalidraw](story-map.excalidraw) | [generate-story-map.mjs](generate-story-map.mjs) | #8 — backbone × stories × release slices |
| [c4-context.excalidraw](c4-context.excalidraw) | [generate-c4-context.mjs](generate-c4-context.mjs) | #9 — C4 L1: system + actors + external systems |
| [c4-container.excalidraw](c4-container.excalidraw) | [generate-c4-container.mjs](generate-c4-container.mjs) | #9 — C4 L2: gateway, 3 services, stores, broker |
| [deployment.excalidraw](deployment.excalidraw) | [generate-deployment.mjs](generate-deployment.mjs) | #9 — where the containers run (cluster, replicas) |
| [clean-architecture.excalidraw](clean-architecture.excalidraw) | [generate-clean-architecture.mjs](generate-clean-architecture.mjs) | #9 — per-module layering, the dependency rule |

## Colour code

🟧 domain event (past tense) · 🟦 command (intent) · 🟨 aggregate (decides) ·
🟪 policy / domain service (whenever… then…) · 🟩 read model / view ·
🟥 actor / external system · 🩷 hotspot (question / decision / exception).

Arrow conventions on the boards: grey spine = business time · orange = aggregate emits an
event · blue = command → aggregate · purple dashed = policy / service · green dashed =
reads a view · pink dashed = exception or loop · ⚡ = a pivotal event that crosses a
service boundary.
