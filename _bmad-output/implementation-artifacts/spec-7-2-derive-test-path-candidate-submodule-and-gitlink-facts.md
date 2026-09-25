---
title: 'Derive test, path, candidate, submodule, and gitlink facts'
type: 'feature'
created: '2026-09-23'
status: 'done'
baseline_commit: 'c69334cb13a981c9112ad687427b1f43fafc2988'
allowed_skipped_tests: []
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v9/story-contracts/7.2.json'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The v2 generator lacks Story 7.2's root test totals, exact changed paths, baseline, final candidate, and predecessor digest.

**Approach:** Extend the record and generator with measured facts from test artifacts and root Git objects; generate the pair after ten prerequisite scenarios pass.

## Boundaries & Constraints

**Always:** Follow all 11 commands in `v9/story-contracts/7.2.json`. Derive nonempty counts for root-owned `.slnx` test projects from current results and the skip policy. Compare the exact normalized baseline-to-candidate path set. Match raw mode-`160000` gitlinks to root `.gitmodules`. Recompute the Story 7.1 digest. Keep result states distinct and applicable ledgers nonempty.

**Never:** Accept caller facts; traverse or change submodules; count files beneath gitlinks as owned paths; rewrite historical evidence; change v1 or Story 7.1 output; implement Stories 7.3–7.4. Story 7.1's `done` row and `PASS` record do not establish terminal `ACCEPTED` authority.

## I/O & Edge-Case Matrix

| State | Result | Required blocker |
| --- | --- | --- |
| Eight current root project results, exact paths, ten gitlinks | Derived, schema-valid deterministic pair | None |
| Missing, stale, failed, unapproved-skipped, zero-run test | No passing record | `TEST_RESULTS_MISSING`, `TEST_RESULTS_STALE`, `TEST_FAILED`, `TEST_SKIPPED`, `TEST_NOT_RUN` |
| Unrelated dirt, divergent list, path below a gitlink | No passing record | `SOURCE_TREE_DIRTY`, `FILE_LIST_DRIFT`, `SUBMODULE_INTERNAL_PATH` |
| Missing/extra/moved gitlink, false `160000` filename, bad baseline, superseded candidate | No passing record | `GITLINK_SCOPE_MISMATCH`, `GITLINK_DRIFT`, `BASELINE_NOT_TRUSTWORTHY`, `CANDIDATE_NOT_FINAL` |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/generate_story_record.py:721,861,1040,1380,1476` -- adapt solution/TRX, path, and candidate derivation to v2. Its v2 route at `:3025,3215,3345,3829` handles gitlinks, JUnit ledgers, and output; retain strict CLI.
- `_bmad/schemas/story-final-record-v2.schema.json:7` -- closed record lacks per-project totals, path set, baseline, and predecessor digest; add a backward-compatible Story 7.2 measurement shape.
- `_bmad/scripts/tests/test_generate_story_record.py:2083,2164,2246` -- extend v2 hermetic fixtures and byte-identity snapshots for the ten exact Story 7.2 selectors; preserve Story 7.1 fixtures.
- `Hexalith.Conversations.slnx:21`, `_bmad/schemas/v9-acceptance-result-v1.schema.json`, and `_bmad-output/planning-artifacts/v9/story-contracts/7.2.json` -- eight-project inventory, closed result envelope, and frozen acceptance commands.
- `docs/runbooks/story-final-record-generation.md:258` and `docs/release-evidence/story-7.1-final-record-v2.json` -- operator rules and predecessor input. New outputs: `docs/release-evidence/story-7.2-final-record-v2.{json,md}`.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/schemas/story-final-record-v2.schema.json` -- add closed optional Story 7.2 measurements; keep the committed Story 7.1 record valid.
- [x] `_bmad/scripts/generate_story_record.py` -- derive tests, paths, baseline/candidate, gitlinks, and predecessor binding; reject named faults; render and verify the new facts.
- [x] `_bmad/scripts/tests/test_generate_story_record.py` -- implement `AC-7.2-01` through `AC-7.2-10` selectors, one-fault fixtures, acceptance-result validation, and byte-identical restoration.
- [x] `docs/runbooks/story-final-record-generation.md` -- document Story 7.2 inputs, blocker codes, exit classes, and operator commands.
- [x] `docs/release-evidence/story-7.2-final-record-v2.{json,md}` -- generate from a committed candidate after the prerequisite results pass and verify identical rerun bytes.

**Acceptance Criteria:**
- Given eight current root project results, when `AC-7.2-01` runs, then artifact-derived per-project and summed counts validate as acceptance-result v1.
- Given each named fault, when `AC-7.2-02` through `AC-7.2-10` run, then each proves its exact blocker and restores its fixture byte-identically.
- Given ten passing prerequisite scenarios and the verified Story 7.1 digest, when `AC-7.2-11` runs, then the pair binds every measured fact and reports `11/11/0/0/0/0`.
- Given existing v1 and Story 7.1 tests, when rerun, then their behavior and Story 7.1's committed pair remain valid.

