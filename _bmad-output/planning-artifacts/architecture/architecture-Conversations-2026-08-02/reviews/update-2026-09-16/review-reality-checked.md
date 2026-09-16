# Reality And Currentness Review — V15 Update Candidate

- **Review date:** 2026-09-16
- **Target:** `/tmp/hexalith-architecture-v15-lint/ARCHITECTURE-SPINE.md`
- **Effective overlay:** `conversations-architecture-2026-09-16-v15`
- **Mode:** reviewer gate; the spine was not edited
- **Lens:** verify every committed decision against the checked-out repository, pinned planning authority, installed BMad routes, and current primary technology sources; reject training-data assumptions and contracts that do not exist or cannot fit the present seams

## Final Re-Review After AD-3 Bootstrap And AD-8 Supersession

- **Reviewed target SHA-256:** `fb14a118fe44df6a54e8d1ff60de6a3b90a7eac5fc1fbba7f45b066475b91d35`
- **Scope:** final patched V15 only; report remaining critical/high reality defects

**PASS — no critical or high reality/currentness defects remain.** The final overlay resolves all three prior high findings and the prior medium identity ambiguity. AD-3 now treats the absent external trust root as `BLOCKED_BOOTSTRAP_AUTHORITY` instead of pretending the repository can authorize its own replacement, and AD-8 explicitly supersedes the incompatible inherited V14 byte-equality clause. Its runtime requirements remain honestly prospective: they bind Stories 16.1 and 16.2, fit existing platform primitives or explicitly require a versioned EventStore addition, and do not claim that current Tenants or Conversations handlers already implement them. Every implementation hold remains `ACTIVE`.

| Final addition | Result | Reality check |
| --- | --- | --- |
| AD-3 external bootstrap | **PASS** | The referenced commit `239758d396d28372687b73f5dc128405892cb520` exists, and its verifier blob at `_bmad/scripts/publish_story_7_1_successor_authorities.py` hashes exactly to the declared `2ddb6da7bfec48d33f555638b6e3175f7623cbc153c4ad2d3ffbe9494e5df126`. The current V21 verifier cannot authorize V22/resolver paths, and V15 now says so. The organization ruleset, external validator, required no-bypass check, pinned policy, and nonce consumer are not asserted to exist: their absence keeps AR-15 blocked and all holds active (`ARCHITECTURE-SPINE.md:2827-2885`). |
| AD-8 explicit supersession | **PASS** | The overlay now expressly replaces V14's byte-identical complete projection/timestamp requirement with byte identity only for canonical deterministic v2 replay state, `ReplayAnchorAt`, and `ReplayStateHash`; public observation-time freshness remains semantically governed by `ProjectionFreshnessV1` and is excluded from replay identity (`ARCHITECTURE-SPINE.md:3097-3109`). This removes the inherited contradiction rather than relying on implicit last-wins interpretation. |

| Prior finding | Final status | Reality check |
| --- | --- | --- |
| REAL15-1 — public `ProjectionGeneratedAt` silently redefined | **RESOLVED** | AD-8 separates internal deterministic `ReplayAnchorAt` from public `ProjectionFreshnessV1.ProjectionGeneratedAt`, preserves the latter as the generation-completion instant, preserves lag/staleness meaning, and excludes operational freshness from replay hashes (`ARCHITECTURE-SPINE.md:3037-3050`). This fits the current public contract while deferring the persistence/mapping refactor to Story 16.2. |
| REAL15-2 — one-value tenant gap checkpoint unsafe under unordered delivery | **RESOLVED** | AD-6 now requires immutable `expected`, monotonic `recoveryThrough`, per-sequence durable evidence, acknowledgement only after durable outcome, fenced leases, horizon-stability proof before `Healthy`, and multi-delivery/concurrent-extension conformance (`ARCHITECTURE-SPINE.md:2933-2956`). This closes the omission path under Tenants' documented at-least-once/unordered model and can use EventStore raw-envelope and CAS primitives. |
| REAL15-3 — source-generated serializer requirement did not fit EventStore batches | **RESOLVED** | AD-8 makes the existing fixed EventStore coordinated-batch canonicalizer authoritative and forbids injecting Conversations API options (`ARCHITECTURE-SPINE.md:3052-3065`). `ReadModelBatchOperation.Write` already produces immutable canonical bytes and publicly exposes `CanonicalValue`, so the specified hash input is available without bypassing the seam. Any source-generated persistence metadata is correctly gated on a versioned EventStore API and golden vectors. |
| REAL15-4 — replay identity ambiguous; position-only `MessageId` discarded | **RESOLVED** | AD-8 binds identities to current fields, makes sequence a contiguity check only, and requires the future decoder/dedup binding to retain identity, payload fingerprint, sequence, and selected time (`ARCHITECTURE-SPINE.md:3021-3035`). It accurately identifies a Story 16.2 change rather than asserting current compliance. |

