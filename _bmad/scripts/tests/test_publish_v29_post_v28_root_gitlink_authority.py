"""Focused publication and fault fixtures for the V29 root-gitlink successor."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys

import pytest


ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location(
    "v29_root_gitlink_publisher", ROOT / "_bmad/scripts/publish_v29_post_v28_root_gitlink_authority.py"
)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = publisher
SPEC.loader.exec_module(publisher)


def git(root: Path, *args: str) -> str:
    """Run fixture Git and return stripped stdout."""

    return subprocess.check_output(["git", "-C", str(root), *args], text=True).strip()


def commit(root: Path, message: str) -> str:
    """Commit only the staged fixture transaction."""

    subprocess.run(["git", "-C", str(root), "-c", "commit.gpgsign=false", "commit", "-q", "-m", message], check=True)
    return git(root, "rev-parse", "HEAD")


def bootstrap_repository(tmp_path: Path) -> tuple[Path, str, str]:
    """Publish a spec-only predecessor and exact nine-path C1 in an isolated clone."""

    root = tmp_path / "v29-repository"
    subprocess.run(["git", "clone", "--shared", "--no-checkout", "-q", str(ROOT), str(root)], check=True)
    git(root, "sparse-checkout", "init", "--no-cone")
    git(root, "sparse-checkout", "set", "--no-cone", "/*", "!/references/*", "!/src/*", "!/tests/*", "!/artifacts/*")
    git(root, "checkout", "-q", "--detach", publisher.BASELINE_COMMIT)
    git(root, "config", "user.name", "V29 fixture")
    git(root, "config", "user.email", "v29-fixture@example.invalid")
    spec = root / publisher.SPEC_PATH
    spec.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ROOT / publisher.SPEC_PATH, spec)
    git(root, "add", "--sparse", "--", publisher.SPEC_PATH)
    predecessor = commit(root, "docs(v29): publish test predecessor")
    for relative_path in publisher.BOOTSTRAP_PATHS:
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative_path, target)
        target.chmod(0o644)
    git(root, "add", "--", *publisher.BOOTSTRAP_PATHS)
    bootstrap = commit(root, "feat(v29): publish test bootstrap")
    assert git(root, "diff-tree", "--no-commit-id", "--name-only", "-r", predecessor, bootstrap).splitlines() == list(publisher.BOOTSTRAP_PATHS)
    return root, predecessor, bootstrap


def published_repository(tmp_path: Path) -> tuple[Path, str, str, str]:
    """Publish the deterministic record-only C2 after C1."""

    root, predecessor, bootstrap = bootstrap_repository(tmp_path)
    (root / publisher.RECORD_PATH).parent.mkdir(parents=True, exist_ok=True)
    document = publisher.write_document(root)
    assert document["result"] == "PASS"
    regenerated, _ = publisher.generate_document(root)
    assert (root / publisher.RECORD_PATH).read_bytes() == publisher.canonical_json(regenerated)
    git(root, "add", "--sparse", "--", publisher.RECORD_PATH)
    c2 = commit(root, "docs(v29): publish test record")
    assert git(root, "diff-tree", "--no-commit-id", "--name-only", "-r", bootstrap, c2) == publisher.RECORD_PATH
    return root, predecessor, bootstrap, c2


def test_v29_history_digest_and_seventeen_raw_rows() -> None:
    """The live six-commit history independently yields the approved path and row digests."""

    rows = []
    for commit_id in publisher.APPROVED_COMMITS:
        parent = publisher.commit_parents(ROOT, commit_id)[0]
        for row in publisher.changed_path_rows(ROOT, parent, commit_id):
            if row["mode"] != "160000":
                continue
            before = publisher.tree_entry(ROOT, parent, row["path"], "V29_HISTORICAL_DIFF_DRIFT")
            after = publisher.tree_entry(ROOT, commit_id, row["path"], "V29_HISTORICAL_DIFF_DRIFT")
            rows.append((commit_id, row["path"], before[2], after[2]))
    assert len(rows) == 17
    assert tuple(rows) == publisher.APPROVED_ROWS
    assert publisher.sha256_bytes("".join(path + "\n" for path in sorted({row[1] for row in rows})).encode()) == publisher.APPROVED_PATH_SHA256
    assert publisher.sha256_bytes("".join("\t".join(row) + "\n" for row in rows).encode()) == publisher.APPROVED_ROW_SHA256


def test_v29_c1_blocks_until_record_publication(tmp_path: Path) -> None:
    """C1 has a nonempty BLOCKED ledger when C2 has not been published."""

    root, _predecessor, bootstrap = bootstrap_repository(tmp_path)
    result = publisher.verify_revision(root, bootstrap, bootstrap)
    assert result["result"] == "BLOCKED"
    assert result["blockers"][0]["code"] == "V29_C2_PUBLICATION_MISSING"
    assert result["assertionLedger"]


def test_v29_c2_and_untouched_descendant_pass(tmp_path: Path) -> None:
    """C2 and its untouched descendant pass with truthful parent diffs and the active hold."""

    root, _predecessor, bootstrap, c2 = published_repository(tmp_path)
    for candidate in (c2,):
        result = publisher.verify_revision(root, candidate, bootstrap)
        assert result["result"] == "PASS"
        assert result["implementationHold"] == "ACTIVE"
        assert all(result[flag] is False for flag in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"))
        assert result["assertionLedger"]
        parent = publisher.commit_parents(root, candidate)[0]
        assert result["observed"]["changedPaths"] == publisher.changed_path_rows(root, parent, candidate)
    note = root / "v29-untouched-note.txt"
    note.write_text("untouched descendant\n", encoding="utf-8")
    git(root, "add", "--", note.name)
    descendant = commit(root, "docs(v29): record untouched test descendant")
    result = publisher.verify_revision(root, descendant, bootstrap)
    assert result["result"] == "PASS"
    assert result["observed"]["parentCommit"] == c2
    assert result["observed"]["changedPaths"][0]["path"] == note.name


def test_v29_later_gitlink_touch_fails_even_after_restoration(tmp_path: Path) -> None:
    """The historical no-touch guard detects a later change and its restoration."""

    root, _predecessor, bootstrap, c2 = published_repository(tmp_path)
    path = "references/Hexalith.EventStore"
    original = dict(publisher.FROZEN_FINAL_LINKS)[path]
    changed = publisher.APPROVED_ROWS[-1][2]
    assert changed != original
    git(root, "update-index", "--cacheinfo", f"160000,{changed},{path}")
    touched = commit(root, "test(v29): touch frozen gitlink")
    first = publisher.verify_revision(root, touched, bootstrap)
    assert first["result"] == "FAIL"
    assert first["blockers"][0]["code"] == "V29_GOVERNED_PATH_TOUCHED"
    git(root, "update-index", "--cacheinfo", f"160000,{original},{path}")
    restored = commit(root, "test(v29): restore frozen gitlink")
    second = publisher.verify_revision(root, restored, bootstrap)
    assert second["result"] == "FAIL"
    assert second["blockers"][0]["code"] == "V29_GOVERNED_PATH_TOUCHED"
    assert git(root, "rev-parse", f"{c2}:{path}") == original


def test_v29_schema_rejects_empty_ledger() -> None:
    """An applicable passing envelope cannot omit its evaluated assertion ledger."""

    from jsonschema import Draft202012Validator

    schema = json.loads((ROOT / publisher.SCHEMA_PATH).read_text(encoding="utf-8"))
    valid = publisher.result_envelope("BLOCKED", "BLOCKED", publisher.empty_observation(), [],
                                      [{"code": "V29_HISTORY_UNAVAILABLE", "detail": "unavailable"}])
    Draft202012Validator(schema).validate(valid)
    valid["assertionLedger"] = []
    assert not Draft202012Validator(schema).is_valid(valid)


def test_v29_unavailable_history_blocks_with_nonempty_ledger(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """History loss cannot be promoted into an applicable PASS or drift FAIL."""

    root, _predecessor, bootstrap = bootstrap_repository(tmp_path)

    def unavailable(_root: Path) -> None:
        raise publisher.SuccessorError("V29_HISTORY_UNAVAILABLE", "synthetic missing objects")

    monkeypatch.setattr(publisher, "require_complete_history", unavailable)
    result = publisher.verify_revision(root, bootstrap, bootstrap)
    assert result["result"] == "BLOCKED"
    assert result["blockers"][0]["code"] == "V29_HISTORY_UNAVAILABLE"
    assert result["assertionLedger"]


def test_v29_one_changed_historical_transition_fails(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """A single altered raw row invalidates the pinned 17-transition ledger."""

    root, _predecessor, bootstrap = bootstrap_repository(tmp_path)
    original = publisher.changed_path_rows

    def one_bad_row(repository: Path, parent: str, commit_id: str) -> list[dict[str, object]]:
        rows = original(repository, parent, commit_id)
        if commit_id == publisher.APPROVED_COMMITS[0]:
            rows[0]["path"] = "references/Hexalith.Parties"
        return rows

    monkeypatch.setattr(publisher, "changed_path_rows", one_bad_row)
    result = publisher.verify_revision(root, bootstrap, bootstrap)
    assert result["result"] == "FAIL"
    assert result["blockers"][0]["code"] == "V29_HISTORICAL_DIFF_DRIFT"
    assert result["assertionLedger"]


def test_v29_record_touch_fails_after_c2(tmp_path: Path) -> None:
    """A later C2 record edit fails even if its replacement is valid JSON."""

    root, _predecessor, bootstrap, _c2 = published_repository(tmp_path)
    record = root / publisher.RECORD_PATH
    value = json.loads(record.read_text(encoding="utf-8"))
    value["executionAllowed"] = True
    record.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    git(root, "add", "--sparse", "--", publisher.RECORD_PATH)
    changed = commit(root, "test(v29): change frozen record")
    result = publisher.verify_revision(root, changed, bootstrap)
    assert result["result"] == "FAIL"
    assert result["blockers"][0]["code"] == "V29_RECORD_HISTORY_TOUCHED"
    assert result["assertionLedger"]
