---
story_key: '6-8-generate-the-final-story-record-mechanically-from-measured-state'
epic: 6
story_id: '6.8'
created: '2026-07-28'
status: 'ready-for-dev'
baseline_commit: 'bb5b777f9b8e6932b1bae93c14b7d456a0e3c5cd'
# file_list_commit: SET THIS AT COMPLETION to the exact revision the File List was derived from.
#   Story 6.7 review chunk 1 established the rule: a fixed File List compared against a moving
#   `HEAD` makes the story's own suite fail forever on the next legitimate commit. Story 6.2
#   omitted the field, which is why its recorded "49 paths" cannot be reproduced today. This is
#   the same defect AC4 (`CANDIDATE_NOT_FINAL`) generalizes from one story to every record.
submodule_promotions: []
# ^ `[]` is the exact transcription of the approved scope. The v4 authority confines Story 6.8 to
#   `_bmad/scripts/`, `_bmad/scripts/tests/`, the two skill trees, conformance tests, planning
#   artifacts, and documentation, and explicitly prohibits modifying sibling submodule source.
#
#   DO NOT treat `[]` as settled. Story 6.7 declared `[]` on exactly this premise and review pass 2
#   proved the premise false against the shipped candidate: four root gitlinks had advanced between
#   baseline and HEAD. Four of the five most recent commits at this story's baseline touch
#   `references/`. If any root gitlink moves between `bb5b777` and the completion candidate, the
#   gate will surface it as `UNDECLARED_GITLINK_CHANGE` and it becomes affected scope. When that
#   happens: STOP, report it, and obtain Jerome's approval before editing this field. Never expand
#   the declaration silently, and never commit inside a submodule to make the gate pass.
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

Status: ready-for-dev

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

