---
title: 'Freeze the conformance assertion inventory, tier decisions, digest, and approvals'
type: 'feature'
created: '2026-10-06'
status: 'done'
baseline_commit: '602f6f52f283f72213826e44f6cd87de05d0d948'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'docs/runbooks/current-change-validation.md'
  - '_bmad-output/implementation-artifacts/epic-9-context.md'
  - '_bmad-output/planning-artifacts/v9/story-contracts/9.1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 9.2 lacks a frozen assertion disposition, current pre-split result, and digest-bound membership approvals.

**Approach:** Freeze source/machine identities, propose tiers, obtain Quality-owner digest approval, and generate the disposition and final record.

## Boundaries & Constraints

**Always:** Honor the exact nine `9.1.json` scenarios and `V9-9.1-ENTRY-v1`. Bind source, result, build, decision, predecessor, and candidate digests. Preserve the three telemetry/status suites and accumulated FR-20 membership. Strength material is the canonical triple of bound assemblies, behavior identity, and negative-case count. Every row needs a justified public replacement or exact internal type/reason and genuine approval evidence.

**Never:** Split projects (Story 9.2), change production/public APIs or dependencies, weaken/rename/remove assertions, edit protected v1 or accepted records, or treat historical approval as approval of unseen rows. Historical checks excluded from CI cannot be counted as currently passed.

