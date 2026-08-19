# Final Verification Review — Revised V13 Overlay (Post-Gate-Recheck)

- **Target:** `_bmad-output/planning-artifacts/architecture.md`, revised block
  `ARCHITECTURE-EXECUTION-OVERLAY-V13` (worktree lines 2360–2619; the block was
  revised after the gate recheck and is currently **worktree-only** — see NV-1).
- **Prior findings verified against:** `review-rubric.md` (F1–F11) and
  `review-adversarial.md` (DC verdicts, R1–R11) in this directory, plus
  `review-reality-checked.md` observations O-1/O-2.
- **Lens:** final verification — per-item resolution check with exact resolving
  text, exact-match ratification check against shipped code, marker/digest
  recomputation, and a hunt for new problems the corrections introduced.
- **Date:** 2026-08-18 (completed 2026-08-19). Read-only inspection; no
  submodules initialized or updated.

## Verdict

**PASS on content — all eleven prior findings are RESOLVED in the revised
worktree block, both ratifications exact-match the shipped code, and every
mechanical claim recomputes — with one MEDIUM operational defect: the copy of
`architecture.md` currently sitting in the git index is the pre-revision,
gate-FAILED V13 block. The revised block must be staged before the publication
commit, or the commit V13 itself mandates will publish the defective version.**

---

## 1. Per-item resolution table

