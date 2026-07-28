---
title: "Sprint Change Proposal — Mechanical Final Story Record Generation"
project: "Conversations"
date: "2026-07-28"
status: "approved"
changeScope: "moderate"
mode: "incremental"
trigger: "Standing Epic 3 action item, reaffirmed by the Story 6.2 final-record corrections"
affectedAuthority: "epic-6-authority-2026-07-27-v3 and conversations-architecture-2026-07-27-v3"
proposedAuthority: "epic-6-authority-2026-07-28-v4 and conversations-architecture-2026-07-28-v4"
supersedesProposalScope: "none — additive to sprint-change-proposal-2026-07-27.md"
---

# Sprint Change Proposal — Mechanical Final Story Record Generation

## 1. Issue Summary

### Problem statement

The final story record — File List, test counts, submodule state, and root
gitlink state — is composed as prose from the agent's recollection of the
session, then corrected during review. It is not derived from the final measured
state of the repository. Every fact in it is already available mechanically, and
one of the four fact families (submodule and gitlink state) is already produced
mechanically by the Story 6.7 checker, but the record is retyped rather than
generated.

### How it was discovered

This is not a new finding. It is a standing action item in
`_bmad-output/implementation-artifacts/sprint-status.yaml` (epic 3, owner
`Dev workflow`, status `in-progress`):

> "Make final story record generation mechanical from final test counts, file
> list, submodule state, and root gitlink state."

It was carried into Epic 4 as action A1 in a narrower, epic-scoped form and
partially implemented. Story 6.2 then reproduced the original failure mode in
full, which is what prompted this proposal.

### Evidence

**Counts for one story disagree between passes of that same story.**
The `sprint-status.yaml` entry written 2026-07-27 records `1,908/1,908 across 8
projects (Conformance 418, Server 622, Contracts 618, Domain 185, Admin.Web 14,
IntegrationTests 14, Client 29, AppHost 8)`. The entry written 2026-07-28 for the
same story records `Server 631/631`, `Conformance 417/418`, and `EventStore
DomainService 146/147`. Both were typed by hand into three separate places — the
story's `Completion Notes List`, its `Debug Log References`, and the sprint-status
comment — with no machine artifact behind any of them.

**The File List is only half mechanical, and crosses a repository boundary.**
`6-2-migrate-conversations-to-platform-owned-hosting.md:626` states the list was
"Derived from `git diff --name-only 29def44..HEAD` … 49 paths". Immediately below
it, a second hand-maintained block titled "Review patch working-tree delta"
adds 34 more paths, eleven of which are *inside* a root-declared submodule
(`references/Hexalith.EventStore/src/…`). Those are files belonging to another
repository's record, listed as this story's files.

**Submodule and gitlink state was recorded at a candidate that stopped being
true.** The `review` transition recorded a passing gate at `953bf71`. Tenants and
Memories kept fast-forwarding in the working tree afterwards, so a re-run
returned `GITLINK_COMMIT_MISMATCH` and `UNCAPTURED_SUBMODULE_PROMOTION`. Three
commits were needed to recover: `c398ea2` (restore the gitlink), `39b9206`
(rebind the evidence), `680fa5f` (re-narrate the record).

**A partial implementation already exists and is inert.** Epic 4 action A1
produced `sprint-change-proposal-2026-07-14-epic-5-final-record-check.md` →
`spec-epic-5-final-record-check.md` → `tests/Test-StoryFinalRecord.ps1` (746
lines) and `tests/Test-StoryFinalRecord.Tests.ps1` (263 lines). That asset is
real and well built: it already parses TRX counters, compares path sets, checks
gitlink/index/worktree identities, freezes pre-existing dirty state by hash, and
runs a non-mutating public-contract-shape comparison. Three properties keep it
from closing the action item:

1. **It verifies; it does not generate.** Its `-InputPath` is a hand-authored
   `epic-5-final-record-input.json` in which a human types the expected counts
   and paths. The numbers still originate in prose; the check only proves the
   prose matches. A wrong number that is wrong in both places passes.
2. **It is bound to Epic 5.** `artifact = 'epic-5-final-record-check'`, hard-coded
   historical records for Stories 5.1/5.2/5.3, and an `approvedProposal` field
   naming the July 14 proposal.
3. **No workflow invokes it.** `grep` across `.claude/skills/` returns zero
   references. It is documented only as a manual `pwsh` invocation at
   `tests/README.md:78`. Stories 6.1, 6.2, and 6.7 all completed without it.

