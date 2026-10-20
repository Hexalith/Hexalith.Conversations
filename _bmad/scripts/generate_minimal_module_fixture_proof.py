#!/usr/bin/env python3
"""Generate the Story 11.2 minimal-module fixture proof (JSON + Markdown).

The proof binds the exact frozen fixture inventory, the Story 11.1 guidance and record digests, the
resolved public platform projects and assemblies, the locked restore, the warnings-as-errors build, the
measured test result and the single repository-only test AppHost. Output is deterministic and bound by
a signable-payload digest. Blocker codes are the verifier's: see verify_minimal_module_fixture.py.
Exit 0 PASS, 1 FAIL, 2 BLOCKED.
"""
from __future__ import annotations

import argparse
import json
import posixpath
import re
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path, PurePosixPath

sys.path.insert(0, str(Path(__file__).resolve().parent))
import verify_minimal_module_fixture as verifier  # noqa: E402

SCHEMA_VERSION = "hexalith.conversations.thin-module-fixture-proof.v1"
PLATFORM_ROOT = "references/Hexalith.EventStore"
OUTPUT_DIRECTORY = "bin/Release/net10.0"
COMMAND_SCENARIOS = {"restore": "AC-11.2-02", "build": "AC-11.2-03", "tests": "AC-11.2-04"}
PROVEN_CAPABILITIES = tuple(verifier.CAPABILITY_TESTS)


class ProofError(Exception):
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code
        self.message = message


sha256_bytes = verifier.sha256_bytes
canonical = verifier.canonical
Blocked = verifier.Blocked


def dotnet_sdk_version(repository: Path) -> str:
    try:
        result = subprocess.run(["dotnet", "--version"], cwd=repository, capture_output=True, text=True,
                                timeout=60, check=False)
    except (OSError, subprocess.TimeoutExpired):
        result = None
    if result is not None and result.returncode == 0 and result.stdout.strip():
        return result.stdout.strip()
    try:
        return json.loads(verifier.read_file(repository, "global.json"))["sdk"]["version"]
    except (ValueError, KeyError, TypeError):
        raise Blocked("the .NET SDK version is unavailable") from None


def lock_packages(content: bytes) -> list[tuple[str, str, str]]:
    packages = set()
    for framework in json.loads(content).get("dependencies", {}).values():
        for name, entry in framework.items():
            if entry.get("type") != "Project":
                packages.add((name, entry.get("resolved", ""), entry.get("contentHash", "")))
    return sorted(packages)


def restore_sources(repository: Path, fixture: str, packages: set) -> list[str]:
    """Report actual package feeds from NuGet's global-package metadata, never inferred defaults."""
    folders: dict[str, None] = {}  # assets-file order: the global packages folder precedes fallback folders
    for project in verifier.EXPECTED_PROJECTS:
        assets = f"{fixture}/{PurePosixPath(project).parent}/obj/project.assets.json"
        folders.update(dict.fromkeys(json.loads(verifier.read_file(repository, assets)).get("packageFolders", {})))
    sources = set()
    for name, version, _ in sorted(packages):
        for folder in folders:
            metadata = Path(folder) / name.lower() / version.lower() / ".nupkg.metadata"
            if metadata.is_file():
                source = json.loads(metadata.read_bytes()).get("source")
                if isinstance(source, str) and source:
                    sources.add(source)
                    break
        else:
            raise ProofError(verifier.BLOCK_RESTORE, f"NuGet source metadata is missing: {name}/{version}")
    return sorted(sources)


