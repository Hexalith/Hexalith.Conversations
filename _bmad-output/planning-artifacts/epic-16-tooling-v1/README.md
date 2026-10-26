# Epic 16 prerequisite tooling v1

This additive package preserves the eighteen ordered scenarios, thirty-two
requirements, thirty-eight graph nodes, sixty-one edges, predecessor identities
and rollback boundaries of adoption v2. It adds measured-input consumption; it
does not adopt a runtime implementation or lift the ACTIVE hold. All four
authority flags remain false. Prerequisite/adoption v1/v2 files are unchanged.

The three contracts embed the exact preserved contracts and bind their original
files. `epic16_trust.py` independently pins those contracts, schemas and the
complete predecessor packages. `binding.json` inventories the current consumer,
shared helpers and enrollment inputs; it cannot redefine the independent pins.
The current binding deliberately has `entry: null`. No current owner decision
is admitted in `TRUSTED_ENTRIES`.

## Measured-input protocol

Each non-generator scenario consumes one native TRX at
`artifacts/epic16-runtime/<story>/<scenario>.trx`. It must contain exactly one
executed, passed, correctly identified test, matching definitions/entries, exact ScenarioNN method and owning class, and
counters, and no skipped, failed, unknown or nested results. Its output is strict
JSON containing exactly six fields: `scenarioId`, full `candidate`, `sources`,
`contractSha256`, `observations`, and `loadedProducts`. Only synthetic tooling-fixture
inputs may omit `loadedProducts` and use five fields. Duplicate JSON keys and non-finite numbers
are invalid. The scenario definitions in the pinned schema describe the closed,
typed observation shapes; the evaluator checks semantic relationships itself.
A caller-authored PASS, count or arbitrary command is never executed or trusted.

The required native source is named by `nativeProtocol.runtimeTestSource` in
each contract. It must exist at the clean committed candidate. Runtime TRX must
also name a contained source-stamped managed test binary. Relative paths and
lexically contained absolute TRX codeBase paths use the same no-follow reader.
The fixed helper verifies PE/CLI metadata, the required public test type/method
and test attribute, and assembly informational/source revision without loading or
executing the evidence assembly. These future runtime test
sources and measurements are not implemented by this prerequisite change.
Root dependencies bind through raw mode-160000 gitlinks, not directory guesses.
Runtime checks also require each direct checkout to be at that commit and clean;
nested dependencies are never initialized or traversed.

The replay scenario retains native EventStore canonical bytes and round-trip
hashes. Its source-bound golden vector includes HTML-sensitive characters,
Unicode surrogate ordering and the number spelling `1.0`. The fixed helper compiles the exact attributed EventStore source fixture; no default
Python test reads `references/` or requires initialized dependencies. Runtime
qualification independently hashes the clean direct EventStore source. Strict
JSON parsing rejects duplicate keys and non-finite numbers before the oracle
checks property order, Web escaping and number tokens. Serialized collection
identities must match their ordinal inventory. The Python evidence JSON encoder
is not used as an EventStore serializer replacement.

Runtime consumption first requires a separately reviewed, independently pinned
entry decision for the exact story and a pre-existing full `authorizationRevision`
that is an ancestor of the implementation candidate. The entry and its independent
trust pin can be committed afterward without a candidate/hash cycle. Actual source,
Git and native evidence still bind the implementation candidate. That decision must
supply all named Platform, enrollment, operational, provider, recovery, operator,
capacity, SM-2 and owner qualifications; Story 16.3 also needs its explicit
AppHost ownership decision. The entry must also bind the exact ordered accepted
predecessor identities and their nonempty, digest-verified evidence inventories.
Qualification-object property order is immaterial; predecessor-array order is fixed.
Adding a JSON file or changing `binding.entry` cannot
supply this trust. Admitting a real decision requires a reviewed successor of the
consumer pins and current source binding. Automated checks cannot provide the
human observations or another owner's acceptance.

## Commands and outputs

Every contract contains the exact five verifier commands and final generator
command. The verifier entrypoints are `verify_story_16_1.py`,
`verify_story_16_2.py`, and `verify_story_16_3.py`. Exit 0 means PASS, 1 means
proven invalid/drifted evidence, and 2 means BLOCKED/unavailable prerequisites.
All applicable results have a nonempty assertion ledger. Runtime commands on
this held checkout return `ENTRY_QUALIFICATION_PENDING` and write no receipts or
final records. Failed and blocked diagnostics remain on stdout.

The final generator independently reevaluates all five native results and
compares each complete derived JSON receipt byte-for-byte. Only then does it
derive scenario six and the 6/6/0/0/0/0 summary. It binds exact source candidate,
raw gitlinks, changed paths, tooling/test/input/schema/receipt/fault identities,
predecessors and the unchanged rollback boundary. JSON/Markdown are deterministic:
the canonical payload excludes the digest envelope, Markdown names that payload
hash, and the final JSON records the Markdown hash. The insertion guard
recomputes this pair and requires verbatim insertion, one atomic native/receipt/
pair publication, and only the designated story/sprint lifecycle edits afterward.
Every committed publication byte must equal the recomputed working evidence;
source changes followed by reverts still fail the per-commit history inspection.
The inherited `story-<id>-final-record-v2.json/.md` destinations are retained.
Existing V9/V14 dispatch and Story 7.3 completion guards remain effective.

