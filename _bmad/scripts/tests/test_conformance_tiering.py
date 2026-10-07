"""Story 9.1 conformance tiering: source model, determinism, CLI behavior, and measured fault fixtures."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any
from xml.sax.saxutils import quoteattr

import pytest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "_bmad/scripts/generate_conformance_tiering.py"
specification = importlib.util.spec_from_file_location("conformance_tiering", SCRIPT)
assert specification is not None and specification.loader is not None
module = importlib.util.module_from_spec(specification)
sys.modules[specification.name] = module  # dataclasses resolve postponed annotations through sys.modules
specification.loader.exec_module(module)
CANDIDATE = subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "HEAD"]).decode().strip()

OUTPUT_ARGUMENTS = [
    "--output-schema", module.OUTPUT_PATHS[0],
    "--output-json", module.OUTPUT_PATHS[1],
    "--output-markdown", module.OUTPUT_PATHS[2],
]
GENERATE_COMMAND = [
    "_bmad/scripts/generate_conformance_tiering.py", "--repository", ".", "--contract", module.CONTRACT_PATH,
    "--decision", module.DECISION_PATH, *OUTPUT_ARGUMENTS,
]
FAULT_PROPERTY = "story91ObservedFault"
FAULTS = {
    "assertion-missing": "CONFORMANCE_ASSERTION_MISSING",
    "assertion-duplicate": "CONFORMANCE_ASSERTION_DUPLICATE",
    "assertion-renamed": "CONFORMANCE_ASSERTION_RENAMED",
    "strength-weakened": "ASSERTION_STRENGTH_WEAKENED",
    "tier-unassigned": "TIER_UNASSIGNED",
    "tier-reason-missing": "TIER_REASON_MISSING",
    "approval-missing": "TIER_APPROVAL_MISSING",
    "denominator-drift": "FR20_DENOMINATOR_DRIFT",
    "public-widened": "PUBLIC_CONTRACT_WIDENED",
    "v1-mutated": "V1_ARTIFACT_DRIFT",
}
TARGET = "tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuiteTest.cs"
TARGET_METHOD = "RunResultShouldHaveExactly12Checks"
TARGET_BLOCK = (
    "    [Fact]\n"
    "    public void RunResultShouldHaveExactly12Checks()\n"
    "    {\n"
    "        ConformanceRunResultV1 run = Run();\n"
    "        run.Checks.Count.ShouldBe(12);\n"
    "    }\n"
    "\n"
)
WIDENING = "src/Hexalith.Conversations.Contracts/Conformance/TieringFaultProbeWidening.cs"
V1_FILE = "docs/release-evidence/release-baseline-v1.md"
FIXTURE_PATHS = sorted([*module.OUTPUT_PATHS, module.APPROVALS_PATH, TARGET, WIDENING, V1_FILE])


def _git(cwd: Path, *arguments: str) -> bytes:
    return subprocess.run(["git", "-C", str(cwd), *arguments], check=True, capture_output=True).stdout


def _mirror_working_tree(source: Path, target: Path) -> None:
    """Replicate the source checkout's uncommitted and untracked non-ignored files."""
    entries = _git(source, "status", "--porcelain=v1", "-z", "--untracked-files=all").split(b"\0")
    index = 0
    while index < len(entries):
        entry = entries[index].decode("utf-8")
        index += 1
        if not entry:
            continue
        status, path = entry[:2], entry[3:]
        if status.startswith("R"):
            index += 1
        origin = source / path
        destination = target / path
        if origin.is_file():
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(origin, destination)
        elif destination.exists():
            destination.unlink()


