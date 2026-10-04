---
title: 'Enforce the 52-decision/28-acceptance zero-gap validator'
type: 'feature'
created: '2026-10-03'
status: 'in-progress'
baseline_commit: '8f2594db2f6e29e6eb98e2fea90467d615674dd0'
implementation_start_commit: 'f415298801097caa5f745e6976d67a79e4e2aab4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-8-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 8.1 provides the preserved UX disposition, but the required Story 8.2 fault selectors and measured zero-gap final-record bindings are missing.

**Approach:** Validate the existing bundle read-only, prove each frozen fault, and generate a candidate-bound record.

## Boundaries & Constraints

**Always:** Follow unchanged `8.2.json` commands and the current-change runbook. Validate ordered, unique 52/28 inventories, sources, ownership, rendering, and non-activation. Bind `SC-8.2`, verified Story 8.1 and disposition digests, source/output hashes, measured mutation/restoration evidence, and inventory digest. Require `11/11/0/0/0/0` before completion.

**Never:** Rewrite accepted Story 8.1 outputs/records, UX sources/mappings, planning contracts, production UI/runtime, gitlinks, or frozen planning history. Do not activate UX or assert release authorization. Rollback removes only Story 8.2 validation, fixtures, results, and record.

## I/O & Edge-Case Matrix

