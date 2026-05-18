# Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want every conversation command and read to pass tenant access checks before touching conversation state,
so that cross-tenant access, enumeration, and stale authorization cannot leak or mutate protected records.

## Acceptance Criteria

1. Given a command or query arrives with tenant context and caller context, when the application boundary handles the request, then it checks the local Tenants access projection before aggregate load, command dispatch, projection read, publication detail access, or audit-sensitive metadata access, and missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, disabled, unavailable, or unknown tenant state fails closed.
2. Given a request targets a conversation from another tenant or an inaccessible tenant scope, when the request is evaluated, then the response is typed and content-safe, and unauthorized, nonexistent, and cross-tenant records are indistinguishable to non-privileged callers unless policy explicitly permits disclosure.
3. Given tenant authorization fails before a write command, when the command is rejected, then no aggregate state is loaded, no domain event is emitted, no projection mutation is performed, and no tenant-crossing metadata is published, and the rejection result maps to documented tenant-binding or tenant-isolation error semantics.
4. Given tenant authorization fails before a read or list operation, when the read boundary responds, then it does not reveal conversation title, participant names, snippets, timestamps, counts, pagination gaps, business references, provider correlation metadata, or whether a protected record exists, and it returns a safe failure or no-access result suitable for adopter handling.
5. Given tenant access tests run, when positive and adversarial cases execute, then tests cover missing tenant, malformed tenant, stale projection, unavailable projection store, disabled tenant, non-member caller, insufficient role, cross-tenant ID guessing, mixed-tenant command metadata, and projection poisoning, and failures are verified before aggregate or projection access.

## Tasks / Subtasks

- [ ] Confirm implementation preconditions and preserve scope. (AC: 1-5)
  - [ ] Verify the contract types from Story 1.2 exist or implement only the minimum error/result additions that Story 1.5 owns without replacing Story 1.2.
  - [ ] Verify the aggregate/command/read surfaces from Stories 1.3, 1.4, 1.4.1, 1.4.2, and 1.7 exist before wiring real guards around them. If they do not exist, add the tenant-access boundary and tests with fake invokers/readers only; do not invent conversation aggregate, message, participant, reference, or projection behavior in this story.
  - [ ] Read every existing file before editing, especially `src/Hexalith.Conversations.Server/Program.cs`, `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`, `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations`, and all matching test files.

- [ ] Add the Conversations tenant-access decision boundary. (AC: 1, 2, 5)
  - [ ] Add `Server/TenantAccess/IConversationTenantAccessService.cs`, `ConversationTenantAccessService.cs`, `ConversationTenantAccessRequirement.cs`, `ConversationTenantAccessDecision.cs`, and `ConversationTenantAccessDenialReason.cs`.
  - [ ] Back the service with `Hexalith.Tenants.Client.Projections.ITenantProjectionStore`; do not call Tenants synchronously on the hot path and do not trust JWT/request tenant claims alone.
  - [ ] Map Tenants roles conservatively: `TenantReader` permits read only, `TenantContributor` permits read/write, and `TenantOwner` permits read/write/admin unless a later ADR narrows this.
  - [ ] Deny missing tenant id, missing caller/user id, unknown tenant, disabled tenant, missing member, insufficient role, unmapped role, projection store failure, stale/gap/rollback signal, malformed tenant context, and tenant mismatch.
  - [ ] Let `OperationCanceledException` propagate so request cancellation is not converted into an authorization result.

- [ ] Wire the tenant guard before every available command/read path. (AC: 1, 3, 4)
  - [ ] For commands, check tenant access before validation steps that could load aggregate state, before EventStore dispatch, before projection mutation, before publication detail access, and before audit-sensitive metadata access.
  - [ ] For reads/lists, check tenant access before projection lookup, count/facet calculation, pagination cursor resolution, Party hydration, provider correlation lookup, or any existence-sensitive branch.
  - [ ] Reject mismatches between trusted request tenant, command body tenant, route tenant, aggregate/conversation tenant, projection key tenant, and idempotency context tenant before touching state.
  - [ ] Keep the aggregate pure: do not put Tenants calls, authorization decisions, request claims, HTTP context, or projection freshness checks inside `ConversationAggregate`.
  - [ ] Keep `Server/Program.cs` fail-closed unless this story or prior completed stories provides a real safe API bootstrap; do not replace the fail-closed startup with permissive endpoints.

