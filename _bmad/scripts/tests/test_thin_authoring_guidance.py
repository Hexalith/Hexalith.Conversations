"""Story 11.1 Checkpoint A: generator, verifier, and restore-checked fault matrix."""

from __future__ import annotations

import hashlib
import importlib.util
import json
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "_bmad/scripts"
sys.path.insert(0, str(SCRIPTS))
import generate_thin_authoring_guidance as gen  # noqa: E402

SOURCE = gen.SOURCE_PATH
CONTRACT = gen.CONTRACT_PATH
SCHEMA = "docs/release-evidence/thin-authoring-guidance-v2.schema.json"
JSON_OUT = "docs/release-evidence/thin-authoring-guidance-v2.json"
MD_OUT = "docs/release-evidence/thin-authoring-guidance-v2.md"
INPUTS = [SOURCE, CONTRACT, SCHEMA] + [path for _, path, _ in gen.PREDECESSORS]
REVIEW = {"reviewer": "Test Reviewer", "decidedAtUtc": "2026-10-18T12:00:00Z"}


def run(script: str, *args: str, cwd: Path) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(SCRIPTS / script), *args], cwd=cwd,
                          capture_output=True, text=True, check=False)


def generate(repo: Path, *extra: str) -> subprocess.CompletedProcess:
    return run("generate_thin_authoring_guidance.py", "--repository", ".", "--contract", CONTRACT,
               "--schema", SCHEMA, "--output-json", JSON_OUT, "--output-markdown", MD_OUT, *extra, cwd=repo)


def verify(repo: Path, review: bool = True, output: Path | None = None) -> tuple[int, list[str]]:
    output = output or repo / "out/result.json"
    result = run("verify_thin_authoring_guidance.py", "--repository", ".", "--evidence", JSON_OUT,
                 "--markdown", MD_OUT, *(["--require-review-decision"] if review else []),
                 "--output", str(output), cwd=repo)
    report = json.loads(output.read_text(encoding="utf-8"))
    return result.returncode, [b["code"] for b in report["blockers"]]


def snapshot(repo: Path) -> dict[str, str]:
    return {str(p.relative_to(repo)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(repo.rglob("*")) if p.is_file() and "out" not in p.relative_to(repo).parts}


@pytest.fixture()
def repo(tmp_path: Path) -> Path:
    for relative in INPUTS:
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)
    done = generate(tmp_path, "--approve-reviewer", REVIEW["reviewer"], "--decided-at-utc", REVIEW["decidedAtUtc"])
    assert done.returncode == 0, done.stderr
    return tmp_path


def test_generator_is_deterministic_and_verifier_passes(repo: Path) -> None:
    before = snapshot(repo)
    assert generate(repo).returncode == 0  # review decision is preserved when still bound
    assert snapshot(repo) == before
    assert verify(repo) == (0, [])
    bundle = json.loads((repo / JSON_OUT).read_text(encoding="utf-8"))
    assert len(bundle["platformOwned"]) == 7 and len(bundle["moduleOwned"]) == 4
    assert bundle["reviewDecision"]["decision"] == "approved"


def test_committed_bundle_matches_current_source_and_reports_review_state(tmp_path: Path) -> None:
    code, blockers = verify(ROOT, review=False, output=tmp_path / "result.json")
    assert (code, blockers) == (0, [])


def edit_source(repo: Path, old: str, new: str) -> None:
    path = repo / SOURCE
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, old
    path.write_text(text.replace(old, new), encoding="utf-8", newline="\n")


def reassign(capability_id: str):
    def mutate(repo: Path) -> None:
        edit_source(repo, f"| `{capability_id}` | platform |", f"| `{capability_id}` | module |")
    return mutate


def drop_row(marker: str):
    def mutate(repo: Path) -> None:
        text = (repo / SOURCE).read_text(encoding="utf-8")
        line = next(item for item in text.split("\n") if marker in item)
        edit_source(repo, line + "\n", "")
    return mutate


