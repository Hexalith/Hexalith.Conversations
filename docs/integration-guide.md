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

Mutating commands require idempotency metadata. Reuse the same idempotency key only for retrying the same logical request. A changed request must use a new key when the client receives `idempotency_conflict`; an uncertain transport outcome maps to `idempotency_outcome_unknown`. Retry with the same metadata when the error is retryable and specifies `retry-same-request`. If the original receipt expired, reconcile the original conversation and message before issuing another mutation.

Typed failures use `ConversationError` with closed `ConversationErrorCode`, `ConversationErrorCategory`, and `ConversationErrorClientAction` values. The canonical table is [Typed Errors](../src/Hexalith.Conversations.Contracts/README.md#typed-errors); branch on the closed fields, not on transport status text.

Read results expose `ProjectionTrustState` and `ProjectionFreshnessV1`. Only `Current` is trust-bearing. `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, and `Redacted` are explicit non-current states that callers must render or remediate without pretending data is current.

Published events carry safe metadata such as schema version, `TenantId`, `ConversationId`, correlation id, causation id, actor `PartyId`, and deduplication key. The public contract is the versioned event shape, not storage mechanics.

Governance behavior is fail-closed where audit pairing, retention, redaction, or sensitivity rules require proof. Public responses stay content-safe: allowed `auditHandle` values may be returned only by errors whose descriptors permit them, and redaction policy internals are not disclosed.

Caller metadata is optional provenance attached to `CreateConversationCommand`, `AppendMessageCommand`, and `UpdateConversationMetadataCommand`. `CallerMetadata` is provenance only; it never becomes tenant identity, authorization truth, governance trust, or idempotency authority. The approved fields, bounds, and reject policy are in [Caller Metadata (Provenance Only)](../src/Hexalith.Conversations.Contracts/README.md#caller-metadata-provenance-only).

Compatibility discovery is exposed through `ConversationContractCompatibility.Current` and `ConversationContractCompatibility.Evaluate(...)`. Current v1 metadata is `supported` for command contracts, projection contracts, and event contracts with `Hexalith.Conversations.Contracts` version `1.0.0` and `Hexalith.Conversations.Client` version `1.0.0`. The FR81 compatibility, deprecation, unsupported-version, and release-evidence classification rules are published in the [Contract Compatibility and Deprecation Policy](release-evidence/contract-compatibility-policy.md).

CORE preconditions are exposed through `ConversationCorePreconditionCatalog.All`. They cover projection freshness, audit availability, supported schema versions, contract compatibility, participant identity validation, idempotency behavior, projection health, and required configuration. The canonical table is [CORE Preconditions and Onboarding Diagnostics](../src/Hexalith.Conversations.Contracts/README.md#core-preconditions-and-onboarding-diagnostics).

## Resolution Scoring Label Opt-in

The optional `ResolutionScoringLabel` fields on creation and metadata command/event contracts record explicit owner classification for internal Projects equality scoring. Use the exact supported purpose and owner classification with the same native `Label`; the value is bounded to 160 characters and existing safe-metadata rules. A classified null records explicit absence only when the native label is null. Ordinary labels remain descriptive. See [Resolution Scoring Labels (Source Contracts)](../src/Hexalith.Conversations.Contracts/README.md#resolution-scoring-labels-source-contracts) for the exact constants, constructor compatibility and rejection rules.

Native creation and replay preserve opt-in. A label update without classification invalidates it; attribute-only and business-reference-only updates preserve it. Explicit reclassification requires a later supported native event and changes idempotent command meaning. Old implicitly classified metadata without persisted purpose remains unavailable, and its original dispatch is refused without modifying stored generations. Reads perform no migration or classification.

This source/replay capability does not add a deployed metadata-mutation route. Production mutation dispatch/adoption and writer enrollment, current native owner authority, guard/rebuild support, installation coverage and qualification must be established independently before relying on the internal scoring read. Existing delegation and permission checks still apply; label classification supplies no authorization. Projects RTI and parent resolution implementation remain gated.

## .NET Client Workflow

The examples use the v1 client package. The [package consumer](../tests/packaged-client-journey/Program.cs) exercises the same HTTP routes against a running installation.

Application code should request `IConversationClient` from dependency injection and call the typed methods shown below. Registration is provided by `ConversationClientServiceCollectionExtensions`, with endpoint configuration carried by `ConversationClientOptions`.

### Register the Client

```csharp
using System.Net.Http.Headers;
using Hexalith.Conversations.Client;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();

services.AddHexalithConversationsClient(options =>
{
    options.Endpoint = new Uri(conversationsApiOrigin);
}).ConfigureHttpClient(http =>
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken));
```

Use the HTTP(S) origin assigned to the Platform resource `conversations-api` as `conversationsApiOrigin`, including the trailing slash. The client adds `api/v1/conversations`. Obtain a current human bearer for that installation from its identity provider. Use HTTPS for deployed endpoints. The package journey also accepts loopback HTTP for local development.

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
    IdempotencyKey: "<create-idempotency-key>");

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
    (context with { IdempotencyKey = "<append-idempotency-key>" }).ToCommandMetadata(),
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
            if (error.IsRetryable && error.ClientAction == ConversationErrorClientAction.RetrySameRequest)
            {
                // Retry with the same metadata and idempotency key.
            }
            else
            {
                // Reconcile the original conversation and message before issuing another mutation.
            }
        }
        else if (error.Category == ConversationErrorCategory.Freshness
            && error.ClientAction == ConversationErrorClientAction.RetryLater)
        {
            // Retry after the projection or dependency reports a current state.
        }
    }
}
```

