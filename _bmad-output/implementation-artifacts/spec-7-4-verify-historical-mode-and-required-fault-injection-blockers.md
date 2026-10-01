---
title: 'Verify historical mode and required fault-injection blockers'
type: 'feature'
created: '2026-10-01'
status: 'done'
baseline_commit: 'fd0d4ed85734fba9aca7b246b2f52b4774dd776c'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The v2 generator rejects Story 7.4's historical command and binds no observed fault or restoration evidence.

**Approach:** Add contract-driven historical verification, execute the frozen 13 faults in isolated repositories, and bind their measured results into a deterministic Story 7.4 record.

## Boundaries & Constraints

**Always:** Run the six commands in `_bmad-output/planning-artifacts/v9/story-contracts/7.4.json` unchanged through `uv run --frozen --no-sync`. Preserve its inventory/digest and exit 0/PASS, 1/FAIL, 2/BLOCKED. Verify predecessor pairs 7.1–7.3 and their chain. Follow the current-change runbook and Story 7.3 completion gate, including scoped candidate, record-only, and lifecycle commits after review; no push is included.

Verify committed bytes, trees, modes, gitlinks, and commit-bound evidence. Compare closed records against closure commits. Story 6.1 has no recorded candidate. Original TRX/binaries and raw promotion results were uncommitted; label archived declarations recorded-only. State that former uncommitted state is not reconstructed. Preserve the pre-generator disposition.

**Never:** Change the contract, closed/predecessor records, product, dependencies, gitlinks, submodules, or workflows. Traverse submodules, reconstruct transient evidence, claim CI enforcement, or require V23–V29 current authority gates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Blocker |
| --- | --- | --- | --- |
| Historical | Closed 6.1/6.2/6.7 records | Acceptance-result v1, nonempty ledger, explicit limits | `HISTORICAL_BLOB_UNRESOLVED`, `HISTORICAL_RECORD_DRIFT` |
| Faults | Every frozen mutation executes against a passing fixture | Observed required blocker and equal before/after hashes | `FAULT_NOT_DETECTED`, `FIXTURE_NOT_RESTORED` |
| Required lane | Missing, skipped, not run, or empty | FAIL; never not-applicable | Existing test/ledger blocker |

</frozen-after-approval>

## Code Map

