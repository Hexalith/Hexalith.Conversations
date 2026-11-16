"""Story 12.1 AC-12.1-05: derived-key decision generator, verifier and restore-checked lifecycle fault matrix.

Every case runs the real CLIs in a disposable repository whose Git objects borrow the real history read-only
(alternates), requires its exact blocker and a nonempty verifier ledger, then restores the fixture bytes.
"""
from __future__ import annotations

import hashlib
import itertools
import json
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "_bmad/scripts"
sys.path.insert(0, str(SCRIPTS))
import generate_derived_key_decision as gen  # noqa: E402
import verify_derived_key_decision as ver  # noqa: E402

APPROVER = "Test Owner"
DECIDED_AT = "2026-11-16T12:00:00Z"
RESULT = "out/result.json"
FAULT_NOT_DETECTED = "FAULT_NOT_DETECTED"
FIXTURE_NOT_RESTORED = "FIXTURE_NOT_RESTORED"


def run(script: str, *args: str, cwd: Path) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(SCRIPTS / script), *args], cwd=cwd, capture_output=True,
                          text=True, check=False)


def generate(repo: Path, *extra: str) -> subprocess.CompletedProcess:
    return run("generate_derived_key_decision.py", "--repository", ".", "--contract", gen.CONTRACT_PATH,
               "--adr", gen.ADR_PATH, "--output-json", gen.JSON_PATH, "--output-markdown", gen.MARKDOWN_PATH,
               *extra, cwd=repo)


def codes(completed: subprocess.CompletedProcess) -> list[str]:
    return [line.split(":", 1)[0] for line in completed.stderr.strip().splitlines() if line.strip()]


def verify(repo: Path, approval: bool = True) -> tuple[int, list[str], dict]:
    (repo / RESULT).unlink(missing_ok=True)
    completed = run("verify_derived_key_decision.py", "--repository", ".", "--evidence", gen.JSON_PATH,
                    "--markdown", gen.MARKDOWN_PATH, *(["--require-approval"] if approval else []),
                    "--output", RESULT, cwd=repo)
    assert (repo / RESULT).is_file(), f"the verifier wrote no report: {completed.stderr}"
    report = json.loads((repo / RESULT).read_text(encoding="utf-8"))
    assert completed.returncode == {"PASS": 0, "FAIL": 1, "BLOCKED": 2}[report["result"]], completed.stderr
    return completed.returncode, [blocker["code"] for blocker in report["blockers"]], report


def digest_of(completed: subprocess.CompletedProcess) -> str:
    return next(line.split(": ", 1)[1] for line in completed.stdout.splitlines()
                if line.startswith("signablePayloadSha256: "))


def snapshot(repo: Path) -> dict[str, str]:
    files = {str(path.relative_to(repo)): hashlib.sha256(path.read_bytes()).hexdigest()
             for path in sorted(repo.rglob("*")) if path.is_file()
             and not {"out", ".git"} & set(path.relative_to(repo).parts)}
    alternates = repo / ".git/objects/info/alternates"
    files[".git/objects/info/alternates"] = alternates.read_text(encoding="utf-8") if alternates.exists() else ""
    replace = repo / ".git/refs/replace"
    files[".git/refs/replace"] = ",".join(sorted(path.name for path in replace.iterdir())) if replace.is_dir() else ""
    return files


def evidence_paths() -> list[str]:
    _, legend, errors = gen.parse_adr((ROOT / gen.ADR_PATH).read_text(encoding="utf-8"))
    assert not errors
    return [row["path"] for row in legend]


