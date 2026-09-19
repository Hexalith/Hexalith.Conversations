#!/usr/bin/env python3
"""Shared fail-closed contract for Conversations release tooling."""

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "tools" / "release-packages.json"
REPOSITORY = "Hexalith/Hexalith.Conversations"
BUILDS_EXECUTION_SHA = "b93e9889e9e7b67036837015b4b2b115e326c4da"
FIRST_RELEASE_VERSION = "1.0.0"
SEMVER_PATTERN = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$"
)

EXACT_PACKAGES: tuple[tuple[str, str], ...] = (
    ("Hexalith.Conversations", "src/Hexalith.Conversations/Hexalith.Conversations.csproj"),
    (
        "Hexalith.Conversations.Contracts",
        "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj",
    ),
    (
        "Hexalith.Conversations.Client",
        "src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj",
    ),
    (
        "Hexalith.Conversations.Testing",
        "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj",
    ),
)
EXACT_PACKAGE_IDS = tuple(package_id for package_id, _ in EXACT_PACKAGES)


@dataclass(frozen=True)
class ReleasePackage:
    """One canonical package-manifest entry."""

    package_id: str
    project: str
    project_path: Path


def reject_duplicate_json_keys(pairs: list[tuple[str, object]]) -> dict[str, object]:
    """Build a JSON object while rejecting duplicate properties."""
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON property '{key}'")
        result[key] = value
    return result


def validate_semver(version: str) -> str:
    """Require SemVer without build metadata or padded numeric identifiers."""
    match = SEMVER_PATTERN.fullmatch(version)
    if match is None:
        raise ValueError(f"'{version}' is not a supported semantic version")
    prerelease = match.group(4)
    if prerelease:
        for identifier in prerelease.split("."):
            if identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"):
                raise ValueError(f"'{version}' contains a padded numeric prerelease identifier")
    return version


def load_manifest(
    manifest_path: Path = DEFAULT_MANIFEST,
    repository_root: Path = ROOT,
) -> tuple[ReleasePackage, ...]:
    """Load and prove the exact, immutable four-package inventory."""
    resolved_root = repository_root.resolve()
    resolved_manifest = manifest_path.resolve()
    if resolved_root not in resolved_manifest.parents:
        raise ValueError(f"release manifest must stay below repository root: {manifest_path}")

    try:
        with resolved_manifest.open("r", encoding="utf-8") as handle:
            document = json.load(handle, object_pairs_hook=reject_duplicate_json_keys)
    except (OSError, UnicodeError, ValueError) as exc:
        raise ValueError(f"release manifest is unusable: {exc}") from exc

    if not isinstance(document, dict) or set(document) != {"packages"}:
        raise ValueError("release manifest must contain only the 'packages' property")
    rows = document["packages"]
    if not isinstance(rows, list):
        raise ValueError("release manifest 'packages' must be an array")

    actual: list[tuple[str, str]] = []
    for index, row in enumerate(rows, start=1):
        if not isinstance(row, dict) or set(row) != {"id", "project"}:
            raise ValueError(f"package row #{index} must contain exactly 'id' and 'project'")
        package_id = row["id"]
        project = row["project"]
        if not isinstance(package_id, str) or not isinstance(project, str):
            raise ValueError(f"package row #{index} must contain string values")
        if package_id != package_id.strip() or project != project.strip():
            raise ValueError(f"package row #{index} must not contain surrounding whitespace")
        actual.append((package_id, project))

    if tuple(actual) != EXACT_PACKAGES:
        raise ValueError(
            "release inventory must equal the canonical four-package sequence; "
            f"expected {list(EXACT_PACKAGES)}, found {actual}"
        )

    packages: list[ReleasePackage] = []
    for package_id, project in actual:
        if project.startswith("-") or Path(project).is_absolute():
            raise ValueError(f"unsafe package project path: {project}")
        project_path = (resolved_root / project).resolve()
        if resolved_root not in project_path.parents:
            raise ValueError(f"package project escapes the repository: {project}")
        if project_path.suffix != ".csproj" or not project_path.is_file():
            raise ValueError(f"package project is not an existing .csproj: {project}")
        packages.append(ReleasePackage(package_id, project, project_path))

    return tuple(packages)


def expected_asset_names(version: str) -> frozenset[str]:
    """Return the exact package and symbol asset names for a release."""
    validate_semver(version)
    return frozenset(
        f"{package_id}.{version}.{extension}"
        for package_id in EXACT_PACKAGE_IDS
        for extension in ("nupkg", "snupkg")
    )
