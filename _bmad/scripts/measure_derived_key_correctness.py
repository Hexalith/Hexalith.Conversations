#!/usr/bin/env python3
"""Build and measure Story 12.3 from a fresh copy of a committed source tree."""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

import generate_derived_key_correctness_proof as proof


def run(directory: Path, *command: str, env: dict[str, str] | None = None, timeout: int = 300) -> None:
    subprocess.run(command, cwd=directory, env=env, stdin=subprocess.DEVNULL, check=True, timeout=timeout)


def git_text(directory: Path, *arguments: str) -> str:
    return proof.git(directory, *arguments).stdout.decode("utf-8").strip()


def assert_snapshot(directory: Path, commit: str) -> None:
    if git_text(directory, "rev-parse", "HEAD") != commit or git_text(
        directory, "status", "--porcelain", "--ignore-submodules=all"
    ):
        raise ValueError(f"Build input changed in {directory} at {commit}.")


def checkout_snapshot(repository: Path, checkout: Path, commit: str, submodules: list[dict]) -> None:
    # Share only local object databases, including for direct dependencies with shallow history.
    git_env = proof.records.git_environment()
    run(
        repository, "git", "clone", "--quiet", "--shared", "--no-checkout", "--", str(repository), str(checkout),
        env=git_env,
    )
    run(checkout, "git", "checkout", "--quiet", "--detach", commit, env=git_env)
    for submodule in submodules:
        path = submodule["path"]
        source = (repository / path).resolve()
        target = checkout / path
        if Path(git_text(source, "rev-parse", "--show-toplevel")).resolve() != source:
            raise ValueError(f"Direct submodule checkout is unavailable at {path}.")
        target.mkdir(parents=True, exist_ok=True)
        run(target, "git", "init", "--quiet", env=git_env)
        objects = git_text(source, "rev-parse", "--path-format=absolute", "--git-path", "objects")
        (target / ".git/objects/info/alternates").write_text(objects + "\n", encoding="utf-8")
        shallow = Path(git_text(source, "rev-parse", "--path-format=absolute", "--git-path", "shallow"))
        if shallow.is_file():
            shutil.copyfile(shallow, target / ".git/shallow")
        tags = proof.git(source, "for-each-ref", "--format=create %(refname) %(objectname)", "refs/tags").stdout
        subprocess.run(
            ("git", "update-ref", "--stdin"), cwd=target, input=tags, env=git_env, check=True, timeout=30,
        )
        run(target, "git", "remote", "add", "origin", submodule["url"], env=git_env)
        run(target, "git", "checkout", "--quiet", "--detach", submodule["gitlink"], env=git_env)


def verify_fault_matrix_result(path: Path) -> None:
    """Require an executed, all-passing pytest JUnit report."""
    try:
        root = ET.parse(path).getroot()
        if root.tag not in ("testsuites", "testsuite"):
            raise ValueError("Fault-matrix report is not a JUnit test suite.")
        suites = root.findall("testsuite") if root.tag == "testsuites" else [root]
        total = 0
        for suite in suites:
            cases = suite.findall("testcase")
            if int(suite.get("tests", "0")) != len(cases) or any(
                int(suite.get(counter, "0")) != 0 for counter in ("errors", "failures", "skipped")
            ) or any(case.find(tag) is not None for case in cases for tag in ("error", "failure", "skipped")):
                raise ValueError("Fault-matrix JUnit report contains inconsistent counts or non-passing tests.")
            total += len(cases)
        if total == 0:
            raise ValueError("Fault-matrix JUnit report contains no tests.")
    except ET.ParseError as error:
        raise ValueError(f"Cannot read the fault-matrix JUnit report: {error}") from None


