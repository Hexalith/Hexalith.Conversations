#!/usr/bin/env bash
set -euo pipefail

if [ -z "${NUGET_API_KEY:-}" ]; then
  echo "NUGET_API_KEY is required for NuGet publication." >&2
  exit 1
fi
