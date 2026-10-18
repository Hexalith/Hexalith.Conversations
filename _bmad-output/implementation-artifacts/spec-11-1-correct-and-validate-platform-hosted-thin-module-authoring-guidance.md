---
title: 'Story 11.1: Correct and validate platform-hosted thin-module authoring guidance'
type: 'feature'
created: '2026-10-18'
status: 'done'
baseline_commit: 'ed81a80ea0a8cb0c6e31f4755f082b8913389e6c'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `docs/domain-module-authoring-template.md` tells authors to create module-owned `ServiceDefaults`, Aspire/Dapr wiring, projection and telemetry pieces, and counts a shipping `AppHost`. It does not assign infrastructure to the platform or forbid a reusable module Aspire library, so SM-2 evidence cannot trust the recipe.

**Approach:** Correct the guidance source, generate a schema-validated deterministic guidance bundle (JSON + Markdown) from it, validate ownership/prohibition/AppHost rules with a Python generator, verifier, xUnit tests and a fault matrix, and record a reviewer decision bound to the bundle digests. Checkpoint A only: no fixture, no SM-2.

## Boundaries & Constraints

**Always:** Module-owned = contracts, domain behavior, module registration, module tests. Platform-owned = `ServiceDefaults`, Dapr implementation/wiring, event subscription, projection/query runtime, publication, health, telemetry. Exactly one repository-only test AppHost, `IsPackable=false`, `IsPublishable=false`, counted in authoring cost. Bundle fields: `schemaVersion` (`hexalith.conversations.thin-authoring-guidance.v2`), `candidate`, `source`, `moduleOwned`, `platformOwned`, `prohibited`, `testAppHost`, `validation`, `reviewDecision`, `digests`. Output is deterministic and byte-restored after every fault. Keep existing `DomainModuleAuthoringTemplateValidationTest` facts passing unless a correction truly supersedes them.

**Never:** Create the fixture, compute or decide SM-2, alter v1 SM-2 baseline or historical guidance evidence (`thin-authoring-template-validation-v1.md`), edit predecessor records, change submodules, or restore retired authority/evidence gates.

**Owner decision, 2026-10-18:** AC-11.1-01 does not build the absent `verify_story_candidate.py`. Substitute current-policy checks: candidate is committed `HEAD`, Story 7.4 and 10.4 final-record v2 digests are recomputed, and Story 6.2 is bound through its existing release evidence (no 6.2 v2 record). Record the substitution in the bundle `validation` and Implementation Notes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Valid guidance | Corrected source, approved review | PASS, deterministic JSON/MD | N/A |
| Ownership drift | Capability missing, extra or reassigned | FAIL | `THIN_GUIDANCE_OWNERSHIP_INVALID` |
| AppHost violation | 0 or 2 hosts, packable/publishable, cost rule omitted | FAIL | `APPHOST_BOUNDARY_INVALID` |
| Platform capability in module / reusable Aspire library | Prohibition absent | FAIL | `PROHIBITED_CAPABILITY_PRESENT` |
| Review missing | No approved decision | FAIL | `REVIEW_DECISION_MISSING` |
| Drift after generation | Source or MD/JSON edited | FAIL | `GUIDANCE_SOURCE_DRIFT` / `EVIDENCE_FORMAT_DRIFT` |

**Owner decision, 2026-10-18 (AC-07):** Add a closed Story 11.1 route to `generate_story_record.py`, its schema and tests, following the Story 10.4 pattern, so the final record binds `SC-11.1` and reaches `7/7/0/0/0/0`.

**Owner decision, 2026-10-18 (reviewer):** The reviewer is Jerome Piquot. The decision is `approved`, stamped with a UTC instant when the human approves the generated bundle at the end of this story, and bound to the bundle's signable-payload digest.

</frozen-after-approval>

## Code Map

