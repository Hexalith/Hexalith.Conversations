"""Fault controls for the published artifact collector's evidence boundaries."""
from __future__ import annotations

from argparse import Namespace
from collections import Counter
from contextlib import ExitStack
import copy
import importlib.util
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import io
import os
from pathlib import Path
import signal
import subprocess
import sys
import threading
import time
import zipfile

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "_bmad/scripts"))
import qualify_epic16_published_artifact as qualification


def inventory():
    """A complete minimal producer inventory for independent identity faults."""
    return dict(schema="hexalith.g4-tool-package-inventory.v1", version="4.38.1", configuration="Release",
                packages=[dict(id=name, version="4.38.1", **{role: dict(file=f"{name}.4.38.1.{role}", sha256="a" * 64, sizeBytes=1)
                           for role in ("nupkg", "snupkg")}) for name in ("Hexalith.Builds.Module.Cli", "Hexalith.Builds.Evidence.Cli")],
                qualificationEvidence=[dict(file="qualification-evidence/positive.json", sha256="a" * 64, sizeBytes=1)],
                qualification=dict(sourceTree=dict(clean=True, revision="b" * 40), releaseEligible=True,
                                   ineligibilityReasons=[], packageBuild=dict(mode="official", result="passed"),
                                   sourceValidation=dict(mode="executed", result="passed"), controls=dict(mode="executed", result="passed"),
                                   fixtures=dict(files=[dict(file="positive.json", sha256="a" * 64, sizeBytes=1)])))


@pytest.fixture
def fresh(tmp_path):
    return qualification.FreshOutput(tmp_path, tmp_path / qualification.OBSERVATIONS / "attempt" / "collection")


def error_code(code, action):
    with pytest.raises(qualification.QualificationError) as caught:
        action()
    assert caught.value.code == code


def ready_candidate(collector):
    collector.ledger = [dict(gate=gate, state="PASS") for gate in qualification.EXECUTION_PREREQUISITES]
    collector.candidate_verified = True


def bare_collector(tmp_path, fresh):
    collector = object.__new__(qualification.Collector)
    collector.root = collector.builds = collector.platform = tmp_path
    collector.output, collector.deadline = fresh, qualification.Deadline(10)
    collector.args = Namespace(version="4.38.1", release_sha="b" * 40)
    collector.ledger, collector.identity, collector.originals = [], {}, {}
    collector.readers = ExitStack()
    collector.env, collector.tool = {}, tmp_path / "candidate-tool"
    collector.runner = Namespace(commands=[])
    collector.api = lambda *_args: dict(workflow_runs=[])
    return collector


def test_inventory_positive_and_exact_identity_substitutions():
    original = inventory()
    result = qualification.validate_inventory(original, "4.38.1", "b" * 40)
    assert result["id"] == "Hexalith.Builds.Module.Cli"
    for field, value in (("version", "4.38.0"), ("configuration", "Debug")):
        faulty = copy.deepcopy(original)
        faulty[field] = value
        error_code("IDENTITY_MISMATCH", lambda: qualification.validate_inventory(faulty, "4.38.1", "b" * 40))
    faulty = copy.deepcopy(original)
    faulty["qualification"]["sourceTree"]["revision"] = "c" * 40
    error_code("IDENTITY_MISMATCH", lambda: qualification.validate_inventory(faulty, "4.38.1", "b" * 40))
    faulty = copy.deepcopy(original)
    faulty["packages"][0]["nupkg"]["sha256"] = "substituted"
    error_code("IDENTITY_MISMATCH", lambda: qualification.validate_inventory(faulty, "4.38.1", "b" * 40))


@pytest.mark.parametrize("fault", ["missing", "extra", "duplicate", "skip"])
def test_producer_controls_cannot_be_vacuous(fault):
    data = inventory()
    qualification.validate_inventory(data, "4.38.1", "b" * 40)
    if fault == "missing":
        data["qualificationEvidence"] = []
    elif fault == "duplicate":
        data["qualificationEvidence"] *= 2
    elif fault == "extra":
        data["packages"].append(copy.deepcopy(data["packages"][0]))
    else:
        data["qualification"]["controls"] = dict(mode="not-run", result="not-run")
    with pytest.raises(qualification.QualificationError):
        qualification.validate_inventory(data, "4.38.1", "b" * 40)


def test_configuration_fields_relocate_semantically_and_preserve_input():
    document = json.loads((ROOT / qualification.MANIFEST).read_bytes())
    original = copy.deepcopy(document)
    fields = qualification.configuration_fields(document)
    secret_fields = document["modules"][0]["integration"]["secrets"]
    input_fields = document["modules"][0]["runtime"]["extensions"][0]["configurationInputs"]
    secret_fields.reverse()
    input_fields.reverse()
    moved = qualification.configuration_fields(document)
    assert [field for _, field in moved] != [field for _, field in fields]
    assert all("[0]" in field for _, field in moved)
    mutated = qualification.mutate(original, fields[0][0], "1TOKEN")
    assert original == json.loads((ROOT / qualification.MANIFEST).read_bytes())
    assert mutated != original
    document["modules"].append(copy.deepcopy(document["modules"][0]))
    error_code("CONTROLS_INCOMPLETE", lambda: qualification.configuration_fields(document))


def control_rows():
    return [dict(field=field, value=value, format=format_name, nativeExitCode=1, ruleId="HXP002", passed=True)
            for field in ("secret.configurationKey", "input.key") for value in qualification.INVALID_KEYS
            for format_name in ("human", "json")]


@pytest.mark.parametrize("fault", ["missing", "extra", "duplicate", "wrong-exit", "wrong-field", "wrong-rule"])
def test_exact_36_control_inventory_rejects_substitutions(fault):
    rows = control_rows()
    qualification.check_control_inventory(rows, ["secret.configurationKey", "input.key"])
    if fault == "missing":
        rows.pop()
    elif fault == "extra":
        rows.append(dict(rows[0], value="unexpected"))
    elif fault == "duplicate":
        rows[-1] = rows[0]
    elif fault == "wrong-exit":
        rows[0]["nativeExitCode"] = 0
    elif fault == "wrong-field":
        rows[0]["field"] = "wrong.key"
    else:
        rows[0]["ruleId"] = "HXP003"
    error_code("CONTROLS_INCOMPLETE", lambda: qualification.check_control_inventory(rows, ["secret.configurationKey", "input.key"]))


@pytest.mark.parametrize("format_name", ["human", "json"])
def test_negative_requires_exact_field_source_and_one_diagnostic(tmp_path, format_name):
    source = tmp_path / "input.json"
    source.write_bytes(b"{}")
    row = dict(ruleId="HXP002", field="actual.key", source=str(source))
    def output(rows):
        if format_name == "json":
            return json.dumps(dict(status="failed", diagnostics=rows)).encode()
        return "\n".join(f"{item['ruleId']} Manifest Manifest source={item['source']} field={item['field']}: invalid" for item in rows).encode()
    native = dict(exitCode=1, termination=None)
    qualification.check_negative(native, output([row]), "actual.key", source, tmp_path, format_name)
    for rows in ([dict(row, field="wrong.key")], [dict(row, source="wrong.json")], [row, row], [dict(row, ruleId="HXP003")]):
        error_code("CONTROL_MISMATCH", lambda: qualification.check_negative(native, output(rows), "actual.key", source, tmp_path, format_name))
    error_code("CONTROL_MISMATCH", lambda: qualification.check_negative(dict(exitCode=0, termination=None), output([row]), "actual.key", source, tmp_path, format_name))
    assert source.read_bytes() == b"{}"


