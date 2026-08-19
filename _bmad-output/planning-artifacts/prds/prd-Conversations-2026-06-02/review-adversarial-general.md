# Adversarial Review — PRD: Conversations Boilerplate Reduction (finalized 2026-07-14)

Reviewed: `prd.md` (732 lines), `addendum.md`, `.memlog.md`, plus spot-verification of the evidence
artifacts the PRD leans on (`docs/release-evidence/*`). Review date: 2026-08-18.

## Verdict

This PRD is unusually disciplined about anti-gaming language — and then quietly commits the sins it
forbids. Its deepest problem is temporal dishonesty of form: it is written as a plan with
before-first-change preconditions (Phase 0 freezes, FR-20's "before the first refactor change"
manifest) while simultaneously reporting, inside the same finalized document, that the refactor is
70.43% complete and the primary target is "met." A requirements baseline cannot be both a
precondition-bearing plan and a mid-flight status report; every gate that reads as a precondition is
in fact a retroactive reconstruction, and the PRD neither admits this nor cites the reconstructed
artifacts it depends on. Around that core defect cluster several real hazards: a performance gate
whose measurement altitude is unspecified (and was, in reality, satisfied by an in-process
microbenchmark structurally blind to the §8 invariant it supposedly counterbalances), a secondary
metric (SM-3) that is unachievable inside the declared scope, a Promote-classified area with no
realizing FR, an SM-1 denominator shrunk ~4.7k LOC by a reclassification with no citable decision
authority, and an SM-2 attainment figure sitting at exactly 50.00% on self-declared low-confidence
evidence. The preserved §14 baseline is largely sound as a constraint set, but its blanket
"constrains FR-20/SM-C1" disposition is vacuous for the third of it that is process requirements.
None of this is fatal to the initiative — most of the underlying engineering evidence exists in the
repo and is better than the PRD's citations of it — but as a standalone contract this document lets
two reasonable implementers build incompatible gates, and lets a motivated one pass every metric
without delivering the promise.

---

## Critical

### C1. The PRD is a post-hoc document masquerading as a plan: its Phase 0 / FR-20 preconditions are unsatisfiable given its own status claims

**Location:** §5.3 Phasing; §6.5 FR-20; §7 SM-1/SM-2; §2 readiness table; frontmatter (`updated: 2026-07-14`).

**Evidence:** FR-20: "Before the first refactor change, the initiative produces and versions a
preservation manifest from an accepted green pre-refactor build." Phase 0: "freeze the versioned
pre-refactor preservation manifest from a green build, and capture the reproducible pre-refactor
P95 command/read benchmark." Yet §7 SM-1, in the same finalized document: "Current evidence:
70.43%; target met." A finalized PRD reporting 70% attainment has, by its own math, already executed
Phases 1–3 — so its before-first-change preconditions are either already satisfied (in which case
the PRD must cite the frozen artifacts, and it cites neither the preservation manifest nor the
SM-C2 baseline by path or version, while it *does* cite artifacts for SM-1 and OQ-2) or were
violated. Repo check confirms the latter for SM-C2: the baseline artifact
(`docs/release-evidence/sm-c2-hot-path-baseline-v1.md`) is dated 2026-07-31 — seventeen days
*after* finalization — and is a self-declared reconstruction: "The production-path fixture does
**not** exist at source commit `29def441…`, so this is the AC1-permitted reconstruction." "AC1" is
a story-level acceptance criterion that appears nowhere in this PRD; the PRD contains no
requirement governing retroactive-baseline fidelity at all.

**Why it matters downstream:** Architecture and epic generation cannot tell which phases are done,
which gates are live preconditions, and which are post-hoc reconstructions. Anyone auditing the
initiative against the PRD text alone will conclude Phase 0 was violated; anyone defending it must
reach for artifacts the PRD refuses to name. Worse, the PRD's inviolable gates (FR-20, SM-C2) hang
on baseline artifacts whose existence and provenance the document never binds.

**Fix:** State actual per-phase execution status at finalization. Cite the frozen preservation
manifest and the SM-C2 baseline artifacts by path/version in FR-20 and SM-C2, exactly as SM-1 cites
`consume-promote-keep-inventory-v1.json`. Add an explicit requirement for when a pre-refactor
baseline may be reconstructed retroactively and what fidelity evidence (commit identity, byte-
identical fixture overlay, identical envelope) that reconstruction must carry.

