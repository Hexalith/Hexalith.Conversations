"""Closed request, publication, and fault tests for V23 Story 7.1 entry authority."""

from __future__ import annotations

import importlib.util
import json
import math
import os
from datetime import datetime, timezone
from decimal import Decimal
from pathlib import Path
import shutil
import subprocess
import sys
from typing import Callable

from jsonschema import Draft202012Validator
from jsonschema.exceptions import ValidationError
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_story_7_1_entry_authority.py"
SPEC = importlib.util.spec_from_file_location("publish_story_7_1_entry_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)


def git(root: Path, *arguments: str) -> str:
    """Run Git in a fixture and return stripped stdout."""

    return subprocess.check_output(["git", "-C", str(root), *arguments], text=True).strip()


def commit(
    root: Path,
    message: str,
    *paths: str,
    committed_at: str = "2026-09-20T10:00:00Z",
) -> str:
    """Commit exact fixture paths without signing."""

    subprocess.run(["git", "-C", str(root), "add", "--sparse", "--", *paths], check=True)
    environment = dict(os.environ)
    environment.update({"GIT_AUTHOR_DATE": committed_at, "GIT_COMMITTER_DATE": committed_at})
    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "commit", "-q", "-m", message],
        check=True,
        env=environment,
    )
    return git(root, "rev-parse", "HEAD")


def materialize_protected_hosts(root: Path, trusted_host_commit: str, host_dir: Path) -> None:
    """Run the workflow's base-SHA Git object materialization for all protected host files."""

    host_dir.mkdir(mode=0o700)
    for relative_path in (
        "_bmad/scripts/resolve_current_planning_authority.py",
        "_bmad/scripts/verify_evidence_boundary.py",
        "pyproject.toml",
        "uv.lock",
    ):
        object_name = f"{trusted_host_commit}:{relative_path}"
        subprocess.run(["git", "-C", str(root), "cat-file", "-e", object_name], check=True)
        (host_dir / Path(relative_path).name).write_bytes(
            subprocess.check_output(["git", "-C", str(root), "show", object_name])
        )


def workflow_run_block(step_name: str) -> str:
    """Extract one literal-block shell program from the checked-in workflow."""

    workflow = (ROOT / publisher.WORKFLOW_PATH).read_text(encoding="utf-8")
    step_marker = f"      - name: {step_name}\n"
    start = workflow.index(step_marker)
    run_start = workflow.index("        run: |\n", start) + len("        run: |\n")
    next_step = workflow.find("\n      - name: ", run_start)
    next_job = workflow.find("\n  candidate-validation:\n", run_start)
    boundaries = [boundary for boundary in (next_step, next_job) if boundary >= 0]
    end = min(boundaries) if boundaries else len(workflow)
    lines = workflow[run_start:end].splitlines()
    block_lines: list[str] = []
    for line in lines:
        if line and not line.startswith("          "):
            break
        block_lines.append(line)
    if not block_lines:
        raise AssertionError(f"workflow step {step_name!r} is not one closed literal shell block")
    return "\n".join(line[10:] if line else "" for line in block_lines) + "\n"


def copy_paths(root: Path, paths: tuple[str, ...]) -> None:
    """Copy declared paths from the implementation worktree."""

    for relative_path in paths:
        if relative_path == publisher.REQUEST_PATH:
            continue
        source = ROOT / relative_path
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)


def candidate_repository(tmp_path: Path) -> tuple[Path, str]:
    """Create the exact request transaction over the frozen tooling baseline."""

    tmp_path.mkdir(parents=True, exist_ok=True)
    root = tmp_path / "candidate"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(
        [
            "git",
            "-C",
            str(root),
            "sparse-checkout",
            "set",
            "--no-cone",
            *publisher.TOOLING_PATHS,
            publisher.ARCHITECTURE_PATH,
        ],
        check=True,
    )
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publisher.TOOLING_BASELINE], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    copy_paths(root, publisher.TOOLING_PATHS)
    request = publisher.render_request(root)
    (root / publisher.REQUEST_PATH).write_bytes(publisher.json_bytes(request))
    return root, commit(root, "fix(planning): add V23 Story 7.1 entry request", *publisher.TOOLING_PATHS)


@pytest.fixture()
def request_candidate(tmp_path: Path) -> tuple[Path, str]:
    """Return a committed exact V23 request fixture."""

    return candidate_repository(tmp_path)


def test_request_is_deterministic_closed_blocked_and_execution_false() -> None:
    """The technical request records every missing gate without claiming approval."""

    first = publisher.render_request(ROOT)
    second = publisher.render_request(ROOT)

    assert publisher.json_bytes(first) == publisher.json_bytes(second)
    assert first["result"] == "BLOCKED"
    assert first["implementationHold"] == "ACTIVE"
    assert first["executionAllowed"] is False
    assert first["ownerApprovalClaimed"] is False
    assert first["assertionLedger"]
    assert {row["code"] for row in first["blockers"]} == {
        "V23_OPERATIONAL_ENVELOPE_MISSING",
        "V23_PRESERVATION_GATE_PENDING",
        "V23_PERFORMANCE_GATE_FAILED",
        "V23_LANDING_ZONE_GATE_BLOCKED",
        "V23_OWNER_APPROVAL_MISSING",
    }
    assert publisher.REQUEST_PATH not in {row["path"] for row in first["toolingTransaction"]["manifest"]}
    assert first["toolingTransaction"]["selfExcludedManifestSha256"] == publisher.canonical_digest(
        first["toolingTransaction"]["manifest"]
    )


def test_request_and_result_schema_are_closed() -> None:
    """The public schema accepts runtime alternatives and rejects structural near misses."""

    schema = json.loads((ROOT / publisher.SCHEMA_PATH).read_text(encoding="utf-8"))
    validator = Draft202012Validator(schema, format_checker=Draft202012Validator.FORMAT_CHECKER)
    request = publisher.render_request(ROOT)
    validator.validate(request)
    request["unexpected"] = True
    with pytest.raises(ValidationError):
        validator.validate(request)

    request = publisher.render_request(ROOT)
    request["toolingTransaction"]["exactChangedPaths"].append("unexpected")
    with pytest.raises(ValidationError):
        validator.validate(request)

    for mutate in (
        lambda document: document["resultSemantics"]["states"].pop(),
        lambda document: document["toolingTransaction"]["exactChangedPaths"].pop(),
        lambda document: document["toolingTransaction"]["manifest"].pop(),
        lambda document: document["sourceBindings"].pop(),
        lambda document: document["rootGitlinks"].pop(),
        lambda document: document["entryGates"].pop(),
        lambda document: document["publicationContract"]["exactChangedPaths"].pop(),
        lambda document: document["assertionLedger"].pop(),
        lambda document: document["blockers"].pop(),
    ):
        truncated = publisher.render_request(ROOT)
        mutate(truncated)
        with pytest.raises(ValidationError):
            validator.validate(truncated)

    detached_request_blocker = publisher.render_request(ROOT)
    detached_request_blocker["blockers"][0]["assertionIndex"] = 0
    with pytest.raises(ValidationError):
        validator.validate(detached_request_blocker)

    request = publisher.render_request(ROOT)
    request["sourceBindings"][0]["path"] = "_bmad-output/./architecture.md"
    with pytest.raises(ValidationError):
        validator.validate(request)

    result = publisher.fail_result("V23_FIXTURE_BLOCKED", "fixture", "BLOCKED")
    validator.validate(result)
    result["unexpected"] = True
    with pytest.raises(ValidationError):
        validator.validate(result)

    contradictory_fail = publisher.fail_result("V23_FIXTURE_FAILED", "fixture", "FAIL")
    contradictory_fail["exitCode"] = 0
    with pytest.raises(ValidationError):
        validator.validate(contradictory_fail)

    contradictory_blocked = publisher.fail_result("V23_FIXTURE_BLOCKED", "fixture", "BLOCKED")
    contradictory_blocked["assertionLedger"][0]["state"] = "PASS"
    with pytest.raises(ValidationError):
        validator.validate(contradictory_blocked)

    detached_failure = publisher.fail_result("V23_FIXTURE_BLOCKED", "fixture", "BLOCKED")
    detached_failure["blockers"][0]["assertionIndex"] = 1
    with pytest.raises(ValidationError):
        validator.validate(detached_failure)

    duplicate_failure = publisher.fail_result("V23_FIXTURE_BLOCKED", "fixture", "BLOCKED")
    duplicate_failure["assertionLedger"].append(dict(duplicate_failure["assertionLedger"][0]))
    duplicate_failure["blockers"].append(dict(duplicate_failure["blockers"][0]))
    with pytest.raises(ValidationError):
        validator.validate(duplicate_failure)

    invalid_observation = publisher.fail_result("V23_FIXTURE_BLOCKED", "fixture", "BLOCKED")
    invalid_observation["observed"] = {"candidateTree": "0" * 40}
    with pytest.raises(ValidationError):
        validator.validate(invalid_observation)

    request_shaped_failure = publisher.fail_result("V23_FIXTURE_FAILED", "fixture", "FAIL")
    request_shaped_failure["observed"] = {
        "requestPublication": "0" * 40,
        "protectedMain": "1" * 40,
        "historicalV22Candidate": "2" * 40,
    }
    with pytest.raises(ValidationError):
        validator.validate(request_shaped_failure)


def test_schema_digest_and_runtime_controls_are_independent_authority_boundaries(
    tmp_path: Path,
) -> None:
    """A relaxed schema cannot grant request or signed-authority execution controls."""

    relaxed = tmp_path / publisher.SCHEMA_PATH
    relaxed.parent.mkdir(parents=True)
    relaxed.write_text('{"$schema":"https://json-schema.org/draft/2020-12/schema"}\n', encoding="utf-8")
    request = publisher.render_request(ROOT)
    request["executionAllowed"] = True
    authority = {
        "schemaVersion": publisher.SCHEMA_VERSION,
        "recordType": "AUTHORITY",
        "authorityId": publisher.AUTHORITY_ID,
        "request": {"path": publisher.REQUEST_PATH, "publicationCommit": "0" * 40, "sha256": "0" * 64},
        "publication": {
            "sourceCommit": "0" * 40,
            "sourceTree": "0" * 40,
            "exactChangedPaths": list(publisher.AUTHORITY_PATHS),
            "requiredMode": "100644",
            "changedGitlinks": [],
        },
        "ownerDecision": {
            "identity": publisher.TRUSTED_OWNER_IDENTITY,
            "decidedAtUtc": "2026-09-20T12:00:00Z",
            "rationale": "Fixture owner approval remains explicit and scoped.",
            "decision": "EXECUTION_ALLOWED",
        },
        "gateDispositions": [],
        "sourceBindings": [],
        "rootGitlinks": [],
        "architecture": {
            "path": publisher.ARCHITECTURE_PATH,
            "prefixBytes": 1,
            "prefixSha256": "0" * 64,
            "version": publisher.ARCHITECTURE_VERSION,
            "requestSha256": "0" * 64,
        },
        "resultSemantics": publisher.result_semantics(),
        "assertionLedger": [],
        "blockers": [],
        "result": "PASS",
        "implementationHold": "EXECUTION_ALLOWED",
        "ownerApprovalClaimed": True,
        "releaseAuthorized": True,
        "pushAuthorized": False,
        "executionAllowed": True,
        "storyExecution": {"7.1": True, "7.2": False, "7.3": False, "7.4": False},
    }

    with pytest.raises(publisher.EntryAuthorityError) as schema_error:
        publisher.validate_schema(tmp_path, request)
    assert schema_error.value.code == "V23_SCHEMA_IDENTITY_MISMATCH"
    with pytest.raises(publisher.EntryAuthorityError) as request_error:
        publisher.validate_request_controls(request)
    assert request_error.value.code == "V23_REQUEST_CONTROL_DRIFT"
    with pytest.raises(publisher.EntryAuthorityError) as authority_error:
        publisher.validate_authority_controls(authority)
    assert authority_error.value.code == "V23_AUTHORITY_CONTROL_DRIFT"


@pytest.mark.parametrize(
    "invalid_path",
    (
        "_bmad-output/./architecture.md",
        "_bmad-output/planning-artifacts/../architecture.md",
        "_bmad-output/planning-artifacts/architecture.md\n",
        "_bmad-output/planning-artifacts/architecture.md\x7f",
    ),
)
def test_schema_and_runtime_reject_the_same_unsafe_repository_paths(invalid_path: str) -> None:
    """Public path syntax and runtime containment reject dot components and controls alike."""

    schema = json.loads((ROOT / publisher.SCHEMA_PATH).read_text(encoding="utf-8"))
    path_schema = {"$ref": "#/$defs/path", "$defs": schema["$defs"]}
    with pytest.raises(ValidationError):
        Draft202012Validator(path_schema).validate(invalid_path)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.safe_path(invalid_path)
    assert error.value.code == "V23_PATH_ESCAPE"


def test_committed_request_validates_exact_scope_and_preserves_v22(
    request_candidate: tuple[Path, str],
) -> None:
    """The committed request validates while historical/current V22 semantics remain unchanged."""

    root, candidate = request_candidate
    request, publication, _content = publisher.validate_request(root, candidate)
    result = publisher.request_check_result(request, publication)

    assert publication == candidate
    assert tuple(request["toolingTransaction"]["exactChangedPaths"]) == publisher.TOOLING_PATHS
    assert request["historicalV22"] == {
        "candidateCommit": publisher.HISTORICAL_V22_CANDIDATE,
        "candidateTree": publisher.commit_tree(root, publisher.HISTORICAL_V22_CANDIDATE),
        "result": "PASS",
        "executionAllowed": False,
    }
    assert result["result"] == "BLOCKED"
    assert result["executionAllowed"] is False
    assert result["assertionLedger"]


def test_request_cli_preserves_blocked_exit_semantics(request_candidate: tuple[Path, str]) -> None:
    """The operator-facing request check maps BLOCKED to exit code 2."""

    root, candidate = request_candidate
    completed = subprocess.run(
        [
            sys.executable,
            str(root / publisher.PUBLISHER_PATH),
            "--repository",
            str(root),
            "--candidate",
            candidate,
            "--verify-request",
        ],
        capture_output=True,
        text=True,
        check=False,
    )
    result = json.loads(completed.stdout)

    assert completed.returncode == 2
    assert result["result"] == "BLOCKED"
    assert result["exitCode"] == 2
    assert result["executionAllowed"] is False
    assert result["assertionLedger"]


