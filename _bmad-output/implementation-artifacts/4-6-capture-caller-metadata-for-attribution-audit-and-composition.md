# Story 4.6: Capture Caller Metadata for Attribution, Audit, and Composition

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter system,
I want to pass caller-supplied client, composer, and origin metadata safely,
so that attribution, audit, downstream projection, and FrontComposer surfaces can preserve useful provenance.

## Acceptance Criteria

1. Only approved, bounded caller metadata fields are accepted; identity/secret fields are rejected as user-editable metadata
   - Given an adopter submits commands or queries,
   - When caller metadata such as client name, client version, composer source, origin, correlation ID, causation ID, and integration context is supplied,
   - Then the system validates, bounds, and stores or forwards only approved metadata fields,
   - And tenant identity, user identity, tokens, claims, provider payloads, raw prompts, and protected content are not accepted as user-editable metadata fields.

2. Caller metadata remains provenance only and never becomes trust, tenant, governance, or authorization truth
   - Given metadata is used for attribution, audit, projections, or FrontComposer composition,
   - When it is rendered or published,
   - Then metadata remains provenance data and does not become authorization, tenant truth, governance truth, or UI-inferred trust state,
   - And every displayed trust claim still maps to projection or command availability metadata.

3. Malformed, oversized, unbounded, sensitive, or unsupported metadata is rejected/truncated/omitted with typed safe diagnostics
   - Given metadata is malformed, oversized, unbounded, sensitive, or unsupported,
   - When a command or query boundary validates it,
   - Then the system rejects, truncates by approved policy, or omits the metadata with typed safe diagnostics,
   - And no logs, traces, metrics, events, or projections include unsafe values.

4. Tests prove safe validation, bounded telemetry, attribution usefulness, and no trust/authorization inference
   - Given metadata tests run,
   - When valid metadata, oversized metadata, token-like values, tenant spoofing attempts, unbounded business identifiers, FrontComposer composition metadata, and publication scenarios are exercised,
   - Then tests prove safe validation, bounded telemetry, attribution usefulness, and no trust or authorization inference from caller-supplied values.

## Tasks / Subtasks

- [x] Confirm scope, readiness gates, the metadata-only definition, and reuse existing primitives before implementation (AC: 1-4)
  - [x] Re-read the architecture **Metadata-Only Definition** and apply it as the gate for every accepted field: a field qualifies only if it is non-personal, non-transcript, non-derived-authority, non-secret, and useful for routing, correlation, lifecycle, or operational diagnosis. If any field cannot be classified confidently, STOP and request an ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Metadata-Only Definition`]
  - [x] Treat caller metadata as **provenance only**: it must never become authorization, tenant truth, governance truth, or UI-inferred trust state. Tenant access stays decided by the local Tenants projection (`ConversationTenantAccessService`), not by caller-supplied values; every displayed trust claim must still map to projection freshness or command availability metadata. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.6: Capture Caller Metadata for Attribution, Audit, and Composition`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
  - [x] Reuse the existing command envelope as the canonical home for correlation/causation: `ConversationCommandMetadata` already carries `SchemaVersion`, `TenantId`, `ActorPartyId`, `CorrelationId`, `CausationId`, and `IdempotencyKey`, and `ConversationEventMetadata` already carries `CorrelationId`/`CausationId` onto published events. Do NOT add a parallel correlation/causation model; bind caller-supplied correlation/causation through the existing envelope. [Source: `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`; `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`]
  - [x] Reuse the existing safe metadata bag(s) rather than inventing a new one where one already fits: `UpdateConversationMetadataCommand.Attributes` (`IReadOnlyDictionary<string,string>`) and `ProviderCorrelationMetadata.ExtensionData` are the precedent for bounded opaque string bags. Reuse the `ProviderCorrelationMetadata` shape/validation pattern (required name/type, schema version, optional bounded `ExtensionData`) when modeling caller client/composer/origin metadata. [Source: `src/Hexalith.Conversations.Contracts/Commands/UpdateConversationMetadataCommand.cs`; `src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs`]
  - [x] Reuse the Story 4.3 typed error catalog (`ConversationErrorCatalog`, `ConversationError`, `ConversationErrorCode`, `ConversationErrorClientAction`) and its free-text content-safety guardrail (`ConversationError.EnsureContentSafe`) for typed safe rejection/omission diagnostics. Prefer `CommandValidationFailed` for malformed/oversized/unsupported metadata; only propose a new code through the catalog with full descriptor coverage and STOP for ADR before changing the public error taxonomy. Do NOT create a parallel error/remediation envelope. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`]
  - [x] Reuse the envelope-validation pattern from `ConversationCommandSchemaValidation.ValidateMetadata(...)` (length caps, control-character rejection, typed rejection via `ConversationRejectedDomainEvent`) as the model for bounding caller metadata at the command boundary; mirror the existing idempotency-key bounding (max length + `char.IsControl` rejection). [Source: `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs`]

- [x] Define the approved caller-metadata contract surface (AC: 1, 2)
  - [x] Add a bounded, content-safe caller-metadata contract under `src/Hexalith.Conversations.Contracts` (e.g. `Identifiers/CallerMetadata.cs` or `Commands/CallerMetadata.cs`, mirroring `ProviderCorrelationMetadata`) using `SchemaVersion.Current`. Approved fields are limited to: client name, client version, composer source, origin, integration context, and a bounded opaque `IReadOnlyDictionary<string,string>` extension bag for additional safe provenance. Correlation ID and causation ID are NOT duplicated here — they live on `ConversationCommandMetadata`/`ConversationEventMetadata`.
  - [x] Eagerly validate every field at construction (sibling pattern: validate identity/correlation fields at boundaries): cap per-value and per-bag size, cap key/value counts, reject control characters, and reject content-unsafe substrings using the shared `ConversationError.EnsureContentSafe` blocklist material. Decide and document (in Dev Notes) the policy split between reject vs. truncate vs. omit per AC3, and apply it deterministically.
  - [x] Forbid identity/secret/protected fields by construction: tenant identity, user/Party identity, tokens, claims, provider payloads, raw prompts, message/redacted text, and protected content must NOT be acceptable as user-editable caller-metadata keys or values. Caller metadata cannot be a back-door for `TenantId`, `PartyId`, auth claims, or provider session/response/payload values.
  - [x] Attach caller metadata as an optional parameter on the command surface where provenance is needed — preferably extend `ConversationCommandMetadata` (so every command carries it uniformly) OR add an optional `CallerMetadata?` to the relevant command records (`CreateConversationCommand`, `AppendMessageCommand`, `UpdateConversationMetadataCommand`) following the existing optional `ProviderCorrelationMetadata? ProviderCorrelation = null` precedent. Choose ONE approach, justify it in Dev Notes, and keep it additive (no breaking change to existing required parameters). If extending `ConversationCommandMetadata` changes the public command envelope shape broadly, STOP for ADR first. **(Chose the additive optional-parameter approach; envelope unchanged — see Completion Notes.)**
  - [x] Keep `Hexalith.Conversations.Contracts` infrastructure-free: no references to EventStore, Tenants, Parties, FrontComposer, ASP.NET Core, Dapr, server, client, or UI packages. [Source: `_bmad-output/project-context.md#Language-Specific Rules`]
  - [x] Register every new/changed contract sample in `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (`AllContracts`) so it participates in serialization, forbidden-surface, and content-safety scans; if a new closed vocabulary is introduced, add converters in `Serialization/ClosedVocabularyJsonConverters.cs` following the existing pattern. **(No new closed vocabulary introduced; no converter needed.)** [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`; `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`]

