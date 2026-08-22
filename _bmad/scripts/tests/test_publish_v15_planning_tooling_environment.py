"""Transaction and fault-injection tests for V15 planning-tooling authority."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
from typing import Callable

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_v15_planning_tooling_environment.py"
SPEC = importlib.util.spec_from_file_location("publish_v15_planning_tooling_environment", MODULE_PATH)
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
    subprocess.run(["git", "-C", str(staged), "config", "user.name", "V15 fixture"], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.email", "v15@example.invalid"], check=True)
    for relative in publisher.C1_PATHS:
        source = ROOT / relative
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    if mutate is not None:
        mutate(staged)
    candidate = commit(staged, (*publisher.C1_PATHS, *extra_paths), "build(deps): stage V15 fixture")
    return staged, candidate


def stage_transaction(tmp_path: Path) -> tuple[Path, str, str]:
    staged, candidate = stage_candidate(tmp_path)
    document = publisher.publish(staged, candidate_revision=candidate, check=False)
    assert document["candidateCommit"] == candidate
    publication = commit(staged, (publisher.AUTHORITY_PATH,), "build(deps): bind V15 fixture")
    return staged, candidate, publication


def test_current_environment_has_exact_versions_hashes_modes_and_thirteen_packages(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)

    document = publisher.publish(
        staged,
        candidate_revision=publication,
        check=True,
        publication_revision=publication,
    )

    assert document["environment"]["packageCount"] == 13
    assert [row["version"] for row in document["environment"]["packages"]] == ["4.26.0", "9.1.1"]
    assert all(row["mode"] == "100644" for row in document["candidateFiles"])
    assert document["publication"]["combinedPaths"] == list(publisher.COMBINED_PATHS)
    assert document["publication"]["changedGitlinks"] == []
    assert document["assertionLedger"]
    assert all(row["state"] == "PASS" for row in document["assertionLedger"])


def test_historical_v9_checker_reproduces_from_isolated_baseline(tmp_path: Path) -> None:
    staged = tmp_path / "historical"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(staged)], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", publisher.BASELINE_COMMIT], check=True)

    result = subprocess.run(
        [
            sys.executable,
            str(staged / "_bmad/scripts/publish_v9_planning_authority.py"),
            "--repository",
            str(staged),
            "--check",
        ],
        cwd=staged,
        capture_output=True,
        text=True,
        check=False,
        timeout=120,
    )

    assert result.returncode == 0, result.stderr or result.stdout
    assert "V14_PLANNING_AUTHORITY_OK" in result.stdout
    assert publisher.sha256((staged / publisher.V9_AUTHORITY_PATH).read_bytes()) == publisher.V9_AUTHORITY_SHA256


@pytest.mark.parametrize(
    ("mutation", "expected_code"),
    (
        (
            lambda root: (root / "pyproject.toml").write_text(
                (root / "pyproject.toml").read_text(encoding="utf-8").replace("jsonschema==4.26.0", "jsonschema==4.25.0"),
                encoding="utf-8",
            ),
            "TOOLING_MANIFEST_VERSION_MISMATCH",
        ),
        (
            lambda root: (root / "uv.lock").write_text(
                (root / "uv.lock").read_text(encoding="utf-8").replace(
                    "sha256:0c26707e2efad8aa1bfc5b7ce170f3fccc2e4918ff85989ba9ffa9facb2be326",
                    "sha256:" + "0" * 64,
                    1,
                ),
                encoding="utf-8",
            ),
            "TOOLING_LOCK_HASH_MISMATCH",
        ),
    ),
)
def test_manifest_version_and_lock_hash_faults_fail_closed(tmp_path: Path, mutation, expected_code: str) -> None:
    staged, candidate = stage_candidate(tmp_path, mutation)

    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.render_authority(staged, candidate)

    assert error.value.code == expected_code


def test_scope_self_reference_mode_and_publication_faults_fail_closed(tmp_path: Path) -> None:
    def add_unexpected(root: Path) -> None:
        path = root / "_bmad/scripts/unexpected-v15.py"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("unexpected = True\n", encoding="utf-8")

    staged, candidate = stage_candidate(tmp_path / "scope", add_unexpected, ("_bmad/scripts/unexpected-v15.py",))
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == "TOOLING_SCOPE_DRIFT"

    def add_self_reference(root: Path) -> None:
        path = root / publisher.AUTHORITY_PATH
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("{}\n", encoding="utf-8")

    staged, candidate = stage_candidate(tmp_path / "self", add_self_reference, (publisher.AUTHORITY_PATH,))
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == "TOOLING_SELF_REFERENCE"

    def executable_mode(root: Path) -> None:
        (root / "_bmad/scripts/publish_v15_planning_tooling_environment.py").chmod(0o755)

    staged, candidate = stage_candidate(tmp_path / "mode", executable_mode)
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == "TOOLING_MODE_DRIFT"

    staged, candidate = stage_candidate(tmp_path / "publication")
    authority = staged / publisher.AUTHORITY_PATH
    authority.parent.mkdir(parents=True, exist_ok=True)
    authority.write_text("{}\n", encoding="utf-8")
    extra = staged / "_bmad-output/planning-artifacts/unexpected-v15.json"
    extra.write_text("{}\n", encoding="utf-8")
    publication = commit(
        staged,
        (publisher.AUTHORITY_PATH, "_bmad-output/planning-artifacts/unexpected-v15.json"),
        "build(deps): drift V15 publication fixture",
    )
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_publication(staged, candidate, publication, publication)
    assert error.value.code == "TOOLING_PUBLICATION_SCOPE_DRIFT"


@pytest.mark.parametrize(
    ("relative_path", "replacement", "expected_code"),
    (
        (publisher.V9_AUTHORITY_PATH, b"{}\n", "TOOLING_PREDECESSOR_DRIFT"),
        (
            publisher.IR0_PATH,
            b"---\nresult: BLOCKED\neffective_hold: ACTIVE\n---\n",
            "TOOLING_IR0_DRIFT",
        ),
    ),
)
def test_predecessor_and_ir0_faults_have_stable_failures(
    tmp_path: Path,
    relative_path: str,
    replacement: bytes,
    expected_code: str,
) -> None:
    def mutate(root: Path) -> None:
        (root / relative_path).write_bytes(replacement)

    staged, candidate = stage_candidate(tmp_path, mutate, (relative_path,))

    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_predecessors(staged, candidate)

    assert error.value.code == expected_code


def test_descendant_and_dirty_worktree_validate_committed_publication(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)
    (staged / "README.md").write_text("descendant\n", encoding="utf-8")
    descendant = commit(staged, ("README.md",), "test: add V15 descendant")
    (staged / "unrelated.txt").write_text("dirty\n", encoding="utf-8")

    document = publisher.publish(staged, candidate_revision=descendant, check=True)

    assert document["candidateCommit"] == candidate
    assert publication == git(staged, "log", "--format=%H", "--diff-filter=A", descendant, "--", publisher.AUTHORITY_PATH)
    assert (staged / "unrelated.txt").read_text(encoding="utf-8") == "dirty\n"


def test_non_ancestor_descendant_and_multi_parent_candidate_block(tmp_path: Path) -> None:
    staged, _, publication = stage_transaction(tmp_path / "ancestry")

    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.publish(staged, candidate_revision=publisher.BASELINE_COMMIT, check=True, publication_revision=publication)
    assert error.value.code == "TOOLING_PUBLICATION_NOT_ANCESTOR"
    assert error.value.state == "BLOCKED"

    staged, candidate = stage_candidate(tmp_path / "merge")
    subprocess.run(["git", "-C", str(staged), "branch", "side", publisher.BASELINE_COMMIT], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", "side"], check=True)
    (staged / "side.txt").write_text("side\n", encoding="utf-8")
    side = commit(staged, ("side.txt",), "test: add side parent")
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", "main"], check=True)
    subprocess.run(["git", "-C", str(staged), "merge", "--no-ff", "-q", "-m", "test: merge parent", side], check=True)
    merge = git(staged, "rev-parse", "HEAD")
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.render_authority(staged, merge)
    assert error.value.code == "TOOLING_CANDIDATE_PARENT_MISMATCH"
    assert error.value.state == "BLOCKED"


def test_malformed_lock_utf8_path_and_installed_metadata_have_stable_results(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    staged, candidate = stage_candidate(
        tmp_path,
        lambda root: (root / "uv.lock").write_text('version = 1\npackage = "invalid"\n', encoding="utf-8"),
    )
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.package_rows(staged, candidate)
    assert error.value.code == "TOOLING_LOCK_INVALID"

    original_run_git = publisher.run_git

    def invalid_path(_root: Path, *arguments: str, **_kwargs):
        if arguments[:3] == ("diff", "--name-only", "-z"):
            return subprocess.CompletedProcess(arguments, 0, stdout=b"\xff\0", stderr=b"")
        return original_run_git(_root, *arguments, **_kwargs)

    monkeypatch.setattr(publisher, "run_git", invalid_path)
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.changed_paths(staged, publisher.BASELINE_COMMIT, candidate)
    assert error.value.code == "TOOLING_PATH_ENCODING_INVALID"
    monkeypatch.setattr(publisher, "run_git", original_run_git)

    def unavailable(_name: str) -> str:
        raise publisher.importlib.metadata.PackageNotFoundError("fixture")

    monkeypatch.setattr(publisher.importlib.metadata, "version", unavailable)
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_installed_versions()
    assert error.value.code == "TOOLING_INSTALLED_METADATA_UNAVAILABLE"
    assert error.value.state == "BLOCKED"


def test_failure_result_is_non_vacuous() -> None:
    error = publisher.ToolingAuthorityError("TOOLING_HISTORY_UNAVAILABLE", "fixture", "BLOCKED")
    result = publisher.failure_document(ROOT, error)

    assert result["result"] == "BLOCKED"
    assert result["assertionLedger"]


def test_closed_schema_rejects_authority_effect_and_anti_vacuity_faults(tmp_path: Path) -> None:
    staged, candidate = stage_candidate(tmp_path)
    document = publisher.render_authority(staged, candidate)

    document["authorityEffect"]["implementationHold"] = "LIFTED"
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_schema(staged, candidate, document)
    assert error.value.code == "TOOLING_AUTHORITY_SCHEMA_INVALID"

    document = publisher.render_authority(staged, candidate)
    document["assertionLedger"] = []
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_schema(staged, candidate, document)
    assert error.value.code == "TOOLING_AUTHORITY_SCHEMA_INVALID"

    document = publisher.render_authority(staged, candidate)
    document["candidateFiles"][0]["path"] = "unexpected.txt"
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_schema(staged, candidate, document)
    assert error.value.code == "TOOLING_AUTHORITY_SCHEMA_INVALID"

    document = publisher.render_authority(staged, candidate)
    document["immutableAuthorities"][0]["sha256"] = "0" * 64
    with pytest.raises(publisher.ToolingAuthorityError) as error:
        publisher.validate_schema(staged, candidate, document)
    assert error.value.code == "TOOLING_AUTHORITY_SCHEMA_INVALID"
