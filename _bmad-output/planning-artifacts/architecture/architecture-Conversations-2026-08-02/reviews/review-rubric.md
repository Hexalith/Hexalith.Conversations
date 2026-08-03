# Architecture Reviewer Gate — Rubric Walker

**Reviewed:** 2026-08-02  
**Target:** `_bmad-output/planning-artifacts/architecture/architecture-Conversations-2026-08-02/ARCHITECTURE-SPINE.md`  
**Focus:** appended V9 Execution Overlay and inherited v8 constraints  
**Approved source:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`  
**Verdict:** **NEEDS REVISION FOR SOURCE-COMPLETE V9 AUTHORITY; V8 TECHNICAL INVARIANTS AND THE GLOBAL IMPLEMENTATION HOLD ARE PRESERVED.**

The overlay correctly replaces only the non-ready v8 execution graph, keeps completed history immutable, preserves the v8 system boundary, and strengthens the hold with same-candidate validation, outcome-neutral IR-0, explicit owner lift, and drift-triggered reassessment. It still leaves two high-severity divergence points in the cross-story contract and inherits one unresolved repository-authority conflict. No implementation is authorized: `publication-candidate=UNBOUND` is an intentional fail-closed state, not a passable placeholder.

## Gate Evidence

- Deterministic spine lint passed with zero findings:
  `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-Conversations-2026-08-02`.
- The begin marker's `v8-prefix-sha256` is correct: the bytes before the marker hash to `7fd33168f34bb7d3326b4abb0eb79999270c11fefc7f50ec3acdd62fb1b86df5`.
- The overlay's authority-discovery rule explicitly freezes the historical v8 frontmatter and resolves current authority from the last complete overlay marker (lines 1833–1839); this is coherent with append-only publication, provided every v9 consumer and validator implements that rule.
- The v9 candidate remains deliberately unbound because its companion epics, maps, stories, projections, and validators are not yet published (lines 1841–1848). The overlay correctly prevents IR-0 and hold lift in this state.
- The historical v1 execution view's recorded architecture digest does not match the current preserved v8 prefix. The overlay explicitly requires the v9 validator to report this pre-existing mismatch as a blocker rather than rewrite v1 or compare it to the appended file (lines 1850–1856). This is a publication blocker already handled fail-closed by the Rule, not a weakening of v8.

## Critical Findings

None.

## High Findings

### R-1 — The successor-story contract drops load-bearing atomic-acceptance invariants

**Checklist test:** Does each Rule prevent the stated divergence, and could two units one level down choose incompatibly?

**Evidence:** The overlay's cross-story rules at lines 1961–1987 preserve stable two-digit AC IDs, exact commands, result semantics, evidence bindings, lane completeness, fault injection, migration-strength digests, and measured final records. However, the approved v9 source makes additional constraints mandatory for *every* successor story (proposal lines 218–265) that the overlay neither repeats nor incorporates as normative rules:

- one acceptance scenario asserts exactly one outcome;
- `Given` freezes exact authority identities, input paths, schema versions, inventories, and digests;
- `When` binds working directory, project, filter, and arguments, not only a generic exact command;
- `Then` binds schema identity and required fields, not only output path and schema;
- every story freezes its file/inventory baseline at entry and forbids dynamic phrases such as “any item added later”;
- rollback identifies the artifacts that remain immutable;
- the shared high-risk catalogue cannot replace story-local acceptance contracts;
- the canonical machine record carries the required candidate, input, scenario, blocker, output, and summary fields.

The current phrase “one bounded outcome” applies to the story, not to each scenario. Two successor authors can therefore produce incompatible scenario granularity, input freezing, rollback retention, and machine-record shapes while both claiming compliance with the overlay.

**Disposition:** **Autofix before final v9 publication.** Add the omitted cross-story invariants directly to `Successor Story Contract`, or explicitly and normatively adopt the approved source's §4.4 contract without weakening it. The canonical epic authority may supply per-story values, but the architecture must bind the shared shape and failure semantics.

### R-2 — Story-level supersession does not mechanically prove obligation-level preservation

**Checklist test:** Does the spine cover the source specification's capabilities and miss no real divergence point?

**Evidence:** Lines 1898–1902 require exactly one disposition for every unfinished v8 Story 6.x and reject missing or duplicate *story* mappings. The approved source requires more: every unfinished v8 obligation must have one successor disposition with no obligation lost (`SC-03`, proposal lines 393–400), while migrated assertions require before/after inventories and strength digests (proposal lines 231–235).

An oversized v8 story can map once to a successor epic while one of its acceptance obligations silently disappears, duplicates, or weakens. The current zero-gap rule would still pass because the old story identifier appears exactly once. This is the central preservation risk created by splitting five oversized stories into atomic successors.

**Disposition:** **Autofix before final v9 publication.** Require the machine-readable supersession record to enumerate a frozen digest-bound inventory of each unfinished v8 obligation/acceptance identity and give each exactly one current successor acceptance identity or explicit preserved/non-executable disposition. Reject missing, duplicate, orphaned, and strength-weakened mappings.

### R-3 — The inherited AppHost rule conflicts with the required Hexalith repository baseline

**Checklist test:** Does the spine ratify brownfield reality without contradicting binding repository architecture?

**Evidence:** Lines 1877–1881 preserve `Hexalith.Conversations.AppHost` as a module-owned, non-packable, non-publishable user/E2E test fixture. The codebase contains that project and mechanically marks it non-packable/non-publishable, so the overlay accurately ratifies v8 and current reality. The required `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, however, states that a domain module must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project and places the AppHost in the platform/host repository.

The execution-only v9 overlay is not authorized to change this technical rule, but leaving two binding authorities unresolved lets future units choose opposite ownership models.