### Verified environment fact

The design below requires machine-readable test output. This repository's
approved fallback runs compiled xUnit v3 executables directly rather than
`dotnet test`, because VSTest socket creation is blocked in restricted
sandboxes. That fallback **does** emit TRX — the flag is `-trx <file>` on the
executable, not `dotnet test --report-trx`, which the runner rejects as an
unknown option. Confirmed on 2026-07-28:

```
tests/…/Hexalith.Conversations.Contracts.Tests -trx contracts.trx -noLogo
  → Total: 618, Errors: 0, Failed: 0, Skipped: 0
  → <Counters total="618" executed="618" passed="618" failed="0" … />
```

That is the same `/TestRun/ResultSummary/Counters` element the existing
PowerShell checker already parses, so the mechanism is proven in this exact
environment and under the documented socket constraint.

### Category

Partial implementation of an earlier approved corrective action — not a new
requirement, not a technical limitation, and not a strategic change. No product
behavior, contract, or user-facing surface is involved.

## 2. Impact Analysis

### Epic impact

Epic 6 remains viable and keeps every existing gate. One story is added; none is
removed, redefined, or resequenced away. Epics 1-5 are immutable history and are
untouched.

The non-obvious cost is that Epic 6's story set is governed by the append-only
authority overlay and pinned mechanically by
`tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs`:

- `ArchitectureVersion` / `OverlayVersion` / `PreviousOverlayVersion` constants
  (lines 28-31);
- the amendment disposition row list `["6.1", "6.2", "6.3", "6.4", "6.5", "6.6", "6.7"]`
  (line 519);
- the binding-order strings `"6.1 -> 6.7 -> 6.2"` (lines 365, 597, 647) and
  `"6.1 authority correction -> 6.7 -> 6.2 -> 6.5 -> 6.6"` (line 601);
- `epic-6-context.md` frontmatter equality for `overlay_version` and
  `architecture_version` (lines 627-628).

Adding Story 6.8 therefore requires a v4 append amendment, an architecture
version bump, a regenerated Epic 6 context, and matching edits to those
assertions. This is the same path Story 6.1 established and is the reason the
scope is Moderate rather than Minor.

### Story impact

| Story | Impact |
| --- | --- |
| 6.1 | None. Completed historical authority; the v4 amendment appends, it does not rewrite. |
| 6.2 | **Not re-driven.** Closes under the current manual process with its already-corrected record. Retro-verified read-only by 6.8's historical mode. |
| 6.3 | Closes through the generator. Its manifest is the largest record in the epic. |
| 6.4 | Closes through the generator. |
| 6.5 | Closes through the generator; SM-2 file/LOC counts stay story-owned evidence, not record fields. |
| 6.6 | Consumes generated records for every prior Epic 6 story; runs last, unchanged. |
| 6.7 | None. Its checker becomes an embedded input to the generator rather than a separate manual step. |
| **6.8 (new)** | Generate the final story record mechanically from measured state. |

Proposed binding sequence:

`6.1 -> 6.7 -> 6.2 -> 6.8 -> {6.3, 6.4, 6.5} -> 6.6`

Story 6.8 blocks the completion of 6.3, 6.4, 6.5, and 6.6 — the four remaining
record-bearing stories. It does not block 6.2.

### Artifact conflicts

| Artifact | Impact |
| --- | --- |
| PRD / addendum | **None.** FR-1…FR-20, 104 Feature-FRs, 77 Feature-NFRs unchanged. This is dev-workflow mechanics, not product scope. |
| UX | **None.** No screen, flow, interaction, accessibility, or FrontComposer behavior. |
| Architecture | Version bump and binding-sequence text only. No ownership, runtime, projection, or topology decision changes. |
| Epics overlay | Append-only v4 amendment adding Story 6.8 and the amended order. |
| Epic 6 context | Regenerated at v4 with the new story and sequence. |
| Conformance tests | Constants, amendment rows, and order strings updated; one new validation test added. |
| Dev workflows | Four surfaces gain a generation step (see §4.3). |
| `sprint-status.yaml` | New story entry; the epic-3 action item flips to `done` only when 6.8 reaches `done`. |
| `tests/README.md` | The Final-Record Completion Gate section is rewritten around generation. |
| Runbooks | New generator runbook, mirroring `docs/runbooks/submodule-promotion-completion-gate.md`. |

