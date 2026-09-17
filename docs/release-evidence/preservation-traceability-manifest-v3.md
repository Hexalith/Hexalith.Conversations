# Preservation Traceability Manifest v3 — Owner Review

- Version: `3.0.0-rc.1`
- Status: `pending-owner-approval`
- Canonical JSON SHA-256: `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc`
- Approval, approver, signature, waiver, and ownership record: `null`

Owner approval is requested for manifest `3.0.0-rc.1` bound to canonical JSON SHA-256 `a85f6b6c544790a21523f9f37c3b5f1cbb0586ba3f79fb58b80516207d57f3fc`. This draft records no approval and remains `pending-owner-approval` until approval is captured in a required hash-bound authority record.

## Bound source

### Committed remediation

- Remediation commit: `d956c9b1de73bcf15969d5e1a6435d6d98a2dd49`
- Tree: `85efb51da9e790550f0991c5c59312ae86dcc838`
- Parent: `c0abd5cb73d420bb2f4b5d04827461ad82c4528c`
- Committed changed paths: `50`

| Root submodule | Gitlink |
|---|---|
| `references/Hexalith.AI.Tools` | `5f93d2ec8239494852c97032c819cb1689939e36` |
| `references/Hexalith.Builds` | `04d961759994396132bb2b113ee465b64740a543` |
| `references/Hexalith.Commons` | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` |
| `references/Hexalith.EventStore` | `629168e3983e5a9cd1639013f39d758fb0068cac` |
| `references/Hexalith.Folders` | `6650dce38ec2a8f4e5f08905979e6c3b50a5ce79` |
| `references/Hexalith.FrontComposer` | `f20a1fc73c6c9be184b6a10949b54799e978e3cb` |
| `references/Hexalith.Memories` | `5829db422522b79c0d164df85adde53a996ba114` |
| `references/Hexalith.Parties` | `14d249fde316b0002aec84351d7a7cdf953d1d30` |
| `references/Hexalith.Projects` | `4f05a352edd67c4d5595913ee584539c1948dd58` |
| `references/Hexalith.Tenants` | `c150d5b1af4f911ff3b2a7ab7901d94838be09d7` |

### Uncommitted Owner-review overlay

- Base commit: `d956c9b1de73bcf15969d5e1a6435d6d98a2dd49`
- Committed: `false`
- Clean-HEAD evidence: Release solution build passed; Conformance failed `456/472`.
- Overlay evidence: focused Release build passed; Conformance failed `456/473`.

| Overlay file | SHA-256 |
|---|---|
| `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs` | `3a80050facaa77a26d707f40bf02c4788e9a21bd11d8f104a9bc63950bdbe505` |
| `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md` | `864c035ebf29a0b4aa129922dbbb39b54f6a39b9bfe7950e2b12e7a24f0244cc` |
| `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md` | `8c2e635a682b1088755905c8a7532092e9cfa37b514a9f85ee3777eae4ae45fe` |

## Gate state

- FR-20: `PENDING`
- SM-C1: `PENDING`
- SM-C2: `FAILED` under the universal every-path p95 regression rule of `<=5%`
- OQ-1: `BLOCKED` — Recorded Owner authority does not close the current-gitlink runtime proof, broad Conformance, ineffective-grant, unclaimed package-publication, conditional rollback, and pending exact successor-manifest approval blockers.
- Implementation hold: `ACTIVE`

## Evidence limitations

- This manifest and its Owner-review overlay are uncommitted; no commit, approval, ownership, waiver, or signature is inferred.
- The historical runtime boundary record remains useful evidence but binds root base c0abd5cb and EventStore gitlink 27cc17f rather than the final d956c9b source and 629168e gitlink.
- The fresh clean-HEAD run failed 16 of 472 tests; the Owner-review overlay run failed 17 of 473 tests, so 100% conformance is not established.
- The approved 384 display-ID set is reconstructed from decision-bound source because no tracked historical XML enumerates every display name; stable method and ordered-argument identity is authoritative.
- Historical runtime evidence reports status=working-tree-validation-only and rootWorkingTreeCommitted=False; it is not promoted to final-HEAD proof.

## Additive test denominator

| Boundary | Count | Exact-ID SHA-256 | State |
|---|---:|---|---|
| Original v1 floor | 214 across 14 suites | `31a3a21b58aa3d704458a242adf87a41b95cc69b5f9b2b38994354735d3fb4bc` | immutable |
| Later approved additions | 170 | `93632988fa4c409847cd0976e11d5deea8c5a7cef01eaea9e8889e5bcbb01180` | approved only through bound signed decision |
| Approved cumulative floor | 384 | `ec0d374dc6d947c662165de79eae9911acaa60bfaf4735e314923bbd9f0e6c61` | immutable |
| Current candidate | 473 | `282fb65c9637e6688fd8bb9a22fe228f6546c74b56c1d8fc387abae10291a332` | pending Owner approval; 17 failing |
| Pending additions beyond approved floor | 89 | embedded in canonical JSON | pending |

No removal, replacement, merging, reclassification, waiver, substitution, or denominator shrinkage may establish acceptance.

## Seven-category mapping summary

| Category | Requirement IDs | Exact test count | Approval |
|---|---|---:|---|
| `tenant-isolation` | `FR-20`, `Feature-FR87`, `Feature-NFR16`, `Feature-NFR17` | 35 | `pending-owner-approval` |
| `idempotency` | `FR-20`, `Feature-FR88`, `Feature-NFR22`, `Feature-NFR23` | 22 | `pending-owner-approval` |
| `contract-validation` | `FR-20`, `Feature-FR92`, `Feature-NFR49`, `Feature-NFR53` | 22 | `pending-owner-approval` |
| `redaction-replay` | `FR-20`, `Feature-FR89`, `Feature-NFR21` | 30 | `pending-owner-approval` |
| `provider-portability` | `FR-20`, `Feature-FR90`, `Feature-NFR50`, `Feature-NFR51`, `Feature-NFR52` | 15 | `pending-owner-approval` |
| `projection-freshness` | `FR-20`, `Feature-FR34`, `Feature-FR36`, `Feature-FR37`, `Feature-NFR44`, `Feature-NFR45`, `Feature-NFR46`, `Feature-NFR47`, `Feature-NFR48` | 16 | `pending-owner-approval` |
| `governance-audit-pairing` | `FR-20`, `Feature-FR47`, `Feature-FR48`, `Feature-FR49`, `Feature-NFR20` | 56 | `pending-owner-approval` |

## Preservation obligations and zero-orphan proof

The manifest inventories **969 required preservation obligations**. Exactly **19 initiative requirements** are active initiative scope; deferred `FR-16` is explicitly `deferred-not-active`; and **0 legacy requirements** are release-activated. **285 legacy/UX obligations** remain `pending-not-inferred`, and activation is not applicable to **664 supporting contract, client, control, and assertion rows**.

Every row has a computed closure: **969/969**. Of these, **277** point to explicit pending governed dispositions with no activation effect. Orphans: **0**. Duplicate IDs: **0**. Missing mapped tests or requirements: **0**.

This proves inventory and traceability completeness only. It does not establish release activation, conformance pass, approval, waiver, or acceptance.

| Kind | Expected | Actual | Orphans |
|---|---:|---:|---:|
| `initiative-fr` | 20 | 20 | 0 |
| `feature-fr` | 104 | 104 | 0 |
| `feature-nfr` | 77 | 77 | 0 |
| `ux-decision` | 52 | 52 | 0 |
| `ux-acceptance` | 52 | 52 | 0 |
| `public-contract` | 196 | 196 | 0 |
| `public-client` | 7 | 7 | 0 |
| `current-control` | 15 | 15 | 0 |
| `conformance-assertion` | 446 | 446 | 0 |

The canonical JSON contains all 969 obligation rows, 277 pending disposition records, exact source bindings, exact denominator IDs, commands, build identities, artifact hashes, and proof lists.

## Reference appendix — exact seven-category mappings

### tenant-isolation

Requirements: `FR-20`, `Feature-FR87`, `Feature-NFR16`, `Feature-NFR17`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.TenantBindingCheckShouldExerciseCrossTenantHiddenSideChannelShape`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveGuardShouldNotRunProtectedOperationWhenLiveServiceDenies`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldAllowAuthorizedOwner`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldDenyContradictoryTenantBindings`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldDenyCrossTenantMemberLeakage`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"ambiguous\", expectedReason: TenantProjectionPoisoned)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"disabled\", expectedReason: TenantDisabled)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"gap\", expectedReason: TenantAccessGapDetected)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"insufficient\", expectedReason: InsufficientRole)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"malformed-projection\", expectedReason: MalformedProjection)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"member-poisoned\", expectedReason: TenantProjectionPoisoned)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"rollback\", expectedReason: TenantAccessRolledBack)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"stale\", expectedReason: TenantAccessStale)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"unavailable\", expectedReason: TenantAccessUnavailable)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"unknown\", expectedReason: UnknownTenant)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"unmapped-role\", expectedReason: UnmappedRole)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedOnEveryReleaseGateTriggerState(trigger: \"unmapped-status\", expectedReason: UnmappedStatus)`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenCallerPrincipalIsMalformed`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenTenantBindingIsMissing`
- `Hexalith.Conversations.Conformance.Tests.LiveTenantFailClosedOracleCharacterizationTest.LiveServiceShouldFailClosedWhenTenantIdIsMalformed`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.AllChecksShouldCarryFR87RequirementAndTenantIsolationGateMappings`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.AllChecksShouldUseTenantBindingCheckId`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.RunResultShouldHaveExactly12Checks`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.TenantIsolationConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError`