- [ ] Map denials to typed, content-safe errors. (AC: 2-4)
  - [ ] Reuse the Story 1.2 error contract vocabulary where present: `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `command_validation_failed`, and related typed problem/result contracts.
  - [ ] Do not add public synonyms such as `access_denied`, `forbidden_tenant`, or `tenant_expired` unless the contract tests and documentation deliberately update the shared vocabulary.
  - [ ] For unauthorized, nonexistent, unknown-tenant, and cross-tenant conversation cases, return the same safe externally observable shape unless a policy explicitly permits disclosure to the caller.
  - [ ] Include only safe metadata such as bounded reason code, retryability, correlation id, optional audit handle, and documentation pointer. Do not include target tenant id, Party data, conversation title, business reference, provider id, snippets, raw upstream problem details, claims, tokens, or member dictionaries.

- [ ] Register Tenants integration without breaking project boundaries. (AC: 1, 5)
  - [ ] Add the smallest required references to `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts` in the server/test projects using central package or project-reference conventions; do not add these dependencies to `Hexalith.Conversations.Contracts` or the domain aggregate project.
  - [ ] Register `AddHexalithTenants(...)` at the server/application boundary when a real host exists, and map the Tenants subscription endpoint only where the host is safely configured with CloudEvents and Dapr subscribe handling.
  - [ ] Keep the default in-memory `ITenantProjectionStore` test/local only. Production durability, sequence/gap tracking, and freshness SLO metadata require ADR-003 or an approved readiness decision before being treated as complete production behavior.
  - [ ] Do not initialize or update nested submodules. Root-level sibling reads are enough for this story.

- [ ] Add local evidence tests for fail-closed behavior. (AC: 1-5)
  - [ ] Add focused unit tests under `tests/Hexalith.Conversations.Server.Tests/TenantAccess` for role mapping, missing tenant, malformed/blank tenant, missing caller, unknown tenant, disabled tenant, missing member, insufficient role, unmapped role, projection-store exception, cancellation propagation, stale/gap/rollback signal, and projection poisoning.
  - [ ] Add command-boundary tests with fake aggregate loader/dispatcher/projection publisher proving denied writes do not load aggregate state, dispatch commands, emit domain events, mutate projections, or publish tenant-crossing metadata.
  - [ ] Add read-boundary tests with fake projection/read services proving denied reads do not call projection lookup, totals, pagination, hydration, provider metadata, or existence-sensitive branches.
  - [ ] Add adversarial tests for cross-tenant ID guessing and mixed metadata, including route/header/body/aggregate tenant mismatches.
  - [ ] Add contract/content-safety tests proving denial payloads omit protected titles, participant names, snippets, timestamps, counts, pagination gaps, business references, provider correlation metadata, raw tenant ids where disclosure is not allowed, Party personal data, and raw upstream errors.
  - [ ] Add or update boundary tests so forbidden Tenants/Parties/EventStore infrastructure references cannot appear in Contracts or domain projects.

- [ ] Validate the story implementation. (AC: 1-5)
  - [ ] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore` first.
  - [ ] If assets are stale, run `dotnet restore .\Hexalith.Conversations.slnx`, `dotnet build .\Hexalith.Conversations.slnx --no-restore`, and `dotnet test .\Hexalith.Conversations.slnx --no-build`.
  - [ ] Capture validation commands and any deferred readiness gaps in the Dev Agent Record.

## Dev Notes

### Scope Boundary

Story 1.5 owns tenant access enforcement and typed fail-closed rejection semantics for the available Conversations command/read boundaries. It must not implement conversation aggregate behavior, participant/message/reference behavior, read-model materialization, idempotency records, governance/audit pairing, publication, FrontComposer UI, conformance manifest signing, or production tenant projection durability beyond the explicit local evidence needed here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`; `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`]

At story creation time, only Story 1.1 is implemented in code, Story 1.2 is a story file, and Stories 1.3/1.4/1.4.1/1.4.2/1.7 are backlog. If that remains true at implementation time, create the reusable tenant-access boundary and local evidence tests with fakes. Do not pull future domain/read behavior forward just to satisfy "every command/read" wording. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; local repository inspection on 2026-05-18]

Story closure requires local automated evidence for the scenarios in the acceptance criteria. Release-gate tenant-isolation manifest coverage is not part of this story; Story 5.5 consumes this evidence later. [Source: `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`]

### Current Repository State

The current source tree is scaffold-heavy. `src/Hexalith.Conversations.Server/Program.cs` intentionally throws `NotImplementedException` with a fail-closed message. The server project references Contracts and domain only. Contracts and domain contain marker assemblies only unless Story 1.2 or later has run by the time this story is implemented. [Source: `src/Hexalith.Conversations.Server/Program.cs`; `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`; `src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj`; `src/Hexalith.Conversations/Hexalith.Conversations.csproj`]

Current tests use xUnit v3, Shouldly, XML project inspection, solution/disk parity checks, and explicit boundary tests. Preserve copyright headers, namespace style, central package management, `net10.0`, nullable clean code, and warnings-as-errors. [Source: `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`; `Directory.Build.props`; `Directory.Packages.props`; `global.json`]

Story 1.1's review lesson still applies: reflection-only assembly boundary tests can pass vacuously when package references are unused. When adding Tenants references, inspect `.csproj` XML for forbidden references in Contracts/domain, not only compiled assembly references. [Source: `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md#Review Findings`; `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`]

