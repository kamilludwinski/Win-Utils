#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found in PATH. Install the .NET SDK and ensure it is on PATH (e.g. reopen the terminal after installing)." >&2
  exit 127
fi

if [[ "${1:-}" == "test" ]]; then
  shift
  exec dotnet test "$ROOT/tests/WinUtil.Tests/WinUtil.Tests.csproj" "$@"
fi

exec dotnet run --project "$ROOT/src/WinUtil/WinUtil.csproj" "$@"
