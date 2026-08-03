# Sprint Change Proposal — Canonicalize Stories 6.10 and 6.11

**Date:** 2026-08-01
**Project:** Hexalith.Conversations
**Mode:** Incremental
**Status:** Approved for implementation
**Raised by:** Implementation-readiness assessment
**Trigger report:** `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01.md`
**Proposed overlay:** `epic-6-authority-2026-08-01-v8`
**Proposed architecture:** `conversations-architecture-2026-08-01-v8`

## 1. Issue Summary

### 1.1 Trigger

No implementation story triggered this correction. The 2026-08-01 implementation-readiness
assessment found the active corrective plan **NOT READY** because Stories 6.10 and 6.11 are
referenced by current authority and sprint tracking but do not have canonical,
implementation-ready definitions in the append-only Epic 6 authority chain.

This is a planning-authority publication failure, not a missing-requirements problem, product
pivot, or failed implementation approach.

### 1.2 Evidence

- The current v7 Epic 6 amendment says Stories 6.10 and 6.11 retain approved scope and ordering,
  but `epics.md` contains no `### Story 6.10:` or `### Story 6.11:` definition.
- Story 6.10 has an approved ten-criterion proposal in
  `sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md`. Its proposed v6
  amendment was never appended, and the v6 identifier was subsequently used by the approved
  Story 6.11 threshold amendment.
- Story 6.11 appears only as a short follow-up description and a one-line v6 disposition. Its
  planned performance optimization does not carry its own complete correctness-preservation
  envelope.
- Sprint tracking contains both identifiers at `backlog`, while the planning-authority
  conformance test validates Stories 6.1–6.9 and then special-cases Story 6.12, mechanically
  ignoring the missing definitions.
- All 124 current functional requirements have traceable epic coverage. The PRD preserves 20
  initiative FRs and 104 Feature-FRs; no requirement addition, deletion, or reinterpretation is
  needed.
- The obsolete live product contract was removed. Its byte-identical archived copy remains at
  `_bmad-output/archive/conversations-product-contract-2026-05-31.md`, SHA-256
  `a5d0ebef4f6565a87ae29cf378a811ff0d1b30423dcb71ecde180775bb373abb`.
- Signed v1 evidence remains unchanged, and its validator resolves the archived source.

### 1.3 Required outcome

Append one non-colliding v8 authority amendment after v7 that:

1. Publishes complete canonical definitions for Stories 6.10 and 6.11.
2. Defines exact dependency and completion semantics.
3. Updates architecture correction authority and the derived Epic 6 context.
4. Synchronizes affected live story guidance without changing implementation status.
5. Makes dangling, duplicated, or untracked story identifiers fail mechanically.
6. Preserves every prior authority block, completed record, archived contract, and signed evidence
   byte-for-byte.

## 2. Impact Analysis

### 2.1 Epic impact

Only Epic 6 requires current-plan changes. Epics 1–5, their 24 completed stories,
retrospectives, accepted baselines, and signed evidence remain immutable history. No new epic is
required, no epic becomes obsolete, and the MVP remains achievable.

Epic 6 remains the active corrective plan. Its product and preservation scope does not expand;
v8 repairs authority completeness and implementation ordering.

### 2.2 Story impact

| Story | Impact |
| --- | --- |
| 6.1 | None. Completed authority history remains unchanged. |
| 6.2 | None. Remains `done`; its record, v2 proof, generated final record, and signed-v1 dependencies remain protected from rewrite. |
| 6.3 | Scope unchanged. Completion requires both Stories 6.10 and 6.12, and its current authority bindings advance to v8. |
| 6.4 | None. UX provenance and preservation work remains backlog and outside this correction. |
| 6.5 | Scope unchanged. Completion additionally requires Story 6.10. |
| 6.6 | Scope unchanged and still last. Requires Stories 6.9, 6.10, and 6.12; its HP-LIST/HP-OPEN rule depends on whether Story 6.11 successfully retires the approved ceiling. |
| 6.7 | None. Its promotion gate remains binding. |
| 6.8 | None. Its final-record gate remains binding. |
| 6.9 | None. Its oracle-tiering decision remains binding. |
| 6.10 | Complete canonical definition is published; status remains `backlog`. |
| 6.11 | Complete canonical definition and correctness envelope are published; status remains `backlog`. |
| 6.12 | Acceptance and scope remain unchanged. Provenance advances to v8 while its criteria remain quoted from v7; status remains `ready-for-dev`. |

