"""Hermetic shell-environment guards for declared conformance execution."""

from __future__ import annotations

import importlib.util
from pathlib import Path

import pytest


ROOT = Path(__file__).resolve().parents[3]
_specification = importlib.util.spec_from_file_location(
    "conformance_environment_verifier", ROOT / "_bmad/scripts/verify_conformance_tiering.py",
)
assert _specification is not None and _specification.loader is not None
story92 = importlib.util.module_from_spec(_specification)
_specification.loader.exec_module(story92)


def _shell_environment_workflow() -> str:
    """Supply the supported declared lane without depending on current CI policy."""
    policy = story92.document(ROOT, story92.AMENDMENT)["executionPolicy"]
    commands = ["set -euo pipefail", "mkdir -p TestResults/conformance"]
    commands.extend(f'{variable}="{Path(project).parent}/bin/Release/net10.0/{Path(project).stem}.dll"'
                    for variable, project in (("portable", story92.PROJECTS["portable"]),
                                              ("internal", story92.PROJECTS["module-internal"])))
    for tier, variable in (("portable", "portable"), ("module-internal", "internal")):
        command = f'uv run --frozen --no-sync dotnet "${variable}" -failSkips'
        if tier == "portable":
            command += "".join(f' -class- "{name}"' for name in
                               [*policy["exclusions"]["classes"], policy["historicalControlClass"]])
            command += "".join(f' -method- "{name}"' for name in policy["exclusions"]["methods"])
        commands.append(command + f" -parallelMode none -result-trx TestResults/conformance/{variable}.trx -noLogo")
    lines = ["name: Synthetic shell environment fixture", "on:", "  push:", "    branches: [main]",
             "  pull_request:", "    branches: [main]", "jobs:", "  repository:",
             "    runs-on: ubuntu-latest", "    steps: []", "  conformance:",
             "    runs-on: ubuntu-latest", "    steps:", "      - name: Initialize .NET",
             "        uses: actions/setup-dotnet@v6.0.0"]
    for tier, name in (("portable", "Build portable conformance project"),
                       ("module-internal", "Build conformance project")):
        lines += [f"      - name: {name}", "        run: |",
                  f"          dotnet build {story92.PROJECTS[tier]} --configuration Release -warnaserror"]
    lines += ["      - name: Run current conformance checks", "        shell: bash", "        run: |",
              *("          " + command for command in commands),
              "      - name: Verify complete monotonic tier execution", "        run: |",
              "          uv run --frozen --no-sync python3 _bmad/scripts/verify_conformance_tiering.py"
              f" --repository . --contract {story92.CONTRACT} --current-tree"
              " --portable-result TestResults/conformance/portable.trx"
              " --internal-result TestResults/conformance/internal.trx --output TestResults/conformance/tiering.json",
              "      - name: Prove structural and execution faults", "        run: |",
              "          uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_conformance_tiering.py"
              ' -k "structural_and_execution_faults or current_tree" --junitxml=TestResults/conformance/faults.xml']
    return "\n".join(lines) + "\n"


