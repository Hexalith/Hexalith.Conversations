---
story_key: '6-8-generate-the-final-story-record-mechanically-from-measured-state'
epic: 6
story_id: '6.8'
created: '2026-07-28'
status: 'review'
baseline_commit: 'bb5b777f9b8e6932b1bae93c14b7d456a0e3c5cd'
file_list_commit: 'f954202206bc3e2ccb941ca9e257c4856d1f0c53'
# ^ The exact revision the File List was derived from.
#   Story 6.7 review chunk 1 established the rule: a fixed File List compared against a moving
#   `HEAD` makes the story's own suite fail forever on the next legitimate commit. Story 6.2
#   omitted the field, which is why its recorded "49 paths" cannot be reproduced today. This is
#   the same defect AC4 (`CANDIDATE_NOT_FINAL`) generalizes from one story to every record.
#
#   RE-ANCHORED from `33d2cac` to `f954202` on 2026-07-28. This is AC4 doing its job, not a
#   correction of an error: a declared gitlink (`references/Hexalith.Tenants`) moved in the
#   concurrent commit `f954202`, which lands AFTER `33d2cac`, so that binding became superseded and
#   the record went red rather than stale. The whole record was re-derived at the new candidate —
#   suite re-run, File List re-derived, gate re-embedded, guards re-injected. Nothing was carried
#   forward from the `33d2cac` pass.
submodule_promotions:
  - path: 'references/Hexalith.EventStore'
    require_remote: true
  - path: 'references/Hexalith.Memories'
    require_remote: true
  - path: 'references/Hexalith.Tenants'
    require_remote: true
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
  overlay: 'epic-6-authority-2026-07-28-v4'
  architecture: 'conversations-architecture-2026-07-28-v4'
  proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28.md'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md'
---

# Story 6.8: Generate the final story record mechanically from measured state

Status: review

## Story

As a developer closing an Epic 6 story,
I want the final story record — test counts, File List, submodule state, and root gitlink state — emitted by a generator that reads the repository's measured final state,
so that a completion record can never contain a number, path, or commit that nobody measured.

## Acceptance Criteria

The eight criteria below are **frozen authority**, quoted verbatim from
`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md:1017-1041`. Do not
paraphrase them, do not renumber them, and do not soften them. Each is followed by an
**Operational contract** block whose source is the approved correction proposal
(`sprint-change-proposal-2026-07-28.md`), one authority level below the overlay — the proposal is
named in `architecture.md:20` `correctionAuthority`, so it is binding input, but it is *not* the
frozen text.

### AC1 — one generator, one source of truth

> 1. One generator emits a versioned final-record document whose every field is
>    derived from the four sources named above. No count, path, or commit may be
>    supplied as caller-authored text.

**The four sources** (`epics.md:1004-1009`, verbatim):

> Derivation sources are exactly four: parsed machine-readable test-result
> artifacts; the git-derived path set between the work baseline and the committed
> candidate unioned with the tracked working-tree delta; mode-`160000` root
> gitlink entries resolved from the committed candidate; and the Story 6.7
> promotion-checker document embedded verbatim. A record that could not derive any
> of them reports a blocker rather than a pass.

**Operational contract** (proposal `:361-369`): `_bmad/scripts/generate_story_record.py`, document
field `"schema": "story-final-record-v1"`, flags `--repository`, `--story`, `--baseline`,
`--candidate`, `--test-results`, `--submodule`, `--require-remote`, `--format json|markdown`, plus
`--historical` from AC7.

### AC2 — counts come only from machine-readable artifacts

> 2. Test counts come only from machine-readable result artifacts. A declared test
>    project with no artifact is recorded as not run and blocks; totals are
>    computed rather than transcribed; an artifact older than the newest file in
>    the derived file list blocks as stale rather than being carried forward.

**Operational contract** (proposal `:372-379`): parse TRX `/TestRun/ResultSummary/Counters`. The
per-project state for a declared project with no artifact is `NOT_RUN`. Never silently omit a
project and never carry a count forward from an earlier pass.

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

**Operational contract** (proposal `:389-394`): embed the Story 6.7 checker document and add the
re-derivation binding. This generalizes Story 6.2's bespoke
`RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` guard from one story's evidence
file to every record.

### AC5 — the completion surfaces generate; none of them types

> 5. The four completion surfaces generate rather than author, and generator
>    blockers block `review` and `done` exactly as the promotion gate does.