- Generator: preserve `verify_historical`; reuse committed tree/blob readers, `v2_scenario_from_acceptance`, `v2_verified_predecessor`, and retention/insertion helpers.
- Tests: reuse `CLOSED_RECORDS`, v2 fixture builders, `v2_snapshot`, 7.2 restoration helpers, and 7.3 workflow faults.
- Historical closure anchors: 6.1 `16e3d3db4530719aa06129ba06b34bd78f7995eb`; 6.2 `e480c3f3176cdc3d911baf91eb3e7a8cd38874aa`; 6.7 `29def441408becfbbbdc5c59b9af14a7717cb21f`. Current bytes match; recorded revisions resolve.
- Required fault blockers: `COUNT` → `TEST_COUNT_INCONSISTENT`; `SUBMODULE_PATH` → `SUBMODULE_INTERNAL_PATH`; `CANDIDATE` → `CANDIDATE_NOT_FINAL`; `GITLINK` → `GITLINK_SCOPE_MISMATCH`; `RESULT_MISSING/STALE` → `TEST_RESULTS_MISSING/STALE`; `RESULT_FAILED/SKIPPED/NOT_RUN` → `TEST_FAILED/SKIPPED/NOT_RUN`; `LEDGER_EMPTY` → `ASSERTION_LEDGER_EMPTY`; `WORKFLOW_REMOVED/DISPLACED` → `WORKFLOW_INTEGRATION_MISSING/DISPLACED`; `MARKDOWN_DIGEST` → `RECORD_CONTENT_DRIFT`.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad/scripts/fixtures/story-7.4-history-v1.json` — pin closure references; derive blob/digest bindings from Git; enumerate evidence and limits.
- [x] `_bmad/schemas/story-final-record-v2.schema.json` — add closed 7.4-only history/predecessor and observed fault/restoration fields; preserve older validation.
- [x] `_bmad/scripts/generate_story_record.py` — implement historical CLI/result reader, bound-fact checks, fault metadata, predecessor chain, retention/insertion, and rendering.
- [x] `_bmad/scripts/tests/test_generate_story_record.py` — add exact AC-02…05 selectors; emit measured fault metadata in JUnit properties. Cover incomplete/duplicate/unknown faults, wrong blockers, restoration drift, historical missing/drifted objects, deterministic output, retention/insertion, and unchanged 7.1–7.3 pairs.
- [x] `docs/runbooks/story-final-record-generation.md` — document commands, verification limits, metadata, new blockers, and completion procedure.
- [x] `docs/release-evidence/story-7.4-final-record-v2.{json,md}`, this spec, and `sprint-status.yaml` — implement and verify committed-candidate retention and insertion; completion execution is captured in the generated record region.

**Acceptance Criteria:**

- Given historical anchors, when AC-01/02 execute, then bindings verify read-only and former state is explicitly unclaimed.
- Given the frozen faults, when AC-03/04 execute, then every required blocker is observed and every fixture restores byte-identically.
- Given a vacuous required lane, when AC-05 executes, then the generator fails with the applicable existing blocker.
- Given five current passing results, when AC-06 executes twice, then identical bytes bind the contract, inventory, 7.1–7.3 digests, historical results, fault ledger, and restoration hashes with the required summary.

## Implementation Notes

- Historical verification reads the pinned closure commits, recorded root revisions,
  root trees and gitlinks, and 160 bound ordinary root blobs. Story 6.1 binds its
  closure bytes where no candidate was recorded; its baseline supplies only paths
  absent at closure. Archived test and promotion declarations remain recorded-only.
  The fixture also binds 67 committed Story 6.2 evidence identities.
- Story 7.4 alone adds closed historical/predecessor and observed fault fields.
  Historical acceptance inputs and ledgers must equal the remeasured facts; every
  fault lane must bind all 13 exact blockers and equal restoration hashes. Stable
  rereads must match the scenario's measured artifact digest.
- Story 7.4 uses the existing record-only candidate retention and designated-spec
  insertion procedure. Story 7.1–7.3 pairs remain byte-identical. The final committed
  candidate, six contract commands, record-only commit, insertion verification, and
  lifecycle commit form the completion gate recorded below; no push is included.

- Missing historical commit/tree/blob objects use Git's explicit absent-object
  probe result to report `HISTORICAL_BLOB_UNRESOLVED`/exit 1. Git execution,
  timeout, permission, and unexpected probe failures preserve `BLOCKED`/exit 2.

## Spec Change Log

## Review Triage Log

| ID | Finding | Verdict | Evidence and route |
| --- | --- | --- | --- |
| B1 | Five gitlink updates appear in the review diff. | false | The initial workspace already contained these updates; external commit `a4f9cb6a30fa4da6927a4d0813f92d64ca74ad92` committed them and the ready spec. This build has made no commits or gitlink changes. Reject; preserve the external commit. |
| B2 | Historical output follows a parent symlink into a submodule. | high | `v2_output_target` checks repository containment but permits a physically resolved root-gitlink descendant; the reviewer reproduced PASS and a submodule write. Patch the historical destination check before writing. |
| B3 | Installed closed-record reads cross a submodule parent symlink. | high | `contained_file` permits any resolved path inside the umbrella repository, and the new historical caller does not exclude resolved root-gitlink descendants. The reviewer reproduced PASS after relocating closed records beneath a gitlink. Patch the historical read boundary before the legacy verifier runs. |
| B4 | Git execution failure preserves a previous PASS historical receipt. | medium | `v2_historical` catches only `V2Stop`; `GateError` escapes to generator-failure stdout without replacing the declared receipt. The reviewer reproduced a timeout after PASS. Patch the established historical receipt path to emit acceptance-result v1 BLOCKED on execution failure. |
| B5 | `.gitmodules` probe permission failures become missing-content FAIL. | medium | `root_submodule_paths` treats every probe exit 128 as absence; the historical caller then misclassifies the existing blob. Preserve Git/environment errors as BLOCKED with a direct correction to the probe handling. |
| B6 | Closed-record permission failure becomes proven content drift. | medium | The new historical read catches every `OSError` and replaces installed bytes with `None`, producing FAIL/1 even when permission denied prevents inspection. Patch the distinction between absent/drifted bytes and environmental read errors. |
| B7 | Schema accepts duplicate required fault IDs. | medium | Parent isolated validation accepted thirteen distinct objects repeating COUNT and omitting MARKDOWN_DIGEST. Runtime rejects them, but standalone record schema consumers accept the incomplete matrix. Patch the 7.4-only structural ID coverage. |
| B8 | Schema accepts duplicate historical story IDs. | medium | Parent isolated validation accepted a second 6.1 record in place of 6.2. Patch the three pinned identities and their existing order in the new branch. |
| B9 | Schema accepts duplicate predecessor IDs. | medium | Parent isolated validation accepted 7.1 in place of 7.3. Patch the 7.4 predecessor branch to require the existing 7.1–7.3 identities in order. |
| B10 | Schema accepts arbitrary replacement historical limits. | medium | Parent isolated validation accepted five unrelated strings, removing all required limitations. Patch the new branch to require the five defined statements. |
| E1 | Closed bytes change before the legacy verifier reopens them. | high | Parent reproduced PASS by appending an unparsed paragraph at `verify_historical` entry after the initial equality check. Patch a stable equality check after verification, preserving read-error classification. |
| E2 | A changed historical contract can direct output over a closed record. | high | Parent isolated reproduction changed AC-01 output to the 6.1 path; the command returned PASS and overwrote it. Patch the historical caller to require the exact frozen output path before any write. |
| V1 | Duplicate-fault test misses the distinct-ID guard. | medium | Pre-verified review demonstrated that `[duplicate]` fails the earlier testcase/metadata check and a removed set guard remains undetected. Patch the duplicate fixture to retain matching suffixes, distinct testcase identities, and distinct metadata objects. |
| V2 | Markdown historical limits lack independent verification. | medium | Pre-verified review removed all rendered limitation lines and the deterministic retention/insertion test still passed. Patch independent assertions for each required generated and inserted limitation. |

All surviving findings were corrected with direct patches: the demonstrated states
require no new public surface or intent change. No findings are deferred. Parent reproduction
evidence is archived at `/tmp/bmad-7-4-verification-7axu3wkx/triage-evidence.json`.

## Verification

- Full post-review suite: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`
  — exit 0; **415 passed** in 280.56 seconds, with no failures or skips. Log:
  `/tmp/bmad-7-4-verification-7axu3wkx/full-post-review-pytest.log`.

