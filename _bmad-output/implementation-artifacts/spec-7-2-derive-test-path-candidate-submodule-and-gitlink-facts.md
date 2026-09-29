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

**Completion under the 2026-09-29 owner waiver** (see Implementation Notes):
- [x] `docs/runbooks/story-final-record-generation.md:549` -- in "Story 7.2 completion repair: current gates", add one dated paragraph recording the waiver: boundary `PASS`, resolver `PASS`, and Story 7.1 terminal `ACCEPTED` are no longer Story 7.2 completion gates; they are still run and their actual results recorded, never reported as `PASS`; the current-candidate v2 gate stays mandatory. At `:432`, note that the same decision authorizes the superseded-pair recovery for this completion.
- [x] `docs/release-evidence/story-7.2-final-record-v2.{json,md}` -- delete both files from the working tree; this retracts the pair pinned to `170ac9d`. Do not stage or commit.
- [ ] Completion gate (parent, step 05, after review) -- run the procedure in Implementation Notes; AC-7.2-11 exits `0` with `11/11/0/0/0/0` at the new candidate. This box stays unticked in the committed spec. The generator freezes everything except the frontmatter `status` at the candidate. The regenerated pair and the `done` commit message carry the gate's evidence.

**Acceptance Criteria:**
- Given eight current root project results, when `AC-7.2-01` runs, then artifact-derived per-project and summed counts validate as acceptance-result v1.
- Given each named fault, when `AC-7.2-02` through `AC-7.2-10` run, then each proves its exact blocker and restores its fixture byte-identically.
- Given ten passing prerequisite scenarios and the verified Story 7.1 digest, when `AC-7.2-11` runs, then the pair binds every measured fact and reports `11/11/0/0/0/0`.
- Given existing v1 and Story 7.1 tests, when rerun, then their behavior and Story 7.1's committed pair remain valid.
- Given the committed retraction candidate, fresh TRX for all eight root projects, and passing `AC-7.2-01` through `AC-7.2-10`, when `AC-7.2-11` runs twice, then both runs exit `0` with identical bytes and `11/11/0/0/0/0`, and a rerun after the pair and lifecycle commits reproduces the same pair.

## Implementation Notes

### Completion under owner waiver, 2026-09-29

On 2026-09-29 the owner, Jerome Piquot, was asked explicitly and chose **waive and complete**. This decision supersedes the terminal-gate item of the 2026-09-27 completion repair below. The evidence-boundary `PASS`, the resolver `PASS`, and Story 7.1 terminal AD-4 `ACCEPTED` authority are waived as Story 7.2 completion gates. Run them anyway and record their actual exit, state, and blocker; never report them as `PASS`. Do not create a V30 successor, a trusted host, or any authority publication. The AD-4 inspector pins the old Story 7.2 pair's hashes, so it reports that pair changed at the new candidate. That is an expected consequence of the waiver, not a defect to fix. The mandatory gates are the current-candidate v2 record, the generator regression suite, and `python3 scripts/check-root-submodules.py --repository .`.

At `76e113621b00028258041c20335372182f98d0a8`, `AC-7.2-11` exits `1` with `CANDIDATE_NOT_FINAL` and `GITLINK_DRIFT`. After the recorded candidate `170ac9d`, nine root gitlinks moved (all except `Hexalith.Builds`). Recovery follows the runbook's superseded-pair procedure (`docs/runbooks/story-final-record-generation.md:416`).

Implementation handoff (step 03): make the runbook edit and delete the old pair in the working tree. Verify with `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`, the root-submodule check, and `git diff --check`. Leave everything uncommitted.

Completion procedure (parent, step 05, after review; submodule checkouts must match their gitlinks):
1. Commit the candidate: the retraction, the runbook paragraph, and this spec with its final review notes. After this commit, only the spec's `status` value, the Story 7.2 sprint row, and `last_updated` may change.
2. Copy the existing `artifacts/v9/7.2` results (`cp -p`) to `artifacts/v9/7.2-superseded-170ac9d/` before overwriting them.
3. Run `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true`. Confirm DAPR ports 3500 and 50001 are free, then set `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`. Run each of the eight `tests/*` executables under `bin/Release/net10.0/` with `-trx <abs>/artifacts/v9/7.2/test-results/<project>.trx`. All results must be nonempty and passing, with zero skips.
4. Run `AC-7.2-01` through `AC-7.2-10` exactly as the contract declares them, through `uv run --frozen --no-sync`. Then run `AC-7.2-11` twice and require identical bytes. Commit only the pair (`docs(evidence): ...`).
5. Set the spec `status` and the sprint row to `done`. Commit them, rerun `AC-7.2-11`, and confirm it reproduces the committed pair. Validate every commit message with the pinned commitlint. Do not push.

