"""Contained A/B outputs using the existing atomic replacement and rollback seam."""

from __future__ import annotations

import os
from pathlib import Path, PurePosixPath


class OutputError(Exception):
    """An output cannot be installed without crossing its ownership boundary."""


def output_target(repository: Path, path: str) -> Path:
    """Contain and type-check an output before either reading or replacing it."""
    import generate_story_record as records  # noqa: PLC0415

    try:
        repository = repository.resolve(strict=True)
        target = records.v2_output_target(repository, path)
        relative = target.relative_to(repository)
        if any((repository / parent).is_symlink() for parent in relative.parents):
            raise ValueError("output parent is a symlink")
        target = Path(os.path.abspath(target))
        target.relative_to(repository)
        return target
    except (OSError, ValueError, RuntimeError) as error:
        raise OutputError(f"invalid output destination: {error}") from None


def write_outputs(repository: Path, outputs: list[tuple[str, bytes]], readonly: tuple[str, ...] = (),
                  readonly_roots: tuple[str, ...] = ()) -> None:
    """Preflight the entire output set, then install it with the shared atomic writer.

    Existing tracked files are immutable unless the requested bytes already match;
    matching tracked files are retained without rewriting. Explicit inputs protect
    disposable repositories that have no Git index. Ordinary output hardlinks are
    replaced rather than followed. This is a static alias boundary, not a concurrent
    filesystem mutation guarantee.
    """
    import generate_story_record as records  # noqa: PLC0415

    try:
        repository = repository.resolve(strict=True)
        protected = {(repository / path).resolve() for path in readonly}
        protected_roots = {(repository / path).resolve() for path in readonly_roots}
        tracked = set()
        administration = set()
        if (repository / ".git").exists():
            tracked = {os.fsdecode(path) for path in records.run_git(repository, "ls-files", "-z").stdout.split(b"\0") if path}
            directories = os.fsdecode(records.run_git(repository, "rev-parse", "--absolute-git-dir", "--git-common-dir").stdout).splitlines()
            if len(directories) != 2:
                raise ValueError("cannot establish Git administration directories")
            administration = {(repository / path).resolve(strict=True) for path in directories}
        targets: list[tuple[Path, bytes]] = []
        seen: set[Path] = set()
        for path, content in outputs:
            target = output_target(repository, path)
            relative = target.relative_to(repository)
            if ".git" in relative.parts or any(root == target or root in target.parents for root in administration):
                raise ValueError("output targets Git administration")
            if target in seen:
                raise ValueError("output destinations are not distinct")
            seen.add(target)
            if (target in protected or any(root == target or root in target.parents for root in protected_roots)
                    or target.name in {"packages.lock.json", "project.assets.json", ".nupkg.metadata"}):
                raise ValueError("output targets a read-only input")
            if any(parent.as_posix() in tracked for parent in PurePosixPath(relative.as_posix()).parents):
                raise ValueError("output is inside a tracked dependency")
            if relative.as_posix() in tracked:
                if target.read_bytes() != content:
                    raise ValueError("output would change a tracked source or publication")
                continue
            targets.append((target, content))
        records.v2_write_outputs(targets)
    except (OSError, ValueError, RuntimeError, records.GateError, records.V2OutputDrift) as error:
        restoration = f"; {records.v2_restore_claim(error)}" if hasattr(error, "restored") else ""
        raise OutputError(f"cannot install outputs: {error}{restoration}") from None