def test_write_request_cli_creates_exact_deterministic_bytes(tmp_path: Path) -> None:
    """The real write-request CLI creates exactly the request rendered from the final tooling blobs."""

    root = tmp_path / "write-request"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publisher.TOOLING_BASELINE], check=True)
    copy_paths(root, publisher.TOOLING_PATHS)
    request_path = root / publisher.REQUEST_PATH
    request_path.unlink(missing_ok=True)
    expected = publisher.json_bytes(publisher.render_request(root))

    completed = subprocess.run(
        [
            sys.executable,
            str(root / publisher.PUBLISHER_PATH),
            "--repository",
            str(root),
            "--write-request",
        ],
        capture_output=True,
        text=True,
        check=False,
    )

    assert completed.returncode == 0, completed.stderr
    assert "V23_STORY_7_1_ENTRY_REQUEST_WRITTEN" in completed.stdout
    assert request_path.read_bytes() == expected

    request_path.chmod(0o755)
    retry = subprocess.run(
        [
            sys.executable,
            str(root / publisher.PUBLISHER_PATH),
            "--repository",
            str(root),
            "--write-request",
        ],
        capture_output=True,
        text=True,
        check=False,
    )
    retry_result = json.loads(retry.stdout)
    assert retry.returncode == 2
    assert "V23_STORY_7_1_ENTRY_REQUEST_WRITTEN" not in retry.stdout
    assert retry_result["blockers"][0]["code"] == "V23_WRITE_PATH_INVALID"


def test_request_scope_manifest_and_descendant_faults_have_stable_codes(tmp_path: Path) -> None:
    """Unexpected scope and post-publication request drift cannot become PASS."""

    root = tmp_path / "scope"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publisher.TOOLING_BASELINE], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    copy_paths(root, publisher.TOOLING_PATHS)
    request = publisher.render_request(root)
    (root / publisher.REQUEST_PATH).write_bytes(publisher.json_bytes(request))
    unexpected = root / "unexpected-v23.txt"
    unexpected.write_text("unexpected\n", encoding="utf-8")
    scope_candidate = commit(
        root,
        "test: inject V23 scope drift",
        *publisher.TOOLING_PATHS,
        "unexpected-v23.txt",
    )
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_request(root, scope_candidate)
    assert error.value.code == "V23_TOOLING_SCOPE_DRIFT"

    root, request_commit = candidate_repository(tmp_path / "descendant")
    request_path = root / publisher.REQUEST_PATH
    request_path.write_bytes(request_path.read_bytes() + b" ")
    drifted = commit(root, "test: inject request drift", publisher.REQUEST_PATH)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_request(root, drifted)
    assert error.value.code == "V23_REQUEST_DESCENDANT_DRIFT"
    assert request_commit != drifted

    root, _request_commit = candidate_repository(tmp_path / "tooling-descendant")
    publisher_path = root / publisher.PUBLISHER_PATH
    publisher_path.write_bytes(publisher_path.read_bytes() + b"\n")
    tooling_drift = commit(root, "test: inject tooling descendant drift", publisher.PUBLISHER_PATH)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_request(root, tooling_drift)
    assert error.value.code == "V23_TOOLING_DESCENDANT_DRIFT"


def pass_evidence(
    root: Path,
    evaluated: str,
    gate_id: str,
    *,
    performance_factor: float = 1.0,
) -> bytes:
    """Return one closed gate-specific record bound to committed sources and command output."""

    authorities = {"PRESERVATION": "FR-20/SM-C1", "PERFORMANCE": "SM-C2", "LANDING_ZONE": "OQ-1"}
    canonical_paths = {
        "PRESERVATION": publisher.PRESERVATION_MANIFEST_PATH,
        "PERFORMANCE": publisher.PERFORMANCE_BASELINE_PATH,
        "LANDING_ZONE": publisher.LANDING_ZONE_AUTHORITY_PATH,
    }
    source = publisher.binding(root, evaluated, canonical_paths[gate_id])
    source_bindings = [source]
    if gate_id == "PRESERVATION":
        manifest = json.loads(publisher.candidate_blob(root, evaluated, publisher.PRESERVATION_MANIFEST_PATH))
        denominator = manifest["testDenominator"]["currentCandidate"]
        output = {
            "gateId": gate_id,
            "testResults": [{"id": test_id, "state": "PASS"} for test_id in denominator["testIds"]],
            "categoryMappings": [
                {
                    "category": row["category"],
                    "requirementIds": row["requirementIds"],
                    "testIds": row["testIds"],
                }
                for row in manifest["categoryMappings"]
            ],
        }
        measurements = {
            "denominatorCount": len(denominator["testIds"]),
            "denominatorSha256": denominator["testIdsSha256"],
            "evaluatedCount": len(denominator["testIds"]),
            "mismatchCount": 0,
        }
        ledger_ids: list[str] = []
        ledger_ids.extend(f"PRESERVATION.TEST.{publisher.sha256(test_id.encode())}" for test_id in denominator["testIds"])
        ledger_ids.extend(f"PRESERVATION.CATEGORY.{row['category']}" for row in manifest["categoryMappings"])
    elif gate_id == "PERFORMANCE":
        baseline = json.loads(publisher.candidate_blob(root, evaluated, publisher.PERFORMANCE_BASELINE_PATH))
        rows = sorted(baseline["rows"], key=lambda row: row["hotPathId"])
        output = {
            "gateId": gate_id,
            "hotPaths": [
                {
                    "hotPathId": row["hotPathId"],
                    "baselineSamples": row["rawMicrosecondsPerOperation"],
                    "candidateSamples": [
                        float(Decimal(str(value)) * Decimal(str(performance_factor)))
                        for value in row["rawMicrosecondsPerOperation"]
                    ],
                }
                for row in rows
            ],
        }
        summaries = []
        output_by_id = {row["hotPathId"]: row for row in output["hotPaths"]}
        for row in rows:
            samples = sorted(Decimal(str(value)) for value in row["rawMicrosecondsPerOperation"])
            candidate_samples = sorted(
                Decimal(str(value)) for value in output_by_id[row["hotPathId"]]["candidateSamples"]
            )
            p95_decimal = samples[math.ceil(Decimal("0.95") * len(samples)) - 1]
            candidate_p95_decimal = candidate_samples[
                math.ceil(Decimal("0.95") * len(candidate_samples)) - 1
            ]
            regression = ((candidate_p95_decimal - p95_decimal) * Decimal(100)) / p95_decimal
            summaries.append(
                {
                    "hotPathId": row["hotPathId"],
                    "sampleCount": len(samples),
                    "baselineP95Microseconds": float(p95_decimal),
                    "candidateP95Microseconds": float(candidate_p95_decimal),
                    "observedRegressionPercent": float(regression),
                }
            )
        measurements = {
            "minimumSampleCount": publisher.PERFORMANCE_MIN_SAMPLE_COUNT,
            "maximumRegressionPercent": publisher.PERFORMANCE_MAX_REGRESSION_PERCENT,
            "hotPaths": summaries,
        }
        ledger_ids = []
        for hot_path_id in publisher.PERFORMANCE_HOT_PATHS:
            ledger_ids.extend((f"PERFORMANCE.{hot_path_id}.BASELINE", f"PERFORMANCE.{hot_path_id}.CANDIDATE"))
    else:
        predecessor_authority_content = publisher.candidate_blob(
            root,
            evaluated,
            publisher.LANDING_ZONE_AUTHORITY_PATH,
        )
        authority = json.loads(predecessor_authority_content)
        decisions = {row["requirementId"]: row for row in authority["decisions"]}
        source_bindings.extend(
            publisher.binding(root, evaluated, path)
            for path in (publisher.LANDING_ZONE_APPROVAL_PATH, *publisher.LANDING_ZONE_GRANT_PATHS.values())
        )
        effective_grants: dict[str, tuple[dict[str, object], dict[str, str]]] = {}
        for grant_id, predecessor_path in publisher.LANDING_ZONE_GRANT_PATHS.items():
            predecessor_grant_content = publisher.candidate_blob(root, evaluated, predecessor_path)
            predecessor_grant = json.loads(predecessor_grant_content)
            successor_grant = {
                "schemaVersion": "hexalith.conversations.oq-1-effective-successor-grant.v1",
                "grantId": f"{grant_id}-EFFECTIVE-SUCCESSOR-v1",
                "predecessor": {
                    "path": predecessor_path,
                    "grantId": grant_id,
                    "status": "issued-pending-evidence-closure",
                    "effective": False,
                    "sha256": publisher.sha256(predecessor_grant_content),
                },
                "grantor": {
                    "authorityId": publisher.LANDING_ZONE_APPROVING_AUTHORITY,
                    "binding": publisher.binding(root, evaluated, publisher.LANDING_ZONE_APPROVAL_PATH),
                },
                "repository": publisher.successor_repository_binding(
                    root,
                    evaluated,
                    grant_id,
                    predecessor_grant["repository"],
                ),
                "requirements": list(publisher.LANDING_ZONE_GRANT_REQUIREMENTS[grant_id]),
                "effective": True,
                "effectiveAtUtc": "2026-09-20T11:15:00Z",
                "decision": "EXECUTION_ALLOWED",
            }
            successor_grant_path = publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS[grant_id]
            target = root / successor_grant_path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(publisher.json_bytes(successor_grant))
            successor_grant_binding = publisher.worktree_binding(root, successor_grant_path)
            source_bindings.append(successor_grant_binding)
            effective_grants[grant_id] = (successor_grant, successor_grant_binding)
        approved: list[tuple[str, dict[str, object], dict[str, str]]] = []
        for decision_id in publisher.LANDING_ZONE_DECISION_IDS:
            predecessor = decisions[decision_id]
            successor = {
                "schemaVersion": "hexalith.conversations.oq-1-approved-successor-decision.v1",
                "decisionId": f"OQ-1-{decision_id}-APPROVED-SUCCESSOR-v1",
                "requirementId": decision_id,
                "state": "APPROVED",
                "supersedes": {
                    "path": publisher.LANDING_ZONE_AUTHORITY_PATH,
                    "recordResult": "BLOCKED",
                    "recordSha256": publisher.sha256(predecessor_authority_content),
                    "decisionObjectSha256": publisher.canonical_digest(predecessor),
                },
                "approvingAuthority": {
                    "authorityId": publisher.LANDING_ZONE_APPROVING_AUTHORITY,
                    "binding": publisher.binding(root, evaluated, publisher.LANDING_ZONE_APPROVAL_PATH),
                },
                "grantAuthorities": [
                    {
                        "authorityId": effective_grants[grant_id][0]["grantId"],
                        "predecessorAuthorityId": grant_id,
                        "binding": effective_grants[grant_id][1],
                    }
                    for grant_id in predecessor["owningRepositoryApproval"]
                ],
            }
            successor_path = publisher.LANDING_ZONE_SUCCESSOR_PATHS[decision_id]
            target = root / successor_path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(publisher.json_bytes(successor))
            successor_binding = publisher.worktree_binding(root, successor_path)
            source_bindings.append(successor_binding)
            approved.append((decision_id, successor, successor_binding))
        output = {
            "gateId": gate_id,
            "predecessorResult": "BLOCKED",
            "decisions": [
                {
                    "decisionId": successor["decisionId"],
                    "requirementId": decision_id,
                    "approvingAuthority": publisher.LANDING_ZONE_APPROVING_AUTHORITY,
                    "predecessorResult": "BLOCKED",
                    "predecessorDecisionObjectSha256": successor["supersedes"]["decisionObjectSha256"],
                    "decisionObjectSha256": successor_binding["sha256"],
                    "state": "APPROVED",
                }
                for decision_id, successor, successor_binding in approved
            ],
            "unresolvedDecisionIds": [],
        }
        measurements = {
            "decisionsRequired": len(publisher.LANDING_ZONE_DECISION_IDS),
            "decisionsApproved": len(publisher.LANDING_ZONE_DECISION_IDS),
            "unresolvedCount": 0,
        }
        ledger_ids = [
            (f"LANDING_ZONE.DECISION.{decision_id}", successor_binding["sha256"])
            for decision_id, _successor, successor_binding in approved
        ]
    output_utf8 = publisher.json_bytes(output).decode("utf-8")
    output_sha256 = publisher.sha256(output_utf8.encode())
    return publisher.json_bytes(
        {
            "schemaVersion": "hexalith.conversations.story-7.1-entry-gate-result.v2",
            "gateId": gate_id,
            "gateAuthority": authorities[gate_id],
            "evaluatedCommit": evaluated,
            "evaluatedTree": publisher.commit_tree(root, evaluated),
            "disposition": {
                "identity": publisher.TRUSTED_OWNER_IDENTITY,
                "decidedAtUtc": "2026-09-20T11:30:00Z",
                "decision": "PASS",
            },
            "sourceBindings": source_bindings,
            "command": {
                "argv": ["fixture", gate_id],
                "exitCode": 0,
                "outputSha256": output_sha256,
                "outputUtf8": output_utf8,
            },
            "measurements": measurements,
            "assertionLedger": [
                {
                    "id": f"{gate_id}.SOURCE.{publisher.sha256(binding['path'].encode())}",
                    "subject": authorities[gate_id],
                    "state": "PASS",
                    "sourceSha256": binding["sha256"],
                    "outputSha256": output_sha256,
                }
                for binding in source_bindings
            ]
            + [
                {
                    "id": ledger_id,
                    "subject": authorities[gate_id],
                    "state": "PASS",
                    "sourceSha256": source_sha256,
                    "outputSha256": output_sha256,
                }
                for ledger_id, source_sha256 in (
                    ledger_ids
                    if gate_id == "LANDING_ZONE"
                    else [(ledger_id, source["sha256"]) for ledger_id in ledger_ids]
                )
            ],
            "result": "PASS",
        }
    )


def recovery_runbook() -> bytes:
    """Return the one canonical, closed Story 7.1 production recovery runbook."""

    return publisher.json_bytes(
        {
            "schemaVersion": "hexalith.conversations.story-7.1-production-recovery.v1",
            "runbookId": "STORY-7.1-PRODUCTION-RECOVERY",
            "ownerIdentity": publisher.TRUSTED_OWNER_IDENTITY,
            "rpoMinutes": 60,
            "rtoMinutes": 240,
            "procedures": [
                {
                    "id": procedure_id,
                    "responsibleOwner": publisher.TRUSTED_OWNER_IDENTITY,
                    "action": publisher.RECOVERY_PROCEDURES[procedure_id][0],
                    "outcome": publisher.RECOVERY_PROCEDURES[procedure_id][1],
                }
                for procedure_id in publisher.RECOVERY_PROCEDURE_IDS
            ],
        }
    )