**Decision:** Use the current supported CI execution lane, retain every source/discovery identity, and bind the historical exclusions explicitly. Retired definitions remain preserved without a current pass claim.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Complete approved inventory | Bound source/result and approved row digest | Deterministic closed schema, JSON, Markdown | PASS, exit 0 |
| Assertion drift | Missing, duplicate, renamed, weakened row | Reject exact observed defect | Corresponding `CONFORMANCE_ASSERTION_*` or `ASSERTION_STRENGTH_WEAKENED` |
| Incomplete disposition | Missing tier, reason, or approval | No accepted bundle | `TIER_UNASSIGNED`, `TIER_REASON_MISSING`, `TIER_APPROVAL_MISSING` |
| Preservation violation | Denominator, public shape, or v1 mutation | Reject; restore isolated fixtures exactly | `FR20_DENOMINATOR_DRIFT`, `PUBLIC_CONTRACT_WIDENED`, `V1_ARTIFACT_DRIFT` |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:2651` — canonical Story 9.1 fields, commands, and ten required fault categories; architecture DC-9 adds Quality ownership.
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` — approved decision with null triage; proposal approves three suite reclassifications, not new individual rows.
- `docs/release-evidence/release-baseline-v1.json` and `preservation-traceability-manifest-v3-rc2.json` — original and accumulated exact denominator identities. V2 manifest supplies protected v1 hashes.
- `tests/Hexalith.Conversations.Conformance.Tests/` — analyze transitive fixture/helper bindings, theories, and assertion sites. Four generation methods overwrite evidence; capture in isolation.
- `_bmad/scripts/generate_ux_preservation_disposition.py` — deterministic schema/rendering and read-only verification precedent.
- `_bmad/scripts/generate_story_record.py` — reuse `v2_successor_command`, result parsing, and predecessor-pair validation; add tiering facts and retention.
- `docs/release-evidence/story-7.4-final-record-v2.json` — valid pair outside current ancestry; verify required content/digests without rewriting history.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/scripts/generate_conformance_tiering.py` — reconcile machine cases and assertion sites, derive strength/transitive bindings, propose tiers, and generate/verify deterministically. Unsupported discovery must fail.
- [x] `artifacts/v9/9.1/` — retain source/discovery, pre-split execution, assembly hashes, command/candidate receipts, and byte-restored fault observations. Freeze before adding validation cases; label those additions separately.
- [x] `docs/release-evidence/conformance-oracle-tiering-approvals-v2.json` — prepare digest-bound proposed membership for Quality review; record the actual owner decision only after approval. Keep historical suite approval distinct.
- [x] `docs/release-evidence/conformance-oracle-tiering-disposition-v2.{schema.json,json,md}` — generate required closed fields, complete rows, versioned denominator membership, and digest-bound supersession after approval; preserve decision/v1 bytes.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConformanceOracleTieringValidationTest.cs` — implement the seven exact AC-9.1-02 through -08 methods with independent positive checks and safe read-only verification.
- [x] `_bmad/scripts/tests/test_conformance_tiering.py` — prove source/theory/helper completeness, deterministic output, all ten defect categories, and exact restoration through real CLI fixtures.
- [x] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/story-final-record-v2.schema.json`, and `_bmad/scripts/tests/test_generate_story_record.py` — bind Story 9.1 measured facts, approvals, predecessor compatibility, outputs, fault evidence, and nonvacuous receipts; retain prior story behavior.
- [x] `docs/runbooks/story-final-record-generation.md` — document capture selection, approval, commands, blockers, and candidate retention.
- [x] `docs/release-evidence/story-9.1-final-record-v2.{json,md}` — generate and verify insertion; update this spec and `sprint-status.yaml` only after completion gates pass.

**Acceptance Criteria:**
- Given approved bound inputs, when AC-9.1-01 runs, then the required disposition schema and deterministic pair verify.
- Given frozen source/result identities, when AC-9.1-02 through -04 run, then every assertion occurs once with exact justified tier and unchanged strength.
- Given denominator, approvals, public baseline, and protected evidence, when AC-9.1-05 through -08 run, then membership, genuine approvals, public shape, and v1 bytes verify.
- Given passing current scenario and fault receipts, when AC-9.1-09 runs, then the derived final record binds all required facts and reports `9/9/0/0/0/0`.

## Implementation Notes

**KEEP across review derivation:** the isolated pre-split capture at freeze commit `611a835af50d66ff1860c1497c16a19713cd5042`; a Quality-owner decision bound to the current membership digest; the source-closure strength definition; the IL lower-bound cross-check; and the unchanged decision and v1 bytes. Any change to a frozen test source, a validation addition, the pre-split result, or the decision changes the membership digest and needs a new owner decision.

- **Freeze and capture.** `--capture-pre-split` cloned the freeze commit into `/var/tmp/hexalith-9.1-pre-split-5c2sa9e6` with the root submodules only, read the `ci / conformance` lane from that commit's `ci.yml`, and built Release with `CI=true` (`SourceRevisionId` equals the freeze commit). Discovery found 452 methods. The lane executed 415 cases (401 methods, two theories with 16 cases): 412 passed and 3 failed. The 3 failures are recorded, not claimed: `GovernanceAuditPairingSafetyNetConformanceTest.AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory`, `PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting`, and `ReleaseBaselineValidationTest.BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface`. GitHub CI at the same commit also failed `SuccessMetricReportAndAttestationValidationTest.SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity`, because the snapshot generator rewrote the protected baseline earlier in that run order. The 51 CI-excluded methods keep their discovered identities with no pass claim. In the clone, the four generation methods rewrote only `public-contract-shape-baseline-v1.json`; the real working tree was not touched.
- **Strength and bindings.** A deterministic C# source model derives each test's closure (declaration, declaring-class constructors, fields, and initialized properties, named members, and named or extension-providing project types, whole and transitive). The canonical triple is the bound first-party module assemblies, the SHA-256 behavior identity of the closure text, and the negative-site count. Unsupported constructs fail with `CONFORMANCE_DISCOVERY_UNSUPPORTED`. The C# `ConformanceTieringIlReferenceReader` independently walks the compiled IL; its Server-bound set equals the source-derived module-internal set (83 tests).
- **Tiers.** Rows binding `Hexalith.Conversations.Server` are module-internal with their exact Server types and reason; all others are portable with an unchanged-assertion replacement at equal strength. No public re-expression was attempted, so no strength changed. The 452 frozen rows split into 376 portable and 76 module-internal (nine classes); every class is single-tier. The seven validation additions bind no Server type: AC-9.1-03 checks exact Server types against the Server project's declared source types, so the validation class stays outside the guarded residual-coupling inventory and is portable. The three reclassified suites keep their exact v1-floor membership and are module-internal. All 13 files of the residual coupling inventory close only over module-internal rows.
- **AC-9.1-07 reading (accepted by the owner on 2026-10-07).** Story 9.1 compares the candidate's public project sources with the frozen pre-story surface. The earlier post-PC Agents growth of the public surface is recorded as `preExistingDrift` with `approvalClaimed: false`; it is not approved here.
- **Approvals.** The proposal binds a per-row digest for every row and suite. Decision `QO-9.1-TIERING-2026-10-07` approved the first membership digest `13c4e3ea0dffc562db0a6e0d7c7de62e9be766f35c431db16f6145984fe99c7d` (kept in Git history at `c655b94`); the validation-class fix changed the seven addition rows. Decision `QO-9.1-TIERING-2026-10-07-R2` (Jerome Piquot, 2026-10-07) approved the regenerated membership digest `37a40bfc94151a6bb86356d311ad4e10918e0a53881f20d93a642bec6be035de`; the 452 frozen rows and three suite rows kept their previously approved digests. The decision-v2 suite approval of 2026-07-28 stays separate as `historicalSuiteApproval` with `approvesRows: false`.
- **Out of scope, pre-existing.** At the untouched `HEAD`, nine Story 8.2 tests in `test_generate_story_record.py` fail because root gitlinks moved after Story 8.2's scope start. The directory lane result is 804 passed and these 9 failed, the same with and without this story.
- **Lifecycle bookkeeping.** The candidate restores `sprint-status.yaml`'s `last_updated` to `YYYY-MM-DD`, the form the retention checker parses, so the post-record status transition stays a lifecycle-only change.

## Spec Change Log

- 2026-10-07, after the first record: running the full current conformance lane at the done commit `030faf2` failed `RemovedTestJustificationLedgerReconciliationValidationTest.ProjectReferenceDispositionShouldMatchCurrentProjectAndServerUsingInventory`. The new validation class imported `Hexalith.Conversations.Server.Diagnostics` (first for AC-9.1-03, then as a class-level anchor), which adds a Server-coupled file outside the guarded v1 residual-coupling inventory. Hiding the import behind full qualification or a subdirectory would defeat that guard, and the guard test may not be weakened. The record pair was retracted in `4f5f79c`; the validation class now verifies exact Server types from the Server project source and binds no Server type, so its seven rows become portable. KEEP the frozen 452 rows, the capture, and the strength definition; do not reintroduce a Server import into a new top-level conformance file.

## Review Triage Log

## Verification

- Run the exact nine commands in `9.1.json`; preserve declared result paths and measured summaries.
- Run focused tiering/record pytest suites and direct xUnit class checks. Use Debug locally; retain distinct Release evidence for frozen acceptance commands.
- Verify determinism, fault restoration, protected inputs, and `git diff --check`.

<!-- STORY-FINAL-RECORD:BEGIN -->
# Story 9.1 Final Record

<!-- hexalith.conversations.story-final-record.v2 markdown projection -->

Generated by `_bmad/scripts/generate_story_record.py` from the committed candidate and measured scenario results. The JSON record is authoritative; this rendering is bound to it by digest.

- Schema: `hexalith.conversations.story-final-record.v2`
- Result: `PASS`
- Story: `9.1`
- Candidate: `cd2e64bc3bc15e7e06c9cbd8e1d3104d89d36c2e`
- JSON content SHA-256 (all three digest fields zeroed): `da14c203c6fde3133eb67efb45083ea56cf40c1c504bee115e1c0a384b128cf7`

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
| `references/Hexalith.Builds` | `160000` | `ba4ca78c3868a4757cb92d912a54c8a237871b54` |
| `references/Hexalith.Commons` | `160000` | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| `references/Hexalith.EventStore` | `160000` | `283b07a52c9c70e1c940164a7011ee8c3ad98b2d` |
| `references/Hexalith.Folders` | `160000` | `381a32304ed9aa2ce4e5f8f1b0bfa4016e082b27` |
| `references/Hexalith.FrontComposer` | `160000` | `c561b3210f15206a90c39c82c58f2e5b1005cd60` |
| `references/Hexalith.Memories` | `160000` | `0a63ff9d6d5075ac298dcb7266ddb7e82085880e` |
| `references/Hexalith.Parties` | `160000` | `b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca` |
| `references/Hexalith.Projects` | `160000` | `eda6ce4c8603dc2616732df5ade675eee082e23f` |
| `references/Hexalith.Tenants` | `160000` | `5a519cd73018067d9b444dd777034dc91f9bd4b2` |

## Inventory

| Inventory | SHA-256 |
| --- | --- |
| `V9-9.1-ENTRY-v1` | `31e18fed38706bbb44e5a8059ff3ea30f00400708902694af697954b889bcdf1` |

## Predecessors

- `7.4`

## Scenarios

| Scenario | Exit | Result | Blockers | Assertions | Result file | Result file SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| `AC-9.1-01` | `0` | `PASS` | `none` | `10` | `docs/release-evidence/conformance-oracle-tiering-disposition-v2.json` | `1203fa7b216d4f4314fcdd28bfd0d8021d1a303abe6d66afb1d49a373ad12273` |
| `AC-9.1-02` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-02.trx` | `274453f4db66cf226a07236159921784db8d6a11c64f20f169a57ed2a2461f2d` |
| `AC-9.1-03` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-03.trx` | `87d678fddffb78de308df8ab388299f31ce7021b2bed33af811ceb71e0df27ef` |
| `AC-9.1-04` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-04.trx` | `29f644794565492aa8773374c1f3c935f244643a6041a7f5b4491fc52eaa130f` |
| `AC-9.1-05` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-05.trx` | `ac60aac0b58b28e3449ad1103cd2d6145e34dd235c1dbaa1d0e6b88d69f7f257` |
| `AC-9.1-06` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-06.trx` | `a56510bfac7b1b001b646c57701668ea8f89e353d2801286effc92fe30821181` |
| `AC-9.1-07` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-07.trx` | `6d6aeddd6ec4bd0a5eb233aa2db7a46d5f7906248181c9df4eb774533b58946a` |
| `AC-9.1-08` | `0` | `PASS` | `none` | `1` | `artifacts/v9/9.1/AC-9.1-08.trx` | `357ded5306e05569f05197ed642867be7ea1209f7115c86249e5edf791418f1f` |
| `AC-9.1-09` | `0` | `PASS` | `none` | `18` | none | none |

### `AC-9.1-01`

Command: `python3 _bmad/scripts/generate_conformance_tiering.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.1.json --decision docs/release-evidence/conformance-oracle-tiering-decision-v2.json --output-schema docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json --output-json docs/release-evidence/conformance-oracle-tiering-disposition-v2.json --output-markdown docs/release-evidence/conformance-oracle-tiering-disposition-v2.md`

| Bound output | SHA-256 |
| --- | --- |
| `docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json` | `c74af9cbd5a5224504105939e8aa608ae93a18803bc0d3ef19e818691e03a22a` |
| `docs/release-evidence/conformance-oracle-tiering-disposition-v2.json` | `1203fa7b216d4f4314fcdd28bfd0d8021d1a303abe6d66afb1d49a373ad12273` |
| `docs/release-evidence/conformance-oracle-tiering-disposition-v2.md` | `f782688765ff79ce2e20b067c1fcbf88857f0a49d8a737970a24436d54771d94` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-01#0001` | `schema::closed-v2` | `PASS` |
| `AC-9.1-01#0002` | `inventory::pre-split-identities-once` | `PASS` |
| `AC-9.1-01#0003` | `strength::canonical-triple-digests` | `PASS` |
| `AC-9.1-01#0004` | `tiers::exact-public-replacement-or-internal-type` | `PASS` |
| `AC-9.1-01#0005` | `approvals::quality-owner-digest` | `PASS` |
| `AC-9.1-01#0006` | `denominator::fr20-membership-unchanged` | `PASS` |
| `AC-9.1-01#0007` | `public::freeze-surface-unchanged` | `PASS` |
| `AC-9.1-01#0008` | `supersession::v1-protected-and-linked` | `PASS` |
| `AC-9.1-01#0009` | `markdown::digest` | `PASS` |
| `AC-9.1-01#0010` | `predecessor::7.4` | `PASS` |

