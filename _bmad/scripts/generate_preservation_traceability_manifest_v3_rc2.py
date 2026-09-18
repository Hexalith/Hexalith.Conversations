#!/usr/bin/env python3
"""Generate or verify the unapproved preservation manifest 3.0.0-rc.2.

The candidate is an exact base-plus-content-addressed overlay. Its digest covers
every source input. Volatile xUnit XML is reduced to a stable
semantic projection after the run, which breaks the former XML/manifest cycle.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import importlib.util
import json
import os
import re
import stat
import subprocess
import sys
import tempfile
import unicodedata
from collections import Counter
from pathlib import Path
from typing import Any

import jsonschema


ROOT = Path(__file__).resolve().parents[2]
ROOT_RESOLVED = ROOT.resolve(strict=True)
RC1_JSON = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.json"
RC1_MARKDOWN = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.md"
RC1_SCHEMA = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.schema.json"
RC1_DIGEST = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3.sha256"
RC1_APPROVAL = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-owner-approval.json"
RC1_APPROVAL_DIGEST = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-owner-approval.sha256"
RC2_JSON = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json"
RC2_MARKDOWN = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-rc2.md"
RC2_SCHEMA = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-rc2.schema.json"
RC2_DIGEST = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-rc2.sha256"
EVIDENCE_ROOT = ROOT / "_bmad-output/implementation-artifacts/preservation-traceability-v3-rc2"
XML_PATH = EVIDENCE_ROOT / "conformance-remediated.xml"
RUN_RECEIPT_PATH = EVIDENCE_ROOT / "conformance-run-receipt.json"
SEMANTIC_RESULTS_PATH = EVIDENCE_ROOT / "conformance-semantic-results.json"
BUILD_LOG_PATH = EVIDENCE_ROOT / "conformance-build-remediated.log"
BUILD_RECEIPT_PATH = EVIDENCE_ROOT / "candidate-build-receipt.json"
TOOLCHAIN_PATH = EVIDENCE_ROOT / "candidate-dotnet-info.txt"
RESTORE_LOG_PATH = EVIDENCE_ROOT / "candidate-restore.log"
RESTORE_INVENTORY_PATH = EVIDENCE_ROOT / "candidate-restore-dependency-inventory.json"
RESTORE_RECEIPT_PATH = EVIDENCE_ROOT / "candidate-restore-receipt.json"
DETACHED_INDEX_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.json"
DETACHED_DIGEST_PATH = ROOT / "docs/release-evidence/preservation-traceability-manifest-v3-rc2-detached-evidence.sha256"
ASSEMBLY_PATH = ROOT / "tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll"
RESTORE_PROJECT_PATH = ROOT / "tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj"
BASE_COMMIT = "2d2ae57db1fdcc164fe01ac4b1d99af15c31b324"
RC1_SHA256 = "a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc"
RC1_APPROVAL_SHA256 = "23e07e8339d8822c35ef665d44a60238e237c4c29e37560d6f475585dd27df5a"
VERSION = "3.0.0-rc.2"
CANDIDATE_DIGEST_ALGORITHM = "sha256-path-mode-hash-size-overlay-v2"
CANDIDATE_KIND = "base-plus-content-addressed-overlay"
REGULAR_GIT_MODES = {"100644", "100755"}
SOURCE_INPUT_MODE = "100644"

# This inventory is the source-pinned boundary.  Adding, removing, or renaming
# an implementation input requires an intentional edit here and in the
# independent Conformance validator.  Generated evidence is deliberately not a
# source input and can therefore be regenerated without changing the candidate.
SOURCE_INPUT_PATHS = sorted(
    [
        ".agents/skills/bmad-build/step-05-present.md",
        ".agents/skills/bmad-build/step-oneshot.md",
        ".agents/skills/bmad-build-auto/step-04-review.md",
        ".agents/skills/bmad-code-review/steps/step-04-present.md",
        ".claude/skills/bmad-build/step-05-present.md",
        ".claude/skills/bmad-build/step-oneshot.md",
        ".claude/skills/bmad-build-auto/step-04-review.md",
        ".claude/skills/bmad-code-review/steps/step-04-present.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md",
        "_bmad/scripts/generate_preservation_traceability_manifest_v3.py",
        "_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py",
        "_bmad/scripts/tests/test_generate_preservation_traceability_manifest.py",
        "_bmad/scripts/tests/test_verify_submodule_promotion.py",
        "tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/PlanningAuthorityV9ValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/PreservationTraceabilityManifestValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs",
        "docs/release-evidence/projection-read-store-population-proof-v2-binding-resolution.json",
    ]
)
EXPECTED_NEW_SOURCE_INPUT_PATHS = frozenset(
    {
        "_bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py",
        "docs/release-evidence/projection-read-store-population-proof-v2-binding-resolution.json",
    }
)

# These records govern assessment/workflow state and intentionally remain
# outside the source-byte candidate digest. Their paths are still frozen and
# participate in exact BASE_COMMIT-to-overlay changed-path equality.
ASSESSMENT_WORKFLOW_RECORD_PATHS = sorted(
    [
        "_bmad-output/implementation-artifacts/spec-remediate-preservation-traceability-v3-conformance.md",
        "_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.memlog.md",
    ]
)

GENERATED_OUTPUT_PATHS = sorted(
    [
        BUILD_LOG_PATH.relative_to(ROOT).as_posix(),
        BUILD_RECEIPT_PATH.relative_to(ROOT).as_posix(),
        TOOLCHAIN_PATH.relative_to(ROOT).as_posix(),
        RESTORE_LOG_PATH.relative_to(ROOT).as_posix(),
        RESTORE_INVENTORY_PATH.relative_to(ROOT).as_posix(),
        RESTORE_RECEIPT_PATH.relative_to(ROOT).as_posix(),
        XML_PATH.relative_to(ROOT).as_posix(),
        RUN_RECEIPT_PATH.relative_to(ROOT).as_posix(),
        SEMANTIC_RESULTS_PATH.relative_to(ROOT).as_posix(),
        RC2_JSON.relative_to(ROOT).as_posix(),
        RC2_MARKDOWN.relative_to(ROOT).as_posix(),
        RC2_SCHEMA.relative_to(ROOT).as_posix(),
        RC2_DIGEST.relative_to(ROOT).as_posix(),
        DETACHED_INDEX_PATH.relative_to(ROOT).as_posix(),
        DETACHED_DIGEST_PATH.relative_to(ROOT).as_posix(),
    ]
)


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical_json(value: Any) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False, sort_keys=False) + "\n").encode()


def atomic_write(path: Path, data: bytes) -> None:
    """Publish bytes with a same-filesystem replace and no partial target."""
    validate_repository_path(path, require_file=False, purpose="generated output")
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", dir=path.parent)
    temporary_path = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        temporary_path.replace(path)
        validate_repository_path(path, require_file=True, purpose="generated output")
    finally:
        if temporary_path.exists():
            temporary_path.unlink()


def binding(path: Path, role: str) -> dict[str, Any]:
    relative = validate_repository_path(path, require_file=True, purpose=f"artifact binding {role}")
    data = (ROOT / relative).read_bytes()
    return {"path": relative, "sha256": sha256_bytes(data), "bytes": len(data), "role": role}


def worktree_git_mode(path: Path) -> str:
    """Return the Git raw mode represented by the current regular-file permissions."""
    relative = validate_repository_path(path, require_file=True, purpose="mode-bound source input")
    mode = os.lstat(ROOT / relative).st_mode
    if not stat.S_ISREG(mode):
        raise ValueError(f"Mode-bound source input is not a regular file: {relative}")
    return "100755" if mode & 0o111 else "100644"


def baseline_source_present(relative: str) -> bool:
    """Cross-check one source's presence in the pinned baseline tree and object database."""
    tree = subprocess.run(
        ["git", "ls-tree", "--full-tree", "-z", BASE_COMMIT, "--", relative],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
    ).stdout
    rows = [row for row in tree.split(b"\0") if row]
    object_check = subprocess.run(
        ["git", "cat-file", "-e", f"{BASE_COMMIT}:{relative}"],
        cwd=ROOT,
        check=False,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
    )
    object_exists = object_check.returncode == 0
    if object_exists:
        if len(rows) != 1:
            raise ValueError(
                f"Source input baseline presence mismatch for {relative}: cat-file exists but ls-tree rows={len(rows)}"
            )
        _metadata, separator, tree_path = rows[0].partition(b"\t")
        if not separator or tree_path.decode("utf-8", errors="strict") != relative:
            raise ValueError(f"Source input baseline tree entry is malformed for {relative}")
        return True
    if rows:
        raise ValueError(
            f"Source input baseline presence mismatch for {relative}: cat-file missing but ls-tree rows={len(rows)}"
        )
    return False