- [x] Enforce bounded, content-safe caller metadata at the command/query boundary (AC: 1, 3)
  - [x] Extend `ConversationCommandSchemaValidation` (or add a focused sibling validator following the `*Boundary.cs`/`*Validation.cs` split in `src/Hexalith.Conversations/Validation`) to validate caller metadata on every command that carries it, returning a typed `ConversationRejectedDomainEvent` (code `CommandValidationFailed`, bounded reason code) for malformed/oversized/unbounded/sensitive/unsupported metadata. Mirror the existing idempotency-key bounding precedent.
  - [x] Apply the documented reject/truncate/omit policy from AC3 deterministically. Truncation/omission must never silently drop a value into a derived store while keeping an unsafe fragment; if truncation cannot guarantee safety, reject with a typed diagnostic instead. **(Policy = reject; no silent truncation.)**
  - [x] Confirm the metadata path never weakens the existing fail-closed gates: caller metadata is validated AFTER (not instead of) tenant-context and authority binding at the command API (`ConversationCommandApi.TryValidateCommandContext`). Reuse the existing tenant access guard, freshness gate, audit pairing, and idempotency executor — do NOT duplicate or relax them. [Source: `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`]
  - [x] Ensure caller metadata never participates in authorization: trusted tenant binding still comes from claims-derived tenant context validated against the local Tenants projection; caller-supplied origin/client/composer values must not influence tenant scope, access decisions, command eligibility, or freshness/trust state. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`]

- [x] Carry caller metadata through attribution, audit, projections, and FrontComposer composition as provenance only (AC: 2)
  - [x] Persist only approved, non-personal caller-metadata fields into durable events/state where attribution/audit provenance is required, following the durable-ID rule (store stable references and metadata explicitly classified as non-personal; never persist Party display names, contact values, tokens, or content). If durable persistence of a new field is required, name its owning decision and STOP for ADR if it crosses an ADR trigger (new durable state). **(No new durable event/state field added — would trigger an ADR; caller provenance flows through the existing correlation/causation envelope. See Completion Notes.)** [Source: `_bmad-output/project-context.md#Language-Specific Rules`; `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]
  - [x] When publishing, expose caller metadata only through safe transport-visible headers consistent with `ConversationTransportMetadata` (safe topic/type/source/subject/headers); never place tenant identity, user identity, tokens, claims, provider payloads, or content into published metadata, and keep cross-tenant metadata non-leaking through topics/envelopes/correlation IDs/errors. **(Verified by `CallerMetadataPublicationTest`: caller-supplied values never reach transport headers; only safe correlation/causation are published.)** [Source: `src/Hexalith.Conversations.Server/Publication/ConversationTransportMetadata.cs`; `_bmad-output/planning-artifacts/prd.md#FR32`]
  - [x] For FrontComposer composition surfaces, caller metadata is provenance display only — it must not become a trust indicator. Every displayed trust claim must still map to Conversations-owned projection freshness or command availability metadata, and the UI must not infer trust/freshness/authorization from caller-supplied values. Use contract-first FrontComposer annotations; do not hand-edit generated files under `obj/`. **(No Admin/FrontComposer project exists in this repo; the contract is provenance-only by construction and the trust-inference test proves caller metadata cannot become tenant/authorization truth. See Completion Notes.)** [Source: `_bmad-output/planning-artifacts/architecture.md` FrontComposer trust-component boundary; `_bmad-output/project-context.md#Framework-Specific Rules`]

