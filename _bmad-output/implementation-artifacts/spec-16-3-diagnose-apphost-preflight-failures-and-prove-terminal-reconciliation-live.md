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

### Review Findings

Review pass 2 (2026-11-15, independent `bmad-code-review`): blind hunter, edge-case hunter, verification-gap and acceptance auditor over `ed6eedc1..bd91c2c6` (code, docs and runner scripts; generated evidence JSON/XML and `attempts/` excluded) plus the uncommitted EventStore submodule edits.

- [x] [Review][Decision] Committed Conversations change depends on uncommitted EventStore APIs — **Resolved 2026-11-15 by the user's "push all changes":** the four EventStore files, byte-identical to every final native lane's source identity, were committed as EventStore `d24bd5166` (validated commitlint) on top of `981fabaa3`, pushed to EventStore `origin/main`, and the root gitlink moved `2dcf708c..d24bd516`. Remaining for package-mode consumers: after EventStore's release publishes a `Hexalith.EventStore.Testing.Integration` containing these APIs, bump the Conversations EventStore package version. Original finding: `bd91c2c6` compiles against `DaprDiagnostics.WaitForAppHostEndpointAsync`/`RequireAvailableDaprControlPlaneBindings`/`RequireDaprExecutable`, the three new constants and `DaprReadModelStoreDecorator`. These exist only as modified/untracked files in `references/Hexalith.EventStore`. Its HEAD and indexed gitlink `2dcf708c` (= EventStore `origin/main`) contain none of them. A clean checkout or the CI source/package lanes (`ci.yml` integration and Aspire projects) cannot compile `Hexalith.Conversations.IntegrationTests` or `Hexalith.Conversations.AppHost.Tests`. The new package-mode `Hexalith.EventStore.Testing.Integration` reference also needs a published version with these APIs. `*-source-identity.json` records `eventStoreCommit` as a dirty-tree HEAD. Needs an authorized order: EventStore commit/push (and package publication for package mode), gitlink bump, then the Conversations commit (amend or follow-up), with validated commitlint messages.
- [ ] [Review][Decision] AppHost control-plane collision preflight is a no-op — `ConversationsAppHostRuntimeBoundaryTest` selects only resources named `placement`/`scheduler`. The AppHost model defines only `eventstore`, `conversations` and `conversations-admin-web`; placement and scheduler are external sidecar addresses, so the binding map is always empty and the helper returns without probing. The handoff and README claim "only ports the current model actually binds are checked". Probing external placement/scheduler ports would always collide, so the correct target is a choice: (a) select the bindings this AppHost actually creates (sidecar HTTP/gRPC/app ports) and assert the set is non-empty, or (b) remove the AppHost call and its claim, keeping helper coverage in the shared and Scenario01 tests.
- [ ] [Review][Patch] Add an owning test that `DaprReadModelStoreDecorator` implements every interface of `DaprReadModelStore` (11 today) so a new capability cannot be silently dropped behind 28 `is IReadModel*` capability checks in EventStore [references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprReadModelStoreDecorator.cs:11]
- [ ] [Review][Patch] Scenario05 assert persisted bytes and `ProjectId` immediately after the duplicate reconcile, before the next `TriggerAsync` redelivers head 2 and can mask a regression [tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3EvidenceTests.cs:158]
- [ ] [Review][Patch] Cover the bare-name-absent-from-PATH branch of `RequireDaprExecutable` (the branch `ConversationGatewayLiveFixture.ResolveDaprdPath` takes without `~/.dapr/bin/daprd`) [references/Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Integration.Tests/DaprTestPrerequisiteDiagnosticsTests.cs:178]
- [ ] [Review][Patch] Assert the claimed native-hint retention: the reconcile request's `Request.NativeSourceProof` equals the original `/project/v2` dispatch's [tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3EvidenceTests.cs:107]
- [ ] [Review][Patch] Pin expected native totals for the `regression` (17) and `server` (117) lanes so tests disappearing inside a selected class cannot pass [module/validation/story-16-3-current-20261115/run-native.py:199]
- [ ] [Review][Patch] Empty-selection control: require xUnit exit 0 and an existing XML before recording REJECTED (a missing file currently counts as EMPTY_TEST_SELECTION) and record the exit code in the control JSON [module/validation/story-16-3-current-20261115/run-native.py:206]
- [ ] [Review][Patch] Correct the fixture's "disposal rejects empty selection" docs: xUnit never creates the collection fixture when no test is selected; disposal rejects partial selection only [tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3LiveFixture.cs:75]
- [ ] [Review][Patch] Include the private Redis port in the released-listener check that backs the "private Redis removed, listeners released" cleanup message [tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3LiveFixture.cs:219]
- [ ] [Review][Patch] Document that the injected `ArgumentException` is chosen because `ConversationAsyncProjectionHandler` classifies it as terminal `HandlerFailure` (`InvalidOperationException` is retryable, others indeterminate), so the lane proves handler-terminal recovery, not transient store outages [tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3FailureStore.cs:45]
- [ ] [Review][Patch] Widen the 100/150 ms readiness budgets in the stable-diagnostic tests, which can expire before the first in-process response on a slow host [references/Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Integration.Tests/GeneratedApiSmokePreflightDiagnosticsTests.cs:126]
- [ ] [Review][Patch] Replace the README's stale "No commit, publication or push was made" with the actual state (Conversations side committed locally in `bd91c2c6`; EventStore uncommitted; nothing pushed) [module/validation/story-16-3-current-20261115/README.md:139]
- [ ] [Review][Patch] README says `test_native_runner.py` provides 14 controls; the file defines 15 and every other record says 15/15 [module/validation/story-16-3-current-20261115/README.md:121]
- [ ] [Review][Patch] README run block lists only 3 of the 5 native lanes and omits the `baseline` and `test_native_runner.py` commands [module/validation/story-16-3-current-20261115/README.md:28]
- [ ] [Review][Patch] README says the directory retains native logs, but `*.log` is git-ignored (`.gitignore:122`; zero logs tracked) while the ledgers point at them; state that logs are local/external per the Verification plan [module/validation/story-16-3-current-20261115/README.md:17]
- [ ] [Review][Patch] Remove the duplicate `import sys` [module/validation/story-16-3-current-20261115/test_native_runner.py:7]

