# Story 16.1 entry investigation — 2026-10-25

Planning-only investigation at `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3`.
This report supplies no runtime approval or owner acceptance. The working tree
was clean at entry; runtime and sprint status remain draft/backlog.

## Code Map

- `references/Hexalith.Builds/docs/module-cli-configuration-key-released-artifact-handoff.md` and `module-cli-configuration-key-release-review.md`: published 4.38.0 fixes token validation; unresolved runtime findings require Platform disposition. Both current tool manifests still pin 4.35.2.
- `_bmad/scripts/generate_story_record.py:3597`: only V9/V14 schemas; `verify_story_16_{1,2,3}.py` absent. Adoption v2 pins generator bytes, so changes need additive successor qualification.
- EventStore Client `Projections/IReadModelStore.cs`, `ReadModelWritePolicy.cs`, `Streams/IAuthoritativeEventStreamReader.cs`: reusable CAS/history. Subscription deduplication still precedes payload binding checks.
- Tenants Client `Projections/ITenantProjectionStore.cs`, `Handlers/TenantProjectionEventHandler.cs`: unconditional saves and process-local locks. Conversations `Server/TenantAccess/` reads state/health separately and defaults missing health to Healthy.

## Verification

Current investigation at `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3`:

- `uv run --frozen --no-sync python3 _bmad/scripts/verify_epic_16_adoption_v2.py --repository .` — exit 1; 64 assertions; `SOURCE_BYTES_DRIFT`/`SOURCE_BINDING_INVALID`. Drift: Platform `README.md`, `src/Hexalith.Platform.Composition/PlatformAppModel.cs`, EventStore `src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs`. Entry BLOCKED; hold ACTIVE; four flags false.
- `dotnet tool run hexalith-module --version` — exit 0, `4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`.
- `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption --output json` — exit 1; six PASS/eighteen FAIL; both HXP002 token fields persist. Builds' separate 4.38.0 handoff is historical evidence, not current Platform acceptance.
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py` — exit 0; 162 passed. Portable fixture success does not qualify this checkout.

No runtime build/start or runtime publication; scoped runtime evidence is not-applicable. After entry: individual Debug builds, focused xUnit assemblies, affected suites and scoped verification; no missing required result may pass.

## Proposed prerequisite sequence

1. Platform owns corrected CLI selection. Evaluate the published 4.38.0 handoff
   and runtime review findings before accepting it or a corrected successor.
   Its source is `e171c5a788eab183b32683431b084198c9b8d373`; recorded artifact
   checks pass ordinary validation and 36 malformed-key controls. Those bounded
   results do not settle runtime findings or current enrollment. Review Platform
   `.config/dotnet-tools.json`, `hexalith-platform.json`, accepted catalog/source
   identities and enrollment documentation together. Only after owner acceptance
   align Conversations `.config/dotnet-tools.json` and measure fresh ordinary and
   both adoption formats under a new observation directory. Preserve all earlier
   accepted records and distinguish the current 18 failures from the handoff's
   different 18-failure inventory.
2. Add a separately scoped successor contract/schema and generator dispatch in
   `_bmad/scripts/generate_story_record.py`, with tests in
   `_bmad/scripts/tests/test_generate_story_record.py`; preserve all eighteen ACs,
   thirty-two requirements and graph edges. Implement the fifteen measured
   scenarios in `_bmad/scripts/verify_story_16_{1,2,3}.py` with focused fault tests.
   Missing runtime measurements must block, never become synthetic passes.
   Generator edits invalidate v2's current binding; preserve v2 unchanged and
   publish additive successor bindings. Passing fixtures cannot grant entry.
3. Resolve the real adoption findings through their owning build/runtime paths,
   qualify the operational envelope and SM-2, and obtain the separate scoped
   entry decision. Named operator/deputy coverage, provider/environment parity,
   recovery drills, capacity/waiver disposition and human breakpoint/edit evidence
   cannot be fabricated by this workflow. AppHost exemption/relocation is a later
   16.3 gate and remains separate. No approval of prerequisite work completes 16.1.

## Runtime dependency order after qualified entry

EventStore supplies CAS/recovery and immutable raw-delivery validation before
Tenants adds durable snapshots/transitions; Conversations then consumes one
snapshot. Tenants already exposes a `configureOptions` overload for injected
pubsub/topic configuration. Preserve existing public signatures and reuse
`TenantLocalState.Clone()` and existing event transitions. EventStore's existing
`SharedProjectionEpochCoordinator` supplies durable epoch infrastructure but does
not implement the AD-6 pending horizon/fingerprint protocol.

Existing focused tests include EventStore Client.Tests
`Subscriptions/EventStoreDomainEventProcessorTests.cs`,
`Projections/ReadModelWritePolicyTests.cs` and
`Projections/SharedProjectionEpochCoordinatorTests.cs`; Tenants Client.Tests
`Handlers/TenantProjectionEventHandlerTests.cs` and
`TenantProjectionEventConsumerProductionPathTests.cs`; Conversations Server.Tests
`TenantAccess/ConversationTenantAccessServiceTest.cs`,
`ConversationTenantAccessRegistrationTest.cs` and
`ConversationTenantAccessSharedParityTest.cs`.