Current-seam check: Tenants' shipped unconditional store/process-local lock remain current reality; EventStore's envelope, ETag/CAS, three-attempt policy, canonical batches, and exposed canonical operation bytes exist; Conversations' index v2, replay anchor, generation fencing, durable catch-up/quarantine, and retained position-only identity remain held Story 16.2 targets. V21 remains historical with effective state failed/`ACTIVE`. The AR-15 external trust root is explicitly absent/blocked, not represented as current repository capability. No patched clause relies on stale training-data versions or a nonexistent current package contract.

## Initial Verdict (Superseded By Final Re-Review)

**SUPERSEDED:** the initial candidate failed with 3 high and 1 medium defects. The sections below preserve the original evidence and correction record; the patched overlay's controlling result is the PASS above.

## Severity Register

| ID | Severity | Reality mismatch |
| --- | --- | --- |
| REAL15-1 | HIGH | AD-8 redefines the existing public meaning of `ProjectionGeneratedAt` and consequently changes `LagDuration`/staleness behavior without a versioned contract decision |
| REAL15-2 | HIGH | AD-6's one-value gap checkpoint cannot safely represent later out-of-order deliveries that the current DAPR/EventStore consumer model permits |
| REAL15-3 | HIGH | AD-8 requires “repository source-generated contract options,” but current coordinated EventStore batches intentionally use a fixed, non-injectable Web serializer before canonicalization |
| REAL15-4 | MEDIUM | AD-8 does not bind “same event identity” to existing fields, and the position-only decoder currently discards the only suitable persisted message identity |

## REAL15-1 — HIGH — `ProjectionGeneratedAt` is already a public wall-clock concept, not a replay anchor

**Committed V15 rule.** AD-8 says the persisted `ProjectionGeneratedAt` is the maximum effective event or lifecycle time and that processing time never enters replayed state (`ARCHITECTURE-SPINE.md:2944-2955`).

**Current authority.** `ProjectionFreshnessV1` is a public v1 contract. It documents `ProjectionGeneratedAt` as “the UTC time when this projection generation was produced” and `LagDuration` as the observed interval from the last accepted event to projection generation (`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs:11-32`, `:51-66`). The current materializer consumes a separately supplied generation time, computes `lag = generatedAt - lastApplied`, and uses that value to derive stale/current behavior (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionMaterializer.cs:115-148`). Live and async handlers currently supply `_timeProvider.GetUtcNow()` (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionHandler.cs:108`; `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs:144`). Several query, governance, rendering, comparison, and dispatch paths consume the field as the generation/evaluation instant.

**Why it cannot be accepted as written.** For a normal projection whose last event is the maximum contributing effective time, V15 makes `ProjectionGeneratedAt == LastAppliedEventTimestamp` and therefore makes the persisted lag zero. That is not merely an implementation choice for deterministic replay: it changes the documented public meaning and can change `LagDuration`, `IsStale`, trust posture, rendering evaluation, and comparison behavior while V15 simultaneously says no public contract/value-set change is in scope. “Processing time is limited to response-time freshness calculation” does not name where that calculation is represented or how the existing persisted public freshness object is mapped.

**Required correction.** Keep an internal deterministic replay anchor under a distinct name, or explicitly version and migrate the public freshness semantics. Name the response-time mapping that produces `ProjectionGeneratedAt`, `LagDuration`, and `IsStale`; do not silently overload `ProjectionFreshnessV1.ProjectionGeneratedAt` with event time.

## REAL15-2 — HIGH — The tenant gap checkpoint is too small for the actual delivery model

**Committed V15 rule.** AD-6 records a future event as `Gapped(expected, observed, fingerprint)` without advancing, then replays `expected..observed` and returns to `Healthy` after contiguous recovery (`ARCHITECTURE-SPINE.md:2887-2900`).

**Current authority.** Tenants explicitly documents DAPR pub/sub as at-least-once and unordered and requires message-id deduplication (`references/Hexalith.Tenants/_bmad-output/project-context.md:73`; `references/Hexalith.Tenants/deploy/dapr/README.md:69`). The shipped handler only has a process-local `SemaphoreSlim`, ignores only strictly older sequence numbers, and unconditionally saves the updated state (`references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs:125-139`). `ITenantProjectionStore` currently exposes only `GetAsync` and unconditional `SaveAsync` (`references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs:6-21`), so AD-6 is correctly prospective and correctly assigns the new CAS substrate to EventStore.