### Qualify a running pilot host

Platform exposes the four v1 operations on the `conversations-api` resource. The domain applies tenant and Party authorization and maintains the timeline. Platform operators can find the issuer, tenant history and Party binding settings in the [pilot host configuration](../module/platform-handoff.md#pilot-api-source-composition). The local source deployment passed the installed-client journey with candidate Client and Contracts packages `2.1.1-story93.2`. Published service images require separate qualification.

Provision two distinct tenant members through Tenants. The [local provisioning runner](../tests/packaged-client-journey/provision.py) creates both on a fresh disposable deployment and waits for their durable memberships. The first needs current write and read access and the matching Party binding. The second needs a Party binding and read-only membership in its own tenant. Give both bearers permission to submit to the gateway so the denial test reaches domain authorization. Allow their tenant membership events and index initialization to reach the domain. Each bearer must identify its principal in `sub` and its selected tenant in `tid` or `tenant_id`. The gateway also checks the signed UI surface, allowed tenant, domain and operation permissions. A request header alone cannot grant tenant access.

Run the journey with .NET 11, Python 3, an isolated local deployment and a feed containing matching Client and Contracts packages. Set the following environment variables in the invoking process.

| Input | Value |
| --- | --- |
| `CONVERSATIONS_ORIGIN` | Public origin of `conversations-api` |
| `CONVERSATIONS_TOKEN`, `CONVERSATIONS_TENANT`, `CONVERSATIONS_SUBJECT`, `CONVERSATIONS_PARTY` | First member's short-lived bearer and bound identities |
| `CONVERSATIONS_OTHER_TOKEN`, `CONVERSATIONS_OTHER_TENANT`, `CONVERSATIONS_OTHER_SUBJECT`, `CONVERSATIONS_OTHER_PARTY` | Independently authenticated second member and tenant |
| `CONVERSATIONS_DOMAIN_DAPR_ORIGIN`, `CONVERSATIONS_READ_MODEL_STORE` | Private loopback URL for the domain sidecar and its configured read-model state component |
| `CONVERSATIONS_DOMAIN_DAPR_TOKEN` | Sidecar API token when enabled |
| `CONVERSATIONS_CATALOG_RECEIPT` | Run-owned routing catalog receipt from the gateway |

```bash
python3 tests/packaged-client-journey/run.py --feed "$PACKAGE_FEED" --version "$PACKAGE_VERSION" \
    --apphost "$PLATFORM_APPHOST" --fault-api-dll "$CONVERSATIONS_API_DLL"
```

Set `PLATFORM_APPHOST` to the running local AppHost file and `CONVERSATIONS_API_DLL` to the built pilot API assembly. The full run checks the gateway's command-status TTL and terminal idempotency retention, as described in the [pilot host configuration](../module/platform-handoff.md#pilot-api-source-composition), and the reader's exact Party binding in the running domain before trying the denied create. The API and domain hosts must keep synchronized clocks, as the [pilot host configuration](../module/platform-handoff.md#pilot-api-source-composition) describes. It installs exact packages into an empty cache, verifies each mutation and its replay, and checks changed intent and denied requests. It interrupts status confirmation for the accepted append, observes the durable message through the domain sidecar, then retries through the regular API. It allows 60 seconds for current list and detail and checks the hidden response bodies and durable timeline.

Use `--without-status-outage` in place of both AppHost arguments for an explicitly partial live run. Use `--build-only` to verify package installation and compilation. Each mode prints its own result after its checks finish.

Run `python3 -m unittest discover -s tests/packaged-client-journey -v` to check the runner’s argument validation, host settings checks and receipt matching without a pilot deployment.

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

- `tenant_projection_stale`: retry when conversation state could not be loaded. A denied tenant-access request returns a single hidden onboarding result, without revealing projection health; a missing, gapped, corrupt, malformed, stale, rolled-back or unavailable tenant access state denies as non-retryable `tenant_isolation_violation`, the same as an unknown tenant.
- `schema_version_unsupported`: use supported v1 contract and client versions.
- `audit_sink_unavailable`: retry after audit recording is available.
- `audit_pairing_required`: provide required audit evidence before retrying.
- `idempotency_conflict`: send a changed request with a new idempotency key.
- `idempotency_outcome_unknown` with `isRetryable: true` and `retry-same-request` calls for the same request and idempotency metadata. An expired receipt returns HTTP 409 with `isRetryable: false` and asks you to reconcile the original outcome.
- `aggregate_not_found`: hide or refresh the unavailable target view without revealing existence.
- `participant_validation_unavailable`: retry when participant identity validation is available.

For onboarding and precondition diagnostics, use the canonical `ready`, `degraded`, `blocked`, and `unknown` statuses from the contracts README. Do not expose policy internals, provider content, local machine paths, or production failure text in adopter-facing messages.
