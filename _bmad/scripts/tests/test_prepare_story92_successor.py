"""Hermetic successor-preparation guards; no .NET or initialized submodules.

Git scope, byte, authorization, receipt, rendering, and schema checks are real.
Only build execution and MSBuild/source derivation use explicit private adapters;
their fixture observations never supply product execution acceptance.
"""

from contextlib import contextmanager
from copy import deepcopy
import importlib.util
import json
import os
from pathlib import Path
import subprocess

import pytest


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "_bmad/scripts/prepare_story92_successor.py"
DIRECTORY = "artifacts/v9/9.2/fixture-successor-v1"


def load_tool():
    specification = importlib.util.spec_from_file_location("prepare_story92_test", SCRIPT)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


def fixture_git(root, *arguments):
    environment = dict(os.environ)
    for key in list(environment):
        if key.startswith("GIT_"):
            environment.pop(key)
    environment.update(GIT_AUTHOR_NAME="Fixture", GIT_AUTHOR_EMAIL="fixture@example.invalid",
                       GIT_COMMITTER_NAME="Fixture", GIT_COMMITTER_EMAIL="fixture@example.invalid",
                       GIT_CONFIG_GLOBAL=os.devnull, GIT_CONFIG_NOSYSTEM="1")
    return subprocess.run(["git", "-c", "commit.gpgsign=false", "-C", str(root), *arguments],
                          env=environment, check=True, capture_output=True, text=True).stdout.strip()


def write(root, path, content):
    target = root / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(content)


def commit(root):
    fixture_git(root, "add", ".")
    fixture_git(root, "commit", "-m", "test: record disposable fixture")
    return fixture_git(root, "rev-parse", "HEAD")


@pytest.fixture
def context(tmp_path, monkeypatch):
    module = load_tool()
    source, authority = tmp_path / "source", tmp_path / "authority"
    source.mkdir(); authority.mkdir()
    tooling = tmp_path / "tooling"
    write(tooling, module.SCHEMA_PATH, (ROOT / module.SCHEMA_PATH).read_bytes())
    write(tooling, "_bmad/scripts/prepare_story92_successor.py", SCRIPT.read_bytes())
    monkeypatch.setattr(module, "SCRIPT_ROOT", tooling)
    fixture_git(source, "init", "-q")
    migration_bytes = (ROOT / module.verifier().MIGRATION).read_bytes()
    approval_bytes = (ROOT / module.verifier().APPROVAL).read_bytes()
    retained = module.parse(migration_bytes)
    write(source, module.verifier().MIGRATION, migration_bytes)
    write(source, module.verifier().APPROVAL, approval_bytes)
    write(source, ".gitmodules", b'[submodule "references/Hexalith.Builds"]\n path = references/Hexalith.Builds\n url = fixture.invalid\n')
    write(source, "src/Hexalith.Conversations/Example.cs", b"class Example {}\n")
    before = commit(source)
    (source / "references/Hexalith.Builds").mkdir(parents=True)
    fixture_git(source, "update-index", "--add", "--cacheinfo", f"160000,{before},references/Hexalith.Builds")
    fixture_git(source, "commit", "-m", "test: record original gitlink")
    original = fixture_git(source, "rev-parse", "HEAD")
    write(source, "docs/release-evidence/retained.json", b'{"retained":true}\n')
    publication = commit(source)
    write(source, "src/Hexalith.Conversations/Example.cs", b"class Example { int Value; }\n")
    fixture_git(source, "update-index", "--cacheinfo", f"160000,{publication},references/Hexalith.Builds")
    candidate = commit(source)
    measured_changes = module.changes(source, original, candidate)
    measured_promotions = module.promotions(source, original, candidate)
    counts = {"committedChangedPaths": len(measured_changes), "productionPaths": 1,
              "changedRootGitlinks": 1, "owningPromotionCommits": len(measured_promotions)}
    protected = []
    for path in (module.verifier().MIGRATION, module.verifier().APPROVAL, "docs/release-evidence/retained.json"):
        content = (source / path).read_bytes()
        protected.append({"path": path, "sha256": module.sha(content), "sizeBytes": len(content),
                          "unchangedFromOriginalPublication": True})
    completed = {"candidate": original, "publication": publication, "lifecycle": candidate}
    scope = {"schemaVersion": "hexalith.conversations.story-9.2-current-main-scope-proposal.v1",
             "status": "prepared-unapproved", "authorizationClaimed": False, "approvalClaimed": False,
             "originalBaseline": before, "originalAcceptedSourceCandidate": original,
             "originalPublication": publication, "measuredCommittedCandidate": candidate,
             "completedOriginalScopeSuccessor": completed, "counts": counts,
             "committedChangesSinceAcceptedCandidate": measured_changes,
             "rootGitlinkPromotionHistorySinceAcceptedCandidate": measured_promotions,
             "preservedProtectedEvidence": protected, "workingTreeSnapshot": [],
             "historicalCandidateEnvironmentVerification": {"result": "FAIL", "blockers": []},
             "requestedScopeDecision": {"status": "unapproved"}}
    scope["proposalSha256"] = module.sha(module.canonical(scope))
    scope_bytes = module.json_bytes(scope)
    authorization = {"schemaVersion": "hexalith.conversations.story-9.2-current-main-scope-authorization.v1",
                     "storyId": "9.2", "status": "authorized", "authorization": {
                         "actor": "user", "authorizedOn": "2026-10-08", "statement": "yes",
                         "evidence": "Disposable fixture's explicit private scope adapter"},
                     "proposal": {"path": module.SCOPE_PATH, "sha256": module.sha(scope_bytes),
                                  "proposalSha256": scope["proposalSha256"]},
                     "measuredCommittedCandidate": candidate, "originalBaseline": before,
                     "workingTreeChangesIncluded": False, "qualityDecisionIncluded": False,
                     "preserveOriginalBaselineFrozenContractsApprovalsAndAcceptedRecords": True}
    authorization_bytes = module.json_bytes(authorization)
    monkeypatch.setattr(module, "PINS", {"authorizationSha256": module.sha(authorization_bytes),
        "scopeSha256": module.sha(scope_bytes), "scopeMaterialSha256": scope["proposalSha256"],
        "candidate": candidate, "baseline": before, "originalCandidate": original,
        "originalPublication": publication, "completedOriginalScopeSuccessor": completed, "counts": counts})
    write(authority, module.AUTHORIZATION_PATH, authorization_bytes)
    write(authority, module.SCOPE_PATH, scope_bytes)
    adapter = module.verifier()
    monkeypatch.setattr(adapter, "derive_migration", lambda root, **kwargs: deepcopy(retained))
    monkeypatch.setattr(module, "run_build", lambda root, command:
        (1, b"error CS0246: unavailable fixture type\n", b"") if "build" in command and "Conformance.Tests.csproj" in command[2]
        else (0, b"Fixture build/restore available\n", b""))
    return module, source, authority, scope