def test_adoption_exact_identity_delta_and_owner_disposition():
    before = [dict(requirement="tool-pin", passed=False, file=".config/dotnet-tools.json", reason="pin"),
              dict(requirement="dependency-selectors", passed=False, file="module/validation/earlier/prepared-imports/SourceMapping.props", reason="root")]
    assert qualification.adoption_delta(before, before)["orderedEqual"] is True
    after = [dict(before[0], reason="changed"), dict(requirement="dapr-subscriptions", passed=False, file="src/File.cs", reason="literal")]
    delta = qualification.adoption_delta(before, after)
    assert delta["orderedEqual"] is False
    assert len(delta["added"]) == len(delta["removed"]) == 2
    assert len(delta["reasonChanges"]) == 1
    for row in before + after:
        result = qualification.disposition(row)
        assert result["acceptanceState"] == "pending" and result["ownerScope"] and result["nextAction"]
    assert qualification.disposition(before[1])["classification"] == "generated import"
    assert qualification.disposition(dict(before[0], file="tests/fixtures/Frozen.cs"))["classification"] == "accepted fixture"
    for faulty in ([], before + before[:1]):
        error_code("ADOPTION_INVENTORY_DRIFT", lambda: qualification.adoption_delta(before, faulty))
    parsed = qualification.human_adoption(b"FAIL tool-pin file=.config/dotnet-tools.json: pin\n")
    assert parsed == before[:1]


def test_output_ownership_rejects_existing_source_and_aliases(tmp_path, fresh):
    fresh.write("retained.txt", b"unchanged")
    error_code("OUTPUT_FORBIDDEN", lambda: fresh.write("retained.txt", b"replacement"))
    assert (fresh.path / "retained.txt").read_bytes() == b"unchanged"
    error_code("OUTPUT_FORBIDDEN", lambda: qualification.FreshOutput(tmp_path, fresh.path))
    error_code("OUTPUT_FORBIDDEN", lambda: qualification.FreshOutput(tmp_path, tmp_path / "src/overwrite"))
    outside = tmp_path / "outside"
    outside.mkdir()
    alias = tmp_path / qualification.OBSERVATIONS / "alias"
    alias.symlink_to(outside, target_is_directory=True)
    error_code("OUTPUT_FORBIDDEN", lambda: qualification.FreshOutput(tmp_path, alias / "collection"))
    leaf = fresh.path / "leaf"
    leaf.symlink_to(tmp_path / "outside-file")
    error_code("OUTPUT_FORBIDDEN", lambda: fresh.write("leaf", b"replacement"))
    assert not (tmp_path / "outside-file").exists()


def test_native_capture_positive_timeout_and_descendant_cleanup(tmp_path, fresh):
    runner = qualification.Runner(fresh, qualification.Deadline(10))
    result, data = runner.run("positive", [sys.executable, "-c", "print('complete')"], tmp_path, maximum=2)
    assert result["exitCode"] == 0 and data == b"complete\n"
    child_pid = fresh.path / "child.pid"
    script = "import subprocess,sys,time,pathlib; p=subprocess.Popen([sys.executable,'-c','import time; time.sleep(60)']); pathlib.Path(sys.argv[1]).write_text(str(p.pid)); time.sleep(60)"
    result, _ = runner.run("timeout", [sys.executable, "-c", script, str(child_pid)], tmp_path, maximum=.2)
    assert result["termination"] == "PROCESS_TIMEOUT" and result["exitCode"] == -signal.SIGKILL
    assert result["durationSeconds"] < 3 and result["stdout"]["complete"] is False
    pid = int(child_pid.read_text())
    status = Path(f"/proc/{pid}/stat")
    for _ in range(100):
        if not status.exists() or status.read_text().split(") ", 1)[1].startswith("Z"):
            break
        time.sleep(.01)
    else:
        pytest.fail("owned child remains running after timeout")


def test_stream_limit_and_caller_cancellation_are_bounded(tmp_path, fresh, monkeypatch):
    monkeypatch.setitem(qualification.LIMITS, "streamBytes", 128)
    runner = qualification.Runner(fresh, qualification.Deadline(10))
    result, data = runner.run("overflow", [sys.executable, "-c", "import sys,time;sys.stdout.write('x'*1024);sys.stdout.flush();time.sleep(60)"], tmp_path, maximum=2)
    assert result["termination"] == "CAPTURE_LIMIT" and len(data) <= 128
    original = qualification.selectors.DefaultSelector.select
    def cancel(self, *args):
        raise KeyboardInterrupt()
    monkeypatch.setattr(qualification.selectors.DefaultSelector, "select", cancel)
    with pytest.raises(KeyboardInterrupt):
        runner.run("cancel", [sys.executable, "-c", "import time;time.sleep(60)"], tmp_path, maximum=2)
    assert runner.commands[-1]["termination"] == "COLLECTION_CANCELLED"
    assert runner.commands[-1]["exitCode"] == -signal.SIGKILL


def test_archive_hash_substitution_and_expansion_bound(tmp_path, monkeypatch):
    package = tmp_path / "package.nupkg"
    with zipfile.ZipFile(package, "w") as archive:
        archive.writestr("payload.dll", b"actual")
        archive.writestr(".signature.p7s", b"signature")
    rows = qualification.package_payload(package, qualification.Deadline(2))
    assert next(row for row in rows if row["file"] == "payload.dll")["sha256"] == qualification.sha(b"actual")
    monkeypatch.setitem(qualification.LIMITS, "expandedBytes", 2)
    error_code("ARCHIVE_LIMIT", lambda: qualification.package_payload(package, qualification.Deadline(2)))


def test_unsigned_archive_requires_exact_ci_hash_size_and_payload(tmp_path):
    package = tmp_path / "unsigned.nupkg"
    with zipfile.ZipFile(package, "w") as archive:
        archive.writestr("payload.dll", b"actual")
    rows = qualification.package_payload(package, qualification.Deadline(2))
    signed_rows = [dict(file=".signature.p7s", sha256="a" * 64, sizeBytes=10), *rows]
    expected = dict(sha256=qualification.sha(package.read_bytes()), sizeBytes=package.stat().st_size)
    assert qualification.compare_unsigned(package, expected, signed_rows, qualification.Deadline(2))["exactCiArchiveEquality"] is True
    for faulty in (dict(expected, sha256="a" * 64), dict(expected, sizeBytes=1)):
        error_code("IDENTITY_MISMATCH", lambda: qualification.compare_unsigned(package, faulty, signed_rows, qualification.Deadline(2)))
    substituted = [dict(rows[0], sha256="a" * 64)]
    error_code("IDENTITY_MISMATCH", lambda: qualification.compare_unsigned(package, expected, substituted, qualification.Deadline(2)))


