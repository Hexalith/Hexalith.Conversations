---
title: "Sprint Change Proposal — PRD Alignment and Preservation Reconciliation"
project: "Conversations"
date: "2026-07-15"
status: "approved"
changeScope: "major"
mode: "incremental"
trigger: "Implementation Readiness Assessment 2026-07-14 — NOT READY"
sourceReport: "implementation-readiness-report-2026-07-14.md"
---

# Sprint Change Proposal — PRD Alignment and Preservation Reconciliation

## Approval State

Jerome approved all six Change Navigation Checklist sections, every detailed edit proposal, and this compiled proposal in incremental mode. The proposal is authorized for implementation handoff as of 2026-07-15.

Approved detailed proposals:

- A1 — Architecture rebaseline.
- E1 — Epic structure and historical integrity.
- E2 — Story 6.1: Rebaseline Architecture and Planning Authority.
- E3 — Story 6.2: Migrate Conversations to Platform-Owned Hosting.
- E4 — Story 6.3: Create the Complete Preservation Traceability Manifest.
- E5 — Story 6.4: Repair UX Provenance and Preservation Governance.
- E6 — Story 6.5: Correct the Thin Authoring Template and Reproduce SM-2.
- E7 — Story 6.6: Revalidate and Issue Superseding Attestation.
- E8 — Append-only historical correction dispositions for completed stories.

## 1. Issue Summary

### Problem Statement

The finalized 2026-07-14 PRD is coherent, but the architecture, epics/stories, UX traceability, implementation topology, authoring guidance, and release evidence remain aligned to superseded ownership and preservation assumptions. The current planning and implementation state therefore cannot safely support a new implementation-readiness claim.

This is a downstream synchronization failure, not a new product requirement, strategic pivot, or request to reduce the PRD.

### Discovery Context

The issue was identified by the 2026-07-14 Implementation Readiness Assessment rather than by one implementation story. All five original epics and all 24 stories are already marked done, which makes append-only correction and superseding evidence mandatory. Rewriting completed history would destroy the audit trail.

### Evidence

- Architecture prescribes Conversations-owned AppHost and ServiceDefaults projects and concludes `READY FOR IMPLEMENTATION`.
- The live solution contains `Hexalith.Conversations.AppHost`, `Hexalith.Conversations.ServiceDefaults`, and their test projects.
- Only 13 of 20 initiative FRs are fully aligned; 7 are partially aligned.
- Zero of 104 preserved Feature-FRs have direct evidence-level traceability.
- Zero of 52 UX decision mappings are reliable against the current epic meanings.
- Ten of 24 stories have critical defects; seven have major defects.
- Signed v1 release evidence is bound to immutable hashes and must not be edited.

### Immediate Control

Suspend new readiness and final-release claims for the boilerplate-reduction initiative until Epic 6 is complete and a new readiness assessment returns `READY`. The signed v1 decision remains historical evidence for its bound scope, not current proof of alignment to the finalized PRD.

## 2. Impact Analysis

### PRD Impact

No PRD modification is required. The initiative scope, preservation requirements, platform-ownership boundary, FR-16 deferral, and SM-C2 gate remain authoritative.

The MVP/refactor outcome remains achievable. Delivery status, not product intent, is blocked.

### Epic Impact

Epics 1–5 remain immutable historical execution context. Add Epic 6 rather than retroactively rewriting completed stories, retrospectives, or signed evidence.

| Epic | Impact | Corrective disposition |
| --- | --- | --- |
| Epic 1 | Baseline and preservation work predate the finalized manifest contract. | Preserve accepted baseline; create complete v2 manifest in Story 6.3. |
| Epic 2 | Host framing and performance evidence require correction/revalidation. | Correct topology in Story 6.2 and performance evidence in Story 6.6. |
| Epic 3 | AppHost/ServiceDefaults/Aspire ownership conflicts with the PRD; FR-16 and OQ states drift. | Correct ownership in Stories 6.1–6.2; classify FR-16 as deferred. |
| Epic 4 | Thin template and SM-2 boundary include prohibited module-owned hosting projects. | Correct and remeasure in Story 6.5. |
| Epic 5 | The signed attestation is bound to an incomplete denominator. | Preserve v1; issue versioned superseding evidence in Story 6.6. |

### Story Impact

The following append-only disposition table will be added to `epics.md`. Original story text remains unchanged.

