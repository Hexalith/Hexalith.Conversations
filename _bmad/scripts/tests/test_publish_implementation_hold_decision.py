"""Transaction and fault-injection tests for the V17 implementation-hold decision authority."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
from typing import Callable

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_implementation_hold_decision.py"
SPEC = importlib.util.spec_from_file_location("publish_implementation_hold_decision", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)


def git(path: Path, *arguments: str) -> str:
    return subprocess.check_output(["git", "-C", str(path), *arguments], text=True).strip()


def commit(path: Path, paths: tuple[str, ...], message: str) -> str:
    subprocess.run(["git", "-C", str(path), "add", "--", *paths], check=True)
    subprocess.run(["git", "-C", str(path), "commit", "-q", "--no-verify", "-m", message], check=True)
    return git(path, "rev-parse", "HEAD")


def stage_candidate(
    tmp_path: Path,
    mutate: Callable[[Path], None] | None = None,
    extra_paths: tuple[str, ...] = (),
) -> tuple[Path, str]:
    staged = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(staged)], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", publisher.BASELINE_COMMIT], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.name", "V17 fixture"], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.email", "v17@example.invalid"], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "core.hooksPath", str(staged / ".git/hooks")], check=True)
    for relative in publisher.C1_PATHS:
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)
    if mutate is not None:
        mutate(staged)
    candidate = commit(staged, (*publisher.C1_PATHS, *extra_paths), "feat(planning): stage V17 fixture")
    return staged, candidate


def stage_transaction(tmp_path: Path) -> tuple[Path, str, str]:
    staged, candidate = stage_candidate(tmp_path)
    publisher.publish(staged, candidate_revision=candidate, check=False)
    publication = commit(staged, publisher.C2_PATHS, "build(planning): bind V17 fixture")
    return staged, candidate, publication


def test_v17_binds_the_exact_transaction_record_predecessors_and_ledger(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)

    document = publisher.publish(
        staged, candidate_revision=publication, publication_revision=publication, check=True
    )

    assert document["candidateCommit"] == candidate
    assert document["publication"]["c1Paths"] == list(publisher.C1_PATHS)
    assert document["publication"]["c2Paths"] == list(publisher.C2_PATHS)
    assert len(document["publication"]["combinedPaths"]) == 8
    assert document["publication"]["changedGitlinks"] == []
    assert len(document["immutableAuthorities"]) == 7
    assert document["immutableAuthorities"][-1]["path"] == publisher.IR0_PATH
    assert document["authorityEffect"] == {
        "implementationHold": "LIFTED",
        "ir0AuthorizationChanged": False,
        "successorActivated": True,
        "unlocks": ["7.1-SCHEMAS"],
        "storyDoneAllowed": False,
        "readinessRerunSatisfied": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }
    assert any("readiness rerun" in claim for claim in document["nonClaims"])
    assert document["assertionLedger"]

    record = json.loads((staged / publisher.RECORD_PATH).read_text(encoding="utf-8"))
    assert record["effectiveState"] == "LIFTED"
    assert record["scope"]["unlocks"] == ["7.1-SCHEMAS"]
    assert record["scope"]["global"] is False
    assert record["expiry"] == {"kind": "candidate-bound", "calendarExpiry": None}
    assert document["holdRecord"]["sha256"] == publisher.sha256(publisher.json_bytes(record))


def test_record_is_absent_from_the_v9_bundle_inventory_and_protected_path_sets(tmp_path: Path) -> None:
    v9_spec = importlib.util.spec_from_file_location(
        "publish_v9_planning_authority", ROOT / "_bmad/scripts/publish_v9_planning_authority.py"
    )
    assert v9_spec is not None and v9_spec.loader is not None
    v9 = importlib.util.module_from_spec(v9_spec)
    v9_spec.loader.exec_module(v9)

    for name in ("CANONICAL_PATHS", "PROTECTED_CANDIDATE_PATHS", "V12_CANONICAL_PATHS"):
        assert publisher.RECORD_PATH not in getattr(v9, name)
        assert publisher.AUTHORITY_PATH not in getattr(v9, name)

    bundle = json.loads((ROOT / publisher.V9_AUTHORITY_PATH).read_text(encoding="utf-8"))
    assert all(row["path"] != publisher.RECORD_PATH for row in bundle["artifacts"])


def test_predecessor_byte_or_mode_drift_fails_closed_and_writes_nothing(tmp_path: Path) -> None:
    staged, candidate = stage_candidate(tmp_path)
    target = staged / publisher.V16_AUTHORITY_PATH
    original = target.read_bytes()
    target.write_text(target.read_text(encoding="utf-8").replace('"result": "PASS"', '"result": "FAIL"'), encoding="utf-8")
    drifted = commit(staged, (publisher.V16_AUTHORITY_PATH,), "test: drift V17 predecessor")

    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_predecessors(staged, drifted)

    assert error.value.code == "HOLD_PREDECESSOR_DRIFT"
    assert error.value.state == "FAIL"
    assert not (staged / publisher.RECORD_PATH).exists()
    assert not (staged / publisher.AUTHORITY_PATH).exists()
    assert (ROOT / publisher.V16_AUTHORITY_PATH).read_bytes() == original


def test_stale_candidate_bundle_or_ir0_binding_fails_closed(tmp_path: Path) -> None:
    staged, candidate = stage_candidate(tmp_path)
    bundle_path = staged / publisher.V9_AUTHORITY_PATH
    bundle = json.loads(bundle_path.read_text(encoding="utf-8"))
    bundle["planningCandidate"] = "0" * 40
    bundle_path.write_text(json.dumps(bundle, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    drifted = commit(staged, (publisher.V9_AUTHORITY_PATH,), "test: drift V17 binding")

    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_binding(staged, drifted)

    assert error.value.code == "HOLD_BINDING_STALE"
    assert not (staged / publisher.RECORD_PATH).exists()


def test_slice_requirement_drift_is_reported_separately(tmp_path: Path) -> None:
    staged, candidate = stage_candidate(tmp_path)
    slice_path = staged / publisher.SLICE_PATH
    document = json.loads(slice_path.read_text(encoding="utf-8"))
    document["holdRequirement"]["effectiveState"] = "ACTIVE"
    slice_path.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    drifted = commit(staged, (publisher.SLICE_PATH,), "test: drift V17 slice requirement")

    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_binding(staged, drifted)

    assert error.value.code == "HOLD_SLICE_DRIFT"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document.__setitem__("unknown", True),
        lambda document: document["assertionLedger"].clear(),
        lambda document: document["immutableAuthorities"][0].__setitem__("sha256", "0" * 64),
        lambda document: document["authorityEffect"].__setitem__("implementationHold", "ACTIVE"),
        lambda document: document["holdRecord"].__setitem__("sha256", "NOTAHASH"),
        lambda document: document["publication"]["c2Paths"].pop(),
    ),
)
def test_closed_authority_schema_rejects_unknown_weak_and_effect_faults(tmp_path: Path, mutation) -> None:
    staged, candidate = stage_candidate(tmp_path)
    document = publisher.render_authority(staged, candidate, publisher.render_record())
    mutation(document)

    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_schema(staged, candidate, publisher.AUTHORITY_SCHEMA_PATH, document)

    assert error.value.code == "HOLD_SCHEMA_INVALID"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document.__setitem__("unknown", True),
        lambda document: document.__setitem__("effectiveState", "ACTIVE"),
        lambda document: document["scope"].__setitem__("global", True),
        lambda document: document["expiry"].__setitem__("calendarExpiry", "2027-01-01"),
        lambda document: document["scope"].__setitem__("ir0Sha256", "0" * 63),
        lambda document: document["nonClaims"].clear(),
        lambda document: document["assertionLedger"].clear(),
    ),
)
def test_closed_record_schema_rejects_lift_weakening_faults(tmp_path: Path, mutation) -> None:
    staged, candidate = stage_candidate(tmp_path)
    record = publisher.render_record()
    mutation(record)

    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_schema(staged, candidate, publisher.RECORD_SCHEMA_PATH, record)

    assert error.value.code == "HOLD_SCHEMA_INVALID"


def test_c1_c2_scope_and_single_parent_faults_fail_closed(tmp_path: Path) -> None:
    def add_unexpected(root: Path) -> None:
        path = root / "_bmad/scripts/unexpected-v17.py"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("unexpected = True\n", encoding="utf-8")

    staged, candidate = stage_candidate(tmp_path / "scope", add_unexpected, ("_bmad/scripts/unexpected-v17.py",))
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.render_authority(staged, candidate, publisher.render_record())
    assert error.value.code == "HOLD_C1_SCOPE_DRIFT"

    staged, candidate = stage_candidate(tmp_path / "publication")
    publisher.publish(staged, candidate_revision=candidate, check=False)
    extra = staged / "_bmad-output/planning-artifacts/unexpected-v17.json"
    extra.write_text("{}\n", encoding="utf-8")
    publication = commit(
        staged,
        (*publisher.C2_PATHS, "_bmad-output/planning-artifacts/unexpected-v17.json"),
        "test: drift V17 publication",
    )
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_publication(staged, candidate, publication, publication)
    assert error.value.code == "HOLD_C2_SCOPE_DRIFT"

    staged, candidate = stage_candidate(tmp_path / "merge")
    subprocess.run(["git", "-C", str(staged), "branch", "side", publisher.BASELINE_COMMIT], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", "side"], check=True)
    (staged / "side.txt").write_text("side\n", encoding="utf-8")
    side = commit(staged, ("side.txt",), "test: add V17 side parent")
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", candidate], check=True)
    subprocess.run(
        ["git", "-C", str(staged), "merge", "--no-ff", "-q", "--no-verify", "-m", "test: merge V17 parent", side],
        check=True,
    )
    merge = git(staged, "rev-parse", "HEAD")
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.render_authority(staged, merge, publisher.render_record())
    assert error.value.code == "HOLD_C1_PARENT_MISMATCH"
    assert error.value.state == "BLOCKED"


def test_non_ancestor_and_descendant_drift_remain_distinct(tmp_path: Path) -> None:
    staged, _, publication = stage_transaction(tmp_path / "ancestry")
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.publish(
            staged, candidate_revision=publisher.BASELINE_COMMIT, publication_revision=publication, check=True
        )
    assert error.value.code == "HOLD_PUBLICATION_NOT_ANCESTOR"
    assert error.value.state == "BLOCKED"

    staged, _, publication = stage_transaction(tmp_path / "drift")
    record = staged / publisher.RECORD_PATH
    record.write_text(
        record.read_text(encoding="utf-8").replace('"effectiveState": "LIFTED"', '"effectiveState": "ACTIVE"'),
        encoding="utf-8",
    )
    descendant = commit(staged, (publisher.RECORD_PATH,), "test: drift V17 descendant record")
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.publish(staged, candidate_revision=descendant, publication_revision=publication, check=True)
    assert error.value.code == "HOLD_AUTHORITY_DESCENDANT_DRIFT"
    assert error.value.state == "FAIL"
    assert error.value.detail == publisher.RECORD_PATH


def test_path_escape_and_unavailable_history_stay_blocked_with_nonempty_results(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    for value in ("/etc/passwd", "../escape", "a\\b", "./a"):
        with pytest.raises(publisher.ImplementationHoldError) as error:
            publisher.safe_path(value)
        assert error.value.code == "HOLD_PATH_ESCAPE"
        assert error.value.state == "BLOCKED"

    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.parse_json(b"[]\n", "HOLD_AUTHORITY_INVALID")
    assert error.value.code == "HOLD_AUTHORITY_INVALID"

    staged, candidate = stage_candidate(tmp_path)
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.resolve_commit(staged, "refs/heads/does-not-exist", "HOLD_CANDIDATE_UNAVAILABLE")
    assert error.value.code == "HOLD_CANDIDATE_UNAVAILABLE"
    assert error.value.state == "BLOCKED"

    result = publisher.failure_document(staged, error.value)
    assert result["result"] == "BLOCKED"
    assert result["assertionLedger"]
    assert result["blockers"][0]["code"] == "HOLD_CANDIDATE_UNAVAILABLE"

    original_run_git = publisher.run_git

    def invalid_path(_root: Path, *arguments: str, **_kwargs):
        if arguments[:3] == ("diff", "--name-only", "-z"):
            return subprocess.CompletedProcess(arguments, 0, stdout=b"\xff\0", stderr=b"")
        return original_run_git(_root, *arguments, **_kwargs)

    monkeypatch.setattr(publisher, "run_git", invalid_path)
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.changed_paths(staged, publisher.BASELINE_COMMIT, candidate)
    assert error.value.code == "HOLD_PATH_ENCODING_INVALID"


def test_schema_unavailable_blocks_rather_than_passing(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    staged, candidate = stage_candidate(tmp_path)
    import builtins

    original_import = builtins.__import__

    def deny(name: str, *arguments, **keywords):
        if name == "jsonschema":
            raise ImportError("fixture: jsonschema unavailable")
        return original_import(name, *arguments, **keywords)

    monkeypatch.setattr(builtins, "__import__", deny)
    with pytest.raises(publisher.ImplementationHoldError) as error:
        publisher.validate_schema(staged, candidate, publisher.RECORD_SCHEMA_PATH, publisher.render_record())
    assert error.value.code == "HOLD_SCHEMA_UNAVAILABLE"
    assert error.value.state == "BLOCKED"
