"""Focused coverage for the V26 Story 7.1 committed-candidate test correction."""

from __future__ import annotations

import importlib.util
import json
import shutil
import subprocess
import sys
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py"
SPEC = importlib.util.spec_from_file_location("v26_committed_candidate_test_correction", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = publisher
SPEC.loader.exec_module(publisher)


def git(root: Path, *arguments: str) -> str:
    """Run Git in a test repository."""

    result = subprocess.run(
        ["git", "-C", str(root), *arguments],
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def clone_predecessor(tmp_path: Path) -> Path:
    """Clone full pinned history and check out the V26 predecessor."""

    repository = tmp_path / "repository"
    subprocess.run(
        ["git", "clone", "--quiet", "--no-hardlinks", str(ROOT), str(repository)],
        check=True,
        capture_output=True,
        text=True,
    )
    git(repository, "checkout", "--quiet", publisher.PREDECESSOR_COMMIT)
    git(repository, "config", "user.name", "V26 fixture")
    git(repository, "config", "user.email", "v26-fixture@example.invalid")
    return repository


def copy_successor_inputs(repository: Path) -> None:
    """Copy the four non-record V26 blobs into a predecessor checkout."""

    for relative_path in publisher.V26_NON_RECORD_PATHS:
        destination = repository / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative_path, destination)
        destination.chmod(destination.stat().st_mode & ~0o111)


def prepare_successor(repository: Path) -> dict[str, object]:
    """Materialize an uncommitted deterministic V26 transaction."""

    copy_successor_inputs(repository)
    return publisher.write_document(repository)


def commit_successor(repository: Path, message: str = "fix(planning): publish V26 test correction fixture") -> str:
    """Commit the exact five-path V26 transaction."""

    git(repository, "add", "--", *publisher.V26_SCOPE)
    git(repository, "commit", "--quiet", "-m", message)
    return git(repository, "rev-parse", "HEAD")


def published_repository(tmp_path: Path) -> tuple[Path, str]:
    """Create one valid committed V26 repository."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    return repository, commit_successor(repository)


def commit_paths(repository: Path, message: str, *paths: str) -> str:
    """Commit named fixture paths and return the new candidate."""

    git(repository, "add", "-A", "--", *paths)
    git(repository, "commit", "--quiet", "-m", message)
    return git(repository, "rev-parse", "HEAD")


def run_v25_suite(root: Path) -> subprocess.CompletedProcess[str]:
    """Run the corrected committed-candidate V25 suite from one worktree."""

    return subprocess.run(
        [
            sys.executable,
            "-m",
            "pytest",
            "-q",
            "-ra",
            "-o",
            "xfail_strict=true",
            publisher.V25_TEST_PATH,
        ],
        cwd=root,
        check=False,
        capture_output=True,
        text=True,
    )


def test_schema_is_closed_and_pinned() -> None:
    """The V26 schema is valid, closed, and pinned outside the record."""

    schema_bytes = (ROOT / publisher.SCHEMA_PATH).read_bytes()
    assert publisher.sha256_bytes(schema_bytes) == publisher.V26_SCHEMA_SHA256
    schema = json.loads(schema_bytes)
    jsonschema.Draft202012Validator.check_schema(schema)
    assert schema["additionalProperties"] is False
    assert schema["properties"]["successorTransaction"]["additionalProperties"] is False
    assert schema["properties"]["assertionLedger"]["minItems"] == 9


def test_historical_v25_authenticates_before_candidate_import() -> None:
    """V25 passes at its exact publication with all five original blobs pinned."""

    historical, bindings, links = publisher.authenticate_v25(ROOT, publisher.PREDECESSOR_COMMIT)
    assert historical["result"] == "PASS"
    assert len(historical["assertionLedger"]) == 10
    assert [row["path"] for row in bindings] == list(publisher.V25_SCOPE)
    assert len(links) == 10


def test_historical_v25_identity_fault_blocks_before_import(monkeypatch: pytest.MonkeyPatch) -> None:
    """A pinned V25 blob mismatch produces a stable V26 blocker."""

    object_id, _, byte_count = publisher.V25_IDENTITIES[publisher.V25_TEST_PATH]
    monkeypatch.setitem(
        publisher.V25_IDENTITIES,
        publisher.V25_TEST_PATH,
        (object_id, "0" * 64, byte_count),
    )
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.authenticate_v25(ROOT, publisher.PREDECESSOR_COMMIT)
    assert error.value.code == "V26_V25_ARTIFACT_MISMATCH"


def test_generation_is_deterministic_closed_non_executable_and_nonvacuous(tmp_path: Path) -> None:
    """Prospective generation binds four blobs and preserves all false authority flags."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    first, _, _ = publisher.generate_document(repository)
    second, _, _ = publisher.generate_document(repository)
    assert first == second
    assert first["successorTransaction"]["exactChangedPaths"] == list(publisher.V26_SCOPE)
    assert len(first["successorTransaction"]["manifest"]) == 4
    assert len(first["historicalV25"]["originalBlobs"]) == 5
    assert len(first["assertionLedger"]) == 9
    assert first["result"] == "PASS"
    assert first["implementationHold"] == "ACTIVE"
    for field in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"):
        assert first[field] is False


def test_generation_is_deterministic_from_linked_predecessor_fixture(tmp_path: Path) -> None:
    """Prospective generation is independent of .git directory layout."""

    repository = clone_predecessor(tmp_path)
    linked = tmp_path / "linked"
    git(repository, "worktree", "add", "--quiet", "--detach", str(linked), publisher.PREDECESSOR_COMMIT)
    try:
        copy_successor_inputs(linked)
        before = git(linked, "status", "--short")
        first, _, _ = publisher.generate_document(linked)
        second, _, _ = publisher.generate_document(linked)
        assert (linked / ".git").is_file()
        assert first == second
        assert git(linked, "status", "--short") == before
    finally:
        git(repository, "worktree", "remove", "--force", str(linked))


def test_capture_rejects_record_self_inclusion() -> None:
    """The self-excluded manifest cannot bind the record containing it."""

    with pytest.raises(publisher.SuccessorError) as error:
        publisher.capture_inputs(ROOT, publisher.V26_SCOPE)
    assert error.value.code == "V26_RECORD_SELF_INCLUSION"
    assert error.value.state == "FAIL"


def test_empty_ledger_is_rejected_by_closed_schema(tmp_path: Path) -> None:
    """Zero evaluated assertions can never validate as PASS."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    document, _, _ = publisher.generate_document(repository)
    document["assertionLedger"] = []
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.validate_schema(document, (repository / publisher.SCHEMA_PATH).read_bytes())
    assert error.value.code == "V26_DOCUMENT_SCHEMA_INVALID"
    assert error.value.state == "FAIL"


def test_write_detects_stale_inputs_before_install(tmp_path: Path) -> None:
    """A post-snapshot mutation blocks without leaving a visible record."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    snapshots = publisher.capture_inputs(repository)
    test_path = repository / publisher.TEST_PATH
    test_path.write_bytes(test_path.read_bytes() + b"\n# stale input fixture\n")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.ensure_snapshots(repository, snapshots, publisher.PREDECESSOR_COMMIT)
    assert error.value.code == "V26_TOOLING_INPUT_DRIFT"
    assert not (repository / publisher.RECORD_PATH).exists()


def test_write_quarantines_only_its_record_on_post_install_failure(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """A late failure removes visible output and preserves one quarantined blob."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    original = publisher.ensure_snapshots
    calls = 0

    def fail_after_install(root: Path, snapshots: object, expected_head: str) -> None:
        nonlocal calls
        calls += 1
        original(root, snapshots, expected_head)
        if calls == 3:
            raise publisher.SuccessorError("V26_TOOLING_INPUT_DRIFT", "late fixture")

    monkeypatch.setattr(publisher, "ensure_snapshots", fail_after_install)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.write_document(repository)
    assert error.value.code == "V26_TOOLING_INPUT_DRIFT"
    assert not (repository / publisher.RECORD_PATH).exists()
    quarantines = list((repository / publisher.RECORD_PATH).parent.glob(f".{Path(publisher.RECORD_PATH).name}.quarantine.*"))
    assert len(quarantines) == 1


def test_valid_committed_successor_verifies(tmp_path: Path) -> None:
    """The exact direct-child five-path V26 publication validates nonvacuously."""

    repository, candidate = published_repository(tmp_path)
    document = publisher.verify_revision(repository, candidate)
    assert document["result"] == "PASS"
    assert len(document["assertionLedger"]) == 9
    assert document["correction"]["authorizedPath"] == publisher.V25_TEST_PATH
    assert document["correction"]["originalBinding"] != document["correction"]["replacementBinding"]
    assert git(repository, "diff-tree", "--no-commit-id", "--name-only", "-r", candidate).splitlines() == list(publisher.V26_SCOPE)


def test_cli_write_and_verify_emit_governed_success(tmp_path: Path) -> None:
    """Both CLI routes return zero and publish stable success tokens."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    command = [sys.executable, str(repository / publisher.PUBLISHER_PATH), "--root", str(repository)]
    written = subprocess.run([*command, "--write"], check=False, capture_output=True, text=True)
    assert written.returncode == 0, written.stderr
    assert written.stdout.count("V26_STORY_7_1_TEST_CORRECTION_WRITTEN") == 1
    candidate = commit_successor(repository)
    verified = subprocess.run([*command, "--verify", candidate], check=False, capture_output=True, text=True)
    assert verified.returncode == 0, verified.stderr
    assert "V26_STORY_7_1_TEST_CORRECTION_OK" in verified.stdout
    assert '"result": "PASS"' in verified.stdout


def test_unexpected_path_publication_fails_exact_scope(tmp_path: Path) -> None:
    """A hidden sixth path cannot fit inside the successor scope."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    (repository / "unexpected-v26.txt").write_text("unexpected\n", encoding="utf-8")
    git(repository, "add", "--", *publisher.V26_SCOPE, "unexpected-v26.txt")
    git(repository, "commit", "--quiet", "-m", "test: add unexpected V26 scope")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V26_SCOPE_DRIFT"
    assert "unexpected-v26.txt" in error.value.detail


def test_wrong_parent_publication_fails_lineage(tmp_path: Path) -> None:
    """V26 cannot be published after an undeclared intermediate commit."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    git(repository, "commit", "--quiet", "--allow-empty", "-m", "test: insert V26 parent")
    commit_successor(repository)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V26_PARENT_DRIFT"


def test_mode_drift_fails_before_record_import(tmp_path: Path) -> None:
    """An executable V26 source blob is rejected before its record is trusted."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    git(repository, "add", "--", *publisher.V26_SCOPE)
    git(repository, "update-index", "--chmod=+x", "--", publisher.SCHEMA_PATH)
    git(repository, "commit", "--quiet", "-m", "test: publish executable V26 schema")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V26_MODE_DRIFT"


def test_descendant_blob_drift_is_sticky(tmp_path: Path) -> None:
    """A descendant cannot silently replace any authenticated V26 blob."""

    repository, _ = published_repository(tmp_path)
    path = repository / publisher.TEST_PATH
    path.write_bytes(path.read_bytes() + b"\n# descendant drift fixture\n")
    candidate = commit_paths(repository, "test: drift V26 tests", publisher.TEST_PATH)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V26_DESCENDANT_ARTIFACT_DRIFT"


def test_unmodified_v25_blob_drift_is_sticky(tmp_path: Path) -> None:
    """A descendant cannot alter a V25 blob outside the authorized test replacement."""

    repository, _ = published_repository(tmp_path)
    path = repository / publisher.V25_CONFORMANCE_PATH
    path.write_bytes(path.read_bytes() + b"\n// V25 retained drift fixture\n")
    candidate = commit_paths(repository, "test: drift retained V25 consumer", publisher.V25_CONFORMANCE_PATH)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V26_V25_RETAINED_ARTIFACT_DRIFT"


def test_record_deletion_is_sticky(tmp_path: Path) -> None:
    """Deleting the V26 marker blocks rather than falling back to V25."""

    repository, _ = published_repository(tmp_path)
    git(repository, "rm", "--quiet", "--", publisher.RECORD_PATH)
    git(repository, "commit", "--quiet", "-m", "test: delete V26 record")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V26_RECORD_REVERTED_OR_DELETED"


def test_record_readdition_is_a_duplicate_publication(tmp_path: Path) -> None:
    """Delete/re-add history is rejected even when final bytes match."""

    repository, publication = published_repository(tmp_path)
    content = subprocess.run(
        ["git", "-C", str(repository), "show", f"{publication}:{publisher.RECORD_PATH}"],
        check=True,
        capture_output=True,
    ).stdout
    git(repository, "rm", "--quiet", "--", publisher.RECORD_PATH)
    git(repository, "commit", "--quiet", "-m", "test: delete V26 record")
    target = repository / publisher.RECORD_PATH
    target.write_bytes(content)
    candidate = commit_paths(repository, "test: re-add V26 record", publisher.RECORD_PATH)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V26_DUPLICATE_PUBLICATION"


def test_original_and_replacement_identity_swap_is_rejected(tmp_path: Path) -> None:
    """A schema-valid correction identity swap cannot redefine the trust roots."""

    repository = clone_predecessor(tmp_path)
    document = prepare_successor(repository)
    document["correction"]["originalBinding"], document["correction"]["replacementBinding"] = (
        document["correction"]["replacementBinding"],
        document["correction"]["originalBinding"],
    )
    (repository / publisher.RECORD_PATH).write_bytes(publisher.canonical_json(document))
    commit_successor(repository)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V26_RECORD_IDENTITY_MISMATCH"


def test_root_gitlink_drift_blocks_descendant(tmp_path: Path) -> None:
    """Raw mode-160000 root gitlink drift remains outside V26 authority."""

    repository, _ = published_repository(tmp_path)
    gitlink = publisher.raw_root_gitlinks(repository, "HEAD")[0]
    git(repository, "update-index", "--cacheinfo", f"160000,{publisher.V25_PARENT},{gitlink['path']}")
    git(repository, "commit", "--quiet", "-m", "test: drift V26 root gitlink")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V26_V25_GITLINK_DRIFT"


def test_valid_successor_verifies_from_linked_worktree(tmp_path: Path) -> None:
    """Verification is independent of whether .git is a directory or gitfile."""

    repository, candidate = published_repository(tmp_path)
    linked = tmp_path / "linked"
    git(repository, "worktree", "add", "--quiet", "--detach", str(linked), candidate)
    try:
        assert (linked / ".git").is_file()
        document = publisher.verify_revision(linked, "HEAD")
        assert document["result"] == "PASS"
    finally:
        git(repository, "worktree", "remove", "--force", str(linked))


def test_cli_blocked_state_is_nonvacuous(tmp_path: Path) -> None:
    """Unavailable V26 history produces BLOCKED/2 with a nonempty ledger."""

    repository = clone_predecessor(tmp_path)
    command = [sys.executable, str(ROOT / publisher.PUBLISHER_PATH), "--root", str(repository), "--verify", "HEAD"]
    result = subprocess.run(command, check=False, capture_output=True, text=True)
    assert result.returncode == 2
    envelope = json.loads(result.stdout.strip().splitlines()[-1])
    assert envelope["result"] == "BLOCKED"
    assert envelope["blockers"][0]["code"] == "V26_PUBLICATION_MISSING"
    assert envelope["assertionLedger"]
    assert envelope["executionAllowed"] is False


def test_cli_fail_state_is_nonvacuous(tmp_path: Path) -> None:
    """A malformed committed V26 record produces FAIL/1 with a nonempty ledger."""

    repository = clone_predecessor(tmp_path)
    document = prepare_successor(repository)
    document["assertionLedger"] = []
    (repository / publisher.RECORD_PATH).write_bytes(publisher.canonical_json(document))
    candidate = commit_successor(repository)
    command = [sys.executable, str(repository / publisher.PUBLISHER_PATH), "--root", str(repository), "--verify", candidate]
    result = subprocess.run(command, check=False, capture_output=True, text=True)
    assert result.returncode == 1
    envelope = json.loads(result.stdout.strip().splitlines()[-1])
    assert envelope["result"] == "FAIL"
    assert envelope["blockers"][0]["code"] == "V26_DOCUMENT_SCHEMA_INVALID"
    assert envelope["assertionLedger"]


def test_committed_candidate_v25_suite_passes_in_primary_and_linked_worktrees(tmp_path: Path) -> None:
    """The corrected V25 suite is skip-free from both committed worktree forms."""

    repository, candidate = published_repository(tmp_path)
    primary = run_v25_suite(repository)
    assert primary.returncode == 0, primary.stdout + primary.stderr
    assert "skipped" not in primary.stdout.lower()
    assert "xfailed" not in primary.stdout.lower()
    assert "xpassed" not in primary.stdout.lower()
    assert not git(repository, "status", "--short")

    linked = tmp_path / "linked"
    git(repository, "worktree", "add", "--quiet", "--detach", str(linked), candidate)
    try:
        result = run_v25_suite(linked)
        assert result.returncode == 0, result.stdout + result.stderr
        assert "skipped" not in result.stdout.lower()
        assert "xfailed" not in result.stdout.lower()
        assert "xpassed" not in result.stdout.lower()
        assert not git(linked, "status", "--short")
    finally:
        git(repository, "worktree", "remove", "--force", str(linked))