### Technical impact

No production source, contract, projection, topology, package, or signed evidence
changes. The change is confined to `_bmad/scripts/`, `_bmad/scripts/tests/`,
`.claude/skills/`, conformance tests, planning artifacts, and documentation.

**Honest limitation, stated rather than absorbed.** Nothing executes these gates
automatically. There is no `.github/workflows` outside `references/`, no git hook
invokes the checkers, and the pytest suite is manual-only and requires `uv`.
`deferred-work.md` already records this for both the planning gate (Story 6.1)
and the promotion gate (Story 6.7). Story 6.8 inherits the same limitation and
must not claim otherwise. What it can do is make the invocation non-deletable in
the same way Story 6.7 did — a conformance test asserting every gated workflow
body still carries the call. Wiring all three gates into CI remains a single open
deferred item, not part of this story.

## 3. Recommended Approach

**Direct Adjustment.** Add one story to the active corrective epic and publish a
v4 append amendment. No rollback, no MVP reduction.

Rollback is not warranted: nothing needs to be undone, and the existing
PowerShell asset is reusable input rather than waste. MVP review is not
warranted: the initiative's product scope is untouched, and the epic's own gates
already depend on trustworthy records — Story 6.6 cannot issue a superseding
attestation over records that were typed by hand.

The generator is implemented in **Python**, alongside `verify_submodule_promotion.py`,
so both halves of the completion gate share one runtime, one JSON contract shape,
one stable-code vocabulary, and one pytest suite — and so the generator can invoke
the promotion checker directly instead of asking an agent to transcribe its
output. The Epic-5 PowerShell asset is retained unchanged as history and as the
proven reference implementation for TRX parsing, frozen-state hashing, and the
contract-shape comparison; its logic is ported, not re-invented.

- **Effort:** Moderate. Low for the authority chain (mechanical, precedent
  exists); moderate for the generator, its tests, and the four workflow edits.
- **Risk:** Low. Additive, no production code, and every guard is fault-injected
  before the story closes.
- **Timeline:** Sits between 6.2 and the 6.3/6.4/6.5 group. It does not extend the
  epic's critical path, because those three stories cannot close correctly
  without it anyway.

## 4. Detailed Change Proposals

### 4.1 Authority chain

#### 4.1.1 `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`

Append a new amendment block after the v3 block's `:END` marker at line 977.
Do not modify the v1, v2, or v3 blocks.

```
NEW (appended):

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN version=epic-6-authority-2026-07-28-v4 supersedes=epic-6-authority-2026-07-27-v3 -->

## Appendix: 2026-07-28 Mechanical Final-Record Authority Amendment

**Overlay version:** `epic-6-authority-2026-07-28-v4`
**Architecture authority:** `conversations-architecture-2026-07-28-v4`
**Supersedes:** `epic-6-authority-2026-07-27-v3` only by adding Story 6.8 and
amending the binding dependency order
**Status:** active corrective amendment; the v1/v2/v3 overlays, completed
history, and signed evidence remain immutable historical records.

### Added Story

**6.8 Generate the final story record mechanically from measured state.**
[acceptance criteria as in §4.2]

### Superseding Story Dispositions

| Story | v4 disposition |
| --- | --- |
| 6.1 | No change. |
| 6.2 | No change. Closes under the pre-6.8 process; retro-verified read-only. |
| 6.3 | No semantic change; its completion record is generated, not authored. |
| 6.4 | No semantic change; its completion record is generated, not authored. |
| 6.5 | No semantic change; its completion record is generated, not authored. SM-2 evidence remains story-owned. |
| 6.6 | No semantic change; it consumes generated records and reruns the record gate. |
| 6.7 | No change; its checker becomes an embedded input to the record generator. |
| 6.8 | New. |

### Binding Dependency Order

`6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`

Stories 6.3 and 6.4 may still proceed after 6.1 where dependencies allow, but no
story after 6.2 may reach `done` without a generated final record. The SM-C2
baseline remains a pre-change gate for 6.2. This amendment introduces one new
story identifier and one sprint-status entry.

<!-- EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:END version=epic-6-authority-2026-07-28-v4 -->
```

**Rationale:** The overlay is append-only by construction. Adding the story any
other way would either rewrite frozen authority or leave the story ungoverned.

#### 4.1.2 `_bmad-output/planning-artifacts/architecture.md`

