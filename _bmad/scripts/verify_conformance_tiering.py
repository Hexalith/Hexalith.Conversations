#!/usr/bin/env python3
"""Verify the structural Story 9.2 split and complete, monotonic tier execution.

The immutable Story 9.1 disposition defines identities and tiers. MSBuild supplies
the actual compilation sets and resolved references. A separate migration proposal
retains every old strength and derives the successor; an absent digest-bound Quality
decision is a blocker, never an implicit approval. Verification is read-only except
for the explicitly requested output. --propose-migration writes review evidence.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import re
import shlex
import struct
import subprocess
import sys
from pathlib import Path
from typing import Any
from xml.etree import ElementTree


def _load(name: str, path: Path) -> Any:
    specification = importlib.util.spec_from_file_location(name, path)
    assert specification is not None and specification.loader is not None
    module = importlib.util.module_from_spec(specification)
    sys.modules[name] = module
    specification.loader.exec_module(module)
    return module


TIERING = _load("story92_frozen_tiering", Path(__file__).with_name("generate_conformance_tiering.py"))
RECORD = TIERING.RECORD
CONTRACT = "_bmad-output/planning-artifacts/v9/story-contracts/9.2.json"
AMENDMENT = "_bmad-output/planning-artifacts/v9/story-9.2-execution-amendment-v1.json"
DISPOSITION = TIERING.OUTPUT_PATHS[1]
PREDECESSOR = "docs/release-evidence/story-9.1-final-record-v2.json"
MIGRATION = "docs/release-evidence/conformance-oracle-tiering-migration-v3.json"
APPROVAL = "docs/release-evidence/conformance-oracle-tiering-migration-approval-v3.json"
SNAPSHOT = "docs/release-evidence/public-contract-shape-story-9.2-v2.json"
PROJECTS = {
    "portable": "tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj",
    "module-internal": TIERING.PROJECT_FILE,
}
CONTROL_IDS = {
    "portable": ["Hexalith.Conversations.Conformance.Portable.Tests.PortableCompileSurfaceValidationTest.ResolvedSurfaceShouldContainNoNonPackableModuleReference"],
    "module-internal": [
        "Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.BothTiersShouldBeDeclaredEverywhere",
        "Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.PostSplitAssertionInventoryShouldEqualApprovedDisposition",
    ],
}
AUTHORIZED_SUCCESSOR_FILES = {
    TIERING.PROJECT_DIR + "/GovernanceAuditPairingSafetyNetConformanceTest.cs":
        "Exact current governance and non-governance command sets include the pre-existing agent/deletion commands; neither set is made optional.",
    TIERING.PROJECT_DIR + "/PublicContractShapeSnapshotGenerationTest.cs":
        "Compare the complete live shape against additive current-surface evidence while retaining immutable v1 bytes; round-trip through a unique temporary file with finally cleanup.",
    TIERING.PROJECT_DIR + "/ReleaseBaselineValidationTest.cs":
        "Keep the v1 count tied to v1 bytes, bind the additive full current snapshot to the live count, and discover all fourteen suites across both assemblies.",
}
FAULT_PROPERTY = "story92ObservedFault"
FAULTS = {
    "nonportable-reference": "PORTABLE_TIER_NONPORTABLE_REFERENCE",
    "transitive-nonportable-reference": "PORTABLE_TIER_NONPORTABLE_REFERENCE",
    "resolved-nonportable-reference": "PORTABLE_TIER_NONPORTABLE_REFERENCE",
    "renamed-nonportable-reference": "PORTABLE_TIER_NONPORTABLE_REFERENCE",
    "assertion-deleted": "ASSERTION_INVENTORY_DRIFT",
    "assertion-duplicated": "ASSERTION_INVENTORY_DRIFT",
    "assertion-renamed": "ASSERTION_INVENTORY_DRIFT",
    "assertion-weakened": "ASSERTION_STRENGTH_WEAKENED",
    "tier-missing": "TIER_UNASSIGNED",
    "reason-missing": "TIER_REASON_MISSING",
    "approval-missing": "TIER_APPROVAL_MISSING",
    "project-missing": "TIER_PROJECT_MISSING",
    "declaration-missing": "TIER_NOT_DECLARED",
    "completion-declaration-missing": "TIER_NOT_DECLARED",
    "denominator-drift": "FR20_DENOMINATOR_DRIFT",
    "public-widened": "PUBLIC_CONTRACT_WIDENED",
    "v1-mutated": "V1_ARTIFACT_DRIFT",
    "predecessor-disposition-detached": "V1_ARTIFACT_DRIFT",
    "execution-skipped": "TIER_EXECUTION_INCOMPLETE",
    "execution-not-run": "TIER_EXECUTION_INCOMPLETE",
    "execution-empty": "ASSERTION_LEDGER_EMPTY",
    "execution-regressed": "EXECUTED_COUNT_REGRESSION",
}


class VerificationError(Exception):
    """One stable blocker, suitable for C# controls and measured fault fixtures."""

    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code


def require(condition: Any, code: str, message: str) -> None:
    if not condition:
        raise VerificationError(code, message)


def read(root: Path, relative: str) -> bytes:
    content = TIERING.read_file(root, relative)
    require(content is not None, "TIERING_INPUT_MISSING", f"Required input is missing: {relative}")
    return content


def document(root: Path, relative: str) -> dict[str, Any]:
    try:
        result = RECORD.v2_parse_json(read(root, relative))
        require(isinstance(result, dict), "TIERING_INPUT_INVALID", f"Expected JSON object: {relative}")
        return result
    except (ValueError, UnicodeError) as error:
        raise VerificationError("TIERING_INPUT_INVALID", f"Invalid JSON: {relative}") from error


def bound(root: Path, relative: str) -> dict[str, str]:
    return {"path": relative, "sha256": TIERING.sha256_bytes(read(root, relative))}