def test_embedded_catalog_matches_actual_source_props_and_detects_substitution():
    raw = b'''<Project><PropertyGroup>
      <EventVersion Condition="'$(EventVersion)' == ''">3.128.3</EventVersion>
      <FrontVersion>4.7.0</FrontVersion><DaprHostingVersion>13.6.1-beta.935</DaprHostingVersion>
      <DaprHostingVersion Condition="'$(MSBuildProjectName)' == 'Unselected.Project'">99.0.0</DaprHostingVersion>
      <HexalithAspireAppHostSdkVersion>13.6.3</HexalithAspireAppHostSdkVersion>
      <HexalithDaprRuntimeVersion>1.18.7</HexalithDaprRuntimeVersion><HexalithDaprCliVersion>1.18.2</HexalithDaprCliVersion>
      <HexalithRedisImage>docker.io/library/redis</HexalithRedisImage><HexalithRedisImageTag>7.4-alpine</HexalithRedisImageTag>
      <HexalithRedisImageDigest>sha256:abc</HexalithRedisImageDigest></PropertyGroup><ItemGroup>
      <PackageVersion Include="Hexalith.EventStore.Aspire" Version="$(EventVersion)"/>
      <PackageVersion Include="Hexalith.FrontComposer.Shell" Version="$(FrontVersion)"/>
      <PackageVersion Include="CommunityToolkit.Aspire.Hosting.Dapr" Version="$(DaprHostingVersion)"/>
      <PackageVersion Include="Dapr.Client" Version="1.18.10"/><PackageVersion Include="Aspire.Hosting" Version="13.6.3"/>
      </ItemGroup></Project>'''
    catalog = dict(eventStoreVersion="3.128.3", frontComposerVersion="4.7.0", daprSdkVersion="1.18.10",
                   aspireHostingVersion="13.6.3", aspireHostingDaprVersion="13.6.1-beta.935", aspireAppHostSdkVersion="13.6.3",
                   daprRuntimeVersion="1.18.7", daprCliVersion="1.18.2", redisImage="docker.io/library/redis",
                   redisImageTag="7.4-alpine", redisImageDigest="sha256:abc")
    assert qualification.compare_catalog(raw, catalog) == catalog
    error_code("IDENTITY_MISMATCH", lambda: qualification.compare_catalog(raw, dict(catalog, eventStoreVersion="0.0.0")))


def test_missing_package_preparation_retains_native_failure_and_no_source_credit(tmp_path, fresh):
    collector = object.__new__(qualification.Collector)
    collector.artifact_validated, collector.root, collector.output = True, tmp_path, fresh
    collector.tool, collector.env = tmp_path / "tool", {}
    ready_candidate(collector)
    class RefusedRunner:
        def run(self, *args, **kwargs):
            assert "package" in args[1] and "prepare-build" in args[1]
            return dict(exitCode=2, termination=None), b'{"status":"failed"}'
    collector.runner = RefusedRunner()
    collector.gate("package-preparation-action", collector.package_preparation)
    assert collector.ledger[-1] == dict(gate="package-preparation-action", state="BLOCKED", code="PACKAGE_PREPARATION_UNAVAILABLE", detail="exact published package preparation native exit 2; action export remains unavailable")
    result = json.loads((fresh.path / "package-preparation-action.json").read_bytes())
    assert result["result"] == "BLOCKED" and result["nativeExitCode"] == 2 and "BLOCKED" in result["actionExports"]


def test_verdict_preserves_failure_blocker_and_non_vacuity():
    assert qualification.verdict([dict(state="PASS")]) == ("PASS", 0)
    assert qualification.verdict([dict(state="PASS"), dict(state="BLOCKED")]) == ("BLOCKED", 2)
    assert qualification.verdict([dict(state="FAIL"), dict(state="BLOCKED")]) == ("FAIL", 1)
    error_code("EMPTY_LEDGER", lambda: qualification.verdict([]))
    error_code("EMPTY_LEDGER", lambda: qualification.verdict([dict(state="not-applicable")]))
    error_code("IDENTITY_MISMATCH", lambda: qualification.load_json(b'{"hash":"original","hash":"substitution"}'))


def test_restoration_fault_is_fail_with_a_nonempty_ledger(tmp_path):
    protected = tmp_path / "manifest.json"
    protected.write_bytes(b"original")
    collector = object.__new__(qualification.Collector)
    collector.ledger, collector.originals = [], {"manifest.json": qualification.sha(b"original")}
    with qualification.ContainedReader(tmp_path) as reader:
        collector.reader = reader
        protected.write_bytes(b"changed")
        try:
            collector.gate("restoration", collector.restoration)
            assert collector.ledger[0]["code"] == "INPUT_RESTORATION_MISMATCH"
            assert qualification.verdict(collector.ledger) == ("FAIL", 1)
        finally:
            protected.write_bytes(b"original")
    assert protected.read_bytes() == b"original"


def test_missing_artifact_and_pending_owner_evidence_stay_blocked():
    collector = object.__new__(qualification.Collector)
    collector.ledger = []
    collector.gate("artifact", collector.artifact)
    assert collector.ledger[0]["code"] == "ARTIFACT_UNAVAILABLE"
    assert qualification.verdict(collector.ledger) == ("BLOCKED", 2)
    collector.pending_gates()
    assert all(row["state"] == "BLOCKED" for row in collector.ledger)
    assert any(row["gate"] == "native-windows-macos" for row in collector.ledger)
    assert all(row["code"] == "OWNER_EVIDENCE_PENDING" for row in collector.ledger[1:])


def test_cli_invalid_roots_and_output_alias_return_nonempty_preflight_ledger(tmp_path, capsys):
    builds = tmp_path / "references/Hexalith.Builds"
    platform = tmp_path / "references/Hexalith.Platform"
    builds.mkdir(parents=True)
    platform.mkdir()
    destination = tmp_path / qualification.OBSERVATIONS / "fresh"
    base = ["--conversations", str(tmp_path), "--platform", str(platform), "--version", "4.38.1",
            "--release-sha", "b" * 40, "--inventory", str(tmp_path / "inventory.json"), "--output", str(destination)]
    assert qualification.main([*base, "--builds", str(platform)]) == 1
    result = json.loads(capsys.readouterr().err)
    assert result["ledger"] and result["ledger"][0]["code"] == "PATH_ESCAPE"
    assert not destination.exists()
    output = qualification.FreshOutput(tmp_path, destination)
    original = tmp_path / "source.json"
    original.write_bytes(b"unchanged source")
    os.link(original, output.path / "alias.json")
    error_code("OUTPUT_FORBIDDEN", lambda: output.write("alias.json", b"replacement"))
    assert original.read_bytes() == b"unchanged source"
    assert qualification.main([*base, "--builds", str(builds)]) == 1
    result = json.loads(capsys.readouterr().err)
    assert result["ledger"] and result["ledger"][0]["code"] == "OUTPUT_FORBIDDEN"


