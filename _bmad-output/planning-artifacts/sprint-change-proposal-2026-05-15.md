---
date: 2026-05-15
project: Hexalith.Conversations
source_report: implementation-readiness-report-2026-05-15.md
workflow: bmad-correct-course
status: approval-pending
recommended_scope: moderate
---

# Sprint Change Proposal: Readiness Corrections Before Implementation Kickoff

## 1. Issue Summary

The implementation readiness assessment completed on 2026-05-15 found that the planning artifact set is complete and the PRD functional requirements are numerically covered, but the project is not yet ready for implementation kickoff.

The trigger is qualitative readiness risk, not missing product scope. Several stories either overclaim traceability, combine multiple proof obligations into one story, depend on unresolved architecture decisions without explicit prerequisites, or defer release evidence scaffolding too late in the plan.

Evidence from the readiness report:

- 0 missing documents.
- 0 missing PRD functional requirement coverage gaps.
- 0 critical epic-structure violations.
- 5 UX/architecture decision gaps.
- 6 major epic/story quality issues.
- 3 minor story quality concerns.

Primary corrections needed before kickoff:

1. Correct Story 1.1's FR coverage overclaim.
2. Split oversized proof stories 5.5 and 5.6.
3. Clarify or split Story 1.11.
4. Add early CI and release-evidence scaffolding.
5. Mark ADR-dependent stories explicitly.
6. Convert UX Leak Sentinel, responsive/accessibility, and disclosure-surface expectations into concrete test assets or conformance checklist items.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains the correct foundation epic, but it needs tighter traceability and clearer sequencing. Story 1.1 should remain a starter-template story, while Story 1.11 should be split so replay determinism, projection rebuild/freshness, and schema compatibility are independently completable.

Epic 2 remains structurally valid, but Stories 2.1, 2.4, 2.5, 2.6, and 2.7 depend on ADRs for audit pairing, redaction replay, temporal anchors, and retention/legal-hold semantics.

Epic 3 remains structurally valid, but Stories 3.4 and 3.8 depend on temporal-anchor, freshness-state, redaction-disclosure, FrontComposer trust-boundary, and responsive disclosure-surface decisions.

Epic 4 remains valid. No immediate structural change is required, but adopter-facing diagnostics and conformance fixture stories should reference the early evidence scaffold once added.

Epic 5 requires backlog reorganization. Story 5.5 must become three stories, and Story 5.6 must become two stories. The existing Stories 5.7 and 5.8 should be renumbered after the split.

Epic 6 remains valid. It should consume the evidence and conformance outputs produced by the split Epic 5 stories.

### Artifact Conflicts

PRD: No functional requirement changes are required. MVP scope remains achievable if the backlog is tightened and ADR prerequisites are explicit.

Architecture: Existing architecture already names the required ADR backlog and open decisions. The epic file must reflect those decisions as prerequisites so implementation does not proceed through local assumptions.

UX Design: Existing UX guidance is strong but needs implementation hooks. Leak Sentinel, responsive duplicate safety, accessibility-tree safety, clipboard safety, and trust-state fixture expectations need explicit story/conformance placement.

Implementation Artifacts: None exist for the affected Conversations stories yet, so no rollback is needed.

CI and Evidence: The release evidence path is currently introduced too late. A baseline CI and evidence scaffold should be established before feature stories start closing.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Classification: Moderate backlog reorganization.

Rationale: The product scope and epic structure are sound. The safest correction is to refine the story backlog, split oversized proof stories, add explicit decision prerequisites, and place a minimal evidence scaffold in Epic 1. No PRD rewrite, rollback, or strategic MVP reduction is needed.

Estimated effort: Medium.

Risk after correction: Low to medium. The remaining risk is ADR completion before trust-bearing stories begin.

Timeline impact: Small planning delay before kickoff, with expected reduction in implementation churn later.

