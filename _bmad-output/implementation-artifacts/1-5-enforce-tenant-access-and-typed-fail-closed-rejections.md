# Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections

Status: review

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
5. Given tenant access tests run, when positive and adversarial cases execute, then tests cover missing tenant, malformed tenant, stale projection, unavailable projection store, disabled tenant, non-member caller, insufficient role, unknown future Tenants role/status values, cross-tenant ID guessing, mixed-tenant command metadata, guard-bypass attempts through non-HTTP callers, and projection poisoning, and failures are verified before aggregate or projection access.

## Tasks / Subtasks

- [x] Confirm implementation preconditions and preserve scope. (AC: 1-5)
  - [x] Verify the contract types from Story 1.2 exist or implement only the minimum error/result additions that Story 1.5 owns without replacing Story 1.2.
  - [x] Verify the aggregate/command/read surfaces from Stories 1.3, 1.4, 1.4.1, 1.4.2, and 1.7 exist before wiring real guards around them. If they do not exist, add the tenant-access boundary and tests with fake invokers/readers only; do not invent conversation aggregate, message, participant, reference, or projection behavior in this story.
  - [x] Read every existing file before editing, especially `src/Hexalith.Conversations.Server/Program.cs`, `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`, `src/Hexalith.Conversations.Contracts`, `src/Hexalith.Conversations`, and all matching test files.

- [x] Add the Conversations tenant-access decision boundary. (AC: 1, 2, 5)
  - [x] Add `Server/TenantAccess/IConversationTenantAccessService.cs`, `ConversationTenantAccessService.cs`, `ConversationTenantAccessRequirement.cs`, `ConversationTenantAccessDecision.cs`, and `ConversationTenantAccessDenialReason.cs`.
  - [x] Model access decisions as a single server/application boundary contract with allowed/denied state, requirement, tenant id, optional caller/principal id, internal denial reason, retryability, and projection version/watermark metadata when available; do not include Party personal data, provider metadata, conversation content, or raw tenant membership dictionaries.
  - [x] Back the service with `Hexalith.Tenants.Client.Projections.ITenantProjectionStore`; do not call Tenants synchronously on the hot path and do not trust JWT/request tenant claims alone.
  - [x] Map Tenants roles conservatively: `TenantReader` permits read only, `TenantContributor` permits read/write, and `TenantOwner` permits read/write/admin unless a later ADR narrows this.
  - [x] Deny missing tenant id, malformed tenant id, missing caller/user id, unknown tenant, disabled tenant, missing member, insufficient role, unmapped role, projection store failure, null or malformed projection record, duplicate or ambiguous membership, stale/gap/rollback signal when the store or wrapper exposes it, and tenant mismatch.
  - [x] Compare trusted request tenant, route tenant, command body tenant, projection tenant, aggregate/conversation tenant, and idempotency tenant using one canonical tenant identifier representation; reject blank, unparseable, differently prefixed, or lossy string-normalized values before any protected delegate is invoked.
  - [x] Treat Tenants role and status mapping as closed-world: only explicitly supported `TenantReader`, `TenantContributor`, `TenantOwner`, `Active`, and `Disabled` states may influence access; unknown additions, duplicate records, or contradictory projection values deny until deliberately mapped.
  - [x] Let `OperationCanceledException` propagate so request cancellation is not converted into an authorization result.

- [x] Wire the tenant guard before every available command/read path. (AC: 1, 3, 4)
  - [x] Implement guards as command handler decorators/pipeline behavior before command invokers and query/read service decorators before projection readers where those seams exist; if no shared seam exists yet, guard each available command, read, publication-detail, and audit-sensitive boundary individually.
  - [x] For commands, check tenant access before validation steps that could load aggregate state, before EventStore dispatch, before projection mutation, before publication detail access, and before audit-sensitive metadata access.
  - [x] For reads/lists, check tenant access before projection lookup, count/facet calculation, pagination cursor resolution, Party hydration, provider correlation lookup, or any existence-sensitive branch.
  - [x] Reject mismatches between trusted request tenant, command body tenant, route tenant, aggregate/conversation tenant, projection key tenant, and idempotency context tenant before touching state.
  - [x] Keep the aggregate pure: do not put Tenants calls, authorization decisions, request claims, HTTP context, or projection freshness checks inside `ConversationAggregate`.
  - [x] Keep `Server/Program.cs` fail-closed unless this story or prior completed stories provides a real safe API bootstrap; do not replace the fail-closed startup with permissive endpoints. Protected routes must not succeed when tenant access services are absent; runtime projection-store unavailability must become a typed fail-closed denial, not a fallback allow path.
  - [x] Ensure HTTP endpoints, background processors, tool/MCP entry points, test-only invokers, and future application services cannot bypass the same tenant gate when they call a guarded command/read delegate; middleware-only authorization is insufficient evidence for this story.