def test_available_platform_catalog_disagreement_is_not_hidden_by_missing_release_tag():
    gitlink = "160000 commit " + "a" * 40 + "\treferences/Hexalith.Builds"
    assert qualification.platform_catalog_binding(gitlink, "a" * 40) is True
    assert qualification.platform_catalog_binding(gitlink, "b" * 40) is False
    error_code("IDENTITY_MISMATCH", lambda: qualification.platform_catalog_binding(gitlink.replace("160000", "100644"), "a" * 40))


@pytest.fixture
def transfer_server():
    """Serve both a complete body and a genuinely trickling HTTP response."""
    received = threading.Event()
    stop = threading.Event()
    state = dict(trickledBytes=0)
    class Handler(BaseHTTPRequestHandler):
        def do_GET(self):
            self.send_response(200)
            body = b"complete artifact bytes" if self.path == "/positive" else b"x" * 4096
            self.send_header("Content-Length", str(len(body) + (20 if self.path == "/truncated" else 0)))
            self.end_headers()
            if self.path != "/trickle":
                self.wfile.write(body if self.path != "/truncated" else b"abc")
                self.wfile.flush()
                return
            received.set()
            try:
                for byte in body:
                    self.wfile.write(bytes([byte]))
                    self.wfile.flush()
                    state["trickledBytes"] += 1
                    if stop.wait(.02):
                        break
            except (BrokenPipeError, ConnectionResetError):
                pass
        def log_message(self, *_args):
            pass
    server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
    thread = threading.Thread(target=lambda: server.serve_forever(poll_interval=.02), daemon=True)
    thread.start()
    try:
        yield f"http://127.0.0.1:{server.server_port}/", received, state
    finally:
        stop.set()
        server.shutdown()
        server.server_close()
        thread.join(timeout=2)
        assert not thread.is_alive()


def test_network_worker_positive_transfer_and_archive_byte_cap(tmp_path, fresh, transfer_server, monkeypatch):
    prefix, _, _ = transfer_server
    runner = qualification.Runner(fresh, qualification.Deadline(5))
    target = qualification.bounded_transfer(runner, "positive.bin", prefix + "positive", tmp_path, allowed_prefix=prefix)
    assert target.read_bytes() == b"complete artifact bytes"
    metadata = json.loads((fresh.path / "positive.bin.http.json").read_bytes())
    assert metadata["complete"] is True and metadata["state"] == "PASS"
    assert metadata["sha256"] == qualification.sha(target.read_bytes())
    monkeypatch.setitem(qualification.LIMITS, "archiveBytes", 16)
    error_code("CAPTURE_LIMIT", lambda: qualification.bounded_transfer(runner, "oversize.bin", prefix + "large", tmp_path, archive=True, allowed_prefix=prefix))
    metadata = json.loads((fresh.path / "oversize.bin.http.json").read_bytes())
    assert metadata["state"] == "BLOCKED" and metadata["complete"] is False
    assert (fresh.path / "oversize.bin").stat().st_size <= 16
    assert runner.commands[-1]["exitCode"] == 2 and runner.commands[-1]["termination"] is None


@pytest.mark.parametrize("network_seconds,overall_seconds", [(.35, 5), (5, .35)])
def test_real_trickle_response_obeys_parent_network_and_remaining_deadlines(tmp_path, fresh, transfer_server, monkeypatch, network_seconds, overall_seconds):
    prefix, received, state = transfer_server
    monkeypatch.setitem(qualification.LIMITS, "networkSeconds", network_seconds)
    runner = qualification.Runner(fresh, qualification.Deadline(overall_seconds))
    started = time.monotonic()
    error_code("PROCESS_TIMEOUT", lambda: qualification.bounded_transfer(runner, "trickle.bin", prefix + "trickle", tmp_path, allowed_prefix=prefix))
    assert received.is_set() and state["trickledBytes"] >= 2
    assert time.monotonic() - started < 1.5
    metadata = json.loads((fresh.path / "trickle.bin.http.json").read_bytes())
    assert metadata["complete"] is False and metadata["code"] == "PROCESS_TIMEOUT"
    native = runner.commands[-1]
    assert native["exitCode"] == -signal.SIGKILL and native["stdout"]["complete"] is False
    assert native["processId"] == native["processGroup"]
    with pytest.raises(ProcessLookupError):
        os.kill(native["processId"], 0)


def test_network_worker_caller_cancellation_reaps_the_owned_worker(tmp_path, fresh, transfer_server, monkeypatch):
    prefix, received, _ = transfer_server
    runner = qualification.Runner(fresh, qualification.Deadline(5))
    original = qualification.selectors.DefaultSelector.select
    def cancel_when_transferring(self, *args):
        if received.is_set():
            raise KeyboardInterrupt()
        return original(self, *args)
    monkeypatch.setattr(qualification.selectors.DefaultSelector, "select", cancel_when_transferring)
    with pytest.raises(KeyboardInterrupt):
        qualification.bounded_transfer(runner, "cancelled.bin", prefix + "trickle", tmp_path, allowed_prefix=prefix)
    assert received.is_set()
    native = runner.commands[-1]
    assert native["termination"] == "COLLECTION_CANCELLED" and native["exitCode"] == -signal.SIGKILL
    with pytest.raises(ProcessLookupError):
        os.kill(native["processId"], 0)
    metadata = json.loads((fresh.path / "cancelled.bin.http.json").read_bytes())
    assert metadata["complete"] is False and metadata["code"] == "COLLECTION_CANCELLED"


def test_real_http_truncation_is_incomplete_blocked_with_positive_control(tmp_path, fresh, transfer_server):
    prefix, _, _ = transfer_server
    runner = qualification.Runner(fresh, qualification.Deadline(5))
    assert qualification.bounded_transfer(runner, "complete", prefix + "positive", tmp_path, allowed_prefix=prefix).read_bytes() == b"complete artifact bytes"
    error_code("INCOMPLETE_TRANSFER", lambda: qualification.bounded_transfer(runner, "short", prefix + "truncated", tmp_path, allowed_prefix=prefix))
    report = json.loads((fresh.path / "short.http.json").read_bytes())
    assert report["state"] == "BLOCKED" and report["complete"] is False and report["bytes"] == 3
    assert (fresh.path / "short").read_bytes() == b"abc"