### Completion repair requested on 2026-09-27

The current request resumes the completed implementation from `28d7b6b677e5c5c652b58dab27278d7426222bb3` and authorizes a bounded repair of `.agents/skills/bmad-build/step-05-present.md` and its byte-identical `.claude/skills/bmad-build/step-05-present.md` twin. Existing execution tasks above are implemented; do not regenerate or retract their historical record. Work on current `main`, preserving the later EventStore gitlink update. Do not push or change the hold, planning authority, predecessor record, generator, schemas, other workflow routes, or sprint status. Broader workflow integration remains Story 7.3.

- [x] In step 05, select Story 7.2's frozen v2 `--contract` invocation before the legacy final-record procedure. Use only the five accepted v2 options, the pinned Python environment, and both contract-declared output paths. Check exit `0`, the final-record v2 schema, story identity, and the contract's exact summary; v2 success has no legacy nested `result` or `markdown` fields.
- [x] Verify the JSON and Markdown bytes against the committed pair and against a second identical invocation. Preserve historical evidence and result artifacts. Document isolated historical reproduction separately from current-candidate completion in `docs/runbooks/story-final-record-generation.md`; a historical reproduction cannot pass a current gate. Do not use v1 digest flags with v2, synthesize result timestamps, or change candidate-retention rules.
- [x] Before any terminal transition, require all explicit Story 7.2 gates, including boundary verification and separate terminal `ACCEPTED` authority for Story 7.1. A `done` row, raw passing record, historical lift, or routine-change policy cannot substitute for that authority. Record blockers and retain Story 7.2 `in-progress` when a required gate does not pass. Do not invent a trusted host.

Verification: inspect a freshly rendered step 05, run the existing generator regression suite through pinned `uv`, and retain commands/results for the historical reproduction, current v2 invocation, evidence-boundary verifier, and authority resolver. The parent workflow records the audit and validates any commit with pinned commitlint after review. The implementation handoff must leave its bounded workflow/runbook repair uncommitted and preserve this spec's historical notes.

The investigation results and exact validation evidence are retained in [the completion resume audit](story-7-2-completion-resume-2026-09-27.md). The historical pair reproduces twice at the requested revision, while current `main` fails `CANDIDATE_NOT_FINAL` / `GITLINK_DRIFT` after the preserved EventStore bump. Boundary and resolver both reproduce the V24 gitlink blocker with active hold. Separate Story 7.1 terminal `ACCEPTED` authority is not established. The story remains `in-progress`; no historical record or result artifact is replaced.

- [ ] Current-candidate v2 and evidence-boundary completion gates pass, and Story 7.1 terminal `ACCEPTED` authority is verified. These required completion conditions remain blocked; passing repair verification is not story acceptance. *Superseded 2026-09-29 by the owner waiver above.*

### Earlier implementation and review history

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

### Review Findings

Review pass 2, 2026-09-25, over the story-owned diff `c69334c..f5a11e7` (the same six paths as pass 1). Layers: blind hunter, edge-case hunter, verification gap, and acceptance auditor, all completed.

