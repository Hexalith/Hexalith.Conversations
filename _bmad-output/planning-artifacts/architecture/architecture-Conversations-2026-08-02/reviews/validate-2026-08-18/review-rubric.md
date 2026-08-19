# Rubric Review — architecture.md (Good-Spine Checklist)

- **Target:** `_bmad-output/planning-artifacts/architecture.md` (2,359 lines, frontmatter v8, overlays through V12; V13/V14 authority in sidecar JSON)
- **Reviewer lens:** good-spine rubric (8 items) + discoverability assessment
- **Date:** 2026-08-18
- **Gate context:** implementation hold ACTIVE; append-only authority document

## Verdict

The document is a coherent, supersession-disciplined, and unusually enforcement-minded authority: every amendment declares exactly what it supersedes, the one detected append-only breach (the v4 in-place rewrite) is disclosed and remediated with byte-length/SHA-256-pinned overlay markers, fail-closed defaults govern both the domain (tenant access, freshness, audit) and the process itself (hold defaults to ACTIVE on any missing or mismatched evidence), and the pattern layer names its 15 divergence points and closes most of them with machine-checkable rules. It is, however, a document that has outgrown its format: the currently binding rule on any topic is the residue of a 12-layer supersession computation plus two sidecar files the document never points to, the operational/environmental envelope (environments, infra/provider strategy, runbooks) is a whole dimension left silent or silently dropped, 13 of 20 initiative FRs have no in-document architectural disposition, and several pre-overlay divergence points (canonical trust/freshness vocabulary, ADR numbering, Feature-FR-to-structure mapping) remain unclosed. Because the hold is ACTIVE none of this is an immediate build hazard, so no finding is rated critical — but the two high findings should be resolved before any hold-lift, since that is the moment builders start reading this document under time pressure.

---

## Findings

### F1 — HIGH — In-document authority discovery now yields a stale answer (Rubric 7, discoverability)

**Evidence:** Lines 1839–1843 define the discovery rule: "Machine readers determine current authority from the last complete architecture overlay marker, never from the historical frontmatter alone." The last overlay marker in the file is V12 (line 2358). V12 states "The graph has exactly 33 nodes" (line 2288) and its END marker carries `hold=ACTIVE`. But current authority has continued outside the document: `v13-current-proof-authority-v1.json` (checkpoint `E6-CURRENT-PROOF`, predecessor `E6-REMEDIATION`) and `v14-current-candidate-authority-v1.json` (checkpoint `E6-CURRENT-CANDIDATE`, planning candidate `151f9651...`, predecessors `E6-REMEDIATION` and `E6-CURRENT-PROOF`) add checkpoint nodes and rebind the planning candidate. Both sidecars declare `"architecture": "conversations-architecture-2026-08-04-v12"`, so the architecture-version answer is still technically correct, but a machine or human reader executing the document's own stated algorithm concludes the graph has 33 nodes and the candidate state is as V12 left it — both now false. Nothing in the document states that the authority chain may continue in sidecar files, and no forward pointer exists.

**Why it matters:** The document's core integrity mechanism — "trust the last overlay marker" — was designed to defeat frontmatter staleness, and it has now itself gone stale in exactly the same way. Two readers (one reading only architecture.md, one who knows the sidecar convention) will disagree on the current checkpoint graph, the current PC, and what E6 remediation state is. An append-only closing rule is needed: either a final in-document pointer overlay ("authority continues in sidecars matching `hexalith.conversations.*-authority.v*`; resolve the newest by predecessor chain") or a V15 marker each time a sidecar becomes current.

### F2 — HIGH — Operational/environmental envelope is a silent dimension (Rubric 8)

**Evidence:** Production composition is delegated ("Platform deployment owns production orchestration; the domain module supplies platform-consumable metadata and domain behavior," line 1658), but the delegation names no platform authority document and no interface contract beyond that one line. In-document there is: no environment topology (dev/staging/prod, what a release candidate promotes through — RG-15 defines a decision record, not an environment path); no infra/provider strategy for module-owned concerns (what consistency/durability the projection state store must provide, secrets, backup/restore of the event store vs. legal hold, data residency); and no operations surface — the superseded May 14 tree had `docs/runbooks/` with `projection-rebuild.md`, `governance-verify.md`, `tenant-isolation-denial.md` (lines 1448–1451), while the authoritative Corrected Target Directory Structure (lines 1400–1422) has `docs/adrs/` and `docs/release-evidence/` only. Runbooks were dropped without a decided/deferred/open disposition, even though rebuild, quarantine-repair, and tenant-denial workflows are binding behavioral requirements throughout (e.g., lines 794–802, 1214). Related open items — "capacity thresholds still need architectural measurement envelopes or buyer-accepted unknown status" (line 454) and "Who signs waivers for unknown numeric capacity targets" (line 609) — remain open with no owner or story.

