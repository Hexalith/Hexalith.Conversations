---
title: "Sprint Change Proposal — Rebase Evidence-Boundary Guidance onto Current Dev/Review Workflows"
project: "Conversations"
date: "2026-08-03"
status: "approved"
changeScope: "moderate"
mode: "batch"
trigger: "Epic 5 retrospective action A5: promote the Story 5.3 evidence-boundary validation pattern into reusable dev/review guidance"
affectedAuthority: "epic-6-authority-2026-08-02-v9 and conversations-architecture-2026-08-02-v9 (planning candidate UNBOUND; implementation hold ACTIVE)"
recommendedPath: "Direct Adjustment — amend existing Epic 10 Stories 10.3 and 10.4; create no duplicate epic or story"
approval: "approved by Jerome on 2026-08-03"
---

# Sprint Change Proposal — Rebase Evidence-Boundary Guidance onto Current Dev/Review Workflows

## 1. Issue Summary

### Trigger and problem statement

Epic 5 retrospective action A5 remains open:

> Promote the Story 5.3 evidence-boundary validation pattern into reusable
> dev/review guidance.

The action is already represented in planning authority. The approved
2026-07-28 evidence-boundary proposal created Story 6.10, and the approved v9
execution correction superseded Story 6.10 with Epic 10, Stories 10.1-10.4.
Creating another story would duplicate accepted scope.

The material change is workflow drift after that v9 plan was authored. Commit
`93a86f2` updated the repository to BMAD `6.10.1n46` and made `bmad-build`,
`bmad-build-auto`, and `bmad-review` the current development/review surfaces.
Story 10.3 still freezes the preceding workflow generation: six of its twelve
named paths no longer exist, while the current primary surfaces are absent.
As written, Story 10.3 cannot satisfy its exact-inventory acceptance contract
and could not prove that the requested guidance reaches the workflows developers
and reviewers now invoke.

### Evidence from Story 5.3

Story 5.3's review converted a generated attestation into a guarded evidence
artifact by adding the following reusable invariants:

1. Recompute declared source hashes instead of trusting declarations.
2. Require every evidence path to be repository-relative, contained, and real.
3. Recompute the signable payload from canonical manifest rows.
4. Compare the final changed-file boundary using exact set equality.
5. Detect submodule gitlinks from raw mode `160000`, not text matching.
6. Prove inventory row identities exactly match the accepted source inventory.
7. Treat unavailable history as a visible skip or blocker, never a pass.
8. Require non-vacuous execution so zero evaluated assertions cannot pass.

The Epic 5 retrospective records that these defects were discovered during
review and explicitly calls for a reusable check before review. The later
approved evidence-boundary proposal also measured duplicated Git runners,
ad-hoc root/hash helpers, tautological artifact bindings, and zero-assertion
green paths. Epic 10 correctly owns the technical consolidation; only its
workflow and guidance projection is now stale.

### Change category

This is a partial implementation and authority-drift correction for an already
approved process action. It is not a new product requirement, runtime design
change, UX activation, or release decision.

### Scope boundary

This proposal authorizes no implementation. It does not change product runtime
code, public contracts, persistence, packages, AppHost topology, deployment,
accepted evidence, completed story records, submodule content, or the active
implementation hold.

## 2. Impact Analysis

### Epic and story impact

| Unit | Impact | Required disposition |
| --- | --- | --- |
| Epics 1-6 | Completed history and corrective foundation remain immutable | No change |
| Epics 7 and 9 | Existing hard predecessors for Epic 10 remain valid | No change |
| Epic 10 | Already owns the unified evidence boundary | Retain outcome, entry, exit, and four-story decomposition |
| Story 10.1 | Neutral TestSupport helpers and safe Git facts | No change |
| Story 10.2 | Shared evidence-boundary invariants | No change |
| Story 10.3 | Frozen workflow inventory targets the superseded workflow generation | Replace it with a current, route-complete v2 inventory and stable guidance blocker codes |
| Story 10.4 | Runbook exists in scope, but current team-owned dev/review customization is not an atomic acceptance outcome | Add project-owned reusable guidance bindings and fault-injection proof without changing the 27-reader migration |
| Epics 11 and 14 | Depend on Epic 10 completion | No dependency change; they consume the corrected Story 10.4 record |
| Epic 15 / RG-15 | Release closure consumes Epic 10 evidence | No semantic change; implementation hold and release decision remain separate |

