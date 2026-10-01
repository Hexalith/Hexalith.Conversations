---
title: 'Verify historical mode and required fault-injection blockers'
type: 'feature'
created: '2026-10-01'
status: 'ready-for-dev'
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

- [ ] `_bmad/scripts/fixtures/story-7.4-history-v1.json` — pin closure references; derive blob/digest bindings from Git; enumerate evidence and limits.
- [ ] `_bmad/schemas/story-final-record-v2.schema.json` — add closed 7.4-only history/predecessor and observed fault/restoration fields; preserve older validation.
- [ ] `_bmad/scripts/generate_story_record.py` — implement historical CLI/result reader, bound-fact checks, fault metadata, predecessor chain, retention/insertion, and rendering.
- [ ] `_bmad/scripts/tests/test_generate_story_record.py` — add exact AC-02…05 selectors; emit measured fault metadata in JUnit properties. Cover incomplete/duplicate/unknown faults, wrong blockers, restoration drift, historical missing/drifted objects, deterministic output, retention/insertion, and unchanged 7.1–7.3 pairs.
- [ ] `docs/runbooks/story-final-record-generation.md` — document commands, verification limits, metadata, new blockers, and completion procedure.
- [ ] `docs/release-evidence/story-7.4-final-record-v2.{json,md}`, this spec, and `sprint-status.yaml` — complete the committed-candidate gate.

**Acceptance Criteria:**

- Given historical anchors, when AC-01/02 execute, then bindings verify read-only and former state is explicitly unclaimed.
- Given the frozen faults, when AC-03/04 execute, then every required blocker is observed and every fixture restores byte-identically.
- Given a vacuous required lane, when AC-05 executes, then the generator fails with the applicable existing blocker.
- Given five current passing results, when AC-06 executes twice, then identical bytes bind the contract, inventory, 7.1–7.3 digests, historical results, fault ledger, and restoration hashes with the required summary.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Full generator pytest suite and the six contract commands; no failed/skipped/not-run required lane.
- Focused C# `StoryFinalRecordGenerationValidationTest` after a Debug test-project build; workflow verifier with temporary outputs preserving archived 7.3 evidence.
- `python3 scripts/check-root-submodules.py --repository .` and `git diff --check`.
- Planning baseline: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k historical` — exit 0, nine passed.
