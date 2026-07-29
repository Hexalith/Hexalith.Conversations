# Projection Read-Store Population Proof v2

**Result:** fail

The production named projection route is `conversation/conversation-read-model`. It materializes the existing event contract through the shared decoder, persists to configured store `statestore`, and reports completion only after both tenant-scoped keys are durable:

- `projection:conversations:{base64url(tenantId)}:{base64url(conversationId)}`
- `projection:conversations-index:{base64url(tenantId)}`

The production-boundary live fixture dispatched through EventStore's discovered named handler into the configured read-model store, then queried through the real Conversations detail/list services. Accepted and stable duplicate delivery converged to one detail and one index row at event position 1. A second-write failure was retryable and both queries stayed non-current until retry completed. Unavailable storage produced a bounded indeterminate result; tenant-mismatched input failed without writes or leakage.

After both derived keys were conditionally erased, detail/list queries did not backfill at query time. EventStore full replay executed the handler's side-effect-free two-operation coordinated plan, restored the exact keys, and produced current query results equivalent to the pre-deletion results.

## Hosting and promotion

The Conversations AppHost remains an explicitly non-packable, non-publishable module user/E2E harness with exactly three project resources: EventStore, Conversations Server, and Admin Web. The public EventStore helper supplies platform identity, `/alive` Dapr health, and EventStore reference/wait topology. Conversations' reusable ServiceDefaults facade and its tests are removed.

EventStore commit `4c63f5d3e8089a85891cdbf8d87ce82ee445354a` is clean, checked out exactly at the root gitlink, and reachable from `origin/main`. It retains the earlier bulk-read and deterministic rebuild-rejection surfaces, promotes Data Protection into the canonical host registration, and adds the bounded redelivery-window/TimeToLive capability required by this review. Commit `700364eddaf92e82a8bd9131e94cdea96681d59d` introduces `IReadModelExpiringStore` and propagates TimeToLive through direct and batch read-model operations; `4c63f5d3e8089a85891cdbf8d87ce82ee445354a` completes the fingerprint semantics and documentation. The story-relevant platform files and focused tests are hash-bound in the companion JSON; the original idempotent Dapr client registration and domain-module reference/wait helper remain byte-bound and intact.

The mechanical checker passed in an isolated clean checkout against umbrella candidate `28e217ed0ac8c1028a4783f80ec47393ff1fbfbd` over the three approved declarations — `references/Hexalith.EventStore`, `references/Hexalith.Builds`, and `references/Hexalith.Tenants`, each with `require_remote: true`. All seven changed root gitlinks were evaluated as initialized, clean, and exactly captured at mode `160000`; the declared three are remotely available. The result has **zero blockers and four undeclared-gitlink warnings** for `references/Hexalith.AI.Tools`, `references/Hexalith.Commons`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Memories`. Those warnings disclose changes outside the approved scope without silently expanding it. The isolated checkout prevents unrelated newer Builds, Memories, and Tenants worktree positions from being moved or captured by this story.

The previously recorded EventStore pointer `b1d08dac328ee6a2f9b4ef07a1a14ad5756ba94e` and umbrella candidate `747263286da796003a73ecaa1b22d2b36263e568` are superseded. The explicit `b1d08da..4c63f5d3` delta in the companion JSON distinguishes the review-required platform commits from unrelated EventStore history instead of treating the current pointer as an opaque promotion.

The recorded candidate is the last revision that moved any root gitlink or production source; later revisions carry evidence, tests, and the story record only. That binding is not left on trust: the conformance validator re-derives the gitlinks from the working tree and requires the candidate to be an ancestor of `HEAD`, `git diff --name-only <candidate>..HEAD -- references/` to be empty, and each recorded gitlink to equal `git rev-parse HEAD:references/<path>`. Moving a declared gitlink afterwards turns this proof red rather than leaving it quietly stale.

## Gateway production boundary (ADR 0003 Verification 1-2)

Story 6.2 task T2 offered two closures for the production-boundary question: strengthen the fixture, or record a named-owner justification narrowing ADR 0003's own verification wording. **Jerome chose to strengthen the fixture**, so nothing in ADR 0003 Verification 1-2 is narrowed, waived, or carried as a residual gap.

`ConversationProjectionGatewayDispatchLiveTests` drives delivery through `IProjectionUpdateOrchestrator` against a real `daprd` sidecar with a Redis-backed `statestore` component, not through an in-process dispatcher call and not against an in-memory fake. The lane asserts that the configured `IReadModelStore` really is `DaprReadModelStore`, that the projection refresh interval is `0` so `UpdateProjectionAsync` dispatches immediately instead of registering polling work and returning against an empty store, and that the gateway discovered the `conversation-read-model` route from the domain service's own operational-index metadata. Structured host logs from the passing run carry `ProjectionUpdateOrchestrator` and `NamedProjectionDispatchCoordinator` categories, so the gateway-side stages are observed rather than assumed.

An accepted append persisted both tenant-scoped keys on one agreeing generation and the production detail/list queries returned `Current`. A duplicate delivery left persisted state unchanged and the tenant index at exactly one row. Missing DAPR/Redis prerequisites and startup failures now fail the mandatory fixture; the recorded run passed 2/2 with **0 skipped**.

The mandatory Aspire runtime-boundary test reached healthy `eventstore` and `conversations` resources, submitted a real `CreateConversation` through EventStore, and observed `Completed` with the expected aggregate id and a non-zero event count. This lane exposed and corrected the former topology mismatch between the plural Aspire resource name `conversations` and EventStore's canonical DAPR service-invocation app id `conversation`. The deterministic topology suite passes 8/8 and pins those distinct identities.

## Verification summary

| Proof | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| EventStore canonical host composition | 41 | 0 | 0 |
| EventStore Aspire domain helper | 4 | 0 | 0 |
| EventStore bulk read-store and registration | 19 | 0 | 0 |
| EventStore deterministic rebuild dispatcher | 30 | 0 | 0 |
| Full AppHost suite with runtime boundary enabled | 9 | 0 | 0 |
| Full Conversations Server suite | 653 | 0 | 0 |
| Full production-boundary integration suite | 14 | 0 | 0 |
| Full module conformance | 428 | 0 | 0 |

SM-C2 now uses byte-identical production-path fixture/project overlays and the frozen Release envelope. CREATE (0.429550 µs) and APPEND (12.871000 µs) pass. LIST (2150.809150 µs versus 568.575000 allowed) and OPEN (276.978600 µs versus 23.718345 allowed) fail. **SM-C2 remains an open release blocker**, so this proof's overall result is fail even though its functional boundary lanes pass.

The companion JSON is authoritative for commands, environment/toolchain, exact state/query observations, source hashes, EventStore promotion identity, raw performance artifact bindings, and immutable signed-v1 hashes.