Rejected (pass 2):

- false — `GetDueAsync` 100-item cap makes Scenario02's exclusion vacuous: the private Redis holds only this collection's work and each scenario clears its own item, so the page is far below the cap.
- false — `test_native_runner.py` `KeyError` without `TMPDIR`: `run-native.py` requires `TMPDIR` identically and both are documented to run under `run-with-temp`.
- false — Commands, trigger and queries bypass authenticated production traffic: the matrix requires authenticated wire recovery (asserted on `/project/v2/reconcile` with channel and workload credentials) and production query receivers; the Code Map prescribes triggering coordinator redispatch because `GetDueAsync` excludes terminal-only work.
- false — New Testing.Integration `ProjectReference` lacks a condition/fallback: with an empty root the sibling Server reference, whose types the test compiles against, is also excluded with no fallback, so that configuration fails regardless.
- false — Collision message falsely "names its owner": the matrix's "owning endpoint/port" is the model endpoint key the message reports.
- false — AppHost `ArtifactsPath`/`EnrolledRepositoryRoot` edits exceed decision 2: they exist only so the verification lane can build into disposable outputs, and provenance is still asserted against the actual layout.
- false — Socket errors other than in-use/denied, relative path without `BaseAddress`, `CancelAfter` maxima, `prepare-build` stderr in the JSON log: each fails loudly on an unreachable or misuse input.
- low — Lifecycle/frozen-block/change-log/stale-text inconsistencies in this spec: the fix edits the spec under review (the spec `done` / row `review` pair is the build workflow's intended pre-review state).
- low — Endpoint-readiness lane-budget expiry now surfaces as bare cancellation: uncommon (the 90 s budget expires first) and the fix adds a translation branch.
- low — `RequireDaprExecutable` lacks executable-bit/PATHEXT checks and its relative-path test assumes `dotnet` on PATH: unlikely on supported hosts; fix adds platform branches.
- low — Unrelated-port and pre-cancelled-token tests prove little: they match the "only selected bindings are probed" contract; stronger variants need extra hooks or default-port contention.
- low — Fixture disposal after a failed init can mask the original error, Redis port bind/release race, partial tenant admission recovery: rare and fixes add guards/retries.
- low — Wire observer can turn malformed JSON into 500: every sender is the production orchestrator or test code with valid `ProjectionDispatchRequest`; fix adds guards.
- low — `Story16_3FailureStore` unsynchronized counter/key and lazy assignment: scenarios are serialized and async completion orders the writes; all five scenarios pass in xUnit's order.
- low — Endpoint-race row maps to retry-then-success and the fixture's missing-Dapr restoration is not driven: the stable timeout diagnostic is covered separately; driving the fixture path needs an injectable daprd path.
- low — Row 1 is simulated on the gateway rather than an AppHost resource; "one terminal item cleared" is not shown against a sibling item: both need new topology or concurrent subjects.
- low — Retained Keycloak baseline predates `baseline-result.json`, hand-written `parent-final-audit.json`, absolute paths in `scoped-checks.json`, Keycloak text in every baseline result, baseline exception exit mismatch, `next(rglob)` DLL choice, missing source-drift controls, no git-status digest: evidence bookkeeping with no reader harm beyond what the README records; fixes need reruns or generators.
- low (maybe-false) — `ConversationGatewayRouteCatalog` unparenthesized `&&`/`||` admits every sidecar operation: intent is not determinable from the diff and the impact is limited to the fixture catalog; string-name `GetType(..., throwOnError: true)` and `FindRepositoryRoot` enrolled-root preference fail loudly or only for cross-checkout binaries.


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
