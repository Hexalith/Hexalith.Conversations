#!/usr/bin/env python3
"""Generate the Story 12.1 derived-key lifecycle decision bundle (JSON + Markdown) from ADR 0005.

The ADR lifecycle table is parsed into a canonical signable payload. The payload binds the Story 12.1
contract and its planning candidate (never HEAD), the recomputed entry inventory, the immutable Story 6.2
record and candidate, and the SM-C2 baseline v1 files. Approval stays outside the payload and binds the
payload digest. Blocker codes: DERIVED_KEY_ADR_INCOMPLETE, EVENTSTORE_AUTHORITY_VIOLATED,
DERIVED_KEY_TRANSITION_UNSAFE, ROLLBACK_PLAN_INVALID, ADR_APPROVAL_MISSING, PREDECESSOR_CANDIDATE_MISMATCH,
ENTRY_INVENTORY_MISMATCH. Exit 0 PASS, 1 FAIL, 2 BLOCKED (unresolvable Git history or unavailable input).
"""
from __future__ import annotations

import argparse
from datetime import datetime
import hashlib
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import authoring_outputs as output_writer  # noqa: E402
import generate_story_record as records  # noqa: E402

SCHEMA_VERSION = "hexalith.conversations.derived-key-lifecycle-decision.v1"
CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/12.1.json"
ADR_PATH = "docs/adrs/0005-conversation-derived-key-lifecycle.md"
SCHEMA_PATH = "docs/release-evidence/derived-key-lifecycle-decision-v1.schema.json"
JSON_PATH = "docs/release-evidence/derived-key-lifecycle-decision-v1.json"
MARKDOWN_PATH = "docs/release-evidence/derived-key-lifecycle-decision-v1.md"
EPICS_PATH = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md"

LIFECYCLE_MARKER = "<!-- derived-key-lifecycle:table v1 -->"
EVIDENCE_MARKER = "<!-- derived-key-lifecycle:evidence v1 -->"

BLOCK_INCOMPLETE = "DERIVED_KEY_ADR_INCOMPLETE"
BLOCK_AUTHORITY = "EVENTSTORE_AUTHORITY_VIOLATED"
BLOCK_UNSAFE = "DERIVED_KEY_TRANSITION_UNSAFE"
BLOCK_ROLLBACK = "ROLLBACK_PLAN_INVALID"
BLOCK_APPROVAL = "ADR_APPROVAL_MISSING"
BLOCK_PREDECESSOR = "PREDECESSOR_CANDIDATE_MISMATCH"
BLOCK_INVENTORY = "ENTRY_INVENTORY_MISMATCH"
BLOCK_FORMAT = "EVIDENCE_FORMAT_DRIFT"
CODE_ORDER = (BLOCK_INCOMPLETE, BLOCK_AUTHORITY, BLOCK_UNSAFE, BLOCK_ROLLBACK, BLOCK_PREDECESSOR,
              BLOCK_INVENTORY, BLOCK_FORMAT, BLOCK_APPROVAL)

# Blocked (exit 2) codes: the check could not run; never a content verdict.
HISTORY_UNAVAILABLE = "HISTORY_UNAVAILABLE"
INPUT_UNAVAILABLE = "INPUT_UNAVAILABLE"
ARGUMENT_INVALID = "ARGUMENT_INVALID"
OUTPUT_WRITE_FAILED = "OUTPUT_WRITE_FAILED"
TOOLING_UNAVAILABLE = "TOOLING_UNAVAILABLE"

FAMILIES = (
    "conversation",
    "tenant-index",
    "dispatch-ledger",
    "resolution-metadata",
    "resolution-selection",
    "resolution-proof",
    "resolution-fence",
)
FIELDS = ("owner", "key", "writer", "readRepair", "order", "compatibility", "rebuild", "backfill",
          "deletion", "expiry", "rollback")
V1_FAMILIES = frozenset({"conversation", "tenant-index", "dispatch-ledger"})

KEY_TEMPLATES = {
    "conversation": "projection:conversations:<seg(tenantId)>:<seg(conversationId)>",
    "tenant-index": "projection:conversations-index:<seg(tenantId)>",
    "dispatch-ledger": "projection:conversations-dispatch:<sha256(dispatchId)>",
    "resolution-metadata": "projection:conversations-resolution:<sha256(tenantId)>:<sha256(conversationId)>",
    "resolution-selection":
        "projection:conversations-resolution-selection:<sha256(tenantId)>:<sha256(conversationId)>",
    "resolution-proof": "projection:conversations-resolution-proof:<sha256(tenantId)>:<sha256(conversationId)>",
    "resolution-fence": "projection:conversations-resolution-fence:<sha256(tenantId)>:<sha256(conversationId)>",
}

# One approved rule token per field; key templates, order and expiry are approved per family.
APPROVED = {
    "owner": "eventstore-derived",
    "writer": "projection-route",
    "readRepair": "false",
    "compatibility": "identity",
    "rebuild": "full-replay-fail-closed",
    "backfill": "none",
    "deletion": "operator-rebuild-only",
    "rollback": "redeploy-then-rebuild",
}
EXPIRY = {family: "none" for family in FAMILIES} | {"dispatch-ledger": "redelivery-window-ttl"}
FIELD_BLOCKER = {
    "owner": BLOCK_AUTHORITY,
    "writer": BLOCK_AUTHORITY,
    "readRepair": BLOCK_AUTHORITY,
    "key": BLOCK_UNSAFE,
    "order": BLOCK_UNSAFE,
    "compatibility": BLOCK_UNSAFE,
    "rebuild": BLOCK_UNSAFE,
    "backfill": BLOCK_UNSAFE,
    "deletion": BLOCK_UNSAFE,
    "expiry": BLOCK_UNSAFE,
    "rollback": BLOCK_ROLLBACK,
}
# Behavior absent from code today must be labeled proposed (Code Map: migration/backfill, production
# delete, expiry other than the ledger, rollback, an in-code grammar version).
PROPOSED_FIELDS = frozenset({"compatibility", "backfill", "deletion", "rollback"})


