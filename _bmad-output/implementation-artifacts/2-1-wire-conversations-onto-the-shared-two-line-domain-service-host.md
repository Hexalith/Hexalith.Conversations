---
baseline_commit: 764d14bb824c25104335967dee9a84d80286f008
---

# Story 2.1: Wire Conversations onto the shared two-line domain-service host

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversations maintainer,
I want the server wired onto the shared EventStore domain-service host instead of bespoke DI/host wiring,
so that the module's host is two lines and all canonical endpoints resolve via the SDK.

This is the **first story of Epic 2** (Consume Existing Technical-Module Surface) and the first story in the
initiative that touches `src/` production code. It is classified **greenfield-adopt**: the host slot is
unbuilt (`Server/Program.cs` throws `NotImplementedException`), so there is no bespoke host to remove — only
a slot to fill correctly with the SDK idiom. Covers **FR-3**. Relevant NFRs: **NFR1** (behavior preservation),
**NFR8** (public-surface / EventStore-concept boundary preserved).

## Acceptance Criteria

1. **(AC-1 — two-line host wiring)** Given `src/Hexalith.Conversations.Server/Program.cs` currently throws
   `NotImplementedException` (greenfield slot), when the host is wired, then it uses
   `builder.AddEventStoreDomainService(...)` + `app.UseEventStoreDomainService()` with **explicit assembly-scanning
   registration** of the Conversations domain assembly (and the Server boundary assembly, for forward-compatible
   handler discovery) — never the calling-assembly overload (which would scan the host project, not the domain).

2. **(AC-2 — canonical endpoints resolve via the shared host)** Given the shared host, when the app starts, then
   all canonical domain endpoints resolve via the SDK route table: `GET /`, `POST /process`, `POST /replay-state`,
   `POST /query`, `POST /project`, and `POST /admin/operational-index-metadata` — plus the ServiceDefaults health
   endpoints (`/health`, `/alive`, `/ready`). "Resolve" means the routes are mapped and the host composes/boots;
   it does **not** mean every query/projection returns live data (no `IDomainQueryHandler` / `IDomainProjectionHandler`
   implementations exist yet — those are adopted in Stories 2.3 / 2.5; see Scope Boundaries).

