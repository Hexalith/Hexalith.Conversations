#!/usr/bin/env python3
"""Bind the October package refresh without rewriting accepted planning evidence.

An uncommitted preview is always BLOCKED. Committed inputs use the existing
single-parent C1 / record-only C2 pattern. This tool never commits or publishes.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
from pathlib import Path
import re
import sys
import tomllib
from typing import Any, Sequence
import xml.etree.ElementTree as ET


_helper_path = Path(__file__).with_name("publish_v18_package_environment_authority.py")
_HELPER_BYTES = _helper_path.read_bytes()
_helper_spec = importlib.util.spec_from_file_location("package_refresh_v18_helpers", _helper_path)
assert _helper_spec is not None and _helper_spec.loader is not None
helpers = importlib.util.module_from_spec(_helper_spec)
_helper_spec.loader.exec_module(helpers)

BASELINE = "8793d26306f91d2cfe00116dc0b8eec41ec8900f"
SCHEMA_VERSION = "hexalith.conversations.package-refresh-environment.v1"
RECORD_PATH = "_bmad-output/planning-artifacts/package-refresh-environment-2026-10-08.json"
SCHEMA_PATH = "_bmad/schemas/package-refresh-environment-v1.schema.json"
SCRIPT_PATH = "_bmad/scripts/check_package_refresh_environment.py"
TEST_PATH = "_bmad/scripts/tests/test_check_package_refresh_environment.py"
BUILDS_PATH = "references/Hexalith.Builds"
CATALOG_PATH = "Props/Directory.Packages.props"
AUDIT_PATH = "Tools/package-version-audit.json"
HELPER_PATH = "_bmad/scripts/publish_v18_package_environment_authority.py"
SOURCE_PATHS = tuple(sorted((
    ".github/workflows/ci.yml", "Directory.Packages.props", "global.json",
    "package.json", "package-lock.json", "pyproject.toml", "uv.lock",
    "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
    "tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs",
    SCHEMA_PATH, SCRIPT_PATH, TEST_PATH,
)))
C1_PATHS = tuple(sorted((
    ".github/workflows/ci.yml",
    "_bmad-output/implementation-artifacts/spec-update-all-packages.md",
    "package.json", "package-lock.json", "uv.lock", BUILDS_PATH,
    "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj",
    SCHEMA_PATH, SCRIPT_PATH, TEST_PATH,
)))
NPM_PINS = {
    "@commitlint/cli": "21.2.3", "@commitlint/config-conventional": "21.2.3",
    "@semantic-release/commit-analyzer": "13.0.1", "@semantic-release/exec": "7.1.0",
    "@semantic-release/github": "12.0.10",
    "@semantic-release/release-notes-generator": "14.1.1", "semantic-release": "25.0.9",
}
PYTHON_PINS = {"jsonschema": "4.26.0", "pytest": "9.1.1"}
PYTHON_PACKAGES = {
    "attrs": "26.1.0", "colorama": "0.4.6", "hexalith-conversations-planning": "0.0.0",
    "iniconfig": "2.3.1", "jsonschema": "4.26.0", "jsonschema-specifications": "2025.9.1",
    "packaging": "26.3", "pluggy": "1.6.0", "pygments": "2.21.0", "pytest": "9.1.1",
    "referencing": "0.37.0", "rpds-py": "2026.9.1", "typing-extensions": "4.16.0",
}
PYTHON_DEPENDENCIES = {
    "hexalith-conversations-planning": [{"name": "jsonschema"}, {"name": "pytest"}],
    "jsonschema": [{"name": name} for name in ("attrs", "jsonschema-specifications", "referencing", "rpds-py")],
    "jsonschema-specifications": [{"name": "referencing"}],
    "pytest": [{"name": "colorama", "marker": "sys_platform == 'win32'"},
               *({"name": name} for name in ("iniconfig", "packaging", "pluggy", "pygments"))],
    "referencing": [{"name": "attrs"}, {"name": "rpds-py"},
                    {"name": "typing-extensions", "marker": "python_full_version < '3.13'"}],
}
MICROSOFT_SERVICING_MEMBERS = frozenset((
    *(f"Microsoft.AspNetCore.{name}" for name in (
        "Authorization", "Authentication.Facebook", "Authentication.JwtBearer", "Authentication.MicrosoftAccount",
        "Authentication.Google", "Authentication.OpenIdConnect", "Components.Authorization", "Components.CustomElements",
        "Components.Web", "Components.WebAssembly", "Components.WebAssembly.Authentication", "Components.WebAssembly.Server",
        "Components.WebAssembly.DevServer", "DataProtection", "DataProtection.Abstractions", "Mvc.Testing", "OpenApi",
        "SignalR.Client", "SignalR.StackExchangeRedis", "TestHost",
    )),
    *(f"Microsoft.Extensions.{name}" for name in (
        "Configuration", "Configuration.Abstractions", "Configuration.Binder", "Configuration.FileExtensions",
        "Configuration.UserSecrets", "DependencyInjection", "DependencyInjection.Abstractions", "Diagnostics.Abstractions",
        "Hosting", "Hosting.Abstractions", "Http", "Identity.Stores", "Localization", "Localization.Abstractions",
        "Logging.Abstractions", "Options", "Options.ConfigurationExtensions", "Options.DataAnnotations",
    )),
    "System.Collections.Immutable", "System.Text.Json",
))
INDEPENDENT_CATALOG_PINS = {
    "Microsoft.AspNetCore.Identity": "2.3.13", "Microsoft.Extensions.Identity.Http": "10.0.9",
    "Microsoft.Extensions.Http.Resilience": "10.10.0", "Microsoft.Extensions.ServiceDiscovery": "10.10.0",
    "Microsoft.Extensions.TimeProvider.Testing": "10.10.0", "System.CommandLine": "2.0.12",
    "System.ComponentModel.Annotations": "5.0.0", "System.IdentityModel.Tokens.Jwt": "8.23.0",
    "System.Reactive": "7.0.0", "System.Threading.Tasks.Extensions": "4.6.3",
}
CATALOG_PINS = {
    "HotChocolate": "16.6.8", "Verify": "33.3.2", "Verify.XunitV3": "33.3.2",
    "Microsoft.Extensions.Http": "10.0.12", "Microsoft.NET.Test.Sdk": "18.10.1",
    "Microsoft.Extensions.Identity.Http": "10.0.9",
    "Aspire.Hosting": "13.6.1", "Aspire.Hosting.Testing": "13.6.1",
    "CommunityToolkit.Aspire.Hosting.Dapr": "13.6.0-preview.1.261001-0243",
}
IMMUTABLE = {
    "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json": "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea",
    "_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json": "5b71e6fbf8851f790af92f0a0d056d7f7e04b3b9749c3a2f3f4ef5f87312d45a",
    "_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json": "24891d990b399fd9526f864c3e8be2b188db728a398f09d9129731d3169f1d94",
}


class RefreshError(helpers.PackageAuthorityError):
    """A stable, non-success refresh result."""


def require(condition: bool, code: str, detail: str, state: str = "FAIL") -> None:
    """Reject a proven mismatch or an unavailable prerequisite."""
    if not condition:
        raise RefreshError(code, detail, state)


def contained_path(root: Path, relative: str) -> Path:
    """Reject every symlink component before accessing a contained path."""
    helpers.safe_path(relative)
    target = root
    require(not target.is_symlink(), "REFRESH_PATH_UNSAFE", relative, "BLOCKED")
    for component in Path(relative).parts:
        target /= component
        require(not target.is_symlink(), "REFRESH_PATH_UNSAFE", relative, "BLOCKED")
    return target


def worktree_bytes(root: Path, relative: str) -> bytes:
    """Read one regular contained file without following a symlink component."""
    target = contained_path(root, relative)
    require(target.is_file(), "REFRESH_INPUT_UNAVAILABLE", relative, "BLOCKED")
    return target.read_bytes()


def source_bytes(root: Path, relative: str, candidate: str | None) -> bytes:
    """Read working preview bytes or a canonical mode-100644 blob."""
    if candidate is None:
        return worktree_bytes(root, relative)
    mode, kind, _ = helpers.raw_tree_record(root, candidate, relative)
    require((mode, kind) == ("100644", "blob"), "REFRESH_SOURCE_MODE_DRIFT", relative)
    return helpers.candidate_blob(root, candidate, relative)


def git_text(root: Path, *arguments: str) -> str:
    """Use the existing bounded Git reader."""
    return helpers.run_git(root, *arguments).stdout.decode("utf-8", errors="strict").strip()


def builds_context(root: Path, candidate: str | None) -> tuple[bytes, dict[str, Any]]:
    """Observe only the declared root Builds worktree and its committed gitlink."""
    builds = contained_path(root, BUILDS_PATH)
    require(builds.is_dir(), "REFRESH_BUILDS_UNAVAILABLE", BUILDS_PATH, "BLOCKED")
    head = helpers.resolve_commit(builds, "HEAD", "REFRESH_BUILDS_UNAVAILABLE")
    mode, kind, recorded = helpers.raw_tree_record(root, candidate or "HEAD", BUILDS_PATH)
    require((mode, kind) == ("160000", "commit"), "REFRESH_GITLINK_MODE_DRIFT", BUILDS_PATH)
    catalog = (worktree_bytes(builds, CATALOG_PATH) if candidate is None
               else helpers.run_git(builds, "show", f"{recorded}:{CATALOG_PATH}").stdout)
    committed = helpers.run_git(builds, "show", f"{head}:{CATALOG_PATH}").stdout
    clean = not git_text(builds, "status", "--porcelain", "--untracked-files=all")
    remote_available = bool(git_text(builds, "branch", "-r", "--contains", head))
    if candidate is not None:
        validate_builds_audit(builds, recorded, catalog)
    return catalog, {
        "path": BUILDS_PATH, "catalogPath": CATALOG_PATH,
        "catalogSha256": helpers.sha256(catalog), "mode": mode,
        "recordedCommit": recorded, "headCommit": head,
        "clean": clean, "catalogCommitted": catalog == committed,
        "remoteAvailable": remote_available,
    }


def catalog_versions(catalog: bytes) -> dict[str, str]:
    """Read the fixed catalog's literal selections and declared default properties."""
    parsed = ET.fromstring(catalog.decode("utf-8-sig"))
    properties: dict[str, str | None] = {}
    for group in parsed.findall("PropertyGroup"):
        for prop in group:
            condition = prop.get("Condition")
            if condition is None or (condition == f"'$({prop.tag})' == ''" and prop.tag not in properties):
                properties[prop.tag] = prop.text
    rows: dict[str, str] = {}
    for item in parsed.iter("PackageVersion"):
        name, value = item.get("Include"), item.get("Version", "")
        if name is None:
            continue
        require(name not in rows, "REFRESH_CATALOG_DUPLICATE", name)
        if value.startswith("$(") and value.endswith(")"):
            value = properties.get(value[2:-1])
        require(isinstance(value, str) and bool(value), "REFRESH_CATALOG_VERSION_INVALID", name)
        rows[name] = value
    return rows