### C2. SM-C2's measurement altitude is unspecified, so the 5% gate is satisfiable by the weakest possible benchmark — and the implemented gate chose exactly that

**Location:** §7 SM-C2; §8 Cross-Cutting NFRs (Performance bullet); §5.3 Phase 0.

**Evidence:** SM-C2: "For every identified command/read hot path, post-refactor P95 latency must be
no more than 5% worse than the frozen pre-refactor P95 under the same reproducible benchmark
envelope." The required evidence list (workload/data shape, concurrency, environment, tool
versions, warm/cold, repetitions, raw results, commit identities) never pins the *boundary* of the
measurement: in-process, sidecar-inclusive, transport-inclusive? Meanwhile §8 asserts: "Shared
capabilities must not introduce synchronous cross-service calls on hot paths" — an invariant that an
in-process benchmark is structurally incapable of checking. The implemented baseline confirms the
loophole was taken: `sm-c2-hot-path-baseline-v1.md` records HP-CREATE P95 = **1.76 µs/op** and
states "this remains an in-process warm-path benchmark, not a sidecar/network latency test." Five
percent of 1.76 µs is 88 nanoseconds; a promoted capability that adds one synchronous sidecar call
(milliseconds) on a hot path would be invisible to this gate, because the added call is not inside
the measured closure. Additionally, "every **identified** command/read hot path" has no identifying
artifact in the PRD or addendum — an empty identification passes the gate vacuously.

**Why it matters downstream:** SM-C2 is claimed to "counterbalance over-abstraction from
promotions" (§7) and is one of only two counter-metrics. As written it counterbalances nothing: the
regression class most likely to be introduced by promotion (a new cross-process hop, extra
serialization boundary) escapes an in-process fixture entirely, and §8's no-sync-cross-service-call
rule has no verifying gate anywhere in the document.

**Fix:** Pin the envelope boundary in SM-C2 (state explicitly that it is in-process, and what that
does and does not prove). Reference the hot-path inventory artifact normatively. Add a separate
testable consequence for §8's cross-service-call invariant (call-graph assertion, trace-based
check, or dependency-boundary conformance test), since a P95 number cannot carry it.

---

## High

### H1. SM-3 is unachievable inside the declared scope — "single source of truth" while fleet migration is out of scope

**Location:** §7 SM-3 vs §5.2 Out of Scope; addendum §E rows 2–4.

**Evidence:** SM-3: "Count of boilerplate patterns that now have a single shared home **instead of
N copies** … Target: every in-scope promoted pattern has exactly one source of truth." §5.2: "Fleet
migration of Folders, Projects, Memories, Parties, or Tenants onto the promoted libraries is a
named follow-on." The duplicated copies live in Folders and Projects (addendum §E: tenant-access
handler "Folders, Projects … structurally identical"; client registration "Folders, Projects …
identical"). The pilot never touches those modules, so post-pilot every promoted pattern has a
shared home *plus* the surviving sibling copies. "Exactly one source of truth" is false by
construction within scope.

**Why it matters downstream:** A secondary success metric that cannot be met as written will either
be scored "failed" against a scope decision the PRD itself made, or silently reinterpreted at
acceptance — both outcomes discredit the measurement discipline the PRD works hard to project.

**Fix:** Restate SM-3 as: a canonical shared implementation exists, Conversations consumes it, and
each surviving sibling copy is registered as follow-on migration debt. Count that.

### H2. A Promote-classified area has no realizing FR: publication/event composition (638 LOC) is scope-homeless

**Location:** Addendum §C row 8; §6.3 FR-10..FR-15; §6.4 FR-17; addendum §F backlog list.

**Evidence:** Addendum §C row 8: "Publication / event composition | 638 / 8 | **Promote (partial)**
| transport marshaling is generic; the failure taxonomy remains domain-specific" — the only Promote
row with no FR reference. FR-10..FR-15 cover ServiceDefaults, tenant-access, client registration,
Aspire/Dapr topology, serialization, and telemetry; none covers publication marshaling. §F's
follow-on backlog (items 1, 7–10) does not carry it either. Since plumbing = Consume + Promote,
these 638 LOC sit inside the SM-1 denominator with no authorized mechanism to remove or
externalize them: FR-17 enumerates only capabilities "added or extended under FR-10..FR-15."

**Why it matters downstream:** One implementer promotes marshaling into EventStore (unauthorized —
outside FR-17's enumeration and OQ-1's FR-10..15 landing-zone mandate); another leaves it in place
(contradicting its frozen classification). Incompatible builds from the same text, on an area that
touches the pub/sub boundary §14.7 assigns to the platform SDK.

