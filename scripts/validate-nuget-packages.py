#!/usr/bin/env python3
"""Validate exact package, symbol, dependency, and archive boundaries."""

from __future__ import annotations

import argparse
import json
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree

from release_contract import EXACT_PACKAGE_IDS, ROOT, load_manifest, validate_semver


@dataclass(frozen=True)
class PackageMetadata:
    """Release-relevant metadata extracted from one NuGet package."""

    package_id: str
    version: str
    readme: str
    has_license: bool
    dependencies: dict[str, str]


def _load_restore_boundary(package_id: str, project_path: Path) -> dict[str, str]:
    assets_path = project_path.parent / "obj" / "project.assets.json"
    if not assets_path.is_file():
        raise ValueError(f"restore assets are missing for {package_id}: {assets_path}")
    try:
        assets = json.loads(assets_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, ValueError) as exc:
        raise ValueError(f"restore assets are unusable for {package_id}: {exc}") from exc

    restore = assets.get("project", {}).get("restore", {})
    restored_path = restore.get("projectPath")
    if not isinstance(restored_path, str) or Path(restored_path).resolve() != project_path.resolve():
        raise ValueError(f"restore assets do not belong to {project_path}")
    if restore.get("projectName") != package_id or restore.get("projectStyle") != "PackageReference":
        raise ValueError(f"restore identity is invalid for {package_id}")
    audit = restore.get("restoreAuditProperties", {})
    if audit.get("enableAudit") != "true" or audit.get("auditMode") != "all":
        raise ValueError(f"NuGet audit must remain enabled in all mode for {package_id}")

    groups = assets.get("projectFileDependencyGroups")
    if not isinstance(groups, dict) or set(groups) != {"net10.0"} or not isinstance(groups["net10.0"], list):
        raise ValueError(f"restore dependency groups are invalid for {package_id}")
    dependencies: dict[str, str] = {}
    for declaration in groups["net10.0"]:
        if not isinstance(declaration, str) or " >= " not in declaration:
            raise ValueError(f"invalid restore dependency declaration for {package_id}: {declaration!r}")
        dependency_id, version = declaration.split(" >= ", maxsplit=1)
        if dependency_id in dependencies:
            raise ValueError(f"duplicate restore dependency for {package_id}: {dependency_id}")
        dependencies[dependency_id] = version

    libraries = assets.get("libraries")
    if not isinstance(libraries, dict):
        raise ValueError(f"restore libraries are invalid for {package_id}")
    for library_identity, library in libraries.items():
        if not isinstance(library_identity, str) or not isinstance(library, dict):
            raise ValueError(f"restore library evidence is malformed for {package_id}")
        library_id = library_identity.split("/", maxsplit=1)[0]
        if library_id.startswith("Hexalith.") and library_id not in EXACT_PACKAGE_IDS:
            if library.get("type") != "package":
                raise ValueError(
                    f"external Hexalith dependency is not package-resolved for {package_id}: {library_id}"
                )
    return dependencies


def _metadata(package_path: Path) -> PackageMetadata:
    try:
        with zipfile.ZipFile(package_path) as archive:
            names = archive.namelist()
            nuspec_names = [name for name in names if name.endswith(".nuspec")]
            if len(nuspec_names) != 1:
                raise ValueError(f"{package_path.name}: expected exactly one .nuspec")
            root = ElementTree.fromstring(archive.read(nuspec_names[0]))
            namespace = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

            def text(name: str) -> str:
                element = (
                    root.find(f".//n:metadata/n:{name}", namespace)
                    if namespace
                    else root.find(f".//metadata/{name}")
                )
                return element.text.strip() if element is not None and element.text else ""

            dependency_path = ".//n:metadata/n:dependencies//n:dependency" if namespace else ".//metadata/dependencies//dependency"
            dependencies: dict[str, str] = {}
            for dependency in root.findall(dependency_path, namespace):
                dependency_id = dependency.attrib.get("id", "").strip()
                version = dependency.attrib.get("version", "").strip()
                if not dependency_id or not version or dependency_id in dependencies:
                    raise ValueError(f"{package_path.name}: malformed or duplicate dependency")
                dependencies[dependency_id] = version

            package_id = text("id")
            version = text("version")
            readme = text("readme")
            if not package_id or not version or not readme:
                raise ValueError(f"{package_path.name}: id, version, and readme are required")
            if readme not in names:
                raise ValueError(f"{package_path.name}: declared readme '{readme}' is absent")
            if f"lib/net10.0/{package_id}.dll" not in names:
                raise ValueError(f"{package_path.name}: canonical package assembly is absent")
            foreign_assemblies = sorted(
                name
                for name in names
                if name.casefold().endswith(".dll")
                and name.startswith(("lib/", "ref/", "runtimes/"))
                and name.rsplit("/", maxsplit=1)[-1] != f"{package_id}.dll"
            )
            if foreign_assemblies:
                raise ValueError(f"{package_path.name}: foreign assemblies are embedded: {foreign_assemblies}")
            return PackageMetadata(
                package_id,
                version,
                readme,
                bool(text("license") or text("licenseFile")),
                dependencies,
            )
    except zipfile.BadZipFile as exc:
        raise ValueError(f"{package_path.name}: invalid NuGet archive") from exc