def validate_builds_audit(builds: Path, recorded: str, catalog: bytes) -> None:
    """Prove the existing owning audit binds this committed catalog and its ancestry."""
    audit = json.loads(source_bytes(builds, AUDIT_PATH, recorded))
    require(isinstance(audit, dict), "REFRESH_BUILDS_AUDIT_INVALID", "audit must be an object")
    require(audit.get("catalogPath") == CATALOG_PATH and audit.get("catalogRawSha256") == helpers.sha256(catalog),
            "REFRESH_BUILDS_AUDIT_CATALOG_DRIFT", "raw catalog binding")
    revision = audit.get("generatedFromRevision")
    require(isinstance(revision, str) and re.fullmatch(r"[0-9a-f]{40}", revision) is not None,
            "REFRESH_BUILDS_AUDIT_REVISION_INVALID", "canonical generatedFromRevision required")
    require(helpers.resolve_commit(builds, revision, "REFRESH_BUILDS_AUDIT_REVISION_UNAVAILABLE") == revision,
            "REFRESH_BUILDS_AUDIT_REVISION_INVALID", revision)
    helpers.require_ancestor(builds, revision, recorded, "REFRESH_BUILDS_AUDIT_REVISION_NOT_ANCESTOR")
    require(source_bytes(builds, CATALOG_PATH, revision) == catalog,
            "REFRESH_BUILDS_AUDIT_REVISION_CATALOG_DRIFT", "generated revision's exact catalog bytes")
    packages = audit.get("packages")
    require(isinstance(packages, list) and all(isinstance(row, dict) and isinstance(row.get("id"), str)
            and isinstance(row.get("selectedVersion"), str) for row in packages),
            "REFRESH_BUILDS_AUDIT_INVALID", "package selections must be objects")
    rows = catalog_versions(catalog)
    require(len(packages) == len(rows) and {row["id"]: row["selectedVersion"] for row in packages} == rows,
            "REFRESH_BUILDS_AUDIT_SELECTION_DRIFT", "complete selected catalog versions")