@contextmanager
def mutated(path, content):
    original = path.read_bytes() if path.exists() else None
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)
        yield
    finally:
        if original is None:
            path.unlink()
        else:
            path.write_bytes(original)
        assert (path.read_bytes() if path.exists() else None) == original


def assert_rejected(context, code, operation=None):
    module, source, authority, _ = context
    before = {path: path.read_bytes() for root in (source, authority)
              for path in root.rglob("*") if path.is_file() and ".git" not in path.parts}
    with pytest.raises(module.PreparationError) as error:
        (operation or (lambda: module.prepare(source, authority, DIRECTORY)))()
    assert error.value.code == code
    assert all(path.read_bytes() == content for path, content in before.items())
    assert not (authority / DIRECTORY / module.JSON_NAME).exists()


def measured(context):
    module, source, authority, _ = context
    module.measure_builds(source, authority, DIRECTORY)
    return module.prepare(source, authority, DIRECTORY)


def test_valid_authorization_prepares_identical_pending_outputs_with_failed_builds(context):
    module, source, authority, _ = context
    document = measured(context)
    outputs = [(authority / DIRECTORY / name).read_bytes() for name in (module.JSON_NAME, module.MARKDOWN_NAME)]
    assert module.prepare(source, authority, DIRECTORY) == document
    assert outputs == [(authority / DIRECTORY / name).read_bytes() for name in (module.JSON_NAME, module.MARKDOWN_NAME)]
    assert document["acceptanceClaimed"] is False
    assert document["qualityApprovalClaimed"] is False
    assert document["acceptance"]["state"] == "blocked"
    assert "INTERNAL_TIER_BUILD_FAILED" in document["acceptance"]["blockers"]
    assert [row["exitCode"] for row in document["buildEvidence"]["observations"]] == [0, 0, 0, 1, 1]
    assert document["successorMaterial"]["proposedMigration"]["assertions"] == module.verifier().document(source, module.verifier().MIGRATION)["proposal"]["assertions"]


