"""Fault-injection tests for immutable Epic 6 completion reconstruction."""

from __future__ import annotations

from copy import deepcopy
import importlib.util
from pathlib import Path
import subprocess

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/verify_epic_6_completion_supersession.py"
SPEC = importlib.util.spec_from_file_location("verify_epic_6_completion_supersession", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)


def passing_checks(_root: Path, story: dict, _rows: list[dict]) -> dict:
    return {
        "promotion": {
            "id": f"STORY-{story['storyId']}-PROMOTION",
            "state": "PASS",
            "result": "pass",
            "blockerCodes": [],
        },
        "tests": [
            {
                "id": command["id"],
                "state": "PASS",
                "exitCode": 0,
                "skippedCount": 0,
                "notRunCount": 0,
                "outputSha256": "0" * 64,
                "outputTail": "passed",
            }
            for command in story["testCommands"]
        ],
    }


def test_valid_checkpoint_reconstructs_exact_paths_and_ten_gitlinks() -> None:
    document = verifier.reconstruct(ROOT, execute_tests=True, check_executor=passing_checks)

    assert document["result"] == "PASS"
    assert [story["storyId"] for story in document["stories"]] == ["6.7", "6.2"]
    assert [len(story["rawDiff"]) for story in document["stories"]] == [8, 9]
    assert all(len(story["candidateRootGitlinks"]) == 10 for story in document["stories"])
    assert all(len(story["doneRootGitlinks"]) == 10 for story in document["stories"])
    assert document["assertionLedger"]
    assert document["implementationHold"] == "ACTIVE"
    assert document["releaseAuthorized"] is False


@pytest.mark.parametrize(
    ("field", "replacement", "code"),
    (
        ("changedPaths", ["README.md"], "E6_CHANGED_PATH_SET_DRIFT"),
        ("candidateCommit", "f" * 40, "E6_HISTORY_UNAVAILABLE"),
    ),
)
def test_path_or_history_mutation_fails_without_modifying_contract(
    monkeypatch: pytest.MonkeyPatch,
    field: str,
    replacement,
    code: str,
) -> None:
    original_bytes = (ROOT / verifier.CONTRACT_PATH).read_bytes()
    original_loader = verifier.load_contract

    def mutated_loader(repository: Path):
        contract, contract_bytes, schema_bytes = original_loader(repository)
        contract = deepcopy(contract)
        contract["stories"][0][field] = replacement
        return contract, contract_bytes, schema_bytes

    monkeypatch.setattr(verifier, "load_contract", mutated_loader)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=True, check_executor=passing_checks)

    assert error.value.code == code
    assert (ROOT / verifier.CONTRACT_PATH).read_bytes() == original_bytes


def test_unavailable_submodule_object_is_blocked(monkeypatch: pytest.MonkeyPatch) -> None:
    def unavailable(_repository: Path, _rows: list[dict], story_id: str, _revision: str) -> None:
        raise verifier.SupersessionError("E6_GITLINK_OBJECT_UNAVAILABLE", "fixture", "BLOCKED", story_id)

    monkeypatch.setattr(verifier, "verify_gitlink_objects", unavailable)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=True, check_executor=passing_checks)

    assert error.value.code == "E6_GITLINK_OBJECT_UNAVAILABLE"
    assert error.value.state == "BLOCKED"