### Tenant Access Architecture

Hexalith.Tenants owns tenant lifecycle, membership, roles, and configuration. Conversations must treat request/JWT tenant context as requested scope only. The local Tenants projection is the authorization source used before aggregate or projection access. Missing, stale, ambiguous, disabled, lagging, rolled-back, deleted, or unavailable tenant state denies access. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/project-context.md#Critical Implementation Rules`]

The architecture requires tenant access before aggregate load, command dispatch, projection read, export, rebuild, admin action, MCP/tool action, background work, and verification detail access. This story should implement the reusable application/server boundary, not sprinkle ad hoc claim checks through controllers. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/planning-artifacts/research/technical-how-to-use-hexalithtenants-to-manage-tenant-isolation-in-hexalithconversations-research-2026-05-10.md#Conversations Integration Blueprint`]

The domain aggregate must remain deterministic and side-effect free. Tenant authorization, Party validation, policy checks, idempotency, and command mapping happen in application handlers before aggregate invocation. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/planning-artifacts/architecture.md#State Management Patterns`]

### Existing Tenants and Parties Patterns to Reuse

`Hexalith.Tenants.Client.Registration.AddHexalithTenants(...)` registers the Tenants client pipeline, `ITenantProjectionStore`, event handlers for tenant lifecycle/membership/configuration events, and `TenantEventProcessor`. Its default store is in-memory if no store is already registered, so production durability must be explicit later. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`; `Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`]

`TenantLocalState` contains `TenantId`, `Name`, `Description`, `Status`, `Members`, and `Configuration`. Conversations should use it for authorization decisions only, not persist tenant membership into conversation events or aggregates. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`]

Tenant events relevant to the access projection include `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved`. Tenant roles are `TenantOwner`, `TenantContributor`, and `TenantReader`; statuses are `Active` and `Disabled`. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events`; `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`; `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs`]

Reuse the Parties authorization pattern as a model, not as a copy-paste source. `Hexalith.Parties.Authorization.TenantAccessService` checks the local projection, fails closed on missing tenant/user, unknown tenant, disabled tenant, missing member, insufficient role, unmapped role, and projection-store failure, and lets cancellation propagate. The test matrix in `TenantAccessServiceTests` and `HelperDrivenTenantAccessTests` is the closest local template for this story. [Source: `Hexalith.Parties/src/Hexalith.Parties/Authorization/TenantAccessService.cs`; `Hexalith.Parties/tests/Hexalith.Parties.Tests/Authorization/TenantAccessServiceTests.cs`; `Hexalith.Parties/tests/Hexalith.Parties.Tests/Authorization/HelperDrivenTenantAccessTests.cs`]

Do not blindly copy the later Parties request-path caveat. Parties currently keeps `ITenantAccessService` out of some command/query gateway paths because EventStore owns those checks there. Conversations architecture explicitly requires Conversations tenant access before conversation aggregate/projection access unless a new ADR changes that boundary. [Source: `Hexalith.Parties/src/Hexalith.Parties/Extensions/PartiesServiceCollectionExtensions.cs`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]

### Typed Error and Non-Disclosure Rules

Story 1.2 defines the contract vocabulary this story should reuse: `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `aggregate_not_found`, `schema_version_unsupported`, and `command_validation_failed`. Story 1.5 should not invent new public error codes unless the contract package, tests, and documentation are updated coherently. [Source: `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md#Typed Error and Trust Vocabulary`; `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`]

Unauthorized, nonexistent, hidden-by-tenant-isolation, and cross-tenant records must be externally indistinguishable for non-privileged callers unless an explicit policy permits disclosure. Reads must not leak existence through titles, participant names, snippets, timestamps, counts, pagination gaps, business references, provider metadata, empty-state wording, response time assumptions, or field-level diagnostics. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`; `_bmad-output/planning-artifacts/ux-design-specification.md#AC-SAFE-001`]

Errors and logs may include bounded reason codes, correlation id, operation class, retryability, and safe diagnostic category. They must not include raw tenant claims, member dictionaries, user tokens, Party personal data, provider payloads, conversation content, redacted content, or raw upstream error bodies. [Source: `_bmad-output/planning-artifacts/architecture.md#Metadata-Only Definition`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### File Structure Guidance

Likely production files for this story belong under:

- `src/Hexalith.Conversations.Server/TenantAccess`
- `src/Hexalith.Conversations.Server/Authorization`
- `src/Hexalith.Conversations.Server/Api` or `CommandHandlers` only if real command/read boundaries exist
- `src/Hexalith.Conversations.Contracts/Errors` or `Results` only for small contract vocabulary adjustments owned by Story 1.5

Likely tests belong under:

- `tests/Hexalith.Conversations.Server.Tests/TenantAccess`
- `tests/Hexalith.Conversations.Server.Tests/Authorization`
- `tests/Hexalith.Conversations.IntegrationTests` for project-boundary or DI wiring checks
- `tests/Hexalith.Conversations.Contracts.Tests` only if typed error/result contracts are changed

Do not put Tenants client or server infrastructure references into `src/Hexalith.Conversations.Contracts` or `src/Hexalith.Conversations`. Contracts must remain infrastructure-free, and domain logic must not know Tenants exists. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `_bmad-output/project-context.md#Critical Implementation Rules`]

