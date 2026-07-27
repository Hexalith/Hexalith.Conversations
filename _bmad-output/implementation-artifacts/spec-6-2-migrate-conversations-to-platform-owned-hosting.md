---
title: 'Migrate Conversations to platform-owned hosting'
type: 'refactor'
created: '2026-07-26T21:00:00+02:00'
status: 'blocked'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/adrs/0003-projection-read-store-population-proof.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Conversations' AppHost currently remains publishable and is not explicitly bounded to module-scoped user testing, while its ServiceDefaults project and remaining host boilerplate duplicate capability assigned to the platform. Production EventStore named projection dispatch also has no Conversations async handler that durably populates the query read store.

**Approach:** Freeze the required SM-C2 baseline; retain `Hexalith.Conversations.AppHost` only as a non-packable, non-publishable harness for Conversations-limited local user and end-to-end tests; remove the module-owned ServiceDefaults facade and any duplicated generic runtime plumbing; and connect the production named projection route to the existing Conversations materializer and read-model writer. Production/deployment composition and reusable hosting capability remain platform-owned. Preserve topology behavior, security, health, publication, admin composition, public contracts, and production query behavior with state-store and replay evidence.

## Boundaries & Constraints

**Always:** Complete Story 6.7 before promotion-bearing completion; capture or reconstruct the versioned `sm-c2-hot-path-inventory-v1` baseline before runtime, projection, or topology edits; retain the existing AppHost source, focused tests, and solution entries with `IsPackable=false` and `IsPublishable=false`; limit its resources to Conversations Server/Admin Web plus required platform dependencies for module-scoped user/end-to-end tests; keep the canonical `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()` runtime host; keep legacy `IDomainProjectionHandler` behavior for v1 compatibility; report named projection completion only after both tenant-scoped detail/summary and tenant index writes are durable; update affected root submodules and exact mode-`160000` gitlinks through the Story 6.7 gate; prove behavior through public production append/replay and query surfaces exercised by the test harness.

**Block If:** Story 6.7's mechanical promotion gate is not complete; the pre-change SM-C2 baseline cannot be captured or reconstructed from preserved source and environment evidence; the local AppHost cannot be made mechanically non-packable/non-publishable or cannot exercise the production Server/EventStore boundaries; a required generic capability has no approved owning platform surface; an affected submodule commit cannot satisfy cleanliness, remote-availability, and exact-gitlink rules.

**Never:** Pack, publish, deploy, or describe `Hexalith.Conversations.AppHost` as a production composition root; move the module test harness into FrontComposer.AppHost or EventStore.AppHost; introduce a Conversations-owned reusable Aspire library or retain a generic ServiceDefaults/DAPR/health/telemetry/projection/query/publication/subscription facade; hide generic platform gaps behind Conversations; call `MapEventStoreDomainService()` directly; treat direct writer calls, DI resolution, mock counts, HTTP acceptance, or the legacy opaque projection response as production population proof; let queries replay or silently backfill; mutate signed v1 evidence, historical Epic 1-5 records, or Story 6.5 authoring-template evidence; initialize or traverse nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Accepted append | Authorized conversation event reaches EventStore named dispatch | Both exact read-model keys are durable and production detail/list queries return current tenant-scoped data | No completion before both writes |
| Duplicate delivery | Same stable dispatch identity is delivered again | Logical state is unchanged and tenant index contains one entry | Return bounded already-completed/idempotent outcome |
| Partial write | Detail write succeeds and index write fails | Query must not report falsely current state; durable retry converges both keys | Non-completed bounded outcome; no raw exception leakage |
| Cross-tenant access | Tenant B addresses tenant A data | No read, write, inference, or shared key is observable | Fail closed with Conversations-safe status |
| Derived-state loss | Detail and index are deleted, then authorized full replay runs | Replay restores logically equivalent detail and duplicate-free index through production dispatch | Rebuild/replay remains side-effect safe and exposes bounded failure state |
| Backend failure | Configured read-model store is unavailable or uncertain | No completed projection outcome; production query is unavailable/non-current | Retryable or indeterminate bounded reason |
| Module user-test startup | The retained Conversations AppHost starts a local test topology | Only Conversations Server/Admin Web and required platform dependencies are composed through public platform helpers; production Server/EventStore boundaries are reachable | Fail startup with an actionable bounded configuration error; do not add local runtime facades |
| Pack or publish attempt | Build tooling evaluates the Conversations AppHost | The project is mechanically non-packable and non-publishable and produces no deployment artifact | Conformance failure if either property is enabled or a publishable artifact is produced |