| Input / state | Expected behavior | Blocker |
| --- | --- | --- |
| Complete preserved 52/28 bundle | Read-only PASS, exit 0 | None |
| Missing, duplicate, or unknown ID | Exit 1; exact identity failure | `UX_DECISION_MISSING/DUPLICATE/UNKNOWN`, `UX_ACCEPTANCE_MISSING/DUPLICATE/UNKNOWN` |
| Missing owner or source hash | Separate fixtures fail | `UX_OWNER_MISSING`, `UX_HASH_MISSING` |
| Source, rendering, or ordering drift | Separate fixtures fail | `UX_SOURCE_DRIFT`, `UX_RENDER_DRIFT`, `UX_ORDER_DRIFT` |
| Activated row or invalid current story | Separate fixtures fail | `UX_ACTIVATION_UNAUTHORIZED`, `UX_CURRENT_STORY_INVALID` |
| Undetected fault or unequal restoration hashes | Completion fails | `FAULT_NOT_DETECTED`, `FIXTURE_NOT_RESTORED` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v9/story-contracts/8.2.json`: frozen commands and inventory; its bytes match the authority bundle.
- `_bmad/scripts/generate_ux_preservation_disposition.py`: reuse schema, frozen IDs, derivation, renderer, and predecessor verification.
- `tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs`: five positive facts; preserve source/row/provenance and Story 8.1 candidate checks.
- `.github/workflows/ci.yml`: current conformance excludes retired historical inventory comparisons; all six UX facts remain enabled.
- `_bmad/scripts/generate_story_record.py`: pytest/xUnit routes exist; reuse Story 7.4's observed JUnit properties. Its closed schema reserves UX binding for 8.1 and observed faults for 7.4.
- `docs/release-evidence/story-8.1-final-record-v2.json`: immutable predecessor; completed 8.1 spec supplies continuity.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad/scripts/generate_ux_preservation_disposition.py` — verify existing bundles read-only against canonical derivation. Classify semantic failures before schema errors; enforce deterministic Markdown and order parity.
- [x] `_bmad/scripts/tests/test_generate_ux_preservation_disposition.py` — implement exact selectors for all 13 fault categories, including historical/nonexistent ownership and JSON/Markdown ordering. Measure baseline PASS, exit 1 with the exact blocker, `finally` restoration, equal hashes, and restored PASS. Export JUnit properties; AC-10 repeats the matrix. Resolve selector overlap and test the verification CLI.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs` — prove rendering and zero-gap parity through the verifier; retain Story 8.1 checks.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — add an 8.2-only closed UX binding and observed ledger. Verify predecessor pair/candidate compatibility, committed inputs/inventories, exact faults, measured exit/blockers, restoration parity, and stamped nonempty results. Reject missing/stale/failed/empty evidence and altered bindings; preserve other stories.
- [x] `docs/runbooks/story-final-record-generation.md` — document verification, Story 8.2 blockers, result properties, and candidate/build prerequisites.
- [x] `.github/workflows/ci.yml`, `_bmad/scripts/generate_story_record.py`, `_bmad/scripts/tests/test_generate_story_record.py` — exclude the historical fixed-446 assertion comparison from current CI, permit that exact workflow path in Story 8.2 scope, and prove other workflows and product paths remain forbidden. Confirm both current validation lanes pass.
- [x] `docs/release-evidence/story-8.2-final-record-v2.json`, `docs/release-evidence/story-8.2-final-record-v2.md`, this spec, `_bmad-output/implementation-artifacts/sprint-status.yaml` — run acceptance commands; generate, insert, and verify the record before done.

**Acceptance Criteria:**

- Given Story 8.1, when AC-01 runs, then complete ordered 52/28 preserved obligations pass with no skipped/not-run tests.
- Given each required mutation, when AC-02 through AC-09 run, then the validator exits 1 with its exact blocker and restored fixtures pass.
- Given all mutations, when AC-10 runs, then every before/after hash matches and coverage is complete.
- Given current PASS results and compatible Story 8.1, when AC-11 runs, then the record binds required facts with `11/11/0/0/0/0`.

### Review Findings

Review 2 (2026-10-03; baseline `8f2594d`; reviewed the full `8f2594d..d562ade` diff, 15 files, +3,032/−29; record pair `e7b524f` retracted in `5bf53d7`). Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; none failed. Before the retraction, the pair verified with `--verify-inserted-record` at `d562ade` against candidate `fdc7b87`, and the DLL and ten receipts matched their recorded digests.

- [x] [Review][Decision] Spec-add commit sits outside the Story 8.2 scope guard, and its four gitlink moves are neither checked nor disclosed — resolved (acknowledge, 2026-10-03): the four moves are the owner's own pushed gitlink bumps, the guard already proves the implementation moved no gitlink, and Implementation Notes now state what the record binds; no code change. `v2_8_2_facts` starts both `committed_path_status` and the gitlink equality at the spec-add commit `f415298`, so that commit's own changes are never examined. It moves `references/Hexalith.Folders` `a20127c→92da4b0`, `Parties` `937cb2a→5388884`, `Projects` `049d24f→0f03582`, and `Tenants` `bfc10cb→c0afce2`. The record names neither `baseline_commit` `8f2594d` nor this delta, although Implementation Notes say "the final record retains the full planning-baseline delta". [_bmad/scripts/generate_story_record.py:6366] (B1, B2, E3, A2, A3)
- [x] [Review][Patch] The new CI-run conformance fact cannot pass in the CI checkout — resolved from decision (publish a tag, 2026-10-03): push `ba1b476` to `refs/tags/evidence/story-7.4-candidate` on origin, which CI's checkout fetches with every other tag, then confirm on the first CI run that both lanes find it and that the conformance job's `python3` imports `jsonschema`. `PreservedBundleShouldPassZeroGapVerification` runs `verify()`, whose `generate()` reads sources at the Story 7.4 candidate `ba1b476`. No ref on origin reaches that commit (the backup branch is gone), and a main-only clone exits `1` with `UX_SOURCE_UNBOUND` (reproduced). `ci / conformance` does not exclude this class. The same missing object already fails 15 `ci / repository` tests since `f415298`, and the new real-Git pytest will join them. Whether the conformance job's unprovisioned system `python3` has `jsonschema` remains unverified. [tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs:27] (B3, E10, V7)
- [x] [Review][Patch] The accepted-evidence archive that the runbook requires was never made — resolved from decision (archive at the replacement candidate, 2026-10-03): once the new pair is accepted, copy the Release output directory and the ten receipts with preserved modification times to `~/hexalith-evidence/conversations/story-8.2/<sourceRevisionId>/`, with a SHA-256 manifest, and verify it against the record. the runbook requires archiving the Release output, the ten receipts, and a manifest under `uxValidation.sourceRevisionId`, outside the working tree. No archive exists, and both locations are gitignored, so the next conformance build overwrites the bound DLL (`34bfb89e…`) and the record can no longer be reverified. [docs/runbooks/story-final-record-generation.md:1322] (B12)
- [x] [Review][Patch] Story 8.2 CLI integration test fails on every clean checkout — it clones `HEAD` and commits workspace copies that already equal `HEAD`, so `git commit` exits `1` (reproduced: 1 failed, 46 passed). With dirt it would stop at `CANDIDATE_NOT_FINAL`, because `HEAD` now carries the 8.2 pair and the done spec. It is the only coverage of the `verify_spec` rederivation and the 8.2 record wiring; `ci / repository` turns red, and the Verification "no failures" claim does not hold. Build the fixture from `implementation_start_commit` with the candidate implementation files overlaid [_bmad/scripts/tests/test_generate_story_record.py:8134] (E1, V1, A1)
- [x] [Review][Patch] Story 8.2 record-schema conditional is untested — add a published-pair test modeled on the Story 8.1 one, rejecting a record without `uxValidation`, a truncated or reordered `faultInjection.results`, and a non-8.2 record carrying `uxValidation` [_bmad/schemas/story-final-record-v2.schema.json:328] (V2)
- [x] [Review][Patch] Story 8.1 disposition-compatibility check is untested — add a `predecessor-disposition` case where committed and installed bytes agree but differ from `predecessor.uxDisposition`, expecting `AUTHORITY_BINDING_INVALID` [_bmad/scripts/generate_story_record.py:6381] (V3)
- [x] [Review][Patch] Protected Story 8.1 paths removed from `V2_8_2_ALLOWED_PATHS` are unpinned — parametrize scope faults over the 8.1 spec, disposition outputs, 8.1 record pair, and `PlanningAuthorityV8ValidationTest.cs`, expecting `UX_PRODUCTION_CHANGE_FORBIDDEN` [_bmad/scripts/generate_story_record.py:2789] (V4)
- [x] [Review][Patch] Observed-fault schema does not pin outcomes — `uxObservedFault` uses generic exit codes and blockers, while successors accept an 8.2 predecessor by schema and pair digest alone. Pin baseline and restored exit `0` with empty blockers, observed exit `1` with one blocker, and each id's expected blocker, as Story 7.4's `observedFault` does [_bmad/schemas/story-final-record-v2.schema.json:1818] (B10, A4)
- [x] [Review][Patch] Per-lane fault-set check is unpinned — add lane-missing, lane-duplicate, and lane-reordered mutations, expecting `FAULT_NOT_DETECTED` rather than the later `FIXTURE_NOT_RESTORED` [_bmad/scripts/generate_story_record.py:6481] (V5)
- [x] [Review][Patch] Verifier schema-bytes check is untested — a loosened installed schema verifies when it is removed; add a `schema-changed` branch expecting `UX_SCHEMA_INVALID` [_bmad/scripts/generate_ux_preservation_disposition.py:444] (V6)
- [x] [Review][Patch] Executed verifier is not bound to the candidate — load `generate_ux_preservation_disposition.py` from `--repository` (already checked by `committed_input`), not from `Path(__file__)` [_bmad/scripts/generate_story_record.py:6388] (B11, E2)
- [x] [Review][Patch] `markdown-order` fixture writes text with platform newlines and locale encoding — write UTF-8 bytes so CRLF cannot turn `UX_ORDER_DRIFT` into `UX_RENDER_DRIFT` [_bmad/scripts/tests/test_generate_ux_preservation_disposition.py:306] (E8)

Rejected (Review 2):

- `false` B5 — `verify()` prints PASS only after both inventories equal the frozen 52/28 lists, so the literals cannot misstate a passing count.
- `false` B6 — an `N/A` owner or hash still exits `1` (`UX_CURRENT_STORY_INVALID` or `UX_SOURCE_DRIFT`); nothing accepts N/A.
- `false` B8 — first-failure precedence is the documented contract: semantic failures precede schema failures so each fixture has one exact blocker.
- `false` B9 — the spec maps historical and nonexistent ownership to the same `UX_CURRENT_STORY_INVALID`, and every non-canonical owner is rejected.
- `false` B16 — AC-8.2-08's "or order is changed" covers JSON order, `markdown-order` keeps JSON unchanged, and the task names JSON/Markdown ordering.
- `false` E4 — needs a hand-written invalid double-quoted escape in tool-written frontmatter, and still fails loudly as `BLOCKED`.
- `false` E9 — the module lives in the repository; local and CI runs always have Git and a work tree.
- `false` V8 — frozen AC-01 runs the whole class, the ledger lists only executed tests, and the generator scope check owns the 8.2 boundary.
- `false` A6 — the runbook's general tables already document every listed code.
- `low` B4 — the verifier writes one line, so stderr cannot fill the pipe; async reads and timeouts add machinery.
- `low` B7 — a missing output or a missing status still fails closed with exit `1`; reclassifying them adds branches.
- `low` B13 — the `DispositionError` pass-through is a one-line wrapper over 57 verifier tests, both Markdown sections share one loop, and the mocked receipt test is superseded by the CLI-test patch.
- `low` B14 — recording counts in the spec is a spec edit; the false "no failures" claim is carried by the CLI-test patch.
- `low` B15 — the unused parameter, repeated `git show`, hard-coded selectors, and duplicated fault map mirror a frozen, digest-bound contract; equality tests add machinery.
- `low` E5 — accumulated findings are dropped only when a receipt is also missing, and the run still fails closed with a valid blocker.
- `low` E6, E7 — need deliberate measurement against dirty code or fabricated digests; rederivation adds machinery.
- `low` A5 — the frozen selectors call the real `main()`, and the real-process test exists; switching fixtures to real Git adds machinery.

## Implementation Notes

Review 2 local patches were implemented and verified on 2026-10-03. The required
two-file Python suite passed all 611 tests with zero failures or skips; the
focused regression selection passed 94 tests. The replacement record and exact
evidence archive will be generated from the committed replacement candidate.
The owner authorized remote publication on 2026-10-04. The historical candidate
tag now resolves on origin to `ba1b47622ad8d72e0ea70f01e4e695e49413c1ee`.
The first CI run passed all six UX conformance facts, proving both historical
source reachability and the runner's system Python `jsonschema` import. The sole
conformance failure was the retired rc.2 comparison of the current assembly with
its frozen 446-assertion inventory: six additive UX facts, no removed assertions.
The current-change policy puts that historical comparison outside current CI.
The accepted pair was retracted in `bd3a3f8` before the CI correction and exact
workflow scope allowance; a replacement record will bind the corrected candidate.

The preserved planning baseline predates the commit that added this spec and
updated four root gitlinks. `implementation_start_commit` records the task-entry
revision for the Story 8.2 no-production-change boundary. The final record binds
that start in `uxValidation.implementationStartCommit` and the candidate gitlinks;
it does not restate the baseline delta. The four gitlink moves in the spec-add
commit are the owner's acknowledged bumps (Review 2), and the validator requires
the task-entry and candidate gitlink identities to match.

Review 3 local patches also passed: 618 tests in the required two-file suite
with zero failures or skips, and eight focused tests under Python 3.11.15.
Isolated mutation checks proved the new tests fail when canonical equality
(two failures) or exact testcase attribution (three failures) is removed.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | high | patch | Synthetic agreeing hashes pass the consumer without binding the committed fixture; derive and require the candidate fixture baseline. |
| BH-02 | false | reject | The spec-add revision is the measured task-entry tree. Its four inherited gitlink changes preceded this task and are retained explicitly; implementation changes after that tree remain governed. |
| BH-03 | high | patch | Inserted verification trusts recorded scenario commands and ledgers. Rederive the complete Story 8.2 scenarios from frozen commands and current artifacts. |
| BH-04 | medium | patch | The new completion branch omits remeasurement of top-level authority, inventory and gitlinks. Compare those Story 8.2 facts with candidate objects and contract. |
| BH-05 | medium | patch | Nine forwarded UX codes are absent from V2_CODES and raise ValueError. Register the required semantic blockers and verify failure receipts. |
| BH-06 | medium | patch | Retained receipt deletion and malformed XML can escape as raw exceptions. Return named missing or malformed-evidence failures. |
| BH-07 | medium | patch | Retained receipt reads omit physical containment checks. Reject symlinks and paths outside the repository before reading. |
| BH-08 | medium | patch | Mutation tests invoke main with mocked Git; add a real isolated Git checkout and executable CLI baseline, semantic fault and restoration test. |
| BH-09 | low | patch | Exact ignored build and result bytes are needed on later verification. Document archiving and recovery limits at the recorded source candidate. |
| BH-10 | low | reject | A deliberately stalled child would hang, but legitimate verifier output is one bounded line and no stall or pipe exhaustion was reproduced. Timeout/process-management guards for this uncommon condition exceed a direct correction. |
| BH-11 | medium | patch | Acceptance-row fields, requirement-map drift and provenance-current branches lack negative coverage. Add focused tests for their exact semantic blockers. |
| EC-01 | high | patch | A candidate-owned baseline set to HEAD can make the inspected range empty. Preserve the original spec-add baseline and derive the scope start independently from Git. |
| EC-02 | medium | patch | The forwarded identity/owner/hash/order codes are undocumented in V2_CODES; the same root cause as BH-05. |
| EC-03 | high | patch | Matching fault lanes can carry unrelated fixture hashes; the same committed-fixture binding gap as BH-01. |
| EC-04 | high | patch | Rehashed scenario output bindings and summary are not compared with frozen measurements; the same completion remeasurement gap as BH-03. |
| VG-01 | medium | patch | Direct helper tests do not cover the Story 8.2 inserted-record CLI seam. Add baseline success and changed-result rejection through that command. |
| R3-B1 | high | patch | Published `evidence/story-7.4-candidate` on origin at ba1b47622ad8d72e0ea70f01e4e695e49413c1ee with the owner's 2026-10-04 authorization. All six UX conformance facts passed in CI run 37178164948, including the verifier that reads the historical sources. |
| R3-B2 | false | reject | The runner's existing system Python imports jsonschema: PreservedBundleShouldPassZeroGapVerification passed in CI run 37178164948. No dependency change is needed. |
| R3-B3 | low | reject | Shared checkout tests exercise the local real-Git CLI; fresh-origin reachability is separately tracked by the tag/CI task. Adding network access to the unit fixture would add a separate external dependency without resolving that known task. |
| R3-B4 | low | reject | A deliberately rewritten and re-finalized pair can carry arbitrary binding paths. Normal generation and inserted-record verification rederive those paths; changing the generic predecessor threat model would add new validation branches for fabricated committed evidence. |
| R3-B5 | low | reject | A deliberately re-finalized pair can carry inconsistent candidate fields. The current completion verifier rejects this through full candidate rederivation; this repeats Review 2's rejected fabricated-evidence concern for the generic predecessor consumer. |
| R3-B6 | low | reject | A deliberately re-finalized pair can carry unequal restoration hashes. Current generation and inserted verification reject it against measured, committed fixture bytes; expanding generic predecessor verification adds guards for the same rejected fabricated-evidence scenario. |
| R3-B7 | low | patch | Python 3.11 Path.resolve raises RuntimeError on a symlink loop, which escapes file_bytes. Fixed: the existing blocker now normalizes this path error, with a Python 3.11 CLI regression. |
| R3-B8 | low | patch | Recovery requires exact modification times, but the manifest instructions name only paths and hashes. Fixed: the runbook explicitly requires original nanosecond modification times, and the archive manifest preserves them. |
| R3-B9 | medium | patch | Changing fault identity also invalidates its blocker and stops at schema validation. Fixed: independent XML name, parameter, and classname mutations keep valid metadata; deleting the guard now fails all three new tests. |
| R3-B10 | medium | patch | Missing/wrong-stamp assembly cases do not pin the correctly stamped but old assembly branch. Fixed: a correctly stamped assembly older than the candidate produces TEST_RESULTS_STALE in its new regression. |
| R3-E1 | high | patch | Same root cause as R3-B1; the published historical tag and first-CI UX success prove the correction. |
| R3-V1 | medium | patch | Deleting canonical JSON/Markdown equality leaves all 58 verifier and 84 Story 8.2 generator tests green. Fixed: self-consistent decision and acceptance-row fixtures require UX_RENDER_DRIFT; removing canonical equality fails both new tests. |
| R3-V2 | medium | patch | Deleting exact testcase attribution leaves all 84 Story 8.2 generator tests green. Same root cause as R3-B9; fixed and independently proved by the three failing guard-deletion probes. |
| R3-V3 | high | patch | Same root cause as R3-B1; complete-history CI now derives the preserved historical source successfully. |

CI followup (2026-10-04): [first run](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/37178164948). Conformance: 416 tests, 415 passed, one historical fixed-inventory comparison failed, zero skips/not-run. The uploaded TRX proves all six UX facts passed.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| CI-01 | high | patch | The retired rc.2 assertion-inventory comparison is still selected by current CI and rejects all six additive UX facts. Exclude that one historical method under the current-change policy, keeping its source and frozen records for reproduction. Permit only ci.yml in the Story 8.2 scope; positive CI-path and negative release-workflow/product-path regressions exercise the guard. |

The CI followup passed all ten focused scope regressions. Independent blind,
edge-case, and verification-gap reviews found no actionable issues in this
correction. The replacement candidate requires a fresh Release build, all
eleven frozen commands, and the exact current conformance selection.

Completion validation (2026-10-04): the corrected candidate
`28bc212f9831efe577c5f4cd7feb3c63b08663c2` passed 621 Python tests and the
exact current conformance selection (415 passed, zero failures/skips/not-run)
in an isolated checkout with root-declared module repositories. All eleven
frozen acceptance commands passed with `11/11/0/0/0/0`; repeated generation
produced identical pair bytes. The inserted record verified successfully.
The complete 263-file archive, including the bound assembly and ten receipts,
was copied with original nanosecond modification times to
`~/hexalith-evidence/conversations/story-8.2/28bc212f9831efe577c5f4cd7feb3c63b08663c2/`
and verified against its manifest and record. The first GitHub repository job
passed 755 tests; product, Admin Web, Aspire, release-tooling, CodeQL, and
commit-message checks also passed. CI-01 corrects the sole remaining historical
inventory failure. No Story 8.2 review finding is deferred.

## Verification

- Run all eleven contract commands verbatim; first build conformance with the required candidate stamp for its Release acceptance lane.
- Run `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py _bmad/scripts/tests/test_generate_story_record.py`; require no failures or skips.
- Pass completion and `--verify-inserted-record` gates; independently probe every new blocker and confirm protected bytes remain identical.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 8.2 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `8.2`
- Candidate: `28bc212f9831efe577c5f4cd7feb3c63b08663c2`
- JSON content SHA-256 (all three digest fields zeroed): `ce196d71d48e0ddc40d189344b8f7748697370136ac7fa3e286671f089f3edc7`

## Authority

| Field | Value |
| --- | --- |
| Epic | `epic-6-authority-2026-08-03-v10` |
| Architecture | `conversations-architecture-2026-08-03-v10` |
| Planning candidate | `1e9a61126d3b7a55b514b7c7c8942d5af03355e5` |
| Bundle digest | `159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055` |

## Root gitlinks

| Path | Mode | Commit |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `160000` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `160000` | `688eec9a4333245cc0ff7772115c769094471863` |
| `references/Hexalith.Commons` | `160000` | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| `references/Hexalith.EventStore` | `160000` | `2c58ffda41759e895ace4b9625c9bd931a217672` |
| `references/Hexalith.Folders` | `160000` | `92da4b01352448cb06296776097e2da4383d9340` |
| `references/Hexalith.FrontComposer` | `160000` | `bf40099f81fcaeac324b7b4377513ac7d49cead4` |
| `references/Hexalith.Memories` | `160000` | `42995692634af9ba35982ec0e1aedec4f5576e71` |
| `references/Hexalith.Parties` | `160000` | `5388884eec84b16545fdc008b2fc04547b0ed5b6` |
| `references/Hexalith.Projects` | `160000` | `0f03582b3457a6d9212d60e2f9146a6043af5f7e` |
| `references/Hexalith.Tenants` | `160000` | `c0afce2e9704efa6a8c5ebe8d275a8b987141c8b` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-8.2-ENTRY-v1` | `d07cee1556fa039169ca2cfa6cfecbd123909b9da56ea729866c7a0591fd26e6` |