def msbuild(root: Path, project: str, configuration: str, *, resolved: bool = False) -> dict[str, Any]:
    require((root / project).is_file(), "TIER_PROJECT_MISSING", f"Tier project is missing: {project}")
    command = ["dotnet", "msbuild", project, f"-p:Configuration={configuration}", "-nr:false",
               "-getProperty:IsPackable,AssemblyName,ProjectAssetsFile", "-getItem:Compile,ProjectReference,PackageReference,ReferencePath"]
    if resolved:
        # Resolve existing outputs without rebuilding them: a rebuild would invalidate the tier TRX files.
        command += ["-target:ResolveReferences", "-p:BuildProjectReferences=false"]
    try:
        result = subprocess.run(command, cwd=root, capture_output=True, text=True, check=False, timeout=180)
        require(result.returncode == 0, "RESOLVED_COMPILE_SURFACE_INVALID", f"MSBuild evaluation failed: {project}")
        start = result.stdout.find("{\n")
        require(start >= 0, "RESOLVED_COMPILE_SURFACE_INVALID", f"MSBuild returned no evaluated items: {project}")
        return json.loads(result.stdout[start:])
    except (OSError, subprocess.TimeoutExpired, ValueError) as error:
        raise VerificationError("RESOLVED_COMPILE_SURFACE_INVALID", f"MSBuild evaluation unavailable: {project}") from error


def evaluated_projects(root: Path, configuration: str = "Release") -> dict[str, Any]:
    """Evaluate both actual compile sets; no XML-only inference of item conditions."""
    return {tier: msbuild(root, path, configuration, resolved=tier == "portable") for tier, path in PROJECTS.items()}


def portable_surface(root: Path, evaluated: dict[str, Any], configuration: str = "Release") -> dict[str, Any]:
    """Reject non-packable modules in evaluated project graph, assets, and ReferencePath."""
    portable = evaluated["portable"]
    initial = portable["Items"]["ProjectReference"]
    direct = [item for item in initial if Path(item["DefiningProjectFullPath"]).resolve() == (root / PROJECTS["portable"]).resolve()]
    require(sorted(Path(item["FullPath"]).stem for item in direct) ==
            ["Hexalith.Conversations.Client", "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Testing"],
            "PORTABLE_TIER_NONPORTABLE_REFERENCE", "Portable direct references must be the three approved shipped surfaces.")
    queue = [Path(item["FullPath"]) for item in initial]
    seen: dict[str, dict[str, Any]] = {}
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
        require(packable,
                "PORTABLE_TIER_NONPORTABLE_REFERENCE", f"Evaluated non-packable module reference: {name}")
        seen[relative] = {"project": relative, "assembly": name, "packable": packable}
        queue.extend(Path(item["FullPath"]) for item in graph["Items"]["ProjectReference"])
        package_roots.update(item["Identity"] for item in graph["Items"].get("PackageReference", []))
    allowed = {row["assembly"] for row in seen.values() if row["packable"]}

    assets_path = Path(portable["Properties"]["ProjectAssetsFile"])
    require(assets_path.is_file() and assets_path.resolve().is_relative_to(root),
            "RESOLVED_COMPILE_SURFACE_INVALID", "Restored transitive assets are missing or outside the repository.")
    assets = json.loads(assets_path.read_bytes())
    compile_assets = []
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
                    compile_assets.append({"target": target, "library": library, "asset": asset})

    def permitted(name: str) -> bool:
        return not name.startswith("Hexalith.") or name in allowed

    references = portable["Items"]["ReferencePath"]
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
        names.append(name)
    require(bool(compile_assets), "RESOLVED_COMPILE_SURFACE_INVALID", "Transitive compile assets are empty.")
    return {"evaluatedProjects": sorted(seen.values(), key=lambda row: row["project"]),
            "referencePathAssemblies": sorted(names), "transitiveCompileAssets": sorted(compile_assets, key=lambda row: (row["target"], row["library"], row["asset"]))}


def fault_source_paths(root: Path) -> list[str]:
    """Candidate inputs copied to the disposable fault fixture, excluding synthetic evidence."""
    frozen = document(root, DISPOSITION)
    paths = {CONTRACT, AMENDMENT, DISPOSITION, PREDECESSOR, PREDECESSOR.replace(".json", ".md"),
             SNAPSHOT, TIERING.CONTRACTS_BASELINE_PATH, *TIERING.OUTPUT_PATHS,
             TIERING.CI_WORKFLOW_PATH, "Hexalith.Conversations.slnx", "Directory.Build.props",
             "Directory.Packages.props", TIERING.TESTS_PROPS, "global.json",
             "_bmad/scripts/verify_conformance_tiering.py", "_bmad/scripts/generate_conformance_tiering.py",
             "_bmad/scripts/generate_story_record.py", "_bmad/scripts/tests/test_conformance_tiering.py",
             "_bmad/schemas/story-final-record-v2.schema.json"}
    paths.update(row["path"] for row in frozen["supersedes"]["v1Artifacts"] + frozen["supersedes"]["tieringLineage"])
    paths.add(frozen["publicContract"]["reviewedClientBaseline"]["path"])
    paths.add(document(root, AMENDMENT)["preSplitMachineResult"]["path"])
    for folder in ("src", *(str(Path(path).parent) for path in PROJECTS.values())):
        paths.update(path.relative_to(root).as_posix() for path in (root / folder).rglob("*")
                     if path.is_file() and path.suffix in (".cs", ".csproj")
                     and not {"bin", "obj"}.intersection(path.relative_to(root / folder).parts))
    return sorted(paths)


def fault_source_digest(root: Path) -> str:
    """Bind fixture source/configuration bytes independently of synthetic approval, TRX, and DLLs."""
    return TIERING.sha256_bytes(TIERING.canonical_json([bound(root, path) for path in fault_source_paths(root)]))


