#!/usr/bin/env python3
"""Publish one previously validated Conversations package set to NuGet.org."""

from __future__ import annotations

import argparse
import os
import pathlib
import subprocess
import sys
from collections.abc import Callable

from release_package_contract import validate_packages


Runner = Callable[..., subprocess.CompletedProcess[bytes]]


def publish_packages(
    package_directory: pathlib.Path,
    version: str,
    source_revision: str,
    api_key: str,
    runner: Runner = subprocess.run,
) -> None:
    """Validate the complete set, then stop at the first failed immutable write."""

    manifest, validated_version = validate_packages(package_directory, version, source_revision)
    if validated_version != version:
        raise ValueError("validated package version drifted before publication")
    for package in manifest:
        archive = package_directory / f"{package.package_id}.{version}.nupkg"
        runner(
            [
                "dotnet",
                "nuget",
                "push",
                str(archive),
                "--source",
                "https://api.nuget.org/v3/index.json",
                "--api-key",
                api_key,
                "--timeout",
                "300",
            ],
            check=True,
        )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=pathlib.Path)
    parser.add_argument("version")
    parser.add_argument("source_revision")
    args = parser.parse_args()
    api_key = os.environ.get("NUGET_API_KEY", "")
    if not api_key:
        raise ValueError("NUGET_API_KEY is required")
    publish_packages(args.package_directory.resolve(), args.version, args.source_revision, api_key)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        print(
            "Package publication stopped after a failed immutable write; do not rerun before reconciling NuGet.org state.",
            file=sys.stderr,
        )
        raise SystemExit(error.returncode)
    except Exception as error:  # noqa: BLE001 - command-line gate reports a concise failure.
        print(f"Package publication failed: {error}", file=sys.stderr)
        raise SystemExit(1)
