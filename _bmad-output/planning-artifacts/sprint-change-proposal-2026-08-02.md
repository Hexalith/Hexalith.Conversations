---
workflow: bmad-correct-course
status: approved
date: 2026-08-02
project: Conversations
mode: Batch
changeScope: Major
recommendedApproach: Direct Adjustment through append-only replanning
implementationHold: active
approval: approved
approvedBy: Jerome
approvedOn: 2026-08-02
triggerReports:
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01-rerun.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-08-02.md
---

# Sprint Change Proposal — Implementation-Readiness Plan Correction

## 1. Issue Summary

### Trigger

The independent implementation-readiness assessments dated 2026-08-01 and 2026-08-02 both returned **NOT READY**. They confirmed complete functional-requirement traceability and broad PRD, Architecture, and UX alignment, but found blocking defects in the executable plan.

This change is triggered by the readiness verdict rather than by a single implementation story. Stories 6.3 and 6.8 were marked `in-progress`, Story 6.12 was marked `ready-for-dev`, and the remaining unfinished Epic 6 stories were in `backlog` when Architecture v8 imposed the global implementation hold.

### Evidence

- All 124 functional requirements are mapped: 20 initiative FRs and 104 preserved product Feature-FRs.
- Epic 6 combines distinct outcomes for completion tooling, UX governance, conformance, evidence integrity, authoring cost, performance, projection assurance, preservation traceability, and release attestation.
- Earlier-numbered Stories 6.3, 6.4, and 6.5 require outputs from later-numbered stories.
- Stories 6.5, 6.8, 6.10, 6.11, and 6.12 contain multiple independently reviewable implementation, evidence, or rollback boundaries.
- Active acceptance criteria are detailed but compound; many do not bind one atomic assertion to exact inputs, commands, output schemas, result semantics, blocker codes, and candidate identity.
- Story 6.6 is a release-program exit gate rather than an independently completable user story.
- Architecture v8 explicitly prohibits starting or resuming remaining implementation until the plan is corrected, mechanically validated, and independently reassessed as `READY`.

### Problem statement

The product contract is complete enough for implementation, but the current execution plan is not. Implementation cannot safely resume while unrelated outcomes share one epic boundary, story identifiers do not express executable order, oversized stories mix rollback units, and acceptance contracts permit interpretation at execution time.

### Non-goals

- Do not change the PRD, its 124-FR denominator, FR-16 deferral, SM-1 baseline, or universal SM-C2 rule.
- Do not activate product UI scope or change the 52 preserved UX decisions or 28 UX acceptance IDs.
- Do not reopen or rewrite Epics 1–5 or completed Stories 6.1, 6.2, and 6.7.
- Do not roll back accepted implementation or signed evidence.
- Do not start or resume product implementation as part of this correction.

## 2. Impact Analysis

### Epic impact

| Authority area | Impact | Required disposition |
| --- | --- | --- |
| Epics 1–5 | Completed history remains valid | Preserve byte history and status; no replanning |
| Epic 6 completed spine | Stories 6.1, 6.2, and 6.7 remain accepted prerequisites | Retain identifiers, records, evidence, and `done` status |
| Epic 6 unfinished work | Current epic is not independently valuable or executable as one unit | Supersede only the unfinished v8 execution definitions through an append-only v9 overlay |
| New execution plan | Distinct remaining outcomes need independent completion boundaries | Add outcome Epics 7–15 with topologically ordered stories |
| Release closure | Story 6.6 mixes implementation with an external decision | Isolate bounded attestation work in Epic 15 and model readiness/release decisions as gates |

Epic 6 can no longer be completed as originally planned. Under v9 it becomes the historical corrective foundation containing the immutable completed spine. Its unfinished obligations move to successor outcome epics without deleting v8 provenance.

### Story disposition map

Every current Story 6.x identifier has one explicit disposition. An oversized unfinished story maps to one successor epic and then to independently completable stories inside that epic.