- Review patch regressions: 28 passed, 387 deselected; subsequent fixture-message
  checks: 11 passed, 404 deselected. Logs: `/tmp/story-7-4-review-fixes-pytest.log`
  and `/tmp/story-7-4-fixture-message-pytest.log`. All nine new fixture messages
  passed pinned commitlint; evidence: `/tmp/story-7-4-fixture-commitlint.log`.
- Post-review Debug build:
  `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Debug -m:1 /nr:false`
  — exit 0, zero warnings/errors. The resulting Debug executable's focused
  `StoryFinalRecordGenerationValidationTest` class passed all 10 tests with
  zero errors, failures, skips, or not-run tests. Logs are
  `/tmp/bmad-7-4-verification-7axu3wkx/debug-build-post-review.log` and
  `/tmp/bmad-7-4-verification-7axu3wkx/csharp-focused-post-review.log`.
- Workflow verification: the frozen Story 7.3 AC-01/02 arguments were passed to
  the verifier in process, redirecting only its output writer to the audit
  directory. All 33 and 22 assertions passed; original archived outputs remained
  byte-identical. The CLI rejects arbitrary temporary output paths, so the writer
  redirection preserved both the exact contract arguments and archived evidence.
  Script and receipts: `/tmp/bmad-7-4-verification-7axu3wkx/check-workflows.py` and
  `workflow-post-review-AC-7.3-{01,02}.json` in the same directory.
- Root-submodule checker and whitespace check: exit 0 after review patches.