@pytest.mark.parametrize("target,mutation,code", [
    ("authorization", "missing", "SUCCESSOR_INPUT_MISSING"),
    ("authorization", "altered", "SUCCESSOR_AUTHORIZATION_INVALID"),
    ("authorization", "unapproved", "SUCCESSOR_AUTHORIZATION_INVALID"),
    ("authorization", "synthetic", "SUCCESSOR_AUTHORIZATION_INVALID"),
    ("authorization", "extra", "SUCCESSOR_AUTHORIZATION_INVALID"),
    ("scope", "missing", "SUCCESSOR_INPUT_MISSING"),
    ("scope", "altered", "SUCCESSOR_SCOPE_INVALID"),
])
def test_scope_mutations_reject_and_restore(context, target, mutation, code):
    module, _, authority, _ = context
    path = authority / (module.AUTHORIZATION_PATH if target == "authorization" else module.SCOPE_PATH)
    original = path.read_bytes()
    if mutation == "missing":
        path.unlink()
        try: assert_rejected(context, code)
        finally: path.write_bytes(original)
    else:
        document = module.parse(original)
        if mutation == "unapproved":document["status"] = "unapproved"
        elif mutation == "synthetic":document["authorization"]["actor"] = "SYNTHETIC-FIXTURE"
        elif mutation == "extra":document["qualityApprovalClaimed"] = True
        else:document["originalBaseline"] = "0" * 40
        with mutated(path, module.json_bytes(document)):assert_rejected(context, code)
    module.authority(authority)
    assert path.read_bytes() == original


@pytest.mark.parametrize("case", ["extra-source", "reverted-source", "extra-gitlink", "reverted-gitlink", "empty-commit"])
def test_wrong_candidates_including_reverted_changes_are_rejected(context, case):
    module, source, authority, _ = context
    if "source" in case:
        path = source / "src/Hexalith.Conversations/Example.cs"
        path.write_bytes(path.read_bytes() + b"// extra committed source\n")
        commit(source)
        if case.startswith("reverted"):
            path.write_bytes(b"class Example { int Value; }\n")
            commit(source)
    elif "gitlink" in case:
        fixture_git(source, "update-index", "--cacheinfo", f"160000,{module.PINS['originalCandidate']},references/Hexalith.Builds")
        fixture_git(source, "commit", "-m", "test: mutate disposable gitlink")
        if case.startswith("reverted"):
            fixture_git(source, "update-index", "--cacheinfo", f"160000,{module.PINS['originalPublication']},references/Hexalith.Builds")
            fixture_git(source, "commit", "-m", "test: restore disposable gitlink")
    else: fixture_git(source, "commit", "--allow-empty", "-m", "test: add unauthorized fixture commit")
    assert_rejected(context, "SUCCESSOR_CANDIDATE_INVALID")
    fixture_git(source, "checkout", "--detach", module.PINS["candidate"])
    module.validate_source(source, context[3])


@pytest.mark.parametrize("target", ["source", "protected", "test", "configuration"])
def test_source_input_dirt_is_rejected_and_restored(context, target):
    module, source, _, _ = context
    paths = {"source": "src/Hexalith.Conversations/Example.cs", "protected": "docs/release-evidence/retained.json",
             "test": "tests/Extra.cs", "configuration": "Directory.Build.props"}
    with mutated(source / paths[target], b"unauthorized bytes\n"):
        assert_rejected(context, "SUCCESSOR_SOURCE_DIRTY")
    module.validate_source(source, context[3])


@pytest.mark.parametrize("flag", ["--assume-unchanged", "--skip-worktree"])
def test_hidden_index_dirt_cannot_supply_current_source(context, flag):
    module, source, _, _ = context
    relative = "src/Hexalith.Conversations/Example.cs"
    fixture_git(source, "update-index", flag, relative)
    try:
        with mutated(source / relative, b"class HiddenDirt {}\n"):
            assert not fixture_git(source, "status", "--porcelain", "--ignore-submodules=all")
            assert_rejected(context, "SUCCESSOR_SOURCE_DIRTY")
    finally: fixture_git(source, "update-index", "--no-" + flag[2:], relative)
    module.validate_source(source, context[3])


@pytest.mark.parametrize("relative", ["src/Hexalith.Conversations/Ignored.cs", "tests/Ignored.cs", "NuGet.Config",
                                      "src/Hexalith.Conversations/Ignored.resx", "build.rsp",
                                      "src/Hexalith.Conversations/__pycache__/Ignored.cs"])
def test_ignored_source_and_configuration_inputs_are_rejected(context, relative):
    module, source, _, _ = context
    exclude = source / ".git/info/exclude"
    before = exclude.read_bytes()
    exclude.write_bytes(before + relative.encode() + b"\n")
    try:
        with mutated(source / relative, b"ignored outside approved tree\n"):
            assert not fixture_git(source, "status", "--porcelain", "--ignore-submodules=all")
            assert_rejected(context, "SUCCESSOR_SOURCE_DIRTY")
    finally: exclude.write_bytes(before)
    module.validate_source(source, context[3])