**Fix:** Either bind area 8's generic half to an FR (new or existing) or reclassify it Keep/backlog
with an FR-2-logged rationale, and reconcile the SM-1 denominator accordingly.

### H3. The SM-1 denominator was shrunk ~4.7k LOC by a reclassification with no citable decision authority — the exact move the PRD forbids for tests but never forbids for LOC

**Location:** Addendum §A; §2 readiness table ("no … silent denominator reduction"); §6.1 FR-2; §12; `.memlog.md`.

**Evidence:** Addendum §A: "Under OQ-3, governance and hydration were classified as Keep now. **The
classification of the Contracts/Testing domain surface as Keep moved ≈4.7k LOC out of plumbing.**"
The first sentence has an authority (OQ-3); the second has none — OQ-3 (§12) covers only
"governance orchestration, temporal reconstruction, and upstream hydration," not Contracts/Testing.
The decision log records nothing about it; the PRD decision register is silent. The Discovery
estimate was ~18,000 plumbing LOC; the accepted baseline is 13,289 — the delta is almost exactly
this uncited move. Meanwhile §2 forbids "silent denominator reduction" — but only for the FR-20
test manifest; SM-1's LOC denominator enjoys no equivalent protection anywhere.

**Why it matters downstream:** Shrinking the plumbing denominator lowers the absolute-LOC bar that
≥40% represents. Whether or not this particular reclassification was correct (the accepted
inventory JSON may hold a rationale note — FR-2 permits "decision log **or** inventory note"), the
PRD's package makes it unverifiable, and nothing prevents the same move being repeated. The
anti-gaming architecture has a hole precisely where the headline metric lives.

**Fix:** Extend the no-silent-denominator-reduction rule to the SM-1 baseline (any inventory
reclassification that changes the frozen plumbing LOC requires the same named approval + versioned
evidence FR-20 demands for tests), and cite the Contracts/Testing decision — owner, rationale,
artifact — in addendum §A.

### H4. SM-2 sits at exactly 50.00% on self-declared low-confidence evidence, against a counterfactual baseline the PRD never defines

**Location:** §7 SM-2; §6.4 FR-19; §12 OQ-2.

**Evidence:** SM-2: "Target: ≥50% fewer hand-authored, module-owned files … within the frozen Story
4.1 measurement boundary, computed inclusively … Current figures (**50.00%** files / 67.95% LOC)
are **provisional** because they come from an accepted low-confidence estimate." An inclusive ≥50%
comparison met at exactly 50.00% means a single file of measurement noise decides attainment — and
the denominator is "the pre-initiative equivalent" of a minimal do-nothing module, a thing that
never existed and must be constructed. The "frozen Story 4.1 measurement boundary" is invoked
normatively but defined nowhere in the PRD or addendum; it lives in an external story artifact
(`docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json`) that the PRD does not
cite.

**Why it matters downstream:** FR-19's reproducible-fixture requirement is good, but it hardens the
*numerator*. The counterfactual denominator — how many files a minimal module "would have" required
pre-initiative — is exactly where construction choices can move the result across a boundary the
current evidence already kisses. Two honest implementers will build different pre-initiative
equivalents; the inclusive comparison rewards the one that lands at 50.00%.

**Fix:** Cite the Story 4.1 boundary artifact normatively in SM-2, define the counterfactual
construction rules (which pre-initiative module is the donor pattern, which files count), and
require the final FR-19 artifact to state sensitivity: which single-file decisions, if reversed,
would flip attainment.

### H5. FR-3 / FR-10 / FR-13 acceptance consequences overlap despite the crosswalk built to prevent exactly that

**Location:** §6.3 crosswalk table ("must not produce duplicate stories"); §6.2 FR-3 consequences; §6.3 FR-10, FR-13 consequences.

**Evidence:** The crosswalk assigns disjoint boundaries (FR-3 = host adoption; FR-10 =
ServiceDefaults; FR-13 = topology). But FR-3's first consequence already claims the other two
slices' evidence: "Conversations is discoverable and runnable through the platform host **without a
Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent runtime-host project**." FR-10:
"Conversations owns no ServiceDefaults project." FR-13: "No Conversations-local AppHost, Aspire,
ServiceDefaults, or equivalent runtime-host module remains." Three FRs each testably assert removal
of the same artifacts.