### 2.3 Binding dependency order

The existing spine remains:

`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`

The complete parallel constraints are:

- Preserve `6.9 -> 6.3` and `6.9 -> 6.6`.
- Add `6.8 + 6.9 -> 6.10`.
- Add `6.10 -> completion of 6.3, 6.5, and 6.6`.
- Preserve `6.8 -> 6.12 -> completion of 6.3`.
- Preserve `6.12 -> 6.6`.
- Preserve `6.2 -> 6.11` as a parallel constraint.
- Stories 6.10 and 6.12 are independent. They coordinate overlapping validator edits but neither
  waits for the other.
- Story 6.11 is not a hard prerequisite for Story 6.6. When 6.11 has not succeeded, 6.6 uses and
  discloses the approved-cost ceiling. When 6.11 has succeeded, 6.6 reconfirms the restored
  `post P95 <= 1.05 x baseline P95` rule for HP-LIST and HP-OPEN.
- Story 6.6 remains last.

### 2.4 Artifact conflicts and required adjustments

| Artifact | Classification | Required adjustment |
| --- | --- | --- |
| Current PRD and addendum | N/A | No content change. Preserve all requirements, journeys, scope, and unresolved dispositions. |
| `epics.md` | Action required | Append v8 with the two canonical stories, dispositions, order, and amendment-log continuation. Do not edit v1–v7. |
| `architecture.md` | Action required | Advance frontmatter to v8, add the omitted Story 6.10 proposal and this proposal as correction authorities, and append Story 6.10/6.11 invariants. |
| UX specification and requirement map | N/A | No UI, journey, interaction, accessibility, or UX behavior change. |
| `epic-6-context.md` | Action required | Regenerate from v8 with both story definitions and corrected order. |
| `sprint-status.yaml` | Action required | Add an audit comment only; retain every current story status and key. |
| Story 6.3 implementation spec | Action required | Consume v8 and require Stories 6.10 and 6.12 before completion/review. |
| Story 6.12 implementation guide | Action required, provenance only | Point current authority to v8 while preserving the v7 acceptance text, baseline, scope, status, and independence. |
| Planning-authority conformance test | Action required | Replace the story-range blind spot with a complete registry and strengthen append-chain/dependency validation. |
| Signed v1 and archived product contract | Protected | No mutation. |

### 2.5 Technical impact

Publishing this correction changes planning authority, derived context, sprint audit prose, live
story guidance, and conformance validation. It does not itself implement Stories 6.10 or 6.11.

Story 6.10 later changes non-shipping test infrastructure, repository validation scripts,
workflow bodies, generated workflow renders, runbooks, and repository-local review customization.
It has no production-source or CI-wiring scope.

Story 6.11 later changes internal Server projection keying, derived read models, writer/read-store
behavior, tests, benchmarks, and additive evidence. It requires an ADR before adding durable
derived state. It has no public API, UI, deployment, package-version, or EventStore-authority
scope.

No deployment, IaC, production topology, monitoring contract, or public contract changes are
required by this proposal.

## 3. Recommended Approach

### 3.1 Selected path: direct adjustment

Publish the v8 planning-authority repair and its derived/mechanical updates atomically. This is the
smallest change that makes the two backlog identifiers safe to create, implement, and use as
prerequisites.

**Scope:** Moderate
**Effort:** Medium
**Risk:** Low–moderate
**Schedule impact:** Limited to the affected Epic 6 completion gates; already-defined work with
unambiguous prerequisites may continue.

### 3.2 Why rollback is rejected

No completed implementation or evidence was found invalid. Rolling back Stories 6.1, 6.2, 6.7,
or other completed work would discard valid results and would not create the missing definitions.

### 3.3 Why MVP reduction is rejected

