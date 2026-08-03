"""Tests for atomic candidate-bound v9/v10 planning publication."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_v9_planning_authority.py"
SPEC = importlib.util.spec_from_file_location("publish_v9_planning_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)


def published_candidate() -> str:
    """Read the committed candidate bound by the checked-in bundle."""

    return json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))["planningCandidate"]


def test_complete_publication_is_deterministic_and_candidate_bound() -> None:
    """Every generated companion must reproduce from the one committed PC."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)

    assert set(outputs).issuperset(
        {
            publisher.BUNDLE_PATH,
            publisher.GRAPH_PATH,
            publisher.SUPERSESSION_PATH,
            publisher.VIEW_V2_PATH,
            publisher.UX_MAP_PATH,
            publisher.SPRINT_PATH,
        }
    )
    assert len([path for path in outputs if "/story-contracts/" in path]) == 27
    for path, content in outputs.items():
        assert (ROOT / path).read_bytes() == content
    bundle = json.loads(outputs[publisher.BUNDLE_PATH])
    assert bundle["planningCandidate"] == candidate
    assert bundle["implementationHold"] == "ACTIVE"
    assert bundle["epic5ActionA5"] == "open"
    assert publisher.BUNDLE_PATH not in {row["path"] for row in bundle["artifacts"]}


