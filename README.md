# Hexalith.Conversations

Hexalith.Conversations is a tenant-scoped conversations module scaffold for the Hexalith ecosystem.

This module began as an intentionally non-operative scaffold establishing project boundaries, central package management, smoke tests, and ADR tracking. As of the boilerplate-reduction work through Epic 4, the `Hexalith.Conversations.Server` host is wired onto the shared EventStore domain-service host (`AddEventStoreDomainService` / `UseEventStoreDomainService`), and the core domain behavior is implemented and conformance-gated: aggregate command dispatch/replay on `EventStoreAggregate<TState>`, query handling via the SDK query-handler + cursor-codec seams, read-model persistence via the shared read-model store + write policy, projections via the SDK projection seam, and fail-closed tenant authorization. The local Aspire/Dapr topology now consumes the shared domain-module hosting helper, and the ServiceDefaults project provides a module-owned hook over the shared ServiceDefaults base. Provider integrations, workers, and FrontComposer runtime behavior remain out of scope.

## Contract Package Guidance

The supported v1 integration path is a .NET client that shares `Hexalith.Conversations.Contracts` with adopter applications. Adopters should create commands and interpret results, projections, and typed errors through these shared contracts; raw EventStore knowledge is not required for normal integration.

For the end-to-end adopter workflow, see the [Developer Integration Guide](docs/integration-guide.md). For release-owner compatibility rules, deprecation handling, and FR81 classification, see the [Contract Compatibility and Deprecation Policy](docs/release-evidence/contract-compatibility-policy.md).

Identity has a strict taxonomy:

- `ConversationId` is the Conversations-owned durable identity and is always interpreted with `TenantId`.
- `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, and `MessageId` are stable references to owning modules or Conversations records.
- `BusinessReference` is an adopter-owned external reference and never replaces `ConversationId`.
- `ProviderCorrelationMetadata` stores opaque provider/thread correlation data only; it is not tenant authority, actor attribution, idempotency, or aggregate identity.
- UI labels and thread names are display/correlation metadata only.
- Correlation IDs, causation IDs, and idempotency keys describe operations, not conversation identity.

Participant attribution follows the same rule: durable conversation events store stable `PartyId` references plus closed participant `type` and `role` vocabulary only. Party display names, contact values, personal details, organization details, and raw Parties failure data are read-time/application-boundary concerns and must not be persisted in Conversations events. Provider session IDs, model labels, thread names, and external user identifiers can only be transient correlation or validation inputs; they never replace `PartyId` as participant authority.

Command-time participant validation is intentionally fail-closed. If Parties cannot prove that the target `PartyId` is valid and visible for the command tenant, the add-participant command returns a typed Conversations rejection such as `participant_validation_unavailable` or `tenant_context_mismatch` and no `ParticipantAdded` event is emitted. Read-time Party hydration can later degrade according to the readiness decisions, but write-side participant membership cannot compensate by storing hydrated Party data.

Project assignment changes are owned by Conversations. Adopters use `ReassignConversationProjectCommand` through `IConversationClient.ReassignConversationProjectAsync(...)` to assign, move, or explicitly clear a conversation's `ProjectId`; the accepted fact is published as `ConversationProjectChanged`. The command target must carry `ConversationProjectAssignmentOperation.Assign` with a target `ProjectId`, or `ConversationProjectAssignmentOperation.Clear` with no target `ProjectId`. A missing target field is invalid and is never treated as unlink intent.

### JSON Wire Shape

Strongly-typed identifiers serialize as URN-style prefixed strings to prevent silent cross-type substitution on the wire:

| Type             | Wire shape          | Example                  |
| ---------------- | ------------------- | ------------------------ |
| `TenantId`       | `"tenant:<value>"`  | `"tenant:tenant-001"`    |
| `ConversationId` | `"conv:<value>"`    | `"conv:conversation-001"`|
| `PartyId`        | `"party:<value>"`   | `"party:party-actor"`    |
| `ProjectId`      | `"project:<value>"` | `"project:project-001"`  |
| `FolderId`       | `"folder:<value>"`  | `"folder:folder-001"`    |
| `FileId`         | `"file:<value>"`    | `"file:file-001"`        |
| `MessageId`      | `"message:<value>"` | `"message:message-001"`  |

JSON payloads lacking the expected prefix are rejected with `JsonException` at deserialization. The C# wrapper types remain plain strings internally; the prefix is enforced by the per-type `JsonConverter`.

`SchemaVersion` serializes as a strict JSON integer. JS adopters must emit integers without trailing `.0` and without exponent notation; numbers like `1.0`, `1e0`, and JSON strings `"1"` are rejected.

Closed-vocabulary values (`ProjectionTrustState`, `ConversationErrorCode`, `ConversationErrorCategory`, `ConversationErrorClientAction`, `ConversationEventType`, `ConversationCommandType`, `ConversationProjectAssignmentOperation`, `ParticipantType`, `ParticipantRole`) serialize as plain strings in their canonical form. Matching on read is **case-sensitive** — `"Current"` is valid, `"current"` is not. The README and IntelliSense are the single source of canonical spellings.

For `ParticipantType`, the canonical wire value diverges from the .NET property name for acronym-heavy entries. Use the wire value, not the property name, when serializing or matching by string:

| .NET property             | Canonical wire value |
| ------------------------- | -------------------- |
| `ParticipantType.Human`   | `"Human"`            |
| `ParticipantType.AiAgent` | `"AIAgent"`          |
| `ParticipantType.Llm`     | `"LLM"`              |

`ParticipantRole` property names and wire values match (`Member`, `Facilitator`, `Observer`).

Compound contracts such as `BusinessReference` and `ProviderCorrelationMetadata` remain JSON objects because they carry multiple fields.

Example command shape:

```csharp
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

