# Story 16.3 native proof — 2026-11-15

This directory retains routine current-change validation under the user's
2026-11-15 waiver. Source identity includes the committed baseline, raw indexed
root gitlinks, the actual EventStore checkout and SHA-256 hashes of the changed
sources. These are working-tree observations; no implementation commit,
publication, package/provider parity or release acceptance is claimed.

The entry ceremony (source-binding repair, owner/envelope qualifications and
trusted entry) and generated six-scenario final pair are **waived**, not PASS.
The existing test AppHost and its consumers are user-waived for this story;
this is not named baseline-owner/Architect/Runtime approval. Historical strict
tooling, V14 evidence, the ACTIVE hold and false authority flags are unchanged.

`run-native.py` prepares the existing Platform source mapping, builds Debug
outputs under the wrapper's private TMPDIR, and runs the built xUnit assembly
with `-class`, `-automated sync` and `-failSkips`. It retains command ledgers,
native XML/logs, source and assembly identities. It requires matching native rows/counters and every selected class, and rejects
an empty or partial native result. Reruns archive this lane's prior outputs
before recording a new source identity; failed runs leave no active PASS carrier. `empty-selection-control.json` records a deliberately absent test
class as REJECTED; this is not a passing runtime scenario.

Run from the Conversations repository:

```bash
TMPDIR="$PWD/artifacts" run-with-temp python3 module/validation/story-16-3-current-20261115/run-native.py diagnostics
TMPDIR="$PWD/artifacts" run-with-temp python3 module/validation/story-16-3-current-20261115/run-native.py apphost
TMPDIR="$PWD/artifacts" run-with-temp python3 module/validation/story-16-3-current-20261115/run-native.py integration
```

The temporary output layout is deliberate: the existing EventStore Aspire
project resolver resolves the owning repository from the assembly's parent
layout. TMPDIR remains disposable, while its root stays beneath ignored
`artifacts/`. No scratch output or mapping survives the wrapper.

The live collection owns a private `redis:7.4-alpine` container. Native fixture
diagnostics retain the actual image ID, Dapr/.NET runtime, app identity,
committed catalog root and configured state store. TenantCreated and membership
enter the channel-protected production HTTP consumer; the persisted healthy
checkpoint is observed before authenticated query traffic. The EventStore
orchestrator sends production projection and reconcile requests over Dapr.
A selected detail-write failure leaves the real pending/index state and
terminal scheduler row intact until injection is released. Cleanup checks
listeners, component/catalog directories and Dapr environment restoration;
removing the private Redis container removes every fixture state family.

## Measured native results and matrix

| Lane | Passed | Failed / skipped / not run |
| --- | ---: | --- |
| EventStore shared preflight diagnostics | 43 | 0 / 0 / 0 |
| Retained AppHost authentication/catalog boundary | 2 | 0 / 0 / 0 |
| Story 16.3 live scenarios | 5 | 0 / 0 / 0 |
| Existing live gateway/security/catalog regressions | 17 | 0 / 0 / 0 |
| Affected Server projection/tenant lanes | 117 | 0 / 0 / 0 |

Native XML, command ledgers and `*-native-result.json` retain the individual
tests and counts. The five-scenario fixture's completion census and disposal
also passed. Its recovery topology cohosts the production EventStore server and
Conversations domain-service registrations behind the existing live sidecar;
the retained Aspire AppHost separately proves real host startup and refusal.
This does not qualify separately deployed replicas or provider parity.

