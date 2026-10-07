# Story 9.2 migration: Quality review packet

**Approved on 2026-10-07.** The user replied “I authorize and approve” to this concrete Quality packet and the independent candidate-scope packet. The [actual Quality decision](conformance-oracle-tiering-migration-approval-v3.json) records approver `user` and binds the proposal, all 14 successor rows, and public drift below. Fresh committed-candidate acceptance and final publication remain required.

- Proposal SHA-256: `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`
- Public-drift SHA-256: `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`
- Authoritative proposal: [conformance-oracle-tiering-migration-v3.json](conformance-oracle-tiering-migration-v3.json).
- Original story baseline: `51aa06b856bcb0aaf22013153cfe02a51b046156`. Final acceptance requires a compatible committed candidate, a genuine Quality decision, and fresh receipts.

The shared catalog now selects EventStore family `3.115.0`. The update changed only `portableSurface` before approval; assertion rows, both strength inventories, and the public-drift digest remained identical. The previous pending proposal (`f59dce5e7642c7ef588dcb0f3e4f7fe045598ee613c9638b3198a5e57f331eef`) and review packet are retained under [the update receipts](../../artifacts/v9/9.2/eventstore-update/). No approved migration or historical evidence was rewritten.

## Approved decision

The decision approves the 14 successor assertion rows and the exact pre-existing public-surface drift below. The split keeps the historical definitions and exclusions, preserves the 415 active case identities, and counts the three live controls separately. The current-surface guards continue to compare exact expectations; the snapshot round-trip uses a temporary file.

The original strength hashes and signed evidence remain retained. Changed hashes are evidence of changed guard behavior; matching assertion counts alone does not establish equal semantic strength. The recorded decision covers the three guard changes and their exact successor digests below.

## Guard behavior to review

| Guard | Successor behavior | Tradeoff |
| --- | --- | --- |
| Governance command inventory | Explicit audit and non-audit command sets include the existing agent, message-edit/delete, and deletion commands. Exact set equality remains required. | The expectations now describe the current implementation and need Quality review. They are not inferred from the reflected result. |
| Full public-contract snapshot | Compare the complete live shape with an additional current snapshot. Generate and round-trip through a unique temporary file with cleanup. | Current acceptance uses the successor snapshot; the original signed v1 snapshot remains historical evidence. |
| Release baseline and suite inventory | Validate the v1 reported count against v1 bytes, validate the successor count against the live surface, and read suite names from both assemblies’ metadata. | Current and historical evidence have distinct roles. Metadata inspection avoids loading Server runtime types into the portable assembly. |

The 14 changed strength rows comprise one governance row, five snapshot rows, and eight release-baseline rows. Class fields and helper closures account for changes to rows whose method bodies were not otherwise edited. All retained rows preserve their bound assembly sets and retain or increase assertion-site and negative-case counts.

## Public-surface drift

The retained v1 snapshot contains 196 types; the current snapshot contains 219. There are 23 added types, 0 removed types, and 4 changed existing types. These API changes predate Story 9.2; this implementation changes test/evidence code.

**No removed types does not mean no removed members.** Three records have replacement constructor and `Deconstruct` signatures.

| Existing type | Member changes |
| --- | --- |
| `Hexalith.Conversations.Contracts.Commands.AddParticipantCommand` | constructor: `(Metadata, ConversationId, ParticipantPartyId, ParticipantType, ParticipantRole, ProviderCorrelation)` replaced by `(Metadata, ConversationId, ParticipantPartyId, ParticipantType, ParticipantRole, ProviderCorrelation, OperationTimestamp)`; Deconstruct: `(Metadata, ConversationId, ParticipantPartyId, ParticipantType, ParticipantRole, ProviderCorrelation)` replaced by `(Metadata, ConversationId, ParticipantPartyId, ParticipantType, ParticipantRole, ProviderCorrelation, OperationTimestamp)`; Added properties: `OperationTimestamp` |
| `Hexalith.Conversations.Contracts.Commands.AppendMessageCommand` | constructor: `(Metadata, ConversationId, MessageId, AuthorPartyId, Text, ProviderCorrelation, CallerMetadata)` replaced by `(Metadata, ConversationId, MessageId, AuthorPartyId, Text, ProviderCorrelation, CallerMetadata, AgentProvenance, OperationTimestamp)`; Deconstruct: `(Metadata, ConversationId, MessageId, AuthorPartyId, Text, ProviderCorrelation, CallerMetadata)` replaced by `(Metadata, ConversationId, MessageId, AuthorPartyId, Text, ProviderCorrelation, CallerMetadata, AgentProvenance, OperationTimestamp)`; Added properties: `AgentProvenance`, `OperationTimestamp` |
| `Hexalith.Conversations.Contracts.Events.ConversationEventType` | Added properties: `AgentParticipantRemoved`, `ConversationDeletionApproved`, `ConversationDeletionDeliveryRecorded`, `MessageDeleted`, `MessageEdited` |
| `Hexalith.Conversations.Contracts.Events.MessageAppended` | constructor: `(Metadata, MessageId, AuthorPartyId, Text, ProviderCorrelation)` replaced by `(Metadata, MessageId, AuthorPartyId, Text, ProviderCorrelation, AgentProvenance, IdempotencyKey)`; Deconstruct: `(Metadata, MessageId, AuthorPartyId, Text, ProviderCorrelation)` replaced by `(Metadata, MessageId, AuthorPartyId, Text, ProviderCorrelation, AgentProvenance, IdempotencyKey)`; Added properties: `AgentProvenance`, `IdempotencyKey` |

