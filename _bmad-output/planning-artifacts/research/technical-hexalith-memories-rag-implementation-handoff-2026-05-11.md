---
artifactType: implementation-handoff
topic: Hexalith.Memories RAG integration for Hexalith.Conversations
date: 2026-05-11
author: Jerome
sourceResearch:
  - _bmad-output/planning-artifacts/research/technical-hexalith-memories-rag-for-conversation-memories-research-2026-05-11.md
status: draft
---

# Implementation Handoff: Conversation Memories via Hexalith.Memories

## Decision Summary

Treat `Hexalith.Memories` as a **derived RAG index**. `Hexalith.Conversations` stays the governed source of truth for conversation history, tenant authorization, redaction, retention, Party attribution, and audit evidence.

This work is **not v1 by default**. The current Conversations PRD explicitly excludes semantic memory, vector search, and automatic summarization from v1. The implementation below should be used for vNext, v1.1+, or an explicitly approved scope promotion.

## First Slice

Build a narrow message-level memory integration:

1. Index only eligible `MessageAppended` content.
2. Use one Memories case per conversation: `caseId = conversationId`.
3. Use one memory unit per message.
4. Use async indexing after the conversation event is durable.
5. Store indexing status in a Conversations projection.
6. Expose retrieval only through a Conversations-owned API/tool.
7. Re-check tenant, conversation access, redaction, and policy version after Memories returns results.

## Project Boundary

Recommended project ownership:

| Project | Responsibility |
| --- | --- |
| `Hexalith.Conversations.Contracts` | No Memories references. Durable contracts remain Conversations-only. |
| `Hexalith.Conversations` / domain | No Memories references. Aggregates do not index or search. |
| `Hexalith.Conversations.Workers` or server edge | Owns `IConversationMemoryIndexer`, Memories adapter, retry/status mapping. |
| `Hexalith.Conversations.Server` | Owns authorized retrieval facade if exposed over HTTP/MCP. |
| `Hexalith.Conversations.Tests` | Pure aggregate and projection tests. |
| `Hexalith.Conversations.Workers.Tests` | Indexer adapter and policy tests. |
| `Hexalith.Conversations.IntegrationTests` | Optional Aspire/Dapr/real Memories E2E tests. |

## Package References

Worker/server edge only:

```xml
<PackageReference Include="Hexalith.Memories.Client.Rest" />
<PackageReference Include="Hexalith.Memories.Contracts" />
```

If using project references during local development, keep them scoped to worker/server projects. Do not let `Contracts` or aggregate projects reference Memories.

## DI Registration

`Hexalith.Memories.Client.Rest` already exposes `AddMemoriesClient(...)`:

```csharp
builder.Services.AddMemoriesClient(options =>
{
    options.Endpoint = builder.Configuration.GetValue<Uri>("Memories:Endpoint");
    options.ApiToken = builder.Configuration["Memories:ApiToken"];
});
```

For Dapr service invocation, the Memories MCP host demonstrates `DaprClient.CreateInvokeHttpClient(appId)`. A Conversations adapter can use the same shape when running inside a Dapr-enabled topology:

```csharp
builder.Services.AddScoped<MemoriesClient>(sp =>
{
    HttpClient httpClient = Dapr.Client.DaprClient.CreateInvokeHttpClient("memories-server");
    return new MemoriesClient(
        httpClient,
        sp.GetRequiredService<IOptions<MemoriesClientOptions>>(),
        sp.GetRequiredService<ILogger<MemoriesClient>>());
});
```

Prefer endpoint configuration for the first unit-testable adapter, then add Dapr invocation in the AppHost/integration layer.

## Conversations Ports

Add a Conversations-owned port:

```csharp
public interface IConversationMemoryIndexer
{
    Task<ConversationMemoryIndexResult> IndexMessageAsync(
        ConversationMemoryIndexRequest request,
        CancellationToken cancellationToken);

    Task<ConversationMemoryDeleteResult> DeleteMessageMemoryAsync(
        ConversationMemoryDeleteRequest request,
        CancellationToken cancellationToken);
}
```

Recommended request:

```csharp
public sealed record ConversationMemoryIndexRequest
{
    public required string TenantId { get; init; }
    public required string ConversationId { get; init; }
    public required string MessageId { get; init; }
    public required long Sequence { get; init; }
    public required string EventId { get; init; }
    public required string PartyId { get; init; }
    public required string Role { get; init; }
    public required string RedactionAwareContent { get; init; }
    public required string PolicyVersion { get; init; }
    public string? ProjectId { get; init; }
    public string? FolderId { get; init; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}
```

Recommended result:

```csharp
public sealed record ConversationMemoryIndexResult(
    string SourceUri,
    string WorkflowInstanceId,
    ConversationMemoryIndexStatus Status);
```

## Source URI Convention

Use a stable URI that is deterministic across retry/replay:

```text
hexalith-conversations://{tenantId}/conversations/{conversationId}/messages/{messageId}
```

If tenant ID is considered sensitive in derived metadata, use:

```text
hexalith-conversations://conversations/{conversationId}/messages/{messageId}
```

and rely on the separate `tenantId` field passed to Memories.

The source URI is load-bearing because Memories deduplicates ingestion by source URI in its workflow.

## Metadata Contract

Use `MetadataOrigin.Human` for values copied from user/domain input and `MetadataOrigin.Ai` only for derived/generated fields. Confidence should be `1.0f` for deterministic domain metadata.

```csharp
static Dictionary<string, MetadataField> BuildMetadata(ConversationMemoryIndexRequest request)
    => new(StringComparer.Ordinal)
    {
        ["conversations.conversationId"] = Field(request.ConversationId),
        ["conversations.messageId"] = Field(request.MessageId),
        ["conversations.eventId"] = Field(request.EventId),
        ["conversations.sequence"] = Field(request.Sequence.ToString(CultureInfo.InvariantCulture)),
        ["conversations.partyId"] = Field(request.PartyId),
        ["conversations.role"] = Field(request.Role),
        ["conversations.policyVersion"] = Field(request.PolicyVersion),
        ["conversations.indexedFrom"] = Field("message-appended"),
    };

static MetadataField Field(string value)
    => new(value, MetadataOrigin.Human, 1.0f);
```

Optional fields:

- `conversations.projectId`
- `conversations.folderId`
- `conversations.correlationId`
- `conversations.causationId`
- `conversations.schemaVersion`
- `conversations.redactionState`

Avoid:

- Party display names
- email addresses or contact channels
- raw attachment paths if path policy treats them as sensitive
- provider prompts or provider secrets
- raw tenant display names
- unbounded metadata values

## Adapter Sketch

Current `MemoriesClient.IngestAsync(...)` is marked experimental (`HXL001`) and always sends `SourceType.File`. For the first slice that is acceptable if the content is plain text and metadata states that it came from Conversations. Longer term, add a generic Memories client overload that accepts `SourceType.Discussion`.

```csharp
public sealed class MemoriesConversationMemoryIndexer : IConversationMemoryIndexer
{
    private readonly MemoriesClient _client;

    public MemoriesConversationMemoryIndexer(MemoriesClient client)
    {
        _client = client;
    }

    public async Task<ConversationMemoryIndexResult> IndexMessageAsync(
        ConversationMemoryIndexRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sourceUri = ConversationMemorySourceUri.ForMessage(
            request.TenantId,
            request.ConversationId,
            request.MessageId);

        byte[] payload = Encoding.UTF8.GetBytes(request.RedactionAwareContent);
        Dictionary<string, MetadataField> metadata = BuildMetadata(request);

#pragma warning disable HXL001
        string workflowInstanceId = await _client.IngestAsync(
            request.TenantId,
            request.ConversationId,
            sourceUri,
            payload,
            "text/plain",
            "hexalith-conversations",
            metadata,
            cancellationToken).ConfigureAwait(false);
#pragma warning restore HXL001

        return new ConversationMemoryIndexResult(
            sourceUri,
            workflowInstanceId,
            ConversationMemoryIndexStatus.Scheduled);
    }
}
```

## Retrieval Facade

Expose a Conversations-owned retrieval API. Do not expose raw Memories search to chatbot/agent callers.

```csharp
public interface IConversationMemoryRetriever
{
    Task<ConversationMemoryContext> RetrieveAsync(
        ConversationMemoryQuery query,
        CancellationToken cancellationToken);
}
```

Flow:

1. Validate tenant context.
2. Validate conversation access.
3. Validate conversation memory feature is enabled for tenant/release.
4. Query Memories with `HybridSearchRequest`.
5. Filter by Conversations indexing projection.
6. Drop stale/redacted/deleted/policy-mismatched units.
7. Return citations and snippets, not raw Memories results.

```csharp
HybridSearchResult result = await memoriesClient.HybridSearchAsync(
    new HybridSearchRequest(
        TenantId: tenantId,
        Query: queryText,
        CaseId: conversationId,
        MaxResults: Math.Clamp(maxResults, 1, 20),
        Explain: false,
        TokenBudget: tokenBudget),
    cancellationToken);
```

## Indexing Projection

Add a Conversations projection keyed by:

```text
tenantId / conversationId / messageId
```

Projection fields:

| Field | Purpose |
| --- | --- |
| `sourceUri` | deterministic dedup and deletion correlation |
| `workflowInstanceId` | status polling/diagnostics |
| `memoryUnitId` | delete/re-index when known |
| `status` | pending/scheduled/indexed/failed/skipped/stale/deleted |
| `policyVersion` | result eligibility check |
| `contentHash` | skip unchanged re-index |
| `indexedAt` | freshness |
| `lastFailureCode` | operator diagnostics |
| `lastFailureMessageSafe` | content-safe diagnostics only |

If `memoryUnitId` is not returned by the schedule call, resolve it through workflow completion or search/inspect by `sourceUri` if Memories exposes that path. If no direct lookup exists, add a small Memories client/server endpoint before relying on delete/re-index in production.

## EventStore / CloudEvents Option

Use Memories EventStore integration later for raw event indexing or graph enrichment.

CloudEvent conventions:

```text
id: conversations-{conversationId}-{eventPosition}
source: hexalith/conversations/{tenantId}
type: Hexalith.Conversations.Conversation.MessageAppendedV1
subject: {conversationId}
datacontenttype: application/json
```

Memories maps CloudEvent data to `SourceType.Event`, stores CloudEvent metadata, and can generate a natural-language event description for event semantic search.

Do not use this as the first user-facing prompt-memory path unless the event payload is already redaction-safe and semantically useful.

## Redaction And Retention Rules

Before indexing:

- Skip non-indexable messages.
- Skip or redact sensitive content according to tenant policy.
- Use the projected/displayable content, not raw event payload, when policy requires redaction.

After indexing:

- On redaction: delete the old memory unit and optionally re-index redacted content.
- On retention expiry: delete memory units or mark them tombstoned and suppress retrieval.
- On policy version change: mark units stale, then re-index if still eligible.
- On legal hold: follow the governance ADR. Do not assume legal hold means searchable.

Release blocker for real tenants: redacted/deleted content must not appear in Memories search results after the propagation SLO.

## Test Plan

Unit tests:

- `IndexMessageAsync` builds stable source URI.
- Metadata uses stable IDs and excludes Party personal data.
- Empty/redacted/non-indexable content is skipped before Memories call.
- Payload over 1 MB maps to skipped/terminal status or chunking-required status.
- `MemoriesRemoteException` maps to retryable/terminal indexing status.
- Cancellation token is passed through.

Projection tests:

- Duplicate message event does not create duplicate active memory mapping.
- Redaction event marks existing mapping stale/deleted.
- Policy version mismatch suppresses retrieval.

Retrieval tests:

- Unauthorized tenant fails before Memories search.
- Unauthorized conversation fails before Memories search.
- Memories result for unknown mapping is dropped.
- Memories result for stale policy is dropped.
- Memories degraded result surfaces safe degraded status.
- Token budget is enforced.

Integration tests:

- Message append eventually becomes searchable.
- Duplicate indexing request is idempotent.
- Redacted message no longer returns.
- Cross-tenant search cannot retrieve another tenant conversation.
- Memories outage does not roll back Conversation write.

## Required ADRs

1. `ADR-CMEM-001`: Memories is derived index, not source of truth.
2. `ADR-CMEM-002`: Case model starts as one case per conversation.
3. `ADR-CMEM-003`: Source URI and idempotency convention.
4. `ADR-CMEM-004`: Redaction/delete/re-index semantics.
5. `ADR-CMEM-005`: Metadata classification and PII exclusion.
6. `ADR-CMEM-006`: Retrieval facade and authorization order.
7. `ADR-CMEM-007`: EventStore CloudEvent conventions for future raw event indexing.
8. `ADR-CMEM-008`: Large-message behavior and chunking threshold.

## Open Gaps

- `MemoriesClient.IngestAsync(...)` does not currently let callers choose `SourceType.Discussion`; it hard-codes `SourceType.File`.
- The client has `GetMemoryUnitAsync` and search, but no obvious first-class `DeleteMemoryUnitAsync`; server has the endpoint. Add client coverage before lifecycle work.
- Need a reliable way to resolve `memoryUnitId` from a scheduled ingestion if delete/re-index is required.
- Memories Server endpoints are not all authenticated in the local docs; Conversations must not expose it directly to callers.
- Conversations runtime projects do not appear to exist yet, so this remains a handoff for upcoming architecture/story work.

## Recommended Next Action

Write the ADR pack first, then implement a fake-port worker story. The fake-port story should not call Memories yet; it should prove source URI, metadata, eligibility, status projection, and retrieval authorization. After that, add the real `MemoriesClient` adapter and one Aspire integration test.
