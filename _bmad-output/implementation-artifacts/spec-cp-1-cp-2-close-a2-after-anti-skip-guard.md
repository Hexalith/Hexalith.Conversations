---
title: 'Add the standalone CP-1 anti-skip guard'
type: 'bugfix'
created: '2026-08-20'
status: 'in-progress'
baseline_commit: '62f27c452b7ef8fb8d1f2a1c88e62e8c792b3893'
submodule_promotions:
  - path: 'references/Hexalith.Builds'
    require_remote: true
  - path: 'references/Hexalith.Commons'
    require_remote: true
  - path: 'references/Hexalith.EventStore'
    require_remote: true
  - path: 'references/Hexalith.FrontComposer'
    require_remote: true
  - path: 'references/Hexalith.Parties'
    require_remote: true
  - path: 'references/Hexalith.Tenants'
    require_remote: true
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-19.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** F-10 is hermetic, but no mechanical guard prevents Python tooling tests from reintroducing ambient `pytest.skip`, `skipif`, or `verifier.worktree_dirt(ROOT)` conditioning. The proposal's named test file is V14 byte-pinned, so editing it would fail the full lane.

**Approach:** Add one standalone recursive AST-based guard under `_bmad/scripts/tests`, leaving the pinned F-10 file unchanged. Promote the clean root-declared submodule checkouts to their fetched `origin/main` revisions, consume NuGet package versions exclusively through the promoted `Hexalith.Builds` catalog, and update the repository-owned commitlint packages to their latest stable compatible release. Preserve the V14-pinned Python manifest and lock because changing either fails the active authority closed. Prove identical full-lane collection and pass counts on clean and controlled-dirty candidate trees, then stop with A2 `in-progress` and item-25 `open` because its evidence gate is blocked.

## Boundaries & Constraints

**Always:** Parse every `_bmad/scripts/tests/**/*.py` source as AST; report sorted repository-relative file-and-line diagnostics for actual prohibited constructs; keep F-10 and the guard collected on every run; preserve the V14 commits and all pre-existing user changes byte-for-byte; compare the final changed-path and gitlink boundaries exactly; require each declared root submodule promotion to equal its fetched `origin/main`; resolve NuGet package versions only from the promoted `Hexalith.Builds` catalog; update repository-owned npm tooling through its generated lockfile.

**Ask First:** Any need to change a V14-pinned file, A2's baseline/status/change log, sprint status, V14 scope/authority, verifier logic, submodule content, a nested submodule, a NuGet package version outside the promoted catalog, or paths outside the standalone guard, six declared root gitlinks, and repository-owned npm manifest/lock; any inability to obtain clean/controlled-dirty evidence without preserving repository history and user-owned state.