| Story | Current finding | Epic 6 disposition |
| --- | --- | --- |
| 1.1 | Critical: no finalized frozen manifest contract. | Superseded for current readiness by 6.3. |
| 1.2 | Major: blind-spot analysis is unbounded. | Retain historical output; register bounded evidence/dispositions through 6.3. |
| 1.3 | Structurally sound. | Retain and map its register into the v2 manifest. |
| 1.4 | Major: story wording retains obsolete ~18,000-LOC uncertainty. | Preserve accepted 13,289-LOC baseline; add historical correction note. |
| 1.5 | Structurally sound. | Retain; apply its approval model to v2 manifest governance. |
| 2.1 | Critical: local host framing. | Superseded by platform-host migration in 6.2. |
| 2.2 | Mostly sound. | Revalidate under 6.6. |
| 2.3 | Mostly sound. | Revalidate under 6.6. |
| 2.4 | Major: no measurable SM-C2 gate. | Revalidate with the reproducible <=5% P95 gate in 6.6. |
| 2.5 | Mostly sound. | Revalidate under 6.6. |
| 2.6 | Critical: forward dependency on 3.6. | Record historical closure; current surface fixed by 6.1 and revalidated by 6.6. |
| 2.7 | Major: ambiguous ServiceDefaults dependency. | Remove local defaults scope in 6.2; revalidate testing evidence in 6.6. |
| 3.1 | Mostly sound. | Retain; reflect its landing zone in corrected architecture. |
| 3.2 | Major: oversized cross-repository slice. | Retain delivered capability; revalidate fail-closed behavior through 6.6. |
| 3.3 | Mostly sound. | Retain; bind metric-name/cardinality evidence through 6.3 and 6.6. |
| 3.4 | Critical: Conversations-owned ServiceDefaults facade/project. | Superseded by 6.2. |
| 3.5 | Critical: Conversations-owned AppHost/topology. | Superseded by 6.2. |
| 3.6 | Major: uncertain extension boundary. | Record exact shared public surface in 6.1; revalidate in 6.6. |
| 3.7 | Critical: FR-16 remained conditionally executable. | Mark deferred/non-adopted for Conversations; additive platform metadata remains outside pilot acceptance. |
| 4.1 | Critical: template teaches prohibited project ownership. | Superseded by 6.5. |
| 4.2 | Major: evidence lacks a reproducible fixture and full metadata. | Superseded by 6.5 v2 evidence. |
| 5.1 | Critical: final proof lacks finalized manifest governance. | Preserve v1; supersede through 6.6. |
| 5.2 | Critical: loose removal-ledger model. | Preserve v1; supersede through v2 manifest mutation governance. |
| 5.3 | Critical: missing SM-C2 threshold and complete traceability. | Preserve v1; supersede through 6.6. |

### Architecture Conflicts

Update these areas in `architecture.md`:

- Input provenance and initiative-versus-preserved-requirement framing.
- Starter selection and initialization commands.
- Project boundary rules and directory tree.
- Core hosting, ServiceDefaults, Aspire/Dapr, and deployment decisions.
- OQ-1 landing-zone register.
- SM-C2 performance language.
- Requirements-to-structure mapping.
- Development workflow, validation, readiness conclusion, and handoff.

The corrected ownership model is:

| Requirement | Public capability owned outside Conversations |
| --- | --- |
| FR-10 | `EventStore.ServiceDefaults` and `EventStore.DomainService`; lower-level Commons implementation remains a platform detail. |
| FR-11 | `Hexalith.Commons.TenantAccess`, consumed through domain-specific adapters. |
| FR-12 | `Hexalith.Commons.Http`. |
| FR-13 | Platform AppHost and `EventStore.Aspire`; Commons Aspire/publication helpers remain platform implementation details. |
| FR-14 | `Hexalith.Commons.Serialization` and the existing shared type-mapping surface. |
| FR-15 | `Hexalith.Commons.Diagnostics` plus EventStore domain telemetry registration. |
| FR-16 | Deferred; additive platform metadata does not activate Conversations adoption. |

### UX Conflicts

The UX specification remains a valuable preservation reference, but it is not an active component-delivery plan for this pilot.

Required changes:

- Point provenance to the finalized PRD and archived legacy source.
- Add a preservation-only banner.
- Mark the component roadmap non-activated unless separately approved.
- Replace stale story-number mappings with manifest evidence, explicit non-activation, or separately approved delivery stories.
- Retain old story mappings only as labeled historical provenance.
- Align guidance to FrontComposer and Blazor Fluent UI V5, Fluent 2 tokens, reuse-first rules, and the page-section accordion convention.

