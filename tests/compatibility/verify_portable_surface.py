#!/usr/bin/env python3
"""Check the evaluated portable product graph and existing compiled dependencies."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import subprocess
from typing import Any

PROJECTS = {"portable": "tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj"}


class VerificationError(ValueError):
    """An evaluated product dependency boundary failed closed."""

    def __init__(self, code: str, message: str):
        super().__init__(code + ": " + message)


def require(condition: Any, code: str, message: str) -> None:
    if not condition:
        raise VerificationError(code, message)


def safe_relative_path(value: str) -> str | None:
    if (not isinstance(value, str) or not value or value.startswith("/") or "\\" in value
            or any(part in ("", ".", "..") for part in value.split("/"))
            or any(ord(char) < 32 or ord(char) == 127 for char in value)
            or ":" in value):
        return None
    return value


def msbuild(root: Path, project: str, configuration: str, *, resolved: bool = False) -> dict[str, Any]:
    require((root / project).is_file(), "PORTABLE_PROJECT_MISSING", f"Portable dependency project is missing: {project}")
    command = ["dotnet", "msbuild", project, f"-p:Configuration={configuration}", "-nr:false", "-noAutoResponse", "-warnaserror",
               "-getProperty:IsPackable,IsPublishable,AssemblyName,ProjectAssetsFile,TargetPath,TargetRefPath",
               "-getItem:Compile,ProjectReference,PackageReference,Reference,ReferencePath,FrameworkReference,ResolvedFrameworkReference"]
    if resolved:
        command += ["-target:ResolveReferences", "-p:BuildProjectReferences=false"]
    try:
        result = subprocess.run(command, cwd=root, capture_output=True, text=True, check=False, timeout=180)
        require(result.returncode == 0, "RESOLVED_COMPILE_SURFACE_INVALID", f"MSBuild evaluation failed: {project}: {result.stdout} {result.stderr}")
        start = result.stdout.find("{\n")
        require(start >= 0, "RESOLVED_COMPILE_SURFACE_INVALID", f"MSBuild returned no evaluated items: {project}")
        return json.loads(result.stdout[start:])
    except (OSError, subprocess.TimeoutExpired, ValueError) as error:
        raise VerificationError("RESOLVED_COMPILE_SURFACE_INVALID", f"MSBuild evaluation unavailable: {project}") from error


def portable_surface(root: Path, evaluated: dict[str, Any], configuration: str = "Release") -> dict[str, Any]:
    """Reject non-packable modules in evaluated project graph, assets, and ReferencePath."""
    portable = evaluated["portable"]
    initial = portable["Items"]["ProjectReference"]
    # Reconciliation removes and re-adds ProjectReference items during targets, so
    # DefiningProjectFullPath in the resolved pass identifies the generated import.
    # Read authored declarations from a separate SDK evaluation without targets;
    # keep the resolved pass independently authoritative for compiled dependencies.
    declarations = msbuild(root, PROJECTS["portable"], configuration)["Items"]["ProjectReference"]
    project_file = (root / PROJECTS["portable"]).resolve()
    declared = sorted(Path(item["FullPath"]).resolve() for item in declarations
                      if Path(item.get("DefiningProjectFullPath") or root).resolve() == project_file)
    approved = sorted((root / "src" / name / f"{name}.csproj").resolve() for name in (
        "Hexalith.Conversations.Client", "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Testing"))
    # The neutral nonshipping TestSupport helper must remain dependency-free.
    helper_relative = "tests/Hexalith.Conversations.TestSupport/Hexalith.Conversations.TestSupport.csproj"
    helper_path = (root / helper_relative).resolve()
    if helper_path in declared:
        approved = sorted([*approved, helper_path])
    require(declared == approved,
            "PORTABLE_TIER_NONPORTABLE_REFERENCE", "Portable declared references must be the three approved shipped surfaces.")
    queue = list(declared)
    seen: dict[str, dict[str, Any]] = {}
    project_outputs: dict[str, set[Path]] = {}
    package_roots = {item["Identity"] for item in portable["Items"].get("PackageReference", [])}
    while queue:
        path = queue.pop()
        require(path.resolve().is_relative_to(root), "RESOLVED_COMPILE_SURFACE_INVALID", "A project reference escapes the repository.")
        relative = path.resolve().relative_to(root).as_posix()
        if relative in seen:
            continue
        graph = msbuild(root, relative, configuration)
        properties = graph["Properties"]
        name = properties["AssemblyName"]
        packable = properties["IsPackable"].lower() == "true"
        neutral_helper = (relative == helper_relative and name == "Hexalith.Conversations.TestSupport"
            and not packable and properties.get("IsPublishable", "true").lower() == "false"
            and all(not graph["Items"].get(item, []) for item in
                ("ProjectReference", "PackageReference", "Reference", "ReferencePath"))
            and all(item["Identity"] == "Microsoft.NETCore.App" and item.get("IsImplicitlyDefined", "").lower() == "true"
                for item in graph["Items"].get("FrameworkReference", [])))
        require(neutral_helper if relative == helper_relative else packable,
                "PORTABLE_TIER_NONPORTABLE_REFERENCE", f"Evaluated non-packable module reference: {name}")
        seen[relative] = {"project": relative, "assembly": name, "packable": packable}
        project_outputs.setdefault(name, set()).update(
            Path(properties[key]).resolve() for key in ("TargetPath", "TargetRefPath") if properties.get(key))
        queue.extend(Path(item["FullPath"]) for item in graph["Items"]["ProjectReference"])
        package_roots.update(item["Identity"] for item in graph["Items"].get("PackageReference", []))
    # The SDK copies transitive project references from the restore, which differ between
    # package and source-reference restores. Any reference this project did not declare,
    # including one imported by a props file, must lie in the approved surfaces' closure.
    reached = {(root / relative).resolve() for relative in seen}
    require(all(Path(item["FullPath"]).resolve() in reached for item in [*declarations, *initial]),
            "PORTABLE_TIER_NONPORTABLE_REFERENCE",
            "A portable evaluated reference is neither declared nor reachable from the approved surfaces.")
    allowed = {row["assembly"] for row in seen.values() if row["packable"] or row["project"] == helper_relative}

    assets_path = Path(portable["Properties"]["ProjectAssetsFile"])
    require(assets_path.is_file() and assets_path.resolve().is_relative_to(root),
            "RESOLVED_COMPILE_SURFACE_INVALID", "Restored transitive assets are missing or outside the repository.")
    assets = json.loads(assets_path.read_bytes())
    compile_assets = []
    package_outputs: dict[str, set[Path]] = {}
    for target, libraries in assets["targets"].items():
        by_name = {library.partition("/")[0]: metadata for library, metadata in libraries.items()}
        pending = list(allowed | package_roots)
        reached = set()
        while pending:
            name = pending.pop()
            if name in reached or name not in by_name:
                continue
            reached.add(name)
            pending.extend(by_name[name].get("dependencies", {}))
        for library, metadata in libraries.items():
            package_name = library.partition("/")[0]
            for asset in metadata.get("compile", {}):
                if asset.endswith(".dll"):
                    name = Path(asset).stem
                    shipped_package = (metadata.get("type") == "package" and package_name in reached and name == package_name)
                    require(not name.startswith("Hexalith.") or name in allowed or shipped_package,
                            "PORTABLE_TIER_NONPORTABLE_REFERENCE", f"Nonportable transitive compile asset: {name}")
                    if shipped_package:
                        allowed.add(name)
                    if metadata.get("type") == "package" and package_name in reached:
                        package_path = assets.get("libraries", {}).get(library, {}).get("path")
                        if package_path:
                            require(safe_relative_path(package_path) == package_path
                                    and safe_relative_path(asset) == asset,
                                    "RESOLVED_COMPILE_SURFACE_INVALID", "A restored package compile path is invalid.")
                            package_outputs.setdefault(name, set()).update(
                                (Path(folder) / package_path / asset).resolve() for folder in assets.get("packageFolders", {}))
                    compile_assets.append({"target": target, "library": library, "asset": asset})

    def permitted(name: str) -> bool:
        return not name.startswith("Hexalith.") or name in allowed

    references = portable["Items"]["ReferencePath"]
    framework_names = {item["Identity"] for item in portable["Items"].get("FrameworkReference", [])}
    framework_packs = {item["Identity"]: item for item in portable["Items"].get("ResolvedFrameworkReference", [])
                       if item["Identity"] in framework_names}
    require(bool(references), "RESOLVED_COMPILE_SURFACE_INVALID", "Resolved ReferencePath is empty.")
    names = []
    for item in references:
        path = Path(item["FullPath"])
        require(path.is_file(), "RESOLVED_COMPILE_SURFACE_INVALID", "A resolved reference is missing.")
        # ResolveAssemblyReference reads the assembly metadata into FusionName. File names
        # can be changed, so they cannot establish the identity of a compile dependency.
        fusion_name = item.get("FusionName", "")
        name = fusion_name.partition(",")[0].strip()
        require(bool(name) and ", Version=" in fusion_name, "RESOLVED_COMPILE_SURFACE_INVALID",
                f"Resolved reference lacks its assembly identity: {path.name}")
        require(permitted(name), "PORTABLE_TIER_NONPORTABLE_REFERENCE", f"Nonportable resolved ReferencePath: {name}")
        resolved_path = path.resolve()
        project_match = resolved_path in project_outputs.get(name, set())
        package_match = resolved_path in package_outputs.get(name, set())
        framework = framework_packs.get(item.get("FrameworkReferenceName"))
        framework_path = Path(framework["TargetingPackPath"]).resolve() if framework and framework.get("TargetingPackPath") else None
        framework_match = (framework_path is not None and resolved_path.is_relative_to(framework_path / "ref")
                           and item.get("NuGetPackageId") == framework.get("TargetingPackName")
                           and path.stem == name)
        require(project_match or package_match or framework_match, "PORTABLE_TIER_NONPORTABLE_REFERENCE",
                f"Resolved reference is outside evaluated packable projects, restored compile assets, and framework packs: {name}")
        names.append(name)
    require(bool(compile_assets), "RESOLVED_COMPILE_SURFACE_INVALID", "Transitive compile assets are empty.")
    return {"evaluatedProjects": sorted(seen.values(), key=lambda row: row["project"]),
            "referencePathAssemblies": sorted(names), "transitiveCompileAssets": sorted(compile_assets, key=lambda row: (row["target"], row["library"], row["asset"]))}



def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path.cwd())
    parser.add_argument("--configuration", choices=("Release", "Debug"), default="Release")
    args = parser.parse_args()
    root = args.repository.resolve(strict=True)
    evaluated = {"portable": msbuild(root, PROJECTS["portable"], args.configuration, resolved=True)}
    portable_surface(root, evaluated, args.configuration)
    print("PASS: portable compiled dependency surface")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