## Predecessors

- `8.1`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-8.2-01` | `0` | `PASS` | `none` | `6` | `artifacts/v9/8.2/AC-8.2-01.trx` | `bc8b69b316b077bb30a001c42b87836d546bbd7285de82a8d0b1960316590b1f` |
| `AC-8.2-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-02.xml` | `6b000dd9dfb306f162b654f255f4546b370b9090123df62b492fb777c187ffc8` |
| `AC-8.2-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-03.xml` | `3e3fb2618303174497bca470fa86f5009ce7243b2fb766ca70adab76e57fc56f` |
| `AC-8.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-04.xml` | `4d6ce78d4663d65d050e2a81b26539b09774500d0a766ead4cb2c1f6c29d95f6` |
| `AC-8.2-05` | `0` | `PASS` | `none` | `3` | `artifacts/v9/8.2/AC-8.2-05.xml` | `791d8eaef001119271d41dd6ad1da1c0cab77a6e75b316e5a13e788c4688a02e` |
| `AC-8.2-06` | `0` | `PASS` | `none` | `2` | `artifacts/v9/8.2/AC-8.2-06.xml` | `52408d79015d25e34fcefb85b44460d5d43bc498b180546c7fd6450496ce57e6` |
| `AC-8.2-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-07.xml` | `4ed795454ee26a64c6b53ff2d93b3e6293d8bea97978255be12fdee57fc7151c` |
| `AC-8.2-08` | `0` | `PASS` | `none` | `3` | `artifacts/v9/8.2/AC-8.2-08.xml` | `b6533b85dc776410cfb1dcc93363c1a16596d30b8c3bc3ab1f17ca641744ec9e` |
| `AC-8.2-09` | `0` | `PASS` | `none` | `3` | `artifacts/v9/8.2/AC-8.2-09.xml` | `f2dfbcdcb2f22c9c4cfc8f011649fde6912650d3ead4b8aaf0108a10163523ed` |
| `AC-8.2-10` | `0` | `PASS` | `none` | `15` | `artifacts/v9/8.2/AC-8.2-10.xml` | `edcb6d55286cf6ccc2b8ea5fd8927123a6b77499627c549d57abf5ded9a15eed` |
| `AC-8.2-11` | `0` | `PASS` | `none` | `15` | none | none |

### `AC-8.2-01`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest -trx artifacts/v9/8.2/AC-8.2-01.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/8.2/AC-8.2-01.trx` | `bc8b69b316b077bb30a001c42b87836d546bbd7285de82a8d0b1960316590b1f` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5a8f9ab50d50b5dc60b6a8bc4b23d4d1cb39f9f7088d7c88a47757ec57848555` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-01#0001` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.PreservedBundleShouldPassZeroGapVerification` | `PASS` |
| `AC-8.2-01#0002` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.AcceptanceCriteriaShouldProjectTheFrozenInventory` | `PASS` |
| `AC-8.2-01#0003` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DecisionsShouldProjectTheFrozenInventory` | `PASS` |
| `AC-8.2-01#0004` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.SourcesShouldBindCanonicalPathsVersionsAndHashes` | `PASS` |
| `AC-8.2-01#0005` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.DispositionsShouldRemainPreservedAndHistorical` | `PASS` |
| `AC-8.2-01#0006` | `Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest.CandidateShouldContainNoProductionUiChange` | `PASS` |