**Why it matters:** The rubric treats a whole silent dimension as a finding in itself. Here the silence is partially disguised as delegation, but delegation without a named counterpart artifact is indistinguishable from a gap: when Epic 15/RG-15 tries to close a release, nobody can point to where environment promotion, operational runbooks, or capacity-unknown waivers were decided. Two builders asked to make "projection rebuild" real would invent divergent operational shapes.

### F3 — MEDIUM — Canonical trust/freshness vocabulary is demanded but never defined (Rubric 1, 3)

**Evidence:** The Shared Vocabulary Rule (lines 1257–1269) is binding: "The architecture must define one canonical vocabulary for these categories before broad implementation... Agents must not invent local synonyms." Yet the document offers only candidates: "Candidate trust states include `Unknown`, `Pending`, `Verified`, `Contradicted`, `Stale`, `Redacted`, `Unavailable`, `Forbidden`" (lines 554, 918) and "Query results expose freshness states **such as** `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted`" (line 1211); the Blocking Freshness Rule gives a "Minimum shape" (lines 1273–1284). The two lists overlap (`Stale`, `Redacted`, `Unavailable`, `Forbidden` appear in both) without stating whether trust and freshness are one enum, two enums, or a composite, and no successor story or ADR owns closing this.

**Why it matters:** This is precisely a two-builders-diverge point: one builds a single `TrustState` enum, another builds separate `FreshnessState`/`TrustState` contracts; both can cite this document. Because the vocabulary is a public contract ("one shared enum or value contract across API, UI, diagnostics, and evidence," line 1129), divergence here is expensive to unwind. The ADR-trigger net ("Exposes a new public contract... trust state," line 1224) catches it only if the implementer notices they are choosing.

### F4 — MEDIUM — 13 of 20 initiative FRs have no in-document architectural disposition (Rubric 6)

**Evidence:** Line 325 activates "FR-1 through FR-15 and FR-17 through FR-20." The Initiative Landing-Zone Register (lines 346–357) has rows for FR-10 through FR-16 only. Line 1680 claims "The 20 initiative FRs are governed by the authority rebaseline and landing-zone register," but FR-1..FR-9 and FR-17..FR-20 never appear as named requirements anywhere in the document. The 124/124 mechanical coverage obligation (lines 2072–2074) delegates the complete FR map to out-of-document validators and story contracts.

**Why it matters:** A builder or reviewer cannot answer "where does FR-7 land architecturally?" from the architecture at all — they must reverse-engineer it from the PRD plus epic authority. For a document whose stated job is to be the initiative architecture authority, more than half the initiative-requirement namespace being invisible is a coverage gap, even if the SM-1/SM-C2/oracle sections implicitly host some of them.

### F5 — MEDIUM — One public-API prohibition is unenforceable as written (Rubric 2)

**Evidence:** Line 1116: "They must not expose EventStore stream names, event type names, sequence numbers, storage offsets, replay mechanics, internal projection topology, route names, DTO property names, serialized payloads, error codes, logs intended for clients, OpenAPI descriptions, or SDK/client package names." Read literally, the tail of the list prohibits the public API from exposing its own route names, DTO property names, serialized payloads, error codes, and OpenAPI descriptions — which every public HTTP API necessarily does, and which other rules require (Problem Details "with stable code," line 1159; "Use OpenAPI for server contracts," line 888).

**Why it matters:** The intended reading is clearly "EventStore's route names, DTO property names, ..." but the sentence does not scope the qualifier. A conformance-test author implementing this rule literally writes an unpassable check; a lenient one silently narrows it. That is a rule that cannot both be enforced and prevent its stated divergence. One clarifying appended sentence fixes it.

### F6 — MEDIUM — Three incompatible ADR numbering spaces coexist (Rubric 1)