```
Line 7
OLD: authorityVersion: 'conversations-architecture-2026-07-27-v3'
NEW: authorityVersion: 'conversations-architecture-2026-07-28-v4'

Lines 8-10 (supersededAuthorityVersions) — prepend:
NEW:   - 'conversations-architecture-2026-07-27-v3'

Line 166
OLD: **Overall Status: READY FOR CORRECTIVE IMPLEMENTATION ONLY.** Story order is
     `6.1 -> 6.7 -> 6.2`; the frozen SM-C2 baseline is also required before 6.2
     completes. Story 6.2 precedes 6.5, and Story 6.6 is last.
NEW: **Overall Status: READY FOR CORRECTIVE IMPLEMENTATION ONLY.** Story order is
     `6.1 -> 6.7 -> 6.2 -> 6.8`; the frozen SM-C2 baseline is also required before
     6.2 completes. Story 6.2 precedes 6.8, Story 6.8 precedes 6.5, and Story 6.6
     is last. No story after 6.2 reaches `done` without a mechanically generated
     final record.

New subsection after the 2026-07-27 Test-Harness Ownership Amendment (line ~56):
NEW: ### 2026-07-28 Mechanical Final-Record Amendment
     Architecture version `conversations-architecture-2026-07-28-v4` supersedes
     v3 only by adding the mechanical final-record obligation and amending the
     binding story order. Completion records for Epic 6 stories after 6.2 are
     derived from parsed test-result artifacts, git-derived path and gitlink
     state, and the Story 6.7 promotion-checker document — never authored as
     prose. Every ownership, runtime, projection, topology, and evidence decision
     in v3 remains in force.
```

**Rationale:** The conformance test asserts semantic alignment between overlay
version and architecture version; drift between them is itself a conformance
failure per `epic-6-context.md:14`.

#### 4.1.3 `_bmad-output/implementation-artifacts/epic-6-context.md`

Regenerate at v4: frontmatter `overlay_version` / `architecture_version` /
`supersedes_overlay_version`; add a `### 6.8` entry to the Stories section; update
Binding Sequence from `6.1 -> 6.7 -> 6.2` to `6.1 -> 6.7 -> 6.2 -> 6.8` with the
new blocking note; add a "Final Record Invariant" section stating that counts,
paths, submodule state, and gitlink state in a completion record are derived
outputs and that a hand-edited record is a conformance failure.

#### 4.1.4 `tests/…/ArchitecturePlanningAuthorityValidationTest.cs`

**Correction to this proposal, found during publication.** The estimate below —
"constants, amendment rows, and order strings" — understated the work. The test
asserts `CountOccurrences(epics, "EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN") == 1`
and that the appended region *ends* with that block's `:END` marker, so a second
block using the same marker family would have broken the append-only assertions
rather than extending them. The v4 amendment therefore uses a distinct marker
family, `EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4`, and the test now asserts the
**chain** — v2 overlay closes, v3 amendment opens and closes, v4 amendment opens
and the document ends with it — so a later amendment cannot be spliced inside or
in place of an immutable earlier block. The version constants also had to be
split three ways (`BaseOverlayVersion` v2, `PreviousOverlayVersion` v3,
`OverlayVersion` v4, plus `PreviousArchitectureVersion`), because a single
"previous" constant was serving two different blocks.

```
Lines 28-31
OLD: ArchitectureVersion  = "conversations-architecture-2026-07-27-v3"
     OverlayVersion       = "epic-6-authority-2026-07-27-v3"
     PreviousOverlayVersion = "epic-6-authority-2026-07-15-v2"
NEW: ArchitectureVersion  = "conversations-architecture-2026-07-28-v4"
     OverlayVersion       = "epic-6-authority-2026-07-28-v4"
     PreviousOverlayVersion = "epic-6-authority-2026-07-27-v3"

Line 519
OLD: amendmentRows.Select(GetFirstTableCell).ShouldBe(["6.1","6.2","6.3","6.4","6.5","6.6","6.7"]);
NEW: amendmentRows.Select(GetFirstTableCell).ShouldBe(["6.1","6.2","6.3","6.4","6.5","6.6","6.7","6.8"]);

Lines 365, 597, 601, 647 — order strings
OLD: "6.1 -> 6.7 -> 6.2"  /  "6.1 authority correction -> 6.7 -> 6.2 -> 6.5 -> 6.6"
NEW: "6.1 -> 6.7 -> 6.2 -> 6.8"  /  "6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6"
     plus a negative assertion that the v3 order does not survive as the active order.
```

