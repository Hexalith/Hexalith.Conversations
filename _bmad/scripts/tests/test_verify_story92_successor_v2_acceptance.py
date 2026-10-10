"""Candidate authority checks for the additive Story 9.2 successor route."""

from __future__ import annotations

import importlib.util
import json
import shutil
import subprocess
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
PATH = ROOT / "_bmad/scripts/verify_story92_successor_v2_acceptance.py"
SPEC = importlib.util.spec_from_file_location("story92_successor_v2_acceptance_test", PATH)
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
VERIFIER_SPEC = importlib.util.spec_from_file_location(
    "story92_successor_v2_verifier_test", ROOT / "_bmad/scripts/verify_conformance_tiering.py")
assert VERIFIER_SPEC and VERIFIER_SPEC.loader
VERIFIER = importlib.util.module_from_spec(VERIFIER_SPEC)
VERIFIER_SPEC.loader.exec_module(VERIFIER)


@pytest.fixture(scope="module")
def approved_source(tmp_path_factory: pytest.TempPathFactory) -> Path:
    checkout = tmp_path_factory.mktemp("story92-v2-authority") / "checkout"
    subprocess.run(["git", "-C", str(ROOT), "worktree", "add", "--detach", str(checkout), MODULE.BASE],
                   check=True, capture_output=True)
    try:
        paths = (MODULE.PREPARER.AUTH, MODULE.PREPARER.SCOPE, MODULE.PREPARER.API_EVIDENCE,
                 MODULE.PREPARER.API_COMPAT_EVIDENCE, MODULE.PROPOSAL, MODULE.REVIEW, MODULE.APPROVAL)
        for relative in paths:
            destination = checkout / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, destination)
        yield checkout
    finally:
        subprocess.run(["git", "-C", str(ROOT), "worktree", "remove", "--force", str(checkout)],
                       check=True, capture_output=True)


def test_exact_v2_scope_and_v5_quality_material_pass_preparation_audit(approved_source: Path) -> None:
    binding = MODULE.validate(approved_source, MODULE.BASE, committed=False)
    assert binding["approvedSource"] == MODULE.BASE
    assert binding["proposalSha256"] == "3e1c8e6a4dfb4358e2797d8a3bfb84d652d330ed28924b8b916995613232bb9a"
    assert binding["approval"] == {"path": MODULE.APPROVAL, "sha256": MODULE.APPROVAL_SHA}
    assert binding["committed"] is False


def test_uncommitted_v5_decision_cannot_authorize_candidate(approved_source: Path) -> None:
    with pytest.raises(MODULE.AcceptanceError) as error:
        MODULE.validate(approved_source, MODULE.BASE)
    assert error.value.code == "SUCCESSOR_INPUT_NOT_COMMITTED"


def test_later_root_gitlink_promotions_reject_v2_route(tmp_path: Path) -> None:
    # Pin the known later history so this remains valid in an ec5-based candidate.
    later = "fbe2f502eed26df45edc12e4a12e9917bf97b438"
    checkout = tmp_path / "later"
    subprocess.run(["git", "-C", str(ROOT), "worktree", "add", "--detach", str(checkout), later],
                   check=True, capture_output=True)
    try:
        for relative in (MODULE.PREPARER.AUTH, MODULE.PREPARER.SCOPE,
                         MODULE.PREPARER.API_EVIDENCE, MODULE.PREPARER.API_COMPAT_EVIDENCE,
                         MODULE.PROPOSAL, MODULE.REVIEW, MODULE.APPROVAL):
            destination = checkout / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, destination)
        with pytest.raises(MODULE.AcceptanceError) as error:
            MODULE.validate(checkout, later, committed=False)
        assert error.value.code == "SUCCESSOR_SCOPE_DRIFT"
        assert str(error.value) == "root gitlinks changed after the v2 authorized source"
    finally:
        subprocess.run(["git", "-C", str(ROOT), "worktree", "remove", "--force", str(checkout)],
                       check=True, capture_output=True)


def test_changed_approval_bytes_reject_before_candidate_checks(approved_source: Path) -> None:
    target = approved_source / MODULE.APPROVAL
    original = target.read_bytes()
    try:
        target.write_bytes(original + b" ")
        with pytest.raises(MODULE.AcceptanceError) as error:
            MODULE.validate(approved_source, MODULE.BASE, committed=False)
        assert error.value.code == "SUCCESSOR_INPUT_DRIFT"
    finally:
        target.write_bytes(original)


def test_v2_environment_schema_is_additive() -> None:
    schema = json.loads((ROOT / "_bmad/schemas/story-final-record-v2.schema.json").read_bytes())
    validator = jsonschema.Draft202012Validator(schema)
    original = json.loads((ROOT / "docs/release-evidence/story-9.2-final-record-v2.json").read_bytes())
    assert not list(validator.iter_errors(original))
    environment = {
        "route": "story-9.2-successor-v2", "approvedSourceCommit": MODULE.BASE,
        "scopeAuthorization": {"path": MODULE.PREPARER.AUTH, "sha256": MODULE.PREPARER.AUTH_SHA},
        "scopeProposal": {"path": MODULE.PREPARER.SCOPE, "sha256": MODULE.PREPARER.SCOPE_SHA,
                          "proposalSha256": MODULE.PREPARER.SCOPE_MATERIAL_SHA},
        "qualityApproval": {"path": MODULE.APPROVAL, "sha256": MODULE.APPROVAL_SHA},
        "rootGitlinks": MODULE.root_gitlinks(ROOT, MODULE.BASE),
    }
    candidate_environment = validator.evolve(schema={"$defs": schema["$defs"],
                                                    "$ref": "#/$defs/candidateEnvironmentV2"})
    assert not list(candidate_environment.iter_errors(environment))
    environment["scopeProposal"]["proposalSha256"] = "0" * 64
    assert list(candidate_environment.iter_errors(environment))


def test_fault_source_digest_includes_new_authority_code_and_schema() -> None:
    paths = VERIFIER.fault_source_paths(ROOT)
    assert "_bmad/scripts/verify_story92_successor_v2_acceptance.py" in paths
    assert "_bmad/scripts/prepare_story92_successor_v2.py" in paths
    assert "_bmad/schemas/story-9.2-current-main-successor-proposal-v2.schema.json" in paths
    assert MODULE.APPROVAL in paths


def test_v5_authority_helper_byte_change_invalidates_fault_receipt(tmp_path: Path) -> None:
    paths = VERIFIER.fault_source_paths(ROOT)
    for relative in paths:
        destination = tmp_path / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, destination)
    original = VERIFIER.fault_source_digest(tmp_path)
    assert original == VERIFIER.fault_source_digest(ROOT)
    helper = tmp_path / "_bmad/scripts/verify_story92_successor_v2_acceptance.py"
    helper.write_bytes(helper.read_bytes() + b"# fault probe\n")
    assert VERIFIER.fault_source_digest(tmp_path) != original
