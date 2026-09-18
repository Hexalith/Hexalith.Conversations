from __future__ import annotations

import copy
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import types
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "_bmad/scripts/generate_preservation_traceability_manifest.py"
RC2_SCRIPT = ROOT / "_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py"


def load_module():
    spec = importlib.util.spec_from_file_location("preservation_manifest", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def load_rc2_module():
    spec = importlib.util.spec_from_file_location("preservation_manifest_rc2_faults", RC2_SCRIPT)
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


def test_check_mode_reports_current_authority_drift_without_rewriting_v2_history():
    protected = (
        ROOT / generator_path
        for generator_path in (
            "docs/release-evidence/preservation-traceability-manifest-v2.json",
            "docs/release-evidence/preservation-non-activation-disposition-v2.json",
            "docs/release-evidence/preservation-traceability-manifest-v2.md",
        )
    )
    before = {path: path.read_bytes() for path in protected}
    completed = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--allow-pending-operator"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
    )
    document = json.loads(completed.stdout)
    codes = {item["code"] for item in document["diagnostics"]}

    assert completed.returncode == 1, completed.stdout + completed.stderr
    assert document["result"] == "fail"
    assert {
        "GENERATED_JSON_DRIFT",
        "GENERATED_DISPOSITION_DRIFT",
        "PROJECTION_DRIFT",
    } <= codes
    assert {path: path.read_bytes() for path in before} == before


def test_rc2_path_validation_rejects_real_symlink_fifo_directory_and_gitlink_modes(tmp_path, monkeypatch):
    rc2 = load_rc2_module()
    repository = tmp_path / "repository"
    repository.mkdir()
    subprocess.run(["git", "init", "--quiet"], cwd=repository, check=True)
    subprocess.run(["git", "config", "user.name", "Fixture"], cwd=repository, check=True)
    subprocess.run(["git", "config", "user.email", "fixture@example.invalid"], cwd=repository, check=True)
    regular = repository / "regular.txt"
    regular.write_text("unchanged", encoding="utf-8")
    subprocess.run(["git", "add", "regular.txt"], cwd=repository, check=True)
    subprocess.run(["git", "commit", "--quiet", "-m", "fixture: regular"], cwd=repository, check=True)
    commit = subprocess.run(["git", "rev-parse", "HEAD"], cwd=repository, check=True, capture_output=True, text=True).stdout.strip()
    monkeypatch.setattr(rc2, "ROOT", repository)
    monkeypatch.setattr(rc2, "ROOT_RESOLVED", repository.resolve())
    monkeypatch.setattr(rc2, "BASE_COMMIT", commit)
    monkeypatch.setattr(rc2, "SOURCE_INPUT_PATHS", ["regular.txt"])
    monkeypatch.setattr(rc2, "EXPECTED_NEW_SOURCE_INPUT_PATHS", frozenset())
    assert rc2.validate_repository_path(regular, require_file=True, purpose="fault fixture") == "regular.txt"
    assert rc2.source_binding(regular)["mode"] == "100644"
    original_mode = regular.stat().st_mode & 0o777
    original_bytes = regular.read_bytes()
    try:
        regular.chmod(original_mode | 0o111)
        with pytest.raises(ValueError, match="worktree mode mismatch"):
            rc2.source_binding(regular)
    finally:
        regular.chmod(original_mode)
    assert regular.read_text(encoding="utf-8") == "unchanged"

    original_stage = subprocess.run(
        ["git", "ls-files", "--stage", "--", "regular.txt"],
        cwd=repository,
        check=True,
        capture_output=True,
    ).stdout
    original_digest = rc2.candidate_digest(rc2.source_bindings())
    try:
        subprocess.run(
            ["git", "update-index", "--chmod=+x", "--", "regular.txt"], cwd=repository, check=True
        )
        assert rc2.worktree_git_mode(regular) == "100644"
        with pytest.raises(ValueError, match="Source input index mode mismatch.*stage=100755"):
            rc2.candidate_digest(rc2.source_bindings())
    finally:
        subprocess.run(
            ["git", "update-index", "--chmod=-x", "--", "regular.txt"], cwd=repository, check=True
        )
        regular.write_bytes(original_bytes)
        regular.chmod(original_mode)

    assert subprocess.run(
        ["git", "ls-files", "--stage", "--", "regular.txt"],
        cwd=repository,
        check=True,
        capture_output=True,
    ).stdout == original_stage
    assert regular.read_bytes() == original_bytes
    assert rc2.worktree_git_mode(regular) == "100644"
    assert rc2.candidate_digest(rc2.source_bindings()) == original_digest

    try:
        subprocess.run(["git", "rm", "--cached", "--", "regular.txt"], cwd=repository, check=True)
        regular.write_bytes(original_bytes)
        regular.chmod(original_mode)
        with pytest.raises(ValueError, match="index entry missing for baseline-present path"):
            rc2.candidate_digest(rc2.source_bindings())
    finally:
        regular.write_bytes(original_bytes)
        regular.chmod(original_mode)
        subprocess.run(["git", "add", "--", "regular.txt"], cwd=repository, check=True)

    assert subprocess.run(
        ["git", "ls-files", "--stage", "--", "regular.txt"],
        cwd=repository,
        check=True,
        capture_output=True,
    ).stdout == original_stage
    assert regular.read_bytes() == original_bytes
    assert rc2.worktree_git_mode(regular) == "100644"
    assert rc2.candidate_digest(rc2.source_bindings()) == original_digest

    new_source = repository / "new-source.txt"
    new_source.write_text("new overlay input", encoding="utf-8")
    monkeypatch.setattr(rc2, "SOURCE_INPUT_PATHS", ["new-source.txt"])
    monkeypatch.setattr(rc2, "EXPECTED_NEW_SOURCE_INPUT_PATHS", frozenset({"new-source.txt"}))
    assert rc2.candidate_digest(rc2.source_bindings())

    symlink = repository / "link.txt"
    symlink.symlink_to(regular)
    with pytest.raises(ValueError, match="symlink"):
        rc2.validate_repository_path(symlink, require_file=True, purpose="fault fixture")
    subprocess.run(["git", "add", "link.txt"], cwd=repository, check=True)
    subprocess.run(["git", "commit", "--quiet", "-m", "fixture: symlink"], cwd=repository, check=True)
    assert subprocess.run(
        ["git", "ls-tree", "HEAD", "--", "link.txt"], cwd=repository, check=True, capture_output=True, text=True
    ).stdout.startswith("120000 ")
    symlink.unlink()
    symlink.write_text("regular worktree bytes", encoding="utf-8")
    with pytest.raises(ValueError, match="Git mode.*120000"):
        rc2.validate_repository_path(symlink, require_file=True, purpose="fault fixture")

    directory = repository / "directory"
    directory.mkdir()
    with pytest.raises(ValueError, match="non-regular"):
        rc2.validate_repository_path(directory, require_file=True, purpose="fault fixture")

    if hasattr(os, "mkfifo"):
        fifo = repository / "fifo"
        os.mkfifo(fifo)
        with pytest.raises(ValueError, match="non-regular"):
            rc2.validate_repository_path(fifo, require_file=True, purpose="fault fixture")

    subprocess.run(["git", "update-index", "--add", "--cacheinfo", f"160000,{commit},nested"], cwd=repository, check=True)
    subprocess.run(["git", "commit", "--quiet", "-m", "fixture: gitlink"], cwd=repository, check=True)
    assert subprocess.run(
        ["git", "ls-tree", "HEAD", "--", "nested"], cwd=repository, check=True, capture_output=True, text=True
    ).stdout.startswith("160000 ")
    nested = repository / "nested"
    nested.write_text("regular worktree bytes", encoding="utf-8")
    with pytest.raises(ValueError, match="Git mode.*160000"):
        rc2.validate_repository_path(nested, require_file=True, purpose="fault fixture")
    assert regular.read_text(encoding="utf-8") == "unchanged"