- [x] Map denials to typed, content-safe errors. (AC: 2-4)
  - [x] Reuse the Story 1.2 error contract vocabulary where present: `tenant_binding_missing`, `tenant_isolation_violation`, `tenant_projection_stale`, `command_validation_failed`, and related typed problem/result contracts.
  - [x] Do not add public synonyms such as `access_denied`, `forbidden_tenant`, or `tenant_expired` unless the contract tests and documentation deliberately update the shared vocabulary.
  - [x] For unauthorized, nonexistent, unknown-tenant, disabled-tenant, projection-unavailable, stale-projection, and cross-tenant conversation cases, return the same safe externally observable status/body/header/pagination shape unless a policy explicitly permits disclosure to the caller.
  - [x] Keep internal denial reasons precise for tests, telemetry, audit handles, and server diagnostics, but do not expose those internal reasons publicly unless an existing Story 1.2 contract explicitly defines the stable public code.
  - [x] Include only safe metadata such as bounded reason code, retryability, correlation id, optional audit handle, and documentation pointer. Do not include target tenant id, Party data, conversation title, business reference, provider id, snippets, raw upstream problem details, claims, tokens, or member dictionaries.
  - [x] Make public retryability and diagnostics non-disclosing: they may communicate a generic retry-safe category only when that category cannot distinguish unauthorized, nonexistent, hidden, stale, or unavailable protected records for the caller.

- [x] Register Tenants integration without breaking project boundaries. (AC: 1, 5)
  - [x] Add the smallest required references to `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts` in the server/test projects using central package or project-reference conventions; do not add these dependencies to `Hexalith.Conversations.Contracts` or the domain aggregate project.
  - [x] Register `AddHexalithTenants(...)` at the server/application boundary when a real host exists, and map the Tenants subscription endpoint only where the host is safely configured with CloudEvents and Dapr subscribe handling.
  - [x] Keep the default in-memory `ITenantProjectionStore` test/local only. Production durability, sequence/gap tracking, and freshness SLO metadata require ADR-003 or an approved readiness decision before being treated as complete production behavior.
  - [x] Do not initialize or update nested submodules. Root-level sibling reads are enough for this story.

- [x] Add local evidence tests for fail-closed behavior. (AC: 1-5)
  - [x] Add focused unit tests under `tests/Hexalith.Conversations.Server.Tests/TenantAccess` for role mapping, missing tenant, malformed/blank tenant, missing caller, unknown tenant, disabled tenant, missing member, insufficient role, unmapped role, projection-store exception, cancellation propagation, stale/gap/rollback signal, and projection poisoning.
  - [x] Add command-boundary tests with fake aggregate loader/dispatcher/event appender/projection publisher proving denied writes do not load aggregate state, dispatch commands, emit domain events, mutate projections, or publish tenant-crossing metadata.
  - [x] Add read-boundary tests with fake projection/read services proving denied reads do not call projection lookup, totals, pagination, hydration, provider metadata, or existence-sensitive branches.
  - [x] Add adversarial tests for cross-tenant ID guessing and mixed metadata, including route/header/body/aggregate tenant mismatches.
  - [x] Add bypass tests proving every available command/read entry point shares the same tenant-access decorator or guard, including direct service calls that skip ASP.NET middleware and any local/test harness seam introduced by this story.
  - [x] Add contract/content-safety tests proving denial payloads omit protected titles, participant names, snippets, timestamps, counts, pagination gaps, headers or metadata that reveal existence, business references, provider correlation metadata, raw tenant ids where disclosure is not allowed, Party personal data, and raw upstream errors.
  - [x] Add observability privacy tests proving logs, metrics, traces, exception messages, activity tags, and audit handles use bounded safe reason categories and correlation ids without raw tenant ids, member dictionaries, caller tokens, Party data, conversation content, business references, provider metadata, or upstream problem bodies.
  - [x] Add or update boundary tests so forbidden Tenants/Parties/EventStore infrastructure references cannot appear in Contracts or domain projects.