`--scope tooling-fixture` exists only for disposable protocol tests. It uses the
same native/semantic/count/source validation but writes under
`artifacts/epic16-tooling-fixture/`, always sets `runtimeQualified: false`, and
cannot write a runtime final record or nominate an entry decision. The test
fixture observations/TRX are intentionally synthetic test inputs, not native
runtime evidence. Passing them grants no runtime, release, host-retirement or
owner-acceptance credit.

All inputs use the neutral descriptor-based contained reader. Writes reuse the
shared protected-output transaction after rejecting symlinks, nonregular paths,
hardlink aliases, protected paths and mismatched output identities. Existing
tracked evidence is preserved. The shared writer protects static path aliases;
it does not claim protection against a hostile concurrent filesystem mutator.

## Reviewed measurement relationships

Pending restart snapshots are Gapped and deny access; every pending sequence is
future to the exact next sequence. Completion CAS binds the measured lease,
version, recovered checkpoint and enlarged horizon. Four unique reverse-save
schedules measure both replicas before and after disable, membership, role and
configuration updates, including their access decisions.

Query mappings derive from observed authorization, existence, provider/index
presence, version, lifecycle, generations, gaps, details, dispatch and rebuild
state. Named cases must describe those inputs. The lifecycle ledger requires the
preserved creation, erase and owner-authorized rebuild causes and transitions.
Erase delivery observations identify durable non-payload references during both
Erasing and Erased, write-before-ack order and unchanged content absence. Rebuild
observations identify the exact authoritative tenant stream inventory at B and
its per-stream replay through captured C; exact-next checks enumerate only the
supplied list, never a range allocated from an artifact integer.

Reconciliation revalidates the first failure and compares pending item, tenant,
runtime and catalog identities. Every dependency edge has missing/invalid-native
regressions. Disposable tests run all three real verifier/generator entrypoints,
assert receipt bytes, then consume them into deterministic fixture-only pairs.

## Iteration 2 protocol completion

Every pending fingerprint must fit the merged recovery horizon, and the rebuilt
watermark must cover captured C. Replay-state JSON has exactly three fields:
`version` (integer 2), `anchorUtcTicks` (the maximum validated contributing event
or lifecycle time), and `items` (the ordered collection identity inventory).
Canonicalization still uses the retained actual EventStore implementation;
provider wrappers and operational metadata cannot enter the hashed state.
Authoritative timestamps are UTC ticks in 1..3155378975999999999. Endpoint
inventory ports are integers in 1..65535.

Tenant acknowledgement observations identify a delivery, its durable outcome
record, the commit timestamp/order and the acknowledgement timestamp/order.
Accepted, duplicate, pending and corrupt outcomes require the same identities
and a durable commit before acknowledgement; provider failure and exhausted
retries have no acknowledgement observation. Healthy authorized and Healthy
denied restart cases separately measure persisted before/after sequence and
freshness, distinct process identities, zero redelivery and consumer-observed
decisions and metadata. The Gapped restart proof remains separate.

Reconciliation carries the pending sequence from first-failure evidence. Both
persisted and queried projection sequences must equal the recovered checkpoint,
which must cover the pending item and prior checkpoint. Equality of two stale
projections is insufficient.

The caller captures its local consumer/dependency/helper source bytes and compares
them with the declared repository before claiming those source identities. A
clean repository with different source bytes fails; identical portable copies
remain usable. Missing or unsafe caller files and helper setup I/O errors retain
structured FAIL/BLOCKED diagnostics. Runtime-path tests use isolated disposable
repositories, test-only trust injection, actual commit ancestry, clean direct
canonicalizer dependencies and genuine pinned xUnit binaries. They never admit
trust or publish runtime evidence in the actual checkout.

## Iteration 3 domain and native-runner protocol

The native test name is its actual fully qualified xUnit class/method. The AC
identity remains in the bound JSON output. A disposable test compiled with the
pinned xUnit 4.0.2 runner executes and emits a complete payload; its untouched TRX
passes this consumer and metadata inspection. No supplied evidence assembly is
executed by the consumer. Expat rejects DTD/entity declarations after decoding
(including UTF-16) before the shared reader parses original bytes. Unknown XML
encodings return structured `NATIVE_INVALID`. Generation resolves its candidate
once and checks that every evaluated row retains that immutable commit.

`epic16_domain.py` is a closed oracle for this controlled native experiment. It
is not a product projection service. Replay retains actual raw Conversations
payloads and authoritative envelopes, validates every duplicate-binding field,
known JSON format, event metadata and computed aliases, then applies the measured
creation, participant, message, file-reference and metadata-update history. Typed
wire identifier prefixes are validated and converted to their internal `Value`
for the controlled state DTO. Business references retain both system and value;
participant role/type, message content, file folder/message references, attributes,
lifecycle and processed identities all enter the v2 canonical state. Collection
keys derive from those complete values. Operational freshness stays outside the
hashed state except its event-derived anchor. Distinct contextual clock reads do
not require distinct completion timestamps; each freshness relation must hold.

The retained `epic16-contract-wire-goldens.json` contains untouched output from
the actual Contracts serializer with synthetic inputs. Its provenance records the
assembly hash, source revision and independently checked source equivalence to
this checkout. ISO DateTimeOffset forms, fractional ticks, numeric schema version,
typed prefixes, business-reference objects, deduplication keys and creation aliases
are covered. `epic16-domain-sources.json` retains attributed source, including the
full-value `ConversationProjectionMaterializer` and actual Tenants read-model/event
semantics, with the upstream MIT notice in `epic16-domain-sources-LICENSE.txt`. Routine tests read these local files; they never read a dependency.

