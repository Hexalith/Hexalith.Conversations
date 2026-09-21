#!/usr/bin/env python3
"""Generate and verify the non-executable V25 Story 7.1 preservation successor."""

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
import unicodedata
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any, NoReturn, Sequence


SCHEMA_VERSION = "hexalith.conversations.story-7.1-preservation-evidence-tooling-successor.v1"
SUCCESSOR_ID = "V25-STORY-7.1-PRESERVATION-EVIDENCE-TOOLING-SUCCESSOR-v1"
RECORD_PATH = "_bmad-output/planning-artifacts/v25-story-7.1-preservation-evidence-tooling-successor-v1.json"
SCHEMA_PATH = "_bmad/schemas/v25-story-7.1-preservation-evidence-tooling-successor-v1.schema.json"
PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_preservation_evidence_successor.py"
TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_preservation_evidence_successor.py"
CONFORMANCE_PATH = "tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs"

PREDECESSOR_COMMIT = "69232b7209c079b0476349818d6251ca4dc83d92"
PREDECESSOR_TREE = "9d0b726f205782138e19bd35b752750dfea87fde"
V23_COMMIT = "5a7234b922371b5d0a12085a444d93783263f278"
V23_TREE = "1d5ab0e9fd2e4f18bef0f605950a801723613b1e"
V24_COMMIT = "20e2cdd2b37e6387055b241c7ee45fc54e836742"
V24_TREE = "7422b9ee859e668ed8293b0c04005c964ab28d52"
RC2_BASE_COMMIT = "2d2ae57db1fdcc164fe01ac4b1d99af15c31b324"
RC2_PUBLICATION_COMMIT = "5ad5d3c0fb57c6ce5f5bbd4567ae9b5a5397e60d"
RC2_LOG_PROVENANCE_COMMIT = "df482b4e652907e100f763615a8a8c4370565066"
RC2_CANDIDATE_DIGEST = "1ba2f3e23b56dc3c1caa05addb175461311e472487c5f2c4580317ee109d8840"

# This digest is deliberately pinned in the verifier rather than accepted from
# the generated record. It is filled from the reviewed schema bytes.
V25_SCHEMA_SHA256 = "f9fc069d16dcd1358c0473609d4d073382774c135694d4dd8b0da82494614a97"

V23_RECORD_PATH = "_bmad-output/planning-artifacts/v23-story-7.1-entry-candidate-v1.json"
V23_SCHEMA_PATH = "_bmad/schemas/v23-story-7.1-entry-authority-v1.schema.json"
V23_PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_entry_authority.py"
V24_RECORD_PATH = "_bmad-output/planning-artifacts/v24-story-7.1-entry-tooling-correction-v1.json"
V24_SCHEMA_PATH = "_bmad/schemas/v24-story-7.1-entry-tooling-correction-v1.schema.json"
V24_PUBLISHER_PATH = V23_PUBLISHER_PATH

V23_IDENTITIES = {
    V23_RECORD_PATH: "caa3dfbefeb716233081b0df3c7dcbf2a326f105594b23d42db1624fc4cf9d39",
    V23_SCHEMA_PATH: "d11340d9b2665c5295a4408f9e6b26218001a61f118d6ec979d6e9ca4da3ea1b",
    V23_PUBLISHER_PATH: "9c1ea485a5906d0a69e4c99058494ffab86b2f96ed47a936ab95d0d4d8be7364",
}
V24_IDENTITIES = {
    V24_RECORD_PATH: "dc9b6d26596e39fb10753a38e482d24ab0d3ac7a203331c7717ab284b6129a3e",
    V24_SCHEMA_PATH: "2cfb5fa98cc523375202deb6e00bd2024a44490a604fc0dbf9785fd13d9b195a",
    V24_PUBLISHER_PATH: "be3419d41ff48b741d6c156662ad87fd2c04fc8530bf4f202e959e92424f0c86",
}

V23_SCOPE = (
    ".github/workflows/planning-authority-preflight.yml",
    V23_RECORD_PATH,
    V23_SCHEMA_PATH,
    V23_PUBLISHER_PATH,
    "_bmad/scripts/resolve_current_planning_authority.py",
    "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py",
    "_bmad/scripts/tests/test_resolve_current_planning_authority.py",
    "_bmad/scripts/tests/test_verify_evidence_boundary.py",
    "_bmad/scripts/verify_evidence_boundary.py",
)
V24_SCOPE = (
    V24_RECORD_PATH,
    V24_SCHEMA_PATH,
    V24_PUBLISHER_PATH,
    "_bmad/scripts/resolve_current_planning_authority.py",
    "_bmad/scripts/tests/test_publish_story_7_1_entry_authority.py",
    "_bmad/scripts/tests/test_resolve_current_planning_authority.py",
    "_bmad/scripts/tests/test_verify_evidence_boundary.py",
    "_bmad/scripts/verify_evidence_boundary.py",
)
V25_SCOPE = (RECORD_PATH, SCHEMA_PATH, PUBLISHER_PATH, TEST_PATH, CONFORMANCE_PATH)
V25_NON_RECORD_PATHS = V25_SCOPE[1:]