def declarations(root: Path) -> dict[str, Any]:
    """Require exact projects once in solution, derived completion inventory, amendment, and CI."""
    for path in PROJECTS.values():
        require((root / path).is_file(), "TIER_PROJECT_MISSING", f"Tier project is missing: {path}")
    try:
        solution = ElementTree.fromstring(read(root, "Hexalith.Conversations.slnx"))
        paths = [item.attrib["Path"].replace("\\", "/") for item in solution.findall(".//Project")]
    except (ElementTree.ParseError, KeyError) as error:
        raise VerificationError("TIER_NOT_DECLARED", "The root solution inventory is invalid.") from error
    for path in PROJECTS.values():
        require(paths.count(path) == 1, "TIER_NOT_DECLARED", f"Tier must occur exactly once in the solution/completion inventory: {path}")
    try:
        completion = RECORD.root_test_projects(root, ("references",))
    except RECORD.GateError as error:
        raise VerificationError("TIER_NOT_DECLARED", "The Story 7 completion project inventory is invalid.") from error
    for path in PROJECTS.values():
        require(completion.get(Path(path).stem) == path, "TIER_NOT_DECLARED", "The completion generator omitted a tier.")
    amendment = document(root, AMENDMENT)
    require(amendment.get("projects") == PROJECTS, "TIER_NOT_DECLARED", "The execution/completion amendment must declare both exact tiers.")
    workflow = read(root, TIERING.CI_WORKFLOW_PATH).decode()
    def run_body(name: str) -> str:
        match = re.search(r"(?m)^      - name: " + re.escape(name) + r"\n(?P<body>(?:        .*\n|\n)+)", workflow)
        require(match is not None, "TIER_NOT_DECLARED", f"CI step is missing: {name}")
        body = match.group("body")
        run = re.search(r"(?m)^        run: [|>]-?\n(?P<commands>(?:          .*\n|\n)+)", body)
        require(run is not None, "TIER_NOT_DECLARED", f"CI executable run block is missing: {name}")
        return re.sub(r"\\\s*\n\s*", " ", run.group("commands"))

    for tier, step in (("portable", "Build portable conformance project"), ("module-internal", "Build conformance project")):
        tokens = shlex.split(run_body(step), comments=True)
        require(tokens == ["dotnet", "build", PROJECTS[tier], "--configuration", "Release", "-warnaserror"],
                "TIER_NOT_DECLARED", f"CI must execute the exact tier build: {PROJECTS[tier]}")
    commands = [shlex.split(line, comments=True) for line in run_body("Run current conformance checks").splitlines()]
    policy = amendment["executionPolicy"]
    for tier, variable in (("portable", "portable"), ("module-internal", "internal")):
        assembly = str(Path(PROJECTS[tier]).parent / "bin/Release/net10.0" / (Path(PROJECTS[tier]).stem + ".dll"))
        assignments = [line for line in commands if line and line[0].startswith(variable + "=")]
        require(assignments == [[variable + "=" + assembly]], "TIER_NOT_DECLARED", f"CI tier variable must name its exact assembly: {variable}")
        expected = ["uv", "run", "--frozen", "--no-sync", "dotnet", "$" + variable, "-failSkips"]
        if tier == "portable":
            for name in [*policy["exclusions"]["classes"], policy["historicalControlClass"]]:
                expected += ["-class-", name]
            for name in policy["exclusions"]["methods"]:
                expected += ["-method-", name]
        expected += ["-parallelMode", "none", "-result-trx", f"TestResults/conformance/{variable}.trx", "-noLogo"]
        require(commands.count(expected) == 1, "TIER_NOT_DECLARED", f"CI must invoke each exact tier once: {variable}")
    require("verify_conformance_tiering.py" in workflow and "test_conformance_tiering.py -k structural_and_execution_faults" in workflow,
            "TIER_NOT_DECLARED", "CI must verify combined execution and measured faults.")
    return {"solution": bound(root, "Hexalith.Conversations.slnx"), "workflow": bound(root, TIERING.CI_WORKFLOW_PATH),
            "completionInventory": completion,
            "executionAmendment": bound(root, AMENDMENT)}


def inputs(root: Path, contract_path: str = CONTRACT) -> tuple[dict[str, Any], dict[str, Any]]:
    require(contract_path == CONTRACT, "TIERING_INPUT_INVALID", "Use the immutable Story 9.2 contract.")
    amendment = document(root, AMENDMENT)
    require(amendment.get("frozenContract") == bound(root, CONTRACT), "TIERING_INPUT_INVALID", "The frozen contract binding changed.")
    require(amendment.get("disposition") == bound(root, DISPOSITION) and amendment.get("predecessor") == bound(root, PREDECESSOR),
            "TIERING_INPUT_INVALID", "Story 9.1 disposition or final record binding changed.")
    predecessor = document(root, PREDECESSOR)
    require(predecessor.get("storyId") == "9.1" and predecessor.get("summary") ==
            {"required": 9, "passed": 9, "failed": 0, "blocked": 0, "skipped": 0, "notRun": 0}
            and not RECORD.v2_verify_pair(read(root, PREDECESSOR), read(root, PREDECESSOR.replace(".json", ".md"))),
            "TIERING_INPUT_INVALID", "Story 9.1 is not a verified accepted predecessor pair.")
    for binding in predecessor["conformanceTiering"]["disposition"].values():
        require(binding == bound(root, binding["path"]), "V1_ARTIFACT_DRIFT",
                "Frozen disposition bytes differ from the accepted Story 9.1 record.")
    d = document(root, DISPOSITION)
    rows = d["assertions"] + d["validationAdditions"]
    require(len({row["id"] for row in rows}) == len(rows), "ASSERTION_INVENTORY_DRIFT", "Frozen identities are duplicated.")
    for row in rows:
        require(row.get("tier") in PROJECTS, "TIER_UNASSIGNED", f"Frozen row lacks a tier: {row['id']}")
        require(bool(row.get("rationale")) and (bool(row.get("internalTypeAndReason", {}).get("reason"))
                if row["tier"] == "module-internal" else bool(row.get("publicReplacement", {}).get("equalStrength"))),
                "TIER_REASON_MISSING", f"Frozen row lacks its exact disposition: {row['id']}")
    protected = d["supersedes"]["v1Artifacts"] + d["supersedes"]["tieringLineage"] + [d["publicContract"]["reviewedClientBaseline"]]
    for row in protected:
        require(TIERING.sha256_bytes(read(root, row["path"])) == row["sha256"], "V1_ARTIFACT_DRIFT", f"Protected evidence changed: {row['path']}")
    require(TIERING.surface(TIERING.WorkTree(root)) == d["publicContract"]["freezeSurface"]["projects"],
            "PUBLIC_CONTRACT_WIDENED", "Public module sources differ from the retained Story 9.1 freeze.")
    require(d["fr20Membership"]["v1Floor"]["suiteCount"] == 14 and d["fr20Membership"]["v1Floor"]["testCount"] == 214,
            "FR20_DENOMINATOR_DRIFT", "The immutable FR-20 denominator changed.")
    policy = amendment["executionPolicy"]
    require(policy.get("exclusions") == d["preSplitResult"]["lane"]["exclusions"] and policy.get("frozenExecutedCaseFloor") == 415,
            "EXECUTED_COUNT_REGRESSION", "The accepted active execution floor or historical exclusions changed.")
    # The floor comes from a hash-bound machine result, not a transcribed count.
    retained_result = d["preSplitResult"]["result"]
    retained_copy = amendment["preSplitMachineResult"]
    require(retained_copy.get("sha256") == retained_result["sha256"] and retained_copy.get("originalPath") == retained_result["path"]
            and bound(root, retained_copy["path"])["sha256"] == retained_result["sha256"],
            "TIERING_PRE_SPLIT_RESULT_INVALID", "The retained pre-split machine result digest changed.")
    measured = RECORD.parse_trx(read(root, retained_copy["path"]))
    require(not RECORD.count_disagreements(measured) and measured["reported"]["executed"] == 415,
            "TIERING_PRE_SPLIT_RESULT_INVALID", "The retained machine result does not prove the 415-case floor.")
    return d, amendment