### idempotency

Requirements: `FR-20`, `Feature-FR88`, `Feature-NFR22`, `Feature-NFR23`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.IdempotencyCheckShouldSurfaceNonRetryableConflictAsBlocked`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.AllChecksShouldCarryFR88RequirementAndIdempotencyMappings`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.AllChecksShouldUseIdempotencyCheckId`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.RunResultShouldHaveExactly8Checks`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.IdempotencyConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError`
- `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorReplayPayloadShouldExcludeCallerSuppliedSecrets`
- `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorShouldPreserveOriginalReasonCodeOnDuplicateRejectionReplay`
- `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorShouldRejectConflictingKeyReuseWithoutMutation`
- `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyConflictOracleCharacterizationTest.LiveExecutorShouldReturnRetryableUncertaintyForPendingKeyWithoutMutation`
- `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyOracleCharacterizationTest.LiveExecutorShouldInvokeMutationOnceForFirstSubmission`
- `Hexalith.Conversations.Conformance.Tests.LiveIdempotencyOracleCharacterizationTest.LiveExecutorShouldReplayDuplicateWithoutReinvokingMutation`

### contract-validation

Requirements: `FR-20`, `Feature-FR92`, `Feature-NFR49`, `Feature-NFR53`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.CompatibilityDiscoveryCheckShouldSurfaceUnsupportedAsBlockedTypedError`
- `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.ErrorEnvelopeCheckShouldReuseSharedTypedErrorCatalog`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.AllChecksShouldCarryFR92RequirementAndContractCompatibilityGateMappings`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.AllChecksShouldUseCompatibilityDiscoveryCheckId`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.RunResultShouldHaveExactly10Checks`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.ContractValidationConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError`
- `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting`
- `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.GenerateAndSaveContractShapeSnapshotFile`
- `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldBeContentSafe`
- `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCaptureExportedPublicTypesDeterministically`
- `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCoverAllSixReleaseGateBehaviorAreas`

