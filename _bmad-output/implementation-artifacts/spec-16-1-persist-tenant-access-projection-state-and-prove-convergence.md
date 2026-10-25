---
title: 'Story 16.1: Persist tenant-access projection state and prove convergence'
type: 'feature'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-context.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenant authorization uses a process-local projection lost on restart. Consumer-group delivery cannot populate every replica, and missing freshness evidence currently defaults to healthy.

**Approach:** Consume an additive Tenants durable projection capability backed by EventStore mutation/recovery infrastructure. Prove restart recovery without redelivery and consistent authorization across replicas.

**Decision:** Prepare prerequisites first; inherit Hexalith.Platform infrastructure and operational policy. On 2026-10-23 the user selected Platform enrollment and successor tooling preparation for review; runtime remains held. On 2026-10-25 the user approved additive prerequisite refresh with accepted CLI 4.35.2; runtime entry remains held.

## Boundaries & Constraints

**Always:** Enforce architecture AD-5/AD-6. EventStore owns identity, CAS, retry, replay, fenced recovery and acknowledgements; Tenants owns transitions/checkpoints. Conversations evaluates one state/health/checkpoint snapshot per invocation. Only proven healthy state authorizes. Preserve compatibility and historical evidence.

**Never:** Direct Dapr persistence, unconditional saves, process-local concurrency, read repair, synthetic health, or early acknowledgement. Exclude 16.2/16.3, deployment and destructive migration. Rollback removes adoption/configuration; retain events, evidence and accepted records.

## I/O & Edge-Case Matrix

| State/delivery | Required behavior |
| --- | --- |
| Healthy, exact-next sequence | Atomically commit transition and checkpoint |
| Identical accepted duplicate | No-op; verify older sequences against authoritative history |
| Conflicting identity/payload | Persist terminal Corrupt; deny |
| Future sequence | Persist Gapped, immutable expected sequence, monotonic recoveryThrough and fingerprints; deny |
| CAS/provider/retry exhaustion | Retryable failure, no acknowledgement; authorization unavailable |
| Fenced recovery | Validate history/evidence; restore Healthy only with AD-6 final CAS and no pending evidence |
| Missing/stale/unavailable or malformed state | Deny without disclosing tenant existence |
| Restart or second replica | Reload the same persisted decision/checkpoint without redelivery |

</frozen-after-approval>

## Code Map

- `module/README.md` and planning `epic-16-adoption-v2/`: current additive preparation with accepted/root CLI 4.35.2. Source schema accepts APP_API_TOKEN; published CLI still rejects both fields. The v1 packages, checker pins and observations remain historical and unchanged.
- EventStore Client: reuse `Projections/IReadModelStore.cs`, `ReadModelWritePolicy.cs`, `Streams/IAuthoritativeEventStreamReader.cs`; existing epoch fencing lacks AD-6 pending-horizon recovery.
- Tenants Client: `Projections/ITenantProjectionStore.cs` has unconditional save; `Handlers/TenantProjectionEventHandler.cs` uses process-local locking.
- Conversations `Server/TenantAccess/`: access reads state/health separately; default signal returns Healthy without proof. Registration uses the in-memory Tenants default. Keep Program.cs disclosure until proofs pass.

## Tasks & Acceptance

**Entry work; additive refresh approved, runtime/tooling qualification still pending:**

- [x] `module/` — retain prepared enrollment; qualification remains pending.
- [x] `.config/dotnet-tools.json`, `module/validation/epic-16-adoption-v2/`, planning `epic-16-adoption-v2/` — aligned accepted 4.35.2 and retained exact additive observations/bindings. Structural preparation passes; native validation still fails and runtime remains held. V1 is preserved.
- [ ] `_bmad/scripts/generate_story_record.py`, `_bmad/schemas/`, `_bmad/scripts/verify_story_16_{1,2,3}.py` — implement approved successor consumption and fifteen measured scenarios; preserve eighteen AC identities and completion guards.
- [ ] Planning `production-operational-envelope-v1.md`, `module/sm-2-checklist.md` — obtain owner-qualified parity/recovery/operator evidence and a separate scoped entry decision before runtime.

