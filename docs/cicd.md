# CI/CD & Releases

How code becomes a tagged, signed, deployable artifact. Decisions behind this live in
[ADR-0008](adr/0008-automated-per-component-releases.md) (releases/versioning) and
[ADR-0009](adr/0009-monorepo-ci-and-signed-images.md) (pipeline + supply chain); the one-time GitHub
settings are in [github-setup.md](github-setup.md).

> TL;DR — you never type a version. Write a Conventional-Commit PR title, squash-merge, and CI does the
> rest: build only what changed, then release-please opens a release PR whose merge cuts the tag, the
> GitHub Release, and the signed multi-arch image.

## Workflows at a glance

| Workflow | Trigger | What it does |
|---|---|---|
| **Backend** (`backend.yml`) | every PR · push `main` (backend paths) | detect → build (warnings-as-errors) → **EF model-drift check** → test (unit + arch + Testcontainers) → coverage → publish service artifacts → **`Backend gate`** |
| **Frontend** (`frontend.yml`) | every PR · push `main` (`src/web/**`) | detect affected SPA → typecheck → lint/format (admin) → unit tests → build → upload `dist/` → **`Frontend gate`** |
| **Docker images** (`docker.yml`) | PR · push `main` (src/docker paths) | detect affected images → build only those (PR: build-only; `main`: push `edge`+`sha`) |
| **Publish image** (`publish-image.yml`) | reusable (`workflow_call`) | build → Trivy scan → push GHCR → **cosign sign** → **SBOM + SLSA provenance** |
| **Release Please** (`release-please.yml`) | push `main` | maintain per-component release PRs; on merge → **tag + GitHub Release** + publish semver image |
| **E2E** (`e2e.yml`) | PR (web/e2e) · nightly · manual | playwright-bdd for admin + terminal |
| **CodeQL · Secret scan · Dependency review** | PR / push / schedule | static security, gitleaks, vulnerable/disallowed deps |

Everything is **affected-aware**: a change to `src/web/admin` builds/tests/publishes only `admin`. A
change to a **shared** backend project (`SharedKernel`, `Contracts`, `ServiceDefaults`, `Directory.*.props`,
`global.json`) fans out to **all** backend images (the service Dockerfiles build from the repo root).

## What runs when

- **On a pull request** — affected build/test/lint + image **build-only** (no push) + security scans. The
  `Backend gate` / `Frontend gate` checks always report (even when their heavy job is skipped), so they're
  the ones marked **required** in the branch ruleset.
- **On push to `main`** (i.e. a squash-merge) — same, plus **push `edge` + `sha-<short>` images** to GHCR
  for the affected components, plus release-please updates its release PRs.
- **On merging a release PR** — release-please creates the **tag** + **GitHub Release**, then builds and
  pushes the **semver** image (`X.Y.Z`, `X.Y`, `latest`) for that component.

## Versioning & releases

Versions are **computed from commit types** (SemVer), never hand-entered — see ADR-0008.

1. **Write a Conventional-Commit PR title** (`feat(admin): …`, `fix(logistics): …`). We squash-merge, so
   the title becomes the `main` commit. `pr-title-lint` enforces it.
   - `feat:` → **minor**, `fix:` → **patch**, `feat!:` / `BREAKING CHANGE:` → **major** (minor while < 1.0.0).
   - `ci` / `chore` / `docs` / `refactor` / `test` → **no release**.
2. **release-please opens a per-component "release PR"** with the computed version + `CHANGELOG.md`.
3. **Merge the release PR** → it tags `<component>-vX.Y.Z`, cuts a GitHub Release, and publishes the
   semver image. Tags are **per component**: `admin-v…`, `terminal-v…`, `gateway-v…`, `warehouse-v…`,
   `logistics-v…`, `masterdata-v…`.
4. **Override** a version with a `Release-As: X.Y.Z` commit footer (e.g. to cut the first `0.1.0`).

Current versions live in `.release-please-manifest.json`; the component → path mapping in
`release-please-config.json`.

## What gets published

| Artifact | Where | When | Notes |
|---|---|---|---|
| **Service builds** (`dotnet publish`) | workflow artifacts (`service-*`) | backend run | 4 services, downloadable from the run |
| **SPA bundles** (`dist/`) | workflow artifacts (`web-*-dist`) | frontend run | admin + terminal |
| **Container images** | `ghcr.io/<owner>/warehouse-<name>` | `main` push (edge/sha) · release (semver) | multi-arch amd64+arm64 on push |
| **Image signature** | Rekor transparency log (cosign keyless) | every pushed image | `cosign verify …` to check |
| **SBOM + SLSA provenance** | attached to the image (attestation) | every pushed image | supply-chain audit |
| **Trivy CVE scan** | Security tab (SARIF) | every build | report-only today |
| **Git tags + GitHub Releases** | repo | release-PR merge | per component, with changelog |
| **`/version` endpoint** | each service + gateway, runtime | — | `{ service, version, gitSha, buildTime }`, stamped from the image tag |

Verify a signature:

```bash
cosign verify ghcr.io/<owner>/warehouse-gateway:latest \
  --certificate-identity-regexp '.*' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com
```

## Branch protection (the gate pattern)

`Backend` and `Frontend` are path-filtered, so their raw jobs can't be required (a skipped check never
reports). Instead each ends in an always-running **`Backend gate` / `Frontend gate`** job that passes when
the real job succeeded *or was skipped*. The ruleset
([`main-branch.json`](../.github/rulesets/main-branch.json)) requires `Backend gate`, `Frontend gate`,
the CodeQL analyses, and `Gitleaks` — plus signed commits, linear history, and squash-only merges.

## Required one-time GitHub settings

See [github-setup.md](github-setup.md). The two that bite if missed:

- **Allow GitHub Actions to create and approve pull requests** — release-please can't open its release PR
  without it (no setting → no tags).
- **Workflow permissions / packages** — GHCR push uses the job's `packages: write`; cosign uses
  `id-token: write`. Both are already granted per job.

## Troubleshooting & lessons

- **A merged release PR but no tag.** release-please creates the tag in the run triggered by the release-PR
  merge. If that run is cancelled or wedged, the tag is missed and release-please won't retroactively
  re-create it (the manifest already shows the version). **Fix:** create it by hand —
  `git tag <component>-vX.Y.Z <release-commit> && git push --tags` — then add a GitHub Release if wanted.
  **Prevented now by:** no `concurrency` group on release-please (rapid merges don't cancel each other) +
  a `timeout-minutes` on image publish (a stuck build can't hang the run).
- **`No release necessary`.** Only `feat`/`fix` bump — a `ci`/`chore`-only change correctly produces no
  release. Use a real `feat`/`fix` or a `Release-As:` footer.
- **Dependency review fails.** Enable *Dependency graph* (Settings → Code security).
