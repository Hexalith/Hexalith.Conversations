"""Story 11.2 Checkpoint B: fixture verifier, proof generator, and restore-checked fault matrix."""

from __future__ import annotations

import hashlib
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "_bmad/scripts"
sys.path.insert(0, str(SCRIPTS))
import generate_minimal_module_fixture_proof as proof  # noqa: E402
import generate_thin_authoring_guidance as guidance  # noqa: E402
import verify_minimal_module_fixture as verifier  # noqa: E402
import run_minimal_module_command as command_runner  # noqa: E402

FIXTURE = verifier.FIXTURE_ROOT
CONTRACT = verifier.CONTRACT_PATH
TRX = verifier.TEST_RESULT_PATH
JSON_OUT = verifier.PROOF_JSON_PATH
MD_OUT = verifier.PROOF_MARKDOWN_PATH
SCHEMA = verifier.PROOF_SCHEMA_PATH
OUT = "out/result.json"
TRX_NAMESPACE = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"

INPUTS = [
    CONTRACT, SCHEMA, guidance.CONTRACT_PATH, guidance.SOURCE_PATH,
    verifier.GUIDANCE_JSON, verifier.GUIDANCE_MARKDOWN, verifier.STORY_11_1_RECORD_JSON,
    verifier.STORY_11_1_RECORD_MARKDOWN, "docs/release-evidence/thin-authoring-guidance-v2.schema.json",
    "global.json",
] + [path for _, path, _ in guidance.PREDECESSORS]


def fixture_input_paths() -> list[str]:
    root = ROOT / FIXTURE
    return [f"{FIXTURE}/{item}" for item in verifier.fixture_files(root)]


def trx_text(names: list[str], outcome: dict[str, str] | None = None) -> str:
    rows = "".join(
        f'<UnitTestResult testName="Hexalith.Conversations.MinimalModule.Tests.MinimalModuleFixtureTests.{name}" '
        f'outcome="{(outcome or {}).get(name, "Passed")}" />'
        for name in names)
    return f'<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="{TRX_NAMESPACE}"><Results>{rows}</Results></TestRun>\n'


ALL_TESTS = sorted(set(verifier.CAPABILITY_TESTS.values()))


def run(script: str, *args: str, cwd: Path) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, str(SCRIPTS / script), *args], cwd=cwd,
                          capture_output=True, text=True, check=False)


def generate(repo: Path) -> subprocess.CompletedProcess:
    return run("generate_minimal_module_fixture_proof.py", "--repository", ".", "--contract", CONTRACT,
               "--fixture", FIXTURE, "--test-result", TRX, "--output-json", JSON_OUT,
               "--output-markdown", MD_OUT, cwd=repo)


def verify(repo: Path, *, inventory_only: bool = False, with_proof: bool = False) -> tuple[int, list[str]]:
    args = ["--repository", ".", "--contract", CONTRACT, "--fixture", FIXTURE, "--output", OUT]
    if inventory_only:
        args.append("--inventory-only")
    else:
        args += ["--test-result", TRX]
    if with_proof:
        args += ["--proof-json", JSON_OUT, "--proof-markdown", MD_OUT]
    result = run("verify_minimal_module_fixture.py", *args, cwd=repo)
    report = json.loads((repo / OUT).read_text(encoding="utf-8"))
    return result.returncode, [item["code"] for item in report["blockers"]]


def snapshot(repo: Path) -> dict[str, str]:
    return {str(p.relative_to(repo)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(repo.rglob("*"))
            if p.is_file() and ".git" not in p.relative_to(repo).parts and "out" not in p.relative_to(repo).parts}


def contents(repo: Path) -> dict[str, bytes]:
    return {str(p.relative_to(repo)): p.read_bytes() for p in sorted(repo.rglob("*"))
            if p.is_file() and ".git" not in p.relative_to(repo).parts and "out" not in p.relative_to(repo).parts}


def restore(repo: Path, saved: dict[str, bytes]) -> None:
    for path, data in saved.items():
        target = repo / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    for path in list(contents(repo)):
        if path not in saved:
            (repo / path).unlink()
    for directory in sorted((p for p in repo.rglob("*") if p.is_dir() and ".git" not in p.relative_to(repo).parts),
                            key=lambda item: len(item.parts), reverse=True):
        if not any(directory.iterdir()):
            directory.rmdir()


def git(repo: Path, *args: str) -> None:
    subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True)