def tracked_source_stage_mode(relative: str, *, baseline_present: bool) -> str | None:
    """Return one stage-0 source mode, allowing no row only for a new overlay input."""
    result = subprocess.run(
        ["git", "ls-files", "--stage", "-z", "--", relative],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
    )
    rows = [row for row in result.stdout.split(b"\0") if row]
    if not rows:
        if baseline_present:
            raise ValueError(
                f"Source input index entry missing for baseline-present path {relative} at {BASE_COMMIT}"
            )
        return None
    if len(rows) != 1:
        raise ValueError(
            f"Source input index entry mismatch for {relative}: expected exactly one stage row, got {len(rows)}"
        )
    metadata, separator, indexed_path = rows[0].partition(b"\t")
    fields = metadata.split()
    if (
        not separator
        or len(fields) != 3
        or fields[2] != b"0"
        or indexed_path.decode("utf-8", errors="strict") != relative
    ):
        raise ValueError(f"Source input index entry is malformed for {relative}")
    return fields[0].decode("ascii", errors="strict")


def source_binding(path: Path) -> dict[str, Any]:
    relative = validate_repository_path(path, require_file=True, purpose="source binding")
    mode = worktree_git_mode(path)
    if mode != SOURCE_INPUT_MODE:
        raise ValueError(
            f"Source input worktree mode mismatch for {relative}: expected {SOURCE_INPUT_MODE}, got {mode}"
        )
    baseline_present = baseline_source_present(relative)
    stage_mode = tracked_source_stage_mode(relative, baseline_present=baseline_present)
    if stage_mode is not None and stage_mode != SOURCE_INPUT_MODE:
        raise ValueError(
            f"Source input index mode mismatch for {relative}: expected stage=declared=worktree="
            f"{SOURCE_INPUT_MODE}, got stage={stage_mode}, declared={SOURCE_INPUT_MODE}, worktree={mode}"
        )
    data = (ROOT / relative).read_bytes()
    return {
        "path": relative,
        "mode": mode,
        "sha256": sha256_bytes(data),
        "bytes": len(data),
        "role": "rc2-source-input",
    }


def bytes_binding(path: Path, data: bytes, role: str) -> dict[str, Any]:
    relative = validate_repository_path(path, require_file=False, purpose=f"artifact binding {role}")
    return {"path": relative, "sha256": sha256_bytes(data), "bytes": len(data), "role": role}


def validate_repository_path(path: Path, *, require_file: bool, purpose: str) -> str:
    """Return a safe repository-relative regular-file path or fail closed."""
    lexical = path if path.is_absolute() else ROOT / path
    try:
        relative = lexical.relative_to(ROOT)
    except ValueError as error:
        raise ValueError(f"Unsafe {purpose} path outside repository: {path}") from error
    if not relative.parts or any(part in {".", ".."} for part in relative.parts):
        raise ValueError(f"Unsafe {purpose} path traversal: {path}")

    current = ROOT
    exists = True
    for index, part in enumerate(relative.parts):
        current /= part
        try:
            mode = os.lstat(current).st_mode
        except FileNotFoundError:
            exists = False
            if require_file or index != len(relative.parts) - 1:
                raise ValueError(f"Missing {purpose} path: {relative.as_posix()}")
            break
        if stat.S_ISLNK(mode):
            raise ValueError(f"Unsafe {purpose} symlink path: {relative.as_posix()}")
        if index < len(relative.parts) - 1 and not stat.S_ISDIR(mode):
            raise ValueError(f"Unsafe {purpose} non-directory component: {relative.as_posix()}")
        if index == len(relative.parts) - 1 and not stat.S_ISREG(mode):
            raise ValueError(f"Unsafe {purpose} non-regular file: {relative.as_posix()}")

    resolved = lexical.resolve(strict=require_file)
    try:
        resolved.relative_to(ROOT_RESOLVED)
    except ValueError as error:
        raise ValueError(f"Unsafe {purpose} resolved path outside repository: {relative.as_posix()}") from error

    stage = subprocess.run(
        ["git", "ls-files", "--stage", "--", relative.as_posix()],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
        text=True,
    ).stdout.splitlines()
    if stage:
        modes = {line.split(" ", 1)[0] for line in stage}
        if len(stage) != 1 or not modes.issubset(REGULAR_GIT_MODES):
            raise ValueError(
                f"Unsafe {purpose} Git mode for {relative.as_posix()}: {sorted(modes)}"
            )
    if require_file and not exists:
        raise ValueError(f"Missing {purpose} path: {relative.as_posix()}")
    return relative.as_posix()


def load_v3_generator() -> Any:
    path = ROOT / "_bmad/scripts/generate_preservation_traceability_manifest_v3.py"
    validate_repository_path(path, require_file=True, purpose="rc.1 generator")
    spec = importlib.util.spec_from_file_location("preservation_v3_rc1_generator", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def git_paths(*args: str) -> set[str]:
    result = subprocess.run(["git", *args], cwd=ROOT, check=True, stdout=subprocess.PIPE)
    return {line.decode("utf-8").strip() for line in result.stdout.splitlines() if line.strip()}


def git_text(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.strip()


def path_inventory_digest(paths: list[str]) -> str:
    canonical = "".join(f"{unicodedata.normalize('NFC', path)}\n" for path in sorted(paths)).encode("utf-8")
    return sha256_bytes(canonical)


def source_bindings() -> list[dict[str, Any]]:
    actual_new = {path for path in SOURCE_INPUT_PATHS if not baseline_source_present(path)}
    if actual_new != EXPECTED_NEW_SOURCE_INPUT_PATHS:
        raise ValueError(
            "Frozen source baseline-presence inventory mismatch: "
            f"expected new={sorted(EXPECTED_NEW_SOURCE_INPUT_PATHS)}, actual new={sorted(actual_new)}"
        )
    return [source_binding(ROOT / path) for path in SOURCE_INPUT_PATHS]


def candidate_digest(bindings: list[dict[str, Any]]) -> str:
    material = bytearray(f"overlay-v2\nbaseCommit\t{BASE_COMMIT}\n".encode("utf-8"))
    for row in bindings:
        normalized_path = unicodedata.normalize("NFC", row["path"])
        material.extend(
            f"{normalized_path}\t{row['mode']}\t{row['sha256']}\t{row['bytes']}\n".encode("utf-8")
        )
    return sha256_bytes(bytes(material))


def parse_toolchain_capture(text: str) -> dict[str, str]:
    sdk = re.search(r"(?m)^ Version:\s+([^\s]+)$", text)
    sdk_commit = re.search(r"(?m)^\.NET SDK:\s*\n Version:\s+[^\s]+\s*\n Commit:\s+([^\s]+)$", text)
    msbuild = re.search(r"(?m)^ MSBuild version:\s+([^\s]+)$", text)
    global_json = re.search(r"(?m)^global\.json file:\s*\n\s+(.+)$", text)
    if sdk is None or sdk_commit is None or msbuild is None or global_json is None:
        raise ValueError("Candidate dotnet --info capture lacks SDK, SDK commit, MSBuild, or global.json fields")
    recorded_global_json = Path(global_json.group(1).strip()).resolve()
    if recorded_global_json != (ROOT / "global.json").resolve():
        raise ValueError(f"Candidate dotnet --info resolved unexpected global.json: {recorded_global_json}")
    pinned_sdk = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))["sdk"]["version"]
    if sdk.group(1) != pinned_sdk:
        raise ValueError(f"Candidate SDK mismatch: global.json={pinned_sdk}, dotnet --info={sdk.group(1)}")
    return {
        "sdkVersion": sdk.group(1),
        "sdkCommit": sdk_commit.group(1),
        "msbuildVersion": msbuild.group(1),
    }


def dependency_inventory(expected_digest: str) -> dict[str, Any]:
    queue = [RESTORE_PROJECT_PATH.resolve()]
    visited: set[Path] = set()
    rows: list[dict[str, Any]] = []
    while queue:
        project = queue.pop()
        if project in visited:
            continue
        visited.add(project)
        validate_repository_path(project, require_file=True, purpose="restore project")
        assets = project.parent / "obj/project.assets.json"
        validate_repository_path(assets, require_file=True, purpose="restore dependency graph")
        document = json.loads(assets.read_text(encoding="utf-8"))
        recorded_project = Path(document["project"]["restore"]["projectPath"]).resolve()
        if recorded_project != project:
            raise ValueError(f"project.assets.json projectPath mismatch: expected {project}, got {recorded_project}")
        data = assets.read_bytes()
        rows.append(
            {
                "projectPath": project.relative_to(ROOT_RESOLVED).as_posix(),
                "assetsPath": assets.relative_to(ROOT_RESOLVED).as_posix(),
                "sha256": sha256_bytes(data),
                "bytes": len(data),
            }
        )
        frameworks = document["project"]["restore"].get("frameworks", {})
        references = {
            Path(reference["projectPath"]).resolve()
            for framework in frameworks.values()
            for reference in framework.get("projectReferences", {}).values()
        }
        queue.extend(references - visited)
    rows.sort(key=lambda row: row["projectPath"])
    return {
        "schemaVersion": "1.0.0",
        "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
        "candidateDigest": expected_digest,
        "rootProject": RESTORE_PROJECT_PATH.relative_to(ROOT).as_posix(),
        "projects": rows,
    }