| Matrix obligation | Executed proof |
| --- | --- |
| Healthy resource, unavailable endpoint | Scenario01; `HealthyResourceWithUnavailableEndpointHasStableSafeDiagnostic` |
| Effective binding collision, unrelated port, cancellation | Scenario01; `EffectiveControlPlaneBindingCollisionNamesItsOwnerAndReleasesProbeSockets`, `UnrelatedOccupiedPortDoesNotFailEffectiveBindings`, `CancelledControlPlaneProbeDoesNotClassifyACollision` |
| Controlled first failure, durable pending and terminal-only work | Scenario02; per-conversation injection count, SDK state reads, scheduler `ReadAsync` and exclusion from `GetDueAsync` |
| Authenticated coordinator reconcile, cleared original work, durable query-visible recovery | Scenario03 |
| Duplicate retry and preserved newer progress | Scenario03 and Scenario05, including an accepted later production append and replay of the original authenticated reconciliation request |
| Drifted/unmapped route and authority refusal | Scenario04: committed-generation drift 503, unmapped-method fallback 401, absent workload assertion 401; terminal identity remains unchanged |
| Endpoint race and bounded caller cancellation | `EndpointRaceRetriesAndThenSucceeds`, `EndpointWaitPreservesCallerCancellation` |
| Missing Dapr and empty selection | Scenario01; `empty-selection-control.json` records the independently rejected zero-test result even though the runner itself returned 0 |
| Cleanup | Collection disposal verifies listeners/files removed and Dapr environment restored after deleting its private Redis instance |

## Native-source and history boundary

The enrolled tenant services use their real SDK gateway authority transport.
The fixture's separate loopback HTTP authority returns 403 and issues no token;
its refusal count is observed. This preserves the predecessor's ungranted
history/native-source boundary and avoids connection-retry delays. Native
source admission returns the supported unproven ordinary generation; this lane
does not qualify privileged resolution metadata, historical gap recovery or a
production worker grant. There is no substituted history client or fabricated
native admission. Each failed conversation must independently increment the
selected store-write failure counter before its terminal scheduler/index state
can count as the recovery subject.

## Separate Aspire CLI baseline

`baseline-commands.json`, the before/after inventories, state snapshots and
resource logs retain a real `aspire start --isolated` attempt. It started, but
Keycloak exited importing the existing realm: the Conversations worker client
has a 300-character description and the Keycloak database column accepts 255.
The three required health waits each timed out with exit 17. Targeted stop
returned 0; the final inventory is `[]`, matching entry. The existing shared
Dapr infrastructure was preserved. Dashboard login credentials are redacted.

This default-Keycloak baseline is BLOCKED and is separate from the native
AppHost lane, which supplies an explicit test authority with Keycloak disabled.
The realm import failure is outside this story's approved diagnostics, failure
injection and recovery scope; it was not converted to PASS.

Source preparation also retains existing HXW011 (missing Platform release tag),
HXW009 (Builds/Platform gitlink disagreement) and HXW010 (CLI/Platform version
range) advisories. Routine source proof supplies no tuple qualification.

The Story 7.3 guard runs used a disposable shared Git clone containing the
current workflow/renderer/contract files, with the contract's exact output
paths. AC-7.3-01 and AC-7.3-02 passed; the retained historical result files in the
working repository were not overwritten.

## Focused review corrections

The owning diagnostics lane now runs 43 cases, adding duplicate selected-binding,
explicit relative executable-path and refused-connection coverage. Cancellation
is triggered only after the handler enters. The fixture cleanup marks Docker
ownership before the create command, independently removes that attempted name,
and joins cancelled Docker processes and output readers.

`test_native_runner.py` provides 14 focused controls for missing/forged rows,
partial selected-class coverage, stale-output invalidation and baseline failures.
The baseline mode now emits `baseline-result.json`, returns nonzero for BLOCKED,
and records the full before/after inventories plus their equality. The retained
Keycloak blocker above remains separate and unchanged. This correction was
checked with only the diagnostics lane (43/43), the edited fixture's five native
scenarios (5/5) and the runner controls (15/15); final whole-change validation is
owned by the parent review.


## Final parent verification

After all patches, the parent independently validated **184 passing native test
rows** (43 diagnostic, 2 AppHost, 5 live, 17 gateway regression, 117 Server) and
**15 passing runner controls**, with zero failures/skips/not-run. Every final
source snapshot matches the current sources and every selected class has executed
rows. `parent-final-audit.json` records the final checks. The live lane was rerun
after the last helper edit. The spec is done; the sprint row is review.
No commit, publication or push was made. The separate Keycloak BLOCKED baseline
and the user-waived formal qualification/final pair remain explicit.
