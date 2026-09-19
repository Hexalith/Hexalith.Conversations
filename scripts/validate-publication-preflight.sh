#!/usr/bin/env bash
set -euo pipefail

version="${1:-}"
phase="${2:-}"
reserved_version="${HEXALITH_RELEASE_RESERVED_VERSION:-}"
authority_issue_url="${HEXALITH_RELEASE_AUTHORITY_ISSUE_URL:-}"
authority_owner="${HEXALITH_RELEASE_AUTHORITY_OWNER:-}"
container_projects="${HEXALITH_CONTAINER_PROJECTS:-}"

fail() {
  echo "[publication-preflight] $1" >&2
  exit 1
}

[ "$version" = "1.0.0" ] || fail "The authorized first release version is exactly 1.0.0."
case "$phase" in verify|publish) ;; *) fail "Publication phase must be verify or publish." ;; esac
[ "${HEXALITH_BUILDS_EXECUTION_SHA:-}" = "b93e9889e9e7b67036837015b4b2b115e326c4da" ] ||
  fail "The reusable release workflow must execute the reviewed Builds commit."
[ "${HEXALITH_RELEASE_SOURCE_BRANCH:-}" = "main" ] || fail "Release source branch must be main."
[ "${HEXALITH_RELEASE_SOURCE_CI_WORKFLOW:-}" = "ci.yml" ] || fail "Release source CI must be ci.yml."
[ "${HEXALITH_RELEASE_PACKAGE_MANIFEST:-}" = "tools/release-packages.json" ] ||
  fail "Release package manifest must be tools/release-packages.json."
[ "${HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT:-}" = "4" ] || fail "Expected package count must be four."
[ "${HEXALITH_RELEASE_ENVIRONMENT:-}" = "production" ] || fail "Release environment must be production."
[ "${HEXALITH_RELEASE_REQUIRE_AUTHORITY:-}" = "false" ] ||
  fail "This authorized bypass requires publication authority to be exactly false."
[ -z "${reserved_version//[[:space:]]/}" ] || fail "Reserved version must be empty."
[ -z "${authority_issue_url//[[:space:]]/}" ] || fail "Authority issue URL must be empty."
[ -z "${authority_owner//[[:space:]]/}" ] || fail "Authority owner must be empty."
[ -z "${container_projects//[[:space:]]/}" ] || fail "Container publication is forbidden."

if [ "$phase" = "verify" ]; then
  python3 scripts/verify-release-source.py \
    --publish-enabled true \
    --repository "${GITHUB_REPOSITORY:-}" \
    --dispatch-ref "${GITHUB_REF:-}" \
    --dispatch-sha "${GITHUB_SHA:-}" \
    --workflow ci.yml \
    --destination-state absent
else
  python3 scripts/verify-release-state.py publishable "$version" \
    --repository "${GITHUB_REPOSITORY:-}" \
    --source-sha "${GITHUB_SHA:-}"
fi
