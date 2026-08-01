from __future__ import annotations

import copy
import importlib.util
import json
import shutil
import subprocess
import sys
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "_bmad/scripts/generate_preservation_traceability_manifest.py"


def load_module():
    spec = importlib.util.spec_from_file_location("preservation_manifest", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


@pytest.fixture(scope="module")
def generator():
    return load_module()


@pytest.fixture(scope="module")
def generated(generator):
    return generator.generate_outputs(ROOT)


def diagnostic_codes(generator, manifest, disposition, *, strict=False, markdown=None, root=ROOT):
    return {
        diagnostic.code
        for diagnostic in generator.validate_outputs(
            root,
            manifest,
            disposition,
            strict=strict,
            markdown=markdown,
        )
    }


def first_obligation(manifest, kind):
    return next(row for row in manifest["obligations"] if row["kind"] == kind)


def completed_tiering_candidate(generator, tmp_path):
    candidate_root = tmp_path / "candidate"
    ignored = shutil.ignore_patterns("bin", "obj", ".pytest_cache", "TestResults")
    for directory in ("src", "tests", "docs", "_bmad-output"):
        shutil.copytree(ROOT / directory, candidate_root / directory, copy_function=shutil.copy2, ignore=ignored)
    for filename in ("Hexalith.Conversations.slnx", "Directory.Packages.props", "global.json"):
        destination = candidate_root / filename
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / filename, destination)

    mutable_paths = (
        generator.TIER_DECISION_PATH,
        generator.MANIFEST_PATH,
        generator.DISPOSITION_PATH,
        generator.MARKDOWN_PATH,
    )
    for relative in mutable_paths:
        path = candidate_root / relative
        if path.exists():
            path.unlink()

    assertions = generator.extract_inventory(candidate_root)["conformance-assertion"]
    structural_path = Path(
        "tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs"
    )
    triage = {
        "assertions": [
            {
                "assertionId": row["id"],
                "tier": "portable" if ordinal % 2 == 0 else "module-internal",
            }
            for ordinal, row in enumerate(assertions)
        ]
    }
    decision = json.loads((ROOT / generator.TIER_DECISION_PATH).read_text(encoding="utf-8"))
    decision["triageResults"] = triage
    decision["portableStructuralEvidence"] = {
        "path": structural_path.as_posix(),
        "sha256": generator.sha256_file(candidate_root / structural_path),
        "result": "pass",
        "assertion": "resolved-compile-surface-has-no-nonpackable-module-binding",
    }
    decision_path = candidate_root / generator.TIER_DECISION_PATH
    decision_path.write_text(generator.canonical_json(decision), encoding="utf-8", newline="\n")

    manifest, disposition, markdown = generator.generate_outputs(candidate_root)
    assert manifest["tiering"]["triageStatus"] == "triaged"
    assert manifest["tiering"]["triageSha256"] == generator.sha256_text(generator.compact_json(triage))
    assert manifest["tiering"]["decisionSha256"] == generator.sha256_file(decision_path)
    return candidate_root, manifest, disposition, markdown


def approve_candidate(generator, candidate_root, manifest, disposition, markdown):
    generator.write_outputs(candidate_root, manifest, disposition, markdown)
    approval_path = Path("docs/release-evidence/test-fixtures/story-6-3-strict-approval.json")
    full_approval_path = candidate_root / approval_path
    full_approval_path.parent.mkdir(parents=True, exist_ok=True)
    full_approval_path.write_text(
        generator.canonical_json(
            {
                "artifact": "story-6-3-strict-approval-test-fixture",
                "approver": "release-owner",
                "scope": "Every exact candidate disposition and the initial v2 mutation.",
                "status": "approved",
            }
        ),
        encoding="utf-8",
        newline="\n",
    )
    approval_evidence = {
        "path": approval_path.as_posix(),
        "sha256": generator.sha256_file(full_approval_path),
    }
    disposition["status"] = "approved"
    for decision in disposition["decisions"]:
        decision["decisionDate"] = "2026-08-01"
        decision["status"] = "approved"
        decision["approvalEvidence"] = copy.deepcopy(approval_evidence)

    approved_manifest = generator.build_manifest(
        candidate_root,
        disposition,
        manifest["obligations"],
        "triaged",
    )
    approved_manifest["mutationGovernance"]["status"] = "approved"
    approved_manifest["mutationGovernance"]["approvalEvidence"] = approval_evidence
    approved_manifest["status"] = "release-gated"
    approved_markdown = generator.render_markdown(approved_manifest)
    generator.write_outputs(candidate_root, approved_manifest, disposition, approved_markdown)
    return approved_manifest, disposition, approved_markdown


