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
from unittest.mock import patch
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
        elif destination.is_symlink() or destination.is_file():
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
    committed = json.loads((tiering_repository / module.OUTPUT_PATHS[1]).read_bytes())

    def rendered() -> tuple[bytes, bytes, bytes]:
        derivation = module.derive(tiering_repository, module.CONTRACT_PATH, module.DECISION_PATH,
                                   pre_split=_pre_split(tiering_repository, committed))
        assert derivation.findings.items == []
        return module.render(derivation.document)

    first = rendered()
    second = rendered()
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
    wrong = _cli(repository, "--repository", ".", "--record-approval", "--approver", "Someone",
                 "--approval-id", "WRONG", "--approved-on", "2026-10-06",
                 "--approved-membership-sha256", "0" * 64, "--approval-evidence", "wrong digest")
    assert wrong.returncode == 1
    assert re.findall(r"^FAIL: ([A-Z_]+):", wrong.stderr, re.M) == ["TIER_APPROVAL_MISSING"]
    assert json.loads((repository / module.APPROVALS_PATH).read_bytes())["decision"] is None
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


# --------------------------------------------------------------------------- live Story 9.2 faults

live_spec = importlib.util.spec_from_file_location("story92_live_verifier", ROOT / "_bmad/scripts/verify_conformance_tiering.py")
assert live_spec is not None and live_spec.loader is not None
story92 = importlib.util.module_from_spec(live_spec)
live_spec.loader.exec_module(story92)


def test_portable_surface_rejects_imported_extra_direct_reference() -> None:
    project = ROOT / story92.PROJECTS["portable"]
    approved = ("Hexalith.Conversations", "Hexalith.Conversations.Client",
                "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Testing")
    references = [{"FullPath": str(ROOT / "src" / name / f"{name}.csproj"),
                   "DefiningProjectFullPath": str(project)} for name in approved]
    references.append({"FullPath": str(ROOT / "src/Hexalith.Extra/Hexalith.Extra.csproj"),
                       "DefiningProjectFullPath": str(ROOT / "Directory.Build.props")})
    with patch.object(story92, "msbuild", side_effect=AssertionError("direct references must be checked first")):
        with pytest.raises(story92.VerificationError) as failure:
            story92.portable_surface(ROOT, {"portable": {"Items": {"ProjectReference": references}}})
    assert failure.value.code == "PORTABLE_TIER_NONPORTABLE_REFERENCE"


def test_verifier_output_rejects_protected_file_and_preserves_bytes(tmp_path: Path) -> None:
    protected = tmp_path / ".github/workflows/release.yml"
    protected.parent.mkdir(parents=True)
    protected.write_bytes(b"protected workflow\n")
    with pytest.raises(story92.VerificationError) as failure:
        story92.write_json(tmp_path, ".github/workflows/release.yml", {"result": "PASS"})
    assert failure.value.code == "OUTPUT_PATH_INVALID"
    assert protected.read_bytes() == b"protected workflow\n"
    story92.write_json(tmp_path, "TestResults/conformance/tiering.json", {"result": "PASS"})
    assert json.loads((tmp_path / "TestResults/conformance/tiering.json").read_bytes()) == {"result": "PASS"}


def _write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def _synthetic_tier_result(repository: Path, tier: str, destination: Path) -> None:
    """Synthetic passing cases live only in the disposable negative-test fixture."""
    frozen = json.loads((repository / story92.DISPOSITION).read_bytes())
    names = sorted(item["testName"] for row in frozen["assertions"]
                   if row["tier"] == tier and row["preSplitResultIdentity"]["lane"] == "executed"
                   for item in row["preSplitResultIdentity"]["results"])
    names += story92.CONTROL_IDS[tier]
    name = Path(story92.PROJECTS[tier]).stem
    binary = repository / Path(story92.PROJECTS[tier]).parent / "bin/Release/net10.0" / (name + ".dll")
    binary.parent.mkdir(parents=True, exist_ok=True)
    # An explicit fixture marker prevents this file from being confused with an acceptance assembly.
    binary.write_bytes(b"SYNTHETIC-STORY92-NEGATIVE-FIXTURE-NOT-ACCEPTANCE-EVIDENCE")
    identities = {item["testName"]: row["id"] for row in frozen["assertions"]
                  if row["tier"] == tier and row["preSplitResultIdentity"]["lane"] == "executed"
                  for item in row["preSplitResultIdentity"]["results"]}
    identities.update({value: value for value in story92.CONTROL_IDS[tier]})
    results = "".join(f'<UnitTestResult testId="fixture-{index:04}" testName={quoteattr(value)} outcome="Passed" />'
                      for index, value in enumerate(names))
    definitions = "".join('<UnitTest id="fixture-' + f'{index:04}' + '" name=' + quoteattr(value)
                          + '><TestMethod codeBase=' + quoteattr(str(binary))
                          + ' className=' + quoteattr(identities[value].rsplit('.', 1)[0])
                          + ' name=' + quoteattr(identities[value].rsplit('.', 1)[1]) + '/></UnitTest>'
                          for index, value in enumerate(names))
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(
        '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>' + results
        + '</Results><TestDefinitions>' + definitions + '</TestDefinitions><ResultSummary outcome="Completed"><Counters '
        + f'total="{len(names)}" executed="{len(names)}" passed="{len(names)}" failed="0" notExecuted="0"'
        + '/></ResultSummary></TestRun>', encoding="utf-8")