@pytest.mark.parametrize("diagnostics,state,code", [
    (b"NU3008: The package integrity check failed.", "FAIL", "SIGNATURE_INVALID"),
    (b"NU3005: The package contains an invalid package signature file.", "FAIL", "SIGNATURE_INVALID"),
    (b"NU3012: The primary signature validation failed.", "FAIL", "SIGNATURE_INVALID"),
    (b"NU3028: The revocation server could not be reached.", "BLOCKED", "SIGNATURE_VERIFICATION_UNAVAILABLE"),
    (b"NU3018: A chain terminated in an untrusted root certificate.", "BLOCKED", "SIGNATURE_VERIFICATION_UNAVAILABLE"),
    (b"Could not contact the verification service.", "BLOCKED", "SIGNATURE_VERIFICATION_UNAVAILABLE"),
])
def test_native_signature_result_distinguishes_invalid_and_unavailable(diagnostics, state, code):
    qualification.check_signature(dict(exitCode=0, termination=None), b"Successfully verified")
    with pytest.raises(qualification.QualificationError) as error:
        qualification.check_signature(dict(exitCode=1, termination=None), diagnostics)
    assert (error.value.state, error.value.code) == (state, code)
    with pytest.raises(qualification.QualificationError) as error:
        qualification.check_signature(dict(exitCode=None, termination="PROCESS_TIMEOUT"), b"")
    assert error.value.state == "BLOCKED"


def test_successful_leader_cleanup_stops_descendant_with_closed_streams(tmp_path, fresh):
    runner = qualification.Runner(fresh, qualification.Deadline(5))
    pidfile = tmp_path / "descendant.pid"
    script = "import pathlib,subprocess,sys; p=subprocess.Popen([sys.executable,'-c','import time;time.sleep(60)'],stdin=subprocess.DEVNULL,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);pathlib.Path(sys.argv[1]).write_text(str(p.pid))"
    native, _ = runner.run("successful-leader", [sys.executable, "-c", script, str(pidfile)], tmp_path, maximum=2)
    assert native["exitCode"] == 0 and native["termination"] is None
    child = Path(f"/proc/{int(pidfile.read_text())}/stat")
    for _ in range(100):
        if not child.exists() or child.read_text().split(") ", 1)[1].startswith("Z"):
            break
        time.sleep(.01)
    else:
        pytest.fail("same-group descendant survived successful leader cleanup")


@pytest.mark.parametrize("cause", ["deadline", "cancellation", "entry-limit"])
def test_zero_size_watched_entries_obey_inner_bounds(tmp_path, fresh, monkeypatch, cause):
    watched = tmp_path / "watched"
    watched.mkdir()
    for index in range(200):
        (watched / str(index)).touch()
    actual = qualification.watch_tree
    checks = []
    def bounded(root, check):
        def inner():
            checks.append(1)
            if cause == "deadline":
                time.sleep(.002)
            if cause == "cancellation" and len(checks) == 10:
                raise KeyboardInterrupt()
            check()
        actual(root, inner)
    monkeypatch.setattr(qualification, "watch_tree", bounded)
    if cause == "entry-limit":
        monkeypatch.setitem(qualification.LIMITS, "watchedEntries", 16)
    runner = qualification.Runner(fresh, qualification.Deadline(5))
    args = [sys.executable, "-c", "import time;time.sleep(60)"]
    if cause == "cancellation":
        with pytest.raises(KeyboardInterrupt):
            runner.run("watch", args, tmp_path, maximum=2, watch_root=watched)
        assert runner.commands[-1]["termination"] == "COLLECTION_CANCELLED"
    else:
        native, _ = runner.run("watch", args, tmp_path, maximum=.03 if cause == "deadline" else 2, watch_root=watched)
        assert native["termination"] == ("PROCESS_TIMEOUT" if cause == "deadline" else "WATCH_TRAVERSAL_LIMIT")
    assert 2 < len(checks) < 200
    assert runner.commands[-1]["exitCode"] == -signal.SIGKILL


@pytest.mark.parametrize("cause,changed", [("cancel", False), ("cancel", True), ("internal", False), ("internal", True)])
def test_collect_finalization_checks_captured_inputs_after_interruptions(tmp_path, fresh, cause, changed):
    collector = bare_collector(tmp_path, fresh)
    path = tmp_path / "protected.json"
    path.write_bytes(b"original")
    collector.reader = collector.readers.enter_context(qualification.ContainedReader(tmp_path))
    def entry():
        collector.originals["protected.json"] = qualification.sha(b"original")
        collector.record("entry", "PASS", "ENTRY", "captured")
    def interrupt():
        if changed:
            path.write_bytes(b"changed")
        raise KeyboardInterrupt() if cause == "cancel" else RuntimeError("injected internal failure")
    collector.entry, collector.provenance = entry, interrupt
    result, exit_code = collector.collect()
    restoration = next(row for row in result["ledger"] if row["gate"] == "restoration")
    assert restoration["state"] == ("FAIL" if changed else "PASS")
    assert result["state"] == ("FAIL" if changed or cause == "internal" else "BLOCKED")
    assert exit_code == (1 if changed or cause == "internal" else 2)
    path.write_bytes(b"original")


def test_human_negative_rejects_other_diagnostic_families(tmp_path):
    source = tmp_path / "bad.json"
    source.write_bytes(b"{}")
    native = dict(exitCode=1, termination=None)
    expected = f"HXP002 Manifest Manifest source={source} field=actual.key: invalid\n".encode()
    qualification.check_negative(native, expected, "actual.key", source, tmp_path, "human")
    for stdout, stderr in [(expected + b"HXC130 Execution Cancelled: cancelled\n", b""), (expected, b"HXW011 Prerequisite: missing\n")]:
        error_code("CONTROL_MISMATCH", lambda: qualification.check_negative(native, stdout, "actual.key", source, tmp_path, "human", stderr))


@pytest.mark.parametrize("name", ["Schema.json", "../escaped.json", "..\\escaped.json", "C:\\escaped.json"])
def test_actual_inspector_resource_path_guard_cannot_create_escaped_file(tmp_path, name):
    directory = tmp_path / "embedded"
    directory.mkdir()
    script = tmp_path / "guard.ps1"
    script.write_text("param([string]$Directory,[string]$Name)\n$ErrorActionPreference='Stop'\n" + qualification.RESOURCE_PATH_GUARD + "\n$path=Get-ContainedResourcePath $Directory $Name\n[IO.File]::WriteAllText($path,'safe')\n")
    native = subprocess.run(["pwsh", "-NoProfile", "-File", str(script), "-Directory", str(directory), "-Name", name], capture_output=True, timeout=10)
    assert (native.returncode == 0) is (name == "Schema.json")
    assert not (tmp_path / "escaped.json").exists()
    assert sorted(path.name for path in directory.iterdir()) == (["Schema.json"] if name == "Schema.json" else [])