### `AC-8.2-02`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k missing_decision --junitxml=artifacts/v9/8.2/AC-8.2-02.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-02#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_missing_decision` | `PASS` |

### `AC-8.2-03`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k duplicate_decision --junitxml=artifacts/v9/8.2/AC-8.2-03.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-03#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_duplicate_decision` | `PASS` |

### `AC-8.2-04`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k unknown_decision --junitxml=artifacts/v9/8.2/AC-8.2-04.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-04#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_unknown_decision` | `PASS` |

### `AC-8.2-05`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k acceptance_identity_faults --junitxml=artifacts/v9/8.2/AC-8.2-05.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-05#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_acceptance_identity_faults[acceptance-missing]` | `PASS` |
| `AC-8.2-05#0002` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_acceptance_identity_faults[acceptance-duplicate]` | `PASS` |
| `AC-8.2-05#0003` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_acceptance_identity_faults[acceptance-unknown]` | `PASS` |

### `AC-8.2-06`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k ownership_and_hash_faults --junitxml=artifacts/v9/8.2/AC-8.2-06.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-06#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_ownership_and_hash_faults[owner-missing]` | `PASS` |
| `AC-8.2-06#0002` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_ownership_and_hash_faults[hash-missing]` | `PASS` |

### `AC-8.2-07`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k source_drift --junitxml=artifacts/v9/8.2/AC-8.2-07.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-07#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_source_drift` | `PASS` |

