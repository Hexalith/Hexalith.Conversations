"""Focused coverage and shared fixtures for the V27 Story 7.1 lifecycle-evidence authority."""

from __future__ import annotations

import builtins
import importlib.util
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Callable

import jsonschema
import pytest


ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = ROOT / "_bmad/scripts/publish_story_7_1_lifecycle_evidence_authority.py"
SPEC = importlib.util.spec_from_file_location("v27_lifecycle_evidence_authority", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
publisher = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = publisher
SPEC.loader.exec_module(publisher)

# Root of trust pinned in consuming test source: the exact approved specification commit that the
# V27 bootstrap publication must descend from. The publisher itself cannot pin an identity it
# introduces, so the bootstrap is authorized externally through protected-host ancestry instead.
PREDECESSOR_COMMIT = "13684cb0a35e2fe3ddea57c147e34bd02c0c8c16"
DESCENDANT_PATH = "docs/runbooks/v27-fixture-descendant-note.md"


def git(root: Path, *arguments: str) -> str:
    """Run Git in a fixture repository and return stripped stdout."""

    return subprocess.check_output(["git", "-C", str(root), *arguments], text=True).strip()


def commit(root: Path, message: str) -> str:
    """Commit the fixture index without signing and return the new commit."""

    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "commit", "-q", "-m", message],
        check=True,
    )
    return git(root, "rev-parse", "HEAD")


def clone_predecessor(tmp_path: Path, name: str = "repository") -> Path:
    """Clone the pinned history and detach at the exact approved predecessor."""

    root = tmp_path / name
    subprocess.run(
        ["git", "clone", "-q", "--shared", "--no-checkout", str(ROOT), str(root)],
        check=True,
    )
    # Every V27 fact is derived from raw committed objects, so the fixture narrows its working
    # tree to the governed cone and keeps whole-repository checkouts out of the test lane.
    git(root, "sparse-checkout", "set", "--cone", ".github", "_bmad", "_bmad-output", "docs")
    subprocess.run(["git", "-C", str(root), "checkout", "-q", "--detach", PREDECESSOR_COMMIT], check=True)
    git(root, "config", "user.name", "V27 fixture")
    git(root, "config", "user.email", "v27-fixture@example.invalid")
    return root


def copy_bootstrap_inputs(root: Path, extra: dict[str, bytes] | None = None) -> None:
    """Copy the exact eight prospective bootstrap blobs into a predecessor checkout."""

    for relative_path in publisher.BOOTSTRAP_PATHS:
        destination = root / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative_path, destination)
        destination.chmod(0o644)
    for relative_path, content in (extra or {}).items():
        destination = root / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(content)
        destination.chmod(0o644)


def bootstrap_repository(
    tmp_path: Path,
    extra: dict[str, bytes] | None = None,
    mutate: Callable[[Path], None] | None = None,
) -> tuple[Path, str]:
    """Build the exact eight-path bootstrap publication on the approved predecessor."""

    root = clone_predecessor(tmp_path)
    copy_bootstrap_inputs(root, extra)
    if mutate is not None:
        mutate(root)
    git(root, "add", "--", *publisher.BOOTSTRAP_PATHS, *(extra or {}))
    return root, commit(root, "fix(planning): publish the V27 lifecycle-evidence bootstrap")


def published_repository(tmp_path: Path) -> tuple[Path, str, str]:
    """Build the bootstrap plus its deterministic record-only child."""

    root, bootstrap = bootstrap_repository(tmp_path)
    publisher.write_document(root)
    git(root, "add", "--", publisher.RECORD_PATH)
    return root, bootstrap, commit(root, "fix(planning): publish the V27 lifecycle-evidence record")


def add_descendant(root: Path, relative_path: str = DESCENDANT_PATH, text: str = "note\n") -> str:
    """Commit one unrelated descendant that touches no governed path."""

    destination = root / relative_path
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(text, encoding="utf-8")
    destination.chmod(0o644)
    git(root, "add", "--", relative_path)
    return commit(root, f"docs(planning): record V27 fixture descendant {text.strip()}")


def descendant_repository(tmp_path: Path) -> tuple[Path, str, str, str]:
    """Build the bootstrap, the record, and one preserved unrelated descendant."""

    root, bootstrap, publication = published_repository(tmp_path)
    return root, bootstrap, publication, add_descendant(root)


