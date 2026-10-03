"""Story 8.1 disposition generation and fault fixtures."""

from __future__ import annotations

import importlib.util
import contextlib
import io
import json
import os
import re
import subprocess
import sys
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
CANDIDATE = subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "HEAD"]).decode().strip()
SCRIPT = ROOT / "_bmad/scripts/generate_ux_preservation_disposition.py"
specification = importlib.util.spec_from_file_location("ux_disposition", SCRIPT)
assert specification is not None and specification.loader is not None
module = importlib.util.module_from_spec(specification)
specification.loader.exec_module(module)


@pytest.fixture
def source_fixture(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> Path:
    """Copy authority inputs so mutations never touch canonical source bytes."""
    paths = (module.SPEC_PATH, module.MAP_PATH, module.CONTRACT_PATH,
             module.PREDECESSOR_PATH, module.PREDECESSOR_MARKDOWN_PATH,
             "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json")
    originals = {}
    for relative in paths:
        destination = tmp_path / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        content = (ROOT / relative).read_bytes()
        destination.write_bytes(content)
        originals[relative] = content

    def git_show(arguments: list[str], **kwargs: object) -> subprocess.CompletedProcess[bytes]:
        assert arguments[0] == "git" and "show" in arguments
        revision_path = arguments[arguments.index("show") + 1]
        return subprocess.CompletedProcess(arguments, 0, originals[revision_path.split(":", 1)[1]], b"")

    monkeypatch.setattr(subprocess, "run", git_show)
    return tmp_path


def _reject(root: Path, code: str) -> None:
    with pytest.raises(module.DispositionError) as failure:
        module.generate(root, module.CONTRACT_PATH)
    assert failure.value.code == code


def _mutate(root: Path, relative: str, old: bytes, new: bytes, code: str) -> None:
    path = root / relative
    before = path.read_bytes()
    assert old in before
    try:
        path.write_bytes(before.replace(old, new, 1))
        _reject(root, code)
    finally:
        path.write_bytes(before)
    assert path.read_bytes() == before
    module.generate(root, module.CONTRACT_PATH)


def test_deterministic_closed_bundle(source_fixture: Path) -> None:
    """Two derivations have byte-identical outputs and a bound Markdown digest."""
    first = module.generate(source_fixture, module.CONTRACT_PATH)
    second = module.generate(source_fixture, module.CONTRACT_PATH)
    assert first == second
    schema, authoritative, rendered = first
    document = json.loads(authoritative)
    assert json.loads(schema)["additionalProperties"] is False
    assert document["schemaVersion"] == module.SCHEMA_VERSION
    assert document["renderedMarkdownSha256"] == module.digest(rendered)
    assert len(document["decisions"]) == 52
    assert len(document["acceptanceCriteria"]) == 28


def test_committed_bundle_matches_fresh_derivation() -> None:
    """The published bundle is exactly the deterministic derivation of its sources."""
    expected = module.generate(ROOT, module.CONTRACT_PATH)
    paths = ("docs/release-evidence/ux-preservation-disposition-v1.schema.json",
             "docs/release-evidence/ux-preservation-disposition-v1.json",
             "docs/release-evidence/ux-preservation-disposition-v1.md")
    assert expected == tuple((ROOT / path).read_bytes() for path in paths)


def test_generator_source_unbound_and_changed_bytes(source_fixture: Path) -> None:
    """A missing source and a changed source byte fail independently."""
    path = source_fixture / module.SPEC_PATH
    before = path.read_bytes()
    try:
        path.unlink()
        _reject(source_fixture, "UX_SOURCE_UNBOUND")
    finally:
        path.write_bytes(before)
    assert path.read_bytes() == before
    _mutate(source_fixture, module.SPEC_PATH, b"# UX Design Specification", b"# UX Design Specification ", "UX_SOURCE_DRIFT")


def test_decision_inventory_drift(source_fixture: Path) -> None:
    """A missing decision does not silently reduce the denominator."""
    _mutate(source_fixture, module.MAP_PATH, b"| UX-DR52 |", b"| UX-DR53 |", "UX_DECISION_INVENTORY_DRIFT")


def test_acceptance_inventory_drift(source_fixture: Path) -> None:
    """A changed acceptance identifier fails source parity."""
    _mutate(source_fixture, module.SPEC_PATH, b"**AC-PERF-001:**", b"**AC-PERF-002:**", "UX_ACCEPTANCE_INVENTORY_DRIFT")


def test_activation_banner_status_and_current_owner(source_fixture: Path) -> None:
    """Every activation fixture reports its exact blocker and restores bytes."""
    _mutate(source_fixture, module.MAP_PATH, b"> **Preservation-only UX authority.**",
            b"> **Activated UX authority.**", "UX_ACTIVATION_UNAUTHORIZED")
    _mutate(source_fixture, module.MAP_PATH, b"preserved-not-activated; Stories 8.1-8.2 preservation contract",
            b"activated; Stories 8.1-8.2 preservation contract", "UX_ACTIVATION_UNAUTHORIZED")
    _mutate(source_fixture, module.MAP_PATH, b"preserved-not-activated; Stories 8.1-8.2 preservation contract",
            b"preserved-not-activated; Story 3.8 implementation", "UX_CURRENT_STORY_INVALID")


def test_malformed_authority_and_predecessor_are_rejected(source_fixture: Path) -> None:
    """A parseable missing authority or false predecessor PASS cannot generate a bundle."""
    contract = source_fixture / module.CONTRACT_PATH
    original = contract.read_bytes()
    try:
        document = json.loads(original)
        del document["authority"]
        contract.write_text(json.dumps(document))
        _reject(source_fixture, "UX_SCHEMA_INVALID")
    finally:
        contract.write_bytes(original)
    bundle = source_fixture / module.BUNDLE_PATH
    original = bundle.read_bytes()
    try:
        document = json.loads(original)
        document["bundleDigest"] = "0" * 64
        bundle.write_text(json.dumps(document))
        _reject(source_fixture, "UX_SCHEMA_INVALID")
    finally:
        bundle.write_bytes(original)
    predecessor = source_fixture / module.PREDECESSOR_PATH
    original = predecessor.read_bytes()
    try:
        document = json.loads(original)
        document["summary"]["passed"] = 5
        predecessor.write_text(json.dumps(document))
        _reject(source_fixture, "UX_SCHEMA_INVALID")
    finally:
        predecessor.write_bytes(original)
    assert module.generate(source_fixture, module.CONTRACT_PATH)


def test_bundle_write_failure_restores_every_output(source_fixture: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """A failure after the first replacement restores all three original byte streams."""
    targets = [source_fixture / relative for relative in module.OUTPUT_PATHS]
    originals = [b"old schema\n", b"old json\n", b"old markdown\n"]
    for target, content in zip(targets, originals):
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
    real_replace = os.replace
    calls = 0

    def fail_second(source: Path, destination: Path) -> None:
        nonlocal calls
        calls += 1
        if calls == 2:
            raise OSError("injected replacement failure")
        real_replace(source, destination)

    monkeypatch.setattr(module.os, "replace", fail_second)
    with pytest.raises(module.DispositionError, match="coherently") as failure:
        module.write_bundle(source_fixture, module.OUTPUT_PATHS,
                            module.generate(source_fixture, module.CONTRACT_PATH))
    assert failure.value.code == "UX_RENDER_DRIFT"
    assert [target.read_bytes() for target in targets] == originals


def test_bundle_outputs_are_readable_and_preserve_existing_mode(source_fixture: Path) -> None:
    """Atomic staging keeps a normal new-file mode and an existing file's mode."""
    existing = source_fixture / module.OUTPUT_PATHS[0]
    existing.parent.mkdir(parents=True, exist_ok=True)
    existing.write_bytes(b"old")
    existing.chmod(0o640)
    private = source_fixture / module.OUTPUT_PATHS[1]
    private.write_bytes(b"old")
    private.chmod(0o600)
    module.write_bundle(source_fixture, module.OUTPUT_PATHS,
                        module.generate(source_fixture, module.CONTRACT_PATH))
    assert existing.stat().st_mode & 0o777 == 0o640
    assert private.stat().st_mode & 0o777 == 0o644
    for relative in module.OUTPUT_PATHS[2:]:
        assert (source_fixture / relative).stat().st_mode & 0o777 == 0o644


def test_schema_paths_must_be_normalized_repository_relative() -> None:
    """Closed schema paths exclude traversal, absolute paths, and empty segments."""
    pattern = module.schema()["properties"]["authority"]["properties"]["contractPath"]["pattern"]
    assert re.fullmatch(pattern, module.CONTRACT_PATH)
    for path in ("../outside", "a/../outside", "a/./inside", "/absolute", "a//double", "a\\outside"):
        assert not re.fullmatch(pattern, path)


def test_exact_cli_arguments_write_three_expected_bytes(source_fixture: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """The declared CLI shape exits zero and emits the derivation's exact three files."""
    (source_fixture / ".git").mkdir()
    expected = module.generate(source_fixture, module.CONTRACT_PATH)
    monkeypatch.setattr(sys, "argv", [str(SCRIPT), "--repository", str(source_fixture),
                                     "--contract", module.CONTRACT_PATH,
                                     "--output-schema", module.OUTPUT_PATHS[0],
                                     "--output-json", module.OUTPUT_PATHS[1],
                                     "--output-markdown", module.OUTPUT_PATHS[2]])
    assert module.main() == 0
    assert tuple((source_fixture / path).read_bytes() for path in module.OUTPUT_PATHS) == expected


FAULTS = {
    "decision-missing": "UX_DECISION_MISSING",
    "decision-duplicate": "UX_DECISION_DUPLICATE",
    "decision-unknown": "UX_DECISION_UNKNOWN",
    "acceptance-missing": "UX_ACCEPTANCE_MISSING",
    "acceptance-duplicate": "UX_ACCEPTANCE_DUPLICATE",
    "acceptance-unknown": "UX_ACCEPTANCE_UNKNOWN",
    "owner-missing": "UX_OWNER_MISSING",
    "hash-missing": "UX_HASH_MISSING",
    "source-changed": "UX_SOURCE_DRIFT",
    "render-changed": "UX_RENDER_DRIFT",
    "json-order": "UX_ORDER_DRIFT",
    "markdown-order": "UX_ORDER_DRIFT",
    "row-activated": "UX_ACTIVATION_UNAUTHORIZED",
    "historical-owner": "UX_CURRENT_STORY_INVALID",
    "nonexistent-owner": "UX_CURRENT_STORY_INVALID",
}
FAULT_PROPERTY = "story82ObservedFault"


@pytest.fixture
def preservation_fixture(source_fixture: Path) -> Path:
    """Install the preserved bundle in an isolated checkout for read-only verification."""
    (source_fixture / ".git").mkdir()
    module.write_bundle(source_fixture, module.OUTPUT_PATHS,
                        module.generate(source_fixture, module.CONTRACT_PATH))
    return source_fixture


def _verification_cli(root: Path) -> tuple[int, list[str]]:
    """Measure the actual verification CLI's return code and reported blockers."""
    arguments = [str(SCRIPT), "--repository", str(root), "--contract", module.CONTRACT_PATH,
                 "--output-schema", module.OUTPUT_PATHS[0], "--output-json", module.OUTPUT_PATHS[1],
                 "--output-markdown", module.OUTPUT_PATHS[2], "--verify"]
    output, error = io.StringIO(), io.StringIO()
    with pytest.MonkeyPatch.context() as patch:
        patch.setattr(sys, "argv", arguments)
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(error):
            code = module.main()
    blockers = re.findall(r"^FAIL: ([A-Z0-9_]+):", error.getvalue(), re.M)
    if code == 0:
        assert "PASS:" in output.getvalue() and not error.getvalue()
    return code, blockers


def _fixture_digest(root: Path, paths: list[str]) -> str:
    """Hash every exact fixture stream, bound to its ordered relative path."""
    return module.digest(b"".join(path.encode() + b"\0" + (root / path).read_bytes() + b"\0" for path in paths))


def _measured_fault(root: Path, fault_id: str) -> dict[str, object]:
    """Prove a baseline PASS, exact rejection, finally restoration, and restored PASS."""
    paths = sorted([module.SPEC_PATH, module.MAP_PATH, module.CONTRACT_PATH,
                    module.PREDECESSOR_PATH, module.PREDECESSOR_MARKDOWN_PATH,
                    module.BUNDLE_PATH, *module.OUTPUT_PATHS])
    originals = {path: (root / path).read_bytes() for path in paths}
    baseline_exit, baseline_blockers = _verification_cli(root)
    assert (baseline_exit, baseline_blockers) == (0, [])
    before = _fixture_digest(root, paths)
    document = json.loads(originals[module.OUTPUT_PATHS[1]])
    try:
        rows = document["acceptanceCriteria"] if fault_id.startswith("acceptance-") else document["decisions"]
        if fault_id.endswith("-missing") and fault_id.startswith(("decision-", "acceptance-")):
            rows.pop()
        elif fault_id.endswith("-duplicate"):
            rows.append(dict(rows[-1]))
        elif fault_id.endswith("-unknown"):
            rows.append(dict(rows[-1], id="UX-DR999" if fault_id.startswith("decision-") else "AC-SAFE-999"))
        elif fault_id == "owner-missing":
            del rows[0]["owner"]
        elif fault_id == "hash-missing":
            del rows[0]["sourceSha256"]
        elif fault_id == "row-activated":
            rows[0]["status"] = "activated"
        elif fault_id.endswith("-owner"):
            rows[0]["owner"] = "Story 3.8 implementation" if fault_id == "historical-owner" else "Story 99.99 implementation"
        elif fault_id == "json-order":
            rows[0], rows[1] = rows[1], rows[0]
        if fault_id == "source-changed":
            (root / module.SPEC_PATH).write_bytes(originals[module.SPEC_PATH] + b"\n")
        elif fault_id == "render-changed":
            (root / module.OUTPUT_PATHS[2]).write_bytes(originals[module.OUTPUT_PATHS[2]] + b"changed rendering\n")
        elif fault_id == "markdown-order":
            lines = originals[module.OUTPUT_PATHS[2]].decode().splitlines(keepends=True)
            positions = [index for index, line in enumerate(lines) if line.startswith("| `UX-DR")]
            lines[positions[0]], lines[positions[1]] = lines[positions[1]], lines[positions[0]]
            (root / module.OUTPUT_PATHS[2]).write_text("".join(lines))
        else:
            (root / module.OUTPUT_PATHS[1]).write_bytes((json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode())
        mutated = _fixture_digest(root, paths)
        assert mutated != before
        observed_exit, observed_blockers = _verification_cli(root)
        assert observed_exit == 1
        assert observed_blockers == [FAULTS[fault_id]]
    finally:
        for path, content in originals.items():
            (root / path).write_bytes(content)
    after = _fixture_digest(root, paths)
    restored_exit, restored_blockers = _verification_cli(root)
    assert after == before
    assert (restored_exit, restored_blockers) == (0, [])
    return {"id": fault_id, "candidateCommit": CANDIDATE, "expectedBlocker": FAULTS[fault_id],
            "observedExitCode": observed_exit, "observedBlockers": observed_blockers,
            "beforeSha256": before, "mutatedSha256": mutated, "afterSha256": after,
            "baselineExitCode": baseline_exit, "baselineBlockers": baseline_blockers,
            "restoredExitCode": restored_exit, "restoredBlockers": restored_blockers}


def test_missing_decision(preservation_fixture: Path, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, "decision-missing"), sort_keys=True))


def test_duplicate_decision(preservation_fixture: Path, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, "decision-duplicate"), sort_keys=True))