**Rationale:** These constants are the mechanism that makes planning authority
non-advisory. Leaving them stale would turn a green suite into proof of the wrong
thing.

### 4.2 New Story 6.8

**Key:** `6-8-generate-the-final-story-record-mechanically-from-measured-state`
**Title:** Generate the final story record mechanically from measured state

**AC1 — One generator, one source of truth.**
`_bmad/scripts/generate_story_record.py` emits a versioned document
(`schema: story-final-record-v1`) with `--repository`, `--story`, `--baseline`,
`--candidate`, `--test-results`, `--submodule`, `--require-remote`, and
`--format json|markdown`. Every field is derived from exactly four sources:
parsed test-result artifacts; `git diff --name-status <baseline>..<candidate>`
plus `git status` and `ls-files --others --exclude-standard`; mode-`160000`
entries resolved through `git ls-tree` / `git diff --raw`; and the
`verify_submodule_promotion.py` JSON document, embedded verbatim. No count,
path, or commit may be passed in as caller-supplied text.

**AC2 — Counts come only from machine-readable artifacts.**
Per-project and total `total/passed/failed/skipped` are parsed from TRX
`/TestRun/ResultSummary/Counters` (xUnit v3 `-trx <file>`; `dotnet test
--report-trx` where VSTest is usable). A declared project with no artifact is
recorded `NOT_RUN` and blocks — never silently omitted and never carried forward
from an earlier pass. Totals are computed, never transcribed. An artifact older
than the newest file in the derived File List blocks as `TEST_RESULTS_STALE`,
which is precisely the 2026-07-27→07-28 carry-forward that produced the
`622` / `631` contradiction.

**AC3 — The File List is derived, singular, and boundary-correct.**
Exactly one File List per record, computed from the baseline→candidate diff
unioned with the tracked working-tree delta. A path inside a root-declared
submodule blocks as `SUBMODULE_INTERNAL_PATH`; those files belong to that
repository's own record. Gitlink paths are emitted in a separate, labeled
promotions section carrying recorded commit and mode. A second, hand-appended
list is a conformance failure.

**AC4 — Submodule and gitlink state is bound to the candidate that is actually
final.** The record embeds the checker document and adds a re-derivation binding:
the candidate must be an ancestor of `HEAD`, and no declared gitlink may have
moved after it. Violations block as `CANDIDATE_NOT_FINAL`, so a superseded
binding goes red rather than stale — the Story 6.2 T1 behavior, generalized from
one story's evidence file to every record.

**AC5 — The four surfaces generate; none of them types.**
`bmad-dev-story` step 9, `bmad-quick-dev` `step-05-present.md` and
`step-oneshot.md`, and `bmad-code-review` `step-04-present.md` each invoke the
generator after their final validation and insert its rendered output verbatim
into the story/spec record and the sprint-status comment. Blockers block
`review` and `done` exactly as the promotion gate does.

**AC6 — Anti-vacuity and non-deletability.**
The generator refuses to emit `pass` when nothing was derived — no artifact
parsed, no candidate resolved, or no record section replaced ⇒ `RECORD_NOT_DERIVED`.
A conformance test asserts all four workflow bodies still contain the
invocation, mirroring the Story 6.7 five-gate-body check that caught a gate body
being replaced with "the gate is optional".

**AC7 — Historical mode.**
`--historical` verifies already-closed records read-only, proving committed
counts, paths, and gitlinks are self-consistent without mutating them. Stories
6.1, 6.2, and 6.7 are verified this way. It must not claim to reconstruct a
former uncommitted working tree — the honest boundary the Epic 5 asset already
states.

**AC8 — Every guard proven able to fail.**
One fault injection per guard, each restoring the mutated artifact
byte-identically: alter a parsed count; add a submodule-internal path; repoint
the candidate; drop a declared gitlink; delete a result artifact; backdate an
artifact. Recorded in the story as a table, per the 6.2/6.7 precedent.

**Stable blocker codes:** `TEST_RESULTS_MISSING`, `TEST_RESULTS_STALE`,
`TEST_COUNT_INCONSISTENT`, `FILE_LIST_DRIFT`, `SUBMODULE_INTERNAL_PATH`,
`CANDIDATE_NOT_FINAL`, `PROMOTION_GATE_NOT_PASS`, `BASELINE_NOT_TRUSTWORTHY`,
`RECORD_NOT_DERIVED`.
**Warning codes:** `UNRELATED_WORKTREE_DIRT`, `TEST_PROJECT_UNDECLARED`.

