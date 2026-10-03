"""Story 8.1 disposition generation and fault fixtures."""

from __future__ import annotations

import importlib.util
import json
import os
import re
import subprocess
import sys
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
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


def test_missing_source_and_source_drift(source_fixture: Path) -> None:
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
