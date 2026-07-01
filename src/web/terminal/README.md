# Warehouse Operator Terminal

The **Operator terminal** front end — a React Native (Expo) app for floor staff on a
rugged handheld: _one hand, gloves, cold room, glare_. Scanner-first, huge tap targets
(≥48 px), one task at a time, zero prose.

It implements the terminal screens from the design pass
([`docs/design/`](../../../docs/design/README.md)) — the prototypes in
[`docs/design/prototypes/terminal-*.html`](../../../docs/design/prototypes/) are the
design source of truth, and the tokens here are a direct port of
[`tokens.css`](../../../docs/design/prototypes/tokens.css).

> **Backend is mocked at the network boundary (ADR-0006).** Like the admin panel, the
> terminal calls `fetch` through the single [`src/core/api/client.ts`](src/core/api/client.ts)
> seam from day one; [MSW](src/core/mocks/handlers.ts) intercepts those requests and returns
> fixtures — a Service Worker on web (`mocks/browser`), a request interceptor on a real
> handheld (`mocks/native`). Going live is turning the worker off, never a rewrite.
> Run `npm run mock:init` once to generate `public/mockServiceWorker.js` (gitignored).

## Folder structure

```
src/
├── app/          # expo-router routes + global config (_layout). Thin files
│                 #   that re-export a feature screen — the router glue only.
├── core/         # infrastructure services & foundations (the mock API client)
├── features/     # business logic by domain — THE MAIN WORK AREA
│   ├── Tasks/        # operator hub                (terminal-1-hub)
│   ├── Receiving/    # goods receipt   · UC-02     (terminal-2-receive)
│   ├── PutAway/      # put-away        · UC-04     (terminal-3-putaway)
│   ├── Picking/      # picking         · UC-10     (terminal-4-pick)
│   ├── Movement/     # move stock      · UC-06     (terminal-5-move)
│   ├── Packing/      # packing         · UC-11     (terminal-6-pack)
│   ├── Scan/         # universal scan dispatcher   (Scan tab)
│   └── Lookup/       # read-only inquiry           (Look up tab)
├── shared/       # agnostic shared assets — the UI kit + design tokens
│   ├── theme/        # tokens.ts (the .terminal scale + status colours)
│   └── ui/           # StatusBadge, ScanField, BigActionButton, …
├── assets/       # static assets (images, fonts)
└── navigation/   # route definitions (typed path map) + router config
```

Each feature is self-contained: `XScreen.tsx` (view) + `x.model.ts` (types, fixtures,
data getter) + `index.ts` (public API). Cross-feature imports go through the feature's
`index.ts`; UI primitives through `@/shared/ui`. The `@/*` alias maps to `src/*`.

### A note on `app/` vs `navigation/`

expo-router is file-based: the tree under `src/app/` _is_ the router. So the route files
stay thin (a one-line re-export) and `navigation/routes.ts` holds the typed path map
(`ROUTES.pack`) so features navigate by name instead of bare string literals.

## Screens & navigation

| Route      | Feature screen            | Use case | Prototype            |
| ---------- | ------------------------- | -------- | -------------------- |
| `/`        | `Tasks/TaskHubScreen`     | landing  | `terminal-1-hub`     |
| `/receive` | `Receiving/ReceiveScreen` | UC-02    | `terminal-2-receive` |
| `/putaway` | `PutAway/PutAwayScreen`   | UC-04    | `terminal-3-putaway` |
| `/pick`    | `Picking/PickingScreen`   | UC-10    | `terminal-4-pick`    |
| `/move`    | `Movement/MovementScreen` | UC-06    | `terminal-5-move`    |
| `/pack`    | `Packing/PackingScreen`   | UC-11    | `terminal-6-pack`    |
| `/scan`    | `Scan/ScanScreen`         | —        | Scan tab             |
| `/lookup`  | `Lookup/LookupScreen`     | —        | Look up tab          |

Navigation mirrors the operator clickpath: the hub launches each task, and tasks loop
back to the hub when done (`/pick` flows on to `/pack`).

### The three tabs (BottomNav)

The bottom nav switches between three top-level roots; `router.navigate` gives tab-like
behaviour (returning to a root already in history rather than stacking duplicates):

- **Tasks** — the hub: today's task piles.
- **Scan** — _“I have a physical thing, what do I do with it?”_ A context-free scanner
  that resolves any code (ASN / order / EAN / LPN / location) and dispatches to the
  matching task.
- **Look up** — _“I want to know something.”_ Read-only, keyboard-driven search over
  products (stock + ATP), locations (capacity, room type) and batches (status, BBE/FEFO).
- **More** — no screen yet, so it stays dimmed and inert.

## Design system

`src/shared/theme/tokens.ts` carries the `.terminal` type scale and the **status colours
that encode domain stock status** (never decoration). The shared primitives in
`src/shared/ui/`:

- `StatusBadge` — dot + label pill; status is never colour alone (gloves + glare).
- `QuantityWithUnit` — never a bare number; always a unit, tabular numerals.
- `ScanField` — always-focused primary input; `onScan` fires on submit (Enter).
- `BigActionButton` — confirm / exception / alternative; 56 px tap floor.
- `Stepper`, `Chip`, `CheckRow`, `Card`, `TopBar`, `BottomNav`, `ScreenScaffold`.

The two **green check rows** on put-away / move make the _environment-compatibility
invariant_ visible; FEFO shows as a batch/BBE badge on picking.

## Running

```bash
npm install
npm run mock:init  # one-time: generate public/mockServiceWorker.js (gitignored)
npm run web        # browser preview (fastest)
npm start          # Expo dev server — scan QR with Expo Go for a real handheld
npm run android    # or a connected Android device / emulator
```

`npm run typecheck` runs the TypeScript compiler with no emit.

## Stack

Expo SDK 52 · React Native 0.76 · expo-router (file-based, typed routes) · TypeScript.