@pytest.mark.parametrize("failed_gate", ["producer-provenance", "qualified-unsigned-archive"])
@pytest.mark.parametrize("state", ["FAIL", "BLOCKED"])
def test_collect_failed_prerequisites_execute_no_candidate_commands(tmp_path, fresh, failed_gate, state):
    collector = bare_collector(tmp_path, fresh)
    calls = []
    def checked(name, args, *_args, **_kwargs):
        calls.append((name, args))
        return b"4.38.1+" + b"b" * 40
    collector.runner.checked = checked
    def phase(gate):
        def action():
            if gate == failed_gate:
                raise qualification.QualificationError("INJECTED_PREREQUISITE", "fault", state)
            collector.record(gate, "PASS", "MEASURED", "fixture")
            if gate == "published-artifact":
                collector.artifact_validated = True
            if gate == "producer-native":
                collector.record("producer-qualification-evidence", "PASS", "MEASURED", "fixture")
        return action
    for method, gate in [("entry", "entry"), ("provenance", "producer-provenance"),
                         ("fixture_inventory", "producer-fixtures"), ("restore", "isolated-restore"),
                         ("artifact", "published-artifact"), ("producer_native", "producer-native"),
                         ("unsigned_archive", "qualified-unsigned-archive"), ("review_findings", "producer-review"),
                         ("platform_pair", "platform-pair")]:
        setattr(collector, method, phase(gate))
    report, _ = collector.collect()
    assert not calls
    blocked = {row["gate"] for row in report["ledger"] if row.get("code") == "CANDIDATE_EXECUTION_BLOCKED"}
    assert blocked == {"candidate-version", "malformed-key-controls", "adoption", "package-preparation-action"}


def test_candidate_version_runs_only_after_complete_independent_prerequisites(tmp_path, fresh):
    collector = bare_collector(tmp_path, fresh)
    ready_candidate(collector)
    collector.candidate_verified = False
    calls = []
    collector.runner.checked = lambda name, args, *_args, **_kwargs: calls.append((name, args)) or b"4.38.1+" + b"b" * 40
    collector.candidate_version()
    assert collector.candidate_verified is True and calls == [("feed-tool-version", [str(collector.tool), "--version"])]


def test_pending_native_qualification_does_not_bar_byte_verified_diagnostic_controls(tmp_path, fresh):
    collector = bare_collector(tmp_path, fresh)
    ready_candidate(collector)
    collector.candidate_verified = False
    collector.record("producer-native", "BLOCKED", "PRODUCER_NATIVE_EVIDENCE_UNAVAILABLE", "missing native report")
    collector.record("producer-qualification-evidence", "BLOCKED", "QUALIFICATION_EVIDENCE_UNAVAILABLE", "missing declared G4 files")
    calls = []
    collector.runner.checked = lambda name, args, *_args, **_kwargs: calls.append(name) or b"4.38.1+" + b"b" * 40
    collector.candidate_version()
    collector.require_candidate_prerequisites()
    assert calls == ["feed-tool-version"] and collector.candidate_verified
    assert qualification.verdict(collector.ledger) == ("BLOCKED", 2)


def test_restore_installs_without_running_the_candidate(tmp_path, fresh):
    collector = bare_collector(tmp_path, fresh)
    calls = []
    def checked(name, args, *_args, **_kwargs):
        calls.append(name)
        assert name == "feed-tool-install"
        tool = Path(args[args.index("--tool-path") + 1])
        tool.mkdir()
        (tool / "hexalith.builds.module.cli.4.38.1.nupkg").write_bytes(b"archive")
        return b"installed"
    collector.runner.checked = checked
    collector.restore()
    assert calls == ["feed-tool-install"] and not hasattr(collector, "version_text")


@pytest.mark.parametrize("outcome", ["success", "failure", "cancellation"])
def test_unsigned_helper_owned_temporary_directory_is_always_removed(tmp_path, fresh, monkeypatch, outcome):
    collector = bare_collector(tmp_path, fresh)
    collector.artifact_validated = True
    sdk = tmp_path / "sdk/10.0.402"
    sdk.mkdir(parents=True)
    for name in ["NuGet.Packaging", "NuGet.Common", "NuGet.Frameworks", "NuGet.Versioning", "NuGet.Configuration"]:
        (sdk / (name + ".dll")).write_bytes(b"sdk")
    unsigned = tmp_path / "reference.nupkg"
    with zipfile.ZipFile(unsigned, "w") as archive:
        archive.writestr("payload", b"actual")
    collector.package = fresh.write("signed.nupkg", b"signed input")
    collector.package_inventory = dict(nupkg=dict(sha256=qualification.sha(unsigned.read_bytes()), sizeBytes=unsigned.stat().st_size))
    payload = qualification.package_payload(unsigned, collector.deadline)
    fresh.json("published-payload-entries.json", [dict(file=".signature.p7s", sha256="a" * 64, sizeBytes=1), *payload])
    directories = []
    def checked(name, args, cwd, **_kwargs):
        if name == "unsigned-helper-sdk-version":
            return b"10.0.402"
        if name == "unsigned-helper-sdk-list":
            return f"10.0.402 [{sdk.parent}]".encode()
        if name == "unsigned-helper-debug-build":
            directories.append(cwd)
            if outcome == "failure":
                raise qualification.QualificationError("NATIVE_COMMAND_UNAVAILABLE", "build unavailable", "BLOCKED")
            if outcome == "cancellation":
                raise KeyboardInterrupt()
            dll = cwd / "bin/Debug/net10.0/UnsignedProof.dll"
            dll.parent.mkdir(parents=True)
            dll.write_bytes(b"helper")
        elif name == "unsigned-archive-reconstruction":
            Path(args[-1]).write_bytes(unsigned.read_bytes())
        return b""
    collector.runner.checked = checked
    if outcome == "success":
        collector.unsigned_archive()
        assert (fresh.path / "unsigned-helper/UnsignedProof.dll.bytes").read_bytes() == b"helper"
        assert (fresh.path / "unsigned-reconstruction-proof.json").exists()
    else:
        with pytest.raises(KeyboardInterrupt if outcome == "cancellation" else qualification.QualificationError):
            collector.unsigned_archive()
    assert directories and all(not path.exists() for path in directories)
    assert (fresh.path / "unsigned-helper/Program.cs.bytes").is_file()


@pytest.mark.parametrize("native,evidence,valid", [(False, False, True), (True, False, True), (True, True, True), (True, True, False)])
def test_producer_payload_native_and_declared_evidence_are_independent(tmp_path, fresh, native, evidence, valid):
    collector = bare_collector(tmp_path, fresh)
    collector.release_run = dict(id=1)
    collector.package_inventory = dict(nupkg=dict(sha256="a" * 64))
    collector.identity = dict(signedArchive=dict(sha256="c" * 64))
    body = b'{"passed":true}'
    collector.inventory = dict(qualificationEvidence=[dict(file="qualification-evidence/control.json", sha256=qualification.sha(body), sizeBytes=len(body))])
    text = "Verified remote 'Hexalith.Builds.Module.Cli' '4.38.1' SHA-256 " + "c" * 64 + " (qualified local SHA-256 " + "a" * 64 + ")\n"
    if native:
        text += "Running tests from native.dll\nTest run summary: Passed!\ntotal: 1\nfailed: 0\nsucceeded: 1\nskipped: 0\n"
    data = io.BytesIO()
    with zipfile.ZipFile(data, "w") as archive:
        archive.writestr("Release.txt", text)
        if evidence:
            archive.writestr("qualification-evidence/control.json", body if valid else b"substituted")
    collector.runner.checked = lambda *_args, **_kwargs: data.getvalue()
    collector.producer_native()
    collector.qualification_evidence()
    gates = {row["gate"]: row["state"] for row in collector.ledger}
    assert gates["producer-payload-binding"] == "PASS"
    assert gates["producer-native"] == ("PASS" if native else "BLOCKED")
    assert gates["producer-qualification-evidence"] == ("BLOCKED" if not evidence else "PASS" if valid else "FAIL")
    rows = json.loads((fresh.path / "producer-qualification-evidence.json").read_bytes())
    assert len(rows) == 1 and rows[0]["state"] == gates["producer-qualification-evidence"]