RC2_SCOPE = (
    ".agents/skills/bmad-build-auto/step-04-review.md",
    ".agents/skills/bmad-build/step-05-present.md",
    ".agents/skills/bmad-build/step-oneshot.md",
    ".agents/skills/bmad-code-review/steps/step-04-present.md",
    ".claude/skills/bmad-build-auto/step-04-review.md",
    ".claude/skills/bmad-build/step-05-present.md",
    ".claude/skills/bmad-build/step-oneshot.md",
    ".claude/skills/bmad-code-review/steps/step-04-present.md",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-build-receipt.json",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-dotnet-info.txt",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-dependency-inventory.json",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-receipt.json",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-remediated.xml",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-run-receipt.json",
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-semantic-results.json",
    "_bmad-output/implementation-artifacts/spec-remediate-preservation-traceability-v3-conformance.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.memlog.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md",
    "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
    "_bmad/scripts/generate_preservation_traceability_manifest_v3.py",
    "_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py",
    "_bmad/scripts/tests/test_generate_preservation_traceability_manifest.py",
    "_bmad/scripts/tests/test_verify_submodule_promotion.py",
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.json",
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.sha256",
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json",
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.md",
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.schema.json",
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.sha256",
    "docs/release-evidence/projection-read-store-population-proof-v2-binding-resolution.json",
    "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs",
    CONFORMANCE_PATH,
    "tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs",
    "tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs",
)

# path -> (retained revision, object id, sha256, bytes)
FROZEN_ARTIFACTS: dict[str, tuple[str, str, str, int]] = {
    "docs/release-evidence/preservation-traceability-manifest-v3.json": (
        "4b1ac8e0a792bd884ca22322ff2d957e93981766", "ee38d9f8076e8aa235a85fb29d0ea2f29aa5f5bf", "a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc", 1667463),
    "docs/release-evidence/preservation-traceability-manifest-v3-owner-approval.json": (
        "4b1ac8e0a792bd884ca22322ff2d957e93981766", "13cfc8a8975f38a4e858fe9055054b8d0d33da83", "23e07e8339d8822c35ef665d44a60238e237c4c29e37560d6f475585dd27df5a", 4655),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-build-receipt.json": (
        RC2_PUBLICATION_COMMIT, "0fd2ad6e6aea4d0d6eb67ecc14c9b44f32a13fa1", "638210b9bd40dcc0428c2eefa286caab57e2ea645d8fbd2bad456d3b294f8b43", 2731),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-dotnet-info.txt": (
        RC2_PUBLICATION_COMMIT, "368f882fae12ec9c714f2dfb2ff35fa98ddc9c9a", "d649c0709ac9cc2b8e07f561b0f3458aab2570ce8aaaeb0e2efe907b4eed3be0", 1770),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-dependency-inventory.json": (
        RC2_PUBLICATION_COMMIT, "774d2dc219f09f71e3584d0983fd914c8faa2c5f", "b003089f8044e8ca053c4aa4975d4dcabf01cfaa326547bdd15a79b00b09a611", 6063),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore-receipt.json": (
        RC2_PUBLICATION_COMMIT, "b4ae610c3c7f9ffd6ed540430e4b83cd3fbd15c3", "ebae68916bef68e96d3f3e98c40b6795a566bdebf7b5095001807292438bc25b", 1810),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/candidate-restore.log": (
        RC2_LOG_PROVENANCE_COMMIT, "5642bf0de265135023fd5c87c9970610cfd4f4a0", "1407c6c5837cdbe1a170e08a04b3d4d2b9cae2236883636781804c7edd6e7b00", 2902),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-build-remediated.log": (
        RC2_LOG_PROVENANCE_COMMIT, "3fb064f50ef44b2b9b57a36cf1f75418effc6653", "36f09a1249f840ffe33cbc11897bf01758af76b4b78820501723632b7419afa3", 6198),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-remediated.xml": (
        RC2_PUBLICATION_COMMIT, "902ed9f3f7606c43c33cac786e1f7ed90c8a723e", "bfac5f1745493c13c3cf3b5c40528ee8e04376e436bdffb95eaf767add8ae7f8", 326351),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-run-receipt.json": (
        RC2_PUBLICATION_COMMIT, "95921b6aed83380c40253cd4fc33d6802f8b12ab", "c8ba4ac11702ff4001e8d24dbf6cbb66f9ebeb899351aac3a8bf6b94882b5c65", 90870),
    "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2/conformance-semantic-results.json": (
        RC2_PUBLICATION_COMMIT, "0245f9a4605aab362ab4a22351325bd54466c9c4", "c2e09f7d041bd2cd3de6c271383efd276be70347c58a81d00422b12e77818236", 90023),
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.json": (
        RC2_PUBLICATION_COMMIT, "e38579bece6ef684156ba0b44897b5b9ba703b2e", "9d6e994069332e80a26734a2df6c523fe8cded068c9be92fc36bdb48449626df", 4379),
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.sha256": (
        RC2_PUBLICATION_COMMIT, "0d64a76c8c088d993c5dda2234b1bef02217b0c3", "6d958c26ae7358356d54916fec489bbf1a5174ce66ac1aba032f567005f96082", 131),
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json": (
        RC2_PUBLICATION_COMMIT, "4b231130f4334e7e41b3a48b7c7874a3809bb4f5", "d033756c84680dc6e671368966c60a1e2ee120965ea04dd0edf4e760cb702864", 1675128),
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.md": (
        RC2_PUBLICATION_COMMIT, "22e507fccfe8a687c995f2e621671e2546b6bbcd", "22173e4de291366915c8ecf7de61652d18403eabf34645b68afa943e4a68e0dd", 1000),
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.schema.json": (
        RC2_PUBLICATION_COMMIT, "e8d4b09ea385501a4634937f42e3bd7fd0f82376", "2ae52028517ca00f9c0a6e8c736b77a664f7092bbbc45b89d52b522dd4c95bf8", 53812),
    "docs/release-evidence/preservation-traceability-manifest-v3-rc2.sha256": (
        RC2_PUBLICATION_COMMIT, "4b9b3bc5e2157eade3d0c503a779948029438395", "90fb722d588e662bec38edb986ed03f1324ee7812e29078f4009db806208f7bd", 344),
}