- `docs/domain-module-authoring-template.md` -- guidance source; correct the skeleton table (ServiceDefaults/Aspire/Dapr rows) and add ownership, prohibition and AppHost sections.
- `tests/Hexalith.Conversations.Contracts.Tests/Documentation/DomainModuleAuthoringTemplateValidationTest.cs` -- add `GuidanceShouldDefineOneNonShippingTestAppHost` and `GuidanceShouldProhibitPlatformCapabilityOwnership`; uses `RepositoryLocator`; CRLF.
- `_bmad-output/planning-artifacts/v9/story-contracts/11.1.json` -- frozen contract with AC commands; do not edit.
- `docs/release-evidence/thin-authoring-template-validation-v1.md`, `minimal-module-authoring-cost-sm2-baseline-v1.*` -- history; do not edit.
- `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json` -- final-record route (see Open Questions); pattern from 10.4.
- `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- pattern for restore-checked fault matrices.

## Tasks & Acceptance

**Execution:**
- [x] `docs/domain-module-authoring-template.md` -- correct ownership, add prohibition and test-AppHost sections -- source of truth
- [x] `docs/release-evidence/thin-authoring-guidance-v2.schema.json` -- closed schema with the ten required fields
- [x] `_bmad/scripts/generate_thin_authoring_guidance.py` -- emit deterministic JSON/MD with digests; exit 1 `THIN_GUIDANCE_OWNERSHIP_INVALID` on inventory mismatch
- [x] `_bmad/scripts/verify_thin_authoring_guidance.py` -- schema, source/evidence drift, format parity, review decision bound to signable-payload digest
- [ ] `docs/release-evidence/thin-authoring-guidance-v2.{json,md}` -- generated bundle with approved reviewer decision (named reviewer and UTC instant supplied by the human)
- [x] `tests/.../DomainModuleAuthoringTemplateValidationTest.cs` -- the two new methods
- [x] `_bmad/scripts/tests/test_thin_authoring_guidance.py` -- `checkpoint_a_fault_matrix`: reassign each platform capability, add reusable Aspire library, packable/publishable AppHost, omit cost rule, post-generation source edit, remove review; exact blockers; byte-identical restore
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py` -- closed 11.1 route and facts; AC-01 substitute; AC-07 final record; receipts under `artifacts/v9/11.1/`

**Acceptance Criteria:**
- Given the corrected source, when the generator runs twice, then JSON and Markdown are byte-identical and ownership matches the inventory.
- Given each fault mutation, when the real checker runs, then it yields its exact blocker and bytes restore.
- Given the Contracts test assembly built in Release, when AC-03/AC-04 run with `-failSkips`, then both pass with zero skips.

## Implementation Notes

