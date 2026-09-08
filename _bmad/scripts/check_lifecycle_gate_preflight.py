#!/usr/bin/env python3
"""Fail loudly when an installed lifecycle route has lost its evidence gate.

Commit `1c36c45` reinstalled the vendored BMAD skill trees and silently erased the
project-owned `V12 lifecycle evidence gates` sections. Nothing rejected that wipe at the
moment it happened: it was discovered only later, through red CI, after every lifecycle
transition had already been left ungated.

This check is the loud failure that was missing. It reads only the installed working
tree -- no Git history, no baseline, no candidate -- so it can run immediately after a
BMAD upgrade, from a pre-commit hook, and in CI. It never repairs anything: an upgrade
that disarms a gate must stop the person doing the upgrade, not be silently patched.

Exit codes: `0` when every declared route is gated, `1` on any finding, `2` on usage or
inventory errors.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
from pathlib import Path
import sys
from typing import Any


VERIFIER_PATH = Path(__file__).resolve().parent / "verify_evidence_boundary.py"


def load_verifier() -> Any:
    """Load the frozen inventory from its single owner rather than restating it."""

    spec = importlib.util.spec_from_file_location("verify_evidence_boundary", VERIFIER_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {VERIFIER_PATH}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def inventory_error(verifier: Any) -> str | None:
    """Return why the frozen structures cannot be walked, or None when they agree.

    Indexing straight into them would raise IndexError or KeyError, and a traceback exits `1` --
    indistinguishable at the hook and CI boundary from a legitimate gate finding. Disagreement
    between the two frozen structures is a usage error, like the duplicate check beside it.
    """

    paths = verifier.ACTIVE_ROUTE_PATHS
    if len(paths) != len(set(paths)):
        return "frozen route inventory has duplicates"
    if any("/skills/" not in path for path in paths):
        return "frozen route inventory has a path outside a skill tree"
    logical = {path.split("/skills/", 1)[1] for path in paths}
    missing = sorted(logical - set(verifier.LIFECYCLE_TOKENS))
    if missing:
        return f"frozen routes have no lifecycle token: {', '.join(missing)}"
    unused = sorted(set(verifier.LIFECYCLE_TOKENS) - logical)
    if unused:
        return f"lifecycle tokens name no frozen route: {', '.join(unused)}"
    return None


def check_routes(root: Path, verifier: Any) -> list[dict[str, str]]:
    """Report one finding per installed route that cannot enforce its gate."""

    findings: list[dict[str, str]] = []
    marker = verifier.GATE_MARKER
    content_by_path: dict[str, str] = {}
    for path in verifier.ACTIVE_ROUTE_PATHS:
        logical = path.split("/skills/", 1)[1]
        try:
            text = (root / path).read_text(encoding="utf-8")
        except OSError as error:
            findings.append(
                {
                    "code": "LIFECYCLE_ROUTE_ABSENT",
                    "path": path,
                    "detail": f"declared route is not installed: {error}",
                }
            )
            continue
        content_by_path[path] = text
        occurrences = text.count(marker)
        if occurrences != 1:
            findings.append(
                {
                    "code": "LIFECYCLE_GATE_MISSING",
                    "path": path,
                    "detail": f"expected exactly one {marker!r} section, found {occurrences}",
                }
            )
            continue
        lifecycle_token = verifier.LIFECYCLE_TOKENS[logical]
        lifecycle = text.find(lifecycle_token)
        if lifecycle < 0:
            findings.append(
                {
                    "code": "LIFECYCLE_TOKEN_ABSENT",
                    "path": path,
                    "detail": f"frozen lifecycle status write {lifecycle_token!r} is gone",
                }
            )
            continue
        gate = text.index(marker)
        if gate > lifecycle:
            findings.append(
                {
                    "code": "LIFECYCLE_GATE_DISPLACED",
                    "path": path,
                    "detail": "gate section no longer precedes the lifecycle status write",
                }
            )
            continue
        required = (
            "verify_submodule_promotion.py",
            "verify_evidence_boundary.py",
            "PASS",
            "FAIL",
            "BLOCKED",
            "not-applicable",
        )
        missing = [token for token in required if token not in text[gate:]]
        if missing:
            findings.append(
                {
                    "code": "LIFECYCLE_GATE_GUTTED",
                    "path": path,
                    "detail": f"gate section lost required text: {', '.join(missing)}",
                }
            )
    for logical in verifier.LOGICAL_ROUTE_PATHS:
        agents_path = f".agents/skills/{logical}"
        claude_path = f".claude/skills/{logical}"
        if agents_path in content_by_path and claude_path in content_by_path:
            if content_by_path[agents_path] != content_by_path[claude_path]:
                findings.append(
                    {
                        "code": "LIFECYCLE_MIRROR_DRIFT",
                        "path": logical,
                        "detail": "the .agents and .claude copies are no longer identical",
                    }
                )
    return findings


def check_orphan_gates(root: Path, verifier: Any) -> list[dict[str, str]]:
    """Report gate sections outside the frozen inventory.

    An upgrade that renames or relocates a route leaves the gate text sitting in a file the
    frozen inventory no longer names. The declared routes would still pass, so the drift has
    to be reported from the other direction.
    """

    declared = set(verifier.ACTIVE_ROUTE_PATHS)
    findings: list[dict[str, str]] = []
    for tree in (".agents/skills", ".claude/skills"):
        base = root / tree
        if not base.is_dir():
            continue
        for candidate in sorted(base.rglob("*.md")):
            relative = candidate.relative_to(root).as_posix()
            if relative in declared:
                continue
            try:
                text = candidate.read_text(encoding="utf-8")
            except (OSError, UnicodeError) as error:
                findings.append(
                    {
                        "code": "LIFECYCLE_TREE_UNREADABLE",
                        "path": relative,
                        "detail": f"cannot be scanned for a gate section: {error}",
                    }
                )
                continue
            if verifier.GATE_MARKER in text:
                findings.append(
                    {
                        "code": "LIFECYCLE_GATE_UNDECLARED_ROUTE",
                        "path": relative,
                        "detail": "carries a gate section but is not in the frozen route inventory",
                    }
                )
    return findings


def check_context_workflows(root: Path, verifier: Any) -> list[dict[str, str]]:
    """Report context workflows that lost their governing identity requirements.

    The same upgrade that erased the route gates stripped these tokens, so leaving them out here
    would let half the regression class keep surfacing only as red CI.
    """

    findings: list[dict[str, str]] = []
    required = ("overlay_version", "architecture_version", "frontmatter")
    for logical in verifier.CONTEXT_WORKFLOW_PATHS:
        contents: dict[str, str] = {}
        for tree in (".agents/skills", ".claude/skills"):
            relative = f"{tree}/{logical}"
            try:
                contents[relative] = (root / relative).read_text(encoding="utf-8")
            except (OSError, UnicodeError) as error:
                findings.append(
                    {
                        "code": "LIFECYCLE_CONTEXT_WORKFLOW_ABSENT",
                        "path": relative,
                        "detail": f"declared context workflow is not readable: {error}",
                    }
                )
        for relative, text in contents.items():
            missing = [token for token in required if token not in text]
            if missing:
                findings.append(
                    {
                        "code": "LIFECYCLE_CONTEXT_IDENTITY_STRIPPED",
                        "path": relative,
                        "detail": f"lost required identity text: {', '.join(missing)}",
                    }
                )
        if len(contents) == 2 and len(set(contents.values())) != 1:
            findings.append(
                {
                    "code": "LIFECYCLE_MIRROR_DRIFT",
                    "path": logical,
                    "detail": "the .agents and .claude copies are no longer identical",
                }
            )
    return findings


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--repository", default=".", help="repository root to inspect")
    parser.add_argument("--json", action="store_true", help="emit the findings as JSON")
    arguments = parser.parse_args(argv)

    root = Path(arguments.repository).resolve()
    if not root.is_dir():
        print(f"lifecycle-gate preflight: {root} is not a directory", file=sys.stderr)
        return 2
    try:
        verifier = load_verifier()
    except Exception as error:  # pragma: no cover - environment failure
        print(f"lifecycle-gate preflight: {error}", file=sys.stderr)
        return 2
    problem = inventory_error(verifier)
    if problem is not None:
        print(f"lifecycle-gate preflight: {problem}", file=sys.stderr)
        return 2

    findings = (
        check_routes(root, verifier)
        + check_context_workflows(root, verifier)
        + check_orphan_gates(root, verifier)
    )
    result = "FAIL" if findings else "PASS"
    if arguments.json:
        print(
            json.dumps(
                {
                    "result": result,
                    "repository": root.as_posix(),
                    "routes": len(verifier.ACTIVE_ROUTE_PATHS),
                    "contextWorkflows": len(verifier.CONTEXT_WORKFLOW_PATHS),
                    "findings": findings,
                },
                indent=2,
            )
        )
    elif findings:
        print(
            "lifecycle-gate preflight: FAIL -- the installed skill trees no longer enforce the",
            file=sys.stderr,
        )
        print(
            "V12 lifecycle evidence gates. A BMAD upgrade most likely overwrote them. Restore",
            file=sys.stderr,
        )
        print("the gates before committing; do not weaken the inventory to match.", file=sys.stderr)
        for finding in findings:
            print(f"  {finding['code']} {finding['path']}: {finding['detail']}", file=sys.stderr)
    else:
        print(
            f"lifecycle-gate preflight: PASS -- {len(verifier.ACTIVE_ROUTE_PATHS)} declared routes"
            f" gated, {len(verifier.CONTEXT_WORKFLOW_PATHS)} context workflows carry their identities"
        )
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