class SuccessorError(RuntimeError):
    """Represent a stable fail-closed V25 diagnostic."""

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
    """Render the canonical LF-terminated JSON representation."""

    return (json.dumps(document, ensure_ascii=False, indent=2) + "\n").encode("utf-8")


def run_git(root: Path, *arguments: str, code: str = "V25_GIT_COMMAND_FAILED") -> bytes:
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


def git_text(root: Path, *arguments: str, code: str = "V25_GIT_COMMAND_FAILED") -> str:
    """Run Git and decode a single textual result."""

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
        raise SuccessorError("V25_REPOSITORY_UNAVAILABLE", str(error)) from error
    if process.returncode != 0:
        raise SuccessorError(
            "V25_REPOSITORY_UNAVAILABLE",
            process.stderr.decode("utf-8", "replace").strip(),
        )
    root = Path(process.stdout.decode("utf-8", "strict").strip()).resolve()
    if not (root / "Hexalith.Conversations.slnx").is_file():
        raise SuccessorError("V25_REPOSITORY_ROOT_MISMATCH", str(root))
    return root


def resolve_commit(root: Path, revision: str, code: str = "V25_REVISION_UNAVAILABLE") -> str:
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
    """Return the exact parent list for a commit."""

    parts = git_text(root, "rev-list", "--parents", "-n", "1", commit).split()
    if not parts or parts[0] != commit:
        raise SuccessorError("V25_LINEAGE_UNAVAILABLE", commit)
    return tuple(parts[1:])


def commit_tree(root: Path, commit: str) -> str:
    """Return a commit's exact tree identity."""

    return git_text(root, "show", "-s", "--format=%T", commit)


def changed_paths(root: Path, parent: str, commit: str) -> tuple[str, ...]:
    """Return the ordinal exact changed-path set between two commits."""

    raw = run_git(root, "diff", "--name-only", "-z", "--no-renames", parent, commit, "--")
    paths = tuple(sorted((part.decode("utf-8", "strict") for part in raw.split(b"\0") if part), key=str))
    if len(paths) != len(set(paths)):
        raise SuccessorError("V25_CHANGED_PATH_DUPLICATE", repr(paths))
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

    raw = run_git(root, "ls-tree", "-r", "-z", "--full-tree", revision, code="V25_GITLINK_INVENTORY_UNAVAILABLE")
    links: list[dict[str, str]] = []
    for row in raw.split(b"\0"):
        if not row:
            continue
        metadata, separator, encoded_path = row.partition(b"\t")
        fields = metadata.decode("ascii", "strict").split()
        if not separator or len(fields) != 3:
            raise SuccessorError("V25_GITLINK_INVENTORY_INVALID", repr(row))
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


def require_blob_identities(root: Path, revision: str, expected: dict[str, str], prefix: str) -> None:
    """Require pinned committed bytes for every named identity."""

    for path, digest in expected.items():
        content = blob_bytes(root, revision, path, f"{prefix}_ARTIFACT_UNAVAILABLE")
        observed = sha256_bytes(content)
        if observed != digest:
            raise SuccessorError(f"{prefix}_ARTIFACT_MISMATCH", f"{path}: expected={digest}; observed={observed}")


