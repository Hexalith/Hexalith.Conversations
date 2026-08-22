"""Transaction and fault-injection tests for V16 lifecycle authority."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
from typing import Callable

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_v16_planning_tooling_lifecycle.py"
SPEC = importlib.util.spec_from_file_location("publish_v16_planning_tooling_lifecycle", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)


def git(path: Path, *arguments: str) -> str:
    return subprocess.check_output(["git", "-C", str(path), *arguments], text=True).strip()


def commit(path: Path, paths: tuple[str, ...], message: str) -> str:
    subprocess.run(["git", "-C", str(path), "add", "--", *paths], check=True)
    subprocess.run(["git", "-C", str(path), "commit", "-q", "-m", message], check=True)
    return git(path, "rev-parse", "HEAD")


def stage_candidate(
    tmp_path: Path,
    mutate: Callable[[Path], None] | None = None,
    extra_paths: tuple[str, ...] = (),
) -> tuple[Path, str]:
    staged = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(staged)], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", publisher.BASELINE_COMMIT], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.name", "V16 fixture"], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.email", "v16@example.invalid"], check=True)
    for relative in publisher.C1_PATHS:
        source = ROOT / relative
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    if mutate is not None:
        mutate(staged)
    candidate = commit(staged, (*publisher.C1_PATHS, *extra_paths), "fix(planning): stage V16 fixture")
    return staged, candidate


def stage_transaction(tmp_path: Path) -> tuple[Path, str, str]:
    staged, candidate = stage_candidate(tmp_path)
    document = publisher.publish(staged, candidate_revision=candidate, check=False)
    assert document["candidateCommit"] == candidate
    publication = commit(staged, (publisher.AUTHORITY_PATH,), "build(planning): bind V16 fixture")
    return staged, candidate, publication


def test_v16_binds_exact_transactions_packages_predecessors_and_ledger(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)

    document = publisher.publish(
        staged,
        candidate_revision=publication,
        publication_revision=publication,
        check=True,
        check_installed=True,
    )

    assert document["candidateCommit"] == candidate
    assert document["publication"]["c1Paths"] == list(publisher.C1_PATHS)
    assert document["publication"]["combinedPaths"] == list(publisher.COMBINED_PATHS)
    assert document["publication"]["changedGitlinks"] == []
    assert document["v15Publication"]["publicationCommit"] == publisher.V15_PUBLICATION_COMMIT
    assert document["environment"]["packageNames"] == list(publisher.PACKAGE_NAMES)
    assert len(document["immutableAuthorities"]) == 6
    assert document["assertionLedger"]
    assert all(row["state"] == "PASS" for row in document["assertionLedger"])


def test_descendant_and_unrelated_dirty_state_use_committed_publication(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)
    (staged / "README.md").write_text("descendant\n", encoding="utf-8")
    descendant = commit(staged, ("README.md",), "test: add V16 descendant")
    (staged / "unrelated.txt").write_text("dirty\n", encoding="utf-8")

    document = publisher.publish(staged, candidate_revision=descendant, check=True)

    assert document["candidateCommit"] == candidate
    assert publication == git(staged, "log", "--format=%H", "--diff-filter=A", descendant, "--", publisher.AUTHORITY_PATH)
    assert (staged / "unrelated.txt").read_text(encoding="utf-8") == "dirty\n"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document.__setitem__("unknown", True),
        lambda document: document["assertionLedger"].clear(),
        lambda document: document["environment"]["packages"][0]["sdist"].__setitem__("url", "https://example.invalid"),
        lambda document: document["immutableAuthorities"][0].__setitem__("sha256", "0" * 64),
        lambda document: document["authorityEffect"].__setitem__("implementationHold", "LIFTED"),
    ),
)
def test_closed_schema_rejects_unknown_weak_url_hash_and_authority_faults(tmp_path: Path, mutation) -> None:
    staged, candidate = stage_candidate(tmp_path)
    document = publisher.render_authority(staged, candidate)
    mutation(document)

    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.validate_schema(staged, candidate, document)

    assert error.value.code == "LIFECYCLE_AUTHORITY_SCHEMA_INVALID"


def test_c1_c2_scope_and_single_parent_faults_fail_closed(tmp_path: Path) -> None:
    def add_unexpected(root: Path) -> None:
        path = root / "_bmad/scripts/unexpected-v16.py"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("unexpected = True\n", encoding="utf-8")

    staged, candidate = stage_candidate(tmp_path / "scope", add_unexpected, ("_bmad/scripts/unexpected-v16.py",))
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == "LIFECYCLE_C1_SCOPE_DRIFT"

    staged, candidate = stage_candidate(tmp_path / "publication")
    publisher.publish(staged, candidate_revision=candidate, check=False)
    extra = staged / "_bmad-output/planning-artifacts/unexpected-v16.json"
    extra.write_text("{}\n", encoding="utf-8")
    publication = commit(
        staged,
        (publisher.AUTHORITY_PATH, "_bmad-output/planning-artifacts/unexpected-v16.json"),
        "test: drift V16 publication",
    )
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.validate_publication(staged, candidate, publication, publication)
    assert error.value.code == "LIFECYCLE_C2_SCOPE_DRIFT"

    staged, candidate = stage_candidate(tmp_path / "merge")
    subprocess.run(["git", "-C", str(staged), "branch", "side", publisher.BASELINE_COMMIT], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", "side"], check=True)
    (staged / "side.txt").write_text("side\n", encoding="utf-8")
    side = commit(staged, ("side.txt",), "test: add V16 side parent")
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", "main"], check=True)
    subprocess.run(["git", "-C", str(staged), "merge", "--no-ff", "-q", "-m", "test: merge V16 parent", side], check=True)
    merge = git(staged, "rev-parse", "HEAD")
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.render_authority(staged, merge)
    assert error.value.code == "LIFECYCLE_C1_PARENT_MISMATCH"
    assert error.value.state == "BLOCKED"


def test_non_ancestor_unavailable_history_and_descendant_drift_remain_distinct(tmp_path: Path) -> None:
    staged, _, publication = stage_transaction(tmp_path / "ancestry")
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.publish(
            staged,
            candidate_revision=publisher.BASELINE_COMMIT,
            publication_revision=publication,
            check=True,
        )
    assert error.value.code == "LIFECYCLE_PUBLICATION_NOT_ANCESTOR"
    assert error.value.state == "BLOCKED"

    staged, _, publication = stage_transaction(tmp_path / "drift")
    authority = staged / publisher.AUTHORITY_PATH
    authority.write_text(authority.read_text(encoding="utf-8").replace('"result": "PASS"', '"result": "FAIL"'), encoding="utf-8")
    descendant = commit(staged, (publisher.AUTHORITY_PATH,), "test: drift V16 descendant")
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.publish(staged, candidate_revision=descendant, publication_revision=publication, check=True)
    assert error.value.code == "LIFECYCLE_AUTHORITY_DESCENDANT_DRIFT"
    assert error.value.state == "FAIL"


def test_malformed_inputs_and_installed_metadata_return_stable_nonempty_results(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.parse_json(b"[]\n", "LIFECYCLE_AUTHORITY_INVALID")
    assert error.value.code == "LIFECYCLE_AUTHORITY_INVALID"

    original_run_git = publisher.run_git

    def invalid_path(_root: Path, *arguments: str, **_kwargs):
        if arguments[:3] == ("diff", "--name-only", "-z"):
            return subprocess.CompletedProcess(arguments, 0, stdout=b"\xff\0", stderr=b"")
        return original_run_git(_root, *arguments, **_kwargs)

    monkeypatch.setattr(publisher, "run_git", invalid_path)
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.changed_paths(ROOT, publisher.BASELINE_COMMIT, "HEAD")
    assert error.value.code == "LIFECYCLE_PATH_ENCODING_INVALID"
    monkeypatch.setattr(publisher, "run_git", original_run_git)

    def unavailable(_name: str) -> str:
        raise publisher.importlib.metadata.PackageNotFoundError("fixture")

    monkeypatch.setattr(publisher.importlib.metadata, "version", unavailable)
    with pytest.raises(publisher.LifecycleAuthorityError) as error:
        publisher.validate_installed_versions()
    assert error.value.code == "LIFECYCLE_INSTALLED_METADATA_UNAVAILABLE"
    assert error.value.state == "BLOCKED"

    result = publisher.failure_document(ROOT, error.value)
    assert result["result"] == "BLOCKED"
    assert result["assertionLedger"]
