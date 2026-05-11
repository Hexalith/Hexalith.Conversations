---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/product-brief-Hexalith.Conversations.md
  - _bmad-output/project-context.md
  - Hexalith.Memories/README.md
  - Hexalith.Memories/docs/dev/eventstore-integration.md
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'Using Hexalith.Memories RAG to create conversation memories'
research_goals: 'Determine how Hexalith.Conversations should use Hexalith.Memories as a derived RAG/semantic-memory index while preserving Conversations as the governed source of truth.'
user_name: 'Jerome'
date: '2026-05-11'
web_research_enabled: true
source_verification: true
---

# Research Report: Hexalith.Memories RAG for Conversation Memories

**Date:** 2026-05-11
**Author:** Jerome
**Research Type:** technical

---

## Executive Summary

`Hexalith.Conversations` should not use `Hexalith.Memories` as the authoritative conversation store. Conversations remains the tenant-isolated, event-sourced, governed business record. `Hexalith.Memories` should be a derived RAG index that receives eligible conversation events or curated conversation text, then serves semantic/hybrid recall to agents through an authorization gate owned by Conversations.

The best integration path is event-driven and post-v1:

1. Conversations emits durable, tenant-scoped EventStore events such as `MessageAppended`, `ConversationClosed`, `ConversationSummaryGenerated`, `DecisionCaptured`, and `ActionItemCaptured`.
2. A Conversations memory-indexer worker filters those events by tenant policy, redaction state, content classification, and release scope.
3. The worker sends eligible units to Memories through `Hexalith.Memories.Client.Rest.MemoriesClient.IngestAsync(...)`, or publishes CloudEvents to the Memories EventStore subscription if the event payload itself is the memory.
4. Memories indexes the content through syntactic search, semantic vector search, graph relations, and optional natural-language event descriptions.
5. Conversations exposes an authorized "retrieve conversation memory" API that calls Memories hybrid search with `tenantId`, `caseId`, `maxResults`, and `tokenBudget`, then re-checks result eligibility before returning snippets/context to the chatbot or agent.

This should be a vNext or explicitly promoted v1.1+ capability. The Conversations PRD currently names "semantic memory, vector search, automatic summarization" as out of v1 scope. A safe v1 implementation should only preserve extension points: stable event metadata, schema versions, redaction policy, and source identifiers that make later memory indexing deterministic.

---

## Scope Confirmation

**Research Topic:** Using Hexalith.Memories RAG to create conversation memories.

**Research Goals:** Identify how Conversations can create searchable memories from conversation records using the existing Memories service without breaking tenant isolation, redaction, audit, or v1 scope boundaries.

**Technical Research Scope:**

- Architecture analysis: source-of-truth boundary, event-driven indexing, retrieval facade.
- Integration patterns: REST client ingestion, EventStore/CloudEvents ingestion, Dapr/Aspire wiring.
- Implementation approach: memory unit mapping, source URI/idempotency, metadata, cases, lifecycle.
- Security and governance: tenant authorization, redaction propagation, result eligibility.
- Performance and operations: async indexing, token budgets, hybrid search, failure recovery.

---

## Current State Findings

### Conversations constraints

The Conversations PRD positions Conversations as the durable business record for AI-assisted exchanges. It also explicitly keeps semantic memory, vector search, and automatic summarization out of v1. The long-term vision includes semantic recall, summaries, decision/action extraction, MCP tools, and agent context retrieval.

Implication: Memories integration is architecturally aligned with the long-term product, but must not silently enter v1 scope unless the PRD is updated.

Relevant local sources:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/product-brief-Hexalith.Conversations.md`
- `_bmad-output/project-context.md`

### Memories capabilities

`Hexalith.Memories` already provides the core RAG substrate:

- REST client: `Hexalith.Memories.Client.Rest.MemoriesClient`
- Ingestion endpoint: `POST /api/ingest`
- Workflow status endpoint: `GET /api/ingest/{instanceId}`
- Search endpoint: `GET /api/search`
- Search axes: `syntactic`, `semantic`, `graph`, `hybrid`
- MCP tools: `ingest_content`, `search_memory`, `traverse_relations`
- EventStore integration package: `Hexalith.Memories.EventStore`
- Dapr pub/sub subscription endpoint: `/events/ingest`
- Dapr Workflow ingestion pipeline: validate -> extract -> embed -> index syntactic/semantic/graph -> verify -> dedup
- Tenant/case grouping model
- Memory unit deletion endpoint: `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}`
- 1 MB inline ingestion limit for non-URL payloads

Relevant local sources:

- `Hexalith.Memories/README.md`
- `Hexalith.Memories/docs/dev/eventstore-integration.md`
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/SourceType.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Server/Activities/Ingestion/IngestionInputValidator.cs`
- `Hexalith.Memories/src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs`

