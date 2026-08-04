---
workflow: bmad-correct-course
status: approved
date: 2026-08-04
project: Conversations
mode: Batch
changeScope: Moderate
recommendedApproach: Direct Adjustment through an append-only Story 7.1 schema-checkpoint amendment
implementationHold: ACTIVE
approval: approved
approvedBy: Jerome
approvedOn: 2026-08-04
triggerArtifact: _bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core.md
affectedAuthority: epic-6-authority-2026-08-03-v10 and conversations-architecture-2026-08-03-v10
---

# Sprint Change Proposal — Story 7.1 Schema-Only Bootstrap Authority

## 1. Issue Summary

### Trigger

The prepared Story 7.1 implementation specification is correctly blocked. It
defines a schema-only preparatory slice, while the effective v10 authority
permits no such independently executable slice:

- `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json` binds one
  outcome containing the four schemas, the deterministic generator core, six
  acceptance scenarios, and the JSON/Markdown final-record bundle;
- the execution graph makes Story 7.1 depend on `IR-0` and the global
  implementation hold remains `ACTIVE`;
- the three new Story 7.1 schema files do not yet exist;
- no approved canonical amendment or machine-readable sidecar separates
  schema work from generator and final-record work; and
- `python3 _bmad/scripts/publish_v9_planning_authority.py --repository .
  --check` exits `1` with
  `OUTPUT_DRIFT: _bmad-output/implementation-artifacts/sprint-status.yaml`.

The current output drift is substantive, not a harmless timestamp mismatch.
Against the bound planning candidate, the publisher would:

1. normalize `last_updated` from `08-03-2026 20:37` to `2026-08-03`;
2. change `epic-6-retrospective` from `done` back to `optional`; and
3. remove all six open Epic 6 retrospective action items added after the
   currently bound planning candidate.

The retrospective state and action items are committed project evidence. They
must not be deleted merely to make a stale publisher check green.

### Problem statement

Story 7.1 has a legitimate implementation-order boundary that its current
whole-story contract does not express. The three missing schemas and their
closed-contract tests can be implemented and reviewed without implementing the
generator or producing a final story record. Treating that work as Story 7.1
completion would be false; requiring generator/final-record output before the
schemas exist makes the preparatory slice impossible to authorize precisely.

At the same time, the active hold is a separate global safety gate. A schema
checkpoint must not become a prose exception to `IR-0` or to the release-owner
hold decision.

### Change category

This is a planning-authority and execution-decomposition correction inside an
already approved story. It changes no product requirement, runtime behavior,
public contract, UX activation, release criterion, package, deployment
topology, submodule, or accepted historical record.

### Non-goals

- Do not implement any Story 7.1 schema, generator, test, result, or final
  record in this correction workflow.
- Do not lift, bypass, scope around, or reinterpret the global implementation
  hold.
- Do not renumber Stories 7.1-7.4 or create a duplicate story.
- Do not mark Story 7.1 `done` from schema-only evidence.
- Do not modify `_bmad/scripts/generate_story_record.py` or produce
  `story-7.1-final-record-v2.*` from the schema checkpoint.
- Do not rewrite the immutable v1-v10 authority prefixes, completed story
  records, accepted evidence, or the existing
  `_bmad/schemas/v9-story-contract-v1.schema.json` bytes.
- Do not revert the Epic 6 retrospective status or discard its six open action
  items to satisfy the publisher.
- Do not modify or discard the user's current Story 7.1 specification while
  this proposal is pending.

## 2. Impact Analysis

### Epic and story impact

| Unit | Impact | Required disposition |
| --- | --- | --- |
| Epics 1-6 | Completed history and the Epic 6 retrospective remain evidence | Preserve; no story reopening or evidence rewrite |
| Epic 7 | Outcome and four-story decomposition remain valid | Add one non-story execution checkpoint inside Story 7.1 |
| Story 7.1 | Whole-story contract is too coarse for ordered implementation | Add an effective schema-only checkpoint; retain all six final acceptance scenarios and the one final record |
| Stories 7.2-7.4 | Depend on complete Story 7.1, not its partial schema checkpoint | No scope or predecessor change |
| Epics 8-15 | Remain downstream of completed Epic 7 or their existing branches | No requirement or scope change; schedule remains gated |
| IR-0 | Remains the entry gate for all successor implementation | Re-run only after the amended authority publishes and validates |
| RG-15 | Release closure is unrelated to this correction | No change |