def _known_generated_metadata(root: Path, project: str, path: Path) -> bool:
    """Exclude only the known SDK/test-runner generated files in the tier's obj output."""
    try:
        parts = path.relative_to(root / Path(project).parent / "obj").parts
    except ValueError:
        return False
    names = {Path(project).stem + ".AssemblyInfo.cs", Path(project).stem + ".GlobalUsings.g.cs",
             ".NETCoreApp,Version=v10.0.AssemblyAttributes.cs", "XunitAutoGeneratedEntryPoint.cs", "SelfRegisteredExtensions.cs"}
    if len(parts) != 3 or parts[0] not in ("Debug", "Release") or parts[1] != "net10.0" or parts[2] not in names:
        return False
    content = path.read_bytes()
    if b"<auto-generated" not in content and parts[2] != ".NETCoreApp,Version=v10.0.AssemblyAttributes.cs":
        return False
    tokens = TIERING.tokenize(content.decode("utf-8-sig"), path.as_posix(), strict=False)
    declared_types = {tokens[index + 1].text for index, token in enumerate(tokens[:-1])
                      if token.text in ("class", "struct", "interface", "enum", "record", "delegate")
                      and tokens[index + 1].kind == "id"}
    allowed_types = {"XunitAutoGeneratedEntryPoint.cs": {"XunitAutoGeneratedEntryPoint"},
                     "SelfRegisteredExtensions.cs": {"SelfRegisteredExtensions", "EmbeddedAttribute"}}.get(parts[2], set())
    return (declared_types <= allowed_types
            and not any(token.kind == "id" and token.text in ("Fact", "Theory", "FactAttribute", "TheoryAttribute") for token in tokens)
            and not TIERING.assertion_sites(tokens, 0, len(tokens)))


def source_inventory(root: Path, evaluated: dict[str, Any], frozen: dict[str, Any]) -> list[dict[str, Any]]:
    """Derive identities, closures, assertion counts, and strengths separately per evaluated tier."""
    tree = TIERING.WorkTree(root)
    assemblies = TIERING.module_assemblies(tree)
    first_party = [item["name"] for item in assemblies]
    modules = {name: [TIERING.parse_source(path, read(root, path), strict=False)
                      for path in TIERING.csharp_files(tree, f"src/{name}")] for name in first_party}
    catalog = TIERING.ModuleCatalog.build(modules)
    before = {row["id"]: row for row in frozen["assertions"] + frozen["validationAdditions"]}
    records = []
    for tier, project in PROJECTS.items():
        paths = []
        for item in evaluated[tier]["Items"]["Compile"]:
            path = Path(item["FullPath"]).resolve()
            require(path.is_relative_to(root), "ASSERTION_INVENTORY_DRIFT", "Compile source escapes the repository.")
            relative = path.relative_to(root).as_posix()
            if {"obj", "bin"}.intersection(path.relative_to(root).parts):
                require(_known_generated_metadata(root, project, path), "ASSERTION_INVENTORY_DRIFT",
                        f"An unrecognized compiled source under bin/obj cannot be omitted: {relative}")
            else:
                paths.append(relative)
        require(len(paths) == len(set(paths)), "ASSERTION_INVENTORY_DRIFT", "Evaluated Compile contains duplicate source files.")
        try:
            files = [TIERING.parse_source(path, read(root, path), strict=True) for path in paths]
            model = TIERING.ProjectModel(files, catalog, tuple(sorted(set(TIERING.using_items(read(root, TIERING.TESTS_PROPS)) + TIERING.using_items(read(root, project))))))
            tests = model.tests()
            for case in tests:
                closure = model.closure(case)
                material = model.strength(case, closure, first_party)
                old = before.get(case.identity)
                records.append({"id": case.identity, "tier": tier, "kind": case.kind,
                                "sourcePath": case.declaring_type.file.path, "sourceSha256": case.declaring_type.file.sha256,
                                "strengthMaterial": material, "strengthSha256": TIERING.strength_sha256(material),
                                "assertionSiteCount": len(closure.assertion_sites),
                                "closureFiles": [{"path": path, "sha256": TIERING.sha256_bytes(read(root, path))} for path in closure.files],
                                "control": old is None, "beforeStrengthSha256": old["strengthSha256"] if old else None,
                                "beforeStrengthMaterial": old["strengthMaterial"] if old else None,
                                "dispositionEvidence": {key: old[key] for key in
                                    ("rationale", "owner", "approval", "publicReplacement", "internalTypeAndReason") if key in old} if old else None})
        except TIERING.SourceModelError as error:
            raise VerificationError("ASSERTION_INVENTORY_DRIFT", str(error)) from error
    ids = [row["id"] for row in records]
    require(len(ids) == len(set(ids)), "ASSERTION_INVENTORY_DRIFT", "An assertion compiles in both tiers.")
    expected = set(before) | {identity for values in CONTROL_IDS.values() for identity in values}
    require(set(ids) == expected, "ASSERTION_INVENTORY_DRIFT", f"Compiled assertion identities differ; missing={sorted(expected-set(ids))[:3]}, added={sorted(set(ids)-expected)[:3]}")
    for row in records:
        old = before.get(row["id"])
        expected_tier = old["tier"] if old else next(tier for tier, controls in CONTROL_IDS.items() if row["id"] in controls)
        require(row["tier"] == expected_tier, "ASSERTION_INVENTORY_DRIFT", f"Assertion compiled in the wrong tier: {row['id']}")
        if old:
            require(row["sourcePath"] == old["sourcePath"], "ASSERTION_INVENTORY_DRIFT", f"An assertion source path was renamed: {row['id']}")
            if row["strengthSha256"] != old["strengthSha256"]:
                require(row["sourcePath"] in AUTHORIZED_SUCCESSOR_FILES,
                        "ASSERTION_STRENGTH_WEAKENED", f"Unreviewed strength departure: {row['id']}")
                require(row["strengthMaterial"]["boundAssemblies"] == old["strengthMaterial"]["boundAssemblies"]
                        and row["strengthMaterial"]["negativeCaseCount"] >= old["strengthMaterial"]["negativeCaseCount"]
                        and row["assertionSiteCount"] >= old["assertionSiteCount"],
                        "ASSERTION_STRENGTH_WEAKENED", f"A successor removes bindings or assertions: {row['id']}")
    return sorted(records, key=lambda row: row["id"])


