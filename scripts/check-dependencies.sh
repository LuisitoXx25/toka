#!/usr/bin/env bash
# Fails when a dependency has a known high or critical vulnerability.
# Usage: ./scripts/check-dependencies.sh [nuget|npm]   (no argument: both)
set -euo pipefail
cd "$(dirname "$0")/.."

check_nuget() {
  echo "== NuGet"
  # Forced to English so the severity words below match regardless of the machine's locale.
  report=$(DOTNET_CLI_UI_LANGUAGE=en dotnet list package --vulnerable --include-transitive 2>&1)
  echo "$report"
  if grep -qE '\b(High|Critical)\b' <<<"$report"; then
    echo "Hay paquetes NuGet con vulnerabilidades altas o críticas." >&2
    return 1
  fi
}

check_npm() {
  echo "== npm"
  (cd web && npm audit --audit-level=high)
}

case "${1:-all}" in
  nuget) check_nuget ;;
  npm) check_npm ;;
  all) check_nuget && check_npm ;;
  *) echo "Uso: $0 [nuget|npm]" >&2; exit 2 ;;
esac