def operational_envelope(
    root: Path,
    evaluated: str,
    runbook_content: bytes | None = None,
    *,
    scaling_strategy: str = "HORIZONTAL",
    waiver_state: str = "NONE",
) -> bytes:
    """Return the closed AD-5 fixture with explicit responsibilities and dispositions."""

    del root, evaluated
    runbook_path = publisher.RECOVERY_RUNBOOK_PATH
    runbook_sha256 = publisher.sha256(runbook_content or recovery_runbook())
    return publisher.json_bytes(
        {
            "schemaVersion": "hexalith.conversations.production-operational-envelope.v1",
            "owner": {
                "identity": publisher.TRUSTED_OWNER_IDENTITY,
                "role": "AD-5 operational owner",
                "authorityId": "AD-5-OWNER-AUTHORITY-v1",
            },
            "evaluatedAtUtc": "2026-09-20T11:00:00Z",
            "environments": [
                {
                    "environmentId": name,
                    "providerId": "local-containers" if name == "local" else "azure-platform",
                    "responsibleOwner": publisher.TRUSTED_OWNER_IDENTITY,
                    "dependencies": [
                        {
                            "capability": capability,
                            "providerId": f"{name}-{capability}-provider",
                            "responsibleOwner": publisher.TRUSTED_OWNER_IDENTITY,
                            "responsibility": f"operate and recover {capability} for {name}",
                        }
                        for capability in ("state", "pubsub", "secrets", "identity", "health", "telemetry")
                    ],
                }
                for name in ("local", "ci", "staging", "production")
            ],
            "recovery": {
                "rpoMinutes": 60,
                "rtoMinutes": 240,
                **publisher.RECOVERY_SUMMARIES,
                "responsibleOwner": publisher.TRUSTED_OWNER_IDENTITY,
                "runbook": {"path": runbook_path, "sha256": runbook_sha256},
            },
            "scaling": {
                "minimumReplicas": 1,
                "maximumReplicas": 1 if scaling_strategy == "FIXED" else 8,
                "strategy": scaling_strategy,
                "metric": "queue.depth",
                "scaleOutThreshold": 0.0 if scaling_strategy == "FIXED" else 100.0,
                "scaleInThreshold": 0.0 if scaling_strategy == "FIXED" else 20.0,
                "responsibleOwner": publisher.TRUSTED_OWNER_IDENTITY,
            },
            "waiver": (
                {
                    "state": "ACTIVE",
                    "expiresAtUtc": "2026-09-21T12:00:00Z",
                    "rationale": "Temporary operational variance approved for Story 7.1 validation.",
                    "ownerIdentity": publisher.TRUSTED_OWNER_IDENTITY,
                }
                if waiver_state == "ACTIVE"
                else {"state": "NONE", "expiresAtUtc": None, "rationale": None, "ownerIdentity": None}
            ),
        }
    )


def authority_repository(
    tmp_path: Path,
    *,
    mutation: Callable[[dict[str, object]], None] | None = None,
    evidence_mutation: Callable[[str, dict[str, object]], None] | None = None,
    runbook_mutation: Callable[[dict[str, object]], None] | None = None,
    signing_key: Path | None = None,
    source_committed_at: str = "2026-09-20T10:00:00Z",
    publication_committed_at: str = "2026-09-20T13:00:00Z",
) -> tuple[Path, str, str, bytes]:
    """Build a request, gate-evidence source, and unsigned two-path authority fixture."""

    root, request_commit = candidate_repository(tmp_path)
    evidence_paths = {
        "preservation": publisher.PRESERVATION_EVIDENCE_PATH,
        "performance": publisher.PERFORMANCE_EVIDENCE_PATH,
        "landing": publisher.LANDING_ZONE_EVIDENCE_PATH,
    }
    runbook_content = recovery_runbook()
    if runbook_mutation is not None:
        runbook_document = json.loads(runbook_content)
        runbook_mutation(runbook_document)
        runbook_content = publisher.json_bytes(runbook_document)
    runbook = root / publisher.RECOVERY_RUNBOOK_PATH
    runbook.parent.mkdir(parents=True, exist_ok=True)
    runbook.write_bytes(runbook_content)
    envelope = root / publisher.OPERATIONAL_ENVELOPE_PATH
    envelope.parent.mkdir(parents=True, exist_ok=True)
    envelope.write_bytes(operational_envelope(root, request_commit, runbook_content))
    for path, gate_id in zip(
        evidence_paths.values(),
        ("PRESERVATION", "PERFORMANCE", "LANDING_ZONE"),
        strict=True,
    ):
        target = root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        evidence = json.loads(pass_evidence(root, request_commit, gate_id))
        if evidence_mutation is not None:
            evidence_mutation(gate_id, evidence)
        target.write_bytes(publisher.json_bytes(evidence))
    source = commit(
        root,
        "test: add current V23 gate evidence",
        publisher.OPERATIONAL_ENVELOPE_PATH,
        publisher.RECOVERY_RUNBOOK_PATH,
        *evidence_paths.values(),
        *publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
        *publisher.LANDING_ZONE_SUCCESSOR_PATHS.values(),
        committed_at=source_committed_at,
    )
    document, architecture = publisher.render_authority(
        root,
        source,
        owner_identity=publisher.TRUSTED_OWNER_IDENTITY,
        decided_at_utc="2026-09-20T12:00:00Z",
        rationale="Approve only Story 7.1 entry after all current gate evidence passed.",
        preservation_evidence=evidence_paths["preservation"],
        performance_evidence=evidence_paths["performance"],
        landing_zone_evidence=evidence_paths["landing"],
    )
    if mutation is not None:
        mutation(document)
        prefix = publisher.candidate_blob(root, source, publisher.ARCHITECTURE_PATH)
        request_content = publisher.candidate_blob(root, request_commit, publisher.REQUEST_PATH)
        architecture = prefix + publisher.expected_v23_suffix(
            publisher.sha256(publisher.json_bytes(document)),
            publisher.sha256(request_content),
        )
    (root / publisher.AUTHORITY_PATH).write_bytes(publisher.json_bytes(document))
    (root / publisher.ARCHITECTURE_PATH).write_bytes(architecture)
    if signing_key is None:
        publication = commit(
            root,
            "test: publish V23 entry authority",
            *publisher.AUTHORITY_PATHS,
            committed_at=publication_committed_at,
        )
    else:
        git(root, "config", "user.name", "Jerome Piquot")
        git(root, "config", "user.email", "jpiquot@itaneo.com")
        git(root, "config", "gpg.format", "ssh")
        git(root, "config", "user.signingkey", str(signing_key))
        subprocess.run(["git", "-C", str(root), "add", "--sparse", "--", *publisher.AUTHORITY_PATHS], check=True)
        environment = dict(os.environ)
        environment.update(
            {
                "GIT_AUTHOR_DATE": publication_committed_at,
                "GIT_COMMITTER_DATE": publication_committed_at,
            }
        )
        subprocess.run(
            ["git", "-C", str(root), "commit", "-q", "-S", "-m", "test: publish signed V23 entry authority"],
            check=True,
            env=environment,
        )
        publication = git(root, "rev-parse", "HEAD")
    return root, request_commit, publication, publisher.json_bytes(document)


def test_approved_publication_passes_nonvacuously_for_story_7_1_only(tmp_path: Path) -> None:
    """The separately approved two-path fixture reaches PASS/true only for Story 7.1."""

    root, _request_commit, publication, _authority = authority_repository(tmp_path)
    result = publisher.resolve_published_authority(
        root,
        publication,
        signature_verifier=lambda _root, _commit, _identity: {
            "status": "G",
            "principal": publisher.TRUSTED_SSH_PRINCIPAL,
            "fingerprint": publisher.TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": publisher.TRUSTED_OWNER_IDENTITY,
        },
    )

    assert result["result"] == "PASS"
    assert result["exitCode"] == 0
    assert result["executionAllowed"] is True
    assert result["ownerApprovalClaimed"] is True
    assert result["releaseAuthorized"] is False
    assert result["pushAuthorized"] is False
    assert result["storyExecution"] == {"7.1": True, "7.2": False, "7.3": False, "7.4": False}
    assert result["assertionLedger"]
    assert {row["state"] for row in result["assertionLedger"]} == {"PASS"}


@pytest.mark.parametrize(
    "invalid_instant",
    (
        "2026-09-20T12:00:00+00:00",
        "2026-09-20T12:00:00.000Z",
    ),
)
def test_authority_schema_rejects_runtime_timestamp_near_misses(
    tmp_path: Path,
    invalid_instant: str,
) -> None:
    """Schema-only consumers reject the offset and fractional instants rejected at runtime."""

    root, _request, _publication, authority_content = authority_repository(tmp_path)
    schema = json.loads((root / publisher.SCHEMA_PATH).read_text(encoding="utf-8"))
    validator = Draft202012Validator(schema, format_checker=Draft202012Validator.FORMAT_CHECKER)
    authority = json.loads(authority_content)
    authority["ownerDecision"]["decidedAtUtc"] = invalid_instant

    with pytest.raises(ValidationError):
        validator.validate(authority)


def test_authority_and_pass_result_schema_require_every_fixed_inventory(tmp_path: Path) -> None:
    """Authority and PASS schemas reject every truncation and trailing source substitution."""

    root, _request, publication, authority_content = authority_repository(tmp_path)
    schema = json.loads((root / publisher.SCHEMA_PATH).read_text(encoding="utf-8"))
    validator = Draft202012Validator(schema, format_checker=Draft202012Validator.FORMAT_CHECKER)
    authority = json.loads(authority_content)
    validator.validate(authority)

    for mutate in (
        lambda document: document["publication"]["exactChangedPaths"].pop(),
        lambda document: document["gateDispositions"].pop(),
        lambda document: document["sourceBindings"].pop(),
        lambda document: document["rootGitlinks"].pop(),
        lambda document: document["assertionLedger"].pop(),
    ):
        truncated = json.loads(authority_content)
        mutate(truncated)
        with pytest.raises(ValidationError):
            validator.validate(truncated)

    substituted = json.loads(authority_content)
    substituted["sourceBindings"][15]["path"] = publisher.OPERATIONAL_ENVELOPE_PATH
    with pytest.raises(ValidationError):
        validator.validate(substituted)

    result = publisher.resolve_published_authority(
        root,
        publication,
        signature_verifier=lambda _root, _commit, _identity: {
            "status": "G",
            "principal": publisher.TRUSTED_SSH_PRINCIPAL,
            "fingerprint": publisher.TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": publisher.TRUSTED_OWNER_IDENTITY,
        },
    )
    validator.validate(result)
    for mutate in (
        lambda document: document["observed"]["changedPaths"].pop(),
        lambda document: document["assertionLedger"].pop(),
    ):
        truncated_result = json.loads(publisher.json_bytes(result))
        mutate(truncated_result)
        with pytest.raises(ValidationError):
            validator.validate(truncated_result)


def test_malformed_committed_canonical_recovery_runbook_blocks_authority(tmp_path: Path) -> None:
    """Digest-matched committed runbook content must satisfy the closed procedure inventory."""

    def remove_procedure(document: dict[str, object]) -> None:
        procedures = document["procedures"]
        assert isinstance(procedures, list)
        procedures.pop()

    with pytest.raises(publisher.EntryAuthorityError) as error:
        authority_repository(tmp_path, runbook_mutation=remove_procedure)

    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"


@pytest.mark.parametrize("procedure_index", range(len(publisher.RECOVERY_PROCEDURE_IDS)))
@pytest.mark.parametrize("field", ("action", "outcome"))
def test_recovery_runbook_rejects_generic_phase_text(
    tmp_path: Path,
    procedure_index: int,
    field: str,
) -> None:
    """Each recovery phase carries its exact actionable verb and observable outcome."""

    def replace_phase_contract(document: dict[str, object]) -> None:
        procedures = document["procedures"]
        assert isinstance(procedures, list)
        assert isinstance(procedures[procedure_index], dict)
        procedures[procedure_index][field] = "Generic recovery step."

    with pytest.raises(publisher.EntryAuthorityError) as error:
        authority_repository(tmp_path, runbook_mutation=replace_phase_contract)

    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"


@pytest.mark.parametrize("summary", tuple(publisher.RECOVERY_SUMMARIES))
def test_operational_envelope_rejects_contradictory_recovery_summary(
    summary: str,
) -> None:
    """Envelope summaries cannot contradict the exact committed recovery procedures."""

    def contradict(document: dict[str, object]) -> None:
        recovery = document["recovery"]
        assert isinstance(recovery, dict)
        recovery[summary] = "Contradictory recovery procedure."

    with pytest.raises(publisher.EntryAuthorityError) as error:
        envelope = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
        contradict(envelope)
        publisher.validate_operational_envelope(publisher.json_bytes(envelope))

    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"


def test_authority_chronology_uses_graph_ancestry_not_git_commit_dates(tmp_path: Path) -> None:
    """Caller-controlled Git dates cannot invalidate an otherwise ordered authority graph."""

    root, _request, publication, _authority = authority_repository(
        tmp_path,
        source_committed_at="2099-01-01T00:00:00Z",
        publication_committed_at="2000-01-01T00:00:00Z",
    )
    result = publisher.resolve_published_authority(
        root,
        publication,
        signature_verifier=lambda _root, _commit, _identity: {
            "status": "G",
            "principal": publisher.TRUSTED_SSH_PRINCIPAL,
            "fingerprint": publisher.TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": publisher.TRUSTED_OWNER_IDENTITY,
        },
    )

    assert result["result"] == "PASS"


