# Adversarial Review — architecture.md (v8 base + V9–V12 overlays)

- **Review date:** 2026-08-18
- **Target:** `_bmad-output/planning-artifacts/architecture.md` (2,359 lines, read in full)
- **Effective authority at review time (per last complete overlay marker):** `conversations-architecture-2026-08-04-v12` / `epic-6-authority-2026-08-04-v12`, hold `ACTIVE`
- **Lens (configured):** adversarial pairwise-construction. For each attack, two units one level down (two stories, two builders, two components, or builder + validator) each obey every still-binding rule to the letter and still build incompatibly.
- **Severity rule:** critical = the incompatibility is reachable by work that is currently authorized (planning/validator/E6-REMEDIATION lanes) or by Wave-1 work the instant the hold lifts (Epic 7 and Epic 12 per the v9 graph, lines 2013–2014).

**Verdict: FAIL — 4 critical, 6 high, 5 medium, 3 low successful attacks.** The supersession machinery (byte-pinned immutable overlays, digest-bound bundles, single-writer ownership) is unusually strong at preventing *rewrites*, but that very strength converts every unresolved shape, every "candidate"/"such as" vocabulary, and every scope-limited supersession clause into a place where two letter-compliant builders diverge with no mechanical tiebreaker. Four of those divergences sit directly on currently-authorized validator work or on Epic 7/Epic 12, which start the moment the hold lifts.

---

## Ranked attack register

| # | Severity | Attack |
| --- | --- | --- |
| A1 | CRITICAL | SM-C2 "usable comparable evidence" vs frozen envelope: Epic 12 is either unsatisfiable or satisfiable two incompatible ways |
| A2 | CRITICAL | Trust/freshness/hydration vocabulary: "one shared enum" ordered, never pinned; the two candidate lists don't even share members |
| A3 | CRITICAL | Epic 7 record-generator declared-dirt policy vs Story 6.7 promotion-gate untracked-cleanliness on the same candidate |
| A4 | CRITICAL | Graph-parity composition across immutable overlays + the underdetermined "exactly 33 nodes" set |
| A5 | HIGH | Temporal evidence anchor: open question (line 604) that HP-OPEN and proof-v3 builders must both answer today |
| A6 | HIGH | Audit-unavailable non-governance boundary + the orphaned ADR-001..010 backlog |
| A7 | HIGH | Read-model derived-key schema unpinned beneath the cross-key validation and the poison BDD scenario |
| A8 | HIGH | Portable-tier migration "strength digest" undefined; tier membership has two deciders |
| A9 | HIGH | AppHost baseline prohibition vs test-harness retention: acknowledged conflict, no owning successor, Epic 12 depends on the harness |
| A10 | HIGH | "Story 6.11 owns retiring the ceiling" (v6) vs v8's demotion of the ceiling: obligation-inventory disposition is two-valued |
| A11 | MEDIUM | Projection-handler duality (legacy sync vs scoped async) outside the query store + dispatch-ledger ownership |
| A12 | MEDIUM | Party hydration degraded-state mapping (`Redacted` vs `Unknown` vs `Unavailable`) with an empty hydration vocabulary |
| A13 | MEDIUM | UX acceptance denominator: "every explicit identifier" scan vs pinned constant 28 |
| A14 | MEDIUM | ADR identity collision: ADR-003/ADR-004 doubly assigned across backlog and docs/adrs |
| A15 | MEDIUM | Authority discovery: frozen frontmatter vs overlay marker while legacy mechanical bodies remain active routes |
| A16 | LOW | 27-reader inventory frozen vs sanctioned alias conversion of legacy bodies |
| A17 | LOW | Legal-hold source-event mutation vs absolute replay-equivalence proof |
| A18 | LOW | Metadata-only rule: allowed tenant-scoped IDs vs membership-reconstruction prohibition |

---

## A1 — CRITICAL — SM-C2: "usable comparable evidence" vs the frozen envelope