## 4. Checklist Results

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | Done | Readiness assessment identified Story 1.1, Story 1.11, Story 5.5, Story 5.6, and ADR-dependent stories as trigger points. |
| 1.2 Core problem | Done | Misalignment between numeric FR coverage and implementation-ready story quality. |
| 1.3 Evidence | Done | Evidence captured in implementation-readiness-report-2026-05-15.md. |
| 2.1 Current epic impact | Done | Epic 1 needs traceability and story-splitting corrections. |
| 2.2 Epic-level changes | Done | Modify Epic 1 and Epic 5 stories; add early evidence scaffold. |
| 2.3 Remaining epics | Done | Epics 2 and 3 need ADR prerequisite notes; Epic 6 consumes evidence outputs. |
| 2.4 New/obsolete epics | N/A | No new epic or removed epic required. |
| 2.5 Order/priority | Done | Add evidence scaffold before closing early implementation stories. |
| 3.1 PRD conflicts | Done | No PRD requirement changes required. |
| 3.2 Architecture conflicts | Done | Existing ADR backlog must become explicit story prerequisites. |
| 3.3 UX conflicts | Done | UX trust/disclosure test expectations need explicit story placement. |
| 3.4 Other artifacts | Done | CI/evidence scaffold should be added early. |
| 4.1 Direct adjustment | Viable | Preferred option. |
| 4.2 Rollback | Not viable | No implemented stories require rollback. |
| 4.3 MVP review | Not viable | MVP remains achievable; issue is backlog readiness. |
| 4.4 Path selected | Done | Direct adjustment with moderate backlog reorganization. |
| 5.1 Issue summary | Done | Included in this proposal. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Recommended path | Done | Direct adjustment. |
| 5.4 MVP action plan | Done | MVP unchanged; update stories before kickoff. |
| 5.5 Handoff plan | Done | Product Owner and Developer update backlog; Architect owns ADR prerequisites. |
| 6.1 Checklist review | Done | Applicable checks completed. |
| 6.2 Proposal accuracy | Done | Based on report plus affected artifact inspection. |
| 6.3 User approval | Action-needed | Approval is pending. |
| 6.4 sprint-status.yaml | N/A | No sprint-status.yaml was found under _bmad-output. |
| 6.5 Handoff confirmation | Action-needed | Confirm after approval. |

## 5. Detailed Change Proposals

### Proposal A: Correct Story 1.1 FR Coverage

Story: 1.1 Set Up Initial Project from Starter Template

Section: Requirements Covered

OLD:

```markdown
**Requirements Covered:** FR1-FR41 foundation; Architecture starter-template requirement.
```

NEW:

```markdown
**Requirements Covered:** Architecture starter-template requirement; implementation foundation for FR1-FR41, with behavioral FR coverage delivered by Stories 1.2-1.11.
```

Rationale: Story 1.1 creates the scaffold and validates boundaries. It does not implement behavioral coverage for FR1-FR41.

### Proposal B: Add Early CI and Evidence Scaffold

Recommended placement: Add a new Story 1.2 after Story 1.1, then renumber current Stories 1.2-1.11.

Story: Establish Baseline CI and Release Evidence Scaffold

```markdown
### Story 1.2: Establish Baseline CI and Release Evidence Scaffold

As a release owner,
I want baseline CI, test artifact publication, and a placeholder conformance manifest,
So that early implementation stories close with evidence hooks instead of informal local test claims.

**Requirements Covered:** FR82-FR84 foundation; FR99 foundation; NFR1-NFR8 foundation; NFR59-NFR63 foundation.

**Acceptance Criteria:**

**Given** the scaffold builds locally
**When** the baseline CI workflow runs
**Then** restore, build, unit test, and formatting or analyzers execute without requiring production secrets, provider credentials, Dapr sidecars, Aspire runtime, or nested submodule initialization
**And** root-level submodule policy remains documented and enforced.

**Given** test results are produced
**When** the CI workflow completes
**Then** test results, build metadata, and failure diagnostics are published as machine-readable artifacts
**And** artifacts avoid conversation content, tenant data, provider payloads, Party personal data, and cross-tenant identifiers.

**Given** the first implementation stories will produce conformance evidence
**When** the placeholder conformance manifest is created
**Then** it defines stable locations, test identifiers, requirement mapping fields, pass criteria fields, waiver fields, environment metadata fields, and evidence links
**And** it marks unimplemented release gates as pending rather than pass.
```

Rationale: This creates the evidence rail before feature stories start closing, without pulling full Epic 5 conformance scope into Epic 1.

### Proposal C: Split Story 1.11

Current story: 1.11 Prove Replay, Schema Versioning, and Projection Rebuild Behavior

OLD scope:

```markdown
**Requirements Covered:** FR12, FR33-FR37, FR40, FR41.
```

NEW: Replace with three independently completable stories.

#### Story 1.x: Prove Deterministic Event Replay

