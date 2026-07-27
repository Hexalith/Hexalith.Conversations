---
title: "Sprint Change Proposal — Restore the Repository-Owned Conversations AppHost"
project: "Conversations"
date: "2026-07-26"
status: "approved"
changeScope: "major"
mode: "batch"
trigger: "Human correction during the paused Story 6.2 bmad-loop resolution"
affectedAuthority: "epic-6-authority-2026-07-15-v2"
---

# Sprint Change Proposal — Restore the Repository-Owned Conversations AppHost

## Approval State

Jerome approved this proposal for major-scope architecture handoff on 2026-07-26. The approval authorizes the named owners to correct architecture and planning authority; it does not authorize sprint-status changes or completion of the paused bmad-loop resolution before those authority changes are in place.

## 1. Issue Summary

### Human Decision

During resolution of the paused Story 6.2 escalation, Jerome corrected the intended ownership decision: **Hexalith.Conversations should have its own AppHost, as modules such as Tenants, Parties, Memories, and Folders do.** The earlier reference to “Memories” was a naming mistake; “Conversions” means the Conversations module.

### Problem Statement

The July 15 corrective rebaseline interpreted shared Aspire capability in FR-13 as requiring a centralized platform AppHost and made removal of `Hexalith.Conversations.AppHost` a binding target. That interpretation conflicts with the human decision, with the repository's pre-existing composition model, and with the current shape of sibling modules.

This is an architecture-ownership correction, not a new Conversations product feature. The domain and UX behavior, preservation denominator, projection-population proof, promotion gate, performance gate, and release-evidence obligations remain in force.

### Evidence

- The current repository already contains `src/Hexalith.Conversations.AppHost/`, its topology class, tests, solution membership, security configuration, EventStore composition, Conversations Server composition, and Admin Web composition.
- Root-declared sibling repositories currently contain `Hexalith.Tenants.AppHost`, `Hexalith.Parties.AppHost`, `Hexalith.Memories.AppHost`, and `Hexalith.Folders.AppHost`.
- Those sibling AppHosts compose platform resources through shared helpers such as `AddHexalithEventStore`, `AddHexalithEventStoreSecurity`, and domain-module resource registration.
- Historical Story 3.5 expected the Conversations AppHost to remain as a thin placeholder after shared Aspire wiring was extracted.
- The current Epic 6 v2 overlay, architecture v2, Story 6.2 frozen spec, derived Epic 6 context, and conformance guards instead require that Conversations own no AppHost.
- The universal Hexalith LLM baseline currently forbids every domain-owned `*.AppHost`, while several active Hexalith modules own one. That governance conflict must be corrected before implementation agents can follow the requested ownership model consistently.

### Immediate Control

Keep the Story 6.2 bmad-loop run paused. Do not choose FrontComposer, EventStore, or another central AppHost as the Conversations composition target. Do not write the loop `resolution.json` marker until the architecture-level authority conflict has been approved and corrected.

## 2. Corrected Ownership Invariant

### Decision

`Hexalith.Conversations.AppHost` is the repository-owned composition root for the Conversations module. It composes Conversations runtime and UI resources with required platform dependencies using public EventStore/Commons Aspire helpers.

The AppHost may own module-specific topology choices such as resource selection, resource names, dependency references, local security configuration, and which optional Conversations surfaces are launched. It must not reimplement generic EventStore hosting, DAPR component implementations, projection/query runtime, telemetry sources, health-check infrastructure, or subscription plumbing.

### Deliberate Scope Boundary

This decision approves the Conversations AppHost only. It does **not** automatically approve a module-owned reusable `Hexalith.Conversations.Aspire` or `Hexalith.Conversations.ServiceDefaults` library.

- Generic reusable hosting, service defaults, EventStore topology helpers, DAPR behavior, health wiring, telemetry wiring, projection/query runtime, and subscription plumbing remain platform-owned.
- `Hexalith.Conversations.ServiceDefaults` remains subject to Story 6.2 removal if it is only a facade over shared platform defaults and has no independently justified domain-specific responsibility.
- No `Hexalith.Conversations.Aspire` project is introduced by this correction.
- The canonical runtime integration remains `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()`; authoring guidance must not teach direct `MapEventStoreDomainService()` use.

### Proposed Cross-Module Rule

A Hexalith domain repository may own one repository-specific AppHost whose sole responsibility is composing that module's runnable topology from public platform capabilities. Reusable runtime capability remains in its owning platform library. This separates **composition ownership** from **capability ownership**.

