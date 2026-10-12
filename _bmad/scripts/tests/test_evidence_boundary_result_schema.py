"""The closed Epic 10 result contract, including procedural code ordering."""

import json
from copy import deepcopy
from pathlib import Path

import jsonschema
import pytest


SCHEMA = Path(__file__).resolve().parents[2] / "schemas/evidence-boundary-result-v1.schema.json"


def validate_result(document):
    """Validate structure and the ordinal code rule JSON Schema cannot express."""
    schema = json.loads(SCHEMA.read_text(encoding="utf-8"))
    jsonschema.Draft202012Validator.check_schema(schema)
    jsonschema.Draft202012Validator(schema).validate(document)
    for name in ("blockers", "warnings"):
        if document[name] != sorted(document[name]):
            raise jsonschema.ValidationError(f"{name} must be ordinally sorted")


def valid_result():
    return {
        "schemaVersion": "hexalith.conversations.evidence-boundary-result.v1",
        "result": "PASS", "candidate": "a" * 40,
        "manifest": [{"path": "proof.json", "sha256": "b" * 64, "role": "proof"}],
        "changedPaths": ["proof.json"], "gitlinks": [],
        "assertionLedger": [{"id": "hash", "subject": "proof.json", "state": "PASS"}],
        "warnings": [], "blockers": [],
    }


def test_evidence_boundary_result_schema_accepts_all_result_states():
    for state in ("PASS", "FAIL", "BLOCKED", "not-applicable"):
        document = valid_result()
        document["result"] = state
        validate_result(document)


@pytest.mark.parametrize("fault", ["missing", "extra", "unknown-result", "unknown-ledger", "duplicate-code", "unordered-code"])
def test_evidence_boundary_result_schema_rejects_invalid_contract(fault):
    document = deepcopy(valid_result())
    if fault == "missing":
        del document["manifest"]
    elif fault == "extra":
        document["extra"] = True
    elif fault == "unknown-result":
        document["result"] = "SUCCESS"
    elif fault == "unknown-ledger":
        document["assertionLedger"][0]["state"] = "SUCCESS"
    elif fault == "duplicate-code":
        document["result"] = "FAIL"
        document["blockers"] = ["EVIDENCE_HASH_MISMATCH"] * 2
    else:
        document["warnings"] = ["Z_CODE", "A_CODE"]
    with pytest.raises(jsonschema.ValidationError):
        validate_result(document)


@pytest.mark.parametrize("fault", ["empty-ledger", "failed-ledger", "skipped-ledger", "blocked-ledger", "blocker", "null-candidate", "gitlink"])
def test_evidence_boundary_result_pass_requires_evaluated_passing_assertions(fault):
    document = valid_result()
    if fault == "empty-ledger":
        document["assertionLedger"] = []
    elif fault == "blocker":
        document["blockers"] = ["EVIDENCE_HASH_MISMATCH"]
    elif fault == "null-candidate":
        document["candidate"] = None
    elif fault == "gitlink":
        document["gitlinks"] = [{"path": "deps/module", "sourceMode": "000000", "destinationMode": "160000"}]
    else:
        document["assertionLedger"][0]["state"] = {
            "failed-ledger": "FAIL", "skipped-ledger": "SKIPPED", "blocked-ledger": "BLOCKED"}[fault]
    with pytest.raises(jsonschema.ValidationError):
        validate_result(document)
    for state in ("FAIL", "BLOCKED", "not-applicable"):
        document["result"] = state
        validate_result(document)


@pytest.mark.parametrize("field", ["candidate", "hash", "code", "path", "role", "changed-path", "gitlink-path"])
def test_evidence_boundary_result_schema_rejects_trailing_newline(field):
    document = valid_result()
    if field == "candidate":
        document["candidate"] += "\n"
    elif field == "code":
        document["warnings"] = ["CODE\n"]
    elif field == "changed-path":
        document["changedPaths"][0] += "\n"
    elif field == "gitlink-path":
        document["gitlinks"] = [{"path": "deps/module\n", "sourceMode": "000000", "destinationMode": "160000"}]
    else:
        document["manifest"][0]["sha256" if field == "hash" else field] += "\n"
    with pytest.raises(jsonschema.ValidationError):
        validate_result(document)
