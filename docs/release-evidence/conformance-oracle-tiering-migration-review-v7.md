# Story 9.2 successor Quality review v7

Preparation only. The v4 current-main scope is measured but unapproved; this packet supplies no scope authorization, Quality decision, or candidate acceptance.

- Current committed candidate: `353e9dbf47a1472b29fe3f43d485a8ae3a03b884`; tree `b3af5900eb22e356360d247adc99a0fe662415fd`.
- Pending v4 scope proposal file SHA-256: `0939819cbe661a25abcb8746dd85a7cecf2fdfd1ed33a957d1265d42257930b1`; material SHA-256: `f5dfa7f71b93bbb799559bbddef69464cd63d2a49dc8ffb127323c2e5e8f1c44`; review SHA-256: `bc1b0ef24f49e31501104310b16ef9853483ddc0a30ec5bf3f73eec48c5865b2`.
- New source snapshot SHA-256: `7a482991536ee2458d41694445f4f918ea0e7fea6e34887cd2ba1b581408bd93`; new Quality material SHA-256: `f23e3d3d005b82be76dbdc825cc7abf6da5e65cd98ef37a65674c035c4f7ec7b`.
- v6 Quality approval SHA-256: `dc66a8610bd6483ee6d8ad50e7f1d6dbe496c6f85e84ebf1b0ab1785cc75e57d`; it binds `fbe2f502eed26df45edc12e4a12e9917bf97b438` and the earlier `3.118.0` environment.

## Exact source and package change

- Production `src/` blobs and exported Conversations surface are byte-identical to v6. The root tree, seven gitlinks, Builds catalog, and effective EventStore package version changed.
- The live migration digest is `514cef81af1a780d89c2bdf6b9670453adc9215fc661b466105f1b120a17cdb5`, versus v6 `8a192571a6660638731184fd1086f97c1d8bd585f68e4290ce16988dc110206a`. The only migration material changes are the two portable transitive compile assets below.

| Asset | v6 library | Current library |
| --- | --- | --- |
| `lib/net10.0/Hexalith.EventStore.Client.dll` | `Hexalith.EventStore.Client/3.118.0` | `Hexalith.EventStore.Client/3.119.0` |
| `lib/net10.0/Hexalith.EventStore.Contracts.dll` | `Hexalith.EventStore.Contracts/3.118.0` | `Hexalith.EventStore.Contracts/3.119.0` |

- All 14 changed assertion row digests remain equal to v6; public drift remains `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1`. The 415-case floor and three controls remain required.

## Released EventStore API evidence

- 3.118.0→3.119.0 dotnet-inspect receipt SHA-256: `b47142a1282841ee1d55afcfbccd33dc61498c5b1a12e2f40fabc2023d126850`.
- .NET 10 SDK RunApiCompat receipt SHA-256: `6bc586e1e1ba670ab1baf0a30a42fea7f89ebca9b7119e0d0a54cb4264c7be95`.
- Client and Contracts: dotnet-inspect reports no public API changes; both SDK compatibility checks pass.
- DomainService and ServiceDefaults: dotnet-inspect reports `BothIncomplete` because metadata inspection fails before and after; both SDK binary compatibility checks pass. Complete additive API review remains open.
- The previous v6 packet disclosed and approved the Client `CP0002` break from 3.117.1→3.118.0. This packet measures only the subsequent 3.118.0→3.119.0 delta.

## Exact Quality binding for review

```json
{
  "role": "Quality owner",
  "proposalSha256": "f23e3d3d005b82be76dbdc825cc7abf6da5e65cd98ef37a65674c035c4f7ec7b",
  "scopeProposalSha256": "f5dfa7f71b93bbb799559bbddef69464cd63d2a49dc8ffb127323c2e5e8f1c44",
  "sourceSnapshotSha256": "7a482991536ee2458d41694445f4f918ea0e7fea6e34887cd2ba1b581408bd93",
  "migrationProposalSha256": "514cef81af1a780d89c2bdf6b9670453adc9215fc661b466105f1b120a17cdb5",
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
  "releasedEventStoreApiDiffSha256": "b47142a1282841ee1d55afcfbccd33dc61498c5b1a12e2f40fabc2023d126850",
  "releasedEventStoreApiCompatSha256": "6bc586e1e1ba670ab1baf0a30a42fea7f89ebca9b7119e0d0a54cb4264c7be95"
}
```

First decide the pending exact v4 scope. A genuine Quality owner can then decide this exact new material, including the incomplete DomainService and ServiceDefaults comparisons. Fresh candidate-bound AC01–10, a deterministic final pair, and insertion verification remain required after both decisions.