Tenant convergence supplies immutable starting state and contiguous authoritative
records through the enlarged horizon. Every pending fingerprint equals its replay
record, and recovered access/domain state follows actual Tenants transitions. All
four reverse-save schedules have their own authoritative trace. The test actor's
role/configuration requirement is explicitly a conformance control, not production
permission policy. Reconciliation correlates pending envelope/full binding and
preceding history with first failure, applies pending and subsequent contiguous
events, and requires both persisted and production-query values to equal that
expected recovered state at its checkpoint.

Public preservation reads complete tracked Contracts source inventories and blobs
independently at the pinned runtime baseline and candidate. Fixture scope uses its
real parent commit as baseline. Native inventories and digest claims must equal
those measurements. This conservative check proves source preservation; it does
not claim binary API compatibility.

Trusted contract `conformanceControls` pins the existing experiment to three
readiness attempts, 1000 ms readiness timeout and 100 ms cancellation bound. These
are test controls, not approved operational SLOs. Endpoint identities/owners are
fixed; native effective ports remain measured. Collision observations cover every
owned binding plus the unrelated owner before, during and after injection. Actual
structured diagnostics must have the exact safe fields, endpoint identity, owner,
port, stable code and attempt count. Empty field-name lists cannot establish safety.

Each of twelve tenant and eight live faults has distinguishing typed inputs and
outcomes, an exact target, retained before/injected/after bytes and validated
assertion entries. The evaluator derives assertion counts and requires target-bound
restoration. The live cleanup inventory also names the measured collision target.
Generic restored bytes, labels, arbitrary positive counts, personal/token text and
foreign endpoint identifiers fail.

### Fifteen-scenario preservation audit

| Scenario | Preserved proof and added measurement binding |
| --- | --- |
| AC-16.1-01 | Fail-closed states, sequence/identity outcomes, atomic projection/checkpoint versions, tenant keys and seam ownership |
| AC-16.1-02 | Gapped denial, future pending horizon, fenced resumption; Healthy authorized/denied persisted sequence/freshness and restart consumers |
| AC-16.1-03 | Three future deliveries, monotonic horizon, exact pending/authoritative fingerprints, recovered domain state, four trace-derived reverse-save schedules and completion CAS |
| AC-16.1-04 | Twelve typed fault conditions/outcomes/restorations, correlated durable commit before acknowledgement, exhausted retries and query purity |
| AC-16.1-05 | Independently read complete Contracts source preservation, tenant safety, ownership and recomputed restart/convergence dependencies |
| AC-16.2-01 | Raw authoritative envelopes, real contract metadata, selected identity/time, full duplicate bindings and pre-deduplication conflict denial |
| AC-16.2-02 | Actual EventStore canonicalization, complete authoritative-event-derived values, unique ordinal collections, closed v2 anchor, hash/round trip and semantic freshness |
| AC-16.2-03 | Measured control-index query matrix, proven empty Ready state, authorization before reads, sole authority and lifecycle causes |
| AC-16.2-04 | Writer/CAS generation fences, Erasing/Erased durable non-payload references, exact retiring targets, unique tenant B/C streams, watermark and later progress |
| AC-16.2-05 | Independently read Contracts sources, tenant/generation preservation, recomputed replay and query dependencies |
| AC-16.3-01 | Effective endpoint, pinned bounded readiness, healthy resource/unreachable endpoint and exact safe emitted diagnostics |
| AC-16.3-02 | Effective Dapr bindings, affected collision occupancy, unrelated-owner preservation and exact cleanup/diagnostics |
| AC-16.3-03 | Hosted production delivery, durable pending before ack, fail-closed access, route/runtime/catalog and pending event/history binding |
| AC-16.3-04 | Authenticated wire recovery, same pending item/event/history, derived corrected persisted/query values, idempotent clearance and newer-progress fences |
| AC-16.3-05 | Eight typed live faults, environment/product distinction, pinned cancellation, actual safe diagnostics and target-bound cleanup |

The generator's sixth scenario for each story still requires all five measured
scenarios and complete byte-identical receipts. All eighteen actual runtime
commands remain held by null entry and empty independent trust pins.

## Iteration 4 retained input relationships

The controlled experiment now derives the complete Conversations storage and index
keys from the existing UTF-8, unpadded base64url convention. AC-16.1-01 and both
public-surface scenarios retain two named tenants sharing one resource identity,
distinct contents, both writes with before/after snapshots, unchanged other-tenant
state, own reads and denied foreign reads. This tests the shared storage boundary;
it does not claim that Tenants already has a durable client snapshot store.

Tenant transition observations retain actual delivered/accepted event fingerprints,
available/missing/unavailable authoritative history, the composite persisted
projection/checkpoint/health and opaque ETags. Exact-next applies the source event
through one measured CAS; conflicts persist the derived fail-closed health through
the same composite boundary. Verified duplicates and older matches do not write.
No old input can reapply projection content, and ETags have no numeric ordering.

Conversation duplicate-conflict rows retain original and reused envelopes with the
actual changed component. Missing/invalid identity/time cases also retain contract
and position-only counterparts. A separate pair proves that changed transport time
preserves a valid winning contract time. The native wire goldens include null and
non-null closure/archive reasons. Tenants dates use general representable
DateTimeOffset semantics; Conversations retains its year-2000 business constraint.
Configuration strings retain valid empty/whitespace values and nonempty whitespace
keys. Domain equality compares recursive primitive types exactly, and sequence/
checkpoint, raw fault arrays and durable erase/ack times retain native bounds.

