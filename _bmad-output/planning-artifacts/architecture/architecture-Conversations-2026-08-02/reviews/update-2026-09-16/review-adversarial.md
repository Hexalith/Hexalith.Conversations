# Adversarial Divergence Review — V15 Update Candidate

## Final Re-review After Bootstrap-Authority Patch

**FINAL VERDICT: PASS — no critical or high adversarial divergence remains.** This section supersedes both earlier review stages below.

ADV15-R1 is resolved. AD-3 now separates the source-pinned V21 bridge diagnostic from the AR-15 exception authority, declares `BLOCKED_BOOTSTRAP_AUTHORITY`, and prohibits opening, accepting, or merging a recovery candidate until a no-bypass organization ruleset installs the external validator (`ARCHITECTURE-SPINE.md:2826-2849`). The contract fixes the external identity, requires its exact digest, fixes the isolated command and output schema, binds repository/parent/candidate trees plus policy/path/gitlink digests, and consumes a single-use nonce on merge (`:2850-2859`). Its ordinal eight-path allowlist rejects renames, mode changes, symlinks, submodules, dependencies, and every unlisted path (`:2861-2873`). Candidate validation, atomic V22 publication, and protected post-merge activation are now separately ordered (`:2874-2885`).

The validator bytes do not yet exist, but that is a safely deferred external prerequisite rather than a competing authority: the explicit blocked state and Deferred gate prevent either a strict V21-only gate or an unstated permissive wrapper from authorizing work (`:3181-3188`).

All other prior critical/high findings remain resolved: monotonic tenant-gap recovery, lifecycle generation and cutover, replay identity/time/canonical bytes, and immutable Story 7.1 acceptance.

**Final gate statement:** PASS for the V15 architecture spine. Current execution remains correctly blocked until the named external bootstrap and other Deferred entry conditions are satisfied.

---

## Initial Review Before Patches

- **Review date:** 2026-09-16
- **Target:** `/tmp/hexalith-architecture-v15-lint/ARCHITECTURE-SPINE.md`
- **Effective overlay:** `conversations-architecture-2026-09-16-v15`
- **Mode:** reviewer gate; the spine was not edited
- **Lens:** construct two units one level down that obey the stated decisions yet can still produce incompatible authority, authorization, lifecycle, replay, or operational outcomes

## Verdict

**FAIL — 2 critical, 3 high, and 1 medium divergence holes remain.** V15 materially closes the V14 review's ownership, single-record lifecycle, time-precedence, and fail-closed-state gaps. It still leaves the authority-recovery publication order internally inconsistent, the tenant gap-recovery horizon unsafe under multiple out-of-order deliveries, and the Conversation lifecycle, replay bytes, Story 7.1 terminality, and operational gate insufficiently total for independent builders.

## Severity Register

| ID | Severity | Live divergence |
| --- | --- | --- |
| ADV15-1 | CRITICAL | AR-15 must publish the resolver atomically, while AD-9 requires that resolver to have landed before the publication it must authorize |
| ADV15-2 | CRITICAL | `Gapped(expected, observed, fingerprint)` does not define the durable high-water/ack protocol for multiple future deliveries, permitting an authorization event to be omitted from recovery |
| ADV15-3 | HIGH | The index-v2 lifecycle has no legal transition table or rebuild/live-delivery cutover, so erase can resurrect data and rebuild can lose or overwrite post-barrier work |
| ADV15-4 | HIGH | “Contradictory time” and canonical persisted JSON/hash bytes remain undefined at the producer/verifier boundary |
| ADV15-5 | HIGH | Story 7.1 is called terminal at `ACCEPTED`, but “any drift” sends it back to `ACTIVE`, allowing incompatible successor-unlock behavior |
| ADV15-6 | MEDIUM | The operational-envelope gate has no pinned acceptance contract, and its Deferred row narrows the lift scopes named by AD-5 |

## ADV15-1 — CRITICAL — The one recovery publication has contradictory ordering and no uniquely bound bridge invocation

**Builder pair.** A recovery publisher implements `AR-15`; protected CI decides whether the publication may become current.

**Letter-compliant constructions.** AD-3 says one atomic transaction publishes V22, its pointer overlay, the generic marker resolver, tests, and protected preflight, and V22 becomes current when protected `main` runs that resolver (`ARCHITECTURE-SPINE.md:2818-2828`). AD-9 and the matching Deferred row say the current inventory/resolver and conformance test “must land before any successor planning-authority publication” (`:2977-2980`, `:3030`). One publisher treats “land before” as “execute earlier inside the same candidate transaction”; another requires the resolver to exist on protected `main` before V22 is proposed.

**Incompatibility.** The first can publish the atomic V22 transaction; the second correctly rejects it because its trust root has not landed. Publishing the resolver first is itself a successor planning-authority publication outside AD-3's only allowed atomic transaction, so the strict reading deadlocks. The temporary bridge is also prose-selected as “V21 current checker” (`:2827-2829`) rather than identified by checker path, source commit/digest, command, and result schema in the V15 marker. A human can infer the present source-pinned workflow, but two marker consumers do not have one mechanically selected invocation.