def authenticate_predecessors(root: Path, evaluated: str) -> list[dict[str, str]]:
    """Authenticate immutable V23 and V24 before trusting any V25 bytes."""

    if resolve_commit(root, V23_COMMIT, "V25_V23_UNAVAILABLE") != V23_COMMIT:
        raise SuccessorError("V25_V23_IDENTITY_MISMATCH", V23_COMMIT)
    if resolve_commit(root, V24_COMMIT, "V25_V24_UNAVAILABLE") != V24_COMMIT:
        raise SuccessorError("V25_V24_IDENTITY_MISMATCH", V24_COMMIT)
    require_ancestor(root, V24_COMMIT, evaluated, "V25_V24_NOT_ANCESTOR")
    if commit_tree(root, V23_COMMIT) != V23_TREE:
        raise SuccessorError("V25_V23_TREE_DRIFT", commit_tree(root, V23_COMMIT))
    if commit_tree(root, V24_COMMIT) != V24_TREE:
        raise SuccessorError("V25_V24_TREE_DRIFT", commit_tree(root, V24_COMMIT))
    require_scope(root, V23_COMMIT, commit_parents(root, V23_COMMIT)[0], V23_SCOPE, "V25_V23")
    require_scope(root, V24_COMMIT, V23_COMMIT, V24_SCOPE, "V25_V24")
    require_blob_identities(root, V23_COMMIT, V23_IDENTITIES, "V25_V23")
    require_blob_identities(root, V24_COMMIT, V24_IDENTITIES, "V25_V24")
    retained_descendant_identities = {
        V23_RECORD_PATH: V23_IDENTITIES[V23_RECORD_PATH],
        V23_SCHEMA_PATH: V23_IDENTITIES[V23_SCHEMA_PATH],
        **V24_IDENTITIES,
    }
    for path, expected_sha in retained_descendant_identities.items():
        content = blob_bytes(root, evaluated, path, "V25_PREDECESSOR_ARTIFACT_MISSING")
        if sha256_bytes(content) != expected_sha:
            raise SuccessorError(
                "V25_PREDECESSOR_DESCENDANT_DRIFT",
                f"{path}: expected={expected_sha}; observed={sha256_bytes(content)}",
            )
    v23_links = raw_root_gitlinks(root, V23_COMMIT)
    v24_links = raw_root_gitlinks(root, V24_COMMIT)
    if len(v24_links) != 10 or v23_links != v24_links:
        raise SuccessorError("V25_PREDECESSOR_GITLINK_DRIFT", f"v23={v23_links!r}; v24={v24_links!r}")
    return v24_links