@pytest.fixture(scope="module")
def template(tmp_path_factory: pytest.TempPathFactory) -> Path:
    repo = tmp_path_factory.mktemp("derived-key-decision") / "repo"
    for relative in {*gen.readonly_inputs(), *evidence_paths()}:
        target = repo / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)
    subprocess.run(["git", "init", "-q", str(repo)], check=True)
    common = subprocess.run(["git", "-C", str(ROOT), "rev-parse", "--path-format=absolute", "--git-common-dir"],
                            check=True, capture_output=True, text=True).stdout.strip()
    # Read-only object access to the real planning candidate and Story 6.2 history, without cloning.
    (repo / ".git/objects/info/alternates").write_text(str(Path(common) / "objects") + "\n", encoding="utf-8")
    unapproved = generate(repo)
    assert unapproved.returncode == 0, unapproved.stderr
    approved = generate(repo, "--approve-reviewer", APPROVER, "--decided-at-utc", DECIDED_AT,
                        "--approved-payload-sha256", digest_of(unapproved))
    assert approved.returncode == 0, approved.stderr
    assert digest_of(approved) == digest_of(unapproved), "approval must stay outside the signable payload"
    return repo


@pytest.fixture()
def repo(template: Path, tmp_path: Path) -> Path:
    target = tmp_path / "repo"
    shutil.copytree(template, target, symlinks=True)
    return target


# ---------------------------------------------------------------- mutation helpers


def edit(repo: Path, relative: str, old: str, new: str) -> None:
    path = repo / relative
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, old
    path.write_text(text.replace(old, new), encoding="utf-8", newline="\n")


def row_line(repo: Path, family: str, field: str) -> str:
    prefix = f"| `{family}` | `{field}` |"
    lines = [line for line in (repo / gen.ADR_PATH).read_text(encoding="utf-8").split("\n") if line.startswith(prefix)]
    assert len(lines) == 1, prefix
    return lines[0]


def set_cell(family: str, field: str, index: int, value: str):
    def mutate(repo: Path) -> None:
        line = row_line(repo, family, field)
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        cells[index] = value
        edit(repo, gen.ADR_PATH, line, "| " + " | ".join(cells) + " |")
    return mutate


def set_rule(family: str, field: str, token: str):
    return set_cell(family, field, 3, f"`{token}`")


def omit_row(family: str, field: str):
    def mutate(repo: Path) -> None:
        edit(repo, gen.ADR_PATH, row_line(repo, family, field) + "\n", "")
    return mutate


def omit_family(family: str):
    def mutate(repo: Path) -> None:
        for field in gen.FIELDS:
            omit_row(family, field)(repo)
    return mutate


def chain(*mutations):
    def mutate(repo: Path) -> None:
        for mutation in mutations:
            mutation(repo)
    return mutate


def duplicate_row(repo: Path) -> None:
    line = row_line(repo, "conversation", "owner")
    edit(repo, gen.ADR_PATH, line + "\n", line + "\n" + line + "\n")


def undeclared_family(repo: Path) -> None:
    line = row_line(repo, "resolution-fence", "rollback")
    edit(repo, gen.ADR_PATH, line + "\n",
         line + "\n| `tenant-shadow` | `owner` | current | `eventstore-derived` | A shadow index copy. | `E01` |\n")


def evidence_test_absent(repo: Path) -> None:
    edit(repo, gen.ADR_PATH, "AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurable` |",
         "AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurableRenamed` |")


def append(relative: str, extra: bytes):
    def mutate(repo: Path) -> None:
        path = repo / relative
        path.write_bytes(path.read_bytes() + extra)
    return mutate


def inventory_row_reordered(repo: Path) -> None:
    edit(repo, gen.EPICS_PATH, "`V8-6.11-AC1`, `V8-6.11-DEPENDENCY-6.2`, and",
         "`V8-6.11-DEPENDENCY-6.2`, `V8-6.11-AC1`, and")


def omit_lifecycle_marker(repo: Path) -> None:
    edit(repo, gen.ADR_PATH, gen.LIFECYCLE_MARKER + "\n", "")


def wrong_cell_count(repo: Path) -> None:
    line = row_line(repo, "conversation", "owner")
    edit(repo, gen.ADR_PATH, line, line + " extra |")


def mistyped_field(repo: Path) -> None:
    edit(repo, gen.ADR_PATH, "| `conversation` | `writer` |", "| `conversation` | `writter` |")


def malformed_evidence_row(repo: Path) -> None:
    edit(repo, gen.ADR_PATH, "| `E05` |", "| `E5` |")