**Operational contract** (proposal `:396-401`): the four surfaces are `bmad-dev-story` step 9,
`bmad-quick-dev/step-05-present.md`, `bmad-quick-dev/step-oneshot.md`, and
`bmad-code-review/steps/step-04-present.md`. Each invokes the generator after its final validation
and inserts the rendered output **verbatim** into the story/spec record and the sprint-status
comment. See **Dev Notes → The four workflow surfaces** — every edit lands in two skill trees, and
one of them silently weakens Story 6.7's guard unless a coupled file is updated in the same change.

### AC6 — anti-vacuity and non-deletability

> 6. The generator cannot report a pass having derived nothing, and the
>    invocation cannot be silently removed from a completion workflow.

**Operational contract** (proposal `:403-408`): no artifact parsed, no candidate resolved, or no
record section replaced ⇒ `RECORD_NOT_DERIVED`. A conformance test asserts all four workflow bodies
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
  - [x] Derive from `git diff --name-status --no-renames <baseline>..<candidate> --` unioned with
        the tracked working-tree delta and `git ls-files --others --exclude-standard`.
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
`StoryFinalRecordGenerationValidationTest` (7 tests, executing in the Conformance lane) holds each of
the four surfaces to a span-bounded contract and proves three mutations fail: heading removed, body
gutted, clause displaced outside the span.

**AC7 — historical mode.** Read-only, no writes of any kind, and the promotion checker is **not** run
because it inspects live submodule worktrees and would amount to reconstructing a former working tree.
Classification is derived from the record's own shape, never from a baked story table.

| Closed record | Result | Classification | Declared | Re-derived | Missing | Unexpected | Promotions | Findings |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `spec-6-1-…-planning-authority.md` | pass | pre-generator | 9 in 1 list | — | 0 | 0 | 0 | `CANDIDATE_NOT_FINAL` (no `file_list_commit`) |
| `6-2-…-platform-owned-hosting.md` | pass | pre-generator | 67 in **2 lists** | — | 0 | 0 | 0 | 10 × `SUBMODULE_INTERNAL_PATH`, `FILE_LIST_DRIFT`, `CANDIDATE_NOT_FINAL` |
| `6-7-…-from-completion.md` | pass | pre-generator | 41 in 1 list | 37 | **0** | **0** | 4 | none beyond the pre-generator notice |

Story 6.7's File List **reproduces exactly**: 37 derived paths plus the 4 root gitlinks the generator
routes to the promotions section, accounting for all 41 recorded entries with zero missing and zero
unexpected. Story 6.2's record legitimately carries a second hand-appended list and ten
submodule-internal paths; per D4 these are reported as warnings, the record is not rewritten, and the
run does not block. All three files were SHA-256-verified byte-identical before and after
(`ad0f819e…dedf`, `c18fc3ad…a609`, `bf58ac6a…7efd`).

**AC8 — every guard proven able to fail.** See the fault-injection table below. Run against the
**live** record and the live artifacts, not only against fixtures. The table was re-executed in full
against the re-anchored record at candidate `f954202` and its freshly measured artifacts; the earlier
`33d2cac` run was not carried forward, because a guard demonstrated against artifacts that no longer
exist has not been demonstrated against the record being shipped.

**Self-application (T11/D6).** This story's own completion record was produced by the generator it
builds and pasted verbatim; nothing between the markers was typed. The run is non-vacuous: 8 artifacts
parsed, 25 file-list paths, 3 gitlink promotions evaluated, all three derivation inputs true, zero
drift, zero blockers.

**Final measured state at candidate `f954202`:** Release build of the full solution 0 warnings / 0
errors with `-p:UseHexalithProjectReferences=true`, from a clean restore against the submodule
worktrees `f954202` checked out. Eight test projects, **1,925 total / 1,922 passed / 2 failed / 1
skipped**, computed by summation from eight TRX artifacts, never transcribed. Promotion completion
gate **pass, 0 blockers, 0 warnings**, all three declared gitlinks initialized, clean,
remote-available, exactly captured at mode `160000`. Checker/workflow/story pytest **129/129**.

**The counts are unchanged from the `33d2cac` pass, and that is a measurement, not an assumption.**
Per-project totals came out identical (425 / 631 / 618 / 185 / 14 / 14 / 29 / 9) after the EventStore
and Tenants source moved underneath the build. The artifacts are nonetheless different files with
different SHA-256 values, and the record binds to the new ones. Nothing was reused: had the counts
been carried forward instead of re-measured, this record would be asserting a number nobody measured
against this tree — the exact defect the story exists to remove.