def measure(repository: Path, checkout: Path, commit: str, scratch: Path, output: Path) -> None:
    submodules = proof.read_root_submodules(repository, commit)
    checkout_snapshot(repository, checkout, commit, submodules)

    def verify_sources() -> None:
        assert_snapshot(checkout, commit)
        for submodule in submodules:
            assert_snapshot(checkout / submodule["path"], submodule["gitlink"])

    verify_sources()
    run(checkout, "dotnet", "tool", "restore")
    preparation = subprocess.run(
        (
            "dotnet", "tool", "run", "hexalith-module", "--", "prepare-build", "--manifest",
            "module/hexalith.module-manifest.v2.json", "--mode", "source", "--workspace",
            str(scratch / "mapping"), "--output", "json",
        ),
        cwd=checkout,
        stdin=subprocess.DEVNULL,
        check=True,
        timeout=300,
        capture_output=True,
    )
    environment = os.environ | json.loads(preparation.stdout)["buildPreparation"]["environment"]
    if environment.get("Configuration") != proof.BUILD_CONFIGURATION:
        raise ValueError("Source mapping did not select the contracted build configuration.")
    verify_sources()
    run(checkout, "dotnet", "build", "Hexalith.Conversations.slnx", "-m:1", env=environment, timeout=900)
    verify_sources()

    receipt_path = checkout / proof.BUILD_RECEIPT_PATH
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    lane_results = {
        "unit": "AC-12.3-02.trx",
        "integration": "integration.trx",
        "dapr": "dapr.trx",
    }
    lanes = {}
    for lane, filename in lane_results.items():
        test_assembly = checkout / proof.lane_assembly(lane)
        server_assembly = test_assembly.with_name(proof.SERVER_ASSEMBLY)
        runtime_config = test_assembly.with_suffix(".runtimeconfig.json")
        if json.loads(runtime_config.read_bytes())["runtimeOptions"]["tfm"] != proof.TARGET_FRAMEWORK:
            raise ValueError(f"{lane} did not build for the contracted target framework.")
        binary_digests = {
            "testAssemblySha256": proof.sha256_bytes(test_assembly.read_bytes()),
            "serverAssemblySha256": proof.sha256_bytes(server_assembly.read_bytes()),
        }
        result = receipt_path.with_name(filename)
        test = proof.EXPECTED_LANE_TESTS[lane][0]
        selector = ("-method", test) if lane == "unit" else ("-class", test.rpartition(".")[0])
        run(
            checkout, "dotnet", str(test_assembly), "-automated", "sync", "-failSkips", *selector,
            "-trx", str(result), env=environment,
        )
        if (
            proof.sha256_bytes(test_assembly.read_bytes()) != binary_digests["testAssemblySha256"]
            or proof.sha256_bytes(server_assembly.read_bytes()) != binary_digests["serverAssemblySha256"]
        ):
            raise ValueError(f"{lane} binaries changed during execution.")
        lanes[lane] = binary_digests | {"trxSha256": proof.sha256_bytes(result.read_bytes())}

    extra_scenarios = (
        (
            checkout / proof.lane_assembly("unit"),
            "Hexalith.Conversations.Server.Tests.Projections.DerivedKeyOptimizationTests.ListAndOpenShouldUseApprovedBoundedReads",
            "AC-12.3-01.trx",
        ),
        (
            checkout / "tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net11.0/Hexalith.Conversations.Contracts.Tests.dll",
            "Hexalith.Conversations.Contracts.Tests.PublicQueryCompatibilityTests.DerivedKeyOptimizationShouldPreserveQueryContract",
            "AC-12.3-05.trx",
        ),
    )
    for assembly, method, filename in extra_scenarios:
        run(
            checkout, "dotnet", str(assembly), "-automated", "sync", "-failSkips", "-method", method,
            "-trx", str(receipt_path.with_name(filename)), env=environment,
        )

    verify_sources()
    receipt_path.write_bytes(proof.canonical({
        "commit": commit,
        "tree": git_text(checkout, "rev-parse", f"{commit}^{{tree}}"),
        "configuration": proof.BUILD_CONFIGURATION,
        "targetFramework": proof.TARGET_FRAMEWORK,
        "lanes": lanes,
    }))
    command = (
        sys.executable, "_bmad/scripts/generate_derived_key_correctness_proof.py",
        "--repository", ".", "--contract", proof.CONTRACT_PATH,
        "--unit-result", receipt_path.with_name(lane_results["unit"]).relative_to(checkout).as_posix(),
        "--integration-result", receipt_path.with_name(lane_results["integration"]).relative_to(checkout).as_posix(),
        "--dapr-result", receipt_path.with_name(lane_results["dapr"]).relative_to(checkout).as_posix(),
        "--output-json", proof.JSON_PATH, "--output-markdown", proof.MARKDOWN_PATH,
    )
    run(checkout, *command)
    rendered = {path: (checkout / path).read_bytes() for path in (proof.JSON_PATH, proof.MARKDOWN_PATH)}
    run(checkout, *command)
    if any((checkout / path).read_bytes() != raw for path, raw in rendered.items()):
        raise ValueError("Proof generation changed bytes on the second pass.")
    matrix_result = receipt_path.with_name("AC-12.3-07.xml")
    run(
        checkout, sys.executable, "-m", "pytest", "-q", "-p", "no:cacheprovider",
        "_bmad/scripts/tests/test_derived_key_correctness.py", "-k", "complete_fault_matrix",
        f"--junitxml={matrix_result}", env=environment, timeout=1200,
    )
    verify_fault_matrix_result(matrix_result)
    output.mkdir(parents=True)
    filenames = (*lane_results.values(), *(scenario[2] for scenario in extra_scenarios), matrix_result.name)
    files = (
        receipt_path, *(receipt_path.with_name(name) for name in filenames),
        checkout / proof.JSON_PATH, checkout / proof.MARKDOWN_PATH,
    )
    for file in files:
        shutil.copyfile(file, output / file.name)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, allow_abbrev=False)
    parser.add_argument("--repository", type=Path, default=Path.cwd())
    parser.add_argument("--output-directory", type=Path, required=True, help="A new directory for the measured proof and receipts.")
    arguments = parser.parse_args()
    repository = arguments.repository.resolve()
    output = arguments.output_directory.resolve()
    try:
        if arguments.output_directory.exists() or arguments.output_directory.is_symlink():
            raise ValueError("Choose a new output directory.")
        proof.ignore_replace_objects()
        commit = proof.resolve_commit(repository, "HEAD")
        with tempfile.TemporaryDirectory(prefix="story-12.3-") as scratch_dir:
            scratch = Path(scratch_dir)
            measure(repository, scratch / "source", commit, scratch, output)
    except (ValueError, OSError, KeyError, subprocess.SubprocessError, proof.Blocked, proof.ProofError) as error:
        print(f"Story 12.3 measurement stopped. {error}", file=sys.stderr)
        return 1
    print(f"Measured committed candidate {commit}. Results saved to {output}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
