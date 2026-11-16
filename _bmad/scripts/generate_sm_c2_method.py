#!/usr/bin/env python3
"""Generate the predeclared Story 12.2 SM-C2 method. Exit 0 PASS, 1 FAIL, 2 BLOCKED.

Approval is outside the canonical payload. This declares a method, never a measured verdict.
The v1 observations are used only for algorithm conformance and immutable provenance.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
from decimal import Decimal
from functools import lru_cache
import hashlib
import json
import os
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import authoring_outputs as output_writer
import generate_story_record as records
from generate_derived_key_decision import canonical, frontmatter_value, sha256_bytes, valid_utc

SCHEMA_VERSION = "hexalith.conversations.sm-c2-measurement-method.v2"
PREFIX = "docs/release-evidence/sm-c2-measurement-method-v2"
SCHEMA_PATH, JSON_PATH, MARKDOWN_PATH = (PREFIX + suffix for suffix in (".schema.json", ".json", ".md"))
SCHEMA_SHA256 = "73238d2795ea20f19f0918269e6333e160fb9e3894e9ddeaa003f7fa0dae61d8"
CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/12.2.json"
AUTHORITY_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
EPICS_PATH = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md"
BASELINE_PATH = "docs/release-evidence/sm-c2-hot-path-baseline-v1.json"
BASELINE_FILES = (BASELINE_PATH, BASELINE_PATH[:-5] + ".md", BASELINE_PATH[:-5] + ".xunit.xml")
POST_PATH = "docs/release-evidence/sm-c2-hot-path-post-v1.json"
FIXTURE_PATH = "tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs"
STORY_SPEC = "_bmad-output/implementation-artifacts/spec-12-1-approve-derived-key-ownership-lifecycle-and-rollback.md"
SPRINT_PATH = "_bmad-output/implementation-artifacts/sprint-status.yaml"
SPRINT_KEY = "12-1-approve-derived-key-ownership-lifecycle-and-rollback"
DECISION_PATH = "docs/release-evidence/derived-key-lifecycle-decision-v1.json"
DECISION_MD = DECISION_PATH[:-5] + ".md"
STORY_COMMIT = "455b8dea499fef5b0e5e6f8426badbb047ef3edf"
PLANNING_COMMIT = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
FIXTURE_BASIS_COMMIT = "eea3a88146d5d910be2fa17a5f71cdf7aecef978"
FIXTURE_BASIS_SHA = "f02c4b5d103c591656d4d78181269ef3981e9af6f757200632d13a4038697eff"
FIXTURE_V1_SHA = "1a43bacc98fb288a81578893679b6422e1088d1398722f89669f80058688d67b"
FIXTURE_MD_SHA = "4838a5a174b247e3aa4cc6be0c181049a46d884612058df248af8892adaf3fff"
DECISION_DIGEST = "13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe"
ROWS = ("HP-APPEND", "HP-CREATE", "HP-LIST", "HP-OPEN")
INVENTORY_ROWS = ("V8-6.11-AC5", "V8-6.11-PROHIBITION-NO-THRESHOLD-CHANGE",
                  "V8-6.11-PROHIBITION-NO-ADVERSE-SAMPLE-DISCARD")
REPETITIONS = {"HP-APPEND": 400, "HP-CREATE": 1200, "HP-LIST": 200, "HP-OPEN": 200}
MASK = (1 << 64) - 1
BLOCK_INCOMPLETE = "SMC2_METHOD_INCOMPLETE"
BLOCK_THRESHOLD = "SMC2_THRESHOLD_MUTATED"
BLOCK_RETENTION = "SMC2_SAMPLE_DISCARDED"
BLOCK_ENVIRONMENT = "SMC2_ENVIRONMENT_UNCONTROLLED"
BLOCK_SIGNAL = "SMC2_SIGNAL_RULE_MUTATED"
BLOCK_APPROVAL = "SMC2_APPROVAL_MISSING"
BLOCK_POSTDATED = "SMC2_APPROVAL_POSTDATED"
CODE_ORDER = (BLOCK_INCOMPLETE, BLOCK_THRESHOLD, BLOCK_RETENTION, BLOCK_ENVIRONMENT,
              BLOCK_SIGNAL, BLOCK_APPROVAL, BLOCK_POSTDATED)
HISTORY_UNAVAILABLE = "HISTORY_UNAVAILABLE"
INPUT_UNAVAILABLE = "INPUT_UNAVAILABLE"
OUTPUT_WRITE_FAILED = "OUTPUT_WRITE_FAILED"
ARGUMENT_INVALID = "ARGUMENT_INVALID"
TOOLING_UNAVAILABLE = "TOOLING_UNAVAILABLE"


class Blocked(Exception):
    """A required input/history could not be evaluated."""
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code, self.message = code, message


class MethodError(Exception):
    """An evaluated method or binding violates the frozen contract."""
    def __init__(self, errors: list[tuple[str, str]]):
        self.errors = order_errors(errors)
        super().__init__(str(self.errors))


def order_errors(errors: list[tuple[str, str]]) -> list[tuple[str, str]]:
    first = dict(reversed(errors))
    return [(code, first[code]) for code in sorted(first, key=lambda code: CODE_ORDER.index(code)
                                                 if code in CODE_ORDER else len(CODE_ORDER))]


def read_file(repository: Path, relative: str) -> bytes:
    # The shared output resolver also rejects traversal and symlink parents for read-only inputs.
    try:
        return output_writer.output_target(repository, relative).read_bytes()
    except (OSError, output_writer.OutputError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"cannot read {relative}: {error}") from None


def ignore_replace_objects() -> None:
    """Read true history: like the other evidence tools, never follow refs/replace substitutions."""
    os.environ["GIT_NO_REPLACE_OBJECTS"] = "1"


def git(repository: Path, *arguments: str, allowed: tuple[int, ...] = (0,)):
    try:
        return records.run_git(repository, *arguments, allowed_returncodes=allowed)
    except records.GateError as error:
        raise Blocked(HISTORY_UNAVAILABLE, str(error)) from None


def resolve_commit(repository: Path, revision: str) -> str:
    result = git(repository, "rev-parse", "--verify", "--quiet", f"{revision}^{{commit}}", allowed=(0, 1, 128))
    commit = result.stdout.decode().strip()
    if result.returncode or not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise Blocked(HISTORY_UNAVAILABLE, f"revision {revision} does not resolve to a commit")
    return commit


def committed_blob(repository: Path, commit: str, path: str) -> bytes | None:
    try:
        return records.v2_committed_blob(repository, commit, path)
    except records.GateError as error:
        raise Blocked(HISTORY_UNAVAILABLE, str(error)) from None


def file_binding(repository: Path, path: str) -> dict:
    return {"path": path, "sha256": sha256_bytes(read_file(repository, path))}


def load_schema(repository: Path, expected_sha256: str = SCHEMA_SHA256) -> dict:
    """Validate the metaschema before requiring the exact approved schema bytes."""
    try:
        import jsonschema
    except ImportError:
        raise Blocked(TOOLING_UNAVAILABLE, "jsonschema is unavailable") from None
    raw = read_file(repository, SCHEMA_PATH)
    try:
        schema = json.loads(raw)
        if not isinstance(schema, dict):
            raise ValueError("the method schema must be an object")
        jsonschema.Draft202012Validator.check_schema(schema)
    except (ValueError, jsonschema.SchemaError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"the method schema cannot be applied: {error}") from None
    if sha256_bytes(raw) != expected_sha256:
        raise MethodError([(BLOCK_INCOMPLETE, "The schema differs from the exact approved schema bytes.")])
    return schema


def bind_candidate(repository: Path) -> dict:
    errors = []
    top = git(repository, "rev-parse", "--show-toplevel").stdout.decode().strip()
    if Path(top).resolve() != repository:
        raise Blocked(HISTORY_UNAVAILABLE, "the repository must own its Git work tree")
    if git(repository, "rev-parse", "--is-shallow-repository").stdout.strip() == b"true":
        raise Blocked(HISTORY_UNAVAILABLE, "shallow history cannot prove the immutable bindings and first method publication")
    try:
        contract_raw = read_file(repository, CONTRACT_PATH)
        contract = json.loads(contract_raw)
        authority = json.loads(read_file(repository, AUTHORITY_PATH))
        if not isinstance(authority, dict) or not isinstance(authority.get("artifacts"), list) \
                or not all(isinstance(row, dict) for row in authority["artifacts"]):
            raise ValueError("the authority bundle must contain an artifacts array of objects")
        if contract["storyId"] != "12.2" or contract["authority"]["planningCandidate"] != PLANNING_COMMIT \
                or contract["predecessors"] != ["12.1"]:
            errors.append((BLOCK_INCOMPLETE, "The Story 12.2 contract identity changed."))
        declared = [row["sha256"] for row in authority["artifacts"] if row.get("path") == CONTRACT_PATH]
        if declared != [sha256_bytes(contract_raw)]:
            errors.append((BLOCK_INCOMPLETE, "The contract differs from its authority-bundle row."))
        epics = read_file(repository, EPICS_PATH).decode()
        inventory_id = contract["inventory"]["id"]
        match = re.search(r"\*\*Frozen inventory:\*\* `" + re.escape(inventory_id)
                          + r"` contains, in order,\s+(.*?);\s+SHA-256\s+`([0-9a-f]{64})`", epics, re.S)
        rows = re.findall(r"`([A-Z0-9][A-Za-z0-9.-]*)`", match.group(1)) if match else []
        digest = sha256_bytes(("\n".join(rows) + "\n").encode())
        if inventory_id != "V9-12.2-ENTRY-v1" or rows != list(INVENTORY_ROWS) \
                or digest != contract["inventory"]["sha256"] or not match or match.group(2) != digest:
            errors.append((BLOCK_INCOMPLETE, "The exact entry inventory does not recompute."))
    except (ValueError, KeyError, TypeError, UnicodeDecodeError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"unreadable Story 12.2 authority inputs: {error}") from None
    planning = resolve_commit(repository, PLANNING_COMMIT)
    predecessor = resolve_commit(repository, STORY_COMMIT)
    basis = resolve_commit(repository, FIXTURE_BASIS_COMMIT)
    if git(repository, "merge-base", "--is-ancestor", planning, predecessor, allowed=(0, 1)).returncode:
        errors.append((BLOCK_INCOMPLETE, "The predecessor does not descend from the planning candidate."))
    immutable = []
    for path in (*BASELINE_FILES, POST_PATH):
        raw = read_file(repository, path)
        if committed_blob(repository, planning, path) != raw:
            errors.append((BLOCK_INCOMPLETE, f"Immutable v1 input changed: {path}."))
        immutable.append({"path": path, "sha256": sha256_bytes(raw)})
    fixture = committed_blob(repository, planning, FIXTURE_PATH)
    basis_blob = committed_blob(repository, basis, FIXTURE_PATH)
    if sha256_bytes(fixture or b"") != FIXTURE_V1_SHA or sha256_bytes(basis_blob or b"") != FIXTURE_BASIS_SHA:
        errors.append((BLOCK_INCOMPLETE, "A bound fixture blob differs from the frozen basis."))
    predecessor_files = []
    for path in (STORY_SPEC, DECISION_PATH, DECISION_MD):
        raw = read_file(repository, path)
        if committed_blob(repository, predecessor, path) != raw:
            errors.append((BLOCK_INCOMPLETE, f"Story 12.1 differs from its done commit: {path}."))
        predecessor_files.append({"path": path, "sha256": sha256_bytes(raw)})
    sprint_at_commit = committed_blob(repository, predecessor, SPRINT_PATH)
    row = rf"^  {re.escape(SPRINT_KEY)}: done$"
    try:
        spec = read_file(repository, STORY_SPEC).decode("utf-8")
        committed_sprint = sprint_at_commit.decode("utf-8") if sprint_at_commit is not None else ""
        live_sprint = read_file(repository, SPRINT_PATH).decode("utf-8")
    except UnicodeDecodeError as error:
        raise Blocked(INPUT_UNAVAILABLE, f"Story 12.1 spec or sprint is not valid UTF-8: {error}") from None
    if frontmatter_value(spec, "status") != "done" or sprint_at_commit is None \
            or not re.search(row, committed_sprint, re.M) or not re.search(row, live_sprint, re.M):
        errors.append((BLOCK_INCOMPLETE, "Story 12.1's spec and sprint row must both read done."))
    try:
        decision = json.loads(read_file(repository, DECISION_PATH))
        if not isinstance(decision, dict):
            raise ValueError("the predecessor decision must be an object")
        payload = {key: value for key, value in decision.items() if key not in ("approval", "digests")}
        actual = sha256_bytes(canonical(payload))
        approval = decision["approval"]
        if actual != DECISION_DIGEST or decision["digests"]["signablePayloadSha256"] != actual \
                or not approval_valid(approval, actual) \
                or approval["boundAdrSha256"] != decision["candidate"]["adr"]["sha256"] \
                or decision["digests"]["markdownSha256"] != sha256_bytes(read_file(repository, DECISION_MD)):
            errors.append((BLOCK_INCOMPLETE, "The Story 12.1 approved decision pair is invalid."))
    except (ValueError, KeyError, TypeError):
        errors.append((BLOCK_INCOMPLETE, "The Story 12.1 approved decision pair is incomplete."))
    if errors:
        raise MethodError(errors)
    return {
        "storyId": "12.2", "contract": file_binding(repository, CONTRACT_PATH), "planningCandidate": planning,
        "rule": "Bind the authority-bundle contract, exact entry inventory, immutable planning blobs and Story 12.1 done commit; never bind HEAD. Unresolvable history is BLOCKED.",
        "inventory": {"id": inventory_id, "rows": rows, "sha256": digest},
        "predecessor": {"storyId": "12.1", "commit": predecessor, "files": predecessor_files,
                        "sprint": {"path": SPRINT_PATH, "key": SPRINT_KEY, "state": "done",
                                   "committedSha256": sha256_bytes(sprint_at_commit)}},
        "immutableProvenance": immutable,
        "planningFixture": {"path": FIXTURE_PATH, "commit": planning, "sha256": FIXTURE_V1_SHA},
        "fixtureBasis": {"path": FIXTURE_PATH, "commit": basis, "sha256": FIXTURE_BASIS_SHA},
        "provenanceBlockers": [{"code": "BASELINE_V1_FIXTURE_HASH_DISAGREEMENT", "state": "open",
                                "jsonFixtureSha256": FIXTURE_V1_SHA, "markdownFixtureSha256": FIXTURE_MD_SHA,
                                "rule": "Preserve both immutable claims; neither resolves the disagreement."}],
        "waivedScenarios": [{"id": "AC-12.2-05", "state": "waived",
                              "decision": "Owner decision 2026-11-16; never PASS. Story 12.3 binds the done spec, done sprint row, commit and approved method pair."}],
    }


def next_splitmix(state: int) -> tuple[int, int]:
    state = (state + 0x9E3779B97F4A7C15) & MASK
    value = ((state ^ (state >> 30)) * 0xBF58476D1CE4E5B9) & MASK
    value = ((value ^ (value >> 27)) * 0x94D049BB133111EB) & MASK
    return state, value ^ (value >> 31)


def nearest_rank(values: list, numerator: int, denominator: int):
    return sorted(values)[(numerator * len(values) + denominator - 1) // denominator - 1]


def decimal_text(value: Decimal) -> str:
    return format(value.normalize(), "f")


@lru_cache(maxsize=8)
def bootstrap(row: str, base: tuple[str, ...], candidate: tuple[str, ...]) -> dict:
    """Use exact integer microsecond units; decimal subtraction has identical C# semantics."""
    decimals = [Decimal(value) for value in (*base, *candidate)]
    scale = 10 ** max(max(0, -value.as_tuple().exponent) for value in decimals)
    baseline = [int(Decimal(value) * scale) for value in base]
    cand = [int(Decimal(value) * scale) for value in candidate]
    seed_bytes = hashlib.sha256(f"sm-c2-measurement-method-v2|{row}".encode()).digest()[:8]
    state = int.from_bytes(seed_bytes, "big")
    differences = []
    for _ in range(10000):
        resamples = []
        for samples in (baseline, cand):
            sample = []
            for _ in samples:
                state, value = next_splitmix(state)
                sample.append(samples[value % len(samples)])
            resamples.append(nearest_rank(sample, 95, 100))
        differences.append(resamples[1] - resamples[0])
    lo, hi = (nearest_rank(differences, rank, 1000) for rank in (25, 975))
    return {"hotPathId": row, "seedHex": seed_bytes.hex(), "baselineCount": len(base), "candidateCount": len(candidate),
            "baselineP95Microseconds": decimal_text(Decimal(nearest_rank(baseline, 95, 100)) / scale),
            "candidateP95Microseconds": decimal_text(Decimal(nearest_rank(cand, 95, 100)) / scale),
            "lowerDifferenceMicroseconds": decimal_text(Decimal(lo) / scale),
            "upperDifferenceMicroseconds": decimal_text(Decimal(hi) / scale),
            "halfWidthMicroseconds": decimal_text(Decimal(hi - lo) / (2 * scale))}


