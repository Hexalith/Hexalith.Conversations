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
- `_bmad/scripts/generate_story_record.py`: pytest/xUnit routes exist; reuse Story 7.4's observed JUnit properties. Its closed schema reserves UX binding for 8.1 and observed faults for 7.4.
- `docs/release-evidence/story-8.1-final-record-v2.json`: immutable predecessor; completed 8.1 spec supplies continuity.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad/scripts/generate_ux_preservation_disposition.py` — verify existing bundles read-only against canonical derivation. Classify semantic failures before schema errors; enforce deterministic Markdown and order parity.
- [x] `_bmad/scripts/tests/test_generate_ux_preservation_disposition.py` — implement exact selectors for all 13 fault categories, including historical/nonexistent ownership and JSON/Markdown ordering. Measure baseline PASS, exit 1 with the exact blocker, `finally` restoration, equal hashes, and restored PASS. Export JUnit properties; AC-10 repeats the matrix. Resolve selector overlap and test the verification CLI.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/UxPreservationDispositionValidationTest.cs` — prove rendering and zero-gap parity through the verifier; retain Story 8.1 checks.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` — add an 8.2-only closed UX binding and observed ledger. Verify predecessor pair/candidate compatibility, committed inputs/inventories, exact faults, measured exit/blockers, restoration parity, and stamped nonempty results. Reject missing/stale/failed/empty evidence and altered bindings; preserve other stories.
- [x] `docs/runbooks/story-final-record-generation.md` — document verification, Story 8.2 blockers, result properties, and candidate/build prerequisites.
- [ ] `docs/release-evidence/story-8.2-final-record-v2.json`, `docs/release-evidence/story-8.2-final-record-v2.md`, this spec, `_bmad-output/implementation-artifacts/sprint-status.yaml` — run acceptance commands; generate, insert, and verify the record before done.

**Acceptance Criteria:**

- Given Story 8.1, when AC-01 runs, then complete ordered 52/28 preserved obligations pass with no skipped/not-run tests.
- Given each required mutation, when AC-02 through AC-09 run, then the validator exits 1 with its exact blocker and restored fixtures pass.
- Given all mutations, when AC-10 runs, then every before/after hash matches and coverage is complete.
- Given current PASS results and compatible Story 8.1, when AC-11 runs, then the record binds required facts with `11/11/0/0/0/0`.

## Implementation Notes

The preserved planning baseline predates the commit that added this spec and
updated four root gitlinks. `implementation_start_commit` records the task-entry
revision for the Story 8.2 no-production-change boundary. The final record retains
the full planning-baseline delta; inherited gitlinks remain visible, while the
validator requires their task-entry and candidate identities to match.

## Spec Change Log

## Review Triage Log

## Verification

- Run all eleven contract commands verbatim; first build conformance with the required candidate stamp for its Release acceptance lane.
- Run `python3 -m pytest -q _bmad/scripts/tests/test_generate_ux_preservation_disposition.py _bmad/scripts/tests/test_generate_story_record.py`; require no failures or skips.
- Pass completion and `--verify-inserted-record` gates; independently probe every new blocker and confirm protected bytes remain identical.