def story_16_3_binding_commit_replaced(repo: Path) -> None:
    """Make the binding commit read as its parent, where the 16.3 sprint row is still review."""
    parent = subprocess.run(["git", "-C", str(ROOT), "rev-parse", gen.STORY_16_3_COMMIT + "^"], check=True,
                            capture_output=True, text=True).stdout.strip()
    subprocess.run(["git", "-C", str(repo), "replace", gen.STORY_16_3_COMMIT, parent], check=True, capture_output=True)


def schema_rejects_generated_bundle(repo: Path) -> None:
    edit(repo, gen.SCHEMA_PATH, f'"const": "{gen.SCHEMA_VERSION}"', f'"const": "{gen.SCHEMA_VERSION}-other"')


def history_unavailable(repo: Path) -> None:
    (repo / ".git/objects/info/alternates").unlink()


def git_unavailable(repo: Path) -> None:
    (repo / ".git").rename(repo / ".git-hidden")


def planning_candidate_unresolvable(repo: Path) -> None:
    pc = json.loads((repo / gen.CONTRACT_PATH).read_text(encoding="utf-8"))["authority"]["planningCandidate"]
    edit(repo, gen.CONTRACT_PATH, pc, "f" * 40)


ADR_FAULTS: dict[str, object] = {}
for _family in gen.FAMILIES:
    ADR_FAULTS[f"omit-family-{_family}"] = omit_family(_family)
    for _field in gen.FIELDS:
        ADR_FAULTS[f"omit-{_family}-{_field}"] = omit_row(_family, _field)
    ADR_FAULTS[f"second-writer-{_family}"] = set_rule(_family, "writer", "query-handler")
    ADR_FAULTS[f"read-repair-{_family}"] = set_rule(_family, "readRepair", "true")
    ADR_FAULTS[f"authoritative-owner-{_family}"] = set_rule(_family, "owner", "conversations-authoritative")
    ADR_FAULTS[f"reverse-write-rollback-{_family}"] = set_rule(_family, "rollback", "reverse-write")
for _index, _field in enumerate(gen.FIELDS):
    _rotating = gen.FAMILIES[_index % len(gen.FAMILIES)]
    ADR_FAULTS[f"blank-statement-{_field}"] = set_cell(_rotating, _field, 4, "")
    ADR_FAULTS[f"blank-rule-{_field}"] = set_cell(_rotating, _field, 3, "")
ADR_FAULTS.update({
    "unlabeled-proposed-rollback": set_cell("conversation", "rollback", 2, "current"),
    "current-row-without-evidence": set_cell("conversation", "owner", 5, "none"),
    "unknown-evidence-id": set_cell("conversation", "owner", 5, "`E99`"),
    "evidence-test-absent": evidence_test_absent,
    "duplicate-row": duplicate_row,
    "malformed-order": set_rule("conversation", "order", "live=five:write;rebuild=3"),
    "undeclared-family": undeclared_family,
    "key-grammar-changed": set_rule("conversation", "key", "projection:conversations:<tenantId>:<conversationId>"),
    "in-place-migration": set_rule("conversation", "compatibility", "in-place-rewrite"),
    "interrupted-rebuild-resumes-partial-plan": set_rule("resolution-proof", "rebuild", "resume-partial-plan"),
    "read-time-backfill": set_rule("tenant-index", "backfill", "read-time-backfill"),
    "query-delete": set_rule("conversation", "deletion", "query-delete"),
    "proof-expires-with-ledger": set_rule("resolution-proof", "expiry", "redelivery-window-ttl"),
    "ledger-never-expires": set_rule("dispatch-ledger", "expiry", "none"),
    "ledger-completes-before-proof": set_rule("dispatch-ledger", "order", "live=1:acquire,8:complete;rebuild=5"),
    "index-merge-before-conversation-write": set_rule("tenant-index", "order", "live=4:pending,5:merge;rebuild=4"),
    "rebuild-plan-proof-not-first": chain(set_rule("resolution-proof", "order", "live=2:pending,9:complete;rebuild=2"),
                                          set_rule("resolution-metadata", "order", "live=7:batch;rebuild=1")),
    "proof-completion-dropped": set_rule("resolution-proof", "order", "live=2:pending;rebuild=1"),
    "rollback-repeats-side-effects": set_rule("tenant-index", "rollback", "replay-side-effects"),
})
ADR_FAULTS.update({
    "omit-lifecycle-marker": omit_lifecycle_marker,
    "wrong-cell-count": wrong_cell_count,
    "mistyped-field": mistyped_field,
    "malformed-evidence-row": malformed_evidence_row,
})
BINDING_FAULTS = {
    "story-6-2-record-mutated": append(gen.STORY_6_2_RECORD, b"\nAn appended line.\n"),
    "baseline-v1-json-mutated": append(gen.BASELINE_V1[0], b" "),
    "baseline-v1-md-mutated": append(gen.BASELINE_V1[1], b"\n"),
    "baseline-v1-xunit-mutated": append(gen.BASELINE_V1[2], b"\n"),
    "contract-edited": append(gen.CONTRACT_PATH, b"\n"),
    "story-16-3-binding-commit-replaced": story_16_3_binding_commit_replaced,
    "inventory-row-reordered": inventory_row_reordered,
    "schema-rejects-generated-bundle": schema_rejects_generated_bundle,
}
HISTORY_FAULTS = {
    "history-unavailable": history_unavailable,
    "git-unavailable": git_unavailable,
    "planning-candidate-unresolvable": planning_candidate_unresolvable,
}
SOURCE_FAULTS = {**ADR_FAULTS, **BINDING_FAULTS, **HISTORY_FAULTS}