def build_payload(repository: Path) -> dict:
    candidate = bind_candidate(repository)
    try:
        baseline = json.loads(read_file(repository, BASELINE_PATH), parse_float=Decimal)
        post = json.loads(read_file(repository, POST_PATH), parse_float=Decimal)
        base_rows = {row["hotPathId"]: row for row in baseline["rows"]}
        post_rows = {row["hotPathId"]: row for row in post["rows"]}
        if set(base_rows) != set(ROWS) or len(baseline["rows"]) != 4 or set(post_rows) != set(ROWS):
            raise ValueError("v1 four-row inventory")
        checks = [bootstrap(row, tuple(str(value) for value in base_rows[row]["rawMicrosecondsPerOperation"]),
                            tuple(str(value) for value in post_rows[row]["rawMicrosecondsPerOperation"])) for row in ROWS]
    except (ValueError, KeyError, TypeError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"the immutable v1 samples are unreadable: {error}") from None
    return {
        "schemaVersion": SCHEMA_VERSION,
        "candidate": candidate,
        "rows": [{"hotPathId": row, "operation": base_rows[row]["operation"], "dataShape": base_rows[row]["dataShape"],
                  "commandPath": baseline["workloadManifest"]["commandPaths"][row],
                  "threshold": {"factor": "1.05", "rule": "postP95 <= 1.05 * baselineP95", "requiresUsableSignal": True}}
                 for row in ROWS],
        "warmup": {"repetitionsPerSession": 5, "includedInRawSamples": False},
        "repetitions": {"perSide": REPETITIONS, "sessionsPerSide": 4,
                        "sessionOrder": ["B", "C", "B", "C", "B", "C", "B", "C"],
                        "perSession": {row: count // 4 for row, count in REPETITIONS.items()},
                        "operationsPerSample": 2000, "adaptive": False,
                        "pooling": "Pool every sample per side in session order.", "failedSession": "void-entire-pair",
                        "rerunSession": False, "dropSession": False},
        "retention": {"retainAllRawSamples": True, "outlierRule": "none", "trimming": "none",
                      "sampleDeletion": False, "adaptiveStopping": False, "rerunSelection": False},
        "classification": {"measured": "warm in-process production path", "coldSubstitution": False,
                           "coldResults": "Separate cold observations; never replace a warm result."},
        "environment": {"host": {"sameHostForBothSides": True,
                                 "recordBeforeObservation": ["kernel", "cpuModel", "logicalProcessors"]},
                        "sdk": "11.0.100", "runtime": "11.0.0", "configuration": "Release", "concurrency": 1,
                        "runnerFlags": ["-parallel", "none", "-maxThreads", "1"],
                        "forbiddenOverridePrefixes": ["DOTNET_", "COMPlus_"],
                        "forbiddenConcurrentProcesses": ["dotnet", "Aspire", "daprd"],
                        "captureBeforeObservation": True, "driftVoidsPair": True},
        "percentile": {"method": "nearest-rank", "level": "0.95", "index": "ceil(0.95*n)-1", "interpolation": False},
        "comparability": {"envelopeRevision": "sm-c2-envelope-v2", "remeasureBothSidesAsPair": True,
                          "baselineSourceCommit": baseline["sourceCommit"],
                          "baselineEventStoreCommit": baseline["reconstruction"]["baselineEventStoreCommit"],
                          "fixtureBasisSha256": FIXTURE_BASIS_SHA,
                          "fixtureOverlayDifference": "Only repetition constants differ between the two sides; production source is the intended comparison delta.",
                          "v1InputRole": "immutable provenance only; never comparison input",
                          "frozenMembers": ["workload/data shape", "concurrency", "paired environment/runtime", "warm/cold classification"],
                          "runtimeDecision": "Owner decision 2026-11-16: applying SDK 11.0.100 and runtime 11.0.0 identically to both sides is not a target change.",
                          "openFreshnessAnchor": "ProjectionFreshnessV1 schema version, cursor and last-applied event position",
                          "firstObservation": "Story 12.4 must prove the method approval precedes its first observation."},
        "signalQuality": {"method": "percentile-bootstrap", "resamples": 10000, "prng": "SplitMix64",
                          "seed": "first 8 bytes, big-endian, of SHA-256(sm-c2-measurement-method-v2|<row>)",
                          "indexDraw": "next() mod n", "drawOrder": "nBase baseline draws, then nCand candidate draws, per resample",
                          "statistic": "P95(candidate*) - P95(baseline*)", "lowerLevel": "0.025", "upperLevel": "0.975",
                          "confidenceIntervalPercentiles": "nearest-rank", "halfWidth": "(hi-lo)/2",
                          "usableRule": "halfWidth <= 0.05 * baselineP95", "maximumHalfWidthFactor": "0.05"},
        "selfChecks": {"purpose": "algorithm conformance, not a verdict", "baseline": file_binding(repository, BASELINE_PATH),
                       "candidate": file_binding(repository, POST_PATH), "vectors": checks},
    }


def signable_of(bundle: dict) -> dict:
    return {key: value for key, value in bundle.items() if key not in ("approval", "digests")}


def serialize_json(bundle: dict) -> str:
    return json.dumps(bundle, indent=2, ensure_ascii=False) + "\n"


def render_markdown(bundle: dict) -> str:
    # The entire signable payload is visible, so no frozen rule is hidden behind a prose summary.
    return ("# SM-C2 measurement method v2\n\nPredeclared method; no measured verdict.\n\n"
            + "Signable payload SHA-256: `" + sha256_bytes(canonical(signable_of(bundle))) + "`\n\n"
            + "The immutable v1 fixture-hash disagreement remains an open provenance blocker. "
              "The self-check vectors are algorithm conformance, not a verdict.\n\n"
            + "```json\n" + serialize_json(signable_of(bundle)).rstrip() + "\n```\n\n"
            + "Owner approval (outside the payload):\n\n```json\n"
            + json.dumps(bundle["approval"], indent=2, ensure_ascii=False) + "\n```\n")


def finalize(payload: dict, approval: dict | None) -> tuple[str, str]:
    bundle = {**payload, "approval": approval,
              "digests": {"signablePayloadSha256": sha256_bytes(canonical(payload)), "markdownSha256": ""}}
    markdown = render_markdown(bundle)
    bundle["digests"]["markdownSha256"] = sha256_bytes(markdown.encode())
    return serialize_json(bundle), markdown


def approval_valid(approval, digest: str) -> bool:
    return (isinstance(approval, dict) and approval.get("decision") == "approved"
            and isinstance(approval.get("approver"), str) and bool(approval["approver"].strip())
            and valid_utc(approval.get("decidedAtUtc")) and approval.get("boundSignablePayloadSha256") == digest)


def approval_postdated(repository: Path, approval: dict, digest: str) -> bool:
    """Find the earliest committed approved JSON for this digest; before publication use verification time."""
    limit = datetime.now(timezone.utc)
    commits = git(repository, "log", "--all", "--reverse", "--format=%H", "--", JSON_PATH).stdout.decode().splitlines()
    for commit in commits:
        raw = committed_blob(repository, commit, JSON_PATH)
        try:
            bundle = json.loads(raw or b"null")
            if not isinstance(bundle, dict) or sha256_bytes(canonical(signable_of(bundle))) != digest \
                    or not approval_valid(bundle.get("approval"), digest):
                continue
        except (ValueError, TypeError):
            continue
        stamp = git(repository, "show", "-s", "--format=%cI", commit).stdout.decode().strip()
        limit = datetime.fromisoformat(stamp).astimezone(timezone.utc)
        break
    return datetime.strptime(approval["decidedAtUtc"], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc) > limit


def readonly_inputs() -> tuple[str, ...]:
    return (SCHEMA_PATH, CONTRACT_PATH, AUTHORITY_PATH, EPICS_PATH, *BASELINE_FILES, POST_PATH,
            STORY_SPEC, SPRINT_PATH, DECISION_PATH, DECISION_MD, FIXTURE_PATH)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    for name in ("repository", "contract", "baseline", "output-json", "output-markdown"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--approve-reviewer")
    parser.add_argument("--decided-at-utc")
    parser.add_argument("--approved-payload-sha256")
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    ignore_replace_objects()
    try:
        if [args.contract, args.baseline, args.output_json, args.output_markdown] != [CONTRACT_PATH, BASELINE_PATH, JSON_PATH, MARKDOWN_PATH]:
            raise Blocked(ARGUMENT_INVALID, "the contracted Story 12.2 input and output paths are required")
        schema = load_schema(repository)
        payload = build_payload(repository)
        digest = sha256_bytes(canonical(payload))
        approval = None
        flags = (args.approve_reviewer, args.decided_at_utc, args.approved_payload_sha256)
        if any(value is not None for value in flags):
            approval = {"decision": "approved", "approver": (args.approve_reviewer or "").strip(),
                        "decidedAtUtc": args.decided_at_utc, "boundSignablePayloadSha256": args.approved_payload_sha256}
            if not approval_valid(approval, digest):
                raise MethodError([(BLOCK_APPROVAL, "Approval must name the owner and bind the presented current payload digest and UTC time.")])
        else:
            try:
                existing = json.loads(read_file(repository, JSON_PATH)).get("approval")
                if approval_valid(existing, digest):
                    approval = existing
            except (Blocked, ValueError, AttributeError):
                pass
        if approval is not None and approval_postdated(repository, approval, digest):
            raise MethodError([(BLOCK_POSTDATED, "The approval is later than method publication or verification time.")])
        json_text, markdown = finalize(payload, approval)
        try:
            import jsonschema
        except ImportError:
            raise Blocked(TOOLING_UNAVAILABLE, "jsonschema is unavailable") from None
        if list(jsonschema.Draft202012Validator(schema).iter_errors(json.loads(json_text))):
            raise MethodError([(BLOCK_INCOMPLETE, "The generated method violates its closed schema.")])
        output_writer.write_outputs(repository, [(JSON_PATH, json_text.encode()), (MARKDOWN_PATH, markdown.encode())], readonly_inputs())
    except MethodError as error:
        for code, message in error.errors:
            print(f"{code}: {message}", file=sys.stderr)
        print("FAIL")
        return 1
    except UnicodeEncodeError as error:
        # An unencodable approval string is a method-input fault, exactly as the verifier classifies it.
        print(f"{BLOCK_INCOMPLETE}: method strings must encode as UTF-8: {error}", file=sys.stderr)
        print("FAIL")
        return 1
    except (Blocked, output_writer.OutputError, ValueError) as error:
        print(f"{getattr(error, 'code', OUTPUT_WRITE_FAILED)}: {error}", file=sys.stderr)
        print("BLOCKED")
        return 2
    print(f"PASS\nsignablePayloadSha256: {digest}\napproval: {'bound' if approval else 'pending'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