```markdown
As a platform owner,
I want deterministic replay of tenant-scoped conversation event streams,
So that conversation records remain recoverable from EventStore history without provider-owned session authority.

**Requirements Covered:** FR12, FR33.

**Acceptance Criteria:**

**Given** a tenant-scoped conversation event stream exists
**When** aggregate state is rehydrated from ordered events
**Then** reconstructed state matches expected conversation identity, lifecycle, participants, messages, business references, provider correlation metadata, and attribution
**And** replay is deterministic for the same event history and contract version.

**Given** replay tests run
**When** duplicate events, tenant mismatch, provider correlation changes, and known historical event sequences are exercised
**Then** tests prove deterministic replay, tenant isolation, provider-correlation portability boundaries, and content-safe diagnostics.
```

#### Story 1.x: Prove Projection Rebuild and Freshness Semantics

```markdown
As a platform owner,
I want projection rebuild and freshness behavior to be explicit and testable,
So that read models never appear current or complete when they are stale, unavailable, rebuilding, or hidden by tenant isolation.

**Requirements Covered:** FR34-FR37.

**Decision Prerequisite:** ADR-003 and ADR-006 must define tenant projection durability, lag handling, projection freshness vocabulary, blocking states, and evidence metadata before implementation starts.

**Acceptance Criteria:**

**Given** v1 projections are deleted or marked rebuilding
**When** the rebuild process replays persisted events
**Then** it produces functionally equivalent summary and detail read models for the same tenant, conversation, event history, and contract version
**And** rebuild progress, stale state, unavailable state, hidden-by-tenant state, and completion are surfaced through the approved freshness metadata.

**Given** derived state disagrees with replayed EventStore state
**When** verification detects the disagreement
**Then** EventStore history wins, the derived artifact is marked stale, invalid, quarantined, or rebuilding, and content-safe diagnostics are emitted
**And** governed disclosure actions remain blocked unless an approved ADR permits action on stale state.
```

#### Story 1.x: Prove Schema Version Compatibility and Unsupported-Version Handling

```markdown
As a platform owner,
I want schema version compatibility and unsupported-version behavior to be explicit,
So that persisted and published conversation contracts evolve without silent data loss or unsafe downgrade behavior.

**Requirements Covered:** FR40, FR41.

**Decision Prerequisite:** ADR-005 must define event schema evolution, upcasting, projection compatibility, and unsupported-version behavior before implementation starts.

**Acceptance Criteria:**

**Given** old, mixed, additive, or unsupported event versions exist in a stream
**When** replay and projection handlers process them
**Then** supported versions replay through documented compatibility or upcaster behavior
**And** unsupported versions fail with typed documented errors rather than being skipped silently.

**Given** version compatibility tests run
**When** old event replay, mixed-version stream replay, unknown event handling, additive-change examples, and projection compatibility cases are exercised
**Then** tests prove version-aware behavior, safe diagnostics, tenant isolation, and release-evidence manifest output.
```

Rationale: Replay, projection rebuild/freshness, and schema compatibility have distinct decisions, fixtures, failure modes, and release evidence.

### Proposal D: Split Story 5.5

Current story: 5.5 Verify Tenant Isolation, Idempotency, and Redaction Replay

OLD:

```markdown
### Story 5.5: Verify Tenant Isolation, Idempotency, and Redaction Replay
...
**Requirements Covered:** FR87-FR89.
```

NEW: Replace with three stories.

#### Story 5.5: Verify Tenant Isolation Conformance

```markdown
As a platform owner,
I want release-gating tenant isolation conformance,
So that cross-tenant access is impossible by construction and tested adversarially before release.

**Requirements Covered:** FR87.

**Acceptance Criteria:**

**Given** the conformance suite runs tenant isolation tests
**When** positive and adversarial cases execute
**Then** it covers authorized access, cross-tenant ID guessing, stale tenant projection, unavailable tenant projection, disabled or deleted tenant, mixed-tenant rebuild attempts, poisoned projection events, malformed metadata, query enumeration, diagnostics, export, and admin/tool access
**And** any tenant isolation failure is an automatic release blocker unless explicitly waived through the named process.
```

#### Story 5.6: Verify Idempotent Command Conformance

```markdown
As a platform owner,
I want release-gating idempotent command conformance,
So that duplicate or retried commands produce stable outcomes without duplicate business effects.

**Requirements Covered:** FR88.

**Decision Prerequisite:** ADR-002 must define idempotency key scope, duplicate handling, non-equivalent duplicate behavior, and stable outcome semantics before implementation starts.

**Acceptance Criteria:**

**Given** the conformance suite runs idempotency tests
**When** duplicate equivalent commands, duplicate non-equivalent commands, reordered delivery, unknown client outcome retry, replayed delivery, and tenant-mismatched key reuse execute
**Then** it proves stable outcomes, conflict rejection, no duplicate business effects, no projection divergence, and content-safe diagnostics.
```

