# Adversarial review of the Conversations pilot architecture

**21 November 2026 · Validation · Conversations 08953f5eed40**

**Verdict — Changes needed.** Three high and two medium findings affect how independent operators, route owners and SDK consumers apply the November decisions. The examples below are static comparisons of the architecture, current implementation, test assertions and operator documentation. No runtime incident or new test result is claimed.

The review target is `ARCHITECTURE-SPINE.md`, the workspace link to `_bmad-output/planning-artifacts/architecture.md`. Spine references below use its line numbers. Other paths are relative to the repository root.

## High

### ADV-1 · A rollback can serve a false Current result

One operator drains traffic and fences old writers until a rebuilt tenant index is safe. Another restores the previous binaries, resumes traffic, then rebuilds. Both can demonstrate that a rollback and replay completed. During the second rollout, a missing index can already have produced an empty Current list for a populated tenant. An old writer can also replace the v2 index with v1 before the new writer resumes. The rollout order matters even when every EventStore event survives.

**Evidence.** The November section requires truthful missing-index reads and rollback evidence. ADR 0005 describes redeployment followed by replay. The operating handoff explicitly records both the earlier reader's false Current result and the earlier writer's rewrite. See spine lines 3359–3361, `docs/adrs/0005-conversation-derived-key-lifecycle.md:155–158`, and `module/platform-handoff.md:597–602`.

**Disposition — Discuss with Platform and Projection.** Keep reads unavailable and prevent incompatible writers from touching the recovering index when an older binary cannot preserve the invariant. Permit another rollout sequence when its evidence establishes the same guarantee. Test a populated tenant with a missing index throughout rollback and roll-forward, including the overlap period if both binaries can serve or write.

### ADV-2 · The creation event proves novelty at a different time from index publication

A handler can durably verify that the genuine sequence-one tenant event has never been processed by its initializer. A second handler also verifies the absence of conversation history through the point at which it publishes Ready. The two diverge if Tenants recovered the creation event independently, the caller became authorized and created a conversation, and delivery to Conversations' tenant initializer was delayed. A first delivery at that initializer is still possible after domain history exists. An absent index and even a durable first-delivery receipt can coexist with a populated tenant.

**Evidence.** The November rule requires durable proof of a genuinely new tenant. It specifies a coverage cutoff for recovery and leaves the creation shortcut's observation point implicit. The handoff documents out-of-order Tenants recovery granting authorization before Conversations receives the creation event. The writer takes only the tenant ID and uses create-only persistence. Its CAS protects against an already-written index. See spine lines 2994–3007 and 3361, `module/platform-handoff.md:537–550`, and `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadModelWriter.cs:312–333`. Story 16.4 already calls for a separate redelivery-after-restart test at `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:4524–4526`.

**Disposition — Tighten AD-7.** Bind the creation shortcut to absence of prior applicable conversation history through a named cutoff and preservation of later concurrent deliveries, or a causal ordering guarantee that proves both. Without either guarantee, use the degraded recovery path. Add delayed *first* delivery after tenant-access recovery and a committed conversation to the qualification cases. The history source remains the assigned owners' decision.

### ADV-3 · HTTP 503 can carry either a read result or an error

One SDK dispatches a non-success HTTP response directly to the typed-error decoder and applies the fallback when that shape is absent. Another SDK recognizes the operation's valid read result before applying a fallback. Both can read the body once and preserve every genuine typed service error. For HTTP 503 with an ordinary Unavailable detail or list body, the first SDK reports a generic error while the second returns a value carrying Unavailable freshness and keeps the HTTP status. The published SDK's callers already depend on the latter distinction. Conversely, HTTP 503 with a genuine `participant_validation_unavailable` error must retain its code, category and correlation.

**Evidence.** AD-11 covers typed failures and empty or malformed bodies. The HTTP implementation returns the operation's read DTO on 503. Existing SDK assertions require `IsSuccess` and a non-current value for a well-formed degraded read. Existing command fallback tests separately differentiate HTTP 400, 401/403, 404, 409 and 500. See spine lines 1210–1213 and 3351–3355, `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs:279–304`, `src/Hexalith.Conversations.Client/ConversationClient.cs:239–261`, `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs:203–233` and `:351–377`, and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-20.md:47`.

**Disposition — Clarify AD-11 from existing behavior.** Specify three cases on a single body read. A valid operation-specific read DTO returns a value with wire freshness and status. A valid error envelope returns the service error. An absent or malformed failure body uses the established status mapping and request correlation. Unknown transport outcome retains the same-request retry rule for a mutation. Exercise both 503 shapes on a forward-only stream.

## Medium

### ADV-4 · The reference for “the same hidden response” depends on the operation

A gateway can give every conflicting scope assertion a common HTTP 404 envelope. A second gateway can reproduce each route's ordinary hidden response. Both reject the identical bad headers before store access, yet clients see different status codes and DTOs. The existing contract returns a hidden detail DTO with HTTP 404 and a hidden list DTO with HTTP 200. A new mismatch-only 404 for list would also distinguish that denial from its existing hidden list result.

**Evidence.** AD-11 says “the same hidden response” without naming the comparison. The current route and API assertions establish the different baselines. Story 9.4 also covers the other resolver-sharing read routes. See spine lines 3353–3355, `src/Hexalith.Conversations.Server/Api/ConversationReadApi.cs:49–54`, `:346–347` and `:384–385`, `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs:94–105` and `:526–540`, and `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:4532–4533`.

**Disposition — Clarify AD-11.** Compare a conflicting or repeated header to the same operation's established hidden status and body. Bind the check to each route using the common resolver and require zero query-store calls. The existing per-route wire contracts supply the expected outcomes.

### ADV-5 · A gap during an explicit rebuild has two public reason codes

Consider a v2 Rebuilding index whose summary position is 4 and whose watermark is 5. A reader can apply the AD-7 row for a rebuilding record and emit `rebuilding`. A second can apply the row for a source-position gap and emit `gap_detected`. Both withhold Current. An operator receives different lag classes for the same stored record, including `WithinThreshold` versus `CriticalLag`.

**Evidence.** The inherited table lists both conditions. The November clarification preserves the Rebuilding state but only specifies an exact reason for contradictory metadata. The current classifier selects an explicit Rebuilding lifecycle before returning its already-calculated gap. The operator handoff explicitly restricts `gap_detected` to Ready indexes. The matrix verifies the individual conditions. See spine lines 3036–3042 and 3361–3363, `src/Hexalith.Conversations.Server/Projections/ConversationProjectionReadStore.cs:312–341`, `module/platform-handoff.md:554–560`, `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTenantIndexReadMatrixTest.cs:68–76`, and `src/Hexalith.Conversations.Server/Diagnostics/ConversationProjectionFreshnessClassifier.cs:60–79`.

**Disposition — Clarify AD-7 from the handoff.** For a structurally valid record, an explicit Rebuilding lifecycle emits `rebuilding`. A coverage gap in a Ready record emits `gap_detected`. Retain the existing contradiction check and add one overlapping-condition vector to the read and telemetry contract.

## Decisions left with their assigned owners

The deployable route, authoritative enumeration provider, supported package ranges and production exporter already have owners and acceptance criteria. Their selection can proceed in the named stories. The historical authority hold, incomplete release evidence and pending grants remain distinct from the five contract findings above.