| Current story | Current state | v9 disposition | Successor |
| --- | --- | --- | --- |
| 6.1 | done | Immutable history | 6.1 |
| 6.2 | done | Immutable history | 6.2 |
| 6.7 | done | Immutable history | 6.7 |
| 6.8 | in-progress, held | Supersede executable definition; preserve partial work as unaccepted input | Epic 7 / Stories 7.1–7.4 |
| 6.4 | backlog, held | Supersede executable definition | Epic 8 / Stories 8.1–8.2 |
| 6.9 | backlog, held | Supersede executable definition | Epic 9 / Stories 9.1–9.2 |
| 6.10 | backlog, held | Supersede executable definition | Epic 10 / Stories 10.1–10.4 |
| 6.5 | backlog, held | Supersede executable definition | Epic 11 / Stories 11.1–11.3 |
| 6.11 | backlog, held | Supersede executable definition | Epic 12 / Stories 12.1–12.4 |
| 6.12 | ready-for-dev, held | Supersede executable definition; preserve its story file as provenance | Epic 13 / Stories 13.1–13.3 |
| 6.3 | in-progress, held | Supersede executable definition; preserve partial work as unaccepted input | Epic 14 / Stories 14.1–14.3 |
| 6.6 | backlog, held | Reclassify release-gate semantics and supersede story definition | Epic 15 / Stories 15.1–15.2 plus Gate RG-15 |

No partial work inherited from 6.3, 6.8, or 6.12 is accepted merely because it exists. A successor story may reuse it only after its own candidate-bound acceptance contract validates it.

### Artifact conflicts and required adjustments

| Artifact | Conflict | Proposed treatment |
| --- | --- | --- |
| PRD and addendum | None | No normative change |
| `epics.md` | v8 mega-epic, forward-numbered dependencies, oversized stories, compound ACs | Append v9 successor authority and old-to-new mapping |
| `architecture.md` | v8 hold and execution graph point to the non-ready plan | Append v9 planning overlay; preserve all system invariants and the hold |
| UX design specification | No semantic conflict; execution references point to Story 6.4 | Rebind planned governance work to Stories 8.1–8.2 only |
| UX requirement map | Story bindings become stale after renumbering | Regenerate with identical 52-decision and 28-acceptance denominators |
| Current execution view | v1 projects the v8 graph | Generate a v2 view from v9 authority; retain v1 as provenance |
| Sprint status | Held `in-progress` and `ready-for-dev` labels refer to superseded units | Replace active unfinished 6.x keys with backlog successor keys after approval |
| Existing 6.3, 6.8, and 6.12 files | Contain useful partial or prepared work but are no longer executable authority | Preserve them and publish a machine-readable supersession map rather than rewriting history |
| Planning validators | Validate v8 identities and structure | Add v9 identity, mapping, topological-order, atomic-contract, and cross-artifact checks |
| Product code, infrastructure, deployment | No product change is authorized | Leave unchanged during the planning correction |

### Technical impact

The correction changes planning authority, generated projections, story specifications, and planning validation only. It does not change runtime components, API contracts, persistence, EventStore authority, tenant isolation, hosting ownership, FrontComposer conventions, or deployment topology.

The working tree already contains pending v8 planning and story work. Any approved v9 publication must preserve those changes, bind the exact candidate it validates, and avoid replacing user-authored or accepted evidence bytes.

## 3. Recommended Approach

### Option evaluation

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable only as an append-only fundamental replan | High planning effort | Medium after mechanical validation | **Selected** |
| Potential Rollback | No completed work caused the planning defects | High | High; would destroy accepted history without simplifying the plan | Reject |
| PRD/MVP Review | Requirements and product scope are aligned and fully traced | Medium | High; would solve the wrong problem | Reject |

### Selected path

Publish an append-only v9 planning correction that:

1. Preserves completed history and every existing requirement denominator.
2. Converts Epic 6 into a historical completed foundation.
3. Moves each unfinished obligation to one outcome-oriented successor epic.
4. Splits implementation, migration, evidence, and fault-injection boundaries into 27 successor stories.
5. Numbers all stories topologically and records explicit predecessor sets.
6. Rewrites acceptance contracts as atomic, mechanically verifiable scenarios.
7. Separates bounded attestation generation from plan-readiness and release-owner decisions.
8. Regenerates all planning projections and validates zero-gap mappings.
9. Keeps the v8 hold active until Gate IR-0 returns a fresh independent `READY` verdict against the corrected candidate.

