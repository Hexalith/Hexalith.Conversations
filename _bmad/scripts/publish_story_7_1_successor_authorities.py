#!/usr/bin/env python3
"""Publish and validate the V19/V20/V21 Story 7.1 successor authorities."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import errno
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import secrets
import shlex
import shutil
import stat
import subprocess
import sys
import tempfile
from typing import Any, Sequence
import unicodedata
import xml.etree.ElementTree as ET


RESULT_STATES = ("PASS", "FAIL", "BLOCKED", "not-applicable")
V19_SCHEMA_VERSION = "hexalith.conversations.story-7.1-checkpoint-completion-authority.v1"
V20_SCHEMA_VERSION = "hexalith.conversations.story-7.1-release-owner-authority.v1"
V21_SCHEMA_VERSION = "hexalith.conversations.story-7.1-authority-correction.v1"
INVENTORY_SCHEMA_VERSION = "hexalith.conversations.story-7.1-input-inventory.v1"
V19_AUTHORITY_ID = "V19-STORY-7.1-CHECKPOINT-COMPLETION"
V20_AUTHORITY_ID = "V20-STORY-7.1-RELEASE-OWNER-AUTHORITY"
V21_AUTHORITY_ID = "V21-STORY-7.1-AUTHORITY-CORRECTION"
V19_PATH = "_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json"
V20_PATH = "_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json"
V21_PATH = "_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json"
V19_SCHEMA_PATH = "_bmad/schemas/v19-story-7.1-checkpoint-completion-authority-v1.schema.json"
V20_SCHEMA_PATH = "_bmad/schemas/v20-story-7.1-release-owner-authority-v1.schema.json"
V21_SCHEMA_PATH = "_bmad/schemas/v21-story-7.1-authority-correction-v1.schema.json"
INVENTORY_PATH = "_bmad-output/planning-artifacts/v20-story-7.1-input-inventory-v1.json"
INVENTORY_SCHEMA_PATH = "_bmad/schemas/v20-story-7.1-input-inventory-v1.schema.json"
FAILURE_SCHEMA_PATH = "_bmad/schemas/story-record-generator-failure-v1.schema.json"
CORRECTION_PATH = "_bmad-output/planning-artifacts/v20-story-7.1-prepublication-correction-v1.md"
CORRECTION_SHA256 = "de18a6b0ca87ca4712bb338c474abdadda1f3578a021d9badc6b5ef0d7d53fac"
PUBLISHER_PATH = "_bmad/scripts/publish_story_7_1_successor_authorities.py"
PUBLISHER_TEST_PATH = "_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py"
PREFLIGHT_PATH = ".github/workflows/planning-authority-preflight.yml"
V21_SPEC_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-publish-v20-story-7-1-release-owner-authority.md"
)

APPROVED_BASELINE_COMMIT = "2ed96eff2adfd5190854165a01df657338def26f"
APPROVED_TOOLING_COMMIT = "4505ff975c7ae7aa877ee077b50729cdd8652151"
APPROVED_CHECKPOINT_COMMIT = "dbd2c5b11f16ddfce8b039e2c31eec38a2e99c8b"
APPROVED_V19_PUBLICATION = "3045060ce48acfed60b66bacd417de4dcfd1adb0"
APPROVED_ENTRY_COMMIT = "5fa5612dd348259ce8fc46865409f678110cec87"
APPROVED_V20_PUBLICATION = "6fa990459dd72d6752e12bbc806d85bf607fff85"
APPROVED_V19_RAW_SHA256 = "58b7bc8e70701718c20668fddddd492ab3da8fbae274a2ab507c004f649589ce"
APPROVED_V20_RAW_SHA256 = "7719deb5787bd59a2194dd9f2a138e74fedcadb2db4f4b575b0974f38c4faa09"

TRUSTED_OWNER_IDENTITY = "Jerome Piquot <jpiquot@itaneo.com>"
TRUSTED_SSH_PRINCIPAL = "jpiquot@itaneo.com"
TRUSTED_SSH_PUBLIC_KEY = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIL0Kt34ByT8WvAx325SbxYRNLKBZ3ggbgWomqD1nCHq4"
TRUSTED_SSH_FINGERPRINT = "SHA256:8XlNQvE3ucPf/e509wU4qtNgiyWA+TKmLei7F7+TCvk"
PREFLIGHT_MODEL_SHA256 = "813a156e0b6421ec77f6b2bf1488a47e212d200dcb9ab0ceecf74def5b8d6559"
CI_RULESET_SOURCE_REPOSITORY = "Hexalith/Hexalith.Conversations"
CI_RULESET_SOURCE_REF = "refs/heads/main"
CI_RULESET_SOURCE_WORKFLOW = PREFLIGHT_PATH
CI_RULESET_SOURCE_WORKFLOW_REF = (
    f"{CI_RULESET_SOURCE_REPOSITORY}/{CI_RULESET_SOURCE_WORKFLOW}@{CI_RULESET_SOURCE_REF}"
)
CI_RULESET_BLOCKER = "V21_CI_RULESET_ACTIVATION_UNVERIFIED"

V19_TOOLING_PATHS = (
    PREFLIGHT_PATH,
    V21_SPEC_PATH,
    V19_SCHEMA_PATH,
    PUBLISHER_PATH,
    PUBLISHER_TEST_PATH,
)
V21_TOOLING_PATHS = (
    PREFLIGHT_PATH,
    V21_SPEC_PATH,
    V21_SCHEMA_PATH,
    PUBLISHER_PATH,
    PUBLISHER_TEST_PATH,
)

PLANNING_CANDIDATE = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5"
V9_BUNDLE_PATH = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json"
V9_BUNDLE_RAW_SHA256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3"
V9_BUNDLE_DIGEST = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055"
V11_PATH = "_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json"
V11_RAW_SHA256 = "14e95c44149594b87e5337b45fd546fdd48d58407fa0b61f3d4b94cba59da82d"
V11_EPIC = "epic-6-authority-2026-08-04-v11"
V11_ARCHITECTURE = "conversations-architecture-2026-08-04-v11"
V18_PATH = "_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json"
V18_RAW_SHA256 = "24891d990b399fd9526f864c3e8be2b188db728a398f09d9129731d3169f1d94"
SEMANTIC_SOURCE_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-7-1-define-the-final-record-schema-and-deterministic-generator-core-2.md"
)
SEMANTIC_SOURCE_SHA256 = "eeee633e7045d9636d4babe62b6bca9744c8137b490da2304fcc9192f84fbaf5"
CHECKPOINT_RESULT_PATH = "artifacts/v9/schema-slice/v2-schema-contract.xml"
CHECKPOINT_COMMAND = (
    "python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py "
    "-k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml"
)
CHECKPOINT_COMMAND_ARGUMENTS = tuple(shlex.split(CHECKPOINT_COMMAND))
CHECKPOINT_SUBJECT_PREFIX = (
    "_bmad.scripts.tests.test_generate_story_record::test_v2_schema_contract_"
)
REQUIRED_CHECKPOINT_SUBJECTS = (
    f"{CHECKPOINT_SUBJECT_PREFIX}hold_drift_is_blocked",
    f"{CHECKPOINT_SUBJECT_PREFIX}hold_metaschema_and_identities",
    f"{CHECKPOINT_SUBJECT_PREFIX}authority_boundary_annotations",
    f"{CHECKPOINT_SUBJECT_PREFIX}valid_in_memory_instances",
    f"{CHECKPOINT_SUBJECT_PREFIX}rejects_missing_and_extra_fields",
    f"{CHECKPOINT_SUBJECT_PREFIX}rejects_invalid_bindings",
    f"{CHECKPOINT_SUBJECT_PREFIX}restores_permissive_and_inconsistent_fixtures",
)

V21_ASSERTION_SUBJECTS = (
    "accepted-baseline-through-v20-lineage",
    "approved-checkpoint-paths-bytes-and-seven-subject-ledger",
    "source-pinned-v20-signature-status-principal-fingerprint-author",
    "historical-revision-aware-inventory",
    "v20-direct-child-hardened-tooling-boundary",
    "protected-default-branch-ruleset-source-workflow-contract",
    "narrow-story-7.1-hold-lift-only",
)

CHECKPOINT_PATHS = (
    "_bmad/schemas/v9-acceptance-result-v1.schema.json",
    "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/scripts/tests/test_generate_story_record.py",
    CHECKPOINT_RESULT_PATH,
)
CHECKPOINT_PATH_DIGEST = "5137dfa15a6c28253898b7edac5969684abddc2c77b35c2f3299b07faa72cf9d"
APPROVED_CHECKPOINT_BINDINGS = (
    (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "47f544d74688d07d8dd8631c187a3e9bfb641aac043811bf87275b97921979da",
    ),
    (
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "e946373ab67301f8385a6700b8af7f692553c2438fc3791bb89d1147e6fe8efd",
    ),
    (
        "_bmad/schemas/story-final-record-v2.schema.json",
        "4f6e04230fff8ae04296414e6ea355676d3d2bab3db3d1cbf0ea4566e8f5f6e4",
    ),
    (
        "_bmad/scripts/tests/test_generate_story_record.py",
        "8b60808493f9770a6068f2af57dd210392af4c096063a4549c37b39845c5fdbd",
    ),
    (
        CHECKPOINT_RESULT_PATH,
        "28fbc41e2f6bc309c11b3fb2cd4ec74c3e52e2ea35918500c5309b9742ce0692",
    ),
)
ROOT_GITLINK_PATHS = (
    "references/Hexalith.AI.Tools",
    "references/Hexalith.Builds",
    "references/Hexalith.Commons",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)
ROOT_GITLINK_PATH_DIGEST = "d1e434be15361351b1aa12b0e22619060761bd2d4b6e1a56f4bcf1f6e8978c20"

HISTORICAL_COMMIT = "b819a7c43a7024295abaabd418a74f5f64cb5af0"
HISTORICAL_PARENT = "73bcee6f04479d4743d5a65ce929728e22687d7d"
HISTORICAL_TREE = "89baa263615d1e510d15410be7f47b09bff30aca"
HISTORICAL_PATHS = (
    "_bmad-output/implementation-artifacts/deferred-work.md",
    "_bmad-output/implementation-artifacts/epic-7-context.md",
    "_bmad-output/implementation-artifacts/spec-7-1-schemas.md",
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/schemas/v9-acceptance-result-v1.schema.json",
    "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)
HISTORICAL_PATH_DIGEST = "be68d84d11534579092215b1f8a758cefb4edfd56644b046cb6687468e5a8212"
HISTORICAL_MISSING_PATHS = (CHECKPOINT_RESULT_PATH,)
HISTORICAL_MISSING_DIGEST = "f0fbd24ece1085ca146f67193721007ab06309d2d025ee1c1bcde0e5046d279e"
HISTORICAL_UNEXPECTED_PATHS = (
    "_bmad-output/implementation-artifacts/deferred-work.md",
    "_bmad-output/implementation-artifacts/epic-7-context.md",
    "_bmad-output/implementation-artifacts/spec-7-1-schemas.md",
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Folders",
    "references/Hexalith.FrontComposer",
    "references/Hexalith.Memories",
    "references/Hexalith.Parties",
    "references/Hexalith.Projects",
    "references/Hexalith.Tenants",
)
HISTORICAL_UNEXPECTED_DIGEST = "be3fb907bd3288d0b5d7df553aafa24be141e6b3f7c5012c2dad36e5eaf4b4db"

PRESERVED_EVIDENCE = (
    {
        "kind": "authority",
        "identity": "V17-IMPLEMENTATION-HOLD-DECISION",
        "path": "_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json",
        "sha256": "1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50",
        "interpretation": "valid-point-in-time-authority-lifted-only-7.1-SCHEMAS",
    },
    {
        "kind": "authority",
        "identity": "V18-PACKAGE-ENVIRONMENT-AUTHORITY",
        "path": V18_PATH,
        "sha256": V18_RAW_SHA256,
        "interpretation": "valid-point-in-time-authority-full-story-hold-active-and-unresolved",
    },
    {
        "kind": "hold-record",
        "identity": "implementation-hold-v1",
        "path": "_bmad-output/planning-artifacts/implementation-hold-v1.json",
        "sha256": "2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12",
        "interpretation": "valid-scoped-decision-unlocks-only-7.1-SCHEMAS-and-is-not-global",
    },
    {
        "kind": "git-transaction",
        "identity": HISTORICAL_COMMIT,
        "parent": HISTORICAL_PARENT,
        "tree": HISTORICAL_TREE,
        "interpretation": "immutable-mixed-transaction-with-additive-nonconformance-finding",
    },
)

IMPLEMENTATION_PATHS = (
    "_bmad/schemas/story-final-record-v2.schema.json",
    "_bmad/schemas/story-record-generator-failure-v1.schema.json",
    "_bmad/scripts/generate_story_record.py",
    "_bmad/scripts/tests/test_generate_story_record.py",
    "docs/runbooks/story-final-record-generation.md",
)
RESULT_PATHS = tuple(f"artifacts/v9/7.1/AC-7.1-0{index}.xml" for index in range(1, 6))
RECORD_OUTPUT_PATHS = (
    "docs/release-evidence/story-7.1-final-record-v2.json",
    "docs/release-evidence/story-7.1-final-record-v2.md",
)
STORY_DESCENDANT_PATHS = IMPLEMENTATION_PATHS + RESULT_PATHS + RECORD_OUTPUT_PATHS
AUTHORITY_PREFIX = (
    V9_BUNDLE_PATH,
    "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
    V11_PATH,
    V19_PATH,
    V20_PATH,
    INVENTORY_PATH,
    "_bmad/schemas/v9-story-contract-v1.schema.json",
    INVENTORY_SCHEMA_PATH,
)
SCENARIO_PATHS = {
    "AC-7.1-01": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-02": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-03": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-04": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-05": AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/schemas/story-record-generator-failure-v1.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
    ),
    "AC-7.1-06": (".gitmodules",)
    + AUTHORITY_PREFIX
    + (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "_bmad/schemas/story-final-record-v2.schema.json",
        "_bmad/schemas/story-record-generator-failure-v1.schema.json",
        "_bmad/scripts/generate_story_record.py",
        "_bmad/scripts/tests/test_generate_story_record.py",
        *RESULT_PATHS,
    ),
}
SCENARIO_IDENTITIES = {
    "AC-7.1-01": "V20-7.1-AC01-INPUT-PATHS-v1",
    "AC-7.1-02": "V20-7.1-AC02-INPUT-PATHS-v1",
    "AC-7.1-03": "V20-7.1-AC03-INPUT-PATHS-v1",
    "AC-7.1-04": "V20-7.1-AC04-INPUT-PATHS-v1",
    "AC-7.1-05": "V20-7.1-AC05-INPUT-PATHS-v1",
    "AC-7.1-06": "V20-7.1-AC06-AGGREGATE-INPUT-PATHS-v1",
}
SCENARIO_DIGESTS = {
    "AC-7.1-01": "78a2fc7f6a53273a4c85ecc2a476cfc88c7896a86f5a6a8fd69eacc44c2540c7",
    "AC-7.1-02": "4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908",
    "AC-7.1-03": "4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908",
    "AC-7.1-04": "4eb9da818aff7ded9b1f40234ce1702aa465a01edaf28075f95e399f120ef908",
    "AC-7.1-05": "2ba87bf3475e3dc799fafbefb30a132c7950057abe98eb887a107f59f6016e26",
    "AC-7.1-06": "ab3e8bb068cf5fc8b1ade9225f004d6b8dcbbccc15eb59657b1af4bfcfbfb91c",
}
CURRENT_INPUTS = (
    (V9_BUNDLE_PATH, "V9 authority index", V9_BUNDLE_RAW_SHA256),
    (
        "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json",
        "Six-scenario Story 7.1 contract",
        "548294d8e9752ff3354897efbfc30a1920bf8cea6a3187ac719c0ca9df618d2e",
    ),
    (V11_PATH, "Exact checkpoint scope", V11_RAW_SHA256),
    (
        "_bmad/schemas/v9-story-contract-v1.schema.json",
        "Story-contract schema",
        "33f0b5dc21f56811b8b4307e52f900f2431e31b5ec0301c314c23f47464dabb0",
    ),
    (
        "_bmad/schemas/v9-acceptance-result-v1.schema.json",
        "Acceptance-result schema, pre-V19 observation",
        "3a0f417edaf9979d3d3ce3f7f06095c89a78cd42b1df20b1529e10f339c67cf9",
    ),
    (
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json",
        "Frozen-inventory schema, pre-V19 observation",
        "3741bc4455d34fb0a0b575321eb3d5e909320bb5e17f185ce5cac15d0f992970",
    ),
    (
        "_bmad/schemas/story-final-record-v2.schema.json",
        "Final-record schema, pre-V19 observation",
        "212bec3facdd2a68ace486be2919f8defdb01c4720afdb8b2fa77472fd45e5ec",
    ),
    (
        "_bmad/scripts/generate_story_record.py",
        "Legacy v1 generator baseline",
        "150a293948ef760173fb7cef7b386e6bd81bfcfdeb9862fa761e0d2286addd8c",
    ),
    (
        "_bmad/scripts/tests/test_generate_story_record.py",
        "Generator/schema tests, pre-V19 observation",
        "ef9c5c9d13d0d70f01b02f2f1093be7a11de5201cf4f1cd662cc809113f2155b",
    ),
    (
        "docs/runbooks/story-final-record-generation.md",
        "Generator runbook baseline",
        "3068c63dcc3f8cf517634c10dfba7eaf4dae7cc469a7143994bda448a2c213c1",
    ),
    (CORRECTION_PATH, "Failed prepublication candidate correction", CORRECTION_SHA256),
    (SEMANTIC_SOURCE_PATH, "Approved semantic source", SEMANTIC_SOURCE_SHA256),
)


class SuccessorAuthorityError(RuntimeError):
    """A stable fail-closed successor-authority result."""

    def __init__(self, code: str, detail: str, state: str = "FAIL") -> None:
        if state not in RESULT_STATES:
            raise ValueError(f"unknown result state: {state}")
        super().__init__(f"{code}: {detail}")
        self.code = code
        self.detail = detail
        self.state = state


def sha256(content: bytes) -> str:
    """Return the lowercase SHA-256 digest of exact bytes."""

    return hashlib.sha256(content).hexdigest()


def json_bytes(value: Any) -> bytes:
    """Serialize canonical repository JSON bytes."""

    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def inventory_digest(paths: Sequence[str]) -> str:
    """Hash an ordered NFC UTF-8 path list with one terminal LF."""

    payload = unicodedata.normalize("NFC", "\n".join(paths) + "\n").encode("utf-8")
    return sha256(payload)


def safe_path(value: str) -> str:
    """Require one normalized repository-relative POSIX path."""

    path = PurePosixPath(value)
    if (
        not value
        or value in (".", "..")
        or "\\" in value
        or path.is_absolute()
        or path.as_posix() != value
        or any(part in ("", ".", "..") for part in path.parts)
        or any(ord(character) < 0x20 for character in value)
    ):
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", repr(value), "BLOCKED")
    return value


def worktree_path(root: Path, relative_path: str) -> Path:
    """Resolve one exact nonsymlink worktree path contained by the repository root."""

    repository = resolve_repository_root(root)
    normalized = safe_path(relative_path)
    target = repository / normalized
    try:
        current = repository
        for part in PurePosixPath(normalized).parts:
            current /= part
            try:
                metadata = current.lstat()
            except FileNotFoundError:
                break
            if stat.S_ISLNK(metadata.st_mode):
                raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", relative_path, "BLOCKED")
        resolved = target.resolve(strict=False)
    except SuccessorAuthorityError:
        raise
    except (OSError, RuntimeError) as error:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", str(error), "BLOCKED") from error
    try:
        resolved.relative_to(repository)
    except ValueError as error:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", relative_path, "BLOCKED") from error
    if resolved != target:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", relative_path, "BLOCKED")
    return target


def resolve_repository_root(root: Path) -> Path:
    """Resolve one existing repository directory with a stable blocker."""

    try:
        repository = root.resolve(strict=True)
        if not repository.is_dir():
            raise OSError(f"repository root is not a directory: {repository}")
    except (OSError, RuntimeError) as error:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", str(error), "BLOCKED") from error
    return repository


def open_repository_root(root: Path) -> tuple[Path, int]:
    """Open the physical repository by walking from the filesystem root without following links."""

    repository = resolve_repository_root(root)
    flags = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW
    current = -1
    child = -1
    descriptor_cleanup_failed = False
    try:
        expected = os.stat(repository, follow_symlinks=False)
        if not stat.S_ISDIR(expected.st_mode):
            raise OSError(f"repository root is not a directory: {repository}")
        current = os.open(os.sep, flags)
        for part in repository.parts[1:]:
            child = os.open(part, flags, dir_fd=current)
            try:
                os.close(current)
            except OSError:
                descriptor_cleanup_failed = True
                raise
            current = child
            child = -1
        observed = os.fstat(current)
        if (observed.st_dev, observed.st_ino) != (expected.st_dev, expected.st_ino):
            raise OSError("resolved repository root identity changed during descriptor acquisition")
    except (OSError, RuntimeError) as error:
        for descriptor in (child, current):
            if descriptor >= 0:
                try:
                    os.close(descriptor)
                except OSError:
                    pass
        if descriptor_cleanup_failed:
            raise
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ESCAPE", str(error), "BLOCKED") from error
    return repository, current


def run_git(
    root: Path,
    *arguments: str,
    allowed: tuple[int, ...] = (0,),
) -> subprocess.CompletedProcess[bytes]:
    """Run bounded non-interactive Git and preserve unavailable history as BLOCKED."""

    environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    environment.update(
        {
            "GIT_CONFIG_NOSYSTEM": "1",
            "GIT_CONFIG_SYSTEM": os.devnull,
            "GIT_CONFIG_GLOBAL": os.devnull,
            "GIT_NO_REPLACE_OBJECTS": "1",
            "GIT_TERMINAL_PROMPT": "0",
            "GCM_INTERACTIVE": "Never",
        }
    )
    try:
        result = subprocess.run(
            ("git", "--no-replace-objects", "-C", str(root), *arguments),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=30,
            env=environment,
        )
    except (OSError, subprocess.TimeoutExpired) as error:
        raise SuccessorAuthorityError("SUCCESSOR_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    if result.returncode not in allowed:
        detail = result.stderr.decode("utf-8", errors="replace").strip() or "Git command failed"
        raise SuccessorAuthorityError("SUCCESSOR_HISTORY_UNAVAILABLE", detail, "BLOCKED")
    return result


def resolve_commit(root: Path, revision: str, code: str) -> str:
    """Resolve one exact commit object."""

    try:
        value = run_git(root, "rev-parse", "--verify", f"{revision}^{{commit}}").stdout.decode("ascii").strip()
    except (SuccessorAuthorityError, UnicodeError) as error:
        detail = error.detail if isinstance(error, SuccessorAuthorityError) else str(error)
        raise SuccessorAuthorityError(code, detail, "BLOCKED") from error
    if re.fullmatch(r"[0-9a-f]{40}", value) is None:
        raise SuccessorAuthorityError(code, value, "BLOCKED")
    return value


def commit_parents(root: Path, commit: str, code: str) -> tuple[str, ...]:
    """Return all parents from the raw commit record."""

    try:
        record = run_git(root, "rev-list", "--parents", "-n", "1", commit).stdout.decode("ascii").split()
    except UnicodeError as error:
        raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error
    if not record or record[0] != commit or any(re.fullmatch(r"[0-9a-f]{40}", row) is None for row in record):
        raise SuccessorAuthorityError(code, repr(record), "BLOCKED")
    return tuple(record[1:])


def require_single_parent(root: Path, commit: str, expected: str, code: str) -> None:
    """Require exactly one parent equal to the expected commit."""

    parents = commit_parents(root, commit, code)
    if parents != (expected,):
        raise SuccessorAuthorityError(code, f"expected=({expected!r},) observed={parents!r}", "BLOCKED")


def require_ancestor(root: Path, ancestor: str, descendant: str, code: str) -> None:
    """Require an immutable ancestor relation."""

    result = run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1))
    if result.returncode != 0:
        raise SuccessorAuthorityError(code, f"{ancestor} is not an ancestor of {descendant}", "BLOCKED")


def is_ancestor(root: Path, ancestor: str, descendant: str) -> bool:
    """Return whether one commit is an ancestor without treating absence as an error."""

    return run_git(root, "merge-base", "--is-ancestor", ancestor, descendant, allowed=(0, 1)).returncode == 0


def require_complete_history(root: Path) -> None:
    """Reject shallow history before drawing lifecycle or publication conclusions."""

    try:
        state = run_git(root, "rev-parse", "--is-shallow-repository").stdout.decode("ascii").strip()
    except UnicodeError as error:
        raise SuccessorAuthorityError("SUCCESSOR_HISTORY_INCOMPLETE", str(error), "BLOCKED") from error
    if state != "false":
        raise SuccessorAuthorityError(
            "SUCCESSOR_HISTORY_INCOMPLETE",
            f"expected complete history; observed is-shallow-repository={state!r}",
            "BLOCKED",
        )


def candidate_blob(root: Path, candidate: str, relative_path: str, code: str) -> bytes:
    """Read one exact committed blob."""

    safe_path(relative_path)
    result = run_git(root, "show", f"{candidate}:{relative_path}", allowed=(0, 128))
    if result.returncode != 0:
        raise SuccessorAuthorityError(code, f"{relative_path}@{candidate}", "BLOCKED")
    return result.stdout


def raw_tree_record(root: Path, candidate: str, relative_path: str, code: str) -> tuple[str, str, str]:
    """Read one raw Git tree mode, type, and object identity."""

    try:
        output = run_git(root, "ls-tree", candidate, "--", safe_path(relative_path)).stdout.decode("utf-8").rstrip("\n")
    except UnicodeError as error:
        raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error
    match = re.fullmatch(r"([0-7]{6}) (blob|commit) ([0-9a-f]{40})\t(.+)", output)
    if match is None or match.group(4) != relative_path:
        raise SuccessorAuthorityError(code, f"{relative_path}@{candidate}", "BLOCKED")
    return match.group(1), match.group(2), match.group(3)


def changed_paths(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Return the ordered raw committed path list with rename detection disabled."""

    content = run_git(root, "diff", "--name-only", "--no-renames", "-z", baseline, candidate, "--").stdout
    try:
        return tuple(safe_path(item.decode("utf-8", errors="strict")) for item in content.split(b"\0") if item)
    except UnicodeError as error:
        raise SuccessorAuthorityError("SUCCESSOR_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error


def changed_gitlinks(root: Path, baseline: str, candidate: str) -> tuple[str, ...]:
    """Derive changed gitlinks only from raw mode-160000 diff records."""

    content = run_git(root, "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--").stdout
    records = [record for record in content.split(b"\0") if record]
    paths: list[str] = []
    for index in range(0, len(records), 2):
        if index + 1 >= len(records):
            raise SuccessorAuthorityError("SUCCESSOR_GITLINK_DIFF_MALFORMED", "incomplete raw record", "BLOCKED")
        try:
            metadata = records[index].decode("ascii", errors="strict").split()
            path = safe_path(records[index + 1].decode("utf-8", errors="strict"))
        except UnicodeError as error:
            raise SuccessorAuthorityError("SUCCESSOR_PATH_ENCODING_INVALID", str(error), "BLOCKED") from error
        if len(metadata) >= 2 and (metadata[0] == ":160000" or metadata[1] == "160000"):
            paths.append(path)
    return tuple(sorted(set(paths)))


def descendant_history_changes(
    root: Path,
    publication: str,
    evaluated: str,
) -> tuple[tuple[tuple[str, str], ...], tuple[tuple[str, str], ...]]:
    """Return every path and gitlink touched by each post-publication commit."""

    require_ancestor(root, publication, evaluated, "V21_PUBLICATION_NOT_ANCESTOR")
    publication_parents = commit_parents(root, publication, "V21_DESCENDANT_HISTORY_INVALID")
    if len(publication_parents) != 1:
        raise SuccessorAuthorityError(
            "V21_DESCENDANT_HISTORY_INVALID",
            f"V21 publication parents={publication_parents!r}",
            "BLOCKED",
        )
    hardened_tooling = publication_parents[0]
    try:
        output = run_git(
            root,
            "rev-list",
            "--topo-order",
            "--reverse",
            evaluated,
            "--not",
            publication,
        ).stdout.decode("ascii", errors="strict")
    except UnicodeError as error:
        raise SuccessorAuthorityError("V21_DESCENDANT_HISTORY_INVALID", str(error), "BLOCKED") from error
    commits = tuple(line for line in output.splitlines() if line)
    if any(re.fullmatch(r"[0-9a-f]{40}", commit) is None for commit in commits):
        raise SuccessorAuthorityError("V21_DESCENDANT_HISTORY_INVALID", repr(commits), "BLOCKED")
    path_touches: list[tuple[str, str]] = []
    gitlink_touches: list[tuple[str, str]] = []
    for commit in commits:
        parents = commit_parents(root, commit, "V21_DESCENDANT_HISTORY_INVALID")
        if not parents:
            raise SuccessorAuthorityError("V21_DESCENDANT_HISTORY_INVALID", commit, "BLOCKED")
        parent_path_changes = [set(changed_paths(root, parent, commit)) for parent in parents]
        parent_gitlink_changes = [set(changed_gitlinks(root, parent, commit)) for parent in parents]
        if len(parents) == 1:
            commit_paths = parent_path_changes[0]
            commit_gitlinks = parent_gitlink_changes[0]
        elif is_ancestor(root, publication, commit):
            # A post-publication merge may inherit stale protected bytes from any
            # parent. Compare its complete result with the V21 publication tree.
            commit_paths = set(changed_paths(root, publication, commit))
            commit_gitlinks = set(changed_gitlinks(root, publication, commit))
        elif is_ancestor(root, hardened_tooling, commit):
            # A pre-V21 merge may inherit the exact approved tooling through any
            # parent. Compare the result with that tooling tree so the tooling
            # paths themselves are not falsely classified as descendant drift.
            commit_paths = set(changed_paths(root, hardened_tooling, commit))
            commit_gitlinks = set(changed_gitlinks(root, hardened_tooling, commit))
        else:
            # An earlier pre-tooling merge cannot contain either publication
            # boundary. Compare it with its first parent while the rev-list
            # traversal independently inspects every introduced parent line.
            commit_paths = parent_path_changes[0]
            commit_gitlinks = parent_gitlink_changes[0]
        path_touches.extend((commit, path) for path in sorted(commit_paths))
        gitlink_touches.extend((commit, path) for path in sorted(commit_gitlinks))
    return tuple(path_touches), tuple(gitlink_touches)


def require_exact_transaction(
    root: Path,
    *,
    source: str,
    candidate: str,
    paths: tuple[str, ...],
    prefix: str,
) -> None:
    """Require one direct-child transaction with an exact path set and no gitlink change."""

    require_single_parent(root, candidate, source, f"{prefix}_PARENT_MISMATCH")
    observed_paths = changed_paths(root, source, candidate)
    if len(observed_paths) != len(set(observed_paths)) or set(observed_paths) != set(paths):
        missing = [path for path in paths if path not in observed_paths]
        unexpected = [path for path in observed_paths if path not in paths]
        raise SuccessorAuthorityError(
            f"{prefix}_PATH_SET_MISMATCH",
            f"missing={missing!r} unexpected={unexpected!r} order={observed_paths!r}",
        )
    observed_gitlinks = changed_gitlinks(root, source, candidate)
    if observed_gitlinks:
        raise SuccessorAuthorityError(f"{prefix}_GITLINK_DRIFT", repr(observed_gitlinks))


def validate_accepted_checkpoint(root: Path, candidate: str) -> None:
    """Pin the approved baseline, tooling transaction, and substantive checkpoint bytes."""

    if candidate != APPROVED_CHECKPOINT_COMMIT:
        raise SuccessorAuthorityError(
            "V19_CHECKPOINT_CANDIDATE_MISMATCH",
            f"expected={APPROVED_CHECKPOINT_COMMIT} observed={candidate}",
        )
    resolve_commit(root, APPROVED_BASELINE_COMMIT, "V19_APPROVED_BASELINE_UNAVAILABLE")
    resolve_commit(root, APPROVED_TOOLING_COMMIT, "V19_APPROVED_TOOLING_UNAVAILABLE")
    require_exact_transaction(
        root,
        source=APPROVED_BASELINE_COMMIT,
        candidate=APPROVED_TOOLING_COMMIT,
        paths=V19_TOOLING_PATHS,
        prefix="V19_APPROVED_TOOLING",
    )
    require_exact_transaction(
        root,
        source=APPROVED_TOOLING_COMMIT,
        candidate=candidate,
        paths=CHECKPOINT_PATHS,
        prefix="V19_APPROVED_CHECKPOINT",
    )
    for path, expected_digest in APPROVED_CHECKPOINT_BINDINGS:
        mode, object_type, _ = raw_tree_record(root, candidate, path, "V19_APPROVED_CHECKPOINT_PATH_MISSING")
        content = candidate_blob(root, candidate, path, "V19_APPROVED_CHECKPOINT_PATH_MISSING")
        if mode != "100644" or object_type != "blob" or sha256(content) != expected_digest:
            raise SuccessorAuthorityError(
                "V19_APPROVED_CHECKPOINT_BINDING_DRIFT",
                f"{path}: mode={mode} type={object_type} sha256={sha256(content)}",
            )


def root_gitlinks(root: Path, candidate: str) -> list[dict[str, str]]:
    """Validate `.gitmodules` and derive the exact ten root mode-160000 entries."""

    mode, object_type, _ = raw_tree_record(root, candidate, ".gitmodules", "SUCCESSOR_GITMODULES_UNAVAILABLE")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError("SUCCESSOR_GITMODULES_MODE_DRIFT", f"{mode} {object_type}")
    try:
        modules = candidate_blob(root, candidate, ".gitmodules", "SUCCESSOR_GITMODULES_UNAVAILABLE").decode(
            "utf-8", errors="strict"
        )
        tree = run_git(root, "ls-tree", "-rz", candidate).stdout.decode("utf-8", errors="strict")
    except UnicodeError as error:
        raise SuccessorAuthorityError("SUCCESSOR_GITLINK_INVENTORY_MALFORMED", str(error), "BLOCKED") from error
    declared = tuple(sorted(re.findall(r"(?m)^\s*path\s*=\s*(\S+)\s*$", modules)))
    if declared != ROOT_GITLINK_PATHS or len(declared) != len(set(declared)):
        raise SuccessorAuthorityError("SUCCESSOR_GITMODULES_DRIFT", repr(declared))
    rows: list[dict[str, str]] = []
    for record in tree.split("\0"):
        match = re.fullmatch(r"160000 commit ([0-9a-f]{40})\t(.+)", record)
        if match:
            rows.append({"path": safe_path(match.group(2)), "commit": match.group(1), "mode": "160000"})
    rows.sort(key=lambda row: row["path"])
    if tuple(row["path"] for row in rows) != ROOT_GITLINK_PATHS:
        raise SuccessorAuthorityError("SUCCESSOR_GITLINK_INVENTORY_DRIFT", repr(rows))
    if inventory_digest(ROOT_GITLINK_PATHS) != ROOT_GITLINK_PATH_DIGEST:
        raise SuccessorAuthorityError("SUCCESSOR_GITLINK_DIGEST_DRIFT", ROOT_GITLINK_PATH_DIGEST)
    return rows


def parse_json(content: bytes, code: str) -> dict[str, Any]:
    """Parse one UTF-8 JSON object with a stable code."""

    def reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
        result: dict[str, Any] = {}
        for key, value in pairs:
            if key in result:
                raise SuccessorAuthorityError(code, f"duplicate JSON key {key!r}")
            result[key] = value
        return result

    try:
        document = json.loads(
            content.decode("utf-8", errors="strict"),
            object_pairs_hook=reject_duplicate_keys,
        )
    except SuccessorAuthorityError:
        raise
    except (UnicodeError, json.JSONDecodeError, RecursionError) as error:
        raise SuccessorAuthorityError(code, str(error)) from error
    if not isinstance(document, dict):
        raise SuccessorAuthorityError(code, "document must be an object")
    return document


def validate_json_schema(schema_bytes: bytes, document: dict[str, Any], code: str) -> None:
    """Validate one document against one Draft 2020-12 schema."""

    try:
        import jsonschema
    except ImportError as error:
        raise SuccessorAuthorityError("SUCCESSOR_SCHEMA_UNAVAILABLE", str(error), "BLOCKED") from error
    try:
        schema = parse_json(schema_bytes, code)
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except SuccessorAuthorityError:
        raise
    except Exception as error:
        raise SuccessorAuthorityError(code, str(error)) from error


def validate_v9_bundle(content: bytes) -> None:
    """Recompute the V9 raw and self-excluding internal bundle digests."""

    if sha256(content) != V9_BUNDLE_RAW_SHA256:
        raise SuccessorAuthorityError("V19_V9_BUNDLE_RAW_DRIFT", sha256(content))
    bundle = parse_json(content, "V19_V9_BUNDLE_INVALID")
    if bundle.get("planningCandidate") != PLANNING_CANDIDATE:
        raise SuccessorAuthorityError("V19_PLANNING_CANDIDATE_DRIFT", repr(bundle.get("planningCandidate")))
    rows = bundle.get("artifacts")
    if not isinstance(rows, list) or not rows:
        raise SuccessorAuthorityError("V19_V9_BUNDLE_INVALID", "artifact rows missing")
    identities: list[str] = []
    digest_rows: list[str] = []
    for row in rows:
        if not isinstance(row, dict):
            raise SuccessorAuthorityError("V19_V9_BUNDLE_INVALID", "artifact row must be an object")
        path = row.get("path")
        digest = row.get("sha256")
        if not isinstance(path, str) or not isinstance(digest, str) or re.fullmatch(r"[0-9a-f]{64}", digest) is None:
            raise SuccessorAuthorityError("V19_V9_BUNDLE_INVALID", repr(row))
        identities.append(safe_path(path))
        digest_rows.append(f"{digest}  {path}\n")
    if len(identities) != len(set(identities)) or identities != sorted(identities):
        raise SuccessorAuthorityError("V19_V9_BUNDLE_INVENTORY_DRIFT", repr(identities))
    observed = sha256("".join(digest_rows).encode("utf-8"))
    if bundle.get("bundleDigest") != observed or observed != V9_BUNDLE_DIGEST:
        raise SuccessorAuthorityError("V19_V9_BUNDLE_DIGEST_DRIFT", observed)


def validate_v11(content: bytes) -> dict[str, Any]:
    """Validate the immutable V11 checkpoint scope and command."""

    if sha256(content) != V11_RAW_SHA256:
        raise SuccessorAuthorityError("V19_V11_RAW_DRIFT", sha256(content))
    document = parse_json(content, "V19_V11_INVALID")
    authority = document.get("authority")
    acceptance = document.get("acceptance")
    if (
        document.get("writablePaths") != list(CHECKPOINT_PATHS)
        or not isinstance(authority, dict)
        or authority.get("epic") != V11_EPIC
        or authority.get("architecture") != V11_ARCHITECTURE
        or authority.get("planningCandidate") != PLANNING_CANDIDATE
        or not isinstance(acceptance, dict)
        or acceptance.get("command") != CHECKPOINT_COMMAND
        or acceptance.get("result") != "PASS"
    ):
        raise SuccessorAuthorityError("V19_V11_SCOPE_DRIFT", "checkpoint scope, authority, or command")
    return document


def validate_preserved_evidence(root: Path, candidate: str) -> None:
    """Recompute every immutable V17/V18/hold digest at the fresh candidate."""

    for row in PRESERVED_EVIDENCE[:3]:
        path = str(row["path"])
        mode, object_type, _ = raw_tree_record(root, candidate, path, "V19_PRESERVED_EVIDENCE_MISSING")
        content = candidate_blob(root, candidate, path, "V19_PRESERVED_EVIDENCE_MISSING")
        if mode != "100644" or object_type != "blob" or sha256(content) != row["sha256"]:
            raise SuccessorAuthorityError("V19_PRESERVED_EVIDENCE_DRIFT", f"{path}: {mode} {sha256(content)}")


def validate_checkpoint_authority_inputs(root: Path, candidate: str) -> None:
    """Require the approved correction and amended semantic source at the checkpoint."""

    rows = (
        (
            CORRECTION_PATH,
            CORRECTION_SHA256,
            "V19_CORRECTION_NOT_COMMITTED",
            "V19_CORRECTION_MODE_DRIFT",
            "V19_CORRECTION_DRIFT",
        ),
        (
            SEMANTIC_SOURCE_PATH,
            SEMANTIC_SOURCE_SHA256,
            "V19_SEMANTIC_SOURCE_NOT_COMMITTED",
            "V19_SEMANTIC_SOURCE_MODE_DRIFT",
            "V19_SEMANTIC_SOURCE_DRIFT",
        ),
    )
    for path, expected_digest, missing_code, mode_code, drift_code in rows:
        mode, object_type, _ = raw_tree_record(root, candidate, path, missing_code)
        if mode != "100644" or object_type != "blob":
            raise SuccessorAuthorityError(mode_code, f"{path}: {mode} {object_type}")
        content = candidate_blob(root, candidate, path, missing_code)
        if sha256(content) != expected_digest:
            raise SuccessorAuthorityError(drift_code, f"{path}: {sha256(content)}")


def validate_historical_transaction(root: Path) -> dict[str, Any]:
    """Independently reproduce the immutable b819a7c NONCONFORMING finding."""

    commit = resolve_commit(root, HISTORICAL_COMMIT, "V19_HISTORICAL_HISTORY_UNAVAILABLE")
    parents = commit_parents(root, commit, "V19_HISTORICAL_HISTORY_UNAVAILABLE")
    try:
        tree = run_git(root, "show", "-s", "--format=%T", commit).stdout.decode("ascii").strip()
    except UnicodeError as error:
        raise SuccessorAuthorityError("V19_HISTORICAL_HISTORY_UNAVAILABLE", str(error), "BLOCKED") from error
    actual = changed_paths(root, HISTORICAL_PARENT, commit)
    missing = tuple(path for path in CHECKPOINT_PATHS if path not in actual)
    unexpected = tuple(path for path in actual if path not in CHECKPOINT_PATHS)
    historical_result = run_git(root, "cat-file", "-e", f"{commit}:{CHECKPOINT_RESULT_PATH}", allowed=(0, 128))
    if (
        parents != (HISTORICAL_PARENT,)
        or tree != HISTORICAL_TREE
        or actual != HISTORICAL_PATHS
        or missing != HISTORICAL_MISSING_PATHS
        or unexpected != HISTORICAL_UNEXPECTED_PATHS
        or inventory_digest(CHECKPOINT_PATHS) != CHECKPOINT_PATH_DIGEST
        or inventory_digest(actual) != HISTORICAL_PATH_DIGEST
        or inventory_digest(missing) != HISTORICAL_MISSING_DIGEST
        or inventory_digest(unexpected) != HISTORICAL_UNEXPECTED_DIGEST
        or historical_result.returncode == 0
    ):
        raise SuccessorAuthorityError("V19_HISTORICAL_EVIDENCE_DRIFT", "commit relation or path partitions")
    return {
        "commit": commit,
        "parent": HISTORICAL_PARENT,
        "tree": HISTORICAL_TREE,
        "expectedChangedPaths": list(CHECKPOINT_PATHS),
        "expectedChangedPathInventorySha256": CHECKPOINT_PATH_DIGEST,
        "actualChangedPaths": list(actual),
        "actualChangedPathInventorySha256": HISTORICAL_PATH_DIGEST,
        "missingPaths": list(missing),
        "missingPathInventorySha256": HISTORICAL_MISSING_DIGEST,
        "unexpectedPaths": list(unexpected),
        "unexpectedPathInventorySha256": HISTORICAL_UNEXPECTED_DIGEST,
        "machineResultCommitted": False,
        "result": "NONCONFORMING",
        "blockers": ["CHANGED_PATH_SET_MISMATCH"],
    }


def normalized_substantive_bytes(content: bytes) -> bytes:
    """Normalize only Unicode and line endings for substantive-change comparison."""

    try:
        text = content.decode("utf-8", errors="strict").replace("\r\n", "\n").replace("\r", "\n")
    except UnicodeError:
        return content
    return unicodedata.normalize("NFC", text).encode("utf-8")


def validate_substantive_checkpoint_changes(root: Path, baseline: str, candidate: str) -> None:
    """Reject touch-only and Unicode/line-ending-only checkpoint rewrites."""

    for path in CHECKPOINT_PATHS:
        candidate_content = candidate_blob(root, candidate, path, "V19_CHECKPOINT_PATH_MISSING")
        baseline_result = run_git(root, "show", f"{baseline}:{path}", allowed=(0, 128))
        if baseline_result.returncode == 0 and normalized_substantive_bytes(baseline_result.stdout) == normalized_substantive_bytes(
            candidate_content
        ):
            raise SuccessorAuthorityError("V19_NON_SUBSTANTIVE_CHANGE", path)


def checkpoint_bindings(root: Path, candidate: str) -> list[dict[str, str]]:
    """Bind all five changed checkpoint paths to committed regular blobs."""

    bindings: list[dict[str, str]] = []
    for path in CHECKPOINT_PATHS:
        mode, object_type, _ = raw_tree_record(root, candidate, path, "V19_CHECKPOINT_PATH_MISSING")
        if mode != "100644" or object_type != "blob":
            raise SuccessorAuthorityError("V19_CHECKPOINT_MODE_DRIFT", f"{path}: {mode} {object_type}")
        content = candidate_blob(root, candidate, path, "V19_CHECKPOINT_PATH_MISSING")
        bindings.append({"path": path, "mode": mode, "sha256": sha256(content)})
    return bindings


def parse_junit(content: bytes) -> tuple[dict[str, Any], list[dict[str, str]]]:
    """Parse committed JUnit counts and derive the ordered nonempty testcase ledger."""

    try:
        root = ET.fromstring(content.decode("utf-8-sig", errors="strict"))
    except (UnicodeError, ET.ParseError) as error:
        raise SuccessorAuthorityError("V19_RESULT_XML_INVALID", str(error)) from error
    suites = list(root.findall("testsuite"))
    if root.tag != "testsuites" or len(suites) != 1:
        raise SuccessorAuthorityError(
            "V19_RESULT_XML_INVALID",
            f"root={root.tag!r} directSuites={len(suites)}",
        )
    suite = suites[0]
    testcases = list(suite.findall("testcase"))
    if list(root.iter("testcase")) != testcases:
        raise SuccessorAuthorityError("V19_RESULT_XML_INVALID", "testcases must be direct children of the one suite")
    ledger: list[dict[str, str]] = []
    seen: set[str] = set()
    failures = errors = skipped = 0
    for ordinal, testcase in enumerate(testcases, start=1):
        classname = testcase.get("classname") or ""
        name = testcase.get("name") or ""
        if not classname.strip() or not name.strip():
            raise SuccessorAuthorityError(
                "V19_RESULT_LEDGER_INVALID",
                f"testcase {ordinal} requires nonempty classname and name",
            )
        identity = f"{classname}::{name}"
        if not identity.startswith(CHECKPOINT_SUBJECT_PREFIX):
            raise SuccessorAuthorityError("V19_RESULT_SUBJECT_INVALID", identity)
        if identity in seen:
            raise SuccessorAuthorityError("V19_RESULT_LEDGER_DUPLICATE", identity)
        seen.add(identity)
        if testcase.find("failure") is not None:
            state = "FAIL"
            failures += 1
        elif testcase.find("error") is not None:
            state = "BLOCKED"
            errors += 1
        elif testcase.find("skipped") is not None:
            state = "not-applicable"
            skipped += 1
        else:
            state = "PASS"
        ledger.append({"id": f"JUNIT-{ordinal:04d}", "subject": identity, "state": state})
    if not ledger:
        raise SuccessorAuthorityError("V19_RESULT_LEDGER_EMPTY", CHECKPOINT_RESULT_PATH, "BLOCKED")
    try:
        declared_tests = int(suite.get("tests", ""))
        declared_failures = int(suite.get("failures", ""))
        declared_errors = int(suite.get("errors", ""))
        declared_skipped = int(suite.get("skipped", ""))
    except ValueError as error:
        raise SuccessorAuthorityError("V19_RESULT_COUNT_DRIFT", str(error)) from error
    if min(declared_tests, declared_failures, declared_errors, declared_skipped) < 0:
        raise SuccessorAuthorityError(
            "V19_RESULT_COUNT_DRIFT",
            "JUnit counts must be nonnegative integers",
        )
    observed = (len(testcases), failures, errors, skipped)
    declared = (declared_tests, declared_failures, declared_errors, declared_skipped)
    if declared != observed:
        raise SuccessorAuthorityError("V19_RESULT_COUNT_DRIFT", f"declared={declared!r} observed={observed!r}")
    if declared_tests <= 0 or any((declared_failures, declared_errors, declared_skipped)):
        raise SuccessorAuthorityError("V19_RESULT_NOT_PASS", repr(declared))
    observed_subjects = tuple(row["subject"] for row in ledger)
    if observed_subjects != REQUIRED_CHECKPOINT_SUBJECTS:
        raise SuccessorAuthorityError(
            "V19_RESULT_LEDGER_INSUFFICIENT",
            f"expected={REQUIRED_CHECKPOINT_SUBJECTS!r} observed={observed_subjects!r}",
        )
    return {
        "tests": declared_tests,
        "failures": declared_failures,
        "errors": declared_errors,
        "skipped": declared_skipped,
    }, ledger


def rerun_checkpoint_command(root: Path, candidate: str) -> tuple[int, bytes]:
    """Rerun the exact frozen command in an isolated checkout of the candidate."""

    try:
        with tempfile.TemporaryDirectory(prefix="story-7-1-v19-rerun-") as temporary_name:
            checkout = Path(temporary_name) / "candidate"
            run_git(root, "clone", "--shared", "--no-checkout", "--", str(root), str(checkout))
            run_git(checkout, "checkout", "--detach", candidate)
            result_path = checkout / CHECKPOINT_RESULT_PATH
            try:
                result_path.unlink(missing_ok=True)
            except OSError as error:
                raise SuccessorAuthorityError("V19_RESULT_STALE_REMOVE_FAILED", str(error), "BLOCKED") from error
            environment = {
                key: value
                for key, value in os.environ.items()
                if key not in {"PYTEST_ADDOPTS", "PYTHONPATH"} and not key.startswith("GIT_")
            }
            try:
                completed = subprocess.run(
                    CHECKPOINT_COMMAND_ARGUMENTS,
                    cwd=checkout,
                    check=False,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    timeout=120,
                    env=environment,
                )
            except (OSError, subprocess.TimeoutExpired) as error:
                raise SuccessorAuthorityError("V19_RESULT_COMMAND_UNAVAILABLE", str(error), "BLOCKED") from error
            if not result_path.is_file():
                detail = completed.stderr.decode("utf-8", errors="replace").strip() or "result XML was not produced"
                raise SuccessorAuthorityError("V19_RESULT_COMMAND_OUTPUT_MISSING", detail, "BLOCKED")
            try:
                content = result_path.read_bytes()
            except OSError as error:
                raise SuccessorAuthorityError("V19_RESULT_COMMAND_OUTPUT_MISSING", str(error), "BLOCKED") from error
            result = completed.returncode, content
    except SuccessorAuthorityError:
        raise
    except OSError as error:
        raise SuccessorAuthorityError("V19_RESULT_TEMPORARY_CLEANUP_FAILED", str(error), "BLOCKED") from error
    return result


def render_v19(root: Path, candidate_revision: str) -> dict[str, Any]:
    """Recompute the complete candidate-bound V19 authority."""

    root = resolve_repository_root(root)
    candidate = resolve_commit(root, candidate_revision, "V19_CANDIDATE_UNAVAILABLE")
    validate_accepted_checkpoint(root, candidate)
    parents = commit_parents(root, candidate, "V19_CANDIDATE_PARENT_UNAVAILABLE")
    if len(parents) != 1:
        raise SuccessorAuthorityError("V19_CANDIDATE_PARENT_MISMATCH", repr(parents), "BLOCKED")
    baseline = parents[0]
    if baseline != APPROVED_TOOLING_COMMIT:
        raise SuccessorAuthorityError(
            "V19_CANDIDATE_PARENT_MISMATCH",
            f"expected={APPROVED_TOOLING_COMMIT} observed={baseline}",
            "BLOCKED",
        )
    validate_checkpoint_authority_inputs(root, candidate)
    observed_paths = changed_paths(root, baseline, candidate)
    observed_gitlinks = changed_gitlinks(root, baseline, candidate)
    if observed_gitlinks:
        raise SuccessorAuthorityError("V19_CHECKPOINT_GITLINK_DRIFT", repr(observed_gitlinks))
    if len(observed_paths) != len(set(observed_paths)) or set(observed_paths) != set(CHECKPOINT_PATHS):
        missing = [path for path in CHECKPOINT_PATHS if path not in observed_paths]
        unexpected = [path for path in observed_paths if path not in CHECKPOINT_PATHS]
        raise SuccessorAuthorityError(
            "V19_CHECKPOINT_PATH_SET_MISMATCH", f"missing={missing!r} unexpected={unexpected!r}"
        )
    validate_substantive_checkpoint_changes(root, baseline, candidate)
    v9_content = candidate_blob(root, candidate, V9_BUNDLE_PATH, "V19_V9_BUNDLE_MISSING")
    validate_v9_bundle(v9_content)
    v11_content = candidate_blob(root, candidate, V11_PATH, "V19_V11_MISSING")
    validate_v11(v11_content)
    validate_preserved_evidence(root, candidate)
    gitlinks = root_gitlinks(root, candidate)
    bindings = checkpoint_bindings(root, candidate)
    result_binding = bindings[-1]
    result_content = candidate_blob(root, candidate, CHECKPOINT_RESULT_PATH, "V19_RESULT_MISSING")
    counts, ledger = parse_junit(result_content)
    command_exit, rerun_content = rerun_checkpoint_command(root, candidate)
    if command_exit != 0:
        state = "FAIL" if command_exit in (1, 5) else "BLOCKED"
        raise SuccessorAuthorityError(
            "V19_RESULT_COMMAND_FAILED",
            f"command={CHECKPOINT_COMMAND!r} exit={command_exit}",
            state,
        )
    rerun_counts, rerun_ledger = parse_junit(rerun_content)
    if rerun_counts != counts or rerun_ledger != ledger:
        raise SuccessorAuthorityError(
            "V19_RESULT_RERUN_DRIFT",
            f"committed={ledger!r} rerun={rerun_ledger!r}",
        )
    return {
        "schemaVersion": V19_SCHEMA_VERSION,
        "authorityId": V19_AUTHORITY_ID,
        "predecessor": {"authorityId": "V18-PACKAGE-ENVIRONMENT-AUTHORITY", "path": V18_PATH, "sha256": V18_RAW_SHA256},
        "preservedEvidence": list(PRESERVED_EVIDENCE),
        "planningAuthority": {
            "planningCandidate": PLANNING_CANDIDATE,
            "bundlePath": V9_BUNDLE_PATH,
            "bundleRawSha256": V9_BUNDLE_RAW_SHA256,
            "bundleDigest": V9_BUNDLE_DIGEST,
        },
        "v11Authority": {
            "epic": V11_EPIC,
            "architecture": V11_ARCHITECTURE,
            "sidecarPath": V11_PATH,
            "sidecarRawSha256": V11_RAW_SHA256,
        },
        "historicalTransaction": validate_historical_transaction(root),
        "freshCheckpoint": {
            "baselineCommit": baseline,
            "candidateCommit": candidate,
            "changedPaths": list(CHECKPOINT_PATHS),
            "changedPathInventorySha256": CHECKPOINT_PATH_DIGEST,
            "changedPathBindings": bindings,
            "changedGitlinkPaths": [],
            "rootGitlinks": gitlinks,
            "machineResult": {
                "path": CHECKPOINT_RESULT_PATH,
                "mode": result_binding["mode"],
                "sha256": result_binding["sha256"],
                "result": "PASS",
                "command": CHECKPOINT_COMMAND,
                "exitCode": command_exit,
                **counts,
            },
            "assertionLedger": ledger,
        },
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True},
        "result": "PASS",
        "authorityEffect": {
            "checkpointComplete": True,
            "implementationHold": "ACTIVE",
            "fullStoryExecutionAllowed": False,
            "storyDoneAllowed": False,
            "successorUnlocked": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
    }


def schema_at(root: Path, candidate: str, path: str, code: str) -> bytes:
    """Require a schema to be a committed regular blob."""

    mode, object_type, _ = raw_tree_record(root, candidate, path, code)
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError(code, f"{path}: {mode} {object_type}", "BLOCKED")
    return candidate_blob(root, candidate, path, code)


def publication_candidates(root: Path, evaluated: str, path: str, code: str) -> tuple[str, ...]:
    """Return every committed addition of one authority path in evaluated history."""

    require_complete_history(root)
    try:
        output = run_git(
            root,
            "log",
            "--full-history",
            "--format=%H",
            "--diff-filter=A",
            evaluated,
            "--",
            path,
        ).stdout.decode("ascii")
    except SuccessorAuthorityError as error:
        raise SuccessorAuthorityError(code, error.detail, "BLOCKED") from error
    except UnicodeError as error:
        raise SuccessorAuthorityError(code, str(error), "BLOCKED") from error
    candidates = tuple(line for line in output.splitlines() if line)
    if any(re.fullmatch(r"[0-9a-f]{40}", candidate) is None for candidate in candidates):
        raise SuccessorAuthorityError(code, repr(candidates), "BLOCKED")
    return candidates


def locate_publication(root: Path, evaluated: str, path: str, requested: str | None, code: str) -> str:
    """Locate exactly one committed addition of an authority in evaluated history."""

    candidates = publication_candidates(root, evaluated, path, code)
    if len(candidates) != 1:
        raise SuccessorAuthorityError(code, f"expected one publication; observed={candidates!r}", "BLOCKED")
    if requested is not None:
        requested_commit = resolve_commit(root, requested, code)
        if candidates[0] != requested_commit:
            raise SuccessorAuthorityError(
                code,
                f"requested={requested_commit} observed={candidates[0]}",
                "BLOCKED",
            )
    return candidates[0]


def require_commit_path_absent(root: Path, candidate: str, path: str, code: str) -> None:
    """Require a future authority path to be absent from its source candidate."""

    if run_git(root, "ls-tree", "-z", candidate, "--", safe_path(path)).stdout:
        raise SuccessorAuthorityError(code, f"{path}@{candidate}")


def validate_publication(
    root: Path,
    *,
    source: str,
    publication: str,
    evaluated: str,
    path: str,
    prefix: str,
) -> bytes:
    """Validate a direct-child exact-one-path publication and immutable descendant bytes."""

    require_single_parent(root, publication, source, f"{prefix}_PUBLICATION_PARENT_MISMATCH")
    prior_entry = run_git(root, "ls-tree", "-z", source, "--", safe_path(path)).stdout
    if prior_entry:
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_NOT_ADDITIVE", path)
    paths = changed_paths(root, source, publication)
    if paths != (path,):
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_SCOPE_DRIFT", repr(paths))
    if changed_gitlinks(root, source, publication):
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_GITLINK_DRIFT", repr(changed_gitlinks(root, source, publication)))
    mode, object_type, _ = raw_tree_record(root, publication, path, f"{prefix}_PUBLICATION_PATH_MISSING")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError(f"{prefix}_PUBLICATION_MODE_DRIFT", f"{mode} {object_type}")
    require_ancestor(root, publication, evaluated, f"{prefix}_PUBLICATION_NOT_ANCESTOR")
    published = candidate_blob(root, publication, path, f"{prefix}_PUBLICATION_PATH_MISSING")
    descendant_mode, descendant_type, _ = raw_tree_record(
        root,
        evaluated,
        path,
        f"{prefix}_AUTHORITY_DESCENDANT_MISSING",
    )
    if descendant_mode != "100644" or descendant_type != "blob":
        raise SuccessorAuthorityError(
            f"{prefix}_AUTHORITY_DESCENDANT_MODE_DRIFT",
            f"{path}@{evaluated}: {descendant_mode} {descendant_type}",
        )
    descendant = candidate_blob(root, evaluated, path, f"{prefix}_AUTHORITY_DESCENDANT_MISSING")
    if published != descendant:
        raise SuccessorAuthorityError(f"{prefix}_AUTHORITY_DESCENDANT_DRIFT", path)
    return published


def open_relative_file(root_descriptor: int, relative_path: str) -> int:
    """Open one regular file below a pinned repository descriptor without following links."""

    parts = PurePosixPath(safe_path(relative_path)).parts
    current = -1
    result = -1
    try:
        current = os.dup(root_descriptor)
        for part in parts[:-1]:
            child = os.open(
                part,
                os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW,
                dir_fd=current,
            )
            try:
                os.close(current)
            except BaseException:
                try:
                    os.close(child)
                except OSError:
                    pass
                raise
            current = child
        result = os.open(parts[-1], os.O_RDONLY | os.O_NOFOLLOW, dir_fd=current)
        os.close(current)
        current = -1
    except BaseException:
        for descriptor in (result, current):
            if descriptor >= 0:
                try:
                    os.close(descriptor)
                except OSError:
                    pass
        raise
    return result


def read_relative_file_no_follow(
    root: Path,
    relative_path: str,
    code: str,
    *,
    missing_ok: bool = False,
) -> bytes | None:
    """Read a regular worktree file through pinned no-follow descriptors."""

    root_descriptor = -1
    content_descriptor = -1
    content: bytes | None = None
    missing = False
    authority_error: SuccessorAuthorityError | None = None
    primary_error: OSError | RuntimeError | None = None
    try:
        _repository, root_descriptor = open_repository_root(root)
        try:
            content_descriptor = open_relative_file(root_descriptor, relative_path)
        except FileNotFoundError as error:
            if missing_ok:
                missing = True
            else:
                primary_error = error
        if not missing and primary_error is None:
            metadata = os.fstat(content_descriptor)
            if not stat.S_ISREG(metadata.st_mode):
                raise OSError(f"not a regular file: {relative_path}")
            chunks: list[bytes] = []
            while True:
                chunk = os.read(content_descriptor, 64 * 1024)
                if not chunk:
                    break
                chunks.append(chunk)
            content = b"".join(chunks)
    except SuccessorAuthorityError as error:
        authority_error = error
    except (OSError, RuntimeError) as error:
        primary_error = primary_error or error

    cleanup_error: OSError | None = None
    for descriptor in (content_descriptor, root_descriptor):
        if descriptor >= 0:
            try:
                os.close(descriptor)
            except OSError as error:
                cleanup_error = cleanup_error or error
    if authority_error is not None:
        if cleanup_error is None:
            raise authority_error
        raise SuccessorAuthorityError(
            authority_error.code,
            f"{authority_error.detail}; cleanup={cleanup_error}",
            authority_error.state,
        ) from authority_error
    if primary_error is not None:
        detail = str(primary_error)
        if cleanup_error is not None:
            detail = f"{detail}; cleanup={cleanup_error}"
        raise SuccessorAuthorityError(code, detail, "BLOCKED") from primary_error
    if cleanup_error is not None:
        raise SuccessorAuthorityError(code, f"descriptor cleanup failed: {cleanup_error}", "BLOCKED") from cleanup_error
    if missing:
        return None
    assert content is not None
    return content


def open_publication_parent(
    root: Path,
    relative_path: str,
) -> tuple[int, int, str]:
    """Open/create a publication parent through pinned, no-follow directory descriptors."""

    parts = PurePosixPath(safe_path(relative_path)).parts
    flags = os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW
    root_descriptor = -1
    parent_descriptor = -1
    child_descriptor = -1
    failed_component: str | None = None
    try:
        _repository, root_descriptor = open_repository_root(root)
        parent_descriptor = os.dup(root_descriptor)
        for part in parts[:-1]:
            failed_component = part
            try:
                os.mkdir(part, 0o755, dir_fd=parent_descriptor)
            except FileExistsError:
                pass
            child_descriptor = os.open(part, flags, dir_fd=parent_descriptor)
            os.close(parent_descriptor)
            parent_descriptor = child_descriptor
            child_descriptor = -1
            failed_component = None
    except BaseException as error:
        path_escape = isinstance(error, OSError) and error.errno == errno.ELOOP
        if (
            isinstance(error, OSError)
            and error.errno == errno.ENOTDIR
            and failed_component is not None
            and parent_descriptor >= 0
        ):
            try:
                path_escape = stat.S_ISLNK(
                    os.stat(
                        failed_component,
                        dir_fd=parent_descriptor,
                        follow_symlinks=False,
                    ).st_mode
                )
            except OSError:
                path_escape = False
        for descriptor in (child_descriptor, parent_descriptor, root_descriptor):
            if descriptor >= 0:
                try:
                    os.close(descriptor)
                except OSError:
                    pass
        if path_escape:
            raise SuccessorAuthorityError(
                "SUCCESSOR_PATH_ESCAPE",
                f"{relative_path}: {error}",
                "BLOCKED",
            ) from error
        raise
    return root_descriptor, parent_descriptor, parts[-1]


def write_atomic_no_clobber(
    root: Path,
    relative_path: str,
    content: bytes,
    *,
    exists_code: str,
    failure_code: str,
) -> None:
    """Publish through repository-rooted descriptors without following swapped pathnames."""

    root_descriptor = -1
    parent_descriptor = -1
    content_descriptor = -1
    installed_descriptor = -1
    temporary_name: str | None = None
    target_name: str | None = None
    installed_identity: tuple[int, int] | None = None

    def best_effort_cleanup() -> str | None:
        cleanup_error: OSError | None = None
        nonlocal installed_descriptor, content_descriptor
        nonlocal temporary_name, parent_descriptor, root_descriptor
        for descriptor_name in ("installed_descriptor", "content_descriptor"):
            descriptor = installed_descriptor if descriptor_name == "installed_descriptor" else content_descriptor
            if descriptor >= 0:
                if descriptor_name == "installed_descriptor":
                    installed_descriptor = -1
                else:
                    content_descriptor = -1
                try:
                    os.close(descriptor)
                except OSError as error:
                    cleanup_error = cleanup_error or error
        if temporary_name is not None and parent_descriptor >= 0:
            owned_temporary_name = temporary_name
            temporary_name = None
            try:
                os.unlink(owned_temporary_name, dir_fd=parent_descriptor)
            except FileNotFoundError:
                pass
            except OSError as error:
                cleanup_error = cleanup_error or error
        for descriptor_name in ("parent_descriptor", "root_descriptor"):
            descriptor = parent_descriptor if descriptor_name == "parent_descriptor" else root_descriptor
            if descriptor >= 0:
                if descriptor_name == "parent_descriptor":
                    parent_descriptor = -1
                else:
                    root_descriptor = -1
                try:
                    os.close(descriptor)
                except OSError as error:
                    cleanup_error = cleanup_error or error
        return None if cleanup_error is None else str(cleanup_error)

    try:
        root_descriptor, parent_descriptor, target_name = open_publication_parent(root, relative_path)
        for _attempt in range(128):
            candidate = f".{target_name}.{os.getpid()}.{secrets.token_hex(8)}.tmp"
            try:
                content_descriptor = os.open(
                    candidate,
                    os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW,
                    0o600,
                    dir_fd=parent_descriptor,
                )
                temporary_name = candidate
                break
            except FileExistsError:
                continue
        else:
            raise OSError("unable to allocate a unique publication temporary name")
        os.fchmod(content_descriptor, 0o644)
        offset = 0
        while offset < len(content):
            written = os.write(content_descriptor, content[offset:])
            if written <= 0:
                raise OSError("publication write made no progress")
            offset += written
        os.fsync(content_descriptor)
        temporary_stat = os.fstat(content_descriptor)
        installed_identity = (temporary_stat.st_dev, temporary_stat.st_ino)
        try:
            os.link(
                temporary_name,
                target_name,
                src_dir_fd=parent_descriptor,
                dst_dir_fd=parent_descriptor,
                follow_symlinks=False,
            )
        except FileExistsError as error:
            cleanup_detail = best_effort_cleanup()
            if cleanup_detail is not None:
                raise SuccessorAuthorityError(failure_code, f"{relative_path}; cleanup={cleanup_detail}", "BLOCKED") from error
            raise SuccessorAuthorityError(exists_code, relative_path) from error
        os.fsync(parent_descriptor)
        installed_descriptor = open_relative_file(root_descriptor, relative_path)
        installed_stat = os.fstat(installed_descriptor)
        if (installed_stat.st_dev, installed_stat.st_ino) != (temporary_stat.st_dev, temporary_stat.st_ino):
            raise OSError("installed publication inode changed during recheck")
        owned_installed_descriptor = installed_descriptor
        installed_descriptor = -1
        os.close(owned_installed_descriptor)
        owned_content_descriptor = content_descriptor
        content_descriptor = -1
        os.close(owned_content_descriptor)
        owned_temporary_name = temporary_name
        assert owned_temporary_name is not None
        os.unlink(owned_temporary_name, dir_fd=parent_descriptor)
        temporary_name = None
        os.fsync(parent_descriptor)
        owned_parent_descriptor = parent_descriptor
        parent_descriptor = -1
        os.close(owned_parent_descriptor)
        owned_root_descriptor = root_descriptor
        root_descriptor = -1
        os.close(owned_root_descriptor)
    except SuccessorAuthorityError as error:
        cleanup_detail = best_effort_cleanup()
        if cleanup_detail is None:
            raise
        raise SuccessorAuthorityError(
            error.code,
            f"{error.detail}; cleanup={cleanup_detail}",
            error.state,
        ) from error
    except (OSError, RuntimeError) as error:
        cleanup_detail = best_effort_cleanup()
        detail = str(error) if cleanup_detail is None else f"{error}; cleanup={cleanup_detail}"
        raise SuccessorAuthorityError(failure_code, detail, "BLOCKED") from error


def publish_v19(
    root: Path,
    *,
    candidate_revision: str | None,
    check: bool,
    publication_revision: str | None = None,
) -> dict[str, Any]:
    """Write V19 or validate its committed candidate-before-record transaction."""

    root = resolve_repository_root(root)
    if check:
        evaluated = resolve_commit(root, candidate_revision or "HEAD", "V19_EVALUATED_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated, V19_PATH, publication_revision, "V19_PUBLICATION_MISSING")
        if publication != APPROVED_V19_PUBLICATION:
            raise SuccessorAuthorityError(
                "V19_PUBLICATION_COMMIT_MISMATCH",
                f"expected={APPROVED_V19_PUBLICATION} observed={publication}",
            )
        existing_bytes = candidate_blob(root, publication, V19_PATH, "V19_AUTHORITY_MISSING")
        existing = parse_json(existing_bytes, "V19_AUTHORITY_INVALID")
        fresh = existing.get("freshCheckpoint")
        if not isinstance(fresh, dict) or not isinstance(fresh.get("candidateCommit"), str):
            raise SuccessorAuthorityError("V19_AUTHORITY_INVALID", "freshCheckpoint.candidateCommit")
        candidate = resolve_commit(root, fresh["candidateCommit"], "V19_CANDIDATE_UNAVAILABLE")
    else:
        candidate = resolve_commit(root, candidate_revision or "HEAD", "V19_CANDIDATE_UNAVAILABLE")
        require_commit_path_absent(root, candidate, V19_PATH, "V19_AUTHORITY_ALREADY_COMMITTED")
        existing = None
        existing_bytes = b""
    document = render_v19(root, candidate)
    validate_json_schema(schema_at(root, candidate, V19_SCHEMA_PATH, "V19_SCHEMA_MISSING"), document, "V19_SCHEMA_INVALID")
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise SuccessorAuthorityError("V19_AUTHORITY_DRIFT", V19_PATH)
        validate_publication(
            root,
            source=candidate,
            publication=publication,
            evaluated=evaluated,
            path=V19_PATH,
            prefix="V19",
        )
    else:
        write_atomic_no_clobber(
            root,
            V19_PATH,
            json_bytes(document),
            exists_code="V19_AUTHORITY_ALREADY_EXISTS",
            failure_code="V19_WRITE_FAILED",
        )
    return document


def binding_role(path: str) -> str:
    """Return the frozen non-interchangeable role for a scenario input path."""

    roles = {
        ".gitmodules": "root-submodule-inventory",
        V9_BUNDLE_PATH: "planning-authority-bundle",
        "_bmad-output/planning-artifacts/v9/story-contracts/7.1.json": "story-contract",
        V11_PATH: "checkpoint-scope-authority",
        V19_PATH: "checkpoint-completion-authority",
        V20_PATH: "release-owner-authority",
        INVENTORY_PATH: "frozen-input-inventory",
        "_bmad/schemas/v9-story-contract-v1.schema.json": "story-contract-schema",
        INVENTORY_SCHEMA_PATH: "frozen-input-inventory-schema",
        "_bmad/schemas/v9-acceptance-result-v1.schema.json": "acceptance-result-schema",
        "_bmad/schemas/v9-frozen-inventory-v1.schema.json": "frozen-inventory-schema",
        "_bmad/schemas/story-final-record-v2.schema.json": "story-final-record-schema",
        "_bmad/schemas/story-record-generator-failure-v1.schema.json": "generator-failure-schema",
        "_bmad/scripts/generate_story_record.py": "story-record-generator",
        "_bmad/scripts/tests/test_generate_story_record.py": "story-record-generator-tests",
    }
    if path in RESULT_PATHS:
        return "machine-result-snapshot"
    return roles[path]


def binding_mode(path: str) -> str:
    """Return the currently required committed mode for one scenario input."""

    return "100755" if path == "_bmad/scripts/generate_story_record.py" else "100644"


def expected_inventory_document() -> dict[str, Any]:
    """Return the exact closed V20 inventory required by proposal sections 5.4-5.6."""

    path_inventories = [
        {
            "role": "implementation-candidate-delta",
            "inventoryId": "V20-7.1-IMPLEMENTATION-PATHS-v1",
            "paths": list(IMPLEMENTATION_PATHS),
            "sha256": "4405332b49ec26ffff40c9b7424c858e7634e0d92313b2c0270011e29a132f4b",
        },
        {
            "role": "machine-results",
            "inventoryId": "V20-7.1-RESULT-PATHS-v1",
            "paths": list(RESULT_PATHS),
            "sha256": "f54595279fc201056604e56971f2e22f5483c505ddb1a2488b69e513f08b0fef",
        },
        {
            "role": "post-candidate-record-outputs",
            "inventoryId": "V20-7.1-RECORD-OUTPUT-PATHS-v1",
            "paths": list(RECORD_OUTPUT_PATHS),
            "sha256": "80b1c47320b8f4ebb98e0dfb40d6777cded1e4e08541c7734678991ab5598b64",
        },
    ]
    scenarios = []
    for scenario, paths in SCENARIO_PATHS.items():
        inputs = [
            {
                "path": path,
                "sha256": "recompute-from-committed-SC-7.1-or-machine-result-snapshot",
                "mode": binding_mode(path),
                "role": binding_role(path),
            }
            for path in paths
        ]
        scenarios.append(
            {
                "scenarioId": scenario,
                "inventoryId": SCENARIO_IDENTITIES[scenario],
                "paths": list(paths),
                "sha256": SCENARIO_DIGESTS[scenario],
                "orderedInputBindings": inputs,
            }
        )
    return {
        "schemaVersion": INVENTORY_SCHEMA_VERSION,
        "inventoryId": "V20-STORY-7.1-INPUT-INVENTORY-v1",
        "digestAlgorithm": "sha256",
        "canonicalization": {
            "rule": "nfc-utf8-lf-path-list",
            "itemEncoding": "one-repository-relative-path-per-line",
            "terminalLf": True,
            "orderSignificant": True,
        },
        "pathInventories": path_inventories,
        "authorityPrefix": {
            "inventoryId": "V20-7.1-AUTHORITY-PREFIX-v1",
            "paths": list(AUTHORITY_PREFIX),
            "sha256": inventory_digest(AUTHORITY_PREFIX),
        },
        "currentInputObservations": [
            {"path": path, "role": role, "rawSha256": digest} for path, role, digest in CURRENT_INPUTS
        ],
        "candidateDerivedBindings": {
            "checkpointCandidate": [
                {
                    "path": path,
                    "rule": "recompute-raw-mode-and-sha256-at-V19-checkpoint-candidate",
                }
                for path in CHECKPOINT_PATHS[:-1]
            ],
            "storyCandidate": [
                {"path": path, "rule": "recompute-raw-mode-and-sha256-at-SC-7.1"}
                for path in IMPLEMENTATION_PATHS
            ],
            "machineResults": [
                {"path": path, "rule": "generator-captured-actual-exit-and-ID-derived-XML-snapshot"}
                for path in RESULT_PATHS
            ],
        },
        "entryObligations": {
            "inventoryId": "V9-7.1-ENTRY-v1",
            "items": [
                "V8-6.8-AC1",
                "V8-6.8-AC6-ANTI-VACUITY",
                "V8-6.8-PROHIBITIONS-SOURCE-BOUNDARY",
            ],
            "sha256": "5fb79e8d9251c3187f2a2de7d4ae3766ab962015e628d345f8033bf14ba8e36e",
        },
        "scenarioInventories": scenarios,
        "scenarioResultBinding": {
            "requiredFields": ["path", "sha256", "mode", "role"],
            "orderedBy": "scenario-inventory-path-order",
            "declaredDigestTrusted": False,
            "candidateMoveInvalidatesResults": True,
        },
    }


def validate_inventory_document(document: dict[str, Any]) -> None:
    """Recompute every path digest and exact identity in the V20 inventory."""

    expected = expected_inventory_document()
    if document != expected:
        raise SuccessorAuthorityError("V20_INVENTORY_CONTENT_DRIFT", INVENTORY_PATH)
    inventories = [*document["pathInventories"], document["authorityPrefix"], *document["scenarioInventories"]]
    for row in inventories:
        paths = row["paths"]
        if len(paths) != len(set(paths)) or any(safe_path(path) != path for path in paths):
            raise SuccessorAuthorityError("V20_INVENTORY_PATH_SET_DRIFT", row["inventoryId"])
        observed = inventory_digest(paths)
        if observed != row["sha256"]:
            raise SuccessorAuthorityError("V20_INVENTORY_DIGEST_DRIFT", f"{row['inventoryId']}: {observed}")
    for scenario in document["scenarioInventories"]:
        bindings = scenario["orderedInputBindings"]
        if [row["path"] for row in bindings] != scenario["paths"]:
            raise SuccessorAuthorityError("V20_SCENARIO_BINDING_ORDER_DRIFT", scenario["scenarioId"])


def validate_inventory_observations(root: Path, evaluated: str) -> list[dict[str, str]]:
    """Validate frozen observations only at the historical revisions that authored them."""

    ledger: list[dict[str, str]] = []
    v19: dict[str, Any] | None = None
    baseline = evaluated
    checkpoint = evaluated
    tooling_relation = run_git(
        root,
        "merge-base",
        "--is-ancestor",
        APPROVED_TOOLING_COMMIT,
        evaluated,
        allowed=(0, 1),
    )
    if tooling_relation.returncode == 0:
        baseline = APPROVED_TOOLING_COMMIT
    if run_git(root, "ls-tree", "-z", evaluated, "--", V19_PATH).stdout:
        v19, _publication, _content = check_v19_at(root, evaluated)
        baseline = str(v19["freshCheckpoint"]["baselineCommit"])
        checkpoint = str(v19["freshCheckpoint"]["candidateCommit"])
    for ordinal, (path, _role, expected_digest) in enumerate(CURRENT_INPUTS, start=1):
        observation_revision = baseline
        mode, object_type, _ = raw_tree_record(
            root,
            observation_revision,
            path,
            "V20_FIXED_INPUT_MISSING",
        )
        content = candidate_blob(root, observation_revision, path, "V20_FIXED_INPUT_MISSING")
        observed_digest = sha256(content)
        if mode != binding_mode(path) or object_type != "blob" or observed_digest != expected_digest:
            raise SuccessorAuthorityError(
                "V20_FIXED_INPUT_DRIFT",
                f"{path}@{observation_revision}: {mode} {observed_digest}",
            )
        ledger.append(
            {
                "id": f"INVENTORY-OBSERVATION-{ordinal:04d}",
                "subject": f"{path}@{observation_revision}",
                "state": "PASS",
            }
        )
    if v19 is not None:
        bindings = v19["freshCheckpoint"]["changedPathBindings"]
        if [binding["path"] for binding in bindings] != list(CHECKPOINT_PATHS):
            raise SuccessorAuthorityError("V20_CHECKPOINT_BINDING_DRIFT", repr(bindings))
        for ordinal, binding in enumerate(bindings, start=1):
            path = str(binding["path"])
            mode, object_type, _ = raw_tree_record(root, checkpoint, path, "V20_CHECKPOINT_INPUT_MISSING")
            content = candidate_blob(root, checkpoint, path, "V20_CHECKPOINT_INPUT_MISSING")
            if mode != binding["mode"] or object_type != "blob" or sha256(content) != binding["sha256"]:
                raise SuccessorAuthorityError("V20_CHECKPOINT_INPUT_DRIFT", f"{path}@{checkpoint}")
            ledger.append(
                {
                    "id": f"INVENTORY-CHECKPOINT-{ordinal:04d}",
                    "subject": f"{path}@{checkpoint}",
                    "state": "PASS",
                }
            )
    if not ledger:
        raise SuccessorAuthorityError("V20_INVENTORY_LEDGER_EMPTY", evaluated, "BLOCKED")
    return ledger


def inventory_route(
    root: Path,
    *,
    check: bool,
    evaluated_revision: str | None = None,
    assertion_ledger: list[dict[str, str]] | None = None,
) -> dict[str, Any]:
    """Write or validate the frozen V20 inventory without publishing an authority."""

    root = resolve_repository_root(root)
    expected = expected_inventory_document()
    evaluated: str | None = None
    if check:
        evaluated = resolve_commit(root, evaluated_revision or "HEAD", "V20_INVENTORY_CANDIDATE_UNAVAILABLE")
        schema_content = schema_at(root, evaluated, INVENTORY_SCHEMA_PATH, "V20_INVENTORY_SCHEMA_MISSING")
    else:
        schema_content = read_relative_file_no_follow(
            root,
            INVENTORY_SCHEMA_PATH,
            "V20_INVENTORY_SCHEMA_MISSING",
        )
        assert schema_content is not None
    validate_json_schema(schema_content, expected, "V20_INVENTORY_SCHEMA_INVALID")
    validate_inventory_document(expected)
    expected_bytes = json_bytes(expected)
    if check:
        assert evaluated is not None
        existing_bytes = candidate_blob(root, evaluated, INVENTORY_PATH, "V20_INVENTORY_MISSING")
        mode, object_type, _ = raw_tree_record(root, evaluated, INVENTORY_PATH, "V20_INVENTORY_MISSING")
        if mode != "100644" or object_type != "blob":
            raise SuccessorAuthorityError("V20_INVENTORY_MODE_DRIFT", f"{mode} {object_type}")
        existing = parse_json(existing_bytes, "V20_INVENTORY_INVALID")
        validate_json_schema(schema_content, existing, "V20_INVENTORY_SCHEMA_INVALID")
        validate_inventory_document(existing)
        if existing_bytes != expected_bytes:
            raise SuccessorAuthorityError("V20_INVENTORY_BYTES_DRIFT", INVENTORY_PATH)
        ledger = validate_inventory_observations(root, evaluated)
    else:
        ledger = []
        for ordinal, (path, _role, digest) in enumerate(CURRENT_INPUTS, start=1):
            content = read_relative_file_no_follow(
                root,
                path,
                "V20_FIXED_INPUT_MISSING",
            )
            assert content is not None
            if sha256(content) != digest:
                raise SuccessorAuthorityError("V20_FIXED_INPUT_DRIFT", f"{path}: {sha256(content)}")
            ledger.append(
                {"id": f"INVENTORY-OBSERVATION-{ordinal:04d}", "subject": path, "state": "PASS"}
            )
        existing_bytes = read_relative_file_no_follow(
            root,
            INVENTORY_PATH,
            "V20_INVENTORY_READ_FAILED",
            missing_ok=True,
        )
        if existing_bytes is not None:
            if existing_bytes != expected_bytes:
                raise SuccessorAuthorityError("V20_INVENTORY_OVERWRITE_REFUSED", INVENTORY_PATH)
        else:
            try:
                write_atomic_no_clobber(
                    root,
                    INVENTORY_PATH,
                    expected_bytes,
                    exists_code="V20_INVENTORY_OVERWRITE_REFUSED",
                    failure_code="V20_INVENTORY_WRITE_FAILED",
                )
            except SuccessorAuthorityError as error:
                if error.code != "V20_INVENTORY_OVERWRITE_REFUSED":
                    raise
                concurrent_bytes = read_relative_file_no_follow(
                    root,
                    INVENTORY_PATH,
                    "V20_INVENTORY_READ_FAILED",
                )
                assert concurrent_bytes is not None
                if concurrent_bytes != expected_bytes:
                    raise
    if assertion_ledger is not None:
        assertion_ledger.extend(ledger)
    return expected


def validate_committed_inventory(root: Path, entry: str) -> tuple[bytes, bytes]:
    """Validate the exact committed inventory schema/data pair at entry."""

    schema_content = schema_at(root, entry, INVENTORY_SCHEMA_PATH, "V20_INVENTORY_SCHEMA_NOT_COMMITTED")
    inventory_content = candidate_blob(root, entry, INVENTORY_PATH, "V20_INVENTORY_NOT_COMMITTED")
    mode, object_type, _ = raw_tree_record(root, entry, INVENTORY_PATH, "V20_INVENTORY_NOT_COMMITTED")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError("V20_INVENTORY_MODE_DRIFT", f"{mode} {object_type}")
    document = parse_json(inventory_content, "V20_INVENTORY_INVALID")
    validate_json_schema(schema_content, document, "V20_INVENTORY_SCHEMA_INVALID")
    validate_inventory_document(document)
    if inventory_content != json_bytes(expected_inventory_document()):
        raise SuccessorAuthorityError("V20_INVENTORY_BYTES_DRIFT", INVENTORY_PATH)
    return inventory_content, schema_content


def validate_owner_fields(identity: str, decided_at_utc: str, rationale: str) -> datetime:
    """Require a concrete independent human identity, UTC instant, and rationale."""

    normalized_identity = identity.strip()
    if re.fullmatch(r"[^<>\r\n]+ <[^<>\s]+@[^<>\s]+>", normalized_identity) is None:
        raise SuccessorAuthorityError("V20_OWNER_IDENTITY_INVALID", repr(identity))
    if normalized_identity != TRUSTED_OWNER_IDENTITY:
        raise SuccessorAuthorityError(
            "V20_OWNER_IDENTITY_MISMATCH",
            f"expected={TRUSTED_OWNER_IDENTITY!r} observed={normalized_identity!r}",
        )
    if not rationale.strip() or V19_AUTHORITY_ID not in rationale or "V20-STORY-7.1-INPUT-INVENTORY-v1" not in rationale:
        raise SuccessorAuthorityError("V20_OWNER_RATIONALE_INVALID", "rationale must bind V19 and the frozen inventory")
    if re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", decided_at_utc) is None:
        raise SuccessorAuthorityError("V20_OWNER_DECISION_TIME_INVALID", repr(decided_at_utc))
    try:
        parsed = datetime.strptime(decided_at_utc, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    except ValueError as error:
        raise SuccessorAuthorityError("V20_OWNER_DECISION_TIME_INVALID", str(error)) from error
    if parsed.year < 2026:
        raise SuccessorAuthorityError("V20_OWNER_DECISION_TIME_INVALID", decided_at_utc)
    return parsed


def commit_time(root: Path, commit: str, code: str) -> datetime:
    """Return one commit's UTC committer instant."""

    try:
        value = run_git(root, "show", "-s", "--format=%ct", commit).stdout.decode("ascii").strip()
        return datetime.fromtimestamp(int(value), tz=timezone.utc)
    except (SuccessorAuthorityError, UnicodeError, ValueError, OverflowError, OSError) as error:
        detail = error.detail if isinstance(error, SuccessorAuthorityError) else str(error)
        raise SuccessorAuthorityError(code, detail, "BLOCKED") from error


def verify_v20_publication_signature(root: Path, publication: str, owner_identity: str) -> dict[str, str]:
    """Verify V20 with only the source-pinned SSH trust anchor and return exact facts."""

    normalized_identity = owner_identity.strip()
    if re.fullmatch(r"[^<>\r\n]+ <[^<>\s]+@[^<>\s]+>", normalized_identity) is None:
        raise SuccessorAuthorityError("V20_OWNER_IDENTITY_INVALID", repr(owner_identity))
    if normalized_identity != TRUSTED_OWNER_IDENTITY:
        raise SuccessorAuthorityError(
            "V20_OWNER_IDENTITY_MISMATCH",
            f"expected={TRUSTED_OWNER_IDENTITY!r} observed={normalized_identity!r}",
        )
    try:
        author = run_git(root, "show", "-s", "--format=%an <%ae>", publication).stdout.decode("utf-8").strip()
    except UnicodeError as error:
        raise SuccessorAuthorityError("V20_PUBLICATION_AUTHOR_INVALID", str(error), "BLOCKED") from error
    if author != normalized_identity:
        raise SuccessorAuthorityError(
            "V20_PUBLICATION_AUTHOR_MISMATCH",
            f"expected={normalized_identity!r} observed={author!r}",
        )
    trusted_ssh_keygen = shutil.which("ssh-keygen")
    if trusted_ssh_keygen is None:
        raise SuccessorAuthorityError(
            "V20_TRUST_TOOL_UNAVAILABLE",
            "ssh-keygen was not found on the trusted runner PATH",
            "BLOCKED",
        )
    try:
        with tempfile.TemporaryDirectory(prefix="story-7-1-v20-trust-") as temporary_name:
            allowed_signers = Path(temporary_name) / "allowed_signers"
            revocation_file = Path(temporary_name) / "revoked_signers"
            allowed_signers.write_text(
                f"{TRUSTED_SSH_PRINCIPAL} {TRUSTED_SSH_PUBLIC_KEY}\n",
                encoding="utf-8",
            )
            revocation_file.write_bytes(b"")
            scoped_config = (
                "-c",
                "gpg.format=ssh",
                "-c",
                f"gpg.ssh.allowedSignersFile={allowed_signers}",
                "-c",
                f"gpg.ssh.program={trusted_ssh_keygen}",
                "-c",
                f"gpg.ssh.revocationFile={revocation_file}",
                "-c",
                "gpg.minTrustLevel=undefined",
            )
            verification = run_git(
                root,
                *scoped_config,
                "verify-commit",
                "--raw",
                publication,
                allowed=tuple(range(256)),
            )
            if verification.returncode != 0:
                detail = verification.stderr.decode("utf-8", errors="replace").strip() or "signature verification failed"
                raise SuccessorAuthorityError("V20_PUBLICATION_SIGNATURE_INVALID", detail)
            try:
                signature = run_git(
                    root,
                    *scoped_config,
                    "show",
                    "-s",
                    "--format=%G?%x00%GS%x00%GF",
                    publication,
                ).stdout.decode("utf-8", errors="strict").rstrip("\n")
            except UnicodeError as error:
                raise SuccessorAuthorityError("V20_PUBLICATION_SIGNER_INVALID", str(error), "BLOCKED") from error
    except SuccessorAuthorityError:
        raise
    except OSError as error:
        raise SuccessorAuthorityError("V20_TRUST_ANCHOR_UNAVAILABLE", str(error), "BLOCKED") from error
    parts = signature.split("\0")
    if len(parts) != 3:
        raise SuccessorAuthorityError("V20_PUBLICATION_SIGNER_INVALID", repr(signature))
    status, signer, fingerprint = parts
    if status != "G":
        raise SuccessorAuthorityError(
            "V20_PUBLICATION_SIGNATURE_STATUS_INVALID",
            f"expected='G' observed={status!r}",
        )
    if signer != TRUSTED_SSH_PRINCIPAL:
        raise SuccessorAuthorityError(
            "V20_PUBLICATION_SIGNER_MISMATCH",
            f"expected={TRUSTED_SSH_PRINCIPAL!r} observed={signer!r}",
        )
    if fingerprint != TRUSTED_SSH_FINGERPRINT:
        raise SuccessorAuthorityError(
            "V20_PUBLICATION_FINGERPRINT_MISMATCH",
            f"expected={TRUSTED_SSH_FINGERPRINT!r} observed={fingerprint!r}",
        )
    return {
        "status": status,
        "principal": signer,
        "fingerprint": fingerprint,
        "authorIdentity": author,
    }


def check_v19_at(root: Path, evaluated: str) -> tuple[dict[str, Any], str, bytes]:
    """Validate the one committed current V19 reachable from an entry candidate."""

    publication = locate_publication(root, evaluated, V19_PATH, None, "V20_V19_PUBLICATION_MISSING")
    if publication != APPROVED_V19_PUBLICATION:
        raise SuccessorAuthorityError(
            "V20_V19_PUBLICATION_COMMIT_MISMATCH",
            f"expected={APPROVED_V19_PUBLICATION} observed={publication}",
        )
    existing_bytes = candidate_blob(root, publication, V19_PATH, "V20_V19_AUTHORITY_MISSING")
    existing = parse_json(existing_bytes, "V20_V19_AUTHORITY_INVALID")
    fresh = existing.get("freshCheckpoint")
    if not isinstance(fresh, dict) or not isinstance(fresh.get("candidateCommit"), str):
        raise SuccessorAuthorityError("V20_V19_AUTHORITY_INVALID", "freshCheckpoint.candidateCommit")
    candidate = resolve_commit(root, fresh["candidateCommit"], "V20_V19_CANDIDATE_UNAVAILABLE")
    if candidate != APPROVED_CHECKPOINT_COMMIT:
        raise SuccessorAuthorityError(
            "V20_V19_CHECKPOINT_COMMIT_MISMATCH",
            f"expected={APPROVED_CHECKPOINT_COMMIT} observed={candidate}",
        )
    expected = render_v19(root, candidate)
    validate_json_schema(schema_at(root, candidate, V19_SCHEMA_PATH, "V20_V19_SCHEMA_MISSING"), expected, "V20_V19_SCHEMA_INVALID")
    if existing != expected or existing_bytes != json_bytes(expected):
        raise SuccessorAuthorityError("V20_V19_AUTHORITY_DRIFT", V19_PATH)
    validate_publication(
        root,
        source=candidate,
        publication=publication,
        evaluated=evaluated,
        path=V19_PATH,
        prefix="V20_V19",
    )
    if sha256(existing_bytes) != APPROVED_V19_RAW_SHA256:
        raise SuccessorAuthorityError("V20_V19_AUTHORITY_DIGEST_DRIFT", sha256(existing_bytes))
    return expected, publication, existing_bytes


def committed_binding(root: Path, entry: str, path: str, role: str) -> dict[str, str]:
    """Bind one exact committed regular input at the entry candidate."""

    mode, object_type, _ = raw_tree_record(root, entry, path, "V20_ENTRY_INPUT_MISSING")
    if mode != "100644" or object_type != "blob":
        raise SuccessorAuthorityError("V20_ENTRY_INPUT_MODE_DRIFT", f"{path}: {mode} {object_type}")
    content = candidate_blob(root, entry, path, "V20_ENTRY_INPUT_MISSING")
    return {"path": path, "sha256": sha256(content), "mode": mode, "role": role}


def render_v20(
    root: Path,
    *,
    entry_revision: str,
    owner_identity: str,
    decided_at_utc: str,
    rationale: str,
) -> dict[str, Any]:
    """Recompute the release-owner-authored, entry-candidate-bound V20 authority."""

    root = resolve_repository_root(root)
    entry = resolve_commit(root, entry_revision, "V20_ENTRY_CANDIDATE_UNAVAILABLE")
    if entry != APPROVED_ENTRY_COMMIT:
        raise SuccessorAuthorityError(
            "V20_ENTRY_CANDIDATE_MISMATCH",
            f"expected={APPROVED_ENTRY_COMMIT} observed={entry}",
        )
    require_exact_transaction(
        root,
        source=APPROVED_V19_PUBLICATION,
        candidate=entry,
        paths=(),
        prefix="V20_APPROVED_ENTRY",
    )
    decision_time = validate_owner_fields(owner_identity, decided_at_utc, rationale)
    v19, v19_publication, v19_bytes = check_v19_at(root, entry)
    require_ancestor(root, v19_publication, entry, "V20_ENTRY_NOT_AFTER_V19")
    if decision_time <= commit_time(root, entry, "V20_ENTRY_TIME_UNAVAILABLE"):
        raise SuccessorAuthorityError("V20_OWNER_DECISION_PREDATES_ENTRY", decided_at_utc)
    require_commit_path_absent(root, entry, FAILURE_SCHEMA_PATH, "V20_FAILURE_SCHEMA_ALREADY_COMMITTED")
    checkpoint = v19["freshCheckpoint"]["candidateCommit"]
    if len({PLANNING_CANDIDATE, checkpoint, entry}) != 3:
        raise SuccessorAuthorityError("V20_CANDIDATE_ROLE_CONFLATION", f"pc={PLANNING_CANDIDATE} checkpoint={checkpoint} entry={entry}")
    for binding in v19["freshCheckpoint"]["changedPathBindings"]:
        path = binding["path"]
        mode, object_type, _ = raw_tree_record(root, entry, path, "V20_CHECKPOINT_INPUT_MISSING")
        content = candidate_blob(root, entry, path, "V20_CHECKPOINT_INPUT_MISSING")
        if mode != binding["mode"] or object_type != "blob" or sha256(content) != binding["sha256"]:
            raise SuccessorAuthorityError("V20_CHECKPOINT_INPUT_DRIFT", path)
    checkpoint_owned = set(CHECKPOINT_PATHS[:-1])
    for path, _role, digest in CURRENT_INPUTS:
        if path in checkpoint_owned or path == SEMANTIC_SOURCE_PATH:
            continue
        mode, object_type, _ = raw_tree_record(root, entry, path, "V20_FIXED_ENTRY_INPUT_MISSING")
        content = candidate_blob(root, entry, path, "V20_FIXED_ENTRY_INPUT_MISSING")
        if mode != binding_mode(path) or object_type != "blob" or sha256(content) != digest:
            raise SuccessorAuthorityError("V20_FIXED_ENTRY_INPUT_DRIFT", path)
    inventory_content, inventory_schema_content = validate_committed_inventory(root, entry)
    semantic = candidate_blob(root, entry, SEMANTIC_SOURCE_PATH, "V20_SEMANTIC_SOURCE_NOT_COMMITTED")
    semantic_mode, semantic_type, _ = raw_tree_record(
        root, entry, SEMANTIC_SOURCE_PATH, "V20_SEMANTIC_SOURCE_NOT_COMMITTED"
    )
    if semantic_mode != "100644" or semantic_type != "blob" or sha256(semantic) != SEMANTIC_SOURCE_SHA256:
        raise SuccessorAuthorityError("V20_SEMANTIC_SOURCE_DRIFT", f"{semantic_mode} {sha256(semantic)}")
    entry_paths = (
        (V19_PATH, "checkpoint-completion-authority"),
        (V19_SCHEMA_PATH, "checkpoint-completion-authority-schema"),
        (V20_SCHEMA_PATH, "release-owner-authority-schema"),
        (INVENTORY_PATH, "frozen-input-inventory"),
        (INVENTORY_SCHEMA_PATH, "frozen-input-inventory-schema"),
        (PUBLISHER_PATH, "successor-authority-publisher"),
        (PUBLISHER_TEST_PATH, "successor-authority-tests"),
        (CORRECTION_PATH, "failed-prepublication-candidate-correction"),
        (SEMANTIC_SOURCE_PATH, "approved-semantic-source"),
    )
    entry_bindings = [committed_binding(root, entry, path, role) for path, role in entry_paths]
    if entry_bindings[0]["sha256"] != sha256(v19_bytes):
        raise SuccessorAuthorityError("V20_V19_BINDING_DRIFT", entry_bindings[0]["sha256"])
    ledger = [
        {"id": "V20-C1", "subject": "committed-current-v19-pass", "state": "PASS"},
        {"id": "V20-C2", "subject": "independent-owner-fields-and-exact-7.1-scope", "state": "PASS"},
        {"id": "V20-C3", "subject": "planning-checkpoint-entry-and-eventual-story-roles", "state": "PASS"},
        {"id": "V20-C4", "subject": "committed-inventory-schema-data-and-semantic-source", "state": "PASS"},
        {"id": "V20-C5", "subject": "exact-story-and-scenario-inventory-identities", "state": "PASS"},
        {"id": "V20-C6", "subject": "narrow-no-done-no-7.2-no-release-no-push-effect", "state": "PASS"},
        {"id": "V20-C7", "subject": "fail-closed-effective-hold-composition", "state": "PASS"},
    ]
    return {
        "schemaVersion": V20_SCHEMA_VERSION,
        "authorityId": V20_AUTHORITY_ID,
        "predecessor": {"authorityId": V19_AUTHORITY_ID, "path": V19_PATH, "sha256": sha256(v19_bytes)},
        "ownerDecision": {
            "owner": "Release owner",
            "identity": owner_identity.strip(),
            "decidedAtUtc": decided_at_utc,
            "rationale": rationale.strip(),
            "decision": "LIFTED",
        },
        "scope": {
            "unlocks": ["7.1"],
            "global": False,
            "checkpointReauthorized": False,
            "storyDoneAllowedWithoutAcceptance": False,
            "successorUnlocked": False,
        },
        "candidateRoles": {
            "planningCandidate": PLANNING_CANDIDATE,
            "checkpointCandidate": checkpoint,
            "entryCandidate": entry,
            "eventualStoryCandidate": "SC-7.1",
            "rolesAreInterchangeable": False,
            "scKnownAtDecisionTime": False,
            "scMustDescendFromEntry": True,
            "scMustBindCurrentRawGitlinks": True,
        },
        "storyWorkBaseline": {
            "resolution": "unique-single-parent-authority-publication",
            "parent": entry,
            "exactChangedPaths": [V20_PATH],
            "resolvedAtSc": True,
        },
        "authorityBundle": {
            "path": V9_BUNDLE_PATH,
            "rawSha256": V9_BUNDLE_RAW_SHA256,
            "bundleDigest": V9_BUNDLE_DIGEST,
        },
        "v11Authority": v19["v11Authority"],
        "frozenInputs": {
            "inventoryPath": INVENTORY_PATH,
            "inventorySha256": sha256(inventory_content),
            "schemaPath": INVENTORY_SCHEMA_PATH,
            "schemaSha256": sha256(inventory_schema_content),
        },
        "semanticSource": {
            "path": SEMANTIC_SOURCE_PATH,
            "sha256": SEMANTIC_SOURCE_SHA256,
            "mode": semantic_mode,
            "preservedUnchanged": True,
        },
        "entryBindings": entry_bindings,
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True},
        "assertionLedger": ledger,
        "result": "PASS",
        "authorityEffect": {
            "implementationHold": "LIFTED",
            "fullStoryExecutionAllowed": True,
            "storyDoneAllowedWithoutAcceptance": False,
            "story71CandidateBindingResolved": True,
            "story72Unlocked": False,
            "releaseAuthorized": False,
            "pushAuthorized": False,
        },
    }


def publish_v20(
    root: Path,
    *,
    entry_revision: str | None,
    owner_identity: str | None,
    decided_at_utc: str | None,
    rationale: str | None,
    check: bool,
    publication_revision: str | None = None,
) -> dict[str, Any]:
    """Write V20 or validate its exact-one-path committed publication."""

    root = resolve_repository_root(root)
    if check:
        evaluated = resolve_commit(root, entry_revision or "HEAD", "V20_EVALUATED_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated, V20_PATH, publication_revision, "V20_PUBLICATION_MISSING")
        if publication != APPROVED_V20_PUBLICATION:
            raise SuccessorAuthorityError(
                "V20_PUBLICATION_COMMIT_MISMATCH",
                f"expected={APPROVED_V20_PUBLICATION} observed={publication}",
            )
        existing_bytes = candidate_blob(root, publication, V20_PATH, "V20_AUTHORITY_MISSING")
        existing = parse_json(existing_bytes, "V20_AUTHORITY_INVALID")
        roles = existing.get("candidateRoles")
        owner = existing.get("ownerDecision")
        if not isinstance(roles, dict) or not isinstance(owner, dict):
            raise SuccessorAuthorityError("V20_AUTHORITY_INVALID", "candidateRoles or ownerDecision")
        entry_value = roles.get("entryCandidate")
        identity_value = owner.get("identity")
        decided_value = owner.get("decidedAtUtc")
        rationale_value = owner.get("rationale")
        if not all(isinstance(value, str) for value in (entry_value, identity_value, decided_value, rationale_value)):
            raise SuccessorAuthorityError("V20_AUTHORITY_INVALID", "owner or entry fields")
        entry = resolve_commit(root, entry_value, "V20_ENTRY_CANDIDATE_UNAVAILABLE")
        identity = identity_value
        decided = decided_value
        rationale_text = rationale_value
    else:
        if not all((entry_revision, owner_identity, decided_at_utc, rationale)):
            raise SuccessorAuthorityError(
                "V20_OWNER_INPUTS_REQUIRED",
                "--entry-candidate, --owner-identity, --decided-at-utc, and --rationale are required",
                "BLOCKED",
            )
        entry = resolve_commit(root, str(entry_revision), "V20_ENTRY_CANDIDATE_UNAVAILABLE")
        require_commit_path_absent(root, entry, V20_PATH, "V20_AUTHORITY_ALREADY_COMMITTED")
        identity = str(owner_identity)
        decided = str(decided_at_utc)
        rationale_text = str(rationale)
        existing = None
        existing_bytes = b""
    document = render_v20(
        root,
        entry_revision=entry,
        owner_identity=identity,
        decided_at_utc=decided,
        rationale=rationale_text,
    )
    validate_json_schema(schema_at(root, entry, V20_SCHEMA_PATH, "V20_SCHEMA_NOT_COMMITTED"), document, "V20_SCHEMA_INVALID")
    if check:
        decision_time = validate_owner_fields(identity, decided, rationale_text)
        publication_time = commit_time(root, publication, "V20_PUBLICATION_TIME_UNAVAILABLE")
        if decision_time > publication_time:
            raise SuccessorAuthorityError("V20_OWNER_DECISION_POSTDATES_PUBLICATION", decided)
        if existing != document or existing_bytes != json_bytes(document):
            raise SuccessorAuthorityError("V20_AUTHORITY_DRIFT", V20_PATH)
        validate_publication(
            root,
            source=entry,
            publication=publication,
            evaluated=evaluated,
            path=V20_PATH,
            prefix="V20",
        )
        verify_v20_publication_signature(root, publication, identity)
        if sha256(existing_bytes) != APPROVED_V20_RAW_SHA256:
            raise SuccessorAuthorityError("V20_AUTHORITY_DIGEST_DRIFT", sha256(existing_bytes))
        entry_v19 = candidate_blob(root, entry, V19_PATH, "V20_CURRENT_V19_MISSING")
        evaluated_v19 = candidate_blob(root, evaluated, V19_PATH, "V20_CURRENT_V19_MISSING")
        if evaluated_v19 != entry_v19:
            raise SuccessorAuthorityError("V20_CURRENT_V19_DRIFT", V19_PATH)
    else:
        write_atomic_no_clobber(
            root,
            V20_PATH,
            json_bytes(document),
            exists_code="V20_AUTHORITY_ALREADY_EXISTS",
            failure_code="V20_WRITE_FAILED",
        )
    return document


def yaml_mapping_entries(
    lines: list[str],
    indent: int,
    code: str,
) -> list[tuple[str, str, int]]:
    """Return one simple YAML mapping level while rejecting duplicate keys."""

    entries: list[tuple[str, str, int]] = []
    seen: set[str] = set()
    prefix = " " * indent
    pattern = re.compile(rf"{re.escape(prefix)}([A-Za-z0-9_-]+):(?: *(.*))?")
    for index, line in enumerate(lines):
        if "\t" in line:
            raise SuccessorAuthorityError(code, f"tab indentation at line {index + 1}")
        stripped = line.lstrip(" ")
        if not stripped or stripped.startswith("#") or len(line) - len(stripped) != indent:
            continue
        match = pattern.fullmatch(line)
        if match is None:
            raise SuccessorAuthorityError(code, f"invalid mapping entry at line {index + 1}")
        key = match.group(1)
        if key in seen:
            raise SuccessorAuthorityError(code, f"duplicate mapping key {key!r}")
        seen.add(key)
        entries.append((key, (match.group(2) or "").strip(), index))
    return entries


def yaml_entry_block(
    lines: list[str],
    entries: list[tuple[str, str, int]],
    key: str,
) -> list[str]:
    """Return the lines nested below one already validated mapping key."""

    position = next(index for index, entry in enumerate(entries) if entry[0] == key)
    start = entries[position][2] + 1
    end = len(lines) if position + 1 == len(entries) else entries[position + 1][2]
    return lines[start:end]


def significant_yaml_lines(lines: list[str]) -> list[str]:
    """Return nonblank, noncomment YAML lines."""

    return [line for line in lines if line.strip() and not line.lstrip().startswith("#")]


def validate_yaml_mapping_structure(lines: list[str], code: str) -> None:
    """Reject duplicate keys at every YAML mapping depth without a YAML dependency."""

    mapping_pattern = re.compile(r"([A-Za-z0-9_-]+):(?: *(.*))?")
    block_scalar_pattern = re.compile(r"[|>](?:(?:[+-][1-9]?)|(?:[1-9][+-]?))?")
    tokens: list[tuple[int, int, str]] = []
    block_scalar_indent: int | None = None
    for line_number, line in enumerate(lines, start=1):
        if "\t" in line:
            raise SuccessorAuthorityError(code, f"tab indentation at line {line_number}")
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        indent = len(line) - len(line.lstrip(" "))
        if block_scalar_indent is not None:
            if indent > block_scalar_indent:
                continue
            block_scalar_indent = None
        content = line[indent:]
        tokens.append((line_number, indent, content))
        candidate = content[1:].lstrip(" ") if content.startswith("-") else content
        match = mapping_pattern.fullmatch(candidate)
        if match is not None and block_scalar_pattern.fullmatch((match.group(2) or "").strip()):
            block_scalar_indent = indent

    def parse_entry(content: str, line_number: int) -> tuple[str, str]:
        match = mapping_pattern.fullmatch(content)
        if match is None:
            raise SuccessorAuthorityError(code, f"invalid mapping entry at line {line_number}")
        value = (match.group(2) or "").strip()
        if value.startswith("{"):
            raise SuccessorAuthorityError(code, f"flow mappings are not accepted at line {line_number}")
        return match.group(1), value

    def parse_child(index: int, parent_indent: int, value: str) -> int:
        if index < len(tokens) and tokens[index][1] > parent_indent:
            if value:
                raise SuccessorAuthorityError(
                    code,
                    f"unexpected nested content at line {tokens[index][0]}",
                )
            return parse_block(index, tokens[index][1])
        return index

    def parse_mapping(index: int, indent: int, seen: set[str] | None = None) -> int:
        keys = set() if seen is None else seen
        while index < len(tokens):
            line_number, observed_indent, content = tokens[index]
            if observed_indent < indent or (observed_indent == indent and content.startswith("-")):
                return index
            if observed_indent != indent:
                raise SuccessorAuthorityError(code, f"invalid indentation at line {line_number}")
            key, value = parse_entry(content, line_number)
            if key in keys:
                raise SuccessorAuthorityError(code, f"duplicate mapping key {key!r} at line {line_number}")
            keys.add(key)
            index += 1
            index = parse_child(index, indent, value)
        return index

    def parse_sequence(index: int, indent: int) -> int:
        while index < len(tokens):
            line_number, observed_indent, content = tokens[index]
            if observed_indent < indent or observed_indent == indent and not content.startswith("-"):
                return index
            if observed_indent != indent or not content.startswith("-"):
                raise SuccessorAuthorityError(code, f"invalid sequence indentation at line {line_number}")
            item = content[1:].lstrip(" ")
            index += 1
            if not item:
                index = parse_child(index, indent, "")
                continue
            if item.startswith(("[", "{")):
                raise SuccessorAuthorityError(code, f"flow collection steps are not accepted at line {line_number}")
            match = mapping_pattern.fullmatch(item)
            if match is None:
                raise SuccessorAuthorityError(code, f"sequence item must be a mapping at line {line_number}")
            key, value = parse_entry(item, line_number)
            item_indent = indent + 2
            index = parse_child(index, item_indent, value)
            if index < len(tokens) and tokens[index][1] == item_indent and not tokens[index][2].startswith("-"):
                index = parse_mapping(index, item_indent, {key})
            elif index < len(tokens) and tokens[index][1] > indent:
                raise SuccessorAuthorityError(code, f"invalid sequence mapping at line {tokens[index][0]}")
        return index

    def parse_block(index: int, indent: int) -> int:
        if tokens[index][2].startswith("-"):
            return parse_sequence(index, indent)
        return parse_mapping(index, indent)

    if tokens:
        final = parse_block(0, tokens[0][1])
        if final != len(tokens):
            raise SuccessorAuthorityError(code, f"unparsed YAML structure at line {tokens[final][0]}")


def yaml_step_model(lines: list[str], code: str) -> list[dict[str, Any]]:
    """Parse the approved Actions step subset into an order-stable semantic model."""

    starts = [index for index, line in enumerate(lines) if re.match(r"^ {6}- ", line)]
    before = lines[: starts[0]] if starts else lines
    if significant_yaml_lines(before):
        raise SuccessorAuthorityError(code, "steps must contain only mapping sequence items")
    models: list[dict[str, Any]] = []
    for position, start in enumerate(starts):
        end = starts[position + 1] if position + 1 < len(starts) else len(lines)
        block = lines[start:end]
        synthetic = ["        " + block[0][8:], *block[1:]]
        entries = yaml_mapping_entries(synthetic, 8, code)
        if not entries:
            raise SuccessorAuthorityError(code, f"empty step at line {start + 1}")
        model: dict[str, Any] = {}
        for key, value, _index in entries:
            children = yaml_entry_block(synthetic, entries, key)
            if key in {"with", "env"}:
                if value:
                    raise SuccessorAuthorityError(code, f"{key} must be a block mapping")
                child_entries = yaml_mapping_entries(children, 10, code)
                if len(significant_yaml_lines(children)) != len(child_entries):
                    raise SuccessorAuthorityError(code, f"nested {key} mapping is malformed")
                child_values = {child_key: child_value for child_key, child_value, _ in child_entries}
                model[key] = {child_key: child_values[child_key] for child_key in sorted(child_values)}
            elif key == "run" and value in {"|", ">-"}:
                script_lines: list[str] = []
                for child in children:
                    if not child.strip():
                        script_lines.append("")
                    elif child.startswith("          "):
                        script_lines.append(child[10:])
                    elif child.lstrip().startswith("#"):
                        continue
                    else:
                        raise SuccessorAuthorityError(code, "run block escaped its step mapping")
                while script_lines and not script_lines[-1]:
                    script_lines.pop()
                model[key] = {"style": value, "script": "\n".join(script_lines)}
            else:
                if significant_yaml_lines(children):
                    raise SuccessorAuthorityError(code, f"unexpected nested step key {key!r}")
                model[key] = value
        models.append({key: model[key] for key in sorted(model)})
    if not models:
        raise SuccessorAuthorityError(code, "workflow has no steps")
    return models


def preflight_workflow_model(content: bytes) -> dict[str, Any]:
    """Return the complete approved workflow subset as a canonical semantic model."""

    try:
        workflow = content.decode("utf-8", errors="strict")
    except UnicodeError as error:
        raise SuccessorAuthorityError("V21_PREFLIGHT_WIRING_INVALID", str(error)) from error
    lines = workflow.splitlines()
    try:
        validate_yaml_mapping_structure(lines, "V21_PREFLIGHT_WIRING_INVALID")
    except RecursionError as error:
        raise SuccessorAuthorityError(
            "V21_PREFLIGHT_WIRING_INVALID",
            "workflow structure exceeds the supported nesting depth",
        ) from error
    top_level = yaml_mapping_entries(lines, 0, "V21_PREFLIGHT_TRIGGER_INVALID")
    top_values = {key: value for key, value, _index in top_level}
    if set(top_values) != {"name", "on", "permissions", "jobs"}:
        raise SuccessorAuthorityError(
            "V21_PREFLIGHT_TRIGGER_INVALID",
            "only name/on/permissions/jobs top-level mappings are allowed",
        )
    if top_values["name"] != "Planning authority preflight":
        raise SuccessorAuthorityError("V21_PREFLIGHT_TRIGGER_INVALID", "workflow name drift")
    if top_values["on"] or top_values["permissions"] or top_values["jobs"]:
        raise SuccessorAuthorityError(
            "V21_PREFLIGHT_TRIGGER_INVALID",
            "on, permissions, and jobs must be block mappings",
        )

    trigger_lines = yaml_entry_block(lines, top_level, "on")
    triggers = yaml_mapping_entries(trigger_lines, 2, "V21_PREFLIGHT_TRIGGER_INVALID")
    trigger_values = {key: value for key, value, _index in triggers}
    if set(trigger_values) != {"pull_request", "push", "workflow_dispatch"} or any(trigger_values.values()):
        raise SuccessorAuthorityError("V21_PREFLIGHT_TRIGGER_INVALID", "required top-level triggers are not active")
    for trigger in ("pull_request", "workflow_dispatch"):
        if significant_yaml_lines(yaml_entry_block(trigger_lines, triggers, trigger)):
            raise SuccessorAuthorityError("V21_PREFLIGHT_TRIGGER_INVALID", f"unexpected {trigger} configuration")
    push_lines = yaml_entry_block(trigger_lines, triggers, "push")
    push_entries = yaml_mapping_entries(push_lines, 4, "V21_PREFLIGHT_TRIGGER_INVALID")
    push = {key: value for key, value, _index in push_entries}
    if push != {"branches": "[main]"}:
        raise SuccessorAuthorityError("V21_PREFLIGHT_TRIGGER_INVALID", "push must target only main")

    permission_lines = yaml_entry_block(lines, top_level, "permissions")
    permissions = {
        key: value
        for key, value, _index in yaml_mapping_entries(
            permission_lines,
            2,
            "V21_PREFLIGHT_PERMISSION_INVALID",
        )
    }
    if permissions != {"contents": "read"}:
        raise SuccessorAuthorityError(
            "V21_PREFLIGHT_PERMISSION_INVALID",
            "top-level permissions must be exactly contents: read",
        )

    jobs_lines = yaml_entry_block(lines, top_level, "jobs")
    jobs = yaml_mapping_entries(jobs_lines, 2, "V21_PREFLIGHT_JOB_INVALID")
    if [entry[0] for entry in jobs] != ["planning-authority"]:
        raise SuccessorAuthorityError("V21_PREFLIGHT_JOB_INVALID", "planning-authority must be the sole job")
    job_lines = yaml_entry_block(jobs_lines, jobs, "planning-authority")
    job_entries = yaml_mapping_entries(job_lines, 4, "V21_PREFLIGHT_JOB_INVALID")
    job_values = {key: value for key, value, _index in job_entries}
    if job_values != {"runs-on": "ubuntu-latest", "timeout-minutes": "30", "steps": ""}:
        raise SuccessorAuthorityError("V21_PREFLIGHT_JOB_INVALID", "planning-authority job enclosure drift")
    steps = yaml_step_model(
        yaml_entry_block(job_lines, job_entries, "steps"),
        "V21_PREFLIGHT_WIRING_INVALID",
    )
    return {
        "name": top_values["name"],
        "on": {"pull_request": {}, "push": push, "workflow_dispatch": {}},
        "permissions": permissions,
        "jobs": {
            "planning-authority": {
                "runs-on": job_values["runs-on"],
                "timeout-minutes": job_values["timeout-minutes"],
                "steps": steps,
            }
        },
    }


def validate_preflight_workflow(content: bytes) -> None:
    """Require the exact approved, fail-closed workflow semantic enclosure."""

    model = preflight_workflow_model(content)
    observed = sha256(json_bytes(model))
    if observed != PREFLIGHT_MODEL_SHA256:
        raise SuccessorAuthorityError(
            "V21_PREFLIGHT_WIRING_INVALID",
            f"expected model={PREFLIGHT_MODEL_SHA256} observed={observed}",
        )
    steps = model["jobs"]["planning-authority"]["steps"]
    names = [step.get("name") for step in steps]
    if names[:2] != [
        "Check out exact candidate",
        "Bootstrap Story 7.1 successor authority boundary",
    ]:
        raise SuccessorAuthorityError(
            "V21_PREFLIGHT_ORDER_INVALID",
            "the bootstrap must be the first post-checkout operation",
        )


def validate_accepted_lineage(root: Path) -> None:
    """Validate the complete fixed baseline-through-signed-V20 topology."""

    for commit, code in (
        (APPROVED_BASELINE_COMMIT, "V21_BASELINE_UNAVAILABLE"),
        (APPROVED_TOOLING_COMMIT, "V21_V19_TOOLING_UNAVAILABLE"),
        (APPROVED_CHECKPOINT_COMMIT, "V21_CHECKPOINT_UNAVAILABLE"),
        (APPROVED_V19_PUBLICATION, "V21_V19_PUBLICATION_UNAVAILABLE"),
        (APPROVED_ENTRY_COMMIT, "V21_ENTRY_UNAVAILABLE"),
        (APPROVED_V20_PUBLICATION, "V21_V20_PUBLICATION_UNAVAILABLE"),
    ):
        resolve_commit(root, commit, code)
    validate_accepted_checkpoint(root, APPROVED_CHECKPOINT_COMMIT)
    require_exact_transaction(
        root,
        source=APPROVED_CHECKPOINT_COMMIT,
        candidate=APPROVED_V19_PUBLICATION,
        paths=(V19_PATH,),
        prefix="V21_V19_PUBLICATION",
    )
    require_exact_transaction(
        root,
        source=APPROVED_V19_PUBLICATION,
        candidate=APPROVED_ENTRY_COMMIT,
        paths=(),
        prefix="V21_ENTRY",
    )
    require_exact_transaction(
        root,
        source=APPROVED_ENTRY_COMMIT,
        candidate=APPROVED_V20_PUBLICATION,
        paths=(V20_PATH,),
        prefix="V21_V20_PUBLICATION",
    )
    v19_bytes = candidate_blob(root, APPROVED_V19_PUBLICATION, V19_PATH, "V21_V19_AUTHORITY_MISSING")
    if sha256(v19_bytes) != APPROVED_V19_RAW_SHA256:
        raise SuccessorAuthorityError("V21_V19_AUTHORITY_DIGEST_DRIFT", sha256(v19_bytes))
    v20_bytes = candidate_blob(root, APPROVED_V20_PUBLICATION, V20_PATH, "V21_V20_AUTHORITY_MISSING")
    if sha256(v20_bytes) != APPROVED_V20_RAW_SHA256:
        raise SuccessorAuthorityError("V21_V20_AUTHORITY_DIGEST_DRIFT", sha256(v20_bytes))


def expected_v21_document(
    root: Path,
    tooling: str,
    signature: dict[str, str] | None = None,
) -> dict[str, Any]:
    """Build the one canonically ordered complete V21 authority document."""

    verified_signature = signature or verify_v20_publication_signature(
        root,
        APPROVED_V20_PUBLICATION,
        TRUSTED_OWNER_IDENTITY,
    )
    expected_lineage = {
        "baselineCommit": APPROVED_BASELINE_COMMIT,
        "v19ToolingCommit": APPROVED_TOOLING_COMMIT,
        "checkpointCommit": APPROVED_CHECKPOINT_COMMIT,
        "v19PublicationCommit": APPROVED_V19_PUBLICATION,
        "entryCommit": APPROVED_ENTRY_COMMIT,
        "v20PublicationCommit": APPROVED_V20_PUBLICATION,
    }
    roles = {
        PREFLIGHT_PATH: "planning-authority-preflight",
        V21_SPEC_PATH: "approved-v21-correction-specification",
        V21_SCHEMA_PATH: "v21-authority-correction-schema",
        PUBLISHER_PATH: "successor-authority-publisher",
        PUBLISHER_TEST_PATH: "successor-authority-tests",
    }
    expected_tooling = {
        "parentCommit": APPROVED_V20_PUBLICATION,
        "commit": tooling,
        "exactChangedPaths": list(V21_TOOLING_PATHS),
        "changedGitlinkPaths": [],
        "changedPathBindings": [committed_binding(root, tooling, path, roles[path]) for path in V21_TOOLING_PATHS],
    }
    expected_effect = {
        "implementationHold": "LIFTED",
        "unlocks": ["7.1"],
        "global": False,
        "fullStoryExecutionAllowed": True,
        "ownerDecisionReplaced": False,
        "storyDoneAllowedWithoutAcceptance": False,
        "story72Unlocked": False,
        "releaseAuthorized": False,
        "pushAuthorized": False,
    }
    return {
        "schemaVersion": V21_SCHEMA_VERSION,
        "authorityId": V21_AUTHORITY_ID,
        "predecessor": {
            "authorityId": V20_AUTHORITY_ID,
            "path": V20_PATH,
            "sha256": APPROVED_V20_RAW_SHA256,
        },
        "acceptedLineage": expected_lineage,
        "verifiedV20": {
            "publicationCommit": APPROVED_V20_PUBLICATION,
            "path": V20_PATH,
            "rawSha256": APPROVED_V20_RAW_SHA256,
            "signature": verified_signature,
        },
        "hardenedTooling": expected_tooling,
        "resultSemantics": {"states": list(RESULT_STATES), "ledgerRequired": True},
        "assertionLedger": [
            {"id": f"V21-C{index}", "subject": subject, "state": "PASS"}
            for index, subject in enumerate(V21_ASSERTION_SUBJECTS, start=1)
        ],
        "result": "PASS",
        "authorityEffect": expected_effect,
    }


def validate_bootstrap_v21_document(root: Path, tooling: str, publication: str, content: bytes) -> None:
    """Validate the dependency-free V21 trust facts needed before package execution."""

    document = parse_json(content, "V21_AUTHORITY_INVALID")
    expected = expected_v21_document(root, tooling)
    if document != expected or content != json_bytes(expected):
        raise SuccessorAuthorityError("V21_AUTHORITY_DRIFT", f"complete canonical document@{publication}")


def validate_source_workflow_identity(
    root: Path,
    *,
    source_repository: str | None,
    source_workflow_ref: str | None,
    source_workflow_sha: str | None,
) -> str | None:
    """Validate the protected job workflow identity and its immutable source blobs."""

    supplied = (source_repository, source_workflow_ref, source_workflow_sha)
    if all(value is None for value in supplied):
        return None
    if any(value is None for value in supplied):
        raise SuccessorAuthorityError(
            "V21_SOURCE_WORKFLOW_IDENTITY_INVALID",
            "source repository, workflow ref, and workflow SHA must be supplied together",
            "BLOCKED",
        )
    if source_repository != CI_RULESET_SOURCE_REPOSITORY:
        raise SuccessorAuthorityError(
            "V21_SOURCE_WORKFLOW_IDENTITY_INVALID",
            f"sourceRepository={source_repository!r}",
            "BLOCKED",
        )
    if source_workflow_ref != CI_RULESET_SOURCE_WORKFLOW_REF:
        raise SuccessorAuthorityError(
            "V21_SOURCE_WORKFLOW_IDENTITY_INVALID",
            f"workflowRef={source_workflow_ref!r}",
            "BLOCKED",
        )
    if source_workflow_sha is None or re.fullmatch(r"[0-9a-f]{40}", source_workflow_sha) is None:
        raise SuccessorAuthorityError(
            "V21_SOURCE_WORKFLOW_IDENTITY_INVALID",
            f"workflowSha={source_workflow_sha!r}",
            "BLOCKED",
        )
    source = resolve_commit(root, source_workflow_sha, "V21_SOURCE_WORKFLOW_SHA_UNAVAILABLE")
    if source != source_workflow_sha:
        raise SuccessorAuthorityError(
            "V21_SOURCE_WORKFLOW_IDENTITY_INVALID",
            f"workflowSha={source_workflow_sha!r} resolved={source!r}",
            "BLOCKED",
        )
    for path in (PREFLIGHT_PATH, PUBLISHER_PATH):
        mode, object_type, _object_id = raw_tree_record(
            root,
            source,
            path,
            "V21_SOURCE_WORKFLOW_BLOB_INVALID",
        )
        if mode != "100644" or object_type != "blob":
            raise SuccessorAuthorityError(
                "V21_SOURCE_WORKFLOW_BLOB_INVALID",
                f"{path}@{source}: {mode} {object_type}",
                "BLOCKED",
            )
    return source


def compare_source_and_candidate_tooling(root: Path, source: str, tooling: str) -> None:
    """Require all five candidate tooling blobs and modes to equal protected-source bytes."""

    for path in V21_TOOLING_PATHS:
        source_record = raw_tree_record(root, source, path, "V21_SOURCE_TOOLING_MISMATCH")
        candidate_record = raw_tree_record(root, tooling, path, "V21_SOURCE_TOOLING_MISMATCH")
        if source_record[:2] != ("100644", "blob") or candidate_record[:2] != ("100644", "blob"):
            raise SuccessorAuthorityError(
                "V21_SOURCE_TOOLING_MISMATCH",
                f"{path}: source={source_record[:2]!r} candidate={candidate_record[:2]!r}",
                "BLOCKED",
            )
        if source_record != candidate_record:
            raise SuccessorAuthorityError(
                "V21_SOURCE_TOOLING_MISMATCH",
                f"{path}: source={source_record!r} candidate={candidate_record!r}",
                "BLOCKED",
            )


def bootstrap_successor_boundary(
    root: Path,
    candidate_revision: str | None = None,
    *,
    source_repository: str | None = None,
    source_workflow_ref: str | None = None,
    source_workflow_sha: str | None = None,
) -> dict[str, Any]:
    """Validate immutable successor trust and workflow bytes without third-party packages."""

    root = resolve_repository_root(root)
    evaluated = resolve_commit(root, candidate_revision or "HEAD", "V21_BOOTSTRAP_CANDIDATE_UNAVAILABLE")
    source = validate_source_workflow_identity(
        root,
        source_repository=source_repository,
        source_workflow_ref=source_workflow_ref,
        source_workflow_sha=source_workflow_sha,
    )
    require_complete_history(root)
    validate_accepted_lineage(root)
    verify_v20_publication_signature(root, APPROVED_V20_PUBLICATION, TRUSTED_OWNER_IDENTITY)
    _counts, checkpoint_ledger = parse_junit(
        candidate_blob(
            root,
            APPROVED_CHECKPOINT_COMMIT,
            CHECKPOINT_RESULT_PATH,
            "V19_APPROVED_CHECKPOINT_PATH_MISSING",
        )
    )
    if tuple(row["subject"] for row in checkpoint_ledger) != REQUIRED_CHECKPOINT_SUBJECTS:
        raise SuccessorAuthorityError("V19_RESULT_LEDGER_INSUFFICIENT", repr(checkpoint_ledger))
    publications = publication_candidates(root, evaluated, V21_PATH, "V21_BOOTSTRAP_HISTORY_UNAVAILABLE")
    historical_stage = evaluated in {
        APPROVED_V19_PUBLICATION,
        APPROVED_ENTRY_COMMIT,
        APPROVED_V20_PUBLICATION,
    }
    if not publications:
        tooling = evaluated
        if historical_stage:
            # The fixed accepted-lineage, raw-authority digests, signed V20, and
            # seven-subject checkpoint checks above authenticate these immutable
            # dependency-free lifecycle points. Package-backed full checks run later.
            pass
        else:
            require_exact_transaction(
                root,
                source=APPROVED_V20_PUBLICATION,
                candidate=tooling,
                paths=V21_TOOLING_PATHS,
                prefix="V21_TOOLING",
            )
            require_commit_path_absent(root, tooling, V21_PATH, "V21_AUTHORITY_ALREADY_COMMITTED")
        state = "ACTIVE"
        publication: str | None = None
    elif len(publications) == 1:
        publication = publications[0]
        parents = commit_parents(root, publication, "V21_PUBLICATION_PARENT_MISMATCH")
        if len(parents) != 1:
            raise SuccessorAuthorityError("V21_PUBLICATION_PARENT_MISMATCH", repr(parents))
        tooling = parents[0]
        require_exact_transaction(
            root,
            source=APPROVED_V20_PUBLICATION,
            candidate=tooling,
            paths=V21_TOOLING_PATHS,
            prefix="V21_TOOLING",
        )
        published = validate_publication(
            root,
            source=tooling,
            publication=publication,
            evaluated=evaluated,
            path=V21_PATH,
            prefix="V21",
        )
        validate_bootstrap_v21_document(root, tooling, publication, published)
        descendant_paths, descendant_gitlinks = descendant_history_changes(root, publication, evaluated)
        if descendant_gitlinks:
            raise SuccessorAuthorityError("V21_DESCENDANT_GITLINK_DRIFT", repr(descendant_gitlinks))
        unauthorized = tuple(
            (commit, path)
            for commit, path in descendant_paths
            if path not in STORY_DESCENDANT_PATHS
        )
        if unauthorized:
            raise SuccessorAuthorityError("V21_DESCENDANT_PATH_DRIFT", repr(unauthorized))
        state = "LIFTED"
    else:
        raise SuccessorAuthorityError(
            "V21_PUBLICATION_MISSING",
            f"expected at most one publication; observed={publications!r}",
            "BLOCKED",
        )
    if source is not None:
        if not historical_stage:
            compare_source_and_candidate_tooling(root, source, tooling)
        workflow = candidate_blob(root, source, PREFLIGHT_PATH, "V21_SOURCE_WORKFLOW_BLOB_INVALID")
        validate_preflight_workflow(workflow)
    elif not historical_stage:
        workflow = candidate_blob(root, tooling, PREFLIGHT_PATH, "V21_PREFLIGHT_MISSING")
        validate_preflight_workflow(workflow)
    return {
        "schemaVersion": "hexalith.conversations.story-7.1-successor-bootstrap-result.v1",
        "route": "bootstrap",
        "result": "PASS",
        "candidate": evaluated,
        "tooling": tooling,
        "publication": publication,
        "effectiveHold": state,
        "assertionLedger": [
            {"id": "BOOTSTRAP-C1", "subject": "complete immutable successor history", "state": "PASS"},
            {"id": "BOOTSTRAP-C2", "subject": "exact V20/tooling/V21 transaction", "state": "PASS"},
            {"id": "BOOTSTRAP-C3", "subject": "candidate protected paths and gitlinks", "state": "PASS"},
            {"id": "BOOTSTRAP-C4", "subject": "exact workflow enclosure", "state": "PASS"},
        ],
        "blockers": [],
    }


def render_v21(root: Path, candidate_revision: str) -> dict[str, Any]:
    """Recompute the machine-derived V21 correction from hardened tooling."""

    root = resolve_repository_root(root)
    tooling = resolve_commit(root, candidate_revision, "V21_TOOLING_CANDIDATE_UNAVAILABLE")
    validate_accepted_lineage(root)
    require_exact_transaction(
        root,
        source=APPROVED_V20_PUBLICATION,
        candidate=tooling,
        paths=V21_TOOLING_PATHS,
        prefix="V21_TOOLING",
    )
    require_commit_path_absent(root, tooling, V21_PATH, "V21_AUTHORITY_ALREADY_COMMITTED")
    schema_at(root, tooling, V21_SCHEMA_PATH, "V21_SCHEMA_NOT_COMMITTED")
    workflow = candidate_blob(root, tooling, PREFLIGHT_PATH, "V21_PREFLIGHT_MISSING")
    validate_preflight_workflow(workflow)
    v20 = publish_v20(
        root,
        entry_revision=tooling,
        owner_identity=None,
        decided_at_utc=None,
        rationale=None,
        check=True,
    )
    signature = verify_v20_publication_signature(
        root,
        APPROVED_V20_PUBLICATION,
        str(v20["ownerDecision"]["identity"]),
    )
    inventory_ledger: list[dict[str, str]] = []
    inventory_route(
        root,
        check=True,
        evaluated_revision=tooling,
        assertion_ledger=inventory_ledger,
    )
    if not inventory_ledger:
        raise SuccessorAuthorityError("V21_INVENTORY_LEDGER_EMPTY", tooling, "BLOCKED")
    return expected_v21_document(root, tooling, signature)


def publish_v21(
    root: Path,
    *,
    candidate_revision: str | None,
    check: bool,
    publication_revision: str | None = None,
) -> dict[str, Any]:
    """Write V21 or validate its direct-child exact-one-path publication."""

    root = resolve_repository_root(root)
    if check:
        evaluated = resolve_commit(root, candidate_revision or "HEAD", "V21_EVALUATED_CANDIDATE_UNAVAILABLE")
        publication = locate_publication(root, evaluated, V21_PATH, publication_revision, "V21_PUBLICATION_MISSING")
        existing_bytes = candidate_blob(root, publication, V21_PATH, "V21_AUTHORITY_MISSING")
        existing = parse_json(existing_bytes, "V21_AUTHORITY_INVALID")
        tooling_section = existing.get("hardenedTooling")
        if not isinstance(tooling_section, dict) or not isinstance(tooling_section.get("commit"), str):
            raise SuccessorAuthorityError("V21_AUTHORITY_INVALID", "hardenedTooling.commit")
        tooling = resolve_commit(root, tooling_section["commit"], "V21_TOOLING_CANDIDATE_UNAVAILABLE")
    else:
        tooling = resolve_commit(root, candidate_revision or "HEAD", "V21_TOOLING_CANDIDATE_UNAVAILABLE")
        require_commit_path_absent(root, tooling, V21_PATH, "V21_AUTHORITY_ALREADY_COMMITTED")
        existing_bytes = b""
        existing = None
    document = render_v21(root, tooling)
    validate_json_schema(
        schema_at(root, tooling, V21_SCHEMA_PATH, "V21_SCHEMA_NOT_COMMITTED"),
        document,
        "V21_SCHEMA_INVALID",
    )
    if check:
        if existing != document or existing_bytes != json_bytes(document):
            raise SuccessorAuthorityError("V21_AUTHORITY_DRIFT", V21_PATH)
        validate_publication(
            root,
            source=tooling,
            publication=publication,
            evaluated=evaluated,
            path=V21_PATH,
            prefix="V21",
        )
        descendant_paths, descendant_gitlinks = descendant_history_changes(root, publication, evaluated)
        if descendant_gitlinks:
            raise SuccessorAuthorityError("V21_DESCENDANT_GITLINK_DRIFT", repr(descendant_gitlinks))
        unauthorized = tuple(
            (commit, path)
            for commit, path in descendant_paths
            if path not in STORY_DESCENDANT_PATHS
        )
        if unauthorized:
            raise SuccessorAuthorityError("V21_DESCENDANT_PATH_DRIFT", repr(unauthorized))
    else:
        write_atomic_no_clobber(
            root,
            V21_PATH,
            json_bytes(document),
            exists_code="V21_AUTHORITY_ALREADY_EXISTS",
            failure_code="V21_WRITE_FAILED",
        )
    return document


def lifecycle_artifact_present(
    root: Path,
    evaluated: str,
    path: str,
    missing_code: str,
    absence_id: str,
    ledger: list[dict[str, Any]],
) -> bool:
    """Distinguish true prepublication absence from removal after publication."""

    if run_git(root, "ls-tree", "-z", evaluated, "--", safe_path(path)).stdout:
        return True
    publications = publication_candidates(
        root,
        evaluated,
        path,
        f"{missing_code}_HISTORY_UNAVAILABLE",
    )
    if publications:
        raise SuccessorAuthorityError(
            missing_code,
            f"published={publications!r} missing={path}@{evaluated}",
            "BLOCKED",
        )
    ledger.append(
        {
            "id": absence_id,
            "subject": path,
            "state": "not-applicable",
        }
    )
    return False


def effective_hold(root: Path, *, evaluated_revision: str | None = None) -> dict[str, Any]:
    """Evaluate the lifecycle-aware hold; only a valid V21 may lift it."""

    try:
        root = resolve_repository_root(root)
        evaluated = resolve_commit(root, evaluated_revision or "HEAD", "V21_EVALUATED_CANDIDATE_UNAVAILABLE")
        require_complete_history(root)
        try:
            require_ancestor(
                root,
                APPROVED_BASELINE_COMMIT,
                evaluated,
                "V21_LIFECYCLE_LINEAGE_INVALID",
            )
        except SuccessorAuthorityError as error:
            raise SuccessorAuthorityError(
                "V21_LIFECYCLE_LINEAGE_INVALID",
                error.detail,
                "BLOCKED",
            ) from error
        ledger: list[dict[str, Any]] = []
        expected_absences: list[dict[str, Any]] = []
        inventory_present = lifecycle_artifact_present(
            root,
            evaluated,
            INVENTORY_PATH,
            "V20_INVENTORY_DESCENDANT_MISSING",
            "LIFECYCLE-INVENTORY-EXPECTED-ABSENCE",
            expected_absences,
        )
        v19_present = lifecycle_artifact_present(
            root,
            evaluated,
            V19_PATH,
            "V19_AUTHORITY_DESCENDANT_MISSING",
            "LIFECYCLE-V19-EXPECTED-ABSENCE",
            expected_absences,
        )
        v20_present = lifecycle_artifact_present(
            root,
            evaluated,
            V20_PATH,
            "V20_AUTHORITY_DESCENDANT_MISSING",
            "LIFECYCLE-V20-EXPECTED-ABSENCE",
            expected_absences,
        )
        v21_present = lifecycle_artifact_present(
            root,
            evaluated,
            V21_PATH,
            "V21_AUTHORITY_DESCENDANT_MISSING",
            "LIFECYCLE-V21-EXPECTED-ABSENCE",
            expected_absences,
        )
        if inventory_present:
            inventory_ledger: list[dict[str, str]] = []
            inventory_route(
                root,
                check=True,
                evaluated_revision=evaluated,
                assertion_ledger=inventory_ledger,
            )
            ledger.append({"id": "LIFECYCLE-INVENTORY", "subject": INVENTORY_PATH, "state": "PASS"})
        if v19_present:
            publish_v19(root, candidate_revision=evaluated, check=True)
            ledger.append({"id": "LIFECYCLE-V19", "subject": V19_PATH, "state": "PASS"})
        if v20_present:
            publish_v20(
                root,
                entry_revision=evaluated,
                owner_identity=None,
                decided_at_utc=None,
                rationale=None,
                check=True,
            )
            ledger.append({"id": "LIFECYCLE-V20", "subject": V20_PATH, "state": "PASS"})
        if not v21_present:
            ledger.extend(expected_absences)
            return {
                "schemaVersion": "hexalith.conversations.story-7.1-effective-hold-result.v1",
                "result": "PASS",
                "effectiveHold": "ACTIVE",
                "assertionLedger": ledger,
                "blockers": [],
            }
        authority = publish_v21(root, candidate_revision=evaluated, check=True)
        ledger.extend(authority["assertionLedger"])
    except SuccessorAuthorityError as error:
        return {
            "schemaVersion": "hexalith.conversations.story-7.1-effective-hold-result.v1",
            "result": error.state,
            "effectiveHold": "ACTIVE",
            "assertionLedger": [
                {"id": error.code, "subject": "Story 7.1 successor authority", "state": error.state, "detail": error.detail}
            ],
            "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
        }
    return {
        "schemaVersion": "hexalith.conversations.story-7.1-effective-hold-result.v1",
        "result": "PASS",
        "effectiveHold": authority["authorityEffect"]["implementationHold"],
        "assertionLedger": ledger,
        "blockers": [],
    }


def failure_document(root: Path, route: str, error: SuccessorAuthorityError) -> dict[str, Any]:
    """Return a parseable nonvacuous failure result with stable state semantics."""

    try:
        repository = str(root.resolve())
    except (OSError, RuntimeError):
        repository = str(root)
    result = {
        "schemaVersion": "hexalith.conversations.story-7.1-successor-authority-result.v1",
        "route": route,
        "result": error.state,
        "repository": repository,
        "resultSemantics": list(RESULT_STATES),
        "assertionLedger": [
            {"id": error.code, "subject": f"Story 7.1 {route}", "state": error.state, "detail": error.detail}
        ],
        "blockers": [{"code": error.code, "state": error.state, "detail": error.detail}],
    }
    if route in {"bootstrap", "v20", "v21"}:
        result["effectiveHold"] = "ACTIVE"
    return result


def ci_trust_boundary_result(
    *,
    rulesets_content: bytes | None = None,
    source_repository: str | None = None,
    source_workflow_ref: str | None = None,
    source_workflow_sha: str | None = None,
    source_repository_id: int | None = None,
) -> dict[str, Any]:
    """Validate externally administered effective organization-ruleset evidence."""

    base = {
        "schemaVersion": "hexalith.conversations.story-7.1-ci-trust-result.v1",
        "sourceRepository": CI_RULESET_SOURCE_REPOSITORY,
        "sourceRef": CI_RULESET_SOURCE_REF,
        "workflowPath": CI_RULESET_SOURCE_WORKFLOW,
    }

    def blocked(detail: str) -> dict[str, Any]:
        row = {
            "id": CI_RULESET_BLOCKER,
            "subject": "organization required-workflow ruleset activation",
            "state": "BLOCKED",
            "detail": detail,
        }
        return {**base, "result": "BLOCKED", "assertionLedger": [row], "blockers": [
            {"code": CI_RULESET_BLOCKER, "state": "BLOCKED", "detail": detail}
        ]}

    requirement = (
        "requires an active organization ruleset selecting "
        f"{CI_RULESET_SOURCE_WORKFLOW_REF} with no bypass actors and direct pushes blocked"
    )
    if rulesets_content is None:
        return blocked(requirement)
    if (
        source_repository != CI_RULESET_SOURCE_REPOSITORY
        or source_workflow_ref != CI_RULESET_SOURCE_WORKFLOW_REF
        or source_workflow_sha is None
        or re.fullmatch(r"[0-9a-f]{40}", source_workflow_sha) is None
        or not isinstance(source_repository_id, int)
        or isinstance(source_repository_id, bool)
        or source_repository_id <= 0
    ):
        return blocked(
            "protected job workflow identity or source repository id is absent, malformed, or mismatched"
        )
    try:
        rulesets = json.loads(rulesets_content.decode("utf-8", errors="strict"))
    except (UnicodeError, json.JSONDecodeError, RecursionError) as error:
        return blocked(f"effective ruleset evidence is unreadable: {error}")
    if not isinstance(rulesets, list) or not rulesets:
        return blocked("effective ruleset evidence is absent or is not a nonempty list")

    matched_ruleset: dict[str, Any] | None = None
    for candidate in rulesets:
        if not isinstance(candidate, dict):
            continue
        conditions = candidate.get("conditions")
        references = conditions.get("ref_name") if isinstance(conditions, dict) else None
        included = references.get("include") if isinstance(references, dict) else None
        excluded = references.get("exclude") if isinstance(references, dict) else None
        if (
            candidate.get("source_type") != "Organization"
            or candidate.get("enforcement") != "active"
            or candidate.get("target") != "branch"
            or candidate.get("bypass_actors") != []
            or not isinstance(included, list)
            or not all(isinstance(value, str) for value in included)
            or not isinstance(excluded, list)
            or excluded
            or not ({"~DEFAULT_BRANCH", CI_RULESET_SOURCE_REF} & set(included))
        ):
            continue
        rules = candidate.get("rules")
        if not isinstance(rules, list):
            continue
        direct_push_blocked = any(
            isinstance(rule, dict) and rule.get("type") == "pull_request" for rule in rules
        )
        workflow_selected = False
        for rule in rules:
            if not isinstance(rule, dict) or rule.get("type") != "workflows":
                continue
            parameters = rule.get("parameters")
            workflows = parameters.get("workflows") if isinstance(parameters, dict) else None
            if not isinstance(workflows, list):
                continue
            for workflow in workflows:
                if not isinstance(workflow, dict):
                    continue
                configured_sha = workflow.get("sha")
                if (
                    workflow.get("path") == CI_RULESET_SOURCE_WORKFLOW
                    and workflow.get("ref") == CI_RULESET_SOURCE_REF
                    and workflow.get("repository_id") == source_repository_id
                    and (configured_sha is None or configured_sha == source_workflow_sha)
                ):
                    workflow_selected = True
        if workflow_selected and direct_push_blocked:
            matched_ruleset = candidate
            break
    if matched_ruleset is None:
        return blocked(
            "no effective active organization ruleset proves the exact workflow, empty bypass, "
            "default-branch scope, and pull-request-only update boundary"
        )
    ruleset_id = matched_ruleset.get("id")
    ledger = [
        {"id": "CI-TRUST-C1", "subject": "protected job workflow identity", "state": "PASS"},
        {"id": "CI-TRUST-C2", "subject": f"active organization ruleset {ruleset_id}", "state": "PASS"},
        {"id": "CI-TRUST-C3", "subject": "exact required workflow source/ref/path", "state": "PASS"},
        {"id": "CI-TRUST-C4", "subject": "empty bypass and direct-push protection", "state": "PASS"},
    ]
    return {**base, "result": "PASS", "assertionLedger": ledger, "blockers": []}


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the inventory, V19, V20, or V21 publication/check route."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    routes = parser.add_subparsers(dest="route", required=True)
    bootstrap_parser = routes.add_parser("bootstrap")
    bootstrap_parser.add_argument("--candidate")
    bootstrap_parser.add_argument("--source-workflow-repository")
    bootstrap_parser.add_argument("--source-workflow-ref")
    bootstrap_parser.add_argument("--source-workflow-sha")
    ci_trust_parser = routes.add_parser("ci-trust")
    ci_trust_parser.add_argument("--rulesets-file")
    ci_trust_parser.add_argument("--source-workflow-repository")
    ci_trust_parser.add_argument("--source-workflow-ref")
    ci_trust_parser.add_argument("--source-workflow-sha")
    ci_trust_parser.add_argument("--source-repository-id", type=int)
    inventory_parser = routes.add_parser("inventory")
    inventory_parser.add_argument("--check", action="store_true")
    v19_parser = routes.add_parser("v19")
    v19_parser.add_argument("--candidate")
    v19_parser.add_argument("--publication")
    v19_parser.add_argument("--check", action="store_true")
    v20_parser = routes.add_parser("v20")
    v20_parser.add_argument("--entry-candidate")
    v20_parser.add_argument("--publication")
    v20_parser.add_argument("--owner-identity")
    v20_parser.add_argument("--decided-at-utc")
    v20_parser.add_argument("--rationale")
    v20_parser.add_argument("--effective-hold", action="store_true")
    v20_parser.add_argument("--check", action="store_true")
    v21_parser = routes.add_parser("v21")
    v21_parser.add_argument("--candidate")
    v21_parser.add_argument("--publication")
    v21_parser.add_argument("--effective-hold", action="store_true")
    v21_parser.add_argument("--check", action="store_true")
    args = parser.parse_args(arguments)
    if args.route in {"v19", "v20", "v21"} and args.publication is not None and not args.check:
        parser.error(f"{args.route} --publication requires --check")
    if (
        args.route in {"v20", "v21"}
        and args.publication is not None
        and args.effective_hold
    ):
        parser.error(f"{args.route} --publication cannot be combined with --effective-hold")
    root = Path(args.repository)
    try:
        if args.route == "bootstrap":
            print(
                json.dumps(
                    bootstrap_successor_boundary(
                        root,
                        candidate_revision=args.candidate,
                        source_repository=args.source_workflow_repository,
                        source_workflow_ref=args.source_workflow_ref,
                        source_workflow_sha=args.source_workflow_sha,
                    ),
                    indent=2,
                    ensure_ascii=False,
                )
            )
        elif args.route == "ci-trust":
            rulesets_content: bytes | None = None
            if args.rulesets_file is not None:
                try:
                    rulesets_content = Path(args.rulesets_file).read_bytes()
                except OSError:
                    rulesets_content = b""
            result = ci_trust_boundary_result(
                rulesets_content=rulesets_content,
                source_repository=args.source_workflow_repository,
                source_workflow_ref=args.source_workflow_ref,
                source_workflow_sha=args.source_workflow_sha,
                source_repository_id=args.source_repository_id,
            )
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return 0 if result["result"] == "PASS" else 2
        elif args.route == "inventory":
            ledger: list[dict[str, str]] = []
            document = inventory_route(root, check=args.check, assertion_ledger=ledger)
            print(
                json.dumps(
                    {
                        "schemaVersion": "hexalith.conversations.story-7.1-input-inventory-result.v1",
                        "route": "inventory",
                        "result": "PASS",
                        "inventories": len(document["scenarioInventories"]) + 4,
                        "assertionLedger": ledger,
                        "blockers": [],
                    },
                    indent=2,
                    ensure_ascii=False,
                )
            )
        elif args.route == "v19":
            document = publish_v19(
                root,
                candidate_revision=args.candidate,
                publication_revision=args.publication,
                check=args.check,
            )
            print(
                "V19_STORY_7_1_CHECKPOINT_AUTHORITY_OK "
                f"CANDIDATE={document['freshCheckpoint']['candidateCommit']} "
                f"TESTS={document['freshCheckpoint']['machineResult']['tests']}"
            )
        elif getattr(args, "effective_hold", False):
            evaluated_revision = args.entry_candidate if args.route == "v20" else args.candidate
            result = effective_hold(root, evaluated_revision=evaluated_revision)
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return 0 if result["result"] == "PASS" else (2 if result["result"] == "BLOCKED" else 1)
        elif args.route == "v20":
            document = publish_v20(
                root,
                entry_revision=args.entry_candidate,
                publication_revision=args.publication,
                owner_identity=args.owner_identity,
                decided_at_utc=args.decided_at_utc,
                rationale=args.rationale,
                check=args.check,
            )
            print(
                "V20_STORY_7_1_RELEASE_OWNER_AUTHORITY_OK "
                f"ENTRY={document['candidateRoles']['entryCandidate']} DECISION=LIFTED"
            )
        else:
            document = publish_v21(
                root,
                candidate_revision=args.candidate,
                publication_revision=args.publication,
                check=args.check,
            )
            if args.check:
                print(
                    "V21_STORY_7_1_AUTHORITY_CORRECTION_OK "
                    f"TOOLING={document['hardenedTooling']['commit']} EFFECTIVE_HOLD=LIFTED"
                )
            else:
                print(
                    "V21_STORY_7_1_AUTHORITY_CORRECTION_OK "
                    f"TOOLING={document['hardenedTooling']['commit']} STATUS=GENERATED"
                )
        return 0
    except SuccessorAuthorityError as error:
        sys.stdout.write(json.dumps(failure_document(root, args.route, error), indent=2, ensure_ascii=False) + "\n")
        return 2 if error.state == "BLOCKED" else 1


if __name__ == "__main__":
    raise SystemExit(main())