# The platform submodule is never initialized in CI: stand-in project files carry only the reference edges
# the proof walks (DomainService -> Client -> Contracts, DomainService -> ServiceDefaults).
PLATFORM_SRC = "references/Hexalith.EventStore/src"
PLATFORM_STUBS = {
    f"{PLATFORM_SRC}/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj": [],
    f"{PLATFORM_SRC}/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj": ["..\\Hexalith.EventStore.Contracts\\Hexalith.EventStore.Contracts.csproj"],
    f"{PLATFORM_SRC}/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj": [],
    f"{PLATFORM_SRC}/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj": [
        "..\\Hexalith.EventStore.Client\\Hexalith.EventStore.Client.csproj",
        "..\\Hexalith.EventStore.ServiceDefaults\\Hexalith.EventStore.ServiceDefaults.csproj"],
    f"{PLATFORM_SRC}/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj": [],
}


def write_platform_stubs(repo: Path) -> None:
    for relative, references in PLATFORM_STUBS.items():
        target = repo / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        items = "".join(f'    <ProjectReference Include="{reference}" />\n' for reference in references)
        target.write_text(f'<Project Sdk="Microsoft.NET.Sdk">\n  <ItemGroup>\n{items}  </ItemGroup>\n</Project>\n',
                          encoding="utf-8", newline="\n")
    # A real, deterministic local checkout proves the same identity/cleanliness contract as the live submodule.
    platform = repo / proof.PLATFORM_ROOT
    (platform / "src/PlatformFixture.cs").write_text("// Hermetic platform source.\n", encoding="utf-8", newline="\n")
    git(platform, "init", "--quiet")
    git(platform, "config", "user.email", "fixture@example.invalid")
    git(platform, "config", "user.name", "Fixture")
    git(platform, "config", "core.autocrlf", "false")
    git(platform, "add", "--all")
    subprocess.run(["git", "commit", "--quiet", "-m", "test: platform fixture"], cwd=platform,
                   env={**os.environ, "GIT_AUTHOR_DATE": "2000-01-01T00:00:00Z",
                        "GIT_COMMITTER_DATE": "2000-01-01T00:00:00Z"}, check=True, capture_output=True)


def write_measured_command_inputs(repo: Path) -> dict[str, bytes]:
    """Hermetic NuGet metadata and measured-command stand-ins for proof/route unit tests."""
    folder = repo / FIXTURE / "obj/packages"
    for project in verifier.EXPECTED_PROJECTS:
        directory = repo / FIXTURE / Path(project).parent
        (directory / "obj").mkdir(parents=True, exist_ok=True)
        (directory / "obj/project.assets.json").write_text(json.dumps({"packageFolders": {str(folder): {}}}))
        for name, version, _ in proof.lock_packages((directory / "packages.lock.json").read_bytes()):
            metadata = folder / name.lower() / version.lower() / ".nupkg.metadata"
            metadata.parent.mkdir(parents=True, exist_ok=True)
            metadata.write_text(json.dumps({"source": "https://api.nuget.org/v3/index.json"}))
    contract = json.loads((repo / CONTRACT).read_bytes())
    receipts = {}
    for scenario, path in verifier.COMMAND_RECEIPTS.items():
        command = next(row["command"] for row in contract["scenarios"] if row["id"] == scenario)
        policy = "lockedMode" if scenario == "AC-11.2-02" else "warningsAsErrors"
        receipts[path] = (json.dumps({"schemaVersion": verifier.COMMAND_RECEIPT_SCHEMA, "scenarioId": scenario,
                                     "command": command, "exitCode": 0, "consoleLog": "Measured test execution\n",
                                     policy: True}, indent=2) + "\n").encode()
        (repo / path).parent.mkdir(parents=True, exist_ok=True)
        (repo / path).write_bytes(receipts[path])
    return receipts