## 3. Impact Analysis

### PRD Impact

No PRD change is required. The initiative still reduces boilerplate and centralizes reusable hosting capability. A thin repository-owned composition root does not create a second orchestration runtime; it consumes the shared runtime and topology helpers.

FR-13 should be interpreted as shared Aspire/DAPR **capability** ownership, not mandatory ownership of every module's composition root by one central host. FR-10 and FR-15 remain platform-owned capability decisions.

### Epic Impact

Epics 1–5 and the existing Epic 6 v1/v2 overlays remain historical provenance. Do not rewrite their frozen bytes. Append an Epic 6 v3 authority overlay that explicitly supersedes only the AppHost-ownership statements in v2.

| Story | Impact | Proposed disposition |
| --- | --- | --- |
| 6.1 | Its completed v2 authority prohibits a Conversations AppHost. | Keep `done` as historical work; supersede the AppHost prohibition through architecture/overlay v3. Do not reopen or rewrite the story record. |
| 6.2 | Its title, intent, boundaries, code map, tasks, and AC 2 require migration to an unnamed central host. | Retitle and revise it to retain and thin the repository-owned Conversations AppHost while preserving the projection proof and all behavioral gates. |
| 6.3 | Preservation manifest semantics do not depend on AppHost ownership. | No scope change; bind the v3 authority and corrected topology evidence. |
| 6.4 | UX governance does not depend on AppHost ownership. | No scope change. |
| 6.5 | Its template excludes AppHost from domain-owned authoring cost. | Include one thin repository-owned AppHost in the fixture and count its authored files/LOC honestly in SM-2. |
| 6.6 | Its final gate requires no Conversations-owned AppHost. | Require a tested thin Conversations AppHost and absence of duplicated generic runtime plumbing instead. |
| 6.7 | Promotion completeness is independent of the ownership choice. | No semantic change; apply it only to submodules actually changed or promoted. |

No new story identifier or sprint-status key is required. The approved v3 authority must precede further Story 6.2 planning or implementation.

### Architecture Impact

Architecture authority must advance from v2 to v3 and add this proposal to `correctionAuthority`. The following current decisions must change:

- Target ownership and migration-input language.
- FR-13 landing-zone responsibility.
- Core architectural decisions about orchestration and composition.
- Corrected target directory tree.
- Infrastructure/deployment guidance and local workflow.
- Requirements-to-structure and requirements-to-test mappings.
- Conformance guards that currently reject any target-state Conversations AppHost.

The following architecture decisions remain unchanged:

- EventStore is authoritative over derived state.
- Tenant access fails closed.
- Projection/query runtime and production read-store population obligations remain binding.
- Replay, idempotency, audit pairing, redaction, and upstream hydration boundaries remain binding.
- The versioned SM-C2 inventory, comparable evidence envelope, and `post P95 <= 1.05 × baseline P95` gate remain binding.
- Story 6.7's root-only promotion-completion invariant remains binding.
- Status remains `READY FOR CORRECTIVE IMPLEMENTATION ONLY` until all Epic 6 gates pass.

### UX Impact

No UX requirement, journey, component contract, accessibility rule, trust-state rule, or FrontComposer/Fluent UI decision changes. `Hexalith.Conversations.Admin.Web` remains an optional domain UI and is launched by `Hexalith.Conversations.AppHost` rather than by a central platform host.

The UX requirement map therefore needs no semantic change for this correction. Any generated traceability artifact should only update its architecture-authority reference if it records one.

### Technical and Operational Impact

- Keep `src/Hexalith.Conversations.AppHost/`, `tests/Hexalith.Conversations.AppHost.Tests/`, and their solution entries.
- Treat `ConversationsAppHostTopology.AddConversations(...)` as module-specific composition, not as a reusable platform facade.
- Continue consuming public EventStore/Commons Aspire helpers and move any newly discovered generic gap to its platform owner.
- Preserve topology, security, DAPR state store/pub-sub references, health, publication, Admin Web composition, and public contracts.
- Decide `Hexalith.Conversations.ServiceDefaults` independently: remove it if it remains only a thin generic wrapper, or retain it only with an explicit domain-specific justification and matching authority amendment.
- Keep production named-projection materialization, tenant-index writes, replay convergence, and query proof in Story 6.2.
- Capture or reconstruct the SM-C2 baseline before Story 6.2 changes runtime, projection, or topology behavior. Compare the repository-owned AppHost before and after correction; do not compare different central-host ownership models.

