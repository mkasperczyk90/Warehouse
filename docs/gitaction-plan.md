# GitHub / CI-CD maturity plan — what to add to be production-pro

> Scope: what's **missing** (or only half-done) to take this repo from "CI runs" to
> "large-scale production-grade delivery". Grounded in the current setup, not generic advice.
> Legend: ✅ already here · 🟡 partial · ⬜ to add. Priorities: **P0** now · **P1** next · **P2** later.

## Where we are today

Workflows in `.github/workflows/`: `backend.yml`, `frontend.yml`, `e2e.yml`, `docker.yml`,
`deploy-aws.yml`, `codeql.yml`, `gitleaks.yml`, `dependency-review.yml`. Plus `dependabot.yml`.
Central Package Management for .NET. Three deployable services + 2 SPAs.

What's **not** there yet: branch protection / rulesets, CODEOWNERS, PR/issue templates, `SECURITY.md`,
a real **registry publish** (Docker builds with `push: false`), image **signing/SBOM/provenance**,
**versioning + releases** (0 tags, apps pinned at `0.1.0`), **environments + gated CD**, a CI
**migration drift** check, **contract tests**, and a **coverage gate**.

---

## 1. Repository governance & rulesets — **P0**

- ⬜ **Branch protection via Rulesets** (Settings → Rules → Rulesets, the modern replacement for branch
  protection) on `main`:
  - Require a PR; **require status checks**: `Backend`, `Frontend`, `CodeQL`, `Secret scan`.
    *Do not* mark path-filtered workflows (E2E, Docker) as required — a skipped check never reports and
    blocks PRs forever (already noted in `CONTRIBUTING.md`).
  - Require branches up to date, **dismiss stale approvals**, require **conversation resolution**,
    **linear history** (no merge commits) or enforce **squash-only** merges.
  - Require **signed commits** (and enable it on the org), block force-push and deletion of `main`.
  - Require **1–2 reviews**, and **require review from Code Owners**.
- ⬜ **Merge queue** (GitHub native) — serializes "green-at-merge-time" so `main` stays releasable when
  PR volume grows. Pairs with required checks.
- ⬜ **CODEOWNERS** (`.github/CODEOWNERS`) — route reviews by area: `/src/Services/**`, `/src/web/admin/**`,
  `/src/web/terminal/**`, `/.github/**`, `/infra/**`, `/docs/**`. This is the backbone of "require review
  from owners".
- ⬜ **Repo settings**: enable **Dependency graph** (the `Dependency review` workflow fails without it —
  documented), **secret scanning + push protection**, **auto-delete head branches**, default merge =
  **squash**, disable merge commits.

## 2. Repo hygiene & templates — **P1**

- ⬜ `.github/pull_request_template.md` — checklist (tests, i18n EN+PL, ADR if a decision, screenshots
  for UI, migration note).
- ⬜ `.github/ISSUE_TEMPLATE/` — bug / feature / tech-debt forms (YAML issue forms).
- ⬜ `SECURITY.md` — disclosure policy + a private **security advisory** path; enable
  **Private vulnerability reporting**.
- ⬜ `.github/release.yml` — auto-generated release-notes categories (Features / Fixes / Breaking /
  Deps) keyed off PR labels.
- ⬜ Label taxonomy + automation (`labeler` action by path; `release` labels drive the changelog).

## 3. Versioning & releases — **P0**

Today there are **0 tags** and versions are hand-pinned (`0.1.0`). For independently-deployable services
you want **automated, reproducible versions**.

- ⬜ Adopt **SemVer** + **Conventional Commits** (we already commit in that style). Enforce with a
  `commitlint` / PR-title-lint check.
- ⬜ Automated versioning. Two good options:
  - **release-please** (Google) — opens a "release PR" per package, maintains `CHANGELOG.md`, tags on
    merge. Works for the monorepo with per-component manifests (admin, terminal, each service).
  - **Nerdbank.GitVersioning** or **MinVer/GitVersion** for the .NET side — derive assembly/package
    version from git height/tags so every build is stamped.
