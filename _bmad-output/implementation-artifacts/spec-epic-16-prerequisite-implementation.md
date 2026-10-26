---
title: 'Qualify corrected Platform tooling and implement Epic 16 successor evidence consumption'
type: 'feature'
created: '2026-10-25'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 12
review_loop_limit_override: 'user approved continuing past the five-iteration limit'
baseline_commit: '8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/story-16-1-entry-investigation-2026-10-25.md'
---

<frozen-after-approval reason="user explicitly authorized prerequisite implementation on 2026-10-25">

## Intent

Enable truthful Epic 16 prerequisite qualification: evaluate a corrected published Module CLI through Platform's acceptance requirements, and implement successor contract/generator consumption and all fifteen measured scenario verifiers. The user answered “I authorize” to corrected CLI qualification followed by successor tooling, retaining Story 16.1's runtime hold.

## Boundaries & Constraints

Preserve every prerequisite/adoption v1/v2 package, schema, checker, receipt and accepted record; publish additive successor inputs and observations. Keep all eighteen AC identities, thirty-two requirements, graph edges, rollback boundaries and completion guards. Use neutral contained reads, strict JSON, independent trust pins, exact inventories and raw Git facts. Derive results/counts/candidate/digests from measured inputs. Keep PASS/FAIL/BLOCKED distinct and nonempty; unavailable prerequisites cannot pass.

This authorization permits prerequisite code/tests/docs and isolated tool qualification. It does not authorize Conversations/EventStore/Tenants runtime implementation, live deployments, runtime hold lift, operator acceptance, synthetic evidence, host retirement, staging/commits/pushes, Git remote operations, dependency checkout updates or nested initialization. Preserve user changes. Work in the owning repository for any upstream fix and inspect its guidance first. No skill inside references may be loaded. Download/restore of the chosen tool into an isolated temporary directory for qualification is permitted; accepted pins align only after their existing qualification requirements pass. Never claim human-only evidence or another owner's acceptance from automated checks.

## I/O & Edge-Case Matrix

| Input | Required behavior |
| --- | --- |
| Corrected published tool | Verify actual installed version/artifact/source identities, ordinary and both adoption results; retain all native failures |
| Unqualified tool or pending owner evidence | Keep accepted pins and runtime hold; name unmet qualification precisely |
| Preserved successor obligations plus bound measured native evidence | Validate exact scenario identity and full candidate/source/test bindings; derive outcome/counts; deterministic final JSON/Markdown |
| Missing native tests/measurements/entry qualification | BLOCKED with stable reason; no completed runtime record |
| Altered contract, inventory, identity, fingerprint, receipt or source | FAIL; no normalization that hides drift |
| Forged PASS/counts, zero/duplicate/unrun/skipped tests, stale candidate | Reject; all required native results must execute and pass |
| Escaping/symlinked/nonregular input or aliased/protected output | Reject safely; preserve source and accepted evidence bytes |
| Existing V9/V14 and current completion routes | Preserve effective behavior; historical reproduction uses exact retained fixtures |

</frozen-after-approval>

## Current execution instruction

The user answered **yes** to continuing beyond the five-iteration review limit.
That approval overrides the limit for this continuing prerequisite build; do not
request the same authorization again merely because the counter exceeds five.
The earlier halt/disposition below is historical. Continue iteration 12 under the
latest amendment below, then verify and review normally. The frozen scope, runtime hold,
no-push rules and independent evidence requirements are unchanged. This is a
worker handoff from the parent's existing rendered workflow: do not invoke or
render another skill, spawn implementation workers, or run completion steps.

## Code Map

The entry investigation provides current paths and gaps. Relevant local source:

- `references/Hexalith.Builds/docs/module-cli-configuration-key-released-artifact-handoff.md` and `module-cli-configuration-key-release-review.md`: actual 4.38.0 artifact fixes token schema; runtime findings remain. Current Builds main includes later runtime corrections. Inspect those before deciding what remains; never assume they qualify the published package.
- Platform `.config/dotnet-tools.json`, `hexalith-platform.json`, `README.md`, `docs/domain-module-enrollment.md`: accepted 4.35.2 and qualification policy. Root `.config/dotnet-tools.json` matches. Existing pins remain until actual qualification allows alignment.
- `_bmad-output/planning-artifacts/epic-16-adoption-v2/`: immutable wrapper contracts, preservedContract, futureCommands, graph/inventories. `_bmad/scripts/verify_epic_16_adoption_v2.py` pins generator bytes. Create additive successor bindings; do not weaken old pins to make current results green.
- `_bmad/scripts/generate_story_record.py`: V9/V14 validation, `v2_generate`, inserted-record verification, neutral Git/TRX/pair/writer helpers. Add narrow successor dispatch using a separate cohesive module rather than duplicating general infrastructure.
- `_bmad/scripts/contained_reader.py`, `evidence_boundary_current.py` and existing record/schema helpers: reuse secure reads, strict parsing, raw gitlinks, canonical digests and contained output writing.
- `_bmad/scripts/tests/test_generate_story_record.py`, `test_verify_epic_16_adoption_v2.py`: regression patterns. The latter reads live root generator bytes into pinned historical fixtures; preserve its original fault assertions by adding exact retained fixtures if necessary.

### Review iteration 1 — evidence relationships and native interoperability

The three independent reviewers found that the initial consumer sometimes verified
self-consistent labels without binding the underlying measured state. This is a
`bad_spec` loopback: the intent already requires all preserved runtime semantics,
but the non-frozen implementation requirements did not describe the necessary
measurement relationships. No new user decision or runtime authorization is needed.

Re-derive the implementation from the amended requirements below. Only this task's
implementation files were reverted; qualification observations, investigation,
specs and handoff remain. The inspected prior tree is retained at
`/tmp/epic16-review1-preservation-01sh8y5i` (manifest.json lists its files). Use it
as a preservation reference, not as an accepted implementation. Restore the sound
parts where they satisfy these instructions, then replace the defective protocols.

**KEEP instructions extracted before re-derivation:** preserve the isolated native
CLI qualification and its 36 controls, accepted pins and runtime hold; all 18
scenarios, 32 requirements, 38 graph nodes and 61 edges; exact historical fixtures
and predecessor pins; strict JSON/closed schema/contained reads; PASS/FAIL/BLOCKED
separation; literal false authority and independent trust; source/Git/dependency
binding; deterministic receipts/pairs and atomic publication/lifecycle checks;
standalone lazy V9/V14 dispatch; the 328 passing prior focused assertions, 72 legacy
and 21 AD-8 selectors where behavior remains valid. Keep the two independently
established broad baseline failures visible. Do not rerun the 39-minute broad lane:
rerun affected tests, workflow guards and held commands on the final source.

The following concrete requirements replace the incomplete implementation choices:

1. Entry authorization must be constructible without a candidate/hash cycle. A
   separately reviewed, independently pinned entry approves a pre-existing full
   `authorizationRevision` that must be an ancestor of the candidate. It authorizes
   starting implementation from qualified prerequisites; candidate/source/native
   bindings independently identify the actual implementation. The entry and its
   trust pin can therefore be committed in a later descendant without hashing
   themselves. Exact story, qualification/predecessor inventories and literal
   approvals remain required. Test a real disposable commit graph, wrong ancestry,
   and wrong story. Keep the actual binding entry null and trusted entries empty.
2. Native TRX supports both repository-relative and normal repository-contained
   absolute codeBase paths. Convert absolute paths lexically, then use the existing
   no-follow reader; never resolve symlinks. Reject outside paths, dot traversal,
   symlinks and nonregular files. A source-stamped binary must be an actual managed
   test assembly, with its required type/method and informational/source revision
   verified from managed metadata, not arbitrary regex substrings. Reuse existing
   metadata tooling if suitable, otherwise add a small cohesive non-executing
   metadata reader/helper; never load or execute evidence assemblies. Include a
   genuine compiled disposable assembly success plus malformed/text-file failures.
   Reject every unrecognized or extra TRX result node, including GenericTestResult.
3. Persisted restart snapshots with pending work must be Gapped and deny access;
   every pending sequence must be future to expectedSequence, not already applied.
   The completion CAS tuple must match measured recovery lease/version, recovered
   checkpoint and enlarged horizon, not just equal a second supplied tuple. Cover
   reverse-save convergence and nonregression for disable, membership, role and
   configuration schedules, with measured before/after state and decisions on both
   replicas; every required schedule must be present exactly once.
4. Replay JSON must pass strict parsing and actual EventStore canonicalization,
   not an assertion that two strings match. Reuse the pinned EventStore source in
   a safe, fixed helper/oracle or another independently verified equivalent that
   correctly preserves its UTF-16 property order, Web escaping and number tokens.
   Do not replace it with Python canonical JSON. Bind the supplied collection keys
   to the actual serialized collections. Reject duplicate keys, NaN/infinity,
   whitespace/noncanonical property order, reversed arrays and altered golden
   vectors. Preserve actual source hashing and Unicode/HTML/number coverage.
   The default Python lane must not require initialized submodules: retain an
   exact pinned canonicalizer source fixture for tooling tests/helper compilation
   (including source attribution); actual runtime acceptance separately hashes
   the clean direct EventStore checkout. No default test may read references/.
5. Query observations must include the measured control-index inputs: version,
   presence/provider state, lifecycle, generation consistency, source gaps,
   authorization/existence, surviving details, dispatch and rebuild state. Derive
   the expected mapping from these inputs and verify the named case actually
   represents them. Include the preserved lifecycle transitions and their causes:
   only TenantCreated+Absent creates Ready(empty), Absent/v1 rebuilds, Ready erases
   through Erasing/Erased, and only explicit owner-authorized rebuild can leave
   Erased via Rebuilding. Current-empty requires proven v2 Ready and no pending
   dispatch; labels alone cannot establish proof.
6. Erase observations must identify deliveries during both Erasing and Erased,
   authoritative non-payload reference identities and their durable-write/ack
   ordering, barrier/deletion targets, and unchanged derived-content absence.
   Rebuild observations identify each authoritative tenant-scoped stream, its
   boundary at B and replay through the captured C. Exact unique stream inventory
   equals the manifest; missing/extra/foreign/duplicate streams fail. Verify
   exact-next via length and enumeration of the supplied list, never allocating
   a range controlled by a potentially huge end integer. Retain all generation,
   queued-after-C, same-generation progress and nonregression guards.
7. Live first-failure and reconciliation observations bind the same pending-item
   ID, tenant, runtime ID and catalog digest. Re-evaluate prerequisite evidence and
   compare these identities before accepting clearance. Add regression tests for
   each dependency edge with missing and semantically invalid prerequisites;
   dependent scenarios must propagate BLOCKED/FAIL.
8. Exercise the actual successful verifier and generator entrypoints in fixture
   scope, including receipt file bytes and successful generator consumption, for
   all three stories. A no-op receipt writer and disabled dependency loop must
   make their targeted tests fail. Fixture success must never imply runtime entry.

These changes add no runtime implementation or actual owner decisions. All native
observations used in tests are explicitly disposable fixtures. The actual held
checkout must still return nonempty ENTRY_QUALIFICATION_PENDING for all 18 commands.

### Review iteration 2 — complete persisted observations and runtime-path enforcement

The second independent review found two remaining protocol omissions: ordered
commit/ack observations and persisted Healthy authorized/denied restart cases.
These are already required by the preserved intent, so this is a second bad_spec
loopback, with no change to the frozen authorization. Re-derive task-owned code
using `/tmp/epic16-review2-preservation-9oo338dt/manifest.json` and its file tree as
the preservation reference. That snapshot contains the 437/72 passing version.

**KEEP before reversion:** retain every prior KEEP item and iteration-1 correction,
including actual EventStore canonicalization, metadata-only inspection, ancestor
entry authorization, all four convergence schedules, lifecycle causes, exact
B/C stream inventories, same-item reconciliation correlation, real CLI fixture
publication, no submodule reads in routine tests, strict finite JSON and 18 actual
held commands. Preserve all existing mutation regressions and source attribution.
Qualification and evidence observations remain in place. Do not run the broad lane
again; its independently established baseline failures remain reported.

Complete these non-frozen implementation requirements:

1. Each pending fingerprint in a merge must be within the merged recovery horizon;
   an extra observed future sequence must extend the horizon and cannot be cleared
   at Healthy before recovery reaches it. Rebuild cutover watermark must cover C,
   not merely be nondecreasing. Keep per-stream B/C and exact-next validation.
2. Make the native replay-state test protocol explicit and closed: canonical bytes
   represent version 2, the measured replay anchor in UTC ticks, and the ordered
   collection identities. Require the anchor inside the hashed state to equal the
   maximum validated contributing event/lifecycle time. Reject unknown/provider
   wrapper fields, misplaced operational metadata and wrong versions. Update the
   typed positive fixture and Unicode cases without weakening the actual oracle.
3. Bind the corrected reconciliation state to the measured recovered checkpoint:
   the protocol's persisted/query projection sequence must equal afterCheckpoint
   and not regress beforeCheckpoint or the measured pending item's sequence.
   Carry the pending sequence from first-failure evidence through correlation.
   Test stale-but-equal persisted/query objects and wrong pending sequence.
4. Tenant acknowledgement rows identify their delivery and durable outcome record,
   with correlated durable-commit and acknowledgement timestamps/order. All four
   acknowledging outcomes require durable commit before acknowledgement. Provider
   failure/retry exhaustion must record no acknowledgement. A pair of true booleans
   is insufficient. Include reordering and mismatched identity mutations.
5. AC-16.1-02 must also contain actual before/after restart observations for Healthy
   authorized and Healthy denied decisions, with the same persisted sequence and
   freshness metadata and process identities, no redelivery, and consumer-observed
   decisions. Retain the separate Gapped restart proof. Bind freshness and sequence
   to each case; test mutation of either post-restart metadata or decision.
6. Validate representable UTC tick ranges (1..DateTime.MaxValue.Ticks) in authoritative
   time/replay observations and ports 1..65535 in endpoint inventories. Convert
   helper temporary-directory/source-copy OSError into HelperUnavailable so runtime
   evaluation emits structured BLOCKED, and test each reachable setup failure.
7. Metadata inspection must reject locally defined Xunit.FactAttribute lookalikes.
   Resolve the attribute's declaring assembly identity to the repository's actual
   pinned xUnit test framework, and require the selected public nonabstract test
   class/method to satisfy this protocol's discoverable shape. Build the positive
   disposable fixture against the genuine pinned framework, never executing the
   evidence assembly. Include local fake attribute, wrong assembly identity and
   invisible/non-test method failures. Do not change repository dependency pins.
   Accept genuine async Task/ValueTask Fact methods and public fixture constructors
   used by this repository; do not mistake a synchronous/parameterless-only
   surrogate for xUnit discoverability. Retain nonpublic/abstract/non-test guards
   and compiled positive asynchronous/fixture-constructor cases.
8. Add an isolated test-only qualified runtime fixture that reaches evaluate(),
   not just individual helpers. Actual root entry stays null/untrusted. Exercise
   entry ancestry, direct dependency cleanliness, native binary admission and real
   canonicalizer-source verification together, including valid bound binary plus
   absent/malformed/wrong-revision failures. Removing the native_binary call must
   fail targeted tests; also prove dependency/source guard invocation. No runtime
   records or acceptance may be written outside disposable test repositories.
9. Before claiming source identities, verify that the actual executing consumer,
   its loaded local dependencies and native-helper source bytes equal the bound
   sources in --repository. A clean candidate checkout with different bound bytes
   from the caller must fail, rather than reporting identities of unexecuted code.
   Keep portable fixture repositories usable when their source bytes are identical.
   Include separate source-tree drift tests for consumer and helper. Capture
   source snapshots without letting missing/unsafe helper files throw during
   module import: a partial tooling installation must emit the same structured
   failure/BLOCKED protocol. Parent reproduced missing Program.cs traceback with
   empty stdout in /tmp/epic16-import-probe-lpnzfd8e; add that subprocess regression.

### Review iteration 3 — domain-value evidence and real native-runner integration

The third review identified remaining label-only proofs. Re-derive task-owned code
from `/tmp/epic16-review3-preservation-zn15hb3l/manifest.json`, using the preserved
548-pass implementation as a reference. This bad_spec loopback completes the same
approved evidence-consumption intent; it does not authorize product runtime work.

**KEEP before reversion:** all prior KEEP items, 548 focused assertions, 72 legacy
checks, 44 metadata/runtime checks, all nineteen demonstrated guard mutations,
actual pinned xUnit identity, async/fixture constructors, source attribution and
partial-installation diagnostics, all preserved identities/graphs, the null actual
entry, accepted pins and runtime hold. Preserve native qualification and every
recorded prior result. The 39-minute broad baseline must not be rerun or weakened.

The amended requirements below supersede the earlier simplified measurement shapes
where necessary, while retaining their version, anchor, ordering and safety guards:

1. Align native identity with untouched output from the actual pinned xUnit/VSTest
   runner: fully qualified test class/method in testName, AC identity in the bound
   JSON payload. The existing real reproduction at
   `/tmp/epic16-review-native-q1h3dqpn/native-full-payload.trx` passes the runner and
   metadata inspector but fails this consumer's AC-name expectation. Add an actual
   disposable compiled test execution that emits the complete payload and consumes
   its untouched TRX through the consumer, rather than synthesizing all native XML.
   Use the repository's pinned framework/runner packages; no repository pin changes.
2. Reject DTD/entities with an encoding-aware parser before handing original TRX
   bytes to the shared reader; ASCII scanning is insufficient for UTF-16. Unknown
   encodings and other parser lookup failures must produce structured NATIVE_INVALID.
   Include UTF-16 entity declarations and unknown-encoding CLI/evaluator regressions.
   Resolve candidate refs once at generator entry, use that immutable commit for all
   five evaluations, and assert every receipt/scenario has that exact candidate.
   Simulate HEAD advancement and prevent a record combining candidate identities.
3. Replay-state items must contain deterministic domain values, not only keys. Use
   actual Conversations event/read-model semantics for a representative controlled
   conversation with participants and file references. Include all measured values
   in canonical v2 bytes, derive the unique ordered identity list from those values,
   and compare both rebuilds against expected state derived from the retained
   authoritative event sequence. Keep version/anchor and exclusion of operational
   metadata. A change to content, participant role or file metadata with unchanged
   IDs must change the hash; divergent native content cannot pass on key equality.
   Reference source: ConversationProjectionSnapshot, ConversationProjectionAccumulator,
   ConversationDetailProjectionV1 and the actual Contracts/Events definitions.
4. Retain each authoritative envelope for selected identity/time and compare every
   duplicate-binding component (tenant, conversation, event type, positive sequence,
   known serialization format, raw payload hash and selected UTC ticks) against it.
   Validate actual event metadata/payload shape for the controlled protocol. An
   unrelated identity or unknown codec cannot establish a full-binding proof.
5. Tenant convergence carries contiguous authoritative replay records from checkpoint
   through the enlarged horizon. Compare every pending message/type/payload fingerprint
   with the corresponding authoritative record, and derive recovered access state
   from those records using the actual Tenants event semantics. Include the immutable
   starting state and reject missing/conflicting/relabelled records and inconsistent
   final decisions. This is a test/evidence oracle, never a second runtime service.
   Read the real TenantReadModel and event definitions; retain any required local
   fixtures with attribution so default tests still need no initialized submodules.
6. Reconciliation includes corrected domain values as well as sequence. Correlate
   the authoritative pending event/full binding with first-failure evidence, apply
   the controlled event to its retained preceding state, and compare that expected
   state with both persisted and production-query observations. A changed checkpoint
   or mutually equal stale content must not prove recovery. Preserve newer progress,
   identity/time, tenant, generation, route and catalog checks.
7. Derive public-contract preservation from retained source/API evidence. Use an
   existing repository API inventory mechanism where suitable; otherwise a complete,
   conservative tracked Contracts source inventory is acceptable, clearly described
   as source preservation rather than a claim of binary compatibility. Read baseline
   and candidate blobs independently, require exact inventories, derive digests and
   compare native claims to them. Do not trust two matching supplied digest strings.
   Cover before/candidate surface drift and forged matching hashes.
8. Bound readiness/cancellation to independently pinned test-control policy, not
   limits nominated by observations. Preserve the existing positive experiment's
   settings as conformance controls (not production operational approval), put them
   in trusted contract data, and compare actual configuration/measurement to them.
   Port evidence records the effective endpoint inventory, ownership and occupancy
   before injection, during collision and after cleanup for each affected binding;
   preserve unrelated owners. A snapshot unrelated to occupiedPort cannot prove
   restoration. Bind endpoint IDs to this inventory and inspect retained structured
   emitted diagnostics against allowed safe fields and stable diagnostic content;
   empty diagnosticPayloadFields is not sufficient. Reject injected tenant/token
   strings and extra diagnostic fields, while retaining actual native observations.
9. Replace fault labels plus a positive number with typed fault-specific injected
   conditions, target identities, measured outcomes and restoration observations.
   Cover all twelve tenant and eight live fault IDs with their actual distinguishing
   conditions (e.g. reversed versions, equal-sequence differing fingerprints, three
   future deliveries, expanded horizon, three exhausted attempts, expired fence,
   missing/conflicting history, unavailable provider, successful contiguous recovery,
   replica equality; removed route, fingerprint mismatch, duplicate clearance,
   endpoint failure, occupied owned port, unavailable Dapr, zero selected tests and
   unauthorized request). Validate those facts, retain assertion entries, and derive
   assertion counts from validated entries. Verify restoration against retained
   before/after bytes or occupancy for the named target. Arbitrary common bytes and
   assertions=1 must not satisfy every fault. Reuse existing scenario predicates and
   correlated evidence where possible, rather than adding a runtime implementation.

Do not require different completion timestamps just because contextual rebuild-clock
observations differ. The preserved AD-8 requirement explicitly tests freshness by
semantic invariants, not timestamp inequality: genuinely coincident completion times
are valid if each measured completion/anchor/lag relationship is correct.

Audit the complete fifteen-scenario preservation matrix after these changes and
exercise mutations of domain values, identities, event traces and actual diagnostics,
not merely the existing labels. Keep all successful consumer/CLI paths and existing
negative boundaries covered. Native fixtures remain visibly synthetic and cannot
qualify the actual held checkout. No required runtime or human approval is invented.

### Review iteration 4 — isolation, conflict, redaction and emitted evidence

The fourth review verified omitted measurement relationships for obligations
already present in frozen intent. This is a bad_spec loopback with no new runtime
authority or user decision. The inspected implementation is preserved at
`/tmp/epic16-review4-preservation-5vyw5_4m`; its manifest identifies every reverted
task-owned file and hash. Restore sound parts and re-derive the protocols below.
Qualification, historical observations, specs and handoff remain.

**KEEP before reversion:** retain all prior KEEP items, the 752 passing focused
cases (590 successor plus 162 historical), 72 legacy checks, 26 effective mutation
probes, genuine untouched xUnit TRX and Contracts wire goldens, complete domain
replay/reconciliation, pinned conformance controls and structured diagnostics,
source/Git/public-source preservation, native/receipt publication, entry null and
empty actual trust, all held commands and human/runtime boundaries. Preserve the
ASCII DateTimeOffset parser, validated offsets, arithmetic tick conversion and
raw save-order type checks. Do not rerun the broad lane; its two independently
established baseline failures stay visible.

Complete these non-frozen requirements without implementing runtime:

1. **Actual wire semantics.** Closure/archive payloads include optional
   `reasonCode`; support genuine serializer output including null, with additional
   actual wire goldens. Separate general DateTimeOffset parsing from Conversations'
   year-at-least-2000 business rule. Tenants events may carry earlier representable
   values, including native minima where their contracts allow them; keep the
   existing positive bounded UTC observation rules distinct. Tenants configuration
   values are strings and may be empty/whitespace. Follow actual validator and
   projection semantics, not nonblank identity rules. Retain source attribution.
2. **Raw numeric domains.** Compare domain values with exact recursive primitive
   types; integer timestamps/sequences cannot alias floats or booleans through
   Python equality. Keep actual EventStore canonicalization and AD-8 semantics.
   Enforce actual native representability inside raw fault JSON and arrays as
   well as outer schema: Int64 event sequences are positive and at most
   9223372036854775807, checkpoints can start at zero but never negative. Apply
   representable UTC ticks to erase durable/acknowledgement times. Test negative
   restart/future snapshots, oversized envelopes, recanonicalized float ticks,
   boolean aliases and oversized erase times.
3. **Tenant key derivation/isolation.** AC-16.1-01 and both public-surface scenarios
   retain explicit tenant identities, measured keys and isolation operations.
   Derive expected keys from the actual pinned segmentation convention; arbitrary
   distinct strings are insufficient. Query authority must equal the complete
   expected `projection:conversations-index:<tenant-segment>` for its measured
   tenant, not just a prefix. Include controlled same-resource cross-tenant
   reads/writes, distinct contents, unchanged other-tenant state and correctly
   scoped results. Derive assertions from inputs/outcomes. Retain attributed key
   goldens including escaping/collision boundaries, without default references/
   reads. This remains a bounded evidence oracle, not runtime storage/access code.
4. **Duplicate-conflict inputs.** AC-16.2-01 retains original/reused authoritative
   inputs for every full-binding conflict and malformed identity/time case.
   Derive the named difference and fail-closed outcome independently; reject
   unchanged pairs, the wrong differing field, omitted inputs and actually-valid
   supposedly malformed values. Preserve raw payload hashes and selected contract
   versus position-only identity/time, including differing transport time with
   valid winning contract time. Labels cannot substitute for conflicting values.
   Apply the same input-derived proof to AC-16.1-01: its next, duplicate,
   accepted-conflict and four older-history rows currently retain only labels,
   sequences and equal version numbers. Retain delivered/accepted fingerprints,
   authoritative-history availability and identities, before/after composite
   projection-checkpoint state and the measured CAS expectation/outcome. Derive
   exact-next, identical no-op, conflict, missing-history and unavailable outcomes;
   prove no old event reapplication and an actual common atomic write for exact-next.
   Do not infer numeric ordering for opaque ETags. Include unchanged/wrong-event,
   mismatched history, split-write and failed-CAS counterexamples.
5. **Redaction preservation.** AC-16.2-05 retains actual source-derived redaction
   histories and corresponding persisted/production-query observations. Follow
   MessageContentRedacted/materializer/query semantics: append-only governance
   does not rewrite/delete source events; existing derived placeholders, safe
   attribution and trust vocabulary remain preserved. Include an actual wire
   golden, targeted message and unaffected control. Reject stale original content,
   missing/foreign targets, inconsistent persisted/query values and changed safe
   attribution. Do not invent a new masking policy or general runtime projection.
6. **Measured tenant-data safety.** Both public-surface scenarios retain actual
   structured emitted Conversations events/log records for a controlled tenant
   experiment. Validate allowed fields/values and their relation to the experiment;
   empty personal-field lists cannot prove absence. Use distinct synthetic personal
   canaries in authoritative tenant inputs and inspect emitted bytes for leaks,
   including nested values/extra fields. Legitimate public Conversations content
   and identities are not prohibited by this tenant-data rule. Bind the control
   to actual source/contract shapes; do not claim general personal-data detection.
7. **Actual catalog binding.** First failure/reconciliation retain catalog bytes
   and derive digest, exact runtime identity and selected project/v2/reconcile
   route from the actual pinned protocol. Compare the common catalog with measured
   runtime/route and pending identity; equal arbitrary hashes are insufficient.
   Reject changed bytes with stale hashes, correctly rehashed foreign runtime/route
   inventories, missing/duplicate routes and cross-scenario drift. Keep authenticated
   wire recovery, EventStore ownership and no direct handler calls.