The MVP and preservation contract remain achievable. Removing Story 6.10 would abandon an
approved evidence-boundary correction. Removing Story 6.11 would leave the SM-C2 ceiling as an
unresolved follow-up. Neither choice is justified by the readiness evidence, and neither repairs
the authority chain.

## 4. Detailed Change Proposals

### 4.1 Canonical Story 6.10

**Artifact:** `epics.md`, new v8 amendment
**Story key:** `6-10-consolidate-the-evidence-boundary-validation-pattern`

**OLD:** Story 6.10 is referenced as retaining approved scope/order, but no canonical story
definition exists in the authority chain.

**NEW:**

#### Story 6.10: Consolidate the evidence-boundary validation pattern into one enforced helper

As a release-evidence maintainer,
I want evidence validation consolidated behind one enforced, non-shipping helper,
So that evidence cannot pass through trusted declarations, incomplete diffs, unavailable history,
or vacuous assertions.

**Prerequisites:** Stories 6.8 and 6.9. Story 6.10 has no dependency on Story 6.12;
both must preserve compatible edits where validation surfaces overlap.

**Acceptance Criteria:**

1. A non-packable `Hexalith.Conversations.TestSupport` project supplies
   `RepositoryLocator`, `GitFacts`, `EvidenceManifest`, `BoundaryAssertions`, and
   `AssertionLedger`. It references no Conversations assembly and does not alter portable or
   module-internal oracle membership.
2. Its git runner has bounded execution, concurrent stdout/stderr draining, explicit UTF-8
   decoding, `core.quotepath=false`, and unavailable-git handling. It resolves revisions,
   changed files, raw diff modes, and historical blob hashes.
3. Manifest integrity is recomputed, never trusted: paths stay repository-relative and inside the
   root; files exist; hashes are canonical lowercase SHA-256 and match recomputation;
   generated/build outputs are rejected; signable payload hashes are recomputed. Supersession
   allowlists cannot cover signed evidence and must prove at least one assertion executed.
4. Changed-file validation uses exact set equality. Gitlinks are rejected by parsing mode
   `160000` from raw-diff columns, never by substring matching.
5. Unavailable history produces an explicit skip, never a pass. A purported success with zero
   executed assertions fails. Root-of-trust commits and hashes remain pinned in consuming test
   source.
6. `_bmad/scripts/verify_evidence_boundary.py` enforces adoption with blocker codes
   `EVIDENCE_HELPER_NOT_USED`, `ADHOC_GIT_RUNNER`, `ADHOC_REPOSITORY_ROOT`,
   `ADHOC_HASH_HELPER`, `EVIDENCE_ARTIFACT_UNVALIDATED`, `EXEMPTION_EXPIRED`,
   `SCOPE_NOT_EVALUATED`, and `BASELINE_NOT_PROVIDED`. It retains warning codes
   `EXEMPTION_ACTIVE` and `EVIDENCE_TEST_OUTSIDE_CONFORMANCE`.
7. The evidence gate is mandatory in the five governed workflow bodies in both `.agents` and
   `.claude`, plus the two generated quick-dev render twins. Mirrored workflow bodies remain
   semantically equivalent. The `bmad-dev-story` definition of done and checklist require the
   shared helper and forbid new ad-hoc equivalents.
8. All 24 approved baseline evidence readers, plus any evidence reader added before implementation
   begins, migrate with zero day-one exemptions. Existing assertion strength, pinned constants,
   and test counts are preserved. Projection-proof validation adopts the helper without resolving,
   weakening, or reclassifying Story 6.12's independent evidence-lifecycle work.
9. The runbook documents invariants, authoring steps, exemptions, and known limitations. Fault
   injection proves every guard can fail, including altered hashes, escaping paths, generated
   evidence, gitlinks, subset comparisons, signed-evidence allowlisting, unavailable git, deleted
   workflow invocations, and malformed authority-chain markers.
10. Story 6.7's inherited gate-span coupling is repaired so inserting the evidence gate cannot
    keep the positive test green while weakening its displacement guard.

A repository-local `bmad-code-review` customization adds the approved evidence-boundary review
layer without modifying shipped customization files.

**Completion dependencies:** Story 6.10 precedes completion of Stories 6.3, 6.5, and 6.6.