def test_extracts_every_authoritative_denominator_exactly_once(generator):
    inventory = generator.extract_inventory(ROOT)

    assert len(inventory["initiative-fr"]) == 20
    assert len(inventory["feature-fr"]) == 104
    assert len(inventory["feature-nfr"]) == 77
    assert len(inventory["ux-decision"]) == 52
    assert len(inventory["ux-acceptance"]) == 52

    for expected_kind, expected_prefix, expected_count in (
        ("initiative-fr", "FR-", 20),
        ("feature-fr", "Feature-FR", 104),
        ("feature-nfr", "Feature-NFR", 77),
        ("ux-decision", "UX-DR", 52),
    ):
        ids = [row["id"] for row in inventory[expected_kind]]
        assert ids == [f"{expected_prefix}{ordinal}" for ordinal in range(1, expected_count + 1)]


def test_ux_acceptance_ids_bind_section_ordinal_and_text_hash(generator):
    rows = generator.extract_inventory(ROOT)["ux-acceptance"]
    assert {row["source"]["section"] for row in rows} == {
        "Design System Acceptance Criteria",
        "2.3 Success Criteria",
        "Safety Acceptance Criteria",
        "Responsive Acceptance Criteria",
    }

    for row in rows:
        source = row["source"]
        assert row["id"].endswith(source["textSha256"][:12])
        assert f"-{source['ordinal']:02d}-" in row["id"]
        assert len(source["textSha256"]) == 64


def test_manifest_is_deterministic_closed_and_structurally_valid(generator, generated):
    manifest, disposition, markdown = generated
    second_manifest, second_disposition, second_markdown = generator.generate_outputs(ROOT)

    assert generator.canonical_json(manifest) == generator.canonical_json(second_manifest)
    assert generator.canonical_json(disposition) == generator.canonical_json(second_disposition)
    assert markdown == second_markdown
    assert diagnostic_codes(generator, manifest, disposition, markdown=markdown) == set()
    assert manifest["schemaVersion"] == 2
    assert manifest["supersession"]["v1MutationAllowed"] is False
    assert manifest["summaries"]
    assert all(summary["expected"] > 0 for summary in manifest["summaries"])


@pytest.mark.parametrize(
    ("mutation", "expected_code"),
    [
        (lambda m: m["obligations"].pop(), "DENOMINATOR_GAP"),
        (lambda m: m["obligations"].append(copy.deepcopy(m["obligations"][0])), "DUPLICATE_OBLIGATION"),
        (lambda m: m["obligations"].clear(), "EMPTY_INVENTORY"),
    ],
)
def test_denominator_faults_fail_closed(generator, generated, mutation, expected_code):
    manifest, disposition, _ = copy.deepcopy(generated)
    mutation(manifest)
    assert expected_code in diagnostic_codes(generator, manifest, disposition)


