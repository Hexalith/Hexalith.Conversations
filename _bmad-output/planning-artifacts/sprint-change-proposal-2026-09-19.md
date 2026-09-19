---
title: "Sprint Change Proposal — Story 7.1 Planning-Authority Route Repair"
date: "2026-09-19"
project: "Conversations"
mode: "Batch"
status: "APPROVED AND APPLIED"
scope: "Moderate — planning-authority routing correction; no implementation authorization"
changeOwner: "Product/Planning owner"
approvalOwner: "Jerome Piquot"
approvalRecord: "Explicit user response: continue and approve"
implementationHold: "ACTIVE"
---

# Sprint Change Proposal — Story 7.1 Planning-Authority Route Repair

## 1. Issue Summary

`bmad-build` discovers `_bmad-output/planning-artifacts/epics.md` as the whole
epics document, but that file is an unfinished extraction draft. Its frontmatter
is `draft-non-authoritative`, its overview says it does not amend V14, its
additional requirements select V14 architecture, and it ends with the unresolved
`{{requirements_coverage_map}}` and `{{epics_list}}` placeholders. It therefore
does not contain an Epic 7 title, goal, or story list from which
`compile-epic-context.md` can regenerate `epic-7-context.md`.

The cached `_bmad-output/implementation-artifacts/epic-7-context.md` is also
stale. It correctly names the Epic 7 backlog authority
`epic-6-authority-2026-08-18-v14`, but incorrectly pairs it with
`conversations-architecture-2026-08-18-v14`. The last complete architecture
marker is now `conversations-architecture-2026-09-16-v15`, whose effective hold
is `ACTIVE`.

The resulting failure is an authority-route defect, not a request to change
Epic 7 scope or authorize Story 7.1. The repair must let `bmad-build` resolve one
complete Epic 7 slice while preserving every V1-V21 authority and evidence byte.

### Trigger and evidence

- Triggering story: Story 7.1, **Define the final-record schema and
  deterministic generator core**.
- Issue type: planning-authority discovery and stale generated-context identity.
- Root epics draft before any correction: 28,871 bytes, SHA-256
  `11b4678fc7298c8e1fef8c3c729436be6efed20c5115eda29cb42bb73f3520b6`.
- Immutable backlog carrier:
  `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`,
  SHA-256
  `7098e7355522f503e136dc8c5e84ce5c873cb5bad08b92aa54495811c54853b6`.
- Exact V14 epic block: 11,093 bytes, SHA-256
  `acd5c07c72d5145bb6477877cab21af710beb8cf172ccfde66837992e41c35c1`,
  measured from the first byte of its BEGIN marker through the last byte of its
  END marker, excluding the following line feed.
- Exact V14 architecture predecessor block: 3,873 bytes, SHA-256
  `d33d977fda0776377684439bb7e78769a6b9a0279c293b8a08e44dfad8466dc5`,
  matching the V15 BEGIN marker.
- Exact V15 architecture block: 35,772 bytes, SHA-256
  `85c7418ca55b1c67be90eed78e280c792ce92360e0cf2817237e41a3bcdfccbe`,
  measured with the same marker-slice convention.
- Planning-candidate evidence:
  `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json`, SHA-256
  `8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3`.
- V15-selected execution/checkpoint evidence:
  `_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json`,
  SHA-256
  `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`.
- Sprint projection:
  `_bmad-output/implementation-artifacts/sprint-status.yaml`, SHA-256
  `3cb6d1e745d78b9c1b34eba3382e577e33af6179583299a0cb7e78c35f9a9833`.
  It links to the immutable backlog carrier and keeps Epic 7 and Stories
  7.1-7.4 at `backlog`.
- Current effective-hold check at repository commit
  `2c6a4af775eaa969deb4bbbfc294be1c472afd7d` returns `FAIL`, blocker
  `V21_DESCENDANT_GITLINK_DRIFT`, and `effectiveHold: ACTIVE`.

## 2. Impact Analysis

### Epic and story impact

