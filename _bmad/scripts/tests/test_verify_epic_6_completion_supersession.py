"""Fault-injection tests for immutable Epic 6 completion reconstruction."""

from __future__ import annotations

from copy import deepcopy
import importlib.util
from pathlib import Path

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


def test_skipped_rebuilt_test_never_becomes_pass() -> None:
    def skipped(_root: Path, story: dict, _rows: list[dict]) -> dict:
        result = passing_checks(_root, story, _rows)
        result["tests"][0]["skippedCount"] = 1
        return result

    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=True, check_executor=skipped)

    assert error.value.code == "E6_REBUILT_TEST_SKIPPED"


def test_omitted_test_execution_is_blocked_with_nonempty_ledger() -> None:
    with pytest.raises(verifier.SupersessionError) as error:
        verifier.reconstruct(ROOT, execute_tests=False, check_executor=passing_checks)

    assert error.value.code == "E6_REBUILT_TESTS_SKIPPED"
    document = verifier.failure_document(error.value)
    assert document["result"] == "BLOCKED"
    assert document["assertionLedger"]


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