### Scope and schedule classification

- **Change scope:** Major — fundamental replan requiring Product Manager and Solution Architect ownership.
- **Product/MVP scope:** Unchanged.
- **Implementation estimate:** Deferred until successor stories are accepted and Gate IR-0 is `READY`.
- **Schedule impact:** The active sprint remains paused. The next executable date is gated by v9 publication, mechanical validation, and independent readiness; no calendar forecast should bypass those gates.
- **Risk if adopted:** Medium. The main risks are mapping loss, stale cross-artifact references, and accidental acceptance of partial work; all receive mechanical gates.
- **Risk if rejected:** Critical. Implementation would violate Architecture v8 and repeat the readiness failures.

## 4. Detailed Change Proposals

### 4.1 Canonical epic authority

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

**OLD — active v8 execution model:**

- One Epic 6 owns all remaining corrective outcomes.
- Stories 6.3–6.6 have completion dependencies on later-numbered Stories 6.8–6.12.
- Stories 6.5, 6.8, 6.10, 6.11, and 6.12 each contain multiple delivery and rollback boundaries.
- Story 6.6 contains both attestation implementation and external readiness/release decisions.

**NEW — proposed v9 execution model:**

| Epic | User/release outcome | Entry predecessors | Exit condition |
| --- | --- | --- | --- |
| 6 — Historical Corrective Foundation | Maintainers retain the accepted hosting and promotion-control foundation | Epics 1–5 | Stories 6.1, 6.2, and 6.7 remain immutable and done |
| 7 — Reliable Mechanical Completion Records | Developers receive deterministic, candidate-bound completion records | Completed Story 6.2 | Stories 7.1–7.4 done |
| 8 — Preserved UX Governance | Product and release owners receive zero-gap UX preservation dispositions without UI activation | Epic 7 | Stories 8.1–8.2 done |
| 9 — Portable Conformance Oracle | Domain authors receive an objectively tiered, structurally portable oracle | Epic 7 | Stories 9.1–9.2 done |
| 10 — Unified Evidence Boundary | Reviewers receive one hardened evidence-integrity boundary across every workflow | Epics 7 and 9 | Stories 10.1–10.4 done |
| 11 — Thin-Module Authoring Proof | Domain authors receive corrected guidance, a minimal live fixture, and reproducible SM-2 evidence | Epics 7 and 10; completed Story 6.2 | Stories 11.1–11.3 done |
| 12 — Universal Performance Restoration | Operators receive correctness-preserving projection performance under the universal SM-C2 rule | Completed Story 6.2 | Stories 12.1–12.4 done |
| 13 — Current Projection-Proof Lifecycle | Release owners receive immutable historical validation and successor-bound current assurance | Epics 7 and 9 | Stories 13.1–13.3 done |
| 14 — Complete Preservation Manifest | Release owners receive a zero-gap, candidate-bound manifest across every preserved contract | Epics 8, 9, 10, and 13 | Stories 14.1–14.3 done |
| 15 — Superseding Release Attestation | Release owners receive bounded revalidation evidence and a signable superseding attestation | Epics 7–14 | Stories 15.1–15.2 done; Gate RG-15 decided |

**Rationale:** Each epic now has one stakeholder outcome, one bounded exit, and dependencies only on completed or lower-numbered epics.

### 4.2 Topologically ordered successor stories

The following titles and predecessor sets are proposed as the v9 authority skeleton. Each story owns a separate final record and rollback boundary.