The checkpoint is not a new story. It creates no story lifecycle key, no
separate story final record, and no new obligation denominator. It is an
ordered implementation slice whose evidence is consumed and rerun by the
existing `AC-7.1-01` before Story 7.1 can complete.

### Artifact conflicts and proposed treatments

| Artifact | Conflict | Proposed treatment |
| --- | --- | --- |
| PRD and addendum | None | No change |
| `epics.md` | Effective Story 7.1 is atomic only at final completion, not at implementation order | Append a v11 amendment defining checkpoint `7.1-SCHEMAS`; preserve v9/v10 bytes |
| `architecture.md` | Current graph and hold rules know only whole Story 7.1 | Append the matching checkpoint semantics while preserving the fail-closed hold model |
| `v9/story-contracts/7.1.json` | Correct as the v10 whole-story completion contract but insufficient as the sole execution authority | Retain it as the base completion contract at the new `PC`; make the v11 sidecar bind its digest and the amendment digest |
| New schema-slice sidecar | No machine-readable authority exists for partial execution | Generate and validate one closed `story-slice-authority.v1` sidecar |
| Execution graph | Story 7.1 is the first executable node | Add non-story checkpoint node `7.1-SCHEMAS` before Story 7.1 |
| Authority bundle/current view | Do not include the checkpoint | Regenerate atomically and include its schema, sidecar, digest, graph edge, and explanatory projection |
| `sprint-status.yaml` | Current publisher would erase committed retrospective state | Preserve `epic-6-retrospective: done` and all six action items; normalize only representation intentionally |
| Publisher tests | Assert 27 stories and a whole-story graph but do not model the checkpoint or retrospective preservation | Add exact checkpoint, hold, preservation, mutation, and output-drift tests |
| Story 7.1 implementation specification | Correctly blocked and uses a schema-only command/result path | Keep untouched pending approval; after authority publication, align only authority bindings/status and any exact command differences through the approved spec workflow |
| UX specification/map | No UI or UX scope is activated | No semantic change; preserve 52/28 parity |

### Technical and operational impact

The correction changes planning Markdown, planning-publication schemas and
tooling, generated planning projections, and focused publisher tests. These are
authority-correction surfaces permitted while the implementation hold is
active. It does not authorize the three product-workflow schemas themselves;
those remain Story 7.1 implementation.

The principal risks are:

- accidentally treating checkpoint evidence as a passing final record;
- allowing a prose-only hold exception;
- making the graph and Story 7.1 contract disagree silently;
- invalidating candidate-bound companions without regenerating all of them;
- losing retrospective status/action items during publication; and
- accepting a stale schema result at the later Story 7.1 final candidate.

Each risk receives a machine check in the detailed proposal below.

## 3. Recommended Approach

### Option evaluation

| Option | Viability | Effort | Risk | Decision |
| --- | --- | --- | --- | --- |
| Direct Adjustment | Viable as an append-only checkpoint amendment inside Story 7.1 | Moderate | Medium until publication and gate tests pass | **Selected** |
| Potential Rollback | Would remove a useful blocked specification or erase retrospective state without fixing the authority boundary | Low apparent effort | High integrity risk | Reject |
| Full story split/renumber | Technically possible, but would renumber the Epic 7 chain and disturb all downstream predecessor and obligation bindings | High | High unnecessary authority churn | Reject |
| PRD/MVP Review | Product requirements, preservation denominators, and UX disposition are unchanged | Medium | Solves the wrong problem | Reject |

### Selected path

Publish an append-only v11 authority amendment that introduces
`7.1-SCHEMAS` as a non-story execution checkpoint. The checkpoint authorizes
only the three missing schemas and their schema-contract tests after the normal
global gates are satisfied. Story 7.1 retains its complete six-scenario
contract and its single authoritative JSON/Markdown final record.

The amended sequence is:

```text
V11 canonical authority committed
  -> deterministic publication and --check PASS
  -> independent IR-0 READY for the same PC and bundle
  -> explicit release-owner LIFTED hold record
  -> 7.1-SCHEMAS implementation and checkpoint evidence
  -> remaining Story 7.1 generator work
  -> fresh AC-7.1-01 through AC-7.1-06 and one Story 7.1 final record
```

There is deliberately no `ACTIVE_WITH_EXCEPTION` state. While the effective
hold is `ACTIVE`, only planning-authority correction may proceed. Schema
implementation begins only after a valid, candidate-matched `LIFTED` decision.

### Scope and schedule classification