**Non-goals:** No production source, public contract, package version, AppHost topology, CI
wiring, signed evidence, or completed Story 6.2 artifact changes. Story 6.10 does not decide or
implement Story 6.12.

**Rationale:** This republishes the already-approved Story 6.10 outcome into a non-colliding live
authority version and rebases its workflow scope onto the repository's current dual active trees.

### 4.2 Canonical Story 6.11

**Artifact:** `epics.md`, new v8 amendment
**Story key:** `6-11-make-cross-key-projection-validation-cheap-enough-to-re-gate-sm-c2`

**OLD:** Story 6.11 is a one-line disposition and follow-up paragraph that names a performance
target but does not define a complete correctness-preservation or delivery contract.

**NEW:**

#### Story 6.11: Make cross-key projection validation cheap enough to re-gate SM-C2

As a Conversations maintainer,
I want fail-closed projection consistency checks redesigned to avoid unnecessary reads,
So that HP-LIST and HP-OPEN can again satisfy the original
`post P95 <= 1.05 x baseline P95` rule without weakening correctness.

**Prerequisite:** Story 6.2. Story 6.11 is independent of Stories 6.10 and 6.12.

**Acceptance Criteria:**

1. Before production implementation, an ADR defines the versioned per-conversation index-entry
   key family, derived-state ownership, write ordering, compatibility transition, rebuild/backfill,
   deletion, ledger expiry, and rollback behavior. EventStore remains the only write authority.
2. The detail/open path validates against a tenant-scoped per-conversation index entry instead of
   deserializing the full tenant index. Identical conversation IDs in different tenants always
   resolve to distinct keys.
3. Page validation removes per-row detail and ledger fan-out wherever a completed,
   identity-consistent ledger already proves the indexed generation. Any removed read has a
   correctness proof and adversarial test; performance alone is insufficient.
4. Cross-key correctness remains fail closed. Tests cover missing, duplicated, stale, advanced,
   pending, malformed, misfiled, and mutually inconsistent detail, index-entry, tenant-index,
   dispatch-reference, and ledger states. No path repairs durable state during a read.
5. Duplicate, retried, delayed, and out-of-order projection deliveries remain deterministic and
   idempotent. Equal-position competing dispatches select the same winner across every key, and
   older deliveries cannot overwrite a newer generation.
6. Independent deletion and replay are covered for every derived key family. Rebuild from
   EventStore produces an equivalent readable state; interrupted rebuilds, partial writes, expired
   ledgers, and terminal-dispatch reconciliation remain non-disclosing and fail closed where
   consistency is unproved.
7. Listing and opening preserve the public query contract: tenant isolation, filtering, ordering,
   pagination/cursor behavior, freshness vocabulary, forbidden/nonexistent indistinguishability,
   and response shapes remain unchanged. Multi-page tests prove no row is leaked, skipped, or
   duplicated.
8. Unit, integration, and real Dapr state-store tests inject partial-write, latency,
   unavailable-store, poison-record, retry, and concurrency failures. Existing projection,
   tenant-isolation, replay, governance, contract-shape, and conformance assertions remain at equal
   or greater strength.
9. HP-LIST and HP-OPEN are measured against the preserved Story 6.2 baseline using the identical
   fixture, environment, invocation, sample policy, and statistical calculation. Exactly one
   comparable baseline and candidate result is retained with every raw sample and environment fact.
10. Story 6.11 reaches `done` only when both rows satisfy
    `post P95 <= 1.05 x baseline P95` with correctness gates green. Its additive result then
    retires the approved-cost ceiling for those rows, and Story 6.6 consumes and reconfirms that
    rule. If either row misses the threshold, Story 6.11 remains incomplete; the ceiling remains
    active and disclosed. Validation may not be weakened, bypassed, or reclassified to manufacture
    a pass.

**Conditional sequencing:** Story 6.11 does not block Story 6.6 unless its result is being used to
retire the ceiling. Story 6.6 remains last and states whether it applied the original 5% rule or
the still-active approved-cost ceiling.

**Non-goals:** No public API, contract shape, UI, deployment topology, package version, signed
evidence, completed Story 6.2 artifact, or EventStore authority change.

