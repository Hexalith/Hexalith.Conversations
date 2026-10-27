# Story 16.1 entry investigation — 2026-10-26

Investigated source candidate `109ef750a1ed0d1b02ba7efee8807d611fbad130`
on `main`. The working tree was clean before this investigation. This report
and its observation directory are additive planning material, not runtime
evidence, owner acceptance or a hold lift. Story 16.1 remains draft/backlog.

## Current state

The [prerequisite implementation spec](spec-epic-16-prerequisite-implementation.md)
is **done**. Its final review records 3,047 combined, 72 legacy and 43 focused
passing tests; those are retained prior results, not tests rerun here. Successor
generator dispatch and all fifteen measured scenario consumers now exist in
`_bmad/scripts/verify_story_16_{1,2,3}.py` and `epic16_tooling.py`. The old
runtime draft's unavailable-tooling tasks were therefore stale.

`_bmad-output/planning-artifacts/epic-16-tooling-v1/binding.json` still has
`entry: null`; `epic16_tooling.py` has empty `TRUSTED_ENTRIES`. Runtime entry
requires independently pinned owner evidence for `platformTool`, `adoption`,
`operationalEnvelope`, `providerParity`, `recoveryDrill`, `operatorCoverage`,
`capacityWaiver`, `sm2` and `ownerEntry`. It must identify this story, a full
ancestor authorization revision and the exact preserved predecessors. A draft
approval, fixture PASS, or self-nominated JSON decision cannot establish trust.

The retained [isolated qualification](../../module/validation/epic-16-prerequisite-implementation/qualification.json)
measured CLI `4.38.0+e171c5a788eab183b32683431b084198c9b8d373`:
ordinary validation passed, all 36 malformed-key controls rejected, and adoption
reported seven passing and seventeen failing rows. Overall qualification is
**BLOCKED**. The artifact is within Platform's accepted version range, but is
not the integration-qualified exact enrollment pin. Root and direct Platform
tool manifests still pin **4.35.2**. Release/source pairing, runtime review and
enrollment qualification remain required before changing those pins.

## Verification

[results.json](../../module/validation/epic-16-entry-2026-10-26/results.json)
retains all ten exact commands, exit codes, durations, separate raw stdout/stderr
paths and SHA-256 hashes. These commands inspect declarations and evidence;
they do not start the AppHost or execute a product runtime scenario.

| Command | Observed result |
| --- | --- |
| `dotnet tool run hexalith-module --version` | Exit 0; `4.35.2+4248b8c70cb35ba79fdd7e4dd63049d4c73211a0` |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` | Exit 1; `UsageOrManifest`; two HXP002 token-key diagnostics |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json --adoption --output json` | Exit 1; six PASS, 24 FAIL; static scope only |
| `uv run --frozen --no-sync python3 _bmad/scripts/verify_epic_16_adoption_v2.py --repository .` | Exit 1; FAIL, `SOURCE_BYTES_DRIFT` / `SOURCE_BINDING_INVALID`; entry BLOCKED |
| Five verifier commands plus final generator command from `epic-16-tooling-v1/story-contracts/16.1.json` | Each exit 2; BLOCKED / `ENTRY_QUALIFICATION_PENDING`; nonempty assertion ledger, zero measured tests; no runtime receipt or final pair written |

The fresh accepted-tool adoption report contains **30 rows**: six PASS and
24 FAIL, comprising one declaration, seventeen dependency-selector and six
Dapr findings. Six failures name newly tracked historical fixtures under
`module/validation/epic-16-prerequisite-implementation/`: three dependency
selectors in review-iteration-9 source-conventions snapshots; two copied
`ConversationProjectionReadModelKeys.cs` files in review-iteration-4; and one
EventStore domain-service snapshot in review-iteration-9. These six rows were
not present in the earlier eighteen-failure inventory. Their source and scanner
scope need owner disposition; preserve the retained fixture bytes. The remaining
eighteen are the prior declaration, fourteen selector and three Dapr findings.
This current 4.35.2 observation must not be represented as a fresh 4.38.0 run.

The preserved v2 checker reports four source mismatches: Platform `README.md`,
Platform `src/Hexalith.Platform.Composition/PlatformAppModel.cs`, root
`_bmad/scripts/generate_story_record.py`, and EventStore
`src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs`.
Its old unavailable-generator/verifier diagnostics describe the frozen v2
package; the additive tooling package supplies the new consumers without
rewriting that historical binding. This FAIL is not a PASS or an exemption.