def add_after(marker: str, row: str):
    def mutate(repo: Path) -> None:
        text = (repo / SOURCE).read_text(encoding="utf-8")
        line = next(item for item in text.split("\n") if marker in item)
        edit_source(repo, line + "\n", line + "\n" + row + "\n")
    return mutate


SOURCE_FAULTS: dict[str, tuple] = {}
for _cap in gen.EXPECTED_PLATFORM_OWNED:
    SOURCE_FAULTS[f"reassign-{_cap}"] = (reassign(_cap), "THIN_GUIDANCE_OWNERSHIP_INVALID")
SOURCE_FAULTS["missing-capability"] = (drop_row("| `health` | platform |"), "THIN_GUIDANCE_OWNERSHIP_INVALID")
SOURCE_FAULTS["extra-capability"] = (
    add_after("| `health` | platform |", "| `billing` | platform | Billing"), "THIN_GUIDANCE_OWNERSHIP_INVALID")
SOURCE_FAULTS["prohibition-removed"] = (drop_row("`reusable-aspire-library` |"), "PROHIBITED_CAPABILITY_PRESENT")
SOURCE_FAULTS["reusable-aspire-library-added"] = (
    add_after("| `Contracts` | Public commands", "| `Aspire` | Reusable module Aspire library. | Yes |"),
    "PROHIBITED_CAPABILITY_PRESENT")
SOURCE_FAULTS["module-service-defaults-added"] = (
    add_after("| `Contracts` | Public commands", "| `ServiceDefaults` | Module hook. | Yes |"),
    "PROHIBITED_CAPABILITY_PRESENT")
SOURCE_FAULTS["namespaced-service-defaults-added"] = (
    add_after("| `Contracts` | Public commands", "| `Hexalith.X.ServiceDefaults` | Module hook. | Yes |"),
    "PROHIBITED_CAPABILITY_PRESENT")
SOURCE_FAULTS["reusable-aspire-library-phrase-added"] = (
    add_after("| `Contracts` | Public commands", "| Reusable Aspire library | Shared wiring. | Yes |"),
    "PROHIBITED_CAPABILITY_PRESENT")
SOURCE_FAULTS["apphost-packable"] = (
    lambda r: edit_source(r, "| `IsPackable` | `false` |", "| `IsPackable` | `true` |"), "APPHOST_BOUNDARY_INVALID")
SOURCE_FAULTS["apphost-publishable"] = (
    lambda r: edit_source(r, "| `IsPublishable` | `false` |", "| `IsPublishable` | `true` |"), "APPHOST_BOUNDARY_INVALID")
SOURCE_FAULTS["apphost-cost-rule-omitted"] = (drop_row("| `authoring-cost` |"), "APPHOST_BOUNDARY_INVALID")
SOURCE_FAULTS["apphost-second-host"] = (
    add_after("| Repository-only test `AppHost` |", "| Second `AppHost` | Another host. | Yes |"), "APPHOST_BOUNDARY_INVALID")
SOURCE_FAULTS["apphost-none"] = (drop_row("| Repository-only test `AppHost` |"), "APPHOST_BOUNDARY_INVALID")


@pytest.mark.parametrize("fault", sorted(SOURCE_FAULTS))
def test_checkpoint_a_fault_matrix_source_faults(repo: Path, fault: str) -> None:
    mutate, code = SOURCE_FAULTS[fault]
    pristine = snapshot(repo)
    original = (repo / SOURCE).read_bytes()
    mutate(repo)
    assert (repo / SOURCE).read_bytes() != original, "mutation did not change the source"
    generated = generate(repo)
    assert generated.returncode == 1
    assert [line.split(":")[0] for line in generated.stderr.strip().splitlines()] == [code]
    exit_code, blockers = verify(repo)
    assert exit_code == 1 and code in blockers
    (repo / SOURCE).write_bytes(original)
    assert snapshot(repo) == pristine, "FIXTURE_NOT_RESTORED"
    assert verify(repo) == (0, [])


def mutate_bundle(repo: Path, change) -> None:
    bundle = json.loads((repo / JSON_OUT).read_text(encoding="utf-8"))
    change(bundle)
    (repo / JSON_OUT).write_text(gen.serialize_json(bundle), encoding="utf-8", newline="\n")


