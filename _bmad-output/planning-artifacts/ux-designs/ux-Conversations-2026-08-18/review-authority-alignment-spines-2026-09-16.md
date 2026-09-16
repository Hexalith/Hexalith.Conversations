---
title: Authority Alignment Review — Conversations UX Spines
date: 2026-09-16
scope:
  - DESIGN.md
  - EXPERIENCE.md
verdict: revise
counts:
  critical: 0
  high: 4
  medium: 4
  low: 0
---

# Authority Alignment Review — New Spine Pair

## Overall Verdict

**REVISE — the pair is a useful preservation draft, but it does not yet pass the
authority-alignment gate and must not be treated as an implementation contract.**
The spines correctly retain `preserved-not-activated`, keep Architecture V15's
hold visible, cover all nine preserved product actors, separately disposition
the three refactor actors, retain WCAG 2.1 AA, and inherit FrontComposer plus
Blazor Fluent UI V5 (`DESIGN.md:4-15,77-104`; `EXPERIENCE.md:3-14,19-29,143-156,176-261`).

The blocking alignment defects are narrower: the declared precedence elevates
the currently rejected PRD, V15 target mappings read as delivered contract
behavior, the trust strip requires an unowned client-side rollup, and several
component claims exceed the named DTO fields. Current reconciliation already
classifies the pair as a draft distillate rather than an implementation contract
(`reconcile-ux-validation.md:70-72`).

## Authority Set Checked

- Current disposition: V4 requirement map
  (`../../ux-requirement-map.md:2-23,27-80`).
- Preserved UX detail and visual provenance: legacy specification and direction
  artifact (`../../ux-design-specification.md:44-54,678-700`;
  `../../ux-design-directions.html:3-18`).
- Product/refactor inputs: canonical PRD, addendum, epics, and current PRD
  validation (`../../prds/prd-Conversations-2026-06-02/prd.md:88-97,443-451`;
  `../../prds/prd-Conversations-2026-06-02/addendum.md:25-35`;
  `../../prds/prd-Conversations-2026-06-02/epics.md:2542-2597,4359-4477`;
  `../../prds/prd-Conversations-2026-06-02/validation-report.md:6-12,34-38,72-76,120-122`).
- Technical authority: Architecture V15 (`../../architecture.md:2702-2739,2765-2810,3030-3109,3137-3202`).
- UI baseline: `references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-51`.
- Update decisions: `reconcile-prd.md:10-82`,
  `reconcile-architecture.md:19-149`, `reconcile-ux-validation.md:13-72`, and
  `.memlog.md:12-19`; PRD update signal at
  `../../prds/prd-Conversations-2026-06-02/.memlog.md:42`.
- Named public contracts were checked for field/value accuracy:
  `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs:11-32`,
  `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:13-27`,
  `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs:10-24,168-185`,
  `src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs:12-57`, and
  `src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs:12-92`.

## High Findings

### H1 — The precedence statement elevates a PRD that current validation rejects

**Evidence.** `EXPERIENCE.md:19` says “the PRD controls preserved product
obligations,” while the PRD validation says the package is unsafe as current
release, architecture, or story-generation authority and recommends no hold
lift (`../../prds/prd-Conversations-2026-06-02/validation-report.md:8-12,120-122`).
The accepted reconciliation limits the PRD to preservation and conflict
discovery until revision and revalidation (`reconcile-prd.md:10-15,25-29,44-55`).
The source arrays in both spines also omit the canonical epics, current PRD
validation, all three reconciliation records, and the memlog decisions that
actually qualify how their listed sources may be used (`DESIGN.md:8-15`;
`EXPERIENCE.md:7-14`; `.memlog.md:12-19`). The V4 map, not the PRD, controls
current UX disposition (`../../ux-requirement-map.md:14-23`).

**Impact.** A downstream reader can incorrectly treat rejected PRD material or
reviewer proposals as current product authority and cannot reproduce the
spines' actual source-selection logic.

**Required fix.** Replace the precedence statement with an explicit ordered
authority rule: V4 map for disposition; V15 for technical state/ownership;
legacy specification for preserved UX detail; PRD/addendum/epics for preserved
product context only, qualified by current validation and reconciliation; and
no activation without separate approved release authority. Add the omitted
qualifying sources to both source manifests. State that the draft spines do not
supersede an accepted source until a binding/acceptance record exists.

