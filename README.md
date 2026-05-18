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

Typed errors are machine-readable first. Error contracts use stable codes such as `tenant_isolation_violation`, `aggregate_not_found`, `tenant_projection_stale`, `audit_sink_unavailable`, `audit_pairing_required`, `idempotency_conflict`, `schema_version_unsupported`, and `command_validation_failed`. Error details must remain content-safe: no inaccessible tenant IDs, Party personal data, conversation existence disclosure, redacted content, provider payloads, storage internals, raw exceptions, or cross-tenant business references.

Read contracts use the approved freshness vocabulary: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted`. Future behavior stories decide when non-current states are acceptable; this package only defines the public vocabulary.

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