Every finding-specific patch below survives this re-derivation. Audit all fifteen
preserved scenarios once more against measured values/relationships, not labels or
claimed counts. Share reusable checks and add no unrelated production behavior.
Remove each critical new validation in disposable copies to prove test sensitivity.
Retain existing consumer/CLI/publication positives and earlier guards, then refresh
bindings, held commands, exact workflow guards, verification record and handoff.

### Review iteration 5 — stored identity and native completion boundaries

The fifth review confirmed further input-binding omissions within the approved
intent. This fifth bad_spec re-derivation adds no runtime authority or new owner
decision. The reviewed implementation is preserved at
`/tmp/epic16-review5-preservation-iuhdnil2`; verify its manifest hashes, restore
sound parts, and replace the incomplete relationships below. Retained qualification,
review observations, specs and handoff remain in place.

**KEEP before reversion:** preserve all prior KEEP instructions and corrections;
the 958-pass combined suite, final 55-pass affected suite, 72 legacy checks and
47 effective mutation guards; actual conversation@v1 enrollment, populated native
catalog metadata and length-framed fingerprints; genuine event/key/catalog/log
samples; full domain and conflict/CAS/isolation/redaction/canary relationships;
source, ancestry, clean dependency, native metadata, canonicalizer and publication
checks; historical pins, entry null, empty trust, 18 held commands and both workflow
guards. Preserve audit UTC/year bounds and all actual Tenants nullable/whitespace
semantics. Keep broad baseline failures visible; do not rerun the broad lane.

Complete these non-frozen requirements:

1. **Erase targets must identify stored objects.** A tenant/generation label next
   to an arbitrary key cannot prove scope. Retain enough source-bound target
   identity and actual captured stored metadata to derive/validate each key's
   tenant/resource and retiring generation independently. Use the existing key
   convention and actual generation location (do not invent a generation suffix
   where the real key has none). Bind exact captured/deleted inventories and
   source records. Reject another tenant's valid key even when its adjacent labels
   claim the requested tenant; reject changed generation, key/record identity and
   missing/extra target observations. Reuse the existing key relationships; this
   remains a controlled evidence check, not a deletion implementation.
   Distinguish the current writer's legacy (event position, dispatch ID) write
   generation from the future v2 lifecycle generation. They are independent;
   a controlled positive must use different values. If the v2 runtime wire type
   is not yet implemented, name the versioned capture DTO honestly and keep entry
   held; do not attribute a lifecycle generation field to existing v1 writer code
   or equate it to freshness.lastAppliedEventPosition.
2. **Authoritative replay prefix.** The controlled rebuild has no verified
   checkpoint snapshot. Require every aggregate stream to replay from sequence 1
   (`start == 0`) through its captured end. Reject dropping the creation/history
   prefix while adjusting the length. Do not add a snapshot protocol merely to
   accept this invalid case; full snapshot support is not needed by this experiment.
3. **Restart pending evidence.** Replace the toy TenantDisabled payload with
   actual serialized tenantId/disabledAt data. Bind pending records to the measured
   restart tenant, actual event type/shape and exact immutable raw payload digest,
   in both before/after snapshots. Preserve Gapped denial and the exact future
   sequence/horizon proof. Reject malformed JSON, wrong event shape/type/tenant,
   altered fingerprint and drift after restart. This controlled positive proof
   concerns valid acknowledged native events; it must not claim a Healthy apply
   merely from preserving pending bytes.
4. **PartyId semantics.** Apply actual PartyId Trim().ToLowerInvariant()
   normalization to all materialized Party identities, including participant,
   author, actor and redaction attribution, while retaining original payload bytes
   for hashes. Use the fixed native helper/source-derived .NET behavior or an
   independently proven exact equivalent, not Python lower/strip approximation.
   Add genuine normalized wire/deserialization goldens including whitespace,
   casing and Unicode; reject duplicate normalized participant identities. Keep
   generic opaque Tenant/Conversation/Message/File identifier semantics unchanged.
   Cache pure native normalization if needed to avoid redundant helper processes.
5. **Native numeric and nullable domains.** Bound raw lease/version fields to
   their native representable domains and actual UTC timestamp fields to .NET
   tick bounds, as for existing sequence checks. Reject the reproduced 2**80
   lease. Allow null recovered TenantReadModel.Description, matching both actual
   records and the already-correct replay oracle. Test both null and string paths.
6. **Redaction identity.** Require the selected redaction event identity to be
   new relative to the source-before processed identity set; reject a changed
   event reusing an earlier ID even if its deduplication key and both expected
   projections are refreshed consistently. Keep source-after append-only and
   existing source/event tenant/conversation/sequence/attribution checks.
7. **Genuine native test completion.** Reject genuine xUnit async-void facts by
   metadata (async state-machine marker plus void return) without executing supplied
   assemblies; keep synchronous void, Task/ValueTask and fixture constructor
   compatibility. Inspect TRX run-level errors/failed cleanup as well as test
   counters and summary. A passed test with RunInfo outcome Error cannot qualify.
   Retain a genuine runner positive and targeted negative cases with stable codes.
8. **Checkout byte stability.** Audit the complete bound source/input inventory
   against Git attributes, adding scoped policies for the four newly bound shared
   Python helpers and any other uncovered input. Preserve raw native Epic16 TRX
   and publication bytes (including UTF-16/CRLF); do not normalize measured data.
   Verify an isolated core.autocrlf=true checkout/round trip against the same hashes.
   Historical generator/attribute fixtures and predecessor pins remain unchanged.
9. **Runtime insertion coverage.** Add the missing disposable qualified-runtime
   insertion test using existing test-only trust injection. Valid generated records
   and verbatim spec insertion must remain BLOCKED/PUBLICATION_MISSING until the
   required native/receipt/final-pair bytes are committed atomically; after that
   exact commit, the real runtime insertion route must PASS. Exercise the actual
   caller, not merely the helper. Removing the publication-required guard must
   fail this test. This proves only a disposable protocol, never actual runtime
   entry; do not admit real trust or publish actual checkout runtime artifacts.

R5B4 is rejected: ready-empty has an observed index but no summaries, surviving
details or pending dispatch. One matching index generation can legitimately be
all present components; do not impose a five-nonempty-component rule. Preserve
mixed-generation and missing/present component safeguards already measured.

Retain all earlier positive/negative paths and add focused mutation sensitivity
for these fixes. Verify affected implementation and legacy paths, refresh source
bindings/held commands/workflow guards, and retain exact command/source-epoch
observations. Audit the preserved scenario matrix before reporting completion.

### Review iteration 6 — complete measured schedules and portable native publication

The user explicitly approved continuation after the recorded review-limit halt.
Use `/tmp/epic16-review6-preservation-llp3zvl4/manifest.json` as the hash-verified
preservation reference. Restore its sound implementation before completing the
requirements below. Only task-owned implementation files are reverted; all
qualification, review evidence, runtime draft, specs and handoff remain in place.

**KEEP before reversion:** retain every prior KEEP item and accepted correction;
the complete final-source 1,024-test suite (862 successor plus 162 historical),
72 legacy checks, 15 new mutation probes and 47 earlier mutation observations;
true native metadata/runner/contract/PartyId/canonicalizer compatibility; complete
source/entry/ancestry/dependency binding; immutable prior packages and accepted
pins; all 18 scenario IDs, 32 requirements and graph edges; atomic publication and
lifecycle checks; exact raw native bytes; entry null and empty trusted entries;
all eighteen actual held commands and both workflow guards. Preserve legitimate
small replay histories, ready-empty index-only state and observation-time
freshness semantics. Keep the broad baseline failures visible and do not rerun
the broad lane. No actual runtime implementation or evidence publication occurs.

1. **Retain the actual native binary in publication.** Derive an exact unique
   assembly inventory from the five validated native results. Hash and inspect
   each retained managed assembly using the existing non-executing metadata
   verifier and candidate/type/method checks. The runtime publication transaction
   includes every consumed retained binary and any necessary capture manifest,
   as well as all five native results/receipts and the deterministic final pair.
   Shared assembly paths are deduplicated only with identical hashes. Missing,
   extra, altered, conflicting, aliased or out-of-scope artifacts fail. Restrict
   retained binary/capture paths to dedicated evidence locations, never arbitrary
   source, Git administration, predecessor or accepted-evidence files. Preserve
   exact committed-byte equality and per-commit source/lifecycle boundaries.
   Updating only a test's expected path set is insufficient: the real insertion
   route must work after a fresh disposable checkout without untracked build
   output, rebuilding evidence assemblies or reading the original measurement
   directory. The fixed trusted metadata helper may still compile normally.
2. **Bound relocation without editing native output.** Keep untouched genuine
   TRX and support its original contained absolute codeBase as well as relative
   paths. Introduce a small closed, versioned capture manifest if needed: bind
   scenario, full candidate, exact native path/hash, original codeBase and its
   original repository-root relationship, retained repository-relative binary
   path and exact hash. Inspect the retained bytes and bind the manifest in
   receipts/final records. Treat the old absolute path only as validated metadata,
   never a filesystem read target in a relocated checkout. Derive the same stable
   binary binding and final pair in both locations. Do not guess basenames,
   normalize traversal, resolve symlinks, execute captured commands, silently
   rewrite TRX, or trust an arbitrary relocation directory. Reject dot traversal,
   uncontained/ambiguous original paths, duplicate or missing mappings, changed
   native/binary/candidate identities and retained-path escapes/aliases. Keep
   direct contained absolute-path interoperability already tested. Account for
   lexical POSIX/Windows capture paths without treating host-specific absolute
   strings as current filesystem paths. Add a genuine raw-TRX capture positive
   and actual runtime generation/publication/insertion after relocating a complete
   disposable qualified repository; only fixture trust is injected.
3. **Measure repeated and conflicting future deliveries.** Keep the three
   distinct future merges and complete authoritative replay. Replace the two
   outcome-label-only proofs with exact duplicate and conflict schedules whose
   before/after composite state and delivered native records are retained. Bind
   them to the same tenant, exact-next checkpoint, horizon and pending fingerprint
   inventory from the gap experiment. An identical future delivery is a no-op:
   unchanged pending data, checkpoint, horizon, Gapped denial and no new commit.
   A different native fingerprint at the same pending sequence durably CAS-marks
   Corrupt while preserving the original pending evidence and projection/checkpoint;
   retain expected version/etag and observed commit, not merely a returned label.
   Derive fingerprints from actual validated raw tenant event payloads. Demonstrate
   duplicate removal/replacement, changed tenant/sequence, missing schedule,
   conflicting identity, ignored corruption and split persistence failures. Do not
   confuse an accepted-sequence conflict (already covered) with this future case.
4. **Bind Healthy restart to its persisted tenant and consumer.** Retain the
   authorized and denied restart cases, with explicit same-tenant store lookup,
   captured complete tenant domain state, sequence/freshness and consumer request
   identities before/after restart. Derive the allowed/denied outcome from that
   state and source events using the existing transition/decision oracle; do not
   let `allowed` or `tenantEnabled` alone establish a decision. The existing
   ITenantProjectionStore.GetAsync(string tenantId) seam and InMemory store key
   directly by the exact tenant ID: attribute that logical key, not an invented
   physical Dapr-key convention. Native sources are
   `references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`
   and `InMemoryTenantProjectionStore.cs` in that directory. Describe any future
   versioned durable capture DTO honestly, without asserting that a durable
   provider already exists. Reject another tenant's snapshot, lookup or consumer
   request even if adjacent scenario labels are refreshed; retain raw immutable
   snapshot equality, different process IDs and zero redelivery.
5. **Observe the unrelated-port negative separately.** Keep the actual Dapr
   collision and exact occupancy restoration. Also retain a separate preflight
   operation with only the unrelated port occupied and every owned binding free,
   its endpoint inventory and actual emitted diagnostic/result. Bind this operation
   to the same control inventory and derive absence of collision classification
   from the separate observation. A null adjacent verdict during the real collision
   is insufficient. Reject absent/unexecuted controls, an owned occupied port in
   the negative case, wrong inventory and any unrelated-port collision diagnostic.
   Do not require the production live lane to run for this isolated preflight.
6. **Measure newer progress and fencing during reconciliation.** Retain a
   controlled concurrent schedule tied to the same pending item, tenant/resource,
   runtime/catalog and authoritative recovery events. Capture the old worker's
   lease/version/generation/checkpoint/watermark expectation, intervening newer
   stored progress, its write/CAS attempt and complete persisted after-state.
   The stale/expired attempt must be rejected, must clear no pending work and
   must leave newer checkpoint, generation, watermark and projection contents
   unchanged. Bind the successful current worker's clearance and duplicate retry
   to the resulting state and existing query proof. Derive progress from retained
   authoritative global-position observations where a global watermark is used;
   never equate aggregate sequence numbers with EventStore global positions.
   Use a controlled positive with deliberately different numeric domains. Cover
   stale version, expired lease, newer generation/watermark and duplicate retry,
   preserving exact scope/identity. Labels such as staleWorkerWrites=0 and scalar
   monotonic comparisons alone cannot establish the required concurrent proof.
   The negative stale-version, expired-lease, older-generation and older-watermark
   cases are independent controls starting from the same captured newer snapshot,
   not successive events in one clock timeline. Each storedBefore is its explicit
   reset/fork boundary and storedAfter proves that control preserved the snapshot.
   Current-worker clearance is a separate positive branch from that snapshot.
   Document these fixed versioned protocol semantics in schema/helper/test text;
   do not infer chronology from array order or confound the isolated lease-expiry
   test with an already-mismatched version or lease expectation.
7. **Horizon and readiness corrections.** In every relevant fault relation,
   future delivery keeps max(previous horizon, all delivered sequences). Reject
   the retained 999-to-14 counterexample and preserve pending evidence. A failed
   readiness observation must reach the pinned attempt limit or timeout, not
   merely be within both maxima; reject one attempt/one millisecond under the
   existing controls. Apply the same terminal-condition semantics to the matching
   endpoint-race fault. Do not change these controlled limits into claimed
   operational SLOs or require all attempts when the timeout has already elapsed.
8. **Native opaque whitespace.** Correct the existing nonblank predicate to
   match .NET ThrowIfNullOrWhiteSpace exactly for opaque identifiers and selected
   native identities, including U+001C–U+001F. Audit downstream Python strip checks
   that could re-reject such validated values. Use the fixed native helper or an
   independently proven exact equivalent; preserve opaque case/whitespace and
   retain PartyId's separate Trim/ToLowerInvariant normalization. Add genuine
   cross-runtime positive and true-whitespace negative boundaries without
   weakening semantic nonempty checks on controlled labels.
9. **Story 16.3 ownership guard.** Exercise the existing runtime entry decision
   with explicit test-only trust for Story 16.3. Otherwise valid entries must
   fail when appHostOwnership is missing, remain BLOCKED when unqualified, and
   pass that entry guard only when qualified PASS. Removing the extra requirement
   must fail the focused test. This is disposable qualification coverage only;
   do not populate actual trust or claim ownership approval.

Retain rejected finding R6B6: a legitimate smaller multicollection history is
allowed by AC-16.2-02, which has no fixed event-type inventory. Do not pin one
particular history or break the existing complete-domain oracle to satisfy it.

For each changed proof, test the actual schema plus evaluator/caller and retain
a targeted guard-removal mutation with failures and zero setup errors. Audit
the full preserved scenario and I/O matrix, then run the combined successor and
historical suite and affected legacy selector once on stable final source.
Retain exact commands, logs, JUnit, source/test/artifact hashes, eighteen actual
held results and both workflow guards under review-iteration-6/implementation.
Update the handoff and distinguish the historical limit halt from this authorized
continuation. Do not overwrite old review or source-epoch observations.

### Review iteration 7 — authoritative boundaries, atomicity and order

Continue under the user's existing review-limit override. Restore the hash-verified
35-file implementation from `/tmp/epic16-review7-preservation-bt3sup0y/manifest.json`
before applying this amendment. The parent reverts only these task-owned code,
configuration and fixture files; qualification artifacts, reviews, specs, handoff
and runtime hold remain. The concurrent user commit
`8748a6b6e80b3ecbae2bb0174b43f40e30e6a148` records Story 5.4 work; preserve all its files, including its external
installed-delivery test and deferred ledger. It does not replace the baseline.
Do not change that unrelated work to satisfy this build's review.

**KEEP before reversion:** preserve all prior KEEP instructions and accepted
corrections, 1,213 final combined checks (1,051 successor and 162 historical),
72 legacy checks, 25 iteration-6 mutation probes and all earlier retained epochs.
Keep portable raw-TRX capture, content-addressed binaries and exact publication
inventory proven through a fresh disposable checkout after removing the original
measurement directory. Keep future duplicate/conflict composite schedules,
tenant-bound Healthy restart, separate unrelated-port preflight, independent
recovery branches, different global-position/aggregate-sequence numeric domains,
exact .NET whitespace and PartyId rules, all domain/native metadata/canonicalizer
compatibility, source/entry/ancestry/dependency binding and per-commit source-history
constraints already specified. Preserve all fifteen scenario routes,
eighteen IDs, thirty-two requirements, prior packages and accepted pins, actual
empty trust and null entry, eighteen held commands, fifty-four absent runtime paths
(including binary directories), and both completion workflow guards.
Do not rerun the broad lane; retain its known unrelated failures separately.

1. **Honor the native helper's repository toolchain policy.** Read the repository's
   exact `global.json` through the contained reader and carry its bytes into the
   isolated helper workspace. Include the policy in the helper/executing-source
   binding and independently check the selected SDK against that policy. Record
   the selected SDK and actual helper runtime/framework identity in trustworthy
   helper provenance and verification evidence. Do not substitute a guessed SDK,
   alter the root pin, let an evidence artifact select commands, or report a
   different installed SDK on this host: both observed workspaces currently select
   10.0.402, but the old workspace lacks the policy. Unavailable approved tooling
   remains structured BLOCKED; invalid inputs remain FAIL. Validate policy syntax
   and object/SDK shape before starting dotnet: malformed JSON or a non-object policy
   must not be mislabeled as an unavailable approved SDK. Exercise that distinction
   through actual isolated helper setup, not only direct validate_sdk calls.
   Preserve deterministic
   records and relocation under the same approved policy; separate local execution
   provenance from portable measured native capture as appropriate. Exercise actual
   helper policy propagation and unavailable/mismatched toolchain handling without
   requiring a second SDK installation.
2. **Preserve binary diffs.** Add narrow final attribute overrides for retained
   PE `.dll` and `.dll.bytes` evidence under this build's evidence/runtime paths:
   disable text conversion and forced text diffs. Preserve byte-for-byte files
   and the existing text policies elsewhere. Verify effective Git attributes and
   that a normal binary diff does not insert undecodable PE bytes into review text.
3. **Accept native nullable state and test terminal replay at the consumer.**
   Conversation snapshot label, businessReference, projectId and folderId may be
   null when native creation permits them. Correct all corresponding schema sites,
   retaining semantic shape/value checks. Run schema plus consumer positives for
   nullable histories and negatives for malformed non-null values. Add AC-16.2-02
   evaluator tests for Closed, Archived and Archived followed by Closed; use actual
   native terminal wire payloads. Require correct terminal state, reject stale Open
   with fully refreshed canonical hashes, and prove lifecycle-branch removal fails
   those tests. Do not impose a fixed event-type inventory on every replay.
4. **Derive rebuild B/C sequence boundaries from authoritative observations.**
   Retain exact per-stream authoritative event envelopes and their global positions
   bound to tenant, stream, sequence and event identity. Derive the last sequence
   at B and C and verify replay/applied boundaries against those observations,
   including complete creation prefix, manifest membership and same-generation
   progress. Global positions need not be contiguous or equal aggregate sequences.
   Reject the reproduced B=C=10 proof with boundarySequence=2/end=3, mismatched
   event/position identity, a boundary beyond its observed position and missing
   prefix/history. Include a legitimate B=C no-advance case and sparse global
   positions. Keep discovery/cutover and post-C queued-progress semantics.
5. **Keep erase reference identities consistent.** Existing non-payload references
   must not assign different immutable message identities to the same authoritative
   tenant/stream/sequence/global location. Verify relevant forward/reverse identity
   relationships across retained references; reject the reproduced collision even
   when durableReference is refreshed. Preserve reference-only durable quarantine,
   exact deletion-target identity and valid native IDs. Do not demand quarantine
   payload storage or invent a physical storage-key convention.
6. **Measure lifecycle atomic writes and stale rejection.** For both erase and
   rebuild, capture complete controlled before/after persisted control-index and
   relevant component state, the writer's plan and expected version/ETag/generation,
   EventStore CAS attempt/result and one shared durable transaction identity.
   Derive generation/version advancement, planned component changes and atomic
   correspondence from these observations; matching version scalars (including
   zero) and staleWrites=0 alone cannot prove a commit. Capture an actual stale plan
   attempted after newer persisted state and rejected without changing any newer
   component, index, generation or watermark. Cover partial/mixed commits, absent
   execution, wrong expected fence, forged successful stale attempt and mutated
   persisted after-state. Bind all observations to the measured tenant and exact
   component identities, with honest versioned future v2 DTO attribution. Keep
   legacy writer event-position/dispatch generation separate from future lifecycle
   generation. These are independent controlled erase/rebuild cases, not invented
   current runtime/provider implementations. Derive the outer summary scalars from
   the detailed observations; do not retain a second unbound source of authority.
   The preserved AD-7 state machine remains mandatory in these measured writes:
   erase first CAS-transitions Ready to Erasing, captures barrier/deletion manifest,
   then deletes retiring targets and writes Erased at the same key; rebuild enters
   Rebuilding before its final Ready cutover. Capture distinct executed plans and
   commits for entry and completion, with one generation advance for the operation
   and same-generation completion. Do not collapse Ready directly to Erased or
   Erased directly to Ready into a claimed single CAS. Match actual component
   presence/deletion and dispatch safety at each boundary, including reference-only
   quarantine retention. This clarifies existing architecture.md AD-7 lines
   2988–3033 and the preserved AC-16.2-03/04; it adds no runtime feature.
7. **Isolate every existing recovery fence.** Extend concurrentRecovery's independent
   branches with a superseded-lease-only attempt and a stale-checkpoint-only attempt.
   Start each from the same newerStored reset boundary, keep every other expected
   fence current, use a live lease time, and prove rejection preserves the entire
   snapshot and pending item. Preserve stale-version, expired-lease, older-generation,
   older-watermark, current clearance and duplicate retry branches. A consumer-level
   mutation dropping either isolated fence must be killed without setup errors.
8. **Prove commit-before-ack with correlated operation order.** Erase quarantine
   deliveries and the production first-failure pending write each need correlated
   commit and acknowledgement operation observations: same delivery/pending identity,
   actual persisted reference/state, durable transaction identity, and strictly
   ordered operation ordinals in one captured schedule. Bind timestamps to those
   operations and retain native bounds, but permit equal timestamps when order is
   proven. Reject acknowledgement before durable success, absent/failed commit,
   unrelated correlation/transaction and inconsistent persisted identity, even if
   both timestamps are equal and all adjacent summaries are refreshed. Reuse the
   existing acknowledgement proof semantics where appropriate; do not impose a
   timestamp-inequality requirement.
9. **Observe a genuine recovery horizon extension.** In AC-16.1-03 capture the actual
   persisted before/after pending set and horizon for each future delivery. The
   controlled scenario must include observed growth while recovery is in progress;
   the reproduced pre-populated final set/horizon in every row does not establish
   extension. Derive exact pending merges and monotonic max from the delivered
   authoritative fingerprints and previous state, keeping duplicate/conflict cases.
   Bind an earlier worker expectation from before that extension to an attempted
   completion against the enlarged persisted state; reject it and preserve the
   enlarged pending work. Then bind successful current-worker completion to complete
   replay through the enlarged horizon and full final persisted state. The existing
   AD-6 completion precondition remains literal: authoritative replay validates and
   consumes observed fingerprints, and the completion CAS observes an already-empty
   pending set before returning Healthy. Do not model Healthy completion as blindly
   clearing still-pending evidence; reject a nonempty pre-completion pending set even
   when the claimed final set is empty. Require the
   complete lease/version/checkpoint/horizon tuple and correlated commit observations,
   not another staleWorkerWrites label. Retain all four reverse-save schedules and
   tenant semantic decisions. Cover missing growth, preloaded future evidence,
   horizon regression, stale completion and incomplete enlarged replay.

Keep every patch finding above in the new implementation. Audit analogous proof
sites across all fifteen scenarios without inventing new product requirements.
For each changed semantic proof retain schema-plus-consumer tests and a focused
removal mutation with assertion failures and zero setup errors. Record original
commands, stdout/stderr, JUnit, current source/test hashes and artifact hashes under
`review-iteration-7/implementation/`; do not overwrite old source epochs. Run the
combined successor/historical suite and affected legacy selector once on stable
final source, eighteen actual held commands, both workflow guards, and absence
checks. Update the honest handoff. Parent probes are retained in
`review-iteration-7/parent-counterexamples.json` and helper policy evidence in
`review-iteration-7/helper-toolchain-observation.json`.

### Review iteration 8 — executed restart, queue, query and cancellation evidence

Continue under the existing user override. Restore the 36 task-owned files from
`/tmp/epic16-review8-preservation-c7g16p_0/manifest.json` and its tree, verifying
hashes before applying this amendment. `global.json` is a bound unchanged input,
not an owned new file: preserve it. Keep the user's concurrent Story 5.4 commit
and deferred ledger untouched. The parent reverts only the manifest's tracked/new
implementation files, leaving all evidence, plans, handoff and hold in place.

**KEEP before reversion:** all earlier KEEP instructions and accepted corrections;
1,467 final combined checks (1,305 successor plus 162 historical), 72 legacy checks,
35 distinct removed guards across 38 recorded mutation runs (159 targeted failures,
zero setup errors), and every source epoch and honest intermediate failure record.
Retain real native fresh-checkout capture/publication, current SDK-policy validation
before dotnet, .NET 10.0.402 SDK / 10.0.13 observed runtime attribution, narrow binary
attributes, nullable native states and terminal lifecycle replay. Retain complete
stage-by-stage lifecycle CAS, authoritative B/C cutoffs, independent recovery fences,
correlated commit/ack order permitting equal timestamps, genuine horizon extension,
replay consuming pending evidence before Healthy, exact integer schema checks and
all older source/trust/domain/canonical/identity/publication safeguards. Keep all
15 scenario routes, 18 IDs, 32 obligations, accepted packages/pins, null entry and
empty trust, 18 actual held commands, 54 absent runtime paths and both workflow
guards. Do not rerun the broad lane or imply its unrelated failures were repaired.

1. **Derive completed rebuild progress.** R8B1 reproduced watermarkAfter=999 accepted
   for C=20 while position 21 remains queued. Derive the final watermark from the
   actual completed barrier/progress and persisted same-generation preexisting
   progress, never a freely chosen upper bound. In this capture, absent separately
   measured later completed progress, final scan watermark is max(watermarkBefore,C).
   Preserve legitimate preexisting newer progress and sparse global positions;
   queued delivery alone cannot advance the completed watermark. Derive outer
   summaries and atomic snapshots/plans from that evidence. Test unsupported jumps
   with every adjacent snapshot refreshed, nonregression and legitimate newer state.
2. **Validate structural fields without interpreting domain map keys.** R8B2's
   recovery-success capture with configuration version="customer-setting" is native
   valid and independently replays correctly, yet recursive key-name validation
   rejects it. Apply numeric/boolean/array typing to known capture structures only;
   delegate native domain states and event payloads to the actual domain validators.
   Configuration keys and member IDs may be named version, allowed, history, etc.
   Preserve strict structural types, exact ints, native map values/roles/IDs and
   event schemas. Exercise colliding opaque keys in recovery and two-replica paths,
   plus malformed structural fields and malformed actual domain values.