### `AC-9.1-02`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.AssertionInventoryShouldMatchPreSplitResultAndSource -trx artifacts/v9/9.1/AC-9.1-02.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-02.trx` | `274453f4db66cf226a07236159921784db8d6a11c64f20f169a57ed2a2461f2d` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-02#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.AssertionInventoryShouldMatchPreSplitResultAndSource` | `PASS` |

### `AC-9.1-03`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ServerBoundAssertionsShouldHaveExactDisposition -trx artifacts/v9/9.1/AC-9.1-03.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-03.trx` | `87d678fddffb78de308df8ab388299f31ce7021b2bed33af811ceb71e0df27ef` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-03#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ServerBoundAssertionsShouldHaveExactDisposition` | `PASS` |

### `AC-9.1-04`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.StrengthDigestsShouldRemainEqual -trx artifacts/v9/9.1/AC-9.1-04.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-04.trx` | `29f644794565492aa8773374c1f3c935f244643a6041a7f5b4491fc52eaa130f` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-04#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.StrengthDigestsShouldRemainEqual` | `PASS` |

### `AC-9.1-05`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.DenominatorSuitesShouldRemainUnchanged -trx artifacts/v9/9.1/AC-9.1-05.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-05.trx` | `ac60aac0b58b28e3449ad1103cd2d6145e34dd235c1dbaa1d0e6b88d69f7f257` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-05#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.DenominatorSuitesShouldRemainUnchanged` | `PASS` |

