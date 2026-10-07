---
title: 'Freeze the conformance assertion inventory, tier decisions, digest, and approvals'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
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
- [ ] `docs/release-evidence/story-9.1-final-record-v2.{json,md}` — generate and verify insertion; update this spec and `sprint-status.yaml` only after completion gates pass.

**Acceptance Criteria:**
- Given approved bound inputs, when AC-9.1-01 runs, then the required disposition schema and deterministic pair verify.
- Given frozen source/result identities, when AC-9.1-02 through -04 run, then every assertion occurs once with exact justified tier and unchanged strength.
- Given denominator, approvals, public baseline, and protected evidence, when AC-9.1-05 through -08 run, then membership, genuine approvals, public shape, and v1 bytes verify.
- Given passing current scenario and fault receipts, when AC-9.1-09 runs, then the derived final record binds all required facts and reports `9/9/0/0/0/0`.

## Implementation Notes

**KEEP across review derivation:** the isolated pre-split capture at freeze commit `611a835af50d66ff1860c1497c16a19713cd5042`; the Quality-owner decision `QO-9.1-TIERING-2026-10-07` for membership digest `13c4e3ea0dffc562db0a6e0d7c7de62e9be766f35c431db16f6145984fe99c7d`; the source-closure strength definition; the IL lower-bound cross-check; and the unchanged decision and v1 bytes. Any change to a frozen test source, a validation addition, the pre-split result, or the decision changes the membership digest and needs a new owner decision.

- **Freeze and capture.** `--capture-pre-split` cloned the freeze commit into `/var/tmp/hexalith-9.1-pre-split-5c2sa9e6` with the root submodules only, read the `ci / conformance` lane from that commit's `ci.yml`, and built Release with `CI=true` (`SourceRevisionId` equals the freeze commit). Discovery found 452 methods. The lane executed 415 cases (401 methods, two theories with 16 cases): 412 passed and 3 failed. The 3 failures are recorded, not claimed: `GovernanceAuditPairingSafetyNetConformanceTest.AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory`, `PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting`, and `ReleaseBaselineValidationTest.BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface`. GitHub CI at the same commit also failed `SuccessMetricReportAndAttestationValidationTest.SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity`, because the snapshot generator rewrote the protected baseline earlier in that run order. The 51 CI-excluded methods keep their discovered identities with no pass claim. In the clone, the four generation methods rewrote only `public-contract-shape-baseline-v1.json`; the real working tree was not touched.
- **Strength and bindings.** A deterministic C# source model derives each test's closure (declaration, declaring-class constructors, fields, and initialized properties, named members, and named or extension-providing project types, whole and transitive). The canonical triple is the bound first-party module assemblies, the SHA-256 behavior identity of the closure text, and the negative-site count. Unsupported constructs fail with `CONFORMANCE_DISCOVERY_UNSUPPORTED`. The C# `ConformanceTieringIlReferenceReader` independently walks the compiled IL; its Server-bound set equals the source-derived module-internal set (83 tests).
- **Tiers.** Rows binding `Hexalith.Conversations.Server` are module-internal with their exact Server types and reason; all others are portable with an unchanged-assertion replacement at equal strength. No public re-expression was attempted, so no strength changed. Result: 376 portable and 83 module-internal (76 frozen rows in nine classes, plus the seven validation additions). Every frozen class is single-tier. The validation class anchors the Server assembly in a static field so the whole class stays module-internal, matching Story 9.2's selectors. The three reclassified suites keep their exact v1-floor membership and are module-internal. All 13 files of the residual coupling inventory close only over module-internal rows.
- **AC-9.1-07 reading (accepted by the owner on 2026-10-07).** Story 9.1 compares the candidate's public project sources with the frozen pre-story surface. The earlier post-PC Agents growth of the public surface is recorded as `preExistingDrift` with `approvalClaimed: false`; it is not approved here.
- **Approvals.** The proposal binds a per-row digest for every row and suite. The decision-v2 suite approval of 2026-07-28 stays separate as `historicalSuiteApproval` with `approvesRows: false`.
- **Out of scope, pre-existing.** At the untouched `HEAD`, nine Story 8.2 tests in `test_generate_story_record.py` fail because root gitlinks moved after Story 8.2's scope start. The directory lane result is 804 passed and these 9 failed, the same with and without this story.
- **Lifecycle bookkeeping.** The candidate restores `sprint-status.yaml`'s `last_updated` to `YYYY-MM-DD`, the form the retention checker parses, so the post-record status transition stays a lifecycle-only change.

## Spec Change Log

## Review Triage Log

## Verification

- Run the exact nine commands in `9.1.json`; preserve declared result paths and measured summaries.
- Run focused tiering/record pytest suites and direct xUnit class checks. Use Debug locally; retain distinct Release evidence for frozen acceptance commands.
- Verify determinism, fault restoration, protected inputs, and `git diff --check`.