@pytest.fixture()
def repo(tmp_path: Path) -> Path:
    for relative in [*INPUTS, *fixture_input_paths()]:
        target = tmp_path / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)
    write_platform_stubs(tmp_path)
    git(tmp_path, "init", "--quiet")
    git(tmp_path, "config", "user.email", "fixture@example.invalid")
    git(tmp_path, "config", "user.name", "Fixture")
    git(tmp_path, "config", "core.autocrlf", "false")
    git(tmp_path, "add", "--all")
    git(tmp_path, "commit", "--quiet", "-m", "test: fixture candidate")
    # Build outputs and the measured result are generated, untracked inputs.
    names = {Path(path).stem for path in PLATFORM_STUBS}
    tests_bin = tmp_path / FIXTURE / "tests/Hexalith.Conversations.MinimalModule.Tests" / proof.OUTPUT_DIRECTORY
    tests_bin.mkdir(parents=True, exist_ok=True)
    for name in sorted(names):
        (tests_bin / f"{name}.dll").write_bytes(f"platform assembly {name}".encode())
    for relative in verifier.EXPECTED_PROJECTS:
        directory = tmp_path / FIXTURE / Path(relative).parent / proof.OUTPUT_DIRECTORY
        directory.mkdir(parents=True, exist_ok=True)
        (directory / f"{Path(relative).stem}.dll").write_bytes(f"fixture assembly {relative}".encode())
    (tmp_path / TRX).parent.mkdir(parents=True, exist_ok=True)
    (tmp_path / TRX).write_text(trx_text(ALL_TESTS), encoding="utf-8", newline="\n")
    write_measured_command_inputs(tmp_path)
    done = generate(tmp_path)
    assert done.returncode == 0, done.stderr
    return tmp_path


def test_generator_is_deterministic_schema_valid_and_verifier_passes(repo: Path) -> None:
    before = snapshot(repo)
    assert generate(repo).returncode == 0
    assert snapshot(repo) == before  # JSON and Markdown are byte-identical across runs
    assert verify(repo, with_proof=True) == (0, [])
    assert verify(repo, inventory_only=True) == (0, [])
    bundle = json.loads((repo / JSON_OUT).read_text(encoding="utf-8"))
    assert list(bundle) == ["schemaVersion", "candidate", "guidanceDigest", "fixtureInventory", "resolvedPublicApis",
                            "restore", "build", "tests", "appHost", "digests"]
    assert bundle["fixtureInventory"]["sha256"] == verifier.INVENTORY_SHA256
    assert bundle["appHost"]["count"] == 1 and bundle["appHost"]["isPackable"] is False
    assert bundle["tests"]["failed"] == bundle["tests"]["skipped"] == bundle["tests"]["notRun"] == 0
    assert bundle["candidate"]["commit"] == verifier.git_head(repo)
    assert bundle["candidate"]["eventStoreGitlink"] == verifier.eventstore_gitlink(repo, verifier.git_head(repo))
    assert bundle["restore"]["sources"] == ["https://api.nuget.org/v3/index.json"]
    for section, scenario in (("restore", "AC-11.2-02"), ("build", "AC-11.2-03")):
        assert bundle[section]["receipt"]["sha256"] == verifier.sha256_bytes((repo / verifier.COMMAND_RECEIPTS[scenario]).read_bytes())
    assert (repo / MD_OUT).read_bytes().endswith(b"\n") and b"\r" not in (repo / MD_OUT).read_bytes()


@pytest.mark.parametrize("field", ["commit", "eventStoreGitlink"])
def test_proof_rejects_resigned_candidate_identity_drift(repo: Path, field: str) -> None:
    bundle = json.loads((repo / JSON_OUT).read_bytes())
    if field == "commit":
        bundle["candidate"]["commit"] = "2" * 40
    else:
        bundle["candidate"]["eventStoreGitlink"]["commit"] = "2" * 40
    json_text, markdown = proof.finalize(proof.signable_of(bundle))
    (repo / JSON_OUT).write_text(json_text, encoding="utf-8", newline="\n")
    (repo / MD_OUT).write_text(markdown, encoding="utf-8", newline="\n")
    errors = verifier.verify_proof(repo, FIXTURE, JSON_OUT, MD_OUT, TRX, rederive=False)
    assert errors and errors[0]["code"] == verifier.BLOCK_PROOF
    assert "candidate or EventStore gitlink differs" in errors[0]["message"]


@pytest.mark.parametrize("field", ["counts", "cases", "capabilities", "xunit", "restore-command", "build-command", "tests-command"])
def test_proof_without_assembly_rederivation_rejects_resigned_execution_facts(repo: Path, field: str) -> None:
    bundle = json.loads((repo / JSON_OUT).read_bytes())
    if field == "counts":
        bundle["tests"]["total"] += 1
        bundle["tests"]["passed"] += 1
    elif field == "cases":
        bundle["tests"]["cases"].append("InventedTest")
    elif field == "capabilities":
        bundle["tests"]["capabilities"].pop()
    elif field == "xunit":
        bundle["tests"]["toolVersions"]["xunitV3"] = "999.0.0"
    else:
        bundle[field.removesuffix("-command")]["command"] += " --unrecorded-option"
    json_text, markdown = proof.finalize(proof.signable_of(bundle))
    (repo / JSON_OUT).write_text(json_text, encoding="utf-8", newline="\n")
    (repo / MD_OUT).write_text(markdown, encoding="utf-8", newline="\n")
    errors = verifier.verify_proof(repo, FIXTURE, JSON_OUT, MD_OUT, TRX, rederive=False)
    assert errors and errors[0]["code"] == verifier.BLOCK_PROOF
    expected = "execution commands" if field.endswith("-command") else "test facts"
    assert expected in errors[0]["message"]


@pytest.mark.parametrize("variant", ["different-head", "dirty-source", "staged-source", "uninitialized"])
def test_platform_checkout_is_checked_before_measurement_and_proof_acceptance(repo: Path, monkeypatch, variant: str) -> None:
    platform = repo / proof.PLATFORM_ROOT
    if variant == "different-head":
        git(platform, "commit", "--quiet", "--allow-empty", "-m", "test: advance the platform checkout")
    elif variant == "uninitialized":
        shutil.rmtree(platform / ".git")
    else:
        (platform / "src/PlatformFixture.cs").write_text("// Changed platform source.\n", encoding="utf-8")
        if variant == "staged-source":
            git(platform, "add", "src/PlatformFixture.cs")
    errors = verifier.verify_proof(repo, FIXTURE, JSON_OUT, MD_OUT, TRX, rederive=False)
    assert errors and errors[0]["code"] == verifier.BLOCK_PROOF
    expected = {"different-head": "HEAD differs", "uninitialized": "not initialized"}.get(variant, "tracked source changes")
    assert expected in errors[0]["message"]
    result = generate(repo)
    assert result.returncode == 1 and verifier.BLOCK_PROOF in result.stderr and expected in result.stderr
    real_run = subprocess.run

    def no_dotnet(arguments, **kwargs):
        assert arguments[0] == "git", "measurement must reject the checkout before executing dotnet"
        return real_run(arguments, **kwargs)

    monkeypatch.setattr(command_runner.subprocess, "run", no_dotnet)
    with pytest.raises(ValueError, match=expected):
        command_runner.measure(repo, "AC-11.2-02")


