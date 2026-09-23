"""Fault-injection tests for the V14 evidence-boundary gate."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
from typing import Any, Callable

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/verify_evidence_boundary.py"
SPEC = importlib.util.spec_from_file_location("verify_evidence_boundary", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)

PUBLISHER_MODULE_PATH = ROOT / verifier.V23_PUBLISHER_PATH
PUBLISHER_SPEC = importlib.util.spec_from_file_location("v23_publisher_for_verifier_tests", PUBLISHER_MODULE_PATH)
assert PUBLISHER_SPEC is not None and PUBLISHER_SPEC.loader is not None
publisher = importlib.util.module_from_spec(PUBLISHER_SPEC)
PUBLISHER_SPEC.loader.exec_module(publisher)


def v24_correction_repository(tmp_path: Path) -> tuple[Path, str]:
    """Build the production-shaped V24 correction through the publisher fixture."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v24_evidence_correction_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    return fixtures.correction_repository(tmp_path)


def v24_fault_repository(tmp_path: Path, fault: str) -> tuple[Path, str]:
    """Build one independently faulted V24 production-route fixture."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v24_evidence_fault_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    return fixtures.v24_fault_repository(tmp_path, fault)


def v24_full_history_repository(tmp_path: Path, scenario: str) -> tuple[Path, str]:
    """Build a V24 history fixture that path-limited Git traversal can prune."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v24_evidence_history_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    return fixtures.v24_full_history_repository(tmp_path, scenario)


def init_repository(path: Path) -> None:
    subprocess.run(["git", "init", "-q", "-b", "main", str(path)], check=True)
    subprocess.run(["git", "-C", str(path), "config", "user.name", "Verifier"], check=True)
    subprocess.run(
        ["git", "-C", str(path), "config", "user.email", "verifier@example.invalid"],
        check=True,
    )


def commit_all(path: Path, message: str) -> str:
    subprocess.run(["git", "-C", str(path), "add", "."], check=True)
    subprocess.run(["git", "-C", str(path), "commit", "-q", "-m", message], check=True)
    return subprocess.check_output(["git", "-C", str(path), "rev-parse", "HEAD"], text=True).strip()


def test_active_route_inventory_is_exact_mirrored_and_pre_transition() -> None:
    """All twelve active route files carry one real gate before lifecycle writes."""

    ledger = verifier.validate_active_routes(ROOT)

    assert len(verifier.ACTIVE_ROUTE_PATHS) == 10
    assert len(ledger) == 10
    assert all(row["state"] == "PASS" for row in ledger)


@pytest.mark.parametrize(
    ("mutation", "expected_code"),
    (
        (lambda content: content.replace(verifier.GATE_MARKER.encode(), b"removed gate", 1), "EVIDENCE_GATE_NOT_USED"),
        (
            lambda content: content.replace(
                verifier.GATE_MARKER.encode(),
                b"verify_submodule_promotion.py verify_evidence_boundary.py PASS FAIL BLOCKED not-applicable",
                1,
            ),
            "EVIDENCE_GATE_NOT_USED",
        ),
        (lambda content: content.replace(b"verify_evidence_boundary.py", b"verify_decoy.py", 1), "EVIDENCE_GATE_DECOY"),
    ),
)
def test_route_gate_faults_fail_without_touching_files(mutation, expected_code: str) -> None:
    """Removal and decoy mutations fail while the real route bytes stay unchanged."""

    path = verifier.ACTIVE_ROUTE_PATHS[0]
    before = (ROOT / path).read_bytes()

    def reader(root: Path, relative_path: str) -> bytes:
        content = (root / relative_path).read_bytes()
        return mutation(content) if relative_path == path else content

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_active_routes(ROOT, reader)

    assert error.value.code == expected_code
    assert (ROOT / path).read_bytes() == before


def test_displaced_gate_and_cross_tree_parity_faults_fail() -> None:
    """A post-transition marker or one-tree drift cannot satisfy gate placement."""

    logical = verifier.LOGICAL_ROUTE_PATHS[0]
    agents_path = f".agents/skills/{logical}"
    lifecycle = verifier.LIFECYCLE_TOKENS[logical].encode()

    def displaced(root: Path, relative_path: str) -> bytes:
        content = (root / relative_path).read_bytes()
        if relative_path not in (agents_path, f".claude/skills/{logical}"):
            return content
        marker = verifier.GATE_MARKER.encode()
        without = content.replace(marker, b"displaced", 1)
        return without.replace(lifecycle, lifecycle + b"\n" + marker, 1)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_active_routes(ROOT, displaced)
    assert error.value.code == "EVIDENCE_GATE_DISPLACED"

    def parity(root: Path, relative_path: str) -> bytes:
        content = (root / relative_path).read_bytes()
        return content + b"\nparity drift\n" if relative_path == agents_path else content

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_active_routes(ROOT, parity)
    assert error.value.code == "EVIDENCE_WORKFLOW_PARITY_DRIFT"


def test_context_frontmatter_is_closed_and_wrong_identity_fails(tmp_path: Path) -> None:
    """Malformed and wrong-identity generated contexts fail closed."""

    target = tmp_path / "_bmad-output/implementation-artifacts/epic-6-context.md"
    target.parent.mkdir(parents=True)
    target.write_text("# missing frontmatter\n", encoding="utf-8")
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context(tmp_path)
    assert error.value.code == "EVIDENCE_CONTEXT_INVALID"

    valid_frontmatter_but_condensed = (
        "---\n"
        "overlay_version: epic-6-authority-2026-08-01-v8\n"
        "architecture_version: conversations-architecture-2026-08-01-v8\n"
        "---\n"
        "# Epic 6 Context: condensed\n"
    )
    target.write_text(valid_frontmatter_but_condensed, encoding="utf-8")
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context(tmp_path)
    assert error.value.code == "EVIDENCE_CONTEXT_INVALID"

    duplicate_identity = valid_frontmatter_but_condensed.replace(
        "architecture_version:",
        "overlay_version: epic-6-authority-2026-08-01-v8\narchitecture_version:",
    )
    target.write_text(duplicate_identity, encoding="utf-8")
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context(tmp_path)
    assert error.value.code == "EVIDENCE_CONTEXT_INVALID"

    target.write_text(
        "---\noverlay_version: wrong\narchitecture_version: conversations-architecture-2026-08-01-v8\n---\n# Epic 6\n",
        encoding="utf-8",
    )
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context(tmp_path)
    assert error.value.code == "EVIDENCE_CONTEXT_INVALID"


def test_context_workflows_are_exact_mirrors_and_fail_closed() -> None:
    ledger = verifier.validate_context_workflows(ROOT)

    assert len(ledger) == 4
    assert all(row["state"] == "PASS" for row in ledger)


SHARED_CONTEXT_TOKENS = (b"overlay_version", b"architecture_version", b"frontmatter")
COMPILE_CONTEXT_TOKENS = (
    "`### 6.1 ` through `### 6.12 `".encode(),
    b"write nothing",
    b"Historical Epic 6 v8 exception",
)
STEP_01_CONTEXT_TOKENS = (
    "`### 6.1 ` through `### 6.12 `".encode(),
    b"filesystem mtime alone",
    b"historical authority",
    b"heading-only context",
)
CONTEXT_WORKFLOW_TOKEN_CASES = tuple(
    (logical, token)
    for logical in verifier.CONTEXT_WORKFLOW_PATHS
    for token in SHARED_CONTEXT_TOKENS
    + (COMPILE_CONTEXT_TOKENS if "compile-epic-context" in logical else STEP_01_CONTEXT_TOKENS)
)


@pytest.mark.parametrize(("logical", "token"), CONTEXT_WORKFLOW_TOKEN_CASES)
def test_context_workflow_token_faults_fail_without_touching_files(
    logical: str, token: bytes
) -> None:
    """Stripping any required identity or semantic token turns the guard red.

    The BMAD 6.12.0 upgrade removed exactly these sentences from all four context workflows, so
    every token is proven load-bearing in every declared path rather than assumed from one. The
    mutation is injected through the reader; the on-disk bytes are asserted unchanged afterwards,
    even though validation fails.
    """

    agents_path = f".agents/skills/{logical}"
    claude_path = f".claude/skills/{logical}"
    before = {path: (ROOT / path).read_bytes() for path in (agents_path, claude_path)}
    assert token in before[agents_path], (logical, token)

    def stripped(root: Path, relative_path: str) -> bytes:
        content = (root / relative_path).read_bytes()
        if relative_path in (agents_path, claude_path):
            return content.replace(token, b"")
        return content

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context_workflows(ROOT, stripped)

    assert error.value.code == "EVIDENCE_CONTEXT_WORKFLOW_INVALID"
    assert {path: (ROOT / path).read_bytes() for path in before} == before


def test_context_workflow_parity_drift_fails_without_touching_files() -> None:
    """One tree drifting from the other is parity drift, reported per logical path."""

    logical = verifier.CONTEXT_WORKFLOW_PATHS[0]
    agents_path = f".agents/skills/{logical}"
    before = (ROOT / agents_path).read_bytes()

    def drifted(root: Path, relative_path: str) -> bytes:
        content = (root / relative_path).read_bytes()
        return content + b"\nparity drift\n" if relative_path == agents_path else content

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context_workflows(ROOT, drifted)

    assert error.value.code == "EVIDENCE_WORKFLOW_PARITY_DRIFT"
    assert error.value.message == logical
    assert (ROOT / agents_path).read_bytes() == before


