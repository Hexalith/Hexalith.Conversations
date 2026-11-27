"""Story 12.3 real-CLI complete fault matrix (AC-12.3-07); every fault restores all fixture bytes."""
from __future__ import annotations

import json
from pathlib import Path
import shutil
import subprocess
import sys

import pytest

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "_bmad/scripts"
sys.path.insert(0, str(SCRIPTS))
import generate_derived_key_correctness_proof as gen

FAULT_NOT_DETECTED = "FAULT_NOT_DETECTED"
FIXTURE_NOT_RESTORED = "FIXTURE_NOT_RESTORED"

UNIT_TRX = "artifacts/v9/12.3/AC-12.3-02.trx"
INTEGRATION_TRX = "artifacts/v9/12.3/integration.trx"
DAPR_TRX = "artifacts/v9/12.3/dapr.trx"


def make_trx(
    lane: str,
    total: int,
    passed: int,
    failed: int = 0,
    skipped: int = 0,
    message: str = "",
    executed: int | None = None,
    test_name: str | None = None,
    force_result_outcome: str | None = None,
) -> bytes:
    exec_count = (total - skipped) if executed is None else executed
    not_executed = max(0, total - exec_count)
    default_name = gen.EXPECTED_LANE_TESTS[lane] + ("Case0" if gen.EXPECTED_LANE_TESTS[lane].endswith(".") else "")
    name = test_name or default_name
    error_block = (
        f"<Output><ErrorInfo><Message>{message}</Message></ErrorInfo></Output>"
        if message
        else ""
    )
    results = []
    count = max(0, total)
    for idx in range(count):
        if force_result_outcome is not None:
            outcome = force_result_outcome
        elif idx < passed:
            outcome = "Passed"
        elif idx < passed + failed:
            outcome = "Failed"
        else:
            outcome = "NotExecuted"
        err = error_block if (outcome != "Passed" and idx == passed) else ""
        results.append(f'<UnitTestResult testName="{name}" outcome="{outcome}">{err}</UnitTestResult>')
    xml = (
        '<?xml version="1.0" encoding="utf-8"?>'
        '<TestRun id="00000000-0000-0000-0000-000000000001" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        f'<Results>{"".join(results)}</Results>'
        "<ResultSummary>"
        f'<Counters total="{total}" executed="{exec_count}" passed="{passed}" failed="{failed}" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="{not_executed}" disconnected="0" warning="0" completed="{exec_count}" inProgress="0" pending="0" />'
        "</ResultSummary>"
        "</TestRun>\n"
    )
    return xml.encode("utf-8")


def run_cli(repo: Path, *extra_args: str) -> subprocess.CompletedProcess[str]:
    cmd = [
        sys.executable,
        str(SCRIPTS / "generate_derived_key_correctness_proof.py"),
        "--repository",
        str(repo),
        "--contract",
        gen.CONTRACT_PATH,
        "--unit-result",
        UNIT_TRX,
        "--integration-result",
        INTEGRATION_TRX,
        "--dapr-result",
        DAPR_TRX,
        "--output-json",
        gen.JSON_PATH,
        "--output-markdown",
        gen.MARKDOWN_PATH,
        *extra_args,
    ]
    return subprocess.run(cmd, cwd=repo, capture_output=True, text=True, check=False, timeout=60)


def snapshot(repo: Path) -> dict[str, bytes | None]:
    paths = [
        *gen.readonly_inputs(UNIT_TRX, INTEGRATION_TRX, DAPR_TRX),
        ".gitmodules",
        gen.JSON_PATH,
        gen.MARKDOWN_PATH,
        ".git/objects/info/alternates",
        ".git/shallow",
    ]
    return {p: (repo / p).read_bytes() if (repo / p).exists() else None for p in paths}


def restore(repo: Path, before: dict[str, bytes | None]) -> None:
    for path, raw in before.items():
        target = repo / path
        if raw is None:
            target.unlink(missing_ok=True)
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(raw)


