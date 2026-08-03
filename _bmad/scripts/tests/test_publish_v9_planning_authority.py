"""Tests for atomic candidate-bound v9/v10 planning publication."""

from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
from typing import Callable

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


def dummy_outputs(prefix: str) -> dict[str, bytes]:
    """Create a complete managed output set for filesystem-boundary tests."""

    return {path: f"{prefix}:{path}\n".encode() for path in publisher.EXPECTED_OUTPUT_PATHS}


def test_complete_publication_is_deterministic_and_candidate_bound() -> None:
    """Every generated companion must reproduce from the one committed PC."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)

    assert set(outputs) == set(publisher.EXPECTED_OUTPUT_PATHS)
    assert len([path for path in outputs if "/story-contracts/" in path]) == 27
    for path, content in outputs.items():
        assert (ROOT / path).read_bytes() == content
    bundle = json.loads(outputs[publisher.BUNDLE_PATH])
    assert bundle["planningCandidate"] == candidate
    assert bundle["implementationHold"] == "ACTIVE"
    assert bundle["epic5ActionA5"] == "open"
    assert [row["path"] for row in bundle["gitlinks"]] == list(publisher.ROOT_GITLINK_PATHS)
    artifact_paths = {row["path"] for row in bundle["artifacts"]}
    assert publisher.BUNDLE_PATH not in artifact_paths
    assert set(publisher.VALIDATOR_PATHS).issubset(artifact_paths)


def test_story_contract_schema_and_representative_parsing_are_exact() -> None:
    """Contracts use the canonical closed shape and explicit result semantics."""

    candidate = published_candidate()
    outputs = publisher.render_outputs(ROOT, candidate)
    publisher.validate_schemas(ROOT, outputs)
    contracts = {
        document["storyId"]: document
        for path, content in outputs.items()
        if "/story-contracts/" in path
        for document in [json.loads(content)]
    }
    required = {
        "schemaVersion",
        "storyId",
        "authority",
        "predecessors",
        "outcome",
        "rollback",
        "inventory",
        "scenarios",
        "finalRecord",
    }
    assert set(contracts) == set(publisher.EXPECTED_STORY_IDS)
    for contract in contracts.values():
        assert set(contract) == required
        assert contract["schemaVersion"] == "hexalith.conversations.story-contract.v1"
        assert contract["authority"]["planningCandidate"] == candidate
        assert contract["predecessors"] == sorted(set(contract["predecessors"]))
        assert contract["scenarios"]
        assert all(scenario["resultSemantics"]["expected"] == "PASS" for scenario in contract["scenarios"])
        assert all(scenario["resultSemantics"]["passExitCodes"] for scenario in contract["scenarios"])
        summary = contract["finalRecord"]["summary"]
        assert summary == {
            "required": len(contract["scenarios"]),
            "passed": len(contract["scenarios"]),
            "failed": 0,
            "blocked": 0,
            "skipped": 0,
            "notRun": 0,
        }
    assert len(contracts["10.3"]["scenarios"]) == 8
    assert len(contracts["10.4"]["scenarios"]) == 9
    assert contracts["10.4"]["scenarios"][-1]["id"] == "AC-10.4-09"
    ac_ten_four_eight = next(row for row in contracts["10.4"]["scenarios"] if row["id"] == "AC-10.4-08")
    assert "summary `9/9/0/0/0/0`" in ac_ten_four_eight["contract"]
    ac_fourteen_three_two = next(row for row in contracts["14.3"]["scenarios"] if row["id"] == "AC-14.3-02")
    assert ac_fourteen_three_two["resultSemantics"]["expected"] == "PASS"


def test_bundle_schema_rejects_invalid_hold_or_gitlink_scope() -> None:
    """Schema validation rejects a lifted hold and duplicate gitlink identities."""

    schema = json.loads((ROOT / publisher.SCHEMA_PATHS[4]).read_text(encoding="utf-8"))
    bundle = json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))
    bundle["implementationHold"] = "LIFTED"
    with pytest.raises(jsonschema.ValidationError):
        jsonschema.Draft202012Validator(schema).validate(bundle)

    bundle = json.loads((ROOT / publisher.BUNDLE_PATH).read_text(encoding="utf-8"))
    bundle["gitlinks"][1]["path"] = bundle["gitlinks"][0]["path"]
    bundle["gitlinks"][1]["commit"] = "f" * 40
    with pytest.raises(jsonschema.ValidationError):
        jsonschema.Draft202012Validator(schema).validate(bundle)


def test_explicit_check_candidate_is_respected_and_mismatch_fails(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """An explicit check candidate is never replaced by the bundle candidate."""

    bundle_candidate = "a" * 40
    requested_candidate = "b" * 40
    bundle = tmp_path / publisher.BUNDLE_PATH
    bundle.parent.mkdir(parents=True)
    bundle.write_text(json.dumps({"planningCandidate": bundle_candidate}), encoding="utf-8")
    revisions: list[str] = []

    def fake_git(root: Path, *arguments: str) -> bytes:
        revisions.append(arguments[-1])
        revision = arguments[-1].removesuffix("^{commit}")
        return f"{revision}\n".encode()

    monkeypatch.setattr(publisher, "git", fake_git)
    assert publisher.resolve_candidate(tmp_path, requested_candidate, check=True) == requested_candidate
    assert revisions[-1] == f"{requested_candidate}^{{commit}}"
    assert publisher.resolve_candidate(tmp_path, None, check=True) == bundle_candidate

    checked_root = tmp_path / "checked"
    old_outputs = dummy_outputs(bundle_candidate)
    new_outputs = dummy_outputs(requested_candidate)
    publisher.publish(checked_root, old_outputs, check=False)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(checked_root, new_outputs, check=True)
    assert error.value.code == "OUTPUT_DRIFT"


def test_ux_projection_rebinds_candidate_and_rejects_order_or_identity_faults() -> None:
    """UX projection carries the requested PC and exact ordered 52/28 identities."""

    source = (ROOT / publisher.UX_MAP_PATH).read_bytes()
    candidate = "f" * 40
    rendered = publisher.render_ux_map(source, candidate).decode()
    assert rendered.count(f"planningCandidate: {candidate}") == 1
    assert publisher.re.findall(r"^\| (UX-DR\d+) \|", rendered, publisher.re.MULTILINE) == list(
        publisher.EXPECTED_UX_DECISION_IDS
    )
    assert publisher.re.findall(
        r"^\| (AC-(?:SAFE|RESP|A11Y|LEAK|MOB|PERF)-\d{3}) \|",
        rendered,
        publisher.re.MULTILINE,
    ) == list(publisher.EXPECTED_UX_ACCEPTANCE_IDS)

    duplicate = source.replace(b"| UX-DR52 |", b"| UX-DR51 |", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_ux_map(duplicate, candidate)
    assert error.value.code == "UX_PARITY_DRIFT"

    swapped = source.replace(b"AC-SAFE-001", b"AC-SAFE-TMP", 1)
    swapped = swapped.replace(b"AC-SAFE-002", b"AC-SAFE-001", 1)
    swapped = swapped.replace(b"AC-SAFE-TMP", b"AC-SAFE-002", 1)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_ux_map(swapped, candidate)
    assert error.value.code == "UX_PARITY_DRIFT"


def test_graph_is_ordinal_and_every_successor_is_downstream_of_ir_zero() -> None:
    """IR-0 gates both entry branches without being run or assigned a story identity."""

    outputs = publisher.render_outputs(ROOT, published_candidate())
    graph = json.loads(outputs[publisher.GRAPH_PATH])
    nodes = {node["id"]: node["predecessors"] for node in graph["nodes"]}
    assert nodes["IR-0"] == ["PC-PUBLICATION"]
    assert nodes["7.1"] == ["6.2", "IR-0"]
    assert nodes["12.1"] == ["6.2", "IR-0"]
    assert all(predecessors == sorted(predecessors) for predecessors in nodes.values())
    assert graph["edges"] == sorted(graph["edges"], key=lambda edge: (edge["from"], edge["to"]))

    for story_id in publisher.EXPECTED_STORY_IDS:
        pending = list(nodes[story_id])
        ancestors: set[str] = set()
        while pending:
            predecessor = pending.pop()
            if predecessor not in ancestors:
                ancestors.add(predecessor)
                pending.extend(nodes[predecessor])
        assert "IR-0" in ancestors, story_id


def test_supersession_projects_the_exact_full_ledger_and_denominators() -> None:
    """The complete two-table ledger and preservation denominators remain non-vacuous."""

    supersession = json.loads((ROOT / publisher.SUPERSESSION_PATH).read_text(encoding="utf-8"))
    assert len(supersession["storyDispositions"]) == 9
    assert [
        row["successorEpic"]
        for row in supersession["storyDispositions"]
        if row["sourceStory"] == "6.10"
    ] == [10]
    ledger = supersession["obligationLedger"]
    assert ledger["inventoryId"] == publisher.OBLIGATION_LEDGER_ID
    assert ledger["sha256"] == publisher.OBLIGATION_LEDGER_DIGEST
    assert ledger["acceptanceCriteriaRows"] == 66
    assert ledger["totalRows"] == 156
    assert len(ledger["rows"]) == 156
    assert [row["ordinal"] for row in ledger["rows"]] == list(range(1, 157))
    assert len({row["sourceId"] for row in ledger["rows"]}) == 156
    ledger_payload = "".join(
        f"{row['sourceId']}|{row['canonicalBinding']}\n"
        for row in ledger["rows"]
    ).encode()
    assert hashlib.sha256(ledger_payload).hexdigest() == ledger["sha256"]
    story_ten = [row for row in ledger["rows"] if row["sourceId"].startswith("V8-6.10-AC")]
    assert len(story_ten) == 10
    assert "AC-10.4-09" in next(row for row in story_ten if row["sourceId"] == "V8-6.10-AC9")[
        "effectiveBindings"
    ]
    assert supersession["preservationDenominators"] == {
        "functionalRequirements": {"required": 124, "mapped": 124},
        "nonFunctionalRequirements": {"required": 77, "mapped": 77},
        "uxDecisions": {"required": 52, "mapped": 52},
        "uxAcceptanceCriteria": {"required": 28, "mapped": 28},
    }


def test_route_parity_aliases_and_inventory_order_have_stable_failures(monkeypatch: pytest.MonkeyPatch) -> None:
    """Route, alias, and inventory tuple mutations fail with stable codes."""

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

    monkeypatch.setattr(publisher, "candidate_blob", original)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.render_inventory(
            ROOT,
            published_candidate(),
            "V9-EVIDENCE-GUIDANCE-v2",
            tuple(reversed(publisher.GUIDANCE_PATHS)),
        )
    assert error.value.code == "INVENTORY_ORDER_DRIFT"


def exercise_guidance_mutation(
    relative_path: str,
    mutation: Callable[[bytes], bytes | None],
    expected_code: str,
) -> None:
    """Apply one real-file fixture and prove exact byte restoration."""

    path = ROOT / relative_path
    before = path.read_bytes()
    try:
        changed = mutation(before)
        if changed is None:
            path.unlink()
        else:
            path.write_bytes(changed)
        with pytest.raises(publisher.PublicationError) as error:
            publisher.render_resolved_customization(ROOT, published_candidate())
        assert error.value.code == expected_code
    finally:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(before)
    assert path.read_bytes() == before


def test_guidance_fault_fixtures_fail_and_restore_byte_identically() -> None:
    """The four AC-10.4-09 guidance faults are red and byte-restoring."""

    exercise_guidance_mutation(
        "_bmad/custom/bmad-build.toml",
        lambda _: None,
        "EVIDENCE_GUIDANCE_NOT_USED",
    )
    exercise_guidance_mutation(
        publisher.RUNBOOK_PATH,
        lambda content: content.replace(b"exact set equality", b"manifest containment", 1),
        "EVIDENCE_GUIDANCE_DRIFT",
    )
    exercise_guidance_mutation(
        publisher.RUNBOOK_PATH,
        lambda content: content.replace(
            b"Recompute every declared source hash",
            b"Trust every declared source hash",
            1,
        ),
        "EVIDENCE_GUIDANCE_DRIFT",
    )
    exercise_guidance_mutation(
        "_bmad/custom/bmad-review.toml",
        lambda content: content.replace(
            b"docs/runbooks/evidence-boundary-validation.md",
            b"docs/runbooks/redirected.md",
            1,
        ),
        "EVIDENCE_GUIDANCE_NOT_USED",
    )


def test_unbound_user_customization_layer_fails_closed() -> None:
    """A skill-specific user layer cannot alter candidate-bound resolved guidance."""

    user_layer = ROOT / "_bmad/custom/bmad-build.user.toml"
    assert not user_layer.exists()
    try:
        user_layer.write_text("[workflow]\npersistent_facts = []\n", encoding="utf-8")
        with pytest.raises(publisher.PublicationError) as error:
            publisher.render_resolved_customization(ROOT, published_candidate())
        assert error.value.code == "EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED"
    finally:
        user_layer.unlink(missing_ok=True)
    assert not user_layer.exists()


def test_dirty_worktree_scope_and_managed_namespaces_are_exact(tmp_path: Path) -> None:
    """Publication preserves unrelated dirt and rejects stale managed artifacts before writing."""

    unrelated_path = tmp_path / "_bmad-output/implementation-artifacts/epic-6-context.md"
    unrelated_path.parent.mkdir(parents=True)
    unrelated_bytes = b"pre-existing unrelated worktree bytes\n"
    unrelated_path.write_bytes(unrelated_bytes)
    outputs = dummy_outputs("generated")
    publisher.publish(tmp_path, outputs, check=False)
    assert unrelated_path.read_bytes() == unrelated_bytes
    actual_files = {
        path.relative_to(tmp_path).as_posix()
        for path in tmp_path.rglob("*")
        if path.is_file()
    }
    assert actual_files == set(outputs) | {"_bmad-output/implementation-artifacts/epic-6-context.md"}

    unexpected_outputs = {**outputs, "unexpected-publication-path.json": b"unexpected\n"}
    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(tmp_path, unexpected_outputs, check=False)
    assert error.value.code == "PUBLICATION_SCOPE_DRIFT"
    assert unrelated_path.read_bytes() == unrelated_bytes

    for stale_path in (
        "_bmad-output/planning-artifacts/v9/stale.json",
        "_bmad-output/planning-artifacts/v9/inventories/stale.json",
        "_bmad-output/planning-artifacts/v9/resolved-customization/stale.json",
        "_bmad-output/planning-artifacts/v9/story-contracts/stale.json",
        "_bmad-output/planning-artifacts/v9-stale-v1.json",
    ):
        stale = tmp_path / stale_path
        stale.parent.mkdir(parents=True, exist_ok=True)
        stale.write_bytes(b"stale\n")
        with pytest.raises(publisher.PublicationError) as error:
            publisher.publish(tmp_path, outputs, check=False)
        assert error.value.code == "PUBLICATION_SCOPE_DRIFT"
        stale.unlink()


def test_mid_commit_failure_restores_the_complete_managed_set(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A mid-replacement filesystem failure restores every old byte and removes staging."""

    before_outputs = dummy_outputs("before")
    publisher.publish(tmp_path, before_outputs, check=False)
    original_replace = publisher.os.replace
    calls = 0

    def fail_once(source: Path, destination: Path) -> None:
        nonlocal calls
        calls += 1
        if calls == 5:
            raise OSError("injected mid-commit failure")
        original_replace(source, destination)

    monkeypatch.setattr(publisher.os, "replace", fail_once)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.publish(tmp_path, dummy_outputs("after"), check=False)
    assert error.value.code == "PUBLICATION_WRITE_FAILED"
    for path, content in before_outputs.items():
        assert (tmp_path / path).read_bytes() == content
    assert not any(path.name.startswith(".v9-publication.") for path in tmp_path.iterdir())