- AC-11.1-01 substitution (owner decision 2026-10-18): `verify_story_candidate.py` is not built. The bundle `validation.authoritySubstitution` and the final record's `thinGuidanceExecution.authoritySubstitution` record that the candidate is the committed `HEAD`, the Story 7.4 and 10.4 final-record v2 digests are recomputed (pair verified, digest equal to the bundle row), and Story 6.2 is bound through `docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json` (no 6.2 v2 record). The generator derives AC-11.1-01 itself; there is no `AC-11.1-01.json` receipt.
- Bundle `candidate` is the story contract plus planning candidate (not `HEAD`) so regeneration is byte-stable across commits; the committed-`HEAD` candidate is bound by the final record. The generator preserves a review decision still bound to the unchanged signable-payload digest, so AC-11.1-02 re-runs are idempotent.
- The reviewer decision is intentionally absent (`reviewDecision: null`) until the human approves. Approve with `generate_thin_authoring_guidance.py ... --approve-reviewer "Jerome Piquot" --decided-at-utc <UTC instant>`, then commit, run AC-11.1-03 through AC-11.1-06 against that commit, and run AC-11.1-07 (receipts under `artifacts/v9/11.1/` must be force-added like the other stories').
- The fault ids and exact blockers live once in `FAULT_EXPECTATIONS` (`generate_thin_authoring_guidance.py`); the fault matrix test, the AC-11.1-06 JUnit check and the final-record mutation ledger all derive from it.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| VG1 | medium | patch | `v2_generate` 11.1 dispatch, `V2Stop` mapping and the disk-receipt freshness/symlink guard are only reached with an injected `read_receipt`; add real-path tests. |
| VG2 | medium | patch | AC-03/04 TRX validation (wrong method, skipped/failed, total != 1) has no negative test. |
| VG3 | medium | patch | AC-11.1-01 substitute: predecessor digest/pair-verification mismatch has no negative test. |
| VG-o1 / E8 | low | patch | `AC-11.1-01.json` in `receipt_paths` tolerates an unvalidated dirt file; no receipt of that name exists. Delete the entry. |
| VG-o2 / B3 | low | patch | `candidate != HEAD` after defaulting to HEAD is tautological for the default path; compare only when `candidate` was supplied. |
| VG-o3 / B7 / E18 | low | defer | Story 10.4 fixtures skip live `.cs` files; narrows 10.4 fixture coverage but is required so adding 11.1 tests does not trip the frozen 10.4 reader check. |
| B1 / E19 | false | reject | Null `reviewDecision` is the specified pre-approval state; the owner decision stamps it at story end. |
| B2 | false | reject | Free-text approver is the owner-decided mechanism (Jerome approves via the CLI). |
| E1 | false | reject | Verification uses the separate `--verify-inserted` path (line ~10005); `v2_generate` is not the verify entry. |
| E3 | low | patch | Zero-result TRX raises IndexError instead of a ValueError; require exactly one result. |
| E15 / E4 | low | patch | Verifier: catch ImportError/UnicodeDecodeError as `Blocked`; guard ParseError on JUnit. |
| E13 | low | patch | `--decided-at-utc` must be a real date (strptime) and ASCII digits only; verifier uses fullmatch. |
| E17 | low | patch | Record schema: `receipts` need unique, exactly the four scenarioIds. |
| E10 / B11a | medium | patch | `skeleton_violation` fullmatch misses names like `Hexalith.X.ServiceDefaults` or "Reusable Aspire library"; broaden to a case-insensitive search excluding the single AppHost row. |
| B4, B5, B6, B8-B14, E2, E5-E7, E9, E11, E12, E14, E16 | low/maybe-false | defer | Hardening, design-duplication and doc-completeness concerns beyond the spec's contract (see deferred-work.md). |

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 11.1 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `11.1`
- Candidate: `df599aeedbd3e833f735603b5190cebd189cdc2e`
- JSON content SHA-256 (all three digest fields zeroed): `e98707d7e24744caaf24f62070f34754a050b6134bd9cd1b24288c2edc8ec2e2`

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
| `references/Hexalith.Builds` | `160000` | `6e8b71e9bc5fe4a38c86069fd8e37f6a6cafa292` |
| `references/Hexalith.Commons` | `160000` | `9e874b1a3c6e7b6ff07b6f40b81d36e4e1403798` |
| `references/Hexalith.EventStore` | `160000` | `1aaa173715305a08aff30c4b9cd6354a7073e541` |
| `references/Hexalith.Folders` | `160000` | `65f34e7c4b2bd41805aaa1c3b8d87ee3b65363f1` |
| `references/Hexalith.FrontComposer` | `160000` | `b102b85b148f21fb26d5742e120c344109df7084` |
| `references/Hexalith.McpCli` | `160000` | `b35b0975a234f1f97e2e35e9dad381d0a988f4af` |
| `references/Hexalith.Memories` | `160000` | `fb39a0c59cc3befd3176dec5e3c790f94d0e5bd1` |
| `references/Hexalith.Parties` | `160000` | `3358e3c71c44af877aa1b775db32b5c4f66c313a` |
| `references/Hexalith.Platform` | `160000` | `b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b` |
| `references/Hexalith.Projects` | `160000` | `3d3b3fab63afe63090e6314e2c2b6e66bcd0ec31` |
| `references/Hexalith.Tenants` | `160000` | `5efab750d471064c667e6d9e36cb222fa823bda5` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-11.1-ENTRY-v1` | `b6c050f6eb387a645fb85bff6d98be90ef9286227c33974c93269f8acbde56ac` |

## Predecessors

- `10.4`
- `6.2`
- `7.4`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-11.1-01` | `0` | `PASS` | `none` | `4` | none | none |
| `AC-11.1-02` | `0` | `PASS` | `none` | `2` | none | none |
| `AC-11.1-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.1/AC-11.1-03.trx` | `431ffdc3ab42c075b4f95a48b6091f3abb3e319c055d35e00ed817bf088f69c0` |
| `AC-11.1-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.1/AC-11.1-04.trx` | `61fb83bf08ba69bd4a4b5729dcbcf69acc3c270854f587f848c48fa25b790318` |
| `AC-11.1-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.1/AC-11.1-05.json` | `5011787bb43a857d18b60c174dc1512bede7df74bd603fa148181f44f00c6944` |
| `AC-11.1-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/11.1/AC-11.1-06.xml` | `f92a797dfed285c4e72c3107a1e9c5963d33634a59958a9eb1f8e524b88cccc7` |
| `AC-11.1-07` | `0` | `PASS` | `none` | `2` | none | none |

### `AC-11.1-01`

Command: `python3 _bmad/scripts/verify_story_candidate.py --repository . --authority-bundle _bmad-output/planning-artifacts/v9-authority-bundle-v1.json --contract _bmad-output/planning-artifacts/v9/story-contracts/11.1.json --candidate HEAD --output artifacts/v9/11.1/AC-11.1-01.json`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-01#0001` | `candidate is the committed HEAD` | `PASS` |
| `AC-11.1-01#0002` | `Story 7.4 and 10.4 final-record v2 digests recomputed` | `PASS` |
| `AC-11.1-01#0003` | `Story 6.2 bound through its existing release evidence` | `PASS` |
| `AC-11.1-01#0004` | `verify_story_candidate.py substitution recorded in the bundle validation` | `PASS` |

### `AC-11.1-02`

Command: `python3 _bmad/scripts/generate_thin_authoring_guidance.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.1.json --schema docs/release-evidence/thin-authoring-guidance-v2.schema.json --output-json docs/release-evidence/thin-authoring-guidance-v2.json --output-markdown docs/release-evidence/thin-authoring-guidance-v2.md`

| Bound output | SHA-256 |
| --- | --- |
| `docs/release-evidence/thin-authoring-guidance-v2.json` | `4f078797f4b5c85aef91b3ea9cedd29dac25a3f28b62dcdca1d8a2db4335c0e2` |
| `docs/release-evidence/thin-authoring-guidance-v2.md` | `990b46d68df0fc05777459e20308ff8dd9e4dba5dda4a8b5da3482ad66b1948b` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-02#0001` | `committed JSON and Markdown equal a fresh deterministic regeneration` | `PASS` |
| `AC-11.1-02#0002` | `module and platform ownership inventories are exact` | `PASS` |

### `AC-11.1-03`

Command: `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.DomainModuleAuthoringTemplateValidationTest.GuidanceShouldDefineOneNonShippingTestAppHost -trx artifacts/v9/11.1/AC-11.1-03.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-03#0001` | `GuidanceShouldDefineOneNonShippingTestAppHost` | `PASS` |

### `AC-11.1-04`

Command: `dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Release/net10.0/Hexalith.Conversations.Contracts.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Contracts.Tests.Documentation.DomainModuleAuthoringTemplateValidationTest.GuidanceShouldProhibitPlatformCapabilityOwnership -trx artifacts/v9/11.1/AC-11.1-04.trx`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-04#0001` | `GuidanceShouldProhibitPlatformCapabilityOwnership` | `PASS` |

### `AC-11.1-05`

Command: `python3 _bmad/scripts/verify_thin_authoring_guidance.py --repository . --evidence docs/release-evidence/thin-authoring-guidance-v2.json --markdown docs/release-evidence/thin-authoring-guidance-v2.md --require-review-decision --output artifacts/v9/11.1/AC-11.1-05.json`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-05#0001` | `verifier PASS with review decision required` | `PASS` |

### `AC-11.1-06`

Command: `python3 -m pytest -q _bmad/scripts/tests/test_thin_authoring_guidance.py -k checkpoint_a_fault_matrix --junitxml=artifacts/v9/11.1/AC-11.1-06.xml`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-06#0001` | `checkpoint A fault matrix, every mutation detected and restored` | `PASS` |

### `AC-11.1-07`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.1.json --format bundle --output-json docs/release-evidence/story-11.1-final-record-v2.json --output-markdown docs/release-evidence/story-11.1-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-11.1-07#0001` | `AC-11.1-01 through AC-11.1-06 are current and compatible` | `PASS` |
| `AC-11.1-07#0002` | `record binds SC-11.1, predecessors, bundle and reviewer digests` | `PASS` |

## Thin-module authoring guidance

- Guidance source: `docs/domain-module-authoring-template.md` (`6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb`)
- Bundle JSON: `docs/release-evidence/thin-authoring-guidance-v2.json` (`4f078797f4b5c85aef91b3ea9cedd29dac25a3f28b62dcdca1d8a2db4335c0e2`)
- Bundle Markdown: `docs/release-evidence/thin-authoring-guidance-v2.md` (`990b46d68df0fc05777459e20308ff8dd9e4dba5dda4a8b5da3482ad66b1948b`)
- Signable payload: `9ce146a26cd693261392928a2f767d3752b0101f6097e8a0bc7757784a07ef80`
- Reviewer: `Jerome Piquot` (`approved` at `2026-10-18T16:29:59Z`)
- AC-11.1-01 substitution: `Owner decision 2026-10-18: AC-11.1-01 does not build the absent verify_story_candidate.py; current-policy checks are substituted.`

| Predecessor | Binding | Path | SHA-256 |
| --- | --- | --- | --- |
| `6.2` | `release-evidence` | `docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json` | `d18d81286e171f4df1330677bc236ecdc388764ff58a0ed4be32111fdba31b76` |
| `7.4` | `final-record-v2` | `docs/release-evidence/story-7.4-final-record-v2.json` | `1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290` |
| `10.4` | `final-record-v2` | `docs/release-evidence/story-10.4-final-record-v2.json` | `f6a670f7a9d6be89b765757c0d480879353e033217980c28329e6cdf3f5d38b9` |

| Receipt | Path | SHA-256 |
| --- | --- | --- |
| `AC-11.1-03` | `artifacts/v9/11.1/AC-11.1-03.trx` | `431ffdc3ab42c075b4f95a48b6091f3abb3e319c055d35e00ed817bf088f69c0` |
| `AC-11.1-04` | `artifacts/v9/11.1/AC-11.1-04.trx` | `61fb83bf08ba69bd4a4b5729dcbcf69acc3c270854f587f848c48fa25b790318` |
| `AC-11.1-05` | `artifacts/v9/11.1/AC-11.1-05.json` | `5011787bb43a857d18b60c174dc1512bede7df74bd603fa148181f44f00c6944` |
| `AC-11.1-06` | `artifacts/v9/11.1/AC-11.1-06.xml` | `f92a797dfed285c4e72c3107a1e9c5963d33634a59958a9eb1f8e524b88cccc7` |

## Fault injection

24 observed mutation results are bound to the retained receipts.

| Fault | Expected blocker | Detected | Restored |
| --- | --- | --- | --- |
| `reassign-service-defaults` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `reassign-dapr-wiring` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `reassign-event-subscription` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `reassign-projection-query-runtime` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `reassign-publication` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `reassign-health` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `reassign-telemetry` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `missing-capability` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `extra-capability` | `THIN_GUIDANCE_OWNERSHIP_INVALID` | `True` | `True` |
| `prohibition-removed` | `PROHIBITED_CAPABILITY_PRESENT` | `True` | `True` |
| `reusable-aspire-library-added` | `PROHIBITED_CAPABILITY_PRESENT` | `True` | `True` |
| `module-service-defaults-added` | `PROHIBITED_CAPABILITY_PRESENT` | `True` | `True` |
| `namespaced-service-defaults-added` | `PROHIBITED_CAPABILITY_PRESENT` | `True` | `True` |
| `reusable-aspire-library-phrase-added` | `PROHIBITED_CAPABILITY_PRESENT` | `True` | `True` |
| `apphost-packable` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-publishable` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-cost-rule-omitted` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-second-host` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `apphost-none` | `APPHOST_BOUNDARY_INVALID` | `True` | `True` |
| `review-removed` | `REVIEW_DECISION_MISSING` | `True` | `True` |
| `source-edited-after-generation` | `GUIDANCE_SOURCE_DRIFT` | `True` | `True` |
| `markdown-edited` | `EVIDENCE_FORMAT_DRIFT` | `True` | `True` |
| `json-edited` | `EVIDENCE_FORMAT_DRIFT` | `True` | `True` |
| `schema-opened` | `EVIDENCE_FORMAT_DRIFT` | `True` | `True` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-11.1-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-11.1-final-record-v2.md` |

## Rollback boundary

restore only the Story 11.1 guidance source and remove its generator, validator, generated v2 bundle, results, and final record; preserve all predecessor records and historical guidance evidence.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `7` | `7` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
