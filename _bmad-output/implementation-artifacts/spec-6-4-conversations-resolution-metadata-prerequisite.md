---
title: 'Story 6.4 Conversations resolution metadata prerequisite'
type: 'feature'
created: '2026-10-20'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1f3891274eba5cae0754987ee79b4ffc87944401'
dependency_status: 'EventStore verified-origin source contract unavailable in current checkout'
---

<frozen-after-approval reason="User-requested implementation scope; dependency investigation remains open">

## Intent

Deliver an isolated Conversations metadata projection and supported query for Projects resolution. Return only authorized Conversation identity, Project membership, lifecycle, a bounded label for internal equality scoring, native freshness/revisions and the owner verification instant. Follow the selected 2026-10-20 decisions and section 3 of the enclosing Projects contract annex.

## Boundaries & Constraints

**Always:** Use EventStore persistence and the completed SDK verified-origin contract. Authorize the exact resource and internal scoring-label purpose using the existing tenant Read requirement before lookup and before disclosure. Preserve native revision scopes and the existing projection generation/rebuild protocol. Require current evidence and stable authority throughout the read. Bound labels to 160 characters; never truncate or manufacture a matching value.

**Never:** Read detail, transcript, hydration or tenant-list data from this query. Do not replay, backfill, repair, write inspection audits or persist caller grants during queries. Do not change legacy routes, parent Story 6.4 tracking, package versions or submodule pins. No commits, staging, checkout changes or nested-submodule updates. Preserve existing work.

## I/O & Edge-Case Matrix

| Scenario | Given / When | Then |
| --- | --- | --- |
| Authorized current read | Verified origin, exact target, allowed scoring purpose, current stable evidence | Return the approved metadata subset and source-backed evidence |
| Protected denial | Missing authority, wrong target/Tenant, forbidden purpose or inaccessible resource | Indistinguishable `QueryResult.Failure("safe-denial")`; no protected fields |
| Non-current evidence | Stale, absent, rebuilding, malformed or unconfirmed required evidence | No membership or label; honest unavailable/non-current result |
| Authority changes | Native permission evidence or authorization changes during lookup | Withhold metadata; no stale permission grant |
| Partial generation | Missing metadata/commit proof, pending dispatch or mixed generations | Unavailable; no read-time repair |
| Recovery | Rebuild, redelivery, restart, concurrent old/new dispatches or expired completed ledger | Converge on the deterministic winning generation; retain lasting consistency proof |
| Label boundary | Label absent, 160 characters or above bound | Preserve absence/valid exact label; over-bound evidence cannot support complete scoring |

</frozen-after-approval>

## Open Questions

- **Required external dependency:** Which completed EventStore verified-origin source contract should this consumer compile against? The current root-declared EventStore checkout is `1eaabca4f8d3597a3a1f9fadf341be5cb9a3a8c0`. Its SDK has no separate originating context, and its bound-owner controller drops original delegation/scopes/audience. Its existing prerequisite spec is `in-progress`. The user does not know another completed location. Resume after the actual contract and verification evidence become available; do not invent consumer fields or silently weaken this requirement.

## Code Map

Paths below are Conversations-relative unless qualified.

- `src/Hexalith.Conversations.Contracts/Queries/`: add the annex's three same-named query/result/metadata records; preserve existing DTOs.
- `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`: reuse native cursor, position, event/generation timestamps and `AllowsTrustBearingDecision()`.
- `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelKeys.cs`: reuse base64url segment encoding for isolated Tenant/Conversation keys.
- Same directory: `ConversationProjectionReadModelWriter.cs`, `ConversationAsyncProjectionHandler.cs`: reuse pending markers, deterministic position/dispatch ordering, durable completion verification and terminal recovery. Existing completion checks cover only detail/index; extend them for metadata.
- Same directory: `IConversationProjectionReadStore.cs`, `ConversationProjectionReadStore.cs`: add a narrow read; existing Read/List/ValidatePage load detail or tenant summaries and are forbidden here.
- `src/Hexalith.Conversations.Server/TenantAccess/`: reuse access service and projection signal. Successful Commons access evaluations omit revision/watermark, and the default signal can report Healthy without them; neither establishes current permission evidence.
- `src/Hexalith.Conversations.Server/Queries/ConversationQueryServiceCollectionExtensions.cs`: additive registration; preserve legacy discriminators and handlers.

## Tasks & Acceptance

**Execution:**

- [ ] Resolve the EventStore dependency above and record its exact public field mappings and immutable source identity before implementing the consumer handler.
- [ ] `src/Hexalith.Conversations.Contracts/Queries/{GetConversationResolutionMetadataQuery,ConversationResolutionMetadataV1,ConversationResolutionMetadataResultV1}.cs`: add annex contracts with `{}` payload and exact envelope target.
- [ ] `src/Hexalith.Conversations.Server/Projections/ConversationResolutionMetadataReadModel.cs` and existing keys/writer/async handler: persist only the subset and native generation evidence; add a content-free lasting per-resource completion proof. Integrate pending, completion, retry and rebuild paths before declaring dispatch durable.
- [ ] Existing projection interface/read store: implement `ReadResolutionMetadataAsync` using only isolated metadata and content-free consistency evidence. Reject mixed/pending generations without touching legacy detail/index keys.
- [ ] `src/Hexalith.Conversations.Server/Queries/ConversationResolutionMetadataAuthorization.cs`: explicitly classify and authorize bounded internal equality-scoring labels, exact target and verified origin. Check native tenant health/revisions around authorization; missing revisions remain unavailable. Do not change legacy access semantics.
- [ ] Same directory: `GetConversationResolutionMetadataDomainQueryHandler.cs` and query registration: implement the exact annex QueryType in domain `conversations`, strict envelope validation, two-pass authorization and stable resource/permission evidence. Source `AuthorizedAt` from the final owner check; invent no lease.
- [ ] `tests/Hexalith.Conversations.Contracts.Tests/ConversationResolutionMetadataContractTest.cs`: verify minimal serialization and existing-contract compatibility.
- [ ] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationResolutionMetadataQueryTest.cs`: cover the matrix through SDK dispatch. Use throwing doubles for detail/transcript/hydration/list access and all query mutations.
- [ ] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationResolutionMetadataPersistenceTest.cs`: assert persisted end-state, each interrupted-write boundary, old/new interleaving, mixed generations, ledger expiry, redelivery and rebuild equivalence.

**Acceptance Criteria:**

- Given the authenticated supported path, when metadata is disclosed, then both actor/resource authorization and the scoring purpose are explicit and current with stable native evidence.
- Given any query outcome, when forbidden-read and mutation doubles are armed, then none is invoked and no content or request grant enters the result or persisted metadata.
- Given partial writes, when restart/rebuild/redelivery runs, then metadata converges with the winning generation or remains unavailable without query repair.

## Implementation Notes

2026-10-20: Read-only investigation and this draft only. No implementation or test execution. Missing SDK contract prevents development readiness; no consumer workaround is authorized. Parent decisions remain selected and parent delivery remains blocked independently. Planning introduces no irreversible action.

## Spec Change Log

## Review Triage Log

## Verification

After resolving the dependency, build individual Contracts and Server test projects in Debug with existing source references. Run their built xUnit v3 assemblies using single-dash class/method filters for focused checks, then the affected suites. Exercise production async projection persistence/rebuild and SDK-verified query dispatch; record exact commands, counts and blockers. Preserve existing build gates and report any broader environment blocker separately. Finish with `git diff --check`. No source test establishes production enrollment or parent Story 6.4 acceptance.