def _cli(repository: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run([sys.executable, str(repository / "_bmad/scripts/generate_conformance_tiering.py"),
                           *arguments], cwd=repository, capture_output=True, text=True, timeout=900, check=False)


def _verify(repository: Path) -> tuple[int, list[str]]:
    """Measure the actual read-only verification CLI's exit code and ordered unique blockers."""
    result = _cli(repository, "--repository", ".", "--verify", *OUTPUT_ARGUMENTS)
    blockers = list(dict.fromkeys(re.findall(r"^(?:FAIL|BLOCKED): ([A-Z0-9_]+):", result.stderr, re.M)))
    if result.returncode == 0:
        assert "PASS:" in result.stdout and not result.stderr
    return result.returncode, blockers


@pytest.fixture(scope="module")
def tiering_repository(tmp_path_factory: pytest.TempPathFactory) -> Path:
    """A disposable clone carrying this checkout's working state and, when retained, its pre-split capture."""
    repository = tmp_path_factory.mktemp("story-9-1") / "conversations"
    subprocess.run(["git", "clone", "--shared", "--quiet", str(ROOT), str(repository)], check=True, capture_output=True)
    _mirror_working_tree(ROOT, repository)
    capture = ROOT / module.CAPTURE_DIR
    if (capture / "receipt.json").is_file():
        shutil.copytree(capture, repository / module.CAPTURE_DIR)
    if any(not (repository / path).is_file() for path in module.OUTPUT_PATHS):
        # Pre-candidate development only: the real proposal awaits the Quality owner, so this disposable clone
        # records a visibly synthetic decision to exercise generation. It is never written to the repository.
        assert _cli(repository, "--repository", ".", "--propose-approvals").returncode == 0
        digest = json.loads((repository / module.APPROVALS_PATH).read_bytes())["proposal"]["membershipSha256"]
        recorded = _cli(repository, "--repository", ".", "--record-approval", "--approver", "SYNTHETIC-FIXTURE",
                        "--approval-id", "SYNTHETIC-FIXTURE-NOT-AN-APPROVAL", "--approved-on", "2026-10-06",
                        "--approved-membership-sha256", digest, "--approval-evidence", "disposable test clone only")
        assert recorded.returncode == 0, recorded.stderr
        generated = _cli(repository, *GENERATE_COMMAND[1:])
        assert generated.returncode == 0, generated.stderr
    return repository


# --------------------------------------------------------------------------- source model


SERVER_SOURCE = """
namespace Hexalith.Conversations.Server.Diagnostics;

public enum ServerKind { A, B }

public static class ServerExtensions
{
    public static int Probe(this string value) => value.Length;
}
"""
CONTRACT_SOURCE = """
namespace Hexalith.Conversations.Contracts.Identifiers;

public sealed record TenantId(string Value);
"""
HELPER_SOURCE = """
using Hexalith.Conversations.Server.Diagnostics;

namespace Hexalith.Conversations.Conformance.Tests;

internal static class ServerHelper
{
    internal static ServerKind Kind() => ServerKind.A;
}
"""
TEST_SOURCE = """
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Diagnostics;
using Kind = Hexalith.Conversations.Server.Diagnostics.ServerKind;

namespace Hexalith.Conversations.Conformance.Tests;

public sealed class SampleTest
{
    private static readonly TenantId Tenant = new("t");

    [Fact]
    public void PortableShouldBindOnlyContracts()
    {
        string text = $"{Tenant.Value} {{literal}} {"nested"}";
        text.ShouldNotBeNull();
        (text == '\\'' + "x").ShouldBeFalse();
    }

    [Fact]
    public void HoleShouldReachHelper() => $"{ServerHelper.Kind()}".ShouldNotBeEmpty();

    [Fact]
    public void AliasShouldBind() => Kind.A.ShouldBe(Kind.A);

    [Fact]
    public void RelativeQualificationShouldBind() => Server.Diagnostics.ServerKind.B.ShouldNotBe(Server.Diagnostics.ServerKind.A);

    [Fact]
    public void ExtensionShouldBind() => "abc".Probe().ShouldBe(3);

    [Theory]
    [InlineData(1)]
    public void TheoryShouldBeDetected(int value) => Should.Throw<InvalidOperationException>(() => Fail(value));

    [Fact]
    public void NestedNameShouldStayLocal() => new Clock().Now().ShouldBe(1);

    private static int Fail(int value) => throw new InvalidOperationException(value.ToString());

    private sealed class Clock
    {
        public int Now() => 1;
    }
}
"""
OTHER_SOURCE = """
using Hexalith.Conversations.Server.Diagnostics;

namespace Hexalith.Conversations.Conformance.Tests;

public sealed class OtherTest
{
    [Fact]
    public void OtherShouldBindServer() => ServerKind.A.ShouldBe(ServerKind.A);

    private sealed class Clock
    {
        public ServerKind Now() => ServerKind.B;
    }
}
"""


def _model(files: dict[str, str]) -> Any:
    sources = [module.parse_source(path, text.encode(), strict=True) for path, text in files.items()]
    catalog = module.ModuleCatalog.build({
        "Hexalith.Conversations.Server": [module.parse_source("server.cs", SERVER_SOURCE.encode(), strict=False)],
        "Hexalith.Conversations.Contracts": [module.parse_source("contracts.cs", CONTRACT_SOURCE.encode(), strict=False)],
    })
    return module.ProjectModel(sources, catalog, ("Shouldly", "Xunit"))


def _material(model: Any, name: str) -> tuple[dict[str, Any], Any]:
    case = next(case for case in model.tests() if case.identity.endswith("." + name))
    closure = model.closure(case)
    return model.strength(case, closure, ["Hexalith.Conversations.Contracts", "Hexalith.Conversations.Server"]), closure


def test_tokenizer_scans_interpolation_verbatim_and_character_literals() -> None:
    tokens = module.tokenize('var a = $"{b.C("x")} {{d}}"; var e = @"f""g"; var h = \'\\\'\'; var i = $@"{j}";',
                             "sample.cs", strict=True)
    strings = [token for token in tokens if token.kind == "str"]
    assert [token.inner for token in strings] == [("b", "C"), (), (), ("j",)]
    assert [token.text for token in tokens if token.kind == "id"] == ["var", "a", "var", "e", "var", "h", "var", "i"]


@pytest.mark.parametrize("source", [
    'var a = """raw""";', "#if DEBUG\nclass A {}\n#endif\n", "using static System.Math;\nclass A {}",
    "global using System;\nclass A {}", 'var a = $$"{{b}}";', 'var a = "unterminated;',
])
def test_unsupported_conformance_constructs_fail_closed(source: str) -> None:
    with pytest.raises(module.SourceModelError):
        module.parse_source("unsupported.cs", source.encode(), strict=True)


def test_closure_binds_alias_relative_extension_hole_and_field_initializers() -> None:
    model = _model({"tests/Helper.cs": HELPER_SOURCE, "tests/SampleTest.cs": TEST_SOURCE,
                    "tests/OtherTest.cs": OTHER_SOURCE})
    portable, closure = _material(model, "PortableShouldBindOnlyContracts")
    assert portable["boundAssemblies"] == ["Hexalith.Conversations.Contracts"]
    assert closure.assertion_sites == ["ShouldNotBeNull", "ShouldBeFalse"]
    assert portable["negativeCaseCount"] == 1
    for name in ("HoleShouldReachHelper", "AliasShouldBind", "RelativeQualificationShouldBind", "ExtensionShouldBind"):
        material, closure = _material(model, name)
        assert "Hexalith.Conversations.Server" in material["boundAssemblies"], name
    _, closure = _material(model, "HoleShouldReachHelper")
    assert [declaration.name for declaration in closure.types] == ["ServerHelper"]
    theory, closure = _material(model, "TheoryShouldBeDetected")
    assert closure.assertion_sites == ["Should.Throw"] and theory["negativeCaseCount"] == 1
    assert {case.identity.rsplit(".", 1)[1]: case.kind for case in model.tests()}["TheoryShouldBeDetected"] == "theory"
    nested, closure = _material(model, "NestedNameShouldStayLocal")
    assert nested["boundAssemblies"] == ["Hexalith.Conversations.Contracts"]
    assert [declaration.full_name for declaration in closure.types] == [
        "Hexalith.Conversations.Conformance.Tests.SampleTest+Clock"]


def test_behavior_identity_is_location_independent_and_detects_weakening() -> None:
    base, _ = _material(_model({"tests/SampleTest.cs": TEST_SOURCE, "tests/Helper.cs": HELPER_SOURCE}), "AliasShouldBind")
    moved = TEST_SOURCE.replace("namespace Hexalith.Conversations.Conformance.Tests;",
                                "namespace Hexalith.Conversations.Conformance.Tests;\n// moved and re-indented\n")
    relocated, _ = _material(_model({"tests/Moved/SampleTest.cs": moved, "tests/Helper.cs": HELPER_SOURCE}), "AliasShouldBind")
    assert relocated == base
    weakened_source = TEST_SOURCE.replace("Kind.A.ShouldBe(Kind.A)", "Kind.A.ShouldBe(Kind.B)")
    weakened, _ = _material(_model({"tests/SampleTest.cs": weakened_source, "tests/Helper.cs": HELPER_SOURCE}), "AliasShouldBind")
    assert weakened["behaviorIdentity"] != base["behaviorIdentity"]
    assert module.strength_sha256(weakened) != module.strength_sha256(base)


@pytest.mark.parametrize("source", [
    TEST_SOURCE.replace("public void AliasShouldBind()", "public void PortableShouldBindOnlyContracts(int unused)"),
    "namespace Hexalith.Conversations.Conformance.Tests;\npublic abstract class BaseTest\n{\n    [Fact]\n"
    "    public void Inherited() { }\n}\npublic sealed class DerivedTest : BaseTest\n{\n}\n",
])
def test_overloaded_or_inherited_tests_are_unsupported(source: str) -> None:
    with pytest.raises(module.SourceModelError):
        _model({"tests/Unsupported.cs": source}).tests()


# --------------------------------------------------------------------------- real inventory


def _pre_split(repository: Path, document: dict[str, Any]) -> Any:
    if (repository / module.RECEIPT_PATH).is_file():
        return module.load_pre_split(repository)
    return module.pre_split_from_document(document)


def test_inventory_reconciles_source_discovery_results_and_helpers(tiering_repository: Path) -> None:
    """Every source test attribute, discovered method, and executed result maps to exactly one frozen row."""
    document = json.loads((tiering_repository / module.OUTPUT_PATHS[1]).read_bytes())
    pre_split = _pre_split(tiering_repository, document)
    derivation = module.derive(tiering_repository, module.CONTRACT_PATH, module.DECISION_PATH, pre_split=pre_split)
    assert derivation.findings.items == []
    rows = derivation.document["assertions"]
    additions = derivation.document["validationAdditions"]
    assert [row["id"] for row in rows] == sorted(pre_split.discovered)
    project = tiering_repository / module.PROJECT_DIR
    attributes = sum(len(re.findall(r"^\s*\[(?:Fact|Theory)\b", path.read_text(encoding="utf-8"), re.M))
                     for path in project.glob("*.cs"))
    assert attributes == len(rows) + len(additions)
    assert {row["id"].rsplit(".", 1)[0] for row in additions} == {
        "Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest"}
    assert len(additions) == 7
    executed = [row for row in rows if row["preSplitResultIdentity"]["lane"] == "executed"]
    assert sum(row["preSplitResultIdentity"]["executed"] for row in executed) == pre_split.summary["executed"]
    assert all(row["preSplitResultIdentity"]["results"] == [] for row in rows if row not in executed)
    theories = [row for row in executed if row["kind"] == "theory"]
    assert theories and all(len(row["preSplitResultIdentity"]["results"]) > 1 for row in theories)
    assert all(bool(row["serverBindings"]) == (row["tier"] == "module-internal") for row in rows + additions)
    outcomes = derivation.document["triage"]["outcomes"]
    assert len(outcomes) == 13 and all(item["assertions"] > 0 and item["tierCounts"]["portable"] == 0 for item in outcomes)
    cardinality = [row for row in rows if ".TelemetryCardinalityConformanceSuiteTest." in row["id"]]
    assert cardinality and all(
        any(entry["path"].endswith("/TelemetryCardinalityConformanceSuite.cs") for entry in row["closureFiles"])
        for row in cardinality)
    assert derivation.document["fr20Membership"]["missingIdentities"] == []


def test_committed_bundle_is_the_deterministic_derivation(tiering_repository: Path) -> None:
    first = module.generate(tiering_repository, module.CONTRACT_PATH, module.DECISION_PATH)
    second = module.generate(tiering_repository, module.CONTRACT_PATH, module.DECISION_PATH)
    assert first == second
    assert first == tuple((tiering_repository / path).read_bytes() for path in module.OUTPUT_PATHS)
    document = json.loads(first[1])
    assert document["schemaVersion"] == module.SCHEMA_VERSION
    assert json.loads(first[0])["additionalProperties"] is False
    assert document["renderedMarkdownSha256"] == hashlib.sha256(first[2]).hexdigest()
    assert document["preSplitResult"]["result"]["sha256"] and document["decision"]["sha256"] == module.DECISION_SHA256


def test_generation_without_retained_capture_is_blocked(tiering_repository: Path, tmp_path: Path) -> None:
    capture = tiering_repository / module.CAPTURE_DIR
    parked = tmp_path / "parked-capture"
    outputs = tuple((tiering_repository / path).read_bytes() for path in module.OUTPUT_PATHS)
    if capture.exists():
        shutil.move(str(capture), str(parked))
    try:
        result = _cli(tiering_repository, *GENERATE_COMMAND[1:])
        assert result.returncode == 2
        assert re.findall(r"^BLOCKED: ([A-Z_]+):", result.stderr, re.M) == ["TIERING_PRE_SPLIT_RESULT_MISSING"]
    finally:
        if parked.exists():
            shutil.move(str(parked), str(capture))
    assert tuple((tiering_repository / path).read_bytes() for path in module.OUTPUT_PATHS) == outputs


def test_exact_generator_command_is_deterministic_from_a_capture(tiering_repository: Path,
                                                                  tmp_path_factory: pytest.TempPathFactory) -> None:
    """The exact AC-9.1-01 command exits 0 twice with identical bytes once a capture and digest decision exist."""
    document = json.loads((tiering_repository / module.OUTPUT_PATHS[1]).read_bytes())
    repository = tmp_path_factory.mktemp("story-9-1-cli") / "conversations"
    subprocess.run(["git", "clone", "--shared", "--quiet", str(ROOT), str(repository)], check=True, capture_output=True)
    _mirror_working_tree(ROOT, repository)
    for path in [*module.OUTPUT_PATHS, module.APPROVALS_PATH]:
        (repository / path).unlink(missing_ok=True)
    _synthetic_capture(repository, document)
    assert _cli(repository, "--repository", ".", "--propose-approvals").returncode == 0
    digest = json.loads((repository / module.APPROVALS_PATH).read_bytes())["proposal"]["membershipSha256"]
    assert _cli(repository, "--repository", ".", "--record-approval", "--approver", "SYNTHETIC-FIXTURE",
                "--approval-id", "SYNTHETIC-FIXTURE-NOT-AN-APPROVAL", "--approved-on", "2026-10-06",
                "--approved-membership-sha256", digest, "--approval-evidence", "disposable test clone only").returncode == 0
    first = _cli(repository, *GENERATE_COMMAND[1:])
    assert first.returncode == 0, first.stderr
    assert "PASS:" in first.stdout and module.SCHEMA_VERSION in first.stdout
    written = tuple((repository / path).read_bytes() for path in module.OUTPUT_PATHS)
    second = _cli(repository, *GENERATE_COMMAND[1:])
    assert second.returncode == 0, second.stderr
    assert tuple((repository / path).read_bytes() for path in module.OUTPUT_PATHS) == written
    assert written == module.generate(repository, module.CONTRACT_PATH, module.DECISION_PATH)
    assert _verify(repository) == (0, [])


def test_generation_and_recording_require_the_genuine_digest(tiering_repository: Path) -> None:
    approvals = tiering_repository / module.APPROVALS_PATH
    original = approvals.read_bytes()
    outputs = tuple((tiering_repository / path).read_bytes() for path in module.OUTPUT_PATHS)
    document = json.loads(original)
    try:
        document["decision"] = None
        document["status"] = "pending-quality-owner-approval"
        approvals.write_text(json.dumps(document, indent=2) + "\n")
        assert _verify(tiering_repository) == (1, ["TIER_APPROVAL_MISSING"])
        wrong = _cli(tiering_repository, "--repository", ".", "--record-approval", "--approver", "Someone",
                     "--approval-id", "WRONG", "--approved-on", "2026-10-06",
                     "--approved-membership-sha256", "0" * 64, "--approval-evidence", "wrong digest")
        assert wrong.returncode == 1 and "TIER_APPROVAL_MISSING" in wrong.stderr
        assert json.loads(approvals.read_bytes())["decision"] is None
    finally:
        approvals.write_bytes(original)
    assert tuple((tiering_repository / path).read_bytes() for path in module.OUTPUT_PATHS) == outputs
    assert _verify(tiering_repository) == (0, [])


def test_current_ci_lane_is_the_bound_lane(tiering_repository: Path) -> None:
    document = json.loads((tiering_repository / module.OUTPUT_PATHS[1]).read_bytes())
    workflow = _git(tiering_repository, "show", f"{document['preSplitResult']['sourceCommit']}:{module.CI_WORKFLOW_PATH}")
    lane = module.ci_lane(workflow)
    assert lane["exclusions"] == document["preSplitResult"]["lane"]["exclusions"]
    assert lane["buildCommand"][:3] == ["dotnet", "build", module.PROJECT_FILE]
    assert document["preSplitResult"]["generationIsolation"]["mode"] == "isolated-clone"
    assert set(module.GENERATION_METHODS) <= {row["id"] for row in document["assertions"]}


def _synthetic_capture(root: Path, document: dict[str, Any]) -> Path:
    """Write a machine-shaped capture from committed facts so capture parsing runs without .NET."""
    capture = root / module.CAPTURE_DIR
    (capture / "assemblies").mkdir(parents=True)
    code_base = "/synthetic/bin/Hexalith.Conversations.Conformance.Tests.dll"
    discovery = []
    definitions = []
    results = []
    for row in document["assertions"]:
        owner, method = row["id"].rsplit(".", 1)
        discovery.append({"Assembly": code_base, "DisplayName": row["id"], "ID": str(len(discovery)),
                          "Class": owner, "Method": method, "Explicit": False})
        for item in row["preSplitResultIdentity"]["results"]:
            identifier = f"00000000-0000-0000-0000-{len(definitions):012d}"
            definitions.append(f"<UnitTest name={quoteattr(item['testName'])} id={quoteattr(identifier)}>"
                               f"<TestMethod codeBase={quoteattr(code_base)} className={quoteattr(owner)} "
                               f"name={quoteattr(method)}/></UnitTest>")
            results.append(f"<UnitTestResult testName={quoteattr(item['testName'])} "
                           f"outcome={quoteattr(item['outcome'])}/>")
    summary = document["preSplitResult"]["result"]["summary"]
    trx = ('<?xml version="1.0" encoding="utf-8"?>\n'
           '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
           f'<ResultSummary><Counters total="{summary["total"]}" executed="{summary["executed"]}" '
           f'passed="{summary["passed"]}" failed="{summary["failed"]}"/></ResultSummary>'
           f'<TestDefinitions>{"".join(definitions)}</TestDefinitions><Results>{"".join(results)}</Results></TestRun>\n')
    files = {"discovery.json": json.dumps(discovery).encode(), "conformance.trx": trx.encode(),
             "build.log": b"build\n", "run.log": b"run\n", "discovery.log": b"discovery\n"}
    for name, content in files.items():
        (capture / name).write_bytes(content)
    facts = copy.deepcopy(document["preSplitResult"]["receiptFacts"])

    def bound(name: str) -> dict[str, str]:
        return {"path": f"{module.CAPTURE_DIR}/{name}", "sha256": hashlib.sha256(files[name]).hexdigest()}

    facts["discovery"]["result"] = bound("discovery.json")
    facts["discovery"]["log"] = bound("discovery.log")
    facts["run"]["result"] = bound("conformance.trx")
    facts["run"]["log"] = bound("run.log")
    facts["run"]["codeBase"] = code_base
    facts["build"]["log"] = bound("build.log")
    for item in [facts["testAssembly"], *facts["moduleAssemblies"]]:
        content = f"synthetic {item.get('name', 'test assembly')}".encode()
        (root / item["retainedCopy"]).write_bytes(content)
        item["sha256"] = hashlib.sha256(content).hexdigest()
    (capture / "receipt.json").write_text(json.dumps(facts, indent=2))
    return capture


def test_pre_split_capture_parses_and_rejects_tampering(tiering_repository: Path, tmp_path: Path) -> None:
    document = json.loads((tiering_repository / module.OUTPUT_PATHS[1]).read_bytes())
    capture = _synthetic_capture(tmp_path, document)
    loaded = module.load_pre_split(tmp_path)
    embedded = module.pre_split_from_document(document)
    assert (loaded.discovered, loaded.results, loaded.exclusions) == (
        embedded.discovered, embedded.results, embedded.exclusions)
    assert loaded.summary == embedded.summary
    trx = capture / "conformance.trx"
    original = trx.read_bytes()
    trx.write_bytes(original.replace(b'outcome="Passed"', b'outcome="Failed"', 1))
    with pytest.raises(module.TieringError) as failure:
        module.load_pre_split(tmp_path)
    assert failure.value.code == "TIERING_PRE_SPLIT_RESULT_INVALID"
    trx.write_bytes(original)
    discovery = capture / "discovery.json"
    entries = json.loads(discovery.read_bytes())
    entries.append(dict(entries[0]))
    discovery.write_text(json.dumps(entries))
    receipt = json.loads((capture / "receipt.json").read_bytes())
    receipt["discovery"]["result"]["sha256"] = hashlib.sha256(discovery.read_bytes()).hexdigest()
    (capture / "receipt.json").write_text(json.dumps(receipt))
    with pytest.raises(module.TieringError) as failure:
        module.load_pre_split(tmp_path)
    assert failure.value.code == "CONFORMANCE_DISCOVERY_UNSUPPORTED"
    (capture / "receipt.json").unlink()
    with pytest.raises(module.TieringError) as failure:
        module.load_pre_split(tmp_path)
    assert failure.value.code == "TIERING_PRE_SPLIT_RESULT_MISSING"


def test_verification_is_read_only(tiering_repository: Path) -> None:
    before = {path: (tiering_repository / path).read_bytes() for path in [*module.OUTPUT_PATHS, module.APPROVALS_PATH]}
    assert _verify(tiering_repository) == (0, [])
    assert before == {path: (tiering_repository / path).read_bytes() for path in before}


# --------------------------------------------------------------------------- measured faults


def _fixture_digest(repository: Path) -> str:
    """Hash every fixture byte stream bound to its ordered relative path; absence is explicit."""
    streams = []
    for path in FIXTURE_PATHS:
        target = repository / path
        content = target.read_bytes() if target.is_file() else b"\0absent"
        streams.append(path.encode() + b"\0" + content + b"\0")
    return hashlib.sha256(b"".join(streams)).hexdigest()


def _apply_fault(repository: Path, fault_id: str) -> None:
    target = repository / TARGET
    source = target.read_text(encoding="utf-8")
    disposition = repository / module.OUTPUT_PATHS[1]
    document = json.loads(disposition.read_bytes())
    rows = document["assertions"]
    if fault_id in ("assertion-missing", "assertion-renamed", "strength-weakened"):
        assert source.count(TARGET_BLOCK) == 1
        if fault_id == "assertion-missing":
            source = source.replace(TARGET_BLOCK, "")
        elif fault_id == "assertion-renamed":
            source = source.replace(TARGET_BLOCK, TARGET_BLOCK.replace(TARGET_METHOD, TARGET_METHOD + "Renamed"))
        else:
            source = source.replace(TARGET_BLOCK, TARGET_BLOCK.replace("        run.Checks.Count.ShouldBe(12);\n", ""))
        target.write_text(source, encoding="utf-8")
        return
    if fault_id == "public-widened":
        (repository / WIDENING).write_text(
            "namespace Hexalith.Conversations.Contracts.Conformance;\n\n"
            "/// <summary>A public type added only to prove widening detection.</summary>\n"
            "public sealed record TieringFaultProbeWidening(string Value);\n", encoding="utf-8")
        return
    if fault_id == "v1-mutated":
        path = repository / V1_FILE
        path.write_bytes(path.read_bytes() + b"\n")
        return
    if fault_id == "assertion-duplicate":
        rows.insert(1, copy.deepcopy(rows[0]))
    elif fault_id == "tier-unassigned":
        del rows[0]["tier"]
    elif fault_id == "tier-reason-missing":
        internal = next(row for row in rows if row["tier"] == "module-internal")
        del internal["internalTypeAndReason"]["reason"]
    elif fault_id == "approval-missing":
        rows[0]["approval"] = None
    elif fault_id == "denominator-drift":
        document["denominatorSuites"][0]["v1FloorTestIds"].pop()
    else:  # pragma: no cover - the parametrization is closed
        raise AssertionError(fault_id)
    disposition.write_bytes((json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode("utf-8"))


def _measured_fault(repository: Path, fault_id: str) -> dict[str, Any]:
    """Measure baseline PASS, the exact single blocker, finally-restoration, and restored PASS."""
    originals = {path: (repository / path).read_bytes() if (repository / path).is_file() else None
                 for path in FIXTURE_PATHS}
    baseline_exit, baseline_blockers = _verify(repository)
    assert (baseline_exit, baseline_blockers) == (0, [])
    before = _fixture_digest(repository)
    try:
        _apply_fault(repository, fault_id)
        mutated = _fixture_digest(repository)
        assert mutated != before
        observed_exit, observed_blockers = _verify(repository)
        assert observed_exit == 1
        assert observed_blockers == [FAULTS[fault_id]]
    finally:
        for path, content in originals.items():
            target = repository / path
            if content is None:
                target.unlink(missing_ok=True)
            else:
                target.write_bytes(content)
    after = _fixture_digest(repository)
    restored_exit, restored_blockers = _verify(repository)
    assert after == before
    assert (restored_exit, restored_blockers) == (0, [])
    return {"id": fault_id, "candidateCommit": CANDIDATE, "expectedBlocker": FAULTS[fault_id],
            "observedExitCode": observed_exit, "observedBlockers": observed_blockers,
            "beforeSha256": before, "mutatedSha256": mutated, "afterSha256": after,
            "baselineExitCode": baseline_exit, "baselineBlockers": baseline_blockers,
            "restoredExitCode": restored_exit, "restoredBlockers": restored_blockers}


@pytest.mark.parametrize("fault_id", list(FAULTS))
def test_tiering_faults(tiering_repository: Path, fault_id: str, record_property: Any) -> None:
    """Each mandatory defect category yields its exact blocker through the real CLI and restores byte-identically."""
    record_property(FAULT_PROPERTY, json.dumps(_measured_fault(tiering_repository, fault_id), sort_keys=True))
