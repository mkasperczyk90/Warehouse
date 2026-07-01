#!/usr/bin/env bash
#
# Run every test suite: backend (.NET, via the solution), the admin SPA's unit tests (Vitest),
# and Playwright e2e (admin + terminal). Mirrors what backend.yml / frontend.yml run in CI, plus
# e2e on top.
#
# The terminal app has no unit test script (see frontend.yml) — only typecheck/lint — so it's
# skipped. E2E needs the full app stack already running locally (Aspire AppHost) — use --no-e2e
# to skip it if the stack isn't up.
#
# Usage:
#   scripts/run-tests.sh                # everything: backend + admin unit + e2e
#   scripts/run-tests.sh --backend-only
#   scripts/run-tests.sh --frontend-only
#   scripts/run-tests.sh --no-e2e       # skip Playwright e2e
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

RUN_BACKEND=1
RUN_FRONTEND=1
RUN_E2E=1
for arg in "$@"; do
  case "$arg" in
    --backend-only) RUN_FRONTEND=0; RUN_E2E=0 ;;
    --frontend-only) RUN_BACKEND=0; RUN_E2E=0 ;;
    --no-e2e) RUN_E2E=0 ;;
    --e2e) RUN_E2E=1 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

need() {
  command -v "$1" >/dev/null 2>&1 || { echo "Required tool '$1' not found on PATH. $2" >&2; exit 1; }
}

FAILED=()

if [ "$RUN_BACKEND" -eq 1 ]; then
  need dotnet 'Install the .NET SDK (global.json pins 10.0.x).'
  echo '==> Backend: dotnet test Warehouse.slnx'
  if ! (cd "$REPO_ROOT" && dotnet test Warehouse.slnx); then
    FAILED+=('backend')
  fi
fi

if [ "$RUN_FRONTEND" -eq 1 ]; then
  need npm 'Install Node.js 20 (used by both SPAs).'
  ADMIN_DIR="$REPO_ROOT/src/web/admin"
  echo '==> Frontend (admin): npm run test:run'
  if [ ! -d "$ADMIN_DIR/node_modules" ]; then
    echo '    node_modules missing — running npm ci first...'
    (cd "$ADMIN_DIR" && npm ci)
  fi
  if ! (cd "$ADMIN_DIR" && npm run test:run); then
    FAILED+=('admin unit tests')
  fi
fi

if [ "$RUN_E2E" -eq 1 ]; then
  need npx 'Install Node.js 20 (npx ships with npm).'
  for dir in tests/e2e/admin tests/e2e/terminal; do
    E2E_DIR="$REPO_ROOT/$dir"
    echo "==> E2E ($dir): npm test  (requires the app stack already running)"
    if [ ! -d "$E2E_DIR/node_modules" ]; then
      echo '    node_modules missing — running npm ci first...'
      (cd "$E2E_DIR" && npm ci)
    fi
    if ! (cd "$E2E_DIR" && npm test); then
      FAILED+=("$dir")
    fi
  done
fi

echo
if [ ${#FAILED[@]} -eq 0 ]; then
  echo '==> All test suites passed.'
  exit 0
else
  echo "==> Failed suites: ${FAILED[*]}" >&2
  exit 1
fi