### Technical and Operational Impact

- Remove Conversations-owned AppHost and ServiceDefaults source/test projects and solution entries.
- Register Conversations and optional Admin Web resources from the platform AppHost through `EventStore.Aspire`.
- Move generic topology or defaults gaps into platform-owned capabilities.
- Replace local topology/defaults tests with platform composition and module integration evidence.
- Update CI lanes, documentation validators, scaffold guards, scripts, and deployment assumptions.
- Preserve public Conversations contracts and domain behavior.
- Capture the pre-correction SM-C2 benchmark before Story 6.2 changes code. If it is not captured, reconstruct it reproducibly from the preserved source commit rather than inventing a baseline.

### Evidence Impact

The following artifacts are historical and must remain byte-identical where bound by signed decisions:

- `success-metric-report-and-attestation-v1.json`
- `success-metric-report-and-attestation-v1.md`
- `success-metric-report-and-attestation-v1-release-owner-decision.json`
- `success-metric-report-and-attestation-v1-release-owner-decision.md`

Create versioned v2 artifacts and a separate supersession record. Do not overwrite or silently invalidate v1.

The accepted 13,289-LOC SM-1 baseline remains frozen. Hosting-project removal is post-baseline evidence, not a reason to rewrite the baseline.

## 3. Recommended Approach

### Selected Path

**Option 1 — Direct Adjustment through a new corrective epic.**

The PRD remains unchanged, completed history remains auditable, and implementation moves forward to the correct ownership boundary.

### Alternatives Considered

| Option | Decision | Reason |
| --- | --- | --- |
| Direct Adjustment | Selected | Corrects artifacts and implementation without erasing history or weakening the PRD. |
| Roll back Epics 3–5 | Rejected | High effort and risk; would disturb shared promotions and signed evidence without guaranteeing the correct platform boundary. |
| Reduce/redefine the PRD | Rejected | The PRD is coherent; weakening ownership or preservation rules would defeat the initiative. |

### Effort, Risk, and Timeline

- **Effort:** High.
- **Risk:** Medium-high, concentrated in platform-host integration, evidence completeness, and performance comparability.
- **Timeline impact:** At least one corrective sprint, plus architecture review and a final readiness review. Exact calendar duration depends on platform-host integration review and test-environment availability.
- **Release impact:** Current final/readiness claims remain blocked until Story 6.6 and the readiness rerun complete.

### Sequencing

1. Story 6.1 establishes corrected architecture and planning authority.
2. Freeze the reproducible pre-correction SM-C2 benchmark.
3. Story 6.2 migrates hosting and removes local projects.
4. Stories 6.3 and 6.4 repair preservation and UX governance; they may proceed after 6.1 where dependencies allow.
5. Story 6.5 corrects the template and reproduces SM-2 after 6.2.
6. Story 6.6 runs all final gates, issues superseding evidence, and triggers readiness reassessment.

## 4. Detailed Change Proposals

### 4.1 Architecture Rebaseline

**OLD**

The architecture selects a composite Conversations-owned AppHost/ServiceDefaults scaffold, treats the legacy absolute performance target as a primary active driver, and concludes `READY FOR IMPLEMENTATION`.

**NEW**

Use a platform-composed domain-module scaffold. Conversations owns domain contracts, behavior, handlers, projections, adapters, and optional domain UI. The platform AppHost owns topology; EventStore DomainService, ServiceDefaults, and Aspire own runtime boilerplate. The active performance gate is SM-C2. Architecture status becomes `READY FOR CORRECTIVE IMPLEMENTATION ONLY` until Epic 6 completes.

### 4.2 Epic Structure

**OLD**

Epics 1–5 are presented as the complete current plan, with no architecture document and stale OQ/UX assumptions.

**NEW**

Retain Epics 1–5 as historical execution context, append the historical correction table, and add Epic 6 as the only active corrective plan. Update the FR coverage map to show Epic 6 corrective coverage for FR-3, FR-10, FR-13, and FR-17–FR-20.

### 4.3 Epic 6 — PRD Alignment and Preservation Reconciliation

#### Story 6.1 — Rebaseline Architecture and Planning Authority

As a platform architect, I want architecture and epic authority reconciled to the finalized PRD, so corrective implementation starts from one ownership and decision model.