def capture_restore_evidence(expected_digest: str) -> int:
    """Capture the current candidate toolchain, restore, and resolved dependency graph."""
    validate_changed_path_boundary(False)
    validate_repository_path(ROOT / "global.json", require_file=True, purpose="SDK pin")
    EVIDENCE_ROOT.mkdir(parents=True, exist_ok=True)
    toolchain_command = ["dotnet", "--info"]
    toolchain_run = subprocess.run(toolchain_command, cwd=ROOT, check=False, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if toolchain_run.returncode != 0:
        raise ValueError(f"dotnet --info failed with exit code {toolchain_run.returncode}")
    atomic_write(TOOLCHAIN_PATH, toolchain_run.stdout)
    toolchain_fields = parse_toolchain_capture(toolchain_run.stdout.decode("utf-8", errors="strict"))

    restore_command = [
        "dotnet",
        "restore",
        RESTORE_PROJECT_PATH.relative_to(ROOT).as_posix(),
        "--force-evaluate",
        "-p:NuGetAudit=false",
        "-p:MinVerVersionOverride=1.0.0",
    ]
    restore_run = subprocess.run(restore_command, cwd=ROOT, check=False, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    atomic_write(RESTORE_LOG_PATH, restore_run.stdout)
    if restore_run.returncode != 0:
        raise ValueError(f"Candidate restore failed with exit code {restore_run.returncode}")
    inventory = dependency_inventory(expected_digest)
    inventory_bytes = canonical_json(inventory)
    atomic_write(RESTORE_INVENTORY_PATH, inventory_bytes)
    receipt = {
        "schemaVersion": "1.0.0",
        "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
        "candidateDigest": expected_digest,
        "toolchain": {
            "command": toolchain_command,
            "exitCode": toolchain_run.returncode,
            **toolchain_fields,
            "capture": binding(TOOLCHAIN_PATH, "candidate-toolchain-capture"),
            "globalJson": binding(ROOT / "global.json", "candidate-sdk-pin"),
        },
        "restore": {
            "command": restore_command,
            "exitCode": restore_run.returncode,
            "result": "pass",
            "log": binding(RESTORE_LOG_PATH, "candidate-restore-log"),
            "dependencyInventory": bytes_binding(
                RESTORE_INVENTORY_PATH, inventory_bytes, "candidate-restore-dependency-inventory"
            ),
            "projectCount": len(inventory["projects"]),
        },
    }
    atomic_write(RESTORE_RECEIPT_PATH, canonical_json(receipt))
    print(
        f"WROTE {RESTORE_RECEIPT_PATH.relative_to(ROOT)} candidate={expected_digest} "
        f"sdk={toolchain_fields['sdkVersion']} msbuild={toolchain_fields['msbuildVersion']} "
        f"projects={len(inventory['projects'])}"
    )
    return 0


def validated_restore_evidence(expected_digest: str) -> dict[str, Any]:
    for path, purpose in (
        (TOOLCHAIN_PATH, "candidate toolchain capture"),
        (RESTORE_LOG_PATH, "candidate restore log"),
        (RESTORE_INVENTORY_PATH, "candidate dependency inventory"),
        (RESTORE_RECEIPT_PATH, "candidate restore receipt"),
    ):
        validate_repository_path(path, require_file=True, purpose=purpose)
    receipt = json.loads(RESTORE_RECEIPT_PATH.read_text(encoding="utf-8"))
    if receipt.get("candidateDigestAlgorithm") != CANDIDATE_DIGEST_ALGORITHM or receipt.get("candidateDigest") != expected_digest:
        raise ValueError("Candidate restore receipt digest mismatch")
    toolchain = receipt.get("toolchain", {})
    fields = parse_toolchain_capture(TOOLCHAIN_PATH.read_text(encoding="utf-8"))
    if toolchain.get("command") != ["dotnet", "--info"] or toolchain.get("exitCode") != 0:
        raise ValueError("Candidate toolchain command/exit mismatch")
    if any(toolchain.get(key) != fields[key] for key in ("sdkVersion", "sdkCommit", "msbuildVersion")):
        raise ValueError("Candidate toolchain SDK/commit/MSBuild fields mismatch")
    if toolchain.get("capture") != binding(TOOLCHAIN_PATH, "candidate-toolchain-capture"):
        raise ValueError("Candidate toolchain capture binding mismatch")
    if toolchain.get("globalJson") != binding(ROOT / "global.json", "candidate-sdk-pin"):
        raise ValueError("Candidate global.json binding mismatch")
    expected_restore_command = [
        "dotnet",
        "restore",
        RESTORE_PROJECT_PATH.relative_to(ROOT).as_posix(),
        "--force-evaluate",
        "-p:NuGetAudit=false",
        "-p:MinVerVersionOverride=1.0.0",
    ]
    restore = receipt.get("restore", {})
    if restore.get("command") != expected_restore_command or restore.get("exitCode") != 0 or restore.get("result") != "pass":
        raise ValueError("Candidate restore command/exit/result mismatch")
    if restore.get("log") != binding(RESTORE_LOG_PATH, "candidate-restore-log"):
        raise ValueError("Candidate restore log binding mismatch")
    expected_inventory = dependency_inventory(expected_digest)
    inventory_bytes = canonical_json(expected_inventory)
    if RESTORE_INVENTORY_PATH.read_bytes() != inventory_bytes:
        raise ValueError("Candidate resolved dependency inventory drift")
    expected_inventory_binding = bytes_binding(
        RESTORE_INVENTORY_PATH, inventory_bytes, "candidate-restore-dependency-inventory"
    )
    if restore.get("dependencyInventory") != expected_inventory_binding or restore.get("projectCount") != len(expected_inventory["projects"]):
        raise ValueError("Candidate restore dependency binding/count mismatch")
    return receipt


def validate_changed_path_boundary(require_outputs: bool) -> None:
    if git_text("rev-parse", f"{BASE_COMMIT}^{{commit}}") != BASE_COMMIT:
        raise ValueError(f"Pinned base commit is unavailable: {BASE_COMMIT}")
    head = git_text("rev-parse", "HEAD^{commit}")
    relationship = subprocess.run(
        ["git", "merge-base", "--is-ancestor", BASE_COMMIT, head], cwd=ROOT, check=False
    ).returncode
    if relationship != 0:
        raise ValueError(f"HEAD {head} is not a descendant of pinned base commit {BASE_COMMIT}")

    for path in SOURCE_INPUT_PATHS:
        validate_repository_path(ROOT / path, require_file=True, purpose="source input")
    for path in ASSESSMENT_WORKFLOW_RECORD_PATHS:
        validate_repository_path(ROOT / path, require_file=True, purpose="assessment/workflow record")
    for path in GENERATED_OUTPUT_PATHS:
        if os.path.lexists(ROOT / path) or require_outputs:
            validate_repository_path(ROOT / path, require_file=True, purpose="generated output")

    actual = git_paths("diff", "--name-only", BASE_COMMIT, "--") | git_paths("ls-files", "--others", "--exclude-standard")
    # Ignored evidence (notably the build log) is invisible to Git. Include
    # each frozen generated output explicitly when it exists.
    actual.update(path for path in GENERATED_OUTPUT_PATHS if os.path.lexists(ROOT / path))
    expected_sources = set(SOURCE_INPUT_PATHS)
    expected_assessment = set(ASSESSMENT_WORKFLOW_RECORD_PATHS)
    expected_outputs = set(GENERATED_OUTPUT_PATHS)
    allowed = expected_sources | expected_assessment | expected_outputs
    missing_sources = sorted(expected_sources - actual)
    missing_assessment = sorted(expected_assessment - actual)
    unexpected = sorted(actual - allowed)
    missing_outputs = sorted(expected_outputs - actual) if require_outputs else []
    if missing_sources or missing_assessment or unexpected or missing_outputs:
        raise ValueError(
            f"Exact {BASE_COMMIT}-to-content-addressed-overlay changed-path boundary mismatch: "
            f"missing source inputs={missing_sources}; missing assessment/workflow records={missing_assessment}; "
            f"missing generated outputs={missing_outputs}; unexpected paths={unexpected}"
        )


def build_command(expected_digest: str) -> list[str]:
    return [
        "dotnet",
        "build",
        RESTORE_PROJECT_PATH.relative_to(ROOT).as_posix(),
        "-c",
        "Release",
        "-t:Rebuild",
        "--no-restore",
        "-m:1",
        "-p:NuGetAudit=false",
        "-p:MinVerVersionOverride=1.0.0",
        f"-p:SourceRevisionId={expected_digest}",
    ]


def parse_build_log() -> dict[str, Any]:
    validate_repository_path(BUILD_LOG_PATH, require_file=True, purpose="focused build log")
    text = BUILD_LOG_PATH.read_text(encoding="utf-8", errors="strict")
    warning_matches = re.findall(r"(?m)^\s*([0-9]+) Warning\(s\)\s*$", text)
    error_matches = re.findall(r"(?m)^\s*([0-9]+) Error\(s\)\s*$", text)
    if not warning_matches or not error_matches:
        raise ValueError("Build log does not contain parseable warning/error counters")
    warnings = int(warning_matches[-1])
    errors = int(error_matches[-1])
    result = "pass" if "Build succeeded." in text and errors == 0 else "fail"
    if ASSEMBLY_PATH.as_posix() not in text and str(ASSEMBLY_PATH.relative_to(ROOT)) not in text:
        raise ValueError("Build log does not name the expected Conformance assembly output")
    if result != "pass" or warnings != 0 or errors != 0:
        raise ValueError(f"Focused rebuild was not clean: result={result}, warnings={warnings}, errors={errors}")
    return {"result": result, "warnings": warnings, "errors": errors}


def assembly_binding(expected_digest: str) -> dict[str, Any]:
    validate_repository_path(ASSEMBLY_PATH, require_file=True, purpose="Conformance assembly")
    data = ASSEMBLY_PATH.read_bytes()
    embedded = sorted(set(match.decode("ascii") for match in re.findall(rb"[0-9a-f]{64}", data)))
    if expected_digest not in embedded:
        raise ValueError(f"Assembly does not embed SourceRevisionId {expected_digest}; embedded 64-hex values={embedded}")
    return {
        "path": ASSEMBLY_PATH.relative_to(ROOT).as_posix(),
        "sha256": sha256_bytes(data),
        "bytes": len(data),
        "sourceRevisionId": expected_digest,
    }


def run_build(expected_digest: str) -> int:
    """Validate restore evidence, execute the one exact build command, and publish its receipt."""
    validate_changed_path_boundary(False)
    restore_receipt = validated_restore_evidence(expected_digest)
    pre_build = {
        "restoreReceipt": binding(RESTORE_RECEIPT_PATH, "pre-build-candidate-restore-receipt"),
        "toolchainCapture": binding(TOOLCHAIN_PATH, "pre-build-candidate-toolchain-capture"),
        "dependencyInventory": binding(
            RESTORE_INVENTORY_PATH, "pre-build-candidate-restore-dependency-inventory"
        ),
        "restoreCommand": restore_receipt["restore"]["command"],
        "restoreExitCode": restore_receipt["restore"]["exitCode"],
        "restoreResult": restore_receipt["restore"]["result"],
    }
    command = build_command(expected_digest)
    completed = subprocess.run(
        command,
        cwd=ROOT,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
    )
    atomic_write(BUILD_LOG_PATH, completed.stdout)
    parsed = parse_build_log()
    if completed.returncode != 0 or parsed["result"] != "pass":
        raise ValueError(
            f"Controlled candidate build failed: exit={completed.returncode}, result={parsed['result']}"
        )
    assembly = assembly_binding(expected_digest)
    receipt = {
        "schemaVersion": "1.0.0",
        "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
        "candidateDigest": expected_digest,
        "sequence": [
            "validated-restore-evidence",
            "executed-exact-build-command",
            "validated-post-build-assembly",
        ],
        "preBuild": pre_build,
        "build": {
            "command": command,
            "exitCode": completed.returncode,
            "result": parsed["result"],
            "warnings": parsed["warnings"],
            "errors": parsed["errors"],
            "log": binding(BUILD_LOG_PATH, "candidate-build-log"),
        },
        "assembly": assembly,
    }
    atomic_write(BUILD_RECEIPT_PATH, canonical_json(receipt))
    print(
        f"WROTE {BUILD_RECEIPT_PATH.relative_to(ROOT)} candidate={expected_digest} "
        f"assemblySha256={assembly['sha256']} warnings={parsed['warnings']} errors={parsed['errors']}"
    )
    return 0


def validated_build_evidence(expected_digest: str) -> dict[str, Any]:
    """Validate the generator-owned restore-before-build receipt against current exact bytes."""
    restore_receipt = validated_restore_evidence(expected_digest)
    validate_repository_path(BUILD_RECEIPT_PATH, require_file=True, purpose="candidate build receipt")
    validate_repository_path(BUILD_LOG_PATH, require_file=True, purpose="candidate build log")
    receipt = json.loads(BUILD_RECEIPT_PATH.read_text(encoding="utf-8"))
    if set(receipt) != {
        "schemaVersion",
        "candidateDigestAlgorithm",
        "candidateDigest",
        "sequence",
        "preBuild",
        "build",
        "assembly",
    }:
        raise ValueError("Candidate build receipt has an unexpected top-level shape")
    if (
        receipt.get("schemaVersion") != "1.0.0"
        or receipt.get("candidateDigestAlgorithm") != CANDIDATE_DIGEST_ALGORITHM
        or receipt.get("candidateDigest") != expected_digest
    ):
        raise ValueError("Candidate build receipt digest mismatch")
    if receipt.get("sequence") != [
        "validated-restore-evidence",
        "executed-exact-build-command",
        "validated-post-build-assembly",
    ]:
        raise ValueError("Candidate build receipt restore/build order mismatch")
    expected_pre_build = {
        "restoreReceipt": binding(RESTORE_RECEIPT_PATH, "pre-build-candidate-restore-receipt"),
        "toolchainCapture": binding(TOOLCHAIN_PATH, "pre-build-candidate-toolchain-capture"),
        "dependencyInventory": binding(
            RESTORE_INVENTORY_PATH, "pre-build-candidate-restore-dependency-inventory"
        ),
        "restoreCommand": restore_receipt["restore"]["command"],
        "restoreExitCode": restore_receipt["restore"]["exitCode"],
        "restoreResult": restore_receipt["restore"]["result"],
    }
    if receipt.get("preBuild") != expected_pre_build:
        raise ValueError("Candidate build receipt pre-build restore/toolchain binding mismatch")
    build = receipt.get("build", {})
    parsed = parse_build_log()
    if build.get("command") != build_command(expected_digest):
        raise ValueError("Candidate build receipt command mismatch")
    if build.get("exitCode") != 0 or build.get("result") != "pass":
        raise ValueError("Candidate build receipt exit/result mismatch")
    if build.get("warnings") != parsed["warnings"] or build.get("errors") != parsed["errors"]:
        raise ValueError("Candidate build receipt warning/error counters mismatch")
    if build.get("log") != binding(BUILD_LOG_PATH, "candidate-build-log"):
        raise ValueError("Candidate build receipt log binding mismatch")
    assembly = assembly_binding(expected_digest)
    if receipt.get("assembly") != assembly:
        raise ValueError("Candidate build receipt assembly binding mismatch")
    return receipt


def derive_xunit(v3: Any, xml_path: Path) -> tuple[list[str], dict[str, str], dict[str, Any]]:
    ids, results, raw = v3.read_xunit(xml_path)
    normalized: Counter[str] = Counter()
    result_names = {"Pass": "passed", "Fail": "failed", "Skip": "skipped", "Not Run": "notRun", "NotRun": "notRun"}
    for test_id, result in results.items():
        if result not in result_names:
            raise ValueError(f"Unknown xUnit result {result!r} for {test_id}")
        normalized[result_names[result]] += 1
    derived = {
        "total": len(ids),
        "passed": normalized["passed"],
        "failed": normalized["failed"],
        "skipped": normalized["skipped"],
        "notRun": normalized["notRun"],
        "testFramework": raw["testFramework"],
        "runtime": raw["runtime"],
        "targetFramework": raw["targetFramework"],
    }
    for counter in ("total", "passed", "failed", "skipped", "notRun"):
        if derived[counter] != raw[counter]:
            raise ValueError(f"xUnit {counter} counter mismatch: tests={derived[counter]}, assembly={raw[counter]}")

    import xml.etree.ElementTree as element_tree

    xml_assembly = element_tree.parse(xml_path).getroot().find("assembly")
    if xml_assembly is None:
        raise ValueError("xUnit XML has no assembly element")
    recorded_assembly = Path(xml_assembly.attrib["name"]).resolve()
    if recorded_assembly != ASSEMBLY_PATH.resolve():
        raise ValueError(f"xUnit XML assembly mismatch: expected {ASSEMBLY_PATH}, got {recorded_assembly}")
    return ids, results, derived


def run_conformance(expected_digest: str) -> int:
    """Run the bound assembly and atomically publish a runner-owned receipt."""
    validate_changed_path_boundary(False)
    build_receipt = validated_build_evidence(expected_digest)
    before = build_receipt["assembly"]
    if assembly_binding(expected_digest) != before:
        raise ValueError("Conformance assembly differs from the controlled build receipt")
    manifest_before = binding(RC2_JSON, "pre-run-final-manifest")
    EVIDENCE_ROOT.mkdir(parents=True, exist_ok=True)
    temporary_xml = Path(tempfile.gettempdir()) / f"hexalith-conversations-rc2-{expected_digest}.xml"
    if os.path.lexists(temporary_xml):
        temporary_xml.unlink()
    command = [
        "dotnet",
        "exec",
        ASSEMBLY_PATH.relative_to(ROOT).as_posix(),
        "-result-xml",
        str(temporary_xml),
    ]
    try:
        completed = subprocess.run(command, cwd=ROOT, check=False)
        try:
            temporary_mode = os.lstat(temporary_xml).st_mode
        except FileNotFoundError:
            raise ValueError(f"Conformance runner exited {completed.returncode} without producing xUnit XML")
        if not stat.S_ISREG(temporary_mode) or stat.S_ISLNK(temporary_mode):
            raise ValueError("Conformance runner produced a non-regular or symlink xUnit XML file")
        after = assembly_binding(expected_digest)
        if after != before:
            raise ValueError("Conformance assembly changed while the runner was executing")
        manifest_after = binding(RC2_JSON, "pre-run-final-manifest")
        if manifest_after != manifest_before:
            raise ValueError("Conformance manifest changed while the runner was executing")
        v3 = load_v3_generator()
        ids, results, summary = derive_xunit(v3, temporary_xml)
        has_test_failure = summary["failed"] > 0
        if (completed.returncode == 0) == has_test_failure:
            raise ValueError(
                f"Conformance exit/result mismatch: exit={completed.returncode}, failed={summary['failed']}"
            )
        green = (
            completed.returncode == 0
            and summary["failed"] == 0
            and summary["skipped"] == 0
            and summary["notRun"] == 0
        )
        xml_data = temporary_xml.read_bytes()
        receipt = {
            "schemaVersion": "1.0.0",
            "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
            "candidateDigest": expected_digest,
            "runner": {
                "command": command,
                "exitCode": completed.returncode,
                "result": "pass" if green else "fail",
            },
            "preRunManifest": manifest_before,
            "assembly": before,
            "xml": {
                "path": XML_PATH.relative_to(ROOT).as_posix(),
                "sha256": sha256_bytes(xml_data),
                "bytes": len(xml_data),
            },
            "summary": summary,
            "testResults": [{"id": test_id, "result": results[test_id]} for test_id in ids],
        }
        atomic_write(XML_PATH, xml_data)
        atomic_write(RUN_RECEIPT_PATH, canonical_json(receipt))
        print(
            f"WROTE {RUN_RECEIPT_PATH.relative_to(ROOT)} candidate={expected_digest} "
            f"xmlSha256={receipt['xml']['sha256']} result={receipt['runner']['result']} "
            f"passed={summary['passed']}/{summary['total']}"
        )
        return 0 if green else (completed.returncode or 1)
    finally:
        if os.path.lexists(temporary_xml):
            temporary_xml.unlink()


def validated_receipt(
    v3: Any,
    expected_digest: str,
    assembly: dict[str, Any],
    *,
    require_current_manifest: bool,
) -> tuple[dict[str, Any], list[str], dict[str, str], dict[str, Any]]:
    """Validate existing runner provenance; never synthesize it during generation."""
    validate_repository_path(RUN_RECEIPT_PATH, require_file=True, purpose="runner-owned receipt")
    validate_repository_path(XML_PATH, require_file=True, purpose="runner-owned xUnit XML")
    receipt = json.loads(RUN_RECEIPT_PATH.read_text(encoding="utf-8"))
    if set(receipt) != {
        "schemaVersion",
        "candidateDigestAlgorithm",
        "candidateDigest",
        "runner",
        "preRunManifest",
        "assembly",
        "xml",
        "summary",
        "testResults",
    }:
        raise ValueError("Conformance receipt has an unexpected top-level shape")
    if receipt["schemaVersion"] != "1.0.0":
        raise ValueError("Conformance receipt schemaVersion mismatch")
    if receipt["candidateDigestAlgorithm"] != CANDIDATE_DIGEST_ALGORITHM or receipt["candidateDigest"] != expected_digest:
        raise ValueError("Conformance receipt candidate mismatch")
    if receipt["assembly"] != assembly:
        raise ValueError("Conformance receipt assembly binding differs from the current exact assembly")
    pre_run_manifest = receipt["preRunManifest"]
    if (
        set(pre_run_manifest) != {"path", "sha256", "bytes", "role"}
        or pre_run_manifest.get("path") != RC2_JSON.relative_to(ROOT).as_posix()
        or pre_run_manifest.get("role") != "pre-run-final-manifest"
        or not re.fullmatch(r"[0-9a-f]{64}", str(pre_run_manifest.get("sha256", "")))
        or not isinstance(pre_run_manifest.get("bytes"), int)
        or pre_run_manifest["bytes"] <= 0
    ):
        raise ValueError("Conformance receipt pre-run manifest binding is malformed")
    if require_current_manifest and pre_run_manifest != binding(RC2_JSON, "pre-run-final-manifest"):
        raise ValueError("Conformance receipt did not run the exact final manifest bytes")
    xml_data = XML_PATH.read_bytes()
    expected_xml = {
        "path": XML_PATH.relative_to(ROOT).as_posix(),
        "sha256": sha256_bytes(xml_data),
        "bytes": len(xml_data),
    }
    if receipt["xml"] != expected_xml:
        raise ValueError("Conformance receipt XML binding mismatch; stale or replaced XML is not runner-owned evidence")
    ids, results, derived = derive_xunit(v3, XML_PATH)
    projected_results = [{"id": test_id, "result": results[test_id]} for test_id in ids]
    if receipt["summary"] != derived or receipt["testResults"] != projected_results:
        raise ValueError("Conformance receipt rows/counters do not match the bound XML")
    expected_command = [
        "dotnet",
        "exec",
        ASSEMBLY_PATH.relative_to(ROOT).as_posix(),
        "-result-xml",
        str(Path(tempfile.gettempdir()) / f"hexalith-conversations-rc2-{expected_digest}.xml"),
    ]
    runner = receipt["runner"]
    if set(runner) != {"command", "exitCode", "result"} or runner["command"] != expected_command:
        raise ValueError("Conformance receipt runner command mismatch")
    green = (
        runner["exitCode"] == 0
        and derived["failed"] == 0
        and derived["skipped"] == 0
        and derived["notRun"] == 0
    )
    if runner["result"] != ("pass" if green else "fail"):
        raise ValueError("Conformance receipt runner result is not derived from exit code and counters")
    if (runner["exitCode"] == 0) == (derived["failed"] > 0):
        raise ValueError("Conformance receipt exit code contradicts failed-test rows")

    projection = {
        "schemaVersion": "1.0.0",
        "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
        "candidateDigest": expected_digest,
        "assembly": assembly,
        "summary": derived,
        "testResults": [{"id": test_id, "result": results[test_id]} for test_id in ids],
    }
    return projection, ids, results, derived


def obligation_projection(row: dict[str, Any]) -> dict[str, Any]:
    result = {
        "id": row["id"],
        "kind": row["kind"],
        "preservationState": row["preservationState"],
        "releaseActivationState": row["releaseActivationState"],
        # Closure byte hashes are refreshed when their source evidence changes;
        # they are bindings, not classification or closure targets.  Everything
        # else in the complete closure stays frozen to rc.1.
        "closure": closure_projection(row["closure"]),
        "orphan": row["orphan"],
    }
    for optional in ("controlOwner", "tier"):
        if optional in row:
            result[optional] = row[optional]
    return result


def closure_projection(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: closure_projection(item) for key, item in value.items() if not key.lower().endswith("sha256")}
    if isinstance(value, list):
        return [closure_projection(item) for item in value]
    return value


def build_schema(binding_count: int, artifact_roles: list[str], source_state: str) -> dict[str, Any]:
    schema = json.loads(RC1_SCHEMA.read_text(encoding="utf-8"))
    schema["$id"] = RC2_SCHEMA.name
    schema["title"] = "Conversations preservation traceability manifest 3.0.0-rc.2"
    properties = schema["properties"]
    properties["$schema"]["const"] = RC2_SCHEMA.name
    properties["manifestVersion"]["const"] = VERSION

    lineage = properties["lineage"]
    lineage["required"].append("predecessorApproval")
    lineage["properties"]["predecessorVersion"]["const"] = "3.0.0-rc.1"
    lineage["properties"]["predecessorApproval"] = {"$ref": "#/$defs/artifactBinding"}

    source = properties["sourceBinding"]["properties"]
    overlay_schema = source["ownerReviewOverlay"]
    overlay_schema["required"] = [
        "candidateKind",
        "baseCommit",
        "bindings",
        "sourceInputPathInventorySha256",
        "assessmentWorkflowRecords",
        "assessmentWorkflowRecordPathInventorySha256",
        "assessmentRecordsExcludedFromCandidateDigest",
        "candidateDigestAlgorithm",
        "candidateDigest",
        "generatedOutputs",
        "generatedOutputPathInventorySha256",
        "purpose",
    ]
    overlay = overlay_schema["properties"]
    overlay.pop("committed", None)
    overlay["candidateKind"] = {"const": CANDIDATE_KIND}
    overlay["baseCommit"]["const"] = BASE_COMMIT
    overlay["bindings"]["minItems"] = binding_count
    overlay["bindings"]["maxItems"] = binding_count
    overlay["bindings"]["items"] = {
        "type": "object",
        "additionalProperties": False,
        "required": ["path", "mode", "sha256", "bytes", "role"],
        "properties": {
            "path": {"type": "string", "minLength": 1},
            "mode": {"const": SOURCE_INPUT_MODE},
            "sha256": {"$ref": "#/$defs/sha256"},
            "bytes": {"type": "integer", "minimum": 0},
            "role": {"const": "rc2-source-input"},
        },
    }
    overlay["sourceInputPathInventorySha256"] = {"$ref": "#/$defs/sha256"}
    overlay["assessmentWorkflowRecords"] = {
        "type": "array",
        "minItems": len(ASSESSMENT_WORKFLOW_RECORD_PATHS),
        "maxItems": len(ASSESSMENT_WORKFLOW_RECORD_PATHS),
        "prefixItems": [{"const": path} for path in ASSESSMENT_WORKFLOW_RECORD_PATHS],
        "items": False,
    }
    overlay["assessmentWorkflowRecordPathInventorySha256"] = {"$ref": "#/$defs/sha256"}
    overlay["assessmentRecordsExcludedFromCandidateDigest"] = {"const": True}
    overlay["candidateDigestAlgorithm"] = {"const": CANDIDATE_DIGEST_ALGORITHM}
    overlay["candidateDigest"] = {"$ref": "#/$defs/sha256"}
    overlay["generatedOutputs"] = {
        "type": "array",
        "minItems": len(GENERATED_OUTPUT_PATHS),
        "maxItems": len(GENERATED_OUTPUT_PATHS),
        "prefixItems": [{"const": path} for path in GENERATED_OUTPUT_PATHS],
        "items": False,
    }
    overlay["generatedOutputPathInventorySha256"] = {"$ref": "#/$defs/sha256"}
    source["schema"]["properties"]["path"]["const"] = f"docs/release-evidence/{RC2_SCHEMA.name}"

    current = properties["testDenominator"]["properties"]["currentCandidate"]
    current["required"].append("notRun")
    current_properties = current["properties"]
    current_properties["sourceState"] = {"const": source_state}
    for counter in ("passed", "failed", "skipped", "notRun"):
        current_properties[counter] = {"type": "integer", "minimum": 0, "maximum": 473}
    current_properties["failedTestIds"] = {"type": "array", "uniqueItems": True, "items": {"type": "string", "minLength": 1}}

    build_binding = schema["$defs"]["buildBinding"]
    build_binding["properties"]["result"] = {"enum": ["pass", "fail"]}
    build_binding["properties"]["warnings"] = {"type": "integer", "minimum": 0}
    build_binding["properties"]["errors"] = {"type": "integer", "minimum": 0}
    build_binding["properties"]["assemblySourceRevisionId"] = {"$ref": "#/$defs/sha256"}
    build_binding["properties"]["toolchainSdkVersion"] = {"type": "string", "minLength": 1}
    build_binding["properties"]["toolchainMsbuildVersion"] = {"type": "string", "minLength": 1}
    build_binding["properties"]["restoreReceiptSha256"] = {"$ref": "#/$defs/sha256"}
    build_binding["properties"]["dependencyGraphSha256"] = {"$ref": "#/$defs/sha256"}
    build_binding["properties"]["buildReceiptSha256"] = {"$ref": "#/$defs/sha256"}
    build_binding.setdefault("allOf", []).append(
        {
            "if": {"properties": {"id": {"const": "owner-review-overlay-conformance-build"}}},
            "then": {
                "required": [
                    "assemblySourceRevisionId",
                    "toolchainSdkVersion",
                    "toolchainMsbuildVersion",
                    "restoreReceiptSha256",
                    "dependencyGraphSha256",
                    "buildReceiptSha256",
                ]
            },
        }
    )

    command_binding = schema["$defs"]["commandBinding"]
    command_binding["properties"]["summary"] = {
        "type": "object",
        "additionalProperties": False,
        "required": ["total", "passed", "failed", "skipped", "notRun", "testFramework", "runtime", "targetFramework"],
        "properties": {
            "total": {"type": "integer", "minimum": 0},
            "passed": {"type": "integer", "minimum": 0},
            "failed": {"type": "integer", "minimum": 0},
            "skipped": {"type": "integer", "minimum": 0},
            "notRun": {"type": "integer", "minimum": 0},
            "testFramework": {"type": "string", "minLength": 1},
            "runtime": {"type": "string", "minLength": 1},
            "targetFramework": {"type": "string", "minLength": 1},
            "startedAtUtc": {"type": "string", "minLength": 1},
            "finishedAtUtc": {"type": "string", "minLength": 1},
        },
    }
    command_binding["allOf"] = [
        {
            "if": {"required": ["summary"], "properties": {"result": {"const": "pass"}}},
            "then": {
                "properties": {
                    "summary": {
                        "properties": {
                            "failed": {"const": 0},
                            "skipped": {"const": 0},
                            "notRun": {"const": 0},
                        },
                        "anyOf": [
                            {"properties": {"total": {"const": 472}, "passed": {"const": 472}}},
                            {"properties": {"total": {"const": 473}, "passed": {"const": 473}}},
                        ],
                    }
                }
            },
        },
        {
            "if": {"required": ["summary"], "properties": {"result": {"const": "fail"}}},
            "then": {
                "properties": {
                    "summary": {
                        "anyOf": [
                            {"properties": {"failed": {"minimum": 1}}},
                            {"properties": {"skipped": {"minimum": 1}}},
                            {"properties": {"notRun": {"minimum": 1}}},
                        ]
                    }
                }
            },
        },
    ]

    artifacts = properties["artifactBindings"]
    artifacts["minItems"] = len(artifact_roles)
    artifacts["maxItems"] = len(artifact_roles)
    artifacts["allOf"] = [
        {"contains": {"properties": {"role": {"const": role}}, "required": ["role"]}}
        for role in artifact_roles
    ]
    return schema


def validate_command_summaries(manifest: dict[str, Any]) -> None:
    for command in manifest["commandBindings"]:
        summary = command.get("summary")
        if summary is None:
            continue
        total = summary["total"]
        component_total = summary["passed"] + summary["failed"] + summary["skipped"] + summary["notRun"]
        if total != component_total:
            raise ValueError(f"Command summary counter mismatch for {command['id']}: total={total}, components={component_total}")
        green = summary["passed"] == total and summary["failed"] == summary["skipped"] == summary["notRun"] == 0
        if (command["result"] == "pass") != green:
            raise ValueError(f"Command summary result/counter mismatch for {command['id']}")


def build_outputs(
    require_outputs: bool = False,
    *,
    require_current_receipt_manifest: bool = False,
) -> tuple[dict[Path, bytes], dict[str, Any]]:
    validate_repository_path(RC1_JSON, require_file=True, purpose="immutable rc.1 manifest")
    validate_repository_path(RC1_APPROVAL, require_file=True, purpose="immutable rc.1 approval")
    validate_repository_path(RC1_SCHEMA, require_file=True, purpose="immutable rc.1 schema")
    if sha256_bytes(RC1_JSON.read_bytes()) != RC1_SHA256:
        raise ValueError("Immutable rc.1 manifest bytes changed")
    if sha256_bytes(RC1_APPROVAL.read_bytes()) != RC1_APPROVAL_SHA256:
        raise ValueError("Immutable rc.1 detached approval bytes changed")

    validate_changed_path_boundary(require_outputs)
    overlay_bindings = source_bindings()
    digest_value = candidate_digest(overlay_bindings)
    source_state = f"{BASE_COMMIT}+content-addressed-overlay-sha256:{digest_value}"
    build_receipt = validated_build_evidence(digest_value)
    restore_receipt = validated_restore_evidence(digest_value)
    build_result = build_receipt["build"]
    assembly = build_receipt["assembly"]

    v3 = load_v3_generator()
    rc1 = json.loads(RC1_JSON.read_text(encoding="utf-8"))
    semantic, current_ids, current_results, summary = validated_receipt(
        v3,
        digest_value,
        assembly,
        require_current_manifest=require_current_receipt_manifest,
    )
    semantic_bytes = canonical_json(semantic)
    rc1_ids = rc1["testDenominator"]["currentCandidate"]["testIds"]
    if current_ids != rc1_ids or v3.list_digest(current_ids) != rc1["testDenominator"]["currentCandidate"]["testIdsSha256"]:
        raise ValueError("The exact ordered 473-test identity set changed from rc.1")

    inventory_module = v3.load_inventory_module()
    inventory = inventory_module.extract_inventory(ROOT)
    inventory_rows = [dict(row) for kind in inventory_module.OBLIGATION_KINDS for row in inventory[kind]]
    mappings = v3.category_mappings(current_ids, {row["id"] for row in inventory_rows})
    obligations, dispositions, orphan_ids = v3.build_obligation_closures(inventory_module, inventory, mappings, current_ids)
    if len(obligations) != 969 or len(dispositions) != 277 or orphan_ids:
        raise ValueError(f"Preservation denominator changed: obligations={len(obligations)}, dispositions={len(dispositions)}, orphans={orphan_ids}")
    if [obligation_projection(row) for row in obligations] != [obligation_projection(row) for row in rc1["obligations"]]:
        raise ValueError("Canonical obligation classification or closure projection changed from rc.1")
    if dispositions != rc1["governedDispositions"]:
        raise ValueError("Canonical governed-disposition projection changed from rc.1")
    if mappings != rc1["categoryMappings"]:
        raise ValueError("Canonical category-mapping projection changed from rc.1")

    manifest = copy.deepcopy(rc1)
    manifest["$schema"] = RC2_SCHEMA.name
    manifest["manifestVersion"] = VERSION
    manifest["approval"] = {
        "state": "pending",
        "requestedRole": "Owner",
        "approver": None,
        "approvedAtUtc": None,
        "approvalReference": None,
        "signature": None,
    }
    manifest["lineage"]["predecessor"] = binding(RC1_JSON, "effective-approved-rc1-predecessor")
    manifest["lineage"]["predecessorVersion"] = "3.0.0-rc.1"
    manifest["lineage"]["predecessorApproval"] = binding(RC1_APPROVAL, "effective-detached-rc1-approval")
    manifest["lineage"]["supersessionBoundary"] = (
        "Effective detached approval remains scoped to immutable 3.0.0-rc.1 bytes only; "
        "this current 3.0.0-rc.2 successor is unapproved and inherits no approval, waiver, activation, or release effect."
    )
    manifest["sourceBinding"]["ownerReviewOverlay"] = {
        "candidateKind": CANDIDATE_KIND,
        "baseCommit": BASE_COMMIT,
        "bindings": overlay_bindings,
        "sourceInputPathInventorySha256": path_inventory_digest(SOURCE_INPUT_PATHS),
        "assessmentWorkflowRecords": ASSESSMENT_WORKFLOW_RECORD_PATHS,
        "assessmentWorkflowRecordPathInventorySha256": path_inventory_digest(ASSESSMENT_WORKFLOW_RECORD_PATHS),
        "assessmentRecordsExcludedFromCandidateDigest": True,
        "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
        "candidateDigest": digest_value,
        "generatedOutputs": GENERATED_OUTPUT_PATHS,
        "generatedOutputPathInventorySha256": path_inventory_digest(GENERATED_OUTPUT_PATHS),
        "purpose": "Bind the exact rc.2 source overlay separately from mutable assessment/workflow records and generated build/test evidence, without rewriting rc.1.",
    }

    artifact_specs: list[tuple[Path, str]] = [
        (RC2_SCHEMA, "v3-rc2-machine-contract"),
        (RC1_JSON, "effective-approved-rc1-predecessor"),
        (RC1_MARKDOWN, "immutable-rc1-projection"),
        (RC1_DIGEST, "immutable-rc1-digest"),
        (RC1_APPROVAL, "effective-detached-rc1-approval"),
        (RC1_APPROVAL_DIGEST, "immutable-rc1-approval-digest"),
        (ROOT / "docs/release-evidence/release-baseline-v1.json", "v1-14-suite-214-test-authority"),
        (ROOT / "docs/release-evidence/success-metric-report-and-attestation-v1.json", "later-approved-384-test-report"),
        (ROOT / "docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json", "later-approved-384-test-decision"),
        (ROOT / "docs/release-evidence/final-conformance-contract-diff-v1.json", "signed-release-conformance-evidence"),
        (ROOT / "docs/release-evidence/projection-read-store-population-proof-v2-binding-resolution.json", "non-approval-historical-binding-resolution"),
        (ROOT / "_bmad-output/implementation-artifacts/apphost-runtime-boundary-working-tree-evidence-v2.json", "historical-runtime-boundary-evidence"),
        (TOOLCHAIN_PATH, "rc2-candidate-toolchain-capture"),
        (RESTORE_LOG_PATH, "rc2-candidate-restore-log"),
        (RESTORE_INVENTORY_PATH, "rc2-candidate-restore-dependency-inventory"),
        (RESTORE_RECEIPT_PATH, "rc2-candidate-restore-receipt"),
        (BUILD_LOG_PATH, "rc2-remediation-build-log"),
        (BUILD_RECEIPT_PATH, "rc2-candidate-build-receipt"),
        (SEMANTIC_RESULTS_PATH, "rc2-remediation-semantic-conformance-result"),
    ]
    artifact_roles = [role for _, role in artifact_specs]
    schema = build_schema(len(overlay_bindings), artifact_roles, source_state)
    schema_bytes = canonical_json(schema)
    jsonschema.Draft202012Validator.check_schema(schema)
    manifest["sourceBinding"]["schema"] = {
        "path": f"docs/release-evidence/{RC2_SCHEMA.name}",
        "sha256": sha256_bytes(schema_bytes),
    }
    manifest["artifactBindings"] = []
    for path, role in artifact_specs:
        if path == RC2_SCHEMA:
            manifest["artifactBindings"].append(bytes_binding(path, schema_bytes, role))
        elif path == SEMANTIC_RESULTS_PATH:
            manifest["artifactBindings"].append(bytes_binding(path, semantic_bytes, role))
        else:
            manifest["artifactBindings"].append(binding(path, role))

    manifest["buildBindings"][1].update(
        {
            "id": "owner-review-overlay-conformance-build",
            "sourceState": source_state,
            "result": build_result["result"],
            "warnings": build_result["warnings"],
            "errors": build_result["errors"],
            "log": binding(BUILD_LOG_PATH, "build-log"),
            "dotnetSdk": restore_receipt["toolchain"]["sdkVersion"],
            "sdkCommit": restore_receipt["toolchain"]["sdkCommit"],
            "msbuild": restore_receipt["toolchain"]["msbuildVersion"],
            "toolchainCapture": binding(TOOLCHAIN_PATH, "toolchain-identity"),
            "conformanceAssemblySha256": assembly["sha256"],
            "assemblySourceRevisionId": assembly["sourceRevisionId"],
            "toolchainSdkVersion": restore_receipt["toolchain"]["sdkVersion"],
            "toolchainMsbuildVersion": restore_receipt["toolchain"]["msbuildVersion"],
            "restoreReceiptSha256": sha256_bytes(RESTORE_RECEIPT_PATH.read_bytes()),
            "dependencyGraphSha256": restore_receipt["restore"]["dependencyInventory"]["sha256"],
            "buildReceiptSha256": sha256_bytes(BUILD_RECEIPT_PATH.read_bytes()),
        }
    )
    manifest["commandBindings"][0].update(
        {
            "command": "dotnet --info",
            "sourceState": source_state,
            "result": "pass",
        }
    )
    manifest["commandBindings"][1].update(
        {
            "command": " ".join(restore_receipt["restore"]["command"]),
            "sourceState": source_state,
            "result": "pass",
        }
    )
    manifest["commandBindings"][0]["sourceState"] = source_state
    manifest["commandBindings"][4].update(
        {
            "command": " ".join(build_receipt["build"]["command"]),
            "sourceState": source_state,
            "result": build_result["result"],
        }
    )
    run_result = "pass" if summary["failed"] == 0 and summary["skipped"] == 0 and summary["notRun"] == 0 else "fail"
    manifest["commandBindings"][5].update(
        {
            "command": (
                "uv run --frozen python3 _bmad/scripts/generate_preservation_traceability_manifest_v3_rc2.py "
                "--run-conformance"
            ),
            "sourceState": source_state,
            "result": run_result,
            "summary": summary,
        }
    )

    current = manifest["testDenominator"]["currentCandidate"]
    current.update(
        {
            "approvalState": "pending-owner-approval",
            "sourceState": source_state,
            "testCount": summary["total"],
            "passed": summary["passed"],
            "failed": summary["failed"],
            "skipped": summary["skipped"],
            "notRun": summary["notRun"],
            "testIds": current_ids,
            "testIdsSha256": v3.list_digest(current_ids),
            "failedTestIds": [test_id for test_id in current_ids if current_results[test_id] == "Fail"],
        }
    )
    manifest["categoryMappings"] = mappings
    manifest["obligations"] = obligations
    manifest["governedDispositions"] = dispositions
    manifest["zeroOrphanProof"].update(
        {
            "actualObligationCount": len(obligations),
            "closureCount": sum(1 for row in obligations if row["closure"]),
            "pendingGovernedDispositionCount": len(dispositions),
            "orphanIds": orphan_ids,
            "duplicateObligationIds": [],
            "missingSourceIds": [],
            "missingOriginalV1TestIds": [],
            "missingApprovedCumulativeTestIds": [],
        }
    )
    manifest["gateStates"]["FR-20"]["reason"] = "The exact rc.2 successor is unapproved and all release gates are not complete."
    manifest["gateStates"]["SM-C1"]["reason"] = "The exact additive rc.2 successor remains pending independent Owner approval."
    manifest["limitations"] = [
        "This 3.0.0-rc.2 successor is an exact base-plus-content-addressed-overlay candidate and is unapproved; rc.1 approval does not transfer.",
        "A green 473-test run establishes Conformance for this candidate but does not approve FR-20 or SM-C1.",
        "SM-C2 remains FAILED under the universal every-path <=5% rule.",
        "OQ-1 remains BLOCKED and the implementation hold remains ACTIVE.",
        "No legacy obligation, grant, release, waiver, signature, or authority expansion is activated or inferred.",
    ]

    validate_command_summaries(manifest)
    jsonschema.validate(manifest, schema)
    json_bytes = canonical_json(manifest)
    json_hash = sha256_bytes(json_bytes)
    markdown = (
        "# Preservation Traceability Manifest v3 — Remediated Successor\n\n"
        f"- Version: `{VERSION}`\n"
        "- Status: `pending-owner-approval`\n"
        f"- Source candidate: `{CANDIDATE_DIGEST_ALGORITHM}:{digest_value}`\n"
        f"- Canonical JSON SHA-256: `{json_hash}`\n"
        "- Predecessor: approved immutable `3.0.0-rc.1`\n"
        "- Successor approval: `pending`\n\n"
        "The successor retains exactly 473 fully qualified test IDs, the 214-test v1 floor, the 384-test approved cumulative floor, "
        f"89 pending additions, seven category mappings, 969 obligation identities, and 277 governed dispositions. The bound Conformance run is "
        f"{summary['passed']}/{summary['total']} with {summary['failed']} failed, {summary['skipped']} skipped, and {summary['notRun']} not-run tests.\n\n"
        "Gate state remains fail-closed: FR-20 `PENDING`, SM-C1 `PENDING`, SM-C2 `FAILED`, OQ-1 `BLOCKED`, implementation hold `ACTIVE`. "
        "Green validation is not approval and does not activate legacy requirements, grants, release authority, waiver, signature, or ownership.\n"
    ).encode()
    digest = (
        f"{json_hash}  {RC2_JSON.name}\n"
        f"{sha256_bytes(markdown)}  {RC2_MARKDOWN.name}\n"
        f"{sha256_bytes(schema_bytes)}  {RC2_SCHEMA.name}\n"
    ).encode()
    outputs = {
        SEMANTIC_RESULTS_PATH: semantic_bytes,
        RC2_SCHEMA: schema_bytes,
        RC2_JSON: json_bytes,
        RC2_MARKDOWN: markdown,
        RC2_DIGEST: digest,
    }
    return outputs, {"candidateDigest": digest_value, "summary": summary, "runResult": run_result}


def require_green_summary(summary: dict[str, Any]) -> None:
    if not (
        summary["total"] == 473
        and summary["passed"] == 473
        and summary["failed"] == 0
        and summary["skipped"] == 0
        and summary["notRun"] == 0
    ):
        raise ValueError(f"Derived Conformance run is not 473/473 green: {summary}")


def detached_index_outputs(core_outputs: dict[Path, bytes], metadata: dict[str, Any]) -> dict[Path, bytes]:
    stale_core = [
        path.relative_to(ROOT).as_posix()
        for path, data in core_outputs.items()
        if not path.exists() or path.read_bytes() != data
    ]
    if stale_core:
        raise ValueError(
            "Final manifest/core evidence must already be byte-exact before detached finalization: "
            + ", ".join(stale_core)
        )
    require_green_summary(metadata["summary"])
    artifacts = [
        binding(RC2_JSON, "exact-final-manifest-tested-by-receipt"),
        binding(RC2_MARKDOWN, "final-manifest-projection"),
        binding(RC2_SCHEMA, "final-manifest-schema"),
        binding(RC2_DIGEST, "final-manifest-digest"),
        binding(RUN_RECEIPT_PATH, "runner-owned-final-manifest-receipt"),
        binding(XML_PATH, "runner-owned-raw-xunit-result"),
        binding(SEMANTIC_RESULTS_PATH, "stable-semantic-test-result"),
        binding(BUILD_LOG_PATH, "candidate-build-log"),
        binding(BUILD_RECEIPT_PATH, "candidate-build-receipt"),
        binding(TOOLCHAIN_PATH, "candidate-toolchain-capture"),
        binding(RESTORE_LOG_PATH, "candidate-restore-log"),
        binding(RESTORE_INVENTORY_PATH, "candidate-restore-dependency-inventory"),
        binding(RESTORE_RECEIPT_PATH, "candidate-restore-receipt"),
    ]
    receipt = json.loads(RUN_RECEIPT_PATH.read_text(encoding="utf-8"))
    final_manifest = artifacts[0]
    if receipt["preRunManifest"] != {**final_manifest, "role": "pre-run-final-manifest"}:
        # Compare path/hash/bytes while keeping the distinct receipt-local role explicit.
        comparable = {key: receipt["preRunManifest"][key] for key in ("path", "sha256", "bytes")}
        expected = {key: final_manifest[key] for key in ("path", "sha256", "bytes")}
        if comparable != expected or receipt["preRunManifest"].get("role") != "pre-run-final-manifest":
            raise ValueError("Final receipt does not bind the exact final manifest")
    index = {
        "schemaVersion": "1.0.0",
        "candidateDigestAlgorithm": CANDIDATE_DIGEST_ALGORITHM,
        "candidateDigest": metadata["candidateDigest"],
        "candidateKind": CANDIDATE_KIND,
        "authorityEffect": "none",
        "approvalState": "not-approved",
        "artifacts": artifacts,
        "testSummary": metadata["summary"],
        "statement": (
            "Detached post-run evidence only: the runner receipt proves the exact final manifest and DLL it tested; "
            "this index grants no approval, release, waiver, activation, or hold lift."
        ),
    }
    index_bytes = canonical_json(index)
    digest_bytes = f"{sha256_bytes(index_bytes)}  {DETACHED_INDEX_PATH.name}\n".encode()
    return {DETACHED_INDEX_PATH: index_bytes, DETACHED_DIGEST_PATH: digest_bytes}


def main() -> int:
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Require all generated outputs and byte-check them.")
    mode.add_argument("--run-conformance", action="store_true", help="Run the exact bound assembly and write runner-owned XML/receipt evidence.")
    mode.add_argument("--run-build", action="store_true", help="Run the exact restore-bound build and write generator-owned log/receipt evidence.")
    mode.add_argument("--capture-restore-evidence", action="store_true", help="Capture pinned toolchain, restore, and dependency-graph evidence.")
    mode.add_argument("--finalize-detached-index", action="store_true", help="Bind the exact final manifest/receipt/XML without rewriting the manifest.")
    mode.add_argument("--print-candidate-digest", action="store_true", help="Print the exact frozen source-overlay digest without requiring evidence.")
    parser.add_argument("--require-green", action="store_true", help="Fail unless the derived semantic result is exactly 473/473 green.")
    args = parser.parse_args()
    if args.print_candidate_digest:
        if args.require_green:
            parser.error("--print-candidate-digest cannot be combined with --require-green")
        validate_changed_path_boundary(False)
        print(candidate_digest(source_bindings()))
        return 0
    if args.run_conformance:
        if args.require_green:
            parser.error("--run-conformance derives its result and cannot be combined with --require-green")
        validate_changed_path_boundary(False)
        return run_conformance(candidate_digest(source_bindings()))
    if args.run_build:
        if args.require_green:
            parser.error("--run-build cannot be combined with --require-green")
        return run_build(candidate_digest(source_bindings()))
    if args.capture_restore_evidence:
        if args.require_green:
            parser.error("--capture-restore-evidence cannot be combined with --require-green")
        return capture_restore_evidence(candidate_digest(source_bindings()))
    if args.finalize_detached_index:
        core_outputs, metadata = build_outputs(False, require_current_receipt_manifest=True)
        detached_outputs = detached_index_outputs(core_outputs, metadata)
        for path, data in detached_outputs.items():
            atomic_write(path, data)
            print(f"WROTE {path.relative_to(ROOT)} sha256={sha256_bytes(data)}")
        print(f"DETACHED PASS candidate={metadata['candidateDigest']} receipt-proves-final-manifest=true")
        return 0

    outputs, metadata = build_outputs(args.check, require_current_receipt_manifest=args.check)
    summary = metadata["summary"]
    if args.require_green:
        try:
            require_green_summary(summary)
        except ValueError as error:
            print(f"FAIL {error}")
            return 1
    if args.check:
        outputs.update(detached_index_outputs(outputs, metadata))
        stale = [path.relative_to(ROOT).as_posix() for path, data in outputs.items() if not path.exists() or path.read_bytes() != data]
        if stale:
            print("STALE " + ", ".join(stale))
            return 1
        print(
            "PASS preservation-traceability-manifest-v3-rc2 byte-exact check "
            f"candidate={metadata['candidateDigest']} result={metadata['runResult']} "
            f"passed={summary['passed']}/{summary['total']}"
        )
        return 0
    for path, data in outputs.items():
        atomic_write(path, data)
        print(f"WROTE {path.relative_to(ROOT)} sha256={sha256_bytes(data)}")
    print(f"CANDIDATE {CANDIDATE_DIGEST_ALGORITHM}:{metadata['candidateDigest']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