## Implementation Notes

- The committed candidate `60f23cb7057aee1ed58a2b032262de4bd15df9d1` generated the JSON/Markdown pair byte-identically on rerun. All ten prerequisite scenarios passed; eight root projects reported 2,026 executed/passed tests with no failures or skips; the final record reports `11/11/0/0/0/0`.
- Review transition gate: `python3 _bmad/scripts/verify_submodule_promotion.py --repository /home/administrator/projects/hexalith/conversations --baseline c69334cb13a981c9112ad687427b1f43fafc2988 --candidate HEAD --format json` exited `0`/`pass`, with five `UNDECLARED_GITLINK_CHANGE` warnings. `python3 /home/administrator/projects/hexalith/conversations/_bmad/scripts/verify_evidence_boundary.py --repository /home/administrator/projects/hexalith/conversations --baseline c69334cb13a981c9112ad687427b1f43fafc2988 --candidate HEAD` exited `2`/`BLOCKED` with `EVIDENCE_V24_ROOT_GITLINK_DRIFT` and a nonempty assertion ledger. The review transition remains blocked; status stays `in-progress`.
- The spec edits after the generated candidate are uncommitted. Regeneration against this working tree will require a new committed source candidate and current test results. Story 7.1 terminal `ACCEPTED` authority has not been established.
- 2026-09-24 resume: the pair committed in `09b9bf4` binds `60f23cb`, a pre-rewrite V28 commit that is no longer an ancestor of `main`, and four later commits moved root gitlinks. AC-7.2-11 at `c6fc53bcfd1e94ea544d42109687a7792b8d2449` exited `1` with `CANDIDATE_NOT_FINAL`. The superseded pair is retracted with this candidate and regenerated from it as a record-only successor.
- 2026-09-24 remeasurement at `c6fc53b` (Release, `UseHexalithProjectReferences=true`, `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`): eight root projects executed and passed 2,026 tests, with no failures or skips. Without the AppHost boundary variable, AppHost reports one skip, which the v2 route rejects.
- Lifecycle gates at `c6fc53b`: the promotion gate exited `0`/`pass` with seven `UNDECLARED_GITLINK_CHANGE` warnings. `verify_evidence_boundary.py` without a trusted host exited `2`/`BLOCKED` (`EVIDENCE_V24_ROOT_GITLINK_DRIFT`, one ledger row). With `--trusted-host c6fc53b` it exited `1`/`FAIL` (`EVIDENCE_V28_GOVERNED_PATH_TOUCHED`). Both results come from the frozen V24/V28 root-gitlink authority chain, which the later submodule bumps touch. They are recorded here, and the story proceeds under the owner's 2026-09-22 waiver of planning-authority ceremony. No hold lift is claimed.
- Review patches: the schema requires `measurements` exactly for Story 7.2; the TOTAL row renders `none`; fixture TRX mtimes are anchored after every fixture write; exact-blocker assertions are used throughout; there are new tests for record-only retention, an invalid prior pair, a repeated `.gitmodules` path, foreign-assembly and header/row TRX faults, and an invalid Story 7.1 predecessor; the runbook adds the Story 7.2 blocker table and the superseded-pair recovery procedure. The generator suite reports 160 passed. The full `_bmad/scripts` lane adds no failures: its 76 failures and 52 errors occur in older authority suites, and all 76 failures also occur on the untouched `HEAD`.

## Spec Change Log

## Review Triage Log

