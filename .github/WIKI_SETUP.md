# Wiki Changelog Setup Guide

## Overview

Changelogs are published to the GitHub Wiki under `Changelog/`. Each **component release**
creates its own changelog page: `Changelog/<component>-v<version>.md`.

release-please here uses `separate-pull-requests: true` (see `release-please-config.json`), so
`admin`, `terminal`, `gateway`, `warehouse`, `logistics`, and `masterdata` each release
independently and routinely land on the same version number on the same day. The page — and the
index row — are keyed by **component + version**, not version alone, so same-day releases from
different components never collide.

## Wiki Structure

```
Wiki/
├── Home.md
└── Changelog/
    ├── index.md                # Master index table: Component | Version | Date
    ├── admin-v1.0.0.md
    ├── gateway-v1.0.0.md
    ├── warehouse-v1.1.0.md
    └── ...
```

## Initial Setup

The Wiki feature is already enabled for this repository. To seed the initial structure
(`Home.md` + an empty `Changelog/index.md`) run:

```powershell
./scripts/init-wiki.ps1
```

or by hand:

```bash
git clone https://github.com/mkasperczyk90/Warehouse.wiki.git Warehouse.wiki
cd Warehouse.wiki
mkdir -p Changelog
cat > Changelog/index.md << 'EOF'
---
title: "Changelog"
status: active
---

# Changelog

Complete release history for the Warehouse project, one row per component release.

| Component | Version | Date |
|-----------|---------|------|
EOF
git add -A
git commit -m "docs: initialize wiki changelog structure"
git push origin master
```

The `wiki-changelog.yml` workflow creates `Changelog/index.md` itself on the first release if
it's missing, so this step is optional — it just gives you a populated `Home.md` up front.

## How It Works

1. **release-please** creates a GitHub Release per component, tagged `<component>-v<version>`.
2. The **Wiki Changelog** workflow (`.github/workflows/wiki-changelog.yml`) triggers on
   `release.published`.
3. The workflow:
   - Parses the tag into `component` + `version` (falls back to an unscoped `v<version>` page
     if the tag has no component prefix).
   - Writes `Changelog/<component>-v<version>.md` with the release body.
   - Adds a row to `Changelog/index.md` — skipped if that exact slug is already indexed (safe to
     re-run).
   - Commits and pushes to the `.wiki` repository.

## Tag Format

| Tag | Wiki Page |
|-----|-----------|
| `admin-v1.0.0` | `Changelog/admin-v1.0.0.md` |
| `gateway-v0.2.0` | `Changelog/gateway-v0.2.0.md` |
| `v1.0.0` (no component) | `Changelog/v1.0.0.md` |

## Manual Wiki Updates

1. Clone: `git clone https://github.com/mkasperczyk90/Warehouse.wiki.git`
2. Edit `.md` files under `Changelog/`.
3. `git commit` and `git push`.

## Required Permissions

The workflow needs `contents: write` on the default `GITHUB_TOKEN` — that's what lets Actions
push to the wiki (the wiki has no separate permission scope; it rides on `contents`).