**Evidence:** (a) The ADR Backlog (lines 756–767): ADR-001 EventStore authority, ADR-002 idempotency, ADR-003 tenant projection, ADR-004 governance audit pairing, ... ADR-010 retention. (b) The historical directory tree (lines 1441–1445): `0001-idempotency-contract.md`, `0002-tenant-projection-freshness.md`, `0003-governance-audit-pairing.md`, `0004-event-schema-versioning.md`, `0005-redaction-replay.md` — same subjects, shifted numbers. (c) Reality: the accepted `docs/adrs/0003-projection-read-store-population-proof.md` (line 399) and v7's "Story 6.12 authors ADR 0004 for an immutable predecessor-linked projection-proof lifecycle" (line 211) — a third assignment for 0003/0004. The backlog section sits in "Core Architectural Decisions," which is not marked superseded.

**Why it matters:** "Create ADR-004" currently has three different meanings depending on which part of the document a builder trusts. The subjects themselves are still required pre-implementation decisions ("The following ADRs are required before dependent implementation stories proceed," line 756), so the collision will be hit exactly when the hold lifts.

### F7 — MEDIUM — Requirements-to-structure mapping binds only the superseded tree (Rubric 5, 1)

**Evidence:** The Feature-FR mapping (lines 1566–1586) maps FR ranges to `Admin/TrustComponents`, `Conformance/Suites/...`, `ServiceDefaults`, `samples` — directories that exist only in the superseded May 14 layout. The v3 namespace note (line 1564) correctly labels the layout historical but supplies no replacement mapping onto the Corrected Target Directory Structure, which differs materially (`Admin.Web` instead of `Admin`, no `src/Conformance` project, conformance moved to two test-tier projects, no `samples/`). Meanwhile the completeness checklist asserts "[x] Requirements to structure mapping complete" (line 1758).

**Why it matters:** The only requirements→structure traceability in the document points at code locations the target tree forbids. A builder implementing FR56–FR69 operator workflows or FR95–FR99 observability has no current answer for where that code lives; the checked checklist item overstates readiness.

### F8 — MEDIUM — V10's frozen BMAD workflow inventories are self-invalidated by the repo's move to BMAD 6.11.0 (Rubric 4, internal staleness)

**Evidence:** V10 binds `V9-EVIDENCE-WORKFLOWS-v2`/`V9-EVIDENCE-GUIDANCE-v2` against BMAD `6.10.1n46` (line 2147) and rules: "Any BMAD workflow generation change invalidates the workflow and guidance inventories and the Story 10.3/10.4 evidence until route coverage, cross-tree parity, deterministic render parity, and resolved customization are revalidated" (lines 2166–2168). The repository's recent history includes "fix: update BMAD 6.11.0" (commit 4ba45a7). The document contains no record of the required revalidation and, being append-only, cannot reflect it below V12.

**Why it matters:** By the document's own drift rule, the V10-frozen inventories are presumptively invalid right now, and a reader has no way to tell whether revalidation happened. This is exactly the internal-staleness signal the gate should chase into the sidecar/validator record; flagging for the web/repo-checking reviewer to confirm whether a post-6.11.0 revalidation artifact exists.

### F9 — MEDIUM — Legacy "Open Architecture Questions" have no disposition register (Rubric 3, 8)

**Evidence:** Lines 602–610 list seven open questions (authoritative temporal evidence anchor; audit-unavailable non-governance command boundary; acceptable degraded hydration states; which freshness states block reliance; redaction/re-index contract for derived indexes; capacity-waiver signing; retention/legal-hold/deletion interaction). Unlike OQ-1..OQ-5, which get an exemplary one-row-per-question register with binding decisions and reopen conditions (lines 360–370), these seven have no state, owner, or story binding. Some are indirectly gated by ADR triggers (e.g., audit degradation, line 1201), but at least the temporal-evidence-anchor and capacity-waiver questions have no gate at all.

**Why it matters:** The document itself declares "A decision without failure semantics and evidence obligation is not ready for implementation" (line 752). Ungated open questions are where two units diverge silently: one story anchors temporal evidence on event position, another on projection version, and both pass review because neither had to consult a register.

### F10 — LOW — Technology-currency flags for the web-checking reviewer (Rubric 4)

