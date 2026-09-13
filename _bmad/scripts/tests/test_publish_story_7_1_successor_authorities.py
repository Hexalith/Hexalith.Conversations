"""Schema, transaction, and mutation tests for the V19/V20 successor authorities."""

from __future__ import annotations

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
from typing import Any, Callable

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_story_7_1_successor_authorities.py"
SPEC = importlib.util.spec_from_file_location("publish_story_7_1_successor_authorities", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)

AUTHORIZED_FILES = (
    publisher.V19_SCHEMA_PATH,
    publisher.V20_SCHEMA_PATH,
    publisher.INVENTORY_SCHEMA_PATH,
    publisher.INVENTORY_PATH,
    publisher.PUBLISHER_PATH,
    publisher.PUBLISHER_TEST_PATH,
)
OWNER_IDENTITY = "release-owner-fixture-2026-09-12"
DECIDED_AT_UTC = "2026-09-12T12:00:00Z"
RATIONALE = (
    "Release-owner decision binds V19-STORY-7.1-CHECKPOINT-COMPLETION "
    "and V20-STORY-7.1-INPUT-INVENTORY-v1 after independent review."
)


def git(root: Path, *arguments: str) -> str:
    """Run Git in a hermetic fixture and return trimmed stdout."""

    return subprocess.check_output(["git", "-C", str(root), *arguments], text=True).strip()


def commit(root: Path, message: str, *, allow_empty: bool = False) -> str:
    """Commit a fixture index and return the exact commit identity."""

    arguments = ["git", "-C", str(root), "commit", "-q", "-m", message]
    if allow_empty:
        arguments.insert(4, "--allow-empty")
    subprocess.run(arguments, check=True)
    return git(root, "rev-parse", "HEAD")


def clone_with_tooling(tmp_path: Path) -> tuple[Path, str]:
    """Clone repository history and commit only tooling/input files needed by future transactions."""

    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.name", "Successor authority fixture"], check=True)
    subprocess.run(
        ["git", "-C", str(root), "config", "user.email", "successor-authority@example.invalid"], check=True
    )
    for relative_path in (*AUTHORIZED_FILES, publisher.SEMANTIC_SOURCE_PATH):
        source = ROOT / relative_path
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    subprocess.run(
        ["git", "-C", str(root), "add", "-f", "--", *AUTHORIZED_FILES, publisher.SEMANTIC_SOURCE_PATH],
        check=True,
    )
    tooling = commit(root, "build(planning): add successor authority fixture tooling")
    return root, tooling


def junit_xml(*, tests: int = 2, failures: int = 0, errors: int = 0, skipped: int = 0) -> str:
    """Render a small deterministic JUnit snapshot."""

    cases = []
    for index in range(tests):
        child = ""
        if index < failures:
            child = '<failure message="failure" />'
        elif index < failures + errors:
            child = '<error message="error" />'
        elif index < failures + errors + skipped:
            child = '<skipped message="skipped" />'
        cases.append(f'<testcase classname="checkpoint.Schema" name="Case{index + 1}">{child}</testcase>')
    return (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        '<testsuites name="pytest">\n'
        f'  <testsuite name="pytest" tests="{tests}" failures="{failures}" errors="{errors}" skipped="{skipped}">\n'
        f"    {''.join(cases)}\n"
        "  </testsuite>\n"
        "</testsuites>\n"
    )


def stage_checkpoint(
    root: Path,
    *,
    xml: str | None = None,
    omit_result: bool = False,
    extra_path: str | None = None,
    gitlink_substitution: bool = False,
) -> str:
    """Create one fresh checkpoint candidate, with optional single-fault mutations."""

    for relative_path in publisher.CHECKPOINT_PATHS[:-2]:
        path = root / relative_path
        document = json.loads(path.read_text(encoding="utf-8"))
        document["$comment"] = f"substantive V19 fixture change for {relative_path}"
        path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    test_path = root / publisher.CHECKPOINT_PATHS[-2]
    test_path.write_text(
        test_path.read_text(encoding="utf-8") + "\n# Substantive V19 fixture checkpoint assertion.\n",
        encoding="utf-8",
    )
    result_path = root / publisher.CHECKPOINT_RESULT_PATH
    if not omit_result:
        result_path.parent.mkdir(parents=True, exist_ok=True)
        result_path.write_text(xml or junit_xml(), encoding="utf-8")
    paths = list(publisher.CHECKPOINT_PATHS[:-1])
    if not omit_result:
        paths.append(publisher.CHECKPOINT_RESULT_PATH)
    if extra_path is not None:
        extra = root / extra_path
        extra.write_text("unexpected checkpoint path\n", encoding="utf-8")
        paths.append(extra_path)
    subprocess.run(["git", "-C", str(root), "add", "-f", "--", *paths], check=True)
    if gitlink_substitution:
        subprocess.run(
            [
                "git",
                "-C",
                str(root),
                "update-index",
                "--cacheinfo",
                "160000,1111111111111111111111111111111111111111,references/Hexalith.Builds",
            ],
            check=True,
        )
    candidate = commit(root, "test: create fresh checkpoint candidate")
    if omit_result:
        result_path.parent.mkdir(parents=True, exist_ok=True)
        result_path.write_text(xml or junit_xml(), encoding="utf-8")
    return candidate


