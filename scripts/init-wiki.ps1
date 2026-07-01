# Script to initialize the GitHub Wiki changelog structure
# Run this from the repo root to push initial wiki content

$ErrorActionPreference = "Stop"

$repo = "Warehouse"
$owner = "mkasperczyk90"
$wikiDir = "Warehouse.wiki"
$wikiUrl = "https://github.com/${owner}/${repo}.wiki.git"

Write-Host "=== Warehouse Wiki Initializer ===" -ForegroundColor Cyan
Write-Host ""

# Check if gh CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: GitHub CLI (gh) is not installed." -ForegroundColor Red
    Write-Host "Install it with: winget install GitHub.gh" -ForegroundColor Yellow
    exit 1
}

# Check if authenticated
if (-not (gh auth status 2>&1 | Select-String "Authentication status: authenticated")) {
    Write-Host "ERROR: Not authenticated with GitHub CLI." -ForegroundColor Red
    Write-Host "Run: gh auth login" -ForegroundColor Yellow
    exit 1
}

# Check if wiki repo already exists
$wikiExists = $false
try {
    $wikiExists = gh repo view "${owner}/${repo}.wiki" --json name > $null 2>&1
} catch {
    $wikiExists = $false
}

if (-not $wikiExists) {
    Write-Host "Creating wiki repository..." -ForegroundColor Yellow
    gh repo create "${owner}/${repo}.wiki" --private --description "Wiki for ${repo}" --source "./$wikiDir" --push 2>&1
    if ($lastExitCode -ne 0) {
        Write-Host "WARNING: Could not create wiki repo. It may already exist." -ForegroundColor Yellow
    }
}

# Clone the wiki if local directory doesn't exist
if (-not (Test-Path $wikiDir)) {
    Write-Host "Cloning wiki repository..." -ForegroundColor Yellow
    gh repo clone "${owner}/${repo}.wiki" $wikiDir -- --bare 2>&1
    # For wikis, we need to clone normally (not bare) to make commits
    Remove-Item $wikiDir -Recurse -Force
    git clone "https://github.com/${owner}/${repo}.wiki.git" $wikiDir
}

# Create Changelog directory
Set-Location $wikiDir
New-Item -ItemType Directory -Path "Changelog" -Force | Out-Null

# Create changelog index
$indexContent = @'
---
title: "Changelog"
status: active
---

# Changelog

Complete release history for the Warehouse project.

## Recent Releases

| Version | Date |
|---------|------|
'@

Set-Content -Path "Changelog\index.md" -Value $indexContent -Encoding UTF8

# Create Home if not exists
if (-not (Test-Path "Home.md")) {
    $homeContent = @'
---
title: "Warehouse Wiki Home"
status: active
---

# Warehouse Project

Welcome to the Warehouse project wiki.

## Quick Links

- [Changelog Index](Changelog/index.md)
'@

    Set-Content -Path "Home.md" -Value $homeContent -Encoding UTF8
}

# Commit and push
git add -A
$status = git status --porcelain
if ($status) {
    git commit -m "ci: initialize wiki changelog structure"
    git push origin main
    Write-Host ""
    Write-Host "Wiki initialized successfully!" -ForegroundColor Green
    Write-Host "View at: https://github.com/${owner}/${repo}/wiki" -ForegroundColor Cyan
} else {
    Write-Host "No changes to commit. Wiki is already up to date." -ForegroundColor Yellow
}

Set-Location ..