| Story | Bounded outcome | Hard predecessors |
| --- | --- | --- |
| 7.1 | Define final-record schema and deterministic generator core | 6.2 |
| 7.2 | Derive test, path, candidate, submodule, and gitlink facts | 7.1 |
| 7.3 | Integrate the generator into every completion workflow and blocking transition | 7.2 |
| 7.4 | Verify historical mode and required fault-injection blockers | 7.3 |
| 8.1 | Generate versioned UX disposition schema, JSON, and Markdown | 7.4 |
| 8.2 | Enforce the 52-decision/28-acceptance zero-gap UX validator | 8.1 |
| 9.1 | Freeze the conformance assertion inventory, tier decisions, digest, and approvals | 7.4 |
| 9.2 | Make the portable tier structural and prove complete monotonic tier execution | 9.1 |
| 10.1 | Provide neutral TestSupport helpers and a safe Git-facts runner | 7.4, 9.2 |
| 10.2 | Implement manifest, hash, ledger, exact-diff, and gitlink invariants | 10.1 |
| 10.3 | Provide the evidence-boundary verifier and integrate every workflow surface | 10.2 |
| 10.4 | Migrate frozen readers, repair gate spans, publish the runbook, and prove fault injection | 10.3 |
| 11.1 | Correct and validate platform-hosted thin-module authoring guidance | 6.2, 7.4, 10.4 |
| 11.2 | Build and verify the minimal fixture against live platform APIs | 11.1 |
| 11.3 | Generate and accept reproducible SM-2 evidence | 11.2 |
| 12.1 | Approve derived-key ownership, compatibility, rebuild, deletion, and rollback ADR | 6.2 |
| 12.2 | Freeze the benchmark method and signal-quality algorithm before production changes | 12.1 |
| 12.3 | Implement correctness-preserving list/open optimization and migration/replay behavior | 12.1, 12.2 |
| 12.4 | Produce candidate-bound evidence and enforce universal SM-C2 | 12.3 |
| 13.1 | Validate historical proof and approve predecessor-chain ADR/schema | 7.4, 9.2 |
| 13.2 | Generate the current successor proof and enforce drift/current-head guards | 13.1 |
| 13.3 | Prove fault injection and bind manifest, conformance, handoff, and final record | 13.2 |
| 14.1 | Freeze complete requirement, contract, test, UX, and evidence denominator inventories | 8.2, 9.2, 10.4, 13.3 |
| 14.2 | Bind dispositions, owners, approvals, evidence, tiers, proof chains, and candidate identity | 14.1 |
| 14.3 | Run zero-gap validation and generate the manifest final record | 14.2 |
| 15.1 | Revalidate all completed preservation, topology, correctness, and metric gates | 7.4, 8.2, 9.2, 10.4, 11.3, 12.4, 13.3, 14.3 |
| 15.2 | Generate the superseding attestation and explicit predecessor-supersession record | 15.1 |

**Rationale:** No successor story requires a later-numbered story. Decisions and frozen measurement contracts precede implementation; implementation precedes migration/evidence closure; attestation is the final executable story.

### 4.3 Gate model

**OLD:** The v8 global hold, normal story statuses, Story 6.6, and the readiness decision are described across the same execution plan.

**NEW:** Use two explicit non-story gates.

| Gate | Timing | Required result | Effect |
| --- | --- | --- | --- |
| IR-0 — Corrected Plan Readiness | After v9 publication and mechanical validation; before any successor implementation | Independent assessment returns `READY` against the exact committed candidate | Only this result may lift the Architecture v8/v9 implementation hold |
| RG-15 — Release Closure | After Story 15.2 | Independent release review and explicit release-owner decision are recorded without predetermining the outcome | Closes or reopens the release; it is not a developer story |

Failure, `NOT READY`, missing evidence, candidate drift, or inability to execute a required check leaves the relevant gate closed.

### 4.4 Atomic acceptance-contract standard

**OLD:** Active v8 stories use detailed numbered criteria and a shared high-risk BDD catalogue, but individual criteria may combine multiple assertions or depend on dynamic phrases such as “any reader,” “unchanged strength,” or “usable signal.”

**NEW:** Every v9 story must satisfy the following publication contract before it can appear in `sprint-status.yaml`.