@pytest.mark.parametrize("binding", ["lock-file", "guidance-markdown", "guidance-source", "platform-project"])
def test_proof_without_assembly_rederivation_checks_every_source_binding(repo: Path, binding: str) -> None:
    bundle = json.loads((repo / JSON_OUT).read_bytes())
    if binding == "lock-file":
        target = repo / bundle["restore"]["lockFiles"][0]["path"]
        target.write_bytes(target.read_bytes() + b"\n")
    elif binding == "platform-project":
        bundle["resolvedPublicApis"]["projects"][0]["sha256"] = "2" * 64
    else:
        field = "bundleMarkdownSha256" if binding == "guidance-markdown" else "sourceSha256"
        bundle["guidanceDigest"][field] = "2" * 64
    if binding != "lock-file":
        json_text, markdown = proof.finalize(proof.signable_of(bundle))
        (repo / JSON_OUT).write_text(json_text, encoding="utf-8", newline="\n")
        (repo / MD_OUT).write_text(markdown, encoding="utf-8", newline="\n")
    errors = verifier.verify_proof(repo, FIXTURE, JSON_OUT, MD_OUT, TRX, rederive=False)
    assert errors and errors[0]["code"] == verifier.BLOCK_PROOF
    assert "current fixture, result, or guidance" in errors[0]["message"]


@pytest.mark.parametrize("scenario", tuple(verifier.COMMAND_RECEIPTS))
def test_command_wrapper_executes_the_frozen_command_and_retains_failure(repo: Path, monkeypatch, scenario: str) -> None:
    calls = []
    real_run = subprocess.run

    def execute(arguments, **kwargs):
        if arguments[0] == "git":
            return real_run(arguments, **kwargs)
        calls.append(arguments)
        if "msbuild" in arguments:
            return subprocess.CompletedProcess(arguments, 0, "true\n", "")
        return subprocess.CompletedProcess(arguments, 17, "NuGet/build failed\n")

    monkeypatch.setattr(command_runner.subprocess, "run", execute)
    assert command_runner.main(["--repository", str(repo), "--scenario", scenario]) == 17
    receipt = json.loads((repo / verifier.COMMAND_RECEIPTS[scenario]).read_bytes())
    import shlex  # noqa: PLC0415

    assert calls[-1] == shlex.split(receipt["command"])
    assert receipt["exitCode"] == 17 and receipt["consoleLog"] == "NuGet/build failed\n"
    with pytest.raises(ValueError, match="measured receipt is invalid"):
        verifier.command_receipt(repo, scenario)


def test_committed_fixture_inventory_is_exact_and_source_controlled(tmp_path: Path) -> None:
    assert verifier.inventory_digest(verifier.FROZEN_PATHS) == verifier.INVENTORY_SHA256
    assert len(verifier.FROZEN_PATHS) == 13 and len(set(verifier.FROZEN_PATHS)) == 13
    blockers, facts = verifier.check_inventory(ROOT, FIXTURE)
    assert blockers == [], blockers
    assert [row["path"] for row in facts["paths"]] == list(verifier.FROZEN_PATHS)
    graph_blockers, graph = verifier.check_graph(ROOT, FIXTURE)
    assert graph_blockers == [], graph_blockers
    assert graph["appHost"]["count"] == 1 and graph["appHost"]["isPackable"] is False


# --- fault mutations -------------------------------------------------------------------------------------

def fx(repo: Path, relative: str) -> Path:
    return repo / FIXTURE / relative


def edit(repo: Path, relative: str, old: str, new: str) -> None:
    path = fx(repo, relative)
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, old
    path.write_text(text.replace(old, new), encoding="utf-8", newline="\n")


def append(repo: Path, relative: str, text: str) -> None:
    path = fx(repo, relative)
    path.write_text(path.read_text(encoding="utf-8") + text, encoding="utf-8", newline="\n")


def write(repo: Path, relative: str, text: str) -> None:
    path = fx(repo, relative)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


SERVER = "src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj"
MODULE = "src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs"
APPHOST = verifier.APPHOST_PROJECT
PROPS = "Directory.Build.props"
README = "README.md"


def add_server_item(repo: Path, item: str) -> None:
    edit(repo, SERVER, "</Project>", f"  <ItemGroup>\n    {item}\n  </ItemGroup>\n</Project>")