def test_adoption_caller_retains_failures_and_cannot_accept_matching_failed_reports(tmp_path, fresh):
    collector = bare_collector(tmp_path, fresh)
    ready_candidate(collector)
    collector.artifact_validated = True
    row = dict(requirement="tool-pin", passed=False, file=".config/dotnet-tools.json", reason="pin mismatch")
    report = json.dumps(dict(adoption=[row])).encode()
    collector.reader = Namespace(read=lambda _path: report)
    collector.runner.run = lambda name, *_args, **_kwargs: (dict(exitCode=1, termination=None), report if name.endswith("json") else b"FAIL tool-pin file=.config/dotnet-tools.json: pin mismatch\n")
    collector.adoption()
    assert next(row for row in collector.ledger if row["gate"] == "adoption-acceptance")["state"] == "FAIL"
    dispositions = json.loads((fresh.path / "adoption-dispositions.json").read_bytes())
    assert dispositions["currentCounts"]["failed"] == 1 and dispositions["failures"][0]["acceptanceState"] == "pending"
    assert dispositions["failures"][0]["reason"] == row["reason"]
    assert qualification.verdict(collector.ledger) == ("FAIL", 1)


def test_platform_pair_caller_keeps_mismatch_fail_and_missing_tag_blocked(tmp_path, fresh):
    collector = bare_collector(tmp_path, fresh)
    inputs = {".config/dotnet-tools.json": json.dumps(dict(tools={"hexalith.builds.module.cli": dict(version="4.35.2", rollForward=False)})).encode(),
              "hexalith-platform.json": b'{"toolVersionRange":"[4.35.2, 5.0.0-0)"}',
              "README.md": b"Version agreement alone is not acceptance.",
              "eng/prepare_builds_artifacts.py": ('VERSION = "4.38.1"\nSOURCE = "' + "b" * 40 + '"\nARCHIVE_SHA256 = "' + "c" * 64 + '"\nTOOLING_SHA256 = "' + "d" * 64 + '"\n').encode()}
    collector.platform_reader = Namespace(read=lambda path: inputs[path])
    values = {"platform-head": b"e" * 40, "platform-tags": b"", "platform-builds-gitlink": ("160000 commit " + "a" * 40 + "\treferences/Hexalith.Builds").encode(), "platform-documented-builds-tag": b"d" * 40}
    collector.git = lambda label, *_args: values[label]
    collector.gate("platform-pair", collector.platform_pair)
    gates = {row["gate"]: row["state"] for row in collector.ledger}
    assert gates["platform-recorded-catalog"] == "FAIL"
    assert gates["platform-pair"] == gates["platform-release-identity"] == "BLOCKED"
    assert gates["platform-prepared-identity"] == "FAIL"
    assert qualification.verdict(collector.ledger) == ("FAIL", 1)


@pytest.mark.parametrize("substitution", [None, "loose", "embedded", "source"])
def test_artifact_path_rejects_each_schema_identity_substitution(tmp_path, fresh, monkeypatch, substitution):
    collector = bare_collector(tmp_path, fresh)
    source_schema = b'{"schema":"original"}'
    nuspec = ('<package><metadata><repository url="https://github.com/Hexalith/Hexalith.Builds" commit="' + "b" * 40 + '"/></metadata></package>').encode()
    package = fresh.target("package.nupkg")
    with zipfile.ZipFile(package, "w") as archive:
        archive.writestr(".signature.p7s", b"signature")
        archive.writestr("Hexalith.Builds.Module.Cli.nuspec", nuspec)
        archive.writestr("tools/net10.0/any/Hexalith.Builds.Tooling.dll", b"tooling")
        for index, name in enumerate(qualification.SCHEMAS):
            archive.writestr("tools/net10.0/any/schemas/" + name, b"substituted" if substitution == "loose" and index == 0 else source_schema)
    collector.package = package
    fresh.write("isolated/tool/Hexalith.Builds.Tooling.dll", b"tooling")
    raw = package.read_bytes()
    responses = {"feed-registration.json": b'{"catalogEntry":"https://api.nuget.org/catalog"}',
                 "feed-catalog.json": json.dumps(dict(packageHashAlgorithm="SHA512", packageHash=qualification.base64.b64encode(qualification.hashlib.sha512(raw).digest()).decode(), packageSize=len(raw))).encode(),
                 "feed.nuspec": nuspec}
    collector.download = lambda name, *_args: fresh.write(name, responses[name])
    collector.runner.run = lambda *_args, **_kwargs: (dict(exitCode=0, termination=None), b"signature verified")
    def inspect(_name, args, *_args, **_kwargs):
        directory = Path(args[args.index("-OutputDirectory") + 1])
        (directory / "Hexalith.Builds.Tooling.Manifest.platform-version-catalog.json").write_bytes(b"{}")
        resources = [dict(name="Embedded." + name, sha256=qualification.sha(b"substituted" if substitution == "embedded" and index == 0 else source_schema)) for index, name in enumerate(qualification.SCHEMAS)]
        return json.dumps(dict(informationalVersion="4.38.1+" + "b" * 40, assemblyLocation=args[args.index("-AssemblyPath") + 1], resources=resources)).encode()
    collector.runner.checked = inspect
    collector.git = lambda name, *_args: b"substituted" if substitution == "source" and name == "source-schema-0" else source_schema
    monkeypatch.setattr(qualification, "compare_catalog", lambda *_args: {})
    collector.gate("published-artifact", collector.artifact)
    assert collector.ledger[-1]["state"] == ("PASS" if substitution is None else "FAIL")
    assert getattr(collector, "artifact_validated", False) is (substitution is None)


