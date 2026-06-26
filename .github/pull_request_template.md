<!--
PR title MUST follow Conventional Commits — it becomes the squashed commit and
feeds release-please / the changelog. Examples:
  feat(admin): URL-driven stock filters
  fix(logistics): release reservations on cancel
  chore(ci): publish images to GHCR
Allowed types: feat, fix, perf, refactor, docs, test, build, ci, chore, revert.
Add `!` after the scope for a breaking change (feat(api)!: …).
-->

## What & why

<!-- One or two sentences. Link the issue (Closes #123) if there is one. -->

## How

<!-- Key implementation notes a reviewer (or future-you) needs. -->

## Checklist

- [ ] Title is a Conventional Commit (`type(scope): summary`)
- [ ] Builds green: backend `dotnet build -c Release` / frontends `npm run build`
- [ ] Tests added/updated and passing (`dotnet test`, `npm run test:run`, e2e if UI)
- [ ] Lint/format clean (`npm run lint` + `npm run format:check` for admin)
- [ ] i18n updated in **both** `en.ts` and `pl.ts` (any user-facing string)
- [ ] DB change ships an EF migration and is **backward-compatible** (expand/contract)
- [ ] An ADR added/updated if this is a notable decision (`docs/adr/`)
- [ ] Screenshots / clips for UI changes

## Risk & rollout

<!-- Breaking change? Migration? Feature-flagged? How to roll back? -->