### H2 — V15 target mappings are presented as current delivered behavior

**Evidence.** `EXPERIENCE.md:94-111` says Architecture V15 and “the public
contract define” the table without marking it as a target-state contract.
Architecture says AD-7/AD-8 are target contracts, current handlers/decoder/
persistence models do not claim compliance, and implementation remains held
(`../../architecture.md:3030-3109`). It also says the existing Epic 16 carriers
are insufficient and cannot enter implementation or review before successor
carriers and validators exist (`../../architecture.md:2733-2739`). The current
code does contain the closed state/reason value sets, but enum presence is not
proof that V15's condition-to-state behavior is delivered
(`src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs:12-57`;
`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs:12-92`).

**Impact.** The table can be cited as evidence that the runtime already
produces the V15 combinations and time semantics, bypassing the exact
convergence work and tests V15 requires.

**Required fix.** Label the table and time rules “V15 normative target; not
current-runtime compliance evidence.” Separate shipped value vocabulary from
target condition mapping, and link activation to the successor Epic 16
contracts, implementation, and conformance evidence required by V15.

### H3 — The trust-posture rollup invents an unowned aggregation decision

**Evidence.** The Foundation forbids client inference of freshness,
completeness, citation, audit, participant, and command state
(`EXPERIENCE.md:27`), yet the Trust Posture Strip tells the client to “roll up”
those dimensions with conservative precedence (`EXPERIENCE.md:82`;
`DESIGN.md:153`). The preserved map requires deterministic conservative
precedence, but does not publish its algorithm or transfer authority to the
client (`../../ux-requirement-map.md:47,57,60`). The named posture DTO exposes
separate fields and no overall posture/precedence field
(`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:13-27`).

**Impact.** Different clients can synthesize different overall states, and a UI
can accidentally turn a locally calculated state into authorization or
reliance evidence.

**Required fix.** Until an approved source-owned aggregate contract and
precedence mapping exist, define the strip as a fixed-order presentation of the
individual server fields with no computed winner. If an aggregate is required,
add it to an owned public DTO plus conformance tests before binding the UX to it.

### H4 — Several component-to-contract claims have no exact field binding

**Evidence.** The Freshness Marker binds to `ProjectionFreshnessV1` but requires
a “source” that the DTO does not contain (`EXPERIENCE.md:75`;
`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs:11-32`).
The Evidence Completeness Indicator introduces
`complete-within-permissions/index`, `incomplete-withheld`, and
`unknown-metadata`, while the named posture DTO exposes only
`EvidenceCompletenessState: ProjectionTrustState` (`EXPERIENCE.md:83`;
`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:16-27`).
`DESIGN.md:142-154` additionally requires generic source and hydration-source
facts without identifying an owning DTO/property. Architecture still gates the
redaction/hydration condition mapping on the missing
`conversations-vocabulary-v1.json` and forbids widening the public values merely
to close the gap (`../../architecture.md:2475-2494,3189`;
`reconcile-architecture.md:134-143`).

**Impact.** Implementers must invent fields, join unrelated data, or translate
closed states into unapproved categories, making the UI contract neither
testable nor source-owned.

**Required fix.** Add an exact component binding table naming DTO, property,
nullability, safe display mapping, and owner for every trust-bearing value.
Remove unsupported fields/categories or mark them as open and non-renderable
until an approved additive contract and the vocabulary sidecar supply them.

## Medium Findings

### M1 — “Restricted” is not explicitly bound to the canonical forbidden state

**Evidence.** Voice guidance presents “Restricted” as a distinct access-denied
term (`EXPERIENCE.md:54-63`), while the state table uses
`Forbidden` / `forbidden` for the non-disclosing authorization boundary
(`EXPERIENCE.md:98-107`). Reconciliation permits legacy terms only as mapped
presentation copy and explicitly requires “Restricted” to map to
`Forbidden` / `forbidden` (`reconcile-architecture.md:123-128`). Architecture
also requires unauthorized and nonexistent tenants to remain indistinguishable
(`../../architecture.md:3036-3043`).

**Required fix.** State the exact mapping beside the microcopy table and require
the same non-disclosing copy/response for unauthorized and nonexistent cases.
Do not allow `Restricted` to become a seventh public trust state.

### M2 — Approval and governance capabilities read as selected before slicing

