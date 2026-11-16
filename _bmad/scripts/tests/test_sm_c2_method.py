"""Story 12.2 real-CLI method fault matrix; every fault restores all fixture bytes."""
from __future__ import annotations

from copy import deepcopy
from datetime import datetime, timedelta, timezone
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

import pytest

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "_bmad/scripts"
sys.path.insert(0, str(SCRIPTS))
import generate_sm_c2_method as gen

RESULT = "out/result.json"
FAULT_NOT_DETECTED = "FAULT_NOT_DETECTED"
FIXTURE_NOT_RESTORED = "FIXTURE_NOT_RESTORED"
PUBLICATION_COMMIT = "0cb5cf651e24ac69b9c60335cb0b058355b4c76f"
PUBLICATION_REF = ".git/refs/heads/sm-c2-publication"
REPLACEMENT_REFS = tuple(".git/refs/replace/" + commit for commit in (gen.PLANNING_COMMIT, gen.FIXTURE_BASIS_COMMIT))


def run(script: str, repo: Path, *args: str):
    return subprocess.run([sys.executable, str(SCRIPTS / script), "--repository", str(repo), *args],
                          cwd=repo, capture_output=True, text=True, check=False, timeout=60)


def generate(repo: Path, *args: str):
    return run("generate_sm_c2_method.py", repo, "--contract", gen.CONTRACT_PATH, "--baseline", gen.BASELINE_PATH,
               "--output-json", gen.JSON_PATH, "--output-markdown", gen.MARKDOWN_PATH, *args)


def verify(repo: Path, *args: str):
    report_path = repo / RESULT
    report_path.unlink(missing_ok=True)
    completed = run("verify_sm_c2_method.py", repo, "--evidence", gen.JSON_PATH, "--markdown", gen.MARKDOWN_PATH,
                    "--output", RESULT, *args)
    assert report_path.is_file(), f"Verifier did not write a fresh report: {completed.stderr}"
    report = json.loads(report_path.read_bytes())
    assert report["assertionLedger"], FAULT_NOT_DETECTED
    assert completed.returncode == {"PASS": 0, "FAIL": 1, "BLOCKED": 2}[report["result"]], completed.stderr
    assert "Traceback" not in completed.stderr
    return completed, report


def snapshot(repo: Path) -> dict[str, bytes | None]:
    paths = [*gen.readonly_inputs(), gen.JSON_PATH, gen.MARKDOWN_PATH, ".git/objects/info/alternates", ".git/shallow",
             PUBLICATION_REF, ".git/logs/refs/heads/sm-c2-publication", *REPLACEMENT_REFS]
    return {path: (repo / path).read_bytes() if (repo / path).exists() else None for path in paths}


def restore(repo: Path, before: dict[str, bytes | None]):
    for path, raw in before.items():
        if raw is None:
            (repo / path).unlink(missing_ok=True)
        else:
            (repo / path).write_bytes(raw)


@pytest.fixture(scope="module")
def repository(tmp_path_factory):
    repo = tmp_path_factory.mktemp("sm-c2-method") / "repo"
    for relative in gen.readonly_inputs():
        target = repo / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, target)
    subprocess.run(["git", "init", "-q", str(repo)], check=True)
    common = subprocess.run(["git", "-C", str(ROOT), "rev-parse", "--path-format=absolute", "--git-common-dir"],
                            check=True, capture_output=True, text=True).stdout.strip()
    (repo / ".git/objects/info/alternates").write_text(str(Path(common) / "objects") + "\n")
    draft = generate(repo)
    assert draft.returncode == 0, draft.stderr
    bundle = json.loads((repo / gen.JSON_PATH).read_bytes())
    digest = bundle["digests"]["signablePayloadSha256"]
    approved = generate(repo, "--approve-reviewer", "Test Owner", "--decided-at-utc", "2000-01-01T00:00:00Z",
                        "--approved-payload-sha256", digest)
    assert approved.returncode == 0, approved.stderr
    completed, report = verify(repo)
    assert completed.returncode == 0, report
    return repo