def restore(repo: Path, saved: dict[str, bytes]) -> None:
    if (repo / ".git-hidden").exists():
        (repo / ".git-hidden").rename(repo / ".git")
    shutil.rmtree(repo / ".git/refs/replace", ignore_errors=True)
    for relative, data in saved.items():
        path = repo / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)


def saved_inputs(repo: Path) -> dict[str, bytes]:
    paths = [*gen.readonly_inputs(), gen.JSON_PATH, gen.MARKDOWN_PATH, ".git/objects/info/alternates"]
    return {path: (repo / path).read_bytes() for path in paths}


@pytest.mark.parametrize("fault", sorted(SOURCE_FAULTS))
def test_lifecycle_fault_matrix_source_faults(repo: Path, fault: str) -> None:
    expected = list(gen.FAULT_EXPECTATIONS[fault])
    pristine, saved = snapshot(repo), saved_inputs(repo)
    SOURCE_FAULTS[fault](repo)
    assert snapshot(repo) != pristine, f"{FAULT_NOT_DETECTED}: the mutation changed nothing"
    generated = generate(repo)
    blocked = fault in HISTORY_FAULTS
    assert generated.returncode == (2 if blocked else 1), f"{FAULT_NOT_DETECTED}: {generated.stdout}{generated.stderr}"
    assert codes(generated) == expected, f"{FAULT_NOT_DETECTED}: {generated.stderr}"
    exit_code, blockers, report = verify(repo)
    verifier_expected = set(expected) | ({gen.BLOCK_APPROVAL} if fault in ADR_FAULTS else set())
    assert (exit_code, set(blockers)) == (2 if blocked else 1, verifier_expected), f"{FAULT_NOT_DETECTED}: {report}"
    assert report["assertionLedger"], "a vacuous verifier ledger is never evidence"
    assert (report["result"] == "BLOCKED") == blocked
    restore(repo, saved)
    assert snapshot(repo) == pristine, FIXTURE_NOT_RESTORED
    assert verify(repo)[:2] == (0, [])


def approval_presented_digest_mismatch(repo: Path) -> subprocess.CompletedProcess:
    return generate(repo, "--approve-reviewer", APPROVER, "--decided-at-utc", DECIDED_AT,
                    "--approved-payload-sha256", "0" * 64)