def validate_graph(sources: dict[str, bytes], catalog: bytes) -> None:
    """Validate exact direct pins, lock parity, release channels, and toolchains."""
    package = json.loads(sources["package.json"])
    lock = json.loads(sources["package-lock.json"])
    require(isinstance(package, dict) and isinstance(lock, dict), "REFRESH_INPUT_INVALID", "npm inputs must be objects")
    root_lock = lock["packages"][""]
    require(isinstance(root_lock, dict), "REFRESH_INPUT_INVALID", "root lock package must be an object")
    for section in ("dependencies", "optionalDependencies", "peerDependencies", "peerDependenciesMeta"):
        require(package.get(section, {}) == {} and root_lock.get(section, {}) == {},
                "REFRESH_NPM_SCOPE_DRIFT", f"unselected npm section: {section}")
    require(package.get("devDependencies") == NPM_PINS, "REFRESH_NPM_PIN_DRIFT", "direct pins")
    require(lock["packages"][""].get("devDependencies") == NPM_PINS,
            "REFRESH_NPM_LOCK_PARITY_DRIFT", "root lock pins")
    for name, version in NPM_PINS.items():
        require(lock["packages"].get(f"node_modules/{name}", {}).get("version") == version,
                "REFRESH_NPM_LOCK_PARITY_DRIFT", name)
    project = tomllib.loads(sources["pyproject.toml"].decode("utf-8"))
    require(sorted(project["project"]["dependencies"]) == sorted(f"{n}=={v}" for n, v in PYTHON_PINS.items()),
            "REFRESH_PYTHON_PIN_DRIFT", "direct pins")
    python_lock = tomllib.loads(sources["uv.lock"].decode("utf-8"))
    packages = python_lock["package"]
    require(len(packages) == len(PYTHON_PACKAGES) and {p["name"]: p["version"] for p in packages} == PYTHON_PACKAGES,
            "REFRESH_PYTHON_GRAPH_DRIFT", "complete locked graph")
    require(all(p.get("dependencies", []) == PYTHON_DEPENDENCIES.get(p["name"], []) for p in packages),
            "REFRESH_PYTHON_RELATIONSHIP_DRIFT", "fixed dependency edges and markers")
    project_rows = [p for p in packages if p["name"] == "hexalith-conversations-planning"]
    metadata = project_rows[0]["metadata"]["requires-dist"]
    require(metadata == [{"name": n, "specifier": f"=={v}"} for n, v in sorted(PYTHON_PINS.items())],
            "REFRESH_PYTHON_LOCK_PARITY_DRIFT", "requires-dist")
    require(json.loads(sources["global.json"])["sdk"]["version"] == "10.0.401",
            "REFRESH_SDK_DRIFT", "SDK pin")
    workflow = sources[".github/workflows/ci.yml"].decode("utf-8")
    require(re.findall(r"\buv==([0-9.]+)", workflow) == ["0.12.23", "0.12.23"],
            "REFRESH_UV_DRIFT", "both current CI pins")
    require(b'ShouldBe("10.0.401")' in sources["tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs"],
            "REFRESH_SDK_CONSUMER_DRIFT", "structural assertion")
    app_host = ET.fromstring(sources["src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj"])
    require(app_host.get("Sdk") == "Aspire.AppHost.Sdk/13.6.1"
            or any(s.get("Name") == "Aspire.AppHost.Sdk" and s.get("Version") == "13.6.1" for s in app_host.findall("Sdk")),
            "REFRESH_ASPIRE_DRIFT", "AppHost SDK alignment")
    require(catalog.startswith(b"\xef\xbb\xbf"), "REFRESH_CATALOG_BOM_DRIFT", "owning catalog BOM")
    rows = catalog_versions(catalog)
    require(all(rows.get(n) == v for n, v in CATALOG_PINS.items()), "REFRESH_CATALOG_PIN_DRIFT", "selected catalog versions")
    require(all(rows.get(name) == "10.0.12" for name in MICROSOFT_SERVICING_MEMBERS),
            "REFRESH_MICROSOFT_FAMILY_DRIFT", "coherent 10.0.x family")
    require(all(rows.get(name) == version for name, version in INDEPENDENT_CATALOG_PINS.items()),
            "REFRESH_CATALOG_CADENCE_DRIFT", "intentional independent servicing cadences")