def drift_repository(tmp_path: Path, scenario: str) -> tuple[Path, str, str, str]:
    """Build one governed-drift fixture whose final tree may be fully restored."""

    root, bootstrap, publication = published_repository(tmp_path)
    if scenario == "governed-restore":
        target = root / publisher.VERIFIER_PATH
        original = target.read_bytes()
        target.write_bytes(original + b"\n# fixture drift\n")
        git(root, "add", "--", publisher.VERIFIER_PATH)
        commit(root, "fix(planning): drift the V27 governed verifier")
        target.write_bytes(original)
        git(root, "add", "--", publisher.VERIFIER_PATH)
        tip = commit(root, "fix(planning): restore the V27 governed verifier")
    elif scenario == "gitmodules":
        target = root / publisher.GITMODULES_PATH
        original = target.read_bytes()
        target.write_bytes(original.replace(b"https://github.com/Hexalith/", b"https://example.invalid/"))
        git(root, "add", "--", publisher.GITMODULES_PATH)
        commit(root, "fix(planning): drift the V27 submodule inventory")
        target.write_bytes(original)
        git(root, "add", "--", publisher.GITMODULES_PATH)
        tip = commit(root, "fix(planning): restore the V27 submodule inventory")
    elif scenario == "gitlink":
        links = publisher.raw_root_gitlinks(root, publication)
        moved = "0" * 39 + "1"
        git(root, "update-index", "--cacheinfo", f"160000,{moved},{links[0]['path']}")
        tip = commit(root, "fix(planning): drift one V27 root gitlink")
    elif scenario == "record":
        target = root / publisher.RECORD_PATH
        document = json.loads(target.read_text(encoding="utf-8"))
        document["blockers"] = []
        document["successorId"] = "V27-STORY-7.1-LIFECYCLE-EVIDENCE-AUTHORITY-v1 "
        target.write_bytes(publisher.canonical_json(document))
        git(root, "add", "--", publisher.RECORD_PATH)
        tip = commit(root, "fix(planning): drift the V27 committed record")
    else:  # pragma: no cover - guarded by the parameterized callers
        raise AssertionError(f"unknown scenario {scenario!r}")
    return root, bootstrap, publication, tip


def shallow_repository(tmp_path: Path, depth: int = 1) -> tuple[Path, str]:
    """Clone the published fixture over the smart transport with truncated history.

    A depth-limited clone is the narrowest fault that makes history genuinely unavailable: the
    V27 artifacts are still present at the tip, but no parent, publication, or full-history
    no-touch fact can be derived from the objects that were fetched.
    """

    root, _bootstrap, _publication, tip = descendant_repository(tmp_path)
    git(root, "branch", "-f", "v27-fixture-tip", tip)
    shallow = tmp_path / "shallow"
    subprocess.run(
        [
            "git",
            "clone",
            "-q",
            "--depth",
            str(depth),
            "--branch",
            "v27-fixture-tip",
            "--no-checkout",
            f"file://{root}",
            str(shallow),
        ],
        check=True,
    )
    git(shallow, "sparse-checkout", "set", "--cone", "_bmad")
    subprocess.run(["git", "-C", str(shallow), "checkout", "-q", "--detach", "v27-fixture-tip"], check=True)
    assert git(shallow, "rev-parse", "--is-shallow-repository") == "true"
    observed = git(shallow, "rev-parse", "HEAD")
    assert observed == tip
    return shallow, tip


def truncated_bootstrap_repository(tmp_path: Path) -> tuple[Path, str]:
    """Build a bootstrap whose ancestry is absent without any shallow marker."""

    root = tmp_path / "truncated"
    root.mkdir(parents=True, exist_ok=True)
    subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
    git(root, "config", "user.name", "V27 fixture")
    git(root, "config", "user.email", "v27-fixture@example.invalid")
    (root / publisher.REPOSITORY_MARKER).write_text("<Solution />\n", encoding="utf-8")
    for relative_path in publisher.BOOTSTRAP_IDENTITY_PATHS:
        destination = root / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative_path, destination)
        destination.chmod(0o644)
    git(root, "add", "-A", "--", publisher.REPOSITORY_MARKER, *publisher.BOOTSTRAP_IDENTITY_PATHS)
    return root, commit(root, "fix(planning): publish a rootless V27 bootstrap fixture")


def partial_clone_repository(tmp_path: Path) -> tuple[Path, str]:
    """Clone the published fixture with blobs omitted so object availability is unknown."""

    root, _bootstrap, _publication, tip = descendant_repository(tmp_path)
    git(root, "config", "uploadpack.allowFilter", "true")
    git(root, "config", "uploadpack.allowAnySHA1InWant", "true")
    git(root, "branch", "-f", "v27-fixture-tip", tip)
    partial = tmp_path / "partial"
    subprocess.run(
        [
            "git",
            "clone",
            "-q",
            "--filter=blob:none",
            "--branch",
            "v27-fixture-tip",
            "--no-checkout",
            f"file://{root}",
            str(partial),
        ],
        check=True,
    )
    git(partial, "sparse-checkout", "set", "--cone", "_bmad")
    subprocess.run(["git", "-C", str(partial), "checkout", "-q", "--detach", "v27-fixture-tip"], check=True)
    assert git(partial, "rev-parse", "--is-shallow-repository") == "false"
    assert git(partial, "config", "--get-regexp", r"^(extensions\.partialclone|remote\..*\.promisor)$")
    return partial, tip