**Rationale:** Performance work that changes fail-closed derived-state validation must carry the
complete correctness and evidence envelope in its own acceptance contract.

### 4.3 Epic 6 authority v8

**Artifact:** `epics.md`, append after the v7 end marker

**OLD:** The file ends at `epic-6-authority-2026-08-01-v7`, which refers to undefined Stories
6.10 and 6.11.

**NEW:** Append
`EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8`, version
`epic-6-authority-2026-08-01-v8`, superseding v7 and aligned to
`conversations-architecture-2026-08-01-v8`.

The block:

- Publishes the exact Story 6.10 and 6.11 definitions above.
- States that v8 changes authority completeness only and does not expand product scope.
- Carries superseding dispositions for Stories 6.3, 6.5, 6.6, 6.10, 6.11, and 6.12.
- Publishes the complete dependency order from section 2.3.
- Records why the approved Story 6.10 proposal was not lawfully appended under its proposed v6
  identifier.
- Continues the overlay amendment log through v8.
- Leaves all v1–v7 bytes and every preservation denominator unchanged.

### 4.4 Architecture authority v8

**Artifact:** `architecture.md`

**OLD:** `authorityVersion` is `conversations-architecture-2026-08-01-v7`; correction authority
omits the approved Story 6.10 proposal; architecture contains only Story 6.11's terse performance
intent.

**NEW:**

- Set `authorityVersion` to `conversations-architecture-2026-08-01-v8` and add v7 to
  `supersededAuthorityVersions`.
- Add the approved Story 6.10 proposal and this proposal to `correctionAuthority`.
- Append `2026-08-01 Stories 6.10 And 6.11 Authority-Completion Amendment`.
- Pin Story 6.10's recomputation, exact-boundary, raw-gitlink, skip-not-pass, non-vacuity,
  source-pinned-root, non-shipping helper, dual-workflow-tree, and no-CI invariants.
- Pin Story 6.11's derived-state-only ownership, ADR-first durable schema, tenant isolation,
  fail-closed/no-repair behavior, retry/deletion/replay envelope, public-query preservation,
  comparable measurement, and conditional ceiling-retirement rules.
- Authorize Story 6.10's test/workflow scope and Story 6.11's bounded internal projection scope
  only; authorize no public, UI, deployment, signed-evidence, or EventStore-authority change.
- Record that Stories 6.10 and 6.12 are independent and must preserve concurrent validator edits.

### 4.5 Derived delivery artifacts

#### Epic 6 context

**OLD:** v7 frontmatter and body contain no canonical Story 6.10/6.11 summaries.

**NEW:** Regenerate `epic-6-context.md` with v8 overlay/architecture identities, v8 source marker,
the new stories, corrected dependencies, Story 6.11's ADR trigger, and the conditional SM-C2 rule.

#### Sprint status

**OLD:** Both identifiers already exist at `backlog`, with comments describing their incomplete
authority state.

**NEW:** Add a chronological v8 publication comment. Keep 6.10 and 6.11 at `backlog`, 6.12 at
`ready-for-dev`, 6.3 and 6.8 at `in-progress`, and every other key/status unchanged. Keep the
evidence-boundary action item open until Story 6.10 is actually done.

#### Story 6.3

**OLD:** The spec consumes cumulative authority through v7, and its generator still carries an
older hard-coded authority identity.

**NEW:** Consume v8; require Stories 6.10 and 6.12 before completion or return to review; use the
Story 6.10 helper when available; generate current authority identities as v8 during Story 6.3
implementation. Preserve its `in-progress` status and all denominator/projection-chain obligations.

#### Story 6.12

**OLD:** Current-authority provenance and dev notes name v7.

**NEW:** Point current overlay/architecture provenance to v8 and retain both the original v7
proposal and this v8 proposal as context. State that v8 preserves the v7 acceptance criteria
verbatim. Preserve `ready-for-dev`, baseline, all criteria, and the explicit absence of dependencies
on Stories 6.9 and 6.10.

### 4.6 Mechanical planning-authority completeness

**Artifact:** `ArchitecturePlanningAuthorityValidationTest.cs`

