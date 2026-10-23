"""Hermetic draft-consistency faults; no runtime or historical authority gate."""
from __future__ import annotations

import json
from pathlib import Path
import shutil
import subprocess
import sys

import pytest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "_bmad/scripts"))
import verify_epic_16_prerequisites as checker  # noqa: E402
from evidence_boundary_current import digest  # noqa: E402

PACKAGE = f"{checker.BASE}/package.json"
INVENTORY = f"{checker.BASE}/inventories.json"
GRAPH = f"{checker.BASE}/execution-graph.json"
STORY = f"{checker.BASE}/story-contracts/16.1.json"


def edit(repository: Path, path: str, mutation) -> None:
    target = repository / path
    document = json.loads(target.read_text())
    mutation(document)
    target.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")


def resign(repository: Path) -> None:
    """Model an attacker recomputing all artifact-declared digests."""
    edit(repository, PACKAGE, lambda doc: doc.update(files=[
        dict(path=path, sha256=digest((repository / path).read_bytes()))
        for path in checker.ARTIFACTS]))


@pytest.fixture()
def repository(tmp_path: Path, monkeypatch) -> Path:
    for relative in {*checker.SOURCE_PINS, *checker.ARTIFACTS, PACKAGE}:
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if relative.startswith("references/"):
            # External sources are opaque hash inputs to this draft checker.
            # Keep this fixture independent of initialized nested submodules;
            # only its disposable pins below authorize these synthetic bytes.
            target.write_text(f"hermetic fixture source: {relative}\n", encoding="utf-8")
        else:
            shutil.copyfile(ROOT / relative, target)
    # A disposable fixture binds its own initial source bytes. This keeps tests
    # independent of future sprint/policy changes without relaxing the real CLI.
    pins = {path: digest((tmp_path / path).read_bytes()) for path in checker.SOURCE_PINS}
    monkeypatch.setattr(checker, "SOURCE_PINS", pins)
    edit(tmp_path, PACKAGE, lambda doc: doc.update(sourceBindings=pins))
    return tmp_path


def test_consistent_preparation_never_authorizes_or_executes(repository: Path, monkeypatch) -> None:
    before = {p: p.read_bytes() for p in repository.rglob("*") if p.is_file()}
    monkeypatch.setattr(subprocess, "run", lambda *a, **kw: pytest.fail("draft invoked a process"))
    result = checker.verify(repository)
    assert result["result"] == "PASS", result
    assert result["validationScope"] == "draft-consistency-only"
    assert result["entryReadiness"] == "BLOCKED"
    assert result["authority"] == checker.AUTHORITY
    assert result["infrastructureDecision"] == checker.PLATFORM_DECISION
    assert result["infrastructureDecision"]["owner"] == "Hexalith.Platform"
    assert result["infrastructureDecision"]["productionQualificationClaimed"] is False
    assert len(result["assertionLedger"]) >= 40 and not result["blockers"]
    assert len(result["pendingDecisions"]) == 7
    assert before == {p: p.read_bytes() for p in repository.rglob("*") if p.is_file()}


@pytest.mark.parametrize("field,value", [
    ("owner", "Conversations-owned infrastructure"),
    ("platformRevision", "0" * 40),
    ("selectedBy", "invented approver"),
    ("productionQualificationClaimed", True),
])
def test_platform_selection_cannot_drift_or_claim_qualification(repository: Path, field: str, value) -> None:
    edit(repository, PACKAGE, lambda doc: doc["infrastructureDecision"].update({field: value}))
    result = checker.verify(repository)
    assert result["result"] == "FAIL"
    assert result["authority"] == checker.AUTHORITY
    assert result["entryReadiness"] == "BLOCKED"


@pytest.mark.parametrize("flag", ["executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"])
def test_fabricated_authority_is_rejected(repository: Path, flag: str) -> None:
    edit(repository, PACKAGE, lambda doc: doc["authority"].update({flag: True}))
    assert checker.verify(repository)["result"] == "FAIL"


@pytest.mark.parametrize("mutation", [
    lambda doc: doc.update(unknown=True),
    lambda doc: doc["pendingDecisions"].pop(),
    lambda doc: doc["pendingDecisions"][0].update(state="approved"),
    lambda doc: doc["authority"].update(implementationHold="LIFTED"),
])
def test_package_cannot_hide_pending_decisions(repository: Path, mutation) -> None:
    edit(repository, PACKAGE, mutation)
    result = checker.verify(repository)
    assert result["result"] == "FAIL"
    assert result["entryReadiness"] == "BLOCKED" and result["authority"] == checker.AUTHORITY