def test_authority_render_requires_request_ancestry(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """A request identity outside the evidence-source ancestry fails before evidence evaluation."""

    root, source = candidate_repository(tmp_path)
    empty_tree = subprocess.check_output(
        ["git", "-C", str(root), "mktree"],
        input=b"",
    ).decode("ascii").strip()
    unrelated = subprocess.check_output(
        ["git", "-C", str(root), "commit-tree", empty_tree, "-m", "test: unrelated request identity"],
        env={
            **os.environ,
            "GIT_AUTHOR_NAME": "V23 fixture",
            "GIT_AUTHOR_EMAIL": "v23-fixture@example.invalid",
            "GIT_COMMITTER_NAME": "V23 fixture",
            "GIT_COMMITTER_EMAIL": "v23-fixture@example.invalid",
        },
        text=True,
    ).strip()
    real_validate_request = publisher.validate_request

    def detached_request(repository: Path, candidate: str) -> tuple[dict[str, object], str, bytes]:
        request, _publication, content = real_validate_request(repository, candidate)
        return request, unrelated, content

    monkeypatch.setattr(publisher, "validate_request", detached_request)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.render_authority(
            root,
            source,
            owner_identity=publisher.TRUSTED_OWNER_IDENTITY,
            decided_at_utc="2026-09-20T12:00:00Z",
            rationale="Approve only Story 7.1 entry after all current gate evidence passed.",
            preservation_evidence=publisher.PRESERVATION_EVIDENCE_PATH,
            performance_evidence=publisher.PERFORMANCE_EVIDENCE_PATH,
            landing_zone_evidence=publisher.LANDING_ZONE_EVIDENCE_PATH,
        )

    assert error.value.code == "V23_GATE_EVIDENCE_GRAPH_DRIFT"


def test_publish_authority_cli_writes_exact_pair(tmp_path: Path) -> None:
    """The owner-facing CLI must write the exact authority and marker bytes it renders."""

    root, _request, publication, authority_content = authority_repository(tmp_path)
    source = git(root, "rev-parse", f"{publication}^")
    (root / publisher.AUTHORITY_PATH).unlink()
    prefix = publisher.candidate_blob(root, source, publisher.ARCHITECTURE_PATH)
    (root / publisher.ARCHITECTURE_PATH).write_bytes(prefix)
    completed = subprocess.run(
        [
            sys.executable,
            str(root / publisher.PUBLISHER_PATH),
            "--repository",
            str(root),
            "--candidate",
            source,
            "--publish-authority",
            "--owner-identity",
            publisher.TRUSTED_OWNER_IDENTITY,
            "--decided-at-utc",
            "2026-09-20T12:00:00Z",
            "--rationale",
            "Approve only Story 7.1 entry after all current gate evidence passed.",
            "--preservation-evidence",
            publisher.PRESERVATION_EVIDENCE_PATH,
            "--performance-evidence",
            publisher.PERFORMANCE_EVIDENCE_PATH,
            "--landing-zone-evidence",
            publisher.LANDING_ZONE_EVIDENCE_PATH,
        ],
        capture_output=True,
        text=True,
        check=False,
    )

    assert completed.returncode == 0, completed.stderr
    assert "V23_STORY_7_1_ENTRY_AUTHORITY_WRITTEN" in completed.stdout
    assert (root / publisher.AUTHORITY_PATH).read_bytes() == authority_content
    document = json.loads(authority_content)
    request_content = publisher.candidate_blob(root, document["request"]["publicationCommit"], publisher.REQUEST_PATH)
    assert (root / publisher.ARCHITECTURE_PATH).read_bytes() == prefix + publisher.expected_v23_suffix(
        publisher.sha256(authority_content),
        publisher.sha256(request_content),
    )


def test_gate_evidence_source_rejects_unexpected_paths(tmp_path: Path) -> None:
    """The approval source may add only the declared gate-evidence paths."""

    root, _request_commit = candidate_repository(tmp_path)
    evidence_paths = {
        "preservation": publisher.PRESERVATION_EVIDENCE_PATH,
        "performance": publisher.PERFORMANCE_EVIDENCE_PATH,
        "landing": publisher.LANDING_ZONE_EVIDENCE_PATH,
    }
    runbook_content = recovery_runbook()
    runbook = root / publisher.RECOVERY_RUNBOOK_PATH
    runbook.parent.mkdir(parents=True, exist_ok=True)
    runbook.write_bytes(runbook_content)
    envelope = root / publisher.OPERATIONAL_ENVELOPE_PATH
    envelope.parent.mkdir(parents=True, exist_ok=True)
    envelope.write_bytes(operational_envelope(root, _request_commit, runbook_content))
    for path, gate_id in zip(
        evidence_paths.values(),
        ("PRESERVATION", "PERFORMANCE", "LANDING_ZONE"),
        strict=True,
    ):
        target = root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(pass_evidence(root, _request_commit, gate_id))
    unexpected = root / "unexpected-v23-source.txt"
    unexpected.write_text("unexpected\n", encoding="utf-8")
    source = commit(
        root,
        "test: inject V23 gate evidence scope drift",
        publisher.OPERATIONAL_ENVELOPE_PATH,
        publisher.RECOVERY_RUNBOOK_PATH,
        *evidence_paths.values(),
        *publisher.LANDING_ZONE_SUCCESSOR_PATHS.values(),
        "unexpected-v23-source.txt",
    )

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.render_authority(
            root,
            source,
            owner_identity=publisher.TRUSTED_OWNER_IDENTITY,
            decided_at_utc="2026-09-20T12:00:00Z",
            rationale="Approve only Story 7.1 entry after all current gate evidence passed.",
            preservation_evidence=evidence_paths["preservation"],
            performance_evidence=evidence_paths["performance"],
            landing_zone_evidence=evidence_paths["landing"],
        )

    assert error.value.code == "V23_GATE_EVIDENCE_SCOPE_DRIFT"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document.update({"unexpected": True}),
        lambda document: document.update({"gateId": "WRONG"}),
        lambda document: document.update({"result": "FAIL"}),
        lambda document: document.update({"assertionLedger": []}),
        lambda document: document["measurements"].update({"mismatchCount": 1}),
        lambda document: document["command"].update({"exitCode": 1}),
    ),
)
def test_gate_specific_evidence_rejects_closed_contract_faults(
    tmp_path: Path,
    mutation: Callable[[dict[str, object]], None],
) -> None:
    """Generic, contradictory, incomplete, and unexpected gate evidence cannot pass."""

    root, evaluated = candidate_repository(tmp_path)
    relative = "_bmad-output/planning-artifacts/fixture-preservation-pass.json"
    document = json.loads(pass_evidence(root, evaluated, "PRESERVATION"))
    mutation(document)
    target = root / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(publisher.json_bytes(document))
    source = commit(root, "test: add invalid preservation evidence", relative)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_pass_evidence(root, source, relative, "PRESERVATION", evaluated)

    assert error.value.code == "V23_PRESERVATION_EVIDENCE_INVALID"


def replace_command_output(document: dict[str, object], output: dict[str, object]) -> None:
    """Replace committed command output bytes and keep their declared digest internally consistent."""

    output_utf8 = publisher.json_bytes(output).decode("utf-8")
    command = document["command"]
    assert isinstance(command, dict)
    command["outputUtf8"] = output_utf8
    command["outputSha256"] = publisher.sha256(output_utf8.encode())
    ledger = document["assertionLedger"]
    assert isinstance(ledger, list)
    for row in ledger:
        assert isinstance(row, dict)
        row["outputSha256"] = command["outputSha256"]


@pytest.mark.parametrize(
    ("gate_id", "mutation"),
    (
        (
            "PERFORMANCE",
            lambda document, output: output["hotPaths"][0]["candidateSamples"].__setitem__(0, True),
        ),
        (
            "PERFORMANCE",
            lambda document, output: output["hotPaths"][0]["candidateSamples"].__setitem__(0, float("inf")),
        ),
        (
            "PERFORMANCE",
            lambda document, output: document["measurements"]["hotPaths"][0].update(
                {"observedRegressionPercent": -99.0}
            ),
        ),
        (
            "PERFORMANCE",
            lambda document, output: document["measurements"]["hotPaths"][0].update(
                {"observedRegressionPercent": False}
            ),
        ),
        (
            "PERFORMANCE",
            lambda document, output: document["measurements"]["hotPaths"][0].update(
                {"sampleCount": 30.0}
            ),
        ),
        (
            "PERFORMANCE",
            lambda document, output: output["hotPaths"][0].update(
                {"candidateSamples": [value * 2 for value in output["hotPaths"][0]["candidateSamples"]]}
            ),
        ),
        (
            "LANDING_ZONE",
            lambda document, output: output["decisions"][0].update({"decisionObjectSha256": "0" * 64}),
        ),
        (
            "LANDING_ZONE",
            lambda document, output: document["measurements"].update({"decisionsApproved": True}),
        ),
        (
            "LANDING_ZONE",
            lambda document, output: document["measurements"].update({"decisionsApproved": 6.0}),
        ),
        (
            "LANDING_ZONE",
            lambda document, output: document["measurements"].update({"unresolvedCount": False}),
        ),
        (
            "PRESERVATION",
            lambda document, output: document["measurements"].update({"evaluatedCount": True}),
        ),
        (
            "PRESERVATION",
            lambda document, output: document["measurements"].update({"mismatchCount": False}),
        ),
    ),
)
def test_each_gate_rejects_raw_measurement_identity_and_numeric_faults(
    tmp_path: Path,
    gate_id: str,
    mutation: Callable[[dict[str, object], dict[str, object]], None],
) -> None:
    """Canonical inventories, raw samples, booleans, and recomputed summaries fail closed."""

    root, evaluated = candidate_repository(tmp_path)
    relative = f"_bmad-output/planning-artifacts/fixture-{gate_id.casefold()}-pass.json"
    document = json.loads(pass_evidence(root, evaluated, gate_id))
    command = document["command"]
    assert isinstance(command, dict)
    output = json.loads(command["outputUtf8"])
    mutation(document, output)
    if output != json.loads(command["outputUtf8"]):
        replace_command_output(document, output)
    target = root / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(publisher.json_bytes(document))
    committed_paths = [relative]
    if gate_id == "LANDING_ZONE":
        committed_paths.extend(publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values())
        committed_paths.extend(publisher.LANDING_ZONE_SUCCESSOR_PATHS.values())
    source = commit(root, "test: add invalid gate evidence", *committed_paths)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_pass_evidence(root, source, relative, gate_id, evaluated)

    assert error.value.code == f"V23_{gate_id}_EVIDENCE_INVALID"


def test_gate_disposition_after_owner_decision_is_rejected(tmp_path: Path) -> None:
    """A future trusted gate disposition cannot authorize an earlier owner decision."""

    def mutate(gate_id: str, document: dict[str, object]) -> None:
        if gate_id == "PERFORMANCE":
            disposition = document["disposition"]
            assert isinstance(disposition, dict)
            disposition["decidedAtUtc"] = "2026-09-20T12:30:00Z"

    with pytest.raises(publisher.EntryAuthorityError) as error:
        authority_repository(tmp_path, evidence_mutation=mutate)

    assert error.value.code == "V23_PERFORMANCE_EVIDENCE_INVALID"


@pytest.mark.parametrize("performance_factor", (1.04, 1.05))
def test_nonzero_in_threshold_performance_evidence_passes(
    tmp_path: Path,
    performance_factor: float,
) -> None:
    """A finite nonzero regression at or below five percent is a valid performance alternative."""

    root, evaluated = candidate_repository(tmp_path)
    relative = "_bmad-output/planning-artifacts/fixture-performance-nonzero-pass.json"
    target = root / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(
        pass_evidence(root, evaluated, "PERFORMANCE", performance_factor=performance_factor)
    )
    source = commit(root, "test: add in-threshold performance evidence", relative)

    binding, disposition = publisher.validate_pass_evidence(
        root,
        source,
        relative,
        "PERFORMANCE",
        evaluated,
    )

    assert binding["path"] == relative
    assert disposition == publisher.utc_instant("2026-09-20T11:30:00Z", "FIXTURE")


def test_performance_rejects_any_exact_threshold_excess(tmp_path: Path) -> None:
    """The five-percent boundary has no hidden numeric epsilon above the approved limit."""

    root, evaluated = candidate_repository(tmp_path)
    relative = "_bmad-output/planning-artifacts/fixture-performance-epsilon-excess.json"
    target = root / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(pass_evidence(root, evaluated, "PERFORMANCE", performance_factor=1.050000000005))
    source = commit(root, "test: add exact-threshold performance excess", relative)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_pass_evidence(root, source, relative, "PERFORMANCE", evaluated)

    assert error.value.code == "V23_PERFORMANCE_EVIDENCE_INVALID"


@pytest.mark.parametrize("rationale", ("x" * 40, "x " * 20, "abc " * 10))
def test_owner_rationale_rejects_repeated_character_placeholders(rationale: str) -> None:
    """Length alone cannot make a repeated-character placeholder substantive."""

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_owner_fields(
            publisher.TRUSTED_OWNER_IDENTITY,
            "2026-09-20T12:00:00Z",
            rationale,
        )

    assert error.value.code == "V23_OWNER_RATIONALE_INVALID"


def test_operational_envelope_rejects_missing_responsibility_and_expired_waiver() -> None:
    """AD-5 requires explicit responsibilities and a valid dated waiver state."""

    valid = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    assert publisher.validate_operational_envelope(
        publisher.json_bytes(valid),
        decision_time=publisher.utc_instant("2026-09-20T12:00:00Z", "FIXTURE"),
        validation_time=publisher.utc_instant("2026-09-20T12:15:00Z", "FIXTURE"),
    ) == valid

    missing = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    missing["environments"][-1]["dependencies"].pop()
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(publisher.json_bytes(missing))
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"

    fixed = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    fixed["scaling"].update({"strategy": "FIXED", "minimumReplicas": 1, "maximumReplicas": 2})
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(publisher.json_bytes(fixed))
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"

    future = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    future["evaluatedAtUtc"] = "2026-09-20T12:30:00Z"
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(
            publisher.json_bytes(future),
            decision_time=publisher.utc_instant("2026-09-20T12:45:00Z", "FIXTURE"),
            validation_time=publisher.utc_instant("2026-09-20T12:15:00Z", "FIXTURE"),
        )
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"

    bad_runbook = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    bad_runbook["recovery"]["runbook"]["path"] = "docs/runbooks/evidence-boundary-validation.md"
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(publisher.json_bytes(bad_runbook))
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"

    bad_scaling = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    bad_scaling["scaling"]["maximumReplicas"] = 0
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(publisher.json_bytes(bad_scaling))
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"

    empty_horizontal_range = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    empty_horizontal_range["scaling"]["maximumReplicas"] = 1
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(publisher.json_bytes(empty_horizontal_range))
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"

    expired = json.loads(operational_envelope(ROOT, publisher.PROTECTED_MAIN))
    expired["waiver"] = {
        "state": "ACTIVE",
        "expiresAtUtc": "2026-09-20T11:30:00Z",
        "rationale": "temporary provider exception approved for recovery testing",
        "ownerIdentity": publisher.TRUSTED_OWNER_IDENTITY,
    }
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_operational_envelope(
            publisher.json_bytes(expired),
            decision_time=publisher.utc_instant("2026-09-20T12:00:00Z", "FIXTURE"),
        )
    assert error.value.code == "V23_OPERATIONAL_ENVELOPE_INVALID"


