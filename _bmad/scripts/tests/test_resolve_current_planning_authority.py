"""Raw-object, schema, workflow, and fault tests for the V22 resolver."""

from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
from typing import Callable

from jsonschema import Draft202012Validator
from jsonschema.exceptions import ValidationError
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/resolve_current_planning_authority.py"
SPEC = importlib.util.spec_from_file_location("resolve_current_planning_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
resolver = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(resolver)

PUBLISHER_MODULE_PATH = ROOT / resolver.V23_PUBLISHER_PATH
PUBLISHER_SPEC = importlib.util.spec_from_file_location("v23_publisher_for_resolver_tests", PUBLISHER_MODULE_PATH)
assert PUBLISHER_SPEC is not None and PUBLISHER_SPEC.loader is not None
publisher = importlib.util.module_from_spec(PUBLISHER_SPEC)
PUBLISHER_SPEC.loader.exec_module(publisher)


def git(root: Path, *arguments: str) -> str:
    """Run Git in a fixture repository and return stripped stdout."""

    return subprocess.check_output(
        ["git", "-C", str(root), *arguments],
        text=True,
    ).strip()


def commit(root: Path, message: str) -> str:
    """Commit the fixture index without signing."""

    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "commit", "-q", "-m", message],
        check=True,
    )
    return git(root, "rev-parse", "HEAD")


def copy_candidate_files(root: Path) -> None:
    """Copy immutable historical V22 transaction bytes into a detached parent fixture."""

    for relative_path in resolver.EXPECTED_PATHS:
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(
            subprocess.check_output(
                ["git", "-C", str(ROOT), "cat-file", "blob", f"{publisher.HISTORICAL_V22_CANDIDATE}:{relative_path}"]
            )
        )


Mutation = Callable[[Path], None]


def candidate_repository(
    tmp_path: Path,
    *,
    mutate: Mutation | None = None,
    extra_path: bool = False,
    executable_path: str | None = None,
) -> tuple[Path, str]:
    """Create one direct-child V22 candidate from the selected parent."""

    root = tmp_path / "candidate"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", resolver.EXPECTED_PARENT], check=True)
    git(root, "config", "user.name", "V22 fixture")
    git(root, "config", "user.email", "v22-fixture@example.invalid")
    copy_candidate_files(root)
    if mutate is not None:
        mutate(root)
    if extra_path:
        (root / "unexpected-v22-path.txt").write_text("unexpected\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", *resolver.EXPECTED_PATHS], check=True)
    if executable_path is not None:
        subprocess.run(["git", "-C", str(root), "update-index", "--chmod=+x", executable_path], check=True)
    if extra_path:
        subprocess.run(["git", "-C", str(root), "add", "unexpected-v22-path.txt"], check=True)
    return root, commit(root, "fix(planning): publish V22 current authority recovery")


def schema() -> dict[str, object]:
    """Load the trusted checked-in recovery/result schema."""

    return json.loads((ROOT / resolver.RECOVERY_SCHEMA_PATH).read_text(encoding="utf-8"))


def validate_result(document: dict[str, object]) -> None:
    """Validate one result against the closed result branch."""

    Draft202012Validator(schema()).validate(document)
    assert document["effectiveHold"] == "ACTIVE"
    assert document["implementationHold"] == "ACTIVE"
    assert document["assertionLedger"]
    assert document["ownerApprovalClaimed"] is False
    assert document["releaseAuthorized"] is False
    assert document["pushAuthorized"] is False
    assert document["executionAllowed"] is False


@pytest.fixture(scope="module")
def valid_candidate(tmp_path_factory: pytest.TempPathFactory) -> tuple[Path, str]:
    """Build one reusable valid V22 candidate."""

    return candidate_repository(tmp_path_factory.mktemp("v22-valid"))


