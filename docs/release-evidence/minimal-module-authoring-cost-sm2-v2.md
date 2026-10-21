# Minimal-module authoring cost — SM-2 v2

Candidate: `11fd90ef91baffd6042d9a5030420310123a5054`. Checkpoint C.

Measured: **13 files**, **451 physical LOC**, **396 nonblank LOC**.
Reference: **58 estimated files / 1,460 estimated LOC**. File reduction **77.59%**; indicative LOC reduction **69.11%**.
OQ-2: `accepted` — `met-on-accepted-estimate`. Measurement confidence `high`; comparison confidence `low`.

- Attainment is against the owner-accepted historical estimate of 58 files and 1,460 LOC; comparison confidence remains low.
- The reference is an estimate, not a reconstructed, compiled pre-initiative fixture; no reference fixture is introduced.
- Fixture physical LOC includes blank and comment lines. Historical LOC represents estimated nonblank logical skeleton lines; the LOC reduction is indicative and is not a like-for-like physical LOC comparison.
- File reduction alone decides OQ-2 at the inclusive 50% threshold before rounding; LOC is supporting evidence only.

Signable payload: `21732a0420006e0479e1208f2ee0449993180a62027e4a300be9c11a18264bab`

## Complete measured payload

```json
{
  "schemaVersion": "hexalith.conversations.minimal-module-authoring-cost-sm2.v2",
  "candidate": {
    "storyId": "11.3",
    "checkpoint": "C",
    "commit": "11fd90ef91baffd6042d9a5030420310123a5054",
    "planningCandidate": "1e9a61126d3b7a55b514b7c7c8942d5af03355e5",
    "contractPath": "_bmad-output/planning-artifacts/v9/story-contracts/11.3.json",
    "contractSha256": "10ee26812edf866ca5e78564c0af1ae5aeceaf0c595a42052a72372951e15d9c",
    "checkpoints": {
      "A": "09007a08f63773923694f09e9c49f972d6df81ae",
      "B": "aeca17f120aa731d0d47b79b7d1b54fb63144bb8",
      "C": "11fd90ef91baffd6042d9a5030420310123a5054"
    },
    "gitlinks": [
      {
        "path": "references/Hexalith.AI.Tools",
        "mode": "160000",
        "commit": "3f194e17174994d308ec84af9ee2b5aa68674d0d"
      },
      {
        "path": "references/Hexalith.Builds",
        "mode": "160000",
        "commit": "6a38cb0f2e74977d7457fc59e6af79457ed8196a"
      },
      {
        "path": "references/Hexalith.Commons",
        "mode": "160000",
        "commit": "5c23c818f639c5bb3175f964c5549440bd5b876d"
      },
      {
        "path": "references/Hexalith.EventStore",
        "mode": "160000",
        "commit": "5d140359cc4021156ef32e8cb5e7bfa763a1f00c"
      },
      {
        "path": "references/Hexalith.Folders",
        "mode": "160000",
        "commit": "9bacd7f62848c23222dc7309635eb65dc6e5ed9c"
      },
      {
        "path": "references/Hexalith.FrontComposer",
        "mode": "160000",
        "commit": "a2196fd67d6da3e9e56200847f6ea3b51419fc0a"
      },
      {
        "path": "references/Hexalith.McpCli",
        "mode": "160000",
        "commit": "b616db3092a9b1c5af196538a3de37a5c7f2d7ca"
      },
      {
        "path": "references/Hexalith.Memories",
        "mode": "160000",
        "commit": "72a97345b4230e39627cd2026934a547df7d43a4"
      },
      {
        "path": "references/Hexalith.Parties",
        "mode": "160000",
        "commit": "4d4be476687cff4210774ec62558131ff8659558"
      },
      {
        "path": "references/Hexalith.Platform",
        "mode": "160000",
        "commit": "bb81fc9e98533246cfeed43ffcce08866a2a57f6"
      },
      {
        "path": "references/Hexalith.Projects",
        "mode": "160000",
        "commit": "12a7668c5cc8f6a07537dfaa8aef2b81c7b5c5c0"
      },
      {
        "path": "references/Hexalith.Tenants",
        "mode": "160000",
        "commit": "243d0c4a3f845e3f582a32c08e373651d978ff0e"
      }
    ]
  },
  "provenance": [
    {
      "path": "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json",
      "sha256": "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3"
    },
    {
      "path": "_bmad-output/planning-artifacts/v9/story-contracts/11.1.json",
      "sha256": "cab730317c6dd72d815c37e07dc5713ef66d4348ce8aa7fc23b7700cc611594d"
    },
    {
      "path": "_bmad-output/planning-artifacts/v9/story-contracts/11.2.json",
      "sha256": "6e67b227fdafe0dc53fcb2fd307f6639b4b7ca44a350243dc4848e8e4dcc5e3a"
    },
    {
      "path": "_bmad-output/planning-artifacts/v9/story-contracts/11.3.json",
      "sha256": "10ee26812edf866ca5e78564c0af1ae5aeceaf0c595a42052a72372951e15d9c"
    },
    {
      "path": "artifacts/v9/11.1/AC-11.1-03.trx",
      "sha256": "40b7aba4422edeeddb746e115245362ef79bad1dc4666a84e784e1231761d93d"
    },
    {
      "path": "artifacts/v9/11.1/AC-11.1-04.trx",
      "sha256": "02e6b435c079354c4525b8398b777a92ee8b7772de7f300bedab97197e4382ba"
    },
    {
      "path": "artifacts/v9/11.1/AC-11.1-05.json",
      "sha256": "5011787bb43a857d18b60c174dc1512bede7df74bd603fa148181f44f00c6944"
    },
    {
      "path": "artifacts/v9/11.1/AC-11.1-06.xml",
      "sha256": "f9ef95932fe58ef3029c1396a0a1853a61d0399f5c7fa9240ce2d205793afc55"
    },
    {
      "path": "artifacts/v9/11.2/AC-11.2-01.json",
      "sha256": "04460bce7bbeb2fc0026b4d7d929a265e3d43fdd0703a8877d756a6310ea75b8"
    },
    {
      "path": "artifacts/v9/11.2/AC-11.2-02.log",
      "sha256": "16ef294f919830b63836c305458cfaecc1011b0478356b5d2c87e7418e9a3673"
    },
    {
      "path": "artifacts/v9/11.2/AC-11.2-03.log",
      "sha256": "006a2b78126123ee02c4538aab709380bad94956b1d0594269e44faf0bbac08e"
    },
    {
      "path": "artifacts/v9/11.2/AC-11.2-05.trx",
      "sha256": "03206037eab295ec9f13f69a7fb82fbf623df6513d28f5e2376c1d28e95b4b4a"
    },
    {
      "path": "artifacts/v9/11.2/AC-11.2-07.xml",
      "sha256": "1afb8255d8feb8ce96c95bee3b355c5750bbcd3d8c86f20c1852306d5909dc90"
    },
    {
      "path": "artifacts/v9/11.2/minimal-module.trx",
      "sha256": "d884ecf23c2e8075bd3e4fb06c73f562baf1c95eb18707bf3edc2e1e7dca9b12"
    },
    {
      "path": "docs/domain-module-authoring-template.md",
      "sha256": "6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb"
    },
    {
      "path": "docs/release-evidence/story-11.1-final-record-v2.json",
      "sha256": "6ff02bf2f6a71dea275c420d7860a9a62b9423d245046703b8c906363a91667a"
    },
    {
      "path": "docs/release-evidence/story-11.1-final-record-v2.md",
      "sha256": "babc3700498613f27211c74390d52e9f46f42fd0e4242782e35093a13d288de6"
    },
    {
      "path": "docs/release-evidence/story-11.2-final-record-v2.json",
      "sha256": "f08f87f896784e7a77daa40e2048fe58fad7776b8da7f60d972365616ba348c0"
    },
    {
      "path": "docs/release-evidence/story-11.2-final-record-v2.md",
      "sha256": "b990f5c979002da732974d50f247b96439c9840df25706a24370b549319adc4b"
    },
    {
      "path": "docs/release-evidence/thin-authoring-guidance-v2.json",
      "sha256": "4f078797f4b5c85aef91b3ea9cedd29dac25a3f28b62dcdca1d8a2db4335c0e2"
    },
    {
      "path": "docs/release-evidence/thin-authoring-guidance-v2.md",
      "sha256": "990b46d68df0fc05777459e20308ff8dd9e4dba5dda4a8b5da3482ad66b1948b"
    },
    {
      "path": "docs/release-evidence/thin-authoring-guidance-v2.schema.json",
      "sha256": "984957e1c29a660d352d945b9f81fc6be9401ea74714df9e266054e201b798e5"
    },
    {
      "path": "docs/release-evidence/thin-module-fixture-proof-v1.json",
      "sha256": "01e384e6578d57e09d78e686a4cf6420123c0fbc69419f29946e6235d2fef0c3"
    },
    {
      "path": "docs/release-evidence/thin-module-fixture-proof-v1.md",
      "sha256": "845d7b4d9414b918695636dd9f35843a3a815091f74fa16ff1116f09d9b2f758"
    },
    {
      "path": "docs/release-evidence/thin-module-fixture-proof-v1.schema.json",
      "sha256": "6e944259e9ead83e1a70b62c6e0fc597702b3fcb32e4c2e160b616cd9b89efa7"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props",
      "sha256": "1829424f3e0bb649130e4dd551e7153a60d3c8b15cc9230462922e5f2b441f00"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx",
      "sha256": "d1d35e98212004aebb896470c454cb27536cecf088bae44688c71b84364f1e02"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/README.md",
      "sha256": "1237ce284dbb1dd03e583d76075536ec07a190b24940810e1dece70d2f83d4e2"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs",
      "sha256": "77bc0e65e08cc5cd47ff30bd485383c4c67307e9a8a050f5bbfd96ab6635707c"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs",
      "sha256": "2bde9eb11aa095f7cb0e7127bffbcce65408663f09b3ff6a6c3cfde3d061532d"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj",
      "sha256": "0c71e7b64c02f5053e556e51dde2284547d9a0d454c90ef21f196bf44181b11e"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/packages.lock.json",
      "sha256": "52cbaccb8876b9df65fa7e691cf18b6755378e69aa50451ad5e4e211fcac0314"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs",
      "sha256": "a700a1ba954e7dd0cf5da40dc79e74f6fda3038460c873a36cf6cc726fba1a4e"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs",
      "sha256": "0c16ebc8432cb8e8e6d9fb0a741a50a9a9ce765050c192eff9c326acbb27e7e8"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj",
      "sha256": "815c4bd91e1ea95728f087c27e08538b818b70c5046f7666caf54436a86723fa"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json",
      "sha256": "d9f8362c9a38b3ad08481559e89dbebc92c474a06e1579923182f4419796295c"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs",
      "sha256": "42e0d71777bc9456a600c8bc8618fa3e6f3d6c4a95a5dfffdee9056d985175a6"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj",
      "sha256": "320077e5282671bbe921ce01c3ed7f8680d096f31a499e0e41a32ff49a02e36f"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/packages.lock.json",
      "sha256": "78b06f0a95f4be921ace42500c2eba6e7578c0dccb162a2400d838c9b15a2423"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj",
      "sha256": "6232260db2a9496548739fbd86398cb75a48208454c1860f9684a7d8d4e1f11f"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs",
      "sha256": "b5a1c813bd41ffd72e54612e94ae742955294b33d2099e3d463d27617d6e2229"
    },
    {
      "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/packages.lock.json",
      "sha256": "7a55c490e0263300eb97079fffc684ca61b1f00acd057bcf213f7f743744d104"
    }
  ],
  "historicalBaseline": {
    "ownerDecision": "2026-10-20: retain the accepted estimated reference of 58 files / 1,460 LOC; comparison confidence remains low.",
    "sm1BaselineLoc": 13289,
    "history": [
      {
        "path": "docs/release-evidence/consume-promote-keep-inventory-v1.json",
        "sha256": "20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7"
      },
      {
        "path": "docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json",
        "sha256": "14de3c86628cb4f900008be42d086a7699ae0869b3b791f92c810ab921192e03"
      },
      {
        "path": "docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.md",
        "sha256": "1cb37b3ddbc9186748c7556a87e5c08feb15d8dee30f4a1b0362bcd5536297fd"
      },
      {
        "path": "docs/release-evidence/oq-2-target-interpretation-decision-v1.json",
        "sha256": "06281924d9760f05f638c4a74661de9cd973f88c773d7ad3263ee25a830a3e06"
      }
    ]
  },
  "measurement": {
    "root": "tests/fixtures/Hexalith.Conversations.MinimalModule",
    "inventoryId": "V9-MINIMAL-MODULE-FIXTURE-v1",
    "inventorySha256": "4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c",
    "paths": [
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props",
        "sha256": "1829424f3e0bb649130e4dd551e7153a60d3c8b15cc9230462922e5f2b441f00",
        "bytes": 1810,
        "physicalLoc": 33,
        "nonblankLoc": 33,
        "category": "repository configuration and documentation",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx",
        "sha256": "d1d35e98212004aebb896470c454cb27536cecf088bae44688c71b84364f1e02",
        "bytes": 595,
        "physicalLoc": 10,
        "nonblankLoc": 10,
        "category": "repository configuration and documentation",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/README.md",
        "sha256": "1237ce284dbb1dd03e583d76075536ec07a190b24940810e1dece70d2f83d4e2",
        "bytes": 977,
        "physicalLoc": 18,
        "nonblankLoc": 14,
        "category": "repository configuration and documentation",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj",
        "sha256": "0c71e7b64c02f5053e556e51dde2284547d9a0d454c90ef21f196bf44181b11e",
        "bytes": 205,
        "physicalLoc": 5,
        "nonblankLoc": 5,
        "category": "Contracts",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs",
        "sha256": "2bde9eb11aa095f7cb0e7127bffbcce65408663f09b3ff6a6c3cfde3d061532d",
        "bytes": 633,
        "physicalLoc": 18,
        "nonblankLoc": 14,
        "category": "Contracts",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs",
        "sha256": "77bc0e65e08cc5cd47ff30bd485383c4c67307e9a8a050f5bbfd96ab6635707c",
        "bytes": 403,
        "physicalLoc": 8,
        "nonblankLoc": 6,
        "category": "Contracts",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj",
        "sha256": "815c4bd91e1ea95728f087c27e08538b818b70c5046f7666caf54436a86723fa",
        "bytes": 843,
        "physicalLoc": 12,
        "nonblankLoc": 12,
        "category": "domain and registration",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs",
        "sha256": "a700a1ba954e7dd0cf5da40dc79e74f6fda3038460c873a36cf6cc726fba1a4e",
        "bytes": 1614,
        "physicalLoc": 37,
        "nonblankLoc": 32,
        "category": "domain and registration",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs",
        "sha256": "0c16ebc8432cb8e8e6d9fb0a741a50a9a9ce765050c192eff9c326acbb27e7e8",
        "bytes": 5064,
        "physicalLoc": 112,
        "nonblankLoc": 98,
        "category": "domain and registration",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj",
        "sha256": "6232260db2a9496548739fbd86398cb75a48208454c1860f9684a7d8d4e1f11f",
        "bytes": 745,
        "physicalLoc": 17,
        "nonblankLoc": 17,
        "category": "module tests",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs",
        "sha256": "b5a1c813bd41ffd72e54612e94ae742955294b33d2099e3d463d27617d6e2229",
        "bytes": 6959,
        "physicalLoc": 155,
        "nonblankLoc": 132,
        "category": "module tests",
        "included": true,
        "reason": "Hand-authored module-owned source, configuration, or documentation."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj",
        "sha256": "320077e5282671bbe921ce01c3ed7f8680d096f31a499e0e41a32ff49a02e36f",
        "bytes": 851,
        "physicalLoc": 14,
        "nonblankLoc": 14,
        "category": "AppHost",
        "included": true,
        "reason": "Hand-authored module-owned source; repository-only test AppHost counts in full."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs",
        "sha256": "42e0d71777bc9456a600c8bc8618fa3e6f3d6c4a95a5dfffdee9056d985175a6",
        "bytes": 685,
        "physicalLoc": 12,
        "nonblankLoc": 9,
        "category": "AppHost",
        "included": true,
        "reason": "Hand-authored module-owned source; repository-only test AppHost counts in full."
      }
    ],
    "fileCount": 13,
    "physicalLoc": 451,
    "nonblankLoc": 396,
    "confidence": "high",
    "physicalLocMetric": "All physical lines, including blank and comment lines; a final newline adds no empty line.",
    "nonblankLocMetric": "Physical lines with at least one non-whitespace character, reported separately.",
    "exclusions": [
      {
        "pattern": "**/bin/**; **/obj/**",
        "reason": "Generated build outputs are not hand-authored module-owned files."
      },
      {
        "pattern": "**/packages.lock.json",
        "reason": "NuGet-generated dependency locks are retained and hash-bound by Checkpoint B, excluded from authored cost."
      },
      {
        "pattern": "references/**",
        "reason": "Shared platform libraries and runtime boilerplate are platform-owned, outside the fixture."
      }
    ],
    "excludedFiles": [
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/packages.lock.json",
        "sha256": "52cbaccb8876b9df65fa7e691cf18b6755378e69aa50451ad5e4e211fcac0314",
        "reason": "Generated dependency lock, excluded from hand-authored file and LOC totals."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json",
        "sha256": "d9f8362c9a38b3ad08481559e89dbebc92c474a06e1579923182f4419796295c",
        "reason": "Generated dependency lock, excluded from hand-authored file and LOC totals."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/packages.lock.json",
        "sha256": "78b06f0a95f4be921ace42500c2eba6e7578c0dccb162a2400d838c9b15a2423",
        "reason": "Generated dependency lock, excluded from hand-authored file and LOC totals."
      },
      {
        "path": "tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/packages.lock.json",
        "sha256": "7a55c490e0263300eb97079fffc684ca61b1f00acd057bcf213f7f743744d104",
        "reason": "Generated dependency lock, excluded from hand-authored file and LOC totals."
      }
    ]
  },
  "comparison": {
    "referenceFileCount": 58,
    "referenceLoc": 1460,
    "referenceMeasurementType": "estimated",
    "referenceConfidence": "low",
    "measurementConfidence": "high",
    "comparisonConfidence": "low",
    "decisiveDimension": "hand-authored module-owned file count",
    "supportingDimension": "physical LOC",
    "thresholdPercentage": "50.00",
    "comparisonOperator": ">=",
    "evaluatedBeforeRounding": true,
    "fileReductionPercentage": "77.59",
    "locReductionPercentage": "69.11",
    "oq2Decision": "accepted",
    "result": "met-on-accepted-estimate",
    "limitations": [
      "Attainment is against the owner-accepted historical estimate of 58 files and 1,460 LOC; comparison confidence remains low.",
      "The reference is an estimate, not a reconstructed, compiled pre-initiative fixture; no reference fixture is introduced.",
      "Fixture physical LOC includes blank and comment lines. Historical LOC represents estimated nonblank logical skeleton lines; the LOC reduction is indicative and is not a like-for-like physical LOC comparison.",
      "File reduction alone decides OQ-2 at the inclusive 50% threshold before rounding; LOC is supporting evidence only."
    ]
  },
  "singleFileSensitivity": {
    "maximumFilesMeetingTarget": 29,
    "additionalFilesBeforeFailure": 16,
    "filesAfterAddingOne": 14,
    "filesAfterRemovingOne": 12,
    "addingOneFlipsAttainment": false,
    "removingOneFlipsAttainment": false,
    "singleFileDecisionsThatFlip": [],
    "explanation": "All 13 frozen authored paths are mandatory. Including or excluding one file does not flip the measured result; the historical 29-file figure sat exactly at 50.00%."
  },
  "toolVersions": {
    "python": "3.14.4",
    "jsonschema": "4.26.0",
    "dotnetSdk": "10.0.402",
    "git": "git version 2.53.0"
  },
  "command": "python3 _bmad/scripts/generate_sm2_evidence.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/11.3.json --fixture-proof docs/release-evidence/thin-module-fixture-proof-v1.json --baseline docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json --output-json docs/release-evidence/minimal-module-authoring-cost-sm2-v2.json --output-markdown docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md"
}
```
