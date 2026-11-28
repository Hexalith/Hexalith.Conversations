#!/usr/bin/env python3
"""Generate the deterministic Story 12.3 derived-key correctness proof.

Exit codes:
  0 = PASS
  1 = FAIL (with exact blocker code(s) on stderr)
  2 = BLOCKED (missing/unreadable input, history, tooling, or invalid arguments)

No final SM-C2 verdict is issued by this script. AC-12.3-08 is recorded as waived, never PASS.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parent))
import authoring_outputs as output_writer
import generate_story_record as records
from generate_derived_key_decision import canonical, frontmatter_value, sha256_bytes

SCHEMA_VERSION = "hexalith.conversations.derived-key-correctness-proof.v1"
PREFIX = "docs/release-evidence/derived-key-correctness-proof-v1"
SCHEMA_PATH = PREFIX + ".schema.json"
JSON_PATH = PREFIX + ".json"
MARKDOWN_PATH = PREFIX + ".md"
BUILD_RECEIPT_PATH = "artifacts/v9/12.3/build-receipt.json"
BUILD_CONFIGURATION = "Debug"
TARGET_FRAMEWORK = "net11.0"
LANE_PROJECTS = {
    "unit": "Hexalith.Conversations.Server.Tests",
    "integration": "Hexalith.Conversations.IntegrationTests",
    "dapr": "Hexalith.Conversations.AppHost.Tests",
}
SERVER_ASSEMBLY = "Hexalith.Conversations.Server.dll"


def lane_assembly(lane: str) -> str:
    project = LANE_PROJECTS[lane]
    return f"tests/{project}/bin/{BUILD_CONFIGURATION}/{TARGET_FRAMEWORK}/{project}.dll"

CONTRACT_PATH = "_bmad-output/planning-artifacts/v9/story-contracts/12.3.json"
EPICS_PATH = "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md"
SPRINT_PATH = "_bmad-output/implementation-artifacts/sprint-status.yaml"
ADR_PATH = "docs/adrs/0005-conversation-derived-key-lifecycle.md"
ADR_SHA256 = "2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068"
DECISION_PATH = "docs/release-evidence/derived-key-lifecycle-decision-v1.json"
DECISION_MD = "docs/release-evidence/derived-key-lifecycle-decision-v1.md"
DECISION_SIGNABLE_SHA = "13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe"
METHOD_PATH = "docs/release-evidence/sm-c2-measurement-method-v2.json"
METHOD_MD = "docs/release-evidence/sm-c2-measurement-method-v2.md"
METHOD_SIGNABLE_SHA = "7d6b2d7a6049a80eda5a578ddf097fa5a0eb0a910327dc9227791f2080174bff"
FROZEN_BENCHMARK_PATH = "tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs"
FROZEN_BENCHMARK_SHA = "f02c4b5d103c591656d4d78181269ef3981e9af6f757200632d13a4038697eff"

STORY_12_1_COMMIT = "455b8dea499fef5b0e5e6f8426badbb047ef3edf"
STORY_12_1_SPEC = "_bmad-output/implementation-artifacts/spec-12-1-approve-derived-key-ownership-lifecycle-and-rollback.md"
STORY_12_1_KEY = "12-1-approve-derived-key-ownership-lifecycle-and-rollback"

STORY_12_2_COMMIT = "dc661903b063cdf56cdca3299d881e437f095d8c"
STORY_12_2_FREEZE = "0cb5cf651e24ac69b9c60335cb0b058355b4c76f"
STORY_12_2_SPEC = "_bmad-output/implementation-artifacts/spec-12-2-freeze-the-benchmark-method-and-signal-quality-algorithm.md"
STORY_12_2_KEY = "12-2-freeze-the-benchmark-method-and-signal-quality-algorithm"

PUBLIC_CONTRACT_V4_PATH = "docs/release-evidence/public-contract-shape-bounded-read-v4.json"
PUBLIC_CONTRACT_V4_SHA = "4de710b386ff63a2316c603e40556bc78b0f61bbf1635a419cf0d436a1353d74"
FROZEN_CONTRACT_SNAPSHOTS = (
    ("docs/release-evidence/public-contract-shape-baseline-v1.json", "ebfc2f67e90ecc8a7734719c6e2673b6e8392ab2cae9956a8e98b7bf769acfca"),
    ("docs/release-evidence/public-contract-shape-story-9.2-v2.json", "0160a2be428c681d22c014afa109fc80eb97706fe6f47367ac571b9a9a7595f8"),
    ("docs/release-evidence/public-contract-shape-story-6.4-v3.json", "ee61d0e210e5d43b9ae906e0ed422cd791d24722c5074c995bf14fea3c5041ec"),
    ("docs/release-evidence/public-contract-shape-story-6.4-v3-review.json", "721eed2082089f61a7b4105bfbca263ff9e3136180ca49c2703ce1f887127c62"),
)

KEYS_PATH = "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs"
KEYS_SHA = "eeefb74362c920227bc8d3cb7c7319b8a5f2006863c8d371c87b6d7415272dd6"
READ_STORE_PATH = "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs"
READ_SIDE_PATHS = (
    READ_STORE_PATH,
    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs",
    "src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs",
    "src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs",
    "src/Hexalith.Conversations.Server/Hydration/EmptyHydrationResults.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationDetailValidationHeader.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationDetailValidationHeaderConverter.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationPageValidationFields.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationPageValidationRecord.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationPageValidationRecordConverter.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupConverter.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionJsonValidation.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupScope.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupTarget.cs",
    "src/Hexalith.Conversations.Server/Projections/ConversationIndexGenerationToken.cs",
    "src/Hexalith.Conversations.Server/Projections/IncrementalHashBufferWriter.cs",
)

EXPECTED_LANE_TESTS: dict[str, tuple[str, ...]] = {
    "unit": (
        "Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.InvalidDerivedStateShouldFailClosedWithoutRepair",
    ),
    "integration": (
        "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.TenantCollisionsShouldNeverDiscloseAcrossTenants",
        "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.RetriesOutOfOrderAndEqualPositionConflictsShouldConvergeDeterministically",
        "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.LegacyV1AndV2WithoutPersistedTokenShouldMatchPersistedGenerationToken",
        "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.ConcurrentWritesShouldConvergeWithoutLostUpdates",
        "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.DeletionInterruptedRebuildReplayAndRollbackRehearsalShouldConvergeDeterministically",
    ),
    "dapr": (
        "Hexalith.Conversations.AppHost.Tests.DerivedKeyDaprFaultTests.BoundedReadsAndCursorContinuityShouldWorkOverRealDaprRedis",
        "Hexalith.Conversations.AppHost.Tests.DerivedKeyDaprFaultTests.PartialWriteShouldFailClosedAndRetryShouldConvergeOverRealDaprRedis",
        "Hexalith.Conversations.AppHost.Tests.DerivedKeyDaprFaultTests.LatencyAndOutageShouldFailClosedWithoutRepairOverRealDaprRedis",
        "Hexalith.Conversations.AppHost.Tests.DerivedKeyDaprFaultTests.PoisonRecordsAndCrossTenantIsolationShouldNeverDiscloseOverRealDaprRedis",
        "Hexalith.Conversations.AppHost.Tests.DerivedKeyDaprFaultTests.ConcurrentWritesShouldConvergeOverRealDaprRedis",
    ),
}

CANDIDATE_BINDING_RE = re.compile(
    r"CANDIDATE_BINDING\|commit=([0-9a-f]{40})\|serverSha256=([0-9a-f]{64})\|testAssemblySha256=([0-9a-f]{64})"
)

PRODUCTION_PATHS = (
    ("src/Hexalith.Conversations.Server/Hydration/ConversationReadHydrationService.cs", "response-hydration"),
    ("src/Hexalith.Conversations.Server/Hydration/EmptyHydrationResults.cs", "hydration-empty-singleton"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationDetailValidationHeader.cs", "page-validation-detail-header-dto"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationDetailValidationHeaderConverter.cs", "page-validation-detail-header-converter"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationIndexGenerationToken.cs", "streaming-canonical-generation-token"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationPageValidationFields.cs", "page-validation-persisted-fields"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationPageValidationRecord.cs", "page-validation-record-dto"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationPageValidationRecordConverter.cs", "page-validation-record-converter"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupConverter.cs", "targeted-utf8-index-lookup-converter"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupResult.cs", "targeted-utf8-index-lookup-dto"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupScope.cs", "targeted-utf8-index-lookup-scope"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexLookupTarget.cs", "targeted-utf8-index-lookup-target"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionJsonValidation.cs", "streaming-json-validation"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexReadModel.cs", "v2-tenant-index-model"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionIndexSnapshot.cs", "tenant-index-snapshot"),
    (KEYS_PATH, "immutable-derived-key-grammar"),
    ("src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs", "v2-tenant-index-writer"),
    (READ_STORE_PATH, "fail-closed-projection-read-store"),
    ("src/Hexalith.Conversations.Server/Projections/IncrementalHashBufferWriter.cs", "pooled-incremental-hash-buffer-writer"),
    ("src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs", "tenant-safe-list-and-open-query-handler"),
)

INVENTORY_ROWS = (
    "V8-6.11-AC2",
    "V8-6.11-AC3",
    "V8-6.11-AC4",
    "V8-6.11-AC8",
    "V8-6.11-PROHIBITION-READS-NEVER-REPAIR-DURABLE-STATE",
    "V8-6.11-ROLLBACK-CORRECTNESS-PRESERVING",
)

BLOCK_FANOUT = "DERIVED_KEY_FANOUT_UNBOUNDED"
BLOCK_FAIL_CLOSED = "DERIVED_STATE_FAIL_CLOSED_VIOLATION"
BLOCK_READ_REPAIR = "READ_SIDE_REPAIR_ATTEMPTED"
BLOCK_TENANT_DISCLOSURE = "TENANT_DISCLOSURE"
BLOCK_REPLAY = "REPLAY_NONDETERMINISTIC"
BLOCK_TRANSITION = "DERIVED_KEY_TRANSITION_UNSAFE"
BLOCK_DAPR = "DAPR_CORRECTNESS_LANE_RED"
BLOCK_CONTRACT = "PUBLIC_QUERY_CONTRACT_DRIFT"
BLOCK_GATE = "CORRECTNESS_GATE_RED"

CODE_ORDER = (
    BLOCK_FANOUT,
    BLOCK_FAIL_CLOSED,
    BLOCK_READ_REPAIR,
    BLOCK_TENANT_DISCLOSURE,
    BLOCK_REPLAY,
    BLOCK_TRANSITION,
    BLOCK_DAPR,
    BLOCK_CONTRACT,
    BLOCK_GATE,
)

HISTORY_UNAVAILABLE = "HISTORY_UNAVAILABLE"
INPUT_UNAVAILABLE = "INPUT_UNAVAILABLE"
OUTPUT_WRITE_FAILED = "OUTPUT_WRITE_FAILED"
ARGUMENT_INVALID = "ARGUMENT_INVALID"
TOOLING_UNAVAILABLE = "TOOLING_UNAVAILABLE"

FORBIDDEN_READ_STORE_WRITES = (
    r"\.\s*SaveAsync\s*[<(]",
    r"\.\s*TrySaveAsync\s*[<(]",
    r"\.\s*TrySaveWithTimeToLiveAsync\s*[<(]",
    r"\.\s*ExecuteAsync\s*\(",
    r"\.\s*TryEraseAsync\s*\(",
    r"\.\s*StageAsync\s*\(",
    r"\.\s*CommitAsync\s*\(",
    r"\.\s*AbortAsync\s*\(",
    r"\.\s*(?:Recover(?:NonExpiring)?Pending|PrepareRecoveryManifest|SettleRecoveryScope|RetireRecoveryScope|ReleaseRecoveryScope|ApplyEvents|ReconcileAppliedReceipt)Async\s*[<(]",
    r"\.\s*SaveStateAsync\s*[<(]",
    r"\.\s*DeleteStateAsync\s*[<(]",
    r"\.\s*ExecuteStateTransactionAsync\s*\(",
    r"\bReadModelWritePolicy\s*\.\s*(?:UpdateAsync|MergeAsync)\s*[<(]",
)


class Blocked(Exception):
    """A required input, history, or tool is unavailable."""

    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code
        self.message = message


class ProofError(Exception):
    """One or more correctness gate blockers fired."""

    def __init__(self, errors: list[tuple[str, str]]):
        self.errors = order_errors(errors)
        super().__init__(str(self.errors))


def order_errors(errors: list[tuple[str, str]]) -> list[tuple[str, str]]:
    first = dict(reversed(errors))
    return [
        (code, first[code])
        for code in sorted(
            first,
            key=lambda code: CODE_ORDER.index(code) if code in CODE_ORDER else len(CODE_ORDER),
        )
    ]


def ignore_replace_objects() -> None:
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


def read_file(repository: Path, relative: str) -> bytes:
    try:
        return output_writer.output_target(repository, relative).read_bytes()
    except (OSError, output_writer.OutputError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"cannot read {relative}: {error}") from None


def read_committed_and_verify_clean(
    repository: Path,
    head_commit: str,
    relative: str,
    errors: list[tuple[str, str]],
) -> bytes:
    """Read a tracked input and bind its working-tree bytes to the exact committed candidate tree."""
    working = read_file(repository, relative)
    committed = committed_blob(repository, head_commit, relative)
    if committed != working:
        # Git applies the tracked attributes and configured EOL conversion to this path.
        # Hash via that same conversion, then use the committed bytes for proof digests.
        candidate_object = git(repository, "rev-parse", "--verify", f"{head_commit}:{relative}", allowed=(0, 128))
        working_object = git(
            repository, "-c", "core.autocrlf=true", f"--attr-source={head_commit}",
            "hash-object", f"--path={relative}", "--", relative,
        )
        if committed is not None and candidate_object.returncode == 0 and working_object.stdout == candidate_object.stdout:
            return committed
        errors.append(
            (
                BLOCK_GATE,
                f"Input {relative} differs from committed candidate {head_commit[:12]}; commit changes and rerun tests.",
            )
        )
    return committed if committed is not None else working


def verify_frozen_contract_snapshot(
    repository: Path, head_commit: str, relative: str, expected: str, errors: list[tuple[str, str]],
) -> str:
    drift: list[tuple[str, str]] = []
    actual = sha256_bytes(read_committed_and_verify_clean(repository, head_commit, relative, drift))
    if drift or actual != expected:
        errors.append((BLOCK_CONTRACT, f"Frozen public contract snapshot {relative} drifted from the committed candidate or approved bytes."))
    return actual


def read_root_submodules(repository: Path, head_commit: str) -> list[dict]:
    """Read all root .gitmodules declarations and bind gitlinks from the committed candidate tree and index."""
    gitmodules = read_file(repository, ".gitmodules")
    committed_gitmodules = committed_blob(repository, head_commit, ".gitmodules")
    if committed_gitmodules is None or committed_gitmodules != gitmodules:
        raise ProofError([(BLOCK_GATE, f"Root .gitmodules differs from committed candidate {head_commit[:12]}.")])

    try:
        config = subprocess.run(
            ["git", "config", "--file", "-", "--null", "--get-regexp", r"^submodule\..*\.(path|url)$"],
            input=gitmodules,
            capture_output=True,
            check=False,
        )
    except OSError as error:
        raise Blocked(TOOLING_UNAVAILABLE, f"cannot invoke git to read .gitmodules: {error}") from None
    if config.returncode != 0:
        raise Blocked(INPUT_UNAVAILABLE, "cannot parse root .gitmodules")

    settings: dict[str, dict[str, str]] = {}
    try:
        for entry in config.stdout.split(b"\0"):
            if not entry:
                continue
            key, sep, value = entry.partition(b"\n")
            if not sep:
                continue
            match = re.fullmatch(r"submodule\.(.+)\.(path|url)", key.decode("utf-8"))
            if not match:
                continue
            name, field = match.groups()
            settings.setdefault(name, {})[field] = value.decode("utf-8")

        committed_links: dict[str, str] = {}
        ls_tree = git(repository, "ls-tree", "-z", head_commit, "--", "references/").stdout
        for entry in ls_tree.split(b"\0"):
            if not entry:
                continue
            meta, sep, path_bytes = entry.partition(b"\t")
            if not sep:
                continue
            parts = meta.decode("ascii", errors="replace").split()
            if len(parts) == 3 and parts[0] == "160000" and parts[1] == "commit":
                committed_links[path_bytes.decode("utf-8")] = parts[2]

        indexed: dict[str, str] = {}
        ls_files = git(repository, "ls-files", "--stage", "-z", "--", "references/").stdout
        for entry in ls_files.split(b"\0"):
            if not entry:
                continue
            meta, sep, path_bytes = entry.partition(b"\t")
            if not sep:
                continue
            parts = meta.decode("ascii", errors="replace").split()
            if len(parts) == 3 and parts[0] == "160000" and parts[2] == "0":
                indexed[path_bytes.decode("utf-8")] = parts[1]
    except UnicodeDecodeError as error:
        raise Blocked(INPUT_UNAVAILABLE, f"root .gitmodules or gitlink entry is not valid UTF-8: {error}") from None

    rows = []
    for fields in sorted(settings.values(), key=lambda item: item.get("path", "")):
        path = fields.get("path", "")
        url = fields.get("url", "")
        gitlink = committed_links.get(path)
        staged = indexed.get(path)
        if not path or not url or not gitlink or not re.fullmatch(r"[0-9a-f]{40}", gitlink):
            raise ProofError([(BLOCK_GATE, f"Root submodule binding missing or malformed in {head_commit[:12]} for {path or '<unknown>'}.")])
        if staged != gitlink:
            raise ProofError([(BLOCK_GATE, f"Staged gitlink for {path} differs from committed candidate {head_commit[:12]}.")])
        rows.append({"path": path, "gitlink": gitlink, "url": url})
    if not rows:
        raise ProofError([(BLOCK_GATE, "No root submodules found.")])
    return rows


def read_build_receipt(repository: Path, head_commit: str, errors: list[tuple[str, str]]) -> dict:
    try:
        receipt = json.loads(read_file(repository, BUILD_RECEIPT_PATH))
    except (ValueError, UnicodeDecodeError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"invalid Story 12.3 build receipt: {error}") from None
    tree = git(repository, "rev-parse", f"{head_commit}^{{tree}}").stdout.decode("ascii").strip()
    if (
        not isinstance(receipt, dict)
        or set(receipt) != {"commit", "tree", "configuration", "targetFramework", "lanes"}
        or receipt.get("commit") != head_commit
        or receipt.get("tree") != tree
        or receipt.get("configuration") != BUILD_CONFIGURATION
        or receipt.get("targetFramework") != TARGET_FRAMEWORK
        or not isinstance(receipt.get("lanes"), dict)
        or set(receipt["lanes"]) != LANE_PROJECTS.keys()
    ):
        errors.append((BLOCK_GATE, "The Story 12.3 build receipt does not bind the required source tree and build configuration."))
    if isinstance(receipt, dict) and isinstance(receipt.get("lanes"), dict):
        return receipt["lanes"]
    return {}


def parse_trx(
    repository: Path,
    lane: str,
    relative_path: str,
    head_commit: str,
    build_lane: object,
) -> tuple[dict, list[tuple[str, str]]]:
    raw = read_file(repository, relative_path)
    try:
        root = ET.fromstring(raw)
    except ET.ParseError as error:
        raise Blocked(INPUT_UNAVAILABLE, f"cannot parse {lane} TRX {relative_path}: {error}") from None

    ns = ""
    if root.tag.startswith("{"):
        ns = root.tag.split("}", 1)[0] + "}"

    summary_el = root.find(f"{ns}ResultSummary")
    summary_outcome_ok = summary_el is not None and summary_el.attrib.get("outcome") in ("Passed", "Completed")
    counters = summary_el.find(f"{ns}Counters") if summary_el is not None else None
    if counters is None:
        raise Blocked(INPUT_UNAVAILABLE, f"{lane} TRX {relative_path} lacks ResultSummary/Counters")

    def attr_int(name: str) -> int:
        val = counters.attrib.get(name, "0")
        try:
            return int(val)
        except ValueError:
            raise Blocked(INPUT_UNAVAILABLE, f"invalid counter {name}={val!r} in {relative_path}") from None

    raw_counts = {
        name: attr_int(name)
        for name in (
            "total",
            "executed",
            "passed",
            "failed",
            "error",
            "timeout",
            "aborted",
            "inconclusive",
            "notRunnable",
            "notExecuted",
            "passedButRunAborted",
            "disconnected",
            "completed",
            "inProgress",
            "pending",
        )
    }
    has_negative = any(v < 0 for v in raw_counts.values())
    total = raw_counts["total"]
    executed = raw_counts["executed"]
    passed = raw_counts["passed"]
    failed = raw_counts["failed"] + raw_counts["error"] + raw_counts["timeout"] + raw_counts["aborted"]
    inconclusive = raw_counts["inconclusive"]
    not_runnable = raw_counts["notRunnable"]
    not_executed = raw_counts["notExecuted"]
    skipped = max(0, total - passed - failed)
    not_run = max(0, total - executed)

    # Test display names are presentation data. Match each result to its unique
    # definition and verify the execution and selected assembly from that definition.
    definitions: dict[str, tuple[str, str, str]] = {}
    bad_definition = False
    for unit_test in root.findall(f"{ns}TestDefinitions/{ns}UnitTest"):
        tid = unit_test.attrib.get("id", "")
        executions = unit_test.findall(f"{ns}Execution")
        methods = unit_test.findall(f"{ns}TestMethod")
        if not tid or tid in definitions or len(executions) != 1 or len(methods) != 1:
            bad_definition = True
            continue
        execution_id = executions[0].attrib.get("id", "")
        method = methods[0]
        class_name = method.attrib.get("className", "").split(",", 1)[0].strip()
        method_name = method.attrib.get("name", "").strip()
        code_base = method.attrib.get("codeBase", "").strip()
        if not (execution_id and class_name and method_name and code_base):
            bad_definition = True
            continue
        definitions[tid] = (execution_id, f"{class_name}.{method_name}", code_base)

    required_tests = set(EXPECTED_LANE_TESTS[lane])
    unit_results = root.findall(f"{ns}Results/{ns}UnitTestResult")
    seen_test_names: list[str] = []
    passed_test_names: set[str] = set()
    non_passing_results = 0
    per_result_bindings: list[set[tuple[str, str, str]]] = []
    seen_ids: set[str] = set()
    seen_execution_ids: set[str] = set()
    code_bases: set[str] = set()
    for item in unit_results:
        outcome = item.attrib.get("outcome", "")
        tid = item.attrib.get("testId", "")
        execution_id = item.attrib.get("executionId", "")
        definition = definitions.get(tid)
        if definition is None or tid in seen_ids or execution_id in seen_execution_ids or execution_id != definition[0]:
            bad_definition = True
        else:
            _, full_name, code_base = definition
            seen_test_names.append(full_name)
            code_bases.add(code_base)
            if outcome == "Passed":
                passed_test_names.add(full_name)
        seen_ids.add(tid)
        seen_execution_ids.add(execution_id)
        if outcome != "Passed":
            non_passing_results += 1

        # xUnit v3 emits test output in <Output><TextMessages><Message>; also support <Output><StdOut>.
        item_texts = []
        for tag in (f".//{ns}TextMessages/{ns}Message", f".//{ns}StdOut"):
            for node in item.findall(tag):
                if node.text:
                    item_texts.append(node.text)
        item_bindings = set(CANDIDATE_BINDING_RE.findall("\n".join(item_texts)))
        per_result_bindings.append(item_bindings)

    has_duplicate_results = len(set(seen_test_names)) != len(seen_test_names)
    missing_required = sorted(required_tests - passed_test_names)

    errors: list[tuple[str, str]] = []
    messages = []
    for elem in root.findall(f".//{ns}Message"):
        if elem.text:
            messages.append(elem.text)
    for elem in root.findall(f".//{ns}StdOut"):
        if elem.text:
            messages.append(elem.text)
    combined_messages = "\n".join(messages)

    # Require every UnitTestResult to emit the exact same CANDIDATE_BINDING matching head_commit.
    all_bindings = set().union(*per_result_bindings) if per_result_bindings else set()
    every_result_bound = bool(per_result_bindings) and all(len(b) == 1 for b in per_result_bindings)
    bound_commit = ""
    server_sha = ""
    test_sha = ""
    binding_ok = False
    if every_result_bound and len(all_bindings) == 1:
        bound_commit, server_sha, test_sha = next(iter(all_bindings))
        expected_assembly = repository / lane_assembly(lane)
        expected_server = expected_assembly.with_name(SERVER_ASSEMBLY)
        expected_lane = {
            "testAssemblySha256": test_sha,
            "serverAssemblySha256": server_sha,
            "trxSha256": sha256_bytes(raw),
        }
        try:
            binding_ok = (
                bound_commit == head_commit
                and build_lane == expected_lane
                and bool(code_bases)
                and all((repository / cb).resolve() == expected_assembly for cb in code_bases)
                and sha256_bytes(read_file(repository, lane_assembly(lane))) == test_sha
                and sha256_bytes(read_file(repository, expected_server.relative_to(repository).as_posix())) == server_sha
            )
        except (Blocked, OSError, ValueError):
            binding_ok = False

    is_green = (
        summary_outcome_ok
        and not has_negative
        and total > 0
        and total == executed
        and total == passed
        and total == len(unit_results)
        and failed == 0
        and inconclusive == 0
        and not_runnable == 0
        and skipped == 0
        and not_executed == 0
        and not_run == 0
        and raw_counts["passedButRunAborted"] == 0
        and raw_counts["disconnected"] == 0
        and raw_counts["completed"] in (0, executed)
        and raw_counts["inProgress"] == 0
        and raw_counts["pending"] == 0
        and non_passing_results == 0
        and not has_duplicate_results
        and not bad_definition
        and seen_ids == definitions.keys()
        and set(seen_test_names) == required_tests
        and not missing_required
        and binding_ok
    )
    if not is_green:
        for token in (
            BLOCK_FANOUT,
            BLOCK_FAIL_CLOSED,
            BLOCK_READ_REPAIR,
            BLOCK_TENANT_DISCLOSURE,
            BLOCK_REPLAY,
            BLOCK_TRANSITION,
            BLOCK_CONTRACT,
        ):
            if token in combined_messages:
                errors.append((token, f"{lane} lane reported blocker {token} in {relative_path}."))
        if lane == "dapr":
            errors.append(
                (
                    BLOCK_DAPR,
                    f"Dapr correctness lane is not green (total={total}, executed={executed}, passed={passed}, failed={failed}, skipped={skipped}, missing={len(missing_required)}, bindingOk={binding_ok}).",
                )
            )
        if missing_required:
            errors.append(
                (
                    BLOCK_GATE,
                    f"{lane} correctness lane is missing required contracted test(s): {', '.join(missing_required)}.",
                )
            )
        elif not binding_ok:
            errors.append(
                (
                    BLOCK_GATE,
                    f"{lane} correctness lane TRX ({relative_path}) is not bound to committed candidate {head_commit[:12]} (found {bound_commit[:12] if bound_commit else 'none'}).",
                )
            )
        else:
            errors.append(
                (
                    BLOCK_GATE,
                    f"{lane} correctness lane failed or had inconsistent execution/result counts ({relative_path}).",
                )
            )

    lane_record = {
        "lane": lane,
        "path": relative_path,
        "sha256": sha256_bytes(raw),
        "boundCommit": bound_commit,
        "serverAssemblySha256": server_sha,
        "testAssemblySha256": test_sha,
        "total": total,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "notRun": not_run,
        "outcome": "PASS" if is_green else "FAIL",
    }
    return lane_record, errors


def _assert_schema_closed(node: object, location: str, resolver, active_refs: frozenset[str] = frozenset()) -> None:
    from referencing.exceptions import Unresolvable

    if node is True:
        raise ProofError([(BLOCK_GATE, f"The proof schema cannot allow arbitrary values at {location}.")])
    if node is False:
        return
    if not isinstance(node, dict):
        raise Blocked(INPUT_UNAVAILABLE, f"proof schema reference does not identify a schema at {location}")
    for reference_keyword in ("$ref", "$dynamicRef"):
        reference = node.get(reference_keyword)
        if reference is None:
            continue
        if reference in active_refs:
            raise ProofError([(BLOCK_GATE, f"The proof schema cannot reference itself recursively at {location}.")])
        try:
            resolved = resolver.lookup(reference)
        except Unresolvable:
            raise Blocked(INPUT_UNAVAILABLE, f"proof schema reference {reference!r} is unresolved at {location}") from None
        _assert_schema_closed(resolved.contents, location + "." + reference_keyword, resolved.resolver, active_refs | {reference})

    node_type = node.get("type")
    types = [node_type] if isinstance(node_type, str) else node_type or []
    has_object_shape = (
        "object" in types
        or any(
            k in node
            for k in (
                "properties",
                "patternProperties",
                "required",
                "minProperties",
                "maxProperties",
                "dependentRequired",
                "dependentSchemas",
                "propertyNames",
            )
        )
    )
    is_untyped_open = (
        "type" not in node
        and "$ref" not in node
        and "$dynamicRef" not in node
        and "const" not in node
        and "enum" not in node
        and not any(k in node for k in ("allOf", "anyOf", "oneOf"))
    )
    if is_untyped_open:
        raise ProofError([(BLOCK_GATE, f"The proof schema must constrain the value type at {location}.")])
    if has_object_shape and node.get("additionalProperties") is not False:
        raise ProofError([(BLOCK_GATE, f"The proof schema must stay closed (additionalProperties: false) at {location}.")])
    if node.get("patternProperties"):
        raise ProofError([(BLOCK_GATE, f"The proof schema cannot admit pattern properties at {location}.")])

    if (
        "array" in types
        and "items" not in node
        and node.get("unevaluatedItems") is not False
        and node.get("maxItems") != 0
        and "const" not in node
        and "enum" not in node
    ):
        raise ProofError([(BLOCK_GATE, f"The proof schema must constrain array items at {location}.")])

    has_composition = any(k in node for k in ("allOf", "anyOf", "oneOf"))
    if has_composition:
        if node.get("additionalProperties") is not False and node.get("unevaluatedProperties") is not False:
            raise ProofError(
                [(BLOCK_GATE, f"Composed proof schema node must stay closed (additionalProperties/unevaluatedProperties: false) at {location}.")]
            )

    for key in ("properties", "dependentSchemas", "$defs", "definitions"):
        mapping = node.get(key)
        if isinstance(mapping, dict):
            for child_name, child_schema in mapping.items():
                _assert_schema_closed(child_schema, f"{location}.{key}.{child_name}", resolver, active_refs)

    for key in ("allOf", "anyOf", "oneOf", "prefixItems"):
        branches = node.get(key)
        if isinstance(branches, list):
            for idx, child_schema in enumerate(branches):
                _assert_schema_closed(child_schema, f"{location}.{key}[{idx}]", resolver, active_refs)

    for key in (
        "items", "additionalItems", "unevaluatedItems", "contains", "not", "if", "then", "else",
        "propertyNames", "additionalProperties", "unevaluatedProperties", "contentSchema",
    ):
        if key in node:
            _assert_schema_closed(node[key], f"{location}.{key}", resolver, active_refs)


def load_schema(repository: Path) -> dict:
    try:
        import jsonschema
        from referencing import Registry
        from referencing.jsonschema import DRAFT202012
    except ImportError:
        raise Blocked(TOOLING_UNAVAILABLE, "jsonschema is unavailable") from None
    raw = read_file(repository, SCHEMA_PATH)
    try:
        schema = json.loads(raw)
        if not isinstance(schema, dict):
            raise ValueError("proof schema must be a JSON object")
        jsonschema.Draft202012Validator.check_schema(schema)
    except (ValueError, jsonschema.SchemaError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"proof schema invalid: {error}") from None
    registry = Registry().with_resource("", DRAFT202012.create_resource(schema))
    _assert_schema_closed(schema, "$", registry.resolver())
    return schema


def check_read_store_no_repair(repository: Path, head_commit: str, errors: list[tuple[str, str]]) -> list[tuple[str, str]]:
    found: list[tuple[str, str]] = []
    for relative in READ_SIDE_PATHS:
        raw = read_committed_and_verify_clean(repository, head_commit, relative, errors)
        text = raw.decode("utf-8", errors="replace")
        for pattern in FORBIDDEN_READ_STORE_WRITES:
            if re.search(pattern, text):
                found.append((BLOCK_READ_REPAIR, f"{relative} contains forbidden write call matching {pattern}."))
                return found
    return found


def verify_predecessor(
    repository: Path,
    story_id: str,
    commit: str,
    freeze_commit: str | None,
    spec_path: str,
    sprint_key: str,
    evidence_json: str,
    evidence_md: str,
    expected_signable_sha: str,
) -> tuple[dict, list[tuple[str, str]]]:
    errors: list[tuple[str, str]] = []
    resolved = resolve_commit(repository, commit)
    resolved_freeze = resolve_commit(repository, freeze_commit) if freeze_commit else None

    files = []
    for path in (spec_path, evidence_json, evidence_md):
        raw = read_file(repository, path)
        check_commit = resolved_freeze if (resolved_freeze and path in (evidence_json, evidence_md)) else resolved
        if committed_blob(repository, check_commit, path) != raw:
            errors.append((BLOCK_GATE, f"Predecessor Story {story_id} file changed since committed baseline: {path}."))
        files.append({"path": path, "sha256": sha256_bytes(raw)})

    sprint_at_commit = committed_blob(repository, resolved, SPRINT_PATH)
    row_pattern = rf"^  {re.escape(sprint_key)}: done$"
    try:
        spec_text = read_file(repository, spec_path).decode("utf-8")
        committed_sprint = (sprint_at_commit.decode("utf-8") if sprint_at_commit is not None else "").replace("\r\n", "\n")
        live_sprint = read_file(repository, SPRINT_PATH).decode("utf-8").replace("\r\n", "\n")
    except UnicodeDecodeError as error:
        raise Blocked(INPUT_UNAVAILABLE, f"predecessor Story {story_id} spec/sprint not UTF-8: {error}") from None

    if (
        frontmatter_value(spec_text, "status") != "done"
        or sprint_at_commit is None
        or not re.search(row_pattern, committed_sprint, re.M)
        or not re.search(row_pattern, live_sprint, re.M)
    ):
        errors.append((BLOCK_GATE, f"Predecessor Story {story_id} spec and sprint row must both read done."))

    try:
        bundle = json.loads(read_file(repository, evidence_json))
        if not isinstance(bundle, dict):
            raise ValueError("predecessor evidence must be a JSON object")
        digests = bundle.get("digests")
        if not isinstance(digests, dict):
            raise ValueError("predecessor digests must be a JSON object")
        payload = {k: v for k, v in bundle.items() if k not in ("approval", "digests")}
        actual_signable = sha256_bytes(canonical(payload))
        approval = bundle.get("approval")
        if (
            actual_signable != expected_signable_sha
            or digests.get("signablePayloadSha256") != actual_signable
            or not isinstance(approval, dict)
            or approval.get("decision") != "approved"
            or approval.get("boundSignablePayloadSha256") != actual_signable
        ):
            errors.append((BLOCK_GATE, f"Predecessor Story {story_id} approved evidence digest is invalid."))
    except (ValueError, KeyError, TypeError, AttributeError):
        errors.append((BLOCK_GATE, f"Predecessor Story {story_id} evidence JSON is malformed."))

    record = {
        "storyId": story_id,
        "commit": resolved,
        "freezeCommit": resolved_freeze,
        "files": files,
        "sprint": {
            "path": SPRINT_PATH,
            "key": sprint_key,
            "state": "done",
            "committedSha256": sha256_bytes(sprint_at_commit) if sprint_at_commit is not None else "",
        },
    }
    return record, errors


def build_payload(
    repository: Path,
    unit_result_path: str,
    integration_result_path: str,
    dapr_result_path: str,
    candidate_revision: str = "HEAD",
) -> dict:
    errors: list[tuple[str, str]] = []

    top = git(repository, "rev-parse", "--show-toplevel").stdout.decode().strip()
    if Path(top).resolve() != repository:
        raise Blocked(HISTORY_UNAVAILABLE, "the repository must own its Git work tree")
    if git(repository, "rev-parse", "--is-shallow-repository").stdout.strip() == b"true":
        raise Blocked(HISTORY_UNAVAILABLE, "shallow history cannot prove Story 12.3 predecessor bindings")

    head_commit = resolve_commit(repository, candidate_revision)
    root_submodules = read_root_submodules(repository, head_commit)

    # Contract & inventory binding (V9 authority bundle is retired under 24 November execution rules).
    try:
        contract_raw = read_file(repository, CONTRACT_PATH)
        contract = json.loads(contract_raw)
        if not isinstance(contract, dict):
            raise ValueError("story contract must be a JSON object")
        inventory_obj = contract.get("inventory")
        rollback_obj = contract.get("rollback")
        if not isinstance(inventory_obj, dict):
            raise ValueError("contract inventory must be a JSON object")
        if not isinstance(rollback_obj, dict):
            raise ValueError("contract rollback must be a JSON object")
        if contract.get("storyId") != "12.3" or contract.get("predecessors") != ["12.1", "12.2"]:
            errors.append((BLOCK_GATE, "Story 12.3 contract identity or predecessors changed."))
        epics = read_file(repository, EPICS_PATH).decode("utf-8")
        inv_id = inventory_obj["id"]
        if not isinstance(inv_id, str):
            raise ValueError("contract inventory.id must be a string")
        match = re.search(
            r"\*\*Frozen inventory:\*\* `" + re.escape(inv_id)
            + r"` contains, in order,\s+(.*?);\s+SHA-256\s+`([0-9a-f]{64})`",
            epics,
            re.S,
        )
        rows = re.findall(r"`([A-Z0-9][A-Za-z0-9.-]*)`", match.group(1)) if match else []
        inv_digest = sha256_bytes(("\n".join(rows) + "\n").encode())
        if inv_id != "V9-12.3-ENTRY-v1" or rows != list(INVENTORY_ROWS) or inv_digest != inventory_obj.get("sha256") or not match or match.group(2) != inv_digest:
            errors.append((BLOCK_GATE, "Story 12.3 entry inventory V9-12.3-ENTRY-v1 does not recompute."))
        rollback_boundary = rollback_obj["boundary"]
        if not isinstance(rollback_boundary, str) or not rollback_boundary.strip():
            raise ValueError("contract rollback.boundary must be a non-empty string")
    except (ValueError, KeyError, TypeError, UnicodeDecodeError) as error:
        raise Blocked(INPUT_UNAVAILABLE, f"unreadable Story 12.3 contract inputs: {error}") from None

    # Verify contract and schema working-tree bytes match committed candidate HEAD after parsing.
    _ = read_committed_and_verify_clean(repository, head_commit, CONTRACT_PATH, errors)
    _ = read_committed_and_verify_clean(repository, head_commit, SCHEMA_PATH, errors)

    # Predecessors 12.1 & 12.2
    pred_12_1, err_12_1 = verify_predecessor(
        repository,
        "12.1",
        STORY_12_1_COMMIT,
        None,
        STORY_12_1_SPEC,
        STORY_12_1_KEY,
        DECISION_PATH,
        DECISION_MD,
        DECISION_SIGNABLE_SHA,
    )
    pred_12_2, err_12_2 = verify_predecessor(
        repository,
        "12.2",
        STORY_12_2_COMMIT,
        STORY_12_2_FREEZE,
        STORY_12_2_SPEC,
        STORY_12_2_KEY,
        METHOD_PATH,
        METHOD_MD,
        METHOD_SIGNABLE_SHA,
    )
    errors.extend(err_12_1)
    errors.extend(err_12_2)

    # ADR 0005 & frozen benchmark immutability
    adr_sha = sha256_bytes(read_committed_and_verify_clean(repository, head_commit, ADR_PATH, errors))
    if adr_sha != ADR_SHA256:
        errors.append((BLOCK_GATE, "ADR 0005 bytes changed; approval is void."))
    bench_sha = sha256_bytes(read_committed_and_verify_clean(repository, head_commit, FROZEN_BENCHMARK_PATH, errors))
    if bench_sha != FROZEN_BENCHMARK_SHA:
        errors.append((BLOCK_GATE, "Frozen SmC2HotPathBenchmark.cs bytes changed."))

    # Key grammar immutability & read-side no-repair static guard (bound to committed candidate)
    keys_bytes = read_committed_and_verify_clean(repository, head_commit, KEYS_PATH, errors)
    keys_sha = sha256_bytes(keys_bytes)
    if keys_sha != KEYS_SHA:
        errors.append((BLOCK_TRANSITION, "Immutable ConversationProjectionReadModelKeys.cs bytes changed."))
    errors.extend(check_read_store_no_repair(repository, head_commit, errors))

    # Public contract snapshot immutability
    v4_sha = verify_frozen_contract_snapshot(repository, head_commit, PUBLIC_CONTRACT_V4_PATH, PUBLIC_CONTRACT_V4_SHA, errors)
    frozen_snaps = []
    for path, expected_sha in FROZEN_CONTRACT_SNAPSHOTS:
        actual_sha = verify_frozen_contract_snapshot(repository, head_commit, path, expected_sha, errors)
        frozen_snaps.append({"path": path, "sha256": actual_sha})

    # Three correctness lanes bound to head_commit and the same tested server binary
    build_lanes = read_build_receipt(repository, head_commit, errors)
    unit_lane, unit_errors = parse_trx(repository, "unit", unit_result_path, head_commit, build_lanes.get("unit"))
    integration_lane, integration_errors = parse_trx(repository, "integration", integration_result_path, head_commit, build_lanes.get("integration"))
    dapr_lane, dapr_errors = parse_trx(repository, "dapr", dapr_result_path, head_commit, build_lanes.get("dapr"))
    errors.extend(unit_errors)
    errors.extend(integration_errors)
    errors.extend(dapr_errors)

    server_shas = {
        lane["serverAssemblySha256"]
        for lane in (unit_lane, integration_lane, dapr_lane)
        if lane["serverAssemblySha256"]
    }
    if len(server_shas) != 1 or not all(
        lane["serverAssemblySha256"] for lane in (unit_lane, integration_lane, dapr_lane)
    ):
        errors.append(
            (
                BLOCK_GATE,
                "All three correctness lanes must execute against the exact same Hexalith.Conversations.Server binary.",
            )
        )

    # Production path inventory bound to committed candidate tree
    production_inventory = []
    for path, role in sorted(PRODUCTION_PATHS, key=lambda item: item[0]):
        raw = read_committed_and_verify_clean(repository, head_commit, path, errors)
        production_inventory.append({
            "path": path,
            "sha256": sha256_bytes(raw),
            "role": role,
        })

    if errors:
        raise ProofError(errors)

    substitutions = [
        {
            "scenarioId": scenario_id,
            "contracted": "Release/net10.0",
            "executed": "Debug/net11.0",
            "reason": "24 November execution rules: producer-selected source-mapping Debug/net11.0 configuration.",
        }
        for scenario_id in ("AC-12.3-01", "AC-12.3-02", "AC-12.3-03", "AC-12.3-04", "AC-12.3-05")
    ]

    mutation_ledger = [
        {"id": "M01-fanout-unbounded", "blockerCode": BLOCK_FANOUT, "category": "bounded-reads", "enforcedBy": "AC-12.3-01 + AC-12.3-07"},
        {"id": "M02-invalid-state-fail-closed", "blockerCode": BLOCK_FAIL_CLOSED, "category": "invalid-derived-state", "enforcedBy": "AC-12.3-02 + AC-12.3-07"},
        {"id": "M03-read-side-repair", "blockerCode": BLOCK_READ_REPAIR, "category": "read-repair-prohibition", "enforcedBy": "AC-12.3-02 + AC-12.3-06 + AC-12.3-07"},
        {"id": "M04-tenant-disclosure", "blockerCode": BLOCK_TENANT_DISCLOSURE, "category": "tenant-isolation", "enforcedBy": "AC-12.3-03 + AC-12.3-04 + AC-12.3-07"},
        {"id": "M05-replay-nondeterministic", "blockerCode": BLOCK_REPLAY, "category": "replay-determinism", "enforcedBy": "AC-12.3-03 + AC-12.3-07"},
        {"id": "M06-derived-key-transition-unsafe", "blockerCode": BLOCK_TRANSITION, "category": "key-grammar-and-migration", "enforcedBy": "AC-12.3-03 + AC-12.3-06 + AC-12.3-07"},
        {"id": "M07-dapr-lane-red", "blockerCode": BLOCK_DAPR, "category": "real-dapr-redis-lane", "enforcedBy": "AC-12.3-04 + AC-12.3-06 + AC-12.3-07"},
        {"id": "M08-public-query-contract-drift", "blockerCode": BLOCK_CONTRACT, "category": "public-contract-compatibility", "enforcedBy": "AC-12.3-05 + AC-12.3-06 + AC-12.3-07"},
        {"id": "M09-correctness-gate-red", "blockerCode": BLOCK_GATE, "category": "proof-and-predecessor-gate", "enforcedBy": "AC-12.3-06 + AC-12.3-07"},
    ]

    return {
        "schemaVersion": SCHEMA_VERSION,
        "candidate": {
            "storyId": "12.3",
            "contract": {"path": CONTRACT_PATH, "sha256": sha256_bytes(contract_raw)},
            "headCommit": head_commit,
            "rootSubmodules": root_submodules,
            "inventory": {"id": inv_id, "rows": rows, "sha256": inv_digest},
            "predecessors": [pred_12_1, pred_12_2],
            "commandSubstitutions": substitutions,
            "waivedScenarios": [
                {
                    "id": "AC-12.3-08",
                    "state": "waived",
                    "decision": "Owner decision 2026-11-27 under 24 November execution rules: per-story v2 final record is waived (never PASS). Story 12.4 consumes derived-key-correctness-proof-v1.json alongside Story 12.3's done spec, sprint row, and commit.",
                }
            ],
        },
        "adrAndMethod": {
            "adrPath": ADR_PATH,
            "adrSha256": adr_sha,
            "decisionPath": DECISION_PATH,
            "decisionSha256": sha256_bytes(read_file(repository, DECISION_PATH)),
            "decisionSignablePayloadSha256": DECISION_SIGNABLE_SHA,
            "methodPath": METHOD_PATH,
            "methodSha256": sha256_bytes(read_file(repository, METHOD_PATH)),
            "methodSignablePayloadSha256": METHOD_SIGNABLE_SHA,
            "frozenBenchmarkPath": FROZEN_BENCHMARK_PATH,
            "frozenBenchmarkSha256": bench_sha,
            "noSmC2VerdictIssued": True,
        },
        "productionPathInventory": production_inventory,
        "publicContract": {
            "snapshotPath": PUBLIC_CONTRACT_V4_PATH,
            "snapshotSha256": v4_sha,
            "frozenSnapshots": frozen_snaps,
            "driftDetected": False,
        },
        "lanes": {
            "unit": unit_lane,
            "integration": integration_lane,
            "dapr": dapr_lane,
        },
        "mutationLedger": mutation_ledger,
        "replayAndRollback": {
            "replayDeterminismVerified": True,
            "rollbackStrategy": "redeploy-then-rebuild",
            "reverseWriteAllowed": False,
            "readRepairAllowed": False,
            "storyRollbackBoundary": rollback_boundary,
        },
    }


def serialize_json(bundle: dict) -> str:
    return json.dumps(bundle, indent=2, ensure_ascii=False) + "\n"


def render_markdown(payload: dict, payload_sha: str) -> str:
    lanes = payload["lanes"]
    lines = [
        "# Derived-Key Correctness Proof v1 (Story 12.3)",
        "",
        "Deterministic correctness proof within ADR 0005 (`conversations-derived-keys-v2`). No final SM-C2 performance verdict is issued.",
        "",
        f"- Schema version: `{payload['schemaVersion']}`",
        f"- Candidate `HEAD`: `{payload['candidate']['headCommit']}`",
        f"- Payload SHA-256: `{payload_sha}`",
        f"- Waived scenarios: `AC-12.3-08` (`waived`, never `PASS`)",
        "",
        "## Correctness Lanes",
        "",
        "| Lane | Result Path | Total | Passed | Failed | Skipped | Outcome |",
        "| --- | --- | ---: | ---: | ---: | ---: | --- |",
    ]
    for name in ("unit", "integration", "dapr"):
        lane = lanes[name]
        lines.append(
            f"| `{name}` | `{lane['path']}` | {lane['total']} | {lane['passed']} | {lane['failed']} | {lane['skipped']} | `{lane['outcome']}` |"
        )
    lines.extend([
        "",
        "## Authoritative Payload",
        "",
        "```json",
        serialize_json(payload).rstrip(),
        "```",
        "",
    ])
    return "\n".join(lines)


def finalize(payload: dict) -> tuple[str, str]:
    payload_sha = sha256_bytes(canonical(payload))
    markdown = render_markdown(payload, payload_sha)
    bundle = {
        **payload,
        "digests": {
            "payloadSha256": payload_sha,
            "markdownSha256": sha256_bytes(markdown.encode("utf-8")),
        },
    }
    return serialize_json(bundle), markdown


def readonly_inputs(unit_path: str, integration_path: str, dapr_path: str) -> tuple[str, ...]:
    return (
        SCHEMA_PATH,
        CONTRACT_PATH,
        EPICS_PATH,
        SPRINT_PATH,
        ADR_PATH,
        DECISION_PATH,
        DECISION_MD,
        METHOD_PATH,
        METHOD_MD,
        FROZEN_BENCHMARK_PATH,
        STORY_12_1_SPEC,
        STORY_12_2_SPEC,
        PUBLIC_CONTRACT_V4_PATH,
        *(path for path, _ in FROZEN_CONTRACT_SNAPSHOTS),
        *READ_SIDE_PATHS,
        *(path for path, _ in PRODUCTION_PATHS),
        BUILD_RECEIPT_PATH,
        unit_path,
        integration_path,
        dapr_path,
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    for flag in (
        "repository",
        "contract",
        "unit-result",
        "integration-result",
        "dapr-result",
        "output-json",
        "output-markdown",
    ):
        parser.add_argument("--" + flag, required=True)
    parser.add_argument("--candidate", default="HEAD")
    args = parser.parse_args(argv)
    repository = Path(args.repository).resolve()
    ignore_replace_objects()

    try:
        if [args.contract, args.output_json, args.output_markdown] != [CONTRACT_PATH, JSON_PATH, MARKDOWN_PATH]:
            raise Blocked(ARGUMENT_INVALID, "the contracted Story 12.3 contract and output paths are required")
        schema = load_schema(repository)
        payload = build_payload(
            repository,
            args.unit_result,
            args.integration_result,
            args.dapr_result,
            candidate_revision=args.candidate,
        )
        json_text, markdown = finalize(payload)
        try:
            import jsonschema
            from referencing import Registry
            from referencing.exceptions import Unresolvable
        except ImportError:
            raise Blocked(TOOLING_UNAVAILABLE, "jsonschema is unavailable") from None
        try:
            schema_errors = list(jsonschema.Draft202012Validator(schema, registry=Registry()).iter_errors(json.loads(json_text)))
        except Unresolvable as error:
            raise Blocked(INPUT_UNAVAILABLE, f"proof schema reference is unresolved: {error}") from None
        if schema_errors:
            raise ProofError([(BLOCK_GATE, f"Generated proof violates closed schema: {schema_errors[0].message}")])
        output_writer.write_outputs(
            repository,
            [(JSON_PATH, json_text.encode("utf-8")), (MARKDOWN_PATH, markdown.encode("utf-8"))],
            readonly_inputs(args.unit_result, args.integration_result, args.dapr_result),
            replace_tracked=(JSON_PATH, MARKDOWN_PATH),
        )
    except ProofError as error:
        for code, message in error.errors:
            print(f"{code}: {message}", file=sys.stderr)
        print("FAIL")
        return 1
    except (Blocked, output_writer.OutputError, ValueError) as error:
        print(f"{getattr(error, 'code', OUTPUT_WRITE_FAILED)}: {error}", file=sys.stderr)
        print("BLOCKED")
        return 2

    payload_sha = json.loads(json_text)["digests"]["payloadSha256"]
    print(f"PASS\npayloadSha256: {payload_sha}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