Added type identities:

- `Hexalith.Conversations.Contracts.Agents.AgentMessageProvenance`
- `Hexalith.Conversations.Contracts.Agents.ApproveConversationDeletionCommand`
- `Hexalith.Conversations.Contracts.Agents.ConversationActiveCountQuery`
- `Hexalith.Conversations.Contracts.Agents.ConversationActiveCountResult`
- `Hexalith.Conversations.Contracts.Agents.ConversationAgentCommandResult`
- `Hexalith.Conversations.Contracts.Agents.ConversationAgentMessage`
- `Hexalith.Conversations.Contracts.Agents.ConversationAgentReadQuery`
- `Hexalith.Conversations.Contracts.Agents.ConversationAgentReadResult`
- `Hexalith.Conversations.Contracts.Agents.ConversationAgentsOutcome`
- `Hexalith.Conversations.Contracts.Agents.ConversationDeletionAcknowledgement`
- `Hexalith.Conversations.Contracts.Agents.ConversationDeletionDeliveryAction`
- `Hexalith.Conversations.Contracts.Agents.ConversationDeletionSignal`
- `Hexalith.Conversations.Contracts.Agents.ConversationDeletionSourceQuery`
- `Hexalith.Conversations.Contracts.Agents.ConversationDeletionSourceResult`
- `Hexalith.Conversations.Contracts.Agents.DeleteConversationMessageCommand`
- `Hexalith.Conversations.Contracts.Agents.EditConversationMessageCommand`
- `Hexalith.Conversations.Contracts.Agents.RecordConversationDeletionDeliveryCommand`
- `Hexalith.Conversations.Contracts.Agents.RemoveAgentParticipantCommand`
- `Hexalith.Conversations.Contracts.Events.AgentParticipantRemoved`
- `Hexalith.Conversations.Contracts.Events.ConversationDeletionApproved`
- `Hexalith.Conversations.Contracts.Events.ConversationDeletionDeliveryRecorded`
- `Hexalith.Conversations.Contracts.Events.MessageDeleted`
- `Hexalith.Conversations.Contracts.Events.MessageEdited`

## Pre-approval measured verification

| Check | Observed result |
| --- | --- |
| Both tier projects, Release with EventStore 3.115.0 | Separate Release restores and builds pass with zero warnings/errors. |
| Portable dependency boundary and declarations | AC02/05 pass; resolved identities, graph packability, compile assets, solution, and CI declarations are checked. |
| Portable execution | 326/326 pass: 325 retained cases plus one live control. |
| Internal execution | 91/92 pass: all 90 retained cases plus the declaration control pass; the inventory approval control reports `TIER_APPROVAL_MISSING`. |
| Combined retained execution | All 415 retained cases pass across 401 active methods; zero skips/omissions. The earlier 415-case capture had 412 passes and three guard failures. |
| Structural/execution faults | 22 pass; each reports its exact blocker and restores identical bytes. Synthetic fixture execution/approval is labeled and excluded from actual acceptance. |
| Generator tooling | 62 focused Story 9.1/9.2 tests pass, including positive derivation, malformed/stale inputs, approval, schema, historical pairs, and inserted-record drift. |
| Shared catalog and audit | Catalog, authority, and exception checks pass; 115 audit-generator scenarios pass. The deterministic audit validates 304 packages; all 145 other family decisions and 291 other package rows are unchanged. |

Release build and tier-execution receipts for EventStore `3.115.0` are under `artifacts/v9/9.2/eventstore-update/`; the earlier Debug, fault, and generator receipts remain under `artifacts/v9/9.2/`. That verifier observed all 415 retained cases and three live controls, with only `TIER_APPROVAL_MISSING` failing. These provisional measurements predate the actual decisions now recorded. No accepted Story 9.2 final record has been published; all effective acceptance scenarios must pass again at the committed implementation candidate.

### Pre-approval committed checkout verification

The 2026-10-07 verification at committed checkout `d54cba773290b555e1939e9d943ae84548e5eba3` reproduces the exact proposal and row bindings above. Both separate Debug and Release builds pass with zero warnings/errors; Debug and Release dependency/declaration controls pass. Both Release assemblies carry this full candidate identity. Fresh portable execution passes 326/326 and internal execution passes 91/92, with only the Quality inventory control failing. All 415 retained cases pass across 401 active methods; all three live controls execute; zero cases are skipped or omitted. The [current verifier receipt](../../artifacts/v9/9.2/current-candidate/AC-9.2-08.json) reports `TIER_APPROVAL_MISSING`.

