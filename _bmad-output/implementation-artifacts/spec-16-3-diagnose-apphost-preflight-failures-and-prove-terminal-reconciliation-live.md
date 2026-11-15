---
title: 'Story 16.3: Diagnose AppHost preflight failures and prove terminal reconciliation live'
type: 'feature'
created: '2026-10-22'
updated: '2026-11-15'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'ed6eedc146b493bfbf7250af7356fd3b20d8333f'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** AppHost readiness errors lack stable classifications, and live coverage does not prove recovery of durable terminal projection work.

**Approach:** Add shared preflight diagnostics and a live AppHost proof of production reconciliation, then derive the six-scenario record from measured results.

**2026-10-30 decision:** The user selected prerequisite-first execution: qualify entry, complete 16.1, then 16.2, then 16.3. Pursue a narrow exemption for the existing AppHost test fixture; named baseline-owner/Architect/Runtime acceptance remains pending. Separate early preparation and Platform relocation are deferred.

**2026-11-15 approved decisions (supersede the earlier entry sequence):**
1. The user approved the routine current-change path for 16.3, waiving the entry ceremony (including source-binding repair, owner/envelope qualifications and trusted entry) and generated six-scenario final pair. Completion proof is passing native xUnit with retained logs and source identity. Record waived items as waived, never PASS; preserve strict tooling, historical records, ACTIVE hold and authority flags. This decision establishes no provider parity, operational qualification or release approval.
2. The user authorized the existing `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj` and its test consumers for this story, waiving pending baseline-owner/Architect/Runtime acceptance. Keep it non-packable/non-publishable; limit changes to diagnostics, failure injection and recovery verification. Record a user waiver, not named-owner approval. Platform relocation and the minimal-module fixture remain outside scope; revisit relocation when an adopted Platform replacement exists.
3. Keep the full technical proof: valid catalog enrollment, real tenant admission, authenticated production traffic, durable terminal/end-state observations, query-visible recovery, duplicate/newer-progress safety and cleanup. Shared helpers remain EventStore-owned. Preserve the predecessor deferrals for tenant-wide rebuild/erase. No commit, publication or push is authorized.

## Boundaries & Constraints

**Always:** EventStore owns writes, hosting and the authenticated `project/v2/reconcile` coordinator route. Shared helpers belong there. Require durable pending/end-state observations, production queries, bounded cancellation, safe diagnostics and isolated fixture cleanup. Bind evidence to committed sources, runtime/route/catalog identities, raw gitlinks and rollback. Preserve V14 history; consume authorized successor contracts.

**Never:** Direct-handler live proof, reflective worker access, another reconciler, read repair, weaker authorization, vacuous PASS or fabricated approvals. Rollback removes new diagnostics/fixtures/evidence; preserve production routes and 16.1/16.2 state.

## I/O & Edge-Case Matrix

| State | Required result |
| --- | --- |
| Healthy resource, endpoint unavailable | `APPHOST_ENDPOINT_NOT_READY`, endpoint identity, bounded retry advice; no tenant leakage |
| Effective control-plane binding occupied | `DAPR_CONTROL_PLANE_PORT_COLLISION`, owning endpoint/port; unrelated ports do not fail |
| Controlled first projection failure | Durable pending/terminal work observable before recovery |
| Production coordinator retry | Reconciliation observed on wire; one terminal item cleared; persisted projection query-visible |
| Duplicate retry or concurrent progress | Idempotent clearance; newer state preserved |
| Removed/drifted route, endpoint race, missing Dapr, empty test selection | Stable failure/blocker; nonempty ledger; ports/files restored |

</frozen-after-approval>

## Code Map

`DaprDiagnostics` lacks both stable codes. The gateway fixture's tenant bypass cannot establish production admission.

`GetDueAsync` excludes terminal-only work: trigger coordinator redispatch through `NamedProjectionDispatchCoordinator.ReconcileTerminalRoutesAsync`, observing public scheduler `ReadAsync`. Preserve deferred rebuild/erase scope.

## Tasks & Acceptance

**After authorized entry, in order:**

- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprDiagnostics.cs` — implement shared diagnostics; extend owning `DaprTestPrerequisiteDiagnosticsTests.cs` and `GeneratedApiSmokePreflightDiagnosticsTests.cs` for matrix/cancellation controls.
- [x] `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs` and its `.csproj` — adopt mapped shared diagnostics; retain authentication/refusal coverage.
- [x] `tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3LiveFixture.cs` — isolate enrolled production hosts, real tenant admission, durable observations and failure injection.
- [x] `tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3EvidenceTests.cs` — implement `Scenario01`–`Scenario05` through production traffic; cover matrix/cleanup.
- [x] `module/platform-handoff.md`, this spec and `sprint-status.yaml` — record fixture decision, retry/rollback, results/review and accepted transition.

**Acceptance Criteria:**

- Given authorized entry, when tests run, then every matrix row executes with zero failures/skips/not-run, authenticated wire recovery is durable/query-visible, retries preserve newer progress and fixtures restore.
- Given the approved native-proof waiver, when completion is recorded, then waived formal checks remain explicit and no formal qualification or generated-pair PASS is claimed.

## Implementation Notes

2026-11-15 implementation: EventStore owns stable endpoint/collision/missing-Dapr
diagnostics and the test-only Dapr store decorator. The decorator forwards the
production adapter's full capabilities; the Conversations fixture overrides
only the selected scalar write failure. The AppHost consumer uses mapped shared
diagnostics and supports disposable build outputs without weakening provenance.

The dedicated native collection reuses the existing cohosted production gateway
and domain-service boundary behind a real sidecar, with a private disposable
Redis instance, committed observed catalog/finite receiver, protected tenant
HTTP delivery and durable healthy membership at checkpoint 2. Each recovery
subject must independently reach the selected store injection, then expose its
pending index marker and terminal scheduler row. Coordinator retry retains the
original actor native hint, crosses authenticated `project/v2/reconcile`, clears
that original row and persists query-visible state. Duplicate and later-progress
controls preserve the newer generation. Disposal checks ports, component/catalog
files and Dapr environment restoration; no worker reflection, direct projection
handler call or alternate reconciler was added.

The real SDK tenant worker transport receives immediate HTTP 403 from an
isolated loopback refusal authority, which issues no token. This preserves the
predecessor's ungranted authoritative-history/native-source boundary: ordinary
generations remain unproven for privileged resolution metadata. The observed
refusal is separate from, and cannot substitute for, the required injected store
failure. Tenant-history gap recovery and tenant-wide rebuild/erase stay deferred.

The user-waived fixture decision, retry/rollback and full matrix mapping are in
`module/platform-handoff.md` and `module/validation/story-16-3-current-20261115/README.md`.
Entry/final-pair and named-owner acceptance remain **waived**, never PASS. No
hold or authority flag changed, and no commit, publication or push was made.
The implementation and native proof are ready for the parent workflow's
independent review; its accepted lifecycle transition remains pending.

## Spec Change Log

2026-10-29: refreshed entry facts, existing-tooling reuse, native test identity and output paths. KEEP frozen intent, draft/backlog, predecessor chain, accepted evidence and ACTIVE hold unchanged.

2026-10-30: recorded the user's selected sequence/exemption direction and removed answered questions. KEEP prior intent, pending owner acceptance, draft/backlog and ACTIVE hold.

2026-11-15: refreshed predecessors/code map and exposed story-specific entry/exemption choices. KEEP frozen intent byte-identical and accepted evidence unchanged.

2026-11-15: user approved the recommended entry/native-proof and test-AppHost waivers; recorded their scope above, removed Open Questions and started implementation. KEEP production-boundary proof and predecessor deferrals.

## Review Triage Log

Review pass 1 (2026-11-15): blind hunter B, edge-case hunter E, verification-gap V. Each finding was assessed before grouping.

| ID | Verdict | Evidence and route |
| --- | --- | --- |
| B1 | false | The frozen matrix explicitly permits duplicate retry **or** concurrent progress. Scenario05 replays the original authenticated request after newer persisted progress and checks byte equality; no overlapping-execution claim is made. Reject the proposed additional concurrency requirement. |
| B2 | false | Scenario04 exercises committed-generation fingerprint drift at the real enrolled receiver and preserves terminal identity, satisfying the matrix's removed/drifted-route alternative. Its separate unmapped-URL control is labelled unmapped, not proof of catalog removal. Reject mandatory catalog-mutation expansion. |
| B3 | medium | Cancellation may fire while TestServer starts, allowing the initial cancellation check to satisfy the test without an in-flight request. Patch the existing test to cancel after handler entry. |
| B4 | medium | Every successful probe socket is disposed within its iteration; two selected names sharing one initially free endpoint both pass. Patch: retain probes through the complete inventory and release them on all exits. |
| B5 | low | AccessDenied can mean permission restriction or an exclusive Windows owner (Microsoft Winsock documentation); the code cannot infer a listening process from this error. Current consumers select local unprivileged bindings and can select an isolated port. Reject extra cross-platform cause-classification machinery as disproportionate to this uncommon ambiguity. |
| B6 | medium | A configured explicit relative path is incorrectly joined beneath PATH directories; existing process startup would resolve it against cwd. Patch explicit path detection and cover the relative case. |
| B7 | medium | Before a failed rerun, source identity is replaced but old XML/result/assembly files remain; readers can combine new input identity with prior PASS. Patch attempt invalidation/status before preparation. |
| B8 | medium | The local validator trusts aggregate counters and accepts missing rows; regression/server selectors can silently disappear because expected inventory is absent. Patch row/result and selected-class checks; add negative controls. |
| B9 | medium | Baseline commands permit failed readiness/cleanup and then exit 0 without comparing inventory. Patch explicit BLOCKED/nonzero result and before/after comparison; preserve the already recorded Keycloak blocker. |
| B10 | medium | Docker may create the named container before its CLI fails, while _started is assigned only on successful return. Patch attempted-container cleanup independent of successful response. |
| B11 | medium | After killing a timed-out Docker process the helper returns without joining the process or settling its readers. Patch child/stream completion before returning. |
| B12 | medium | Authority disposal precedes Redis removal in one finally block; its exception prevents later cleanup. Patch independent finally cleanup for the owned container. |
| E1 | medium | Same demonstrated socket-lifetime defect as B4. Patch that root cause once. |
| E2 | medium | Same attempted-container cleanup defect as B10. Patch that root cause once. |
| E3 | medium | Same teardown sequencing defect as B12. Patch that root cause once. |
| E4 | medium | Same incomplete selected-class census as B8. Patch that validator root cause once. |
| E5 | medium | Same missing child-process join as B11. Patch that root cause once. |
| V1 | medium | Pre-verified gap: all readiness failure cases obtain HTTP responses; no test reaches HttpRequestException. Patch a refused-connection case asserting bounded stable safe timeout diagnostics. |

Parent patch audit P1: **medium / patch** — the runner's exception branch writes BLOCKED/2 and then raises an ordinary Python exception, whose CLI exit is 1. Correct ordinary-failure reporting to agree with the actual CLI exit; preserve the explicit baseline BLOCKED/2 path. This is a local result-reporting correction, not a change to runtime acceptance.

All surviving findings route to patch: local corrections/tests with no new production public surface or changed recovery authority. Keep authenticated durable recovery, predecessor semantics, source observations and the approved waivers.


## Verification

At `ed6eedc146b493bfbf7250af7356fd3b20d8333f`, ran:

`run-with-temp python3 _bmad/scripts/verify_story_16_3.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --scenario AC-16.3-01 --candidate HEAD`

Repeated with `epic-16-tooling-v3`. Both: FAIL/1, `SOURCE_DRIFT` on `.config/dotnet-tools.json`, zero runtime tests/receipts. Hold ACTIVE; flags false. V2 remains selected; V3 was diagnostic only.

After authorization: Platform-mapped Debug builds; built xUnit `-class`/`-method`, `-automated sync -failSkips` for diagnostics, AppHost, live recovery and affected Server lanes. Use `run-with-temp`; retain logs externally. Run whitespace/Story 7.3 guards. Formal evidence-boundary/final-pair gates are waived for this story; routine checks and native proof apply. No commit/publication/push without authorization and required commitlint.

### Routine native verification — 2026-11-15

Retained commands, XML/logs, committed baseline/raw gitlinks, changed-source
SHA-256 and built assembly identities are in
`module/validation/story-16-3-current-20261115/`. Run each lane through
`TMPDIR="$PWD/artifacts" run-with-temp python3 module/validation/story-16-3-current-20261115/run-native.py <lane>`.

Native results: `diagnostics` **40/40**, `apphost` **2/2**, `integration`
**5/5**, `regression` **17/17**, `server` **117/117**; zero failed, skipped or
not-run tests. Builds were mapped Debug with zero warnings/errors. The separate
zero-selection control was rejected by the exact-five/nonempty validator even
though xUnit returned 0. Story 7.3 `AC-7.3-01` and `AC-7.3-02` passed using the
current workflow files in a disposable Git snapshot without overwriting their
historical output paths. Whitespace checks pass in both owning repositories.

The additional default-Keycloak `aspire start --isolated` baseline is **BLOCKED**:
realm import rejects the existing worker client's 300-character description
against the 255-character Keycloak database column. All three health waits
returned 17 after 120 seconds. Exact state/resource logs and command results are
retained separately. Targeted stop returned 0 and final Aspire inventory `[]`
matched entry; pre-existing Dapr infrastructure was preserved. This is distinct
from the passing explicit-test-authority AppHost lane, and needs separately
scoped realm correction. Existing HXW011/HXW009/HXW010 source-preparation
advisories retain their unqualified meanings.

Parent pre-review audit: independently parsed all 181 passing XML test rows and checked matrix coverage. Current live/AppHost/regression source snapshots match. Diagnostics and Server snapshots predate only the later integration-test assertion edit, which neither lane compiles; their changed compiled inputs match. Root gitlink and whitespace checks pass. Formal evidence qualification remains waived; independent review is in progress.


### Final post-review verification — 2026-11-15

All review patches were inspected and verified. The final mapped Debug lanes pass:
shared diagnostics **43/43**, AppHost **2/2**, live recovery **5/5**, existing
gateway regressions **17/17**, and affected Server **117/117**: **184 native tests**
with zero failed, skipped or not-run. The result runner's **15/15** controls pass,
including actual CLI failure/result agreement, forged/missing native rows,
partial class selection, stale-output archiving and baseline inventory/status.
`module/validation/story-16-3-current-20261115/parent-final-audit.json` independently
checks the XML rows/classes, current source identities, empty-selection rejection
and Story 7.3 receipts. Every final native source snapshot matches the current
sources; the live lane was rerun after the last shared-helper edit.

Review resolutions: B3/V1 now cover active-request cancellation and refused
connections; B4/E1 holds listening probes across the complete inventory and
releases them in finally; B6 handles explicit relative executable paths;
B7/B8/E4 archives stale results and checks rows/classes; B9 reports baseline
BLOCKED/nonzero with inventory equality; B10/E2, B11/E5 and B12/E3 independently
clean attempted container creation, join killed children/readers and retain
container cleanup after authority disposal errors. Parent P1's ordinary exception
record now reports FAIL/1, proved against an actual subprocess exit. No new
review item was deferred; the rejected expansion/low-impact items remain in the
triage log.

The formal evidence-boundary gate is **not-applicable under the approved routine
path**. The v9/tooling-v2/v3 generated-final-pair completion gate is **waived by
the 2026-11-15 user decision**, not PASS; no pair was generated or inserted.
Strict tooling, historical records, hold and authority flags remain unchanged.
The separately observed Keycloak baseline remains **BLOCKED**, with successful
cleanup/equal inventories; worker history/native-source authority remains denied.
These limits do not establish provider parity, privileged metadata or release
qualification.

The spec is done and the owned sprint row is review under the build workflow.
All work remains local, including EventStore submodule content edits. The user's
approved scope excludes commit/publication/push, so the workflow's local commit
step is not executed.