3. **(AC-3 — no re-implemented SDK discovery)** Given any per-feature `ServiceCollectionExtensions` that *merely
   re-implement SDK discovery* (domain-processor / query-handler / projection-handler registration), then they are
   removed or **never introduced**. The host must rely on the SDK's convention discovery, not a hand-rolled scanner.
   (Legitimate Conversations app-service registrations — cursor options, hydration, telemetry, tenant access — are
   **not** SDK-discovery re-implementations and are out of this AC's scope; do not delete them.)

4. **(AC-4 — boundary guard updated, not silently broken)** The `ServerBoundaryTest` scaffold-safety guard
   (`tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`) currently asserts the Server references **no**
   EventStore runtime/host packages. That premise is exactly what this story changes. Given the Server is now a
   legitimate EventStore domain-service host, when the boundary test is re-evaluated, then it is updated/re-expressed
   to permit the `Hexalith.EventStore.DomainService` reference while still forbidding the genuinely-out-of-bounds
   dependencies (`Hexalith.Tenants.Server`, `Hexalith.Parties`, `Hexalith.FrontComposer`, and direct
   `Hexalith.EventStore.Server` gateway reference) — and the change is recorded per AC-7.

5. **(AC-5 — T3 internal governance audit gate: surface or retire)** The internal governance audit gate
   (fail-closed-on-sink-failure), currently `internal` and **oracle-unreachable**, was explicitly handed to
   **Story 2.1** by Story 1.3's at-risk register and the Epic 1 retrospective (action T3). The publicly-observable
   audit-pairing invariant is already pinned in the oracle (`GovernanceAuditPairingSafetyNetConformanceTest`).
   Given this residual internal gate, when the host wiring is complete, then the gate is either **surfaced** so the
   conformance oracle can observe it through the host/public surface, **or retired with a recorded justification** if
   the shared host makes it redundant — with the disposition recorded in the FR-20 ledger (AC-7). Do not leave it
   silently internal a second time.

6. **(AC-6 — standing conformance gate holds)** The full conformance suite is **100% green** on the story branch
   (Epic 1 closed at **348 tests**; the count must hold or grow, never regress). The **public contract-shape diff** vs
   the Story 1.1 snapshot (`docs/release-evidence/public-contract-shape-baseline-v1.json`, 196 types) is **empty**
   (`Program.cs` is not public API — adding the host must not change the public contract shape; if it does, that is a
   regression to investigate, not approve). No hot-path regression is introduced (NFR1/NFR2 — behavior preserved).

7. **(AC-7 — ledger updated for any removed/weakened test)** Per team agreement A2, no test is deleted or weakened
   without a matching entry traceable to its `at-risk-test-register-v1` row (the FR-20 ledger). Any modification to
   `ServerBoundaryTest` (AC-4) and any disposition of the internal audit gate (AC-5) is recorded as an append-only
   entry in `docs/release-evidence/at-risk-test-register-v1.{json,md}` via its generation test (do not hand-edit the
   JSON — regenerate it). Assertion strength of the oracle is not reduced vs the Story 1.1 baseline.

## Tasks / Subtasks

- [x] **Task 1 — Add the DomainService SDK project reference** (AC: 1, 4)
  - [x] In `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`, add a `ProjectReference` to
        `Hexalith.EventStore.DomainService` using the **existing submodule-conditional dual pattern** already used for
        `Hexalith.EventStore.Contracts` (lines 8–9): one ref via `$(HexalithEventStoreRoot)` when set, one via
        `..\..\Hexalith.EventStore\src\...` when not. Do **not** hardcode a package version (Central Package Management).
  - [x] Confirm the transitive closure pulled in is acceptable: `Hexalith.EventStore.Client`,
        `Hexalith.EventStore.ServiceDefaults`, `Dapr.AspNetCore`, `FrameworkReference Microsoft.AspNetCore.App`.
        `Hexalith.EventStore.DomainService` is **not** `Hexalith.EventStore.Server` (the gateway) — keep the gateway
        out of the Server's references.
- [x] **Task 2 — Replace the `NotImplementedException` stub with the two-line host** (AC: 1, 2, 3)
  - [x] Rewrite `Program.cs` to the SDK idiom: `WebApplication.CreateBuilder(args)` →
        `builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly)`
        (explicit-assemblies overload — pass the **domain** assembly so `ConversationAggregate` is discovered, and the
        **Server** assembly so future `IDomainQueryHandler`/`IDomainProjectionHandler` implementations are discovered
        without re-touching `Program.cs` in 2.3/2.5) → `app.Build()` → `app.UseEventStoreDomainService()` → `app.Run()`.
  - [x] Keep the ITANEO copyright header. File-scoped top-level statements are fine (matches the AppHost/Admin.Web
        `Program.cs` style in this module).
  - [x] Do **not** author a custom domain-processor/handler discovery `ServiceCollectionExtensions` (AC-3) — the SDK
        owns discovery. Do **not** wire query/read-model/projection adoption here (that is 2.3/2.4/2.5 — Scope Boundaries).
- [x] **Task 3 — Update the boundary guard and record it** (AC: 4, 7)
  - [x] Update `ServerBoundaryTest` so both the assembly-level and `.csproj`-XML assertions reflect the new reality:
        permit `Hexalith.EventStore.DomainService` while still asserting the forbidden references
        (`Hexalith.Tenants.Server`, `Hexalith.Parties`, `Hexalith.FrontComposer`, and the `Hexalith.EventStore.Server`
        gateway). Verify by building whether adding the DomainService ref trips the existing `Dapr.Client` /
        `Hexalith.EventStore.Server` assertions and adjust the *intent* accordingly — do not just delete assertions.
  - [x] Record the guard change as an append-only entry in the FR-20 ledger
        (`docs/release-evidence/at-risk-test-register-v1.{json,md}`) by **regenerating** it via
        `AtRiskTestRegisterGenerationTest` (do not hand-edit the JSON).
- [x] **Task 4 — Resolve the T3 internal governance audit gate** (AC: 5, 7)
  - [x] Locate the internal fail-closed-on-sink-failure governance audit gate (visible only to `Server.Tests`;
        see `Server/Governance/*` and `Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs`).
  - [x] Decide and execute: **surface** it so the oracle observes it through the host/public surface, **or retire**
        it with a recorded justification if the shared host makes it redundant. Record the disposition in the ledger
        (append-only, regenerated). Do not leave it silently internal again.
- [x] **Task 5 — Add a host composition smoke test** (AC: 2)
  - [x] Add a Server.Tests (or IntegrationTests) test that builds the host via `WebApplicationFactory<Program>`
        / minimal-host composition and asserts the canonical routes are present in the endpoint data source
        (`GET /`, `POST /process`, `/replay-state`, `/query`, `/project`, `/admin/operational-index-metadata`) and
        that the app composes without throwing. Make `Program` test-visible if needed (partial `Program` class /
        `InternalsVisibleTo` already declared for `Hexalith.Conversations.Server.Tests`). Endpoints whose execution
        requires a live DAPR sidecar / EventStore gateway are an integration concern — assert **route presence and
        composition**, not live request round-trips.
- [x] **Task 6 — Run the standing conformance gate and finalize the record** (AC: 6, 7)
  - [x] Build the solution (`Hexalith.Conversations.slnx`) Release; run the full conformance suite and the Server
        test project. Confirm green ≥ 348 (monotonic), public-contract-shape diff empty, no `src/` public API change.
  - [x] **Generate the Dev Agent Record test counts / File List from the final `dotnet test` run as the last step**
        (Epic 1 retro P1/P2 — the human-curated count drifted in 5/5 Epic 1 stories; generate it last so the record
        matches the working tree at first review).

## Dev Notes

### Scope Boundaries — what this story does and does NOT do

**DOES (FR-3, greenfield-adopt):**
- Fill the unbuilt `Server/Program.cs` host slot with the canonical two-line EventStore domain-service host.
- Add the `Hexalith.EventStore.DomainService` project reference.
- Use SDK convention discovery (explicit-assemblies overload) for the Conversations domain.
- Update the now-obsolete `ServerBoundaryTest` scaffold-safety premise and record it.
- Surface-or-retire the residual internal governance audit gate (T3, owned by this story).

**DOES NOT (later Epic 2 stories — actively avoid scope creep):**
- **2.2 (FR-7):** refactoring `ConversationAggregate` onto `EventStoreAggregate<TState>` reflection dispatch / removing
  idempotency-bridge shims. (The aggregate *already extends* `EventStoreAggregate<ConversationState>`; do not re-touch it.)
- **2.3 (FR-4):** implementing `IDomainQueryHandler` / adopting `IQueryCursorCodec` / removing the hand-rolled HMAC cursor.
  The existing `ConversationQueryHandler` and `ConversationQueryServiceCollectionExtensions` stay as-is here.
- **2.4 (FR-5):** `IReadModelStore` + `ReadModelWritePolicy` adoption.
- **2.5 (FR-6):** `IDomainProjectionHandler` adoption / projection-materializer plumbing removal.
- **2.6 (FR-8):** shared serialization helpers.
- **2.7 (FR-9):** EventStore.Testing assertions/fakes + local ServiceDefaults adoption.
- **Do NOT** consolidate or delete the module's own `Hexalith.Conversations.ServiceDefaults` / `.AppHost` / `.Aspire`
  projects. The EventStore authoring guidance says domain modules ideally don't ship those, but that consolidation is
  Epic 3 (3.4 ServiceDefaults, 3.5 Aspire) — out of scope here. The SDK's `AddEventStoreDomainService` calls its own
  `AddServiceDefaults()` internally; that does not require touching the local ServiceDefaults marker in 2.1.

### The SDK contract (authoritative facts for wiring)

The "two-line host" is the documented authoring idiom for any Hexalith EventStore domain module. Per the EventStore
submodule's own authoring rules: *"Host shape — `Program.cs` is two lines: `builder.AddEventStoreDomainService();`
then `app.UseEventStoreDomainService();`. The SDK provides convention discovery/registration and the canonical DAPR
endpoints."* [Source: Hexalith.EventStore/CLAUDE.md#Domain-Module-Authoring]

`AddEventStoreDomainService` overloads (pick the **explicit-assemblies** one for Conversations, since domain code lives
in a separate assembly from the host):
```csharp
WebApplicationBuilder AddEventStoreDomainService(this WebApplicationBuilder builder);                                   // calling assembly — NOT for us
WebApplicationBuilder AddEventStoreDomainService(this WebApplicationBuilder builder, Action<EventStoreOptions> configure);
WebApplicationBuilder AddEventStoreDomainService(this WebApplicationBuilder builder, params Assembly[] domainAssemblies); // ← use this
WebApplicationBuilder AddEventStoreDomainService(this WebApplicationBuilder builder, Action<EventStoreOptions> configure, params Assembly[] domainAssemblies);
```
[Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs:46-90]

What `AddEventStoreDomainServiceCore` does (in order): `builder.AddServiceDefaults()` (observability, health, service
discovery, HTTP resilience) → `builder.Services.AddEventStore(domainAssemblies)` (convention discovery + **keyed**
`IDomainProcessor` registration — this is how `ConversationAggregate` becomes reachable by `/process`) →
`AddDomainQueryHandlers(...)` (scans for `IDomainQueryHandler`, scoped) → `AddDomainProjectionHandlers(...)` (scans for
`IDomainProjectionHandler`, singleton). It throws `ArgumentException` if zero assemblies are passed; the handler scans
are idempotent (skip if already registered).
[Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs:171-228]

> **Key implication for 2.1:** at this story there is exactly **one** discoverable domain type — `ConversationAggregate`
> (`IDomainProcessor`). There are **no** `IDomainQueryHandler` or `IDomainProjectionHandler` implementations yet (the
> bespoke `ConversationQueryHandler` does not implement the SDK interface — that conversion is Story 2.3; projections
> become `IDomainProjectionHandler` in Story 2.5). So `/query` and `/project` will be **mapped but have no handlers to
> dispatch to** until those stories land. That is expected and correct — AC-2 is "routes resolve / host composes",
> not "every endpoint returns live data." Do not try to make queries/projections work end-to-end here.

`UseEventStoreDomainService` = `app.UseEventStore()` (activation manifest / DAPR resource-name resolution) +
`app.MapDefaultEndpoints()` (health: `/health`, `/alive`, `/ready`) + `app.MapEventStoreDomainService()` (the canonical
routes). [Source: ...EventStoreDomainServiceExtensions.cs:101-114]

Canonical routes mapped by `MapEventStoreDomainService` — these are AC-2's checklist:
`GET /` (status root), `POST /process`, `POST /replay-state`, `POST /query`, `POST /project` (skipped if the app already
mapped its own `/project`), `POST /admin/operational-index-metadata`.
[Source: ...EventStoreDomainServiceExtensions.cs:130-168]

**Reference host** (the canonical minimal sample — mirror this shape, but use the explicit-assemblies overload):
```csharp
using Hexalith.EventStore.DomainService;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddEventStoreDomainService();      // sample uses calling-assembly; Conversations passes its domain assemblies
WebApplication app = builder.Build();
app.UseEventStoreDomainService();
app.Run();
```
[Source: Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Program.cs:1-38]

### Files to touch (and their current state)

| File | State | Change |
|---|---|---|
| `src/Hexalith.Conversations.Server/Program.cs` | `throw new NotImplementedException(...)` (greenfield) | Replace with the two-line host (Task 2). |
| `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` | refs Contracts, domain, EventStore.Contracts, Tenants.Client/Contracts; `InternalsVisibleTo Server.Tests` | Add `Hexalith.EventStore.DomainService` ProjectReference, submodule-conditional (Task 1). |
| `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs` | asserts Server references **no** EventStore runtime/host pkgs | Update premise to permit the DomainService host ref; keep genuine forbidden-ref assertions (Task 3). |
| `docs/release-evidence/at-risk-test-register-v1.{json,md}` | seeded by Story 1.3 | Append boundary-guard change + audit-gate disposition; **regenerate**, don't hand-edit (Tasks 3, 4, 7). |
| `tests/Hexalith.Conversations.Server.Tests/**` (new) | — | Add host-composition route-presence smoke test (Task 5). |
| `src/Hexalith.Conversations.Server/Governance/**` | internal audit gate | Surface or retire per AC-5 (Task 4). |

Assembly markers available to pass: `Hexalith.Conversations.ConversationsAssemblyMarker` (domain assembly, contains
`ConversationAggregate`) and `Hexalith.Conversations.Server.ServerAssemblyMarker`.
[Source: src/Hexalith.Conversations/ConversationsAssemblyMarker.cs ; src/Hexalith.Conversations.Server/ServerAssemblyMarker.cs]

### Regression watch — `ServerBoundaryTest` is the primary trap

`ServerBoundaryTest` has two facts (`ServerAssemblyShouldReferenceContractsAndDomainWithoutEventStoreRuntime` — uses
`GetReferencedAssemblies()`; and `ServerProjectFileShouldNotDeclareForbiddenRuntimeReferences` — parses the `.csproj`
XML). Both encode the *scaffold-era* premise that the Server is not yet a host. This story makes the Server a host, so:
- The XML test will now see a `Hexalith.EventStore.DomainService` ProjectReference. It currently only forbids
  `Hexalith.EventStore.Server`, `Hexalith.Tenants.Server`, `Hexalith.Parties`, `Hexalith.FrontComposer`, `Dapr.Client`
  — `DomainService` is none of those by string match, so the XML test *may still pass as written*. **Verify by building.**
- The assembly-level test reads `GetReferencedAssemblies()` on the compiled Server assembly. `Dapr.AspNetCore`
  (transitively `Dapr.Client`) and `Hexalith.EventStore.Client` arrive transitively. Whether `Dapr.Client` appears in
  the Server assembly's *direct* metadata references depends on whether `Program.cs` touches those types (it does not).
  **Verify by building**; if a forbidden-string assertion now trips, the assertion's *intent is obsolete* — update it
  deliberately and record it (AC-7), do not silently delete.
[Source: tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs:24-74]

### Standing conformance gate (applies to every Epic 2–4 story)

Suite 100% green on the branch; public contract-shape diff vs the Story 1.1 snapshot empty or explicitly approved &
recorded; local copy (where one exists — none here, greenfield) deleted; no test deleted without a recorded FR-20
ledger justification. [Source: epics.md#Epic-2 standing-conformance-gate]

### Project Structure Notes

- Module follows the Hexalith project shape: `Contracts`, `Client`, `Server`, `Admin.Web`, `AppHost`,
  `ServiceDefaults`, `Testing`, with `tests/Hexalith.Conversations.*.Tests` mirrors. The Server is the host project.
- Conformance/Server tests run **per-project**, not solution-wide (`dotnet test tests/Hexalith.Conversations.Conformance.Tests/`).
  Use `Hexalith.Conversations.slnx` for restore/build only. [Source: Hexalith.EventStore/CLAUDE.md#Build-&-Test-Commands]
- Submodule rule (binding): initialize/update **root-level submodules only**; never
  `git submodule update --init --recursive`. The EventStore SDK is consumed via the submodule-conditional project
  reference, building from source (the DomainService SDK is `IsPackable=false`, so a NuGet package is not available).
  [Source: CLAUDE.md#Git-Submodules ; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/*.csproj]

### Carry-forward technical-debt awareness (do not let it flake the gate)

- **T1 (HIGH, open since 1.2):** a test-parallelism race between `PublicContractShapeSnapshotGenerationTest` (writer)
  and `ReleaseBaselineValidationTest` (reader) can throw a transient `JsonReaderException` under xUnit parallelism.
  The Epic 1 retro recommended fixing it *before* Epic 2 raises run frequency. If it recurs while running the gate,
  fix it test-only (xUnit `[Collection]` to serialize, or write to a temp file) — this is the owning moment.
  [Source: epic-1-retro-2026-06-03.md §3.2, §7 T1]

### References

- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story-2.1] — story statement + ACs + standing gate.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR-Coverage-Map] — FR-3 → Epic 2, greenfield slot.
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#B] — `AddEventStoreDomainService(...)`/`UseEventStoreDomainService()` two-line host; assembly scan; DI/ServiceCollection row #9 (Promote/Consume, FR-3).
- [Source: Hexalith.EventStore/CLAUDE.md#Domain-Module-Authoring] — canonical two-line host shape + domain-module rules.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs:46-228] — overloads, core registration, route mapping.
- [Source: Hexalith.EventStore/samples/Hexalith.EventStore.Sample/Program.cs:1-38] — reference host.
- [Source: docs/release-evidence/at-risk-test-register-v1.md] — Story 2.1 owns the governance structural re-registration + the residual internal audit gate (carry-forward #1 / T3).
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md §7] — P1/P2 (generate record last), T1 (parallelism race), T3 (audit gate → 2.1), A2 (ledger entry per removed test).
- [Source: src/Hexalith.Conversations.Server/Program.cs] — current `NotImplementedException` greenfield slot.
- [Source: src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs] — the one discoverable `IDomainProcessor` (already on `EventStoreAggregate<ConversationState>`; do not refactor here — that's 2.2).
- [Source: src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj] — submodule-conditional ProjectReference pattern to copy.
- [Source: Directory.Build.props:4-5] — `$(HexalithEventStoreRoot)` resolution for the conditional reference.

## Developer Context

### Technical Requirements (dev agent guardrails)

- .NET 10 (`net10.0`), SDK pinned `10.0.302` (`global.json`). Nullable enabled, implicit usings, **warnings-as-errors**
  — do not suppress broadly. File-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix, CRLF.
- Central Package Management (`Directory.Packages.props`) — never put package versions in `.csproj`.
- Keep the change scoped to Conversations artifacts + the test/evidence updates this story mandates. **Do not edit**
  EventStore/Tenants/Parties/FrontComposer sources (no backward-compat edits are required for Epic 2 — confirmed).
- The ITANEO copyright header must stay on edited source files.

### Architecture Compliance

- Let EventStore own routing, actor identity, persistence, snapshots, publication, projection invalidation, command
  status — the host wiring delegates all of that to the SDK. Do **not** expose raw EventStore command envelopes,
  aggregate IDs, or projection internals as the adopter API (NFR8: public-surface/EventStore-concept boundary).
- Keep authorization/tenant lookups/HTTP/Parties calls out of aggregate logic — unchanged here; this story only wires
  the host, it does not move domain logic.
- Fail-closed posture is preserved: the SDK host is the canonical path; the bespoke `NotImplementedException` was a
  deliberate fail-closed placeholder, now replaced by the real fail-closed-by-construction host.

### Library / Framework Requirements

- **`Hexalith.EventStore.DomainService`** (namespace `Hexalith.EventStore.DomainService`) — provides
  `AddEventStoreDomainService` / `UseEventStoreDomainService`. Referenced as a **project** (submodule, `IsPackable=false`),
  not a NuGet package. Transitively brings `Hexalith.EventStore.Client`, `Hexalith.EventStore.ServiceDefaults`,
  `Dapr.AspNetCore`, and `FrameworkReference Microsoft.AspNetCore.App`.
- Server SDK is `Microsoft.NET.Sdk.Web` already (the `.csproj` uses it) — compatible with `WebApplication`.
- Versions in this ecosystem: Dapr `1.17.7`, Aspire `13.2.x`/`13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`,
  NSubstitute `5.3.0`. Use these via CPM; do not introduce new package versions.

### File Structure Requirements

- `Program.cs` stays in `src/Hexalith.Conversations.Server/`. New tests go under
  `tests/Hexalith.Conversations.Server.Tests/` (mirrors `src`). Evidence artifacts stay under `docs/release-evidence/`
  and are written by generation tests, never hand-edited.

### Testing Requirements

- xUnit v3 + Shouldly + NSubstitute. Run per-project. Host-composition test should use `WebApplicationFactory<Program>`
  (make `Program` test-visible; `InternalsVisibleTo Hexalith.Conversations.Server.Tests` is already declared).
- **Prove behavior, not mirrors** (Epic 1 L1 / agreement A1): a route-presence assertion is fine for AC-2, but if you
  surface the audit gate (AC-5), prove it with a fault-injection that turns a test **RED** when the gate is bypassed —
  "green alone is not evidence."
- Conformance suite must stay ≥ 348 and monotonic; assertion strength must not drop vs the Story 1.1 baseline.
- Integration-test rule (carried from EventStore conventions): a Tier-2/3 test must inspect real end-state, not only a
  202/return code or a mock call count — applies if you add any request-level integration test.

### Previous-Story Intelligence (Epic 1 → Epic 2 carry-forward)

This is the first story of Epic 2; there is no prior Epic 2 story. The binding learnings from the Epic 1 retrospective:
- **L1 / A1 — coverage ≠ live-path exercise.** Pin behavior by fault-injection (flip the branch, watch it go red).
- **P1 / P2 — generate the Dev Agent Record (counts + File List) from the final `dotnet test` run, last.** The
  human-curated count drifted in 5/5 Epic 1 stories; do the record generation as the final step (Task 6).
- **L3 — validators need teeth against reality** (resolve to real files / recompute), not internal consistency.
- **T2 / projectReferenceDisposition** — the `Conformance.Tests → Server` project reference is deliberately kept until
  a later owning story (2.2/2.5/3.2/3.3) removes it; **do not remove it in 2.1.**
- **T3 — the internal governance audit gate is explicitly this story's to surface-or-retire** (AC-5).
- **A2 / A3** — ledger entry for any removed/weakened test; reclassifications go through the
  `classification-change-procedure-v1` append-only changeLog (none expected in 2.1, but stay append-only).

### Git Intelligence (recent work patterns)

Recent commits are all Epic 1 stories (`feat(story-1.1..1.5)`) — test-only + evidence-artifact, **zero `src/`
production change**. This story (2.1) is the **first `src/` production change in the initiative**. Established patterns
to reuse: the evidence-generation-test idiom (repo-root discovery → deterministic indented-JSON write → re-read +
re-validate + content-safety scan) used for all `docs/release-evidence/*` artifacts (regenerate, never hand-edit).
Commit style is Conventional Commits, e.g. `feat(story-2.1): ...`.

### Project Context Reference

`_bmad-output/project-context.md` is binding. Most-relevant rules for this story:
- "Model the first aggregate as `Conversation`… EventStore owns routing/actor identity/persistence/snapshots/
  publication/projection invalidation/command status." — the host wiring delegates exactly this.
- "Do not expose raw EventStore command envelopes/aggregate IDs/snapshot mechanics/projection internals as the primary
  adopter API." — keep the SDK's canonical endpoints as the host surface; do not leak internals.
- "Treat EventStore… as bounded-context dependencies; do not copy their contracts or reimplement their runtime
  behavior inside Conversations." — consume `AddEventStoreDomainService`; do not re-implement discovery (AC-3).
- "Never initialize nested submodules / no `--init --recursive`." — root-level submodule only.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (1M context) — BMAD dev-story workflow.

### Debug Log References

- Fault-injection verification (AC-5 teeth): temporarily replaced `ConversationGovernanceAuditGate`'s
  `return AuditUnavailable()` with `throw;` → `GovernanceAuditSinkFailClosedConformanceTest.GovernedMutationShouldFailClosedWhenAuditSinkThrows`
  turned RED (exception propagated out of `HandleAsync`); reverted. Confirms green is real evidence, not a mirror.
- T1 parallelism race reproduced once during the gate run (1/351 transient fail on the contract-shape
  writer/reader interleave), then fixed test-only via `ReleaseEvidenceArtifactCollection`; verified green across
  5 consecutive full-suite runs.

### Completion Notes List

**Outcome — all 7 ACs satisfied; story is greenfield-adopt (first `src/` production change in the initiative).**

- **AC-1 / AC-2 (Tasks 1, 2, 5):** `Server/Program.cs` is now the canonical two-line host —
  `builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly)`
  (explicit-assemblies overload, **not** calling-assembly) + `app.UseEventStoreDomainService()`. Added the
  submodule-conditional `Hexalith.EventStore.DomainService` ProjectReference (no inline version). A new host-composition
  smoke test (`ConversationsDomainServiceHostCompositionTest`) proves the host composes without throwing and that all six
  canonical routes (`GET /`, `/process`, `/replay-state`, `/query`, `/project`, `/admin/operational-index-metadata`) plus
  the three ServiceDefaults health endpoints (`/health`, `/alive`, `/ready`) are mapped. A companion discovery test
  (`ConversationsDomainDiscoveryHostCompositionTest`) supplies the AC-1 teeth: it proves the explicit-assemblies overload
  registers `ConversationAggregate` as the keyed `IDomainProcessor` the `/process` route resolves, and that the forbidden
  calling-assembly overload would discover nothing — a regression the route-presence smoke test alone cannot catch. Used direct SDK minimal-host
  composition rather than `WebApplicationFactory<Program>` because `Microsoft.AspNetCore.Mvc.Testing` is not in the
  Conversations CPM and the guardrails forbid introducing a new package version; the story permits "minimal-host composition".
- **AC-3:** No hand-rolled discovery `ServiceCollectionExtensions` introduced; the SDK owns convention discovery. No
  query/read-model/projection adoption done here (deferred to 2.3/2.4/2.5 per Scope Boundaries).
- **AC-4 / AC-7 (Task 3):** `ServerBoundaryTest` re-expressed — both facts now **require** the `Hexalith.EventStore.DomainService`
  host reference (positive assertion, strengthened) while still forbidding the gateway (`Hexalith.EventStore.Server`),
  `Hexalith.Tenants.Server`, `Hexalith.Parties`, `Hexalith.FrontComposer`, and a direct `Dapr.Client` reference. Verified
  by building: `DomainService` is a direct metadata reference; `Dapr.Client`/gateway are not. Recorded in the FR-20 ledger.
- **AC-5 / T3 (Task 4):** The residual internal fail-closed-on-sink-failure audit gate was **surfaced, not retired** —
  the behavior is live (used by the governed command handlers), so the shared host does not make it redundant. New oracle
  test `GovernanceAuditSinkFailClosedConformanceTest` observes it through the **public** governed command-handler surface
  (a throwing audit sink → fail-closed `audit_unavailable` rejection with no mutation; healthy-sink contrast fact).
  The gate stays `internal` (making it public would change the public contract shape, which AC-6 forbids). Fault-injection
  proven (see Debug Log). ✅ Resolved review finding [T3]: internal governance audit gate surfaced to the oracle.
- **AC-6:** Full conformance suite **351 green** (was 348 at Epic 1 close → monotonic +3). Public-contract-shape baseline
  byte-unchanged (`Program.cs` is not public API). Solution builds Release with **0 warnings** (warnings-as-errors).
- **AC-7:** All test/guard premise changes recorded append-only in `at-risk-test-register-v1.{json,md}` via its generation
  test (regenerated, not hand-edited; content-safety scan passes). New `story21StructuralDispositions` section records the
  `ServerBoundaryTest` and `ScaffoldSmokeTest` guard updates and the audit-gate surfacing; the two Story 1.2 carry-forwards
  (`internal-governance-audit-gate`, `test-parallelism-race`) are marked CLOSED by Story 2.1.
- **Carry-forward T1 (owning moment):** the parallelism race recurred under the gate and was fixed test-only via a shared
  non-parallel `ReleaseEvidenceArtifactCollection` across all release-evidence file readers/writers.
- **Collateral guard fix:** `ScaffoldSmokeTest` (IntegrationTests) — added the host SDK ref to the Server's expected set
  (same premise change as `ServerBoundaryTest`) and fixed a pre-existing cross-platform path-normalization bug (Windows
  `\` separators in `.csproj` Include were not collapsed on Linux) so the guard is green on every platform.

**Test results (final per-project `dotnet test -c Release`):** Conformance 351/351 ✅ · Server.Tests 527/527 ✅ ·
Contracts.Tests 587/587 ✅ · Client.Tests 25/25 ✅ · Tests 173/173 ✅ · IntegrationTests 8/8 ✅. Admin.Web.Tests: 12/14
(the 2 failures are the Playwright E2E lane — Chromium browser not installed on this machine; `pwsh tests/install-playwright.ps1`
required — environmental, unrelated to host wiring, not introduced by this story).

**Not committed:** the `Hexalith.EventStore` submodule shows as dirty/pointer-moved from pre-existing external state; this
story made **no** EventStore source edits and does not stage the submodule.

### File List

**Production (src/):**
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj` (modified — added submodule-conditional `Hexalith.EventStore.DomainService` ProjectReference)
- `src/Hexalith.Conversations.Server/Program.cs` (modified — replaced `NotImplementedException` stub with the two-line domain-service host)

**Tests (new):**
- `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainServiceHostCompositionTest.cs` (AC-2 route-presence / composition smoke test)
- `tests/Hexalith.Conversations.Server.Tests/HostComposition/ConversationsDomainDiscoveryHostCompositionTest.cs` (AC-1 teeth — proves the explicit-assemblies overload discovers `ConversationAggregate` as the keyed `IDomainProcessor` and the calling-assembly overload would not)
- `tests/Hexalith.Conversations.Conformance.Tests/GovernanceAuditSinkFailClosedConformanceTest.cs`
- `tests/Hexalith.Conversations.Conformance.Tests/ReleaseEvidenceArtifactCollection.cs`

**Tests (modified):**
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs` (re-expressed boundary premise — AC-4)
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs` (Server expected-ref set + cross-platform path-normalization fix)
- `tests/Hexalith.Conversations.Conformance.Tests/AtRiskTestRegisterGenerationTest.cs` (FR-20 ledger generator: new `story21StructuralDispositions`, carry-forward closures, new validation fact, `[Collection]`)
- `[Collection]`-only (T1 fix): `ClassificationChangeProcedureValidationTest.cs`, `ConformanceManifestValidationTest.cs`, `ConsumePromoteKeepInventoryValidationTest.cs`, `LiveTenantFailClosedOracleCharacterizationTest.cs`, `OracleBlindSpotAnalysisArtifactGenerationTest.cs`, `PublicContractShapeSnapshotGenerationTest.cs`, `ReleaseBaselineValidationTest.cs`, `ReleaseConformanceArtifactGenerationTest.cs`, `ReleaseWaiverValidationTest.cs` (all under `tests/Hexalith.Conversations.Conformance.Tests/`)

**Release evidence (regenerated / updated):**
- `docs/release-evidence/at-risk-test-register-v1.json` (regenerated via `AtRiskTestRegisterGenerationTest`)
- `docs/release-evidence/at-risk-test-register-v1.md` (companion doc updated)

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | Story 2.1 implemented (FR-3, greenfield-adopt): wired `Server/Program.cs` onto the shared two-line EventStore domain-service host with explicit assembly-scanning; added the submodule-conditional `Hexalith.EventStore.DomainService` reference; added a host-composition route-presence smoke test; re-expressed `ServerBoundaryTest` (and the `ScaffoldSmokeTest` reference guard) to permit/require the host SDK while keeping the genuine forbidden-ref assertions; surfaced the T3 internal governance audit gate into the conformance oracle via the public command-handler surface (fault-injection proven); closed the T1 parallelism race test-only via a non-parallel release-evidence collection; recorded all guard/gate changes append-only in the FR-20 ledger. Conformance suite 348 → 351 (monotonic), public-contract-shape diff empty, Release build 0 warnings. Status → review. |
| 2026-06-03 | Automated review (story-automator-review, auto-fix). Re-ran Release build (0 warnings) and the gate per-project: Conformance 351/351 ✅, Server.Tests 527/527 ✅; public-contract-shape baseline byte-unchanged; FR-20 ledger regenerated with the `story21StructuralDispositions` section. All 7 ACs verified against the implementation (including the AC-1/AC-5 fault-injection teeth). Two MEDIUM record-accuracy fixes applied: (M1) added the omitted `ConversationsDomainDiscoveryHostCompositionTest.cs` to the File List; (M2) corrected the recorded Server.Tests count 525 → 527 (the 2 extra facts are that discovery test). No CRITICAL issues; no code changes required. Status → done. |