def test_valid_candidate_and_committed_main_are_schema_valid_pass(
    valid_candidate: tuple[Path, str],
) -> None:
    """Cover the valid-candidate and post-fast-forward committed-main matrix rows."""

    root, candidate = valid_candidate
    direct = resolver.resolve_authority(root, candidate)
    validate_result(direct)
    assert direct["result"] == "PASS"
    assert direct["exitCode"] == 0
    assert {row["state"] for row in direct["assertionLedger"]} == {"PASS"}
    assert direct["blockers"] == []
    assert direct["observed"]["candidateCommit"] == candidate
    assert direct["observed"]["parentCommit"] == resolver.EXPECTED_PARENT
    assert [row["path"] for row in direct["observed"]["changedPaths"]] == list(resolver.EXPECTED_PATHS)
    assert direct["observed"]["parentGitlinks"] == direct["observed"]["candidateGitlinks"]

    subprocess.run(["git", "-C", str(root), "branch", "-f", "main", candidate], check=True)
    subprocess.run(["git", "-C", str(root), "switch", "-q", "main"], check=True)
    committed = resolver.resolve_authority(root, "main")
    validate_result(committed)
    assert committed["result"] == "PASS"
    assert committed["observed"]["candidateCommit"] == candidate


def test_cli_emits_the_pass_envelope_and_exit_zero(valid_candidate: tuple[Path, str]) -> None:
    """Exercise the exact operator-facing command shape."""

    root, candidate = valid_candidate
    completed = subprocess.run(
        [
            sys.executable,
            str(MODULE_PATH),
            "--repository",
            str(root),
            "--candidate",
            candidate,
            "--check",
        ],
        check=False,
        capture_output=True,
        text=True,
    )
    document = json.loads(completed.stdout)
    validate_result(document)
    assert completed.returncode == 0
    assert document["result"] == "PASS"


def mutate_marker(root: Path) -> None:
    """Drift one V22 marker binding."""

    path = root / resolver.ARCHITECTURE_PATH
    content = path.read_text(encoding="utf-8")
    path.write_text(content.replace("hold=ACTIVE -->", "hold=INACTIVE -->", 1), encoding="utf-8")


def mutate_route_digest(root: Path) -> None:
    """Change only the route artifact's raw digest while preserving its JSON value."""

    path = root / resolver.ROUTE_PATH
    path.write_bytes(path.read_bytes() + b" ")


def mutate_workflow_route(root: Path) -> None:
    """Retire the one active V22 workflow command."""

    path = root / resolver.WORKFLOW_PATH
    content = path.read_text(encoding="utf-8")
    path.write_text(content.replace("--candidate HEAD --check", "--candidate HEAD^ --check", 1), encoding="utf-8")


@pytest.mark.parametrize(
    ("mutation", "extra_path", "executable_path", "expected_code"),
    [
        (None, True, None, "TRANSACTION_PATH_DRIFT"),
        (None, False, resolver.WORKFLOW_PATH, "TRANSACTION_MODE_DRIFT"),
        (mutate_marker, False, None, "V22_MARKER_DRIFT"),
        (mutate_route_digest, False, None, "ROUTE_INVENTORY_DIGEST_DRIFT"),
        (mutate_workflow_route, False, None, "WORKFLOW_ROUTE_DRIFT"),
    ],
)
def test_semantic_drift_is_schema_valid_fail_with_a_stable_code(
    tmp_path: Path,
    mutation: Mutation | None,
    extra_path: bool,
    executable_path: str | None,
    expected_code: str,
) -> None:
    """Cover path, mode, marker, digest, and workflow-route semantic drift."""

    root, candidate = candidate_repository(
        tmp_path,
        mutate=mutation,
        extra_path=extra_path,
        executable_path=executable_path,
    )
    document = resolver.resolve_authority(root, candidate)
    validate_result(document)
    assert document["result"] == "FAIL"
    assert document["exitCode"] == 1
    assert document["blockers"][0]["code"] == expected_code
    assert any(row["state"] == "FAIL" for row in document["assertionLedger"])


def test_wrong_parent_is_schema_valid_fail(valid_candidate: tuple[Path, str]) -> None:
    """A descendant cannot substitute for the one selected direct-child candidate."""

    root, _candidate = valid_candidate
    path = root / "wrong-parent.txt"
    path.write_text("wrong parent\n", encoding="utf-8")
    subprocess.run(["git", "-C", str(root), "add", "wrong-parent.txt"], check=True)
    descendant = commit(root, "test: add wrong V22 parent")
    document = resolver.resolve_authority(root, descendant)
    validate_result(document)
    assert document["result"] == "FAIL"
    assert document["exitCode"] == 1
    assert document["blockers"][0]["code"] == "CANDIDATE_PARENT_DRIFT"