**Impact.** This is the only route out of the current authority deadlock. Implementations can either remain permanently blocked or admit a recovery candidate under a trust-order interpretation another conforming gate rejects.

**Disposition: AUTOFIX.** State one bootstrap ordering explicitly: name the immutable external trust root; bind the bridge checker path/source digest/command/result schema; allow the resolver/preflight candidate to validate the whole atomic V22 transaction before merge; then require the protected post-merge rerun. Replace “must land before” with language that distinguishes candidate validation from protected activation.

## ADV15-2 — CRITICAL — Gap recovery has no total multi-delivery horizon or acknowledgement rule

**Builder pair.** The EventStore delivery/recovery substrate receives sequences `n+2`, `n+4`, and `n+3` while the Tenants projection remains at `n`; the Tenants transition owner persists the fail-closed checkpoint.

**Letter-compliant constructions.** AD-6 requires each future sequence to record `Gapped(expected, observed, fingerprint)` without advancing and later replay `expected..observed` (`:2887-2896`). A provider may retain the first observed gap and reject/park later deliveries, recovering only through `n+2`. Another may replace `observed` on every CAS, including with the last arrival `n+3`; a third may retain the numeric maximum `n+4`. All remain `Gapped`, do not advance the accepted checkpoint, and use the same restart-safe lease.

**Incompatibility.** The record carries one observed value and fingerprint, but the rule does not say whether it is the first gap edge, last arrival, or monotonic maximum; whether later deliveries are acknowledged, durably parked, or redelivered; or whether a lower future arrival may reduce the recovery horizon. If `n+4` is acknowledged and a later `n+3` replaces the horizon, recovery can return `Healthy(n+3)` while the disable/role/configuration event at `n+4` has been omitted. Both provider and consumer conformance suites can pass the named single-gap cases while disagreeing on this schedule.

**Impact.** A lost disable, membership, or role event can reopen authorization after recovery; fail-closed behavior during the gap does not repair the wrong terminal `Healthy` state.

**Disposition: DISCUSS / UPDATE.** Define a monotonic recovery high-watermark plus the durable pending identity set (or mandate non-ack/redelivery), CAS rules for additional future and late events while `Gapped`, lease handoff behavior, and the exact condition for returning `Healthy`. Add a three-or-more-event reverse/interleaved schedule to the mandatory provider and consumer suite.

## ADV15-3 — HIGH — Index-v2 lifecycle transitions and rebuild cutover remain non-total

**Builder pair.** A lifecycle/erase adapter and the live Conversation projection handler both submit intents to the sole writer while rebuild or erase is in progress.

**Letter-compliant constructions.** AD-7 gives one key, one writer, CAS ownership, `Erasing -> Erased`, and `Rebuilding -> Ready` after manifest streams are gap-free through a barrier (`:2908-2927`). It does not define which incoming intents are legal from `Erasing`, `Erased`, or `Rebuilding`, nor how live events after the captured barrier are buffered and cut over. One writer can reject every Conversation event while `Erased`; another can treat the next exact-sequence event as an intent to recreate a summary and return to `Ready`. During rebuild, one coordinator can buffer post-barrier events; another can allow live CAS merges and later install the rebuilt manifest.

**Incompatibility.** The erase implementations disagree on whether derived content may reappear without an explicit lifecycle reactivation. The rebuild implementations can both prove every discovered stream through the barrier yet disagree on post-barrier state; a late rebuild CAS may overwrite live summaries/watermarks or expose `Ready` before queued events are applied. “Discovered stream” also lacks a discovery source and snapshot rule, so a stream absent from a stale index can be omitted from one manifest but included in another.

**Impact.** This can resurrect erased/redacted content or lose current projections while all reads remain superficially `Ready`.

**Disposition: DISCUSS / UPDATE.** Add the legal lifecycle transition table, epoch/generation fencing, manifest discovery source, post-barrier buffering/catch-up rule, CAS merge precedence, and explicit policy for events received during/after `Erasing` and `Erased`. A `Ready` transition must prove both through-barrier completeness and safe handoff to live delivery.

## ADV15-4 — HIGH — Replay time is selected, but conflict and byte-canonicalization semantics are not

**Builder pair.** The materializer produces index/detail state; the rebuild verifier independently computes replay hashes.

**Letter-compliant constructions.** AD-8 selects payload `OccurredAt` for Conversations events and envelope `Timestamp` for position-only events (`:2944-2950`), but then declares “contradictory time” corrupt (`:2951-2954`). A materializer may treat a payload/envelope mismatch as expected because payload time has precedence; a verifier may treat the two immutable times as contradictory. For JSON, one unit can use the public `ConversationsJsonContext` options and hash pre-store serializer output; another can use the internal read-store serializer/provider bytes and sort a collection by a different natural ordinal key. Both can claim source-generated options, UTC normalization, and ordinal ordering (`:2956-2959`).