def write_bundle(repo: Path, bundle: dict, resign: bool = True):
    if resign:
        payload = gen.signable_of(bundle)
        approval = deepcopy(bundle.get("approval"))
        if isinstance(approval, dict):
            approval["boundSignablePayloadSha256"] = gen.sha256_bytes(gen.canonical(payload))
        raw, markdown = gen.finalize(payload, approval)
        (repo / gen.JSON_PATH).write_text(raw)
        (repo / gen.MARKDOWN_PATH).write_text(markdown)
    else:
        (repo / gen.JSON_PATH).write_text(gen.serialize_json(bundle))


def replace(path: str, value):
    def mutate(bundle):
        target = bundle
        parts = path.split(".")
        for part in parts[:-1]:
            target = target[int(part)] if isinstance(target, list) else target[part]
        if isinstance(target, list):
            target[int(parts[-1])] = value
        else:
            target[parts[-1]] = value
    return mutate


# Literal public blocker expectations are test-owned, independent of production constants.
FAULTS = [
    ("row-omitted", lambda b: b["rows"].pop(), "SMC2_METHOD_INCOMPLETE"),
    ("row-reordered", lambda b: b["rows"].reverse(), "SMC2_METHOD_INCOMPLETE"),
    ("row-added", lambda b: b["rows"].append(deepcopy(b["rows"][0])), "SMC2_METHOD_INCOMPLETE"),
    ("factor", replace("rows.0.threshold.factor", "1.06"), "SMC2_THRESHOLD_MUTATED"),
    ("numeric-factor", replace("rows.0.threshold.factor", 1.05), "SMC2_THRESHOLD_MUTATED"),
    ("signal-not-required", replace("rows.0.threshold.requiresUsableSignal", False), "SMC2_THRESHOLD_MUTATED"),
    *[(key, replace("rows.0." + key, True), "SMC2_THRESHOLD_MUTATED") for key in
      ("approvedCostCeilingMicroseconds", "disclosure", "approval", "recordedOnly", "correctnessRationale", "ownerAcceptance")],
    ("trim", replace("retention.trimming", "top-5-percent"), "SMC2_SAMPLE_DISCARDED"),
    ("outlier", replace("retention.outlierRule", "iqr"), "SMC2_SAMPLE_DISCARDED"),
    ("adaptive-count", replace("repetitions.adaptive", True), "SMC2_SAMPLE_DISCARDED"),
    ("count-changed", replace("repetitions.perSide.HP-CREATE", 30), "SMC2_SAMPLE_DISCARDED"),
    ("sample-deletion", replace("retention.sampleDeletion", True), "SMC2_SAMPLE_DISCARDED"),
    ("raw-retention", replace("retention.retainAllRawSamples", False), "SMC2_SAMPLE_DISCARDED"),
    ("adaptive-stopping", replace("retention.adaptiveStopping", True), "SMC2_SAMPLE_DISCARDED"),
    ("rerun-selection", replace("retention.rerunSelection", True), "SMC2_SAMPLE_DISCARDED"),
    ("session-drop", replace("repetitions.dropSession", True), "SMC2_SAMPLE_DISCARDED"),
    ("session-rerun", replace("repetitions.rerunSession", True), "SMC2_SAMPLE_DISCARDED"),
    ("warmups", replace("warmup.repetitionsPerSession", 0), "SMC2_SAMPLE_DISCARDED"),
    *[(field, replace("environment." + field, value), "SMC2_ENVIRONMENT_UNCONTROLLED") for field, value in
      (("runtime", "10.0.10"), ("sdk", "10.0.302"), ("configuration", "Debug"), ("concurrency", 2),
       ("host.sameHostForBothSides", False), ("host.recordBeforeObservation", []),
       ("forbiddenOverridePrefixes", []), ("forbiddenConcurrentProcesses", []),
       ("runnerFlags", ["-parallel", "all"]))],
    ("cold-for-warm", replace("classification.coldSubstitution", True), "SMC2_ENVIRONMENT_UNCONTROLLED"),
    ("percentile", replace("percentile.level", "0.90"), "SMC2_SIGNAL_RULE_MUTATED"),
    ("interpolation", replace("percentile.interpolation", True), "SMC2_SIGNAL_RULE_MUTATED"),
    *[(field, replace("signalQuality." + field, value), "SMC2_SIGNAL_RULE_MUTATED") for field, value in
      (("resamples", 9999), ("prng", "Random"), ("seed", "0"), ("lowerLevel", "0.05"),
       ("upperLevel", "0.95"), ("maximumHalfWidthFactor", "0.10"), ("indexDraw", "round(next*n)"),
       ("drawOrder", "candidate first"))],
    ("vector-seed", replace("selfChecks.vectors.0.seedHex", "0000000000000000"), "SMC2_SIGNAL_RULE_MUTATED"),
]


