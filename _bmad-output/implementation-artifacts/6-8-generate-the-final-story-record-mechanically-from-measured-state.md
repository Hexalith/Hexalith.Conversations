---
story_key: '6-8-generate-the-final-story-record-mechanically-from-measured-state'
epic: 6
story_id: '6.8'
created: '2026-07-28'
status: 'in-progress'
baseline_commit: 'bb5b777f9b8e6932b1bae93c14b7d456a0e3c5cd'
submodule_promotions:
  - path: 'references/Hexalith.EventStore'
    require_remote: true
  - path: 'references/Hexalith.Memories'
    require_remote: true
  - path: 'references/Hexalith.Tenants'
    require_remote: true
allowed_skipped_tests:
  - test: 'Hexalith.Conversations.AppHost.Tests.ConversationsAppHostRuntimeBoundaryTest.RetainedAppHostShouldRunEventStoreAndConversationsProductionBoundary'
    reason: 'Opt-in live AppHost boundary; requires the external service lane.'
# ^ `[]` was the exact transcription of the approved scope at story creation. The v4 authority
#   confines Story 6.8 to `_bmad/scripts/`, `_bmad/scripts/tests/`, the two skill trees, conformance
#   tests, planning artifacts, and documentation, and explicitly prohibits modifying sibling
#   submodule source. That prohibition still holds: this story changed no submodule source, and
#   committed nothing inside any submodule.
#
#   The `[]` premise was then falsified exactly as the original comment predicted it might be. A
#   CONCURRENT session committed `e74c09a` ("feat: close Epic 5 release-owner decision ledger entry")
#   during this story's implementation window, moving two root gitlinks that sit between the baseline
#   and every candidate this story can produce:
#     references/Hexalith.Memories  1868c8f9...8cca -> 115d30b5...2547
#     references/Hexalith.Tenants   f9e51c66...fae7 -> 8d64563c...3d37
#   Story 6.8 did not cause either move. Both are inherited affected scope.
#
#   APPROVED SCOPE EXPANSION (first, two paths): Jerome approved declaring Memories and Tenants with
#   require_remote: true on 2026-07-28, after the condition was reported and before this field was
#   edited, choosing the Story 6.2 D1 precedent (declare every changed gitlink) over leaving them as
#   non-blocking UNDECLARED_GITLINK_CHANGE warnings. Never expand this declaration silently, and
#   never commit inside a submodule to make the gate pass.
#
#   APPROVED SCOPE EXPANSION (second, one path): the same condition recurred. A concurrent
#   correct-course session committed `f954202` ("feat: add Conformance Oracle Tiering Change Proposal
#   and Decision Artifacts", authority v5 / Story 6.9) after this story's first completion candidate
#   `33d2cac`, moving a third root gitlink and moving Tenants a second time:
#     references/Hexalith.EventStore  5a1d277e...2487 -> 589da8b9...a56d6   (newly affected)
#     references/Hexalith.Tenants     8d64563c...3d37 -> 2e61f57b...6b11d   (moved again)
#   The Tenants move is *after* `33d2cac`, so that candidate became a superseded binding and AC4
#   `CANDIDATE_NOT_FINAL` forced re-anchoring to the committed head. Story 6.8 caused neither move.
#   Jerome approved declaring `references/Hexalith.EventStore` with require_remote: true on
#   2026-07-28, again after the condition was reported and before this field was edited.
#
#   All three declared paths were verified before the field was edited: initialized, worktree clean,
#   submodule HEAD equal to the recorded gitlink, captured at mode 160000, and each recorded commit
#   contained by its own `refs/remotes/origin/main` — so the stricter declaration is satisfiable
#   rather than aspirational. All three are INHERITED AFFECTED SCOPE, not this story's promotions.
authority:
  overlay: 'epic-6-authority-2026-08-01-v8'
  architecture: 'conversations-architecture-2026-08-01-v8'
  proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-implementation-readiness-authority-correction.md'
  frozen_criteria_sources:
    - 'epic-6-authority-2026-07-28-v4'
    - 'epic-6-authority-2026-07-31-v6'
  current_view: '_bmad-output/planning-artifacts/epic-6-current-execution-view-v1.md'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md'
---

# Story 6.8: Generate the final story record mechanically from measured state

Status: in-progress

> **Global hold:** Story 6.8 remains `in-progress` as a lifecycle fact, but its
> implementation is paused. It may resume only after comprehensive v8 authority
> validation passes and a separate independent implementation-readiness
> assessment returns `READY`.

## Story

As a developer closing an Epic 6 story,
I want the final story record — test counts, File List, submodule state, and root gitlink state — emitted by a generator that reads the repository's measured final state,
so that a completion record can never contain a number, path, or commit that nobody measured.

## Acceptance Criteria

The eight criteria below are **frozen authority**, quoted verbatim from
`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:1019-1042` (the v4
amendment block). Do not paraphrase them, do not renumber them, and do not soften them. Each is
followed by an **Operational contract** block whose source is the approved correction proposal
(`sprint-change-proposal-2026-07-28.md`), one authority level below the overlay — the proposal is
named in `architecture.md:20` `correctionAuthority`, so it is binding input, but it is *not* the
frozen text.

> **Quote re-sync, 2026-07-31 (v6).** Commit `1b7a06b` had rewritten four of these items **inside**
> the published v4 block, after v5 declared v4 immutable. The quotes here were then partly synced to
> the edited text and partly not, so this section half-matched its own authority. Per the release
> owner's 2026-07-31 decision the v4 bytes are restored and the four improvements are republished as
> `epic-6-authority-2026-07-31-v6` (`sprint-change-proposal-2026-07-31-sm-c2-threshold-and-v4-restoration.md`).
> AC1, AC2, AC4, and AC5 below therefore carry **two** quotes: v4 as published, then the v6 amendment
> that supersedes it for that item. The v6 text is what binds; the v4 text is kept so the change is
> readable rather than silent.

### AC1 — one generator, one source of truth

> 1. One generator emits a versioned final-record document whose every field is
>    derived from the four sources named above. No count, path, or commit may be
>    supplied as caller-authored text.

**The four sources** — v4 as published (`epics.md:1004-1009`), with the second source amended by
the v6 republication (`epics.md:1307-1311`):

> Derivation sources are exactly four: parsed machine-readable test-result
> artifacts; the git-derived path set between the work baseline and the committed
> candidate unioned with the tracked working-tree delta; mode-`160000` root
> gitlink entries resolved from the committed candidate; and the Story 6.7
> promotion-checker document embedded verbatim. A record that could not derive any
> of them reports a blocker rather than a pass.

> **Derivation sources.** The second source is the git-derived path set between
> the work baseline and the committed candidate, **with source-tree dirt
> blocked outside record outputs and declared TRX inputs** — replacing the
> union with the tracked working-tree delta. A record must describe a committed
> tree, not a tree plus whatever else was open at the time.

**Operational contract** (proposal `:361-369`): `_bmad/scripts/generate_story_record.py`, document
field `"schema": "story-final-record-v1"`, flags `--repository`, `--story`, `--baseline`,
`--candidate`, `--test-results`, `--submodule`, `--require-remote`, `--format
json|markdown|bundle`, `--verify-record-sha256`, plus `--historical` from AC7.

### AC2 — counts come only from machine-readable artifacts

> 2. Test counts come only from machine-readable result artifacts. A declared test
>    project with no artifact is recorded as not run and blocks; totals are
>    computed rather than transcribed; an artifact older than the newest file in
>    the derived file list blocks as stale rather than being carried forward.

Amended by the v6 republication (`epics.md:1312-1314`):

> **Invariant 2.** The root `.slnx` defines the required root-owned test
> projects; failures block, and skips require exact versioned
> identity-and-reason policy.

**Operational contract** (proposal `:372-379`): parse TRX `/TestRun/ResultSummary/Counters`. The
per-project state for a declared project with no artifact is `NOT_RUN`. Never silently omit a
project and never carry a count forward from an earlier pass. Required projects come from the root
`.slnx`; failures block, and skips require an exact versioned identity and reason.

### AC3 — the File List is derived, singular, and boundary-correct

> 3. The file list is derived, singular, and boundary-correct. A path inside a
>    root-declared submodule blocks: it belongs to that repository's own record.
>    Gitlink promotions appear in a separate labeled section with recorded commit
>    and mode.

**Operational contract** (proposal `:382-387`): exactly one File List per record. A second,
hand-appended list is a conformance failure.

### AC4 — gitlink state binds to the candidate that is actually final

> 4. Submodule and gitlink state binds to the candidate that is actually final.
>    The candidate must be an ancestor of the committed head with no declared
>    gitlink movement after it, so a superseded binding goes red rather than
>    stale.

Amended by the v6 republication (`epics.md:1315-1317`):

> **Invariant 4.** After the bound candidate, only record-output paths may
> change and no gitlink may move, so a superseded binding goes red rather than
> stale.

**Operational contract** (proposal `:389-394`): embed the Story 6.7 checker document and add the
re-derivation binding. This generalizes Story 6.2's bespoke
`RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` guard from one story's evidence
file to every record.

### AC5 — the completion surfaces generate; none of them types

> 5. The four completion surfaces generate rather than author, and generator
>    blockers block `review` and `done` exactly as the promotion gate does.

Amended by the v6 republication (`epics.md:1318-1321`):

> **Invariant 5.** The four completion surfaces generate one
> document-and-Markdown bundle rather than author it, verify the inserted
> Markdown digest, and let generator blockers block `review` and `done` exactly
> as the promotion gate does.

**Operational contract** (proposal `:396-401`): the four surfaces are `bmad-dev-story` step 9,
`bmad-quick-dev/step-05-present.md`, `bmad-quick-dev/step-oneshot.md`, and
`bmad-code-review/steps/step-04-present.md`. Each invokes the generator after its final validation
and inserts the bundled rendered output **verbatim** into the story/spec record, then verifies its
digest before changing state. Sprint status references the record without restating counts. See
**Dev Notes → The four workflow surfaces** — every edit lands in two skill trees, and
one of them silently weakens Story 6.7's guard unless a coupled file is updated in the same change.

### AC6 — anti-vacuity and non-deletability

> 6. The generator cannot report a pass having derived nothing, and the
>    invocation cannot be silently removed from a completion workflow.

**Operational contract** (proposal `:403-408`): no artifact parsed, no candidate resolved, or no
record section replaced and digest-verified ⇒ `RECORD_NOT_DERIVED`/`RECORD_CONTENT_DRIFT`. A conformance test asserts all four workflow bodies
still contain the invocation, mirroring the Story 6.7 five-gate-body check that caught a gate body
being replaced with "the gate is optional".

### AC7 — historical mode

> 7. A read-only historical mode verifies already-closed records without mutating
>    them, and does not claim to reconstruct a former uncommitted working tree.

**Operational contract** (proposal `:410-416`): Stories 6.1, 6.2, and 6.7 are verified this way. See
**Dev Notes → Open decisions → D4** — 6.2's record legitimately violates AC3, and the v4 disposition
table authorizes it to stay that way. Historical mode must report that without blocking and without
rewriting it.

### AC8 — every guard proven able to fail

> 8. Every guard is fault-injected and proven able to fail, with each mutated
>    artifact restored byte-identically.

**Operational contract** (proposal `:417-421`): six mandated mutations — alter a parsed count; add a
submodule-internal path; repoint the candidate; drop a declared gitlink; delete a result artifact;
backdate an artifact. Record them in the story as a table (format in **Dev Notes → Fault-injection
table format**).

### Prohibitions

Verbatim from `epics.md:1043-1048`. Note this list is **broader** than the one in
`epic-6-context.md:140`, which drops three categories. This is the binding text:

> **Prohibitions.** Story 6.8 does not modify production source, public contracts,
> package versions, generated output, accepted baselines, signed evidence, or
> sibling submodule source. It does not rewrite closed story records. It does not
> initialize, update, fetch, or traverse submodules. It does not claim to have
> wired any gate into continuous integration; automatic execution of the planning,
> promotion, and final-record gates remains a single recorded deferred item.

## Tasks / Subtasks

- [x] **T1 — Generator skeleton and document contract** (AC: 1, 6)
  - [x] Create `_bmad/scripts/generate_story_record.py`, mode 755, PEP-723 header
        `requires-python = ">=3.11"`, stdlib only, module docstring one line.
  - [x] Port the sibling's structural spine from `verify_submodule_promotion.py`: `SCHEMA`
        constant, `GateError`, `GateArgumentParser`, `diagnostic()`, `empty_document()`,
        `run_git()` with the `-c` hardening set and `git_environment()` var-popping, `verify()`
        returning a dict, `main(argv) -> int`, `write_output()`. Do not invent a new shape.
  - [x] Implement the exit-code contract: `0` pass, `1` blocked, `2` error. A parseable document is
        written to stdout on **every** path including exit 2; never print a traceback.
  - [x] Implement `RECORD_NOT_DERIVED`: the document may not report `pass` when no artifact was
        parsed, no candidate resolved, or no record section replaced.
  - [x] Add the `--format markdown` renderer path (replaces the sibling's `text`), including the
        `pre_parse_output_format()` trick so a `GateError` raised before `parse_args` still honors
        the requested format.
- [x] **T2 — Test-count derivation from TRX** (AC: 2)
  - [x] Parse `/TestRun/ResultSummary/Counters` with namespace-agnostic matching (see **Dev Notes →
        TRX parsing** — a plain `/TestRun/...` path matches nothing).
  - [x] Map `total`, `executed`, `passed`, `failed`, and `notExecuted` → `skipped`.
  - [x] Compute totals by summation across declared projects. Never accept a caller-supplied total.
  - [x] A declared project with no artifact ⇒ per-project state `NOT_RUN` + blocker
        `TEST_RESULTS_MISSING`. An undeclared project with an artifact ⇒ warning
        `TEST_PROJECT_UNDECLARED`.
  - [x] Staleness: block `TEST_RESULTS_STALE` when an artifact is older than the newest file in the
        derived File List, **excluding the generator's own write targets** (see D3).
  - [x] Record each artifact's SHA-256 in the document, per the PowerShell precedent.
- [x] **T3 — File List derivation** (AC: 3)
  - [x] Derive from `git diff --name-status --no-renames <baseline>..<candidate> --`; block tracked
        or untracked source-tree dirt outside record outputs and declared TRX artifacts.
  - [x] Emit exactly one File List. Self-account for the generator's own output paths.
  - [x] Block `SUBMODULE_INTERNAL_PATH` for any path under a root-declared submodule prefix.
  - [x] Emit gitlink entries in a separate labeled promotions section carrying recorded commit and
        mode — never inline in the File List.
  - [x] Block `FILE_LIST_DRIFT` when the record's existing list disagrees with the derived set.
- [x] **T4 — Candidate and gitlink binding** (AC: 1, 4)
  - [x] Resolve mode-`160000` entries by **parsing the mode column** of `git diff --raw --no-abbrev
        -z` and `git ls-tree -z`. Never substring-match `160000`.
  - [x] Require the candidate to be an ancestor of `HEAD` and no declared gitlink to have moved
        after it ⇒ `CANDIDATE_NOT_FINAL`.
  - [x] Invoke `verify_submodule_promotion.py` and embed its document verbatim under a dedicated
        key. Branch on its `result` field (`pass`/`blocked`/`error`) **first**, not on
        `blockers[].code` and not on the return code alone — exit 1 and 2 both emit valid JSON.
        Map a non-`pass` result to `PROMOTION_GATE_NOT_PASS`.
  - [x] Block `BASELINE_NOT_TRUSTWORTHY` on a missing, unresolvable, or non-ancestor baseline.
- [x] **T5 — Markdown renderer** (AC: 1, 5)
  - [x] Render the JSON document to the exact block the four surfaces paste in: File List,
        promotions section, per-project counts table, totals, embedded gate summary, candidate
        binding.
  - [x] The renderer must **name what it derived**. Story 6.7 review finding: a vacuous zero-scope
        PASS was byte-identical to a fully verified promotion, and text was the default format.
  - [x] State in the rendered output that the JSON is authoritative and the Markdown is rendered
        from it.
- [x] **T6 — Historical mode** (AC: 7)
  - [x] `--historical` verifies a closed record read-only. No writes of any kind.
  - [x] Classify per D4: a record carrying no `story-final-record-v1` block is `pre-generator`; its
        AC3/AC2-shaped findings are reported as warnings, not blockers.
  - [x] Carry the honest boundary verbatim from the Epic 5 asset: committed bytes, path modes, and
        cross-record claims are verified; a former uncommitted working tree is not reconstructed or
        claimed.
  - [x] Run it over Stories 6.1, 6.2, and 6.7 and record the results.
- [x] **T7 — pytest suite and fault injection** (AC: 8)
  - [x] Create `_bmad/scripts/tests/test_generate_story_record.py` following the house convention:
        PEP-723 header with `dependencies = ["pytest>=8.0"]`, `sys.exit(pytest.main([__file__,
        "-q"]))` guard at the bottom.
  - [x] Reuse the hermetic fixture pattern from `test_verify_submodule_promotion.py`: `GIT_ENV`
        with the seven `GIT_*` redirect vars popped, deterministic author/committer,
        `GIT_CONFIG_GLOBAL=os.devnull`, `-c init.defaultBranch=main -c commit.gpgsign=false -c
        protocol.file.allow=always`, everything under `tmp_path`.
  - [x] Run the six AC8 mutations. Each must trip a distinct guard, and each mutated artifact must
        be restored byte-identically (verify with a hash, not by inspection).
  - [x] Add the decoy tests the sibling proved necessary: a filename containing the literal digits
        `160000`, and a filename containing a backslash.
  - [x] Record results in the fault-injection table (format in Dev Notes).
- [x] **T8 — The four workflow surfaces** (AC: 5)
  - [x] Edit `bmad-dev-story/SKILL.md` step 9: insert the generator invocation before line 428
        (`<action>Update the story Status to: "review"</action>`), mirroring the promotion gate's
        block at lines 416-427 — same blocker handling, same status rollback to `in-progress`, same
        HALT shape, same XML escaping (`&lt;value&gt;`).
  - [x] Amend the definition-of-done list (lines 430-443): replace the File List bullet with the
        generator-derived wording and add the count-traceability bullet.
  - [x] Edit `bmad-quick-dev/step-05-present.md` and `step-oneshot.md`. **These two files are not
        identical in this region** — step-05 writes to `{spec_file}` and is followed by `### Mark
        Spec Done and Synchronize`; oneshot writes to "the trace" and is followed by `### Complete
        Trace and Commit Completion Record`. Write two blocks, not one copy-paste.
  - [x] Edit `bmad-code-review/steps/step-04-present.md` section `### 6. Update story status and
        sync sprint tracking`. The generator runs after patches are applied and before the `done`
        branch at line 99.
  - [x] **Apply every edit to both `.claude/skills/` and `.agents/skills/`.** They are byte-identical
        and a test enforces it. Four surfaces = eight files.
  - [x] Read `baseline_commit` **or** `baseline_revision` in every invocation — `bmad-dev-auto` uses
        the second name. Do not rename existing keys.
- [x] **T9 — Non-deletability guard and the 6.7 coupling** (AC: 6)
  - [x] **Update `WORKFLOW_GATE_CONTRACTS` in `_bmad/scripts/tests/test_verify_submodule_promotion.py`
        in the same change as T8.** Inserting a section between the promotion gate heading and its
        follower marker *widens* Story 6.7's gate span to swallow the new section, which silently
        weakens its displacement guard. Add the new heading as the follower marker for each affected
        entry. This is not optional and it is not a 6.7 change — it is repairing a coupling this
        story breaks.
  - [x] Create `tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs`
        (see D1), porting the proven four-test shape: contract table → span-bounded body extraction →
        positive contract assertion → three mutation assertions (heading removed / body gutted /
        clause displaced outside the span).
  - [x] Assert `.claude/skills/` ↔ `.agents/skills/` byte identity for **every** skill path in this
        story's File List, not just the four gated ones.
- [x] **T10 — Runbook and test documentation** (AC: 5, 7)
  - [x] Create `docs/runbooks/story-final-record-generation.md`, mirroring
        `docs/runbooks/submodule-promotion-completion-gate.md`: versioned frontmatter, numbered
        sections, a blocker→remediation table matching the code table verbatim, an explicit
        exit-code list, a Safety boundary section, and a Known limitations section.
  - [x] Rewrite `tests/README.md` §Final-Record Completion Gate around generation. Document `-trx
        <file>` on the xUnit v3 executable and state that `dotnet test --report-trx` is rejected as
        an unknown option. Retain the PowerShell checker as the Epic 5 historical asset — do not
        delete it and do not re-mark Epic 4 action A1.
  - [x] Add this story as a **third instance** of the standing "nothing executes these gates
        automatically" entry in `_bmad-output/implementation-artifacts/deferred-work.md`. Do not
        resolve it. (That ledger has no IDs and no status field — match the existing bullet shape;
        do not invent a `DW-n` reference.)
- [x] **T11 — Self-application** (AC: 1, 5, 6)
  - [x] Story 6.8 completes after Story 6.2, so its own completion record must be produced by the
        generator it builds. Run it against this story and paste the output verbatim. A hand-typed
        6.8 record would falsify the story's own AC5 on delivery.
  - [x] Set `file_list_commit` in this file's frontmatter at completion.
- [x] **T12 — Boundary confirmation and final validation**
  - [x] Full .NET suite with `-p:UseHexalithProjectReferences=true` (see Dev Notes → environment).
  - [x] `uv run --with pytest pytest _bmad/scripts/tests/ -q`.
  - [x] `diff -rq .agents/skills .claude/skills` — expect only `Only in .agents/skills: aspire`.
  - [x] `git diff --check` in **both** forms: bare working-tree and over the committed range.
  - [x] Write the `### Boundary Confirmation` section naming what this story changed and what it did
        not.

### Review Findings

Adversarial four-layer review (Blind Hunter, Edge Case Hunter, Verification Gap Reviewer, Acceptance
Auditor) against diff `e74c09a`..`51138ed`, path-restricted to the 17 files touched by this story's
three commits (`33d2cac`, `2d5417c`, `51138ed`). The concurrent commits `e74c09a` and `f954202` were
excluded from the diff as not this story's work. 48 raw findings merged to 39; 5 dismissed.

Every high-severity finding below was re-verified against the source before rating — not accepted
from the reviewing layer. The generator itself was re-run twice during triage.

- [ ] [Review][Decision] **The record is red at its own bound candidate; closure needs a third re-anchor, and the treadmill is structural** — Re-running the generator at the recorded `file_list_commit` (`f954202`) with HEAD at `cfdddbe` returns `blocked`: `CANDIDATE_NOT_FINAL` ×3 (all three declared gitlinks moved after the candidate) plus `PROMOTION_GATE_NOT_PASS` (`GITLINK_COMMIT_MISMATCH` ×3). `cfdddbe` is a concurrent session's commit, not this story's. AC4 is working exactly as designed, but the record is committed *after* the candidate it binds to, so with three declared gitlinks that other sessions move freely, every record is one concurrent commit away from red — and each re-anchor requires a full suite re-run, because this repo builds with `-p:UseHexalithProjectReferences=true` and a submodule re-checkout silently invalidates the counts. This is the third re-anchor for one story. Needs an owner call on the closure strategy and on whether three declared gitlinks is the right scope.
- [ ] [Review][Decision] **The gate never validates the record that actually ships** — All four surfaces run the generator with `--format json` to decide the gate, then *re-run* it with `--format markdown` to get the text to paste (`bmad-dev-story/SKILL.md:429,437`; same in `step-05-present.md`, `step-oneshot.md`, `bmad-code-review/steps/step-04-present.md:101,104`). There is no `--format both`. So the pass verdict is computed against the record as it existed *before* insertion, and the inserted artifact is never gated. This compounds with `generate_story_record.py:1132`, which suppresses `FILE_LIST_DRIFT` when the record has neither a generated block nor a declared list — so a first run against an empty File List passes, and `RECORD_NOT_DERIVED` only checks that a section was *located* (`:1044`, `anchor is not None`), never that it was *replaced*. A workflow that gates green and then skips the insertion is indistinguishable from a correct run. Design fork: re-gate after insertion, add `--format both` from one measurement, or let the generator write the block itself (which would break the "never mutates repository state" property).
- [ ] [Review][Decision] **No source of truth for which test projects must be declared — a record can pass having measured 1 of 8** — Found independently by three of four layers. `derive_test_results` iterates only caller-supplied `--test-results` declarations, and `derived.test_results` is `any(state == "PARSED")` — one artifact suffices (`:1159`). The undeclared-artifact scan is seeded only from the parent directories of *already-declared* artifacts (`:757`, `:800`), so with zero or partial declarations it cannot fire, and `TEST_PROJECT_UNDECLARED` is a warning that no surface promotes to a gate failure — unlike `SCOPE_NOT_EVALUATED` in the promotion gate one paragraph above it. There is no `test_projects` frontmatter field (contrast `submodule_promotions`) and no analogue of the promotion checker's repo-derived `UNDECLARED_GITLINK_CHANGE` cross-check. The gap is already being relied on incorrectly: the Story 6.9 comment added to `sprint-status.yaml` in this diff asserts "Declare the new project to the 6.8 generator AND the .slnx **or 6.8 AC2 records it not-run and blocks**" — it would not; an undeclared project is invisible. Needs a decision on the source of truth (`.slnx` enumeration? a declared frontmatter field?).
- [ ] [Review][Decision] **The story's own prose restates the generated numbers — which the frozen authority forbids — and two restatements are arithmetically wrong** — `epics.md:1000-1002` (frozen v4 overlay) states "Narrative prose may surround a generated record; it may not restate the generated numbers"; `epic-6-context.md:166` repeats it as the Final Record Invariant. This story's prose restates them repeatedly (lines 886, 892, 880-882) and the ~6 KB hand-authored `sprint-status.yaml` comment re-types all eight per-project counts. Two are wrong: line 1050 says "eighteen of the twenty-five" derived paths were authored by this story and line 1057 says "Seven of the twenty-five" by concurrent sessions. I partitioned the shipped 25-path File List against the three story commits: **17 own, 8 foreign**. The enumeration in the same paragraph sums to 17, and the story concedes the 8th at lines 1072-1074, contradicting itself. `18` appears to be the File List size at the superseded candidate `33d2cac`, carried forward into an authorship claim — which is precisely the defect class this story exists to remove. Needs a call on whether to correct the numbers or delete the restatements as the authority requires.
- [ ] [Review][Patch] Recorded counts are never compared against derived counts — `verify_live` reads only the File List back out of the record (`declared_file_list`, `:1104`); no count, total, SHA-256, gitlink commit, or candidate hash in the pasted block is ever compared to the derived document, so "every count here is derived" rests entirely on agent honesty [_bmad/scripts/generate_story_record.py:1104-1128]
- [ ] [Review][Patch] Duplicate `--test-results` declarations multiply the totals — no dedup on declarations or artifacts; one 2-test artifact declared under four names yields `total: 8`, `result: pass` [_bmad/scripts/generate_story_record.py:747,819-824]
- [ ] [Review][Patch] A zero-test TRX satisfies the anti-vacuity guard — `RECORD_NOT_DERIVED` keys on *parsed*, never on *measured*, so an artifact with all-zero counters (what `dotnet test --filter <no-match>` emits) passes with totals of zero [_bmad/scripts/generate_story_record.py:817-824,1159-1161]
- [ ] [Review][Patch] Historical mode claims test results were derived without parsing anything — `derived["test_results"] = generated`, a substring test on the record file; the rendered output emits the self-contradiction "Derived: test results **yes** … 0 test artifact(s) parsed" [_bmad/scripts/generate_story_record.py:1339]
- [ ] [Review][Patch] Historical classification is a whole-document substring search for the schema name — adding the literal `story-final-record-v1` to a prose heading flips a pre-generator record to `generated`; the same anti-pattern the file rejects for `160000` at `:367-371` [_bmad/scripts/generate_story_record.py:1217]
- [ ] [Review][Patch] Historical mode passes silently when the baseline is not an ancestor of `file_list_commit` — the `else` branch emits no finding, so a generated closed record with a fabricated File List returns `pass` with zero blockers and zero warnings; live mode blocks this exact condition at `:1081` [_bmad/scripts/generate_story_record.py:1330-1337]
- [ ] [Review][Patch] TRX outcome vocabulary is incomplete in three ways — `Error`/`Timeout`/`Aborted` are folded into `failed` but read from `<Counters>` as `failed=` only, producing an un-remediable `TEST_COUNT_INCONSISTENT`; `Inconclusive` is counted as skipped but compared against `notExecuted`; and outcomes outside the three tuples (`Warning`, `Blocked`, `Pending`, …) inflate `total` without landing in any bucket, with no `else` branch naming the unrecognized outcome [_bmad/scripts/generate_story_record.py:101-103,648,656-659]
- [ ] [Review][Patch] A dirty worktree at measurement time is never compared against the candidate — worktree status silently overlays the committed range, and `UNRELATED_WORKTREE_DIRT` fires only for paths *absent* from that range, so an uncommitted edit to an in-range file passes with 0 blockers and the counts describe a tree the record does not bind to [_bmad/scripts/generate_story_record.py:899,923]
- [ ] [Review][Patch] Gitlinks are kept out of the File List by `.gitmodules` membership, not by mode `160000` — a mode-160000 entry absent from `.gitmodules` is emitted as an ordinary File List entry with no `SUBMODULE_INTERNAL_PATH` protection, contradicting the runbook's mode-rigor claim; the mode evidence is already computed one function away [_bmad/scripts/generate_story_record.py:906-913]
- [ ] [Review][Patch] `submodule.<name>.ignore` and `GIT_CONFIG_*` are not neutralized — hardening sets `diff.ignoreSubmodules=none`, which git ranks *below* `submodule.<name>.ignore`, and `GIT_ENVIRONMENT_OVERRIDES` pops seven redirect vars but not `GIT_CONFIG_COUNT`/`KEY_n`/`VALUE_n`/`GLOBAL`/`SYSTEM`; with `ignore=all` a real gitlink move becomes invisible to every AC4 guard and the run returns `pass`. Adding `--ignore-submodules=none` to the raw-diff command line restores detection [_bmad/scripts/generate_story_record.py:38-55,372-380]
- [ ] [Review][Patch] The AC6 non-deletability test is a tautology — `gutted = content.replace("generate_story_record.py", …)` then asserting the string is absent tests `str.replace`, not the workflow file; it passes against a file that never invoked the generator, and only reads `.agents/skills` [_bmad/scripts/tests/test_generate_story_record.py:797-801]
- [ ] [Review][Patch] The C# gate asserts blocker *vocabulary*, not reachability — `ShouldContain(code)` is a substring search over the script source, and all nine codes are also keys of `BLOCKER_REMEDIATION`, so they survive deletion of every emission site; the comment directly above claims reachability is what is being proven [tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs:98-113]
- [ ] [Review][Patch] The runbook misstates the staleness exclusion set — it says the exclusion "covers those two paths and nothing else", but the code also excludes every declared TRX artifact from its own comparand; the `TEST_RESULTS_STALE` table row repeats the omission. The exclusion itself is correct and explained in Completion Notes; only the operator-facing text is wrong [docs/runbooks/story-final-record-generation.md:53-55,117 vs _bmad/scripts/generate_story_record.py:845-847]
- [ ] [Review][Patch] The quick-dev surfaces drop "never synchronize `review`" from the record gate — the promotion gate in the same file carries it; AC5 requires the record gate to block "exactly as the promotion gate does". The C# guard's clause list does not include the phrase, so it cannot detect the divergence [.claude/skills/bmad-quick-dev/step-05-present.md:74, step-oneshot.md:74]
- [ ] [Review][Patch] No surface carries the authority's "may not restate its numbers" clause — `grep -rn "restate"` returns zero hits across all eight shipped skill files; `bmad-dev-story/SKILL.md:439` instead says "Quote the generator's derived totals in the sprint-status comment", an instruction to transcribe rather than insert verbatim. The clause was in the block the story was told to insert and was dropped without disclosure in D1–D7 or the Change Log [.agents/skills/**, .claude/skills/**]
- [ ] [Review][Patch] `STORY_ANCHOR_END` is a heading the project's story template does not have — when `### Boundary Confirmation` is absent, `record_anchor` returns `### File List` → EOF, and `bmad-dev-story/SKILL.md:437` instructs replacing everything between them; on any normally-created story that deletes the `## Change Log` the same file's DoD demands at `:455` [_bmad/scripts/generate_story_record.py:32,562 vs .claude/skills/bmad-create-story/template.md:57]
- [ ] [Review][Patch] The commit that defines the derived range is scoped by the hand-authored File List the generator replaces — `SKILL.md:418` says "Stage only the paths in the story File List", so an omitted file is never committed, lands in `UNRELATED_WORKTREE_DIRT`, and the "derived" list reproduces the omission; the bullet that used to catch this ("File List includes every new/modified/deleted file") was deleted by this diff with nothing replacing its completeness requirement [.claude/skills/bmad-dev-story/SKILL.md:418,452]
- [ ] [Review][Patch] The documented three-input union contributes nothing once a baseline resolves — the union of committed status, worktree delta and untracked files is immediately filtered to paths the committed range already contains, so an untracked new file never reaches the File List; the union only overrides status letters. Both `tests/README.md` and the AC3 Completion Note state the union as the contract [_bmad/scripts/generate_story_record.py:895-899,923-936]
- [ ] [Review][Patch] Authority frontmatter names v4 but the bound candidate carries v5 — at `f954202`, `architecture.md:7` is `…-v5`, `epic-6-context.md:4` is `…-v5`, and `epics.md:1076` opens a V5 amendment block; Dev Notes → Authority precedence still asserts the conformance test carries the v4 string, which `f954202` changed. This story's ACs are unaffected (v5 appends), but a reviewer following the pointer reads a superseded version [story frontmatter:63-66, Dev Notes:343-347]
- [ ] [Review][Patch] Frontmatter parsing is brittle in three reachable ways — a quoted scalar with a trailing comment keeps its quotes and yields a false `BASELINE_NOT_TRUSTWORTHY` pointing at a field that is already correct; a UTF-8 BOM voids the entire frontmatter; and a non-UTF-8 byte escapes the dedicated `OSError` handler as `INTERNAL_ERROR` rather than `INVALID_SCOPE` (`verify_historical` reads with no handler at all) [_bmad/scripts/generate_story_record.py:531,542-546,1036-1039,1215]
- [ ] [Review][Patch] The runbook-coverage test cannot see any `GateError` code — the regex matches only literal `diagnostic("CODE"` calls, so all 16 `GateError` sites reach the document through `diagnostic(error.code, …)`, a variable. Those codes are documented today, but a new one would ship undocumented with the suite green [_bmad/scripts/tests/test_generate_story_record.py:844]
- [x] [Review][Defer] Non-declared submodules that move after the candidate never trip `CANDIDATE_NOT_FINAL` [_bmad/scripts/generate_story_record.py:1009-1020] — deferred, disclosed by the story as a residual limitation; `gitlinks_moved_after_candidate` is already in the document
- [x] [Review][Defer] The C# guard is hard-pinned to Story 6.8's own record path, and the three mutation tests run only against `AgentTree` [tests/…/StoryFinalRecordGenerationValidationTest.cs:28-29,138-176] — deferred, pre-existing shape of this test file
- [x] [Review][Defer] The pytest suite hard-binds to live repository history despite T7's "everything under `tmp_path`" claim [_bmad/scripts/tests/test_generate_story_record.py:682-718] — deferred, pre-existing pattern
- [x] [Review][Defer] `HEAD` is resolved independently at three points with no snapshot [_bmad/scripts/generate_story_record.py:505-515,982,1173] — deferred, broader concurrency issue the story already documents twice
- [x] [Review][Defer] `tests/README.md` retires the PowerShell manifest's immutability and contract-shape assertions with no equivalent in the replacement [tests/README.md] — deferred, needs an owner call on whether those checks are superseded or merely unenforced
- [x] [Review][Defer] File List annotations are not reproducible from `file_list_commit` — the status letter comes from transient worktree dirtiness, so the shipped block says `(modified)` where a re-run says `(new)` [_bmad/scripts/generate_story_record.py:898-899] — deferred, paths reproduce exactly; only the parenthetical drifts
- [x] [Review][Defer] The generated Gitlink Promotions table presents inherited affected scope as this story's promotions [render_promotions] — deferred, the disclaimer currently lives only in surrounding prose, which is the layer the authority says may not carry record semantics
- [x] [Review][Defer] The runbook's "never … otherwise mutates repository state — in either mode" is checked by one hash of one file in one mode [_bmad/scripts/tests/test_generate_story_record.py:696] — deferred, property holds by construction; no regression guard
- [x] [Review][Defer] Story 6.8's own record is excluded from the `CLOSED_RECORDS` re-derivation test it wrote for its siblings [_bmad/scripts/tests/test_generate_story_record.py:684-689] — deferred, cannot be added until the story closes

**Dismissed as noise (5):** a `.gitmodules` entry outside `references/` aborting the run, and a repo with no root `.gitmodules` never passing — both deliberate Hexalith conventions in a repo-local script; a root-level story record emitting a phantom `./sprint-status.yaml` — stories always live under `implementation-artifacts/`; backtick/newline paths breaking the Markdown round-trip — not reachable under this repo's path conventions; File List order/duplicates/annotations not compared — set comparison is the right contract, and the substantive half is the counts patch above.

### Review Findings — Chunk 1 (2026-07-29)

Four parallel adversarial layers reviewed the generator and its two Python conformance suites at
`7472632` against baseline `bb5b777`. Their 43 raw findings normalized to 22 current findings; none
were dismissed. The repaired focused suites are green (`62 + 85 = 147` tests).

- [x] [Review][Patch] **The gate does not validate the record that is ultimately shipped** — Resolve with a single measurement bundle containing JSON and Markdown, verbatim workflow insertion, and a post-insertion verification pass that compares the shipped block. `record_section` currently becomes derived when an anchor is merely found, an empty first-run File List suppresses drift, and no recorded count, artifact hash, candidate, or promotion value is parsed back and compared. [_bmad/scripts/generate_story_record.py:1041-1044,1130-1144]
- [x] [Review][Patch] **There is no authoritative scope for required test projects** — Resolve by deriving the required set from test projects declared in the root `.slnx`, excluding paths beneath root submodules, and requiring CLI artifact declarations to match that set exactly. Callers currently choose every label and artifact path; scan directories are learned only from those declarations, and one parsed artifact satisfies derivation, making omitted projects and foreign-but-valid TRX files invisible. [_bmad/scripts/generate_story_record.py:737-825,1156-1161]
- [x] [Review][Patch] **“Final candidate” permits arbitrary source commits after the candidate** — Resolve by allowing `candidate..HEAD` to change only the story record and its `sprint-status.yaml`; every other path and every gitlink movement blocks finality. The current check rejects post-candidate movement only for affected gitlinks, so ordinary source commits are accepted even though the record does not bind to them. [_bmad/scripts/generate_story_record.py:998-1021]
- [x] [Review][Patch] **The committed-candidate contract conflicts with the specified working-tree union** — Resolve with a clean-source-tree contract: outside the story and sprint-status outputs, every tracked or untracked change blocks, and the File List derives from committed history. Update the AC and operator documentation that currently promise a working-tree union. Worktree-only paths are presently discarded while dirty in-range paths silently overwrite committed status. [_bmad/scripts/generate_story_record.py:895-936]
- [x] [Review][Patch] **A red or skipped suite can produce a passing final-record gate** — Resolve by making every failed test a blocker and permitting skipped tests only when their test identities and reasons appear in explicit versioned policy. Failures and skips are currently rendered as warning prose but never affect the verdict. [_bmad/scripts/generate_story_record.py:789-798,1197,1463-1473]
- [x] [Review][Patch] Duplicate `--test-results` declarations are processed repeatedly and multiply project totals [_bmad/scripts/generate_story_record.py:744-747,817-824]
- [x] [Review][Patch] A parsed zero-test TRX satisfies `derived.test_results` and the anti-vacuity guard [_bmad/scripts/generate_story_record.py:817-824,1159-1161]
- [x] [Review][Patch] Valid TRX outcome counters are mapped incompletely, and unknown outcomes are not named or rejected explicitly [_bmad/scripts/generate_story_record.py:643-676]
- [x] [Review][Patch] TRX counts, hash, and timestamp come from three independent reads, allowing one artifact to yield internally mismatched evidence during concurrent replacement [_bmad/scripts/generate_story_record.py:769,784-786]
- [x] [Review][Patch] Integer-second mtime truncation lets a source edit later in the same second evade `TEST_RESULTS_STALE` [_bmad/scripts/generate_story_record.py:786,849-865]
- [x] [Review][Patch] Lexically safe story and TRX paths may resolve through symlinks outside the repository and import foreign evidence [_bmad/scripts/generate_story_record.py:748-769,1031-1039]
- [x] [Review][Patch] Historical classification is a whole-document schema substring check, so deleting or relocating one schema line can demote a generated record's findings to warnings [_bmad/scripts/generate_story_record.py:1217-1235]
- [x] [Review][Patch] Historical mode marks test results derived without parsing or structurally validating any recorded result [_bmad/scripts/generate_story_record.py:1339]
- [x] [Review][Patch] Historical mode reconstructs promotions from the File List instead of parsing the rendered Gitlink Promotions table, leaving recorded promotion claims unchecked [_bmad/scripts/generate_story_record.py:1270,1304-1315]
- [x] [Review][Patch] Historical mode silently returns an empty comparison when the baseline is not an ancestor of `file_list_commit` [_bmad/scripts/generate_story_record.py:1292-1337]
- [x] [Review][Patch] Removed or undeclared mode-`160000` entries are separated by candidate `.gitmodules` membership rather than structural Git mode and can leak into the ordinary File List [_bmad/scripts/generate_story_record.py:323-361,906-913]
- [x] [Review][Patch] Per-submodule ignore configuration and `GIT_CONFIG_*` injection can hide real gitlink movement from raw diffs [_bmad/scripts/generate_story_record.py:37-55,372-380]
- [x] [Review][Patch] Porcelain rename/copy parsing consumes the second path only when `R` or `C` is in the index-status column, misparsing worktree-column renames [_bmad/scripts/generate_story_record.py:483-504]
- [x] [Review][Patch] The AC6 non-deletability mutation proves only that `str.replace` removed a substring, so it remains green if a completion surface never invoked the generator [_bmad/scripts/tests/test_generate_story_record.py:797-810]
- [x] [Review][Patch] Runbook-code coverage scans only literal `diagnostic("CODE")` calls and cannot detect undocumented `GateError` codes [_bmad/scripts/tests/test_generate_story_record.py:846-855]
- [x] [Review][Patch] An unmatched generated-block begin marker falls through to broad heading replacement instead of failing closed [_bmad/scripts/generate_story_record.py:549-567]
- [x] [Review][Patch] Caller-controlled project labels and legal path punctuation are emitted into Markdown tables and code spans without escaping; JSON remains authoritative but the human record can be malformed [_bmad/scripts/generate_story_record.py:1370-1383,1432-1451]

**Chunk 1 action result.** All 22 selected patches were applied. Python validation: generator
`62/62`, promotion checker `85/85`; Ruff format/check and `git diff --check` pass. The focused C#
contract test could not build because the current Tenants checkout cannot resolve EventStore contract
types; both project-reference settings fail before reaching this test, and no submodule was changed.

Completion remains blocked. Promotion gate: `GITLINK_COMMIT_MISMATCH` for EventStore, Memories, and
Tenants, plus `UNCAPTURED_SUBMODULE_PROMOTION` for AI.Tools. Final-record gate: `TEST_RESULTS_FAILED`,
`TEST_RESULTS_STALE`, `WORKTREE_NOT_CLEAN`, `FILE_LIST_DRIFT`, and embedded
`PROMOTION_GATE_NOT_PASS`. Per the fail-closed workflow, story and sprint status return to
`in-progress`; no record is regenerated from stale/red evidence.

### Review Findings — Chunk 2 (2026-07-29)

Four parallel layers reviewed the 14 non-Python story-owned files at the current working tree against
baseline `bb5b777`: the eight mirrored completion-workflow surfaces, the story/sprint/deferred
records, operator documentation, the C# conformance guard, and test guidance. Their findings were
re-verified at the current call sites and normalized to 2 decisions and 9 patches; 7 additional
claims were dismissed as duplicates of the superseded-record condition, already-recorded deferrals,
or unreachable under the repository's line-ending policy.

- [x] [Review][Patch] **Remove the superseded authoritative `PASS` block until a green re-anchor replaces it** — Owner decision 2026-07-29: remove the obsolete generated block rather than preserve misleading finality or retain it unchanged. The retained block declares `PASS` while recording two failed tests and binding candidate `f954202`, although the story is now `in-progress`, the current head and all three recorded gitlinks have advanced, and the Chunk 1 gate re-run is blocked. [story final-record block and Chunk 1 action result]
- [x] [Review][Patch] **Add an explicit user-authorized commit checkpoint for post-review patches, then gate its immutable SHA** — Owner decision 2026-07-29: do not auto-commit and do not force every patched review into a separate dev-story pass. `step-04-present.md` must request commit authorization after patches are applied, commit only the approved scoped paths when authorized, capture that commit SHA once, and run both completion gates against it. [.agents/skills/bmad-code-review/steps/step-04-present.md:64-104]
- [x] [Review][Patch] Add an explicit rollback-and-HALT branch for failed post-insertion digest verification before the unconditional transition to `review` [.agents/skills/bmad-dev-story/SKILL.md:439-442]
- [x] [Review][Patch] Remove the no-VCS escape hatch that lets both quick-dev routes skip generation and write `done`; an unresolved candidate must stay `in-progress` with `GIT_UNAVAILABLE`/`RECORD_NOT_DERIVED` [.agents/skills/bmad-quick-dev/step-05-present.md:66-74; .agents/skills/bmad-quick-dev/step-oneshot.md:69-77]
- [x] [Review][Patch] Create a replaceable `## Verification` target before the first one-shot generator invocation; the one-shot template currently deletes every section the generator can replace, making the first completion deterministically fail `RECORD_NOT_DERIVED` [.agents/skills/bmad-quick-dev/step-oneshot.md:39-73]
- [x] [Review][Patch] Capture one immutable candidate SHA and pass it to both gates instead of re-resolving the moving `HEAD`, whose concurrent advancement can bind records to commits the measured tests did not exercise [.agents/skills/bmad-dev-story/SKILL.md:419-429; .agents/skills/bmad-code-review/steps/step-04-present.md:91-101]
- [x] [Review][Patch] Extend the workflow conformance contracts to prove structural marker order, insertion-before-digest-verification, and the success transition's dependency on `record_gate_failed`; unordered token presence inside a span does not reject a reordered or promotion-only completion path [tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs:34-217]
- [x] [Review][Patch] Replace the generator-source blocker `ShouldContain` loop with executable reachability evidence (or remove its reachability claim); remediation dictionaries, comments, and dead constants currently satisfy the test after every emission site is deleted [tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs:99-134]
- [x] [Review][Patch] Remove hand-restated test/path/promotion numbers from story narrative and `sprint-status.yaml`, leaving stable references to the generated record; the current prose already contains the wrong `18/7` authorship partition for the measured `17/8` split [_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md:995-1199; _bmad-output/implementation-artifacts/sprint-status.yaml:42]
- [x] [Review][Patch] Replace the runbook's obsolete JSON-then-Markdown checklist with one `--format bundle` measurement, verbatim insertion, and explicit `--verify-record-sha256`; also replace its instruction to quote totals with a no-restatement record reference [docs/runbooks/story-final-record-generation.md:244-255]
- [x] [Review][Patch] Make the dev-story commit scope come from task-established paths and halt on ambiguous concurrent dirt; Git delta alone carries no authorship and cannot safely distinguish story work from another session's changes [.agents/skills/bmad-dev-story/SKILL.md:418]


**Chunk 2 action result.** Every owner-approved patch is applied. The focused conformance build and
workflow class, Python generator and promotion suites, mirror parity, and path-scoped whitespace
validation pass. The story remains `in-progress`, with no generated final record and no
`file_list_commit`, until an explicitly authorized review-patch commit can be gated and re-anchored.

## Dev Notes

### Authority precedence — read this first

1. Finalized initiative PRD/addendum and approved correction proposals.
2. `epics.md:979-1074` — the frozen v4 overlay block. **The ACs above are this text.**
3. `_bmad-output/planning-artifacts/architecture.md` at `conversations-architecture-2026-07-28-v4`.
4. `epic-6-context.md` — derived. Semantic drift between it and the amendment is itself a
   conformance failure (`epic-6-context.md:14`).

**The v4 authority already landed** in commit `bb5b777`, including the conformance assertions:
`ArchitecturePlanningAuthorityValidationTest.cs` already carries `ArchitectureVersion =
"conversations-architecture-2026-07-28-v4"`, `OverlayVersion = "epic-6-authority-2026-07-28-v4"`,
the eight-row v4 disposition table, `### Story 6.8:` content assertions, the `story <= 8` loop, and
the order string `6.1 authority correction -> 6.7 -> 6.2 -> 6.8 -> 6.5 -> 6.6`.

**Verify the Conformance suite is green before you start.** Story 6.7 hit a five-failure HALT from
pre-existing planning-authority drift discovered mid-story and had to escalate for expanded scope.
Two known failures exist at this baseline, both in `ProjectionReadStorePopulationProofValidationTest`
and both belonging to Story 6.2's open evidence state, not to this story. Confirm that is still the
exact failure set before attributing anything to your own work.

### Non-negotiable constraints

1. Never initialize, update, fetch, or traverse submodules. Read root `.gitmodules` only. No
   recursive submodule commands, ever.
2. Never `git add -A` or `git commit -a`. Stage only the paths in the File List and the exact root
   gitlinks named by `submodule_promotions` (which is `[]` here).
3. Never commit inside a sibling submodule to make a gate pass. If untracked dirt appears inside a
   `references/` path from a concurrent session, **park the file outside the submodule
   byte-identically** — the Story 6.2 precedent, hash-verified before and after.
4. Do not edit signed evidence to satisfy a documentation requirement. Story 6.7 hit this: the
   required runbook edit landed on a hash-pinned signed-v1 file. It reverted the edit and created a
   **new** live runbook instead. Do the same.
5. Do not invent a blocker or warning code without adding it to the tables in the same change.
6. Do not flip the Epic 3 action item. It is `status: in-progress` and closes only when Story 6.8
   itself reaches `done`. Direct precedent: 6.7's dev agent flipped action A2 to `done` at
   self-verification and code review reverted it.
7. Preserve superseded Debug Log entries. Annotate them inline in bold parentheses; never delete
   them.

### Verified environment facts

| Fact | Value |
| --- | --- |
| System Python | 3.14.4, **no pytest on PATH** |
| pytest invocation | `uv run --with pytest pytest _bmad/scripts/tests/... -q` (uv provisions 3.11.15 + pytest 9.1.1) |
| Generator runtime | plain `python3`, stdlib only — matches how all five workflow surfaces invoke the sibling checker |
| .NET restore/build/test | **must** carry `-p:UseHexalithProjectReferences=true`; otherwise `NU1102` on unpublished EventStore `999.1.20-proof.*` packages. This blocked 6.7's review pass from running Conformance at all. |
| Test execution | xUnit v3 / Microsoft.Testing.Platform. **No `dotnet test --filter`.** Build, then run the executable with single-dash `-class` / `-method`. |
| TRX emission | `-trx <file>` on the built executable (plus `-noLogo`). `dotnet test --report-trx` is rejected as an unknown option. |
| VSTest | socket creation blocked in restricted sandboxes — the executable fallback is the approved path, documented at `tests/README.md` §VSTest Socket Fallback |
| Build flags for fallback | `-c Release --no-restore /nr:false /m:1`; output at `bin/Release/net10.0/` |
| Root submodules | 10 declared in `.gitmodules` |
| Declared test projects | 8 under `tests/` — `Hexalith.Conversations.Tests` is the "Domain" project in count breakdowns |
| `.gitattributes` | `_bmad-output/**/*.{md,json,yaml,yml}` pinned `eol=lf`. `_bmad/scripts/*.py` and the skill trees are **not** pinned — write `\n`. |

### Generator contract to mirror

Port the structural spine of `_bmad/scripts/verify_submodule_promotion.py` (860 lines). Do not
design a new one — the point is that both halves of the completion gate share one runtime, one
document shape, and one code vocabulary.

**Document shape** (sibling at `:750-764`):

```json
{
  "schema": "story-final-record-v1",
  "result": "pass" | "blocked" | "error",
  "repository": "...", "baseline": "...", "candidate": "...",
  "...": "derived sections",
  "blockers": [ { "code": "...", "path": "...|null", "message": "...", "remediation": "..." } ],
  "warnings": [ { "code": "...", "path": "...|null", "message": "..." } ]
}
```

- `diagnostic(code, message, path=None, remediation=None)` — key order `code`, `path`, `message`,
  then `remediation`, which is **omitted entirely** when `None`. Blockers carry it; warnings do not.
- `empty_document()` pre-seeds every top-level key so consumers never `KeyError` even on a total
  failure.
- Serialize as `json.dumps(document, indent=2, ensure_ascii=False) + "\n"`.
- `write_output()` reconfigures `sys.stdout` to `errors="backslashreplace"` **before** writing, so a
  surrogate-escaped diagnostic cannot turn a deliberate exit 2 into an exit 1 with empty stdout.
- State is plain `dict[str, Any]`. No dataclasses. `blockers` / `warnings` are lists passed by
  reference into evaluators and appended to.

**Git hardening** — every invocation:

```
git -c core.quotepath=false -c diff.ignoreSubmodules=none -c diff.renames=true -C <repo> <args>
```

with `timeout=20`, `capture_output=True`, `check=False`, output kept as **bytes**, decoded with
`errors="surrogateescape"`, and an environment that pops `GIT_DIR`, `GIT_WORK_TREE`,
`GIT_INDEX_FILE`, `GIT_OBJECT_DIRECTORY`, `GIT_COMMON_DIR`, `GIT_ALTERNATE_OBJECT_DIRECTORIES`,
`GIT_NAMESPACE`.

**Path safety** — port `safe_relative_path()` (`:180-199`), which rejects empty, `.`, `..`, any
backslash, absolute paths, non-round-tripping paths (catches a trailing `/`), any part in
`("", ".", "..")`, and any character below `0x20`. Do **not** port the PowerShell
`ConvertTo-NormalizedPath`'s `.TrimStart('./')` — it is a character-set trim that strips any leading
run of `.` and `/`, not a `./` prefix trim.

**Calling the promotion checker.** It exposes `main(argv) -> int` and `verify(namespace) -> dict`,
neither of which calls `sys.exit()`. Prefer in-process `verify(build_parser().parse_args([...]))`,
or subprocess with `--format json` and `json.loads(stdout)`. Either way, branch on the document's
`result` field first — exit 1 and exit 2 both produce valid JSON, and error codes land inside
`blockers[]` outside the frozen blocker table.

### TRX parsing

TRX carries the namespace `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`. A literal
`/TestRun/ResultSummary/Counters` XPath matches **nothing**. The PowerShell asset uses
`local-name()`; in Python use `ElementTree` with `{*}` wildcards or an explicit namespace map.

Attribute mapping — note the last row, which is the one that gets missed:

| Record field | TRX attribute |
| --- | --- |
| `total` | `total` |
| `executed` | `executed` |
| `passed` | `passed` |
| `failed` | `failed` |
| `skipped` | **`notExecuted`** |

A real TRX exists in-repo for shape reference:
`_bmad-output/implementation-artifacts/tests/epic-5-final-record.trx`.

Verified 2026-07-28: `tests/…/Hexalith.Conversations.Contracts.Tests -trx contracts.trx -noLogo`
produced `<Counters total="618" executed="618" passed="618" failed="0" … />`.

### Git plumbing traps — all eight verified during Story 6.7, reuse them

- **T-1** An uninitialized submodule directory answers as the umbrella. `git -C
  references/<empty-dir> status` returns the *umbrella's* dirty files. Guard with the
  `own_worktree()` pattern: walk every path component rejecting symlinks, resolve and re-check
  containment, require `path/.git`, and require `rev-parse --show-toplevel` to resolve back to the
  same path.
- **T-2** `git ls-tree <rev> -- <nonexistent-path>` exits **0** with empty output. Detect by empty
  output, never by exit status.
- **T-3** Mode **column**, not substring. `160000` can legitimately appear inside a blob hash or a
  filename. Raw format is `:<srcmode> <dstmode> <srcsha> <dstsha> <status>\t<path>`; with `-z` the
  path tokens follow as separate NUL fields, and a *second* path token appears only for status `R`
  or `C` — evaluate the destination path. The Epic 5 PowerShell asset gets this wrong
  (`.Contains('160000')`); do not port that line.
- **T-4** Never let unresolvable history pass green. Reproduced with `GIT_DIR=/nonexistent`; shallow
  clones make it real.
- **T-5** Remote availability with no network: `git -C <path> for-each-ref --contains <sha>
  --format='%(refname)' refs/remotes/`.
- **T-6** Non-traversal dirt detection is a two-command split: `status --porcelain --untracked-files=all
  --ignore-submodules=all` **plus** `diff-index --cached --name-status HEAD`.
- **T-7** Read stdout and stderr **concurrently** (`subprocess.run(..., capture_output=True,
  timeout=...)`), never sequentially from live pipes — the dotnet suite had to be patched for exactly
  this deadlock. Always pass a timeout.
- **T-8** `.gitattributes` line-ending pins as tabled above.

### File List derivation

Observed inventory, per the Epic 5 asset's `Get-DiffEntries`:

- `git diff --name-status --no-renames <range> --` → `path → status` map. `--no-renames` decomposes
  a rename into delete+add rather than an `R100` two-path record.
- `git ls-files --others --exclude-standard` → status `?`.
- **Self-accounting:** the generator writes into the tree it measures. Inject its own output paths
  into the observed map before comparing, or the record reports itself as an unexpected path.

Declared inventory, per `Get-StoryFileList`: enter on a line exactly equal to `### File List`, exit
on the next `^#{1,3}\s+` heading, collect `^-\s+`(?<path>[^`]+)``, sort unique. The Python sibling's
`story_file_list()` uses the same shape, splitting between `### File List` and `### Boundary
Confirmation`.

**File List format** (Story 6.7 precedent, 41 entries) — flat bullets of backticked repo-relative
paths, each annotated in parentheses, gitlinks last with a fuller annotation, **no submodule-internal
paths**:

```markdown
- `_bmad/scripts/generate_story_record.py` (new)
- `.agents/skills/bmad-dev-story/SKILL.md` (modified)
```

### The four workflow surfaces

| Surface | Insertion point |
| --- | --- |
| `bmad-dev-story/SKILL.md` | before line 428 `<action>Update the story Status to: "review"</action>`; mirror the promotion gate block at 416-427; amend the DoD list at 430-443 |
| `bmad-quick-dev/step-05-present.md` | between `### Promotion Completion Gate` (58-62) and `### Mark Spec Done and Synchronize` (64-66) |
| `bmad-quick-dev/step-oneshot.md` | between `### Promotion Completion Gate` (61-65) and `### Complete Trace and Commit Completion Record` (67-69) |
| `bmad-code-review/steps/step-04-present.md` | inside `### 6. Update story status and sync sprint tracking` (83), after the promotion gate subsection (87-95), before the `done` branch at 99 |

**Every one of these lands twice** — `.claude/skills/` and `.agents/skills/` are byte-identical
(936 vs 946 tracked files; the only delta is that `.agents/skills/` additionally contains
`aspire/`), and `test_both_skill_trees_stay_byte_identical_for_every_changed_file` enforces it.

**The coupling that breaks Story 6.7 (T9).** `_bmad/scripts/tests/test_verify_submodule_promotion.py`
defines `WORKFLOW_GATE_CONTRACTS` at `:916-981`: per workflow, a tuple of ordering markers plus
enforcement clauses that must live *inside* the gate span. `promotion_gate_span()` (`:983-997`)
bounds the span from the promotion-gate marker to **the next marker in the list**. For step-05 that
follower is hard-coded as `### Mark Spec Done and Synchronize`; for oneshot it is `### Complete Trace
and Commit Completion Record`; for `bmad-dev-story/SKILL.md` it is `Update the story Status to:
"review"`.

Inserting the new section between them **widens** the 6.7 gate span to swallow the new section. The
positive test still passes — which is why this is dangerous — but
`test_workflow_contract_rejects_enforcement_clause_outside_gate` gets weaker: a promotion clause
displaced into the final-record section would now count as "inside the gate". Add the new heading to
each affected marker tuple in the same change.

`bmad-dev-auto/step-04-review.md` is the **fifth** entry in that table and is not in this story's
four-surface scope — see D5.

### Blocker and warning codes

Source: `sprint-change-proposal-2026-07-28.md:423-427`. These are **not** in the frozen overlay,
which names blocking conditions only. Attribute them to the proposal in the runbook and code tables.

| Blocker | Condition | AC |
| --- | --- | --- |
| `TEST_RESULTS_MISSING` | declared project has no artifact | 2 |
| `TEST_RESULTS_STALE` | artifact older than newest derived-list file (excl. generator outputs) | 2 |
| `TEST_COUNT_INCONSISTENT` | computed totals disagree with parsed per-project counters | 2 |
| `FILE_LIST_DRIFT` | record's list disagrees with the derived set | 3 |
| `SUBMODULE_INTERNAL_PATH` | a path under a root-declared submodule appears in the File List | 3 |
| `CANDIDATE_NOT_FINAL` | candidate not an ancestor of `HEAD`, or a declared gitlink moved after it | 4 |
| `PROMOTION_GATE_NOT_PASS` | embedded checker document `result` is not `pass` | 4 |
| `BASELINE_NOT_TRUSTWORTHY` | baseline missing, unresolvable, or not an ancestor | 1 |
| `RECORD_NOT_DERIVED` | nothing parsed, resolved, or replaced | 6 |

| Warning | Condition |
| --- | --- |
| `UNRELATED_WORKTREE_DIRT` | dirty tracked/untracked state outside the derived scope |
| `TEST_PROJECT_UNDECLARED` | an artifact exists for a project not in the declaration |

Plus `NOT_RUN` as a per-project **state** (not a diagnostic code), per AC2.

### Previous story intelligence — the review patterns that will be applied to this story

Story 6.7 and 6.2 were reviewed hard. These are the findings that recur, and every one of them maps
onto a guard this story ships. Assume the reviewer checks all of them.

- **Guards that cannot fail.** 6.7 shipped three. One: `AUTHORIZED_BOUNDARY_EXCEPTIONS` listed every
  path the change mutated, so the boundary test asserted `[] == []`. Two: the File List completeness
  test compared prose against `EXPECTED_STORY_FILES`, a hand-maintained constant in the same file —
  `grep 'name-only\|name-status'` over the test returned nothing, so it could not detect an omission
  unless the author also edited the constant. Three: replacing quick-dev's whole gate body with "The
  gate is optional" while keeping the heading yielded zero violations. **AC8 exists because of these.
  A guard you cannot demonstrate failing has not been demonstrated.**
- **Vacuous pass.** `verify_submodule_promotion.py --repository . --candidate HEAD` returned `result:
  pass`, `evaluated: 0`, exit 0. Any agent that "ran the gate and it exited 0" satisfied the textual
  gate without evaluating anything. `RECORD_NOT_DERIVED` is the same fix; the Markdown renderer must
  make a nothing-derived run visibly different from a real one.
- **Self-comparison.** Both of 6.7's "live gate" runs passed `--baseline` and `--candidate` as the
  *same commit*, which cannot detect a gitlink change by construction.
- **Evidence bound to no revision.** 6.7 recorded 38/38 and 41/41 counts that belonged to a tree
  committed later; at the commit named in the record the suite necessarily failed. Those numbers were
  then copied into sprint-status as the justification for `in-progress → review`.
- **Hand-maintained constants drift.** 6.7's expected-path set said 39 while the File List said 41.
- **Counts contradict across passes.** 6.2 recorded Server `622` (07-27) then `631` (07-28),
  Conformance `418/418` then `417/418`, and never recomputed the `1,908` total after the change. This
  is the motivating defect; do not reproduce it in this story's own record.
- **Partial parity checks.** Only 5 of 13 skill-file pairs were compared. Compare every path in the
  File List across both trees.
- **Bookkeeping self-contradiction.** A task was checked `[x]` while the Debug Log asserted the
  opposite of the file's real state.
- **`git diff --check` half-truth.** Earlier "passed" claims were true only for the bare working-tree
  form. Run both forms.

### Reference implementation — port map and do-not-port list

`tests/Test-StoryFinalRecord.ps1` (746 lines) + `tests/Test-StoryFinalRecord.Tests.ps1` (263 lines)
are the Epic 4 action A1 asset. They are **well built** and already solve the hard parts. Port the
logic; do not re-invent it, and do not delete the asset — `tests/README.md` retains it as the Epic 5
historical record.

**Port:** TRX counter parsing (`Test-TrxResult`, 395-453) · path-set comparison producing
`missing`/`unexpected` deltas (`Test-PathSets`, 233-260) · frozen-state hashing by kind
(`Test-PreexistingState`, 262-355 — `untracked-file` / `tracked-file` / `gitlink`, each with its own
identity set, excluded from comparison *only while provably untouched*) · the input fingerprint that
proves no executable/test input changed after the final run (`Get-InputFingerprint`, 357-393 — the
direct ancestor of `TEST_RESULTS_STALE`) · the non-mutating contract-shape comparison (511-515) ·
the historical `pass-with-approved-amendment` mechanism · the always-write-output-then-fail ordering.

**Do not port:**

1. `artifact = 'epic-5-final-record-check'` — use `"schema": "story-final-record-v1"`.
2. `approvedProposal` — a July-14 pointer with no meaning here.
3. The three hard-coded historical story records for 5.1/5.2/5.3 with baked `blobOid`s and
   `finalCommit`s. Historical mode takes story identity as **input**, never a baked table.
4. The Markdown literals `'# Epic 5 Final-Record Check'` / `'## Historical Epic 5 Audit'`.
5. **`live.expectedChangedPaths` and `live.expectedCounts` as hand-authored input.** This is the
   entire defect Story 6.8 exists to remove: *"A wrong number that is wrong in both places passes."*
   They may survive only as derived output fields.
6. `amendmentPattern: "FINAL-RECORD-AMENDMENT-5.2-TEST-SUMMARY"` — port the mechanism, not the
   literal.
7. `ConvertTo-NormalizedPath`'s `.TrimStart('./')` character-set bug.
8. The `diff --raw` `.Contains('160000')` substring test (T-3).
9. The throw-based ungraded exit. Use the sibling's 0/1/2 contract.
10. The `executableInputFingerprint: "PENDING_FINAL_RUN"` placeholder — a generator computes it.

### Fault-injection table format (AC8)

Reproduce the Story 6.2 structure at `6-2-…md:542-552`. Columns are **Mutation** (the concrete edit,
value and file), **Target** (the guard it must trip), **Result** (observed failing count, bold). The
trailing restoration sentence is part of the precedent.

```markdown
**Fault injection — one mutation per guard, all confirmed able to fail.**

| Mutation | Target | Result |
| --- | --- | --- |
| TRX `passed` attribute decremented by one | AC2 `TEST_COUNT_INCONSISTENT` | **1 failed** |

Artifacts restored byte-identically after each injection (hash-verified), worktree left clean.
```

### Open decisions

These were resolved during story preparation. Each records the call and its reasoning so a reviewer
can overturn it deliberately rather than discover it.

- **D1 — AC6 guard is C#, not Python.** The proposal names
  `tests/…/StoryFinalRecordGenerationValidationTest.cs` (`:491`), but the only existing
  non-deletability guard is Python (`test_verify_submodule_promotion.py:916-1068`) and no conformance
  test currently reads the skill trees. **Decision: follow the proposal and write C#**, porting the
  proven four-test shape. Rationale: the C# lane actually executes (it is part of the 1,900-test
  conformance run), whereas the pytest suite is manual-only and requires `uv` — the standing deferred
  limitation. A guard that nothing runs is the weaker of the two. `ReadRepositoryFile` /
  `FindRepositoryRoot` already exist in the Conformance project and read `_bmad-output/` files, so
  reading `.claude/skills/` is the same mechanism. Trade-off accepted: Story 6.7's own guard stays
  Python-only, so the two live in different lanes.
- **D2 — the 6.7 gate-span coupling is repaired in this story (T9).** Not deferred. The failure mode
  is silent: the positive test keeps passing while the displacement guard weakens.
- **D3 — staleness excludes the generator's own write targets.** AC2 blocks on an artifact older than
  the newest file in the derived File List; AC5 requires writing the record *into* the story file,
  which is in that list. Left unresolved, every re-run — including the code-review surface, which
  regenerates after patches — would raise `TEST_RESULTS_STALE` on a correct record. **Decision:**
  compare against the derived list minus the generator's output targets (the story/spec record and
  the sprint-tracking file). Ship a fault injection proving a genuinely stale artifact still blocks,
  so the exclusion cannot be mistaken for a hole.
- **D4 — historical mode classifies by record shape, not by a story table.** Running `--historical`
  over Story 6.2 will legitimately find submodule-internal paths and a second hand-maintained list —
  and the v4 disposition table authorizes 6.2 to close exactly as it is, while the prohibitions forbid
  rewriting closed records. **Decision:** a record carrying no `story-final-record-v1` block is
  `pre-generator`; its AC2/AC3-shaped findings are reported as warnings, not blockers. Derived from
  the record itself, so no hard-coded story list (do-not-port item 3).
- **D5 — `bmad-dev-auto/step-04-review.md` is out of scope, and that is disclosed.** The authority
  says "the four completion surfaces" and names four. But this fifth surface has a promotion gate and
  writes `frontmatter status: done`, so leaving it ungated is a real bypass route to `done` without a
  generated record. **Decision:** do not add it — expanding frozen AC scope is not a dev-agent call.
  Record it in `deferred-work.md` as a known bypass and surface it to the owner. If Jerome authorizes
  the expansion, it is a fifth entry in T8 and T9, not a redesign.
- **D6 — Story 6.8 closes through its own generator (T11).** The order places 6.8 after 6.2, and no
  story completing after 6.2 reaches `done` without a generated record. No source states the
  self-application explicitly; it follows, and a hand-typed 6.8 record would falsify AC5 on delivery.
- **D7 — code strings are attributed to the proposal, not the overlay.** The frozen block names
  conditions only. Do not write "per the Epic 6 authority" next to a code string.

### Project Structure Notes

`architecture.md:1153-1178` declares its target directory tree "authoritative" and does **not**
contain `_bmad/`, `_bmad-output/`, or any scripts directory — that tree describes the .NET module,
not the workflow tooling. The generator's placement at `_bmad/scripts/generate_story_record.py` rests
on the proposal (`:361`, `:488`) plus the established precedent of `verify_submodule_promotion.py`,
`memlog.py`, `resolve_config.py`, and `resolve_customization.py` already living there. State this
rationale in the runbook so a reviewer does not read the placement as a structure violation.

Known artifact defects found during preparation, **not fixed by this story** (they would be authority
edits, and expanding scope is not a dev-agent call). Record them in `deferred-work.md`:

- `epic-6-context.md:8` — `source_overlay_begin` still names the v3 marker
  (`EPIC-6-AUTHORITY-OVERLAY-AMENDMENT:BEGIN`) while `overlay_version` is v4, whose real marker is
  `EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V4:BEGIN`. The context declares provenance from the block it
  supersedes.
- `epic-6-context.md:140` — the derived prohibition list drops three categories the frozen text
  carries (package versions, generated output, accepted baselines). This story quotes the frozen text
  verbatim above to neutralize it for 6.8; the context itself stays wrong for the next reader.
- `architecture.md:1548-1550` — `Corrective Implementation Handoff` still states the pre-v4 sequence
  with no mention of 6.8, contradicting `:193` in the same document.
- `_bmad/render/bmad-quick-dev/` and `bmad-dev-auto/` are derived, stale snapshots that Story 6.7
  decided not to edit — but commit `5ed5e20` modified five of them anyway, so the "untouched" state
  6.7 recorded no longer holds. Do not edit them; record what you find.

### References

- Frozen authority: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:979-1074`
- Correction authority: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md:62-85` (v4 amendment), `:191-193`
  (corrective readiness), `:187-189` (promotion completion invariant), `:1084-1087` (conformance-test
  guardrail), `:1093-1096` (failure-injection requirement)
- Derived context: `_bmad-output/implementation-artifacts/epic-6-context.md:132-140` (story 6.8),
  `:152-154` (Final Record Invariant), `:156-158` (Promotion Completion Invariant)
- Sibling implementation: `_bmad/scripts/verify_submodule_promotion.py`
- Test precedent: `_bmad/scripts/tests/test_verify_submodule_promotion.py:916-1083`
- Reference implementation to port: `tests/Test-StoryFinalRecord.ps1`, `tests/Test-StoryFinalRecord.Tests.ps1`
- Runbook to mirror: `docs/runbooks/submodule-promotion-completion-gate.md`
- Conformance pin: `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:28-45`, `:490-578`, `:637-694`, `:703-739`
- Previous stories: `_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md`, `_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md`
- Standing limitation: `_bmad-output/implementation-artifacts/deferred-work.md:7`, `:33`
- Environment: `tests/README.md:59-93`, `_bmad-output/project-context.md`

## Dev Agent Record

### Agent Model Used

claude-opus-5 (Claude Code, `bmad-dev-story`)

### Debug Log References

**Baseline state confirmed before implementation (2026-07-28).** `HEAD` was `bb5b777`, matching
frontmatter `baseline_commit`; working tree carried one modified file (`sprint-status.yaml`) and two
untracked files. Conformance built Release 0 warnings / 0 errors with
`-p:UseHexalithProjectReferences=true` and ran **418 total, 2 failed, 0 skipped, 0 not run** — both
failures in `ProjectionReadStorePopulationProofValidationTest`
(`ProofSourceAndSignedV1BindingsShouldRemainByteIdentical`,
`RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks`), which is exactly the
pre-existing failure set this story's Dev Notes attributes to Story 6.2's open evidence state. Not
caused by, and not attributable to, this story.

**BLOCKING SCOPE FINDING — two undeclared root gitlinks moved after the baseline.** A concurrent
session committed `e74c09a` ("feat: close Epic 5 release-owner decision ledger entry") during this
implementation window. That commit is not this story's work, but it is now in history between the
baseline and any candidate this story can produce, and it moved two root gitlinks:

| Path | Baseline `bb5b777` | Current `e74c09a` | Worktree state |
| --- | --- | --- | --- |
| `references/Hexalith.Memories` | `1868c8f94ca1ec723a30b256a29c7c8495bc8cca` | `115d30b59101910d0fd30717f49a5fb7f1782547` | initialized, clean, on `origin/main` |
| `references/Hexalith.Tenants` | `f9e51c66745557da4f267ab40f32294f2f27fae7` | `8d64563c75423c861b0be0e3a7cc4de18f673d37` | initialized, clean, on `origin/main` |

`e74c09a` also swept in three umbrella paths that were untracked or modified in this session's
working tree at the time: this story file, the `sprint-status.yaml` `ready-for-dev → in-progress`
edit made by this workflow, and the release-owner-decision ledger proposal.

This is precisely the condition the frontmatter comment on `submodule_promotions` predicted and
forbade resolving silently. The promotion gate surfaces both paths as `UNDECLARED_GITLINK_CHANGE`,
so they are affected scope for this story's record whether or not this story caused them.
**`submodule_promotions` was NOT edited.** The declaration decision was put to Jerome before the
completion gate; see Completion Notes. Nothing was committed inside either submodule, and neither
submodule was initialized, updated, or fetched.

**(SUPERSEDED IN PART, 2026-07-28 — the `Current e74c09a` column above is no longer current for
`references/Hexalith.Tenants`, which moved again to `2e61f57bda6379192007d1bc6fabbde61996b11d` in
`f954202`. The entry is preserved because it is the true record of the condition as found at that
moment; see the next entry for the state that superseded it.)**

**SECOND CONCURRENT COMMIT — the same condition recurred, and AC4 caught it (2026-07-28).** After
this story's first completion candidate `33d2cac`, a concurrent `correct-course` session committed
`f954202` ("feat: add Conformance Oracle Tiering Change Proposal and Decision Artifacts" — authority
v5, Story 6.9). Three consequences, each verified in the tree rather than inferred:

| Path | Baseline `bb5b777` | Candidate `f954202` | Moved by | Worktree state |
| --- | --- | --- | --- | --- |
| `references/Hexalith.EventStore` | `5a1d277ec0583e304986488d299eb3e6e5022487` | `589da8b91bbf443b39f48fbc0aa7ac30286a56d6` | `f954202` | initialized, clean, HEAD == gitlink, on `origin/main` |
| `references/Hexalith.Memories` | `1868c8f94ca1ec723a30b256a29c7c8495bc8cca` | `115d30b59101910d0fd30717f49a5fb7f1782547` | `e74c09a` | initialized, clean, HEAD == gitlink, on `origin/main` |
| `references/Hexalith.Tenants` | `f9e51c66745557da4f267ab40f32294f2f27fae7` | `2e61f57bda6379192007d1bc6fabbde61996b11d` | `e74c09a`, then `f954202` | initialized, clean, HEAD == gitlink, on `origin/main` |

1. **The `33d2cac` binding was superseded, exactly as AC4 requires.** `Hexalith.Tenants` is a
   *declared* gitlink and it moved *after* the candidate, so the record went red
   (`CANDIDATE_NOT_FINAL`) rather than quietly stale. The remedy the runbook names — re-run against
   the committed head — is what was done. This is the guard working, not a defect being patched.
2. **The 15:35 measurement was invalidated by something no file-content check would catch.** The
   `EventStore` and `Tenants` submodule *worktrees* were re-checked-out at 17:50, three hours after
   the suite ran. Because this repository builds with `-p:UseHexalithProjectReferences=true`, those
   worktrees are compile inputs, so the recorded counts no longer described the tree at `HEAD`. The
   full suite was re-run from a clean restore before anything was regenerated. Recorded here because
   `TEST_RESULTS_STALE` compares mtimes of files in the *derived list*, and a gitlink is routed to
   the promotions section instead — so this particular invalidation is caught by a human reading the
   checkout times, not by the gate. It is a real residual limitation, not a hypothetical.
3. **`sprint-status.yaml` was clobbered by the concurrent write.** `f954202` reverted this story's
   line from `review` to `in-progress` and replaced its generated completion comment with the Story
   6.9 comment. That is the shared-file collision the Story 6.2 precedent warns about, not a
   deliberate reopening of the story. Both were restored from generated state, and again only this
   story's own status line was staged.

`submodule_promotions` was again **not** edited until Jerome approved the expansion; see the
frontmatter. Nothing was committed inside any submodule, and no submodule was initialized, updated,
fetched, or traversed.

### Completion Notes List

**AC1 — one generator, one source of truth.** `_bmad/scripts/generate_story_record.py` (mode 755,
PEP-723 `requires-python = ">=3.11"`, stdlib only) emits `"schema": "story-final-record-v1"` with
the flags the operational contract names. It ports the sibling checker's structural spine — `SCHEMA`,
`GateError`, `GateArgumentParser`, `diagnostic()`, `empty_document()`, hardened `run_git()` with the
`GIT_*` var-popping environment, `verify()` → dict, `main(argv) -> int`, `write_output()` — so both
halves of the completion gate share one runtime, one document shape, and one code vocabulary. Exit
`0`/`1`/`2`; a parseable document reaches stdout on every path including exit `2`, verified by
`test_an_invocation_error_still_emits_a_parseable_document`.

**AC2 — counts come only from artifacts.** TRX is parsed with `{*}` wildcards; a literal
`/TestRun/...` XPath matches nothing against the `…/TeamTest/2010` namespace. `skipped` is read from
`notExecuted`. Beyond parsing the summary the generator **recomputes** the counts from the
`<UnitTestResult>` outcomes and blocks when the two disagree, which is what makes "computed rather
than transcribed" mechanical rather than aspirational. Totals are summed across projects; no caller
total is accepted. Each artifact's SHA-256 and mtime are recorded.

**AC3 — File List derived, singular, boundary-correct.** Derived from
`git diff --name-status --no-renames <baseline>..<candidate>` unioned with the tracked working-tree
delta and `git ls-files --others --exclude-standard`, self-accounting for the generator's own output
targets. Gitlinks are routed to `### Gitlink Promotions` with recorded commit and mode and never
appear as file entries. `SUBMODULE_INTERNAL_PATH` is asserted against the **record's declared list**,
not only the derived set — a first implementation checked only the derived set and could not fail,
because `git status --ignore-submodules=all` means the umbrella's own delta can never surface a path
inside an initialized submodule. That gap was found by the fault injection, which is the point of AC8.

**AC4 — candidate binding.** Mode-`160000` entries are resolved by parsing the mode **column** of
`git diff --raw --no-abbrev -z` and `git ls-tree -z`; the substring test the Epic 5 PowerShell asset
uses was deliberately not ported, and a decoy file named `blob-160000-not-a-gitlink.txt` proves the
difference. The candidate must be an ancestor of the committed head with no affected gitlink moved
after it. The Story 6.7 checker is invoked in-process and its document embedded verbatim; the branch
is on its `result` field, never on its return code or on `blockers[].code`, because exit 1 and exit 2
both emit valid JSON.

**AC5 — the four surfaces generate.** All four now invoke the generator and insert its rendered block
verbatim, in **both** skill trees (eight files). `diff -rq` reports only the approved
`Only in .agents/skills: aspire`.

**AC6 — anti-vacuity and non-deletability.** `RECORD_NOT_DERIVED` fires when no artifact was parsed,
no candidate resolved, or no replaceable record section found, and the document can never report
`pass` while it is present. The Markdown renderer names what it derived on every run, so a vacuous
run is not byte-comparable to a measured one (`test_a_nothing_derived_run_renders_visibly_differently`).
`StoryFinalRecordGenerationValidationTest` executes in the Conformance lane, holds each completion
surface to a span-bounded and order-sensitive contract, and fault-injects structural removal,
displacement, reordering, and completion-dependency loss.

**AC7 — historical mode.** Read-only, no writes of any kind, and the promotion checker is **not** run
because it inspects live submodule worktrees and would amount to reconstructing a former working tree.
Classification is derived from the record's own shape, never from a baked story table. The historical
checks cover the approved pre-generator records named by AC7, report their legacy shape honestly,
and leave every source record byte-identical. Their detailed measurements belong to the generator
output from each verification run and are not retyped into this narrative.

**AC8 — every guard proven able to fail.** See the fault-injection table below. The permanent
regressions exercise the current generator rather than relying on prose from an earlier measurement.

**Self-application (T11/D6).** The previous generated block is superseded and has been removed by
owner decision during Chunk 2 review. This story remains `in-progress` with no `file_list_commit`
until a green, final candidate can produce and digest-verify its replacement. No earlier measurement
is carried forward as completion evidence.

**Completion validation remains open.** The earlier measurement is retained only in the historical
debug log above; it is not a completion claim and none of its totals are repeated here. Chunk 1
re-ran the current gates and recorded their stable blocker codes in the review section. A later green
candidate must run the required solution, focused workflow, and promotion checks again and place all
measured values only inside the new generated record.

**Reviewer-facing honesty note on the artifacts.** TRX evidence lives in the gitignored
`TestResults/` directory and is not committed, diverging from the Epic 5 precedent. A TRX embeds
fresh identifiers and timestamps on every run, so committing it preserves bytes but does not make a
future run reproducible. The generated record binds each artifact by SHA-256; re-verification reruns
the suite and derives a new record rather than copying values from this narrative.

**A defect this story found in its own scope and fixed.** The first staleness implementation compared
every artifact against the newest derived path *including the other artifacts*. A suite takes time
to run, so an earlier project artifact is naturally older than a later one. Artifacts are now
excluded from their own comparand alongside the D3 output targets, and
`test_staleness_exclusion_does_not_hide_a_genuinely_stale_artifact` proves the exclusion is exactly
that narrow: touching only an output target does not report staleness, touching any ordinary derived
path still does.

**The File List is a derived range, not a claim of authorship.** Concurrent commits can legitimately
enter the baseline-to-candidate range even when another session authored them. The generator must
report the complete range and must never hand-trim it to imply ownership. The superseded list is no
longer repeated here; its replacement will be emitted only after the final candidate is green.

### File List

_Pending regeneration from a green, final candidate. The superseded generated record was removed by
owner decision during Chunk 2 review and none of its measured values are carried forward._

**Fault injection — one mutation per guard, all confirmed able to fail.** This table records the
prior fault-injection procedure without retyping the superseded record's measured values. It does
not establish current completion; the permanent hermetic regressions must pass again before a green
record is generated.

| Mutation | Target | Result |
| --- | --- | --- |
| A TRX `passed` counter decremented | AC2 `TEST_COUNT_INCONSISTENT` | **blocked — TEST_COUNT_INCONSISTENT** |
| `- \`references/Hexalith.EventStore/src/Leaked.cs\`` appended to the record's File List | AC3 `SUBMODULE_INTERNAL_PATH` | **blocked — SUBMODULE_INTERNAL_PATH + FILE_LIST_DRIFT** |
| `--candidate` repointed to the baseline | AC4 `CANDIDATE_NOT_FINAL` | **blocked — CANDIDATE_NOT_FINAL + FILE_LIST_DRIFT + PROMOTION_GATE_NOT_PASS** |
| `references/Hexalith.EventStore` dropped from `--submodule`, kept in `--require-remote` | AC4 `PROMOTION_GATE_NOT_PASS` | **blocked — PROMOTION_GATE_NOT_PASS, embedded `INVALID_SCOPE`** |
| `TestResults/6-8-conformance.trx` removed | AC2 `TEST_RESULTS_MISSING` | **blocked — TEST_RESULTS_MISSING, project state `NOT_RUN`** |
| `TestResults/6-8-conformance.trx` mtime backdated | AC2 `TEST_RESULTS_STALE` | **blocked — TEST_RESULTS_STALE** |

Artifacts were restored byte-identically after each injection (SHA-256-verified rather than checked
by inspection). The candidate-repoint and dropped-gitlink mutations change arguments rather than
files, so no restoration applies. Each mutation is also encoded as a permanent hermetic regression in
`_bmad/scripts/tests/test_generate_story_record.py`, so a future change that silently removes a guard
turns the suite red rather than passing quietly.

### Boundary Confirmation

**This story authored** workflow tooling and documentation only: the generator and its pytest suite;
the completion surfaces in both skill trees; the coupled promotion-workflow contracts; the focused
Conformance guard; the runbook and test guidance; the deferred-work entry; and its own story/sprint
record updates. Exact path membership belongs only in the generated File List.

**Concurrent changes are not story authorship.** A derived baseline-to-candidate range may contain
paths authored by other sessions. The prior Boundary Confirmation disclosed those paths for review,
but no path total or authorship partition is carried forward after the superseded record's removal:

| Foreign path | Authored by |
| --- | --- |
| `_bmad-output/implementation-artifacts/epic-6-context.md` | `f954202` — authority v5 correct-course |
| `_bmad-output/planning-artifacts/architecture.md` | `f954202` — authority v5 correct-course |
| `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` | `f954202` — authority v5 correct-course |
| `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` | `f954202` — authority v5 correct-course |
| `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` | `f954202` — authority v5 correct-course |
| `docs/release-evidence/conformance-oracle-tiering-decision-v2.md` | `f954202` — authority v5 correct-course |
| `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` | `f954202` — authority v5 correct-course |

The release-owner decision-ledger proposal was also concurrent work from an earlier commit and was
already disclosed during the prior pass.

**This story did not change** production source, public contracts, package versions, generated
output, accepted baselines, signed evidence, or sibling submodule source. It did not rewrite any
closed story record — the three verified historically are SHA-256-identical before and after. It did
not initialize, update, fetch, or traverse any submodule, and committed nothing inside one. **It did
not edit `epics.md`, `architecture.md`, `epic-6-context.md`, or
`ArchitecturePlanningAuthorityValidationTest.cs`, which appear in the derived list solely as
range members** — verified by `git diff` between `33d2cac` and this candidate attributing every byte
of those four files to `f954202`; the frozen v4 acceptance criteria are quoted in this file and were
never modified in their source. It did not touch `_bmad/render/`. It did not flip the Epic 3 action
item, which stays `in-progress` until Story 6.8 itself reaches `done`, per the Story 6.7 review
precedent. It did not delete or re-mark the Epic 5 PowerShell asset
(`tests/Test-StoryFinalRecord.ps1`), which `tests/README.md` retains as the historical record, and it
did not re-mark Epic 4 action A1.

**It does not claim** that anything executes these gates automatically. No CI workflow or hook runs
the generator; `StoryFinalRecordGenerationValidationTest` proves the invocation prose is present and
still enforcing, which is not the same as proving the generator ran. That remains the standing
deferred item, now recorded for a third time.

**It does not claim** `bmad-dev-auto/step-04-review.md` is gated. Per D5 it was deliberately left out
of scope because the frozen text names four surfaces, and it is disclosed in `deferred-work.md` as a
live bypass route to `done` rather than silently absorbed.

**All three root gitlinks in this story's declared scope were moved by other sessions, not by this
story.** `references/Hexalith.Memories` and `references/Hexalith.Tenants` advanced in `e74c09a`;
`references/Hexalith.EventStore` advanced in `f954202`, which also advanced `Hexalith.Tenants` a
second time. Both commits sit between the baseline and every candidate this story can produce. All
three are declared as **inherited affected scope** under Jerome's approvals recorded in the
frontmatter — the first two on the `33d2cac` pass, EventStore on this one — and none is claimed as
this story's promotion. Declaring a gitlink here asserts that its recorded state was evaluated, never
that this story advanced it.

**This story has now been through the failure mode it was written to prevent, from the other side.**
Its first record bound to `33d2cac` with counts measured at 15:35. A concurrent commit then moved a
declared gitlink and re-checked-out two submodule worktrees, and had that record been left in place
it would have been exactly the artifact this story exists to make impossible: real numbers, correctly
derived, bound to a tree that no longer existed. `CANDIDATE_NOT_FINAL` turned it red instead of
letting it go stale, which is the entire point of AC4 and the direct generalization of Story 6.2's
bespoke `RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` guard.

## Change Log

| Date | Change |
| --- | --- |
| 2026-07-28 | Story created from the v4 authority amendment and the approved 2026-07-28 correction proposal. Status `backlog` → `ready-for-dev`. |
| 2026-07-28 | Implemented the story tasks. Added the generator, its regression suite, the C# non-deletability guard, the runbook, and the gated completion surfaces in both skill trees; repaired the Story 6.7 gate-span coupling in the same change. Inherited gitlink scope expansions were owner-approved before declaration. A generated record was inserted and the story moved to `review`; that record was later superseded. |
| 2026-07-28 | Re-anchored after a concurrent correct-course commit moved declared gitlinks past the candidate and changed source-reference worktrees. The suite, File List, promotion gate, and fault injections were re-derived rather than carried forward. A later review proved the replacement record was also no longer final. |
| 2026-07-29 | Chunk 2 review removed the superseded generated block by owner decision, restored `in-progress`, and prohibited restating its measured values. A new `file_list_commit` will be written only by a green final-candidate bundle. |