@pytest.fixture(scope="module")
def structural_execution_repository(tmp_path_factory: pytest.TempPathFactory) -> Path:
    """Use real MSBuild evaluation/source derivation, with explicitly synthetic execution/approval fixtures."""
    repository = tmp_path_factory.mktemp("story-9-2-faults") / "conversations"
    subprocess.run(["git", "clone", "--shared", "--quiet", str(ROOT), str(repository)], check=True, capture_output=True)
    _mirror_working_tree(ROOT, repository)
    # Copy ordinary centralized configuration without initializing any submodule.
    props = repository / "references/Hexalith.Builds/Props"
    props.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props", props)
    # Existing restored source-reference graphs can reach root-declared dependencies.
    # Copy only their evaluated projects and ordinary ancestor build configuration;
    # do not initialize/update submodules or turn a package graph into a source graph.
    surface = story92.portable_surface(ROOT, {"portable": story92.msbuild(ROOT, story92.PROJECTS["portable"], "Release", resolved=True)})
    # Resolve the same restored compile assets without dependency updates or rebuilding fixtures.
    projects = {*story92.PROJECTS.values(), *(f"src/{row['name']}/{row['name']}.csproj"
                for row in json.loads((ROOT / story92.DISPOSITION).read_bytes())["moduleAssemblies"]),
                *(row["project"] for row in surface["evaluatedProjects"])}
    for project in sorted(projects):
        source = ROOT / Path(project).parent
        target = repository / Path(project).parent
        if project.startswith("references/"):
            shutil.copytree(source, target, dirs_exist_ok=True, ignore=shutil.ignore_patterns("bin", "obj"))
            for ancestor in (source, *source.parents):
                if ancestor == ROOT:
                    break
                for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json"):
                    if (ancestor / name).is_file():
                        destination = repository / ancestor.relative_to(ROOT) / name
                        destination.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copy2(ancestor / name, destination)
        for folder in ("bin/Release", "obj"):
            if (source / folder).is_dir():
                shutil.copytree(source / folder, target / folder, dirs_exist_ok=True)
        # NuGet's generated files embed the original restore root. Rebase their paths inside the disposable clone.
        for path in (target / "obj").rglob("*"):
            if path.is_file() and path.suffix in (".json", ".props", ".targets"):
                path.write_bytes(path.read_bytes().replace(str(ROOT).encode(), str(repository).encode()))
    proposal = story92.derive_migration(repository, current_tree=True)
    _write_json(repository / story92.MIGRATION, proposal)
    approval = {"schemaVersion": "hexalith.conversations.conformance-oracle-tiering-migration-approval.v3",
                "status": "approved", "role": "Quality owner", "approver": "SYNTHETIC-FIXTURE",
                "approvalId": "SYNTHETIC-FIXTURE-NOT-AN-APPROVAL", "approvedOn": "2026-10-07",
                "evidence": "Disposable negative-test fixture only; no repository or public drift approval.",
                "binding": {"proposalSha256": proposal["proposalSha256"],
                            "changedAssertionRows": [[row["id"], row["rowSha256"]] for row in proposal["proposal"]["changedAssertions"]],
                            "publicDriftSha256": proposal["proposal"]["publicSurface"]["driftSha256"]}}
    _write_json(repository / story92.APPROVAL, approval)
    for tier, path in (("portable", "artifacts/v9/9.2/portable.trx"), ("module-internal", "artifacts/v9/9.2/internal.trx")):
        _synthetic_tier_result(repository, tier, repository / path)
    return repository


