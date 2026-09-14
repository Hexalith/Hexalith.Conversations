"""Schema, transaction, and mutation tests for the V19/V20 successor authorities."""

from __future__ import annotations

from copy import deepcopy
import hashlib
import importlib.util
import json
import os
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
    publisher.CORRECTION_PATH,
    publisher.PUBLISHER_PATH,
    publisher.PUBLISHER_TEST_PATH,
)
DECIDED_AT_UTC = "2026-09-12T12:00:00Z"
RATIONALE = (
    "Release-owner decision binds V19-STORY-7.1-CHECKPOINT-COMPLETION "
    "and V20-STORY-7.1-INPUT-INVENTORY-v1 after independent review."
)
OWNER_IDENTITY = "Successor authority fixture <successor-authority@example.invalid>"
TOOLING_BASELINE = "2ed96eff2adfd5190854165a01df657338def26f"
PREFLIGHT_WORKFLOW = ROOT / ".github/workflows/planning-authority-preflight.yml"


def git(root: Path, *arguments: str) -> str:
    """Run Git in a hermetic fixture and return trimmed stdout."""

    return subprocess.check_output(["git", "-C", str(root), *arguments], text=True).strip()


def commit(
    root: Path,
    message: str,
    *,
    allow_empty: bool = False,
    sign: bool = False,
    committed_at: str = "2026-09-12T10:00:00Z",
) -> str:
    """Commit a fixture index and return the exact commit identity."""

    arguments = ["git", "-C", str(root), "commit", "-q", "-m", message]
    if allow_empty:
        arguments.insert(4, "--allow-empty")
    if sign:
        arguments.insert(4, "-S")
    environment = {
        **os.environ,
        "GIT_AUTHOR_DATE": committed_at,
        "GIT_COMMITTER_DATE": committed_at,
    }
    subprocess.run(arguments, check=True, env=environment)
    return git(root, "rev-parse", "HEAD")