- [ ] [Review][Patch] (resolved from Decision, 2026-09-25: Jerome chose option 1, exempting `sprint-status.yaml` and the Story 7.2 spec as lifecycle bookkeeping in the record-only successor check, with a test and runbook text) A lifecycle commit after the record supersedes it, so AC-7.2-11 cannot be reproduced at HEAD — `f5a11e7` changes only the spec `status:` line and `sprint-status.yaml` after candidate `26da803`. `v2_story_7_2_candidate` allows only the two output paths to follow the candidate, so a rerun at `f5a11e7` exits `1` with `CANDIDATE_NOT_FINAL`. The runbook's retract-and-regenerate recovery never converges, because every later status move supersedes the new pair too. Measurements read the spec from the candidate commit, not from HEAD, so a later status edit cannot change any measured fact. (medium; blind+edge)
- [ ] [Review][Patch] Candidate retention reads the pair from the working tree, but the runbook says a *committed* pair pins the candidate [_bmad/scripts/generate_story_record.py:3883]. A leftover uncommitted pair blocks every rerun after a source commit. Read the pair from `HEAD`, and commit the pair in the `test_v2_blocks_superseded_candidate` setup. (low; auditor)
- [ ] [Review][Patch] No command-line test proves `story_id` reaches `v2_gitlinks`: the `GITLINK_SCOPE_MISMATCH` code and the repeated-path check are asserted only through direct calls [_bmad/scripts/generate_story_record.py:4170] (medium; verification-gap)
- [ ] [Review][Patch] Gitlink exclusion from `changedPaths` is never exercised: no fixture moves a gitlink between baseline and candidate, although the real history moves seven [_bmad/scripts/generate_story_record.py:3957] (medium; verification-gap)
- [ ] [Review][Patch] No test covers an orphaned prior candidate (not an ancestor of HEAD) raising `CANDIDATE_NOT_FINAL` [_bmad/scripts/generate_story_record.py:3903] (medium; verification-gap)
- [ ] [Review][Patch] The staleness floor from changed-source mtimes is never exercised: no TRX is newer than the commit but older than a changed source [_bmad/scripts/generate_story_record.py:4016] (medium; verification-gap)
- [ ] [Review][Patch] The schema's Story 7.2 `measurements` `if`/`then`/`else` is never tested with a rejection case (7.2 without `measurements`, 7.1 with them) [_bmad/schemas/story-final-record-v2.schema.json:22] (medium; verification-gap)
- [ ] [Review][Patch] AC-7.2-10's "valid ancestry" half is untested: only an unresolvable all-zero baseline is tried, never a resolvable non-ancestor [_bmad/scripts/tests/test_generate_story_record.py:3845] (medium; auditor+blind)
- [ ] [Review][Patch] `test_v2_blocks_invalid_predecessor_record` captures `before` but never asserts byte-identical restoration [_bmad/scripts/tests/test_generate_story_record.py:3953] (low; auditor+blind)
- [ ] [Review][Patch] The `tampered-json` predecessor fault changes `storyId`, so the digest and projection check is never isolated; tamper with a non-identity field instead [_bmad/scripts/tests/test_generate_story_record.py:3955] (low; blind)
- [ ] [Review][Patch] The second phase of `test_v2_blocks_superseded_candidate` commits after the fixture's TRX anchor (+10 s), so a slow runner adds `TEST_RESULTS_STALE` and the exact-blocker assertion fails. Rewrite the TRX after the commit [_bmad/scripts/tests/test_generate_story_record.py:3849] (low; edge)
- [ ] [Review][Patch] A TRX naming another assembly is reported as `TEST_FAILED`; it is a missing result for that project (`TEST_RESULTS_MISSING`), per "keep result states distinct" [_bmad/scripts/generate_story_record.py:4044] (low; auditor)
- [x] [Review][Defer] Test-project `tests/` filtering against a `.slnx` that has `src/` projects and `File` entries, and the approved-skip PASS path, run only on idealized fixtures [_bmad/scripts/generate_story_record.py:3993] — deferred: the current spec approves no skips and the live run passed
- [x] [Review][Defer] TRX freshness is mtime-only, and submodule checkout HEADs are not compared with gitlinks [_bmad/scripts/generate_story_record.py:4015] — deferred: already recorded in `deferred-work.md` (2026-09-24, pass-1 B3/E4/E5); no new entry

