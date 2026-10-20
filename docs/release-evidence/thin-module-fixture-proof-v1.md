# Thin-Module Fixture Proof v1

Schema: `hexalith.conversations.thin-module-fixture-proof.v1`

## Candidate

- Story: `11.2` (Checkpoint B)
- Contract: `_bmad-output/planning-artifacts/v9/story-contracts/11.2.json` (`6e67b227fdafe0dc53fcb2fd307f6639b4b7ca44a350243dc4848e8e4dcc5e3a`)
- Planning candidate: `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`
- Committed candidate: `ab71d77293aed2ea072dcf413963bcf56cb91463`
- EventStore gitlink: `references/Hexalith.EventStore` (mode `160000`, `ce1eba0c459f4a00b2e555668e2e548af81f34f5`)

## Guidance digest

- Bundle: `docs/release-evidence/thin-authoring-guidance-v2.json` (`4f078797f4b5c85aef91b3ea9cedd29dac25a3f28b62dcdca1d8a2db4335c0e2`)
- Bundle Markdown: `990b46d68df0fc05777459e20308ff8dd9e4dba5dda4a8b5da3482ad66b1948b`
- Signable payload: `9ce146a26cd693261392928a2f767d3752b0101f6097e8a0bc7757784a07ef80`
- Guidance source: `6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb`
- Story 11.1 record: `docs/release-evidence/story-11.1-final-record-v2.json` (`6ff02bf2f6a71dea275c420d7860a9a62b9423d245046703b8c906363a91667a`)

## Fixture inventory

- Inventory `V9-MINIMAL-MODULE-FIXTURE-v1`: `4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c`
- Root: `tests/fixtures/Hexalith.Conversations.MinimalModule`