def stage_v19(root: Path, candidate: str) -> str:
    """Publish and commit one exact-path V19 transaction."""

    before = git(root, "status", "--porcelain=v1", "--untracked-files=all")
    assert before in ("", f"?? {publisher.CHECKPOINT_RESULT_PATH}")
    publisher.publish_v19(root, candidate_revision=candidate, check=False)
    status = git(root, "status", "--porcelain=v1", "--untracked-files=all")
    expected = f"?? {publisher.V19_PATH}"
    if before:
        expected = f"?? {publisher.V19_PATH}\n{before}"
    assert set(status.splitlines()) == set(expected.splitlines())
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V19_PATH], check=True)
    return commit(root, "build(planning): bind V19 fixture authority")


def stage_entry(root: Path) -> str:
    """Create a distinct committed entry root after V19 without Story 7.1 changes."""

    return commit(root, "build(planning): select Story 7.1 entry fixture", allow_empty=True)


def stage_v20(root: Path, entry: str) -> str:
    """Publish and commit one exact-one-path V20 transaction."""

    assert git(root, "status", "--porcelain=v1", "--untracked-files=all") == ""
    publisher.publish_v20(
        root,
        entry_revision=entry,
        owner_identity=OWNER_IDENTITY,
        decided_at_utc=DECIDED_AT_UTC,
        rationale=RATIONALE,
        check=False,
    )
    assert git(root, "status", "--porcelain=v1", "--untracked-files=all") == f"?? {publisher.V20_PATH}"
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    return commit(root, "build(planning): bind V20 fixture authority")


def full_transaction(tmp_path: Path) -> dict[str, Any]:
    """Create the future tooling/checkpoint/V19/entry/V20 transaction chain."""

    root, tooling = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    v19 = stage_v19(root, candidate)
    entry = stage_entry(root)
    v20 = stage_v20(root, entry)
    return {"root": root, "tooling": tooling, "candidate": candidate, "v19": v19, "entry": entry, "v20": v20}


def object_schema_nodes(value: object) -> list[dict[str, Any]]:
    """Walk all explicit object-typed schema nodes."""

    nodes: list[dict[str, Any]] = []
    if isinstance(value, dict):
        if value.get("type") == "object":
            nodes.append(value)
        for child in value.values():
            nodes.extend(object_schema_nodes(child))
    elif isinstance(value, list):
        for child in value:
            nodes.extend(object_schema_nodes(child))
    return nodes


def assert_recursively_closed(schema: dict[str, Any]) -> None:
    """Require every explicit object schema to reject additional fields."""

    for node in object_schema_nodes(schema):
        assert node.get("additionalProperties") is False
        assert set(node.get("required", ())) <= set(node.get("properties", ()))


def assert_error(code: str, operation: Callable[[], object], *, state: str | None = None) -> None:
    """Assert one stable fail-closed result."""

    with pytest.raises(publisher.SuccessorAuthorityError) as error:
        operation()
    assert error.value.code == code
    if state is not None:
        assert error.value.state == state
    result = publisher.failure_document(ROOT, "v20" if code.startswith("V20") else "v19", error.value)
    assert result["assertionLedger"]
    assert result["result"] == error.value.state