Rejected:
- Gitlink baseline-to-candidate delta missing from the record — low: the baseline is bound, so the delta is derivable; the fix adds record surface.
- `changedPaths` not story-scoped — false: pass-1 B11; the spec defines the frozen baseline-to-candidate set.
- Test projects missing from `.slnx` go unnoticed — low: every `tests/*.csproj` is in the `.slnx` today; the fix adds a guard.
- Skip allow-list not scoped by project — low: no skips are approved; the fix adds a branch.
- Static self-invocation ledger omits Story 7.2 checks — low: PASS is unreachable unless every measurement check passes, and the facts are bound in `measurements`.
- TRX not archived, run conditions not recorded — low: TRX digests are bound, and the skip gate catches the AppHost skip; the fix adds new surface.
- Story-specific synonym codes; schema hard-codes `7.2` — false: pass-1 B8; the frozen matrix mandates these names.
- Contract-path vs `story_id` gating diverge — false: `safe_relative_path` rejects non-normalized spellings (pass-1 B9).
- `head` may be `None` in the measurement supersession check — false: HEAD already resolved in `v2_generate`; the duplicate check is pass-1 V-o3.
- Repeated `.gitmodules` path checked only for 7.2 — false: the spec forbids changing Story 7.1 behavior.
- Pair verification does not cross-check totals — low: pass-1 B2.
- Absent-spec, `.slnx`-count, and unreadable-TRX branches untested — low: unlikely, and each needs new fixtures.
- Tests lack `try`/`finally` cleanup — false: every test builds its own `tmp_path` fixture.
- "JUnit" preamble, hard-coded "eight", `scenarios[:10]`, asserting 10 gitlinks — low: pass-1 B12b; the contract is frozen.
- Control-character path or bad JSON escape becomes `INTERNAL_ERROR` — low: pass-1 E1/E3.
- Symbolic-ref `baseline_commit` — low: the committed spec uses a 40-hex id; the fix adds a guard.
- Removed baseline gitlink recorded as a root path — low: pass-1 E2.
- Future-dated TRX mtime, symlinked prior output, `GateError` in `changed_gitlinks` — low: unlikely; each fix adds a guard.
- Predecessor ancestry and passing state not checked — low: pass-1 B15; the committed 7.1 record is `PASS` and an ancestor; the fix adds guards.
- Fixture project glob vs `.slnx` inventory — low: the two agree today.
- Runbook overstates input binding and the fixed project count — low: wording only; binding is deferred above.
- Path set compared against itself — false: epic-7 context requires one derived set from Git objects, with no declared list.
- Recovery "rewrites history" — false: `09b9bf4` remains in history; the retraction is an additive commit.
- AC-7.2-08 no-traversal assertion and monkeypatched fixture — low: pass-1 B4.
- AC-7.2-01 validates a test-built acceptance document — low: pass-1 B14.
- Committed pair not checked by any test — low: the generator rerun is the check.
- Spec status and Implementation Notes contradict the record (notes say regenerated at `c6fc53b` and "status stays `in-progress`"; the record binds `26da803`) — rejected by rule: the fix edits the spec under review; owner to reconcile.

### Review pass 3, 2026-09-27

