---
title: 'Migrate Conversations to platform-owned hosting'
type: 'refactor'
created: '2026-07-27T14:49:16+02:00'
status: 'superseded'
superseded_on: '2026-07-27'
superseded_by:
  - '_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md'
supersession_note: >-
  This spec route implemented the bulk of Story 6.2 before the story record existed, and was never code reviewed
  (warnings oversized, multiple-goals; review_loop_iteration 0). Its acceptance is now owned by the Story 6.2
  record, which binds the same work to the epic's acceptance criteria, carries the five open gaps as tasks T1-T5,
  and routes completion through the Story 6.7 promotion gate. Closed as superseded so it cannot be mistaken for a
  second live authority; the intent contract below is retained unchanged as the implementation's origin record.
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/adrs/0003-projection-read-store-population-proof.md'
warnings:
  - oversized
  - multiple-goals
submodule_promotions:
  - path: references/Hexalith.EventStore
    require_remote: true
---

<intent-contract>

## Intent

**Problem:** Conversations still carries a publishable AppHost, an unused reusable ServiceDefaults facade, and explicit generic DAPR registration, while its persisted query store is not populated by EventStore's production named-projection path. The required pre-change performance record and v2 production-path proof also do not exist.

**Approach:** First capture the frozen four-row SM-C2 baseline without changing runtime behavior. Then retain the AppHost only as a non-packable, non-publishable module test harness, move generic host ownership to EventStore, remove Conversations ServiceDefaults, connect named append/replay dispatch to idempotent two-key projection persistence, and produce production-boundary state/query and performance evidence.

## Boundaries & Constraints

**Always:** Treat Story 6.7 as the promotion gate; record `HP-CREATE`, `HP-APPEND`, `HP-LIST`, and `HP-OPEN` baseline P95 values before runtime, projection, or topology edits; retain the AppHost and its solution/test entries only for module-scoped local user/E2E verification with `IsPackable=false` and `IsPublishable=false`; keep `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()` canonical; preserve legacy `IDomainProjectionHandler` v1 behavior; use `ConversationProjectionReadModelWriter`, `ReadModelWritePolicy`, and configured `IReadModelStore`; report named dispatch complete only after both tenant-scoped keys are durable; make reads non-current when the two keys do not prove the same completed generation; keep EventStore history authoritative; promote the EventStore commit and exact root gitlink through the mechanical gate.

**Block If:** Comparable SM-C2 baseline evidence cannot be captured now or reconstructed from the preserved source commit; the local AppHost cannot exercise the production Server/EventStore boundaries while remaining mechanically non-shipping; production full replay cannot reach a Conversations rebuild-capable named handler; sequential-write completion cannot be made observable without contradicting accepted ADR 0003; or the EventStore promotion is dirty, unavailable from a remote-tracking ref, or absent from the committed mode-`160000` gitlink.

**Never:** Publish/deploy the Conversations AppHost or describe it as production composition; move the harness into FrontComposer/EventStore; add a Conversations Aspire/runtime facade; retain generic ServiceDefaults, DAPR, health, telemetry, query, projection, publication, or subscription plumbing; call `MapEventStoreDomainService()` directly; introduce direct DAPR state writes or query-time replay/backfill; claim atomic multi-key persistence; use direct writer calls, DI resolution, mock counts, HTTP acceptance, or the legacy opaque response as proof; mutate signed v1 evidence, frozen Epic 1-5 history, or nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Accepted append | Authorized event reaches EventStore named dispatch | Both exact keys are durable; production detail/list queries expose the same current generation | Complete only after both writes |
| Duplicate or partial delivery | Stable dispatch repeats, including after detail succeeds and index fails | Retry converges to one logical detail and one duplicate-free index row; neither query falsely reports a partial generation as current | Retryable/indeterminate bounded outcome; no raw storage detail |
| Tenant/store failure | Cross-tenant request or unavailable/uncertain store | No foreign key, row, inference, completed outcome, or falsely current query is observable | Fail closed with Conversations/platform-safe outcome |
| Rebuild | Derived keys are deleted and authorized full replay runs | Production rebuild dispatch restores equivalent keys and query results without external side effects | Bounded rebuilding/failure state until convergence |
| Test harness | AppHost starts or packaging tooling evaluates it | Only EventStore, Conversations Server, Admin Web, and required platform dependencies compose; no pack/publish artifact is enabled | Actionable startup/conformance failure |