**Explicit non-goals:** does not wire any gate into CI (open deferred item);
does not modify production source, contracts, or signed evidence; does not
rewrite closed records; does not initialize, update, fetch, or traverse
submodules.

### 4.3 Workflow surfaces

#### `.claude/skills/bmad-dev-story/SKILL.md` step 9

```
OLD (line 428):
  <action>Update the story Status to: "review"</action>

NEW (inserted before it):
  <action>Run `python3 {project-root}/_bmad/scripts/generate_story_record.py
    --repository {project-root} --story {{story_file}} --baseline {{baseline_commit}}
    --candidate HEAD --test-results {{test_results_dir}} --format json`, passing the
    same `--submodule` / `--require-remote` declarations used by the promotion gate.
    Parse the JSON result.</action>
  <check if="result is not pass or the generator exits nonzero">
    <action>Record every stable blocker code in Dev Agent Record → Debug Log References.</action>
    <action>Set story frontmatter status and Status section to `in-progress`; if sprint
      tracking exists, set development_status[{{story_key}}] to `in-progress`.</action>
    <action>HALT: "Final story record generation failed; correct the measured state or the
      declaration — never the record"</action>
  </check>
  <action>Replace the story's File List, the count-bearing entries in Dev Agent Record →
    Completion Notes, and the Change Log row with the generator's rendered output verbatim.
    Do not retype any count, path, or commit. Narrative prose may surround the generated
    block; it may not restate its numbers.</action>
```

Also amend the definition-of-done list (lines 431-443):

```
OLD: - File List includes every new/modified/deleted file (relative paths)
NEW: - File List is the generator's derived output, inserted unedited
     - Every count in the record traces to a parsed test-result artifact
```

#### `.claude/skills/bmad-quick-dev/step-05-present.md` and `step-oneshot.md`

Insert a `### Final Record Generation` section between `### Promotion Completion
Gate` and `### Mark Spec Done and Synchronize`, with the same invocation, the
same blocker handling (return to `in-progress`, never write `done`, never
synchronize `review`), and the same verbatim-insertion rule. Both files change
identically — they are the two entry points of one route.

#### `.claude/skills/bmad-code-review/steps/step-04-present.md`

Add to section 6 (Sprint Status Update): after patches are applied, the review
pass regenerates the record rather than writing a fresh set of counts. This is
the surface that produced the `Server 631` / `Conformance 417/418` figures that
contradicted the record written the day before.

### 4.4 New assets and documentation

| Path | Purpose |
| --- | --- |
| `_bmad/scripts/generate_story_record.py` | The generator. |
| `_bmad/scripts/tests/test_generate_story_record.py` | pytest suite, including the AC8 fault injections. |
| `docs/runbooks/story-final-record-generation.md` | Operator runbook, mirroring the promotion-gate runbook. |
| `tests/…/StoryFinalRecordGenerationValidationTest.cs` | AC6 non-deletability guard over the four workflow bodies. |
| `tests/README.md` §Final-Record Completion Gate | Rewritten around generation; documents `-trx <file>` for the executable fallback and retains the PowerShell checker as the Epic 5 historical asset. |

### 4.5 `sprint-status.yaml`

```
OLD:   6-7-mechanically-block-incomplete-submodule-promotions-from-completion: done
       epic-6-retrospective: optional
NEW:   6-7-mechanically-block-incomplete-submodule-promotions-from-completion: done
       6-8-generate-the-final-story-record-mechanically-from-measured-state: backlog
       epic-6-retrospective: optional
```

The epic-3 action item stays `in-progress` and flips to `done` only when Story
6.8 itself reaches `done` — the same discipline applied to action item A2 during
the Story 6.7 review, where a premature flip was reverted.

## 5. Implementation Handoff

### Scope classification

**Moderate.** Binding authority and Epic 6 language change and one story is
added, but no product, UX, contract, or cross-repository baseline change is
required.

### Responsibilities

- **Architect / planning owner** — publish the v4 amendment, bump architecture to
  v4, regenerate `epic-6-context.md`, update the four conformance assertions.