The review used the full `c69334c..HEAD` diff required by this spec's baseline. Changes from the intervening current-change policy and root-submodule work are triaged separately from Story 7.2 changes.

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| G1 | Root checker has no duplicate-declaration regression test | low | defer | The checker rejects duplicates, but none of its three tests supplies one; this checker predates the resumed Story 7.2 change. |
| G2 | Unrelated sprint-status edits retain the old candidate | medium | patch | `V2_7_2_LIFECYCLE_PATHS` exempts the whole sprint file; the reviewer committed an unrelated row and the generator still passed. |
| E1 | A symbolic baseline can move | low | reject | carried: the prior review rejected this exact symbolic-ref claim; the committed Story 7.2 spec uses a full 40-hex commit ID. |
| E2 | Unrelated sprint-status edits retain the old candidate | medium | patch | The same whole-file exemption at `v2_story_7_2_candidate` accepts changes outside Story 7.2's row. |
| E3 | Foreign TRX result IDs count toward this project | medium | patch | `parse_trx` derives assembly names from definitions but never relates `UnitTestResult.testId` to those definitions. |
| E4 | Zero executed with a passing result is accepted | medium | patch | `count_disagreements` bounds `executed` by total but does not compare it with passed and failed outcomes. |
| B1 | Current API and legal-rule checks are excluded with the architecture conformance class | medium | defer | CI excludes the entire class while it still contains `StillBindingReplayProjectionParticipantIdempotencyAndLegalRulesShouldRemain` and public API checks; this CI choice predates this Story 7.2 resume. |
| B2 | Default Python and CI tooling lanes omit the Story 7.2 suite | medium | defer | `pyproject.toml` selects only `tests/tooling`; the generator suite ran locally for this story but is absent from routine CI. |
| B3 | Whitespace-only pre-commit hook does not guard workflow text | low | defer | `.githooks/pre-commit` only runs `git diff --cached --check`; the proposed fix edits agent workflow context outside this story. |
| B4 | Root checker compares worktree `.gitmodules` with index gitlinks | medium | defer | `check()` reads the file from disk and `git ls-files --stage` for gitlinks; a staged tree can differ from the validated tree. This checker predates this story. |
| B5 | Root checker accepts a 40-hex gitlink whose object is unavailable | low | defer | The checker validates only mode and shape, so it cannot establish target availability; that current-tree checker is independent of Story 7.2. |
| B6 | Routine BMad completion can rely on focused tests alone | medium | defer | The generic step-05 text permits completion after focused tests; a fix edits agent workflow context and does not change this story's full eight-project run. |
| B7 | Unrelated sprint-status edits retain the old candidate | medium | patch | The same unrestricted lifecycle exemption at `_bmad/scripts/generate_story_record.py:3934` allows the demonstrated unrelated row. |
| B8 | A symbolic baseline can move | low | reject | carried: same location and claim as the prior symbolic-ref review row; this story's committed baseline is immutable. |
| B9 | TRX freshness does not bind tested binaries to the candidate | medium | defer | carried: the first review already deferred this exact mtime-only provenance issue to `deferred-work.md`. |
| B10 | A filtered, nonempty test run can produce a passing record | medium | defer | The inherited TRX parser has no authoritative suite enumeration; the frozen Story 7.2 contract requires nonempty per-project results, and full suites were run for this candidate. |
| B11 | Zero executed with a passing result is accepted | medium | patch | The reviewer reproduced `{total:1, executed:0, passed:1}` without a parser disagreement. |
| B12 | Foreign TRX result IDs count toward this project | medium | patch | Result IDs are not matched to `TestDefinitions`, so a foreign result can count under the expected assembly's definition. |
| B13 | Root test inventory filters on `tests/` | low | reject | carried: the prior review rejected this exact claim; all eight current root test projects are under `tests/`. |
| R1 | Exact sprint comparison would reject valid `last_updated` metadata | medium | patch | `sync-sprint-status.md` updates `last_updated` with a lifecycle transition; the review patch now permits that date with Story 7.2's status while rejecting unrelated rows. |
| R2 | A zero-run result gained an extra missing-ID blocker | medium | patch | The full generator suite caught `TEST_RESULTS_MISSING` beside the required `TEST_NOT_RUN`; the ID check now runs only when result rows exist. |

### Completion repair review, 2026-09-27

Reviewed the bounded repair diff from the current main commit, preserving the earlier full Story 7.2 reviews above. Three independent reviewers ran: blind hunter, edge-case hunter, and verification-gap reviewer. Edge-case and verification-gap reviews reported no findings. No lifecycle transition to review/done was made.

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| C1 | Restoring changed outputs mentions bytes but omits their original timestamps | medium | patch | Restore and verify preserved bytes and metadata in both step-05 twins and runbook. |
| C2 | Generic operator procedure and legacy checklist follow the preservation repair without an exclusion | medium | patch | Explicitly exclude this repair from those subsequent regeneration and legacy steps. |
| C3 | Durable audit omits the generator regression result | low | patch | Record the full run's 170 passes and parity failure plus the corrected parity rerun, separately from historical reproduction. |
| C4 | Durable audit omits rendered-step and twin-parity evidence | low | patch | Record the rendered step identity, contract-command comparison, and byte-identical twins. |
| C5 | Audit summarizes blocked assertion ledgers without retaining exact rows | low | patch | Preserve both complete ledgers with their exact command/revision bindings. |
| C6 | Terminal prerequisite needs all AD-4 publication/integration bindings and a clear missing-authority disposition | medium | patch | Include atomic authority/pointer publication and accepted tree/gitlink equality; explicitly retain the missing terminal publication blocker. |
| C7 | Archived-byte comparison could incorrectly constrain a future authorized successor | low | patch | Limit that comparison to the present preservation repair; a future successor requires its own scope and current contract gates. |

### Waiver completion review, 2026-09-29

The review covered this round's uncommitted change against `76e1136` (runbook waiver text and the pair retraction). The spec was excluded from the blind and verification-gap inputs. Earlier `c69334c..HEAD` content was already triaged above, and the AD-4 tooling under its own spec. Layers: blind hunter (B), edge-case hunter (E), verification gap (V).