def test_operational_envelope_accepts_fixed_and_live_active_waiver_alternatives() -> None:
    """The closed envelope accepts exactly-one FIXED scaling and a live owner waiver."""

    decision_time = datetime(2026, 9, 20, 12, 0, tzinfo=timezone.utc)
    validation_time = datetime(2026, 9, 20, 12, 15, tzinfo=timezone.utc)
    fixed = operational_envelope(
        ROOT,
        publisher.PROTECTED_MAIN,
        scaling_strategy="FIXED",
    )
    active_waiver = operational_envelope(
        ROOT,
        publisher.PROTECTED_MAIN,
        waiver_state="ACTIVE",
    )

    assert publisher.validate_operational_envelope(
        fixed,
        decision_time=decision_time,
        validation_time=validation_time,
    )["scaling"] == {
        "minimumReplicas": 1,
        "maximumReplicas": 1,
        "strategy": "FIXED",
        "metric": "queue.depth",
        "scaleOutThreshold": 0.0,
        "scaleInThreshold": 0.0,
        "responsibleOwner": publisher.TRUSTED_OWNER_IDENTITY,
    }
    assert publisher.validate_operational_envelope(
        active_waiver,
        decision_time=decision_time,
        validation_time=validation_time,
    )["waiver"]["state"] == "ACTIVE"


def test_landing_zone_requires_new_approved_successors_over_blocked_predecessors(tmp_path: Path) -> None:
    """Blocked OQ-1 rows cannot be relabelled, inherited, duplicated, or invented as approval."""

    root, evaluated = candidate_repository(tmp_path)
    relative = "_bmad-output/planning-artifacts/fixture-landing-pass.json"
    document = json.loads(pass_evidence(root, evaluated, "LANDING_ZONE"))
    successor_path = publisher.LANDING_ZONE_SUCCESSOR_PATHS["FR-10"]
    successor = json.loads((root / successor_path).read_bytes())
    successor["state"] = "BLOCKED"
    (root / successor_path).write_bytes(publisher.json_bytes(successor))
    (root / relative).parent.mkdir(parents=True, exist_ok=True)
    (root / relative).write_bytes(publisher.json_bytes(document))
    source = commit(
        root,
        "test: retain blocked landing-zone predecessor",
        relative,
        *publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
        *publisher.LANDING_ZONE_SUCCESSOR_PATHS.values(),
    )

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_pass_evidence(root, source, relative, "LANDING_ZONE", evaluated)

    assert error.value.code == "V23_LANDING_ZONE_EVIDENCE_INVALID"


def test_landing_zone_rejects_ineffective_successor_grant(tmp_path: Path) -> None:
    """A decision object cannot promote an ineffective predecessor or successor grant."""

    root, evaluated = candidate_repository(tmp_path)
    relative = "_bmad-output/planning-artifacts/fixture-landing-pass.json"
    document = json.loads(pass_evidence(root, evaluated, "LANDING_ZONE"))
    grant_path = next(iter(publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values()))
    grant = json.loads((root / grant_path).read_bytes())
    grant["effective"] = False
    (root / grant_path).write_bytes(publisher.json_bytes(grant))
    (root / relative).parent.mkdir(parents=True, exist_ok=True)
    (root / relative).write_bytes(publisher.json_bytes(document))
    source = commit(
        root,
        "test: retain ineffective landing-zone grant",
        relative,
        *publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
        *publisher.LANDING_ZONE_SUCCESSOR_PATHS.values(),
    )

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_pass_evidence(root, source, relative, "LANDING_ZONE", evaluated)

    assert error.value.code == "V23_LANDING_ZONE_EVIDENCE_INVALID"


@pytest.mark.parametrize("grant_id", tuple(publisher.LANDING_ZONE_GRANT_PATHS))
def test_landing_zone_successor_grants_reject_stale_accepted_snapshots(
    tmp_path: Path,
    grant_id: str,
) -> None:
    """Effective grants bind current request gitlinks or the frozen Conversations source."""

    root, evaluated = candidate_repository(tmp_path)
    relative = "_bmad-output/planning-artifacts/fixture-landing-current-snapshot.json"
    document = json.loads(pass_evidence(root, evaluated, "LANDING_ZONE"))
    grant_path = publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS[grant_id]
    grant = json.loads((root / grant_path).read_bytes())
    predecessor = json.loads(
        publisher.candidate_blob(root, evaluated, publisher.LANDING_ZONE_GRANT_PATHS[grant_id])
    )
    grant["repository"]["acceptedSourceSnapshot"] = predecessor["repository"]["acceptedSourceSnapshot"]
    (root / grant_path).write_bytes(publisher.json_bytes(grant))
    (root / relative).parent.mkdir(parents=True, exist_ok=True)
    (root / relative).write_bytes(publisher.json_bytes(document))
    source = commit(
        root,
        "test: retain stale accepted source snapshot",
        relative,
        *publisher.LANDING_ZONE_SUCCESSOR_GRANT_PATHS.values(),
        *publisher.LANDING_ZONE_SUCCESSOR_PATHS.values(),
    )

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_pass_evidence(root, source, relative, "LANDING_ZONE", evaluated)

    assert error.value.code == "V23_LANDING_ZONE_EVIDENCE_INVALID"

def test_current_workflow_route_is_mechanically_validated() -> None:
    """The request's workflow gate is derived from the committed route inventory and workflow."""

    row = publisher.validate_workflow_route(ROOT, publisher.PROTECTED_MAIN)

    assert row["path"] == "_bmad-output/planning-artifacts/v22-workflow-route-inventory-v1.json"
    assert row["sha256"] == publisher.V22_ROUTE_SHA256


@pytest.mark.parametrize(
    "replacement",
    (
        "\n  pull_request_target:\n",
        "\n  pull_request_target:\n    branches: [develop]\n",
    ),
)
def test_successor_workflow_requires_exact_main_protected_pr_trigger(
    tmp_path: Path,
    replacement: str,
) -> None:
    """The base-owned trust event is accepted only for pull requests targeting main."""

    root, candidate = candidate_repository(tmp_path)
    assert publisher.validate_workflow_route(root, candidate)

    workflow = root / publisher.WORKFLOW_PATH
    content = workflow.read_text(encoding="utf-8")
    exact_trigger = "\n  pull_request_target:\n    branches: [main]\n"
    assert exact_trigger in content
    workflow.write_text(
        content.replace(exact_trigger, replacement, 1),
        encoding="utf-8",
    )
    candidate_controlled = commit(root, "test: inject candidate-controlled PR trigger", publisher.WORKFLOW_PATH)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_workflow_route(root, candidate_controlled)

    assert error.value.code == "V23_WORKFLOW_ROUTE_INVALID"


def test_successor_workflow_rejects_candidate_tooling_before_protected_hosts(tmp_path: Path) -> None:
    """Candidate-selected package execution cannot precede either protected host verdict."""

    root, candidate = candidate_repository(tmp_path)
    assert publisher.validate_workflow_route(root, candidate)

    workflow = root / publisher.WORKFLOW_PATH
    content = workflow.read_text(encoding="utf-8")
    protected_sync = "      - name: Synchronize protected planning verifier environment\n"
    assert protected_sync in content
    workflow.write_text(
        content.replace(
            protected_sync,
            "      - name: Unsafe candidate package install\n"
            "        run: npm ci --ignore-scripts\n\n"
            + protected_sync,
            1,
        ),
        encoding="utf-8",
    )
    unsafe_candidate = commit(root, "test: execute candidate tooling before trust hosts", publisher.WORKFLOW_PATH)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_workflow_route(root, unsafe_candidate)

    assert error.value.code == "V23_WORKFLOW_ROUTE_INVALID"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda content: content.replace(
            "TRUSTED_EVENT_BASE: ${{ github.event.pull_request.base.sha || github.event.before }}",
            "TRUSTED_EVENT_BASE: ${{ github.event.pull_request.head.sha || github.sha }}",
            1,
        ),
        lambda content: content.replace(
            "      - name: Synchronize protected planning verifier environment\n",
            "      - name: Hostile extra protected command\n        run: python3 -c 'print(1)'\n\n"
            "      - name: Synchronize protected planning verifier environment\n",
            1,
        ),
        lambda content: content.replace(
            'git show "$TRUSTED_HOST_COMMIT:$path"',
            'git show "$TRUSTED_EVENT_HEAD:$path"',
            1,
        ),
        lambda content: content.replace(
            "    name: protected-planning-authority-${{ github.event_name }}",
            "    name: protected-planning-authority",
            1,
        ),
        lambda content: content.replace(
            '"$protected_python" -I -P',
            "/usr/bin/python3 -I -P",
            1,
        ),
        lambda content: content.replace(
            'test "$observed" = "$TRUSTED_EVENT_HEAD"',
            'test "$observed" = "$TRUSTED_EVENT_BASE"',
            1,
        ),
        lambda content: content.replace(
            "uses: actions/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065",
            "uses: actions/setup-python@main",
            1,
        ),
        lambda content: content.replace("          allow-unsafe-pr-checkout: true\n", "", 1),
        lambda content: content.replace(
            "          env -i PATH=/usr/bin:/bin HOME=\"$RUNNER_TEMP/planning-authority-protected-home\" \\\n",
            "          env PATH=/usr/bin:/bin HOME=\"$RUNNER_TEMP/planning-authority-protected-home\" \\\n",
            1,
        ),
    ),
)
def test_successor_workflow_closed_identity_rejects_hostile_base_head_harness(
    tmp_path: Path,
    mutation: Callable[[str], str],
) -> None:
    """The runnable protected route rejects head-host, environment, and extra-command mutations."""

    root, candidate = candidate_repository(tmp_path)
    assert publisher.validate_workflow_route(root, candidate)
    workflow = root / publisher.WORKFLOW_PATH
    original = workflow.read_text(encoding="utf-8")
    hostile = mutation(original)
    assert hostile != original
    workflow.write_text(hostile, encoding="utf-8")
    hostile_candidate = commit(root, "test: mutate protected workflow identity", publisher.WORKFLOW_PATH)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_workflow_route(root, hostile_candidate)

    assert error.value.code == "V23_WORKFLOW_ROUTE_INVALID"


def test_successor_workflow_materializes_and_executes_protected_base_hosts(tmp_path: Path) -> None:
    """The workflow's Git-show contract executes base resolver/verifier bytes, never hostile head bytes."""

    root = tmp_path / "protected-materialization"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    base_hosts = {
        "_bmad/scripts/resolve_current_planning_authority.py": b"print('protected-base-resolver')\n",
        "_bmad/scripts/verify_evidence_boundary.py": b"print('protected-base-verifier')\n",
        "pyproject.toml": b"[project]\nname = 'protected-base'\nversion = '1.0.0'\n",
        "uv.lock": b"version = 1\nrevision = 1\n",
    }
    for relative_path, content in base_hosts.items():
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
    protected_base = commit(root, "test: add protected base hosts", *base_hosts)

    hostile_hosts = {
        "_bmad/scripts/resolve_current_planning_authority.py": b"raise SystemExit('hostile-head-resolver')\n",
        "_bmad/scripts/verify_evidence_boundary.py": b"raise SystemExit('hostile-head-verifier')\n",
    }
    for relative_path, content in hostile_hosts.items():
        (root / relative_path).write_bytes(content)
    hostile_head = commit(root, "test: replace protected hosts from head", *hostile_hosts)
    assert protected_base != hostile_head

    materialized = tmp_path / "materialized-hosts"
    materialize_protected_hosts(root, protected_base, materialized)
    for relative_path, expected in base_hosts.items():
        output = materialized / Path(relative_path).name
        assert output.read_bytes() == expected
        if relative_path in hostile_hosts:
            assert output.read_bytes() != subprocess.check_output(
                ["git", "-C", str(root), "show", f"{hostile_head}:{relative_path}"]
            )
    assert subprocess.check_output(
        [sys.executable, str(materialized / "resolve_current_planning_authority.py")],
        text=True,
    ).strip() == "protected-base-resolver"
    assert subprocess.check_output(
        [sys.executable, str(materialized / "verify_evidence_boundary.py")],
        text=True,
    ).strip() == "protected-base-verifier"