def test_unavailable_inotify_capacity_blocks_exact_dapr_lane(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    contract, _contract_bytes, _schema_bytes = verifier.load_contract(ROOT)
    story = contract["stories"][1]

    def unavailable(_root: Path, command: dict, story_id: str) -> None:
        raise verifier.SupersessionError(
            "E6_REBUILT_TEST_ENVIRONMENT_UNAVAILABLE",
            f"{command['id']}: errno 28 (No space left on device)",
            "BLOCKED",
            story_id,
        )

    monkeypatch.setattr(verifier, "require_linux_inotify_watch", unavailable)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.execute_story_checks(ROOT, story, [])

    assert error.value.code == "E6_REBUILT_TEST_ENVIRONMENT_UNAVAILABLE"
    assert error.value.state == "BLOCKED"
    assert error.value.story_id == "6.2"
    assert "STORY-6-2-FULL-SOLUTION-TESTS" in error.value.message


def test_skipped_rebuilt_test_never_becomes_pass() -> None:
    def skipped(_root: Path, story: dict, _rows: list[dict]) -> dict:
        result = passing_checks(_root, story, _rows)
        result["tests"][0]["skippedCount"] = 1
        return result

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=True, check_executor=skipped)

    assert error.value.code == "E6_REBUILT_TEST_SKIPPED"


def test_not_run_count_requires_a_positive_count() -> None:
    assert verifier.not_run_count("Not Run: 0") == 0
    assert verifier.not_run_count("not-run=2") == 2


def test_failed_rebuilt_test_preserves_actionable_diagnostics() -> None:
    def failed(_root: Path, story: dict, _rows: list[dict]) -> dict:
        result = passing_checks(_root, story, _rows)
        result["tests"][0].update(
            {
                "state": "FAIL",
                "exitCode": 1,
                "outputSha256": "f" * 64,
                "outputTail": "exact failure detail",
            }
        )
        return result

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=True, check_executor=failed)

    assert error.value.code == "E6_REBUILT_CHECK_FAILED"
    assert "exact failure detail" in error.value.message


def test_omitted_test_execution_is_blocked_with_nonempty_ledger() -> None:
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=False, check_executor=passing_checks)

    assert error.value.code == "E6_REBUILT_TESTS_SKIPPED"
    document = verifier.failure_document(error.value)
    assert document["result"] == "BLOCKED"
    assert document["assertionLedger"]


def test_blocked_markdown_binds_authority_and_forbids_acceptance_decision() -> None:
    error = verifier.SupersessionError(
        "E6_REBUILT_TEST_ENVIRONMENT_UNAVAILABLE",
        "inotify capacity exhausted",
        "BLOCKED",
        "6.2",
    )
    authority = {
        "candidateCommit": "a" * 40,
        "path": "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json",
        "sha256": "b" * 64,
    }

    document = verifier.failure_document(error, authority)
    rendered = verifier.markdown(document)

    assert document["authorityBundle"] == authority
    assert "Planning candidate" in rendered
    assert "cannot support acceptance-evidence supersession" in rendered


def test_record_and_promotion_declaration_mutations_fail(monkeypatch: pytest.MonkeyPatch) -> None:
    original = verifier.recorded_story

    def displaced(repository: Path, story: dict) -> dict:
        row = original(repository, story)
        if story["storyId"] == "6.7":
            raise verifier.SupersessionError("E6_PROMOTION_DECLARATION_DRIFT", "fixture", story_id="6.7")
        return row

    monkeypatch.setattr(verifier, "recorded_story", displaced)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=True, check_executor=passing_checks)
    assert error.value.code == "E6_PROMOTION_DECLARATION_DRIFT"


def test_malformed_contract_schema_is_rejected() -> None:
    contract, _contract_bytes, schema_bytes = verifier.load_contract(ROOT)
    schema = verifier.json.loads(schema_bytes)
    malformed = deepcopy(contract)
    malformed["unexpected"] = True

    errors = list(verifier.Draft202012Validator(schema).iter_errors(malformed))
    assert errors


