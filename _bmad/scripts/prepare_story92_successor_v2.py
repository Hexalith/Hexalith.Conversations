#!/usr/bin/env python3
"""Prepare a pending Story 9.2 successor for the exact authorized v2 scope.

This additive route leaves the v1 preparer and every accepted artifact unchanged.
It accepts a clean detached source checkout and writes only a new versioned packet.
"""

from __future__ import annotations

import importlib.util
import json
import os
import subprocess
from pathlib import Path
from typing import Any

import jsonschema


ROOT = Path(__file__).resolve().parents[2]
V1_PATH = Path(__file__).with_name("prepare_story92_successor.py")
SPEC = importlib.util.spec_from_file_location("story92_preparer_v1", V1_PATH)
assert SPEC and SPEC.loader
V1 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(V1)

AUTH = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-authorization-v2.json"
SCOPE = "_bmad-output/planning-artifacts/v9/story-9.2-current-main-scope-proposal-v2.json"
SCHEMA = "_bmad/schemas/story-9.2-current-main-successor-proposal-v2.schema.json"
JSON_NAME = "conformance-oracle-tiering-migration-v5-proposal.json"
MARKDOWN_NAME = "conformance-oracle-tiering-migration-review-v5.md"
BUILD_NAME = "build-evidence-v2.json"
API_EVIDENCE = "docs/release-evidence/conformance-oracle-tiering-eventstore-released-api-diff-v1.json"
API_EVIDENCE_SHA = "6a5e5d51f9ec9e5f5cd144b83b25a55547c3231110693e224d6db84fad978b7e"
API_COMPAT_EVIDENCE = "docs/release-evidence/conformance-oracle-tiering-eventstore-released-api-compat-v1.json"
API_COMPAT_EVIDENCE_SHA = "ebe059f6e4d0213ad29fe1884f384d685beea4262bb1dafdf53ab71f8f2e302d"
CANDIDATE = "ec5c9be52631b96793ab028febfbbfba4d362d48"
PRIOR = "59e72b82cdc2ecc58971e34594c4e8b62896518b"
AUTH_SHA = "f5391b2f2d45023f8fc548a2ecbe5db18ce42380b4ffafa473f9dcf29df51438"
SCOPE_SHA = "ea902b33ed0d35ad44922a7ba63b14e23b65058bcb3a47a11357c16d3f1fd083"
SCOPE_MATERIAL_SHA = "925cf11a819e9412055f3d35ea85a0a4334f9371e7d1decdb8199bb7b6757672"
V4_SHA = "8aea6446a2f10d308284580cbc11ab4d90268ef2bc69e7e140f6d9300a4eb95d"
V4_REVIEW_SHA = "57ee42ba8c45a4e09414d4f94ce13c68cf57413f884f738e585681caa19b6665"
VERSION = "hexalith.conversations.story-9.2-current-main-successor-proposal.v2"


def require(condition: Any, code: str, message: str) -> None:
    V1.require(condition, code, message)


def authority(root: Path) -> tuple[dict[str, Any], dict[str, Any]]:
    authorization_bytes, scope_bytes = V1.read(root, AUTH), V1.read(root, SCOPE)
    require(V1.sha(authorization_bytes) == AUTH_SHA, "SUCCESSOR_AUTHORIZATION_INVALID", "v2 authorization bytes differ")
    require(V1.sha(scope_bytes) == SCOPE_SHA, "SUCCESSOR_SCOPE_INVALID", "v2 scope bytes differ")
    authorization, scope = V1.parse(authorization_bytes), V1.parse(scope_bytes)
    require(authorization == {
        "schemaVersion": "hexalith.conversations.story-9.2-current-main-scope-authorization.v2",
        "storyId": "9.2", "status": "authorized",
        "authorization": authorization.get("authorization"),
        "proposal": {"path": SCOPE, "sha256": SCOPE_SHA, "proposalSha256": SCOPE_MATERIAL_SHA},
        "measuredCommittedCandidate": CANDIDATE, "priorAuthorizedCandidate": PRIOR,
        "originalBaseline": V1.PINS["baseline"], "workingTreeChangesIncluded": False,
        "qualityDecisionIncluded": False, "preserveOriginalBaselineFrozenContractsApprovalsAndAcceptedRecords": True,
    } and authorization["authorization"]["actor"] == "user"
            and authorization["authorization"]["statement"] == "yes",
            "SUCCESSOR_AUTHORIZATION_INVALID", "v2 decision is not the closed preparation-only authorization")
    require(V1.sha(V1.canonical({key: value for key, value in scope.items() if key != "proposalSha256"}))
            == scope.get("proposalSha256") == SCOPE_MATERIAL_SHA
            and scope.get("schemaVersion") == "hexalith.conversations.story-9.2-current-main-scope-proposal.v2"
            and scope.get("status") == "prepared-unapproved"
            and scope.get("authorizationClaimed") is False
            and scope.get("qualityApprovalClaimed") is False
            and scope.get("workingTreeChangesIncluded") is False
            and scope.get("measuredCommittedCandidate") == CANDIDATE
            and scope.get("priorAuthorizedCandidate") == PRIOR
            and scope.get("originalBaseline") == V1.PINS["baseline"]
            and scope.get("priorScopeProposal") == {"path": V1.SCOPE_PATH, "sha256": V1.PINS["scopeSha256"]}
            and scope.get("priorScopeAuthorization") == {"path": V1.AUTHORIZATION_PATH,
                                                          "sha256": V1.PINS["authorizationSha256"]}
            and scope.get("requestedScopeDecision", {}).get("qualityDecisionIncluded") is False,
            "SUCCESSOR_SCOPE_INVALID", "v2 scope identity or material differs")
    return authorization, scope