### Evidence and Metric Impact

- SM-C2 keeps the same frozen hot-path denominator and comparison rule.
- SM-2 must count the thin AppHost because it is part of the module author's required repository shape. Excluding it would understate authoring cost.
- Signed v1 evidence remains byte-identical.
- v2 evidence must identify `Hexalith.Conversations.AppHost` as the tested composition root and prove that generic platform plumbing was not duplicated.
- The preservation manifest must bind architecture v3, Epic 6 overlay v3, the corrected Story 6.2 spec, AppHost composition tests, and the unchanged projection-population proof.

### Cross-Repository Governance Impact

The universal baseline in `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` currently says a domain module must not ship its own `*.AppHost`. That instruction directly conflicts with this decision and would cause later agents to reintroduce the same error.

Its owning repository must receive an approved amendment before or together with the Conversations authority correction. EventStore and module project-context documents that repeat the absolute prohibition should then be regenerated or corrected by their owners. This proposal does not authorize editing those sibling repositories in the current resolver session.

## 4. Recommended Path

### Selected Path

**Option 1 — Direct adjustment through a versioned authority correction.**

Preserve completed history, append architecture/Epic 6 v3 authority, revise the still-blocked Story 6.2, and continue corrective implementation with the repository-owned AppHost.

### Alternatives Considered

| Option | Decision | Reason |
| --- | --- | --- |
| Direct authority adjustment | Selected | Implements the human decision, preserves history, and limits change to composition ownership and its downstream evidence. |
| Roll back completed Story 6.1 | Rejected | Rewriting or reopening completed authority work would obscure the audit trail; a versioned superseding authority is safer. |
| Keep central platform-host ownership | Rejected | Directly contradicts the human decision and the established sibling-module composition pattern. |
| Reduce the PRD or preservation scope | Rejected | The product and preservation requirements remain achievable and are not the source of the conflict. |

### Effort, Risk, and Timeline

- **Planning effort:** Medium. Architecture, Epic 6 authority, Story 6.2, derived context, documentation, and conformance guards must be reconciled.
- **Implementation effort:** Low-to-medium for AppHost ownership because the composition root already exists; projection population and evidence work remain the larger Story 6.2 effort.
- **Risk:** Medium-high until the universal baseline is corrected. Conflicting agent instructions are a repeat-error risk.
- **Release impact:** No readiness or release claim may advance until v3 authority is approved, Story 6.2 is re-planned, and existing Epic 6 gates pass.

## 5. Detailed Change Proposals

### A1 — Architecture Target Ownership

**OLD (v2)**

> The platform AppHost owns topology and deployment composition. The local Conversations AppHost and ServiceDefaults are pre-Story-6.2 drift and migration input only, and Story 6.2 removes them.

**NEW (proposed v3)**

> `Hexalith.Conversations.AppHost` owns the repository-specific composition of the Conversations runnable topology. It composes Conversations Server, optional Conversations Admin Web, and required platform resources through public EventStore/Commons Aspire helpers. EventStore and Commons continue to own reusable hosting, endpoints, service defaults, DAPR implementations, health and telemetry wiring, projection/query runtime, and subscription plumbing. `Hexalith.Conversations.AppHost` is target architecture; `Hexalith.Conversations.ServiceDefaults` is retained only if a separately justified domain-specific responsibility remains after shared defaults are consumed.

### A2 — Architecture FR-13 Landing Zone

**OLD (v2)**

> Public owner: Platform AppHost plus `Hexalith.EventStore.Aspire`; Conversations supplies module metadata and owns no AppHost, Aspire, DAPR, or publication facade.

**NEW (proposed v3)**

> Public capability owner: `Hexalith.EventStore.Aspire` and applicable Commons Aspire helpers expose `AddHexalithEventStore`, domain-module resource registration, security, DAPR reference, health, and publication composition surfaces. Conversations owns `Hexalith.Conversations.AppHost`, supplies module project metadata and module-specific topology choices, and owns no reusable Aspire/DAPR/publication facade.

### A3 — Architecture Target Tree

**OLD (v2)**

> The corrected target tree omits AppHost, AppHost tests, ServiceDefaults, and ServiceDefaults tests.

**NEW (proposed v3)**