def test_rc2_runner_rejects_dll_replacement_between_pre_and_post_observation(tmp_path, monkeypatch):
    rc2 = load_rc2_module()
    candidate = "a" * 64
    repository = tmp_path / "repository"
    assembly = repository / "assembly.dll"
    manifest = repository / "manifest.json"
    repository.mkdir()
    subprocess.run(["git", "init", "--quiet"], cwd=repository, check=True)
    assembly.write_bytes(b"assembly:" + candidate.encode())
    manifest.write_text("{}", encoding="utf-8")
    monkeypatch.setattr(rc2, "ROOT", repository)
    monkeypatch.setattr(rc2, "ROOT_RESOLVED", repository.resolve())
    monkeypatch.setattr(rc2, "ASSEMBLY_PATH", assembly)
    monkeypatch.setattr(rc2, "RC2_JSON", manifest)
    monkeypatch.setattr(rc2, "EVIDENCE_ROOT", repository / "evidence")
    monkeypatch.setattr(rc2, "validate_changed_path_boundary", lambda _required: None)
    before = rc2.assembly_binding(candidate)
    monkeypatch.setattr(rc2, "validated_build_evidence", lambda _digest: {"assembly": before})
    original_run = rc2.subprocess.run

    def fake_run(command, **kwargs):
        if command[:2] != ["dotnet", "exec"]:
            return original_run(command, **kwargs)
        assembly.write_bytes(assembly.read_bytes() + b":replacement")
        Path(command[-1]).write_text("runner output", encoding="utf-8")
        return types.SimpleNamespace(returncode=0)

    monkeypatch.setattr(rc2.subprocess, "run", fake_run)
    with pytest.raises(ValueError, match="assembly changed"):
        rc2.run_conformance(candidate)