</intent-contract>

## Code Map

- `src/Hexalith.Conversations.AppHost/` -- retain as the non-packable, non-publishable module user/end-to-end test harness; thin its topology to public platform helpers and module-specific resource selection.
- `tests/Hexalith.Conversations.AppHost.Tests/` -- retain and extend the topology contract to prove module-only scope, public-helper consumption, and the pack/publish prohibition.
- `src/Hexalith.Conversations.ServiceDefaults/` and its tests/solution entries -- remove as an unused generic wrapper; EventStore DomainService already owns runtime defaults.
- `src/Hexalith.Conversations.Server/Program.cs` -- retain the two-line canonical host and remove generic `DaprClient` boilerplate only after EventStore owns it.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs` -- legacy synchronous route whose decoding/materialization rules must be shared with the new named route.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs` -- two-key persistence path requiring completion visibility and bounded partial-write outcomes.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs` -- exact tenant-scoped detail and index key contract.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/` -- canonical host, discovery, `/project/v2`, and generic `DaprClient` registration owner.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/` -- shared domain-module composition and possible typed gateway-registration helper owner.
- `tests/Hexalith.Conversations.IntegrationTests/` -- production-boundary append/replay, state-store end-state, and query proof lane.
- `docs/release-evidence/` -- new authoritative `projection-read-store-population-proof-v2` JSON and derived Markdown; signed v1 files remain unchanged.

## Tasks & Acceptance

1. Complete Story 6.7's promotion gate and capture/reconstruct one comparable baseline for every frozen SM-C2 row before changing runtime, projection, or topology behavior.
2. Retain `Hexalith.Conversations.AppHost`, its tests, and solution entries; set the project to non-packable/non-publishable and constrain `ConversationsAppHostTopology` to module-test composition through public EventStore/Commons helpers.
3. Remove `Hexalith.Conversations.ServiceDefaults`, its tests, and solution entries; remove generic `DaprClient` registration from `Server/Program.cs` only after the approved EventStore public surface supplies it.
4. Add the scoped Conversations `IAsyncDomainProjectionHandler` path, reuse the existing materializer, and make both read-model writes part of the durable completion outcome with bounded idempotent retry semantics.
5. Extend AppHost, server, integration, and conformance tests for the full matrix above, including actual state-store end state and production query results after accepted append and authorized replay.
6. Produce `projection-read-store-population-proof-v2`, the post-SM-C2 comparison, exact affected-submodule declarations/gitlinks, and corrected topology evidence without mutating signed v1 artifacts.

**Acceptance criteria:**

- **Given** the retained AppHost project, **when** its evaluated build properties and composition model are inspected, **then** it is non-packable/non-publishable, contains only the module test topology, and consumes public platform helpers without duplicating generic runtime capability.
- **Given** an authorized production-boundary append or full replay executed through the module test harness, **when** named projection dispatch completes, **then** both exact tenant-scoped read-model keys are durable and the production detail/list queries return the expected state.
- **Given** duplicate delivery, a partial write, cross-tenant input, backend uncertainty, or derived-state deletion, **when** retry/replay runs, **then** the bounded outcomes and convergence rules in the matrix hold without direct-writer proof shortcuts.
- **Given** the pre/post hot-path evidence, **when** SM-C2 is evaluated, **then** every frozen row satisfies `post P95 <= 1.05 x baseline P95` under the identical recorded envelope.

## Spec Change Log

- 2026-07-27: Resolved the AppHost ownership gap. Retain the existing AppHost only as a non-shipping Conversations user/E2E test harness; production deployment and reusable runtime capability remain platform-owned. Story 6.7 and the SM-C2 baseline remain prerequisites.

## Review Triage Log

## Design Notes

The AppHost ambiguity is resolved by separating test composition from production ownership. `Hexalith.Conversations.AppHost` remains in this repository only to run module-limited local user and end-to-end tests. It must not be published or deployed. Story 6.2 does not move this harness into FrontComposer or EventStore and does not choose a centralized product AppHost; it consumes the approved platform runtime surfaces while production deployment composition remains platform-owned.

The production projection seam exists in EventStore, but the Conversations handler, two-key completion visibility, authorized replay behavior, and live proof do not. A direct handler or writer test cannot substitute for accepted append/replay crossing `ProjectionUpdateOrchestrator` and `NamedProjectionDispatchCoordinator` into `/project/v2`, then asserting configured state-store end state and the production query result.

## Verification

**Commands after the blockers are resolved and the spec is repaired:**
- `dotnet restore Hexalith.Conversations.slnx` -- expected: all root and declared source-mode dependencies restore.
- `dotnet build Hexalith.Conversations.slnx --configuration Release -m:1 -p:NuGetAudit=false` -- expected: zero warnings and errors with the test-only AppHost retained and the generic ServiceDefaults project absent.
- `tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net10.0/Hexalith.Conversations.AppHost.Tests -class Hexalith.Conversations.AppHost.Tests.ConversationsAppHostTopologyTest` -- expected: the retained harness is module-scoped, consumes public platform helpers, and is non-packable/non-publishable.
- `tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests -class Hexalith.Conversations.Server.Tests.Projections.ConversationAsyncProjectionHandlerTest` -- expected: all named-handler and two-key outcome cases pass.
- `tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests -class Hexalith.Conversations.IntegrationTests.Projections.ConversationProjectionReadStorePopulationLiveTests` -- expected: all production-dispatch, state-store, query, retry, isolation, deletion, and replay cases pass with no skips.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest` -- expected: v2 evidence hashes, scenarios, commands, environment, gitlinks, and summaries validate.