@pytest.mark.parametrize("field", ["counts", "changes", "promotions", "protected"])
def test_git_remeasurement_rejects_scope_claims(context, field):
    module, source, _, scope = context
    altered = deepcopy(scope)
    if field == "counts":altered["counts"]["productionPaths"] += 1
    elif field == "changes":altered["committedChangesSinceAcceptedCandidate"][0]["status"] = "D"
    elif field == "promotions":altered["rootGitlinkPromotionHistorySinceAcceptedCandidate"][0]["parent"] = "0" * 40
    else:altered["preservedProtectedEvidence"][0]["sha256"] = "0" * 64
    assert_rejected(context, "SUCCESSOR_PROTECTED_DRIFT" if field == "protected" else "SUCCESSOR_SCOPE_DRIFT",
                    lambda: module.validate_source(source, altered))
    module.validate_source(source, scope)


@pytest.mark.parametrize("case", ["log", "source", "exit", "acceptance", "command"])
def test_build_receipt_mutations_do_not_become_acceptance(context, case):
    module, source, authority, _ = context
    module.measure_builds(source, authority, DIRECTORY)
    path = authority / DIRECTORY / module.BUILD_NAME
    receipt = module.parse(path.read_bytes())
    if case == "log":
        with mutated(authority / receipt["observations"][0]["stdout"]["path"], b"edited log\n"):
            assert_rejected(context, "SUCCESSOR_BUILD_EVIDENCE_INVALID")
    else:
        if case == "source":receipt["sourceSnapshotSha256"] = "0" * 64
        elif case == "exit":receipt["observations"][-1]["exitCode"] = 0
        elif case == "command":receipt["observations"][0]["command"] = ["echo", "PASS"]
        else:receipt["acceptanceClaimed"] = True
        with mutated(path, module.json_bytes(receipt)):assert_rejected(context, "SUCCESSOR_BUILD_EVIDENCE_INVALID")
    module.prepare(source, authority, DIRECTORY)


@pytest.mark.parametrize("relative", ["../outside-v1", "docs/release-evidence", "artifacts/v9/9.2/retained", "artifacts/v9/9.2/../escape-v1"])
def test_output_directory_must_be_contained_and_versioned(context, relative):
    module, source, authority, _ = context
    assert_rejected(context, "SUCCESSOR_OUTPUT_INVALID", lambda: module.prepare(source, authority, relative))


@pytest.mark.parametrize("alias", ["symlink", "hardlink", "directory-symlink", "nonfile"])
def test_output_aliases_and_collisions_preserve_all_inputs(context, alias):
    module, source, authority, _ = context
    target = authority / DIRECTORY / module.JSON_NAME
    target.parent.mkdir(parents=True)
    protected = source / "docs/release-evidence/retained.json"
    original = protected.read_bytes()
    if alias == "symlink":target.symlink_to(protected)
    elif alias == "hardlink":os.link(protected, target)
    elif alias == "directory-symlink":
        target.parent.rmdir();target.parent.symlink_to(protected.parent, target_is_directory=True)
    else:target.mkdir()
    with pytest.raises(module.PreparationError) as error:module.prepare(source, authority, DIRECTORY)
    assert error.value.code == "SUCCESSOR_OUTPUT_INVALID"
    assert protected.read_bytes() == original


def test_existing_different_output_and_immutable_measurement_are_preserved(context):
    module, source, authority, _ = context
    module.measure_builds(source, authority, DIRECTORY)
    before = (authority / DIRECTORY / module.BUILD_NAME).read_bytes()
    assert_rejected(context, "SUCCESSOR_OUTPUT_COLLISION", lambda: module.measure_builds(source, authority, DIRECTORY))
    target = authority / DIRECTORY / module.JSON_NAME
    with mutated(target, b"retained different proposal\n"):
        with pytest.raises(module.PreparationError) as error:module.prepare(source, authority, DIRECTORY)
        assert error.value.code == "SUCCESSOR_OUTPUT_COLLISION"
        assert target.read_bytes() == b"retained different proposal\n"
    assert (authority / DIRECTORY / module.BUILD_NAME).read_bytes() == before