Review 2026-09-24 over the story-owned diff (`c69334c..c6fc53b`, six story paths; V28 authority commits and gitlink bumps excluded as not Story 7.2 work). Layers: blind hunter (B), edge-case hunter (E), verification gap (V).

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| E13, V-o1 | Committed pair binds `60f23cb`, a dangling pre-rewrite V28 commit not reachable from `main` | high | patch | `git merge-base --is-ancestor 60f23cb HEAD` false; AC-7.2-11 at `c6fc53b` exits `1` `CANDIDATE_NOT_FINAL`. Regenerate from a new committed candidate. |
| E6, V-o2 | No documented recovery once a prior pair's candidate is superseded | medium | patch | `v2_story_7_2_candidate` retains the prior pair's candidate; only deleting the pair clears it. Runbook documents the retraction step. |
| V1 | Record-only successor retention and prior-pair corruption untested | medium | patch | Gap pre-verified; no test commits the pair alone or corrupts it. |
| V2 | Duplicate `.gitmodules` path check untested | medium | patch | Gap pre-verified. |
| V3 | TRX assembly-identity and header/row disagreement untested for 7.2 | medium | patch | Gap pre-verified. |
| V4 | Story 7.1 predecessor verification untested | medium | patch | Gap pre-verified. |
| V5 | Markdown measurements section only self-verified | medium | patch | Gap pre-verified. |
| B1, E11 | Schema does not require `measurements` for Story 7.2 | low | patch | Top-level schema has no conditional; direct `if`/`then` correction. |
| B12, E10 | TOTAL row renders an empty code span | low | patch | `code("")` at the TOTAL row yields literal backticks in the committed `.md`. |
| B6 | AC-7.2-01 hardcodes total `16` over a workspace-derived project list | low | patch | `STORY_7_2_PROJECTS` globs the live workspace; direct correction to `2 * len`. |
| E12 | Fixture TRX mtime anchored to spec write time plus 10 s | low | patch | Commit and `.slnx` writes follow the spec; slow runners can read fixtures as stale. Direct correction. |
| E14 | AC-7.2-07/09/10 assert blocker membership, not the exact set | low | patch | `in`/subset checks; direct tightening to equality. |
| B10 | Runbook v2 blocker table lacks the Story 7.2 codes | low | patch | Codes appear only in prose; direct doc addition. |
| B3, E4 | Freshness is mtime-only; TRX not bound to binaries built from the candidate | medium | defer | Real; binary provenance is a new mechanism beyond this story's fix scope. |
| E5 | Submodule worktree HEAD not compared with candidate gitlinks | medium | defer | Real (see TRX measured on re-checked-out submodules); reading submodule HEADs conflicts with the story's never-traverse boundary. |
| B2 | Pair verification does not recompute count invariants | low | reject | Counts are re-derived from TRX on every run; hand-edit harm requires deliberate tampering and the fix adds checks. |
| B4, E2 | Internal-path check exercised only by monkeypatch; removed gitlink path listed as a root path | low | reject | A gitlink-to-directory conversion makes paths genuinely root-owned; removed declarations still hit `GITLINK_SCOPE_MISMATCH`. |
| B5 | Assorted untested branches | low | reject | Material branches are covered by V1–V4 patches; the rest are unlikely and need new fixtures. |
| B7, E7 | Test projects selected by `tests/` prefix | low | reject | All eight root test projects live under `tests/`; `src/*.Testing` is a helper library. |
| B8 | Story-specific synonym codes | false | reject | The frozen matrix mandates the 7.2 names and forbids changing Story 7.1 output. |
| B9 | Dispatch keyed by contract path and story id | low | reject | The contract path is fixed by `finalRecord.paths`; unlikely divergence. |
| B11 | `changedPaths` includes V28 work | false | reject | The record truthfully reports the frozen baseline-to-candidate set the spec defines. |
| B12b | Header says JUnit though counts come from TRX | low | reject | Shared header; changing it alters Story 7.1 bytes, which the spec forbids. |
| B13 | Runbook `uv run` vs contract `python3` | false | reject | The generator accepts the pinned-`uv` self-invocation; verified by the AC-7.2-11 run. |
| B14 | AC-7.2-01 validates a test-built acceptance document | low | reject | Emitting acceptance documents from the generator adds public surface. |
| B15 | Predecessor ancestry and rollback wording | low | reject | Rollback text is contract-frozen; ancestry adds guards for an unlikely case. |
| E1 | `GateError` in the raw diff pass becomes `INTERNAL_ERROR` | low | reject | Requires control characters in tracked paths; the fix adds a guard. |
| E3 | Malformed JSON-escaped `baseline_commit` becomes `INTERNAL_ERROR` | low | reject | Unlikely; the fix adds a guard. |
| E8 | Backslash `.slnx` paths | low | reject | The committed `.slnx` uses forward slashes. |
| E9 | Amended identical-tree candidate reads stale | low | reject | A new commit is a new candidate by design; rerun remediates. |
| E15 | Unparseable TRX reported as missing | false | reject | Runbook §8 documents this mapping deliberately. |
| V-o3 | Measurement-level supersession check unreachable | low | reject | Harmless duplicate guard. |

## Design Notes

Read each project's TRX at `artifacts/v9/7.2/test-results/<project>.trx`, deriving `<project>` from the committed `.slnx`. The direct Story 7.2 request authorizes implementation. Current planning authority lacks Story 7.1 terminal `ACCEPTED`; report that condition without claiming its lifecycle gate passed.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- v1, 7.1, and 7.2 pass.
- Run `AC-7.2-01` through `AC-7.2-10` exactly as declared in the frozen contract through pinned `uv` -- current nonempty JUnit results; each negative fixture proves its blocker.
- Run `AC-7.2-11` exactly through pinned `uv` after committing source -- schema-valid `PASS`, deterministic pair, ten gitlinks, `11/11/0/0/0/0`.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline c69334cb13a981c9112ad687427b1f43fafc2988 --candidate HEAD` -- record its actual state before review/done.
- `git diff --check` -- no whitespace errors.