### `AC-9.1-06`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ReclassificationsShouldBindApprovals -trx artifacts/v9/9.1/AC-9.1-06.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-06.trx` | `a56510bfac7b1b001b646c57701668ea8f89e353d2801286effc92fe30821181` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-06#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.ReclassificationsShouldBindApprovals` | `PASS` |

### `AC-9.1-07`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.TieringShouldNotWidenPublicContracts -trx artifacts/v9/9.1/AC-9.1-07.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-07.trx` | `6d6aeddd6ec4bd0a5eb233aa2db7a46d5f7906248181c9df4eb774533b58946a` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-07#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.TieringShouldNotWidenPublicContracts` | `PASS` |

### `AC-9.1-08`

Command: `dotnet tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll -automated sync -failSkips -method Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.V2ShouldSupersedeWithoutEditingV1 -trx artifacts/v9/9.1/AC-9.1-08.trx`

| Bound output | SHA-256 |
| --- | --- |
| `artifacts/v9/9.1/AC-9.1-08.trx` | `357ded5306e05569f05197ed642867be7ea1209f7115c86249e5edf791418f1f` |
| `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-08#0001` | `Hexalith.Conversations.Conformance.Tests.ConformanceOracleTieringValidationTest.V2ShouldSupersedeWithoutEditingV1` | `PASS` |

