---
baseline_commit: 5310494
---

# Story 3.3: Promote & adopt the diagnostics/telemetry scaffolding helper

Status: done
<!-- Senior Developer Review (AI) 2026-06-08 (second pass): implementation + all gates re-verified green here
     (Commons.Diagnostics 16, Server 582, Conformance 361 ≥360, Release 0/0). The prior pass's sole blocker —
     orchestrator-owned Commons submodule commit + push + root gitlink bump (AC-6 / Task 7) — is now RESOLVED:
     Commons committed at 17820f8, pushed to origin/main (ls-remote verified), worktree clean; root gitlink bumped
     in the umbrella working tree (unstaged, to land in the orchestrator's umbrella commit) — the exact state in
     which Story 3.2 was marked done. 0 CRITICAL after fixes → Status set to done. -->

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a technical-module maintainer,
I want the meter/counter/classifier scaffolding promoted into a shared helper and adopted by Conversations,
so that a domain module supplies only metric names and bounded dimension vocabularies.

## Acceptance Criteria

**AC-1 - Resolve and promote the FR-15 telemetry helper in the ratified technical module.**
Given the landing zone for FR-15 is resolved under Epic 3 OQ-1,
When the repeated `IMeterFactory`/`Meter.CreateCounter`/bounded-tag/None-sentinel/logging scaffolding is promoted with module-owned tests,
Then the helper lives in the chosen technical module, is additive/backward-compatible, and lets a domain module provide only meter name, counter names, descriptions, bounded dimension keys, enum token vocabularies, and safe log message templates.
[Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 3.3]

**AC-2 - Adopt the helper in Conversations without changing observable telemetry.**
Given the promoted helper,
When `ConversationProjectionTelemetry`, `ConversationRejectionTelemetry`, and `ConversationConformanceTelemetry` adopt it,
Then the hand-written metric construction and repeated lowercasing/tag validation scaffolding is deleted or reduced to thin wrappers,
And the Conversations-owned interfaces, domain metric names, dimension keys, classifier enums, and safe log event text remain stable unless every conformance guard is deliberately updated.
[Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetry.cs]

**AC-3 - Observability continuity and disclosure safety are preserved.**
Given existing dashboards and alert rules depend on the current metric contract,
When every projection, publication, rejection, tenant-denial, privileged-access, and conformance outcome signal is emitted after adoption,
Then the meter remains `Hexalith.Conversations`, counter names remain unchanged, approved dimension key sets remain unchanged, boolean values remain `"true"`/`"false"`, enum tokens remain the existing lowercase invariant form, no tenant/conversation/Party/content/provider/raw fault value is emitted as a metric dimension or log field, and the `TelemetryRedactionConformanceSuite` and `TelemetryCardinalityConformanceSuite` remain green.
[Source: tests/Hexalith.Conversations.Conformance.Tests/TelemetryDisclosureConformanceFixtures.cs; tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuite.cs; tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuite.cs]

**AC-4 - Classifier behavior remains domain-owned and unchanged.**
Given the helper is generic telemetry scaffolding, not Conversations policy,
When command rejection, tenant denial, projection freshness, publication failure, and conformance status inputs are classified,
Then existing classifier mappings stay behavior-identical and remain in Conversations unless a neutral helper can express only the mechanical enum-to-token formatting without importing Conversations contracts.
[Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClassifier.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceStatusClassifier.cs]

**AC-5 - Story 1.3 telemetry/status retarget obligation is closed honestly.**
Given Story 1.3 marked telemetry/cardinality/redaction/conformance-status tests as `coupled-by-design-retarget-in-owning-story` for Story 3.3,
When the telemetry helper is adopted,
Then the coupled tests are retargeted to the promoted helper and/or thin Conversations wrappers without losing assertion strength,
And the `Hexalith.Conversations.Conformance.Tests -> Hexalith.Conversations.Server` project-reference disposition is updated: remove the reference if no release-gate behavior still requires live Server types; otherwise record the exact remaining live coupling in the FR-20 ledger and do not claim the structural dependency is closed.
[Source: docs/release-evidence/at-risk-test-register-v1.md#Project-reference-disposition]

**AC-6 - NFR6, conformance, and submodule mechanics hold.**
And dependent sibling modules compile green against the promoted API, the full Conversations conformance suite is monotonic at **>= 360**, the public-contract-shape baseline diff is empty, and any technical-module promotion is committed as a separate submodule commit plus a root-level gitlink pointer bump. Never initialize nested submodules recursively.
[Source: docs/release-evidence/promote-adopt-runbook.md; _bmad-output/implementation-artifacts/3-2-promote-adopt-the-generic-tenant-access-projection-handler-registration.md]

## Tasks / Subtasks

- [x] **Task 0 - Resolve and record the FR-15 landing zone before code edits.** (AC: 1, 6)
  - [x] **RATIFIED by user (2026-06-08, story-automator — "Commons, all Epic-3"):** landing zone = new **`Hexalith.Commons.Diagnostics`** library in the `Hexalith.Commons` submodule (self-contained `Directory.Build.props`, mirror 3.1/3.2). Keep Conversations telemetry registration as a THIN FACADE delegating to the shared helper; do NOT re-open this OQ. Commit the new lib on Commons `main` + push + bump root gitlink to Commons `main` HEAD ("use main for Hexalith.Commons"; orchestrator does the push). Rationale: Commons is the right altitude for domain-agnostic metric scaffolding; EventStore's `EventStoreDomainTelemetry` only owns domain instrument naming, Tenants/FrontComposer/Parties telemetry is module-specific.
  - [x] Add the chosen landing-zone decision to `docs/release-evidence/promote-adopt-runbook.md`. (Added the "Ratified for Story 3.3 (FR-15)" bullet in section 0 during AI review.)
  - [x] If using Commons, mirror Story 3.1/3.2 self-contained `Directory.Build.props` so the library builds from the umbrella without initializing Commons' nested `Hexalith.Builds`. (`Hexalith.Commons.Diagnostics/Directory.Build.props` is self-contained; full umbrella Release build is 0/0.)

- [x] **Task 1 - Characterize the current Conversations telemetry contract before replacement.** (AC: 2, 3, 4, 5)
  - [x] Pin the exact current meter/counter/dimension contract in tests or a small data-driven manifest:
    - Meter: `Hexalith.Conversations`.
    - Counters: `conversations.command.rejections`, `conversations.tenant.denials`, `conversations.privileged.access`, `conversations.projection.freshness`, `conversations.projection.rebuild`, `conversations.publication.failures`, `conversations.conformance.outcomes`.
    - Dimension key sets: `rejection_class/operation_class/retryable`, `denial_class/operation_class/retryable`, `access_class/operation_class`, `freshness_class/lag_class`, `rebuild_class`, `failure_class`, `status_class/gate_id/blocking`. (Pinned by the new `ConversationTelemetryContractTest` + `ConversationTelemetryDefinitions` manifest.)
  - [x] Preserve existing log event names: `ConversationCommandRejected`, `ConversationTenantDenied`, `ConversationPrivilegedAccess`, `ConversationProjectionFreshness`, `ConversationProjectionRebuild`, `ConversationPublicationFailure`, `ConversationConformanceOutcome`. (Log templates unchanged in all three wrappers.)
  - [x] Keep the `None` sentinel rejection behavior for every enum class that currently throws. (Now enforced by `BoundedMetricDimension.EnumToken` throwing on the `None` token; preserved by the `*_NoneClass_ThrowsArgumentException` tests.)

- [x] **Task 2 - Promote the shared helper with module-owned tests.** (AC: 1, 3, 6)
  - [x] Build a helper around `IMeterFactory`/`Meter`/`Counter<long>` that centralizes counter creation, enum-token formatting, fixed dimension emission, boolean token emission, `None` sentinel validation, and optional content-safe logging support.
  - [x] Do not require OpenTelemetry package references for the helper if `System.Diagnostics.Metrics`, `Microsoft.Extensions.Diagnostics.Abstractions`, and `Microsoft.Extensions.Logging.Abstractions` are sufficient. (Helper csproj references only `Microsoft.Extensions.Diagnostics.Abstractions` + `Microsoft.Extensions.Logging.Abstractions`; no OpenTelemetry dependency.)
  - [x] Prefer allocation-conscious `TagList` or equivalent where a signal emits more than one tag; keep behavior identical before optimizing. (`BoundedTelemetryCounter` builds a `TagList`.)
  - [x] Add helper tests for constructor null guards, counter creation, dimension key order/content, lowercase invariant enum token formatting, boolean token formatting, sentinel rejection, no accidental free-text dimension values, and registration/factory reuse. (`BoundedTelemetryCounterTest` + `BoundedTelemetryHelperTest`, 16 cases.)
  - [x] Add a generic test fixture equivalent to `FakeMeterFactory`/`MeterListener` only if it belongs in the shared module; otherwise keep Conversations test helpers local. (Kept `FakeMeterFactory` local — private nested types in both the Commons and Conversations test files.)

- [x] **Task 3 - Adopt the helper in Conversations and delete duplicated scaffolding.** (AC: 2, 3, 4)
  - [x] Wire the shared helper from source using the existing `HexalithCommonsRoot` property pattern and guarded `ProjectReference`; update `Directory.Build.props` detection if it currently requires only the HTTP and TenantAccess libraries. (Added a `Hexalith.Commons.Diagnostics` existence check to both `HexalithCommonsRoot` conditions and a guarded `ProjectReference` pair in `Hexalith.Conversations.Server.csproj`.)
  - [x] Refactor `ConversationProjectionTelemetry`, `ConversationRejectionTelemetry`, and `ConversationConformanceTelemetry` into thin wrappers over the helper. Preserve their public interfaces and DI registration extensions unless every caller/test is deliberately retargeted. (Public interfaces + DI extensions unchanged.)
  - [x] Delete duplicated inline meter/counter/tag creation code from Conversations, not Conversations-owned names, enums, interfaces, or policy classifiers. (Removed inline `Meter.Create`/`CreateCounter<long>`/`KeyValuePair` tag construction + per-method `None`-checks; classifiers untouched.)
  - [x] Keep `ConversationOnboardingDiagnosticsService` and `Contracts/Diagnostics` domain contract vocabulary in scope only if the helper provides mechanical scaffolding they can use without reshaping public contracts. Do not move public diagnostics contracts into Commons. (No public diagnostics contract moved; contract-shape diff empty.)

- [x] **Task 4 - Retarget tests without weakening telemetry release gates.** (AC: 3, 4, 5)
  - [x] Update `ConversationProjectionTelemetryTest`, `ConversationRejectionTelemetryTest`, and `ConversationConformanceTelemetryTest` so they prove the thin wrappers still emit the exact current metric contract and safe logs. (Existing emission/redaction assertions kept; stable-event-name log tests added; new `ConversationTelemetryContractTest` + `ConversationTelemetryGuardsTest` pin contract + guards.)
  - [x] Update classifier tests only if implementation moved; preserve all mapping assertions exactly. (Classifiers did not move → classifier tests unchanged.)
  - [x] Retarget `TelemetryRedactionConformanceSuite`, `TelemetryCardinalityConformanceSuite`, and `ConformanceStatusConformanceSuite` so they no longer fail for moved internal helper types. Keep high-cardinality load, forbidden-value scan, fixed enum budgets, approved gate-id vocabulary, and exact dimension-key assertions. (Wrappers keep their public surface, so these suites required no edit and stay green — verified, not weakened.)
  - [x] Add/update `story33StructuralDispositions` in the FR-20 ledger if any conformance/server reference, test premise, or file-level assertion changes. Do not silently remove telemetry/status tests. (Added `story33StructuralDispositions` + refreshed the project-reference disposition rationale in `at-risk-test-register-v1.json` and `AtRiskTestRegisterGenerationTest`.)

- [x] **Task 5 - Preserve integration and registration behavior.** (AC: 2, 3, 6)
  - [x] Keep `AddConversationProjectionTelemetry()`, `AddConversationRejectionTelemetry()`, and `AddConversationConformanceTelemetry()` resolving the same service interfaces as singletons unless a full public/internal rename is deliberately propagated. (DI registration extensions unchanged; `AddConversationRejectionTelemetry_RegistersServiceCorrectly` green.)
  - [x] Keep `services.AddMetrics()` as the host/test source of `IMeterFactory`; do not create ad hoc static meters in the server emitters. (Wrappers take `IMeterFactory` via ctor; `BoundedTelemetryMeter` calls `meterFactory.Create`; no static meters.)
  - [x] Verify no new metric dimension contains tenant id, conversation id, Party id, provider payload, redacted content, raw exception text, command body, event payload, or raw gate labels outside the approved set. (Redaction/cardinality conformance suites green; emitters emit only bounded enum/boolean/approved-gate tokens.)

- [x] **Task 6 - Prove release gates and sibling compatibility.** (AC: 3, 5, 6)
  - [x] Run the promoted helper module tests. (`Hexalith.Commons.Diagnostics.Tests` — 16 passed, 0 failed.)
  - [x] Run `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Release`. (582 passed, 0 failed.)
  - [x] Run `dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj -c Release`; required count is `>= 360`. (361 passed, 0 failed — monotonic ≥ 360.)
  - [x] Run the full Release build with warnings as errors and verify the public-contract-shape diff is empty. (Full `Hexalith.Conversations.slnx` Release build: 0 warnings / 0 errors; contract-shape baseline test green within the 361 conformance run.)
  - [x] Build dependent siblings that may consume shared diagnostics/telemetry helpers or compile against Conversations, especially Projects/Folders/Tenants/Parties/EventStore/FrontComposer, against the promoted API. (EventStore + Tenants projects build green in the umbrella; the promoted `Hexalith.Commons.Diagnostics` API is purely additive and no sibling consumes it yet, so additive sibling compatibility holds — same disposition recorded for 3.1/3.2.)

- [x] **Task 7 - Submodule commit, pointer bump, and final record.** (AC: 6) — **ORCHESTRATOR-OWNED, DONE.**
  - [x] Verify root submodule gitlinks before build; do not use recursive submodule commands. (Build verified against the local Commons working tree; no recursive submodule commands used.)
  - [x] **DONE (orchestrator, 2026-06-08; re-verified in second review pass):** committed the `Hexalith.Commons.Diagnostics` lib + test project + Commons `Directory.Packages.props` on Commons `main` (`feaf007..17820f8`), pushed to `origin/main` (VERIFIED via `ls-remote`: remote main HEAD = `17820f8`), Commons worktree clean. Root gitlink bumped `feaf007 → 17820f8` (= Commons `origin/main`) in the umbrella working tree (unstaged ` M Hexalith.Commons`, to land in the orchestrator's umbrella commit — mirrors Story 3.2). **AC-6 submodule mechanics SATISFIED — a clean checkout at the bumped pointer builds.** ("use main for Hexalith.Commons".)
  - [x] Generate the Dev Agent Record last to avoid count/file-list drift. (Completed during this AI review, after all gates were green.)

## Dev Notes

### Current implementation to read before editing

`ConversationProjectionTelemetry` owns three counters on meter `Hexalith.Conversations`: `conversations.projection.freshness` tagged by `freshness_class` and `lag_class`, `conversations.projection.rebuild` tagged by `rebuild_class`, and `conversations.publication.failures` tagged by `failure_class`. It rejects `None` classes and logs only bounded class names plus correlation id. [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs]

`ConversationRejectionTelemetry` owns three counters on the same meter: `conversations.command.rejections` tagged by `rejection_class`, `operation_class`, `retryable`; `conversations.tenant.denials` tagged by `denial_class`, `operation_class`, `retryable`; and `conversations.privileged.access` tagged by `access_class`, `operation_class`. It rejects `None` classes and must not emit tenant, conversation, Party, provider, or payload values as dimensions/log fields. [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs]

`ConversationConformanceTelemetry` owns `conversations.conformance.outcomes` tagged by `status_class`, `gate_id`, and `blocking`. `gate_id` is the only approved non-enum string dimension and is constrained by `TelemetryDisclosureConformanceFixtures.ApprovedGateIds`. [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetry.cs; tests/Hexalith.Conversations.Conformance.Tests/TelemetryDisclosureConformanceFixtures.cs]

The classifier files are domain policy, not generic scaffolding. They map Conversations error codes, tenant denial reasons, projection trust states, freshness reason codes, publication errors, and conformance outcomes into bounded Conversations enums. Preserve this behavior unless every test is retargeted with equal or stronger assertions. [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationCommandRejectionClassifier.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceStatusClassifier.cs; src/Hexalith.Conversations.Server/Diagnostics/ConversationPublicationFailureClassifier.cs]

`Contracts/Diagnostics` contains public onboarding/precondition result contracts. Do not move or reshape these public contract types as part of a generic telemetry helper unless an explicit contract-shape change is approved. The standing gate expects the public-contract-shape diff to be empty. [Source: src/Hexalith.Conversations.Contracts/Diagnostics; _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-20]

### Metric contract to preserve

| Signal | Counter | Dimensions |
|---|---|---|
| Command rejection | `conversations.command.rejections` | `rejection_class`, `operation_class`, `retryable` |
| Tenant denial | `conversations.tenant.denials` | `denial_class`, `operation_class`, `retryable` |
| Privileged access | `conversations.privileged.access` | `access_class`, `operation_class` |
| Projection freshness | `conversations.projection.freshness` | `freshness_class`, `lag_class` |
| Projection rebuild | `conversations.projection.rebuild` | `rebuild_class` |
| Publication failure | `conversations.publication.failures` | `failure_class` |
| Conformance outcome | `conversations.conformance.outcomes` | `status_class`, `gate_id`, `blocking` |

Every enum token currently uses `ToString().ToLowerInvariant()`, which produces existing tokens such as `withinthreshold`, `thresholdbreached`, `insufficientaccess`, and `infrastructurefailure`. Do not switch to snake_case or kebab-case in this story; that would break dashboards and conformance fixtures. [Source: tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionTelemetryTest.cs; tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationRejectionTelemetryTest.cs; tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationConformanceTelemetryTest.cs]

### At-risk test and FR-20 obligations

Story 1.3 explicitly left telemetry/status suites coupled to `Server.Diagnostics` until Story 3.3. The owning files include `TelemetryCardinalityConformanceSuite*`, `TelemetryRedactionConformanceSuite*`, `TelemetryDisclosureConformanceFixtures`, `TelemetryValidationTestHelpers`, `ConformanceStatusConformanceSuite*`, and `ConformanceStatusConformanceFixtures`. Retarget these tests with the helper adoption; do not delete them as "plumbing-only" because they assert release-gate observability continuity, disclosure safety, and bounded cardinality. [Source: docs/release-evidence/at-risk-test-register-v1.md]

The conformance project reference to `Hexalith.Conversations.Server` is the last structural coupling named in the Story 1.3 register after Stories 2.2, 2.5, and 3.2. Try to remove it after telemetry/status retargeting. If a real live-server dependency remains, record the exact remaining reason in the ledger and sprint notes so Story 5.1/5.2 can reconcile it honestly. [Source: docs/release-evidence/at-risk-test-register-v1.md#Project-reference-disposition]

### Sibling and shared-module intelligence

There is no existing `Hexalith.Commons` telemetry helper. Current Commons libraries are `Hexalith.Commons.Http`, `Hexalith.Commons.TenantAccess`, `Configurations`, `Metadatas`, `StringEncoders`, `UniqueIds`, and core Commons. [Source: Hexalith.Commons/src/libraries]

`Hexalith.EventStore.DomainService.EventStoreDomainTelemetry` provides domain ActivitySource/Meter naming conventions and OpenTelemetry registration hooks. It does not provide reusable bounded counter/tag/classifier scaffolding, but it is useful prior art for validating a domain name, centralizing instrument names, and registering meters with OpenTelemetry. [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetry.cs]

`Hexalith.Tenants.Telemetry.TenantTelemetry` and `Hexalith.FrontComposer.Shell.Infrastructure.Telemetry.FrontComposerTelemetry` are module-specific examples of bounded tags and sanitization, not reusable FR-15 helpers. Use them as cautionary evidence for cardinality and safe-token handling, not as copy-paste sources. [Source: Hexalith.Tenants/src/Hexalith.Tenants/Telemetry/TenantTelemetry.cs; Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerTelemetry.cs]

### Previous story intelligence

Story 3.1 proved the Epic 3 runbook: resolve OQ-1, promote into Commons with self-contained props, test the helper in-module, adopt from source, delete hand-rolled logic while preserving thin facades when needed, run conformance, build siblings, then commit the submodule and bump the root gitlink. Story 3.2 repeated that pattern for tenant access and left conformance at 360 passing in review evidence. Treat `>= 360` as the minimum monotonic conformance gate for this story. [Source: docs/release-evidence/promote-adopt-runbook.md; _bmad-output/implementation-artifacts/3-1-promote-adopt-the-generic-typed-httpclient-registration.md; _bmad-output/implementation-artifacts/3-2-promote-adopt-the-generic-tenant-access-projection-handler-registration.md]

Story 3.2 also reinforced the deletion-vs-facade rule: delete duplicated mechanics, not domain vocabulary. Apply the same rule here. The Conversations telemetry interfaces and metric names are the consumer-facing/adopter-facing shape; the helper should remove the repeated mechanics behind them. [Source: _bmad-output/implementation-artifacts/3-2-promote-adopt-the-generic-tenant-access-projection-handler-registration.md]

### Architecture and product guardrails

Observability dimensions must not include raw conversation content, provider payloads, redacted text, raw exception text, or unbounded identifiers. Logs, traces, metrics, diagnostic dumps, conformance artifacts, screenshots, and release evidence are disclosure surfaces. [Source: _bmad-output/planning-artifacts/architecture.md#Observability]

Projection freshness, trust, diagnostics, and evidence vocabulary must stay consistent across API/admin/diagnostics/conformance. Do not create a second set of names that diverges from existing Conversations classes. [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]

Runtime configuration lives under `Server/Configuration`, `AppHost`, and `ServiceDefaults`; `ServiceDefaults` owns OpenTelemetry, health, discovery, and resilience defaults. This story is about emitter scaffolding, not replacing ServiceDefaults. Shared ServiceDefaults is Story 3.4. [Source: _bmad-output/planning-artifacts/architecture.md#Development Workflow Integration]

### Testing requirements

Minimum test set:

- Promoted helper module tests for counter creation, dimension tags, enum-token formatting, boolean formatting, sentinel validation, and safe logging hooks.
- Conversations server tests proving each thin wrapper emits the exact same metric names, meter name, dimensions, values, and safe logs as before.
- Classifier tests preserved green for command rejection, tenant denial, projection freshness/lag, publication failure, and conformance status.
- Telemetry redaction and cardinality conformance suites green and not weakened.
- FR-20 ledger/project-reference disposition updated for any retargeted or remaining Server coupling.
- Full conformance suite `>= 360`, Release build 0 warnings, and public-contract-shape diff empty.

### Latest technical specifics

Do not upgrade package versions as part of this story. The local root and sibling modules already pin OpenTelemetry `1.15.3` family packages and target .NET `10.0`; `Microsoft.Extensions.Diagnostics.Abstractions` current stable NuGet version is `10.0.8` and supports `net10.0`, while the repo currently relies mostly on shared framework/package pins. Add package references only through Central Package Management and only if the helper cannot compile against existing references. [Source: Directory.Packages.props; NuGet: https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.Abstractions/10.0.8; https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/1.15.3]

Microsoft's .NET metrics guidance uses `System.Diagnostics.Metrics`, notes that hosts register `IMeterFactory` via `AddMetrics()` on modern .NET, and recommends efficient tag APIs such as `TagList` when emitting larger tag sets. Preserve the existing `IMeterFactory` pattern; do not introduce static global meters in Conversations emitters. [Source: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation]

### Project Structure Notes

- Likely new shared code: `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/` or `Hexalith.Commons/src/libraries/Hexalith.Commons.Telemetry/`, plus tests under `Hexalith.Commons/test/<chosen>.Tests/`.
- Conversations adoption files likely touched: `Directory.Build.props`, `Hexalith.Conversations.slnx`, `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`, `src/Hexalith.Conversations.Server/Diagnostics/*`, `tests/Hexalith.Conversations.Server.Tests/Diagnostics/*`, and telemetry/status conformance tests.
- Do not edit generated files or `obj/`.
- Keep package versions out of `.csproj` files; use Central Package Management.
- Do not initialize nested submodules or use recursive submodule update commands.

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Epic 3]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 3.3]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-15]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#FR-20]
- [Source: docs/release-evidence/consume-promote-keep-inventory-v1.json#diagnostics-telemetry-scaffolding]
- [Source: docs/release-evidence/at-risk-test-register-v1.md]
- [Source: docs/release-evidence/promote-adopt-runbook.md]
- [Source: _bmad-output/implementation-artifacts/3-1-promote-adopt-the-generic-typed-httpclient-registration.md]
- [Source: _bmad-output/implementation-artifacts/3-2-promote-adopt-the-generic-tenant-access-projection-handler-registration.md]
- [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs]
- [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs]
- [Source: src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetry.cs]
- [Source: tests/Hexalith.Conversations.Conformance.Tests/TelemetryDisclosureConformanceFixtures.cs]
- [Source: tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuite.cs]
- [Source: tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuite.cs]

## Dev Agent Record

### Agent Model Used

- create-story + dev-story: Codex (GPT-5)
- test-automation (automate) + senior developer review: Claude (Opus 4.8, 1M context)

### Debug Log References

- 2026-06-08: BMAD create-story workflow resolved with no activation prepend/append steps; persistent project context loaded from `_bmad-output/project-context.md`.
- 2026-06-08: Story 3.3 selected explicitly by user; sprint status showed `3-3-promote-adopt-the-diagnostics-telemetry-scaffolding-helper: backlog` and `epic-3: in-progress`.
- 2026-06-08: Input discovery loaded PRD/epics, architecture, project context, Story 3.2, runbook, inventory, at-risk test register, current diagnostics source/tests, sibling telemetry examples, and current package/version context.
- 2026-06-08 (dev, codex): promoted `Hexalith.Commons.Diagnostics` + adopted the three Conversations telemetry wrappers + `ConversationTelemetryDefinitions`; codex hit a context limit mid-wrap-up (task boxes left unchecked, Commons lib left uncommitted) but the implementation was complete and `dotnet build Hexalith.Conversations.slnx -c Release` was 0/0.
- 2026-06-08 (automate, claude): added `BoundedTelemetryHelperTest` (10 cases) and `ConversationTelemetryGuardsTest` (7 cases); fixed analyzer-as-error findings (IDE0004 redundant cast / S3878) in the new helper; Server.Tests 582, Conformance 361, Release clean.
- 2026-06-08 (review, claude): adversarial review — verified metric/log contract continuity, redaction/cardinality safety, AC-5 reference disposition, and all release gates; completed the story records below. Confirmed the sole outstanding item is the orchestrator's Commons submodule commit + push + root gitlink bump.

### Completion Notes List

- Story context generated by BMAD create-story workflow on 2026-06-08; validated against `.claude/skills/bmad-create-story/checklist.md`.
- FR-15 landing zone RATIFIED = new `Hexalith.Commons.Diagnostics` library (self-contained `Directory.Build.props`), recorded in the promote-adopt runbook.
- Promoted helper: `BoundedTelemetryCounterDefinition`, `BoundedTelemetryMeter`, `BoundedTelemetryCounter` (1/2/3/`params` `AddOne` overloads with declared key-order + count validation), `BoundedMetricDimension` (`EnumToken` lowercase-invariant + `None`-reject, `BooleanToken`, control-char-guarded `SafeToken`), and `BoundedTelemetryLog`. Depends only on `Microsoft.Extensions.Diagnostics.Abstractions` (added to Commons CPM at 10.0.8) + `Microsoft.Extensions.Logging.Abstractions`; no OpenTelemetry reference.
- Adoption: the three Conversations emitters became thin wrappers; meter `Hexalith.Conversations`, all seven counter names, exact dimension-key sets/order, lowercase-invariant enum tokens, `"true"`/`"false"` booleans, safe log event names, and `None`-sentinel rejection are preserved. Public interfaces, DI registration extensions, classifiers, and public diagnostics contracts are unchanged; public-contract-shape diff empty.
- AC-5 honesty: `Conformance.Tests -> Server` project reference deliberately retained; the exact remaining live coupling (tenant access, projection, command-handler, governance, diagnostics-classifier types) is recorded in `projectReferenceDisposition` and `story33StructuralDispositions`. No telemetry/status test silently removed or weakened.
- Gates: Commons.Diagnostics.Tests 16/16; Server.Tests 582/582; Conformance 361/361 (monotonic ≥ 360); full umbrella Release build 0 warnings / 0 errors.
- **RESOLVED (AC-6 / Task 7, orchestrator-owned) — second review pass 2026-06-08:** the `Hexalith.Commons.Diagnostics` library + its test project + the Commons `Directory.Packages.props` edit are now COMMITTED on Commons `main` at `17820f8` and PUSHED to `origin/main` (verified: Commons HEAD = `17820f8`, `git ls-remote origin main` = `17820f8`, worktree clean). The root `Hexalith.Commons` gitlink is bumped `feaf007 → 17820f8` in the umbrella working tree (unstaged ` M Hexalith.Commons`, to land in the orchestrator's umbrella commit). A clean checkout at the bumped pointer builds against the published Commons commit — AC-6 submodule mechanics satisfied, mirroring the state in which Story 3.2 was marked done.

### File List

**Promoted helper — `Hexalith.Commons` submodule (committed + pushed: `origin/main` at `17820f8`):**
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/Hexalith.Commons.Diagnostics.csproj`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/Directory.Build.props`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryCounterDefinition.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryMeter.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryCounter.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedMetricDimension.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Diagnostics/BoundedTelemetryLog.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Diagnostics.Tests/Hexalith.Commons.Diagnostics.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.Diagnostics.Tests/BoundedTelemetryCounterTest.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Diagnostics.Tests/BoundedTelemetryHelperTest.cs`
- `Hexalith.Commons/Directory.Packages.props` (added `Microsoft.Extensions.Diagnostics.Abstractions` 10.0.8)

**Conversations adoption — umbrella repo:**
- `Directory.Build.props` (`HexalithCommonsRoot` now also requires `Hexalith.Commons.Diagnostics`)
- `Hexalith.Conversations.slnx` (added Diagnostics library + its test project)
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` (guarded `ProjectReference` to the helper)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationTelemetryDefinitions.cs` (new — metric-contract manifest)
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionTelemetry.cs`
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationRejectionTelemetry.cs`
- `src/Hexalith.Conversations.Server/Diagnostics/ConversationConformanceTelemetry.cs`
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationTelemetryContractTest.cs` (new)
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationTelemetryGuardsTest.cs` (new)
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationProjectionTelemetryTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Diagnostics/ConversationRejectionTelemetryTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs`
- `docs/release-evidence/at-risk-test-register-v1.json` (FR-20 ledger: project-reference disposition + `story33StructuralDispositions`)
- `docs/release-evidence/promote-adopt-runbook.md` (Story 3.3 landing-zone entry — added during AI review)

**Process artifacts:**
- `_bmad-output/implementation-artifacts/3-3-promote-adopt-the-diagnostics-telemetry-scaffolding-helper.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260602-180057.md`

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot (AI-assisted, Claude Opus 4.8) · **Date:** 2026-06-08 · **Mode:** autonomous story-automator review (auto-fix)

**Outcome: Changes Requested (one blocking item, orchestrator-owned).** Implementation is correct and every release gate is green; the story cannot be marked *done* until the Commons submodule promotion is committed/pushed and the root gitlink is bumped (AC-6 / Task 7).

### Verified against the story claims
- **AC-1/AC-2/AC-4** — helper promoted into `Hexalith.Commons.Diagnostics`; the three emitters are thin wrappers; meter name, seven counter names, dimension keys/order, lowercase-invariant enum tokens, boolean tokens, safe log event names, and `None`-sentinel rejection are byte-for-behavior preserved (confirmed by reading the diffs and `ConversationTelemetryContractTest`). Classifiers were not moved and were not touched.
- **AC-3** — redaction & cardinality conformance suites stay green and unweakened; emitters emit only bounded enum/boolean/approved-gate tokens + a caller-supplied correlation id. `gate_id` validation is, if anything, *strengthened* (now also rejects control characters) — not a regression.
- **AC-5** — `Conformance.Tests -> Server` reference honestly retained with the exact remaining live coupling recorded in the FR-20 ledger; no telemetry/status test removed.
- **AC-6 gates** — Commons.Diagnostics.Tests 16/16, Server.Tests 582/582, Conformance 361/361 (≥360), full Release 0/0, contract-shape diff empty.

### Findings & disposition
1. **[CRITICAL — blocks done, orchestrator-owned] Commons promotion uncommitted + root gitlink not bumped.** The helper library, its test project, and the Commons `Directory.Packages.props` change are untracked/modified in the `Hexalith.Commons` submodule; the root gitlink still points at `feaf007` (no `Hexalith.Commons.Diagnostics`). A clean checkout at the recorded pointer would fail to build. *Not auto-fixed:* committing/pushing a submodule is the orchestrator's recorded step ("orchestrator does the push", as for 3.2). Left as the single remaining obligation.
2. **[HIGH — fixed] Tasks/Subtasks were all unchecked despite verified completion.** Checked off Tasks 0–6 with per-item evidence; left Task 7's commit/bump unchecked (honest).
3. **[MEDIUM — fixed] Dev Agent Record File List listed only 2 files** versus ~14 source/test/wiring files + 11 Commons files actually changed. Replaced with the full grouped list.
4. **[MEDIUM — fixed] Dev Agent Record was create-story content only** (no dev/automate/review notes, no test evidence). Updated Agent Model, Debug Log, and Completion Notes.
5. **[LOW — fixed] Task 0 runbook sub-item not done.** Added the "Ratified for Story 3.3 (FR-15)" landing-zone bullet to `promote-adopt-runbook.md`.
6. **[INFO — verified non-issue]** Adding `Hexalith.Commons.Diagnostics.Tests` to the umbrella `.slnx` does **not** break the build: its `test/Directory.Build.props` resolves to the umbrella props, the helper library's own self-contained props keep it off the uninitialized nested `Hexalith.Builds`, and the full Release build is 0/0.
7. **[INFO — verified non-issue]** `EnumToken` now also `None`-checks `operation_class`; harmless because `ConversationTenantAccessRequirement` has no `None` member, so emitted tokens are unchanged.

### Senior Developer Review (AI) — second pass (supersedes the v0.4 "Changes Requested" outcome)

**Reviewer:** Jerome Piquot (autonomous story-automator review, auto-fix) · **Date:** 2026-06-08 · **Verdict:** Approve — 0 CRITICAL; status → done.

**Independent re-verification (all gates re-run here, not trusted from prior notes):**

- Full `Hexalith.Conversations.slnx` Release build (warnings-as-errors): **0 warnings / 0 errors**.
- `Hexalith.Commons.Diagnostics.Tests`: **16 passed / 0 failed**.
- `Hexalith.Conversations.Server.Tests`: **582 passed / 0 failed**.
- `Hexalith.Conversations.Conformance.Tests`: **361 passed / 0 failed** — `>= 360` monotonic gate met; `Contracts` unchanged ⇒ contract-shape diff empty.

**Promotion faithfulness confirmed by reading every helper + wrapper + the diffs (not just the story claims):** the three emitters are genuinely thin wrappers over `Hexalith.Commons.Diagnostics`; meter `Hexalith.Conversations`, all seven counter names, exact dimension-key sets/order, **counter descriptions**, lowercase-invariant enum tokens (`ToString().ToLower(InvariantCulture)`), `"true"`/`"false"` booleans, the seven safe log event names with byte-identical templates/args, and `None`-sentinel rejection are preserved. Classifiers were not moved or touched. `gate_id` `SafeToken` validation is *strengthened* (now also rejects control characters), not weakened. Verified independently that `ConversationTenantAccessRequirement` has only `Read/Write/Admin/Governance` (no `None`), so the helper's new `operation_class` `None`-check can never change an emitted token. File List matches `git status` reality (only `_bmad-output/**` process files are uncommitted-and-unlisted, which the workflow excludes).

**AC-6 sibling/submodule mechanics — prior v0.4 blocker now RESOLVED:** the `Hexalith.Commons` submodule is **clean** at `17820f8` on branch `main`; `git ls-remote origin main` = `17820f8` (push verified); the root gitlink is bumped `feaf007 → 17820f8` in the umbrella working tree (unstaged ` M Hexalith.Commons`, to land in the orchestrator's umbrella commit). The v0.4 CRITICAL ("Commons promotion uncommitted + root gitlink not bumped; clean checkout would fail") no longer holds — this is the exact state in which Story 3.2 was marked **done**. The promotion is purely additive (no pre-existing shared Commons file behaviorally repurposed) and no sibling references the new library yet, so AC-6 sibling compatibility holds by construction.

**Findings fixed during this pass (auto-fix, documentation-accuracy only — no code change required):**

- **[MEDIUM] Stale story metadata claimed the Commons promotion was uncommitted and that the story must stay `in-progress`.** Updated the Status header comment, the Completion Notes "OUTSTANDING"→"RESOLVED" item, and the File List Commons header to reflect the committed/pushed `17820f8` reality.
- **[LOW] Task 7 wording said "Root gitlink bumped … in the umbrella commit"** when the bump is (correctly) an unstaged working-tree change to land in the orchestrator's umbrella commit. Reworded to the honest Story 3.2 phrasing.

**No CRITICAL findings:** every `[x]` task has verifiable evidence, all six ACs are implemented and test-proven, gates are green, and the submodule mechanics are satisfied. Status set to **done** per the workflow rule (0 CRITICAL after fixes). The only nominal follow-up is the orchestrator's umbrella commit that records the already-staged-in-working-tree gitlink — the same satisfied-by-construction tail Story 3.2 carried.

## Change Log

| Date | Version | Description | Author |
|---|---|---|---|
| 2026-06-08 | 0.1 | Story context created (create-story). | Codex |
| 2026-06-08 | 0.2 | Dev: promoted `Hexalith.Commons.Diagnostics` + adopted Conversations telemetry wrappers (codex hit context limit mid-wrap-up). | Codex |
| 2026-06-08 | 0.3 | Automate: added helper + wrapper-guard tests; fixed analyzer-as-error findings; gates green. | Claude |
| 2026-06-08 | 0.4 | Senior Developer Review (AI): verified contract continuity + gates; completed story records; runbook landing-zone entry added. Outcome: Changes Requested — pending orchestrator Commons commit/push + gitlink bump. | Claude |
| 2026-06-08 | 0.5 | Senior Developer Review (AI) second pass: re-ran all gates here (Commons.Diagnostics 16, Server 582, Conformance 361 ≥360, Release 0/0); re-read every helper + wrapper + diff to confirm contract continuity; confirmed the v0.4 Commons commit/push + gitlink blocker is RESOLVED (`17820f8` on `origin/main`, clean, gitlink bumped in working tree) — exact Story 3.2 done-state. Auto-fixed stale story metadata (Status/Completion Notes/File List/Task 7 wording). 0 CRITICAL → Status set to done. | Claude |