def resolve_platform_closure(repository: Path, fixture: str, projects: dict) -> list[str]:
    """Repository-relative platform projects the fixture references, directly and transitively."""
    pending: list[str] = []
    for project in projects.values():
        for reference in project["projectReferences"]:
            normalized = reference.replace("\\", "/")
            if "FixtureEventStoreRoot" in normalized:
                pending.append(posixpath.normpath(normalized.replace("$(FixtureEventStoreRoot)", PLATFORM_ROOT)))
    seen: dict[str, None] = {}
    while pending:
        path = pending.pop(0)
        if path in seen:
            continue
        seen[path] = None
        target = repository / path
        try:
            root = ElementTree.parse(target).getroot()
        except (OSError, ElementTree.ParseError):
            raise ProofError(verifier.BLOCK_RESTORE, f"platform project is unavailable: {path}") from None
        for item in root.iter("ProjectReference"):
            include = item.get("Include", "").replace("\\", "/")
            if "$(" in include:  # conditional source-reference variants are not part of the resolved graph
                continue
            nested = posixpath.normpath(posixpath.join(posixpath.dirname(path), include))
            if nested.startswith(f"{PLATFORM_ROOT}/src/"):
                pending.append(nested)
    return sorted(seen)


def find_assembly(repository: Path, fixture: str, project_dirs: list[str], name: str) -> tuple[str, str]:
    for directory in project_dirs:
        relative = f"{fixture}/{directory}/{OUTPUT_DIRECTORY}/{name}.dll"
        path = repository / relative
        if path.is_file():
            return relative, sha256_bytes(path.read_bytes())
    raise ProofError(verifier.BLOCK_BUILD, f"build output is missing: {name}.dll (run AC-11.2-03)")