def validate_frozen_evidence(root: Path, evaluated: str = PREDECESSOR_COMMIT) -> list[dict[str, Any]]:
    """Validate rc.1/approval and rc.2 exclusively at retained revisions."""

    require_ancestor(root, RC2_PUBLICATION_COMMIT, evaluated, "V25_RC2_PUBLICATION_NOT_ANCESTOR")
    require_ancestor(root, RC2_LOG_PROVENANCE_COMMIT, evaluated, "V25_RC2_LOG_PROVENANCE_NOT_ANCESTOR")
    if commit_parents(root, RC2_PUBLICATION_COMMIT) != (RC2_BASE_COMMIT,):
        raise SuccessorError("V25_RC2_PARENT_DRIFT", repr(commit_parents(root, RC2_PUBLICATION_COMMIT)))
    observed_scope = changed_paths(root, RC2_BASE_COMMIT, RC2_PUBLICATION_COMMIT)
    if observed_scope != tuple(sorted(RC2_SCOPE)):
        missing = sorted(set(RC2_SCOPE) - set(observed_scope))
        unexpected = sorted(set(observed_scope) - set(RC2_SCOPE))
        raise SuccessorError("V25_RC2_PUBLICATION_SCOPE_DRIFT", f"missing={missing!r}; unexpected={unexpected!r}")
    for path in RC2_SCOPE:
        mode, kind, _ = tree_entry(root, RC2_PUBLICATION_COMMIT, path, "V25_RC2_PUBLICATION_MODE_DRIFT")
        if mode != "100644" or kind != "blob":
            raise SuccessorError("V25_RC2_PUBLICATION_MODE_DRIFT", f"{path}: {mode} {kind}")

    artifacts: list[dict[str, Any]] = []
    for path, (revision, object_id, expected_sha, expected_bytes) in FROZEN_ARTIFACTS.items():
        mode, kind, observed_object = tree_entry(root, revision, path, "V25_RC2_HISTORICAL_ARTIFACT_MISSING")
        content = blob_bytes(root, revision, path, "V25_RC2_HISTORICAL_ARTIFACT_MISSING")
        if mode != "100644" or kind != "blob" or observed_object != object_id or sha256_bytes(content) != expected_sha or len(content) != expected_bytes:
            raise SuccessorError(
                "V25_RC2_HISTORICAL_ARTIFACT_MISMATCH",
                f"{revision}:{path}: mode={mode}; type={kind}; object={observed_object}; sha256={sha256_bytes(content)}; bytes={len(content)}",
            )
        evaluated_mode, evaluated_kind, evaluated_object = tree_entry(
            root,
            evaluated,
            path,
            "V25_FROZEN_ARTIFACT_MISSING",
        )
        if evaluated_mode != "100644" or evaluated_kind != "blob" or evaluated_object != object_id:
            raise SuccessorError(
                "V25_FROZEN_ARTIFACT_DESCENDANT_DRIFT",
                f"{evaluated}:{path}: mode={evaluated_mode}; type={evaluated_kind}; object={evaluated_object}; expected={object_id}",
            )
        artifacts.append(
            {
                "path": path,
                "revision": revision,
                "mode": "100644",
                "objectId": object_id,
                "sha256": expected_sha,
                "bytes": expected_bytes,
            }
        )

    manifest_bytes = blob_bytes(
        root,
        RC2_PUBLICATION_COMMIT,
        "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json",
        "V25_RC2_MANIFEST_UNAVAILABLE",
    )
    try:
        manifest = json.loads(manifest_bytes)
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        raise SuccessorError("V25_RC2_MANIFEST_INVALID", str(error)) from error
    overlay = manifest["sourceBinding"]["ownerReviewOverlay"]
    current = manifest["testDenominator"]["currentCandidate"]
    gates = manifest["gateStates"]
    observed_gates = {name: gates[name]["state"] for name in ("FR-20", "SM-C1", "SM-C2", "OQ-1", "implementationHold")}
    expected_gates = {"FR-20": "PENDING", "SM-C1": "PENDING", "SM-C2": "FAILED", "OQ-1": "BLOCKED", "implementationHold": "ACTIVE"}
    if (
        overlay["baseCommit"] != RC2_BASE_COMMIT
        or overlay["candidateDigest"] != RC2_CANDIDATE_DIGEST
        or len(current["testIds"]) != 473
        or len(manifest["obligations"]) != 969
        or len(manifest["categoryMappings"]) != 7
        or len(manifest["governedDispositions"]) != 277
        or observed_gates != expected_gates
    ):
        raise SuccessorError("V25_RC2_DENOMINATOR_OR_GATE_DRIFT", "rc.2 identities, counts, or gate states drifted")
    return artifacts


def checked_worktree_path(root: Path, relative_path: str) -> Path:
    """Resolve one regular-file input without following path aliases."""

    if Path(relative_path).is_absolute() or "\\" in relative_path:
        raise SuccessorError("V25_UNSAFE_INPUT_PATH", relative_path)
    segments = relative_path.split("/")
    if not segments or any(segment in ("", ".", "..") for segment in segments):
        raise SuccessorError("V25_UNSAFE_INPUT_PATH", relative_path)
    current = root
    for index, segment in enumerate(segments):
        current = current / segment
        try:
            metadata = current.lstat()
        except OSError as error:
            raise SuccessorError("V25_TOOLING_INPUT_UNAVAILABLE", f"{relative_path}: {error}") from error
        if stat.S_ISLNK(metadata.st_mode):
            raise SuccessorError("V25_TOOLING_INPUT_ALIAS", relative_path)
        if index < len(segments) - 1 and not stat.S_ISDIR(metadata.st_mode):
            raise SuccessorError("V25_TOOLING_INPUT_UNAVAILABLE", f"non-directory component: {relative_path}")
    metadata = current.lstat()
    if not stat.S_ISREG(metadata.st_mode) or metadata.st_nlink != 1:
        raise SuccessorError("V25_TOOLING_INPUT_ALIAS", relative_path)
    if metadata.st_mode & (stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH):
        raise SuccessorError("V25_TOOLING_MODE_DRIFT", f"{relative_path}: executable worktree mode")
    try:
        current.resolve().relative_to(root.resolve())
    except ValueError as error:
        raise SuccessorError("V25_UNSAFE_INPUT_PATH", relative_path) from error
    return current


def capture_inputs(root: Path, paths: Sequence[str] = V25_NON_RECORD_PATHS) -> tuple[InputSnapshot, ...]:
    """Capture exact identities for the four self-excluded V25 inputs."""

    if tuple(paths) != V25_NON_RECORD_PATHS or RECORD_PATH in paths:
        raise SuccessorError("V25_RECORD_SELF_INCLUSION", repr(tuple(paths)), "FAIL")
    snapshots: list[InputSnapshot] = []
    for relative_path in paths:
        path = checked_worktree_path(root, relative_path)
        metadata = path.stat()
        content = path.read_bytes()
        object_id = git_text(root, "hash-object", "--no-filters", "--", relative_path, code="V25_TOOLING_INPUT_UNAVAILABLE")
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

    if resolve_commit(root, "HEAD", "V25_GENERATION_BASELINE_UNAVAILABLE") != expected_head:
        raise SuccessorError("V25_GENERATION_BASELINE_DRIFT", expected_head)
    if capture_inputs(root) != expected:
        raise SuccessorError("V25_TOOLING_INPUT_DRIFT", "one or more V25 inputs changed during generation")