```text
Hexalith.Conversations/
├── src/
│   ├── Hexalith.Conversations.Contracts/
│   ├── Hexalith.Conversations.Client/
│   ├── Hexalith.Conversations/
│   ├── Hexalith.Conversations.Server/
│   ├── Hexalith.Conversations.Admin.Web/
│   ├── Hexalith.Conversations.AppHost/          # repository-specific composition root
│   └── Hexalith.Conversations.Testing/
├── tests/
│   ├── Hexalith.Conversations.AppHost.Tests/    # topology/composition contract
│   ├── Hexalith.Conversations.Contracts.Tests/
│   ├── Hexalith.Conversations.Tests/
│   ├── Hexalith.Conversations.Server.Tests/
│   ├── Hexalith.Conversations.IntegrationTests/
│   └── Hexalith.Conversations.Conformance.Tests/
└── references/                                  # root-declared submodules only
```

The target tree omits a module-owned `Aspire` library. It also omits `ServiceDefaults` unless architecture review records a non-generic domain responsibility that cannot be expressed through shared options/metadata.

### E1 — Epic 6 Authority Overlay v3

**OLD (v2)**

- Story 6.1 AC 4 requires no Conversations-owned AppHost.
- FR-13 says 6.1 fixes platform AppHost ownership and 6.2 migrates topology.
- Historical disposition rows 2.1, 3.5, and 4.1 treat local AppHost ownership as defective.

**NEW (proposed v3 append-only disposition)**

- Keep v1/v2 text byte-identical as historical authority.
- Append `epic-6-authority-2026-07-26-v3` and declare that v3 supersedes v2 only for repository-specific AppHost ownership and directly dependent acceptance/evidence text.
- Record Story 6.1 as completed historical rebaseline work whose AppHost prohibition is superseded by the approved July 26 correction.
- Change FR-13 corrective coverage to: “v3 distinguishes repository composition ownership from shared Aspire capability ownership; 6.2 aligns and proves the Conversations AppHost; 6.6 verifies.”
- Change FR-17 corrective coverage to: “6.2 establishes the thin repository-owned composition root; 6.5 teaches and measures it.”
- Preserve `6.7 -> 6.2`, the SM-C2 pre-change gate, `6.2 -> 6.5`, and 6.6-last sequencing.

### E2 — Story 6.2

**OLD**

> **Title:** Migrate Conversations to platform-owned hosting
>
> **Intent:** Compose Conversations exclusively in a designated platform AppHost and remove local AppHost/ServiceDefaults projects.

**NEW (proposed)**

> **Title:** Align Conversations Repository-Owned AppHost with Shared Platform Hosting
>
> **Story:** As a Conversations maintainer, I want a thin repository-owned AppHost composed from public platform capabilities, so Conversations can run and be validated like other Hexalith modules without duplicating platform hosting infrastructure.

Proposed acceptance criteria:

1. The versioned pre-correction SM-C2 benchmark is captured before Story 6.2 runtime, projection, or topology changes, or is reproducibly reconstructed from the preserved source commit.
2. `Hexalith.Conversations.AppHost`, its composition tests, and solution entries remain. Its topology uses verified public EventStore/Commons helpers and preserves security, DAPR references, health, publication, Conversations Server, Admin Web, and public contracts.
3. No generic hosting, DAPR, defaults, telemetry, health, projection/query, or subscription capability is reimplemented behind a Conversations facade. `Hexalith.Conversations.ServiceDefaults` is removed if architecture review confirms it is only a generic wrapper.
4. The existing production named-projection handler/materializer/read-model-writer obligations remain exactly as established by Epic 6 v2 and ADR 0003.
5. `projection-read-store-population-proof-v2` still proves accepted append or authorized replay through the production EventStore named-dispatch boundary into durable per-conversation and duplicate-free tenant-index state, followed by the production query result.
6. Duplicate delivery, partial-write retry, tenant isolation, bounded failure, derived-state deletion, and replay convergence tests remain binding.
7. AppHost composition tests prove the repository-owned topology; Story 6.7 applies to every submodule actually changed or promoted. No move to FrontComposer.AppHost or EventStore.AppHost is performed.

The frozen spec's `Block If` condition “owning platform AppHost is not named” becomes satisfied by naming `src/Hexalith.Conversations.AppHost/` as the owner. Its `Never` rule changes from “never keep a Conversations AppHost” to “never move composition to a central host or duplicate reusable platform capability.”