- **Product owner** — add the 6-8 sprint-status entry; keep the epic-3 action item
  `in-progress` until 6.8 is `done`.
- **Conversations developer** — implement the generator, its pytest suite, the
  non-deletability conformance test, the runbook, and the four workflow edits;
  run the AC8 fault injections.
- **Test / release owner** — confirm every remaining Epic 6 story closes through a
  generated record, and that Story 6.6 consumes those records rather than prose.

### Sequence

1. Record this proposal and obtain approval.
2. Publish the v4 authority amendment and regenerate the Epic 6 context.
3. Update the conformance assertions; confirm the suite is green at v4.
4. `create-story` for 6.8, then drive it through `dev-story`.
5. Retro-verify 6.1, 6.2, and 6.7 in historical mode.
6. Close 6.3, 6.4, 6.5, and 6.6 through the generator only.
7. Flip the epic-3 action item to `done` when 6.8 is `done`.

### Success criteria

- No count, path, or commit in any post-6.2 completion record is typed by hand.
- A record whose counts, file list, or gitlink binding no longer match measured
  state goes **red**, not stale.
- Submodule-internal paths cannot appear in a story File List.
- The generator cannot emit `pass` having derived nothing.
- The invocation cannot be silently removed from any of the four workflows.
- Stories 6.1, 6.2, and 6.7 pass historical verification without being rewritten.
- The CI-wiring limitation is stated in the story and left as the existing open
  deferred item rather than claimed as resolved.

## 6. Approval And Publication Record

Approved by Jerome on 2026-07-28, with the v4 authority chain published in the
same session rather than deferred to the handoff.

### Applied in this session

| Artifact | Change |
| --- | --- |
| `…/epics.md` | Appended the `EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4` block: decision, Story 6.8 with eight ACs and its prohibitions, the 6.1-6.8 disposition table, and the amended order. |
| `…/architecture.md` | `authorityVersion` → v4, v3 added to `supersededAuthorityVersions`, this proposal added to `correctionAuthority`, new `### 2026-07-28 Mechanical Final-Record Amendment` section, readiness order amended. |
| `…/epic-6-context.md` | Regenerated at v4: frontmatter versions, `### 6.8` story section, amended Binding Sequence, new Final Record Invariant section. |
| `…/ArchitecturePlanningAuthorityValidationTest.cs` | Version constants split three ways; v2→v3→v4 chain assertions; v4 disposition table 6.1-6.8; Story 6.8 content assertions; v4 order assertions; context and architecture v4 semantics; story loop extended to 6.8. |
| `…/sprint-status.yaml` | `6-8-…: backlog` added; correct-course provenance comment recorded. The epic-3 action item stays `in-progress`. |

### Verification

- **Append-only property re-proved by bytes**, not by inspection: frozen prefix
  `bd437b80…f0e8a8` at 55,536 bytes and the v2 overlay `8825a7a2…63baa` at 14,843
  bytes both recomputed identical after the append.
- **Planning-authority conformance: 17/17 green at v4.**
- **Full conformance: 418 total, 416 passed, 2 failed, 0 skipped.**

### The two failures, stated plainly

Both are in `ProjectionReadStorePopulationProofValidationTest` and are Story
6.2's known-open evidence state, not a consequence of this change:

1. `ProofSourceAndSignedV1BindingsShouldRemainByteIdentical` — changed source
   hashes require a new evidence generation. Already recorded in the
   2026-07-28 sprint-status entry as `417/418`.
2. `RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` — root
   gitlinks moved after the recorded candidate `c398ea2`.

Independence evidence: that test class reads `docs/release-evidence` and git
state and references none of the five files changed here; no commit was made
during this session; and `HEAD` advanced from `ff3ae49` to `44d680e` mid-session
through a concurrent session that moved the `Hexalith.EventStore` and
`Hexalith.Tenants` gitlinks again, with `references/Hexalith.Memories` drifting
in the working tree. Failure 2 is the Story 6.2 T1 guard behaving exactly as
designed — going red rather than stale when a declared gitlink moves after the
candidate its evidence is bound to. It is also the clearest possible argument
for Story 6.8: the same binding is asserted today by one story's bespoke
validator, and needs to be a property of every record.

The drifted `references/Hexalith.Memories` pointer was left untouched. It
belongs to another session's activity and to Story 6.2's promotion scope, and
restoring or capturing it here would be exactly the out-of-scope gitlink capture
the July 27 D1 decision corrected.