The redaction experiment retains creation, target/control messages and the actual
MessageContentRedacted wire payload. Source history remains append-only. The
persisted/production-query observations must show only the targeted derived text
as `[redacted]`, preserve message identity/author/time, leave the control untouched,
and retain the source-derived safe attribution, audit reference and `Redacted`/
`Current` trust vocabulary. General replay explicitly rejects governance events
outside this separate bounded experiment; it cannot silently ignore them.

Both public-surface scenarios retain controlled TenantUpdated/configuration inputs
with distinct synthetic personal canaries and complete emitted Conversations event
and structured log bytes. The checks bind genuine contract/source shapes and the
experiment identity, inspect parsed nested values and raw bytes, and permit public
Conversations content and identities. This proves absence of those controlled
canaries, not a general classifier for personal information. A compiled isolated
copy of the existing default audit diagnostic confirms the exact warning output.

Catalog evidence retains the actual serialized operational-metadata response,
including capability subset arrays. The raw-byte SHA-256 is distinct from the real
EventStore length-prefixed, UTF-8 catalog fingerprint. The independently pinned
experiment uses the enrolled `conversation` app, the host's `v1` service version,
and the existing `conversation-read-model` full-replay route. Named/ordinary and
capability subsets, project/reconcile transport inventory, measured runtime, pending
item location and both first-failure/reconciliation observations must agree.
The bounded control has no optional source-guard/recovery descriptor enrollment;
a different enrollment needs a separately reviewed control. It conveys no runtime
entry or operational approval. Genuine .NET key and catalog serializer/framing
goldens are retained locally, so routine tests do not need submodules.

The fifteen-scenario audit above retains every earlier proof. Its affected rows now
also require: AC-16.1-01 measured transition/CAS and tenant operations;
AC-16.1-05 emitted canary controls and tenant operations; AC-16.2-01 original/reused
conflicts; AC-16.2-03 the complete tenant-derived authority key; AC-16.2-05 source-
derived redaction and emitted-data/isolation controls; and AC-16.3-03/04 catalog
bytes, actual framed fingerprint, route inventories and correlated pending location.

## Iteration 5 stored identity and completion boundaries

Erase targets now retain an exact captured-object inventory, source-event history,
and hashed captured metadata. The controlled **v2 evidence DTO** has `version: 2`
and `lifecycleGeneration`; this anticipates the held runtime protocol and does not
claim an existing native v2 storage implementation. Its summary/detail selectors
use actual stored `tenantId`, `conversationId` and
`freshness.lastAppliedEventPosition` locations, together with pair `dispatchId`.
Lifecycle generation is distinct from source event position and the legacy
position/dispatch ordering tuple. The positive fixture deliberately uses position
3 and lifecycle generation 2. Stable keys derive from the actual captured
source/record tenant and conversation; they contain no generation suffix. Captured,
planned and deleted inventories must match exactly. Full rebuild streams start at
zero and include creation; this experiment has no verified checkpoint snapshot.

Gapped restart snapshots bind the measured tenant and retain valid native Tenants
payloads (including `tenantId` and `disabledAt`), event shape/type and exact raw
fingerprints in both processes. Preserving future pending bytes proves durable
denial, never Healthy application. Raw fault lease/version fields use positive
Int64 bounds; UTC expiry/observation fields use actual .NET tick bounds. Recovered
TenantReadModel descriptions preserve both null and string values.

All materialized Party references use the actual `.Trim().ToLowerInvariant()`
behavior through the fixed .NET helper. Participant/author values and actor/
redaction attribution normalize consistently, normalized participant duplicates
fail, and original raw payload bytes still determine fingerprints. Opaque tenant,
conversation, message and file identifiers retain their original semantics.
Native constructor/deserialization/serialization goldens include Unicode cases
where Python `strip().lower()` differs; attributed source and artifact identities
are retained. Pure normalization results are cached.

Redaction identity must be absent from the preceding processed-event inventory.
Refreshing deduplication keys and expected projections cannot hide reuse. Native
metadata rejects genuine async-void Facts while retaining synchronous void,
Task/ValueTask and fixture constructors. Native run errors/failed cleanup invalidate
otherwise-passing tests with `NATIVE_RUN_FAILED`; informational warnings remain
permitted. Supplied evidence assemblies are never executed.

The complete bound input inventory, future native test source paths, native TRX
(including UTF-16 and CRLF), binary bytes, receipts and final pairs have explicit
checkout policies. A disposable `core.autocrlf=true` checkout and index round trip
verify byte equality. Existing predecessor generator/attribute fixtures are intact.
The runtime insertion caller is exercised with explicit test-only entry and
public-baseline/contract trust in a disposable repository: generated pairs and
verbatim insertion remain `BLOCKED/PUBLICATION_MISSING` until the exact native,
receipt and pair inventory is atomically committed. Production trust remains empty.

The fifteen-scenario audit retains every prior requirement and additionally binds
AC-16.1-02 pending native identity, AC-16.1-03 nullable recovered state,
AC-16.1-04 raw lease/time domains, AC-16.2-01/02 normalized Party identities,
AC-16.2-04 captured stored-object identities and full replay prefix, and
AC-16.2-05 unique redaction identity/normalized attribution. Every native route
checks run completion and every final-record route retains publication admission.
No required owner, operational or runtime qualification is supplied by this tooling.

## Iteration 6 capture and schedule protocol

