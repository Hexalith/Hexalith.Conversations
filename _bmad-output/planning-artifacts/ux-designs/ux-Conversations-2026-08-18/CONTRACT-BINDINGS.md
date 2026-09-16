---
title: Conversations UX Contract Bindings
date: 2026-09-16
status: draft
currentDisposition: preserved-not-activated
activationAuthority: separate-approved-release-authority-required
componentsReviewed: 20
fullyBoundNow: 7
mixedCurrentAndOpen: 13
fullyTargetOnly: 0
fullyUnbound: 0
v15TargetSemanticSets: 2
unresolvedBindingGroups: 13
---

# Conversations UX Contract Bindings

This is a read-only binding analysis for the draft `DESIGN.md` and
`EXPERIENCE.md` spines. It records what the current public contracts can safely
render; it does not activate UI, prove runtime population, approve a capability,
or authorize client inference. “Available now” means the public type/property
exists in `Hexalith.Conversations.Contracts`, not that every current handler
conforms to Architecture V15.

## Binding Classes

| Class | Authority | UX treatment |
|---|---|---|
| Contract state | `ProjectionTrustState`: `Current`, `Stale`, `Rebuilding`, `Unavailable`, `Forbidden`, `Redacted` (`src/Hexalith.Conversations.Contracts/TrustStates/ProjectionTrustState.cs:12-57`). | Render the received state; do not mint synonyms as public states. |
| Contract reason | `ProjectionFreshnessReasonCode`: the 12 closed tokens from `current` through `metadata_write_failed` (`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessReasonCode.cs:12-92`). | A machine-safe cause paired with a state, not user-facing prose and not a visual role. |
| Domain-specific contract state | Citation availability, audit readiness, verification, search-match source, release-gate status, waiver lifecycle, and buyer-acceptance status each have their own closed vocabulary (`src/Hexalith.Conversations.Contracts/Queries/ConversationSearchVocabularies.cs:12-197`; `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs:13-87`; `src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs:13-92`; `src/Hexalith.Conversations.Contracts/Conformance/BuyerAcceptanceVocabulary.cs:13-36`). | Preserve the vocabulary boundary; do not coerce these values into `ProjectionTrustState`. |
| Display label / explanation | Server-supplied `SafeLabel`, `SafeSummaryLabel`, `SafeDetailLabel`, `SafeAccessibilityLabel`, `SafeNextAction`, `BlockedReason`, or `WhyVisible`, where the named DTO supplies it. | Render only in its declared context. A local phrase such as “Restricted” is presentation copy, not a wire value. |
| Visual role | Inherited Fluent UI V5 component parameter or Fluent 2 semantic role. Exact trust-role mappings are unapproved (`DESIGN.md:87-104`; `references/Hexalith.AI.Tools/hexalith-ux-instructions.md:18-36`). | Supplemental styling only; never authorization or state. |
| Composite posture | `ConversationEvidenceTrustPostureV1` groups freshness, completeness, participant, citation, audit, verification, and command fields (`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:13-55`). | Present the dimensions independently. It has no overall state or precedence result. |

## Status Meanings

- **NOW** — every trust-bearing claim named in this row has a current public
  DTO/property binding. Activation is still separately held.
- **MIXED** — useful current fields exist, but at least one spine claim is
  target-only or open. Only the named current subset may render.
- **V15 TARGET** — Architecture V15 is normative design authority, but current
  runtime compliance is expressly not claimed.
- **OPEN / NON-RENDERABLE** — no approved public field or mapping exists. The
  client must omit the claim or show a generic source-owned unavailable result;
  it must not derive a replacement.

## Component Binding Matrix

All contract types below are owned by the Conversations module and published by
`Hexalith.Conversations.Contracts`. Parties retains ownership of personal data;
FrontComposer owns UI composition, not Conversations truth
(`../../architecture.md:2765-2810`).