def test_exact_tree_clones_disable_cross_device_hardlinks(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    calls: list[tuple[str, ...]] = []

    def completed(arguments, **_kwargs):
        calls.append(tuple(arguments))
        return verifier.subprocess.CompletedProcess(arguments, 0, stdout=b"", stderr=b"")

    monkeypatch.setattr(verifier.subprocess, "run", completed)
    verifier.clone_exact_tree(
        tmp_path / "source",
        tmp_path / "destination",
        "a" * 40,
        [{"path": "references/Example", "objectId": "b" * 40}],
    )

    clone_calls = [arguments for arguments in calls if arguments[:2] == ("git", "clone")]
    assert len(clone_calls) == 2
    assert all("--no-hardlinks" in arguments for arguments in clone_calls)


def passing_current_proof_test(_root: Path, command: dict) -> dict:
    return {
        "id": command["id"],
        "argv": command["argv"],
        "state": "PASS",
        "exitCode": 0,
        "skippedCount": 0,
        "notRunCount": 0,
        "outputSha256": "0" * 64,
        "outputTail": "passed",
        "_output": "passed",
    }


def test_current_proof_binds_head_gitlinks_and_nonempty_ledger() -> None:
    document = verifier.current_proof(ROOT, execute_tests=True, test_executor=passing_current_proof_test, allow_dirty_worktree=True)

    assert document["result"] == "PASS"
    assert document["evidenceId"] == verifier.CURRENT_PROOF_EVIDENCE_ID
    assert document["schemaVersion"] == verifier.CURRENT_PROOF_EVIDENCE_SCHEMA_VERSION
    assert set(document["storyDoneCommits"]) == {"6.7", "6.2"}
    assert len(document["rootGitlinks"]) == 10
    assert all(row["mode"] == "160000" for row in document["rootGitlinks"])
    assert document["assertionLedger"]
    assert document["implementationHold"] == "ACTIVE"
    assert document["releaseAuthorized"] is False
    assert document["supersedesHistoricalEvidence"] is False
    for story_id in ("6.7", "6.2"):
        observed = document["postDoneChangedPaths"][story_id]
        assert observed
        expected = verifier.post_done_changed_paths(
            ROOT,
            document["storyDoneCommits"][story_id],
            document["currentHeadCommit"],
            "references/",
        )
        assert observed == expected["paths"]


def test_current_proof_unreachable_done_commit_is_blocked(monkeypatch: pytest.MonkeyPatch) -> None:
    original_bytes = (ROOT / verifier.CURRENT_PROOF_CONTRACT_PATH).read_bytes()
    original_loader = verifier.load_current_proof_contract

    def mutated_loader(repository: Path):
        contract, contract_bytes, schema_bytes = original_loader(repository)
        contract = deepcopy(contract)
        contract["storyDoneCommits"] = {
            **contract["storyDoneCommits"],
            "6.7": "f" * 40,
        }
        return contract, contract_bytes, schema_bytes

    monkeypatch.setattr(verifier, "load_current_proof_contract", mutated_loader)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=passing_current_proof_test, allow_dirty_worktree=True)

    assert error.value.state == "BLOCKED"
    assert error.value.code in {"E6_HISTORY_UNAVAILABLE", "E6_COMMIT_ID_MISMATCH"}
    document = verifier.current_proof_failure_document(error.value)
    assert document["result"] == "BLOCKED"
    assert document["assertionLedger"]
    assert (ROOT / verifier.CURRENT_PROOF_CONTRACT_PATH).read_bytes() == original_bytes


