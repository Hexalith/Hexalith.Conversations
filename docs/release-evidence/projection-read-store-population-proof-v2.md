# Projection Read-Store Population Proof v2

**Result:** fail

The production named projection route is `conversation/conversation-read-model`. It materializes the existing event contract through the shared decoder, persists to configured store `statestore`, and reports completion only after both tenant-scoped keys are durable:

- `projection:conversations:{base64url(tenantId)}:{base64url(conversationId)}`
- `projection:conversations-index:{base64url(tenantId)}`

The production-boundary live fixture dispatched through EventStore's discovered named handler into the configured read-model store, then queried through the real Conversations detail/list services. Accepted and stable duplicate delivery converged to one detail and one index row at event position 1. A second-write failure was retryable and both queries stayed non-current until retry completed. Unavailable storage produced a bounded indeterminate result; tenant-mismatched input failed without writes or leakage.

After the detail, tenant-index, and dispatch-ledger keys were conditionally erased, detail/list queries did not backfill at query time. EventStore full replay executed the handler's side-effect-free three-operation coordinated plan, restored all three exact keys, and produced current query results equivalent to the pre-deletion results.

## Hosting and promotion

The Conversations AppHost remains an explicitly non-packable, non-publishable module user/E2E harness with exactly three project resources: EventStore, Conversations Server, and Admin Web. The public EventStore helper supplies platform identity, `/alive` Dapr health, and EventStore reference/wait topology. Conversations' reusable ServiceDefaults facade and its tests are removed.

EventStore commit `defb426f0bd9e3bd1247bc7149605b4bb6ef70d0` is the clean root gitlink and remains reachable from `origin/main`. It retains the earlier bulk-read, deterministic rebuild-rejection, Data Protection, bounded-redelivery, and TimeToLive surfaces. Story capability commit `bb4c81d4eaf33521afc00bdfa634e1c2e790f796` adds the platform-owned terminal dispatch-reconciliation contract used by Conversations; the final `defb426f` pointer contains that capability. The exact four production files and four focused tests in the `4c63f5d3..defb426f` delta are hash-bound in the companion JSON.

The mechanical checker passed in an isolated clean checkout against umbrella candidate `b261fe209c4ca6c966f4bd2a78a62a2d83ddde08` over the three approved declarations — `references/Hexalith.EventStore`, `references/Hexalith.Builds`, and `references/Hexalith.Tenants`, each with `require_remote: true`. All seven changed root gitlinks were evaluated as initialized, clean, and exactly captured at mode `160000`; the declared three are remotely available. The result has **zero blockers and four undeclared-gitlink warnings** for `references/Hexalith.AI.Tools`, `references/Hexalith.Commons`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Memories`. Those warnings disclose changes outside the approved scope without silently expanding it. The isolated checkout prevents unrelated newer Builds, Memories, and Tenants worktree positions from being moved or captured by this story.

The previously recorded EventStore pointer `4c63f5d3e8089a85891cdbf8d87ce82ee445354a` and umbrella candidate `28e217ed0ac8c1028a4783f80ec47393ff1fbfbd` are superseded. The explicit `4c63f5d3..defb426f` delta in the companion JSON distinguishes the review-required platform capability commit from the final recorded root gitlink.

The recorded candidate is the last revision that moved any root gitlink or production source; later revisions carry evidence, tests, and the story record only. The conformance validator re-derives the candidate's complete changed-gitlink set, candidate tree entries, declared remote-tracking containment, and exact production/test/platform hashes. It also requires no committed or uncommitted production source and no root-gitlink movement after the candidate. Any later production or gitlink movement turns this proof red.

## Gateway production boundary (ADR 0003 Verification 1-2)

Story 6.2 task T2 offered two closures for the production-boundary question: strengthen the fixture, or record a named-owner justification narrowing ADR 0003's own verification wording. **Jerome chose to strengthen the fixture**, so nothing in ADR 0003 Verification 1-2 is narrowed, waived, or carried as a residual gap.

`ConversationProjectionGatewayDispatchLiveTests` drives delivery through `IProjectionUpdateOrchestrator` against a real `daprd` sidecar with a Redis-backed `statestore` component, not through an in-process dispatcher call and not against an in-memory fake. The lane verifies the live sidecar metadata reports a `state.redis` `statestore` with the `ACTOR` capability. It also asserts that the configured `IReadModelStore` is `DaprReadModelStore`, the projection refresh interval is `0`, and the gateway discovered the `conversation-read-model` route from the domain service's operational-index metadata. Structured host logs from the passing run contain both `ProjectionUpdateOrchestrator` and `NamedProjectionDispatchCoordinator` categories, observing the gateway-side stages rather than assuming them.

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
| Bound deterministic dispatch artifact | 27 | 0 | 0 |
| Bound gateway production-boundary artifact | 2 | 0 | 0 |
| Bound population/replay artifact | 2 | 0 | 0 |

SM-C2 uses byte-identical production-path fixture/project overlays, an evaluated direct-project-reference manifest, and raw xUnit runner artifacts under the frozen Release envelope. CREATE crosses authorization and the real create boundary/aggregate; APPEND crosses authorization and the production idempotent executor's success, replay, and conflict branches. LIST and OPEN exercise production query/read-store/hydration paths. The latest mechanically bound values remain authoritative in the companion JSON. **SM-C2 remains an open release blocker** while any row exceeds the frozen 5% threshold, so this proof's overall result is fail even though its functional boundary lanes pass.

The companion JSON is authoritative for commands, environment/toolchain, exact state/query observations, source hashes, EventStore promotion identity, raw performance artifact bindings, and immutable signed-v1 hashes.
