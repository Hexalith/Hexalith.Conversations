#!/usr/bin/env python3
"""Check that root submodule declarations match the Git index."""

from __future__ import annotations

import argparse
from pathlib import Path
import re
import subprocess
import sys


SHA = re.compile(r"[0-9a-f]{40}\Z")
SETTING = re.compile(r"submodule\.(.+)\.(path|url)\Z")


def git(root: Path, *args: str) -> bytes:
    result = subprocess.run(
        ["git", "-C", str(root), *args], capture_output=True, check=False
    )
    if result.returncode:
        detail = result.stderr.decode("utf-8", errors="replace").strip()
        raise ValueError(detail or f"git {args[0]} exited {result.returncode}")
    return result.stdout


def check(root: Path) -> list[str]:
    """Return current-tree problems without reading history or submodule worktrees."""

    problems: list[str] = []
    gitmodules = root / ".gitmodules"
    if not gitmodules.is_file():
        return ["root .gitmodules is missing"]

    settings: dict[str, dict[str, str]] = {}
    for entry in git(
        root,
        "config",
        "--null",
        "--no-includes",
        "--file",
        str(gitmodules),
        "--get-regexp",
        r"^submodule\..*\.(path|url)$",
    ).split(b"\0"):
        if not entry:
            continue
        key, separator, value = entry.partition(b"\n")
        match = SETTING.fullmatch(key.decode("utf-8")) if separator else None
        if match is None:
            problems.append("malformed .gitmodules setting")
            continue
        name, field = match.groups()
        fields = settings.setdefault(name, {})
        if field in fields:
            problems.append(f"duplicate {field} for submodule {name}")
        fields[field] = value.decode("utf-8")

    declared: set[str] = set()
    for name, fields in settings.items():
        path = fields.get("path", "")
        if (
            not path.startswith("references/")
            or path == "references/"
            or ".." in Path(path).parts
        ):
            problems.append(f"invalid root submodule path for {name}: {path!r}")
            continue
        if path in declared:
            problems.append(f"duplicate root submodule path: {path}")
        declared.add(path)
        if not fields.get("url", "").strip():
            problems.append(f"missing URL for {path}")
    if not declared:
        problems.append("no root submodules declared")

    indexed: set[str] = set()
    for entry in git(root, "ls-files", "--stage", "-z", "--", "references/").split(b"\0"):
        if not entry:
            continue
        metadata, separator, path_bytes = entry.partition(b"\t")
        parts = metadata.decode("ascii", errors="replace").split()
        if not separator or len(parts) != 3:
            problems.append("malformed Git index entry under references/")
            continue
        mode, object_id, stage = parts
        path = path_bytes.decode("utf-8", errors="replace")
        if stage != "0" or mode != "160000" or SHA.fullmatch(object_id) is None:
            problems.append(f"{path}: expected a resolved mode-160000 gitlink")
        indexed.add(path)

    for path in sorted(declared - indexed):
        problems.append(f"{path}: declared but no gitlink is indexed")
    for path in sorted(indexed - declared):
        problems.append(f"{path}: indexed under references/ but not declared")
    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path.cwd())
    root = parser.parse_args().repository.resolve()
    try:
        problems = check(root)
    except (OSError, UnicodeError, ValueError) as error:
        problems = [str(error)]
    for problem in problems:
        print(f"root submodules: {problem}", file=sys.stderr)
    if problems:
        return 1
    print("root submodules: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
