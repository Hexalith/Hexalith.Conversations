# Thin-Module Fixture Proof v1

Schema: `hexalith.conversations.thin-module-fixture-proof.v1`

## Candidate

- Story: `11.2` (Checkpoint B)
- Contract: `_bmad-output/planning-artifacts/v9/story-contracts/11.2.json` (`6e67b227fdafe0dc53fcb2fd307f6639b4b7ca44a350243dc4848e8e4dcc5e3a`)
- Planning candidate: `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`

## Guidance digest

- Bundle: `docs/release-evidence/thin-authoring-guidance-v2.json` (`4f078797f4b5c85aef91b3ea9cedd29dac25a3f28b62dcdca1d8a2db4335c0e2`)
- Bundle Markdown: `990b46d68df0fc05777459e20308ff8dd9e4dba5dda4a8b5da3482ad66b1948b`
- Signable payload: `9ce146a26cd693261392928a2f767d3752b0101f6097e8a0bc7757784a07ef80`
- Guidance source: `6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb`
- Story 11.1 record: `docs/release-evidence/story-11.1-final-record-v2.json` (`6ff02bf2f6a71dea275c420d7860a9a62b9423d245046703b8c906363a91667a`)

## Fixture inventory

- Inventory `V9-MINIMAL-MODULE-FIXTURE-v1`: `4fa26d6fa11a365339d9ea39c79e38751340847cf963fe71a82c37062f7df15c`
- Root: `tests/fixtures/Hexalith.Conversations.MinimalModule`

