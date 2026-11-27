"""Story 12.3 real-CLI complete fault matrix (AC-12.3-07); every fault restores all fixture bytes."""
from __future__ import annotations

import hashlib
import json
import os
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

SERVER_TEST_BIN = ROOT / "tests/Hexalith.Conversations.Server.Tests/bin/Debug/net11.0"
INTEGRATION_TEST_BIN = ROOT / "tests/Hexalith.Conversations.IntegrationTests/bin/Debug/net11.0"
CONTRACTS_TEST_BIN = ROOT / "tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net11.0"

FAKE_SERVER_SHA = "a" * 64
FAKE_TEST_SHA = "b" * 64


def git_run(repo: Path, *args: str) -> str:
    proc = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=True,
        capture_output=True,
        text=True,
    )
    return proc.stdout.strip()


def make_trx(
    lane: str,
    commit_sha: str,
    *,
    server_sha: str = FAKE_SERVER_SHA,
    test_sha: str = FAKE_TEST_SHA,
    methods: tuple[str, ...] | None = None,
    failed: int = 0,
    skipped: int = 0,
    message: str = "",
    executed: int | None = None,
    force_total: int | None = None,
    force_passed: int | None = None,
    force_result_outcome: str | None = None,
    include_binding: bool = True,
    omit_binding_for_index: int | None = None,
    summary_outcome: str = "Completed",
    code_base: str | None = None,
) -> bytes:
    selected = gen.EXPECTED_LANE_TESTS[lane] if methods is None else methods
    total = len(selected) if force_total is None else force_total
    passed = max(0, total - max(0, failed) - max(0, skipped)) if force_passed is None else force_passed
    exec_count = (total - max(0, skipped)) if executed is None else executed
    not_executed = max(0, total - exec_count)

    binding_line = (
        f"CANDIDATE_BINDING|commit={commit_sha}|serverSha256={server_sha}|testAssemblySha256={test_sha}"
        if include_binding
        else ""
    )
    code_base_attr = f' codeBase="{code_base}"' if code_base else ""
    results = []
    definitions = []
    for idx, full_name in enumerate(selected):
        if force_result_outcome is not None:
            outcome = force_result_outcome
        elif idx < passed:
            outcome = "Passed"
        elif idx < passed + max(0, failed):
            outcome = "Failed"
        else:
            outcome = "NotExecuted"
        tid = f"00000000-0000-0000-0000-{idx + 1:012d}"
        cls_name, _, method_name = full_name.rpartition(".")
        definitions.append(
            f'<UnitTest name="{full_name}" id="{tid}"><Execution id="{tid}" />'
            f'<TestMethod{code_base_attr} className="{cls_name}" name="{method_name}" /></UnitTest>'
        )
        out_parts = []
        if binding_line and idx != omit_binding_for_index:
            out_parts.append(f"<TextMessages><Message>{binding_line}</Message></TextMessages>")
        if message and (outcome != "Passed" and idx == passed):
            out_parts.append(f"<ErrorInfo><Message>{message}</Message></ErrorInfo>")
        out_xml = f"<Output>{''.join(out_parts)}</Output>" if out_parts else ""
        results.append(
            f'<UnitTestResult testId="{tid}" executionId="{tid}" testName="{full_name}" outcome="{outcome}">{out_xml}</UnitTestResult>'
        )

    xml = (
        '<?xml version="1.0" encoding="utf-8"?>'
        '<TestRun id="00000000-0000-0000-0000-000000000001" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        f'<Results>{"".join(results)}</Results>'
        f'<TestDefinitions>{"".join(definitions)}</TestDefinitions>'
        f'<ResultSummary outcome="{summary_outcome}">'
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


def restore(repo: Path, before: dict[str, bytes | None], head_sha: str) -> None:
    for git_meta in (".git/objects/info/alternates", ".git/shallow"):
        raw = before.get(git_meta)
        target = repo / git_meta
        if raw is None:
            target.unlink(missing_ok=True)
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(raw)
    git_run(repo, "reset", "--hard", "-q", head_sha)
    for path, raw in before.items():
        if path in (".git/objects/info/alternates", ".git/shallow"):
            continue
        target = repo / path
        if raw is None:
            target.unlink(missing_ok=True)
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(raw)


def commit_mutation(repo: Path, relative_path: str, mutated_bytes: bytes) -> str:
    """Write mutated bytes, stage, and commit a disposable candidate commit (with re-stamped TRXs)."""
    (repo / relative_path).write_bytes(mutated_bytes)
    git_run(repo, "add", "--", relative_path)
    git_run(
        repo,
        "-c", "user.name=Test",
        "-c", "user.email=test@example.invalid",
        "commit", "-q", "--no-verify", "-m", f"test: mutate {relative_path}",
    )
    new_head = git_run(repo, "rev-parse", "HEAD")
    for lane_name, trx_rel in (("unit", UNIT_TRX), ("integration", INTEGRATION_TRX), ("dapr", DAPR_TRX)):
        (repo / trx_rel).write_bytes(make_trx(lane_name, new_head))
    return new_head


def _compile_mutated_server_dll(
    work_dir: Path,
    source_rel_path: str,
    old_text: str,
    new_text: str,
    base_bin_dir: Path,
) -> Path:
    """Compile a single mutated Hexalith.Conversations.Server.dll into work_dir using existing bin references."""
    src_dest = work_dir / "src" / "Hexalith.Conversations.Server"
    shutil.copytree(
        ROOT / "src" / "Hexalith.Conversations.Server",
        src_dest,
        ignore=shutil.ignore_patterns("bin", "obj"),
    )
    target_cs = work_dir / source_rel_path
    content = target_cs.read_text(encoding="utf-8")
    assert old_text in content, f"Mutation anchor not found in {source_rel_path}"
    target_cs.write_text(content.replace(old_text, new_text, 1), encoding="utf-8")

    refs = []
    for dll in sorted(base_bin_dir.glob("*.dll")):
        name = dll.name
        if (
            name.lower() == "hexalith.conversations.server.dll"
            or ".tests." in name.lower()
            or name.lower().startswith(("xunit.", "microsoft.testing.", "testhost", "shouldly", "nsubstitute"))
        ):
            continue
        escaped = str(dll).replace("&", "&amp;").replace('"', "&quot;")
        refs.append(f'    <Reference Include="{escaped}" Private="false" />')

    csproj = (
        '<Project Sdk="Microsoft.NET.Sdk.Web">\n'
        "  <PropertyGroup>\n"
        "    <TargetFramework>net11.0</TargetFramework>\n"
        "    <OutputType>Library</OutputType>\n"
        "    <AssemblyName>Hexalith.Conversations.Server</AssemblyName>\n"
        "    <RootNamespace>Hexalith.Conversations.Server</RootNamespace>\n"
        "    <Nullable>enable</Nullable>\n"
        "    <ImplicitUsings>enable</ImplicitUsings>\n"
        "    <LangVersion>latest</LangVersion>\n"
        "    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\n"
        "    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>\n"
        "  </PropertyGroup>\n"
        "  <ItemGroup>\n"
        '    <InternalsVisibleTo Include="Hexalith.Conversations.Server.Tests" />\n'
        '    <InternalsVisibleTo Include="Hexalith.Conversations.IntegrationTests" />\n'
        '    <InternalsVisibleTo Include="Hexalith.Conversations.AppHost.Tests" />\n'
        "  </ItemGroup>\n"
        "  <ItemGroup>\n"
        '    <Compile Include="src/Hexalith.Conversations.Server/**/*.cs" Exclude="src/Hexalith.Conversations.Server/Program.cs" />\n'
        "  </ItemGroup>\n"
        "  <ItemGroup>\n"
        + "\n".join(refs)
        + "\n  </ItemGroup>\n"
        "</Project>\n"
    )
    proj_path = work_dir / "MutatedServer.csproj"
    proj_path.write_text(csproj, encoding="utf-8")
    clean_env = {
        k: v
        for k, v in os.environ.items()
        if k not in {
            "CustomBeforeDirectoryBuildProps",
            "CustomAfterMicrosoftCommonProps",
            "CustomBeforeDirectoryBuildTargets",
            "CustomAfterDirectoryBuildTargets",
            "AfterMicrosoftNETSdkTargets",
            "HEXALITH_SOURCE_MAPPING_HASH",
        }
    }
    proc = subprocess.run(
        ["dotnet", "build", str(proj_path), "--configuration", "Debug", "-v", "quiet"],
        cwd=work_dir,
        env=clean_env,
        capture_output=True,
        text=True,
        check=False,
        timeout=120,
    )
    assert proc.returncode == 0, f"Mutated server compile failed:\n{proc.stdout}\n{proc.stderr}"
    dll_path = work_dir / "bin" / "Debug" / "net11.0" / "Hexalith.Conversations.Server.dll"
    assert dll_path.is_file()
    return dll_path


def _run_mutated_test_assembly(
    tmp_dir: Path,
    base_bin_dir: Path,
    test_dll_name: str,
    mutated_server_dll: Path,
    method_full_name: str,
) -> Path:
    """Overlay mutated Hexalith.Conversations.Server.dll into a disposable bin dir and run xUnit v3."""
    run_bin = tmp_dir / "run-bin"
    run_bin.mkdir(parents=True, exist_ok=True)
    for item in base_bin_dir.iterdir():
        dest = run_bin / item.name
        if item.name == "Hexalith.Conversations.Server.dll":
            shutil.copyfile(mutated_server_dll, dest)
        elif item.name == test_dll_name or item.name.endswith((".deps.json", ".runtimeconfig.json")):
            shutil.copyfile(item, dest)
        elif item.is_file():
            dest.symlink_to(item)
        elif item.is_dir():
            dest.symlink_to(item, target_is_directory=True)
    trx_path = tmp_dir / "mutated-run.trx"
    proc = subprocess.run(
        [
            "dotnet",
            str(run_bin / test_dll_name),
            "-automated",
            "sync",
            "-failSkips",
            "-method",
            method_full_name,
            "-trx",
            str(trx_path),
        ],
        cwd=ROOT,
        stdin=subprocess.DEVNULL,
        capture_output=True,
        text=True,
        check=False,
        timeout=120,
    )
    assert proc.returncode != 0, (
        f"{FAULT_NOT_DETECTED}: real test {method_full_name} unexpectedly passed against mutated server binary!\n"
        f"{proc.stdout}\n{proc.stderr}"
    )
    assert trx_path.is_file(), f"Expected failed TRX at {trx_path}"
    return trx_path


@pytest.fixture(scope="module")
def repository(tmp_path_factory):
    repo = tmp_path_factory.mktemp("derived-key-proof") / "repo"
    # Intentionally omit retired v9-authority-bundle-v1.json to prove no dependency on it.
    copy_paths = [
        ".gitmodules",
        gen.SCHEMA_PATH,
        gen.CONTRACT_PATH,
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

    subprocess.run(["git", "init", "-q", str(repo)], check=True)
    common = git_run(ROOT, "rev-parse", "--path-format=absolute", "--git-common-dir")
    (repo / ".git/objects/info/alternates").write_text(str(Path(common) / "objects") + "\n", encoding="utf-8")

    # Stage all copied files + exact root submodule gitlinks from ROOT and create a real committed candidate HEAD.
    git_run(repo, "add", ".")
    ls_stage = git_run(ROOT, "ls-files", "--stage", "--", "references/")
    for line in ls_stage.splitlines():
        if not line.strip():
            continue
        meta, path = line.split("\t", 1)
        mode, sha, _ = meta.split()
        git_run(repo, "update-index", "--add", "--cacheinfo", f"{mode},{sha},{path}")

    git_run(
        repo,
        "-c", "user.name=Story 12.3 Test",
        "-c", "user.email=story123@example.invalid",
        "commit", "-q", "--no-verify", "-m", "test: seed committed story 12.3 candidate",
    )
    head_sha = git_run(repo, "rev-parse", "HEAD")

    for lane_name, trx_rel in (("unit", UNIT_TRX), ("integration", INTEGRATION_TRX), ("dapr", DAPR_TRX)):
        target = repo / trx_rel
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(make_trx(lane_name, head_sha))

    first = run_cli(repo)
    assert first.returncode == 0, (first.stdout, first.stderr)
    return repo, head_sha


def blocker_codes(stderr: str) -> list[str]:
    return [line.split(":", 1)[0].strip() for line in stderr.splitlines() if ":" in line]


def test_complete_fault_matrix_determinism_waiver_and_no_v9_authority_bundle(repository):
    repo, head_sha = repository
    before = snapshot(repo)
    assert not (repo / "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json").exists()

    run1 = run_cli(repo)
    assert run1.returncode == 0, run1.stderr
    json1 = (repo / gen.JSON_PATH).read_bytes()
    md1 = (repo / gen.MARKDOWN_PATH).read_bytes()

    run2 = run_cli(repo)
    assert run2.returncode == 0, run2.stderr
    assert (repo / gen.JSON_PATH).read_bytes() == json1
    assert (repo / gen.MARKDOWN_PATH).read_bytes() == md1

    bundle = json.loads(json1)
    assert bundle["schemaVersion"] == "hexalith.conversations.derived-key-correctness-proof.v1"
    assert bundle["candidate"]["headCommit"] == head_sha
    assert bundle["adrAndMethod"]["noSmC2VerdictIssued"] is True
    assert bundle["candidate"]["waivedScenarios"] == [
        {
            "id": "AC-12.3-08",
            "state": "waived",
            "decision": bundle["candidate"]["waivedScenarios"][0]["decision"],
        }
    ]
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def test_complete_fault_matrix_refreshable_tracked_proof_pair(repository):
    """Committed proof pair can be refreshed atomically while other tracked files remain protected."""
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        (repo / gen.JSON_PATH).write_bytes(b'{"stale": true}\n')
        (repo / gen.MARKDOWN_PATH).write_bytes(b"# stale\n")
        git_run(repo, "add", "--", gen.JSON_PATH, gen.MARKDOWN_PATH)
        git_run(
            repo,
            "-c", "user.name=Test",
            "-c", "user.email=test@example.invalid",
            "commit", "-q", "--no-verify", "-m", "test: commit stale tracked proof pair",
        )
        new_head = git_run(repo, "rev-parse", "HEAD")
        for lane_name, trx_rel in (("unit", UNIT_TRX), ("integration", INTEGRATION_TRX), ("dapr", DAPR_TRX)):
            (repo / trx_rel).write_bytes(make_trx(lane_name, new_head))

        refreshed = run_cli(repo)
        assert refreshed.returncode == 0, (refreshed.stdout, refreshed.stderr)
        bundle = json.loads((repo / gen.JSON_PATH).read_bytes())
        assert bundle["candidate"]["headCommit"] == new_head
        assert "stale" not in bundle
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def test_complete_fault_matrix_candidate_binding_rejects_uncommitted_and_stale_trxs(repository):
    """Dirty working tree or committed source change with stale TRXs must fail with CORRECTNESS_GATE_RED."""
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        # 1. Uncommitted production source change with old TRXs must fail.
        target = "src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs"
        (repo / target).write_bytes(before[target] + b"\n// uncommitted controlled source change\n")
        dirty_res = run_cli(repo)
        assert dirty_res.returncode == 1, (FAULT_NOT_DETECTED, dirty_res.stdout, dirty_res.stderr)
        assert blocker_codes(dirty_res.stderr) == ["CORRECTNESS_GATE_RED"], dirty_res.stderr

        # 2. Commit that source change WITHOUT regenerating TRXs -> TRX boundCommit != HEAD -> must fail.
        git_run(repo, "add", "--", target)
        git_run(
            repo,
            "-c", "user.name=Test",
            "-c", "user.email=test@example.invalid",
            "commit", "-q", "--no-verify", "-m", "test: controlled source change with stale TRXs",
        )
        stale_res = run_cli(repo)
        assert stale_res.returncode == 1, (FAULT_NOT_DETECTED, stale_res.stdout, stale_res.stderr)
        assert "CORRECTNESS_GATE_RED" in blocker_codes(stale_res.stderr), stale_res.stderr
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def test_complete_fault_matrix_real_xunit_trx_and_codebase_digest_verification(repository):
    """Real xUnit v3 TRX shape (<TextMessages><Message>) and repo-local codeBase DLL digests pass when matching and fail when tampered."""
    repo, head_sha = repository
    before = snapshot(repo)
    bin_dir = repo / "tmp-bin"
    try:
        bin_dir.mkdir(parents=True, exist_ok=True)
        server_dll = bin_dir / "Hexalith.Conversations.Server.dll"
        test_dll = bin_dir / "Hexalith.Conversations.Server.Tests.dll"
        server_dll.write_bytes(b"server-binary-v1")
        test_dll.write_bytes(b"unit-test-binary-v1")
        server_sha = hashlib.sha256(server_dll.read_bytes()).hexdigest()
        test_sha = hashlib.sha256(test_dll.read_bytes()).hexdigest()

        # Use relative codeBase inside repository and matching server_sha across all 3 lanes.
        rel_code_base = "tmp-bin/Hexalith.Conversations.Server.Tests.dll"
        (repo / UNIT_TRX).write_bytes(
            make_trx("unit", head_sha, server_sha=server_sha, test_sha=test_sha, code_base=rel_code_base)
        )
        (repo / INTEGRATION_TRX).write_bytes(
            make_trx("integration", head_sha, server_sha=server_sha, test_sha=FAKE_TEST_SHA)
        )
        (repo / DAPR_TRX).write_bytes(
            make_trx("dapr", head_sha, server_sha=server_sha, test_sha=FAKE_TEST_SHA)
        )

        # Matching on-disk DLLs pass.
        ok = run_cli(repo)
        assert ok.returncode == 0, (ok.stdout, ok.stderr)

        # Tampering with sibling server DLL on disk fails with CORRECTNESS_GATE_RED.
        server_dll.write_bytes(b"server-binary-tampered")
        tampered_server = run_cli(repo)
        assert tampered_server.returncode == 1, (FAULT_NOT_DETECTED, tampered_server.stdout, tampered_server.stderr)
        assert blocker_codes(tampered_server.stderr) == ["CORRECTNESS_GATE_RED"], tampered_server.stderr

        # Restore server DLL and tamper with test DLL on disk -> also fails with CORRECTNESS_GATE_RED.
        server_dll.write_bytes(b"server-binary-v1")
        test_dll.write_bytes(b"unit-test-binary-tampered")
        tampered_test = run_cli(repo)
        assert tampered_test.returncode == 1, (FAULT_NOT_DETECTED, tampered_test.stdout, tampered_test.stderr)
        assert blocker_codes(tampered_test.stderr) == ["CORRECTNESS_GATE_RED"], tampered_test.stderr

        # Verify actual xUnit v3 TRX XML file emitted on disk (if present) parses via parse_trx structure.
        real_trx = ROOT / UNIT_TRX
        if real_trx.is_file():
            raw = real_trx.read_bytes()
            assert b"<TextMessages><Message>CANDIDATE_BINDING|" in raw
    finally:
        shutil.rmtree(bin_dir, ignore_errors=True)
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def test_complete_fault_matrix_uncommitted_contract_and_schema_rejected(repository):
    """Uncommitted edits to CONTRACT_PATH or SCHEMA_PATH (even when valid/closed) must fail with CORRECTNESS_GATE_RED."""
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        (repo / gen.CONTRACT_PATH).write_bytes(before[gen.CONTRACT_PATH] + b"\n")
        res_contract = run_cli(repo)
        assert res_contract.returncode == 1, (FAULT_NOT_DETECTED, res_contract.stdout, res_contract.stderr)
        assert blocker_codes(res_contract.stderr) == ["CORRECTNESS_GATE_RED"], res_contract.stderr

        (repo / gen.CONTRACT_PATH).write_bytes(before[gen.CONTRACT_PATH])
        (repo / gen.SCHEMA_PATH).write_bytes(before[gen.SCHEMA_PATH] + b"\n")
        res_schema = run_cli(repo)
        assert res_schema.returncode == 1, (FAULT_NOT_DETECTED, res_schema.stdout, res_schema.stderr)
        assert blocker_codes(res_schema.stderr) == ["CORRECTNESS_GATE_RED"], res_schema.stderr
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize(
    "name,trx_path,builder,expected_blockers",
    [
        ("unit-empty", UNIT_TRX, lambda sha: make_trx("unit", sha, methods=(), force_total=0, force_passed=0), ["CORRECTNESS_GATE_RED"]),
        ("unit-skipped", UNIT_TRX, lambda sha: make_trx("unit", sha, skipped=1, force_passed=0), ["CORRECTNESS_GATE_RED"]),
        ("unit-wrong-test", UNIT_TRX, lambda sha: make_trx("unit", sha, methods=("Unrelated.TestMethod",)), ["CORRECTNESS_GATE_RED"]),
        ("unit-negative-counter", UNIT_TRX, lambda sha: make_trx("unit", sha, failed=-1, force_passed=1), ["CORRECTNESS_GATE_RED"]),
        ("unit-not-run", UNIT_TRX, lambda sha: make_trx("unit", sha, executed=0, force_passed=1), ["CORRECTNESS_GATE_RED"]),
        ("unit-failed-result-item", UNIT_TRX, lambda sha: make_trx("unit", sha, force_result_outcome="Failed", force_passed=1), ["CORRECTNESS_GATE_RED"]),
        ("unit-summary-failed-outcome", UNIT_TRX, lambda sha: make_trx("unit", sha, summary_outcome="Failed"), ["CORRECTNESS_GATE_RED"]),
        ("unit-missing-binding", UNIT_TRX, lambda sha: make_trx("unit", sha, include_binding=False), ["CORRECTNESS_GATE_RED"]),
        ("unit-mismatched-server-sha", UNIT_TRX, lambda sha: make_trx("unit", sha, server_sha="f" * 64), ["CORRECTNESS_GATE_RED"]),
        ("integration-empty", INTEGRATION_TRX, lambda sha: make_trx("integration", sha, methods=(), force_total=0, force_passed=0), ["CORRECTNESS_GATE_RED"]),
        ("integration-reduced-single-test", INTEGRATION_TRX, lambda sha: make_trx("integration", sha, methods=(gen.EXPECTED_LANE_TESTS["integration"][0],)), ["CORRECTNESS_GATE_RED"]),
        ("integration-missing-binding-on-one-result", INTEGRATION_TRX, lambda sha: make_trx("integration", sha, omit_binding_for_index=2), ["CORRECTNESS_GATE_RED"]),
        ("integration-wrong-test", INTEGRATION_TRX, lambda sha: make_trx("integration", sha, methods=("Unrelated.IntegrationTest",)), ["CORRECTNESS_GATE_RED"]),
        ("dapr-empty", DAPR_TRX, lambda sha: make_trx("dapr", sha, methods=(), force_total=0, force_passed=0), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-reduced-single-test", DAPR_TRX, lambda sha: make_trx("dapr", sha, methods=(gen.EXPECTED_LANE_TESTS["dapr"][0],)), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-skipped", DAPR_TRX, lambda sha: make_trx("dapr", sha, skipped=1), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-wrong-test", DAPR_TRX, lambda sha: make_trx("dapr", sha, methods=("Unrelated.DaprTest",)), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-failed", DAPR_TRX, lambda sha: make_trx("dapr", sha, failed=1, message="Dapr partial write fault"), ["DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
        ("dapr-tenant-disclosure", DAPR_TRX, lambda sha: make_trx("dapr", sha, failed=1, message="TENANT_DISCLOSURE: poison leaked"), ["TENANT_DISCLOSURE", "DAPR_CORRECTNESS_LANE_RED", "CORRECTNESS_GATE_RED"]),
    ],
    ids=lambda item: item if isinstance(item, str) else None,
)
def test_complete_fault_matrix_lane_faults(repository, name, trx_path, builder, expected_blockers):
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        (repo / trx_path).write_bytes(builder(head_sha))
        result = run_cli(repo)
        assert result.returncode == 1, (FAULT_NOT_DETECTED, result.stdout, result.stderr)
        assert blocker_codes(result.stderr) == expected_blockers, (FAULT_NOT_DETECTED, result.stderr)
        assert "Traceback" not in result.stderr
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def _open_nested_schema(raw: bytes) -> bytes:
    schema = json.loads(raw)
    schema["properties"]["lanes"]["additionalProperties"] = True
    return json.dumps(schema).encode("utf-8")


def _open_allof_schema(raw: bytes) -> bytes:
    schema = json.loads(raw)
    schema["properties"]["lanes"]["allOf"] = [
        {
            "type": "object",
            "properties": {
                "extra": {"type": "string"},
            },
        }
    ]
    return json.dumps(schema).encode("utf-8")


def _untyped_open_schema(raw: bytes) -> bytes:
    schema = json.loads(raw)
    schema["properties"]["untypedOpen"] = {}
    return json.dumps(schema).encode("utf-8")


@pytest.mark.parametrize(
    "name,target_path,mutator,expected_blockers,commit_change",
    [
        (
            "read-side-repair-mutation",
            gen.READ_STORE_PATH,
            lambda raw: raw + b'\n// mutated: _store.SaveAsync("s", "k", new object());\n',
            ["READ_SIDE_REPAIR_ATTEMPTED"],
            True,
        ),
        (
            "read-side-repair-in-query-handler",
            "src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs",
            lambda raw: raw + b'\n// mutated: ReadModelWritePolicy.MergeAsync<object>(null!, "s", "k", null!, null!, null!, null!, null!);\n',
            ["READ_SIDE_REPAIR_ATTEMPTED"],
            True,
        ),
        (
            "read-side-repair-in-read-service",
            "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs",
            lambda raw: raw + b'\n// mutated: _store.TryEraseAsync("s", "k", "e");\n',
            ["READ_SIDE_REPAIR_ATTEMPTED"],
            True,
        ),
        (
            "derived-key-grammar-mutation",
            gen.KEYS_PATH,
            lambda raw: raw + b"\n// mutated key grammar\n",
            ["DERIVED_KEY_TRANSITION_UNSAFE"],
            True,
        ),
        (
            "public-contract-v4-drift",
            gen.PUBLIC_CONTRACT_V4_PATH,
            lambda raw: raw + b" ",
            ["PUBLIC_QUERY_CONTRACT_DRIFT"],
            False,
        ),
        (
            "public-contract-v1-drift",
            gen.FROZEN_CONTRACT_SNAPSHOTS[0][0],
            lambda raw: raw + b" ",
            ["PUBLIC_QUERY_CONTRACT_DRIFT"],
            False,
        ),
        (
            "adr-0005-edited",
            gen.ADR_PATH,
            lambda raw: raw + b"\nedit\n",
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "frozen-benchmark-edited",
            gen.FROZEN_BENCHMARK_PATH,
            lambda raw: raw + b"\n// edit\n",
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "schema-opened",
            gen.SCHEMA_PATH,
            lambda raw: json.dumps({**json.loads(raw), "additionalProperties": True}).encode("utf-8"),
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "nested-schema-opened",
            gen.SCHEMA_PATH,
            _open_nested_schema,
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "allof-composed-schema-opened",
            gen.SCHEMA_PATH,
            _open_allof_schema,
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "untyped-empty-schema-opened",
            gen.SCHEMA_PATH,
            _untyped_open_schema,
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "non-dict-predecessor-json",
            gen.DECISION_PATH,
            lambda _raw: b"[]\n",
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
        (
            "predecessor-12-2-sprint-regressed",
            gen.SPRINT_PATH,
            lambda raw: raw.replace(
                f"  {gen.STORY_12_2_KEY}: done\n".encode(),
                f"  {gen.STORY_12_2_KEY}: in-progress\n".encode(),
            ),
            ["CORRECTNESS_GATE_RED"],
            False,
        ),
    ],
)
def test_complete_fault_matrix_code_and_contract_mutations(
    repository,
    name,
    target_path,
    mutator,
    expected_blockers,
    commit_change,
):
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        mutated = mutator(before[target_path])
        assert mutated != before[target_path]
        if commit_change:
            commit_mutation(repo, target_path, mutated)
        else:
            (repo / target_path).write_bytes(mutated)
        result = run_cli(repo)
        assert result.returncode == 1, (FAULT_NOT_DETECTED, result.stdout, result.stderr)
        assert blocker_codes(result.stderr) == expected_blockers, (FAULT_NOT_DETECTED, result.stderr)
        assert "Traceback" not in result.stderr
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize(
    "name,source_rel,old_snippet,new_snippet,bin_dir,test_dll,method_name,target_trx,expected_blocker",
    [
        (
            "real-fanout-unbounded",
            "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs",
            "ConversationProjectedReadModels? models = entry.Value;",
            "ConversationProjectedReadModels? models = entry.Value;\n        _ = await _store.GetAsync<ConversationProjectedReadModels>(_storeOptions.StateStoreName, ConversationProjectionReadModelKeys.ConversationKey(tenantId, conversationId), cancellationToken).ConfigureAwait(false);",
            SERVER_TEST_BIN,
            "Hexalith.Conversations.Server.Tests.dll",
            "Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.ListAndOpenShouldUseApprovedBoundedReads",
            UNIT_TRX,
            "DERIVED_KEY_FANOUT_UNBOUNDED",
        ),
        (
            "real-invalid-state-fail-closed",
            "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs",
            "if (entry.Value is null)\n        {\n            return ConversationProjectionIndexSnapshot.Missing;\n        }",
            "if (entry.Value is null)\n        {\n            return new ConversationProjectionIndexSnapshot { ReadState = ConversationProjectionIndexReadState.Ready };\n        }",
            SERVER_TEST_BIN,
            "Hexalith.Conversations.Server.Tests.dll",
            "Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.InvalidDerivedStateShouldFailClosedWithoutRepair",
            UNIT_TRX,
            "DERIVED_STATE_FAIL_CLOSED_VIOLATION",
        ),
        (
            "real-read-side-repair",
            "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs",
            "if (entry.Value is null)\n        {\n            return ConversationProjectionIndexSnapshot.Missing;\n        }",
            "if (entry.Value is null)\n        {\n            await _store.SaveAsync(_storeOptions.StateStoreName, ConversationProjectionReadModelKeys.TenantIndexKey(tenantId), new ConversationProjectionIndexReadModel(), cancellationToken).ConfigureAwait(false);\n            return ConversationProjectionIndexSnapshot.Missing;\n        }",
            SERVER_TEST_BIN,
            "Hexalith.Conversations.Server.Tests.dll",
            "Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.InvalidDerivedStateShouldFailClosedWithoutRepair",
            UNIT_TRX,
            "READ_SIDE_REPAIR_ATTEMPTED",
        ),
        (
            "real-tenant-disclosure",
            "src/Hexalith.Conversations.Server/Queries/ConversationQueryHandler.cs",
            "if (query.Page.ContinuationCursor is not null)",
            "if (false && query.Page.ContinuationCursor is not null)",
            INTEGRATION_TEST_BIN,
            "Hexalith.Conversations.IntegrationTests.dll",
            "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.TenantCollisionsShouldNeverDiscloseAcrossTenants",
            INTEGRATION_TRX,
            "TENANT_DISCLOSURE",
        ),
        (
            "real-transition-unsafe",
            "src/Hexalith.Conversations.Server/Projections/ConversationIndexGenerationToken.cs",
            "return Convert.ToHexStringLower(digest);",
            "return new string('0', 64);",
            INTEGRATION_TEST_BIN,
            "Hexalith.Conversations.IntegrationTests.dll",
            "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.LegacyV1AndV2WithoutPersistedTokenShouldMatchPersistedGenerationToken",
            INTEGRATION_TRX,
            "DERIVED_KEY_TRANSITION_UNSAFE",
        ),
        (
            "real-replay-nondeterministic",
            "src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs",
            "return positionComparison != 0\n            ? positionComparison\n            : StringComparer.Ordinal.Compare(incomingDispatchId, existingDispatchId);",
            "return positionComparison != 0 ? positionComparison : 1;",
            INTEGRATION_TEST_BIN,
            "Hexalith.Conversations.IntegrationTests.dll",
            "Hexalith.Conversations.IntegrationTests.Projections.DerivedKeyLifecycleTests.RetriesOutOfOrderAndEqualPositionConflictsShouldConvergeDeterministically",
            INTEGRATION_TRX,
            "REPLAY_NONDETERMINISTIC",
        ),
    ],
)
def test_complete_fault_matrix_real_runtime_mutations(
    repository,
    tmp_path,
    name,
    source_rel,
    old_snippet,
    new_snippet,
    bin_dir,
    test_dll,
    method_name,
    target_trx,
    expected_blocker,
):
    """Compile real mutated production Server binaries, run real xUnit v3 tests, and verify exact proof CLI blockers."""
    repo, head_sha = repository
    before = snapshot(repo)
    assert (bin_dir / test_dll).is_file(), f"Built test assembly missing: {bin_dir / test_dll}"
    try:
        mutated_dll = _compile_mutated_server_dll(
            tmp_path / name,
            source_rel,
            old_snippet,
            new_snippet,
            bin_dir,
        )
        failed_trx = _run_mutated_test_assembly(
            tmp_path / f"{name}-run",
            bin_dir,
            test_dll,
            mutated_dll,
            method_name,
        )
        trx_bytes = failed_trx.read_bytes()
        assert expected_blocker.encode("utf-8") in trx_bytes, (
            f"{FAULT_NOT_DETECTED}: real failed TRX for {name} did not report {expected_blocker}"
        )
        (repo / target_trx).write_bytes(trx_bytes)
        result = run_cli(repo)
        assert result.returncode == 1, (FAULT_NOT_DETECTED, result.stdout, result.stderr)
        codes = blocker_codes(result.stderr)
        assert expected_blocker in codes and "CORRECTNESS_GATE_RED" in codes, (FAULT_NOT_DETECTED, result.stderr)
        assert "Traceback" not in result.stderr
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def test_complete_fault_matrix_crlf_sprint_status(repository):
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        crlf_sprint = before[gen.SPRINT_PATH].replace(b"\r\n", b"\n").replace(b"\n", b"\r\n")
        (repo / gen.SPRINT_PATH).write_bytes(crlf_sprint)
        result = run_cli(repo)
        assert result.returncode == 0, (result.stdout, result.stderr)
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED


def _strip_rollback_boundary(repo: Path) -> None:
    contract = json.loads((repo / gen.CONTRACT_PATH).read_bytes())
    del contract["rollback"]["boundary"]
    (repo / gen.CONTRACT_PATH).write_text(json.dumps(contract, indent=2) + "\n", encoding="utf-8")


@pytest.mark.parametrize(
    "name,setup,expected_code",
    [
        ("malformed-trx", lambda repo: (repo / UNIT_TRX).write_bytes(b"<not-xml"), "INPUT_UNAVAILABLE"),
        ("non-object-story-contract", lambda repo: (repo / gen.CONTRACT_PATH).write_bytes(b"[]\n"), "INPUT_UNAVAILABLE"),
        ("missing-rollback-boundary", _strip_rollback_boundary, "INPUT_UNAVAILABLE"),
        ("shallow-history", lambda repo: (repo / ".git/shallow").write_text(gen.STORY_12_1_COMMIT + "\n", encoding="utf-8"), "HISTORY_UNAVAILABLE"),
        ("missing-history", lambda repo: (repo / ".git/objects/info/alternates").write_text("", encoding="utf-8"), "HISTORY_UNAVAILABLE"),
    ],
)
def test_complete_fault_matrix_blocked_conditions(repository, name, setup, expected_code):
    repo, head_sha = repository
    before = snapshot(repo)
    try:
        setup(repo)
        result = run_cli(repo)
        assert result.returncode == 2 and result.stdout.strip() == "BLOCKED", (result.returncode, result.stderr)
        assert blocker_codes(result.stderr) == [expected_code], result.stderr
        assert "Traceback" not in result.stderr
    finally:
        restore(repo, before, head_sha)
    assert snapshot(repo) == before, FIXTURE_NOT_RESTORED
