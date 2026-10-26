"""Current v2 preparation: portable exact-preservation and safe-read faults."""
from __future__ import annotations

import errno
import gzip
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys

import jsonschema
import pytest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "_bmad/scripts"))
import verify_epic_16_adoption_v2 as checker  # noqa: E402
import contained_reader as secure  # noqa: E402
from evidence_boundary_current import digest  # noqa: E402
from generate_story_record import run_git  # noqa: E402

PACKAGE = f"{checker.BASE}/package.json"
STORY = f"{checker.BASE}/story-contracts/16.1.json"
INVENTORY = f"{checker.BASE}/inventories.json"
GRAPH = f"{checker.BASE}/execution-graph.json"


def edit(repository: Path, path: str, mutation) -> None:
    target = repository / path
    document = json.loads(target.read_text())
    mutation(document)
    target.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")


def rehash(repository: Path) -> None:
    """An attacker may rewrite every declared digest; independent pins still apply."""
    edit(repository, PACKAGE, lambda doc: doc.update(files=[
        dict(path=path, sha256=digest((repository / path).read_bytes()))
        for path in checker.ARTIFACTS]))


def populate_repository(destination: Path) -> Path:
    """Populate current fixed inputs without history or live reference reads."""
    snapshot = json.loads((ROOT / "_bmad/scripts/tests/fixtures/epic16-current-reference-inputs-v2.json").read_text())
    for relative in {*checker.SOURCE_PINS, *checker.ARTIFACTS, PACKAGE}:
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if relative.startswith("references/"):
            row = snapshot["files"][relative]
            content = row["utf8"].encode("utf-8")
            assert digest(content) == row["sha256"] == checker.SOURCE_PINS[relative]
            target.write_bytes(content)
        elif relative == ".gitattributes":
            content = (ROOT / "_bmad/scripts/tests/fixtures/epic16-adoption-v2-gitattributes.txt").read_bytes()
            assert digest(content) == checker.ARTIFACT_PINS[relative]
            target.write_bytes(content)
        elif relative == "_bmad/scripts/generate_story_record.py":
            content = gzip.decompress((ROOT / "_bmad/scripts/tests/fixtures/epic16-adoption-v2-generator.py.gz").read_bytes())
            assert digest(content) == checker.SOURCE_PINS[relative]
            target.write_bytes(content)
        else:
            shutil.copyfile(ROOT / relative, target)
    return destination


@pytest.fixture()
def repository(tmp_path: Path) -> Path:
    return populate_repository(tmp_path)


def no_authority(report: dict) -> None:
    # Independent literals: implementation constants cannot redefine expected authority.
    assert report["authority"] == {"implementationHold": "ACTIVE", "executionAllowed": False,
        "ownerApprovalClaimed": False, "releaseAuthorized": False, "pushAuthorized": False}
    for flag in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"):
        assert report["authority"][flag] is False


def rejected(report: dict, code: str | None = None) -> None:
    assert report["result"] == "FAIL", report
    assert report["assertionLedger"]
    assert report["entryReadiness"] == "BLOCKED"
    no_authority(report)
    if code:
        assert code in report["blockers"], report


def test_valid_package_passes_structure_but_reports_actual_entry_failures_without_execution(repository: Path, monkeypatch) -> None:
    before = {str(path): path.read_bytes() for path in repository.rglob("*") if path.is_file()}
    def no_command(*args, **kwargs):
        pytest.fail("review verifier attempted command execution")
    monkeypatch.setattr(subprocess, "run", no_command)
    monkeypatch.setattr(subprocess, "Popen", no_command)
    monkeypatch.setattr(os, "system", no_command)
    report = checker.verify(repository)
    assert report["result"] == "PASS", report
    assert report["validationScope"] == "structural-consistency-only"
    assert len(report["assertionLedger"]) >= 80
    assert report["preservedObligations"] == dict(scenarios=18, requirements=32, graphNodes=38, graphEdges=61, finalRecordSummary="6/6/0/0/0/0")
    package = json.loads((repository / PACKAGE).read_text())
    assert package["capabilities"]["supportedGeneratorSchemas"] == [
        checker.V2_CONTRACT_SCHEMA_VERSION, checker.V2_V14_CONTRACT_SCHEMA_VERSION]
    assert not report["blockers"]
    assert report["entryReadiness"] == "BLOCKED"
    no_authority(report)
    observed = report["observedStaticValidation"]
    assert (observed["passingRows"], observed["failingRows"]) == (6, 18)
    assert observed["ordinaryManifestExitCode"] == 1 and observed["adoptionExitCode"] == 1
    assert observed["ordinaryManifestResult"] == "FAIL" and observed["adoptionTextExitCode"] == 1
    assert observed["currentSourceManifestSchemaResult"] == "PASS"
    assert observed["publishedToolTokenCompatibility"] == "BLOCKED"
    assert observed["trackedSourceOnly"] and not observed["additiveFilesTrackedAtObservation"]
    assert not observed["runtimeExecuted"] and not observed["sm2Verified"]
    entry_codes = {row["code"] for row in report["entryBlockers"]}
    assert {"MODULE_ADOPTION_FAILED", "GENERATOR_UNSUPPORTED", "SCENARIO_VERIFIERS_UNAVAILABLE",
            "QUALIFICATION_PENDING", "SM2_UNVERIFIED", "OWNER_ENTRY_DECISION_PENDING",
            "TENANTS_OPTIONS_BINDING_PENDING", "DATA_PROTECTION_REGISTRATION_PENDING",
            "DECLARATIVE_SUBSCRIPTIONS_PENDING", "ACCEPTED_TOOL_TOKEN_FORMAT_BLOCKED"} <= entry_codes
    assert len([r for r in report["entryBlockers"] if r["state"] == "FAIL"]) == 18
    schema_blockers = [r for r in report["entryBlockers"] if r["code"] == "ACCEPTED_TOOL_TOKEN_FORMAT_BLOCKED"]
    assert len(schema_blockers) == 2 and all("HXP002" in r["detail"] for r in schema_blockers)
    source_failures = [r for r in report["entryBlockers"] if r["code"] == "MODULE_ADOPTION_FAILED"
                       and ("dependency-selectors" in r["detail"] or "dapr-subscriptions" in r["detail"])]
    assert len(source_failures) == 17 and all("HXA001" in r["detail"] for r in source_failures)
    assert report["evidenceBoundary"]["status"] == "not-applicable"
    assert before == {str(path): path.read_bytes() for path in repository.rglob("*") if path.is_file()}


@pytest.mark.parametrize("story", checker.historic.prerequisite.STORIES)
@pytest.mark.parametrize("mutation", [
    lambda d: d["scenarios"].pop(),
    lambda d: d["scenarios"][0].update(id=d["scenarios"][1]["id"]),
    lambda d: d["scenarios"][0].update(given="Given no durable state"),
    lambda d: d["scenarios"][0].update(when="When tests do not run"),
    lambda d: d["scenarios"][0].update(then="Then any state authorizes access"),
    lambda d: d["scenarios"][0]["requirementIds"].pop(),
    lambda d: d["scenarios"][0]["requirementIds"].append(d["scenarios"][0]["requirementIds"][0]),
    lambda d: d["scenarios"][0].update(legacyCommand="touch OWNED"),
    lambda d: d["predecessors"].append("16.3"),
    lambda d: d["outcome"].update(bounded="Allow weakened behavior"),
    lambda d: d["rollback"].update(boundary="Delete all event history"),
    lambda d: d["finalRecord"]["paths"].reverse(),
    lambda d: d["adoption"].update(generatorCompatible=True),
])
def test_all_inherited_story_assertions_survive_rehashed_weakening(repository: Path, story: str, mutation) -> None:
    edit(repository, f"{checker.BASE}/story-contracts/{story}.json", lambda d: mutation(d["preservedContract"]))
    rehash(repository)
    rejected(checker.verify(repository))
    assert not (repository / "OWNED").exists()


@pytest.mark.parametrize("mutation", [
    lambda d: d["futureCommands"].pop(),
    lambda d: d["futureCommands"].append(d["futureCommands"][0]),
    lambda d: d["futureCommands"][0].update(command="touch OWNED"),
    lambda d: d["futureCommands"][0].update(scenarioId="AC-16.1-02"),
    lambda d: d["futureCommands"][0].update(outputs=["artifacts/forged.json"]),
    lambda d: d["futureCommands"][5].update(command="python3 _bmad/scripts/generate_story_record.py --contract old.json"),
    lambda d: d["futureCommands"][0].update(executed=True),
])
def test_future_commands_are_exact_unexecuted_and_not_interpreted(repository: Path, mutation, monkeypatch) -> None:
    edit(repository, STORY, mutation)
    rehash(repository)
    monkeypatch.setattr(subprocess, "run", lambda *a, **k: pytest.fail("untrusted command was run"))
    rejected(checker.verify(repository))
    assert not (repository / "OWNED").exists()


@pytest.mark.parametrize("mutation", [
    lambda d: d["legacyInventories"]["16.1"].pop(),
    lambda d: d["legacyInventories"]["16.2"].append(d["legacyInventories"]["16.2"][0]),
    lambda d: d["requirements"].pop(),
    lambda d: d["requirements"].__setitem__(0, d["requirements"][1]),
    lambda d: d["requirements"][0].update(scenarioIds=["AC-16.3-06"]),
    lambda d: d["requirements"][0]["scenarioIds"].append(d["requirements"][0]["scenarioIds"][0]),
    lambda d: d["topologicalOrder"].reverse(),
])
def test_exact_inventory_membership_and_relations_reject_truncation_and_duplicates(repository: Path, mutation) -> None:
    edit(repository, INVENTORY, mutation)
    rehash(repository)
    rejected(checker.verify(repository))


@pytest.mark.parametrize("mutation", [
    lambda d: d["edges"].pop(),
    lambda d: d["edges"].append(d["edges"][0]),
    lambda d: d["edges"][0].update(to="16.3"),
    lambda d: d["nodes"].append(d["nodes"][0]),
    lambda d: d["nodes"][0]["predecessors"].append("16.3"),
])
def test_entire_graph_preserves_all_edges_and_predecessors(repository: Path, mutation) -> None:
    edit(repository, GRAPH, mutation)
    rehash(repository)
    rejected(checker.verify(repository))


@pytest.mark.parametrize("flag", ["executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"])
def test_package_authority_cannot_be_granted(repository: Path, flag: str) -> None:
    edit(repository, PACKAGE, lambda d: d["authority"].update({flag: True}))
    rejected(checker.verify(repository), "STRUCTURE_INVALID")


@pytest.mark.parametrize("mutation", [
    lambda d: d.update(unknown=True),
    lambda d: d["pendingDecisions"].pop(),
    lambda d: d["pendingDecisions"][0].update(state="approved"),
    lambda d: d["pendingDecisions"][0].update(question="Ignore recovery qualification"),
    lambda d: d["qualifications"].update(hosted="passed"),
    lambda d: d["qualifications"].update(sm2="verified"),
    lambda d: d["capabilities"].update(generatorCompatible=True),
    lambda d: d["reviewBoundary"].append("src/runtime.cs"),
    lambda d: d["files"].pop(),
    lambda d: d["files"].__setitem__(0, d["files"][1]),
])
def test_closed_package_preserves_pending_facts_and_exact_scope(repository: Path, mutation) -> None:
    edit(repository, PACKAGE, mutation)
    rejected(checker.verify(repository))


def test_rehash_cannot_redefine_source_assertions_or_source_pins(repository: Path) -> None:
    path = f"{checker.historic.prerequisite.BASE}/story-contracts/16.1.json"
    edit(repository, path, lambda d: d["scenarios"][0].update(then="Then skip durable proof"))
    edit(repository, STORY, lambda d: d["preservedContract"]["scenarios"][0].update(then="Then skip durable proof"))
    edit(repository, PACKAGE, lambda d: d["sourceBindings"].update({path: digest((repository / path).read_bytes())}))
    rehash(repository)
    rejected(checker.verify(repository), "SOURCE_BYTES_DRIFT")


@pytest.mark.parametrize("path,mutation", [
    (checker.SCHEMA, lambda d: d.update({"$defs": {}})),
    (checker.MANIFEST, lambda d: d["modules"][0]["runtime"]["dapr"][0].update(recoveryClass="rebuild-only")),
    ("module/hosting-inventory.json", lambda d: d["projects"].pop()),
    ("module/hosting-inventory.json", lambda d: d["projects"][0].update(state="retired")),
    (f"{checker.NATIVE}/adoption.json", lambda d: d.update(status="success")),
    (checker.OBSERVATION, lambda d: d.update(sm2Verified=True)),
    (checker.TOOL, lambda d: d["tools"]["hexalith.builds.module.cli"].update(version="99.0.0")),
])
def test_rehashed_enrollment_schema_and_static_observation_cannot_be_weakened(repository: Path, path: str, mutation) -> None:
    edit(repository, path, mutation)
    rehash(repository)
    rejected(checker.verify(repository))


@pytest.mark.parametrize("path,code", [
    (PACKAGE, "INPUT_UNAVAILABLE"),
    (checker.SCHEMA, "INPUT_UNAVAILABLE"),
    (f"{checker.historic.prerequisite.BASE}/story-contracts/16.1.json", "INPUT_UNAVAILABLE"),
    (checker.TOOL, "ACCEPTED_TOOL_UNAVAILABLE"),
    (f"{checker.NATIVE}/version.txt", "ACCEPTED_TOOL_UNAVAILABLE"),
])
def test_missing_prerequisite_or_accepted_tool_is_blocked_with_exact_diagnostic(repository: Path, path: str, code: str) -> None:
    (repository / path).unlink()
    report = checker.verify(repository)
    assert report["result"] == "BLOCKED", report
    assert report["blockers"] == [code]
    assert path in report["diagnostics"][-1]["detail"]
    assert report["entryReadiness"] == "BLOCKED"
    no_authority(report)


@pytest.mark.parametrize("bad_path", ["/tmp/outside", "../../outside", "a/../outside", ".git/config", "a\\outside", "a//b"])
def test_declared_escape_paths_are_rejected_without_opening_them(repository: Path, bad_path: str, monkeypatch) -> None:
    edit(repository, PACKAGE, lambda d: d["files"][0].update(path=bad_path))
    original = Path.open
    def confined_open(path, *args, **kwargs):
        assert path.resolve().is_relative_to(repository), f"outside open attempted: {path}"
        return original(path, *args, **kwargs)
    monkeypatch.setattr(Path, "open", confined_open)
    rejected(checker.verify(repository), "PATH_INVALID")


@pytest.mark.parametrize("target", [PACKAGE, checker.SCHEMA, "module/hosting-inventory.json", checker.TOOL])
def test_fixed_input_symlink_escape_rejected_before_outside_read(repository: Path, tmp_path_factory, monkeypatch, target: str) -> None:
    outside = tmp_path_factory.mktemp("outside") / "unreadable-input"
    outside.write_bytes((repository / target).read_bytes())
    (repository / target).unlink()
    (repository / target).symlink_to(outside)
    original = Path.open
    def confined_open(path, *args, **kwargs):
        assert path.resolve().is_relative_to(repository), f"outside open attempted: {path}"
        return original(path, *args, **kwargs)
    monkeypatch.setattr(Path, "open", confined_open)
    rejected(checker.verify(repository), "PATH_ESCAPE")


@pytest.mark.parametrize("content", [
    b'{"status":"candidate-for-owner-review","status":"candidate-for-owner-review"}',
    b'{"value": NaN}',
    b'{"value": 1e999}',
    b'{"value": -1e999}',
    b'not-json',
    b'\xff',
])
def test_malformed_json_and_duplicate_keys_fail_instead_of_blocking(repository: Path, content: bytes) -> None:
    (repository / PACKAGE).write_bytes(content)
    rejected(checker.verify(repository), "JSON_INVALID")


def test_cli_missing_inputs_returns_blocked_and_has_no_output_writer(tmp_path: Path) -> None:
    command = [sys.executable, str(ROOT / "_bmad/scripts/verify_epic_16_adoption_v2.py"), "--repository", str(tmp_path)]
    process = subprocess.run(command, capture_output=True, text=True, check=False)
    report = json.loads(process.stdout)
    assert process.returncode == 2 and report["result"] == "BLOCKED"
    assert report["assertionLedger"]
    no_authority(report)
    target = tmp_path / "MUST-NOT-EXIST"
    forbidden = subprocess.run([*command, "--output", str(target)], capture_output=True, text=True, check=False)
    assert forbidden.returncode == 2 and not target.exists()


def test_capability_claim_is_checked_against_actual_generator_constants(repository: Path, monkeypatch) -> None:
    monkeypatch.setattr(checker, "V2_CONTRACT_SCHEMA_VERSION", "unsupported-schema")
    rejected(checker.verify(repository), "GENERATOR_CAPABILITY_DRIFT")


def test_real_cli_structural_failure_has_exit_one_and_literal_no_authority(repository: Path) -> None:
    (repository / PACKAGE).write_bytes(b'{"broken":')
    process = subprocess.run([sys.executable, str(ROOT / "_bmad/scripts/verify_epic_16_adoption_v2.py"),
                              "--repository", str(repository)], capture_output=True, text=True, check=False)
    assert process.returncode == 1, process.stdout + process.stderr
    rejected(json.loads(process.stdout), "JSON_INVALID")


def test_fixture_and_verifier_need_no_live_reference_content(tmp_path: Path, monkeypatch) -> None:
    original = Path.open
    def no_live_reference_open(path, *args, **kwargs):
        assert not path.is_relative_to(ROOT / "references"), f"live reference opened: {path}"
        return original(path, *args, **kwargs)
    monkeypatch.setattr(Path, "open", no_live_reference_open)
    report = checker.verify(populate_repository(tmp_path))
    assert report["result"] == "PASS", report
    no_authority(report)


@pytest.mark.parametrize("path", [PACKAGE, STORY, INVENTORY, GRAPH, checker.OBSERVATION])
def test_published_schema_root_accepts_each_entry_point(repository: Path, path: str) -> None:
    schema = json.loads((repository / checker.SCHEMA).read_text())
    jsonschema.Draft202012Validator(schema).validate(json.loads((repository / path).read_text()))


@pytest.mark.parametrize("document", [{}, {"arbitrary": "document"}, [], True, 42, None])
def test_published_schema_root_rejects_arbitrary_documents(repository: Path, document) -> None:
    schema = json.loads((repository / checker.SCHEMA).read_text())
    with pytest.raises(jsonschema.ValidationError):
        jsonschema.Draft202012Validator(schema).validate(document)


@pytest.mark.parametrize("content", [b'1e999', b'-1e999', b'{"nested":[1e999]}'])
def test_json_exponent_overflow_is_rejected_directly(content: bytes) -> None:
    with pytest.raises(checker.InputProblem, match="non-finite JSON number"):
        checker.parse(content, "overflow.json")
    assert checker.parse(b'1e300', "finite.json") == 1e300


def test_declared_channel_secret_keys_match_the_actual_sdk_constant(repository: Path) -> None:
    source = "references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/DaprAppChannelToken.cs"
    sdk = (repository / source).read_text()
    actual_key = re.search(r'public const string ConfigurationKey\s*=\s*"([^"]+)"', sdk).group(1)
    assert actual_key == "APP_API_TOKEN"
    module = json.loads((repository / checker.MANIFEST).read_text())["modules"][0]
    inputs = module["runtime"]["extensions"][0]["configurationInputs"]
    assert [row["key"] for row in inputs if row["reference"] == "app-channel-token"] == [actual_key]
    assert [row["configurationKey"] for row in module["integration"]["secrets"]
            if row["name"] == "app-channel-token"] == [actual_key]
    assert not {"Tenants:Enabled", "Tenants:ServiceName", "Tenants:CommandApiAppId"} & {r["key"] for r in inputs}
    assert any(r["key"] == "Dapr:DeclarativeSubscriptions" and
               r["reference"] == "dapr-declarative-subscriptions-true" for r in inputs)
    assert {r["name"] for r in module["integration"]["egress"]} >= {"workload-oidc-metadata", "workload-oidc-jwks"}
    sdk_routes = (repository / "references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs").read_text()
    assert 'GetValue<bool>("Dapr:DeclarativeSubscriptions")' in sdk_routes


@pytest.mark.parametrize("field", ["input", "secret"])
def test_rehashed_wrong_sdk_token_key_is_rejected(repository: Path, field: str) -> None:
    def wrong_key(document):
        module = document["modules"][0]
        rows = (module["runtime"]["extensions"][0]["configurationInputs"] if field == "input"
                else module["integration"]["secrets"])
        key = "key" if field == "input" else "configurationKey"
        for row in rows:
            if row.get("reference", row.get("name")) == "app-channel-token":
                row[key] = "EventStore:AppChannelToken"
    edit(repository, checker.MANIFEST, wrong_key)
    rehash(repository)
    rejected(checker.verify(repository), "SOURCE_BYTES_DRIFT")


@pytest.mark.parametrize("kind", ["directory", "fifo"])
def test_present_nonregular_input_is_fail(repository: Path, kind: str) -> None:
    target = repository / PACKAGE
    target.unlink()
    target.mkdir() if kind == "directory" else os.mkfifo(target)
    rejected(checker.verify(repository), "INPUT_NOT_REGULAR")


def test_unavailable_secure_open_environment_blocks_without_fallback(repository: Path, monkeypatch) -> None:
    monkeypatch.setattr(secure, "secure_open_supported", lambda: False)
    monkeypatch.setattr(Path, "open", lambda *a, **k: pytest.fail("unsafe fallback attempted"))
    report = checker.verify(repository)
    assert report["result"] == "BLOCKED" and report["blockers"] == ["SECURE_OPEN_UNAVAILABLE"]
    no_authority(report)


def test_unreadable_input_is_blocked_with_its_path(repository: Path, monkeypatch) -> None:
    original = os.open
    target_parent = (repository / PACKAGE).parent.stat().st_ino
    def denied(name, flags, *, dir_fd=None):
        if name == "package.json" and dir_fd is not None and os.fstat(dir_fd).st_ino == target_parent:
            raise PermissionError(errno.EACCES, "permission denied", name)
        return original(name, flags, dir_fd=dir_fd)
    monkeypatch.setattr(os, "supports_dir_fd", {*os.supports_dir_fd, denied})
    monkeypatch.setattr(os, "open", denied)
    report = checker.verify(repository)
    assert report["result"] == "BLOCKED" and report["blockers"] == ["INPUT_UNAVAILABLE"]
    assert PACKAGE in report["diagnostics"][-1]["detail"]
    no_authority(report)


@pytest.mark.parametrize("replace_target", ["ancestor", "leaf"])
@pytest.mark.parametrize("timing", ["before-open", "after-open"])
def test_descriptor_reads_refuse_ancestor_or_leaf_replacement(tmp_path: Path, monkeypatch,
                                                           replace_target: str, timing: str) -> None:
    repository, outside = tmp_path / "repository", tmp_path / "outside"
    (repository / "safe").mkdir(parents=True)
    outside.mkdir()
    (repository / "safe/leaf").write_bytes(b"INSIDE")
    (outside / "leaf").write_bytes(b"OUTSIDE")
    attacked = False
    opened = os.open
    read = os.read
    actual_reads = []
    def attack():
        nonlocal attacked
        attacked = True
        target = repository / ("safe" if replace_target == "ancestor" else "safe/leaf")
        target.rename(target.with_name(target.name + "-original"))
        target.symlink_to(outside if replace_target == "ancestor" else outside / "leaf")
    def racing_open(name, flags, *, dir_fd=None):
        assert flags & os.O_NOFOLLOW, "descriptor traversal must never follow symlinks"
        if name == "safe":
            assert flags & os.O_DIRECTORY
        if not attacked and name == ("safe" if replace_target == "ancestor" else "leaf"):
            if timing == "before-open":
                attack()
                return opened(name, flags, dir_fd=dir_fd)
            descriptor = opened(name, flags, dir_fd=dir_fd)
            attack()
            return descriptor
        return opened(name, flags, dir_fd=dir_fd)
    def monitored_read(descriptor, count):
        value = read(descriptor, count)
        actual_reads.append(value)
        assert b"OUTSIDE" not in value, "external content was opened"
        return value
    # The wrapper retains the real descriptor-relative os.open implementation.
    monkeypatch.setattr(os, "supports_dir_fd", {*os.supports_dir_fd, racing_open})
    monkeypatch.setattr(os, "open", racing_open)
    monkeypatch.setattr(os, "read", monitored_read)
    with secure.ContainedReader(repository) as reader:
        with pytest.raises(secure.ReadError) as caught:
            reader.read("safe/leaf")
    assert attacked
    assert caught.value.code in {"PATH_ESCAPE", "INPUT_CHANGED"}
    assert not any(b"OUTSIDE" in value for value in actual_reads)


def test_bounded_reader_rejects_oversize_without_reading_content(tmp_path: Path, monkeypatch) -> None:
    (tmp_path / "large").write_bytes(b"12345")
    monkeypatch.setattr(os, "read", lambda *a, **k: pytest.fail("oversize content opened"))
    with secure.ContainedReader(tmp_path, limit=4) as reader:
        with pytest.raises(secure.ReadError) as caught:
            reader.read("large")
    assert caught.value.code == "INPUT_TOO_LARGE"


@pytest.mark.parametrize("flag", ["executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"])
@pytest.mark.parametrize("value", [0, "false", None])
def test_authority_requires_literal_false_not_false_like_values(repository: Path, flag: str, value) -> None:
    edit(repository, PACKAGE, lambda d: d["authority"].update({flag: value}))
    rejected(checker.verify(repository), "STRUCTURE_INVALID")


@pytest.mark.parametrize("path,mutation", [
    (checker.OBSERVATION, lambda d: d["commands"][2].update(exitCode=0)),
    (checker.OBSERVATION, lambda d: d["commands"][3].update(command="touch OWNED")),
    (checker.OBSERVATION, lambda d: d["commands"].reverse()),
    (checker.OBSERVATION, lambda d: d["commands"].pop()),
    (checker.OBSERVATION, lambda d: d.update(restoredVersion="4.35.2+invented")),
    (f"{checker.NATIVE}/provenance.json", lambda d: d.update(workingTreeStatus=[])),
    (f"{checker.NATIVE}/provenance.json", lambda d: d["rootGitlinks"].pop()),
    (f"{checker.NATIVE}/adoption.json", lambda d: d["adoption"].pop()),
    (f"{checker.NATIVE}/adoption.json", lambda d: d["adoption"][1].update(passed=True)),
    (f"{checker.NATIVE}/adoption.json", lambda d: d.update(diagnostics=[])),
])
def test_rehash_cannot_rewrite_commands_native_failures_or_observation_provenance(repository: Path, path: str, mutation) -> None:
    edit(repository, path, mutation)
    rehash(repository)
    rejected(checker.verify(repository), "ENROLLMENT_OR_REVIEW_DRIFT")
    assert not (repository / "OWNED").exists()


def test_current_schema_acceptance_does_not_erase_published_tool_rejection(repository: Path) -> None:
    manifest = json.loads((repository / checker.MANIFEST).read_bytes())
    schema = json.loads((repository / checker.MANIFEST_SCHEMA).read_bytes())
    jsonschema.Draft202012Validator(schema).validate(manifest)
    report = checker.verify(repository)
    assert report["result"] == "PASS", report
    assert report["observedStaticValidation"]["currentSourceManifestSchemaResult"] == "PASS"
    assert report["observedStaticValidation"]["ordinaryManifestResult"] == "FAIL"
    blockers = [row for row in report["entryBlockers"] if row["code"] == "ACCEPTED_TOOL_TOKEN_FORMAT_BLOCKED"]
    assert len(blockers) == 2 and all(row["state"] == "BLOCKED" for row in blockers)
    assert all("published accepted 4.35.2" in row["detail"] for row in blockers)


def test_rehash_cannot_redefine_current_schema_semantics(repository: Path) -> None:
    edit(repository, checker.MANIFEST_SCHEMA, lambda d: d["$defs"]["configurationKey"].update(pattern=".*"))
    edit(repository, PACKAGE, lambda d: d["sourceBindings"].update({
        checker.MANIFEST_SCHEMA: digest((repository / checker.MANIFEST_SCHEMA).read_bytes())}))
    rehash(repository)
    rejected(checker.verify(repository), "SOURCE_BYTES_DRIFT")