- [x] Add focused tests across contracts, domain validation, and server/publication (AC: 1-4)
  - [x] Contract tests: serialization with `JsonSerializerDefaults.Web`, additive-JSON tolerance, eager construction-time validation (size caps, count caps, control-character and content-unsafe rejection), and forbidden-surface/content-safety scans for all new caller-metadata fields. Register the new contract in `ContractSamples.AllContracts` so the existing `ForbiddenPublicSurfaceTest`/`ContractSerializationTest` cover it.
  - [x] Domain/validation tests: valid metadata accepted; oversized metadata rejected/truncated per policy; token-like values rejected; tenant-spoofing attempts (caller-supplied `tenantId`/`partyId`/claim-like keys) do NOT alter tenant scope or authorization; unbounded business identifiers bounded; control characters rejected. Assert typed `ConversationRejectedDomainEvent`/`ConversationError` with the correct code and content-safe diagnostics.
  - [x] Trust-inference tests (AC2): prove that caller metadata cannot become authorization, tenant truth, governance truth, or UI trust state — e.g. a request whose caller metadata claims a different tenant/elevated origin is still bound by the claims-derived tenant projection, and freshness/command-availability/trust claims continue to derive from projection/command-availability metadata only.
  - [x] Bounded-telemetry / no-leak tests (AC3): scan serialized events/projections, any published transport headers, and any log-like/diagnostic output for forbidden fragments — tenant IDs, Party IDs, conversation IDs/existence, provider session/payload, business-reference values, tokens, raw prompts/content, raw exception text, `C:\`, `D:\`, and the infrastructure terms already in the `ConversationError` blocklist. Closed-vocabulary tokens are safe machine identifiers; scope the leakage scan to free-text/protected-value disclosure (Story 4.4/4.5 lesson). Prove no logs/traces/metrics/events/projections include unsafe values.
  - [x] FrontComposer composition + publication tests (AC2, AC4): prove caller metadata renders/publishes as provenance only, every displayed trust claim still maps to projection/command-availability metadata, and published transport metadata stays content-safe and non-leaking.
  - [x] If any logging is added, use source-generated logging or static templates with semantic placeholders; never interpolate raw error text, secrets, protected IDs, provider payloads, or caller-supplied free text. **(No logging added.)** [Source: `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`]
  - [x] Run targeted tests first, then the full solution:
    - `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~CallerMetadata|FullyQualifiedName~Metadata|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"`
    - `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~CallerMetadata|FullyQualifiedName~Validation"`
    - `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~CallerMetadata|FullyQualifiedName~Publication|FullyQualifiedName~CommandApi"`
    - `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation`
    - `dotnet test Hexalith.Conversations.slnx`
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 4.6 evidence after implementation.

- [x] Document the approved caller-metadata surface for adopters minimally (AC: 1)
  - [x] Update `README.md` and/or `src/Hexalith.Conversations.Contracts/README.md` with a compact table: approved caller-metadata fields, bounds/limits, reject-vs-truncate-vs-omit policy, and the rule that metadata is provenance only (never trust/tenant/authorization truth). Keep examples at the contract/client level, not raw HTTP fallback. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]
  - [x] Do NOT document raw server routes, EventStore mechanics, internal handler names, storage keys, projection topology, provider payloads, secrets, or production exception samples. The full developer integration guide remains Story 4.7.

- [x] Preserve scope boundaries and stop conditions (AC: 1-4)
  - [x] Do NOT implement Story 4.7 full developer integration guide, DocFX/API reference pipeline, expanded API examples, or raw HTTP public examples.
  - [x] Do NOT implement Epic 5 release signing, versioned conformance manifest, named-waiver lifecycle, deprecation policy publication, or release-gate evidence aggregation. Do NOT implement Epic 6 operational telemetry dashboards or Admin UI work beyond the minimal FrontComposer provenance-display proof above.
  - [x] STOP for ADR/architecture review before adding new durable state, a new public error/status/freshness vocabulary outside what the shared gates allow, a new runtime gate semantic, a globally-runnable host, broadly changing the public command envelope shape, or any degraded/fail-open behavior. **(None of these triggers were crossed; design stayed additive and reused existing primitives.)**

## Dev Notes

### Epic and Business Context