def _verify_story92(repository: Path) -> dict[str, Any]:
    assert repository.resolve() != ROOT.resolve()
    # These private overrides apply only to the disposable negative fixture.
    # Ordinary verification rejects both synthetic identities and marker-only binaries.
    with patch.object(story92, "_genuine_approval_identity", return_value=True), \
            patch.object(story92, "_execution_binary_is_managed", return_value=True), \
            patch.object(story92, "_require_execution_binary_identity", return_value=None):
        return story92.verify(repository, portable_result="artifacts/v9/9.2/portable.trx", internal_result="artifacts/v9/9.2/internal.trx", current_tree=True)


def _story92_fault(repository: Path, fault_id: str) -> None:
    source_path = repository / TARGET
    source = source_path.read_text(encoding="utf-8")
    migration_path = repository / story92.MIGRATION
    migration = json.loads(migration_path.read_bytes())
    if fault_id in ("assertion-deleted", "assertion-duplicated", "assertion-renamed", "assertion-weakened"):
        assert source.count(TARGET_BLOCK) == 1
        if fault_id == "assertion-deleted":
            source = source.replace(TARGET_BLOCK, "")
        elif fault_id == "assertion-duplicated":
            source = source.replace(TARGET_BLOCK, TARGET_BLOCK + TARGET_BLOCK)
        elif fault_id == "assertion-renamed":
            source = source.replace(TARGET_METHOD, TARGET_METHOD + "Renamed")
        else:
            source = source.replace("        run.Checks.Count.ShouldBe(12);\n", "")
        source_path.write_text(source, encoding="utf-8")
    elif fault_id == "nonportable-reference":
        path = repository / story92.PROJECTS["portable"]
        path.write_text(path.read_text().replace('</Project>', '<ItemGroup><ProjectReference Include="../../src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj" Condition="\'$(Configuration)\' == \'Release\'" /></ItemGroup></Project>'))
    elif fault_id == "transitive-nonportable-reference":
        path = repository / "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj"
        path.write_text(path.read_text().replace('</Project>', '<ItemGroup><ProjectReference Include="../Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj" /></ItemGroup></Project>'))
    elif fault_id in ("resolved-nonportable-reference", "renamed-nonportable-reference"):
        path = repository / story92.PROJECTS["portable"]
        hint = "../../src/Hexalith.Conversations.Server/bin/Release/net10.0/Hexalith.Conversations.Server.dll"
        name = "Hexalith.Conversations.Server"
        if fault_id == "renamed-nonportable-reference":
            renamed = repository / "artifacts/v9/9.2/RenamedCompileReference.dll"
            shutil.copy2(repository / "src/Hexalith.Conversations.Server/bin/Release/net10.0/Hexalith.Conversations.Server.dll", renamed)
            hint = "../../artifacts/v9/9.2/RenamedCompileReference.dll"
            name = "RenamedCompileReference"
        path.write_text(path.read_text().replace('</Project>', f'<ItemGroup><Reference Include="{name}"><HintPath>{hint}</HintPath></Reference></ItemGroup></Project>'))
    elif fault_id == "project-missing":
        (repository / story92.PROJECTS["portable"]).unlink()
    elif fault_id == "declaration-missing":
        path = repository / "Hexalith.Conversations.slnx"
        path.write_text(path.read_text().replace('    <Project Path="' + story92.PROJECTS["portable"] + '" />\n', ""))
    elif fault_id == "completion-declaration-missing":
        path = repository / story92.AMENDMENT
        value = json.loads(path.read_bytes()); del value["projects"]["portable"]
        _write_json(path, value)
    elif fault_id == "approval-missing":
        (repository / story92.APPROVAL).unlink()
    elif fault_id == "tier-missing":
        del migration["proposal"]["assertions"][0]["tier"]
        _write_json(migration_path, migration)
    elif fault_id == "reason-missing":
        row = next(row for row in migration["proposal"]["assertions"] if row["tier"] == "module-internal")
        del row["dispositionEvidence"]["internalTypeAndReason"]["reason"]
        _write_json(migration_path, migration)
    elif fault_id == "denominator-drift":
        migration["proposal"]["fr20Membership"]["v1Floor"]["testCount"] -= 1
        _write_json(migration_path, migration)
    elif fault_id == "predecessor-disposition-detached":
        path = repository / story92.DISPOSITION
        value = json.loads(path.read_bytes()); value["assertions"][0]["rationale"] += " detached"
        _write_json(path, value)
        amendment_path = repository / story92.AMENDMENT
        amendment = json.loads(amendment_path.read_bytes()); amendment["disposition"] = story92.bound(repository, story92.DISPOSITION)
        _write_json(amendment_path, amendment)
    elif fault_id == "public-widened":
        (repository / WIDENING).write_text('namespace Hexalith.Conversations.Contracts.Conformance; public sealed record TieringFaultProbeWidening(string Value);')
    elif fault_id == "v1-mutated":
        path = repository / V1_FILE; path.write_bytes(path.read_bytes() + b"\n")
    else:
        path = repository / "artifacts/v9/9.2/portable.trx"
        tree = module.RECORD.ElementTree.fromstring(path.read_bytes())
        results = tree.find('./{*}Results'); counters = tree.find('./{*}ResultSummary/{*}Counters')
        assert results is not None and counters is not None
        if fault_id == "execution-empty":
            results.clear()
            for key in ("total", "executed", "passed", "failed", "notExecuted"): counters.set(key, "0")
        elif fault_id == "execution-skipped":
            results[0].set("outcome", "NotExecuted")
            counters.set("notExecuted", "1")
            for key in ("executed", "passed"): counters.set(key, str(int(counters.get(key, "0")) - 1))
        elif fault_id == "execution-not-run":
            results.remove(results[0]); counters.set("notExecuted", "1")
            for key in ("executed", "passed"): counters.set(key, str(int(counters.get(key, "0")) - 1))
        elif fault_id == "execution-regressed":
            results.remove(results[0])
            for key in ("total", "executed", "passed"): counters.set(key, str(int(counters.get(key, "0")) - 1))
        else:
            raise AssertionError(fault_id)
        path.write_bytes(module.RECORD.ElementTree.tostring(tree))


