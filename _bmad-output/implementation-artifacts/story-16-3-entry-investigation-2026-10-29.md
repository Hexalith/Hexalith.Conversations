# Story 16.3 entry investigation — 2026-10-29

Investigated clean `main` at `acd8b0b59238ee7c84c49874339cc68ebfd6d008` before the
planning edits in this session. Story 16.3 remains draft/backlog. No runtime
implementation, owner qualification, publication, commit or push is performed.

## Current entry result

All six exact tooling-v2 commands below returned **BLOCKED / exit 2** with
`ENTRY_QUALIFICATION_PENDING`. Each returned a two-row assertion ledger and
counts `{total: 0, passed: 0, failed: 0, skipped: 0}`. The implementation hold
remained ACTIVE and execution, owner-approval, release and push flags were false.
The runtime directory and both final-record paths remained absent. These are
fresh entry-gate observations; no native runtime test executed.

The selected contract is
[`epic-16-tooling-v2/story-contracts/16.3.json`](../planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json).
Its command array supplies the following exact invocations, run from the
repository root:

```bash
python3 _bmad/scripts/verify_story_16_3.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --scenario AC-16.3-01 --candidate HEAD --output artifacts/epic16-runtime/16.3/AC-16.3-01.json
python3 _bmad/scripts/verify_story_16_3.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --scenario AC-16.3-02 --candidate HEAD --output artifacts/epic16-runtime/16.3/AC-16.3-02.json
python3 _bmad/scripts/verify_story_16_3.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --scenario AC-16.3-03 --candidate HEAD --output artifacts/epic16-runtime/16.3/AC-16.3-03.json
python3 _bmad/scripts/verify_story_16_3.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --scenario AC-16.3-04 --candidate HEAD --output artifacts/epic16-runtime/16.3/AC-16.3-04.json
python3 _bmad/scripts/verify_story_16_3.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --scenario AC-16.3-05 --candidate HEAD --output artifacts/epic16-runtime/16.3/AC-16.3-05.json
python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/epic-16-tooling-v2/story-contracts/16.3.json --format bundle --output-json docs/release-evidence/story-16.3-final-record-v2.json --output-markdown docs/release-evidence/story-16.3-final-record-v2.md
```

`HEAD` resolved to the full revision above. Exit 2 is an entry blocker, not a
passed acceptance result. The selected verifier checks entry before native
measurement consumption and writes a receipt only on PASS; the generator also
stops before publication. Historical evidence-boundary reproduction and runtime
publication are **not-applicable to this planning refresh**.

## Entry facts and unresolved choices

- [Sprint status](sprint-status.yaml) still lists 16.1, 16.2 and 16.3 as backlog;
  no accepted Story 16.1/16.2 final-record pairs exist in `docs/release-evidence/`.
  Preserve `7.4, IR-0 -> 16.1 -> 16.2 -> 16.3`.
- Tooling-v2 `binding.json` has `entry: null`;
  `_bmad/scripts/epic16_trust_v2.py` has no trusted entries. In
  `_bmad/scripts/epic16_tooling.py`, `entry_evidence` requires ten PASS
  qualifications for 16.3: `platformTool`, `adoption`, `operationalEnvelope`,
  `providerParity`, `recoveryDrill`, `operatorCoverage`, `capacityWaiver`, `sm2`,
  `ownerEntry` and `appHostOwnership`. It independently checks trusted bytes,
  story identity, an ancestor authorization revision, explicit authority, and
  exact hashed predecessor files for 16.2. A chat choice alone cannot supply
  those qualification records.
- The [latest Platform handoff](../../module/platform-handoff.md) records actual
  4.38.1 artifact/source/CI/schema/catalog checks and 36 malformed-key controls
  passing. Adoption remains six PASS/sixteen FAIL; the Platform catalog mismatch
  is FAIL. Package preparation/action export and owner/native qualifications
  remain BLOCKED. The completed collector prerequisite supplies measurements,
  not runtime acceptance.
- [The operational envelope](../planning-artifacts/production-operational-envelope-v1.md)
  remains a draft for owner review. Architect/Runtime owns acceptance; Release
  alone accepts applicable capacity/parity waivers.
- `module/hosting-inventory.json` still freezes both AppHosts with pending
  replacement/retirement gates. The architecture's AppHost baseline decision
  requires the baseline owner with Architect/Runtime to record an exemption or
  relocation before Story 16.3 entry. Historical retention and a healthy local
  Debug topology do not satisfy that decision.

The draft therefore asks whether to resolve predecessors and qualification
first or scope a separate preparation plan with runtime held, and which
owner-reviewed AppHost disposition to pursue. No prerequisite implementation
scope or owner approval is inferred here.