def rebuild_without_review(repo: Path) -> None:
    bundle = json.loads((repo / JSON_OUT).read_text(encoding="utf-8"))
    json_text, markdown = gen.finalize(gen.signable_of(bundle), None)
    (repo / JSON_OUT).write_text(json_text, encoding="utf-8", newline="\n")
    (repo / MD_OUT).write_text(markdown, encoding="utf-8", newline="\n")


def source_edited_after_generation(repo: Path) -> None:
    path = repo / SOURCE
    path.write_text(path.read_text(encoding="utf-8") + "\nAn extra sentence.\n", encoding="utf-8", newline="\n")


def markdown_edited(repo: Path) -> None:
    path = repo / MD_OUT
    path.write_text(path.read_text(encoding="utf-8").replace("# Thin-Module", "# Edited Thin-Module"),
                    encoding="utf-8", newline="\n")


def json_edited(repo: Path) -> None:
    mutate_bundle(repo, lambda b: b["candidate"].update(planningCandidate="0" * 40))


def schema_open(repo: Path) -> None:
    mutate_bundle(repo, lambda b: b.update(unexpected=True))


EVIDENCE_FAULTS = {
    "review-removed": (rebuild_without_review, "REVIEW_DECISION_MISSING"),
    "source-edited-after-generation": (source_edited_after_generation, "GUIDANCE_SOURCE_DRIFT"),
    "markdown-edited": (markdown_edited, "EVIDENCE_FORMAT_DRIFT"),
    "json-edited": (json_edited, "EVIDENCE_FORMAT_DRIFT"),
    "schema-opened": (schema_open, "EVIDENCE_FORMAT_DRIFT"),
}


@pytest.mark.parametrize("fault", sorted(EVIDENCE_FAULTS))
def test_checkpoint_a_fault_matrix_evidence_faults(repo: Path, fault: str) -> None:
    mutate, code = EVIDENCE_FAULTS[fault]
    pristine = snapshot(repo)
    saved = {p: (repo / p).read_bytes() for p in (SOURCE, JSON_OUT, MD_OUT)}
    mutate(repo)
    assert {p: (repo / p).read_bytes() for p in saved} != saved
    exit_code, blockers = verify(repo)
    assert exit_code == 1 and blockers == [code]
    for relative, data in saved.items():
        (repo / relative).write_bytes(data)
    assert snapshot(repo) == pristine, "FIXTURE_NOT_RESTORED"
    assert verify(repo) == (0, [])


def test_review_bound_to_wrong_digest_is_missing(repo: Path) -> None:
    def stale(bundle: dict) -> None:
        bundle["reviewDecision"]["boundSignablePayloadSha256"] = "1" * 64
    mutate_bundle(repo, stale)
    markdown = gen.render_markdown(json.loads((repo / JSON_OUT).read_text(encoding="utf-8")))
    bundle = json.loads((repo / JSON_OUT).read_text(encoding="utf-8"))
    bundle["digests"]["markdownSha256"] = gen.sha256_bytes(markdown.encode("utf-8"))
    (repo / JSON_OUT).write_text(gen.serialize_json(bundle), encoding="utf-8", newline="\n")
    (repo / MD_OUT).write_text(markdown, encoding="utf-8", newline="\n")
    assert verify(repo) == (1, ["REVIEW_DECISION_MISSING"])


def test_checkpoint_a_fault_matrix_inventory_matches_generator() -> None:
    declared = {**{k: v[1] for k, v in SOURCE_FAULTS.items()}, **{k: v[1] for k, v in EVIDENCE_FAULTS.items()}}
    assert declared == gen.FAULT_EXPECTATIONS


def test_skeleton_row_that_merely_mentions_apphost_is_not_exempt(repo: Path) -> None:
    add_after("| `Contracts` | Public commands", "| Reusable Aspire `AppHost` library | Shared wiring. | Yes |")(repo)
    generated = generate(repo)
    assert generated.returncode == 1
    assert "PROHIBITED_CAPABILITY_PRESENT" in [line.split(":")[0] for line in generated.stderr.strip().splitlines()]
