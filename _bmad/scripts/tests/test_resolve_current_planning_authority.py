"""Raw-object, schema, workflow, and fault tests for the V22 resolver."""

from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
from typing import Callable

from jsonschema import Draft202012Validator
from jsonschema.exceptions import ValidationError
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/resolve_current_planning_authority.py"
SPEC = importlib.util.spec_from_file_location("resolve_current_planning_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
resolver = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(resolver)


def git(root: Path, *arguments: str) -> str:
    """Run Git in a fixture repository and return stripped stdout."""

    return subprocess.check_output(
        ["git", "-C", str(root), *arguments],
        text=True,
    ).strip()


def commit(root: Path, message: str) -> str:
    """Commit the fixture index without signing."""

    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "commit", "-q", "-m", message],
        check=True,
    )
    return git(root, "rev-parse", "HEAD")


def copy_candidate_files(root: Path) -> None:
    """Copy the eight V22 transaction paths into a detached parent fixture."""

    for relative_path in resolver.EXPECTED_PATHS:
        source = ROOT / relative_path
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)


Mutation = Callable[[Path], None]


def candidate_repository(
    tmp_path: Path,
    *,
    mutate: Mutation | None = None,
    extra_path: bool = False,
    executable_path: str | None = None,
) -> tuple[Path, str]:
    """Create one direct-child V22 candidate from the selected parent."""

    root = tmp_path / "candidate"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", resolver.EXPECTED_PARENT], check=True)
    git(root, "config", "user.name", "V22 fixture")
    git(root, "config", "user.email", "v22-fixture@example.invalid")
    copy_candidate_files(root)
    if mutate is not None:
        mutate(root)
    if extra_path:
        (root / "unexpected-v22-path.txt").write_text("unexpected\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", *resolver.EXPECTED_PATHS], check=True)
    if executable_path is not None:
        subprocess.run(["git", "-C", str(root), "update-index", "--chmod=+x", executable_path], check=True)
    if extra_path:
        subprocess.run(["git", "-C", str(root), "add", "unexpected-v22-path.txt"], check=True)
    return root, commit(root, "fix(planning): publish V22 current authority recovery")


def schema() -> dict[str, object]:
    """Load the trusted checked-in recovery/result schema."""

    return json.loads((ROOT / resolver.RECOVERY_SCHEMA_PATH).read_text(encoding="utf-8"))


def validate_result(document: dict[str, object]) -> None:
    """Validate one result against the closed result branch."""

    Draft202012Validator(schema()).validate(document)
    assert document["effectiveHold"] == "ACTIVE"
    assert document["implementationHold"] == "ACTIVE"
    assert document["assertionLedger"]
    assert document["ownerApprovalClaimed"] is False
    assert document["releaseAuthorized"] is False
    assert document["pushAuthorized"] is False
    assert document["executionAllowed"] is False


@pytest.fixture(scope="module")
def valid_candidate(tmp_path_factory: pytest.TempPathFactory) -> tuple[Path, str]:
    """Build one reusable valid V22 candidate."""

    return candidate_repository(tmp_path_factory.mktemp("v22-valid"))


def test_valid_candidate_and_committed_main_are_schema_valid_pass(
    valid_candidate: tuple[Path, str],
) -> None:
    """Cover the valid-candidate and post-fast-forward committed-main matrix rows."""

    root, candidate = valid_candidate
    direct = resolver.resolve_authority(root, candidate)
    validate_result(direct)
    assert direct["result"] == "PASS"
    assert direct["exitCode"] == 0
    assert {row["state"] for row in direct["assertionLedger"]} == {"PASS"}
    assert direct["blockers"] == []
    assert direct["observed"]["candidateCommit"] == candidate
    assert direct["observed"]["parentCommit"] == resolver.EXPECTED_PARENT
    assert [row["path"] for row in direct["observed"]["changedPaths"]] == list(resolver.EXPECTED_PATHS)
    assert direct["observed"]["parentGitlinks"] == direct["observed"]["candidateGitlinks"]

    subprocess.run(["git", "-C", str(root), "branch", "-f", "main", candidate], check=True)
    subprocess.run(["git", "-C", str(root), "switch", "-q", "main"], check=True)
    committed = resolver.resolve_authority(root, "main")
    validate_result(committed)
    assert committed["result"] == "PASS"
    assert committed["observed"]["candidateCommit"] == candidate


