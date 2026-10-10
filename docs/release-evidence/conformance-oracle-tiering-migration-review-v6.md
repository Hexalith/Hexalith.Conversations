# Story 9.2 successor Quality review v6

Preparation only. The v3 scope authorization does not approve new Quality material or candidate acceptance.

- Authorized source commit: `fbe2f502eed26df45edc12e4a12e9917bf97b438`; tree: `ebbf83ea0d617232be5455da88b03cf267cd6586`.
- V3 scope authorization SHA-256: `54f3193f21d05ce6a769e0f6e6a9071091fbff60844f65f3b07fcae32ee9efe9`; proposal material: `81b73f5642f6fff1f155d0f4bdce6b76dfad25a7a80e5b8991bdc3df53253745`.
- New source snapshot SHA-256: `a52f067f7b6d7546677a7d888efa3a07a8bd039c6ed39c20d53cb511617770f9`.
- New proposal material SHA-256: `eb6d91b7600b6916942f824ed8ba4fdfd8973c3197f5969f54b0a3f6ac7b60aa`.
- Prior v5 Quality approval SHA-256: `6a0218ea421ff448dee93e066b6bbc801498cecd28d95e7322a783a97ebbceb3`; it covers only `ec5c9be52631b96793ab028febfbbfba4d362d48`.

## Exact root gitlinks

| Path | Commit |
| --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `2cf00028bbe563d80d4d12b5fb2054914f14fcb6` |
| `references/Hexalith.Commons` | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| `references/Hexalith.EventStore` | `37451b529ab21869fa4e2806968b5143ddeea14b` |
| `references/Hexalith.Folders` | `31909333eb763f73275c4d3df4367f5d06219c2f` |
| `references/Hexalith.FrontComposer` | `0e114214007c22f5cdbac21a6853cff4208340ee` |
| `references/Hexalith.Memories` | `7b33e016a52907989181139298db18edef1a05d7` |
| `references/Hexalith.Parties` | `926faa207bab55eeeed4bc7d5d5a50b3e6af917f` |
| `references/Hexalith.Projects` | `fff5a5dbb74ff79bf56cd10527260178776eb060` |
| `references/Hexalith.Tenants` | `4b0cfa3440d2a0b623c4e9d4d1ba4b6aba9de100` |

## Migration and released API

Fresh migration digest: `8a192571a6660638731184fd1086f97c1d8bd585f68e4290ce16988dc110206a`; equal to the exact v5 migration.
All 14 ordered changed-row digests and public drift `3357725bc7ebc039baca0bf928718f03f2ae037dbd130e37704bd3315480d2d1` are unchanged; the 415-case floor and three controls remain required.
Released EventStore API diff SHA-256: `6a5e5d51f9ec9e5f5cd144b83b25a55547c3231110693e224d6db84fad978b7e`; SDK API compatibility receipt SHA-256: `ebe059f6e4d0213ad29fe1884f384d685beea4262bb1dafdf53ab71f8f2e302d`.
The repeated Client 3.117.1→3.118.0 diff reports one breaking constructor signature and 28 additive changes across 25 types. Contracts has 97 additive changes across 97 types in the prior exact receipt. DomainService and ServiceDefaults remain BothIncomplete under dotnet-inspect because metadata inspection failed before and after. The .NET 10 RunApiCompat receipt reports Client CP0002 for the removed five-parameter marker-store constructor; DomainService and ServiceDefaults pass that target. Complete additive API review is not claimed.

## Exact Quality decision requested

```json
{
  "role": "Quality owner",
  "proposalSha256": "eb6d91b7600b6916942f824ed8ba4fdfd8973c3197f5969f54b0a3f6ac7b60aa",
  "scopeProposalSha256": "81b73f5642f6fff1f155d0f4bdce6b76dfad25a7a80e5b8991bdc3df53253745",
  "sourceSnapshotSha256": "a52f067f7b6d7546677a7d888efa3a07a8bd039c6ed39c20d53cb511617770f9",
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
  "releasedEventStoreApiDiffSha256": "6a5e5d51f9ec9e5f5cd144b83b25a55547c3231110693e224d6db84fad978b7e",
  "releasedEventStoreApiCompatSha256": "ebe059f6e4d0213ad29fe1884f384d685beea4262bb1dafdf53ab71f8f2e302d"
}
```

A genuine Quality owner must review and bind this new source/scope material. Fresh committed-candidate AC01–10, restored fault receipts, final pair, and insertion verification remain required. No Quality approval or acceptance is claimed.
