"""Focused coverage for the V25 Story 7.1 preservation successor."""

from __future__ import annotations

import importlib.util
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py"
SPEC = importlib.util.spec_from_file_location("v25_preservation_successor", MODULE_PATH)
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
    """Clone the full pinned history and check out the V25 predecessor."""

    repository = tmp_path / "repository"
    subprocess.run(
        ["git", "clone", "--quiet", "--no-hardlinks", str(ROOT), str(repository)],
        check=True,
        capture_output=True,
        text=True,
    )
    git(repository, "checkout", "--quiet", publisher.PREDECESSOR_COMMIT)
    git(repository, "config", "user.name", "V25 fixture")
    git(repository, "config", "user.email", "v25-fixture@example.invalid")
    return repository


def copy_successor_inputs(repository: Path) -> None:
    """Copy the four non-record V25 blobs into a predecessor checkout."""

    for relative_path in publisher.V25_NON_RECORD_PATHS:
        destination = repository / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative_path, destination)
        destination.chmod(destination.stat().st_mode & ~0o111)


def prepare_successor(repository: Path) -> dict[str, object]:
    """Materialize an uncommitted deterministic V25 transaction."""

    copy_successor_inputs(repository)
    return publisher.write_document(repository)


def commit_successor(repository: Path, message: str = "fix(planning): publish V25 preservation successor fixture") -> str:
    """Commit the exact five-path V25 transaction."""

    git(repository, "add", "--", *publisher.V25_SCOPE)
    git(repository, "commit", "--quiet", "-m", message)
    return git(repository, "rev-parse", "HEAD")


def published_repository(tmp_path: Path) -> tuple[Path, str]:
    """Create one valid committed V25 repository."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    return repository, commit_successor(repository)


def commit_paths(repository: Path, message: str, *paths: str) -> str:
    """Commit named fixture paths and return the new candidate."""

    git(repository, "add", "-A", "--", *paths)
    git(repository, "commit", "--quiet", "-m", message)
    return git(repository, "rev-parse", "HEAD")


def test_schema_is_closed_and_pinned() -> None:
    """The successor schema is valid, closed, and pinned outside the record."""

    schema_bytes = (ROOT / publisher.SCHEMA_PATH).read_bytes()
    assert publisher.sha256_bytes(schema_bytes) == publisher.V25_SCHEMA_SHA256
    schema = json.loads(schema_bytes)
    jsonschema.Draft202012Validator.check_schema(schema)
    assert schema["additionalProperties"] is False
    assert schema["properties"]["successorTransaction"]["additionalProperties"] is False
    assert schema["properties"]["assertionLedger"]["minItems"] == 10


def test_predecessors_and_frozen_evidence_authenticate_before_generation() -> None:
    """Pinned V23/V24 and retained rc.1/rc.2 evidence validate independently."""

    head = publisher.resolve_commit(ROOT, "HEAD")
    links = publisher.authenticate_predecessors(ROOT, head)
    artifacts = publisher.validate_frozen_evidence(ROOT)
    assert len(links) == 10
    assert len(artifacts) == 17
    assert {row["revision"] for row in artifacts} >= {
        publisher.RC2_PUBLICATION_COMMIT,
        publisher.RC2_LOG_PROVENANCE_COMMIT,
    }


def test_generation_is_deterministic_closed_non_executable_and_nonvacuous() -> None:
    """Prospective generation binds four blobs and preserves every false authority flag."""

    first, _, _ = publisher.generate_document(ROOT)
    second, _, _ = publisher.generate_document(ROOT)
    assert first == second
    assert first["successorTransaction"]["exactChangedPaths"] == list(publisher.V25_SCOPE)
    assert len(first["successorTransaction"]["manifest"]) == 4
    assert len(first["assertionLedger"]) == 10
    assert first["result"] == "PASS"
    assert first["implementationHold"] == "ACTIVE"
    for field in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"):
        assert first[field] is False


def test_generation_rejects_bad_ancestry() -> None:
    """A candidate before V24 cannot import the successor trust boundary."""

    with pytest.raises(publisher.SuccessorError) as error:
        publisher.authenticate_predecessors(ROOT, publisher.V23_COMMIT)
    assert error.value.code == "V25_V24_NOT_ANCESTOR"


def test_capture_rejects_record_self_inclusion() -> None:
    """The self-excluded manifest cannot bind the record containing it."""

    with pytest.raises(publisher.SuccessorError) as error:
        publisher.capture_inputs(ROOT, publisher.V25_SCOPE)
    assert error.value.code == "V25_RECORD_SELF_INCLUSION"
    assert error.value.state == "FAIL"


def test_empty_ledger_is_rejected_by_closed_schema() -> None:
    """Zero evaluated assertions can never validate as PASS."""

    document, _, _ = publisher.generate_document(ROOT)
    document["assertionLedger"] = []
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.validate_schema(ROOT, document, (ROOT / publisher.SCHEMA_PATH).read_bytes())
    assert error.value.code == "V25_DOCUMENT_SCHEMA_INVALID"


def test_write_detects_stale_inputs_before_install(tmp_path: Path) -> None:
    """A post-snapshot mutation blocks without leaving a visible record."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    snapshots = publisher.capture_inputs(repository)
    test_path = repository / publisher.TEST_PATH
    test_path.write_bytes(test_path.read_bytes() + b"\n# stale input fixture\n")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.ensure_snapshots(repository, snapshots, publisher.PREDECESSOR_COMMIT)
    assert error.value.code == "V25_TOOLING_INPUT_DRIFT"
    assert not (repository / publisher.RECORD_PATH).exists()


