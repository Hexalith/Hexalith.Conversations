# Architecture Validation Report — 2026-08-18

- **Target:** `_bmad-output/planning-artifacts/architecture.md` (via `ARCHITECTURE-SPINE.md` symlink; 2,359 lines, 150,505 bytes; v8 base + append-only overlays V9–V12)
- **Intent:** Validate (Reviewer Gate, no changes applied)
- **Lenses:** deterministic lint · good-spine rubric · reality-check (configured) · adversarial pairwise-construction (configured) · authority-chain integrity (ad-hoc)
- **Full reviews:** `reviews/validate-2026-08-18/` (four files, cited throughout as [rubric], [reality], [adversarial], [chain])

## Gate verdict

**FAIL — for use as the single current build authority.** The document's mechanical integrity is flawless — every overlay byte-count and SHA-256 digest reproduces exactly, the supersession chain v8→v12 is unbroken, and every named platform surface it commits to exists in the tracked submodules. But its own authority-discovery rule ("trust the last complete overlay marker") now resolves to stale V12 authority: the true current authority lives in V13/V14 sidecar JSONs and two approved 2026-08-18 proposals the document never names, one of which is untracked and one worktree-drifted from its bundle pin. On top of that, the adversarial lens constructed four letter-compliant builder pairs that diverge on currently-authorized or Wave-1 work. The ACTIVE implementation hold means nothing is burning today — but the critical set must close before IR-0 is rerun, because IR-0 binds the exact authority bundle these ambiguities live in.

## Update addendum — remediation applied (2026-08-18/19)

At the release owner's direction, the findings were rolled into an Update run. A **V13 overlay** (`conversations-architecture-2026-08-18-v13`, block 17,857 bytes, SHA-256 `c7d5c867…605a`) was appended to the spine, closing **C1** (authority-relocation and discovery rule with `sidecar-head` marker attributes, frontmatter-freeze restatement), **C2–C5** and **H5–H10** (divergence closures DC-1…DC-11), **H2/H4** (statusAsOf obligation, factual refresh), plus the chain-hygiene mediums (normative digest convention, sidecar-head pinning, marker attribute schema). The update ran its own three-lens gate: the first recheck caught that two closures contradicted shipped Story 6.2 contracts — DC-2 and DC-5 were rewritten to **ratify** `ProjectionTrustState` (the shipped unified 6-member trust/freshness vocabulary) and the `ProjectionFreshnessV1` composite anchor — and a final verification pass confirmed all eleven gate findings RESOLVED with exact-match against the shipped code (`reviews/update-2026-08-18/`). All V9–V12 digests and the v8 prefix pin reproduce unchanged. **Still owed (H1):** the single publication commit carrying the V13 overlay and both approved 2026-08-18 proposals, followed by companion regeneration and candidate rebind — the overlay itself now binds that step as a precondition of any IR-0 rerun. H3 (operational envelope) is now a declared open dimension owned by the release owner, due before hold-lift.

## Original gate findings (2026-08-18, pre-remediation)

| Lens | Verdict | C | H | M | L |
| --- | --- | --- | --- | --- | --- |
| Lint (deterministic) | PASS (0 findings) | – | – | – | – |
| Rubric walker | Sound with reservations | 0 | 2 | 7 | 5 |
| Reality-check | Pass with corrections | 0 | 2 | 3 | 4 |
| Adversarial | FAIL | 4 | 6 | 5 | 3 |
| Authority-chain | FAIL | 1 | 2 | 2 | 1 |
| **Consolidated (deduped)** | **FAIL** | **5** | **10** | **~13** | **~9** |

## Critical findings

### C1 — Authority discovery resolves to stale authority; V13/V14 are invisible from the document

The discovery rule (lines 1840–1842) terminates at the V12 overlay, but current authority has moved into `v13-current-proof-authority-v1.json` (A1 closed `done` 2026-08-09 via the current-proof route), `v14-current-candidate-authority-v1.json` (candidate rebound twice, now `151f9651`), and the two approved 2026-08-18 proposals — none of which the document names (grep: zero hits). A compliant reader concludes A1 is open and only satisfiable by done-tree reconstruction, the graph has 33 nodes, and the candidate is unrebound — all wrong. The frozen frontmatter (v8, execution-view v1) compounds this for any reader who starts at the top. This is the exact staleness failure the overlay machinery was built to prevent. *Sources: [chain F1 — CRITICAL], [rubric F1 — HIGH], [reality F5], [adversarial A15].*