The 22 structural/execution faults pass with exact blockers and byte-identical restoration; their exported properties bind this candidate and the measured source digest. The focused Story 9.1/9.2 final-record tooling checks pass 62/62 and the source-model checks pass 11/11. A broader source-model selector also attempted historical Story 9.1 freeze generation against the split checkout: five tests fail with `CONFORMANCE_ASSERTION_UNKNOWN`. That retired reproduction lane and its [failed receipt](../../artifacts/v9/9.2/current-candidate/source-model-tests-broad.log) are preserved; current acceptance and historical expectations are unchanged. [Command receipts](../../artifacts/v9/9.2/current-candidate/verification-receipts.json) record the exact executed commands; only artifact destinations differ from the effective acceptance commands to retain earlier receipts.

Before authorization, the final-record API independently rejected seven already landed root gitlink changes with `AUTHORITY_BINDING_INVALID`. The [scope decision packet](../../artifacts/v9/9.2/current-candidate/candidate-scope-review-v2.md) binds their exact before/after identities and five owning promotion commits. The user authorized its exact proposal `a5c12ba32a82338ce8bcb791f8b64355e5a820d7433145e0648d107441bc3d20`, now embedded unchanged in the [closed environment amendment](../../_bmad-output/planning-artifacts/v9/story-9.2-candidate-environment-amendment-v1.json). Story 9.2 validation hash-pins that amendment, preserves the original baseline, and measures the seven gitlinks plus every owning promotion commit/parent/diff. Every extra promotion, even if reverted, and every unrelated protected-path change remain forbidden. This environment authorization and the separately recorded Quality approval supply the two actual decisions; they do not claim fresh acceptance, a published final pair, or completion.

## Exact changed-row bindings

Every identity below retains its original before-strength hash in the authoritative proposal. The recorded decision binds all row digests in this order.

| Assertion identity | Assertion sites before → after | Row SHA-256 |
| --- | --- | --- |
| `Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory` | 2 → 2 | `c04079b647f59c5f26e5e738b4c600e731d995e2a6b7a659777c964c848faffd` |
| `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting` | 6 → 6 | `d2ff5d529caa31961ed04ef54ad40deea5b666edb0355640671eb9daf19579bf` |
| `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.GenerateAndSaveContractShapeSnapshotFile` | 5 → 5 | `540f4f7071decfc6d26f91fcfa417d4a83417100cd0d4d49d5640ee18c8f650b` |
| `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldBeContentSafe` | 6 → 6 | `0e6408fb2708ed2d466023176fbe62cf0c1330f70770429ba6715b1bddbf60b8` |
| `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCaptureExportedPublicTypesDeterministically` | 7 → 7 | `160bda151f562ff610d8c7a987d33d09549e64f3d6037f4ad4c0b54d661e1725` |
| `Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCoverAllSixReleaseGateBehaviorAreas` | 7 → 7 | `311a3e56c2fbc835f15d95784bb3631db8e23122ad95f862f03a56347b5504e5` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineCommitShouldBeAFullFortyCharacterHexShaOnMain` | 5 → 5 | `fabf5413c654c5b364de99cb295aa291044d07868559e232025d3a8cfc93070f` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineEnumeratedSuiteClassesShouldMatchTheActualSuiteClassesInTheAssembly` | 6 → 7 | `172dce81104ef4d61da501780a30a5e27514a8ccce9d215a019c74e43c9f186f` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface` | 6 → 7 | `e1bd1dbe2c65488a73b5b605321022fad62acf359dd9321f6942308ae3ebc4aa` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineSurvivabilityClassificationShouldAccountForAllFourteenSuites` | 2 → 2 | `77a8500a8e609d5ad18056813fb303397572de97c77ee5a2f7b10484cd8a43ba` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedBaselineRecordShouldExistAndDescribeAGreenAllPassOracle` | 8 → 8 | `3c32a8cc2658841addb3dc0e42117d7901ba98ada5f4aabfd8d549c53d418e93` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotCapturedSurfaceShouldPassContentSafetyScan` | 2 → 2 | `c9b3f6db8563709c5748e48a1f3b55a20c85269003fec2c6bc020290be79442f` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotShouldExistAndDeclareItsAssemblyAndTypeCount` | 5 → 5 | `9a96174beab84496b95d7f0471371a713892bd4e740123e260b15562b680de8a` |
| `Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface` | 2 → 2 | `6fc850af46b626a9a1f9663504a3a991bf794fd7022249189f0569963cffca18` |

## Recorded decisions and remaining acceptance

The [Quality approval](conformance-oracle-tiering-migration-approval-v3.json) records the actual user decision in the runbook’s closed format and covers the proposal SHA-256 above, every changed row, and the public-drift SHA-256. The [environment authorization](../../_bmad-output/planning-artifacts/v9/story-9.2-candidate-environment-amendment-v1.json) independently accepts exactly the reviewed seven existing gitlinks and five owning promotions. Both retain the original baseline; neither waives the acceptance gates. Approach decisions and synthetic fixtures remain insufficient approval evidence.

After approval: commit the implementation candidate, rerun its effective acceptance scenarios, generate the final pair twice, verify insertion, and complete the story lifecycle. Preserve the original baseline and all historical evidence.