def test_schemas_are_draft_2020_12_recursively_closed_and_inventory_is_exact() -> None:
    """Cover the inventory matrix row and every recursively closed schema identity."""

    schema_paths = (publisher.V19_SCHEMA_PATH, publisher.V20_SCHEMA_PATH, publisher.INVENTORY_SCHEMA_PATH)
    for relative_path in schema_paths:
        schema = json.loads((ROOT / relative_path).read_text(encoding="utf-8"))
        jsonschema.Draft202012Validator.check_schema(schema)
        assert_recursively_closed(schema)
    inventory = publisher.inventory_route(ROOT, check=True)
    assert publisher.PRESERVED_EVIDENCE[0]["identity"] == "V17-IMPLEMENTATION-HOLD-DECISION"
    assert inventory == publisher.expected_inventory_document()
    assert [row["sha256"] for row in inventory["pathInventories"]] == [
        "4405332b49ec26ffff40c9b7424c858e7634e0d92313b2c0270011e29a132f4b",
        "f54595279fc201056604e56971f2e22f5483c505ddb1a2488b69e513f08b0fef",
        "80b1c47320b8f4ebb98e0dfb40d6777cded1e4e08541c7734678991ab5598b64",
    ]
    assert [len(row["paths"]) for row in inventory["scenarioInventories"]] == [12, 13, 13, 13, 14, 20]
    for row in inventory["scenarioInventories"]:
        assert [binding["path"] for binding in row["orderedInputBindings"]] == row["paths"]
        assert all(set(binding) == {"path", "sha256", "mode", "role"} for binding in row["orderedInputBindings"])


def test_future_v19_and_v20_transactions_pass_with_distinct_roles_and_narrow_effects(tmp_path: Path) -> None:
    """Cover valid V19/V20 publication and effective-hold matrix rows."""

    transaction = full_transaction(tmp_path)
    root = transaction["root"]
    v19 = publisher.publish_v19(root, candidate_revision=transaction["v20"], check=True)
    assert v19["historicalTransaction"]["result"] == "NONCONFORMING"
    assert v19["historicalTransaction"]["blockers"] == ["CHANGED_PATH_SET_MISMATCH"]
    assert v19["freshCheckpoint"]["machineResult"]["tests"] == 2
    assert [row["path"] for row in v19["freshCheckpoint"]["changedPathBindings"]] == list(
        publisher.CHECKPOINT_PATHS
    )
    assert all(row["mode"] == "100644" for row in v19["freshCheckpoint"]["changedPathBindings"])
    assert v19["freshCheckpoint"]["assertionLedger"]
    assert all(row["state"] == "PASS" for row in v19["freshCheckpoint"]["assertionLedger"])
    assert [row["path"] for row in v19["freshCheckpoint"]["rootGitlinks"]] == list(
        publisher.ROOT_GITLINK_PATHS
    )
    assert all(row["mode"] == "160000" for row in v19["freshCheckpoint"]["rootGitlinks"])
    assert v19["authorityEffect"]["implementationHold"] == "ACTIVE"

    v20 = publisher.publish_v20(
        root,
        entry_revision=transaction["v20"],
        owner_identity=None,
        decided_at_utc=None,
        rationale=None,
        check=True,
    )
    roles = v20["candidateRoles"]
    assert len({roles["planningCandidate"], roles["checkpointCandidate"], roles["entryCandidate"]}) == 3
    assert roles["eventualStoryCandidate"] == "SC-7.1"
    assert v20["scope"]["unlocks"] == ["7.1"]
    assert v20["authorityEffect"] == {
        "implementationHold": "LIFTED",
        "fullStoryExecutionAllowed": True,
        "storyDoneAllowedWithoutAcceptance": False,
        "story71CandidateBindingResolved": True,
        "story72Unlocked": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }
    assert publisher.effective_hold(root, evaluated_revision=transaction["v20"])["effectiveHold"] == "LIFTED"


@pytest.mark.parametrize("extra_path", (None, "unexpected-checkpoint.txt"))
def test_checkpoint_path_omission_and_addition_fail_with_exact_set_code(tmp_path: Path, extra_path: str | None) -> None:
    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, omit_result=extra_path is None, extra_path=extra_path)
    assert_error(
        "V19_CHECKPOINT_PATH_SET_MISMATCH",
        lambda: publisher.render_v19(root, candidate),
    )


def test_raw_gitlink_substitution_fails_before_path_shape_can_hide_it(tmp_path: Path) -> None:
    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, gitlink_substitution=True)
    assert_error("V19_CHECKPOINT_GITLINK_DRIFT", lambda: publisher.render_v19(root, candidate))