def test_synchronized_project_venv_imports_jsonschema_in_clean_environment(tmp_path: Path) -> None:
    """Exact workflow blocks materialize, sync, identify, and run protected hosts in isolation."""

    root = tmp_path / "production-shaped-protected-flow"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    repository_literal = repr(str(root))
    resolver_source = f"""import json
import subprocess
import sys

repository = {repository_literal}
candidate = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "rev-parse", "HEAD"], text=True
).strip()
baseline = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "rev-parse", "HEAD^"], text=True
).strip()
expected = ["--repository", repository, "--candidate", candidate, "--check"]
if sys.argv[1:] != expected:
    raise SystemExit(f"resolver arguments drifted: {{sys.argv[1:]!r}} != {{expected!r}}")
tree = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "rev-parse", candidate + "^{{tree}}"], text=True
).strip()
parent_tree = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "rev-parse", baseline + "^{{tree}}"], text=True
).strip()
changed = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "diff", "--name-only", baseline, candidate], text=True
).splitlines()
document = {{
    "schemaVersion": "hexalith.conversations.current-planning-authority-result.v1",
    "result": "PASS",
    "exitCode": 0,
    "effectiveHold": "ACTIVE",
    "implementationHold": "ACTIVE",
    "observed": {{
        "candidateCommit": candidate,
        "candidateTree": tree,
        "parentCommit": baseline,
        "parentTree": parent_tree,
        "changedPaths": changed,
        "parentGitlinks": [],
        "candidateGitlinks": [],
    }},
    "assertionLedger": [{{
        "id": "FIXTURE.RESOLVER",
        "subject": "exact-protected-resolver-cli",
        "state": "PASS",
        "detail": "resolver received the exact workflow-owned candidate arguments",
    }}],
    "blockers": [],
    "ownerApprovalClaimed": False,
    "releaseAuthorized": False,
    "pushAuthorized": False,
    "executionAllowed": False,
}}
print(json.dumps(document))
"""
    verifier_source = f"""import json
import subprocess
import sys

repository = {repository_literal}
candidate = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "rev-parse", "HEAD"], text=True
).strip()
baseline = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "rev-parse", "HEAD^"], text=True
).strip()
expected = ["--repository", repository, "--baseline", baseline, "--candidate", candidate]
if sys.argv[1:] != expected:
    raise SystemExit(f"verifier arguments drifted: {{sys.argv[1:]!r}} != {{expected!r}}")
changed = subprocess.check_output(
    ["/usr/bin/git", "-C", repository, "diff", "--name-only", baseline, candidate], text=True
).splitlines()
document = {{
    "schemaVersion": "hexalith.conversations.evidence-boundary-result.v1",
    "result": "PASS",
    "repository": repository,
    "baseline": baseline,
    "candidate": candidate,
    "changedPaths": changed,
    "worktreePaths": [],
    "assertionLedger": [{{
        "id": "FIXTURE.EVIDENCE",
        "subject": "exact-protected-verifier-cli",
        "state": "PASS",
        "detail": "verifier received the exact workflow-owned comparison range",
    }}],
    "blockers": [],
}}
print(json.dumps(document))
"""
    base_hosts = {
        "_bmad/scripts/resolve_current_planning_authority.py": resolver_source.encode(),
        "_bmad/scripts/verify_evidence_boundary.py": verifier_source.encode(),
        "pyproject.toml": (ROOT / "pyproject.toml").read_bytes(),
        "uv.lock": (ROOT / "uv.lock").read_bytes(),
    }
    for relative_path, content in base_hosts.items():
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
    protected_base = commit(root, "test: add protected production-shaped hosts", *base_hosts)
    for relative_path in (
        "_bmad/scripts/resolve_current_planning_authority.py",
        "_bmad/scripts/verify_evidence_boundary.py",
    ):
        (root / relative_path).write_text("raise SystemExit('hostile candidate host')\n", encoding="utf-8")
    hostile_head = commit(
        root,
        "test: add hostile candidate hosts",
        "_bmad/scripts/resolve_current_planning_authority.py",
        "_bmad/scripts/verify_evidence_boundary.py",
    )
    runner_temp = tmp_path / "runner-temp"
    runner_temp.mkdir()
    (runner_temp / "planning-authority-trusted-home").mkdir()
    hostile_imports = tmp_path / "hostile-imports"
    hostile_imports.mkdir()
    (hostile_imports / "json.py").write_text("raise SystemExit('hostile json')\n", encoding="utf-8")
    base_python = subprocess.check_output(
        [str(ROOT / ".venv/bin/python"), "-c", "import sys; print(sys._base_executable)"],
        text=True,
    ).strip()
    range_output = runner_temp / "range-output"
    environment = {
        "PATH": "/usr/bin:/bin",
        "RUNNER_TEMP": str(runner_temp),
        "GITHUB_WORKSPACE": str(root),
        "TRUSTED_EVENT_BASE": protected_base,
        "TRUSTED_EVENT_HEAD": hostile_head,
        "GITHUB_OUTPUT": str(range_output),
        "PROTECTED_BASE_PYTHON": base_python,
        "PROTECTED_UV": shutil.which("uv") or "",
        "PYTHONPATH": str(hostile_imports),
        "PYTHONHOME": str(tmp_path / "hostile-python-home"),
        "PYTHONSTARTUP": str(hostile_imports / "json.py"),
        "UV_PROJECT": str(tmp_path / "hostile-uv-project"),
        "UV_PYTHON": str(tmp_path / "hostile-python"),
    }
    assert environment["PROTECTED_UV"]

    subprocess.run(
        ["/bin/bash", "-c", workflow_run_block("Resolve immutable comparison range")],
        cwd=runner_temp,
        env=environment,
        check=True,
    )
    range_values = dict(
        line.split("=", 1) for line in range_output.read_text(encoding="utf-8").splitlines()
    )
    assert range_values == {
        "baseline": protected_base,
        "candidate": hostile_head,
        "trusted_host_commit": protected_base,
    }
    environment.update(
        {
            "TRUSTED_HOST_COMMIT": range_values["trusted_host_commit"],
            "RANGE_BASELINE": range_values["baseline"],
            "RANGE_CANDIDATE": range_values["candidate"],
        }
    )

    for step_name in (
        "Materialize protected planning-authority trust hosts",
        "Synchronize protected planning verifier environment",
        "Verify synchronized protected Python identity",
    ):
        subprocess.run(
            ["/bin/bash", "-c", workflow_run_block(step_name)],
            cwd=runner_temp,
            env=environment,
            check=True,
        )
    resolver_block = workflow_run_block("Resolve current planning authority through the protected host").replace(
        '"${{ steps.range.outputs.candidate }}"',
        '"$RANGE_CANDIDATE"',
    )
    evidence_block = workflow_run_block(
        "Verify lifecycle evidence and candidate-bound publication scope"
    ).replace('"${{ steps.range.outputs.baseline }}"', '"$RANGE_BASELINE"').replace(
        '"${{ steps.range.outputs.candidate }}"',
        '"$RANGE_CANDIDATE"',
    )
    subprocess.run(["/bin/bash", "-c", resolver_block], cwd=runner_temp, env=environment, check=True)
    evidence_completed = subprocess.run(
        ["/bin/bash", "-c", evidence_block],
        cwd=runner_temp,
        env=environment,
        check=True,
        capture_output=True,
        text=True,
    )

    resolver_result = json.loads(
        (runner_temp / "current-planning-authority-result.json").read_text(encoding="utf-8")
    )
    assert set(resolver_result) == {
        "schemaVersion",
        "result",
        "exitCode",
        "effectiveHold",
        "implementationHold",
        "observed",
        "assertionLedger",
        "blockers",
        "ownerApprovalClaimed",
        "releaseAuthorized",
        "pushAuthorized",
        "executionAllowed",
    }
    assert resolver_result["observed"]["candidateCommit"] == range_values["candidate"]
    assert resolver_result["observed"]["parentCommit"] == range_values["baseline"]
    assert resolver_result["assertionLedger"] == [
        {
            "id": "FIXTURE.RESOLVER",
            "subject": "exact-protected-resolver-cli",
            "state": "PASS",
            "detail": "resolver received the exact workflow-owned candidate arguments",
        }
    ]
    evidence_result = json.loads(evidence_completed.stdout)
    assert set(evidence_result) == {
        "schemaVersion",
        "result",
        "repository",
        "baseline",
        "candidate",
        "changedPaths",
        "worktreePaths",
        "assertionLedger",
        "blockers",
    }
    assert evidence_result["baseline"] == range_values["baseline"]
    assert evidence_result["candidate"] == range_values["candidate"]
    assert evidence_result["assertionLedger"] == [
        {
            "id": "FIXTURE.EVIDENCE",
            "subject": "exact-protected-verifier-cli",
            "state": "PASS",
            "detail": "verifier received the exact workflow-owned comparison range",
        }
    ]

    host_dir = runner_temp / "planning-authority-protected-host"
    assert (host_dir / "resolve_current_planning_authority.py").read_bytes() == base_hosts[
        "_bmad/scripts/resolve_current_planning_authority.py"
    ]
    assert (host_dir / "verify_evidence_boundary.py").read_bytes() == base_hosts[
        "_bmad/scripts/verify_evidence_boundary.py"
    ]


def test_candidate_checkout_identity_assertion_fails_on_runnable_mismatch(tmp_path: Path) -> None:
    """The post-checkout gate exits nonzero when HEAD differs from the event-selected SHA."""

    root, candidate = candidate_repository(tmp_path)
    unexpected = git(root, "rev-parse", f"{candidate}^")
    runner_temp = tmp_path / "runner-temp"
    runner_temp.mkdir()
    command = workflow_run_block("Verify checked-out candidate identity")
    completed = subprocess.run(
        ["/bin/bash", "-c", command],
        env={
            "PATH": "/usr/bin:/bin",
            "RUNNER_TEMP": str(runner_temp),
            "GITHUB_WORKSPACE": str(root),
            "TRUSTED_EVENT_HEAD": unexpected,
        },
        capture_output=True,
        text=True,
        check=False,
    )

    assert completed.returncode != 0


@pytest.mark.parametrize(
    ("mutation", "expected_code"),
    (
        (
            lambda document: document["gateDispositions"][0].update(
                {"evidence": "_bmad-output/implementation-artifacts/sprint-status.yaml"}
            ),
            "V23_DOCUMENT_SCHEMA_INVALID",
        ),
        (
            lambda document: document["publication"].update({"exactChangedPaths": [publisher.AUTHORITY_PATH]}),
            "V23_DOCUMENT_SCHEMA_INVALID",
        ),
        (
            lambda document: document["rootGitlinks"][0].update({"objectId": "0" * 40}),
            "V23_ROOT_GITLINK_DRIFT",
        ),
        (
            lambda document: document["sourceBindings"].pop(),
            "V23_DOCUMENT_SCHEMA_INVALID",
        ),
        (
            lambda document: document["ownerDecision"].update({"decidedAtUtc": "2099-01-01T00:00:00Z"}),
            "V23_OWNER_TIME_INVALID",
        ),
    ),
)
def test_authority_faults_fail_closed(
    tmp_path: Path,
    mutation: Callable[[dict[str, object]], None],
    expected_code: str,
) -> None:
    """Gate, scope, and raw-gitlink faults cannot retain execution permission."""

    root, _request_commit, publication, _authority_bytes = authority_repository(
        tmp_path,
        mutation=mutation,
    )
    result = publisher.resolve_published_authority(
        root,
        publication,
        signature_verifier=lambda _root, _commit, _identity: {
            "status": "G",
            "principal": publisher.TRUSTED_SSH_PRINCIPAL,
            "fingerprint": publisher.TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": publisher.TRUSTED_OWNER_IDENTITY,
        },
    )

    assert result["result"] in ("FAIL", "BLOCKED")
    assert result["executionAllowed"] is False
    assert result["assertionLedger"]
    assert result["blockers"][0]["code"] == expected_code


def publication_pair_fixture(tmp_path: Path, name: str) -> tuple[Path, dict[str, object], bytes, bytes]:
    """Create one minimal committed architecture source and exact target pair."""

    root = tmp_path / name
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    prefix = b"prefix\n"
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    architecture.write_bytes(prefix)
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    document: dict[str, object] = {"publication": {"sourceCommit": source}}
    return root, document, prefix, prefix + b"marker\n"


def test_publication_pair_restores_authority_when_marker_write_fails(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A failed second write does not leave a partial owner authority behind."""

    root = tmp_path / "write"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    architecture.write_bytes(b"prefix\n")
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    def failing_append(_base: Path, relative: str, _prefix: bytes, _suffix: bytes) -> None:
        raise publisher.EntryAuthorityError("V23_FIXTURE_WRITE_FAILED", relative, "BLOCKED")

    monkeypatch.setattr(publisher, "append_suffix_exact", failing_append)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            {"publication": {"sourceCommit": source}},
            b"prefix\nmarker\n",
        )

    assert error.value.code == "V23_FIXTURE_WRITE_FAILED"
    assert not (root / publisher.AUTHORITY_PATH).exists()
    assert architecture.read_bytes() == b"prefix\n"


def test_publication_pair_preserves_preexisting_identical_authority_when_marker_write_fails(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Rollback never deletes an identical authority that predated the attempted pair write."""

    root = tmp_path / "write-existing"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    architecture.write_bytes(b"prefix\n")
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True, exist_ok=True)
    document = {"publication": {"sourceCommit": source}}
    authority_content = publisher.json_bytes(document)
    authority.write_bytes(authority_content)
    def failing_append(_base: Path, relative: str, _prefix: bytes, _suffix: bytes) -> None:
        raise publisher.EntryAuthorityError("V23_FIXTURE_WRITE_FAILED", relative, "BLOCKED")

    monkeypatch.setattr(publisher, "append_suffix_exact", failing_append)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            document,
            b"prefix\nmarker\n",
        )

    assert error.value.code == "V23_FIXTURE_WRITE_FAILED"
    assert authority.read_bytes() == authority_content
    assert architecture.read_bytes() == b"prefix\n"