def build_signable(repository: Path, fixture: str, test_result: str, candidate_commit: str | None = None) -> dict:
    contract_bytes = verifier.read_file(repository, verifier.CONTRACT_PATH)
    try:
        contract = json.loads(contract_bytes)
        commands = {key: next(row["command"] for row in contract["scenarios"] if row["id"] == scenario)
                    for key, scenario in COMMAND_SCENARIOS.items()}
        planning = contract["authority"]["planningCandidate"]
        if contract["storyId"] != "11.2":
            raise ValueError("storyId")
    except (ValueError, KeyError, TypeError, StopIteration):
        raise Blocked("story contract is not a valid Story 11.2 contract") from None

    blockers, facts = verifier.verify(repository, fixture, False, test_result, candidate_commit=candidate_commit)
    if blockers:
        first = blockers[0]
        raise ProofError(first["code"], "; ".join(f"{item['code']}: {item['message']}" for item in blockers))

    receipts = {}
    for scenario, blocker in (("AC-11.2-02", verifier.BLOCK_RESTORE), ("AC-11.2-03", verifier.BLOCK_BUILD)):
        try:
            receipts[scenario] = verifier.command_receipt(repository, scenario)
        except ValueError as error:
            raise ProofError(blocker, str(error)) from None
    commit = facts["candidate"]["commit"]
    if commit is None:
        raise Blocked("the proof requires a committed candidate")

    inventory = facts["inventory"]
    predecessor = facts["predecessor"]
    projects = facts["graph"]["projects"]
    project_dirs = [str(PurePosixPath(name).parent) for name in verifier.EXPECTED_PROJECTS]
    # Prefer the consuming test and host outputs, where copied platform assemblies sit.
    ordered_dirs = sorted(project_dirs, key=lambda item: (not item.startswith("tests/"), item))

    # Restore: locked restore evidence from the committed lock files.
    lock_rows = []
    all_packages: set = set()
    for row in inventory["lockFiles"]:
        content = verifier.read_file(repository, row["path"])
        packages = lock_packages(content)
        all_packages.update(packages)
        lock_rows.append({"path": row["path"], "sha256": row["sha256"], "packageCount": len(packages)})
    lock_rows.sort(key=lambda item: item["path"])
    if len(lock_rows) != len(verifier.EXPECTED_PROJECTS):
        raise ProofError(verifier.BLOCK_RESTORE, "a project has no lock file")
    xunit = next((version for name, version, _ in sorted(all_packages) if name == "xunit.v3"), "")
    resolved_packages = sha256_bytes(canonical(sorted(all_packages)))

    # Build: fixture assemblies and warnings-as-errors policy.
    assemblies = []
    for name in verifier.EXPECTED_PROJECTS:
        stem = PurePosixPath(name).stem
        relative, digest = find_assembly(repository, fixture, [str(PurePosixPath(name).parent)], stem)
        assemblies.append({"project": f"{fixture}/{name}", "path": relative, "sha256": digest})
    apphost_sdk = projects[verifier.APPHOST_PROJECT]["sdk"]

    # Resolved public platform API graph: project hashes and assembly hashes.
    platform_projects = []
    platform_assemblies = []
    for path in resolve_platform_closure(repository, fixture, projects):
        platform_projects.append({"name": PurePosixPath(path).stem, "path": path,
                                  "sha256": sha256_bytes(verifier.read_file(repository, path))})
        relative, digest = find_assembly(repository, fixture, ordered_dirs, PurePosixPath(path).stem)
        # A copy that differs from the platform's own Release output came from another configuration or build.
        release = repository / PurePosixPath(path).parent / OUTPUT_DIRECTORY / f"{PurePosixPath(path).stem}.dll"
        if not release.is_file() or sha256_bytes(release.read_bytes()) != digest:
            raise ProofError(verifier.BLOCK_BUILD, f"platform assembly is not its Release build output: {relative}")
        platform_assemblies.append({"name": PurePosixPath(path).stem, "path": relative, "sha256": digest})

    trx = verifier.read_file(repository, test_result)
    tests_facts = facts["tests"]
    counts = tests_facts["counts"]
    app = facts["appHost"]

    return {
        "schemaVersion": SCHEMA_VERSION,
        "candidate": {
            "storyId": "11.2",
            "checkpoint": "B",
            "contractPath": verifier.CONTRACT_PATH,
            "contractSha256": sha256_bytes(contract_bytes),
            "planningCandidate": planning,
            "commit": commit,
            "eventStoreGitlink": verifier.eventstore_gitlink(repository, commit),
        },
        "guidanceDigest": {
            "bundleJsonPath": verifier.GUIDANCE_JSON,
            "bundleJsonSha256": predecessor["bundleJsonSha256"],
            "bundleMarkdownSha256": predecessor["bundleMarkdownSha256"],
            "signablePayloadSha256": predecessor["signablePayloadSha256"],
            "sourceSha256": predecessor["sourceSha256"],
            "story11_1RecordPath": predecessor["recordPath"],
            "story11_1RecordSha256": predecessor["recordSha256"],
        },
        "fixtureInventory": {
            "id": verifier.INVENTORY_ID,
            "sha256": verifier.INVENTORY_SHA256,
            "root": fixture,
            "paths": inventory["paths"],
        },
        "resolvedPublicApis": {
            "platformRoot": PLATFORM_ROOT,
            "projects": platform_projects,
            "assemblies": platform_assemblies,
        },
        "restore": {
            "command": commands["restore"],
            "lockedMode": receipts["AC-11.2-02"]["lockedMode"],
            "receipt": {key: receipts["AC-11.2-02"][key] for key in ("path", "sha256", "exitCode")},
            "sources": restore_sources(repository, fixture, all_packages),
            "lockFiles": lock_rows,
            "resolvedPackagesSha256": resolved_packages,
            "packageCount": len(all_packages),
        },
        "build": {
            "command": commands["build"],
            "configuration": "Release",
            "warningsAsErrors": receipts["AC-11.2-03"]["warningsAsErrors"],
            "receipt": {key: receipts["AC-11.2-03"][key] for key in ("path", "sha256", "exitCode")},
            "toolVersions": {"dotnetSdk": dotnet_sdk_version(repository), "aspireAppHostSdk": apphost_sdk},
            "assemblies": assemblies,
        },
        "tests": {
            "command": commands["tests"],
            "toolVersions": {"xunitV3": xunit},
            "resultFile": {"path": test_result, "sha256": sha256_bytes(trx)},
            "total": counts["total"],
            "passed": counts["passed"],
            "failed": counts["failed"],
            "skipped": counts["skipped"],
            "notRun": counts["notRun"],
            "cases": tests_facts["cases"],
            "capabilities": [{"capability": key, "test": tests_facts["capabilities"][key]}
                             for key in PROVEN_CAPABILITIES],
        },
        "appHost": {
            "count": app["count"],
            "project": app["project"],
            "isPackable": app["isPackable"],
            "isPublishable": app["isPublishable"],
            "repositoryOnly": app["repositoryOnly"],
            "countedInAuthoringCost": True,
            "reusableAspireProjects": 0,
            "platformHelper": "AddAspireDaprDomainModule",
        },
    }