### redaction-replay

Requirements: `FR-20`, `Feature-FR89`, `Feature-NFR21`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.RedactedMessageShouldStaySuppressedThroughPublicReadSurface`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.AllChecksShouldCarryFR89RequirementAndRedactionGateMappings`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.RunResultShouldHaveExactly10Checks`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.RedactionConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.BooleanDimensionsShouldOnlyCarryBoundedTrueOrFalseTokens`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.ClassDimensionsShouldOnlyCarryClosedVocabularyTokens`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.EveryMeasurementShouldCarryOnlyApprovedDimensionKeys`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.EveryTelemetrySurfaceShouldRejectTheSentinelNoneValue`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.FixtureForbiddenValuesShouldCoverEveryRequiredDisclosureClass`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.GateIdDimensionShouldOnlyCarryApprovedBoundedGateIds`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.GateIdShouldBeTheOnlyStringDimensionOutsideTheClassAndBooleanVocabularies`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoMeasurementDimensionShouldCarryRawIdentifierShapes`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoMeasurementDimensionShouldEverCarryAForbiddenValue`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoStructuredLogMessageShouldEverCarryAForbiddenValue`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.NoneSentinelGuardShouldPreventEmissionOfANoneDimensionValue`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.RunShouldEmitAtLeastOneMeasurementPerCounter`
- `Hexalith.Conversations.Conformance.Tests.TelemetryRedactionConformanceSuiteTest.StructuredLogMessagesShouldNotCarryTenantOrPartyOrConversationIdShapes`

### provider-portability

Requirements: `FR-20`, `Feature-FR90`, `Feature-NFR50`, `Feature-NFR51`, `Feature-NFR52`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.AllChecksShouldCarryFR90RequirementAndPortabilityGateMappings`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.AllChecksShouldUseEventPublicationCheckId`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.AllConformantScenariosProduceOverallReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.BlockedScenariosShouldHaveNonNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.EachScenarioCheckShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.EachScenarioShouldProduceExpectedConformanceOutcome`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.ReadyScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.RunResultShouldHaveExactly10Checks`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.ProviderPortabilityConformanceSuiteTest.UnknownScenariosShouldCarryAggregateNotFoundTypedError`