def merge_repository(tmp_path: Path) -> tuple[Path, str, str]:
    """Build a two-parent descendant whose first-parent diff hides the second branch."""

    root, bootstrap, publication, tip = descendant_repository(tmp_path)
    git(root, "checkout", "-q", "-b", "v27-fixture-side", publication)
    side = add_descendant(root, "docs/runbooks/v27-fixture-side-note.md", "side\n")
    git(root, "checkout", "-q", "--detach", tip)
    subprocess.run(
        ["git", "-c", "commit.gpgsign=false", "-C", str(root), "merge", "-q", "--no-ff", "-m",
         "fix(planning): merge the V27 fixture side branch", side],
        check=True,
    )
    merged = git(root, "rev-parse", "HEAD")
    assert len(publisher.commit_parents(root, merged)) == 2
    return root, bootstrap, merged


def run_cli(root: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    """Run the publisher command line exactly as an operator would."""

    return subprocess.run(
        [sys.executable, str(MODULE_PATH), "--root", str(root), *arguments],
        check=False,
        capture_output=True,
        text=True,
    )


@pytest.fixture(scope="module")
def published(tmp_path_factory: pytest.TempPathFactory) -> tuple[Path, str, str, str]:
    """Provide one reusable bootstrap, record, and preserved descendant."""

    return descendant_repository(tmp_path_factory.mktemp("v27-published"))


def test_schema_is_closed_pinned_and_route_discriminated() -> None:
    """The pinned V27 schema is a valid closed contract with route-discriminated results."""

    schema_bytes = (ROOT / publisher.SCHEMA_PATH).read_bytes()
    assert publisher.sha256_bytes(schema_bytes) == publisher.V27_SCHEMA_SHA256
    schema = json.loads(schema_bytes)
    jsonschema.Draft202012Validator.check_schema(schema)
    record = schema["$defs"]["publicationRecord"]
    result = schema["$defs"]["resultEnvelope"]
    assert record["additionalProperties"] is False
    assert result["additionalProperties"] is False
    assert record["properties"]["bootstrapTransaction"]["properties"]["exactChangedPaths"]["maxItems"] == 8
    assert record["properties"]["assertionLedger"]["minItems"] == 10
    discriminators = {
        json.dumps(case["if"]["properties"]["assertionLedger"]["prefixItems"][0]["properties"]["id"], sort_keys=True)
        for case in result["allOf"]
    }
    assert discriminators == {
        json.dumps({"const": route}, sort_keys=True)
        for route in ("V27.ROUTE.C1", "V27.ROUTE.C2", "V27.ROUTE.DESCENDANT", "V27.ROUTE.DRIFT", "V27.ROUTE.BLOCKED")
    } | {json.dumps({"type": "string", "pattern": "^V27_[A-Z0-9_]+$"}, sort_keys=True)}
    # Every accepted first-row id is discriminated by one of those branches, and nothing else.
    accepted = result["properties"]["assertionLedger"]["prefixItems"][0]["allOf"][1]["properties"]["id"]["oneOf"]
    assert accepted[0]["enum"] == [
        "V27.ROUTE.C1",
        "V27.ROUTE.C2",
        "V27.ROUTE.DESCENDANT",
        "V27.ROUTE.DRIFT",
        "V27.ROUTE.BLOCKED",
    ]
    assert accepted[1]["pattern"] == "^V27_[A-Z0-9_]+$"


def test_bootstrap_scope_is_exactly_eight_paths() -> None:
    """The declared bootstrap scope is the exact ordered eight-path inventory."""

    assert publisher.BOOTSTRAP_PATHS == tuple(sorted(publisher.BOOTSTRAP_PATHS))
    assert len(publisher.BOOTSTRAP_PATHS) == 8
    assert publisher.RECORD_PATH not in publisher.BOOTSTRAP_PATHS
    assert set(publisher.BOOTSTRAP_IDENTITY_PATHS) <= set(publisher.BOOTSTRAP_PATHS)


def test_exact_bootstrap_is_blocked_until_the_record_lands(tmp_path: Path) -> None:
    """Authorized C1 without C2 returns a nonempty schema-valid BLOCKED envelope."""

    root, bootstrap = bootstrap_repository(tmp_path)
    document = publisher.verify_revision(root, bootstrap, bootstrap)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["exitCode"] == 2
    assert document["blockers"] == [{"code": "V27_C2_PUBLICATION_MISSING", "detail": bootstrap}]
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.C1"
    assert len(document["assertionLedger"]) >= 2
    assert [row["path"] for row in document["observed"]["changedPaths"]] == list(publisher.BOOTSTRAP_PATHS)
    assert document["observed"]["parentCommit"] == PREDECESSOR_COMMIT
    assert document["executionAllowed"] is False


def test_bootstrap_before_the_protected_host_cannot_authorize_itself(tmp_path: Path) -> None:
    """A protected host that predates C1 refuses the whole V27 route."""

    root, bootstrap = bootstrap_repository(tmp_path)
    document = publisher.verify_revision(root, bootstrap, PREDECESSOR_COMMIT)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_BOOTSTRAP_NOT_PROTECTED"
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.BLOCKED"


def test_combined_bootstrap_and_record_landing_is_rejected(tmp_path: Path) -> None:
    """A single commit carrying both C1 and the record is never a valid bootstrap."""

    root = clone_predecessor(tmp_path)
    copy_bootstrap_inputs(root)
    record = root / publisher.RECORD_PATH
    record.parent.mkdir(parents=True, exist_ok=True)
    record.write_bytes(b"{}\n")
    git(root, "add", "--", *publisher.BOOTSTRAP_PATHS, publisher.RECORD_PATH)
    combined = commit(root, "fix(planning): publish a combined V27 landing")
    document = publisher.verify_revision(root, combined, combined)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] in ("V27_BOOTSTRAP_SCOPE_DRIFT", "V27_COMBINED_PUBLICATION_REJECTED")


