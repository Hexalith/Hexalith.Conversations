# Story 9.2 current-main successor Quality review v5

Preparation evidence only. Scope authorization supplies no Quality decision or acceptance.

Authorized source: `ec5c9be52631b96793ab028febfbbfba4d362d48`.
Authorized scope material: `925cf11a819e9412055f3d35ea85a0a4334f9371e7d1decdb8199bb7b6757672`.
Successor material: `3e1c8e6a4dfb4358e2797d8a3bfb84d652d330ed28924b8b916995613232bb9a`.
Proposed migration material: `8a192571a6660638731184fd1086f97c1d8bd585f68e4290ce16988dc110206a`.

Retained v4 proposal SHA-256: `8aea6446a2f10d308284580cbc11ab4d90268ef2bc69e7e140f6d9300a4eb95d`.
Original migration material: `5ea111cbb12a196ba79231bccfeadf239109e50002a40ca0b942ea9568e79217`.
Prior pending migration material: `606a7e57dafb2084101e85c4003c93bf602643cf2a3f1265c1275424cb050fb4`.
Changed assertion row digests changed: `false`.
Conversations public drift digest changed: `false`.

The proposal preserves all 452 frozen definitions, 401 active methods, 51 historical exclusions,
the 415-case floor, three live controls, FR-20 membership, and original before-strength bindings.
The actual current production source snapshot is bound separately from the historical freeze.

Exact released-package API evidence: `docs/release-evidence/conformance-oracle-tiering-eventstore-released-api-diff-v1.json` (SHA-256 `6a5e5d51f9ec9e5f5cd144b83b25a55547c3231110693e224d6db84fad978b7e`).
The source pins released EventStore `3.118.0`. Its package public API is outside the Conversations
public-drift digest. Client has 1 breaking and 28 additive changes across 25 types. The breaking
change replaces the marker-store EventStoreDomainEventProcessor five-parameter constructor with
a six-parameter signature adding optional EventPayloadEvolutionRegistry. Existing binaries that
call the old constructor signature require recompilation. Contracts has 97 additive changes
across 97 types. DomainService and ServiceDefaults are BothIncomplete due to one metadata
inspection failure on each side; neither receives a complete API classification here.
SDK package API compatibility evidence: `docs/release-evidence/conformance-oracle-tiering-eventstore-released-api-compat-v1.json` (SHA-256 `ebe059f6e4d0213ad29fe1884f384d685beea4262bb1dafdf53ab71f8f2e302d`).
The .NET 10 RunPackageValidation/RunApiCompat target reports CP0002 for the removed Client
five-parameter constructor. DomainService and ServiceDefaults each pass that SDK API
compatibility target. Those exit codes are the SDK observations for those packages;
the earlier dotnet-inspect BothIncomplete results remain exact observations of that tool.
The SDK target does not complete additive API review, and Contracts was not measured by it.
The v7 EventStore Client API diff is preparation history. Complete released-package API review
and a genuine Quality decision remain required.

## Exact later Quality binding