def test_committed_xml_is_authoritative_and_uncommitted_ignored_substitution_cannot_pass(tmp_path: Path) -> None:
    valid_root, _ = clone_with_tooling(tmp_path / "valid")
    candidate = stage_checkpoint(valid_root)
    committed = publisher.render_v19(valid_root, candidate)
    result_path = valid_root / publisher.CHECKPOINT_RESULT_PATH
    result_path.write_text(junit_xml(tests=1, failures=1), encoding="utf-8")
    repeated = publisher.render_v19(valid_root, candidate)
    assert repeated == committed
    assert repeated["freshCheckpoint"]["machineResult"]["tests"] == 2

    invalid_root, _ = clone_with_tooling(tmp_path / "ignored")
    invalid_candidate = stage_checkpoint(invalid_root, omit_result=True)
    assert (invalid_root / publisher.CHECKPOINT_RESULT_PATH).is_file()
    assert_error(
        "V19_CHECKPOINT_PATH_SET_MISMATCH",
        lambda: publisher.render_v19(invalid_root, invalid_candidate),
    )


@pytest.mark.parametrize(
    ("xml", "code", "state"),
    (
        (junit_xml(tests=0), "V19_RESULT_LEDGER_EMPTY", "BLOCKED"),
        (junit_xml(tests=1, skipped=1), "V19_RESULT_NOT_PASS", "FAIL"),
    ),
)
def test_empty_or_skipped_junit_never_passes_vacuously(tmp_path: Path, xml: str, code: str, state: str) -> None:
    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, xml=xml)
    assert_error(code, lambda: publisher.render_v19(root, candidate), state=state)


