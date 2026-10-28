# Story 16.1 entry investigation — 2026-10-28

Investigated Conversations `main` at
`ef01d578a00efd527bae9226dca302f54cd012f0`, tree
`912197eb1e492690bf1a6784335f05c2a693011c`. The user pushed the prior changes;
the repository was clean on resumption. Epic context was regenerated before
command capture, so the observation's entry status truthfully includes that
planning edit. This report and its observations are planning material. Runtime
implementation remains held and Story 16.1 remains draft/backlog.

## Current prerequisites

The committed [AppHost retry](../../module/validation/epic-16-tooling-qualification-v2/attempt-20261028-apphost-baseline-02/README.md)
records successful mapped Development/Debug resource readiness, followed by
exact-target stop and an empty session list. It clears the earlier launch
blocker. All 54 retained command-stream sizes and hashes recompute. The baseline
executed no producer qualification or runtime tests; its 39 warnings and two
errors remain disclosed. Readiness does not establish functional correctness,
durable tenant access, production parity, artifact qualification or SM-2.
This investigation did not start another AppHost.

The [tooling prerequisite spec](spec-epic-16-tooling-qualification-and-binding-refresh.md)
is in-progress; its implementation notes record prior approval and a stop
before execution. Its earlier failed-baseline note is historical. Neither
`_bmad-output/planning-artifacts/epic-16-tooling-v2/` nor
`_bmad/scripts/epic16_packages.py` exists. Producer qualification tests, successor
package selection and its fault/regression matrix remain unexecuted here.
The prerequisite remains distinct from runtime authorization.

Root `.config/dotnet-tools.json` now pins **4.38.1**; direct Platform still pins
**4.35.2**. This difference was already committed before this investigation.
The installed root binary reports
`4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`. Ordinary declaration
validation now passes; the previous HXP002 failure is historical. This local
result does not accept a published release/source/catalog tuple or align pins.

## Verification

[results.json](../../module/validation/epic-16-entry-2026-10-28/results.json)
retains all ten exact argument lists, working directories, timestamps, bounded
durations, exits, separate stdout/stderr sizes and SHA-256 hashes, full candidate
and tree, raw root gitlinks and checkout identities. Each command had a
60-second timeout and an 8 MiB output bound; none reached a bound. Capture reused
the retained baseline driver's bounded process and preservation helpers with a
fresh output directory. Earlier evidence was not rewritten.

| Command | Current result |
| --- | --- |
| `dotnet tool run hexalith-module --version` | Exit 0; exact 4.38.1 identity above |
| `dotnet tool run hexalith-module -- validate --manifest module/hexalith.module-manifest.v2.json` | Exit 0; `validated: Success` |
| Same validation with `--adoption --output json` | Exit 1; HXA001; six PASS, twelve FAIL |
| Same validation with `--adoption` | Exit 1; the same eighteen ordered rows |
| Five verifier commands and final generator command from `epic-16-tooling-v1/story-contracts/16.1.json` | Each FAIL/exit 1/SOURCE_DRIFT on `.config/dotnet-tools.json`; one failing assertion, zero measured tests |

The six story commands were executed exactly as declared, including their
contracted output paths. Every failure left all five runtime receipt paths and
both final-record paths absent. No runtime or producer qualification test ran.

The current adoption inventory has one tool-pin failure, eight
dependency-selector failures and three Dapr failures. Relative to the
[2026-10-27 observation](story-16-1-entry-investigation-2026-10-27.md), declaration
failure is removed and these three failures are added:

- `.config/dotnet-tools.json`: root 4.38.1 differs from Platform's accepted pin.
- `module/validation/epic-16-tooling-qualification-v2/attempt-20261028-apphost-baseline-02/prepared-imports/source-mapping/SourceMapping.late.props`.
- `module/validation/epic-16-tooling-qualification-v2/attempt-20261028-apphost-baseline-02/prepared-imports/source-mapping/SourceMapping.values.props`.

The last two findings concern retained producer-generated imports, not newly
hand-authored dependency selectors. Keep their bytes and record their scanner
disposition through separately reviewed work. The other nine failing identities
are unchanged: six retained source captures, the accepted MinimalModule fixture
and two frozen host/direct-consumer projects. Do not remove archives, exclude
paths or change accepted fixtures merely to obtain a passing scan.

Recomputing every v1 binding row finds exactly two mismatches:

| Source | Accepted SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `.config/dotnet-tools.json` | `5d72a022a9416cb753da75cda1b0f1945fd938c065521fd9946aaa525fe048da` | `5b826b67ce6520bba1fa80ef3982e56cc71b38d7ced437c0aaf5b1697339b411` |
| `.gitattributes` | `d743202ab8c0bdf3dcd000a4a101acde352b6e6cf305a49b708b23c6e7029d7d` | `d7b349e3620e883fcc66000a53e614c1ec331a45024cab5189d9943b51a5ef4c` |

The first mismatch stops command evaluation before the second and before
`entry_evidence`. Preserve both legitimate current files and accepted v1
bindings. The prerequisite plan's old expectation of a first failure on
`.gitattributes` needs reconciliation with this current ordering; no v1 digest
was changed here.

## Runtime code map after qualified entry

- EventStore Client `Subscriptions/EventStoreDomainEventProcessor.cs` already
  validates raw identifiers and a non-null payload before marker acquisition;
  event deserialization and exact payload/aggregate binding follow it. Reuse
  `Projections/IReadModelStore.cs`, `ReadModelWritePolicy.cs`,
  `SharedProjectionEpochCoordinator.cs` and
  `Streams/IAuthoritativeEventStreamReader.cs`. Add generic durable mutation,
  pending-fingerprint/horizon, recovery-fence and acknowledgement semantics in
  that owning repository; existing primitives alone do not establish AD-6.
- Tenants Client `Projections/ITenantProjectionStore.cs` exposes unconditional
  `SaveAsync`; default registration selects `InMemoryTenantProjectionStore`.
  `Handlers/TenantProjectionEventHandler.cs` still uses a process-local
  semaphore and load/apply/save, and ignores older sequences without history
  verification. Add durable coherent snapshots and pure transitions through
  EventStore, preserving existing signatures and clone semantics.
- Conversations Server `TenantAccess/ConversationTenantAccessService.cs`
  obtains state and health separately; `DefaultConversationTenantProjectionSignal.cs`
  synthesizes Healthy when the store lacks a signal. Add the missing
  `ConversationTenantProjectionSnapshotAdapter.cs` and evaluate one persisted
  snapshot per invocation. Preserve the already configured Tenants delivery
  options and required declarative subscription checks in
  `ConversationTenantAccessServiceCollectionExtensions.cs`. Retain Program.cs's
  restart/single-replica disclosure until actual proofs pass.
- Reuse owner Client tests and Conversations
  `tests/Hexalith.Conversations.Server.Tests/TenantAccess/`; add the missing
  `tests/Hexalith.Conversations.IntegrationTests/Epic16/Story16_1EvidenceTests.cs`
  only after entry. Prove persisted end-state, concurrent recovery and durable
  acknowledgements using untouched native TRX and loaded-product captures.

This remains one cross-repository runtime goal. No deployment, destructive
migration, external data mutation or publication is proposed by the draft.

## Remaining owner gates and next scope

The binding retains `entry: null`; the consuming verifier retains
`TRUSTED_ENTRIES = {}`. Required story-scoped qualifications are `platformTool`,
`adoption`, `operationalEnvelope`, `providerParity`, `recoveryDrill`,
`operatorCoverage`, `capacityWaiver`, `sm2` and `ownerEntry`, plus compatible
predecessor records and an ancestor authorization revision. Neither local
readiness nor a new approval message can manufacture these facts.

`production-operational-envelope-v1.md` remains an owner-review draft;
`module/sm-2-checklist.md` remains pending. Architect/Runtime and Release must
qualify provider/environment parity, recovery/operator coverage and capacity
disposition. A human must supply actual active-checkout breakpoint/edit and
SM-2 acceptance evidence.

The next scope choice is to resume the already approved tooling prerequisite,
reconciling its measured current pin/scanner/source-binding facts, or validate
supplied qualified owner records. Completing that prerequisite still supplies
no runtime entry by itself. No additional approval of that already approved
spec is requested by this investigation.

## Preservation and dispositions

Only the runtime draft's unfrozen planning sections, regenerated epic context,
this report and the fresh observation directory changed. Frozen intent, sprint
status, runtime code, tool pins, raw root gitlinks/checkouts, accepted bindings,
previous reports and index were preserved. No staging, commit, push, dependency
initialization/update or runtime lifecycle transition occurred.

Recorded results remain distinct: declaration **PASS**, adoption and six v1
commands **FAIL**, runtime entry **BLOCKED**, runtime evidence publication
**not-applicable**. A FAIL or BLOCKED result halts runtime review/completion.
Focused planning checks recompute all twenty fresh command-stream hashes,
compare the eighteen adoption rows exactly between formats, verify the six
nonempty failure ledgers and seven absent outputs, and compare protected state
and frozen intent. Native product tests and historical evidence-boundary replay
do not apply to these planning edits.