@pytest.mark.parametrize("name,mutation,expected", FAULTS, ids=[row[0] for row in FAULTS])
def test_method_fault_matrix_semantics(repository, name, mutation, expected):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        mutation(bundle)
        write_bundle(repository, bundle)
        completed, report = verify(repository)
        assert completed.returncode == 1, FAULT_NOT_DETECTED
        assert [item["code"] for item in report["blockers"]] == [expected], (FAULT_NOT_DETECTED, report)
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("target", ["approval", "digests", "extra-approval-metadata", "formatting"])
def test_method_fault_matrix_schema_pin(repository, target):
    before = snapshot(repository)
    try:
        schema = json.loads(before[gen.SCHEMA_PATH])
        if target == "formatting":
            (repository / gen.SCHEMA_PATH).write_bytes(before[gen.SCHEMA_PATH] + b"\n")
        else:
            schema["properties"]["approval" if target == "extra-approval-metadata" else target] = {}
            (repository / gen.SCHEMA_PATH).write_text(json.dumps(schema))
            if target == "extra-approval-metadata":
                bundle = json.loads(before[gen.JSON_PATH])
                bundle["approval"]["unapprovedMetadata"] = True
                write_bundle(repository, bundle)
        generated = generate(repository)
        assert generated.returncode == 1
        assert generated.stderr.split(":", 1)[0] == "SMC2_METHOD_INCOMPLETE", generated.stderr
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == ["SMC2_METHOD_INCOMPLETE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("schema", [None, {"type": "invalid-type"}], ids=["null-root", "invalid-type"])
def test_method_fault_matrix_unusable_schema(repository, schema):
    before = snapshot(repository)
    try:
        (repository / gen.SCHEMA_PATH).write_text(json.dumps(schema))
        generated = generate(repository)
        assert generated.returncode == 2 and "Traceback" not in generated.stderr
        assert generated.stderr.split(":", 1)[0] == "INPUT_UNAVAILABLE", generated.stderr
        completed, report = verify(repository)
        assert completed.returncode == 2 and report["result"] == "BLOCKED"
        assert [item["code"] for item in report["blockers"]] == ["INPUT_UNAVAILABLE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("authority", [None, [], 1, {"artifacts": None}, {"artifacts": {}}, {"artifacts": "bad"},
                                      *[{"artifacts": [value]} for value in (None, 1, "bad", [])]],
                         ids=["root-null", "root-array", "root-scalar", "collection-null", "collection-object",
                              "collection-string", "item-null", "item-scalar", "item-string", "item-array"])
def test_method_fault_matrix_authority_shape(repository, authority):
    before = snapshot(repository)
    try:
        (repository / gen.AUTHORITY_PATH).write_text(json.dumps(authority))
        generated = generate(repository)
        assert generated.returncode == 2 and "Traceback" not in generated.stderr
        assert generated.stderr.split(":", 1)[0] == "INPUT_UNAVAILABLE", generated.stderr
        completed, report = verify(repository)
        assert completed.returncode == 2 and report["result"] == "BLOCKED"
        assert [item["code"] for item in report["blockers"]] == ["INPUT_UNAVAILABLE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("decision", [None, [], 1, "bad"], ids=["null", "array", "number", "string"])
def test_method_fault_matrix_predecessor_shape(repository, decision):
    before = snapshot(repository)
    try:
        (repository / gen.DECISION_PATH).write_text(json.dumps(decision))
        generated = generate(repository)
        assert generated.returncode == 1 and "Traceback" not in generated.stderr
        assert generated.stderr.split(":", 1)[0] == "SMC2_METHOD_INCOMPLETE", generated.stderr
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == ["SMC2_METHOD_INCOMPLETE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("field", ["schemaVersion", "candidate", "rows", "warmup", "repetitions", "retention",
                                  "classification", "environment", "percentile", "comparability", "signalQuality", "selfChecks"])