Runtime evidence-boundary publication is **not-applicable** because this
investigation publishes no runtime receipt/final record. This does not turn
entry BLOCKED or the historical structural FAIL into not-applicable.

## Code map and implementation order after entry

1. EventStore Client `Subscriptions/EventStoreDomainEventProcessor.cs` validates
   legacy metadata, raw identifiers and non-null payload before marker acquisition.
   Event deserialization and exact payload/aggregate binding still follow the
   deduplication path. Preserve the existing early checks while extending the
   generic immutable binding and durable outcome protocol. Reuse
   `Projections/IReadModelStore.cs`, `ReadModelWritePolicy.cs`,
   `SharedProjectionEpochCoordinator.cs` and
   `Streams/IAuthoritativeEventStreamReader.cs`; the epoch coordinator alone does
   not implement AD-6's pending fingerprints/horizon/final-CAS protocol.
2. Tenants Client `Projections/ITenantProjectionStore.cs` exposes unconditional
   `SaveAsync`; `Handlers/TenantProjectionEventHandler.cs` uses per-process
   semaphores and a load/apply/save sequence. Add coherent durable snapshots and
   pure transitions through EventStore's mutation/recovery substrate. Preserve
   existing signatures and `TenantLocalState.Clone()` semantics. The existing
   registration overload accepts `Action<EventStoreDomainEventsOptions>`.
3. Conversations Server `TenantAccess/ConversationTenantAccessService.cs`
   adapts state and health separately through Commons' evaluator.
   `DefaultConversationTenantProjectionSignal.cs` synthesizes Healthy when the
   store supplies no signal. Add `ConversationTenantProjectionSnapshotAdapter.cs`
   and consume one durable state/health/checkpoint snapshot per invocation;
   missing evidence must deny. Bind injected Tenants pub/sub and topic options
   through the existing registration overload. Keep `Program.cs`'s restart and
   single-replica disclosure until actual persisted-state proofs pass.
4. Reuse owner Client tests and Conversations
   `tests/Hexalith.Conversations.Server.Tests/TenantAccess/`. Add the exact native
   source required by the successor contract:
   `tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_1EvidenceTests.cs`.
   The old draft's `IntegrationTests/TenantAccess/` directory does not exist.
   Capture untouched native TRX and loaded product identities; assert persisted
   contents/checkpoints, recovery fences and durable acknowledgement ordering.

This is a cross-repository runtime feature, not a oneshot edit. EventStore owns
generic durability/recovery; Tenants owns transitions; Conversations consumes
their public seams. The footprint includes those three owners, focused tests
and source-bound runtime evidence. No deployment, destructive migration or
external data mutation is part of the planned story. Obtain owner authority and
inspect each repository's effective guidance before modifying its sources.

## Remaining entry work

- Scope Platform enrollment/adoption remediation separately: tool-mapped
  project/package roots, injected Dapr bindings, SDK Data Protection/declarative
  subscription qualification and owner disposition of scanner findings. Preserve
  public behavior, accepted records and frozen hosting inventory.
- Architect/Runtime must qualify
  `_bmad-output/planning-artifacts/production-operational-envelope-v1.md`, which
  remains an owner-review draft. Bind provider/environment identities, parity,
  recovery drills, named operator/deputy coverage, telemetry, resource bounds and
  Release-owned capacity/waiver disposition to measured evidence.
- `module/sm-2-checklist.md` remains pending throughout. A real active-checkout
  breakpoint, observed edit/runtime effect and Story 3.11 owner acceptance require
  human evidence. Automated static checks cannot supply them.
- After qualifications pass, obtain the separately scoped story-entry decision
  and reviewed consumer trust/source bindings. Keep actual entry null and trust
  empty until then. Story 16.3's AppHost exemption/relocation remains a separate
  later gate; completing prerequisite tooling does not complete Story 16.1.

The unresolved choice is whether to prepare that separate remediation scope now,
or validate supplied owner-qualified evidence and entry records. Neither option
permits substituting an approval message for absent qualification evidence.

## Scope and preservation checks

Only the runtime draft, this investigation and the fresh observation directory
are changed. Frozen intent, sprint status, runtime sources, tool pins, dependency
gitlinks/checkouts, accepted prerequisite packages and historical records remain
unchanged. No staging, commit, push, dependency initialization or runtime launch
was performed. Final local checks compare the frozen block with HEAD, recompute
observation hashes, verify held outputs remain absent and inspect whitespace.