</intent-contract>

## Code Map

- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` and `ConversationsAppHostTopology.cs` -- retained module test harness; currently publishable and uses the lower-level Commons Aspire helper.
- `src/Hexalith.Conversations.ServiceDefaults/` and `tests/Hexalith.Conversations.ServiceDefaults.Tests/` -- unused packable generic wrapper to remove with solution/scaffold references.
- `src/Hexalith.Conversations.Server/Program.cs` -- canonical two-line host plus temporary explicit `AddDaprClient()` gap.
- `src/Hexalith.Conversations.Server/Projections/` -- legacy decoder/materializer, sequential two-key writer, persisted read store, and new named/rebuild handler landing zone.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` -- platform owner for canonical DAPR-client registration and handler discovery.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs` -- public module-sidecar wrapper for the retained harness.
- `tests/Hexalith.Conversations.IntegrationTests/` -- production-boundary projection proof and repeatable SM-C2 workload lane.
- `docs/release-evidence/` -- immutable baseline/post/projection proof artifacts; signed v1 evidence is read-only.

## Tasks & Acceptance

**Execution:**
- `tests/Hexalith.Conversations.IntegrationTests/Performance/SmC2HotPathBenchmark.cs` and `docs/release-evidence/sm-c2-hot-path-baseline-v1.{json,md}` -- add a repeatable warm-path fixture and capture commit-bound raw samples/P95 for every frozen row before any production edit; later emit `sm-c2-hot-path-post-v1.{json,md}` with the identical workload, data, concurrency, runtime/tool versions, repetitions, and processing.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs` and `references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/EventStoreDomainServiceExtensionsTests.cs` -- make canonical domain-service registration own idempotent `DaprClient` setup and prove it; promote the resulting commit with `require_remote: true`.
- `src/Hexalith.Conversations.Server/Program.cs`, `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs`, `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs`, and `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs` -- remove explicit generic DAPR ownership only after the promoted EventStore surface supplies it, while preserving the canonical host and dependency boundaries.
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`, `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs`, and `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs` -- make the harness mechanically non-shipping, use EventStore's public domain-module helper, preserve the EventStore reference/wait, identity, `/alive`, security, and exact module-only topology, and prove evaluated properties.
- `src/Hexalith.Conversations.ServiceDefaults/`, `tests/Hexalith.Conversations.ServiceDefaults.Tests/`, `Hexalith.Conversations.slnx`, `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`, and `README.md` -- delete the unused facade/tests, remove only their solution/scaffold expectations, and document platform ownership plus the test-only AppHost exception.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs`, `src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs`, `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs`, `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs`, `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs`, and `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs` -- share legacy decoding, add scoped named and rebuild-capable dispatch, map platform outcomes safely, keep separate idempotent policy writes, and ensure query surfaces cannot trust a detail/index generation until both keys agree.
- `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationAsyncProjectionHandlerTest.cs`, existing projection writer/read tests, and `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationProjectionReadStorePopulationLiveTests.cs` -- cover accepted append, stable duplicate, injected second-write failure/retry, unavailable store, tenant isolation, derived-state deletion, full replay, exact configured state-store keys, and production detail/list results without direct-writer proof shortcuts.
- `docs/release-evidence/projection-read-store-population-proof-v2.{json,md}` and `tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs` -- bind commands, environment, source hashes, EventStore gitlink/promotion result, dispatch outcomes, exact store/query evidence, replay equivalence, and SM-C2 post comparison; validate source JSON mechanically and leave signed v1 bytes unchanged.

