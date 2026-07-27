# Projection Read-Store Population Proof v2

**Result:** pass

The production named projection route is `conversation/conversation-read-model`. It materializes the existing event contract through the shared decoder, persists to configured store `statestore`, and reports completion only after both tenant-scoped keys are durable:

- `projection:conversations:{tenantId}:{conversationId}`
- `projection:conversations-index:{tenantId}`

The production-boundary live fixture dispatched through EventStore's discovered named handler into the configured read-model store, then queried through the real Conversations detail/list services. Accepted and stable duplicate delivery converged to one detail and one index row at event position 1. A second-write failure was retryable and both queries stayed non-current until retry completed. Unavailable storage produced a bounded indeterminate result; tenant-mismatched input failed without writes or leakage.

After both derived keys were conditionally erased, detail/list queries did not backfill at query time. EventStore full replay executed the handler's side-effect-free two-operation coordinated plan, restored the exact keys, and produced current query results equivalent to the pre-deletion results.

## Hosting and promotion

The Conversations AppHost remains an explicitly non-packable, non-publishable module user/E2E harness with exactly three project resources: EventStore, Conversations Server, and Admin Web. The public EventStore helper supplies platform identity, `/alive` Dapr health, and EventStore reference/wait topology. Conversations' reusable ServiceDefaults facade and its tests are removed.

EventStore commit `0eb365797d06207e42b517375664f46405a7ad7d` owns idempotent Dapr client registration and the domain-module reference/wait helper. It is clean and reachable from `origin/main`. The umbrella candidate must record that exact commit as a mode-`160000` gitlink and run the mechanical promotion checker after the umbrella commit exists.

Aspire source-mode startup reached `eventstore`, `conversations`, and `conversations-admin-web`. The tool environment reaped the detached child after `aspire start` returned, so later live description was unavailable; deterministic topology tests independently cover the resource contract.

## Verification summary

| Proof | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| EventStore canonical host composition | 41 | 0 | 0 |
| EventStore Aspire domain helper | 4 | 0 | 0 |
| Conversations AppHost topology | 8 | 0 | 0 |
| Conversations async projection unit matrix | 6 | 0 | 0 |
| Production-boundary population/rebuild live fixture | 2 | 0 | 0 |

SM-C2 used the byte-identical baseline fixture and frozen envelope. All four rows satisfy `post P95 <= 1.05 * baseline P95`: CREATE 0.448550, APPEND 9.511050, LIST 3.331050, and OPEN 0.029950 microseconds per operation.

The companion JSON is authoritative for commands, environment/toolchain, exact state/query observations, source hashes, EventStore promotion identity, raw performance artifact bindings, and immutable signed-v1 hashes.
