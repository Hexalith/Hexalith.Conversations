---
project_name: 'Hexalith.Conversations'
user_name: 'Jerome'
date: '2026-05-10'
sections_completed: ['discovery', 'technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'code_quality_rules', 'workflow_rules', 'critical_rules']
existing_patterns_found: 11
status: 'complete'
rule_count: 103
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- C# / .NET `10.0`; sibling Hexalith modules pin SDK `10.0.300` and target `net10.0`.
- New Conversations projects should mirror sibling module defaults: nullable enabled, implicit usings enabled, warnings as errors.
- Use Central Package Management through `Directory.Packages.props`; do not add package versions directly in `.csproj` files unless matching an existing local exception.
- Core Hexalith dependencies: `Hexalith.EventStore` for event-sourced persistence, `Hexalith.Tenants` for tenant source of truth, `Hexalith.Parties` for stable participant identity, and `Hexalith.FrontComposer` for admin UI contracts.
- Key package families currently used nearby: Dapr `1.17.7`, Aspire `13.2.x`, MediatR `14.1.0`, FluentValidation `12.1.1`, Microsoft.Extensions `10.0.x`, OpenTelemetry `1.15.x`, Fluent UI Blazor `5.0.0-rc.2-26098.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0` or `6.0.0-rc.1`, Testcontainers `4.10.0`.
- Prefer current local workspace modules over deprecated public/legacy packages, especially for Parties.
- Treat EventStore, Tenants, Parties, and FrontComposer as bounded-context dependencies; do not copy their contracts or reimplement their runtime behavior inside Conversations.

## Critical Implementation Rules

### Language-Specific Rules

- Use `net10.0`, nullable reference types, implicit usings, and warnings as errors for new C# projects.
- Favor modern C# records and `required` members for contracts when that matches sibling module patterns.
- Public contracts must be serialization-friendly and explicit about required values; validate identity/correlation fields eagerly at boundaries where possible.
- Keep project boundaries clean: `Contracts` must not reference server infrastructure, Dapr implementation details, HTTP clients, EventStore server packages, or UI shell packages.
- Domain/server code may depend on EventStore client/runtime packages; public client packages must expose Conversations concepts, not raw EventStore envelopes.
- Wrap `Hexalith.Parties.Client` behind a Conversations-owned adapter such as `IParticipantDirectory`; never call Parties from aggregate logic.
- Fail closed for authorization, tenant projection failures, unknown tenant/member state, stale state, and participant validation failures.
- Map denial and upstream failures to Conversations-safe problem/status types; do not leak raw Parties/Tenants details that could reveal tenant or personal data.
- Preserve payload secrecy in logs and `ToString()` methods; follow EventStore's payload-redaction precedent.
- Model the first aggregate as `Conversation`, not `Message`; EventStore actors serialize writes per aggregate identity.
- Aggregate handlers should follow the EventStore style: `Handle(Command, State?) -> DomainResult`.
- Store stable upstream IDs in durable events, not upstream-owned state: `PartyId`, `ProjectId`, `FolderId`, file references, and tenant scope.
- Do not persist Party display names, contact values, identifiers, person details, or organization details in conversation events unless an approved governance decision requires an immutable audit snapshot.

### Framework-Specific Rules

- Use `Hexalith.EventStore` as the authoritative write-side persistence path; do not create direct transcript tables or module-owned event storage.
- Implement `ConversationAggregate : EventStoreAggregate<ConversationState>` and emit domain events for meaningful state changes.
- Let EventStore own routing, actor identity, persistence, snapshots, publication, projection invalidation, and command status.
- Do not expose raw EventStore command envelopes, aggregate IDs, snapshot mechanics, SignalR groups, or projection internals as the primary adopter API.
- Define Conversations' idempotency contract explicitly before coding write APIs; local EventStore docs contain mixed wording around command submission idempotency.
- `Hexalith.Tenants` owns tenant lifecycle, membership, roles, and configuration.
- Conversations must keep a local, event-fed tenant access projection and fail closed when state is missing, stale, ambiguous, disabled, or unavailable.
- Authorize tenant/user/operation before any aggregate load, command dispatch, projection read, admin action, or tool/MCP operation.
- Do not trust JWT tenant claims alone; claims provide requested context, while the local Tenants projection decides access.
- Tenant event handlers must be idempotent; production projections should track freshness and sequence/gap behavior.
- `Hexalith.Parties` owns Party identity and personal data; Conversations owns participant membership and attribution within a conversation.
- Validate Party IDs at application/API boundaries, then pass stable references into commands.
- Hydrate display/status data at read time through a Conversations adapter over `Hexalith.Parties.Client`.
- Writes fail closed when Parties cannot validate a new participant; authorized reads may deliberately degrade display hydration if policy allows.
- Use contract-first FrontComposer annotations for admin commands/projections; generate baseline UI instead of hand-building a separate portal.
- Never hand-edit generated FrontComposer files under `obj/`.
- Generated command payloads must not include tenant identity, user identity, tokens, claims, or host authorization context.
- Admin UI should consume Conversations commands/projections and freshness states; it must not browse raw EventStore streams as the governance UI.

### Testing Rules

- Follow sibling module layout: `tests/Hexalith.Conversations.*.Tests` beside `src/Hexalith.Conversations.*`.
- Use focused unit tests for aggregates, contracts, validators, adapters, and access services.
- Use integration tests for command/query HTTP paths, Tenants subscription wiring, EventStore command flow, and FrontComposer generated contract registration.
- Keep conformance-style tests explicit and named; tenant isolation, audit pairing, idempotency, and redaction replay are release-gate concerns.
- Use xUnit v3, Shouldly, NSubstitute, and Testcontainers as in sibling modules.
- Prefer Hexalith testing helpers where available: `Hexalith.EventStore.Testing`, `Hexalith.Tenants.Testing`, and local in-memory fakes.
- Reuse Parties/Tenants authorization test patterns instead of inventing new authorization fakes.
- Do not mock inside aggregate logic; aggregate tests should be pure command/state/event tests.
- Tenant access tests must cover missing tenant, unknown tenant, disabled tenant, missing user, non-member, insufficient role, stale projection, projection-store failure, duplicate/out-of-order tenant events, and event-driven revocation.
- EventStore flow tests must cover command idempotency, correlation/causation propagation, expected event sequence, no-op/rejection paths, projection rebuild safety, and duplicate/replayed event handling.
- Governance tests must prove every retention/redaction/sensitive-data command emits an auditable domain event with rationale, and no path mutates governance state without required audit pairing.
- Privacy tests must assert Conversations events do not serialize Parties personal-data objects such as `PersonDetails`, contact channels, identifiers, names, or raw upstream problem details.
- Admin UI tests must cover generated FrontComposer metadata, authorization boundaries, projection freshness/degraded states, redaction display, and absence of generated-file edits.

### Code Quality & Style Rules

- Keep nullable clean and warnings-as-errors clean; do not suppress warnings broadly.
- Respect Central Package Management and existing `Directory.Build.props` patterns.
- Keep generated output out of hand-authored source unless a local convention explicitly commits generated artifacts.
- Add comments only for domain invariants, security/audit reasoning, or non-obvious EventStore/Tenants behavior.
- Use the Hexalith project shape: `Contracts`, `Client`, `Server` or `CommandApi`, `Aspire`, `AppHost`, `ServiceDefaults`, `Testing`, and focused test projects.
- Name durable domain concepts in Conversations language, not substrate language: `Conversation`, `Participant`, `Message`, `RetentionPolicy`, `Redaction`, `SensitiveData`.
- Keep adapter names explicit, such as `PartiesParticipantDirectory`, `ConversationTenantAccessService`, and `EventStoreConversationCommandDispatcher`.
- Use stable IDs consistently: `TenantId`, `ConversationId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`.
- ADRs are required for load-bearing choices: idempotency contract, schema evolution/upcasting, tenant projection durability/freshness, governance audit enforcement, and provider portability proof.
- Public contracts need enough XML docs or README/API guidance for adopters to use them without learning EventStore internals.
- Do not duplicate the PRD in code comments; encode rules as tests, validators, or clear contract types.
- Keep aggregate state and event application deterministic and replay-safe.
- Keep authorization/application orchestration outside aggregate logic.
- Keep upstream hydration and UI display composition outside durable events.
- Prefer small adapters over leaking external client APIs throughout handlers.

### Development Workflow Rules

- Treat Conversations as a greenfield module inside a brownfield Hexalith ecosystem.
- Never initialize or update nested submodules recursively; initialize/update only root-level submodules unless nested submodules are explicitly requested.
- Do not use `git submodule update --init --recursive`.
- Keep changes scoped to Conversations artifacts unless the task explicitly asks to modify EventStore, Tenants, Parties, FrontComposer, or other sibling modules.
- Do not copy code from sibling modules when a project/package reference or adapter is the correct boundary.
- PRD/planning artifacts under `_bmad-output/planning-artifacts` are binding context until superseded.
- Open questions in the PRD must not be silently assumed closed by implementation.
- Carry-forward callouts about audit invariants, fail-closed tenant isolation, idempotency, schema evolution, redaction replay, and operator proof obligations are implementation constraints.
- If architecture and PRD wording conflict, prefer canonical PRD scope vocabulary and the latest approved architecture/story artifact.
- Start foundation slices with tenant access, EventStore aggregate shape, idempotency, and audit enforcement before UI polish or optional integrations.
- Add tests with each feature, especially for tenant isolation and governance behavior.
- When touching generated FrontComposer output, change contract annotations or generator inputs, not generated files.
- For local dependency investigation, read source/docs from sibling modules; avoid making incidental changes there.

### Critical Don't-Miss Rules

- Do not implement Conversations as a chatbot transcript table.
- Do not make the chatbot the owner of conversation persistence.
- Do not use provider chat/session IDs as durable conversation identity; provider IDs are correlation metadata only.
- Do not bypass EventStore for writes, governance state changes, or audit-related mutations.
- Do not put authorization, tenant lookups, HTTP calls, Parties calls, or UI shaping inside aggregate logic.
- Do not store upstream personal data from Parties in durable conversation events.
- Do not expose raw EventStore mechanics as the adopter-facing integration path.
- Do not let admin/MCP/tool paths bypass the same tenant and resource gates as REST/browser paths.
- Tenant access must fail closed on missing, stale, unavailable, disabled, ambiguous, or insufficient projection state.
- Cross-tenant access must be impossible by construction and tested adversarially.
- Governance commands must emit paired audit/domain events with rationale; no silent mutation paths.
- Redaction must preserve auditability: projected/displayed content changes, but event history and rationale remain defensible according to the approved redaction model.
- Every SRE/operator action that touches tenant data must produce tenant-visible audit evidence where in scope.
- Dapr pub/sub is at-least-once; all projection/event handlers must tolerate duplicates and replay.
- Tenant events may arrive out of order; production projection behavior must detect gaps/regressions or rebuild.
- Projection reads must surface stale/rebuilding/unavailable states rather than pretending data is fresh.
- Read-time Party hydration can degrade, but command-time participant validation must fail closed.
- Conversation URLs/permalinks that encode temporal cursors must re-resolve identically.
- Keep hot read/write paths local after authorization; do not synchronously call Tenants on every request.
- Use EventStore snapshots/projections for long conversations rather than loading unbounded history into every command or read path.
- Avoid N+1 Parties hydration; use batching/caching adapters or add a proper Parties bulk endpoint if profiling proves it necessary.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive option.
- Update this file if new project patterns emerge.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when the technology stack or architecture constraints change.
- Review periodically for outdated or now-obvious rules.
- Remove rules that no longer prevent real implementation mistakes.

Last Updated: 2026-05-10