| # | Trust-bearing component | Exact current public binding | Nullability / vocabulary | Safe display role | Status and unsupported claims |
|---:|---|---|---|---|---|
| 1 | Trust Fact | Context-specific only: `ProjectionFreshnessV1`; `ConversationSearchTrustPreviewV1`; `ConversationEvidenceEntryV1`; `ConversationCitationV1`; `ReleaseGateResultV1` | Each type validates required state/time/text; citation/audit references can be nullable. | Atomic label/value pair using only fields supplied by its enclosing DTO. | **MIXED.** There is no generic Trust Fact DTO and no universal `Source`, `Scope`, or `Confidence` property. `MatchSource` is only search-match provenance; `SafeSourcePosition` is only an approved position—not a generic source. |
| 2 | SafeReasonInline | `ConversationCommandAvailabilityV1.BlockedReason`; result-wrapper `SafeNextAction`; evidence `SafeSummaryLabel` / `SafeNextAction`; `ConversationError.SafeMessage` | Command reason is required non-null safe text; evidence/error labels can be nullable (`src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs:67-99`; `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs:62-90`; `src/Hexalith.Conversations.Contracts/Errors/ConversationError.cs:80-108`). | Inline explanation only beside the object that supplied it. | **MIXED.** Current command/evidence/error reasons are usable; there is no generic state-to-reason contract for every degraded, denied, or unavailable surface. |
| 3 | SafeReasonDetail | `ConversationEvidenceEntryV1.SafeDetailLabel`; authorized `ConversationCitationResult`, `ConversationAuditRecordResult`, or `ConversationTemporalDetailResult` safe outcome fields | Evidence detail label is nullable; result detail objects are nullable when hidden/unavailable, while state/reason/next action are required (`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs:67-90`; `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationResult.cs:15-65`; `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs:16-89`; `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailResult.cs:15-99`). | Authorized detail text, otherwise the wrapper's safe state and next action. | **MIXED.** There is no generic independently authorized SafeReasonDetail envelope or pending/downgrade state contract. |
| 4 | Redaction Placeholder | `ConversationRedactionAttributionV1.Placeholder`, `AttributionState`, safe labels/actions; `ConversationEvidenceEntryV1.VisibleText` and `RedactionAttribution`; `ConversationRedactionProjectionV1.Placeholder`, `TrustState` | Placeholder/state/labels are non-null in attribution; evidence attribution and visible text are nullable, with validation forcing canonical `[redacted]` when redacted (`src/Hexalith.Conversations.Contracts/Queries/ConversationRedactionAttributionV1.cs:15-64`; `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs:87-136`; `src/Hexalith.Conversations.Contracts/Projections/ConversationRedactionProjectionV1.cs:24-64`). | Source-supplied placeholder plus safe summary/accessibility label and next action. | **NOW.** Render only when supplied. The wider condition-to-display mapping remains gated by the vocabulary sidecar; the client does not create attribution. |
| 5 | Freshness Marker | `ProjectionFreshnessV1.ProjectionContractSchemaVersion`, `ProjectionCursor`, `LastAppliedEventPosition`, `LastAppliedEventTimestamp`, `ProjectionGeneratedAt`, `LagDuration`, `IsStale`, `FreshnessState`, `ReasonCode` | All except `LagDuration` are non-null/value-required; state and reason are closed (`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs:23-90`). | Raw state/reason with distinctly labeled replay timestamp, generation timestamp, lag, cursor, position, and schema version. | **MIXED.** No `Source` field exists. V15's exact source-condition mapping and generation-time behavior are **V15 TARGET**, not current-runtime proof (`../../architecture.md:3030-3109`). |
| 6 | Command Availability Marker | `ConversationCommandAvailabilityV1` fields: `ActionName`, `AvailabilityState`, `RequiredPermission`, `PreconditionState`, `RiskLevel`, `FreshnessRequirementState`, `AuditRequirement`, `BlockedReason`, `LastEvaluatedAt`, `ActionClassification`, `RequiresFreshServerRecheck` | All effective properties non-null; classification is closed to `read-only` / `governance-changing`; fresh recheck must be true. `ActionName`, `RequiredPermission`, `RiskLevel`, and `BlockedReason` are validated strings, not closed category vocabularies. `Current` availability requires current precondition/freshness and ready audit for governance-changing actions (`src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs:13-28,67-99,134-188`). | State, blocked reason, requirement summary, evaluation time, and action classification. | **NOW.** This is source-owned availability metadata, not proof of current caller authorization and not authority to expose a capability absent an approved slice. |
| 7 | Citation Control | `ConversationCitationResult` plus non-null `ConversationCitationV1` when visible; `SafeCopiedText`, `SafeLabel`, `SafeAccessibilityLabel`, `SafeNextAction`, state, citation/audit availability, cursor/version/time | Result `Citation` is nullable for hidden/unavailable/rebuilding. Visible citation safe-copy fields are required; `ActorPartyId` and `AuditEvidence` can be null (`src/Hexalith.Conversations.Contracts/Queries/ConversationCitationResult.cs:15-65`; `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationV1.cs:16-108`). | Copy only `SafeCopiedText`; label from source fields; result state/reason/next action when no citation is returned. | **NOW.** `CitationAvailability` does not itself authorize copy; the authorized citation result does. “Missing citation blocks evidence acceptance” has no single acceptance-gate field and remains an open cross-contract rule. |
| 8 | Participant Identity Marker | `ConversationParticipantProjectionV1.ParticipantPartyId`, `ParticipantType`, `ParticipantRole`, optional `OccurredAt`; matching `PartyReferenceHydrationV1.PartyId`, `HydrationState`, `Resolved`, `SafeLabel`, `SafeToken`, `SafeStatus`; posture `ParticipantResolutionState` | Participant identity/type/role and hydration state/labels are non-null; participant time is nullable. Hydration state uses closed `ProjectionTrustState` (`src/Hexalith.Conversations.Contracts/Projections/ConversationParticipantProjectionV1.cs:18-42`; `src/Hexalith.Conversations.Contracts/Queries/PartyReferenceHydrationV1.cs:14-45`). | Stable Party ID where authorized, source-supplied safe label/status/token, and received hydration/resolution state. | **MIXED.** No hydration `Source` property exists. Parties owns personal data. Exact hydration condition-to-state/display mapping is open pending `conversations-vocabulary-v1.json` (`../../architecture.md:2475-2494,3189`). |
| 9 | Tenant-scoped Find Pane | `ConversationListFilterV1`; `ConversationListResult.Conversations`, `Page`, state/reason, `SafeNextAction`; `ConversationPageMetadata.ReturnedCount`, nullable `ContinuationCursor` | Filter fields are individually nullable; result/page/state/reason/action are non-null; returned count covers accessible rows only (`src/Hexalith.Conversations.Contracts/Queries/ConversationListFilterV1.cs:11-109`; `src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs:15-75`; `src/Hexalith.Conversations.Contracts/Queries/ConversationPageMetadata.cs:8-31`). | Authorized filters, visible-row count, continuation, result state/reason, and source-safe next action. | **MIXED.** No public facets, autocomplete, arbitrary ordering, total inaccessible count, or timing-disclosure DTO exists. V15 true-empty versus rebuilding/unavailable mapping is target-only until runtime convergence. |
| 10 | Trust Preview Result Row | `ConversationSummaryV1` plus `SearchTrustPreview`; preview fields `FreshnessState`, `FreshnessReasonCode`, `RedactionState`, `ParticipantResolutionState`, `CitationAvailability`, `AuditReadiness`, `VerificationState`, `MatchSource`, `WhyVisible` | Summary IDs/freshness/preview non-null; optional business/hydration fields as declared. Preview fields and `WhyVisible` are non-null closed/safe values (`src/Hexalith.Conversations.Contracts/Queries/ConversationSummaryV1.cs:15-78`; `src/Hexalith.Conversations.Contracts/Queries/ConversationSearchTrustPreviewV1.cs:11-60`). | Compact per-dimension preview, exact search match source, and source-supplied why-visible explanation. | **NOW.** `MatchSource` is the only current field legitimately called a search result “source”; it is not projection provenance. |
| 11 | Governed Record Header | `ConversationDetailResult` plus visible `ConversationDetailsV1`: tenant/conversation identity, `Freshness`, `TrustPosture`; posture `TemporalCursor` and `CommandEligibility` | `Details` nullable for hidden/unavailable; wrapper state/reason/next action non-null. Visible details and defaulted posture/list fields are non-null (`src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs:15-71`; `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs:17-74`; `src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:16-55`). | Only show identity/context when `Details` exists; otherwise wrapper safe outcome. Show posture dimensions, not a calculated overall state. | **NOW.** Header ordering is UX composition; truth remains in the current result/details contracts. |
| 12 | Trust Posture Strip | `ConversationEvidenceTrustPostureV1.Freshness`, `EvidenceCompletenessState`, `ParticipantResolutionState`, `CitationAvailability`, `AuditReadiness`, `VerificationState`, `CommandEligibility` | Every dimension is effective non-null; absent command list becomes one explicit unavailable read-command entry (`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:16-27,35-55,95-120`). | Fixed-order, separately labeled dimensions. | **MIXED.** No `OverallState`, `RollupState`, precedence result, or aggregate reason exists. A “conservative winner” is **OPEN / NON-RENDERABLE** until the server publishes an owned aggregate or an approved mapping. |
| 13 | Evidence Completeness Indicator | `ConversationEvidenceTrustPostureV1.EvidenceCompletenessState`; temporal-only `ConversationTemporalConfidenceV1.IsComplete`, `ConfidenceState`, `ReasonCode`, `FreshnessSummary` | Posture completeness is a non-null closed `ProjectionTrustState`; temporal `IsComplete` is non-null bool within temporal reconstruction only (`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:16-40`; `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalConfidenceV1.cs:20-50`). | Render the raw posture state; temporal completeness only inside a temporal result. | **MIXED.** `complete-within-permissions/index`, `incomplete-withheld`, and `unknown-metadata` are not public completeness values or fields. They are **OPEN / NON-RENDERABLE** until a source-owned mapping/label exists. |
| 14 | Evidence Timeline Entry | `ConversationEvidenceEntryV1`: identity/kind/actor/time, trust, citation, audit, degraded state, optional visible content/targets/evidence/safe labels/redaction attribution/source position | Core state/time fields non-null; content, actor, references, audit evidence, labels, attribution, and source position are explicitly nullable (`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceEntryV1.cs:15-90`). | Chronological entry using supplied visible text or redaction placeholder; optional actions/labels appear only when present. | **NOW.** Do not fall back to `ConversationTimelineMessageProjectionV1.Text` for governed evidence when the evidence entry withholds content. |
| 15 | Safe State Message | Current result wrappers expose state, reason, nullable content, and required `SafeNextAction`: list, detail, citation, audit, temporal. `ConversationError.SafeMessage` is optional. | Wrapper state/reason/action are non-null; content is nullable in non-visible outcomes (`src/Hexalith.Conversations.Contracts/Queries/ConversationListResult.cs:15-75`; `src/Hexalith.Conversations.Contracts/Queries/ConversationDetailResult.cs:15-71`; `src/Hexalith.Conversations.Contracts/Queries/ConversationCitationResult.cs:15-65`; `src/Hexalith.Conversations.Contracts/Queries/ConversationAuditRecordResult.cs:16-89`; `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailResult.cs:15-99`). | State/reason plus the exact source-supplied safe next action/message. | **MIXED.** Loading is a client lifecycle state, not a contract trust state. Generic display labels and the unclosed reason-to-label map remain open; `Restricted` may only be mapped presentation copy for `Forbidden` / `forbidden`. |
| 16 | Evidence Detail Drawer | Use the specific authorized result: `ConversationCitationResult`, `ConversationAuditRecordResult`, `ConversationTemporalDetailResult`, or response-scoped hydration fields on `ConversationDetailsV1`; never a generic combined model | Each result's details are nullable and suppressed for hidden/unavailable states. Hydration lists may be empty and individual project/folder hydration may be null (`src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs:108-126`). | Generic shell until a specific result returns; then render only that result's safe DTO fields. | **MIXED.** There is no generic drawer authorization/pending/downgrade DTO or permission token. Reauthorization behavior and clear-on-downgrade remain activation design/implementation obligations. |
| 17 | Command Gate | `ConversationEvidenceTrustPostureV1.CommandEligibility[]` of `ConversationCommandAvailabilityV1`; use each exact action/state/requirements/reason/time/classification/recheck flag | List is non-null; missing data defaults fail-closed. Each item enforces non-null fields and fresh recheck (`src/Hexalith.Conversations.Contracts/Queries/ConversationEvidenceTrustPostureV1.cs:54-55,95-120`; `src/Hexalith.Conversations.Contracts/Queries/ConversationCommandAvailabilityV1.cs:13-24,147-188`). | One source-owned action row per item. Never infer availability from visibility, role name, or UI state. | **NOW.** The binding exists, but the release capability matrix and accessible blocked-control mechanism remain unapproved; contract presence does not activate a command. |
| 18 | Permission-gated Forensic Timeline Mode | Authorized temporal path: `ConversationTemporalDetailResult`, `ConversationTemporalDetailsV1`, `ConversationTemporalAnchorV1`, `ConversationTemporalConfidenceV1`; authorized audit detail via `ConversationAuditRecordResult` | Result details/anchor can be null when hidden/unavailable; anchor subfields are kind-dependent nullable values; confidence/state/reason/action are required (`src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalAnchorV1.cs:24-100`; `src/Hexalith.Conversations.Contracts/Queries/ConversationTemporalDetailsV1.cs:15-71`). | Exact temporal/audit fields only after their specific result is visible; show confidence and safe next action. | **MIXED.** No public “forensic mode permission,” mode state, or audit-log confirmation field exists. Mode authorization/audit gating is **OPEN / NON-RENDERABLE**; V15 does not activate it. |
| 19 | Evidence Acceptance Summary | `BuyerAcceptanceEvidenceSummaryV1` / step results for synthetic scenario status, runner, time, ownership, safe summary, requirement/evidence handles; `ReleaseConformanceArtifactV1` / `ReleaseGateResultV1` for signer-or-runner, manifest, evidence links, gate status/time; `CapabilityReleaseScopeEntryV1` for scope/owner/review | Core summary/artifact fields non-null; evidence/verification lists may be empty by their validators; capability refs/waiver/expiry can be nullable (`src/Hexalith.Conversations.Contracts/Governance/BuyerAcceptanceDemoContracts.cs:343-456`; `src/Hexalith.Conversations.Contracts/Conformance/ReleaseConformanceArtifactV1.cs:31-117`; `src/Hexalith.Conversations.Contracts/Conformance/ReleaseGateStatus.cs:176-218`; `src/Hexalith.Conversations.Contracts/Conformance/CapabilityReleaseScopeEntryV1.cs:21-63`). | Keep buyer-demo, release-gate, and capability-scope vocabularies visibly distinct; use their source-owned status and evidence fields. | **MIXED.** No single DTO contains scope, outcome, signer, timestamp, projection freshness, and evidence sufficiency. `RunnerId` is not automatically a signer. UI acceptance mutation and the FR-20 preservation-proof gap remain open/held. |
| 20 | Waiver and Blocker Summary | `ReleaseWaiverV1`: owner/approver, affected requirement/gate/stories, blocker, risk, compensating control, expiry, buyer impact/status, evidence links, review date, lifecycle; `BuyerPartialAcceptanceItemV1`; `CapabilityReleaseScopeEntryV1.Scope` for `deferred` | Waiver core fields non-null; approver/gate/buyer status are nullable under documented validation; lifecycle closed to active/expired/rejected/superseded. Capability scope is a separate closed vocabulary (`src/Hexalith.Conversations.Contracts/Conformance/ReleaseWaiverV1.cs:94-209`; `src/Hexalith.Conversations.Contracts/Conformance/BuyerPartialAcceptanceItemV1.cs:8-39`; `src/Hexalith.Conversations.Contracts/Conformance/CapabilityReleaseScopeVocabulary.cs:13-60`). | Show each source type and vocabulary separately; never collapse waiver lifecycle, blocker boolean, partial acceptance, and capability scope into one state. | **MIXED.** “Deferred” comes only from capability scope, not waiver lifecycle. No generic `DowngradeTrigger` field exists. Recording/approving a waiver is not activated by these read contracts. |