| # | Prior finding | Verdict | Resolving V13 text (exact) |
| --- | --- | --- | --- |
| 1 | Rubric F1 / adversarial discovery-rule FAIL (R2): markers must carry sidecar-head + sha, attribute set normative, same-commit atomicity, fork sanctioned as transition, validator tasking | **RESOLVED** | BEGIN and END markers now carry `sidecar-head=v14-current-candidate-authority-v1.json sidecar-head-sha256=e96c34df…83da7f` (verified on both marker lines). Grammar: "From V13 forward every `BEGIN` marker carries the full attribute set (`version`, `epic-authority`, `supersedes`, predecessor block bytes and SHA-256, `sidecar-head`, `sidecar-head-sha256`, `candidate-binding`, `hold`), and every `END` marker repeats `version`, `epic-authority`, `sidecar-head`, `sidecar-head-sha256`, `candidate-binding`, and `hold`." Atomicity: "A new sidecar authority version and its appended pointer amendment are published in the same commit; publishing one without the other is an authority-publication failure." Fork sanction: "The presently committed v13/v14 sidecars predate this rule; the staged state carrying this overlay is the sanctioned transition, and the rule binds from this overlay's publication commit forward." Validator tasking: "The publication obligation below tasks the planning validators with a current-sidecar-head check; until that validator lands, the rule is enforced at review." — but see NV-1: the "staged state carrying this overlay" is not, at this moment, what the index holds. |
| 2 | Rubric F2 / adversarial DC-9 & DC-10 carrier gaps: publication obligation must direct the paired `epic-6-authority-2026-08-18-v13` amendment, graph regeneration, and validator tasking | **RESOLVED** | Chain Hygiene: "That same publication must author the paired canonical `epic-6-authority-2026-08-18-v13` epic amendment carrying the DC-9, DC-10, and DC-11 obligations into the epic block, story contracts, and obligation inventory; regenerate `v9-execution-graph-v1.json` with the composed node set per DC-4; and task the planning validators with the current-sidecar-head check the discovery rule names. Prose alone carries none of these obligations once their canonical carriers exist." DC-9: "carried by the Epic 9 story contracts through the paired epic-authority amendment this overlay directs." DC-10: "carried into the canonical epic authority and graph by the paired epic-authority amendment this overlay directs." Residual (LOW): the authoring role is implicit (PM via the v9 single-writer rule), not named. |
| 3 | Rubric F3: dispatch-ledger key must be a DECLARED exception to the key-level tenant-segment rule (line 1105) | **RESOLVED** | DC-7: "The dispatch-ledger key is the declared exception to the key-level tenant-segment rule: it derives solely from the platform-owned dispatch-identity digest, is fixed-length and non-disclosing, and tenant fail-closed enforcement applies on every ledger read and write path instead." Matches F3's second fix option. See NV-2 for the poison-scenario coverage consequence. |
| 4 | Rubric F4: `conversations-vocabulary-v1` must have a named owner and path | **RESOLVED** | DC-2: "owed by `conversations-vocabulary-v1` — a candidate-bound planning sidecar at `_bmad-output/planning-artifacts/conversations-vocabulary-v1.json`, authored under the Quality owner — which must ratify shipped contracts and may not require a public-contract change. No successor story that serializes redaction or hydration display state may enter review before that contract is published; the review gate enforces this until a validator carries it." File correctly does not exist yet (fail-closed gate). See NV-3 on the owner choice. |
| 5 | Rubric F5: DC-1's confidence-interval method must be anchored (declaration site + default) | **RESOLVED** | DC-1: "The method computing the half-width is declared in the baseline artifact and is identical for baseline and post; absent a declaration, the method is the percentile bootstrap over the retained raw samples." Bonus closure of adversarial R8: "An envelope member not explicitly enumerated as versioned above is frozen." Carried-forward residual: which statistic's CI is bounded (post vs difference vs ratio) is still unstated. |
| 6 | Rubric F6: DC-6 must name the sentences it supersedes | **RESOLVED** | DC-6: "This supersedes, in their default reading, the earlier sentences 'Non-governance commands may continue during audit degradation only by explicit ADR' and 'Non-governance behavior during audit degradation requires an ADR before implementation'." Both quotes verified **verbatim** against lines 868 and 1201. Residual (LOW): the corresponding open question (line 605) is not named as answered. |
| 7 | Adversarial DC-2 CRITICAL (R1): must RATIFY shipped `ProjectionTrustState`, no rule may require a public-contract change | **RESOLVED** | DC-2: "The shipped public contract `Hexalith.Conversations.Contracts.TrustStates.ProjectionTrustState` — the closed value set `Current, Stale, Rebuilding, Unavailable, Forbidden, Redacted`, accepted by Story 6.2 as the single public trust-and-freshness vocabulary for read contracts — is ratified as the normative v1 vocabulary and is the 'one shared enum or value contract' the pattern rules require. It is consumed, never re-declared, by API, UI, diagnostics, and evidence. The May-era candidate trust list (`Unknown, Pending, Verified, Contradicted, …`) is historical candidate material, not a rename or widening requirement; members from it may arrive only through compatible additive contract evolution with named-owner approval under the preservation rules." Member set exact-match confirmed (section 2). The quoted phrase "one shared enum or value contract" matches line 1129 verbatim; lines 554/918 are labeled candidate lists, so their demotion is consistent. |
| 8 | Adversarial DC-5 CRITICAL (R3): must ratify shipped `ProjectionFreshnessV1` composite as the anchor, prohibition scoped to EventStore internals | **RESOLVED** | DC-5: "the authoritative temporal evidence anchor on public and trust-bearing surfaces is the `ProjectionFreshnessV1` composite — `ProjectionContractSchemaVersion`, `ProjectionCursor`, and `LastAppliedEventPosition`, the Conversations-owned public event position the accepted contract deliberately exposes. Timestamps in that contract are display and lag metadata, never the anchor. The public-surface prohibition on 'event sequence numbers' and 'storage offsets' is hereby scoped to EventStore stream internals; the Conversations-owned public event position is not an EventStore internal. Non-public evidence artifacts may additionally carry raw EventStore positions." Field names exact-match confirmed (section 2). R3's second failure ("projection version" two sources) is dissolved: the phrase is gone and the composite includes the rebuild-deterministic `LastAppliedEventPosition`, so the anchor is neither static nor determinism-unproven. Scoping analysis in section 4. |
| 9 | Adversarial DC-4 HIGH (R4): name `v9-execution-graph-v1.json`, task its regeneration, give the point-in-time rule | **RESOLVED** | DC-4: "The enumerated node-ID set in `v9-execution-graph-v1.json`, regenerated under this overlay's publication to include the sidecar checkpoint nodes, is the sole membership authority; a bare cardinality — including V12's 'exactly 33 nodes', which described the graph at V12 publication before the sidecar checkpoints — is a derived check, never the pin. A sidecar authority's architecture-version reference is a point-in-time binding to the version current at its minting; a later architecture overlay does not invalidate it, and each new sidecar version binds the then-current architecture version." Regeneration also tasked in Chain Hygiene ("regenerate `v9-execution-graph-v1.json` with the composed node set per DC-4"). The point-in-time rule also closes rubric F10's precedence gap. Carried-forward residual: no `graphDelta` field required of future (v15+) sidecars. |
| 10 | Reality O-1 (LOW): Aspire preview-package nuance | **RESOLVED** | Factual Refresh: "Shared `Hexalith.Builds` props pin Aspire `13.4.6` (the Keycloak and Kubernetes hosting packages at `13.4.6-preview`) and Dapr `1.18.5`". Verified: `Aspire.Hosting.Keycloak` and `Aspire.Hosting.Kubernetes` are `13.4.6-preview.1.26319.6` in `references/Hexalith.Builds/Props/Directory.Packages.props` — "13.4.6-preview" is fair shorthand for the same base version. |
| 11 | Reality O-2 (LOW): V9-and-V10 hold-note (grammar note excused V10 only) | **RESOLVED** | Grammar section: "The absence of `hold=` on the V9 and V10 markers is a schema gap, not a hold gap; both bodies state the hold remained `ACTIVE`." Now covers both overlays, matching O-2's ask. The same grammar sentence also declares the END-marker attribute set, closing the other half of rubric F9. |