### E3 — Story 6.5

**OLD (v2)**

> The template/fixture contains no domain-owned AppHost, Aspire, ServiceDefaults, or equivalent project.

**NEW (proposed)**

> The template/fixture contains one thin repository-owned AppHost that composes the module through verified live public platform APIs. It contains no module-owned reusable Aspire library, generic ServiceDefaults facade, DAPR implementation, projection/query runtime, or subscription plumbing. The AppHost's hand-authored files and LOC are included in SM-2.

Validators should require the AppHost composition contract and reject duplicated generic platform capability rather than rejecting the AppHost project name.

### E4 — Story 6.6

**OLD (v2)**

> Final evidence requires no Conversations-owned AppHost, Aspire, ServiceDefaults, or equivalent runtime project.

**NEW (proposed)**

> Final evidence requires one tested `Hexalith.Conversations.AppHost` composition root, no module-owned reusable Aspire/DAPR/publication facade, no unjustified generic ServiceDefaults wrapper, and no duplicate generic hosting, health, telemetry, projection/query, or subscription plumbing.

All projection-population, manifest, contract, SM-C2, SM-2, promotion, signed-v1 immutability, v2 attestation, and readiness-rerun gates remain unchanged.

### G1 — Universal Hexalith LLM Baseline (separate owning repository)

**OLD**

> A domain module must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project.

**NEW (proposed)**

> A domain module may ship one repository-specific `*.AppHost` project whose sole responsibility is composing that module's runnable topology from public platform resources and helpers. The AppHost may contain module-specific resource selection and configuration, but must not reimplement generic hosting, DAPR behavior, telemetry, health, projection/query runtime, publication, or subscription plumbing. Reusable `*.Aspire` and `*.ServiceDefaults` capability remains in its owning platform library unless an explicitly approved module-specific responsibility requires otherwise.

The workflow example should say that `aspire run` targets the repository's designated AppHost, which may live in the module repository or a separate host repository according to the approved architecture.

### V1 — Validation and Derived Artifacts

After authority approval, update or regenerate:

- `architecture.md` authorityVersion/correctionAuthority and affected sections.
- The append-only Epic 6 v3 overlay in `epics.md`.
- `epic-6-context.md` from v3 authority.
- The Story 6.2 frozen spec, including its AC/test matrix and code map.
- Architecture/planning conformance tests so they require the Conversations AppHost and reject duplicated generic capability.
- Authoring-template validators and evidence rules for the thin AppHost.
- Responsibility-boundary documentation, project context, local-run documentation, CI lanes, and release-evidence mappings that assert central-host ownership.

Do not weaken tests unrelated to AppHost ownership. Fault-inject at least these drifts: central-host ownership reintroduced; AppHost omitted; generic topology copied into Conversations; AppHost excluded from SM-2; projection-population obligations lost; and AppHost tests removed.

## 6. Implementation Handoff

### Scope Classification

**Major.** The requested change reverses current architecture and Epic 6 authority, changes the authoring template and success-metric denominator, and requires a cross-repository governance correction. Product and UX scope remain unchanged.

### Recipients and Responsibilities

| Role / owner | Responsibility |
| --- | --- |
| Product Manager / Product Owner | Approve the corrected interpretation without expanding feature scope; preserve completed history and signed evidence. |
| Platform / Solution Architect | Ratify the composition-versus-capability invariant; publish architecture v3 and Epic 6 overlay v3. |
| Hexalith.AI.Tools owner | Correct the universal AppHost prohibition and coordinate downstream project-context regeneration. |
| Conversations maintainer | Re-plan Story 6.2 around the existing AppHost; retain module-specific composition and remove only unjustified generic wrappers. |
| EventStore/Commons owners | Provide or confirm public helpers for any generic gap; do not absorb ownership of the Conversations composition root. |
| Quality / Test Architect | Update ownership conformance, preserve projection and performance gates, and include AppHost cost in SM-2. |
| Release owner | Bind v3 authority and corrected topology evidence into v2 superseding evidence without mutating v1. |

### Required Sequence