def derive_migration(root: Path, *, configuration: str = "Release", evaluated: dict[str, Any] | None = None) -> dict[str, Any]:
    """Derive review material without claiming Quality approval."""
    frozen, amendment = inputs(root)
    declared = declarations(root)
    evaluated = evaluated or evaluated_projects(root, configuration)
    surface = portable_surface(root, evaluated, configuration)
    rows = source_inventory(root, evaluated, frozen)
    baseline = document(root, TIERING.CONTRACTS_BASELINE_PATH)
    current = document(root, SNAPSHOT)
    old_types = {row["namespace"] + "." + row["name"]: row for row in baseline["types"]}
    new_types = {row["namespace"] + "." + row["name"]: row for row in current["types"]}
    drift = {"addedTypes": sorted(set(new_types)-set(old_types)), "removedTypes": sorted(set(old_types)-set(new_types)),
             "changedTypes": [{"identity": identity, "before": old_types[identity], "after": new_types[identity]}
                              for identity in sorted(set(old_types) & set(new_types)) if old_types[identity] != new_types[identity]]}
    changed = []
    for row in rows:
        if row["beforeStrengthSha256"] is not None and row["beforeStrengthSha256"] != row["strengthSha256"]:
            changed.append({**row, "reason": AUTHORIZED_SUCCESSOR_FILES[row["sourcePath"]],
                            "rowSha256": TIERING.sha256_bytes(TIERING.canonical_json(row))})
    proposal = {"storyId": "9.2", "contract": bound(root, CONTRACT), "executionAmendment": bound(root, AMENDMENT),
                "predecessorRecord": bound(root, PREDECESSOR), "beforeDisposition": bound(root, DISPOSITION),
                "projects": {tier: bound(root, path) for tier, path in PROJECTS.items()}, "declarations": declared,
                "portableSurface": surface, "assertions": [row for row in rows if not row["control"]],
                "liveControls": [row for row in rows if row["control"]], "changedAssertions": changed,
                "beforeIdentitySha256": TIERING.sha256_bytes(TIERING.canonical_json(sorted(row["id"] for row in frozen["assertions"]))),
                "afterIdentitySha256": TIERING.sha256_bytes(TIERING.canonical_json(sorted(row["id"] for row in rows if row["id"] in {r["id"] for r in frozen["assertions"]}))),
                "beforeStrengthInventorySha256": TIERING.sha256_bytes(TIERING.canonical_json([[row["id"], row["strengthSha256"]] for row in frozen["assertions"]])),
                "afterStrengthInventorySha256": TIERING.sha256_bytes(TIERING.canonical_json([[row["id"], row["strengthSha256"]] for row in rows if not row["control"] and row["id"] in {r["id"] for r in frozen["assertions"]} ])),
                "fr20Membership": frozen["fr20Membership"], "denominatorSuites": frozen["denominatorSuites"],
                "publicSurface": {"baseline": bound(root, TIERING.CONTRACTS_BASELINE_PATH), "currentSnapshot": bound(root, SNAPSHOT),
                                  "drift": drift, "driftSha256": TIERING.sha256_bytes(TIERING.canonical_json(drift)),
                                  "productionSurface": frozen["publicContract"]["freezeSurface"], "approvalClaimed": False},
                "executionPolicy": amendment["executionPolicy"], "rollback": amendment["rollback"]}
    return {"schemaVersion": "hexalith.conversations.conformance-oracle-tiering-migration.v3",
            "status": "quality-review-pending", "proposal": proposal,
            "proposalSha256": TIERING.sha256_bytes(TIERING.canonical_json(proposal)),
            "approvalRequired": {"role": "Quality owner", "path": APPROVAL, "changedAssertionRows": len(changed),
                                 "publicDriftApproved": False, "successorStrengthsApproved": False}}


def _genuine_approval_identity(approval: dict[str, Any]) -> bool:
    return not any("SYNTHETIC-FIXTURE" in approval.get(field, "").upper() for field in ("approver", "approvalId"))