### External verification

Current public docs support the local design:

- Azure AI Search documents hybrid retrieval as keyword plus vector retrieval merged into a unified result set, often with better relevance when tuned carefully.
- Dapr pub/sub guarantees at-least-once delivery, so Memories and Conversations handlers must be idempotent.
- Dapr supports dead-letter topics for messages that cannot be processed.
- Aspire Dapr integration supports `WithDaprSidecar`, Dapr service invocation by app ID, and Dapr state store wiring for local orchestration.

Sources:

- Azure AI Search hybrid query: https://learn.microsoft.com/en-us/azure/search/hybrid-search-how-to-query
- Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/
- Aspire Dapr integration: https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr

---

## Recommended Architecture

### Boundary rule

Conversations is authoritative. Memories is derived.

Conversations owns:

- Conversation aggregate and event stream.
- Tenant authorization and resource authorization.
- Party attribution and stable participant identity.
- Retention, redaction, sensitive-data policy, and audit evidence.
- Provider-portable timeline retrieval.
- Conversation projections and freshness semantics.

Memories owns:

- Content extraction where applicable.
- Embedding generation.
- Syntactic/BM25 search.
- Semantic/vector search.
- Graph indexing and traversal.
- Hybrid search result fusion.
- Memory-unit consistency, repair, and search telemetry.

### Preferred flow

```text
Conversation command
  -> ConversationAggregate
  -> EventStore persisted event
  -> Conversations projection update
  -> Memory indexing worker/subscriber
  -> Memories IngestAsync or CloudEvent subscription
  -> Memories Dapr Workflow
  -> Redis/RediSearch + vector index + FalkorDB graph
  -> Conversations authorized memory retrieval API
  -> chatbot/agent prompt context
```

### Cases

Use one Memories `Case` per conversation when the retrieval boundary is a single conversation. This gives exact scoping and maps naturally to `caseId = conversationId`.

Use one Memories `Case` per business context, such as project or folder, only when the product explicitly wants cross-conversation recall inside that context. That is more powerful but has a larger authorization and redaction blast radius.

Recommendation:

- Start with `caseId = conversationId`.
- Add project/folder-scoped cases later through an explicit ADR and conformance tests.

### Memory units

Start with one memory unit per eligible conversation message, not one memory unit per full conversation. Per-message units are easier to delete, redact, update, cite, and test.

Later, add summary/decision/action memory units as separate derived units:

- `SourceType.Discussion` for human-readable message or turn content.
- `SourceType.Event` for raw event-payload indexing through CloudEvents.
- `SourceType.Annotation` for later human or AI annotations.
- `SourceType.Projection` only if indexing a curated projection snapshot.

Recommended first memory unit types:

| Memory unit | SourceType | Created from | Notes |
| --- | --- | --- | --- |
| Message content | `Discussion` or `File` via REST client limitation | `MessageAppended` | Use REST `IngestAsync` first; it currently hard-codes `SourceType.File`. Consider a Memories client extension for generic source types. |
| Raw domain event | `Event` | EventStore CloudEvent | Uses `Hexalith.Memories.EventStore`; good for audit/search of event payloads, less good for user-facing semantic recall unless NL embeddings are enabled. |
| Conversation summary | `Discussion` or `Projection` | governed summarization process | Not v1. Requires summary/redaction ADR. |
| Decision/action extraction | `Annotation` or `Projection` | governed extraction process | vNext. Needs provenance and correction model. |

---

## Concrete Integration Options

### Option A: Worker uses Memories REST client

Use this for message/content memories.