@pytest.mark.parametrize("candidate_only", [False, True])
@pytest.mark.parametrize("root_version", ["4.35.2", "4.35.2-preview.1", None, False, 42, {}, [], "", "latest", "4.38", "4.38.3\n", "4.38.3/escape"])
def test_candidate_only_separates_root_pin_and_retains_planning_and_selected_inputs(tmp_path, fresh, candidate_only, root_version):
    collector = bare_collector(tmp_path, fresh)
    collector.args.candidate_only = candidate_only
    collector.planning_inputs, collector.adoption_predecessor = "planning.json", "previous.json"
    collector.tooling_contract = qualification.TOOLING_CONTRACT
    binding = str(Path(collector.tooling_contract).parent.parent / "binding.json")
    pin = json.dumps(dict(tools={"hexalith.builds.module.cli": dict(version=root_version, rollForward=False)})).encode()
    inputs = {path: b"unchanged" for path in [qualification.MANIFEST, ".gitmodules", ".gitattributes", "_bmad/scripts/epic16_trust_v2.py", "_bmad/scripts/qualify_epic16_published_artifact.py", "tests/tooling/test_epic16_published_artifact_qualification.py", collector.tooling_contract, "previous.json", "planned-source.txt"]}
    inputs.update({".config/dotnet-tools.json": pin, binding: b'{"sources":[]}', "planning.json": b'{"sources":[{"path":"planned-source.txt","sha256":"0000"}]}'})
    collector.reader = Namespace(read=lambda path: inputs[path])
    links = b"".join(b"160000 " + b"b" * 40 + b" 0\t" + path.encode() + b"\0" for path in ["references/Hexalith.Builds", "references/Hexalith.Platform"])
    collector.index_digest = lambda _label: "a" * 64
    def git(label, repository, *args):
        if label == "root-identity" or label.startswith("dependency-root-"):
            return str(repository).encode()
        if label == "root-gitlinks": return links
        if label == "root-index-modes": return b"160000 " + b"b" * 40 + b" 0\0" + b"160000 " + b"b" * 40 + b" 0\0"
        if label.startswith("dependency-head-"): return b"b" * 40
        return b""
    collector.git = git
    if candidate_only and root_version in ("4.35.2", "4.35.2-preview.1"):
        collector.entry()
        assert collector.identity["rootPin"]["versionsEqual"] is False
        assert collector.identity["rootPin"]["adoptionAccepted"] is False
        assert [row["state"] for row in collector.ledger] == ["PASS", "FAIL"]
        actual = json.loads((fresh.path / "entry-identities.json").read_bytes())
        assert actual["planningDifferences"] and actual["toolingContract"] == qualification.TOOLING_CONTRACT
        assert actual["adoptionPredecessor"] == "previous.json"
    else:
        error_code("IDENTITY_MISMATCH", collector.entry)
    assert inputs[".config/dotnet-tools.json"] == pin
    # Candidate-only never bypasses unrelated provenance/execution prerequisites.
    with pytest.raises(qualification.QualificationError) as caught:
        collector.require_candidate_prerequisites(version=False)
    assert caught.value.state == "BLOCKED" and caught.value.code == "CANDIDATE_EXECUTION_BLOCKED"


@pytest.mark.parametrize("prepared_match", [False, True])
def test_prepared_platform_tuple_and_readme_never_supply_acceptance(tmp_path, fresh, prepared_match):
    collector = bare_collector(tmp_path, fresh)
    prepared = "b" * 40 if prepared_match else "a" * 40
    inputs = {".config/dotnet-tools.json": b'{"tools":{"hexalith.builds.module.cli":{"version":"4.38.1","rollForward":false}}}',
              "hexalith-platform.json": b'{"toolVersionRange":"[4.38.1, 5.0.0-0)"}',
              "README.md": ("catalog commit is `" + prepared + "` and approved source agrees").encode(),
              "eng/prepare_builds_artifacts.py": ('VERSION="4.38.1"\nSOURCE="'+prepared+'"\nARCHIVE_SHA256="'+"c"*64+'"\nTOOLING_SHA256="'+"d"*64+'"\n').encode()}
    collector.platform_reader = Namespace(read=lambda path: inputs[path])
    values = {"platform-head": b"e" * 40, "platform-tags": b"v1.0.0", "platform-builds-gitlink": ("160000 commit " + prepared + "\treferences/Hexalith.Builds").encode()}
    collector.git = lambda label, *_args: values[label]
    collector.platform_pair()
    states = {row["gate"]: row["state"] for row in collector.ledger}
    assert states["platform-prepared-identity"] == ("PASS" if prepared_match else "FAIL")
    assert states["platform-recorded-catalog"] == "PASS" and states["platform-pair"] == "BLOCKED"
    assert json.loads((fresh.path / "platform-pair.json").read_bytes())["acceptance"]["evidence"] is None


@pytest.mark.parametrize("fault", [None, "missing", "substituted", "archive-digest", "duplicate"])
def test_exact_ci_evidence_carriers_preserve_missing_substituted_and_duplicate_states(tmp_path, fresh, fault):
    collector = bare_collector(tmp_path, fresh)
    name, body = "qualification-evidence/control.json", b'{"passed":true}'
    collector.inventory = dict(qualificationEvidence=[dict(file=name, sha256=qualification.sha(body), sizeBytes=len(body))])
    collector.producer_runs = [dict(id=1, head_sha="b" * 40)]
    data = io.BytesIO()
    with zipfile.ZipFile(data, "w") as archive:
        archive.writestr("unrelated.txt", b"no qualification credit")
        if fault != "missing": archive.writestr(name, b"substituted" if fault == "substituted" else body)
        if fault == "duplicate":
            with pytest.warns(UserWarning): archive.writestr(name, body)
    raw = data.getvalue()
    artifact = dict(id=2, expired=False, size_in_bytes=len(raw), digest="sha256:" + ("0" * 64 if fault == "archive-digest" else qualification.sha(raw)))
    collector.api = lambda *_args: dict(total_count=1, artifacts=[artifact])
    collector.runner.run = lambda *_args, **_kwargs: (dict(exitCode=0, termination=None), raw)
    collector.gate("producer-qualification-carriers", collector.qualification_evidence)
    rows = json.loads((fresh.path / "producer-qualification-evidence.json").read_bytes())
    expected = "PASS" if fault is None else "FAIL" if fault == "substituted" else "BLOCKED"
    assert rows[0]["state"] == expected
    state, _ = qualification.verdict(collector.ledger)
    assert state == ("PASS" if fault is None else "BLOCKED" if fault == "missing" else "FAIL")
    assert collector.ledger


def test_duplicate_adoption_rows_retain_every_native_row_and_owner_disposition(tmp_path, fresh):
    collector = bare_collector(tmp_path, fresh)
    ready_candidate(collector)
    collector.artifact_validated = True
    row = dict(requirement="tool-pin", passed=False, file=".config/dotnet-tools.json", reason="pin mismatch")
    report = json.dumps(dict(adoption=[row, row])).encode()
    collector.reader = Namespace(read=lambda _path: json.dumps(dict(adoption=[row])).encode())
    human = b"FAIL tool-pin file=.config/dotnet-tools.json: pin mismatch\n" * 2
    collector.runner.run = lambda name, *_args, **_kwargs: (dict(exitCode=1, termination=None), report if name.endswith("json") else human)
    collector.gate("adoption", collector.adoption)
    retained = json.loads((fresh.path / "adoption-inventories.json").read_bytes())
    assert retained["currentRows"] == [row, row]
    assert len(retained["failures"]) == len(retained["currentDispositions"]) == 2
    assert all(row["acceptanceState"] == "pending" and row["ownerScope"] for row in retained["currentDispositions"])
    assert qualification.verdict(collector.ledger) == ("FAIL", 1)
