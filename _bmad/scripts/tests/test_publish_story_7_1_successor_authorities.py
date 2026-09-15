"""Real-lineage, trust-boundary, lifecycle, and mutation tests for V19/V20/V21."""

from __future__ import annotations

from copy import deepcopy
from datetime import timedelta
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from typing import Callable

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_story_7_1_successor_authorities.py"
SPEC = importlib.util.spec_from_file_location("publish_story_7_1_successor_authorities", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(publisher)


def git(root: Path, *arguments: str) -> str:
    """Run Git and return trimmed standard output."""

    return subprocess.check_output(["git", "-C", str(root), *arguments], text=True).strip()


def commit(root: Path, message: str, *, allow_empty: bool = False) -> str:
    """Commit a fixture index with deterministic metadata."""

    arguments = ["git", "-C", str(root), "commit", "-q", "-m", message]
    if allow_empty:
        arguments.insert(4, "--allow-empty")
    environment = {
        **os.environ,
        "GIT_AUTHOR_DATE": "2026-09-15T12:00:00Z",
        "GIT_COMMITTER_DATE": "2026-09-15T12:00:00Z",
    }
    subprocess.run(arguments, check=True, env=environment)
    return git(root, "rev-parse", "HEAD")


def commit_path_bytes(
    root: Path,
    parent: str,
    path: str,
    content: bytes,
    message: str,
) -> str:
    """Create one exact-path child commit without mutating the fixture worktree."""

    blob = subprocess.check_output(
        ["git", "-C", str(root), "hash-object", "-w", "--stdin"],
        input=content,
    ).decode("ascii").strip()
    with tempfile.TemporaryDirectory(prefix="story-7-1-index-") as temporary_name:
        index = Path(temporary_name) / "index"
        environment = {
            **os.environ,
            "GIT_INDEX_FILE": str(index),
            "GIT_AUTHOR_DATE": "2026-09-15T12:00:00Z",
            "GIT_COMMITTER_DATE": "2026-09-15T12:00:00Z",
        }
        subprocess.run(["git", "-C", str(root), "read-tree", parent], check=True, env=environment)
        subprocess.run(
            ["git", "-C", str(root), "update-index", "--add", "--cacheinfo", "100644", blob, path],
            check=True,
            env=environment,
        )
        tree = subprocess.check_output(
            ["git", "-C", str(root), "write-tree"],
            env=environment,
            text=True,
        ).strip()
        return subprocess.check_output(
            ["git", "-C", str(root), "commit-tree", tree, "-p", parent],
            input=message + "\n",
            env=environment,
            text=True,
        ).strip()


def configure_fixture_identity(root: Path, *, name: str = "V21 fixture") -> None:
    """Configure a local, non-signing fixture author."""

    subprocess.run(["git", "-C", str(root), "config", "user.name", name], check=True)
    subprocess.run(["git", "-C", str(root), "config", "user.email", "v21-fixture@example.invalid"], check=True)
    subprocess.run(["git", "-C", str(root), "config", "commit.gpgsign", "false"], check=True)


def clone_at(tmp_path: Path, revision: str) -> Path:
    """Create a shared local clone detached at one exact historical revision."""

    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", revision], check=True)
    configure_fixture_identity(root)
    return root


def copy_v21_tooling(root: Path) -> None:
    """Copy exactly the five approved V21 tooling paths into a V20 checkout."""

    for relative_path in publisher.V21_TOOLING_PATHS:
        source = ROOT / relative_path
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    subprocess.run(["git", "-C", str(root), "add", "--", *publisher.V21_TOOLING_PATHS], check=True)


def stage_tooling(tmp_path: Path, *, extra_path: str | None = None) -> tuple[Path, str]:
    """Create the direct-child V21 tooling transaction, optionally with one extra path."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    copy_v21_tooling(root)
    if extra_path is not None:
        target = root / extra_path
        target.write_text("unexpected tooling path\n", encoding="utf-8")
        subprocess.run(["git", "-C", str(root), "add", "--", extra_path], check=True)
    return root, commit(root, "fix(planning): harden Story 7.1 V21 authority checks")


def stage_v21(root: Path, tooling: str, *, extra_path: str | None = None) -> str:
    """Generate and commit V21, optionally contaminating its one-path transaction."""

    publisher.publish_v21(root, candidate_revision=tooling, check=False)
    paths = [publisher.V21_PATH]
    if extra_path is not None:
        target = root / extra_path
        target.write_text("unexpected V21 publication path\n", encoding="utf-8")
        paths.append(extra_path)
    subprocess.run(["git", "-C", str(root), "add", "--", *paths], check=True)
    return commit(root, "build(planning): publish Story 7.1 V21 authority correction")


def workflow_bootstrap_script() -> str:
    """Return the exact first post-checkout shell program from the approved workflow."""

    model = publisher.preflight_workflow_model((ROOT / publisher.PREFLIGHT_PATH).read_bytes())
    step = model["jobs"]["planning-authority"]["steps"][1]
    run = step["run"]
    assert isinstance(run, dict)
    assert run["style"] == "|"
    assert isinstance(run["script"], str)
    return run["script"]


def active_ruleset_evidence(source_sha: str, repository_id: int = 1234) -> bytes:
    """Return explicit effective organization-ruleset evidence for the protected workflow."""

    return publisher.json_bytes(
        [
            {
                "id": 71,
                "source_type": "Organization",
                "target": "branch",
                "enforcement": "active",
                "bypass_actors": [],
                "conditions": {"ref_name": {"include": ["~DEFAULT_BRANCH"], "exclude": []}},
                "rules": [
                    {"type": "pull_request", "parameters": {"required_approving_review_count": 1}},
                    {
                        "type": "workflows",
                        "parameters": {
                            "workflows": [
                                {
                                    "path": publisher.PREFLIGHT_PATH,
                                    "ref": publisher.CI_RULESET_SOURCE_REF,
                                    "repository_id": repository_id,
                                    "sha": source_sha,
                                }
                            ]
                        },
                    },
                ],
            }
        ]
    )


def run_workflow_bootstrap(
    root: Path,
    source_sha: str,
    *,
    rulesets_content: bytes | None = None,
) -> subprocess.CompletedProcess[str]:
    """Execute the real bootstrap shell step in one isolated fixture repository."""

    evidence = root / ".test-effective-rulesets.json"
    evidence.write_bytes(rulesets_content if rulesets_content is not None else active_ruleset_evidence(source_sha))
    script = workflow_bootstrap_script().replace(
        "https://api.github.com/repos/$source_repository/rulesets?includes_parents=true&per_page=100",
        evidence.resolve().as_uri(),
    )
    try:
        return subprocess.run(
            ["bash", "-c", script],
            cwd=root,
            check=False,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=30,
            env={
                **os.environ,
                "RULESET_TOKEN": "fixture-token",
                "SOURCE_REPOSITORY_ID": "1234",
                "SOURCE_WORKFLOW_REF": publisher.CI_RULESET_SOURCE_WORKFLOW_REF,
                "SOURCE_WORKFLOW_REPOSITORY": publisher.CI_RULESET_SOURCE_REPOSITORY,
                "SOURCE_WORKFLOW_SHA": source_sha,
            },
        )
    finally:
        evidence.unlink(missing_ok=True)


def commit_malicious_publisher(root: Path, message: str) -> str:
    """Commit a publisher that leaves a marker immediately when Python executes it."""

    target = root / publisher.PUBLISHER_PATH
    target.write_text(
        "from pathlib import Path\n"
        "Path('malicious-publisher-executed').write_text('executed\\n', encoding='utf-8')\n",
        encoding="utf-8",
    )
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.PUBLISHER_PATH], check=True)
    return commit(root, message)


@pytest.fixture(scope="module")
def v21_repository(tmp_path_factory: pytest.TempPathFactory) -> tuple[Path, str, str]:
    """Build one valid real-lineage tooling and V21 transaction for read-only tests."""

    root, tooling = stage_tooling(tmp_path_factory.mktemp("v21-valid"))
    publication = stage_v21(root, tooling)
    return root, tooling, publication


def assert_error(
    code: str,
    operation: Callable[[], object],
    *,
    state: str | None = None,
) -> publisher.SuccessorAuthorityError:
    """Require one stable failure code and a nonempty machine-readable ledger."""

    with pytest.raises(publisher.SuccessorAuthorityError) as captured:
        operation()
    error = captured.value
    assert error.code == code
    if state is not None:
        assert error.state == state
    result = publisher.failure_document(ROOT, "v21", error)
    assert result["assertionLedger"]
    assert result["blockers"]
    assert result["result"] == error.state
    return error


def object_schema_nodes(value: object) -> list[dict[str, object]]:
    """Return every explicit object-typed schema node."""

    nodes: list[dict[str, object]] = []
    if isinstance(value, dict):
        if value.get("type") == "object":
            nodes.append(value)
        for child in value.values():
            nodes.extend(object_schema_nodes(child))
    elif isinstance(value, list):
        for child in value:
            nodes.extend(object_schema_nodes(child))
    return nodes


def test_source_pins_exact_owner_key_lineage_checkpoint_and_seven_subjects() -> None:
    """Freeze every root of trust and accepted checkpoint fact in consuming source."""

    assert publisher.TRUSTED_OWNER_IDENTITY == "Jerome Piquot <jpiquot@itaneo.com>"
    assert publisher.TRUSTED_SSH_PRINCIPAL == "jpiquot@itaneo.com"
    assert publisher.TRUSTED_SSH_PUBLIC_KEY == (
        "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIL0Kt34ByT8WvAx325SbxYRNLKBZ3ggbgWomqD1nCHq4"
    )
    assert publisher.TRUSTED_SSH_FINGERPRINT == "SHA256:8XlNQvE3ucPf/e509wU4qtNgiyWA+TKmLei7F7+TCvk"
    assert publisher.APPROVED_CHECKPOINT_COMMIT == "dbd2c5b11f16ddfce8b039e2c31eec38a2e99c8b"
    assert len(publisher.APPROVED_CHECKPOINT_BINDINGS) == 5
    assert tuple(path for path, _digest in publisher.APPROVED_CHECKPOINT_BINDINGS) == publisher.CHECKPOINT_PATHS
    assert len(publisher.REQUIRED_CHECKPOINT_SUBJECTS) == 7
    assert publisher.REQUIRED_CHECKPOINT_SUBJECTS[2].endswith("authority_boundary_annotations")


def test_accepted_v19_v20_and_historical_inventory_pass_without_worktree_substitution() -> None:
    """Validate the accepted chain and its historical/current inventory revisions."""

    v19 = publisher.publish_v19(ROOT, candidate_revision="HEAD", check=True)
    assert v19["freshCheckpoint"]["candidateCommit"] == publisher.APPROVED_CHECKPOINT_COMMIT
    assert tuple(row["subject"] for row in v19["freshCheckpoint"]["assertionLedger"]) == (
        publisher.REQUIRED_CHECKPOINT_SUBJECTS
    )
    v20 = publisher.publish_v20(
        ROOT,
        entry_revision="HEAD",
        owner_identity=None,
        decided_at_utc=None,
        rationale=None,
        check=True,
    )
    assert v20["ownerDecision"]["identity"] == publisher.TRUSTED_OWNER_IDENTITY
    ledger: list[dict[str, str]] = []
    publisher.inventory_route(ROOT, check=True, assertion_ledger=ledger)
    assert ledger
    assert all(row["state"] == "PASS" for row in ledger)
    assert any(f"@{publisher.APPROVED_TOOLING_COMMIT}" in row["subject"] for row in ledger)
    assert any(f"@{publisher.APPROVED_CHECKPOINT_COMMIT}" in row["subject"] for row in ledger)


def test_fresh_clone_v20_ignores_missing_and_hostile_local_trust_configuration(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Prove source-pinned V20 trust works with no repository or user allowlist."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    subprocess.run(
        ["git", "-C", str(root), "config", "gpg.ssh.allowedSignersFile", str(tmp_path / "missing")],
        check=True,
    )
    subprocess.run(
        ["git", "-C", str(root), "config", "gpg.ssh.program", "/bin/false"],
        check=True,
    )
    revoked = tmp_path / "revoked-signers"
    revoked.write_text(f"{publisher.TRUSTED_SSH_PUBLIC_KEY}\n", encoding="utf-8")
    subprocess.run(
        ["git", "-C", str(root), "config", "gpg.ssh.revocationFile", str(revoked)],
        check=True,
    )
    subprocess.run(
        ["git", "-C", str(root), "config", "gpg.minTrustLevel", "ultimate"],
        check=True,
    )
    hostile = tmp_path / "hostile.gitconfig"
    hostile.write_text("[gpg \"ssh\"]\nallowedSignersFile = /definitely/missing\n", encoding="utf-8")
    monkeypatch.setenv("GIT_CONFIG_GLOBAL", str(hostile))
    monkeypatch.setenv("GIT_CONFIG_SYSTEM", str(hostile))
    document = publisher.publish_v20(
        root,
        entry_revision="HEAD",
        owner_identity=None,
        decided_at_utc=None,
        rationale=None,
        check=True,
    )
    assert document["result"] == "PASS"


@pytest.mark.parametrize("failure_phase", ("creation", "cleanup"))
def test_v20_trust_directory_failures_are_stable_blockers(
    monkeypatch: pytest.MonkeyPatch,
    failure_phase: str,
) -> None:
    """Convert temporary trust-directory creation and cleanup errors to BLOCKED."""

    original_temporary_directory = publisher.tempfile.TemporaryDirectory
    if failure_phase == "creation":

        def fail_creation(*_args: object, **_kwargs: object) -> object:
            raise OSError("trust directory creation failure")

        replacement: object = fail_creation
    else:

        class CleanupFailure(original_temporary_directory):
            def cleanup(self) -> None:
                super().cleanup()
                raise OSError("trust directory cleanup failure")

        replacement = CleanupFailure
    monkeypatch.setattr(publisher.tempfile, "TemporaryDirectory", replacement)
    error = assert_error(
        "V20_TRUST_ANCHOR_UNAVAILABLE",
        lambda: publisher.verify_v20_publication_signature(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
        state="BLOCKED",
    )
    assert f"trust directory {failure_phase} failure" in error.detail


@pytest.mark.parametrize(
    ("identity", "code"),
    (
        ("not-an-address", "V20_OWNER_IDENTITY_INVALID"),
        ("Another Owner <owner@example.invalid>", "V20_OWNER_IDENTITY_MISMATCH"),
    ),
)
def test_owner_identity_is_format_checked_and_exact_before_write(identity: str, code: str) -> None:
    """Reject malformed or unapproved owner identities before authority bytes are written."""

    assert_error(
        code,
        lambda: publisher.validate_owner_fields(
            identity,
            "2026-09-15T12:00:00Z",
            "V19-STORY-7.1-CHECKPOINT-COMPLETION V20-STORY-7.1-INPUT-INVENTORY-v1",
        ),
    )


def test_v20_owner_decision_cannot_predate_the_fixed_entry() -> None:
    """Reject an owner decision before the accepted entry on the fixed lineage."""

    accepted = json.loads(
        publisher.candidate_blob(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.V20_PATH,
            "TEST",
        )
    )
    decided_at = (
        publisher.commit_time(ROOT, publisher.APPROVED_ENTRY_COMMIT, "TEST")
        - timedelta(seconds=1)
    ).strftime("%Y-%m-%dT%H:%M:%SZ")
    assert_error(
        "V20_OWNER_DECISION_PREDATES_ENTRY",
        lambda: publisher.render_v20(
            ROOT,
            entry_revision=publisher.APPROVED_ENTRY_COMMIT,
            owner_identity=accepted["ownerDecision"]["identity"],
            decided_at_utc=decided_at,
            rationale=accepted["ownerDecision"]["rationale"],
        ),
    )


def test_v20_owner_decision_must_be_strictly_after_the_fixed_entry() -> None:
    """Reject the equality boundary as not being a post-entry human decision."""

    accepted = json.loads(
        publisher.candidate_blob(ROOT, publisher.APPROVED_V20_PUBLICATION, publisher.V20_PATH, "TEST")
    )
    decided_at = publisher.commit_time(ROOT, publisher.APPROVED_ENTRY_COMMIT, "TEST").strftime(
        "%Y-%m-%dT%H:%M:%SZ"
    )
    assert_error(
        "V20_OWNER_DECISION_PREDATES_ENTRY",
        lambda: publisher.render_v20(
            ROOT,
            entry_revision=publisher.APPROVED_ENTRY_COMMIT,
            owner_identity=accepted["ownerDecision"]["identity"],
            decided_at_utc=decided_at,
            rationale=accepted["ownerDecision"]["rationale"],
        ),
    )


def test_v20_owner_decision_cannot_postdate_the_fixed_publication(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Reject a decision instant after publication while checking the fixed lineage."""

    original_validate_owner_fields = publisher.validate_owner_fields
    postpublication_time = (
        publisher.commit_time(ROOT, publisher.APPROVED_V20_PUBLICATION, "TEST")
        + timedelta(seconds=1)
    )

    def validate_as_postpublication(identity: str, decided_at_utc: str, rationale: str) -> object:
        original_validate_owner_fields(identity, decided_at_utc, rationale)
        return postpublication_time

    monkeypatch.setattr(publisher, "validate_owner_fields", validate_as_postpublication)
    assert_error(
        "V20_OWNER_DECISION_POSTDATES_PUBLICATION",
        lambda: publisher.publish_v20(
            ROOT,
            entry_revision=publisher.APPROVED_V20_PUBLICATION,
            owner_identity=None,
            decided_at_utc=None,
            rationale=None,
            check=True,
        ),
    )


def test_wrong_source_pinned_key_rejects_the_real_v20(monkeypatch: pytest.MonkeyPatch) -> None:
    """Fail closed when the tracked public key is replaced."""

    monkeypatch.setattr(
        publisher,
        "TRUSTED_SSH_PUBLIC_KEY",
        "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAICe0NXwYOqOQ1X8MhxP7lK7L1OeRZxIPMhmjrSEscQvZ",
    )
    assert_error(
        "V20_PUBLICATION_SIGNATURE_INVALID",
        lambda: publisher.verify_v20_publication_signature(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
    )


def test_v20_author_mismatch_has_an_independent_stable_failure(tmp_path: Path) -> None:
    """Check exact author identity before accepting any signature claim."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    configure_fixture_identity(root, name="Different Author")
    different_author = commit(root, "test: create different author fixture", allow_empty=True)
    assert_error(
        "V20_PUBLICATION_AUTHOR_MISMATCH",
        lambda: publisher.verify_v20_publication_signature(
            root,
            different_author,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
    )


def test_v20_signer_and_fingerprint_mismatches_have_independent_codes(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Require exact signer principal and fingerprint after cryptographic verification."""

    original = publisher.run_git

    def mismatched_signer(root: Path, *arguments: str, **kwargs: object) -> subprocess.CompletedProcess[bytes]:
        if "--format=%G?%x00%GS%x00%GF" in arguments:
            return subprocess.CompletedProcess(
                arguments,
                0,
                f"G\0another@example.invalid\0{publisher.TRUSTED_SSH_FINGERPRINT}\n".encode(),
                b"",
            )
        return original(root, *arguments, **kwargs)

    monkeypatch.setattr(publisher, "run_git", mismatched_signer)
    assert_error(
        "V20_PUBLICATION_SIGNER_MISMATCH",
        lambda: publisher.verify_v20_publication_signature(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
    )
    monkeypatch.setattr(publisher, "run_git", original)
    monkeypatch.setattr(publisher, "TRUSTED_SSH_FINGERPRINT", "SHA256:wrong")
    assert_error(
        "V20_PUBLICATION_FINGERPRINT_MISMATCH",
        lambda: publisher.verify_v20_publication_signature(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
    )


def test_v20_untrusted_signature_status_is_not_accepted(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Reject status U even after the cryptographic command reports success."""

    original = publisher.run_git

    def untrusted_status(root: Path, *arguments: str, **kwargs: object) -> subprocess.CompletedProcess[bytes]:
        if "--format=%G?%x00%GS%x00%GF" in arguments:
            return subprocess.CompletedProcess(
                arguments,
                0,
                f"U\0{publisher.TRUSTED_SSH_PRINCIPAL}\0{publisher.TRUSTED_SSH_FINGERPRINT}\n".encode(),
                b"",
            )
        return original(root, *arguments, **kwargs)

    monkeypatch.setattr(publisher, "run_git", untrusted_status)
    assert_error(
        "V20_PUBLICATION_SIGNATURE_STATUS_INVALID",
        lambda: publisher.verify_v20_publication_signature(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
    )


@pytest.mark.parametrize(
    "subjects",
    (
        publisher.REQUIRED_CHECKPOINT_SUBJECTS[:-1],
        (*publisher.REQUIRED_CHECKPOINT_SUBJECTS, f"{publisher.CHECKPOINT_SUBJECT_PREFIX}synthetic_extra"),
    ),
)
def test_noncanonical_or_incomplete_checkpoint_ledgers_fail_closed(subjects: tuple[str, ...]) -> None:
    """Require identity equality with the frozen seven-case checkpoint inventory."""

    cases = []
    for subject in subjects:
        classname, name = subject.split("::", 1)
        cases.append(f'<testcase classname="{classname}" name="{name}" />')
    xml = (
        '<testsuites><testsuite tests="'
        + str(len(cases))
        + '" failures="0" errors="0" skipped="0">'
        + "".join(cases)
        + "</testsuite></testsuites>"
    ).encode()
    assert_error("V19_RESULT_LEDGER_INSUFFICIENT", lambda: publisher.parse_junit(xml))


def test_alternate_checkpoint_and_published_binding_are_rejected() -> None:
    """Prevent same-shaped synthetic checkpoint histories from replacing approved bytes."""

    assert_error(
        "V19_CHECKPOINT_CANDIDATE_MISMATCH",
        lambda: publisher.render_v19(ROOT, publisher.APPROVED_TOOLING_COMMIT),
    )
    bindings = publisher.checkpoint_bindings(ROOT, publisher.APPROVED_CHECKPOINT_COMMIT)
    assert [(row["path"], row["sha256"]) for row in bindings] == list(
        publisher.APPROVED_CHECKPOINT_BINDINGS
    )


def test_stale_checkpoint_xml_is_removed_before_rerun(monkeypatch: pytest.MonkeyPatch) -> None:
    """A successful command that does not freshly create XML cannot reuse the committed snapshot."""

    original_run = subprocess.run

    def no_output(arguments: object, *args: object, **kwargs: object) -> subprocess.CompletedProcess[bytes]:
        if tuple(arguments) == publisher.CHECKPOINT_COMMAND_ARGUMENTS:
            return subprocess.CompletedProcess(arguments, 0, b"", b"")
        return original_run(arguments, *args, **kwargs)

    monkeypatch.setattr(publisher.subprocess, "run", no_output)
    assert_error(
        "V19_RESULT_COMMAND_OUTPUT_MISSING",
        lambda: publisher.rerun_checkpoint_command(ROOT, publisher.APPROVED_CHECKPOINT_COMMIT),
        state="BLOCKED",
    )


@pytest.mark.parametrize("route", ("render", "publish"))
def test_v19_callers_propagate_mandatory_rerun_failure(
    monkeypatch: pytest.MonkeyPatch,
    route: str,
) -> None:
    """Keep the mandatory rerun on both render and committed-check caller paths."""

    def fail_rerun(_root: Path, _candidate: str) -> tuple[int, bytes]:
        raise publisher.SuccessorAuthorityError("V19_RESULT_COMMAND_FAILED", "fixture failure", "FAIL")

    monkeypatch.setattr(publisher, "rerun_checkpoint_command", fail_rerun)
    if route == "render":
        operation = lambda: publisher.render_v19(ROOT, publisher.APPROVED_CHECKPOINT_COMMIT)
    else:
        operation = lambda: publisher.publish_v19(
            ROOT,
            candidate_revision=publisher.APPROVED_V20_PUBLICATION,
            check=True,
        )
    assert_error("V19_RESULT_COMMAND_FAILED", operation, state="FAIL")


def test_checkpoint_rerun_checks_out_the_bound_historical_candidate(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Run the frozen command at V19's historical checkpoint, never current HEAD."""

    original = publisher.run_git
    checkouts: list[tuple[str, ...]] = []

    def observe(root: Path, *arguments: str, **kwargs: object) -> subprocess.CompletedProcess[bytes]:
        if arguments[:2] == ("checkout", "--detach"):
            checkouts.append(arguments)
        return original(root, *arguments, **kwargs)

    monkeypatch.setattr(publisher, "run_git", observe)
    exit_code, content = publisher.rerun_checkpoint_command(ROOT, publisher.APPROVED_CHECKPOINT_COMMIT)
    assert exit_code == 0
    assert content
    assert ("checkout", "--detach", publisher.APPROVED_CHECKPOINT_COMMIT) in checkouts


def test_nonzero_checkpoint_rerun_fails_even_with_canonical_xml(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Treat a nonzero frozen-command exit as FAIL even when canonical XML exists."""

    canonical_xml = publisher.candidate_blob(
        ROOT,
        publisher.APPROVED_CHECKPOINT_COMMIT,
        publisher.CHECKPOINT_RESULT_PATH,
        "TEST",
    )
    monkeypatch.setattr(
        publisher,
        "rerun_checkpoint_command",
        lambda _root, _candidate: (1, canonical_xml),
    )
    assert_error(
        "V19_RESULT_COMMAND_FAILED",
        lambda: publisher.render_v19(ROOT, publisher.APPROVED_CHECKPOINT_COMMIT),
        state="FAIL",
    )


def test_v21_schema_is_closed_and_cannot_redefine_its_trusted_key(
    v21_repository: tuple[Path, str, str],
) -> None:
    """Keep the key in consuming source and the machine correction recursively closed."""

    root, tooling, _publication = v21_repository
    schema_bytes = publisher.candidate_blob(root, tooling, publisher.V21_SCHEMA_PATH, "TEST")
    schema = json.loads(schema_bytes)
    jsonschema.Draft202012Validator.check_schema(schema)
    assert schema["$id"] == (
        "https://hexalith.io/schemas/conversations/"
        "v21-story-7.1-authority-correction-v1.schema.json"
    )
    assert all(node.get("additionalProperties") is False for node in object_schema_nodes(schema))
    assert publisher.TRUSTED_SSH_PUBLIC_KEY not in schema_bytes.decode("utf-8")
    document = publisher.render_v21(root, tooling)
    changed = deepcopy(document)
    changed["verifiedV20"]["trustedPublicKey"] = publisher.TRUSTED_SSH_PUBLIC_KEY
    assert_error(
        "V21_SCHEMA_INVALID",
        lambda: publisher.validate_json_schema(schema_bytes, changed, "V21_SCHEMA_INVALID"),
    )


def test_valid_v21_is_exact_one_path_and_lifts_only_story_7_1(
    v21_repository: tuple[Path, str, str],
) -> None:
    """Cover the V21 publication and final effective-hold matrix row."""

    root, tooling, publication = v21_repository
    document = publisher.publish_v21(root, candidate_revision=publication, check=True)
    assert git(root, "rev-parse", f"{publication}^") == tooling
    assert git(root, "diff", "--name-only", tooling, publication) == publisher.V21_PATH
    assert document["verifiedV20"]["signature"] == {
        "status": "G",
        "principal": publisher.TRUSTED_SSH_PRINCIPAL,
        "fingerprint": publisher.TRUSTED_SSH_FINGERPRINT,
        "authorIdentity": publisher.TRUSTED_OWNER_IDENTITY,
    }
    assert document["authorityEffect"]["unlocks"] == ["7.1"]
    assert document["authorityEffect"]["ownerDecisionReplaced"] is False
    hold = publisher.effective_hold(root, evaluated_revision=publication)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "LIFTED"
    assert hold["assertionLedger"]


def test_checked_in_v21_transaction_is_valid_when_present() -> None:
    """Exercise the repository's real transaction once V21 has been committed."""

    head = git(ROOT, "rev-parse", "HEAD")
    publications = publisher.publication_candidates(ROOT, head, publisher.V21_PATH, "TEST")
    if publications:
        assert len(publications) == 1
        publication = publications[0]
        document = publisher.publish_v21(ROOT, candidate_revision=head, check=True)
        tooling = document["hardenedTooling"]["commit"]
        assert tooling == git(ROOT, "rev-parse", f"{publication}^")
        assert publisher.changed_paths(ROOT, tooling, publication) == (
            publisher.V21_PATH,
        )
    else:
        hold = publisher.effective_hold(ROOT, evaluated_revision=head)
        assert hold["result"] == "PASS"
        assert hold["effectiveHold"] == "ACTIVE"


@pytest.mark.parametrize(
    "revision",
    (
        publisher.APPROVED_V19_PUBLICATION,
        publisher.APPROVED_ENTRY_COMMIT,
        publisher.APPROVED_V20_PUBLICATION,
    ),
)
def test_supported_pre_v21_lifecycle_states_are_valid_active(revision: str) -> None:
    """Treat expected V21 absence as valid ACTIVE without masking present evidence."""

    hold = publisher.effective_hold(ROOT, evaluated_revision=revision)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["blockers"] == []
    assert hold["assertionLedger"]
    assert hold["assertionLedger"][-1]["state"] == "not-applicable"


def test_shallow_history_blocks_lifecycle_before_expected_absence(tmp_path: Path) -> None:
    """Never classify a missing successor as expected when history is incomplete."""

    root = tmp_path / "repository"
    subprocess.run(
        ["git", "clone", "--depth", "1", "--no-local", "-q", str(ROOT), str(root)],
        check=True,
    )
    assert git(root, "rev-parse", "--is-shallow-repository") == "true"
    hold = publisher.effective_hold(root, evaluated_revision="HEAD")
    assert hold["result"] == "BLOCKED"
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]
    assert hold["blockers"] == [
        {
            "code": "SUCCESSOR_HISTORY_INCOMPLETE",
            "state": "BLOCKED",
            "detail": "expected complete history; observed is-shallow-repository='true'",
        }
    ]


def test_malformed_present_v20_is_not_treated_as_expected_absence(tmp_path: Path) -> None:
    """A present but drifted authority fails the lifecycle instead of becoming ACTIVE/PASS."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    path = root / publisher.V20_PATH
    path.write_bytes(path.read_bytes() + b"\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    descendant = commit(root, "test: drift present V20 evidence")
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["result"] == "FAIL"
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]
    assert hold["blockers"]


def test_published_v21_deletion_is_blocked_instead_of_expected_absence(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Distinguish deletion after publication from a genuine pre-V21 lifecycle state."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    subprocess.run(["git", "-C", str(root), "rm", "-q", "--", publisher.V21_PATH], check=True)
    descendant = commit(root, "test: delete published V21 authority")
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["result"] == "BLOCKED"
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]
    assert hold["blockers"][0]["code"] == "V21_AUTHORITY_DESCENDANT_MISSING"


@pytest.mark.parametrize(
    ("path", "code"),
    (
        (publisher.INVENTORY_PATH, "V20_INVENTORY_DESCENDANT_MISSING"),
        (publisher.V19_PATH, "V19_AUTHORITY_DESCENDANT_MISSING"),
        (publisher.V20_PATH, "V20_AUTHORITY_DESCENDANT_MISSING"),
    ),
)
def test_published_predecessor_deletion_is_blocked(
    tmp_path: Path,
    path: str,
    code: str,
) -> None:
    """Treat removal of every historically published predecessor as evidence loss."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    subprocess.run(["git", "-C", str(root), "rm", "-q", "--", path], check=True)
    descendant = commit(root, "test: delete published predecessor authority")
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["result"] == "BLOCKED"
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]
    assert hold["blockers"][0]["code"] == code


def test_v21_introduced_through_second_parent_is_not_merge_drift(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Accept a no-fast-forward merge that brings the existing V21 through parent two."""

    source, tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", tooling], check=True)
    configure_fixture_identity(root)
    path = root / publisher.IMPLEMENTATION_PATHS[2]
    path.write_bytes(path.read_bytes() + b"\n# Allowed pre-V21 side branch work.\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.IMPLEMENTATION_PATHS[2]], check=True)
    side_tip = commit(root, "test: add allowed side branch work")
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", publication, "-m", "test: merge V21 second parent"],
        check=True,
    )
    merge = git(root, "rev-parse", "HEAD")
    assert git(root, "rev-list", "--parents", "-n", "1", merge).split()[1:] == [side_tip, publication]
    paths, _gitlinks = publisher.descendant_history_changes(root, publication, merge)
    assert all(path != publisher.V21_PATH for _commit, path in paths)
    publisher.publish_v21(root, candidate_revision=merge, check=True)
    hold = publisher.effective_hold(root, evaluated_revision=merge)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "LIFTED"


def test_allowed_only_pre_v21_merge_is_accepted_when_later_merged_with_v21(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Compare a pre-V21 merge with its first parent while inspecting both lines."""

    source, tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", tooling], check=True)
    configure_fixture_identity(root)
    first_path = root / publisher.IMPLEMENTATION_PATHS[2]
    first_path.write_bytes(first_path.read_bytes() + b"\n# Allowed first pre-V21 branch.\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.IMPLEMENTATION_PATHS[2]], check=True)
    first_tip = commit(root, "test: add first allowed pre-V21 branch")
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", tooling], check=True)
    second_path = root / publisher.IMPLEMENTATION_PATHS[4]
    second_path.write_bytes(second_path.read_bytes() + b"\nAllowed second pre-V21 branch.\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.IMPLEMENTATION_PATHS[4]], check=True)
    second_tip = commit(root, "test: add second allowed pre-V21 branch")
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", first_tip], check=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", second_tip, "-m", "test: merge allowed pre-V21 branches"],
        check=True,
    )
    pre_v21_merge = git(root, "rev-parse", "HEAD")
    assert not publisher.is_ancestor(root, publication, pre_v21_merge)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", pre_v21_merge, "-m", "test: merge allowed work with V21"],
        check=True,
    )
    descendant = git(root, "rev-parse", "HEAD")
    publisher.publish_v21(root, candidate_revision=descendant, check=True)
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "LIFTED"


def test_allowed_pre_v21_branch_inheriting_tooling_as_second_parent_is_accepted(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Use the approved tooling tree for a pre-V21 merge that inherits it via parent two."""

    source, tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(
        ["git", "-C", str(root), "checkout", "-q", "--detach", publisher.APPROVED_V20_PUBLICATION],
        check=True,
    )
    configure_fixture_identity(root)
    path = root / publisher.IMPLEMENTATION_PATHS[2]
    path.write_bytes(path.read_bytes() + b"\n# Allowed V20-based Story 7.1 work.\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.IMPLEMENTATION_PATHS[2]], check=True)
    side_tip = commit(root, "test: add allowed V20-based Story 7.1 work")
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", tooling, "-m", "test: inherit tooling as parent two"],
        check=True,
    )
    pre_v21_merge = git(root, "rev-parse", "HEAD")
    assert git(root, "rev-list", "--parents", "-n", "1", pre_v21_merge).split()[1:] == [side_tip, tooling]
    assert publisher.is_ancestor(root, tooling, pre_v21_merge)
    assert not publisher.is_ancestor(root, publication, pre_v21_merge)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", pre_v21_merge, "-m", "test: merge allowed tooling branch with V21"],
        check=True,
    )
    descendant = git(root, "rev-parse", "HEAD")
    publisher.publish_v21(root, candidate_revision=descendant, check=True)
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "LIFTED"


def test_pre_v21_side_branch_touch_and_restore_remains_visible_after_merge(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Inspect side commits forked before V21 even when their endpoint tree is clean."""

    source, tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", tooling], check=True)
    configure_fixture_identity(root)
    path = root / publisher.V20_PATH
    original = path.read_bytes()
    path.write_bytes(original + b"\nforbidden side-branch drift\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    commit(root, "test: touch authority before V21")
    path.write_bytes(original)
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    side_tip = commit(root, "test: restore authority before V21")
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", side_tip, "-m", "test: merge restored side branch"],
        check=True,
    )
    assert_error(
        "V21_DESCENDANT_PATH_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision="HEAD", check=True),
    )


def test_full_history_rejects_duplicate_v21_publications(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Find both branch-local additions even when a merge simplifies path history."""

    source, tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", tooling], check=True)
    configure_fixture_identity(root)
    publisher.publish_v21(root, candidate_revision=tooling, check=False)
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V21_PATH], check=True)
    duplicate = commit(root, "build(planning): publish duplicate Story 7.1 V21 authority")
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "-q", duplicate, "-m", "test: merge duplicate V21"],
        check=True,
    )
    candidates = publisher.publication_candidates(root, "HEAD", publisher.V21_PATH, "TEST")
    assert set(candidates) == {publication, duplicate}
    assert_error(
        "V21_PUBLICATION_MISSING",
        lambda: publisher.publish_v21(root, candidate_revision="HEAD", check=True),
        state="BLOCKED",
    )
    assert_error(
        "V21_PUBLICATION_MISSING",
        lambda: publisher.publish_v21(
            root,
            candidate_revision="HEAD",
            publication_revision=publication,
            check=True,
        ),
        state="BLOCKED",
    )


def test_merge_created_forbidden_resolution_is_rejected(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Inspect merge-result bytes that differ from every parent tree."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    first_parent = commit(root, "test: first merge parent", allow_empty=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    commit(root, "test: second merge parent", allow_empty=True)
    subprocess.run(["git", "-C", str(root), "merge", "--no-ff", "--no-commit", "-q", first_parent], check=True)
    forbidden = root / "unexpected-merge-resolution.txt"
    forbidden.write_text("merge-created forbidden path\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "--", forbidden.name], check=True)
    merge = commit(root, "test: create forbidden merge resolution")
    assert_error(
        "V21_DESCENDANT_PATH_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=merge, check=True),
    )


def test_merge_result_cannot_restore_stale_tooling_from_an_older_parent(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Compare protected merge-result bytes with the V21 publication tree."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(
        ["git", "-C", str(root), "checkout", "-q", "--detach", publisher.APPROVED_V20_PUBLICATION],
        check=True,
    )
    configure_fixture_identity(root)
    old_parent = commit(root, "test: retain an older pre-tooling parent", allow_empty=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    current_parent = commit(root, "test: create current merge parent", allow_empty=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "--no-commit", "-q", old_parent],
        check=True,
    )
    stale = publisher.candidate_blob(root, old_parent, publisher.V21_SPEC_PATH, "TEST")
    path = root / publisher.V21_SPEC_PATH
    path.write_bytes(stale)
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V21_SPEC_PATH], check=True)
    merge = commit(root, "test: restore stale tooling in merge result")
    assert git(root, "rev-list", "--parents", "-n", "1", merge).split()[1:] == [
        current_parent,
        old_parent,
    ]
    assert publisher.candidate_blob(root, merge, publisher.V21_SPEC_PATH, "TEST") == stale
    assert_error(
        "V21_DESCENDANT_PATH_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=merge, check=True),
    )


def test_merge_created_gitlink_is_rejected(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Exercise merge-result mode-160000 detection independently of ordinary paths."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    first_parent = commit(root, "test: create first gitlink merge parent", allow_empty=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    commit(root, "test: create second gitlink merge parent", allow_empty=True)
    subprocess.run(
        ["git", "-C", str(root), "merge", "--no-ff", "--no-commit", "-q", first_parent],
        check=True,
    )
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
    merge = commit(root, "test: create a gitlink in merge result")
    assert_error(
        "V21_DESCENDANT_GITLINK_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=merge, check=True),
    )


def test_allowed_story_descendant_passes_historical_inventory_and_keeps_lift(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Resolve frozen evidence historically while allowing only declared implementation changes."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    path = root / "_bmad/scripts/generate_story_record.py"
    path.write_bytes(path.read_bytes() + b"\n# Story 7.1 implementation descendant.\n")
    subprocess.run(["git", "-C", str(root), "add", "--", str(path.relative_to(root))], check=True)
    descendant = commit(root, "feat: implement Story 7.1 fixture")
    ledger: list[dict[str, str]] = []
    publisher.inventory_route(root, check=True, evaluated_revision=descendant, assertion_ledger=ledger)
    assert ledger
    publisher.publish_v21(root, candidate_revision=descendant, check=True)
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "LIFTED"


def test_forbidden_evidence_drift_after_v21_reactivates_hold(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Reject descendant drift outside the exact Story 7.1 implementation inventory."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    path = root / publisher.V20_PATH
    path.write_bytes(path.read_bytes() + b"\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    descendant = commit(root, "test: drift frozen V20 evidence")
    assert_error(
        "V21_DESCENDANT_PATH_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=descendant, check=True),
    )
    hold = publisher.effective_hold(root, evaluated_revision=descendant)
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]


def test_touched_and_reverted_forbidden_evidence_remains_visible(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Inspect every descendant commit instead of trusting only endpoint bytes."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    path = root / publisher.V20_PATH
    original = path.read_bytes()
    path.write_bytes(original + b"\nforbidden intermediate drift\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    commit(root, "test: touch forbidden authority evidence")
    path.write_bytes(original)
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    descendant = commit(root, "test: restore forbidden authority evidence")
    assert publisher.candidate_blob(root, descendant, publisher.V20_PATH, "TEST") == original
    assert_error(
        "V21_DESCENDANT_PATH_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=descendant, check=True),
    )


@pytest.mark.parametrize(
    ("path", "allowed"),
    tuple((path, True) for path in (*publisher.RESULT_PATHS, *publisher.RECORD_OUTPUT_PATHS))
    + (("artifacts/v9/7.1/AC-7.1-06.xml", False),),
)
def test_descendant_output_allowlist_is_exact(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
    path: str,
    allowed: bool,
) -> None:
    """Permit every canonical result/final-record output and reject an adjacent path."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    target = root / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text("Story 7.1 generated output fixture\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "--force", "--", path], check=True)
    descendant = commit(root, "test: add Story 7.1 output fixture")
    if allowed:
        publisher.publish_v21(root, candidate_revision=descendant, check=True)
    else:
        assert_error(
            "V21_DESCENDANT_PATH_DRIFT",
            lambda: publisher.publish_v21(root, candidate_revision=descendant, check=True),
        )


def test_v21_record_drift_after_publication_is_rejected(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Preserve the published V21 bytes unchanged at every evaluated descendant."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    path = root / publisher.V21_PATH
    path.write_bytes(path.read_bytes() + b"\n")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V21_PATH], check=True)
    descendant = commit(root, "test: drift published V21 evidence")
    assert_error(
        "V21_AUTHORITY_DESCENDANT_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=descendant, check=True),
    )


def test_v21_tooling_requires_v20_as_direct_parent(tmp_path: Path) -> None:
    """Reject an intermediary commit between signed V20 and hardened tooling."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    commit(root, "test: insert unauthorized intermediary", allow_empty=True)
    copy_v21_tooling(root)
    tooling = commit(root, "fix(planning): harden V21 after intermediary")
    assert_error(
        "V21_TOOLING_PARENT_MISMATCH",
        lambda: publisher.render_v21(root, tooling),
    )


def test_v21_tooling_rejects_extra_path(tmp_path: Path) -> None:
    """Require the tooling transaction's exact five-path set."""

    root, tooling = stage_tooling(tmp_path, extra_path="unexpected-tooling.txt")
    assert_error(
        "V21_TOOLING_PATH_SET_MISMATCH",
        lambda: publisher.render_v21(root, tooling),
    )


def test_v21_publication_rejects_extra_path(tmp_path: Path) -> None:
    """Require V21 itself to add exactly one JSON path."""

    root, tooling = stage_tooling(tmp_path)
    publication = stage_v21(root, tooling, extra_path="unexpected-publication.txt")
    assert_error(
        "V21_PUBLICATION_SCOPE_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=publication, check=True),
    )


def test_v21_write_refuses_to_overwrite_existing_bytes(tmp_path: Path) -> None:
    """Preserve locally prepared authority evidence instead of overwriting it."""

    root, tooling = stage_tooling(tmp_path)
    target = root / publisher.V21_PATH
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(b'{"protected":true}\n')
    before = target.read_bytes()
    assert_error(
        "V21_AUTHORITY_ALREADY_EXISTS",
        lambda: publisher.publish_v21(root, candidate_revision=tooling, check=False),
    )
    assert target.read_bytes() == before


def test_v21_write_rejects_parent_symlink_escape(tmp_path: Path) -> None:
    """Reject a write parent redirected outside the repository before creating bytes."""

    root, tooling = stage_tooling(tmp_path)
    target = root / publisher.V21_PATH
    saved_parent = tmp_path / "saved-planning-artifacts"
    target.parent.rename(saved_parent)
    outside = tmp_path / "outside"
    outside.mkdir()
    target.parent.symlink_to(outside, target_is_directory=True)
    assert_error(
        "SUCCESSOR_PATH_ESCAPE",
        lambda: publisher.publish_v21(root, candidate_revision=tooling, check=False),
        state="BLOCKED",
    )
    assert not (outside / target.name).exists()


def test_v21_atomic_publication_preserves_a_concurrent_writer(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Refuse a target created after rendering but before atomic publication."""

    root, tooling = stage_tooling(tmp_path)
    target = root / publisher.V21_PATH
    protected = b'{"concurrent":true}\n'
    original_link = publisher.os.link

    def create_competing_target(
        source: str,
        destination: str,
        *,
        src_dir_fd: int,
        dst_dir_fd: int,
        follow_symlinks: bool,
    ) -> None:
        descriptor = os.open(destination, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644, dir_fd=dst_dir_fd)
        try:
            os.write(descriptor, protected)
        finally:
            os.close(descriptor)
        original_link(
            source,
            destination,
            src_dir_fd=src_dir_fd,
            dst_dir_fd=dst_dir_fd,
            follow_symlinks=follow_symlinks,
        )

    monkeypatch.setattr(publisher.os, "link", create_competing_target)
    assert_error(
        "V21_AUTHORITY_ALREADY_EXISTS",
        lambda: publisher.publish_v21(root, candidate_revision=tooling, check=False),
    )
    assert target.read_bytes() == protected
    assert not tuple(target.parent.glob(f".{target.name}.*.tmp"))


@pytest.mark.parametrize(
    ("route", "revision", "path", "code"),
    (
        (
            "v19",
            publisher.APPROVED_CHECKPOINT_COMMIT,
            publisher.V19_PATH,
            "V19_AUTHORITY_ALREADY_EXISTS",
        ),
        (
            "v20",
            publisher.APPROVED_ENTRY_COMMIT,
            publisher.V20_PATH,
            "V20_AUTHORITY_ALREADY_EXISTS",
        ),
    ),
)
def test_v19_and_v20_atomic_publication_preserve_a_concurrent_writer(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    route: str,
    revision: str,
    path: str,
    code: str,
) -> None:
    """Refuse concurrent V19/V20 authority bytes without replacing them."""

    root = clone_at(tmp_path, revision)
    target = root / path
    protected = f'{{"concurrent":"{route}"}}\n'.encode()
    original_link = publisher.os.link

    def create_competing_target(
        source: str,
        destination: str,
        *,
        src_dir_fd: int,
        dst_dir_fd: int,
        follow_symlinks: bool,
    ) -> None:
        descriptor = os.open(destination, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644, dir_fd=dst_dir_fd)
        try:
            os.write(descriptor, protected)
        finally:
            os.close(descriptor)
        original_link(
            source,
            destination,
            src_dir_fd=src_dir_fd,
            dst_dir_fd=dst_dir_fd,
            follow_symlinks=follow_symlinks,
        )

    monkeypatch.setattr(publisher.os, "link", create_competing_target)
    if route == "v19":
        operation = lambda: publisher.publish_v19(
            root,
            candidate_revision=revision,
            check=False,
        )
    else:
        accepted = json.loads(
            publisher.candidate_blob(
                ROOT,
                publisher.APPROVED_V20_PUBLICATION,
                publisher.V20_PATH,
                "TEST",
            )
        )
        operation = lambda: publisher.publish_v20(
            root,
            entry_revision=revision,
            owner_identity=accepted["ownerDecision"]["identity"],
            decided_at_utc=accepted["ownerDecision"]["decidedAtUtc"],
            rationale=accepted["ownerDecision"]["rationale"],
            check=False,
        )
    assert_error(code, operation)
    assert target.read_bytes() == protected
    assert not tuple(target.parent.glob(f".{target.name}.*.tmp"))


def test_run_git_ignores_replacement_refs(tmp_path: Path) -> None:
    """Read the accepted commit graph without caller-created replacement objects."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    publication = publisher.APPROVED_V20_PUBLICATION
    parent = git(root, "rev-parse", f"{publication}^")
    expected_tree = git(root, "--no-replace-objects", "show", "-s", "--format=%T", publication)
    subprocess.run(["git", "-C", str(root), "replace", publication, parent], check=True)
    assert git(root, "show", "-s", "--format=%T", publication) != expected_tree
    observed = publisher.run_git(root, "show", "-s", "--format=%T", publication).stdout.decode().strip()
    assert observed == expected_tree


def test_publication_history_failure_uses_the_route_specific_code(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Remap shared Git failures at publication discovery to the caller's stable route code."""

    original = publisher.run_git

    def fail_log(root: Path, *arguments: str, **kwargs: object) -> object:
        if arguments and arguments[0] == "log":
            raise publisher.SuccessorAuthorityError("SUCCESSOR_HISTORY_UNAVAILABLE", "injected history failure", "BLOCKED")
        return original(root, *arguments, **kwargs)

    monkeypatch.setattr(publisher, "run_git", fail_log)
    assert_error(
        "V21_ROUTE_HISTORY_UNAVAILABLE",
        lambda: publisher.publication_candidates(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.V21_PATH,
            "V21_ROUTE_HISTORY_UNAVAILABLE",
        ),
        state="BLOCKED",
    )


def test_path_resolution_failure_is_structured_and_nonvacuous(tmp_path: Path) -> None:
    """Convert recursive symlink resolution exhaustion to SUCCESSOR_PATH_ESCAPE/BLOCKED."""

    root = tmp_path / "repository"
    root.mkdir()
    (root / "loop").symlink_to("loop")
    assert_error(
        "SUCCESSOR_PATH_ESCAPE",
        lambda: publisher.worktree_path(root, "loop/result.json"),
        state="BLOCKED",
    )


def test_recursive_json_and_schema_exhaustion_are_structured() -> None:
    """Keep hostile recursive inputs inside the stable JSON/schema result boundary."""

    depth = 10_000
    nested_document = ("{\"a\":" * depth + "0" + "}" * depth).encode()
    assert_error(
        "TEST_JSON_INVALID",
        lambda: publisher.parse_json(nested_document, "TEST_JSON_INVALID"),
    )
    nested_schema = ("{\"type\":\"array\",\"items\":" * depth + "{}" + "}" * depth).encode()
    assert_error(
        "TEST_SCHEMA_INVALID",
        lambda: publisher.validate_json_schema(nested_schema, {}, "TEST_SCHEMA_INVALID"),
    )


def test_replacing_atomic_write_helper_is_not_exposed() -> None:
    """Keep every live authority publisher on the no-clobber descriptor primitive."""

    assert not hasattr(publisher, "write_atomic")


def test_inventory_check_uses_committed_bytes_when_worktree_is_dirty(tmp_path: Path) -> None:
    """Ignore dirty worktree inventory/schema substitutes during revision checks."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    for path in (publisher.INVENTORY_PATH, publisher.INVENTORY_SCHEMA_PATH):
        target = root / path
        target.write_bytes(target.read_bytes() + b"\nworktree-only drift\n")
    ledger: list[dict[str, str]] = []
    publisher.inventory_route(
        root,
        check=True,
        evaluated_revision=publisher.APPROVED_V20_PUBLICATION,
        assertion_ledger=ledger,
    )
    assert ledger


def test_inventory_check_ignores_unused_worktree_symlink(tmp_path: Path) -> None:
    """Validate committed inventory bytes without resolving the worktree target."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    outside = tmp_path / "outside-inventory.json"
    outside.write_text("not authority evidence\n", encoding="utf-8")
    target = root / publisher.INVENTORY_PATH
    target.unlink()
    target.symlink_to(outside)
    ledger: list[dict[str, str]] = []
    publisher.inventory_route(
        root,
        check=True,
        evaluated_revision=publisher.APPROVED_V20_PUBLICATION,
        assertion_ledger=ledger,
    )
    assert ledger


def test_atomic_write_parent_failure_has_stable_blocked_result(tmp_path: Path) -> None:
    """Convert parent-directory creation errors into the requested stable result."""

    root = tmp_path / "repository"
    root.mkdir()
    parent = root / "occupied"
    parent.write_text("regular file blocks directory creation\n", encoding="utf-8")
    assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "occupied/result.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )


@pytest.mark.parametrize(
    "failure",
    ("create", "link", "file-fsync", "directory-fsync", "recheck", "unlink"),
)
def test_live_no_clobber_failures_are_blocked_and_rolled_back(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    failure: str,
) -> None:
    """Roll back only before link; preserve the installed target after the commit point."""

    root = tmp_path / "repository"
    root.mkdir()
    target = root / "nested/result.json"
    original_open = publisher.os.open
    original_fsync = publisher.os.fsync
    original_unlink = publisher.os.unlink
    fsync_calls = 0
    unlink_failed = False

    if failure == "create":
        def fail_create(path: object, flags: int, *args: object, **kwargs: object) -> int:
            if flags & os.O_CREAT:
                raise OSError("create failure")
            return original_open(path, flags, *args, **kwargs)

        monkeypatch.setattr(publisher.os, "open", fail_create)
    elif failure == "link":
        monkeypatch.setattr(
            publisher.os,
            "link",
            lambda *_args, **_kwargs: (_ for _ in ()).throw(OSError("link failure")),
        )
    elif failure in {"file-fsync", "directory-fsync"}:
        def fail_fsync(descriptor: int) -> None:
            nonlocal fsync_calls
            fsync_calls += 1
            expected_call = 1 if failure == "file-fsync" else 2
            if fsync_calls == expected_call:
                raise OSError(f"{failure} failure")
            original_fsync(descriptor)

        monkeypatch.setattr(publisher.os, "fsync", fail_fsync)
    elif failure == "recheck":
        monkeypatch.setattr(
            publisher,
            "open_relative_file",
            lambda *_args, **_kwargs: (_ for _ in ()).throw(OSError("recheck failure")),
        )
    else:
        def fail_first_temporary_unlink(path: str, *, dir_fd: int) -> None:
            nonlocal unlink_failed
            if path.startswith(".result.json.") and not unlink_failed:
                unlink_failed = True
                raise OSError("unlink failure")
            original_unlink(path, dir_fd=dir_fd)

        monkeypatch.setattr(publisher.os, "unlink", fail_first_temporary_unlink)

    error = assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "nested/result.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert failure.split("-")[0] in error.detail
    if failure in {"directory-fsync", "recheck", "unlink"}:
        assert target.read_bytes() == b"{}\n"
    else:
        assert not target.exists()
    assert not tuple(target.parent.glob(".result.json.*.tmp"))


def test_atomic_cleanup_preserves_target_replaced_after_link(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Never unlink a competing inode installed after the publication hard link."""

    root = tmp_path / "repository"
    root.mkdir()
    target = root / "nested/result.json"
    protected = b'{"concurrent":true}\n'
    original_fsync = publisher.os.fsync
    fsync_calls = 0

    def replace_target_then_fail(descriptor: int) -> None:
        nonlocal fsync_calls
        fsync_calls += 1
        if fsync_calls == 2:
            target.unlink()
            target.write_bytes(protected)
            raise OSError("directory fsync failure after target replacement")
        original_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", replace_target_then_fail)
    error = assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "nested/result.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert "directory fsync failure after target replacement" in error.detail
    assert target.read_bytes() == protected
    assert not tuple(target.parent.glob(".result.json.*.tmp"))


@pytest.mark.parametrize("route", ("v19", "v20", "v21", "inventory"))
def test_each_live_publication_route_blocks_parent_swap_without_outside_residual(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    route: str,
) -> None:
    """Keep a post-validation directory swap from redirecting any authority publisher."""

    if route == "v19":
        root = clone_at(tmp_path, publisher.APPROVED_CHECKPOINT_COMMIT)
        operation = lambda: publisher.publish_v19(
            root,
            candidate_revision=publisher.APPROVED_CHECKPOINT_COMMIT,
            check=False,
        )
        path = publisher.V19_PATH
        code = "V19_WRITE_FAILED"
    elif route == "v20":
        root = clone_at(tmp_path, publisher.APPROVED_ENTRY_COMMIT)
        accepted = json.loads(
            publisher.candidate_blob(ROOT, publisher.APPROVED_V20_PUBLICATION, publisher.V20_PATH, "TEST")
        )
        operation = lambda: publisher.publish_v20(
            root,
            entry_revision=publisher.APPROVED_ENTRY_COMMIT,
            owner_identity=accepted["ownerDecision"]["identity"],
            decided_at_utc=accepted["ownerDecision"]["decidedAtUtc"],
            rationale=accepted["ownerDecision"]["rationale"],
            check=False,
        )
        path = publisher.V20_PATH
        code = "V20_WRITE_FAILED"
    elif route == "v21":
        root, tooling = stage_tooling(tmp_path)
        operation = lambda: publisher.publish_v21(root, candidate_revision=tooling, check=False)
        path = publisher.V21_PATH
        code = "V21_WRITE_FAILED"
    else:
        root = clone_at(tmp_path, publisher.APPROVED_TOOLING_COMMIT)
        path = publisher.INVENTORY_PATH
        (root / path).unlink()
        operation = lambda: publisher.inventory_route(root, check=False)
        code = "V20_INVENTORY_WRITE_FAILED"

    target = root / path
    parent = target.parent
    saved_parent = root / "saved-publication-parent"
    outside = tmp_path / f"outside-{route}"
    outside.mkdir()
    original_link = publisher.os.link
    swapped = False

    def swap_then_link(*args: object, **kwargs: object) -> None:
        nonlocal swapped
        if not swapped:
            parent.rename(saved_parent)
            parent.symlink_to(outside, target_is_directory=True)
            swapped = True
        original_link(*args, **kwargs)

    monkeypatch.setattr(publisher.os, "link", swap_then_link)
    assert_error(code, operation, state="BLOCKED")
    assert not (outside / target.name).exists()
    assert (saved_parent / target.name).is_file()
    assert not tuple(saved_parent.glob(f".{target.name}.*.tmp"))


def test_inventory_write_is_idempotent_and_refuses_different_existing_bytes(tmp_path: Path) -> None:
    """Preserve existing inventory evidence unless its bytes are already canonical."""

    root = clone_at(tmp_path, publisher.APPROVED_TOOLING_COMMIT)
    target = root / publisher.INVENTORY_PATH
    expected = target.read_bytes()
    publisher.inventory_route(root, check=False)
    assert target.read_bytes() == expected
    target.write_bytes(b'{"protected":true}\n')
    protected = target.read_bytes()
    assert_error(
        "V20_INVENTORY_OVERWRITE_REFUSED",
        lambda: publisher.inventory_route(root, check=False),
    )
    assert target.read_bytes() == protected


@pytest.mark.parametrize("identical", (False, True))
def test_inventory_atomic_publication_preserves_concurrent_bytes(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    identical: bool,
) -> None:
    """Accept only identical concurrent inventory publication and never clobber bytes."""

    root = clone_at(tmp_path, publisher.APPROVED_TOOLING_COMMIT)
    target = root / publisher.INVENTORY_PATH
    expected = target.read_bytes()
    target.unlink()
    protected = expected if identical else b'{"concurrent":true}\n'
    original_link = publisher.os.link

    def create_competing_target(
        source: str,
        destination: str,
        *,
        src_dir_fd: int,
        dst_dir_fd: int,
        follow_symlinks: bool,
    ) -> None:
        descriptor = os.open(destination, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644, dir_fd=dst_dir_fd)
        try:
            os.write(descriptor, protected)
        finally:
            os.close(descriptor)
        original_link(
            source,
            destination,
            src_dir_fd=src_dir_fd,
            dst_dir_fd=dst_dir_fd,
            follow_symlinks=follow_symlinks,
        )

    monkeypatch.setattr(publisher.os, "link", create_competing_target)
    if identical:
        publisher.inventory_route(root, check=False)
    else:
        assert_error(
            "V20_INVENTORY_OVERWRITE_REFUSED",
            lambda: publisher.inventory_route(root, check=False),
        )
    assert target.read_bytes() == protected
    assert not tuple(target.parent.glob(f".{target.name}.*.tmp"))


def test_atomic_write_descriptor_cleanup_failure_preserves_original_blocker(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Keep descriptor-close errors from replacing the original stable write failure."""

    root = tmp_path / "repository"
    root.mkdir()

    def fail_fchmod(_descriptor: int, _mode: int) -> None:
        nonlocal primary_failed
        primary_failed = True
        raise OSError("primary descriptor write failure")

    original_close = publisher.os.close
    primary_failed = False

    def fail_close(descriptor: int) -> None:
        if primary_failed:
            raise OSError("secondary descriptor close failure")
        original_close(descriptor)

    monkeypatch.setattr(publisher.os, "fchmod", fail_fchmod)
    monkeypatch.setattr(publisher.os, "close", fail_close)
    error = assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "result.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert "primary descriptor write failure" in error.detail


def test_atomic_write_temporary_cleanup_failure_preserves_replacement_blocker(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Keep temporary-unlink errors from replacing a failed atomic replacement."""

    root = tmp_path / "repository"
    root.mkdir()

    def fail_link(*_args: object, **_kwargs: object) -> None:
        raise OSError("primary link failure")

    def fail_unlink(_path: str, *, dir_fd: int) -> None:
        del dir_fd
        raise OSError("secondary temporary cleanup failure")

    monkeypatch.setattr(publisher.os, "link", fail_link)
    monkeypatch.setattr(publisher.os, "unlink", fail_unlink)
    error = assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "result.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert "primary link failure" in error.detail


def test_atomic_write_link_failure_is_stable_and_cleans_temporary_file(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Convert link failure to BLOCKED and remove its temporary file."""

    root = tmp_path / "repository"
    root.mkdir()

    def fail_link(*_args: object, **_kwargs: object) -> None:
        raise OSError("link failure")

    monkeypatch.setattr(publisher.os, "link", fail_link)
    error = assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "result.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert "link failure" in error.detail
    assert not (root / "result.json").exists()
    assert list(root.glob(".result.json.*.tmp")) == []


def test_checkpoint_temporary_directory_cleanup_failure_is_blocked(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Convert isolated-checkout cleanup failure into a stable nonempty blocker."""

    original_temporary_directory = publisher.tempfile.TemporaryDirectory

    class CleanupFailure(original_temporary_directory):
        def cleanup(self) -> None:
            super().cleanup()
            raise OSError("checkpoint temporary cleanup failure")

    monkeypatch.setattr(publisher.tempfile, "TemporaryDirectory", CleanupFailure)
    error = assert_error(
        "V19_RESULT_TEMPORARY_CLEANUP_FAILED",
        lambda: publisher.rerun_checkpoint_command(ROOT, publisher.APPROVED_CHECKPOINT_COMMIT),
        state="BLOCKED",
    )
    assert "checkpoint temporary cleanup failure" in error.detail


@pytest.mark.parametrize(
    ("path", "invalid_bytes"),
    (
        (publisher.INVENTORY_PATH, b"{}\n"),
        (publisher.INVENTORY_SCHEMA_PATH, b"{\n"),
    ),
)
def test_inventory_check_rejects_committed_invalid_bytes_behind_valid_worktree(
    tmp_path: Path,
    path: str,
    invalid_bytes: bytes,
) -> None:
    """Never validate a clean worktree copy in place of the evaluated commit blob."""

    root = clone_at(tmp_path, publisher.APPROVED_V20_PUBLICATION)
    target = root / path
    valid_bytes = target.read_bytes()
    target.write_bytes(invalid_bytes)
    subprocess.run(["git", "-C", str(root), "add", "--", path], check=True)
    invalid_commit = commit(root, "test: commit invalid inventory fixture")
    target.write_bytes(valid_bytes)
    assert target.read_bytes() == valid_bytes
    assert_error(
        "V20_INVENTORY_SCHEMA_INVALID",
        lambda: publisher.inventory_route(root, check=True, evaluated_revision=invalid_commit),
    )


def test_historical_observation_digest_fault_turns_inventory_red(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Exercise the independent historical-observation digest predicate."""

    v19 = publisher.publish_v19(ROOT, candidate_revision=publisher.APPROVED_V20_PUBLICATION, check=True)
    v19_bytes = publisher.candidate_blob(ROOT, publisher.APPROVED_V19_PUBLICATION, publisher.V19_PATH, "TEST")
    monkeypatch.setattr(
        publisher,
        "check_v19_at",
        lambda _root, _evaluated: (v19, publisher.APPROVED_V19_PUBLICATION, v19_bytes),
    )
    original = publisher.candidate_blob

    def historical_fault(root: Path, candidate: str, path: str, code: str) -> bytes:
        content = original(root, candidate, path, code)
        if candidate == publisher.APPROVED_TOOLING_COMMIT and path == publisher.V9_BUNDLE_PATH:
            return content + b"historical fault"
        return content

    monkeypatch.setattr(publisher, "candidate_blob", historical_fault)
    assert_error(
        "V20_FIXED_INPUT_DRIFT",
        lambda: publisher.validate_inventory_observations(ROOT, publisher.APPROVED_V20_PUBLICATION),
    )


def test_checkpoint_binding_digest_fault_turns_inventory_red(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Exercise the independent historical checkpoint-binding digest predicate."""

    v19 = publisher.publish_v19(ROOT, candidate_revision=publisher.APPROVED_V20_PUBLICATION, check=True)
    v19_bytes = publisher.candidate_blob(ROOT, publisher.APPROVED_V19_PUBLICATION, publisher.V19_PATH, "TEST")
    monkeypatch.setattr(
        publisher,
        "check_v19_at",
        lambda _root, _evaluated: (v19, publisher.APPROVED_V19_PUBLICATION, v19_bytes),
    )
    original = publisher.candidate_blob
    checkpoint_path = publisher.CHECKPOINT_PATHS[0]

    def checkpoint_fault(root: Path, candidate: str, path: str, code: str) -> bytes:
        content = original(root, candidate, path, code)
        if candidate == publisher.APPROVED_CHECKPOINT_COMMIT and path == checkpoint_path:
            return content + b"checkpoint fault"
        return content

    monkeypatch.setattr(publisher, "candidate_blob", checkpoint_fault)
    assert_error(
        "V20_CHECKPOINT_INPUT_DRIFT",
        lambda: publisher.validate_inventory_observations(ROOT, publisher.APPROVED_V20_PUBLICATION),
    )


def test_v21_descendant_gitlink_drift_has_a_stable_code(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Derive forbidden gitlink drift from raw mode 160000 records."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "repository"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
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
    descendant = commit(root, "test: drift a root gitlink")
    assert_error(
        "V21_DESCENDANT_GITLINK_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=descendant, check=True),
    )


@pytest.mark.parametrize(
    ("mutation", "code"),
    (
        ("comment", "V21_PREFLIGHT_WIRING_INVALID"),
        ("step-if", "V21_PREFLIGHT_WIRING_INVALID"),
        ("continue-on-error", "V21_PREFLIGHT_WIRING_INVALID"),
        ("conditional-effective-hold", "V21_PREFLIGHT_WIRING_INVALID"),
        ("job-if", "V21_PREFLIGHT_JOB_INVALID"),
        ("job-continue", "V21_PREFLIGHT_JOB_INVALID"),
        ("job-if-after-steps", "V21_PREFLIGHT_JOB_INVALID"),
        ("job-continue-after-steps", "V21_PREFLIGHT_JOB_INVALID"),
        ("job-needs-after-steps", "V21_PREFLIGHT_JOB_INVALID"),
        ("workflow-trigger", "V21_PREFLIGHT_TRIGGER_INVALID"),
        ("duplicate-trigger", "V21_PREFLIGHT_WIRING_INVALID"),
        ("on-block-scalar", "V21_PREFLIGHT_TRIGGER_INVALID"),
        ("top-level-env", "V21_PREFLIGHT_WIRING_INVALID"),
        ("top-level-defaults", "V21_PREFLIGHT_TRIGGER_INVALID"),
        ("nested-duplicate-uses", "V21_PREFLIGHT_WIRING_INVALID"),
        ("scalar-step", "V21_PREFLIGHT_WIRING_INVALID"),
        ("flow-sequence-step", "V21_PREFLIGHT_WIRING_INVALID"),
        ("flow-mapping-step", "V21_PREFLIGHT_WIRING_INVALID"),
        ("candidate-test-before-successor", "V21_PREFLIGHT_WIRING_INVALID"),
        ("candidate-python-alias-before-successor", "V21_PREFLIGHT_WIRING_INVALID"),
        ("worktree-successor-verifier", "V21_PREFLIGHT_WIRING_INVALID"),
    ),
)
def test_preflight_rejects_disabled_conditional_or_allowed_to_fail_commands(
    mutation: str,
    code: str,
) -> None:
    """Prove the workflow, job, and successor step remain active and fail-closed."""

    content = (ROOT / publisher.PREFLIGHT_PATH).read_bytes()
    publisher.validate_preflight_workflow(content)
    text = content.decode("utf-8")
    if mutation == "comment":
        text = text.replace(
            "            uv run --frozen --no-sync python3 \"$successor_verifier\"",
            "            # uv run --frozen --no-sync python3 \"$successor_verifier\"",
            1,
        )
    elif mutation == "step-if":
        text = text.replace(
            "      - name: Verify Story 7.1 successor authorities and effective hold\n",
            "      - name: Verify Story 7.1 successor authorities and effective hold\n        if: false\n",
        )
    elif mutation == "continue-on-error":
        text = text.replace(
            "      - name: Verify Story 7.1 successor authorities and effective hold\n",
            "      - name: Verify Story 7.1 successor authorities and effective hold\n        continue-on-error: true\n",
        )
    elif mutation == "conditional-effective-hold":
        command = (
            "          uv run --frozen --no-sync python3 \"$successor_verifier\" "
            "--repository . v21 --candidate HEAD --effective-hold --check\n"
        )
        text = text.replace(command, f"          if false; then\n  {command}          fi\n")
    elif mutation == "job-if":
        text = text.replace(
            "  planning-authority:\n    runs-on: ubuntu-latest\n",
            "  planning-authority:\n    if: false\n    runs-on: ubuntu-latest\n",
        )
    elif mutation == "job-continue":
        text = text.replace(
            "  planning-authority:\n    runs-on: ubuntu-latest\n",
            "  planning-authority:\n    continue-on-error: true\n    runs-on: ubuntu-latest\n",
        )
    elif mutation == "job-if-after-steps":
        text += "    if: false\n"
    elif mutation == "job-continue-after-steps":
        text += "    continue-on-error: true\n"
    elif mutation == "job-needs-after-steps":
        text += "    needs: untrusted-job\n"
    elif mutation == "workflow-trigger":
        text = text.replace("on:\n  pull_request:\n", "on:\n  # pull_request disabled\n")
    elif mutation == "duplicate-trigger":
        text += "on:\n  workflow_dispatch:\n"
    elif mutation == "on-block-scalar":
        text = text.replace("on:\n  pull_request:\n", "on: |\n  pull_request:\n")
    elif mutation == "top-level-env":
        text = text.replace(
            "name: Planning authority preflight\n",
            "name: Planning authority preflight\nenv: {SHELLOPTS: noexec}\n",
        )
    elif mutation == "top-level-defaults":
        text = text.replace(
            "name: Planning authority preflight\n",
            "name: Planning authority preflight\ndefaults:\n  run:\n    shell: false\n",
        )
    elif mutation == "nested-duplicate-uses":
        text = text.replace(
            "        uses: actions/checkout@11d5960a326750d5838078e36cf38b85af677262\n",
            "        uses: actions/checkout@11d5960a326750d5838078e36cf38b85af677262\n"
            "        uses: actions/checkout@v3\n",
            1,
        )
    elif mutation in {"scalar-step", "flow-sequence-step", "flow-mapping-step"}:
        item = {
            "scalar-step": "      - bogus\n",
            "flow-sequence-step": "      - [name, bogus]\n",
            "flow-mapping-step": "      - {name: bogus, run: false}\n",
        }[mutation]
        text = text.replace(
            "      - name: Verify Story 7.1 successor authorities and effective hold\n",
            item + "      - name: Verify Story 7.1 successor authorities and effective hold\n",
        )
    elif mutation == "candidate-test-before-successor":
        text = text.replace(
            "          uv sync --frozen\n\n"
            "      - name: Verify Story 7.1 successor authorities and effective hold\n",
            "          uv sync --frozen\n\n"
            "      - name: Run candidate-controlled tests too early\n"
            "        run: uv run --frozen --no-sync python3 -m pytest -q\n\n"
            "      - name: Verify Story 7.1 successor authorities and effective hold\n",
        )
    elif mutation == "candidate-python-alias-before-successor":
        text = text.replace(
            "          npx --no-install commitlint\n",
            "          python _bmad/scripts/generate_story_record.py\n",
        )
    else:
        text = text.replace(
            "trusted_git show \"$SOURCE_WORKFLOW_SHA:$publisher_path\"",
            "cp _bmad/scripts/publish_story_7_1_successor_authorities.py",
        )
    assert_error(
        code,
        lambda: publisher.validate_preflight_workflow(text.encode("utf-8")),
    )


def test_preflight_validation_is_independent_of_mapping_order() -> None:
    """Accept equivalent trigger and job mappings in a different key order."""

    text = (ROOT / publisher.PREFLIGHT_PATH).read_text(encoding="utf-8")
    text = text.replace(
        "on:\n  pull_request:\n  push:\n    branches: [main]\n  workflow_dispatch:\n",
        "on:\n  workflow_dispatch:\n  push:\n    branches: [main]\n  pull_request:\n",
    )
    text = text.replace(
        "  planning-authority:\n    runs-on: ubuntu-latest\n    timeout-minutes: 30\n    steps:\n",
        "  planning-authority:\n    timeout-minutes: 30\n    runs-on: ubuntu-latest\n    steps:\n",
    )
    publisher.validate_preflight_workflow(text.encode("utf-8"))


@pytest.mark.parametrize(
    ("mutation", "code"),
    (
        ("extra-job", "V21_PREFLIGHT_JOB_INVALID"),
        ("write-permissions", "V21_PREFLIGHT_PERMISSION_INVALID"),
        ("unpinned-checkout", "V21_PREFLIGHT_WIRING_INVALID"),
        ("local-action", "V21_PREFLIGHT_WIRING_INVALID"),
        ("direct-shebang", "V21_PREFLIGHT_WIRING_INVALID"),
        ("github-env", "V21_PREFLIGHT_WIRING_INVALID"),
        ("npm-lifecycle", "V21_PREFLIGHT_WIRING_INVALID"),
        ("candidate-commitlint-before-bootstrap", "V21_PREFLIGHT_WIRING_INVALID"),
        ("dependency-sync-before-bootstrap", "V21_PREFLIGHT_WIRING_INVALID"),
        ("extra-checkout-key", "V21_PREFLIGHT_WIRING_INVALID"),
    ),
)
def test_preflight_exact_allowlist_rejects_prebootstrap_execution_and_enclosure_drift(
    mutation: str,
    code: str,
) -> None:
    """Pin the sole job, least privilege, action revisions, mapping shapes, and bootstrap order."""

    text = (ROOT / publisher.PREFLIGHT_PATH).read_text(encoding="utf-8")
    if mutation == "extra-job":
        text += "  bypass:\n    runs-on: ubuntu-latest\n    steps:\n      - run: _bmad/scripts/generate_story_record.py\n"
    elif mutation == "write-permissions":
        text = text.replace("permissions:\n  contents: read\n", "permissions:\n  contents: write\n  actions: write\n")
    elif mutation == "unpinned-checkout":
        text = text.replace("actions/checkout@11d5960a326750d5838078e36cf38b85af677262", "actions/checkout@v4", 1)
    elif mutation == "local-action":
        text = text.replace(
            "actions/checkout@11d5960a326750d5838078e36cf38b85af677262",
            "./candidate-action",
            1,
        )
    elif mutation == "direct-shebang":
        marker = "      - name: Bootstrap Story 7.1 successor authority boundary\n"
        text = text.replace(
            marker,
            "      - name: Execute candidate script before bootstrap\n"
            "        run: _bmad/scripts/generate_story_record.py\n\n"
            + marker,
        )
    elif mutation == "github-env":
        text = text.replace(
            "          set -euo pipefail\n",
            "          printf 'PYTHONPATH=%s\\n' '$PWD' >> \"$GITHUB_ENV\"\n",
            1,
        )
    elif mutation == "npm-lifecycle":
        text = text.replace("run: npm ci --ignore-scripts", "run: npm ci")
    elif mutation == "candidate-commitlint-before-bootstrap":
        marker = "      - name: Bootstrap Story 7.1 successor authority boundary\n"
        text = text.replace(
            marker,
            "      - name: Load candidate commitlint configuration too early\n"
            "        run: npx commitlint --config commitlint.config.mjs --from HEAD^ --to HEAD\n\n"
            + marker,
        )
    elif mutation == "dependency-sync-before-bootstrap":
        marker = "      - name: Bootstrap Story 7.1 successor authority boundary\n"
        text = text.replace(
            marker,
            "      - name: Synchronize candidate dependencies too early\n"
            "        run: uv sync --frozen\n\n"
            + marker,
        )
    else:
        text = text.replace(
            "          fetch-depth: 0\n",
            "          fetch-depth: 0\n          persist-credentials: true\n",
            1,
        )
    assert_error(code, lambda: publisher.validate_preflight_workflow(text.encode()))


def test_bootstrap_uses_immutable_tooling_at_tooling_v21_and_descendant_lifecycle_points(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Validate dependency-free bootstrap source selection and protected descendant history."""

    source, tooling, publication = v21_repository
    tooling_result = publisher.bootstrap_successor_boundary(source, tooling)
    assert tooling_result["result"] == "PASS"
    assert tooling_result["tooling"] == tooling
    assert tooling_result["effectiveHold"] == "ACTIVE"
    publication_result = publisher.bootstrap_successor_boundary(source, publication)
    assert publication_result["tooling"] == tooling
    assert publication_result["publication"] == publication
    assert publication_result["effectiveHold"] == "LIFTED"

    root = tmp_path / "descendant"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    allowed = root / publisher.RESULT_PATHS[0]
    allowed.parent.mkdir(parents=True, exist_ok=True)
    allowed.write_text("allowed Story 7.1 result\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "-f", "--", publisher.RESULT_PATHS[0]], check=True)
    descendant = commit(root, "test: add allowed Story 7.1 result")
    result = publisher.bootstrap_successor_boundary(root, descendant)
    assert result["tooling"] == tooling
    assert result["effectiveHold"] == "LIFTED"
    assert result["assertionLedger"]


def test_shell_bootstrap_rejects_extra_prepublication_commit_before_publisher_execution(
    tmp_path: Path,
) -> None:
    """Prove an invalid no-V21 HEAD cannot become an executable publisher trust root."""

    root, tooling = stage_tooling(tmp_path)
    commit_malicious_publisher(root, "test: add malicious prepublication publisher")
    result = run_workflow_bootstrap(root, tooling)
    assert result.returncode != 0, result.stdout + result.stderr
    assert not (root / "malicious-publisher-executed").exists()


def test_shell_bootstrap_accepts_exact_tooling_and_publication_transactions(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Exercise the pre-execution shell proof at both valid lifecycle boundaries."""

    source, tooling, publication = v21_repository
    tooling_root = tmp_path / "tooling"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(tooling_root)], check=True)
    subprocess.run(["git", "-C", str(tooling_root), "checkout", "-q", "--detach", tooling], check=True)
    tooling_result = run_workflow_bootstrap(tooling_root, tooling)
    assert tooling_result.returncode == 0, tooling_result.stdout + tooling_result.stderr

    assert git(source, "rev-parse", "HEAD") == publication
    publication_result = run_workflow_bootstrap(source, tooling)
    assert publication_result.returncode == 0, publication_result.stdout + publication_result.stderr


def test_shell_bootstrap_rejects_fake_publication_parent_before_publisher_execution(
    tmp_path: Path,
) -> None:
    """Prove a fake V21 child cannot nominate its invalid parent as executable tooling."""

    root, tooling = stage_tooling(tmp_path)
    commit_malicious_publisher(root, "test: add malicious fake-publication parent")
    fake = root / publisher.V21_PATH
    fake.parent.mkdir(parents=True, exist_ok=True)
    fake.write_text("{}\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V21_PATH], check=True)
    commit(root, "test: add fake V21 publication")
    result = run_workflow_bootstrap(root, tooling)
    assert result.returncode != 0, result.stdout + result.stderr
    assert not (root / "malicious-publisher-executed").exists()


def test_checkpoint_stage_inventory_and_effective_hold_are_valid_and_active() -> None:
    """Preserve the real checkpoint lifecycle before V19 exists."""

    ledger: list[dict[str, str]] = []
    publisher.inventory_route(
        ROOT,
        check=True,
        evaluated_revision=publisher.APPROVED_CHECKPOINT_COMMIT,
        assertion_ledger=ledger,
    )
    assert ledger
    hold = publisher.effective_hold(ROOT, evaluated_revision=publisher.APPROVED_CHECKPOINT_COMMIT)
    assert hold["result"] == "PASS"
    assert hold["effectiveHold"] == "ACTIVE"
    assert hold["assertionLedger"]


def test_operator_v20_check_reports_decision_without_claiming_effective_lift(
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Keep the signed pre-V21 owner decision distinct from the effective hold."""

    assert publisher.main(
        ["--repository", str(ROOT), "v20", "--entry-candidate", publisher.APPROVED_V20_PUBLICATION, "--check"]
    ) == 0
    output = capsys.readouterr().out
    assert "DECISION=LIFTED" in output
    assert "EFFECTIVE_HOLD=LIFTED" not in output


def test_operator_v21_routes_report_active_then_lifted(
    v21_repository: tuple[Path, str, str],
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Cover lifecycle CLI output and exit semantics before and after V21."""

    root, _tooling, publication = v21_repository
    repository_args = ["--repository", str(root)]
    assert publisher.main(
        [*repository_args, "v21", "--candidate", publisher.APPROVED_V20_PUBLICATION, "--effective-hold", "--check"]
    ) == 0
    active = json.loads(capsys.readouterr().out)
    assert active["result"] == "PASS"
    assert active["effectiveHold"] == "ACTIVE"
    assert publisher.main([*repository_args, "v21", "--candidate", publication, "--check"]) == 0
    assert capsys.readouterr().out.startswith("V21_STORY_7_1_AUTHORITY_CORRECTION_OK")
    assert publisher.main(
        [*repository_args, "v21", "--candidate", publication, "--effective-hold", "--check"]
    ) == 0
    lifted = json.loads(capsys.readouterr().out)
    assert lifted["result"] == "PASS"
    assert lifted["effectiveHold"] == "LIFTED"


def test_operator_v21_direct_and_effective_hold_failures_are_nonvacuous_and_active(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Expose deleted publication failures through both CLI routes with an active hold."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "deleted-v21"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    subprocess.run(["git", "-C", str(root), "rm", "-q", "--", publisher.V21_PATH], check=True)
    deleted = commit(root, "test: delete V21 publication")
    arguments = ["--repository", str(root), "v21", "--candidate", deleted]

    assert publisher.main([*arguments, "--check"]) == 2
    direct = json.loads(capsys.readouterr().out)
    assert direct["result"] == "BLOCKED"
    assert direct["effectiveHold"] == "ACTIVE"
    assert direct["assertionLedger"]
    assert direct["blockers"]

    assert publisher.main([*arguments, "--effective-hold", "--check"]) == 2
    effective = json.loads(capsys.readouterr().out)
    assert effective["result"] == "BLOCKED"
    assert effective["effectiveHold"] == "ACTIVE"
    assert effective["assertionLedger"]
    assert effective["blockers"]


def test_bootstrap_cli_is_machine_readable_and_uses_the_tooling_parent(
    v21_repository: tuple[Path, str, str],
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Pin the stdlib bootstrap CLI contract used as the first post-checkout command."""

    root, tooling, publication = v21_repository
    assert publisher.main(
        ["--repository", str(root), "bootstrap", "--candidate", publication]
    ) == 0
    output = json.loads(capsys.readouterr().out)
    assert output["result"] == "PASS"
    assert output["tooling"] == tooling
    assert output["publication"] == publication
    assert output["effectiveHold"] == "LIFTED"
    assert output["assertionLedger"]


def test_workflow_extracts_both_verifiers_from_the_protected_source_commit() -> None:
    """Never execute candidate publisher bytes at either authority gate."""

    workflow = (ROOT / publisher.PREFLIGHT_PATH).read_text(encoding="utf-8")
    bootstrap_show = 'trusted_git show "$SOURCE_WORKFLOW_SHA:$publisher_path"'
    full_verifier_show = (
        'git --no-replace-objects show "$SOURCE_WORKFLOW_SHA:_bmad/scripts/'
        'publish_story_7_1_successor_authorities.py"'
    )
    assert workflow.count(bootstrap_show) == 1
    assert workflow.count(full_verifier_show) == 1
    assert "python3 -I -B \"$successor_bootstrap\"" in workflow
    assert "env -i \\\n" in workflow
    assert "${{ fromJSON(toJSON(job)).workflow_repository }}" in workflow
    assert "${{ fromJSON(toJSON(job)).workflow_ref }}" in workflow
    assert "${{ fromJSON(toJSON(job)).workflow_sha }}" in workflow
    assert "ci-trust" in workflow
    assert "$GITHUB_ENV" not in workflow
    assert "$GITHUB_PATH" not in workflow


def test_operator_v21_write_reports_generated_without_claiming_lift(
    tmp_path: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Reserve the effective-hold lift message for committed check mode."""

    root, tooling = stage_tooling(tmp_path)
    assert publisher.main(["--repository", str(root), "v21", "--candidate", tooling]) == 0
    output = capsys.readouterr().out
    assert "STATUS=GENERATED" in output
    assert "EFFECTIVE_HOLD=LIFTED" not in output
    assert (root / publisher.V21_PATH).is_file()
    assert git(root, "status", "--short", "--", publisher.V21_PATH).startswith("??")


def test_operator_inventory_check_reports_complete_machine_readable_result(
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Pin the complete inventory CLI JSON contract and nonempty PASS ledger."""

    assert publisher.main(["--repository", str(ROOT), "inventory", "--check"]) == 0
    output = json.loads(capsys.readouterr().out)
    assert output["schemaVersion"] == "hexalith.conversations.story-7.1-input-inventory-result.v1"
    assert output["route"] == "inventory"
    assert output["result"] == "PASS"
    assert output["inventories"] == len(publisher.expected_inventory_document()["scenarioInventories"]) + 4
    assert output["assertionLedger"]
    assert all(row["state"] == "PASS" for row in output["assertionLedger"])
    assert output["blockers"] == []


@pytest.mark.parametrize(
    "mutation",
    (
        "duplicate-key",
        "noncanonical",
        "schemaVersion",
        "authorityId",
        "predecessor",
        "acceptedLineage",
        "verifiedV20",
        "hardenedTooling",
        "resultSemantics",
        "result",
        "authorityEffect",
        "extra-top-level",
        "ledger-subject",
        "ledger-extra-field",
    ),
)
def test_exact_topology_bootstrap_rejects_every_malformed_v21_section(
    v21_repository: tuple[Path, str, str],
    mutation: str,
) -> None:
    """Reject noncanonical or semantically altered V21 bytes before package execution."""

    root, tooling, publication = v21_repository
    canonical = publisher.candidate_blob(root, publication, publisher.V21_PATH, "TEST")
    document = json.loads(canonical)
    expected_code = "V21_AUTHORITY_DRIFT"
    if mutation == "duplicate-key":
        content = canonical.replace(b"{\n", b'{\n  "result": "FAIL",\n', 1)
        expected_code = "V21_AUTHORITY_INVALID"
    elif mutation == "noncanonical":
        content = json.dumps(document, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    else:
        changed = deepcopy(document)
        if mutation in {
            "schemaVersion",
            "authorityId",
            "result",
        }:
            changed[mutation] = "altered"
        elif mutation in {
            "predecessor",
            "acceptedLineage",
            "verifiedV20",
            "hardenedTooling",
            "resultSemantics",
            "authorityEffect",
        }:
            changed[mutation]["unexpected"] = True
        elif mutation == "extra-top-level":
            changed["unexpected"] = True
        elif mutation == "ledger-subject":
            changed["assertionLedger"][0]["subject"] = "altered-subject"
        else:
            changed["assertionLedger"][0]["detail"] = "undeclared"
        content = publisher.json_bytes(changed)
    malformed_publication = commit_path_bytes(
        root,
        tooling,
        publisher.V21_PATH,
        content,
        f"test: publish malformed V21 {mutation}",
    )
    assert_error(
        expected_code,
        lambda: publisher.bootstrap_successor_boundary(root, malformed_publication),
    )


def test_unresolved_schema_reference_is_a_stable_schema_failure() -> None:
    """Convert jsonschema reference-resolution errors to the route's stable code."""

    schema = publisher.json_bytes(
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "$ref": "missing-local-schema.json",
        }
    )
    assert_error(
        "TEST_SCHEMA_REFERENCE_INVALID",
        lambda: publisher.validate_json_schema(schema, {}, "TEST_SCHEMA_REFERENCE_INVALID"),
    )


def test_deep_workflow_yaml_is_a_stable_wiring_failure() -> None:
    """Convert recursive YAML exhaustion to the fail-closed preflight code."""

    workflow = "\n".join("  " * depth + f"key{depth}:" for depth in range(2_000)).encode("utf-8")
    assert_error(
        "V21_PREFLIGHT_WIRING_INVALID",
        lambda: publisher.preflight_workflow_model(workflow),
    )


def test_v20_check_rejects_evaluated_descendant_mode_drift(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Keep each standalone authority route truthful about descendant mode/type drift."""

    source, _tooling, publication = v21_repository
    root = tmp_path / "mode-drift"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", publication], check=True)
    configure_fixture_identity(root)
    target = root / publisher.V20_PATH
    target.chmod(0o755)
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.V20_PATH], check=True)
    descendant = commit(root, "test: change V20 authority mode")
    assert_error(
        "V20_AUTHORITY_DESCENDANT_MODE_DRIFT",
        lambda: publisher.publish_v20(
            root,
            entry_revision=descendant,
            owner_identity=None,
            decided_at_utc=None,
            rationale=None,
            check=True,
        ),
    )


def test_missing_ssh_keygen_is_blocked_as_an_unavailable_trust_tool(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Distinguish an unavailable verifier from an invalid owner signature."""

    monkeypatch.setattr(publisher.shutil, "which", lambda _name: None)
    assert_error(
        "V20_TRUST_TOOL_UNAVAILABLE",
        lambda: publisher.verify_v20_publication_signature(
            ROOT,
            publisher.APPROVED_V20_PUBLICATION,
            publisher.TRUSTED_OWNER_IDENTITY,
        ),
        state="BLOCKED",
    )


@pytest.mark.parametrize("failure", ("dup", "traversal-close"))
def test_publication_parent_descriptor_acquisition_never_leaks(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    failure: str,
) -> None:
    """Close every descriptor acquired before a parent traversal failure."""

    root = tmp_path / "descriptor-repository"
    root.mkdir()
    before = set(os.listdir("/proc/self/fd"))
    if failure == "dup":
        monkeypatch.setattr(
            publisher.os,
            "dup",
            lambda _descriptor: (_ for _ in ()).throw(OSError("dup acquisition failure")),
        )
        relative_path = "result.json"
    else:
        original_close = publisher.os.close
        injected = False

        def fail_first_close(descriptor: int) -> None:
            nonlocal injected
            if not injected:
                injected = True
                raise OSError("traversal close failure")
            original_close(descriptor)

        monkeypatch.setattr(publisher.os, "close", fail_first_close)
        relative_path = "nested/result.json"
    assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            relative_path,
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert set(os.listdir("/proc/self/fd")) == before


def test_inventory_idempotence_rejects_a_symlink_swap_before_descriptor_read(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Never reopen a validated inventory pathname through a concurrent symlink."""

    root = clone_at(tmp_path, publisher.APPROVED_TOOLING_COMMIT)
    target = root / publisher.INVENTORY_PATH
    saved = target.with_name("saved-inventory.json")
    outside = tmp_path / "outside-inventory.json"
    outside.write_bytes(target.read_bytes())
    original_open = publisher.open_relative_file
    swapped = False

    def swap_before_open(root_descriptor: int, relative_path: str) -> int:
        nonlocal swapped
        if relative_path == publisher.INVENTORY_PATH and not swapped:
            target.rename(saved)
            target.symlink_to(outside)
            swapped = True
        return original_open(root_descriptor, relative_path)

    monkeypatch.setattr(publisher, "open_relative_file", swap_before_open)
    assert_error(
        "V20_INVENTORY_READ_FAILED",
        lambda: publisher.inventory_route(root, check=False),
        state="BLOCKED",
    )
    assert saved.is_file()
    assert target.is_symlink()


def test_unrelated_history_cannot_report_a_valid_active_lifecycle(tmp_path: Path) -> None:
    """Require approved lineage before expected authority absence can be not-applicable."""

    root = tmp_path / "unrelated"
    root.mkdir()
    subprocess.run(["git", "-C", str(root), "init", "-q"], check=True)
    configure_fixture_identity(root)
    (root / "README.md").write_text("unrelated\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "README.md"], check=True)
    commit(root, "test: create unrelated history")
    result = publisher.effective_hold(root)
    assert result["result"] == "BLOCKED"
    assert result["effectiveHold"] == "ACTIVE"
    assert result["assertionLedger"]
    assert result["blockers"][0]["code"] == "V21_LIFECYCLE_LINEAGE_INVALID"


def test_v21_publication_argument_requires_check_mode(capsys: pytest.CaptureFixture[str]) -> None:
    """Reject a revision selector that write mode cannot honor."""

    with pytest.raises(SystemExit) as captured:
        publisher.main(
            [
                "--repository",
                str(ROOT),
                "v21",
                "--candidate",
                "HEAD",
                "--publication",
                "HEAD",
            ]
        )
    assert captured.value.code == 2
    assert "--publication requires --check" in capsys.readouterr().err


def test_external_ruleset_trust_contract_remains_explicitly_blocked() -> None:
    """Record the external source binding without letting candidate YAML self-attest it."""

    result = publisher.ci_trust_boundary_result()
    assert result["result"] == "BLOCKED"
    assert result["sourceRepository"] == "Hexalith/Hexalith.Conversations"
    assert result["sourceRef"] == "refs/heads/main"
    assert result["workflowPath"] == publisher.PREFLIGHT_PATH
    assert result["assertionLedger"]
    assert result["blockers"] == [
        {
            "code": publisher.CI_RULESET_BLOCKER,
            "state": "BLOCKED",
            "detail": result["assertionLedger"][0]["detail"],
        }
    ]
    workflow = (ROOT / publisher.PREFLIGHT_PATH).read_text(encoding="utf-8")
    assert "candidate's copy is defense in depth only" in workflow


@pytest.mark.parametrize("path", publisher.V21_TOOLING_PATHS)
def test_protected_source_comparison_rejects_each_candidate_tooling_blob_before_execution(
    tmp_path: Path,
    path: str,
) -> None:
    """Compare every candidate tooling blob/mode with source bytes before candidate Python runs."""

    root, source_tooling = stage_tooling(tmp_path)
    subprocess.run(
        ["git", "-C", str(root), "checkout", "-q", "--detach", publisher.APPROVED_V20_PUBLICATION],
        check=True,
    )
    copy_v21_tooling(root)
    target = root / path
    if path == publisher.PUBLISHER_PATH:
        target.write_text(
            "from pathlib import Path\n"
            "Path('malicious-publisher-executed').write_text('executed\\n', encoding='utf-8')\n",
            encoding="utf-8",
        )
    elif path == publisher.PUBLISHER_TEST_PATH:
        target.write_text("# deliberately empty successor suite\n", encoding="utf-8")
    else:
        target.write_bytes(target.read_bytes() + b"\n")
    subprocess.run(["git", "-C", str(root), "add", "--", path], check=True)
    commit(root, f"test: alter candidate {Path(path).name}")
    result = run_workflow_bootstrap(root, source_tooling)
    assert result.returncode != 0, result.stdout + result.stderr
    assert not (root / "malicious-publisher-executed").exists()


def test_protected_source_comparison_rejects_candidate_tooling_mode_drift(tmp_path: Path) -> None:
    """Bind the regular-blob mode as well as every tooling path's bytes."""

    root, source_tooling = stage_tooling(tmp_path)
    subprocess.run(
        ["git", "-C", str(root), "checkout", "-q", "--detach", publisher.APPROVED_V20_PUBLICATION],
        check=True,
    )
    copy_v21_tooling(root)
    (root / publisher.PUBLISHER_TEST_PATH).chmod(0o755)
    subprocess.run(["git", "-C", str(root), "add", "--", publisher.PUBLISHER_TEST_PATH], check=True)
    commit(root, "test: alter candidate tooling mode")
    result = run_workflow_bootstrap(root, source_tooling)
    assert result.returncode != 0, result.stdout + result.stderr


@pytest.mark.parametrize(
    ("repository", "workflow_ref", "workflow_sha"),
    (
        ("attacker/repository", publisher.CI_RULESET_SOURCE_WORKFLOW_REF, None),
        (publisher.CI_RULESET_SOURCE_REPOSITORY, "attacker/repository/workflow.yml@refs/heads/main", None),
        (publisher.CI_RULESET_SOURCE_REPOSITORY, publisher.CI_RULESET_SOURCE_WORKFLOW_REF, "f" * 40),
    ),
)
def test_bootstrap_rejects_missing_or_mismatched_protected_source_identity(
    v21_repository: tuple[Path, str, str],
    repository: str,
    workflow_ref: str,
    workflow_sha: str | None,
) -> None:
    """Fail before candidate tooling can run when the protected job identity is unverifiable."""

    root, tooling, publication = v21_repository
    expected_code = "V21_SOURCE_WORKFLOW_SHA_UNAVAILABLE" if workflow_sha is not None else "V21_SOURCE_WORKFLOW_IDENTITY_INVALID"
    assert_error(
        expected_code,
        lambda: publisher.bootstrap_successor_boundary(
            root,
            publication,
            source_repository=repository,
            source_workflow_ref=workflow_ref,
            source_workflow_sha=workflow_sha or tooling,
        ),
        state="BLOCKED",
    )


@pytest.mark.parametrize(
    "revision",
    (
        publisher.APPROVED_V19_PUBLICATION,
        publisher.APPROVED_ENTRY_COMMIT,
        publisher.APPROVED_V20_PUBLICATION,
    ),
)
def test_protected_source_bootstrap_accepts_each_historical_active_stage(
    v21_repository: tuple[Path, str, str],
    revision: str,
) -> None:
    """Recognize valid V19, entry, and signed V20 states without requiring a candidate workflow mirror."""

    root, source_tooling, _publication = v21_repository
    result = publisher.bootstrap_successor_boundary(
        root,
        revision,
        source_repository=publisher.CI_RULESET_SOURCE_REPOSITORY,
        source_workflow_ref=publisher.CI_RULESET_SOURCE_WORKFLOW_REF,
        source_workflow_sha=source_tooling,
    )
    assert result["result"] == "PASS"
    assert result["effectiveHold"] == "ACTIVE"
    assert result["assertionLedger"]


def test_reordered_v21_document_fails_the_bootstrap_canonical_order(
    v21_repository: tuple[Path, str, str],
) -> None:
    """Use the same uniquely ordered complete expected V21 bytes in bootstrap and full verification."""

    root, tooling, publication = v21_repository
    document = json.loads(publisher.candidate_blob(root, publication, publisher.V21_PATH, "TEST"))
    reordered = {key: document[key] for key in reversed(tuple(document))}
    malformed = commit_path_bytes(
        root,
        tooling,
        publisher.V21_PATH,
        publisher.json_bytes(reordered),
        "test: reorder V21 top-level keys",
    )
    assert_error(
        "V21_AUTHORITY_DRIFT",
        lambda: publisher.bootstrap_successor_boundary(root, malformed),
    )
    assert_error(
        "V21_AUTHORITY_DRIFT",
        lambda: publisher.publish_v21(root, candidate_revision=malformed, check=True),
    )


@pytest.mark.parametrize("operation", ("read", "write"))
def test_filesystem_root_anchoring_rejects_a_swapped_repository_ancestor(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    operation: str,
) -> None:
    """Reject an ancestor swap between physical-root resolution and descriptor traversal."""

    container = tmp_path / "container"
    root = container / "repository"
    replacement = container / "replacement"
    saved = container / "saved"
    root.mkdir(parents=True)
    replacement.mkdir()
    (root / "input.json").write_bytes(b"inside\n")
    (replacement / "input.json").write_bytes(b"outside\n")
    original_open = publisher.os.open
    swapped = False

    def swap_final_component(path: object, flags: int, *args: object, **kwargs: object) -> int:
        nonlocal swapped
        if path == "repository" and kwargs.get("dir_fd") is not None and not swapped:
            root.rename(saved)
            replacement.rename(root)
            swapped = True
        return original_open(path, flags, *args, **kwargs)

    monkeypatch.setattr(publisher.os, "open", swap_final_component)
    if operation == "read":
        action = lambda: publisher.read_relative_file_no_follow(root, "input.json", "TEST_READ_FAILED")
    else:
        action = lambda: publisher.write_atomic_no_clobber(
            root,
            "output.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        )
    assert_error("SUCCESSOR_PATH_ESCAPE", action, state="BLOCKED")
    assert not (root / "output.json").exists()
    assert not (saved / "output.json").exists()


def test_existing_target_cleanup_is_idempotent_and_has_no_fabricated_ebadf(tmp_path: Path) -> None:
    """Refuse an existing publication without double-closing owned descriptors."""

    root = tmp_path / "repository"
    root.mkdir()
    target = root / "authority.json"
    target.write_bytes(b"existing\n")
    error = assert_error(
        "TEST_EXISTS",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "authority.json",
            b"new\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
    )
    assert "cleanup=" not in error.detail
    assert "Bad file descriptor" not in error.detail
    assert target.read_bytes() == b"existing\n"


def test_short_publication_writes_retry_until_exact_bytes(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Persist exact bytes when the operating system reports partial write progress."""

    root = tmp_path / "repository"
    root.mkdir()
    original_write = publisher.os.write

    def short_write(descriptor: int, content: bytes) -> int:
        return original_write(descriptor, content[:3])

    monkeypatch.setattr(publisher.os, "write", short_write)
    expected = b'{"authority":"complete"}\n'
    publisher.write_atomic_no_clobber(
        root,
        "authority.json",
        expected,
        exists_code="TEST_EXISTS",
        failure_code="TEST_WRITE_FAILED",
    )
    assert (root / "authority.json").read_bytes() == expected


def test_zero_progress_publication_blocks_without_installing_a_target(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Fail closed when a publication write cannot make progress."""

    root = tmp_path / "repository"
    root.mkdir()
    monkeypatch.setattr(publisher.os, "write", lambda _descriptor, _content: 0)
    assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "authority.json",
            b"{}\n",
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert not (root / "authority.json").exists()
    assert not tuple(root.glob(".authority.json.*.tmp"))


def test_final_directory_fsync_failure_keeps_the_committed_target(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Keep the installed authority after link success even when final directory durability fails."""

    root = tmp_path / "repository"
    root.mkdir()
    original_fsync = publisher.os.fsync
    calls = 0

    def fail_third_fsync(descriptor: int) -> None:
        nonlocal calls
        calls += 1
        if calls == 3:
            raise OSError("final directory fsync failure")
        original_fsync(descriptor)

    monkeypatch.setattr(publisher.os, "fsync", fail_third_fsync)
    expected = b"committed\n"
    assert_error(
        "TEST_WRITE_FAILED",
        lambda: publisher.write_atomic_no_clobber(
            root,
            "authority.json",
            expected,
            exists_code="TEST_EXISTS",
            failure_code="TEST_WRITE_FAILED",
        ),
        state="BLOCKED",
    )
    assert (root / "authority.json").read_bytes() == expected
    assert not tuple(root.glob(".authority.json.*.tmp"))


@pytest.mark.parametrize(
    ("route", "candidate_option"),
    (("v19", "--candidate"), ("v20", "--entry-candidate"), ("v21", "--candidate")),
)
def test_every_publication_selector_is_rejected_in_write_mode(
    route: str,
    candidate_option: str,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Reject publication selectors on every route that cannot honor them in write mode."""

    with pytest.raises(SystemExit) as captured:
        publisher.main(
            ["--repository", str(ROOT), route, candidate_option, "HEAD", "--publication", "HEAD"]
        )
    assert captured.value.code == 2
    assert f"{route} --publication requires --check" in capsys.readouterr().err


@pytest.mark.parametrize(
    ("route", "candidate_option"),
    (("v20", "--entry-candidate"), ("v21", "--candidate")),
)
def test_effective_hold_rejects_a_publication_selector(
    route: str,
    candidate_option: str,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """Reject a revision selector that effective-hold routing would otherwise discard."""

    with pytest.raises(SystemExit) as captured:
        publisher.main(
            [
                "--repository",
                str(ROOT),
                route,
                candidate_option,
                "HEAD",
                "--publication",
                "HEAD",
                "--effective-hold",
                "--check",
            ]
        )
    assert captured.value.code == 2
    assert "--publication cannot be combined with --effective-hold" in capsys.readouterr().err


@pytest.mark.parametrize(
    "mutation",
    (
        "absent",
        "unreadable",
        "wrong-source",
        "bypass",
        "wrong-path",
        "wrong-ref",
        "no-direct-push-protection",
    ),
)
def test_ci_trust_evidence_fails_closed_for_every_unverifiable_binding(
    mutation: str,
) -> None:
    """Keep external CI enforcement blocked unless every effective-ruleset fact is exact."""

    source_sha = "a" * 40
    content: bytes | None = active_ruleset_evidence(source_sha)
    source_repository = publisher.CI_RULESET_SOURCE_REPOSITORY
    document = json.loads(content)
    if mutation == "absent":
        content = None
    elif mutation == "unreadable":
        content = b"not-json"
    elif mutation == "wrong-source":
        source_repository = "attacker/repository"
    elif mutation == "bypass":
        document[0]["bypass_actors"] = [{"actor_type": "OrganizationAdmin"}]
    elif mutation == "wrong-path":
        document[0]["rules"][1]["parameters"]["workflows"][0]["path"] = ".github/workflows/no-op.yml"
    elif mutation == "wrong-ref":
        document[0]["rules"][1]["parameters"]["workflows"][0]["ref"] = "refs/heads/feature"
    else:
        document[0]["rules"] = document[0]["rules"][1:]
    if mutation not in {"absent", "unreadable"}:
        content = publisher.json_bytes(document)
    result = publisher.ci_trust_boundary_result(
        rulesets_content=content,
        source_repository=source_repository,
        source_workflow_ref=publisher.CI_RULESET_SOURCE_WORKFLOW_REF,
        source_workflow_sha=source_sha,
        source_repository_id=1234,
    )
    assert result["result"] == "BLOCKED"
    assert result["assertionLedger"]
    assert result["blockers"][0]["code"] == publisher.CI_RULESET_BLOCKER


def test_ci_trust_route_passes_only_complete_effective_organization_ruleset_evidence() -> None:
    """Accept an exact inherited active ruleset with no bypass and pull-request-only updates."""

    source_sha = "a" * 40
    result = publisher.ci_trust_boundary_result(
        rulesets_content=active_ruleset_evidence(source_sha),
        source_repository=publisher.CI_RULESET_SOURCE_REPOSITORY,
        source_workflow_ref=publisher.CI_RULESET_SOURCE_WORKFLOW_REF,
        source_workflow_sha=source_sha,
        source_repository_id=1234,
    )
    assert result["result"] == "PASS"
    assert len(result["assertionLedger"]) == 4
    assert result["blockers"] == []


def test_workflow_surfaces_external_ci_blocker_after_valid_historical_bootstrap(
    tmp_path: Path,
    v21_repository: tuple[Path, str, str],
) -> None:
    """Make absent ruleset activation a nonzero integrated workflow result, not a disconnected helper."""

    source, tooling, _publication = v21_repository
    root = tmp_path / "historical-v20"
    subprocess.run(["git", "clone", "--shared", "-q", str(source), str(root)], check=True)
    subprocess.run(
        ["git", "-C", str(root), "checkout", "-q", "--detach", publisher.APPROVED_V20_PUBLICATION],
        check=True,
    )
    result = run_workflow_bootstrap(root, tooling, rulesets_content=b"[]\n")
    assert result.returncode != 0
    assert '"effectiveHold": "ACTIVE"' in result.stdout
    assert publisher.CI_RULESET_BLOCKER in result.stdout


if __name__ == "__main__":
    raise SystemExit(pytest.main([__file__, "-q"]))