3. **Validate acknowledgement identities.** R8B3's whitespace-only correlated
   delivery or durable-record ID passes. Apply the existing native nonblank text
   predicate to every actual acknowledgement delivery and durable record identity,
   including non-acknowledging rows where delivery identity is still present.
   Preserve exact opaque identity, including native-valid U+001C through U+001F;
   do not replace the .NET-compatible predicate with Python strip/isspace.
4. **Prove restart resumption and old-worker fencing.** Replace AC-16.1-02's isolated
   oldFence/newFence/staleWorkerWrites labels with a versioned measured schedule:
   complete pre-restart Gapped snapshot, persisted replacement lease/fencing token
   and record-version acquisition, correlated restarted worker/process and attempted
   resumption, plus the earlier worker's actual expected tuple and attempted CAS
   against the new persisted snapshot. Require failure preserving every newer state
   field and pending fingerprint. Derive summary fences from the operations. Bind
   tenant, storage identity, processes, operation order, transaction identity and
   full before/after snapshots. Recovery remains fail-closed until verified Healthy;
   no new source delivery may be required to resume. Isolate each relevant lease,
   record-version and checkpoint/horizon precondition instead of proving rejection
   only through an unrelated simultaneous mismatch. Do not duplicate the runtime
   provider: this is honest future-v2 controlled capture and oracle validation.
   Preserve the existing Healthy authorized/denied restart metadata round-trip.
   Business-event payload dates are not defined as Tenants freshness clock values:
   R8B4 does not justify importing Conversations AD-8 event-time policy here.
5. **Bind the durable rebuild catch-up queue to authoritative deliveries.** R8B6
   shows position-only arrays do not prove retained work. Capture exact tenant,
   stream, sequence, immutable event/message/global-position identity, generation,
   delivery observation and non-payload durable queue reference, with executed
   enqueue/persisted-before/after operations and correlated commit-before-ack proof
   where acknowledged. Derive the post-C inventory from observed after-B deliveries
   and their drain disposition through C, retaining every after-C reference at Ready
   cutover. Include before/at/after-C boundaries, exact duplicates, reordered arrival
   and conflicting identities. Duplicate deliveries and arrival reordering are valid;
   do not impose sorted unique arrival positions or contiguous global positions.
   Derive canonical durable reference identity/deduplication from exact immutable
   identity and prove no lost, forged, foreign, wrong-generation or prematurely
   drained reference. Bind queue component state and summaries to this inventory,
   retaining authoritative stream replay/cutoff and complete atomic-write evidence.
6. **Execute queries at lifecycle boundaries.** R8B7's queryWrites=[] alone does
   not establish concurrent reads. Capture actual executed query calls during
   Erasing (including after deletion/quarantine), Erased, Rebuilding and final Ready
   cutover, correlated with the precise committed control-index/component snapshot
   and captured operation ordering. Derive expected public state/reason/content
   visibility from that snapshot, authorization and provider inputs. Capture actual
   returned values and read/write calls so missing query execution, wrong snapshot,
   stale generation exposure, false Current/empty, read repair/recovery and writes
   are rejected. Include both sides of cutover and reference-only tombstone safety.
   Do not demand a concurrent physical provider in these disposable capture tests.
7. **Separate two preserved generation-mismatch contexts in actual observations.**
   R8B8 is resolved by AD-7's distinction, already reflected in native query source:
   `_bmad-output/planning-artifacts/architecture.md` AD-7 specifies contradictory or
   internally mixed lifecycle metadata => Unavailable/metadata_contradictory;
   mismatch between otherwise valid index, detail and dispatch component generations
   => Rebuilding. `ConversationProjectionReadService.cs` lines 70–118 and
   `ConversationQueryHandler.cs` lines 340–376 return Rebuilding/MixedGeneration for
   cross-key consistency failures; the existing fail-closed tests cover that case.
   Encode explicit structural facts for these distinct conditions, not a new label
   selecting an arbitrary result. Replace AC-16.2-05's bare mixedGenerationResult
   claim with an executed cross-key query capture, and ensure AC-16.2-03's internally
   contradictory control-record case remains Unavailable. Share derivation with
   lifecycle query observations as appropriate. Test each constituent mismatch,
   internally valid matching state and both non-disclosing failure paths. Preserve
   existing public enums/reason vocabulary and frozen contracts; do not edit runtime.
8. **Measure bounded cancellation through cleanup.** R8B9's two scalars do not
   prove any cancellation occurred. Capture the same operation's start, cancellation
   request, observed cancellation, resource cleanup and terminal completion, with
   executed operation IDs/order and an elapsed monotonic clock observation. Correlate
   actual preexisting, owned and restored resources/ports to the controlled operation
   and final completion so unrelated or missing cleanup cannot satisfy the proof.
   Derive cancelElapsedMs from request through terminal cleanup/completion and enforce
   the existing pinned 100ms control bound. Zero elapsed time is valid when ordered
   operations share a coarse clock tick; do not require unequal timestamps. Reject
   missing requests, unrelated completions, backwards order/time, unfinished work,
   resource leaks or removal of preexisting resources and out-of-bound completion.
   This proves the controlled test policy, not a new product SLO.

Every semantic change needs schema-plus-consumer positive/negative tests and focused
removal mutations that fail assertions with zero setup errors. Audit callers for
analogous field validation, without adding unsupported product rules. Retain original
commands, logs, JUnit, hashes and source epochs under `review-iteration-8/implementation/`.
Once source is stable, run the combined successor/historical suite, affected legacy
selector, all 18 held commands, two completion guards and absence/preservation checks.
Do not edit during a running verification epoch. Update honest handoff and spec
verification; no new workflow render, child implementation worker or completion step.

### Review iteration 9 — complete native execution and required negative evidence

Continue under the existing user override. Restore exactly the 36 owned files from
`/tmp/epic16-review9-preservation-6bgv3zo6/manifest.json`, verifying every hash first.
The unchanged root global.json is a bound policy input, never a new owned file.
Keep the concurrent user Story 5.4 commit, its files and the deferred ledger intact.
The parent preserves all evidence/docs and reverts only the owned implementation.
The CI-selection discrepancy is preexisting and deferred separately; do not change
CI, package.json, pyproject.toml or the known broad baseline to make this work green.

**KEEP before reversion:** all prior KEEP sets and accepted corrections, all 1,750
final combined tests (1,588 successor including 1,305 retained plus 283 new, and 162
historical), 72 legacy tests, 44/44 independent parent probes and 39 demonstrated
removed guards across 40 runs with 81 assertion failures and zero setup errors.
Preserve the one honestly insensitive redundant summary probe, all 334 hashed final
artifacts and intermediate epochs, and the exact genuine native publication/fresh
checkout proof. Keep portable captured binaries/raw TRX, non-executing metadata
inspection, exact .NET IDs and PartyId rules, native nullability/terminal replay,
canonical source/SDK policy, complete enqueue/drain and lifecycle snapshots,
post-C new-stream/duplicate/reordered delivery positives, admissible isolated restart
renewals with current positive, query context distinctions and measured cancellation.
Retain all source/trust/ancestry/history/dependency/transaction/negative controls,
18 IDs, 32 obligations, 15 routes, old packages and pins, null entry/empty trust,
18 held commands, 54 absent runtime paths and both completion guards. Do not rerun
the established broad lane. No new runtime implementation or owner approval is granted.

1. **Capture the required rejected replay operations.** AC-16.2-02's
   unorderedCollectionPersisted=false and generationBeforeAnchorState=Unavailable
   are currently only supplied labels. Add executed controlled negative observations
   containing the actual invalid input, attempted writer/materialization operation,
   observed rejection and complete relevant persisted before/after state. Cover an
   unordered collection lacking a stable composite key (and wrong ordering where
   applicable) failing before persistence, and generation earlier than the replay
   anchor returning the existing fail-closed freshness mapping without clamping.
   Derive invalidity from actual native value/key/time rules and bind the same
   tenant/resource and attempted state; do not invent a new product collection shape
   or public error enum. Missing execution, valid substituted input, false rejection,
   retained unsafe content or changed persisted state must fail the consumer. Keep
   normal deterministic canonical replay and valid zero lag. R9B2 is carried false
   from R3B4: contextual clockTicks do not establish distinct completion instants;
   neither timestamp inequality nor equality to that contextual clock is required.
2. **Observe warning removal at the actual candidate surface.** Keep restart and
   two-replica prerequisite validation, then establish warning removal from bound
   candidate source or actual content-safe diagnostics at the effective warning
   surface, not warningRemoved=true. Current source includes the SINGLE-REPLICA-ONLY
   disclosure in src/Hexalith.Conversations.Server/Program.cs around line 42.
   Inspect the actual current declaration and baseline before designing its capture.
   The historical release-evidence disclosure is immutable historical evidence and
   must not be deleted or rewritten to satisfy a current warning assertion. Bind
   before/current observations to the actual candidate/source, derive warningRemoved,
   and reject a candidate retaining the active warning even when both prerequisites
   pass. Also reject unrelated/missing surface evidence. Keep actual runtime source
   unchanged in this prerequisite build; test the future gate in disposable fixtures.
3. **Bind the product assemblies used by the native test.** A source-stamped test
   assembly alone does not identify the Conversations, Tenants and EventStore code
   it ran. Extend the versioned native capture/protocol with the actual loaded
   product assembly inventory from the measured test execution. Independently pin
   required ownership/surfaces in the successor contract; observations cannot choose
   their own expected owners or substitute source-only names. Bind exact binary bytes,
   managed identity/MVID or equivalent supported native metadata, source revision
   and repository ownership to the root candidate and direct dependency gitlinks.
   Inspect existing build/version/SourceLink conventions before choosing supported
   metadata; do not guess that an arbitrary attribute exists. Verify the bytes with
   the fixed non-executing metadata helper, never Assembly.Load or evidence-supplied
   commands. Keep the same test/run/scenario identity and correlate its loaded
   inventory with raw native output, the selected test binary and actual retained
   artifacts. Missing, stale, wrong-owner, omitted, duplicate, relabelled, foreign
   source-revision and substituted product assemblies must not qualify.
   Retain every required product binary through the exact content-addressed capture
   and publication inventory, with no dependence on original build directories.
   Derive inventory counts instead of retaining the old fixture's literal 18 paths
   as a new invariant. Preserve the original 18-file proof as historical evidence;
   extend the genuine current fixture to load source-stamped disposable product
   assemblies and prove relocation after its source build/output directory is gone.
   Those stand-ins remain explicitly synthetic test-only product fixtures and grant
   no real runtime qualification. Default tests must not read initialized submodules.
   Actual capture remains blocked until real expected product evidence is available.
4. **Verify upstream additivity independently.** AC-16.1-05 requires Tenants and
   EventStore surfaces to remain additive; the current root-only source-preservation
   check proves only unchanged Conversations contracts. Obtain baseline and candidate
   API evidence for the actual upstream seams from the corresponding raw root
   gitlinks and owning repository history. Bind exact source/assembly artifacts and
   full revisions through contained reads/clean direct dependencies. Verify each
   baseline public member remains compatible; allow legitimate additive members
   instead of requiring upstream byte equality. Keep the stricter unchanged root
   Conversations public-contract shape. Do not trust caller-authored API lists or
   equality of two arbitrary digests: derive them from actual pinned artifacts using
   established repository tooling or a cohesive non-executing inspector. Define
   the comparison's supported surface honestly, failing/ blocking unverified shapes
   rather than claiming general binary compatibility. Preserve existing signatures,
   member/type visibility, generic shape and applicable parameter/return/property
   contracts. Include disposable native/API baselines with a legitimate addition,
   removal of an unrelated baseline public member, incompatible type/signature and
   wrong source/revision. Keep upstream repositories and accepted gitlinks untouched;
   only read permitted ordinary source/docs and never load their skills.
5. **Retain the production reconciliation request/response path.** AC-16.3-04
   explicitly requires a request observed on the wire. Extend its future-v2 capture
   with the executed content-safe request/response observations and the hosted
   coordinator dispatch, tied to the same pending identity, tenant, runtime, measured
   catalog/route identity, operation ordering and successful durable transaction
   already captured by the reconciliation schedule. Inspect the current EventStore
   wire contract/route source for actual vocabulary and clearly identify future DTO
   fields; do not invent a parallel public endpoint or coordinator implementation.
   Derive wireRoute/authenticated/coordinator summaries from the observations.
   Prove authorization precedes dispatch and that the observed result corresponds
   to the actual clearance, preserving duplicate retry and stale-worker controls.
   Reject missing/unexecuted requests, mismatched request/response or pending/runtime
   identities, wrong route/method, failed authorization, direct handler bypass,
   successful-looking response without commit, and uncorrelated response/transaction.
   Retain no credentials, tokens, personal data or event content in wire diagnostics;
   use the existing safe identities and closed structural metadata.
6. **Emit bounded retry advice in readiness diagnostics.** The preserved
   AC-16.3-01 requires endpoint identity and bounded retry advice. Its closed current
   diagnostic shape has only code/endpoint/owner/port/attempts. Add actual emitted
   structured advice bound to the admitted attempt/timeout control policy and the
   measured terminal outcome. Include APPHOST_ENDPOINT_NOT_READY observations in
   both readiness and the live fault matrix where they occur. Preserve closed safe
   diagnostics and unrelated-port behavior; do not invent a production retry SLO
   or require unrelated diagnostic codes to have unspecified advice. Reject missing,
   inconsistent, unbounded or unsafe guidance and verify the actual captured output,
   not just adjacent configuration values.
7. **Apply the reproduced narrow corrections.** Close known query lifecycle and
   provider-state domains without changing the existing precedence/public mappings.
   Validate the cross-key baseline's required dispatch inventory before selecting
   it, so absence yields structured FAIL rather than uncaught StopIteration. Bind
   component tenant/resource identities and pending dispatch references to the
   control index; a foreign dispatch component or nonempty dispatch while the index
   claims none cannot establish Current. Check analogous existing component lookups
   and reject malformed baseline inventories with the existing typed failure path.
   Preserve legitimate initialized-empty states; this is not a blanket requirement
   for five physical components in every query (R5B4 remains false).
   At each verifier entrypoint handle missing internal Python modules/dependencies
   before import can escape, emitting structured nonempty BLOCKED/exit 2 with no
   traceback or output artifacts. Keep genuinely malformed input FAIL distinct and
   avoid swallowing arbitrary implementation defects. Test all three wrappers in
   partial installations, retaining existing Program.cs missing/symlink controls.
   Finally, when deriving the final retained artifact inventory, reread the retained
   binary/capture bytes and recheck contained regular/no-alias constraints against
   the earlier admitted digests. Parent reconstructed the genuine retained fixture,
   replaced all six contents, and inventory still returned the old hashes. Reject
   this demonstrated stale boundary and alias variants without claiming an absolute
   hostile-concurrent-filesystem guarantee; preserve exact publication and raw TRX.

Keep every earlier correction and the frozen authorization. For each new semantic
proof use schema-plus-consumer cases and focused removal mutations with real
assertion failures and zero setup errors. Exercise the actual verifier/generator
boundaries, native positive fixtures and fresh-checkout publication. Validate exact
new disposable commit messages with the owning repository's pinned commitlint before
using them, preserving evidence; never stage or commit the real repository. Record
all commands, streams, JUnit, hashes and source epochs under
`review-iteration-9/implementation/`. Freeze source before final combined/historical,
legacy, held-command, workflow-guard and preservation checks, and do not edit during
that epoch. Report milestones and retain intermediate failures honestly. Update the
handoff/spec, then return to the parent; do not render/invoke another skill, spawn
implementation workers or run completion orchestration.

### Review iteration 10 — correlated products and complete admission evidence

Restore exactly the 40 owned implementation files from
`/tmp/epic16-review10-preservation-2ci2k503/manifest.json`, verifying its hashes before
restoration. The parent reverted three tracked files and removed 37 task-owned new
files; all evidence, specs and user changes remain. Root global.json remains an
unchanged bound policy, never an owned new file. Preserve the concurrent user
Story 5.4 commit, its files and both existing deferred entries. This is the same
parent-rendered implementation handoff: do not invoke/render skills, spawn agents,
run completion orchestration or request the already granted review-limit override.

The **KEEP extracted before reversion** in the iteration-10 triage log is mandatory,
as are all earlier KEEP sets and change-log constraints. In particular, preserve
all 2,038/72 passing tests, all 41 demonstrated guards across 43 runs, 879 retained
artifact hashes and 84-file source epoch, independent parent probes, exact native
publication proof and explicit historical failures. The counterexamples and their
source/build/binary evidence are in `review-iteration-10/parent-counterexamples.json`
and `parent-proof-manifest.json`. The temporary experiments are also retained at
`/tmp/epic16-r10-parent-qqxy7o6h` and the nullable-constraint proof at
`/tmp/epic16-review10-constraint-3rf7ll_p`. These are intentionally defective
synthetic examples, never runtime qualification. Inspect the actual proof when
implementing its correction; do not relax preserved expectations to fit it.

1. **Correlate the admitted implementation binaries.** For AC-16.1-05, candidate
   upstream API evidence must identify the very same owned product bytes/MVID and
   source revision that this native execution captured. Independently valid stamps
   on two different DLLs do not establish that relationship. Bind the existing
   metadata/byte identities at the evaluator boundary; keep baseline evidence
   independently tied to the owning baseline gitlink. At final generation require
   one consistent inventory of the seven owned product implementations across all
   five measured scenarios. Parent constructed a real passing final record with
   distinct Server/Tenants.Client implementations in only scenario 05, and an API
   preservation result against a different Client DLL. Reject both demonstrated
   relationships using the existing typed failure paths. Keep each native test's
   own identity and do not invent equality of timestamps or all test-run metadata.
   Positive fixtures must deliberately reuse the admitted product artifacts, not
   accidentally rely on a per-process compiler cache. Preserve content-addressed
   retention, exact inventories and genuine fresh-checkout publication; derive
   counts as necessary baseline/current artifacts are added.
2. **Retain complete supported native type identities and constraints.** Fix
   SignatureNames' AssemblyReference projection to preserve the full supported
   identity, including name, version, culture, public key/token and relevant flags.
   Distinct signing identities cannot collapse into one public signature. Keep
   plain declaration/attribute lookup separate where required, and preserve the
   class/value-kind correction for definitions/references/specifications. Include
   custom attributes of each GenericParameterConstraint, not only its constraint
   type and the GenericParameter attributes. The retained IFoo? to IFoo example
   breaks an actual nullable caller but currently has equal API metadata. Compile
   positive same-identity/constraint additions and negative signed-reference and
   constraint changes; retain actual caller diagnostics where used. Continue to
   state conservative declaration preservation, not general binary or behavioral
   compatibility. Unsupported native forms must remain explicit failures.
3. **Define supported test-construction evidence without executing assemblies.**
   The test inspector currently accepts any sole public constructor, including a
   required string with no provider; the genuine pinned xUnit runner fails it.
   Inspect the actual pinned framework conventions and repository fixture shapes,
   then require supported metadata evidence that required constructor parameters
   have the corresponding framework/declared fixture provider. Preserve legitimate
   output-helper injection and class/collection/assembly fixture cases that the
   supported protocol can resolve, including applicable inheritance/default rules.
   Do not execute evidence constructors, attributes, methods or evidence-supplied
   commands, and do not pretend static metadata proves arbitrary constructor-body
   behavior. Fail closed with an honest supported boundary where a provider cannot
   be established. Cover the demonstrated missing-provider constructor and actual
   runnable positive fixtures with the genuine pinned runner. Keep plain synchronous
   void, Task and ValueTask Facts and all existing skip/async-void/framework guards.
4. **Prove the root compiled public shape as well as source preservation.** Keep
   the strict unchanged complete Conversations.Contracts source inventory, and add
   independently bound baseline/current compiled public-shape evidence at both
   AC-16.1-05 and AC-16.2-05. Bind the baseline to the required root baseline revision
   and the candidate to the actual loaded Contracts DLL from native execution.
   Require the promised unchanged supported public shape, not merely upstream
   additivity. Parent changed only an external Directory.Build.targets Compile
   removal: both builds succeed and root source equality passes while a public type
   disappears. Cover that concrete case and analogous generated/build-input effects
   without enumerating an arbitrary partial source allowlist as a substitute for
   the compiled proof. Retain baseline/current bytes through publication, validate
   actual revisions with the non-executing inspector and reject unverified shapes.
   Do not modify real runtime source or dependencies. This does not claim general
   implementation-behavior equivalence; preserve existing native wire goldens and
   source/SDK attribution alongside the new declaration proof.
5. **Capture the unauthorized/nonexistent public query response.** Extend the
   closed AC-16.2-03 protocol to retain the executed request/result relationship and
   actual supported public response structure/content for these authorization cases.
   Derive non-disclosure from the observed response, not status/reason, zero reads
   or another supplied boolean alone. Bind the response to the same query/tenant
   and preserve Forbidden/forbidden vocabulary, identical non-disclosing behavior
   for unauthorized and nonexistent tenants, zero index reads and zero repair writes.
   Reject leaked cached detail/summary content, mismatched or unexecuted responses,
   unexpected response members and stale/unrelated observations. Use actual current
   public contract vocabulary; do not invent a new DTO, endpoint, public enum or
   disclosure policy. Keep safe diagnostics and actual evidence free of credentials,
   tokens and personal/event content; leaking negative examples are synthetic test
   values. Maintain initialized-empty/current controls and later executed lifecycle/
   cross-key query proofs; those do not replace this authorization response proof.
6. **Apply the narrow native/import corrections.** The test inspector must accept
   both actual SDK SourceRevisionId informational-version forms already accepted by
   the product inspector, including existing +metadata followed by .<full-revision>.
   Preserve exact full revision matching and optional SourceRevisionId agreement.
   Require exactly one TestMethod and one Execution per admitted TRX UnitTest; reject
   additional conflicting children rather than taking find()'s first result. Keep
   genuine xUnit TextMessages/Message output, all current counters/identity/DTD and
   path guards. At the generator's lazy successor import, classify only genuinely
   missing declared dependencies as unavailable. Internal ImportError defects must
   not become SCHEMA_UNAVAILABLE. Keep standalone V9/V14 behavior and exact historical
   generator fixtures; all three wrappers' missing-module behavior remains intact.

R10B10 is rejected: authorization revision precedes committed entry/trust and the
implementation candidate. Preserve the already executed ancestry and qualified
runtime tests; there is no new authorization sequence or owner decision to invent.

For every new semantic proof, cover schema-plus-consumer admission and the real
caller path, with focused guard-removal mutations causing assertion failures and
zero setup errors. Preserve all existing case names/expectations where valid,
including historical adoption and valid current-empty/zero-lag/native constructor
cases. Correct obsolete positive capture construction when a new correlated proof
is required, and retain intermediate failures honestly. Validate exact new disposable
commit messages with the owning repository's pinned commitlint before use. Never
stage/commit the real repository or update dependency checkouts.

Retain commands, streams, source epochs, JUnit, mutation artifacts, actual native
fixtures and preservation observations under `review-iteration-10/implementation/`.
Once the source is stable, freeze it before the full affected combined/historical
suite, 72 legacy selector, eighteen actual held commands, both workflow guards,
provenance and preservation checks. Do not edit bound inputs while that sequence
runs. Do not rerun the established 39-minute broad lane, change its gates, expand
into CI/default selection or revisit user Story 5.4. Update the handoff and spec
with observed results and then return; the parent owns final audit/review/completion.
Actual entry stays null, trust empty, accepted CLI 4.35.2, runtime spec draft/backlog
and hold ACTIVE. No runtime or owner authority is granted by any fixture result.

### Review iteration 11 — complete metadata and executed outcome proofs

Restore exactly the 42 owned files from
`/tmp/epic16-review11-preservation-9mpchdvv/manifest.json`, SHA-256
`6158d4086e0c442fd1ddbd3556648c255f8793af93f739f63efc3239098b7a33`,
verifying every saved hash first. This latest restoration instruction supersedes
historical paths above. The parent restored four tracked files to the original
baseline and removed 38 task-owned new files. TestConstruction.cs and the narrow
root .gitignore evidence-packaging correction are owned and must survive. Root
global.json is protected, never an owned new file. All evidence, specs and user
work remain intact. Keep the existing parent-rendered workflow: do not invoke or
render skills, spawn agents, perform completion orchestration or request the
already granted review-limit override.

The iteration-11 triage KEEP set and all earlier change-log constraints apply.
Independent parent reproductions, compiled binaries/caller build results,
schema-admitted captures and xUnit diagnostics are under
`module/validation/epic-16-prerequisite-implementation/review-iteration-11/`.
The two parent probe scripts retain reproducible inputs and assertions. These
intentionally defective synthetic examples are not runtime qualification.

1. **Complete the conservative native declaration projection.** Retain custom
   attributes on each InterfaceImplementation, preserving its complete decoded
   interface identity and attribute constructor identity/signature/blob. Attribute
   type plus value bytes alone is insufficient: the retained int/uint overload
   example has the same blob with different constructors. Preserve constructor
   identity consistently for locally defined and referenced attribute constructors.
   Keep generic-constraint attributes, referenced signing identities and native
   class/value markers. Preserve relevant method implementation/calling-contract
   flags, or explicitly reject unsupported interop declarations; the retained
   PreserveSig change must not compare equal and pass. Preserve owning assembly
   identity flags as well as referenced flags. Apply these comparisons to both
   upstream additive and unchanged root compiled-public-shape checks. Preserve
   the existing upstream version policy; do not require exact version equality.
2. **Define assembly/module attributes and build exclusions.** Capture supported
   contract attributes on the owning assembly and module. The retained
   CLSCompliant true/false change breaks an actual consumer and must fail both
   relevant preservation paths. Define narrow documented exclusions for expected
   build/source identity metadata, retaining the separately verified source stamp.
   Do not broadly discard AssemblyMetadata or every assembly attribute to ignore
   revisions. Preserve contract-relevant values and constructor identities;
   unsupported forms fail explicitly. Compile unchanged/additive controls across
   different revisions and negative interface-nullability, attribute-overload,
   PreserveSig, CLS and owning-flag changes. Retain real caller diagnostics where
   used. Continue to claim conservative supported declarations, not general
   behavioral or binary equivalence.
3. **Bind tenant acknowledgement to the actual durable outcome.** Extend the
   closed AC-16.1-04 capture with executed successful commit and subsequent
   executed acknowledgement observations, one correlated delivery/record/transaction
   identity, and actual persisted outcome contents observed by the acknowledgement.
   Derive expected contents from the corresponding controlled source/durable state:
   accepted projection/checkpoint, verified duplicate outcome, pending reference
   or fail-closed corrupt state. A newly named outcome string or boolean cannot
   substitute for persisted contents. Reuse commit-before-ack semantics where
   appropriate without inventing public product DTOs, physical state keys,
   endpoints or runtime provider code. A duplicate may observe an already committed
   outcome; do not require reapplication or an unnecessary second mutation.
   Preserve exactly six outcomes, bounded retries and no ack for provider failure
   or exhaustion. Operation order proves commit before ack even at equal ticks.
   Reject missing/unexecuted/failed commits, wrong transaction/record/delivery,
   swapped or contradictory contents, early ack and ack after terminal failure.
   Cover every durable outcome through the real schema-plus-consumer caller.