def test_current_proof_non_gitlink_root_path_is_blocked_via_recorded_gitlink(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    promotion = verifier.promotion_module()

    def not_gitlink(_repository: Path, _head: str, _path: str) -> tuple[str | None, str | None]:
        return "100644", "a" * 40

    monkeypatch.setattr(promotion, "recorded_gitlink", not_gitlink)
    monkeypatch.setattr(verifier, "promotion_module", lambda: promotion)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=passing_current_proof_test, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_GITLINK_MODE_DRIFT"
    assert error.value.state == "BLOCKED"
    document = verifier.current_proof_failure_document(error.value)
    assert document["assertionLedger"]
    assert document["result"] == "BLOCKED"


def test_current_proof_head_not_descendant_is_blocked(monkeypatch: pytest.MonkeyPatch) -> None:
    original_git = verifier.git

    def deny_ancestor(repository: Path, *arguments: str, allowed=(0,), timeout=60):
        if arguments[:2] == ("merge-base", "--is-ancestor"):
            return verifier.subprocess.CompletedProcess(arguments, 1, stdout=b"", stderr=b"")
        return original_git(repository, *arguments, allowed=allowed, timeout=timeout)

    monkeypatch.setattr(verifier, "git", deny_ancestor)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=passing_current_proof_test, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_HEAD_NOT_DESCENDANT"
    assert error.value.state == "BLOCKED"


def test_current_proof_skipped_surface_is_blocked() -> None:
    def skipped(_root: Path, command: dict) -> dict:
        result = passing_current_proof_test(_root, command)
        result["skippedCount"] = 1
        return result

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=skipped, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_TEST_SKIPPED"
    assert error.value.state == "BLOCKED"


def test_current_proof_not_run_surface_is_blocked() -> None:
    def not_run(_root: Path, command: dict) -> dict:
        result = passing_current_proof_test(_root, command)
        result["notRunCount"] = 2
        return result

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=not_run, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_TEST_NOT_RUN"
    assert error.value.state == "BLOCKED"


def test_current_proof_malformed_test_result_is_blocked() -> None:
    def malformed(_root: Path, command: dict) -> dict:
        return {"id": command["id"], "state": "PASS"}

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=malformed, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_TEST_MALFORMED"
    assert error.value.state == "BLOCKED"


def test_current_proof_empty_ledger_is_blocked() -> None:
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.require_nonempty_ledger([], "E6_CURRENT_PROOF_LEDGER_EMPTY")

    assert error.value.code == "E6_CURRENT_PROOF_LEDGER_EMPTY"
    assert error.value.state == "BLOCKED"
    document = verifier.current_proof_failure_document(error.value)
    assert document["assertionLedger"]
    assert document["result"] == "BLOCKED"


def test_current_proof_omitted_test_execution_is_blocked() -> None:
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=False, test_executor=passing_current_proof_test, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_TESTS_SKIPPED"
    assert error.value.state == "BLOCKED"


def test_current_proof_failed_surface_preserves_diagnostics() -> None:
    def failed(_root: Path, command: dict) -> dict:
        result = passing_current_proof_test(_root, command)
        result.update(
            {
                "state": "FAIL",
                "exitCode": 1,
                "outputSha256": "f" * 64,
                "outputTail": "current-proof surface failure detail",
            }
        )
        return result

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(ROOT, execute_tests=True, test_executor=failed, allow_dirty_worktree=True)

    assert error.value.code == "E6_CURRENT_PROOF_TEST_FAILED"
    assert "current-proof surface failure detail" in error.value.message


def test_current_proof_cli_bypasses_historical_reconstruct(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    reconstruct_calls: list[object] = []

    def forbidden(*_args, **_kwargs):
        reconstruct_calls.append(True)
        raise AssertionError("historical reconstruct() must not run on --current-proof")

    monkeypatch.setattr(verifier, "reconstruct", forbidden)
    monkeypatch.setattr(
        verifier,
        "current_proof",
        lambda *_args, **_kwargs: {
            "schemaVersion": verifier.CURRENT_PROOF_EVIDENCE_SCHEMA_VERSION,
            "evidenceId": verifier.CURRENT_PROOF_EVIDENCE_ID,
            "issuedOn": "2026-08-09",
            "result": "PASS",
            "implementationHold": "ACTIVE",
            "releaseAuthorized": False,
            "supersedesHistoricalEvidence": False,
            "assertionLedger": [{"id": "CURRENT-PROOF-CLI", "state": "PASS"}],
            "currentHeadCommit": "a" * 40,
            "storyDoneCommits": {},
            "rootGitlinks": [],
            "postDoneChangedPaths": {},
            "testResults": [],
            "decisionRequired": True,
        },
    )
    output_json = tmp_path / "current-proof.json"
    exit_code = verifier.main(
        [
            "--repository",
            str(ROOT),
            "--current-proof",
            "--execute-tests",
            "--output-json",
            str(output_json),
        ]
    )

    assert exit_code == 0
    assert reconstruct_calls == []
    document = verifier.json.loads(output_json.read_text(encoding="utf-8"))
    assert document["evidenceId"] == verifier.CURRENT_PROOF_EVIDENCE_ID
    assert document["schemaVersion"] == verifier.CURRENT_PROOF_EVIDENCE_SCHEMA_VERSION


def test_current_proof_markdown_forbids_hold_lift_and_historical_rewrite() -> None:
    document = verifier.current_proof(ROOT, execute_tests=True, test_executor=passing_current_proof_test, allow_dirty_worktree=True)
    rendered = verifier.current_proof_markdown(document)

    assert "does not by itself lift the implementation hold" in rendered
    assert "never rewrites, narrows, or" in rendered or "never rewrites" in rendered
    assert "epic-6-retro-item-24" in rendered
    assert document["supersedesHistoricalEvidence"] is False


# --- E6-REMEDIATION A3 §4.6 evidence-integrity faults ----------------------


def test_dirty_tracked_worktree_blocks_current_proof(tmp_path: Path) -> None:
    """F-10: uncommitted tracked bytes must never be attributed to the resolved commit."""
    for relative_path in (
        verifier.CURRENT_PROOF_CONTRACT_PATH,
        verifier.CURRENT_PROOF_SCHEMA_PATH,
    ):
        target = tmp_path / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative_path).read_bytes())
    dirty_path = tmp_path / "controlled-dirty-input.txt"
    dirty_path.write_text("committed\n", encoding="utf-8")
    subprocess.run(("git", "init", "-q", "-b", "main", str(tmp_path)), check=True)
    subprocess.run(("git", "-C", str(tmp_path), "config", "user.name", "Verifier"), check=True)
    subprocess.run(
        ("git", "-C", str(tmp_path), "config", "user.email", "verifier@example.invalid"),
        check=True,
    )
    subprocess.run(("git", "-C", str(tmp_path), "add", "."), check=True)
    subprocess.run(("git", "-C", str(tmp_path), "commit", "-q", "-m", "test: baseline"), check=True)
    dirty_path.write_text("uncommitted\n", encoding="utf-8")

    assert verifier.worktree_dirt(tmp_path) == [dirty_path.name]
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(tmp_path, execute_tests=True, test_executor=passing_current_proof_test)
    assert error.value.state == "BLOCKED"
    assert error.value.code == "E6_CURRENT_PROOF_WORKTREE_DIRTY"
    assert dirty_path.name in error.value.message