def render(root: Path, candidate: str | None = None) -> dict[str, Any]:
    """Compute a closed, byte-bound preview or committed source projection."""
    if candidate is not None:
        candidate = helpers.resolve_commit(root, candidate, "REFRESH_CANDIDATE_UNAVAILABLE")
        parents = helpers.commit_parents(root, candidate, "REFRESH_HISTORY_UNAVAILABLE")
        require(len(parents) == 1, "REFRESH_C1_PARENT_INVALID", candidate, "BLOCKED")
        require(parents == (BASELINE,), "REFRESH_C1_PARENT_DRIFT", "C1 must directly follow the recorded baseline", "BLOCKED")
        helpers.require_ancestor(root, BASELINE, candidate, "REFRESH_BASELINE_NOT_ANCESTOR")
        require(source_bytes(root, HELPER_PATH, candidate) == _HELPER_BYTES,
                "REFRESH_HELPER_DRIFT", "executed V18 helper differs from the committed candidate")
        require(helpers.changed_paths(root, parents[0], candidate) == C1_PATHS,
                "REFRESH_C1_SCOPE_DRIFT", "exact approved C1 paths required")
        require(helpers.changed_gitlinks(root, parents[0], candidate) == (BUILDS_PATH,),
                "REFRESH_GITLINK_SCOPE_DRIFT", "one Builds gitlink required")
    sources = {p: source_bytes(root, p, candidate) for p in SOURCE_PATHS}
    catalog, builds = builds_context(root, candidate)
    validate_graph(sources, catalog)
    for path, digest in IMMUTABLE.items():
        require(helpers.sha256(source_bytes(root, path, candidate)) == digest,
                "REFRESH_PREDECESSOR_DRIFT", path)
    blockers = []
    if candidate is None:
        blockers.append({"code": "REFRESH_SOURCE_COMMIT_REQUIRED", "detail": "Preview bytes have no committed C1 candidate."})
    if not builds["clean"] or not builds["catalogCommitted"]:
        blockers.append({"code": "REFRESH_BUILDS_COMMIT_REQUIRED", "detail": "Commit the owning catalog and its generated audit separately."})
    if builds["headCommit"] != builds["recordedCommit"]:
        blockers.append({"code": "REFRESH_GITLINK_COMMIT_REQUIRED", "detail": "The committed root gitlink must equal the clean Builds HEAD."})
    if not builds["remoteAvailable"]:
        blockers.append({"code": "REFRESH_REMOTE_COMMIT_UNAVAILABLE", "detail": "No locally known remote-tracking ref contains Builds HEAD."})
    ledger = [{"id": name, "state": "PASS"} for name in (
        "REFRESH.DIRECT-LOCK-PARITY", "REFRESH.TOOLCHAIN-ALIGNMENT",
        "REFRESH.CATALOG-CHANNELS", "REFRESH.IMMUTABLE-PREDECESSORS", "REFRESH.EXACT-BYTE-INVENTORY",
    )]
    ledger.extend({"id": b["code"], "state": "BLOCKED"} for b in blockers)
    return {
        "schemaVersion": SCHEMA_VERSION, "baselineCommit": BASELINE,
        "candidateCommit": candidate,
        "bindingKind": "working-tree-preview" if candidate is None else "committed-candidate",
        "sourceBindings": [{"path": p, "mode": "100644", "sha256": helpers.sha256(sources[p])} for p in SOURCE_PATHS],
        "buildsCatalog": builds,
        "packages": {"npmDirect": NPM_PINS, "pythonDirect": PYTHON_PINS,
                     "pythonGraph": PYTHON_PACKAGES, "catalogSelected": CATALOG_PINS},
        "toolchain": {"dotnetSdk": "10.0.401", "uv": "0.12.23", "aspire": "13.6.1"},
        "immutableAuthorities": [{"path": p, "sha256": h} for p, h in sorted(IMMUTABLE.items())],
        "authorityEffect": {"executionAllowed": False, "ownerApprovalClaimed": False,
                            "releaseAuthorized": False, "pushAuthorized": False},
        "result": "BLOCKED" if blockers else "PASS", "assertionLedger": ledger, "blockers": blockers,
    }