def test_cli_emits_the_pass_envelope_and_exit_zero(valid_candidate: tuple[Path, str]) -> None:
    """Exercise the exact operator-facing command shape."""

    root, candidate = valid_candidate
    completed = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--repository",
            str(root),
            "--candidate",
            candidate,
            "--check",
        ],
        check=False,
        capture_output=True,
        text=True,
    )
    document = json.loads(completed.stdout)
    validate_result(document)
    assert completed.returncode == 0
    assert document["result"] == "PASS"


def mutate_marker(root: Path) -> None:
    """Drift one V22 marker binding."""

    path = root / resolver.ARCHITECTURE_PATH
    content = path.read_text(encoding="utf-8")
    path.write_text(content.replace("hold=ACTIVE -->", "hold=INACTIVE -->", 1), encoding="utf-8")


def mutate_route_digest(root: Path) -> None:
    """Change only the route artifact's raw digest while preserving its JSON value."""

    path = root / resolver.ROUTE_PATH
    path.write_bytes(path.read_bytes() + b" ")


def mutate_workflow_route(root: Path) -> None:
    """Retire the one active V22 workflow command."""

    path = root / resolver.WORKFLOW_PATH
    content = path.read_text(encoding="utf-8")
    path.write_text(content.replace("--candidate HEAD --check", "--candidate HEAD^ --check", 1), encoding="utf-8")


@pytest.mark.parametrize(
    ("mutation", "extra_path", "executable_path", "expected_code"),
    [
        (None, True, None, "TRANSACTION_PATH_DRIFT"),
        (None, False, resolver.WORKFLOW_PATH, "TRANSACTION_MODE_DRIFT"),
        (mutate_marker, False, None, "V22_MARKER_DRIFT"),
        (mutate_route_digest, False, None, "ROUTE_INVENTORY_DIGEST_DRIFT"),
        (mutate_workflow_route, False, None, "WORKFLOW_ROUTE_DRIFT"),
    ],
)
def test_semantic_drift_is_schema_valid_fail_with_a_stable_code(
    tmp_path: Path,
    mutation: Mutation | None,
    extra_path: bool,
    executable_path: str | None,
    expected_code: str,
) -> None:
    """Cover path, mode, marker, digest, and workflow-route semantic drift."""

    root, candidate = candidate_repository(
        tmp_path,
        mutate=mutation,
        extra_path=extra_path,
        executable_path=executable_path,
    )
    document = resolver.resolve_authority(root, candidate)
    validate_result(document)
    assert document["result"] == "FAIL"
    assert document["exitCode"] == 1
    assert document["blockers"][0]["code"] == expected_code
    assert any(row["state"] == "FAIL" for row in document["assertionLedger"])


