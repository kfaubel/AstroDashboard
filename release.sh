#!/usr/bin/env bash

set -euo pipefail

usage() {
  echo "Usage: ./release.sh [patch|minor|major]"
}

if [[ $# -ne 1 ]]; then
  usage
  exit 1
fi

BUMP_TYPE="$1"
case "$BUMP_TYPE" in
  patch|minor|major)
    ;;
  *)
    echo "Error: invalid bump type '$BUMP_TYPE'"
    usage
    exit 1
    ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if command -v pwsh >/dev/null 2>&1; then
  echo "Starting release with bump type: $BUMP_TYPE (via pwsh)"
  pwsh -NoProfile -File "$SCRIPT_DIR/release.ps1" -BumpType "$BUMP_TYPE"
else
  echo "Starting release with bump type: $BUMP_TYPE (via powershell)"
  powershell -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/release.ps1" -BumpType "$BUMP_TYPE"
fi