@pytest.mark.parametrize("fault_id", list(story92.FAULTS))
def test_structural_and_execution_faults(structural_execution_repository: Path, fault_id: str, record_property: Any) -> None:
    """Real evaluated/source verifier detects each defect and restores every mutated byte stream exactly."""
    repository = structural_execution_repository
    measured_candidate = _git(ROOT, "rev-parse", "--verify", "HEAD^{commit}").decode().strip()
    paths = [TARGET, story92.MIGRATION, story92.APPROVAL, story92.DISPOSITION, story92.AMENDMENT,
             story92.PROJECTS["portable"], "Hexalith.Conversations.slnx", WIDENING, V1_FILE,
             "src/Hexalith.Conversations.Testing/Hexalith.Conversations.Testing.csproj",
             "artifacts/v9/9.2/portable.trx", "artifacts/v9/9.2/internal.trx",
             "artifacts/v9/9.2/RenamedCompileReference.dll"]
    source_digest = story92.fault_source_digest(ROOT)
    assert story92.fault_source_digest(repository) == source_digest
    originals = {path: (repository / path).read_bytes() if (repository / path).is_file() else None for path in paths}

    def digest() -> str:
        return hashlib.sha256(b"".join(path.encode() + b"\0" + ((repository / path).read_bytes()
                      if (repository / path).is_file() else b"\0absent") + b"\0" for path in sorted(paths))).hexdigest()

    baseline = _verify_story92(repository)
    assert baseline["result"] == "PASS", baseline
    before = digest()
    try:
        _story92_fault(repository, fault_id)
        mutated = digest()
        assert mutated != before
        observed = (story92.verify(repository, mode="surface") if fault_id == "transitive-nonportable-reference"
                    else _verify_story92(repository))
        assert observed["exitCode"] == 1 and [row["code"] for row in observed["blockers"]] == [story92.FAULTS[fault_id]], observed
    finally:
        for path, content in originals.items():
            target = repository / path
            if content is None:
                target.unlink(missing_ok=True)
            else:
                target.write_bytes(content)
    after = digest()
    restored = _verify_story92(repository)
    assert after == before and restored["result"] == "PASS", restored
    assert _git(ROOT, "rev-parse", "--verify", "HEAD^{commit}").decode().strip() == measured_candidate
    assert story92.fault_source_digest(ROOT) == source_digest
    record_property(story92.FAULT_PROPERTY, json.dumps({"id": fault_id, "candidateCommit": measured_candidate,
        "expectedBlocker": story92.FAULTS[fault_id], "observedExitCode": observed["exitCode"],
        "observedBlockers": [row["code"] for row in observed["blockers"]],
        "beforeSha256": before, "mutatedSha256": mutated, "afterSha256": after,
        "baselineExitCode": baseline["exitCode"], "baselineBlockers": baseline["blockers"],
        "restoredExitCode": restored["exitCode"], "restoredBlockers": restored["blockers"],
        "sourceInputsSha256": source_digest,
        "syntheticExecutionAndApprovalFixture": True}, sort_keys=True))