def configure_ssh_signing(root: Path) -> None:
    """Configure one fixture-local SSH signing identity and trust file."""

    key = root / ".git/v20-fixture-signing-key"
    subprocess.run(
        ["ssh-keygen", "-q", "-t", "ed25519", "-N", "", "-C", "successor-authority@example.invalid", "-f", str(key)],
        check=True,
    )
    public_key = key.with_suffix(".pub").read_text(encoding="utf-8").strip()
    allowed_signers = root / ".git/v20-fixture-allowed-signers"
    allowed_signers.write_text(
        f"successor-authority@example.invalid {public_key}\n",
        encoding="utf-8",
    )
    subprocess.run(["git", "-C", str(root), "config", "gpg.format", "ssh"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.signingkey", str(key)], check=True)
    subprocess.run(
        ["git", "-C", str(root), "config", "gpg.ssh.allowedSignersFile", str(allowed_signers)],
        check=True,
    )


def clone_with_tooling(tmp_path: Path) -> tuple[Path, str]:
    """Clone repository history and commit only tooling/input files needed by future transactions."""

    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", TOOLING_BASELINE], check=True)
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


def junit_xml(
    *,
    tests: int | None = None,
    failures: int = 0,
    errors: int = 0,
    skipped: int = 0,
    subjects: list[str] | None = None,
) -> str:
    """Render a small deterministic JUnit snapshot."""

    if subjects is None:
        count = len(publisher.REQUIRED_CHECKPOINT_SUBJECTS) if tests is None else tests
        subjects = list(publisher.REQUIRED_CHECKPOINT_SUBJECTS[:count])
        subjects.extend(
            f"{publisher.CHECKPOINT_SUBJECT_PREFIX}additional_{index}"
            for index in range(len(subjects) + 1, count + 1)
        )
    tests = len(subjects)
    cases = []
    for index, subject in enumerate(subjects):
        child = ""
        if index < failures:
            child = '<failure message="failure" />'
        elif index < failures + errors:
            child = '<error message="error" />'
        elif index < failures + errors + skipped:
            child = '<skipped message="skipped" />'
        classname, separator, name = subject.partition("::")
        assert separator == "::"
        cases.append(f'<testcase classname="{classname}" name="{name}">{child}</testcase>')
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
    normalization_only_path: str | None = None,
    command_failure: bool = False,
    rerun_extra_subject: bool = False,
) -> str:
    """Create one fresh checkpoint candidate, with optional single-fault mutations."""

    for relative_path in publisher.CHECKPOINT_PATHS[:-2]:
        path = root / relative_path
        if relative_path == normalization_only_path:
            path.write_bytes(path.read_bytes().replace(b"\n", b"\r\n"))
        else:
            document = json.loads(path.read_text(encoding="utf-8"))
            document["$comment"] = f"substantive V19 fixture change for {relative_path}"
            path.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    test_path = root / publisher.CHECKPOINT_PATHS[-2]
    test_change = "\n# Substantive V19 fixture checkpoint assertion.\n"
    if command_failure:
        test_change += "\ndef test_v2_schema_contract_forced_command_failure() -> None:\n    assert False\n"
    elif rerun_extra_subject:
        test_change += "\ndef test_v2_schema_contract_additional_rerun_case() -> None:\n    assert True\n"
    test_path.write_text(test_path.read_text(encoding="utf-8") + test_change, encoding="utf-8")
    result_path = root / publisher.CHECKPOINT_RESULT_PATH
    if not omit_result:
        result_path.parent.mkdir(parents=True, exist_ok=True)
        if xml is None:
            subprocess.run(publisher.CHECKPOINT_COMMAND_ARGUMENTS, cwd=root, check=True)
        else:
            result_path.write_text(xml, encoding="utf-8")
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


def stage_entry(root: Path, *, committed_at: str = "2026-09-12T11:00:00Z") -> str:
    """Create a distinct committed entry root after V19 without Story 7.1 changes."""

    return commit(
        root,
        "build(planning): select Story 7.1 entry fixture",
        allow_empty=True,
        committed_at=committed_at,
    )


def stage_v20(
    root: Path,
    entry: str,
    *,
    decided_at_utc: str = DECIDED_AT_UTC,
    published_at: str = "2026-09-12T13:00:00Z",
) -> str:
    """Publish and commit one exact-one-path V20 transaction."""

    configure_ssh_signing(root)
    assert git(root, "status", "--porcelain=v1", "--untracked-files=all") == ""
    publisher.publish_v20(
        root,
        entry_revision=entry,
        owner_identity=OWNER_IDENTITY,
        decided_at_utc=decided_at_utc,
        rationale=RATIONALE,
        check=False,
    )
    assert git(root, "status", "--porcelain=v1", "--untracked-files=all") == f"?? {publisher.V20_PATH}"
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    return commit(root, "build(planning): bind V20 fixture authority", sign=True, committed_at=published_at)


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


def test_prepublication_correction_binds_the_first_committed_human_semantic_source() -> None:
    """Make the failed-candidate classification and corrected semantic binding explicit."""

    content = (ROOT / publisher.CORRECTION_PATH).read_bytes()
    assert publisher.sha256(content) == publisher.CORRECTION_SHA256
    text = content.decode("utf-8")
    assert "596cee6fa5ae12a7ff6e8ac35960f60863b55a27" in text
    assert "90477eb2666c2dc693192770b801664fedab385638917e6202dd9a7e09d4166d" in text
    assert publisher.SEMANTIC_SOURCE_SHA256 in text
    assert "V19 authority published from the failed candidate: `false`" in text
    assert "V20 authority published from the failed candidate: `false`" in text


def test_worktree_evidence_paths_cannot_escape_through_symlinks(tmp_path: Path) -> None:
    """Require the filesystem routes to enforce the same containment as committed-object routes."""

    root = tmp_path / "repository"
    outside = tmp_path / "outside"
    root.mkdir()
    outside.mkdir()
    (root / "evidence").symlink_to(outside, target_is_directory=True)
    assert_error(
        "SUCCESSOR_PATH_ESCAPE",
        lambda: publisher.worktree_path(root, "evidence/result.json"),
        state="BLOCKED",
    )


def test_unavailable_checkpoint_history_is_blocked_with_a_nonempty_ledger(tmp_path: Path) -> None:
    """Keep an unavailable candidate distinct from evidence failure or non-applicability."""

    root, _ = clone_with_tooling(tmp_path)
    assert_error(
        "V19_CANDIDATE_UNAVAILABLE",
        lambda: publisher.render_v19(root, "f" * 40),
        state="BLOCKED",
    )


@pytest.mark.parametrize(
    ("path", "mutation", "code", "state"),
    (
        (publisher.CORRECTION_PATH, "missing", "V19_CORRECTION_NOT_COMMITTED", "BLOCKED"),
        (publisher.CORRECTION_PATH, "mode", "V19_CORRECTION_MODE_DRIFT", "FAIL"),
        (publisher.SEMANTIC_SOURCE_PATH, "bytes", "V19_SEMANTIC_SOURCE_DRIFT", "FAIL"),
    ),
)
def test_checkpoint_requires_exact_committed_correction_and_semantic_source(
    tmp_path: Path,
    path: str,
    mutation: str,
    code: str,
    state: str,
) -> None:
    """Bind V19 to the approved correction and amended semantic source bytes."""

    root, _ = clone_with_tooling(tmp_path)
    target = root / path
    if mutation == "missing":
        subprocess.run(["git", "-C", str(root), "rm", "-q", "--", path], check=True)
    elif mutation == "mode":
        target.chmod(0o755)
        subprocess.run(["git", "-C", str(root), "add", "--", path], check=True)
    else:
        target.write_bytes(target.read_bytes() + b"\nunauthorized amendment\n")
        subprocess.run(["git", "-C", str(root), "add", "--", path], check=True)
    commit(root, "test: invalidate an approved V19 authority input")
    candidate = stage_checkpoint(root)
    assert_error(code, lambda: publisher.render_v19(root, candidate), state=state)


def test_run_git_ignores_hostile_git_environment_config_and_replacement_refs(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Resolve repository history independently of caller-provided Git overrides."""

    root, tooling = clone_with_tooling(tmp_path)
    parent = git(root, "rev-parse", f"{tooling}^")
    subprocess.run(["git", "-C", str(root), "replace", tooling, parent], check=True)
    expected_tree = git(root, "--no-replace-objects", "show", "-s", "--format=%T", tooling)
    assert git(root, "show", "-s", "--format=%T", tooling) != expected_tree
    hostile_config = tmp_path / "hostile.gitconfig"
    hostile_config.write_text("[invalid\n", encoding="utf-8")
    for key, value in {
        "GIT_DIR": str(tmp_path / "decoy.git"),
        "GIT_WORK_TREE": str(tmp_path / "decoy-worktree"),
        "GIT_INDEX_FILE": str(tmp_path / "decoy-index"),
        "GIT_OBJECT_DIRECTORY": str(tmp_path / "decoy-objects"),
        "GIT_CONFIG_GLOBAL": str(hostile_config),
        "GIT_CONFIG_SYSTEM": str(hostile_config),
        "GIT_NO_REPLACE_OBJECTS": "0",
        "GIT_CONFIG_COUNT": "1",
        "GIT_CONFIG_KEY_0": "core.repositoryformatversion",
        "GIT_CONFIG_VALUE_0": "1",
    }.items():
        monkeypatch.setenv(key, value)
    observed_tree = publisher.run_git(root, "show", "-s", "--format=%T", tooling).stdout.decode().strip()
    assert observed_tree == expected_tree


def test_explicit_publication_revision_cannot_turn_a_rewrite_into_an_addition(tmp_path: Path) -> None:
    """Keep the optional explicit revision selector inside the additive-publication boundary."""

    root, source = clone_with_tooling(tmp_path)
    inventory_path = root / publisher.INVENTORY_PATH
    inventory_path.write_bytes(inventory_path.read_bytes() + b"\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.INVENTORY_PATH], check=True)
    publication = commit(root, "test: rewrite an existing authority input")
    assert_error(
        "TEST_PUBLICATION_NOT_ADDITIVE",
        lambda: publisher.validate_publication(
            root,
            source=source,
            publication=publication,
            evaluated=publication,
            path=publisher.INVENTORY_PATH,
            prefix="TEST",
        ),
    )


def test_v19_write_refuses_to_overwrite_existing_evidence_and_preserves_bytes(tmp_path: Path) -> None:
    """Preserve an existing authority record even before it has been committed."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    authority_path = root / publisher.V19_PATH
    authority_path.parent.mkdir(parents=True, exist_ok=True)
    authority_path.write_bytes(b'{"protected":true}\n')
    before = authority_path.read_bytes()
    assert_error(
        "V19_AUTHORITY_ALREADY_EXISTS",
        lambda: publisher.publish_v19(root, candidate_revision=candidate, check=False),
    )
    assert authority_path.read_bytes() == before


def test_atomic_write_ignores_hard_linked_legacy_temp_and_preserves_linked_bytes(tmp_path: Path) -> None:
    """Never write through the predictable legacy temporary pathname."""

    root, _ = clone_with_tooling(tmp_path / "fixture")
    candidate = stage_checkpoint(root)
    outside = tmp_path / "outside.json"
    outside.write_bytes(b'{"protected":true}\n')
    temporary_path = root / f"{publisher.V19_PATH}.tmp"
    temporary_path.parent.mkdir(parents=True, exist_ok=True)
    os.link(outside, temporary_path)
    before = outside.read_bytes()
    publisher.publish_v19(root, candidate_revision=candidate, check=False)
    assert (root / publisher.V19_PATH).is_file()
    assert outside.read_bytes() == before
    assert temporary_path.read_bytes() == before


def test_future_v19_and_v20_transactions_pass_with_distinct_roles_and_narrow_effects(tmp_path: Path) -> None:
    """Cover valid V19/V20 publication and effective-hold matrix rows."""

    transaction = full_transaction(tmp_path)
    root = transaction["root"]
    before = git(root, "status", "--porcelain=v1", "--untracked-files=all")
    authority_bytes = {
        path: (root / path).read_bytes()
        for path in (publisher.V19_PATH, publisher.V20_PATH)
    }
    v19 = publisher.publish_v19(root, candidate_revision=transaction["v20"], check=True)
    assert v19["historicalTransaction"]["result"] == "NONCONFORMING"
    assert v19["historicalTransaction"]["blockers"] == ["CHANGED_PATH_SET_MISMATCH"]
    machine_result = v19["freshCheckpoint"]["machineResult"]
    assert machine_result["tests"] == len(publisher.REQUIRED_CHECKPOINT_SUBJECTS)
    assert machine_result["command"] == publisher.CHECKPOINT_COMMAND
    assert machine_result["exitCode"] == 0
    assert [row["path"] for row in v19["freshCheckpoint"]["changedPathBindings"]] == list(
        publisher.CHECKPOINT_PATHS
    )
    assert all(row["mode"] == "100644" for row in v19["freshCheckpoint"]["changedPathBindings"])
    assert v19["freshCheckpoint"]["assertionLedger"]
    assert all(row["state"] == "PASS" for row in v19["freshCheckpoint"]["assertionLedger"])
    assert set(publisher.REQUIRED_CHECKPOINT_SUBJECTS) <= {
        row["subject"] for row in v19["freshCheckpoint"]["assertionLedger"]
    }
    assert [row["path"] for row in v19["freshCheckpoint"]["rootGitlinks"]] == list(
        publisher.ROOT_GITLINK_PATHS
    )
    assert all(row["mode"] == "160000" for row in v19["freshCheckpoint"]["rootGitlinks"])
    direct_gitlinks = []
    for path in publisher.ROOT_GITLINK_PATHS:
        mode, object_type, object_id, observed_path = git(
            root,
            "ls-tree",
            transaction["candidate"],
            "--",
            path,
        ).split(maxsplit=3)
        assert object_type == "commit"
        assert observed_path == path
        direct_gitlinks.append({"path": path, "commit": object_id, "mode": mode})
    assert v19["freshCheckpoint"]["rootGitlinks"] == direct_gitlinks
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
    expected_entry_roles = (
        (publisher.V19_PATH, "checkpoint-completion-authority"),
        (publisher.V19_SCHEMA_PATH, "checkpoint-completion-authority-schema"),
        (publisher.V20_SCHEMA_PATH, "release-owner-authority-schema"),
        (publisher.INVENTORY_PATH, "frozen-input-inventory"),
        (publisher.INVENTORY_SCHEMA_PATH, "frozen-input-inventory-schema"),
        (publisher.PUBLISHER_PATH, "successor-authority-publisher"),
        (publisher.PUBLISHER_TEST_PATH, "successor-authority-tests"),
        (publisher.CORRECTION_PATH, "failed-prepublication-candidate-correction"),
        (publisher.SEMANTIC_SOURCE_PATH, "approved-semantic-source"),
    )
    direct_entry_bindings = []
    for path, role in expected_entry_roles:
        mode, object_type, _object_id, observed_path = git(
            root,
            "ls-tree",
            transaction["entry"],
            "--",
            path,
        ).split(maxsplit=3)
        assert object_type == "blob"
        assert observed_path == path
        content = subprocess.check_output(
            ["git", "-C", str(root), "show", f'{transaction["entry"]}:{path}']
        )
        direct_entry_bindings.append(
            {"path": path, "sha256": hashlib.sha256(content).hexdigest(), "mode": mode, "role": role}
        )
    assert v20["entryBindings"] == direct_entry_bindings
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
    assert git(root, "status", "--porcelain=v1", "--untracked-files=all") == before
    assert {path: (root / path).read_bytes() for path in authority_bytes} == authority_bytes

    inventory_ledger: list[dict[str, str]] = []
    inventory = publisher.inventory_route(root, check=True, assertion_ledger=inventory_ledger)
    assert inventory == publisher.expected_inventory_document()
    assert inventory_ledger
    assert all(row["state"] == "PASS" for row in inventory_ledger)
    assert any(row["id"].startswith("INVENTORY-CHECKPOINT-") for row in inventory_ledger)


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


def test_normalization_only_checkpoint_rewrite_is_not_substantive(tmp_path: Path) -> None:
    """Reject a checkpoint path whose only change is LF-to-CRLF normalization."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, normalization_only_path=publisher.CHECKPOINT_PATHS[0])
    assert_error("V19_NON_SUBSTANTIVE_CHANGE", lambda: publisher.render_v19(root, candidate))


@pytest.mark.parametrize("path", tuple(str(row["path"]) for row in publisher.PRESERVED_EVIDENCE[:3]))
def test_v17_v18_and_hold_digest_drift_have_a_stable_blocker(tmp_path: Path, path: str) -> None:
    """Keep each preserved point-in-time authority byte-bound at V19."""

    root, _ = clone_with_tooling(tmp_path)
    target = root / path
    target.write_bytes(target.read_bytes() + b"\ndrift\n")
    subprocess.run(["git", "-C", str(root), "add", "--", path], check=True)
    commit(root, "test: drift preserved point-in-time evidence")
    candidate = stage_checkpoint(root, xml=junit_xml())
    assert_error("V19_PRESERVED_EVIDENCE_DRIFT", lambda: publisher.render_v19(root, candidate))


def test_nonregular_committed_gitmodules_is_rejected_before_parsing(tmp_path: Path) -> None:
    """Require `.gitmodules` itself to be a committed regular blob."""

    root, parent = clone_with_tooling(tmp_path)
    tree_rows = subprocess.check_output(["git", "-C", str(root), "ls-tree", parent]).splitlines()
    mutated_rows = [
        b"120000" + row[6:] if row.endswith(b"\t.gitmodules") else row
        for row in tree_rows
    ]
    assert mutated_rows != tree_rows
    tree = subprocess.check_output(
        ["git", "-C", str(root), "mktree"],
        input=b"\n".join(mutated_rows) + b"\n",
    ).decode().strip()
    candidate = subprocess.check_output(
        ["git", "-C", str(root), "commit-tree", tree, "-p", parent],
        input=b"nonregular gitmodules fixture\n",
    ).decode().strip()
    assert_error("SUCCESSOR_GITMODULES_MODE_DRIFT", lambda: publisher.root_gitlinks(root, candidate))


def test_committed_xml_is_authoritative_and_uncommitted_ignored_substitution_cannot_pass(tmp_path: Path) -> None:
    valid_root, _ = clone_with_tooling(tmp_path / "valid")
    candidate = stage_checkpoint(valid_root)
    committed = publisher.render_v19(valid_root, candidate)
    result_path = valid_root / publisher.CHECKPOINT_RESULT_PATH
    result_path.write_text(junit_xml(tests=1, failures=1), encoding="utf-8")
    repeated = publisher.render_v19(valid_root, candidate)
    assert repeated == committed
    assert repeated["freshCheckpoint"]["machineResult"]["tests"] == len(
        publisher.REQUIRED_CHECKPOINT_SUBJECTS
    )

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


@pytest.mark.parametrize(
    ("xml", "code"),
    (
        (
            junit_xml(subjects=list(publisher.REQUIRED_CHECKPOINT_SUBJECTS[:-1])),
            "V19_RESULT_LEDGER_INSUFFICIENT",
        ),
        (
            junit_xml(
                subjects=[
                    *publisher.REQUIRED_CHECKPOINT_SUBJECTS,
                    "unrelated.module::test_v2_schema_contract_unrelated",
                ]
            ),
            "V19_RESULT_SUBJECT_INVALID",
        ),
    ),
)
def test_insufficient_or_unrelated_junit_ledgers_fail_closed(
    tmp_path: Path,
    xml: str,
    code: str,
) -> None:
    """Reject a passing XML ledger unless it contains every frozen canonical subject."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, xml=xml)
    assert_error(code, lambda: publisher.render_v19(root, candidate))


def test_frozen_checkpoint_command_failure_is_not_hidden_by_committed_passing_xml(tmp_path: Path) -> None:
    """Capture the actual frozen-command exit and fail even when committed XML claims PASS."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, xml=junit_xml(), command_failure=True)
    assert_error(
        "V19_RESULT_COMMAND_FAILED",
        lambda: publisher.render_v19(root, candidate),
        state="FAIL",
    )


def test_isolated_rerun_ledger_must_equal_the_committed_ledger(tmp_path: Path) -> None:
    """Reject a canonical passing snapshot that omits a testcase executed by the candidate."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, xml=junit_xml(), rerun_extra_subject=True)
    assert_error("V19_RESULT_RERUN_DRIFT", lambda: publisher.render_v19(root, candidate))


@pytest.mark.parametrize(
    ("xml", "code"),
    (
        (
            '<testsuite tests="1" failures="0" errors="0" skipped="0">'
            '<testcase classname="checkpoint.Schema" name="Case1" />'
            "</testsuite>",
            "V19_RESULT_XML_INVALID",
        ),
        (
            '<testsuites><testsuite tests="1" failures="0" errors="0" skipped="0">'
            '<group><testcase classname="checkpoint.Schema" name="Case1" /></group>'
            "</testsuite></testsuites>",
            "V19_RESULT_XML_INVALID",
        ),
        (
            '<testsuites><testsuite tests="1" failures="0" errors="0" skipped="0">'
            '<testcase classname="" name="Case1" />'
            "</testsuite></testsuites>",
            "V19_RESULT_LEDGER_INVALID",
        ),
        (
            '<testsuites><testsuite tests="many" failures="0" errors="0" skipped="0">'
            '<testcase classname="_bmad.scripts.tests.test_generate_story_record" '
            'name="test_v2_schema_contract_hold_drift_is_blocked" />'
            "</testsuite></testsuites>",
            "V19_RESULT_COUNT_DRIFT",
        ),
    ),
)
def test_malformed_junit_shape_or_counts_never_produce_a_passing_ledger(
    tmp_path: Path,
    xml: str,
    code: str,
) -> None:
    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root, xml=xml)
    assert_error(code, lambda: publisher.render_v19(root, candidate))


def test_duplicate_junit_subject_is_rejected_with_stable_ledger_code(tmp_path: Path) -> None:
    """Prevent two testcases from satisfying the ledger with one repeated identity."""

    root, _ = clone_with_tooling(tmp_path)
    subject = publisher.REQUIRED_CHECKPOINT_SUBJECTS[0]
    classname, _separator, name = subject.partition("::")
    xml = (
        '<testsuites><testsuite tests="2" failures="0" errors="0" skipped="0">'
        f'<testcase classname="{classname}" name="{name}" />'
        f'<testcase classname="{classname}" name="{name}" />'
        "</testsuite></testsuites>"
    )
    candidate = stage_checkpoint(root, xml=xml)
    assert_error("V19_RESULT_LEDGER_DUPLICATE", lambda: publisher.render_v19(root, candidate))


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


def test_owner_decision_cannot_predate_the_v20_entry(tmp_path: Path) -> None:
    """Keep the human decision after the committed entry that contains V19."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    stage_v19(root, candidate)
    entry = stage_entry(root, committed_at="2026-09-12T13:00:00Z")
    assert_error(
        "V20_OWNER_DECISION_PREDATES_ENTRY",
        lambda: publisher.render_v20(
            root,
            entry_revision=entry,
            owner_identity=OWNER_IDENTITY,
            decided_at_utc=DECIDED_AT_UTC,
            rationale=RATIONALE,
        ),
    )


def test_owner_decision_cannot_postdate_committed_v20_publication(tmp_path: Path) -> None:
    """Reject a committed authority whose claimed human decision is still in the future."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    stage_v19(root, candidate)
    entry = stage_entry(root)
    publication = stage_v20(
        root,
        entry,
        decided_at_utc="2026-09-12T14:00:00Z",
        published_at="2026-09-12T13:00:00Z",
    )
    assert_error(
        "V20_OWNER_DECISION_POSTDATES_PUBLICATION",
        lambda: publisher.publish_v20(
            root,
            entry_revision=publication,
            owner_identity=None,
            decided_at_utc=None,
            rationale=None,
            check=True,
        ),
    )


def test_future_failure_schema_must_be_absent_at_v20_entry(tmp_path: Path) -> None:
    """Prevent Story 7.1 implementation schema bytes from entering before V20."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    stage_v19(root, candidate)
    schema_path = root / publisher.FAILURE_SCHEMA_PATH
    assert not schema_path.exists()
    schema_path.parent.mkdir(parents=True, exist_ok=True)
    schema_path.write_text("{}\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.FAILURE_SCHEMA_PATH], check=True)
    entry = commit(root, "test: add failure schema before V20 entry", committed_at="2026-09-12T11:00:00Z")
    assert_error(
        "V20_FAILURE_SCHEMA_ALREADY_COMMITTED",
        lambda: publisher.render_v20(
            root,
            entry_revision=entry,
            owner_identity=OWNER_IDENTITY,
            decided_at_utc=DECIDED_AT_UTC,
            rationale=RATIONALE,
        ),
    )


def test_current_v19_drift_after_v20_reactivates_effective_hold(tmp_path: Path) -> None:
    """Keep the evaluated V19 bytes equal to the V19 that V20 validated at entry."""

    transaction = full_transaction(tmp_path)
    root = transaction["root"]
    v19_path = root / publisher.V19_PATH
    v19_path.write_bytes(v19_path.read_bytes() + b"\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V19_PATH], check=True)
    descendant = commit(root, "test: drift current V19 after V20")
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["result"] == "FAIL"
    assert hold["blockers"][0]["code"] == "V20_CURRENT_V19_DRIFT"


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


def test_inventory_write_is_idempotent_and_refuses_to_overwrite_different_bytes(tmp_path: Path) -> None:
    """Preserve existing inventory evidence unless it is already byte-identical."""

    root, _ = clone_with_tooling(tmp_path)
    path = root / publisher.INVENTORY_PATH
    expected = path.read_bytes()
    publisher.inventory_route(root, check=False)
    assert path.read_bytes() == expected
    path.write_bytes(b'{"protected":true}\n')
    protected = path.read_bytes()
    assert_error(
        "V20_INVENTORY_OVERWRITE_REFUSED",
        lambda: publisher.inventory_route(root, check=False),
    )
    assert path.read_bytes() == protected


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


def test_unsigned_v20_publication_keeps_the_effective_hold_active(tmp_path: Path) -> None:
    """Require the human V20 publication commit itself to carry a verifiable signature."""

    root, _ = clone_with_tooling(tmp_path)
    candidate = stage_checkpoint(root)
    stage_v19(root, candidate)
    entry = stage_entry(root)
    publisher.publish_v20(
        root,
        entry_revision=entry,
        owner_identity=OWNER_IDENTITY,
        decided_at_utc=DECIDED_AT_UTC,
        rationale=RATIONALE,
        check=False,
    )
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    unsigned_publication = commit(
        root,
        "build(planning): bind unsigned V20 fixture authority",
        committed_at="2026-09-12T13:00:00Z",
    )
    assert_error(
        "V20_PUBLICATION_SIGNATURE_INVALID",
        lambda: publisher.publish_v20(
            root,
            entry_revision=unsigned_publication,
            owner_identity=None,
            decided_at_utc=None,
            rationale=None,
            check=True,
        ),
    )
    hold = publisher.effective_hold(root, evaluated_revision=unsigned_publication)
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]
    assert hold["blockers"][0]["code"] == "V20_PUBLICATION_SIGNATURE_INVALID"


def test_preflight_wires_successor_checks_in_order_before_conformance_build() -> None:
    """Keep final-HEAD inventory, V19, V20, and hold checks ordered in normal CI."""

    workflow = PREFLIGHT_WORKFLOW.read_text(encoding="utf-8")
    commands = (
        "publish_story_7_1_successor_authorities.py --repository . inventory --check",
        "publish_story_7_1_successor_authorities.py --repository . v19 --candidate HEAD --check",
        "publish_story_7_1_successor_authorities.py --repository . v20 --entry-candidate HEAD --check",
        (
            "publish_story_7_1_successor_authorities.py --repository . v20 "
            "--entry-candidate HEAD --effective-hold --check"
        ),
        "- name: Build conformance verifier",
    )
    offsets = [workflow.index(command) for command in commands]
    assert offsets == sorted(offsets)
    assert workflow.count("publish_story_7_1_successor_authorities.py") == 4


def test_operator_cli_routes_report_success_blocked_hold_and_lifted_hold(
    tmp_path: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Cover inventory, V19, V20, and effective-hold routing and exit semantics."""

    root, _ = clone_with_tooling(tmp_path)
    repository_args = ["--repository", str(root)]
    assert publisher.main([*repository_args, "inventory"]) == 0
    inventory_result = json.loads(capsys.readouterr().out)
    assert inventory_result["result"] == "PASS"
    assert inventory_result["assertionLedger"]
    assert publisher.main([*repository_args, "inventory", "--check"]) == 0
    inventory_result = json.loads(capsys.readouterr().out)
    assert inventory_result["result"] == "PASS"
    assert inventory_result["assertionLedger"]

    candidate = stage_checkpoint(root)
    assert publisher.main([*repository_args, "v19", "--candidate", candidate]) == 0
    assert capsys.readouterr().out.startswith(
        f"V19_STORY_7_1_CHECKPOINT_AUTHORITY_OK CANDIDATE={candidate} "
        f"TESTS={len(publisher.REQUIRED_CHECKPOINT_SUBJECTS)}"
    )
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V19_PATH], check=True)
    v19_publication = commit(root, "build(planning): bind V19 through CLI")
    assert publisher.main([*repository_args, "v19", "--candidate", v19_publication, "--check"]) == 0
    assert capsys.readouterr().out.startswith("V19_STORY_7_1_CHECKPOINT_AUTHORITY_OK")

    entry = stage_entry(root)
    assert publisher.main([*repository_args, "v20", "--entry-candidate", entry, "--effective-hold"]) == 2
    active = json.loads(capsys.readouterr().out)
    assert active["result"] == "BLOCKED"
    assert active["effectiveHold"] == "ACTIVE"
    assert active["blockers"][0]["code"] == "V20_PUBLICATION_MISSING"

    assert (
        publisher.main(
            [
                *repository_args,
                "v20",
                "--entry-candidate",
                entry,
                "--owner-identity",
                OWNER_IDENTITY,
                "--decided-at-utc",
                DECIDED_AT_UTC,
                "--rationale",
                RATIONALE,
            ]
        )
        == 0
    )
    assert capsys.readouterr().out.startswith(
        f"V20_STORY_7_1_RELEASE_OWNER_AUTHORITY_OK ENTRY={entry} EFFECTIVE_HOLD=LIFTED"
    )
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    configure_ssh_signing(root)
    v20_publication = commit(
        root,
        "build(planning): bind V20 through CLI",
        sign=True,
        committed_at="2026-09-12T13:00:00Z",
    )
    assert publisher.main([*repository_args, "v20", "--entry-candidate", v20_publication, "--check"]) == 0
    assert capsys.readouterr().out.startswith("V20_STORY_7_1_RELEASE_OWNER_AUTHORITY_OK")
    assert (
        publisher.main(
            [*repository_args, "v20", "--entry-candidate", v20_publication, "--effective-hold"]
        )
        == 0
    )
    lifted = json.loads(capsys.readouterr().out)
    assert lifted["result"] == "PASS"
    assert lifted["effectiveHold"] == "LIFTED"
    assert lifted["blockers"] == []


if __name__ == "__main__":
    raise SystemExit(pytest.main([__file__, "-q"]))