ConversationCommandMetadata metadata = new(
    SchemaVersion.Current,
    new TenantId("tenant-001"),
    new PartyId("party-actor"),
    "correlation-001",
    causationId: null,
    idempotencyKey: "idempotency-001");

CreateConversationCommand command = new(
    metadata,
    new BusinessReference("crm", "case-123"),
    label: "Case 123");
```

The JSON for `metadata.TenantId` above is `"tenant:tenant-001"`, not `"tenant-001"`.

### Typed Errors and Content Safety

Typed errors are machine-readable first. Error contracts use stable codes such as `tenant_binding_missing`, `tenant_isolation_violation`, `aggregate_not_found`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `schema_version_unsupported`, `command_validation_failed`, `duplicate_participant`, `unsupported_participant`, `participant_validation_unavailable`, `tenant_context_mismatch`, and `provider_only_identity_forbidden`. Error details must remain content-safe: no inaccessible tenant IDs, Party personal data, conversation existence disclosure, redacted content, provider payloads, storage internals, raw exceptions, or cross-tenant business references.

`ConversationError` carries a stable `Code`, broad `Category`, `IsRetryable`, `ClientAction`, `SafeMessage`, `CorrelationId`, optional allowed `AuditHandle`, optional `SafeFieldDiagnostics`, and an HTTPS `Documentation` pointer. Adopter applications should branch on `Code`, `Category`, and `ClientAction`; transport status remains metadata.

| Code | Category | Retryable | Client action | Safe message intent | Documentation |
| --- | --- | --- | --- | --- | --- |
| `tenant_binding_missing` | `authorization` | `false` | `provide-context` | Provide authenticated tenant and caller context. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `tenant_isolation_violation` | `authorization` | `false` | `check-access` | The supplied access context cannot complete the request. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `tenant_projection_stale` | `freshness` | `true` | `retry-later` | Retry after tenant access state is current. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `audit_sink_unavailable` | `audit` | `true` | `retry-later` | Retry after audit recording is available. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `audit_pairing_required` | `audit` | `false` | `provide-audit-evidence` | Provide required audit evidence before retrying. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `idempotency_conflict` | `conflict` | `false` | `use-new-idempotency-key` | Use a new idempotency key for a changed command payload. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `idempotency_outcome_unknown` | `uncertainty` | `true` | `retry-same-request` | Retry with the same idempotency metadata. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `idempotency_key_missing` | `validation` | `false` | `provide-idempotency-key` | Provide idempotency metadata before sending the command. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `aggregate_not_found` | `hidden` | `false` | `hide-or-refresh` | The requested conversation is not available. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `schema_version_unsupported` | `versioning` | `false` | `use-supported-version` | Use supported Conversations contract and client versions. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `command_validation_failed` | `validation` | `false` | `correct-request` | Correct the request and retry. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `duplicate_participant` | `conflict` | `false` | `correct-request` | Correct participant membership and retry. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `unsupported_participant` | `validation` | `false` | `correct-request` | Use a supported participant type and role. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `participant_validation_unavailable` | `validation` | `true` | `retry-later` | Retry after participant validation is available. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `tenant_context_mismatch` | `authorization` | `false` | `align-context` | Align the request context with the authenticated context. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `provider_only_identity_forbidden` | `validation` | `false` | `use-party-identity` | Use a Conversations Party identity for participant attribution. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |

The contract surface enforces a **best-effort** blocklist on the free-text fields `CorrelationId`, `AuditHandle`, `DeveloperGuidance`, `SafeMessage`, and `SafeFieldDiagnostics` entries. The blocklist rejects substrings like `EventStore`, `stream`, `snapshot`, `dispatcher`, `handler`, `repository`, `aggregate identity`, `raw upstream`, and known leak markers. The primary non-disclosure mechanism is the closed-vocabulary `Code`, `Category`, and `ClientAction`; treat the blocklist as a guardrail against accidental drift, not as a complete enforcement layer.

### Freshness and Result Shapes

Read contracts use the approved freshness vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. Future behavior stories decide when non-current states are acceptable; this package only defines the public vocabulary.

Create flows return `ConversationCreatedResult`. Its positional constructor is `(SchemaVersion, TenantId, ConversationId, CorrelationId, IdempotencyKey, ReadModelVisibility, ConversationCommandType)` — `CommandType` is the trailing positional parameter so adding it does not break adopter call sites that use positional construction for the leading fields.

`ConversationCommandAcceptedResult.ConversationId` remains non-null because accepted non-create commands target an existing tenant-scoped conversation.

Future implementation stories should keep the current readiness decisions and ADR tracker visible:

- `_bmad-output/implementation-artifacts/readiness-gate-decisions-2026-05-17.md`
- `_bmad-output/implementation-artifacts/readiness-gates.md`
- `docs/adrs/index.md`

## Local Validation

Use the repository-pinned .NET SDK from `global.json`:

```powershell
dotnet restore Hexalith.Conversations.slnx
dotnet build Hexalith.Conversations.slnx
dotnet test Hexalith.Conversations.slnx
```

If the local sandbox blocks VSTest socket creation, build the affected test project and run its compiled xUnit v3 executable directly. See [test framework guidance](tests/README.md#vstest-socket-fallback).

The scaffold smoke checks must not require Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, external cloud resources, or nested submodule initialization.

## Submodules

Root-level sibling modules are preserved through `.gitmodules`. Do not run recursive submodule initialization for this repository unless nested submodules are explicitly requested.
