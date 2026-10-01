---
title: 'Verify historical mode and required fault-injection blockers'
type: 'feature'
created: '2026-10-01'
status: 'in-progress'
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