def test_predecessor_and_all_carrier_fields_are_preserved(repository: Path) -> None:
    package = json.loads((repository / PACKAGE).read_bytes())
    old_package = f"{checker.historic.BASE}/package.json"
    assert package["predecessor"] == dict(path=old_package, sha256=digest((repository / old_package).read_bytes()))
    assert package["pendingDecisions"] == json.loads((repository / old_package).read_bytes())["pendingDecisions"]
    assert PACKAGE not in checker.ARTIFACTS
    assert not any("/implementation-artifacts/spec-" in path for path in checker.ARTIFACTS)
    for story in ("16.1", "16.2", "16.3"):
        before = json.loads((repository / checker.historic.BASE / "story-contracts" / f"{story}.json").read_bytes())
        after = json.loads((repository / checker.BASE / "story-contracts" / f"{story}.json").read_bytes())
        assert after["preservedContract"] == before["preservedContract"]
        assert after["source"] == before["source"]
        assert after == dict(before, schemaVersion="hexalith.conversations.epic16-adoption-candidate.v2",
                             futureCommands=checker.expected_commands(story, before["preservedContract"]))


def test_real_v1_cli_keeps_reporting_current_source_drift(repository: Path) -> None:
    process = subprocess.run([sys.executable, str(ROOT / "_bmad/scripts/verify_epic_16_adoption.py"),
                              "--repository", str(repository)], capture_output=True, text=True, check=False)
    report = json.loads(process.stdout)
    assert process.returncode == 1
    rejected(report, "SOURCE_BYTES_DRIFT")
    assert "SOURCE_BINDING_INVALID" in report["blockers"]


def test_real_v2_cli_success_has_exit_zero_nonempty_ledger_and_no_authority(repository: Path) -> None:
    process = subprocess.run([sys.executable, str(ROOT / "_bmad/scripts/verify_epic_16_adoption_v2.py"),
                              "--repository", str(repository)], capture_output=True, text=True, check=False)
    report = json.loads(process.stdout)
    assert process.returncode == 0 and report["result"] == "PASS", process.stdout + process.stderr
    assert len(report["assertionLedger"]) >= 100 and report["entryReadiness"] == "BLOCKED"
    assert report["evidenceBoundary"]["status"] == "not-applicable"
    no_authority(report)


def test_real_cli_surrogate_path_emits_structured_failure(repository: Path) -> None:
    edit(repository, PACKAGE, lambda d: d["files"][0].update(path="../\ud800"))
    process = subprocess.run([sys.executable, str(ROOT / "_bmad/scripts/verify_epic_16_adoption_v2.py"),
                              "--repository", str(repository)], capture_output=True, check=False)
    assert process.returncode == 1, process.stderr
    assert b"\\ud800" in process.stdout and not process.stderr
    rejected(json.loads(process.stdout), "PATH_INVALID")


def test_rehashed_checkout_policy_cannot_redefine_pinned_bytes(repository: Path) -> None:
    (repository / ".gitattributes").write_bytes(b"* text eol=crlf\n")
    rehash(repository)
    rejected(checker.verify(repository), "ENROLLMENT_OR_REVIEW_DRIFT")


def test_disposable_git_checkout_preserves_bound_root_bytes_with_autocrlf(repository: Path, tmp_path_factory) -> None:
    """Exercise Git's clean/smudge round trip without staging the real workspace."""
    paths = sorted(path for path in {*checker.SOURCE_PINS, *checker.ARTIFACTS, PACKAGE}
                   if not path.startswith("references/"))
    before = {path: (repository / path).read_bytes() for path in paths}
    assert b"\r\n" in before["src/Hexalith.Conversations.Server/Program.cs"]
    assert b"\r" not in before[checker.SCHEMA]
    run_git(repository, "init", "--quiet")
    run_git(repository, "config", "core.autocrlf", "true")
    run_git(repository, "config", "core.safecrlf", "true")
    run_git(repository, "add", "--", *paths)
    for path, content in before.items():
        assert run_git(repository, "cat-file", "blob", f":{path}").stdout == content, path
    checkout = tmp_path_factory.mktemp("automatic-conversion-checkout")
    run_git(repository, "checkout-index", "--all", "--force", f"--prefix={checkout}/")
    assert {path: (checkout / path).read_bytes() for path in paths} == before
    # Dependencies are separate repositories; populate their exact portable
    # snapshots after root checkout instead of tracking them in this Git fixture.
    for path in checker.SOURCE_PINS:
        if path.startswith("references/"):
            target = checkout / path
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(repository / path, target)
    report = checker.verify(checkout)
    assert report["result"] == "PASS", report
    no_authority(report)
