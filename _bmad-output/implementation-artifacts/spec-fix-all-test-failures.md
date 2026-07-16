---
title: 'Run all solution-defined tests and fix all failures'
type: 'chore'
created: '2026-07-14'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'c6670fac7347ecd7240f7bab7e5e23147c8dfc65'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository needs a current, complete test result and correction of every reproducible failure. A planning baseline on 2026-07-14 found the solution build and all 14 solution-defined test projects green: 1,932 passed, 0 failed, and 0 skipped.

**Approach:** Treat `Hexalith.Conversations.slnx` as the authoritative product-test inventory, build it once, and execute every listed test project individually. If a failure reproduces, make the smallest root-owned production or test correction, rerun its focused lane, and rerun the full inventory; when no failure reproduces, leave product code unchanged.

## Boundaries & Constraints

**Always:** Preserve the recorded root-submodule commits; run the nine Conversations and five Commons test projects individually; use the SDK selected by `global.json`; keep warnings as errors; distinguish product failures from environment failures; validate that test-generated evidence is deterministic; finish with a clean full-suite rerun and an unchanged gitlink diff.

**Ask First:** Any correction that would require editing submodule content, changing a submodule pointer, changing a public contract, updating a golden/baseline artifact to accept new behavior, upgrading a dependency, disabling or skipping a test, weakening an assertion, or expanding beyond the solution-defined product-test inventory.

**Never:** Initialize nested submodules; use recursive or remote submodule updates; edit files under `references/`; change gitlinks; hide failures with warning suppression, retries, broader timeouts, skipped tests, or reduced assertions; hand-edit generated `obj/` or `bin/` output; run a solution-level `dotnet test` in place of the required per-project runs.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Clean suite | Restore/build succeeds and all projects pass | Report exact project and test totals; make no product change | Confirm working tree and gitlinks remain unchanged |
| Product regression | A test fails reproducibly in its project | Correct the root-owned cause and retain or strengthen coverage | Run the focused project, then all 14 projects |
| Environment limitation | Runner fails before exercising tests | Use the documented focused build/direct xUnit executable fallback | Record the broad-gate blocker separately; do not classify it as product green |
| Generated evidence drift | A test changes tracked evidence | Accept only when it deterministically reflects an approved behavior change | Revert or correct unintended drift; ask before rebasing a golden artifact |

</frozen-after-approval>

## Code Map