def test_publication_pair_rechecks_architecture_identity_after_authority_creation(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A concurrent architecture edit is preserved and blocks the append without replacing bytes."""

    root = tmp_path / "concurrent-append"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    architecture.write_bytes(b"prefix\n")
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    real_append = publisher.append_suffix_exact

    def concurrent_append(base: Path, relative: str, prefix: bytes, suffix: bytes) -> None:
        architecture.write_bytes(prefix + b"concurrent user edit\n")
        real_append(base, relative, prefix, suffix)

    monkeypatch.setattr(publisher, "append_suffix_exact", concurrent_append)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            {"publication": {"sourceCommit": source}},
            b"prefix\nmarker\n",
        )

    assert error.value.code == "V23_ARCHITECTURE_WORKTREE_DRIFT"
    assert architecture.read_bytes() == b"prefix\nconcurrent user edit\n"
    assert not (root / publisher.AUTHORITY_PATH).exists()


def test_architecture_append_retries_short_writes_to_exact_completion(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A short write is completed under the lock without truncating or duplicating bytes."""

    root = tmp_path / "short-write"
    root.mkdir()
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    prefix = b"prefix\n"
    suffix = b"complete marker suffix\n"
    architecture.write_bytes(prefix)
    real_write = publisher.os.write
    write_sizes: list[int] = []

    def short_write(descriptor: int, content: bytes | memoryview) -> int:
        chunk_size = max(1, len(content) // 2)
        write_sizes.append(chunk_size)
        return real_write(descriptor, content[:chunk_size])

    monkeypatch.setattr(publisher.os, "write", short_write)
    publisher.append_suffix_exact(root, publisher.ARCHITECTURE_PATH, prefix, suffix)

    assert len(write_sizes) > 1
    assert architecture.read_bytes() == prefix + suffix


def test_authority_creation_retries_short_writes_and_publishes_exact_pair(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """The exclusive authority-file path writes all bytes even when writes are short."""

    root, document, _prefix, architecture = publication_pair_fixture(tmp_path, "authority-short-write")
    real_write = publisher.os.write
    calls = 0

    def short_write(descriptor: int, content: bytes | memoryview) -> int:
        nonlocal calls
        calls += 1
        size = max(1, len(content) // 2)
        return real_write(descriptor, content[:size])

    monkeypatch.setattr(publisher.os, "write", short_write)
    publisher.publish_authority_pair(root, document, architecture)

    assert calls > 2
    assert (root / publisher.AUTHORITY_PATH).read_bytes() == publisher.json_bytes(document)
    assert (root / publisher.ARCHITECTURE_PATH).read_bytes() == architecture


def test_publication_pair_byte_identical_retry_is_idempotent(tmp_path: Path) -> None:
    """A fully published byte-identical pair can be retried without changing either inode."""

    root, document, _prefix, architecture = publication_pair_fixture(tmp_path, "pair-retry")
    publisher.publish_authority_pair(root, document, architecture)
    authority = root / publisher.AUTHORITY_PATH
    architecture_path = root / publisher.ARCHITECTURE_PATH
    identities = (authority.stat().st_ino, architecture_path.stat().st_ino)

    publisher.publish_authority_pair(root, document, architecture)

    assert (authority.stat().st_ino, architecture_path.stat().st_ino) == identities
    assert authority.read_bytes() == publisher.json_bytes(document)
    assert architecture_path.read_bytes() == architecture


@pytest.mark.parametrize("invalid_mode_path", (publisher.AUTHORITY_PATH, publisher.ARCHITECTURE_PATH))
def test_publication_pair_retry_rejects_executable_existing_files(
    tmp_path: Path,
    invalid_mode_path: str,
) -> None:
    """Byte-identical retry succeeds only while both governed files remain mode 100644."""

    root, document, _prefix, architecture = publication_pair_fixture(
        tmp_path,
        f"pair-mode-{Path(invalid_mode_path).name}",
    )
    publisher.publish_authority_pair(root, document, architecture)
    invalid = root / invalid_mode_path
    invalid.chmod(0o755)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(root, document, architecture)

    assert error.value.code in {"V23_WRITE_PATH_INVALID", "V23_ARCHITECTURE_APPEND_FAILED"}
    assert invalid.stat().st_mode & 0o777 == 0o755


def test_late_final_pair_validation_failure_restores_both_visible_paths(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A fault at the final joint check rolls back both paths after both appeared correct."""

    root, document, prefix, architecture = publication_pair_fixture(tmp_path, "late-pair-validation")

    def fail_final_validation(*_args: object, **_kwargs: object) -> None:
        authority = root / publisher.AUTHORITY_PATH
        architecture_path = root / publisher.ARCHITECTURE_PATH
        assert authority.read_bytes() == publisher.json_bytes(document)
        assert architecture_path.read_bytes() == architecture
        raise publisher.EntryAuthorityError(
            "V23_PUBLICATION_FINAL_IDENTITY_DRIFT",
            "fixture late joint validation failure",
            "BLOCKED",
        )

    monkeypatch.setattr(publisher, "validate_publication_pair", fail_final_validation)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(root, document, architecture)

    assert error.value.code == "V23_PUBLICATION_FINAL_IDENTITY_DRIFT"
    assert (root / publisher.ARCHITECTURE_PATH).read_bytes() == prefix
    assert not (root / publisher.AUTHORITY_PATH).exists()
    assert list((root / publisher.ARCHITECTURE_PATH).parent.glob(".architecture.md.rollback.*"))
    assert list((root / publisher.AUTHORITY_PATH).parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))


def test_authority_create_directory_fsync_failure_quarantines_partial_publication(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A create-directory durability fault leaves no visible authority and retains quarantine evidence."""

    root = tmp_path / "authority-create-fsync"
    root.mkdir()
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True)
    real_fsync = publisher.os.fsync
    failed = False

    def fail_first_directory_fsync(descriptor: int) -> None:
        nonlocal failed
        if not failed and publisher.stat.S_ISDIR(publisher.os.fstat(descriptor).st_mode):
            failed = True
            raise OSError("fixture create directory fsync failure")
        real_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", fail_first_directory_fsync)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.atomic_write(root, publisher.AUTHORITY_PATH, b"authority\n", no_clobber=True)

    assert error.value.code == "V23_WRITE_FAILED"
    assert failed is True
    assert not authority.exists()
    assert any(path.read_bytes() == b"authority\n" for path in authority.parent.glob(".*.rollback.*"))


def test_authority_initial_fstat_failure_quarantines_created_path(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """The first fallible operation after O_EXCL is inside rollback coverage."""

    root = tmp_path / "authority-initial-fstat"
    root.mkdir()
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True)
    real_fstat = publisher.os.fstat
    failed = False

    def fail_first_fstat(descriptor: int) -> os.stat_result:
        nonlocal failed
        if not failed:
            failed = True
            raise OSError("fixture initial authority fstat failure")
        return real_fstat(descriptor)

    monkeypatch.setattr(publisher.os, "fstat", fail_first_fstat)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.atomic_write(root, publisher.AUTHORITY_PATH, b"authority\n", no_clobber=True)

    assert error.value.code == "V23_WRITE_FAILED"
    assert "rollbackQuarantine=" in error.value.detail
    assert not authority.exists()
    quarantines = list(authority.parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))
    assert len(quarantines) == 1
    assert quarantines[0].read_bytes() == b""


def test_authority_post_create_hard_link_is_quarantined_without_erasing_alias(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A link-count race after O_EXCL blocks publication and preserves every inode name."""

    root = tmp_path / "authority-post-create-hard-link"
    root.mkdir()
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True)
    alias = tmp_path / "authority-external-alias.json"
    real_fstat = publisher.os.fstat
    linked = False

    def link_before_initial_observation(descriptor: int) -> os.stat_result:
        nonlocal linked
        metadata = real_fstat(descriptor)
        if not linked and publisher.stat.S_ISREG(metadata.st_mode):
            linked = True
            os.link(authority, alias)
            metadata = real_fstat(descriptor)
        return metadata

    monkeypatch.setattr(publisher.os, "fstat", link_before_initial_observation)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.atomic_write(root, publisher.AUTHORITY_PATH, b"authority\n", no_clobber=True)

    assert error.value.code == "V23_WRITE_PATH_INVALID"
    assert "V23_ROLLBACK_OWNERSHIP_RACE" in error.value.detail
    assert "preserved=" in error.value.detail
    assert not authority.exists()
    assert alias.read_bytes() == b""
    assert list(authority.parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))


def test_quarantine_open_failure_reports_exact_preserved_path(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Every post-rename rollback fault names the quarantine that retains the bytes."""

    root = tmp_path / "quarantine-open-fault"
    root.mkdir()
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True)
    authority.write_bytes(b"authority\n")
    identity = (authority.stat().st_dev, authority.stat().st_ino)
    real_open = publisher.os.open

    def fail_quarantine_open(path: object, flags: int, *args: object, **kwargs: object) -> int:
        if isinstance(path, str) and path.startswith(".v23-story-7.1-entry-authority-v1.json.rollback."):
            raise OSError("fixture quarantine open failure")
        return real_open(path, flags, *args, **kwargs)

    monkeypatch.setattr(publisher.os, "open", fail_quarantine_open)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.remove_owned_file(root, publisher.AUTHORITY_PATH, b"authority\n", identity)

    assert error.value.code == "V23_ROLLBACK_FAILED"
    quarantines = list(authority.parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))
    assert len(quarantines) == 1
    assert str(quarantines[0].relative_to(root)) in error.value.detail
    assert quarantines[0].read_bytes() == b"authority\n"


def test_post_marker_directory_fsync_failure_restores_both_visible_paths(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A fault after marker installation restores the prefix and removes the visible authority."""

    root, document, prefix, architecture = publication_pair_fixture(tmp_path, "pair-directory-fsync")
    real_fsync = publisher.os.fsync
    directory_calls = 0

    def fail_second_directory_fsync(descriptor: int) -> None:
        nonlocal directory_calls
        if publisher.stat.S_ISDIR(publisher.os.fstat(descriptor).st_mode):
            directory_calls += 1
            if directory_calls == 2:
                raise OSError("fixture post-marker directory fsync failure")
        real_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", fail_second_directory_fsync)
    with pytest.raises(OSError, match="post-marker directory fsync failure"):
        publisher.publish_authority_pair(root, document, architecture)

    assert (root / publisher.ARCHITECTURE_PATH).read_bytes() == prefix
    assert not (root / publisher.AUTHORITY_PATH).exists()
    assert list((root / publisher.ARCHITECTURE_PATH).parent.glob(".architecture.md.rollback.*"))
    assert list((root / publisher.AUTHORITY_PATH).parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))


def test_authority_rollback_directory_fsync_failure_preserves_originating_blocker(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A rollback-directory fault retains quarantine and remains diagnostic context."""

    root, document, prefix, architecture = publication_pair_fixture(tmp_path, "rollback-directory-fsync")
    real_fsync = publisher.os.fsync
    directory_calls = 0

    def fail_second_directory_fsync(descriptor: int) -> None:
        nonlocal directory_calls
        if publisher.stat.S_ISDIR(publisher.os.fstat(descriptor).st_mode):
            directory_calls += 1
            if directory_calls == 2:
                raise OSError("fixture rollback directory fsync failure")
        real_fsync(descriptor)

    def failing_append(_root: Path, relative: str, _prefix: bytes, _suffix: bytes) -> tuple[bool, tuple[int, int]]:
        raise publisher.EntryAuthorityError("V23_ORIGINAL_APPEND_BLOCKER", relative, "BLOCKED")

    monkeypatch.setattr(publisher.os, "fsync", fail_second_directory_fsync)
    monkeypatch.setattr(publisher, "append_suffix_exact", failing_append)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(root, document, architecture)

    assert error.value.code == "V23_ORIGINAL_APPEND_BLOCKER"
    assert "authorityRollbackError=V23_ROLLBACK_FAILED" in error.value.detail
    assert (root / publisher.ARCHITECTURE_PATH).read_bytes() == prefix
    assert not (root / publisher.AUTHORITY_PATH).exists()
    assert list((root / publisher.AUTHORITY_PATH).parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))


def test_architecture_append_restores_prefix_when_fsync_fails(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A durability failure restores both visible paths and quarantines appended bytes."""

    root = tmp_path / "fsync-failure"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    prefix = b"prefix\n"
    suffix = b"partial marker must roll back\n"
    architecture.write_bytes(prefix)
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    architecture_inode = architecture.stat().st_ino
    real_fsync = publisher.os.fsync
    failed = False

    def fail_architecture_fsync(descriptor: int) -> None:
        nonlocal failed
        if not failed and publisher.os.fstat(descriptor).st_ino == architecture_inode:
            failed = True
            raise OSError("fixture fsync failure")
        real_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", fail_architecture_fsync)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            {"publication": {"sourceCommit": source}},
            prefix + suffix,
        )

    assert error.value.code == "V23_ARCHITECTURE_APPEND_FAILED"
    assert "fixture fsync failure" in error.value.detail
    assert failed is True
    assert architecture.read_bytes() == prefix
    assert not (root / publisher.AUTHORITY_PATH).exists()
    quarantines = list(architecture.parent.glob(".architecture.md.rollback.*"))
    assert len(quarantines) == 1
    assert quarantines[0].read_bytes() == prefix + suffix


def test_publication_rollback_failure_preserves_originating_blocker(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A rollback fault is diagnostic context and never replaces the marker-write blocker."""

    root = tmp_path / "rollback-fault"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    architecture.write_bytes(b"prefix\n")
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)

    def failing_append(_base: Path, relative: str, _prefix: bytes, _suffix: bytes) -> None:
        raise publisher.EntryAuthorityError("V23_ORIGINAL_APPEND_BLOCKER", relative, "BLOCKED")

    def failing_remove(
        _root: Path,
        _relative_path: str,
        _expected_content: bytes,
        _expected_identity: tuple[int, int],
    ) -> None:
        raise publisher.EntryAuthorityError("V23_ROLLBACK_OWNERSHIP_RACE", "fixture rollback failure", "BLOCKED")

    monkeypatch.setattr(publisher, "append_suffix_exact", failing_append)
    monkeypatch.setattr(publisher, "remove_owned_file", failing_remove)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            {"publication": {"sourceCommit": source}},
            b"prefix\nmarker\n",
        )

    assert error.value.code == "V23_ORIGINAL_APPEND_BLOCKER"
    assert "authorityRollbackError=" in error.value.detail


def test_publication_rejects_symlink_targets_without_touching_external_bytes(tmp_path: Path) -> None:
    """No-follow publication cannot append through architecture or authority symlinks."""

    root = tmp_path / "symlink-publication"
    root.mkdir()
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    external = tmp_path / "external-architecture.md"
    external.write_bytes(b"external bytes\n")
    architecture.symlink_to(external)

    with pytest.raises(publisher.EntryAuthorityError):
        publisher.append_suffix_exact(root, publisher.ARCHITECTURE_PATH, b"external bytes\n", b"marker\n")
    assert external.read_bytes() == b"external bytes\n"

    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True, exist_ok=True)
    external_authority = tmp_path / "external-authority.json"
    external_authority.write_bytes(b"other writer\n")
    authority.symlink_to(external_authority)
    with pytest.raises(publisher.EntryAuthorityError):
        publisher.atomic_write(root, publisher.AUTHORITY_PATH, b"ours\n", no_clobber=True)
    assert external_authority.read_bytes() == b"other writer\n"


def test_publication_rejects_hardlinked_architecture_and_authority_retry(tmp_path: Path) -> None:
    """A multiply linked governed inode cannot mutate or authenticate an external alias."""

    root = tmp_path / "hardlink-publication"
    root.mkdir()
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    external_architecture = tmp_path / "external-architecture-hardlink.md"
    external_architecture.write_bytes(b"prefix\n")
    os.link(external_architecture, architecture)

    with pytest.raises(publisher.EntryAuthorityError) as architecture_error:
        publisher.append_suffix_exact(root, publisher.ARCHITECTURE_PATH, b"prefix\n", b"marker\n")
    assert architecture_error.value.code == "V23_ARCHITECTURE_APPEND_FAILED"
    assert external_architecture.read_bytes() == b"prefix\n"

    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True, exist_ok=True)
    external_authority = tmp_path / "external-authority-hardlink.json"
    external_authority.write_bytes(b"authority\n")
    os.link(external_authority, authority)
    with pytest.raises(publisher.EntryAuthorityError) as authority_error:
        publisher.atomic_write(root, publisher.AUTHORITY_PATH, b"authority\n", no_clobber=True)
    assert authority_error.value.code == "V23_WRITE_PATH_INVALID"
    assert external_authority.read_bytes() == b"authority\n"


def test_publication_lock_timeout_is_bounded_and_stable(tmp_path: Path) -> None:
    """A stalled cooperative holder produces a stable blocker instead of an unbounded hang."""

    target = tmp_path / "locked"
    target.write_bytes(b"locked\n")
    first = os.open(target, os.O_RDONLY)
    second = os.open(target, os.O_RDONLY)
    try:
        publisher.fcntl.flock(first, publisher.fcntl.LOCK_EX | publisher.fcntl.LOCK_NB)
        started = publisher.time.monotonic()
        with pytest.raises(publisher.EntryAuthorityError) as error:
            publisher.acquire_bounded_lock(second, timeout_seconds=0.02)
        elapsed = publisher.time.monotonic() - started
    finally:
        os.close(second)
        os.close(first)

    assert error.value.code == "V23_PUBLICATION_LOCK_TIMEOUT"
    assert elapsed < 1.0