- [x] Validate the story implementation. (AC: 1-5)
  - [x] Run `dotnet test .\Hexalith.Conversations.slnx --no-restore` first.
  - [x] If assets are stale, run `dotnet restore .\Hexalith.Conversations.slnx`, `dotnet build .\Hexalith.Conversations.slnx --no-restore`, and `dotnet test .\Hexalith.Conversations.slnx --no-build`.
  - [x] Capture validation commands and any deferred readiness gaps in the Dev Agent Record.

## Dev Notes

### Scope Boundary

Story 1.5 owns tenant access enforcement and typed fail-closed rejection semantics for the available Conversations command/read boundaries. It must not implement conversation aggregate behavior, participant/message/reference behavior, read-model materialization, idempotency records, governance/audit pairing, publication, FrontComposer UI, conformance manifest signing, or production tenant projection durability beyond the explicit local evidence needed here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: Enforce Tenant Access and Typed Fail-Closed Rejections`; `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`]

At story creation time, only Story 1.1 is implemented in code, Story 1.2 is a story file, and Stories 1.3/1.4/1.4.1/1.4.2/1.7 are backlog. If that remains true at implementation time, create the reusable tenant-access boundary and local evidence tests with fakes. Do not pull future domain/read behavior forward just to satisfy "every command/read" wording. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; local repository inspection on 2026-05-18]

Story closure requires local automated evidence for the scenarios in the acceptance criteria. Release-gate tenant-isolation manifest coverage is not part of this story; Story 5.5 consumes this evidence later. [Source: `_bmad-output/planning-artifacts/epics.md#Two-Level Evidence Rules`]

### Pre-Dev Party-Mode Review Decisions

The 2026-05-18 party-mode review clarified Story 1.5 without changing product scope:

- Story 1.5 must use one Conversations-owned server/application authorization boundary (`IConversationTenantAccessService`) instead of ad hoc claim checks in handlers, controllers, projections, or aggregate code.
- JWT/request tenant claims can identify requested context, but the local Tenants projection remains the authorization source. Missing, unavailable, malformed, disabled, unknown, stale, lagging, rolled-back, ambiguous, mismatched, or poisoned projection state denies access.
- Internal denial reasons are allowed and expected for tests, server telemetry, audit handles, and diagnostics. Public adopter-facing responses must reuse Story 1.2 contract vocabulary and collapse unauthorized, nonexistent, disabled-tenant, unavailable-projection, stale-projection, and cross-tenant cases into the same non-disclosing externally observable shape unless an explicit disclosure policy exists.
- Public response indistinguishability includes status, body shape, problem title/type, headers, counts, pagination behavior, empty-state wording, and safe metadata. Correlation or audit handles must not reveal tenant identity, conversation existence, Party data, provider correlation data, business references, or upstream problem details.
- Denied writes must prove no side effects before authorization: no aggregate load, command dispatch, event append, projection mutation, publication detail access, metadata lookup, or tenant-crossing publication.
- Denied reads/lists must prove no existence-sensitive work before authorization: no projection lookup, totals/facet calculation, pagination cursor resolution, Party hydration, provider correlation lookup, or content-derived branch.
- If the current Tenants projection contract does not expose freshness, gap, rollback, or poisoning signals directly, Story 1.5 may add a Conversations-owned wrapper/result abstraction and tests that fake those signals. The long-term production freshness SLA, durability, and cache strategy remain deferred.
- Admin/audit-sensitive access in this story is limited to currently available surfaces or fake guarded seams for local evidence. Do not implement publication, audit UX, or future admin product behavior to satisfy this story.

Suggested denial evidence matrix:

| Condition | Internal denial reason | Public Story 1.2 mapping | Protected delegate invoked? | Events/projections/publication metadata? |
|---|---|---|---|---|
| missing tenant id | `MissingTenant` | same non-disclosing denial shape | no | none |
| malformed tenant id | `MalformedTenant` | same non-disclosing denial shape | no | none |
| missing caller/user id | `MissingCaller` | same non-disclosing denial shape | no | none |
| projection unavailable or throws | `TenantAccessUnavailable` | same non-disclosing denial shape | no | none |
| stale, lagging, gap, or rollback signal | `TenantAccessStale` | same non-disclosing denial shape | no | none |
| unknown tenant or null projection | `UnknownTenant` | same non-disclosing denial shape | no | none |
| disabled tenant | `TenantDisabled` | same non-disclosing denial shape | no | none |
| no membership, insufficient role, or unmapped role | `TenantAccessDenied` | same non-disclosing denial shape | no | none |
| cross-tenant target or tenant mismatch | `TenantAccessDenied` | same non-disclosing denial shape | no | none |
| ambiguous, duplicate, malformed, or poisoned projection state | `TenantAccessAmbiguous` | same non-disclosing denial shape | no | none |

Deferred decisions from the review: exact long-term tenant projection freshness SLA, distributed decision cache strategy, admin/audit UX copy, localization of denial messages, any explicit existence-disclosure policy, and any Conversations-specific role model beyond Tenants `TenantReader`, `TenantContributor`, and `TenantOwner`.

### Advanced Elicitation Hardening

The 2026-05-19 advanced elicitation pass kept Story 1.5 within the party-reviewed scope and clarified failure modes that could otherwise become implementation shortcuts:

- Tenant equality must be canonical and lossless across route, header/request context, command body, projection key, aggregate/conversation identity, and idempotency context. A string comparison that trims, lowercases, strips prefixes, or accepts incompatible identifier forms is not sufficient evidence.
- Role and status handling must be explicit and closed-world. Future Tenants enum values, duplicated membership records, contradictory projection state, or partially deserialized records deny until a later story or ADR deliberately expands the mapping.
- The guard must be reusable below transport middleware. ASP.NET authorization alone does not satisfy the story if background processors, tool/MCP actions, direct application services, local test harnesses, or future command/query invokers can reach protected delegates without `IConversationTenantAccessService`.
- Public retryability, diagnostics, logs, metrics, traces, exception text, and audit handles must not become secondary disclosure channels. They can carry bounded categories and correlation handles only when those values do not reveal tenant identity, membership, conversation existence, Party data, provider metadata, business references, or upstream failures.
- The story still does not require distributed transactions, production-grade tenant freshness durability, policy authoring UI, new public disclosure policy, custom Conversations roles, or cross-module conformance manifests. Those remain deferred decisions unless an explicit ADR/story pulls them in.

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

GPT-5 Codex

### Debug Log References

- `dotnet test .\tests\Hexalith.Conversations.Server.Tests\Hexalith.Conversations.Server.Tests.csproj --no-restore` failed red because `Hexalith.Conversations.Server.TenantAccess` did not exist yet.
- `dotnet test .\tests\Hexalith.Conversations.Server.Tests\Hexalith.Conversations.Server.Tests.csproj` passed after implementation: 51 passed.
- `dotnet test .\Hexalith.Conversations.slnx --no-restore` passed: Contracts 77, Client 1, Server 51, Domain 56, Integration 8.
- `dotnet restore .\Hexalith.Conversations.slnx` passed after Tenants references were added.
- `dotnet build .\Hexalith.Conversations.slnx --no-restore` passed with 0 warnings and 0 errors.
- `dotnet build .\Hexalith.Conversations.slnx --no-restore` passed after stricter closed-world role validation with 0 warnings and 0 errors.
- `dotnet test .\Hexalith.Conversations.slnx --no-build` passed after stricter closed-world role validation: Contracts 77, Client 1, Server 52, Domain 56, Integration 8.

### Implementation Plan

- Added a Conversations-owned server tenant access boundary backed by `ITenantProjectionStore`, with closed-world Tenants role/status mapping, canonical tenant comparison, optional projection health signals, content-safe decision/rejection mapping, and cancellation propagation.
- Added a shared `ConversationTenantAccessGuard` and rewired the available participant command handler to load aggregate state and call Party validation only after tenant access succeeds.
- Kept `Program.cs` fail-closed; added DI registration through `AddConversationTenantAccess()` for the future real host boundary, without mapping Dapr subscription endpoints in the current non-host startup.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented Story 1.5 tenant access boundary in `src/Hexalith.Conversations.Server/TenantAccess`.
- Wired `AddParticipantCommandHandler` through the tenant guard before state loading, Party validation, and aggregate dispatch.
- Added local evidence tests for role mapping, missing/malformed tenant, missing caller, unknown/disabled tenant, missing member, insufficient/unmapped role, projection exceptions, cancellation propagation, stale/gap/rollback/poison signals, cross-tenant mismatches, denied write/read delegate bypass prevention, content-safe errors, safe logging, DI registration, and project-boundary references.
- Deferred production tenant projection durability, freshness SLOs, distributed decision caching, explicit existence-disclosure policy, and subscription endpoint mapping until the approved host/ADR work exists.

