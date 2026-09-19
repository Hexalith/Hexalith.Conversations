#!/usr/bin/env python3
"""Verify the immutable NuGet and GitHub results of a Conversations release."""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from collections.abc import Callable

from release_package_contract import load_manifest


JsonRequest = Callable[[str, str | None], object]


def request_json(url: str, token: str | None) -> object:
    """Fetch one JSON document from an official endpoint."""

    headers = {"Accept": "application/vnd.github+json", "User-Agent": "hexalith-release-verifier"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
        headers["X-GitHub-Api-Version"] = "2022-11-28"
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def resolve_tag_commit(repository: str, version: str, token: str, fetch: JsonRequest = request_json) -> str:
    """Resolve a lightweight or annotated release tag to its commit SHA."""

    encoded_tag = urllib.parse.quote(f"v{version}", safe="")
    payload = fetch(f"https://api.github.com/repos/{repository}/git/ref/tags/{encoded_tag}", token)
    if not isinstance(payload, dict) or not isinstance(payload.get("object"), dict):
        raise ValueError("GitHub returned an invalid release tag document")
    target = payload["object"]
    for _ in range(5):
        object_type = target.get("type")
        sha = target.get("sha")
        if object_type == "commit" and isinstance(sha, str):
            return sha
        if object_type != "tag" or not isinstance(sha, str):
            break
        tag_payload = fetch(f"https://api.github.com/repos/{repository}/git/tags/{sha}", token)
        if not isinstance(tag_payload, dict) or not isinstance(tag_payload.get("object"), dict):
            break
        target = tag_payload["object"]
    raise ValueError("GitHub release tag does not resolve to a commit")


def missing_nuget_packages(
    package_ids: list[str],
    version: str,
    fetch: JsonRequest = request_json,
) -> list[str]:
    """Return package IDs whose exact version is not visible on NuGet.org."""

    missing: list[str] = []
    for package_id in package_ids:
        url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
        try:
            payload = fetch(url, None)
        except urllib.error.HTTPError as error:
            if error.code == 404:
                error.close()
                missing.append(package_id)
                continue
            raise
        versions = payload.get("versions") if isinstance(payload, dict) else None
        if not isinstance(versions, list) or version not in versions:
            missing.append(package_id)
    return missing


def verify_release_assets(
    repository: str,
    version: str,
    expected_assets: set[str],
    token: str,
    fetch: JsonRequest = request_json,
) -> None:
    """Require the GitHub release to contain exactly the sealed package pairs."""

    encoded_tag = urllib.parse.quote(f"v{version}", safe="")
    payload = fetch(f"https://api.github.com/repos/{repository}/releases/tags/{encoded_tag}", token)
    if not isinstance(payload, dict) or payload.get("tag_name") != f"v{version}":
        raise ValueError("GitHub release tag identity is missing or incorrect")
    assets = payload.get("assets")
    if not isinstance(assets, list):
        raise ValueError("GitHub release asset inventory is missing")
    actual_assets = {asset.get("name") for asset in assets if isinstance(asset, dict)}
    if actual_assets != expected_assets:
        missing = sorted(expected_assets - actual_assets)
        unexpected = sorted(actual_assets - expected_assets)
        raise ValueError(f"GitHub release asset boundary drifted; missing={missing}, unexpected={unexpected}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("version")
    parser.add_argument("source_revision")
    parser.add_argument("--attempts", type=int, default=int(os.environ.get("HEXALITH_NUGET_VERIFY_ATTEMPTS", "30")))
    parser.add_argument("--interval-seconds", type=float, default=float(os.environ.get("HEXALITH_NUGET_VERIFY_INTERVAL_SECONDS", "10")))
    args = parser.parse_args()
    if len(args.source_revision) != 40 or any(character not in "0123456789abcdef" for character in args.source_revision):
        raise ValueError("source revision must be an exact lowercase commit SHA")
    if args.attempts < 1 or args.interval_seconds < 0:
        raise ValueError("verification retry bounds must be nonnegative and include at least one attempt")

    repository = os.environ.get("GITHUB_REPOSITORY", "")
    token = os.environ.get("GITHUB_TOKEN", "")
    if not repository or not token:
        raise ValueError("GITHUB_REPOSITORY and GITHUB_TOKEN are required")
    package_ids = [package.package_id for package in load_manifest()]
    missing = package_ids
    for attempt in range(1, args.attempts + 1):
        missing = missing_nuget_packages(package_ids, args.version)
        if not missing:
            break
        if attempt < args.attempts:
            time.sleep(args.interval_seconds)
    if missing:
        raise ValueError(
            "NuGet publication is incomplete; do not rerun Release before reconciliation: "
            + ", ".join(missing)
        )

    tag_commit = resolve_tag_commit(repository, args.version, token)
    if tag_commit != args.source_revision:
        raise ValueError(f"v{args.version} targets {tag_commit}, not {args.source_revision}")
    expected_assets = {
        filename
        for package_id in package_ids
        for filename in (f"{package_id}.{args.version}.nupkg", f"{package_id}.{args.version}.snupkg")
    }
    verify_release_assets(repository, args.version, expected_assets, token)
    print(f"Verified {len(package_ids)} NuGet packages and {len(expected_assets)} GitHub assets for v{args.version}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - command-line gate reports a concise failure.
        print(f"Published release verification failed: {error}", file=sys.stderr)
        raise SystemExit(1)