def test_gitlink_tuple_drift_is_schema_valid_fail(
    valid_candidate: tuple[Path, str],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Exercise the raw mode-160000 tuple guard independently of path drift."""

    root, candidate = valid_candidate
    calls = 0

    def drift_second_call(_repository: Path, _commit: str) -> tuple[tuple[str, str, str], ...]:
        nonlocal calls
        calls += 1
        rows = list(resolver.EXPECTED_GITLINKS)
        if calls == 2:
            path, mode, _object_id = rows[0]
            rows[0] = (path, mode, "0" * 40)
        return tuple(rows)

    monkeypatch.setattr(resolver, "gitlinks", drift_second_call)
    document = resolver.resolve_authority(root, candidate)
    validate_result(document)
    assert document["result"] == "FAIL"
    assert document["blockers"][0]["code"] == "ROOT_GITLINK_DRIFT"


def mutate_recovery_to_malformed_json(root: Path) -> None:
    """Make committed recovery evidence unavailable to the duplicate-safe loader."""

    (root / resolver.RECOVERY_PATH).write_text("{\n", encoding="utf-8")


def test_malformed_evidence_is_schema_valid_blocked(tmp_path: Path) -> None:
    """Cover malformed committed evidence without coercing it to FAIL or PASS."""

    root, candidate = candidate_repository(tmp_path, mutate=mutate_recovery_to_malformed_json)
    document = resolver.resolve_authority(root, candidate)
    validate_result(document)
    assert document["result"] == "BLOCKED"
    assert document["exitCode"] == 2
    assert document["blockers"][0]["code"] == "RECOVERY_POLICY_MALFORMED"
    assert any(row["state"] == "BLOCKED" for row in document["assertionLedger"])


def test_missing_candidate_is_schema_valid_blocked(valid_candidate: tuple[Path, str]) -> None:
    """Cover unavailable history/object evidence with a nonvacuous BLOCKED envelope."""

    root, _candidate = valid_candidate
    document = resolver.resolve_authority(root, "0" * 40)
    validate_result(document)
    assert document["result"] == "BLOCKED"
    assert document["exitCode"] == 2
    assert document["blockers"][0]["code"] == "GIT_OBJECT_UNAVAILABLE"


def test_policy_route_and_result_schemas_are_closed(
    valid_candidate: tuple[Path, str],
) -> None:
    """Reject candidate-defined extension fields on both artifacts and result evidence."""

    root, candidate = valid_candidate
    recovery_schema = schema()
    policy = json.loads((ROOT / resolver.RECOVERY_PATH).read_text(encoding="utf-8"))
    policy["unexpected"] = True
    with pytest.raises(ValidationError):
        Draft202012Validator(recovery_schema).validate(policy)

    route_schema = json.loads((ROOT / resolver.ROUTE_SCHEMA_PATH).read_text(encoding="utf-8"))
    route = json.loads((ROOT / resolver.ROUTE_PATH).read_text(encoding="utf-8"))
    route["unexpected"] = True
    with pytest.raises(ValidationError):
        Draft202012Validator(route_schema).validate(route)

    result = resolver.resolve_authority(root, candidate)
    result["unexpected"] = True
    with pytest.raises(ValidationError):
        Draft202012Validator(recovery_schema).validate(result)


def test_workflow_executes_authority_hosts_from_the_protected_event_base() -> None:
    """The real PR entry point materializes both trust hosts from protected base bytes."""

    workflow = (ROOT / resolver.WORKFLOW_PATH).read_text(encoding="utf-8")
    assert hashlib.sha256(workflow.encode("utf-8")).hexdigest() == resolver.V23_WORKFLOW_SHA256
    assert "\n  pull_request_target:\n" in workflow
    assert "\n  pull_request:\n" in workflow
    assert (
        "\n  protected-planning-authority:\n"
        "    name: protected-planning-authority-${{ github.event_name }}\n"
        "    if: github.event_name != 'pull_request'\n"
    ) in workflow
    assert "uses: actions/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065" in workflow
    assert 'python-version: "3.11.15"' in workflow
    assert "update-environment: false" in workflow
    assert "UV_PYTHON_DOWNLOADS=never" in workflow
    assert "allow-unsafe-pr-checkout: true" in workflow
    assert "$RUNNER_TEMP/planning-authority-protected-host/.venv/bin/python" in workflow
    assert "- name: Verify synchronized protected Python identity" in workflow
    assert "import jsonschema" in workflow
    assert "sys._base_executable" in workflow
    assert "raise SystemExit(observed_exit)" in workflow
    assert "            pyproject.toml \\" in workflow
    assert "            uv.lock" in workflow
    assert "uses: astral-sh/setup-uv@c771a70e6277c0a99b617c7a806ffedaca235ff9" in workflow
    assert 'checksum: "745765a3b6e360ad76743599ae5c42e9278c7edf8bbff9fc76d05bf2623a04dd"' in workflow
    assert '"$PROTECTED_UV" sync' in workflow
    assert '"$protected_python" -I -P' in workflow
    assert '--candidate "${{ steps.range.outputs.candidate }}"' in workflow
    assert 'trusted_git show "$TRUSTED_HOST_COMMIT:$path"' in workflow
    assert "env -i PATH=/usr/bin:/bin" in workflow
    assert '/usr/bin/git -C "$GITHUB_WORKSPACE"' in workflow
    assert "-c core.hooksPath=/dev/null -c protocol.file.allow=never" in workflow
    assert 'working-directory: ${{ runner.temp }}' in workflow
    assert 'PYTHONSAFEPATH: "1"' in workflow
    assert "/usr/bin/python3" not in workflow[: workflow.index("\n  candidate-validation:\n")]
    assert "TRUSTED_EVENT_BASE: ${{ github.event.pull_request.base.sha || github.event.before }}" in workflow
    assert 'trusted_host_commit="$baseline"' in workflow
    assert 'test "$trusted_host_commit" = "$baseline"' in workflow
    assert "$RUNNER_TEMP/planning-authority-protected-host/resolve_current_planning_authority.py" in workflow
    assert "$RUNNER_TEMP/planning-authority-protected-host/verify_evidence_boundary.py" in workflow
    assert "- name: Verify checked-out candidate identity" in workflow
    assert 'test "$observed" = "$TRUSTED_EVENT_HEAD"' in workflow
    assert "    name: candidate-validation-${{ github.event_name }}\n" in workflow
    assert "python3 _bmad/scripts/resolve_current_planning_authority.py" not in workflow
    assert "python3 _bmad/scripts/verify_evidence_boundary.py" not in workflow
    protected_sync = workflow.index("- name: Synchronize protected planning verifier environment")
    resolver_step = workflow.index("- name: Resolve current planning authority through the protected host")
    evidence_step = workflow.index("- name: Verify lifecycle evidence and candidate-bound publication scope")
    candidate_job = workflow.index("\n  candidate-validation:\n")
    candidate_sync = workflow.index("- name: Synchronize frozen candidate environment")
    assert protected_sync < resolver_step < candidate_job
    assert protected_sync < evidence_step < candidate_job < candidate_sync
    assert "python3 -m pip" not in workflow[:candidate_job]
    assert "    needs: protected-planning-authority\n" in workflow
    assert "      always() &&\n" in workflow
    assert "      (github.event_name == 'pull_request' ||\n" in workflow
    assert "      needs.protected-planning-authority.result == 'success'))\n" in workflow
    for candidate_only in (
        "submodules: true",
        "global-json-file: global.json",
        "node-version-file: package.json",
        "npm ci --ignore-scripts",
        "_bmad/scripts/check_lifecycle_gate_preflight.py",
    ):
        assert workflow.index(candidate_only) > candidate_job
    assert resolver.EXPECTED_V21_TOOLING in workflow
    assert resolver.V21_TEST_PATH in workflow
    for forbidden in (
        "RULESET_TOKEN",
        "/rulesets?",
        " ci-trust ",
        f"{resolver.V21_PUBLISHER_PATH} --repository",
        f'{resolver.V21_PUBLISHER_PATH}" --repository',
    ):
        assert forbidden not in workflow


def test_historical_v21_sources_are_unchanged_and_faults_restore_source_bytes() -> None:
    """Pin historical V21 bytes and prove fixture faults never alter the source worktree."""

    for relative_path in (
        resolver.V21_RECORD_PATH,
        resolver.V21_SCHEMA_PATH,
        resolver.V21_PUBLISHER_PATH,
        resolver.V21_TEST_PATH,
    ):
        worktree = (ROOT / relative_path).read_bytes()
        parent = subprocess.check_output(
            ["git", "-C", str(ROOT), "cat-file", "blob", f"{resolver.EXPECTED_PARENT}:{relative_path}"]
        )
        assert worktree == parent
    assert hashlib.sha256((ROOT / resolver.V21_RECORD_PATH).read_bytes()).hexdigest() == resolver.EXPECTED_V21_SHA256


def test_caller_synthesized_blocked_result_is_nonvacuous_and_closed() -> None:
    """A caller that receives no/malformed output can fail closed without an empty ledger."""

    document = resolver.blocked_result("OUTPUT_UNAVAILABLE", "resolver emitted no parseable output")
    validate_result(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"] == [
        {"code": "OUTPUT_UNAVAILABLE", "detail": "resolver emitted no parseable output"}
    ]


def v23_request_repository(tmp_path: Path) -> tuple[Path, str]:
    """Create the exact V23 request transaction for host-boundary tests."""

    root = tmp_path / "v23"
    subprocess.run(["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)], check=True)
    subprocess.run(
        [
            "git",
            "-C",
            str(root),
            "sparse-checkout",
            "set",
            "--no-cone",
            *resolver.V23_TOOLING_PATHS,
            resolver.ARCHITECTURE_PATH,
        ],
        check=True,
    )
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", resolver.V23_TOOLING_BASELINE], check=True)
    git(root, "config", "user.name", "V23 fixture")
    git(root, "config", "user.email", "v23-fixture@example.invalid")
    for relative_path in resolver.V23_TOOLING_PATHS:
        if relative_path == resolver.V23_REQUEST_PATH:
            continue
        target = root / relative_path
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative_path, target)
    request = publisher.render_request(root)
    (root / resolver.V23_REQUEST_PATH).parent.mkdir(parents=True, exist_ok=True)
    (root / resolver.V23_REQUEST_PATH).write_bytes(publisher.json_bytes(request))
    subprocess.run(
        ["git", "-C", str(root), "add", "--sparse", "--", *resolver.V23_TOOLING_PATHS],
        check=True,
    )
    return root, commit(root, "fix(planning): add V23 request fixture")


def test_complete_v23_marker_reaches_the_authenticated_production_loader(tmp_path: Path) -> None:
    """A real committed complete marker selects V23 only after publisher authentication."""

    root, request_commit = v23_request_repository(tmp_path)
    architecture = root / resolver.ARCHITECTURE_PATH
    architecture.write_bytes(
        publisher.candidate_blob(root, request_commit, resolver.ARCHITECTURE_PATH)
        + b"\n<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n"
        + b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END fixture -->\n"
    )
    subprocess.run(["git", "-C", str(root), "add", "--sparse", "--", resolver.ARCHITECTURE_PATH], check=True)
    candidate = commit(root, "test: add complete V23 marker")

    assert resolver.v23_marker_selected(root, candidate) is True
    module, publication = resolver.load_v23_publisher(root, candidate)
    assert publication == request_commit
    assert callable(module.resolve_published_authority)
    result = resolver.resolve_authority(root, candidate)
    assert result["result"] in ("FAIL", "BLOCKED")
    assert result["executionAllowed"] is False
    assert result["assertionLedger"]


def test_protected_host_rejects_relaxed_candidate_schema_before_publisher_load(tmp_path: Path) -> None:
    """Candidate schema semantics cannot redefine the resolver's external trust root."""

    root, _candidate = v23_request_repository(tmp_path)
    (root / resolver.V23_SCHEMA_PATH).write_text(
        '{"$schema":"https://json-schema.org/draft/2020-12/schema"}\n',
        encoding="utf-8",
    )
    request_path = root / resolver.V23_REQUEST_PATH
    request = json.loads(request_path.read_bytes())
    request["executionAllowed"] = True
    request["releaseAuthorized"] = True
    request_path.write_bytes(publisher.json_bytes(request))
    subprocess.run(
        [
            "git",
            "-C",
            str(root),
            "add",
            "--sparse",
            "--",
            resolver.V23_SCHEMA_PATH,
            resolver.V23_REQUEST_PATH,
        ],
        check=True,
    )
    subprocess.run(
        ["git", "-C", str(root), "commit", "-q", "--amend", "--no-edit"],
        check=True,
    )
    relaxed_candidate = git(root, "rev-parse", "HEAD")

    with pytest.raises(resolver.ResolutionError) as error:
        resolver.load_v23_publisher(root, relaxed_candidate)

    assert error.value.code == "V23_SCHEMA_IDENTITY_MISMATCH"


def test_signed_v23_authority_passes_through_production_resolver_host(
    tmp_path: Path,
) -> None:
    """A real signed authority traverses authenticated loading, dispatch, and host result closure."""

    fixture_path = ROOT / "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py"
    fixture_spec = importlib.util.spec_from_file_location("v23_resolver_authority_fixtures", fixture_path)
    assert fixture_spec is not None and fixture_spec.loader is not None
    fixtures = importlib.util.module_from_spec(fixture_spec)
    fixture_spec.loader.exec_module(fixtures)
    private_key = tmp_path / "resolver_authority_key"
    subprocess.run(
        [publisher.SSH_KEYGEN_EXECUTABLE, "-q", "-t", "ed25519", "-N", "", "-f", str(private_key)],
        check=True,
    )
    public_key = private_key.with_suffix(".pub").read_text(encoding="utf-8").strip()
    fingerprint = subprocess.check_output(
        [publisher.SSH_KEYGEN_EXECUTABLE, "-lf", str(private_key.with_suffix(".pub")), "-E", "sha256"],
        text=True,
    ).split()[1]
    root, _request, publication, _authority = fixtures.authority_repository(
        tmp_path / "authority",
        signing_key=private_key,
    )
    authenticated_module, request_publication = resolver.load_v23_publisher(root, publication)
    assert request_publication != publication

    def verify(repo: Path, commit: str, owner: str) -> dict[str, str]:
        facts = authenticated_module.verify_publication_signature(
            repo,
            commit,
            owner,
            trusted_public_key=public_key,
            trusted_fingerprint=fingerprint,
            ssh_keygen_path=publisher.SSH_KEYGEN_EXECUTABLE,
        )
        assert facts["status"] == "G"
        return {
            "status": "G",
            "principal": resolver.V23_TRUSTED_SSH_PRINCIPAL,
            "fingerprint": resolver.V23_TRUSTED_SSH_FINGERPRINT,
            "authorIdentity": resolver.V23_TRUSTED_OWNER_IDENTITY,
        }

    result = resolver.resolve_authority(root, publication, signature_verifier=verify)

    assert result["result"] == "PASS"
    assert result["executionAllowed"] is True
    assert result["storyExecution"] == {"7.1": True, "7.2": False, "7.3": False, "7.4": False}
    assert result["assertionLedger"]


def test_protected_host_rejects_coordinated_candidate_host_and_publisher(tmp_path: Path) -> None:
    """A hostile candidate cannot redefine both the host digest and the publisher verdict."""

    root, request_commit = v23_request_repository(tmp_path)
    architecture = root / resolver.ARCHITECTURE_PATH
    architecture.write_bytes(
        publisher.candidate_blob(root, request_commit, resolver.ARCHITECTURE_PATH)
        + b"\n<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN hostile -->\n"
        + b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END hostile -->\n"
    )
    (root / resolver.V23_PUBLISHER_PATH).write_text(
        "def resolve_published_authority(*args, **kwargs):\n"
        "    return {'result': 'PASS', 'exitCode': 0, 'executionAllowed': True}\n",
        encoding="utf-8",
    )
    (root / "_bmad/scripts/resolve_current_planning_authority.py").write_text(
        "print('candidate-controlled PASS')\n",
        encoding="utf-8",
    )
    subprocess.run(
        [
            "git",
            "-C",
            str(root),
            "add",
            "--sparse",
            "--",
            resolver.ARCHITECTURE_PATH,
            resolver.V23_PUBLISHER_PATH,
            "_bmad/scripts/resolve_current_planning_authority.py",
        ],
        check=True,
    )
    candidate = commit(root, "test: inject coordinated hostile hosts")

    result = resolver.resolve_authority(root, candidate)

    assert result["result"] == "BLOCKED"
    assert result["blockers"][0]["code"] == "V23_PUBLISHER_DESCENDANT_DRIFT"
    assert result["executionAllowed"] is False


@pytest.mark.parametrize(
    "mutation",
    (
        lambda document: document.update({"schemaVersion": "candidate.schema"}),
        lambda document: document.update({"effectiveHold": "ACTIVE"}),
        lambda document: document.update({"ownerApprovalClaimed": False}),
        lambda document: document.update({"releaseAuthorized": True}),
        lambda document: document.update({"storyExecution": {"7.1": True, "7.2": True, "7.3": False, "7.4": False}}),
        lambda document: document["assertionLedger"][0].update({"state": "BLOCKED"}),
        lambda document: document["assertionLedger"][0].update({"detail": ""}),
        lambda document: document["assertionLedger"].append(dict(document["assertionLedger"][0])),
        pytest.param(lambda document: document.update({"observed": {}}), id="empty-observed"),
        pytest.param(lambda document: document["observed"].update({"unexpected": "hostile"}), id="unknown-observed"),
        pytest.param(
            lambda document: document["observed"].update({"authorityPublication": "d" * 40}),
            id="contradictory-observed",
        ),
        pytest.param(
            lambda document: document["observed"]["ownerSignature"].update({"fingerprint": "SHA256:hostile"}),
            id="wrong-owner-fingerprint",
        ),
    ),
)
def test_protected_host_rejects_contradictory_publisher_results(
    mutation: Callable[[dict[str, object]], None],
) -> None:
    """No contradictory hold, authorization, story, schema, or ledger claim crosses the host."""

    document: dict[str, object] = {
        "schemaVersion": resolver.V23_RESULT_SCHEMA_VERSION,
        "result": "PASS",
        "exitCode": 0,
        "effectiveHold": "EXECUTION_ALLOWED",
        "implementationHold": "EXECUTION_ALLOWED",
        "observed": {
            "candidateCommit": "a" * 40,
            "candidateTree": "b" * 40,
            "authorityPublication": "a" * 40,
            "sourceCommit": "c" * 40,
            "changedPaths": [resolver.ARCHITECTURE_PATH, resolver.V23_AUTHORITY_PATH],
            "ownerSignature": {
                "status": "G",
                "principal": resolver.V23_TRUSTED_SSH_PRINCIPAL,
                "fingerprint": resolver.V23_TRUSTED_SSH_FINGERPRINT,
                "authorIdentity": resolver.V23_TRUSTED_OWNER_IDENTITY,
            },
        },
        "assertionLedger": [{"id": "V23.TEST", "subject": "fixture", "state": "PASS", "detail": "fixture passed"}],
        "blockers": [],
        "ownerApprovalClaimed": True,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": True,
        "storyExecution": {"7.1": True, "7.2": False, "7.3": False, "7.4": False},
    }
    mutation(document)

    with pytest.raises(resolver.ResolutionError) as error:
        resolver.validate_v23_result(document)

    assert error.value.code == "V23_PUBLISHER_RESULT_INVALID"


def test_protected_host_preserves_empty_observed_for_structured_failure() -> None:
    """An early stable blocker may have no observations while retaining its closed result."""

    document = {
        "schemaVersion": resolver.V23_RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": "V23_HISTORY_UNAVAILABLE",
                "subject": "v23-entry-authority",
                "state": "BLOCKED",
                "detail": "history unavailable",
            }
        ],
        "blockers": [{"code": "V23_HISTORY_UNAVAILABLE", "detail": "history unavailable", "assertionIndex": 0}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    assert resolver.validate_v23_result(document) == document


def test_protected_host_accepts_closed_v23_fail_document() -> None:
    """The protected host positively exercises the valid FAIL result alternative."""

    document = {
        "schemaVersion": resolver.V23_RESULT_SCHEMA_VERSION,
        "result": "FAIL",
        "exitCode": 1,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": "V23_GATE_DISPOSITION_DRIFT",
                "subject": "v23-entry-authority",
                "state": "FAIL",
                "detail": "gate disposition drift",
            }
        ],
        "blockers": [
            {"code": "V23_GATE_DISPOSITION_DRIFT", "detail": "gate disposition drift", "assertionIndex": 0}
        ],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    assert resolver.validate_v23_result(document) == document