**Why it cannot be implemented safely as written.** While state is `Gapped(expected=n+1, observed=n+2, ...)`, deliveries for `n+4` and then `n+3` are legal. V15 does not say whether they are acknowledged, rejected for redelivery, retained, or CAS-merged; whether `observed` is the first value, last arrival, or monotonic maximum; or what fingerprint represents after more than one future delivery. A provider can acknowledge `n+4`, later recover only through `n+2`, and return `Healthy` while omitting an authorization-relevant event. The named conformance schedules do not force this case.

**Required correction.** Define a monotonic recovery high-watermark plus durable pending identities, or require later future deliveries to remain unacknowledged/redeliverable. Specify every CAS transition while `Gapped`, lease handoff, and the exact proof for returning `Healthy`. Add at least a three-event interleaved/reverse delivery schedule to both provider and consumer conformance suites.

## REAL15-3 — HIGH — The named serializer contract does not exist at the EventStore batch seam

**Committed V15 rule.** AD-8 requires persisted JSON to use “the repository's source-generated contract options,” UTC normalization, ordinal ordering, and byte-identical output/hashes (`ARCHITECTURE-SPINE.md:2956-2959`).

**Current authority.** Conversations does have `ConversationsJsonContext`, but it covers public contract types; the new internal v2 index persistence shape is not currently in that context (`src/Hexalith.Conversations.Contracts/Serialization/ConversationsJsonContext.cs:28-220`). More importantly, coordinated EventStore read-model batches deliberately serialize with a private fixed `new JsonSerializerOptions(JsonSerializerDefaults.Web)`, not consumer-supplied options, and then recursively sort object properties ordinally (`references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelBatchCanonicalJson.cs:6-32`, `:35-71`). Its documentation says custom serializer options must not use that batch seam because the bytes are versioned fingerprint material.

**Why it cannot be implemented as written.** There is no current API by which `ConversationProjectionReadModelWriter` can inject a source-generated `JsonTypeInfo` or options into the EventStore coordinated-batch serializer. Adding that behavior locally would either bypass the platform batch invariant or require a new EventStore contract. “Repository” is also ambiguous between Conversations and EventStore, so independent builders can choose different serializers while both claim compliance.

**Required correction.** Bind the existing EventStore canonical-batch algorithm and its exact hash input, or explicitly require a new versioned EventStore canonical serialization API that accepts named type metadata without weakening fingerprint stability. Name the context/type info for every persisted v2 type and distinguish canonical pre-store bytes from provider round-trip verification.

## REAL15-4 — MEDIUM — “Same event identity” has two live candidates and one is discarded

**Committed V15 rule.** AD-8 requires identity/time validation before duplicate suppression and treats “the same event identity with a different effective time” as corruption (`ARCHITECTURE-SPINE.md:2944-2954`).