**Why it matters downstream:** Story generation will either produce the duplicate stories the PRD
forbids, or one story's evidence will silently satisfy another FR's consequence, leaving FR-10/
FR-13 acceptance hollow (their distinctive content — behavior preservation of health/telemetry and
topology — is the part most likely to be skipped when the removal claim is already checked off).

**Fix:** Strip the AppHost/Aspire/ServiceDefaults-removal clause from FR-3's consequences; FR-3
should evidence host adoption and operation continuity only, leaving artifact-removal assertions to
FR-10 and FR-13 where the crosswalk says they live.

---

## Medium

### M1. The Vision contradicts the accepted baseline the addendum declares authoritative

**Location:** §1 Vision vs addendum §A.

**Evidence:** §1: Conversations is "roughly 35,800 lines of source, and **about half of it is
plumbing**." Addendum §A: authoritative accepted baseline is **13,289 LOC (37.15%)**, accepted
2026-06-03 — six weeks before the PRD's `updated: 2026-07-14`. The finalized normative document
leads with a plumbing share ~35% higher than its own accepted evidence, and the "half" framing is
what stakeholders and follow-on business cases will quote.

**Fix:** Rewrite the Vision figure to the accepted 37.15%, keeping the ~50% Discovery estimate as
explicitly labeled provenance if desired.

### M2. CORE is defined over "all eight preserved acceptance journeys" — §14.3 lists nine

**Location:** §4 Glossary (CORE) vs §14.3 table.

**Evidence:** Glossary: "the minimum non-cuttable capability and Foundation Gate set required for
credible substrate behavior **across all eight preserved acceptance journeys**." §14.3 lists nine
actors/journeys: Maya, Atlas, Sarah, Diego, Marcus, Julian, Helen, Naomi, Daniel. CORE is
load-bearing — Feature-FR77, FR-79, and FR-93 gate on "CORE preconditions" — and the off-by-one
lets an implementer argue any one journey out of CORE's extension. Notably, `.memlog.md` records
this definition was added deliberately as a "clinical prose fix"; it shipped wrong anyway.

**Fix:** Say "nine," or name which journeys are the acceptance set if one row is intentionally
excluded.

### M3. The §6.3 FR→addendum crosswalk is wrong in both directions, and FR-15's promised duplication evidence does not exist

**Location:** §6.3 requirements-mapping table vs addendum §E and §F.

**Evidence:** The PRD maps "FR-11, FR-12, FR-14, FR-15 → Addendum §E" and "FR-10, FR-13, FR-16 →
Addendum §F." In fact §E contains rows for FR-10 and FR-13 (rows 1, 5); §F contains dispositions
for FR-14, FR-11, FR-12 (rows 2, 3, 5); and **FR-15 appears in §E nowhere** — its only grounding is
§D's ServiceDefaults row and §C area 4, neither of which shows cross-module duplication. R1's
mitigation says "promote only patterns with ≥2 real consumers **or a confirmed Conversations
need**" — FR-15 therefore rests entirely on a "confirmed need" that no artifact in the package
confirms.

**Fix:** Correct the mapping table, and either add FR-15's duplication/need evidence to §E/§F or
state explicitly that it is a single-consumer promotion justified under the second branch of R1.

### M4. §14.1's blanket disposition is vacuous for the process third of the preserved baseline

**Location:** §14.1 vs §14.6 (Feature-NFR1–8, NFR4, NFR30, NFR37, NFR69–75).

**Evidence:** §14.1: "Every Feature-FR and Feature-NFR below has the disposition **preserved** as a
behavioral or quality constraint on FR-20 and SM-C1." Feature-NFR4 ("Implementation for GA cannot
begin until unresolved capacity and latency targets are converted…"), NFR1–3, NFR5–8, NFR30, NFR37
are process/planning requirements; NFR69–75 are accessibility/usability audits that were plainly in
no green pre-refactor conformance run. None of these can appear in FR-20's frozen manifest or
constrain SM-C1 in any testable way — for them "preserved as a constraint on FR-20" is
unfalsifiable boilerplate. Worse, a literal reader can invert it: if every NFR constrains FR-20,
then a manifest without accessibility tests is incomplete.

**Fix:** Partition §14.5/§14.6 entries into behavior-preserving (FR-20/SM-C1-constraining) and
process/roadmap (preserved for traceability, not FR-20-relevant) classes.

### M5. The greenfield-latitude assumption's revisit trigger fires only after the irreversible action it licenses

**Location:** §9 Greenfield latitude; §13 Assumptions row for §9.