**The 2 failures are pre-existing and not attributable to this story.** Both are in
`ProjectionReadStorePopulationProofValidationTest` — `ProofSourceAndSignedV1BindingsShouldRemainByteIdentical`
and `RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` — and belong to Story 6.2's open
evidence state. They were confirmed present at baseline `bb5b777` **before** any file was written, with
the identical failure set and the identical 418-test denominator. Conformance is now 425 because this
story added 7 tests; 418 + 7 = 425 with zero regressions. The 1 skipped test is the AppHost opt-in live
lane, unchanged.

**Reviewer-facing honesty note on the artifacts.** The eight TRX files live in the gitignored
`TestResults/` directory and are therefore **not** committed, diverging from the Epic 5 precedent which
committed one. Reason, stated rather than glossed: a TRX embeds fresh GUIDs and timestamps on every
run, so committing one preserves it but does not make it reproducible, while putting 3.6 MB of
per-run XML inside the record's own File List makes the list churn on every regeneration. The record
binds to each artifact by SHA-256, so the counts are bound to the exact bytes measured; re-verification
is by re-running the suite and comparing counts, which the runbook states.

**A defect this story found in its own scope and fixed.** The first staleness implementation compared
every artifact against the newest derived path *including the other artifacts*. A suite takes minutes
to run, so the project finishing first is always older than the project finishing last, and the guard
would have reported a correct 8-project run as stale. Artifacts are now excluded from their own
comparand alongside the D3 output targets, and
`test_staleness_exclusion_does_not_hide_a_genuinely_stale_artifact` proves the exclusion is exactly
that narrow: touching only an output target does not report staleness, touching any ordinary derived
path still does.

**The File List is a derived range, not a claim of authorship — and it now proves the difference.**
At the `33d2cac` pass those seven authority-v5 paths were dirty-but-uncommitted, so the record
excluded them and named each in an `UNRELATED_WORKTREE_DIRT` warning. The concurrent session then
committed them in `f954202`, which is now inside `bb5b777..f954202`, so the same unchanged derivation
rule pulls all seven **into** the list: 18 paths became 25. Both outcomes are correct, and the
transition is the clearest available demonstration of what AC3 actually specifies. The File List is
"the git-derived path set between the work baseline and the committed candidate", nothing more. It
answers *what changed in this range*, never *who wrote it*. The seven foreign paths are named
individually in Boundary Confirmation so no reader mistakes range membership for authorship, and they
are the reason a derived list must never be hand-trimmed to look tidier than the range it binds to.

A record binds to `file_list_commit`; a path that revision does not contain cannot be re-derived from
it, which is precisely the defect that makes Story 6.2's recorded "49 paths" unreproducible today.
One path remains dirty-but-uncommitted at this candidate — a third concurrent session's
`sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md` — and it is excluded and
named by the surviving `UNRELATED_WORKTREE_DIRT` warning rather than dropped silently. Nothing
belonging to any concurrent session was staged or committed by this story, including
`sprint-status.yaml`, where the sessions' edits interleave in one file: **only this story's own status
line and its own comment were staged**, by writing the exact blob to the index rather than by
`git add`.

<!-- STORY-FINAL-RECORD:BEGIN -->

**Final record** — `story-final-record-v1`, result **PASS**, mode `live`. The JSON document is authoritative; this Markdown is rendered from it.

Derived: test results **yes**, candidate **yes**, record section **yes** · 8 test artifact(s) parsed · 25 file-list path(s) · 3 gitlink promotion(s) evaluated.

Baseline `bb5b777f9b8e6932b1bae93c14b7d456a0e3c5cd` → candidate `f954202206bc3e2ccb941ca9e257c4856d1f0c53`.

### File List

