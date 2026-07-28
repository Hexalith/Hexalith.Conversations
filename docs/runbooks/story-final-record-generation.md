---
title: Story Final-Record Generation
version: 1
status: active
effective_date: 2026-07-28
---

# Story Final-Record Generation

This operational runbook governs how a story or spec completion record is produced.
It is a live workflow document. The historical Epic 5 asset at
`tests/Test-StoryFinalRecord.ps1` remains the record of that epic's final-record
check and is not superseded byte-for-byte by this runbook; the logic it proved is
ported here, its Epic 5 bindings are not.

The rule this gate exists to enforce: **a completion record may not contain a
count, path, or commit that nobody measured.** Every field is derived by
`_bmad/scripts/generate_story_record.py` from repository state. Nothing in the
record is caller-authored text.

## 1. Derivation sources

Exactly four, and no others:

1. Parsed machine-readable test-result artifacts (TRX).
2. The git-derived path set between the work baseline and the committed
   candidate, unioned with the tracked working-tree delta.
3. Mode-`160000` root gitlink entries resolved from the committed candidate.
4. The Story 6.7 promotion-checker document, embedded verbatim.

A record that could not derive any of them reports a blocker rather than a pass.

### Why the generator lives in `_bmad/scripts/`

`_bmad-output/planning-artifacts/architecture.md` declares its target directory
tree authoritative for the **.NET module**; that tree contains no `_bmad/`,
`_bmad-output/`, or scripts directory at all, because it does not describe the
workflow tooling. The placement follows the approved correction proposal
(`sprint-change-proposal-2026-07-28.md:361`, `:488`) and the established
precedent of `verify_submodule_promotion.py`, `memlog.py`, `resolve_config.py`,
and `resolve_customization.py` already living there. It is not a structure
violation.

## 2. Finalize the tree before you measure

Complete every executable, test, and documentation change first. Then run the
tests, then generate the record. An artifact older than the newest file the
record binds to blocks as `TEST_RESULTS_STALE`, because its counts describe an
earlier tree.

The generator's own write targets — the story/spec record and the sprint-tracking
file — are excluded from that comparison. Without the exclusion every correct
re-run would report itself stale, since the record is written into a file that is
itself in the derived file list. The exclusion covers those two paths and nothing
else; a genuinely stale artifact still blocks.

## 3. Emit machine-readable test results

Run each declared test project and capture TRX. On this repository's xUnit v3 /
Microsoft.Testing.Platform lane, TRX is emitted by the built executable:

```bash
tests/<Project>/bin/Release/net10.0/<Project> -noLogo -trx <absolute-path>.trx
```

`dotnet test --report-trx` is rejected as an unknown option on this lane. See
`tests/README.md` § VSTest Socket Fallback for when the executable path is the
approved route.

Counts are read from `/TestRun/ResultSummary/Counters` — namespace-agnostically,
because TRX carries the `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`
namespace and a literal `/TestRun/...` XPath matches nothing. `skipped` comes from
the `notExecuted` attribute; there is no `skipped` attribute. The generator also
recomputes the counts from the `<UnitTestResult>` outcomes and blocks when the
artifact's own summary disagrees with the results it contains.

Totals are computed by summation across declared projects. A caller-supplied total
is never accepted.

## 4. Run the generator

```bash
python3 _bmad/scripts/generate_story_record.py \
  --repository <root> \
  --story <story-or-spec-record> \
  --baseline <story-baseline-commit> \
  --candidate <committed-umbrella-revision> \
  --test-results Conformance=<path>.trx \
  --test-results Server=<path>.trx \
  --submodule references/Hexalith.EventStore \
  --require-remote references/Hexalith.EventStore \
  --format json
```

`--test-results` takes `NAME=PATH`, repeated once per declared test project, with a
repository-relative artifact path. A declared project with no artifact is recorded
as `NOT_RUN` and blocks; it is never silently omitted and its count is never
carried forward from an earlier pass.

Re-run with `--format markdown` to obtain the block the completion surfaces insert
verbatim. The JSON document is authoritative; the Markdown is rendered from it.

## 5. Interpret the result

- Exit `0`: the record was derived and every guard passed.
- Exit `1`: the invocation was valid, but completion blockers remain.
- Exit `2`: the invocation or repository state cannot support a trustworthy record.

A parseable document is written to stdout on every path, including exit `2`.

| Blocker | Condition | Remediation |
| --- | --- | --- |
| `TEST_RESULTS_MISSING` | A declared project has no artifact, or its artifact yields no counters | Run the project and pass the artifact it emitted. Never carry a count forward. |
| `TEST_RESULTS_STALE` | An artifact predates the newest file in the derived list, excluding the generator's own write targets | Re-run the tests after the last file change. |
| `TEST_COUNT_INCONSISTENT` | An artifact's summary disagrees with the results it contains, or with TRX arithmetic | Re-run the project and pass the artifact it emitted; never edit an artifact. |
| `FILE_LIST_DRIFT` | The record's list disagrees with the derived set, or the record carries more than one list | Replace the record's File List with the generated one. Never hand-edit either side into agreement. |
| `SUBMODULE_INTERNAL_PATH` | A path under a root-declared submodule appears in the record's File List | Remove it: it belongs to that repository's own record, and the gitlink belongs in the promotions section. |
| `CANDIDATE_NOT_FINAL` | The candidate is not an ancestor of the committed head, or an affected gitlink moved after it | Re-run against the committed head, or restore the gitlink that moved. |
| `PROMOTION_GATE_NOT_PASS` | The embedded Story 6.7 checker document reports a result other than `pass` | Remediate the embedded checker's own blockers per `submodule-promotion-completion-gate.md`. |
| `BASELINE_NOT_TRUSTWORTHY` | The baseline is missing, `NO_VCS`, unresolvable, or not an ancestor of the candidate | Record a resolvable `baseline_commit` that is an ancestor of the candidate. |
| `RECORD_NOT_DERIVED` | No artifact was parsed, no candidate resolved, or no replaceable record section found | Supply the missing input. A run that derived nothing proves nothing and can never be read as a pass. |