**Never:** Close A3; run IR-0; change the `ACTIVE` hold; start successor work; modify product code, submodule-owned content, nested submodules, NuGet versions outside `Hexalith.Builds`, or the V14-pinned Python manifest/lock; weaken tests; amend/rebase/rewrite V14; edit or capture `implementation-readiness.md`; commit, stage, push, or clean user changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Allowed sources | Strings/comments mention banned forms; F-10 calls `worktree_dirt(tmp_path)` | Guard passes without self-triggering; F-10 executes | AST behavior, not raw text, decides |
| Prohibited source | Actual skip call/marker/decorator or ambient `worktree_dirt(ROOT)` call | Guard fails with file-and-line diagnostics | No source mutation |
| Clean versus controlled dirt | Same candidate bytes in both trees | Identical collected/passed counts; zero failed/skipped/not-run | Any delta blocks CP-1 completion |
| Latest dependency baseline | Clean root submodule checkouts after fetching `origin/main` | All ten checkouts equal `origin/main`; six advanced gitlinks are declared; package restore consumes the promoted Builds catalog | Dirty, divergent, unavailable, nested, or undeclared dependency state blocks review |
| Owned tooling packages | Latest stable commitlint releases; V14-pinned Python graph | npm manifest/lock resolve commitlint 21.2.2; Python manifest/lock remain byte-identical | Any Python refresh reports `CANDIDATE_SOURCE_DRIFT` and is not retained |
| Existing A2 gate failure | `EVIDENCE_SCOPE_BASELINE_MISMATCH` | A2 stays `in-progress`; item-25 stays `open`; no closure note | Report the stable blocker; do not bypass it |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/tests/test_static_anti_skip_guard.py` -- new standalone recursive AST guard; the only implementation path.
- `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py:477` -- read-only F-10 controlled-dirt proof; V14 candidate-byte checks prohibit editing it.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-19.md:108` -- approved CP-1 detector contract; lines 171-177 are the byte-exact CP-2 closure note.
- `_bmad-output/implementation-artifacts/spec-e6-remediation-a2-restore-lifecycle-gates.md:1` and `_bmad-output/implementation-artifacts/sprint-status.yaml:245` -- read-only blocked A2 lifecycle state.
- `_bmad/scripts/publish_v9_planning_authority.py:261` -- read-only V14 canonical-path pin explaining the standalone placement.
- `pyproject.toml:11` and `.gitattributes:7` -- strict pytest collection and LF policy for evidence artifacts.
- `.gitmodules` and `references/{Hexalith.Builds,Hexalith.Commons,Hexalith.EventStore,Hexalith.FrontComposer,Hexalith.Parties,Hexalith.Tenants}` -- root-declared latest-main promotions; no nested submodule work.
- `Directory.Packages.props` and `references/Hexalith.Builds/Props/Directory.Packages.props` -- import-only root wrapper and authoritative promoted package catalog.
- `package.json` and `package-lock.json` -- repository-owned commitlint manifest and generated npm lock.
- `pyproject.toml` and `uv.lock` -- read-only V14-pinned Python verification graph.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/scripts/tests/test_static_anti_skip_guard.py` -- add the bounded recursive AST guard with sorted file/line failures while allowing controlled `tmp_path` dirt.
- [x] Run guard/F-10 focus, collection, and full clean/controlled-dirty lanes; preserve machine-produced counts.
- [x] Reconfirm the final path/gitlink boundary and report A2's existing stable gate blocker without changing either lifecycle artifact.
- [x] Bind the six advanced root gitlinks to their fetched `origin/main` revisions and verify the remaining four root gitlinks are already current.
- [x] Restore and build the Debug solution against the promoted source dependencies and latest Builds package catalog.
- [x] Update both repository-owned commitlint packages to 21.2.2, regenerate the npm lock, and verify a locked install plus CLI execution.

**Acceptance Criteria:**
- Given any recursive Python test source, when the guard parses it, then actual `pytest.skip`, any `skipif` expression, and `verifier.worktree_dirt(ROOT)` fail with path/line diagnostics while strings, comments, and `worktree_dirt(tmp_path)` do not.
- Given the closure candidate in clean and controlled-dirty trees, when the full Python lane runs, then both collect and pass exactly 280 tests, including F-10 and the guard, with zero failed, skipped, or not-run.
- Given the recorded A2 evidence-gate failure, when CP-1 completes, then the closure note remains absent, the A2 spec remains `in-progress`, and item-25 remains `open`.
- Given the fetched root-declared dependency set, when dependency validation runs, then every checkout equals `origin/main`, exactly the six declared gitlinks differ from the baseline, no nested submodule is initialized or changed, and restore/build consume the promoted Builds package catalog without a local package-version override.
- Given repository-owned tooling, when package validation runs, then npm resolves both commitlint packages at 21.2.2 while `pyproject.toml` and `uv.lock` retain their V14-authorized bytes.

## Spec Change Log

- 2026-08-22: The user directed the implementation to use the latest root submodules and packages. Declared the six fetched `origin/main` promotions, adopted the promoted Builds catalog, and updated both repository-owned commitlint packages to 21.2.2. A trial refresh of jsonschema/pytest was reverted because the active V14 authority failed closed on the pinned `pyproject.toml`/`uv.lock` bytes.

## Verification

**Commands:**
- `uv run --frozen python3 -m pytest -q --collect-only _bmad/scripts/tests` -- expected: 280 collected; F-10 and guard both listed.
- `uv run --frozen python3 -m pytest -q --tb=short _bmad/scripts/tests/test_verify_epic_6_completion_supersession.py _bmad/scripts/tests/test_static_anti_skip_guard.py -k 'dirty_tracked_worktree_blocks_current_proof or python_tooling_lane_has_no_ambient_skip_constructs'` -- expected: 2 passed, zero skipped/not-run.
- `uv run --frozen python3 -m pytest -q --tb=short _bmad/scripts/tests` in clean and controlled-dirty candidate trees -- expected: identical 280 collected/280 passed.
- `git diff --check` and exact changed-path/raw-mode-160000 comparisons -- expected: no hygiene error, unexpected path, or undeclared gitlink change.
- Root-only `git fetch origin main` plus exact `HEAD == origin/main` checks for every `.gitmodules` path -- expected: ten equal checkouts and six declared baseline gitlink advances.
- `dotnet restore Hexalith.Conversations.slnx` then `dotnet build Hexalith.Conversations.slnx --configuration Debug --no-restore` -- expected: success with warnings treated as errors against the promoted source/catalog dependency set.
- `npm ci --ignore-scripts`, `npm ls --depth=0`, and `npm run commitlint -- --version` -- expected: locked install succeeds and both direct packages plus the CLI report 21.2.2.
