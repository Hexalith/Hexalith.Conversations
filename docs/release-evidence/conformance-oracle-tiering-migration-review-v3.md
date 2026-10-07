# Story 9.2 migration: Quality review packet

**Decision pending.** This packet describes the concrete proposal prepared under the user’s “do recommended” approach decision and refreshed after the user requested “update to latest eventstore”. It records no Quality approval.

- Proposal SHA-256: `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`
- Public-drift SHA-256: `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`
- Authoritative proposal: [conformance-oracle-tiering-migration-v3.json](conformance-oracle-tiering-migration-v3.json).
- Working-tree baseline: `51aa06b856bcb0aaf22013153cfe02a51b046156`. Final acceptance requires a committed, approved candidate and fresh receipts.

The shared catalog now selects the latest listed stable EventStore family, `3.115.0`. The update changes only `portableSurface` in the pending proposal; assertion rows, both strength inventories, and the public-drift digest remain identical. The previous pending proposal (`f59dce5e7642c7ef588dcb0f3e4f7fe045598ee613c9638b3198a5e57f331eef`) and review packet are retained under [the update receipts](../../artifacts/v9/9.2/eventstore-update/). No approved migration or historical evidence was rewritten.

## Recommended decision

Approve the 14 successor assertion rows and the exact pre-existing public-surface drift below. The split keeps the historical definitions and exclusions, preserves the 415 active case identities, and counts the three live controls separately. The current-surface guards continue to compare exact expectations; the snapshot round-trip uses a temporary file.

The original strength hashes and signed evidence remain retained. Changed hashes are evidence of changed guard behavior; matching assertion counts alone does not establish equal semantic strength. The owner should assess the three guard changes below before approving their digests.

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

## Measured verification

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

Fresh Release build and tier-execution receipts for EventStore `3.115.0` are under `artifacts/v9/9.2/eventstore-update/`; the earlier Debug, fault, and generator receipts remain under `artifacts/v9/9.2/`. The fresh verifier observes all 415 retained cases and three live controls, with only `TIER_APPROVAL_MISSING` failing. These are provisional working-tree measurements. No accepted Story 9.2 final record has been published; formal AC04/07/08/10 remain pending the Quality decision, compatible candidate scope, and candidate rerun.

## Exact changed-row bindings

Every identity below retains its original before-strength hash in the authoritative proposal. The decision must bind all row digests in this order.

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

## Recording an actual decision

The Quality owner’s approval must name the proposal SHA-256 above and cover every changed row plus the public-drift SHA-256. Record the actual approver, date, decision ID, and evidence in the separate `conformance-oracle-tiering-migration-approval-v3.json` file using the runbook’s closed decision format. An approach decision or a synthetic test fixture does not authorize that record.

After approval: commit the implementation candidate, rerun its effective acceptance scenarios, generate the final pair twice, verify insertion, and complete the story lifecycle. Preserve the original baseline and all historical evidence.