- `tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props` (`9ced242b48e8062d3003f6bc2bb23f9ecbf1bf23fca0f3d9f5e1f38bc790f84e`, 1422 bytes, 26 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx` (`d1d35e98212004aebb896470c454cb27536cecf088bae44688c71b84364f1e02`, 595 bytes, 10 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/README.md` (`1e10a09ab623ebb6e151455cc5b5242c596f384421d8a444c050cef6a37dd28b`, 772 bytes, 15 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/Hexalith.Conversations.MinimalModule.Contracts.csproj` (`0c71e7b64c02f5053e556e51dde2284547d9a0d454c90ef21f196bf44181b11e`, 205 bytes, 5 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/CreateConversation.cs` (`2bde9eb11aa095f7cb0e7127bffbcce65408663f09b3ff6a6c3cfde3d061532d`, 633 bytes, 18 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/ConversationCreated.cs` (`77bc0e65e08cc5cd47ff30bd485383c4c67307e9a8a050f5bbfd96ab6635707c`, 403 bytes, 8 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/Hexalith.Conversations.MinimalModule.Server.csproj` (`815c4bd91e1ea95728f087c27e08538b818b70c5046f7666caf54436a86723fa`, 843 bytes, 12 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs` (`a700a1ba954e7dd0cf5da40dc79e74f6fda3038460c873a36cf6cc726fba1a4e`, 1614 bytes, 37 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs` (`27b0672b186e632a2345b72d7a89c03bca52467bf38b22cf7d8efb7589929d9b`, 4974 bytes, 111 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/Hexalith.Conversations.MinimalModule.Tests.csproj` (`6232260db2a9496548739fbd86398cb75a48208454c1860f9684a7d8d4e1f11f`, 745 bytes, 17 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/MinimalModuleFixtureTests.cs` (`41a103f4f0d316f9f3ec55a3eed6baaf9c37d0d50fcce7ce20b7a977c1575b8a`, 6904 bytes, 155 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/Hexalith.Conversations.MinimalModule.AppHost.csproj` (`320077e5282671bbe921ce01c3ed7f8680d096f31a499e0e41a32ff49a02e36f`, 851 bytes, 14 lines)
- `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/AppHost.cs` (`42e0d71777bc9456a600c8bc8618fa3e6f3d6c4a95a5dfffdee9056d985175a6`, 685 bytes, 12 lines)

## Resolved public platform APIs

- Platform root: `references/Hexalith.EventStore`
- Project `Hexalith.EventStore.Aspire`: `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj` (`a05fa59392fd33324d0a37a502a91fc3f707d64be1e0accb860ae574c8060656`)
- Project `Hexalith.EventStore.Client`: `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj` (`a1dce85420fd2a7e7f4154a32e7276d519639ff25fe368de33876aa4c75445e4`)
- Project `Hexalith.EventStore.Contracts`: `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj` (`6ae249d8b25d97f6b1620f965730f317e1e67537ce478fa791725a94cb0b859a`)
- Project `Hexalith.EventStore.DomainService`: `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj` (`a33f6eff943d309ae17d4278d0b2e7dafd2fab2c14c1134c01821ac622cd99db`)
- Project `Hexalith.EventStore.ServiceDefaults`: `references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj` (`9016f62711665a16f65946bec0a96876951cf2be3006bd9bbf0c30dd0adbf415`)
- Assembly `Hexalith.EventStore.Aspire`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/bin/Release/net10.0/Hexalith.EventStore.Aspire.dll` (`fcf90a8104945bba65c8b8f909f6c69f826b815616f4ba0fc041322ca142fb54`)
- Assembly `Hexalith.EventStore.Client`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.Client.dll` (`4a721fc9ca66cb86019378e3bead72d3b15fef1e8439ae784e0b8e7299381484`)
- Assembly `Hexalith.EventStore.Contracts`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.Contracts.dll` (`23b56a11fe72879e242f2a072011a38a2af6ca1d2172a47972b01d4eb9b80e69`)
- Assembly `Hexalith.EventStore.DomainService`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.DomainService.dll` (`3e8df9027828a8f57d7715e57d9dc466c977d2e5fbf95e1a2776f13870cba98f`)
- Assembly `Hexalith.EventStore.ServiceDefaults`: `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.EventStore.ServiceDefaults.dll` (`1326033fe4aa1e7157c7a7108739a3d8f8a60b04a86870908eb24f21b7f78fbd`)

## Restore

- Command: `dotnet restore tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --locked-mode`
- Locked mode: true
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/packages.lock.json` (`52cbaccb8876b9df65fa7e691cf18b6755378e69aa50451ad5e4e211fcac0314`, 2 packages)
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/packages.lock.json` (`d9f8362c9a38b3ad08481559e89dbebc92c474a06e1579923182f4419796295c`, 66 packages)
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/packages.lock.json` (`78b06f0a95f4be921ace42500c2eba6e7578c0dccb162a2400d838c9b15a2423`, 78 packages)
- Lock file `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/packages.lock.json` (`7a55c490e0263300eb97079fffc684ca61b1f00acd057bcf213f7f743744d104`, 93 packages)
- Resolved packages: 171 (`7bba507fbbd79a36f7020e765e09c48a4ce00b7a1b998302d5abac9442df59f3`)

## Build

- Command: `dotnet build tests/fixtures/Hexalith.Conversations.MinimalModule/Hexalith.Conversations.MinimalModule.slnx --configuration Release --no-restore`
- Configuration: Release
- Warnings as errors: true
- .NET SDK: `10.0.402`
- Aspire AppHost SDK: `Aspire.AppHost.Sdk/13.6.1`
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Contracts/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Contracts.dll` (`3214f4b934ec2e1f8286b31e04534f1727f38b904c363ce1943a62ed9c22e75e`)
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Server.dll` (`70cb7e8da4ade3bb6b0a71fa53e395bd6ca736a2a81863ff5eb52e6286304e54`)
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Tests.dll` (`6fda9dbda196001b578952f5f210648b2cd8d86e7996fade8ec5f7e72af69e28`)
- Assembly `tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.AppHost/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.AppHost.dll` (`4492362545f75995b68f950a48c0d5413eedcee0c2374563fa167bbc94663bfc`)

## Tests

- Command: `dotnet tests/fixtures/Hexalith.Conversations.MinimalModule/tests/Hexalith.Conversations.MinimalModule.Tests/bin/Release/net10.0/Hexalith.Conversations.MinimalModule.Tests.dll -automated sync -failSkips -trx artifacts/v9/11.2/minimal-module.trx`
- xUnit v3: `4.0.2`
- Result: `artifacts/v9/11.2/minimal-module.trx` (`38abbe67ca1d3d26d40f21b3b07f3ed897fd7684a5129ec823d152206dd2ea77`)
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
- Signable payload: `91924bab9eafedbaf7eeffe298867cbd18d31bfc071dacca8e06a2dcc767ca41`