- Focused missing-object patch checks: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k 'missing_tree_or_blob or historical_git_environment_failures or historical_missing_or_drifted_objects or missing_recorded_baseline or historical_mode_states_worktree_limit or deterministic_record_retention_and_insertion'`
  — exit 0; 16 passed, 390 deselected; log:
  `/tmp/story-7-4-missing-object-pytest.log`.

- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`
  — earlier implementation run: exit 0; 397 passed, no failed/skipped/not-run tests; log:
  `/tmp/story-7-4-final-pytest.log`.
- Focused historical and deterministic retention/insertion checks passed; the full
  suite includes all frozen faults and restoration checks, incomplete/duplicate/
  unknown/wrong-blocker metadata, vacuous required lanes, historical missing/drifted
  facts, predecessor-chain drift, closed schemas, and stable artifact reread races.

- Full generator pytest suite and the six contract commands; no failed/skipped/not-run required lane.
- Focused C# `StoryFinalRecordGenerationValidationTest` after a Debug test-project build; workflow verifier with temporary outputs preserving archived 7.3 evidence.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`.
- Planning baseline: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k historical` — exit 0, nine passed.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 7.4 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate, measured JUnit results, and acceptance results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `7.4`
- Candidate: `70aa0729704389ad263b5a4960e3b8113db5b9af`
- JSON content SHA-256 (all three digest fields zeroed): `d06b0a2e0345685bfb35982e1e0f0e7fedd8d384c0bcaecb5c578546e0426d3d`

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
| `references/Hexalith.Builds` | `160000` | `611f7e882ec615bad534de73149c7e95431b4cd4` |
| `references/Hexalith.Commons` | `160000` | `c13dc6679aa91144b6d541078f3f20019d79c2eb` |
| `references/Hexalith.EventStore` | `160000` | `8096455e4f23f2912998e36738058b8e3d961be6` |
| `references/Hexalith.Folders` | `160000` | `c292ead0c730e7b8389e25a3d70d746f14fbe8d0` |
| `references/Hexalith.FrontComposer` | `160000` | `0e8fc836ec2292589ad800df67630a9b36bcbb5c` |
| `references/Hexalith.Memories` | `160000` | `ece4edc4c9a37a62b34d3b7c8aa901fc363c038c` |
| `references/Hexalith.Parties` | `160000` | `937cb2a343aaa74963db9bb867a2c3a01ff48677` |
| `references/Hexalith.Projects` | `160000` | `9506193e11d85ef65580207da48e5671624d9e2d` |
| `references/Hexalith.Tenants` | `160000` | `3ce15d103227fb7767820fc89c51b75b86401ff3` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-7.4-ENTRY-v1` | `4d6bb01942d41d315ad4f3b08070a3cfe8ddcc2cae1b8981fee9b64d55ebe911` |

## Predecessors