- **Change scope:** Moderate — one existing story gains a machine-bound
  execution checkpoint and the publisher gains matching projection rules.
- **Product/MVP scope:** Unchanged.
- **Implementation estimate:** Deferred until publication, independent IR-0,
  and release-owner hold lift.
- **Schedule impact:** Story 7.1 remains blocked. The schema slice can be the
  first Story 7.1 implementation activity after the hold lifts; it does not
  unlock Story 7.2.
- **Risk if adopted:** Medium, concentrated in candidate binding, graph parity,
  and partial-evidence semantics.
- **Risk if rejected:** High. The schema-only specification remains
  unauthorized, and implementing it would violate both Story 7.1 authority and
  the global hold.

## 4. Detailed Change Proposals

### 4.1 Canonical Story 7.1 execution amendment

**Artifact:**
`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

**OLD — effective Story 7.1 execution boundary:**

> The four schemas and the v2 generator core together produce schema-valid
> authoritative JSON plus deterministic Markdown; the story completes from
> `AC-7.1-01` through `AC-7.1-06` and one final-record bundle.

No smaller executable authority is defined.

**NEW — proposed v11 effective execution boundary:**

Append a successor amendment with new epic and architecture authority
identities selected during publication. Preserve the complete Story 7.1
bounded outcome, rollback ceiling, six acceptance scenarios, and final-record
paths. Add the following ordered checkpoint:

#### Checkpoint `7.1-SCHEMAS` — closed schema contracts

**Purpose:** establish the three missing closed Draft 2020-12 schemas and prove
all four canonical schemas through the `v2_schema_contract` selector before
generator implementation.

**Entry conditions:**

1. completed Story 6.2 remains the historical predecessor;
2. the v11 planning publisher and `--check` pass against one committed `PC`;
3. an independent IR-0 report is `READY` for that exact `PC`, authority bundle,
   and authority identities; and
4. the release owner records a valid, candidate-matched `LIFTED` decision in
   the separately governed implementation-hold record.

**Writable implementation paths:**

```text
_bmad/schemas/v9-acceptance-result-v1.schema.json
_bmad/schemas/v9-frozen-inventory-v1.schema.json
_bmad/schemas/story-final-record-v2.schema.json
_bmad/scripts/tests/test_generate_story_record.py
artifacts/v9/schema-slice/v2-schema-contract.xml
```

The result path is generated evidence and does not authorize any additional
source path.

**Read-only inputs:**

```text
_bmad/schemas/v9-story-contract-v1.schema.json
_bmad-output/planning-artifacts/v9/story-contracts/7.1.json
_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json
```

**Explicit prohibitions:**

- no edit to `_bmad/scripts/generate_story_record.py`;
- no acceptance-result instance or Story 7.1 final-record output;
- no implementation of `AC-7.1-02` through `AC-7.1-06`;
- no Story 7.2-7.4 work;
- no production/public-contract, package, deployment, submodule, gitlink,
  completed-record, accepted-evidence, or signed-evidence change; and
- no story `done` transition.

**Checkpoint command:**

```text
python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml
```

**Checkpoint result:** exit `0`, nonempty `PASS`, all four schemas pass the
Draft 2020-12 metaschema, representative valid instances pass, and missing,
extra, malformed, duplicate, non-normalized, or permissive mutations fail with
`OUTPUT_SCHEMA_INVALID` semantics and restore fixtures byte-identically.

**Completion effect:** checkpoint evidence may support continued Story 7.1
implementation and may move Story 7.1 to `in-progress`; it never moves the
story to `done`, never serves as its final record, and never satisfies a
successor predecessor. `AC-7.1-01` must run again or be proven current against
the final `SC-7.1` before `AC-7.1-06` can pass.

**Rollback boundary:** remove only the three new schemas, schema-specific test
changes, and checkpoint results. Preserve the existing story-contract schema,
planning authority, publisher, completed history, and all non-checkpoint work.

### 4.2 Machine-readable slice authority

**New generated artifact:**
`_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json`

**New planning schema:**
`_bmad/schemas/v11-story-slice-authority-v1.schema.json`

The sidecar must be recursively closed and contain at least:

```yaml
schemaVersion: hexalith.conversations.story-slice-authority.v1
sliceId: 7.1-SCHEMAS
storyId: 7.1
authority:
  epic: <v11-epic-authority>
  architecture: <v11-architecture-authority>
  planningCandidate: <PC>
  authorityBundlePath: _bmad-output/planning-artifacts/v9-authority-bundle-v1.json