### `AC-8.2-08`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k rendering_and_order_drift --junitxml=artifacts/v9/8.2/AC-8.2-08.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-08#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_rendering_and_order_drift[render-changed]` | `PASS` |
| `AC-8.2-08#0002` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_rendering_and_order_drift[json-order]` | `PASS` |
| `AC-8.2-08#0003` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_rendering_and_order_drift[markdown-order]` | `PASS` |

### `AC-8.2-09`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k activation_and_story_binding_faults --junitxml=artifacts/v9/8.2/AC-8.2-09.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-09#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_activation_and_story_binding_faults[row-activated]` | `PASS` |
| `AC-8.2-09#0002` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_activation_and_story_binding_faults[historical-owner]` | `PASS` |
| `AC-8.2-09#0003` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_activation_and_story_binding_faults[nonexistent-owner]` | `PASS` |

### `AC-8.2-10`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py -k fixtures_restore_byte_identically --junitxml=artifacts/v9/8.2/AC-8.2-10.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-10#0001` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[decision-missing]` | `PASS` |
| `AC-8.2-10#0002` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[decision-duplicate]` | `PASS` |
| `AC-8.2-10#0003` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[decision-unknown]` | `PASS` |
| `AC-8.2-10#0004` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[acceptance-missing]` | `PASS` |
| `AC-8.2-10#0005` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[acceptance-duplicate]` | `PASS` |
| `AC-8.2-10#0006` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[acceptance-unknown]` | `PASS` |
| `AC-8.2-10#0007` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[owner-missing]` | `PASS` |
| `AC-8.2-10#0008` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[hash-missing]` | `PASS` |
| `AC-8.2-10#0009` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[source-changed]` | `PASS` |
| `AC-8.2-10#0010` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[render-changed]` | `PASS` |
| `AC-8.2-10#0011` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[json-order]` | `PASS` |
| `AC-8.2-10#0012` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[markdown-order]` | `PASS` |
| `AC-8.2-10#0013` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[row-activated]` | `PASS` |
| `AC-8.2-10#0014` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[historical-owner]` | `PASS` |
| `AC-8.2-10#0015` | `_bmad.scripts.tests.test_generate_ux_preservation_disposition::test_fixtures_restore_byte_identically[nonexistent-owner]` | `PASS` |