- `7.3`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-7.4-01` | `0` | `PASS` | `none` | `20` | `artifacts/v9/7.4/AC-7.4-01.json` | `c80faf674c4680cff9b90edf27ea058862aa41802f8ea7cb991e7164ca090dd9` |
| `AC-7.4-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/7.4/AC-7.4-02.xml` | `1d8eb2d9e6aff9fc7b7e167e4043d6524bd6b3bdb16547b77e336d35347fc063` |
| `AC-7.4-03` | `0` | `PASS` | `none` | `13` | `artifacts/v9/7.4/AC-7.4-03.xml` | `1bb7bdc1dd9e76cf32a3602793875c70cde54879898777f96899b53beb9d6fbc` |
| `AC-7.4-04` | `0` | `PASS` | `none` | `13` | `artifacts/v9/7.4/AC-7.4-04.xml` | `807ca4aff3b964de9342fa77e412d6d922d0cfc9da66176d9607fd1da45d52e3` |
| `AC-7.4-05` | `0` | `PASS` | `none` | `4` | `artifacts/v9/7.4/AC-7.4-05.xml` | `2690c39eb37a43cb6b043e29037c0fbb2609bd2cd0891304eda42d6de18f6811` |
| `AC-7.4-06` | `0` | `PASS` | `none` | `13` | none | none |

### `AC-7.4-01`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.4.json --historical --format json --output-json artifacts/v9/7.4/AC-7.4-01.json`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.4-01#0001` | `history::A former uncommitted working tree is not reconstructed and is not claimed.` | `PASS` |
| `AC-7.4-01#0002` | `history::Original TRX, test binaries, and raw promotion results were uncommitted; archived declarations are recorded-only.` | `PASS` |
| `AC-7.4-01#0003` | `history::Story 6.1 has no recorded candidate; its closure commit is not a reconstructed candidate.` | `PASS` |
| `AC-7.4-01#0004` | `history::Pre-generator findings retain their approved warning disposition.` | `PASS` |
| `AC-7.4-01#0005` | `history::Only root Git objects are read; submodule contents, former runtime state, and CI enforcement are not verified.` | `PASS` |
| `AC-7.4-01#0006` | `history::6.1::closure-record-bytes-modes-and-digest` | `PASS` |
| `AC-7.4-01#0007` | `history::6.1::root-commits-trees-and-gitlinks` | `PASS` |
| `AC-7.4-01#0008` | `history::6.1::bound-blobs-and-commit-bound-evidence` | `PASS` |
| `AC-7.4-01#0009` | `history::6.1::archived-declarations-recorded-only` | `PASS` |
| `AC-7.4-01#0010` | `history::6.1::disposition::pre-generator` | `PASS` |
| `AC-7.4-01#0011` | `history::6.2::closure-record-bytes-modes-and-digest` | `PASS` |
| `AC-7.4-01#0012` | `history::6.2::root-commits-trees-and-gitlinks` | `PASS` |
| `AC-7.4-01#0013` | `history::6.2::bound-blobs-and-commit-bound-evidence` | `PASS` |
| `AC-7.4-01#0014` | `history::6.2::archived-declarations-recorded-only` | `PASS` |
| `AC-7.4-01#0015` | `history::6.2::disposition::generated` | `PASS` |
| `AC-7.4-01#0016` | `history::6.7::closure-record-bytes-modes-and-digest` | `PASS` |
| `AC-7.4-01#0017` | `history::6.7::root-commits-trees-and-gitlinks` | `PASS` |
| `AC-7.4-01#0018` | `history::6.7::bound-blobs-and-commit-bound-evidence` | `PASS` |
| `AC-7.4-01#0019` | `history::6.7::archived-declarations-recorded-only` | `PASS` |
| `AC-7.4-01#0020` | `history::6.7::disposition::pre-generator` | `PASS` |

### `AC-7.4-02`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_historical_mode_states_worktree_limit --junitxml=artifacts/v9/7.4/AC-7.4-02.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.4-02#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_historical_mode_states_worktree_limit` | `PASS` |

### `AC-7.4-03`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_complete_fault_matrix --junitxml=artifacts/v9/7.4/AC-7.4-03.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.4-03#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[COUNT]` | `PASS` |
| `AC-7.4-03#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[SUBMODULE_PATH]` | `PASS` |
| `AC-7.4-03#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[CANDIDATE]` | `PASS` |
| `AC-7.4-03#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[GITLINK]` | `PASS` |
| `AC-7.4-03#0005` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[RESULT_MISSING]` | `PASS` |
| `AC-7.4-03#0006` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[RESULT_STALE]` | `PASS` |
| `AC-7.4-03#0007` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[RESULT_FAILED]` | `PASS` |
| `AC-7.4-03#0008` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[RESULT_SKIPPED]` | `PASS` |
| `AC-7.4-03#0009` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[RESULT_NOT_RUN]` | `PASS` |
| `AC-7.4-03#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[LEDGER_EMPTY]` | `PASS` |
| `AC-7.4-03#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[WORKFLOW_REMOVED]` | `PASS` |
| `AC-7.4-03#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[WORKFLOW_DISPLACED]` | `PASS` |
| `AC-7.4-03#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_complete_fault_matrix[MARKDOWN_DIGEST]` | `PASS` |