4. **Measure stale reconciliation and duplicate execution.** Extend the closed
   AC-16.3-04 protocol so each of the six stale attempts retains an executed and
   completed operation, its identity/order, exact scope, expected/proposed state,
   actual stored before/after observations and failed CAS outcome. Bind all fields
   to the same operation; unrelated execution labels cannot qualify unchanged
   snapshots. Duplicate retry must execute and complete after current clearance,
   observing the actual final state and correlated AlreadyCompleted/no-write result.
   Correlate with existing current wire execution and clearance transaction where
   relevant; transaction instrumentation remains separate from public response
   fields. Preserve isolated reset/fork branches without ordering unrelated clocks,
   all six isolated fences, actual current success, pending and newer domain state.
   Reject omitted, unexecuted, incomplete, mismatched, reordered, wrong-outcome and
   non-preserving captures through schema and evaluator, with guard-removal controls.
5. **Correct native discovery and declared-import boundaries.** Reject the
   demonstrated Fact+Theory conflict instead of counting only exact Fact. Admit
   exactly the supported unambiguous pinned xUnit Fact declaration; account for
   resolvable Fact-derived conflicts without loading evidence assemblies or
   executing attributes. Preserve ordinary runnable Fact/Task/ValueTask, skip and
   async-void guards and supported fixture providers. Retain actual pinned runner
   diagnostics and executable positive controls. All three verifier launchers must
   distinguish genuinely absent declared dependencies from internal import defects,
   consistently with the corrected generator. The parent reproduced epic16_typo
   in every wrapper. Keep structured BLOCKED for actually missing declared imports
   and tracebacks for internal defects; a prefix cannot authorize dependencies.
6. **Preserve multi-resource erase captures and correct producer documentation.**
   The schema permits multiple retiring conversations. Derive the control index
   dispatch inventory consistently from every source-derived per-resource dispatch
   component, preserving logical identities and multiplicity. The parent capture
   passes target identity, metadata digest, replay, every lifecycle transaction and
   delivery guard before contradicting the index. Correct this derivation; do not
   narrow the schema or drop a resource. Cover two distinct resources at every erase
   boundary and concurrent query, plus missing/foreign/extra reference rejection.
   State all six runtime JSON fields, including loadedProducts, in the primary
   README protocol. Distinguish the five-field synthetic fixture shape and describe
   the required retained capture/product inventory consistently with the validator.
   Preserve the runtime/fixture authority distinction.

For every new semantic guard, retain positive and negative caller tests and
guard-removal mutations causing assertion failures with zero setup errors.
Preserve all 2,152 combined and 72 legacy passing cases, adapting only necessary
fixture captures without weakening expectations. Reuse retained product artifacts
deliberately, retain exact source epochs/commands/streams/JUnit/artifact hashes,
and run the prescribed focused combined/legacy verification on final source.
All 1,623 prior indexed artifacts and protected files remain byte-identical. Keep
18 real held commands, two completion guards, 54 absent runtime paths and the
18/32/38/61 matrix/inventory audit. Do not rerun the prohibited broad lane or fix
the carried CI selection issue. Provide a coherent source checkpoint to the parent
before final long verification so it can inspect the delta without changing
frozen inputs.

**Parent source-audit clarifications within iteration 11:** R11P1's compiled
external Attributes.dll uses System.Custom.HiddenFactAttribute and an aliased
System.Reflection.AssemblyFileVersionAttribute. Namespace/suffix recognition
currently admits a pinned-runner discovery failure and drops a non-framework
contract value change. Establish the actual supported framework assembly identity
before treating an attribute as a framework build stamp or known non-Fact type;
retain legitimate framework controls. R11P2 preserves the existing opaque native
domain maps: new source-state and persisted projection schemas must allow valid
member/configuration keys beyond the fixture's actor-a and conversations.enabled,
including valid empty maps, while validating native values. Do not infer domain-map
keys from one example. The retained semantic-positive/schema-negative capture adds
customer.setting and actor-extra=TenantReader consistently to all durable states.
Both are corrections to this amendment and earlier KEEP constraints, not new scope.

### Review iteration 12 — executed tenant decisions and failure schedules

Restore exactly the 42 owned files from
`/tmp/epic16-review12-preservation-mdzqxec0/manifest.json`, SHA-256
`0182f23d1089abe546e9de9e644402ed743819ec80765e00603d45616ec1ed2f`,
verifying every saved hash before restoration. This latest restoration instruction
supersedes every historical restoration path above. Four tracked files were
restored to the original baseline and 38 owned new files removed; all 7,951
existing evidence files and five protected files were verified unchanged.
TestConstruction.cs and the root .gitignore evidence exception remain owned;
root global.json remains protected. Do not invoke/render skills, spawn agents,
perform parent completion orchestration or request the existing review override.

All earlier constraints and the iteration-12 KEEP set apply. Parent counterexamples
and exact compiled fixture bytes are retained under
`module/validation/epic-16-prerequisite-implementation/review-iteration-12/parent-proofs/`.
They are defective synthetic controls, not runtime evidence or authority.

1. **Execute each reverse-save experiment against one durable state.** Extend the
   closed AC-16.1-03 capture for disable, membership, role and configuration with
   executed, completed older/newer save operations. Bind each operation to its
   tenant, shared store/record identity, distinct replica/process, request,
   expected ETag/version, proposed full projection/checkpoint, actual before/after
   persisted contents, result and transaction where a write succeeds. Derive both
   proposed states and their access decisions from each schedule's retained
   authoritative replay. The newer save commits first; the stale older attempt
   observes that committed state and fails its actual CAS without changing it.
   Do not replace this with two equal expected/observed dictionaries or a
   staleAccepted label. Both distinct replicas must execute correlated store reads
   after the attempts and consume that same final durable snapshot to derive the
   safe decision and contiguous sequence. Preserve the independent horizon-growth,
   pending-fingerprint and fenced-recovery experiments; they do not substitute
   for these four schedules. Branch clocks are independent; meaningful operation
   order within a branch is sufficient and equal timestamps remain valid.
2. **Derive the six authorization states and observe their consumers.** Extend
   the authorization rows in AC-16.1-01 and AC-16.1-02 with versioned controlled
   inputs and an executed/completed tenant-access request, provider/store read and
   response. Correlate tenant, principal, store/record, process, request and order;
   bind observed sequence and response to the actual read result and complete
   supported native state. Healthy requires valid persisted domain/checkpoint and
   freshness evidence; Unknown cannot manufacture missing metadata; Stale must
   derive from retained freshness evidence; Gapped/Corrupt/Unavailable must retain
   their actual gap/conflict/provider cause. Derive allowed from the real state
   and native domain access inputs, never the row's state label alone. Use existing
   source/public shapes and documented controlled observation semantics. If a
   freshness threshold is needed for a deterministic test, pin and name it as a
   test control rather than inventing a production SLO or owner policy. Preserve
   valid zero lag/equal ticks, native nullability and open member/configuration
   maps. Retain the separate authorized/denied Healthy restart and Gapped
   resumption proofs; do not force missing/unavailable storage into an invented
   successful snapshot. No query/read repair or consumer-owned recovery.
   Parent R12P2 additionally compiles the actual retained Commons evaluator:
   `tenant/a` is MalformedTenant before any health/store read, while a fully
   correlated renamed AC-16.1-01 capture currently passes with allowed=true.
   Validate the controlled access request's tenant with the actual native .NET
   input rules before admitting its claimed read, including canonical form,
   whitespace/control, reserved-character and Unicode-category rules. Do not
   narrow unrelated opaque identifiers or introduce an ASCII-only surrogate.
   Keep the corrected native Unknown-role InsufficientRole result and principal
   validation, confirmed by the parent's compiled evaluator parity controls.
   Exact proofs are in `review-iteration-12/parent-access-tenant-proof/` and
   `parent-access-parity-proof/`; the latter verifies corrections the worker made
   before the successful parent probe, not an unresolved role/principal defect.
   Preserve the copied Commons source's actual MIT notice (its root LICENSE is
   Copyright (c) 2025 Hexalith), alongside existing source notices, and identify
   that attribution beside the copied native helper methods. The existing
   domain-source notice currently names only 2026; retain both when combining
   the source attributions rather than replacing an earlier notice.
3. **Observe bounded failed delivery processing.** Extend the provider-failure
   and retry-exhaustion rows in AC-16.1-04 with actual executed/completed delivery
   processing and ordered attempt observations. Bind every attempt/result to the
   same delivery, tenant and store operation, retain the actual provider/CAS failure
   and terminal processing outcome, and derive the existing attempt count from
   that inventory (one failure or three total exhausted attempts respectively).
   Retain controlled durable state before processing and after it can be observed,
   including restoration when the provider is unavailable, proving no successful
   commit and no applied projection/checkpoint/outcome change. Observe the actual
   acknowledgement inventory for that completed delivery and require it empty;
   null ack/commit summaries alone do not prove execution. Correlate the relevant
   existing fault/restoration observations where reused rather than maintaining
   contradictory independent failure stories. Keep all four successful durable
   outcomes, duplicate zero-write semantics and existing commit-before-ack proofs.
   Do not prescribe runtime provider implementations, keys or public DTOs.
4. **Apply real framework identity consistently to demonstrated native cases.**
   Test and product source stamps must resolve to the approved actual framework attribute and
   supported constructor/signature/blob, not a local namespace/name lookalike.
   Preserve both existing supported SDK stamp forms and genuine positive stamps.
   Task/ValueTask return admission must establish the actual supported framework
   identity; the retained unsigned custom Task is not a supported framework async
   return even though its null-returning method passes xUnit. Params constructor
   supply must recognize a genuine framework ParamArrayAttribute with its valid
   constructor/blob; the retained foreign attribute must fail inspection and stay
   a real runner rejection. Reuse the existing full identity helper where suitable,
   never execute supplied assemblies/attributes during inspection, and preserve
   genuine void/Task/ValueTask, optional/default/params and all fixture/provider/
   collection cases. The R12B1 local-attribute product probe was rejected; do not
   claim it passed. Parent R12P1 independently compiled an external aliased
   AssemblyInformationalVersionAttribute: the preserved product inspector admits
   that fake stamp without any genuine framework informational version. Its exact
   helper source, product/external source, binaries and commands are retained under
   `review-iteration-12/parent-product-stamp-proof/`. Apply the same ownership and
   constructor validation to this demonstrated product source-stamp path while
   preserving genuine supported stamps and contract-attribute behavior.
   Do not broaden compatibility claims. Add caller controls for the exact demonstrated
   shapes and retain genuine runner comparisons where used.
5. **Correct the two bounded caller defects.** Seed catch-up enqueue transaction
   uniqueness with the existing entry/work/completion transaction identities, so
   one ID cannot denote their different ordered CAS states. Preserve the intended
   work/drain shared transaction and other legitimate existing correlations.
   Helper prerequisite failures must retain bounded stdout, stderr and exit details
   for actual SDK selection/build subprocess failures while keeping HelperUnavailable
   and structured BLOCKED behavior. Do not dump unbounded process output or change
   the selected SDK/dependency policy. Exercise the demonstrated stderr-only cases.

Every changed protocol needs real schema-plus-evaluator/caller positive and
negative controls, including unexecuted/incomplete operations, wrong identity,
contradictory state and bounded-count/order changes. Demonstrate each new guard
with narrow mutations causing assertions to fail without setup errors, and show
those exact cases passing on final source. Preserve all 2,416 combined and 72
legacy cases, adapting only required captures without weakening expectations.
Preserve all 2,854 iteration-11 indexed artifacts and every prior evidence epoch.
Retain exact commands/streams/JUnit, frozen source/test/policy hashes, mutation
bytes and native publication files. Verify the 18/32/38/61 inventory, 18 held
commands, two completion guards, 54 absent runtime paths, null entry/empty trust
and accepted CLI 4.35.2. Keep protected files and the real index untouched.
Do not repeat the prohibited broad lane, patch carried CI/Story 5.4 work, or
refactor the schema representation. Provide a coherent source checkpoint before
final long verification so the parent can audit without changing frozen inputs.

## Tasks & Acceptance

1. [x] Qualify the corrected CLI in isolation; retain new exact commands, exits, versions, artifact hashes and separate stdout/stderr in `module/validation/epic-16-prerequisite-implementation/`. Review current Builds fixes against the release findings. Produce an explicit Platform acceptance disposition; align accepted manifests only when existing requirements are satisfied. Otherwise report named blockers without changing pins. Do not rerun historical writing commands.
2. [ ] Add a current successor contract/binding/schema under planning `epic-16-tooling-v1/` and `_bmad/schemas/`. Preserve all eighteen ordered scenarios, requirement mappings, graph edges and frozen runtime semantics; bind unchanged predecessors and current inputs. Preparation has no runtime authority. Bound entry evidence must be independently validated before runtime receipts/final records can pass.
3. [ ] Implement `_bmad/scripts/verify_story_16_1.py`, `verify_story_16_2.py`, `verify_story_16_3.py` and a shared evaluator for all fifteen non-generator scenarios. Implement real validation of bound measured native evidence and each scenario's required proof, not constant blockers or placeholder validators. No arbitrary artifact-supplied commands. Missing future runtime tests/measurements yield BLOCKED; positive disposable fixtures exercise the full measurement-validation path but earn no runtime qualification.
4. [ ] Add narrow successor dispatch to `_bmad/scripts/generate_story_record.py` and necessary schema support, including deterministic JSON/Markdown, exact candidate/source/schema/input/output/fault/gitlink/rollback binding and inserted-record guards. Compute the sixth scenario only after five measured passing scenarios; never create runtime records from this held checkout.
5. [ ] Add focused positive/fault tests covering the matrix, all fifteen routes, false-like authority, missing history, stale/dirty candidate, inventory equality, receipt/native count forgery and output aliasing. Preserve historical fixtures/checker behavior. Run affected current Python lanes and workflow completion guards; independently probe that each new blocker fails under mutation.
6. [x] Update this spec and an additive prerequisite handoff with observed results, remaining adoption findings and human-only parity/recovery/SM-2 requirements. Leave runtime spec draft, sprint backlog and old accepted evidence unchanged except the explicit authorization note already recorded in the runtime draft.

**Acceptance Criteria:**

- Given the selected installed artifact, when native qualification runs, then exact measured results and unresolved Platform conditions are reviewable without claiming runtime entry.
- Given all eighteen inherited identities, when successor tooling evaluates measured fixture inputs, then all fifteen scenario paths and final-record derivation are exercised with exact contracts; unavailable runtime measurements block.
- Given malformed, forged, unavailable or aliased inputs, when validators run, then failures are nonempty and stable, cannot write protected evidence, and preserve historical/V9/V14/completion behavior.
- Given this actual held checkout, when successor runtime/final-record commands run, then they remain BLOCKED and no runtime completion or owner approval is manufactured.

## Implementation Notes

This implementation is a step-03 worker handoff from the already-rendered bmad-build workflow. The parent orchestrates review/completion; the worker implements this spec and does not render or invoke another workflow.

2026-10-25: User authorized this prerequisite scope. Runtime story 16.1 remains draft/backlog. Baseline tree contains only our prior runtime-draft update and entry investigation; preserve both. No separate approval of runtime, tooling acceptance or owner evidence is inferred.

## Spec Change Log

- Review iteration 12: R12B4/B5/B6 expose three missing executed protocols for
  reverse saves, six-state tenant authorization and failed bounded delivery.
  Amend only the non-frozen capture/validation requirements to bind actual state,
  operations and consumer results. Include R12B1/B2/B3 framework identity,
  R12B9 prerequisite diagnostics and R12E1 transaction uniqueness corrections.
  Avoid label-only success and contradictory durable identities. Preserve the
  iteration-12 KEEP set, all 2,416/72 cases, 35 guards, 2,854 indexed artifacts,
  native publication evidence, protected user files and runtime hold. R12B7/B8
  carry existing deferrals; R12B10 causes no schema refactor. Counter 11 becomes
  12 under the existing user override; frozen intent is unchanged.

- Review iteration 11: R11B4/B8/B9 require a defined assembly/module attribute
  boundary and executed durable acknowledgement/stale-reconciliation proofs.
  Include the eight metadata, import, discovery, erase and protocol-documentation
  corrections verified separately. Avoid collapsed contract declarations,
  unexecuted negative controls, unbound durable outcome contents and contradictory
  multi-resource inventories. Preserve the eleventh KEEP set, all prior evidence,
  protected user work, narrow packaging exception and runtime hold. R11B7 carries
  the existing CI deferral without duplicate work. Frozen scope is unchanged;
  review_loop_iteration advances 10 to 11 under the existing user override.

- Review iteration 10: R10B6/B7/B8 require declared native constructor-provider
  evidence, baseline/current root compiled public shape, and actual non-disclosing
  authorization responses. Include R10B1/B2/B3/B4/B5/B9/E1's demonstrated product
  correlation, complete metadata, native identity/version and import-boundary fixes.
  Avoid unexercised API artifacts, mixed final product implementations, collapsed
  signed/nullable type contracts, unprovided constructor dependencies, source-only
  public shape and status-only disclosure proof. Preserve the tenth KEEP set, all
  prior passing cases/evidence, runtime hold and existing ancestry interpretation;
  R10B10 adds no new authorization rule. Existing review-limit override persists.


- Review iteration 9: R9B3/B5/B7/B8/B9/B10 require executed rejected replay
  controls, candidate-bound warning removal, loaded product binary identities,
  independent upstream API additivity, production wire request/response evidence
  and emitted bounded retry advice. R9B4/B6/E1/E2/V1 remain narrow query, inventory
  and import-boundary corrections. Avoid labels substituting for required execution
  or compatibility evidence. Preserve all ninth KEEP items, legitimate zero lag,
  historical warnings and initialized-empty states. R9B2 carries R3B4's rejection;
  no timestamp-inequality rule. The user's review-limit override and frozen scope persist.

- Review iteration 8: R8B1/B5/B6/B7/B8/B9 require completed-progress derivation,
  executed restart fencing, authoritative durable catch-up references, lifecycle
  query execution, explicit internal-metadata versus cross-key mismatch facts and
  measured cancellation/cleanup. R8B2/B3 remain structural-map and identity fixes.
  Avoid scalar-only proof, arbitrary domain-map typing, lost queue work and conflated
  query contexts. Preserve every eighth KEEP item; R8B4 adds no unsupported Tenants
  business-time policy. Frozen intent and the existing review-limit override persist.

- Review iteration 7: R7B3/B5/B7/B8/B9/B10 require native toolchain policy/provenance,
  authoritative rebuild boundaries, measured lifecycle CAS, isolated recovery fences,
  correlated commit/ack ordering and real horizon-extension schedules. R7B2/B4/B6/V1
  remain explicit patch requirements for binary attributes, nullable state, erase
  identity consistency and terminal replay coverage. Avoid scalar/label-only proof
  and equal-time ordering ambiguity; retain every seventh KEEP item and the frozen
  intent. The user already authorized continuing beyond the review limit.

- Review iteration 6: user approved overriding the five-iteration limit. Complete native binary retention/relocation, future duplicate/conflict schedules, tenant-bound Healthy restart, independent unrelated-port preflight and measured concurrent recovery fencing. Include all remaining patch findings and preserve the sixth KEEP set; do not impose R6B6's unrequested fixed history. Frozen scope remains unchanged.

- Review iteration 5: completed stored erase-target and pending-native identity relationships, actual PartyId normalization, replay prefix and native completion/checkout boundaries; added runtime publication guard coverage. Avoid adjacent-label scope proofs and unsupported native shapes. Preserve fifth KEEP set and legitimate current-empty component inventory. Frozen scope is unchanged.

- Review iteration 4: completed omitted isolation, conflict, redaction, emitted-data and catalog evidence relationships after R4 findings; corrected raw numeric/native wire semantics. Avoid arbitrary-key/hash and label-only proofs. Preserve the fourth KEEP set and all earlier constraints, with frozen intent unchanged.

- Review iteration 3: completed non-frozen domain-value, authoritative-envelope/replay, public-surface, fault-control and diagnostic protocols after R3 findings. Added real xUnit TRX interoperability, encoding-aware parsing and immutable generator candidate requirements. Preserve all previous KEEP items and AD-8 semantic freshness; no timestamp-inequality rule is introduced.

- Review iteration 2: completed non-frozen persisted-restart, acknowledgement, replay/projection identity and executing-source requirements after R2B1–R2B10/R2E1–R2E4/R2V1. Preserve the second KEEP set and the 437/72 assertions. Frozen scope and actual runtime hold are unchanged.

- Review iteration 1: amended non-frozen measurement, entry-ancestry and native-metadata requirements after B1–B11/E1–E7/V1–V2. Avoid label-only or unrelated self-consistent proofs. Preserve the KEEP instructions above and re-derive task-owned code; frozen authorization is unchanged.

- 2026-10-25: Implemented the additive tooling package, pinned schema and contracts,
  fifteen shared measured-scenario evaluators, verifier entrypoints and narrow
  canonical-generator dispatch. Retained exact historical generator/attribute
  fixtures instead of changing predecessor pins. No frozen runtime semantics changed.
- 2026-10-25: Qualified the actual signed 4.38.0 artifact in isolation. Ordinary
  validation passed; adoption remains 7 PASS / 17 FAIL. All 36 malformed-key probes
  failed as required. Platform range acceptance, exact enrollment-pin parity and
  missing integration qualification remain separate; accepted pins stay unchanged.
- 2026-10-25: Added closed observation schemas, native test/method/source identity,
  dependency cleanliness and exact predecessor binding, deterministic pair generation,
  publication-byte/lifecycle guards, strict false-like authority and unsafe-output
  rejection. No runtime receipt or final record was published.

## Review Triage Log

- Incorporated independent review of canonical candidate resolution, inherited output
  identities, per-commit publication scope, committed versus working evidence bytes,
  owned sprint transitions, Unicode-safe failure reporting, observation anti-vacuity,
  exact qualification-map membership and EventStore canonicalization differences.
- Fixed independently reproduced standalone legacy generator import failure by
  selecting only the three exact successor contract paths before importing the
  optional consumer. Added UTF-16 ordinal collection ordering and independent
  runtime hashing of actual clean EventStore canonicalizer source bytes.
- Preserved the existing broad-lane `REFRESH_UV_DRIFT` failure. Its CI, checker and
  test files match HEAD; this prerequisite change does not repair or weaken that gate.


### Independent review iteration 1

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | bad_spec | The committed source binding pins entry bytes, and the entry requires that same commit SHA. This cryptographic cycle prevents real runtime admission; use a reviewed ancestor authorization revision. |
| B2 | medium | patch (superseded by loopback) | Native codeBase goes directly to relative_path; actual native TRX uses contained absolute paths. Lexical contained normalization is required; parent independently observed the same defect. |
| B3 | high | bad_spec | native_binary uses only dotnet_source_revisions regex. A text file with the candidate substring passes; require genuine managed metadata and the test type/method without executing the binary. |
| B4 | high | patch (superseded by loopback) | Restart snapshot equality never checks the actual snapshot authorization/state. A Gapped authorized snapshot passes independently of the separate authorization matrix. |
| B5 | high | bad_spec | Completion compares two supplied tuples but does not bind either to recovery lease/version/checkpoint/horizon; an all-zero tuple passes. |
| B6 | high | bad_spec | Preserved AC-16.1-03 explicitly names disable, membership, role and configuration; closed observations only prove tenantEnabled=false, omitting three schedules. |
| B7 | high | bad_spec | Permissive json.loads and self-hash equality permit duplicate keys, NaN and reversed actual items; collectionKeys is checked independently of serialized items. |
| B8 | high | bad_spec | Query mapping uses case IDs as inputs. The v1 fixture contains indexVersion=2 and no lifecycle evidence; preserved Ready/initialization/owner transitions cannot be proven. |
| B9 | high | bad_spec | Erase count/tombstone flags omit delivery identities, durable-reference acknowledgement order and absence of recreated content required by preserved AC-16.2-04. |
| B10 | high | bad_spec | Manifest arrays can agree while anonymous streams omit an expected stream. No row identity connects replay evidence to tenant-scoped authoritative discovery. |
| B11 | high | bad_spec | AC-16.3-04 reevaluates predecessor PASS but cannot compare pending item/runtime/catalog/tenant, because those fields are absent from the clearance schema. |
| E1 | high | patch (superseded by loopback) | Same verified unsafe restart authorization as B4; snapshot fields have no state/denial predicate. |
| E2 | high | patch (superseded by loopback) | Pending fingerprints check positive unique sequences, but not that they are future to expectedSequence. Older applied evidence can masquerade as a durable gap. |
| E3 | high | bad_spec | Same verified unrelated completion fence as B5; final checkpoint/horizon do not constrain the supplied CAS tuple. |
| E4 | high | bad_spec | Same canonical-byte root cause as B7: noncanonical whitespace/property ordering passes self-hash/roundtrip comparisons without invoking the pinned oracle. |
| E5 | medium | patch (superseded by loopback) | Schema permits an arbitrarily large integer end; list(range(...)) raises OverflowError or allocates excessive memory. Validate supplied count and enumerated sequence instead. |
| E6 | high | patch (superseded by loopback) | parse_trx and parse_native select UnitTestResult only, ignoring a sibling failed GenericTestResult. Require exact result child inventory. |
| E7 | high | bad_spec | Same verified cross-scenario identity omission as B11, directly contradicting the preserved same-item clearance requirement. |
| V1 | medium | patch (superseded by loopback) | Pre-verified regression gap: no test evaluates a dependent scenario with missing/invalid prerequisite. Disabling dependency evaluation still passes all 166 tests; add each edge and both outcomes. |
| V2 | medium | patch (superseded by loopback) | Pre-verified regression gap: positive tests manually write receipts; deleting the CLI safe_write call still passes 166 tests. Test actual successful subprocess publication and generator consumption. |

| P1 | medium | bad_spec | Parent found the default new Python test reads the live references/Hexalith.EventStore source, contrary to the current CI lane that has no initialized submodules. Retain an exact pinned local source fixture for tooling tests and check the real dependency only during runtime qualification. |

Groups after individual verdicts: B4/E1 restart; B5/E3 completion fence; B7/E4 canonical bytes; B11/E7 correlated clearance. All other findings retain independent root causes. The bad_spec groups trigger re-derivation; the patch rows are recorded as concrete requirements of the new implementation. No finding is deferred or silently dropped.


### Independent review iteration 2

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R2B1 | high | patch (superseded by loopback) | Pending rows retain prior fingerprints but do not require every observed sequence <= horizon. Extra sequence 99 can be cleared with checkpoint 14; check the entire inventory. |
| R2B2 | high | bad_spec | The parsed replay object permits version 1, an unrelated embedded anchor and provider wrapper fields. The protocol must define the hashed v2 value and correlate its anchor. |
| R2B3 | high | patch (superseded by loopback) | watermarkAfter >= watermarkBefore permits watermark 10 at cutover C=20. Require coverage of captured C in addition to monotonicity. |
| R2B4 | high | bad_spec | Persisted/query object equality can establish visibility but not corrected progress: both can contain sequence 0 while afterCheckpoint=3. Define and bind the projection/pending sequence relationship. |
| R2B5 | medium | bad_spec | AttributeType accepts a locally defined Xunit.FactAttribute; the compiled positive fixture deliberately defines one. Real framework assembly identity and protocol-discoverable type shape are absent. |
| R2B6 | high | bad_spec | Acknowledgement schema exposes booleans and attempts, no commit/ack ordering or common identity. Preserved AC-16.1-04 requires durably committed outcomes before acknowledgement. |
| R2B7 | high | bad_spec | Preserved AC-16.1-02 explicitly requires authorized and denied persisted outcomes with sequence/freshness. Only a Gapped snapshot is process-bound; the authorization matrix cannot establish restart persistence. |
| R2B8 | medium | patch (superseded by loopback) | Selected ticks only require positivity and ports only inventory membership. Out-of-range .NET timestamps and port 100000 pass; add domain bounds. |
| R2B9 | medium | patch (superseded by loopback) | All positive evaluator tests use fixture scope; runtime-only guards are tested as helpers. Add isolated runtime-path positives and guard-removal sensitivity tests; actual checkout remains held. |
| R2B10 | high | bad_spec | Helper compilation reads sources beside __file__, while validate_package hashes the supplied repository. Separate differing trees can yield claims for unexecuted bytes; enforce executing/bound source equality. |
| R2E1 | high | patch (superseded by loopback) | Same verified pending-horizon omission as R2B1, reproduced with sequence 999. |
| R2E2 | high | bad_spec | Same stale-but-equal projection outcome as R2B4; the protocol lacks the checkpoint/pending relationship. |
| R2E3 | medium | patch (superseded by loopback) | Same unrepresentable selected time as R2B8; 10**30 satisfies the current positivity guard. |
| R2E4 | medium | patch (superseded by loopback) | TemporaryDirectory and copied-source writes precede the OSError catch. Setup failures escape HelperUnavailable and evaluate cannot emit BLOCKED. |
| R2V1 | medium | patch (superseded by loopback) | Pre-verified caller-path gap: replacing only evaluate -> native_binary with None leaves all 275 tests passing. Add qualified disposable evaluator-path checks and assert returned binary binding. |

