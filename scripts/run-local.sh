#!/usr/bin/env bash
#
# Build the Keycloak badge-authenticator jar, then run the Aspire AppHost.
#
# The AppHost bind-mounts the SPI jar into the Keycloak container, so the jar MUST exist before the host
# starts (Docker fails a bind mount of a missing file). This script enforces that order: build the jar with
# Maven, verify it, then launch the AppHost (Keycloak + Postgres + RabbitMQ + services + gateway).
#
# Usage:
#   scripts/run-local.sh              # build jar, then run the AppHost
#   scripts/run-local.sh --skip-jar   # reuse an already-built jar
#   scripts/run-local.sh --jar-only   # build the jar and stop
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
SPI_DIR="$REPO_ROOT/src/Identity/keycloak-badge-authenticator"
JAR="$SPI_DIR/target/badge-authenticator.jar"
APPHOST="$REPO_ROOT/src/AppHost/Warehouse.AppHost"

SKIP_JAR=0
JAR_ONLY=0
for arg in "$@"; do
  case "$arg" in
    --skip-jar) SKIP_JAR=1 ;;
    --jar-only) JAR_ONLY=1 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

need() {
  command -v "$1" >/dev/null 2>&1 || { echo "Required tool '$1' not found on PATH. $2" >&2; exit 1; }
}

if [ "$SKIP_JAR" -eq 0 ]; then
  need mvn 'Install Maven and a JDK 17+ (https://maven.apache.org).'
  echo '==> Building the Keycloak badge-authenticator jar...'
  (cd "$SPI_DIR" && mvn -q clean package)
fi

if [ ! -f "$JAR" ]; then
  echo "Provider jar not found at '$JAR'. Build it first (run without --skip-jar)." >&2
  exit 1
fi
echo "==> Jar ready: $JAR"

[ "$JAR_ONLY" -eq 1 ] && exit 0

need dotnet 'Install the .NET SDK (global.json pins 10.0.x).'
need docker 'Docker is required (Keycloak, Postgres and RabbitMQ run as containers).'
docker info >/dev/null 2>&1 || { echo 'Docker daemon not responding. Start Docker and retry.' >&2; exit 1; }

echo '==> Starting the Aspire AppHost (Keycloak, Postgres, RabbitMQ, services, gateway)...'
exec dotnet run --project "$APPHOST"