def test_schema_and_story_contract_denominators_are_non_vacuous() -> None:
    """All schemas and generated JSON documents must validate with exact counts."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)
    publisher.validate_schemas(ROOT, outputs)

    contracts = [
        json.loads(content)
        for path, content in outputs.items()
        if "/story-contracts/" in path
    ]
    assert {contract["storyId"] for contract in contracts} == set(publisher.EXPECTED_STORY_IDS)
    assert all(contract["scenarios"] for contract in contracts)
    assert len(next(contract for contract in contracts if contract["storyId"] == "10.3")["scenarios"]) == 8
    story_ten_four = next(contract for contract in contracts if contract["storyId"] == "10.4")
    assert len(story_ten_four["scenarios"]) == 9
    assert story_ten_four["scenarios"][-1]["id"] == "AC-10.4-09"


def test_invalid_hold_is_rejected_by_bundle_schema() -> None:
    """Schema validation must fail rather than accept a lifted publication hold."""

    schema = json.loads((ROOT / publisher.SCHEMA_PATHS[4]).read_text(encoding="utf-8"))
    bundle = json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))
    bundle["implementationHold"] = "LIFTED"

    with pytest.raises(jsonschema.ValidationError):
        jsonschema.Draft202012Validator(schema).validate(bundle)


def test_route_parity_and_alias_faults_return_stable_codes(monkeypatch: pytest.MonkeyPatch) -> None:
    """A route or alias mutation must fail with its exact stable blocker."""

    original = publisher.candidate_blob

    def parity_fault(root: Path, candidate: str, path: str) -> bytes:
        content = original(root, candidate, path)
        return content + b"\nparity fault\n" if path == publisher.MECHANICAL_PATHS[6] else content

    monkeypatch.setattr(publisher, "candidate_blob", parity_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_route_topology(ROOT, published_candidate())
    assert error.value.code == "EVIDENCE_WORKFLOW_PARITY_DRIFT"

    def alias_fault(root: Path, candidate: str, path: str) -> bytes:
        content = original(root, candidate, path)
        if path == ".agents/skills/bmad-dev-auto/SKILL.md":
            return content.replace(b"invoke `bmad-build-auto` exactly once", b"invoke `bmad-build` exactly once", 1)
        return content

    monkeypatch.setattr(publisher, "candidate_blob", alias_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_route_topology(ROOT, published_candidate())
    assert error.value.code == "EVIDENCE_ALIAS_ROUTE_INVALID"


def test_guidance_binding_removal_returns_stable_code(monkeypatch: pytest.MonkeyPatch) -> None:
    """Resolved customization may not silently lose the canonical runbook."""

    original = publisher.load_customization

    def missing_guidance(project_root: Path, skill_dir: Path):
        resolved = original(project_root, skill_dir)
        if skill_dir.name == "bmad-build":
            resolved["workflow"]["persistent_facts"] = [
                value
                for value in resolved["workflow"]["persistent_facts"]
                if "evidence-boundary-validation.md" not in value
            ]
        return resolved

    monkeypatch.setattr(publisher, "load_customization", missing_guidance)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_resolved_customization(ROOT, published_candidate())
    assert error.value.code == "EVIDENCE_GUIDANCE_NOT_USED"


def test_validation_failure_writes_no_partial_output(tmp_path: Path) -> None:
    """A pre-publication drift blocker must leave all proposed outputs untouched."""

    contract_directory = tmp_path / "_bmad-output/planning-artifacts/v9/story-contracts"
    contract_directory.mkdir(parents=True)
    (contract_directory / "unexpected.json").write_text("{}\n", encoding="utf-8")
    target = tmp_path / "generated.json"

    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(tmp_path, {"generated.json": b"generated\n"}, check=False)
    assert error.value.code == "STORY_CONTRACT_SET_DRIFT"
    assert not target.exists()


def test_dirty_worktree_is_preserved_and_unexpected_scope_fails_before_writing(tmp_path: Path) -> None:
    """Publication touches only its complete output set and preserves unrelated dirty bytes."""

    unrelated_path = tmp_path / "_bmad-output/implementation-artifacts/epic-6-context.md"
    unrelated_path.parent.mkdir(parents=True)
    unrelated_bytes = b"pre-existing unrelated worktree bytes\n"
    unrelated_path.write_bytes(unrelated_bytes)
    outputs = {path: f"generated:{path}\n".encode() for path in publisher.EXPECTED_OUTPUT_PATHS}

    publisher.publish(tmp_path, outputs, check=False)

    assert unrelated_path.read_bytes() == unrelated_bytes
    actual_files = {
        path.relative_to(tmp_path).as_posix()
        for path in tmp_path.rglob("*")
        if path.is_file()
    }
    assert actual_files == set(outputs) | {"_bmad-output/implementation-artifacts/epic-6-context.md"}

    blocked_root = tmp_path / "blocked"
    blocked_unrelated = blocked_root / "unrelated-context.md"
    blocked_unrelated.parent.mkdir(parents=True)
    blocked_unrelated.write_bytes(unrelated_bytes)
    unexpected_outputs = {**outputs, "unexpected-publication-path.json": b"unexpected\n"}

    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(blocked_root, unexpected_outputs, check=False)

    assert error.value.code == "PUBLICATION_SCOPE_DRIFT"
    assert blocked_unrelated.read_bytes() == unrelated_bytes
    assert not any((blocked_root / path).exists() for path in unexpected_outputs)


def test_publication_preserves_the_unrun_independent_assessment_boundary() -> None:
    """Generated planning state must neither run nor predetermine IR-0."""

    outputs = publisher.render_outputs(ROOT, published_candidate())
    bundle = json.loads(outputs[publisher.BUNDLE_PATH])
    view = outputs[publisher.VIEW_V2_PATH].decode("utf-8")
    sprint = outputs[publisher.SPRINT_PATH].decode("utf-8")

    assert bundle["implementationHold"] == "ACTIVE"
    assert bundle["epic5ActionA5"] == "open"
    assert "IR-0: not run by this publication." in view
    assert "does not implement a story,\n> run IR-0, lift the hold" in view
    assert "IR-0 was not run" in sprint
    assert sprint.count("# V10 PLANNING PUBLICATION:") == 1
    assert not any("ir-0" in path.lower() for path in outputs)
    assert not any("ir-0" in row["path"].lower() for row in bundle["artifacts"])
    assert "READY" not in view
    assert "NOT READY" not in view


def test_supersession_ux_sprint_and_customization_coverage_is_exact() -> None:
    """Mappings, 52/28 parity, successor backlog, hold, and A5 remain exact."""

    supersession = json.loads((ROOT / publisher.SUPERSESSION_PATH).read_text(encoding="utf-8"))
    assert len(supersession["storyDispositions"]) == 9
    assert [row["successorEpic"] for row in supersession["storyDispositions"] if row["sourceStory"] == "6.10"] == [10]
    story_ten = [row for row in supersession["v8AcceptanceObligations"] if row["sourceId"].startswith("V8-6.10-AC")]
    assert len(story_ten) == 10
    assert "AC-10.4-09" in next(row for row in story_ten if row["sourceId"] == "V8-6.10-AC9")["bindings"]

    ux = (ROOT / publisher.UX_MAP_PATH).read_text(encoding="utf-8")
    assert len(publisher.re.findall(r"^\| UX-DR\d+ \|", ux, publisher.re.MULTILINE)) == 52
    assert len(publisher.re.findall(r"^\| AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3} \|", ux, publisher.re.MULTILINE)) == 28
    assert "currentDisposition: preserved-not-activated" in ux

    sprint = (ROOT / publisher.SPRINT_PATH).read_text(encoding="utf-8")
    assert len(publisher.re.findall(r"^  (?:[7-9]|1[0-5])-\d+-[^:]+: backlog$", sprint, publisher.re.MULTILINE)) == 27
    assert "GLOBAL IMPLEMENTATION HOLD remains ACTIVE" in sprint
    assert 'action: "Promote the Story 5.3 evidence-boundary validation pattern into reusable dev/review guidance."' in sprint
    assert "status: open" in sprint[sprint.index("Promote the Story 5.3 evidence-boundary") :]

    for skill in ("bmad-build", "bmad-build-auto", "bmad-review"):
        resolved_path = ROOT / f"_bmad-output/planning-artifacts/v9/resolved-customization/{skill}.json"
        resolved = json.loads(resolved_path.read_text(encoding="utf-8"))
        assert resolved["planningCandidate"] == published_candidate()
        assert "evidence-boundary-validation.md" in json.dumps(resolved["resolved"])