@pytest.mark.parametrize("current_tree", [False, True])
def test_current_tree_migration_allows_dependency_versions_only_in_live_mode(
        structural_execution_repository: Path, current_tree: bool) -> None:
    repository = structural_execution_repository
    derived = json.loads((repository / story92.MIGRATION).read_bytes())
    # A resolved version change is measured freshly and is not an assertion-strength change.
    asset = next(row for row in derived["proposal"]["portableSurface"]["transitiveCompileAssets"]
                 if row["library"].startswith("Hexalith.EventStore.Contracts/"))
    asset["library"] = "Hexalith.EventStore.Contracts/3.117.2"
    with patch.object(story92, "_genuine_approval_identity", return_value=True):
        if current_tree:
            assert story92.approved_migration(repository, derived, current_tree=True) == story92.bound(repository, story92.APPROVAL)
        else:
            with pytest.raises(story92.VerificationError) as failure:
                story92.approved_migration(repository, derived)
            assert failure.value.code == "ASSERTION_STRENGTH_WEAKENED"


@pytest.mark.parametrize("mutation,blocker", [
    ("successor-strength", "ASSERTION_STRENGTH_WEAKENED"),
    ("approval-binding", "TIER_APPROVAL_MISSING"),
    ("proposal-digest", "TIERING_INPUT_INVALID"),
    ("control-strength", "ASSERTION_STRENGTH_WEAKENED"),
    ("schema", "TIERING_INPUT_INVALID"),
    ("status", "TIERING_INPUT_INVALID"),
    ("approval-requirements", "TIERING_INPUT_INVALID"),
])
def test_current_tree_migration_keeps_exact_strength_and_approval_bindings(
        structural_execution_repository: Path, mutation: str, blocker: str) -> None:
    repository = structural_execution_repository
    derived = json.loads((repository / story92.MIGRATION).read_bytes())
    path = repository / (story92.APPROVAL if mutation == "approval-binding" else story92.MIGRATION)
    original = path.read_bytes()
    try:
        if mutation == "successor-strength":
            row = next(row for row in derived["proposal"]["assertions"]
                       if row["sourcePath"] in story92.AUTHORIZED_SUCCESSOR_FILES)
            row["strengthSha256"] = "0" * 64
        elif mutation == "control-strength":
            row = derived["proposal"]["liveControls"][0]
            row["strengthSha256"] = "0" * 64
            row["strengthMaterial"]["boundAssemblies"] = []
        elif mutation in ("schema", "status", "approval-requirements"):
            recorded = json.loads(original)
            recorded[{"schema": "schemaVersion", "status": "status", "approval-requirements": "approvalRequired"}[mutation]] = "invalid"
            _write_json(path, recorded)
        elif mutation == "approval-binding":
            approval = json.loads(original)
            approval["binding"]["proposalSha256"] = "0" * 64
            _write_json(path, approval)
        else:
            recorded = json.loads(original)
            recorded["proposalSha256"] = "0" * 64
            _write_json(path, recorded)
        with patch.object(story92, "_genuine_approval_identity", return_value=True):
            with pytest.raises(story92.VerificationError) as failure:
                story92.approved_migration(repository, derived, current_tree=True)
        assert failure.value.code == blocker
    finally:
        path.write_bytes(original)


def test_current_tree_source_guard_preserves_historical_default() -> None:
    with pytest.raises(story92.VerificationError) as failure:
        story92.inputs(ROOT)
    assert failure.value.code == "PUBLIC_CONTRACT_WIDENED"
    assert story92.inputs(ROOT, current_tree=True)[0]["fr20Membership"]["v1Floor"]["testCount"] == 214


def test_current_tree_declarations_require_regression_selection(monkeypatch: pytest.MonkeyPatch) -> None:
    original_read = story92.read
    def read_without_regressions(root: Path, path: str) -> bytes:
        content = original_read(root, path)
        return (content.replace(b'"structural_and_execution_faults or current_tree"', b'structural_and_execution_faults')
                if path == story92.TIERING.CI_WORKFLOW_PATH else content)
    monkeypatch.setattr(story92, "read", read_without_regressions)
    assert story92.declarations(ROOT)
    with pytest.raises(story92.VerificationError) as failure:
        story92.declarations(ROOT, current_tree=True)
    assert failure.value.code == "TIER_NOT_DECLARED"
