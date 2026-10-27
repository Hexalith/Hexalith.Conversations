# Epic 16 enrollment/adoption remediation plan

Prepared 2026-10-27 from source `109ef750a1ed0d1b02ba7efee8807d611fbad130`.
The user selected preparation of a separate remediation plan. This document initially authorized planning only. The user subsequently approved
implementation of the separate spec and its scoped offline baseline exception.
The approved spec controls execution; runtime launch, pin changes and owner
acceptance remain outside its scope.

## Goal and first implementation slice

Bring active Conversations build/configuration consumers into the declared
Platform conventions while preserving accepted fixtures and showing every
remaining enrollment blocker. The implementation spec is
[the separate draft](spec-epic-16-enrollment-adoption-remediation.md).

The 2026-10-26 accepted-CLI report contains 24 failures. **Fourteen** are
eligible for this first slice: eleven build/helper selectors and three Dapr
findings. Ten remain outside the slice: the declaration/tool failure, six
retained captures, the accepted MinimalModule fixture, and two frozen hosting
project/consumer files. These are scope dispositions, not waivers or PASS rows.
The host inventory says to retain the exact project and consumers until its
replacement gates pass; this plan conservatively leaves both named project
files unchanged. A dependency-only freeze amendment needs an explicit scope.

## Complete observed failure inventory

Source: [raw accepted-tool report](../../module/validation/epic-16-entry-2026-10-26/adoption-json.stdout.txt).
No new native observation was made on 2026-10-27.

| Requirement | Exact path | Planned disposition |
| --- | --- | --- |
| declaration | `module/hexalith.module-manifest.v2.json` | Tool qualification |
| dependency-selectors | `Directory.Build.props` | First implementation slice |
| dependency-selectors | `Directory.Packages.props` | First implementation slice |
| dependency-selectors | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/Directory.Build.props` | Retained capture |
| dependency-selectors | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/references/Hexalith.EventStore/Directory.Build.props` | Retained capture |
| dependency-selectors | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/references/Hexalith.Tenants/Directory.Build.props` | Retained capture |
| dependency-selectors | `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` | Frozen host / direct consumer |
| dependency-selectors | `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj` | First implementation slice |
| dependency-selectors | `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj` | First implementation slice |
| dependency-selectors | `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` | First implementation slice |
| dependency-selectors | `src/Hexalith.Conversations/Hexalith.Conversations.csproj` | First implementation slice |
| dependency-selectors | `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` | Frozen host / direct consumer |
| dependency-selectors | `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` | First implementation slice |
| dependency-selectors | `tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` | First implementation slice |
| dependency-selectors | `tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj` | First implementation slice |
| dependency-selectors | `tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` | First implementation slice |
| dependency-selectors | `tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj` | First implementation slice |
| dependency-selectors | `tests/fixtures/Hexalith.Conversations.MinimalModule/Directory.Build.props` | Accepted MinimalModule fixture |
| dapr-subscriptions | `module/validation/epic-16-prerequisite-implementation/review-iteration-4/implementation/protocol-goldens-final/ConversationProjectionReadModelKeys.cs` | Retained capture |
| dapr-subscriptions | `module/validation/epic-16-prerequisite-implementation/review-iteration-4/implementation/protocol-goldens/ConversationProjectionReadModelKeys.cs` | Retained capture |
| dapr-subscriptions | `module/validation/epic-16-prerequisite-implementation/review-iteration-9/implementation/source-conventions/references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` | Retained capture |
| dapr-subscriptions | `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` | First implementation slice |
| dapr-subscriptions | `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs` | First implementation slice |
| dapr-subscriptions | `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs` | First implementation slice |

## Dependency work

1. `Directory.Build.props`: remove sibling/root discovery and assignments to
   tool-owned root/mode properties. Keep ordinary framework/analyzer settings.
   Only the enrollment kit's inert direct `HexalithBuildsRoot` bootstrap is
   permitted; do not replace root probes with another module-owned selector.
2. `Directory.Packages.props`: import centralized versions from the mapped
   Builds root; remove ancestor search and alternate dependency selectors.
3. Update exactly the eight non-frozen project files in the table and
   `ContractPackageInventoryTest.cs` to consume tool-selected project/package
   pairs. Keep Contracts/Client package identities and in-repository references.
   Replace the inventory test's two-layout assertion with meaningful mapped
   dependency assertions. Do not simply change expected XML counts.
4. Add mapped MSBuild integration coverage in
   `tests/Hexalith.Conversations.Contracts.Tests/PlatformEnrollmentMappingTest.cs`
   and its test-project reference if needed. Use Builds' public
   `SourceMappingMaterializer.WriteAsync` and `Environment`, with the existing
   `WorkspaceMsBuildTests` patterns, instead of recreating root discovery or
   mapping generation. Test actual evaluated reference identities, source/Debug,
   package/Release evaluation, unmapped dependencies, and competing siblings.
   Local compilation remains Debug; Release artifact execution belongs to CI or
   separate owner qualification. No public map-only CLI was found. `validate`
   never resolves a mapping. Synthetic mapping tests are not an ordinary
   Platform run, enrollment acceptance or deployed parity.

## Configuration work

The declared read-model component key is
`EventStore:Projections:ReadModelStateStoreName`. Read it at composition and
supply one immutable binding consistently to existing SDK read/write, batch
scope, pending recovery and native-owner-proof calls. The current constant is
in `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs`.
The SDK `IReadModelStore` takes the store name explicitly; its registration
does not automatically bind this manifest key. Keep all tenant key bytes and
persistence/CAS algorithms unchanged. Do not mutate process-global static state.

Current production consumers requiring coordinated updates:

- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationResolutionMetadataPersistence.cs`
- `src/Hexalith.Conversations.Server/Projections/ConversationResolutionNativeProofRegistration.cs`
- `src/Hexalith.Conversations.Server/Queries/GetConversationAdvisoryMetadataDomainQueryHandler.cs`
- `src/Hexalith.Conversations.Server/Queries/ConversationResolutionNativeEvidence.cs`

