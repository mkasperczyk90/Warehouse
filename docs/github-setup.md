# GitHub setup — governance, hygiene, versioning

How to wire up the files added for governance (1), hygiene (2) and versioning/releases (3).
Tuned for a **solo maintainer**: PRs + status checks, but **no required human review** (you can't
approve your own PR), and you can self-merge once checks are green.

What's a **file in the repo** vs. a **GitHub UI setting**:

| Concern | Lives as | Where |
|---|---|---|
| Code owners | file | `.github/CODEOWNERS` |
| PR / issue templates | files | `.github/pull_request_template.md`, `.github/ISSUE_TEMPLATE/` |
| Security policy | file | `SECURITY.md` |
| Release-notes taxonomy | file | `.github/release.yml` |
| Auto-labelling | files | `.github/labeler.yml` + `.github/workflows/labeler.yml` |
| Conventional-commit gate | file | `.github/workflows/pr-title-lint.yml` |
| Versioning / releases | files | `.github/workflows/release-please.yml`, `release-please-config.json`, `.release-please-manifest.json` |
| Branch protection / merge queue / signed commits | **UI setting** (seed = `.github/rulesets/main-branch.json`) | Settings → Rules → Rulesets |
| Enable features (dep graph, secret scanning…) | **UI setting** | Settings → Code security |

---

## 1. Governance

### One-time repo settings (Settings → …)

- **General → Pull Requests**: allow **squash** only, "Default to PR title for squash commits",
  **Automatically delete head branches**.
- **Code security**: enable **Dependency graph** (the `Dependency review` workflow fails without it),
  **Secret scanning** + **Push protection**, and **Private vulnerability reporting** (powers `SECURITY.md`).
- **Code security**: keep Dependabot alerts/updates on (already configured in `.github/dependabot.yml`).

### Commit signing — do this BEFORE enabling the ruleset

The ruleset **requires signed commits**; set this up first or you'll lock yourself out. SSH signing is
simplest:

```bash
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true
```

Then add the **same key** to GitHub as a **Signing key** (Settings → SSH and GPG keys → New, key type
*Signing*). Tip: import the ruleset with `"enforcement": "evaluate"` first, confirm your commits show
**Verified**, then flip to `"active"`.

### Import the ruleset

Settings → **Rules → Rulesets → New ruleset → Import a ruleset** → pick
[`.github/rulesets/main-branch.json`](../.github/rulesets/main-branch.json). It encodes:

- Target = default branch (`main`); block **deletion** and **force-push**; **linear history**.
- **Require a PR**, **0 approvals** (solo), require **conversation resolution**, **squash-only** merges.
- **Require signed commits**.
- **Required status checks** (see the caveat below).
- Bypass: the **repository admin** may merge a PR even if a rule would otherwise block — handy for a solo
  repo. Remove the `bypass_actors` entry if you want zero bypass.

### Required status checks — mind the path-filter trap

A check that is **skipped by a `paths:` filter never reports**, so requiring it blocks PRs that don't
touch those paths *forever*. `Backend` and `Frontend` are path-filtered, so they are **not** in the
required set. The ruleset requires only the checks that run on **every** PR and have unambiguous names:

- `Analyze csharp`, `Analyze javascript-typescript` (CodeQL)
- `Gitleaks` (secret scan)

To also gate Backend/Frontend cleanly, add a tiny **aggregator job** that always runs and `needs:` the
real jobs (skipped jobs report success), then require *that* single check. Until then, rely on
**strict** mode ("branches up to date") + a quick look at the PR's checks before you self-merge.
Note: the `admin`/`terminal` job names are shared by `frontend.yml` and `e2e.yml` — don't require those
two names directly (ambiguous).

### Merge queue (optional for solo)

Settings → Rules → add a **merge queue** to the ruleset if PR volume ever justifies it. For a single dev
it mostly adds latency — skip for now.

---

## 2. Hygiene — nothing to enable

`CODEOWNERS`, the PR/issue templates, `SECURITY.md`, `release.yml`, and the labeler all activate on the
next push. Create the labels referenced by `release.yml` / `labeler.yml` (or let the labeler's
`sync-labels` manage area labels). Suggested labels: `feat fix perf refactor docs chore ci build
breaking dependencies enhancement bug area:* ignore-for-release`.

---

## 3. Versioning & releases

### Conventional Commits

We **squash-merge**, so the **PR title** becomes the commit on `main`. `pr-title-lint.yml` enforces
`type(scope): summary`. Allowed types match the changelog sections in `release-please-config.json`.

### release-please (the source of truth for versions/tags/changelogs)

`release-please.yml` runs on every push to `main`. It keeps a **per-component release PR** that bumps the
version and updates that component's `CHANGELOG.md`. Merging the release PR:

- tags the component — `admin-vX.Y.Z`, `terminal-vX.Y.Z`, `gateway-vX.Y.Z`, `warehouse-vX.Y.Z`,
  `logistics-vX.Y.Z`, `masterdata-vX.Y.Z` (config in `release-please-config.json`, current versions in
  `.release-please-manifest.json`),
- cuts a GitHub Release with notes grouped by type.

So each service/SPA releases on its **own cadence** — the point of independent deployability. Web
components (`node`) also bump their `package.json`; backend components (`simple`) are tag + changelog.

> Limitation: the default `GITHUB_TOKEN` won't re-trigger CI on the release PR. If a release must run the
> full pipeline, swap in a **PAT** or **GitHub App** token in `release-please.yml`.

### Build stamping → the `/version` endpoint

Every service and the gateway expose **`GET /version`** → `{ service, version, gitSha, buildTime }`
(added in `ServiceDefaults`, safe in all environments). The baseline version is
`VersionPrefix` in `Directory.Build.props` (`0.1.0`). CI should override it from the released tag and
inject the commit/time:

```bash
dotnet build -c Release \
  -p:Version=2.1.0 \
  -p:SourceRevisionId=$GITHUB_SHA
# or pass BUILD_GIT_SHA / BUILD_TIME as env to the running container
```

### Image publishing → GHCR, tagged + signed (wired)

The reusable [`publish-image.yml`](../.github/workflows/publish-image.yml) builds an image and
publishes it to **GHCR** (`ghcr.io/<owner>/warehouse-<name>`), scanned (Trivy), **signed with cosign**
(keyless / OIDC) and carrying an **SBOM + SLSA provenance** attestation. It's called by:

- **`docker.yml`** — on PRs: `ci` channel = build-only gate (no push). On push to `main`: `edge` channel
  → pushes `:edge` + `:sha-<short>`.
- **`release-please.yml`** — when a release PR merges: `release` channel → pushes `:X.Y.Z`, `:X.Y`,
  `:latest`, `:sha-<short>` for the released component, **in the same run** (sidesteps the
  GITHUB_TOKEN-doesn't-re-trigger limitation). The version is also baked into the build via the
  Dockerfile `VERSION`/`GIT_SHA` args, so the image's `/version` matches its tag (verified end-to-end).

One-time setup:

- **Permissions** are already granted per job (`packages: write` for GHCR, `id-token: write` for cosign,
  `security-events: write` for the Trivy SARIF). No PAT needed — GHCR push uses the `GITHUB_TOKEN`.
- After the first push, the GHCR package is created **private**; link it to the repo and (optionally)
  set visibility under **Packages**. Inherit the repo's access so deploys can pull.
- **Verify a signature**: `cosign verify ghcr.io/<owner>/warehouse-gateway:latest \
  --certificate-identity-regexp '.*' --certificate-oidc-issuer https://token.actions.githubusercontent.com`
  (tighten the identity regexp to your repo once stable).
- Trivy SARIF upload to the Security tab needs a **public repo** or GHAS; the scan itself runs regardless.

### Optional next step — derive the .NET version from git (Nerdbank.GitVersioning)

Not wired yet **on purpose**: NBGV/MinVer read git history, which breaks two things in the current
pipeline unless handled:

1. **Shallow clones** — add `fetch-depth: 0` to `actions/checkout` in `backend.yml` (and any workflow
   that builds .NET).
2. **Docker builds have no `.git`** — the image build context excludes it, so NBGV can't compute a
   version inside the Dockerfile. Either copy `.git` into the build context, or (cleaner) compute the
   version in CI and pass it as a build-arg → `-p:Version=...` (the snippet above). The build-arg route
   is the recommended production pattern and needs no NBGV.

If you still want NBGV's git-height versioning for local/dev builds, add:

```jsonc
// version.json (repo root)
{
  "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/main/src/NerdBank.GitVersioning/version.schema.json",
  "version": "0.1",
  "publicReleaseRefSpec": ["^refs/heads/main$"]
}
```

```xml
<!-- Directory.Packages.props --> <PackageVersion Include="Nerdbank.GitVersioning" Version="3.10.85" />
<!-- Directory.Build.props -->    <PackageReference Include="Nerdbank.GitVersioning" PrivateAssets="all" />
```

…then remove `VersionPrefix` (NBGV owns the version) and fix the two clone issues above.

---

## Quick checklist

- [ ] Set up **commit signing** (verified locally)
- [ ] Enable Dependency graph, secret scanning + push protection, private vuln reporting
- [ ] Repo: squash-only, auto-delete head branches
- [ ] Import `main-branch.json` ruleset (`evaluate` → confirm → `active`)
- [ ] Create the issue/PR labels; confirm the labeler runs
- [ ] Confirm `pr-title-lint` and `release-please` run on the next PR / push
- [ ] (Later) aggregator CI gate to require Backend/Frontend safely; NBGV or build-arg version stamping