**Evidence.** The IA, component table, desktop description, and Julian/Helen
flow describe an Evidence Acceptance Review, governance forms, role-gated
approval, and recording accept/reject/waiver/blocker outcomes
(`EXPERIENCE.md:33-46,87-90,160-165,213-221`). The same spine leaves the
activation-time capability matrix unresolved (`EXPERIENCE.md:265-276`), and
reconciliation explicitly defers mutation/export capability slicing and all UI
activation (`reconcile-prd.md:57-75`). Architecture calls the operator UI
optional and preserved, not activated (`../../architecture.md:3175-3179,3197-3202`).

**Required fix.** Mark these as conceptual/read-only evidence-review outcomes.
Make recording an outcome, waiver, or other governance mutation conditional on
an approved capability matrix, source-owned command contract, authorization,
audit pairing, and fresh server recheck. Do not name desktop approval as a
currently selected capability.

### M3 — The current trust-preservation proof blocker is absent from the spines

**Evidence.** Current PRD validation says the FR-20 denominator does not
demonstrably contain projection-freshness or governance/audit-pairing coverage
(`../../prds/prd-Conversations-2026-06-02/validation-report.md:34-38`). The PRD
reconciliation therefore says not to claim those UX behaviors are proven
preserved (`reconcile-prd.md:48-53`). Nevertheless, the spines present freshness,
audit-pairing, and evidence acceptance as preserved flows/components without
carrying this proof gap into Foundation or Open Decisions
(`DESIGN.md:41-72,146-161`; `EXPERIENCE.md:69-90,213-231,263-276`).

**Required fix.** Add an explicit authority blocker: these behaviors remain
preservation obligations, but their current proof denominator is incomplete
and cannot support acceptance or hold lift until an approved trace artifact
closes the gap.

### M4 — The UI composition and module ownership boundary is incomplete

**Evidence.** The spines correctly inherit FrontComposer and Fluent UI V5
(`DESIGN.md:83-89,167-174`; `EXPERIENCE.md:21-29,46-48`), matching the UX
baseline (`references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-36,41-51`).
They do not carry V15's full boundary: the optional operator UI is in
`Admin.Web`, composed through FrontComposer; FrontComposer owns composition,
while Conversations owns domain contracts and projections
(`../../architecture.md:2765-2810,3175-3179`). The custom component inventory
otherwise has no explicit placement/ownership guardrail.

**Required fix.** Add a boundary statement, not an implementation assignment:
optional Conversations UI belongs at the `Admin.Web` composition boundary;
FrontComposer owns reusable composition; Conversations owns the domain-specific
contracts/projections; no domain-specific component is promoted into a shared
platform package without separate owner approval and reuse evidence.

## Confirmed Alignments

- **Activation/disposition:** both spines are drafts, explicitly
  `preserved-not-activated`, and do not claim a hold lift (`DESIGN.md:4-6,77`;
  `EXPERIENCE.md:3-5,19,29`).
- **Actors/journeys:** all nine preserved product actors are represented, while
  Nadia, Sam, and Priya are explicitly kept out of product UX
  (`EXPERIENCE.md:176-261`; `../../prds/prd-Conversations-2026-06-02/prd.md:443-451`).
- **Accessibility:** WCAG 2.1 AA remains the current floor; 2.2 is correctly
  deferred (`EXPERIENCE.md:143-156,267-269`; `reconcile-prd.md:54`).
- **Visual/system posture:** FrontComposer/Fluent V5 inheritance, the one-
  accordion rule, and non-normative HTML palette treatment align with the
  current baseline and legacy direction record (`DESIGN.md:83-104,120-126`;
  `../../ux-design-directions.html:10-18`;
  `references/Hexalith.AI.Tools/hexalith-ux-instructions.md:5-51`).
- **V15 semantics:** the listed AD-7 state pairs and AD-8 time definitions are
  accurate as normative target semantics; H2 concerns their delivery status,
  not the mapping content (`EXPERIENCE.md:98-111`;
  `../../architecture.md:3036-3043,3067-3109`).

## Gate Decision

Do not promote the spine pair from draft preservation distillate to accepted
UX authority until H1-H4 are corrected and re-reviewed. M1-M4 must be closed or
explicitly carried as activation blockers. This review changes no activation,
release, story, or implementation status.