**After qualified entry; upstream paths belong to their root-declared repositories:**

- [ ] EventStore Client `Subscriptions/` and `Projections/` — validate raw binding before deduplication; add atomic CAS mutation, pending evidence, fenced replay and durable acknowledgement.
- [ ] Tenants Client `Projections/`, `Handlers/TenantProjectionEventHandler.cs`, `Registration/TenantServiceCollectionExtensions.cs` — add durable coherent snapshots and pure transitions; preserve signatures.
- [ ] Conversations `Server/TenantAccess/ConversationTenantProjectionSnapshotAdapter.cs`, access/signal/registration — evaluate one persisted snapshot; deny unproven health and bind injected configuration.
- [ ] Owner Client tests and Conversations `Server.Tests/TenantAccess/`, `IntegrationTests/TenantAccess/` — cover the frozen matrix, reverse saves, three interleaved future deliveries, horizon extension, lease expiry/restart, provider exhaustion and production composition; assert persisted end-state/acknowledgements.

**Acceptance Criteria:**

- Given acknowledged events, when restarting without redelivery or reading from another replica, then decisions/checkpoints agree.
- Given concurrent mutation/recovery, when authorizing, then one coherent snapshot controls access and unsafe states deny.
- Given qualified entry and committed owner revisions, when six adopted scenarios run, then the generated record reports `6/6/0/0/0/0`, compatibility, safe diagnostics and raw gitlink/rollback scope.

## Implementation Notes

2026-10-22: Hexalith.Platform selected; enrollment and entry qualification remain open.

2026-10-23: three owner repositories plus enrollment/evidence affected; no runtime changes. Approved prerequisite preparation is tracked separately in `spec-epic-16-adoption-preparation.md`; this runtime story remains draft/backlog.

## Spec Change Log

## Review Triage Log

## Verification

Historical 2026-10-23 handoff: prerequisite/adoption checks passed; 168 focused tests passed, 131 portably. Correct 4.35.1 token binding failed HXP002; native adoption retained 15 failing rows. These observations do not qualify the current tree.

2026-10-25 investigation at `8925c9fef46026d53d15478e2a744b3604c38a98`:

- `python3 _bmad/scripts/verify_epic_16_prerequisites.py --repository .` — exit 1, FAIL/SOURCE_BYTES_DRIFT; 75 assertions.
- `python3 _bmad/scripts/verify_epic_16_adoption.py --repository .` — exit 1, FAIL/SOURCE_BYTES_DRIFT and SOURCE_BINDING_INVALID; 45 assertions. Ten pinned inputs differ. Entry BLOCKED; hold ACTIVE; four flags false.
- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_verify_epic_16_prerequisites.py _bmad/scripts/tests/test_verify_epic_16_adoption.py` — exit 1; 134 passed, 34 failed. Adoption fixtures fail source binding before intended assertions.
- Runtime verifiers remain absent; envelope/SM-2 unaccepted. After entry, run individual Debug builds, focused xUnit assemblies, affected suites and scoped verification.

2026-10-25: user approved additive 4.35.2 prerequisite refresh. Implementation is tracked in `spec-epic-16-adoption-v2-preparation.md`; this runtime draft and sprint backlog remain held.

2026-10-25 additive refresh: [v2 owner package](../planning-artifacts/epic-16-adoption-v2/README.md)
binds current inspected sources, the immutable v1 predecessor and
[fresh native observations](../../module/validation/epic-16-adoption-v2/results.json).
Accepted `4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0` restores/version-checks
successfully. Ordinary and both adoption modes still exit 1, including two
HXP002 token fields; current-source schema acceptance does not establish binary
compatibility. Adoption retains six PASS/eighteen FAIL rows. V2 structural PASS
grants no runtime entry; all owner/qualification blockers remain. The original
v1 CLI truthfully fails current-source drift, and its unchanged historical
assertions reproduce with exact portable root/tool/reference fixtures. Sprint
status is untouched; runtime tasks remain unchecked.