def test_rc2_run_build_orders_restore_build_and_assembly_and_short_circuits_on_restore_failure(monkeypatch):
    rc2 = load_rc2_module()
    candidate = "a" * 64
    events = []
    restore_receipt = {
        "restore": {
            "command": ["dotnet", "restore", "fixture.csproj"],
            "exitCode": 0,
            "result": "pass",
        }
    }

    monkeypatch.setattr(rc2, "validate_changed_path_boundary", lambda _required: None)
    monkeypatch.setattr(
        rc2,
        "validated_restore_evidence",
        lambda _digest: events.append("restore-validation") or restore_receipt,
    )
    monkeypatch.setattr(
        rc2,
        "binding",
        lambda path, role: {"path": Path(path).name, "sha256": "b" * 64, "bytes": 1, "role": role},
    )
    monkeypatch.setattr(rc2, "build_command", lambda _digest: ["dotnet", "build", "fixture.csproj"])

    def run_build_process(command, **_kwargs):
        assert command == ["dotnet", "build", "fixture.csproj"]
        events.append("subprocess-build")
        return types.SimpleNamespace(returncode=0, stdout=b"controlled build output")

    monkeypatch.setattr(rc2.subprocess, "run", run_build_process)
    monkeypatch.setattr(rc2, "atomic_write", lambda _path, _data: None)
    monkeypatch.setattr(rc2, "parse_build_log", lambda: {"result": "pass", "warnings": 0, "errors": 0})
    monkeypatch.setattr(
        rc2,
        "assembly_binding",
        lambda _digest: events.append("assembly-validation")
        or {"path": "fixture.dll", "sha256": "c" * 64, "bytes": 1, "sourceRevisionId": candidate},
    )

    assert rc2.run_build(candidate) == 0
    assert events == ["restore-validation", "subprocess-build", "assembly-validation"]

    events.clear()

    def reject_restore(_digest):
        events.append("restore-validation")
        raise ValueError("restore validation rejected")

    monkeypatch.setattr(rc2, "validated_restore_evidence", reject_restore)
    with pytest.raises(ValueError, match="restore validation rejected"):
        rc2.run_build(candidate)
    assert events == ["restore-validation"]


def test_rc2_receipt_rejects_semantic_xml_drift_even_with_recomputed_hash(tmp_path, monkeypatch):
    rc2 = load_rc2_module()
    import xml.etree.ElementTree as element_tree

    xml_path = tmp_path / "result.xml"
    xml_path.write_bytes(rc2.XML_PATH.read_bytes())
    tree = element_tree.parse(xml_path)
    assembly_element = tree.getroot().find("assembly")
    assert assembly_element is not None
    first_test = next(assembly_element.iter("test"))
    assert first_test.attrib["result"] == "Pass"
    first_test.attrib["result"] = "Fail"
    assembly_element.attrib["passed"] = str(int(assembly_element.attrib["passed"]) - 1)
    assembly_element.attrib["failed"] = str(int(assembly_element.attrib["failed"]) + 1)
    tree.write(xml_path, encoding="utf-8", xml_declaration=True)
    receipt_path = tmp_path / "receipt.json"
    receipt = json.loads(rc2.RUN_RECEIPT_PATH.read_text(encoding="utf-8"))
    candidate = receipt["candidateDigest"]
    assembly = receipt["assembly"]
    receipt["xml"] = {
        "path": xml_path.relative_to(Path("/")).as_posix(),
        "sha256": rc2.sha256_bytes(xml_path.read_bytes()),
        "bytes": xml_path.stat().st_size,
    }
    receipt_path.write_bytes(rc2.canonical_json(receipt))
    v3 = rc2.load_v3_generator()
    monkeypatch.setattr(rc2, "ROOT", Path("/"))
    monkeypatch.setattr(rc2, "ROOT_RESOLVED", Path("/"))
    monkeypatch.setattr(rc2, "RC2_JSON", Path("/") / receipt["preRunManifest"]["path"])
    monkeypatch.setattr(rc2, "RUN_RECEIPT_PATH", receipt_path)
    monkeypatch.setattr(rc2, "XML_PATH", xml_path)
    monkeypatch.setattr(rc2, "validate_repository_path", lambda path, **_kwargs: Path(path).name)
    with pytest.raises(ValueError, match="rows/counters"):
        rc2.validated_receipt(v3, candidate, assembly, require_current_manifest=False)


