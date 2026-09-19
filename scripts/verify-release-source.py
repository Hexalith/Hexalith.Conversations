#!/usr/bin/env python3
"""Prove a dispatch selects the live main tip with exact-source successful CI."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Callable

from release_contract import FIRST_RELEASE_VERSION, REPOSITORY, load_manifest
from release_state import verify_absent


SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
GetJson = Callable[[str], object]


def default_get_json(url: str) -> object:
    """Read one GitHub API document with the workflow token."""
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "Hexalith.Conversations-release-source-verifier",
    }
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            if response.status != 200:
                raise ValueError(f"GitHub API returned HTTP status {response.status}")
            return json.loads(response.read())
    except urllib.error.HTTPError as exc:
        raise ValueError(f"GitHub API returned HTTP status {exc.code}") from exc
    except (urllib.error.URLError, UnicodeError, ValueError) as exc:
        raise ValueError(f"GitHub API response was unavailable or invalid: {exc}") from exc


def verify_source(
    publish_enabled: str,
    repository: str,
    dispatch_ref: str,
    dispatch_sha: str,
    workflow: str,
    get_json: GetJson = default_get_json,
    verify_destinations: Callable[[str, str], None] = verify_absent,
) -> bool:
    """Return false for an exact frozen posture, otherwise prove release source."""
    load_manifest()
    if publish_enabled != "true":
        return False
    if repository != REPOSITORY:
        raise ValueError(f"release repository must be exactly {REPOSITORY}")
    if dispatch_ref != "refs/heads/main":
        raise ValueError("release must be dispatched from refs/heads/main")
    if SHA_PATTERN.fullmatch(dispatch_sha) is None:
        raise ValueError("dispatch SHA must be an exact lowercase 40-character commit")
    if workflow != "ci.yml":
        raise ValueError("source CI workflow must be exactly ci.yml")

    live = get_json(f"https://api.github.com/repos/{repository}/git/ref/heads/main")
    if not isinstance(live, dict) or not isinstance(live.get("object"), dict):
        raise ValueError("live main response is malformed")
    live_sha = live["object"].get("sha")
    if live_sha != dispatch_sha:
        raise ValueError("STALE_SOURCE: dispatched source is not the live main tip")

    query = urllib.parse.urlencode(
        {
            "branch": "main",
            "event": "push",
            "head_sha": dispatch_sha,
            "per_page": "100",
        }
    )
    runs = get_json(
        f"https://api.github.com/repos/{repository}/actions/workflows/{workflow}/runs?{query}"
    )
    workflow_runs = runs.get("workflow_runs") if isinstance(runs, dict) else None
    if not isinstance(workflow_runs, list):
        raise ValueError("exact-source CI response is malformed")
    exact_runs = [
        run
        for run in workflow_runs
        if isinstance(run, dict)
        and run.get("head_sha") == dispatch_sha
        and run.get("head_branch") == "main"
        and run.get("event") == "push"
    ]
    if not exact_runs:
        raise ValueError("EXACT_CI_ABSENT: no push CI run exists for the current main SHA")
    if any(
        not isinstance(run.get(field), int) or isinstance(run.get(field), bool)
        for run in exact_runs
        for field in ("run_number", "run_attempt", "id")
    ):
        raise ValueError("exact-source CI response has malformed run ordering metadata")
    latest = max(
        exact_runs,
        key=lambda run: (run["run_number"], run["run_attempt"], run["id"]),
    )
    if latest.get("status") != "completed" or latest.get("conclusion") != "success":
        raise ValueError(
            "EXACT_CI_ABSENT: newest push CI run/attempt for the current main SHA is not successful"
        )
    verify_destinations(FIRST_RELEASE_VERSION, repository)
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify release source and first-release destinations.")
    parser.add_argument("--publish-enabled", required=True)
    parser.add_argument("--repository", default=os.environ.get("GITHUB_REPOSITORY", ""))
    parser.add_argument("--dispatch-ref", default=os.environ.get("GITHUB_REF", ""))
    parser.add_argument("--dispatch-sha", default=os.environ.get("GITHUB_SHA", ""))
    parser.add_argument("--workflow", default="ci.yml")
    parser.add_argument("--destination-state", choices=("absent",), default="absent")
    parser.add_argument("--github-output", type=Path)
    args = parser.parse_args()
    enabled = verify_source(
        args.publish_enabled,
        args.repository,
        args.dispatch_ref,
        args.dispatch_sha,
        args.workflow,
    )
    if args.github_output is not None:
        with args.github_output.open("a", encoding="utf-8", newline="\n") as handle:
            handle.write(f"publish-enabled={'true' if enabled else 'false'}\n")
    if enabled:
        print("Verified live main, exact-source successful CI, and absent 1.0.0 destinations.")
    else:
        print("Release publication is frozen; publication validation is not applicable.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line verification reports one concise failure.
        print(f"Release-source verification failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