def expected_status(family: str, field: str) -> str:
    if field in PROPOSED_FIELDS or (field == "expiry" and family != "dispatch-ledger"):
        return "proposed"
    return "current"


ORDER_ACTIONS = {
    "conversation": ("write",),
    "tenant-index": ("pending", "merge"),
    "dispatch-ledger": ("acquire", "complete"),
    "resolution-metadata": ("batch",),
    "resolution-selection": ("batch",),
    "resolution-proof": ("pending", "complete"),
    "resolution-fence": ("pending", "batch", "complete"),
}
ORDER_TOKEN = re.compile(r"live=(?P<live>[1-9][0-9]*:[a-z]+(?:,[1-9][0-9]*:[a-z]+)*);rebuild=(?P<rebuild>[1-9][0-9]*)")
# The decided PrepareRebuildAsync plan: proof 1, metadata 2, conversation 3, tenant-index 4, dispatch-ledger 5,
# fence 6, selection 7. The last three invariants below pin it jointly, and each one stays individually detectable.
REBUILD_SEQUENCE = ("resolution-proof", "resolution-metadata", "conversation", "tenant-index", "dispatch-ledger",
                    "resolution-fence", "resolution-selection")
# Named write-order invariants of the accepted-append path. "The proof claim precedes every rich write" is not
# listed: it follows from the claim, pending-reference, write and merge chain below, so it could never fail alone.
ORDER_INVARIANTS = (
    "the dispatch ledger is acquired before any other family is written",
    "the dispatch ledger completes after every other family",
    "the proof claim precedes the fence claim",
    "the fence claim precedes the index pending reference",
    "the index pending reference precedes the conversation write",
    "the conversation write precedes the summary merge",
    "metadata, selection and fence share one batch after the summary merge",
    "the fence completes after its batch and before the proof",
    "every family appears exactly once in the rebuild plan",
    "the rebuild plan installs the proof first",
    "the rebuild plan orders metadata, conversation, tenant-index, dispatch-ledger, fence, then selection",
)

GRAMMAR = {
    "ratified": "conversations-derived-keys-v2",
    "supersedes": "conversations-derived-keys-v1",
    "migrationTreatment": "identity",
    "v1Families": [family for family in FAMILIES if family in V1_FAMILIES],
    "addedFamilies": [family for family in FAMILIES if family not in V1_FAMILIES],
    "segmentEncoding": "seg(x) is unpadded base64url (+ to -, / to _) over the UTF-8 identifier",
    "digestEncoding": "sha256(x) is the lowercase hexadecimal SHA-256 of the UTF-8 identifier",
    "stateStore": {
        "configurationKey": "EventStore:Projections:ReadModelStateStoreName",
        "legacyDefault": "statestore",
        "configurable": True,
    },
    "inCodeGrammarVersion": None,
    "scopedOut": [
        "Tenants-owned tenants-projection:v1:* keys share the store and are governed by Tenants.",
        "Duplicate-event-identity semantics stay open under epic-16-retro-item-52-reconcile-duplicate-event-identity-semantics.",
    ],
}

STORY_6_2_RECORD = "_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md"
STORY_6_2_CANDIDATE = "2971ab79efcf3ef11d4fba7b9139d7cae457a3f9"
BASELINE_V1 = (
    "docs/release-evidence/sm-c2-hot-path-baseline-v1.json",
    "docs/release-evidence/sm-c2-hot-path-baseline-v1.md",
    "docs/release-evidence/sm-c2-hot-path-baseline-v1.xunit.xml",
)
PREDECESSOR_PATHS = (STORY_6_2_RECORD, *BASELINE_V1)
PREDECESSOR_IDS = {
    STORY_6_2_RECORD: "story-6.2-record",
    BASELINE_V1[0]: "sm-c2-hot-path-baseline-v1-json",
    BASELINE_V1[1]: "sm-c2-hot-path-baseline-v1-md",
    BASELINE_V1[2]: "sm-c2-hot-path-baseline-v1-xunit",
}
AUTHORITY_BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
STORY_16_3_SPEC = ("_bmad-output/implementation-artifacts/"
                   "spec-16-3-diagnose-apphost-preflight-failures-and-prove-terminal-reconciliation-live.md")
STORY_16_3_COMMIT = "8da7f2bc8997c03a6aea3d72464369496c7b9717"
STORY_16_3_SPRINT_KEY = "16-3-diagnose-apphost-preflight-failures-and-prove-terminal-reconciliation-live"
SPRINT_STATUS_PATH = "_bmad-output/implementation-artifacts/sprint-status.yaml"

CANDIDATE_RULE = (
    "The decision candidate is the Story 12.1 contract and its planning candidate, never HEAD. The contract "
    "digest must equal its v9 authority-bundle row. Every Story 6.2 predecessor file must be byte-identical in "
    "the working tree and at the planning candidate, and the Story 6.2 candidate must be the record's "
    "file_list_commit and an ancestor of the planning candidate. Story 16.3 is bound at its done commit: the "
    "spec blob and the sprint row recorded there must both be done. Unresolvable history is BLOCKED, never PASS."
)
WAIVED_SCENARIOS = (
    {"id": "AC-12.1-01", "state": "waived",
     "decision": "Owner decision 2026-11-16: the candidate-verifier route is waived and recorded as waived, never PASS."},
    {"id": "AC-12.1-06", "state": "waived",
     "decision": "Owner decision 2026-11-16: the v2 final pair is waived and recorded as waived, never PASS; "
                 "Story 12.2 binds Story 12.1 by its done spec, sprint row and commit."},
)