**Evidence:** Aspire is stated three ways — templates "expose Aspire `13.0`", "current public Aspire documentation shows newer Aspire `13.3`" (line 699), "Sibling modules pin Aspire mainly around 13.2.x and Dapr client packages at 1.17.7" (line 936), "Aspire 13.3 upgrade should be evaluated separately" (line 738) — all May-14-era observations never refreshed by any overlay. Also unverified-current in-document: .NET SDK pin `10.0.302` (lines 699, 934), Dapr client `1.17.7`, and the testing stack's bare "xUnit v3" (line 965) with no version pin, despite a known umbrella-wide constraint that xunit must stay at stable 3.2.2 (4.0.0-pre breaks umbrella restore). The CPM/Hexalith.Builds baseline (line 1647) structurally mitigates but does not record the constraint.

**Why it matters:** These are stale-looking but declared-historical; the risk is a builder treating the "evaluate Aspire 13.3" deferred decision as still-live guidance about a version landscape that is over three months old. Needs external verification, not in-document repair beyond a dated note in the next overlay.

### F11 — LOW — Dead-in-place superseded rule text imposes supersession-computation burden (Rubric 7, discoverability)

**Evidence:** The SM-C2 rule appears three times: the rebaseline gate (`post P95 <= 1.05 x baseline P95` per row, lines 383–387 and Release Gates line 996), the v6 amendment replacing it for specific rows with an approved-cost ceiling and disclosure (lines 156–169), and v8 restoring the plain rule as "the sole current metric authority" (lines 269–275). Supersession IS declared each time — this is not a rubric-7 violation — but the v6 text stands unmarked in place, and only a reader who processes all three layers in order lands on the right rule. The same pattern applies to AppHost ownership (stated four times: v3 amendment, rebaseline, corrected tree, v9 inherited invariants — consistent, but quadruplicated).

**Why it matters:** Every dead-but-unmarked rule statement is a chance for a builder to cite the wrong layer. The v6 ceiling text is the most dangerous instance because it reads as a considered, numerically-specific engineering position — more persuasive prose than the one-paragraph v8 reversal.

### F12 — LOW — v4 append-only breach: disclosed, restored, and structurally prevented going forward (Rubric 7 — resolved, recorded for completeness)

**Evidence:** "The v4 amendment above was rewritten in place on 2026-07-29 after v5 declared it immutable. Its bytes are restored; the four substantive improvements are republished here and take effect from v6 forward" (lines 174–176). From V9 onward, overlay markers pin the predecessor block's exact byte length and SHA-256 (lines 1802, 2127, 2194, 2248).

**Why it matters:** Demonstrates the append-only discipline once failed under exactly the pressure it exists to resist — and that the remediation (hash-pinned markers) is the right mechanical answer. No action needed; retained as evidence the current marker scheme is load-bearing, which strengthens F1's case for extending it to the sidecar chain.

### F13 — LOW — Conditionally-advisory patterns are unenumerated (Rubric 2)

**Evidence:** "Every implementation pattern in this section must have at least one conformance test, analyzer, contract test, or architecture test. A pattern without an enforceable check is advisory only and must not be treated as complete" (lines 1328–1330). No inventory maps pattern→check, so which patterns are currently binding versus advisory is undecidable from the document.

**Why it matters:** The rule honestly prevents false confidence, but it creates a hidden state: two reviewers can disagree about whether a given naming or structure rule is "complete" without either being wrong. A generated pattern-to-check coverage table (the document already has the generator discipline for this) would close it.

### F14 — LOW — Minor identifier ambiguity: two "A5" action namespaces (Rubric 1)

**Evidence:** V10: "Epic 5 action A5 remains `open`" (line 2189). V12 owns "retrospective actions A1, A2, and A3" with "A4-A6 remain open" from the Epic 6 retrospective (lines 2278, 2352–2354). Two different A5s distinguished only by the phrase "Epic 5 action."

**Why it matters:** Low-stakes, but action IDs feed checkpoint gates (E6-REMEDIATION owns exactly A1–A3); an unqualified "A5" in a future record is ambiguous.

---

## Discoverability Assessment (requested extra dimension)