def test_lifecycle_fault_matrix_approval_of_a_different_digest_is_refused(repo: Path) -> None:
    pristine = snapshot(repo)
    refused = approval_presented_digest_mismatch(repo)
    assert refused.returncode == 1 and codes(refused) == list(gen.FAULT_EXPECTATIONS["approval-presented-digest-mismatch"])
    assert snapshot(repo) == pristine, FIXTURE_NOT_RESTORED
    exit_code, blockers, report = verify(repo)
    assert (exit_code, blockers) == (0, []) and report["assertionLedger"]


def test_lifecycle_fault_matrix_non_closed_path_is_refused(repo: Path) -> None:
    pristine = snapshot(repo)
    other = "docs/release-evidence/derived-key-lifecycle-decision-other.json"
    refused = run("generate_derived_key_decision.py", "--repository", ".", "--contract", gen.CONTRACT_PATH, "--adr",
                  gen.ADR_PATH, "--output-json", other, "--output-markdown", gen.MARKDOWN_PATH, cwd=repo)
    assert refused.returncode == 2 and codes(refused) == list(gen.FAULT_EXPECTATIONS["non-closed-path"]), refused.stderr
    completed = run("verify_derived_key_decision.py", "--repository", ".", "--evidence", other, "--markdown",
                    gen.MARKDOWN_PATH, "--require-approval", "--output", RESULT, cwd=repo)
    assert (repo / RESULT).is_file(), f"the verifier wrote no report: {completed.stderr}"
    report = json.loads((repo / RESULT).read_text(encoding="utf-8"))
    assert completed.returncode == 2 and report["result"] == "BLOCKED"
    assert [b["code"] for b in report["blockers"]] == list(gen.FAULT_EXPECTATIONS["non-closed-path"])
    assert report["assertionLedger"], "a vacuous verifier ledger is never evidence"
    assert not (repo / other).exists() and snapshot(repo) == pristine, FIXTURE_NOT_RESTORED
    assert verify(repo)[:2] == (0, [])


def load(repo: Path) -> dict:
    return json.loads((repo / gen.JSON_PATH).read_text(encoding="utf-8"))


def write_bundle(repo: Path, bundle: dict, *, resign: bool = False) -> None:
    """Write a bundle with consistent digests and Markdown; resign also rebinds the approval."""
    signable = gen.signable_of(bundle)
    approval = bundle["approval"]
    if resign and approval is not None:
        approval = {**approval, "boundSignablePayloadSha256": gen.sha256_bytes(gen.canonical(signable))}
    json_text, markdown = gen.finalize(signable, approval)
    (repo / gen.JSON_PATH).write_text(json_text, encoding="utf-8", newline="\n")
    (repo / gen.MARKDOWN_PATH).write_text(markdown, encoding="utf-8", newline="\n")


def rewrite(change, *, resign: bool = False):
    def mutate(repo: Path) -> None:
        bundle = load(repo)
        change(bundle)
        write_bundle(repo, bundle, resign=resign)
    return mutate


def ownership_rule(field: str, token: str):
    def change(bundle: dict) -> None:
        bundle["ownership"][2][field]["rule"] = token
    return change


def section_rule(section: str, family_index: int, token: str):
    def change(bundle: dict) -> None:
        rows = bundle[section]["families"] if isinstance(bundle[section], dict) else bundle[section]
        rows[family_index]["rule"] = token
    return change


def markdown_edited(repo: Path) -> None:
    edit(repo, gen.MARKDOWN_PATH, "# Derived-Key Lifecycle Decision v1", "# Edited Derived-Key Lifecycle Decision v1")


def json_unparseable(repo: Path) -> None:
    (repo / gen.JSON_PATH).write_text("{", encoding="utf-8", newline="\n")


def key_prefix(token: str):
    def change(bundle: dict) -> None:
        bundle["keyFamilies"][0]["keyPrefix"] = token
    return change


def json_noncanonical(repo: Path) -> None:
    (repo / gen.JSON_PATH).write_text(json.dumps(load(repo), indent=4, ensure_ascii=False) + "\n",
                                      encoding="utf-8", newline="\n")


def schema_opened(repo: Path) -> None:
    bundle = load(repo)
    bundle["unexpected"] = True
    (repo / gen.JSON_PATH).write_text(gen.serialize_json(bundle), encoding="utf-8", newline="\n")