**Units.** (1) The Epic 12 implementer (Stories 12.1–12.4, "Universal performance restoration", entry "Story 6.2 done" — line 2002; successor of Story 6.11 per line 1988). (2) The SM-C2 gate validator / Epic 15 attestation assembler (Stories 15.1–15.2, RG-15, lines 2005, 2103–2107; release gate line 996).

**What each may legitimately build.** V8 restored PRD SM-C2 as the sole metric authority: all four rows "require usable comparable evidence under the identical frozen envelope and `post P95 <= 1.05 x baseline P95`", and "unusable signal cannot substitute for that gate" (lines 268–275, reaffirmed 1946–1950). But the immutable v6 record establishes as fact that under that envelope HP-CREATE and HP-APPEND showed "measured within-run dispersion two orders of magnitude wider than the threshold" (lines 165–168). The envelope freezes "workload and data, concurrency, environment and runtime, benchmark tool/version, warm/cold classification, repetition policy, and raw-result processing" (lines 383–387).

- Builder 1 reads v8's "Story 6.11 owns correctness-preserving signal and performance work for all four rows" (lines 273–274, inherited by Epic 12) as license to change repetition policy, tooling, or sampling to *make* the signal usable — signal work is the story's explicit mandate.
- Builder 2 reads "identical frozen envelope" (line 1948) plus "Changing the target requires a separate approved PRD-level proposal" (line 275) plus per-row envelope identity (lines 383–387) as prohibiting any change to repetition policy or tooling; evidence produced under a modified envelope is non-comparable and the lane is `BLOCKED` (story-contract item 6, lines 2057–2058: "Environmental inability is BLOCKED, never PASS").

**Incompatibility.** Either Epic 12 ships evidence that Epic 15/RG-15 must reject as envelope-drifted, or Epic 12 is unsatisfiable as written (the frozen envelope provably cannot yield usable signal on two of four mandatory rows) and the release chain dead-ends at a gate no authorized work may unblock. Both readings are letter-faithful; the document defines "usable" nowhere and never says whether "signal work" may touch envelope members.

**Closing rule.** Amend the SM-C2 section to (a) define "usable comparable evidence" numerically (e.g., a maximum confidence-interval half-width relative to the 5% threshold), and (b) partition the envelope into frozen members (workload/data, concurrency, environment, warm classification) and versioned members (repetition count, raw-sample volume, tool version) that may change only via a named envelope revision under which *both* baseline and post are re-measured as a pair. State explicitly that an envelope revision is not a target change under line 275.

---

## A2 — CRITICAL — Trust/freshness/hydration vocabulary: ordered "one shared enum", never pinned

**Units.** (1) The Contracts author of `Contracts/TrustStates` (target structure line 1463; Epic 11 thin-module authoring proof, line 2001, or any Wave-1 story that serializes an HP envelope). (2) The Server projection/HP-OPEN envelope builder in Epic 12 plus the conformance validator that must assert "freshness evidence using the shared vocabulary" (line 1212, conformance line 977).

**What each may legitimately build.** Line 1129 commands: "Trust/freshness states must use one shared enum or value contract across API, UI, diagnostics, and evidence." Yet:

- The trust-state list is only ever "candidate": `Unknown, Pending, Verified, Contradicted, Stale, Redacted, Unavailable, Forbidden` (lines 554, 918).
- The freshness list is only ever "such as"/"minimum shape": `Current, Stale, Rebuilding, Unavailable, Forbidden, Redacted` (lines 1211, 1277–1282).
- The Shared Vocabulary Rule (lines 1257–1267) lists **projection freshness state**, **trust state**, **redaction state**, and **hydration state** as *separate* categories — and the hydration category has no candidate list anywhere in 2,359 lines.
- `Current` exists only in the freshness list; `Verified/Pending/Contradicted/Unknown` exist only in the trust list; `Redacted` appears in three categories.

Builder 1 may therefore build **one** combined 9-member `TrustState` enum (union, per line 1129 "one shared enum"). Builder 2 may build **two** enums, `FreshnessState` (6 members) and `TrustState` (8 members), per the Shared Vocabulary Rule's separate categories. Both are letter-compliant; line 1269 only forbids *synonyms*, not structural choices.

