#!/usr/bin/env python3
"""Verify the Story 11.2 minimal-module fixture (Checkpoint B).

Checks the exact frozen path inventory (V9-MINIMAL-MODULE-FIXTURE-v1), the static project
graph (one non-shipping test AppHost, no reusable Aspire library, ServiceDefaults or
platform-runtime ownership), public-API-only use, restore and build policy, the measured
test result, and the generated fixture proof.

Blocker codes: MINIMAL_FIXTURE_INVENTORY_DRIFT, MINIMAL_FIXTURE_RESTORE_FAILED,
MINIMAL_FIXTURE_BUILD_FAILED, NONPUBLIC_PLATFORM_API_USED, MINIMAL_FIXTURE_TESTS_VACUOUS,
APPHOST_BOUNDARY_INVALID, REUSABLE_ASPIRE_LIBRARY_PRESENT, FIXTURE_PROOF_DRIFT.
Exit 0 PASS, 1 FAIL, 2 BLOCKED.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
import unicodedata
import xml.etree.ElementTree as ElementTree
from pathlib import Path, PurePosixPath

sys.path.insert(0, str(Path(__file__).resolve().parent))

CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/11.2.json"
FIXTURE_ROOT = "tests/fixtures/Hexalith.Conversations.MinimalModule"
INVENTORY_ID = "V9-MINIMAL-MODULE-FIXTURE-v1"
INVENTORY_SHA256 = "4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c"
RESULT_SCHEMA = "hexalith.conversations.minimal-module-fixture-result.v1"
PROOF_SCHEMA_PATH = "docs/release-evidence/thin-module-fixture-proof-v1.schema.json"
PROOF_JSON_PATH = "docs/release-evidence/thin-module-fixture-proof-v1.json"
PROOF_MARKDOWN_PATH = "docs/release-evidence/thin-module-fixture-proof-v1.md"
TEST_RESULT_PATH = "artifacts/v9/11.2/minimal-module.trx"
LOCK_FILE = "packages.lock.json"
COMMAND_RECEIPT_SCHEMA = "hexalith.conversations.minimal-module-command-receipt.v1"
COMMAND_RECEIPTS = {"AC-11.2-02": "artifacts/v9/11.2/AC-11.2-02.log",
                    "AC-11.2-03": "artifacts/v9/11.2/AC-11.2-03.log"}

GUIDANCE_JSON = "docs/release-evidence/thin-authoring-guidance-v2.json"
GUIDANCE_MARKDOWN = "docs/release-evidence/thin-authoring-guidance-v2.md"
STORY_11_1_RECORD_JSON = "docs/release-evidence/story-11.1-final-record-v2.json"
STORY_11_1_RECORD_MARKDOWN = "docs/release-evidence/story-11.1-final-record-v2.md"

FROZEN_RELATIVE = (
    "Directory.Build.props",
    "Hexalith.Conversations.MinimalModule.slnx",
    "README.md",
    "src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj",
    "src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs",
    "src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs",
    "src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj",
    "src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs",
    "src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs",
    "tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj",
    "tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs",
    "tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj",
    "tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs",
)
FROZEN_PATHS = tuple(f"{FIXTURE_ROOT}/{relative}" for relative in FROZEN_RELATIVE)
APPHOST_PROJECT = "tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj"
EXPECTED_PROJECTS = tuple(path for path in FROZEN_RELATIVE if path.endswith(".csproj"))

BLOCK_INVENTORY = "MINIMAL_FIXTURE_INVENTORY_DRIFT"
BLOCK_RESTORE = "MINIMAL_FIXTURE_RESTORE_FAILED"
BLOCK_BUILD = "MINIMAL_FIXTURE_BUILD_FAILED"
BLOCK_NONPUBLIC = "NONPUBLIC_PLATFORM_API_USED"
BLOCK_VACUOUS = "MINIMAL_FIXTURE_TESTS_VACUOUS"
BLOCK_APPHOST = "APPHOST_BOUNDARY_INVALID"
BLOCK_REUSABLE = "REUSABLE_ASPIRE_LIBRARY_PRESENT"
BLOCK_PROOF = "FIXTURE_PROOF_DRIFT"
BLOCK_NOT_DETECTED = "FAULT_NOT_DETECTED"
BLOCK_NOT_RESTORED = "FIXTURE_NOT_RESTORED"
CHECKER_BLOCKERS = (
    BLOCK_INVENTORY, BLOCK_RESTORE, BLOCK_BUILD, BLOCK_NONPUBLIC,
    BLOCK_VACUOUS, BLOCK_APPHOST, BLOCK_REUSABLE, BLOCK_PROOF,
)
ALL_BLOCKERS = CHECKER_BLOCKERS + (BLOCK_NOT_DETECTED, BLOCK_NOT_RESTORED)

# Capability -> test method that must execute and pass (Story 11.2 AC-11.2-04).
CAPABILITY_TESTS = {
    "registration": "Registration_ExposesTheModuleHandlersThroughThePlatformHost",
    "commandHandling": "CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay",
    "eventProduction": "CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay",
    "subscription": "SubscriptionAndPublication_AreServedByPlatformOwnedEndpointsOnly",
    "projection": "Projection_IsDispatchedByThePlatformDispatcher",
    "query": "Query_IsDispatchedByThePlatformDispatcher",
    "publication": "PublishedEvent_IsConsumedThroughThePlatformProcessor",
}

# Fault id -> exact blocker the real checker must yield (Story 11.2 AC-11.2-07 ledger; single source of truth).
FAULT_EXPECTATIONS = {
    "inventory-path-added": BLOCK_INVENTORY,
    "inventory-path-removed": BLOCK_INVENTORY,
    "inventory-path-renamed": BLOCK_INVENTORY,
    "inventory-crlf-edit": BLOCK_INVENTORY,
    "inventory-lock-file-misplaced": BLOCK_INVENTORY,
    "apphost-none": BLOCK_APPHOST,
    "apphost-second": BLOCK_APPHOST,
    "apphost-packable": BLOCK_APPHOST,
    "apphost-publishable": BLOCK_APPHOST,
    "reusable-aspire-library-added": BLOCK_REUSABLE,
    "service-defaults-project-added": BLOCK_REUSABLE,
    "service-defaults-reference-added": BLOCK_REUSABLE,
    "platform-runtime-project-reference-added": BLOCK_REUSABLE,
    "dapr-package-added": BLOCK_REUSABLE,
    "module-authored-endpoint": BLOCK_REUSABLE,
    "internals-visible-to-item": BLOCK_NONPUBLIC,
    "internals-visible-to-attribute": BLOCK_NONPUBLIC,
    "nonpublic-reflection": BLOCK_NONPUBLIC,
    "lock-file-missing": BLOCK_RESTORE,
    "lock-file-policy-disabled": BLOCK_RESTORE,
    "warnings-as-errors-disabled": BLOCK_BUILD,
    "build-output-missing": BLOCK_BUILD,
    "tests-zero": BLOCK_VACUOUS,
    "tests-skipped": BLOCK_VACUOUS,
    "tests-failed": BLOCK_VACUOUS,
    "capability-test-missing": BLOCK_VACUOUS,
    "proof-json-edited": BLOCK_PROOF,
    "proof-markdown-edited": BLOCK_PROOF,
    "result-edited": BLOCK_PROOF,
    "fixture-edited-after-proof": BLOCK_PROOF,
}

NONPUBLIC_PATTERNS = (
    re.compile(r"InternalsVisibleTo"),
    re.compile(r"BindingFlags\s*\.\s*NonPublic"),
    re.compile(r"UnsafeAccessor"),
)
PLATFORM_RUNTIME_SOURCE = re.compile(
    r"AddServiceDefaults|AddHexalithServiceDefaults|MapDefaultEndpoints|MapHealthChecks|AddHealthChecks"
    r"|AddOpenTelemetry|DaprClient|PublishEventAsync|MapSubscribeHandler|\.MapGet\(|\.MapPost\(|WithDaprSidecar"
)
PLATFORM_RUNTIME_PROJECT = re.compile(
    r"ServiceDefaults|Hexalith\.EventStore\.(Server|Gateway|Admin|Operations|SignalR|PayloadProtection)\b"
)
PLATFORM_RUNTIME_PACKAGE = re.compile(r"^(Dapr\.|OpenTelemetry|Microsoft\.Extensions\.Diagnostics\.HealthChecks)")
ASPIRE_PACKAGE = re.compile(r"^(Aspire\.|CommunityToolkit\.Aspire)")
ASPIRE_NAME = re.compile(r"Aspire|ServiceDefaults", re.I)


class Blocked(Exception):
    """The check could not run (missing input or tooling); never a content verdict."""


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical(value) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


def read_file(repository: Path, relative: str) -> bytes:
    try:
        return (repository / relative).read_bytes()
    except OSError as error:
        raise Blocked(f"cannot read {relative}: {error}") from None


def inventory_digest(paths) -> str:
    return sha256_bytes(("\n".join(paths) + "\n").encode("utf-8"))


def is_git_repository(repository: Path) -> bool:
    return (repository / ".git").exists()


def git_tracked(repository: Path, paths) -> set[str]:
    result = subprocess.run(["git", "ls-files", "-z", "--", *paths], cwd=repository,
                            capture_output=True, check=False)
    if result.returncode:
        raise Blocked("git ls-files failed")
    return {item for item in result.stdout.decode("utf-8").split("\0") if item}


def git_head(repository: Path) -> str | None:
    result = subprocess.run(["git", "rev-parse", "HEAD"], cwd=repository, capture_output=True, text=True, check=False)
    return result.stdout.strip() if result.returncode == 0 else None


def resolve_commit(repository: Path, revision: str) -> str:
    """Resolve a supplied candidate revision to its full commit id."""
    result = subprocess.run(["git", "rev-parse", "--verify", "--quiet", f"{revision}^{{commit}}"], cwd=repository,
                            capture_output=True, text=True, check=False)
    if result.returncode or not result.stdout.strip():
        raise Blocked(f"the candidate is not a commit: {revision}")
    return result.stdout.strip()


def eventstore_gitlink(repository: Path, commit: str) -> dict:
    """Read the platform identity from the candidate tree, without opening the submodule."""
    path = "references/Hexalith.EventStore"
    result = subprocess.run(["git", "ls-tree", commit, "--", path], cwd=repository,
                            capture_output=True, text=True, check=False)
    parts = result.stdout.strip().split()
    if result.returncode or len(parts) != 4 or parts[:2] != ["160000", "commit"] or parts[3] != path:
        raise Blocked("the candidate has no EventStore gitlink")
    return {"path": path, "mode": parts[0], "commit": parts[2]}


def check_platform_checkout(repository: Path, candidate: str | None = None) -> list[dict]:
    """Require the measured platform sources to be the clean checkout pinned by the candidate."""
    candidate = candidate or git_head(repository)
    if candidate is None:
        return [{"code": BLOCK_PROOF, "message": "the platform checkout requires a committed candidate"}]
    try:
        expected = eventstore_gitlink(repository, candidate)
    except Blocked as error:
        return [{"code": BLOCK_PROOF, "message": str(error)}]
    root = repository / expected["path"]
    if not root.is_dir():
        return [{"code": BLOCK_PROOF, "message": "the EventStore checkout is unavailable"}]
    top = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=root,
                         capture_output=True, text=True, check=False)
    if top.returncode or Path(top.stdout.strip()).resolve() != root.resolve():
        return [{"code": BLOCK_PROOF, "message": "the EventStore checkout is not initialized at the platform root"}]
    if git_head(root) != expected["commit"]:
        return [{"code": BLOCK_PROOF, "message": "the EventStore checkout HEAD differs from the candidate gitlink"}]
    # Untracked sources count too: SDK-style projects compile every *.cs file under their folder.
    status = subprocess.run(["git", "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=none"],
                            cwd=root, capture_output=True, text=True, check=False)
    if status.returncode or status.stdout.strip():
        return [{"code": BLOCK_PROOF, "message": "the EventStore checkout has uncommitted source changes"}]
    return []


def command_receipt(repository: Path, scenario: str, content: bytes | None = None) -> dict:
    """Validate the measured exit and policy for an exact frozen restore/build command."""
    path = COMMAND_RECEIPTS[scenario]
    raw = read_file(repository, path) if content is None else content
    contract = json.loads(read_file(repository, CONTRACT_PATH))
    command = next(row["command"] for row in contract["scenarios"] if row["id"] == scenario)
    try:
        receipt = json.loads(raw)
        policy = "lockedMode" if scenario == "AC-11.2-02" else "warningsAsErrors"
        if (set(receipt) != {"schemaVersion", "scenarioId", "command", "exitCode", "consoleLog", policy}
                or receipt["schemaVersion"] != COMMAND_RECEIPT_SCHEMA or receipt["scenarioId"] != scenario
                or receipt["command"] != command or type(receipt["exitCode"]) is not int
                or receipt["exitCode"] != 0 or receipt[policy] is not True
                or not isinstance(receipt["consoleLog"], str) or not receipt["consoleLog"].strip()):
            raise ValueError("receipt is not a successful execution of the frozen command and policy")
    except (KeyError, TypeError, ValueError) as error:
        raise ValueError(f"{scenario} measured receipt is invalid: {error}") from None
    return {**receipt, "path": path, "sha256": sha256_bytes(raw)}


def fixture_files(root: Path) -> list[str]:
    """Every non-generated file under the fixture root as sorted POSIX paths relative to it."""
    found: list[str] = []
    if not root.is_dir():
        return found
    for path in sorted(root.rglob("*")):
        relative = path.relative_to(root)
        if any(part in ("bin", "obj") for part in relative.parts):
            continue
        if path.is_symlink() or path.is_file():
            found.append(relative.as_posix())
    return sorted(found)


def lock_files(root: Path) -> list[str]:
    return [item for item in fixture_files(root) if PurePosixPath(item).name == LOCK_FILE]


def check_inventory(repository: Path, fixture: str) -> tuple[list[dict], dict]:
    """Exact frozen set, containment, source control, hand-authored and LF/NFC form."""
    problems: list[str] = []
    root = repository / fixture
    actual = fixture_files(root)
    locks = [item for item in actual if PurePosixPath(item).name == LOCK_FILE]
    project_dirs = {str(PurePosixPath(item).parent) for item in FROZEN_RELATIVE if item.endswith(".csproj")}
    stray_locks = [item for item in locks if str(PurePosixPath(item).parent) not in project_dirs]
    non_lock = [item for item in actual if item not in locks]
    if fixture != FIXTURE_ROOT:
        problems.append("fixture root differs from the frozen root")
    exact = set(non_lock) == set(FROZEN_RELATIVE) and len(non_lock) == len(FROZEN_RELATIVE)
    if not exact:
        problems.append("path set differs from the frozen inventory")
    if stray_locks:
        problems.append("lock file outside a project directory")
    # The frozen digest is over the contract's listed order, not a re-sorted order.
    digest = inventory_digest(f"{fixture}/{item}" for item in (FROZEN_RELATIVE if exact else non_lock))
    if digest != INVENTORY_SHA256:
        problems.append("inventory digest differs")

    files: list[dict] = []
    tracked: set[str] | None = None
    if is_git_repository(repository):
        tracked = git_tracked(repository, [f"{fixture}/{item}" for item in FROZEN_RELATIVE])
    for relative in FROZEN_RELATIVE:
        path = root / relative
        full = f"{fixture}/{relative}"
        ancestors = [repository / parent for parent in PurePosixPath(full).parents if parent != PurePosixPath(".")]
        if path.is_symlink() or any(parent.is_symlink() for parent in ancestors) or not path.is_file():
            problems.append(f"{relative} is not a regular non-symlink file")
            continue
        try:
            path.resolve(strict=True).relative_to(root.resolve(strict=True))
        except (OSError, ValueError):
            problems.append(f"{relative} is not contained in the fixture")
            continue
        data = path.read_bytes()
        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            problems.append(f"{relative} is not UTF-8")
            continue
        if b"\r" in data:
            problems.append(f"{relative} is not LF-normalized")
        if not unicodedata.is_normalized("NFC", text):
            problems.append(f"{relative} is not NFC")
        if re.search(r"auto-generated|<autogenerated", text, re.I):
            problems.append(f"{relative} is not hand-authored")
        if tracked is not None and full not in tracked:
            problems.append(f"{relative} is not source-controlled")
        files.append({"path": full, "sha256": sha256_bytes(data), "bytes": len(data),
                      "lines": text.count("\n") + (0 if text.endswith("\n") or not text else 1)})
    if [row["path"] for row in files] != list(FROZEN_PATHS):
        problems.append("evaluated rows differ from the frozen inventory")
    blockers = [{"code": BLOCK_INVENTORY, "message": "; ".join(sorted(set(problems)))}] if problems else []
    facts = {
        "id": INVENTORY_ID,
        "sha256": INVENTORY_SHA256,
        "paths": files,
        "lockFiles": [{"path": f"{fixture}/{item}", "sha256": sha256_bytes((root / item).read_bytes())}
                      for item in locks if (root / item).is_file()],
    }
    return blockers, facts


def parse_properties(path: Path) -> dict[str, str]:
    """Unconditional property values declared in an MSBuild file (later declarations win)."""
    values: dict[str, str] = {}
    try:
        tree = ElementTree.parse(path)
    except (OSError, ElementTree.ParseError):
        return values
    for group in tree.getroot().iter("PropertyGroup"):
        if group.get("Condition"):
            continue
        for child in group:
            if not child.get("Condition"):
                values[child.tag] = (child.text or "").strip()
    return values


def parse_project(path: Path) -> dict:
    try:
        root = ElementTree.parse(path).getroot()
    except (OSError, ElementTree.ParseError):
        raise Blocked(f"cannot parse {path.name}") from None
    return {
        "sdk": root.get("Sdk", ""),
        "properties": parse_properties(path),
        "projectReferences": [item.get("Include", "") for item in root.iter("ProjectReference")],
        "packageReferences": [item.get("Include", "") for item in root.iter("PackageReference")],
        "internalsVisibleTo": [item.get("Include", "") for item in root.iter("InternalsVisibleTo")],
        "rawText": path.read_text(encoding="utf-8", errors="replace"),
    }


def effective(project: dict, defaults: dict[str, str], name: str) -> str:
    return project["properties"].get(name, defaults.get(name, "")).lower()


def graph_facts(repository: Path, fixture: str) -> dict:
    """Static project-graph facts of the fixture: projects, AppHost, references and policy."""
    root = repository / fixture
    defaults = parse_properties(root / "Directory.Build.props")
    solution_projects = []
    try:
        solution = ElementTree.parse(root / "Hexalith.Conversations.MinimalModule.slnx").getroot()
        if solution.tag == "Solution":
            entries = solution.findall("Project") + solution.findall("Folder/Project")
            solution_projects = sorted(item.get("Path", "") for item in entries)
    except (OSError, ElementTree.ParseError, LookupError):
        pass
    project_paths = [relative for relative in fixture_files(root) if relative.endswith(".csproj")]
    projects = {}
    for relative in project_paths:
        try:
            projects[relative] = parse_project(root / relative)
        except (Blocked, LookupError):
            if relative in EXPECTED_PROJECTS:
                raise
    apphosts = [name for name, project in projects.items()
                if project["sdk"].startswith("Aspire.AppHost") or PurePosixPath(name).stem.endswith(".AppHost")]
    return {
        "defaults": defaults,
        "projects": projects,
        "projectPaths": project_paths,
        "appHosts": apphosts,
        "solutionProjects": solution_projects,
    }


def source_files(repository: Path, fixture: str) -> dict[str, str]:
    root = repository / fixture
    sources = {}
    for relative in fixture_files(root):
        if relative.endswith((".cs", ".csproj", ".props")):
            sources[relative] = (root / relative).read_text(encoding="utf-8", errors="replace")
    return sources


def check_graph(repository: Path, fixture: str) -> tuple[list[dict], dict]:
    """AppHost boundary, reusable Aspire/ServiceDefaults/platform-runtime absence, public-API-only use,
    and restore/build policy."""
    facts = graph_facts(repository, fixture)
    defaults, projects, apphosts = facts["defaults"], facts["projects"], facts["appHosts"]
    problems: dict[str, list[str]] = {code: [] for code in CHECKER_BLOCKERS}

    # AppHost boundary: exactly one, repository-only, non-packable, non-publishable.
    if len(apphosts) != 1:
        problems[BLOCK_APPHOST].append(f"{len(apphosts)} test AppHost projects (exactly one required)")
    for name in apphosts:
        project = projects[name]
        if effective(project, defaults, "IsPackable") != "false":
            problems[BLOCK_APPHOST].append(f"{name} is packable")
        if effective(project, defaults, "IsPublishable") != "false":
            problems[BLOCK_APPHOST].append(f"{name} is publishable")
        if not name.startswith("tests/"):
            problems[BLOCK_APPHOST].append(f"{name} is not repository-only (outside tests/)")
        if any(PLATFORM_RUNTIME_PROJECT.search(ref) and "ServiceDefaults" in ref for ref in project["projectReferences"]):
            problems[BLOCK_REUSABLE].append(f"{name} references ServiceDefaults")

    # Reusable Aspire / ServiceDefaults / platform runtime ownership by the module.
    for name, project in projects.items():
        if name in apphosts:
            continue
        stem = PurePosixPath(name).stem
        if ASPIRE_NAME.search(stem) or project["sdk"].startswith("Aspire"):
            problems[BLOCK_REUSABLE].append(f"{name} is a reusable Aspire or ServiceDefaults project")
        for reference in project["projectReferences"]:
            normalized = reference.replace("\\", "/")
            if PLATFORM_RUNTIME_PROJECT.search(normalized):
                problems[BLOCK_REUSABLE].append(f"{name} references platform runtime project {normalized.rsplit('/', 1)[-1]}")
            elif re.search(r"Hexalith\.EventStore\.Aspire", normalized):
                problems[BLOCK_REUSABLE].append(f"{name} references the platform Aspire helper outside the test AppHost")
        for package in project["packageReferences"]:
            if PLATFORM_RUNTIME_PACKAGE.search(package) or ASPIRE_PACKAGE.search(package):
                problems[BLOCK_REUSABLE].append(f"{name} references platform runtime package {package}")
    if facts["projectPaths"] != sorted(EXPECTED_PROJECTS) or facts["solutionProjects"] != sorted(EXPECTED_PROJECTS):
        problems[BLOCK_APPHOST].append("project files and solution entries must each equal the four frozen projects")
        extra = sorted(set(facts["projectPaths"]) - set(EXPECTED_PROJECTS))
        if any(ASPIRE_NAME.search(PurePosixPath(item).stem) for item in extra):
            problems[BLOCK_REUSABLE].append("an additional Aspire or ServiceDefaults project exists")

    # Source scans: internal-API use, and platform-owned runtime authored by the module.
    for name, text in source_files(repository, fixture).items():
        if any(pattern.search(text) for pattern in NONPUBLIC_PATTERNS):
            problems[BLOCK_NONPUBLIC].append(f"{name} uses a non-public platform API")
        if name.endswith(".cs") and "/AppHost" not in name and PLATFORM_RUNTIME_SOURCE.search(text):
            problems[BLOCK_REUSABLE].append(f"{name} authors a platform-owned runtime capability")

    # Restore and build policy (the measured dotnet commands enforce these; a static gate keeps the bytes honest).
    if defaults.get("RestorePackagesWithLockFile", "").lower() != "true":
        problems[BLOCK_RESTORE].append("RestorePackagesWithLockFile is not enabled")
    root = repository / fixture
    for relative in EXPECTED_PROJECTS:
        if relative in projects and not (root / PurePosixPath(relative).parent / LOCK_FILE).is_file():
            problems[BLOCK_RESTORE].append(f"{relative} has no {LOCK_FILE}")
    if defaults.get("TreatWarningsAsErrors", "").lower() != "true" or any(
            project["properties"].get("TreatWarningsAsErrors", "true").lower() != "true" for project in projects.values()):
        problems[BLOCK_BUILD].append("warnings-as-errors is not enforced")

    blockers = [{"code": code, "message": "; ".join(sorted(set(items)))} for code, items in problems.items() if items]
    appHost = projects[apphosts[0]] if len(apphosts) == 1 else None
    summary = {
        "count": len(apphosts),
        "project": f"{fixture}/{apphosts[0]}" if apphosts else None,
        "isPackable": effective(appHost, defaults, "IsPackable") == "true" if appHost else None,
        "isPublishable": effective(appHost, defaults, "IsPublishable") == "true" if appHost else None,
        "repositoryOnly": bool(apphosts) and all(name.startswith("tests/") for name in apphosts),
        "reusableAspireProjects": len([item for item in blockers if item["code"] == BLOCK_REUSABLE]),
        "projects": sorted(projects),
    }
    return blockers, {"appHost": summary, "graph": facts}


def parse_trx(content: bytes) -> dict:
    """Outcome counts and per-test results of a TRX file."""
    try:
        root = ElementTree.fromstring(content)
    except ElementTree.ParseError:
        raise Blocked("the test result is not parseable TRX") from None
    namespace = root.tag.split("}")[0] + "}" if root.tag.startswith("{") else ""
    results = []
    for item in root.iter(f"{namespace}UnitTestResult"):
        results.append({"test": item.get("testName", ""), "outcome": item.get("outcome", "")})
    counts = {"total": len(results), "passed": 0, "failed": 0, "skipped": 0, "notRun": 0}
    for item in results:
        outcome = item["outcome"]
        if outcome == "Passed":
            counts["passed"] += 1
        elif outcome == "Failed":
            counts["failed"] += 1
        elif outcome == "NotExecuted":
            counts["skipped"] += 1
        else:
            counts["notRun"] += 1
    return {"counts": counts, "results": results}


def check_tests(content: bytes) -> tuple[list[dict], dict]:
    parsed = parse_trx(content)
    counts, results = parsed["counts"], parsed["results"]
    problems = []
    if counts["total"] == 0:
        problems.append("zero tests executed")
    if counts["failed"] or counts["skipped"] or counts["notRun"]:
        problems.append("tests failed, were skipped or were not run")
    passed = {item["test"].rsplit(".", 1)[-1] for item in results if item["outcome"] == "Passed"}
    missing = sorted(name for name in set(CAPABILITY_TESTS.values()) if name not in passed)
    if missing and counts["total"]:
        problems.append("capability tests did not pass: " + ", ".join(missing))
    blockers = [{"code": BLOCK_VACUOUS, "message": "; ".join(problems)}] if problems else []
    capabilities = {key: CAPABILITY_TESTS[key] for key in CAPABILITY_TESTS if CAPABILITY_TESTS[key] in passed}
    return blockers, {"counts": counts, "cases": sorted(item["test"].rsplit(".", 1)[-1] for item in results),
                      "capabilities": capabilities}


def check_predecessor(repository: Path) -> dict:
    """Story 11.1: recompute the final-record v2 and guidance digests; mismatch is a blocked binding."""
    import verify_thin_authoring_guidance as guidance  # noqa: PLC0415

    bundle_json = read_file(repository, GUIDANCE_JSON)
    bundle_md = read_file(repository, GUIDANCE_MARKDOWN)
    record_json = read_file(repository, STORY_11_1_RECORD_JSON)
    record_md = read_file(repository, STORY_11_1_RECORD_MARKDOWN)
    try:
        bundle = json.loads(bundle_json)
        record = json.loads(record_json)
        facts = record["thinGuidanceExecution"]["guidance"]
        expected = (facts["jsonSha256"], facts["markdownSha256"], facts["signablePayloadSha256"])
        actual = (sha256_bytes(bundle_json), sha256_bytes(bundle_md), bundle["digests"]["signablePayloadSha256"])
        if record["storyId"] != "11.1" or expected != actual:
            raise Blocked("the Story 11.1 record does not bind the committed guidance bundle")
    except (ValueError, KeyError, TypeError):
        raise Blocked("the Story 11.1 record or guidance bundle is malformed") from None
    problems = guidance.verify(repository, GUIDANCE_JSON, GUIDANCE_MARKDOWN, True)
    if problems:
        raise Blocked("the guidance bundle does not verify: " + ", ".join(item["code"] for item in problems))
    if is_git_repository(repository):
        import generate_story_record as records  # noqa: PLC0415

        head = git_head(repository)
        _, validators = records.v2_load_schemas()
        if head is None or records.v2_verified_predecessor(
                repository, head, "11.1", STORY_11_1_RECORD_JSON, STORY_11_1_RECORD_MARKDOWN,
                validators["record"]) != sha256_bytes(record_json):
            raise Blocked("the committed Story 11.1 final-record v2 pair does not verify")
    return {
        "recordPath": STORY_11_1_RECORD_JSON,
        "recordSha256": sha256_bytes(record_json),
        "recordMarkdownSha256": sha256_bytes(record_md),
        "bundleJsonSha256": actual[0],
        "bundleMarkdownSha256": actual[1],
        "signablePayloadSha256": actual[2],
        "sourceSha256": bundle["source"]["sha256"],
    }


def verify(repository: Path, fixture: str, inventory_only: bool, test_result: str | None = None,
           candidate_commit: str | None = None) -> tuple[list[dict], dict]:
    blockers, inventory = check_inventory(repository, fixture)
    facts: dict = {"inventory": inventory, "predecessor": check_predecessor(repository)}
    head = git_head(repository) if is_git_repository(repository) else None
    facts["candidate"] = {"commit": candidate_commit or head}
    if not inventory_only:
        blockers += check_platform_checkout(repository, candidate_commit or head)
        graph_blockers, graph = check_graph(repository, fixture)
        blockers += graph_blockers
        facts.update(graph)
        if test_result:
            test_blockers, tests = check_tests(read_file(repository, test_result))
            blockers += test_blockers
            facts["tests"] = tests
    return blockers, facts


def verify_proof(repository: Path, fixture: str, proof_json: str, proof_markdown: str, test_result: str,
                 rederive: bool = True, candidate_commit: str | None = None) -> list[dict]:
    """The committed proof must equal a fresh deterministic derivation from the current fixture and result.

    With ``rederive`` false the build-output hashes are not recomputed; the proof is still checked for schema,
    canonical bytes, Markdown parity, digest and its fixture, guidance, contract and result bindings."""
    import generate_minimal_module_fixture_proof as proof  # noqa: PLC0415

    raw = read_file(repository, proof_json)
    markdown = read_file(repository, proof_markdown)
    problems = []
    try:
        committed = json.loads(raw)
    except ValueError:
        return [{"code": BLOCK_PROOF, "message": "the proof JSON is not parseable"}]
    try:
        import jsonschema  # noqa: PLC0415
        schema = json.loads(read_file(repository, PROOF_SCHEMA_PATH))
        if list(jsonschema.Draft202012Validator(schema).iter_errors(committed)):
            problems.append("the proof does not satisfy its closed schema")
    except ImportError:
        raise Blocked("the jsonschema package is unavailable") from None
    if raw.decode("utf-8") != proof.serialize_json(committed):
        problems.append("the proof JSON bytes are not canonical")
    try:
        candidate = candidate_commit or git_head(repository)
        platform_blockers = check_platform_checkout(repository, candidate)
        if platform_blockers:
            return platform_blockers
        if (committed["candidate"]["commit"] != candidate
                or committed["candidate"]["eventStoreGitlink"] != eventstore_gitlink(repository, candidate)):
            problems.append("the proof candidate or EventStore gitlink differs")
        if markdown != proof.render_markdown(committed).encode("utf-8"):
            problems.append("the proof Markdown differs from its JSON rendering")
        signable = proof.signable_of(committed)
        if committed["digests"]["signablePayloadSha256"] != sha256_bytes(canonical(signable)):
            problems.append("the signable-payload digest differs")
        if committed["digests"]["markdownSha256"] != sha256_bytes(markdown):
            problems.append("the Markdown digest differs")
        if rederive:
            fresh = proof.build_signable(repository, fixture, test_result, candidate_commit=candidate)
            if fresh != signable:
                problems.append("the proof differs from the current fixture, result, or guidance")
        else:
            inventory_blockers, inventory = check_inventory(repository, fixture)
            predecessor = check_predecessor(repository)
            guidance_facts = committed["guidanceDigest"]
            _, graph = check_graph(repository, fixture)
            projects = [{"name": PurePosixPath(path).stem, "path": path,
                         "sha256": sha256_bytes(read_file(repository, path))}
                        for path in proof.resolve_platform_closure(repository, fixture, graph["graph"]["projects"])]
            packages = set()
            locks = []
            for row in inventory["lockFiles"]:
                resolved = proof.lock_packages(read_file(repository, row["path"]))
                packages.update(resolved)
                locks.append({**row, "packageCount": len(resolved)})
            contract = json.loads(read_file(repository, CONTRACT_PATH))
            commands = {section: next(row["command"] for row in contract["scenarios"] if row["id"] == scenario)
                        for section, scenario in proof.COMMAND_SCENARIOS.items()}
            if any(committed[section]["command"] != command for section, command in commands.items()):
                problems.append("the proof execution commands differ from the frozen contract")
            test_blockers, tests = check_tests(read_file(repository, test_result))
            xunit = next((version for name, version, _ in sorted(packages) if name == "xunit.v3"), "")
            expected_tests = {
                "command": commands["tests"], "toolVersions": {"xunitV3": xunit},
                "resultFile": {"path": test_result, "sha256": sha256_bytes(read_file(repository, test_result))},
                **tests["counts"], "cases": tests["cases"],
                "capabilities": [{"capability": key, "test": tests["capabilities"][key]}
                                 for key in CAPABILITY_TESTS if key in tests["capabilities"]],
            }
            if test_blockers or committed["tests"] != expected_tests:
                problems.append("the proof test facts differ from the retained TRX or lock files")
            for section, scenario in (("restore", "AC-11.2-02"), ("build", "AC-11.2-03")):
                measured = command_receipt(repository, scenario)
                if committed[section]["receipt"] != {key: measured[key] for key in ("path", "sha256", "exitCode")}:
                    problems.append(f"the proof differs from the measured {section} receipt")
            if (inventory_blockers or committed["fixtureInventory"]["paths"] != inventory["paths"]
                    or committed["restore"]["lockFiles"] != sorted(locks, key=lambda row: row["path"])
                    or committed["restore"]["resolvedPackagesSha256"] != sha256_bytes(canonical(sorted(packages)))
                    or committed["restore"]["packageCount"] != len(packages)
                    or committed["restore"]["sources"] != proof.restore_sources(repository, fixture, packages)
                    or committed["resolvedPublicApis"]["projects"] != projects
                    or committed["build"]["toolVersions"]["aspireAppHostSdk"] != graph["graph"]["projects"][APPHOST_PROJECT]["sdk"]
                    or guidance_facts["bundleMarkdownSha256"] != predecessor["bundleMarkdownSha256"]
                    or guidance_facts["sourceSha256"] != predecessor["sourceSha256"]
                    or guidance_facts["bundleJsonSha256"] != predecessor["bundleJsonSha256"]
                    or guidance_facts["signablePayloadSha256"] != predecessor["signablePayloadSha256"]
                    or guidance_facts["story11_1RecordSha256"] != predecessor["recordSha256"]
                    or committed["tests"]["resultFile"]["sha256"] != sha256_bytes(read_file(repository, test_result))
                    or committed["candidate"]["contractSha256"] != sha256_bytes(read_file(repository, CONTRACT_PATH))):
                problems.append("the proof differs from the current fixture, result, or guidance")
    except (KeyError, TypeError, ValueError):
        problems.append("the proof is malformed")
    except proof.ProofError as error:
        problems.append(f"the proof cannot be re-derived: {error.message}")
    return [{"code": BLOCK_PROOF, "message": "; ".join(problems)}] if problems else []


def write_report(repository: Path, output: str | None, blockers: list[dict], result: str, facts: dict | None) -> None:
    if not output:
        return
    report = {"schemaVersion": RESULT_SCHEMA, "result": result, "blockers": blockers}
    if facts is not None and result == "PASS":
        report["inventorySha256"] = facts["inventory"]["sha256"]
        report["fixtureFileCount"] = len(facts["inventory"]["paths"])
    target = repository / output
    target.parent.mkdir(parents=True, exist_ok=True)
    with open(target, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(report, indent=2) + "\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--contract", required=True)
    parser.add_argument("--fixture", required=True)
    parser.add_argument("--inventory-only", action="store_true")
    parser.add_argument("--test-result")
    parser.add_argument("--proof-json")
    parser.add_argument("--proof-markdown")
    parser.add_argument("--candidate", help="committed candidate the proof binds (default: HEAD)")
    parser.add_argument("--output")
    args = parser.parse_args(argv)
    repository = Path(args.repository)
    facts = None
    try:
        if args.contract.replace("\\", "/") != CONTRACT_PATH:
            raise Blocked("--contract must be the Story 11.2 contract")
        try:
            contract = json.loads(read_file(repository, args.contract))
            if contract["storyId"] != "11.2":
                raise ValueError("storyId")
        except (ValueError, KeyError, TypeError):
            raise Blocked("story contract is not a valid Story 11.2 contract") from None
        fixture = args.fixture.replace("\\", "/").rstrip("/")
        candidate = resolve_commit(repository, args.candidate) if args.candidate else None
        blockers, facts = verify(repository, fixture, args.inventory_only, args.test_result, candidate_commit=candidate)
        if args.proof_json:
            blockers += verify_proof(repository, fixture, args.proof_json, args.proof_markdown or PROOF_MARKDOWN_PATH,
                                     args.test_result or TEST_RESULT_PATH, candidate_commit=candidate)
        result, code = ("FAIL", 1) if blockers else ("PASS", 0)
    except Blocked as error:
        blockers, result, code = [{"code": "BLOCKED", "message": str(error)}], "BLOCKED", 2
    write_report(repository, args.output, blockers, result, facts)
    for blocker in blockers:
        print(f"{blocker['code']}: {blocker['message']}", file=sys.stderr)
    print(result)
    return code


if __name__ == "__main__":
    sys.exit(main())