#### Story 5.7: Verify Redaction Replay Conformance

```markdown
As a platform owner,
I want release-gating redaction replay conformance,
So that redacted content never reappears through projections, logs, traces, errors, exports, accessibility output, clipboard payloads, caches, or derived indexes.

**Requirements Covered:** FR89.

**Decision Prerequisite:** ADR-007 and ADR-010 must define redaction replay, non-disclosure surfaces, retention, deletion, legal hold, export, and derived-index lifecycle behavior before implementation starts.

**Acceptance Criteria:**

**Given** the conformance suite runs redaction replay tests
**When** projections, temporal views, logs, traces, errors, exports, accessibility output, clipboard payloads, caches, screenshots, telemetry, and derived indexes are checked
**Then** redacted content does not reappear
**And** audit evidence remains citeable without exposing redacted values.
```

Rationale: These are three release-blocking suites with separate ownership, fixture design, and failure analysis.

### Proposal E: Split Story 5.6

Current story: 5.6 Prove Provider Portability and Event Schema Evolution

OLD:

```markdown
### Story 5.6: Prove Provider Portability and Event Schema Evolution
...
**Requirements Covered:** FR90, FR91.
```

NEW: Replace with two stories after the split Story 5.5 sequence.

#### Story 5.8: Prove Provider Portability

```markdown
As a platform owner,
I want provider portability proof,
So that conversation history remains recoverable without provider-owned session authority.

**Requirements Covered:** FR90.

**Acceptance Criteria:**

**Given** provider portability verification runs
**When** provider-owned correlation identifiers are stripped, changed, unavailable, migrated, duplicated, or inconsistent
**Then** conversation history remains recoverable from Conversations identity, stable references, and EventStore history
**And** provider IDs remain correlation metadata rather than durable source-of-truth identity.

**Given** portability verification covers contract-level behavior
**When** persistence semantics, pub/sub semantics, projection rebuild behavior, and observability evidence are evaluated
**Then** tenant isolation, idempotency, ordering tolerance, auditability, and replay determinism remain invariant across provider configuration differences.
```

#### Story 5.9: Prove Event Schema Evolution

```markdown
As a platform owner,
I want event schema evolution proof,
So that persisted and published conversation events can evolve safely across supported contract versions.

**Requirements Covered:** FR91.

**Decision Prerequisite:** ADR-005 must define event schema evolution, upcasting/projection compatibility, and unsupported-version behavior before implementation starts.

**Acceptance Criteria:**

**Given** event schema evolution verification runs
**When** old event versions, mixed-version streams, unsupported versions, and at least one worked additive-change example are processed
**Then** supported versions replay through documented compatibility behavior
**And** unsupported versions fail with typed documented errors.

**Given** release evidence is generated
**When** schema evolution checks complete
**Then** evidence maps compatibility outcomes to the conformance manifest and flags unsupported or missing-version behavior as release-gate failures unless explicitly waived.
```

Rationale: Provider portability and schema evolution are separate proof obligations with different fixtures and ADR dependencies.

### Proposal F: Add ADR Prerequisite Notes

Add explicit prerequisite notes to affected stories before their acceptance criteria.

Recommended additions:

```markdown
**Decision Prerequisite:** ADR-002 must define command idempotency semantics before implementation starts.
```

Applies to: Story 1.6 and split idempotency conformance story.

```markdown
**Decision Prerequisite:** ADR-003 and ADR-006 must define tenant projection durability, freshness vocabulary, blocking states, and evidence metadata before implementation starts.
```

Applies to: Stories 1.5, 1.7, split projection rebuild/freshness story, Story 3.7, Story 4.4, Story 6.2.

```markdown
**Decision Prerequisite:** ADR-005 must define schema evolution, upcasting, projection compatibility, and unsupported-version handling before implementation starts.
```

Applies to: Story 1.10, split schema compatibility story, split event schema evolution story, Story 5.1, Story 5.7 after renumbering.

```markdown
**Decision Prerequisite:** ADR-007 must define redaction replay and non-disclosure behavior across projections, logs, exports, accessibility, clipboard, caches, and derived indexes before implementation starts.
```

Applies to: Story 2.4, Story 3.3, redaction replay conformance story, Story 6.8.

```markdown
**Decision Prerequisite:** ADR-010 must define retention, deletion, tombstoning, legal hold, export, projection rebuild, and derived-index lifecycle behavior before implementation starts.
```