**Evidence:** §9 authorizes deleting plumbing-only tests because "[ASSUMPTION: Conversations not
yet in production for external tenants.]" §13's revisit: "Release owner verifies consumer
compatibility and production status **before any shared-package or external release**." The
deletions happen during Phases 1–3; the verification is scheduled at release time — after the tests
are gone. If the assumption is wrong, the guard rail arrives after the cliff.

**Fix:** Move the production-status verification to Phase 0 (a one-line check against tenant
deployments), keeping the release-time re-check as a second gate.

### M6. FR-20 protects only what got into the manifest, and manifest completeness has no criterion

**Location:** §6.5 FR-20; §4 Glossary ("Conformance suite", "Plumbing-only test"); §9.

**Evidence:** The denominator is "the exact set of passing **release-gate conformance tests**,"
where the conformance suite is glossary-defined with an open list: "tenant isolation, idempotency,
contract validation, redaction, provider portability, **etc.**" Any behavioral test not manifested
at freeze can thereafter be deleted as "plumbing-only" with zero FR-20 process — the gate's entire
strength is decided at freeze time by an inclusion rule the PRD leaves at "etc." (The repo shows
`at-risk-test-register-v1` and `removed-test-justification-ledger-reconciliation-v1` exist — the
implementation was more careful than the PRD requires, which is the wrong direction for a contract.)

**Fix:** Give FR-20 a manifest-completeness consequence: an enumerated closed category list, plus a
requirement that every deleted non-manifested test carry a plumbing-only justification in a
versioned ledger.

### M7. "Already-demonstrated generic SDK seam" — the only soft edge on a 9.5k-LOC Keep boundary — is undefined

**Location:** §6.3 Notes; §12 OQ-3; §5.2.

**Evidence:** "The pilot may consume an **already-demonstrated** generic SDK seam without moving
the domain behavior." Demonstrated where, by what evidence, accepted by whom? Governance (4,337
LOC), temporal reconstruction, and hydration (828 LOC) are otherwise hard-Kept; this clause is the
single discretionary opening, and it is left entirely to implementer judgment.

**Fix:** Define "already-demonstrated" (e.g., the seam ships in a released technical-module version
and is exercised by at least one existing consumer or SDK conformance test) or require an OQ-1-style
architect sign-off per use.

---

## Low

### L1. SM-1 "Validates FR-3..FR-17" sweeps in deferred FR-16

**Location:** §7 SM-1 vs §2, §6.3 FR-16, §12 OQ-4. The range notation includes FR-16, which is
deferred and "excluded from pilot acceptance." Should read "FR-3..FR-15, FR-17." Traceability
tooling that expands the range will emit a contradiction.

### L2. Vision overstates the duplication evidence

**Location:** §1 vs addendum §E row 2. §1: "the same 80-line tenant-access handler" copied across
"(Folders, Projects, Memories, Parties)"; §E locates it in Folders and Projects only. Rhetorical
inflation in the paragraph that justifies the whole initiative.

### L3. Addendum §E pre-empts OQ-1 with a nonexistent landing zone

**Location:** Addendum §E row 8 ("shared test fixtures (Commons.Testing)") vs §4 Glossary technical
modules and §12 OQ-1. "Commons.Testing" is not among the listed technical modules; naming it as the
recommendation quietly answers the landing-zone question OQ-1 reserves for the architect. Label it
as a candidate, not a target.

### L4. The only inventory in the package violates FR-1/FR-2's own consequences

**Location:** Addendum §C vs §6.1 FR-1 ("exactly one classification"), FR-2 ("No area is …
dual-classified"). §C rows carry "Consume + Promote", "Promote / Consume", "Promote (partial)",
"Mixed", and the listed areas sum to ≈22.6k of the claimed 35,769 LOC. The compliant artifact
exists (`docs/release-evidence/consume-promote-keep-inventory-v1.json`, committed 2026-06-03) but
outside the package; a reader of the PRD+addendum alone sees an inventory that fails the PRD's own
acceptance tests. Mark §C explicitly as superseded provenance and cite the accepted artifact as the
sole FR-1 object. (Related: Feature-FR102 hard-codes "the Option A v1 deal" while §14.4 declares no
option selected — same class of stale-provenance leakage into normative text.)

---

## Counts

- Critical: 2 (C1, C2)
- High: 5 (H1–H5)
- Medium: 7 (M1–M7)
- Low: 4 (L1–L4)