No new epic or story is required. Story 10.3 continues to own mechanical
workflow enforcement; Story 10.4 continues to own guidance, reader migration,
runbook publication, and complete fault injection.

### Artifact impact

| Artifact | Impact | Proposed treatment |
| --- | --- | --- |
| PRD and addendum | No requirement or MVP conflict | No change |
| `epics.md` v9 block | Story 10.3 inventory is stale; Story 10.4 does not bind current reusable guidance surfaces | Publish a narrow append-only successor amendment; do not rewrite v8/v9 history |
| `architecture.md` v9 overlay | Invariants remain correct, but the workflow projection predates the installed workflow generation | Append the matching narrow authority correction and preserve every v9 technical invariant |
| UX specification and UX requirement map | UX remains `preserved-not-activated` | No change |
| V9 authority bundle, graph, supersession map, story contracts, generated execution view, validators, and sprint projection | Any authority amendment changes the unbound planning candidate | Regenerate and validate atomically as part of v9 publication; never patch one projection alone |
| `sprint-status.yaml` | Epic 5 A5 is still correctly `open`; the file still projects v8 unfinished work | Keep A5 open. Apply the already-approved v9 successor projection atomically; do not close A5 until corrected Story 10.4 passes |
| `docs/runbooks/evidence-boundary-validation.md` | Already owned by Story 10.4 | Make it the canonical human guidance source for both dev and review customizations |
| `_bmad/custom/bmad-build.toml` | Missing project-owned evidence guidance | Add team-owned regular and one-shot evidence-boundary review/gate guidance |
| `_bmad/custom/bmad-build-auto.toml` | Missing project-owned evidence guidance | Add the equivalent unattended build guidance and fail-closed result handling |
| `_bmad/custom/bmad-review.toml` | Missing standing evidence guidance/lens | Add canonical `review_guidance` and an evidence-boundary lens for applicable changes |
| Installed `*/customize.toml` defaults | Marked `DO NOT EDIT` and overwritten by updates | Do not modify; use `_bmad/custom/` overrides |

### Technical and operational impact

The change affects planning authority, non-shipping verification tooling,
workflow instructions, project-owned BMAD customization, tests, and a runbook.
It introduces no runtime dependency and does not change either conformance
tier's production boundary.

The principal operational risk is another BMAD workflow update invalidating a
hand-maintained inventory. The corrected contract therefore has to validate
both route coverage and resolved customization, not merely phrase existence in
a frozen list.

## 3. Recommended Approach

### Option evaluation

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable: Epic 10 already owns the full outcome | Moderate | Low-to-medium after mechanical validation | **Selected** |
| Potential Rollback | Not viable: rollback would remove the current BMAD workflow generation and would not close A5 | High | High; loses current workflow capability | Reject |
| PRD/MVP Review | Not warranted: no product requirement or UX scope changes | Medium | High; solves the wrong boundary | Reject |

### Selected path

Publish a narrow append-only authority correction that rebases Epic 10's
workflow and guidance inventories onto the current BMAD generation while
preserving the entire v9 outcome graph and technical boundary.

The correction must:

1. Create no new epic or story.
2. Preserve Story 10.1, Story 10.2, the 27-reader inventory, and all Epic 10
   predecessors and successors.
3. Replace Story 10.3's obsolete workflow inventory with the current routed
   entry points in both installed agent trees.
4. Keep mechanical enforcement distinct from reusable human/reviewer guidance.
5. Put durable project guidance in `_bmad/custom/` and the Story 10.4 runbook,
   not in overwrite-prone default customization files.