**Disposition:** **Discuss and defer to a separately approved technical architecture or baseline amendment; do not resolve inside this execution overlay.** Record the conflict as a blocking inherited open item before implementation. The existing global hold safely prevents code action meanwhile.

## Medium/Low Tail

One medium issue remains: the inherited historical version notes say public Aspire documentation is 13.3 and sibling Dapr packages are 1.17.7, while the brownfield central pins are Aspire 13.4.6 and Dapr .NET 1.18.5; official documentation identifies Aspire 13.4 as supported and Dapr 1.18 as latest. Because the section is explicitly historical and the durable Rule is to follow central sibling pins, this does not weaken v9, but the catch-all inheritance wording should distinguish binding invariants from stale historical version observations. See [Aspire support policy](https://aspire.dev/support/) and [Dapr reference documentation](https://docs.dapr.io/reference/).

## Good-Spine Checklist Result

| Criterion | Result | Review |
| --- | --- | --- |
| Fixes real divergence points for the level below | **Partial** | Epic boundaries, graph, hold, candidate binding, and evidence lanes are fixed; scenario atomicity and obligation-level supersession are incomplete (R-1, R-2). |
| Every Rule is enforceable and prevents its divergence | **Partial** | Marker/hash, hold, graph, lane, denominator, and drift rules are mechanical; the shortened acceptance and mapping rules admit incompatible compliant implementations. |
| Deferred/open material is safe for the next step | **Pass, fail-closed** | The unbound candidate and unpublished companions explicitly block IR-0 and implementation. |
| Named technology is verified-current | **Partial** | No new stack is bound by v9; one historical inherited version observation is stale but current central pins control. |
| Ratifies brownfield codebase | **Pass with inherited conflict** | The overlay matches current AppHost/test-fixture reality but conflicts with the repository baseline (R-3). |
| Covers the approved v9 source | **Partial** | Outcome graph, dispositions, denominators, gate semantics, and hold land correctly; source §4.4 and obligation-level SC-03 are incomplete. |
| Does not weaken inherited v8 invariants | **Pass** | No v9 rule weakens EventStore authority, tenant fail-closed behavior, ownership, preservation denominators, UX non-activation, SM-C2, evidence rules, or completed history. |
| Covers every owned structural dimension | **Pass for execution-only scope** | Technical, data, security, hosting, deployment, and operational envelopes are explicitly inherited; v9 decides execution authority, gates, evidence identity, and publication boundaries. |

## Preserved Invariants Confirmed

- Conversations/platform/deployment ownership remains unchanged.
- Hexalith.EventStore remains the sole durable write-side authority; derived stores remain non-authoritative.
- Tenant authorization remains fail-closed across interactive, background, tool, export, and verification paths, with cross-tenant non-disclosure.
- The 20/104/77/52 denominators, exactly 124/124 functional coverage, exactly 28 UX acceptance IDs, and FR-16-only deferral remain fixed.
- UX remains `preserved-not-activated`; v9 adds no product UI scope.
- The universal four-row SM-C2 gate remains `post P95 <= 1.05 x baseline P95` under the identical frozen envelope with no substitute disposition.
- Epics 1–5 and Stories 6.1, 6.2, and 6.7 remain immutable completed history; partial/prepared successor input remains unaccepted until candidate-bound validation.
- Conformance tiering, projection-proof lifecycle, measured final records, promotion, evidence boundaries, audit, privacy, idempotency, hosting, and deployment constraints remain inherited and read-only.
- IR-0 is independent and outcome-neutral; complete actual results are preserved unchanged, and no non-`READY`, incomplete, blocked, stale, or drifted assessment can lift the hold.

## Gate Conclusion

The requested append-only overlay succeeds at preserving v8 technical invariants and the implementation hold. It should not be treated as source-complete v9 execution authority until R-1 and R-2 are corrected, R-3 is recorded for separate authority resolution, all companion artifacts are published against one bound candidate, the historical v1 mismatch is handled by the v9 validator, mechanical validation passes, IR-0 independently returns candidate-matched `READY`, and the release owner explicitly lifts the hold.

## Recheck Addendum — Latest Shared Architecture

**Rechecked:** 2026-08-02  
**Latest verdict (supersedes the original high-finding verdict above):** **PASS FOR THE THREE RECHECKED HIGH FINDINGS.** R-1 and R-2 are resolved; R-3 is now explicitly and correctly deferred without weakening either v8 or the Hexalith baseline. No critical/high rubric finding remains from this review. This does not lift the separately binding implementation hold.

- **R-1 — Resolved.** Lines 2021–2058 now bind the complete shared atomic-acceptance contract: one outcome per scenario, exact `Given`/`When`/`Then` inputs and semantics, frozen baselines, rollback immutability, fault injection, before/after strength digests, measured final records, and the high-risk-catalogue non-substitution rule. The common Epic 7 generator owns final-record shape, while each story must bind its exact output schema and required fields; this prevents per-story drift without bloating the spine with the full seed schema.
- **R-2 — Resolved.** Lines 1953–1962 now state that story-row coverage is insufficient and require a frozen, digest-bound inventory of every v8 criterion, checkpoint, prohibition, dependency, evidence obligation, rollback condition, and completion gate. Each item maps exactly once to a successor atomic AC or explicit non-executable disposition; missing, duplicate, orphaned, or many-to-none mappings fail publication.
- **R-3 — Correctly deferred.** Lines 1944–1949 now surface the AppHost/baseline interpretation explicitly: v8 retains only the existing non-shipping test fixture, the baseline prohibition on shipping a module AppHost remains binding, and a stricter interpretation that also forbids a repository-local fixture requires a separate approved technical amendment. V9 neither expands the fixture nor silently chooses between authorities.