def adr_edited_after_approval(repo: Path) -> None:
    path = repo / gen.ADR_PATH
    path.write_text(path.read_text(encoding="utf-8") + "\nAn extra sentence after approval.\n",
                    encoding="utf-8", newline="\n")


EVIDENCE_FAULTS = {
    "approval-removed": rewrite(lambda bundle: bundle.update(approval=None)),
    "approval-bound-to-stale-digest": rewrite(lambda bundle: bundle["approval"].update(boundSignablePayloadSha256="1" * 64)),
    "approval-unnamed": rewrite(lambda bundle: bundle["approval"].update(approver=" ")),
    "adr-edited-after-approval": adr_edited_after_approval,
    "markdown-edited": markdown_edited,
    "json-unparseable": json_unparseable,
    "json-noncanonical": json_noncanonical,
    "schema-opened": schema_opened,
    "resigned-second-writer": rewrite(ownership_rule("writer", "query-handler"), resign=True),
    "resigned-read-repair": rewrite(ownership_rule("readRepair", "true"), resign=True),
    "resigned-proof-expiry": rewrite(section_rule("expiry", 5, "redelivery-window-ttl"), resign=True),
    "resigned-reverse-write-rollback": rewrite(section_rule("rollback", 0, "reverse-write"), resign=True),
    "resigned-key-prefix": rewrite(key_prefix("projection:conversation:"), resign=True),
}


@pytest.mark.parametrize("fault", sorted(EVIDENCE_FAULTS))
def test_lifecycle_fault_matrix_evidence_faults(repo: Path, fault: str) -> None:
    pristine, saved = snapshot(repo), saved_inputs(repo)
    EVIDENCE_FAULTS[fault](repo)
    assert snapshot(repo) != pristine, f"{FAULT_NOT_DETECTED}: the mutation changed nothing"
    exit_code, blockers, report = verify(repo)
    assert (exit_code, blockers) == (1, list(ver.VERIFIER_FAULT_EXPECTATIONS[fault])), f"{FAULT_NOT_DETECTED}: {report}"
    assert report["assertionLedger"], "a vacuous verifier ledger is never evidence"
    restore(repo, saved)
    assert snapshot(repo) == pristine, FIXTURE_NOT_RESTORED
    assert verify(repo)[:2] == (0, [])


def test_lifecycle_fault_matrix_complete_lifecycle_is_deterministic_and_verified(repo: Path) -> None:
    before = snapshot(repo)
    regenerated = generate(repo)  # a still-bound approval is preserved
    assert regenerated.returncode == 0 and regenerated.stdout.splitlines()[0] == "PASS", regenerated.stderr
    assert snapshot(repo) == before, "regeneration must be byte-identical"
    exit_code, blockers, report = verify(repo)
    assert (exit_code, blockers) == (0, [])
    assert report["assertionLedger"] and all(row["state"] == "PASS" for row in report["assertionLedger"])
    bundle = load(repo)
    assert bundle["approval"]["approver"] == APPROVER
    assert bundle["approval"]["boundSignablePayloadSha256"] == bundle["digests"]["signablePayloadSha256"]
    cells = gen.cells_from_bundle(bundle)
    assert sorted(cells) == sorted((family, field) for family in gen.FAMILIES for field in gen.FIELDS)
    assert all(cell["rule"].strip() and cell["statement"].strip() for cell in cells.values())
    assert [row["family"] for row in bundle["keyFamilies"]] == list(gen.FAMILIES)


def test_lifecycle_fault_matrix_inventory_matches_the_tools() -> None:
    argument_faults = {"approval-presented-digest-mismatch", "non-closed-path"}
    assert {fault: gen.FAULT_EXPECTATIONS[fault] for fault in SOURCE_FAULTS} == {
        fault: code for fault, code in gen.FAULT_EXPECTATIONS.items() if fault not in argument_faults}
    assert set(ORDER_REORDERS) == set(gen.ORDER_INVARIANTS)
    assert set(EVIDENCE_FAULTS) == set(ver.VERIFIER_FAULT_EXPECTATIONS)
    assert {(family, field) for family in gen.FAMILIES for field in gen.FIELDS} <= {
        (family, field) for family in gen.FAMILIES for field in gen.FIELDS if f"omit-{family}-{field}" in SOURCE_FAULTS}