**Incompatibility.** HP-OPEN's post disposition requires "Same response/trust envelope" comparability (line 381) — the envelope shape must exist before Epic 12 measures. A one-enum Contracts assembly and a two-enum Server projection cannot share the wire shape; the conformance assertion "must not be inferred from HTTP status" (line 1212) binds against whichever shape its author picked; the Blocking Freshness Rule's default "only `Current` is acceptable" (line 1284) is unrepresentable in a builder whose trust enum has no `Current` member.

**Closing rule.** Publish the canonical vocabulary as a versioned contract table in the architecture (or a digest-bound sidecar) before hold-lift: one enum per category, exact members, and an explicit statement of whether trust and freshness are one type or two with a defined mapping. Downgrade lines 554/918/1211 from "candidate"/"such as" to normative or mark them superseded by the table.

---

## A3 — CRITICAL — Epic 7 generator dirt policy vs Story 6.7 promotion-gate cleanliness

**Units.** (1) The Epic 7 record-generator builder (Stories 7.1–7.4, "Reliable mechanical completion records", line 1997; story-contract item 11, lines 2067–2069). (2) The promotion checker implementing the Story 6.7 invariant, whose document the record embeds verbatim (lines 84–88, 423–425).

**What each may legitimately build.** The v6 record contract admits working-tree dirt in exactly two shapes: "source-tree dirt blocked **outside record outputs and declared TRX inputs**" (lines 179–182), and "After the bound candidate only record-output paths may change and no gitlink may move" (lines 184–185). The promotion gate requires every affected root-declared submodule "initialized, clean **including untracked files**" (lines 423–425).

- Builder 1's generator legitimately accepts a candidate whose declared TRX inputs (or their unavoidable build byproducts) were produced by test runs that write under a submodule worktree — nothing in v6 constrains *where* declared TRX inputs live, and umbrella test lanes build platform projects inside `references/` by project reference.
- Builder 2's gate legitimately blocks that same candidate: an untracked file inside a changed-gitlink submodule is a hard block, with no carve-out for declared TRX inputs (line 424).

**Incompatibility.** The same candidate is simultaneously generator-valid and gate-blocked, and because the record must embed the promotion-checker document verbatim (line 87) and "a binding that no longer describes the final candidate goes red rather than stale" (line 90), the generator's own output turns itself red. No precedence between the two dirt policies is stated anywhere. (This is not hypothetical: it is the operational failure mode already observed in this repo's promotion-gate history.)

**Closing rule.** Pin the allowed dirt locations by path: record outputs and declared TRX inputs must resolve under root-owned directories only, never under `references/`; state that promotion-gate cleanliness dominates the record contract; require test lanes that touch submodule worktrees to redirect outputs (or park byproducts) before record generation begins.

---

## A4 — CRITICAL — Graph-parity composition across immutable overlays + the "exactly 33 nodes" set

**Units.** (1) The graph-projection generator (workflow owner, lines 1911–1913). (2) The bundle/graph validator (Quality owner, lines 1914–1915) as run by the currently-authorized V12 preflight, which "builds the Conformance project and directly runs V9, V8, and architecture validation classes" (lines 2344–2346). This attack is reachable **today**: E6-REMEDIATION work is authorized and in progress.

**What each may legitimately build.** V9 requires the validator to "reject any semantic or digest difference among the canonical epic block, **architecture overlay**, graph, map, generated view, UX map, sprint projection, and story contracts" (lines 1900–1903). But the v9 architecture overlay's own graph (mermaid, lines 2007–2035) is byte-immutable (pinned by v10, lines 2144–2148) and contains no `7.1-SCHEMAS`, no `E6-REMEDIATION`, no `PC-PUBLICATION`, and a direct IR-0→Epic edge structure that v12 explicitly removed ("The prior direct PC-PUBLICATION -> IR-0 edge is absent", lines 2278–2279). V11 legitimized exactly one graph-only reconciliation ("The graph validator reconciles this one supplemental edge from the closed sidecar; **arbitrary graph-only predecessors are invalid**", lines 2223–2225) — and v12's `E6-REMEDIATION` is precisely a graph-only node owned by no canonical epic-block story ("no story contract, sprint key, or final-record contract is created for the remediation node", lines 2280–2281).

