# Test Automation Summary

## Story 3.4 Promote & Adopt the Shared ServiceDefaults Base

Story 3.4 (FR-10, Promote) lifts the duplicated per-module Aspire ServiceDefaults file (OpenTelemetry logging/metrics/tracing, `/health`·`/alive`·`/ready` endpoints, status-code mapping, dev JSON health writer, health-probe trace exclusion, service discovery, HTTP resilience, OTLP env gate) into a new domain-neutral `Hexalith.Commons.ServiceDefaults` base (`HexalithServiceDefaults` + `HexalithServiceDefaultsOptions`), makes `Hexalith.EventStore.ServiceDefaults` a backward-compatible facade over it, and adopts it into the previously-empty Conversations slot via a thin `ConversationsServiceDefaults` wrapper. There is **no UI and no new HTTP endpoint** (internal developer-platform refactor, PRD FR-20) so the automated-test surface is the **registration/extension API** of the base and the Conversations wrapper, plus the existing host-composition route/behavior guardrails — not browser E2E. Framework: xUnit v3 `3.2.2` + Shouldly `4.3.0` (CPM; no new package). The dev-story shipped its tests **unverified** (its sandbox blocked VSTest with `SocketException (13) Permission denied`); the runner works in this environment, which surfaced one real failure and one untested AC.

### Discovered Gaps → Applied (1 new project/file with 7 cases, 1 existing test fixed)
- [x] Gap 1 (AC-2/Task 2 — broken hook-order test, never executed) — `HexalithServiceDefaultsTest.AddHexalithServiceDefaultsShouldExecuteModuleHooksAfterSharedRegistration` asserted eager order `["logging","metrics","tracing","health"]` but observed `["metrics","tracing","health"]`. Root cause: `builder.Logging.AddOpenTelemetry(configure)` registers its callback **lazily** (fires when the logging pipeline materializes), while metrics/tracing/health hooks run eagerly — a strict eager cross-provider order including logging is unprovable, but the logging hook is **not dropped**, it runs later. Fix is test-only (production behavior is correct): assert the deterministic eager order `["metrics","tracing","health"]`, then force logging materialization (`BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger(...)`) and assert the logging hook fired — directly satisfying the AC's "without silently dropping module-specific instrumentation" intent.
- [x] Gap 2 (AC-3 — Conversations thin wrapper had **zero** coverage) — `src/Hexalith.Conversations.ServiceDefaults/ConversationsServiceDefaults.cs`, the entire Conversations-owned adoption surface of this story, was untested (no test project referenced it; only a scaffold path-existence check in IntegrationTests mentioned it). Closed by a new dedicated test project beside the source, per the module convention.

### Generated Tests
- [x] `tests/Hexalith.Conversations.ServiceDefaults.Tests/ConversationsServiceDefaultsTest.cs` — new file, **7 cases**: `ConfigureConversationsDefaults` null-options guard (AC-3 fail-closed); keeps `Hexalith.Conversations` service name (AC-3); registers the `Hexalith.Conversations` meter source (AC-3/AC-5, instrumentation not dropped); preserves the `/health`·`/alive`·`/ready` endpoint contract through adoption (AC-4); `AddConversationsServiceDefaults` null-builder guard (AC-2/AC-3); wires ServiceDiscovery + Resilience + OpenTelemetry through the base (AC-5); registers the liveness `self` check **exactly once** with `live` tag and not `ready` (AC-2/AC-3 no double registration).
- [x] `tests/Hexalith.Conversations.ServiceDefaults.Tests/Hexalith.Conversations.ServiceDefaults.Tests.csproj` — new project, registered in `Hexalith.Conversations.slnx`.
- [x] `Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests/HexalithServiceDefaultsTest.cs` — **fixed** the hook-order test (Gap 1, submodule).

### Implementation
- No production source changed by this QA run — the promote/adopt was the dev-story step. One new test project added to the umbrella (`Hexalith.Conversations.ServiceDefaults.Tests`, with slnx entry) and one existing test fixed in the `Hexalith.Commons` submodule. No assertion relaxed: the hook-order fix **strengthens** coverage (it now proves the lazy logging hook actually fires).

### Validation
- [x] `dotnet test Hexalith.Commons/test/Hexalith.Commons.ServiceDefaults.Tests -c Release` — **14 passed** (was 13 passed / 1 failed before the fix), 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.ServiceDefaults.Tests -c Release` — **7 passed** (new), 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests -c Release` — **589 passed**, 0 failed, 0 skipped (existing AC-2/AC-4/AC-5 host-composition guardrails unchanged and green).
- [x] Combined ServiceDefaults surface: **610 passed, 0 failed**. New project + fixed test build clean under warnings-as-errors (Release).

### Coverage
- Promoted `Hexalith.Commons.ServiceDefaults` (existing suite, now fully green): null-builder guard, default/ready/skip self-check, configurable endpoint paths, status-code mapping (Healthy/Degraded→200, Unhealthy→503), dev JSON writer with non-serializable tolerance, health-probe trace exclusion, service discovery + HTTP resilience + OpenTelemetry registration, OTLP env-gated exporter, and eager-hook order **plus** proven lazy logging-hook execution.
- Conversations `ConversationsServiceDefaults` wrapper (the gap): service-name + meter continuity, endpoint-contract preservation, both fail-closed null guards, observability side effects, and single-self-check no-double-registration.
- Existing host composition (`ConversationsDomainServiceHostCompositionTest`): `/health`·`/alive`·`/ready` mapped alongside domain routes, status-code mapping, dev detailed-JSON, trace exclusion, single `self` + discovery/resilience/OpenTelemetry through the live `AddEventStoreDomainService` runtime path.
- Out of scope for this QA run (story dev/release gates, executed-or-deferred, not edited here): full `Hexalith.Conversations.slnx` Release build, conformance suite `>= 361`, public-contract-shape diff, sibling builds, and the `Hexalith.Commons` submodule commit + root gitlink bump that must carry the hook-order test fix (dev Task 7). No UI/E2E (Playwright) applies — Story 3.4 has no UI surface.

## Story 3.3 Promote & Adopt the Diagnostics/Telemetry Scaffolding Helper

Story 3.3 (FR-15, Promote) lifts the repeated `IMeterFactory`/`Meter.CreateCounter`/bounded-tag/`None`-sentinel/content-safe-logging scaffolding into a new shared `Hexalith.Commons.Diagnostics` library (`BoundedTelemetryCounterDefinition`, `BoundedTelemetryMeter`, `BoundedTelemetryCounter`, `BoundedMetricDimension`, `BoundedTelemetryLog`) and adopts it through three thin Conversations wrappers (`ConversationProjectionTelemetry`, `ConversationRejectionTelemetry`, `ConversationConformanceTelemetry`) plus a `ConversationTelemetryDefinitions` metric-contract manifest. There is **no UI and no new HTTP endpoint** — it is observability emitter scaffolding — so the automated-test surface is the **public API of the promoted helper** (module-owned tests, AC-1/Task 2) and the **metric/log emission contract of the thin wrappers** (AC-2/AC-3/Task 4), driven directly through `System.Diagnostics.Metrics.MeterListener`, not browser E2E. Framework: xUnit v3 `3.2.2` + Shouldly `4.3.0` (CPM; no new package — the helper compiles against `Microsoft.Extensions.Diagnostics.Abstractions` + `Microsoft.Extensions.Logging.Abstractions` only). The shipped suites were strong; this run auto-applied the discovered helper-API and wrapper-guard gaps.

### Discovered Gaps → Applied (2 new files, 17 cases)
- [x] Gap 1 (AC-1/Task 2 — promoted helper API under-covered) — the shipped `BoundedTelemetryCounterTest` (6 cases) covered ctor guard, counter creation, enum lowercase + `None`-sentinel reject, 2-dim key-order reject, `SafeToken` empty/control-char reject, and the log-hook guard, but the **`BoundedTelemetryCounterDefinition` constructor guards** (missing name/description, null key array, blank key, **duplicate-key uniqueness**, `DimensionKeys` declared order), the **1-dim / 3-dim / `params` `AddOne` overloads** (count mismatch, null array, 3-dim key-order validation), `BooleanToken` `true`, the empty-key guard across all three token factories, and **meter/factory reuse** (one underlying meter across N counters — explicitly named in Task 2) were unexercised. Closed by `BoundedTelemetryHelperTest` (10 cases).
- [x] Gap 2 (AC-2/AC-3/Task 4 — wrapper guards + meter-name continuity under-covered) — the shipped wrapper suites covered each signal's happy-path emission, bounded tag values, `None`-sentinel reject, redaction-safe log content, and DI registration, but the **constructor null guards** (meter factory / logger) for all three wrappers, the **`correlationId` empty-guard** on freshness/rebuild/publication/tenant-denial/privileged-access (only command-rejection + conformance were guarded), the **`gate_id` control-character rejection** at the Conversations boundary (only empty was tested), and — most importantly for AC-3 observability continuity — **assertion at emission time that signals land on the stable `Hexalith.Conversations` meter** (the contract test only pins the name *constant*, not the emitted meter) were unpinned. Closed by `ConversationTelemetryGuardsTest` (7 cases).

### Generated Tests
- [x] `Hexalith.Commons/test/Hexalith.Commons.Diagnostics.Tests/BoundedTelemetryHelperTest.cs` — new file, **10 cases**: definition rejects missing name/description/null-keys; rejects blank + duplicate keys; exposes `DimensionKeys` in declared order; `AddOne` single-dim emits one tag; `AddOne` three-dim emits all tags in order; three-dim rejects unexpected key order; `params` emits declared count and rejects count mismatch; `params` rejects null array; boolean `true`/`false` + blank-key guards on `EnumToken`/`BooleanToken`/`SafeToken`; one meter reused across counters (`CreatedMeters == 1`).
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationTelemetryGuardsTest.cs` — new file, **7 cases**: ctor `ArgumentNullException` guards for all three wrappers; `correlationId` empty-guard for projection freshness / rebuild / publication failure; `correlationId` empty-guard for tenant denial / privileged access; conformance `gate_id` control-character rejection; meter-name continuity for projection freshness, command rejection, and conformance outcome (each emits on `Hexalith.Conversations`).

### Implementation
- No production source changed by this QA run — the promote/adopt was the dev-story step. Only test files added: **1 new file in the `Hexalith.Commons` submodule** (`Hexalith.Commons.Diagnostics.Tests`) and **1 new file in `Hexalith.Conversations.Server.Tests`**. No existing test removed or weakened; no assertion relaxed. The new Commons test file should be included in the Story 3.3 Commons submodule commit + root gitlink bump (Task 7).

### Validation
- [x] `dotnet test Hexalith.Commons/test/Hexalith.Commons.Diagnostics.Tests -c Release` — **16 passed** (6 baseline + 10 new), 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests -c Release` (Diagnostics filter) — **119 passed** (112 baseline + 7 new); full project **582 passed**, 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests -c Release` — **361 passed** (release gate `>= 360` holds), 0 failed, 0 skipped.
- [x] All projects build clean under warnings-as-errors (Release); the IDE/Sonar analyzer-as-error set (e.g. IDE0004, S3878) passes after fixing the new helper file's redundant-cast / inline-`params`-array findings.

### Coverage
- Promoted `Hexalith.Commons.Diagnostics`: definition guards (incl. duplicate-key uniqueness + declared order), every `AddOne` overload (1/2/3/`params`) with count + key-order validation and null-array guard, all three dimension-token factories (lowercase-invariant enum + `None` reject, boolean `true`/`false`, safe-token empty/control-char/blank-key), the content-safe log hook, and meter/factory reuse — the full Task-2 minimum helper set is now exercised.
- Conversations wrappers: per-signal bounded emission + redaction-safe logs + `None`-sentinel reject + DI registration (pre-existing) **+** ctor null guards, full `correlationId` guard coverage, gate-id control-char rejection, and emission-time `Hexalith.Conversations` meter-name continuity (the gaps).
- Scope boundary owned by the story's open dev gates (not this QA run): classifier policy tests, the redaction/cardinality/conformance-status conformance suites, the FR-20 project-reference disposition, and the Commons submodule commit + root gitlink bump (Tasks 3–7) — these were **executed** to confirm green, not edited.

## Story 3.2 Promote & Adopt the Generic Tenant-Access Projection Handler + Registration

Story 3.2 promotes the duplicated tenant-access projection/update mechanics and the fail-closed decision engine into a shared `Hexalith.Commons.TenantAccess` capability and adopts it through thin Conversations facades. It is backend plumbing (DI registration + a thin fail-closed service); there is **no UI and no new HTTP endpoint**, so the automated-test surface is the **public API of the newly promoted shared capability** — `TenantAccessEvaluator.EvaluateAsync(...)` (fail-closed decision engine) and `TenantAccessProjectionHandler<TEvent,TProjection>` (replay/retry-tolerant projection handler) — not browser E2E. The Conversations-side facade is already covered by the pre-existing 627-line `ConversationTenantAccessServiceTest`, the new `ConversationTenantAccessSharedParityTest`, and `ConversationTenantAccessRegistrationTest`, so this run did not duplicate it; gaps were found and filled in the **promoted module's own test suite** (its rightful home per AC-1 "module-owned tests"). Frameworks: the Commons helper tests target **xUnit v2 `2.9.3` / VSTest + Shouldly** (note: the root Conversations repo uses xUnit v3 `3.2.2` / Microsoft.Testing.Platform; generated tests use only APIs common to both).

> Environment note: contrary to the Dev Agent Record's recorded `SocketException (13)` under VSTest discovery, the suite **does run** in this environment when invoked against the already-built assembly (`dotnet test … --no-build`), which avoids the discovery-time socket bind. This let the tests actually execute for the first time — surfacing the latent defect below.

### Discovered Gaps → Applied (2 new files, 39 cases) + 1 latent defect fixed
- [x] Gap 1 (AC-5 — replay/revocation/retry tolerance of the promoted handler under-covered) — the shipped handler suite covered add/dedup/divergent-duplicate/out-of-order/config-filter/malformed/retry-success, but **event-driven revocation** (`UserRemovedFromTenant`), `UserRoleChanged`, the enable→disable→enable toggle, missing-tenant-id no-op, non-positive sequence malformed evidence, and the **retry-exhaustion rethrow / non-retryable propagation** branches were unexercised. A regression dropping revocation or silently swallowing a persistence failure would stay green. Closed by `TenantAccessProjectionHandlerReplayToleranceTest` (9 cases).
- [x] Gap 2 (AC-3/AC-4 — evaluator fail-closed contract surface under-covered) — the shipped evaluator suite covered the denial/role theories, but the **undefined-requirement throw**, the 10 `ArgumentNullException` collaborator guards, the **pre-cancelled token** short-circuit, the allowed happy-path identity, injection-safe **tenant-character canonicalization** (18 forbidden inputs), unsafe **caller-principal** rejection (5), null-member-map malformed projection, and multi-input tenant-binding resolution were unpinned. Closed by `TenantAccessEvaluatorContractTest` (30 cases).
- [x] Latent defect fixed — `TenantAccessEvaluatorTest.EvaluateAsyncShouldDenyUnsafeProjectionHealth(health: null, …)` **failed on first real execution**: its `StaticHealthProvider` stub coalesced a `null` health record to a *healthy* one (`health ?? new(…)`), so the AC-3 "unavailable tenant state denies" case was silently never covered and returned an *allowed* decision. Production code denies correctly; the **stub** was wrong. Fixed by splitting it into a parameterless (healthy default) constructor and an explicit-value constructor so `null` flows through verbatim. This survived because the dev sandbox could never run VSTest.

### Generated Tests
- [x] `Hexalith.Commons/test/Hexalith.Commons.TenantAccess.Tests/TenantAccessProjectionHandlerReplayToleranceTest.cs` — new file, **9 cases**: `UserRemovedFromTenantShouldRevokeProjectedPrincipal`, `UserRoleChangedShouldUpdateProjectedRoleEvidence`, `TenantDisabledThenEnabledShouldToggleEnabledFlag`, `EventWithMissingTenantIdShouldBeNoOpWithoutTouchingStore` (×2), `NonPositiveSequenceNumberShouldMarkMalformedEvidence` (×2), `ExhaustingRetryablePersistenceFailuresShouldRethrowWithoutPartialCommit`, `NonRetryablePersistenceFailureShouldPropagateImmediately`.
- [x] `Hexalith.Commons/test/Hexalith.Commons.TenantAccess.Tests/TenantAccessEvaluatorContractTest.cs` — new file, **30 cases**: undefined-requirement throw, 10-arg null guards, pre-cancelled token, valid-owner happy-path identity, `EvaluateAsyncShouldRejectForbiddenTenantCharacters` (×18), `EvaluateAsyncShouldRejectUnsafeCallerPrincipals` (×5), null-member malformed projection, matching multi-input resolution, and contradictory later-position binding → `TenantMismatch`.

### Implementation
- No production source under `src/` (or under the Commons library) changed by this QA run — the promote/adopt was the dev-story step. Only test files changed: **2 new files added** and **1 existing stub fixed** (`TenantAccessEvaluatorTest.cs`), all inside the `Hexalith.Commons` submodule. No test removed or weakened.

### Validation
- [x] `dotnet build Hexalith.Commons/test/Hexalith.Commons.TenantAccess.Tests/…csproj -c Debug` — **0 Error(s)** (5 CA2007 ConfigureAwait warnings, non-blocking; IDE/VSTHRD/Sonar analyzers are errors in this project and all pass).
- [x] `dotnet test Hexalith.Commons/test/Hexalith.Commons.TenantAccess.Tests/…csproj -c Debug --no-build` — **Passed! Failed: 0, Passed: 70, Skipped: 0** (was 69 passed / 1 failed before the latent-defect fix).

### Coverage
- Promoted `TenantAccessEvaluator`: all 16 `TenantAccessDenialKind` outcomes now reachable, plus argument guards, cancellation, identity canonicalization, and the tenant/caller validation boundary (AC-3 fail-closed; AC-4 no deny→allow / safe retryability on the shared engine).
- Promoted `TenantAccessProjectionHandler`: create/enable/disable, add/remove/role-change, dedup, divergent-duplicate replay conflict, out-of-order drop, config filter/tombstone, malformed evidence, bounded-retry success **and** exhaustion/non-retryable propagation (AC-5).
- New cases: **39** (30 + 9). Latent failures fixed: **1**. Suite total: **70 passed / 0 failed**.
- Scope boundary owned by the story's open release gates (not this QA run): Server.Tests + conformance suite (`>= 360`) + warnings-as-errors Release build / contract-shape diff (Task 6), and the Commons submodule commit + root gitlink bump with the remote push that was blocked in the dev sandbox (Task 7). The 2 new test files + the 1 stub fix should be included in that Commons commit.

## Story 3.1 Promote & Adopt the Generic Typed-HttpClient Registration

Story 3.1 is a .NET client-registration/API-surface story, not a browser UI story. The automated QA surface is the promoted Commons helper and the Conversations typed-client adoption facade. Frameworks detected: xUnit v3 + Shouldly for Conversations, xUnit v2 + Shouldly for the Commons helper tests.

### Discovered Gap → Applied
- [x] Gap (AC-1/AC-2: adoption facade did not prove returned builder chaining or endpoint use) — existing Conversations client tests covered valid registration and eager rejection of missing, relative, and non-http(s) endpoints, but only resolved the typed client. They did not prove `AddHexalithConversationsClient(...)` still returns a usable `IHttpClientBuilder` after delegating to the shared helper, nor that the configured endpoint is used by the resolved client. Closed by `ServiceCollectionExtensionShouldReturnBuilderForHandlerChainingAndUseConfiguredEndpoint`.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` — added 1 xUnit fact that chains a probe `DelegatingHandler` through the returned builder, configures a fake primary handler, resolves `IConversationClient`, executes `CreateConversationAsync`, and asserts:
  - the chained handler observed `/api/v1/conversations`;
  - the primary handler received `https://conversations.example.test/api/v1/conversations`;
  - the probe header reached the primary handler.

### Existing Story 3.1 Coverage Verified
- [x] `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` — valid endpoint registration, missing endpoint rejection, relative endpoint rejection, and non-http scheme rejection.
- [x] `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/HttpClientRegistrationTest.cs` — promoted helper tests for eager missing/relative/non-web rejection, valid endpoint acceptance, builder return for handler chaining, lazy missing rejection, permissive non-web absolute URI behavior, and configuration-section binding.
- API endpoints: N/A — story scope is typed `HttpClient` DI registration, not an HTTP server endpoint.
- UI E2E tests: N/A — no UI surface exists for this story.

### Validation
- [x] `dotnet build tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --no-restore -m:1 /nr:false -p:UseSharedCompilation=false` — 0 warnings, 0 errors.
- [x] `tests/Hexalith.Conversations.Client.Tests/bin/Debug/net10.0/Hexalith.Conversations.Client.Tests` — 29 passed, 0 failed, 0 skipped.
- [x] `dotnet build Hexalith.Commons/test/Hexalith.Commons.Http.Tests/Hexalith.Commons.Http.Tests.csproj --no-restore -m:1 /nr:false -p:UseSharedCompilation=false` — 0 warnings, 0 errors.
- [~] `dotnet test Hexalith.Commons/test/Hexalith.Commons.Http.Tests/Hexalith.Commons.Http.Tests.csproj --no-restore --no-build` — blocked by sandbox socket denial in VSTest (`SocketException (13): Permission denied` while opening the test communication listener). No local xUnit v2 console runner was available; the project builds cleanly.

### Checklist Result
- [x] API/registration tests generated where applicable.
- [x] No UI E2E tests generated because story 3.1 has no UI.
- [x] Tests use standard project APIs: xUnit + Shouldly + `IHttpClientBuilder`.
- [x] Happy path covered: valid endpoint, typed client resolution, configured endpoint used.
- [x] Critical error cases covered: missing endpoint, relative URI, non-http(s) scheme.
- [x] Tests have clear descriptions and no hardcoded waits.
- [x] Tests are independent.
- [x] Summary includes coverage metrics and validation status.

## Story 2.7 Consume Shared EventStore.Testing Assertions and Fakes