| ID | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| B1, E1, E2 | Archive command `cp -p artifacts/v9/7.2 …/` cannot copy a directory; a rerun could nest into or overwrite the archive | medium | patch | Reproduced: GNU `cp -p` on a directory prints `-r not specified; omitting directory` and exits `1`. Use `cp -a` with no trailing slash, and refuse if the destination exists. |
| B2, E3 | Historical-reproduction steps still copy inputs from `artifacts/v9/7.2`, which regeneration overwrites | low | patch | The reproduction section reads the live results path; after regeneration the `170ac9d` inputs exist only in the local ignored archive. |
| B3, V3 | New text retracts the pair in the candidate commit; the older rule says "in its own commit" | low | patch | Wording conflict only: `v2_story_7_2_candidate` treats `HEAD` as the candidate once both outputs are absent. State the exception explicitly. |
| B4, E7, V4a | Runbook says the AD-4 inspector reports the pair "changed" | low | patch | `inspect_story_7_1_acceptance.py:196` emits `AD4_EVIDENCE_ABSENT` while the pair is retracted, and `:297` raises `AD4_RECORD_BYTES_CHANGED` (`FAIL`) after regeneration. Name both. |
| B6, V4b | `docs/runbooks/story-7.1-ad4-acceptance.md:85-86` still lists the retracted hashes as preserved bytes | low | patch | One explanatory sentence; the pinned bytes stay in Git history at `aefe400`. |
| B5, E10, V1 | `test_actual_revision_preserves_all_existing_evidence_bytes_and_mtimes` reads the pair from the working tree and is run by no prescribed check | medium | patch | Reproduced as `FileNotFoundError` with the pair absent. CI runs only `test_check_root_submodules.py` from `tests/tooling`. Require the suite after the pair commit. |
| B7 | Waived-gate results have no durable destination after the candidate commit | medium | patch | Only the spec `status`, the sprint row, and `last_updated` may change after the candidate. Record the results in the lifecycle commit message body. |
| B8, E5 | "Check for the Story 7.1 terminal publication" names no command | low | patch | Name the AD-4 inspector `--check` invocation at the candidate. |
| B9, E9 | Superseded gate text and the "do not regenerate/replace" preambles (`:636-638`, `:657-659`) are unqualified; the done transition is not explicitly authorized | low | patch | Add a pointer at the top of the current-gates section, qualify both preambles, and authorize the spec/sprint `done` transition. |
| B10, E6 | The runbook lacks the Story 7.2 remeasurement procedure and the generator-suite command | medium | patch | Operator procedure covers only five scenarios. The AppHost env var, DAPR ports, and project-reference build flag appear only in the spec. |
| E4 | "Never report a waived gate as `PASS`" conflicts with recording the actual result | low | patch | Direct wording correction: record what was observed; a waived gate is non-gating either way. |
| B12 | The runbook omits the post-lifecycle rerun that the new spec AC requires | low | patch | Direct addition. |
| E8, V2 | Both `bmad-build/step-05-present.md` twins still require the waived gates and forbid retraction | medium | defer | The fix edits agent-context skill files. For this run, the parent applies the owner waiver at step 05. |
| B11 | Regeneration widens `changedPaths` to foreign commits with no disclosure, and the record shows no waiver | low | reject | carried: pass-1 B11 (the record reports the frozen baseline-to-candidate set). The closed schema admits no waiver field; disclosure goes in the lifecycle commit message. |

## Design Notes

Read each project's TRX at `artifacts/v9/7.2/test-results/<project>.trx`, deriving `<project>` from the committed `.slnx`. The direct Story 7.2 request authorizes implementation. Current planning authority lacks Story 7.1 terminal `ACCEPTED`; report that condition without claiming its lifecycle gate passed.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py` -- v1, 7.1, and 7.2 pass.
- Run `AC-7.2-01` through `AC-7.2-10` exactly as declared in the frozen contract through pinned `uv` -- current nonempty JUnit results; each negative fixture proves its blocker.
- Run `AC-7.2-11` exactly through pinned `uv` after committing source -- schema-valid `PASS`, deterministic pair, ten gitlinks, `11/11/0/0/0/0`.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline c69334cb13a981c9112ad687427b1f43fafc2988 --candidate HEAD` -- record its actual state before review/done.
- `git diff --check` -- no whitespace errors.