def test_unknown_obligation_fails_closed(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    manifest["obligations"][0]["id"] = "FR-UNKNOWN"
    assert "UNKNOWN_OBLIGATION" in diagnostic_codes(generator, manifest, disposition)


def test_source_hash_and_path_faults_fail_closed(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    manifest["obligations"][0]["source"]["textSha256"] = "0" * 64
    assert "SOURCE_TEXT_HASH_MISMATCH" in diagnostic_codes(generator, manifest, disposition)

    manifest, disposition, _ = copy.deepcopy(generated)
    manifest["sourceBindings"][0]["path"] = "../outside"
    assert "PATH_ESCAPE" in diagnostic_codes(generator, manifest, disposition)


def test_evidence_hash_staleness_and_self_attestation_fail_closed(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    row = first_obligation(manifest, "current-control")
    row["closure"]["evidence"][0]["sha256"] = "0" * 64
    assert "EVIDENCE_HASH_MISMATCH" in diagnostic_codes(generator, manifest, disposition)

    manifest, disposition, _ = copy.deepcopy(generated)
    row = first_obligation(manifest, "current-control")
    row["closure"]["evidence"][0]["authoritySha256"] = "0" * 64
    assert "EVIDENCE_STALE" in diagnostic_codes(generator, manifest, disposition)

    manifest, disposition, _ = copy.deepcopy(generated)
    row = first_obligation(manifest, "current-control")
    row["closure"]["evidence"][0]["path"] = row["source"]["path"]
    row["closure"]["evidence"][0]["sha256"] = row["source"]["fileSha256"]
    assert "SELF_ATTESTED_EVIDENCE" in diagnostic_codes(generator, manifest, disposition)


def test_control_ownership_reversal_fails_closed(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    control = next(row for row in manifest["obligations"] if row["id"] == "CTRL-CANONICAL-HOST-SHAPE")
    control["controlOwner"] = "module"
    assert "CONTROL_OWNERSHIP_REVERSAL" in diagnostic_codes(generator, manifest, disposition)


def test_pending_and_compatibility_governance_fail_strictly(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    assert "APPROVAL_PENDING" in diagnostic_codes(generator, manifest, disposition, strict=True)

    entry = disposition["decisions"][0]
    entry["proposedClosure"] = "delivered-to-inactive"
    entry["compatibilityEvidence"] = []
    assert "COMPATIBILITY_EVIDENCE_REQUIRED" in diagnostic_codes(generator, manifest, disposition)


def test_tiering_is_complete_only_after_exact_story_6_9_triage(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    assertions = [row for row in manifest["obligations"] if row["kind"] == "conformance-assertion"]
    assert assertions
    assert all(row["tier"] == "pending-story-6.9" for row in assertions)
    assert "TIERING_INCOMPLETE" in diagnostic_codes(generator, manifest, disposition, strict=True)

    assertions[0].pop("tier")
    assert "TIER_REQUIRED" in diagnostic_codes(generator, manifest, disposition)


def test_completed_tiering_with_only_human_approval_pending_has_exact_strict_diagnostic(generator, tmp_path):
    candidate_root, manifest, disposition, markdown = completed_tiering_candidate(generator, tmp_path)

    assert manifest["status"] == "pending-operator"
    assert diagnostic_codes(
        generator,
        manifest,
        disposition,
        strict=True,
        markdown=markdown,
        root=candidate_root,
    ) == {"APPROVAL_PENDING"}


def test_fully_approved_and_triaged_candidate_passes_strict_check(generator, tmp_path):
    candidate_root, manifest, disposition, markdown = completed_tiering_candidate(generator, tmp_path)
    manifest, disposition, markdown = approve_candidate(
        generator,
        candidate_root,
        manifest,
        disposition,
        markdown,
    )

    assert diagnostic_codes(
        generator,
        manifest,
        disposition,
        strict=True,
        markdown=markdown,
        root=candidate_root,
    ) == set()
    completed = subprocess.run(
        [sys.executable, str(SCRIPT), "--repository", str(candidate_root), "--check"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    assert completed.returncode == 0, completed.stdout + completed.stderr
    summary = json.loads(completed.stdout)
    assert summary == {
        "artifact": "preservation-traceability-manifest-v2-validation",
        "mode": "strict",
        "result": "pass",
        "diagnostics": [],
    }


def test_immutable_v1_root_of_trust_cannot_be_redeclared(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    manifest["immutableV1Bindings"][0]["sha256"] = "0" * 64
    assert "IMMUTABLE_V1_MISMATCH" in diagnostic_codes(generator, manifest, disposition)


def test_markdown_projection_drift_fails_closed(generator, generated):
    manifest, disposition, markdown = generated
    assert "PROJECTION_DRIFT" in diagnostic_codes(
        generator,
        manifest,
        disposition,
        markdown=markdown + "\nmutated\n",
    )


def test_schema_rejects_unknown_properties(generator, generated):
    manifest, disposition, _ = copy.deepcopy(generated)
    manifest["unexpected"] = True
    assert "SCHEMA_CLOSED_VOCABULARY" in diagnostic_codes(generator, manifest, disposition)


def test_check_mode_matches_committed_outputs_after_generation():
    completed = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--allow-pending-operator"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    assert completed.returncode == 0, completed.stdout + completed.stderr
    summary = json.loads(completed.stdout)
    assert summary["result"] == "pass"
    assert summary["mode"] == "structural"
