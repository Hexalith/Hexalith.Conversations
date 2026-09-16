---
title: "PRD Reconciliation — Conversations UX Update"
date: "2026-09-16"
status: reconciled-input
disposition: preserved-not-activated
---

# PRD Reconciliation

## Reconciliation Result

The PRD package is accepted as a preservation and conflict-discovery input for
this UX update, not as authority to activate product UI work. The current UX
disposition remains `preserved-not-activated`. No PRD review proposal is treated
as an approved requirement until the PRD is revised and revalidated.

## Confirmed Sources

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/addendum.md`
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/validation-report.md`
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.memlog.md`

The authoritative PRD, addendum, and epics have no uncommitted content delta.
The 2026-09-16 update signal is the new validation decision in `.memlog.md:42`
and the accompanying review/report changes. The validation grades the current
PRD `Poor` and bars its use for release, architecture, or story-generation
authority pending correction (`validation-report.md:6-12,120-122`).

## Accepted UX Inputs

| Input | UX treatment | Source |
| --- | --- | --- |
| The initiative is a plumbing refactor, not a feature release. | Keep refactor/developer experience separate from preserved product UX. | `prd.md:10-20` |
| Current refactor users and journeys are Nadia, Sam, and Priya. | Carry as source journeys requiring either explicit UX flow coverage or an approved non-UX/deferred disposition. | `prd.md:34-58` |
| Product behavior and public contracts must remain externally unchanged. | Preserve fail-closed tenant isolation, governance/audit pairing, idempotency, redaction replay, freshness/degraded signaling, and contract shape as trust constraints. | `prd.md:20,72-74` |
| UI redesign is outside this initiative. | Preserve existing UX decisions without activating screens, components, routes, or product UI stories. | `prd.md:88-97` |
| The preserved product contract names nine affected actors and journeys. | Carry Maya, Atlas, Sarah, Diego, Marcus, Julian, Helen, Naomi, and Daniel into source-to-flow coverage, while retaining their preservation-only disposition. | `prd.md:421-433` |
| Operator evidence, trust, and accessibility requirements remain preserved. | Retain operator workflows, freshness states, safe errors, WCAG 2.1 AA, and human-trust obligations as future-activation constraints. | `prd.md:529-558,658-705` |
| Epic 8 governs UX preservation only. | Retain the closed 52-decision/28-acceptance denominator and prohibit production UI changes. | `epics.md:2542-2597,2599-2632` |
| Epic 16 adds operational truth states. | Record unknown/stale/gapped/corrupt authorization, initialized-empty versus missing/erased/unavailable state, and pending/reconciled recovery as source-owned semantic inputs requiring an explicit UI mapping. | `epics.md:4359-4379,4392-4412,4425-4445,4458-4477` |

## Conflicts And Unresolved Proposals

| Conflict or proposal | Reconciliation disposition | Source |
| --- | --- | --- |
| The legacy UX says current authority comes from the PRD/addendum, but current validation rejects that PRD as release/architecture/story authority. | Surface as an authority conflict. Use the PRD for preservation context only until remediation is approved. | `ux-design-specification.md:44-54`; `validation-report.md:8-12,120-122` |
| The PRD calls developers the only users and assumes no external/customer-facing surface, while its own preserved contract affects adopter APIs, tenant users, and operator workflows. | Do not collapse these groups. Distinguish direct refactor users from stakeholders harmed by preservation failure. The reviewer proposal is unresolved until accepted into a revised PRD. | `prd.md:34-48`; `validation-report.md:84-90` |
| The FR-20 denominator does not demonstrably contain projection-freshness or governance/audit-pairing coverage. | Treat trust-preservation proof as blocked; do not claim those UX behaviors are proven preserved. | `validation-report.md:34-38` |
| Section 14 lacks a machine-safe per-requirement activation model. | Keep every imported Feature-FR/Feature-NFR explicitly preservation-only until a signed activation/disposition authority exists. | `validation-report.md:72-76`; `epics.md:3528-3559,3581-3587` |
| Exact stale-tenant-projection status/retry semantics and audit-pairing health exposure remain open. | Do not invent UI status, retry action, microcopy, or announcement semantics. | `addendum.md:25-33` |
| Epic 16 introduces states not closed over the existing UX vocabulary. | Require a source-owned wire-value/reason-code-to-display-state mapping; client-side inference remains forbidden. | `epics.md:4392-4477`; `ux-design-specification.md:1027-1053,1068-1079` |
| PRD accessibility authority remains WCAG 2.1 AA; UX validation proposes WCAG 2.2 AA. | Preserve 2.1 AA as the current source requirement and record 2.2 AA as an activation-time decision, not an accepted change. | `prd.md:695-705`; `ux-designs/ux-Conversations-2026-08-18/validation-report.md:118-128` |
| The normative 5% hot-path gate conflicts with evidence accepting much larger list/open regressions. | Record as a future Find/Open experience risk; do not infer a UX latency promise or a passing state. | `validation-report.md:26-32` |

## Deliberately Dropped Or Deferred

- **Dropped from the UX update:** the unqualified claim that there is no
  external/customer-facing blast radius. It is contradicted by the preserved
  actor, contract, tenant-isolation, and operator obligations and by the current
  validation finding (`prd.md:36,45-48,421-437`;
  `validation-report.md:86-90`). This does not activate customer-visible work.
- **Deferred:** product UI implementation, navigation, production components,
  and screen activation. The implementation hold and UX non-activation route
  remain in force (`epics.md:2586-2597,4370-4375,4494-4496`).
- **Deferred:** exact presentation labels and actions for gapped, corrupt,
  erased, pending-reconciliation, endpoint-readiness, and audit-health states
  until source contracts publish canonical mappings.
- **Deferred:** upgrading the normative accessibility floor to WCAG 2.2 AA,
  selecting release-slice mutation/export capabilities, and asserting Find/Open
  performance acceptance. Each requires separate approved authority.
- **Not carried as decisions:** reviewer-proposed PRD fixes. They remain
  remediation proposals until the PRD, addendum, and evidence chain are revised
  and revalidated.

## UX Update Constraint

Any resulting `DESIGN.md` and `EXPERIENCE.md` may distill these preservation
inputs and explicitly document their conflicts, but must retain
`status: preserved-not-activated`, must not assign implementation ownership, and
must not turn PRD validation recommendations into approved product decisions.
