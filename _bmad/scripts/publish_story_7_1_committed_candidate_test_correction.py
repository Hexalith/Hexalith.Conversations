#!/usr/bin/env python3
"""Generate and verify the non-executable V26 Story 7.1 test correction."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
import tempfile
import types
import unicodedata
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any, NoReturn, Sequence


SCHEMA_VERSION = "hexalith.conversations.story-7.1-committed-candidate-test-correction.v1"
SUCCESSOR_ID = "V26-STORY-7.1-COMMITTED-CANDIDATE-TEST-CORRECTION-v1"
RECORD_PATH = "_bmad-output/planning-artifacts/v26-story-7.1-committed-candidate-test-correction-v1.json"
SCHEMA_PATH = "_bmad/schemas/v26-story-7.1-committed-candidate-test-correction-v1.schema.json"
PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_committed_candidate_test_correction.py"
TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_committed_candidate_test_correction.py"
V25_TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py"

PREDECESSOR_COMMIT = "bb7300a99336f2dcdd75411b173e0050980d6aa3"
PREDECESSOR_TREE = "23d9b096ec1d7070a7f3fcb552b507bbb69b694c"
V25_COMMIT = "dbada954388430a6579b42508f51db806aebf3b8"
V25_TREE = "247843b255634a0c414f94eaf023c02cfdfc300f"
V25_PARENT = "69232b7209c079b0476349818d6251ca4dc83d92"

V25_RECORD_PATH = "_bmad-output/planning-artifacts/v25-story-7.1-preservation-evidence-tooling-successor-v1.json"
V25_SCHEMA_PATH = "_bmad/schemas/v25-story-7.1-preservation-evidence-tooling-successor-v1.schema.json"
V25_PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py"
V25_CONFORMANCE_PATH = "tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs"
V25_SCOPE = (V25_RECORD_PATH, V25_SCHEMA_PATH, V25_PUBLISHER_PATH, V25_TEST_PATH, V25_CONFORMANCE_PATH)
V25_IDENTITIES: dict[str, tuple[str, str, int]] = {
    V25_RECORD_PATH: ("f4b91580be8a23b0f5f95f84bbd0ae3f6d8b7c9b", "7994673c08f874ccc99813261db065081c24d33003ce5e926a679704e9a0dc3a", 18676),
    V25_SCHEMA_PATH: ("130f19e2a146e9b79dc8203972653ae59d9704b8", "f9fc069d16dcd1358c0473609d4d073382774c135694d4dd8b0da82494614a97", 10400),
    V25_PUBLISHER_PATH: ("91a4ff170ffdf44962ca709b9026426ae5f64d9e", "adbe8efbc10a60975b96dc4d0ad4f55a0be2cca8a8ea5e370389eb4ab9737787", 43342),
    V25_TEST_PATH: ("dd08a0375311315836137cff7a1049063cd3bd17", "b65e00945a7b2022c6481903e324609ff9959b2c238369ecfcdd23f2274ab120", 17004),
    V25_CONFORMANCE_PATH: ("5ca0183dc4adb3ac0fbb3191e7071d4464103887", "49b19b84f8fa5b9be8d7bb895a82338dbb5821f9bada011c725d0cb73504f548", 92222),
}

V26_SCOPE = (RECORD_PATH, SCHEMA_PATH, PUBLISHER_PATH, TEST_PATH, V25_TEST_PATH)
V26_NON_RECORD_PATHS = V26_SCOPE[1:]
V26_SCHEMA_SHA256 = "38796306d9dc396c1fb4a45467cc9f4ee5a09535427ccc7bae4af4addd70f247"


class SuccessorError(RuntimeError):
    """Represent a stable fail-closed V26 diagnostic."""

    def __init__(self, code: str, detail: str, state: str = "BLOCKED") -> None:
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail
        self.state = state


@dataclass(frozen=True)
class InputSnapshot:
    """Bind one prospective input to its bytes and filesystem identity."""

    path: str
    device: int
    inode: int
    size: int
    mtime_ns: int
    sha256: str
    object_id: str


def sha256_bytes(content: bytes) -> str:
    """Return a lowercase SHA-256 digest."""

    return hashlib.sha256(content).hexdigest()


def canonical_json(document: Any) -> bytes:
    """Render canonical LF-terminated JSON."""

    return (json.dumps(document, ensure_ascii=False, indent=2) + "\n").encode("utf-8")


def run_git(root: Path, *arguments: str, code: str = "V26_GIT_COMMAND_FAILED") -> bytes:
    """Run Git without shell interpolation and return exact stdout bytes."""

    process = subprocess.run(
        ["git", "-C", str(root), *arguments],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if process.returncode != 0:
        detail = process.stderr.decode("utf-8", "replace").strip()
        raise SuccessorError(code, f"git {' '.join(arguments)}: {detail}")
    return process.stdout


def git_text(root: Path, *arguments: str, code: str = "V26_GIT_COMMAND_FAILED") -> str:
    """Run Git and decode one textual result."""

    return run_git(root, *arguments, code=code).decode("utf-8", "strict").strip()


def resolve_root(start: Path) -> Path:
    """Resolve and verify the owning repository root."""

    try:
        process = subprocess.run(
            ["git", "-C", str(start), "rev-parse", "--show-toplevel"],
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except OSError as error:
        raise SuccessorError("V26_REPOSITORY_UNAVAILABLE", str(error)) from error
    if process.returncode != 0:
        raise SuccessorError("V26_REPOSITORY_UNAVAILABLE", process.stderr.decode("utf-8", "replace").strip())
    root = Path(process.stdout.decode("utf-8", "strict").strip()).resolve()
    if not (root / "Hexalith.Conversations.slnx").is_file():
        raise SuccessorError("V26_REPOSITORY_ROOT_MISMATCH", str(root))
    return root


def resolve_commit(root: Path, revision: str, code: str = "V26_REVISION_UNAVAILABLE") -> str:
    """Resolve one revision to an exact 40-hex commit."""

    commit = git_text(root, "rev-parse", "--verify", f"{revision}^{{commit}}", code=code)
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise SuccessorError(code, repr(commit))
    return commit


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require an exact ancestry relation."""

    process = subprocess.run(
        ["git", "-C", str(root), "merge-base", "--is-ancestor", ancestor, descendant],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if process.returncode == 1:
        raise SuccessorError(code, f"{ancestor} is not an ancestor of {descendant}")
    if process.returncode != 0:
        raise SuccessorError(code, process.stderr.decode("utf-8", "replace").strip())


def commit_parents(root: Path, commit: str) -> tuple[str, ...]:
    """Return the exact parent list for one commit."""

    parts = git_text(root, "rev-list", "--parents", "-n", "1", commit).split()
    if not parts or parts[0] != commit:
        raise SuccessorError("V26_LINEAGE_UNAVAILABLE", commit)
    return tuple(parts[1:])


def commit_tree(root: Path, commit: str) -> str:
    """Return a commit's exact tree identity."""

    return git_text(root, "show", "-s", "--format=%T", commit)


def changed_paths(root: Path, parent: str, commit: str) -> tuple[str, ...]:
    """Return the ordinal exact changed-path set between two commits."""

    raw = run_git(root, "diff", "--name-only", "-z", "--no-renames", parent, commit, "--")
    paths = tuple(sorted((part.decode("utf-8", "strict") for part in raw.split(b"\0") if part), key=str))
    if len(paths) != len(set(paths)):
        raise SuccessorError("V26_CHANGED_PATH_DUPLICATE", repr(paths))
    return paths


def tree_entry(root: Path, revision: str, path: str, code: str) -> tuple[str, str, str]:
    """Return the unique raw mode, type, and object ID for a path."""

    raw = run_git(root, "ls-tree", "-z", "--full-tree", revision, "--", path, code=code)
    rows = [row for row in raw.split(b"\0") if row]
    if len(rows) != 1:
        raise SuccessorError(code, f"{revision}:{path}: rows={len(rows)}")
    metadata, separator, encoded_path = rows[0].partition(b"\t")
    fields = metadata.decode("ascii", "strict").split()
    if not separator or encoded_path.decode("utf-8", "strict") != path or len(fields) != 3:
        raise SuccessorError(code, f"malformed tree row for {revision}:{path}")
    return fields[0], fields[1], fields[2]


def blob_bytes(root: Path, revision: str, path: str, code: str) -> bytes:
    """Read exact committed blob bytes after requiring mode 100644."""

    mode, kind, _ = tree_entry(root, revision, path, code)
    if mode != "100644" or kind != "blob":
        raise SuccessorError(code, f"{revision}:{path}: {mode} {kind}")
    return run_git(root, "show", f"{revision}:{path}", code=code)


def raw_root_gitlinks(root: Path, revision: str) -> list[dict[str, str]]:
    """Derive root gitlinks exclusively from raw tree mode 160000."""

    raw = run_git(root, "ls-tree", "-r", "-z", "--full-tree", revision, code="V26_GITLINK_INVENTORY_UNAVAILABLE")
    links: list[dict[str, str]] = []
    for row in raw.split(b"\0"):
        if not row:
            continue
        metadata, separator, encoded_path = row.partition(b"\t")
        fields = metadata.decode("ascii", "strict").split()
        if not separator or len(fields) != 3:
            raise SuccessorError("V26_GITLINK_INVENTORY_INVALID", repr(row))
        mode, _, object_id = fields
        if mode == "160000":
            links.append({"path": encoded_path.decode("utf-8", "strict"), "mode": mode, "objectId": object_id})
    return sorted(links, key=lambda row: row["path"])


def require_scope(root: Path, commit: str, expected_parent: str, expected_paths: Sequence[str], prefix: str) -> None:
    """Require one single-parent exact-scope, regular-file-only publication."""

    parents = commit_parents(root, commit)
    if parents != (expected_parent,):
        raise SuccessorError(f"{prefix}_PARENT_DRIFT", f"expected={(expected_parent,)!r}; observed={parents!r}")
    observed = changed_paths(root, expected_parent, commit)
    expected = tuple(sorted(expected_paths))
    if observed != expected:
        missing = sorted(set(expected) - set(observed))
        unexpected = sorted(set(observed) - set(expected))
        raise SuccessorError(f"{prefix}_SCOPE_DRIFT", f"missing={missing!r}; unexpected={unexpected!r}")
    for path in expected_paths:
        mode, kind, _ = tree_entry(root, commit, path, f"{prefix}_MODE_DRIFT")
        if mode != "100644" or kind != "blob":
            raise SuccessorError(f"{prefix}_MODE_DRIFT", f"{path}: {mode} {kind}")


def committed_binding(root: Path, revision: str, path: str, code: str = "V26_TOOLING_INPUT_UNAVAILABLE") -> dict[str, Any]:
    """Create one binding from a committed regular-file blob."""

    mode, kind, object_id = tree_entry(root, revision, path, code)
    if mode != "100644" or kind != "blob":
        raise SuccessorError(code, f"{path}: {mode} {kind}")
    content = run_git(root, "show", f"{revision}:{path}", code=code)
    return {"path": path, "mode": mode, "objectId": object_id, "sha256": sha256_bytes(content), "bytes": len(content)}


def load_and_verify_historical_v25(root: Path) -> dict[str, Any]:
    """Load only the authenticated historical V25 publisher and verify V25 at publication."""

    source = blob_bytes(root, V25_COMMIT, V25_PUBLISHER_PATH, "V26_V25_PUBLISHER_UNAVAILABLE")
    _, expected_sha, _ = V25_IDENTITIES[V25_PUBLISHER_PATH]
    if sha256_bytes(source) != expected_sha:
        raise SuccessorError("V26_V25_PUBLISHER_IDENTITY_MISMATCH", sha256_bytes(source))
    module_name = f"_v26_authenticated_v25_{uuid.uuid4().hex}"
    module = types.ModuleType(module_name)
    module.__file__ = f"{V25_COMMIT}:{V25_PUBLISHER_PATH}"
    sys.modules[module_name] = module
    try:
        exec(compile(source, module.__file__, "exec"), module.__dict__)
        document = module.verify_revision(root, V25_COMMIT)
    except Exception as error:
        raise SuccessorError("V26_V25_HISTORICAL_VERIFICATION_FAILED", str(error)) from error
    finally:
        sys.modules.pop(module_name, None)
    if (
        document.get("result") != "PASS"
        or len(document.get("assertionLedger", [])) != 10
        or document.get("preservationEvidence", {}).get("testIdentityCount") != 473
        or document.get("preservationEvidence", {}).get("obligationIdentityCount") != 969
        or document.get("implementationHold") != "ACTIVE"
        or any(document.get(field) is not False for field in ("executionAllowed", "ownerApprovalClaimed", "releaseAuthorized", "pushAuthorized"))
    ):
        raise SuccessorError("V26_V25_HISTORICAL_RESULT_DRIFT", "historical V25 did not retain its closed PASS result")
    return document


def authenticate_v25(root: Path, evaluated: str) -> tuple[dict[str, Any], list[dict[str, Any]], list[dict[str, str]]]:
    """Authenticate all V25 identities before loading its verifier or any V26 record."""

    if resolve_commit(root, V25_COMMIT, "V26_V25_UNAVAILABLE") != V25_COMMIT:
        raise SuccessorError("V26_V25_IDENTITY_MISMATCH", V25_COMMIT)
    require_ancestor(root, V25_COMMIT, evaluated, "V26_V25_NOT_ANCESTOR")
    if commit_tree(root, V25_COMMIT) != V25_TREE:
        raise SuccessorError("V26_V25_TREE_DRIFT", commit_tree(root, V25_COMMIT))
    require_scope(root, V25_COMMIT, V25_PARENT, V25_SCOPE, "V26_V25")

    bindings: list[dict[str, Any]] = []
    for path in V25_SCOPE:
        binding = committed_binding(root, V25_COMMIT, path, "V26_V25_ARTIFACT_UNAVAILABLE")
        expected_object, expected_sha, expected_bytes = V25_IDENTITIES[path]
        if (
            binding["objectId"] != expected_object
            or binding["sha256"] != expected_sha
            or binding["bytes"] != expected_bytes
        ):
            raise SuccessorError(
                "V26_V25_ARTIFACT_MISMATCH",
                f"{path}: expected={(expected_object, expected_sha, expected_bytes)!r}; observed={binding!r}",
            )
        bindings.append(binding)

    for path in V25_SCOPE:
        if path == V25_TEST_PATH:
            continue
        if tree_entry(root, evaluated, path, "V26_V25_RETAINED_ARTIFACT_MISSING") != tree_entry(
            root,
            V25_COMMIT,
            path,
            "V26_V25_ARTIFACT_UNAVAILABLE",
        ):
            raise SuccessorError("V26_V25_RETAINED_ARTIFACT_DRIFT", path)

    links = raw_root_gitlinks(root, V25_COMMIT)
    if len(links) != 10 or raw_root_gitlinks(root, evaluated) != links:
        raise SuccessorError("V26_V25_GITLINK_DRIFT", "V25 and evaluated root gitlinks differ")
    return load_and_verify_historical_v25(root), bindings, links


def checked_worktree_path(root: Path, relative_path: str) -> Path:
    """Resolve one regular-file input without following aliases."""

    if Path(relative_path).is_absolute() or "\\" in relative_path:
        raise SuccessorError("V26_UNSAFE_INPUT_PATH", relative_path)
    segments = relative_path.split("/")
    if not segments or any(segment in ("", ".", "..") for segment in segments):
        raise SuccessorError("V26_UNSAFE_INPUT_PATH", relative_path)
    current = root
    for index, segment in enumerate(segments):
        current = current / segment
        try:
            metadata = current.lstat()
        except OSError as error:
            raise SuccessorError("V26_TOOLING_INPUT_UNAVAILABLE", f"{relative_path}: {error}") from error
        if stat.S_ISLNK(metadata.st_mode):
            raise SuccessorError("V26_TOOLING_INPUT_ALIAS", relative_path)
        if index < len(segments) - 1 and not stat.S_ISDIR(metadata.st_mode):
            raise SuccessorError("V26_TOOLING_INPUT_UNAVAILABLE", f"non-directory component: {relative_path}")
    metadata = current.lstat()
    if not stat.S_ISREG(metadata.st_mode) or metadata.st_nlink != 1:
        raise SuccessorError("V26_TOOLING_INPUT_ALIAS", relative_path)
    if metadata.st_mode & (stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH):
        raise SuccessorError("V26_TOOLING_MODE_DRIFT", f"{relative_path}: executable worktree mode")
    try:
        current.resolve().relative_to(root.resolve())
    except ValueError as error:
        raise SuccessorError("V26_UNSAFE_INPUT_PATH", relative_path) from error
    return current


def capture_inputs(root: Path, paths: Sequence[str] = V26_NON_RECORD_PATHS) -> tuple[InputSnapshot, ...]:
    """Capture exact identities for the four self-excluded V26 inputs."""

    if tuple(paths) != V26_NON_RECORD_PATHS or RECORD_PATH in paths:
        raise SuccessorError("V26_RECORD_SELF_INCLUSION", repr(tuple(paths)), "FAIL")
    snapshots: list[InputSnapshot] = []
    for relative_path in paths:
        path = checked_worktree_path(root, relative_path)
        metadata = path.stat()
        content = path.read_bytes()
        object_id = git_text(root, "hash-object", "--no-filters", "--", relative_path, code="V26_TOOLING_INPUT_UNAVAILABLE")
        snapshots.append(
            InputSnapshot(
                relative_path,
                metadata.st_dev,
                metadata.st_ino,
                metadata.st_size,
                metadata.st_mtime_ns,
                sha256_bytes(content),
                object_id,
            )
        )
    return tuple(snapshots)


def ensure_snapshots(root: Path, expected: tuple[InputSnapshot, ...], expected_head: str) -> None:
    """Reject input replacement, mutation, or candidate drift during a write."""

    if resolve_commit(root, "HEAD", "V26_GENERATION_BASELINE_UNAVAILABLE") != expected_head:
        raise SuccessorError("V26_GENERATION_BASELINE_DRIFT", expected_head)
    if capture_inputs(root) != expected:
        raise SuccessorError("V26_TOOLING_INPUT_DRIFT", "one or more V26 inputs changed during generation")


def snapshot_bindings(snapshots: Sequence[InputSnapshot]) -> list[dict[str, Any]]:
    """Project input snapshots into the closed binding contract."""

    return [
        {"path": row.path, "mode": "100644", "objectId": row.object_id, "sha256": row.sha256, "bytes": row.size}
        for row in snapshots
    ]


def manifest_digest(bindings: Sequence[dict[str, Any]]) -> str:
    """Digest canonical self-excluded manifest rows."""

    material = "".join(
        f"{unicodedata.normalize('NFC', row['path'])}\t{row['mode']}\t{row['objectId']}\t{row['sha256']}\t{row['bytes']}\n"
        for row in bindings
    ).encode("utf-8")
    return sha256_bytes(material)


def assertion_ledger() -> list[dict[str, str]]:
    """Return the fixed, nonempty evaluated assertion inventory."""

    subjects = (
        ("immutable-v25-publication", "V25 retains its direct-child exact five-path publication and tree"),
        ("v25-original-blobs", "all five original V25 blobs retain pinned object, digest, and byte identities"),
        ("historical-v25-verification", "the authenticated V25 verifier passes only at its historical publication"),
        ("successor-five-path-scope", "V26 declares exactly five mode-100644 paths"),
        ("authorized-test-replacement", "only the V25 focused-test blob is replaced by V26"),
        ("self-excluded-manifest", "the four non-record V26 blobs are independently bound"),
        ("raw-root-gitlinks", "all ten root gitlinks equal the immutable V25 inventory"),
        ("sticky-successor-history", "V26 publication identities remain sticky for every evaluated descendant"),
        ("active-non-executable-hold", "V26 grants no approval, execution, release, or push authority"),
    )
    return [
        {"id": f"V26.SUCCESSOR.{index:02d}", "subject": subject, "state": "PASS", "detail": detail}
        for index, (subject, detail) in enumerate(subjects, start=1)
    ]


def build_document(
    bindings: list[dict[str, Any]],
    original_v25: list[dict[str, Any]],
    gitlinks: list[dict[str, str]],
    historical: dict[str, Any],
) -> dict[str, Any]:
    """Build the deterministic V26 record after historical V25 authentication."""

    if set(V26_SCOPE).intersection(V25_SCOPE) != {V25_TEST_PATH}:
        raise SuccessorError("V26_AUTHORIZED_SCOPE_DRIFT", repr(set(V26_SCOPE).intersection(V25_SCOPE)))
    original_test = next(row for row in original_v25 if row["path"] == V25_TEST_PATH)
    replacement_test = next(row for row in bindings if row["path"] == V25_TEST_PATH)
    if original_test["objectId"] == replacement_test["objectId"] or original_test["sha256"] == replacement_test["sha256"]:
        raise SuccessorError("V26_CORRECTION_NOT_APPLIED", V25_TEST_PATH, "FAIL")
    return {
        "schemaVersion": SCHEMA_VERSION,
        "recordType": "COMMITTED_CANDIDATE_TEST_CORRECTION",
        "successorId": SUCCESSOR_ID,
        "lineage": {
            "predecessorCommit": PREDECESSOR_COMMIT,
            "predecessorTree": PREDECESSOR_TREE,
            "v25Commit": V25_COMMIT,
            "v25Tree": V25_TREE,
        },
        "historicalV25": {
            "verificationRevision": V25_COMMIT,
            "result": historical["result"],
            "assertionCount": len(historical["assertionLedger"]),
            "testIdentityCount": historical["preservationEvidence"]["testIdentityCount"],
            "obligationIdentityCount": historical["preservationEvidence"]["obligationIdentityCount"],
            "originalBlobs": original_v25,
        },
        "correction": {
            "authorizedPath": V25_TEST_PATH,
            "originalBinding": original_test,
            "replacementBinding": replacement_test,
        },
        "successorTransaction": {
            "baselineCommit": PREDECESSOR_COMMIT,
            "baselineTree": PREDECESSOR_TREE,
            "exactChangedPaths": list(V26_SCOPE),
            "requiredMode": "100644",
            "selfExcludedManifestSha256": manifest_digest(bindings),
            "manifest": bindings,
        },
        "rootGitlinks": gitlinks,
        "resultSemantics": {
            "states": ["PASS", "FAIL", "BLOCKED"],
            "exitCodes": {"PASS": 0, "FAIL": 1, "BLOCKED": 2},
            "ledgerRequired": True,
        },
        "assertionLedger": assertion_ledger(),
        "blockers": [],
        "result": "PASS",
        "implementationHold": "ACTIVE",
        "executionAllowed": False,
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }


def validate_schema(document: dict[str, Any], schema_bytes: bytes) -> None:
    """Validate against the pinned closed Draft 2020-12 schema."""

    observed = sha256_bytes(schema_bytes)
    if observed != V26_SCHEMA_SHA256:
        raise SuccessorError("V26_SCHEMA_IDENTITY_MISMATCH", f"expected={V26_SCHEMA_SHA256}; observed={observed}")
    try:
        schema = json.loads(schema_bytes)
        import jsonschema  # Imported only after the candidate schema identity is pinned.

        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise SuccessorError("V26_DOCUMENT_SCHEMA_INVALID", str(error), "FAIL") from error


def generate_document(root: Path) -> tuple[dict[str, Any], tuple[InputSnapshot, ...], str]:
    """Generate a prospective V26 document at the exact frozen predecessor."""

    head = resolve_commit(root, "HEAD", "V26_GENERATION_BASELINE_UNAVAILABLE")
    if head != PREDECESSOR_COMMIT or commit_tree(root, head) != PREDECESSOR_TREE:
        raise SuccessorError("V26_GENERATION_BASELINE_DRIFT", f"expected={PREDECESSOR_COMMIT}; observed={head}")
    historical, original_v25, gitlinks = authenticate_v25(root, head)
    snapshots = capture_inputs(root)
    if snapshots[0].sha256 != V26_SCHEMA_SHA256:
        raise SuccessorError("V26_SCHEMA_IDENTITY_MISMATCH", snapshots[0].sha256)
    document = build_document(snapshot_bindings(snapshots), original_v25, gitlinks, historical)
    validate_schema(document, (root / SCHEMA_PATH).read_bytes())
    return document, snapshots, head


def write_document(root: Path) -> dict[str, Any]:
    """Atomically write V26, quarantining only output created by this call on failure."""

    target = root / RECORD_PATH
    if target.exists() or target.is_symlink():
        raise SuccessorError("V26_RECORD_ALREADY_EXISTS", RECORD_PATH, "FAIL")
    document, snapshots, head = generate_document(root)
    content = canonical_json(document)
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary: Path | None = None
    installed = False
    try:
        ensure_snapshots(root, snapshots, head)
        descriptor, name = tempfile.mkstemp(prefix=f".{target.name}.tmp.", dir=target.parent)
        temporary = Path(name)
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        ensure_snapshots(root, snapshots, head)
        os.replace(temporary, target)
        temporary = None
        installed = True
        ensure_snapshots(root, snapshots, head)
        if target.read_bytes() != content:
            raise SuccessorError("V26_PUBLICATION_FINAL_IDENTITY_DRIFT", RECORD_PATH)
        return document
    except BaseException:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
        if installed and target.exists() and target.read_bytes() == content:
            quarantine = target.with_name(f".{target.name}.quarantine.{uuid.uuid4().hex}")
            os.replace(target, quarantine)
        raise


def path_exists(root: Path, revision: str, path: str) -> bool:
    """Return whether a path exists at a revision without conflating Git failure."""

    process = subprocess.run(
        ["git", "-C", str(root), "cat-file", "-e", f"{revision}:{path}"],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if process.returncode not in (0, 1, 128):
        raise SuccessorError("V26_HISTORY_UNAVAILABLE", process.stderr.decode("utf-8", "replace").strip())
    return process.returncode == 0


def locate_publication(root: Path, evaluated: str) -> str:
    """Find the unique sticky V26 record introduction across full ancestry."""

    history = git_text(root, "rev-list", "--reverse", evaluated, code="V26_HISTORY_UNAVAILABLE").splitlines()
    additions: list[str] = []
    for commit in history:
        if not path_exists(root, commit, RECORD_PATH):
            continue
        parents = commit_parents(root, commit)
        if not parents or all(not path_exists(root, parent, RECORD_PATH) for parent in parents):
            additions.append(commit)
    if not additions:
        raise SuccessorError("V26_PUBLICATION_MISSING", evaluated)
    if len(additions) != 1:
        raise SuccessorError("V26_DUPLICATE_PUBLICATION", repr(additions))
    return additions[0]


def verify_revision(root: Path, revision: str = "HEAD") -> dict[str, Any]:
    """Verify a committed V26 candidate and all sticky descendant invariants."""

    evaluated = resolve_commit(root, revision, "V26_EVALUATED_CANDIDATE_UNAVAILABLE")
    historical, original_v25, expected_links = authenticate_v25(root, evaluated)
    publication = locate_publication(root, evaluated)
    require_scope(root, publication, PREDECESSOR_COMMIT, V26_SCOPE, "V26")
    if not path_exists(root, evaluated, RECORD_PATH):
        raise SuccessorError("V26_RECORD_REVERTED_OR_DELETED", RECORD_PATH)

    for path in V26_SCOPE:
        publication_entry = tree_entry(root, publication, path, "V26_PUBLICATION_ARTIFACT_MISSING")
        evaluated_entry = tree_entry(root, evaluated, path, "V26_RECORD_REVERTED_OR_DELETED")
        if publication_entry != evaluated_entry:
            raise SuccessorError("V26_DESCENDANT_ARTIFACT_DRIFT", path)

    if raw_root_gitlinks(root, publication) != expected_links or raw_root_gitlinks(root, evaluated) != expected_links:
        raise SuccessorError("V26_ROOT_GITLINK_DRIFT", "V25, V26 publication, and evaluated gitlinks differ")

    schema_bytes = blob_bytes(root, publication, SCHEMA_PATH, "V26_SCHEMA_UNAVAILABLE")
    bindings = [committed_binding(root, publication, path) for path in V26_NON_RECORD_PATHS]
    expected = build_document(bindings, original_v25, expected_links, historical)
    record_bytes = blob_bytes(root, publication, RECORD_PATH, "V26_RECORD_UNAVAILABLE")
    try:
        observed = json.loads(record_bytes)
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        raise SuccessorError("V26_RECORD_INVALID", str(error), "FAIL") from error
    validate_schema(observed, schema_bytes)
    if observed != expected or record_bytes != canonical_json(expected):
        raise SuccessorError("V26_RECORD_IDENTITY_MISMATCH", "record is not the deterministic authenticated projection")
    if not observed["assertionLedger"]:
        raise SuccessorError("V26_EMPTY_ASSERTION_LEDGER", "zero evaluated assertions cannot pass", "FAIL")
    return observed


def result_envelope(state: str, code: str | None = None, detail: str | None = None) -> dict[str, Any]:
    """Create the stable CLI result envelope."""

    ledger = assertion_ledger() if state == "PASS" else [
        {"id": "V26.SUCCESSOR.ERROR", "subject": "verification", "state": state, "detail": detail or code or state}
    ]
    return {
        "result": state,
        "assertionLedger": ledger,
        "blockers": [] if state == "PASS" else [{"code": code, "detail": detail, "state": state}],
        "implementationHold": "ACTIVE",
        "executionAllowed": False,
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }


def fail(error: SuccessorError) -> NoReturn:
    """Print one deterministic failure envelope and terminate with governed semantics."""

    print(json.dumps(result_envelope(error.state, error.code, error.detail), sort_keys=True))
    raise SystemExit(1 if error.state == "FAIL" else 2)


def parse_args(arguments: Sequence[str] | None = None) -> argparse.Namespace:
    """Parse the closed V26 CLI."""

    parser = argparse.ArgumentParser(description=__doc__)
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--write", action="store_true", help="generate the prospective V26 record at the frozen predecessor")
    action.add_argument("--verify", metavar="REVISION", help="verify a committed V26 candidate")
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="repository working tree")
    return parser.parse_args(arguments)


def main(arguments: Sequence[str] | None = None) -> int:
    """Run generation or verification with PASS/FAIL/BLOCKED exit semantics."""

    options = parse_args(arguments)
    try:
        root = resolve_root(options.root)
        if options.write:
            document = write_document(root)
            print(f"V26_STORY_7_1_TEST_CORRECTION_WRITTEN path={RECORD_PATH} sha256={sha256_bytes(canonical_json(document))}")
        else:
            document = verify_revision(root, options.verify)
            print(f"V26_STORY_7_1_TEST_CORRECTION_OK revision={resolve_commit(root, options.verify)} assertions={len(document['assertionLedger'])}")
        print(json.dumps(result_envelope("PASS"), sort_keys=True))
        return 0
    except SuccessorError as error:
        fail(error)


if __name__ == "__main__":
    sys.exit(main())