| Warning | Condition |
| --- | --- |
| `UNRELATED_WORKTREE_DIRT` | Dirty tracked or untracked state outside the derived scope |
| `TEST_PROJECT_UNDECLARED` | A result artifact exists beside a declared one for a project the record does not declare |

`NOT_RUN` is a per-project **state**, not a diagnostic code.

Exit `2` carries an error code rather than a completion blocker: `INVALID_SCOPE`
(bad invocation, missing or unreadable story record, malformed `--test-results`),
`GIT_UNAVAILABLE`, `NOT_A_GIT_REPOSITORY`, `GIT_COMMAND_FAILED`,
`CANDIDATE_UNRESOLVABLE`, `PROMOTION_CHECKER_UNAVAILABLE`, and `INTERNAL_ERROR`.
Correct the reported condition before relying on the result. Never reinterpret an
error as a pass.

The blocker and warning code strings are defined by
`sprint-change-proposal-2026-07-28.md:423-427`, not by the frozen Epic 6 overlay,
which names blocking conditions only and enumerates no code strings.

## 6. Insert the record verbatim

The Markdown renderer emits one contiguous block delimited by
`<!-- STORY-FINAL-RECORD:BEGIN -->` and `<!-- STORY-FINAL-RECORD:END -->`.

- In a story record, it replaces everything between `### File List` and
  `### Boundary Confirmation`.
- In a quick-dev spec, it is appended under `## Verification`.
- On any later run — including the code-review surface, which regenerates after
  patches are applied — it replaces the previous block between its own markers.

Do not edit the inserted text. Gitlink promotions appear in their own labelled
`### Gitlink Promotions` section with recorded commit and mode; they never appear
as File List entries.

Set frontmatter `file_list_commit` to the revision the block was derived from. A
fixed File List compared against a moving `HEAD` makes the record's own suite fail
on the next legitimate commit, and a record with no `file_list_commit` cannot be
re-derived at all.

## 7. Workflow behavior on failure

The four completion surfaces generate rather than author:

- `bmad-dev-story` step 9, `bmad-quick-dev/step-05-present.md`,
  `bmad-quick-dev/step-oneshot.md`, and `bmad-code-review/steps/step-04-present.md`.

Each keeps or returns story and sprint state to `in-progress` and cannot write
`done` while the gate fails. Preserve the stable codes and remediation text in the
workflow record, resolve the named state, and rerun the same command. Never
hand-edit a count, path, or commit into agreement with the record as remediation.

## 8. Historical mode

`--historical --story <closed-record>` verifies an already-closed record
**read-only**. It performs no writes of any kind and does not run the promotion
checker, which inspects live submodule worktrees and would therefore claim to
reconstruct a former working tree.

Records are classified by their own shape, never by a hard-coded story table. A
record carrying no `story-final-record-v1` block is `pre-generator`: its AC2- and
AC3-shaped findings are reported as **warnings**, not blockers, because the
prohibitions forbid rewriting closed records and the approved disposition table
authorises those records to close exactly as they are. A record that does carry
the block is held to the full contract.

### Safety boundary

Committed bytes, path modes, and cross-record claims are verified. **A former
uncommitted working tree is not reconstructed and is not claimed.**

Discovery is root-only. The generator never initializes, updates, fetches, enters,
or traverses a submodule, and never uses a recursive submodule command. It never
commits, adds, checks out, resets, pushes, or otherwise mutates repository state —
in either mode.

### Known limitations

- Nothing runs this gate automatically. There is no CI workflow or hook in this
  repository, so the gate is only as strong as the workflow prose that invokes it,
  plus `StoryFinalRecordGenerationValidationTest` which proves that prose is still
  present. Tracked in `_bmad-output/implementation-artifacts/deferred-work.md`
  alongside the same condition for the planning and promotion gates.
- `bmad-dev-auto/step-04-review.md` writes frontmatter `status: done` behind a
  promotion gate but is **not** one of the four surfaces the frozen authority
  names, so it is not gated by this generator. That is a known bypass route to
  `done` without a generated record, recorded in `deferred-work.md` rather than
  closed by widening frozen acceptance scope.
- Staleness is an mtime comparison. A checkout or file copy that rewrites mtimes
  without changing content can produce a false `TEST_RESULTS_STALE`; re-running the
  tests is always a valid remediation and never a way to hide a real staleness.
- An artifact that exists but cannot be parsed is reported as
  `TEST_RESULTS_MISSING` with the parse failure in its message, because a project
  whose artifact yields no counters is not-run in the only sense a record can
  honestly claim.

## Ordered checklist (copy per story)

1. [ ] Every executable, test, and documentation change complete and saved.
2. [ ] Scoped commit created; committed `HEAD` resolved as the candidate.
3. [ ] Baseline read from frontmatter `baseline_commit` (or `baseline_revision`) and confirmed resolvable and an ancestor.
4. [ ] Every declared test project run, each emitting its own TRX artifact.
5. [ ] Generator run with `--format json`; exit `0` and `result: pass`.
6. [ ] `derived` reports `test_results`, `candidate`, and `record_section` all true.
7. [ ] Generator re-run with `--format markdown`; block inserted verbatim, unedited.
8. [ ] Frontmatter `file_list_commit` set to the revision the block was derived from.
9. [ ] Sprint-status comment quotes the generator's derived totals, not an earlier pass.
10. [ ] No count, path, or commit anywhere in the record was typed by hand.