def with_sizes(root: Path, rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    result = []
    for row in rows:
        measured = {"path": row["path"], "status": row["status"]}
        for key in ("before", "after"):
            entry = row[key]
            measured[key] = ({**entry, "sizeBytes": int(V1.git(root, "cat-file", "-s", entry["object"]))}
                             if entry and entry["type"] == "blob" else entry)
        result.append(measured)
    return result


def validate_source(source: Path, scope: dict[str, Any], authority_root: Path) -> dict[str, Any]:
    require(V1.git(source, "rev-parse", "--show-toplevel").decode().strip() == str(source)
            and V1.git(source, "rev-parse", "HEAD^{commit}").decode().strip() == CANDIDATE,
            "SUCCESSOR_CANDIDATE_INVALID", "use an isolated checkout of the exact approved commit")
    require(not V1.git(source, "diff", "--cached", "HEAD", "--name-only", "--ignore-submodules=all")
            and not V1.git(source, "ls-files", "--others", "--exclude-standard", "-z"),
            "SUCCESSOR_SOURCE_DIRTY", "source contains excluded tracked or untracked edits")
    require(V1.git(source, "merge-base", PRIOR, CANDIDATE).decode().strip() == PRIOR,
            "SUCCESSOR_SCOPE_DRIFT", "prior authorized candidate is not an ancestor")
    actual_changes = V1.changes(source, PRIOR, CANDIDATE)
    require(with_sizes(source, actual_changes) == scope["changesSincePriorAuthorizedCandidate"],
            "SUCCESSOR_SCOPE_DRIFT", "approved path and gitlink changes differ from Git")
    expected_commits = scope["interveningCommits"]
    actual_commits = []
    for revision in V1.git(source, "rev-list", "--reverse", f"{PRIOR}..{CANDIDATE}").decode().splitlines():
        parents = V1.git(source, "rev-list", "--parents", "-n", "1", revision).decode().split()[1:]
        require(len(parents) == 1, "SUCCESSOR_SCOPE_DRIFT", "unexpected merge in approved history")
        actual_commits.append({"commit": revision, "parent": parents[0],
                               "subject": V1.git(source, "show", "-s", "--format=%s", revision).decode().strip()})
    require(actual_commits == expected_commits
            and scope["counts"] == {"changedPaths": len(actual_changes),
                                    "changedRootGitlinks": sum(any(row[key] and row[key]["mode"] == "160000"
                                                                   for key in ("before", "after")) for row in actual_changes),
                                    "interveningCommits": len(actual_commits)},
            "SUCCESSOR_SCOPE_DRIFT", "approved commit chain or counts differ")
    candidate_tree = V1.tree(source, CANDIDATE)
    V1.verify_tracked_bytes(source, candidate_tree, "SUCCESSOR_SOURCE_DIRTY")
    V1.reject_ignored_inputs(source, "SUCCESSOR_SOURCE_DIRTY")
    gitlinks = [{"path": path, **entry} for path, entry in sorted(candidate_tree.items()) if entry["mode"] == "160000"]
    declaration = V1.git(source, "config", "--blob", f"{CANDIDATE}:.gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    paths = sorted(line.split(None, 1)[1] for line in declaration.decode().splitlines())
    require(paths == [row["path"] for row in gitlinks] and len(paths) == len(set(paths)),
            "SUCCESSOR_ENVIRONMENT_DRIFT", "root gitlinks differ from declared modules")
    dependencies = []
    for row in gitlinks:
        copied = source / row["path"]
        require(not copied.is_symlink(), "SUCCESSOR_ENVIRONMENT_DRIFT", "copied dependency is a symlink")
        if not copied.exists() or (copied.is_dir() and not any(copied.iterdir())):
            continue
        owner = authority_root / row["path"]
        require(copied.is_dir() and not copied.is_symlink() and owner.is_dir()
                and V1.git(owner, "rev-parse", "HEAD^{commit}").decode().strip() == row["object"],
                "SUCCESSOR_ENVIRONMENT_DRIFT", "copied root dependency has the wrong identity")
        owned_tree = V1.tree(owner, row["object"])
        expected_paths = set()
        for path, entry in owned_tree.items():
            if entry["type"] != "blob":
                continue
            expected_paths.add(path)
            target = copied / path
            if entry["mode"] == "120000":
                matches = target.is_symlink() and os.fsencode(os.readlink(target)) == V1.git(owner, "cat-file", "blob", entry["object"])
            else:
                hashed = subprocess.run(["git", "-C", str(owner), "hash-object", "--path", path, "--stdin"],
                                        input=target.read_bytes() if target.is_file() and not target.is_symlink() else b"",
                                        capture_output=True, check=False, timeout=30)
                matches = (target.is_file() and not target.is_symlink() and hashed.returncode == 0
                           and hashed.stdout.decode().strip() == entry["object"])
            require(matches, "SUCCESSOR_ENVIRONMENT_DRIFT", "copied dependency differs: " + row["path"] + "/" + path)
        actual_paths = {path.relative_to(copied).as_posix() for path in copied.rglob("*")
                        if path.is_file() or path.is_symlink()}
        require(actual_paths == expected_paths, "SUCCESSOR_ENVIRONMENT_DRIFT", "copied dependency has extra or missing files")
        dependencies.append({"path": row["path"], "commit": row["object"],
                             "tree": V1.git(owner, "rev-parse", "HEAD^{tree}").decode().strip()})
    source_files = [{"path": path, **entry, "sha256": V1.sha(V1.read(source, path))}
                    for path, entry in sorted(candidate_tree.items()) if path.startswith("src/") and entry["type"] == "blob"]
    snapshot = {"candidate": CANDIDATE, "tree": V1.git(source, "rev-parse", "HEAD^{tree}").decode().strip(),
                "sourceFiles": source_files, "sourceFilesSha256": V1.sha(V1.canonical(source_files)),
                "actualProductionSurface": V1.verifier().TIERING.surface(V1.verifier().TIERING.WorkTree(source)),
                "rootGitlinks": gitlinks, "copiedRootDependencies": dependencies,
                "changesSincePriorAuthorizedCandidate": actual_changes,
                "interveningCommits": actual_commits, "scopeProposalSha256": SCOPE_MATERIAL_SHA}
    return snapshot


def commands() -> list[tuple[str, list[str]]]:
    base = V1.build_commands()
    return base + [("module-internal-fallback", base[-1][1] +
                    ["-m:1", "-p:NuGetAudit=false", "-p:MinVerVersionOverride=1.0.0"])]


def measure(source: Path, authority_root: Path, directory: str) -> dict[str, Any]:
    _, scope = authority(authority_root)
    snapshot = validate_source(source, scope, authority_root)
    targets = V1.output_targets(authority_root, source, directory,
                                [BUILD_NAME] + [label + suffix for label, _ in commands()
                                                for suffix in (".stdout.log", ".stderr.log")])
    require(all(not target.exists() for target in targets), "SUCCESSOR_OUTPUT_COLLISION", "build receipt already exists")
    observations, logs = [], []
    for label, command in commands():
        code, stdout, stderr = V1.run_build(source, command)
        logs.extend([stdout, stderr])
        observations.append({"id": label, "command": command, "exitCode": code,
                             "state": "available" if code == 0 else "blocked",
                             "stdout": {"path": directory + "/" + label + ".stdout.log", "sha256": V1.sha(stdout)},
                             "stderr": {"path": directory + "/" + label + ".stderr.log", "sha256": V1.sha(stderr)}})
    require(validate_source(source, scope, authority_root) == snapshot, "SUCCESSOR_SOURCE_DIRTY", "build changed source inputs")
    receipt = {"schemaVersion": "hexalith.conversations.story-9.2-successor-build-evidence.v2",
               "candidate": CANDIDATE, "sourceSnapshotSha256": V1.sha(V1.canonical(snapshot)),
               "acceptanceClaimed": False, "observations": observations}
    V1.write_outputs(targets, [V1.json_bytes(receipt), *logs], immutable=True)
    return receipt


def load_builds(authority_root: Path, directory: str, snapshot: dict[str, Any]) -> dict[str, Any]:
    path = directory + "/" + BUILD_NAME
    raw = V1.read(authority_root, path)
    receipt = V1.parse(raw)
    require(receipt == {"schemaVersion": "hexalith.conversations.story-9.2-successor-build-evidence.v2",
                        "candidate": CANDIDATE, "sourceSnapshotSha256": V1.sha(V1.canonical(snapshot)),
                        "acceptanceClaimed": False, "observations": receipt.get("observations")},
            "SUCCESSOR_BUILD_EVIDENCE_INVALID", "receipt does not bind approved source")
    require(len(receipt["observations"]) == len(commands()), "SUCCESSOR_BUILD_EVIDENCE_INVALID", "missing build observations")
    for row, (label, command) in zip(receipt["observations"], commands(), strict=True):
        require(row["id"] == label and row["command"] == command and type(row["exitCode"]) is int
                and row["state"] == ("available" if row["exitCode"] == 0 else "blocked"),
                "SUCCESSOR_BUILD_EVIDENCE_INVALID", "build status or command differs")
        for stream in ("stdout", "stderr"):
            binding = row[stream]
            require(binding == {"path": directory + "/" + label + "." + stream + ".log",
                                "sha256": V1.sha(V1.read(authority_root, binding["path"]))},
                    "SUCCESSOR_BUILD_EVIDENCE_INVALID", "build log differs")
    return {"receipt": {"path": path, "sha256": V1.sha(raw)}, **receipt}


def released_api_evidence(authority_root: Path) -> dict[str, Any]:
    raw = V1.read(authority_root, API_EVIDENCE)
    require(V1.sha(raw) == API_EVIDENCE_SHA, "SUCCESSOR_API_EVIDENCE_INVALID", "released API evidence bytes differ")
    evidence = V1.parse(raw)
    require(evidence["schemaVersion"] == "hexalith.conversations.story-9.2-released-eventstore-api-diff.v1"
            and evidence["status"] == "incomplete-quality-review-required"
            and evidence["tool"] == {"name": "dotnet-inspect", "version": "0.26.0+d236a7a",
                                    "invocationPrefix": ["dnx", "dotnet-inspect", "-y", "--"]}
            and evidence["versions"] == {"before": "3.117.1", "after": "3.118.0"}
            and evidence["qualityApprovalClaimed"] is False
            and evidence["acceptedStoryRecordChanged"] is False,
            "SUCCESSOR_API_EVIDENCE_INVALID", "released API evidence claims an unsupported result")
    findings = evidence["findings"]
    require(findings["client"] == {"state": "complete", "breaking": 1, "additive": 28, "affectedTypes": 25}
            and findings["contracts"] == {"state": "complete", "breaking": 0, "additive": 97, "affectedTypes": 97}
            and findings["domainService"] == {"state": "BothIncomplete", "beforeMetadataInspectionFailures": 1,
                                               "afterMetadataInspectionFailures": 1}
            and findings["serviceDefaults"] == findings["domainService"]
            and findings["allFourPackageComparisonsComplete"] is False,
            "SUCCESSOR_API_EVIDENCE_INVALID", "package classification differs from the raw observations")
    constructor = findings["clientMarkerStoreConstructor"]
    require(constructor["type"] == "Hexalith.EventStore.Client.Subscriptions.EventStoreDomainEventProcessor"
            and constructor["beforeDigest"] == "acd44cf693" and constructor["afterDigest"] == "5022f823cd"
            and constructor["beforeParameterCount"] == 5 and constructor["afterParameterCount"] == 6
            and constructor["newParameterOptional"] is True and constructor["binarySignatureChanged"] is True
            and constructor["beforeSignature"].endswith("string? payloadAggregateIdPropertyName = null)")
            and constructor["afterSignature"].endswith("EventPayloadEvolutionRegistry? evolution = null)"),
            "SUCCESSOR_API_EVIDENCE_INVALID", "breaking constructor binding differs")
    probes = evidence["probes"]
    require([row["id"] for row in probes] == ["client", "contracts", "domainservice", "servicedefaults",
                                             "client-constructor-before", "client-constructor-after"],
            "SUCCESSOR_API_EVIDENCE_INVALID", "exact API probes are missing or duplicated")
    for row in probes:
        for stream in ("stdout", "stderr"):
            binding = row[stream]
            content = binding["text"].encode("utf-8")
            require(binding["sha256"] == V1.sha(content),
                    "SUCCESSOR_API_EVIDENCE_INVALID", "API probe output digest differs: " + row["id"])
            raw_log = authority_root / binding["path"]
            if raw_log.exists():
                require(raw_log.read_bytes() == content,
                        "SUCCESSOR_API_EVIDENCE_INVALID", "API probe log differs: " + row["id"])
    for row, package, expected_exit, summary in zip(probes[:4],
            ("Client", "Contracts", "DomainService", "ServiceDefaults"), (0, 0, 1, 1),
            ("1 breaking, 28 additive across 25 types", "97 additive across 97 types", "BothIncomplete", "BothIncomplete"), strict=True):
        require(row["command"] == ["dnx", "dotnet-inspect", "-y", "--", "diff", "--package",
                                   f"Hexalith.EventStore.{package}@3.117.1..3.118.0", "--table"]
                and row["exitCode"] == expected_exit
                and summary in row["stdout"]["text"] + row["stderr"]["text"],
                "SUCCESSOR_API_EVIDENCE_INVALID", "API comparison command or result differs")
    for row, version, signature in zip(probes[4:], ("3.117.1", "3.118.0"),
                                        (constructor["beforeSignature"], constructor["afterSignature"]), strict=True):
        require(row["command"] == ["dnx", "dotnet-inspect", "-y", "--", "member",
                                   "EventStoreDomainEventProcessor", "--package",
                                   "Hexalith.EventStore.Client@" + version, "-m", ".ctor"]
                and row["exitCode"] == 0 and signature in row["stdout"]["text"],
                "SUCCESSOR_API_EVIDENCE_INVALID", "constructor signature probe differs")
    expected_archives = {(f"Hexalith.EventStore.{name}", version)
                         for name in ("Client", "Contracts", "DomainService", "ServiceDefaults")
                         for version in ("3.117.1", "3.118.0")}
    require({(row["package"], row["version"]) for row in evidence["packageArchives"]} == expected_archives
            and len(evidence["packageArchives"]) == 8,
            "SUCCESSOR_API_EVIDENCE_INVALID", "package archive set is incomplete")
    for archive in evidence["packageArchives"]:
        name = archive["package"].lower()
        version = archive["version"]
        path = Path.home() / ".nuget/packages" / name / version / f"{name}.{version}.nupkg"
        if path.exists():
            content = path.read_bytes()
            require(archive["sha256"] == V1.sha(content) and archive["sizeBytes"] == len(content),
                    "SUCCESSOR_API_EVIDENCE_INVALID", "published package archive differs: " + name + "@" + version)
    return {"path": API_EVIDENCE, "sha256": API_EVIDENCE_SHA,
            "state": evidence["status"], "toolVersion": evidence["tool"]["version"]}


def released_api_compat_evidence(authority_root: Path) -> dict[str, Any]:
    raw = V1.read(authority_root, API_COMPAT_EVIDENCE)
    require(V1.sha(raw) == API_COMPAT_EVIDENCE_SHA, "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID",
            "SDK API compatibility evidence bytes differ")
    evidence = V1.parse(raw)
    require(evidence["schemaVersion"] == "hexalith.conversations.story-9.2-released-eventstore-api-compat.v1"
            and evidence["status"] == "partial-api-compatibility-observation-quality-review-required"
            and evidence["sdk"] == {"command": ["dotnet", "--version"], "exitCode": 0,
                                    "stdout": "10.0.401\n", "stderr": ""}
            and evidence["versions"] == {"before": "3.117.1", "after": "3.118.0"}
            and evidence["settings"] == {"target": "RunPackageValidation", "EnablePackageValidation": True,
                                         "RunApiCompat": True, "RunPackageValidationWithoutReferences": True}
            and evidence["qualityApprovalClaimed"] is False
            and evidence["acceptedStoryRecordChanged"] is False,
            "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK API compatibility authority differs")
    findings = evidence["findings"]
    require(findings == {"client": {"state": "breaking", "exitCode": 1, "diagnostic": "CP0002",
                                    "removedConstructorParameterCount": 5},
                         "domainService": {"state": "api-compat-pass", "exitCode": 0},
                         "serviceDefaults": {"state": "api-compat-pass", "exitCode": 0},
                         "contracts": {"state": "not-measured-by-this-tool"},
                         "allAdditiveApiReviewComplete": False},
            "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK API compatibility findings differ")
    prior = V1.parse(V1.read(authority_root, API_EVIDENCE))
    prior_archives = {(row["package"], row["version"]): row for row in prior["packageArchives"]}
    require(len(evidence["packageArchives"]) == 6,
            "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK package archive set differs")
    for row in evidence["packageArchives"]:
        old = prior_archives.get((row["package"], row["version"]))
        require(old is not None and row["sha256"] == old["sha256"]
                and row["sizeBytes"] == old["sizeBytes"],
                "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK package archive differs")
    expected = (("client", "Client", 1), ("domainservice", "DomainService", 0),
                ("servicedefaults", "ServiceDefaults", 0))
    require([row["id"] for row in evidence["probes"]] == [item[0] for item in expected],
            "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK probe set differs")
    for row, (identifier, package, exit_code) in zip(evidence["probes"], expected, strict=True):
        project = f"/tmp/story92-apicompat-{identifier}/Compare.csproj"
        require(row["command"] == ["dotnet", "msbuild", project, "-t:RunPackageValidation", "-v:minimal"]
                and row["exitCode"] == exit_code and row["project"]["path"] == project
                and row["project"]["sha256"] == V1.sha(row["project"]["text"].encode()),
                "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK command, result, or disposable project differs")
        project_text = row["project"]["text"]
        for fragment in ("<TargetFramework>net10.0</TargetFramework>",
                         "<EnablePackageValidation>true</EnablePackageValidation>",
                         "<RunApiCompat>true</RunApiCompat>",
                         "<RunPackageValidationWithoutReferences>true</RunPackageValidationWithoutReferences>",
                         f"<PackageId>Hexalith.EventStore.{package}</PackageId>",
                         "<PackageVersion>3.118.0</PackageVersion>",
                         f"hexalith.eventstore.{identifier}/3.117.1/",
                         f"hexalith.eventstore.{identifier}/3.118.0/"):
            require(fragment in project_text, "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID",
                    "SDK package comparison project differs")
        for stream in ("stdout", "stderr"):
            binding = row[stream]
            content = binding["text"].encode()
            require(binding["sha256"] == V1.sha(content),
                    "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK output digest differs")
            raw_log = authority_root / binding["path"]
            if raw_log.exists():
                require(raw_log.read_bytes() == content, "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID",
                        "SDK output log differs")
        output = row["stdout"]["text"] + row["stderr"]["text"]
        require(("error CP0002" in output and "EventStoreDomainEventProcessor.EventStoreDomainEventProcessor"
                 in output and "exists on [Baseline]" in output) if exit_code == 1 else "error" not in output,
                "SUCCESSOR_API_COMPAT_EVIDENCE_INVALID", "SDK API compatibility observation differs")
    return {"path": API_COMPAT_EVIDENCE, "sha256": API_COMPAT_EVIDENCE_SHA,
            "state": evidence["status"], "sdkVersion": evidence["sdk"]["stdout"].strip()}


def prepare(source: Path, authority_root: Path, directory: str) -> dict[str, Any]:
    authorization, scope = authority(authority_root)
    targets = V1.output_targets(authority_root, source, directory, [JSON_NAME, MARKDOWN_NAME])
    snapshot = validate_source(source, scope, authority_root)
    builds = load_builds(authority_root, directory, snapshot)
    api_evidence = released_api_evidence(authority_root)
    api_compat = released_api_compat_evidence(authority_root)
    require(b"<HexalithEventStoreVersion>3.118.0</HexalithEventStoreVersion>"
            in V1.read(source, "Directory.Packages.props"),
            "SUCCESSOR_ENVIRONMENT_DRIFT", "released EventStore package pin differs")
    tiering = V1.verifier()
    try:
        derived = tiering.derive_migration(source, current_tree=True)
        tiering.approved_migration(source, derived, current_tree=True)
    except tiering.VerificationError as error:
        raise V1.PreparationError(error.code, str(error)) from error
    original = tiering.document(source, tiering.MIGRATION)
    previous = V1.parse(V1.read(source, "docs/release-evidence/conformance-oracle-tiering-migration-v4-proposal.json"))
    require(V1.sha(V1.read(source, "docs/release-evidence/conformance-oracle-tiering-migration-v4-proposal.json")) == V4_SHA
            and V1.sha(V1.read(source, "docs/release-evidence/conformance-oracle-tiering-migration-review-v4.md")) == V4_REVIEW_SHA,
            "SUCCESSOR_PROTECTED_DRIFT", "prior pending successor changed")
    for binding in previous["successorMaterial"]["originalEvidence"]["protected"]:
        content = V1.read(source, binding["path"])
        require(V1.sha(content) == binding["sha256"] and len(content) == binding["sizeBytes"]
                and content == V1.git(source, "show", f"{V1.PINS['originalPublication']}:{binding['path']}"),
                "SUCCESSOR_PROTECTED_DRIFT", "original accepted evidence changed: " + binding["path"])
    proposal = derived["proposal"]
    require(proposal["executionPolicy"]["frozenExecutedCaseFloor"] == 415
            and len(proposal["liveControls"]) == 3,
            "SUCCESSOR_INVENTORY_INVALID", "frozen floor or controls changed")
    rows = [[row["id"], row["rowSha256"]] for row in proposal["changedAssertions"]]
    previous_proposal = previous["successorMaterial"]["proposedMigration"]
    old_rows = [[row["id"], row["rowSha256"]] for row in previous_proposal["changedAssertions"]]
    material = {"approvedScope": {"authorization": {"path": AUTH, "sha256": AUTH_SHA},
                                   "proposal": {"path": SCOPE, "sha256": SCOPE_SHA},
                                   "proposalSha256": SCOPE_MATERIAL_SHA, "decision": authorization},
                "originalEvidence": {"baseline": V1.PINS["baseline"],
                                     "acceptedCandidate": V1.PINS["originalCandidate"],
                                     "publication": V1.PINS["originalPublication"],
                                     "completedOriginalScopeSuccessor": V1.PINS["completedOriginalScopeSuccessor"],
                                     "protected": previous["successorMaterial"]["originalEvidence"]["protected"]},
                "priorPendingSuccessor": {"path": "docs/release-evidence/conformance-oracle-tiering-migration-v4-proposal.json",
                                          "sha256": V4_SHA, "proposalSha256": previous["proposalSha256"]},
                "releasedEventStoreApiDiff": api_evidence,
                "releasedEventStoreApiCompat": api_compat,
                "currentSourceSnapshot": snapshot, "proposedMigration": proposal,
                "proposedMigrationSha256": derived["proposalSha256"],
                "comparison": {"originalMigrationProposalSha256": original["proposalSha256"],
                               "previousMigrationProposalSha256": previous["successorMaterial"]["proposedMigrationSha256"],
                               "changedRowsChanged": rows != old_rows,
                               "originalChangedRows": old_rows, "proposedChangedRows": rows,
                               "publicDriftChanged": proposal["publicSurface"]["driftSha256"] != previous_proposal["publicSurface"]["driftSha256"],
                               "previousPublicDriftSha256": previous_proposal["publicSurface"]["driftSha256"],
                               "proposedPublicDriftSha256": proposal["publicSurface"]["driftSha256"]}}
    digest = V1.sha(V1.canonical(material))
    quality = {"role": "Quality owner", "proposalSha256": digest,
               "migrationProposalSha256": derived["proposalSha256"],
               "changedAssertionRows": rows,
               "publicDriftSha256": proposal["publicSurface"]["driftSha256"],
               "sourceSnapshotSha256": V1.sha(V1.canonical(snapshot)),
               "scopeProposalSha256": SCOPE_MATERIAL_SHA,
               "releasedEventStoreApiDiffSha256": API_EVIDENCE_SHA,
               "releasedEventStoreApiCompatSha256": API_COMPAT_EVIDENCE_SHA}
    document = {"schemaVersion": VERSION, "storyId": "9.2", "status": "quality-review-pending",
                "preparationOnly": True, "qualityApprovalClaimed": False, "acceptanceClaimed": False,
                "successorMaterial": material, "proposalSha256": digest,
                "requiredQualityBinding": quality, "buildEvidence": builds,
                "acceptance": {"state": "blocked", "executionClaimed": False,
                               "blockers": ["SUCCESSOR_QUALITY_DECISION_REQUIRED", "FRESH_CANDIDATE_ACCEPTANCE_REQUIRED"],
                               "requiredFrozenCases": 415, "requiredLiveControls": 3}}
    schema = V1.parse(V1.read(ROOT, SCHEMA))
    jsonschema.Draft202012Validator(schema).validate(document)
    require(document["proposalSha256"] == V1.sha(V1.canonical(material))
            and quality["sourceSnapshotSha256"] == V1.sha(V1.canonical(snapshot))
            and quality["migrationProposalSha256"] == V1.sha(V1.canonical(proposal)),
            "SUCCESSOR_BINDING_INVALID", "candidate, migration, or Quality binding digest differs")
    require(validate_source(source, scope, authority_root) == snapshot, "SUCCESSOR_SOURCE_DIRTY", "preparation changed source inputs")
    markdown = render(document)
    V1.write_outputs(targets, [V1.json_bytes(document), markdown])
    return document


def render(document: dict[str, Any]) -> bytes:
    material = document["successorMaterial"]
    comparison = material["comparison"]
    quality = document["requiredQualityBinding"]
    api_evidence = material["releasedEventStoreApiDiff"]
    api_compat = material["releasedEventStoreApiCompat"]
    lines = ["# Story 9.2 current-main successor Quality review v5", "",
             "Preparation evidence only. Scope authorization supplies no Quality decision or acceptance.", "",
             f"Authorized source: `{material['currentSourceSnapshot']['candidate']}`.",
             f"Authorized scope material: `{material['approvedScope']['proposalSha256']}`.",
             f"Successor material: `{document['proposalSha256']}`.",
             f"Proposed migration material: `{material['proposedMigrationSha256']}`.", "",
             f"Retained v4 proposal SHA-256: `{material['priorPendingSuccessor']['sha256']}`.",
             f"Original migration material: `{comparison['originalMigrationProposalSha256']}`.",
             f"Prior pending migration material: `{comparison['previousMigrationProposalSha256']}`.",
             f"Changed assertion row digests changed: `{str(comparison['changedRowsChanged']).lower()}`.",
             f"Conversations public drift digest changed: `{str(comparison['publicDriftChanged']).lower()}`.", "",
             "The proposal preserves all 452 frozen definitions, 401 active methods, 51 historical exclusions,",
             "the 415-case floor, three live controls, FR-20 membership, and original before-strength bindings.",
             "The actual current production source snapshot is bound separately from the historical freeze.", "",
             f"Exact released-package API evidence: `{api_evidence['path']}` (SHA-256 `{api_evidence['sha256']}`).",
             "The source pins released EventStore `3.118.0`. Its package public API is outside the Conversations",
             "public-drift digest. Client has 1 breaking and 28 additive changes across 25 types. The breaking",
             "change replaces the marker-store EventStoreDomainEventProcessor five-parameter constructor with",
             "a six-parameter signature adding optional EventPayloadEvolutionRegistry. Existing binaries that",
             "call the old constructor signature require recompilation. Contracts has 97 additive changes",
             "across 97 types. DomainService and ServiceDefaults are BothIncomplete due to one metadata",
             "inspection failure on each side; neither receives a complete API classification here.",
             f"SDK package API compatibility evidence: `{api_compat['path']}` (SHA-256 `{api_compat['sha256']}`).",
             "The .NET 10 RunPackageValidation/RunApiCompat target reports CP0002 for the removed Client",
             "five-parameter constructor. DomainService and ServiceDefaults each pass that SDK API",
             "compatibility target. Those exit codes are the SDK observations for those packages;",
             "the earlier dotnet-inspect BothIncomplete results remain exact observations of that tool.",
             "The SDK target does not complete additive API review, and Contracts was not measured by it.",
             "The v7 EventStore Client API diff is preparation history. Complete released-package API review",
             "and a genuine Quality decision remain required.", "",
             "## Exact later Quality binding", "", "```json", json.dumps(quality, indent=2), "```", "",
             "Review this complete material, every changed row, the current source/environment, and public drift.",
             "A later genuine Quality decision must bind these exact digests.", "",
             "## Measured Release build evidence", "", "| Observation | Exit | State | Command |",
             "| --- | --- | --- | --- |"]
    lines += [f"| {row['id']} | {row['exitCode']} | {row['state']} | `{' '.join(row['command'])}` |"
              for row in document["buildEvidence"]["observations"]]
    lines += ["", "Build logs and their hashes are bound in the JSON proposal. Current-tree tier execution",
              "and fault runs remain provisional; candidate-bound AC01–10 and final-record insertion are still required.",
              "The original baseline, v1–v4 preparation, v3 Quality approval, accepted pairs, and frozen records",
              "retain their original bytes. Story 9.2 stays in-progress.", ""]
    return "\n".join(lines).encode()


def main() -> int:
    parser = V1.PreparationArgumentParser(description=__doc__, allow_abbrev=False)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--authorization-root", required=True)
    parser.add_argument("--output-directory", required=True)
    parser.add_argument("--measure-builds", action="store_true")
    try:
        arguments = parser.parse_args()
        source = Path(arguments.repository).absolute()
        authority_root = Path(arguments.authorization_root).absolute()
        require(source != authority_root, "SUCCESSOR_SOURCE_INVALID", "use an isolated source checkout")
        result = (measure if arguments.measure_builds else prepare)(source, authority_root, arguments.output_directory)
        print(json.dumps({"status": "builds-measured" if arguments.measure_builds else "prepared",
                          "acceptanceClaimed": False, "documentSha256": V1.sha(V1.json_bytes(result))}, indent=2))
        return 0
    except V1.PreparationError as error:
        print(json.dumps({"status": "rejected", "acceptanceClaimed": False,
                          "blockers": [{"code": error.code, "message": str(error)}]}, indent=2))
        return 1
    except (OSError, KeyError, TypeError, ValueError, jsonschema.ValidationError) as error:
        print(json.dumps({"status": "rejected", "acceptanceClaimed": False,
                          "blockers": [{"code": "SUCCESSOR_INPUT_INVALID", "message": type(error).__name__}]}, indent=2))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