1. Every acceptance scenario has a stable ID in the form `AC-<epic>.<story>-<two-digit-sequence>`.
2. One scenario asserts one outcome. Compound assertions become separate scenario IDs even when they share a command.
3. `Given` names exact authority identities, candidate identity rules, input paths, schema versions, frozen inventories, and SHA-256 digests.
4. `When` contains one exact non-interactive command, including project, filter, arguments, and working directory.
5. `Then` declares exact exit-code meaning, machine result (`PASS`, `FAIL`, or `BLOCKED`), output path, schema identity, required fields, blocker codes, and candidate/gitlink bindings.
6. Required test lanes pass with zero failed, zero skipped, and zero not-run tests. Environmental inability is `BLOCKED`, never `PASS`.
7. Every mutation story includes at least one named negative or fault-injection scenario proving the intended blocker.
8. Every story freezes its file/inventory baseline at entry; phrases such as “any item added later” are prohibited.
9. Every migrated assertion binds a before/after inventory and strength digest; silent weakening or deletion fails.
10. Every story declares its rollback boundary and the artifacts that remain immutable if rollback occurs.
11. The Epic 7 generator emits the authoritative story record from measured state. Hand-copied counts, commits, file lists, or verdicts are prohibited.
12. The shared high-risk catalogue remains cross-story coverage and does not replace story acceptance contracts.

The canonical machine record must contain, at minimum:

```yaml
schemaVersion: <exact-version>
storyId: <exact-id>
candidate:
  rootCommit: <sha>
  worktreePolicy: <exact-policy>
  gitlinks: <sorted path/commit list>
inputs:
  - path: <repository-relative-path>
    sha256: <digest>
scenarios:
  - id: <stable-ac-id>
    command: <exact-command>
    exitCode: <integer>
    result: PASS|FAIL|BLOCKED
    blockers: <sorted stable codes>
    outputs: <sorted path/schema/digest bindings>
summary:
  required: <integer>
  passed: <integer>
  failed: <integer>
  blocked: <integer>
  skipped: <integer>
  notRun: <integer>
```

The v9 planning validator must reject a story missing any field, any referenced inventory digest, any exact command, or any atomic scenario binding.

### 4.5 PRD and addendum

**Artifacts:** `prd.md`, `addendum.md`

**OLD:** 124 mapped functional requirements, FR-16 deferred, SM-1 fixed at the accepted 13,289 LOC baseline, and the universal SM-C2 rule fixed at post P95 no greater than 1.05 times baseline P95.

**NEW:** No change.

**Rationale:** The readiness failures concern decomposition and acceptance precision, not missing or conflicting product requirements. Changing the denominator would conceal the planning problem.

### 4.6 Architecture

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**OLD:** v8 records the correct system invariants and a global `NOT READY` hold but projects the remaining work through the current Epic 6 graph.

**NEW:** Append an Architecture v9 planning overlay that:

- references the v9 epic authority identity and supersession map;
- replaces only the remaining-work execution graph with Epics 7–15 and Gates IR-0/RG-15;
- declares v8 system ownership, EventStore authority, tenant fail-closed behavior, public-contract preservation, UX non-activation, AppHost/test-harness limits, performance rule, and completed-history protections unchanged;
- states that normal story statuses cannot override the global hold;
- permits hold release only when the mechanical v9 validator passes and an independent IR-0 report says `READY` for the same candidate;
- treats candidate or authority drift after the report as requiring reassessment.

**Rationale:** Architecture needs a current execution projection, not a new technical design.

### 4.7 UX specification and requirement map

**Artifacts:** `ux-design-specification.md`, `ux-requirement-map.md`

**OLD:** Planned UX preservation governance is assigned to Story 6.4.

**NEW:**

- Rebind artifact generation to Story 8.1 and zero-gap validation to Story 8.2.
- Keep all 52 UX decision identities and all 28 UX acceptance identities byte-for-byte stable.
- Keep status `preserved-not-activated`.
- Add no screen, component, navigation, interaction, or visual implementation scope.
- Regenerate the requirement map and fail on missing, duplicate, or orphan binding.

**Rationale:** Only execution ownership changes; UX meaning does not.

### 4.8 Execution projections and supersession record

**OLD:** `epic-6-current-execution-view-v1.md` deterministically projects v8 and current Story 6.x identifiers.

**NEW:**

- Retain v1 unchanged as provenance.
- Add a generated v2 current execution view sourced solely from v9 authority.
- Add a machine-readable v9 supersession map containing old ID, source status, successor epic, successor story IDs, preserved artifacts, and salvage policy.
- Require zero missing and zero duplicate old-story dispositions.
- Reject any view or map whose authority digest differs from the canonical v9 overlay.

**Rationale:** Generated views must not silently amend canonical authority or erase partial-work provenance.