**Current authority.** Public Conversation events deduplicate by `ConversationEventMetadata.EventId` (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionAccumulator.cs:284-299`). EventStore projection records separately expose persisted `ProjectionEventDto.MessageId` (`references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs:39-81`). The position-only decoder currently creates `ConversationProjectionPositionOnlyEvent(Timestamp)` and drops `MessageId` (`src/Hexalith.Conversations.Server/Projections/ConversationProjectionEventDecoder.cs:193-207`; `src/Hexalith.Conversations.Server/Projections/ConversationProjectionPositionOnlyEvent.cs:8-10`).

**Why it matters.** A builder can interpret identity as public `EventId`, envelope `MessageId`, or aggregate sequence. For position-only events, the current internal record preserves only time, so the required identity/time contradiction check cannot be implemented after decoding. The current processor's message-level idempotency does not replace deterministic replay validation inside a projection history.

**Required correction.** Bind identity explicitly—normally public `EventId` for Conversation contract events and persisted EventStore `MessageId` for position-only events—and require the decoder/accumulator record to retain it before duplicate suppression. Define whether sequence plus stream identity is merely a consistency check or an alternate identity.

## Decision-By-Decision Reality Check

| Decision | Result | Repository/currentness evidence |
| --- | --- | --- |
| AD-1 EventStore write authority | **PASS** | `ConversationAggregate` is an `EventStoreAggregate<ConversationState>`; the server uses the two-line `AddEventStoreDomainService`/`UseEventStoreDomainService` host; no Conversations-owned authoritative event store is introduced. |
| AD-2 capability ownership | **PASS** | State/DAPR/CAS/rebuild capabilities are present in EventStore; Tenants owns tenant semantics; Conversations owns its aggregate, projection, and public contract mappings. The ownership split matches `hexalith-state-instructions.md`. |
| AD-3 marker and V21 recovery | **PASS, with declared prospective work** | The V9 bundle digest is exactly `8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3`; the V21 sidecar digest is exactly `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`. V21 still carries raw `LIFTED` fields and was published without the pointer. The current workflow/checker is V21-specific, so the generic resolver in AR-15 is correctly described as not yet present. |
| AD-4 Story 7.1 state machine | **PASS as future authority** | The state machine is not presented as implemented; current effective state remains `ACTIVE`. No existing runtime contract is contradicted by adding the planning-state record. |
| AD-5 operational envelope | **PASS / hold correctly active** | `_bmad-output/planning-artifacts/production-operational-envelope-v1.md` does not exist. V15 correctly makes that absence a blocker before Story 16.1 or a relevant hold lift. |
| AD-6 tenant projection mutation | **FAIL** | EventStore has `IReadModelStore` ETag/CAS and `ReadModelWritePolicy.DefaultMaxAttempts == 3`, and the raw envelope contains the required message/type/payload data. The target is feasible, but the gap protocol is not total under documented delivery reality (REAL15-2). |
| AD-7 Conversation lifecycle/index v2 | **PASS as an additive design target** | The existing key grammar is `projection:conversations-index:<tenant-segment>` and the current index lacks lifecycle/watermarks. EventStore has coordinated batch/CAS and shared-rebuild seams, so the v2 target can fit without a parallel store. It does require the stated writer/coordinator refactor and must not be described as current implementation. |
| AD-8 replay-visible time | **FAIL** | `ConversationEventMetadata.OccurredAt` and `ProjectionEventDto.Timestamp` exist, but the public freshness meaning, serializer seam, and event identity are not compatible or fully named (REAL15-1, REAL15-3, REAL15-4). |
| AD-9 BMad routes and technology seed | **PASS** | Installed manifest is BMad `6.12.0`; both agent trees contain `bmad-build` and `bmad-build-auto`, and neither contains `bmad-dev-auto` nor `bmad-quick-dev`. `global.json` pins SDK `10.0.401`; central package authority pins Aspire `13.5.3`, Dapr `1.18.7`, and Fluent UI `5.0.0-rc.5-26219.1`. V15 correctly makes exact versions code-owned and defers the Fluent v5 prerelease/stable policy. |
| AD-10 public-boundary scope | **PASS as prospective enforcement** | Current public projection/trust/reason types exist. `_bmad-output/planning-artifacts/architecture-rule-enforcement-v1.json` does not yet exist, and V15 explicitly defers it; no nonexistent ledger is represented as implemented. |

## Authority And Tooling Reproduction

At repository `fd3dd58c3c10b512c79cc425f94d57c4d3400b20`, the exact V15 command was run:

```text
uv run --frozen --no-sync python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v21 --candidate HEAD --effective-hold --check
```

It exited `1` with `result: FAIL`, blocker `V21_DESCENDANT_GITLINK_DRIFT`, and effective hold `ACTIVE`, enumerating descendant EventStore, Folders, and FrontComposer gitlink drift. This independently confirms V15's evaluated-state row; it is not a training-data assertion.

The protected workflow still hard-codes the V21 authority path, so V15 is also correct that a generic marker-driven resolver is future work rather than current behavior (`.github/workflows/planning-authority-preflight.yml`). The raw V21 sidecar's historical `LIFTED` values must not be read as the present effective state.

## Technology Currentness

The binding exact versions come from tracked repository files, which is the right authority. A primary-source spot check found the pinned Dapr Client `1.18.7` package and Aspire Hosting `13.5.3` package available, while Fluent UI's current v5 line is still prerelease and the stable package line remains v4. This supports V15's decision to defer the v5 prerelease/stable policy rather than guess from model memory: [Dapr.Client 1.18.7](https://www.nuget.org/packages/Dapr.Client/1.18.7), [Aspire.Hosting](https://www.nuget.org/packages/Aspire.Hosting/), [Microsoft Fluent UI ASP.NET Core Components](https://www.nuget.org/packages/microsoft.fluentui.aspnetcore.components).

No binding V15 decision relies on a stale training-data version. The version problem is semantic/API fit in AD-8, not package recency.

## Initial Acceptance Condition (Satisfied By Patched Overlay)

All four findings are now closed in the patched architecture text, and the overlay remains explicit that the resolver, operational envelope, durable tenant projection, index v2, and enforcement ledger are target artifacts rather than current implementation. No product code change was required to clear this architecture-review gate.
