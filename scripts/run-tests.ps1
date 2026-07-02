#requires -Version 5.1
<#
.SYNOPSIS
    Run every test suite: backend (.NET), the admin SPA's unit tests (Vitest), and Playwright e2e.

.DESCRIPTION
    Mirrors what backend.yml / frontend.yml run in CI, plus e2e on top.

    The terminal app has no unit test script (see frontend.yml) — only typecheck/lint — so it's
    skipped. E2E needs the full app stack already running locally (Aspire AppHost) — use -NoE2E
    to skip it if the stack isn't up.

.PARAMETER BackendOnly
    Only run the backend (.NET) test suite.

.PARAMETER FrontendOnly
    Only run the admin SPA's unit tests.

.PARAMETER NoE2E
    Skip Playwright e2e (admin + terminal).

.EXAMPLE
    ./scripts/run-tests.ps1
.EXAMPLE
    ./scripts/run-tests.ps1 -BackendOnly
.EXAMPLE
    ./scripts/run-tests.ps1 -NoE2E
#>
[CmdletBinding()]
param(
    [switch]$BackendOnly,
    [switch]$FrontendOnly,
    [switch]$NoE2E
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$RunBackend = -not $FrontendOnly
$RunFrontend = -not $BackendOnly
$E2E = -not ($NoE2E -or $BackendOnly -or $FrontendOnly)
$Failed = @()

function Assert-Tool([string]$Name, [string]$Hint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: Required tool '$Name' was not found on PATH. $Hint" -ForegroundColor Red
        exit 1
    }
}

if ($RunBackend) {
    Assert-Tool 'dotnet' 'Install the .NET SDK (global.json pins 10.0.x).'
    Write-Host '==> Backend: dotnet test Warehouse.slnx' -ForegroundColor Cyan
    Push-Location $RepoRoot
    try {
        dotnet test Warehouse.slnx
        if ($LASTEXITCODE -ne 0) { $Failed += 'backend' }
    }
    finally { Pop-Location }
}

if ($RunFrontend) {
    Assert-Tool 'npm' 'Install Node.js 20 (used by both SPAs).'
    $AdminDir = Join-Path $RepoRoot 'src\web\admin'
    Write-Host '==> Frontend (admin): npm run test:run' -ForegroundColor Cyan
    Push-Location $AdminDir
    try {
        if (-not (Test-Path (Join-Path $AdminDir 'node_modules'))) {
            Write-Host '    node_modules missing — running npm ci first...' -ForegroundColor Yellow
            npm ci
        }
        npm run test:run
        if ($LASTEXITCODE -ne 0) { $Failed += 'admin unit tests' }
    }
    finally { Pop-Location }
}

if ($E2E) {
    Assert-Tool 'npx' 'Install Node.js 20 (npx ships with npm).'
    foreach ($dir in @('tests\e2e\admin', 'tests\e2e\terminal')) {
        $E2EDir = Join-Path $RepoRoot $dir
        Write-Host "==> E2E ($dir): npm test  (requires the app stack already running)" -ForegroundColor Cyan
        Push-Location $E2EDir
        try {
            if (-not (Test-Path (Join-Path $E2EDir 'node_modules'))) {
                Write-Host '    node_modules missing — running npm ci first...' -ForegroundColor Yellow
                npm ci
            }
            npm test
            if ($LASTEXITCODE -ne 0) { $Failed += $dir }
        }
        finally { Pop-Location }
    }
}

Write-Host ''
if ($Failed.Count -eq 0) {
    Write-Host '==> All test suites passed.' -ForegroundColor Green
    exit 0
}
else {
    Write-Host "==> Failed suites: $($Failed -join ', ')" -ForegroundColor Red
    exit 1
}