### Testing Guidance

The minimum local evidence is a matrix, not a single happy-path test. Include positive role mapping and adversarial denials. Denial tests must prove the side effect did not happen by using fakes/spies with explicit invocation counters for aggregate load, EventStore dispatch, projection read, projection mutation, Party hydration, publication, and metadata lookup. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`; `_bmad-output/planning-artifacts/architecture.md#Architecture Verification Strategy`]

Test projection poisoning and order/freshness signals conservatively. The current Tenants `ITenantProjectionStore` does not expose sequence or freshness metadata by itself, but the architecture and readiness decisions require fail-closed handling for stale, lagging, rolled-back, gap-detected, or poisoned state. If implementation cannot model this through the current store contract, add a Conversations-owned wrapper/result abstraction and fake store that can signal these conditions. Production-grade durability/freshness remains ADR-003 work. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#Projection freshness blocking semantics`; `_bmad-output/planning-artifacts/architecture.md#ADR Backlog Created By Core Decisions`]

When tests use Tenants data, prefer public Tenants testing helpers and projection fakes from the sibling modules where practical. Avoid constructing authorization success from only a JWT claim or raw dictionary, because that hides the core invariant that Tenants membership is the source of truth. [Source: `Hexalith.Parties/tests/Hexalith.Parties.Tests/Authorization/HelperDrivenTenantAccessTests.cs`; `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`]

### Latest Technical Notes

.NET 10 is the project baseline and is currently a Long Term Support release supported until November 2028. Keep `global.json` pinned to `10.0.300` and target `net10.0`; do not downgrade framework or inline package versions to make local tooling pass. [Source: Microsoft Learn, .NET releases and support, checked 2026-05-18: `https://learn.microsoft.com/en-us/dotnet/core/releases-and-support`; `global.json`; `Directory.Build.props`]

Dapr pub/sub is at-least-once, so Tenants event handlers and projection updates must tolerate duplicate delivery. Dapr supports dead-letter topics for messages that cannot be delivered after retry policy handling; tenant-event poison handling must fail closed and be operationally visible. [Source: Dapr Docs, Publish and subscribe overview, checked 2026-05-18: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/`; Dapr Docs, Dead Letter Topics, checked 2026-05-18: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/`]

Aspire AppHost remains local orchestration only. This story should not require Aspire runtime launch, Dapr sidecars, tenant seed data, provider credentials, production secrets, external cloud resources, or nested submodule initialization for unit-level validation. [Source: Microsoft Learn, Aspire AppHost overview, checked 2026-05-18: `https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview`; `README.md`]

### Anti-Reinvention Warnings

- Do not create a Conversations-owned tenant membership table as source of truth.
- Do not make request/JWT tenant claims sufficient authorization.
- Do not call Hexalith.Tenants synchronously on every hot request as the primary authorization path.
- Do not put authorization inside `ConversationAggregate`.
- Do not leak tenant access decisions through read counts, pagination, not-found wording, logs, telemetry, autocomplete, or timing assumptions.
- Do not add fallback "admin only" or tool/MCP paths that skip the same tenant gate.
- Do not store Party display names, tenant membership, provider session authority, message content, or raw upstream error data in denials, logs, durable events, or projections.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`
- `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`
- `_bmad-output/planning-artifacts/architecture.md#Process Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- `_bmad-output/planning-artifacts/prd.md#Error Codes & Failure Modes`
- `_bmad-output/planning-artifacts/ux-design-specification.md#AC-SAFE-001`
- `_bmad-output/planning-artifacts/research/technical-how-to-use-hexalithtenants-to-manage-tenant-isolation-in-hexalithconversations-research-2026-05-10.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md`
- `_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md`
- `_bmad-output/project-context.md`
- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`
- `Hexalith.Parties/src/Hexalith.Parties/Authorization/TenantAccessService.cs`
- `Hexalith.Parties/tests/Hexalith.Parties.Tests/Authorization/TenantAccessServiceTests.cs`
- `Hexalith.Parties/tests/Hexalith.Parties.Tests/Authorization/HelperDrivenTenantAccessTests.cs`
- `src/Hexalith.Conversations.Server/Program.cs`
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