def test_contract_bytes_must_come_from_the_resolved_commit(monkeypatch: pytest.MonkeyPatch) -> None:
    """R2: a worktree-only contract edit cannot be presented as the resolved commit's evidence."""
    original_loader = verifier.load_current_proof_contract

    def mutated_loader(repository: Path):
        contract, contract_bytes, schema_bytes = original_loader(repository)
        return contract, contract_bytes + b"\n", schema_bytes

    monkeypatch.setattr(verifier, "load_current_proof_contract", mutated_loader)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_proof(
            ROOT, execute_tests=True, test_executor=passing_current_proof_test, allow_dirty_worktree=True
        )
    assert error.value.state == "BLOCKED"
    assert error.value.code == "E6_CURRENT_PROOF_INPUT_WORKTREE_DRIFT"


def test_changed_path_enumeration_uses_history_union_not_endpoint_difference() -> None:
    """F-11/F-12: the authoritative set is the history union, cross-checked by an independent oracle."""
    head = verifier.resolve_head(ROOT)
    contract, _contract_bytes, _schema_bytes = verifier.load_current_proof_contract(ROOT)
    done = verifier.resolve_commit(ROOT, contract["storyDoneCommits"]["6.2"], "6.2")

    enumeration = verifier.post_done_changed_paths(ROOT, done, head, "references/")
    endpoint = verifier.endpoint_changed_paths(ROOT, done, head, "references/")
    union = verifier.history_union_changed_paths(ROOT, done, head, "references/")

    assert enumeration["paths"] == union
    assert enumeration["endpointPaths"] == endpoint
    # The union can never omit a path the endpoint diff reports; that asymmetry is the whole point.
    assert set(endpoint) <= set(union)
    assert enumeration["revertedWithinRange"] == sorted(set(union) - set(endpoint))
    assert all(path.startswith("references/") for path in enumeration["paths"])