def test_alternate_bootstrap_scope_is_rejected(tmp_path: Path) -> None:
    """An eight-path bootstrap plus any extra path fails the exact-scope contract."""

    root, bootstrap = bootstrap_repository(tmp_path, {"v27-extra-fixture-path.txt": b"extra\n"})
    document = publisher.verify_revision(root, bootstrap, bootstrap)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_BOOTSTRAP_SCOPE_DRIFT"


def pinned_schema() -> dict[str, object]:
    """Load the V27 schema only after pinning its digest in consuming test source."""

    content = (ROOT / publisher.SCHEMA_PATH).read_bytes()
    assert publisher.sha256_bytes(content) == publisher.V27_SCHEMA_SHA256
    return json.loads(content)


def validate_envelope(document: dict[str, object]) -> None:
    """Validate one result envelope against the digest-pinned closed V27 schema."""

    jsonschema.Draft202012Validator(pinned_schema()).validate(document)
    assert document["assertionLedger"][0]["id"].startswith("V27.ROUTE.")
    assert document["schemaVersion"] == publisher.RESULT_SCHEMA_VERSION
    assert document["effectiveHold"] == "ACTIVE"
    assert document["implementationHold"] == "ACTIVE"
    assert document["assertionLedger"]
    assert document["ownerApprovalClaimed"] is False
    assert document["releaseAuthorized"] is False
    assert document["pushAuthorized"] is False
    assert document["executionAllowed"] is False


def test_record_only_child_passes_with_the_record_as_the_parent_diff(
    published: tuple[Path, str, str, str],
) -> None:
    """Exact C2 returns a nonempty PASS whose observed diff is the record alone."""

    root, bootstrap, publication, _tip = published
    document = publisher.verify_revision(root, publication, bootstrap)
    validate_envelope(document)
    assert document["result"] == "PASS"
    assert document["exitCode"] == 0
    assert document["blockers"] == []
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.C2"
    assert [row["id"] for row in document["assertionLedger"][1:]] == [
        f"V27.SUCCESSOR.{index:02d}" for index in range(1, 11)
    ]
    assert [row["path"] for row in document["observed"]["changedPaths"]] == [publisher.RECORD_PATH]
    assert document["observed"]["parentCommit"] == bootstrap
    assert len(document["observed"]["candidateGitlinks"]) == publisher.GITLINK_COUNT


def test_preserved_descendant_passes_with_its_own_truthful_parent_diff(
    published: tuple[Path, str, str, str],
) -> None:
    """An untouched descendant passes and reports its actual immediate-parent diff."""

    root, bootstrap, publication, tip = published
    document = publisher.verify_revision(root, tip, bootstrap)
    validate_envelope(document)
    assert document["result"] == "PASS"
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.DESCENDANT"
    assert [row["path"] for row in document["observed"]["changedPaths"]] == [DESCENDANT_PATH]
    assert document["observed"]["parentCommit"] == publication
    assert document["observed"]["candidateCommit"] == tip


def test_committed_record_is_the_deterministic_projection(
    published: tuple[Path, str, str, str],
) -> None:
    """The committed record equals the recomputed canonical projection byte for byte."""

    root, bootstrap, publication, _tip = published
    bindings, parent, tree, parent_tree = publisher.validate_bootstrap(root, bootstrap)
    submodules = publisher.submodule_binding(root, bootstrap, "V27_SUBMODULE_BINDING_DRIFT")
    expected = publisher.build_document(bindings, bootstrap, tree, parent, parent_tree, submodules)
    observed = publisher.blob_bytes(root, publication, publisher.RECORD_PATH, "V27_RECORD_UNAVAILABLE")
    assert observed == publisher.canonical_json(expected)
    assert expected["authorization"]["selfAuthorized"] is False
    assert expected["authorization"]["bootstrapParent"] == PREDECESSOR_COMMIT
    assert len(expected["submoduleBinding"]["rootGitlinks"]) == publisher.GITLINK_COUNT
    assert len(expected["assertionLedger"]) == 10
    jsonschema.Draft202012Validator(
        json.loads((ROOT / publisher.SCHEMA_PATH).read_bytes())
    ).validate(expected)