def test_unknown_decision(preservation_fixture: Path, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, "decision-unknown"), sort_keys=True))


@pytest.mark.parametrize("fault_id", ["acceptance-missing", "acceptance-duplicate", "acceptance-unknown"])
def test_acceptance_identity_faults(preservation_fixture: Path, fault_id: str, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, fault_id), sort_keys=True))


@pytest.mark.parametrize("fault_id", ["owner-missing", "hash-missing"])
def test_ownership_and_hash_faults(preservation_fixture: Path, fault_id: str, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, fault_id), sort_keys=True))


def test_source_drift(preservation_fixture: Path, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, "source-changed"), sort_keys=True))


@pytest.mark.parametrize("fault_id", ["render-changed", "json-order", "markdown-order"])
def test_rendering_and_order_drift(preservation_fixture: Path, fault_id: str, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, fault_id), sort_keys=True))


@pytest.mark.parametrize("fault_id", ["row-activated", "historical-owner", "nonexistent-owner"])
def test_activation_and_story_binding_faults(preservation_fixture: Path, fault_id: str, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, fault_id), sort_keys=True))


@pytest.mark.parametrize("fault_id", list(FAULTS))
def test_fixtures_restore_byte_identically(preservation_fixture: Path, fault_id: str, record_property) -> None:
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(preservation_fixture, fault_id), sort_keys=True))