### `AC-9.1-09`

Command: `python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/9.1.json --format bundle --output-json docs/release-evidence/story-9.1-final-record-v2.json --output-markdown docs/release-evidence/story-9.1-final-record-v2.md`

| Assertion | Subject | State |
| --- | --- | --- |
| `AC-9.1-09#0001` | `generator::contract-schema-and-identity` | `PASS` |
| `AC-9.1-09#0002` | `generator::authority-bundle-digest-recomputed` | `PASS` |
| `AC-9.1-09#0003` | `generator::raw-gitlinks-equal-root-gitmodules` | `PASS` |
| `AC-9.1-09#0004` | `generator::committed-candidate-worktree-clean` | `PASS` |
| `AC-9.1-09#0005` | `generator::predecessor-scenarios-pass-with-ledgers` | `PASS` |
| `AC-9.1-09#0006` | `generator::declared-output-paths` | `PASS` |
| `AC-9.1-09#0007` | `generator::record-schema-valid` | `PASS` |
| `AC-9.1-09#0008` | `generator::deterministic-rendering` | `PASS` |
| `AC-9.1-09#0009` | `generator::json-markdown-digest-cross-binding` | `PASS` |
| `AC-9.1-09#0010` | `generator::tiering-disposition-reproduced-and-verified-read-only` | `PASS` |
| `AC-9.1-09#0011` | `generator::story-7.4-predecessor-pair-verified` | `PASS` |
| `AC-9.1-09#0012` | `generator::retained-pre-split-capture-receipt-bound` | `PASS` |
| `AC-9.1-09#0013` | `generator::assertion-and-strength-inventories-bound` | `PASS` |
| `AC-9.1-09#0014` | `generator::quality-owner-approval-digest-bound` | `PASS` |
| `AC-9.1-09#0015` | `generator::all-ten-tiering-fault-categories-measured` | `PASS` |
| `AC-9.1-09#0016` | `generator::all-ten-fixtures-restored-byte-identically` | `PASS` |
| `AC-9.1-09#0017` | `generator::seven-exact-xunit-selectors-passed` | `PASS` |
| `AC-9.1-09#0018` | `generator::candidate-stamped-nonempty-results` | `PASS` |

## Story 9.1 conformance tiering

- Candidate: `cd2e64bc3bc15e7e06c9cbd8e1d3104d89d36c2e`
- Candidate rule: SC-9.1 is HEAD^{commit} at final-record generation
- Story 7.4 record SHA-256: `1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290`
- Story 7.4 candidate: `ba1b47622ad8d72e0ea70f01e4e695e49413c1ee`
- Inventory SHA-256: `31e18fed38706bbb44e5a8059ff3ea30f00400708902694af697954b889bcdf1`
- Pre-split freeze commit: `611a835af50d66ff1860c1497c16a19713cd5042`
- Pre-split methods / executed cases: `452` / `415`
- Assertions: `452`; validation additions: `7`; portable: `383`; module-internal: `76`
- Assertion inventory SHA-256: `0dc9e3d73bcf571c288938c0a1757f7574be013f4607a3bfd41f2e6aaeb2e526`
- Strength inventory SHA-256: `0dfedef6baded48ccf01901c8a774df402b82e3fbc9aadbdf7781f41687737e9`
- Approval: `QO-9.1-TIERING-2026-10-07-R2` by Jerome Piquot; membership `37a40bfc94151a6bb86356d311ad4e10918e0a53881f20d93a642bec6be035de`