## Exact State and Reason Disposition

| Binding | Availability | Constraint |
|---|---|---|
| Closed state values and closed reason-code values | **NOW** | The public values are shipped; consumers may parse and render them but may not widen or rename them (`../../architecture.md:2475-2494`). |
| `Current/current` and `Stale/stale_threshold_exceeded` members | **NOW as contract vocabulary** | The DTO enforces the exact current/non-stale invariant and stale flag, but does not enforce `stale_threshold_exceeded` as the only reason for `Stale`. This proves shape/local validation, not full handler population (`src/Hexalith.Conversations.Contracts/Projections/ProjectionFreshnessV1.cs:83-90,138-173`). |
| V15 AD-7 conditions mapped to `Current/current`, `Rebuilding/rebuilding`, `Rebuilding/gap_detected`, `Unavailable/unavailable`, `Unavailable/metadata_contradictory`, `Forbidden/forbidden` | **V15 TARGET** | Normative future behavior; current handlers/decoder/models do not claim compliance (`../../architecture.md:3030-3043,3107-3109`). |
| V15 AD-8 meanings for replay anchor, generation completion, lag, and contradictory time | **V15 TARGET semantics over NOW fields** | The fields exist, but activation/runtime evidence must prove the V15 behavior (`../../architecture.md:3067-3109`). |
| Remaining reason-to-display-label combinations, including out-of-order, mixed-generation, poison-event, and metadata-write-failed | **OPEN / NON-RENDERABLE as bespoke labels** | Render the source state/reason with a generic safe unavailable treatment until an approved mapping supplies copy; never infer a more specific posture. |
| Redaction/hydration condition-to-display mapping and per-surface indistinguishability | **OPEN / NON-RENDERABLE beyond supplied safe DTO fields** | Blocked on `conversations-vocabulary-v1.json`; existing public values may not be widened to close it (`../../architecture.md:2486-2494,3189`). |