### 4.9 Sprint status

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:** Epic 6 is `in-progress`; 6.3 and 6.8 are `in-progress`; 6.12 is `ready-for-dev`; the other unfinished 6.x stories are `backlog`; comments state that all are held.

**NEW — only after explicit approval and v9 publication:**

- Retain Stories 6.1, 6.2, and 6.7 as `done`.
- Remove superseded unfinished 6.x keys from the active `development_status` projection; preserve their source states in the supersession map.
- Project Epic 6 as the completed historical foundation.
- Add Epics 7–15 and Stories 7.1–15.2 as `backlog`.
- Add each new epic retrospective as `optional`.
- Keep the global implementation hold comment prominent.
- Do not infer `ready-for-dev` or `in-progress` from partial predecessor work.
- Regenerate rather than hand-edit story counts or dependency summaries.

**Rationale:** Schema-valid statuses must describe executable authority. Prose holds cannot make stale active identifiers safe.

### 4.10 Planning validation

**OLD:** Current conformance tests validate v8 authority identity, cross-artifact references, and selected invariants.

**NEW:** Extend planning validation to fail mechanically when any of the following is true:

1. Epics 1–5 or completed Stories 6.1, 6.2, or 6.7 change outside an explicitly authorized immutable-reference update.
2. Any unfinished v8 story lacks exactly one successor-epic disposition.
3. Any successor story depends on an equal or greater story number or on a greater epic number.
4. The graph contains a cycle.
5. A story owns more than one declared rollback boundary or lacks a separate final record.
6. An acceptance scenario lacks a stable ID, exact command, frozen input digest, output schema, exit/result semantics, blocker codes, or candidate binding.
7. PRD FR coverage differs from 124/124 or any current path is missing or duplicated.
8. UX coverage differs from 52 decisions and 28 acceptance IDs.
9. Epics, Architecture, execution view, supersession map, UX map, or sprint status reference different authority identities.
10. A required test is failed, skipped, not run, stale, or bound to another candidate.
11. The v8/v9 implementation hold is absent before a candidate-matched independent `READY` report exists.

**Rationale:** The correction is complete only when the same defects cannot recur through prose interpretation or stale projections.

## 5. Implementation Handoff

### Scope and recipients

This is a **Major** change.

| Recipient | Responsibility |
| --- | --- |
| Product Manager | Confirm outcome-epic boundaries, unchanged PRD scope, and old-to-new obligation coverage |
| Solution Architect | Publish the append-only v9 architecture/execution overlay and preserve all v8 technical invariants |
| Product Owner | Accept story ordering, backlog replacement, gate semantics, and sprint-status projection |
| Test Architect / Quality owner | Define the atomic acceptance-contract validator and independent IR-0 assessment boundary |
| Developer/workflow owner | Regenerate deterministic projections and implement planning validators only; do not resume product work |
| Release owner | Approve the corrected plan, decide hold release only from a candidate-matched `READY`, and later decide RG-15 |

### Publication sequence

1. Obtain explicit approval for this proposal.
2. Freeze the v9 publication candidate and inventories; record their digests.
3. Append the v9 authority overlays to Epics and Architecture without rewriting completed history.
4. Publish the supersession map and atomic successor story specifications.
5. Rebind and regenerate the UX map, execution view, and sprint-status projection.
6. Extend and run planning-authority validators.
7. Resolve every mechanical failure without weakening denominators or assertions.
8. Run an independent implementation-readiness assessment against the exact corrected candidate.
9. Keep the implementation hold if the result is anything other than `READY`.
10. If and only if IR-0 is candidate-matched `READY`, obtain the release-owner hold-lift decision and estimate/sequence implementation.

### Success criteria