def test_method_fault_matrix_missing_field(repository, field):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        del bundle[field]
        write_bundle(repository, bundle)
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == ["SMC2_METHOD_INCOMPLETE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("fault", ["approval-removed", "approval-deleted", "approval-surrogate", "payload-surrogate",
                                   "approval-stale", "approval-unnamed", "approval-postdated",
                                   "payload-edited-after-approval", "history-unavailable", "history-shallow", "markdown-drift", "schema-opened",
                                   "digests-null", "digests-list", "digests-string", "digests-missing"])
def test_method_fault_matrix_protocol(repository, fault):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        expected, exit_code = ["SMC2_APPROVAL_MISSING"], 1
        if fault == "approval-removed":
            bundle["approval"] = None
            write_bundle(repository, bundle)
        elif fault == "approval-deleted":
            del bundle["approval"]
            write_bundle(repository, bundle, resign=False)
        elif fault in ("approval-surrogate", "payload-surrogate"):
            if fault == "approval-surrogate":
                bundle["approval"]["approver"] = "\ud800"
            else:
                bundle["rows"][0]["dataShape"] = "\ud800"
            (repository / gen.JSON_PATH).write_text(json.dumps(bundle, indent=2) + "\n")
            expected = ["SMC2_METHOD_INCOMPLETE"]
        elif fault == "approval-stale":
            bundle["approval"]["boundSignablePayloadSha256"] = "0" * 64
            raw, md = gen.finalize(gen.signable_of(bundle), bundle["approval"])
            (repository / gen.JSON_PATH).write_text(raw)
            (repository / gen.MARKDOWN_PATH).write_text(md)
        elif fault == "approval-unnamed":
            bundle["approval"]["approver"] = " "
            write_bundle(repository, bundle)
        elif fault == "approval-postdated":
            bundle["approval"]["decidedAtUtc"] = "2999-01-01T00:00:00Z"
            write_bundle(repository, bundle)
            expected = ["SMC2_APPROVAL_POSTDATED"]
        elif fault == "payload-edited-after-approval":
            bundle["environment"]["runtime"] = "10.0.10"
            write_bundle(repository, bundle, resign=False)
            expected = ["SMC2_METHOD_INCOMPLETE", "SMC2_ENVIRONMENT_UNCONTROLLED", "SMC2_APPROVAL_MISSING"]
        elif fault == "history-unavailable":
            (repository / ".git/objects/info/alternates").write_text("")
            expected, exit_code = ["HISTORY_UNAVAILABLE"], 2
        elif fault == "history-shallow":
            (repository / ".git/shallow").write_text(gen.STORY_COMMIT + "\n")
            expected, exit_code = ["HISTORY_UNAVAILABLE"], 2
        elif fault == "markdown-drift":
            (repository / gen.MARKDOWN_PATH).write_bytes(before[gen.MARKDOWN_PATH] + b"drift\n")
            expected = ["SMC2_METHOD_INCOMPLETE"]
        elif fault == "schema-opened":
            schema = json.loads(before[gen.SCHEMA_PATH])
            schema["additionalProperties"] = True
            (repository / gen.SCHEMA_PATH).write_text(json.dumps(schema))
            expected = ["SMC2_METHOD_INCOMPLETE"]
        elif fault.startswith("digests-"):
            value = {"digests-null": None, "digests-list": [], "digests-string": "bad"}.get(fault)
            if fault == "digests-missing":
                del bundle["digests"]
            else:
                bundle["digests"] = value
            write_bundle(repository, bundle, resign=False)
            expected = ["SMC2_METHOD_INCOMPLETE", "SMC2_APPROVAL_MISSING"]
        completed, report = verify(repository)
        assert completed.returncode == exit_code, report
        assert [item["code"] for item in report["blockers"]] == expected, (FAULT_NOT_DETECTED, report)
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def test_method_fault_matrix_complete_pair_and_outputs(repository):
    before = snapshot(repository)
    for _ in range(2):
        completed = generate(repository)
        assert completed.returncode == 0, completed.stderr
        assert snapshot(repository) == before, "regeneration must be byte-identical"
    completed, report = verify(repository)
    assert completed.returncode == 0 and report["result"] == "PASS", report
    denied = run("verify_sm_c2_method.py", repository, "--evidence", gen.JSON_PATH, "--markdown", gen.MARKDOWN_PATH,
                 "--output", gen.POST_PATH)
    assert denied.returncode == 2 and "OUTPUT_WRITE_FAILED:" in denied.stderr
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def test_method_fault_matrix_generator_approval_digest(repository):
    before = snapshot(repository)
    result = generate(repository, "--approve-reviewer", "Test Owner", "--decided-at-utc", "2000-01-01T00:00:00Z",
                      "--approved-payload-sha256", "0" * 64)
    assert result.returncode == 1
    assert [line.split(":", 1)[0] for line in result.stderr.splitlines()] == ["SMC2_APPROVAL_MISSING"], result.stderr
    completed, report = verify(repository)
    assert completed.returncode == 0 and report["assertionLedger"], report
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("field,value,expected", [
    ("rows.0.threshold.requiresUsableSignal", 1, "SMC2_THRESHOLD_MUTATED"),
    ("retention.retainAllRawSamples", 1, "SMC2_SAMPLE_DISCARDED"),
    ("retention.sampleDeletion", 0, "SMC2_SAMPLE_DISCARDED"),
    ("repetitions.adaptive", 0, "SMC2_SAMPLE_DISCARDED"),
    ("environment.concurrency", True, "SMC2_ENVIRONMENT_UNCONTROLLED"),
    ("environment.host.sameHostForBothSides", 1, "SMC2_ENVIRONMENT_UNCONTROLLED"),
    ("percentile.interpolation", 0, "SMC2_SIGNAL_RULE_MUTATED"),
])
def test_method_fault_matrix_semantic_types(repository, field, value, expected):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        replace(field, value)(bundle)
        write_bundle(repository, bundle)
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == [expected], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("key,value", [("gate", "recorded-only"), ("gate", "recorded-not-gated"),
                                      ("gate", "approved-cost-ceiling"), ("gate", "disclosure-only"),
                                      ("result", "recorded"), ("gate", "owner-approval")])