def approved_migration(root: Path, derived: dict[str, Any], *, require_approval: bool = True) -> dict[str, Any] | None:
    recorded = document(root, MIGRATION)
    for row in recorded.get("proposal", {}).get("assertions", []):
        require(row.get("tier") in PROJECTS, "TIER_UNASSIGNED", "A migrated assertion lacks a tier.")
        evidence = row.get("dispositionEvidence") or {}
        require(bool(evidence.get("rationale")) and (bool(evidence.get("internalTypeAndReason", {}).get("reason"))
                if row["tier"] == "module-internal" else bool(evidence.get("publicReplacement", {}).get("equalStrength"))),
                "TIER_REASON_MISSING", "A migrated assertion lacks its exact disposition reason.")
    require(recorded.get("proposal", {}).get("fr20Membership") == derived["proposal"]["fr20Membership"]
            and recorded.get("proposal", {}).get("denominatorSuites") == derived["proposal"]["denominatorSuites"],
            "FR20_DENOMINATOR_DRIFT", "Migrated denominator membership differs from the frozen disposition.")
    require(recorded == derived, "ASSERTION_STRENGTH_WEAKENED", "Recorded migration does not reproduce from the current evaluated sources.")
    if not require_approval:
        return None
    require((root / APPROVAL).is_file(), "TIER_APPROVAL_MISSING", "Quality approval of the exact migration and public drift digest is pending.")
    approval = document(root, APPROVAL)
    required = {"proposalSha256": derived["proposalSha256"],
                "changedAssertionRows": [[row["id"], row["rowSha256"]] for row in derived["proposal"]["changedAssertions"]],
                "publicDriftSha256": derived["proposal"]["publicSurface"]["driftSha256"]}
    require(set(approval) == {"schemaVersion", "status", "role", "approver", "approvalId", "approvedOn", "evidence", "binding"}
            and approval.get("schemaVersion") == "hexalith.conversations.conformance-oracle-tiering-migration-approval.v3"
            and approval.get("status") == "approved" and approval.get("role") == "Quality owner"
            and all(isinstance(approval.get(field), str) and approval[field].strip() for field in ("approver", "approvalId", "approvedOn", "evidence"))
            and TIERING.iso_date(approval.get("approvedOn"))
            and _genuine_approval_identity(approval)
            and approval.get("binding") == required,
            "TIER_APPROVAL_MISSING", "Quality approval is absent or does not bind every proposed changed row and public drift digest.")
    return bound(root, APPROVAL)


def _execution_binary_is_managed(content: bytes) -> bool:
    """Recognize a PE/CLI assembly with contained CLI metadata, not a version/fixture marker."""
    try:
        require(content[:2] == b"MZ", "TIER_EXECUTION_INCOMPLETE", "not a PE image")
        pe = struct.unpack_from("<I", content, 0x3C)[0]
        if content[pe:pe+4] != b"PE\0\0":
            return False
        sections = struct.unpack_from("<H", content, pe + 6)[0]
        optional_size = struct.unpack_from("<H", content, pe + 20)[0]
        optional = pe + 24
        magic = struct.unpack_from("<H", content, optional)[0]
        directories = optional + (96 if magic == 0x10B else 112 if magic == 0x20B else 0)
        if directories == optional or directories + 15 * 8 > optional + optional_size:
            return False
        if struct.unpack_from("<I", content, directories - 4)[0] < 15:
            return False
        cli_rva, cli_size = struct.unpack_from("<II", content, directories + 14 * 8)
        def offset(rva: int, size: int) -> int:
            for index in range(sections):
                section = optional + optional_size + index * 40
                virtual_size, address, raw_size, raw_offset = struct.unpack_from("<IIII", content, section + 8)
                delta = rva - address
                if 0 <= delta and delta + size <= raw_size and raw_offset + delta + size <= len(content):
                    return raw_offset + delta
            raise ValueError("RVA is outside image sections")
        if not cli_rva or cli_size < 72:
            return False
        cli = offset(cli_rva, 72)
        if struct.unpack_from("<I", content, cli)[0] < 72:
            return False
        metadata_rva, metadata_size = struct.unpack_from("<II", content, cli + 8)
        metadata = offset(metadata_rva, metadata_size)
        if metadata_size < 20 or content[metadata:metadata+4] != b"BSJB":
            return False
        version_size = struct.unpack_from("<I", content, metadata + 12)[0]
        cursor = metadata + 16 + ((version_size + 3) & ~3)
        if not version_size or cursor + 4 > metadata + metadata_size:
            return False
        streams_count = struct.unpack_from("<H", content, cursor + 2)[0]
        cursor += 4
        streams = {}
        if not 1 <= streams_count <= 64:
            return False
        for _ in range(streams_count):
            stream_offset, stream_size = struct.unpack_from("<II", content, cursor)
            end = content.index(b"\0", cursor + 8, min(cursor + 40, metadata + metadata_size))
            name = content[cursor+8:end]
            if name in streams or stream_offset + stream_size > metadata_size:
                return False
            streams[name] = (metadata + stream_offset, stream_size)
            cursor = (end + 4) & ~3
        table = streams.get(b"#~") or streams.get(b"#-")
        if not table or not {b"#Strings", b"#Blob", b"#GUID"}.issubset(streams) or table[1] < 24:
            return False
        valid = struct.unpack_from("<Q", content, table[0] + 8)[0]
        if not valid & 1 or not valid & (1 << 32) or table[1] < 24 + valid.bit_count() * 4:
            return False
        module_rows = struct.unpack_from("<I", content, table[0] + 24)[0]
        assembly_rows = struct.unpack_from("<I", content, table[0] + 24 + (valid & ((1 << 32)-1)).bit_count() * 4)[0]
        return module_rows == assembly_rows == 1
    except (VerificationError, ValueError, struct.error):
        return False


def _joined_execution_definitions(content: bytes, root: Path, assembly_path: str,
                                  methods: set[str], expected_names: dict[str, str]) -> None:
    """Join each result to exactly one definition with its real tier method and binary."""
    tree = ElementTree.fromstring(content)
    definitions = {}
    for definition in tree.findall("./{*}TestDefinitions/{*}UnitTest"):
        identifier = definition.get("id")
        declarations = definition.findall("./{*}TestMethod")
        require(identifier and identifier not in definitions and len(declarations) == 1,
                "TIER_EXECUTION_INCOMPLETE", "TRX test definitions must have unique IDs and one method.")
        method = declarations[0]
        identity = (method.get("className") or "") + "." + (method.get("name") or "")
        require(identity in methods and bool(method.get("codeBase"))
                and Path(method.get("codeBase")).resolve() == (root / assembly_path).resolve(),
                "TIER_EXECUTION_INCOMPLETE", "TRX definition does not identify a valid method in its tier assembly.")
        definitions[identifier] = identity
    for result in tree.findall("./{*}Results/{*}UnitTestResult"):
        identity = definitions.get(result.get("testId"))
        require(identity is not None and expected_names.get(result.get("testName")) == identity,
                "TIER_EXECUTION_INCOMPLETE", "TRX result is orphaned or claims a different tier method.")