```json
{
  "role": "Quality owner",
  "proposalSha256": "3e1c8e6a4dfb4358e2797d8a3bfb84d652d330ed28924b8b916995613232bb9a",
  "migrationProposalSha256": "8a192571a6660638731184fd1086f97c1d8bd585f68e4290ce16988dc110206a",
  "changedAssertionRows": [
    [
      "Hexalith.Conversations.Conformance.Tests.GovernanceAuditPairingSafetyNetConformanceTest.AggregateGovernanceCommandSurfaceShouldMatchTheAuditPairedInventory",
      "c04079b647f59c5f26e5e738b4c600e731d995e2a6b7a659777c964c848faffd"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting",
      "d2ff5d529caa31961ed04ef54ad40deea5b666edb0355640671eb9daf19579bf"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.GenerateAndSaveContractShapeSnapshotFile",
      "540f4f7071decfc6d26f91fcfa417d4a83417100cd0d4d49d5640ee18c8f650b"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldBeContentSafe",
      "0e6408fb2708ed2d466023176fbe62cf0c1330f70770429ba6715b1bddbf60b8"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCaptureExportedPublicTypesDeterministically",
      "160bda151f562ff610d8c7a987d33d09549e64f3d6037f4ad4c0b54d661e1725"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.PublicContractShapeSnapshotGenerationTest.SnapshotShouldCoverAllSixReleaseGateBehaviorAreas",
      "311a3e56c2fbc835f15d95784bb3631db8e23122ad95f862f03a56347b5504e5"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineCommitShouldBeAFullFortyCharacterHexShaOnMain",
      "fabf5413c654c5b364de99cb295aa291044d07868559e232025d3a8cfc93070f"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineEnumeratedSuiteClassesShouldMatchTheActualSuiteClassesInTheAssembly",
      "172dce81104ef4d61da501780a30a5e27514a8ccce9d215a019c74e43c9f186f"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface",
      "e1bd1dbe2c65488a73b5b605321022fad62acf359dd9321f6942308ae3ebc4aa"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.BaselineSurvivabilityClassificationShouldAccountForAllFourteenSuites",
      "77a8500a8e609d5ad18056813fb303397572de97c77ee5a2f7b10484cd8a43ba"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedBaselineRecordShouldExistAndDescribeAGreenAllPassOracle",
      "3c32a8cc2658841addb3dc0e42117d7901ba98ada5f4aabfd8d549c53d418e93"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotCapturedSurfaceShouldPassContentSafetyScan",
      "c9b3f6db8563709c5748e48a1f3b55a20c85269003fec2c6bc020290be79442f"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotShouldExistAndDeclareItsAssemblyAndTypeCount",
      "9a96174beab84496b95d7f0471371a713892bd4e740123e260b15562b680de8a"
    ],
    [
      "Hexalith.Conversations.Conformance.Tests.ReleaseBaselineValidationTest.CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface",
      "6fc850af46b626a9a1f9663504a3a991bf794fd7022249189f0569963cffca18"
    ]
  ],
  "publicDriftSha256": "3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1",
  "sourceSnapshotSha256": "1905788953005d0140473839e7eef7ea67f5a73ebd81245b31e081adf6086ddd",
  "scopeProposalSha256": "925cf11a819e9412055f3d35ea85a0a4334f9371e7d1decdb8199bb7b6757672",
  "releasedEventStoreApiDiffSha256": "6a5e5d51f9ec9e5f5cd144b83b25a55547c3231110693e224d6db84fad978b7e",
  "releasedEventStoreApiCompatSha256": "ebe059f6e4d0213ad29fe1884f384d685beea4262bb1dafdf53ab71f8f2e302d"
}
```

Review this complete material, every changed row, the current source/environment, and public drift.
A later genuine Quality decision must bind these exact digests.

## Measured Release build evidence

| Observation | Exit | State | Command |
| --- | --- | --- | --- |
| portable-restore | 0 | available | `dotnet restore tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj -p:Configuration=Release --source /home/administrator/.nuget/packages -p:NuGetAudit=false` |
| portable-build | 0 | available | `dotnet build tests/Hexalith.Conversations.Conformance.Portable.Tests/Hexalith.Conversations.Conformance.Portable.Tests.csproj --configuration Release --no-restore` |
| module-internal-restore | 0 | available | `dotnet restore tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -p:Configuration=Release --source /home/administrator/.nuget/packages -p:NuGetAudit=false` |
| module-internal-build | 0 | available | `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore` |
| module-internal-fallback | 0 | available | `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` |

Build logs and their hashes are bound in the JSON proposal. Current-tree tier execution
and fault runs remain provisional; candidate-bound AC01–10 and final-record insertion are still required.
The original baseline, v1–v4 preparation, v3 Quality approval, accepted pairs, and frozen records
retain their original bytes. Story 9.2 stays in-progress.