## Auto Run Result (2026-07-26; superseded by the 2026-07-27 resolution)

Status: blocked

Blocking condition: intent gap and unmet binding prerequisites.

Resolved decision: Keep `Hexalith.Conversations.AppHost` in this repository only as a mechanically non-packable/non-publishable, module-scoped user and end-to-end test harness. No external repository becomes the owner of this harness. Production deployment composition and reusable runtime capability remain platform-owned.

Evidence:

- Neither candidate currently composes Conversations; choosing one changes repository ownership, topology, deployment, test relocation, promotion declarations, and gitlinks.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` records Story 6.7 as `backlog`, while Epic 6 authority requires `6.1 -> 6.7 -> 6.2` and makes its mechanical promotion gate a prerequisite for affected submodules.
- No executable or captured SM-C2 baseline artifact exists for frozen rows `HP-CREATE`, `HP-APPEND`, `HP-LIST`, and `HP-OPEN`; authority requires that baseline before topology edits and post-P95 comparison at no more than `1.05 x` baseline.
- `src/Hexalith.Conversations.Server/Program.cs` still registers `DaprClient` because the canonical EventStore host does not, demonstrating an EventStore-owned generic gap and unavoidable promotion-bearing work.
- ADR 0003 and code inspection confirm no scoped Conversations `IAsyncDomainProjectionHandler` populates the production query store; current tests call `ConversationProjectionReadModelWriter` directly and cannot satisfy the required public production boundary.