def _candidate_dependencies(boundary: dict[str, str], version: str) -> dict[str, str]:
    """Bind in-repository project dependencies to the candidate version."""
    return {
        dependency_id: version if dependency_id in EXACT_PACKAGE_IDS else dependency_version
        for dependency_id, dependency_version in boundary.items()
    }


def _validate_symbol_archive(symbol_path: Path, package_id: str) -> None:
    """Require one canonical Portable PDB and no unrelated symbol payload."""
    try:
        with zipfile.ZipFile(symbol_path) as archive:
            canonical_pdb = f"lib/net10.0/{package_id}.pdb"
            pdb_names = sorted(name for name in archive.namelist() if name.casefold().endswith(".pdb"))
            if pdb_names != [canonical_pdb]:
                raise ValueError(
                    f"{symbol_path.name}: expected exactly canonical PDB {canonical_pdb}; found {pdb_names}"
                )
            if not archive.read(canonical_pdb).startswith(b"BSJB"):
                raise ValueError(f"{symbol_path.name}: canonical PDB is not a Portable PDB")
    except zipfile.BadZipFile as exc:
        raise ValueError(f"{symbol_path.name}: invalid symbol archive") from exc


def validate_packages(package_directory: Path, expected_version: str) -> str:
    """Validate a complete package directory and return its shared version."""
    expected_version = validate_semver(expected_version)
    packages = load_manifest()
    expected_ids = frozenset(EXACT_PACKAGE_IDS)
    package_paths = sorted(package_directory.glob("*.nupkg"))
    symbol_paths = sorted(package_directory.glob("*.snupkg"))
    if len(package_paths) != 4 or len(symbol_paths) != 4:
        raise ValueError(
            f"expected four package and four symbol archives, found {len(package_paths)} and {len(symbol_paths)}"
        )

    boundaries = {
        package.package_id: _load_restore_boundary(package.package_id, package.project_path)
        for package in packages
    }
    metadata_by_id: dict[str, PackageMetadata] = {}
    for package_path in package_paths:
        metadata = _metadata(package_path)
        if metadata.package_id in metadata_by_id:
            raise ValueError(f"duplicate package id in output: {metadata.package_id}")
        if metadata.package_id not in expected_ids:
            raise ValueError(f"unknown package id in output: {metadata.package_id}")
        if metadata.version != expected_version:
            raise ValueError(
                f"{package_path.name}: package version {metadata.version} does not equal expected {expected_version}"
            )
        if not metadata.has_license:
            raise ValueError(f"{package_path.name}: license metadata is absent")
        expected_dependencies = _candidate_dependencies(
            boundaries[metadata.package_id],
            expected_version,
        )
        if metadata.dependencies != expected_dependencies:
            missing = sorted(set(expected_dependencies) - set(metadata.dependencies))
            unexpected = sorted(set(metadata.dependencies) - set(expected_dependencies))
            mismatched = sorted(
                dependency_id
                for dependency_id in set(expected_dependencies) & set(metadata.dependencies)
                if expected_dependencies[dependency_id] != metadata.dependencies[dependency_id]
            )
            raise ValueError(
                f"{package_path.name}: dependency boundary mismatch; missing={missing}, "
                f"unexpected={unexpected}, version-mismatch={mismatched}"
            )
        metadata_by_id[metadata.package_id] = metadata

    if set(metadata_by_id) != expected_ids:
        raise ValueError(f"package ids do not equal the release inventory: {sorted(metadata_by_id)}")
    version = expected_version

    expected_names = {
        f"{package_id}.{version}.nupkg" for package_id in EXACT_PACKAGE_IDS
    } | {
        f"{package_id}.{version}.snupkg" for package_id in EXACT_PACKAGE_IDS
    }
    actual_names = {path.name for path in package_paths + symbol_paths}
    if actual_names != expected_names:
        raise ValueError(
            f"archive names differ from the exact release inventory; "
            f"missing={sorted(expected_names - actual_names)}, unexpected={sorted(actual_names - expected_names)}"
        )
    for symbol_path in symbol_paths:
        package_id = symbol_path.name.removesuffix(f".{version}.snupkg")
        _validate_symbol_archive(symbol_path, package_id)
    return version


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate Conversations release packages.")
    parser.add_argument("package_directory", type=Path)
    parser.add_argument(
        "expected_version",
        nargs="?",
        default="0.0.0-ci-test",
        help="Expected package version (the shared CI caller defaults to 0.0.0-ci-test).",
    )
    args = parser.parse_args()
    version = validate_packages(args.package_directory.resolve(), args.expected_version)
    print(f"Validated four package and symbol pairs at version {version}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line validation must report one concise failure.
        print(f"Package validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
