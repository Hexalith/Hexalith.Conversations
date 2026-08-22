"""Fault-injection tests for the V14 evidence-boundary gate."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import subprocess

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/verify_evidence_boundary.py"
SPEC = importlib.util.spec_from_file_location("verify_evidence_boundary", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)


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

    assert len(verifier.ACTIVE_ROUTE_PATHS) == 12
    assert len(ledger) == 12
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
    monkeypatch.setattr(verifier, "validate_active_routes", lambda _root: [])
    monkeypatch.setattr(
        verifier,
        "validate_context",
        lambda _root: verifier.assertion("CONTEXT-01", "fixture", "PASS"),
    )
    monkeypatch.setattr(verifier, "validate_context_workflows", lambda _root: [])
    monkeypatch.setattr(
        verifier,
        "validate_csharp_signature_guard",
        lambda _root: verifier.assertion("SIGNATURE-01", "fixture", "PASS"),
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
