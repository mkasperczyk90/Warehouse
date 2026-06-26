#requires -Version 5.1
<#
.SYNOPSIS
    Build the Keycloak badge-authenticator jar, then run the Aspire AppHost.

.DESCRIPTION
    The AppHost bind-mounts the SPI jar into the Keycloak container, so the jar MUST exist before the host
    starts (Docker fails a bind mount of a missing file). This script enforces that order: it builds the jar
    with Maven, verifies it, then launches the AppHost (Keycloak + Postgres + RabbitMQ + services + gateway).

.PARAMETER SkipJar
    Reuse an already-built jar (skip the Maven build).

.PARAMETER JarOnly
    Build the jar and stop (don't start the AppHost).

.EXAMPLE
    ./scripts/run-local.ps1
.EXAMPLE
    ./scripts/run-local.ps1 -SkipJar
#>
[CmdletBinding()]
param(
    [switch]$SkipJar,
    [switch]$JarOnly
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$SpiDir   = Join-Path $RepoRoot 'src\Identity\keycloak-badge-authenticator'
$Jar      = Join-Path $SpiDir 'target\badge-authenticator.jar'
$AppHost  = Join-Path $RepoRoot 'src\AppHost\Warehouse.AppHost'

function Fail([string]$Message) {
    Write-Host "ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Assert-Tool([string]$Name, [string]$Hint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Fail "Required tool '$Name' was not found on PATH. $Hint"
    }
}

if (-not $SkipJar) {
    Assert-Tool 'mvn' 'Install Maven and a JDK 17+ (https://maven.apache.org).'
    Write-Host '==> Building the Keycloak badge-authenticator jar...' -ForegroundColor Cyan
    Push-Location $SpiDir
    try {
        mvn -q clean package
        if ($LASTEXITCODE -ne 0) { Pop-Location; Fail "Maven build failed (exit code $LASTEXITCODE)." }
    }
    finally {
        if ((Get-Location).Path -eq $SpiDir) { Pop-Location }
    }
}

if (-not (Test-Path $Jar)) {
    Fail "Provider jar not found at '$Jar'. Build it first (run without -SkipJar)."
}
Write-Host "==> Jar ready: $Jar" -ForegroundColor Green

if ($JarOnly) { return }

Assert-Tool 'dotnet' 'Install the .NET SDK (global.json pins 10.0.x).'
Assert-Tool 'docker' 'Docker is required (Keycloak, Postgres and RabbitMQ run as containers).'

docker info | Out-Null
if ($LASTEXITCODE -ne 0) {
    Fail 'Docker is installed but the daemon is not responding. Start Docker Desktop and retry.'
}

Write-Host '==> Starting the Aspire AppHost (Keycloak, Postgres, RabbitMQ, services, gateway)...' -ForegroundColor Cyan
dotnet run --project $AppHost
exit $LASTEXITCODE