| R2P1 | medium | patch during re-derivation | New import-time executing-source snapshot raises ReadError before main when Program.cs is missing. Parent partial-copy subprocess exits 1 with empty stdout and traceback; preserve the failure and emit structured diagnostics during evaluation. |

| R2P2 | medium | patch during re-derivation | New inspector requires void-returning Scenario methods and parameterless constructors, excluding genuine async Task Facts and public IClassFixture constructors used by repository integration tests. Support those existing shapes without weakening framework identity or nonpublic/abstract/non-test rejection. |

Groups after individual verdicts: R2B1/R2E1 pending horizon; R2B4/R2E2 corrected projection; R2B8/R2E3 domain bounds; R2B9/R2V1 runtime caller coverage. The remaining roots are distinct. Protocol omissions and executed-source attribution require re-derivation; all patch requirements remain recorded. No finding is deferred.


### Independent review iteration 3

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R3B1 | high | bad_spec | Closed replay state hashes only keys, version and anchor. It cannot encode different conversation, participant or file values under the same IDs; add measured domain state and authoritative-event derivation. |
| R3B2 | high | bad_spec | Full binding checks only member names, payload hash and selected time. Tenant/conversation/type/sequence/codec lack an independently compared authoritative envelope. |
| R3B3 | high | bad_spec | Pending hashes are internally consistent but never compared to contiguous authoritative replay or used to derive recovered state. Relabelled enabled events can coexist with disabled results. |
| R3B4 | false | reject | AD-8 explicitly validates observation-time freshness by semantic invariants rather than timestamp byte inequality. Different contextual clock observations do not prove distinct completion instants; both completion/anchor/lag triples can legitimately coincide. Requiring inequality would invent a constraint the preserved AC rejects. |
| R3B5 | high | bad_spec | public_surface compares two supplied digest strings without reading underlying API/source bytes. Matching arbitrary digests therefore establish no independently verified preservation. |
| R3B6 | high | bad_spec | Closed projection observations contain only sequence, so corrected domain content cannot be represented. Include pending-event-derived expected values and compare persisted/query state. |
| R3B7 | medium | bad_spec | Readiness and cancellation caps are supplied by the observations themselves; absurd durations satisfy the relation. Pin conformance-control limits independently without claiming production policy approval. |
| R3B8 | high | bad_spec | portsBefore/After equality is unrelated to occupiedPort. Matching [123] can pass a 50001 collision proof; require affected endpoint/ownership/occupancy restoration. |
| R3B9 | high | bad_spec | fault_matrix accepts generic restored bytes, expected labels and caller-provided assertions>=1. Distinct injected conditions, validated assertion entries and target-bound restoration are missing. |
| R3B10 | high | bad_spec | An empty diagnosticPayloadFields list does not inspect diagnostics. Endpoint strings are unconstrained beyond nonempty, so personal/token text can pass; bind inventory identity and validate actual structured output. |
| R3E1 | medium | patch (superseded by loopback) | Unknown XML encoding raises LookupError outside current parse_native catches; stable NATIVE_INVALID is required. |
| R3E2 | high | patch (superseded by loopback) | The raw ASCII DTD/entity scan misses valid UTF-16 declarations, which ElementTree then expands. Use encoding-aware forbidden-declaration handling. |
| R3E3 | high | patch (superseded by loopback) | generate passes symbolic HEAD into five independent evaluate calls and takes candidate from the first. Concurrent advancement with corresponding receipts can mix candidates; resolve once and compare all results. |
| R3V1 | high | patch (superseded by loopback) | Verified other finding: parse_native requires testName==AC ID, while genuine pinned xUnit TRX uses fully qualified class/method. Actual 1/1 runner output and accepted metadata in the supplied reproduction fail only that parser comparison; consume untouched runner output in regression coverage. |

Every finding was graded before grouping. R3B1/R3B6 share a domain-state omission but require both replay and reconciliation proof sites; the other surviving roots remain distinct. R3B4 is rejected on the preserved AD-8 semantics. The bad_spec entries require re-derivation, retaining the patch requirements. Nothing is deferred.

### Independent review iteration 4

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R4B1 | medium | patch (superseded by loopback) | Tenant configuration validator uses NotNull/MaximumLength, while oracle text rejects valid empty/whitespace values. Parent reproduced both rejections. |
| R4B2 | medium | patch (superseded by loopback) | TenantUpdated is an unconstrained DateTimeOffset record; common parser applies Conversations year >= 2000. Parent reproduced 1999 rejection. |
| R4B3 | medium | patch (superseded by loopback) | Raw restart sequence -10/pending [-5] passes schema and measurements after digest refresh. Raw primitive checks lack native domain bounds. |
| R4B4 | high | bad_spec | Prefix-only authority check accepts projection:conversations-index: with no tenant segment. Parent reproduced; measured tenant identity and key derivation are absent. |
| R4B5 | high | bad_spec | Arbitrary unsegmented-a/unsegmented-b keys pass. Distinct strings and public-surface equivalents do not retain same-resource cross-tenant operations or expected segmentation. |
| R4B6 | high | bad_spec | Conflict rows contain only identities/state/reason. No original/reused input can establish each required binding difference or malformed identity/time. |
| R4B7 | high | bad_spec | Preserved AC-16.2-05 explicitly requires redaction, but neither its fields nor dependencies represent redaction history/state. Actual materializer retains governance and masks targeted derived content; preserve those existing semantics, not source deletion. |
| R4B8 | high | bad_spec | Public-surface checks inspect two empty claimed personal-data lists without emitted event/log contents. Actual outputs and the controlled tenant-data inputs must be retained and inspected. |
| R4B9 | high | bad_spec | Parent reproduced all-zero catalog hash acceptance. Cross-scenario equality only correlates a claim; no catalog bytes establish runtime/route membership or digest. |
| R4B10 | medium | patch (superseded by loopback) | Parent reproduced erase durable/ack timestamps beyond DateTime.MaxValue.Ticks; only ordering is checked. |
| R4E1 | high | patch (superseded by loopback) | Parent recanonicalized nested occurredAtUtcTicks as equivalent floats and refreshed hashes; schema and semantic checks accept different encodings through Python numeric equality. |
| R4E2 | medium | patch (superseded by loopback) | Parent reproduced negative checkpoint/horizon/deliveries in the three-future fault. Same raw-domain omission as R4B3 at another proof site. |
| R4E3 | medium | patch (superseded by loopback) | Parent reproduced sequence 2**63 in envelope/full binding; positivity alone permits an unrepresentable native Int64 sequence. |
| R4V1 | medium | patch (superseded by loopback) | Both actual terminal event records declare ReasonCode; the oracle lists no fields and rejects their valid wire payload. Extend actual serializer goldens and shape. |
| R4P1 | high | bad_spec during re-derivation | Parent fifteen-scenario audit found AC-16.1-01 transition rows contain only case/outcome labels, sequences and matching version numbers; no delivered/accepted/history fingerprints or measured composite CAS state can establish the explicitly preserved identity and atomicity semantics. Complete this analogous conflict-input proof with the current loopback. |

| R4P2 | high | patch during re-derivation | Initial new composite transition proof required no persisted write for conflicts and kept Healthy after-state despite returned Corrupt. Persist fail-closed health atomically while preserving projection/checkpoint; bind accepted fingerprint sequence. |
| R4P3 | high | patch during re-derivation | Adding redaction to envelope parsing without a replay transition silently recorded its event ID while retaining original content. Apply actual redaction or reject it outside the separately bounded experiment; never silently ignore it. |
| R4P4 | medium | patch during re-derivation | Actual Set/Remove configuration validators accept nonempty whitespace keys, but text() rejected them. Parent reproduced a space key rejection; preserve nonempty string length semantics. |
| R4P5 | high | patch during re-derivation | Initial catalog control conversations@1.0.0 contradicts bound manifest/topology appId conversation and Aspire-injected serviceVersion v1. Use the actual hosted identity and native goldens. |
| R4P6 | high | patch during re-derivation | Actual admitted catalog responses populate fullReplay/mappedOnlyStreamed/incrementalReplay/sharedEpoch/singleEvent lists; the new exact shape omitted them. Retain genuine populated wire output and validate route subset relationships. |

All findings were individually verified and graded before grouping. R4B4/R4B5
share missing tenant derivation/isolation; R4B3/R4E2/R4E3 share raw sequence bounds
at distinct sites. Remaining roots are distinct. Protocol omissions require this
fourth re-derivation; patches remain explicit. No finding is deferred. Parent
counterexamples are retained in review-iteration-4/parent-before-corrections.json.

### Independent review iteration 5

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R5B1 | high | patch (superseded by loopback) | Parent reproduced reused source event ID with refreshed redaction metadata/sourceAfter/processed lists; schema and semantic checks accept conflicting identity. |
| R5B2 | high | bad_spec | Parent replaced captured/deleted keys with a valid foreign-tenant key; unchanged adjacent tenant/generation labels still pass. Captured stored identity is missing from this proof. |
| R5B3 | high | patch (superseded by loopback) | Parent dropped sequence 1 by start=1/applied=[2,3]; complete rebuild passes without any checkpoint snapshot proving omitted state. This experiment must start at zero. |
| R5B4 | false | reject | The reproduced row is ready-empty: indexPresent is true and summaries, survivingDetails and pendingDispatch are empty. A single index generation legitimately describes all present components; truncating this example does not demonstrate omitted existing content. A mandatory five-component list would reject valid empty state. |
| R5B5 | medium | patch (superseded by loopback) | Parent reproduced raw lease=2**80 acceptance; exact integer type checks alone do not bound native lease/version/time values. |
| R5B6 | high | bad_spec | Actual PartyId constructor trims and lowercases invariantly; parent oracle returns literal spaces/uppercase. Materialized identities and normalized duplicate detection diverge from native semantics. |
| R5B7 | medium | patch (superseded by loopback) | git check-attr confirms text/eol unspecified for authoring_outputs, evidence_boundary_current, config_utils, verify_story_completion_workflows and runtime TRX. Autocrlf conversion can change newly bound/published bytes. |
| R5B8 | medium | patch (superseded by loopback) | IsTestSignature accepts any supported void signature, including genuine Fact methods marked AsyncStateMachine. No later attribute check excludes xUnit v3 unsupported async void. |
| R5B9 | medium | patch (superseded by loopback) | Parent added run-level Error to an accepted genuine TRX; parse_native still accepts. Successful test counters do not erase a native cleanup/run failure. |
| R5B10 | medium | bad_spec | Positive restart fixture uses a non-contract TenantDisabled payload; replacing it with not JSON and refreshed hashes also passes. Valid measured pending event/tenant identity is absent from the protocol. |
| R5E1 | medium | patch (superseded by loopback) | Parent replay with actual TenantUpdated description null passes semantics but fails recovered-state schema. Actual Tenants record and oracle permit null. |
| R5E2 | high | patch (superseded by loopback) | Same reproduced reused redaction identity as R5B1; refresh of expected lists hides the conflict. |
| R5V1 | medium | patch (superseded by loopback) | Pre-verified regression gap: all insertion tests use fixture scope, which skips runtime PUBLICATION_MISSING. Existing runtime fixture covers evaluation only; add actual runtime insertion and removal sensitivity. |

| R5P1 | high | clarification during re-derivation | Existing writer calls (LastAppliedEventPosition, DispatchId) its legacy generation, which differs from Epic16 v2 lifecycle generation. Keep separate captured values and an unequal positive; do not misattribute a future v2 capture DTO to current runtime code. |

Every finding was graded individually before grouping. R5B1/R5E2 share the reused
redaction identity root. Other surviving roots are distinct. R5B2/R5B6/R5B10 need
non-frozen protocol completion and trigger this fifth re-derivation; all patches
remain explicit. No finding is deferred. Parent reproductions are retained under
review-iteration-5/parent-before-corrections.json.

### Independent review iteration 6 — workflow limit reached

The fifth re-derivation passed its focused verification, but the next independent
review found remaining protocol omissions. The implementation is **not accepted
or complete**. Every finding below was graded before grouping; none matches an
unchanged prior finding closely enough to carry its previous disposition.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R6B1 | high | bad_spec | `native_binary()` requires the source-stamped assembly and binds its bytes, but `verify_inserted()` permits only native TRX, receipts and the final pair in the publication commit. The new runtime publication test leaves `artifacts/native/evidence.dll` untracked. Including it violates the exact publication inventory; a fresh checkout cannot recover it from that publication. Define and derive the retained binary inventory. |
| R6B2 | medium | bad_spec | Native admission accepts only absolute codeBase paths beneath the current repository prefix. An unchanged accepted TRX from another checkout therefore fails PATH_INVALID after relocation, even with the matching binary at its relative location. Publication must define portable capture or explicitly bound relocation without weakening containment or modifying retained native bytes silently. |
| R6B3 | high | patch (pending; loopback limit) | Parent changed the three-future fault's prior horizon to 999 and refreshed restoration hashes; schema and semantics still accept observed horizon 14. The relation uses max(deliveries) instead of retaining the prior horizon. |
| R6B4 | high | bad_spec | `duplicateFutureState` and `conflictingFutureState` are only outcome strings. The merge schedule contains unique authoritative pending fingerprints but no identical repeated delivery or conflicting payload and persisted outcome; AC-16.1-03 explicitly requires both behaviors. Extend the measured schedules instead of treating labels as proof. |
| R6B5 | high | bad_spec | Parent relabelled the measured tenant and both Gapped native snapshots while leaving every Healthy restart observation unchanged; schema and semantics pass. Healthy snapshot/consumer shapes have no tenant or storage identity, so they cannot establish the same tenant's persisted authorized/denied decisions. |
| R6B6 | false | reject | The smaller creation-plus-participant history passes, but AC-16.2-02 requires deterministic canonical state for identical authoritative inputs, not a fixed minimum set of event types. The oracle derives complete domain values for that legitimate history and still requires multiple stable collection keys. Dropping events does not itself demonstrate incorrect replay; imposing the reviewer's fixed history would add an unstated requirement. |
| R6B7 | medium | patch (pending; loopback limit) | Parent reduced readiness to one attempt and one millisecond, including its diagnostic; both validators pass despite the independent three-attempt/1,000-ms controls. Require a reached retry/timeout terminal condition, not only upper bounds. |
| R6B8 | high | bad_spec | The only measured preflight diagnostic is captured during the Dapr collision. Unrelated occupancy exists in the before/after ledger, but `unrelatedCode: null` has no independently measured unrelated-only preflight result. AC-16.3-02 explicitly requires that negative control. |
| R6B9 | high | bad_spec | Parent retained recovered checkpoint three while both watermarks were zero; checks still claim newer-progress fences. There is no competing worker, lease/version expectation or persisted global-position/generation schedule proving protection of newer progress. Add that evidence; do not compare aggregate sequence numerically with EventStore global positions, which are different domains. |
| R6B10 | medium | patch (pending; loopback limit) | Actual opaque-ID constructors use .NET ThrowIfNullOrWhiteSpace. The native predicate accepts U+001C–U+001F, but Python strip in domain.text rejects them; the parent reproduced all four. Correct the existing predicate with cross-runtime boundary coverage. |
| R6E1 | medium | patch (pending; loopback limit) | Independently verified same opaque-ID whitespace rejection as R6B10 at domain.text/identifier. Native-valid serialized identifiers fail the current oracle. |
| R6V1 | medium | patch (pending; loopback limit) | Pre-verified regression gap: trusted-entry tests cover 16.1, qualified runtime/publication covers 16.2, and 16.3 cases either bypass entry in fixture scope or stop at missing entry. No test reaches its additional appHostOwnership requirement. Cover missing, unqualified and PASS ownership through the existing disposable trusted-entry mechanism. |

After individual grading, only R6B10/R6E1 share a root cause. Other roots remain
distinct. R6B1/R6B2/R6B4/R6B5/R6B8/R6B9 require non-frozen specification and protocol
corrections; the patch entries remain outstanding and are not deferred. The
counter advances from 5 to 6. The effective bmad-build step-04 instruction says:
**"If it exceeds 5, HALT and escalate to the human."** Consequently no sixth
re-derivation, code reversion, patch implementation or completion transition was
performed. The entire current implementation and its verification evidence remain
available for review. Continuing requires an explicit override of that workflow
limit, not another authorization of the already-approved prerequisite scope.

Review input: `/tmp/epic16-review6-zbl1gf6i/complete.diff` (9,799,008 bytes).
Parent counterexamples and this disposition are retained under
`module/validation/epic-16-prerequisite-implementation/review-iteration-6/`.
Tasks 2–5 are reopened for the unresolved protocol/correctness/coverage work;
task 1's actual CLI qualification and task 6's honest handoff remain recorded.
Story 16.1 remains draft/backlog, runtime hold ACTIVE, and accepted pins unchanged.

### Independent review iteration 7

Every finding was graded individually before grouping. None is the same unchanged
location-and-claim as an earlier disposition. Edge review initially could not decode
forced-text PE bytes in the diff; after a lossless Git binary-patch representation
was supplied, it completed and returned no findings. No source or evidence bytes
were changed to repair the review representation.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R7B1 | medium | defer (concurrent user work) | The default-discovered Story 5.4 test reads a hardcoded external administrator evidence directory in setUp. A fresh checkout without that packet cannot run it. It belongs to concurrent commit 8748a6b6e80b3ecbae2bb0174b43f40e30e6a148 and is independent of Epic 16 prerequisite intent; preserve it. |
| R7B2 | medium | patch (included in loopback) | Effective attributes set diff for retained PE DLL bytes and runtime binaries. The full review diff contained invalid UTF-8 PE bytes and blocked a reviewer until represented as Git binary patches. Add narrow binary overrides. |
| R7B3 | medium | bad_spec | The helper copies no global.json into its temporary workspace, bypassing the root SDK policy and omitting actual helper toolchain attribution. Parent observed policy absent there; both current SDK selections happen to be 10.0.402, so actual version drift is not claimed. |
| R7B4 | medium | patch (included in loopback) | Parent retained valid nullable creation fields and derived beforeProjection: semantics accepts, but AC-16.3-03 schema rejects None as non-string. Snapshot schema contradicts actual native optional metadata. |
| R7B5 | high | bad_spec | Parent set B=C=10 while boundarySequence=2/end=3 remains; schema and evaluator accept. No event-position evidence derives the per-stream boundaries, allowing an impossible advance through an unchanged global cutover. |
| R7B6 | high | patch (included in loopback) | Parent assigned different message/delivery IDs to the same tenant/stream/sequence/global position and refreshed durableReference; both validators accept conflicting immutable authoritative identities. Existing reference fields suffice to reject this collision. |
| R7B7 | high | bad_spec | Both lifecycle/index CAS versions can be zero and schema/evaluator still accept. No captured successful shared transaction, complete before/after component state or attempted stale plan proves atomic lifecycle fencing. |
| R7B8 | medium | bad_spec | Every stale attempt has the current lease and checkpoint; only version, expiry, generation and watermark are isolated. Lease-only/checkpoint-only branch removal can escape the claimed conformance experiment despite comparison of those fields in the oracle. |
| R7B9 | high | bad_spec | Equal durable/ack timestamps pass for erase and pending-failure observations, which lack correlated operation ordinals. Equal times can be valid, but cannot establish the required happens-before relationship; add operation proof instead of banning equality. |
| R7B10 | high | bad_spec | Parent pre-populated every merge with the complete final pending set and horizon 14; schema/evaluator accept without any observed growth or earlier worker attempted completion against enlarged state. The scenario does not prove extension during recovery. |
| R7V1 | medium | patch (included in loopback) | Pre-verified gap: existing terminal goldens exercise envelope shape only. Removing both terminal lifecycle assignments still passes all 15 routes plus two relevant goldens (17 passed); add consumer terminal histories and stale-Open rejection with refreshed canonical hashes. |
| R7V2 | medium | defer (concurrent user work) | Independently verified the same external packet dependency as R7B1. Keep the unrelated Story 5.4 user files untouched. |

| R7P1 | high | clarification during re-derivation | Parent inspected the initial new lifecycle transaction helper: it modeled Ready directly to Erased and Erased directly to Ready in one CAS. Preserved AD-7 explicitly requires Erasing/Rebuilding entry and later completion; retain distinct measured transactions and same-generation completion rather than bypassing that state machine. |

| R7P2 | high | clarification during re-derivation | Parent inspected the initial horizon_extension schedule: replay retained pending fingerprints and the Healthy CAS cleared them. Preserved AD-6 requires the completion CAS to observe no pending fingerprint; consume verified evidence during replay and require an already-empty completion input. |

| R7P3 | medium | patch during re-derivation | Parent copied the fixed helper to isolated roots with global.json containing malformed JSON and an array. Both _helper setups returned HelperUnavailable because dotnet ran before policy validation; validate syntax/shape first so invalid policy is FAIL and missing compatible SDK remains BLOCKED. |

Only R7B1/R7V2 share a root cause; record their one deferred entry once. Other
roots remain distinct. The six bad_spec groups require non-frozen amendment and
re-derivation; patch findings remain explicit requirements. The counter moves
from 6 to 7 under the user's existing override, without another approval request.
The 35 owned implementation files are hash-preserved before reversion. Runtime
Story 16.1 remains draft/backlog with its hold ACTIVE and accepted pins unchanged.
Review input `/tmp/epic16-review7-w65r426l/complete.diff` has 13,190,134 bytes and
SHA-256 `d4c11b91eaf064b3861b1f763bc6296583c9dbf0f6d3ed95a6f9d5a308a8cdc2`.

### Review round 8

All three reviewers completed before triage; verification-gap review returned no
findings. Each row was graded separately before grouping. Parent counterexamples
are retained in `review-iteration-8/parent-counterexamples.json`.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R8B1 | high | bad_spec | Parent refreshed every lifecycle snapshot after setting completed watermark 999 with C=20 and queued position 21; schema and evaluator still accept. The upper progress is unsupported by completed observations and could skip pending work. |
| R8B2 | medium | patch (included in loopback) | Native-valid configuration version="customer-setting" independently replays to the observed recovery result, but fault validation rejects it as a structural integer. Arbitrary native map keys are incorrectly interpreted as capture field names. |
| R8B3 | medium | patch (included in loopback) | Correlated accepted delivery or record IDs consisting of a space pass schema and semantic checks; native opaque IDs require nonblank text. Existing predicate/fields suffice. |
| R8B4 | false | reject | AC-16.1-02 requires identical persisted/reloaded sequence and freshness, which raw snapshots and consumer observations bind; representability, generated>=lastApplied and exact lag are checked. Neither that contract nor AD-6 equates TenantEnabled/Disabled payload business dates with projection freshness ticks. Comparing fixture business dates to 1000/1200 does not establish a violated clock relation; AD-8's selected-event rule binds Conversations replay, not Tenants restart. |
| R8B5 | high | bad_spec | Parent set newFence=999 without a measured acquisition or old-worker attempt; evaluator accepts fenced-resumption. No actual persisted replacement lease/version and rejected stale attempt establishes restart safety. |
| R8B6 | high | bad_spec | Parent refreshed queue components for repeated/reordered positions and both validators accept. Those arrival patterns can be legitimate; the demonstrated gap is that no authoritative delivery identity or durable enqueue/drain inventory exists to prove every post-C reference survives cutover. |
| R8B7 | high | bad_spec | The complete lifecycle positive has no executed query observations at any intermediate or cutover snapshot and is accepted. queryWrites=[] cannot establish that concurrent queries ran or returned fail-closed state. |
| R8B8 | medium | bad_spec | AC-16.2-05 accepts a bare Rebuilding label while AC-16.2-03 maps componentGenerations mismatch to Unavailable. AD-7 and current query source distinguish internal lifecycle-record contradiction from cross-key partial-generation mismatch; capture and derive both contexts explicitly. |
| R8B9 | high | bad_spec | Parent set elapsed cancellation to zero with no request/completion/cleanup operation observations; both validators accept bounded safe cleanup. Zero itself can be valid, but the scalars establish neither executed cancellation nor its bound. |
| R8B10 | medium | defer (carried) | carried R7B1/R7V2: same unchanged concurrent Story 5.4 external packet dependency. Preserve user code; existing deferred entry stands without duplication. |
| R8E1 | medium | defer (carried) | carried R7B1/R7V2: same unchanged external packet setUp dependency. The reviewer's guard snippet was a proposed fix, not current code; do not patch this unrelated file or append another deferral. |

| R8P1 | high | clarification during re-derivation | Early source audit found enqueue snapshots advancing the durable queue while lifecycle work.before still equaled the earlier empty entry snapshot. Bind complete intermediate enqueue CAS snapshots and the actual work/drain before-state as amendment 5 already requires. |
| R8P2 | high | clarification during re-derivation | The initial isolated restart attempts proposed Healthy while clearing pending evidence, independently forbidden by AD-6. Use an otherwise admissible pending-preserving write and matching current-fence positive so rejection isolates the named precondition. |
| R8P3 | medium | clarification during re-derivation | Initial queue validation required every delivery stream in the manifest discovered at B. A valid new tenant ConversationCreated after C cannot be in that earlier manifest; independently bind its stream identity and retain its reference for normal delivery. |

The six bad_spec roots remain distinct. Two small patch roots are included in the
amendment; both carried external-packet rows reuse the existing disposition. Counter
7 becomes 8 under the user's prior approval. Hash-preserve 36 task-owned files before
reversion; do not revert the unchanged bound global.json. Full review diff is
`/tmp/epic16-review-next-d_ao7pn1/complete.diff`, 18,262,821 bytes, SHA-256
`0e56651ea2bbfa5d6610869be69581a14addb8666a640d5995629095ed44a5fa`.

### Review round 9