def test_context_workflow_absence_fails_closed() -> None:
    """A declared context workflow that is not installed blocks; it is never a silent pass."""

    logical = verifier.CONTEXT_WORKFLOW_PATHS[0]
    agents_path = f".agents/skills/{logical}"
    before = (ROOT / agents_path).read_bytes()

    def absent(root: Path, relative_path: str) -> bytes:
        if relative_path == agents_path:
            raise FileNotFoundError(relative_path)
        return (root / relative_path).read_bytes()

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_context_workflows(ROOT, absent)

    assert error.value.code == "EVIDENCE_CONTEXT_WORKFLOW_INVALID"
    assert logical in error.value.message
    assert (ROOT / agents_path).read_bytes() == before


def test_absent_declared_route_fails_and_is_never_not_applicable() -> None:
    """An upstream route deletion must name the path, not disappear into a pass."""

    path = verifier.ACTIVE_ROUTE_PATHS[0]
    before = (ROOT / path).read_bytes()

    def absent(root: Path, relative_path: str) -> bytes:
        if relative_path == path:
            return verifier.read_route(root, "does/not/exist.md")
        return verifier.read_route(root, relative_path)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_active_routes(ROOT, absent)

    assert error.value.code == "EVIDENCE_GATE_NOT_USED"
    assert error.value.state == "FAIL"
    assert (ROOT / path).read_bytes() == before


def test_retired_dev_story_route_is_recorded_as_deleted_not_inferred() -> None:
    """`bmad-dev-story` was deleted upstream; the inventory must not silently carry it."""

    assert len(verifier.ACTIVE_ROUTE_PATHS) == 10
    assert not any("bmad-dev-story" in path for path in verifier.ACTIVE_ROUTE_PATHS)
    assert not any("bmad-dev-story" in logical for logical in verifier.LIFECYCLE_TOKENS)
    assert set(verifier.LIFECYCLE_TOKENS) == set(verifier.LOGICAL_ROUTE_PATHS)
    for tree in (".agents/skills", ".claude/skills"):
        assert not (ROOT / tree / "bmad-dev-story").exists()


def test_wrong_signature_or_current_tree_fallback_is_rejected() -> None:
    path = ROOT / "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs"
    content = path.read_bytes()
    assert verifier.validate_csharp_signature_guard(ROOT, content)["state"] == "PASS"

    vacuous = content.replace(b"Trim().Length == 0", b"Trim().Length >= 0", 1)
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_csharp_signature_guard(ROOT, vacuous)
    assert error.value.code == "EVIDENCE_SIGNATURE_GUARD_INVALID"

    fallback = content.replace(
        b"current checkout bytes are not historical evidence",
        b"fall back to current checkout",
        1,
    )
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_csharp_signature_guard(ROOT, fallback)
    assert error.value.code == "EVIDENCE_SIGNATURE_GUARD_INVALID"


def test_failure_results_keep_a_nonempty_ledger(tmp_path: Path) -> None:
    error = verifier.BoundaryError("EVIDENCE_HISTORY_UNAVAILABLE", "fixture", "BLOCKED")
    document = verifier.failure_document(tmp_path, error)

    assert document["result"] == "BLOCKED"
    assert document["assertionLedger"]


def test_unavailable_history_and_path_escape_are_blocked(tmp_path: Path) -> None:
    """Unavailable Git evidence and escaping paths never become not-applicable or PASS."""

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.repository_root(tmp_path)
    assert error.value.code == "EVIDENCE_HISTORY_UNAVAILABLE"
    assert error.value.state == "BLOCKED"

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.safe_relative_path("../escape")
    assert error.value.code == "EVIDENCE_PATH_ESCAPE"


