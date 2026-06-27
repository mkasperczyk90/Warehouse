# ADR-0008 — Automated, per-component releases (release-please + SemVer)

- **Status:** Accepted
- **Date:** 2026-06-27
- **Deciders:** dev team
- **Related:** [ADR-0001](0001-microservices-from-day-one.md) (independent deployability); [ADR-0009](0009-monorepo-ci-and-signed-images.md) (the pipeline that consumes these tags); [docs/cicd.md](../cicd.md); [docs/github-setup.md](../github-setup.md)

## Context

The repo is a monorepo holding six independently-deployable units (4 .NET services + gateway, 2 SPAs).
Each should be able to release on its **own cadence** — the whole point of ADR-0001's
independent-deployability stance. We need versions that are:

- **reproducible** — derived from history, not hand-typed (hand-typed versions drift and get forgotten);
- **per-component** — a change to the admin SPA must not bump the logistics service;
- **traceable** — every version has a changelog and a tag a deploy can pin to.

Hand-tagging (`git tag`) does none of this reliably, and a single repo-wide version would couple
unrelated components.

## Decision

**Versions are computed, not entered.** We use **Conventional Commits** + **release-please** (manifest
mode) over **SemVer**, one logical component per path:

- The PR **title** is a Conventional Commit (we squash-merge, so the title becomes the commit on `main`).
  `pr-title-lint` enforces the format. `feat:` → minor, `fix:` → patch, `feat!:`/`BREAKING CHANGE:` →
  major (minor while < 1.0.0); `ci/chore/docs/refactor/test` → **no release**.
- On every push to `main`, release-please opens/updates a **per-component release PR** (version + that
  component's `CHANGELOG.md`). **Merging that release PR** creates the tag (`admin-vX.Y.Z`,
  `warehouse-vX.Y.Z`, …) and a GitHub Release. Config: `release-please-config.json` +
  `.release-please-manifest.json`.
- A `Release-As: X.Y.Z` commit footer is the escape hatch for a forced/initial version.

Operationally: release-please runs **without a concurrency group** (so rapid back-to-back release-PR
merges aren't queued-and-cancelled), and the image-publish that follows a release has a job timeout so a
stuck build can't wedge a run.

## Consequences

- **Positive:** nobody types a version; the bump is a mechanical function of commit types. Each
  component releases independently with its own changelog and tag. Deploys pin an exact, signed tag
  (ADR-0009). The discipline that makes it work (Conventional Commit titles) is enforced in CI, not by
  convention.
- **Negative / the price:** it only works if commit titles are honest — a `feat` mislabelled `chore`
  silently skips a release. Tags appear in **two steps** (merge feature PR → merge release PR), which
  surprises people expecting a tag on the first merge. release-please needs the repo setting *Allow GitHub
  Actions to create and approve pull requests*, and the default `GITHUB_TOKEN` doesn't re-trigger
  downstream workflows on the release PR (see [docs/cicd.md](../cicd.md)).
- **Revisit if:** components start needing to release together (a lockstep change) often enough that
  per-component PRs become noise — then a grouped/linked-versions release strategy is the lighter path.