@pytest.fixture(scope="module")
def repository(tmp_path_factory):
    repo = tmp_path_factory.mktemp("derived-key-proof") / "repo"
    # Copy all required files + gitlinks into an isolated temporary git repository sharing the real object store.
    copy_paths = [
        ".gitmodules",
        gen.SCHEMA_PATH,
        gen.CONTRACT_PATH,
        gen.AUTHORITY_PATH,
        gen.EPICS_PATH,
        gen.SPRINT_PATH,
        gen.ADR_PATH,
        gen.DECISION_PATH,
        gen.DECISION_MD,
        gen.METHOD_PATH,
        gen.METHOD_MD,
        gen.FROZEN_BENCHMARK_PATH,
        gen.STORY_12_1_SPEC,
        gen.STORY_12_2_SPEC,
        gen.PUBLIC_CONTRACT_V4_PATH,
        *(p for p, _ in gen.FROZEN_CONTRACT_SNAPSHOTS),
        *gen.READ_SIDE_PATHS,
        *(p for p, _ in gen.PRODUCTION_PATHS),
    ]
    for relative in dict.fromkeys(copy_paths):
        target = repo / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)

    # Seed valid lane TRXs (real ones if present, else synthetic green TRXs).
    for lane_name, trx_rel, count in (("unit", UNIT_TRX, 1), ("integration", INTEGRATION_TRX, 5), ("dapr", DAPR_TRX, 5)):
        target = repo / trx_rel
        target.parent.mkdir(parents=True, exist_ok=True)
        if (ROOT / trx_rel).is_file():
            shutil.copyfile(ROOT / trx_rel, target)
        else:
            target.write_bytes(make_trx(lane_name, count, count))

    subprocess.run(["git", "init", "-q", str(repo)], check=True)
    common = subprocess.run(
        ["git", "-C", str(ROOT), "rev-parse", "--path-format=absolute", "--git-common-dir"],
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()
    (repo / ".git/objects/info/alternates").write_text(str(Path(common) / "objects") + "\n", encoding="utf-8")

    head_sha = subprocess.run(
        ["git", "-C", str(ROOT), "rev-parse", "HEAD"],
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()
    subprocess.run(["git", "-C", str(repo), "update-ref", "HEAD", head_sha], check=True)

    # Populate indexed gitlinks matching ROOT's index so read_root_submodules sees exact root gitlinks.
    ls_stage = subprocess.run(
        ["git", "-C", str(ROOT), "ls-files", "--stage", "--", "references/"],
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    for line in ls_stage.splitlines():
        if not line.strip():
            continue
        meta, path = line.split("\t", 1)
        mode, sha, _ = meta.split()
        subprocess.run(
            ["git", "-C", str(repo), "update-index", "--add", "--cacheinfo", f"{mode},{sha},{path}"],
            check=True,
        )

    first = run_cli(repo)
    assert first.returncode == 0, first.stderr
    return repo


def blocker_codes(stderr: str) -> list[str]:
    return [line.split(":", 1)[0].strip() for line in stderr.splitlines() if ":" in line]


def test_complete_fault_matrix_determinism_and_waiver(repository):
    before = snapshot(repository)
    run1 = run_cli(repository)
    assert run1.returncode == 0, run1.stderr
    json1 = (repository / gen.JSON_PATH).read_bytes()
    md1 = (repository / gen.MARKDOWN_PATH).read_bytes()

    run2 = run_cli(repository)
    assert run2.returncode == 0, run2.stderr
    assert (repository / gen.JSON_PATH).read_bytes() == json1
    assert (repository / gen.MARKDOWN_PATH).read_bytes() == md1

    bundle = json.loads(json1)
    assert bundle["schemaVersion"] == "hexalith.conversations.derived-key-correctness-proof.v1"
    assert bundle["adrAndMethod"]["noSmC2VerdictIssued"] is True
    assert bundle["candidate"]["waivedScenarios"] == [
        {
            "id": "AC-12.3-08",
            "state": "waived",
            "decision": bundle["candidate"]["waivedScenarios"][0]["decision"],
        }
    ]
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize(
    "name,trx_path,payload,expected_blockers",
    [
        ("unit-empty", UNIT_TRX, make_trx("unit", 0, 0), ["CORRECTNESS_GATE_RED"]),
        ("unit-skipped", UNIT_TRX, make_trx("unit", 2, 1, skipped=1), ["CORRECTNESS_GATE_RED"]),
        ("unit-wrong-test", UNIT_TRX, make_trx("unit", 1, 1, test_name="Unrelated.TestMethod"), ["CORRECTNESS_GATE_RED"]),
        ("unit-negative-counter", UNIT_TRX, make_trx("unit", 1, 1, failed=-1), ["CORRECTNESS_GATE_RED"]),
        ("unit-not-run", UNIT_TRX, make_trx("unit", 2, 2, executed=1), ["CORRECTNESS_GATE_RED"]),
        ("unit-failed-result-item", UNIT_TRX, make_trx("unit", 1, 1, force_result_outcome="Failed"), ["CORRECTNESS_GATE_RED"]),
        ("unit-fanout-unbounded", UNIT_TRX, make_trx("unit", 2, 1, failed=1, message="DERIVED_KEY_FANOUT_UNBOUNDED: extra read"), ["DERIVED_KEY_FANOUT_UNBOUNDED", "CORRECTNESS_GATE_RED"]),
        ("unit-invalid-state", UNIT_TRX, make_trx("unit", 2, 1, failed=1, message="DERIVED_STATE_FAIL_CLOSED_VIOLATION: stale leaked"), ["DERIVED_STATE_FAIL_CLOSED_VIOLATION", "CORRECTNESS_GATE_RED"]),
        ("unit-read-repair", UNIT_TRX, make_trx("unit", 2, 1, failed=1, message="READ_SIDE_REPAIR_ATTEMPTED: write on read"), ["READ_SIDE_REPAIR_ATTEMPTED", "CORRECTNESS_GATE_RED"]),
        ("integration-empty", INTEGRATION_TRX, make_trx("integration", 0, 0), ["CORRECTNESS_GATE_RED"]),
        ("integration-wrong-test", INTEGRATION_TRX, make_trx("integration", 5, 5, test_name="Unrelated.IntegrationTest"), ["CORRECTNESS_GATE_RED"]),
        ("integration-tenant-disclosure", INTEGRATION_TRX, make_trx("integration", 5, 4, failed=1, message="TENANT_DISCLOSURE: cross-tenant cursor"), ["TENANT_DISCLOSURE", "CORRECTNESS_GATE_RED"]),
        ("integration-replay-nondeterministic", INTEGRATION_TRX, make_trx("integration", 5, 4, failed=1, message="REPLAY_NONDETERMINISTIC: rebuild mismatch"), ["REPLAY_NONDETERMINISTIC", "CORRECTNESS_GATE_RED"]),
        ("integration-transition-unsafe", INTEGRATION_TRX, make_trx("integration", 5, 4, failed=1, message="DERIVED_KEY_TRANSITION_UNSAFE: token mismatch"), ["DERIVED_KEY_TRANSITION_UNSAFE", "CORRECTNESS_GATE_RED"]),
        ("dapr-empty", DAPR_TRX, make_trx("dapr", 0, 0), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-skipped", DAPR_TRX, make_trx("dapr", 5, 4, skipped=1), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-wrong-test", DAPR_TRX, make_trx("dapr", 5, 5, test_name="Unrelated.DaprTest"), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-failed", DAPR_TRX, make_trx("dapr", 5, 4, failed=1, message="Dapr partial write fault"), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-tenant-disclosure", DAPR_TRX, make_trx("dapr", 5, 4, failed=1, message="TENANT_DISCLOSURE: poison leaked"), ["TENANT_DISCLOSURE", "DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
    ],
    ids=lambda item: item if isinstance(item, str) else None,
)
def test_complete_fault_matrix_lane_faults(repository, name, trx_path, payload, expected_blockers):
    before = snapshot(repository)
    try:
        (repository / trx_path).write_bytes(payload)
        result = run_cli(repository)
        assert result.returncode == 1, (FAULT_NOT_DETECTED, result.stdout, result.stderr)
        assert blocker_codes(result.stderr) == expected_blockers, (FAULT_NOT_DETECTED, result.stderr)
        assert "Traceback" not in result.stderr
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def _open_nested_schema(raw: bytes) -> bytes:
    schema = json.loads(raw)
    schema["properties"]["lanes"]["additionalProperties"] = True
    return json.dumps(schema).encode("utf-8")


@pytest.mark.parametrize(
    "name,target_path,mutator,expected_blockers",
    [
        (
            "read-side-repair-mutation",
            gen.READ_STORE_PATH,
            lambda raw: raw + b"\n// mutated: _store.SaveAsync(\"s\", \"k\", new object());\n",
            ["READ_SIDE_REPAIR_ATTEMPTED"],
        ),
        (
            "read-side-repair-in-query-handler",
            "src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs",
            lambda raw: raw + b"\n// mutated: ReadModelWritePolicy.MergeAsync<object>(null!, \"s\", \"k\", null!, null!, null!, null!, null!);\n",
            ["READ_SIDE_REPAIR_ATTEMPTED"],
        ),
        (
            "read-side-repair-in-read-service",
            "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs",
            lambda raw: raw + b"\n// mutated: _store.TryEraseAsync(\"s\", \"k\", \"e\");\n",
            ["READ_SIDE_REPAIR_ATTEMPTED"],
        ),
        (
            "derived-key-grammar-mutation",
            gen.KEYS_PATH,
            lambda raw: raw + b"\n// mutated key grammar\n",
            ["DERIVED_KEY_TRANSITION_UNSAFE"],
        ),
        (
            "public-contract-v4-drift",
            gen.PUBLIC_CONTRACT_V4_PATH,
            lambda raw: raw + b" ",
            ["PUBLIC_QUERY_CONTRACT_DRIFT"],
        ),
        (
            "public-contract-v1-drift",
            gen.FROZEN_CONTRACT_SNAPSHOTS[0][0],
            lambda raw: raw + b" ",
            ["PUBLIC_QUERY_CONTRACT_DRIFT"],
        ),
        (
            "adr-0005-edited",
            gen.ADR_PATH,
            lambda raw: raw + b"\nedit\n",
            ["CORRECTNESS_GATE_RED"],
        ),
        (
            "frozen-benchmark-edited",
            gen.FROZEN_BENCHMARK_PATH,
            lambda raw: raw + b"\n// edit\n",
            ["CORRECTNESS_GATE_RED"],
        ),
        (
            "schema-opened",
            gen.SCHEMA_PATH,
            lambda raw: json.dumps({**json.loads(raw), "additionalProperties": True}).encode("utf-8"),
            ["CORRECTNESS_GATE_RED"],
        ),
        (
            "nested-schema-opened",
            gen.SCHEMA_PATH,
            _open_nested_schema,
            ["CORRECTNESS_GATE_RED"],
        ),
        (
            "non-dict-predecessor-json",
            gen.DECISION_PATH,
            lambda _raw: b"[]\n",
            ["CORRECTNESS_GATE_RED"],
        ),
        (
            "predecessor-12-2-sprint-regressed",
            gen.SPRINT_PATH,
            lambda raw: raw.replace(
                f"  {gen.STORY_12_2_KEY}: done\n".encode(),
                f"  {gen.STORY_12_2_KEY}: in-progress\n".encode(),
            ),
            ["CORRECTNESS_GATE_RED"],
        ),
    ],
)
def test_complete_fault_matrix_code_and_contract_mutations(repository, name, target_path, mutator, expected_blockers):
    before = snapshot(repository)
    try:
        mutated = mutator(before[target_path])
        assert mutated != before[target_path]
        (repository / target_path).write_bytes(mutated)
        result = run_cli(repository)
        assert result.returncode == 1, (FAULT_NOT_DETECTED, result.stdout, result.stderr)
        assert blocker_codes(result.stderr) == expected_blockers, (FAULT_NOT_DETECTED, result.stderr)
        assert "Traceback" not in result.stderr
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def test_complete_fault_matrix_crlf_sprint_status(repository):
    before = snapshot(repository)
    try:
        crlf_sprint = before[gen.SPRINT_PATH].replace(b"\r\n", b"\n").replace(b"\n", b"\r\n")
        (repository / gen.SPRINT_PATH).write_bytes(crlf_sprint)
        result = run_cli(repository)
        assert result.returncode == 0, (result.stdout, result.stderr)
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def _strip_rollback_boundary(repo: Path) -> None:
    contract = json.loads((repo / gen.CONTRACT_PATH).read_bytes())
    del contract["rollback"]["boundary"]
    (repo / gen.CONTRACT_PATH).write_text(json.dumps(contract, indent=2) + "\n", encoding="utf-8")


@pytest.mark.parametrize(
    "name,setup,expected_code",
    [
        ("malformed-trx", lambda repo: (repo / UNIT_TRX).write_bytes(b"<not-xml"), "INPUT_UNAVAILABLE"),
        ("missing-rollback-boundary", _strip_rollback_boundary, "INPUT_UNAVAILABLE"),
        ("shallow-history", lambda repo: (repo / ".git/shallow").write_text(gen.STORY_12_1_COMMIT + "\n", encoding="utf-8"), "HISTORY_UNAVAILABLE"),
        ("missing-history", lambda repo: (repo / ".git/objects/info/alternates").write_text("", encoding="utf-8"), "HISTORY_UNAVAILABLE"),
    ],
)
def test_complete_fault_matrix_blocked_conditions(repository, name, setup, expected_code):
    before = snapshot(repository)
    try:
        setup(repository)
        result = run_cli(repository)
        assert result.returncode == 2 and result.stdout.strip() == "BLOCKED", (result.returncode, result.stderr)
        assert blocker_codes(result.stderr) == [expected_code], result.stderr
        assert "Traceback" not in result.stderr
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED
