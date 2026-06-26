# Contributing

How to build, test, and ship the Warehouse system locally and through CI.

## Prerequisites

- **.NET SDK 10** (pinned in [`global.json`](global.json) — `rollForward: latestFeature`)
- **Node.js 20** (the SPAs and e2e suites; matches the `node:20` build images)
- **Docker** — required for the backend **integration tests** (Testcontainers spins up PostgreSQL)
  and for building/running the container images and the Aspire AppHost

## Layout

| Path | What it is |
|------|------------|
| `src/AppHost` | .NET Aspire orchestrator — runs the whole stack (Postgres, RabbitMQ, 3 APIs, gateway, both SPAs) |
| `src/Gateway`, `src/Services/*` | Backend services (.NET 10, minimal APIs) |
| `src/web/admin` | Admin SPA — Vite + React 19 (MSW-mocked) |
| `src/web/terminal` | Operator terminal — Expo / React Native Web (MSW-mocked) |
| `tests/*` | Backend unit / architecture / integration tests (xUnit) |
| `tests/e2e/*` | Playwright-BDD end-to-end suites |

## Build & test locally

### Backend (.NET)

```bash
dotnet build Warehouse.slnx -c Release          # warnings-as-errors = the analyzer/format gate
dotnet test  Warehouse.slnx -c Release           # unit + architecture + integration (needs Docker)
```

Coverage (same as CI) — needs `coverlet.collector`, already referenced by the test projects:

```bash
dotnet test Warehouse.slnx -c Release --collect "XPlat Code Coverage"
```

Run the full system with Aspire:

```bash
dotnet run --project src/AppHost/Warehouse.AppHost
```

### Frontend (run inside `src/web/admin` or `src/web/terminal`)

```bash
npm ci
npm run mock:init        # one-time: generate public/mockServiceWorker.js (gitignored)
npm run typecheck
npm run test:run         # admin only (vitest); terminal has no unit tests yet
npm run build            # admin → dist/   (terminal: npm run build:web)
```

### End-to-end (run inside `tests/e2e/admin` or `tests/e2e/terminal`)

The Playwright config boots the app's own dev server, so the **web app's** deps and MSW worker must
be present too:

```bash
# in the matching src/web/<app>:  npm ci && npm run mock:init
npm ci
npx playwright install --with-deps chromium
npm test
```

### Docker images

Backend images build from the **repository root** (cross-project references + `Directory.*.props`);
the SPAs build from their own directory. Each Dockerfile's header has the exact command, e.g.:

```bash
docker build -t warehouse-gateway -f src/Gateway/Warehouse.Gateway/Dockerfile .
docker build -t warehouse-admin   src/web/admin
```

## Continuous integration

Workflows live in [`.github/workflows`](.github/workflows). Nothing deploys — CI builds, tests, scans,
and uploads artifacts only.

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| **Backend** | push `main` / PR (backend paths) | build, run all test projects, coverage summary, publish each service as an artifact |
| **Frontend** | push `main` / PR (`src/web/**`) | per-SPA typecheck, admin unit tests, build, upload `dist/` |
| **E2E** | PR (web/e2e paths) · nightly · manual | Playwright-BDD for admin + terminal |
| **Docker images** | push `main` / PR (src + Dockerfiles) | build all 6 images (no push), Trivy CVE scan → Security tab, SBOM artifact |
| **CodeQL** | push / PR / weekly | static security analysis (C# + JS/TS) |
| **Secret scan** | push `main` / PR / weekly | gitleaks — fails on a leaked secret |
| **Dependency review** | PR | flags vulnerable / disallowed-license deps (**needs Dependency graph — see below**) |

**Test visibility:** each suite publishes a per-test pass/fail **Check** (via `dorny/test-reporter`),
Playwright also annotates failures inline, and backend coverage is rendered into the run's job summary.

**Dependencies:** [Dependabot](.github/dependabot.yml) opens weekly PRs (NuGet, npm, GitHub Actions),
grouped to keep the noise down. Let CI run on those PRs and merge the green ones.

## Required one-time repository settings

These are **GitHub settings, not code** — a maintainer must enable them once:

1. **Dependency graph** — *Settings → Code security → Dependency graph* → **Enable**.
   The **Dependency review** workflow fails without it (`Dependency review is not supported on this
   repository`).
2. **Branch protection (recommended)** — protect `main` and mark **Backend** / **Frontend** /
   **CodeQL** / **Secret scan** as required status checks.
   Caveat: a workflow skipped by its `paths:` filter never reports — don't mark a path-filtered
   workflow as required, or PRs that don't touch those paths will block forever.

## Good to know

- **E2E is intentionally its own workflow** (heavier, can flake when booting dev servers) — keep it
  **non-required** and rely on the nightly run + the per-test Check.
- **Trivy SARIF** upload to the Security tab needs a **public repo** (or GitHub Advanced Security on a
  private one). The image build itself runs regardless.
- **Frontend tests** get extra timeout + retries on CI only (`process.env.CI` in `vite.config.ts`) —
  the shared CI runners are slower than a dev box. Local runs keep the strict defaults.
- `dorny/test-reporter` needs a writable token, so the per-test Check won't appear on PRs **from
  forks** (their token is read-only). Same-repo branches are fine.
