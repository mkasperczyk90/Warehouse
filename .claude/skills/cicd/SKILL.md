---
name: cicd
description: Work with the Warehouse CI/CD pipeline — cut a release, add a new deployable component to the build/test/image matrix, wire a tag/image, or debug missing tags and the required-check gates. Covers the monorepo affected-detection, the aggregate gate pattern, release-please per-component SemVer, and signed multi-arch GHCR images (ADR-0008, ADR-0009). Use when changing anything under .github/workflows, release-please config, Dockerfiles, or when a release/tag/image didn't appear as expected.
---

# CI/CD & releases

Read [docs/cicd.md](../../../docs/cicd.md) first — it's the source of truth. Decisions:
[ADR-0008](../../../docs/adr/0008-automated-per-component-releases.md) (releases),
[ADR-0009](../../../docs/adr/0009-monorepo-ci-and-signed-images.md) (pipeline). One-time GitHub settings:
[github-setup.md](../../../docs/github-setup.md).

**Golden rules**

- **Never hand-type a version.** Versions come from Conventional-Commit PR titles via release-please.
- **Build only what changed.** Every build workflow has a `detect` job; don't add static all-components matrices.
- **Require the gate, not the job.** `Backend gate` / `Frontend gate` are the required checks (they always
  report); the underlying `Build & test` / per-app jobs are path-skipped and must not be required.
- **Mirror an existing entry** rather than inventing structure.

## Cut a release

1. Land work with a Conventional-Commit PR title — `feat(scope): …` (minor) or `fix(scope): …` (patch);
   `ci/chore/docs/refactor/test` produce **no** release. Squash-merge.
2. release-please opens a **per-component release PR** (version + `CHANGELOG.md`). Merge it → tag
   `<component>-vX.Y.Z` + GitHub Release + semver image.
3. Force a version (e.g. first `0.1.0`, or a jump to `1.0.0`) with a `Release-As: X.Y.Z` commit footer.

## Add a new deployable component to the pipeline

Touch these in lockstep (use an existing component, e.g. `gateway`, as the template):

1. **`release-please-config.json`** — add the package (path → `release-type` + `component`).
2. **`.release-please-manifest.json`** — add the path at its current version.
3. **`.github/workflows/docker.yml`** — add a `dorny/paths-filter` entry + a `map` entry in the
   `github-script` step (`{ name, context, dockerfile }`); if it's a backend service it must also be in
   the `allBackend` fan-out set.
4. **`.github/workflows/release-please.yml`** — add the path → image `map` entry (mirrors docker.yml).
5. **Dockerfile** — accept `ARG VERSION`/`ARG GIT_SHA` and stamp the build (`-p:Version` for .NET, an env
   for the SPAs), so `/version` matches the tag.

For a backend **service**: also add it to `backend.yml`'s `publish` matrix. For a new **shared** backend
project: add its path to the `shared_backend` filter (docker.yml) and the `backend` filter (backend.yml)
so it correctly rebuilds/tests everything.

## Images

The reusable **`publish-image.yml`** owns build → Trivy → GHCR push → cosign sign → SBOM + provenance.
Don't duplicate that logic; call it. Channels: `ci` (PR, build-only, single arch), `edge` (main push,
`edge`+`sha`), `release` (semver + `latest`). Pushes are multi-arch (amd64+arm64). Verify a signature with
`cosign verify ghcr.io/<owner>/warehouse-<name>:<tag> --certificate-oidc-issuer https://token.actions.githubusercontent.com --certificate-identity-regexp '.*'`.

## When something doesn't appear

- **Release PR never opens** → enable *Settings → Actions → General → Allow GitHub Actions to create and
  approve pull requests*.
- **Release PR merged but no tag** → the run that should tag was cancelled/stuck; release-please won't
  retroactively re-create it (manifest already bumped). Create it by hand:
  `git tag <component>-vX.Y.Z <release-commit> && git push --tags`. Prevention is already in place (no
  `concurrency` on release-please + a publish `timeout-minutes`).
- **`No release necessary`** → the change was `ci`/`chore`/`docs`; use `feat`/`fix` or `Release-As:`.
- **A required check blocks a PR that didn't touch its area** → you required a path-filtered job; require
  the `*-gate` check instead.

## Definition of done (for pipeline changes)

`yaml` valid; affected-detection still maps shared deps → all backend; gate jobs still always-run; no new
static full-matrix build; docs/cicd.md updated if behaviour changed.