**Repair:** append (never edit) a V13 pointer overlay after V12 carrying `supersedes=conversations-architecture-2026-08-04-v12`, `v12-block-bytes=6075`, `v12-block-sha256=3050b326c5759fc51bc0e800944b0a1a591ab1782f6798f12abfdc10051b5796`, an authority-relocation rule naming the sidecar files and the two proposals with their digests, and a restated frontmatter-freeze warning at the document's tail. Thereafter every new sidecar version is announced by an appended pointer amendment.

### C2 — SM-C2 "usable comparable evidence" vs the frozen envelope: Epic 12 unsatisfiable or two-ways satisfiable

The immutable v6 record establishes that under the frozen envelope HP-CREATE/HP-APPEND showed dispersion two orders of magnitude wider than the 5% threshold (lines 165–168); v8 then demands usable comparable evidence under that *identical frozen envelope* for all four rows (lines 268–275). One builder reads Epic 12's signal-work mandate as license to change repetition policy/tooling; another reads envelope identity + "changing the target requires a PRD-level proposal" as prohibiting exactly that, making the lane BLOCKED. Either Epic 12 ships evidence RG-15 must reject as envelope-drifted, or the release chain dead-ends at a gate no authorized work may unblock. *Source: [adversarial A1].*

**Repair:** define "usable" numerically; partition the envelope into frozen vs versioned members (repetition count, sample volume, tool version) revisable only as a named pair-re-measured envelope revision; state that an envelope revision is not a target change.

### C3 — Trust/freshness/hydration vocabulary ordered as "one shared enum" but never pinned

Line 1129 commands one shared enum across API/UI/diagnostics/evidence, yet the trust list is only ever "candidate" (lines 554, 918), the freshness list only "such as" (1211, 1277–1284), the two share just 4 of 10 members, and the hydration category (1265) has no members anywhere in 2,359 lines. A one-enum Contracts builder and a two-enum Server/HP-OPEN builder are both letter-compliant and wire-incompatible — inside Epic 12's comparability window. *Sources: [adversarial A2 — CRITICAL, A12], [rubric F3].*

**Repair:** publish the canonical vocabulary as a versioned contract table (one enum per category, exact members, trust↔freshness structural relation, hydration condition→state mapping) before hold-lift; downgrade the candidate lists to superseded.

### C4 — Record-generator declared-dirt policy vs promotion-gate untracked-cleanliness on the same candidate

The v6 record contract admits declared TRX inputs as legitimate dirt (lines 179–185) without constraining where they live; the Story 6.7 promotion gate blocks any untracked file in a changed-gitlink submodule (lines 423–425) with no carve-out. A candidate whose TRX inputs or build byproducts land under `references/` is simultaneously generator-valid and gate-blocked — and since the record embeds the gate verdict verbatim, the generator's own output turns itself red. This is the operational failure mode already observed in this repository's promotion-gate history. *Source: [adversarial A3].*

**Repair:** pin allowed dirt locations to root-owned paths only; state that promotion-gate cleanliness dominates the record contract; require submodule-touching test lanes to redirect outputs before record generation.

### C5 — Graph-parity has no composition rule and "exactly 33 nodes" underdetermines the set — reachable today

V9 requires the validator to reject "any semantic difference" against the architecture overlay (lines 1900–1903), but the immutable v9 mermaid lacks the v11/v12 nodes and v12 removed an edge it contains; V11 legitimized exactly one graph-only reconciliation while v12's E6-REMEDIATION is itself a story-less graph node. A validator comparing against each immutable overlay always fails; one comparing against an (undefined) overlay-composed effective graph passes. Separately, two letter-compliant generators can emit different 33-node sets. This sits in the E6-REMEDIATION/preflight lane that is authorized and running now. *Source: [adversarial A4].*

