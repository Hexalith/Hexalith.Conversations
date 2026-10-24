"""Disposable successor-result regressions for deferred validation findings."""

import json
import os
import subprocess
from xml.etree import ElementTree

import pytest

from test_generate_story_record import load_generator, WORKSPACE


@pytest.fixture()
def python_result(tmp_path, monkeypatch):
    module = load_generator()
    candidate = "a" * 40
    script = "_bmad/scripts/example.py"
    (tmp_path / script).parent.mkdir(parents=True)
    (tmp_path / script).write_bytes(b"script")
    output = "example.json"
    monkeypatch.setattr(module, "v2_committed_blob",
                        lambda _root, _revision, path: b"script" if path == script else None)
    monkeypatch.setattr(module.subprocess, "run", lambda *_args, **_kwargs:
                        subprocess.CompletedProcess([], 0, b"", b""))

    def evaluate(document):
        (tmp_path / output).write_text(json.dumps(document), encoding="utf-8")
        return module.v2_successor_scenario_from_results(
            tmp_path, {"id": "AC-EXAMPLE-01", "command": "python fixture"},
            {"kind": "python", "script": script, "outputs": [output], "options": {}},
            candidate, 0, [])

    return evaluate


@pytest.mark.parametrize("document,passes", [
    ({"exitCode": 0, "status": "preserved-not-activated"}, True),
    ({"result": "PASS", "status": "preserved-not-activated"}, True),
    ({"status": "preserved-not-activated"}, False),
    ({"status": "PASS"}, True),
    ({"status": "passed", "exitCode": 0}, True),
    ({"status": "FAIL", "exitCode": 0}, False),
    ({"result": "FAIL", "exitCode": 0}, False),
    ({"result": "PASS", "status": "BLOCKED"}, False),
    ({"result": None, "exitCode": 0}, False),
    ({"result": True, "exitCode": 0}, False),
    ({"result": "PASS", "exitCode": 1}, False),
    ({"result": "PASS", "exitCode": False}, False),
    ({"exitCode": False}, False),
    ({"exitCode": 0.0}, False),
    ({"exitCode": "0"}, False),
    ({"exitCode": 0, "blockers": ["NOT_READY"]}, False),
])
def test_successor_verdict_is_independent_of_domain_status(python_result, document, passes):
    record, category, findings = python_result(document)
    assert (category == "passed") == passes
    assert record["result"] == ("PASS" if passes else "FAIL")
    assert bool(findings) != passes
    assert record["assertionLedger"]
    if not passes:
        assert record["blockers"] == ["TEST_RESULTS_FAILED"]


SUMMARY = dict(required=2, passed=2, failed=0, blocked=0, skipped=0, notRun=0)


@pytest.mark.parametrize("missing", list(SUMMARY))
def test_successor_summary_cannot_omit_any_scenario_counter(python_result, missing):
    summary = {key: value for key, value in SUMMARY.items() if key != missing}
    record, category, _ = python_result(dict(result="PASS", summary=summary))
    assert category == "failed" and record["blockers"] == ["TEST_RESULTS_FAILED"]


@pytest.mark.parametrize("summary,passes", [
    (SUMMARY, True),
    (dict(SUMMARY, required=1), False),
    (dict(SUMMARY, failed=1), False),
    (dict(SUMMARY, blocked=-1), False),
    (dict(SUMMARY, skipped=True), False),
    (dict(SUMMARY, notRun=0.0), False),
    (dict(SUMMARY, required=0, passed=0), False),
    ({"passed": 1}, False),
    ({"rows": 52, "state": "preserved"}, True),
    ({}, True),
    ([], False),
])
def test_successor_summary_preserves_domain_shapes_and_checks_scenario_counts(python_result, summary, passes):
    record, category, findings = python_result(dict(result="PASS", summary=summary))
    assert (category == "passed") == passes
    assert bool(findings) != passes
    assert record["result"] == ("PASS" if passes else "FAIL")


@pytest.mark.parametrize("suffix,passes", [
    ("", True),
    ("()", True),
    ('(trigger: "unknown", expectedReason: UnknownTenant)', True),
    ('(text: "a(b)c", value: null)', True),
    ("Other(value: 1)", False),
    (".Other(value: 1)", False),
    (" (value: 1)", False),
    ("(value: 1)extra", False),
    ("(value: 1", False),
])
def test_successor_method_selector_accepts_only_its_parameter_suffix(tmp_path, monkeypatch, suffix, passes):
    module = load_generator()
    method = "Example.Tests.Proof.ShouldPass"
    record, category, findings = evaluate_xunit(module, tmp_path, monkeypatch, [method + suffix], method)
    assert (category == "passed") == passes
    assert bool(findings) != passes
    if passes:
        assert record["assertionLedger"][0]["subject"] == method + suffix
    else:
        assert "TEST_FAILED" in record["blockers"]


def evaluate_xunit(module, repository, monkeypatch, names, selector):
    candidate = "a" * 40
    assembly = "Example.dll"
    binary = repository / assembly
    binary.write_bytes(b"1.0.0+" + candidate.encode())
    root = ElementTree.Element("TestRun")
    results = ElementTree.SubElement(root, "Results")
    for name in names:
        ElementTree.SubElement(results, "UnitTestResult", testName=name, outcome="Passed")
    method = ElementTree.SubElement(ElementTree.SubElement(root, "TestDefinitions"), "UnitTest")
    ElementTree.SubElement(method, "TestMethod", codeBase=assembly)
    summary = ElementTree.SubElement(root, "ResultSummary")
    ElementTree.SubElement(summary, "Counters", total=str(len(names)), executed=str(len(names)),
                          passed=str(len(names)), failed="0")
    output = repository / "example.trx"
    output.write_bytes(ElementTree.tostring(root))
    os.utime(binary, ns=(output.stat().st_mtime_ns,) * 2)
    monkeypatch.setattr(module, "v2_committed_blob", lambda *_args: None)
    return module.v2_successor_scenario_from_results(
        repository, {"id": "AC-EXAMPLE-01", "command": "xunit fixture"},
        dict(kind="xunit", assembly=assembly, output="example.trx", selector=selector, selectorKind="-method"),
        candidate, 0, [])


def test_successor_method_selector_accepts_native_theory_names_and_rejects_foreign_rows(tmp_path, monkeypatch):
    module = load_generator()
    path = WORKSPACE / "docs/release-evidence/conformance-oracle-pre-split-story-9.2.trx"
    original = path.read_bytes()
    root = ElementTree.fromstring(original)
    method = ("Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest."
              "LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState")
    names = [row.get("testName") for row in root.findall("./{*}Results/{*}UnitTestResult")
             if row.get("testName", "").startswith(method + "(") and row.get("outcome") == "Passed"]
    assert len(names) > 1
    record, category, findings = evaluate_xunit(module, tmp_path, monkeypatch, names, method)
    assert category == "passed" and not findings
    assert [row["subject"] for row in record["assertionLedger"]] == names
    for foreign in (method.lower() + "(value: 1)", "Other." + method + "(value: 1)"):
        record, category, _ = evaluate_xunit(module, tmp_path, monkeypatch, names + [foreign], method)
        assert category == "failed" and "TEST_FAILED" in record["blockers"]
    assert path.read_bytes() == original