Acceptance includes platform-owned hosting/defaults/topology, FR-10–FR-15 landing zones, resolved OQ states, deferred FR-16, SM-C2 language, corrected diagrams/tree/workflow, append-only epic amendments, and no historical evidence mutation.

#### Story 6.2 — Migrate Conversations to Platform-Owned Hosting

As a Conversations maintainer, I want Conversations composed exclusively by the platform host, so the domain module contains no platform-owned hosting boilerplate.

Acceptance includes pre-change benchmark capture, removal of local AppHost/ServiceDefaults projects and tests, platform-host registration, preserved topology/security/health/publication behavior, platform-owned generic extensions, updated CI/docs/tests, unchanged public contracts, and green conformance.

#### Story 6.3 — Create the Complete Preservation Traceability Manifest

As a release owner, I want a frozen, versioned preservation manifest with complete requirement dispositions, so preservation claims are exact and resistant to denominator drift.

Acceptance includes source/build/test/baseline hashes; 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX-DRs, and all UX acceptance criteria; evidence or owner-approved non-activation for every obligation; module/platform control separation; versioned mutation governance; and automated zero-gap validation.

#### Story 6.4 — Repair UX Provenance and Preservation Governance

As a UX governance owner, I want the UX specification treated as a preservation reference with reliable evidence mappings, so it constrains behavior without silently authorizing UI delivery.

Acceptance includes finalized provenance, preservation banner, non-activated roadmap, reliable mappings, historical-only old story references, manifest-linked safety criteria, preserved-but-not-automatically-active absolute targets, and current Hexalith UI rules. No production UI change is authorized.

#### Story 6.5 — Correct the Thin Authoring Template and Reproduce SM-2

As a domain-module author, I want a platform-hosted thin template with reproducible authoring-cost evidence, so SM-2 measures only code a domain module owns.

Acceptance includes no domain-owned AppHost/Aspire/ServiceDefaults projects, live platform API validation, a reproducible minimal fixture, complete measurement metadata, frozen SM-1 baseline, v2 template/SM-2 evidence, and validators that reject prohibited project ownership.

#### Story 6.6 — Revalidate and Issue Superseding Attestation

As a release owner, I want the corrected implementation revalidated against the complete preservation contract, so a new readiness decision rests on current evidence.

Acceptance includes:

- Confirmation that the pre-correction benchmark was captured reproducibly before Story 6.2, or reconstructed from the preserved source commit.
- 100% of the v2 manifest denominator passing.
- Public-contract shape equality or explicitly approved compatible change evidence.
- SM-C2 post-correction P95 no more than 5% above baseline under the same envelope.
- Reproducible SM-2 and final SM-1/SM-3 results.
- No Conversations-owned AppHost/Aspire/ServiceDefaults projects.
- Platform topology, health, security, publication, and admin composition evidence.
- Versioned v2 attestation, separate supersession record, and new release-owner decision.
- Signed v1 evidence unchanged.
- A rerun of Implementation Readiness returning `READY` before release closure.

### 4.4 UX Requirement Map

**OLD**

Every UX-DR maps to obsolete story numbers, including nonexistent stories.

**NEW**

Use columns for preservation-manifest mapping, activation status, evidence/disposition, current delivery story if any, and historical story provenance. Zero current mapping may rely only on a reused story number.

### 4.5 Thin Authoring Template

**OLD**

The minimal skeleton includes AppHost and optionally ServiceDefaults as domain-owned projects.

**NEW**

The minimal skeleton contains only domain-owned contracts, behavior, handlers/adapters, a minimal SDK entry point where required, client/testing assets, and optional domain UI. Platform topology and defaults are consumed from the platform host and SDK.

### 4.6 Release Evidence

**OLD**

The signed v1 attestation is the final initiative record despite incomplete finalized-PRD traceability.

**NEW**

Keep v1 immutable as historical evidence. Create v2 evidence bound to the corrected source, complete manifest, reproducible performance/authoring measurements, and platform-host proof. Record supersession separately and obtain a new release-owner decision.

## 5. Implementation Handoff

### Scope Classification

**Major** — the correction changes architecture authority, completed-backlog interpretation, module/platform boundaries, test topology, evidence governance, and release status.

### Recipients and Responsibilities

