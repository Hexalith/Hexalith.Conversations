"""Fault-injection tests for the V12 evidence-boundary gate."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import subprocess

import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/verify_evidence_boundary.py"
SPEC = importlib.util.spec_from_file_location("verify_evidence_boundary", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)


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
    assert document["changedPaths"] == ["README.md"]
    assert document["assertionLedger"]
    assert all(row["state"] == "PASS" for row in document["assertionLedger"])