def validate_schema(root: Path, document: dict[str, Any], candidate: str | None = None) -> None:
    """Validate the closed record with the source-bound schema."""
    try:
        import jsonschema
    except ImportError as error:
        raise RefreshError("REFRESH_TOOLING_UNAVAILABLE", str(error), "BLOCKED") from error
    schema = json.loads(source_bytes(root, SCHEMA_PATH, candidate))
    try:
        jsonschema.Draft202012Validator.check_schema(schema)
        jsonschema.Draft202012Validator(schema).validate(document)
    except jsonschema.exceptions.SchemaError as error:
        raise RefreshError("REFRESH_SCHEMA_INVALID", str(error), "BLOCKED") from error
    except jsonschema.exceptions.ValidationError as error:
        raise RefreshError("REFRESH_RECORD_SCHEMA_INVALID", error.message) from error


def check(root: Path, document: dict[str, Any]) -> dict[str, Any]:
    """Recompute every declared byte binding; never accept a self-declared ledger."""
    require(isinstance(document, dict), "REFRESH_RECORD_SCHEMA_INVALID", "record must be an object")
    candidate = document.get("candidateCommit")
    require((document.get("bindingKind") == "working-tree-preview" and candidate is None)
            or (document.get("bindingKind") == "committed-candidate"
                and isinstance(candidate, str) and re.fullmatch(r"[0-9a-f]{40}", candidate) is not None),
            "REFRESH_RECORD_SCHEMA_INVALID", "binding kind and canonical candidate must agree")
    validate_schema(root, document, candidate)
    expected = render(root, document["candidateCommit"])
    require(document == expected, "REFRESH_RECORD_DRIFT", "record is not its deterministic projection")
    if document["candidateCommit"] is not None and document["result"] == "PASS":
        publications = git_text(root, "log", "--format=%H", "--diff-filter=A", "HEAD", "--", RECORD_PATH).splitlines()
        require(len(publications) == 1, "REFRESH_C2_PUBLICATION_REQUIRED", RECORD_PATH, "BLOCKED")
        publication = publications[0]
        helpers.require_single_parent(root, publication, document["candidateCommit"], "REFRESH_C2_PARENT_DRIFT")
        require(helpers.changed_paths(root, document["candidateCommit"], publication) == (RECORD_PATH,),
                "REFRESH_C2_SCOPE_DRIFT", "record-only direct child required")
        require(source_bytes(root, RECORD_PATH, publication) == helpers.json_bytes(document),
                "REFRESH_C2_BYTES_DRIFT", RECORD_PATH)
        require(source_bytes(root, RECORD_PATH, "HEAD") == helpers.json_bytes(document),
                "REFRESH_RECORD_DESCENDANT_DRIFT", RECORD_PATH)
    return expected