- Epic 4 makes adopter integration credible through a contract package, a supported .NET client, compatibility discovery, typed sanitized errors, onboarding diagnostics, remediation guidance, CORE preconditions, and adopter-facing conformance tests. Story 4.6 adds safe caller-supplied provenance metadata so attribution, audit, downstream projections, and FrontComposer composition can preserve useful provenance without ever becoming trust or authority. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`; `_bmad-output/planning-artifacts/epics.md#Story 4.6: Capture Caller Metadata for Attribution, Audit, and Composition`]
- Story 4.6 covers FR76: expose caller-supplied client, composer, or origin metadata needed for attribution, audit, downstream projection use, and Hexalith front-end composition surfaces. [Source: `_bmad-output/planning-artifacts/prd.md#FR76`; `_bmad-output/planning-artifacts/epics.md` Requirements Covered FR76]
- The defining safety property is **provenance, not authority**: caller metadata is correlation/attribution detail only. It must never become authorization, tenant truth, governance truth, or UI-inferred trust state, and every displayed trust claim must still map to Conversations-owned projection freshness or command availability metadata. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.6`; `_bmad-output/planning-artifacts/prd.md#FR32`]

### Readiness Gate Context

- No readiness gate lists Story 4.6 in its Blocks column, so no gate directly blocks this story. The binding constraints are the always-on shared rules: the architecture **Metadata-Only Definition** (a field qualifies only if non-personal, non-transcript, non-derived-authority, non-secret, and useful for routing/correlation/lifecycle/diagnosis), the Shared Trust/Freshness Vocabulary rule, fail-closed tenant isolation, and the ADR triggers. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/planning-artifacts/architecture.md#Metadata-Only Definition`; `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`]
- `Command availability metadata` is `decided`: server-owned command metadata controls eligibility, disabled state, required permission, precondition, risk level, freshness, audit requirement, and blocked reason. Caller metadata must NOT substitute for or override any of these server-owned trust signals. [Source: `_bmad-output/implementation-artifacts/readiness-gates.md`; `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#command-availability-metadata`]
- The `.NET client versus raw HTTP fallback policy` is `decided`: the supported v1 path is the .NET client plus shared contract package; do not add adopter-facing raw HTTP fallback examples for caller metadata. [Source: `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`]

### Current Implementation State (reuse these; do NOT build parallel models)

- `ConversationCommandMetadata` (the canonical command envelope) already carries `SchemaVersion`, `TenantId`, `ActorPartyId`, required `CorrelationId`, optional `CausationId`, and optional `IdempotencyKey`, with eager validation. Correlation/causation are already first-class — bind caller-supplied correlation/causation through this envelope; do NOT re-model them inside a new caller-metadata bag. [Source: `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`]
- `ConversationEventMetadata` already propagates `CorrelationId`/`CausationId` onto published events and exposes a stable `DeduplicationKey`; caller-attribution provenance on events should flow through this existing event metadata, not a parallel structure. [Source: `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`]
- `ProviderCorrelationMetadata` is the canonical bounded-opaque-metadata precedent: required `ProviderName`/`ProviderType`, `MetadataSchemaVersion`, optional session/response references, and a validated `ExtensionData` string bag (non-empty keys, non-null values). It is attached as an optional `ProviderCorrelationMetadata? ProviderCorrelation = null` on `CreateConversationCommand` and `AppendMessageCommand`. Model `CallerMetadata` on this exact shape and attachment pattern; provider IDs remain opaque correlation, never authority. [Source: `src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs`; `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`; `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs`]
- `UpdateConversationMetadataCommand` already has an optional `IReadOnlyDictionary<string,string>? Attributes` "safe adopter metadata" bag and an optional `Label`. NOTE: `ConversationCommandSchemaValidation` currently validates ONLY the shared envelope (tenant, schema version, idempotency-key length/control chars) — the `Attributes` bag does NOT yet appear to be bounded/content-scanned. Story 4.6 should close this gap by bounding caller metadata (including this existing `Attributes` bag) at the command boundary, reusing the existing validation precedent rather than inventing a new path. [Source: `src/Hexalith.Conversations.Contracts/Commands/UpdateConversationMetadataCommand.cs`; `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs`]
- `ConversationCommandSchemaValidation.ValidateMetadata(...)` is the canonical envelope-bounding pattern: it caps idempotency-key length (`IdempotencyKeyMaxLength = 200`), rejects control characters via `char.IsControl`, and returns typed `ConversationRejectedDomainEvent(code, reasonCode, schemaVersion, correlationId, causationId)`. Reuse this exact reject/bound pattern for caller metadata. [Source: `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs`]
- `ConversationError` enforces content safety on all free-text fields via the internal `EnsureContentSafe(...)` blocklist (rejects `tenant:`, `tenant-`, `party:`, `party-`, `conv:`, `conversation-`, `provider-session`, `provider payload`, `business reference`, `EventStore`, `envelope`, `stream`, `snapshot`, `SignalR`, `handler`, `dispatcher`, `repository`, `store`, `exception`, `C:\`, `D:\`, etc.). Reuse this guardrail material for caller-metadata content-safety and for typed rejection diagnostics; do NOT weaken it. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`]
- `ConversationErrorCode`/`ConversationErrorCatalog` are the canonical typed-error taxonomy. `CommandValidationFailed`, `TenantBindingMissing`, `TenantContextMismatch`, and `ProviderOnlyIdentityForbidden` already exist and cover the expected metadata-rejection cases. Reuse these; STOP for ADR before adding a new public code. [Source: `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`; `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`]
- `ConversationCommandApi.TryValidateCommandContext` already binds tenant/caller authority from claims and rejects on missing/ mismatched tenant context BEFORE handler dispatch. Caller-metadata validation must run alongside/after this and never bypass it. [Source: `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`]
- `ConversationTransportMetadata.FromEvent(...)` is the canonical safe-publication header builder (safe topic/type/source/subject + bounded headers including correlation/causation). Route any published caller-attribution provenance through this safe header surface. [Source: `src/Hexalith.Conversations.Server/Publication/ConversationTransportMetadata.cs`]
- The contract test safety net (`ContractSamples.cs` `AllContracts`, `ForbiddenPublicSurfaceTest.cs`, `ContractSerializationTest.cs`, `ContractPackageInventoryTest.cs`) is the scan that catches leaks. New caller-metadata contracts MUST be registered in `AllContracts` or they bypass forbidden-surface/content-safety scanning. [Source: `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`]