### File List

- `_bmad-output/implementation-artifacts/1-5-enforce-tenant-access-and-typed-fail-closed-rejections.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs`
- `src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessDecision.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessDenialReason.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessGuard.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessRequirement.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessService.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantProjectionHealth.cs`
- `src/Hexalith.Conversations.Server/TenantAccess/IConversationTenantAccessService.cs`
- `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/AddParticipantCommandHandlerTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj`
- `tests/Hexalith.Conversations.Server.Tests/ServerBoundaryTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/AddParticipantCommandHandlerTenantAccessTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantAccessGuardTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantAccessRegistrationTest.cs`
- `tests/Hexalith.Conversations.Server.Tests/TenantAccess/ConversationTenantAccessServiceTest.cs`

## Party-Mode Review

- Date/time: 2026-05-18T14:04:45Z
- Selected story key: `1-5-enforce-tenant-access-and-typed-fail-closed-rejections`
- Command/skill invocation used: `/bmad-party-mode 1-5-enforce-tenant-access-and-typed-fail-closed-rejections; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: Reviewers converged on contract drift risk around Story 1.2 error vocabulary, public denial-shape indistinguishability, projection freshness and poisoning signals, explicit guard placement, role-state constraints, fail-closed bootstrap behavior, and proof tests that denied requests do not invoke protected delegates or leak through secondary signals.
- Changes applied: Added a single boundary decision-shape requirement; clarified local projection authority and no synchronous Tenants fallback; expanded denial conditions; named guard placement seams; clarified fail-closed startup/runtime behavior; separated internal denial reasons from public response mapping; expanded content-safety and side-effect proof tests; added a denial evidence matrix and deferred decision list.
- Findings deferred: Exact production freshness SLA/watermark policy, distributed tenant-decision cache strategy, admin/audit UX wording, localization of denial messages, explicit existence-disclosure exceptions, and any Conversations-specific role model beyond Tenants roles.
- Final recommendation: ready-for-dev

## Change Log

- 2026-05-18: Story created and moved to ready-for-dev by BMAD create-story workflow.
- 2026-05-18: Party-mode review completed; denial mapping, guard placement, projection-state, and non-disclosure clarifications applied.
- 2026-05-19: Advanced elicitation applied canonical tenant equality, closed-world role/status, guard-bypass, public retryability, and observability privacy clarifications.
- 2026-05-19: Implemented tenant access boundary, guarded participant command path, Tenants registration, fail-closed tests, and validation evidence; story moved to review.

## Advanced Elicitation

- Date/time: 2026-05-19T00:03:58Z
- Selected story key: `1-5-enforce-tenant-access-and-typed-fail-closed-rejections`
- Command/skill invocation used: `/bmad-advanced-elicitation 1-5-enforce-tenant-access-and-typed-fail-closed-rejections`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; Critique and Refine
- Reshuffled Batch 2 method names: First Principles Analysis; Pre-mortem Analysis; Architecture Decision Records; Socratic Questioning; User Persona Focus Group
- Findings summary: Elicitation found that the story was already directionally strong, but implementers could still pass tests with brittle string-based tenant comparisons, open-ended role/status mapping, middleware-only guards, retryability or diagnostics that reveal protected state, and observability surfaces that leak identifiers or upstream details.
- Changes applied: Clarified canonical tenant identifier comparison across all tenant-bearing inputs; added closed-world Tenants role/status handling; required shared guard coverage beyond HTTP middleware; constrained public retryability and diagnostics; expanded bypass and observability privacy evidence tests; added an advanced hardening note preserving deferred decision boundaries.
- Findings deferred: Distributed tenant-decision cache strategy, production freshness durability/SLO, custom Conversations roles, explicit existence-disclosure policy, policy/admin UX copy and localization, distributed transaction semantics between authorization and dispatch, and release-level conformance manifest coverage.
- Final recommendation: ready-for-dev