Runtime evidence requires one closed version-2 capture manifest per native
result (see the [current version-2 requirements](#iteration-9-complete-native-execution-and-negative-controls)).
The original version-1 and 18-file fixture observations in this iteration are
historical, not producer instructions for current evidence. The current path is `artifacts/epic16-runtime/<story>/<scenario>.capture.json`. It binds the
scenario, immutable candidate, exact native path/hash, original codeBase,
original repository root and lexical relative relationship, and retained assembly
path/hash. Retained binaries use the dedicated content-addressed path
`artifacts/epic16-runtime/<story>/binaries/<sha256>.dll`. Five native admissions
inspect their actual managed bytes independently; the final generator deduplicates
identical assembly hashes and publishes the exact manifest/binary inventory with
all five native results, five receipts and final JSON/Markdown. Missing inputs
block; altered, extra, conflicting, aliased or out-of-scope captures fail.

Original absolute paths are metadata only during capture consumption, including
in the original checkout. POSIX and drive-qualified Windows paths have explicit
lexical policies; relative codeBase uses the existing POSIX repository-relative
spelling. Dot segments, repeated or mixed separators, UNC/drive-relative ambiguity,
foreign roots and escapes are rejected. The consumer never reads the original
measurement location, guesses basenames, resolves symlinks or rewrites native TRX.
The raw native bytes and capture digest enter each receipt; identical retained
bytes produce the same final pair after relocation. A genuine pinned xUnit runner
executes five disposable tests once; a fresh Git checkout then regenerates and
verifies their atomic publication with the measurement/build directory unavailable.
Only the fixed trusted metadata helper may compile during consumption. Evidence
assemblies are never executed by the consumer.

AC-16.1-03 adds two full composite before/after schedules tied to the same final
gap inventory and native tenant records. An identical future delivery changes
nothing and commits nothing. A different native fingerprint at the same pending
sequence CAS-commits Corrupt health, with matching projection/checkpoint/pending/
health commit identities, while retaining the original pending evidence and
projection. The independent future-delivery faults retain the maximum of the
previous horizon and every delivered sequence.

Healthy restart observations carry complete tenant state derived from retained
source events, raw immutable version-1 snapshot bytes, exact tenant lookup key,
process and consumer request identities. The logical key is the exact tenant ID
used by `ITenantProjectionStore.GetAsync(string tenantId)` and the current
`InMemoryTenantProjectionStore`; attributed source is retained locally. This
capture DTO does not claim that a durable Tenants provider already exists. Access
is derived from status, membership, role and configuration, not allowed/enabled
labels. Gapped denial and the separate authorized/denied Healthy cases remain.

AC-16.3-02 retains a distinct executed and completed unrelated-only preflight:
all owned bindings are free, the unrelated owner occupies its own port, and the
actual checked-inventory/result/diagnostic observations show no collision. It is
independent of the real Dapr collision and does not require starting the live lane.
A failed readiness probe or endpoint-race fault must reach the pinned retry limit
or timeout. These remain controlled experiment settings, not operational SLOs.

AC-16.3-04 retains an old worker snapshot, authoritative envelope-to-global-position
observations, intervening newer domain state, four rejected CAS schedules (stale
version, expired lease, older generation and older watermark), successful current
clearance and a no-op duplicate retry. The positive stale proposal is checkpoint
3/global position 700; newer stored state is checkpoint 4/global position 1100,
including the additional file-reference event. Each rejected attempt has its own
explicit `storedBefore` reset/fork boundary from the same measured `newerStored`
snapshot; `storedAfter` proves preservation. The current-clearance/duplicate-retry
observations form a separate positive branch. Timestamps are validated inside
each branch and are not a chronological sequence across independent controls.
This isolates expired-lease rejection while all its other fence fields match.
The complete newer projection,
pending inventory, generation, checkpoint and watermark survive every rejected
write. Current clearance uses one measured composite commit and its resulting
state agrees with the production-query observation. Aggregate sequence and global
position domains are never equated.

Opaque identifiers preserve native .NET nonblank semantics and their original
case/whitespace; an exhaustive UTF-16 code-unit comparison checks the exact
`Char.IsWhiteSpace` set. U+001C–U+001F remain valid. PartyId retains its separate
native Trim/ToLowerInvariant behavior. Story 16.3 has disposable entry tests for
missing, unqualified and qualified AppHost ownership; actual entry remains null.

The full fifteen-scenario audit above still applies. This iteration strengthens
AC-16.1-02/03/04, AC-16.3-01/02/04/05 and all runtime publication routes without
changing any AC/requirement identity, graph edge, rollback boundary or predecessor
pin. Legitimate small multicollection histories, ready-empty index-only state and
observation-time freshness semantics remain supported. The earlier review-limit
halt is historical; the user explicitly authorized this sixth continuation.

## Iteration 7 authoritative boundaries and operation order

The fixed helper carries exact bound `global.json` bytes into its isolated build
and checks selected SDK policy independently. Local execution provenance records
its SDK and actual runtime/framework identities; it does not enter portable
measurement receipts. Retained PE files have narrow binary diff/checkout overrides.
Native nullable creation metadata and terminal Closed/Archived histories pass the
same schema and consumer. Stale Open values fail even with new canonical hashes.

AC-16.2-04 derives each aggregate's last sequence at B and C from complete retained
authoritative envelopes with their actual global positions and selected identities.
The positions can be sparse; B=C legitimately permits no sequence advance. Every
reference-only erase quarantine identity is consistent in both directions across
message, stream sequence and global location.

`atomicity` is a controlled future-v2 capture DTO, not an existing runtime storage
provider. Ordered stages retain complete index and logical component snapshots,
writer plans, expected versions/ETags/generations, executed EventStore CAS results
and one shared durable transaction per stage. Erase enters Erasing, then deletes
only captured retiring targets, quarantines a delivery with no derived content,
completes Erased at the same index key, and quarantines a second delivery after the
tombstone. Rebuild enters Rebuilding, captures complete authoritative replay/catch-up
progress, then completes Ready. Only entry advances lifecycle generation; completion
is at that same generation. Existing detail and index keys are derived; other
component identities are logical capture slots, not invented physical keys. Old
detail/dispatch generations remain visibly old until deletion or replacement, and
queries remain fail-closed at intermediate states. Absence slots do not claim stored
objects. Final scalar summaries derive from complete stages. A previously captured
entry plan actually attempts a stale CAS after final progress and preserves every
newer component, index, generation, watermark and quarantine reference.

Erase quarantine and first-failure pending writes retain executed commit/ack
operations in one captured schedule, with the same delivery, durable record,
transaction and actual persisted reference/state. Strict operation ordinals establish
commit-before-ack; timestamps can coincide. Quarantine operations bind the precise
lifecycle-stage transaction and occur in the observed Erasing/Erased phase.

AC-16.1-03 now starts with an empty future-pending set and captures the complete
before/after composite for each of three distinct future deliveries. Each exact
union and monotonic horizon is CAS-persisted while recovery is active. The old
worker captures its expectation after the first merge; its attempted completion
against enlarged pending work is rejected. Complete authoritative replay then
advances the current worker's composite checkpoint, and a separately captured
completion uses the full lease/version/ETag/checkpoint/horizon expectation while
proving the pending set was already consumed by replay. Repeated identical/conflicting deliveries remain independent
branches from the enlarged gap. Four semantic reverse-save experiments remain.

Reconciliation adds independent superseded-lease-only and stale-checkpoint-only
controls, with all other fences current and the lease live. They share the same
explicit newerStored reset boundary as the four previous negative branches; they
are not successive events in a fictitious cross-branch timeline. All fifteen
scenario routes, eighteen identities, thirty-two requirements, graph edges,
predecessors and actual runtime/human-entry holds remain unchanged.

## Iteration 8 executed restart, queue, query and cancellation captures

The completed rebuild watermark is exactly `max(watermarkBefore, C)` in this
controlled protocol. A later queued delivery is retained work, not completed scan
progress. Independently captured preexisting newer same-generation progress remains
valid. All adjacent index, component and writer-plan summaries bind this result.

Raw fault structure validation stops at native tenant domain boundaries: the actual
TenantReadModel validator owns configuration/member maps. Keys such as `version`,
`allowed` and `history` remain opaque domain keys, with their native value rules.
Acknowledgement delivery and durable-record identities use exact .NET-compatible
nonblank validation, including native-valid U+001C through U+001F.

The future-v2 restart schedule retains the complete Gapped tenant snapshot,
projection, pending fingerprints, freshness, exact logical lookup, worker/process,
lease, version, ETag and checkpoint/horizon. One executed transaction acquires the
replacement lease and record version; a persisted-pending trigger resumes the
captured interval without a new delivery. Independent old-worker, lease-only,
version-only, checkpoint-only and horizon-only attempts reset to that acquired
snapshot. Their otherwise-admissible pending-preserving renewal is rejected; a
current-worker positive admits the same renewal and remains Gapped. Both old and
new lease times are live for the isolated controls. No Tenants business-event date
is treated as a projection freshness clock. Healthy restart observations remain.

Rebuild catch-up captures each transport arrival and its immutable authoritative
tenant, stream, sequence, event/message identity, position and generation. A hash
of that exact non-payload reference is the capture deduplication identity, not a
new physical storage key. Each executed enqueue retains full before/after snapshots,
expected CAS fences and one index/component transaction; correlated durable commit
and acknowledgement operations establish ordering. Duplicate and reordered arrivals
are valid and preserve a unique reference set. Before/at-B observations do not enter
the queue. A drain starts from the actual last enqueue snapshot, consumes exactly
through C and shares the rebuild work transaction; every after-C reference remains
in the Ready component. Identity comparisons include retained authoritative history,
not only new arrivals. New tenant-scoped streams created after C may remain queued
without belonging to the earlier B manifest. This is an evidence DTO, not a queue
or durable provider implementation.

Every lifecycle entry, deletion/work, quarantine and completion boundary has an
executed query, ordered after its exact durable transaction and before the next
commit. Captures include complete observed snapshots, inputs, returned state/reason/
content, exact read calls and empty write/recovery calls. The authorized available
provider controls demonstrate actual reads on both sides of cutover; Erasing,
Erased and Rebuilding never disclose derived content. Separate query captures use
source-derived Ready data and isolate index/detail/dispatch mismatches. Named fields
inside one control record establish internal contradiction and map to
`Unavailable/metadata_contradictory`; cross-key generation mismatch maps to
`Rebuilding/mixed_generation`. The actual query sources and public reason vocabulary
are retained with attribution in the local source fixture; runtime code is unchanged.

Cancellation has one operation/clock identity and executed start, request,
observation, cleanup and terminal completion. Monotonic observations allow equal
clock ticks while operation ordinals prove order. Owned resources and endpoint ports
are correlated with their acquisition and exact release; preexisting resources
survive every cleanup boundary. Elapsed cancellation derives from request through
terminal cleanup/completion and retains the independently pinned 100-ms test bound.
This isolated conformance control establishes no production SLO.

All fifteen scenario routes and inherited identities, graphs and rollback obligations
remain intact. These synthetic test captures do not qualify actual runtime entry,
provide owner approval, publish runtime evidence or lift Story 16.1's hold.


## Iteration 9 complete native execution and negative controls

Current portable captures use closed version 2. The historical version-1 and
18-file fixture observations above remain historical. The independently pinned
product policy requires Conversations Server/Contracts, Tenants Client/Contracts,
EventStore Client/Contracts/DomainService/Server and Commons.TenantAccess, including their actual owning
surfaces. Native output from the selected test carries its loaded inventory;
capture manifests bind each original lexical codeBase, retained content-addressed
PE bytes, MVID, managed identity and source revision. Root products bind the one
candidate; upstream products bind raw direct gitlinks and clean dependencies.
No observation chooses its own expected owner or required assembly. Generation
rereads every retained capture/binary and checks aliases against admitted digests;
publication inventory is derived, including upstream API baseline artifacts.

The genuine disposable test loads nine explicitly synthetic source-stamped product
stand-ins and emits their actual metadata and hashes in untouched xUnit TRX. Its
fresh-checkout generation/insertion loses the original runner, product/build and
payload directories. This proves interoperability and retention, never real product
qualification. The actual runtime remains blocked with no native capture artifacts.

AC-16.1-05 independently compares baseline and candidate public metadata for all
five upstream assemblies, binding them to owning repository history via raw root
gitlinks. Supported declaration contracts must remain; legitimate concrete/default
members may be added. New abstract implementor obligations fail. Unsupported
native/forwarded shapes fail, and this conservative comparison does not claim
general binary compatibility. Root Conversations Contracts source remains exactly
preserved. Warning removal additionally reads the actual Program.cs baseline and
candidate bytes after both restart and convergence pass; a retained active warning
fails. Historical release disclosures are immutable and actual Program.cs is untouched.

AC-16.2-02 retains executed negative writer operations with actual invalid native
collection identities/order or generation before the event-derived anchor. Captured
complete before/after bytes remain equal, no write commits, and contradictory
freshness maps to existing Unavailable/metadata_contradictory without clamping.
Valid zero lag and coincident generation instants remain accepted. Query lifecycle
and provider domains are closed before mapping precedence; component tenant/resource
identity and pending dispatch agree with the index. The cross-key experiment requires
its actual detail/dispatch controls, without imposing nonempty physical components
on legitimate initialized-empty queries.

AC-16.3-04 retains ordered executed request, authorization, hosted dispatch, durable
result and response observations on POST /project/v2/reconcile. Existing
ProjectionDispatchRequest/Response and EventStoreWorkload:domain-service:project
vocabulary is source-attributed. Content-safe request scope and actual per-projection
Completed/AlreadyCompleted response outcomes bind the same pending work/catalog.
HTTP 200 alone is insufficient. Transaction/clearance links are explicitly future
capture instrumentation, separate from the actual response body. The wire capture
contains no credentials or event/state content: its persisted-state digest is
recomputed from the separately validated complete concurrentRecovery snapshot.
Readiness output includes safe structured retry advice bound to the measured
terminal outcome and the pinned three-attempt/1000-ms conformance policy. This
policy is an experiment control, not a new production SLO.

All three wrappers report unavailable internal/external Python imports as nonempty
BLOCKED/exit 2 before any output publication. Unrelated implementation exceptions
are not swallowed. Runtime entry remains null, independent trust stays empty,
accepted CLI pins remain 4.35.2, and the original eighteen scenarios, thirty-two
requirements, graph and rollback boundaries are unchanged.

## Correlated native admission

Candidate upstream API declarations in AC-16.1-05 must come from the same owned
SHA-256, MVID and source revision as the product DLL captured by that native test.
All five scenarios must use one consistent nine-product implementation inventory.
Test run identities and timestamps may differ. Final generation rereads and retains
all required native test/product and baseline API binaries, deduplicating only
identical content-addressed paths. Fixture authors deliberately reuse the admitted
product files; compiler caching is not their correlation mechanism.

AC-16.1-05 and AC-16.2-05 retain `compiledContracts.baseline` and `.candidate` in
addition to the unchanged complete tracked Contracts source inventory. The baseline
binds the required root revision; the candidate binds the actually loaded Contracts
DLL in runtime scope. Both supported public declaration inventories must be equal.
The source inventory alone cannot detect external Compile removals or generated
public code. Baseline and candidate binaries join the exact publication inventory,
so fresh-checkout verification requires neither their original build directories
nor rebuilding product evidence. This remains conservative declaration comparison,
not a general binary or behavioral compatibility claim.

AC-16.2-03 now retains two executed authorization query captures. Request/response
correlation, tenant, operation order and actual `ListConversationsQuery` and
`ConversationListResult` member values establish the unauthorized/nonexistent
control. Both return the native `Hidden` shape: Forbidden/forbidden, no summaries,
zero returned count, no cursor, and the existing safe next action. No index read
or repair occurs. The versioned capture's operation metadata is instrumentation;
it introduces no product DTO, endpoint or public vocabulary. Attributed native
contract/query source is retained locally. Leaking negative values are synthetic.

Native TRX must contain exactly one TestMethod and one Execution per UnitTest.
The lazy generator import reports genuinely absent declared dependencies as
unavailable while allowing internal import defects to surface. Legacy standalone
routes and historical generator bytes remain unchanged.


Iteration 11 completes executed outcome observations. The runtime native JSON has
exactly six fields: `scenarioId`, `candidate`, `sources`, `contractSha256`,
`observations`, and `loadedProducts`. Only the synthetic `tooling-fixture` shape
may omit `loadedProducts` and contain five fields. Runtime capture version 2
retains the selected test binary and all nine required owned product binaries,
correlated with raw TRX; baseline/current compiled API artifacts also join the
exact deduplicated publication inventory. Synthetic fixture success grants no
runtime entry, owner qualification or product execution claim.

AC-16.1-04's controlled durable DTO retains source state/event, delivered native
fingerprint, complete before/after projection/checkpoint/health/pending state and
CAS expectation. Its executed commit and acknowledgement identify one durable
transaction and the same actual persisted contents. The duplicate case observes
an existing committed outcome with zero new writes; failure/exhaustion have no
commit, acknowledgement or durable evidence. Operation order permits equal ticks.
These are evidence captures, not public product DTOs or new physical storage keys.

Each AC-16.3-04 stale branch now contains a request, executed CAS attempt and
completed result with one operation identity and exact scope. Canonical digests
bind its expected/proposed and stored before/after snapshots to those operations.
Each branch remains an independent reset/fork without a cross-branch clock order.
Duplicate retry executes after the current wire response, correlates that request
and clearance transaction, and returns AlreadyCompleted without mutation.
Multi-resource erase derives index dispatch references by concatenating each
source-derived resource component's references, retaining multiplicity.

Iteration 12's versioned tenant captures retain the executed experiments behind
three earlier summaries. Each of the four reverse-save branches derives its
older/newer full projection and checkpoint from native replay, records the newer
successful CAS followed by the stale rejected CAS against that same durable
record, then observes both distinct replica processes reading the committed
snapshot. Request, operation, tenant, logical store record, ETag/version, result
and transaction are correlated. Only order within a branch is meaningful.

The six authorization rows retain an executed CheckAccessAsync request, composite
provider/store observation and completed native decision. This is a future-v2
instrumentation DTO around the existing ITenantProjectionStore tenant-id seam;
it does not assert a durable runtime implementation exists. Missing and failed
reads contain no fabricated snapshot or checkpoint. Available snapshots retain
native domain state, source event/fingerprint, checkpoint, pending/conflict cause
and freshness. Healthy, Stale, Gapped and Corrupt derive from these facts.
The independently pinned `tenant-access-observation-v1` control classifies age
greater than 100 ticks as Stale; equality and zero lag are valid. This deliberately
small synthetic test threshold is neither a production SLO nor owner policy.
Native Read/Write/Admin/Governance permission and denial vocabulary derive from
current attributed Conversations/Commons code. Configuration remains an open
native string map; it is not added to that service's permission policy. The
older reverse-save configuration experiment retains its separate existing
`conversations.enabled=true` conformance control. Consumer reads never repair
or schedule recovery.

Provider-failure and retry-exhaustion rows now retain complete native source and
controlled before-state, executed delivery-start observation, one or three ordered
failed provider attempts, terminal retryable completion, and an executed empty
acknowledgement inventory for that same delivery. Restoration and a subsequent
read prove no changed projection/checkpoint/outcome. The capture references the
exact existing fault's injection/restoration bytes. The current controlled retry
case uses provider unavailability on all three attempts. Its absent provider reads
remain null; the after-state becomes observable only after restoration. Start
observation completion means the start was recorded; terminal delivery processing
has its separate correlated completion. Successful/duplicate durable outcomes
retain the earlier commit-before-ack and zero-reapplication semantics.

Catch-up enqueue transactions are distinct from every lifecycle stage transaction;
the existing drain/work transaction remains shared. Native source stamps, supported
Task/ValueTask returns and ParamArray constructor supply require actual approved
framework ownership. SDK selection/build failures retain the actual exit and
separate 2,000-character stdout/stderr tails while returning structured BLOCKED.

## Iteration 13 construction, owner and publication boundaries

The independently pinned implementation inventory additionally includes
Hexalith.EventStore.Server (ProjectionDeliveryReconciler and
ProjectionDeliveryRetryWorker) and Hexalith.Commons.TenantAccess
(TenantAccessEvaluator). All five scenarios bind the same nine exact loaded
implementations, their source revisions/owning root gitlinks, MVIDs and retained
bytes. The public-additivity promise remains the five named upstream assemblies
in upstreamApiAssemblies; these two added implementation owners do not expand it.

Final generation rereads every raw TRX against its admitted digest and rejects
symlinks/hardlinks, alongside the existing capture/binary rereads. Raw native files
remain their existing separate publication paths. Counts derive from the exact
inventory. This boundary does not claim protection against an arbitrary concurrent
filesystem adversary. Fixed helper rejection is distinguished from unavailable
host/runtime startup; the latter yields bounded diagnostic BLOCKED results.

Test construction validates the selected typed default and complete supported
xUnit provider attribute constructor/blob. Valid Optional, params, primitive,
nullable primitive, null, local enum/value type, decimal and DateTime defaults
retain the documented metadata subset. A real fixture/framework provider takes
precedence over an unused default. Nonnull Nullable<Enum> constants are unsupported
because the pinned runner receives an underlying integer, not an enum instance.
CollectionDefinition's DisableParallelization Boolean is a supported named option.
Malformed named counts, signatures, values and trailing data reject; generic forms
are equally checked. Task/ValueTask also retain their encoded class/value kind.
No supplied evidence assembly, constructor or attribute executes in inspection.