Epic 7 remains **Reliable Mechanical Completion Records** with the unchanged
outcome “Developers receive deterministic, candidate-bound completion records.”
Its story inventory and order remain 7.1, 7.2, 7.3, and 7.4. No epic or story is
added, removed, renumbered, reordered, or moved out of `backlog`.

The repair affects only the route through which `bmad-build` discovers the
current Epic 7 planning slice. Downstream Epics 8-15 remain dependent on the
compatible completion of Epic 7 as already recorded. Epic 16 remains separately
qualified by V15 and receives no execution authority from this correction.

### Artifact conflicts

| Artifact | Conflict | Required disposition |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/epics.md` | Draft, non-authoritative, stale V14 architecture statement, missing Epic 7 because placeholders remain | Append one complete, delimited Epic 7 route-correction block; do not rewrite the 28,871-byte prefix |
| `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` | None; it is the immutable V14 backlog carrier | Preserve byte-identically |
| `_bmad-output/planning-artifacts/architecture.md` | None; V15 is current | Preserve byte-identically and cite its exact identity and current gate semantics |
| `_bmad-output/implementation-artifacts/epic-7-context.md` | Stale V14 architecture identity and stale candidate-lift interpretation | Do not hand-edit in this correction; invalidate it by the exact identity mismatch and let `bmad-build` regenerate it from the repaired epics authority |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Its V14 comment is historical and its source path points to the immutable carrier, but its Epic 7 lifecycle rows are correct | Preserve byte-identically; bind the correction to the exact existing keys and `backlog` values |
| V1-V21 overlays, sidecars, evidence, completed records, signed evidence | Immutable history | Preserve byte-identically; treat V15-V21 sidecar effects as point-in-time evidence under V15 AD-3 |

### PRD, architecture, and UX impact

- PRD scope is unchanged. The current PRD still records FR-20/SM-C1 as
  `PENDING`, SM-C2 as `FAILED`, OQ-1 as `BLOCKED`, and the implementation hold
  as `ACTIVE`.
- Architecture is unchanged. V15 and every inherited rule not expressly
  superseded continue to govern.
- UX has no activated implementation impact. The 52 UX decisions and 28 UX
  acceptance identifiers remain preserved denominator obligations only.
- Product/runtime code, public contracts, dependencies, packages, deployment,
  submodules, and gitlinks are outside this correction.

## 3. Recommended Approach

Use a **Direct Adjustment**: append one authority-route block to the discovered
root `epics.md`. The block will bind its untouched prefix, designate itself as
the complete Epic 7 discovery slice, retain the V14 epic/backlog authority,
select V15 architecture, record the canonical title/goal/story list, bind the
sprint rows, and state every current entry gate fail-closed.

This is lower risk than rewriting the draft or moving the immutable V14 carrier.
It also avoids treating a generated context file as authority. Rollback is not
appropriate because the defect is a missing current route, and an MVP review is
not appropriate because product scope is unchanged.

- Effort: low.
- Authority risk: high if identities or gates are misstated; mitigated by exact
  marker hashes, append-only prefix binding, repository-relative paths, and a
  no-authorization clause.
- Timeline impact: Story 7.1 remains blocked; this correction removes only the
  context-compilation blocker.
- Scope classification: moderate, because planning authority and build routing
  require Product/Planning ownership and release-owner approval even though no
  backlog status changes.

## 4. Detailed Change Proposals

### 4.1 Designated Epic 7 planning route

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**OLD:** The file ends with unresolved `{{requirements_coverage_map}}` and
`{{epics_list}}` placeholders and contains no Epic 7 definition. Its earlier
draft and V14 statements are all a build route can discover.

**NEW:** Append the exact block in §5. The earlier content remains untouched and
historical. Within this file, the last complete correction marker controls Epic
7 discovery and supersedes the earlier draft disclaimer, V14-architecture
selection, and unresolved placeholders only for Epic 7 context compilation.

**Rationale:** `compile-epic-context.md` requires a title, goal, story list, and
exact overlay/architecture identities in the designated epics file. The append
supplies those without rewriting V1-V21 or creating a second backlog authority.

### 4.2 Generated Epic 7 context

**Artifact:** `_bmad-output/implementation-artifacts/epic-7-context.md`

**OLD frontmatter:**

```yaml
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-08-18-v14'
```

**NEW regeneration result required from `bmad-build`:**

```yaml
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-16-v15'
```

The regenerated context must use the exact title, goal, and story list in §5,
must state `ACTIVE` rather than inherit a raw V20/V21 `LIFTED` field, and must
not claim Story 7.1 entry until all gates in §5 pass. This workflow does not
hand-edit or pre-generate that cache.

### 4.3 Sprint status

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD and NEW:** byte-identical. The following existing linkage is required and
must remain exact:

- `source_epics` points to the immutable V14 backlog carrier;
- `epic-7: backlog`;
- Story keys 7.1 through 7.4 exist exactly once and remain `backlog`;
- `epic-7-retrospective: optional`.

The root `epics.md` append is a current build-discovery route over that immutable
carrier, not a lifecycle-state projection and not a replacement of completed
history.

## 5. Exact Append-Only Correction

Upon approval, append the following text verbatim after the current final line
of `_bmad-output/planning-artifacts/epics.md`:

````markdown
<!-- EPIC-7-PLANNING-AUTHORITY-ROUTE-CORRECTION:BEGIN correction-id=epic-7-planning-route-2026-09-19 source-prefix-bytes=28871 source-prefix-sha256=11b4678fc7298c8e1fef8c3c729436be6efed20c5115eda29cb42bb73f3520b6 overlay-version=epic-6-authority-2026-08-18-v14 architecture-version=conversations-architecture-2026-09-16-v15 hold=ACTIVE -->

## Epic 7: Reliable Mechanical Completion Records

**Authority status:** This is the last complete Epic 7 discovery block in this
file and is the designated Epic 7 input for `bmad-build` context compilation.
It supersedes this file's earlier `draft-non-authoritative` disclaimer, V14
architecture selection, and unresolved `{{requirements_coverage_map}}` and
`{{epics_list}}` placeholders only for Epic 7 discovery. The 28,871-byte prefix
remains immutable and is bound by SHA-256
`11b4678fc7298c8e1fef8c3c729436be6efed20c5115eda29cb42bb73f3520b6`.

**Epic/backlog authority:** `epic-6-authority-2026-08-18-v14`, carried by
`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`.
That carrier remains immutable and is not replaced or narrowed by this route.

**Architecture authority:** `conversations-architecture-2026-09-16-v15`, the
last complete marker in `_bmad-output/planning-artifacts/architecture.md`, plus
every earlier invariant it does not explicitly supersede.

**Required compiled-context frontmatter:**

```yaml
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-16-v15'
```

**Goal:** Provide deterministic, candidate-bound completion records whose test
counts, exact changed-path set, candidate identity, submodule condition, root
gitlink state, and verdict are derived from machine results and Git objects
rather than copied by a caller, so no story can reach review or done with
vacuous scope, stale or incomplete tests, dirty or moved dependencies,
displaced workflow integration, or rewritten historical evidence.

### Stories

- Story 7.1: Define the final-record schema and deterministic generator core
- Story 7.2: Derive test, path, candidate, submodule, and gitlink facts
- Story 7.3: Integrate generation into every blocking completion transition
- Story 7.4: Verify historical mode and required fault-injection blockers

### Governing constraints

- Story 6.2 is the immutable hard epic predecessor. Superseded Story 6.8 and its
  partial implementation remain unaccepted inputs only.
- Records derive counts, commits, repository-relative paths, mode-`160000`
  root gitlinks, and verdicts. Caller-authored completion facts are prohibited.
- Required test results must be current, nonempty, passing, and contain zero
  failed, unapproved-skipped, or not-run checks. Environmental inability is
  `BLOCKED`, never `PASS`.
- Changed-path and input/output bindings are repository-relative,
  slash-separated, contain no `..` segment or backslash, and never name a path
  beneath a root-declared `references/` gitlink as an owned mutation.
- Submodules are never initialized, updated, or traversed to derive evidence.
  Gitlink identity comes from raw root-tree mode `160000` entries and must match
  the ordinal root `.gitmodules` inventory exactly.
- One authoritative JSON final record and its digest-bound deterministic
  Markdown rendering are produced only after every declared scenario passes.
- Historical verification is read-only. V1-V21 authority, evidence, completed
  records, accepted baselines, signed evidence, product scope, public value
  sets, and gitlink evidence remain immutable point-in-time history.
- V15-V21 sidecars are evidence at their recorded candidates. A raw
  `authorityEffect`, including `LIFTED`, is never live authorization.
- Epic 7 exits only when Stories 7.1-7.4 are `done` at compatible accepted
  candidates. Story 7.2 remains locked until Story 7.1 reaches the V15 AD-4
  terminal `ACCEPTED` state through atomic successor authority.

### Current hold and Story 7.1 entry gates

The effective implementation hold is `ACTIVE`. This block fixes discovery only
and does not authorize Story 7.1 implementation, review, completion, release, or
push. Story 7.1 may enter implementation only when **all** of the following are
true at the evaluated protected candidate:

1. The V15 AD-3 selected protected checker returns a schema-valid, explicitly
   scoped `PASS`; missing input, digest mismatch, incomplete history, nonzero
   exit, `FAIL`, `BLOCKED`, candidate drift, or gitlink drift resolves to
   `ACTIVE`.
2. AR-15 is no longer `BLOCKED_BOOTSTRAP_AUTHORITY`: the exact external
   no-bypass organization-ruleset validator is identity/digest pinned, the
   one-time atomic V22 recovery candidate passes and merges, its nonce is
   consumed, and the generic resolver passes on protected `main`. V22 itself
   preserves `ACTIVE` and grants no Story 7.1 execution authority.
3. The current workflow-route inventory, generic marker resolver, and Quality
   conformance gate required by V15 AD-9 exist and pass.
4. `_bmad-output/planning-artifacts/production-operational-envelope-v1.md`
   exists and its V15 AD-5 gate passes; any waiver is explicit, current, and
   accepted by the Release owner.
5. A later owner-approved AD-4 successor authority enters
   `EXECUTION_ALLOWED`, binds the exact protected merge candidate, admissible
   integration paths, every root gitlink, and current checkpoint/input
   evidence, and explicitly retires or replaces V21's temporary descendant
   restriction.
6. The Story 6.2, `7.1-SCHEMAS`, IR-0, V19 checkpoint-completion, V20 input
   inventory, and Story 7.1 contract bindings required by that successor are
   present, digest-valid, candidate-compatible, and not stale. Their historical
   `READY`, `PASS`, or `LIFTED` fields do not satisfy current-state gates by
   themselves.
7. The PRD's independently blocking current gates pass or receive an explicit
   valid disposition under their own authority: FR-20/SM-C1 preservation is no
   longer `PENDING`, SM-C2 is no longer `FAILED`, and OQ-1 is no longer
   `BLOCKED`.
8. Sprint linkage still contains exactly one `epic-7` row and exactly one row
   for each Story 7.1-7.4. No lifecycle transition occurs before the preceding
   gates pass; until then all five rows remain `backlog`.

At repository commit `2c6a4af775eaa969deb4bbbfc294be1c472afd7d`,
the selected V21 current-state diagnostic returns `FAIL` with blocker
`V21_DESCENDANT_GITLINK_DRIFT`; AR-15 is `BLOCKED_BOOTSTRAP_AUTHORITY`; the V22
recovery artifacts, generic resolver, and production operational envelope are
absent; FR-20/SM-C1 is pending, SM-C2 is failed, and OQ-1 is blocked. Therefore
Story 7.1 is **not authorized** and the effective hold remains `ACTIVE`.

### Sprint-status linkage

`_bmad-output/implementation-artifacts/sprint-status.yaml` remains the lifecycle
projection and continues to link `source_epics` to the immutable V14 backlog
carrier. Its canonical Epic 7 keys are:

- `epic-7`
- `7-1-define-the-final-record-schema-and-deterministic-generator-core`
- `7-2-derive-test-path-candidate-submodule-and-gitlink-facts`
- `7-3-integrate-generation-into-every-blocking-completion-transition`
- `7-4-verify-historical-mode-and-required-fault-injection-blockers`
- `epic-7-retrospective`

This route correction changes none of their values.

<!-- EPIC-7-PLANNING-AUTHORITY-ROUTE-CORRECTION:END correction-id=epic-7-planning-route-2026-09-19 overlay-version=epic-6-authority-2026-08-18-v14 architecture-version=conversations-architecture-2026-09-16-v15 hold=ACTIVE -->
````

## 6. Validation Plan and Current Results

### Required application checks

1. Recompute the pre-append prefix byte count and SHA-256; both must remain
   exactly 28,871 and
   `11b4678fc7298c8e1fef8c3c729436be6efed20c5115eda29cb42bb73f3520b6`.
2. Confirm the only modification to `epics.md` is an append after byte 28,871.
3. Confirm exactly one complete BEGIN/END route-correction pair and one canonical
   Epic 7 title, goal, and four-story list in the correction block.
4. Verify every path named by the correction is normalized,
   repository-relative, slash-separated, contains neither a `..` segment nor a
   backslash, and resolves inside the repository root. No path under
   `references/` is a mutation target.
5. Recompute the V14 epic block, V14 architecture predecessor block, V15
   architecture block, V9 bundle, V21 sidecar, immutable carrier, and sprint
   projection hashes recorded in this proposal.
6. Confirm the V15 BEGIN/END markers agree on architecture identity, V14 epic
   authority, V21 sidecar head/digest, V9 candidate binding, and `hold=ACTIVE`.
7. Run the selected V21 current-state diagnostic at `HEAD`; it must be reported
   as observed, and anything other than scoped `PASS` must leave `ACTIVE`.
8. Parse all V11-V21 JSON evidence without modification and confirm Git reports
   no change to any V1-V21 evidence carrier.
9. Confirm `sprint-status.yaml` still links to the immutable carrier, has exactly
   one Epic 7 and four Story 7.x keys, and keeps all five lifecycle rows at
   `backlog`.
10. Perform a dry context extraction from the repaired `epics.md`. It must yield
    exactly the two required identities, canonical title, canonical goal, and
    four canonical stories. Do not write `epic-7-context.md` in this correction.
11. Run `git diff --check` and a final scoped `git status --short`.

### Checks already completed while drafting

- Required Hexalith baseline and repository guidance loaded.
- Working tree was clean on `main` at commit
  `2c6a4af775eaa969deb4bbbfc294be1c472afd7d` before this proposal was created.
- V9 bundle and V21 sidecar file hashes match the V15 authority table and marker.
- V14 architecture block byte count/hash match the V15 predecessor binding.
- V15 marker selects V14 epic/backlog scope, V21 sidecar evidence, V9 candidate
  evidence, and `hold=ACTIVE`.
- V21 effective-hold command returned `FAIL` /
  `V21_DESCENDANT_GITLINK_DRIFT` / `ACTIVE` at current `HEAD`.
- AR-15 V22 files, the generic resolver, and the AD-5 production operational
  envelope are absent.
- Sprint linkage contains Epic 7 plus Stories 7.1-7.4 exactly once, all at
  `backlog`.

## 7. Implementation Handoff

After explicit approval:

- Product/Planning applies the exact append in §5 to the root `epics.md` and
  executes every check in §6.
- Quality verifies prefix immutability, marker/hash lineage, repository-relative
  boundaries, V1-V21 non-modification, and the dry context extraction.
- `bmad-build` may then invalidate the stale cached `epic-7-context.md` because
  its architecture identity is V14 and regenerate a V14/V15 context from the
  repaired route.
- Release/Architecture/Quality retain ownership of AR-15, AD-4, and AD-5 gates.
  This proposal hands off no product implementation and no hold lift.

### Success criteria

- The root whole-document epics route contains one complete canonical Epic 7
  slice after an unchanged 28,871-byte prefix.
- A deterministic context compilation resolves
  `epic-6-authority-2026-08-18-v14` and
  `conversations-architecture-2026-09-16-v15` without inference.
- Sprint status remains linked and unchanged.
- Every V1-V21 evidence byte remains unchanged.
- Story 7.1 remains unauthorized while any current gate is absent, failed,
  blocked, stale, drifted, or not explicitly passed.

## 8. Checklist Record

- [x] 1.1-1.3 — Trigger, problem, and evidence identified.
- [x] 2.1-2.5 — Epic 7 remains viable; no scope, order, or future-epic change.
- [x] 3.1 — PRD conflict assessed; current failed/pending gates retained.
- [x] 3.2 — V15 architecture conflict and V14/V15 identity pairing resolved.
- [N/A] 3.3 — No UX activation or UI change.
- [x] 3.4 — Context cache and sprint linkage assessed.
- [x] 4.1 — Direct adjustment viable; low effort, authority-sensitive risk.
- [N/A] 4.2 — Rollback does not repair the missing route.
- [N/A] 4.3 — MVP scope is unchanged.
- [x] 4.4 — Direct append-only adjustment selected.
- [x] 5.1-5.5 — Issue, impact, approach, action plan, and handoff documented.
- [x] 6.1-6.2 — Proposal reviewed for completeness and consistency.
- [x] 6.3 — Explicit user approval received from Jerome: “continue and approve.”
- [N/A] 6.4 — No sprint-status value change is proposed.
- [x] 6.5 — Approved correction applied; verification and handoff completed.

## Approval Gate

Jerome explicitly approved this proposal with “continue and approve.” The exact
§5 block was appended to `epics.md`. This approval covers only the planning-route
correction and does not authorize regeneration by hand, a sprint-status change,
Story 7.1 implementation, release, or push.

## Application and Handoff Record

Applied on 2026-09-19 at root `HEAD`
`2c6a4af775eaa969deb4bbbfc294be1c472afd7d`.

- The original `epics.md` prefix remains exactly 28,871 bytes with SHA-256
  `11b4678fc7298c8e1fef8c3c729436be6efed20c5115eda29cb42bb73f3520b6`.
- The applied BEGIN-to-END correction block matches the approved §5 block
  byte-for-byte and has one marker pair, one Epic 7 heading, and four story
  entries.
- Dry context extraction returns exactly overlay
  `epic-6-authority-2026-08-18-v14`, architecture
  `conversations-architecture-2026-09-16-v15`, the canonical title and goal,
  and Stories 7.1-7.4.
- Every named mutation/evidence path is normalized and repository-relative;
  no submodule-internal path is a mutation target.
- The V14 epic block, V14 architecture predecessor block, V15 architecture
  block, V9 bundle, V21 sidecar, immutable epics carrier, sprint projection,
  cached context, and V11-V21 JSON evidence retained their pre-application
  hashes/bytes. All V11-V21 JSON files parse successfully.
- Sprint linkage remains exact: one Epic 7 row, four Story 7.x rows, all five
  lifecycle rows at `backlog`, and the retrospective at `optional`.
- The post-application V21 effective-hold check again returned `FAIL`, blocker
  `V21_DESCENDANT_GITLINK_DRIFT`, and `effectiveHold: ACTIVE`.
- The AD-5 operational envelope, AR-15 V22 recovery files, route inventory, and
  generic current-authority resolver remain absent. Story 7.1 is not
  authorized.
- `git diff --check` passes.
- A concurrent clean fast-forward inside `references/Hexalith.EventStore`
  moved its checked-out commit beyond the root gitlink during this workflow.
  That user/external submodule state was neither modified nor reverted by this
  correction and remains visible separately in root `git status`.

Handoff is to `bmad-build` for deterministic cache regeneration only. It must
reject the stale V14/V14 cached context, compile the V14/V15 replacement from
the designated block, and then stop on the recorded `ACTIVE` hold instead of
entering Story 7.1 implementation.