| Binding | Path | SHA-256 |
| --- | --- | --- |
| Contract | `_bmad-output/planning-artifacts/v9/story-contracts/9.1.json` | `7a395d8b868f6bd5313083c88fccd2ce04628b3da8bf3c62a00a2bcc40e705f8` |
| Decision | `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` | `1fe609f0114d52622b01b5cbaa74df799c09690f33804921961cad7a725dbcd3` |
| Pre-split result | `artifacts/v9/9.1/pre-split/conformance.trx` | `b2e23d8f5f3f57f95afbf331cbd535e9d670d4facfab3b43a3774930f06f9edd` |
| Pre-split receipt | `artifacts/v9/9.1/pre-split/receipt.json` | `b18e21126acea4c8b52e9b78a5b4579336ec7e3b8d2885992956b3c58240025d` |
| Approvals | `docs/release-evidence/conformance-oracle-tiering-approvals-v2.json` | `314d2341525fe9e8cae4fd9c66ad453d78597c39819bfd04e8e4a0f5b7c51700` |
| Fault evidence | `artifacts/v9/9.1/faults.xml` | `d4d066362b6bda392267187c39b0131789977d798c0e369460af3f29a098df25` |
| Test assembly | `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests.dll` | `5425ea28dbeed7078abeca1b0100c928347f895d851372c3c232c69016c5a898` |
| `schema` | `docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json` | `c74af9cbd5a5224504105939e8aa608ae93a18803bc0d3ef19e818691e03a22a` |
| `json` | `docs/release-evidence/conformance-oracle-tiering-disposition-v2.json` | `1203fa7b216d4f4314fcdd28bfd0d8021d1a303abe6d66afb1d49a373ad12273` |
| `markdown` | `docs/release-evidence/conformance-oracle-tiering-disposition-v2.md` | `f782688765ff79ce2e20b067c1fcbf88857f0a49d8a737970a24436d54771d94` |

## Fault injection

| Fault | Expected blocker | Observed exit | Observed blockers | Before SHA-256 | After SHA-256 |
| --- | --- | --- | --- | --- | --- |
| `assertion-missing` | `CONFORMANCE_ASSERTION_MISSING` | `1` | `CONFORMANCE_ASSERTION_MISSING` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `assertion-duplicate` | `CONFORMANCE_ASSERTION_DUPLICATE` | `1` | `CONFORMANCE_ASSERTION_DUPLICATE` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `assertion-renamed` | `CONFORMANCE_ASSERTION_RENAMED` | `1` | `CONFORMANCE_ASSERTION_RENAMED` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `strength-weakened` | `ASSERTION_STRENGTH_WEAKENED` | `1` | `ASSERTION_STRENGTH_WEAKENED` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `tier-unassigned` | `TIER_UNASSIGNED` | `1` | `TIER_UNASSIGNED` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `tier-reason-missing` | `TIER_REASON_MISSING` | `1` | `TIER_REASON_MISSING` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `approval-missing` | `TIER_APPROVAL_MISSING` | `1` | `TIER_APPROVAL_MISSING` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `denominator-drift` | `FR20_DENOMINATOR_DRIFT` | `1` | `FR20_DENOMINATOR_DRIFT` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `public-widened` | `PUBLIC_CONTRACT_WIDENED` | `1` | `PUBLIC_CONTRACT_WIDENED` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |
| `v1-mutated` | `V1_ARTIFACT_DRIFT` | `1` | `V1_ARTIFACT_DRIFT` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` | `152c5dcb9bd8debee54c184ca2190d67809a570b606e78f7f17c78f451731a80` |

## Outputs

| Output | Path |
| --- | --- |
| JSON | `docs/release-evidence/story-9.1-final-record-v2.json` |
| Markdown | `docs/release-evidence/story-9.1-final-record-v2.md` |

## Rollback boundary

remove only Story 9.1 generator, new v2 disposition, fixtures/results, and final record; preserve v1 artifacts, public-contract baselines, existing tests, and Story 6.9 partial work.

## Summary

| Required | Passed | Failed | Blocked | Skipped | Not run |
| --- | --- | --- | --- | --- | --- |
| `9` | `9` | `0` | `0` | `0` | `0` |
<!-- STORY-FINAL-RECORD:END -->