1. Jerome reviews and explicitly approves, rejects, or revises this proposal.
2. The architecture owner and AI.Tools owner reconcile the v3 authority and universal baseline. These are prerequisites for implementation.
3. Regenerate Epic 6 context and update semantic conformance guards.
4. Resume `bmad-loop-resolve`; minimally amend the frozen Story 6.2 spec and affected AC/test matrix, then write the run's `resolution.json` last.
5. Execute Story 6.7 where affected promotions require it and freeze/capture the SM-C2 baseline.
6. Implement the corrected Story 6.2 and its production projection proof.
7. Update Story 6.5 evidence and complete Story 6.6 final gates.

### Sprint Status

No sprint-status edit is required to express the decision: Story 6.2 remains the implementation story and no new story key is introduced. The current resolver session must not alter sprint status. If the Product Owner later needs to record an authority-correction activity, that update is outside this resolver session and must preserve existing historical states.

### Success Criteria

- Architecture v3 names `Hexalith.Conversations.AppHost` as the target repository-specific composition root.
- Epic 6 v3 supersedes v2 AppHost prohibitions without rewriting v1/v2 or completed story history.
- The universal Hexalith baseline permits a thin repository-owned AppHost and still prohibits duplicated generic runtime capability.
- Story 6.2 no longer asks the implementation agent to choose or modify FrontComposer.AppHost or EventStore.AppHost.
- AppHost source, tests, and solution entries remain and prove topology, security, DAPR references, health, publication, Server, and Admin Web composition.
- No unjustified generic Conversations Aspire/ServiceDefaults/DAPR/runtime facade is introduced or retained.
- Production projection population, replay convergence, tenant isolation, promotion completeness, and SM-C2 gates remain intact.
- SM-2 includes the thin AppHost's actual authoring cost.
- UX behavior and public Conversations contracts remain unchanged.
- Signed v1 evidence remains byte-identical and final readiness still requires all Epic 6 gates.

### Non-Goals

- No new Conversations product feature or UX component delivery.
- No centralized fleet AppHost redesign.
- No blanket approval of module-owned Aspire or ServiceDefaults libraries.
- No weakening of tenant isolation, governance, redaction, replay, idempotency, projection, performance, or evidence requirements.
- No retroactive rewrite of completed stories, historical Epic 6 overlays, retrospectives, or signed evidence.
- No code implementation, test execution, dependency update, commit, push, or sprint-status mutation in this proposal workflow.

## Change Navigation Checklist Record

### Section 1 — Trigger and Context

- [x] 1.1 Triggering Story 6.2 escalation identified.
- [x] 1.2 Human decision recorded: Conversations owns its AppHost.
- [x] 1.3 Evidence from current repository, sibling modules, and prior authority recorded.

### Section 2 — Epic Impact

- [!] 2.1 Current Story 6.2 cannot proceed under v2 authority.
- [x] 2.2 Epic 6 remains viable through append-only v3 correction.
- [x] 2.3 Stories 6.1, 6.2, 6.5, and 6.6 have explicit dispositions.
- [x] 2.4 Completed history remains immutable.
- [x] 2.5 Existing corrective sequencing is preserved with v3 authority as a prerequisite.

### Section 3 — Artifact Impact

- [x] 3.1 PRD remains unchanged and achievable.
- [!] 3.2 Architecture and conformance authority require correction.
- [x] 3.3 UX behavior and UX requirements are unchanged.
- [!] 3.4 Epic overlay, Story 6.2, authoring template, evidence, docs, and cross-repository baseline are affected.

### Section 4 — Path Forward

- [x] 4.1 Direct adjustment selected; medium planning effort and medium-high coordination risk.
- [N/A] 4.2 Rollback rejected.
- [N/A] 4.3 PRD/MVP reduction rejected.
- [x] 4.4 Recommended path documented.

### Section 5 — Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic and artifact impacts complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 Explicit old-to-new proposals complete.
- [x] 5.5 Major-scope handoff and success criteria complete.

### Section 6 — Final Review and Handoff

- [x] 6.1 Jerome reviewed the complete batch proposal.
- [x] 6.2 Jerome continued without requesting revisions.
- [x] 6.3 Explicit final approval received on 2026-07-26.
- [N/A] 6.4 No sprint-status update is required or permitted in the current resolver session.
- [x] 6.5 Major-scope handoff activated for the named owners.

## Final Review Decision

- **Reviewer:** Jerome
- **Decision:** Approved for major-scope architecture handoff
- **Approval date:** 2026-07-26
- **Scope:** Major
- **First post-approval action:** Correct architecture/Epic 6 authority and the universal AppHost baseline before resuming Story 6.2 resolution.