### `AC-7.4-04`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_fault_fixtures_restore_byte_identically --junitxml=artifacts/v9/7.4/AC-7.4-04.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.4-04#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[COUNT]` | `PASS` |
| `AC-7.4-04#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[SUBMODULE_PATH]` | `PASS` |
| `AC-7.4-04#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[CANDIDATE]` | `PASS` |
| `AC-7.4-04#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[GITLINK]` | `PASS` |
| `AC-7.4-04#0005` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[RESULT_MISSING]` | `PASS` |
| `AC-7.4-04#0006` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[RESULT_STALE]` | `PASS` |
| `AC-7.4-04#0007` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[RESULT_FAILED]` | `PASS` |
| `AC-7.4-04#0008` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[RESULT_SKIPPED]` | `PASS` |
| `AC-7.4-04#0009` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[RESULT_NOT_RUN]` | `PASS` |
| `AC-7.4-04#0010` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[LEDGER_EMPTY]` | `PASS` |
| `AC-7.4-04#0011` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[WORKFLOW_REMOVED]` | `PASS` |
| `AC-7.4-04#0012` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[WORKFLOW_DISPLACED]` | `PASS` |
| `AC-7.4-04#0013` | `_bmad.scripts.tests.test_generate_story_record::test_v2_fault_fixtures_restore_byte_identically[MARKDOWN_DIGEST]` | `PASS` |

### `AC-7.4-05`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_required_lane_cannot_pass_vacuously --junitxml=artifacts/v9/7.4/AC-7.4-05.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.4-05#0001` | `_bmad.scripts.tests.test_generate_story_record::test_v2_required_lane_cannot_pass_vacuously[missing-TEST_RESULTS_MISSING]` | `PASS` |
| `AC-7.4-05#0002` | `_bmad.scripts.tests.test_generate_story_record::test_v2_required_lane_cannot_pass_vacuously[skipped-TEST_SKIPPED]` | `PASS` |
| `AC-7.4-05#0003` | `_bmad.scripts.tests.test_generate_story_record::test_v2_required_lane_cannot_pass_vacuously[not-run-TEST_NOT_RUN]` | `PASS` |
| `AC-7.4-05#0004` | `_bmad.scripts.tests.test_generate_story_record::test_v2_required_lane_cannot_pass_vacuously[empty-ASSERTION_LEDGER_EMPTY]` | `PASS` |

### `AC-7.4-06`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.4.json --format bundle --output-json docs/release-evidence/story-7.4-final-record-v2.json --output-markdown docs/release-evidence/story-7.4-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-7.4-06#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-7.4-06#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-7.4-06#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-7.4-06#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-7.4-06#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-7.4-06#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-7.4-06#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-7.4-06#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-7.4-06#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-7.4-06#0010` | `generator::historical-closure-facts-equal-acceptance-result` | `PASS` |
| `AC-7.4-06#0011` | `generator::predecessor-records-7.1-7.3-and-chain-verified` | `PASS` |
| `AC-7.4-06#0012` | `generator::all-thirteen-required-fault-blockers-observed` | `PASS` |
| `AC-7.4-06#0013` | `generator::all-thirteen-fixtures-restored-byte-identically` | `PASS` |

## Story 7.4 historical verification

- Contract SHA-256: `2a8f4ee3bb7e5f03e9e7846cc9f28d4dfeae441bf5274c67e9daf4eaf7c592ed`
- Historical fixture: `_bmad/scripts/fixtures/story-7.4-history-v1.json`
- Historical fixture SHA-256: `602a343c7d067b73f8ef3c24c536831e1b5059781bff5591158e176b3f485511`

| Predecessor | Record | SHA-256 |
| --- | --- | --- |
| `7.1` | `docs/release-evidence/story-7.1-final-record-v2.json` | `0a4ede3074fca55853f2fd59f065677cacea3eb700491cbc87594e117557e2f9` |
| `7.2` | `docs/release-evidence/story-7.2-final-record-v2.json` | `063a71b65e69f73f34c69e50dc516794fab5dc5377b0d20f34c85ff274a3bd5f` |
| `7.3` | `docs/release-evidence/story-7.3-final-record-v2.json` | `3825d891725e12235a54bf11f90af3811607869a9f0dd0331423b12646b07eaa` |