- `tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props` (`1829424f3e0bb649130e4dd551e7153a60d3c8b15cc9230462922e5f2b441f00`, 1810 bytes, 33 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx` (`d1d35e98212004aebb896470c454cb27536cecf088bae44688c71b84364f1e02`, 595 bytes, 10 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/README.md` (`29387d7ca25e178230f7caa8d54f6e71cd8f509af14c355e61705f1d27604a15`, 1455 bytes, 27 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj` (`0c71e7b64c02f5053e556e51dde2284547d9a0d454c90ef21f196bf44181b11e`, 205 bytes, 5 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs` (`2bde9eb11aa095f7cb0e7127bffbcce65408663f09b3ff6a6c3cfde3d061532d`, 633 bytes, 18 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs` (`77bc0e65e08cc5cd47ff30bd485383c4c67307e9a8a050f5bbfd96ab6635707c`, 403 bytes, 8 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj` (`815c4bd91e1ea95728f087c27e08538b818b70c5046f7666caf54436a86723fa`, 843 bytes, 12 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs` (`a700a1ba954e7dd0cf5da40dc79e74f6fda3038460c873a36cf6cc726fba1a4e`, 1614 bytes, 37 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs` (`0c16ebc8432cb8e8e6d9fb0a741a50a9a9ce765050c192eff9c326acbb27e7e8`, 5064 bytes, 112 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj` (`6232260db2a9496548739fbd86398cb75a48208454c1860f9684a7d8d4e1f11f`, 745 bytes, 17 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs` (`b5a1c813bd41ffd72e54612e94ae742955294b33d2099e3d463d27617d6e2229`, 6959 bytes, 155 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj` (`320077e5282671bbe921ce01c3ed7f8680d096f31a499e0e41a32ff49a02e36f`, 851 bytes, 14 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs` (`42e0d71777bc9456a600c8bc8618fa3e6f3d6c4a95a5dfffdee9056d985175a6`, 685 bytes, 12 lines)

## Resolved public platform APIs

- Platform root: `references/Hexalith.EventStore`
- Project `Hexalith.EventStore.Aspire`: `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj` (`a05fa59392fd33324d0a37a502a91fc3f707d64be1e0accb860ae574c8060656`)
- Project `Hexalith.EventStore.Client`: `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj` (`a1dce85420fd2a7e7f4154a32e7276d519639ff25fe368de33876aa4c75445e4`)
- Project `Hexalith.EventStore.Contracts`: `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj` (`6ae249d8b25d97f6b1620f965730f317e1e67537ce478fa791725a94cb0b859a`)
- Project `Hexalith.EventStore.DomainService`: `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj` (`a33f6eff943d309ae17d4278d0b2e7dafd2fab2c14c1134c01821ac622cd99db`)
- Project `Hexalith.EventStore.ServiceDefaults`: `references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj` (`9016f62711665a16f65946bec0a96876951cf2be3006bd9bbf0c30dd0adbf415`)
- Assembly `Hexalith.EventStore.Aspire`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/bin/Release/net10.0/Hexalith.EventStore.Aspire.dll` (`f853f14bd62d1c1b56edffc46d26623dde210998b0c701e76bdae6a828fa7ecb`)
- Assembly `Hexalith.EventStore.Client`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.Client.dll` (`a84e1125004f972d6cb9e2a8173a689517828d9cacbece55e66c800037af55de`)
- Assembly `Hexalith.EventStore.Contracts`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.Contracts.dll` (`0c47a88674d225c36fe1f1080c4af4453f037cab7391cc31566f57a29a98ee67`)
- Assembly `Hexalith.EventStore.DomainService`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.DomainService.dll` (`d74abab993e5f9613afa7824fc40d071a74102dd4e901eb05766e93ac4792741`)
- Assembly `Hexalith.EventStore.ServiceDefaults`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.ServiceDefaults.dll` (`911b0bcd3f83118e00d678a08fd5caa8db8fbec2a62cd9fdab4ac9a454c25da7`)

## Restore

- Command: `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode`
- Locked mode: true
- Receipt: `artifacts/v9/11.2/AC-11.2-02.log` (`7a99d7964c9f2f2600bbef90d2f4c9d5a22cdbb6f2aaffdb6935ae4b03ff5e82`, exit 0)
- Package source: `https://api.nuget.org/v3/index.json`
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/packages.lock.json` (`52cbaccb8876b9df65fa7e691cf18b6755378e69aa50451ad5e4e211fcac0314`, 2 packages)
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json` (`d9f8362c9a38b3ad08481559e89dbebc92c474a06e1579923182f4419796295c`, 66 packages)
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/packages.lock.json` (`78b06f0a95f4be921ace42500c2eba6e7578c0dccb162a2400d838c9b15a2423`, 78 packages)
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/packages.lock.json` (`7a55c490e0263300eb97079fffc684ca61b1f00acd057bcf213f7f743744d104`, 93 packages)
- Resolved packages: 171 (`7bba507fbbd79a36f7020e765e09c48a4ce00b7a1b998302d5abac9442df59f3`)

## Build

- Command: `dotnet build tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --configuration Release --no-restore`
- Configuration: Release
- Warnings as errors: true
- Receipt: `artifacts/v9/11.2/AC-11.2-03.log` (`d6b1e4b6f422a4986372ce33b45530f0d488d1e55fbd62e0a8d500d912d23d34`, exit 0)
- .NET SDK: `10.0.402`
- Aspire AppHost SDK: `Aspire.AppHost.Sdk/13.6.1`
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Contracts.dll` (`5be2b55070d7e289b945552dcfb54e7554471f76df5076d50e7688d17578032f`)
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Server.dll` (`0d0bd1fe2c77d1bdbbbff62e8fc436ad78d35a356eb0263ffaf4b64337e99406`)
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Tests.dll` (`9176bfa0878b53d54f7ff99fd51fe349e820f6d843d548d26c3f467260180763`)
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.AppHost.dll` (`c656c1f2364dae4a9fec5b9c65bf069c049b956924ee03eca66923ce11eeab95`)

## Tests

- Command: `dotnet tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Tests.dll -automated sync -failSkips -trx artifacts/v9/11.2/minimal-module.trx`
- xUnit v3: `4.0.2`
- Result: `artifacts/v9/11.2/minimal-module.trx` (`065bb8c953730f269bd0447c5bc2d9abb9c15cc3a470ce40b4628865f8064136`)
- Total 6, passed 6, failed 0, skipped 0, not run 0
- Case `CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay`
- Case `Projection_IsDispatchedByThePlatformDispatcher`
- Case `PublishedEvent_IsConsumedThroughThePlatformProcessor`
- Case `Query_IsDispatchedByThePlatformDispatcher`
- Case `Registration_ExposesTheModuleHandlersThroughThePlatformHost`
- Case `SubscriptionAndPublication_AreServedByPlatformOwnedEndpointsOnly`
- Capability `registration`: `Registration_ExposesTheModuleHandlersThroughThePlatformHost`
- Capability `commandHandling`: `CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay`
- Capability `eventProduction`: `CommandHandling_ProducesTheCreatedEventAndIsIdempotentAfterReplay`
- Capability `subscription`: `SubscriptionAndPublication_AreServedByPlatformOwnedEndpointsOnly`
- Capability `projection`: `Projection_IsDispatchedByThePlatformDispatcher`
- Capability `query`: `Query_IsDispatchedByThePlatformDispatcher`
- Capability `publication`: `PublishedEvent_IsConsumedThroughThePlatformProcessor`

## Test AppHost

- Count: 1
- Project: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj`
- IsPackable: false
- IsPublishable: false
- Repository-only: true
- Counted in authoring cost: true
- Reusable Aspire projects: 0
- Platform helper: `AddAspireDaprDomainModule`

## Digests

- Inventory: `4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c`
- Signable payload: `4cd2c1df302ec7fccf16a69370ce85c493f5138446b8f16c0ef56c270aa79612`