- `.agents/skills/bmad-code-review/steps/step-04-present.md` (modified)
- `.agents/skills/bmad-dev-story/SKILL.md` (modified)
- `.agents/skills/bmad-quick-dev/step-05-present.md` (modified)
- `.agents/skills/bmad-quick-dev/step-oneshot.md` (modified)
- `.claude/skills/bmad-code-review/steps/step-04-present.md` (modified)
- `.claude/skills/bmad-dev-story/SKILL.md` (modified)
- `.claude/skills/bmad-quick-dev/step-05-present.md` (modified)
- `.claude/skills/bmad-quick-dev/step-oneshot.md` (modified)
- `_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md` (modified)
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified)
- `_bmad-output/implementation-artifacts/epic-6-context.md` (modified)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
- `_bmad-output/planning-artifacts/architecture.md` (modified)
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` (modified)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` (new)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-release-owner-decision-ledger-closure.md` (new)
- `_bmad/scripts/generate_story_record.py` (new)
- `_bmad/scripts/tests/test_generate_story_record.py` (new)
- `_bmad/scripts/tests/test_verify_submodule_promotion.py` (modified)
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` (new)
- `docs/release-evidence/conformance-oracle-tiering-decision-v2.md` (new)
- `docs/runbooks/story-final-record-generation.md` (new)
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` (modified)
- `tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs` (new)
- `tests/README.md` (modified)

### Gitlink Promotions

| Path | Declared | Recorded mode | Recorded commit | Baseline commit |
| --- | --- | --- | --- | --- |
| `references/Hexalith.EventStore` | yes | `160000` | `589da8b91bbf443b39f48fbc0aa7ac30286a56d6` | `5a1d277ec0583e304986488d299eb3e6e5022487` |
| `references/Hexalith.Memories` | yes | `160000` | `115d30b59101910d0fd30717f49a5fb7f1782547` | `1868c8f94ca1ec723a30b256a29c7c8495bc8cca` |
| `references/Hexalith.Tenants` | yes | `160000` | `2e61f57bda6379192007d1bc6fabbde61996b11d` | `f9e51c66745557da4f267ab40f32294f2f27fae7` |

### Test Results

| Test project | State | Total | Passed | Failed | Skipped | Artifact SHA-256 |
| --- | --- | --- | --- | --- | --- | --- |
| Conformance | PARSED | 425 | 423 | 2 | 0 | `bdb07ef4cf37dbba` |
| Server | PARSED | 631 | 631 | 0 | 0 | `d8fb713f35570f31` |
| Contracts | PARSED | 618 | 618 | 0 | 0 | `ed986554c12fbd08` |
| Domain | PARSED | 185 | 185 | 0 | 0 | `33959cf744279498` |
| Admin.Web | PARSED | 14 | 14 | 0 | 0 | `f401b4924ae58154` |
| IntegrationTests | PARSED | 14 | 14 | 0 | 0 | `a0c09485fdd69a31` |
| Client | PARSED | 29 | 29 | 0 | 0 | `bc072d9bf3a4f723` |
| AppHost | PARSED | 9 | 8 | 0 | 1 | `503e8208eb83e7e7` |
| **Total (computed)** | **8 parsed** | **1925** | **1922** | **2** | **1** | — |

**This suite is not fully green: 2 failed, 1 skipped.**

### Candidate Binding

- Candidate `f954202206bc3e2ccb941ca9e257c4856d1f0c53` · committed head `f954202206bc3e2ccb941ca9e257c4856d1f0c53` · ancestor of head: **yes**
- Gitlinks moved after the candidate: none

### Promotion Completion Gate

- Result **PASS** · declared: references/Hexalith.EventStore, references/Hexalith.Memories, references/Hexalith.Tenants · changed gitlinks: references/Hexalith.EventStore, references/Hexalith.Memories, references/Hexalith.Tenants · evaluated: references/Hexalith.EventStore, references/Hexalith.Memories, references/Hexalith.Tenants

### Record Diagnostics

- WARNING `UNRELATED_WORKTREE_DIRT` (`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md`): _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-evidence-boundary-validation-pattern.md is dirty in the working tree but absent from the committed range, so it is outside this record's derived scope

<!-- STORY-FINAL-RECORD:END -->

**Fault injection — one mutation per guard, all confirmed able to fail.** Run against the live record
and the live artifacts at candidate `f954202`, not against fixtures. The generator returned `pass`
with zero blockers immediately before the first injection and immediately after the last. Record
SHA-256 `abb4fb60…dc2f`, conformance artifact SHA-256 `bdb07ef4…7c6e`, both re-verified unchanged
after the last restoration.

| Mutation | Target | Result |
| --- | --- | --- |
| `TestResults/6-8-conformance.trx` Counters `passed="423"` → `"422"` | AC2 `TEST_COUNT_INCONSISTENT` | **blocked — TEST_COUNT_INCONSISTENT** |
| `- \`references/Hexalith.EventStore/src/Leaked.cs\`` appended to the record's File List | AC3 `SUBMODULE_INTERNAL_PATH` | **blocked — SUBMODULE_INTERNAL_PATH + FILE_LIST_DRIFT** |
| `--candidate` repointed from `f954202` to baseline `bb5b777` | AC4 `CANDIDATE_NOT_FINAL` | **blocked — CANDIDATE_NOT_FINAL ×3 + FILE_LIST_DRIFT + PROMOTION_GATE_NOT_PASS, embedded `GITLINK_COMMIT_MISMATCH` ×3** |
| `references/Hexalith.EventStore` dropped from `--submodule`, kept in `--require-remote` | AC4 `PROMOTION_GATE_NOT_PASS` | **blocked — PROMOTION_GATE_NOT_PASS, embedded `INVALID_SCOPE`** |
| `TestResults/6-8-conformance.trx` removed | AC2 `TEST_RESULTS_MISSING` | **blocked — TEST_RESULTS_MISSING, project state `NOT_RUN`** |
| `TestResults/6-8-conformance.trx` mtime backdated 86,400 s | AC2 `TEST_RESULTS_STALE` | **blocked — TEST_RESULTS_STALE** |