**Repair:** state the composition rule (v9 canonical graph amended by each later overlay's enumerated deltas; immutable blocks are provenance, not comparison targets) and enumerate the exact 33 node IDs in the v12 sidecar instead of pinning a count.

## High findings

- **H1 — Approved authority uncommitted/drifted.** `sprint-change-proposal-2026-08-18-readiness-gate-ac10-ac11.md` is APPROVED yet **untracked** (unrecoverable if lost); `…-e6-remediation-a3.md` is worktree-modified (`cbc2eb2f…`) against its bundle pin as `canonical-authority-input` (`fbac2624…` = committed blob) — candidate-source drift by the document's own V9/V10 rules. Commit both (commitlint-validated), then regenerate and rebind the candidate-bound companions. [chain F2]
- **H2 — Misleading entry points.** The byte-pinned frontmatter still claims v8 / execution-view v1 (v2 exists, committed, bundle-pinned), and the regenerated v12 sidecar says A1 `open` while v13 and sprint-status say `done` — two live sidecars disagree on one action. The only freeze warning sits ~1,800 lines in. Fix via the C1 pointer overlay + regenerating the v12 sidecar with a `statusAsOf` marker. [chain F3]
- **H3 — Operational/environmental envelope silent.** No environment topology, no module-owned infra/provider strategy (projection-store consistency, secrets, backup/restore vs legal hold, residency), and runbooks (`projection-rebuild`, `governance-verify`, `tenant-isolation-denial`) silently dropped from the target tree; delegation to "platform deployment" names no counterpart artifact; capacity unknowns have no waiver owner. Decide, defer with owner, or formally delegate before hold-lift. [rubric F2]
- **H4 — Stale version premises in still-binding text.** "Sibling modules pin Aspire 13.2.x / Dapr 1.17.7" and "evaluate Aspire 13.3" — reality is Aspire **13.4.6** (shared props *and* this repo's AppHost SDK line) and Dapr **1.18.5**; current stable is 13.4, 13.5 in development. The list was hand-refreshed for the SDK number only, so stale values read as re-verified. [reality F1, F2]
- **H5 — Temporal evidence anchor open but load-bearing.** Line 604's open question must be answered *now* by both the Epic 12 HP-OPEN envelope and Epic 13 proof-v3 replay evidence, and the two natural picks (event position vs opaque projection version) fail each other's conformance checks. Pin a composite anchor contract. [adversarial A5]
- **H6 — Audit-unavailable boundary undecided + orphaned ADR backlog.** Whether non-governance commands proceed during audit degradation without an ADR is two-valued, and ADR-001..010 are "required before dependent stories" yet no Epic 7–15 story owns authoring any of them. Pin the default; map ADR authorship into the obligation inventory. [adversarial A6, rubric F9]
- **H7 — Derived-key grammar unpinned.** "Keys must carry tenant scope" is the only shape rule beneath the fail-closed cross-key validation and the cross-tenant poison BDD scenario; two compliant key grammars make the validation vacuous or falsely red. Pin the exact grammar as a versioned contract. [adversarial A7]
- **H8 — "Strength digest" undefined; tier membership has two deciders.** Epic 9's migration inventory hinges on a term defined nowhere; count-based vs compile-surface-based digests give opposite verdicts on the same migration. Define the digest and name the single decider. [adversarial A8]
- **H9 — AppHost baseline conflict acknowledged, then abandoned.** The platform baseline forbids a module AppHost; Epic 12's post-measurement contract requires the harness to exist and run; v9 demands "a separate approved technical amendment" that no successor story owns and IR-0 never requires. Make its resolution an IR-0 entry criterion or Epic 12 hard-entry condition. [adversarial A9, rubric scorecard]
- **H10 — "Retiring the ceiling" is a two-valued obligation.** v6 assigns Story 6.11 a retirement obligation whose subject v8 dissolved without superseding the sentence; the zero-gap obligation inventory cannot be complete under both readings. One sentence in the next overlay closes it. [adversarial A10]

## Medium and low (rolled up)

**Medium (~13):** 13 of 20 initiative FRs have no in-document disposition [rubric F4] · one public-API prohibition literally unenforceable as written [rubric F5] · three colliding ADR numbering spaces (ADR-003/004 doubly assigned) [rubric F6, adversarial A14] · FR→structure mapping binds only the superseded May 14 tree [rubric F7] · V10's BMAD-6.10.1n46-frozen inventories presumptively self-invalidated by the repo's 6.11.0 move (the A3 proposal is remediating the fallout) [rubric F8] · seven legacy open questions have no disposition register [rubric F9] · still-binding text claims `ServiceDefaults` exists (removed) [reality F3] · authoritative test tree drifted both directions (`Client.Tests`/`Admin.Web.Tests` exist but absent; `Conformance.Server.Tests` listed but not built) [reality F4] · projection-handler duality + unowned dispatch ledger [adversarial A11] · hydration degraded-state mapping unassigned [adversarial A12] · UX denominator scan-vs-constant-28 deadlock [adversarial A13] · v14 chain head digest-pinned by nothing; v13-era bundle pins silently stale after regeneration [chain F4] · the digest byte-range convention exists nowhere in writing — it had to be determined experimentally [chain F5].

**Low (~9):** May-era tech-currency notes in superseded sections (Aspire 13.0/13.3 note, SDK 10.0.303/10.0.400 shipped 2026-08-11, xunit.v3 4.0.0 stable 2026-08-15 — umbrella stays 3.2.2 by known NU1608 constraint, Fluent UI rides a 5.0 RC) [rubric F10, reality F6–F9] · dead-in-place superseded rule text (the v6 SM-C2 ceiling prose is the most persuasive wrong layer) [rubric F11] · v4 append-only breach disclosed and structurally remediated — recorded as evidence the marker scheme is load-bearing [rubric F12] · advisory-vs-binding pattern state unenumerated [rubric F13] · two "A5" action namespaces [rubric F14] · 27-reader inventory vs alias conversion counting rule [adversarial A16] · legal-hold source mutation vs absolute replay equivalence [adversarial A17] · metadata-only ID co-emission [adversarial A18] · marker attribute schema inconsistent across overlays [chain F6].

## Verified strengths (for the record)

All V9–V12 predecessor digests and the v8 prefix pin reproduce byte-exactly; marker pairing well-formed; supersession chain unbroken; the authority bundle digest and every testable sidecar cross-pin verify [chain §1]. Every landing-zone platform surface exists in the tracked submodules; the AppHost test-only boundary is mechanically real (`IsPackable/IsPublishable=false`); `.slnx` + CPM + shared version baseline + ten root gitlinks all match [reality F10–F12]. The OQ-1..5 disposition register is exemplary; supersession is declared at every layer; the one historical immutability breach (v4) was disclosed and structurally prevented [rubric].

## Recommended remediation sequence

1. **Protect the approved authority** (H1): commit both 2026-08-18 proposals with commitlint-validated messages; do not capture the `references/Hexalith.FrontComposer` gitlink drift in that commit.
2. **Restore discovery** (C1, H2, chain F5/F6): append the V13 pointer overlay (authority-relocation rule, frontmatter-freeze restatement, normative digest-convention sentence, full marker attribute set), regenerate the v12 sidecar with `statusAsOf`, regenerate the bundle including v13/v14 as pinned artifacts, rebind the candidate.
3. **Close the divergence pairs before IR-0 rerun** (C2–C5, H5–H10): each has a small append-only closing rule specified in the adversarial review; none reopens immutable history.
4. **Before hold-lift** (H3, H4): disposition the operational envelope (decide / defer-with-owner / delegate-with-named-artifact) and refresh the version premises in one dated overlay note.
5. **Structural relief** (rubric discoverability): generate a current-rules AD-spine projection — like `epic-6-current-execution-view-v2`, non-amending and digest-bound — so builders stop computing 12-layer supersession by hand.

---

*Reviewer gate run 2026-08-18 under the bmad-architecture Validate intent. No changes were applied to the spine. Full reviews: `reviews/validate-2026-08-18/review-{rubric,reality-checked,adversarial,authority-chain}.md`.*