**11 / 11 RESOLVED.**

## 2. Ratification exact-match checks against shipped code

### DC-2 vs `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs`

| DC-2 stated member | Code | Verdict |
| --- | --- | --- |
| Current | `public static ProjectionTrustState Current` | MATCH |
| Stale | `Stale` | MATCH |
| Rebuilding | `Rebuilding` | MATCH |
| Unavailable | `Unavailable` | MATCH |
| Forbidden | `Forbidden` | MATCH |
| Redacted | `Redacted` | MATCH |

Six members, same order as the `KnownStates` dictionary; `Parse` throws on any
other value, confirming "closed value set". The type's XML doc — "the public
trust and freshness vocabulary for read contracts" — supports DC-2's "single
public trust-and-freshness vocabulary for read contracts". **EXACT MATCH.**

### DC-5 vs `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs`

| DC-5 stated field | Code | Verdict |
| --- | --- | --- |
| `ProjectionContractSchemaVersion` | `SchemaVersion ProjectionContractSchemaVersion` | MATCH |
| `ProjectionCursor` | `string ProjectionCursor` | MATCH |
| `LastAppliedEventPosition` | `long LastAppliedEventPosition` (validated `>= 1`, XML-doc "the last accepted public event position") | MATCH |

DC-5's "timestamps in that contract are display and lag metadata" correctly
describes the remaining temporal fields (`LastAppliedEventTimestamp`,
`ProjectionGeneratedAt`, `LagDuration`). **EXACT MATCH.**

## 3. Mechanical verifications (V13 markers and sidecar head)

- **BEGIN marker (line 2360)** carries
  `sidecar-head=v14-current-candidate-authority-v1.json` and
  `sidecar-head-sha256=e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f`. **CONFIRMED.**
- **END marker (line 2619)** carries the same two attributes. **CONFIRMED.**
- **`sha256sum _bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json`**
  = `e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f`.
  **MATCH** (file is committed and unmodified).