| Role | Responsibility |
| --- | --- |
| Product Manager / Product Owner | Authorize Epic 6, preserve historical scope, and prevent unrelated feature expansion. |
| Platform / Solution Architect | Complete Story 6.1, ratify public capability boundaries, and review platform-host migration. |
| Developer | Execute Story 6.2 and Story 6.5 implementation changes; update affected tests, solution, docs, and CI. |
| Quality / Test Architect | Own manifest completeness, performance comparability, conformance execution, and final evidence validation. |
| UX Designer / Technical Writer | Execute Story 6.4 provenance, preservation banner, mappings, and current UI-governance wording. |
| Release Owner | Approve non-activated dispositions, v2 attestation, supersession record, and final release decision. |

### Sprint Status Update After Approval

Preserve all Epic 1–5 and story statuses as `done`. Append:

```yaml
epic-6: backlog
6-1-rebaseline-architecture-and-planning-authority: backlog
6-2-migrate-conversations-to-platform-owned-hosting: backlog
6-3-create-complete-preservation-traceability-manifest: backlog
6-4-repair-ux-provenance-and-preservation-governance: backlog
6-5-correct-thin-authoring-template-and-reproduce-sm-2: backlog
6-6-revalidate-and-issue-superseding-attestation: backlog
epic-6-retrospective: optional
```

### Success Criteria

- Architecture contains no Conversations-owned hosting/defaults/topology decision and records current FR-10–FR-15 landing zones.
- No Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent topology/defaults project remains.
- All 20 initiative FRs are semantically aligned.
- All 104 Feature-FRs, all 77 Feature-NFRs, all 52 UX-DRs, and applicable UX acceptance criteria have evidence or approved dispositions.
- FR-16 is unambiguously deferred/non-adopted for Conversations.
- No forward story dependency remains in the active corrective plan.
- FR-19 and SM-C2 have reproducible, versioned evidence.
- Full v2 manifest denominator passes at 100% and public contracts remain compatible.
- Signed v1 evidence remains byte-identical; v2 evidence and supersession are explicit.
- The final Implementation Readiness Assessment returns `READY`.

### Non-Goals

- No new Conversations product feature or UX component delivery.
- No weakening of tenant isolation, governance, audit, redaction, replay, idempotency, or contract-preservation requirements.
- No fleet migration of other domain modules.
- No retroactive rewriting of completed story files, retrospectives, or signed v1 evidence.
- No recursive or nested submodule initialization.

## Checklist Record

### Section 1 — Trigger and Context

- [N/A] 1.1 No single triggering story; the readiness assessment is the trigger.
- [x] 1.2 Core problem defined as downstream requirement/ownership drift.
- [x] 1.3 Supporting evidence recorded.

### Section 2 — Epic Impact

- [!] 2.1 Completed initiative cannot establish readiness as written.
- [!] 2.2 Corrective Epic 6 required.
- [!] 2.3 All five completed epics affected.
- [x] 2.4 Original epics preserved historically; no wholesale deletion.
- [!] 2.5 Corrective sequencing required.

### Section 3 — Artifact Impact

- [x] 3.1 PRD unchanged and achievable.
- [!] 3.2 Architecture correction required.
- [!] 3.3 UX governance correction required.
- [!] 3.4 Implementation, tests, evidence, docs, CI, and sprint status affected.

### Section 4 — Path Forward

- [x] 4.1 Direct adjustment selected; high effort, medium-high risk.
- [N/A] 4.2 Broad rollback rejected.
- [N/A] 4.3 PRD/MVP reduction rejected.
- [x] 4.4 Recommended path approved.

### Section 5 — Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic and artifact impacts complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 MVP impact and action plan complete.
- [x] 5.5 Major-scope handoff defined.

### Section 6 — Final Review and Handoff

- [x] 6.1 Checklist reviewed and action items carried forward.
- [x] 6.2 Proposal accuracy confirmed by final user review.
- [x] 6.3 Explicit final approval received on 2026-07-15.
- [x] 6.4 Sprint status updated append-only with Epic 6 and Stories 6.1–6.6 in backlog.
- [x] 6.5 Major-scope handoff activated for the named recipient roles.

## Final Approval and Handoff Record

- **Approved by:** Jerome
- **Approval date:** 2026-07-15
- **Decision:** Approved for implementation
- **Scope:** Major
- **Sprint activation:** Epic 6 and Stories 6.1–6.6 registered as backlog; Epics 1–5 remain historical and `done`.
- **First implementation step:** Story 6.1 — Rebaseline Architecture and Planning Authority.
- **Routing:** Product Manager / Product Owner, Platform / Solution Architect, Developer, Quality / Test Architect, UX Designer / Technical Writer, and Release Owner.