Story 2.7 (FR-9, Consume — Epic 2's seventh and final consume story) is a **test-only verify-record-correct** story: **no `src/` change**. Its substantive deliverable (AC-1) is a *verified-gap finding* — the test tree holds **zero in-module re-implementations** of the three shared `EventStore.Testing` types FR-9 named for consume (`InMemoryStateManager`/`IActorStateManager`, `FakeEventStoreGatewayClient`/`IEventStoreGatewayClient`, and `DomainResultAssertions`). The genuine fake-consume (`InMemoryReadModelStore` in `Server.Tests`) already landed in Story 2.4 and is preserved; net **zero** assertion swaps (the direct aggregate assertions are strictly stronger). There is **no UI and no HTTP API in scope**, so the QA surface is a **regression guard** over the source tree, not an HTTP/E2E lane. Framework: xUnit v3 `3.2.2` + Shouldly `4.3.0` (CPM; no new package, no new ProjectReference).

### Discovered Gap → Applied (1 new test file, 3 facts)
- [x] **Gap (AC-1 unguarded invariant)** — the "zero in-module duplicates" finding was established only by one-off greps recorded in the Dev Agent Record; nothing in the suite *enforced* it, so a later change could re-introduce an in-module duplicate (e.g. a bespoke `InMemoryStateManager` or a `DomainResultAssertions` extension that silently weakens the oracle) and re-open the FR-9 gap undetected. Closed by `NoInModuleSharedFakeDuplicateConformanceTest` — declaration-anchored source scans over the module's own `src/`+`tests/` trees that codify the three greps as durable assertions.

Detectors are anchored to real C# declaration syntax (a class base-list implementing `IActorStateManager`/`IEventStoreGatewayClient`, a `*StateManager`/`*GatewayClient` class name, or a `(this DomainResult …)` extension receiver), so they match a genuine duplicate **declaration**, never a prose mention of the type name in the at-risk register's recorded findings. The guard exempts only its own meta-file (which legitimately names the patterns). Teeth proven against synthetic re-introduced duplicates; green on the clean tree.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Conformance.Tests/NoInModuleSharedFakeDuplicateConformanceTest.cs` — new file, 3 facts: `NoInModuleActorStateManagerDuplicateShouldExist`, `NoInModuleEventStoreGatewayClientDuplicateShouldExist`, `NoInModuleDomainResultAssertionsDuplicateShouldExist`.

### API / E2E
- N/A — test-only story, no API endpoint and no UI surface.

### Coverage
- Build: `Hexalith.Conversations.Conformance.Tests` Release — **0 Warning(s), 0 Error(s)** (warnings-as-errors).
- Conformance suite: **360 passed**, 0 failed, 0 skipped (357 story-2.7 baseline **+3** new guard facts → monotonic; gate floor 356 holds).
- New class is `*ConformanceTest` (not `*ConformanceSuiteTest`), so the "exactly 14 suite classes" FR-20 guard is unaffected. No `src/` change; public-contract-shape diff stays empty. The AC-5 inventory `changeLog` entry and `story27StructuralDispositions` ledger rows were already covered by the existing inventory/procedure/ledger validators (verified, not duplicated).

## Story 2.6 Adopt Shared Serialization Helpers for Generic Converters

Story 2.6 (FR-8, Consume) is a **verify-record-defer** story — the dev step made **no `src/` change**: FR-8's named shared target (Commons generic value/identifier converters + a source-gen JSON-context base) does not exist in the Epic-2 consumable surface (verified at Commons `30620b9`), so the build + `NameTypeMapper` publicize is deferred to FR-14 / Story 3.6, and the five `generic-serialization-converters` files stay in place behavior-identical. There is **no UI or HTTP API in scope** (the feature is a Contracts-assembly serialization seam), so the QA surface is **contract-serialization behavior of the existing converters**, driven directly through `System.Text.Json` web defaults. Framework: xUnit v3 `3.2.2` + Shouldly `4.3.0` (CPM; no new package). The shipped converter suite (`ContractSerializationTest` positive wire-shape oracle + `IdentifierValidationTest` identifier/schema-version negative paths) was strong; this run auto-applied the discovered token-type-guard gaps on the two genuinely-ruleless base skeletons that AC-3 names and AC-4 wants pinned as the FR-14/3.6 characterization oracle.

### Discovered Gaps → Applied (3 new [Theory]/[Fact] tests, 16 cases)
- [x] Gap 1 (AC-3/AC-4 — `ConversationStringValueJsonConverter<T>` non-string token guard untested) — the string-value base skeleton's only negative test fed a *string* that fails the domain parse (`"current"` → `JsonException`); the prior `JsonTokenType.String` guard itself (reject a JSON number / object / array / boolean before any parse) was unpinned. A regression that dropped the token guard would still pass every existing test. Closed by `StringValueSkeletonShouldRejectNonStringTokens` (7 cases over `ProjectionTrustState` + `ProjectionFreshnessReasonCode`: `123`, `1.5`, `true`, `false`, `{}`, `[]`, `["Current"]` → `JsonException`).
- [x] Gap 2 (AC-3/AC-4 — `ConversationIntValueJsonConverter<T>` non-number/overflow guard untested) — `SchemaVersion` covered fractional/exponent/string-wrapped and out-of-domain-range (`0`,`-1`), but the skeleton's `TokenType == Number && TryGetInt32` guard for non-number tokens and **Int32 overflow/underflow** was unpinned (a too-large number would otherwise risk a truncated/overflowing value rather than a clean `JsonException`). Closed by `IntValueSkeletonShouldRejectNonInt32Tokens` (8 cases over `SchemaVersion`: `true`, `{}`, `[]`, `[1]`, `"1"`, `2147483648`, `9999999999`, `-2147483649` → `JsonException`).
- [x] Gap 3 (symmetry guard) — proves the new negative-path assertions characterize *rejection* without disturbing *acceptance*. Closed by `SkeletonsShouldStillRoundTripCanonicalValues` (canonical `ProjectionTrustState.Current` / `ProjectionFreshnessReasonCode.Current` / `SchemaVersion.Current` still round-trip to themselves).

The skeletons are exercised through the public value types whose converters derive from them, so **no source under `src/Serialization` is modified** (the Keep domain-rule converters are not touched) and the positive wire-shape oracle is left intact.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/GenericValueConverterSkeletonTest.cs` — new file, 3 tests / 16 cases (Gaps 1–3 above), reusing the project's existing `System.Text.Json` + Shouldly conventions. Documents the file as the FR-14/3.6 behavior-exact characterization the future shared-helper replacement must preserve.

### Implementation
- No production source under `src/` changed by this QA run (verify-record-defer story; behavior preserved exactly). Only the one new test-only file was added; no test removed or weakened (no FR-20 ledger entry required for additions). The `ContractSerializationTest` wire-shape oracle is un-weakened (not modified).

### Validation
- [x] Submodule gitlinks verified at recorded commits before building (Commons `30620b9`, EventStore `ad2c957`, FrontComposer `451830b`, Parties `485616f`, Tenants `5b4424e`, …); non-recursive (CLAUDE.md compliant).
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/ -c Release --filter "FullyQualifiedName~GenericValueConverterSkeletonTest"` — **16 passed**, 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/ -c Release` — **603 passed** (587 baseline + 16 new), 0 failed, 0 skipped; `ContractSerializationTest` green & un-weakened.
- [x] Release build: 0 warnings / 0 errors (warnings-as-errors). Additive `Contracts.Tests`-only change → conformance gate (monotonic, 356 after the dev step) untouched; public contract-shape diff empty (no `src/` change; no contract type touched).

### Coverage
- Generic ruleless skeletons (`ConversationStringValueJsonConverter<T>`, `ConversationIntValueJsonConverter<T>`): token-type guard (was the gap) **+** domain-parse rejection **+** happy-path round-trip — now fully characterized as the FR-14/3.6 oracle.
- Prefixed-identifier converters (7 families): per-type prefix, cross-type-substitution rejection, malformed-payload rejection — already covered by `IdentifierValidationTest`; unchanged.
- Closed-vocabulary / freshness / trust-state converters (Keep, 432 LOC): covered by existing contract tests; not touched (out of scope per AC-3).
- Scope boundary: building the shared generic-converter / source-gen JSON-context base + publicizing `NameTypeMapper` is FR-14 / Story 3.6 (Promote), out of scope here; this file + `ContractSerializationTest` are the characterization that future replacement must keep green.

## Story 2.5 Implement Projections Against the SDK Projection Seam

Story 2.5 (FR-6, Consume) serves the conversation full-replay projection through the platform `IDomainProjectionHandler` `/project` seam via a new `ConversationProjectionHandler` (`src/Hexalith.Conversations.Server/Projections/`), delegating the generic replay/dispatch/discovery orchestration to the SDK while the conversation-specific field selection, freshness formula, and evidence construction stay in the kept `ConversationProjectionMaterializer`. There is **no UI** in scope and the seam is a server-internal synchronous contract (`Project(ProjectionRequest) → ProjectionResponse`), so the automated-test surface is **behavior tests driving the feature end-to-end through its real entry point** (decode → kept materialization → serialized projection state) — asserting observable field/freshness/evidence values, not mocks or call-counts (Epic 1 L1/A1). Framework: xUnit v3 + Shouldly + NSubstitute (CPM; no new package). The shipped seam suite was strong; this run auto-applied the discovered degraded-state and replay-safety gaps.

### Discovered Gaps → Applied (5 new [Fact] tests)
- [x] Gap 1 (NFR5 / AC-3 — idempotency under at-least-once delivery untested **through the seam**) — the shipped suite proved gap/poison degraded states but never proved that a **re-delivered** event leaves the read model unchanged. A regression that dropped the per-event `_processedEventIds` dedup would still pass every existing test. Closed by `DuplicateEventDeliveryShouldProjectIdenticalReadModelThroughTheSeam` (deliver the same events once vs. with duplicates → byte-identical `ProjectionResponse.State`, stays `Current`).
- [x] Gap 2 (NFR5 / AC-3 — `OutOfOrderEvent` reason code untested) — the existing gap test covered a *forward* gap (`GapDetected`), but the distinct **position-regression** branch (`OutOfOrderEvent → Rebuilding`) was unexercised. Closed by `OutOfOrderEventShouldSurfaceDegradedFreshnessThroughTheSeam` (positions 1,2,2 → `Rebuilding` / `OutOfOrderEvent`, not trust-bearing).
- [x] Gap 3 (AC-3 — `StaleThresholdExceeded` degraded state untested) — the freshness formula's stale branch (`lag > staleAfter`) was reachable through the seam via the injected clock but unasserted. Closed by `StaleProjectionShouldSurfaceStaleThresholdThroughTheSeam` (clock 10 min past the last event → `Stale` / `StaleThresholdExceeded`).
- [x] Gap 4 (AC-3 — empty / no-`ConversationCreated` stream untested) — an empty event sequence must project a **non-current** `Rebuilding` model, never empty-but-current. Closed by `EmptyEventSequenceShouldSurfaceRebuildingThroughTheSeam` (no events → `Rebuilding` / `Rebuilding`, `MessageCount == 0`, not trust-bearing).
- [x] Gap 5 (robustness — null-request boundary untested) — the seam's `ArgumentNullException.ThrowIfNull(request)` fail-closed guard had no test. Closed by `NullRequestShouldThrowArgumentNullException`.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionHandlerTest.cs` — +5 [Fact] tests (Gaps 1–5) + a `Handler(DateTimeOffset clock)` helper overload, reusing the file's existing `Request`/`Dto`/event-factory/`FixedTimeProvider` fixtures.

### Implementation
- No production source under `src/` changed by this QA run (the handler was the dev-story step). Only the one existing test file was extended; no test removed or weakened (no FR-20 ledger entry required for additions).

### Validation
- [x] Release build: 0 warnings / 0 errors (warnings-as-errors); submodule gitlinks verified at recorded commits before build (EventStore `ad2c957`, FrontComposer `451830b`, Parties `485616f`, Tenants `5b4424e`, Commons `30620b9`); non-recursive (CLAUDE.md compliant).
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/ -c Release` — **561 passed** (556 baseline + 5 new), 0 failed, 0 skipped. Handler class in isolation: **12/12** green.
- [x] Additive `Server.Tests`-only change → conformance gate unaffected (monotonic ≥ 354 holds); public contract-shape diff empty (no `src/` change).

### Coverage
- Degraded-state reason codes now exercised **through the seam**: `Current`, `GapDetected`, `OutOfOrderEvent` (was Gap 2), `StaleThresholdExceeded` (was Gap 3), `Rebuilding` (was Gap 4), `PoisonEvent`/`Unavailable`, `Redacted` (suppression). Idempotency under duplicate delivery (was Gap 1) and the null-request boundary (was Gap 5) now pinned.
- Not reachable through the steady-state seam **by design** (handler hardcodes `isRebuilding:false`, `metadataWriteFailed:false` — stateless full-replay): `MetadataWriteFailed`, `isRebuilding`-driven `Rebuilding`, `MetadataContradictory`, `UnsupportedVersion`. These stay exercised via `ConversationProjectionRebuildVerifier` and the read-service degraded-state path per the story's freshness-input sourcing decision — not a seam gap.
- Scope boundary: the read-store-population thread (handler→`ConversationProjectionReadModelWriter`) is a flagged open thread in the Dev Agent Record (no sync-over-async inside the seam); a replay→persist→read integration loop belongs to that follow-on, not this QA run.

## Story 2.4 Persist Read Models via the Shared Store + Write Policy

Story 2.4 (FR-5, greenfield-adopt) adds the read-model persistence **substrate**: registers the SDK `IReadModelStore` (`AddEventStoreReadModelStore()` + `AddDaprClient()`), implements the production `ConversationProjectionReadStore` (read) over `IReadModelStore`, and a `ConversationProjectionReadModelWriter` (write) through the SDK `ReadModelWritePolicy` (optimistic-concurrency, reload-and-merge), closing the `IConversationProjectionReadStore` binding deferred from Story 2.3. There is **no UI** in scope, so the automated-test surface is the production read-store/writer over the canonical SDK `InMemoryReadModelStore` double plus the DI host-composition test — not browser E2E. Framework: xUnit v3 + Shouldly + NSubstitute (CPM; only the additive `Hexalith.EventStore.Testing` project reference). The shipped persistence/concurrency/fail-closed suite was thorough; this run auto-applied the discovered behavioral coverage gaps.

### Discovered Gaps → Applied (3 new [Fact] tests)
- [x] Gap 1 (AC-2 / NFR5 — `MergeIndex` newest-generation-wins **supersede** untested) — the shipped idempotency test only covered the *equal*-position no-op; the `>` side of the `>=` merge branch (a re-materialization at a **higher** applied event position replacing the stale index entry in place) was unexercised. A regression that dropped the supersede branch would still pass every existing test. Closed by `RepersistingAtHigherGenerationSupersedesIndexEntry` (persist ConvA@pos1 then ConvA@pos5 → single entry, `LastAppliedEventPosition == 5`).
- [x] Gap 2 (AC-2 / NFR5 — `MergeIndex` stale-write guard untested) — the *false* side of the merge branch (a late/out-of-order **lower**-position re-apply must NOT regress a newer persisted entry) had no test. Closed by `RepersistingAtLowerGenerationDoesNotOverwriteIndexEntry` (persist ConvA@pos5 then ConvA@pos1 → entry stays at `LastAppliedEventPosition == 5`).
- [x] Gap 3 (AC-4 / NFR2 — `ListAsync` empty/absent path untested) — the `?? []` fail-soft branch (a fresh tenant with no persisted index) was only exercised on the populated path. Closed by `ListAsyncReturnsEmptyWhenNoIndexExists` (empty list from a **single** index read — no fan-out on the absent path).

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs` — +3 [Fact] tests (Gaps 1–3), reusing the file's existing `Models(conversationId, position)` and `CountingReadModelStore` fixtures.

### Implementation
- No production source under `src/` changed by this QA run (the persistence substrate was the dev-story step). Only the one existing test file was extended; no test removed or weakened (no FR-20 ledger entry required for additions).

### Validation
- [x] Release build: `dotnet build Hexalith.Conversations.slnx -c Release` — 0 warnings / 0 errors (warnings-as-errors).
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/ -c Release` — **548 passed** (545 baseline + 3 new), 0 failed, 0 skipped.
- [x] The 3 new tests run green in isolation (Failed 0, Passed 3).
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/ -c Release` — **354 passed** (monotonic gate ≥ 353 holds, unchanged — additive test-only change in `Server.Tests`).
- [x] Submodule gitlinks verified at recorded commits before building (EventStore `ad2c957`, FrontComposer `451830b`, Parties `485616f`, Tenants `5b4424e`); non-recursive (CLAUDE.md compliant).

### Coverage
- AC-2 / NFR5 write path (`ConversationProjectionReadModelWriter` / `MergeIndex`): per-conversation `UpdateAsync` + tenant-index `MergeAsync`, all three merge branches — **new entry** (pre-existing), **newer supersedes** (was Gap 1), **older ignored** (was Gap 2) — plus equal-position idempotency, no-lost-update reload-and-reapply, and fail-loud retry exhaustion (pre-existing).
- AC-4 / NFR2 read path (`ConversationProjectionReadStore`): keyed `ReadAsync` (present) + `ListAsync` populated single-read no-N+1 (pre-existing) **+ absent/empty single-read** (was Gap 3).
- AC-4 read boundary over the real store: Forbidden / Unavailable / PoisonEvent / Rebuilding / Current (pre-existing, unchanged).
- AC-1 host composition: production `IReadModelStore` → `DaprReadModelStore`, `IConversationProjectionReadStore` → `ConversationProjectionReadStore`, full query/governance consumer graph builds (pre-existing).
- Scope boundary: materializer→writer replay wiring and a full replay→persist→read integration loop are Story 2.5 / FR-6 concerns, not in scope here; the generation-precedence behavior proven here is the contract that wiring must honor.

## Story 2.3 Adopt the SDK Query-Handler + Cursor Codec, Remove Hand-Rolled HMAC Cursor

Story 2.3 (FR-4, remove-and-replace) swaps the hand-rolled HMAC continuation-cursor codec for the SDK `IQueryCursorCodec` + `QueryCursorScope`, and exposes conversation list/detail queries through the SDK `IDomainQueryHandler` `/query` seam as thin adapters. There is **no UI or HTTP API in scope** (the live entrypoint is the SDK `/query` dispatch seam), so the automated-test surface is the dispatch seam and the `ConversationQueryHandler` cursor touch-points, driven directly. Framework: xUnit v3 + Shouldly + NSubstitute (CPM; no new package). The shipped cursor fail-closed suite and dispatch teeth tests were thorough; this run auto-applied the discovered coverage gaps.

### Discovered Gaps → Applied (5 new [Fact] tests)
- [x] Gap 1 (AC-2 — filter scope binding untested) — the cursor binds four scope dimensions (tenant/caller/filter/generation); tenant, caller, and generation each had a fail-closed test but the **filter fingerprint** binding did not. A cursor minted under a different filter could regress to silently decoding if the filter dropped out of the scope and every other test would stay green. Closed by `FilterMismatchedCursorShouldFailClosed` (mint under a project filter, present under the empty filter → wrong-scope, zero reads).
- [x] Gap 2 (AC-3 — exception containment untested) — AC-3 requires the adapter "never an exception leak" past the seam, but no test forced a fault. Closed by `QueryFaultShouldBeContainedAsCoarseFailureNotExceptionLeak` (undeserializable payload → coarse `QueryResult.Failure`, not the dispatcher's "No query handler" miss, no raw exception text).
- [x] Gap 3 (AC-3 — envelope aggregate-id resolution untested) — the detail adapter resolves the conversation id from `EntityId ?? AggregateId` when the payload omits it (aggregate-routed gateway path); the shipped detail test always supplied the id in the body. Closed by `DetailQueryShouldResolveConversationIdFromAggregateIdWhenPayloadOmitsIt` (empty payload + aggregate id → reaches handler, projection read attempted).
- [x] Gap 4 (AC-3 — unresolvable-id fail-closed untested) — defense in depth when no id is resolvable. Closed by `DetailQueryWithNoResolvableConversationIdShouldFailClosed` (coarse failure, zero reads).
- [x] Gap 5 (AC-3 / project-context fail-closed — missing-user gate untested) — the adapter rejects a blank authenticated user before any state access; the `QueryEnvelope` contract enforces non-empty user id at construction, so the adapter's own defense-in-depth gate was unexercised. Closed by `MissingAuthenticatedUserShouldFailClosedBeforeProjectionRead` (zero reads). Gaps 4–5 drive degenerate envelope states via a record object-initializer the constructor otherwise forbids.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationDomainQueryDispatchTest.cs` — +4 dispatch-seam tests (Gaps 2–5); `EmptyProjectionReadStore` extended with `ListReads`/`DetailReads` counters to pin zero-read fail-closed behavior (the `store.ListReads.ShouldBe(0)` idiom from the handler suite).
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` — +1 cursor test (Gap 1), mirroring `TenantMismatchedCursorShouldFailClosed`.

### Implementation
- No production source under `src/` changed by this QA run (the cursor/handler swap was the dev-story step). Only the two existing test files were extended; no test removed or weakened (no FR-20 ledger entry required for additions).

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/ -c Release` — **535 passed** (530 baseline + 5 new), 0 failed, 0 skipped.
- [x] Release build: 0 warnings / 0 errors (warnings-as-errors). New tests live in `Server.Tests`, so the conformance gate count (353, monotonic) and the public-contract-shape baseline (196 types) are untouched. Submodule gitlinks verified at recorded commits before building (EventStore `ad2c957`); no drift.

### Coverage
- AC-2 cursor scope bindings: tenant + caller + **filter (was the gap)** + generation, plus tamper / wrong-key / expired / future-dated / excessive-offset / malformed / round-trip — all four bindings now pinned, each fail-closed case asserting the safe shape and zero projection reads.
- AC-3 adapter seam: matched dispatch + unmatched domain + unmatched query-type (pre-existing) **+ fault containment + aggregate-id resolution + unresolvable-id fail-closed + missing-user fail-closed** (the gaps).
- AC-4 (filters/freshness/temporal/`Contracts` unchanged): pre-existing coverage; no source touched here.
- Scope boundary: live `/query` round-trips over a DAPR sidecar are Tier-3 integration concerns, not in scope; the temporal/citation/audit/justification reads are not yet exposed as their own `IDomainQueryHandler` adapters ("as applicable" in AC-3) — out of scope for this run.

## Story 2.2 Adopt `EventStoreAggregate<TState>` Base-Class Conventions

Story 2.2 (FR-7, remove-and-replace) is deletion-dominant: it deletes the dead `EventStoreCommandStatusIdempotencyBridge` shim and proves the SDK base-class **reflection dispatch** (`IDomainProcessor.ProcessAsync`) and **replay** (`IAggregateReplay.Replay` → `AggregateReplayer`) are the live route into `ConversationAggregate` / `ConversationState`. There is no UI or HTTP API in scope, so the automated-test surface is the SDK domain-processor entry points, driven directly. Framework: xUnit v3 + Shouldly (CPM; no new package introduced). This run treated the shipped teeth test `ConversationAggregateBaseClassDispatchTest` as the feature under test and auto-applied the discovered coverage gaps.

### Discovered Gaps → Applied (2 new [Fact] tests)
- [x] Gap 1 (AC-1 dispatch with non-null state untested) — the shipped test drove all six `Handle` overloads through `ProcessAsync` **only with `currentState: null`**, so the five state-dependent commands proved only the rejection-on-null path. If the base class ever bound `null` into `parameters[1]` regardless of `currentState`, every existing case would stay green ("prove behavior, not mirrors", Epic 1 L1/A1). Closed by exercising the **success path** with a rehydrated created state.
- [x] Gap 2 (AC-1 replay only single-event) — the shipped replay test applied a single `ConversationCreated` event; multi-event ordered accumulation through a **second** `Apply` overload was untested. Closed by replaying an ordered 2-event stream and asserting accumulation.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateBaseClassDispatchTest.cs` — extended from 10 to **12** cases (reusing the file's existing fixtures, same pure aggregate project):
  - `ProcessAsyncDeliversRehydratedNonNullStateToTheHandlerSuccessPath` — drives `AddParticipant` through `ProcessAsync` against a created state; asserts the handler **succeeds** and emits `ParticipantAddedDomainEvent` (an outcome the null-state cases can never produce), proving reflection binds the live rehydrated state. Includes a fixture-sanity guard (direct handler must genuinely succeed) so the assertion is not vacuous.
  - `ReplayAppliesAnOrderedEventSequenceThroughTheApplyConventionAccumulatingState` — replays `ConversationCreated` (seq 1) → `ParticipantAdded` (seq 2); asserts reconstruction advances to sequence 2 and the rebuilt `StateJson` carries the participant, proving the replay engine reaches >1 `Apply` overload and accumulates.

### Implementation
- No production source under `src/` changed by this QA run (the bridge removal + aggregate baseline were the dev-story step). Only the one existing test file was extended; pre-existing aggregate, idempotency, replay-verifier, and ledger tests are untouched. No test removed or weakened (no FR-20 ledger entry required for additions).

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Tests/ -c Release` — **185 passed** (183 baseline + 2 new), 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/ -c Release` — **352 passed** (standing gate, unchanged, monotonic ≥ 351), 0 failed, 0 skipped.
- [x] Release build: 0 warnings / 0 errors (warnings-as-errors). New tests live in the pure aggregate project, so the conformance gate count and the public-contract-shape baseline (196 types) are untouched.

### Coverage
- AC-1 (reflection dispatch is the live route): all 6 `Handle` overloads via `ProcessAsync` (null-state rejection paths, pre-existing) **+ success path with a rehydrated non-null state** (was the gap) — proves state is actually delivered through reflection, not a null placeholder.
- AC-1 (replay via `Apply` convention): single-event happy path + unknown-event teeth (pre-existing) **+ ordered multi-event accumulation reaching a second `Apply` overload** (was the gap).
- Teeth retained: unknown command → `InvalidOperationException("No Handle method found…")`; unknown event → `UnknownEventType`.
- AC-4 (pure aggregate tests stay green, direct `Handle`/`Apply` style): unchanged; the new dispatch tests drive the SDK path, not the pure-function style.
- Scope boundary: live `/process` round-trips over a DAPR sidecar are Tier-3 integration concerns, not in scope here.

> Note: the dev-story Dev Agent Record cites the pure aggregate count as 183; it is **185** after these additions (conformance gate count unchanged at 352). Update that count if the story is re-validated.

## Story 2.1 Wire Conversations onto the Shared Two-Line Domain-Service Host

Story 2.1 is the first `src/` production change in the initiative: `Server/Program.cs` becomes the canonical two-line EventStore domain-service host (`builder.AddEventStoreDomainService(<domain>, <server>)` + `app.UseEventStoreDomainService()`). There is no UI in this slice, so the automated-test surface is host/API-composition level (no browser E2E applies). Framework: xUnit v3 `3.2.2` + Shouldly `4.3.0` (project standard; `Microsoft.AspNetCore.Mvc.Testing` intentionally **not** introduced per the CPM/no-new-package guardrail — SDK minimal-host composition is driven directly). This run treated `ConversationsDomainServiceHostCompositionTest` as the feature under test and auto-applied the one substantive coverage gap.

### Discovered Gap → Applied (2 new [Fact] tests)
- [x] Gap (AC-1 explicit-assemblies wiring untested with teeth) — the pre-existing host-composition test asserts only **route presence**, but the SDK maps `/process`, `/query`, … unconditionally: the route table is byte-identical whether the host uses the mandated explicit-assemblies overload or the **forbidden** calling-assembly overload. A regression to calling-assembly would leave `/process` with no discoverable `ConversationAggregate` processor while every route still mapped — and every existing test would stay green ("prove behavior, not mirrors", Epic 1 L1/A1). Closed with a discovery test plus fault-injection teeth.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs` — 2 [Fact] tests:
  - `ExplicitAssemblyScanShouldRegisterConversationDomainProcessor` — the exact `Program.cs` wiring (domain + Server boundary assemblies) discovers `ConversationAggregate` and registers it as the **keyed** `IDomainProcessor` (key `"conversation"`) the request router resolves for `POST /process`.
  - `CallingAssemblyOverloadWouldNotDiscoverTheConversationDomainProcessor` — **teeth/contrast:** scanning only the Server host assembly (what the forbidden calling-assembly overload scans) registers **no** keyed `"conversation"` processor, proving the explicit domain-assembly argument is load-bearing — if `Program.cs` regressed to calling-assembly, the first fact turns RED.

### Implementation
- No production source under `src/` changed by this QA run (`Program.cs` host wiring was already implemented in the dev-story step). Only the one new test-only file above was added; pre-existing host-composition, governance-gate, boundary, and ledger tests are left untouched.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/ -c Release --filter "FullyQualifiedName~HostComposition"` — **12 passed** (10 baseline + 2 new), 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/ -c Release` — **527 passed** (525 baseline + 2 new), 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/ -c Release` — **351 passed** (standing gate, monotonic ≥ 348), 0 failed, 0 skipped.

### Coverage
- AC-1 (explicit assembly-scanning, never calling-assembly): **now covered with behavioral teeth** (was the gap) — discovery proven via the keyed `IDomainProcessor` the router actually resolves, with a calling-assembly contrast that turns the test RED on regression.
- AC-2 (canonical routes resolve + host composes): 6 canonical + 3 health routes (pre-existing route-presence facts), reinforced by proving `/process` has a real discoverable processor behind it.
- AC-5 (audit gate surfaced with teeth): covered by pre-existing `GovernanceAuditSinkFailClosedConformanceTest` (not regenerated).
- Scope boundary: live request round-trips for `/process` / `/query` / `/project` require a DAPR sidecar + EventStore gateway and are Tier-3 integration concerns deferred to Stories 2.3 / 2.5; not in scope here.

## Story 1.5 Establish Classification Dispute-Resolution and Reclassification Escape Hatch

Story 1.5 is a documentation / governance-procedure / evidence story — **zero `src/` production code and no UI or HTTP API**, so the applicable automated-test surface is the read-only structural conformance validator that gives the committed procedure artifacts *teeth* (mirroring `ConsumePromoteKeepInventoryValidationTest`), not browser E2E. This run treated `ClassificationChangeProcedureValidationTest` as the feature under test and auto-applied all discovered AC1–AC5 coverage gaps. The validator only **reads** `docs/release-evidence/classification-change-procedure-v1.{json,md}` + the accepted `consume-promote-keep-inventory-v1.json`; it never mutates anything.

### Discovered Gaps → Applied (8 new [Fact] tests)
- [x] Gap 1 (AC5 "illustrative, NOT applied") — `WorkedExampleEntriesShouldNotLeakIntoTheRealInventoryChangeLog`: no worked-example `entryId` appears in the inventory's real `changeLog`; the top-level `note` declares not-applied. (Original validator folded examples but never asserted they were never applied.)
- [x] Gap 2 (AC2/AC5e honesty-of-measurement) — `ProcedureRecordedBaselineShouldMatchTheGovernedInventory`: the procedure's frozen `acceptedInventoryBaseline` (`baselineCommit`/`plumbingBaselineLoc`/`sourceTotalLoc`) matches the governed inventory, so the fold teeth never measure against a stale baseline.
- [x] Gap 3 (AC2 "flips the label only, never approxLoc/paths") — `ReclassificationEntriesShouldNotOverrideApproxLocOrPaths`: no reclassification entry (worked example or real) carries an `approxLoc`/`paths` override.
- [x] Gap 4 (AC1 "a reclassified challenge IS a reclassification") — `EveryReclassifiedChallengeShouldHaveAMatchingReclassificationEntry`: every `resolution: reclassified` challenge has a matching `reclassification` entry on the same `areaId` (cross-entry consistency; vacuous now, guards future appends).
- [x] Gap 5 (AC2 no-silent-change, real entries) — `EveryRealReclassificationEntryShouldBeAppliedToTheLiveClassification`: a real reclassification entry's `to` equals the live inventory classification (vacuous now — `changeLog` is `[]` — bites the moment Epic 2/3 appends an entry logged-but-not-applied).
- [x] Gap 6 (AC4 bidirectional discoverability) — `GovernedInventoryShouldBackReferenceStoryOneFiveForBidirectionalDiscoverability`: the inventory's `versioningConvention` forward-references Story 1.5, so a reader landing on either artifact finds the other (the original validator only asserted procedure→inventory via `governsArtifact`).
- [x] Gap 7 (AC4 copy-pasteable template) — `EntryTemplateShouldProvideCopyPasteableShapesForBothTypes`: `entryTemplate` supplies both entry shapes, each carrying every schema-required field.
- [x] Gap 8 (AC4 documented procedure) — `ProcedureMarkdownShouldDocumentTheFiveStepProcedureAndCanonicalLog`: the `.md` documents Steps 1–5, the canonical `changeLog`, the append-only rule, the recompute rule, and both entry types (the original validator only asserted the `.md` was non-empty).

### Generated/Extended Tests
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ClassificationChangeProcedureValidationTest.cs` — extended from 9 pre-existing AC5(a)–(g) facts to **17 facts** (+8 gap-coverage facts above, added under a dedicated "QA gap-coverage facts" section mirroring the sibling inventory validator).

### Implementation
- No production source under `src/` changed (Story 1.5 is gate-zero, behavior-preservation scope). Only the one validator test file was extended; the committed release-evidence artifacts are read read-only. The accepted `consume-promote-keep-inventory-v1.{json,md}` is left byte-for-byte unchanged (`git diff --exit-code` clean); the procedure artifacts are unchanged by this QA run; no sibling submodule touched.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — **348 passed, 0 failed, 0 skipped** (340 baseline + 8 new). Duration ~240 ms.
- [x] `git diff --exit-code -- docs/release-evidence/consume-promote-keep-inventory-v1.{json,md}` — no diff (accepted inventory byte-immutable).
- [x] `git status --short -- src/` — empty (zero `src/` production changes).

### Coverage
- AC1 (challenge resolvable + recorded, never silent): schema(challenge) + upheld example + reclassified↔reclassification cross-entry consistency. ✅
- AC2 (reclassification logged, label-only, no silent change): schema(reclassification) + fold teeth + approxLoc/paths-unchanged + applied-to-live + baseline parity. ✅
- AC3 (FR-2 invariant re-holds after every change): fold-and-recheck teeth (every area once, single valid label, LOC reconciles to `sourceTotalLoc`). ✅
- AC4 (procedure documented + discoverable): `.md` 5-step content + `entryTemplate` both types + bidirectional discoverability. ✅
- AC5 (committed, schema+examples, validator with teeth, scope-clean): committed/accepted + governance fields + illustrative-not-applied + content-safety scan. ✅
- Validator facts: 17 total (9 pre-existing + 8 added). Conformance suite 348 passed / 0 failed / 0 skipped.

## Story 1.1 Pin the Conformance Oracle Green on Main and Snapshot the Public Contract Shape

Story 1.1 is an evidence-artifact story (no UI / no HTTP API), so the appropriate automated tests follow the project's established committed-artifact validation pattern (`ConformanceManifestValidationTest`), not browser E2E. Existing coverage (`PublicContractShapeSnapshotGenerationTest`, 4 tests) exercises only the in-memory snapshot; the committed evidence files were entirely unguarded. Gaps auto-applied per request.

### Discovered Gaps → Applied
- [x] Gap 1 (AC1/AC4) — `release-baseline-v1.json` was never read back or shape-validated.
- [x] Gap 2 (AC1/FR-20) — the 14 enumerated suite classes were not verified to exist, and the suite count was not pinned (no "no suite silently added/dropped" guard).
- [x] Gap 3 (AC2) — no drift guard tying the committed contract-shape snapshot to the live exported Contracts surface (the Story 5.1 mechanic itself).
- [x] Gap 4 (AC1/AC2) — no cross-artifact consistency check (baseline-reported 196 == snapshot `typeCount` == live exported-type count).
- [x] Gap 5 (guardrail) — no content-safety scan over the committed on-disk snapshot surface.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseBaselineValidationTest.cs` — 8 [Fact] tests over the committed evidence: `CommittedBaselineRecordShouldExistAndDescribeAGreenAllPassOracle`, `BaselineCommitShouldBeAFullFortyCharacterHexShaOnMain`, `BaselineEnumeratedSuiteClassesShouldMatchTheActualSuiteClassesInTheAssembly`, `BaselineSurvivabilityClassificationShouldAccountForAllFourteenSuites`, `CommittedSnapshotShouldExistAndDeclareItsAssemblyAndTypeCount`, `CommittedSnapshotTypeCountShouldMatchTheLiveExportedContractSurface`, `CommittedSnapshotCapturedSurfaceShouldPassContentSafetyScan`, `BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface`.

### Implementation
- No production source under `src/` changed (Story 1.1 behavior-preservation scope). Only the one new test-only file above was added; the committed release-evidence artifacts are read read-only.

### Validation
- [x] `dotnet test ... --filter "FullyQualifiedName~ReleaseBaselineValidationTest"` — 8 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — 260 passed, 0 failed, 0 skipped (was 252; +8 new).

### Coverage
- AC1: committed baseline record exists, is shape-valid, reports all-pass/green, 40-char hex SHA on `main`, and its 14 enumerated suite classes match exactly the 14 `*ConformanceSuiteTest` classes present in the assembly.
- AC2: committed contract-shape snapshot exists, its `typeCount` matches the captured payload and the live exported Contracts surface (drift guard), and its captured surface passes the content-safety scan on the on-disk bytes.
- AC4: cross-artifact consistency — baseline-reported type count, committed snapshot `typeCount`, and live exported-type count all agree, and the baseline's snapshot pointer references a file that exists.
- Scope boundary: AC3 survivability classification is asserted complete/consistent (all 14 suites accounted for); decoupling is deliberately left to Story 1.3 and not tested here.

## Story 3.8B Verify Accessibility Tree, Keyboard, and Screen-Reader Safety

### Generated Tests
- [x] `tests/Hexalith.Conversations.Admin.Web.Tests/Rendering/InvestigationWorkspaceAccessibilityTest.cs` — 7 [Fact] renderer unit tests: exactly one document `<h1>`; heading outline follows trust order (h1 → trust-order h2 → five trust-panel h3 → timeline h2); skip link + banner/search/main landmarks + programmatic `tabindex="-1"` skip target; no positive/zero tabindex; safe `aria-live="polite"` status region with no poison sentinel; disabled governance commands expose the safe blocked reason via `aria-describedby` to a `command-reason` span while preserving the `disabled aria-disabled="true"` contract; hidden-read renders byte-identical for unauthorized-existing and nonexistent records.
- [x] `tests/Hexalith.Conversations.Admin.Web.Tests/Accessibility/AccessibilityEvidenceHarnessTest.cs` — 1 Playwright-backed [Fact] driving headless Chromium against the running host across 15 scenario rows (all 12 fixtures at desktop, plus a forced-colors+reduced-motion row and two 200%-zoom rows). Per row it captures the accessibility tree via `Locator.AriaSnapshotAsync()`, the heading outline, landmark roles via `Page.GetByRole`, the keyboard Tab focus-order trace, and the resolved accessible-name/description surface, then asserts single-h1 heading order, banner/search/main + skip link presence, trust-order-before-timeline, accessible-name forbidden-sentinel absence, skip-link-first focus order, content-safe focus trace, blocked-command reasons resolvable through `aria-describedby`, and honored forced-colors/reduced-motion context. Suite-level guards prove command buttons and a forced-colors row were actually exercised.

### Implementation
- [x] `src/Hexalith.Conversations.Admin.Web/Rendering/InvestigationWorkspaceRenderer.cs` — extended the 3.8A renderer with accessibility semantics (no new disclosure; all text still derived from the already-authorized/redacted view model). Added a single document `<h1>` in a banner landmark; a `role="search"` find landmark; a `#governed-record` skip link targeting `<main tabindex="-1" aria-labelledby="workspace-title">`; a coherent heading outline (section `h2` for Trust order / Evidence timeline, `h3` for panels and rows) replacing the previous duplicated `<h1>`/`<h2>`; a safe `aria-live="polite"` `role="status"` region announcing the safe trust/completeness/command classes; accessible blocked-reason descriptions wired with `aria-describedby` to visible `command-reason` text; a `:focus-visible` outline that switches to system `Highlight` under forced colors. The redundant responsive duplicate surfaces are marked `aria-hidden="true"` so assistive technology hears the trust posture once. All `data-testid`, `data-trust-rank`, telemetry-label, forced-colors/reduced-motion, HTML-encoding, and indistinguishable hidden-read contracts from 3.8A are preserved.
- [x] `_bmad-output/implementation-artifacts/evidence/3-8b-accessibility-tree-keyboard-screen-reader-safety/` — generated `accessibility-matrix.json`, `aria-snapshots.json` (captured accessibility tree per fixture/mode), `focus-order-trace.json`, `accessible-name-scan.json`, `fixture-matrix.json`, `evidence-summary.md`, and the `manual-keyboard-screen-reader-notes.md` keyboard/screen-reader walkthrough + human Narrator/NVDA confirmation checklist.
- [x] `tests/README.md` — documented the accessibility lane running alongside the responsive lane in the same project.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Admin.Web.Tests/Hexalith.Conversations.Admin.Web.Tests.csproj` — 14 passed (5 baseline 3.8A renderer + 7 new accessibility renderer + 1 responsive harness + 1 new accessibility harness); 3.8A responsive lane stays green.
- [x] `dotnet test Hexalith.Conversations.slnx` — 1529 passed, 0 failed, 0 skipped (Contracts 580, Server 503, Conformance 248, Core 153, Client 23, Admin.Web 14, Integration 8), up from the 1519 baseline after 3.8A.
- [x] `dotnet build Hexalith.Conversations.slnx --configuration Release` — 0 warnings, 0 errors.
- [x] Playwright Chromium confirmed installed (`%LOCALAPPDATA%/ms-playwright`); host + browser launch verified end-to-end.

### Coverage
- AC1: The captured accessibility tree and Tab focus trace prove keyboard/AT traversal reaches tenant scope, identity, trust posture, evidence completeness, and the command gate before timeline reliance (single-h1 heading outline + data-trust-rank order + skip-link-first focus). Blocked-action reasons and safe next actions are exposed through accessible descriptions; redacted/unauthorized values are absent from accessible names, descriptions, live region, headings, and the focus trace (forbidden-sentinel scan over the resolved accessible-name surface and the aria snapshot).
- AC2: Accessibility safety is exercised across no-access (cross-tenant + unauthorized-existing hidden reads), denied/blocked-command, redacted, stale/rebuilding, missing-citation, unresolved-participant, permission-downgrade, virtualized-restricted, and high-contrast/reduced-motion/200%-zoom rows. Each row records component, surface, scenario, and pass/fail in `accessibility-matrix.json`.
- AC3: ARIA snapshots, focus-order traces, accessible-name scans, and the manual notes are tenant-safe and content-safe (no poison sentinel), generated from the canonical `BuyerAcceptanceInvestigationWorkspaceCatalog` fixtures, and linked to this story's evidence bundle.
- Scope boundary: This story does not re-prove Story 3.8A responsive layout / mobile safe-triage (kept green) or Story 3.8C leakage/clipboard/browser-title/full telemetry-disclosure closure (owned there).

## Story 3.8A Verify Responsive Layout and Mobile Safe Triage

### Generated Tests
- [x] `tests/Hexalith.Conversations.Admin.Web.Tests/Rendering/InvestigationWorkspaceRendererTest.cs` — 3 [Fact] tests covering required responsive fixture exposure, cross-tenant poison sentinel absence in the no-access render, trust-order test IDs, and disabled governance-changing mobile triage actions.
- [x] `tests/Hexalith.Conversations.Admin.Web.Tests/Responsive/ResponsiveEvidenceHarnessTest.cs` — 1 Playwright-backed [Fact] test that renders 11 canonical fixtures across 7 viewport/zoom rows (77 evidence rows) and validates trust order, mobile safe triage, responsive duplicate safety, poison-sentinel absence in DOM text/attributes/title/duplicates/telemetry labels, safe viewport telemetry labels, high contrast, reduced motion, and 200 percent browser-zoom equivalents.

### Implementation
- [x] `src/Hexalith.Conversations.Admin.Web/` — new first-party rendered Admin Web host for the narrow Find -> Read -> Trust investigation workspace evidence surface. It consumes permission-safe Conversations projection/query DTOs plus existing buyer-acceptance synthetic fixtures and does not query raw EventStore streams, logs, envelopes, aggregate internals, or provider session IDs.
- [x] `src/Hexalith.Conversations.Admin.Web/Rendering/` — permission-safe catalog, view model, fixture summaries, and renderer. The renderer preserves tenant scope, record identity, trust posture, evidence completeness, command eligibility, then timeline order in DOM/visual checks; all governance-changing actions render disabled from the read surface.
- [x] `src/Hexalith.Conversations.AppHost/Program.cs` and `.csproj` — AppHost now registers the Admin Web host as `conversations-admin-web`.
- [x] `tests/install-playwright.ps1` and `tests/README.md` — documented and automated Playwright Chromium installation for the rendered evidence lane.
- [x] `_bmad-output/implementation-artifacts/evidence/3-8a-responsive-layout-mobile-safe-triage/` — generated `viewport-matrix.json`, `fixture-matrix.json`, `safe-telemetry-label-scan.json`, `evidence-summary.md`, and representative screenshots.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Admin.Web.Tests/Hexalith.Conversations.Admin.Web.Tests.csproj` — 4 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` — 580 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationTestIds"` — 5 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` — 103 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` — 1519 passed.
- [x] `dotnet build Hexalith.Conversations.slnx --configuration Release` — 0 warnings, 0 errors.
- [x] Live browser sanity: `http://127.0.0.1:5183/investigations?fixture=TenantA_Admin_FullTrust` loaded with no console warnings/errors and full-page screenshot captured.

### Coverage
- AC1: Desktop, tablet, mobile, wide desktop, and 200 percent zoom rows validate that tenant scope, record identity, trust posture, evidence completeness, and command eligibility precede timeline reliance. Mobile triage remains read-only; governance-changing controls are disabled with safe reasons.
- AC2: Sticky summaries, drawer summaries, skeleton placeholders, condensed panels, and timeline rows are rendered from permission-safe DTO/view-model data. Cross-tenant poison sentinel values are asserted absent from DOM text, attributes, page title, responsive duplicate markup, and telemetry labels.
- AC3: Evidence is generated from canonical buyer-acceptance fixtures extended into 11 responsive scenario IDs. The evidence folder contains machine-readable viewport, fixture, telemetry scan, and screenshot outputs traceable to this story.
- Scope boundary: This story does not claim Story 3.8B accessibility-tree/screen-reader closure or Story 3.8C clipboard/browser-title/full telemetry-disclosure closure.

## Story 6.7 Publish Responsibility Boundary Documentation

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ResponsibilityBoundaryValidationTest.cs` — 11 [Fact] tests: ResponsibilityBoundaryDocument_Exists_AtExpectedPath, ResponsibilityBoundaryDocument_ContainsAllRequiredSections, ResponsibilityBoundaryDocument_MentionsAll10AdjacentSystems, ResponsibilityBoundaryDocument_MentionsConversationsOwnedConcepts, ResponsibilityBoundaryDocument_MentionsBoundaryStructure, ResponsibilityBoundaryDocument_MentionsInheritedControls, ResponsibilityBoundaryDocument_MentionsRequirementFR104, ResponsibilityBoundaryDocument_RelatedLinksAreWellFormed, ResponsibilityBoundaryDocument_FreeTextPassesContentSafety, ResponsibilityBoundaryDocument_DoesNotClaimOwnershipOfAdjacentSystems, IntegrationGuide_LinksToResponsibilityBoundaries.

### Implementation
- [x] `docs/responsibility-boundaries.md` — New file: operator/buyer-evaluator/compliance-stakeholder boundary document with 6 required sections (Overview, What Conversations Owns, Responsibility Boundaries, Inherited Platform Controls, Requirement Mapping, Related Documentation) and all 10 FR104 adjacent-system boundaries (chatbot, LLM provider, legal-hold, attachment, identity, tenant lifecycle, project/folder, Party personal data, provider availability, Hexalith platform controls), each with Owner, Source of Truth, Failure Semantics, Evidence Obligation, and Handoff Path.
- [x] `docs/integration-guide.md` — Added one cross-reference sentence in the "Responsibility Boundaries" section pointing to `responsibility-boundaries.md`.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` — Story 6.7 entry added (entry 17): testId=story-6-7-responsibility-boundary-documentation, requirementId=FR104, measurementMethod=automated-doc-validation-test, evidenceArtifactHandle=responsibility-boundary-document, releaseGateId=null.

### Validation
- [x] Targeted tests: `dotnet test ... --filter "FullyQualifiedName~ResponsibilityBoundary|FullyQualifiedName~IntegrationGuide"` — 18 passed (11 new + 7 pre-existing).
- [x] Full Contracts suite: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/...` — 579 passed (568 baseline + 11 new).
- [x] Full solution: all 1482 tests pass (23 Client + 216 Conformance + 8 Integration + 153 Core + 503 Server + 579 Contracts).
- [x] `dotnet build Hexalith.Conversations.slnx` — 0 warnings, 0 errors.

### Coverage
- AC1: `docs/responsibility-boundaries.md` distinguishes Conversations responsibilities from all 10 adjacent systems: chatbot, LLM provider sessions, legal-hold authority, attachment storage, identity, tenant lifecycle, project/folder lifecycle, Party personal data, provider availability, and Hexalith platform controls. Each boundary names what Conversations does NOT own.
- AC2: All 10 boundary sections carry Owner, Source of Truth, Failure Semantics, Evidence Obligation, and Handoff Path. Document does not claim Conversations owns data or authority delegated to EventStore, Tenants, Parties, Folders, FrontComposer, or provider systems.
- AC3: `ResponsibilityBoundaryValidationTest.cs` validates structure (6 required sections), FR104 adjacent-system coverage (all 10), Conversations-owned concepts, boundary structure fields, inherited controls, and cross-reference from integration guide. Content-safety scan uses conservative forbidden list; ownership violation assertions prevent stale or contradictory ownership claims.

## Story 6.6 Track Second-Adopter Status and Downgrade-Rule Milestones

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterVocabularyTest.cs` — 8 [Fact] tests: SecondAdopterStatus_AllContains4Values, SecondAdopterStatus_Parse_Identified_ReturnsIdentified, SecondAdopterStatus_Parse_Qualified_ReturnsQualified, SecondAdopterStatus_Parse_Deferred_ReturnsDeferred, SecondAdopterStatus_Parse_Disqualified_ReturnsDisqualified, SecondAdopterStatus_Parse_UnknownValue_ThrowsArgumentException, SecondAdopterStatus_SerializesAndDeserializesToCorrectValue, SecondAdopterStatus_Disqualified_WireValueIsDisqualified.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/SecondAdopterStatusValidatorTest.cs` — 10 [Fact] tests: ValidateEntry_Identified_FutureMilestone_ReturnsNoErrors, ValidateEntry_Qualified_WithTrigger_ReturnsNoErrors, ValidateEntry_Deferred_WithValidWaiver_ReturnsNoErrors, ValidateEntry_Disqualified_WithRationale_ReturnsNoErrors, ValidateEntry_MilestoneOverdue_ReturnsMilestoneOverdue, ValidateEntry_ReviewOverdue_ReturnsReviewOverdue, ValidateEntry_Qualified_NoTrigger_ReturnsQualifiedNoDowngradeTrigger, ValidateEntry_WaiverExpired_ReturnsWaiverExpired, ValidateEntry_Disqualified_NoRationale_ReturnsRevertedMissingRationale, ValidateEntry_Deferred_NoWaiverRef_DoesNotTriggerWaiverExpired.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuiteTest.cs` — 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseGovernancePreconditionCheckId, AllPassScenariosShouldProduceReadyOutcome, AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails, AllChecksShouldBeClassifiedAsConformant, AllChecksShouldCarryFR103RequirementAndSecondAdopterMappings, PassScenariosShouldHaveNullTypedError, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip, NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow, MilestoneOverdueShouldProduceConformantResult, RevertedNoRationaleShouldProduceConformantResult.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterVocabulary.cs` — New file: SecondAdopterStatus sealed record vocabulary (4 values: Identified, Qualified, Deferred, Disqualified).
- [x] `src/Hexalith.Conversations.Contracts/Conformance/SecondAdopterStatusEntryV1.cs` — New file: SecondAdopterStatusEntryV1 positional record with eager field validation (12 params) + SecondAdopterStatusValidator static class (5 error tokens: milestone-overdue, review-overdue, qualified-no-downgrade-trigger, waiver-expired, reverted-missing-rationale).
- [x] `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — Added SecondAdopterStatusJsonConverter.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — Added SecondAdopterStatus (4 values) and SecondAdopterStatusEntryV1 sample.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceFixtures.cs` — SecondAdopterScenarioData record + SecondAdopterConformanceSeedData with 10 deterministic synthetic scenarios.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuite.cs` — Suite runner; SuiteId=second-adopter-suite; calls SecondAdopterStatusValidator.ValidateEntry per scenario; conformant when actual errors match expected errors.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` — Story 6.6 entry added: testId=story-6-6-second-adopter-status, requirementId=FR103, releaseGateId=null, evidenceArtifactHandle=second-adopter-suite-result.

### Validation
- [x] Targeted tests (vocabulary + validator): `dotnet test ... --filter "FullyQualifiedName~SecondAdopter"` — 18 passed.
- [x] Targeted tests (conformance suite): `dotnet test ... --filter "FullyQualifiedName~SecondAdopterConformance"` — 15 passed.
- [x] Full Contracts suite: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/...` — 568 passed (550 baseline + 18 new).
- [x] Full Conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` — 216 passed (201 baseline + 15 new).
- [x] Full solution: all 1471 tests pass (23 Client + 216 Conformance + 8 Integration + 153 Core + 503 Server + 568 Contracts).

### Coverage
- AC1: SecondAdopterStatusEntryV1 (12 params) carries all required governance fields: status, affected requirements, review owner, milestone date, downgrade-rule trigger, capability ref, waiver ref/expiry, rationale, conformance artifact, review date.
- AC2: SecondAdopterStatusValidator identifies capabilities requiring review via 5 error tokens; milestone-overdue and review-overdue surface timing issues; qualified-no-downgrade-trigger enforces downgrade rule; waiver-expired and reverted-missing-rationale enforce lifecycle governance.
- AC3: All 5 validator error tokens tested; 10-scenario conformance suite covers all AC3-required paths; serialization stability and content-safety verified.

## Story 6.5 Support Buyer Partial Acceptance and Waiver Review

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/BuyerAcceptanceVocabularyTest.cs` — 8 [Fact] tests: BuyerAcceptanceItemStatus_AllContains4Values, BuyerAcceptanceItemStatus_Parse_Accepted_ReturnsAccepted, BuyerAcceptanceItemStatus_Parse_Excluded_ReturnsExcluded, BuyerAcceptanceItemStatus_Parse_UnknownAccepted_ReturnsUnknownAccepted, BuyerAcceptanceItemStatus_Parse_Waived_ReturnsWaived, BuyerAcceptanceItemStatus_Parse_UnknownValue_ThrowsArgumentException, BuyerAcceptanceItemStatus_SerializesAndDeserializesToCorrectValue, BuyerAcceptanceItemStatus_UnknownAccepted_WireValueIsUnknownAccepted.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/BuyerPartialAcceptanceValidatorTest.cs` — 11 [Fact] tests: ValidateItem_Accepted_WithAck_ReturnsNoErrors, ValidateItem_Excluded_NoAckRequired_ReturnsNoErrors, ValidateItem_UnknownAccepted_WithAck_ReturnsNoErrors, ValidateItem_Waived_WithWaiverLink_ReturnsNoErrors, ValidateItem_Blocker_WithApprover_ReturnsNoErrors, ValidateItem_Accepted_MissingAck_ReturnsMissingBuyerAcknowledgement, ValidateItem_Blocker_MissingApprover_ReturnsBlockerRequiresApprover, ValidateItem_ExpiredItem_ReturnsExpiredAcceptanceItem, ValidateItem_ReviewDue_ReturnsReviewDue, ValidateItem_Waived_NoLink_ReturnsWaivedMissingWaiverLink, ValidateItem_Excluded_DoesNotRequireBuyerAck.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceSuiteTest.cs` — 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseGovernancePreconditionCheckId, AllPassScenariosShouldProduceReadyOutcome, AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails, AllChecksShouldBeClassifiedAsConformant, AllChecksShouldCarryFR102RequirementAndBuyerAcceptanceMappings, PassScenariosShouldHaveNullTypedError, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip, NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow, ExpiredItemShouldProduceConformantResult, MissingAckShouldProduceConformantResult.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/BuyerAcceptanceVocabulary.cs` — New file: BuyerAcceptanceItemStatus sealed record vocabulary (4 values: Accepted, Excluded, UnknownAccepted, Waived).
- [x] `src/Hexalith.Conversations.Contracts/Conformance/BuyerPartialAcceptanceItemV1.cs` — New file: BuyerPartialAcceptanceItemV1 positional record with eager field validation (15 params) + BuyerPartialAcceptanceItemValidator static class (5 error tokens: blocker-requires-approver, missing-buyer-acknowledgement, expired-acceptance-item, review-due, waived-missing-waiver-link).
- [x] `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — Added BuyerAcceptanceItemStatusJsonConverter.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — Added BuyerAcceptanceItemStatus (4 values) and BuyerPartialAcceptanceItemV1 samples.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceFixtures.cs` — BuyerAcceptanceScenarioData record + BuyerAcceptanceConformanceSeedData with 10 deterministic synthetic scenarios.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceSuite.cs` — Suite runner; SuiteId=buyer-acceptance-suite; calls BuyerPartialAcceptanceItemValidator.ValidateItem per scenario; conformant when actual errors match expected errors.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` — Story 6.5 entry added: testId=story-6-5-buyer-partial-acceptance, requirementId=FR102, releaseGateId=null, evidenceArtifactHandle=buyer-acceptance-suite-result.

### Validation
- [x] Targeted tests (vocabulary + validator): `dotnet test ... --filter "FullyQualifiedName~BuyerAcceptance"` — 27 passed.
- [x] Targeted tests (conformance suite): `dotnet test ... --filter "FullyQualifiedName~BuyerAcceptanceConformance"` — 15 passed.
- [x] Full Contracts suite: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/...` — 550 passed (531 baseline + 19 new).
- [x] Full Conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` — 201 passed (186 baseline + 15 new).

### Coverage
- AC1: BuyerPartialAcceptanceItemV1 (15 params) carries all required governance fields: accepted capabilities, excluded capabilities, active waivers, unknown-accepted items, compensating controls, owners, expiry dates, buyer acknowledgement, review milestones, and links to signed conformance artifacts and release manifests.
- AC2: BuyerPartialAcceptanceItemValidator enforces "blocker-requires-approver" for release blocker items; buyer-visible rationale enforced via required Approver field on blockers.
- AC3: All 5 validator error tokens tested; 10-scenario conformance suite covers all AC3-required paths; serialization stability and content-safety verified.
- AC4: BuyerPartialAcceptanceItemValidator links directly to waiver entries, conformance manifest rows, affected stories, and release-scope consequence statements; missing links detected by "waived-missing-waiver-link" and "missing-buyer-acknowledgement" tokens.

## Story 6.4 Classify Release Scope and Deferred Capability Consequences

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/CapabilityReleaseScopeVocabularyTest.cs` — 9 [Fact] tests: CapabilityReleaseScope_AllContains7Values, CapabilityReleaseScope_Parse_V1_ReturnsV1, CapabilityReleaseScope_Parse_Deferred_ReturnsDeferred, CapabilityReleaseScope_Parse_UnknownValue_ThrowsArgumentException, SubstrateConsequenceArea_AllContains8Values, SubstrateConsequenceArea_Parse_TenantIsolation_ReturnsTenantIsolation, SubstrateConsequenceArea_Parse_UnknownValue_ThrowsArgumentException, CapabilityReleaseScope_SerializesAndDeserializesToCorrectValue, SubstrateConsequenceArea_SerializesAndDeserializesToCorrectValue.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/CapabilityReleaseScopeValidatorTest.cs` — 10 [Fact] tests: ValidateEntry_V1Scope_ReturnsNoErrors, ValidateEntry_V1Point1Scope_ReturnsNoErrors, ValidateEntry_DeferredWithConsequences_ReturnsNoErrors, ValidateEntry_DeferredNoConsequences_ReturnsDeferredSubstrateNoConsequences, ValidateEntry_WaivedWithReference_ReturnsNoErrors, ValidateEntry_WaivedNoReference_ReturnsWaivedNoReference, ValidateEntry_ConditionalWithFutureExpiry_ReturnsNoErrors, ValidateEntry_ConditionalWithPastExpiry_ReturnsExpiredConditionalScope, ValidateEntry_ConditionalNullExpiry_ReturnsExpiredConditionalScope, ValidateEntry_OutOfScope_ReturnsNoErrors.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceSuiteTest.cs` — 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseGovernancePreconditionCheckId, AllPassScenariosShouldProduceReadyOutcome, AllFailScenariosShouldProduceBlockedOutcomeWhenValidatorFails, AllChecksShouldBeClassifiedAsConformant, AllChecksShouldCarryFR100RequirementAndReleaseScopeMappings, PassScenariosShouldHaveNullTypedError, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip, NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow, DeferredNoAreasShouldProduceConformantResult, WaivedNoRefShouldProduceConformantResult.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/CapabilityReleaseScopeVocabulary.cs` — New file: CapabilityReleaseScope (7 values: V1, V1Point1, VNext, Deferred, Waived, Conditional, OutOfScope) and SubstrateConsequenceArea (8 values: TenantIsolation, AuditPairing, Idempotency, SchemaEvolution, ProjectionFreshness, RedactionReplay, ProviderPortability, AdopterCompatibility) sealed record vocabularies.
- [x] `src/Hexalith.Conversations.Contracts/Conformance/CapabilityReleaseScopeEntryV1.cs` — New file: CapabilityReleaseScopeEntryV1 positional record with eager field validation + CapabilityReleaseScopeValidator static class (3 error tokens: deferred-substrate-no-consequences, waived-no-reference, expired-conditional-scope).
- [x] `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` — Added CapabilityReleaseScopeJsonConverter and SubstrateConsequenceAreaJsonConverter.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceFixtures.cs` — ReleaseScopeScenarioData record + ReleaseScopeConformanceSeedData with 10 deterministic synthetic scenarios.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceSuite.cs` — Suite runner; SuiteId=release-scope-suite; calls CapabilityReleaseScopeValidator.ValidateEntry per scenario; conformant when actual errors match expected errors.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` — Story 6.4 entry added: testId=story-6-4-release-scope-classification, requirementId=FR100, releaseGateId=null, evidenceArtifactHandle=release-scope-suite-result.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` — Added CapabilityReleaseScope, SubstrateConsequenceArea, and CapabilityReleaseScopeEntryV1 samples.

### Validation
- [x] Targeted tests (vocabulary): `dotnet test ... --filter "FullyQualifiedName~CapabilityReleaseScope"` — 19 passed.
- [x] Targeted tests (conformance suite): `dotnet test ... --filter "FullyQualifiedName~ReleaseScopeConformance"` — 15 passed.
- [x] Full Contracts suite: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/...` — 531 passed (512 baseline + 19 new).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` — 186 passed (171 baseline + 15 new).
- [x] Full solution: `dotnet test Hexalith.Conversations.slnx` — 1404 tests, 0 failures (Client 23, Conformance 186, Integration 8, Core 153, Server 503, Contracts 531).

### Coverage
- AC1: CapabilityReleaseScope (7 values) + CapabilityReleaseScopeEntryV1 carry full traceability fields (RequirementRef, ReleaseGateRef, DependencyRef, Owner, ReviewDateUtc); JSON serialization round-trip verified.
- AC2: SubstrateConsequenceArea (8 values) + CapabilityReleaseScopeValidator error token "deferred-substrate-no-consequences" enforces that deferred entries must name substrate impact areas; cannot hide behind generic deferred label.
- AC3: All 3 validator error tokens tested (deferred-substrate-no-consequences, waived-no-reference, expired-conditional-scope); 10-scenario conformance suite covers pass/fail paths; serialization stability and content-safety verified.

## Story 6.3 Surface Conformance and Verification Status for Incidents and CI

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationConformanceStatusClassifierTest.cs` — 13 [Fact] tests: Classify_ConformantReady_ReturnsPass, Classify_ConformantBlocked_ReturnsPass, Classify_ConformantDegraded_ReturnsStaleEvidence, Classify_ConformantUnknown_ReturnsUnknownAccepted, Classify_ProductInvariant_ReturnsFail, Classify_Infrastructure_ReturnsInfrastructureFailure, Classify_UnavailableDependency_ReturnsInfrastructureFailure, Classify_ExecutionClassification_ReturnsExecutionFailure, Classify_Configuration_ReturnsExecutionFailure, ClassifyGate_Pass_ReturnsPass, ClassifyGate_Fail_ReturnsFail, ClassifyGate_Waived_ReturnsWaived, ClassifyGate_UnknownAccepted_ReturnsUnknownAccepted.
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationConformanceTelemetryTest.cs` — 10 [Fact] tests: RecordConformanceOutcome_PassClass_EmitsBoundedCounterWithCorrectDimensions, RecordConformanceOutcome_FailClass_EmitsBlockingTrueDimension, RecordConformanceOutcome_WaivedClass_EmitsBlockingFalseDimension, RecordConformanceOutcome_InfrastructureFailure_EmitsBoundedCounter, RecordConformanceOutcome_StaleEvidence_EmitsBoundedCounter, RecordConformanceOutcome_LogMessageContainsOnlyBoundedFields_NoTenantOrConversationIds, RecordConformanceOutcome_NoneClass_ThrowsArgumentException, RecordConformanceOutcome_EmptyGateId_ThrowsArgumentException, RecordConformanceOutcome_EmptyCorrelationId_ThrowsArgumentException, AddConversationConformanceTelemetry_RegistersServiceCorrectly.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConformanceStatusConformanceSuiteTest.cs` — 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseGovernancePreconditionCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR99RequirementAndConformanceStatusMappings, PreconditionMappingsShouldNotBeEmpty, PassScenariosShouldHaveNullTypedError, FailScenariosShouldHaveNonNullTypedError, OnlyProductInvariantFailScenarioShouldHaveBlockingTrue, WaivedGateScenarioShouldProduceReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip, NullScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceStatusClass.cs` — New enum (8 values: None, Pass, Fail, Waived, UnknownAccepted, InfrastructureFailure, StaleEvidence, ExecutionFailure).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceStatusClassifier.cs` — Static helper mapping ConformanceOutcome+ConformanceFailureClassification → ConversationConformanceStatusClass and ReleaseGateStatus → ConversationConformanceStatusClass.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/IConversationConformanceTelemetry.cs` — Interface with RecordConformanceOutcome(statusClass, safeGateId, isBlocking, correlationId).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetry.cs` — Implementation using IMeterFactory (counter: conversations.conformance.outcomes with status_class, gate_id, blocking dimensions) and ILogger with content-safe template. None class guard throws ArgumentException.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetryServiceCollectionExtensions.cs` — AddConversationConformanceTelemetry() registers singleton.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConformanceStatusConformanceFixtures.cs` — ConformanceStatusScenarioData record + ConformanceStatusConformanceSeedData with 10 deterministic synthetic scenarios (note: placed in Conformance.Tests rather than Testing due to Testing→Server boundary constraint).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConformanceStatusConformanceSuite.cs` — Suite runner; SuiteId=conformance-status-suite; calls Classify or ClassifyGate per scenario; maps conformant → Ready/Conformant/null, non-conformant → Blocked/ProductInvariant/error.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` — Story 6.3 entry added: testId=story-6-3-conformance-status, requirementId=FR99, releaseGateId=null, evidenceArtifactHandle=conformance-status-suite-result.

### Validation
- [x] Targeted tests (classifier): `dotnet test ... --filter "FullyQualifiedName~ConversationConformanceStatus"` — 13 passed.
- [x] Targeted tests (telemetry): `dotnet test ... --filter "FullyQualifiedName~ConversationConformanceTelemetry"` — 10 passed.
- [x] Targeted tests (conformance suite): `dotnet test ... --filter "FullyQualifiedName~ConformanceStatus"` — 15 passed.
- [x] Full Server suite: `dotnet test tests/Hexalith.Conversations.Server.Tests/...` — 503 passed (480 baseline + 23 new).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...` — 170 passed (155 baseline + 15 new).
- [x] Full solution: `dotnet test Hexalith.Conversations.slnx` — 1369 tests, 0 failures (Client 23, Conformance 170, Integration 8, Core 153, Server 503, Contracts 512).

### Coverage
- AC1: ConversationConformanceStatusClass (8 values: None, Pass, Fail, Waived, UnknownAccepted, InfrastructureFailure, StaleEvidence, ExecutionFailure); counter conversations.conformance.outcomes emits bounded status_class, gate_id, blocking dimensions; no TenantId, ConversationId, or free-text dimensions.
- AC2: ConversationConformanceStatusClassifier.Classify maps ConformanceFailureClassification (non-Conformant overrides outcome) + ConformanceOutcome (Conformant path); ClassifyGate maps ReleaseGateStatus; all 9 classifier table entries verified; Waived only reachable via ClassifyGate.
- AC3: 15 conformance suite tests cover classifier mapping, telemetry bounds, serialization stability, content-safety, and conformance manifest traceability; None guard, empty gate ID, empty correlation ID guards all tested.

## Story 6.2 Observe Projection Lag, Rebuild, Availability, and Publication Failures

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionFreshnessClassifierTest.cs` — 9 [Fact] tests: ClassifyTrustState_Current_ReturnsCurrent, ClassifyTrustState_Stale_ReturnsStale, ClassifyTrustState_Rebuilding_ReturnsRebuilding, ClassifyTrustState_Unavailable_ReturnsUnavailable, ClassifyTrustState_Forbidden_ReturnsUnavailable, ClassifyLag_StaleThresholdExceeded_ReturnsThresholdBreached, ClassifyLag_GapDetected_ReturnsCriticalLag, ClassifyLag_OutOfOrderEvent_ReturnsCriticalLag, ClassifyLag_Current_ReturnsWithinThreshold.
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationPublicationFailureClassifierTest.cs` — 4 [Fact] tests: ClassifyCode_SchemaVersionUnsupported_ReturnsUnsupportedSchema, ClassifyCode_TenantContextMismatch_ReturnsTenantViolation, ClassifyCode_TenantIsolationViolation_ReturnsTenantViolation, ClassifyCode_CommandValidationFailed_ReturnsTransientFailure.
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionTelemetryTest.cs` — 11 [Fact] tests: RecordProjectionFreshnessState_CurrentWithinThreshold_EmitsBoundedCounterWithBothDimensions, RecordProjectionFreshnessState_Stale_EmitsBoundedCounterWithStaleClass, RecordProjectionFreshnessState_Rebuilding_EmitsBoundedCounterWithRebuildingClass, RecordProjectionFreshnessState_LogMessageContainsOnlyBoundedFields_NoTenantOrConversationIds, RecordProjectionRebuildProgress_Rebuilding_EmitsBoundedCounter, RecordProjectionRebuildProgress_NoneClass_ThrowsArgumentException, RecordPublicationFailure_UnsupportedSchema_EmitsBoundedCounter, RecordPublicationFailure_LogMessageContainsOnlyBoundedFields, RecordPublicationFailure_NoneClass_ThrowsArgumentException, RecordProjectionFreshnessState_NoneClass_ThrowsArgumentException, AddConversationProjectionTelemetry_RegistersServiceCorrectly.

### Implementation
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClass.cs` — New enum (6 values: None, Current, Stale, Rebuilding, Unavailable, PartiallyRebuilt).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionLagClass.cs` — New enum (5 values: None, WithinThreshold, ThresholdBreached, CriticalLag, Unavailable).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClass.cs` — New enum (6 values: None, TransientFailure, UnsupportedSchema, DeadLettered, ReplayRequired, TenantViolation).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs` — Static helper mapping ProjectionTrustState → ConversationProjectionFreshnessClass and ProjectionFreshnessReasonCode → ConversationProjectionLagClass. Forbidden collapses to Unavailable (side-channel prevention).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClassifier.cs` — Static helper mapping ConversationErrorCode → ConversationPublicationFailureClass.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/IConversationProjectionTelemetry.cs` — Interface with RecordProjectionFreshnessState, RecordProjectionRebuildProgress, RecordPublicationFailure.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs` — Implementation using IMeterFactory (counters: conversations.projection.freshness, conversations.projection.rebuild, conversations.publication.failures) and ILogger with content-safe templates. None class guard throws ArgumentException.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetryServiceCollectionExtensions.cs` — AddConversationProjectionTelemetry() registers singleton.
- [x] `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadService.cs` — Added optional IConversationProjectionTelemetry? constructor param; EmitFreshnessTelemetryAndReturn helper emits freshness + rebuild signals for all non-Forbidden result paths (Unavailable, Rebuilding, final result).
- [x] `src/Hexalith.Conversations.Server/Publication/ConversationPublicationService.cs` — New non-static wrapper around ConversationPublicationMapper; emits RecordPublicationFailure on rejected results with safe short correlationId.

### Validation
- [x] Targeted tests (freshness classifier): `dotnet test ... --filter "FullyQualifiedName~ConversationProjectionFreshness"` — 9 passed.
- [x] Targeted tests (publication failure classifier): `dotnet test ... --filter "FullyQualifiedName~ConversationPublicationFailure"` — 4 passed.
- [x] Targeted tests (telemetry): `dotnet test ... --filter "FullyQualifiedName~ConversationProjectionTelemetry"` — 11 passed.
- [x] Full Server suite: `dotnet test tests/Hexalith.Conversations.Server.Tests/...` — 477 passed (453 baseline + 24 new).
- [x] Full solution: `dotnet test Hexalith.Conversations.slnx` — 1328 tests, 0 failures (Client 23, Conformance 155, Integration 8, Core 153, Server 477, Contracts 512).

### Coverage
- AC1: ProjectionTrustState → ConversationProjectionFreshnessClass (all 6 states mapped); ProjectionFreshnessReasonCode → ConversationProjectionLagClass (all 12 reason codes mapped); signals emitted at all non-Forbidden result paths in ConversationProjectionReadService; no TenantId, ConversationId, or free-text dimensions.
- AC2: ConversationErrorCode → ConversationPublicationFailureClass (all 16 codes covered with fallback TransientFailure); ConversationPublicationService wrapper emits RecordPublicationFailure on rejected results using safe generated correlationId only.
- AC3: Tests verify counter dimensions (freshness_class, lag_class, rebuild_class, failure_class) are bounded lowercase enum names; log messages contain only bounded fields; None class guards throw ArgumentException; DI registration verified.

## Story 6.1 Observe Command Rejections and Tenant Isolation Denials Safely

### Generated Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationCommandRejectionClassifierTest.cs` — 15 [Fact] tests: ClassifyErrorCode_TenantBindingMissing_ReturnsTenantBinding, ClassifyErrorCode_TenantIsolationViolation_ReturnsTenantIsolation, ClassifyErrorCode_TenantProjectionStale_ReturnsTenantProjectionUnavailable, ClassifyErrorCode_CommandValidationFailed_ReturnsValidation, ClassifyErrorCode_SchemaVersionUnsupported_ReturnsValidation, ClassifyErrorCode_IdempotencyConflict_ReturnsIdempotency, ClassifyErrorCode_AuditSinkUnavailable_ReturnsAuditUnavailable, ClassifyDenialReason_MissingTenant_ReturnsMissingContext, ClassifyDenialReason_MalformedTenant_ReturnsMissingContext, ClassifyDenialReason_UnknownTenant_ReturnsUnknownOrDisabled, ClassifyDenialReason_TenantDisabled_ReturnsUnknownOrDisabled, ClassifyDenialReason_InsufficientRole_ReturnsInsufficientAccess, ClassifyDenialReason_TenantAccessUnavailable_ReturnsProjectionUnavailable, ClassifyDenialReason_TenantAccessStale_ReturnsProjectionUnavailable, ClassifyDenialReason_TenantMismatch_ReturnsContextMismatch.
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationRejectionTelemetryTest.cs` — 10 [Fact] tests: RecordCommandRejection_EmitsCounterWithBoundedDimensions_NoConversationIdDimension, RecordCommandRejection_LogMessageContainsOnlyBoundedFields_NoTenantOrPartyIds, RecordTenantDenial_EmitsCounterWithBoundedDimensions_NoTargetTenantValue, RecordTenantDenial_LogMessageContainsOnlyBoundedFields_NoCrosstenantData, RecordPrivilegedAccessAttempt_EmitsCounterWithBoundedDimensions, RecordCommandRejection_NullOrEmptyCorrelationId_ThrowsArgumentException, RecordCommandRejection_NoneClass_ThrowsArgumentException, RecordTenantDenial_NoneClass_ThrowsArgumentException, RecordPrivilegedAccessAttempt_NoneClass_ThrowsArgumentException, AddConversationRejectionTelemetry_RegistersServiceCorrectly. (review auto-fix: renamed DoesNotEmit→ThrowsArgumentException; added 2 None-class guard tests)

### Implementation
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClass.cs` — New enum (9 values: None, Validation, TenantBinding, TenantIsolation, TenantProjectionUnavailable, Idempotency, AuditUnavailable, PolicyRejection, Infrastructure).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationTenantDenialClass.cs` — New enum (6 values: None, MissingContext, UnknownOrDisabled, InsufficientAccess, ProjectionUnavailable, ContextMismatch).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationPrivilegedAccessClass.cs` — New enum (3 values: None, AuthorizedPrivilegedOperation, UnauthorizedPrivilegedAttempt).
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClassifier.cs` — Static helper mapping ConversationErrorCode → ConversationCommandRejectionClass and ConversationTenantAccessDenialReason → ConversationTenantDenialClass.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/IConversationRejectionTelemetry.cs` — Interface with RecordCommandRejection, RecordTenantDenial, RecordPrivilegedAccessAttempt.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs` — Implementation using IMeterFactory (counters: conversations.command.rejections, conversations.tenant.denials, conversations.privileged.access) and ILogger with content-safe templates.
- [x] `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetryServiceCollectionExtensions.cs` — AddConversationRejectionTelemetry() registers singleton.
- [x] `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs` — Added optional IConversationRejectionTelemetry? and correlationId? parameters to RunAsync; emits RecordTenantDenial + RecordCommandRejection(TenantBinding) on denial when supplied.
- [x] `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs` — Added optional IConversationRejectionTelemetry? constructor param; emits RecordCommandRejection(Idempotency) on Conflict/Unknown decisions.
- [x] `src/Hexalith.Conversations.Server/CommandHandlers/SetConversationRetentionPolicyCommandHandler.cs` — Added optional IConversationRejectionTelemetry?; wired to guard and AuditUnavailable rejection path.
- [x] `src/Hexalith.Conversations.Server/CommandHandlers/RedactMessageContentCommandHandler.cs` — Added optional IConversationRejectionTelemetry?; wired to guard and AuditUnavailable rejection path.
- [x] `src/Hexalith.Conversations.Server/CommandHandlers/MarkConversationContentSensitiveCommandHandler.cs` — Added optional IConversationRejectionTelemetry?; wired to guard and AuditUnavailable rejection path.

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Server.Tests/... --filter "FullyQualifiedName~ConversationCommandRejection|FullyQualifiedName~ConversationRejection"` — 23 passed.
- [x] Full Server suite: `dotnet test tests/Hexalith.Conversations.Server.Tests/...` — 451 passed (428 baseline + 23 new).
- [x] Full solution: `dotnet test Hexalith.Conversations.slnx` — 1302 tests, 0 failures (Client 23, Conformance 155, Integration 8, Core 153, Server 451, Contracts 512).
- [x] Post-review (auto-fix): Full solution — 1304 tests, 0 failures (Server 453, +2 None-class guard tests added).

### Coverage
- AC1: All 16 ConversationErrorCode values classified into bounded ConversationCommandRejectionClass; signals emit only closed-vocabulary enum names as dimension values; no TenantId, PartyId, ConversationId, or free-text dimensions.
- AC2: All 16 ConversationTenantAccessDenialReason values mapped to 5-value ConversationTenantDenialClass; tenant denials fire in all governance command handler paths via ConversationTenantAccessGuard.
- AC3: Tests verify bounded label dimensions, no cross-tenant data, None class throws ArgumentException, and DI registration works correctly. Content-safety enforced by MeterListener-based counter capture + CapturingLogger log message assertions.

## Story 5.11 Separate Module-Level Evidence from Platform Controls

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/PlatformEvidenceSeparationConformanceFixtures.cs` - `PlatformEvidenceSeparationScenarioData` sealed record and `PlatformEvidenceSeparationConformanceSeedData` static class with 10 deterministic synthetic scenario records (8 ready, 1 blocked, 1 unknown — all conformant classification). All 10 scenario tokens verified safe against full 31-term UnsafeTerms blocklist. No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuite.cs` - Non-test suite runner following ContractValidationConformanceSuite pattern. SuiteId = `"platform-evidence-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 10 checks use `ConformanceCheck.GovernancePrecondition`, RequirementMappings = `["FR94"]`, PreconditionMappings = `["platform-evidence-separation-precondition"]`, ReleaseGateMappings = `["platform-evidence"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseGovernancePreconditionCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR94RequirementAndPlatformEvidenceGateMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (1 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (1 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/PlatformEvidenceSeparationConformanceFixtures.cs` - New fixture file with 10 scenarios: conversations-controls-documented (ready), eventlog-controls-inherited (ready), access-management-inherited (ready), parties-registry-inherited (ready), ui-framework-inherited (ready), infra-runtime-inherited (ready), missing-inherited-evidence-hidden (unknown/AggregateNotFound), incompatible-inherited-evidence-blocked (blocked/SchemaVersionUnsupported), approver-view-summarizes-controls (ready), approver-view-content-safe (ready).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 10-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 11th Story 5.11 entry (testId=story-5-11-platform-evidence-separation, requirementId=FR94, carryForwardCommitmentRef=null, releaseGateId=null (platform-evidence NOT in ReleaseGateId closed vocabulary), evidenceArtifactHandle=platform-evidence-conformance-suite-result, releaseDecisionStatus=pass).

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~PlatformEvidence"` - 15 passed (15 new).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 155 passed (140 baseline + 15 new).
- [x] `dotnet test Hexalith.Conversations.slnx` - 1279 tests, 0 failures (Client 23, Conformance 155, Integration 8, Core 153, Server 428, Contracts 512).

### Coverage
- AC1: suite covers all 10 required platform evidence separation surfaces; conversations-owned controls are distinguished from inherited controls with source, version reference, and scope limitation.
- AC2: missing-inherited-evidence-hidden and incompatible-inherited-evidence-blocked scenarios prove that absent or incompatible inherited evidence is disclosed explicitly rather than silently omitted.
- AC3: approver-view-summarizes-controls and approver-view-content-safe scenarios prove non-developer approver views are content-safe and summarize all required fields.
- Two-Level Evidence rule honored: carryForwardCommitmentRef=null (platform boundary documentation spans multiple prior stories); Story 5.11 adds release-gating coverage under "platform-evidence" mapping without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.10 Validate Commands, Queries, Events, Errors, and Version Discovery

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/ContractValidationConformanceFixtures.cs` - `ContractValidationScenarioData` sealed record and `ContractValidationConformanceSeedData` static class with 10 deterministic synthetic scenario records (8 ready, 1 blocked, 1 unknown — all conformant classification). Scenario 4 token renamed from `"error-envelope-shape"` to `"typed-error-shape"` because `"envelope"` is in the UnsafeTerms blocklist. No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`. Content-safe messages follow the same rules as Stories 5.5–5.9.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs` - Non-test suite runner following ProviderPortabilityConformanceSuite pattern. SuiteId = `"contract-validation-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 10 checks use `ConformanceCheck.CompatibilityDiscovery`, RequirementMappings = `["FR92"]`, ReleaseGateMappings = `["contract-compatibility"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseCompatibilityDiscoveryCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR92RequirementAndContractCompatibilityGateMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (1 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (1 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/ContractValidationConformanceFixtures.cs` - New fixture file with 10 scenarios: command-contract-shape (ready), query-contract-shape (ready), event-publication-shape (ready), typed-error-shape (ready), version-discovery-shape (ready), core-fixture-happy-path (ready), core-fixture-blocked-schema (blocked/SchemaVersionUnsupported), core-fixture-probe-hidden (unknown/AggregateNotFound), redaction-consumer-contract (ready), conformance-invariant-proof (ready).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 10-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 10th Story 5.10 entry (testId=story-5-10-contract-validation-conformance, requirementId=FR92, carryForwardCommitmentRef=story-4-5-adopter-conformance-suite, releaseGateId=contract-compatibility (IS in ReleaseGateId closed vocabulary), evidenceArtifactHandle=contract-validation-conformance-suite-result, releaseDecisionStatus=pass).

### Debug Log
- Bug fix: Scenario 4 token `"error-envelope-shape"` and SafeMessage `"Typed error envelope..."` both blocked by `"envelope"` in UnsafeTerms. Renamed token to `"typed-error-shape"` and SafeMessage to `"Typed error contract is content-safe..."`. Dev Notes listed only `"tenant-"`, `"provider-session"`, and `"stream"` as blocked, but the actual UnsafeTerms list in ConversationError.cs includes `"envelope"` and many more terms.

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~ContractValidation"` - 15 passed (15 new).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 140 passed (125 baseline + 15 new).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded (implicit in test run), 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - 1264 tests, 0 failures (Client 23, Conformance 140, Integration 8, Core 153, Server 428, Contracts 512).

### Coverage
- AC1: suite covers all 10 required contract validation surfaces; any non-conformant result is an automatic release gate flag under the contract-compatibility mapping.
- AC2: consumer-driven contract tests (redaction-consumer-contract scenario) prove stability for Stories 2.4 and 4.2; manifest entry identifies covered scenarios, pass criteria, FR92 requirement, carry-forward commitment to Story 4.5, evidence artifact handle, and content-safe diagnostics.
- AC3: adopter-style CORE fixture scenarios (core-fixture-happy-path, core-fixture-blocked-schema, core-fixture-probe-hidden) prove realistic integration behavior with synthetic tenant-safe fixture data.
- AC4: conformance-invariant-proof scenario validates project conformance invariants have traceable automated evidence.
- AC5: contract validation failure reporting is content-safe (all messages pass UnsafeTerms check) and traceable (correlation IDs, requirement mappings, release-gate mappings).
- Two-Level Evidence rule honored: `carryForwardCommitmentRef` links to Story 4.5 production proof; Story 5.10 adds release-gating coverage under `contract-compatibility` without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.9 Prove Event Schema Evolution

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/EventSchemaEvolutionConformanceFixtures.cs` - `EventSchemaEvolutionScenarioData` sealed record and `EventSchemaEvolutionConformanceSeedData` static class with 10 deterministic synthetic scenario records (7 ready, 2 blocked, 1 unknown — all conformant classification). No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`. Content-safe messages follow the same rules as Stories 5.5–5.8.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuite.cs` - Non-test suite runner following ProviderPortabilityConformanceSuite pattern. SuiteId = `"schema-evolution-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 10 checks use `ConformanceCheck.EventPublication`, RequirementMappings = `["FR91"]`, ReleaseGateMappings = `["unsupported-schema-rejection"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseEventPublicationCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR91RequirementAndSchemaEvolutionGateMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (2 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (1 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/EventSchemaEvolutionConformanceFixtures.cs` - New fixture file with 10 scenarios: schema-v1-replay (ready), additive-field-replay (ready), version-metadata-present (ready), mixed-version-history-replay (ready), projection-rebuild-mixed-versions (ready), upcaster-boundary-deterministic (ready), diagnostics-content-safety (ready), unsupported-version-blocked (blocked/SchemaVersionUnsupported), unsupported-version-not-skipped (blocked/SchemaVersionUnsupported), version-schema-probe-hidden (unknown/AggregateNotFound).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 10-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 9th Story 5.9 entry (testId=story-5-9-schema-evolution-conformance, requirementId=FR91, carryForwardCommitmentRef=story-1-11-schema-evolution-proof, releaseGateId=unsupported-schema-rejection (IS in ReleaseGateId closed vocabulary), evidenceArtifactHandle=schema-evolution-conformance-suite-result, releaseDecisionStatus=pass).

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~SchemaEvolution"` - 15 passed (15 new).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 125 passed (110 baseline + 15 new).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded (implicit in test run), 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - 1249 tests, 0 failures (Client 23, Conformance 125, Integration 8, Core 153, Server 428, Contracts 512).

### Coverage
- AC1: suite covers all 10 required event schema evolution surfaces; any non-conformant result is an automatic release gate flag under the unsupported-schema-rejection mapping.
- AC2: manifest entry identifies covered scenarios, pass criteria, FR91 requirement, carry-forward commitment to Story 1.11, evidence artifact handle, and content-safe diagnostics without exposing protected identifiers.
- AC3: 15 automated tests provide minimum evidence; missing required evidence would block gate closure per AC3 requirements.
- Two-Level Evidence rule honored: `carryForwardCommitmentRef` links to Story 1.11 production proof; Story 5.9 adds release-gating coverage under `unsupported-schema-rejection` without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.8 Prove Provider Portability

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs` - `ProviderPortabilityScenarioData` sealed record and `ProviderPortabilityConformanceSeedData` static class with 10 deterministic synthetic scenario records (7 ready, 2 blocked, 1 unknown — all conformant classification). No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`. Content-safe messages: "EventStore", "store", and "provider payload" terms replaced with safe equivalents.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs` - Non-test suite runner following RedactionConformanceSuite pattern. SuiteId = `"portability-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 10 checks use `ConformanceCheck.EventPublication`, RequirementMappings = `["FR90"]`, ReleaseGateMappings = `["provider-portability"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseEventPublicationCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR90RequirementAndPortabilityGateMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (2 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (1 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/ProviderPortabilityConformanceFixtures.cs` - New fixture file with 10 scenarios: provider-id-stripped (ready), provider-id-changed (ready), session-expiry-recoverable (ready), provider-id-migrated (ready), projection-rebuild-without-provider (ready), replay-determinism-without-provider (ready), provider-only-identity-blocked (blocked/ProviderOnlyIdentityForbidden), session-authority-blocked (blocked/ProviderOnlyIdentityForbidden), cross-provider-correlation-hidden (unknown/AggregateNotFound), diagnostics-content-safety (ready).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 10-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 8th Story 5.8 entry (testId=story-5-8-portability-conformance, requirementId=FR90, carryForwardCommitmentRef=story-1-11-replay-portability-proof, releaseGateId=provider-portability (IS in ReleaseGateId closed vocabulary), evidenceArtifactHandle=portability-conformance-suite-result, releaseDecisionStatus=pass).

### Debug Log
- Bug fix: 6 SafeMessage values in Dev Notes contained blocked UnsafeTerms ("EventStore", "store", "provider payload"). Replaced with content-safe equivalents: "EventStore" → "the event log" or "the event history source of truth"; "stored as" → "kept as"; "provider payload" → "infrastructure terms".

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~Portability"` - 16 passed (15 new + 1 pre-existing).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 110 passed (95 baseline + 15 new).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - 1234 tests, 0 failures (Client 23, Conformance 110, Integration 8, Core 153, Server 428, Contracts 512).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: suite covers all 10 required provider portability surfaces; any non-conformant result is an automatic release gate flag under the provider-portability mapping.
- AC2: invariants (tenant isolation, idempotency, ordering tolerance, auditability, replay determinism) remain stable across all 10 portability scenarios; blocked and unknown outcomes prove fail-closed and side-channel-safe behavior.
- AC3: manifest entry identifies covered scenarios, pass criteria, FR90 requirement, carry-forward commitment to Story 1.11, evidence artifact handle, and content-safe diagnostics without exposing protected identifiers.
- AC4: 15 automated tests provide minimum evidence; missing required evidence would block gate closure per AC4 requirements.
- Two-Level Evidence rule honored: `carryForwardCommitmentRef` links to Story 1.11 production proof; Story 5.8 adds release-gating coverage without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.7 Verify Redaction Replay Conformance

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/RedactionConformanceFixtures.cs` - `RedactionReplayScenarioData` sealed record and `RedactionConformanceSeedData` static class with 10 deterministic synthetic scenario records (7 ready, 2 blocked, 1 unknown — all conformant classification). No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/RedactionConformanceSuite.cs` - Non-test suite runner following IdempotencyConformanceSuite pattern. SuiteId = `"redaction-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 10 checks use `ConformanceCheck.GovernancePrecondition`, RequirementMappings = `["FR89"]`, ReleaseGateMappings = `["redaction-non-leakage"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/RedactionConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly10Checks, AllChecksShouldUseGovernancePreconditionCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR89RequirementAndRedactionGateMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (2 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (1 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/RedactionConformanceFixtures.cs` - New fixture file with 10 scenarios: projection-replay-content-safe (ready), temporal-view-replay-hidden (ready), rebuild-replay-content-safe (ready), audit-citation-without-exposure (ready), log-trace-output-content-safe (ready), error-response-content-safe (ready), stale-projection-blocked (blocked/TenantProjectionStale), audit-sink-blocked (blocked/AuditSinkUnavailable), cross-scope-replay-hidden (unknown/AggregateNotFound), diagnostics-content-safety (ready).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/RedactionConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 10-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 7th Story 5.7 entry (testId=story-5-7-redaction-conformance, requirementId=FR89, carryForwardCommitmentRef=story-2-4-redaction-replay-non-disclosure, releaseGateId=redaction-non-leakage (IS in ReleaseGateId closed vocabulary), evidenceArtifactHandle=redaction-conformance-suite-result, releaseDecisionStatus=pass).

### Debug Log
- Bug fix: 3 SafeMessage values in Dev Notes contained "redacted content" (forbidden by UnsafeTerms). Replaced with content-safe equivalents.
- Bug fix: pre-written test used `ConversationConformanceCoreFixtures` (static class) as variable type; corrected to `ConversationConformanceCoreSeedData` (return type of `Create()`).

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~Redaction"` - 15 passed (15 new).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 95 passed (80 baseline + 15 new).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - 1219 tests, 0 failures (Client 23, Conformance 95, Integration 8, Core 153, Server 428, Contracts 512).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: suite covers all 10 required redaction replay disclosure surfaces; any non-conformant result is an automatic release gate flag under the redaction-non-leakage mapping.
- AC2: manifest entry identifies covered scenarios, pass criteria, FR89 requirement, carry-forward commitment to Story 2.4, evidence artifact handle, and content-safe diagnostics without exposing protected identifiers.
- Two-Level Evidence rule honored: `carryForwardCommitmentRef` links to Story 2.4 production proof; Story 5.7 adds release-gating coverage without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.6 Verify Idempotent Command Conformance

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/IdempotencyConformanceFixtures.cs` - `IdempotencyScenarioData` sealed record and `IdempotencyConformanceSeedData` static class with 8 deterministic synthetic scenario records (5 ready, 2 blocked, 1 unknown — all conformant classification). No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/IdempotencyConformanceSuite.cs` - Non-test suite runner following TenantIsolationConformanceSuite pattern. SuiteId = `"idempotency-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 8 checks use `ConformanceCheck.Idempotency`, RequirementMappings = `["FR88"]`, ReleaseGateMappings = `["idempotency"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/IdempotencyConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly8Checks, AllChecksShouldUseIdempotencyCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR88RequirementAndIdempotencyMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (2 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (1 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/IdempotencyConformanceFixtures.cs` - New fixture file with 8 scenarios: duplicate-equivalent-command (ready), duplicate-nonequivalent-command (blocked/IdempotencyConflict), reordered-delivery (ready), unknown-outcome-retry (ready), replayed-delivery (ready), mismatched-key-reuse (unknown/AggregateNotFound), missing-idempotency-key (blocked/IdempotencyKeyMissing), diagnostics-content-safety (ready).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/IdempotencyConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 8-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 6th Story 5.6 entry (testId=story-5-6-idempotency-conformance, requirementId=FR88, carryForwardCommitmentRef=story-1-6-idempotency-stable-outcomes, releaseGateId=null (idempotency not in ReleaseGateId closed vocabulary), evidenceArtifactHandle=idempotency-conformance-suite-result, releaseDecisionStatus=pass).

### Debug Log
- manifest releaseGateId `"idempotency"` rejected by closed-vocabulary JSON converter (7 gate IDs; idempotency is not one). Dev Notes stated field is "not schema-validated" — incorrect; tests DO deserialize via converter. Fixed: set `releaseGateId` to `null` matching stories 5.1–5.4 pattern.

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~Idempotency"` - 16 passed (15 new + 1 existing from AdopterConformanceSuite).
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 80 passed (65 baseline + 15 new).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - 1204 tests, 0 failures (Client 23, Conformance 80, Integration 8, Core 153, Server 428, Contracts 512).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: suite covers all 8 required idempotency scenarios; any non-conformant result is an automatic release gate flag under the idempotency mapping.
- AC2: manifest entry identifies covered scenarios, pass criteria, FR88 requirement, carry-forward commitment to Story 1.6, evidence artifact handle, and content-safe diagnostics without exposing protected identifiers.
- Two-Level Evidence rule honored: `carryForwardCommitmentRef` links to Story 1.6 production proof; Story 5.6 adds release-gating coverage without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.5 Verify Tenant Isolation Conformance

### Generated Tests
- [x] `src/Hexalith.Conversations.Testing/Fixtures/TenantIsolationConformanceFixtures.cs` - `TenantIsolationScenarioData` sealed record and `TenantIsolationConformanceSeedData` static class with 12 deterministic synthetic scenario records (2 ready, 7 blocked, 3 unknown — all conformant classification). No real tenant IDs, Party IDs, or conversation IDs. Marked with `SyntheticDataMarker = "synthetic-conformance-data"`.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuite.cs` - Non-test suite runner following AdopterConformanceSuite pattern. SuiteId = `"isolation-conformance-suite"`, RunnerId = `"local-ci-runner"`. All 12 checks use `ConformanceCheck.TenantBinding`, RequirementMappings = `["FR87"]`, ReleaseGateMappings = `["tenant-isolation"]`. Read-only: no aggregate command dispatch, no event appends.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuiteTest.cs` - 15 [Fact] tests: RunResultShouldHaveExactly12Checks, AllChecksShouldUseTenantBindingCheckId, EachScenarioShouldProduceExpectedConformanceOutcome, EachScenarioCheckShouldBeClassifiedAsConformant, AllChecksShouldCarryFR87RequirementAndTenantIsolationGateMappings (incl. PreconditionMappings.ShouldNotBeEmpty), ReadyScenariosShouldHaveNullTypedError, BlockedScenariosShouldHaveNonNullTypedError (7 blocked), UnknownScenariosShouldCarryAggregateNotFoundTypedError (3 unknown, HideOrRefresh, !IsRetryable), AllConformantScenariosProduceOverallReadyOutcome, SuiteIdAndRunnerIdShouldMatchSpecifiedValues, RunResultShouldNotLeakPoisonSentinelsOrForbiddenFragments, RunResultShouldSerializeToStableCamelCaseJsonAndRoundTrip (incl. PreconditionMappings round-trip), NullScenariosListShouldThrow, EmptyScenariosListShouldThrow, NullCorrelationIdShouldThrow.

### Implementation
- [x] `src/Hexalith.Conversations.Testing/Fixtures/TenantIsolationConformanceFixtures.cs` - New fixture file with 12 scenarios: authorized-access (ready), hidden-id-probe (unknown/AggregateNotFound), stale-projection (blocked/TenantProjectionStale), unavailable-projection (blocked/TenantProjectionStale), disabled-tenant (blocked/TenantIsolationViolation), deleted-tenant (blocked/TenantIsolationViolation), mixed-scope-rebuild (blocked/TenantIsolationViolation), poisoned-projection-event (unknown/AggregateNotFound), malformed-binding (blocked/TenantBindingMissing), query-enumeration (unknown/AggregateNotFound), diagnostics-content-safety (ready), admin-tool-access (blocked/TenantIsolationViolation).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuite.cs` - Suite runner with explicit parameter signature. Aggregation: anyFailure → blocked; anyDegraded → degraded; else → ready. All-conformant 12-scenario fixture produces overallOutcome = ready.
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with 5th Story 5.5 entry (testId=story-5-5-isolation-conformance, requirementId=FR87, carryForwardCommitmentRef=story-1-5-binding-fail-closed, releaseGateId=tenant-isolation, evidenceArtifactHandle=isolation-conformance-suite-result, releaseDecisionStatus=pass).
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` - Line 429: `ShouldBe(4)` → `ShouldBeGreaterThanOrEqualTo(4)` to accommodate 5th manifest entry.

### Validation
- [x] Targeted tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --filter "FullyQualifiedName~TenantIsolation"` - 15 passed.
- [x] Full conformance suite: `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 65 passed (50 baseline + 15 new).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - 1189 tests, 0 failures (Client 23, Conformance 65, Integration 8, Core 153, Server 428, Contracts 512).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: suite covers all 12 required tenant isolation adversarial and positive scenarios; any non-conformant result is an automatic release blocker under NFR62 (tenant-isolation gate).
- AC2: manifest entry identifies covered scenarios, pass criteria, blocking failures, waiver status, environment metadata, and content-safe diagnostics without exposing protected identifiers.
- Two-Level Evidence rule honored: `carryForwardCommitmentRef` links to Story 1.5 production proof; Story 5.5 adds release-gating coverage without re-proving production behavior.
- No new ConformanceCheck values, ConformanceOutcome values, ReleaseGateId values, public error codes, src/ library projects, or production runtime changes.

## Story 5.4 Support Named Waivers for Release-Gate Exceptions

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseWaiverContractTest.cs` - 43 contract tests covering `WaiverLifecycleStatus` closed-vocabulary completeness (4 values: active, expired, rejected, superseded), JSON rejection of synonyms (valid, invalid, cancelled, done, pending) via Parse and all 4 spec synonyms (valid, invalid, cancelled, done) via JsonSerializer.Deserialize, `Parse` round-trips for all 4 values, `IsActive` (true only for active), `IsStale` (true for expired and superseded, false for active and rejected), `ReleaseWaiverV1` construction-time validation (null AffectedStoryIds, null/empty WaiverId, null/empty Owner, null Approver accepted, null/empty AffectedRequirementId, null AffectedGateId accepted, empty/null-element AffectedStoryIds, empty Risk/CompensatingControl/BuyerImpact, non-UTC ExpiryDateUtc, null BuyerAcceptanceStatus accepted, null EvidenceLinks throws ArgumentNullException, empty EvidenceLinks accepted, non-UTC ReviewDateUtc, null LifecycleStatus throws ArgumentNullException, non-UTC CreatedAtUtc), `ReleaseWaiverValidator.ValidateWaiver` error tokens (blocker-requires-approver, expired-waiver, stale-review-date, buyer-facing-missing-acceptance), stable camelCase web JSON, round-trip, additive tolerance, fixture file existence/deserialization/zero-errors/content-safety, manifest fixture has 4 entries. Review auto-fixed: JSON synonym test extended to all 4 spec synonyms; null AffectedStoryIds test added (+1 test).
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseWaiverValidationTest.cs` - 7 validation tests covering fixture waiver passes `ValidateWaiver` with zero errors (evaluatedAt=2026-05-23), blocker with null approver returns blocker-requires-approver, past ExpiryDateUtc returns expired-waiver, past ReviewDateUtc returns stale-review-date, fixture content-safety scan, stable camelCase JSON and deterministic round-trip, `WaiverLifecycleStatus.All` returns exactly 4 values.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - registered `WaiverLifecycleStatus` (4 values), and two `ReleaseWaiverV1` samples (non-blocker and blocker-with-gate) to extend serialization, forbidden-surface, and content-safety coverage automatically.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs` - Defines `WaiverLifecycleStatus` (4 values: active, expired, rejected, superseded; `IsActive` and `IsStale` computed properties; sealed-record closed-vocabulary pattern), `ReleaseWaiverV1` (sealed record, 16 validated fields using `RequiredSafeToken`/`RequiredSafeText`/`RequiredUtcTimestamp`/`OptionalSafeToken`; `AffectedStoryIds` uses mapping-token validation without disclosure blocklist to allow story IDs containing "exception"; `EvidenceLinks` null-guarded), and `ReleaseWaiverValidator` (static class with `ValidateWaiver(waiver, evaluatedAt)` returning 4 content-safe error tokens).
- [x] `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` - Added `WaiverLifecycleStatusJsonConverter` following the `ConversationStringValueJsonConverter<T>` pattern exactly like `ReleaseGateStatusJsonConverter`.
- [x] `docs/release-evidence/waiver.schema.json` - Structured human-navigable JSON specification with field definitions (16 fields with type/required/validation/description), validation rules (4 rules matching ValidateWaiver error tokens), and example record; navigable by non-developer release approvers (NFR68).
- [x] `docs/release-evidence/release-waiver-v1-fixture.json` - Synthetic deterministic fixture waiver (waiverId=waiver-story-5-4-named-waiver-process, owner=release-engineer, approver=release-approver, affectedRequirementId=FR85, affectedGateId=null, isBlocker=false, lifecycleStatus=active, all dates future relative to 2026-05-23, content-safe).
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Extended with Story 5.4 entry (testId=story-5-4-named-waiver-process, requirementId=FR85, evidenceArtifactHandle=release-waiver-v1-fixture, releaseDecisionStatus=pass); now has 4 entries.

### Validation
- [x] Targeted contract tests: `dotnet test tests/Hexalith.Conversations.Contracts.Tests --filter "FullyQualifiedName~ReleaseWaiver|FullyQualifiedName~WaiverLifecycle"` - 43 passed (42 original + 1 review-added).
- [x] Conformance validation tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests` - 50 passed (43 existing + 7 new Story 5.4 tests).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 50, Integration 8, Core 153, Server 428, Contracts 512 (1174 total, 0 failures).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: `ReleaseWaiverV1` records all required governance fields (owner, approver, affected requirement/gate/stories, isBlocker, risk, compensatingControl, expiryDateUtc, buyerImpact, buyerAcceptanceStatus, evidenceLinks, reviewDateUtc, lifecycleStatus, createdAtUtc); automatic release blockers cannot be waived without explicit named approval (`blocker-requires-approver` error token).
- AC2: `WaiverLifecycleStatus` (4 values: active/expired/rejected/superseded) distinguishes lifecycle states in release evidence; `IsStale` computed property marks expired and superseded waivers as findings; `ValidateWaiver` flags expired waivers and stale review dates regardless of lifecycle status.
- AC3: 43 contract tests and 7 conformance tests prove governance traceability (blocker enforcement, expiry detection, review staleness, buyer-facing acceptance), release decision clarity (all validator error tokens exercised), and content-safe evidence output (fixture passes content-safety scan).

## Story 5.3 Maintain Versioned Conformance Manifest with Traceability

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceManifestContractTest.cs` - 41 contract tests covering `ConformanceManifestLifecycleStage` closed-vocabulary completeness (6 NFR1 values), JSON rejection of synonyms (test, testing, design, ops, review, load-test), `Parse` round-trips, `ConformanceManifestRowV1` construction-time validation (null/empty test ID, empty test name/requirement/pass-criteria/evidence/owner/environment, null lifecycle stage, non-UTC timestamp, unsafe free-text), null `ReleaseGateId` accepted, null `WaiverReference` accepted at construction, `ConformanceManifestChangeV1` construction-time validation (null change ID, empty summary, empty affected IDs, non-UTC timestamp, empty changed-by), `ConformanceManifestV1` construction-time validation (null schema version, empty manifest version/release reference, empty entries, null entry in list, null change-log), `ConformanceManifestValidator.ValidateManifest` returns errors for duplicate test IDs and waived-without-waiver, stable camelCase web JSON, round-trip, additive tolerance, fixture file existence/deserialization/zero-diagnostics/3-entry minimum/content-safety.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConformanceManifestValidationTest.cs` - 6 validation tests covering fixture passes `ValidateManifest` with zero errors, duplicate test ID returns `duplicate-test-id`, waived entry without waiver reference returns `missing-waiver-reference`, fixture entries pass content-safety scan, stable camelCase JSON and deterministic round-trip, `ConformanceManifestLifecycleStage.All` returns exactly 6 stages matching NFR1.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - registered `ConformanceManifestLifecycleStage`, `ConformanceManifestRowV1`, `ConformanceManifestChangeV1`, and `ConformanceManifestV1` samples to extend serialization, forbidden-surface, and content-safety coverage automatically.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ConformanceManifestV1.cs` - Defines `ConformanceManifestLifecycleStage` (6 NFR1 stages, sealed-record closed-vocabulary pattern), `ConformanceManifestRowV1` (sealed record, 14 validated fields), `ConformanceManifestChangeV1` (sealed record, version history entry), `ConformanceManifestV1` (sealed record, versioned manifest), and `ConformanceManifestValidator` (static class with `ValidateManifest` returning content-safe error tokens).
- [x] `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` - Added `ConformanceManifestLifecycleStageJsonConverter` following the `ConversationStringValueJsonConverter<T>` pattern.
- [x] `docs/release-evidence/manifest.schema.json` - Structured human-navigable JSON specification with field definitions, validation rules, and an example row; navigable by non-developer release approvers (NFR68).
- [x] `docs/release-evidence/conformance-manifest-v1-fixture.json` - Synthetic deterministic fixture manifest with 3 entries (story-5-1, story-5-2, story-5-3), empty change-log, release-reference "local-test-release", manifest-version "v1-fixture".

### Validation
- [x] Targeted contract tests: `dotnet test tests/Hexalith.Conversations.Contracts.Tests --filter "FullyQualifiedName~ConformanceManifest|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"` - 57 passed.
- [x] Conformance validation tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests` - 43 passed (including 6 new Story 5.3 tests).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 43, Integration 8, Core 153, Server 428, Contracts 466 (1121 total).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: each manifest row maps to functional requirements, NFRs, carry-forward commitments, release-gate status, pass criteria, waiver status, measurement method, environment, and evidence artifact handle.
- AC2: `ConformanceManifestChangeV1` preserves version history with affected requirement IDs; `ValidateManifest` flags stale/orphan entries (duplicate test IDs).
- AC3: `ValidateManifest` returns content-safe typed error tokens for duplicate IDs, waived-without-reference; diagnostics are actionable and never expose protected identifiers.
- AC4: each row carries requirement ID, gate status, evidence artifact handle, owner, lifecycle stage, release decision status, and conditional waiver reference; validation rejects entries lacking traceability.

## Story 5.2 Generate Signed Release Conformance Artifact

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ReleaseConformanceArtifactContractTest.cs` - 31 contract tests covering `ReleaseGateStatus` closed-vocabulary completeness (4 values), JSON rejection of synonyms, `IsBlocking` property, `ReleaseGateId` closed-vocabulary completeness (7 gates), JSON rejection of unknown gate IDs, `ReleaseGateResultV1` construction-time validation (null gate/status, empty handle/requirement/summary, non-UTC timestamp), `ReleaseConformanceArtifactV1` construction-time validation (empty build hash, missing signer, null schema, empty/incomplete gate list), `ValidateArtifact` errors, `OverallStatus` computed matrix (all-pass/any-fail/some-waived/mixed), stable camelCase web JSON, round-trip, additive tolerance, fixture file existence/validity/gate-completeness/content-safety.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactGenerationTest.cs` - 10 generation tests covering builder produces valid artifact from CORE fixture, all 7 gates present, overall status deterministic, audit-integrity=pass (GovernancePrecondition Ready), tenant-isolation=unknown-accepted (TenantBinding Unknown outcome), provider-portability=unknown-accepted (no adopter mapping), all gate statuses in closed vocabulary, content-safety scan, null rejection, builder determinism.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ReleaseConformanceArtifactBuilder.cs` - `ReleaseConformanceArtifactBuilder` class with deterministic gate-to-check mapping (ready→pass, blocked+non-conformant→fail, else→unknown-accepted), 7-gate construction, injected `TimeProvider`, and fail-closed null validation.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - registered `ReleaseGateStatus`, `ReleaseGateId`, `ReleaseGateResultV1`, and `ReleaseConformanceArtifactV1` samples to extend serialization, forbidden-surface, and content-safety coverage automatically.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs` - Defines `ReleaseGateStatus` (pass, fail, waived, unknown-accepted with `IsBlocking` property), `ReleaseGateId` (7 gate IDs), and `ReleaseGateResultV1` sealed record with content-safe field validation.
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs` - Sealed record with all required evidence fields, computed `OverallStatus`, constructor-time gate completeness validation, and static `ValidateArtifact` returning typed errors.
- [x] `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` - Added `ReleaseGateStatusJsonConverter` and `ReleaseGateIdJsonConverter` following the `ConversationStringValueJsonConverter<T>` pattern.
- [x] `docs/release-evidence/release-conformance-artifact-v1-fixture.json` - Committed synthetic deterministic fixture file (schema v1, "test-runner" signer, "ci-build-test-fixture" build hash, 7 gate results, overall status unknown-accepted) generated and validated by the generation test.

### Validation
- [x] Targeted contract tests: `dotnet test tests/Hexalith.Conversations.Contracts.Tests --filter "FullyQualifiedName~ReleaseConformance|FullyQualifiedName~ReleaseGate"` - 31 passed.
- [x] Conformance generation tests: `dotnet test tests/Hexalith.Conversations.Conformance.Tests` - 37 passed (including 10 new Story 5.2 tests).
- [x] Full contracts suite: `dotnet test tests/Hexalith.Conversations.Contracts.Tests` - 425 passed (including serialization, forbidden-surface, content-safety scans over new types).
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 37, Integration 8, Core 153, Server 428, Contracts 425 (1074 total).
- [x] No validation step used nested submodule initialization.

### Coverage
- AC1: artifact captures build hash, schema/event versions, contract package versions, test environment identity, dataset scale, tool versions, timestamped evidence links, signer identity, and release manifest reference; machine-readable camelCase JSON, deterministic, content-safe.
- AC2: 7 gate IDs (tenant-isolation, audit-integrity, redaction-non-leakage, unsupported-schema-rejection, projection-rebuild-determinism, contract-compatibility, provider-portability) each classified as pass/fail/waived/unknown-accepted; overall-status computation is deterministic and non-forgeable.
- AC3: construction-time validation rejects unsafe/incomplete/unsigned artifacts; `ValidateArtifact` returns typed diagnostics; content-safety scan blocks forbidden fragments in all free-text fields.
- No Story 5.3 manifest, 5.4 named waivers, 5.5-5.9 domain suites, 5.10 aggregation, PKI signing, durable store, CLI, or new public error codes were added.

## Story 5.1 Publish Contract Compatibility and Deprecation Policy

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Documentation/ContractCompatibilityPolicyValidationTest.cs` - Added deterministic FR81 policy validation for policy publication/linkage, public compatibility surface coverage, stable release-evidence classification IDs, metadata alignment with `ConversationContractCompatibility.Current`, compatibility evaluation scenarios, safe diagnostics, HTTPS documentation pointers, and policy content-safety scanning.

### Implementation
- [x] `docs/release-evidence/contract-compatibility-policy.md` - Published the FR81 compatibility and deprecation policy covering additive changes, breaking changes, deprecation/minimum-version rules, unsupported-version behavior, persisted-event versus public-contract boundaries, and Story 5.1 evidence boundaries without creating signed artifacts, manifests, waiver approvals, or release-gate decisions.
- [x] `README.md`, `docs/integration-guide.md`, and `src/Hexalith.Conversations.Contracts/README.md` - Linked the policy from adopter-facing documentation without duplicating drift-prone metadata tables.

### Validation
- [x] Red phase: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ContractCompatibility|FullyQualifiedName~CompatibilityPolicy|FullyQualifiedName~SchemaVersionCompatibility|FullyQualifiedName~IntegrationGuide"` - failed as expected before the policy existed: 4 policy validation tests failed on missing `docs/release-evidence/contract-compatibility-policy.md`.
- [x] Targeted green phase: same command - 38 passed after publishing the policy, links, and validation test.
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 25, Integration 8, Core 153, Server 428, Contracts 395 (1032 total).
- [x] No validation step used or required nested submodule initialization or `git submodule update --init --recursive`.

### Coverage
- AC1: policy covers commands, projections, published events, typed errors, version discovery, contracts package, .NET client package, additive/breaking rules, deprecation, minimum v1 support, unsupported behavior, remediation, and the persisted-event/public-contract distinction.
- AC2: local release-evidence summary uses stable policy IDs and classifications `additive`, `breaking`, `deprecated`, `unsupported`, and `waiver-dependent` without creating Story 5.2-5.4 artifacts.
- AC3: policy checks prove FR81 traceability, metadata alignment with current contract/package versions, supported/deprecated/unsupported/invalid/additive scenarios, and content-safe diagnostics.
- No runtime tenant authorization, audit pairing, redaction replay, idempotency, projection freshness, event persistence, client transport behavior, public compatibility vocabulary, signed artifact, manifest, waiver, release-gate aggregation, CLI, DocFX site, durable store, worker, or UI surface was added.

## Story 4.7 Publish Developer Integration Guide and API Examples

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs` - Added deterministic documentation validation for the integration guide, root README link, shipped client/contract type names, embedded C# snippet identifiers, canonical error tables, compatibility metadata, conformance command/reference accuracy, HTTPS documentation pointers, and content-safety forbidden fragments.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideWorkflowExampleTest.cs` - Added compile-time adopter workflow coverage for the documented DI registration, create conversation, append message, timeline read with freshness, typed error retry branches, compatibility discovery, and CORE precondition surfaces.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - Added a project reference to `Hexalith.Conversations.Client` so documentation validation can prove client-surface examples reference shipped types and members.

### Implementation
- [x] `docs/integration-guide.md` - Published the adopter developer integration guide with responsibility boundaries, CORE behavior, fail-closed failure modes, compatibility discovery, CORE preconditions, conformance guidance, and validated .NET client/contract snippets for setup, create, append, read timeline, typed errors, idempotent retry, freshness, compatibility, and preconditions.
- [x] `README.md` - Linked the new Developer Integration Guide from the supported v1 contract package guidance.

### Validation
- [x] Red phase: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Documentation|FullyQualifiedName~IntegrationGuide|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ErrorCatalog"` - failed as expected before the guide existed: 4 documentation tests failed on missing `docs/integration-guide.md`.
- [x] Targeted green phase: same command - 28 passed after publishing the guide and README link.
- [x] QA automation follow-up: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Documentation|FullyQualifiedName~IntegrationGuide|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ErrorCatalog"` - 30 passed after adding compile-time workflow coverage.
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 25, Integration 8, Core 153, Server 428, Contracts 390 (1027 total).

### Coverage
- AC1: guide documents Conversations responsibility boundaries and CORE behavior using shipped contract/client/precondition/compatibility names, with canonical tables linked rather than copied.
- AC2: embedded C# snippets cover .NET client registration, create, append, timeline read, typed errors, idempotent retry, freshness, compatibility discovery, CORE preconditions, and conformance execution; docs validation asserts referenced types and members exist.
- AC2 follow-up: compile-time workflow coverage now exercises the same supported client/contract sequence against the shipped public surface so signature drift fails tests.
- AC3: failure-mode guidance stays content-safe, documents fail-closed behavior, hidden `aggregate_not_found`, stale projection, unsupported schema, audit handles where allowed, and remediation paths without policy internals or unsafe substrate detail.
- AC4: documentation checks validate README error tables against `ConversationErrorCatalog`, compatibility metadata against `ConversationContractCompatibility.Current`, conformance references, HTTPS pointers, and unsafe example text.
- No raw HTTP fallback examples, sample host, DocFX pipeline, production behavior, new public vocabulary, durable state, Epic 5 release evidence, or Epic 6 operator/buyer documentation was added.

## Story 4.6 Capture Caller Metadata for Attribution, Audit, and Composition

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/CallerMetadataContractsTest.cs` - Added construction-time validation (size caps, count caps, control-character rejection, content-unsafe rejection of tenant/Party/provider-payload/business-reference/EventStore/local-path/exception fragments), whitespace-only rejection, malformed extension data rejection, required schema version, stable camelCase web JSON shape, additive-JSON tolerance, and boundary-bag bounding coverage for `CallerMetadata.TryValidateMetadataBag`.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Registered a `CallerMetadata` sample in `AllContracts` and attached it to the create/append/update command samples so the new contract participates in serialization, forbidden-surface, and content-safety scans.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs` - Updated the representative `CreateConversationCommand` wire fixture for the additive `callerMetadata` member.
- [x] `tests/Hexalith.Conversations.Tests/Validation/CallerMetadataValidationTest.cs` - Added command-boundary coverage: valid caller metadata passes, absent caller metadata stays valid, token-like/sensitive values rejected at construction, oversized/tenant-spoofing/over-count `UpdateConversationMetadataCommand.Attributes` bag returns typed `command_validation_failed` rejections with bounded reason codes that never echo caller-supplied values, and a trust-inference assertion that caller metadata never alters the envelope tenant binding.
- [x] `tests/Hexalith.Conversations.Server.Tests/Publication/CallerMetadataPublicationTest.cs` - Added publication non-leak coverage (no caller-supplied client/composer/origin value appears in safe transport headers; correlation/causation remain the canonical provenance carrier) and a command-API trust-inference test proving caller metadata claiming a different tenant/elevated origin does not override the claims-derived tenant binding.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Identifiers/CallerMetadata.cs` - New bounded, content-safe, eagerly-validated provenance record modeled on `ProviderCorrelationMetadata`. Approved fields: `clientName`, `clientVersion`, `composerSource`, `origin`, `integrationContext`, and a bounded opaque `extensionData` string bag. Reuses the shared `ConversationError.EnsureContentSafe` guardrail; exposes `TryValidateBounds`/`TryValidateMetadataBag` non-throwing boundary helpers returning bounded reason codes.
- [x] `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`, `AppendMessageCommand.cs`, `UpdateConversationMetadataCommand.cs` - Added an additive optional `CallerMetadata? CallerMetadata = null` following the existing optional `ProviderCorrelationMetadata? ProviderCorrelation = null` precedent (no breaking change; no broad envelope change, so no ADR trigger).
- [x] `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs` - Extended the canonical envelope validator to bound caller metadata AFTER the shared envelope (tenant/schema/idempotency), reusing the idempotency-key reject/bound pattern. Closes the noted gap by also bounding the previously unbounded `UpdateConversationMetadataCommand.Attributes` bag with the same deterministic policy. Returns typed `ConversationRejectedDomainEvent(CommandValidationFailed, ...)` with bounded reason codes.
- [x] `src/Hexalith.Conversations.Contracts/README.md` - Added a compact adopter-facing caller-metadata table (approved fields, bounds, reject policy) and the provenance-only rule.

### Design Decisions (Dev Notes)
- Attachment approach: additive optional parameter on the three relevant command records, NOT a broad `ConversationCommandMetadata` envelope change (avoids the broad-public-envelope ADR trigger).
- Correlation/causation are not duplicated in caller metadata; they remain first-class on `ConversationCommandMetadata`/`ConversationEventMetadata`.
- Reject/truncate/omit policy (AC3): deterministic **reject** at construction and at the command boundary; no silent truncation, because a truncated content-unsafe value cannot guarantee a safe residual fragment.
- No new durable state: caller metadata is a command-boundary provenance bag bound by the validator; no new durable event field was added, and published provenance flows only through the existing safe correlation/causation transport headers (no ADR trigger).

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~CallerMetadata|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractValidation"` - 64 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~CallerMetadata|FullyQualifiedName~Validation"` - 22 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~CallerMetadata|FullyQualifiedName~Publication|FullyQualifiedName~CommandApi"` - 31 passed.
- [x] `dotnet build Hexalith.Conversations.slnx` - succeeded, 0 warnings, 0 errors.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 25, Integration 8, Core 150, Server 425, Contracts 372 (1003 total).

### Coverage
- AC1: only approved bounded caller-metadata fields are accepted; identity/secret/protected values are rejected by construction and at the command boundary.
- AC2: caller metadata stays provenance only and never becomes tenant truth, authorization, governance truth, or UI trust state; the command-API trust-inference test proves a spoofing caller cannot override the claims-derived tenant binding.
- AC3: malformed/oversized/unbounded/sensitive/unsupported metadata is rejected with typed bounded diagnostics that never echo caller-supplied values; publication non-leak test proves no caller value reaches transport headers.
- AC4: contract, domain-validation, trust-inference, and publication/no-leak tests prove safe validation, bounded telemetry, attribution usefulness, and no trust/authorization inference.
- No Story 4.7 guide/DocFX/raw HTTP examples, no Epic 5/6 work, no parallel correlation/error/attribution model, no new durable state, and no caller-metadata-as-authority were added.

## Story 4.5 Provide Adopter-Facing Conformance Tests and CORE Fixture

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Conformance/ConformanceContractsTest.cs` - Added closed-vocabulary coverage for the conformance check/outcome/failure-classification vocabularies (full CORE check set, readiness-aligned outcomes, six failure classes), JSON rejection of synonyms (`ok`, `healthy`, `pass-ish`, `maybe`, `pass`, `fail`) and prefixed/storage tokens, the outcome-based typed-error invariant (ready carries no error; non-ready carries the observed error), unique/non-empty requirement/precondition/release-gate mappings, free-text protected-value rejection, HTTPS documentation enforcement, stable camelCase web JSON, round-trip, and additive-JSON tolerance.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Registered the conformance check/outcome/classification vocabularies, a conformant and a non-conformant `ConformanceCheckResultV1`, and a `ConformanceRunResultV1` so the new contracts participate in serialization, forbidden-surface, and content-safety scans.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuiteTest.cs` - Added adopter-suite coverage proving all eleven CORE checks run, every check passes against the synthetic fixture, the AC4 scenario matrix (supported, unsupported, cross-tenant, duplicate command, projection lag, sanitized error) is exercised through the run result, every check carries traceable requirement/precondition/release-gate mappings, the tenant-binding check exercises the cross-tenant denial and collapses it to the hidden side-channel-equivalent `unknown` outcome carrying the typed `aggregate_not_found` denial, idempotency surfaces a non-retryable conflict as `blocked`, projection freshness surfaces stale as `degraded`, compatibility discovery surfaces unsupported as a `blocked` typed error, the error-envelope check reuses the shared catalog, and the run result serializes to deterministic, round-trippable, additive-tolerant web JSON.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/CoreFixtureContentSafetyTest.cs` - Added content-safety coverage proving the serialized run result and typed failures never leak poison sentinels, protected tenant/conversation identifiers, or infrastructure terms; the cross-tenant denial uses the hidden `aggregate_not_found` shape without revealing existence; the fixture is synthetic-marked and deterministic; and poison sentinels appear only in the poison projection, never in authorized surfaces.

### Implementation
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ConformanceVocabulary.cs` - Closed `ConformanceCheck` (11 CORE checks), `ConformanceOutcome` (`ready`/`degraded`/`blocked`/`unknown`, aligned to the shared trust/freshness + Story 4.4 readiness language), and `ConformanceFailureClassification` (`conformant`, `product-invariant`, `infrastructure`, `configuration`, `unavailable-dependency`, `execution`).
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ConformanceCheckResultV1.cs` and `ConformanceRunResultV1.cs` - Content-safe machine-readable per-check and run results carrying requirement/precondition/release-gate traceability, the shared `ConversationError` for typed failures, and a documentation pointer; mapping tokens use a closed-token validator that does not run the free-text disclosure blocklist (the Story 4.4 lesson) so legitimate `release-gate-tenant-isolation`-style identifiers are valid.
- [x] `src/Hexalith.Conversations.Contracts/Conformance/ConformanceContractValidation.cs` and `Serialization/ClosedVocabularyJsonConverters.cs` - Field validation and closed-vocabulary JSON converters following the existing pattern.
- [x] `src/Hexalith.Conversations.Testing/Fixtures/ConversationConformanceCoreFixtures.cs` - Deterministic synthetic CORE fixture reusing existing projection/error contracts (no parallel transcript model), with one authorized happy-path conversation plus unsupported/stale/cross-tenant/duplicate-command/sanitized-error typed failures and unique poison sentinels.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs` - The reusable adopter suite runner that reuses Story 4.1 `ConversationContractCompatibility.Evaluate`, Story 4.3 `ConversationErrorCatalog`, and Story 4.4 `ConversationCorePreconditionCatalog`, and emits a `ConformanceRunResultV1`.
- Decision (Dev Notes): the minimum local-evidence slice ships through `Hexalith.Conversations.Testing` (fixture) plus the new `tests/Hexalith.Conversations.Conformance.Tests` project. No separate packable `src/Hexalith.Conversations.Conformance` library and no Manifest/Evidence/signing surface were added; those are deferred to Story 5.10.

### Validation
- [x] Readiness gates re-read in `_bmad-output/implementation-artifacts/readiness-gates.md`: `.NET client versus raw HTTP fallback policy` and `Projection freshness blocking semantics` remain `decided`; conformance targets contracts/client (no raw HTTP fallback) and classifies freshness with exactly the shared `Current`/`Stale`/`Rebuilding`/`Unavailable`/`Forbidden`/`Redacted` vocabulary.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Conformance|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~Versioning"` - 58 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` - 25 passed (after the senior review added the tenant-binding cross-tenant side-channel suite check).
- [x] `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Contracts.1.0.0.nupkg`; the new conformance contracts remain infrastructure-free and adopter-safe.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Conformance 25, Integration 8, Core 139, Server 423, Contracts 349 (967 total) after the senior review added the cross-tenant side-channel suite coverage.

### Coverage
- The adopter-facing conformance suite covers all eleven CORE checks (create conversation, append message, read timeline, tenant binding, Party identity, idempotency, error envelope, projection freshness, event publication, governance preconditions, compatibility discovery) and the AC4 scenario matrix, mapping each check to requirement/precondition/release-gate identifiers so Story 5.10 can aggregate without rework.
- Failure classification distinguishes product-invariant failures from infrastructure, configuration, unavailable-dependency, and execution failures; the cross-tenant scenario collapses to the hidden `aggregate_not_found` shape and never distinguishes unauthorized from nonexistent.
- The synthetic CORE fixture is content-safe and deterministic, reuses existing primitives, and runs after root-level submodule initialization only — no test step requires nested submodule init or `git submodule update --init --recursive`.
- The machine-readable `ConformanceRunResultV1` serializes to deterministic, round-trippable, additive-tolerant camelCase web JSON suitable for CI pass/fail/classification consumption.
- No release-gate aggregation, signed artifacts, manifest rows, waiver governance, raw HTTP fallback examples, new durable state, or new public trust/freshness/outcome vocabulary outside the shared gates was added.

### Checklist Validation
- [x] Contract, adopter-suite, and content-safety/side-channel tests generated for Story 4.5 AC1-AC4.
- [x] Tests use xUnit v3, Shouldly, deterministic synthetic fixtures, and existing serialization/error/compatibility/precondition patterns with no sleeps, live servers, or external services.
- [x] Summary includes targeted validation commands, package-validation evidence, the no-nested-submodule guarantee, and full-solution results.

## Story 4.4 Define CORE Preconditions and Onboarding Diagnostics

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Diagnostics/OnboardingDiagnosticContractsTest.cs` - Added closed-vocabulary coverage for the diagnostic check/status vocabularies, JSON rejection of unsupported/synonym values (`ok`, `healthy`, `maybe`), ready/non-ready error invariants, HTTPS documentation enforcement, content-safe free-text rejection, stable camelCase run-result JSON shape, additive-JSON tolerance, CORE precondition catalog coverage (every required precondition, trust-bearing `Current` state, shared error-catalog reuse), and free-text leakage scanning.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Registered the diagnostic check/status vocabularies, `OnboardingDiagnosticCheckResultV1` (ready and degraded), `OnboardingDiagnosticRunResultV1`, and a `CorePreconditionV1` so the new contracts participate in serialization, forbidden-surface, and content-safety scans.
- [x] `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationOnboardingDiagnosticsServiceTest.cs` - Added AC4 service coverage with deterministic fakes for ready, missing tenant context, denied access side-channel equivalence, production-faithful freshness/availability access denials (stale/gap/rollback/poisoned/unavailable) collapsing to the hidden `unknown` shape, stale tenant projection, projection subscription failure, audit sink unavailable, unsupported contract, schema incompatibility, missing provider config, Parties integration unavailable, throwing-signal fail-closed behavior, content-safety scans, mutation-boundary separation, and DI registration with fail-closed defaults.

### Validation
- [x] Readiness gates re-read in `_bmad-output/implementation-artifacts/readiness-gates.md`: `Projection freshness blocking semantics` and `Command availability metadata` remain decided; diagnostics use only the shared `Current`/`Stale`/`Rebuilding`/`Unavailable`/`Forbidden`/`Redacted` vocabulary.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Diagnostic|FullyQualifiedName~Precondition|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~Versioning"` - 61 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Diagnostic|FullyQualifiedName~Onboarding"` - all targeted diagnostics tests passed (36 diagnostics tests after the review added production-faithful freshness/availability side-channel coverage).
- [x] `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Contracts.1.0.0.nupkg`.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Integration 8, Core 139, Server 423, Contracts 316 (909 total) after the senior review added side-channel coverage.

### Coverage
- Diagnostics contracts expose a closed check vocabulary, a closed status vocabulary mapped to the shared trust/freshness language (`ready`/`degraded`/`blocked`/`unknown`), per-check and run results, a CORE precondition descriptor, and a contract-owned precondition catalog reused by docs and tests, all reusing the shared `ConversationError`/`ConversationErrorCatalog` envelope rather than a parallel model.
- The read-only server orchestrator binds tenant/caller authority from the trusted boundary, fails closed on missing/denied/cross-tenant requests with a side-channel-equivalent `unknown` result, derives projection-subscription/freshness from `ConversationTenantProjectionHealth`, delegates schema/contract compatibility to `ConversationContractCompatibility.Evaluate`, and reports bounded audit, Parties, and provider-configuration statuses without leaking protected detail.
- No new durable state, runtime gate semantic, public error/status/freshness vocabulary, or globally-runnable host was added; dependent command/query gating continues through the existing tenant-access guard, freshness gate, audit pairing, and idempotency executor proven by existing command/read API tests.

### Checklist Validation
- [x] Contract, server, content-safety, side-channel, dependency-boundary, and DI tests generated for Story 4.4 AC1-AC4.
- [x] Tests use xUnit, Shouldly, deterministic fakes, and existing tenant-access/projection-health/participant-directory patterns with no sleeps or live services.
- [x] Summary includes targeted validation commands, package-validation evidence, and full-solution results.

## Story 4.3 Expose Typed Sanitized Errors and Remediation Guidance

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs` - Added descriptor coverage for every supported error code, retryability/catalog consistency, safe message/action fields, HTTPS documentation pointers, audit-handle allowance, closed action vocabulary serialization, and additive JSON tolerance.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs` - Extended unsafe free-text rejection to the new `SafeMessage` field and preserved serialized content-safety checks across curated error fixtures.
- [x] Senior review auto-fix: `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs` now rejects tenant, Party, conversation, provider-session/payload, business-reference, local-path, and exception markers across every protected `ConversationError` free-text field.
- [x] Senior review auto-fix: `tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs` now proves unsupported error code/category/action parser and JSON converter diagnostics do not echo raw protected values.
- [x] Senior review auto-fix: `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs` now proves invalid package versions return `send-semantic-package-version` while invalid schema versions keep `send-positive-integer-schema-version`.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs` and `ContractSamples.cs` - Updated representative wire fixtures and serialization samples for `clientAction`, `safeMessage`, catalog descriptors, and closed action vocabulary.
- [x] `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` - Added typed fallback coverage for unsupported schema before send, typed server error bodies, non-JSON 400/401/403/404/409/500 responses, timeout/unknown outcome, tenant denial fallback, idempotency conflict, and client-visible content-safety.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs` - Added command API coverage for malformed body, missing metadata, unauthenticated caller, missing tenant claim, tenant mismatch, route/body mismatch, handler-supplied idempotency conflict, handler-supplied audit unavailable, stale projection, participant/onboarding unavailable, provider-identity failures, and shared catalog action/message fields.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Error|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~Versioning|FullyQualifiedName~ContractSerialization"` - 49 passed.
- [x] Senior review auto-fix: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Error|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~Versioning|FullyQualifiedName~ContractSerialization"` - 50 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 23 passed.
- [x] Senior review regression: `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 23 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi"` - 13 passed after QA automation follow-up coverage.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~ConversationReadApi|FullyQualifiedName~Governance|FullyQualifiedName~Idempotency"` - 116 passed after QA automation follow-up coverage.
- [x] Senior review regression: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~ConversationReadApi|FullyQualifiedName~Governance|FullyQualifiedName~Idempotency"` - 116 passed.
- [x] `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Contracts.1.0.0.nupkg`.
- [x] Senior review regression: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - passed.
- [x] `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Client.1.0.0.nupkg`. First parallel attempt collided on a shared contracts intermediate DLL lock; serial rerun passed.
- [x] Senior review regression: `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - exited successfully.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Contracts 278, Integration 8, Core 139, Server 386.
- [x] Senior review regression: `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Contracts 279, Integration 8, Core 139, Server 390.

### Coverage
- Contracts now expose canonical typed remediation fields through `ConversationErrorClientAction`, `ConversationErrorDescriptor`, and `ConversationErrorCatalog` without replacing `ConversationErrorResult`.
- REST, client fallback, and compatibility checks now source category, retryability, safe action, safe message, documentation pointer, and audit-handle allowance from the shared catalog.
- Client and server tests prove raw non-JSON failures, malformed requests, authorization failures, idempotency conflicts, audit unavailable, stale projection, participant/onboarding unavailable, provider-identity failures, unsupported schemas, and unknown outcomes coarsen to bounded typed errors without leaking tenant IDs, provider/session details, route internals, raw exception text, or storage/infrastructure terms.
- Senior review coverage proves typed error free-text guards reject common protected identifier, provider, business-reference, local-path, and exception markers, and parser/converter diagnostics avoid echoing unsupported raw closed-vocabulary values.
- Compatibility remediation coverage now distinguishes invalid schema-version guidance from invalid package-version guidance.
- README and contract package docs now document compact adopter-facing error semantics at the contract/client level without adding raw HTTP fallback examples.

### Checklist Validation
- [x] Story 4.3 AC1-AC4 are covered by contract descriptor tests, client fallback tests, server command API tests, compatibility serialization tests, forbidden-surface scans, package validation, and full-solution regression tests.
- [x] The implementation keeps shared contracts plus `Hexalith.Conversations.Client` as the supported v1 path and does not add adopter-facing raw HTTP fallback guidance.
- [x] Summary includes targeted validation commands, package-validation evidence, full-solution results, and the transient pack-lock rerun note.

## Story 4.2 Provide Supported .NET Client Happy Path

### Generated Tests
- [x] `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` - Added deterministic fake-transport coverage for create, append, and read request mapping; typed success/error mapping; current and non-current freshness outcomes; unsupported schema handling; duplicate replay; idempotency conflict; timeout/unknown outcome retry; non-seekable HTTP response content; tenant denial fallback; sanitized server errors; and DI typed-client registration.
- [x] `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs` - Extended client assembly boundary coverage for allowed Microsoft HTTP/DI references only and absence of raw HTTP fallback public surface.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs` - Added opt-in command API coverage for authorization metadata, create/append route shape, tenant binding, route/body conversation mismatch, typed idempotency conflict mapping, and content-safety.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` - Updated client package inventory coverage now that Story 4.2 intentionally adds supported client behavior.

### Validation
- [x] Readiness gates verified in `_bmad-output/implementation-artifacts/readiness-gates.md`: `Projection freshness blocking semantics` and `.NET client versus raw HTTP fallback policy` are `decided`.
- [x] Red phase: `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` failed before implementation because `ConversationClient`, `IConversationClient`, and DI references did not exist.
- [x] `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 17 passed after review fix.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationReadApi|FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~Idempotency"` - 56 passed.
- [x] `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Client.1.0.0.nupkg`.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed after review fix: Client 17, Contracts 273, Integration 8, Core 139, Server 382.

### Coverage
- The supported .NET client now posts v1 `CreateConversationCommand` and `AppendMessageCommand`, reads `ConversationDetailResult` from the existing read route, and returns typed success or `ConversationErrorResult` outcomes without exposing `HttpResponseMessage` or EventStore mechanics.
- Review fix hardened response deserialization for non-seekable HTTP content streams used by real transports.
- Idempotency metadata is preserved through command bodies and safe headers; duplicate replay and conflict behavior remain typed and caller-visible without using provider session IDs as durable identity.
- Freshness handling preserves `ConversationDetailResult` trust states; only `Current` + `current` + non-stale detail allows trust-bearing timeline use.
- A narrow opt-in `ConversationCommandApi` server extension was added for hosts/tests while `Program.cs` remains fail-closed.
- Raw HTTP fallback remains non-promotional: no public raw HTTP fallback API, examples, README snippets, or docs were added.

### Checklist Validation
- [x] Client, server API, boundary, package inventory, and raw-fallback-negative tests generated or updated.
- [x] Tests use xUnit, Shouldly, deterministic fake HTTP handlers, ASP.NET endpoint invocation, and existing contract DTOs.
- [x] Tests cover happy path plus unsupported schema, stale/rebuilding/unavailable/forbidden freshness, timeout retry, duplicate replay, idempotency conflict, tenant denial, sanitized errors, and dependency boundaries.
- [x] Summary includes validation commands, package-validation evidence, and full-solution results.

## Story 4.1 Publish Conversations Contract Package and Compatibility Metadata

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs` - Added compatibility metadata coverage for active v1 command/projection/event/package discovery, closed status vocabulary serialization, supported/deprecated/unsupported/invalid checks, malformed and unsupported package-version inputs, additive JSON tolerance, typed safe failures, and forbidden content fragments.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` - Added package inventory coverage that packs the contracts project and inspects `.nupkg`/`.nuspec` metadata and entries for adopter-safe contents.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization and forbidden-surface fixtures for compatibility status, package version, remediation, request, metadata, and result contracts.

### Validation
- [x] Red phase: targeted contract test filter failed before implementation because the new compatibility types did not exist.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"` - 38 passed.
- [x] `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Contracts.1.0.0.nupkg`.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ContractPackageInventory"` - 2 passed.
- [x] QA follow-up: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractPackageInventory"` - 44 passed.
- [x] QA follow-up: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - completed successfully.
- [x] Senior review auto-fix: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractPackageInventory"` - 49 passed.
- [x] Senior review auto-fix: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - completed successfully.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed after senior review fixes: Contracts 273, Client 1, Integration 8, Core 139, Server 377.

### Coverage
- Compatibility metadata now exposes active v1 command, projection, event, contracts package, and .NET client package versions through contract-owned DTOs.
- Compatibility status is a closed vocabulary with JSON converter coverage for `supported`, `deprecated`, `unsupported`, and `invalid`.
- Compatibility checks return content-safe typed results for supported, deprecated package, unsupported schema/package, and malformed schema/package inputs.
- Senior review coverage enforces status/remediation/error invariants, non-null compatibility status, package-specific contracts/client version evaluation, and client package metadata alignment without adding client behavior.
- Package validation proves the contracts `.nupkg` includes adopter metadata and README guidance while excluding server, infrastructure, UI, test, and generated files.
- No server compatibility endpoint, client happy-path behavior, onboarding diagnostics, conformance package, release signing, or deprecation policy lifecycle was added.

### Checklist Validation
- [x] Contract, package inventory, serialization, and boundary tests generated.
- [x] Tests use xUnit, Shouldly, `System.Text.Json`, NuGet package inspection, and existing contract-sample safety patterns.
- [x] Tests cover happy path plus deprecated, unsupported, malformed, additive JSON, package inventory, dependency boundary, and content-safety scenarios.
- [x] Summary includes validation commands and package-validation evidence.

## Story 3.7 Provide Self-Serve Buyer Acceptance Demo

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/BuyerAcceptanceDemoContractTest.cs` - Added buyer acceptance scenario, fixture, step, evidence summary, verification summary, closed vocabulary, JSON shape, duplicate mapping, temporal cursor validation, undeclared fixture rejection, and content-safety coverage.
- [x] `tests/Hexalith.Conversations.Tests/Testing/BuyerAcceptanceDemoFixtureTest.cs` - Added deterministic synthetic fixture coverage for canonical trust states, synthetic marker, unique scenario steps, composite temporal cursor fixture, authorized projection data, verification pass/fail fixtures, and cross-tenant poison sentinel non-disclosure.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationBuyerAcceptanceDemoServiceTest.cs` - Added service runner coverage for full walkthrough summary, selected verification pass/fail output, out-of-scope verification filtering, temporal replay source wiring, module-vs-inherited evidence scope, cross-tenant denial, missing caller fail-closed behavior with verification summary suppression, missing/same-tenant probe partial outcomes, poison sentinel safety, DI registration, and mutation-boundary separation.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 18 passed after red phase and review fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 2 passed after red phase.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 8 passed after QA and review gap fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 78 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationGovernanceVerificationServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 121 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 52 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 145 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 774 passed.

### Coverage
- Buyer acceptance contracts expose deterministic scenario, fixture, step, evidence summary, selected verification summary, pass/fail status, requirement mappings, evidence handles, and module-vs-inherited ownership without open polymorphism.
- Synthetic fixtures cover full trust, redaction with audit evidence, stale evidence, missing citation/incomplete audit, unresolved participant hydration, blocked governance command metadata, verification pass/fail, and cross-tenant poison sentinels.
- The service runner composes existing read/query/projection and attached verification outputs, binds tenant/caller authority from the trusted boundary, fails closed without caller authority, and returns only an in-memory content-safe summary.
- No HTTP endpoint, UI shell, durable demo store, production seed store, export artifact, command handler, governance audit gate, or EventStore append path was added.

### Checklist Validation
- [x] Contract, fixture-builder, service, DI, and safety tests generated for Story 3.7.
- [x] API/demo-host route assessed as not necessary for current repo shape; service and tests provide the self-serve execution entry point without mutation semantics.
- [x] Tests use standard xUnit, Shouldly, DI resolution, deterministic fixture builders, fake in-memory read stores, and reflection safety-net checks.
- [x] Tests cover repeatability, safe fixture handling, selected verification output, out-of-scope verification filtering, canonical temporal cursor handling, content-safe evidence summary, module-vs-inherited evidence separation, missing caller/verification/probe partial or failed outcomes, same-tenant hidden read rejection for cross-tenant proof, cross-tenant poison non-disclosure, and mutation-boundary separation.

## Story 3.6 Run Governance Verification and Return Structured Results

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/GovernanceVerificationContractTest.cs` - Added contract coverage for stable verification JSON shape, closed verification scope/suite/status/classification/remediation vocabularies, safe diagnostic text rejection, required v1 suite/classification vocabulary, duplicate suite rejection, and inverted time-window rejection.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for verification vocabularies, scope, request, check result, run result, and evidence handle records.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationGovernanceVerificationServiceTest.cs` - Added service coverage for passing verification, missing audit pair, redaction replay failure, projection rebuild disagreement, unsupported schema, stale projection, missing/non-verify privileged justification, tenant-wide deferred scope, local read-only audit-not-recorded reason, dependency unavailable, retained-coverage data unavailable, rebuilding temporal evidence, thrown event-source failure, unauthorized scope, cross-tenant poison, provider correlation authority misuse, DI registration, and mutation-boundary separation.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractSerialization"` - 17 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationGovernanceVerification"` - 18 passed after senior review auto-fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ConversationQuery|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 66 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifier|FullyQualifiedName~ConversationAggregateRedaction|FullyQualifiedName~ConversationAggregateRetentionPolicy|FullyQualifiedName~ConversationAggregateSensitivity"` - 51 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationProjectionRebuildVerifierTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 125 passed after senior review auto-fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~ConversationPrivilegedOperationalJustificationService|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 133 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 746 passed after senior review auto-fixes.

### Coverage
- Verification contracts now expose tenant-safe request scope, suite selection, per-check results, run results, execution status, failure classification, evidence handles, and safe remediation without open polymorphism or raw infrastructure disclosure.
- Server verification runs through trusted tenant/caller authority, requires existing verify justification before touching tenant conversation data, blocks on non-current projection freshness, reuses replay and projection rebuild proof paths, and keeps verification output as derived evidence only.
- Check adapters distinguish governance failures from dependency, stale projection, data unavailable, unsupported version, unauthorized/hidden, and execution-style failures across audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, schema compatibility, missing/incorrect privileged verification justification, deferred tenant scope, and local read-only proof paths.
- No HTTP execution surface, worker, durable verification store, event append path, UI shell, evidence bundle export, or Story 3.7/3.8 scope was added.

### Checklist Validation
- [x] Contract, service, and safety tests generated for the implemented verification workflow.
- [x] API/CLI boundary assessed as service-only for this story because the repository has no approved CLI/worker/Admin shell and adding an HTTP execution endpoint was optional.
- [x] Tests use standard xUnit, Shouldly, DI resolution, reflection safety-net checks, and existing fake-store patterns.
- [x] Tests cover happy path plus invariant failure, infrastructure/dependency failure, stale projection, retained-coverage data unavailable, missing audit pair, missing/non-verify verify justification, local read-only audit state, tenant-wide deferred scope, redaction replay failure, projection rebuild disagreement, unsupported schema, provider portability failure, cross-tenant poison, unauthorized scope, and release-gate suitability.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes validation commands and coverage metrics.

## Story 3.5 Preserve Read-Only Compliance Workflows and Safe Command Gates

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationCommandAvailabilityContractTest.cs` - Added fail-closed command availability contract coverage for explicit read-only/governance-changing classification, fresh server recheck requirements, missing-metadata defaults, available-governance gate validation, valid available-metadata recheck requirements, mandatory recheck on unavailable metadata, normalized safe vocabulary rejection, and stable JSON serialization.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added projection state matrix coverage proving default governance command metadata remains unavailable, server-owned, audit/freshness annotated, and recheck-required across current, audit-ready, stale, rebuilding, unavailable, and unsupported-schema projections.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added read-boundary coverage proving projection-owned command metadata is preserved as advisory metadata, available metadata stays recheck-gated, stale projections clear protected detail state, and default missing command metadata remains unavailable/read-only/recheck-required.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added API coverage proving the authorized read group exposes GET routes only and ignores client-supplied command metadata/authority.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs` - Added safety-net coverage proving read-only workspace boundaries do not directly depend on governance mutation handlers, the governance audit gate, or idempotent command execution.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCommandAvailability|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 66 passed after senior review auto-fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 122 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 118 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 714 passed after senior review auto-fixes.

### Coverage
- Command availability metadata is now explicitly classified as read-only or governance-changing, requires fresh server recheck for every metadata instance, and rejects unsafe vocabulary tied to EventStore internals, provider payloads, browser/client state, route secrets, raw exceptions, and Party personal data, including separator/casing variants.
- Missing command metadata still produces non-empty unavailable defaults; projection-owned governance commands remain advisory from read workflows, and any available metadata must still carry required permission, precondition, risk, freshness, audit, blocked reason, classification, last-evaluated metadata, and a fresh server recheck requirement.
- Read APIs remain GET-only under the existing authorized `/api/v1/conversations` group and continue binding tenant/caller from trusted claims only.
- Stale or denied read transitions close protected detail/citation/audit/command fields rather than retaining clipboard-ready, temporal, audit, or command authority data.
- UI/component E2E was not applicable for Story 3.5 because there is still no `Hexalith.Conversations.Admin` or web project; the implemented scope is contracts, server projections/query behavior, read API, and safety tests.

### Checklist Validation
- [x] API and backend read-path safety tests generated for command-gate behavior.
- [x] UI E2E tests assessed as not applicable because no UI exists in this repository for this story.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, reflection safety-net checks, and existing fake-store patterns.
- [x] Tests cover fail-closed defaults, blocked command metadata, available command metadata, mandatory recheck metadata, normalized safe command fields, stale projection clearing, client-supplied metadata rejection, read-only route shape, and mutation-boundary separation.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes validation commands and coverage metrics.

## Story 3.4 Copy Citations and Stable Temporal Evidence Links

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationCitationContractTest.cs` - Added citation DTO/result serialization coverage for safe copied text, safe labels/accessibility text, audit-handle inclusion, temporal cursor metadata, forbidden EventStore/provider/storage/personal-data/browser-selection vocabulary, unsafe citation DTO construction, and unsafe evidence-entry target rejection.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs` - Extended temporal anchor coverage for composite authoritative anchors carrying safe source position, projection cursor, projection version, and supporting timestamp while rejecting mismatched composite cursors.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for citation target, citation query, citation DTO, and citation result.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added citation query coverage for authorized safe DTO construction, redacted target placeholder/attribution output, missing audit-handle downgrade, denied/missing/stale/cross-tenant projection fail-closed behavior, future source-position gap handling, no original message text, and tenant authorization/projection read boundaries.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs` - Updated deterministic temporal re-resolution assertions for composite authoritative anchors with projection cursor/version metadata and mismatched projection-version cursor fail-closed behavior.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added authorized route metadata and read API coverage for citation and temporal routes, trusted claim binding, malformed target/cursor hidden equivalence, strict malformed projection-cursor rejection, unsafe query-string value exclusion, and citation permission-downgrade clearing of clipboard/link metadata.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~TemporalReconstruction|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 37 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 74 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 183 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 687 passed.

### Coverage
- Citation copy now resolves through a Conversations-owned `ConversationCitationAccessService` after tenant/caller authorization and current projection freshness recheck; DTO output is built from governed evidence metadata rather than rendered/client text.
- Citation contracts expose schema, tenant, conversation, evidence id/kind, timestamp, actor PartyId, audit handle when ready, projection cursor/version, temporal cursor, safe copied text, safe labels, and safe next action without raw EventStore/provider/storage or original redacted content.
- Missing audit handles, missing/deleted evidence entries, redacted targets, stale projections, denied callers, cross-tenant projection poison, malformed targets, future source-position gaps, permission downgrades, and malformed temporal cursors fail closed with hidden/unavailable/rebuilding shapes rather than trusted citation output.
- Temporal reconstruction now returns a composite authoritative anchor containing safe source position plus projection cursor/version; timestamps remain supporting metadata only.
- HTTP surfaces remain read-only under the existing authorized `/api/v1/conversations` group. UI E2E is not applicable for Story 3.4 because there is still no Admin/FrontComposer project in this repository.

### Checklist Validation
- [x] API and backend E2E-style read-path tests generated for the implemented citation and stable temporal-link feature.
- [x] UI E2E tests assessed as not applicable because Story 3.4 is implemented as backend contracts/server/API read behavior in this repository.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, and existing fake-store patterns.
- [x] Tests cover happy path plus missing audit handle, malformed cursor, stale projection, redacted target, missing/deleted evidence, outside coverage, unsupported schema, cross-tenant link/projection poison, unauthorized-existing record, permission downgrade, and clipboard/browser/accessibility metadata safety.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes coverage metrics.

## Story 3.3 Inspect Redaction Attribution and Governance Audit Trail

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs` - Added redaction attribution contract coverage for safe JSON shape, safe labels/accessibility text, audit-handle linkage, missing-audit incomplete state, canonical placeholder enforcement, target-key consistency, visible-text consistency, readiness consistency, and forbidden original-content/provider/storage vocabulary.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixture coverage for `ConversationRedactionAttributionV1`.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added projection coverage proving redacted message evidence carries inline attribution, redaction evidence links to the same audit handle, governance evidence anchors expose safe detail metadata, chronological evidence ordering remains stable, and redacted content stays suppressed.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added route coverage proving audit-detail reads are under the authorized `/api/v1/conversations` group, bind tenant/caller only from trusted claims, ignore caller-supplied authority/action query data, return safe detail JSON, hide malformed handles without projection reads, hide missing trusted-tenant claims without projection reads, coarsen unexpected audit store failures to safe unavailable responses, and clear protected audit detail after a permission downgrade.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs` - Added gap coverage proving redaction audit records with missing audit anchors return hidden detail while preserving redacted placeholders, and unexpected audit source failures return content-safe unavailable results.
- [x] Existing `ConversationAuditRecordAccessServiceTest` and `ConversationQueryHandlerTest` coverage exercised independent audit authorization, stale/rebuilding/unavailable/cross-tenant/malformed handle states, redaction audit detail, and safe policy treatment.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationEvidence|FullyQualifiedName~Redaction|FullyQualifiedName~AuditRecord|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 32 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 94 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~Temporal|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationQueryRegistrationTest"` - 152 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 665 passed.

### Coverage
- Governed evidence entries now carry server-owned safe audit target/link metadata and optional inline redaction attribution without original content, raw snippets, provider payloads, upstream Party details, EventStore topology, storage locations, or audit sink details.
- Redacted message entries and redaction evidence entries link to the same safe audit evidence reference when present; missing audit metadata remains explicit through incomplete readiness instead of becoming ready/current by default.
- Redaction placeholders are canonical `[redacted]` markers, redaction attribution target keys must match governed targets, and redacted evidence visible text plus audit readiness must stay consistent with attached attribution.
- Missing redaction audit anchors, missing trusted tenant claims, audit store failures, and permission-downgraded inline audit detail refreshes fail closed without retaining protected policy basis, audit evidence handles, raw failure terms, or detail payloads.
- Retention, sensitivity, and redaction evidence records expose stable target, actor, timestamp, policy basis, rationale class, trust state, audit readiness, safe labels, and next-action metadata for a future trust component without adding an Admin shell.
- Audit detail reads are exposed through the existing authorized read API/query boundary and continue to rely on tenant authorization, current projection freshness, malformed-handle hiding, policy-blocked shapes, and content-safe unavailable/rebuilding results.
- UI E2E tests are not applicable for Story 3.3 because this repository still has no Admin/FrontComposer project for this slice; the implemented scope is contracts, server projection/query behavior, API route, and safety tests.

### Checklist Validation
- [x] API and backend E2E-style read-path tests generated for the implemented redaction/audit inspection feature.
- [x] UI E2E tests assessed as not applicable because Story 3.3 is implemented as backend contracts/server/API read behavior in this repository.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, and existing fake-store patterns.
- [x] Tests cover happy path plus unauthorized audit, redacted evidence, missing audit anchor, stale projection, malformed handle, permission downgrade, accessibility-label contract, and safe hidden/unavailable states.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes coverage metrics.

## Story 3.2 Governed Conversation Evidence Read

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs` - Added governed detail contract coverage for trust posture, evidence entries before message timeline data, fail-closed command eligibility defaults, explicit unavailable metadata defaults, and forbidden infrastructure/provider/session/transcript vocabulary checks.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for governed evidence trust posture, evidence entries, and command availability metadata.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added projection-owned trust posture/evidence-entry assertions, evidence kind coverage for messages/participants/attachments/freshness, chronological message evidence ordering, and redaction placeholder preservation in evidence entries.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs` - Added QA follow-up coverage proving stale, rebuilding, unavailable, and redacted detail projections do not become trust-bearing reads.
- [x] `tests/Hexalith.Conversations.Server.Tests/Hydration/ConversationReadHydrationServiceTest.cs` - Added detail participant-resolution downgrade coverage while preserving projection-owned evidence completeness and citation availability.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added governed detail query assertions for tenant scope, record identity, temporal cursor, command eligibility, missing citation metadata, partial evidence metadata, and evidence entry propagation after authorized projection read/hydration.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added existing detail route coverage proving authorized reads return trust posture, evidence entries, command eligibility, malformed route values fail hidden without projection reads, trusted claims are used instead of caller-supplied authority, and no unsafe EventStore/provider-session/transcript terms are exposed.
- [x] AI review follow-up - Added regression coverage for full evidence-entry chronology across participant/message/attachment/freshness records, forbidden participant-resolution aggregation precedence, and projection store exception coarsening for detail/list reads.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 20 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationReadHydrationServiceTest|FullyQualifiedName~ConversationReadApiTest"` - 93 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Temporal|FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryRegistrationTest"` - 148 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 653 passed.

### Coverage
- Detail projections and query details now expose source-owned trust posture with tenant scope, record identity, safe temporal cursor, projection freshness, evidence completeness, participant resolution, citation availability, audit readiness, verification state, and server-owned command availability metadata that defaults blocked/unavailable.
- Governed evidence entries now represent messages, participants, attachments, retention policy, sensitivity marks, redactions, and freshness metadata as evidence records rather than chat bubbles, preserving chronological evidence ordering and redacted placeholders.
- Server coverage verifies tenant/freshness denial ordering remains unchanged, non-current detail projections fail closed, projection store failures coarsen to unavailable, missing citation and partial evidence metadata remain explicit, detail hydration is response-scoped and only aggregates participant resolution, and the existing authorized API route returns the governed read shape without unsafe new routes or caller-supplied authority.

### Checklist Validation
- [x] API and backend E2E-style read-path tests generated for the implemented governed detail feature.
- [x] UI E2E tests assessed as not applicable because Story 3.2 is implemented as backend contracts/server/API read behavior in this repository.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, and existing fake-store patterns.
- [x] Tests cover happy path plus stale, rebuilding, unavailable, redacted, malformed route, missing citation, partial evidence, unresolved participant, and cross-tenant/hidden cases.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes coverage metrics.

## Story 3.1 Tenant-Scoped Find Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs` - Added Story 3.1 filter vocabulary validation, invalid date range rejection, search trust-preview JSON shape coverage, and assertions that list responses avoid totals, facets, autocomplete, recent-search, provider-session, and transcript surfaces.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixture coverage for Story 3.1 search trust-preview contracts and vocabularies.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added exact filter coverage for redaction state, freshness state, audit readiness, and verification state; safe no-accessible-matches shape coverage; trust-preview hydration behavior; and review regression coverage for non-current accessible matches beyond the continuation lookahead row.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added query-string binding coverage for Story 3.1 trust filters and fail-closed malformed date/closed-vocabulary filter handling without projection reads.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationSearch|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 17 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 117 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~ConversationQueryRegistrationTest"` - 138 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 642 passed.

### Coverage
- Contracts now extend the existing tenant-scoped list-query path with closed, safe search filters for redaction, freshness, audit readiness, and verification state, without adding provider/session identifiers, broad transcript search, totals, facets, autocomplete, or recent-search metadata.
- Summary and projection contracts now expose compact search trust previews with freshness, redaction, participant resolution, citation availability, audit readiness, verification state, match source, and safe why-visible copy. Older projections default to non-assumptive trust metadata.
- Server/API coverage verifies tenant access still gates projection reads, poison rows are tenant-filtered before search, new filters apply only to tenant-scoped projection fields, malformed query values return hidden list shape, hydration updates participant resolution state after paging, non-current accessible matches downgrade list freshness even beyond the continuation lookahead row, and no accessible matches use safe empty copy.

## Story 2.8 Privileged Operational Justification Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/PrivilegedOperationalJustificationContractTest.cs` - Added privileged operation-class vocabulary, structured justification command/detail/result JSON shape, required-field validation, unsupported vocabulary rejection, `ToString()` safety, and forbidden substrate/personal-data field coverage.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for privileged justification vocabulary, command, query, details, and result contracts.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationPrivilegedOperationalJustificationServiceTest.cs` - Added server precondition coverage for approved privileged action, missing justification, unauthorized operator, governance-class authorization, stale/rebuilding freshness, cross-tenant projection poison, audit unavailable/unsafe/uncertain/policy-blocked paths, partial operation outcome, and no delegate execution before gates pass.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationPrivilegedJustificationReviewServiceTest.cs` - Added review-history coverage for authorized reviewer access, unauthorized non-disclosure, malformed handles, unavailable review source, stale review evidence, and explicit redacted/withheld fields.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryRegistrationTest.cs` - Added fail-closed query registration coverage proving the handler resolves with the default unavailable privileged review source when no durable source is configured.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs` - Updated audit-pairing inventory so `RecordPrivilegedJustification` is an implemented privileged audit boundary without making ordinary conversation commands audit-sink dependent.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 69 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest"` - 36 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 97 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ContractSerializationTest|FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 72 passed.
- [x] Review fix validation `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~ConversationQueryRegistrationTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~Projection"` - 119 passed.
- [x] Review fix validation `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 69 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 625 passed.

### Coverage
- Contracts now expose a closed privileged operation-class vocabulary, structured justification command, governed review query, coherent review details, and content-safe result states without raw conversation content, EventStore topology, storage paths, provider payloads, Party personal data, tokens, claims, or raw audit sink identifiers.
- Server coverage verifies tenant authorization occurs before protected evidence resolution, privileged reads/exports/verifications use Admin access, governance-changing metadata/visibility paths use Governance access, current freshness and audit evidence are required before executing privileged delegates, non-success/throwing delegate outcomes are audit-linked with content-safe diagnostics, and unsafe states fail closed without mutation/disclosure.
- Review coverage verifies authorized compliance reviewers receive coherent tenant-scoped records, while unauthorized, malformed, stale, unavailable, and redacted states remain explicit and non-disclosing.

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs` - Added Story 2.3 server-boundary tests for non-success audit statuses, tenant mismatch before audit proof, idempotency conflict before state load/audit, compatible duplicate replay, materially different same-key conflict, and sanitized replay payloads.

### E2E Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added replay/materialization coverage for sensitivity-mark events from accepted public events through derived read state, plus unsupported-version downgrade behavior.
- [x] UI E2E tests are not applicable for Story 2.3 because this repository currently exposes backend contracts/server flows and no implemented UI workflow for sensitivity marking.

## Coverage
- API/application boundary: governance authorization, audit fail-closed behavior, tenant binding, idempotency conflict, duplicate replay, materially different same-key rejection, and sanitized retry-safe outcomes are covered.
- Projection/E2E-style workflow: accepted sensitivity events rebuild target-keyed read-model state with safe audit/trust metadata; unsupported-version sensitivity events do not upgrade projected trust.
- Existing Story 2.3 coverage remains in contract, aggregate, publication, projection accumulator, privacy, and serialization tests.
- UI features: 0/0 applicable for this backend-only story.

## Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` - 152 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-restore` - 124 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore` - 228 passed.
- [x] `dotnet test Hexalith.Conversations.slnx --no-restore` - 513 passed.

## Checklist Validation
- [x] API/application-boundary tests generated.
- [x] E2E-style replay/materialization tests generated for the backend workflow.
- [x] UI E2E tests assessed as not applicable because no UI exists.
- [x] Tests use standard xUnit and Shouldly APIs.
- [x] Tests cover happy path duplicate replay and critical error cases.
- [x] Tests use clear descriptions, no hardcoded waits, and no order dependency.
- [x] Summary includes coverage metrics and validation commands.

## Next Steps
- Keep the contract, domain, server, projection, and solution test lanes in CI for Story 2.3.

## Story 2.7 Audit Record Governance Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs` - Added audit-record action vocabulary, audit target key, missing-handle validation, JSON shape, query/contract `ToString()` safety, unsupported vocabulary rejection, and forbidden substrate field coverage.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added handler entry-point coverage proving `GetAuditRecordAsync()` returns citeable audit evidence through the governed query boundary.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs` - Added server audit-record read/export coverage for allowed read, denied read, denied export, policy-blocked export, redacted/withheld details, stale/rebuilding projection, malformed handles, cross-tenant projection poison, source unavailability, rebuild preservation, outcome-only action blocking, and mutation attempts.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Existing projection/redaction replay coverage was exercised with Story 2.7 filters to prove rebuild and redaction behavior remains stable.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 63 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest"` - 46 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 69 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 595 passed.

### Coverage
- Contracts now expose closed audit-record action classes, safe audit-record target keys, missing-handle rejection, policy treatment metadata, governed audit review details, and content-safe read/export results without raw audit sink, storage, EventStore topology, provider payload, message text, redacted text, Party personal data, or raw upstream fields.
- Server coverage verifies tenant authorization occurs before handle parsing and projection reads; the query handler exposes the same governed audit-record boundary; unauthorized, malformed, cross-tenant, unavailable, stale, and rebuilding paths return non-disclosing results; allowed export is in-memory only; policy-blocked, outcome-only, and separate-log paths do not create unmanaged durable export surfaces.
- Rebuild/redaction coverage verifies derived audit views preserve citeable metadata while message redaction remains distinct from audit-record redaction and does not reintroduce suppressed message text.

## Story 2.6 Point-in-Time Governance Reconstruction Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs` - Added temporal anchor/result serialization, supported cursor form validation, forbidden substrate vocabulary checks, and safe hidden-result shape coverage.
- [x] `tests/Hexalith.Conversations.Tests/Replay/ConversationReplayVerifierTest.cs` - Added replay coverage proving retention, sensitivity, and redaction governance events replay deterministically with existing fail-closed replay protections preserved.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added redaction projection coverage proving redacted message text is replaced with a safe placeholder, redaction read state carries safe audit metadata, and prior redaction state suppresses later materialized message text.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs` - Added server temporal reconstruction coverage for timestamp anchors, safe-position cursors, projection cursors, contract cursors, malformed/cross-tenant cursor failure, projection rebuild, incomplete sources, source gaps, unsupported schema, and out-of-coverage behavior.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 22 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifierTest|FullyQualifiedName~Temporal"` - 20 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 53 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 572 passed.

### Coverage
- Contract coverage verifies temporal query/result DTOs expose Conversations-owned anchors, safe next actions, confidence/freshness metadata, and no EventStore stream/snapshot/raw substrate terms.
- Replay coverage verifies public/domain governance events are applied by `ConversationReplayVerifier` while existing tenant, conversation, schema, event-type, position, duplicate, malformed payload, unknown-event, and rejection no-op behavior remains covered.
- Server coverage verifies authorization and current disclosure projection checks happen before temporal evidence reads; timestamp, safe-position, projection-cursor, and contract-cursor anchors resolve to safe authoritative anchors; and unsafe cursor/source/projection states return hidden, unavailable, or rebuilding results without protected detail disclosure.
- Redaction coverage verifies current redaction policy suppresses historical message text, prior redaction state suppresses later materialized message text, and responses expose only placeholders, policy reason class, actor attribution, timestamp, and citeable audit handles.

## Story 2.5 Audit Pairing Enforcement Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRetentionPolicyTest.cs` - Added aggregate coverage proving mismatched retention audit evidence fails with `AuditPairingRequired` / `audit_pairing_mismatch` before retention mutation events.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateSensitivityTest.cs` - Added aggregate coverage proving mismatched sensitivity audit evidence fails with `AuditPairingRequired` / `audit_pairing_mismatch` before sensitivity mutation events.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/SetConversationRetentionPolicyCommandHandlerTest.cs` - Added retention handler coverage for audit-service exceptions, closed-state pre-audit rejection, and mismatched returned audit evidence without mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs` - Added sensitivity handler coverage for audit-service exceptions, invalid target pre-audit rejection, compatible duplicate no-op before duplicate audit, and mismatched returned audit evidence without mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added redaction handler coverage proving audit-service exceptions map to fail-closed `audit_unavailable` without mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs` - Added explicit release-gate inventory for implemented governance mutation handlers, aggregate commands, domain mutation events, and operation kinds; future vocabulary remains prepared but unimplemented; review tightened coverage so audited aggregate commands and non-governance command paths must remain explicit.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter FullyQualifiedName~Governance` - completed; no tests currently match this aggregate-project filter.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationAggregateRetentionPolicyTest|FullyQualifiedName~ConversationAggregateSensitivityTest|FullyQualifiedName~ConversationAggregateRedactionTest"` - 31 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter FullyQualifiedName~TenantAccess` - 125 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter FullyQualifiedName~GovernanceAuditPairingSafetyNetTest` - 3 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Governance"` - 128 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - 156 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 556 passed.

## Story 2.4 Redaction Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/RedactionContractTest.cs` - Added redaction command/event/result JSON and content-safety coverage for message and opaque content-segment targets.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs` - Added aggregate redaction success, replay, duplicate/no-op, conflict, audit-pairing, target validation, and no-event rejection coverage.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added tenant/governance authorization-before-load, audit fail-closed, tenant mismatch, idempotency conflict, and successful mutation coverage.
- [x] `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs` - Added public redaction event publication mapping coverage.
- [x] `tests/Hexalith.Conversations.Tests/Idempotency/ConversationCommandFingerprintTest.cs` - Added redaction command fingerprint scope coverage using canonical safe target/policy/rationale/category metadata.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added QA automation follow-up coverage for unsupported schema rejection before tenant/idempotency/load/audit disclosure, stale state-load coarsening before audit, and completed duplicate replay without state load or duplicate audit evidence.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs` - Added QA automation gap coverage proving existing sensitivity marks do not block separately audited redaction intent or mutate replay state before event persistence.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added QA automation gap coverage proving already-sensitive targets still require and use the redaction audit gate before mutation.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/RedactionContractTest.cs` - Added QA automation gap coverage for documented redaction result round trips: success, denied, audit unavailable, policy blocked, unsupported target, already-redacted duplicate, and idempotency conflict.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs` - Added review regression coverage for mismatched audit evidence failing closed before redaction mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added review regression coverage for invalid targets before audit side effects, compatible duplicate no-op before audit, and mismatched audit evidence rejection before mutation.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - 155 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj` - 132 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` - 237 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 533 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-build --no-restore` - 155 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-build --no-restore` - 132 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-build --no-restore` - 237 passed.
- [ ] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore -p:DisableMsCoverageReferencedPathMaps=true` - blocked before test execution because the sandbox denied writing the generated Coverlet source-root mapping file under `tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0`.
- [ ] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` - blocked before test execution because the sandbox denied writing the generated Microsoft CodeCoverage source-root mapping file under `tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0`.
- [ ] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore -p:DisableMsCoverageReferencedPathMaps=true` - blocked before test execution because the sandbox then denied writing the generated Coverlet source-root mapping file under `tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0`.
- [ ] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore -p:OutputPath=...` - blocked before test execution because the sandbox denied creating the alternate output directory.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-build --no-restore` - 155 passed; this validates the existing compiled assembly only and does not compile the new QA follow-up tests.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-build --no-restore` - 132 passed; this validates the existing compiled assembly only and does not compile the new QA follow-up tests.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-build --no-restore` - 237 passed; this validates the existing compiled assembly only and does not compile the new QA follow-up tests.

## Story 1.2 Measure the Oracle's Blind Spots and Backfill Characterization Tests

Story 1.2 is an oracle-strengthening story (no UI / no HTTP API), so the appropriate automated tests are live-decision-code characterization tests added **inside** the conformance oracle, following the project's xUnit v3 + Shouldly idiom (not browser E2E). The dev backfill pinned the five release-gate behaviors; this QA pass enumerated the live decision code's deny/downgrade branches to find any still-unpinned safety path, then auto-applied the gaps. Framework detected and reused: xUnit v3 + Shouldly + `coverlet.collector` on .NET 10 (no new tooling). Suite under test: `tests/Hexalith.Conversations.Conformance.Tests`.

### Discovered Gaps → Applied
`ConversationTenantAccessService` (behavior #1, tenant fail-closed, NFR3 — the dominant invariant) exposes **16 denial reasons**; the existing oracle backfill pinned only **7**. The remaining live deny branches are all release-gate fail-closed concerns per `project-context.md` yet were unpinned in the oracle (a fail-open mutation of any rode green). Eight auto-applied:
- [x] Gap 1 (AC3) — projection **sequence gap** (`health.HasGap`) → `TenantAccessGapDetected` (Dapr at-least-once / out-of-order events fail closed).
- [x] Gap 2 (AC3) — projection **watermark regression** (`health.HasRollback`) → `TenantAccessRolledBack`.
- [x] Gap 3 (AC3) — caller's own role **outside the closed-world set** → `UnmappedRole` (partial Tenants SDK rollout must not widen access).
- [x] Gap 4 (AC3) — non-active **`Unknown` status sentinel** → `UnmappedStatus` (missing status must never read as active, TEN-2).
- [x] Gap 5 (AC3) — **non-canonical stored projection tenant id** → `MalformedProjection`.
- [x] Gap 6 (AC3) — **member key with trim drift** → `TenantProjectionPoisoned` (poisoned/non-Ordinal membership map must not widen access, TEN-3).
- [x] Gap 7 (AC3) — **non-canonical request tenant id** (reserved delimiter) → `MalformedTenant`.
- [x] Gap 8 (AC3) — **caller principal with trim drift** → `MissingCaller`.

### Generated Tests
- [x] `tests/Hexalith.Conversations.Conformance.Tests/LiveTenantFailClosedOracleCharacterizationTest.cs` — extended by 8 tests (6 new `[Theory]` rows on `FailClosedTriggerStates` + 2 new `[Fact]` for malformed tenant id / malformed caller). Each asserts `IsAllowed == false` and the exact current `DenialReason`, so any mutation that flips the branch to allow — or changes its classification — turns the oracle RED.

### Implementation
- No production source under `src/` changed (Story 1.2 behavior-preservation scope). Only the one existing test-only file was extended; no sibling submodule touched; no submodule recursed. Reused `Hexalith.Tenants` types and the existing in-file stub patterns; no new authorization fakes invented.

### Validation
- [x] `dotnet test ... --filter "FullyQualifiedName~LiveTenantFailClosedOracleCharacterizationTest"` — 19 passed (was 11; +8 new).
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — **294 passed, 0 failed, 0 skipped** (was 286; +8 new). All green on `main` (characterization — pins current behavior).
- [x] `git status src/` clean — zero production changes.

### Coverage
- Tenant fail-closed denial reasons pinned in the oracle: **7/16 → 15/16** (the 16th, `None`, is the allow path, pinned by the positive control).
- All five release-gate behaviors remain backfilled; behavior #1 materially strengthened. Behaviors #2–#5 (governance pairing, idempotency, redaction replay, projection freshness) were already pinned across their safety-critical branches — no high-confidence gap found.
- AC2 traceability note: left `docs/release-evidence/oracle-blind-spot-analysis-v1.json` unchanged to preserve the dev's baseline-commit evidence verbatim; if these 8 tests are folded into the official record, add their method names to `behaviors[0].backfillTests` and bump the counts.

## Story 1.3 Decouple the Internal-Coupled Tests That Would Break Under Refactor

Story 1.3 is a **test-and-evidence** story (zero `src/` production changes). The QA pass verified the four re-expressed/conformance test classes run green against the existing xUnit v3 + Shouldly stack, validated them against `checklist.md`, and **auto-applied one discovered coverage gap**. No UI / no new HTTP endpoint in scope, so the appropriate automated tests are command/state/event + public read-service conformance tests, not browser E2E.

### Discovered Gaps → Applied
- [x] Gap 1 (AC2) — AC2 enumerates the degraded projection states the public read surface must fail closed on as `stale/rebuilding/gap/poison/unavailable`. The re-expression's `DegradedProjectionShouldNotExposeTrustBearingDetail` `[Theory]` covered `stale`, `rebuilding`, `unavailable`, and a mixed-tenant poison case but **omitted `gap`**, even though a position gap is observable through the public read surface (downgrades to `Rebuilding` / non-trust-bearing). Added an `[InlineData("gap")]` case (events at positions 1 and 3, none at 2) asserting `Projection == null`, `IsAvailableForTrustBearingActions == false`, `FreshnessState != Current`. Internal gap *reason code* stays plumbing-only; only the fail-closed *read outcome* is re-expressed — assertion strength increased only (AC4-safe).

### Generated / Modified Tests
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConversationProjectionReadSurfaceConformanceTest.cs` — added the `gap` degraded-state read-surface case.
- [x] `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` — register projection re-express entry now lists `(stale/rebuilding/gap/mixed-tenant/unavailable)`; the committed `docs/release-evidence/at-risk-test-register-v1.json` regenerated deterministically and passed the content-safety scan (avoided the forbidden term `poison`).

### Implementation
- No production source under `src/` changed; no sibling submodule touched; no test deleted. Changes are isolated to the conformance test project + the regenerated register artifact.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/...csproj` — **316 passed, 0 failed, 0 skipped** (was 315; +1 gap case). Green on `main`.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/...csproj` — 515 passed (untouched).
- [x] `git status src/` clean — zero production changes.

### Coverage
- AC2 degraded-state read-surface matrix: stale / rebuilding / **gap (new)** / mixed-tenant / unavailable — all fail closed through the public read surface.
- Governance audit-pairing (AC1), idempotency conflict/pending/reason/payload (AC2), and register generation + content-safety (AC5) were already pinned across their release-gate branches — no further high-confidence gap found.

## Story 1.4 Accept the Canonical Consume/Promote/Keep Inventory and Record Baseline Plumbing-LOC

Story 1.4 is a documentation / analysis / evidence story (no UI, no HTTP API, zero `src/`). The only testable artifact is the committed decision-spine inventory `docs/release-evidence/consume-promote-keep-inventory-v1.{json,md}` and its read-only structural validator. The project-appropriate test type is therefore the existing xUnit v3 / Shouldly committed-artifact validation pattern — there is no API or browser E2E surface to script. This run extended the validator (`ConsumePromoteKeepInventoryValidationTest`) from 7 to 15 facts, closing AC-coverage gaps left by the original author. All new facts assert structural / internal-consistency invariants (relationships the artifact claims about itself), never the hand-curated per-area LOC values (per AC5).

### Discovered Gaps → Applied
- [x] Gap 1 (AC5) — only the `.json` was read back; the human-readable `.md` sibling was never asserted committed/non-empty.
- [x] Gap 2 (AC1) — "no source double-counted" was not enforced at the path level (only unique `areaId`).
- [x] Gap 3 (AC1) — `reconciliation.{consume,promote,keep}Subtotal` were never validated against the actual per-classification sums.
- [x] Gap 4 (AC3) — `plumbingBaselinePctOfSource` was never checked against `100 * plumbingBaselineLoc / sourceTotalLoc`.
- [x] Gap 5 (AC3) — `plumbingDerivation.{consume,promote}Rows` were only summed, never verified to enumerate exactly the Consume/Promote areas with matching LOC.
- [x] Gap 6 (AC3) — the addendum ~18,000/~50% first-pass confirm-or-correct verdict (and the `why` on correction) was never asserted.
- [x] Gap 7 (AC4) — the recorded versioning convention and OQ-1/OQ-2/OQ-3 "left open" guardrail were never asserted.
- [x] Gap 8 (AC2) — `fr` / `owningStory` cross-references were checked non-empty but not well-formed (`FR-<n>`, `<epic>.<story>`).

### Generated Tests
- [x] `tests/Hexalith.Conversations.Conformance.Tests/ConsumePromoteKeepInventoryValidationTest.cs` — 8 new [Fact] tests added beside the original 7: `BothJsonAndMarkdownSiblingArtifactsShouldBeCommitted`, `NoSourcePathShouldBeDoubleCountedAcrossAreas`, `ReconciliationPerClassificationSubtotalsShouldBeConsistent`, `RecordedPlumbingPercentageShouldMatchTheComputedRatio`, `PlumbingDerivationRowsShouldEnumerateExactlyTheConsumeAndPromoteAreas`, `AddendumFirstPassShouldBeExplicitlyConfirmedOrCorrected`, `InventoryShouldRecordVersioningConventionAndLeaveOpenQuestionsOpen`, `ConsumePromoteCrossReferencesShouldUseWellFormedFrAndStoryIdentifiers`.

### Implementation
- No production source under `src/` changed (gate-zero, behavior-preservation scope). Only the one existing test-only file was extended; the committed inventory artifact is read read-only. No sibling submodule touched or recursed.

### Validation
- [x] `dotnet test ... --filter "FullyQualifiedName~ConsumePromoteKeepInventoryValidationTest"` — 15 passed, 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj` — 331 passed, 0 failed, 0 skipped (was 323; +8 new).

### Coverage
- AC1 (single classification, full reconciliation, no double-count): original facts + Gaps 2, 3.
- AC2 (capability / FR / owning-story cross-reference): original fact + Gap 8.
- AC3 (plumbing baseline derivation + addendum confirm/correct): original fact + Gaps 4, 5, 6.
- AC4 (accepted marker, FR-2 governance, versioning, OQ open): original fact + Gap 7.
- AC5 (committed `.json` + `.md`, reproducible, scope-clean, content-safe): original facts + Gap 1.
- Validator facts: 7 → 15.

### Known pre-existing issue (NOT introduced by this story — out of scope)
- `ReleaseBaselineValidationTest.BaselineReportedTypeCountShouldAgreeWithTheCommittedSnapshotAndLiveSurface` (Story 1.1's artifact validator) intermittently fails in the full-suite run with a JSON torn-read (`Expected end of string … reached end of data`) but passes in isolation. Root cause is a test-isolation race: `PublicContractShapeSnapshotGenerationTest` rewrites `public-contract-shape-baseline-v1.json` while `ReleaseBaselineValidationTest` reads the same file concurrently under xUnit v3 parallelism. Story 1.4's validator reads a file no generator writes, so it is not part of this race and did not introduce it. Recommend fixing separately (shared test collection to serialize the generate/read pair, or a torn-read retry). Did not reproduce in the final run above (331 passed).