### `AC-8.2-11`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/8.2.json --format bundle --output-json docs/release-evidence/story-8.2-final-record-v2.json --output-markdown docs/release-evidence/story-8.2-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-8.2-11#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-8.2-11#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-8.2-11#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-8.2-11#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-8.2-11#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-8.2-11#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-8.2-11#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-8.2-11#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-8.2-11#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-8.2-11#0010` | `generator::ordered-unique-52-decisions-and-28-acceptance-ids` | `PASS` |
| `AC-8.2-11#0011` | `generator::story-8.1-pair-candidate-and-disposition-compatible` | `PASS` |
| `AC-8.2-11#0012` | `generator::source-output-and-inventory-digests-bound` | `PASS` |
| `AC-8.2-11#0013` | `generator::all-thirteen-fault-categories-measured` | `PASS` |
| `AC-8.2-11#0014` | `generator::all-fifteen-fixtures-restored-with-pass-and-matching-hashes` | `PASS` |
| `AC-8.2-11#0015` | `generator::candidate-stamped-nonempty-results` | `PASS` |

## Story 8.2 zero-gap UX validation

- Candidate: `28bc212f9831efe577c5f4cd7feb3c63b08663c2`
- Story 8.1 candidate: `7f91d33ea366edca7aca8fa0a31b386409126799`
- Story 8.1 record SHA-256: `ba23b3b94d75d030532c538beb0807d82d5f45b9fc6635478cda6399f095f18a`
- Inventory SHA-256: `ea18b1f65c4077c1f91a7f8bd65e4b17def20f26336d53c1325c5ee574c29196`
- Preserved decisions: 52; acceptance criteria: 28; no activation authorized.