### Architecture and Contract Guardrails

- **Metadata-Only Definition is the acceptance gate.** Approved caller metadata is limited to non-personal, non-transcript, non-derived-authority, non-secret provenance useful for routing/correlation/lifecycle/diagnosis (client name/version, composer source, origin, integration context, correlation/causation via the envelope). Forbidden: message/prompt text, model output, redacted text, Party display names/contact channels, provider payloads, tokens, claims, raw upstream bodies, unauthorized resource names, or authorization decisions. If a field cannot be confidently classified, STOP for ADR. [Source: `_bmad-output/planning-artifacts/architecture.md#Metadata-Only Definition`]
- `Contracts` defines commands, projections, events, typed errors, IDs, freshness/trust states, and schema versions and must stay infrastructure-free. Public APIs must not expose EventStore stream names, positions, snapshots, envelopes, projection topology, SignalR groups, or handler names. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/project-context.md#Language-Specific Rules`]
- Generated FrontComposer command payloads must not include tenant identity, user identity, tokens, claims, or host authorization context. Caller metadata feeding composed views must follow this rule and stay provenance-only display; the UI must not infer trust/freshness/authorization. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/planning-artifacts/architecture.md` FrontComposer trust-component boundary]
- Tenant access fails closed and is decided by the local Tenants projection, never by JWT/claims alone and never by caller-supplied values. Missing/invalid tenant context must never default to a tenant or global query, and failures must not reveal whether a protected conversation exists. [Source: `_bmad-output/planning-artifacts/architecture.md#Authorization Pattern`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Errors are logged with metadata only; never log message content, redacted text, Party personal data, tokens, raw upstream payloads, or unauthorized resource existence. No logs/traces/metrics/events/projections may include unsafe caller-supplied values (AC3). [Source: `_bmad-output/project-context.md#Framework-Specific Rules`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Tenant-aware events/notifications must not leak cross-tenant metadata through topics, envelopes, correlation IDs, errors, or negative results. Caller-supplied correlation/origin values must not become a cross-tenant leak vector. [Source: `_bmad-output/planning-artifacts/prd.md#FR32`]
- ADR triggers: any new durable state, new runtime service endpoint, public error/status taxonomy change, schema evolution rule change, broad public command envelope change, or degraded/fail-open behavior triggers ADR review before implementation. [Source: `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`]

### File Structure Guidance

- Likely new contract files (under `src/Hexalith.Conversations.Contracts`):
  - `Identifiers/CallerMetadata.cs` (or `Commands/CallerMetadata.cs`) — bounded, content-safe, eagerly-validated record modeled on `ProviderCorrelationMetadata`.
- Likely update files:
  - `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`, `AppendMessageCommand.cs`, `UpdateConversationMetadataCommand.cs` — add optional `CallerMetadata? CallerMetadata = null` (additive), OR extend `ConversationCommandMetadata` once (justify the chosen approach in Dev Notes; broad envelope change requires ADR).
  - `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs` (plus a focused `*Validation.cs`/`*Boundary.cs` sibling if needed) — bound caller metadata and the existing `UpdateConversationMetadataCommand.Attributes` bag.
  - `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs` / `ConversationTransportMetadata.cs` — only if approved caller provenance is published as safe headers.
  - `src/Hexalith.Conversations.Contracts/README.md` and/or `README.md` — adopter table.
  - `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`, `ForbiddenPublicSurfaceTest.cs`, `ContractPackageInventoryTest.cs` (if the public file inventory changes).
  - `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs` (only if a new closed vocabulary requires a converter).
- Likely new test files:
  - `tests/Hexalith.Conversations.Contracts.Tests/.../CallerMetadataContractsTest.cs`
  - `tests/Hexalith.Conversations.Tests/.../CallerMetadataValidationTest.cs`
  - `tests/Hexalith.Conversations.Server.Tests/.../CallerMetadataPublicationTest.cs` (publication/FrontComposer provenance + trust-inference coverage)