def test_final_pair_validation_rereads_authority_after_architecture(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A same-inode authority mutation during the architecture read is caught by A2."""

    root, document, _prefix, architecture = publication_pair_fixture(tmp_path, "authority-a1-b-a2")
    authority_content = publisher.json_bytes(document)
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True, exist_ok=True)
    authority.write_bytes(authority_content)
    architecture_path = root / publisher.ARCHITECTURE_PATH
    architecture_path.write_bytes(architecture)
    authority_identity = (authority.stat().st_dev, authority.stat().st_ino)
    architecture_identity = (architecture_path.stat().st_dev, architecture_path.stat().st_ino)
    real_observation = publisher.publication_observation
    authority_seen = False

    def mutate_between_reads(
        parent_descriptor: int,
        name: str,
        descriptor: int,
        relative_path: str,
    ) -> tuple[bytes, tuple[int, int, int, int, int, int, int]]:
        nonlocal authority_seen
        if relative_path == publisher.AUTHORITY_PATH and not authority_seen:
            authority_seen = True
        elif relative_path == publisher.ARCHITECTURE_PATH:
            assert authority_seen
            replacement = b"X" + authority_content[1:]
            writer = publisher.os.open(authority, publisher.os.O_WRONLY)
            try:
                publisher.os.pwrite(writer, replacement, 0)
                publisher.os.fsync(writer)
            finally:
                publisher.os.close(writer)
        return real_observation(parent_descriptor, name, descriptor, relative_path)

    monkeypatch.setattr(publisher, "publication_observation", mutate_between_reads)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.validate_publication_pair(
            root,
            authority_content,
            authority_identity,
            architecture,
            architecture_identity,
        )

    assert error.value.code == "V23_PUBLICATION_FINAL_IDENTITY_DRIFT"


@pytest.mark.parametrize("race_point", ("authority-a1", "architecture-b", "authority-a2"))
def test_final_pair_observations_reject_mid_read_hard_link_races(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    race_point: str,
) -> None:
    """Each A1/B/A2 observation blocks a new external alias without erasing it."""

    root, document, _prefix, architecture = publication_pair_fixture(tmp_path, f"hard-link-{race_point}")
    real_observation = publisher.publication_observation
    authority_observations = 0
    alias = tmp_path / f"{race_point}-alias"

    def link_during_observation(
        parent_descriptor: int,
        name: str,
        descriptor: int,
        relative_path: str,
    ) -> tuple[bytes, tuple[int, int, int, int, int, int, int]]:
        nonlocal authority_observations
        should_link = False
        if relative_path == publisher.AUTHORITY_PATH:
            authority_observations += 1
            should_link = race_point == f"authority-a{authority_observations}"
        elif relative_path == publisher.ARCHITECTURE_PATH:
            should_link = race_point == "architecture-b"
        if should_link:
            os.link(root / relative_path, alias)
        return real_observation(parent_descriptor, name, descriptor, relative_path)

    monkeypatch.setattr(publisher, "publication_observation", link_during_observation)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(root, document, architecture)

    assert error.value.code == "V23_PUBLICATION_FINAL_IDENTITY_DRIFT"
    assert alias.exists()
    assert alias.read_bytes() in (publisher.json_bytes(document), architecture)
    assert "RollbackError=" in error.value.detail


def test_architecture_rollback_preserves_recreated_destination(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """No-replace restoration never overwrites a pathname recreated during rollback."""

    root = tmp_path / "rollback-no-replace"
    root.mkdir()
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    appended = b"prefix\nmarker\n"
    architecture.write_bytes(appended)
    identity = (architecture.stat().st_dev, architecture.stat().st_ino)
    real_rename = publisher.rename_no_replace
    renamed = False

    def recreate_after_quarantine(parent_descriptor: int, source: str, destination: str) -> None:
        nonlocal renamed
        real_rename(parent_descriptor, source, destination)
        if not renamed:
            renamed = True
            descriptor = os.open(
                source,
                os.O_WRONLY | os.O_CREAT | os.O_EXCL,
                0o644,
                dir_fd=parent_descriptor,
            )
            try:
                os.write(descriptor, b"other writer\n")
                os.fsync(descriptor)
            finally:
                os.close(descriptor)

    monkeypatch.setattr(publisher, "rename_no_replace", recreate_after_quarantine)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.rollback_architecture_append(
            root,
            publisher.ARCHITECTURE_PATH,
            b"prefix\n",
            identity,
        )

    assert error.value.code == "V23_WRITE_WOULD_CLOBBER"
    assert architecture.read_bytes() == b"other writer\n"
    quarantines = list(architecture.parent.glob(".architecture.md.rollback.*"))
    assert len(quarantines) == 1
    assert quarantines[0].read_bytes() == appended
    assert f"preserved={quarantines[0].relative_to(root)}" in error.value.detail


def test_architecture_noncooperating_writer_is_preserved_and_blocks_rollback(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A writer racing after append is retained; rollback never truncates its bytes."""

    root = tmp_path / "architecture-race"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    prefix = b"prefix\n"
    suffix = b"marker\n"
    architecture.write_bytes(prefix)
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    architecture_inode = architecture.stat().st_ino
    real_fsync = publisher.os.fsync
    raced = False

    def race_after_write(descriptor: int) -> None:
        nonlocal raced
        if not raced and publisher.os.fstat(descriptor).st_ino == architecture_inode:
            raced = True
            with architecture.open("ab") as stream:
                stream.write(b"other writer\n")
                stream.flush()
        real_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", race_after_write)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            {"publication": {"sourceCommit": source}},
            prefix + suffix,
        )

    assert error.value.code == "V23_ARCHITECTURE_FINAL_IDENTITY_DRIFT"
    assert "architectureQuarantine=" in error.value.detail
    assert architecture.read_bytes() == prefix
    quarantines = list(architecture.parent.glob(".architecture.md.rollback.*"))
    assert len(quarantines) == 1
    assert quarantines[0].read_bytes() == prefix + suffix + b"other writer\n"


def test_authority_replacement_race_is_preserved_during_pair_rollback(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A replacement authority inode is never unlinked when marker publication fails."""

    root = tmp_path / "authority-race"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    architecture = root / publisher.ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True)
    architecture.write_bytes(b"prefix\n")
    source = commit(root, "test: add architecture source", publisher.ARCHITECTURE_PATH)
    replacement = b"other writer authority\n"

    def replace_then_fail(_base: Path, _relative: str, _prefix: bytes, _suffix: bytes) -> None:
        authority = root / publisher.AUTHORITY_PATH
        displaced = root / "displaced-authority"
        authority.rename(displaced)
        authority.write_bytes(replacement)
        raise publisher.EntryAuthorityError("V23_ORIGINAL_APPEND_BLOCKER", "fixture", "BLOCKED")

    monkeypatch.setattr(publisher, "append_suffix_exact", replace_then_fail)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(
            root,
            {"publication": {"sourceCommit": source}},
            b"prefix\nmarker\n",
        )

    assert error.value.code == "V23_ORIGINAL_APPEND_BLOCKER"
    assert "V23_ROLLBACK_OWNERSHIP_RACE" in error.value.detail
    assert not (root / publisher.AUTHORITY_PATH).exists()
    quarantines = list((root / publisher.AUTHORITY_PATH).parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))
    assert any(path.read_bytes() == replacement for path in quarantines)


def test_authority_same_inode_mutation_is_preserved_when_atomic_write_fails(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Rollback compares only process-written bytes and preserves a same-inode writer's replacement."""

    root = tmp_path / "authority-same-inode-race"
    root.mkdir()
    authority = root / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True)
    replacement = b"same-inode concurrent writer\n"
    real_fsync = publisher.os.fsync

    def mutate_same_inode(descriptor: int) -> None:
        publisher.os.pwrite(descriptor, replacement, 0)
        publisher.os.ftruncate(descriptor, len(replacement))
        real_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", mutate_same_inode)
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.atomic_write(
            root,
            publisher.AUTHORITY_PATH,
            b"publication bytes owned by this process\n",
            no_clobber=True,
        )

    assert error.value.code == "V23_WRITE_FAILED"
    assert "V23_ROLLBACK_OWNERSHIP_RACE" in error.value.detail
    assert not authority.exists()
    quarantines = list(authority.parent.glob(".v23-story-7.1-entry-authority-v1.json.rollback.*"))
    assert any(path.read_bytes() == replacement for path in quarantines)


def test_unavailable_history_is_blocked_with_nonempty_ledger(tmp_path: Path) -> None:
    """Environmental inability never becomes PASS or an empty result."""

    result = publisher.fail_result("V23_HISTORY_UNAVAILABLE", str(tmp_path), "BLOCKED")

    assert result["result"] == "BLOCKED"
    assert result["executionAllowed"] is False
    assert result["assertionLedger"]


def test_publication_pair_rejects_dirty_architecture_without_clobbering(tmp_path: Path) -> None:
    """Pre-existing architecture edits remain byte-identical and prevent publication."""

    root, _request, publication, authority_bytes = authority_repository(tmp_path)
    document = json.loads(authority_bytes)
    source = document["publication"]["sourceCommit"]
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", source], check=True)
    architecture_path = root / publisher.ARCHITECTURE_PATH
    dirty = architecture_path.read_bytes() + b"dirty user edit\n"
    architecture_path.write_bytes(dirty)
    rendered = publisher.candidate_blob(root, publication, publisher.ARCHITECTURE_PATH)

    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.publish_authority_pair(root, document, rendered)

    assert error.value.code == "V23_ARCHITECTURE_WORKTREE_DRIFT"
    assert architecture_path.read_bytes() == dirty
    assert not (root / publisher.AUTHORITY_PATH).exists()


def signed_fixture_repository(tmp_path: Path) -> tuple[Path, str, str, str, str]:
    """Create one real SSH-signed commit and return its explicit trust facts."""

    root = tmp_path / "signed"
    root.mkdir()
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    owner = "Fixture Owner <fixture@example.invalid>"
    principal = "fixture@example.invalid"
    git(root, "config", "user.name", "Fixture Owner")
    git(root, "config", "user.email", "fixture@example.invalid")
    private_key = tmp_path / "fixture_signing_key"
    subprocess.run(
        ["/usr/bin/ssh-keygen", "-q", "-t", "ed25519", "-N", "", "-f", str(private_key)],
        check=True,
    )
    public_key = (tmp_path / "fixture_signing_key.pub").read_text(encoding="utf-8").strip()
    fingerprint = subprocess.check_output(
        ["/usr/bin/ssh-keygen", "-lf", str(tmp_path / "fixture_signing_key.pub"), "-E", "sha256"],
        text=True,
    ).split()[1]
    git(root, "config", "gpg.format", "ssh")
    git(root, "config", "user.signingkey", str(private_key))
    target = root / "signed.txt"
    target.write_text("signed\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "signed.txt"], check=True)
    subprocess.run(["git", "-C", str(root), "commit", "-q", "-S", "-m", "test: signed fixture"], check=True)
    return root, git(root, "rev-parse", "HEAD"), owner, principal, public_key + "|" + fingerprint


def test_real_ssh_signature_accepts_only_explicit_trust_anchor(tmp_path: Path) -> None:
    """Exercise real Git SSH verification, signer, fingerprint, revocation, and tool pins."""

    root, publication, owner, principal, packed = signed_fixture_repository(tmp_path)
    public_key, fingerprint = packed.split("|", 1)
    facts = publisher.verify_publication_signature(
        root,
        publication,
        owner,
        trusted_owner_identity=owner,
        trusted_principal=principal,
        trusted_public_key=public_key,
        trusted_fingerprint=fingerprint,
        ssh_keygen_path="/usr/bin/ssh-keygen",
    )
    assert facts == {
        "status": "G",
        "principal": principal,
        "fingerprint": fingerprint,
        "authorIdentity": owner,
    }

    with pytest.raises(publisher.EntryAuthorityError):
        publisher.verify_publication_signature(
            root,
            publication,
            owner,
            trusted_owner_identity=owner,
            trusted_principal="wrong@example.invalid",
            trusted_public_key=public_key,
            trusted_fingerprint=fingerprint,
            ssh_keygen_path="/usr/bin/ssh-keygen",
            allowed_signer_principal=principal,
        )
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.verify_publication_signature(
            root,
            publication,
            owner,
            trusted_owner_identity=owner,
            trusted_principal=principal,
            trusted_public_key=public_key,
            trusted_fingerprint="SHA256:wrong",
            ssh_keygen_path="/usr/bin/ssh-keygen",
        )
    assert error.value.code == "V23_PUBLICATION_SIGNER_INVALID"
    with pytest.raises(publisher.EntryAuthorityError):
        publisher.verify_publication_signature(
            root,
            publication,
            owner,
            trusted_owner_identity=owner,
            trusted_principal=principal,
            trusted_public_key=public_key,
            trusted_fingerprint=fingerprint,
            ssh_keygen_path="/usr/bin/ssh-keygen",
            revoked_public_keys=(public_key,),
        )
    with pytest.raises(publisher.EntryAuthorityError) as error:
        publisher.verify_publication_signature(
            root,
            publication,
            owner,
            trusted_owner_identity=owner,
            trusted_principal=principal,
            trusted_public_key=public_key,
            trusted_fingerprint=fingerprint,
            ssh_keygen_path=str(tmp_path / "missing-ssh-keygen"),
        )
    assert error.value.code == "V23_TRUST_TOOL_UNAVAILABLE"


def test_full_authority_resolution_invokes_real_signature_verifier_with_exact_publication(
    tmp_path: Path,
) -> None:
    """The decisive resolution path verifies the exact authority commit and owner identity."""

    private_key = tmp_path / "authority_signing_key"
    subprocess.run(
        [publisher.SSH_KEYGEN_EXECUTABLE, "-q", "-t", "ed25519", "-N", "", "-f", str(private_key)],
        check=True,
    )
    public_key = private_key.with_suffix(".pub").read_text(encoding="utf-8").strip()
    fingerprint = subprocess.check_output(
        [publisher.SSH_KEYGEN_EXECUTABLE, "-lf", str(private_key.with_suffix(".pub")), "-E", "sha256"],
        text=True,
    ).split()[1]
    root, _request, publication, _authority = authority_repository(tmp_path / "authority", signing_key=private_key)
    observed: list[tuple[Path, str, str]] = []

    def verify(root_arg: Path, publication_arg: str, owner_arg: str) -> dict[str, str]:
        observed.append((root_arg, publication_arg, owner_arg))
        facts = publisher.verify_publication_signature(
            root_arg,
            publication_arg,
            owner_arg,
            trusted_public_key=public_key,
            trusted_fingerprint=fingerprint,
            ssh_keygen_path=publisher.SSH_KEYGEN_EXECUTABLE,
        )
        assert facts["status"] == "G"
        return {
            "status": "G",
            "principal": publisher.TRUSTED_SSH_PRINCIPAL,
            "fingerprint": publisher.TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": publisher.TRUSTED_OWNER_IDENTITY,
        }

    result = publisher.resolve_published_authority(root, publication, signature_verifier=verify)

    assert result["result"] == "PASS"
    assert observed == [(root, publication, publisher.TRUSTED_OWNER_IDENTITY)]
