# Wiki Changelog Setup Guide

## Overview

Changelogs are published to the GitHub Wiki under the `Changelog/` directory. Each release creates ONE combined changelog page for all components: `Changelog/v<version>.md`.

## Wiki Structure

```
Wiki/
├── Home.md
└── Changelog/
    ├── index.md              # Master changelog index table
    ├── v1.0.0.md             # Combined changelog per release
    ├── v1.1.0.md
    ├── v2.0.0.md
    └── ...
```

## Initial Setup

### 1. Ensure Wiki is Enabled for the Repository

Go to repository **Settings** → **General** and ensure the Wiki feature is enabled.

### 2. Create the Initial Wiki Structure

Run the following commands to initialize the wiki repository:

```bash
# Clone the wiki repository
gh repo clone {owner}/{repo}.wiki Warehouse.wiki
# or:
# git clone https://github.com/mkasperczyk90/Warehouse.wiki.git Warehouse.wiki

# Create the Changelog directory
mkdir -p Warehouse.wiki/Changelog

# Create the changelog index
cat > Warehouse.wiki/Changelog/index.md << 'EOF'
---
title: "Changelog"
status: active
---

# Changelog

Complete release history for the Warehouse project.

## Recent Releases

| Version | Date |
|---------|------|
EOF

# Create the Home wiki page if it doesn't exist
cat > Warehouse.wiki/Home.md << 'EOF'
---
title: "Warehouse Wiki Home"
status: active
---

# Warehouse Project

Welcome to the Warehouse project wiki.

## Quick Links

- [Changelog Index](Changelog/index.md)
EOF

# Commit and push
cd Warehouse.wiki
git add -A
git commit -m "ci: initialize wiki changelog structure"
git push origin main
```

### 3. Ensure the GitHub Actions Workflow is Present

The `.github/workflows/wiki-changelog.yml` file should be present in the main repository. This workflow triggers on every published release and automatically updates the wiki.

## How It Works

1. **release-please** creates a GitHub Release with changelog content in the release body
2. The **Wiki Changelog** workflow (`wiki-changelog.yml`) triggers on `release.published`
3. The workflow:
   - Extracts the version number from the release tag
   - Generates a combined changelog page: `Changelog/v<version>.md`
   - Appends the full release body (all component changelogs combined)
   - Updates the `Changelog/index.md` table of contents
   - Commits and pushes to the `.wiki` repository

## Tag Format

The workflow works with any semver tag format:

| Tag Format | Wiki Page |
|------------|-----------|
| `v1.0.0` | `Changelog/v1.0.0.md` |
| `admin-v1.0.0` | `Changelog/v1.0.0.md` |

## Manual Wiki Updates

You can always update the wiki directly by:

1. Cloning the wiki repo: `git clone https://github.com/{owner}/{repo}.wiki.git`
2. Making changes to `.md` files in the `Changelog/` directory
3. Committing and pushing: `git commit -m "..." && git push`

## Required Permissions

The GitHub Actions workflow needs:
- **Contents**: read
- **Pages**: write (for wiki updates)
- **ID Token**: write (for Pages deployment if using GitHub Pages wiki)

The workflow uses the default `GITHUB_TOKEN` which has these permissions when the wiki repo is linked to the main repo.