def main(arguments: Sequence[str] | None = None) -> int:
    """Generate a truthful preview or validate the exact candidate-bound record."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=".")
    parser.add_argument("--candidate")
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(arguments)
    try:
        root = Path(args.repository).resolve(strict=True)
        require(not (args.write and args.check), "REFRESH_ARGUMENT_INVALID", "choose write or check", "BLOCKED")
        if args.check:
            record = worktree_bytes(root, RECORD_PATH)
            document = json.loads(record)
            require(record == helpers.json_bytes(document), "REFRESH_RECORD_BYTES_NONCANONICAL", RECORD_PATH)
            document = check(root, document)
            require(record == helpers.json_bytes(document), "REFRESH_RECORD_BYTES_NONCANONICAL", RECORD_PATH)
        else:
            document = render(root, args.candidate)
            validate_schema(root, document, args.candidate)
            if args.write:
                target = contained_path(root, RECORD_PATH)
                if target.exists():
                    previous = json.loads(worktree_bytes(root, RECORD_PATH))
                    require(isinstance(previous, dict) and previous.get("bindingKind") == "working-tree-preview",
                            "REFRESH_ACCEPTED_RECORD_IMMUTABLE", RECORD_PATH, "BLOCKED")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(helpers.json_bytes(document))
                if document["result"] == "PASS":
                    raise RefreshError("REFRESH_C2_PUBLICATION_REQUIRED", "Generated source projection; record-only C2 is not yet committed.", "BLOCKED")
            elif document["result"] == "PASS":
                document = check(root, document)
        print(helpers.json_bytes(document).decode("utf-8"), end="")
        return 0 if document["result"] == "PASS" else 1
    except helpers.PackageAuthorityError as error:
        print(json.dumps({"result": error.state, "assertionLedger": [{"id": error.code, "state": error.state}],
                          "blockers": [{"code": error.code, "detail": error.detail}]}))
        return 1
    except (OSError, UnicodeError) as error:
        print(json.dumps({"result": "BLOCKED", "assertionLedger": [{"id": "REFRESH_IO_UNAVAILABLE", "state": "BLOCKED"}],
                          "blockers": [{"code": "REFRESH_IO_UNAVAILABLE", "detail": str(error)}]}))
        return 1
    except (ValueError, KeyError, TypeError, AttributeError, IndexError, ET.ParseError) as error:
        print(json.dumps({"result": "FAIL", "assertionLedger": [{"id": "REFRESH_INPUT_INVALID", "state": "FAIL"}],
                          "blockers": [{"code": "REFRESH_INPUT_INVALID", "detail": str(error)}]}))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