def test_not_applicable_still_has_a_nonempty_assertion_ledger(tmp_path: Path) -> None:
    """A real Git change outside evidence scope records exact paths and cannot pass vacuously."""

    subprocess.run(["git", "init", "-q", "-b", "main", str(tmp_path)], check=True)
    subprocess.run(["git", "-C", str(tmp_path), "config", "user.name", "Verifier"], check=True)
    subprocess.run(["git", "-C", str(tmp_path), "config", "user.email", "verifier@example.invalid"], check=True)
    (tmp_path / "README.md").write_text("one\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(tmp_path), "add", "README.md"], check=True)
    subprocess.run(["git", "-C", str(tmp_path), "commit", "-q", "-m", "test: baseline"], check=True)
    baseline = subprocess.check_output(["git", "-C", str(tmp_path), "rev-parse", "HEAD"], text=True).strip()
    (tmp_path / "README.md").write_text("two\n", encoding="utf-8")

    document = verifier.verify(tmp_path, baseline, "HEAD")

    assert document["result"] == "not-applicable"
    assert document["changedPaths"] == []
    assert document["worktreePaths"] == ["README.md"]
    assert document["assertionLedger"]
    assert all(row["state"] == "PASS" for row in document["assertionLedger"])


def test_candidate_bound_publication_scope_is_exact_and_rejects_gitlinks(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    baseline = commit_all(tmp_path, "test: baseline")
    path = tmp_path / verifier.PUBLICATION_SCOPE_PATH
    path.parent.mkdir(parents=True)
    expected = [verifier.PUBLICATION_SCOPE_PATH, "_bmad/scripts/example.py"]
    example = tmp_path / expected[1]
    example.parent.mkdir(parents=True)
    example.write_text("example\n", encoding="utf-8")
    path.write_text(
        json.dumps(
            {
                "schemaVersion": "hexalith.conversations.v14-planning-publication-scope.v1",
                "baseline": baseline,
                "expectedChangedPaths": expected,
                "requireNoGitlinkChanges": True,
            }
        ),
        encoding="utf-8",
    )
    candidate = commit_all(tmp_path, "test: candidate")
    row = verifier.validate_publication_scope(tmp_path, baseline, candidate, expected, {"paths": []})
    assert row == {
        "id": "SCOPE-01",
        "subject": "candidate-bound-publication-allowlist",
        "state": "PASS",
        "count": 2,
    }

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_publication_scope(
            tmp_path,
            baseline,
            candidate,
            [*expected, "src/product.cs"],
            {"paths": []},
        )
    assert error.value.code == "EVIDENCE_PUBLICATION_SCOPE_DRIFT"

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_publication_scope(
            tmp_path,
            baseline,
            candidate,
            expected,
            {"paths": ["references/Hexalith.Tenants"]},
        )
    assert error.value.code == "EVIDENCE_GITLINK_SET_DRIFT"


def test_publication_scope_rejects_non_object_candidate_manifest(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    baseline = commit_all(tmp_path, "test: baseline")
    path = tmp_path / verifier.PUBLICATION_SCOPE_PATH
    path.parent.mkdir(parents=True)
    path.write_text("[]\n", encoding="utf-8")
    candidate = commit_all(tmp_path, "test: candidate")

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_publication_scope(
            tmp_path,
            baseline,
            candidate,
            [verifier.PUBLICATION_SCOPE_PATH],
            {"paths": []},
        )

    assert error.value.code == "EVIDENCE_SCOPE_MANIFEST_INVALID"


def test_publication_scope_uses_candidate_manifest_not_dirty_worktree_bytes(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    baseline = commit_all(tmp_path, "test: baseline")
    expected = [verifier.PUBLICATION_SCOPE_PATH]
    path = tmp_path / verifier.PUBLICATION_SCOPE_PATH
    path.parent.mkdir(parents=True)
    path.write_text(
        json.dumps(
            {
                "schemaVersion": "hexalith.conversations.v14-planning-publication-scope.v1",
                "baseline": baseline,
                "expectedChangedPaths": expected,
                "requireNoGitlinkChanges": True,
            }
        ),
        encoding="utf-8",
    )
    candidate = commit_all(tmp_path, "test: candidate")
    path.write_text("[]\n", encoding="utf-8")

    row = verifier.validate_publication_scope(
        tmp_path,
        baseline,
        candidate,
        expected,
        {"paths": []},
    )

    assert row["state"] == "PASS"
    assert row["count"] == 1


def test_raw_gitlink_diff_is_rename_safe(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    commit_all(tmp_path, "test: repository baseline")
    object_id = subprocess.check_output(
        ["git", "-C", str(tmp_path), "rev-parse", "HEAD"],
        text=True,
    ).strip()
    subprocess.run(
        [
            "git",
            "-C",
            str(tmp_path),
            "update-index",
            "--add",
            "--cacheinfo",
            f"160000,{object_id},references/Old",
        ],
        check=True,
    )
    subprocess.run(["git", "-C", str(tmp_path), "commit", "-q", "-m", "test: add gitlink"], check=True)
    baseline = subprocess.check_output(
        ["git", "-C", str(tmp_path), "rev-parse", "HEAD"],
        text=True,
    ).strip()
    subprocess.run(
        ["git", "-C", str(tmp_path), "update-index", "--force-remove", "references/Old"],
        check=True,
    )
    subprocess.run(
        [
            "git",
            "-C",
            str(tmp_path),
            "update-index",
            "--add",
            "--cacheinfo",
            f"160000,{object_id},references/New",
        ],
        check=True,
    )
    subprocess.run(["git", "-C", str(tmp_path), "commit", "-q", "-m", "test: rename gitlink"], check=True)
    candidate = subprocess.check_output(
        ["git", "-C", str(tmp_path), "rev-parse", "HEAD"],
        text=True,
    ).strip()

    row = verifier.validate_gitlinks(tmp_path, baseline, candidate)

    assert row["paths"] == ["references/New", "references/Old"]


def test_verify_applies_candidate_scope_and_rejects_unlisted_path(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    baseline = commit_all(tmp_path, "test: baseline")
    path = tmp_path / verifier.PUBLICATION_SCOPE_PATH
    path.parent.mkdir(parents=True)
    path.write_text(
        json.dumps(
            {
                "schemaVersion": "hexalith.conversations.v14-planning-publication-scope.v1",
                "baseline": baseline,
                "expectedChangedPaths": [verifier.PUBLICATION_SCOPE_PATH],
                "requireNoGitlinkChanges": True,
            }
        ),
        encoding="utf-8",
    )
    candidate = commit_all(tmp_path, "test: scoped candidate")
    real_candidate_blob = verifier.candidate_blob

    def fixture_candidate_blob(root: Path, commit: str, relative: str) -> bytes:
        if relative in {
            "_bmad-output/implementation-artifacts/epic-6-context.md",
            "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
        }:
            return (ROOT / relative).read_bytes()
        return real_candidate_blob(root, commit, relative)

    monkeypatch.setattr(verifier, "candidate_blob", fixture_candidate_blob)
    monkeypatch.setattr(verifier, "validate_active_routes", lambda _root, _reader=None: [])
    monkeypatch.setattr(
        verifier,
        "validate_context",
        lambda _root, _content=None: verifier.assertion("CONTEXT-01", "fixture", "PASS"),
    )
    monkeypatch.setattr(verifier, "validate_context_workflows", lambda _root, _reader=None: [])
    monkeypatch.setattr(
        verifier,
        "validate_csharp_signature_guard",
        lambda _root, _content=None: verifier.assertion("SIGNATURE-01", "fixture", "PASS"),
    )
    monkeypatch.setattr(
        verifier,
        "run_publication_check",
        lambda _root, **_kwargs: verifier.assertion("PUBLICATION-01", "fixture", "PASS"),
    )

    document = verifier.verify(tmp_path, baseline, candidate)

    scope = next(row for row in document["assertionLedger"] if row["id"] == "SCOPE-01")
    assert document["result"] == "PASS"
    assert scope == {
        "id": "SCOPE-01",
        "subject": "candidate-bound-publication-allowlist",
        "state": "PASS",
        "count": 1,
    }

    unexpected = tmp_path / "_bmad/scripts/unlisted.py"
    unexpected.parent.mkdir(parents=True)
    unexpected.write_text("unexpected\n", encoding="utf-8")
    drifted_candidate = commit_all(tmp_path, "test: unlisted path")
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.verify(tmp_path, baseline, drifted_candidate)
    assert error.value.code == "EVIDENCE_PUBLICATION_SCOPE_DRIFT"


def test_v15_two_commit_scope_is_exact_candidate_bound_and_zero_gitlink(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    baseline = commit_all(tmp_path, "test: baseline")
    c1_paths = verifier.V15_C1_PATHS
    for relative in c1_paths:
        path = tmp_path / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(relative + "\n", encoding="utf-8")
    c1 = commit_all(tmp_path, "test: V15 C1")
    combined = tuple(sorted((*c1_paths, verifier.V15_AUTHORITY_PATH)))
    authority_path = tmp_path / verifier.V15_AUTHORITY_PATH
    authority_path.parent.mkdir(parents=True, exist_ok=True)
    authority_path.write_text(
        json.dumps(
            {
                "schemaVersion": "hexalith.conversations.v15-planning-tooling-environment-authority.v1",
                "baselineCommit": baseline,
                "candidateCommit": c1,
                "publication": {
                    "c1Paths": list(sorted(c1_paths)),
                    "c2Path": verifier.V15_AUTHORITY_PATH,
                    "combinedPaths": list(combined),
                    "changedGitlinks": [],
                },
            }
        )
        + "\n",
        encoding="utf-8",
    )
    c2 = commit_all(tmp_path, "test: V15 C2")
    row = verifier.validate_authority_scope(
        tmp_path,
        c2,
        version="v15",
        authority_path=verifier.V15_AUTHORITY_PATH,
        schema_version="hexalith.conversations.v15-planning-tooling-environment-authority.v1",
        baseline=baseline,
        expected_c1_paths=c1_paths,
    )

    assert row == {
        "id": "V15-SCOPE-01",
        "subject": "v15-planning-tooling-boundary",
        "state": "PASS",
        "applied": True,
        "publication": c2,
        "count": 11,
    }


def test_v15_scope_rejects_predecessor_scope_and_gitlink_faults(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    baseline = commit_all(tmp_path, "test: baseline")
    c1_path = "_bmad/scripts/publish_v15_planning_tooling_environment.py"
    target = tmp_path / c1_path
    target.parent.mkdir(parents=True)
    target.write_text("candidate\n", encoding="utf-8")
    c1 = commit_all(tmp_path, "test: candidate")
    authority_path = tmp_path / verifier.V15_AUTHORITY_PATH
    authority_path.parent.mkdir(parents=True, exist_ok=True)
    authority = {
        "schemaVersion": "hexalith.conversations.v15-planning-tooling-environment-authority.v1",
        "baselineCommit": baseline,
        "candidateCommit": c1,
        "publication": {
            "c1Paths": [c1_path],
            "c2Path": verifier.V15_AUTHORITY_PATH,
            "combinedPaths": [verifier.V15_AUTHORITY_PATH, c1_path],
            "changedGitlinks": [],
        },
    }
    authority_path.write_text(json.dumps(authority) + "\n", encoding="utf-8")
    c2 = commit_all(tmp_path, "test: authority")
    authority["candidateCommit"] = baseline
    authority_path.write_text(json.dumps(authority) + "\n", encoding="utf-8")
    wrong_predecessor = commit_all(tmp_path, "test: wrong predecessor binding")
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_authority_scope(
            tmp_path,
            wrong_predecessor,
            version="v15",
            authority_path=verifier.V15_AUTHORITY_PATH,
            schema_version="hexalith.conversations.v15-planning-tooling-environment-authority.v1",
            baseline=baseline,
            expected_c1_paths=(c1_path,),
        )
    assert error.value.code == "EVIDENCE_V15_AUTHORITY_DESCENDANT_DRIFT"

    authority_path.write_text(json.dumps(authority) + "\n", encoding="utf-8")
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_authority_scope(
            tmp_path,
            wrong_predecessor,
            version="v15",
            authority_path=verifier.V15_AUTHORITY_PATH,
            schema_version="hexalith.conversations.v15-planning-tooling-environment-authority.v1",
            baseline=baseline,
            expected_c1_paths=(c1_path, "unexpected.txt"),
        )
    assert error.value.code == "EVIDENCE_V15_AUTHORITY_INVALID"


def test_child_blocked_result_is_preserved() -> None:
    result = subprocess.CompletedProcess(
        ["publisher"],
        2,
        stdout=json.dumps(
            {
                "result": "BLOCKED",
                "blockers": [{"code": "LIFECYCLE_HISTORY_UNAVAILABLE", "detail": "fixture"}],
            }
        ),
        stderr="",
    )

    error = verifier.child_failure(result)

    assert error.code == "LIFECYCLE_HISTORY_UNAVAILABLE"
    assert error.state == "BLOCKED"


def test_authority_route_uses_candidate_tree_not_dirty_worktree(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    candidate = commit_all(tmp_path, "test: baseline")
    dirty = tmp_path / verifier.V16_AUTHORITY_PATH
    dirty.parent.mkdir(parents=True)
    dirty.write_text("{}\n", encoding="utf-8")

    assert verifier.authority_route(tmp_path, candidate) == "legacy"
    assert verifier.V16_AUTHORITY_PATH in verifier.worktree_paths(tmp_path)


def test_v17_route_wins_over_v16_and_is_chosen_from_the_candidate_tree(tmp_path: Path) -> None:
    init_repository(tmp_path)
    (tmp_path / "README.md").write_text("baseline\n", encoding="utf-8")
    commit_all(tmp_path, "test: baseline")
    for relative in (verifier.V16_AUTHORITY_PATH, verifier.V17_AUTHORITY_PATH):
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text("{}\n", encoding="utf-8")
    candidate = commit_all(tmp_path, "test: publish V17 over V16")

    assert verifier.authority_route(tmp_path, candidate) == "v17"


def test_v17_route_is_applicable_and_carries_a_two_path_c2_inventory() -> None:
    assert verifier.is_applicable(()) is False
    assert "v17" in ("v15", "v16", "v17")
    assert len(verifier.V17_C1_PATHS) == 6
    assert len(set(verifier.V17_C1_PATHS)) == 6
    assert verifier.V17_RECORD_PATH not in verifier.V17_C1_PATHS
    assert verifier.V17_AUTHORITY_PATH not in verifier.V17_C1_PATHS
    combined = tuple(sorted((*verifier.V17_C1_PATHS, verifier.V17_AUTHORITY_PATH, verifier.V17_RECORD_PATH)))
    assert len(combined) == 8


def test_multi_path_c2_does_not_change_the_single_path_v15_and_v16_contract() -> None:
    source = (ROOT / "_bmad/scripts/verify_evidence_boundary.py").read_text(encoding="utf-8")

    assert '"c2Path": authority_path' in source
    assert '"c2Paths": list(expected_c2)' in source
    assert "extra_c2_paths: tuple[str, ...] = ()" in source


def test_v23_marker_parser_rejects_reversed_and_trailing_content(monkeypatch: pytest.MonkeyPatch) -> None:
    """The evidence host maps malformed successor markers to stable BLOCKED results."""

    reversed_marker = (
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END fixture -->\n"
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n"
    )
    monkeypatch.setattr(verifier, "candidate_blob", lambda _root, _candidate, _path: reversed_marker)
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.v23_marker_complete(ROOT, "0" * 40)
    assert error.value.code == "EVIDENCE_V23_MARKER_INCOMPLETE"
    assert error.value.state == "BLOCKED"

    trailing = (
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n"
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END fixture -->\n"
        b"unexpected\n"
    )
    monkeypatch.setattr(verifier, "candidate_blob", lambda _root, _candidate, _path: trailing)
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.v23_marker_complete(ROOT, "0" * 40)
    assert error.value.code == "EVIDENCE_V23_MARKER_NOT_TERMINAL"


def test_complete_v23_marker_without_request_or_authority_blocks_legacy_route(tmp_path: Path) -> None:
    """A terminal successor marker cannot fall through to a legacy authority route."""

    init_repository(tmp_path)
    architecture = tmp_path / verifier.V23_ARCHITECTURE_PATH
    architecture.parent.mkdir(parents=True, exist_ok=True)
    architecture.write_bytes(
        b"architecture\n"
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n"
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END fixture -->\n"
    )
    candidate = commit_all(tmp_path, "test: orphan V23 marker")

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.authority_route(tmp_path, candidate)

    assert error.value.code == "EVIDENCE_V23_MARKER_WITHOUT_RECORD"
    assert error.value.state == "BLOCKED"


def test_candidate_blob_reader_isolated_from_dirty_governed_worktree_bytes(tmp_path: Path) -> None:
    """Committed route validation ignores unrelated dirty replacements of governed files."""

    init_repository(tmp_path)
    for relative in verifier.ACTIVE_ROUTE_PATHS:
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((ROOT / relative).read_bytes())
    candidate = commit_all(tmp_path, "test: commit governed routes")
    dirty = tmp_path / verifier.ACTIVE_ROUTE_PATHS[0]
    dirty.write_text("candidate-controlled decoy\n", encoding="utf-8")

    ledger = verifier.validate_active_routes(
        tmp_path,
        lambda _root, relative: verifier.candidate_blob(tmp_path, candidate, relative),
    )

    assert len(ledger) == len(verifier.ACTIVE_ROUTE_PATHS)
    assert all(row["state"] == "PASS" for row in ledger)


def test_evidence_host_authenticates_v23_publisher_before_loading(tmp_path: Path) -> None:
    """The evidence verifier independently proves topology and blob identity before compilation."""

    root = tmp_path / "v23"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", verifier.V23_REQUEST_PUBLICATION], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.name", "Verifier"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.email", "verifier@example.invalid"], check=True)
    candidate = verifier.V23_REQUEST_PUBLICATION

    module, publication = verifier.load_v23_publisher(root, candidate)

    assert publication == candidate
    assert callable(module.validate_request)
    assert publisher.sha256(publisher.candidate_blob(root, candidate, verifier.V23_PUBLISHER_PATH)) == (
        verifier.V23_PUBLISHER_SHA256
    )

    result = verifier.verify(root, verifier.V23_TOOLING_BASELINE, candidate)
    assert result["result"] == "PASS"
    assert result["assertionLedger"]


def test_evidence_host_passes_exact_v24_with_eight_path_nonvacuous_scope(tmp_path: Path) -> None:
    """Successor-aware evidence validates exact V24 while execution remains false."""

    root, candidate = v24_correction_repository(tmp_path)
    result = verifier.verify(root, verifier.V23_REQUEST_PUBLICATION, candidate)
    row = next(item for item in result["assertionLedger"] if item["id"] == "V24-SCOPE-01")

    assert result["result"] == "PASS"
    assert tuple(result["changedPaths"]) == verifier.V24_TOOLING_PATHS
    assert row["state"] == "PASS"
    assert row["count"] == 8
    assert row["executionAllowed"] is False
    assert result["assertionLedger"]


@pytest.mark.parametrize(
    ("fault", "expected_code"),
    (
        ("topology", "EVIDENCE_V24_TOOLING_PARENT_MISMATCH"),
        ("scope", "EVIDENCE_V24_TOOLING_SCOPE_DRIFT"),
        ("manifest", "EVIDENCE_V24_TOOLING_MANIFEST_DRIFT"),
        ("corrected_blob", "EVIDENCE_V24_PUBLISHER_IDENTITY_MISMATCH"),
        ("schema", "EVIDENCE_V24_SCHEMA_IDENTITY_MISMATCH"),
        ("root_gitlink", "EVIDENCE_V24_ROOT_GITLINK_DRIFT"),
    ),
)
def test_v24_evidence_production_route_faults_block_before_execution(
    tmp_path: Path,
    capsys: pytest.CaptureFixture[str],
    fault: str,
    expected_code: str,
) -> None:
    """Each independent V24 fault reaches the production gate and blocks its route."""

    root, candidate = v24_fault_repository(tmp_path, fault)

    exit_code = verifier.main(
        [
            "--repository",
            str(root),
            "--baseline",
            verifier.V23_REQUEST_PUBLICATION,
            "--candidate",
            candidate,
        ]
    )
    document = json.loads(capsys.readouterr().out)

    assert exit_code == 2
    assert document["result"] == "BLOCKED"
    assert document["candidate"] is None
    assert document["assertionLedger"]
    assert all(row["state"] == "BLOCKED" for row in document["assertionLedger"])
    assert len(document["blockers"]) == 1
    assert document["blockers"][0]["code"] == expected_code
    assert document["blockers"][0]["state"] == "BLOCKED"
    assert not any(row["id"] == "V24-SCOPE-01" for row in document["assertionLedger"])


def test_evidence_host_rejects_v24_record_mode_drift(tmp_path: Path) -> None:
    """The independent host checks the correction record's raw mode before execution."""

    root, _candidate = v24_correction_repository(tmp_path)
    subprocess.run(["git", "-C", str(root), "update-index", "--chmod=+x", verifier.V24_CORRECTION_PATH], check=True)
    subprocess.run(["git", "-C", str(root), "commit", "-q", "--amend", "--no-edit"], check=True)
    candidate = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.load_v24_publisher(root, candidate)

    assert error.value.code == "EVIDENCE_V24_TOOLING_MODE_DRIFT"


@pytest.mark.parametrize("relative_path", (verifier.V24_CORRECTION_PATH, verifier.V23_REQUEST_PATH))
def test_evidence_host_rejects_descendant_request_and_correction_mode_drift(
    tmp_path: Path,
    relative_path: str,
) -> None:
    """Mode-only descendant drift stays on the V24 evidence route and blocks."""

    root, correction = v24_correction_repository(tmp_path)
    subprocess.run(["git", "-C", str(root), "update-index", "--chmod=+x", relative_path], check=True)
    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "commit", "-q", "-m", "test: drift evaluated V24 record mode"],
        check=True,
    )
    candidate = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v24_scope(root, candidate)

    assert candidate != correction
    assert error.value.code == "EVIDENCE_V24_TOOLING_MODE_DRIFT"


def test_evidence_route_is_sticky_for_marker_free_deleted_and_reverted_v24_descendants(tmp_path: Path) -> None:
    """V24 ancestry cannot disappear into the legacy V23 evidence route."""

    marker_root, correction = v24_correction_repository(tmp_path / "marker-free")
    unrelated = marker_root / "unrelated-v24-descendant.txt"
    unrelated.write_text("marker-free descendant\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(marker_root), "add", "--sparse", "--", unrelated.name], check=True)
    subprocess.run(
        [
            "git",
            "-c",
            "commit.gpgsign=false",
            "-C",
            str(marker_root),
            "commit",
            "-q",
            "-m",
            "test: add marker-free V24 descendant",
        ],
        check=True,
    )
    marker_candidate = subprocess.check_output(
        ["git", "-C", str(marker_root), "rev-parse", "HEAD"],
        text=True,
    ).strip()
    assert marker_candidate != correction
    assert verifier.authority_route(marker_root, marker_candidate) == "v24"
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v24_scope(marker_root, marker_candidate)
    assert error.value.code == "EVIDENCE_V24_DESCENDANT_REQUIRES_AUTHORITY"

    for restore_v23_tooling in (False, True):
        root, _correction = v24_correction_repository(tmp_path / f"deleted-{restore_v23_tooling}")
        (root / verifier.V24_CORRECTION_PATH).unlink()
        if restore_v23_tooling:
            subprocess.run(
                [
                    "git",
                    "-C",
                    str(root),
                    "checkout",
                    verifier.V23_REQUEST_PUBLICATION,
                    "--",
                    verifier.V23_PUBLISHER_PATH,
                    "_bmad/scripts/verify_evidence_boundary.py",
                ],
                check=True,
            )
        candidate = commit_all(root, "test: delete V24 route without authority")

        assert verifier.authority_route(root, candidate) == "v24"
        with pytest.raises(verifier.BoundaryError) as error:
            verifier.validate_v24_scope(root, candidate)
        assert error.value.code == "EVIDENCE_V24_TOOLING_MODE_DRIFT"


@pytest.mark.parametrize(
    ("scenario", "expected_code"),
    (
        ("readd", "EVIDENCE_V24_CORRECTION_PUBLICATION_MISSING"),
        ("treesame", "EVIDENCE_V24_TOOLING_MODE_DRIFT"),
    ),
)
def test_v24_evidence_production_route_traverses_full_history(
    tmp_path: Path,
    scenario: str,
    expected_code: str,
) -> None:
    """Sticky routing sees TREESAME ancestry and rejects duplicate correction additions."""

    root, candidate = v24_full_history_repository(tmp_path, scenario)

    assert verifier.authority_route(root, candidate) == "v24"
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v24_scope(root, candidate)

    assert error.value.code == expected_code


def test_evidence_host_rejects_self_consistent_hostile_v24_publisher(tmp_path: Path) -> None:
    """A hostile publisher and matching manifest cannot redefine the evidence trust root."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v24_evidence_hostile_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    root = fixtures.correction_writer_root(tmp_path)
    subprocess.run(["git", "-C", str(root), "config", "user.name", "V24 hostile fixture"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.email", "v24-hostile@example.invalid"], check=True)
    (root / verifier.V23_PUBLISHER_PATH).write_text(
        "def validate_correction(*args, **kwargs): return ({}, '0' * 40, b'')\n"
        "def validate_current_request(*args, **kwargs): return ({}, '0' * 40, b'', '0' * 40)\n"
        "def request_check_result(*args, **kwargs): return {'result': 'PASS'}\n"
        "def resolve_published_authority(*args, **kwargs): return {'result': 'PASS'}\n",
        encoding="utf-8",
    )
    correction = publisher.render_correction(root)
    (root / verifier.V24_CORRECTION_PATH).write_bytes(publisher.json_bytes(correction))
    subprocess.run(["git", "-C", str(root), "add", "--", *verifier.V24_TOOLING_PATHS], check=True)
    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "commit", "-q", "-m", "test: publish hostile V24 tooling"],
        check=True,
    )
    candidate = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.load_v24_publisher(root, candidate)

    assert error.value.code == "EVIDENCE_V24_PUBLISHER_IDENTITY_MISMATCH"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document["assertionLedger"].__setitem__(0, "scalar"),
        lambda document: document["assertionLedger"][0].update({"extra": "open"}),
        lambda document: document["assertionLedger"][0].update({"detail": ""}),
        lambda document: document["blockers"].__setitem__(0, "scalar"),
        lambda document: document["blockers"][0].update({"extra": "open"}),
        lambda document: document["blockers"][0].update({"detail": ""}),
    ),
)
def test_evidence_v24_host_rejects_nonclosed_nested_request_rows(
    monkeypatch: pytest.MonkeyPatch,
    mutation: Callable[[dict[str, Any]], None],
) -> None:
    """Corrected request rows remain closed under the independent evidence host."""

    candidate = "0" * 40
    request = publisher.render_request(ROOT)
    malformed = publisher.request_check_result(request, verifier.V23_REQUEST_PUBLICATION)
    mutation(malformed)

    class MalformedModule:
        @staticmethod
        def validate_correction(*_args: object, **_kwargs: object) -> tuple[dict[str, object], str, bytes]:
            return {}, candidate, b"correction"

        @staticmethod
        def validate_current_request(*_args: object, **_kwargs: object) -> tuple[dict[str, object], str, bytes, str]:
            return request, verifier.V23_REQUEST_PUBLICATION, b"request", candidate

        @staticmethod
        def request_check_result(*_args: object, **_kwargs: object) -> dict[str, object]:
            return malformed

    monkeypatch.setattr(verifier, "load_v24_publisher", lambda _root, _candidate: (MalformedModule(), candidate))
    monkeypatch.setattr(verifier, "v23_marker_complete", lambda _root, _candidate: False)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v24_scope(ROOT, candidate)

    assert error.value.code == "EVIDENCE_V24_REQUEST_RESULT_INVALID"


def test_protected_v23_base_host_exposes_v24_bootstrap_blocker(tmp_path: Path) -> None:
    """The unchanged protected-base verifier visibly blocks V24 until host migration."""

    root, candidate = v24_correction_repository(tmp_path / "candidate")
    protected_host = tmp_path / "protected-v23-verifier.py"
    protected_host.write_bytes(
        subprocess.check_output(
            [
                "git",
                "-C",
                str(ROOT),
                "show",
                f"{verifier.V23_REQUEST_PUBLICATION}:_bmad/scripts/verify_evidence_boundary.py",
            ]
        )
    )
    completed = subprocess.run(
        [
            sys.executable,
            str(protected_host),
            "--repository",
            str(root),
            "--baseline",
            verifier.V23_REQUEST_PUBLICATION,
            "--candidate",
            candidate,
        ],
        capture_output=True,
        text=True,
        check=False,
    )
    document = json.loads(completed.stdout)

    assert completed.returncode == 2
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "EVIDENCE_V23_PUBLISHER_DESCENDANT_DRIFT"


def test_evidence_host_rejects_relaxed_candidate_schema_before_publisher_load(tmp_path: Path) -> None:
    """The evidence host pins schema bytes independently of candidate JSON Schema rules."""

    root = tmp_path / "relaxed-v23-schema"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", verifier.V23_REQUEST_PUBLICATION], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.name", "Verifier"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.email", "verifier@example.invalid"], check=True)
    request_path = root / verifier.V23_REQUEST_PATH
    request = json.loads(request_path.read_bytes())
    (root / verifier.V23_SCHEMA_PATH).write_text(
        '{"$schema":"https://json-schema.org/draft/2020-12/schema"}\n',
        encoding="utf-8",
    )
    request["executionAllowed"] = True
    request["storyExecution"] = {"7.1": True, "7.2": True, "7.3": False, "7.4": False}
    request_path.write_bytes(publisher.json_bytes(request))
    subprocess.run(["git", "-C", str(root), "add", "--", verifier.V23_SCHEMA_PATH, verifier.V23_REQUEST_PATH], check=True)
    subprocess.run(["git", "-C", str(root), "commit", "-q", "--amend", "--no-edit"], check=True)
    candidate = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.load_v23_publisher(root, candidate)

    assert error.value.code == "EVIDENCE_V23_SCHEMA_IDENTITY_MISMATCH"


def test_signed_v24_corrected_authority_passes_through_production_evidence_host(
    tmp_path: Path,
) -> None:
    """The evidence host accepts signed authority only after V24 authenticated loading."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v23_evidence_authority_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    private_key = tmp_path / "evidence_authority_key"
    subprocess.run(
        [publisher.SSH_KEYGEN_EXECUTABLE, "-q", "-t", "ed25519", "-N", "", "-f", str(private_key)],
        check=True,
    )
    public_key = private_key.with_suffix(".pub").read_text(encoding="utf-8").strip()
    fingerprint = subprocess.check_output(
        [publisher.SSH_KEYGEN_EXECUTABLE, "-lf", str(private_key.with_suffix(".pub")), "-E", "sha256"],
        text=True,
    ).split()[1]
    root, _request, publication, _authority = fixtures.authority_repository(
        tmp_path / "authority",
        signing_key=private_key,
    )
    authenticated_module, correction_publication = verifier.load_v24_publisher(root, publication)
    assert correction_publication != publication

    def verify(repo: Path, commit: str, owner: str) -> dict[str, str]:
        facts = authenticated_module.verify_publication_signature(
            repo,
            commit,
            owner,
            trusted_public_key=public_key,
            trusted_fingerprint=fingerprint,
            ssh_keygen_path=publisher.SSH_KEYGEN_EXECUTABLE,
        )
        assert facts["status"] == "G"
        return {
            "status": "G",
            "principal": verifier.V23_TRUSTED_SSH_PRINCIPAL,
            "fingerprint": verifier.V23_TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": verifier.V23_TRUSTED_OWNER_IDENTITY,
        }

    result = verifier.verify(
        root,
        verifier.V23_TOOLING_BASELINE,
        publication,
        signature_verifier=verify,
    )
    row = next(row for row in result["assertionLedger"] if row["id"] == "V24-SCOPE-01")

    assert result["result"] == "PASS"
    assert row["state"] == "PASS"
    assert row["route"] == "authority"
    assert row["publisherSha256"] == verifier.V24_PUBLISHER_SHA256


def test_v23_request_route_rejects_descendants_and_complete_markers(tmp_path: Path) -> None:
    """The request route is exact and cannot coexist with descendant scope or an authority marker."""

    root = tmp_path / "request-route"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", verifier.V23_REQUEST_PUBLICATION], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.name", "Verifier"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.email", "verifier@example.invalid"], check=True)
    unexpected = root / "unexpected-descendant.txt"
    unexpected.write_text("unexpected\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "unexpected-descendant.txt"], check=True)
    subprocess.run(["git", "-C", str(root), "commit", "-q", "-m", "test: request descendant"], check=True)
    descendant = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v23_scope(root, descendant, authority=False)
    assert error.value.code == "EVIDENCE_V23_REQUEST_DESCENDANT_SCOPE"

    architecture = root / verifier.V23_ARCHITECTURE_PATH
    architecture.write_bytes(
        publisher.candidate_blob(root, descendant, verifier.V23_ARCHITECTURE_PATH)
        + b"\n<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n"
        + b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END fixture -->\n"
    )
    subprocess.run(["git", "-C", str(root), "add", verifier.V23_ARCHITECTURE_PATH], check=True)
    subprocess.run(["git", "-C", str(root), "commit", "-q", "-m", "test: conflicting request marker"], check=True)
    marked = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()
    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v23_scope(root, marked, authority=False)
    assert error.value.code == "EVIDENCE_V23_REQUEST_ROUTE_CONFLICT"


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document.update({"schemaVersion": "hostile"}),
        lambda document: document.update({"implementationHold": "ACTIVE"}),
        lambda document: document.update({"pushAuthorized": True}),
        lambda document: document["storyExecution"].update({"7.2": True}),
        lambda document: document["assertionLedger"][0].update({"state": "FAIL"}),
        lambda document: document["assertionLedger"][0].update({"detail": ""}),
        lambda document: document["assertionLedger"].append(dict(document["assertionLedger"][0])),
        pytest.param(lambda document: document.update({"observed": {}}), id="empty-observed"),
        pytest.param(lambda document: document["observed"].update({"unexpected": "hostile"}), id="unknown-observed"),
        pytest.param(
            lambda document: document["observed"].update({"authorityPublication": "d" * 40}),
            id="contradictory-observed",
        ),
        pytest.param(
            lambda document: document["observed"]["ownerSignature"].update({"principal": "hostile@example.invalid"}),
            id="wrong-owner-principal",
        ),
    ),
)
def test_evidence_host_rejects_contradictory_v23_results(
    mutation: Callable[[dict[str, Any]], None],
) -> None:
    """The evidence host independently closes every authorization and ledger invariant."""

    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": "PASS",
        "exitCode": 0,
        "effectiveHold": "EXECUTION_ALLOWED",
        "implementationHold": "EXECUTION_ALLOWED",
        "observed": {
            "candidateCommit": "a" * 40,
            "candidateTree": "b" * 40,
            "authorityPublication": "a" * 40,
            "sourceCommit": "c" * 40,
            "changedPaths": [verifier.V23_ARCHITECTURE_PATH, verifier.V23_AUTHORITY_PATH],
            "ownerSignature": {
                "status": "G",
                "principal": verifier.V23_TRUSTED_SSH_PRINCIPAL,
                "fingerprint": verifier.V23_TRUSTED_SSH_FINGERPRINT,
                "authorIdentity": verifier.V23_TRUSTED_OWNER_IDENTITY,
            },
        },
        "assertionLedger": [
            {
                "id": f"V23.AUTHORITY.{index:02d}",
                "subject": subject,
                "state": "PASS",
                "detail": f"{subject} passed",
            }
            for index, subject in enumerate(verifier.V23_AUTHORITY_GATE_SUBJECTS, start=1)
        ]
        + [
            {
                "id": "V23.AUTHORITY.SIGNATURE",
                "subject": "trusted-owner-publication-signature",
                "state": "PASS",
                "detail": "trusted owner publication signature passed",
            }
        ],
        "blockers": [],
        "ownerApprovalClaimed": True,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": True,
        "storyExecution": {"7.1": True, "7.2": False, "7.3": False, "7.4": False},
    }
    assert verifier.validate_v23_result(document, "PASS") == document
    mutation(document)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v23_result(document, "PASS")

    assert error.value.code == "EVIDENCE_V23_RESULT_INVALID"


def test_evidence_host_preserves_empty_observed_for_structured_failure() -> None:
    """Authority failures remain stable before any observable Git fact is available."""

    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": "V23_HISTORY_UNAVAILABLE",
                "subject": "v23-entry-authority",
                "state": "BLOCKED",
                "detail": "history unavailable",
            }
        ],
        "blockers": [{"code": "V23_HISTORY_UNAVAILABLE", "detail": "history unavailable", "assertionIndex": 0}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    assert verifier.validate_v23_result(document, "BLOCKED") == document


def test_evidence_host_accepts_closed_v23_fail_document() -> None:
    """The evidence host positively exercises the valid authority FAIL alternative."""

    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": "FAIL",
        "exitCode": 1,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": "V23_GATE_DISPOSITION_DRIFT",
                "subject": "v23-entry-authority",
                "state": "FAIL",
                "detail": "gate disposition drift",
            }
        ],
        "blockers": [
            {"code": "V23_GATE_DISPOSITION_DRIFT", "detail": "gate disposition drift", "assertionIndex": 0}
        ],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    assert verifier.validate_v23_result(document, "FAIL") == document


@pytest.mark.parametrize("state", ("FAIL", "BLOCKED"))
def test_v23_authority_scope_preserves_authenticated_publisher_failure(
    monkeypatch: pytest.MonkeyPatch,
    state: str,
) -> None:
    """A closed publisher failure keeps its stable state and blocker after host validation."""

    code = f"V23_FIXTURE_{state}"
    detail = f"authenticated publisher returned {state}"
    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": state,
        "exitCode": {"FAIL": 1, "BLOCKED": 2}[state],
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": code,
                "subject": "v23-entry-authority",
                "state": state,
                "detail": detail,
            }
        ],
        "blockers": [{"code": code, "detail": detail, "assertionIndex": 0}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    class AuthenticatedPublisher:
        @staticmethod
        def resolve_published_authority(*_args: object, **_kwargs: object) -> dict[str, Any]:
            return document

    candidate = "a" * 40
    loaded: list[str] = []

    def load(_root: Path, evaluated: str) -> tuple[object, str]:
        loaded.append(evaluated)
        return AuthenticatedPublisher(), "b" * 40

    monkeypatch.setattr(verifier, "v23_marker_complete", lambda _root, _candidate: True)
    monkeypatch.setattr(verifier, "load_v23_publisher", load)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v23_scope(ROOT, candidate, authority=True)

    assert loaded == [candidate]
    assert error.value.code == code
    assert error.value.state == state
    assert error.value.message == detail


@pytest.mark.parametrize("state", ("FAIL", "BLOCKED"))
def test_v24_authority_scope_preserves_authenticated_publisher_failure(
    monkeypatch: pytest.MonkeyPatch,
    state: str,
) -> None:
    """Authenticated V24 authority failures preserve publisher state, code, and detail."""

    candidate = "a" * 40
    correction_publication = "b" * 40
    code = f"V24_FIXTURE_{state}"
    detail = f"authenticated V24 publisher returned {state}"
    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": state,
        "exitCode": {"FAIL": 1, "BLOCKED": 2}[state],
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": code,
                "subject": "v23-entry-authority",
                "state": state,
                "detail": detail,
            }
        ],
        "blockers": [{"code": code, "detail": detail, "assertionIndex": 0}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    class AuthenticatedV24Publisher:
        @staticmethod
        def validate_correction(*_args: object, **_kwargs: object) -> tuple[dict[str, object], str, bytes]:
            return {}, correction_publication, b"correction"

        @staticmethod
        def resolve_published_authority(*_args: object, **_kwargs: object) -> dict[str, Any]:
            return document

    loaded: list[str] = []

    def load(_root: Path, evaluated: str) -> tuple[object, str]:
        loaded.append(evaluated)
        return AuthenticatedV24Publisher(), correction_publication

    monkeypatch.setattr(verifier, "load_v24_publisher", load)
    monkeypatch.setattr(verifier, "v23_marker_complete", lambda _root, _candidate: True)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v24_scope(ROOT, candidate)

    assert loaded == [candidate]
    assert error.value.code == code
    assert error.value.state == state
    assert error.value.message == detail


def test_evidence_host_rejects_blocker_without_matching_ledger_row() -> None:
    """A blocker must correlate to an equally detailed non-PASS assertion."""

    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": "V23_HISTORY_UNAVAILABLE",
                "subject": "v23-entry-authority",
                "state": "BLOCKED",
                "detail": "history unavailable",
            }
        ],
        "blockers": [{"code": "V23_OTHER", "detail": "history unavailable", "assertionIndex": 0}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    with pytest.raises(verifier.BoundaryError):
        verifier.validate_v23_result(document, "BLOCKED")


@pytest.mark.parametrize(
    "mutation",
    (
        pytest.param(lambda observed: observed.clear(), id="empty-observed"),
        pytest.param(lambda observed: observed.update({"unexpected": "hostile"}), id="unknown-observed"),
        pytest.param(lambda observed: observed.update({"protectedMain": "d" * 40}), id="contradictory-observed"),
    ),
)
def test_evidence_host_rejects_invalid_request_observed(mutation: Callable[[dict[str, str]], None]) -> None:
    """Request results expose exactly the three immutable publication identities."""

    observed = {
        "requestPublication": "a" * 40,
        "protectedMain": verifier.V23_PROTECTED_MAIN,
        "historicalV22Candidate": verifier.V23_HISTORICAL_V22_CANDIDATE,
    }
    document = {
        "schemaVersion": verifier.V23_RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": observed,
        "assertionLedger": [
            {"id": identifier, "subject": subject, "state": state, "detail": f"fixture {identifier}"}
            for identifier, subject, state in verifier.V23_REQUEST_LEDGER
        ],
        "blockers": [
            {"code": code, "detail": f"fixture {code}", "assertionIndex": index}
            for code, index in zip(verifier.V23_REQUEST_BLOCKERS, (6, 7, 8, 9, 10), strict=True)
        ],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }
    assert verifier.validate_v23_result(document, "BLOCKED", route="request") == document
    mutation(observed)

    with pytest.raises(verifier.BoundaryError) as error:
        verifier.validate_v23_result(document, "BLOCKED", route="request")

    assert error.value.code == "EVIDENCE_V23_RESULT_INVALID"


def test_evidence_git_environment_ignores_hostile_redirects(monkeypatch: pytest.MonkeyPatch) -> None:
    """Ambient config, object, and executable redirects are excluded from evidence reads."""

    monkeypatch.setenv("PATH", "/tmp/hostile")
    monkeypatch.setenv("GIT_DIR", "/tmp/hostile.git")
    monkeypatch.setenv("GIT_OBJECT_DIRECTORY", "/tmp/objects")
    environment = verifier.trusted_environment()

    assert environment["PATH"] == verifier.TRUSTED_EXECUTABLE_PATH
    assert "GIT_DIR" not in environment
    assert "GIT_OBJECT_DIRECTORY" not in environment
    assert environment["GIT_CONFIG_GLOBAL"] == "/dev/null"


def test_malformed_utf8_from_committed_route_returns_structured_blocked(
    monkeypatch: pytest.MonkeyPatch,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """A strict UTF-8 read failure cannot escape the evidence host result contract."""

    monkeypatch.setattr(
        verifier,
        "verify",
        lambda *_args, **_kwargs: (_ for _ in ()).throw(UnicodeDecodeError("utf-8", b"\xff", 0, 1, "invalid")),
    )

    exit_code = verifier.main(["--repository", str(ROOT), "--baseline", "HEAD", "--candidate", "HEAD"])
    document = json.loads(capsys.readouterr().out)

    assert exit_code == 2
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "EVIDENCE_MALFORMED"
    assert document["assertionLedger"]


def test_production_verify_ignores_all_governed_dirty_worktree_categories(tmp_path: Path) -> None:
    """Routes, context, context workflows, and signature guards are read from candidate objects."""

    root = tmp_path / "dirty-production"
    subprocess.run(["git", "clone", "-q", "--shared", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", verifier.V23_REQUEST_PUBLICATION], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.name", "Verifier"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.email", "verifier@example.invalid"], check=True)
    candidate = verifier.V23_REQUEST_PUBLICATION

    dirty_paths = (
        verifier.ACTIVE_ROUTE_PATHS[0],
        "_bmad-output/implementation-artifacts/epic-6-context.md",
        ".agents/skills/bmad-build/compile-epic-context.md",
        "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
    )
    for relative in dirty_paths:
        target = root / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text("candidate-controlled dirty decoy\n", encoding="utf-8")

    result = verifier.verify(root, verifier.V23_TOOLING_BASELINE, candidate)

    assert result["result"] == "PASS"
    assert set(dirty_paths).issubset(result["worktreePaths"])
    assert result["assertionLedger"]


def v27_fixtures() -> Any:
    """Load the shared V27 repository fixtures from the publisher test module."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_lifecycle_evidence_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v27_evidence_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    original_copy = fixtures.copy_bootstrap_inputs

    def copy_historical_workflow(root: Path, extra: dict[str, bytes] | None = None) -> None:
        original_copy(root, extra)
        content = subprocess.check_output(
            ["git", "-C", str(ROOT), "cat-file", "blob", f"188c5a33eaede168a04c8412380fc8d5c02e11a3:{verifier.V27_WORKFLOW_PATH}"]
        )
        (root / verifier.V27_WORKFLOW_PATH).write_bytes(content)

    fixtures.copy_bootstrap_inputs = copy_historical_workflow
    return fixtures


@pytest.fixture(scope="module")
def v27_published(tmp_path_factory: pytest.TempPathFactory) -> tuple[Any, Path, str, str, str]:
    """Provide one reusable V27 bootstrap, record, and preserved descendant."""

    fixtures = v27_fixtures()
    root, bootstrap, publication, tip = fixtures.descendant_repository(tmp_path_factory.mktemp("v27-evidence"))
    return fixtures, root, bootstrap, publication, tip


def test_v27_route_requires_protected_host_provenance_and_precedes_v24(
    v27_published: tuple[Any, Path, str, str, str],
) -> None:
    """The evidence host selects V27 only from externally authorized provenance."""

    fixtures, root, bootstrap, publication, _tip = v27_published
    assert verifier.authority_route(root, publication) == "v24"
    assert verifier.authority_route(root, publication, fixtures.PREDECESSOR_COMMIT) == "v24"
    assert verifier.authority_route(root, publication, bootstrap) == "v27"
    assert verifier.v27_bootstrap_publication(root, publication) == bootstrap


def test_v27_record_only_child_and_descendant_pass_the_evidence_scope(
    v27_published: tuple[Any, Path, str, str, str],
) -> None:
    """Exact C2 and a preserved descendant both produce a non-executable PASS row."""

    _fixtures, root, bootstrap, publication, tip = v27_published
    record_row = verifier.validate_v27_scope(root, publication, bootstrap)
    descendant_row = verifier.validate_v27_scope(root, tip, bootstrap)
    assert record_row["id"] == "V27-SCOPE-01"
    assert record_row["state"] == "PASS"
    assert record_row["route"] == "V27.ROUTE.C2"
    assert record_row["executionAllowed"] is False
    assert descendant_row["route"] == "V27.ROUTE.DESCENDANT"
    assert descendant_row["bootstrap"] == bootstrap


def test_v27_exact_bootstrap_blocks_the_evidence_boundary(tmp_path: Path) -> None:
    """The authorized bootstrap alone blocks with the stable missing-record code."""

    fixtures = v27_fixtures()
    root, bootstrap = fixtures.bootstrap_repository(tmp_path)
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.validate_v27_scope(root, bootstrap, bootstrap)
    assert failure.value.code == "EVIDENCE_V27_C2_PUBLICATION_MISSING"
    assert failure.value.state == "BLOCKED"


def test_v27_scope_without_provenance_is_blocked(
    v27_published: tuple[Any, Path, str, str, str],
) -> None:
    """Absent or pre-bootstrap provenance never reaches the V27 boundary."""

    fixtures, root, _bootstrap, publication, _tip = v27_published
    for provenance in (None, fixtures.PREDECESSOR_COMMIT):
        with pytest.raises(verifier.BoundaryError) as failure:
            verifier.validate_v27_scope(root, publication, provenance)
        assert failure.value.code == "EVIDENCE_V27_BOOTSTRAP_NOT_PROTECTED"
        assert failure.value.state == "BLOCKED"


@pytest.mark.parametrize(
    ("scenario", "code"),
    [
        ("governed-restore", "EVIDENCE_V27_GOVERNED_PATH_TOUCHED"),
        ("gitlink", "EVIDENCE_V27_GOVERNED_PATH_TOUCHED"),
        ("record", "EVIDENCE_V27_RECORD_HISTORY_TOUCHED"),
    ],
)
def test_v27_governed_drift_fails_the_evidence_boundary(
    tmp_path: Path,
    scenario: str,
    code: str,
) -> None:
    """Restored governed drift is a stable FAIL at the independent evidence host."""

    fixtures = v27_fixtures()
    root, bootstrap, _publication, tip = fixtures.drift_repository(tmp_path, scenario)
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.validate_v27_scope(root, tip, bootstrap)
    assert failure.value.code == code
    assert failure.value.state == "FAIL"


def test_v27_top_level_verify_dispatches_the_v27_scope(
    v27_published: tuple[Any, Path, str, str, str],
) -> None:
    """The complete gate, not only the focused helper, routes and records V27."""

    _fixtures, root, bootstrap, publication, tip = v27_published
    document = verifier.verify(root, bootstrap, publication, trusted_host=bootstrap)
    assert document["result"] == "PASS"
    assert document["changedPaths"] == [verifier.V27_RECORD_PATH]
    rows = [row for row in document["assertionLedger"] if row["id"] == "V27-SCOPE-01"]
    assert len(rows) == 1
    assert rows[0]["route"] == "V27.ROUTE.C2"
    assert not any(row["id"] == "V24-SCOPE-01" for row in document["assertionLedger"])

    descendant = verifier.verify(root, publication, tip, trusted_host=bootstrap)
    assert descendant["result"] == "PASS"
    assert [row["route"] for row in descendant["assertionLedger"] if row["id"] == "V27-SCOPE-01"] == [
        "V27.ROUTE.DESCENDANT"
    ]


def test_v27_top_level_verify_blocks_the_exact_bootstrap(tmp_path: Path) -> None:
    """The complete gate preserves the missing-record blocker instead of passing."""

    fixtures = v27_fixtures()
    root, bootstrap = fixtures.bootstrap_repository(tmp_path)
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.verify(root, fixtures.PREDECESSOR_COMMIT, bootstrap, trusted_host=bootstrap)
    assert failure.value.code == "EVIDENCE_V27_C2_PUBLICATION_MISSING"
    assert failure.value.state == "BLOCKED"


def test_v27_cli_preserves_pass_and_blocked_exit_codes(
    v27_published: tuple[Any, Path, str, str, str],
) -> None:
    """The operator-facing gate carries provenance and exit semantics end to end."""

    _fixtures, root, bootstrap, publication, _tip = v27_published
    passing = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--repository",
            str(root),
            "--baseline",
            bootstrap,
            "--candidate",
            publication,
            "--trusted-host",
            bootstrap,
        ],
        check=False,
        capture_output=True,
        text=True,
    )
    assert passing.returncode == 0
    assert json.loads(passing.stdout)["result"] == "PASS"

    blocked = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--repository",
            str(root),
            "--baseline",
            bootstrap,
            "--candidate",
            bootstrap,
            "--trusted-host",
            bootstrap,
        ],
        check=False,
        capture_output=True,
        text=True,
    )
    assert blocked.returncode == 2
    document = json.loads(blocked.stdout)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "EVIDENCE_V27_C2_PUBLICATION_MISSING"
    assert document["assertionLedger"]


def test_v27_evidence_host_rejects_an_untruthful_observed_parent_diff(
    v27_published: tuple[Any, Path, str, str, str],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """The evidence host recomputes the parent diff instead of trusting the publisher."""

    _fixtures, root, bootstrap, publication, _tip = v27_published
    module, schema_content = verifier.load_v27_publisher(root, bootstrap)
    honest = module.verify_revision(root, publication, bootstrap)

    class Untruthful:
        @staticmethod
        def verify_revision(repository: Path, candidate: str, trusted_host: str) -> dict[str, Any]:
            document = json.loads(json.dumps(honest))
            document["observed"]["parentCommit"] = "0" * 40
            return document

    monkeypatch.setattr(verifier, "load_v27_publisher", lambda root_, commit: (Untruthful, schema_content))
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.validate_v27_scope(root, publication, bootstrap)
    assert failure.value.code in (
        "EVIDENCE_V27_OBSERVED_DIFF_UNTRUTHFUL",
        "EVIDENCE_V27_RESULT_SCHEMA_INVALID",
    )
    assert failure.value.state == "BLOCKED"


def test_v27_unavailable_history_blocks_route_scope_and_top_level_gate(tmp_path: Path) -> None:
    """Truncated history blocks selection, the focused scope, and the complete gate."""

    fixtures = v27_fixtures()
    shallow, tip = fixtures.shallow_repository(tmp_path)
    for call in (
        lambda: verifier.authority_route(shallow, tip, tip),
        lambda: verifier.validate_v27_scope(shallow, tip, tip),
        lambda: verifier.verify(shallow, tip, tip, trusted_host=tip),
    ):
        with pytest.raises(verifier.BoundaryError) as failure:
            call()
        assert failure.value.code == "EVIDENCE_V27_HISTORY_UNAVAILABLE"
        assert failure.value.state == "BLOCKED"


def test_v27_unavailable_history_cli_is_blocked_with_a_nonempty_ledger(tmp_path: Path) -> None:
    """The operator-facing gate maps truncated history to BLOCKED and exit 2."""

    fixtures = v27_fixtures()
    shallow, tip = fixtures.shallow_repository(tmp_path)
    completed = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--repository",
            str(shallow),
            "--baseline",
            tip,
            "--candidate",
            tip,
            "--trusted-host",
            tip,
        ],
        check=False,
        capture_output=True,
        text=True,
    )
    assert completed.returncode == 2
    document = json.loads(completed.stdout)
    assert document["result"] == "BLOCKED"
    assert document["result"] not in ("PASS", "FAIL", "not-applicable")
    assert document["blockers"][0]["code"] == "EVIDENCE_V27_HISTORY_UNAVAILABLE"
    assert document["assertionLedger"]


def test_v27_propagated_blocker_codes_use_one_evidence_namespace(tmp_path: Path) -> None:
    """Every code this host raises or propagates carries the EVIDENCE_V27_ prefix."""

    fixtures = v27_fixtures()
    root, bootstrap = fixtures.bootstrap_repository(tmp_path)
    with pytest.raises(verifier.BoundaryError) as propagated:
        verifier.validate_v27_scope(root, bootstrap, bootstrap)
    assert propagated.value.code.startswith("EVIDENCE_V27_")
    assert propagated.value.code == "EVIDENCE_V27_C2_PUBLICATION_MISSING"

    with pytest.raises(verifier.BoundaryError) as native:
        verifier.validate_v27_scope(root, bootstrap, None)
    assert native.value.code.startswith("EVIDENCE_V27_")


def test_every_evidence_document_states_the_hold_and_four_authority_flags(
    v27_published: tuple[Any, Path, str, str, str],
    tmp_path: Path,
) -> None:
    """AC3 holds at this host too: the hold and flags are stated, never inferred."""

    _fixtures, root, bootstrap, publication, _tip = v27_published
    passing = verifier.verify(root, bootstrap, publication, trusted_host=bootstrap)
    failing = verifier.failure_document(root, verifier.BoundaryError("EVIDENCE_V27_HISTORY_UNAVAILABLE", "x", "BLOCKED"))

    # The third evidence document is the not-applicable one; it states the same contract.
    inapplicable = tmp_path / "inapplicable"
    init_repository(inapplicable)
    (inapplicable / "unrelated.txt").write_text("unrelated\n", encoding="utf-8")
    baseline = commit_all(inapplicable, "test: baseline")
    (inapplicable / "unrelated.txt").write_text("changed\n", encoding="utf-8")
    candidate = commit_all(inapplicable, "test: candidate")
    not_applicable = verifier.verify(inapplicable, baseline, candidate)
    assert not_applicable["result"] == "not-applicable"

    for document in (passing, failing, not_applicable):
        assert document["effectiveHold"] == "ACTIVE"
        assert document["implementationHold"] == "ACTIVE"
        assert document["ownerApprovalClaimed"] is False
        assert document["releaseAuthorized"] is False
        assert document["pushAuthorized"] is False
        assert document["executionAllowed"] is False


def test_v27_empty_provenance_leaves_v24_authoritative_at_the_evidence_host(
    v27_published: tuple[Any, Path, str, str, str],
) -> None:
    """An unset workflow output is absent provenance, not a Git revision error."""

    _fixtures, root, _bootstrap, publication, _tip = v27_published
    assert verifier.v27_route_selected(root, publication, "") is None
    assert verifier.authority_route(root, publication, "") == "v24"


def test_v27_merge_and_partial_clone_candidates_block_the_evidence_boundary(tmp_path: Path) -> None:
    """Neither a merge candidate nor a partial clone reaches a V27 pass at this host."""

    fixtures = v27_fixtures()
    merge_root = tmp_path / "merge"
    merge_root.mkdir()
    partial_root = tmp_path / "partial-clone"
    partial_root.mkdir()

    root, bootstrap, merged = fixtures.merge_repository(merge_root)
    with pytest.raises(verifier.BoundaryError) as merge_failure:
        verifier.validate_v27_scope(root, merged, bootstrap)
    assert merge_failure.value.code == "EVIDENCE_V27_CANDIDATE_PARENT_DRIFT"
    assert merge_failure.value.state == "BLOCKED"

    partial, tip = fixtures.partial_clone_repository(partial_root)
    with pytest.raises(verifier.BoundaryError) as partial_failure:
        verifier.validate_v27_scope(partial, tip, tip)
    assert partial_failure.value.code == "EVIDENCE_V27_HISTORY_UNAVAILABLE"
    assert "partial clone" in partial_failure.value.message


def test_v27_evidence_host_recomputes_the_full_identity_of_the_parent_diff(
    v27_published: tuple[Any, Path, str, str, str],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Fabricated mode, object or digest values are rejected even when the paths are truthful."""

    _fixtures, root, bootstrap, _publication, tip = v27_published
    module, schema_content = verifier.load_v27_publisher(root, bootstrap)
    honest = module.verify_revision(root, tip, bootstrap)

    for field, value in (("mode", "100755"), ("objectId", "0" * 40), ("sha256", "1" * 64)):
        class Fabricated:
            @staticmethod
            def verify_revision(repository: Path, candidate: str, trusted_host: str) -> dict[str, Any]:
                document = json.loads(json.dumps(honest))
                document["observed"]["changedPaths"][0][field] = value
                return document

        monkeypatch.setattr(verifier, "load_v27_publisher", lambda root_, commit: (Fabricated, schema_content))
        with pytest.raises(verifier.BoundaryError) as failure:
            verifier.validate_v27_scope(root, tip, bootstrap)
        assert failure.value.code == "EVIDENCE_V27_OBSERVED_DIFF_UNTRUTHFUL", field
        monkeypatch.undo()


def v28_fixtures() -> Any:
    """Load the production-shaped V28 publication fixtures."""

    path = ROOT / "_bmad/scripts/tests/test_publish_v28_five_root_gitlink_authority.py"
    spec = importlib.util.spec_from_file_location("v28_evidence_fixtures", path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_v28_evidence_host_requires_protected_c1_and_then_blocks(tmp_path: Path) -> None:
    """The independent host retains the predecessor route before protected C1."""

    fixtures = v28_fixtures()
    root, bootstrap = fixtures.bootstrap_repository(tmp_path)
    assert verifier.v28_route_selected(root, bootstrap, fixtures.PREDECESSOR_COMMIT) is None
    with pytest.raises(verifier.BoundaryError) as old:
        verifier.validate_v27_scope(root, bootstrap, fixtures.PREDECESSOR_COMMIT)
    assert old.value.code == "EVIDENCE_V27_GOVERNED_PATH_TOUCHED"
    with pytest.raises(verifier.BoundaryError) as blocked:
        verifier.validate_v28_scope(root, bootstrap, bootstrap)
    assert blocked.value.code == "EVIDENCE_V28_C2_PUBLICATION_MISSING"


def test_v28_evidence_host_passes_c2_and_untouched_descendant(tmp_path: Path) -> None:
    """The evidence host independently authenticates the frozen C1 manifest."""

    fixtures = v28_fixtures()
    root, bootstrap, publication, tip = fixtures.descendant_repository(tmp_path)
    for candidate in (publication, tip):
        row = verifier.validate_v28_scope(root, candidate, bootstrap)
        assert row["state"] == "PASS"
        assert row["executionAllowed"] is False
        document = verifier.verify(root, bootstrap, candidate, trusted_host=bootstrap)
        assert document["result"] == "PASS"
        assert document["assertionLedger"]
        assert document["implementationHold"] == "ACTIVE"
        assert all(document[key] is False for key in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"))


@pytest.mark.parametrize(
    "scenario",
    ("governed-restore", "frozen-story-json", "frozen-story-md", "frozen-v27-record", "frozen-v27-schema", "frozen-v27-publisher", "frozen-v28-spec"),
)
def test_v28_evidence_host_rejects_restored_governed_history(tmp_path: Path, scenario: str) -> None:
    """The evidence host cannot accept a restored C1 or frozen authority artifact."""

    fixtures = v28_fixtures()
    assert verifier.V28_FROZEN_AUTHORITY_PATHS == fixtures.publisher.FROZEN_AUTHORITY_PATHS
    root, bootstrap, _publication, tip = fixtures.drift_repository(tmp_path, scenario)
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.validate_v28_scope(root, tip, bootstrap)
    assert failure.value.code == "EVIDENCE_V28_GOVERNED_PATH_TOUCHED"


def test_v28_evidence_host_returns_record_invalid_for_nonobject_transaction(tmp_path: Path) -> None:
    """A malformed C2 record gets a stable evidence diagnostic."""

    fixtures = v28_fixtures()
    root, bootstrap = fixtures.bootstrap_repository(tmp_path)
    target = root / fixtures.publisher.RECORD_PATH
    target.write_text('{"bootstrapTransaction":[],"authorization":{}}\n', encoding="utf-8")
    fixtures.git(root, "add", "--", fixtures.publisher.RECORD_PATH)
    malformed = fixtures.commit(root, "test(planning): publish malformed V28 record")
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.validate_v28_scope(root, malformed, bootstrap)
    assert failure.value.code == "EVIDENCE_V28_RECORD_INVALID"
    assert failure.value.state == "FAIL"


def test_v28_evidence_host_rejects_falsified_pass_diff_digest(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch,
) -> None:
    """This host recomputes the changed blob digest after publisher execution."""

    fixtures = v28_fixtures()
    root, bootstrap, publication = fixtures.published_repository(tmp_path)
    module, schema_content = verifier.load_v28_publisher(root, bootstrap)
    honest = module.verify_revision(root, publication, bootstrap)

    class Fabricated:
        @staticmethod
        def verify_revision(repository: Path, candidate: str, trusted_host: str) -> dict[str, Any]:
            document = json.loads(json.dumps(honest))
            document["observed"]["changedPaths"][0]["sha256"] = "0" * 64
            return document

    monkeypatch.setattr(verifier, "load_v28_publisher", lambda repository, commit: (Fabricated, schema_content))
    with pytest.raises(verifier.BoundaryError) as failure:
        verifier.validate_v28_scope(root, publication, bootstrap)
    assert failure.value.code == "EVIDENCE_V28_OBSERVED_DIFF_UNTRUTHFUL"