def test_wrong_parent_is_schema_valid_fail(valid_candidate: tuple[Path, str]) -> None:
    """A descendant cannot substitute for the one selected direct-child candidate."""

    root, _candidate = valid_candidate
    path = root / "wrong-parent.txt"
    path.write_text("wrong parent\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "wrong-parent.txt"], check=True)
    descendant = commit(root, "test: add wrong V22 parent")
    document = resolver.resolve_authority(root, descendant)
    validate_result(document)
    assert document["result"] == "FAIL"
    assert document["exitCode"] == 1
    assert document["blockers"][0]["code"] == "CANDIDATE_PARENT_DRIFT"


def test_gitlink_tuple_drift_is_schema_valid_fail(
    valid_candidate: tuple[Path, str],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Exercise the raw mode-160000 tuple guard independently of path drift."""

    root, candidate = valid_candidate
    calls = 0

    def drift_second_call(_repository: Path, _commit: str) -> tuple[tuple[str, str, str], ...]:
        nonlocal calls
        calls += 1
        rows = list(resolver.EXPECTED_GITLINKS)
        if calls == 2:
            path, mode, _object_id = rows[0]
            rows[0] = (path, mode, "0" * 40)
        return tuple(rows)

    monkeypatch.setattr(resolver, "gitlinks", drift_second_call)
    document = resolver.resolve_authority(root, candidate)
    validate_result(document)
    assert document["result"] == "FAIL"
    assert document["blockers"][0]["code"] == "ROOT_GITLINK_DRIFT"


def mutate_recovery_to_malformed_json(root: Path) -> None:
    """Make committed recovery evidence unavailable to the duplicate-safe loader."""

    (root / resolver.RECOVERY_PATH).write_text("{\n", encoding="utf-8")


def test_malformed_evidence_is_schema_valid_blocked(tmp_path: Path) -> None:
    """Cover malformed committed evidence without coercing it to FAIL or PASS."""

    root, candidate = candidate_repository(tmp_path, mutate=mutate_recovery_to_malformed_json)
    document = resolver.resolve_authority(root, candidate)
    validate_result(document)
    assert document["result"] == "BLOCKED"
    assert document["exitCode"] == 2
    assert document["blockers"][0]["code"] == "RECOVERY_POLICY_MALFORMED"
    assert any(row["state"] == "BLOCKED" for row in document["assertionLedger"])


def test_missing_candidate_is_schema_valid_blocked(valid_candidate: tuple[Path, str]) -> None:
    """Cover unavailable history/object evidence with a nonvacuous BLOCKED envelope."""

    root, _candidate = valid_candidate
    document = resolver.resolve_authority(root, "0" * 40)
    validate_result(document)
    assert document["result"] == "BLOCKED"
    assert document["exitCode"] == 2
    assert document["blockers"][0]["code"] == "GIT_OBJECT_UNAVAILABLE"


def test_policy_route_and_result_schemas_are_closed(
    valid_candidate: tuple[Path, str],
) -> None:
    """Reject candidate-defined extension fields on both artifacts and result evidence."""

    root, candidate = valid_candidate
    recovery_schema = schema()
    policy = json.loads((ROOT / resolver.RECOVERY_PATH).read_text(encoding="utf-8"))
    policy["unexpected"] = True
    with pytest.raises(ValidationError):
        Draft202012Validator(recovery_schema).validate(policy)

    route_schema = json.loads((ROOT / resolver.ROUTE_SCHEMA_PATH).read_text(encoding="utf-8"))
    route = json.loads((ROOT / resolver.ROUTE_PATH).read_text(encoding="utf-8"))
    route["unexpected"] = True
    with pytest.raises(ValidationError):
        Draft202012Validator(route_schema).validate(route)

    result = resolver.resolve_authority(root, candidate)
    result["unexpected"] = True
    with pytest.raises(ValidationError):
        Draft202012Validator(recovery_schema).validate(result)


def test_workflow_retires_current_v21_and_external_trust_routes() -> None:
    """Keep only the V22 route active while naming the frozen V21 test revision."""

    workflow = (ROOT / resolver.WORKFLOW_PATH).read_text(encoding="utf-8")
    assert workflow.count(resolver.ACTIVE_COMMAND) == 1
    assert resolver.EXPECTED_V21_TOOLING in workflow
    assert resolver.V21_TEST_PATH in workflow
    for forbidden in (
        "RULESET_TOKEN",
        "/rulesets?",
        " ci-trust ",
        f"{resolver.V21_PUBLISHER_PATH} --repository",
        f'{resolver.V21_PUBLISHER_PATH}" --repository',
    ):
        assert forbidden not in workflow


def test_historical_v21_sources_are_unchanged_and_faults_restore_source_bytes() -> None:
    """Pin historical V21 bytes and prove fixture faults never alter the source worktree."""

    for relative_path in (
        resolver.V21_RECORD_PATH,
        resolver.V21_SCHEMA_PATH,
        resolver.V21_PUBLISHER_PATH,
        resolver.V21_TEST_PATH,
    ):
        worktree = (ROOT / relative_path).read_bytes()
        parent = subprocess.check_output(
            ["git", "-C", str(ROOT), "cat-file", "blob", f"{resolver.EXPECTED_PARENT}:{relative_path}"]
        )
        assert worktree == parent
    assert hashlib.sha256((ROOT / resolver.V21_RECORD_PATH).read_bytes()).hexdigest() == resolver.EXPECTED_V21_SHA256


def test_caller_synthesized_blocked_result_is_nonvacuous_and_closed() -> None:
    """A caller that receives no/malformed output can fail closed without an empty ledger."""

    document = resolver.blocked_result("OUTPUT_UNAVAILABLE", "resolver emitted no parseable output")
    validate_result(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"] == [
        {"code": "OUTPUT_UNAVAILABLE", "detail": "resolver emitted no parseable output"}
    ]