**Rating: LOW.** The document is not in lean AD-n spine format — no stable AD IDs, no per-decision Binds/Prevents/Rule blocks. To locate the currently binding rule on any topic, a builder must: (1) skip the frozen frontmatter, (2) read the rebaseline plus six embedded amendments (v3–v8), each scoped "supersedes only for X," (3) read four overlay blocks (V9–V12), each again narrowly scoped, (4) know — from outside the document — that V13/V14 sidecar authorities exist and check whether they touch the topic, and (5) mentally subtract dead-in-place text (F11). Worked example: the effective SM-C2 rule requires reconciling five layers (rebaseline gate → v6 ceiling → v8 restoration → v9 inherited-invariant restatement → confirming V10–V14 silence). The machine-validation side of discoverability is excellent (digest-pinned markers, deterministic projections, single-writer ownership, parity validators); the human side is unmitigated in-document. An `ARCHITECTURE-SPINE.md` exists in this sidecar folder (`architecture/architecture-Conversations-2026-08-02/ARCHITECTURE-SPINE.md`) but architecture.md never references it, so it cannot function as the discoverability remedy for a reader who starts — as the CLAUDE.md-directed workflow does — from the authority document. **Recommendation:** treat a current-rules AD-spine projection exactly like `epic-6-current-execution-view-v2` — generated, non-amending, digest-bound to the current overlay/sidecar chain, with one AD entry per topic carrying Binds/Prevents/Rule and the layer it derives from — and add the F1 forward-pointer rule so both the spine and the sidecars are reachable from the document itself.

---

## Checklist Scorecard

| # | Rubric item | Rating | Summary |
| --- | --- | --- | --- |
| 1 | Fixes real divergence points; two builders could not diverge | **Partial** | Pattern layer names its 15 conflict points and closes most with enforceable rules; open divergence remains on vocabulary canon (F3), ADR numbering (F6), FR→structure mapping (F7), minor ID ambiguity (F14). |
| 2 | Every binding rule enforceable and divergence-preventing | **Pass with exceptions** | Overwhelmingly enforceable (digest-pinned overlays, fail-closed hold, machine-checkable dependency scans); one prohibition unenforceable as written (F5), advisory-pattern state unenumerated (F13). |
| 3 | Nothing deferred/open lets two units diverge | **Partial** | OQ-1..5 register is exemplary (one row, binding decision, reopen condition); seven legacy open questions have no register, owner, or gate (F9); FR-16 deferral is airtight. |
| 4 | Named technology verified-current | **Flagged** | Aspire 13.0/13.2.x/13.3, SDK 10.0.302, Dapr 1.17.7, bare "xUnit v3" all May-era and unrefreshed (F10); V10's BMAD-6.10.1n46-frozen inventories presumptively self-invalidated by repo's 6.11.0 update (F8) — hand to web/repo-checking reviewer. |
| 5 | Ratifies brownfield conventions | **Pass** | CPM + Hexalith.Builds baseline, slnx, versionless PackageReference, submodule/nested-submodule rules, SDK-pin divergence declared with rationale, AppHost-vs-baseline tension surfaced honestly (lines 1960–1965); only the FR→structure mapping lags the ratified tree (F7). |
| 6 | Covers PRD capabilities (FR-1..20; preserved denominators) | **Partial** | Denominators (20/104/77/52 + 28 UX AC IDs, 13,289 LOC SM-1) restated consistently across every layer; FR-10..16 landed explicitly; FR-1..9 and FR-17..20 invisible in-document (F4). |
| 7 | No undeclared weakening/contradiction by later overlays | **Pass** | Supersession is declared every time, including the v6→v8 SM-C2 reversal; the one immutability breach (v4) is disclosed and structurally remediated (F12); residual cost is dead-in-place text (F11) and the sidecar continuation gap (F1). |
| 8 | Every owned dimension decided, deferred, or open | **Fail (operational envelope)** | Domain, runtime, evidence, performance, UX-preservation, and process dimensions are thoroughly dispositioned; environments, infra/provider strategy, and operations/runbooks are silent or silently dropped, and capacity unknowns have no waiver owner (F2, F9). |
| — | Discoverability (extra) | **Low** | 12 in-document layers + 2 unreferenced sidecars; no AD-spine, no current-rules projection of the architecture, no in-document pointer to the existing ARCHITECTURE-SPINE.md or to V13/V14 (F1, F11, Discoverability section). |

**Severity totals:** 0 critical · 2 high (F1, F2) · 7 medium (F3–F9) · 5 low (F10–F14).

**Gate recommendation:** No critical blockers; the hold makes the findings non-urgent today. Resolve F1 (sidecar forward-pointer rule) and F2 (operational-envelope disposition — decide, defer with owner, or formally delegate with a named counterpart artifact) before any hold-lift decision; fold F3–F7 and F9 into the next append-only overlay or the successor-story contracts that will consume them.