## Runtime code map after qualified entry

| Path / symbol | Reuse or required action |
| --- | --- |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprDiagnostics.cs` | Shared prerequisite probing/redaction exists. Add bounded structured endpoint-readiness and effective control-plane collision diagnostics; neither required stable code exists here yet. |
| `references/Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Integration.Tests/DaprTestPrerequisiteDiagnosticsTests.cs` and `GeneratedApiSmokePreflightDiagnosticsTests.cs` | Existing owning regression suites; the old draft's `DaprDiagnosticsTests.cs` does not exist. Preserve current callers and test positive/negative classifications, cancellation and redaction. |
| `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostRuntimeBoundaryTest.cs` | Existing production-host/authentication/provenance patterns. Its current live assertion proves command rejection. Replace the private readiness helper's raw body/exception diagnostics with shared safe diagnostics. |
| `tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj` | Add the shared Testing.Integration dependency through Platform-selected source/package mapping. No existing reference supplies it. |
| `tests/Hexalith.Conversations.IntegrationTests/Projections/ConversationGatewayLiveFixture.cs` | Reuse isolated Dapr/Redis components and cleanup patterns. Its injected `AllowConfiguredTenantAccessService` and ten-minute retry interval cannot be copied unchanged as production admission/recovery proof. |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs`, `DomainProjectionDispatcher.ReconcileAsync` | Existing authenticated `POST /project/v2/reconcile`; preserve route and workload policy. |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/NamedProjectionDispatchCoordinator.cs`, `ReconcileTerminalRoutesAsync` | Production coordinator invokes the module route and prunes durable retry state after validated outcomes. Preserve ownership. |
| `IProjectionDeliveryRetryScheduler.ReadAsync` / `GetDueAsync`, `DaprProjectionDeliveryRetryScheduler` | Public durable pending-state observation seams. Observe actual hosted execution; `ProjectionDeliveryRetryWorker` and `RunOnceAsync` are internal and must not be reached through reflection. |
| `src/Hexalith.Conversations.Server/Projections/ConversationAsyncProjectionHandler.cs`, `ReconcileAsync` | Existing projection retry/terminal-state recovery; preserve contracts and adopt the accepted predecessor semantics. |
| `tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_3EvidenceTests.cs` | Missing contract-bound native source. Implement `Scenario01`–`Scenario05`, loading candidate-bound production products and emitting the existing strict native protocol. |

The effective AppHost ownership decision may relocate fixtures. Resolve it
before committing to runtime file paths. Each changed `.cs` file must retain
one type, the owning repository's formatting and required documentation.

## Existing evidence tooling and verification

`verify_story_16_3.py` already dispatches to shared `epic16_tooling.py`;
`generate_story_record.py` already selects tooling-v2. Reuse
`epic16_measurements.py`, `epic16_faults.py`, `epic16_execution.py`, native
capture/product checks and neutral contained-output helpers. Creating a second
runner or changing the historical final-record schema is unnecessary.

Existing checks cover safe exact diagnostics, effective endpoint inventories,
unrelated-port negative controls, durable pending/ack ordering, correlated
authenticated wire reconciliation, persisted query-visible state, duplicate and
concurrent progress, bounded cancellation, restoration and nonempty execution.
Pinned conformance controls use three attempts, 1000 ms readiness and 100 ms
controlled cancellation with endpoint IDs `conversations-http`,
`dapr-placement` and `dapr-scheduler`; these are controlled experiments, not an
accepted operational envelope.

Future focused tooling regression selectors are:

```bash
python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling_v2.py -k held_selected_v2_emits_no_runtime_outputs
python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py -k 'readiness or port_collision or structured_diagnostics or reconciliation or story163_entry or cancellation or round9_wire'
```

These selectors were identified, not executed in this planning refresh; their
isolated fixture results would not qualify runtime. Native validation after
entry needs Platform-materialized mappings, individual Debug project builds,
built xUnit `-class`/`-method` execution with `-failSkips`, and actual persisted
end-state. The six commands above consume bound measurements; they do not
replace running the native tests. Capture/binary inventories and final records
must agree byte-for-byte with the selected contract. Preserve all accepted
records and predecessor history.

No `.editorconfig` exists at this repository root or the inspected enclosing
project directories. Root `.gitattributes` preserves planning Markdown as LF;
`Directory.Build.props`, `tests/Directory.Build.props`, `global.json`, the
affected test project and mapped-validation documentation were inspected.
Planning edits preserve the frozen intent verbatim and leave sprint status,
runtime sources, bindings, trust pins and dependencies unchanged.