def snapshot_bindings(snapshots: Sequence[InputSnapshot]) -> list[dict[str, Any]]:
    """Project input snapshots into the closed binding contract."""

    return [
        {
            "path": row.path,
            "mode": "100644",
            "objectId": row.object_id,
            "sha256": row.sha256,
            "bytes": row.size,
        }
        for row in snapshots
    ]


def manifest_digest(bindings: Sequence[dict[str, Any]]) -> str:
    """Digest canonical self-excluded manifest rows."""

    material = "".join(
        f"{unicodedata.normalize('NFC', row['path'])}\t{row['mode']}\t{row['objectId']}\t{row['sha256']}\t{row['bytes']}\n"
        for row in bindings
    ).encode("utf-8")
    return sha256_bytes(material)


def predecessor_document(commit: str, tree: str, paths: tuple[str, str, str], digests: dict[str, str]) -> dict[str, str]:
    """Build one closed predecessor identity object."""

    record_path, schema_path, publisher_path = paths
    return {
        "commit": commit,
        "tree": tree,
        "recordPath": record_path,
        "recordSha256": digests[record_path],
        "schemaPath": schema_path,
        "schemaSha256": digests[schema_path],
        "publisherPath": publisher_path,
        "publisherSha256": digests[publisher_path],
    }


def assertion_ledger() -> list[dict[str, str]]:
    """Return the fixed, nonempty evaluated assertion inventory."""

    subjects = (
        ("immutable-v23-publication", "V23 retains its exact nine-path publication and pinned identities"),
        ("immutable-v24-publication", "V24 retains its direct-child exact eight-path correction and pinned identities"),
        ("rc2-historical-publication", "rc.2 retains its direct-child exact 36-path publication"),
        ("rc2-retained-log-provenance", "both retained logs match their later committed provenance"),
        ("rc1-and-approval", "rc.1 and its detached approval retain their pinned bytes"),
        ("preservation-denominators", "473 tests, 969 obligations, seven categories, and 277 dispositions remain exact"),
        ("successor-five-path-scope", "V25 declares exactly five mode-100644 paths"),
        ("self-excluded-manifest", "the four non-record V25 blobs are independently bound"),
        ("raw-root-gitlinks", "all ten root gitlinks equal the V23/V24 inventory"),
        ("active-non-executable-hold", "V25 grants no approval, execution, release, or push authority"),
    )
    return [
        {"id": f"V25.SUCCESSOR.{index:02d}", "subject": subject, "state": "PASS", "detail": detail}
        for index, (subject, detail) in enumerate(subjects, start=1)
    ]


def build_document(
    root: Path,
    bindings: list[dict[str, Any]],
    gitlinks: list[dict[str, str]],
    evaluated: str = PREDECESSOR_COMMIT,
) -> dict[str, Any]:
    """Build the deterministic V25 record after predecessor authentication."""

    artifacts = validate_frozen_evidence(root, evaluated)
    by_path = {row["path"]: row for row in artifacts}
    return {
        "schemaVersion": SCHEMA_VERSION,
        "recordType": "PRESERVATION_EVIDENCE_TOOLING_SUCCESSOR",
        "successorId": SUCCESSOR_ID,
        "lineage": {
            "predecessorCommit": PREDECESSOR_COMMIT,
            "predecessorTree": PREDECESSOR_TREE,
            "v23": predecessor_document(V23_COMMIT, V23_TREE, (V23_RECORD_PATH, V23_SCHEMA_PATH, V23_PUBLISHER_PATH), V23_IDENTITIES),
            "v24": predecessor_document(V24_COMMIT, V24_TREE, (V24_RECORD_PATH, V24_SCHEMA_PATH, V24_PUBLISHER_PATH), V24_IDENTITIES),
        },
        "preservationEvidence": {
            "rc1": by_path["docs/release-evidence/preservation-traceability-manifest-v3.json"],
            "rc1Approval": by_path["docs/release-evidence/preservation-traceability-manifest-v3-owner-approval.json"],
            "rc2PublicationCommit": RC2_PUBLICATION_COMMIT,
            "rc2BaseCommit": RC2_BASE_COMMIT,
            "rc2CandidateDigest": RC2_CANDIDATE_DIGEST,
            "rc2ExactChangedPaths": list(RC2_SCOPE),
            "rc2Artifacts": artifacts,
            "testIdentityCount": 473,
            "obligationIdentityCount": 969,
            "categoryCount": 7,
            "governedDispositionCount": 277,
            "gateStates": {"FR-20": "PENDING", "SM-C1": "PENDING", "SM-C2": "FAILED", "OQ-1": "BLOCKED", "implementationHold": "ACTIVE"},
        },
        "successorTransaction": {
            "baselineCommit": PREDECESSOR_COMMIT,
            "baselineTree": PREDECESSOR_TREE,
            "exactChangedPaths": list(V25_SCOPE),
            "requiredMode": "100644",
            "selfExcludedManifestSha256": manifest_digest(bindings),
            "manifest": bindings,
        },
        "rootGitlinks": gitlinks,
        "resultSemantics": {"states": ["PASS", "FAIL", "BLOCKED"], "exitCodes": {"PASS": 0, "FAIL": 1, "BLOCKED": 2}, "ledgerRequired": True},
        "assertionLedger": assertion_ledger(),
        "blockers": [],
        "result": "PASS",
        "implementationHold": "ACTIVE",
        "executionAllowed": False,
        "ownerApprovalClaimed": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }


def validate_schema(root: Path, document: dict[str, Any], schema_bytes: bytes) -> None:
    """Validate against the pinned closed Draft 2020-12 schema."""

    observed = sha256_bytes(schema_bytes)
    if observed != V25_SCHEMA_SHA256:
        raise SuccessorError("V25_SCHEMA_IDENTITY_MISMATCH", f"expected={V25_SCHEMA_SHA256}; observed={observed}")
    try:
        schema = json.loads(schema_bytes)
        import jsonschema  # Imported only after the candidate schema identity is pinned.

        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except (json.JSONDecodeError, jsonschema.SchemaError, jsonschema.ValidationError) as error:
        raise SuccessorError("V25_DOCUMENT_SCHEMA_INVALID", str(error), "FAIL") from error


def generate_document(root: Path) -> tuple[dict[str, Any], tuple[InputSnapshot, ...], str]:
    """Generate a prospective V25 document at the exact frozen predecessor."""

    head = resolve_commit(root, "HEAD", "V25_GENERATION_BASELINE_UNAVAILABLE")
    if head != PREDECESSOR_COMMIT or commit_tree(root, head) != PREDECESSOR_TREE:
        raise SuccessorError("V25_GENERATION_BASELINE_DRIFT", f"expected={PREDECESSOR_COMMIT}; observed={head}")
    gitlinks = authenticate_predecessors(root, head)
    if raw_root_gitlinks(root, head) != gitlinks:
        raise SuccessorError("V25_GENERATION_GITLINK_DRIFT", "predecessor gitlinks differ from immutable V24")
    snapshots = capture_inputs(root)
    if snapshots[0].sha256 != V25_SCHEMA_SHA256:
        raise SuccessorError("V25_SCHEMA_IDENTITY_MISMATCH", snapshots[0].sha256)
    document = build_document(root, snapshot_bindings(snapshots), gitlinks, head)
    validate_schema(root, document, (root / SCHEMA_PATH).read_bytes())
    return document, snapshots, head


def write_document(root: Path) -> dict[str, Any]:
    """Atomically write V25, quarantining only output created by this call on failure."""

    target = root / RECORD_PATH
    if target.exists() or target.is_symlink():
        raise SuccessorError("V25_RECORD_ALREADY_EXISTS", RECORD_PATH, "FAIL")
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
            raise SuccessorError("V25_PUBLICATION_FINAL_IDENTITY_DRIFT", RECORD_PATH)
        return document
    except BaseException:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
        if installed and target.exists() and target.read_bytes() == content:
            quarantine = target.with_name(f".{target.name}.quarantine.{uuid.uuid4().hex}")
            os.replace(target, quarantine)
        raise


def committed_binding(root: Path, revision: str, path: str) -> dict[str, Any]:
    """Create one binding from a committed V25 blob."""

    mode, kind, object_id = tree_entry(root, revision, path, "V25_TOOLING_INPUT_UNAVAILABLE")
    if mode != "100644" or kind != "blob":
        raise SuccessorError("V25_TOOLING_MODE_DRIFT", f"{path}: {mode} {kind}")
    content = run_git(root, "show", f"{revision}:{path}", code="V25_TOOLING_INPUT_UNAVAILABLE")
    return {"path": path, "mode": mode, "objectId": object_id, "sha256": sha256_bytes(content), "bytes": len(content)}


