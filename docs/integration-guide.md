# Hexalith Conversations Developer Integration Guide

This guide is the adopter-facing path for using Conversations through `Hexalith.Conversations.Client` and `Hexalith.Conversations.Contracts`. It consolidates the shipped v1 contract, client, typed-error, CORE precondition, conformance, caller-metadata, and release-evidence surfaces. The canonical catalog tables remain in [the root README](../README.md#contract-package-guidance) and [the contracts README](../src/Hexalith.Conversations.Contracts/README.md); this guide links to them instead of duplicating drift-prone tables.

## Responsibility Boundaries

Conversations owns the tenant-scoped conversation record, durable `ConversationId` identity, stable participant attribution through `PartyId`, adopter-owned `BusinessReference` linkage, idempotent commands, versioned domain events, projection freshness and trust state, typed sanitized errors, compatibility discovery, and CORE preconditions.

Conversations does not own chatbot or agent orchestration, LLM provider behavior, legal-hold systems, attachment or file storage, the identity provider, tenant lifecycle in `Hexalith.Tenants`, Party personal data in `Hexalith.Parties`, project, folder, or file lifecycle, or upstream business-record lifecycle. Provider correlation is opaque correlation metadata only; it is never durable conversation identity, tenant authority, participant authority, or idempotency authority.

The supported v1 path is the typed .NET client plus shared contracts. Do not publish or rely on raw HTTP fallback examples unless a later approved diagnostics scope records that exception.

For formal boundary accountability including operator, buyer evaluator, and compliance stakeholder guidance — owner, source of truth, failure semantics, evidence obligation, and handoff path for each adjacent system — see [Responsibility Boundaries](responsibility-boundaries.md).

## CORE Behavior

Use `ConversationClientContext` to bind each call to a trusted tenant context, stable actor `PartyId`, caller principal, correlation id, optional causation id, and idempotency key. This tenant binding is fail-closed. The server still validates tenant access from authenticated context plus the local Tenants projection; the client metadata is not a JWT-only shortcut and cannot bypass fail-closed checks.

Party identity is durable only as `PartyId` plus closed participant vocabulary. Display names, contact values, personal details, and organization details are hydrated at read time where policy allows and are not stored as durable conversation content.

Mutating commands require idempotency metadata. Reuse the same idempotency key only for retrying the same logical request. A changed request must use a new key when the client receives `idempotency_conflict`; an uncertain transport outcome maps to `idempotency_outcome_unknown` and should be retried with the same metadata.

Typed failures use `ConversationError` with closed `ConversationErrorCode`, `ConversationErrorCategory`, and `ConversationErrorClientAction` values. The canonical table is [Typed Errors](../src/Hexalith.Conversations.Contracts/README.md#typed-errors); branch on the closed fields, not on transport status text.

Read results expose `ProjectionTrustState` and `ProjectionFreshnessV1`. Only `Current` is trust-bearing. `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted` are explicit non-current states that callers must render or remediate without pretending data is current.

Published events carry safe metadata such as schema version, `TenantId`, `ConversationId`, correlation id, causation id, actor `PartyId`, and deduplication key. The public contract is the versioned event shape, not storage mechanics.

Governance behavior is fail-closed where audit pairing, retention, redaction, or sensitivity rules require proof. Public responses stay content-safe: allowed `auditHandle` values may be returned only by errors whose descriptors permit them, and redaction policy internals are not disclosed.

Caller metadata is optional provenance attached to `CreateConversationCommand`, `AppendMessageCommand`, and `UpdateConversationMetadataCommand`. `CallerMetadata` is provenance only; it never becomes tenant identity, authorization truth, governance trust, or idempotency authority. The approved fields, bounds, and reject policy are in [Caller Metadata (Provenance Only)](../src/Hexalith.Conversations.Contracts/README.md#caller-metadata-provenance-only).

Compatibility discovery is exposed through `ConversationContractCompatibility.Current` and `ConversationContractCompatibility.Evaluate(...)`. Current v1 metadata is `supported` for command contracts, projection contracts, and event contracts with `Hexalith.Conversations.Contracts` version `1.0.0` and `Hexalith.Conversations.Client` version `1.0.0`. The FR81 compatibility, deprecation, unsupported-version, and release-evidence classification rules are published in the [Contract Compatibility and Deprecation Policy](release-evidence/contract-compatibility-policy.md).

CORE preconditions are exposed through `ConversationCorePreconditionCatalog.All`. They cover projection freshness, audit availability, supported schema versions, contract compatibility, participant identity validation, idempotency behavior, projection health, and required configuration. The canonical table is [CORE Preconditions and Onboarding Diagnostics](../src/Hexalith.Conversations.Contracts/README.md#core-preconditions-and-onboarding-diagnostics).

## .NET Client Workflow

The snippets below are embedded instead of placed in a standalone sample project because the v1 deliverable is documentation plus deterministic docs validation. They are validated by contract tests against the shipped type and member names, while avoiding a globally runnable host or extra build dependencies.

Application code should request `IConversationClient` from dependency injection and call the typed methods shown below. Registration is provided by `ConversationClientServiceCollectionExtensions`, with endpoint configuration carried by `ConversationClientOptions`.

### Register the Client

```csharp
using Hexalith.Conversations.Client;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();

services.AddHexalithConversationsClient(options =>
{
    options.Endpoint = new Uri("https://docs.hexalith.local/conversations/api/");
});
```

### Create a Conversation

```csharp
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;

ConversationClientContext context = new(
    new TenantId("<tenant-id>"),
    new PartyId("<actor-party-id>"),
    "<caller-principal-id>",
    "<correlation-id>",
    IdempotencyKey: "<idempotency-key>");

CreateConversationCommand create = new(
    context.ToCommandMetadata(),
    new BusinessReference("<business-system>", "<record-key>"),
    Label: "Support conversation");

ConversationClientResult<ConversationCreatedResult> created =
    await client.CreateConversationAsync(create, cancellationToken);
```

### Append a Message

```csharp
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;

AppendMessageCommand append = new(
    context.ToCommandMetadata(),
    created.Value!.ConversationId,
    new MessageId("<message-id>"),
    context.ActorPartyId,
    "Message text approved for Conversations content handling.");

ConversationClientResult<ConversationCommandAcceptedResult> appended =
    await client.AppendMessageAsync(append, cancellationToken);
```

### Read the Timeline and Freshness

```csharp
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

GetConversationQuery query = context.ToGetConversationQuery(created.Value!.ConversationId);
ConversationClientResult<ConversationDetailResult> detail =
    await client.GetConversationAsync(query, cancellationToken);

if (detail.IsSuccess && detail.Value!.FreshnessState == ProjectionTrustState.Current)
{
    ProjectionFreshnessV1 freshness = detail.Value.Details!.Freshness;
    bool trustBearing = freshness.AllowsTrustBearingDecision();
    IReadOnlyList<ConversationTimelineMessageProjectionV1> messages = detail.Value.Details.Messages;
}
```

### Handle Typed Errors and Retry Idempotently

```csharp
using Hexalith.Conversations.Contracts.Errors;

if (!created.IsSuccess)
{
    foreach (ConversationError error in created.Error!.Errors)
    {
        if (error.Code == ConversationErrorCode.IdempotencyConflict)
        {
            // Changed command payload: correct the request and send it with a new idempotency key.
        }
        else if (error.Code == ConversationErrorCode.IdempotencyOutcomeUnknown)
        {
            // Unknown outcome: retry the same command metadata and idempotency key.
        }
        else if (error.Category == ConversationErrorCategory.Freshness
            && error.ClientAction == ConversationErrorClientAction.RetryLater)
        {
            // Retry after the projection or dependency reports a current state.
        }
    }
}
```

### Discover Compatibility

```csharp
using Hexalith.Conversations.Contracts.Versioning;

ContractCompatibilityMetadata active = ConversationContractCompatibility.Current;

ContractCompatibilityResult compatibility = ConversationContractCompatibility.Evaluate(
    new ContractCompatibilityRequest(
        CommandSchemaVersion: active.CommandContracts.ActiveSchemaVersion.Value.ToString(),
        ProjectionSchemaVersion: active.ProjectionContracts.ActiveSchemaVersion.Value.ToString(),
        EventSchemaVersion: active.EventContracts.ActiveSchemaVersion.Value.ToString(),
        ContractsPackageVersion: active.ContractsPackage.Version,
        ClientPackageVersion: active.ClientPackage.Version));
```

### Inspect CORE Preconditions

```csharp
using Hexalith.Conversations.Contracts.Diagnostics;

foreach (CorePreconditionV1 precondition in ConversationCorePreconditionCatalog.All)
{
    string preconditionId = precondition.PreconditionId;
    string requiredState = precondition.RequiredTrustState.Value;
    string unmetErrorCode = precondition.UnmetErrorCode.Value;
}
```

### Run Conformance Tests

Run the adopter conformance suite as part of CI:

```bash
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
```

The serialized result is `ConformanceRunResultV1`. `overallOutcome` uses the closed `ready`, `degraded`, `blocked`, and `unknown` vocabulary. Only `ready` is trust-bearing, and per-check failures carry bounded classifications and shared typed errors.

## Failure Modes and Remediation

Content-safe failures return closed fields: `code`, `category`, `clientAction`, retryability, safe message, correlation id, optional allowed `auditHandle`, safe diagnostics, and an HTTPS documentation pointer such as `https://docs.hexalith.local/conversations/contracts/v1/errors`.

Do not bypass fail-closed gates. Tenant isolation, audit pairing, schema compatibility, idempotency metadata, and projection freshness must remain enforced. A denied or inaccessible conversation collapses to the hidden `aggregate_not_found` shape and does not disclose whether the target exists.

Use these public responses:

- `tenant_projection_stale`: retry after tenant access and projection preconditions report `Current`.
- `schema_version_unsupported`: use supported v1 contract and client versions.
- `audit_sink_unavailable`: retry after audit recording is available.
- `audit_pairing_required`: provide required audit evidence before retrying.
- `idempotency_conflict`: send a changed request with a new idempotency key.
- `idempotency_outcome_unknown`: retry the same request with the same idempotency metadata.
- `aggregate_not_found`: hide or refresh the unavailable target view without revealing existence.
- `participant_validation_unavailable`: retry when participant identity validation is available.

For onboarding and precondition diagnostics, use the canonical `ready`, `degraded`, `blocked`, and `unknown` statuses from the contracts README. Do not expose policy internals, provider content, local machine paths, or production failure text in adopter-facing messages.