```csharp
#pragma warning disable HXL001
string workflowInstanceId = await memoriesClient.IngestAsync(
    tenantId: tenantId,
    caseId: conversationId,
    sourceUri: $"hexalith-conversations://{tenantId}/conversations/{conversationId}/messages/{messageId}",
    content: Encoding.UTF8.GetBytes(redactionAwareContent),
    contentType: "text/plain",
    ingestedBy: "hexalith-conversations",
    metadata: metadata,
    ct: cancellationToken);
#pragma warning restore HXL001
```

Recommended metadata:

| Key | Value |
| --- | --- |
| `conversations.conversationId` | stable conversation id |
| `conversations.messageId` | stable message id |
| `conversations.eventId` | EventStore event id/position if available |
| `conversations.sequence` | message sequence |
| `conversations.partyId` | speaker Party ID, not display name |
| `conversations.role` | user/assistant/tool/system/etc. |
| `conversations.projectId` | optional stable Project ID |
| `conversations.folderId` | optional stable Folder ID |
| `conversations.attachmentIds` | hashed or bounded list if policy allows |
| `conversations.schemaVersion` | Conversations event/schema version |
| `conversations.redactionState` | clear/redacted/partially-redacted |
| `conversations.policyVersion` | policy version used at indexing time |
| `conversations.indexedFrom` | event/projection/summary |

Pros:

- Best control over redaction-aware content.
- Lets Conversations enforce authorization and policy before indexing.
- Can preserve stable source URI and idempotency key.
- Avoids indexing raw event payloads when user-facing text is safer.

Cons:

- `MemoriesClient.IngestAsync` currently maps to `SourceType.File`.
- Requires a Conversations indexing worker and status projection.
- Payload limit is 1 MB.

Recommendation: use this as the first practical implementation, behind a Conversations-owned adapter such as `IConversationMemoryIndexer`.

### Option B: Publish Conversation events to Memories EventStore integration

Use this for raw event memory and event audit search.

Memories subscribes to one Dapr pub/sub topic and maps CloudEvents to `IngestionInput` with:

- `TenantId` from `SourceToTenantMap`
- `CaseId` from aggregate type routing/auto-create
- `SourceUri = cloudevent.id`
- `SourceType = Event`
- metadata keys such as `cloudevent.id`, `cloudevent.source`, `cloudevent.type`, `cloudevent.subject`, `event.aggregateType`

For Conversations, publish CloudEvents like:

```text
id: conversations-{conversationId}-{eventPosition}
source: hexalith/conversations/{tenantId}
type: Hexalith.Conversations.Conversation.MessageAppendedV1
subject: {conversationId}
datacontenttype: application/json
data: redaction-safe event payload
```

Pros:

- Minimal code in Conversations once EventStore publication exists.
- Preserves event provenance.
- Enables event graph relationships through causation/correlation.
- Uses Memories' existing dual-embedding pipeline for `SourceType.Event`.

Cons:

- Routing is source-prefix based and explicitly not authenticated by itself.
- Production auto-create cases should be disabled or tightly governed.
- Raw event JSON is not always ideal for semantic recall.
- Replays are idempotent by CloudEvent ID and require delete/recreate for rebuild scenarios.

Recommendation: use this only after security/routing ADRs are complete, or use it for internal event observability rather than user-facing prompt memory.

### Option C: Direct MCP tools

Memories exposes MCP tools for `ingest_content`, `search_memory`, and `traverse_relations`.

This is useful for agent/operator experiments, but should not be the production Conversations integration. Conversations needs to preserve its own authorization, redaction, lifecycle, and audit rules. Production agents should call a Conversations-owned memory retrieval tool that delegates to Memories after policy checks.

---

## Retrieval Design

Expose a Conversations-owned retrieval API, for example:

```csharp
Task<ConversationMemoryContext> RetrieveMemoryAsync(
    TenantId tenantId,
    ConversationId conversationId,
    string query,
    int maxResults,
    int tokenBudget,
    CancellationToken cancellationToken);
```

Inside that API:

1. Validate tenant access through Conversations' tenant projection.
2. Validate resource access to the conversation/project/folder.
3. Determine allowed case scope, usually `caseId = conversationId`.
4. Call Memories `HybridSearchAsync`:

```csharp
HybridSearchResult result = await memoriesClient.HybridSearchAsync(
    new HybridSearchRequest(
        TenantId: tenantId,
        Query: query,
        CaseId: conversationId,
        MaxResults: maxResults,
        Explain: false,
        TokenBudget: tokenBudget),
    cancellationToken);
```

5. Re-check each result against Conversations indexing projection and current policy.
6. Drop stale, deleted, redacted, unauthorized, or policy-mismatched memory units.
7. Return attributed snippets with conversation/message citations, not raw Memories objects.

The retrieval response should include:

- snippet/content
- conversation id
- message id
- speaker Party ID
- event position/timestamp
- source URI
- redaction state
- confidence/source score if allowed
- reason omitted count when token budget truncates results

---

## Lifecycle And Consistency

### Create memory

Trigger on eligible `MessageAppended` events after the event is durable and the message projection is available. Do not synchronously block command acceptance on Memories indexing.

### Update memory

Immutable conversation events should not be edited in place. Instead:

- New correction/annotation event -> new memory unit or annotation.
- Redaction event -> delete/re-index the affected memory unit with redacted text, or tombstone it and filter it from retrieval.
- Summary refresh -> new summary memory unit version with old one deleted or marked superseded.

### Delete memory

Use Memories' memory-unit delete endpoint when a message must no longer appear in RAG:

```http
DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}
```

Conversations must keep an indexing projection mapping:

```text
(tenantId, conversationId, messageId, policyVersion)
  -> memoryUnitId, sourceUri, workflowInstanceId, status, indexedAt, contentHash
```

Without this projection, Conversations cannot safely delete, re-index, prove freshness, or reconcile drift.

### Rebuild/backfill

For backfill, replay Conversations events into the indexing worker with stable `sourceUri` values. Memories deduplicates by source URI for REST ingestion and by CloudEvent ID for event ingestion. If the goal is a full rebuild, delete the case or memory units first, then replay.

---

## Security And Governance

Hard rules:

- Do not index messages that are redacted, retention-expired, under legal hold restrictions that disallow derived search, or classified as not indexable.
- Do not put raw secrets, provider prompts, bearer tokens, raw tenant names, Party display names, or personal data snapshots in metadata.
- Do not let agents query Memories directly for conversation recall.
- Do not use Memories as proof of what happened; use Conversations/EventStore for audit proof.
- Do not return a Memories result until Conversations has revalidated current authorization.
- Do not rely on Memories `tenantId` alone for resource security; tenant is necessary but not sufficient.

Risk controls:

| Risk | Control |
| --- | --- |
| Cross-tenant leakage | Conversations authorizes before retrieval; Memories query scoped by tenant and case; negative conformance tests. |
| Stale redacted content | Redaction event deletes/reindexes memory units; retrieval checks indexing projection policy version. |
| Deleted message still searchable | Memory-unit delete or tombstone filter; reconciliation job. |
| Prompt context exceeds budget | Use Memories `tokenBudget` plus Conversations-side truncation. |
| Dapr duplicate delivery | Stable source URI/idempotency; idempotent handlers. |
| Raw event JSON poor semantic quality | Prefer curated message text for prompt recall; use event ingestion for event observability and graph. |
| Unauthorized direct Memories access | Keep Memories service private; expose only Conversations-owned RAG API/tool. |

---

## Performance And Operations

Make indexing asynchronous. The user-visible append-message flow should complete when Conversations has persisted and projected the authoritative record, not when Memories finishes embedding.

Operational states to project in Conversations:

- `not_indexed`
- `not_eligible`
- `pending`
- `scheduled`
- `indexing`
- `indexed`
- `failed_retryable`
- `failed_terminal`
- `stale_policy`
- `deleted`
- `reconciliation_required`

Metrics to add:

- conversation memory indexing lag
- pending/failed indexing count by tenant
- skipped count by reason
- re-index count by redaction/policy change
- retrieval latency
- retrieval result count after eligibility filtering
- Memories degraded axis count
- stale result suppression count

Aspire/Dapr local wiring can follow the Memories AppHost pattern and official Aspire Dapr guidance: attach Dapr sidecars with `WithDaprSidecar`, and use Dapr app IDs or configured HTTP endpoints for service-to-service calls.

---

## Implementation Roadmap

### Phase 0: ADRs and scope