| Closed story | Classification | Closure | Recorded candidate | Bound blobs |
| --- | --- | --- | --- | --- |
| `6.1` | `pre-generator` | `16e3d3db4530719aa06129ba06b34bd78f7995eb` | none recorded | `9` |
| `6.2` | `generated` | `e480c3f3176cdc3d911baf91eb3e7a8cd38874aa` | `2971ab79efcf3ef11d4fba7b9139d7cae457a3f9` | `114` |
| `6.7` | `pre-generator` | `29def441408becfbbbdc5c59b9af14a7717cb21f` | `aa2b6b7d05d277e1c083252462b9c8244914970e` | `37` |

- A former uncommitted working tree is not reconstructed and is not claimed.
- Original TRX, test binaries, and raw promotion results were uncommitted; archived declarations are recorded-only.
- Story 6.1 has no recorded candidate; its closure commit is not a reconstructed candidate.
- Pre-generator findings retain their approved warning disposition.
- Only root Git objects are read; submodule contents, former runtime state, and CI enforcement are not verified.

## Fault injection

| Fault | Expected blocker | Observed exit | Observed blockers | Before SHA-256 | After SHA-256 |
| --- | --- | --- | --- | --- | --- |
| `COUNT` | `TEST_COUNT_INCONSISTENT` | `1` | `TEST_COUNT_INCONSISTENT` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `SUBMODULE_PATH` | `SUBMODULE_INTERNAL_PATH` | `1` | `SUBMODULE_INTERNAL_PATH` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `CANDIDATE` | `CANDIDATE_NOT_FINAL` | `1` | `CANDIDATE_NOT_FINAL` | `62bc95b2a18fb361318fe0417a0ea47fae898947c7c570878b21a3ba694c818f` | `62bc95b2a18fb361318fe0417a0ea47fae898947c7c570878b21a3ba694c818f` |
| `GITLINK` | `GITLINK_SCOPE_MISMATCH` | `1` | `GITLINK_SCOPE_MISMATCH` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `RESULT_MISSING` | `TEST_RESULTS_MISSING` | `1` | `TEST_RESULTS_MISSING` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `RESULT_STALE` | `TEST_RESULTS_STALE` | `1` | `TEST_RESULTS_STALE` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `RESULT_FAILED` | `TEST_FAILED` | `1` | `TEST_FAILED` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `RESULT_SKIPPED` | `TEST_SKIPPED` | `1` | `TEST_SKIPPED` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `RESULT_NOT_RUN` | `TEST_NOT_RUN` | `1` | `TEST_NOT_RUN` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `LEDGER_EMPTY` | `ASSERTION_LEDGER_EMPTY` | `1` | `ASSERTION_LEDGER_EMPTY` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` | `f973cd21b7983fe33d4167118ef55156f776a99673fd9988474e816c9c25c3e6` |
| `WORKFLOW_REMOVED` | `WORKFLOW_INTEGRATION_MISSING` | `1` | `WORKFLOW_INTEGRATION_MISSING` | `39980f2aff1d017530dedf9152d3fd9981652d19bcbb538fd8a4e8361b22f7c9` | `39980f2aff1d017530dedf9152d3fd9981652d19bcbb538fd8a4e8361b22f7c9` |
| `WORKFLOW_DISPLACED` | `WORKFLOW_INTEGRATION_DISPLACED` | `1` | `WORKFLOW_INTEGRATION_DISPLACED` | `39980f2aff1d017530dedf9152d3fd9981652d19bcbb538fd8a4e8361b22f7c9` | `39980f2aff1d017530dedf9152d3fd9981652d19bcbb538fd8a4e8361b22f7c9` |
| `MARKDOWN_DIGEST` | `RECORD_CONTENT_DRIFT` | `1` | `RECORD_CONTENT_DRIFT` | `1075a16ed9956cf0a293e925048087404876c42bcfa259e29ddc79e31733929c` | `1075a16ed9956cf0a293e925048087404876c42bcfa259e29ddc79e31733929c` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-7.4-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-7.4-final-record-v2.md` |

## Rollback boundary

remove only Story 7.4 historical/fault fixtures, results, and records; retain Stories 7.1-7.3 and never mutate a closed record.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `6` | `6` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