| Binding | Path | SHA-256 |
| --- | --- | --- |
| Source | `_bmad-output/planning-artifacts/ux-design-specification.md` | `948a5ac40a05fce510bffdd6818e3fcf3c871874b8779468954de57e452d8f18` |
| Source | `_bmad-output/planning-artifacts/ux-requirement-map.md` | `5965394e662a3b708896f5df85d2b981798bc590ea68feb3a974f66300c2751f` |
| `schema` | `docs/release-evidence/ux-preservation-disposition-v1.schema.json` | `d189f4dd1b1e701f683a4ff2ce7be54a1dba57290162555b9dd141bb832fe5dc` |
| `json` | `docs/release-evidence/ux-preservation-disposition-v1.json` | `e359a97e19d3f021792006a4c14ba3d1ff42249c05e5c76fba0e43302aa4461f` |
| `markdown` | `docs/release-evidence/ux-preservation-disposition-v1.md` | `330aff37565e46b3bbe5435c0c6c9ecabc814e54220f2102b7dc234de5abcbc1` |

## Fault injection

| Fault | Expected blocker | Observed exit | Observed blockers | Before SHA-256 | After SHA-256 |
| --- | --- | --- | --- | --- | --- |
| `decision-missing` | `UX_DECISION_MISSING` | `1` | `UX_DECISION_MISSING` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `decision-duplicate` | `UX_DECISION_DUPLICATE` | `1` | `UX_DECISION_DUPLICATE` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `decision-unknown` | `UX_DECISION_UNKNOWN` | `1` | `UX_DECISION_UNKNOWN` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `acceptance-missing` | `UX_ACCEPTANCE_MISSING` | `1` | `UX_ACCEPTANCE_MISSING` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `acceptance-duplicate` | `UX_ACCEPTANCE_DUPLICATE` | `1` | `UX_ACCEPTANCE_DUPLICATE` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `acceptance-unknown` | `UX_ACCEPTANCE_UNKNOWN` | `1` | `UX_ACCEPTANCE_UNKNOWN` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `owner-missing` | `UX_OWNER_MISSING` | `1` | `UX_OWNER_MISSING` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `hash-missing` | `UX_HASH_MISSING` | `1` | `UX_HASH_MISSING` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `source-changed` | `UX_SOURCE_DRIFT` | `1` | `UX_SOURCE_DRIFT` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `render-changed` | `UX_RENDER_DRIFT` | `1` | `UX_RENDER_DRIFT` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `json-order` | `UX_ORDER_DRIFT` | `1` | `UX_ORDER_DRIFT` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `markdown-order` | `UX_ORDER_DRIFT` | `1` | `UX_ORDER_DRIFT` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `row-activated` | `UX_ACTIVATION_UNAUTHORIZED` | `1` | `UX_ACTIVATION_UNAUTHORIZED` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `historical-owner` | `UX_CURRENT_STORY_INVALID` | `1` | `UX_CURRENT_STORY_INVALID` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |
| `nonexistent-owner` | `UX_CURRENT_STORY_INVALID` | `1` | `UX_CURRENT_STORY_INVALID` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` | `2d9959916ef0aed28ed4f651f7e57195cf65305f265e11dfbf9cc3dd047229b2` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-8.2-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-8.2-final-record-v2.md` |

## Rollback boundary

remove only Story 8.2 validator/fault fixtures/results and final record; retain accepted Story 8.1 outputs and all source UX bytes.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `11` | `11` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