6. Verify routed aliases reach a governed current entry point instead of
   duplicating gates in forwarding shims.
7. Fail closed on missing/displaced gates, missing/drifted guidance, unresolved
   history, empty scope, parity drift, and customization-resolution drift.
8. Keep the v9 planning candidate `UNBOUND` and the implementation hold `ACTIVE`
   until the entire amended authority bundle is published, validated, assessed
   independently as `READY`, and explicitly lifted by the release owner.

### Scope and schedule classification

- **Change scope:** Moderate — planning-authority and workflow correction inside
  an already approved epic.
- **Product/MVP scope:** Unchanged.
- **Implementation sequence:** Execute only after v9 atomic publication and hold
  lift; Stories 10.3 and 10.4 remain behind Stories 7.4, 9.2, 10.1, and 10.2.
- **Schedule impact:** No new story count. Story 10.3/10.4 estimates must include
  current workflow/customization migration and its fault fixtures.
- **Risk if adopted:** Low-to-medium; concentrated in route coverage, cross-tree
  parity, and workflow-update durability.
- **Risk if rejected:** High; Story 10.3 is unsatisfiable against missing paths,
  and A5 remains absent from current primary dev/review guidance.

## 4. Detailed Change Proposals

### 4.1 Canonical epic authority — Story 10.3 workflow inventory