All three reviews completed before individual grading. The verification layer filed
one Other finding (verified normally) and no separate coverage gap. Parent schema,
semantic, subprocess and retained-artifact probes are in
`review-iteration-9/parent-counterexamples.json`. The first parent inventory probe
used an expired pytest temp path and stopped; the successful probe reconstructed
all exact retained bytes instead. No product failure is inferred from that audit setup.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R9B1 | medium | defer (preexisting CI selection) | pyproject/npm/CI selection omits _bmad/scripts/tests despite tracked routine-policy wording. All three files are byte-identical to original baseline; this is the known broad-lane configuration discrepancy, while the new suite actually ran explicitly. Record one separate deferred entry and preserve the baseline scope. |
| R9B2 | false | reject (carried) | carried R3B4: same contextual clockTicks versus completion-time claim at the unchanged AD-8 freshness checks. Different contextual clocks do not require distinct completion instants; valid anchor/completion equality and zero lag remain allowed. Do not reverify or impose an unstated clock equation. |
| R9B3 | high | bad_spec | Schema/evaluator accept the positive with only unorderedCollectionPersisted=false and generationBeforeAnchorState=Unavailable. No invalid input, executed rejected operation or before/after state establishes either required negative control. |
| R9B4 | medium | patch (included in loopback) | Parent substituted UnsupportedValue for the v1 lifecycle and unavailable provider state; schema and semantics accept. Known capture domains need typed admission before those values can stand in for required cases. |
| R9B5 | medium | bad_spec | The warning-removal route accepts warningRemoved=true with no warning-surface observation. Revalidated restart/convergence are prerequisites, not proof that the current Program.cs single-replica declaration was removed; historical evidence must remain intact. |
| R9B6 | high | patch (included in loopback) | Parent reconstructed the genuine admitted fixture, accepted its six retained artifacts, replaced all six contents and obtained the same inventory with every digest wrong. The final inventory boundary does not reread bytes or aliases; add that check without claiming absolute concurrent-mutator protection. |
| R9B7 | high | bad_spec | native_binary and the closed native capture bind only the test PE assembly. No loaded production binary inventory relates the actual Conversations/Tenants/EventStore assemblies to the candidate/gitlinks; test stamping and clean source alone cannot detect stale product output. |
| R9B8 | high | bad_spec | verify_public_surface hardcodes only src/Hexalith.Conversations.Contracts and root source equality. AC-16.1-05 also explicitly requires upstream additive surfaces, but no dependency baseline/current API evidence is read or compared. |
| R9B9 | high | bad_spec | Parent accepted the reconciliation positive with wireRoute/authenticated/coordinator labels but no executed wire request/response capture. Existing durable snapshots/catalog/fences do not demonstrate the explicitly required production request path or its correlation to clearance. |
| R9B10 | medium | bad_spec | AC-16.3-01 explicitly requires bounded retry advice; the accepted captured diagnostic contains only code, endpointId, owner, port and attempts, and its closed schema cannot represent advice. |
| R9E1 | medium | patch (included in loopback) | Parent removed dispatch from baseline, snapshots and reads; schema accepts and evaluator raises StopIteration at next(...). Require the demonstrated component inventory before selection and emit structured failure. |
| R9E2 | high | patch (included in loopback) | Parent changed the dispatch tenant everywhere to foreign-tenant, or added pending dispatch absent from the index; schema and semantics still accept Current and AC-16.2-05. Bind existing component identity and dispatch values to the control record. |
| R9E3 | medium | defer (carried) | carried R7B1/R7V2 and R8B10/E1: unchanged concurrent Story 5.4 external packet dependency. Keep the existing deferred entry; no new duplicate and no patch to user work. |
| R9V1 | medium | patch (included in loopback) | Parent copied declared executors, deleted only epic16_capture.py and invoked all three verifier wrappers. Each exits 1 with empty stdout and ModuleNotFoundError before main; structured unavailable-source handling currently covers Program.cs, not Python import dependencies. |

After individual verdicts, R9E1/E2 share incomplete cross-key baseline admission but
retain both verified outcomes. The six protocol omissions remain distinct bad_spec
roots; the other patch roots are included in re-derivation. R9B1 receives one new
baseline deferral; R9E3 reuses the existing disposition. Counter 8 becomes 9 under
the existing user override. All 36 owned files are hash-preserved before reversion.
Review diff `/tmp/epic16-review-next-di3w43lp/complete.diff` is 21,393,361 bytes with
SHA-256 `0b22fe8574d772d57a089763192794b22b53cb832ebb0ac159ee1c14b51c4a93`.

### Independent review iteration 10

All three reports completed before triage. No claim is silently combined. The
verification reviewer reported no new gaps. The parent verified the new claims
against callers and retained independent counterexamples; none repeats the same
location/claim/state of an earlier carried row.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R10B1 | high | patch (included in loopback) | Parent admitted a loaded Tenants.Client DLL and independently admitted API evidence with the same source revision but different SHA-256 and public APIs. products.upstream has no link to native_binary products; the API declaration proof can describe an unexercised binary. |
| R10B2 | high | patch (included in loopback) | Parent changed Server and Tenants.Client binaries for only AC-16.2-05, retained valid identities/revisions and refreshed derived receipts. All five actual runtime evaluations and generate still return PASS, retaining ten assemblies; final acceptance never requires one product implementation across its scenarios. |
| R10B3 | high | patch (included in loopback) | Two compiled External assemblies with different signing public keys produce identical inspected Probe.Api parameter/return inventories and additive PASS. SignatureNames retains only AssemblyReference.Name, omitting the identity that distinguishes those types. |
| R10B4 | medium | patch (included in loopback) | Parent compiled the SDK-compatible 1.0.0+existing.<revision> stamp: product inspection accepts it while test inspection rejects it solely at the direct +revision suffix check. |
| R10B5 | high | patch (included in loopback) | Extra conflicting TestMethod and Execution elements, separately and together in retained genuine TRX, all pass parse_native with 1/1/0/0 counts. find() selects the first definition child without enforcing cardinality. |
| R10B6 | medium | bad_spec | Parent compiled a genuine pinned xUnit Fact with an unprovided string constructor parameter. The actual pinned runner exits 1 with a failed test, while the metadata inspector still accepts it. Supported constructor-provider metadata must be defined without rejecting valid fixture/output-helper injection or claiming to prove arbitrary constructor execution. |
| R10B7 | high | bad_spec | Parent retained identical committed Contracts files and changed only external Directory.Build.targets to remove an unrelated public type. Both builds succeed, the actual inspected public API changes, and verify_public_surface still passes. Source-directory equality does not establish the promised compiled public shape. |
| R10B8 | high | bad_spec | AC-16.2-03 schema/consumer accepts authorization observations containing status/reason and zero reads, but there is no returned public-response content. The protocol cannot distinguish a non-disclosing response from one carrying cached content. The later lifecycle/query experiments do not cover these unauthorized/nonexistent response cases. |
| R10B9 | medium | patch (included in loopback) | A disposable successor module that raises an internal ImportError is converted by the generator into BLOCKED/SCHEMA_UNAVAILABLE/exit 2 with no traceback. The verifier wrappers correctly distinguish missing declared modules from implementation defects; the generator does not. |
| R10B10 | false | reject | The paragraph first introduces a pre-existing authorizationRevision; entry/trust can follow that revision, and the implementation candidate contains those bytes. Existing test_entry_can_be_committed_after_authorization_without_a_hash_cycle and qualified-runtime evaluator tests execute this ordering and passed in the final suite. No after-candidate admission or hash cycle is promised or reproduced. |
| R10E1 | high | patch (included in loopback) | Parent independently re-inspected both retained compiled APIs: IFoo? to IFoo constraint tightening remains byte-equivalent in api and passes additive. The retained caller changes from exit 0 to CS8631/exit 1. GenericParameterConstraint custom attributes are omitted even though other generic attributes are retained. |

The ten surviving findings have distinct proof sites and outcomes. R10B6/B7/B8
require non-frozen protocol amendments and trigger re-derivation; the seven narrow
corrections are included in that work. R10B10 is rejected. No new deferred item is
created. The existing user authorization overrides the five-iteration limit, so
review_loop_iteration advances from 9 to 10 without another permission request.

**KEEP extracted before reversion:** preserve all earlier KEEP sets and every
accepted iteration-9 correction, including seven product owners/capture-v2 native
retention, rejected replay execution, actual warning surface, safe wire response
outcomes, bounded retry advice, closed query domains, missing-import boundaries,
final artifact rereads, abstract obligations and class/value signature markers.
Keep all 2,038 passing combined cases (1,876 successor including 288 new; 162
historical), 72 legacy, 41 demonstrated guards/43 runs/192 assertion failures with
zero setup errors/skips, 51 parent semantic probes and nine missing-import probes,
the independent signature correction, all 879 hashed artifacts and every honest
intermediate epoch. Keep the exact genuine 25-path/eight-assembly native fixture
and its 30 retained committed files as historical proof; future inventories remain
derived when more necessary artifacts are added. Preserve all eight matrix rows,
18 IDs/32 requirements/38 nodes/61 edges, rollback, exact predecessor pins,
all validated fixtures and 32 disposable messages, null actual entry/empty trust,
18 held commands, both completion guards, 54 absent paths, accepted CLI 4.35.2,
unchanged runtime/dependencies/SDK and protected concurrent user Story 5.4 work.
Do not reinterpret valid zero lag, initialized-empty state or authorization ancestry.
Keep the two existing deferrals and established broad failures without rerunning
the broad lane or creating duplicates. Frozen runtime authorization is unchanged.

Counterexamples, raw reports and 114 retained native/source/build artifacts are
indexed under [review iteration 10](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/reviews.md),
[parent results](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-counterexamples.json)
and [proof manifest](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-proof-manifest.json).

### Independent review iteration 11

All three reviewers completed before grading. The verification layer reported no
new gaps. Every other new claim was checked at its caller and guards. Independent
compiled and protocol counterexamples are retained under
`review-iteration-11/parent-metadata-proof/` and `parent-protocol-proof/`; the full
review reports are in `review-iteration-11/reviews.md`.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R11B1 | high | patch (included in loopback) | Parent compiled IBase<string> versus IBase<string?> inheritance: API inventories are equal and additive accepts; the same real nullable consumer changes from build exit 0 to 1. InterfaceImplementation custom attributes are omitted. |
| R11B2 | high | patch (included in loopback) | Parent compiled two existing Stamp constructors with distinguishable int/uint behavior and changed only the applied overload. Equal attribute blobs and type names collapse the different constructor tokens/signatures, so additive accepts the changed attribute declaration. |
| R11B3 | high | patch (included in loopback) | Parent compiled a ComImport interface with and without PreserveSig. Both native inventories are equal and additive accepts the changed interop signature because MethodDefinition implementation flags are discarded. |
| R11B4 | high | bad_spec | Parent changed assembly CLSCompliant true to false; both inventories compare equal and additive accepts while an actual CLS-compliant consumer changes from build exit 0 to 1. Assembly/module contract attributes need a defined comparison boundary with narrow build-stamp exclusions. |
| R11B5 | medium | patch (included in loopback) | Parent compiled AssemblyFlags.Retargetable absent/present; represented owning identity and API compare equal and additive accepts. Owning assembly flags are omitted although referenced identity retains them. |
| R11B6 | medium | patch (included in loopback) | All three copied wrappers import an otherwise present epic16_tooling module containing import epic16_typo and emit BLOCKED/exit 2 instead of the internal defect traceback. The broad prefix test does not establish a genuinely absent declared dependency. |
| R11B7 | medium | defer (carried) | carried R9B1: same default CI/npm/pytest selection claim and unchanged configuration. The explicit prerequisite suite ran; retain the existing deferred entry and do not patch, reverify or defer this baseline issue again. |
| R11B8 | high | bad_spec | Parent admitted the closed AC-16.1-04 schema and complete measurement evaluator with only named outcome, record/delivery IDs and times/orders. No successful transaction identity, executed commit/ack observation or persisted record contents can be expressed by these closed fields, so the durable-ack proof cannot establish the actual outcome. |
| R11B9 | high | bad_spec | Parent admitted the closed AC-16.3-04 schema and evaluator with six stale records and duplicate retry lacking execution/completion observations. The separate executed wire chain covers current success only, not these negative and duplicate operations. |
| R11B10 | medium | patch (included in loopback) | The primary README promises exactly five native output members; parse_native accepts that fixture shape but runtime products.loaded requires loadedProducts and emits PRODUCT_INVENTORY_INVALID when absent. Later product documentation does not correct the contradictory primary producer instruction. |
| R11E1 | high | patch (included in loopback) | The parent metadata inspector accepts a real compiled Fact+Theory scenario. The actual pinned xUnit runner exits 1 with multiple [Fact]-derived attributes and never executes it; counting only exact Fact attributes misses the conflicting Theory. |
| R11E2 | medium | patch (included in loopback) | Parent repaired the reviewer's incomplete clone: two distinct source-derived conversations, correct encoded keys/digests and complete transaction/component/query captures pass schema, erase-target, lifecycle-transaction and erase-delivery checks. The full evaluator then fails because entry index has one dispatch reference while its two per-resource components each contain that reference. |

These distinct proof sites retain separate verdicts. R11B4/B8/B9 require
non-frozen protocol clarification and trigger re-derivation; include the other
eight corrections in that work. R11B7 carries the existing deferral with no new
ledger entry. The user's review-limit override continues to apply.

| Parent finding | Verdict | Disposition | Evidence |
| --- | --- | --- | --- |
| R11P1 | high | correction during re-derivation | At the first source checkpoint, an external System.Custom.HiddenFact attribute plus pinned Fact is admitted while the genuine runner rejects multiple Fact-derived attributes. An external aliased AssemblyFileVersion attribute's one/two contract values also collapse and pass additive. Retained compiled proofs are in parent-framework-identity-proof; namespace names do not prove framework ownership. |
| R11P2 | medium | correction during re-derivation | The new capture's inferred schema closes native member/configuration maps to the sample keys. Parent's fully correlated extra-member/configuration capture passes the semantic evaluator but produces 12 schema errors. Retain native map value rules and arbitrary valid keys in every new source/projection location. |

**KEEP extracted before reversion:** preserve all earlier KEEP sets and all
iteration-10 corrections, including R10P1's concrete-type collection inheritance,
29 native constructor controls, seven consistent loaded products, exact API/native
correlation, compiled root public shape, full referenced signing identities and
constraint attributes, actual authorization responses, both SDK stamp forms and
TRX cardinality. Preserve all 2,152 combined and 72 legacy passing cases, all 20
demonstrated guard roots across 24 mutation runs and their 54 assertion failures,
the insensitive intermediate mutation as such, every one of the 1,623 artifact
hashes, the 85-file frozen epoch, 71 bound inputs and all independent parent audits.
Keep the 31 exact native files, 26 publication paths, nine binaries and genuine
fresh-checkout proof. Preserve the root .gitignore evidence-only exception and
the byte-identical archived .gitignore.bytes relocation; this packaging file is
the 42nd owned file, not a frozen runtime input. Keep all actual runtime holds,
null entry, empty trust, accepted CLI 4.35.2, 18/32/38/61 inventories, 18 blocked
commands and 54 absent runtime paths. Preserve the user Story 5.4 commit/files,
existing deferred ledger, unchanged global.json and every historical failure.
No runtime implementation, broader CI repair, dependency updates, authority lift,
actual Git staging/commit/push or repeated broad test lane is authorized.

### Independent review iteration 12

All three context-free reviewers completed before grading. The verification layer
reported no gaps. Raw reports are retained in `review-iteration-12/reviews.md`;
independent compiled and schema/semantic counterexamples are in
`review-iteration-12/parent-proofs/`. The optional extension of the fake test-stamp
probe to product inspection was rejected and is recorded as such, not counted as
an additional defect. Each reviewer finding receives its own verdict below.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R12B1 | high | patch (included in loopback) | Parent inspected the exact compiled assembly with only a locally defined AssemblyInformationalVersionAttribute and a genuine Fact; test metadata admission reports the requested source revision. Namespace/name recognition does not establish a genuine framework source stamp. The same fixture is rejected by the product inspector; no product-admission claim is inferred. |
| R12B2 | medium | patch (included in loopback) | Parent admits the unsigned System.Threading.Tasks 1.0.0 custom Task return type. The real pinned runner also passes this null-returning method, so this is not a discovery failure; the inspector nevertheless misclassifies an unrelated type as the supported framework asynchronous return, without establishing its actual ownership. |
| R12B3 | high | patch (included in loopback) | Parent admits the retained foreign ParamArrayAttribute metadata fixture; the actual pinned xUnit runner exits 1 because String[] values has no matching fixture data. Constructor supply must recognize the actual framework attribute, not an external-name suffix. |
| R12B4 | high | bad_spec | The closed AC-16.1-03 schema and complete evaluator accept all four reverse-save schedules with source-derived decisions but no executed writes, CAS expectations/results, durable snapshots, or identified replica store reads. The separately captured recovery schedule cannot establish these four reverse-save executions against shared persisted state. |
| R12B5 | high | bad_spec | Parent admits AC-16.1-01 and AC-16.1-02 with six independent state/allowed/sequence rows. Their closed shapes cannot express underlying state/freshness or actual consumer execution; Unknown and Stale denial are not derived from persisted inputs. Healthy/Gapped restart observations elsewhere do not prove these distinct six-state requests. |
| R12B6 | high | bad_spec | Parent admits AC-16.1-04 with provider-failure and retry-exhaustion rows containing only booleans/counts/IDs and null durable fields. The fault rows contain three failed labels but no correlated executed attempt/completion inventory for those deliveries, so unexecuted processing is indistinguishable from completed bounded failure. |
| R12B7 | medium | defer (carried) | carried R7B1/R7V2, R8B10/E1 and R9E3: the same unchanged concurrent Story 5.4 external evidence directory dependency. Preserve the user files and existing ledger entry without re-verification, patching or another deferral. |
| R12B8 | medium | defer (carried) | carried R9B1/R11B7: the same default pytest/npm/CI selection discrepancy in unchanged baseline configuration. The explicitly selected prerequisite suite ran; preserve the existing deferral without repetition. |
| R12B9 | medium | patch (included in loopback) | Parent supplied reachable SDK/build subprocess failures with stderr-only diagnostics and nonzero exits. HelperUnavailable omits both exit codes and stderr; the build message has an empty suffix. Preserve bounded diagnostic streams and exit information for prerequisite troubleshooting. |
| R12B10 | false | reject | Recursive inspection confirms twelve byte-equivalent expanded snapshot schema objects; the finding identifies no divergent definition or incorrect caller admission. Current schema/source audit and executed tests cover the changed maps consistently. Duplication alone does not establish the claimed validation defect and does not justify a representation refactor in this correction. |
| R12E1 | high | patch (included in loopback) | Parent reuses each of the entry/work/completion transaction IDs in the first durable enqueue, updating all existing links. All three captures pass schema and the full evaluator even though the same ID denotes different ordered CAS states. The enqueue uniqueness set omits lifecycle stage transactions. |

After individual grading, B1/B2/B3 share unverified framework identity recognition
but retain their distinct demonstrated outcomes. B4, B5 and B6 are separate
missing execution protocols and trigger a non-frozen amendment and re-derivation.
B9 and E1 are separate direct corrections included in that work. The two carried
deferrals receive no new ledger entries; B10 is rejected. No intent gap or runtime
authorization change is needed. The existing review-limit override applies.

**KEEP extracted before reversion:** retain all preceding KEEP constraints and
all iteration-11 fixes, including interface/constructor/implementation/assembly
metadata, real framework ownership for contract exclusions and Fact discovery,
actual durable outcome contents, stale/duplicate reconciliation execution,
multi-resource erase multiplicity, exact import classification and six runtime
fields. Preserve open native member/configuration maps with value validation,
R10P1 constructor/collection inheritance, all 2,416 combined and 72 legacy cases,
35 demonstrated guards/162 assertion failures, the unselected exploratory run's
honest disposition, all 2,854 indexed implementation artifacts and 86 frozen files.
Keep all 71 bound sources, 31 native fixture files, nine binaries, five identical
seven-product inventories and genuine fresh-checkout reproduction. Preserve all
earlier evidence and parent audits, the narrow .gitignore packaging exception,
five protected files, user Story 5.4 work and existing deferred ledger. All runtime
holds, null entry, empty trust, accepted CLI 4.35.2, 18/32/38/61 inventories,
18 blocked commands, two completion guards and 54 absent runtime paths remain.
No broad-lane rerun, baseline CI repair, runtime implementation, dependency update,
authority lift, real Git staging/commit/push or schema representation refactor.

| Parent finding | Verdict | Disposition | Evidence |
| --- | --- | --- | --- |
| R12P1 | high | correction during re-derivation | Using the exact preserved helper source, parent compiled a product referencing an external aliased AssemblyInformationalVersionAttribute with no genuine framework stamp. Product metadata admission returns the requested revision. This distinct external fixture establishes the shared source-stamp ownership defect in the product path; the earlier local fixture remains a recorded rejection. Apply iteration-12 framework identity/constructor validation consistently and retain the exact compiled proof. |
| R12P2 | high | correction during re-derivation | Parent renamed exact tenant values, native payloads and encoded keys to tenant/a and refreshed matching fingerprints. AC-16.1-01 schema and full evaluator accept allowed=true. The compiled actual Commons evaluator returns MalformedTenant with zero health/store reads. Enforce its native request tenant rules specifically in the new access capture, preserving unrelated opaque identifier semantics. |

## Verification