def test_protected_host_rejects_blocker_without_matching_ledger_row() -> None:
    """A blocker cannot be detached from the non-PASS assertion that explains it."""

    document = {
        "schemaVersion": resolver.V23_RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": {},
        "assertionLedger": [
            {
                "id": "V23_HISTORY_UNAVAILABLE",
                "subject": "v23-entry-authority",
                "state": "BLOCKED",
                "detail": "history unavailable",
            }
        ],
        "blockers": [{"code": "V23_HISTORY_UNAVAILABLE", "detail": "different detail", "assertionIndex": 0}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
        "storyExecution": {"7.1": False, "7.2": False, "7.3": False, "7.4": False},
    }

    with pytest.raises(resolver.ResolutionError):
        resolver.validate_v23_result(document)


def test_trusted_git_environment_ignores_hostile_redirects(monkeypatch: pytest.MonkeyPatch) -> None:
    """Ambient Git and executable redirects never enter the protected host process."""

    monkeypatch.setenv("PATH", "/tmp/hostile")
    monkeypatch.setenv("GIT_DIR", "/tmp/hostile.git")
    monkeypatch.setenv("GIT_OBJECT_DIRECTORY", "/tmp/hostile-objects")
    environment = resolver.trusted_environment()

    assert environment["PATH"] == resolver.TRUSTED_EXECUTABLE_PATH
    assert "GIT_DIR" not in environment
    assert "GIT_OBJECT_DIRECTORY" not in environment
    assert environment["GIT_CONFIG_GLOBAL"] == "/dev/null"


@pytest.mark.parametrize(
    "architecture",
    (
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:END fixture -->\n"
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n",
        b"<!-- ARCHITECTURE-EXECUTION-OVERLAY-V23:BEGIN fixture -->\n",
    ),
)
def test_malformed_v23_marker_is_structured_blocked(
    monkeypatch: pytest.MonkeyPatch,
    architecture: bytes,
) -> None:
    """Reversed and incomplete markers never escape the closed resolver model."""

    monkeypatch.setattr(resolver, "resolve_commit", lambda _root, _revision: "0" * 40)
    monkeypatch.setattr(resolver, "candidate_blob", lambda _root, _commit, _path: architecture)

    result = resolver.resolve_authority(ROOT, "HEAD")

    assert result["result"] == "BLOCKED"
    assert result["exitCode"] == 2
    assert result["executionAllowed"] is False
    assert result["blockers"][0]["code"] == "V23_MARKER_INCOMPLETE"
    assert result["assertionLedger"]


def test_candidate_module_system_exit_is_caught_by_host_boundary(monkeypatch: pytest.MonkeyPatch) -> None:
    """An authenticated module interface failure cannot terminate the resolver process."""

    class HostileModule:
        @staticmethod
        def resolve_published_authority(*_args: object, **_kwargs: object) -> dict[str, object]:
            raise SystemExit(7)

    monkeypatch.setattr(resolver, "resolve_commit", lambda _root, _revision: "0" * 40)
    monkeypatch.setattr(resolver, "v23_marker_selected", lambda _root, _candidate: True)
    monkeypatch.setattr(resolver, "load_v23_publisher", lambda _root, _candidate: (HostileModule(), "1" * 40))

    result = resolver.resolve_authority(ROOT, "HEAD")

    assert result["result"] == "BLOCKED"
    assert result["blockers"][0]["code"] == "V23_PUBLISHER_EXECUTION_FAILED"
    assert result["executionAllowed"] is False