def trx(repo: Path, names: list[str], outcome: dict[str, str] | None = None) -> None:
    (repo / TRX).write_text(trx_text(names, outcome), encoding="utf-8", newline="\n")


def remove_apphost(repo: Path) -> None:
    fx(repo, APPHOST).unlink()
    fx(repo, "tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs").unlink()


def edit_proof_json(repo: Path) -> None:
    path = repo / JSON_OUT
    bundle = json.loads(path.read_text(encoding="utf-8"))
    bundle["tests"]["total"] += 1
    path.write_text(proof.serialize_json(bundle), encoding="utf-8", newline="\n")


def edit_proof_markdown(repo: Path) -> None:
    path = repo / MD_OUT
    path.write_text(path.read_text(encoding="utf-8").replace("# Thin-Module", "# Edited Thin-Module"),
                    encoding="utf-8", newline="\n")


def remove_output(repo: Path) -> None:
    (fx(repo, "src/Hexalith.Conversations.MinimalModule.Server") / proof.OUTPUT_DIRECTORY
     / "Hexalith.Conversations.MinimalModule.Server.dll").unlink()


# id -> (mutation, route). Routes: verify (static checker), inventory (--inventory-only), generate, proof.
FAULTS = {
    "inventory-path-added": (lambda r: write(r, "Extra.cs", "// extra\n"), "inventory"),
    "inventory-path-removed": (lambda r: fx(r, README).unlink(), "inventory"),
    "inventory-path-renamed": (lambda r: fx(r, README).rename(fx(r, "README.txt")), "inventory"),
    "inventory-crlf-edit": (lambda r: fx(r, README).write_bytes(fx(r, README).read_bytes().replace(b"\n", b"\r\n")), "inventory"),
    "inventory-lock-file-misplaced": (lambda r: write(r, "packages.lock.json", "{}\n"), "inventory"),
    "apphost-none": (remove_apphost, "verify"),
    "apphost-second": (lambda r: write(
        r, "tests/Second.AppHost/Second.AppHost.csproj",
        '<Project Sdk="Aspire.AppHost.Sdk/13.6.1">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n  </PropertyGroup>\n</Project>\n'), "verify"),
    "apphost-packable": (lambda r: edit(r, APPHOST, "<IsPackable>false</IsPackable>", "<IsPackable>true</IsPackable>"), "verify"),
    "apphost-publishable": (lambda r: edit(r, APPHOST, "<IsPublishable>false</IsPublishable>", "<IsPublishable>true</IsPublishable>"), "verify"),
    "reusable-aspire-library-added": (lambda r: write(
        r, "src/Hexalith.Conversations.MinimalModule.Aspire/Hexalith.Conversations.MinimalModule.Aspire.csproj",
        '<Project Sdk="Microsoft.NET.Sdk">\n</Project>\n'), "verify"),
    "service-defaults-project-added": (lambda r: write(
        r, "src/Hexalith.Conversations.MinimalModule.ServiceDefaults/Hexalith.Conversations.MinimalModule.ServiceDefaults.csproj",
        '<Project Sdk="Microsoft.NET.Sdk">\n</Project>\n'), "verify"),
    "service-defaults-reference-added": (lambda r: add_server_item(
        r, '<ProjectReference Include="$(FixtureEventStoreRoot)/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj" />'), "verify"),
    "platform-runtime-project-reference-added": (lambda r: add_server_item(
        r, '<ProjectReference Include="$(FixtureEventStoreRoot)/src/Hexalith.EventStore.Server/Hexalith.EventStore.Server.csproj" />'), "verify"),
    "dapr-package-added": (lambda r: add_server_item(r, '<PackageReference Include="Dapr.Client" Version="1.18.10" />'), "verify"),
    "module-authored-endpoint": (lambda r: append(r, MODULE, "\ninternal static class Endpoint { static void Map(Microsoft.AspNetCore.Builder.WebApplication app) => app.MapPost(\"/x\", () => 1); }\n"), "verify"),
    "internals-visible-to-item": (lambda r: add_server_item(r, '<InternalsVisibleTo Include="Other" />'), "verify"),
    "internals-visible-to-attribute": (lambda r: append(r, MODULE, "\n[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Other\")]\n"), "verify"),
    "nonpublic-reflection": (lambda r: append(r, MODULE, "\ninternal static class Reach { static object? Get(System.Type t) => t.GetMethod(\"X\", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); }\n"), "verify"),
    "lock-file-missing": (lambda r: fx(r, "src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json").unlink(), "verify"),
    "lock-file-policy-disabled": (lambda r: edit(r, PROPS, "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>",
                                                 "<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>"), "verify"),
    "warnings-as-errors-disabled": (lambda r: edit(r, PROPS, "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",
                                                   "<TreatWarningsAsErrors>false</TreatWarningsAsErrors>"), "verify"),
    "build-output-missing": (remove_output, "generate"),
    "tests-zero": (lambda r: trx(r, []), "generate"),
    "tests-skipped": (lambda r: trx(r, ALL_TESTS, {ALL_TESTS[0]: "NotExecuted"}), "generate"),
    "tests-failed": (lambda r: trx(r, ALL_TESTS, {ALL_TESTS[0]: "Failed"}), "generate"),
    "capability-test-missing": (lambda r: trx(r, [name for name in ALL_TESTS if not name.startswith("PublishedEvent")]), "generate"),
    "proof-json-edited": (edit_proof_json, "proof"),
    "proof-markdown-edited": (edit_proof_markdown, "proof"),
    "result-edited": (lambda r: (r / TRX).write_text(trx_text(ALL_TESTS) + "<!-- edited -->\n", encoding="utf-8", newline="\n"), "proof"),
    "fixture-edited-after-proof": (lambda r: append(r, MODULE, "\n// edited after the proof was generated\n"), "proof"),
}


