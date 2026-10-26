"""Bounded repository reads anchored to no-follow directory descriptors.

No process execution, path-based content opens, or unsupported-platform fallback.
All directory descriptors stay open until the read finishes, so replacing an
ancestor cannot redirect an open to a symlink target outside the chosen root.
"""
from __future__ import annotations

import errno
import os
from pathlib import Path, PurePosixPath
import stat


class ReadError(Exception):
    """A stable distinction between invalid inputs and unavailable safe reads."""

    def __init__(self, code: str, detail: str, *, unavailable: bool = False):
        super().__init__(detail)
        self.code, self.unavailable = code, unavailable


def relative_path(value: str) -> tuple[str, ...]:
    """Reject lexical escapes and Git administration paths without target reads."""
    path = PurePosixPath(value)
    if (not value or not path.parts or path.is_absolute() or path.as_posix() != value
            or "\\" in value or ":" in value
            or any(part in (".", "..", ".git") for part in path.parts)):
        raise ReadError("PATH_INVALID", f"not a contained repository-relative input: {value}")
    return path.parts


def secure_open_supported() -> bool:
    """Require the exact descriptor/no-follow facilities used by this reader."""
    return (os.name == "posix" and all(hasattr(os, name) for name in (
        "O_DIRECTORY", "O_NOFOLLOW", "O_NONBLOCK", "O_CLOEXEC"))
        and os.open in os.supports_dir_fd and os.stat in os.supports_dir_fd
        and os.stat in os.supports_follow_symlinks)


def _identity(value: os.stat_result) -> tuple[int, int]:
    return value.st_dev, value.st_ino


def _version(value: os.stat_result) -> tuple[int, int, int, int, int]:
    return (*_identity(value), value.st_size, value.st_mtime_ns, value.st_ctime_ns)


class ContainedReader:
    """Own a repository descriptor and securely read regular contained files."""

    def __init__(self, repository: Path, *, limit: int = 8 * 1024 * 1024):
        self.repository = repository.absolute()
        self.limit = limit
        self._root_fds: list[int] = []
        self._root_entries: list[tuple[int, str, os.stat_result]] = []

    def _open(self, name: str, parent: int | None, *, directory: bool) -> tuple[int, os.stat_result]:
        flags = os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK
        if directory:
            flags |= os.O_DIRECTORY
        try:
            before = os.stat(name, dir_fd=parent, follow_symlinks=False)
            if stat.S_ISLNK(before.st_mode):
                raise ReadError("PATH_ESCAPE", f"symlink input forbidden: {name}")
            if directory and not stat.S_ISDIR(before.st_mode):
                raise ReadError("INPUT_NOT_REGULAR", f"input ancestor is not a directory: {name}")
            if not directory and not stat.S_ISREG(before.st_mode):
                raise ReadError("INPUT_NOT_REGULAR", f"input is not a regular file: {name}")
            descriptor = os.open(name, flags, dir_fd=parent)
            try:
                opened = os.fstat(descriptor)
                if _identity(before) != _identity(opened):
                    raise ReadError("INPUT_CHANGED", f"input replaced while opening: {name}", unavailable=True)
                return descriptor, opened
            except BaseException:
                os.close(descriptor)
                raise
        except OSError as error:
            if error.errno in (errno.ELOOP, errno.ENOTDIR):
                raise ReadError("PATH_ESCAPE", f"symlink or replaced ancestor refused: {name}: {error}") from error
            code = "SECURE_OPEN_UNAVAILABLE" if error.errno in (
                errno.ENOSYS, errno.EOPNOTSUPP, errno.EINVAL) else "INPUT_UNAVAILABLE"
            raise ReadError(code, f"cannot securely open {name}: {error}", unavailable=True) from error

    def __enter__(self) -> ContainedReader:
        if not secure_open_supported():
            raise ReadError("SECURE_OPEN_UNAVAILABLE", "descriptor-relative no-follow reads are unavailable", unavailable=True)
        try:
            descriptor, _ = self._open(os.sep, None, directory=True)
            self._root_fds.append(descriptor)
            for name in self.repository.parts[1:]:
                child, observed = self._open(name, descriptor, directory=True)
                self._root_entries.append((descriptor, name, observed))
                self._root_fds.append(child)
                descriptor = child
            return self
        except BaseException:
            self.__exit__(None, None, None)
            raise

    def __exit__(self, *_args) -> None:
        for descriptor in reversed(self._root_fds):
            os.close(descriptor)
        self._root_fds.clear()
        self._root_entries.clear()

    @staticmethod
    def _unchanged(entries: list[tuple[int, str, os.stat_result]]) -> None:
        for parent, name, expected in entries:
            current = os.stat(name, dir_fd=parent, follow_symlinks=False)
            if _identity(current) != _identity(expected) or stat.S_ISLNK(current.st_mode):
                raise ReadError("INPUT_CHANGED", f"input entry replaced during read: {name}", unavailable=True)

    def read(self, relative: str) -> bytes:
        """Read at most limit bytes and reject replacements or unstable versions."""
        parts = relative_path(relative)
        descriptors: list[int] = []
        entries = list(self._root_entries)
        try:
            parent = self._root_fds[-1]
            for name in parts[:-1]:
                child, observed = self._open(name, parent, directory=True)
                descriptors.append(child)
                entries.append((parent, name, observed))
                parent = child
            leaf, before = self._open(parts[-1], parent, directory=False)
            descriptors.append(leaf)
            entries.append((parent, parts[-1], before))
            if before.st_size > self.limit:
                raise ReadError("INPUT_TOO_LARGE", f"input exceeds {self.limit} bytes: {relative}")
            chunks, size = [], 0
            while True:
                block = os.read(leaf, min(1024 * 1024, self.limit + 1 - size))
                if not block:
                    break
                chunks.append(block)
                size += len(block)
                if size > self.limit:
                    raise ReadError("INPUT_TOO_LARGE", f"input exceeds {self.limit} bytes: {relative}")
            if _version(before) != _version(os.fstat(leaf)):
                raise ReadError("INPUT_CHANGED", f"input changed during read: {relative}", unavailable=True)
            self._unchanged(entries)
            return b"".join(chunks)
        except OSError as error:
            raise ReadError("INPUT_UNAVAILABLE", f"cannot securely read {relative}: {error}", unavailable=True) from error
        finally:
            for descriptor in reversed(descriptors):
                os.close(descriptor)