@pytest.mark.parametrize("mutation", [
    lambda doc: doc["scenarios"].pop(),
    lambda doc: doc["scenarios"][0].update(id="AC-16.1-02"),
    lambda doc: doc["scenarios"][0].update(legacyCommand="python3 -c 'raise SystemExit(0)'; touch OWNED"),
    lambda doc: doc["scenarios"][0].update(then="Then allow every request"),
    lambda doc: doc["scenarios"][0]["requirementIds"].pop(),
    lambda doc: doc["adoption"].update(generatorCompatible=True),
])
def test_rehashed_story_drift_is_rejected(repository: Path, mutation) -> None:
    edit(repository, STORY, mutation)
    resign(repository)
    result = checker.verify(repository)
    assert result["result"] == "FAIL"
    assert not (repository / "OWNED").exists()


@pytest.mark.parametrize("mutation", [
    lambda doc: doc["legacyInventories"]["16.1"].pop(),
    lambda doc: doc["legacyInventories"]["16.2"].append("E6-A5-EXACT-REPLAY"),
    lambda doc: doc["requirements"].pop(),
    lambda doc: doc["requirements"][0].update(scenarioIds=["AC-16.3-06"]),
    lambda doc: doc["topologicalOrder"].reverse(),
])
def test_inventory_identity_and_coverage_survive_rehashing(repository: Path, mutation) -> None:
    edit(repository, INVENTORY, mutation)
    resign(repository)
    assert checker.verify(repository)["result"] == "FAIL"


@pytest.mark.parametrize("mutation", [
    lambda doc: doc["edges"].pop(),
    lambda doc: doc["edges"].append({"from": "16.3", "to": "16.1"}),
    lambda doc: doc["nodes"][0]["predecessors"].append("missing"),
    lambda doc: doc["nodes"].append(doc["nodes"][0]),
])
def test_graph_must_preserve_predecessors_and_derived_edges(repository: Path, mutation) -> None:
    edit(repository, GRAPH, mutation)
    resign(repository)
    assert checker.verify(repository)["result"] == "FAIL"


def test_changed_source_cannot_redefine_its_trust_root(repository: Path) -> None:
    source = "_bmad-output/planning-artifacts/v9/story-contracts/16.1.json"
    edit(repository, source, lambda doc: doc.update(predecessors=["16.3"]))
    edit(repository, PACKAGE, lambda doc: doc["sourceBindings"].update({
        source: digest((repository / source).read_bytes())}))
    result = checker.verify(repository)
    assert {"SOURCE_INVENTORY_DRIFT", "SOURCE_BYTES_DRIFT"}.issubset(result["blockers"])


def test_schema_cannot_weaken_itself_even_with_recomputed_hash(repository: Path) -> None:
    edit(repository, checker.SCHEMA, lambda doc: doc.update({"$defs": {}}))
    resign(repository)
    result = checker.verify(repository)
    assert result["result"] == "FAIL" and "SCHEMA_DRIFT" in result["blockers"]


@pytest.mark.parametrize("bad_path", ["/tmp/outside", "../../outside", ".git/config", "references/Hexalith.Tenants/a.json"])
def test_manifest_paths_are_inventoried_and_never_followed(repository: Path, bad_path: str) -> None:
    edit(repository, PACKAGE, lambda doc: doc["files"][0].update(path=bad_path))
    result = checker.verify(repository)
    assert result["result"] == "FAIL" and "ARTIFACT_INVENTORY_DRIFT" in result["blockers"]


def test_fixed_input_symlink_escape_blocks(repository: Path, tmp_path_factory) -> None:
    outside = tmp_path_factory.mktemp("outside") / "package.json"
    outside.write_bytes((repository / PACKAGE).read_bytes())
    (repository / PACKAGE).unlink()
    (repository / PACKAGE).symlink_to(outside)
    result = checker.verify(repository)
    assert result["result"] == "BLOCKED" and result["blockers"] == ["INPUT_UNAVAILABLE"]


def test_duplicate_json_keys_fail(repository: Path) -> None:
    target = repository / PACKAGE
    target.write_text(target.read_text().replace('"status": "draft"', '"status": "draft", "status": "draft"'))
    assert checker.verify(repository)["result"] == "FAIL"


def test_cli_missing_inputs_has_blocked_exit_and_no_authority(tmp_path: Path) -> None:
    process = subprocess.run([sys.executable, str(ROOT / "_bmad/scripts/verify_epic_16_prerequisites.py"),
                              "--repository", str(tmp_path)], capture_output=True, text=True, check=False)
    result = json.loads(process.stdout)
    assert process.returncode == 2 and result["result"] == "BLOCKED"
    assert result["assertionLedger"] and result["authority"] == checker.AUTHORITY