- Validator-builder 1 compares the current graph against each immutable overlay's own semantics → permanent semantic difference against v9's mermaid → always-FAIL (a poisoned red).
- Validator-builder 2 compares against an overlay-composed effective graph (v9 base + v11 edge + v12 node) → PASS. No composition rule is stated anywhere; "reconciles this one supplemental edge" names only the v11 sidecar.

Separately, "The graph has exactly 33 nodes" (line 2282) underdetermines membership. 27 successor stories + IR-0 + RG-15 + 7.1-SCHEMAS + E6-REMEDIATION = 31; the remaining 2 can be {PC-PUBLICATION, 6.2} (forced only if one infers PC-PUBLICATION from the v12 edge diagram and 6.2 from 7.1-SCHEMAS's predecessors) — but a generator that also materializes completed-foundation nodes 6.1 and 6.7 (they are first-class rows of the supersession table, line 1982, and hard-entry referents, lines 1997/2001) reaches 35, or reaches 33 by a *different* selection. Two letter-compliant generators emit different node sets; whichever the validator's author assumed, the other fails.

**Closing rule.** (a) State the composition rule: graph parity is validated against the effective overlay-composed graph, defined as v9's canonical graph amended by each later overlay's explicitly enumerated node/edge deltas; immutable overlay blocks are provenance, not comparison targets. (b) Enumerate the exact 33 node IDs (and the exclusion of 6.1/6.7/history nodes) in the v12 sidecar rather than pinning only a count.

---

## A5 — HIGH — Temporal evidence anchor: open, and load-bearing for two Wave-adjacent builders

**Units.** (1) The Epic 12 HP-OPEN builder, who must serialize "freshness, redaction filtering, evidence metadata" into a comparable response envelope (line 381) and include freshness metadata on every trust-bearing read (lines 1161, 1212). (2) The Epic 13 proof-v3 builder, who must carry "fresh machine-readable deterministic, gateway/DAPR, state-store, query, deletion, and replay evidence" (lines 216–218) and whose replay-equivalence comparison needs an anchor.

**The hole.** Line 604 leaves open: "What is the authoritative temporal evidence anchor: event position, projection version, timestamp, or a composite?" Meanwhile the public surface **must not** expose "event sequence numbers" or "storage offsets" (lines 1116, 1322) yet **may** expose "status, freshness, and evidence references" (line 1117). Builder 1 legitimately picks an opaque projection-version string (satisfies 1117, avoids 1322). Builder 2 legitimately anchors replay evidence on event position (the only anchor that is deterministic under rebuild, and proof artifacts are not public APIs). Evidence bundles must also state "which redaction policy and event versions produced them" (lines 820–821) — a third partial anchor.

**Incompatibility.** HP-OPEN baseline/post envelopes carry different anchor fields → non-comparable under line 381; proof-v3's replay-equivalence and the runtime's freshness metadata cannot be cross-checked; a conformance test asserting "no sequence numbers in responses" (line 1322) fails the builder who chose position, while a determinism audit fails the builder who chose timestamp.

**Closing rule.** Resolve the open question as a pinned composite contract: a Conversations-owned opaque anchor (projection version + redaction-policy version), with event position permitted only inside non-public evidence artifacts, and add the anchor's field names to the vocabulary table from A2.

---

## A6 — HIGH — Audit-unavailable command boundary + the orphaned ADR backlog

**Units.** (1) The Epic 12 command-path builder measuring HP-CREATE/HP-APPEND (non-governance commands) in a harness where the audit sink may be absent. (2) The failure-injection/conformance builder implementing "partial evidence availability" injection (line 1338) and "audit sink unavailable blocks governance mutation" (line 976).

**The hole.** Line 605 leaves the boundary open. The binding prose is two-valued: "Non-governance commands may continue during audit degradation only by explicit ADR" (line 868) and "Non-governance behavior during audit degradation requires an ADR before implementation" (lines 1200–1201). Builder 1 reads: fail-closed attaches to governance mutations only; append has no audit dependency, so no "audit degradation behavior" is being implemented and no ADR is needed — benchmarks run. Builder 2 reads: absent the ADR, the default is closed for *everything* (by analogy with line 1284's "if not declared, only Current is acceptable" posture) — the chaos suite asserts all commands block, and Builder 1's harness is a fail-open violation.

Compounding: ADR-001..010 are "required before dependent implementation stories proceed" (lines 754–767) and "Treat ADR triggers and stop conditions as blocking" (line 1791) — but no Epic 7–15 story owns authoring any of them, and v9's 124/124 coverage (lines 2071–2073) counts FRs, not ADRs. Two builders can disagree about whether any successor story is "dependent."

**Closing rule.** Pin the default in the architecture: non-governance commands proceed without audit-sink dependency; only governance mutations fail closed; the ADR requirement gates *adding* a continue-under-degradation behavior to a path that otherwise has an audit dependency. Map ADR authorship (or an explicit "not required for Epics 7–15" disposition) into the obligation inventory.

---

## A7 — HIGH — Derived-key schema unpinned beneath cross-key validation and the poison scenario

**Units.** (1) The projection materializer builder (summary/detail + tenant-index writer, lines 411–414). (2) The fail-closed cross-key reader / Epic 12 HP-LIST optimizer, whose read path is "a detail read plus a tenant-index read plus a dispatch-ledger read, with a bounded per-row detail and ledger read added to page verification" (lines 160–164), and the Epic 13 proof-v3 builder recording state-store/query/deletion evidence against concrete keys.

**The hole.** The only shape rule is "Durable stream keys, projection keys, cache keys, evidence keys, and worker job keys must carry tenant scope" (line 1105). Conversations owns "tenant-scoped read-model keys" (line 405). V8 binds a BDD scenario for "cross-tenant derived-key poison" (line 304) — the *failure* is contracted, but the key-derivation function it attacks is never pinned. Builder 1 derives `{tenantId}:{conversationId}`; Builder 2's page verification recomputes `{tenantId}/summary/{conversationId}`; both "carry tenant scope."

**Incompatibility.** Cross-key validation silently validates nothing (reads miss, fail closed → HP-LIST goes red for a non-defect), or worse, the poison test passes vacuously against keys production never writes. Proof-v3 evidence keyed one way cannot re-run against a store keyed the other.

**Closing rule.** Pin the exact derived-key grammar (summary/detail key, tenant-index key, dispatch-ledger key: field order, separator, encoding) as a versioned contract in the ADR 0003 lineage or Contracts, and make the poison BDD scenario cite that grammar's version.

---

## A8 — HIGH — Portable-tier migration "strength digest" undefined; tier membership has two deciders

**Units.** (1) The Epic 9 migration builder ("Portable conformance oracle", Stories 9.1–9.2, line 1999) moving assertions toward the portable tier. (2) The validator enforcing v5's tier law: "Weakening or deleting an assertion so it can move to the portable tier is a conformance failure. A check that cannot be re-expressed at full strength belongs in the module-internal tier" (lines 133–136), plus widening the public contract for reachability is prohibited (lines 130–132).

**The hole.** Story-contract item 9 requires "Every migrated assertion binds before/after inventories and **strength digests**; silent weakening or deletion fails" (lines 2064–2066) — but strength-digest semantics are defined nowhere. Builder 1 digests assertion count + asserted-outcome text (a re-expression against Testing-tier fakes preserves both). Builder 2 digests the bound compile surface (the same re-expression drops `Hexalith.Conversations.Server` and is by definition weaker). Each is letter-compliant; "full strength" has no mechanical definition, and nothing names a single owner of the per-assertion tier disposition.

**Incompatibility.** Epic 9 ships a portable suite the validator scores as mass-weakening (Epic 9's bounded exit unreachable), or the validator accepts fake-backed re-expressions that v5's author would call weakened — and Epic 14's preservation manifest inherits whichever answer shipped.

**Closing rule.** Define the strength digest as (bound-assembly set, asserted behavior identity, negative-case count) and declare the Quality owner as the single decider of tier membership, recorded in a digest-bound per-assertion inventory.

---

## A9 — HIGH — AppHost: baseline prohibition vs harness retention, acknowledged but unowned, with Epic 12 depending on it

**Units.** (1) The Epic 12 measurement builder, whose post dispositions for HP-CREATE/HP-OPEN are defined as "through platform-owned runtime surfaces **exercised by the module test harness**" (lines 378, 381) — the harness is load-bearing on day 1 of hold-lift. (2) A thin-module/conformance builder (Epic 11, line 2001) applying the platform baseline, which flatly states a domain module "must not ship its own `*.AppHost` … project" (`references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, Domain-Module Authoring).

**The hole is documented and then abandoned.** V9 itself says: "If that prohibition is interpreted to forbid even this repository-local test fixture, the conflict requires a separate approved technical amendment. V9 neither expands the fixture nor silently overrides the inherited v8 decision or the baseline" (lines 1960–1965). Retrospective actions A4–A6 (including the Test/AppHost owner) "remain open … and require separately approved successor authority" (lines 2355–2357). No Epic 7–15 story owns producing that amendment, and IR-0 has no criterion requiring the conflict to be resolved.

**Incompatibility.** Builder 2's thin-module proof or a baseline-conformance check legitimately concludes the AppHost must not exist; Builder 1's entire post-measurement contract legitimately requires it to exist and run. Hold-lift authorizes both simultaneously.

**Closing rule.** Make resolution of the AppHost interpretation (the "separate approved technical amendment" v9 demands) an explicit IR-0 entry criterion or an Epic 12 hard-entry condition; until then record the pinned interpretation ("repository-local non-packable test fixture is outside the baseline's 'ship' prohibition") in the overlay chain, not in an unowned open action.

---

## A10 — HIGH — "Retiring the ceiling": a v6 obligation whose v9 disposition is two-valued

**Units.** (1) The Epic 12 story author mapping Story 6.11 obligations (supersession row, line 1988). (2) The obligation-inventory validator, which must map "every v8 acceptance criterion, checkpoint, prohibition, dependency, evidence obligation … exactly once to a successor atomic acceptance ID or to an explicit immutable-history/non-executable disposition" with "missing, duplicate, orphaned, or many-to-none obligation mappings fail publication" (lines 1973–1978).

**The hole.** V6 says "Story 6.11 owns retiring the ceiling" (line 169). V8 then demoted the ceiling itself: "Story 6.2's v6 ceiling/disclosure disposition is historical completion context only; it is neither reopened nor accepted as the current release rule" (lines 263–264), and "An approved-cost ceiling … cannot substitute for that gate" (lines 271–273). Is "retire the ceiling" (a) a live evidence obligation Epic 12 must discharge with a retirement artifact, or (b) already non-executable because v8 dissolved the thing to be retired? Builder 1 produces a ceiling-retirement record — which a strict reader rejects as *referencing* a ceiling v8 says cannot appear in current release reasoning. Builder 2 dispositions it immutable-history — which a strict inventory reader flags as an unmapped v6→v8 obligation (v8 republished 6.11's contract; the v6 sentence was never explicitly superseded, only its subject was).

**Incompatibility.** The zero-gap inventory cannot be simultaneously complete under both readings; publication fails, or a phantom deliverable enters Epic 12.

**Closing rule.** One sentence in the next overlay: "The v6 ceiling and its retirement obligation are non-executable history; no successor story owes a retirement artifact."

---

## A11 — MEDIUM — Projection-handler duality outside the query store + dispatch-ledger ownership

**Units.** (1) The builder of a non-query-store projection — the local tenant-access projection (`TenantAccess` consumes Tenants events, line 1550) or the status projections required for async workflows (lines 1021, 1214). (2) A conformance/oracle builder asserting production population ownership.

**The hole.** ADR 0003 pins the scoped named `IAsyncDomainProjectionHandler` as production owner **for the persisted query store** only; the legacy synchronous `IDomainProjectionHandler` "remains version-1 compatibility only" (lines 405–409). Builder 1 legitimately implements the tenant-access projection on the legacy interface (it *is* v1-era compatibility surface, and it is not the query store). Builder 2 legitimately asserts no new legacy-handler registrations exist (reading "version-1 compatibility only" as frozen-to-existing). Additionally, the read path performs a "dispatch-ledger read" (lines 161–164) while EventStore owns "stable dispatch identity" (line 404) — whether the ledger is an EventStore-owned surface Conversations reads, or a Conversations-owned record, is unstated, so its record shape and key are inventable twice (compounds A7).

**Closing rule.** Enumerate each projection class (query-store, tenant-access, status/operational) with its required handler interface, and name the dispatch ledger's owner, shape, and read contract.

---

## A12 — MEDIUM — Party hydration degraded states: three candidate states, no assignment, empty vocabulary

**Units.** (1) The hydrator builder (`IParticipantDirectory`, lines 1125, 1551). (2) The redaction non-disclosure conformance builder (line 980–981).

**The hole.** Deleted/inaccessible Party must "render stable fallback or redacted/unknown state" (line 952) and "hydrates as redacted/unknown" (line 980); adapter degradation yields "a policy-defined non-personal hydration placeholder while preserving explicit degraded state" (line 393). Which of `Redacted`, `Unknown`, `Unavailable` maps to deleted vs inaccessible vs adapter-down is a builder's choice — the hydration-state vocabulary category (line 1265) has no member list anywhere. Builder 1 distinguishes deleted (`Unknown`) from adapter-down (`Unavailable`); Builder 2's disclosure suite treats the very distinguishability of "deleted" as a Party-lifecycle leak and asserts a single indistinct state. HP-OPEN's envelope includes batched hydration (line 381), so the choice also lands inside Epic 12's comparability window.

**Closing rule.** Enumerate hydration states, pin the condition→state mapping, and state explicitly which conditions must be mutually indistinguishable at each disclosure surface.

---

## A13 — MEDIUM — UX acceptance denominator: scan vs constant

**Units.** (1) The Epic 8 disposition-schema/zero-gap-validator builder (Stories 8.1–8.2, line 1998), whose v8 mandate is to inventory "UX-DR1-52 plus **every explicit UX acceptance-criterion identifier**" (lines 298–299). (2) The v9/v10 parity validator holding "exactly 52 UX decisions plus **28 UX acceptance IDs** with zero missing, orphaned, or duplicate bindings" (lines 2071–2074, 2186–2189).

**The hole.** One unit derives the denominator by scanning the UX specification ("every explicit identifier" is a rule, not a number); the other hard-codes 28. If the spec's explicit identifiers number anything but 28 — or if "explicit" is judged differently (labeled ACs vs numbered ACs vs criteria embedded in decision prose) — each unit's validator fails the other's artifact, and "Validator failure cannot be waived by prose" (line 2074) makes the deadlock unresolvable in-band.

**Closing rule.** Bind the 28 IDs as an enumerated, digest-bound list (not a count) together with the extraction rule that produced it; make the Epic 8 validator consume that list rather than re-derive it.

---

## A14 — MEDIUM — ADR identity collision: ADR-003 / ADR-004 doubly assigned

**Units.** (1) The Epic 13 builder, whose lineage authority is "ADR 0004 for an immutable predecessor-linked projection-proof lifecycle" (line 212). (2) Any future builder instructed by the still-binding backlog to "create or update" "ADR-004: Governance audit pairing enforcement and audit-unavailable command behavior" (line 761) — and similarly ADR-003 is both "Tenant access projection durability…" (line 760) and the accepted `docs/adrs/0003-projection-read-store-population-proof.md` (lines 30, 397–399).

**Incompatibility.** Two documents claim one identity in each slot; the "ADR coverage map" evidence artifact (line 1005) and any validator binding ADR IDs cannot resolve which ADR-003/ADR-004 a citation means. The May-14 backlog is never superseded (line 59 keeps historical analysis binding for "unaffected" decisions).

**Closing rule.** Declare the May-14 backlog labels historical aliases, renumber the unauthored backlog entries into the real `docs/adrs` sequence, and state that the `docs/adrs` filename sequence is the sole ADR namespace.

---

## A15 — MEDIUM — Authority discovery: frozen frontmatter vs overlay marker, with legacy bodies still active

**Units.** (1) A legacy directly-callable mechanical body — v10 keeps "bmad-dev-story and bmad-code-review legacy bodies … in the mechanical inventory until they become forwarding aliases or are removed" (lines 2162–2165) — that reads the document frontmatter: `status: authority-correction-only-not-ready`, `authorityVersion: v8`, `currentExecutionView: …-v1.md` (lines 5–7, 31). (2) A marker-following v12 reader: "Machine readers determine current authority from the last complete architecture overlay marker, never from the historical frontmatter alone" (lines 1840–1842), with cached epic context valid only on exact `overlay_version`/`architecture_version` match (lines 2336–2340).

**Incompatibility.** Both units obey their own binding text — the frontmatter is *deliberately* frozen provenance (lines 1838–1840), so it can never be corrected to point at v12 — yet unit 1 resolves authority v8 and the v1 execution view while unit 2 resolves v12. A legacy body performing a lifecycle write against v8's story graph (6.x stories) writes states the v9 supersession map says no longer exist.

**Closing rule.** Add a route-inventory conformance check: every active mechanical body must demonstrate marker-based discovery (or be blocked from lifecycle writes until converted to a forwarding alias).

---

## A16 — LOW — 27-reader inventory frozen vs sanctioned alias conversion

V10 simultaneously freezes "the exact 27-reader inventory" as an unchanged v9 obligation (lines 2184–2186) and sanctions converting legacy bodies into forwarding aliases (lines 2164–2165), while v12 rules "Forwarding aliases are not counted as second mechanical bodies" (lines 2332–2333). A maintainer performing the sanctioned conversion and an inventory validator counting readers can disagree on whether an alias remains a reader; the frozen 27 breaks or holds depending on an unstated counting rule. **Close:** state whether aliases count as readers, and version the reader inventory instead of freezing its cardinality.

## A17 — LOW — Legal-hold source-event mutation vs absolute replay-equivalence

The denominators permit "approved compensating events, tombstones, or source-event treatment" (line 327) and an approved legal ADR may authorize "irreversible source-event redaction" (lines 812, 818) — while the projection proof demands "full replay equivalence" (line 419) and Epic 13 carries replay evidence (lines 216–218) with no carve-out for legally mutated streams. A proof builder treating any replay divergence as FAIL and a governance builder executing an approved source-event redaction are both compliant and mutually red. Deferred (no legal ADR exists), but the proof-lifecycle ADR 0004 should define predecessor-linked treatment of legally mutated streams now, while the contract is being authored.

## A18 — LOW — Metadata-only: allowed IDs vs membership reconstruction

Allowed examples bless "tenant-scoped IDs when authorized for the receiving surface" (line 1248); forbidden examples ban "enough identifiers to reconstruct conversation membership" (lines 1254–1255). A telemetry builder emitting `conversationId` and `partyId` on separate spans of one trace obeys the first bullet; a disclosure auditor correlating them invokes the second. **Close:** pin a per-surface identifier whitelist and forbid co-emission of participant and conversation identifiers on correlatable telemetry unless the surface is authorized for membership.

---

## Verdict

**FAIL for hold-lift readiness of the architecture document as the single build authority.** The append-only overlay discipline successfully protects history, but four attacks (A1–A4) show letter-compliant, currently-authorized or Wave-1 work pairs that build incompatibly with no mechanical tiebreaker: the SM-C2 envelope/usability contradiction, the unpinned trust/freshness vocabulary beneath the HP-OPEN envelope, the generator-vs-promotion-gate dirt conflict inside Epic 7's own deliverable, and the overlay-graph parity composition ambiguity inside the validator lane running today. Each has a small, append-only closing rule identified above; none requires reopening immutable history. The high-severity set (A5–A10) should be closed before IR-0 is rerun, since IR-0 binds the exact authority bundle these ambiguities live in.
