---
title: 'Reconcile and verify the standalone CP-1 anti-skip guard'
type: 'bugfix'
created: '2026-08-20'
status: 'done'
baseline_commit: '62f27c452b7ef8fb8d1f2a1c88e62e8c792b3893'
completion_scope: 'documentation-reconciliation'
reconciliation_baseline_commit: '8793d26306f91d2cfe00116dc0b8eec41ec8900f'
implementation_commit: '36febdd94faaaf0db99fcb4d0feae82ab4df115c'
submodule_promotions: []
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-19.md'
---

## Reconciled Scope (2026-10-08)

The user approved reconciling this stale spec with current-main policy, preserving
existing A2/A3 closures, then finishing focused verification and review. The
standalone guard was already delivered in `implementation_commit`. This run
changes only this spec; `status` tracks its documentation reconciliation and
current guard verification.

The frozen section below preserves the original intent as historical context.
Its 280-test requirement, six-gitlink promotion boundary, V14 Python pins, and
A2 `in-progress` / item-25 `open` expectation no longer govern this run. A2 and
item-25 are already `done`; their closure records and the A3 closure remain intact.
Use the [current-change policy](../../docs/runbooks/current-change-validation.md)
and the reconciled tasks and acceptance below. Do not rerun historical evidence
or publication gates, refresh packages, move submodules, or build product code
for this documentation correction. Completion makes no new hold, readiness,
release, or historical-gate acceptance claim.

Keep the original `baseline_commit` as provenance. Review only this spec's
current diff against `reconciliation_baseline_commit`; later committed history
and unrelated shared-workspace edits are outside this change. Preserve all
pre-existing changes, the guard, F-10, lifecycle records, dependency files,
authorities, and submodule content. Do not stage, commit, push, or clean them.

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

- `_bmad/scripts/tests/test_static_anti_skip_guard.py` -- existing recursive AST guard and prohibited/allowed source fixtures; read-only.
- `_bmad/scripts/tests/test_verify_epic_6_completion_supersession.py` -- read-only F-10 controlled-dirt test; select it explicitly without running the historical suite.
- `_bmad/scripts/tests/conftest.py` and `.github/workflows/ci.yml` -- current tooling collection policy; the guard remains in the directory lane, while CI excludes the supersession suite.
- `scripts/check-root-submodules.py` -- current index/declaration check; reads no submodule content.
- `spec-e6-remediation-a2-restore-lifecycle-gates.md` and `sprint-status.yaml` -- existing A2/item-25 closure records; read-only.
- `docs/runbooks/current-change-validation.md` -- governing current-work policy.

## Tasks & Acceptance

**Execution:**
- [x] This spec -- reconcile obsolete prerequisites with the user-approved current scope; preserve original frozen intent and baseline provenance.
- [x] `_bmad/scripts/tests/test_static_anti_skip_guard.py` and F-10 -- run the focused detector fixtures and controlled-dirt proof, recording measured results.
- [x] `_bmad/scripts/tests` -- collect the current directory lane and explicitly confirm both focused test node IDs; record counts without requiring a historical total.
- [x] `scripts/check-root-submodules.py` -- verify current root declarations against indexed gitlinks without updating dependencies.
- [x] This spec -- audit preservation and the owned diff, complete independent review, and record the current result.

**Acceptance Criteria:**
- Given recursive Python test sources, when the existing guard runs, then its prohibited call/marker/decorator fixtures produce sorted path/line diagnostics and its string/comment/`tmp_path` fixtures pass.
- Given F-10's temporary repository, when it introduces controlled tracked dirt, then the verifier rejects that dirt with `E6_CURRENT_PROOF_WORKTREE_DIRTY`; both focused tests execute with zero failures or skips.
- Given current directory collection, when collection completes, then the guard is listed; explicit focused collection also lists F-10, with no historical fixed-count requirement.
- Given the current root index, when the root-submodule checker runs, then declared paths match resolved mode-`160000` gitlinks.
- Given the reconciliation entry state, when this change completes, then only this spec was edited by this task, original frozen intent and `baseline_commit` are preserved, and existing A2/A3 closures, F-10, the guard, dependencies, and user changes remain intact.

## Spec Change Log

- 2026-08-22: The user directed the implementation to use the latest root submodules and packages. Declared the six fetched `origin/main` promotions, adopted the promoted Builds catalog, and updated both repository-owned commitlint packages to 21.2.2. A trial refresh of jsonschema/pytest was reverted because the active V14 authority failed closed on the pinned `pyproject.toml`/`uv.lock` bytes.

- 2026-10-08: The user approved current-main reconciliation after focused verification exposed stale prerequisites. The guard already exists, A2/A3 and item-25 are closed, and later policy and dependency changes supersede this spec's operational assumptions. Keep the original frozen intent and baseline verbatim as history; apply focused current checks to this spec's reconciliation diff. Preserve existing closures and all unrelated work. No historical PASS or renewed readiness/hold authorization is inferred.

## Verification