Applies to: Story 2.1, Story 2.6, Story 2.7, redaction replay conformance story.

```markdown
**Decision Prerequisite:** ADR-009 must define FrontComposer trust-component boundaries and disclosure-surface test requirements before implementation starts.
```

Applies to: Story 3.2, Story 3.3, Story 3.4, Story 3.8.

Rationale: The architecture already says these ADRs are required. The story file should carry that stop condition directly where implementers will see it.

### Proposal G: Tighten Story 3.8 as Verification Scope

Story: 3.8 Verify Responsive and Accessible Investigation Experience

Section: Requirements Covered

OLD:

```markdown
**Requirements Covered:** FR56-FR69 support; UX-DR39-UX-DR52; NFR69-NFR77.
```

NEW:

```markdown
**Requirements Covered:** FR56-FR69 verification support; UX-DR39-UX-DR52; NFR69-NFR77.
**Scope Note:** This story verifies responsive, accessibility, and disclosure-surface safety for the investigation workspace. It does not implement the full workspace feature set; implementation remains in Stories 3.1-3.7.
**Decision Prerequisite:** ADR-009 and ADR-007 must define trust-component boundaries, disclosure-surface test requirements, and redaction non-disclosure expectations before implementation starts.
```

Rationale: This prevents Story 3.8 from becoming a catch-all implementation story for the whole investigation workspace.

### Proposal H: Add UX Disclosure Test Asset Placement

Recommended placement: Add a test artifact requirement to Story 3.8 and connect it to Epic 5 conformance.

```markdown
**Given** the Leak Sentinel helper and canonical disclosure fixtures are prepared
**When** desktop, tablet, mobile, screen-reader, clipboard, tooltip, browser-title, telemetry, loading, empty, denied, redacted, stale, and responsive-duplicate states are exercised
**Then** forbidden strings and structured forbidden values are absent from rendered DOM text, attributes, ARIA properties, page title, clipboard output, telemetry envelopes, screenshots, and accessibility snapshots where available
**And** the resulting evidence is traceable from the conformance manifest.
```

Rationale: The UX document already defines these expectations. This makes them implementation-ready and evidence-producing.

## 6. Renumbering Guidance

If Proposal B and Proposal C are accepted:

- Add new Story 1.2 for CI/evidence scaffold.
- Current Story 1.2 becomes 1.3.
- Current Story 1.3 becomes 1.4.
- Continue renumbering through current Story 1.10.
- Replace current Story 1.11 with three stories after the current Story 1.10 sequence.

If Proposal D and Proposal E are accepted:

- Replace current Story 5.5 with Story 5.5, 5.6, and 5.7.
- Replace current Story 5.6 with Story 5.8 and 5.9.
- Current Story 5.7 becomes Story 5.10.
- Current Story 5.8 becomes Story 5.11.

Renumbering should update cross-references, requirement maps, and any generated story indexes.

## 7. Implementation Handoff

Change scope: Moderate.

Recommended recipients:

- Product Owner or Story Manager: Apply backlog edits to `epics.md`, including story splits, renumbering, scope notes, and requirement map updates.
- Architect: Draft or confirm ADR prerequisites before dependent implementation stories begin.
- Developer agent: After approval and backlog update, generate implementation stories from the corrected backlog only.
- Test Architect: Convert early evidence scaffold, Leak Sentinel fixtures, and split conformance suites into named test artifacts and manifest entries.

Success criteria:

1. Story 1.1 no longer claims behavioral FR1-FR41 coverage.
2. Early CI/evidence scaffold exists before behavior stories close.
3. Story 1.11 is split or phased so replay, projection rebuild/freshness, and schema compatibility can close independently.
4. Story 5.5 is split into tenant isolation, idempotency, and redaction replay conformance stories.
5. Story 5.6 is split into provider portability and schema evolution proof stories.
6. ADR-dependent stories include explicit decision prerequisite notes.
7. Story 3.8 is framed as responsive/accessibility/disclosure verification, not broad workspace implementation.
8. UX disclosure fixtures and Leak Sentinel expectations are represented as test assets or conformance manifest entries.
9. Readiness assessment can be rerun with major issues closed or explicitly waived.

## 8. Approval Status

Approval required before modifying `epics.md` or related backlog/status artifacts.

Recommended approval decision: Approve direct adjustment.

Post-approval next action: Update `_bmad-output/planning-artifacts/epics.md` according to the accepted proposals, then rerun implementation readiness.