Bind through `Queries/ConversationQueryServiceCollectionExtensions.cs` and
enrollment composition in `Program.cs`; preserve existing public call paths
with additive overloads where required. A new single-type
`Projections/ConversationProjectionStoreOptions.cs` may carry the module
binding; it must not implement persistence or invent physical component names.
Resolve missing/blank enrolled configuration before any store operation.

Use the existing Tenants `configureOptions` overload in
`TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs` to consume
`Tenants:PubSubName` and `Tenants:TopicName`. Enrolled composition must supply
`Dapr:DeclarativeSubscriptions=true`; qualify configured event POST routes
without subscription discovery using the existing SDK host. Use SDK
`AddEventStoreDataProtection(IConfiguration, stableApplicationName)` rather than
a module key-ring implementation. Preserve cursor purpose and workload
authentication/authorization. Key-ring restart behavior remains a live
qualification requirement, not something DI inspection proves.

Update the two flagged IntegrationTests/Projections files to use injected,
deliberately non-default test component names. Carry the same name through
fixture YAML generation, service configuration, SDK calls and persisted-state
assertions; do not rename a forbidden literal merely to evade the scanner.

Affected existing unit coverage includes
`Server.Tests/Projections/{ConversationAsyncProjectionHandlerTest,ConversationProjectionReadStoreFailClosedTest,ConversationProjectionReadModelPersistenceTest}.cs`
and `Server.Tests/TenantAccess/ConversationTenantAccessRegistrationTest.cs`.
Add `Server.Tests/Projections/ConversationProjectionConfigurationTest.cs` for
non-default stores, absent/blank enrollment values, native proof alignment, and
two simultaneous service providers with different bindings. Test state contents
and untouched other-store data rather than just service registration counts.

## Upstream qualification and evidence owners