def path_exists(root: Path, revision: str, path: str) -> bool:
    """Return whether a path exists at a revision without conflating Git failure."""

    process = subprocess.run(
        ["git", "-C", str(root), "cat-file", "-e", f"{revision}:{path}"],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if process.returncode not in (0, 1, 128):
        raise SuccessorError("V25_HISTORY_UNAVAILABLE", process.stderr.decode("utf-8", "replace").strip())
    return process.returncode == 0


def locate_publication(root: Path, evaluated: str) -> str:
    """Find the unique sticky V25 record introduction across full ancestry."""

    history = git_text(root, "rev-list", "--reverse", evaluated, code="V25_HISTORY_UNAVAILABLE").splitlines()
    additions: list[str] = []
    for commit in history:
        if not path_exists(root, commit, RECORD_PATH):
            continue
        parents = commit_parents(root, commit)
        if not parents or all(not path_exists(root, parent, RECORD_PATH) for parent in parents):
            additions.append(commit)
    if not additions:
        raise SuccessorError("V25_PUBLICATION_MISSING", evaluated)
    if len(additions) != 1:
        raise SuccessorError("V25_DUPLICATE_PUBLICATION", repr(additions))
    return additions[0]


def verify_revision(root: Path, revision: str = "HEAD") -> dict[str, Any]:
    """Verify a committed V25 candidate and all sticky descendant invariants."""

    evaluated = resolve_commit(root, revision, "V25_EVALUATED_CANDIDATE_UNAVAILABLE")
    expected_links = authenticate_predecessors(root, evaluated)
    publication = locate_publication(root, evaluated)
    require_scope(root, publication, PREDECESSOR_COMMIT, V25_SCOPE, "V25")
    if not path_exists(root, evaluated, RECORD_PATH):
        raise SuccessorError("V25_RECORD_REVERTED_OR_DELETED", RECORD_PATH)

    for path in V25_SCOPE:
        publication_entry = tree_entry(root, publication, path, "V25_PUBLICATION_ARTIFACT_MISSING")
        evaluated_entry = tree_entry(root, evaluated, path, "V25_RECORD_REVERTED_OR_DELETED")
        if publication_entry != evaluated_entry:
            raise SuccessorError("V25_DESCENDANT_ARTIFACT_DRIFT", path)

    evaluated_links = raw_root_gitlinks(root, evaluated)
    publication_links = raw_root_gitlinks(root, publication)
    if evaluated_links != expected_links or publication_links != expected_links:
        raise SuccessorError("V25_ROOT_GITLINK_DRIFT", "V24, V25 publication, and evaluated gitlinks differ")

    schema_bytes = blob_bytes(root, publication, SCHEMA_PATH, "V25_SCHEMA_UNAVAILABLE")
    bindings = [committed_binding(root, publication, path) for path in V25_NON_RECORD_PATHS]
    expected = build_document(root, bindings, expected_links, evaluated)
    record_bytes = blob_bytes(root, publication, RECORD_PATH, "V25_RECORD_UNAVAILABLE")
    try:
        observed = json.loads(record_bytes)
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        raise SuccessorError("V25_RECORD_INVALID", str(error), "FAIL") from error
    validate_schema(root, observed, schema_bytes)
    if observed != expected or record_bytes != canonical_json(expected):
        raise SuccessorError("V25_RECORD_IDENTITY_MISMATCH", "record is not the deterministic authenticated projection")
    if not observed["assertionLedger"]:
        raise SuccessorError("V25_EMPTY_ASSERTION_LEDGER", "zero evaluated assertions cannot pass", "FAIL")
    return observed


def result_envelope(state: str, code: str | None = None, detail: str | None = None) -> dict[str, Any]:
    """Create the stable CLI result envelope."""

    ledger = assertion_ledger() if state == "PASS" else [
        {"id": "V25.SUCCESSOR.ERROR", "subject": "verification", "state": state, "detail": detail or code or state}
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
    """Parse the closed V25 CLI."""

    parser = argparse.ArgumentParser(description=__doc__)
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--write", action="store_true", help="generate the prospective V25 record at the frozen predecessor")
    action.add_argument("--verify", metavar="REVISION", help="verify a committed V25 candidate")
    parser.add_argument("--root", type=Path, default=Path.cwd(), help="repository working tree")
    return parser.parse_args(arguments)


def main(arguments: Sequence[str] | None = None) -> int:
    """Run generation or verification with PASS/FAIL/BLOCKED exit semantics."""

    options = parse_args(arguments)
    try:
        root = resolve_root(options.root)
        if options.write:
            document = write_document(root)
            print(f"V25_STORY_7_1_PRESERVATION_SUCCESSOR_WRITTEN path={RECORD_PATH} sha256={sha256_bytes(canonical_json(document))}")
        else:
            document = verify_revision(root, options.verify)
            print(f"V25_STORY_7_1_PRESERVATION_SUCCESSOR_OK revision={resolve_commit(root, options.verify)} assertions={len(document['assertionLedger'])}")
        print(json.dumps(result_envelope("PASS"), sort_keys=True))
        return 0
    except SuccessorError as error:
        fail(error)


if __name__ == "__main__":
    sys.exit(main())
