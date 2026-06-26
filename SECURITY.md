# Security Policy

## Reporting a vulnerability

**Please do not open a public issue for security problems.**

Report privately via a GitHub **Security Advisory**:
[Report a vulnerability](https://github.com/mkasperczyk90/Warehouse/security/advisories/new)
(Repository → **Security** → **Advisories** → **Report a vulnerability**). Enable
*Private vulnerability reporting* in the repo settings so this path is available.

You'll get an acknowledgement, and a fix or mitigation plan will be coordinated privately
before any public disclosure.

## Supported versions

This is a portfolio project under active development; only the latest `main` is supported.
Released components are tagged per component (e.g. `admin-vX.Y.Z`, `warehouse-vX.Y.Z`).

## Scope

In scope: the services (`src/Services`, `src/Gateway`), the SPAs (`src/web/*`), auth/identity
(`src/Identity`, Keycloak brokering), and the CI/CD supply chain (`.github/`, `infra/`).

## What the pipeline already enforces

- **CodeQL** static analysis (C# + JS/TS)
- **gitleaks** secret scanning + (enable) GitHub secret-scanning push protection
- **Dependency review** + **Dependabot** (NuGet, npm, GitHub Actions)
- **Trivy** image CVE scan + SBOM artifact in the Docker workflow

See [`docs/gitaction-plan.md`](docs/gitaction-plan.md) for the hardening still on the roadmap
(image signing, provenance/SLSA, pinned action SHAs, OIDC, OpenSSF Scorecard).
