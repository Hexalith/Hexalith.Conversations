---
title: 'Enforce the 52-decision/28-acceptance zero-gap validator'
type: 'feature'
created: '2026-10-03'
status: 'done'
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
- [x] `docs/release-evidence/story-8.2-final-record-v2.json`, `docs/release-evidence/story-8.2-final-record-v2.md`, this spec, `_bmad-output/implementation-artifacts/sprint-status.yaml` — run acceptance commands; generate, insert, and verify the record before done.

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
- Candidate: `fdc7b8793d320f81124c0e30eebcbdb020715140`
- JSON content SHA-256 (all three digest fields zeroed): `e2cb05df7a52e87176b69ff8c2bbb9dd7a8a6d5e2803626bc7b02edc7f0f9360`

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
| `AC-8.2-01` | `0` | `PASS` | `none` | `6` | `artifacts/v9/8.2/AC-8.2-01.trx` | `abde691d1b1b342753edaf9b3c9ccac1da7b670b05485a293fac7e77f8d18664` |
| `AC-8.2-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-02.xml` | `f18f4e908307e1dd2a2a75b0146977a3b5cd6d39ab3032ff191be5f4f6681286` |
| `AC-8.2-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-03.xml` | `93b70c2e86cd6d11ee43acf5cda1e64db22844af545527d145f6aa2d8f1c2126` |
| `AC-8.2-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-04.xml` | `ec6d63ca7560bbb6456af5c33fa1adb3d059c9d51ea8aeb74e1be38a0303f933` |
| `AC-8.2-05` | `0` | `PASS` | `none` | `3` | `artifacts/v9/8.2/AC-8.2-05.xml` | `e594d80282598d0a3129e3d8e3336a3d4361c3ac0ada08688557669a8349d0d7` |
| `AC-8.2-06` | `0` | `PASS` | `none` | `2` | `artifacts/v9/8.2/AC-8.2-06.xml` | `b35723f24621f9d4d0b2aa9104332aa8dcaa4edfd00dfa49ef275a9676ba75ee` |
| `AC-8.2-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/8.2/AC-8.2-07.xml` | `b387a652d41dec2cca47827dc18965336e36a06f5a8c4d08b67ef66aeaca5f21` |
| `AC-8.2-08` | `0` | `PASS` | `none` | `3` | `artifacts/v9/8.2/AC-8.2-08.xml` | `2b9a4de8e024e8f7ba7d2d792ae41582d932870f27c776a8d6dd22123df1125b` |
| `AC-8.2-09` | `0` | `PASS` | `none` | `3` | `artifacts/v9/8.2/AC-8.2-09.xml` | `9b0c83fe58b1a9773fe0ffa7b64b8aa7f2d52cda9f5a3e0696852806a649edc6` |
| `AC-8.2-10` | `0` | `PASS` | `none` | `15` | `artifacts/v9/8.2/AC-8.2-10.xml` | `9d0f459b1c6cae91d4fa93b1069194e86d8bce0ed99084cdc9e52a2d4cfe10d6` |
| `AC-8.2-11` | `0` | `PASS` | `none` | `15` | none | none |

### `AC-8.2-01`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -class Hexalith.Conversations.Conformance.Tests.UxPreservationDispositionValidationTest -trx artifacts/v9/8.2/AC-8.2-01.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/8.2/AC-8.2-01.trx` | `abde691d1b1b342753edaf9b3c9ccac1da7b670b05485a293fac7e77f8d18664` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `34bfb89eb9104951b15f3a5cc6704480340f39dc5001c67e0063b71bc55ba6bb` |

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

- Candidate: `fdc7b8793d320f81124c0e30eebcbdb020715140`
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