- Confirm this is vNext or explicitly promote it to v1.1+.
- ADR: Conversations source-of-truth vs Memories derived index.
- ADR: case model (`caseId = conversationId` first).
- ADR: redaction and delete/re-index behavior.
- ADR: metadata classification.
- ADR: EventStore CloudEvent source/type/id conventions.

### Phase 1: Adapter and projection

- Add `IConversationMemoryIndexer` in a worker/server edge, not in contracts or aggregate logic.
- Add Memories REST adapter in a Conversations worker project.
- Add indexing-status projection.
- Unit test source URI/idempotency, metadata, redaction skip, and error mapping.

### Phase 2: Message indexing

- Trigger from `MessageAppended`.
- Validate policy and redaction state.
- Create or ensure a Memories case for the conversation.
- Ingest redaction-aware text with stable source URI.
- Poll or observe workflow status.
- Store mapping to `memoryUnitId`/workflow id.

### Phase 3: Retrieval facade

- Add Conversations-owned RAG retrieval API/client method.
- Authorize tenant and conversation before search.
- Call Memories hybrid search scoped by case.
- Revalidate current eligibility for each result.
- Return conversation/message citations and token-budgeted snippets.

### Phase 4: Lifecycle handling

- Delete/tombstone memory unit on redaction or retention removal.
- Re-index on policy version changes.
- Add backfill/reconciliation job.
- Add negative security tests for stale/redacted/deleted recall.

### Phase 5: Event and graph enrichment

- Publish selected Conversations events to Memories EventStore integration.
- Add causation/correlation IDs to support graph traversal.
- Add generated summaries, decisions, and action items only after governed summary/extraction ADRs.

---

## Recommended First Slice

Build the smallest useful capability:

1. `MessageAppended` eligible message -> one memory unit.
2. `caseId = conversationId`.
3. `sourceUri = hexalith-conversations://{tenantId}/conversations/{conversationId}/messages/{messageId}`.
4. Metadata only uses stable IDs and policy/version fields.
5. Retrieval API accepts `(conversationId, query, maxResults, tokenBudget)`.
6. Retrieval always revalidates authorization and redaction before returning snippets.
7. Delete/re-index is implemented before the feature is available to real tenants.

This proves RAG recall without turning Memories into the conversation store.

---

## Key Decisions To Make Before Coding

1. Is Memories integration vNext, v1.1, or a v1 scope change?
2. Should the first retrieval scope be one case per conversation only?
3. Should `MemoriesClient.IngestAsync` grow a generic source-type overload so Conversations can send `SourceType.Discussion` directly?
4. Is message text itself indexable, or only summaries/decisions/actions?
5. What redaction action is required: delete, re-index redacted content, or tombstone/filter?
6. Will Conversations publish its events to Memories EventStore integration, or use only a worker REST adapter first?
7. What metadata is allowed for Party, Project, Folder, and attachment references?
8. What conformance tests block release?

---

## Source Verification

Local:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/product-brief-Hexalith.Conversations.md`
- `_bmad-output/project-context.md`
- `Hexalith.Memories/README.md`
- `Hexalith.Memories/docs/dev/quickstart.md`
- `Hexalith.Memories/docs/dev/eventstore-integration.md`
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/SearchRequest.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/HybridSearchRequest.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/SearchResult.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Server/Program.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `Hexalith.Memories/src/Hexalith.Memories.EventStore/EventIngestionService.cs`
- `Hexalith.Memories/src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`
- `Hexalith.Memories/src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs`

Public:

- Azure AI Search hybrid search: https://learn.microsoft.com/en-us/azure/search/hybrid-search-how-to-query
- Dapr pub/sub overview: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/
- Aspire Dapr integration: https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr

---

## Conclusion

Use `Hexalith.Memories` as a derived semantic and hybrid-retrieval index for conversation memory, not as the system of record. The safest design is a Conversations-owned indexing worker and retrieval facade: Conversations decides what may be indexed and what may be returned; Memories handles embeddings, hybrid search, graph traversal, and consistency mechanics.

The first implementation should be narrow: message-level memory units scoped to a single conversation case, asynchronous indexing, stable source URIs, explicit indexing status, and fail-closed retrieval. EventStore/CloudEvents ingestion and summary/decision/action memories can follow once redaction, metadata, and release-scope ADRs are locked.