Run focused new tests, current record-generator regressions (respect the repository's explicit retirement of live Story 11.3 reproduction), current Python lane and Story 7.3 completion-workflow guards. Use exact portable historical fixtures when generator bytes change. Record broad blockers separately; do not weaken tests. No .NET application start is needed for Python/tool-artifact work. Explicit runtime evidence publication remains not-applicable until authorized; held runtime commands must block.


### Observed implementation verification — 2026-10-25

- Final-source combined suite after all review corrections: exit 0, **328 passed**
  (166 successor, 162 historical adoption-v2) in 86.27s. Exact command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --junitxml=/tmp/epic16-final-source.xml`.
  `git diff --check` passes. Earlier runs below overlap this final suite.

- `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --junitxml=/tmp/epic16-final-focused.xml` — exit 0, 318 passed in 117.84s.
- Final Unicode and entry/dependency/native/lifecycle hardening selectors — exit 0,
  16 passed, 142 deselected; `/tmp/epic16-final-hardening.xml`. The two new Unicode
  cases were added after the combined run; affected guards were rerun.
- Both exact Story 7.3 completion commands exit 0. Their native outputs are retained
  in the [new observation directory](../../module/validation/epic-16-prerequisite-implementation/completion-commands.json).
- All eighteen current successor runtime commands return exit 2 with
  `ENTRY_QUALIFICATION_PENDING`; no runtime receipts or final pairs exist. The
  [held-command observation](../../module/validation/epic-16-prerequisite-implementation/held-commands.json)
  binds the final tooling-source inventory.
- The broad current Python lane completed with exit 1: **3,118 passed, 38 failed,
  9 errors, 17 deselected, 295 warnings in 2355.08s**. Package-refresh contributes
  33 failures and nine errors from the existing `REFRESH_UV_DRIFT` baseline; the
  five evidence-boundary failures arise from the existing Story 10.4 guidance
  binding versus current runbook bytes. All 25 relevant checker/test/guidance/
  workflow files match HEAD. The five-test independent reproduction fails again
  in 21.48s. Task 5 execution and assessment are complete; the broad lane remains
  failing, and no gate was weakened. The broad run spans source epochs because
  lazy-dispatch and AD-8 fixes were applied while it ran; final-source affected
  verification is the separate 328-pass run above. Terminal results and log
  hashes are retained in `module/validation/epic-16-prerequisite-implementation/verification-final.json`.
- Runtime publication is not-applicable to this prerequisite scope. Story 16.1
  draft/backlog and the prior investigation are preserved. Real runtime entry,
  owner acceptance and human parity/recovery/SM-2 evidence remain outstanding.

- Final review selectors: 72 standalone/Story 11.1/11.2 regressions passed in
  94.62s and 21 AD-8/positive/final-record regressions passed in 21.17s. Both exact
  workflow guards pass again after these fixes; all eighteen runtime commands
  still block against the refreshed final source binding.


### Re-derived implementation verification — review iteration 1

This section supersedes the pre-review implementation verification above for the
current task-owned source. The historical qualification observations and broad
baseline failures remain intact.

- Replaced entry candidate self-binding with a separately pinned full
  `authorizationRevision` ancestry check. A real disposable graph commits entry
  and trust evidence after that revision and validates a later candidate; wrong
  story and available non-ancestor revisions fail. Actual `binding.entry` remains
  null and `TRUSTED_ENTRIES` remains empty.
- Added a fixed .NET helper using `PEReader`/`MetadataReader` without assembly
  loading or execution. Genuine disposable compiled test metadata passes;
  malformed/text binaries, wrong source revision/method and unsafe paths fail.
  Repository-contained absolute and relative codeBase paths use lexical
  normalization followed by the existing no-follow reader. Unknown or extra TRX
  result nodes, including GenericTestResult, fail.
- Bound pending restart state/denial, future pending sequences, measured recovery
  lease/version/checkpoint/horizon and all four unique reverse-save schedules.
  Replay uses strict finite JSON and the actual pinned EventStore canonicalizer;
  collections bind to serialized items. Retained attributed source permits
  helper compilation and the default Python lane without initialized submodules;
  runtime qualification independently hashes actual clean EventStore source.
- Query status now derives from measured control-index inputs and exact named
  cases, with preserved lifecycle causes and explicit owner-authorized rebuild.
  Erase observations measure both erased phases, non-payload durable references,
  acknowledgement order, exact deletion targets and content absence. Rebuild
  proves the unique tenant stream inventory, B/C boundaries and same-generation
  progress without allocating a range from an artifact-supplied integer.
  Reconciliation binds pending-item, tenant, runtime and catalog identity to the
  recomputed first-failure evidence.
- Final affected command: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=/tmp/epic16-review1-combined.xml` — **exit 0, 437 passed in 194.04s**
  (275 successor including all retained 166 assertions, 162 historical adoption-v2).
  This includes all prior 21 AD-8/positive/final-record selectors and actual
  successful verifier/generator subprocess publication and consumption for all
  three stories.
- Legacy command: `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_epic16_tooling.py -k 'story_111 or story_112 or standalone_generator' --junitxml=/tmp/epic16-review1-legacy.xml` — **exit 0, 72 passed, 1736 deselected in 86.84s**.
- Independent source mutations in disposable copies turn the targeted tests red:
  a no-op CLI receipt write causes all three subprocess publication tests to fail;
  disabling dependency evaluation causes all ten missing/invalid dependency-edge
  tests to fail. Six independently reproduced review counterexamples now reject,
  including the huge stream integer without an overflow/allocation failure.
- Both exact Story 7.3 workflow guards pass. All eighteen held runtime/final-record
  commands return nonempty `ENTRY_QUALIFICATION_PENDING`, exit 2, and publish no
  runtime evidence. `git diff --check` passes.
- Exact commands, separate stdout/stderr, JUnit, hashes, mutation probes and the
  independent counterexample retest are retained additively under
  `module/validation/epic-16-prerequisite-implementation/review-iteration-1/`.
  `verification.json` binds this source and retains the two prior broad baseline
  failures. Per this amended spec, the 39-minute broad lane was not rerun and no
  unrelated gate was weakened. Runtime implementation, real owner entry and
  human parity/recovery/SM-2 remain outside this prerequisite completion.

### Re-derived implementation verification — review iteration 2

This section supersedes prior implementation observations for the current source.
Qualification evidence, predecessor packages and accepted CLI pins remain intact.

- Completed all nine iteration-2 requirements: every pending sequence fits the
  merged horizon; cutover watermark covers C; replay JSON is the closed v2 state
  with a hashed, event-derived UTC anchor; reconciliation binds persisted/query
  sequence to recovered checkpoint and the same measured pending sequence.
- Tenant acknowledgement rows now carry correlated delivery/outcome identities,
  durable-commit and acknowledgement timestamps and strict ordering. Healthy
  authorized and denied cases measure persisted restart sequence/freshness,
  separate process identities, no redelivery, and consumer-observed decisions.
  Gapped restart, all four reverse-save schedules and prior lifecycle proofs remain.
- UTC timestamps and endpoint ports have representable bounds. Helper directory,
  copy and spawn I/O failures yield structured BLOCKED. Caller/loaded-local-helper
  source bytes must equal the bound repository; partial caller installations also
  return structured diagnostics. Program.cs retains its required CRLF bytes under
  an explicit byte-preserving Git attribute.
- Managed metadata resolves the genuine pinned xUnit 4.0.2 assembly identity and
  rejects local lookalikes, wrong framework identities and invisible/non-test
  methods. Genuine async Task/ValueTask methods and xUnit fixture/output-helper
  constructors pass without executing evidence assemblies. A test-only qualified
  runtime repository exercises evaluate() through ancestry, clean dependencies,
  native binary admission and actual canonicalizer-source verification together.
  Actual entry remains null and actual trusted entries remain empty.
- Final combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=/tmp/epic16-review2-final-combined.xml`
  — **exit 0, 548 passed in 220.71s** (386 successor, 162 historical adoption-v2).
  This retains the prior 275 successor cases and adds 111. The separate final
  metadata/runtime selector passed **44 tests** in 49.22s; it overlaps this run.
- Standalone/Story 11.1/11.2 selector passed **72 tests** in 84.03s before the final
  metadata interoperability adjustment. Its generator, historical fixtures and
  selected assertions were unchanged by that adjustment. The final combined run
  also repeats all six standalone dispatch assertions. An exploratory combined
  attempt found five new port fixtures violating the existing two-port schema;
  those inputs were corrected before the final 548-pass run. Source epochs and
  all separate stdout/stderr/JUnit files are explicit in `verification.json`.
- All **19** independent disposable source mutants turn their targeted tests red,
  including removal of the binary caller (4 failures), executing-source check
  (5), canonicalizer-source call (1), actual replay oracle (2), acknowledgement
  observations (30), Healthy restarts (16), framework identity (5), receipt writer
  (3) and dependency loop (10). All six parent review counterexamples reject.
- All eighteen final-source held commands return nonempty **BLOCKED / exit 2 /
  ENTRY_QUALIFICATION_PENDING** and leave runtime receipt/final paths absent.
  Both exact Story 7.3 completion commands pass and prior operational output bytes
  are restored. `git diff --check` passes. Exact commands, source binding, native
  logs and mutation results are retained additively under
  `module/validation/epic-16-prerequisite-implementation/review-iteration-2/`.
- The broad Python lane was not rerun, as required. Its independent package-refresh
  and Story 10.4 guidance-binding baseline failures remain reported. Real Platform
  integration qualification, seventeen adoption findings, provider/environment
  parity, recovery drills, operator coverage, capacity/waiver disposition, human
  SM-2 and separate owner entry remain outstanding. Runtime implementation and
  publication are outside this prerequisite completion; Story 16.1 remains draft
  and its sprint entry remains backlog.


### Re-derived implementation verification — review iteration 3

This section supersedes earlier implementation results for the current source;
all prior qualification, review and failed/interrupted verification observations
remain retained. The frozen prerequisite scope and actual runtime hold are unchanged.

- Native identity now consumes the real fully qualified xUnit class/method while
  retaining the AC in JSON. A genuine disposable test compiled/executed with the
  pinned xUnit 4.0.2 runner emits complete, untouched TRX that passes the consumer
  and metadata inspector. The consumer never executes supplied evidence assemblies.
  Encoding-aware Expat rejects UTF-16 DTD/entities and unknown encodings yield
  `NATIVE_INVALID`. Generation resolves one immutable candidate and rejects any
  returned scenario/receipt candidate mismatch.
- Closed v2 replay state includes event-derived conversation content, business
  references, participant roles/types, message content, file references, attributes
  and processed identities. Both rebuilds must match retained authoritative history
  through the actual EventStore canonicalizer. Full duplicate bindings match the
  independent envelope and actual contract metadata/payload shape. Genuine Contracts
  serialization goldens cover numeric schema versions, typed identifier prefixes,
  computed aliases, business-reference objects and DateTimeOffset formats. Exact
  UTC arithmetic rejects invalid offsets, UTC overflow and non-ASCII date digits.
- Tenant convergence compares pending fingerprints to contiguous authoritative
  records and derives recovered state and all four reverse-save decisions from
  actual Tenants transitions. Reconciliation correlates prior history and the same
  full-bound pending event, then compares corrected domain values and checkpoint
  with persisted and production-query results. Public preservation independently
  reads complete baseline/candidate Contracts source inventories and blobs; this
  is conservative source preservation, not a binary compatibility claim.
- Trusted contracts pin the existing readiness/cancellation experiment controls.
  Effective endpoint identity, ownership, before/during/after occupancy, unrelated
  owners and retained structured diagnostics are validated. All twelve tenant and
  eight live faults have typed distinguishing inputs/outcomes, exact targets,
  restoration bytes and independently derived assertion entries/counts. Raw
  boolean/float aliases and invalid starting tenant-state primitives fail. The
  complete fifteen-scenario preservation audit is in the tooling README.
- Final combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=module/validation/epic-16-prerequisite-implementation/review-iteration-3/final-utc/combined.xml`
  — **exit 0, 752 passed in 317.75s** (590 successor, including all prior 386,
  and 162 historical adoption-v2). The 72 standalone/Story 11.1/11.2 checks passed
  in 88.18s before later domain-only refinements; their generator/fixture/assertion
  sources were unchanged, and all six standalone cases also pass in the final
  combined run. No broad lane was rerun.
- All **26 final-source isolated source mutants** cause nonempty targeted test
  failures with no setup errors. These include domain values, full envelope binding,
  authoritative pending fingerprints, recovered values, reverse event effects,
  actual canonicalizer invocation, corrected projection, independent public-source
  reads, controls/ports/diagnostics, fault assertions, native admission, receipt
  writing, dependency evaluation, raw primitive types and timestamp bounds. All
  four parent counterexamples reject. Earlier mutant results remain preserved.
- All eighteen final-source runtime/final-record commands remain **BLOCKED / exit 2 /
  ENTRY_QUALIFICATION_PENDING** and leave runtime paths absent. Both exact Story 7.3
  completion commands pass, with prior operational output bytes restored.
  `git diff --check` passes. Exact commands, separate native streams, JUnit, source
  bindings, mutation ledgers, genuine wire goldens/provenance, parent probes and
  source-epoch dispositions are retained in
  `module/validation/epic-16-prerequisite-implementation/review-iteration-3/verification.json`.
- Earlier exploratory results remain explicit: 382/4 before protocol-aware fixture
  corrections, 168/1 and 732/1 from stable-code/checkpoint-control expectations,
  one retry-list typing correction, and the deliberately interrupted pre-UTC-fix
  verification epoch. They are superseded by the final 752-pass run. AD-8 still
  allows coincident completion timestamps when each freshness relationship holds.
- The known `REFRESH_UV_DRIFT` and Story 10.4 guidance-binding broad baseline failures
  remain unresolved and unweakened. Platform integration qualification, seventeen
  adoption findings, provider/environment parity, recovery drills, operator and
  capacity/waiver decisions, human SM-2 and separate owner entry remain outstanding.
  Actual entry is null, trust pins are empty, accepted CLI pins stay 4.35.2, Story
  16.1 remains draft/backlog, and runtime publication is not-applicable here.


### Re-derived implementation verification — review iteration 4

The additive implementation is complete for the authorized prerequisite scope.
This section preserves all earlier qualification observations and supersedes prior
implementation-source results with explicit source epochs. Runtime entry remains
null/untrusted and Story 16.1 remains draft/backlog.

- Restored the 31 task-owned preservation files after verifying their manifest
  hashes, then added a cohesive relationship evaluator and local protocol goldens.
  All 18 scenarios, 32 requirements, 38 graph nodes, 61 edges, rollback boundaries,
  predecessor pins and exact historical fixtures remain unchanged.
- Corrected actual wire/type semantics: genuine closure/archive serializer output
  with null/non-null reasonCode, general Tenants DateTimeOffset minima/pre-2000
  values, empty/whitespace configuration strings and nonempty whitespace keys.
  Recursive domain equality preserves primitive types; event/checkpoint/fault
  sequences and arrays use native Int64 bounds; erase commit/ack ticks are bounded.
- AC-16.1-01 derives each transition from delivered/accepted fingerprints,
  authoritative history and composite persisted projection/checkpoint/health,
  opaque ETags and a measured common CAS. Contradictions persist fail-closed health;
  verified duplicates/older matches preserve a true no-op. The three isolation
  proof sites derive actual UTF-8 base64url tenant/resource keys and measure both
  tenants' writes, unchanged other-tenant contents and own/foreign reads. Query
  authority requires the complete tenant-derived control-index key.
- Duplicate-conflict proofs retain the actual original/reused component and both
  contract/position-only malformed cases, with independently derived fail-closed
  outcomes. Changed transport time preserves valid winning contract time.
  Redaction retains append-only authoritative history, targeted/control messages,
  source-derived masking, safe attribution/audit references and actual trust
  vocabulary in persisted and production-query observations. General replay
  explicitly rejects governance outside this bounded redaction experiment.
- Public-surface proofs inspect complete emitted event/log bytes tied to genuine
  source/contract shapes and distinct synthetic tenant-data canaries. The actual
  existing audit diagnostic was compiled in isolation and emitted the exact
  retained structured warning. These controls do not claim general personal-data
  classification or runtime execution.
- Catalog proofs retain genuine serialized metadata declarations with populated
  capability arrays. Raw-byte SHA and EventStore's actual length-framed catalog
  fingerprint are distinct. The enrolled `conversation@v1`, exact named/ordinary
  full-replay route, capability subsets, project/reconcile inventory and pending
  location are bound and correlated across first failure/reconciliation. The
  controlled catalog has no optional source-guard/recovery enrollment; a different
  enrollment needs separately reviewed controls. Attributed native key/catalog
  and wire goldens remain local, with no default-test submodule reads.
- Complete combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=module/validation/epic-16-prerequisite-implementation/review-iteration-4/implementation/final-combined.xml`
  — **exit 0, 958 passed in 375.22s** (796 successor including all prior 590,
  162 historical adoption-v2). The 72 standalone/Story 11.1/11.2 checks passed in
  85.30s before later relationship/control refinements; generator, historical
  fixtures and those selected assertions were unchanged, and the combined suite
  repeats all six standalone cases.
- One final source-contract check added the actual governance audit constructor's
  UTC-offset-zero and year-2000..9998 constraint. Its final-source affected selector
  passed **55 tests, 744 deselected in 46.95s**, including all fifteen positive
  routes, real verifier/generator publication, redaction, wire and timestamps.
  The prior 958-case suite is explicitly a pre-audit source epoch, not claimed as
  a final-source snapshot. Exact hash-verified pre-audit consumer/test bytes and
  both source bindings are retained. The narrowly affected final tests passed;
  no third combined or broad run was necessary.
- All **46 pre-audit source mutations** cause nonempty targeted failures with no
  setup errors (351 targeted test failures across the runs); the final audit
  guard's separate mutant causes all **3** targeted tests to fail. Thus **47**
  guards are demonstrated across explicit source epochs: all 26 retained guards
  and 21 new guards. Independent parent checks reject all eight retained bad
  inputs and accept valid configuration/date edge cases.
- All eighteen refreshed final-source runtime/final-record commands remain
  **BLOCKED / exit 2 / ENTRY_QUALIFICATION_PENDING**, with no runtime outputs.
  Both exact Story 7.3 workflow commands pass and prior operational bytes were
  restored. `git diff --check` passes. Commands, source inventories, stdout/stderr,
  JUnit, mutations, parent probes, native source/output hashes and epoch dispositions
  are retained in
  `module/validation/epic-16-prerequisite-implementation/review-iteration-4/verification.json`.
- The broad lane was not rerun, as required. The existing REFRESH_UV_DRIFT and
  Story 10.4 guidance-binding failures remain visible and unweakened. Real Platform
  integration qualification, seventeen adoption findings, provider/environment
  parity, recovery drills, operator/deputy and capacity/waiver decisions, human
  SM-2 and separate owner entry remain outstanding. Accepted CLI pins stay 4.35.2;
  runtime implementation, publication, deployment and hold lift remain unauthorized.


### Re-derived implementation verification — review iteration 5

All authorized prerequisite implementation tasks are complete. The 33 task-owned
preservation files were hash-verified before restoration. Frozen authorization,
predecessor packages/pins, accepted 4.35.2 CLI pins and the actual runtime hold are
unchanged; the parent retains review/completion orchestration.

- Erase proves exact captured/planned/deleted object inventories, actual tenant/
  resource key derivation, source history and hashed persisted metadata selectors.
  The explicitly versioned controlled v2 capture holds lifecycle generation apart
  from native pair event-position/dispatch metadata; its positive position 3 and
  lifecycle generation 2 demonstrate the distinction. It does not claim an existing
  v2 runtime storage implementation. Rebuilds include the complete creation prefix.
- Both restart snapshots bind their measured tenant, actual TenantDisabled native
  shape/date and immutable raw payload fingerprints while retaining Gapped denial.
  Raw lease/version and UTC observation domains are bounded. Recovered tenant
  descriptions accept null and string values. Party identities use the exact native
  Trim/ToLowerInvariant behavior, including Unicode cases that differ from Python;
  actor/redaction attribution normalizes and duplicate normalized participants fail.
  Raw payload hashes and opaque non-Party identifier semantics remain unchanged.
- Redaction rejects reused source identities even when all expected lists are
  refreshed. Metadata rejects genuine async-void Facts while preserving sync void,
  Task/ValueTask and fixtures. Native run/cleanup errors yield NATIVE_RUN_FAILED
  even with passed counters; the actual pinned runner positive and its run-error
  mutation are exercised. Supplied evidence assemblies are never executed.
- The complete bound input inventory and future native test sources have explicit
  checkout policies. A disposable core.autocrlf=true checkout and renormalization
  retain bound hashes, CRLF/UTF-16 native TRX, binaries, receipts and final pairs.
  The real runtime insertion caller, under explicit disposable entry and public-
  baseline/contract trust injection, remains BLOCKED/PUBLICATION_MISSING until
  exact native/receipt/final-pair bytes are committed atomically, then passes.
- Final combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-5/implementation/combined.xml`
  — **exit 0, 1,024 passed in 408.64s** (862 successor: all 799 retained plus
  63 new; 162 historical adoption-v2).
- Final legacy command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_epic16_tooling.py -k 'story_111 or story_112 or standalone_generator' --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-5/implementation/legacy.xml`
  — **exit 0, 72 passed, 2323 deselected in 85.50s**. Implementation and test
  sources stayed unchanged across these final checks and the held/workflow checks.
- All **15 new final-source validation mutations** produce targeted failures:
  **39 failed tests, zero setup errors**. Earlier 47 guard observations and their
  source epochs are retained; all corresponding prior regressions pass in the
  final combined suite. Independent parent counterexamples reject, and valid
  nullable-description, normalized PartyId and one-index ready-empty cases pass.
- All eighteen runtime/final-record commands return nonempty **BLOCKED / exit 2 /
  ENTRY_QUALIFICATION_PENDING**. All 36 actual native/receipt/final output paths
  remain absent. Both exact Story 7.3 guards pass with prior operational bytes
  restored; `git diff --check` passes. Entry remains null and actual trust empty.
- Exact commands, source/test hashes, separate stdout/stderr, JUnit, native PartyId
  artifact/source provenance, mutation ledgers, independent parent probes, held
  commands and intermediate-epoch dispositions are retained in
  `module/validation/epic-16-prerequisite-implementation/review-iteration-5/verification.json`.
  The record also discloses the worker's extra workflow render before it reached
  the no-secondary-workflow instruction; no secondary steps or agents followed.
- The prohibited broad lane was not rerun. Its REFRESH_UV_DRIFT and Story 10.4
  guidance-binding failures remain reported and unweakened. Actual Platform
  integration qualification, seventeen adoption findings, operational/provider
  parity, recovery drills, operator/deputy coverage, capacity/waiver decisions,
  human SM-2 and separate owner entry remain outstanding. Story 16.1 stays draft/
  backlog; runtime implementation, deployment and publication remain outside scope.


### Re-derived implementation verification — review iteration 6

The user-approved continuation is implemented and verified. The existing parent
workflow retains independent review and completion orchestration. The prior
review-limit disposition and every earlier evidence/source epoch remain historical
records; no runtime hold or owner qualification is inferred from these checks.

- Restored all 33 task-owned preservation files after validating their manifest
  hashes. All 18 scenarios, 32 requirements, 38 graph nodes, 61 edges, rollback
  obligations and pinned predecessor files remain intact. The final source/test
  inventory contains 67 exact byte hashes, unchanged across final verification.
- Runtime evidence uses a closed version-1 capture manifest and dedicated
  content-addressed managed binaries. Receipts bind raw native bytes, original
  lexical POSIX/Windows path relationships, scenario/candidate and retained bytes.
  The original location is metadata only; the existing contained reader remains
  POSIX-only with no fallback. Captured assemblies are inspected without execution.
  Generation derives the exact unique assembly/manifest inventory; publication
  requires that inventory plus all five native results/receipts and the final pair.
- The genuine xUnit 4.0.2 capture test emits unchanged raw TRX from generated fixture
  Facts, commits the exact 18-file evidence transaction and separate lifecycle
  insertion, then creates a fresh disposable checkout. It hides the original root,
  deletes external build/payload directories and has no untracked runner there.
  Regeneration produces byte-identical final files and the actual insertion route
  passes. Missing/extra/conflicting/altered/aliased captures and binaries fail.
  Retained artifacts explicitly identify synthetic observation payloads and
  test-only entry/contract trust: this is interoperability evidence, not product
  runtime qualification or owner approval.
- Duplicate and conflicting future deliveries now retain measured complete
  composite schedules, exact native fingerprints and version/ETag/commit evidence.
  Healthy restart binds the complete event-derived tenant state, immutable snapshot
  bytes, exact ITenantProjectionStore tenant lookup and consumer request identities.
  The durable capture DTO is explicitly a controlled future protocol, not a claim
  that a durable provider exists. The unrelated-port control is a separately
  executed preflight with every owned port free and observed result/diagnostics.
- Recovery retains authoritative aggregate sequences and distinct global positions,
  an old worker expectation, strictly newer checkpoint/projection/watermark and
  rejected CAS attempts that preserve complete persisted state. Four negative
  controls independently reset/fork from the same newer snapshot; the isolated
  lease-expiry case matches the other current fences. Current clearance and its
  duplicate retry form a separate positive branch. Schema/helper/test text makes
  that chronology explicit. Legitimate small replay histories remain allowed.
- Future fault relations retain the previous horizon; readiness and endpoint-race
  failures reach the pinned attempt limit or timeout. Native opaque nonblank checks
  match all 65,536 UTF-16 code units against the fixed .NET helper, including valid
  U+001C–U+001F, with downstream identity/metadata/selected-ID checks corrected.
  PartyId normalization remains distinct. Test-only Story 16.3 entry coverage proves
  missing ownership FAIL, unqualified ownership BLOCKED and qualified PASS.
- Final combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-6/implementation/combined.xml`
  — **exit 0, 1,213 passed in 540.32s** (1,051 successor: all 862 retained plus
  189 new; 162 historical adoption-v2), zero failures/errors/skips.
- Final legacy command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_epic16_tooling.py -k 'story_111 or story_112 or standalone_generator' --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-6/implementation/legacy.xml`
  — **exit 0, 72 passed, 2,512 deselected in 75.51s**, zero failures/errors/skips.
- All **25 new guard-removal mutations** produce nonempty targeted failures:
  **173 failed tests, zero setup errors**. These retain their exact source epoch
  before the independent-branch description and checkpoint-negative correction;
  no functional guard changed afterward. The former checkpoint-4 negative became
  checkpoint 5 because the measured valid recovery now reaches 4. All 47 affected
  tests passed, followed by the final combined run. The earlier 47+15 mutation
  observations remain historical, with their source hashes, not new rerun claims.
  The initial hash-control sensitivity issue and both interrupted/failed combined
  epochs are retained honestly; the final valid-managed-binary hash control fails
  when its guard is removed.
- Parent probes accept all fifteen unchanged positives and reject all fourteen
  new schedule faults; all ten lexical capture probes match expectation. Retained
  horizon/readiness/foreign-tenant/zero-watermark counterexamples reject, legitimate
  smaller replay accepts, and native whitespace boundaries match.
- All eighteen actual runtime/final-record commands return nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. All 54 actual native/receipt/final/capture
  paths and binary directories remain absent. Both exact Story 7.3 guards pass,
  with operational bytes restored. `git diff --check` passes. Entry is null, trust
  is empty and accepted CLI pins remain 4.35.2.
- Exact source/test/artifact hashes, commands, separate streams, JUnit, source-epoch
  dispositions, mutations, parent probes, commitlint validation for the three new
  disposable-only commit messages, preserved scenario/I/O matrix and native fixture
  provenance are retained in
  `module/validation/epic-16-prerequisite-implementation/review-iteration-6/verification.json`.
  No workflow was rendered, no secondary orchestration or subagent was started,
  and no real repository stage/commit/push or dependency update was performed.
- The broad lane was not rerun. Existing REFRESH_UV_DRIFT and Story 10.4
  guidance-binding failures remain unresolved and unweakened. Actual Platform
  integration qualification, seventeen adoption findings, operational/provider
  parity, recovery drills, operator/deputy coverage, capacity/waiver decisions,
  human SM-2 and separate owner entry remain outstanding. Concurrent user changes
  to the Story 5.4 spec, its runtime-qualification document and its installed-delivery
  packet test are outside this task and were neither changed nor claimed validated.


### Re-derived implementation verification — review iteration 7

All authorized prerequisite implementation tasks are implemented and verified.
The existing parent workflow retains independent review and completion. The
35 task-owned preservation files were hash-verified before restoration; the
concurrent Story 5.4 user commit and deferred-ledger edits remain preserved.

- The fixed native helper carries the exact unchanged root `global.json` into its
  isolated workspace and binds it into execution provenance. Both workspaces select
  SDK **10.0.402**; the helper reports actual **.NET 10.0.13** and target framework
  `.NETCoreApp,Version=v10.0`. Malformed/non-object/invalid SDK policy fails before
  dotnet starts; unavailable or mismatched approved tooling remains BLOCKED. Local
  helper provenance is separate from portable measured capture. Narrow binary
  attributes preserve PE bytes and prevent forced text diffs.
- All native nullable conversation fields pass schema and consumer validation.
  Native Closed, Archived and Archived-then-Closed histories prove terminal replay;
  independently expected lifecycle values reject coherently refreshed stale Open
  results. Authoritative per-stream envelopes derive rebuild B/C boundaries with
  sparse global positions and complete creation prefixes; legitimate B=C does not
  advance. Erase references retain consistent immutable identity relationships.
- Controlled future-v2 lifecycle captures retain distinct Ready-to-Erasing and
  Erasing-to-Erased commits, and Erased-to-Rebuilding and Rebuilding-to-Ready commits.
  Each operation advances generation once; completion keeps that generation.
  Source-derived complete control-index/components, captured plans, version/ETag/
  generation fences and shared EventStore transaction identities prove every stage.
  Actual stale plans reject without changing newer state. Deletion, dispatch safety
  and reference-only quarantine are checked at each boundary. These DTOs do not
  claim a current runtime storage/provider implementation.
- Recovery adds independently isolated superseded-lease and stale-checkpoint
  attempts. Durable write/ack observations correlate delivery, persisted identity
  and transaction with strictly increasing operation ordinals; equal timestamps
  remain valid. Future deliveries genuinely enlarge persisted pending/horizon state,
  reject an earlier worker against the enlarged set, and replay through the enlarged
  horizon. Replay validates and consumes pending evidence; Healthy completion sees
  an already-empty set and the complete current CAS tuple.
- Final combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-7/implementation/combined.xml`
  — **exit 0, 1,467 passed in 635.39s** (1,305 successor: all 1,051 retained plus
  254 new; 162 historical adoption-v2), zero failures/errors/skips.
- Final legacy command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_epic16_tooling.py -k 'story_111 or story_112 or standalone_generator' --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-7/implementation/legacy.xml`
  — **exit 0, 72 passed, 2,766 deselected in 80.12s**, zero failures/errors/skips.
  All 72 frozen source/test/policy hashes stayed unchanged across final verification.
- **35 distinct guard mutations** are demonstrated across explicit source epochs:
  **38 runs, 159 targeted test failures, zero setup errors**. The initial 33 probes
  produced 147 failures; four final SDK runs produced eight, and the final integer
  schema guard produced four. Only affected SDK/schema guards changed after the
  initial mutation epoch, and those were retested. Earlier mutation observations
  remain historical. Intermediate fixture failures, four exploratory
  EXECUTING_SOURCE_DRIFT failures from editing during a run, and one overly narrow
  exception-type assertion are retained honestly; final runs were sequential and
  frozen. Final parent probes pass **29/29** schema/semantic and **3/3** actual
  malformed-policy setup checks with matching source hashes.
- Genuine xUnit 4.0.2 fixture execution, unchanged raw TRX, the exact 18-file
  publication, fresh-checkout regeneration/insertion and lost original measurement
  directory checks pass again. Exact committed fixture bytes are retained with
  their candidate/publication/lifecycle commits. Observation payloads and entry
  trust are explicitly synthetic/test-only and convey no product runtime authority.
- All eighteen actual runtime/final-record commands return nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. The same 54 runtime inventory entries
  remain absent: **51 files and three binary directories**. Both exact Story 7.3
  workflow guards pass with prior operational bytes restored; `git diff --check`
  passes. All eighteen scenarios, thirty-two requirements, thirty-eight nodes,
  sixty-one edges, rollback obligations and predecessor pins remain unchanged.
  Entry is null, trust empty, accepted CLI pins 4.35.2 and Story 16.1 draft/backlog.
- Exact commands, separate streams, JUnit, source/test/artifact hashes, toolchain
  provenance, source-epoch dispositions, mutations, parent probes, fixture artifacts,
  all thirty disposable message commitlint validations and preservation inventories
  are bound in
  `module/validation/epic-16-prerequisite-implementation/review-iteration-7/verification.json`.
  The worker read and rendered bmad-build once before reaching this spec's
  no-secondary-workflow instruction; that deviation was disclosed to the parent.
  No secondary workflow step, subagent or completion orchestration followed.
  No real repository stage/commit/push or dependency update occurred.
- The prohibited broad lane was not rerun. Its existing REFRESH_UV_DRIFT and
  Story 10.4 guidance-binding failures remain unresolved and unweakened. Actual
  Platform integration qualification, seventeen adoption findings, provider/
  environment parity, recovery drills, operator/deputy coverage, capacity/waiver
  decisions, human SM-2 and separate owner entry remain outstanding. Runtime
  implementation, deployment, publication and hold lift remain outside this task.


### Re-derived implementation verification — review iteration 8

All authorized prerequisite implementation tasks are implemented and verified.
The parent retains independent review and completion orchestration. The 36 owned
preservation files were hash-verified before restoration; all five protected inputs,
including unchanged global.json and the concurrent Story 5.4/deferred-ledger work,
remain byte-identical. No secondary workflow was rendered or invoked.

- Completed watermark derives exactly from captured C and prior same-generation
  progress. Structural fault validation delegates native domain maps to their own
  validators; opaque configuration/member keys remain valid. Every acknowledgement
  delivery/record identity uses native .NET nonblank semantics, preserving U+001C–1F.
- Future-v2 restart captures complete stored snapshots, one replacement lease/version
  acquisition, actual persisted-pending resumption without redelivery, isolated live
  lease/version/checkpoint/horizon controls and a successful current-worker renewal.
  All attempts use otherwise-admissible pending-preserving proposals; rejected
  attempts preserve complete newer state. Healthy restart and Tenants clock rules
  remain unchanged.
- Durable catch-up captures exact authoritative event/message/location identities,
  duplicate/reordered arrivals and complete enqueue CAS snapshots/transactions.
  Drain starts from the last actual enqueue state, consumes exactly through C and
  retains every later reference in the Ready component. Collision checks include
  earlier retained stream history; legitimate new post-C streams remain queued.
- Every lifecycle boundary has an ordered, executed query against the exact committed
  snapshot, actual returned values, read calls and no mutation/recovery. Named internal
  metadata contradiction yields Unavailable/metadata_contradictory; actual cross-key
  index/detail/dispatch mismatch yields Rebuilding/mixed_generation. Current query
  sources and reason vocabulary are retained locally with exact source attribution.
- Cancellation retains one executed operation/clock schedule through request,
  observation, owned-resource cleanup and terminal completion. Derived elapsed time
  respects the pinned 100-ms control; equal coarse-clock readings are valid and
  preexisting resources survive. These controlled DTOs do not claim runtime providers.
- Final combined command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_epic16_tooling.py _bmad/scripts/tests/test_verify_epic_16_adoption_v2.py --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-8/implementation/combined.xml`
  — **exit 0, 1,750 passed in 752.83s** (1,588 successor: all 1,305 retained plus
  283 new; 162 historical adoption-v2), zero failures/errors/skips.
- Final legacy command:
  `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py _bmad/scripts/tests/test_epic16_tooling.py -k 'story_111 or story_112 or standalone_generator' --maxfail=3 --junitxml=/home/administrator/projects/hexalith/conversations/module/validation/epic-16-prerequisite-implementation/review-iteration-8/implementation/legacy.xml`
  — **exit 0, 72 passed, 3,049 deselected in 86.48s**, zero failures/errors/skips.
  All 74 frozen source/test/policy hashes remained unchanged during final verification.
- **39 guard removals** are demonstrated across **40 recorded mutation runs**:
  **81 targeted assertion failures, zero setup errors/skips**. A redundant internal
  generation-summary comparison survived removal because independent named-field
  mapping/case checks still rejected the fault; that observation is retained without
  claiming sensitivity. Removing the actual internal-mapping guard fails all five
  controls. Earlier iteration mutations remain historical, with their source epochs.
- Parent final probes pass **44/44**, with matching source hashes before/after.
  Exploratory epochs retain the initial 284-pass result and later 550-pass/two-failure
  result: the latter exposed an equal-B/C fixture needing new captures and an overly
  narrow queue schema for an at-B arrival after queued work. The corrected selector
  passes 10/10, followed by the final combined run. No failed epoch is presented as
  final-source evidence.
- Genuine pinned xUnit execution, unchanged raw TRX, exact 18-file publication and
  fresh-checkout regeneration/insertion pass again with the original measurement
  directory unavailable. Twenty-three exact committed fixture artifacts are retained.
  Their payloads/trust are explicitly synthetic/test-only and confer no product
  runtime qualification or owner approval. Fixed helper provenance again verifies
  approved SDK 10.0.402 and actual .NET 10.0.13.
- All eighteen actual runtime/final-record commands remain nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths remain absent (51 files,
  three binary directories). Both exact Story 7.3 guards pass with operational bytes
  restored; `git diff --check` passes. All eighteen scenarios, thirty-two requirements,
  thirty-eight nodes, sixty-one edges, rollback obligations and predecessor pins are
  preserved. Entry stays null, trust empty, accepted CLI pins 4.35.2, Story 16.1
  draft/backlog and hold ACTIVE.
- Commands, separate streams, JUnit, hashes, source epochs, mutations, native fixture
  provenance, thirty disposable commit-message validations and preservation evidence
  are bound in `module/validation/epic-16-prerequisite-implementation/review-iteration-8/verification.json`.
  The worker initially read the build-skill manifest, then honored this handoff's
  prohibition: no workflow render, secondary step, child worker or completion
  orchestration followed. No real repository staging/commit/push, dependency update,
  application start, deployment or runtime evidence publication occurred.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4 guidance
  binding remain unresolved and unweakened. Actual Platform integration qualification,
  seventeen adoption findings, provider/environment parity, recovery drills,
  operator/deputy coverage, capacity/waiver decisions, human SM-2 and separate owner
  entry remain outstanding. These are outside this prerequisite implementation.


### Re-derived implementation verification — review iteration 9

All authorized prerequisite implementation tasks are implemented and verified.
The parent retains final artifact review and completion orchestration. Exactly
36 owned preservation files were hash-verified before restoration. All five
protected inputs, including global.json and the concurrent Story 5.4/deferred
ledger work, remain byte-identical. The frozen final epoch binds 84 source,
test and policy files; none changed during final verification.

- Required rejected replay operations now retain executed invalid native inputs,
  attempted writers and complete before/after persisted bytes. Missing stable keys,
  wrong order and generation before the event-derived anchor fail without commits
  or clamping. Valid zero lag and coincident generation instants remain supported.
- Warning removal derives from actual baseline/candidate Program.cs bytes after
  restart and convergence pass. Both the actual active warning and historical
  release disclosure remain untouched; future removal is tested in disposable
  candidate repositories.
- Portable capture version 2 binds all seven required loaded Conversations,
  Tenants and EventStore product assemblies to actual test output, exact retained
  PE bytes, managed name/MVID, full source revision and independently pinned owner.
  Root assemblies match the candidate; dependencies match raw direct gitlinks.
  Fixed PEReader/MetadataReader inspection follows the actual SDK informational
  version convention, without loading evidence assemblies. Source/version, route,
  policy and contract declarations are retained with hashes. Helper execution uses
  unchanged approved SDK 10.0.402 and actual .NET 10.0.13.
- Baseline/candidate public metadata for all five upstream assemblies derives from
  the owning revisions and actual bound binaries. Existing types and supported
  member/parameter/return/property/generic contracts must survive. Concrete and
  default-interface additions pass; new abstract implementor obligations fail.
  An ordinary member name containing Abstract remains valid. This conservative
  declaration comparison does not claim general binary compatibility. All eight
  final API fixtures retain exact native binaries, source and raw gitlinks; unsupported
  native marshalling/PInvoke/RVA/forwarded shapes fail explicitly.
- Reconciliation retains an ordered executed POST /project/v2/reconcile request,
  EventStoreWorkload:domain-service:project authorization, hosted ReconcileAsync
  dispatch, durable result and actual ProjectionDispatchResponse outcomes.
  HTTP 200 alone is insufficient: expected Completed/AlreadyCompleted outcomes
  must match the projection route and committed clearance. Transaction/clearance
  instrumentation is separate from response members. Wire diagnostics contain no
  event/state content; their state digest is recomputed from the separately
  validated complete recovery snapshot. Readiness emits closed bounded retry
  advice tied to the measured terminal condition and existing experiment policy.
- Query lifecycle/provider domains are closed before mapping. Component tenant,
  resource and dispatch references match the control index; missing required
  cross-key inventory yields structured failure while initialized-empty queries
  remain valid. All wrappers emit nonempty BLOCKED/exit 2 for missing declared
  Python dependencies. Final artifact derivation rereads retained capture/binary
  bytes and rechecks containment/no-alias constraints against admitted hashes.
- Final combined result: **2038 passed in 1071.15s (0:17:51)** — 1,876 successor tests (all 1,588
  retained plus 288 new) and 162 historical adoption-v2 tests. Final legacy result:
  **72 passed, 3337 deselected in 82.59s (0:01:22)**. Both runs have zero failures/errors/skips; exact commands,
  separate streams, JUnit and hashes are retained in test-commands.json.
- **41 distinct guards across 43 removal runs produced 192 targeted assertion
  failures and zero setup errors/skips.** The initial 39 runs produced 175 failures.
  After R9P5, both affected API comparison guards and the two new class/value-kind
  guards produced 17 failures on final source. All original round9 control/helper
  function ASTs remain identical. Four older fixtures were also adapted across
  earlier epochs; their corrected selectors pass 5/5 and 4/4. Parent frozen-source
  probes and compiled reference-kind verification remain independently retained.
- Parent R9P5 exposed an actual decoder defect: a referenced parameter/return type
  changing from class to struct produced identical API metadata. SignatureNames
  now retains native class/value markers for definitions, references and specified
  signatures while declaration/attribute names remain separate and unprefixed.
  Ten added compiled cases cover six breaking parameter/return/property/field/
  generic/by-reference changes, two valid same-kind additions and two local-type
  declaration-name controls. The focused API selector passes 23/23. The original
  parent counterexample and all eight final external-type fixtures retain exact
  source/binary/build evidence. This is an existing requirement correction, not
  expanded runtime authority or a general binary-compatibility claim.
- The genuine pinned xUnit test loads seven explicitly synthetic source-stamped
  product stand-ins and retains untouched raw TRX. Its current publication is
  derived from capture contents: 25 paths including
  8 distinct PE binaries. Fresh-checkout regeneration
  and inserted-record verification pass after the original measurement root/build/
  payload locations disappear. Exact committed fixture bytes and candidate,
  publication and lifecycle revisions are retained. These fixtures grant no real
  product qualification. The historical 18-file proof remains unchanged.
- Intermediate epochs remain visible: the first expanded selector had 225 passes
  and five fixture failures, followed by 249 passes. The retained exploratory run
  was deliberately interrupted after 966 passes/three known fixture failures
  (278 deselected; exit 2); its progress percentages used all 1,588 selected cases.
  The first frozen combined then passed 2,027 with one nullable fixture failure;
  it is preserved in full. The fixture now recomputes its safe wire-state digest
  after changing the valid persisted nullable values. The next epoch passed
  2,028 combined and 72 legacy checks, then was superseded by R9P5. Its evidence
  audit script initially expected uppercase in the unchanged lowercase historical
  disclosure; that assertion was corrected and the failure retained. The final
  signature-aware combined JUnit is authoritative. Each correction and earlier
  failure is described in source-epoch-disposition.json, with recovered exact
  intermediate invocation lines and separate streams.
- All eighteen actual commands remain **BLOCKED / exit 2 /
  ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths are absent (51 files and
  three binary directories). Both exact Story 7.3 workflow guards and git diff
  --check pass, with previous operational bytes restored. All eighteen scenarios,
  thirty-two requirements, thirty-eight nodes, sixty-one edges, rollback obligations
  and predecessor pins remain preserved. Entry is null, trust empty, accepted CLI
  pins 4.35.2, and Story 16.1 remains draft/backlog with its hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-9/verification.json`.
  Disposable commit messages were checked by the pinned commitlint before use.
  The worker's initial extra bmad-build render, performed before reaching this
  handoff restriction, remains an explicit orchestration deviation; the parent's
  existing render stays authoritative. No secondary workflow steps, child workers,
  completion orchestration, actual repository staging/commit/push, dependency
  updates, application start or runtime publication followed.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4
  guidance-binding failures remain unresolved and unweakened. Actual Platform
  integration qualification, seventeen adoption findings, provider/environment
  parity, recovery drills, operator/deputy coverage, capacity/waiver decisions,
  human SM-2 and separate owner entry remain outstanding and outside this build.

### Parent acceptance and matrix audit — iteration 9

The parent read the complete baseline diff inventory and current incremental
implementation delta, checked each task/criterion and all eight matrix rows, and
verified all 1,750 prior executed cases plus 288 new cases in the final 2,038-case
JUnit; all 72 legacy checks pass. Independent audits verified 879 exact artifact
hashes, 84 frozen files, 70 bound sources, three test hashes, 32 validated disposable
commit messages, both completion guards, eighteen held commands and 54 absent
runtime paths. Every failed mutation case passes on final source. The 30 retained
native fixture files additionally match actual disposable lifecycle Git objects.
The index is empty and protected user files remain unchanged. This completes
implementation acceptance; independent code review follows, and runtime entry
remains outside this authorization.

- [Verification record](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/verification.json), SHA-256 `f8fc7b3a71598e90ee82fa82c3ec87781913279b4442b108210f2ccb282f53e6`.
- [Parent acceptance audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/parent-audit.json).
- [Artifact audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/parent-artifact-audit.json).
- [Matrix audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/parent-matrix-audit.json).
- [Mutation audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/parent-mutation-audit.json).
- [Native retention audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-9/parent-native-retention-audit.json).


### Re-derived implementation verification — review iteration 10

All authorized prerequisite implementation tasks are implemented and verified.
The parent retains independent review and completion orchestration. Exactly 40
owned files were hash-verified before restoration; the new TestConstruction.cs
brings the implementation inventory to 41. The parent-owned narrow .gitignore
packaging rule brings the task inventory to 42 files. All five protected inputs and all 879
prior accepted artifact hashes remain unchanged. The final epoch freezes 85 files
and 71 bound sources; none changed during the final verification sequence.

- Candidate upstream API evidence now identifies the exact product bytes/MVID and
  revision admitted from the same native execution. All five measured scenarios
  must use one consistent inventory of seven owned implementations. Positive
  fixtures deliberately reuse retained artifacts, including after compiler-cache
  clearing. Root Contracts source equality remains strict; both public-surface
  scenarios additionally bind baseline/current compiled declarations and candidate
  loaded bytes. Actual external Compile removal and generated-source additions
  fail despite unchanged complete Contracts source inventories.
- Supported signatures retain assembly name, version, culture, public key/token
  and flags, alongside class/value kind and generic-constraint custom attributes.
  Native signed-reference and nullable-constraint controls retain exact binaries,
  build streams and actual caller diagnostics. Comparisons remain conservative
  declaration preservation, with explicit unsupported-shape failures; they do not
  claim general binary or implementation-behavior compatibility.
- Metadata-only constructor admission resolves pinned xUnit output/context helpers,
  local class/collection/assembly fixtures, supported inheritance and default,
  optional and param-array arguments. Missing required providers reject. Actual
  pinned-runner controls cover 29 supported and failing shapes. Parent R10P1 found
  that distinct inherited collection attribute types survived reflection despite
  the inspector's nearest-declaration shortcut. The correction suppresses only
  the same concrete attribute type; inherited distinct types reject, while valid
  overrides and inherited-only providers pass. Discovery-failure controls retain
  bounded eight-second diagnostics, never a claimed successful runner exit. Static
  metadata does not prove arbitrary constructor-body behavior.
- Unauthorized/nonexistent query captures retain actual executed requests and
  correlated public ConversationListResult.Hidden content. Both results must be
  identical and non-disclosing, with the existing Forbidden/forbidden vocabulary,
  empty summaries/page, expected safe action, zero index reads and zero repairs.
  Correlation, execution, order, content leakage and equal malicious-response
  controls fail. Five retained current contract sources anchor the closed protocol.
- Native test stamps accept both genuine SDK informational-version forms. TRX
  definitions require exactly one TestMethod and Execution. Lazy generator imports
  classify only genuinely absent declared modules as unavailable; internal import
  defects propagate. Historical and standalone behavior remains intact.
- Final combined verification: **2,152 passed** in
  **1355.87s**, including all 2,038 prior cases plus
  114 new cases (1,990 successor and 162 historical adoption-v2). Final legacy
  verification: **72 passed** in **84.71s**.
  Both runs have zero failures/errors/skips. Exact commands, environment, separate
  streams, JUnit and source hashes are retained in test-commands.json.
- **20 distinct guards are demonstrated across 24 recorded mutation runs**:
  **54 targeted assertion failures and zero setup errors/skips**. One intermediate
  inherited-ambiguity removal is explicitly insensitive because differing fixture
  providers still triggered the independent missing-provider check. Three genuine
  conflicts with a shared provider isolate the ambiguity rule and all fail under
  its removal. The affected constructor guards were rerun after R10P1; unchanged
  guards retain their exact prior source epoch. Every failed mutation case passes
  on final source. Parent pure controls pass 79/79 plus nine import boundaries;
  original same-level/inherited conflict PEs independently reject.
- Failed exploratory epochs remain intact: 17 passes/15 source-binding failures
  before adding the new C# file's byte-preserving Git attribute; then 80 passes/six
  namespace-mismatched fixture failures before correcting test namespaces. The
  corrected native selector passed 27; the affected selector passed 124; final
  pre-R10P1 controls passed eight. The first frozen combined run was intentionally
  interrupted for R10P1 after 578 completed passes, with actual uv exit 1 and no
  assertion failures. That incomplete run and its original 85-file snapshot remain
  archived. The corrected 26 native cases and three shared-provider controls pass.
  Exact exploratory commands were recovered from this task's execution transcript;
  the first two exploratory epochs have no complete frozen source inventory and
  are not presented as final-source evidence.
- Genuine xUnit 4.0.2 execution retains untouched raw TRX and seven explicitly
  synthetic product stand-ins. The final publication derives **26 paths**,
  including **9 PE binaries**, from the captured inventory and root
  baseline/current compiled proof. Fresh-checkout regeneration and insertion pass
  after original measurement/build/payload locations disappear. All
  **31 exact committed fixture artifacts** and their candidate/publication/
  lifecycle revisions are retained. Fixture trust and products convey no real
  runtime or owner authority. Final helper provenance confirms approved SDK
  10.0.402 and actual .NET 10.0.13, with unchanged global.json.
- All eighteen actual runtime/final-record commands remain nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths remain absent
  (51 files and three binary directories). Both exact Story 7.3 guards and
  git diff --check pass, with prior operational bytes restored. All eighteen
  scenarios, thirty-two requirements, thirty-eight graph nodes, sixty-one edges,
  rollback obligations and predecessor pins remain preserved. Entry is null,
  trust empty, accepted CLI 4.35.2, Story 16.1 draft/backlog and hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-10/verification.json`.
  Exact disposable commit messages were validated with the pinned commitlint
  before use, including commit-tree fixtures. No skill was read or rendered, no
  secondary workflow, subagent or completion orchestration ran, and no actual
  repository staging/commit/push, dependency update, application start or runtime
  publication occurred. Parent review remains separate.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4 guidance
  binding remain unresolved and unweakened. Actual Platform integration qualification,
  seventeen adoption findings, provider/environment parity, recovery drills,
  operator/deputy coverage, capacity/waiver decisions, human SM-2 and separate owner
  entry remain outside this prerequisite implementation.


### Parent acceptance audit — iteration 10

The parent independently accepts tasks 2–5 against the complete baseline/untracked
diff and the final verification outputs. All 2,038 prior cases and 114 new cases
ran and passed; all eight matrix rows retain executed coverage. Independent checks
verify 1,623 exact trackable artifacts, 85 frozen files, 71 bound sources, three
test hashes, 38 validated disposable commit messages, both completion guards,
eighteen held commands and 54 absent runtime paths. All 31 native retention files
match actual disposable lifecycle Git objects, with 26 publication paths and nine
assemblies. Twenty guards are demonstrated; all mutant test cases pass on final
source, with the insensitive intermediate probe preserved explicitly.

R10P1 and its corrected native controls are retained separately. A packaging-only
Git ignore exception preserves exact historical/current evidence paths without
changing the frozen verification inputs. One archived experiment ignore file is
retained byte-for-byte as `.gitignore.bytes` so it cannot hide adjacent proof files;
the original manifest and relocation mapping remain available. All protected user
files and the empty index are preserved. Explicit runtime publication remains
**not-applicable** to this prerequisite scope; the actual runtime hold remains
ACTIVE. Independent review follows this acceptance.

- [Verification record](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/verification.json), SHA-256 `887e5643753115aeb38f250e8fd48ff3f36b08fdc131bfe5925ea4ce38b60efa`.
- [Parent acceptance audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-audit.json).
- [Artifact audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-artifact-audit.json).
- [Matrix audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-matrix-audit.json).
- [Mutation audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-mutation-audit.json).
- [Native retention audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-native-retention-audit.json).
- [Packaging audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-packaging-audit.json).
- [Exact successful parent audit commands](../../module/validation/epic-16-prerequisite-implementation/review-iteration-10/parent-audit-commands.json).

### Re-derived implementation verification — review iteration 11

All authorized prerequisite changes are implemented and verified. The parent retains
independent review and completion orchestration. Exactly 42 owned files were
hash-verified before restoration; the narrow evidence-packaging rule and
TestConstruction.cs remain preserved. All five protected inputs and all 1,623
prior indexed artifact hashes are unchanged. Final verification freezes 86 owned,
source, test and policy files, including the unchanged packaging rule; all 71 bound
source paths are preserved and no frozen bytes changed during verification.

- Conservative native declarations now retain interface implementation annotations,
  complete local/referenced attribute constructor identities/signatures/blobs,
  method implementation flags, owning assembly flags and assembly/module contract
  attributes. Only independently verified informational/source stamps and expected
  file-version metadata are excluded; other AssemblyMetadata remains contractual.
  Upstream version changes and legitimate additions remain supported. Both upstream
  additivity and unchanged root compiled-shape callers reject the compiled negative
  controls. These checks do not claim general binary or behavioral compatibility.
- P1's retained counterexamples showed that System namespaces/suffixes do not prove
  framework ownership. The fixed inspector now requires the approved .NET 10
  framework assembly name/version/culture/flags/signing identity for discovery
  and build-attribute exclusions. Genuine pinned xUnit execution proves plain Fact,
  Task/ValueTask and ordinary attributes, and rejects Fact+Theory plus local,
  inherited and external Fact-derived conflicts. Supplied evidence assemblies,
  attributes and constructors are never executed by the inspector.
- Tenant acknowledgement captures retain native source/delivery inputs, complete
  controlled before/after projection/checkpoint/health/pending contents, expected
  ETag and actual durable transaction. Executed successful commit and subsequent
  acknowledgement must observe those exact derived contents. A duplicate observes
  an already committed outcome with zero new writes; provider failure and retry
  exhaustion have no acknowledgement. Equal timestamps remain valid with ordered
  operations. P2's alternate/extra/empty native maps remain open to opaque keys,
  with actual role/string value validation instead of fixture-specific map keys.
- Each of the six isolated stale reconciliation branches retains correlated,
  executed and completed request/CAS/result operations, exact scope and immutable
  expected/proposed/stored-state digests. Duplicate retry executes after current
  wire clearance, observes the final durable state and returns AlreadyCompleted
  without writing. Independent branch clocks remain unordered. Multi-resource
  erase retains every per-resource dispatch reference and its multiplicity through
  all lifecycle boundaries and executed queries.
- All verifier launchers use exact declared dependency names and verify actual
  absence before emitting BLOCKED. Internal typo/import defects retain tracebacks.
  The primary producer documentation now specifies six runtime JSON fields,
  including loadedProducts, and distinguishes the five-field synthetic fixture
  shape and required retained product/capture inventory.
- Final combined verification: **2,416 passed** in **1570.09s**, including
  all 2,152 prior cases plus 264 new cases (2,254 successor and 162 historical
  adoption-v2). Final legacy verification: **72 passed** in **80.26s**.
  Both runs have zero failures/errors/skips. Exact commands, environment, separate
  streams, JUnit and source hashes are retained in test-commands.json.
- **35 distinct guards across 36 recorded mutation runs** produce **162 targeted
  assertion failures and zero setup errors/skips**. One initial selector excluded
  the entire epic16 module and selected zero tests; it is retained as a selector
  error, never guard-sensitivity evidence. Its corrected run selects seven cases
  and detects all six stale-operation failures. The additional branch/scope and
  native-map schema mutations fail as required. Every mutant failure passes on
  final source. Exact mutant bytes, patches and the narrow mutated-schema trust
  adjustment are retained. Parent native/protocol/source/data checks are separate.
- Intermediate epochs remain intact: 245 passes/three analyzer-blocked deliberately
  conflicting Fact fixtures; then 279 passes/three external-library package-shape
  failures. Only the intentionally invalid Fact builds disable analyzers to reach
  actual runner discovery. The synthetic attribute library uses pinned 4.0.2
  extensibility-core; its corrected three controls pass. No product dependency,
  consumer validation or expected failure was weakened. Exact source snapshots,
  commands and separate streams describe each superseded epoch.
- Genuine pinned xUnit execution retains untouched raw TRX and seven explicitly
  synthetic product stand-ins. Final publication derives **26 paths**,
  including **9 PE binaries**. Fresh-checkout regeneration and insertion
  pass after original measurement/build/payload locations disappear. All
  **31 exact committed fixture artifacts** and candidate/publication/lifecycle
  revisions are retained. Fixed helper provenance again verifies approved SDK
  10.0.402 and actual .NET 10.0.13. Fixtures grant no actual runtime or owner authority.
- All eighteen actual runtime/final-record commands remain nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths remain absent
  (51 files and three binary directories). Both exact Story 7.3 guards and
  git diff --check pass, with prior operational bytes restored. All eighteen
  scenarios, thirty-two requirements, thirty-eight nodes, sixty-one edges,
  rollback obligations and predecessor pins are preserved. Entry remains null,
  trust empty, accepted CLI 4.35.2, Story 16.1 draft/backlog and hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-11/verification.json`.
  Exact disposable commit messages were validated with pinned commitlint before
  use. No skill was read/rendered, no secondary workflow, child agent or completion
  orchestration ran, and no actual repository staging/commit/push, dependency
  checkout update, application start or runtime publication occurred.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4 guidance
  binding remain unresolved and unweakened. Actual Platform integration qualification,
  seventeen adoption findings, provider/environment parity, recovery drills,
  operator/deputy coverage, capacity/waiver decisions, human SM-2 and separate owner
  entry remain outside this prerequisite implementation.

### Parent acceptance audit — iteration 11

The parent accepts all six prerequisite tasks against the current source and
retained execution evidence. The complete baseline-to-worktree diff includes
7,954 file sections and 802 binary patches; its inventory was inspected alongside
the 705-line final source/docs/tests delta, current schema/data changes, and the
previously reviewed history. Exact bytes of all 2,854 new implementation artifacts
and 1,623 prior artifacts were independently verified. Binary evidence was checked
against its hashes and disposable Git objects, rather than interpreted as text.

- Final-source JUnit independently confirms 2,416 combined and 72 legacy passes,
  all 2,152 earlier cases retained, 264 new cases, and all eight matrix rows
  covered by executed passing tests. No failures, errors or skips occur.
- The 35 demonstrated mutation guards produce 162 assertion failures, each with
  a passing final-source control. The zero-selection exploratory command is
  retained separately and receives no guard credit. Independent parent controls
  cover native metadata/discovery, protocol execution and P1/P2 corrections.
- All 86 frozen source/test/policy hashes and 71 bound source hashes match;
  2,878 nested evidence references resolve. The 31 retained native fixture files
  match actual lifecycle commits, including all nine assemblies; each of the
  five captures has the same seven-product inventory.
- Both completion guards pass; all eighteen held runtime commands remain
  BLOCKED with ENTRY_QUALIFICATION_PENDING. All 54 runtime paths remain absent,
  the real index is empty, protected inputs are unchanged, and whitespace checks
  pass. Runtime evidence publication is explicitly not-applicable to this scope.
  Existing broad-lane blockers remain recorded and were not rerun or weakened.

Evidence:

- [Parent acceptance audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-audit.json).
- [Executed matrix coverage](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-matrix-audit.json).
- [Exact artifact/reference audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-artifact-audit.json).
- [Mutation sensitivity audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-mutation-audit.json).
- [Native Git-object retention audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-native-retention-audit.json).
- [Source preservation audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-source-audit.json).
- [Schema, data and trust audit](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-data-source-audit.json).
- [Exact parent audit commands](../../module/validation/epic-16-prerequisite-implementation/review-iteration-11/parent-audit-commands.json).