@pytest.mark.parametrize(
    ("scenario", "code"),
    [
        ("governed-restore", "V27_GOVERNED_PATH_TOUCHED"),
        ("gitmodules", "V27_GOVERNED_PATH_TOUCHED"),
        ("gitlink", "V27_GOVERNED_PATH_TOUCHED"),
        ("record", "V27_RECORD_HISTORY_TOUCHED"),
    ],
)
def test_governed_drift_fails_closed_even_after_restoration(
    tmp_path: Path,
    scenario: str,
    code: str,
) -> None:
    """Any governed touch is a stable FAIL, including a fully restored final tree."""

    root, bootstrap, _publication, tip = drift_repository(tmp_path, scenario)
    document = publisher.verify_revision(root, tip, bootstrap)
    validate_envelope(document)
    assert document["result"] == "FAIL"
    assert document["exitCode"] == 1
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.DRIFT"
    assert document["blockers"][0]["code"] == code
    assert document["assertionLedger"]


def test_restored_governed_tree_still_matches_the_bootstrap_bytes(tmp_path: Path) -> None:
    """The restored fixture is byte-identical, so only full history can see the drift."""

    root, bootstrap, _publication, tip = drift_repository(tmp_path, "governed-restore")
    for relative_path in publisher.BOOTSTRAP_PATHS:
        assert publisher.tree_entry(root, tip, relative_path, "V27_TEST") == publisher.tree_entry(
            root, bootstrap, relative_path, "V27_TEST"
        )


def test_replacement_objects_cannot_influence_verification(tmp_path: Path) -> None:
    """A hostile replacement graft changes ambient Git but never the pinned V27 view."""

    root, bootstrap, publication = published_repository(tmp_path)
    git(root, "replace", "--graft", bootstrap, PREDECESSOR_COMMIT + "^")
    ambient = subprocess.check_output(
        ["git", "-C", str(root), "rev-list", "--parents", "-n", "1", bootstrap], text=True
    ).split()
    assert ambient[1] != PREDECESSOR_COMMIT
    document = publisher.verify_revision(root, publication, bootstrap)
    validate_envelope(document)
    assert document["result"] == "PASS"
    assert publisher.commit_parents(root, bootstrap) == (PREDECESSOR_COMMIT,)