**OLD:** Version and append assertions accrete hand-written predecessor variables; story/context
equivalence loops over 6.1–6.9 and special-cases 6.12, allowing 6.10 and 6.11 to be absent while the
test remains green.

**NEW:**

- Use explicit v2–v8 architecture and overlay identities.
- Validate a declared ordered table of `(marker family, version, supersedes)`.
- Require each begin/end marker exactly once, strict block adjacency, correct supersession, unique
  version reservation, and the v8 end marker at end of file.
- Replace the partial loop with one registry containing exactly Stories 6.1–6.12.
- Require equality among defined epics stories, generated-context summaries, and sprint-status
  story keys.
- Require every dependency source and target to exist, and assert the v8 ordering, independence,
  conditional Story 6.11 rule, and Story 6.6-last invariant.
- Pin the load-bearing Story 6.10 and Story 6.11 semantic invariants.
- Fault-inject missing/duplicate stories, tracked/defined mismatches, missing dependency targets,
  version collisions, misplaced/duplicate/missing markers, a false 6.10-to-6.12 dependency, and a
  false mandatory 6.11-to-6.6 dependency.
- Preserve historical-prefix, signed-evidence, v1–v7, denominator, and projection-proof lifecycle
  assertions.

**Rationale:** A planning authority cannot claim completeness when the mechanical validator
enumerates only a hand-selected subset of the identifiers it governs.

## 5. Implementation Handoff

### 5.1 Scope classification

**Moderate.** The correction reorganizes backlog authority and its enforcement, then routes two
already-tracked stories for later implementation. It does not require a fundamental product replan.

### 5.2 Responsibilities

**Planning owner / architect**

- Append Epic 6 v8 and the architecture v8 amendment without modifying prior blocks.
- Add both proposal paths to architecture correction authority.
- Regenerate Epic 6 context from the published v8 semantics.

**Product owner**

- Preserve current story statuses while recording v8 publication in sprint status.
- Keep Stories 6.10 and 6.11 at backlog until dedicated implementation story files are prepared.
- Preserve the open evidence-boundary action item until Story 6.10 is complete.

**Developer**

- Apply the planning-authority conformance changes atomically with publication.
- Synchronize Story 6.3 and Story 6.12 guidance as specified.
- Later implement Story 6.10's test/workflow scope and Story 6.11's ADR-gated internal projection
  scope as separate candidates.
- Preserve concurrent Story 6.12 changes on shared validation surfaces.

**Test / release owner**

- Confirm v1–v7 authority, the archived product contract, completed Story 6.2 evidence, and signed
  v1 evidence remain byte-identical.
- Run the focused planning-authority conformance test and relevant repository checks.
- Rerun implementation readiness after v8 publication and record the new assessment separately.

### 5.3 Publication sequence

1. Approve this complete proposal.
2. Append `epics.md` v8 and architecture v8 as one authority candidate.
3. Regenerate Epic 6 context; synchronize sprint audit prose and Stories 6.3/6.12 guidance.
4. Update and fault-inject the planning-authority conformance validator.
5. Verify earlier authority blocks and protected evidence are unchanged.
6. Run focused and relevant conformance checks.
7. Rerun implementation readiness. Keep the current `NOT READY` report as immutable pre-correction
   evidence.
8. Prepare dedicated Story 6.10 and Story 6.11 implementation files when each moves out of backlog.

### 5.4 Later implementation sequence

- `6.8 + 6.9 -> 6.10`
- `6.8 -> 6.12`
- `6.2 -> 6.11` in parallel
- `6.10 + 6.12 -> 6.3 completion`
- `6.10 -> 6.5 completion`
- `6.9 + 6.10 + 6.12 -> 6.6`
- Story 6.6 remains last; Story 6.11's result determines which HP-LIST/HP-OPEN gate it applies.

### 5.5 Success criteria

The correction is successfully published when:

- `epics.md` ends in one valid v8 amendment and every earlier byte remains unchanged.
- Architecture, epics, generated context, sprint tracking, and live story guidance agree on v8.
- Stories 6.1–6.12 each have exactly one canonical definition, one context summary, and one sprint
  key.