def test_candidate_and_descendant_authority_drift_leave_effective_hold_active(tmp_path: Path) -> None:
    transaction = full_transaction(tmp_path)
    root = transaction["root"]
    assert_error(
        "V19_CHECKPOINT_PATH_SET_MISMATCH",
        lambda: publisher.render_v19(root, transaction["v19"]),
    )
    authority_path = root / publisher.V20_PATH
    authority = json.loads(authority_path.read_text(encoding="utf-8"))
    authority["ownerDecision"]["rationale"] += " stale descendant"
    authority_path.write_text(json.dumps(authority, indent=2) + "\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    descendant = commit(root, "test: drift V20 descendant authority")
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["result"] == "FAIL"
    assert hold["assertionLedger"]
    assert hold["blockers"][0]["code"] == "V20_AUTHORITY_DESCENDANT_DRIFT"


def test_v19_v20_scope_conflation_and_widened_unlock_are_schema_invalid(tmp_path: Path) -> None:
    transaction = full_transaction(tmp_path)
    root = transaction["root"]
    v19 = publisher.publish_v19(root, candidate_revision=transaction["v20"], check=True)
    conflated_v19 = deepcopy(v19)
    conflated_v19["authorityEffect"]["implementationHold"] = "LIFTED"
    assert_error(
        "V19_SCHEMA_INVALID",
        lambda: publisher.validate_json_schema(
            publisher.schema_at(root, transaction["candidate"], publisher.V19_SCHEMA_PATH, "V19_SCHEMA_MISSING"),
            conflated_v19,
            "V19_SCHEMA_INVALID",
        ),
    )
    v20 = publisher.publish_v20(
        root,
        entry_revision=transaction["v20"],
        owner_identity=None,
        decided_at_utc=None,
        rationale=None,
        check=True,
    )
    for mutation in (
        lambda document: document["scope"].__setitem__("unlocks", ["7.1", "7.2"]),
        lambda document: document["candidateRoles"].__setitem__("eventualStoryCandidate", document["candidateRoles"]["entryCandidate"]),
    ):
        changed = deepcopy(v20)
        mutation(changed)
        assert_error(
            "V20_SCHEMA_INVALID",
            lambda changed=changed: publisher.validate_json_schema(
                publisher.schema_at(root, transaction["entry"], publisher.V20_SCHEMA_PATH, "V20_SCHEMA_MISSING"),
                changed,
                "V20_SCHEMA_INVALID",
            ),
        )


def test_planning_checkpoint_and_entry_candidate_conflation_is_rejected(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    transaction = full_transaction(tmp_path)
    root = transaction["root"]
    valid_v19 = publisher.publish_v19(root, candidate_revision=transaction["v20"], check=True)
    v19_bytes = (root / publisher.V19_PATH).read_bytes()
    for conflated_checkpoint in (publisher.PLANNING_CANDIDATE, transaction["entry"]):
        conflated = deepcopy(valid_v19)
        conflated["freshCheckpoint"]["candidateCommit"] = conflated_checkpoint
        monkeypatch.setattr(
            publisher,
            "check_v19_at",
            lambda _root, _entry, document=conflated: (document, transaction["v19"], v19_bytes),
        )
        assert_error(
            "V20_CANDIDATE_ROLE_CONFLATION",
            lambda: publisher.render_v20(
                root,
                entry_revision=transaction["entry"],
                owner_identity=OWNER_IDENTITY,
                decided_at_utc=DECIDED_AT_UTC,
                rationale=RATIONALE,
            ),
        )


def test_semantic_source_drift_and_missing_v20_are_fail_closed_active(tmp_path: Path) -> None:
    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    v19 = stage_v19(root, candidate)
    missing = publisher.effective_hold(root, evaluated_revision=v19)
    assert missing["effectiveHold"] == "ACTIVE"
    assert missing["result"] == "BLOCKED"
    assert missing["blockers"][0]["code"] == "V20_PUBLICATION_MISSING"

    semantic = root / publisher.SEMANTIC_SOURCE_PATH
    semantic.write_bytes(semantic.read_bytes() + b"\nsemantic drift\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.SEMANTIC_SOURCE_PATH], check=True)
    entry = commit(root, "test: drift semantic source at entry")
    assert_error(
        "V20_SEMANTIC_SOURCE_DRIFT",
        lambda: publisher.render_v20(
            root,
            entry_revision=entry,
            owner_identity=OWNER_IDENTITY,
            decided_at_utc=DECIDED_AT_UTC,
            rationale=RATIONALE,
        ),
    )


@pytest.mark.parametrize(
    ("path", "code"),
    (
        ("_bmad/schemas/story-final-record-v2.schema.json", "V20_CHECKPOINT_INPUT_DRIFT"),
        ("docs/runbooks/story-final-record-generation.md", "V20_FIXED_ENTRY_INPUT_DRIFT"),
    ),
)
def test_stale_checkpoint_or_fixed_entry_input_blocks_v20(tmp_path: Path, path: str, code: str) -> None:
    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    stage_v19(root, candidate)
    target = root / path
    target.write_bytes(target.read_bytes() + b"\nstale entry input\n")
    subprocess.run(["git", "-C", str(root), "add", "--", path], check=True)
    entry = commit(root, "test: drift a V20 entry input")
    assert_error(
        code,
        lambda: publisher.render_v20(
            root,
            entry_revision=entry,
            owner_identity=OWNER_IDENTITY,
            decided_at_utc=DECIDED_AT_UTC,
            rationale=RATIONALE,
        ),
    )


def test_inventory_self_digest_misuse_fails_and_fixture_restores_byte_identically(tmp_path: Path) -> None:
    root, _ = clone_with_tooling(tmp_path)
    path = root / publisher.INVENTORY_PATH
    before = path.read_bytes()
    try:
        document = json.loads(before)
        document["selfSha256"] = publisher.sha256(before)
        path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
        assert_error(
            "V20_INVENTORY_CONTENT_DRIFT",
            lambda: publisher.validate_inventory_document(document),
        )
    finally:
        path.write_bytes(before)
    assert path.read_bytes() == before


def test_publication_writes_only_authority_paths_and_never_sprint_or_loop_state(tmp_path: Path) -> None:
    root, _ = clone_with_tooling(tmp_path)
    resolution_path = (
        root
        / ".bmad-loop/runs/fixture/resolve/7-1-define-the-final-record-schema-and-deterministic-generator-core/resolution.json"
    )
    resolution_path.parent.mkdir(parents=True, exist_ok=True)
    resolution_path.write_bytes(b'{"fixture":true}\n')
    protected_paths = [
        "_bmad-output/implementation-artifacts/sprint-status.yaml",
        "_bmad-output/implementation-artifacts/deferred-work.md",
        str(resolution_path.relative_to(root)),
    ]
    protected_before = {path: (root / path).read_bytes() for path in protected_paths}
    candidate = stage_checkpoint(root)
    publisher.publish_v19(root, candidate_revision=candidate, check=False)
    assert set(git(root, "status", "--porcelain=v1", "--untracked-files=all").splitlines()) == {
        f"?? {publisher.V19_PATH}"
    }
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V19_PATH], check=True)
    commit(root, "build(planning): bind V19 boundary fixture")
    entry = stage_entry(root)
    publisher.publish_v20(
        root,
        entry_revision=entry,
        owner_identity=OWNER_IDENTITY,
        decided_at_utc=DECIDED_AT_UTC,
        rationale=RATIONALE,
        check=False,
    )
    assert git(root, "status", "--porcelain=v1", "--untracked-files=all") == f"?? {publisher.V20_PATH}"
    assert {path: (root / path).read_bytes() for path in protected_paths} == protected_before


if __name__ == "__main__":
    raise SystemExit(pytest.main([__file__, "-q"]))