@pytest.mark.parametrize("claim", ["qualityApprovalClaimed", "acceptanceClaimed", "executionClaimed", "publicApproval", "scopeQuality", "extraField"])
def test_closed_schema_rejects_synthetic_approval_and_acceptance(context, claim):
    module, _, _, _ = context
    document = measured(context)
    if claim == "executionClaimed":document["acceptance"][claim] = True
    elif claim == "publicApproval":document["successorMaterial"]["proposedMigration"]["publicSurface"]["approvalClaimed"] = True
    elif claim == "scopeQuality":document["successorMaterial"]["approvedScope"]["decision"]["qualityDecisionIncluded"] = True
    elif claim == "extraField":document["approval"] = {"approver": "SYNTHETIC-FIXTURE", "status": "approved"}
    else:document[claim] = True
    document["proposalSha256"] = module.sha(module.canonical(document["successorMaterial"]))
    with pytest.raises(module.PreparationError) as error:module.validate_document(document)
    assert error.value.code == "SUCCESSOR_SCHEMA_INVALID"


@pytest.mark.parametrize("binding", ["quality", "migration", "row", "build", "history"])
def test_recomputed_outer_digest_cannot_hide_inconsistent_bindings(context, binding):
    module, _, _, _ = context
    document = measured(context)
    if binding == "quality":document["requiredQualityBinding"]["publicDriftSha256"] = "0" * 64
    elif binding == "migration":document["successorMaterial"]["proposedMigrationSha256"] = "0" * 64
    elif binding == "row":document["successorMaterial"]["proposedMigration"]["changedAssertions"][0]["rowSha256"] = "0" * 64
    elif binding == "build":document["buildEvidence"]["observations"][-1]["state"] = "available"
    else:document["successorMaterial"]["currentSourceSnapshot"]["historySha256"] = "0" * 64
    document["proposalSha256"] = module.sha(module.canonical(document["successorMaterial"]))
    with pytest.raises(module.PreparationError) as error:module.validate_document(document)
    assert error.value.code in ("SUCCESSOR_BINDING_INVALID", "SUCCESSOR_BUILD_EVIDENCE_INVALID")


def test_restore_measurement_uses_only_local_package_sources(context):
    module, *_ = context
    commands = module.build_commands()
    for name, command in commands:
        if name.endswith("-restore"):
            assert command[command.index("--source") + 1] == str(Path.home() / ".nuget/packages")
            assert "-p:NuGetAudit=false" in command
            assert not any("http" in item for item in command)


@pytest.mark.parametrize("case", ["hidden-tracked", "ignored-source", "wrong-commit"])
def test_copied_root_dependencies_cannot_hide_unapproved_inputs(context, case):
    module, source, _, scope = context
    dependency = source / "references/Hexalith.Builds"
    fixture_git(dependency, "init", "-q")
    # Copy local objects only, and check out the exact fixture's recorded root gitlink.
    fixture_git(dependency, "fetch", "--no-tags", str(source), module.PINS["originalPublication"])
    fixture_git(dependency, "checkout", "--detach", "FETCH_HEAD")
    module.validate_source(source, scope)
    if case == "hidden-tracked":
        relative = "src/Hexalith.Conversations/Example.cs"
        fixture_git(dependency, "update-index", "--assume-unchanged", relative)
        try:
            with mutated(dependency / relative, b"class DirtyDependency {}\n"):
                assert_rejected(context, "SUCCESSOR_ENVIRONMENT_DRIFT")
        finally:fixture_git(dependency, "update-index", "--no-assume-unchanged", relative)
    elif case == "ignored-source":
        exclude = dependency / ".git/info/exclude"
        with mutated(exclude, exclude.read_bytes() + b"src/Ignored.cs\n"):
            with mutated(dependency / "src/Ignored.cs", b"class IgnoredDependency {}\n"):
                assert_rejected(context, "SUCCESSOR_ENVIRONMENT_DRIFT")
    else:
        fixture_git(dependency, "checkout", "--detach", module.PINS["originalCandidate"])
        assert_rejected(context, "SUCCESSOR_ENVIRONMENT_DRIFT")
        fixture_git(dependency, "checkout", "--detach", module.PINS["originalPublication"])
    module.validate_source(source, scope)


@pytest.mark.parametrize("option", ["--approve", "--candidate", "--passed", "--acceptance"])
def test_cli_rejects_caller_authored_approval_or_acceptance(context, capsys, option):
    module, source, authority, _ = context
    assert module.main(["--repository", str(source), "--authorization-root", str(authority),
                        "--output-directory", DIRECTORY, option, "SYNTHETIC-FIXTURE"]) == 1
    output = json.loads(capsys.readouterr().out)
    assert output["blockers"][0]["code"] == "SUCCESSOR_ARGUMENT_INVALID"
    assert output["acceptanceClaimed"] is False