- `SC-01`: v9 is append-only and completed history/evidence remains unchanged.
- `SC-02`: PRD coverage remains exactly 124/124 with no orphan or duplicate path.
- `SC-03`: Every unfinished v8 Story 6.x obligation has one successor-epic disposition and no obligation is lost.
- `SC-04`: Epics 7–15 each state one stakeholder outcome and one bounded exit.
- `SC-05`: Stories 7.1–15.2 form an acyclic, topologically numbered graph.
- `SC-06`: Each successor story has one rollback boundary, one final record, and atomic machine-verifiable acceptance scenarios.
- `SC-07`: Stories 6.5, 6.8, 6.10, 6.11, and 6.12 are decomposed at the required contract, implementation, migration, evidence, and fault-injection boundaries.
- `SC-08`: Story 6.6 implementation work is bounded in Epic 15 and external decisions are non-story gates.
- `SC-09`: UX remains `preserved-not-activated` with exactly 52 decision and 28 acceptance bindings.
- `SC-10`: All canonical and generated artifacts reference the same v9 identity and candidate.
- `SC-11`: Mechanical validation passes with zero failures, skips, not-run checks, stale evidence, or candidate drift.
- `SC-12`: An independent IR-0 assessment returns `READY` before any implementation starts or resumes.

### Checklist record

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the independent readiness assessment, not one story |
| 1.2 Core problem | [x] | Execution-plan structure and acceptance precision |
| 1.3 Supporting evidence | [x] | Both readiness reports and canonical planning artifacts reconciled |
| 2.1 Current epic viability | [x] | Epic 6 cannot complete as the current executable container |
| 2.2 Required epic changes | [x] | Preserve foundation; add outcome Epics 7–15 |
| 2.3 Remaining-epic impact | [x] | All unfinished obligations mapped |
| 2.4 Obsolete/new epics | [x] | Unfinished Epic 6 execution superseded; nine successor epics needed |
| 2.5 Ordering | [x] | Proposed graph is topological |
| 3.1 PRD conflict | [x] | No PRD or MVP change |
| 3.2 Architecture conflict | [!] | v9 planning overlay required; system design unchanged |
| 3.3 UX conflict | [!] | Story bindings must change; semantics remain unchanged |
| 3.4 Other artifacts | [!] | Execution view, supersession map, sprint status, and validators require updates |
| 4.1 Direct Adjustment | [x] Viable | High planning effort; selected |
| 4.2 Rollback | [x] Not viable | Accepted history is not the cause |
| 4.3 PRD/MVP Review | [x] Not viable | Scope is complete and aligned |
| 4.4 Recommended path | [x] | Append-only v9 fundamental replan |
| 5.1 Issue summary | [x] | Included |
| 5.2 Impact and artifacts | [x] | Included |
| 5.3 Approach and trade-offs | [x] | Included |
| 5.4 MVP and action plan | [x] | Unchanged MVP; gated sequence included |
| 5.5 Handoff | [x] | PM, Architect, PO, Quality, Developer, and release-owner responsibilities defined |
| 6.1 Checklist review | [x] | All applicable items addressed |
| 6.2 Proposal accuracy | [x] | Cross-checked against PRD, addendum, Epics, Architecture, UX, and both reports |
| 6.3 Explicit approval | [x] | Approved by Jerome on 2026-08-02 |
| 6.4 Sprint-status update | [!] | Routed to the publication sequence; prohibited until v9 authority is published |
| 6.5 Handoff confirmation | [x] | Major-change handoff assigned below |

## Approval Record

**Status:** Approved by Jerome on 2026-08-02.

Approval of this document authorizes the planning-authority correction and backlog reorganization described above. It does not authorize product implementation, hold bypass, evidence rewriting, commits, pushes, or release closure.

## Workflow Execution Log

- **Issue addressed:** Implementation-readiness failure caused by Epic 6 structure, forward-numbered dependencies, oversized stories, compound acceptance contracts, and the Architecture v8 hold.
- **Change scope:** Major.
- **Artifact modified by this workflow:** This Sprint Change Proposal only.
- **Primary handoff:** Product Manager and Solution Architect for the append-only v9 authority correction.
- **Supporting handoff:** Product Owner, Test Architect/Quality owner, workflow owner, and release owner for backlog projection, atomic-contract validation, mechanical checks, and gate decisions.
- **Approval:** Jerome approved the proposal on 2026-08-02.
- **Publication state:** Not started by this workflow. Epics, Architecture, UX mappings, execution projections, story specifications, and sprint status remain unchanged pending the approved publication sequence.
- **Implementation state:** Globally held. No product implementation may start or resume before a candidate-matched IR-0 `READY` result and release-owner hold-lift decision.