## Unsupported or Unresolved Binding Groups

1. **Generic source/provenance:** no universal `Source` field backs Trust Fact or
   Freshness Marker. `ConversationSearchTrustPreviewV1.MatchSource` is only why a
   search match is visible; `SafeSourcePosition` is a position, not provenance.
2. **Generic scope/confidence:** no universal Trust Fact scope/confidence shape
   exists; use only context-specific DTO fields.
3. **Completeness categories:** the contract supplies
   `EvidenceCompletenessState: ProjectionTrustState`, not
   `complete-within-permissions/index`, `incomplete-withheld`, or
   `unknown-metadata`.
4. **Hydration source/mapping:** hydration DTOs supply safe label/status/token
   and state, but no source field; condition/display mapping awaits the vocabulary
   sidecar.
5. **Composite rollup:** the posture DTO has independent dimensions and no
   overall state, winner, reason, or precedence algorithm.
6. **Audit health:** `AuditReadiness` and nullable `AuditEvidence` are current;
   generic audit-sink health, pull/push semantics, and a cross-surface audit
   posture are not (`../../prds/prd-Conversations-2026-06-02/addendum.md:33-35`).
7. **Citation acceptance:** citation availability and an authorized citation DTO
   are current; no single field says “missing citation blocks evidence
   acceptance.”