**Incompatibility.** The same history can be accepted by one unit and become `Unavailable/metadata_contradictory` in another. Even when both accept it, property ordering, dictionary/set canonicalization, collection sort keys, null/default emission, and pre-store versus round-tripped provider bytes can yield different hashes. The current public source-generated context does not itself name the new internal index-v2 persistence shape.

**Impact.** Rebuild equivalence and evidence hashes can fail across providers or, worse, certify different bytes as canonical.

**Disposition: AUTOFIX.** Say explicitly whether non-selected payload/envelope differences are ignored, recorded diagnostically, or corrupt, with a tolerance only if intended. Name the exact serialization context/type metadata, canonical sort key per collection, property/null/default rules, and the exact byte sequence hashed; require provider round-trip equality separately from canonical replay hashing.

## ADV15-5 — HIGH — `ACCEPTED` is simultaneously terminal and drift-revocable

**Builder pair.** The Story 7.1 terminal publisher unlocks Story 7.2; the descendant protected-branch checker evaluates a later unrelated commit.

**Letter-compliant constructions.** AD-4 calls the transition terminal and requires a separate atomic pointer publication to reach `ACCEPTED`, but also says “any drift or failed gate returns to `ACTIVE`” (`:2844-2850`). One implementation treats `ACCEPTED` as immutable completed history and subjects only pre-acceptance states to drift. Another reruns the current checker on every descendant and moves accepted Story 7.1 back to `ACTIVE` after an unrelated gitlink/path change, matching the literal any-drift rule.

**Incompatibility.** The first leaves Story 7.2 legitimately unlocked; the second relocks it or invalidates already-produced successor evidence. The rule also does not state whether the candidate-matched final record binds the feature head, protected synthetic merge candidate, or committed `main` identity when integration strategy changes the commit.

**Disposition: AUTOFIX.** Make `ACCEPTED` irreversible historical completion. Scope drift rollback to `EXECUTION_ALLOWED` and `MERGE_CANDIDATE_VERIFIED`; define which identities/tree/parents the merge proof and final record bind; and state what a later incompatible descendant invalidates (release/current-candidate evidence), without reopening the completed story.

## ADV15-6 — MEDIUM — The operational deferral is safe by default but not mechanically satisfiable in one way

AD-5 requires `production-operational-envelope-v1.md` before **every** future lift, including planning, checkpoint, schema, and story lifts, and says its gate must pass (`:2860-2873`). The Deferred row shortens that to Story 16.1 or product/runtime/release lifts (`:3025`). The artifact has no schema/version digest, approval/status field, named validator, or exact parity/waiver result vocabulary. One gate can accept a reviewed Markdown narrative for a schema-only lift; another can require provider-backed staging proof and a signed waiver. Both can claim the categories are named.

The missing artifact correctly keeps the current state `ACTIVE`, so this is not an immediate unsafe execution path. It becomes divergent at the first attempted lift.

**Disposition: AUTOFIX.** Make the Deferred row repeat all AD-5 scopes. Bind a versioned schema or companion machine record, owner approval, validator command/result semantics, artifact digest in every lift record, and the minimum evidence required for parity versus a time-bounded Release-owner waiver.

## Deferred-Gate Audit

- `conversations-vocabulary-v1.json` is safely stop-gated before an affected successor enters review.
- AppHost interpretation is safely entry-gated before Epic 12 or Story 16.3.
- Optional UI policy is safely gated before UI activation.
- Portable-tier work is gated only before Epic 9 completion, but the named Quality owner and existing canonical story authority make it lower risk than the findings above.
- AR-15, Story 7.1, resolver, and production-envelope deferrals default to `ACTIVE`; however ADV15-1 and ADV15-6 show that their satisfaction criteria are not yet uniquely executable.

## Closure Assessment Against The 2026-09-16 V14 Review

| Prior area | V15 result |
| --- | --- |
| Current authority/raw V21 lift confusion | **Substantially closed** by V15's pointer and point-in-time qualification; recovery bootstrap remains ADV15-1. |
| Cross-replica unconditional tenant save | **Substantially closed** by CAS, identity, retry, and recovery ownership; multi-delivery recovery remains ADV15-2. |
| Single lifecycle/watermark owner and key | **Closed** at authority/key level; transition/cutover behavior remains ADV15-3. |
| Replay time source | **Closed** at precedence level; conflict and canonical bytes remain ADV15-4. |
| Vocabulary sidecar | **Safely deferred**, still not decided. |
| Operational envelope | **Owned and stop-gated**, but the gate contract remains ADV15-6. |
| Tenant-gap recovery owner | **Closed** by EventStore recovery ownership; recovery horizon remains ADV15-2. |

## Gate Statement

V15 should not be handed off as a convergent build substrate yet. Keep every hold `ACTIVE`; fix ADV15-1, ADV15-4, and ADV15-5 directly in the spine, and either fix ADV15-2/ADV15-3 here or add explicit pre-entry architecture gates that prohibit Stories 16.1/16.2 until their total transition and recovery contracts are published.