def test_exact_gitlink_scope_and_faults(monkeypatch: pytest.MonkeyPatch) -> None:
    """Bundle gitlinks equal the ten root declarations and raw mode-160000 tree entries."""

    candidate = published_candidate()
    assert [row["path"] for row in publisher.gitlinks(ROOT, candidate)] == list(publisher.ROOT_GITLINK_PATHS)
    original_blob = publisher.candidate_blob

    def declaration_fault(root: Path, revision: str, path: str) -> bytes:
        content = original_blob(root, revision, path)
        if path == ".gitmodules":
            return content.replace(
                b"path = references/Hexalith.Tenants",
                b"path = references/Hexalith.NotTenants",
                1,
            )
        return content

    monkeypatch.setattr(publisher, "candidate_blob", declaration_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.gitlinks(ROOT, candidate)
    assert error.value.code == "GITLINK_SCOPE_MISMATCH"

    monkeypatch.setattr(publisher, "candidate_blob", original_blob)
    original_git = publisher.git
    raw = original_git(ROOT, "ls-tree", "-rz", candidate)

    def raw_fault(root: Path, *arguments: str) -> bytes:
        if arguments[:2] == ("ls-tree", "-rz"):
            entries = raw.split(b"\0")
            return b"\0".join(entry for entry in entries if b"references/Hexalith.Tenants" not in entry)
        return original_git(root, *arguments)

    monkeypatch.setattr(publisher, "git", raw_fault)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.gitlinks(ROOT, candidate)
    assert error.value.code == "GITLINK_SCOPE_MISMATCH"


def test_stable_input_failures_replace_tracebacks(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """Git, marker, and schema read faults produce stable publication blockers."""

    def timeout(*args, **kwargs):
        raise subprocess.TimeoutExpired("git", 30)

    monkeypatch.setattr(publisher.subprocess, "run", timeout)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.git(ROOT, "rev-parse", "HEAD")
    assert error.value.code == "CANDIDATE_GIT_UNAVAILABLE"

    monkeypatch.setattr(
        publisher.subprocess,
        "run",
        lambda *args, **kwargs: (_ for _ in ()).throw(OSError("spawn")),
    )
    with pytest.raises(publisher.PublicationError) as error:
        publisher.git(ROOT, "rev-parse", "HEAD")
    assert error.value.code == "CANDIDATE_GIT_UNAVAILABLE"

    malformed = b"<!-- BEGIN --><!-- END"
    with pytest.raises(publisher.PublicationError) as error:
        publisher.marker_block(malformed, "<!-- BEGIN", "<!-- END")
    assert error.value.code == "AUTHORITY_MARKER_INVALID"

    original_read_text = Path.read_text

    def malformed_schema(path: Path, *args, **kwargs) -> str:
        if path == ROOT / publisher.SCHEMA_PATHS[0]:
            return "{"
        return original_read_text(path, *args, **kwargs)

    monkeypatch.setattr(Path, "read_text", malformed_schema)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.validate_schemas(ROOT, {})
    assert error.value.code == "SCHEMA_VALIDATION_FAILED"

    monkeypatch.setattr(Path, "read_text", original_read_text)
    original_read_bytes = Path.read_bytes

    def failed_candidate_read(path: Path) -> bytes:
        if path == ROOT / publisher.EPICS_PATH:
            raise OSError("read")
        return original_read_bytes(path)

    monkeypatch.setattr(Path, "read_bytes", failed_candidate_read)
    with pytest.raises(publisher.PublicationError) as error:
        publisher.require_candidate_bytes(ROOT, published_candidate(), (publisher.EPICS_PATH,))
    assert error.value.code == "CANDIDATE_SOURCE_READ_FAILED"


def test_inventory_outputs_are_exact_ordinal_tuples() -> None:
    """Workflow, guidance, and reader inventories preserve their frozen ordered tuples."""

    cases = (
        ("evidence-workflows-v2.json", publisher.MECHANICAL_PATHS),
        ("evidence-guidance-v2.json", publisher.GUIDANCE_PATHS),
        ("evidence-readers-v1.json", publisher.READER_PATHS),
    )
    for filename, expected in cases:
        inventory = json.loads((ROOT / f"_bmad-output/planning-artifacts/v9/inventories/{filename}").read_text())
        rows = inventory["rows"]
        assert [row["ordinal"] for row in rows] == list(range(1, len(expected) + 1))
        assert [row["path"] for row in rows] == list(expected)


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