- **Builds/Platform tool:** the isolated 4.38.0 artifact fixes token-key format
  but is not integration-qualified. Current Builds source contains later runtime
  corrections; it is not proof that 4.38.0 includes them. The historical B6
  structured-list-cancellation concern still applies by inspection. Reconcile
  the full retained review inventory against the exact selected published
  artifact, including platform-specific discovery tests. Keep accepted 4.35.2
  pins and all root gitlinks unchanged in this slice. Qualifying/releasing a
  successor and accepting a release/source pair are separate owner work.
- **Retained captures and frozen fixture:** Builds' scanner uses all root
  `git ls-files -z` supported source paths. It has no general archive exemption;
  only Dapr checks exclude exact verified frozen-host directories. Source
  snapshots are therefore real FAIL rows. Do not change extensions, compress,
  relocate, delete, exclude or rewrite accepted evidence in this slice. A later
  evidence migration needs exact identities, preserved bytes and all reader/
  replay updates under its own approved contract; a broad scanner skip is not
  an acceptable remediation.
- **Frozen hosting:** preserve both inventoried hosts, their topology,
  non-packable/non-publishable flags and consumers. The host-related selector
  findings remain pending until the owner scopes an amendment. No exemption,
  relocation or retirement decision is made here.
- **Platform registration:** create `module/platform-handoff.md` from the direct
  Platform template. List executable/image bindings, scoped configuration,
  token adapters, EventStore gateway routing tuples, topic/dead-letter/ACL/mTLS
  policy and exact source/package/deployed parity requirements. Keep owners'
  acceptance and evidence pending; a declaration does not enroll an executable.
- **Operational and human gates:** preserve the draft operational envelope and
  pending SM-2 checklist. Provider parity, recovery drills, named coverage/deputy
  access, capacity/waiver disposition, human breakpoint/edit evidence and scoped
  owner entry remain separate. No machine result can supply them.

## Verification sequence for the implementation slice

1. Record exact current source/tool/dependency identities and the full native
   inventory before edits. This plan reuses the retained 2026-10-26 observation.
2. Build the Contracts and Server test projects individually in Debug using
   generated mapping imports; run focused native xUnit assemblies with `-class`
   or `-method`, then affected suites. Do not run solution-level `dotnet test`.
3. Evaluate package-mode references using the same mapping machinery; this
   source check does not establish Release/package runtime qualification.
4. Capture ordinary and adoption validation in both output formats under the
   exact accepted tool, into a fresh `module/validation/` observation directory.
   Require all fourteen targeted identities to disappear as failures; retain
   the ten known outside-slice failures and every unexpected new one. Count
   from actual results rather than hardcoding a successful summary.
5. Scans must name their tracked candidate. New untracked source is outside the
   native scanner's inventory; no final qualification may be claimed before an
   owner supplies the intended reviewed/tracked candidate. This plan permits
   no staging or commits merely to change scanner coverage.
6. Run both Story 7.3 completion guards and the six actual Story 16.1 contract
   commands. Story 16.1 must remain BLOCKED/ENTRY_QUALIFICATION_PENDING with no
   runtime receipts or final record; entry null, trust empty, status/backlog
   and ACTIVE hold unchanged.
7. Compare all protected fixture/archive/hosting bytes, exact raw gitlinks and
   accepted pins against entry; inspect ordinary diffs and whitespace. Existing
   historical checker FAILs are not repaired or reinterpreted as PASS.

This slice needs no deployment, secret or live-provider operation. No product
runtime launch, operator drill, debugger session or real workload subscription
delivery is authorized. Bounded unit-test/in-process host construction and
disposable test stores may verify composition; they cannot claim live parity.
The user approved the separate spec with an explicit exception to the pre-edit
AppHost launch: mapped Debug builds and isolated composition tests are the
baseline for this prerequisite slice. Do not launch the held runtime or infer
live qualification from that exception.

## Completion meaning

The first slice delivers source corrections and a truthful pending handoff.
It may complete with native adoption still FAIL on explicit outside-slice rows.
Full enrollment and Story 16.1 entry remain blocked until those rows and all
owner qualifications pass separately. No partial result is renamed PASS.