baseStoryContract:
  path: _bmad-output/planning-artifacts/v9/story-contracts/7.1.json
  sha256: <base-contract-digest>
  epic: epic-6-authority-2026-08-03-v10
  architecture: conversations-architecture-2026-08-03-v10
amendmentSectionSha256: <v11-story-7.1-amendment-digest>
predecessors:
  - 6.2
  - IR-0
holdRequirement:
  effectiveState: LIFTED
  recordPath: _bmad-output/planning-artifacts/implementation-hold-v1.json
writablePaths: <exact ordinal list above>
readOnlyInputs: <exact ordinal list above>
prohibitedPaths: <closed exact/prefix rules>
acceptance:
  scenarioId: AC-7.1-01
  command: <exact checkpoint command>
  result: PASS
  passExitCodes: [0]
  failExitCodes: [1, 5]
  blockedExitCodes: [2, 3, 4]
completionEffect:
  storyDoneAllowed: false
  finalRecordProduced: false
  successorUnlocked: false
rollback: <exact checkpoint rollback boundary>
```

The sidecar records the bundle path but not the bundle digest, avoiding a
self-reference. The bundle lists the sidecar and its SHA-256. The later IR-0
and hold-decision records bind the completed bundle digest. Publisher and
schema tests must enforce this one-way digest order.

The existing `_bmad/schemas/v9-story-contract-v1.schema.json` remains
byte-identical, including its v10 authority constants. The generated
`7.1.json` remains the v10 base whole-story completion contract and retains its
existing closed shape, six scenarios, and final-record paths while binding the
new `PC`. The v11 sidecar is the effective execution amendment and must bind
the exact base-contract digest plus the canonical v11 amendment-section
digest. The publisher must distinguish base story-contract authority from the
current amendment authority instead of rewriting the v1 schema or silently
ignoring the sidecar.

### 4.3 Execution graph and hold semantics

**Artifacts:** execution-graph schema/output, architecture overlay, current
execution view, and their publisher tests.

Add node `7.1-SCHEMAS` with kind `checkpoint` and effective predecessors
`6.2` and `IR-0`. Add it as an additional execution predecessor of Story 7.1.
The graph must still prove that every successor story is downstream of `IR-0`
and remain acyclic.

The story contract retains `6.2` as its exact story predecessor because
`7.1-SCHEMAS` is not a story. The graph validator must explicitly reconcile
the supplemental checkpoint edge from the machine-readable slice authority;
it must not accept arbitrary graph-only predecessors.

Publication-time projections and the authority bundle continue to state
`implementationHold: ACTIVE`. This is the fail-closed default and does not
conflict with a later mutable hold-decision record, which remains outside the
bundle digest. Effective implementation authorization exists only when all of
the following bind the same authority:

1. publisher validator `PASS`;
2. independent IR-0 `READY`;
3. valid release-owner `LIFTED` decision; and
4. no later authority, candidate, bundle, graph, or story-contract drift.

Missing, invalid, stale, mismatched, revoked, or non-`READY` evidence evaluates
to `ACTIVE`. No scoped exception or story status can substitute for these
conditions.

### 4.4 Publisher drift and sprint projection preservation

**Artifacts:**
`_bmad/scripts/publish_v9_planning_authority.py`, its focused tests,
`sprint-status.yaml`, and the regenerated companion set.

Do not repair the current drift by hand-restoring the old generated sprint
bytes. Instead:

1. make the committed `epic-6-retrospective: done` state an intentional input
   to the new publication;
2. preserve the six committed Epic 6 retrospective action rows exactly unless
   a separately approved retrospective transition changes them;
3. normalize `last_updated` to ISO `2026-08-04` during the v11 publication;
4. keep Epic 7 and Story 7.1 lifecycle values at `backlog` during planning
   publication; a checkpoint declaration is not implementation start;
5. add one prominent v11 notice that the schema checkpoint is planning-only
   until IR-0 and the release-owner lift are valid;
6. include the checkpoint schema/sidecar and all amended generated companions
   in the bundle and managed namespace; and
7. regenerate from a new committed planning candidate rather than rebinding
   the existing candidate in place.

Add tests proving:

- `epic-6-retrospective: done` cannot regress to `optional`;
- all six retrospective action IDs and their open status survive publication;
- a missing, duplicate, or reordered required retrospective row fails with a
  stable projection blocker rather than being silently deleted;
- `7.1-SCHEMAS` appears once in the sidecar, graph, and current view, and never
  as a sprint story lifecycle key;
- the checkpoint cannot report `storyDoneAllowed: true`, produce final-record
  paths, or omit `LIFTED` from its entry conditions;
- Story 7.2 remains dependent on complete Story 7.1;
- the v11 bundle remains publication-time `ACTIVE` and does not contain an
  IR-0 or hold-decision result;
- all managed outputs reproduce byte-identically from the new `PC`; and
- the exact check command exits `0` and prints `V9_PLANNING_AUTHORITY_OK`.

### 4.5 Prepared Story 7.1 specification

**Artifact:**
`_bmad-output/implementation-artifacts/spec-7-1-define-the-final-record-schema-and-deterministic-generator-core.md`

The current user-authored changes are preserved as evidence of the block. This
proposal does not edit them.

After this proposal is approved and the v11 authority is published, the spec
workflow may update only the fields needed to bind the new authority, checkpoint
sidecar, exact gate evidence, and lifecycle status. Its frozen schema-only
intent, three-schema scope, generator prohibition, submodule prohibition, and
no-final-record rule remain unchanged unless Jerome separately renegotiates
them.

The spec must continue to report `blocked` while any publication, IR-0, or hold
condition is unsatisfied. Approval of this proposal alone is not
implementation authorization.

### 4.6 PRD, architecture invariants, and UX

**PRD/addendum:** no normative change. The 124 functional requirements, 77
non-functional requirements, FR-16 deferral, FR-20/SM-C1 preservation rule,
SM-1 baseline, and universal SM-C2 rule remain unchanged.

**Architecture:** append only the checkpoint, sidecar ownership, graph parity,
and hold interpretation. Runtime authority, tenant isolation, EventStore
ownership, candidate scopes, final-record derivation, evidence safety,
deployment, and release-gate invariants remain unchanged.

**UX:** no change. All 52 UX decisions and 28 UX acceptance identifiers remain
`preserved-not-activated` under Stories 8.1-8.2. No UI work is authorized.

### 4.7 Required validation before IR-0

The authority/publication handoff is incomplete unless all of these commands
pass from the repository root against the same committed planning candidate:

```text
python3 -m pytest -q _bmad/scripts/tests/test_publish_v9_planning_authority.py
python3 _bmad/scripts/publish_v9_planning_authority.py --repository . --check
git diff --check
```

The focused publisher suite must be non-vacuous. The exact `--check` command
must exit `0` and identify the new `PC` and bundle digest. These checks do not
run IR-0, lift the hold, or implement Story 7.1.

## 5. Implementation and Handoff Plan

### Ownership

| Role | Responsibility |
| --- | --- |
| Product Manager | Approve the Story 7.1 checkpoint semantics, exact scope, no-done effect, and v11 epic amendment |
| Solution Architect | Approve the checkpoint graph, sidecar ownership, candidate binding, and unchanged hold model |
| Workflow owner | Update the deterministic publisher, schemas, projections, and sprint preservation logic without hand-editing generated outputs |
| Quality/Test Architect | Add mutation and parity tests, validate the complete publication, and conduct independent IR-0 without a predetermined result |
| Release owner | Keep the effective hold `ACTIVE` unless the candidate-matched evidence supports an explicit `LIFTED` record |
| Developer | After lift only, execute the bounded schema checkpoint; do not implement generator/final-record scope from the slice |

### Ordered handoff

1. Obtain explicit approval of this proposal.
2. Mark this proposal approved without changing implementation status.
3. Append the v11 epic and architecture authority amendments.
4. Add the planning-only slice schema and publisher/test support.
5. Commit the canonical authority source candidate through the repository's
   normal governed process.
6. Generate the complete companion set atomically from that committed `PC`.
7. Run the focused publisher tests and exact `--check` command.
8. Run an independent IR-0 assessment against the same `PC` and bundle without
   instructing its verdict.
9. If and only if IR-0 is `READY`, obtain an explicit release-owner hold
   decision.
10. If and only if the effective decision is `LIFTED`, update the Story 7.1
    spec binding/status and execute `7.1-SCHEMAS`.
11. Keep Story 7.2 blocked until all Story 7.1 scenarios and its final record
    pass at a compatible final candidate.

### Success criteria

- `SC-01`: An approved append-only authority amendment defines
  `7.1-SCHEMAS` as a non-story checkpoint.
- `SC-02`: The machine-readable sidecar closes the exact writable, read-only,
  prohibited, acceptance, rollback, and completion-effect fields.
- `SC-03`: Story 7.1 retains six final acceptance scenarios and one
  JSON/Markdown final record; the checkpoint cannot mark it done.
- `SC-04`: Story 7.2 remains gated by complete Story 7.1.
- `SC-05`: The existing story-contract schema remains byte-identical.
- `SC-06`: The graph contains one checkpoint node, remains acyclic, and keeps
  all successor stories downstream of IR-0.
- `SC-07`: The global hold has no schema exception; implementation requires a
  valid release-owner `LIFTED` record after validator `PASS` and IR-0 `READY`.
- `SC-08`: Epic 6 retrospective status remains `done`, and all six open action
  items survive deterministic publication.
- `SC-09`: The full managed companion set reproduces byte-identically from one
  new committed planning candidate.
- `SC-10`: The exact publisher check exits `0` with
  `V9_PLANNING_AUTHORITY_OK`.
- `SC-11`: PRD, preservation, UX, runtime, deployment, completed-record, and
  submodule boundaries remain unchanged.
- `SC-12`: The user's prepared Story 7.1 specification remains blocked until
  every publication, IR-0, and hold condition is actually satisfied.

## 6. Change-Scope Checklist

### Trigger and context

- [x] Triggering unit identified: Story 7.1 schema-only preparatory work.
- [x] Issue classified: planning-authority decomposition and publication drift.
- [x] Evidence captured: effective Story 7.1 contract, active hold, graph
  dependency, missing schemas, prepared blocked spec, exact publisher exit.

### Epic and story impact

- [x] Epic 7 can still complete without product-scope change.
- [x] Existing Story 7.1 remains the correct owner.
- [x] Stories 7.2-7.4 and Epics 8-15 remain valid and blocked by their existing
  complete-story predecessors.
- [N/A] No new epic or story is required.
- [x] No story renumbering or downstream resequencing is required.

### Artifact conflict analysis

- [x] PRD conflict: none.
- [!] Architecture amendment required for checkpoint/graph semantics.
- [N/A] UX change not required; preservation-only bindings remain unchanged.
- [!] Publisher, graph, bundle, view, story binding, sprint projection, and
  focused tests require coordinated changes after approval.

### Path evaluation

- [x] Direct Adjustment is viable and selected.
- [x] Rollback is rejected because it would erase useful evidence without
  resolving the authority gap.
- [x] Full story split/renumber and PRD/MVP review are rejected as unnecessary.
- [x] Effort: Moderate. Risk: Medium. Schedule remains gate-driven.

### Proposal components

- [x] Issue summary and evidence included.
- [x] Epic, story, artifact, technical, and schedule impact included.
- [x] Exact old/new execution boundary and machine sidecar proposed.
- [x] Hold, rollback, validation, ownership, and handoff specified.

### Review and handoff

- [x] User review completed in Batch mode.
- [x] Explicit approval received from Jerome on 2026-08-04.
- [N/A] No sprint-status structural edit is due in this workflow because no
  epic or story is added, removed, or renumbered.
- [!] Authority implementation and deterministic publication are routed but
  not performed by this workflow.
- [!] Independent IR-0 and release-owner decision remain separate future gates.

## 7. Approval Gate

**Status:** Approved by Jerome on 2026-08-04.

Approval authorizes the planning-authority correction and its deterministic
publication work only. It does not approve Story 7.1 implementation, declare
IR-0 `READY`, lift the global hold, authorize release, or permit a commit/push
outside the repository's separately governed process.

## 8. Workflow Execution Log

- `2026-08-04`: Blocking condition verified against the canonical Story 7.1
  contract, execution graph, hold rules, prepared implementation spec, and
  exact publisher check.
- `2026-08-04`: Batch impact analysis completed across PRD, epics,
  architecture, UX, sprint projection, publisher, and focused tests.
- `2026-08-04`: Jerome reviewed the complete proposal and explicitly approved
  it for planning-authority implementation.
- **Final scope:** Moderate.
- **Primary route:** Product Owner / Developer for backlog-authority and
  deterministic publication work.
- **Required supporting review:** Solution Architect for the architecture/graph
  amendment, Quality/Test Architect for publisher validation and independent
  IR-0, and Release Owner for any later hold decision.
- **Sprint-status disposition:** no structural edit in this workflow. The
  committed Epic 6 retrospective state and action items remain untouched; their
  deterministic preservation is part of the routed v11 implementation.
- **Effective implementation state:** global hold remains `ACTIVE`; Story 7.1
  schema implementation remains blocked.