def test_hostile_path_cannot_redirect_the_pinned_git_executable(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """The publisher runs the pinned executable with a trusted environment only."""

    root, bootstrap, publication = published_repository(tmp_path)
    hostile = tmp_path / "hostile-bin"
    hostile.mkdir()
    (hostile / "git").write_text("#!/bin/sh\nexit 3\n", encoding="utf-8")
    (hostile / "git").chmod(0o755)
    monkeypatch.setenv("PATH", str(hostile))
    monkeypatch.setenv("GIT_CONFIG_GLOBAL", str(tmp_path / "hostile-config"))
    monkeypatch.setenv("GIT_REPLACE_REF_BASE", "refs/hostile")
    environment = publisher.trusted_environment()
    assert environment["PATH"] == publisher.TRUSTED_EXECUTABLE_PATH
    assert environment["GIT_NO_REPLACE_OBJECTS"] == "1"
    assert publisher.verify_revision(root, publication, bootstrap)["result"] == "PASS"


def test_symlinked_record_parent_cannot_escape_the_repository(tmp_path: Path) -> None:
    """A symlinked publication parent fails closed and installs nothing outside the root."""

    root, _bootstrap = bootstrap_repository(tmp_path)
    outside = tmp_path / "outside"
    outside.mkdir()
    parent = root / "_bmad-output/planning-artifacts"
    parked = root / "_bmad-output/planning-artifacts-parked"
    os.rename(parent, parked)
    os.symlink(outside, parent)
    try:
        with pytest.raises(publisher.SuccessorError) as failure:
            publisher.write_document(root)
    finally:
        os.unlink(parent)
        os.rename(parked, parent)
    assert failure.value.code == "V27_RECORD_PARENT_ALIAS"
    assert failure.value.state == "BLOCKED"
    assert list(outside.iterdir()) == []


def test_existing_record_is_never_overwritten(tmp_path: Path) -> None:
    """A record that already exists fails closed and keeps its foreign bytes."""

    root, _bootstrap = bootstrap_repository(tmp_path)
    target = root / publisher.RECORD_PATH
    target.write_bytes(b"foreign\n")
    with pytest.raises(publisher.SuccessorError) as failure:
        publisher.write_document(root)
    assert failure.value.code == "V27_RECORD_ALREADY_EXISTS"
    assert failure.value.state == "BLOCKED"
    assert target.read_bytes() == b"foreign\n"


def test_concurrent_record_creation_is_not_replaced(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A record created inside the write window survives untouched."""

    root, _bootstrap = bootstrap_repository(tmp_path)
    target = root / publisher.RECORD_PATH
    original = publisher.resolve_commit
    calls: list[str] = []

    def racing(repository: Path, revision: str, code: str = "V27_REVISION_UNAVAILABLE") -> str:
        calls.append(revision)
        if len(calls) == 2:
            target.write_bytes(b"concurrent\n")
        return original(repository, revision, code)

    monkeypatch.setattr(publisher, "resolve_commit", racing)
    with pytest.raises(publisher.SuccessorError) as failure:
        publisher.write_document(root)
    assert failure.value.code == "V27_RECORD_CONCURRENT_PUBLICATION"
    assert failure.value.state == "BLOCKED"
    assert target.read_bytes() == b"concurrent\n"
    assert not any(name.startswith(".") for name in os.listdir(target.parent) if "tmp" in name)


def test_write_requires_the_exact_bootstrap_baseline(tmp_path: Path) -> None:
    """Generation refuses any baseline other than the discovered bootstrap publication."""

    root, bootstrap, _publication = published_repository(tmp_path)
    tip = add_descendant(root, "docs/runbooks/v27-fixture-write-note.md")
    os.unlink(root / publisher.RECORD_PATH)
    with pytest.raises(publisher.SuccessorError) as failure:
        publisher.write_document(root)
    assert failure.value.code == "V27_GENERATION_BASELINE_DRIFT"
    assert failure.value.detail == f"expected={bootstrap}; observed={tip}"


def test_cli_returns_zero_for_the_record_only_publication(
    published: tuple[Path, str, str, str],
) -> None:
    """The direct command line reports the closed PASS envelope and exit 0."""

    root, bootstrap, publication, _tip = published
    completed = run_cli(root, "--verify", publication, "--trusted-host", bootstrap)
    assert completed.returncode == 0
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["result"] == "PASS"


def test_cli_returns_two_for_the_exact_bootstrap(tmp_path: Path) -> None:
    """The direct command line reports BLOCKED and exit 2 with a nonempty ledger."""

    root, bootstrap = bootstrap_repository(tmp_path)
    completed = run_cli(root, "--verify", bootstrap, "--trusted-host", bootstrap)
    assert completed.returncode == 2
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_C2_PUBLICATION_MISSING"
    assert len(document["assertionLedger"]) >= 2


def test_cli_returns_one_for_governed_drift(tmp_path: Path) -> None:
    """The direct command line reports FAIL and exit 1 for restored governed drift."""

    root, bootstrap, _publication, tip = drift_repository(tmp_path, "governed-restore")
    completed = run_cli(root, "--verify", tip, "--trusted-host", bootstrap)
    assert completed.returncode == 1
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["result"] == "FAIL"
    assert document["blockers"][0]["code"] == "V27_GOVERNED_PATH_TOUCHED"
    assert document["assertionLedger"]


def test_cli_requires_explicit_protected_host_provenance(
    published: tuple[Path, str, str, str],
) -> None:
    """Verification without recorded protected-host provenance fails closed."""

    root, _bootstrap, publication, _tip = published
    completed = run_cli(root, "--verify", publication)
    assert completed.returncode == 2
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_TRUSTED_HOST_REQUIRED"
    assert document["assertionLedger"]


def test_cli_write_is_idempotently_refused_after_publication(
    published: tuple[Path, str, str, str],
) -> None:
    """A committed record cannot be republished over its own committed bytes."""

    root, _bootstrap, _publication, _tip = published
    completed = run_cli(root, "--write")
    assert completed.returncode == 1
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["result"] == "FAIL"
    assert document["blockers"][0]["code"] in (
        "V27_RECORD_ALREADY_EXISTS",
        "V27_GENERATION_BASELINE_DRIFT",
    )


def test_unavailable_history_blocks_and_is_never_pass_fail_or_not_applicable(tmp_path: Path) -> None:
    """Truncated history is a stable BLOCKED result with a nonempty ledger."""

    shallow, tip = shallow_repository(tmp_path)
    document = publisher.verify_revision(shallow, tip, tip)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["exitCode"] == 2
    assert document["result"] not in ("PASS", "FAIL", "not-applicable")
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.BLOCKED"
    assert document["assertionLedger"]
    assert document["blockers"][0]["code"] == "V27_HISTORY_UNAVAILABLE"
    assert "shallow" in document["blockers"][0]["detail"]
    assert document["executionAllowed"] is False


def test_partial_history_blocks_at_every_evaluated_depth(tmp_path: Path) -> None:
    """A deeper but still truncated clone blocks with the same stable code."""

    shallow, tip = shallow_repository(tmp_path, depth=2)
    document = publisher.verify_revision(shallow, tip, tip)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_HISTORY_UNAVAILABLE"
    assert publisher.git_text(shallow, "rev-parse", "--is-shallow-repository") == "true"


def test_truncated_bootstrap_ancestry_blocks_without_a_shallow_marker(tmp_path: Path) -> None:
    """A bootstrap with no available parent blocks as unavailable history, not as drift."""

    root, bootstrap = truncated_bootstrap_repository(tmp_path)
    assert publisher.git_text(root, "rev-parse", "--is-shallow-repository") == "false"
    assert publisher.commit_parents(root, bootstrap) == ()
    document = publisher.verify_revision(root, bootstrap, bootstrap)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_HISTORY_UNAVAILABLE"
    assert "truncated" in document["blockers"][0]["detail"]
    assert document["assertionLedger"]


def test_cli_returns_two_for_unavailable_history(tmp_path: Path) -> None:
    """Both command-line actions refuse truncated history with BLOCKED and exit 2."""

    shallow, tip = shallow_repository(tmp_path)
    verified = run_cli(shallow, "--verify", tip, "--trusted-host", tip)
    assert verified.returncode == 2
    document = json.loads(verified.stdout)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_HISTORY_UNAVAILABLE"
    assert document["assertionLedger"]

    written = run_cli(shallow, "--write")
    assert written.returncode == 2
    generated = json.loads(written.stdout)
    validate_envelope(generated)
    assert generated["result"] == "BLOCKED"
    assert generated["blockers"][0]["code"] == "V27_HISTORY_UNAVAILABLE"
    assert generated["assertionLedger"]
    assert not (shallow / publisher.RECORD_PATH).exists()


def test_schema_rejects_every_envelope_without_a_route_discriminator() -> None:
    """Route discrimination is closed: a non-route first ledger row can never validate."""

    validator = jsonschema.Draft202012Validator(pinned_schema())
    envelope = {
        "schemaVersion": publisher.RESULT_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "effectiveHold": "ACTIVE",
        "implementationHold": "ACTIVE",
        "observed": publisher.empty_observation(),
        "assertionLedger": [
            {"id": "V27.ROUTE.BLOCKED", "subject": "route", "state": "BLOCKED", "detail": "d"},
        ],
        "blockers": [{"code": "V27_HISTORY_UNAVAILABLE", "detail": "d"}],
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
        "executionAllowed": False,
    }
    assert validator.is_valid(envelope)

    # A protected host that blocks before the routed publisher names its blocker code first.
    # That form is discriminated too: it is constrained to a failing, blocker-carrying result.
    host_failure = json.loads(json.dumps(envelope))
    host_failure["assertionLedger"][0]["id"] = "V27_HISTORY_UNAVAILABLE"
    assert validator.is_valid(host_failure)

    for impostor in ("V27.SUCCESSOR.01", "V27.ROUTE.UNKNOWN", "V27.ROUTE.c2"):
        candidate = json.loads(json.dumps(envelope))
        candidate["assertionLedger"][0]["id"] = impostor
        assert not validator.is_valid(candidate), impostor

    # The exact reported gap: neither impostor may validate as a passing, empty-diff envelope.
    for impostor in ("V27.SUCCESSOR.01", "V27_HISTORY_UNAVAILABLE"):
        passing = json.loads(json.dumps(envelope))
        passing["assertionLedger"][0] = {"id": impostor, "subject": "s", "state": "PASS", "detail": "d"}
        passing["result"] = "PASS"
        passing["exitCode"] = 0
        passing["blockers"] = []
        assert not validator.is_valid(passing), impostor


def test_result_envelope_is_self_validated_before_it_is_returned(
    published: tuple[Path, str, str, str],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A contradictory envelope is replaced by a governed blocker, never returned as PASS."""

    root, bootstrap, publication, _tip = published
    honest = publisher.result_envelope

    def contradictory(result, route, observed, ledger, blockers):
        # Corrupt only the first envelope so the governed replacement is built honestly.
        publisher.result_envelope = honest
        envelope = honest(result, route, observed, ledger, blockers)
        envelope["assertionLedger"][0]["id"] = "V27.SUCCESSOR.01"
        return envelope

    monkeypatch.setattr(publisher, "result_envelope", contradictory)
    document = publisher.verify_revision(root, publication, bootstrap)
    monkeypatch.undo()
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_RESULT_ENVELOPE_INVALID"
    assert "V27_DOCUMENT_SCHEMA_INVALID" in document["blockers"][0]["detail"]


def test_unavailable_validation_tooling_is_blocked_not_failed(
    published: tuple[Path, str, str, str],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A missing jsonschema dependency is unavailable tooling, never governed drift."""

    root, bootstrap, publication, _tip = published
    real_import = builtins.__import__

    def refuse(name: str, *arguments: object, **keywords: object) -> object:
        if name == "jsonschema":
            raise ImportError("no module named jsonschema")
        return real_import(name, *arguments, **keywords)

    monkeypatch.setattr(builtins, "__import__", refuse)
    with pytest.raises(publisher.SuccessorError) as failure:
        publisher.validate_schema({}, (ROOT / publisher.SCHEMA_PATH).read_bytes())
    monkeypatch.undo()
    assert failure.value.code == "V27_TOOLING_UNAVAILABLE"
    assert failure.value.state == "BLOCKED"


def test_partial_clone_blocks_like_every_other_unavailable_history(tmp_path: Path) -> None:
    """A blob-filtered clone reports complete depth but unknown object availability."""

    partial, tip = partial_clone_repository(tmp_path)
    document = publisher.verify_revision(partial, tip, tip)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_HISTORY_UNAVAILABLE"
    assert "partial clone" in document["blockers"][0]["detail"]


def test_merge_candidates_are_rejected_not_evaluated_on_a_first_parent_diff(tmp_path: Path) -> None:
    """A two-parent candidate never passes on the diff of its first parent alone."""

    root, bootstrap, merged = merge_repository(tmp_path)
    document = publisher.verify_revision(root, merged, bootstrap)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_CANDIDATE_PARENT_DRIFT"
    assert document["assertionLedger"][0]["id"] == "V27.ROUTE.BLOCKED"


def test_publication_io_failure_is_a_governed_envelope_not_a_traceback(tmp_path: Path) -> None:
    """An unwritable publication directory exits with governed JSON, never a raw traceback."""

    root, _bootstrap = bootstrap_repository(tmp_path)
    parent = root / "_bmad-output/planning-artifacts"
    mode = parent.stat().st_mode
    parent.chmod(0o500)
    try:
        completed = run_cli(root, "--write")
    finally:
        parent.chmod(mode)
    assert completed.returncode == 2
    assert "Traceback" not in completed.stderr
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["result"] == "BLOCKED"
    assert document["blockers"][0]["code"] == "V27_PUBLICATION_IO_FAILED"


def test_failed_publication_leaves_no_temporary_or_quarantine_dirt(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A failure after installation removes only this call's bytes and leaves no dirt."""

    root, _bootstrap = bootstrap_repository(tmp_path)
    target = root / publisher.RECORD_PATH
    real_fsync = os.fsync
    calls: list[int] = []

    def failing(descriptor: int) -> None:
        calls.append(descriptor)
        if len(calls) == 2:  # the directory sync, after the no-replace link installed the record
            raise OSError(28, "no space left on device")
        real_fsync(descriptor)

    monkeypatch.setattr(os, "fsync", failing)
    with pytest.raises(OSError):
        publisher.write_document(root)
    monkeypatch.undo()
    assert not target.exists()
    assert [name for name in os.listdir(target.parent) if name.startswith(f".{target.name}")] == []


def test_write_refuses_protected_host_provenance_it_never_reads(tmp_path: Path) -> None:
    """Generation rejects `--trusted-host` instead of silently ignoring it."""

    root, bootstrap = bootstrap_repository(tmp_path)
    completed = run_cli(root, "--write", "--trusted-host", bootstrap)
    assert completed.returncode == 2
    document = json.loads(completed.stdout)
    validate_envelope(document)
    assert document["blockers"][0]["code"] == "V27_TRUSTED_HOST_NOT_APPLICABLE"
    assert not (root / publisher.RECORD_PATH).exists()


def test_a_dirtied_worktree_schema_cannot_change_a_governed_verdict(tmp_path: Path) -> None:
    """Outgoing envelopes are validated against committed bytes, never the working tree."""

    root, bootstrap, _publication, tip = drift_repository(tmp_path, "governed-restore")
    clean = publisher.verify_revision(root, tip, bootstrap)
    validate_envelope(clean)
    assert (clean["result"], clean["exitCode"]) == ("FAIL", 1)
    assert clean["blockers"][0]["code"] == "V27_GOVERNED_PATH_TOUCHED"

    schema = root / publisher.SCHEMA_PATH
    original = schema.read_bytes()
    schema.write_bytes(original + b"\n")
    try:
        assert publisher.sha256_bytes(schema.read_bytes()) != publisher.V27_SCHEMA_SHA256
        dirtied = publisher.verify_revision(root, tip, bootstrap)
    finally:
        schema.write_bytes(original)
    assert dirtied == clean

    completed = run_cli(root, "--verify", tip, "--trusted-host", bootstrap)
    assert completed.returncode == 1
    assert json.loads(completed.stdout)["blockers"][0]["code"] == "V27_GOVERNED_PATH_TOUCHED"


def test_committed_record_is_the_deterministic_projection_of_the_committed_bootstrap() -> None:
    """The repository's own committed record is recomputed, so one altered byte fails here.

    The record binds the bootstrap commit that carries this file, so its digest cannot be pinned as
    a literal here without a cycle. Recomputing the projection from committed objects is the pin.
    """

    head = publisher.resolve_commit(ROOT, "HEAD", "V27_TEST")
    publications = publisher.additions(ROOT, head, publisher.RECORD_PATH)
    assert len(publications) <= 1
    if not publications:
        assert not publisher.path_exists(ROOT, head, publisher.RECORD_PATH)
        return
    bootstrap = publisher.discover_bootstrap(ROOT, head)
    document = publisher.verify_revision(ROOT, head, bootstrap)
    validate_envelope(document)
    assert document["result"] == "PASS", document["blockers"]
    assert document["assertionLedger"][0]["id"] in ("V27.ROUTE.C2", "V27.ROUTE.DESCENDANT")
    committed = publisher.blob_bytes(ROOT, publications[0], publisher.RECORD_PATH, "V27_TEST")
    bindings, parent, tree, parent_tree = publisher.validate_bootstrap(ROOT, bootstrap)
    submodules = publisher.submodule_binding(ROOT, bootstrap, "V27_TEST")
    expected = publisher.build_document(bindings, bootstrap, tree, parent, parent_tree, submodules)
    assert committed == publisher.canonical_json(expected)