def test_rc2_restore_build_and_detached_faults_fail_closed_and_restore_bytes():
    rc2 = load_rc2_module()
    candidate = rc2.candidate_digest(rc2.source_bindings())
    protected = [
        rc2.TOOLCHAIN_PATH,
        rc2.RESTORE_INVENTORY_PATH,
        rc2.RESTORE_RECEIPT_PATH,
        rc2.BUILD_LOG_PATH,
        rc2.BUILD_RECEIPT_PATH,
        rc2.SEMANTIC_RESULTS_PATH,
        rc2.DETACHED_INDEX_PATH,
        rc2.DETACHED_DIGEST_PATH,
    ]
    before = {path: path.read_bytes() for path in protected}

    def restore(path):
        path.write_bytes(before[path])

    try:
        restore_receipt = json.loads(before[rc2.RESTORE_RECEIPT_PATH])
        mutated = copy.deepcopy(restore_receipt)
        mutated["restore"]["command"][0] = "mutated-dotnet"
        rc2.RESTORE_RECEIPT_PATH.write_bytes(rc2.canonical_json(mutated))
        with pytest.raises(ValueError, match="restore command/exit/result"):
            rc2.validated_restore_evidence(candidate)
        restore(rc2.RESTORE_RECEIPT_PATH)

        mutated = copy.deepcopy(restore_receipt)
        mutated["restore"]["exitCode"] = 1
        rc2.RESTORE_RECEIPT_PATH.write_bytes(rc2.canonical_json(mutated))
        with pytest.raises(ValueError, match="restore command/exit/result"):
            rc2.validated_restore_evidence(candidate)
        restore(rc2.RESTORE_RECEIPT_PATH)

        rc2.TOOLCHAIN_PATH.write_bytes(before[rc2.TOOLCHAIN_PATH].replace(b"10.0.401", b"10.0.999", 1))
        with pytest.raises(ValueError, match="Candidate SDK mismatch"):
            rc2.validated_restore_evidence(candidate)
        restore(rc2.TOOLCHAIN_PATH)

        inventory = json.loads(before[rc2.RESTORE_INVENTORY_PATH])
        inventory["projects"][0]["sha256"] = "0" * 64
        rc2.RESTORE_INVENTORY_PATH.write_bytes(rc2.canonical_json(inventory))
        with pytest.raises(ValueError, match="dependency inventory drift"):
            rc2.validated_restore_evidence(candidate)
        restore(rc2.RESTORE_INVENTORY_PATH)

        build_receipt = json.loads(before[rc2.BUILD_RECEIPT_PATH])
        build_mutations = [
            (lambda value: value["sequence"].reverse(), "restore/build order"),
            (lambda value: value["build"]["command"].__setitem__(0, "mutated-dotnet"), "build receipt command"),
            (lambda value: value["build"].__setitem__("exitCode", 1), "exit/result"),
            (lambda value: value["preBuild"]["restoreReceipt"].__setitem__("sha256", "0" * 64), "pre-build restore/toolchain"),
        ]
        for mutate, diagnostic in build_mutations:
            mutated = copy.deepcopy(build_receipt)
            mutate(mutated)
            rc2.BUILD_RECEIPT_PATH.write_bytes(rc2.canonical_json(mutated))
            with pytest.raises(ValueError, match=diagnostic):
                rc2.validated_build_evidence(candidate)
            restore(rc2.BUILD_RECEIPT_PATH)

        rc2.BUILD_LOG_PATH.write_bytes(before[rc2.BUILD_LOG_PATH] + b"\nmutated\n")
        with pytest.raises(ValueError, match="build receipt log binding"):
            rc2.validated_build_evidence(candidate)
        restore(rc2.BUILD_LOG_PATH)

        rc2.DETACHED_INDEX_PATH.write_bytes(before[rc2.DETACHED_INDEX_PATH] + b"\n")
        completed = subprocess.run(
            [sys.executable, str(RC2_SCRIPT), "--check", "--require-green"],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
        assert completed.returncode == 1
        assert rc2.DETACHED_INDEX_PATH.relative_to(ROOT).as_posix() in completed.stdout
        restore(rc2.DETACHED_INDEX_PATH)

        rc2.SEMANTIC_RESULTS_PATH.write_bytes(before[rc2.SEMANTIC_RESULTS_PATH] + b"\n")
        completed = subprocess.run(
            [sys.executable, str(RC2_SCRIPT), "--check", "--require-green"],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
        assert completed.returncode != 0
        assert "Final manifest/core evidence must already be byte-exact" in completed.stderr
        assert rc2.SEMANTIC_RESULTS_PATH.relative_to(ROOT).as_posix() in completed.stderr
    finally:
        for path, data in before.items():
            path.write_bytes(data)

    assert {path: path.read_bytes() for path in protected} == before