def execution(root: Path, frozen: dict[str, Any], portable_result: str, internal_result: str, configuration: str = "Release",
              *, allow_control_failure: bool = False) -> dict[str, Any]:
    """Recompute actual cases; exclude historical definitions, and count live controls separately."""
    rows = frozen["assertions"]
    baseline_names = {item["testName"] for row in rows if row["preSplitResultIdentity"]["lane"] == "executed" for item in row["preSplitResultIdentity"]["results"]}
    require(len(baseline_names) == frozen["preSplitResult"]["counts"]["executedTestCases"] == 415,
            "EXECUTED_COUNT_REGRESSION", "The machine-derived baseline case floor is inconsistent.")
    tiers = {}
    combined_names = []
    combined_methods = []
    for tier, result_path in (("portable", portable_result), ("module-internal", internal_result)):
        content = read(root, result_path)
        parsed = RECORD.parse_trx(content)
        require(parsed["reported"]["executed"] > 0 and parsed["results"], "ASSERTION_LEDGER_EMPTY", f"The {tier} tier executed zero cases.")
        require(not RECORD.count_disagreements(parsed) and not parsed["unknown_outcomes"], "TIER_EXECUTION_INCOMPLETE", f"TRX counters/outcomes disagree: {tier}")
        require(not parsed["reported"]["skipped"] and parsed["reported"]["executed"] == parsed["reported"]["total"],
                "TIER_EXECUTION_INCOMPLETE", f"The {tier} tier contains skipped or not-run cases.")
        failed_names = [row["test"] for row in parsed["results"] if row["outcome"] != "Passed"]
        permitted_failure = allow_control_failure and set(failed_names).issubset(CONTROL_IDS[tier])
        require(permitted_failure or (not parsed["reported"]["failed"] and parsed["reported"]["passed"] == parsed["reported"]["total"]),
                "TIER_EXECUTION_FAILED", f"The {tier} tier contains failing cases.")
        project_name = Path(PROJECTS[tier]).stem
        assembly_path = str(Path(PROJECTS[tier]).parent / "bin" / configuration / "net10.0" / (project_name + ".dll"))
        require(parsed["assemblies"] == [project_name] and parsed["code_bases"] and
                all(Path(path).resolve() == (root / assembly_path).resolve() for path in parsed["code_bases"]),
                "TIER_EXECUTION_INCOMPLETE", f"TRX does not identify the exact tier assembly: {tier}")
        require((root / assembly_path).is_file() and (root / assembly_path).stat().st_mtime_ns <= (root / result_path).stat().st_mtime_ns,
                "TEST_RESULTS_STALE", f"The {tier} assembly was rebuilt after its result.")
        require(_execution_binary_is_managed(read(root, assembly_path)), "TIER_EXECUTION_INCOMPLETE",
                f"The {tier} execution binary is not a managed assembly.")
        identities = {item["testName"]: row["id"] for row in rows if row["tier"] == tier
                      and row["preSplitResultIdentity"]["lane"] == "executed" for item in row["preSplitResultIdentity"]["results"]}
        identities.update({name: name for name in CONTROL_IDS[tier]})
        _joined_execution_definitions(content, root, assembly_path, set(identities.values()), identities)
        expected = {item["testName"] for row in rows if row["tier"] == tier and row["preSplitResultIdentity"]["lane"] == "executed"
                    for item in row["preSplitResultIdentity"]["results"]}
        controls = set(CONTROL_IDS[tier])
        names = [item["test"] for item in parsed["results"]]
        require(len(names) == len(set(names)), "ASSERTION_INVENTORY_DRIFT", f"Duplicated executed case: {tier}")
        executed_assertions = set(names) - controls
        require(len(executed_assertions) >= len(expected), "EXECUTED_COUNT_REGRESSION", f"The {tier} tier omitted frozen cases.")
        require(executed_assertions == expected and set(names) & controls == controls,
                "ASSERTION_INVENTORY_DRIFT", f"The {tier} case ledger differs from frozen active cases plus required controls.")
        methods = sorted(row["id"] for row in rows if row["tier"] == tier and row["preSplitResultIdentity"]["lane"] == "executed")
        combined_names.extend(executed_assertions)
        combined_methods.extend(methods)
        tiers[tier] = {"project": bound(root, PROJECTS[tier]), "assembly": bound(root, assembly_path),
                       "result": bound(root, result_path), "counts": parsed["reported"],
                       "activeFrozenMethods": len(methods), "activeFrozenCases": len(executed_assertions),
                       "controlCases": len(controls), "failedControlCases": failed_names,
                       "caseIdentitySha256": TIERING.sha256_bytes(TIERING.canonical_json(sorted(executed_assertions)))}
    require(len(combined_names) == len(set(combined_names)) and set(combined_names) == baseline_names,
            "ASSERTION_INVENTORY_DRIFT", "The combined tiers do not exactly preserve the active frozen case identities.")
    return {"tiers": tiers, "beforeExecutedCases": len(baseline_names), "afterExecutedCases": len(combined_names),
            "activeFrozenMethods": len(combined_methods), "controlsExecuted": sum(row["controlCases"] for row in tiers.values()),
            "beforeCaseIdentitySha256": TIERING.sha256_bytes(TIERING.canonical_json(sorted(baseline_names))),
            "afterCaseIdentitySha256": TIERING.sha256_bytes(TIERING.canonical_json(sorted(combined_names))),
            "failed": sum(row["counts"]["failed"] for row in tiers.values()), "skipped": 0, "notRun": 0,
            "completePassingExecution": all(not row["counts"]["failed"] for row in tiers.values())}