def signable_of(bundle: dict) -> dict:
    return {key: value for key, value in bundle.items() if key != "digests"}


def serialize_json(bundle: dict) -> str:
    return json.dumps(bundle, indent=2, ensure_ascii=False) + "\n"


def render_markdown(bundle: dict) -> str:
    cand = bundle["candidate"]
    guidance = bundle["guidanceDigest"]
    inventory = bundle["fixtureInventory"]
    apis = bundle["resolvedPublicApis"]
    restore, build, tests, host = bundle["restore"], bundle["build"], bundle["tests"], bundle["appHost"]
    out = ["# Thin-Module Fixture Proof v1", "", f"Schema: `{bundle['schemaVersion']}`", ""]
    out += ["## Candidate", "",
            f"- Story: `{cand['storyId']}` (Checkpoint {cand['checkpoint']})",
            f"- Contract: `{cand['contractPath']}` (`{cand['contractSha256']}`)",
            f"- Planning candidate: `{cand['planningCandidate']}`",
            f"- Committed candidate: `{cand['commit']}`",
            f"- EventStore gitlink: `{cand['eventStoreGitlink']['path']}` "
            f"(mode `{cand['eventStoreGitlink']['mode']}`, `{cand['eventStoreGitlink']['commit']}`)", ""]
    out += ["## Guidance digest", "",
            f"- Bundle: `{guidance['bundleJsonPath']}` (`{guidance['bundleJsonSha256']}`)",
            f"- Bundle Markdown: `{guidance['bundleMarkdownSha256']}`",
            f"- Signable payload: `{guidance['signablePayloadSha256']}`",
            f"- Guidance source: `{guidance['sourceSha256']}`",
            f"- Story 11.1 record: `{guidance['story11_1RecordPath']}` (`{guidance['story11_1RecordSha256']}`)", ""]
    out += ["## Fixture inventory", "",
            f"- Inventory `{inventory['id']}`: `{inventory['sha256']}`",
            f"- Root: `{inventory['root']}`", ""]
    out += [f"- `{row['path']}` (`{row['sha256']}`, {row['bytes']} bytes, {row['lines']} lines)" for row in inventory["paths"]]
    out += ["", "## Resolved public platform APIs", "", f"- Platform root: `{apis['platformRoot']}`"]
    out += [f"- Project `{row['name']}`: `{row['path']}` (`{row['sha256']}`)" for row in apis["projects"]]
    out += [f"- Assembly `{row['name']}`: `{row['path']}` (`{row['sha256']}`)" for row in apis["assemblies"]]
    out += ["", "## Restore", "", f"- Command: `{restore['command']}`", f"- Locked mode: {str(restore['lockedMode']).lower()}"]
    out += [f"- Receipt: `{restore['receipt']['path']}` (`{restore['receipt']['sha256']}`, exit {restore['receipt']['exitCode']})"]
    out += [f"- Package source: `{source}`" for source in restore["sources"]]
    out += [f"- Lock file `{row['path']}` (`{row['sha256']}`, {row['packageCount']} packages)" for row in restore["lockFiles"]]
    out += [f"- Resolved packages: {restore['packageCount']} (`{restore['resolvedPackagesSha256']}`)", ""]
    out += ["## Build", "", f"- Command: `{build['command']}`", f"- Configuration: {build['configuration']}",
            f"- Warnings as errors: {str(build['warningsAsErrors']).lower()}",
            f"- Receipt: `{build['receipt']['path']}` (`{build['receipt']['sha256']}`, exit {build['receipt']['exitCode']})",
            f"- .NET SDK: `{build['toolVersions']['dotnetSdk']}`",
            f"- Aspire AppHost SDK: `{build['toolVersions']['aspireAppHostSdk']}`"]
    out += [f"- Assembly `{row['path']}` (`{row['sha256']}`)" for row in build["assemblies"]]
    out += ["", "## Tests", "", f"- Command: `{tests['command']}`",
            f"- xUnit v3: `{tests['toolVersions']['xunitV3']}`",
            f"- Result: `{tests['resultFile']['path']}` (`{tests['resultFile']['sha256']}`)",
            f"- Total {tests['total']}, passed {tests['passed']}, failed {tests['failed']}, "
            f"skipped {tests['skipped']}, not run {tests['notRun']}"]
    out += [f"- Case `{case}`" for case in tests["cases"]]
    out += [f"- Capability `{row['capability']}`: `{row['test']}`" for row in tests["capabilities"]]
    out += ["", "## Test AppHost", "",
            f"- Count: {host['count']}", f"- Project: `{host['project']}`",
            f"- IsPackable: {str(host['isPackable']).lower()}",
            f"- IsPublishable: {str(host['isPublishable']).lower()}",
            f"- Repository-only: {str(host['repositoryOnly']).lower()}",
            f"- Counted in authoring cost: {str(host['countedInAuthoringCost']).lower()}",
            f"- Reusable Aspire projects: {host['reusableAspireProjects']}",
            f"- Platform helper: `{host['platformHelper']}`", ""]
    out += ["## Digests", "",
            f"- Inventory: `{bundle['digests']['inventorySha256']}`",
            f"- Signable payload: `{bundle['digests']['signablePayloadSha256']}`", ""]
    return "\n".join(out)


