"""Transaction and fault-injection tests for V18 package authority."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
from typing import Callable

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_v18_package_environment_authority.py"
SPEC = importlib.util.spec_from_file_location("publish_v18_package_environment_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)


def git(path: Path, *arguments: str) -> str:
    """Run Git in a fixture repository and return trimmed output."""

    return subprocess.check_output(["git", "-C", str(path), *arguments], text=True).strip()


def commit(path: Path, message: str) -> str:
    """Commit the fixture index and return the exact commit identity."""

    subprocess.run(["git", "-C", str(path), "commit", "-q", "-m", message], check=True)
    return git(path, "rev-parse", "HEAD")


def stage_candidate(
    tmp_path: Path,
    mutate: Callable[[Path, Path], None] | None = None,
    extra_paths: tuple[str, ...] = (),
) -> tuple[Path, str, str]:
    """Create one exact candidate with an initialized synthetic Builds gitlink."""

    staged = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(staged)], check=True)
    subprocess.run(["git", "-C", str(staged), "checkout", "-q", publisher.BASELINE_COMMIT], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.name", "V18 fixture"], check=True)
    subprocess.run(["git", "-C", str(staged), "config", "user.email", "v18@example.invalid"], check=True)

    for relative in publisher.CANDIDATE_FILE_PATHS:
        source = ROOT / relative
        target = staged / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)

    builds = staged / publisher.BUILDS_PATH
    builds.mkdir(parents=True, exist_ok=True)
    subprocess.run(["git", "-C", str(builds), "init", "-q"], check=True)
    subprocess.run(["git", "-C", str(builds), "config", "user.name", "V18 Builds fixture"], check=True)
    subprocess.run(["git", "-C", str(builds), "config", "user.email", "v18-builds@example.invalid"], check=True)
    catalog = builds / publisher.BUILDS_CATALOG_PATH
    catalog.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(ROOT / publisher.BUILDS_PATH / publisher.BUILDS_CATALOG_PATH, catalog)

    if mutate is not None:
        mutate(staged, builds)

    subprocess.run(["git", "-C", str(builds), "add", "--", publisher.BUILDS_CATALOG_PATH], check=True)
    builds_commit = commit(builds, "build(deps): stage V18 Builds fixture")
    subprocess.run(
        ["git", "-C", str(staged), "add", "--", *publisher.CANDIDATE_FILE_PATHS, *extra_paths],
        check=True,
    )
    subprocess.run(
        [
            "git",
            "-C",
            str(staged),
            "update-index",
            "--add",
            "--cacheinfo",
            f"160000,{builds_commit},{publisher.BUILDS_PATH}",
        ],
        check=True,
    )
    candidate = commit(staged, "build(deps): stage V18 fixture")
    return staged, candidate, builds_commit


def stage_transaction(tmp_path: Path) -> tuple[Path, str, str]:
    """Create the exact C1/C2 fixture transaction."""

    staged, candidate, _ = stage_candidate(tmp_path)
    document = publisher.publish(staged, candidate_revision=candidate, check=False)
    assert document["candidateCommit"] == candidate
    subprocess.run(["git", "-C", str(staged), "add", "--", publisher.AUTHORITY_PATH], check=True)
    publication = commit(staged, "build(planning): bind V18 fixture")
    return staged, candidate, publication


def test_v18_binds_exact_graph_toolchains_gitlink_predecessors_and_ledger(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)

    document = publisher.publish(
        staged,
        candidate_revision=publication,
        publication_revision=publication,
        check=True,
    )

    assert document["candidateCommit"] == candidate
    assert document["publication"]["c1Paths"] == list(publisher.C1_PATHS)
    assert document["publication"]["changedGitlinks"] == [publisher.BUILDS_PATH]
    assert document["gitlinks"][0]["mode"] == "160000"
    assert document["gitlinks"][0]["candidateCommit"] != document["gitlinks"][0]["baselineCommit"]
    assert document["directPins"]["unchangedFromBaseline"] is True
    assert document["pythonEnvironment"]["packages"] == [
        {"name": name, "version": version} for name, version in publisher.PYTHON_PACKAGES
    ]
    assert document["toolchain"] == {
        "dotnetSdk": "10.0.401",
        "uv": "0.12.13",
        "aspire": "13.5.3",
        "communityToolkitAspireDapr": "13.5.1-beta.751",
        "microsoftNetTestSdk": "18.10.0",
    }
    assert len(document["immutableAuthorities"]) == 4
    assert document["assertionLedger"]
    assert all(row["state"] == "PASS" for row in document["assertionLedger"])


def test_descendant_and_unrelated_dirty_state_use_committed_publication(tmp_path: Path) -> None:
    staged, candidate, publication = stage_transaction(tmp_path)
    (staged / "README.md").write_text("descendant\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(staged), "add", "--", "README.md"], check=True)
    descendant = commit(staged, "test: add V18 descendant")
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
        lambda document: document["sourceBindings"][0].__setitem__("sha256", "0" * 63),
        lambda document: document["toolchain"].__setitem__("communityToolkitAspireDapr", "13.5.0"),
        lambda document: document["authorityEffect"].__setitem__("story71CandidateBindingResolved", True),
    ),
)
def test_closed_schema_rejects_unknown_weak_hash_channel_and_hold_faults(tmp_path: Path, mutation) -> None:
    staged, candidate, _ = stage_candidate(tmp_path)
    document = publisher.render_authority(staged, candidate)
    mutation(document)

    with pytest.raises(publisher.PackageAuthorityError) as error:
        publisher.validate_schema(staged, candidate, document)

    assert error.value.code == "PACKAGE_AUTHORITY_SCHEMA_INVALID"


def test_current_direct_manifests_remain_byte_identical_while_locks_refresh(tmp_path: Path) -> None:
    staged, candidate, _ = stage_candidate(tmp_path)

    assert publisher.candidate_blob(staged, candidate, "package.json") == publisher.candidate_blob(
        staged, publisher.BASELINE_COMMIT, "package.json"
    )
    assert publisher.candidate_blob(staged, candidate, "pyproject.toml") == publisher.candidate_blob(
        staged, publisher.BASELINE_COMMIT, "pyproject.toml"
    )
    assert publisher.candidate_blob(staged, candidate, "package-lock.json") != publisher.candidate_blob(
        staged, publisher.BASELINE_COMMIT, "package-lock.json"
    )
    assert publisher.candidate_blob(staged, candidate, "uv.lock") != publisher.candidate_blob(
        staged, publisher.BASELINE_COMMIT, "uv.lock"
    )


@pytest.mark.parametrize(
    ("old", "new", "code"),
    (
        ("2.21.0", "2.20.0", "PACKAGE_PYTHON_GRAPH_DRIFT"),
        ("10.0.401", "10.0.402", "PACKAGE_DOTNET_SDK_DRIFT"),
        ("uv==0.12.13", "uv==0.11.16", "PACKAGE_UV_CLIENT_DRIFT"),
    ),
)
def test_python_sdk_and_uv_regressions_fail_closed(tmp_path: Path, old: str, new: str, code: str) -> None:
    def mutate(staged: Path, _builds: Path) -> None:
        targets = {
            "2.21.0": staged / "uv.lock",
            "10.0.401": staged / "global.json",
            "uv==0.12.13": staged / ".github/workflows/planning-authority-preflight.yml",
        }
        target = targets[old]
        target.write_text(target.read_text(encoding="utf-8").replace(old, new), encoding="utf-8")

    staged, candidate, _ = stage_candidate(tmp_path, mutate)
    with pytest.raises(publisher.PackageAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == code


def test_prerelease_catalog_and_exact_builds_gitlink_fail_closed(tmp_path: Path) -> None:
    def mutate(_staged: Path, builds: Path) -> None:
        catalog = builds / publisher.BUILDS_CATALOG_PATH
        content = catalog.read_text(encoding="utf-8-sig")
        catalog.write_text(content.replace("13.5.1-beta.751", "13.5.0-preview.1.260825-0345"), encoding="utf-8")

    staged, candidate, _ = stage_candidate(tmp_path, mutate)
    with pytest.raises(publisher.PackageAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == "PACKAGE_BUILDS_VERSION_DRIFT"


def test_c1_c2_scope_and_descendant_authority_drift_fail_closed(tmp_path: Path) -> None:
    def add_unexpected(staged: Path, _builds: Path) -> None:
        path = staged / "unexpected-v18.txt"
        path.write_text("unexpected\n", encoding="utf-8")

    staged, candidate, _ = stage_candidate(tmp_path / "scope", add_unexpected, ("unexpected-v18.txt",))
    with pytest.raises(publisher.PackageAuthorityError) as error:
        publisher.render_authority(staged, candidate)
    assert error.value.code == "PACKAGE_C1_SCOPE_DRIFT"

    staged, _, publication = stage_transaction(tmp_path / "drift")
    authority = staged / publisher.AUTHORITY_PATH
    authority.write_text(authority.read_text(encoding="utf-8").replace('"result": "PASS"', '"result": "FAIL"'), encoding="utf-8")
    subprocess.run(["git", "-C", str(staged), "add", "--", publisher.AUTHORITY_PATH], check=True)
    descendant = commit(staged, "test: drift V18 descendant")
    with pytest.raises(publisher.PackageAuthorityError) as error:
        publisher.publish(staged, candidate_revision=descendant, publication_revision=publication, check=True)
    assert error.value.code == "PACKAGE_AUTHORITY_DESCENDANT_DRIFT"


def test_unavailable_history_is_blocked_with_nonempty_result(tmp_path: Path) -> None:
    with pytest.raises(publisher.PackageAuthorityError) as error:
        publisher.resolve_commit(tmp_path, "HEAD", "PACKAGE_CANDIDATE_UNAVAILABLE")

    assert error.value.code == "PACKAGE_CANDIDATE_UNAVAILABLE"
    assert error.value.state == "BLOCKED"
    result = publisher.failure_document(tmp_path, error.value)
    assert result["result"] == "BLOCKED"
    assert result["assertionLedger"]