@pytest.mark.parametrize("current_tree", [False, True])
@pytest.mark.parametrize("scope,declaration", [
    ("workflow", "env:\n  BASH_ENV: /tmp/override.sh\n"),
    ("workflow", "env: { BASH_ENV: /tmp/override.sh }\n"),
    ("workflow", "'env': { BASH_ENV: /tmp/override.sh }\n"),
    ("workflow", "env: ${{ vars.SHELL_ENVIRONMENT }}\n"),
    ("workflow", "? env\n: { BASH_ENV: /tmp/override.sh }\n"),
    ("workflow", "&environment env: { BASH_ENV: /tmp/override.sh }\n"),
    ("workflow", "!!str env: { BASH_ENV: /tmp/override.sh }\n"),
    ("workflow", "*environment: { BASH_ENV: /tmp/override.sh }\n"),
    ("job", "    env:\n      BASH_ENV: /tmp/override.sh\n"),
    ("job", "    env: { 'BASH_ENV': /tmp/override.sh }\n"),
    ("job", "    env: ${{ vars.SHELL_ENVIRONMENT }}\n"),
    ("job", "    env: { HARMLESS: value }\n"),
    ("job", "    container: { image: ubuntu:24.04, env: { BASH_ENV: /tmp/override.sh } }\n"),
    ("job", '    container: { image: ubuntu:24.04, options: "--env BASH_ENV=/tmp/override.sh" }\n'),
    ("job", "    container:\n      image: ubuntu:24.04\n      env:\n          BASH_ENV: /tmp/override.sh\n"),
    ("Build portable conformance project", "        env:\n          BASH_ENV: /tmp/override.sh\n"),
    ("Build conformance project", "        env: { BASH_ENV: /tmp/override.sh }\n"),
    ("Run current conformance checks", "        env: ${{ vars.SHELL_ENVIRONMENT }}\n"),
    ("Verify complete monotonic tier execution", "        env: { BASH_ENV: /tmp/override.sh }\n"),
    ("Prove structural and execution faults", "        env: { BASH_ENV: /tmp/override.sh }\n"),
    ("Initialize .NET", "        env: { BASH_ENV: /tmp/override.sh }\n"),
    ("Initialize .NET", "        'env': { BASH_ENV: /tmp/override.sh }\n"),
    ("Initialize .NET", '        "e\\u006ev": { BASH_ENV: /tmp/override.sh }\n'),
    ("Initialize .NET", "        <<: { env: { BASH_ENV: /tmp/override.sh } }\n"),
    ("Initialize .NET", "        &environment env: { BASH_ENV: /tmp/override.sh }\n"),
    ("Initialize .NET", "        !!str env: { BASH_ENV: /tmp/override.sh }\n"),
    ("Initialize .NET", "        *environment: { BASH_ENV: /tmp/override.sh }\n"),
    ("setup", "      - env: { BASH_ENV: /tmp/override.sh }\n        run: echo setup\n"),
    ("setup", "      - { name: Setup, env: { BASH_ENV: /tmp/override.sh }, run: echo setup }\n"),
    ("setup", "      - ? env\n        : { BASH_ENV: /tmp/override.sh }\n        run: echo setup\n"),
    ("setup", '      - name: Set startup hook\n        run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      - run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      -   run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      -\trun: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      -   name: Setup\n          run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      -   name: Setup\n          \'run\': echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      -   name: Setup\n          <<: { run: \'echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\' }\n'),
    ("setup", "      -   name: Setup\n          uses: actions/setup-dotnet@v6.0.0\n          env: { BASH_ENV: /tmp/override.sh }\n"),
    ("setup", '    - run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '        - run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      - # setup\n          run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      - \n          run: echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", '      -\n          "run": echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'),
    ("setup", "      - # setup\n          uses: actions/setup-dotnet@v6.0.0\n          env: { BASH_ENV: /tmp/override.sh }\n"),
])
def test_current_tree_declarations_reject_shell_environment_overrides(
    monkeypatch: pytest.MonkeyPatch, current_tree: bool, scope: str, declaration: str,
) -> None:
    """Inherited or ambiguous mappings cannot alter the verified Bash startup."""
    original_read = story92.read
    workflow = _shell_environment_workflow()
    monkeypatch.setattr(story92, "read", lambda root, path: workflow.encode()
                        if path == story92.TIERING.CI_WORKFLOW_PATH else original_read(root, path))
    assert story92.declarations(ROOT, current_tree=current_tree)
    if scope == "workflow":
        changed = declaration + workflow
    else:
        before, job = workflow.split("  conformance:\n", 1)
        if scope == "job":
            job = declaration + job
        elif scope == "setup":
            job = job.replace("    steps:\n", "    steps:\n" + declaration, 1)
        else:
            marker = "      - name: " + scope + "\n"
            assert marker in job
            job = job.replace(marker, marker + declaration, 1)
        changed = before + "  conformance:\n" + job
    assert changed != workflow
    monkeypatch.setattr(story92, "read", lambda root, path: changed.encode()
                        if path == story92.TIERING.CI_WORKFLOW_PATH else original_read(root, path))
    with pytest.raises(story92.VerificationError) as failure:
        story92.declarations(ROOT, current_tree=current_tree)
    assert failure.value.code == "TIER_NOT_DECLARED"


@pytest.mark.parametrize("current_tree", [False, True])
def test_current_tree_declarations_preserve_other_job_environment(
    monkeypatch: pytest.MonkeyPatch, current_tree: bool,
) -> None:
    """A separate job's mappings do not enter the conformance shell environment."""
    original_read = story92.read
    workflow = _shell_environment_workflow().encode()
    changed = workflow.replace(b"  repository:\n", b"  repository:\n    env: { BASH_ENV: unrelated.sh }\n", 1)
    assert changed != workflow
    monkeypatch.setattr(story92, "read", lambda root, path: changed
                        if path == story92.TIERING.CI_WORKFLOW_PATH else original_read(root, path))
    assert story92.declarations(ROOT, current_tree=current_tree)


@pytest.mark.parametrize("current_tree", [False, True])
@pytest.mark.parametrize("fault", [False, True])
def test_declarations_preserve_only_the_exact_accepted_tooling_setup(monkeypatch, current_tree, fault):
    original_read = story92.read
    setup = ("      - name: Install locked tier verification tooling\n"
             "        run: |\n"
             "          python -m pip install --disable-pip-version-check uv==0.12.23\n"
             "          uv sync --locked --python 3.11\n")
    if fault:
        setup += '          echo "BASH_ENV=/tmp/override.sh" >> "$GITHUB_ENV"\n'
    workflow = _shell_environment_workflow().replace("    steps:\n", "    steps:\n" + setup, 1)
    monkeypatch.setattr(story92, "read", lambda root, path: workflow.encode()
                        if path == story92.TIERING.CI_WORKFLOW_PATH else original_read(root, path))
    if fault:
        with pytest.raises(story92.VerificationError) as failure:
            story92.declarations(ROOT, current_tree=current_tree)
        assert failure.value.code == "TIER_NOT_DECLARED"
    else:
        assert story92.declarations(ROOT, current_tree=current_tree)
