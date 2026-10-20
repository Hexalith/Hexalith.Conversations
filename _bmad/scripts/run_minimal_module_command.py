#!/usr/bin/env python3
"""Measure a frozen Story 11.2 restore/build command and retain its console log and exit code.

Usage: python3 _bmad/scripts/run_minimal_module_command.py --repository . --scenario AC-11.2-02
The wrapper executes the contract command unchanged. Its JSON receipt has a .log extension because it
retains the complete combined console output alongside the measured status and build policy.
"""
from __future__ import annotations

import argparse
import json
import shlex
import subprocess
import sys
from pathlib import Path

import verify_minimal_module_fixture as verifier


def measure(repository: Path, scenario: str) -> dict:
    contract = json.loads(verifier.read_file(repository, verifier.CONTRACT_PATH))
    command = next(row["command"] for row in contract["scenarios"] if row["id"] == scenario)
    arguments = shlex.split(command)
    policy = "lockedMode" if scenario == "AC-11.2-02" else "warningsAsErrors"
    enabled = "--locked-mode" in arguments
    if scenario == "AC-11.2-03":
        enabled = True
        # Query each fixture project with the same configuration that the frozen build uses.
        for project in verifier.EXPECTED_PROJECTS:
            evaluated = subprocess.run(
                ["dotnet", "msbuild", f"{verifier.FIXTURE_ROOT}/{project}", "-property:Configuration=Release",
                 "-getProperty:TreatWarningsAsErrors"], cwd=repository, capture_output=True, text=True, check=False)
            enabled = enabled and evaluated.returncode == 0 and evaluated.stdout.strip().lower() == "true"
    result = subprocess.run(arguments, cwd=repository, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            text=True, check=False)
    return {"schemaVersion": verifier.COMMAND_RECEIPT_SCHEMA, "scenarioId": scenario, "command": command,
            "exitCode": result.returncode, "consoleLog": result.stdout, policy: enabled}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--scenario", required=True, choices=tuple(verifier.COMMAND_RECEIPTS))
    args = parser.parse_args(argv)
    repository = Path(args.repository)
    receipt = measure(repository, args.scenario)
    target = repository / verifier.COMMAND_RECEIPTS[args.scenario]
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(receipt["consoleLog"], end="")
    return receipt["exitCode"]


if __name__ == "__main__":
    sys.exit(main())