Artifacts restored byte-identically after each injection (SHA-256-verified before and after, not by
inspection), worktree left clean, and the generator re-verified `pass` with zero blockers afterwards.
The candidate-repoint and dropped-gitlink mutations change arguments rather than files, so no
restoration applies; the repository head and the record hash were confirmed unchanged. Each mutation
is also encoded as a permanent hermetic regression in
`_bmad/scripts/tests/test_generate_story_record.py`, so a future change that silently removes a guard
turns the suite red rather than passing quietly.

The mutation counts scale with the declaration and that is worth reading rather than skimming: with
three declared gitlinks instead of two, the candidate repoint now trips `CANDIDATE_NOT_FINAL` three
times and the embedded checker reports three `GITLINK_COMMIT_MISMATCH` entries. A guard whose output
did **not** change when the declared scope grew would be evaluating the declaration rather than the
repository.

### Boundary Confirmation

**This story authored** the workflow tooling and its documentation only — eighteen of the twenty-five
derived paths: the new generator `_bmad/scripts/generate_story_record.py` and its pytest suite; the
four completion surfaces in both `.claude/skills/` and `.agents/skills/`; `WORKFLOW_GATE_CONTRACTS`
in `_bmad/scripts/tests/test_verify_submodule_promotion.py`, repaired in the same change that broke
it; one new Conformance test file; the new runbook; the `tests/README.md` final-record section; the
deferred-work ledger; this story file; and its own `sprint-status.yaml` line.

**Seven of the twenty-five derived paths were authored by concurrent sessions, not by this story.**
The File List is the git-derived path set across `bb5b777..f954202`; membership in that range is not
a claim of authorship, and the derivation is deliberately not trimmed to flatter the story. The seven
are named individually so nothing is absorbed silently:

| Foreign path | Authored by |
| --- | --- |
| `_bmad-output/implementation-artifacts/epic-6-context.md` | `f954202` — authority v5 correct-course |
| `_bmad-output/planning-artifacts/architecture.md` | `f954202` — authority v5 correct-course |
| `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` | `f954202` — authority v5 correct-course |
| `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-conformance-oracle-tiering.md` | `f954202` — authority v5 correct-course |
| `docs/release-evidence/conformance-oracle-tiering-decision-v2.json` | `f954202` — authority v5 correct-course |
| `docs/release-evidence/conformance-oracle-tiering-decision-v2.md` | `f954202` — authority v5 correct-course |
| `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` | `f954202` — authority v5 correct-course |

`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-28-release-owner-decision-ledger-closure.md`
is an eighth such path, authored by the earlier concurrent commit `e74c09a` and already disclosed in
the `33d2cac` pass.

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
| 2026-07-28 | Implemented T1–T12. Added the final-record generator, its pytest suite with six live fault injections, the C# non-deletability guard, the runbook, and the four gated completion surfaces in both skill trees; repaired the Story 6.7 gate-span coupling in the same change. `submodule_promotions` expanded from `[]` to two inherited root gitlinks under recorded owner approval. Record generated from measured state and inserted verbatim. Status `in-progress` → `review`. |
| 2026-07-28 | Re-anchored the record from candidate `33d2cac` to `f954202` after a concurrent correct-course session moved a declared gitlink past the candidate, tripping AC4 `CANDIDATE_NOT_FINAL`, and re-checked-out two submodule worktrees that are compile inputs under `-p:UseHexalithProjectReferences=true`. Nothing was carried forward: full suite re-run from a clean restore (identical counts, new artifacts), File List re-derived (18 → 25 paths as the concurrent session's work entered the committed range), promotion gate re-embedded, and all six AC8 fault injections re-executed against the new record and artifacts. `submodule_promotions` expanded to a third inherited root gitlink (`references/Hexalith.EventStore`) under recorded owner approval. Restored this story's `sprint-status.yaml` line and completion comment, which the concurrent write had reverted to `in-progress`. No implementation code changed. Status `in-progress` → `review`. |
