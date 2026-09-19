#!/usr/bin/env bash
set -euo pipefail

nuget_api_key="${NUGET_API_KEY:-}"
container_projects="${HEXALITH_CONTAINER_PROJECTS:-}"

if [ -z "${nuget_api_key//[[:space:]]/}" ]; then
  echo "[release-secrets] NUGET_API_KEY is required before publishing NuGet packages." >&2
  exit 1
fi

if [ -n "${container_projects//[[:space:]]/}" ] || [ -e ./.hexalith/release/publish-containers.sh ]; then
  echo "[release-secrets] Conversations is a NuGet-only release and must not publish containers." >&2
  exit 1
fi