- `Hexalith.Conversations.slnx` -- authoritative inventory of the 14 product test projects and the restore/build graph.
- `tests/README.md` -- local test prerequisites, lane descriptions, and restricted-runner fallback.
- `tests/Hexalith.Conversations.*Tests/` -- nine root-owned Conversations test projects, including `IntegrationTests`.
- `references/Hexalith.Commons/test/` -- five solution-listed dependency tests to execute but never edit in this task.
- `src/` and `tests/` -- only permissible correction surfaces, limited to files directly implicated by a reproducible failure.
- `global.json`, `Directory.Build.props`, and `Directory.Packages.props` -- inspect-only root configuration; request approval before editing if a restore/build failure implicates them.
- `docs/release-evidence/` and `_bmad-output/implementation-artifacts/evidence/` -- tracked outputs that generation tests may verify or rewrite.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.Conversations.slnx` -- assert that the inventory parser resolves exactly 14 test projects, then restore and build the complete Debug graph serially with zero warnings and errors.
- [x] `references/Hexalith.Commons/test/` and `tests/Hexalith.Conversations.*Tests/` -- execute all 14 solution-listed projects individually with TRX output; assert per-project discovery, zero failures/skips, and the approved aggregate of 1,932 passed tests.
- [x] `docs/release-evidence/` and `_bmad-output/implementation-artifacts/evidence/` -- compare pre/post-run hashes so passing generation tests cannot hide nondeterministic tracked evidence.
- [x] `src/` and `tests/` -- only if a failure reproduces, patch the directly implicated root-owned file, rebuild, verify the focused project, and rebuild again before the full `--no-build` rerun.
- [x] `.gitmodules` and `references/` gitlinks -- compare working/index state to `baseline_commit` and confirm no nested `.git` metadata, submodule-content edit, or pointer change occurred.
- [x] `_bmad-output/implementation-artifacts/spec-fix-all-test-failures.md` -- record the SDK, commit/worktree state, timestamp, and per-project results as auditable execution evidence.

**Acceptance Criteria:**
- Given the root solution at its recorded submodule commits, when it is restored and built in Debug, then the build completes with zero warnings and zero errors.
- Given the 14 test projects listed in the solution, when each is run individually, then every executed test passes with no failures or skips.
- Given a reproducible failure, when the correction is applied, then its focused project and the complete 14-project suite pass without weakened coverage.
- Given completion of validation, when repository state is inspected, then only approved root-owned fixes and this workflow artifact are changed, while every gitlink remains byte-for-byte unchanged.

## Spec Change Log

- **Iteration 1 — verification evidence hardening:** Adversarial review found that the reusable loop could succeed after discovering fewer than 14 projects or while tests were skipped, and that the written Git checks did not fully prove baseline-relative gitlink equality or absence of nested initialization. The tasks and commands now require an exact inventory count, TRX assertions for discovery/failure/skip/aggregate totals, pre/post evidence hashes, baseline/index gitlink comparisons, explicit nested metadata inspection, and an auditable result table. This avoids a false-green validation artifact. **KEEP:** the approved 14-project solution scope, individual project execution, serial Debug build, full Playwright lane, no submodule edits, and no product changes when the suite is green.

## Validation Results

**Execution identity:** `2026-07-14T13:47:26+02:00`; baseline/HEAD `c6670fac7347ecd7240f7bab7e5e23147c8dfc65`; SDK `10.0.302`; initial tracked worktree and gitlink diff clean.

| Project | Passed | Failed | Skipped |
|---------|-------:|-------:|--------:|
| `Hexalith.Commons.Aspire.Tests` | 9 | 0 | 0 |
| `Hexalith.Commons.Publication.Tests` | 9 | 0 | 0 |
| `Hexalith.Commons.Diagnostics.Tests` | 16 | 0 | 0 |
| `Hexalith.Commons.ServiceDefaults.Tests` | 14 | 0 | 0 |
| `Hexalith.Commons.Serialization.Tests` | 21 | 0 | 0 |
| `Hexalith.Conversations.Admin.Web.Tests` | 14 | 0 | 0 |
| `Hexalith.Conversations.AppHost.Tests` | 7 | 0 | 0 |
| `Hexalith.Conversations.Client.Tests` | 29 | 0 | 0 |
| `Hexalith.Conversations.Conformance.Tests` | 384 | 0 | 0 |
| `Hexalith.Conversations.Contracts.Tests` | 618 | 0 | 0 |
| `Hexalith.Conversations.IntegrationTests` | 9 | 0 | 0 |
| `Hexalith.Conversations.ServiceDefaults.Tests` | 7 | 0 | 0 |
| `Hexalith.Conversations.Server.Tests` | 610 | 0 | 0 |
| `Hexalith.Conversations.Tests` | 185 | 0 | 0 |
| **Total** | **1,932** | **0** | **0** |

The solution parser resolved exactly 14 test projects, and the solution restore and serial Debug build completed with 0 warnings and 0 errors. The review-iteration rerun produced 14 unique TRX files with `total = executed = passed` for every project, including the rendered Admin Web Playwright lane, and an aggregate of 1,932 passed, 0 failed, and 0 not executed. Pre/post SHA-256 manifests for 78 evidence files were identical; no tracked source, test, or evidence file changed; all gitlinks remained equal to the baseline commit; and no nested `.git` metadata was present.

## Verification

**Commands:**

Run the inventory, manifest, test, TRX, and cleanup entries below consecutively in the same Bash session; the `projects`, `RESULTS`, and manifest variables intentionally carry forward.

- `dotnet restore Hexalith.Conversations.slnx` -- expected: every solution project restores successfully.
- `dotnet build Hexalith.Conversations.slnx -c Debug --no-restore /m:1 /nr:false` -- expected: 0 warnings and 0 errors.
- `mapfile -t projects < <(sed -n 's/.*<Project Path="\([^"]*Tests[^"]*\.csproj\)".*/\1/p' Hexalith.Conversations.slnx); test "${#projects[@]}" -eq 14; test "$(printf '%s\n' "${projects[@]}" | sort -u | wc -l)" -eq 14; test "$(printf '%s\n' "${projects[@]}" | grep -c '^references/Hexalith.Commons/test/')" -eq 5; test "$(printf '%s\n' "${projects[@]}" | grep -c '^tests/')" -eq 9; for project in "${projects[@]}"; do test -f "$project" || exit; done` -- expected: exactly 14 unique existing projects in the approved five-Commons/nine-Conversations split.
- `before_manifest="$(mktemp)"; after_manifest="$(mktemp)"; LC_ALL=C find docs/release-evidence _bmad-output/implementation-artifacts/evidence -type f -print0 | sort -z | xargs -0 sha256sum > "$before_manifest"` -- expected: a null-safe pre-run manifest of all 78 tracked evidence files.
- `results="$(mktemp -d)"; export RESULTS="$results"; for project in "${projects[@]}"; do name="$(basename "$project" .csproj)"; dotnet test "$project" -c Debug --no-build --no-restore --nologo --verbosity minimal --logger "trx;LogFileName=$name.trx" --results-directory "$results" /nr:false || exit; done` -- expected: one successful TRX file per project.
- `pwsh -NoProfile -Command '$files = @(Get-ChildItem "$env:RESULTS/*.trx"); if ($files.Count -ne 14) { throw "Expected 14 TRX files" }; $total = 0; foreach ($file in $files) { [xml]$xml = Get-Content $file; $c = $xml.TestRun.ResultSummary.Counters; if ([int]$c.total -le 0 -or [int]$c.executed -ne [int]$c.total -or [int]$c.passed -ne [int]$c.total -or [int]$c.failed -ne 0 -or [int]$c.notExecuted -ne 0) { throw "Non-green counters in $($file.Name)" }; $total += [int]$c.total }; if ($total -ne 1932) { throw "Expected 1932 tests, got $total" }'` -- expected: 14 non-empty projects, 1,932 passed, 0 failed, 0 skipped.
- `LC_ALL=C find docs/release-evidence _bmad-output/implementation-artifacts/evidence -type f -print0 | sort -z | xargs -0 sha256sum > "$after_manifest"; diff -u "$before_manifest" "$after_manifest"` -- expected: all 78 evidence paths and hashes are byte-identical before and after the suite.
- `test "$(git rev-parse HEAD)" = c6670fac7347ecd7240f7bab7e5e23147c8dfc65; git -c diff.ignoreSubmodules=none diff --exit-code --submodule=log c6670fac7347ecd7240f7bab7e5e23147c8dfc65 -- .gitmodules references; git -c diff.ignoreSubmodules=none diff --cached --exit-code --submodule=log -- .gitmodules references; test -z "$(find references -mindepth 3 -name .git -print -quit)"; test -z "$(find .git/modules/references -mindepth 2 -type d -name modules -print -quit 2>/dev/null)"` -- expected: HEAD and gitlinks equal the baseline, no staged/dirty submodule state, and no nested worktree or administrative metadata.
- `test "$(git status --porcelain=v1 --untracked-files=all)" = '?? _bmad-output/implementation-artifacts/spec-fix-all-test-failures.md'; rm -rf "$RESULTS" "$before_manifest" "$after_manifest"` -- expected: only this workflow artifact is untracked, then all temporary evidence is removed.

## Suggested Review Order

**Scope and safety**

- Start with the approved solution-defined inventory and no-change behavior.
  [spec-fix-all-test-failures.md:16](spec-fix-all-test-failures.md#L16)

- Confirm submodule, baseline, and coverage guardrails remain non-negotiable.
  [spec-fix-all-test-failures.md:22](spec-fix-all-test-failures.md#L22)

**Validation evidence**

- Inspect the exact SDK, revision, and per-project pass counts.
  [spec-fix-all-test-failures.md:71](spec-fix-all-test-failures.md#L71)

- Verify the hardened rerun proves evidence and gitlink stability.
  [spec-fix-all-test-failures.md:91](spec-fix-all-test-failures.md#L91)

**Reproducible gates**

- Review fail-closed inventory discovery and TRX execution sequencing.
  [spec-fix-all-test-failures.md:101](spec-fix-all-test-failures.md#L101)

- Finish with evidence, HEAD, gitlink, nested-metadata, and status assertions.
  [spec-fix-all-test-failures.md:105](spec-fix-all-test-failures.md#L105)
