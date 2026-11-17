#!/usr/bin/env python3
"""Run the built seven-case fixture and verify native xUnit 4.0.3 JSON in memory."""

from __future__ import annotations

import json
import os
from pathlib import Path
import signal
import subprocess
import sys

from verify_results import expected_tests

ROOT = Path(__file__).resolve().parents[3]
ASSEMBLY = ROOT / "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net11.0/Hexalith.Conversations.MinimalModule.Tests.dll"


def _unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate xUnit JSON key")
        result[key] = value
    return result


def verify_records(records: list[dict]) -> None:
    """Reject undiscovered, missing, duplicated, skipped, failed or erroneous results."""
    expected = expected_tests()
    started = {}
    passed, finished = set(), set()
    assembly, discoveries, completions = None, 0, 0
    for message in records:
        if not isinstance(message, dict) or not isinstance(message.get("$type"), str):
            raise ValueError("invalid native xUnit JSON message")
        kind = message["$type"]
        if kind in {"error", "fatal-error", "test-failed", "test-skipped", "test-not-run"} or kind.endswith("-cleanup-failure"):
            raise ValueError("compatibility result contains an unsuccessful native xUnit message: " + kind)
        if kind == "test-assembly-starting":
            if assembly is not None or not isinstance(message.get("AssemblyUniqueID"), str):
                raise ValueError("compatibility result must contain exactly one assembly")
            assembly = message["AssemblyUniqueID"]
        if kind == "discovery-complete":
            discoveries += 1
            if type(message.get("TestCasesToRun")) is not int or message["TestCasesToRun"] != len(expected):
                raise ValueError("compatibility discovery differs from the seven required assertions")
        if kind == "test-starting":
            identity, name = message.get("TestUniqueID"), message.get("TestDisplayName")
            if not isinstance(identity, str) or identity in started or name not in expected or name in started.values():
                raise ValueError("compatibility assertion identity is missing, duplicated or unexpected")
            started[identity] = name
        if kind in {"test-passed", "test-finished"}:
            identity = message.get("TestUniqueID")
            seen = passed if kind == "test-passed" else finished
            if identity not in started or identity in seen or message.get("AssemblyUniqueID") != assembly:
                raise ValueError("compatibility result is unbound or duplicated")
            seen.add(identity)
        if kind == "test-assembly-finished":
            completions += 1
            if (message.get("AssemblyUniqueID") != assembly or type(message.get("TestsTotal")) is not int
                    or message["TestsTotal"] != len(expected)
                    or any(type(message.get(key)) is not int or message[key] != 0
                           for key in ("TestsFailed", "TestsNotRun", "TestsSkipped"))):
                raise ValueError("compatibility assembly counters are not seven complete passing tests")
    if (assembly is None or discoveries != 1 or completions != 1 or set(started.values()) != expected
            or passed != set(started) or finished != set(started)):
        raise ValueError("expected exactly seven distinct completed passing compatibility assertions")


def main() -> int:
    if not ASSEMBLY.is_file():
        raise ValueError("the root solution must build the compatibility fixture before this action")
    command = ["dotnet", str(ASSEMBLY), "-automated", "sync", "-failSkips", "-noAutoReporters", "-reporter", "json"]
    with subprocess.Popen(command, cwd=ROOT, text=True, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, start_new_session=True) as process:
        try:
            output, errors = process.communicate(timeout=240)
        except BaseException:
            os.killpg(process.pid, signal.SIGKILL)
            process.wait()
            raise
        if process.returncode:
            print(output, end="", file=sys.stderr)
            print(errors, end="", file=sys.stderr)
            return process.returncode if process.returncode > 0 else 128 - process.returncode
    if errors:
        raise ValueError("native compatibility runner emitted stderr: " + errors)
    messages = [json.loads(line, object_pairs_hook=_unique) for line in output.splitlines() if line.strip()]
    verify_records(messages)
    print("Minimal-module platform compatibility: 7 passed, 0 failed/skipped/errors")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