- Every dependency edge resolves to a defined story and matches the approved order.
- Story 6.10 and Story 6.12 remain independent.
- Story 6.11 remains conditional for Story 6.6 and cannot retire the ceiling without both measured
  rows passing the original rule.
- The focused planning-authority conformance test passes and every new fault injection proves red.
- PRD/UX scope, archived contract hash, completed Story 6.2 evidence, signed v1 evidence, public
  contracts, and deployment topology remain unchanged.
- A new readiness assessment no longer reports Stories 6.10 and 6.11 as dangling authority and
  finds no replacement critical blocker to corrective implementation.

## 6. Risks and Controls

| Risk | Control |
| --- | --- |
| Earlier authority is rewritten while adding v8 | Append-only byte-prefix and marker-chain assertions; v8 must end the file. |
| Story 6.10 and 6.12 overwrite shared validator work | No dependency is invented; both stories must preserve concurrent changes and rebase against the live candidate. |
| Story 6.11 buys speed by weakening correctness | ADR-first design, explicit correctness matrix, public-query preservation, adversarial tests, and a non-negotiable measured done-when. |
| The per-conversation index becomes hidden authority | Architecture identifies it as derived state; EventStore remains the only write authority; rebuild and deletion behavior are mandatory. |
| A story becomes dangling again | Defined/context/tracked set equality, dependency-target validation, and duplicate-version/story fault injection. |
| Readiness history is overwritten | The existing readiness report remains immutable; rerun produces a separate assessment. |

## 7. Checklist Record

### Understand the trigger and context

- [N/A] 1.1 — No triggering implementation story; the readiness assessment triggered the change.
- [x] 1.2 — Planning-authority publication failure defined precisely.
- [x] 1.3 — Missing definitions, version collision, sprint references, validator blind spot, and
  protected-evidence facts recorded.

### Epic impact assessment

- [x] 2.1 — Epic 6 remains completable with an append-only repair.
- [x] 2.2 — Existing Epic 6 scope is amended; no new epic is required.
- [x] 2.3 — Remaining stories and completion dependencies assessed.
- [x] 2.4 — No epic is invalidated or made obsolete.
- [x] 2.5 — Corrected dependency order approved.

### Artifact conflict and impact analysis

- [N/A] 3.1 — PRD/MVP change not required.
- [!] 3.2 — Architecture v8 publication required.
- [N/A] 3.3 — No UX change.
- [!] 3.4 — Epics, derived context, sprint audit prose, story guidance, and conformance validation
  require synchronized changes.

### Path-forward evaluation

- [x] 4.1 — Direct adjustment is viable; medium effort, low–moderate risk.
- [x] 4.2 — Rollback is not viable.
- [x] 4.3 — MVP reduction is not required.
- [x] 4.4 — Direct adjustment selected and approved.

### Proposal components

- [x] 5.1 — Issue summary complete.
- [x] 5.2 — Epic, story, artifact, and technical impacts documented.
- [x] 5.3 — Recommendation and rejected alternatives documented.
- [x] 5.4 — MVP impact, action plan, dependencies, and sequencing documented.
- [x] 5.5 — Moderate-scope Product Owner / Developer / Architect / Test handoff defined.

### Final review and handoff

- [x] 6.1 — Applicable checklist sections addressed; action-needed items are carried into the
  handoff.
- [x] 6.2 — Complete-proposal consistency review passed.
- [x] 6.3 — Jerome explicitly approved the complete proposal on 2026-08-01.
- [N/A] 6.4 — Sprint entries already exist; status changes are prohibited until implementation.
- [x] 6.5 — Moderate-scope handoff, sequence, responsibilities, and success criteria confirmed.

## 8. Approval

The six component edit proposals were individually approved by Jerome on 2026-08-01 during the
incremental correct-course workflow. Complete-proposal approval is intentionally separate.

**Release-owner decision:** Approved for implementation
**Approved by:** Jerome
**Approval date:** 2026-08-01
**Conditions:** Apply the v8 publication atomically, preserve every protected historical artifact,
and rerun implementation readiness before using Stories 6.10 or 6.11 as implementation authority.