def test_verification_cli_preserves_every_bundle_byte(preservation_fixture: Path) -> None:
    before = {path: (preservation_fixture / path).read_bytes() for path in module.OUTPUT_PATHS}
    assert _verification_cli(preservation_fixture) == (0, [])
    assert before == {path: (preservation_fixture / path).read_bytes() for path in module.OUTPUT_PATHS}


@pytest.mark.parametrize("mapping", [1, "invalid", {}, [None], [1]])
def test_verification_cli_rejects_malformed_nested_mappings(preservation_fixture: Path, mapping: object) -> None:
    path = preservation_fixture / module.OUTPUT_PATHS[1]
    document = json.loads(path.read_bytes())
    document["decisions"][0]["historicalMappings"] = mapping
    path.write_text(json.dumps(document))
    assert _verification_cli(preservation_fixture) == (1, ["UX_SCHEMA_INVALID"])


@pytest.mark.parametrize("branch,blocker", [
    ("acceptance-owner", "UX_OWNER_MISSING"), ("acceptance-hash", "UX_HASH_MISSING"),
    ("acceptance-activation", "UX_ACTIVATION_UNAUTHORIZED"), ("acceptance-order", "UX_ORDER_DRIFT"),
    ("map-byte", "UX_SOURCE_DRIFT"), ("mapping-current", "UX_CURRENT_STORY_INVALID"),
    ("mapping-classification", "UX_CURRENT_STORY_INVALID"), ("provenance-current", "UX_CURRENT_STORY_INVALID"),
    ("provenance-classification", "UX_CURRENT_STORY_INVALID"),
], ids=[f"branch-{number:02d}" for number in range(1, 10)])
def test_verifier_additional_semantics(preservation_fixture: Path, branch: str, blocker: str) -> None:
    path = preservation_fixture / module.OUTPUT_PATHS[1]
    original = path.read_bytes()
    map_path = preservation_fixture / module.MAP_PATH
    map_original = map_path.read_bytes()
    document = json.loads(original)
    row = document["acceptanceCriteria"][0]
    try:
        if branch == "acceptance-owner": del row["owner"]
        elif branch == "acceptance-hash": del row["sourceSha256"]
        elif branch == "acceptance-activation": row["status"] = "activated"
        elif branch == "acceptance-order": document["acceptanceCriteria"].reverse()
        elif branch == "map-byte": map_path.write_bytes(map_original + b"\n")
        elif branch == "mapping-current": row["historicalMappings"][0]["current"] = True
        elif branch == "mapping-classification": row["historicalMappings"][0]["classification"] = "current"
        elif branch == "provenance-current": document["historicalProvenance"]["currentImplementationOwner"] = True
        else: document["historicalProvenance"]["classification"] = "current"
        path.write_bytes((json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode())
        assert _verification_cli(preservation_fixture) == (1, [blocker])
    finally:
        path.write_bytes(original)
        map_path.write_bytes(map_original)
    assert _verification_cli(preservation_fixture) == (0, [])


def test_verifier_executable_cli_restores_real_repository(tmp_path: Path) -> None:
    """Use real Git and a subprocess to measure baseline, semantic fault, and restoration."""
    repository = tmp_path / "real-repository"
    subprocess.run(["git", "clone", "--shared", "--quiet", str(ROOT), str(repository)],
                   check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    command = [sys.executable, str(SCRIPT), "--repository", str(repository), "--contract", module.CONTRACT_PATH,
               "--output-schema", module.OUTPUT_PATHS[0], "--output-json", module.OUTPUT_PATHS[1],
               "--output-markdown", module.OUTPUT_PATHS[2], "--verify"]
    paths = [*module.OUTPUT_PATHS, module.SPEC_PATH, module.MAP_PATH]
    before = {path: (repository / path).read_bytes() for path in paths}
    baseline = subprocess.run(command, capture_output=True)
    assert baseline.returncode == 0 and b"PASS:" in baseline.stdout
    path = repository / module.OUTPUT_PATHS[1]
    document = json.loads(path.read_bytes())
    try:
        document["acceptanceCriteria"][0]["owner"] = "Story 99.99 implementation"
        path.write_text(json.dumps(document, indent=2))
        rejected = subprocess.run(command, capture_output=True)
        assert rejected.returncode == 1
        assert re.findall(rb"FAIL: ([A-Z0-9_]+):", rejected.stderr) == [b"UX_CURRENT_STORY_INVALID"]
    finally:
        path.write_bytes(before[module.OUTPUT_PATHS[1]])
    restored = subprocess.run(command, capture_output=True)
    assert restored.returncode == 0 and b"PASS:" in restored.stdout
    assert before == {path: (repository / path).read_bytes() for path in paths}
