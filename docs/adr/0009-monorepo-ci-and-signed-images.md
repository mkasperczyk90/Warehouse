# ADR-0009 — Monorepo CI: affected-only builds, aggregate gates, and signed images

- **Status:** Accepted
- **Date:** 2026-06-27
- **Deciders:** dev team
- **Related:** [ADR-0001](0001-microservices-from-day-one.md) (independent deployability); [ADR-0008](0008-automated-per-component-releases.md) (the releases this pipeline builds); [docs/cicd.md](../cicd.md); [docs/github-setup.md](../github-setup.md)

## Context

In a monorepo, the naive CI builds and tests **everything** on every change. A one-line admin tweak
rebuilt all six container images and ran the whole .NET suite. That's slow, wasteful, and obscures what a
change actually affects. We want CI that scales with the **blast radius** of the change, not the size of
the repo — while keeping branch protection honest and the artifacts trustworthy.

Two GitHub-specific constraints shape the design:

1. **Path-filtered checks can't be required.** A check skipped by a `paths:` filter never reports, so
   marking it a required status check blocks PRs that don't touch those paths *forever*.
2. **Container images are a supply-chain surface.** A pushed image needs to be attributable and verifiable
   (who built it, from what source, with what dependencies).

## Decision

**Build what changed; gate with an always-running aggregate; sign and attest everything pushed.**

- **Affected detection + dynamic matrix.** A `detect` job (`dorny/paths-filter`) computes which components
  changed, with **dependency fan-out** (a change to `SharedKernel`/`Contracts`/`ServiceDefaults`/
  `Directory.*.props` rebuilds *all* backend images; SPAs are independent). A `github-script` step emits a
  matrix of only the affected units; `docker.yml` and `frontend.yml` build only those.
- **Aggregate gate jobs.** `backend.yml` and `frontend.yml` run on **every** PR, skip the heavy work when
  irrelevant, and end in an always-running **`Backend gate` / `Frontend gate`** job that passes if the
  real job succeeded *or was skipped*. We require **those** checks — they always report, sidestepping the
  path-filter trap. (We do not use a third-party build-graph tool like Nx/Bazel — GitHub-native
  path-filters are enough at this size; revisit if the dependency graph outgrows hand-maintained mapping.)
- **Signed, attested, multi-arch images.** One reusable `publish-image.yml` builds each image, scans it
  (Trivy), pushes to **GHCR**, **signs with cosign** (keyless/OIDC), and attaches an **SBOM + SLSA
  provenance** attestation. Pushes are **multi-arch** (amd64 + arm64); PRs build a single arch so the
  image can be loaded and scanned. Tags: `edge` + `sha-<short>` on `main`, semver + `latest` on release.

## Consequences

- **Positive:** CI cost tracks the change (one admin commit builds one image, not six). Branch protection
  is enforceable via the gate checks without the path-filter deadlock. Every published image is
  verifiable (`cosign verify`), has a bill of materials, and runs on Graviton/arm64. The build/scan/sign
  logic lives in **one** reusable workflow, so the six call-sites stay thin.
- **Negative / the price:** the dependency fan-out map is **hand-maintained** — add a shared project and
  you must teach `detect` that it rebuilds the backend, or you'll ship a stale image. The gate indirection
  is a layer to understand ("why is `Build & test` skipped but the PR green?"). Multi-arch builds use QEMU
  emulation for arm64, which is slower than native.
- **Revisit if:** the affected-detection mapping becomes error-prone or the build graph grows complex
  enough to warrant a real build system (Nx for the JS side, or Bazel across both), which compute the
  affected set and cache results from the actual dependency graph.