- ⬜ **`CHANGELOG.md`** generated from Conventional Commits (don't hand-write).
- ⬜ Tag strategy for a monorepo: component-scoped tags (`admin-v1.4.0`, `warehouse-svc-v2.1.0`) so
  services release on their own cadence (the whole point of microservices-from-day-one).
- ⬜ Stamp the build into the artifact: `InformationalVersion` (.NET) and `import.meta.env` (Vite) →
  expose a `/version` or health payload with `{ version, gitSha, buildTime }` for prod debugging.

## 4. Build → publish (the missing half of `docker.yml`) — **P0**

`docker.yml` builds all images with **`push: false`** + Trivy scan. To deploy real artifacts you must
publish them.

- ⬜ **Push to a registry** on `main` and on tags: **GHCR** (`ghcr.io/<org>/warehouse-*`) and/or **ECR**
  (since deploy is AWS ECS). Use `docker/metadata-action` to tag `sha-<short>`, `:edge` (main),
  semver tags on releases, and `:latest` only for the newest stable.
- ⬜ **Multi-arch** (`linux/amd64,linux/arm64`) via Buildx + QEMU — Graviton on ECS is cheaper.
- ⬜ **Build cache** (`type=gha` or registry cache) to keep image builds fast as they grow.
- ⬜ **Supply-chain hardening** on the published image:
  - **Sign** with **cosign** (keyless / OIDC) and verify at deploy time.
  - **SBOM** per image (we already attach an SBOM artifact — also **attach it to the image** as an
    attestation).
  - **Build provenance / SLSA** attestations (`actions/attest-build-provenance`).
  - Fail the build on **Trivy** HIGH/CRITICAL (today the scan reports; make it a gate for release images).

## 5. Environments & continuous delivery — **P0/P1**

`deploy-aws.yml` is **manual, ephemeral, on-demand** (great for review apps) — there is no governed
path to **staging/prod**.

- ⬜ Define **GitHub Environments** (`staging`, `production`) with:
  - **Required reviewers** (manual approval gate for prod), **wait timers**, and
    **deployment branch rules** (only `main`/tags deploy to prod).
  - Environment-scoped **secrets/vars** (per-account AWS config).
- ⬜ **OIDC to AWS** (`aws-actions/configure-aws-credentials` with `role-to-assume`) instead of
  long-lived access keys — short-lived, auditable, least-privilege. (Check whether `deploy-aws.yml`
  still uses static keys; migrate to OIDC.)
- ⬜ **CD flow**: merge to `main` → publish images → auto-deploy **staging** → smoke tests → **manual
  approval** → deploy **production**. Tag/release can drive prod.
- ⬜ **Per-service deploys** — deploy only the service whose image changed (paths filter / changed-image
  matrix), not the whole stack. This is where independent deployability pays off.
- ⬜ **Ephemeral PR preview environments** (you have the ECS plumbing) — spin up on `pull_request`,
  comment the URL, tear down on close.

## 6. Progressive & safe delivery — **P1**

- ⬜ **Blue/green or canary** on ECS via **CodeDeploy** (ECS supports it natively) — shift traffic
  gradually, auto-rollback on alarm.
- ⬜ **Database migrations in the pipeline** with **expand/contract** (backward-compatible) so a rolling
  deploy never breaks the running version. Run EF migrations as a **dedicated job/task** (not on app
  start) gated before the new revision takes traffic.
- ⬜ **Post-deploy smoke + health gate** — hit `/health`, a synthetic happy-path, and watch error rate
  for N minutes; auto-rollback on regression.
- ⬜ **Feature flags** (e.g. OpenFeature) to decouple deploy from release and kill-switch risky changes.

## 7. Quality gates that are still missing — **P1**

- ⬜ **Coverage gate** — publish to **Codecov**/**Coverlet** summary and fail PRs that drop coverage or
  fall under a floor (backend already collects coverage; turn it into a gate).
- ⬜ **EF migration drift check** in `backend.yml` —
  `dotnet ef migrations has-pending-model-changes` per DbContext (called for in `02-bounded-contexts.md`
  but not wired). Fails when the model and migrations diverge.
- ⬜ **Contract tests (PactNet)** between services + the **broker/can-i-deploy** check — the right way to
  test choreography instead of brittle cross-service E2E (planned in PLAN.md Phase 0, not started).
- ⬜ **Frontend lint in CI for the terminal too** (admin now runs ESLint+Prettier; terminal uses
  `expo lint` with no committed config — give it a flat config and gate it).
- ⬜ **Accessibility** check (axe / Playwright-axe) in the admin E2E; **bundle-size budget** gate
  (size-limit) now that code-splitting landed.
- ⬜ **Mutation testing** (Stryker.NET / StrykerJS) on core domain (Inventory ledger, allocation) —
  proves the tests actually catch regressions.
- ⬜ **Load/perf** smoke (k6) against staging for the hot paths (put-away, reservation).

## 8. Security & supply chain — **P1**

- ✅ CodeQL, gitleaks, dependency-review, Dependabot, Trivy already present.
- ⬜ **Pin actions to commit SHAs** (not floating tags) + enable Dependabot for `github-actions` to bump
  them — supply-chain hygiene.
- ⬜ **Least-privilege `GITHUB_TOKEN`**: set `permissions: {}` at workflow top and grant per-job. Audit
  the existing workflows.
- ⬜ **OpenSSF Scorecard** workflow + badge — measures exactly this checklist and trends it.
- ⬜ **Dependabot auto-merge** for patch/minor dev-deps once checks pass (cuts noise).
- ⬜ **CODEOWNERS-gated** `/.github/**` and `/infra/**` so pipeline/infra changes need a platform review.
- ⬜ **Environment secret rotation** + drop any static cloud keys (see OIDC above).

## 9. Operational excellence — **P2**

- ⬜ **Reusable workflows** (`workflow_call`) — factor the repeated "setup node / setup dotnet / cache"
  into shared workflows; the matrix in `frontend.yml`/`e2e.yml` is the first candidate.
- ⬜ **Concurrency** on every workflow (most have it) + **`paths` filters** tuned so unrelated PRs don't
  run the whole matrix.
- ⬜ **Runner cost/scale** — larger runners for the .NET integration tests (Testcontainers), and
  consider self-hosted/ARM runners for image builds.
- ⬜ **Release health → observability** — wire deploy markers into the OTel/APM backend (Grafana/Datadog)
  so a release annotates dashboards; define **SLOs + error budgets** and alert on burn (PLAN Phase 5).
- ⬜ **Renovate** as a richer alternative to Dependabot (grouping, schedules) if dependency volume grows.
- ⬜ **CITEXT/secret scanning for IaC**, plus **`infra/` plan-on-PR / apply-on-merge** if Terraform grows.

## 10. Definition of "pro" — prioritized checklist

**P0 (do first — governance + real artifacts):**
- [ ] Rulesets on `main` (required checks, reviews, signed commits, squash, linear history) + merge queue
- [ ] CODEOWNERS; enable Dependency graph, secret-scanning push protection, auto-delete branches
- [ ] Push images to GHCR/ECR with semver+sha tags; multi-arch; Trivy gate on release images
- [ ] cosign signing + SBOM/provenance attestations on published images
- [ ] Versioning automation (release-please + Nerdbank/MinVer) + CHANGELOG + component tags
- [ ] GitHub Environments (staging/prod) with required reviewers; **OIDC to AWS** (kill static keys)

**P1 (delivery + quality):**
- [ ] main → staging auto-deploy → smoke → approval → prod; per-service deploys; PR preview envs
- [ ] Blue/green or canary on ECS (CodeDeploy) + auto-rollback; migrations as a gated job (expand/contract)
- [ ] Coverage gate; EF migration-drift check; PactNet contract tests + can-i-deploy
- [ ] Pin actions to SHAs; least-privilege GITHUB_TOKEN; OpenSSF Scorecard; PR/issue templates; SECURITY.md

**P2 (scale & polish):**
- [ ] Reusable workflows; bundle-size + a11y gates; mutation + load testing
- [ ] Feature flags; release markers → SLO/error-budget alerting; Renovate

---

*References in this repo: `.github/workflows/*`, `.github/dependabot.yml`, `CONTRIBUTING.md`
(§Required one-time repository settings), `docs/PLAN.md` (Phase 0 / Phase 5),
`docs/02-bounded-contexts.md` (CI migration check, contract tests).*