8. **Command categories/capabilities:** exact availability fields and the two
   action classifications are current; the action catalog, capability matrix,
   caller authorization result, and blocked-control mechanics are not activated.
9. **State/reason/display/visual mapping:** wire states and reasons are not
   display labels or Fluent roles; exact labels and color/icon roles remain open.
10. **Search extensions:** facets, autocomplete, arbitrary ordering, global
    totals, and material timing disclosure have no current public DTO.
11. **Generic detail/forensic gate:** specific citation/audit/temporal result
    envelopes exist; a generic drawer or forensic-mode permission/audit state
    does not.
12. **Acceptance aggregation/mutation:** current evidence types do not form one
    projection-freshness-aware acceptance DTO, equate runner with signer, or
    authorize recording accept/reject/waiver/blocker outcomes.
13. **Downgrade trigger:** waiver, partial-acceptance, and capability-scope DTOs
    have dates/statuses/refs, but no generic `DowngradeTrigger` field.

## Ownership and Activation Guard

- Conversations owns the public DTOs, states, projections, and safe result
  wrappers; upstream modules retain ownership of their source data, including
  Parties personal data (`../../architecture.md:2798-2810`).
- FrontComposer owns composition. Optional Conversations operator UI belongs at
  the `Admin.Web` composition boundary and remains preserved, not activated
  (`../../architecture.md:3175-3179`).
- AD-7/AD-8 are target contracts, current Epic 16 carriers are insufficient,
  and implementation remains held (`../../architecture.md:2733-2739,3107-3109`).
- The current PRD validation cannot prove the full freshness/audit-pairing
  preservation denominator, so these bindings are not acceptance evidence
  (`../../prds/prd-Conversations-2026-06-02/validation-report.md:34-38,120-122`).
- This record does not supersede the V4 UX disposition map, either spine, the
  architecture, or any public contract. Any unsupported field remains
  non-renderable until an approved additive contract and evidence make it
  source-owned.