- [ ] **T1 — Generator skeleton and document contract** (AC: 1, 6)
  - [ ] Create `_bmad/scripts/generate_story_record.py`, mode 755, PEP-723 header
        `requires-python = ">=3.11"`, stdlib only, module docstring one line.
  - [ ] Port the sibling's structural spine from `verify_submodule_promotion.py`: `SCHEMA`
        constant, `GateError`, `GateArgumentParser`, `diagnostic()`, `empty_document()`,
        `run_git()` with the `-c` hardening set and `git_environment()` var-popping, `verify()`
        returning a dict, `main(argv) -> int`, `write_output()`. Do not invent a new shape.
  - [ ] Implement the exit-code contract: `0` pass, `1` blocked, `2` error. A parseable document is
        written to stdout on **every** path including exit 2; never print a traceback.
  - [ ] Implement `RECORD_NOT_DERIVED`: the document may not report `pass` when no artifact was
        parsed, no candidate resolved, or no record section replaced.
  - [ ] Add the `--format markdown` renderer path (replaces the sibling's `text`), including the
        `pre_parse_output_format()` trick so a `GateError` raised before `parse_args` still honors
        the requested format.
- [ ] **T2 — Test-count derivation from TRX** (AC: 2)
  - [ ] Parse `/TestRun/ResultSummary/Counters` with namespace-agnostic matching (see **Dev Notes →
        TRX parsing** — a plain `/TestRun/...` path matches nothing).
  - [ ] Map `total`, `executed`, `passed`, `failed`, and `notExecuted` → `skipped`.
  - [ ] Compute totals by summation across declared projects. Never accept a caller-supplied total.
  - [ ] A declared project with no artifact ⇒ per-project state `NOT_RUN` + blocker
        `TEST_RESULTS_MISSING`. An undeclared project with an artifact ⇒ warning
        `TEST_PROJECT_UNDECLARED`.
  - [ ] Staleness: block `TEST_RESULTS_STALE` when an artifact is older than the newest file in the
        derived File List, **excluding the generator's own write targets** (see D3).
  - [ ] Record each artifact's SHA-256 in the document, per the PowerShell precedent.
- [ ] **T3 — File List derivation** (AC: 3)
  - [ ] Derive from `git diff --name-status --no-renames <baseline>..<candidate> --` unioned with
        the tracked working-tree delta and `git ls-files --others --exclude-standard`.
  - [ ] Emit exactly one File List. Self-account for the generator's own output paths.
  - [ ] Block `SUBMODULE_INTERNAL_PATH` for any path under a root-declared submodule prefix.
  - [ ] Emit gitlink entries in a separate labeled promotions section carrying recorded commit and
        mode — never inline in the File List.
  - [ ] Block `FILE_LIST_DRIFT` when the record's existing list disagrees with the derived set.
- [ ] **T4 — Candidate and gitlink binding** (AC: 1, 4)
  - [ ] Resolve mode-`160000` entries by **parsing the mode column** of `git diff --raw --no-abbrev
        -z` and `git ls-tree -z`. Never substring-match `160000`.
  - [ ] Require the candidate to be an ancestor of `HEAD` and no declared gitlink to have moved
        after it ⇒ `CANDIDATE_NOT_FINAL`.
  - [ ] Invoke `verify_submodule_promotion.py` and embed its document verbatim under a dedicated
        key. Branch on its `result` field (`pass`/`blocked`/`error`) **first**, not on
        `blockers[].code` and not on the return code alone — exit 1 and 2 both emit valid JSON.
        Map a non-`pass` result to `PROMOTION_GATE_NOT_PASS`.
  - [ ] Block `BASELINE_NOT_TRUSTWORTHY` on a missing, unresolvable, or non-ancestor baseline.
- [ ] **T5 — Markdown renderer** (AC: 1, 5)
  - [ ] Render the JSON document to the exact block the four surfaces paste in: File List,
        promotions section, per-project counts table, totals, embedded gate summary, candidate
        binding.
  - [ ] The renderer must **name what it derived**. Story 6.7 review finding: a vacuous zero-scope
        PASS was byte-identical to a fully verified promotion, and text was the default format.
  - [ ] State in the rendered output that the JSON is authoritative and the Markdown is rendered
        from it.
- [ ] **T6 — Historical mode** (AC: 7)
  - [ ] `--historical` verifies a closed record read-only. No writes of any kind.
  - [ ] Classify per D4: a record carrying no `story-final-record-v1` block is `pre-generator`; its
        AC3/AC2-shaped findings are reported as warnings, not blockers.
  - [ ] Carry the honest boundary verbatim from the Epic 5 asset: committed bytes, path modes, and
        cross-record claims are verified; a former uncommitted working tree is not reconstructed or
        claimed.
  - [ ] Run it over Stories 6.1, 6.2, and 6.7 and record the results.
- [ ] **T7 — pytest suite and fault injection** (AC: 8)
  - [ ] Create `_bmad/scripts/tests/test_generate_story_record.py` following the house convention:
        PEP-723 header with `dependencies = ["pytest>=8.0"]`, `sys.exit(pytest.main([__file__,
        "-q"]))` guard at the bottom.
  - [ ] Reuse the hermetic fixture pattern from `test_verify_submodule_promotion.py`: `GIT_ENV`
        with the seven `GIT_*` redirect vars popped, deterministic author/committer,
        `GIT_CONFIG_GLOBAL=os.devnull`, `-c init.defaultBranch=main -c commit.gpgsign=false -c
        protocol.file.allow=always`, everything under `tmp_path`.
  - [ ] Run the six AC8 mutations. Each must trip a distinct guard, and each mutated artifact must
        be restored byte-identically (verify with a hash, not by inspection).
  - [ ] Add the decoy tests the sibling proved necessary: a filename containing the literal digits
        `160000`, and a filename containing a backslash.
  - [ ] Record results in the fault-injection table (format in Dev Notes).
- [ ] **T8 — The four workflow surfaces** (AC: 5)
  - [ ] Edit `bmad-dev-story/SKILL.md` step 9: insert the generator invocation before line 428
        (`<action>Update the story Status to: "review"</action>`), mirroring the promotion gate's
        block at lines 416-427 — same blocker handling, same status rollback to `in-progress`, same
        HALT shape, same XML escaping (`&lt;value&gt;`).
  - [ ] Amend the definition-of-done list (lines 430-443): replace the File List bullet with the
        generator-derived wording and add the count-traceability bullet.
  - [ ] Edit `bmad-quick-dev/step-05-present.md` and `step-oneshot.md`. **These two files are not
        identical in this region** — step-05 writes to `{spec_file}` and is followed by `### Mark
        Spec Done and Synchronize`; oneshot writes to "the trace" and is followed by `### Complete
        Trace and Commit Completion Record`. Write two blocks, not one copy-paste.
  - [ ] Edit `bmad-code-review/steps/step-04-present.md` section `### 6. Update story status and
        sync sprint tracking`. The generator runs after patches are applied and before the `done`
        branch at line 99.
  - [ ] **Apply every edit to both `.claude/skills/` and `.agents/skills/`.** They are byte-identical
        and a test enforces it. Four surfaces = eight files.
  - [ ] Read `baseline_commit` **or** `baseline_revision` in every invocation — `bmad-dev-auto` uses
        the second name. Do not rename existing keys.
- [ ] **T9 — Non-deletability guard and the 6.7 coupling** (AC: 6)
  - [ ] **Update `WORKFLOW_GATE_CONTRACTS` in `_bmad/scripts/tests/test_verify_submodule_promotion.py`
        in the same change as T8.** Inserting a section between the promotion gate heading and its
        follower marker *widens* Story 6.7's gate span to swallow the new section, which silently
        weakens its displacement guard. Add the new heading as the follower marker for each affected
        entry. This is not optional and it is not a 6.7 change — it is repairing a coupling this
        story breaks.
  - [ ] Create `tests/Hexalith.Conversations.Conformance.Tests/StoryFinalRecordGenerationValidationTest.cs`
        (see D1), porting the proven four-test shape: contract table → span-bounded body extraction →
        positive contract assertion → three mutation assertions (heading removed / body gutted /
        clause displaced outside the span).
  - [ ] Assert `.claude/skills/` ↔ `.agents/skills/` byte identity for **every** skill path in this
        story's File List, not just the four gated ones.
- [ ] **T10 — Runbook and test documentation** (AC: 5, 7)
  - [ ] Create `docs/runbooks/story-final-record-generation.md`, mirroring
        `docs/runbooks/submodule-promotion-completion-gate.md`: versioned frontmatter, numbered
        sections, a blocker→remediation table matching the code table verbatim, an explicit
        exit-code list, a Safety boundary section, and a Known limitations section.
  - [ ] Rewrite `tests/README.md` §Final-Record Completion Gate around generation. Document `-trx
        <file>` on the xUnit v3 executable and state that `dotnet test --report-trx` is rejected as
        an unknown option. Retain the PowerShell checker as the Epic 5 historical asset — do not
        delete it and do not re-mark Epic 4 action A1.
  - [ ] Add this story as a **third instance** of the standing "nothing executes these gates
        automatically" entry in `_bmad-output/implementation-artifacts/deferred-work.md`. Do not
        resolve it. (That ledger has no IDs and no status field — match the existing bullet shape;
        do not invent a `DW-n` reference.)
- [ ] **T11 — Self-application** (AC: 1, 5, 6)
  - [ ] Story 6.8 completes after Story 6.2, so its own completion record must be produced by the
        generator it builds. Run it against this story and paste the output verbatim. A hand-typed
        6.8 record would falsify the story's own AC5 on delivery.
  - [ ] Set `file_list_commit` in this file's frontmatter at completion.
- [ ] **T12 — Boundary confirmation and final validation**
  - [ ] Full .NET suite with `-p:UseHexalithProjectReferences=true` (see Dev Notes → environment).
  - [ ] `uv run --with pytest pytest _bmad/scripts/tests/ -q`.
  - [ ] `diff -rq .agents/skills .claude/skills` — expect only `Only in .agents/skills: aspire`.
  - [ ] `git diff --check` in **both** forms: bare working-tree and over the committed range.
  - [ ] Write the `### Boundary Confirmation` section naming what this story changed and what it did
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

### Debug Log References

### Completion Notes List

### File List

### Boundary Confirmation

## Change Log

| Date | Change |
| --- | --- |
| 2026-07-28 | Story created from the v4 authority amendment and the approved 2026-07-28 correction proposal. Status `backlog` → `ready-for-dev`. |