def test_committed_decision_matches_its_adr() -> None:
    """The repository's own pair verifies; approval is reported by AC-12.1-04, not required here."""
    completed = run("verify_derived_key_decision.py", "--repository", ".", "--evidence", gen.JSON_PATH,
                    "--markdown", gen.MARKDOWN_PATH, cwd=ROOT)
    assert completed.returncode == 0 and completed.stdout.strip() == "PASS", completed.stderr


def decided_orders() -> dict:
    cells, legend, errors = gen.parse_adr((ROOT / gen.ADR_PATH).read_text(encoding="utf-8"))
    assert not errors and not gen.evaluate(cells, legend)
    return {family: gen.parse_order(cells[(family, "order")]["rule"]) for family in gen.FAMILIES}


def reorder(**families: tuple[dict, int]):
    """Return the decided orders with the named families replaced, one minimal change per invariant."""
    def apply() -> dict:
        orders = decided_orders()
        orders.update({family.replace("_", "-"): order for family, order in families.items()})
        return orders
    return apply


ORDER_REORDERS = dict(zip(gen.ORDER_INVARIANTS, (
    reorder(dispatch_ledger=({"acquire": 2, "complete": 10}, 5)),
    reorder(dispatch_ledger=({"acquire": 1, "complete": 9}, 5)),
    reorder(resolution_proof=({"pending": 3, "complete": 9}, 1),
            resolution_fence=({"pending": 2, "batch": 7, "complete": 8}, 6)),
    reorder(resolution_fence=({"pending": 4, "batch": 7, "complete": 8}, 6)),
    reorder(tenant_index=({"pending": 5, "merge": 6}, 4)),
    reorder(conversation=({"write": 6}, 3)),
    reorder(resolution_selection=({"batch": 8}, 7)),
    reorder(resolution_fence=({"pending": 3, "batch": 7, "complete": 9}, 6),
            resolution_proof=({"pending": 2, "complete": 8}, 1)),
    reorder(resolution_selection=({"batch": 7}, 8)),
    reorder(resolution_proof=({"pending": 2, "complete": 9}, 7), resolution_metadata=({"batch": 7}, 1),
            conversation=({"write": 5}, 2), tenant_index=({"pending": 4, "merge": 6}, 3),
            dispatch_ledger=({"acquire": 1, "complete": 10}, 4),
            resolution_fence=({"pending": 3, "batch": 7, "complete": 8}, 5), resolution_selection=({"batch": 7}, 6)),
    reorder(conversation=({"write": 5}, 4), tenant_index=({"pending": 4, "merge": 6}, 3)),
), strict=True))


def test_lifecycle_fault_matrix_decided_order_has_no_violation() -> None:
    assert gen.order_violations(decided_orders()) == []


@pytest.mark.parametrize("invariant", gen.ORDER_INVARIANTS)
def test_lifecycle_fault_matrix_order_invariant_isolated(invariant: str) -> None:
    assert gen.order_violations(ORDER_REORDERS[invariant]()) == [invariant], FAULT_NOT_DETECTED


def test_lifecycle_fault_matrix_rebuild_invariants_pin_the_exact_sequence() -> None:
    """Every rebuild-plan assignment other than proof 1 ... selection 7 violates a rebuild invariant."""
    decided = decided_orders()
    rebuild_names = set(gen.ORDER_INVARIANTS[-3:])
    passing = []
    for positions in itertools.permutations(range(1, len(gen.FAMILIES) + 1)):
        orders = {family: (decided[family][0], positions[index]) for index, family in enumerate(gen.FAMILIES)}
        if not rebuild_names & set(gen.order_violations(orders)):
            passing.append({family: orders[family][1] for family in gen.FAMILIES})
    assert passing == [{family: gen.REBUILD_SEQUENCE.index(family) + 1 for family in gen.FAMILIES}]
