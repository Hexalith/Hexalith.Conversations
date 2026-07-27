# Projection Read-Store Population Proof v2

**Result:** pass

The production named projection route is `conversation/conversation-read-model`. It materializes the existing event contract through the shared decoder, persists to configured store `statestore`, and reports completion only after both tenant-scoped keys are durable:

- `projection:conversations:{tenantId}:{conversationId}`
- `projection:conversations-index:{tenantId}`

The production-boundary live fixture dispatched through EventStore's discovered named handler into the configured read-model store, then queried through the real Conversations detail/list services. Accepted and stable duplicate delivery converged to one detail and one index row at event position 1. A second-write failure was retryable and both queries stayed non-current until retry completed. Unavailable storage produced a bounded indeterminate result; tenant-mismatched input failed without writes or leakage.

After both derived keys were conditionally erased, detail/list queries did not backfill at query time. EventStore full replay executed the handler's side-effect-free two-operation coordinated plan, restored the exact keys, and produced current query results equivalent to the pre-deletion results.

## Hosting and promotion

The Conversations AppHost remains an explicitly non-packable, non-publishable module user/E2E harness with exactly three project resources: EventStore, Conversations Server, and Admin Web. The public EventStore helper supplies platform identity, `/alive` Dapr health, and EventStore reference/wait topology. Conversations' reusable ServiceDefaults facade and its tests are removed.

EventStore commit `c8c7003052a7f811d3b821f3442379ca5f3a9c65` owns idempotent Dapr client registration and the domain-module reference/wait helper. It is clean and reachable from `origin/main`. The Story 6.7 mechanical checker passed against umbrella candidate `953bf71a5ad23c16c416db13ed7cea046abf58d0` over three declared root gitlinks — `references/Hexalith.EventStore`, `references/Hexalith.Builds`, and `references/Hexalith.Tenants`, each with `require_remote: true` — all initialized, clean, remotely available, and exactly captured at mode `160000`, with **no blockers and no warnings**.

The previously recorded pair (`0eb3657` / candidate `b11b0c7`) is superseded. Commit `48069d7` re-pointed the EventStore gitlink to `c8c7003`, so the earlier evidence no longer corresponded to any single revision. The intervening EventStore delta `0eb3657..c8c7003` is two commits touching publication preflight, story documentation, a nested pointer, and container publishing governance tests; **neither promoted-capability file changed** — `EventStoreDomainServiceExtensions.cs` still registers the idempotent `AddDaprClient()` at line 310 and `HexalithEventStoreDomainModuleExtensions.cs` is byte-identical. Both remain hash-bound in the companion JSON.

The recorded candidate is the last revision that moved any root gitlink or production source; later revisions carry evidence, tests, and the story record only. That binding is not left on trust: the conformance validator re-derives the gitlinks from the working tree and requires the candidate to be an ancestor of `HEAD`, `git diff --name-only <candidate>..HEAD -- references/` to be empty, and each recorded gitlink to equal `git rev-parse HEAD:references/<path>`. Moving a declared gitlink afterwards turns this proof red rather than leaving it quietly stale.

## Gateway production boundary (ADR 0003 Verification 1-2)

Story 6.2 task T2 offered two closures for the production-boundary question: strengthen the fixture, or record a named-owner justification narrowing ADR 0003's own verification wording. **Jerome chose to strengthen the fixture**, so nothing in ADR 0003 Verification 1-2 is narrowed, waived, or carried as a residual gap.

`ConversationProjectionGatewayDispatchLiveTests` drives delivery through `IProjectionUpdateOrchestrator` against a real `daprd` sidecar with a Redis-backed `statestore` component, not through an in-process dispatcher call and not against an in-memory fake. The lane asserts that the configured `IReadModelStore` really is `DaprReadModelStore`, that the projection refresh interval is `0` so `UpdateProjectionAsync` dispatches immediately instead of registering polling work and returning against an empty store, and that the gateway discovered the `conversation-read-model` route from the domain service's own operational-index metadata. Structured host logs from the passing run carry `ProjectionUpdateOrchestrator` and `NamedProjectionDispatchCoordinator` categories, so the gateway-side stages are observed rather than assumed.

An accepted append persisted both tenant-scoped keys on one agreeing generation and the production detail/list queries returned `Current`. A duplicate delivery left persisted state unchanged and the tenant index at exactly one row. The fixture skips when no DAPR prerequisite is reachable, and a skip does not satisfy AC5 — the recorded run executed with **0 skipped**.

Aspire source-mode startup reached `eventstore`, `conversations`, and `conversations-admin-web`. The tool environment reaped the detached child after `aspire start` returned, so later live description was unavailable; deterministic topology tests independently cover the resource contract.

## Verification summary

| Proof | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| EventStore canonical host composition | 41 | 0 | 0 |
| EventStore Aspire domain helper | 4 | 0 | 0 |
| Conversations AppHost topology | 8 | 0 | 0 |
| Conversations async projection unit matrix | 6 | 0 | 0 |
| Production-boundary population/rebuild live fixture | 2 | 0 | 0 |
| Gateway production boundary over DAPR (ADR 0003 V1-2) | 2 | 0 | 0 |
| Full module conformance | 418 | 0 | 0 |

SM-C2 used the byte-identical baseline fixture and frozen envelope. All four rows satisfy `post P95 <= 1.05 * baseline P95`: CREATE 0.448550, APPEND 9.511050, LIST 3.331050, and OPEN 0.029950 microseconds per operation.

The companion JSON is authoritative for commands, environment/toolchain, exact state/query observations, source hashes, EventStore promotion identity, raw performance artifact bindings, and immutable signed-v1 hashes.
