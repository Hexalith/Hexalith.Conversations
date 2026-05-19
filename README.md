# Hexalith.Conversations

Hexalith.Conversations is a tenant-scoped conversations module scaffold for the Hexalith ecosystem.

This initial scaffold is intentionally non-operative. It establishes project boundaries, central package management, smoke tests, and ADR tracking for future stories without implementing conversation persistence, tenant authorization, provider integrations, workers, read models, governance commands, or FrontComposer runtime behavior.

## Contract Package Guidance

The supported v1 integration path is a .NET client that shares `Hexalith.Conversations.Contracts` with adopter applications. Adopters should create commands and interpret results, projections, and typed errors through these shared contracts; raw EventStore knowledge is not required for normal integration.

Identity has a strict taxonomy:

- `ConversationId` is the Conversations-owned durable identity and is always interpreted with `TenantId`.
- `TenantId`, `PartyId`, `ProjectId`, `FolderId`, `FileId`, and `MessageId` are stable references to owning modules or Conversations records.
- `BusinessReference` is an adopter-owned external reference and never replaces `ConversationId`.
- `ProviderCorrelationMetadata` stores opaque provider/thread correlation data only; it is not tenant authority, actor attribution, idempotency, or aggregate identity.
- UI labels and thread names are display/correlation metadata only.
- Correlation IDs, causation IDs, and idempotency keys describe operations, not conversation identity.

Participant attribution follows the same rule: durable conversation events store stable `PartyId` references plus closed participant `type` and `role` vocabulary only. Party display names, contact values, personal details, organization details, and raw Parties failure data are read-time/application-boundary concerns and must not be persisted in Conversations events. Provider session IDs, model labels, thread names, and external user identifiers can only be transient correlation or validation inputs; they never replace `PartyId` as participant authority.

Command-time participant validation is intentionally fail-closed. If Parties cannot prove that the target `PartyId` is valid and visible for the command tenant, the add-participant command returns a typed Conversations rejection such as `participant_validation_unavailable` or `tenant_context_mismatch` and no `ParticipantAdded` event is emitted. Read-time Party hydration can later degrade according to the readiness decisions, but write-side participant membership cannot compensate by storing hydrated Party data.

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

Closed-vocabulary values (`ProjectionTrustState`, `ConversationErrorCode`, `ConversationErrorCategory`, `ConversationEventType`, `ConversationCommandType`, `ParticipantType`, `ParticipantRole`) serialize as plain strings in their canonical form. Matching on read is **case-sensitive** — `"Current"` is valid, `"current"` is not. The README and IntelliSense are the single source of canonical spellings.

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

The contract surface enforces a **best-effort** blocklist on the free-text fields `CorrelationId`, `AuditHandle`, `DeveloperGuidance`, and `SafeFieldDiagnostics` entries. The blocklist rejects substrings like `EventStore`, `stream`, `snapshot`, `dispatcher`, `handler`, `repository`, `aggregate identity`, `raw upstream`, and known leak markers. The primary non-disclosure mechanism is the closed-vocabulary `Code` and `Category`; treat the blocklist as a guardrail against accidental drift, not as a complete enforcement layer.

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

The scaffold smoke checks must not require Aspire runtime launch, Dapr sidecars, tenant seed data, production secrets, provider credentials, external cloud resources, or nested submodule initialization.

## Submodules

Root-level sibling modules are preserved through `.gitmodules`. Do not run recursive submodule initialization for this repository unless nested submodules are explicitly requested.