- Central Package Management is active. Any required package version belongs in `Directory.Packages.props`, never inline in `.csproj`. Avoid new dependencies unless proven necessary. [Source: `Directory.Packages.props`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Testing Requirements

- Contract tests must prove eager construction-time validation (size caps, count caps, control-character rejection, content-unsafe rejection), `JsonSerializerDefaults.Web` serialization, additive-JSON tolerance, and forbidden-surface/content-safety scans for all new caller-metadata fields, with the new contract registered in `ContractSamples.AllContracts`.
- Domain/validation tests must cover the full AC4 matrix with deterministic inputs (no live server, no sleeps, no external services): valid metadata, oversized metadata, token-like values, tenant-spoofing attempts, unbounded business identifiers, FrontComposer composition metadata, and publication scenarios.
- Trust/authorization-inference tests must prove caller metadata never alters tenant scope, authorization, command eligibility, freshness, or trust state — every displayed trust claim still derives from projection/command-availability metadata.
- No-leak tests must scan serialized events/projections, published transport headers, and any log-like/diagnostic/exception text for forbidden fragments (tenant/Party/conversation IDs and existence, provider session/payload, business-reference values, tokens, raw prompts/content, raw exception text, `C:\`, `D:\`, infrastructure terms). Scope the scan to free-text/protected-value disclosure; closed-vocabulary machine identifiers are safe (Story 4.4/4.5 lesson).
- Use xUnit v3, Shouldly, NSubstitute, and existing Tenants/Parties/EventStore testing helpers and fakes; reuse existing authorization/serialization test patterns rather than inventing new fakes. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Previous Story Intelligence

- Story 4.1 created compatibility metadata and enforced invariants (supported status must not carry remediation; unsupported/invalid carries a typed error). Keep any new caller-metadata contract additive and compatibility-safe. [Source: `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`]
- Story 4.2 implemented the supported .NET client and configures tenant context, caller metadata, correlation metadata, and endpoint settings; Story 4.6's caller-metadata surface must align with the client's typed-result behavior so REST, client, and conformance surfaces agree. [Source: `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`; `_bmad-output/planning-artifacts/epics.md` (client configured with caller metadata)]
- Story 4.3 centralized typed sanitized errors in `ConversationErrorCatalog`/`ConversationError` with `ClientAction`/`SafeMessage` and hardened free-text safety (rejecting tenant, Party, conversation, provider-session/payload, business-reference, local-path, and raw-exception markers). Reuse this catalog and guardrail for caller-metadata rejection diagnostics; do NOT introduce a parallel error/remediation model and do NOT weaken the free-text guardrails. [Source: `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`]
- Story 4.4 / 4.5 lesson (recurring): a content-safety blocklist that is too broad collides with legitimate closed-vocabulary tokens. Scope leakage scans to free-text and protected-value disclosure, not closed machine identifiers; reuse the canonical `ConversationError.EnsureContentSafe` for free-text fields and a bounded closed-token charset for any new vocabulary. [Source: `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`; `_bmad-output/implementation-artifacts/4-5-provide-adopter-facing-conformance-tests-and-core-fixture.md`]
- Recent commits are story-scoped and test-heavy: `feat(story-4.3): Expose typed sanitized errors and remediation guidance`, `feat(story-4.2): Provide supported .NET client happy path`, `feat(story-4.1): Add contract compatibility metadata`. Continue with focused tests, content-safety/no-leak checks, and full-solution validation before closing. [Source: `git log --oneline -5`]

### Latest Technical Notes

- `System.Text.Json` supports records/immutable types and bounded dictionaries; new caller-metadata contracts should follow the existing `JsonSerializerDefaults.Web` plus custom-converter pattern in `ClosedVocabularyJsonConverters.cs` only if a new closed vocabulary is introduced. [Source: `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`]
- Microsoft library logging guidance recommends source-generated logging and warns against string interpolation; if caller-metadata handling emits logs, use static templates/source-generated logging and never log raw caller-supplied free text, protected IDs, provider payloads, secrets, or exception detail. [Source: `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`]
- `dotnet test --filter` supports `FullyQualifiedName~...` and `|` composition for xUnit selection; run targeted filters first, then full-solution validation. [Source: `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`]

### Out of Scope

- No Story 4.7 full developer integration guide, DocFX/API reference pipeline, expanded API examples, or raw HTTP public examples.
- No Epic 5 release signing, versioned conformance manifest, named-waiver lifecycle, deprecation policy publication, or release-gate evidence aggregation; no Epic 6 telemetry dashboards or Admin UI work beyond the minimal FrontComposer provenance-display proof.
- No parallel correlation/causation, error/remediation, or attribution model — reuse the existing command/event envelope, `ProviderCorrelationMetadata`/`Attributes` bag pattern, and the typed error catalog.
- No new durable state, transcript tables, runtime health dashboard, background worker, raw EventStore endpoint, or globally-runnable host.
- No use of caller metadata as authorization, tenant truth, governance truth, or UI trust state; no caller-supplied override of tenant scope, command availability, or freshness/trust signals.
- No new public error/status/freshness vocabulary outside the shared gates, no broad public command envelope change, and no degraded/fail-open behavior, without ADR approval.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 4.6: Capture Caller Metadata for Attribution, Audit, and Composition`
- `_bmad-output/planning-artifacts/epics.md#Epic 4: Adopter Integration and Developer Readiness`
- `_bmad-output/planning-artifacts/prd.md#FR76`
- `_bmad-output/planning-artifacts/prd.md#FR32`
- `_bmad-output/planning-artifacts/architecture.md#Metadata-Only Definition`
- `_bmad-output/planning-artifacts/architecture.md#Shared Vocabulary Rule`
- `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Authorization Pattern`
- `_bmad-output/planning-artifacts/architecture.md#ADR Triggers`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#command-availability-metadata`
- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md#net-client-versus-raw-http-fallback-policy`
- `_bmad-output/implementation-artifacts/4-1-publish-conversations-contract-package-and-compatibility-metadata.md`
- `_bmad-output/implementation-artifacts/4-2-provide-supported-net-client-happy-path.md`
- `_bmad-output/implementation-artifacts/4-3-expose-typed-sanitized-errors-and-remediation-guidance.md`
- `_bmad-output/implementation-artifacts/4-4-define-core-preconditions-and-onboarding-diagnostics.md`
- `_bmad-output/implementation-artifacts/4-5-provide-adopter-facing-conformance-tests-and-core-fixture.md`
- `_bmad-output/project-context.md`
- `CLAUDE.md`
- `README.md`
- `src/Hexalith.Conversations.Contracts/README.md`
- `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs`
- `src/Hexalith.Conversations.Contracts/Commands/UpdateConversationMetadataCommand.cs`
- `src/Hexalith.Conversations.Contracts/Events/ConversationEventMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Identifiers/ProviderCorrelationMetadata.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs`
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCatalog.cs`
- `src/Hexalith.Conversations.Contracts/Serialization/ClosedVocabularyJsonConverters.cs`
- `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs`
- `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs`
- `src/Hexalith.Conversations.Server/Publication/ConversationTransportMetadata.cs`
- `src/Hexalith.Conversations.Server/Publication/ConversationPublicationMapper.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs`
- `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs`
- `Directory.Build.props`
- `Directory.Packages.props`
- `https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/deserialization#deserialization-behavior`
- `https://learn.microsoft.com/dotnet/core/extensions/logging/library-guidance#prefer-source-generated-logging`
- `https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests#xunit-examples`

## Dev Agent Record

### Agent Model Used

Opus 4.7 (1M context) — claude-opus-4-7[1m]

### Debug Log References

- Initial `ContractSamples` `Caller` sample value `"case-intake"` collided with the shared `ConversationError.EnsureContentSafe` blocklist fragment `case-`; changed the sample integration context to `"intake"` (and matching test fixtures). Confirms the content-safety guardrail is correctly applied to caller-metadata fields.
- `ContractSerializationTest.RepresentativeFixturesShouldKeepStableCamelCaseJsonShapes` required the representative `CreateConversationCommand` wire shape to include the additive `"callerMetadata":null` member; updated the fixture.

### Completion Notes List

- **Attachment approach (justification):** Added an additive optional `CallerMetadata? CallerMetadata = null` to `CreateConversationCommand`, `AppendMessageCommand`, and `UpdateConversationMetadataCommand`, mirroring the existing optional `ProviderCorrelationMetadata? ProviderCorrelation = null` precedent. Chose this over extending `ConversationCommandMetadata` because a broad public command-envelope change is an ADR trigger; the additive parameter is non-breaking and keeps the envelope stable.
- **New contract:** `CallerMetadata` (under `Identifiers`) modeled on `ProviderCorrelationMetadata`. Approved fields only: `ClientName`, `ClientVersion`, `ComposerSource`, `Origin`, `IntegrationContext`, plus a bounded opaque `ExtensionData` string bag. Correlation/causation are intentionally NOT duplicated — they remain first-class on the command/event envelope.
- **Validation reuse:** Every field is eagerly validated at construction (per-value <= 256 chars, <= 32 extension entries, control-character rejection, and the shared `ConversationError.EnsureContentSafe` blocklist). `CallerMetadata.TryValidateBounds`/`TryValidateMetadataBag` are non-throwing boundary helpers returning bounded reason codes. `ConversationCommandSchemaValidation.ValidateEnvelope` now bounds caller metadata AFTER the shared envelope and also bounds the previously-unbounded `UpdateConversationMetadataCommand.Attributes` bag (the noted gap), returning typed `ConversationRejectedDomainEvent(CommandValidationFailed, <reason>)`.
- **AC3 policy = reject:** Malformed/oversized/unbounded/sensitive/unsupported metadata is rejected (not silently truncated), because a truncated content-unsafe value cannot guarantee a safe residual fragment. Diagnostics are bounded reason codes (`caller_metadata_invalid`, `caller_metadata_too_many_entries`) and never echo caller-supplied values.
- **Provenance-not-authority (AC2):** Caller metadata never participates in tenant binding/authorization; the command-API trust-inference test proves a caller claiming a different tenant/elevated origin is still bound to the claims-derived tenant. Caller metadata is validated after `ConversationCommandApi.TryValidateCommandContext`, never instead of it.
- **No new durable state / no leak (AC3):** No new durable event/state field was added (would trigger an ADR). Published provenance flows only through the existing safe correlation/causation transport headers; `CallerMetadataPublicationTest` proves no caller-supplied client/composer/origin value reaches `ConversationTransportMetadata`.
- **FrontComposer:** No Admin/FrontComposer project exists in this repository; the contract is provenance-only by construction and the trust-inference test proves caller metadata cannot become tenant/authorization/trust truth.
- **Test results:** Targeted Contracts 64, Core 22, Server 31 passed; `dotnet pack` produced the adopter-safe contracts nupkg; full solution `dotnet test Hexalith.Conversations.slnx` = 1003 passed (Client 23, Conformance 25, Integration 8, Core 150, Server 425, Contracts 372), 0 failed.

### File List

- `src/Hexalith.Conversations.Contracts/Identifiers/CallerMetadata.cs` (new)
- `src/Hexalith.Conversations.Contracts/Commands/CreateConversationCommand.cs` (modified)
- `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs` (modified)
- `src/Hexalith.Conversations.Contracts/Commands/UpdateConversationMetadataCommand.cs` (modified)
- `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs` (modified)
- `src/Hexalith.Conversations.Contracts/README.md` (modified)
- `tests/Hexalith.Conversations.Contracts.Tests/CallerMetadataContractsTest.cs` (new)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` (modified)
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs` (modified)
- `tests/Hexalith.Conversations.Tests/Validation/CallerMetadataValidationTest.cs` (new)
- `tests/Hexalith.Conversations.Server.Tests/Publication/CallerMetadataPublicationTest.cs` (new)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (modified)

## Story Context Validation

- Checklist reviewed: `.claude/skills/bmad-create-story/checklist.md`.
- Input discovery completed in YOLO mode:
  - Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`, focusing on Epic 4 and Story 4.6 ACs (FR76), the provenance-not-authority framing, and the downstream Story 4.7 / Epic 5-6 boundaries.
  - Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`, focusing on FR76 (caller-supplied client/composer/origin metadata) and FR32 (no cross-tenant metadata leakage through topics/envelopes/correlation IDs/errors).
  - Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`, focusing on the Metadata-Only Definition, the Shared Vocabulary Rule, architectural boundaries, the authorization pattern, the FrontComposer trust-component boundary, and ADR triggers.
  - Loaded persistent project-context facts from `_bmad-output/project-context.md`, including .NET 10, central package management, Contracts/Client/Server boundaries, fail-closed tenant isolation, metadata/personal-data rules, logging safety, and the root-level submodule policy.
  - Loaded sibling Stories 4.4 and 4.5 for house style; previous Stories 4.1-4.3; the current sprint status; readiness gates/decisions; the existing command/event metadata, `ProviderCorrelationMetadata`, `UpdateConversationMetadataCommand.Attributes`, command-envelope validation, command API, transport metadata, typed error system, and contract test safety net; plus recent git history.
  - Checked official Microsoft documentation for `System.Text.Json` deserialization behavior, .NET library logging guidance, and `dotnet test --filter`.
- Checklist fixes applied in YOLO mode:
  - Story points dev work to the existing command/event envelope (correlation/causation), the `ProviderCorrelationMetadata`/`Attributes` bounded-metadata precedent, the envelope-validation reject/bound pattern, the typed error catalog, the safe transport-metadata header builder, and the contract test safety net instead of new parallel models.
  - Flagged the concrete gap that `UpdateConversationMetadataCommand.Attributes` is not yet bounded by `ConversationCommandSchemaValidation`, directing the dev agent to close it rather than build a separate path.
  - Added explicit guardrails for the Metadata-Only Definition, provenance-not-authority, no trust/authorization inference, fail-closed tenant binding, no cross-tenant leakage, content-safety/no-leak scanning, ContractSamples registration, FrontComposer provenance-only display, and minimal adopter docs.
  - Added likely new/updated file lists, targeted/full validation commands, package validation, official Microsoft documentation references, previous-story learnings, and explicit out-of-scope boundaries (Story 4.7, Epic 5-6).
- Validation result: ready-for-dev. The story includes concrete acceptance criteria, scoped tasks, current-code constraints, architecture guardrails, test requirements, latest technical references, and explicit scope boundaries.
- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Change Log

- 2026-05-23: Created Story 4.6 context from Epic 4 / FR76 requirements, PRD/architecture (Metadata-Only Definition, FR32) / readiness gates / project context, the existing command/event envelope, `ProviderCorrelationMetadata` and `UpdateConversationMetadataCommand.Attributes` precedents, the command-envelope validation and typed error catalog, the safe transport-metadata builder, Stories 4.1-4.5 learnings, and official Microsoft documentation. Status set to ready-for-dev.
- 2026-05-23: Implemented Story 4.6. Added the bounded, content-safe `CallerMetadata` provenance contract (modeled on `ProviderCorrelationMetadata`); attached it additively to the create/append/update commands; extended `ConversationCommandSchemaValidation` to bound caller metadata and close the previously-unbounded `UpdateConversationMetadataCommand.Attributes` gap with typed `command_validation_failed` rejections; documented the adopter-facing caller-metadata surface in the contracts README. Added contract, domain-validation, trust-inference, and publication/no-leak tests proving AC1-AC4 (caller metadata stays provenance-only and never becomes tenant/authorization/trust truth). Full solution `dotnet test Hexalith.Conversations.slnx` = 1003 passed, 0 failed. No new durable state, no parallel models, no broad envelope change. Status set to review.