def run_route(repo: Path, route: str) -> tuple[int, list[str]]:
    if route == "generate":
        generated = generate(repo)
        return generated.returncode, [line.split(":")[0] for line in generated.stderr.strip().splitlines()]
    if route == "proof":
        return verify(repo, with_proof=True)
    return verify(repo, inventory_only=route == "inventory")


@pytest.mark.parametrize("fault", sorted(FAULTS))
def test_checkpoint_b_fault_matrix(repo: Path, fault: str) -> None:
    mutate, route = FAULTS[fault]
    expected = verifier.FAULT_EXPECTATIONS[fault]
    pristine = snapshot(repo)
    saved = contents(repo)
    mutate(repo)
    assert snapshot(repo) != pristine, "FAULT_NOT_DETECTED: the mutation did not change the fixture"
    exit_code, blockers = run_route(repo, route)
    assert exit_code == 1, f"FAULT_NOT_DETECTED: exit {exit_code} {blockers}"
    assert expected in blockers, f"FAULT_NOT_DETECTED: expected {expected}, got {blockers}"
    assert set(blockers) <= set(verifier.CHECKER_BLOCKERS), blockers
    restore(repo, saved)
    assert snapshot(repo) == pristine, "FIXTURE_NOT_RESTORED"
    assert verify(repo, with_proof=True) == (0, [])


def test_checkpoint_b_fault_matrix_inventory_matches_checker() -> None:
    assert sorted(FAULTS) == sorted(verifier.FAULT_EXPECTATIONS)
    assert set(verifier.FAULT_EXPECTATIONS.values()) == set(verifier.CHECKER_BLOCKERS)
    assert len(verifier.ALL_BLOCKERS) == 10 and len(set(verifier.ALL_BLOCKERS)) == 10


def test_blocker_codes_are_exactly_the_contract_codes() -> None:
    contract = json.loads((ROOT / CONTRACT).read_text(encoding="utf-8"))
    cited = set()
    for scenario in contract["scenarios"]:
        text = scenario["contract"]
        cited.update(token for token in verifier.ALL_BLOCKERS if token in text)
    assert cited == set(verifier.ALL_BLOCKERS)
