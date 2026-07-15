---
epic: 6
generated: '2026-07-15'
overlay_version: 'epic-6-authority-2026-07-15-v1'
architecture_version: 'conversations-architecture-2026-07-15-v1'
source_epics: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md'
source_overlay_begin: 'EPIC-6-AUTHORITY-OVERLAY:BEGIN'
status: 'active-corrective-context'
---

# Epic 6 Context: PRD Alignment And Preservation Reconciliation

This developer context is derived from the versioned append-only Epic 6 overlay in the amended epic plan. The overlay and this context share version `epic-6-authority-2026-07-15-v1`; semantic drift between them is a conformance failure. The finalized initiative PRD/addendum and approved July 15 proposals remain the authority above this derived context.

## Authority And Immutable History

- The initiative has 20 FRs: FR-1 through FR-20. FR-16 is the only non-activation and is deferred.
- The preservation denominator is all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, and all UX acceptance criteria.
- The accepted SM-1 baseline remains 13,289 LOC.
- Epics 1-5, all 24 completed stories, retrospectives, `done` states, and signed v1 evidence remain immutable historical records.
- A delivered-to-inactive disposition or compatible public-contract change requires named owner approval, rationale, and compatibility evidence.
- Epic 6 is the only active corrective plan. It does not activate preserved feature scope.

## Corrected Ownership Spine

Conversations owns contracts, aggregate/domain behavior, validators, handlers, projections/read-model semantics, domain adapters, domain telemetry definitions, client/testing assets, and optional domain UI. The platform AppHost owns topology. EventStore DomainService, ServiceDefaults, and Aspire own generic hosting, endpoints, DAPR resources, health, telemetry wiring, query/projection runtime, and subscriptions.

Current `Hexalith.Conversations.AppHost` and `Hexalith.Conversations.ServiceDefaults` projects are pre-6.2 migration input, not target architecture. Story 6.1 does not remove them. Story 6.2 removes them after the benchmark is frozen.

The canonical domain-host pair is:

```csharp
builder.AddEventStoreDomainService(/* domain assemblies/options */);
app.UseEventStoreDomainService();
```

Do not teach direct `MapEventStoreDomainService()` use.

| FR | Public landing zone |
| --- | --- |
| FR-10 | EventStore.ServiceDefaults + EventStore.DomainService |
| FR-11 | Hexalith.Commons.TenantAccess |
| FR-12 | Hexalith.Commons.Http |
| FR-13 | Platform AppHost + EventStore.Aspire |
| FR-14 | Hexalith.Commons.Serialization |
| FR-15 | Hexalith.Commons.Diagnostics + EventStore domain telemetry |
| FR-16 | deferred, non-activated |

OQ-1 through OQ-5 are resolved in architecture. Governance/temporal/hydration behavior remains domain-owned; performance uses SM-C2; preserved absolute targets activate only through a current release decision.

## Still-Binding Safety Decisions

- Keep versioned events, compatible readers/upcasters, deterministic mixed-stream replay, and typed failure for unsupported versions.
- EventStore history wins over projections/caches/exports. Derived disagreement causes quarantine/stale/rebuild state, and rebuild does not replay external side effects.
- Tenants access and participant validation writes fail closed. Authorized Party hydration reads may degrade only to a policy-defined non-personal placeholder with explicit state.
- Idempotency preserves equivalent retry outcomes, rejects payload mismatch, and records unknown outcome explicitly rather than blindly retrying.
- Governance mutations require paired audit/domain evidence. Approved legal-policy exceptions require named owner, rationale, scope, and evidence.

## SM-C2 Contract

Frozen inventory version: `sm-c2-hot-path-inventory-v1`.

| ID | Kind | Operation |
| --- | --- | --- |
| HP-CREATE | command-warm | authorized conversation creation |
| HP-APPEND | command-warm-idempotent | append including duplicate replay and payload mismatch |
| HP-LIST | read-warm | authorized filtered/cursored list |
| HP-OPEN | read-warm | detail with freshness, redaction, evidence, and Party hydration |

Every baseline row has exactly one post disposition. Each must satisfy `post P95 <= 1.05 x baseline P95` with identical workload/data, concurrency, environment/runtime, tooling, warm/cold classification, repetitions, raw evidence processing, and commit-bound evidence.

## Stories

### 6.1 Rebaseline architecture and planning authority

- Reconcile architecture/epic authority, public landing zones, OQs, SM-C2, target tree, and corrective-only readiness.
- Append rather than rewrite history; preserve signed v1 evidence and the original epic prefix.
- Add Story 6.7 and the promotion invariant.

### 6.2 Migrate Conversations to platform-owned hosting

- Freeze/reconstruct the versioned benchmark before topology changes.
- Remove local hosting/defaults projects and tests, compose through platform surfaces, preserve topology/security/health/publication/admin behavior and public contracts.
- Put generic gaps in the owning platform surface and pass Story 6.7 for promotions.

### 6.3 Create the complete preservation traceability manifest

- Cover all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs, 52 UX decisions, all UX acceptance criteria, public contracts, and current controls with zero gaps.
- Require evidence or named-owner non-activation plus rationale; bind hashes, versioned mutation governance, and module/platform control ownership.

### 6.4 Repair UX provenance and preservation governance

- Treat UX as preservation-only unless separately activated; repair provenance/mappings while retaining historical mappings as labeled provenance.
- Preserve current FrontComposer/Fluent UI V5 governance; authorize no production UI change.

### 6.5 Correct the thin authoring template and reproduce SM-2

- Prohibit domain-owned AppHost/Aspire/ServiceDefaults from the template.
- Use live public platform APIs and a reproducible fixture/versioned v2 measurement while preserving the 13,289-LOC baseline.

### 6.6 Revalidate and issue superseding attestation

- Run the complete manifest, public-contract, SM-C2, SM-1/SM-2/SM-3, and platform-composition gates.
- Issue versioned v2 evidence, a separate supersession record, and a new release-owner decision without mutating v1.
- Run last and require readiness `READY` before release closure.

### 6.7 Mechanically block incomplete submodule promotions from completion

- Declare exact root `references/...` promotion paths and remote-availability policy.
- Require initialized/clean affected submodules and exact mode-`160000` committed root gitlinks; include changed gitlinks since baseline.
- Block review/completion with stable codes; warn on unrelated state.
- Read root `.gitmodules` only. Never initialize, update, or traverse nested submodules.

## Binding Sequence

`6.1 -> 6.7 -> 6.2`

- 6.7 and the frozen benchmark precede 6.2 completion.
- 6.3/6.4 may proceed after 6.1 where dependencies allow.
- 6.2 precedes 6.5.
- 6.6 is last.

## Promotion Completion Invariant

Before promotion-bearing work reaches `done`, every affected root-declared submodule must be clean including untracked files, satisfy the declared availability policy, and be represented by its exact commit in a mode-`160000` gitlink in the committed umbrella revision. The affected set is the declaration plus gitlinks changed since the work baseline. Unrelated state warns; only affected state blocks. Nested submodules are never initialized or traversed.

## Story 6.1 Verification Boundary

Story 6.1 changes planning authority and conformance validation only. It must not modify the finalized PRD/addendum, historical epic prefix, retrospectives, signed v1 evidence, runtime source, solution membership, submodule contents/gitlinks, UX governance, thin template, release evidence, or the Story 6.7 gate implementation.