**Artifact:**
`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

**OLD:** `V9-EVIDENCE-WORKFLOWS-v1` freezes twelve paths from the prior workflow
generation. Six are now absent:

- both `bmad-dev-auto/step-04-review.md` paths;
- both `bmad-quick-dev/step-05-present.md` paths; and
- both `bmad-quick-dev/step-oneshot.md` paths.

The inventory does not name the current `bmad-build`, `bmad-build-auto`, or
`bmad-review` surfaces.

**NEW:** publish `V9-EVIDENCE-WORKFLOWS-v2` as two separately frozen sets.

#### A. Mechanical transition/gate inventory

The route-complete minimum is the following six logical bodies in both active
agent trees, twelve tracked paths total:

```text
.agents/skills/bmad-build/step-04-review.md
.agents/skills/bmad-build/step-05-present.md
.agents/skills/bmad-build/step-oneshot.md
.agents/skills/bmad-build-auto/step-04-review.md
.agents/skills/bmad-dev-story/SKILL.md
.agents/skills/bmad-code-review/steps/step-04-present.md
.claude/skills/bmad-build/step-04-review.md
.claude/skills/bmad-build/step-05-present.md
.claude/skills/bmad-build/step-oneshot.md
.claude/skills/bmad-build-auto/step-04-review.md
.claude/skills/bmad-dev-story/SKILL.md
.claude/skills/bmad-code-review/steps/step-04-present.md
```

The first four logical bodies are current primary development routes. The last
two remain directly callable legacy routes and therefore retain explicit gate
coverage. `bmad-dev-auto` and `bmad-quick-dev` are forwarding shims; validation
must prove that each resolves once to `bmad-build-auto` or `bmad-build` and does
not fork a second gate implementation.

Each current body must invoke the verifier once at the relevant boundary:

- before changing a spec/story to `in-review`, `review`, or `done`;
- before finalizing an unattended run;
- before presenting a code-review result as complete; and
- before committing a one-shot result.

Cross-tree parity remains mandatory. Generated, ephemeral render output is
validated through deterministic rendering/parity tests rather than frozen as a
tracked authority path.

#### B. Reusable guidance inventory

Freeze these project-owned sources separately:

```text
_bmad/custom/bmad-build.toml
_bmad/custom/bmad-build-auto.toml
_bmad/custom/bmad-review.toml
docs/runbooks/evidence-boundary-validation.md
```

The verifier must validate the resolved customization, not only raw TOML text,
so a merge-key mistake or later default change cannot silently remove the
guidance.

**Rationale:** gate placement and reusable guidance have different durability
and rollback semantics. Treating them as one path list would either omit current
guidance or incorrectly make overwrite-prone defaults the team's source of
truth.

### 4.2 Story 10.3 bounded outcome, codes, and acceptance changes

**OLD bounded outcome:** one verifier governs twelve frozen workflow files with
stable blocker/warning semantics and parity-checked transitions.

**NEW bounded outcome:** one verifier governs every current or directly callable
transition route, verifies deprecated aliases forward once to a governed route,
and validates the resolved project-owned guidance inventory with stable
blocker/warning semantics.

Preserve `AC-10.3-01` through `AC-10.3-08` identities. Amend their inputs and
expected results as follows:

- `AC-10.3-03`: validate the v2 mechanical inventory and exact insertion spans.
- `AC-10.3-04`: validate six logical gate bodies across `.agents` and `.claude`,
  plus deterministic render parity; remove the obsolete five-body/two-quick-dev
  assumption.
- `AC-10.3-05`: keep `not-applicable` distinct from `PASS` and require a
  nonempty evaluated ledger for applicable changes.
- `AC-10.3-06`: prove `FAIL` and `BLOCKED` prevent every current transition and
  unattended finalization; valid `not-applicable` continues but is recorded.
- `AC-10.3-07`: remove and displace current gates, break one alias route, and
  remove one resolved guidance binding; each fault must produce its exact code
  and restore byte-identically.
- `AC-10.3-08`: bind both v2 inventory digests, resolved customization digests,
  current BMAD version/route facts, and all updated results.

Add stable blocker codes without renaming the existing registry:

```text
EVIDENCE_ALIAS_ROUTE_INVALID
EVIDENCE_GUIDANCE_NOT_USED
EVIDENCE_GUIDANCE_DRIFT
EVIDENCE_CUSTOMIZATION_RESOLUTION_FAILED
```

Keep existing codes including `EVIDENCE_GATE_NOT_USED`,
`EVIDENCE_GATE_DISPLACED`, `EVIDENCE_WORKFLOW_PARITY_DRIFT`,
`SCOPE_NOT_EVALUATED`, and `BASELINE_NOT_PROVIDED`.

**Rollback boundary:** remove the verifier and current gate insertions as one
unit, remove the three project-owned customization files, and restore the
pre-10.3 workflow bytes. Retain Stories 10.1-10.2 and do not restore absent
pre-6.10 workflow files.

### 4.3 Story 10.4 reusable guidance acceptance

**Artifact:** same canonical `epics.md` v9 successor block.

Keep the exact 27-reader inventory and `AC-10.4-01` through `AC-10.4-08`.
Append one atomic acceptance scenario rather than renumbering existing IDs:

#### `AC-10.4-09` — Current dev/review guidance is reusable and resolved

**Given:**

- the canonical runbook;
- the three project-owned customization files;
- the v2 guidance inventory and digest;
- the current BMAD customization defaults; and
- Story 10.3's current verifier and result schema.

**When:** resolve `bmad-build`, `bmad-build-auto`, and `bmad-review`
customization and run the Story 10.4 guidance validator from the repository
root.

**Then:**

1. Regular build, one-shot build, unattended build, and general code review all
   receive the same canonical evidence-boundary guidance.
2. The guidance checks recomputed hashes, containment, canonical signable
   payload, exact changed-path equality, raw-mode gitlink exclusion, exact
   inventory identity, root-of-trust pinning, and anti-vacuity.
3. Applicable evidence changes invoke the mechanical verifier and preserve its
   `PASS`/`FAIL`/`BLOCKED`/`not-applicable` distinction.
4. Missing or drifted guidance fails with
   `EVIDENCE_GUIDANCE_NOT_USED` or `EVIDENCE_GUIDANCE_DRIFT`.
5. No shipped `DO NOT EDIT` default customization file is modified.
6. Removing a team override, weakening set equality to containment, trusting a
   declared hash, or redirecting the runbook path turns the validator red and
   restores the fixture byte-identically.

Update Story 10.4's generated final-record summary from `8/8/0/0/0/0` to
`9/9/0/0/0/0` and bind the three resolved customization results plus the
runbook digest. The Epic 5 action is not closed merely because the files exist;
it closes only when this generated final record is current, compatible, and
`PASS`.

### 4.4 Canonical runbook content

**Artifact:** `docs/runbooks/evidence-boundary-validation.md`

Retain the already planned sections `Invariants`, `Authoring`, `Exemptions`,
`Fault injection`, and `Known limitations`. Add explicit `Development workflow`
and `Review workflow` subsections.

The development subsection must state:

- use TestSupport helpers rather than local Git/root/hash implementations;
- run the verifier before an applicable review/done transition;
- treat `BLOCKED` as blocked, never as pass or not-applicable;
- keep roots of trust in consuming test source;
- freeze inventories at story entry and compare exact sets; and
- record stable blocker codes without weakening assertions to finish a story.

The review subsection must require reviewers to ask:

- Are declared hashes and the signable payload independently recomputed?
- Can every path be proven inside the repository?
- Is the changed-file boundary exact, including missing and unexpected paths?
- Are gitlinks derived from raw modes?
- Does the asserted inventory equal its frozen source inventory?
- Does unavailable history skip or block visibly?
- Did at least one applicable assertion execute?
- Would each guard turn red under its named fault fixture?

The runbook is guidance, not a substitute for the verifier or acceptance tests.

### 4.5 Architecture authority

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

Append a narrow successor correction; do not edit the v8 prefix or v9 block.
It must state:

- the BMAD `6.10.1n46` workflow generation superseded the workflow-path
  projection used by `V9-EVIDENCE-WORKFLOWS-v1`;
- Epic 10's outcome, entry, exit, non-shipping boundary, and all inherited v8/v9
  technical invariants remain unchanged;
- current route coverage and resolved project customization are authority-bound
  inputs to Story 10.3/10.4;
- deprecated forwarding aliases are validated as routes, not forked as a second
  source of implementation guidance; and
- any workflow generation change invalidates the workflow/guidance inventory
  and Story 10.3/10.4 evidence until revalidated.

Use the next append-only architecture/epic authority identities selected during
atomic publication. Do not mutate the existing `final` v9 markers or assign a
new current execution view independently of the complete bundle.

### 4.6 V9 companion publication and sprint projection

Regenerate in one candidate-bound operation:

1. v9 successor story contracts for 10.3 and 10.4;
2. workflow and guidance inventories with ordinal paths and SHA-256 digests;
3. authority bundle, execution graph, supersession map, generated current view,
   and planning-validator expectations;
4. UX map parity with unchanged `preserved-not-activated` denominators; and
5. sprint projection containing Epic 10 and Stories 10.1-10.4.

The supersession map continues to map Story 6.10 exactly once to Epic 10. No
new obligation is created: `AC-10.4-09` makes the already-mapped
`V8-6.10-AC9` guidance obligation atomic against the current workflow
generation.

Keep the Epic 5 action row `open` through Stories 10.1-10.3. Close it only after
Story 10.4's compatible `9/9/0/0/0/0` final record passes. Status alone, a
runbook alone, or customization files alone do not satisfy done-when.

## 5. Validation and Acceptance Plan

### Planning validation

Before any hold-lift assessment:

1. Validate the preserved v8 and v9 prefix hashes and complete append-only
   authority chain.
2. Validate one-to-one Story 6.10 to Epic 10 supersession and zero-gap mapping
   of all ten v8 acceptance obligations.
3. Validate the current workflow and guidance path sets, digests, route graph,
   cross-tree parity, and customization resolution.
4. Validate all cross-artifact identities and digests against one planning
   candidate.
5. Rerun the independent IR-0 assessment without instructing its verdict.

### Story-level validation

- Story 10.3 proves all current gates, aliases, result states, and guidance
  bindings through isolated fault fixtures.
- Story 10.4 proves the 27 reader migrations at equal strength, canonical
  runbook, reusable current dev/review guidance, complete mutation matrix, and
  inherited promotion-gate span.
- Every test lane has zero failed, skipped, and not-run checks. Environmental
  inability is `BLOCKED`, never `PASS`.

### Success criteria

The correction is successful when:

1. No frozen workflow path is missing at Story 10.3 entry.
2. Every current or directly callable build/review route is governed exactly
   once or proven to forward exactly once to a governed route.
3. Resolved `bmad-build`, `bmad-build-auto`, and `bmad-review` customization
   contains the canonical evidence guidance.
4. Applicable evidence changes cannot reach review/done with a failing,
   blocked, unevaluated, or missing boundary result.
5. All named guidance and gate fault injections turn red and restore.
6. No product/runtime/public-contract/UX/signed-evidence bytes change.
7. Epic 5 action A5 closes only from the passing Story 10.4 final record.

## 6. Handoff Plan

### Ownership

| Role | Responsibility |
| --- | --- |
| Product Manager | Approve the no-new-scope disposition and append-only Epic 10 contract correction |
| Solution Architect | Own the matching architecture authority amendment and accept the complete bundle |
| Workflow owner | Freeze current route/guidance inventories; implement gate and customization integration |
| Quality owner | Own stable-code, parity, route, customization-resolution, and fault-injection validation |
| Release owner | Keep the implementation hold active; record any later hold-lift only after validator PASS and independent IR-0 READY |
| Dev/Review maintainers | Use the canonical runbook and project-owned customization; do not edit overwritten defaults |

### Implementation sequence

1. Approve this course correction.
2. Reconcile it into the unbound v9 adoption specification and publish the
   narrow append-only authority successor plus all companion projections.
3. Run mechanical planning validation.
4. Run independent IR-0 on the exact same candidate and bundle.
5. Obtain explicit release-owner hold-lift, if the evidence permits it.
6. Execute Epic 10 in dependency order; implement these changes in Stories 10.3
   and 10.4, not as an out-of-band workflow patch.
7. Close Epic 5 action A5 from the Story 10.4 final record only.

### Approval gate

This document is a proposal. Approval authorizes planning-authority publication
and later story implementation through the existing v9 gates. It does not by
itself lift the implementation hold, authorize a release, permit evidence
rewrites, or approve commits/pushes outside the applicable implementation
workflow.

## 7. Correct-Course Checklist Record

| Checklist area | Result |
| --- | --- |
| Trigger and context | `[x]` Story 5.3 and Epic 5 A5 identified; issue is partial implementation plus workflow-authority drift |
| Epic impact | `[x]` Epic 10 remains viable; no new epic/story; dependencies and downstream outcomes preserved |
| Artifact conflict | `[x]` PRD/UX unchanged; epics/architecture/companions require a narrow append-only correction |
| Direct adjustment | `[x]` Viable and selected |
| Rollback | `[N/A]` Would remove current workflow generation and not close the action |
| MVP review | `[N/A]` Product scope and requirements remain aligned |
| Proposal components | `[x]` Issue, impact, approach, detailed edits, validation, and handoff included |
| Final approval | `[x]` Approved by Jerome on 2026-08-03 |

## 8. Approval Record

- **Decision:** Approved
- **Approver:** Jerome
- **Date:** 2026-08-03
- **Approval response:** `approve`
- **Change classification:** Moderate
- **Authorized next step:** Product Owner and Scrum Master replan the affected
  Epic 10 story contracts and coordinate the append-only authority publication
  with the Solution Architect, workflow owner, and Quality owner.
- **Authority boundary:** Approval does not lift the v9 implementation hold,
  authorize release closure, close Epic 5 action A5, or implement Stories
  10.3-10.4. Those outcomes remain governed by the validation, IR-0,
  release-owner, dependency, and final-record gates in this proposal.