def verify(root: Path, contract_path: str = CONTRACT, portable_result: str | None = None, internal_result: str | None = None,
           *, configuration: str = "Release", require_approval: bool = True, evaluated: dict[str, Any] | None = None,
           mode: str = "complete") -> dict[str, Any]:
    """Public read-only API for final-record integration and negative fixtures."""
    report: dict[str, Any] = {"schemaVersion": "hexalith.conversations.conformance-tier-execution.v1", "storyId": "9.2",
                              "result": "FAIL", "exitCode": 1, "blockers": []}
    try:
        if mode == "declarations":
            report["declarations"] = declarations(root)
        elif mode == "surface":
            evaluated = evaluated or {"portable": msbuild(root, PROJECTS["portable"], configuration, resolved=True)}
            report["portableSurface"] = portable_surface(root, evaluated, configuration)
        else:
            frozen, _ = inputs(root, contract_path)
            report["declarations"] = declarations(root)
            if mode == "complete" and portable_result and internal_result:
                report["observedExecution"] = execution(root, frozen, portable_result, internal_result, configuration,
                                                        allow_control_failure=True)
            derived = derive_migration(root, configuration=configuration, evaluated=evaluated)
            approval = approved_migration(root, derived, require_approval=require_approval)
            report["migration"] = bound(root, MIGRATION)
            report["proposalSha256"] = derived["proposalSha256"]
            report["approval"] = approval
            report["inventories"] = {key: derived["proposal"][key] for key in
                                     ("beforeIdentitySha256", "afterIdentitySha256", "beforeStrengthInventorySha256", "afterStrengthInventorySha256")}
            if mode == "complete":
                require(portable_result and internal_result, "ASSERTION_LEDGER_EMPTY", "Both tier results are required.")
                report["execution"] = execution(root, frozen, portable_result, internal_result, configuration)
        report.update(result="PASS", exitCode=0)
    except (VerificationError, TIERING.TieringError) as error:
        report["blockers"] = [{"code": error.code, "message": str(error)}]
    except (KeyError, TypeError, ValueError, OSError, ElementTree.ParseError) as error:
        report["blockers"] = [{"code": "TIERING_INPUT_INVALID", "message": type(error).__name__ + ": " + str(error)}]
    return report


def _json_output_target(root: Path, path: str) -> Path:
    relative = RECORD.safe_relative_path(path)
    target = root / relative
    require(not target.is_symlink() and target.resolve().is_relative_to(root), "OUTPUT_PATH_INVALID", "Output escapes the repository.")
    return target


def write_json(root: Path, path: str, value: dict[str, Any], *, result_inputs: tuple[str, ...] = ()) -> None:
    target = _json_output_target(root, path)
    protected = {CONTRACT, AMENDMENT, DISPOSITION, PREDECESSOR, MIGRATION, APPROVAL, SNAPSHOT,
                 "artifacts/v9/9.2/portable.trx", "artifacts/v9/9.2/internal.trx", *result_inputs}
    resolved = target.resolve().relative_to(root).as_posix()
    require(not resolved.startswith(("docs/release-evidence/", "_bmad-output/", "src/", "tests/", "_bmad/scripts/", "_bmad/schemas/",
                                     "references/", ".git/", "artifacts/v9/9.2/retained/"))
            and not {"obj", "bin"}.intersection(Path(resolved).parts)
            and Path(resolved).suffix.lower() not in (".trx", ".xml", ".dll", ".cs", ".csproj", ".props", ".targets")
            and resolved not in protected
            and resolved not in {"Hexalith.Conversations.slnx", "Directory.Build.props", "Directory.Packages.props", "global.json", TIERING.CI_WORKFLOW_PATH},
            "OUTPUT_PATH_INVALID", "Output would overwrite protected verifier input or evidence.")
    for relative in protected:
        source = root / relative
        require(target.resolve() != source.resolve(), "OUTPUT_PATH_INVALID", "Output aliases a verifier input.")
    require(not target.exists() or target.stat().st_nlink == 1, "OUTPUT_PATH_INVALID", "Output has a hard-link alias.")
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes((json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode())


def _write_migration_proposal(root: Path, value: dict[str, Any]) -> None:
    """The explicit proposal writer alone may target MIGRATION; approved bytes stay untouched."""
    if (root / APPROVAL).exists():
        require(document(root, MIGRATION) == value, "TIER_APPROVAL_MISSING",
                "An approved migration cannot be rewritten; a changed proposal requires versioned successor evidence.")
        return
    target = _json_output_target(root, MIGRATION)
    require(not target.exists() or target.stat().st_nlink == 1, "OUTPUT_PATH_INVALID", "Migration output has a hard-link alias.")
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes((json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode())


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path("."))
    parser.add_argument("--contract", default=CONTRACT)
    parser.add_argument("--portable-result")
    parser.add_argument("--internal-result")
    parser.add_argument("--configuration", choices=("Debug", "Release"), default="Release")
    parser.add_argument("--output")
    parser.add_argument("--propose-migration", action="store_true")
    parser.add_argument("--structure-only", action="store_true")
    parser.add_argument("--declarations-only", action="store_true")
    parser.add_argument("--surface-only", action="store_true")
    args = parser.parse_args(argv)
    root = args.repository.resolve()
    if args.propose_migration:
        try:
            value = derive_migration(root, configuration=args.configuration)
            _write_migration_proposal(root, value)
            print(f"PREPARED: {value['proposalSha256']}; {len(value['proposal']['changedAssertions'])} successor rows; Quality approval pending.")
            return 0
        except (VerificationError, TIERING.TieringError) as error:
            print(f"FAIL: {error.code}: {error}", file=sys.stderr)
            return 1
    mode = "declarations" if args.declarations_only else "surface" if args.surface_only else "structure" if args.structure_only else "complete"
    report = verify(root, args.contract, args.portable_result, args.internal_result, configuration=args.configuration, mode=mode)
    if args.output:
        try:
            write_json(root, args.output, report, result_inputs=tuple(path for path in (args.contract, args.portable_result, args.internal_result) if path))
        except (VerificationError, RECORD.GateError) as error:
            report.update(result="FAIL", exitCode=1, blockers=[{"code": "OUTPUT_PATH_INVALID", "message": str(error)}])
    for finding in report["blockers"]:
        print(f"FAIL: {finding['code']}: {finding['message']}", file=sys.stderr)
    if report["exitCode"] == 0:
        print("PASS: Story 9.2 " + mode + " verified.")
    return report["exitCode"]


if __name__ == "__main__":
    raise SystemExit(main())