UTC = re.compile(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z")
SHA = re.compile(r"[0-9a-f]{64}")
COMMIT = re.compile(r"[0-9a-f]{40}")
EVIDENCE_ID = re.compile(r"E[0-9]{2}")
TEST_NAME = re.compile(r"(?P<cls>[A-Za-z_][A-Za-z0-9_]*)\.(?P<method>[A-Za-z_][A-Za-z0-9_]*)")


def _fault_expectations() -> dict[str, tuple[str, ...]]:
    """Fault id -> exact blocker codes from the real generator (Story 12.1 AC-12.1-05 ledger)."""
    faults: dict[str, tuple[str, ...]] = {}
    for family in FAMILIES:
        faults[f"omit-family-{family}"] = (BLOCK_INCOMPLETE,)
        for field in FIELDS:
            faults[f"omit-{family}-{field}"] = (BLOCK_INCOMPLETE,)
        faults[f"second-writer-{family}"] = (BLOCK_AUTHORITY,)
        faults[f"read-repair-{family}"] = (BLOCK_AUTHORITY,)
        faults[f"authoritative-owner-{family}"] = (BLOCK_AUTHORITY,)
        faults[f"reverse-write-rollback-{family}"] = (BLOCK_ROLLBACK,)
    for field in FIELDS:
        faults[f"blank-statement-{field}"] = (BLOCK_INCOMPLETE,)
        faults[f"blank-rule-{field}"] = (BLOCK_INCOMPLETE,)
    faults.update({
        "unlabeled-proposed-rollback": (BLOCK_INCOMPLETE,),
        "current-row-without-evidence": (BLOCK_INCOMPLETE,),
        "unknown-evidence-id": (BLOCK_INCOMPLETE,),
        "evidence-test-absent": (BLOCK_INCOMPLETE,),
        "duplicate-row": (BLOCK_INCOMPLETE,),
        "malformed-order": (BLOCK_INCOMPLETE,),
        "undeclared-family": (BLOCK_UNSAFE,),
        "key-grammar-changed": (BLOCK_UNSAFE,),
        "in-place-migration": (BLOCK_UNSAFE,),
        "interrupted-rebuild-resumes-partial-plan": (BLOCK_UNSAFE,),
        "read-time-backfill": (BLOCK_UNSAFE,),
        "query-delete": (BLOCK_UNSAFE,),
        "proof-expires-with-ledger": (BLOCK_UNSAFE,),
        "ledger-never-expires": (BLOCK_UNSAFE,),
        "ledger-completes-before-proof": (BLOCK_UNSAFE,),
        "index-merge-before-conversation-write": (BLOCK_UNSAFE,),
        "rebuild-plan-proof-not-first": (BLOCK_UNSAFE,),
        "proof-completion-dropped": (BLOCK_UNSAFE,),
        "rollback-repeats-side-effects": (BLOCK_ROLLBACK,),
        "omit-lifecycle-marker": (BLOCK_INCOMPLETE,),
        "wrong-cell-count": (BLOCK_INCOMPLETE,),
        "mistyped-field": (BLOCK_INCOMPLETE,),
        "malformed-evidence-row": (BLOCK_INCOMPLETE,),
        "story-6-2-record-mutated": (BLOCK_PREDECESSOR,),
        "baseline-v1-json-mutated": (BLOCK_PREDECESSOR,),
        "baseline-v1-md-mutated": (BLOCK_PREDECESSOR,),
        "baseline-v1-xunit-mutated": (BLOCK_PREDECESSOR,),
        "contract-edited": (BLOCK_PREDECESSOR,),
        "story-16-3-binding-commit-replaced": (BLOCK_PREDECESSOR,),
        "inventory-row-reordered": (BLOCK_INVENTORY,),
        "schema-rejects-generated-bundle": (BLOCK_FORMAT,),
        "history-unavailable": (HISTORY_UNAVAILABLE,),
        "git-unavailable": (HISTORY_UNAVAILABLE,),
        "planning-candidate-unresolvable": (HISTORY_UNAVAILABLE,),
        "approval-presented-digest-mismatch": (BLOCK_APPROVAL,),
        "non-closed-path": (ARGUMENT_INVALID,),
    })
    return faults


FAULT_EXPECTATIONS = _fault_expectations()


class Blocked(Exception):
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code
        self.message = message


class DecisionError(Exception):
    """One or more content blockers, deduplicated by code in a fixed order."""

    def __init__(self, errors: list[tuple[str, str]]):
        self.errors = order_errors(errors)
        super().__init__("; ".join(code for code, _ in self.errors))


def order_errors(errors: list[tuple[str, str]]) -> list[tuple[str, str]]:
    first: dict[str, str] = {}
    for code, message in errors:
        first.setdefault(code, message)
    return [(code, first[code]) for code in sorted(first, key=lambda c: CODE_ORDER.index(c) if c in CODE_ORDER else 99)]


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical(value) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


def read_file(repository: Path, relative: str) -> bytes:
    try:
        return (repository / relative).read_bytes()
    except OSError as error:
        raise Blocked(INPUT_UNAVAILABLE, f"cannot read {relative}: {error}") from None


def key_prefix(template: str) -> str:
    return template.split("<", 1)[0]


# ---------------------------------------------------------------- ADR parsing


def table_after(text: str, marker: str) -> list[list[str]] | None:
    """Return the data rows of the table directly after a unique marker, or None."""
    if text.count(marker) != 1:
        return None
    lines: list[str] = []
    for line in text.split(marker, 1)[1].split("\n"):
        if line.startswith("|"):
            lines.append(line)
        elif lines or line.startswith("#"):
            break
    if len(lines) < 2:
        return None
    return [[cell.strip() for cell in line.strip().strip("|").split("|")] for line in lines[2:]]


def ticked(value: str) -> str | None:
    """Return the content of exactly one backticked token, or None."""
    match = re.fullmatch(r"`([^`]*)`", value.strip())
    return match.group(1) if match else None


def parse_evidence_ids(value: str) -> list[str] | None:
    if value.strip() == "none":
        return []
    ids = []
    for part in value.split(","):
        token = ticked(part)
        if token is None or not EVIDENCE_ID.fullmatch(token):
            return None
        ids.append(token)
    return ids if ids and len(ids) == len(set(ids)) else None


def parse_adr(text: str) -> tuple[dict, list, list[tuple[str, str]]]:
    """Parse the lifecycle and evidence tables into cells keyed by (family, field)."""
    errors: list[tuple[str, str]] = []
    cells: dict[tuple[str, str], dict] = {}
    rows = table_after(text, LIFECYCLE_MARKER)
    if rows is None:
        errors.append((BLOCK_INCOMPLETE, "The ADR has no unique machine-readable lifecycle table."))
        rows = []
    for row in rows:
        if len(row) != 6:
            errors.append((BLOCK_INCOMPLETE, "A lifecycle row does not have exactly six cells."))
            continue
        family, field = ticked(row[0]), ticked(row[1])
        if family is not None and field in FIELDS and family not in FAMILIES:
            errors.append((BLOCK_UNSAFE, f"Undeclared key family `{family}` needs a versioned grammar successor."))
            continue
        if family not in FAMILIES or field not in FIELDS:
            errors.append((BLOCK_INCOMPLETE, "A lifecycle row names an unknown family or field."))
            continue
        if (family, field) in cells:
            errors.append((BLOCK_INCOMPLETE, f"Lifecycle row {family}/{field} is duplicated."))
            continue
        evidence = parse_evidence_ids(row[5])
        cells[(family, field)] = {
            "status": row[2],
            "rule": ticked(row[3]),
            "statement": row[4],
            "evidence": evidence,
        }
    evidence_tests: list[dict] = []
    legend = table_after(text, EVIDENCE_MARKER)
    if legend is None:
        errors.append((BLOCK_INCOMPLETE, "The ADR has no unique evidence table."))
        legend = []
    seen: set[str] = set()
    for row in legend:
        identity = ticked(row[0]) if len(row) == 3 else None
        test, path = (ticked(row[1]), ticked(row[2])) if len(row) == 3 else (None, None)
        if not identity or not EVIDENCE_ID.fullmatch(identity) or identity in seen or not test or not path \
                or not TEST_NAME.fullmatch(test):
            errors.append((BLOCK_INCOMPLETE, "An evidence row is malformed or duplicated."))
            continue
        seen.add(identity)
        evidence_tests.append({"id": identity, "test": test, "path": path})
    return cells, evidence_tests, errors


# ---------------------------------------------------------------- lifecycle rules


def parse_order(token: str) -> tuple[dict[str, int], int] | None:
    match = ORDER_TOKEN.fullmatch(token)
    if not match:
        return None
    live: dict[str, int] = {}
    for part in match.group("live").split(","):
        position, action = part.split(":")
        if action in live:
            return None
        live[action] = int(position)
    return live, int(match.group("rebuild"))


def order_violations(orders: dict[str, tuple[dict[str, int], int]]) -> list[str]:
    """Return every violated write-order invariant (empty when the protocol is safe)."""
    live = {(family, action): position for family, (actions, _) in orders.items() for action, position in actions.items()}
    rebuild = {family: position for family, (_, position) in orders.items()}
    p = live.__getitem__
    ledger_acquire, ledger_complete = p(("dispatch-ledger", "acquire")), p(("dispatch-ledger", "complete"))
    others = [position for key, position in live.items() if key[0] != "dispatch-ledger"]
    batch = {p(("resolution-metadata", "batch")), p(("resolution-selection", "batch")), p(("resolution-fence", "batch"))}
    after_proof = [rebuild[family] for family in REBUILD_SEQUENCE[1:]]
    checks = dict(zip(ORDER_INVARIANTS, (
        all(ledger_acquire < o for o in others),
        all(ledger_complete > o for o in others),
        p(("resolution-proof", "pending")) < p(("resolution-fence", "pending")),
        p(("resolution-fence", "pending")) < p(("tenant-index", "pending")),
        p(("tenant-index", "pending")) < p(("conversation", "write")),
        p(("conversation", "write")) < p(("tenant-index", "merge")),
        len(batch) == 1 and min(batch) > p(("tenant-index", "merge")),
        min(batch) < p(("resolution-fence", "complete")) < p(("resolution-proof", "complete")),
        sorted(rebuild.values()) == list(range(1, len(FAMILIES) + 1)),
        rebuild[REBUILD_SEQUENCE[0]] == 1,
        all(first < second for first, second in zip(after_proof, after_proof[1:])),
    ), strict=True))
    return [name for name, passed in checks.items() if not passed]


def evaluate(cells: dict, evidence_tests: list[dict]) -> list[tuple[str, str]]:
    """Apply the lifecycle rules to parsed or bundle-derived cells."""
    errors: list[tuple[str, str]] = []
    legend = {row["id"] for row in evidence_tests}
    orders: dict[str, tuple[dict[str, int], int]] = {}
    for family in FAMILIES:
        for field in FIELDS:
            cell = cells.get((family, field))
            where = f"{family}/{field}"
            if cell is None:
                errors.append((BLOCK_INCOMPLETE, f"Lifecycle field {where} is omitted."))
                continue
            rule, statement, evidence = cell.get("rule"), cell.get("statement"), cell.get("evidence")
            if not isinstance(rule, str) or not rule.strip() or not isinstance(statement, str) or not statement.strip():
                errors.append((BLOCK_INCOMPLETE, f"Lifecycle field {where} has an empty rule or statement."))
                continue
            if cell.get("status") != expected_status(family, field):
                errors.append((BLOCK_INCOMPLETE, f"Lifecycle field {where} must be labeled {expected_status(family, field)}."))
            if not isinstance(evidence, list) or any(item not in legend for item in evidence) \
                    or (cell.get("status") == "current" and not evidence):
                errors.append((BLOCK_INCOMPLETE, f"Lifecycle field {where} lacks resolvable evidence."))
            if field == "order":
                parsed = parse_order(rule)
                if parsed is None:
                    errors.append((BLOCK_INCOMPLETE, f"Lifecycle field {where} is not a machine-readable order."))
                elif tuple(sorted(parsed[0])) != tuple(sorted(ORDER_ACTIONS[family])):
                    errors.append((BLOCK_UNSAFE, f"Write order {where} changes the family's write actions."))
                else:
                    orders[family] = parsed
                continue
            approved = (KEY_TEMPLATES[family] if field == "key" else EXPIRY[family] if field == "expiry"
                        else APPROVED[field])
            if rule != approved:
                errors.append((FIELD_BLOCKER[field], f"Lifecycle field {where} uses unapproved rule `{rule}`."))
    if len(orders) == len(FAMILIES):
        for violation in order_violations(orders):
            errors.append((BLOCK_UNSAFE, "Unsafe write order: " + violation + "."))
    return errors


def check_evidence_tests(repository: Path, evidence_tests: list[dict]) -> list[tuple[str, str]]:
    """Every cited test must be declared in its cited file (file reads only)."""
    errors = []
    for row in evidence_tests:
        match = TEST_NAME.fullmatch(row["test"])
        try:
            source = (repository / row["path"]).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            source = ""
        if not match or not re.search(rf"\bclass {re.escape(match['cls'])}\b", source) \
                or not re.search(rf"\b(?:Task|void) {re.escape(match['method'])}\(", source):
            errors.append((BLOCK_INCOMPLETE, f"Evidence {row['id']} does not resolve to a declared test."))
    return errors


# ---------------------------------------------------------------- candidate binding


def git(repository: Path, *arguments: str, allowed: tuple[int, ...] = (0,)):
    try:
        return records.run_git(repository, *arguments, allowed_returncodes=allowed)
    except records.GateError as error:
        raise Blocked(HISTORY_UNAVAILABLE, str(error)) from None


def resolve_commit(repository: Path, revision: str) -> str:
    result = git(repository, "rev-parse", "--verify", "--quiet", f"{revision}^{{commit}}", allowed=(0, 1, 128))
    commit = result.stdout.decode("utf-8", "replace").strip()
    if result.returncode or not COMMIT.fullmatch(commit):
        raise Blocked(HISTORY_UNAVAILABLE, f"revision {revision} does not resolve to a commit")
    return commit


def require_repository(repository: Path) -> None:
    top = git(repository, "rev-parse", "--show-toplevel").stdout.decode("utf-8", "replace").strip()
    if not top or Path(top).resolve() != repository.resolve():
        raise Blocked(HISTORY_UNAVAILABLE, "the repository is not the root of its own Git work tree")


def committed_blob(repository: Path, commit: str, path: str) -> bytes | None:
    try:
        return records.v2_committed_blob(repository, commit, path)
    except records.GateError as error:
        raise Blocked(HISTORY_UNAVAILABLE, str(error)) from None


def frontmatter_value(text: str, key: str) -> str | None:
    if not text.startswith("---\n"):
        return None
    header = text[4:].split("\n---\n", 1)[0]
    match = re.search(rf"^{re.escape(key)}: '?([^'\n]*)'?$", header, re.M)
    return match.group(1) if match else None


def entry_inventory(repository: Path, contract: dict) -> tuple[dict, list[tuple[str, str]]]:
    epics = read_file(repository, EPICS_PATH).decode("utf-8")
    inventory_id = contract["inventory"]["id"]
    match = re.search(r"\*\*Frozen inventory:\*\* `" + re.escape(inventory_id) + r"` contains, in order,\s+(.*?);\s+SHA-256\s+`([0-9a-f]{64})`",
                      epics, re.S)
    rows = re.findall(r"`([A-Z0-9][A-Za-z0-9.-]*)`", match.group(1)) if match else []
    digest = sha256_bytes(("\n".join(rows) + "\n").encode("utf-8")) if rows else ""
    inventory = {"id": inventory_id, "rows": rows, "sha256": digest}
    if not rows or digest != contract["inventory"]["sha256"] or match.group(2) != digest:
        return inventory, [(BLOCK_INVENTORY, "The entry inventory rows do not recompute to the contract digest.")]
    return inventory, []


def bind_candidate(repository: Path, contract_bytes: bytes, adr_bytes: bytes) -> tuple[dict, list[tuple[str, str]]]:
    try:
        contract = json.loads(contract_bytes)
        planning = contract["authority"]["planningCandidate"]
        if contract["storyId"] != "12.1" or "6.2" not in contract["predecessors"] or not COMMIT.fullmatch(planning) \
                or not isinstance(contract["rollback"]["boundary"], str) or not contract["inventory"]["id"]:
            raise ValueError("contract")
    except (ValueError, KeyError, TypeError):
        raise Blocked(INPUT_UNAVAILABLE, "the story contract is not a valid Story 12.1 contract") from None
    errors: list[tuple[str, str]] = []
    inventory, inventory_errors = entry_inventory(repository, contract)
    errors += inventory_errors
    try:
        rows = json.loads(read_file(repository, AUTHORITY_BUNDLE_PATH))["artifacts"]
        bound = [row.get("sha256") for row in rows if isinstance(row, dict) and row.get("path") == CONTRACT_PATH]
    except (ValueError, KeyError, TypeError):
        raise Blocked(INPUT_UNAVAILABLE, "the v9 authority bundle has no readable artifact rows") from None
    if bound != [sha256_bytes(contract_bytes)]:
        errors.append((BLOCK_PREDECESSOR, "The Story 12.1 contract differs from its v9 authority-bundle row."))
    require_repository(repository)
    planning_commit = resolve_commit(repository, planning)
    story_commit = resolve_commit(repository, STORY_6_2_CANDIDATE)
    ancestry = git(repository, "merge-base", "--is-ancestor", story_commit, planning_commit, allowed=(0, 1))
    if ancestry.returncode:
        errors.append((BLOCK_PREDECESSOR, "The Story 6.2 candidate is not an ancestor of the planning candidate."))
    predecessors = []
    for path in PREDECESSOR_PATHS:
        content = read_file(repository, path)
        if committed_blob(repository, planning_commit, path) != content:
            errors.append((BLOCK_PREDECESSOR, f"{path} differs from its immutable planning-candidate bytes."))
        entry = {"id": PREDECESSOR_IDS[path], "storyId": "6.2", "path": path, "sha256": sha256_bytes(content)}
        if path == STORY_6_2_RECORD:
            text = content.decode("utf-8", "replace")
            if frontmatter_value(text, "status") != "done" or frontmatter_value(text, "file_list_commit") != STORY_6_2_CANDIDATE:
                errors.append((BLOCK_PREDECESSOR, "The Story 6.2 record is not done at its recorded candidate."))
            entry["candidateCommit"] = story_commit
        predecessors.append(entry)
    done_commit = resolve_commit(repository, STORY_16_3_COMMIT)
    spec = committed_blob(repository, done_commit, STORY_16_3_SPEC)
    sprint = committed_blob(repository, done_commit, SPRINT_STATUS_PATH)
    spec_done = spec is not None and frontmatter_value(spec.decode("utf-8", "replace"), "status") == "done"
    row_done = sprint is not None and re.search(rf"^  {re.escape(STORY_16_3_SPRINT_KEY)}: done$",
                                                sprint.decode("utf-8", "replace"), re.M) is not None
    if not (spec_done and row_done):
        errors.append((BLOCK_PREDECESSOR, "Story 16.3 is not done in its spec and sprint row at the binding commit."))
    predecessors.append({"id": "story-16.3-spec", "storyId": "16.3", "path": STORY_16_3_SPEC,
                         "sha256": sha256_bytes(spec or b""), "candidateCommit": done_commit})
    candidate = {
        "storyId": "12.1",
        "contract": {"path": CONTRACT_PATH, "sha256": sha256_bytes(contract_bytes)},
        "planningCandidate": planning_commit,
        "rule": CANDIDATE_RULE,
        "inventory": inventory,
        "adr": {"path": ADR_PATH, "sha256": sha256_bytes(adr_bytes)},
        "predecessors": predecessors,
        "waivedScenarios": [dict(item) for item in WAIVED_SCENARIOS],
    }
    return {"candidate": candidate, "storyRollbackBoundary": contract["rollback"]["boundary"]}, errors


# ---------------------------------------------------------------- payload


def section(cells: dict, field: str) -> list[dict]:
    return [{"family": family, **cells[(family, field)]} for family in FAMILIES]


def live_sequence(cells: dict) -> list[dict]:
    rows = []
    for family in FAMILIES:
        actions, _ = parse_order(cells[(family, "order")]["rule"])
        rows += [{"position": position, "family": family, "action": action} for action, position in actions.items()]
    return sorted(rows, key=lambda row: (row["position"], FAMILIES.index(row["family"])))


def rebuild_sequence(cells: dict) -> list[dict]:
    rows = [{"position": parse_order(cells[(family, "order")]["rule"])[1], "family": family} for family in FAMILIES]
    return sorted(rows, key=lambda row: row["position"])


def build_payload(binding: dict, cells: dict, evidence_tests: list[dict]) -> dict:
    return {
        "schemaVersion": SCHEMA_VERSION,
        "candidate": binding["candidate"],
        "keyFamilies": [{"family": family, "grammarOrigin": "conversations-derived-keys-v1" if family in V1_FAMILIES
                         else "conversations-derived-keys-v2", "keyPrefix": key_prefix(cells[(family, "key")]["rule"]),
                         "tenantSegment": "tenantId" in cells[(family, "key")]["rule"], **cells[(family, "key")]}
                        for family in FAMILIES],
        "ownership": [{"family": family, **{field: cells[(family, field)] for field in ("owner", "writer", "readRepair")}}
                      for family in FAMILIES],
        "writeOrdering": {"families": section(cells, "order"), "live": live_sequence(cells),
                          "rebuild": rebuild_sequence(cells)},
        "compatibility": {"grammar": GRAMMAR, "families": section(cells, "compatibility")},
        "rebuild": section(cells, "rebuild"),
        "backfill": section(cells, "backfill"),
        "deletion": section(cells, "deletion"),
        "expiry": section(cells, "expiry"),
        "rollback": {"storyBoundary": binding["storyRollbackBoundary"],
                     "runtime": "Redeploy the prior binaries, then rebuild from EventStore; never reverse-write "
                                "derived state and never repeat external side effects.",
                     "families": section(cells, "rollback")},
        "evidenceTests": evidence_tests,
    }


def build_signable(repository: Path, adr_bytes: bytes, contract_bytes: bytes) -> dict:
    """Parse, validate and bind the ADR. Raises DecisionError (FAIL) or Blocked (BLOCKED)."""
    try:
        text = adr_bytes.decode("utf-8")
    except UnicodeDecodeError:
        raise DecisionError([(BLOCK_INCOMPLETE, "The ADR is not UTF-8 text.")]) from None
    cells, evidence_tests, errors = parse_adr(text)
    errors += evaluate(cells, evidence_tests)
    errors += check_evidence_tests(repository, evidence_tests)
    binding, binding_errors = bind_candidate(repository, contract_bytes, adr_bytes)
    errors += binding_errors
    if errors:
        raise DecisionError(errors)
    return build_payload(binding, cells, evidence_tests)


def cells_from_bundle(bundle: dict) -> dict:
    """Recover (family, field) cells from a bundle for independent rule evaluation."""
    cells: dict[tuple[str, str], dict] = {}

    def take(row: dict, field: str, cell: dict) -> None:
        if isinstance(row, dict) and row.get("family") in FAMILIES and isinstance(cell, dict):
            cells.setdefault((row["family"], field), {k: cell.get(k) for k in ("status", "rule", "statement", "evidence")})

    for row in bundle.get("keyFamilies", []):
        take(row, "key", row)
    for row in bundle.get("ownership", []):
        for field in ("owner", "writer", "readRepair"):
            take(row, field, row.get(field) if isinstance(row, dict) else None)
    for field, rows in (("order", bundle.get("writeOrdering", {}).get("families", [])),
                        ("compatibility", bundle.get("compatibility", {}).get("families", [])),
                        ("rebuild", bundle.get("rebuild", [])), ("backfill", bundle.get("backfill", [])),
                        ("deletion", bundle.get("deletion", [])), ("expiry", bundle.get("expiry", [])),
                        ("rollback", bundle.get("rollback", {}).get("families", []))):
        for row in rows:
            take(row, field, row)
    return cells


# ---------------------------------------------------------------- rendering


def render_markdown(bundle: dict) -> str:
    cand = bundle["candidate"]
    out = ["# Derived-Key Lifecycle Decision v1", "", f"Schema: `{bundle['schemaVersion']}`", ""]
    out += ["## Candidate", "",
            f"- Story: `{cand['storyId']}`",
            f"- Contract: `{cand['contract']['path']}` (`{cand['contract']['sha256']}`)",
            f"- Planning candidate: `{cand['planningCandidate']}`",
            f"- ADR: `{cand['adr']['path']}` (`{cand['adr']['sha256']}`)",
            f"- Entry inventory: `{cand['inventory']['id']}` = "
            + ", ".join(f"`{row}`" for row in cand["inventory"]["rows"]) + f" (`{cand['inventory']['sha256']}`)",
            f"- Rule: {cand['rule']}"]
    for item in cand["predecessors"]:
        suffix = f"; candidate `{item['candidateCommit']}`" if "candidateCommit" in item else ""
        out.append(f"- Predecessor `{item['id']}` (Story {item['storyId']}): `{item['path']}` (`{item['sha256']}`){suffix}")
    for item in cand["waivedScenarios"]:
        out.append(f"- `{item['id']}`: **{item['state']}**. {item['decision']}")
    grammar = bundle["compatibility"]["grammar"]
    out += ["", "## Grammar", "",
            f"- Ratified: `{grammar['ratified']}`, superseding `{grammar['supersedes']}`; migration treatment `{grammar['migrationTreatment']}`",
            "- v1 families: " + ", ".join(f"`{f}`" for f in grammar["v1Families"]),
            "- Added families: " + ", ".join(f"`{f}`" for f in grammar["addedFamilies"]),
            f"- {grammar['segmentEncoding']}; {grammar['digestEncoding']}",
            f"- State store: configuration key `{grammar['stateStore']['configurationKey']}`, legacy default "
            f"`{grammar['stateStore']['legacyDefault']}`, configurable {str(grammar['stateStore']['configurable']).lower()}",
            "- In-code grammar version: none"]
    out += [f"- Out of scope: {item}" for item in grammar["scopedOut"]]
    out += ["", "## Key families", "", "| Family | Origin | Key | Prefix | Tenant segment |", "| --- | --- | --- | --- | --- |"]
    out += [f"| `{r['family']}` | `{r['grammarOrigin']}` | `{r['rule']}` | `{r['keyPrefix']}` | {str(r['tenantSegment']).lower()} |"
            for r in bundle["keyFamilies"]]
    out += ["", "## Write ordering", "", "Live (`ProjectAsync`):", ""]
    out += [f"- Position {r['position']}: `{r['family']}` {r['action']}" for r in bundle["writeOrdering"]["live"]]
    out += ["", "Rebuild plan (`PrepareRebuildAsync`):", ""]
    out += [f"- Position {r['position']}: `{r['family']}`" for r in bundle["writeOrdering"]["rebuild"]]
    out += ["", "## Lifecycle", "", "| Family | Field | Status | Rule | Statement | Evidence |",
            "| --- | --- | --- | --- | --- | --- |"]
    cells = cells_from_bundle(bundle)
    for family in FAMILIES:
        for field in FIELDS:
            cell = cells[(family, field)]
            evidence = ", ".join(f"`{e}`" for e in cell["evidence"]) or "none"
            out.append(f"| `{family}` | `{field}` | {cell['status']} | `{cell['rule']}` | {cell['statement']} | {evidence} |")
    rollback = bundle["rollback"]
    out += ["", "## Rollback", "", f"- Story: {rollback['storyBoundary']}", f"- Runtime: {rollback['runtime']}"]
    out += ["", "## Evidence tests", ""] + [f"- `{r['id']}`: `{r['test']}` (`{r['path']}`)" for r in bundle["evidenceTests"]]
    approval = bundle["approval"]
    out += ["", "## Approval", ""]
    if approval is None:
        out += ["- Decision: pending (no named owner approval recorded)"]
    else:
        out += [f"- Decision: {approval['decision']}", f"- Approver: {approval['approver']}",
                f"- Decided at (UTC): {approval['decidedAtUtc']}",
                f"- Bound ADR digest: `{approval['boundAdrSha256']}`",
                f"- Bound signable-payload digest: `{approval['boundSignablePayloadSha256']}`"]
    digests = bundle["digests"]
    out += ["", "## Digests", "", f"- ADR: `{digests['adrSha256']}`",
            f"- Signable payload: `{digests['signablePayloadSha256']}`", ""]
    return "\n".join(out)


def serialize_json(bundle: dict) -> str:
    return json.dumps(bundle, indent=2, ensure_ascii=False) + "\n"


def signable_of(bundle: dict) -> dict:
    return {key: value for key, value in bundle.items() if key not in ("approval", "digests")}


def finalize(signable: dict, approval: dict | None) -> tuple[str, str]:
    """Return (json_text, markdown_text) for a signable payload and an optional approval."""
    bundle = {**signable, "approval": approval,
              "digests": {"adrSha256": signable["candidate"]["adr"]["sha256"],
                          "signablePayloadSha256": sha256_bytes(canonical(signable)), "markdownSha256": ""}}
    markdown = render_markdown(bundle)
    bundle["digests"]["markdownSha256"] = sha256_bytes(markdown.encode("utf-8"))
    return serialize_json(bundle), markdown


def valid_utc(value) -> bool:
    if not isinstance(value, str) or not UTC.fullmatch(value):
        return False
    try:
        datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ")
    except ValueError:
        return False
    return True


def approval_valid(approval, signable_digest: str, adr_digest: str) -> bool:
    return (isinstance(approval, dict) and approval.get("decision") == "approved"
            and isinstance(approval.get("approver"), str) and bool(approval["approver"].strip())
            and valid_utc(approval.get("decidedAtUtc"))
            and approval.get("boundSignablePayloadSha256") == signable_digest
            and approval.get("boundAdrSha256") == adr_digest)


def readonly_inputs(evidence_tests: list[dict] | None = None) -> tuple[str, ...]:
    return (ADR_PATH, CONTRACT_PATH, SCHEMA_PATH, EPICS_PATH, AUTHORITY_BUNDLE_PATH, *PREDECESSOR_PATHS,
            *(row["path"] for row in evidence_tests or []))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--contract", required=True)
    parser.add_argument("--adr", required=True)
    parser.add_argument("--output-json", required=True)
    parser.add_argument("--output-markdown", required=True)
    parser.add_argument("--approve-reviewer", help="Record the named owner's approval of the payload digest.")
    parser.add_argument("--decided-at-utc", help="UTC instant (YYYY-MM-DDTHH:MM:SSZ) of the approval.")
    parser.add_argument("--approved-payload-sha256",
                        help="The presented signable-payload digest the owner approved; it must equal the recomputed one.")
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    try:
        given = [value.replace("\\", "/") for value in (args.contract, args.adr, args.output_json, args.output_markdown)]
        if given != [CONTRACT_PATH, ADR_PATH, JSON_PATH, MARKDOWN_PATH]:
            raise Blocked(ARGUMENT_INVALID, "the closed Story 12.1 contract, ADR and decision paths are required")
        adr_bytes = read_file(repository, ADR_PATH)
        contract_bytes = read_file(repository, CONTRACT_PATH)
        try:
            schema = json.loads(read_file(repository, SCHEMA_PATH))
        except ValueError:
            raise Blocked(INPUT_UNAVAILABLE, "the decision schema is not JSON") from None
        signable = build_signable(repository, adr_bytes, contract_bytes)
    except DecisionError as error:
        for code, message in error.errors:
            print(f"{code}: {message}", file=sys.stderr)
        print("FAIL")
        return 1
    except Blocked as error:
        print(f"{error.code}: {error.message}", file=sys.stderr)
        print("BLOCKED")
        return 2

    digest = sha256_bytes(canonical(signable))
    adr_digest = signable["candidate"]["adr"]["sha256"]
    approval = None
    if args.approve_reviewer is not None:
        if not args.approve_reviewer.strip() or not valid_utc(args.decided_at_utc):
            print(f"{ARGUMENT_INVALID}: approval needs a named reviewer and a UTC --decided-at-utc", file=sys.stderr)
            print("BLOCKED")
            return 2
        if args.approved_payload_sha256 != digest:
            print(f"{BLOCK_APPROVAL}: the approved digest is not the recomputed signable payload {digest}", file=sys.stderr)
            print("FAIL")
            return 1
        approval = {"decision": "approved", "approver": args.approve_reviewer.strip(),
                    "decidedAtUtc": args.decided_at_utc, "boundAdrSha256": adr_digest,
                    "boundSignablePayloadSha256": digest}
    else:  # preserve a still-bound existing approval so regeneration is idempotent
        try:
            existing = json.loads(output_writer.output_target(repository, JSON_PATH).read_text(encoding="utf-8"))["approval"]
            if approval_valid(existing, digest, adr_digest):
                approval = existing
        except output_writer.OutputError as error:
            print(f"{OUTPUT_WRITE_FAILED}: {error}", file=sys.stderr)
            print("BLOCKED")
            return 2
        except (OSError, ValueError, KeyError, TypeError):
            approval = None

    json_text, markdown = finalize(signable, approval)
    try:
        import jsonschema  # noqa: PLC0415
    except ImportError:
        print(f"{TOOLING_UNAVAILABLE}: the jsonschema package is unavailable", file=sys.stderr)
        print("BLOCKED")
        return 2
    try:
        jsonschema.Draft202012Validator(schema).validate(json.loads(json_text))
    except jsonschema.ValidationError as error:
        print(f"{BLOCK_FORMAT}: the generated bundle violates the closed schema: {error.message}", file=sys.stderr)
        print("FAIL")
        return 1
    except Exception as error:  # an unusable schema is a blocked environment, not a content verdict
        print(f"{TOOLING_UNAVAILABLE}: the decision schema cannot be applied: {error}", file=sys.stderr)
        print("BLOCKED")
        return 2
    try:
        output_writer.write_outputs(repository, [(JSON_PATH, json_text.encode("utf-8")),
                                                 (MARKDOWN_PATH, markdown.encode("utf-8"))],
                                    readonly_inputs(signable["evidenceTests"]))
    except output_writer.OutputError as error:
        print(f"{OUTPUT_WRITE_FAILED}: {error}", file=sys.stderr)
        print("BLOCKED")
        return 2
    print("PASS")
    print(f"signablePayloadSha256: {digest}")
    print("approval: " + ("bound" if approval else "pending"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