- **Marker attribute sentence vs actual markers:** the declared BEGIN set
  (`version`, `epic-authority`, `supersedes`, predecessor block bytes and
  SHA-256, `sidecar-head`, `sidecar-head-sha256`, `candidate-binding`, `hold`)
  matches the actual BEGIN attributes one-for-one and in order
  (`v12-block-bytes`/`v12-block-sha256` being the predecessor pins); the
  declared END set of six matches the actual END attributes exactly. The
  grammar sentence is **true of the markers that carry it** — the F1 defect
  (rule promising what the marker cannot hold) is gone.
- **Other named digests spot-recomputed** (unchanged from the reality review's
  round, reconfirmed against current worktree bytes): a3 proposal
  `cbc2eb2f…d1d2c0`, readiness-gate proposal `66d12d5f…4494dd`, v13 sidecar
  `f2f02115…448138f`, execution view v2 `8fce0f38…cbdbed1e` — all MATCH.
  (V9–V12 chain digests were verified in the prior rounds and were not
  recomputed here per the gate instruction.)

## 4. DC-5 scoping vs the still-binding prohibition text (lines 1116, 1322)

**The still-binding sentences:**

- Line 1116: "They must not expose EventStore stream names, event type names,
  sequence numbers, storage offsets, replay mechanics, internal projection
  topology, …"
- Line 1322: "…contract tests proving the response contains no EventStore
  identifiers, stream names, event sequence numbers, raw event payloads,
  internal trust flags, or unhydrated Party personal data."

**Assessment: defensible clarification, not a contradiction.** Four reasons:

1. Line 1116's list reads naturally as an EventStore-internals disclosure list
   (the head noun "EventStore" plausibly distributes across "stream names,
   event type names, sequence numbers, storage offsets, replay mechanics").
2. The immediately following still-binding bullet (line 1117) already permits
   "status, freshness, and evidence references" on public APIs while banning
   "EventStore identifiers" — DC-5's scoping is exactly this bullet's split
   applied to positions.
3. Story 6.2 was accepted with `ProjectionFreshnessV1.LastAppliedEventPosition`
   on every trust-bearing read and (per the accepted contract-test obligation)
   passing tests, so the operative interpretation of "event sequence numbers"
   already excluded the deliberately-public domain position. DC-5 ratifies the
   accepted reading rather than changing behavior.
4. The scoping is **declared**, not silent: "is hereby scoped to EventStore
   stream internals" is supersession-in-place, and DC-1..DC-11 sit inside the
   preamble's enumerated supersession scope ("the enumerated divergence
   closures DC-1 through DC-11"), so the clause carries authority. It does not
   *lack* supersession language.

**What it does lack** is DC-6's technique of quoting the narrowed sentences
verbatim: DC-5 quotes only the key phrases ("event sequence numbers" — verbatim
in line 1322; line 1116 says "sequence numbers" without "event" — and "storage
offsets", verbatim in 1116). The literal text at 1116/1322 stays dead-in-place,
and a line-1322 test author must apply the scoping by cross-reference (assert
absence of EventStore-internal positions while permitting
`lastAppliedEventPosition`). That reading is determinate, so this is a LOW
citation-hygiene residual (NV-5), not a defect requiring rework.

## 5. NEW findings introduced by the corrections

### NV-1 — MEDIUM — The gate-FAILED prior block is what is actually staged; the revised block exists only in the worktree

`git status --porcelain` shows `architecture.md` as `MM` (staged **and**
unstaged changes). `git show :_bmad-output/planning-artifacts/architecture.md`
contains **zero** occurrences of `sidecar-head=` and its V13 END marker is the
old four-attribute form — i.e. the index still holds the pre-revision block
that FAILED the rubric and adversarial gate. The revised block's own sanction
sentence — "the staged state carrying this overlay is the sanctioned
transition" — is therefore currently false of the git index: the staged state
carries the *prior* overlay. A publication commit made from the index as-is
would publish the defective version and permanently freeze the F1/R2 defects.
**Fix:** `git add _bmad-output/planning-artifacts/architecture.md` (staging the
revised worktree bytes) before the publication commit; nothing in the document
text needs to change.

### NV-2 — LOW — DC-7's poison-scenario coverage now explicitly excludes the dispatch-ledger key

The revision changed the scenario-binding sentence to "The cross-tenant
derived-key poison scenario binds this grammar version **and covers the
tenant-segmented conversation and index keys**". Combined with the new
declared exception, the one key with no tenant segment is now covered by no
named executable scenario; cross-tenant ledger isolation rests entirely on the
prose rule "tenant fail-closed enforcement applies on every ledger read and
write path instead". The prose rule is binding and the trade is a legitimate
reading of F3's fix options, but no BDD/test artifact is named that proves the
path-level enforcement. One clause naming a ledger-path fail-closed scenario
(or extending the poison scenario with a ledger read-path assertion) would
close it.

### NV-3 — LOW — The Quality owner's portfolio is extended without amending the single-writer enumeration

DC-2 assigns `conversations-vocabulary-v1` authoring to the Quality owner. The
v9 single-writer list (still binding, lines ~1908–1917) gives the Quality owner
"validator rules and the independent IR-0 report"; a vocabulary/display-state
contract is a new artifact class outside every enumerated portfolio. Creating
and assigning a new artifact is not a cross-owner mutation, so this is not a
violation — but a strict parity reader comparing owner rosters has no line
authorizing the extension. One clause ("the single-writer inventory is extended
by this closure") would remove the wedge.

### NV-4 — LOW — "Sidecar authority version" is now an overloaded trigger

The discovery rule's same-commit obligation fires on "a new sidecar authority
version", and DC-2 describes `conversations-vocabulary-v1` as "a candidate-bound
planning sidecar". Whether publishing the vocabulary sidecar (which can never be
the `sidecar-head`) triggers the pointer-amendment obligation is undefined.
Intent is clearly "checkpoint-authority sidecars only"; one qualifying word
("checkpoint sidecar authority version") settles it.

### NV-5 — LOW — DC-5's scoping does not cite the sentences it narrows

Per section 4: defensible and declared, but lines 1116 and 1322 keep their
literal unscoped wording with no in-place pointer — the dead-text hazard the
gate itself flagged (rubric F6/F11 pattern). Recommend DC-6-style verbatim
citation at the next amendment; no rework required now.

## 6. Carried-forward residuals (pre-existing, unchanged by the revision — recorded, not re-scored)

- DC-1: which statistic's CI is bounded (post vs difference vs ratio) — adversarial R6, second half.
- DC-9: "asserted-behavior identity" not mechanically defined — adversarial R7 (its carrier gap is fixed; the definition gap is not).
- Line 977 "hidden-by-tenant" freshness state is in no ratified list — plausibly resolved by the vocabulary sidecar's owed condition-to-state mapping; no disposition sentence yet.
- `BuyerAcceptanceDemoTrustState` (Testing package: `Incomplete`, `Hidden`, `Failed`) still has no disposition under the no-synonyms rule (line 1130).
- Future (v15+) sidecars owe no `graphDelta` field — DC-4 attack 2's forward half.
- Hold-record validity is still not wired to the operational-envelope disposition — rubric F8.
- Preamble residual-force sentence vs DC-3's record-contract touch — adversarial R10 (the enumerated supersession list controls, but the clarifying clause was not added).
- Sidecar-vs-architecture version-namespace reservation going forward — rubric F10 tail.

None of these blocks the publication commit; all have one-sentence fixes
available in a future amendment or in the paired epic/publication artifacts.

## 7. Conclusion

The revision resolves every finding it was chartered to resolve, and it does so
with text that recomputes, exact-matches the shipped contracts it ratifies, and
stays inside the overlay's declared supersession scope. The single blocking
item is operational, not textual: stage the revised `architecture.md` bytes
before the publication commit (NV-1), or the commit will freeze the version
this gate already failed.