**Acceptance Criteria:**
- Given the frozen inventory and unchanged pre-correction runtime, when the baseline is captured or reproducibly reconstructed, then all four rows have commit-bound raw warm samples and P95 values under one recorded envelope before any production edit.
- Given the retained AppHost and removed ServiceDefaults facade, when evaluated build properties, topology, solution membership, and public helper effects are inspected, then the harness is non-packable/non-publishable, module-scoped, and contains no reusable Conversations hosting capability.
- Given an authorized append or full replay through the production EventStore coordinator/dispatcher, when named Conversations projection processing completes, then both exact tenant-scoped keys are durable and production detail/list queries return the same current event position.
- Given duplicate delivery, second-write failure, backend uncertainty, cross-tenant input, deletion, or replay, when retry/rebuild executes, then bounded outcomes and convergence match the matrix without false completion/currentness, duplicate index entries, leakage, or query-time backfill.
- Given the promoted EventStore change, when the Story 6.7 checker evaluates the committed umbrella candidate, then `references/Hexalith.EventStore` is clean, remotely available, and recorded by the exact mode-`160000` gitlink.
- Given the post-run evidence, when SM-C2 and proof validation execute, then every frozen row satisfies `post P95 <= 1.05 x baseline P95`, production-path evidence is hash-bound and complete, and signed v1 artifacts remain byte-identical.

## Spec Change Log

## Review Triage Log

## Design Notes

The AppHost exception is about test composition, not runtime ownership: the project stays only because it exercises Conversations' production boundaries locally, while pack/publish/deployment remain impossible. ADR 0003 intentionally keeps two idempotent policy writes and makes second-write uncertainty non-completion; the implementation must expose cross-key generation consistency to queries rather than claim atomicity or hide lag through read-time repair.

## Verification

**Commands:**
- `dotnet restore Hexalith.Conversations.slnx -p:UseHexalithProjectReferences=true` -- expected: all source-mode dependencies restore.
- `dotnet build Hexalith.Conversations.slnx --configuration Release -m:1 -p:NuGetAudit=false -p:UseHexalithProjectReferences=true` -- expected: zero warnings/errors; AppHost retained and Conversations ServiceDefaults absent.
- `dotnet msbuild src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj -getProperty:IsPackable -getProperty:IsPublishable` -- expected: both properties are `false`.
- `tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net10.0/Hexalith.Conversations.AppHost.Tests -class Hexalith.Conversations.AppHost.Tests.ConversationsAppHostTopologyTest` -- expected: all test-harness topology checks pass.
- `tests/Hexalith.Conversations.Server.Tests/bin/Release/net10.0/Hexalith.Conversations.Server.Tests -class Hexalith.Conversations.Server.Tests.Projections.ConversationAsyncProjectionHandlerTest` -- expected: all named-handler, outcome, and partial-write cases pass.
- `tests/Hexalith.Conversations.IntegrationTests/bin/Release/net10.0/Hexalith.Conversations.IntegrationTests -class Hexalith.Conversations.IntegrationTests.Projections.ConversationProjectionReadStorePopulationLiveTests` -- expected: all production dispatch/state/query/rebuild cases pass with no skips.
- `tests/Hexalith.Conversations.Conformance.Tests/bin/Release/net10.0/Hexalith.Conversations.Conformance.Tests -class Hexalith.Conversations.Conformance.Tests.ProjectionReadStorePopulationProofValidationTest` -- expected: all v2 evidence and immutable-v1 checks pass.
- `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline "$(git merge-base origin/main HEAD)" --candidate HEAD --submodule references/Hexalith.EventStore --require-remote references/Hexalith.EventStore --format json` -- expected: `result` is `pass` with no blockers after the committed umbrella candidate records the promoted EventStore revision.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.