### projection-freshness

Requirements: `FR-20`, `Feature-FR34`, `Feature-FR36`, `Feature-FR37`, `Feature-NFR44`, `Feature-NFR45`, `Feature-NFR46`, `Feature-NFR47`, `Feature-NFR48`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.AdopterConformanceSuiteTest.ProjectionFreshnessCheckShouldSurfaceStaleAsDegradedNonTrustBearing`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: \"gap\")`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: \"rebuilding\")`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: \"stale\")`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.DegradedProjectionShouldNotExposeTrustBearingDetail(degradedState: \"unavailable\")`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.GovernanceEvidenceShouldBeAnchoredThroughPublicReadSurface`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.PoisonProjectionShouldNotExposeDetailThroughPublicReadSurface`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.RedactedMessageShouldStaySuppressedThroughPublicReadSurface`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveFreshnessClassifierShouldNeverPromoteDegradedStatesToCurrent`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldReportCurrentProjectionAsTrustBearing`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceActiveRebuildAsNonTrustBearing`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceGapAsRebuildingNonTrustBearing`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceMetadataWriteFailureAsUnavailable`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldSurfaceStaleProjectionAsNonTrustBearing`

### governance-audit-pairing

Requirements: `FR-20`, `Feature-FR47`, `Feature-FR48`, `Feature-FR49`, `Feature-NFR20`

Exact fully qualified test IDs:

- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllChecksShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllChecksShouldCarryFR102RequirementAndBuyerAcceptanceMappings`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.AllPassScenariosShouldProduceReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.ExpiredItemShouldProduceConformantResult`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.MissingAckShouldProduceConformantResult`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.PassScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.RunResultShouldHaveExactly10Checks`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.BuyerAcceptanceConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.ConversationProjectionReadSurfaceConformanceTest.GovernanceEvidenceShouldBeAnchoredThroughPublicReadSurface`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.EveryGovernanceMutationShouldPairItsEventWithAuditEvidence`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.FutureGovernanceVocabularyShouldNotAppearAsImplementedMutationPaths`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.GovernanceMutationWithMismatchedAuditEvidenceShouldFailClosed`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.GovernanceMutationWithMissingAuditEvidenceShouldFailClosed`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.NonGovernanceCommandsShouldEmitEventsWithoutAuditEvidenceDependency`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditSinkFailClosedConformanceTest.GovernedMutationShouldEmitEventWhenAuditSinkHealthy`
- `Hexalith.Conversations.Conformance.Tests.GovernanceAuditSinkFailClosedConformanceTest.GovernedMutationShouldFailClosedWhenAuditSinkThrows`
- `Hexalith.Conversations.Conformance.Tests.LiveProjectionFreshnessOracleCharacterizationTest.LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence`
- `Hexalith.Conversations.Conformance.Tests.ReleaseConformanceArtifactGenerationTest.AuditIntegrityGateShouldBePassWhenGovernancePreconditionIsReady`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllChecksShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllChecksShouldCarryFR100RequirementAndReleaseScopeMappings`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.AllPassScenariosShouldProduceReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.DeferredNoAreasShouldProduceConformantResult`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.PassScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.RunResultShouldHaveExactly10Checks`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`
- `Hexalith.Conversations.Conformance.Tests.ReleaseScopeConformanceSuiteTest.WaivedNoRefShouldProduceConformantResult`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllChecksShouldBeClassifiedAsConformant`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllChecksShouldCarryFR103RequirementAndSecondAdopterMappings`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllChecksShouldUseGovernancePreconditionCheckId`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.AllPassScenariosShouldProduceReadyOutcome`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.EmptyScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.MilestoneOverdueShouldProduceConformantResult`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.NullCorrelationIdShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.NullScenariosListShouldThrow`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.PassScenariosShouldHaveNullTypedError`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RevertedNoRationaleShouldProduceConformantResult`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RunResultShouldHaveExactly10Checks`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip`
- `Hexalith.Conversations.Conformance.Tests.SecondAdopterConformanceSuiteTest.SuiteIdAndRunnerIdShouldMatchSpecifiedValues`