def test_write_quarantines_only_its_record_on_post_install_failure(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """A late failure removes the visible output and preserves a quarantined diagnostic blob."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    original = publisher.ensure_snapshots
    calls = 0

    def fail_after_install(root: Path, snapshots: object, expected_head: str) -> None:
        nonlocal calls
        calls += 1
        original(root, snapshots, expected_head)
        if calls == 3:
            raise publisher.SuccessorError("V25_TOOLING_INPUT_DRIFT", "late fixture")

    monkeypatch.setattr(publisher, "ensure_snapshots", fail_after_install)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.write_document(repository)
    assert error.value.code == "V25_TOOLING_INPUT_DRIFT"
    assert not (repository / publisher.RECORD_PATH).exists()
    quarantines = list((repository / publisher.RECORD_PATH).parent.glob(f".{Path(publisher.RECORD_PATH).name}.quarantine.*"))
    assert len(quarantines) == 1


def test_valid_committed_successor_verifies(tmp_path: Path) -> None:
    """The exact direct-child five-path publication validates with a nonempty ledger."""

    repository, candidate = published_repository(tmp_path)
    document = publisher.verify_revision(repository, candidate)
    assert document["result"] == "PASS"
    assert len(document["assertionLedger"]) == 10
    assert git(repository, "diff-tree", "--no-commit-id", "--name-only", "-r", candidate).splitlines() == list(publisher.V25_SCOPE)


def test_cli_write_and_verify_emit_governed_success(tmp_path: Path) -> None:
    """Both CLI routes return zero and publish stable success tokens."""

    repository = clone_predecessor(tmp_path)
    copy_successor_inputs(repository)
    command = [sys.executable, str(repository / publisher.PUBLISHER_PATH), "--root", str(repository)]
    written = subprocess.run([*command, "--write"], check=False, capture_output=True, text=True)
    assert written.returncode == 0, written.stderr
    assert written.stdout.count("V25_STORY_7_1_PRESERVATION_SUCCESSOR_WRITTEN") == 1
    candidate = commit_successor(repository)
    verified = subprocess.run([*command, "--verify", candidate], check=False, capture_output=True, text=True)
    assert verified.returncode == 0, verified.stderr
    assert "V25_STORY_7_1_PRESERVATION_SUCCESSOR_OK" in verified.stdout
    assert '"result": "PASS"' in verified.stdout


def test_ninth_path_publication_fails_exact_scope(tmp_path: Path) -> None:
    """A sixth/ninth-style hidden path cannot fit inside the successor scope."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    (repository / "unexpected-v25.txt").write_text("unexpected\n", encoding="utf-8")
    git(repository, "add", "--", *publisher.V25_SCOPE, "unexpected-v25.txt")
    git(repository, "commit", "--quiet", "-m", "test: add unexpected V25 scope")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V25_SCOPE_DRIFT"
    assert "unexpected-v25.txt" in error.value.detail


def test_wrong_parent_publication_fails_lineage(tmp_path: Path) -> None:
    """V25 cannot be published after an undeclared intermediate commit."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    git(repository, "commit", "--quiet", "--allow-empty", "-m", "test: insert V25 parent")
    commit_successor(repository)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V25_PARENT_DRIFT"


def test_mode_drift_fails_before_record_import(tmp_path: Path) -> None:
    """An executable V25 source blob is rejected before its record is trusted."""

    repository = clone_predecessor(tmp_path)
    prepare_successor(repository)
    git(repository, "add", "--", *publisher.V25_SCOPE)
    git(repository, "update-index", "--chmod=+x", "--", publisher.SCHEMA_PATH)
    git(repository, "commit", "--quiet", "-m", "test: publish executable V25 schema")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V25_MODE_DRIFT"


def test_descendant_blob_drift_is_sticky(tmp_path: Path) -> None:
    """A descendant cannot silently replace any authenticated V25 blob."""

    repository, _ = published_repository(tmp_path)
    path = repository / publisher.CONFORMANCE_PATH
    path.write_bytes(path.read_bytes() + b"\n// descendant drift fixture\n")
    candidate = commit_paths(repository, "test: drift V25 consumer", publisher.CONFORMANCE_PATH)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V25_DESCENDANT_ARTIFACT_DRIFT"


def test_frozen_evidence_descendant_drift_is_sticky(tmp_path: Path) -> None:
    """A descendant cannot replace frozen rc.2 evidence while retaining historical objects."""

    repository, _ = published_repository(tmp_path)
    relative_path = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.md"
    path = repository / relative_path
    path.write_bytes(path.read_bytes() + b"\nDescendant drift fixture.\n")
    candidate = commit_paths(repository, "test: drift frozen rc.2 evidence", relative_path)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V25_FROZEN_ARTIFACT_DESCENDANT_DRIFT"


def test_v24_identity_descendant_drift_is_sticky(tmp_path: Path) -> None:
    """A descendant cannot replace the authenticated V24 correction record."""

    repository, _ = published_repository(tmp_path)
    path = repository / publisher.V24_RECORD_PATH
    path.write_bytes(path.read_bytes() + b"\n")
    candidate = commit_paths(repository, "test: drift immutable V24 identity", publisher.V24_RECORD_PATH)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V25_PREDECESSOR_DESCENDANT_DRIFT"


def test_record_deletion_is_sticky(tmp_path: Path) -> None:
    """Deleting the V25 marker blocks rather than falling back to V24."""

    repository, _ = published_repository(tmp_path)
    git(repository, "rm", "--quiet", "--", publisher.RECORD_PATH)
    git(repository, "commit", "--quiet", "-m", "test: delete V25 record")
    candidate = git(repository, "rev-parse", "HEAD")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V25_RECORD_REVERTED_OR_DELETED"


def test_record_readdition_is_a_duplicate_publication(tmp_path: Path) -> None:
    """Delete/re-add history is rejected even when the final bytes match."""

    repository, publication = published_repository(tmp_path)
    content = subprocess.run(
        ["git", "-C", str(repository), "show", f"{publication}:{publisher.RECORD_PATH}"],
        check=True,
        capture_output=True,
    ).stdout
    git(repository, "rm", "--quiet", "--", publisher.RECORD_PATH)
    git(repository, "commit", "--quiet", "-m", "test: delete V25 record")
    target = repository / publisher.RECORD_PATH
    target.write_bytes(content)
    candidate = commit_paths(repository, "test: re-add V25 record", publisher.RECORD_PATH)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, candidate)
    assert error.value.code == "V25_DUPLICATE_PUBLICATION"


def test_predecessor_identity_swap_is_rejected(tmp_path: Path) -> None:
    """A schema-valid V23/V24 identity swap cannot redefine the trust roots."""

    repository = clone_predecessor(tmp_path)
    document = prepare_successor(repository)
    document["lineage"]["v23"], document["lineage"]["v24"] = document["lineage"]["v24"], document["lineage"]["v23"]
    (repository / publisher.RECORD_PATH).write_bytes(publisher.canonical_json(document))
    commit_successor(repository)
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V25_RECORD_IDENTITY_MISMATCH"


def test_root_gitlink_drift_blocks_descendant(tmp_path: Path) -> None:
    """Raw mode-160000 root gitlink drift remains outside the successor authority."""

    repository, _ = published_repository(tmp_path)
    gitlink = publisher.raw_root_gitlinks(repository, "HEAD")[0]
    alternate = publisher.V23_COMMIT
    git(repository, "update-index", "--cacheinfo", f"160000,{alternate},{gitlink['path']}")
    git(repository, "commit", "--quiet", "-m", "test: drift V25 root gitlink")
    with pytest.raises(publisher.SuccessorError) as error:
        publisher.verify_revision(repository, "HEAD")
    assert error.value.code == "V25_ROOT_GITLINK_DRIFT"


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


def test_cli_failure_is_nonvacuous_and_uses_blocked_exit(tmp_path: Path) -> None:
    """Unavailable V25 history produces BLOCKED/2 with a nonempty ledger."""

    repository = clone_predecessor(tmp_path)
    command = [
        sys.executable,
        str(ROOT / publisher.PUBLISHER_PATH),
        "--root",
        str(repository),
        "--verify",
        "HEAD",
    ]
    result = subprocess.run(command, check=False, capture_output=True, text=True)
    assert result.returncode == 2
    envelope = json.loads(result.stdout.strip().splitlines()[-1])
    assert envelope["result"] == "BLOCKED"
    assert envelope["blockers"][0]["code"] == "V25_PUBLICATION_MISSING"
    assert envelope["assertionLedger"]
    assert envelope["executionAllowed"] is False