def finalize(signable: dict) -> tuple[str, str]:
    bundle = {**signable, "digests": {"inventorySha256": signable["fixtureInventory"]["sha256"],
                                      "signablePayloadSha256": sha256_bytes(canonical(signable)),
                                      "markdownSha256": ""}}
    markdown = render_markdown(bundle)
    bundle["digests"]["markdownSha256"] = sha256_bytes(markdown.encode("utf-8"))
    return serialize_json(bundle), markdown


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--contract", required=True)
    parser.add_argument("--fixture", required=True)
    parser.add_argument("--test-result", required=True)
    parser.add_argument("--output-json", required=True)
    parser.add_argument("--output-markdown", required=True)
    args = parser.parse_args(argv)
    repository = Path(args.repository)
    try:
        if args.contract.replace("\\", "/") != verifier.CONTRACT_PATH:
            raise Blocked("--contract must be the Story 11.2 contract")
        fixture = args.fixture.replace("\\", "/").rstrip("/")
        signable = build_signable(repository, fixture, args.test_result.replace("\\", "/"))
        json_text, markdown = finalize(signable)
        try:
            import jsonschema  # noqa: PLC0415
            schema = json.loads(verifier.read_file(repository, verifier.PROOF_SCHEMA_PATH))
            jsonschema.Draft202012Validator(schema).validate(json.loads(json_text))
        except ImportError:
            raise Blocked("the jsonschema package is unavailable") from None
        except Exception as error:  # noqa: BLE001 - a schema failure is a blocked environment, not a verdict
            if isinstance(error, Blocked):
                raise
            raise Blocked(f"generated proof failed schema validation: {error}") from None
    except ProofError as error:
        print(f"{error.code}: {error.message}", file=sys.stderr)
        return 1
    except Blocked as error:
        print(f"BLOCKED: {error}", file=sys.stderr)
        return 2
    for relative, text in ((args.output_json, json_text), (args.output_markdown, markdown)):
        target = repository / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        with open(target, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    print("PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