def test_method_fault_matrix_legacy_gate(repository, key, value):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        bundle["rows"][0][key] = value
        write_bundle(repository, bundle)
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == ["SMC2_THRESHOLD_MUTATED"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("after_publication", [False, True], ids=["before-publication", "after-publication-before-now"])
def test_method_fault_matrix_publication_history(repository, after_publication):
    before = snapshot(repository)
    try:
        # Reuse an existing validated publication object. No fixture commit or commit message is created.
        subprocess.run(["git", "-C", str(repository), "update-ref", "refs/heads/sm-c2-publication", PUBLICATION_COMMIT],
                       check=True, capture_output=True)
        stamp = subprocess.run(["git", "-C", str(repository), "show", "-s", "--format=%cI", PUBLICATION_COMMIT],
                               check=True, capture_output=True, text=True).stdout.strip()
        publication = datetime.fromisoformat(stamp).astimezone(timezone.utc)
        decided = publication + timedelta(seconds=1 if after_publication else -1)
        assert decided < datetime.now(timezone.utc), "The postdated fault must precede verification time."
        bundle = json.loads(before[gen.JSON_PATH])
        bundle["approval"]["decidedAtUtc"] = decided.strftime("%Y-%m-%dT%H:%M:%SZ")
        write_bundle(repository, bundle)
        generated = generate(repository)
        completed, report = verify(repository)
        expected = ["SMC2_APPROVAL_POSTDATED"] if after_publication else []
        assert generated.returncode == completed.returncode == (1 if after_publication else 0), (generated.stderr, report)
        assert [line.split(":", 1)[0] for line in generated.stderr.splitlines()] == expected, generated.stderr
        assert [item["code"] for item in report["blockers"]] == expected, report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("raw", [b"unrelated existing untracked text\n", b'{"schemaVersion":"foreign.report.v1"}\n'],
                         ids=["untracked-text", "foreign-schema-json"])
def test_method_fault_matrix_foreign_output(repository, raw):
    before = snapshot(repository)
    target = repository / "out/foreign-output"
    assert not target.exists()
    try:
        completed, report = verify(repository)
        assert completed.returncode == 0 and report["assertionLedger"], report
        target.write_bytes(raw)
        denied = run("verify_sm_c2_method.py", repository, "--evidence", gen.JSON_PATH,
                     "--markdown", gen.MARKDOWN_PATH, "--output", "out/foreign-output")
        assert denied.returncode == 2 and denied.stdout.strip() == "BLOCKED"
        assert [line.split(":", 1)[0] for line in denied.stderr.splitlines()] == ["OUTPUT_WRITE_FAILED"], denied.stderr
        assert target.read_bytes() == raw, FIXTURE_NOT_RESTORED
    finally:
        target.unlink(missing_ok=True)
        restore(repository, before)
    assert not target.exists() and snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("path", [gen.STORY_SPEC, gen.SPRINT_PATH], ids=["predecessor-spec", "live-sprint"])
def test_method_fault_matrix_non_utf8_predecessor(repository, path):
    before = snapshot(repository)
    try:
        (repository / path).write_bytes(before[path] + b"\xff")
        generated = generate(repository)
        assert generated.returncode == 2 and generated.stdout.strip() == "BLOCKED", generated.stderr
        assert generated.stderr.split(":", 1)[0] == "INPUT_UNAVAILABLE", generated.stderr
        assert "Traceback" not in generated.stderr
        completed, report = verify(repository)
        assert completed.returncode == 2 and report["result"] == "BLOCKED", report
        assert [item["code"] for item in report["blockers"]] == ["INPUT_UNAVAILABLE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def test_method_fault_matrix_repository_approved_pair(repository):
    before = snapshot(repository)
    try:
        # Exercise the actual repository approval and Markdown, rather than a generated test-owner pair.
        for path in (gen.JSON_PATH, gen.MARKDOWN_PATH):
            shutil.copyfile(ROOT / path, repository / path)
        subprocess.run(["git", "-C", str(repository), "update-ref", "refs/heads/sm-c2-publication", PUBLICATION_COMMIT],
                       check=True, capture_output=True)
        completed, report = verify(repository)
        assert completed.returncode == 0 and report["result"] == "PASS" and report["requireApproval"], report
        assert all(row["state"] == "PASS" for row in report["assertionLedger"]), report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("fault", ["live-sprint", "authority-digest", "inventory-digest",
                                   "planning-history-replaced", "fixture-history-replaced"])
def test_method_fault_matrix_source_bindings(repository, fault):
    before = snapshot(repository)
    try:
        if fault == "live-sprint":
            raw = before[gen.SPRINT_PATH]
            changed = raw.replace(f"  {gen.SPRINT_KEY}: done\n".encode(), f"  {gen.SPRINT_KEY}: review\n".encode())
            assert changed != raw
            (repository / gen.SPRINT_PATH).write_bytes(changed)
        elif fault == "authority-digest":
            authority = json.loads(before[gen.AUTHORITY_PATH])
            rows = [row for row in authority["artifacts"] if row.get("path") == gen.CONTRACT_PATH]
            assert len(rows) == 1
            rows[0]["sha256"] = "0" * 64
            (repository / gen.AUTHORITY_PATH).write_text(json.dumps(authority))
        elif fault == "inventory-digest":
            epics, count = re.subn(
                r"(\*\*Frozen inventory:\*\* `V9-12\.2-ENTRY-v1` contains, in order,\s+.*?;\s+SHA-256\s+`)[0-9a-f]{64}(`)",
                lambda match: match.group(1) + "0" * 64 + match.group(2),
                before[gen.EPICS_PATH].decode(), count=1, flags=re.S)
            assert count == 1
            (repository / gen.EPICS_PATH).write_text(epics)
        else:
            # Existing commit objects suffice to replace history; never create a fixture commit or message.
            original, replacement = (gen.PLANNING_COMMIT, "29def441408becfbbbdc5c59b9af14a7717cb21f") \
                if fault == "planning-history-replaced" else (gen.FIXTURE_BASIS_COMMIT, gen.PLANNING_COMMIT)
            subprocess.run(["git", "-C", str(repository), "replace", original, replacement], check=True, capture_output=True)
        generated = generate(repository)
        assert generated.returncode == 1 and generated.stdout.strip() == "FAIL", generated.stderr
        assert [line.split(":", 1)[0] for line in generated.stderr.splitlines()] == ["SMC2_METHOD_INCOMPLETE"], generated.stderr
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == ["SMC2_METHOD_INCOMPLETE"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("fault,failed_assertion", [("json-formatting", "json:canonical"),
                                                   ("markdown-digest", "markdown:parity")])
def test_method_fault_matrix_serialization_guards(repository, fault, failed_assertion):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        if fault == "json-formatting":
            (repository / gen.JSON_PATH).write_text(json.dumps(bundle))
        else:
            # Markdown already equals its rendering; only the independently recorded digest is wrong.
            bundle["digests"]["markdownSha256"] = "0" * 64
            write_bundle(repository, bundle, resign=False)
        completed, report = verify(repository)
        assert completed.returncode == 1
        assert [item["code"] for item in report["blockers"]] == ["SMC2_METHOD_INCOMPLETE"], report
        assert [row["id"] for row in report["assertionLedger"] if row["state"] != "PASS"] == [failed_assertion], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("flag", ["--contract", "--baseline", "--output-json", "--output-markdown"])
def test_method_fault_matrix_generator_argument_paths(repository, flag):
    before = snapshot(repository)
    generated = generate(repository, flag, "out/uncontracted-path")
    assert generated.returncode == 2 and generated.stdout.strip() == "BLOCKED", generated.stderr
    assert [line.split(":", 1)[0] for line in generated.stderr.splitlines()] == ["ARGUMENT_INVALID"], generated.stderr
    assert not (repository / "out/uncontracted-path").exists()
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("flag", ["--evidence", "--markdown"])
def test_method_fault_matrix_verifier_argument_paths(repository, flag):
    before = snapshot(repository)
    completed, report = verify(repository, flag, "out/uncontracted-path")
    assert completed.returncode == 2 and report["result"] == "BLOCKED", report
    assert [item["code"] for item in report["blockers"]] == ["ARGUMENT_INVALID"], report
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


@pytest.mark.parametrize("approval,expected", [("missing", []), ("valid", []),
                                             ("stale", ["SMC2_APPROVAL_MISSING"]),
                                             ("unnamed", ["SMC2_APPROVAL_MISSING"]),
                                             ("postdated", ["SMC2_APPROVAL_POSTDATED"])])
def test_method_fault_matrix_draft_approval(repository, approval, expected):
    before = snapshot(repository)
    try:
        bundle = json.loads(before[gen.JSON_PATH])
        if approval == "missing":
            bundle["approval"] = None
        elif approval == "stale":
            bundle["approval"]["boundSignablePayloadSha256"] = "0" * 64
        elif approval == "unnamed":
            bundle["approval"]["approver"] = " "
        elif approval == "postdated":
            bundle["approval"]["decidedAtUtc"] = "2999-01-01T00:00:00Z"
        # Preserve the selected approval fault, while keeping payload digests and rendering valid.
        raw, markdown = gen.finalize(gen.signable_of(bundle), bundle["approval"])
        (repository / gen.JSON_PATH).write_text(raw)
        (repository / gen.MARKDOWN_PATH).write_text(markdown)
        completed, report = verify(repository, "--allow-unapproved")
        assert completed.returncode == (1 if expected else 0) and not report["requireApproval"], report
        assert [item["code"] for item in report["blockers"]] == expected, report
        assert any(row["id"] == "approval:bound" for row in report["assertionLedger"]) == (approval != "missing"), report
        if approval == "missing":
            completed, report = verify(repository)
            assert completed.returncode == 1 and report["requireApproval"], report
            assert [item["code"] for item in report["blockers"]] == ["SMC2_APPROVAL_MISSING"], report
    finally:
        restore(repository, before)
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED


def test_method_fault_matrix_report_must_be_fresh(repository, monkeypatch):
    before = snapshot(repository)
    completed, report = verify(repository)
    assert completed.returncode == 0 and report["result"] == "PASS"
    monkeypatch.setattr(sys.modules[__name__], "run",
                        lambda *args: subprocess.CompletedProcess([], 1, stdout="", stderr="no report produced"))
    with pytest.raises(AssertionError, match="Verifier did not write a fresh report"):
        verify(repository)
    assert not (repository / RESULT).exists()
    assert snapshot(repository) == before, FIXTURE_NOT_RESTORED