def test_root_gitlink_inventory_requires_exact_equality(monkeypatch: pytest.MonkeyPatch) -> None:
    """F-13: a root gitlink present at HEAD but absent from the contract must block."""
    head = verifier.resolve_head(ROOT)
    contract, _contract_bytes, _schema_bytes = verifier.load_current_proof_contract(ROOT)
    declared = list(contract["rootGitlinks"])

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_root_gitlinks(ROOT, head, declared[:-1])
    assert error.value.state == "BLOCKED"
    assert error.value.code == "E6_CURRENT_PROOF_GITLINK_INVENTORY_DRIFT"


def test_unavailable_gitlink_object_blocks_current_proof(monkeypatch: pytest.MonkeyPatch) -> None:
    """F-14: a recorded object that is not reachable in its submodule is not evidence."""
    head = verifier.resolve_head(ROOT)
    contract, _contract_bytes, _schema_bytes = verifier.load_current_proof_contract(ROOT)
    # promotion_module() re-executes a fresh module on every call, so patching the object it
    # returns has no effect; the factory itself must be replaced.
    real_module = verifier.promotion_module()
    original = real_module.recorded_gitlink

    class StubPromotionModule:
        GateError = real_module.GateError

        @staticmethod
        def recorded_gitlink(repository: Path, commit: str, path: str):
            mode, object_id = original(repository, commit, path)
            if path.endswith("Hexalith.Tenants"):
                return mode, "0" * 39 + "1"
            return mode, object_id

    monkeypatch.setattr(verifier, "promotion_module", lambda: StubPromotionModule)
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.current_root_gitlinks(ROOT, head, list(contract["rootGitlinks"]))
    assert error.value.state == "BLOCKED"
    assert error.value.code == "E6_CURRENT_PROOF_GITLINK_OBJECT_UNAVAILABLE"


def test_gitmodules_inventory_must_match_the_declared_roots() -> None:
    """The declared roots, HEAD's mode-160000 tree entries, and .gitmodules must agree exactly."""
    head = verifier.resolve_head(ROOT)
    contract, _contract_bytes, _schema_bytes = verifier.load_current_proof_contract(ROOT)
    declared = sorted(contract["rootGitlinks"])
    assert verifier.declared_root_gitlink_inventory(ROOT, head) == declared
    assert verifier.gitmodules_paths(ROOT, head) == declared


@pytest.mark.parametrize(
    ("output", "expected"),
    [
        ("1 skipped", 1),
        ("3 passed, 1 skipped in 0.12s", 1),
        ("Skipped: 2", 2),
        ("skipped=4", 4),
        ("12 passed in 0.30s", 0),
        ("225 passed, 7 skipped, 1 failed", 7),
    ],
)
def test_skip_parser_reads_count_first_and_label_first_forms(output: str, expected: int) -> None:
    """F-15: pytest's normal count-first summary was previously invisible to the parser."""
    assert verifier.skipped_count(output) == expected


@pytest.mark.parametrize(
    ("output", "expected"),
    [
        ("2 not run", 2),
        ("Not run: 3", 3),
        ("not-run = 5", 5),
        ("all tests executed", 0),
    ],
)
def test_not_run_parser_reads_both_orders(output: str, expected: int) -> None:
    assert verifier.not_run_count(output) == expected