**Commands (run from the repository root):**
- `uv run --frozen --no-sync python3 -m pytest -q --tb=short _bmad/scripts/tests/test_verify_epic_6_completion_supersession.py _bmad/scripts/tests/test_static_anti_skip_guard.py -k 'dirty_tracked_worktree_blocks_current_proof or python_tooling_lane_has_no_ambient_skip_constructs'` -- expected: both selected tests pass, zero skips.
- The same focused command with `--collect-only` -- expected: both node IDs appear.
- `uv run --frozen --no-sync python3 -m pytest -q --collect-only _bmad/scripts/tests` -- expected: collection succeeds and includes the guard; counts are measured, not pinned to 280.
- `python3 scripts/check-root-submodules.py --repository .` -- expected: `root submodules: PASS`.
- `git diff --check -- _bmad-output/implementation-artifacts/spec-cp-1-cp-2-close-a2-after-anti-skip-guard.md` -- expected: no whitespace errors.
- Compare the original frozen block, original baseline, guard/F-10 bytes, A2 status and item-25 closure with the reconciliation entry state; review the spec-only diff from `reconciliation_baseline_commit`. Distinguish concurrent unrelated edits from task-owned changes.

**Measured results (2026-10-08):**

- The focused execution passed: `2 passed, 40 deselected in 0.65s`, with zero failures or skips.
- Focused collection listed both requested node IDs: `2/42 tests collected (40 deselected) in 0.07s`.
- Current directory collection succeeded: `983 tests collected in 0.25s`; both the guard and F-10 were listed. This is the measured shared-workspace count, not a fixed historical requirement.
- The root-index check returned `root submodules: PASS`; no dependency checkout was updated.
- The original frozen block is byte-identical to `reconciliation_baseline_commit`, with SHA-256 `9983736253ddae90ba983329ea4e4fa70c70f8458700752b5cd6b54a12940555`; original `baseline_commit` provenance is intact.
- Implementation changed only this spec. Existing guard/F-10 bytes, A2/item-25 and A3 closures, dependency files, and unrelated shared-workspace changes are preserved. The final review and reconciliation lifecycle result are recorded below.

## Review Triage Log

Three independent reviewers examined the spec-only reconciliation diff. The
edge-case reviewer returned `[]`; the verification reviewer found no gaps. The
blind reviewer supplied four observations, each assessed below before grouping.
No finding requires a code patch or historical-gate repair. Spec-edit suggestions
are rejected under the build review rule; the evidence below records the checks
that settle the actual preservation and selection claims.

| ID | Finding | Verdict | Evidence and disposition |
| --- | --- | --- | --- |
| CP1-B1 | Preservation record omits entry-state hashes/index/user changes. | low | The pre-edit snapshot is retained locally at `/tmp/cp1-reconcile-sycy978i/preexisting-state.json`, with the original spec at `original-spec.md` beside it. It records entry HEAD, pre-existing path status, indexed gitlinks, and hashes for the guard, F-10, and A2 spec. `python3 /tmp/cp1-reconcile-sycy978i/preservation.py` passed; only this spec was written by this task. A snapshot locator is an evidence-navigation improvement; reject the requested spec edit. These temporary files are local audit support, not committed release evidence. |
| CP1-B2 | A3 preservation lacks an explicit comparison target. | low | CP-3 in `../planning-artifacts/sprint-change-proposal-2026-08-19.md` identifies A3 as sprint item-26. The preservation audit compares entire item-24, item-25, and item-26 rows with `reconciliation_baseline_commit`: all remain byte-identical and `done`. A2's full file matches its entry hash. Reject the requested Code Map/procedure edit; the explicit A3 check passed without modifying lifecycle records. |
| CP1-B3 | Substring selection and missing node IDs could silently change the intended pair. | false | Both the recorded `-k` collection and a separate exact-node collection select the existing `test_verify_epic_6_completion_supersession.py::test_dirty_tracked_worktree_blocks_current_proof` and `test_static_anti_skip_guard.py::test_python_tooling_lane_has_no_ambient_skip_constructs`, under `_bmad/scripts/tests/`. The exact-node check measured `2 tests collected in 0.04s`. The claimed silent selection drift does not occur in this run. |
| CP1-B4 | `--no-sync` does not prove the installed environment matches the lock. | false | This reconciliation claims focused results, not environment synchronization. Read-only inspection through `uv run --frozen --no-sync python3` confirmed `.venv/bin/python3`, Python `3.11.15`, pytest `9.1.1`, and jsonschema `4.26.0`; the latter two match current `pyproject.toml` pins. No refresh or claim that `--no-sync` validates lock parity is made. |

## Completion (2026-10-08)

Documentation reconciliation is complete under the user-approved current scope.
The two focused tests passed, both node IDs were collected, the root-submodule
index check passed, and the preservation audit passed after review. The final
scoped whitespace check passed. All three review lenses completed; no code
patches or deferred findings remain. This task edited only this spec and left
it uncommitted. A2/A3 closures and all unrelated work remain untouched.

Current directory collection is evidence of discovery only; it is not a claim
that 983 tests executed or passed. The original full-lane, dependency-promotion,
package-refresh, and historical evidence gates were not rerun for this
owner-approved documentation correction. The historical frozen matrix is
retained as context, not certified as satisfied by these focused checks.
