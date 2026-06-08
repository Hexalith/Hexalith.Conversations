---
baseline_commit: 4c6e290e855e9e294c738948e92541767940ba64
---

# Story 3.1: *(Tracer-bullet)* Promote & adopt the generic typed-HttpClient registration

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a technical-module maintainer,
I want the duplicated typed-HttpClient registration extracted into a shared domain-agnostic helper and adopted by Conversations as the pilot tracer-bullet,
so that the per-capability promote→adopt→delete pipeline mechanics are proven on the lowest-risk, cleanly-isolated capability first and become the reusable runbook for stories 3.2–3.7.

## Acceptance Criteria

**AC-1 — Promote (helper lives in the chosen technical module).**
Given the landing zone for client registration is resolved (OQ-1, see Dev Notes → *Precondition*) and the `AddXxxClient()` pattern is identical/domain-agnostic across Folders and Projects,
When it is promoted into a shared, domain-agnostic registration helper with options binding and validation, **with its own tests**,
Then the helper lives in the chosen technical module (submodule), is additive/backward-compatible (NFR6), and has unit tests covering: missing endpoint rejected, relative URI rejected, non-http(s) scheme rejected, valid endpoint accepted, and `IHttpClientBuilder` returned for handler chaining.
[Source: epics.md#Story 3.1:455-459; prd.md#4.3 FR-12:179-186]

**AC-2 — Adopt + delete the hand-rolled implementation (FR-17), behavior preserved.**
Given the shared helper,
When Conversations registers its typed `IConversationClient`/`ConversationClient` through it,
Then the hand-rolled registration/validation **logic** in `Hexalith.Conversations.Client` is removed (FR-17 — see Dev Notes → *Deletion vs facade decision*),
And options-validation behavior is **preserved or strengthened, never weakened** (missing endpoint → rejected; relative URI → rejected; non-http(s) scheme → rejected — see *Behavior-preservation contract* below).
[Source: epics.md#Story 3.1:461-464; prd.md#4.3 FR-12:185]

**AC-3 — Tracer-bullet runbook documented.**
Given this is the first promote story,
Then the pipeline mechanics — **promote → test-in-module → adopt → delete → conformance green → additive sibling-CI build → submodule commit + root pointer bump** — are documented as the reusable runbook for stories 3.2–3.7 (a committed markdown artifact, see Task 6).
[Source: epics.md#Story 3.1:466-467; Epic 3 intro:441-447]

**AC-4 — NFR6: dependent siblings compile green; standing conformance gate holds.**
And the promoted API is additive/backward-compatible such that Folders and Projects (and any other dependent module) compile green; the full release-gate conformance suite passes (monotonic, **≥ 357** — the count at 2.7 close) and the public-contract-shape baseline diff is **empty** (the baseline enumerates the Contracts assembly only; Client changes must not alter it).
[Source: epics.md#Story 3.1:469; epics.md#NFR6:85; prd.md#4.5 FR-20:254-263]

**AC-5 — Submodule mechanics (root-only, never recursive).**
Given the helper lands in a sibling submodule (Commons recommended),
Then the promotion is a separate technical-module submodule commit + a root-level gitlink (pointer) bump; **never** recurse into nested submodules; verify all root submodule gitlinks match recorded pointers before building.
[Source: epics.md#Epic 3 per-promote-story additions:447; CLAUDE.md Git Submodules]

## Tasks / Subtasks

- [x] **Task 0 — Resolve the OQ-1 landing zone (gating precondition for AC-1).** (AC: 1)
  - [x] Confirm the landing module. **Ratified: a new `Hexalith.Commons.Http` library** under `Hexalith.Commons/src/libraries/Hexalith.Commons.Http/` (user-ratified, 2026-06-04). EventStore rejected (wrong altitude).
  - [x] No divergent architecture-recorded zone exists; chosen zone + the build-infra caveat (nested `Hexalith.Builds` not initialized → self-contained library `Directory.Build.props`) recorded in the runbook (Task 6).
- [x] **Task 1 — Promote the generic helper into the chosen submodule, with tests.** (AC: 1, 4)
  - [x] Added `HttpClientRegistration.AddTypedHttpClient<TClient,TImplementation,TOptions>(...)` parameterized on interface, implementation, options, and an endpoint selector. Mirrors the EventStore template-method style.
  - [x] Generalized over BOTH shapes without weakening either: lazy `IOptions<T>.Validate` + `BindConfiguration(section)` overload (Folders/Projects), AND eager registration-time validation with a first-class opt-in `requireWebScheme` http/https guard (Conversations). Timing selectable via `HttpClientEndpointValidation`.
  - [x] Added 8 unit tests in `Hexalith.Commons.Http.Tests`: missing endpoint, relative URI, non-http(s) scheme, valid endpoint, builder-returned-for-chaining (+ lazy rejection, lazy permissive, config-section bind).
  - [x] API additive/backward-compatible; `AddFoldersClient`/`AddProjectsClient` signatures untouched.
- [x] **Task 2 — Adopt in Conversations + apply FR-17 deletion.** (AC: 2)
  - [x] Re-implemented `ConversationClientServiceCollectionExtensions.cs` to delegate to the shared helper; removed the bespoke `ValidateEndpoint` body and inline `AddHttpClient<...>` call.
  - [x] **Thin-facade decision adopted (user-ratified):** kept `AddHexalithConversationsClient(Action<ConversationClientOptions>)` (byte-identical public signature) delegating to the shared helper; deleted only the hand-rolled validation/registration logic.
  - [x] Wired the reference via the repo's local-deps convention: `HexalithCommonsRoot` property in root `Directory.Build.props` + guarded `ProjectReference` (with relative fallback) in `Hexalith.Conversations.Client.csproj`. Client builds 0-warning.
- [x] **Task 3 — Preserve/strengthen options-validation behavior and its tests.** (AC: 2, 4)
  - [x] All three rejections still hold for Conversations (eager timing preserved — no oracle weakening): null/missing endpoint, relative URI, non-http(s) scheme → rejected at registration.
  - [x] Positive test `ServiceCollectionExtensionShouldRegisterTypedClientWithConfiguredEndpoint` stays green.
  - [x] **Added the three missing negative tests** in `ConversationClientTest.cs`: reject missing / relative / non-http(s) endpoint.
- [x] **Task 4 — Update the three test guards that pin the Client surface.** (AC: 2, 4)
  - [x] `ContractPackageInventoryTest` `.cs` allowlist — thin facade keeps `ConversationClientServiceCollectionExtensions.cs` → file set unchanged → **no change** (verified green, 7 files).
  - [x] `ClientBoundaryTest` Microsoft-transport allowlist — the facade introduced **no new direct `Microsoft.*` references** in the Client assembly metadata (`GetReferencedAssemblies()` is the used-refs set, not the runtime closure) → **no change** (test passes unmodified; AspNetCore/Server/EventStore/Dapr prohibitions intact).
  - [x] Integration-guide example + both doc tests — entrypoint name unchanged → **no change** (verified green).
- [x] **Task 5 — Prove NFR6: dependent siblings compile green; handle the cross-submodule consumer.** (AC: 2, 4, 5)
  - [x] Determined Projects references Conversations.Client via **local source `ProjectReference`** (`$(HexalithConversationsRoot)\src\...`), not NuGet — so facade removal WOULD have broken it. The thin facade keeps it green with zero sibling edits.
  - [x] Thin-facade approach keeps `AddHexalithConversationsClient` callable; `Projects.Server:146` unchanged.
  - [x] Built `Hexalith.Projects.Server` against the **modified** Conversations source (`-p:HexalithConversationsRoot=<umbrella>`): **0-warning**. `Hexalith.Folders.Client` builds green transitively. Full umbrella Release build: 0 warnings.
- [x] **Task 6 — Author the reusable promote→adopt runbook (tracer-bullet deliverable).** (AC: 3)
  - [x] Created `docs/release-evidence/promote-adopt-runbook.md` capturing the ordered pipeline (resolve landing zone → promote+test → adopt → delete → re-express guards → conformance green → additive sibling build → submodule commit + root pointer bump → verify gitlinks), including the Commons build-infra caveat. Referenced from AC-3.
- [x] **Task 7 — Conformance gate + submodule pointer bump + evidence.** (AC: 4, 5)
  - [x] Ran the full release-gate conformance suite: **360 passed** (monotonic ≥ 357); public-contract-shape diff empty (Contracts assembly unchanged).
  - [x] Release build with **0 warnings** (warnings-as-errors) across the full solution.
  - [~] Submodule commit (Commons): **DONE** — the promote is committed in the Commons submodule at `7425d4a` ("feat: Add Hexalith.Commons.Http library for typed HttpClient registration"), a self-contained commit carrying the library + 8 tests + the `.sln` entry. Root gitlink pointer bump: **NOT done — pending user approval** (outward, hard-to-reverse).
  - [~] **Correction (review 2026-06-08):** the earlier claim "Commons gitlink matches recorded pointer" is **inaccurate**. The umbrella records gitlink `30620b9` for `Hexalith.Commons`, but the submodule working tree is at `d0ea6e2` (drifted **+11 commits**; `git status` shows ` M Hexalith.Commons`). Of those 11, only the promote tip `7425d4a` (and its build-infra parent `7ceca7b`) belong to this story; the other 10 (BMAD removal, UniqueIds revert, PolymorphicSerializations, package bumps, etc.) are unrelated. **Consequence:** the umbrella currently builds green only because Commons is drifted *forward* to include the helper — a fresh checkout restored to the recorded pointer `30620b9` (per the runbook's "verify gitlinks before building" rule) would **fail** to build `Hexalith.Conversations.Client` (no `Hexalith.Commons.Http`). **Required action (AC-5):** bump the root gitlink to **`7425d4a`** (the isolated promote tip — NOT current HEAD `d0ea6e2`, which would carry the 10 unrelated commits). EventStore/FrontComposer/Parties (and the other submodules) carry pre-existing out-of-scope drift to exclude/restore per the runbook. Never recurse nested submodules.
  - [x] Generated the Dev Agent Record (file list, counts) last.

## Dev Notes

### Precondition — OQ-1 landing zone (do not skip)
Epic 3's gate: *"no promote story starts until the landing zone for its capability is resolved by the downstream architecture workflow. Don't promote into the dark."* OQ-1 is still listed as an **open question** in the PRD (not yet closed by architecture). [Source: epics.md#Epic 3 gate:445; prd.md#12 Open Questions:344; prd.md#3 Glossary:59]
- **Resolution for this story (recommended default, to be ratified):** new library `Hexalith.Commons.Http` in the `Hexalith.Commons` submodule. Rationale: Commons is the domain-agnostic infrastructure home; it has **no** existing HTTP-client registration helper (verified — zero `AddHttpClient`/`IHttpClientBuilder` references in Commons libraries); `Hexalith.Commons.Configurations` already pulls `Microsoft.Extensions.Options*`. EventStore is the wrong altitude.
- Open questions must not be silently assumed closed (project-context.md workflow rule). Record the ratified zone in the runbook.

### What is being promoted — the duplicated pattern
The duplicated, domain-agnostic capability is typed-HttpClient DI registration with endpoint-options validation. Confirmed duplication:
- `Hexalith.Folders/src/Hexalith.Folders.Client/FoldersClientServiceCollectionExtensions.cs` — `AddFoldersClient()` (two overloads: `BindConfiguration(section)` + `Action<Options>`), private `AddConfiguredFoldersClient` with lazy `IOptions<FoldersClientOptions>.Validate(BaseAddress not null; BaseAddress absolute)`, `AddHttpClient<IClient, GeneratedFoldersClient>`. Options: `FoldersClientOptions { const DefaultConfigurationSectionName="Folders"; Uri? BaseAddress }`.
- `Hexalith.Projects/src/Hexalith.Projects.Client/ProjectsClientServiceCollectionExtensions.cs` — byte-for-byte identical shape to Folders (`Projects`/`BaseAddress`).
- These two are **identical to each other** (the "identical, domain-agnostic" addendum claim). [Source: addendum.md#C duplication table:53]

### What this story TOUCHES — read before editing (UPDATE files)
- `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs` — **current state:** `AddHexalithConversationsClient(IServiceCollection, Action<ConversationClientOptions>)` returns `IHttpClientBuilder`; **eager** `ValidateEndpoint(options.Endpoint)` throwing `InvalidOperationException` for (a) null/relative URI ("must be an absolute URI."), (b) non-http/https scheme ("must use http or https."); then `AddHttpClient<IConversationClient, ConversationClient>(c => c.BaseAddress = endpoint)`. **Differs from siblings:** eager (not lazy) validation; property `Endpoint` (not `BaseAddress`); extra http/https scheme guard; no config-section binding. The promoted helper must accommodate this shape without weakening it.
- `src/Hexalith.Conversations.Client/ConversationClientOptions.cs` — `sealed record ConversationClientOptions { Uri? Endpoint }`.
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj` — references only `Hexalith.Conversations.Contracts` (project) + `Microsoft.Extensions.Http` (package). Adding the helper reference is the only new dependency.
- `IConversationClient.cs` / `ConversationClient.cs` — the typed client itself (5 async methods, header/idempotency/correlation handling, typed `ConversationClientResult<T>` error mapping). **NOT in scope to change** — only its registration path changes.

### Behavior-preservation contract (the bright line — AC-4 / FR-20)
Preserve **or strengthen, never weaken**. Concretely:
- All three rejections must remain enforced for Conversations: missing/null endpoint, relative URI, non-http(s) scheme.
- If validation timing moves from eager (throw at `Add...`) to lazy (`IOptions.Validate` at first resolve), that is a behavior change in *timing*; only acceptable if a test still proves rejection. Prefer keeping eager semantics for Conversations to avoid an oracle-weakening dispute.
- Public-contract-shape baseline (`docs/release-evidence/public-contract-shape-baseline-v1.json`) enumerates the **Contracts assembly only**, NOT the Client assembly — so this work must produce an **empty** contract-shape diff. (The Client surface is instead pinned by `ContractPackageInventoryTest` + `ClientBoundaryTest`, which you update deliberately.)

### Deletion vs facade decision (FR-17 reconciliation)
FR-17/AC-2 literally says the hand-rolled `AddHexalithConversationsClient()` pair is "deleted." But `AddHexalithConversationsClient` has a **cross-submodule consumer** at `Hexalith.Projects.Server:146`, and removing the entrypoint would break Projects (NFR6 forbids that). Two paths:
1. **Recommended (lowest tracer-bullet risk): thin facade.** Delete the hand-rolled *logic*; keep a one-line `AddHexalithConversationsClient(Action<ConversationClientOptions>)` that delegates to the shared generic helper. Satisfies "adopt the shared helper" + "delete the hand-rolled implementation" + keeps Projects + inventory/guide tests green with zero sibling edits.
2. **Literal full removal.** Delete the method entirely, migrate `Hexalith.Projects.Server:146` to the generic helper (sanctioned cross-submodule promotion edit), update `ContractPackageInventoryTest` allowlist (drop the file) and the integration guide. Higher blast radius for a tracer-bullet.
Pick (1) unless the architecture explicitly mandates the symbol's removal; record the choice + rationale in the runbook.

### Submodule mechanics (mandatory)
- Promote into the Commons submodule = a Commons commit + a **root-level pointer (gitlink) bump**. Each promotion is a separate submodule commit + pointer bump. [Source: epics.md#Epic 3:447]
- **Never** `git submodule update --init --recursive`; never initialize/update nested submodules. [Source: CLAUDE.md; project-context.md:104-105]
- **CRITICAL recurring hazard (2.2–2.7):** verify all root submodule gitlinks match recorded pointers BEFORE building — out-of-scope working-tree drift broke the build in 2.2. Restore drifted submodules to recorded gitlinks first.

### Project Structure Notes
- Conversations module shape is `Contracts / Client / Server / Aspire / AppHost / ServiceDefaults / Testing` + focused test projects. The Client package is `IsPackable=true` (`PackageId=Hexalith.Conversations.Client`) and must remain an isolated adopter package (no Server/EventStore/Dapr/AspNetCore references). [Source: project-context.md Code Quality; ClientBoundaryTest.cs]
- Tech: C#/.NET 10, nullable + implicit usings + warnings-as-errors, Central Package Management (`Directory.Packages.props`), xUnit v3 + Shouldly. Match sibling layout/style. [Source: project-context.md]
- Conventions: file-scoped namespaces, 4-space indent/CRLF/UTF-8/final newline, `Async` suffix, prefer `sealed`. [Source: Hexalith.Projects/CLAUDE.md Naming Conventions]
- **Detected variance / conflict:** Conversations' client validation shape differs from the Folders/Projects pattern the helper is generalized from (eager vs lazy; `Endpoint` vs `BaseAddress`; +scheme check). Rationale: the helper must be a superset — parameterize the endpoint selector and offer both eager and lazy validation, defaulting Conversations to its current (stronger) behavior.

### References
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 3.1:449-469]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Epic 3 intro & gate:441-447]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#FR Coverage Map FR-12/FR-17:138,143; NFR6:85]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#4.3 FR-12:179-186; #4.5 FR-20:254-263; #9 Constraints:323; #12 OQ-1:344]
- [Source: _bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md#C duplication table:53]
- [Source: src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs:21-48]
- [Source: src/Hexalith.Conversations.Client/ConversationClientOptions.cs:11-17]
- [Source: src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj]
- [Source: Hexalith.Folders/src/Hexalith.Folders.Client/FoldersClientServiceCollectionExtensions.cs]
- [Source: Hexalith.Projects/src/Hexalith.Projects.Client/ProjectsClientServiceCollectionExtensions.cs]
- [Source: Hexalith.Projects/src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:144-147]
- [Source: tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs:40-60]
- [Source: tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs:381-394]
- [Source: tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs:57-89]
- [Source: tests/Hexalith.Conversations.Contracts.Tests/Documentation/IntegrationGuideValidationTest.cs:34; IntegrationGuideWorkflowExampleTest.cs:36-39]
- [Source: tests/Hexalith.Conversations.Conformance.Tests/PublicContractShapeSnapshotGenerationTest.cs (Contracts-only baseline)]
- Standing conformance count at 2.7 close = 357 (sprint-status.yaml note); gate is monotonic ≥ 357.

### Open questions for the user (saved per skill protocol)
1. **OQ-1 ratification:** Confirm `Hexalith.Commons.Http` (new library in the Commons submodule) as the FR-12 landing zone, or name the architecture-approved module.
2. **FR-17 strictness:** Accept the thin-facade reconciliation (keep `AddHexalithConversationsClient` delegating to the shared helper), or require full symbol removal + Projects-sibling call-site migration?

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- Commons libraries fail to build from the umbrella because `src/libraries/Directory.Build.props`
  unconditionally imports the **nested** `Hexalith.Builds` submodule (root-only submodule policy leaves it
  uninitialized). Verified via `dotnet build Hexalith.Commons.UniqueIds` (MSB4019). Resolution: a
  self-contained `Directory.Build.props` in the new library folder (MSBuild stops at the nearest), which the
  user ratified.
- `ClientBoundaryTest.ClientAssemblyShouldOnlyReferenceAllowedMicrosoftTransportAssemblies` passed
  **unmodified** after adding the helper reference: `GetReferencedAssemblies()` returns only used direct
  metadata refs (DI.Abstractions + Http), not the transitive runtime closure.
- NFR6 proof for the out-of-tree consumer: `dotnet build Hexalith.Projects.Server -p:HexalithConversationsRoot=<umbrella>` → 0-warning against the modified Conversations source.
- 2 Admin.Web E2E failures are environment-only (Playwright Chromium not installed); unrelated to this story.

### Completion Notes List

- **OQ-1 ratified → `Hexalith.Commons.Http`** (new library in the Commons submodule), with a self-contained
  library `Directory.Build.props` so it builds from umbrella checkouts that do not initialize Commons's
  nested `Hexalith.Builds`. Central package versions resolve via the resilient `Commons/Directory.Packages.props` fallback chain.
- **FR-12 promote:** `HttpClientRegistration.AddTypedHttpClient<TClient,TImplementation,TOptions>` — a
  superset of the Folders/Projects (lazy + section-bind) and Conversations (eager + http/https guard) shapes;
  validation timing via `HttpClientEndpointValidation`; scheme guard is first-class opt-in. 8/8 helper tests green.
- **FR-17 deletion via thin facade:** deleted the hand-rolled `ValidateEndpoint` + inline `AddHttpClient`;
  kept `AddHexalithConversationsClient` (byte-identical signature) delegating to the shared helper. Behavior
  preserved/strengthened (FR-20): three rejections still enforced eagerly + three new negative tests added.
- **Guard tests (Task 4):** no edits required — thin facade preserved the Client `.cs` file set, the boundary
  allowlist, and the integration-guide entrypoint. All verified green.
- **NFR6:** Projects.Server (consumer at `:146`) + Folders.Client compile 0-warning against the promoted API;
  full umbrella Release build 0-warning.
- **Conformance:** 360 passed (≥ 357 monotonic), contract-shape diff empty.
- **Submodule mechanics:** Commons promote **committed** at `7425d4a`; root gitlink bump **pending user
  approval** (outward action). **Review correction (2026-06-08):** the Commons gitlink does **NOT** match
  the recorded pointer — recorded `30620b9`, submodule HEAD `d0ea6e2` (+11 commits, only `7425d4a` is the
  promote). The pending pointer bump must target `7425d4a`, not HEAD. EventStore/FrontComposer/Parties carry
  pre-existing out-of-scope drift to exclude/restore. Nested submodules never initialized or recursed.

### File List

**New — `Hexalith.Commons` submodule (the promote):**
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Http/Directory.Build.props`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Http/Hexalith.Commons.Http.csproj`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Http/HttpClientRegistration.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Http/HttpClientEndpointValidation.cs`
- `Hexalith.Commons/src/libraries/Hexalith.Commons.Http/README.md`
- `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/Hexalith.Commons.Http.Tests.csproj`
- `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/HttpClientRegistrationTest.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/ITestClient.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/TestClientOptions.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/TestClient.cs`
- `Hexalith.Commons/test/Hexalith.Commons.Http.Tests/MarkerHandler.cs`

**Modified — `Hexalith.Commons` submodule:**
- `Hexalith.Commons/Hexalith.Commons.sln` (added the two new projects)

**Modified — umbrella (Conversations adoption + wiring + evidence):**
- `Directory.Build.props` (added `HexalithCommonsRoot` source-root resolution)
- `Hexalith.Conversations.slnx` (added the `Hexalith.Commons.Http` project to the umbrella solution — **added by review 2026-06-08; was missing from this list**)
- `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs` (thin facade)
- `src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj` (helper `ProjectReference`)
- `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` (**4** added tests: 3 negative — missing/relative/non-http(s) endpoint — **plus** `ServiceCollectionExtensionShouldReturnBuilderForHandlerChainingAndUseConfiguredEndpoint`, an end-to-end handler-chaining/configured-endpoint test; the narrative's "3 negative tests" undercounted)

**Out-of-scope working-tree drift present at review (NOT introduced by Story 3.1; the conformance gate and 0-warning builds were re-verified green against current state on 2026-06-08):**
- `Directory.Packages.props` (Aspire.Hosting 13.2.2→13.4.2, Http.Resilience/ServiceDiscovery 10.4.0→10.6.0, OpenTelemetry instrumentation, coverlet 8.0.1→10.0.1, NET.Test.Sdk 18.3.0→18.6.0, Playwright 1.59.0→1.60.0)
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` (Aspire.AppHost.Sdk 13.2.2→13.4.2) and other AppHost/Aspire-config files
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`, `README.md`, and broad submodule-pointer drift across non-Commons submodules. Left as-is (reverting unrelated drift is out of scope); flagged here for transparency.

**New — umbrella:**
- `docs/release-evidence/promote-adopt-runbook.md` (AC-3 reusable runbook)

**Pending (Task 7, on approval):** `Hexalith.Commons` submodule commit + root gitlink pointer bump.

## Change Log

| Date       | Version | Description                                                                                      | Author |
|------------|---------|--------------------------------------------------------------------------------------------------|--------|
| 2026-06-04 | 0.1     | Promoted generic typed-HttpClient registration to `Hexalith.Commons.Http`; adopted in Conversations via thin facade (FR-12/FR-17); preserved+strengthened validation (FR-20); proved NFR6 (Projects/Folders + full Release 0-warning); conformance 360 ≥ 357, contract-shape diff empty; authored promote→adopt runbook (AC-3). Status → review. | Amelia (Dev) |
| 2026-06-08 | 0.2     | Adversarial review (auto-fix). Re-verified all builds/tests green: Commons.Http 8/8, Conversations.Client 29/29, Contracts 603/603, Conformance 360/360, Projects.Server NFR6 0-warning. Fixed HIGH (false "gitlink matches recorded" claim → corrected; pointer-bump target = `7425d4a`), MEDIUM (`.slnx` + drift added to File List), LOW (test-count 4-not-3; CA1014 + eager-message follow-ups recorded). 0 CRITICAL → Status → done. **Outstanding outward action: root gitlink pointer bump (AC-5).** | Jerome Piquot (AI Review) |

## Senior Developer Review (AI)

**Reviewer:** Jerome Piquot (automated adversarial review) · **Date:** 2026-06-08 · **Outcome:** Approve with action items (0 CRITICAL)

### Verification performed (ground truth, not story claims)
- `Hexalith.Commons.Http.Tests` — **8/8 pass** (Release). Covers the 5 AC-1 cases + lazy/permissive/config-bind shapes.
- `Hexalith.Conversations.Client.Tests` — **29/29 pass** (Release). Includes the 3 new negative tests + handler-chaining test + `ClientBoundaryTest`.
- `Hexalith.Conversations.Contracts.Tests` — **603/603 pass** (Release). Includes `ContractPackageInventoryTest` + integration-guide guards (Task 4: no change needed — confirmed).
- `Hexalith.Conversations.Conformance.Tests` — **360/360 pass** (≥ 357 monotonic); contract-shape snapshot test green ⇒ baseline diff empty (AC-4).
- NFR6: `Hexalith.Projects.Server` built against the **modified** Conversations source (`-p:HexalithConversationsRoot`/`HexalithCommonsRoot`) → **0 warning, 0 error**; `Hexalith.Folders.Client` green transitively.
- Code read: facade signature is byte-identical; hand-rolled `ValidateEndpoint`/inline `AddHttpClient` removed (FR-17); helper is a clean superset (eager+scheme-guard / lazy+bind); no logic bug found.

### AC coverage
- **AC-1 Promote** — ✅ helper in `Hexalith.Commons.Http` with 8 tests covering all five required cases.
- **AC-2 Adopt + delete** — ✅ logic deleted, byte-identical facade kept (user-ratified); 3 rejections preserved eagerly + new negative tests.
- **AC-3 Runbook** — ✅ `docs/release-evidence/promote-adopt-runbook.md` (127 lines, full ordered pipeline).
- **AC-4 NFR6 + conformance** — ✅ siblings 0-warning; conformance 360; contract-shape diff empty.
- **AC-5 Submodule mechanics** — ⚠️ **PARTIAL** — submodule commit done (`7425d4a`); **root gitlink pointer bump NOT done (pending user approval — outward action).**

### Findings

**🔴 HIGH**
- **H1 — Inaccurate gitlink claim + reproducibility gap (AC-5).** Story claimed "Commons gitlink matches recorded pointer." It does not: recorded `30620b9`, submodule HEAD `d0ea6e2` (+11 commits; only `7425d4a` is the promote). The umbrella builds green only because Commons is drifted forward; a clean checkout at the recorded pointer would fail to build `Conversations.Client`. **Action (USER, outward):** bump the root gitlink to **`7425d4a`** (isolated promote tip — not HEAD `d0ea6e2`, which carries 10 unrelated commits), as a separate submodule pointer-bump commit; restore/exclude the other drifted submodules first; never recurse nested submodules. *Story text corrected; the git action itself is left for the user (hard-to-reverse).*

**🟡 MEDIUM**
- **M1 — File List incomplete.** `Hexalith.Conversations.slnx` (adds the `Hexalith.Commons.Http` project — a real Story-3.1 change) was missing from the File List. **Fixed** (added).
- **M2 — Undocumented out-of-scope drift.** `Directory.Packages.props` (Aspire/OpenTelemetry/testing package bumps) and `AppHost.csproj` (Aspire SDK 13.2.2→13.4.2), plus `ScaffoldSmokeTest.cs`/`README.md`/non-Commons submodule drift, are modified but unrelated to 3.1 and undocumented. **Fixed** (documented as out-of-scope drift; not reverted — gate re-verified green against current state).

**🟢 LOW**
- **L1 — Test undercount.** Narrative/File List said "3 negative tests"; **4** tests were added (the extra is the handler-chaining/configured-endpoint integration test). **Fixed** (File List corrected).
- **L2 — `CA1014` warning in `Hexalith.Commons.Http.Tests`** ("Mark assemblies with CLSCompliant"). Isolated to the Commons solution (not part of the umbrella 0-warning gate). **Not applied** — fixing it edits the already-committed, pending-approval Commons submodule and would complicate the clean `7425d4a` pointer bump. *Follow-up:* add `CA1014` to the test project `NoWarn` (or a `[assembly: CLSCompliant]`) when the next Commons commit is made.
- **L3 — Eager-path validation message is fully generic**, dropping the options-type name that the lazy path includes (`{optionsType.Name} endpoint must be…`) and the original "Conversations…" wording. Diagnostics-only; FR-20 oracle (exception **type** + rejection) preserved, so behavior is not weakened. **Not applied** (same submodule-churn reason as L2). *Follow-up:* include the type name in `ValidateEndpointOrThrow` for parity.

### Status decision
0 CRITICAL findings → **Status: done** per the review workflow (HIGH/MEDIUM/LOW are tracked, non-blocking). **However, the promote is not fully *landed* until the AC-5 root gitlink pointer bump (→ `7425d4a`) is performed — an outward action awaiting user approval.** Treat that as the one remaining gate before Stories 3.2–3.7 build on this promote.
