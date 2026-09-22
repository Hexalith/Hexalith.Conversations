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

Sections 1-8 govern the v1 route. The contract-bound v2 route, which Story 7.1
uses to produce `hexalith.conversations.story-final-record.v2` records, is
described in section 9.

## 1. Derivation sources

Exactly four, and no others:

1. Parsed machine-readable test-result artifacts (TRX).
2. The git-derived path set between the work baseline and the committed
   candidate. Source-tree dirt outside the two record outputs and declared TRX
   inputs blocks rather than being mixed into a commit-bound record.
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

Complete every executable, test, and documentation change first, commit every
story-owned source path, and require the remaining source tree clean. Then run
the tests and generate the record. An artifact older than the newest file the
record binds to blocks as `TEST_RESULTS_STALE`, because its counts describe an
earlier tree.

The generator's own write targets — the story/spec record and the sprint-tracking
file — and declared TRX evidence inputs are allowed to remain uncommitted. The
record outputs and the TRX artifacts themselves are excluded from the freshness
comparison. Without the output exclusion every correct re-run would report itself
stale, since the record is written into a file that is itself in the derived file
list. Every ordinary committed path remains in the comparison; timestamps are
compared at nanosecond precision and a genuinely stale artifact still blocks.

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

The required project set is derived from root-owned projects under `tests/` in
the single root `.slnx`; projects beneath root submodules are excluded. Each TRX
must identify the matching full project assembly, and declarations must match
that set exactly without duplicate names or reused artifacts. Totals are computed
by summation. A caller-supplied total is never accepted, and a zero-test artifact
measures nothing.

Every failed test blocks completion. A skipped test blocks unless its exact test
identity and a non-empty reason appear under the record's versioned
`allowed_skipped_tests` frontmatter policy. Unused allowances are reported so
stale exceptions remain visible.

Before running those tests, clean-rebuild the committed candidate with
`-t:Rebuild -p:SourceRevisionId=<candidate>`. Each TRX must identify exactly one
repository-contained test binary. The generator reads that binary, requires its
managed `AssemblyInformationalVersion` to embed the exact candidate as
`SourceRevisionId`, verifies that the TRX is not older than the binary, and emits
a persisted build-manifest table with the binary's full SHA-256. A fresh TRX
against a stale `--no-build` output therefore blocks instead of inheriting the
candidate by timestamp.

## 4. Run the generator

```bash
python3 _bmad/scripts/generate_story_record.py \
  --repository <root> \
  --story <story-or-spec-record> \
  --baseline <story-baseline-commit> \
  --candidate <committed-umbrella-revision> \
  --test-results Hexalith.Conversations.Conformance.Tests=<path>.trx \
  --test-results Hexalith.Conversations.Server.Tests=<path>.trx \
  --submodule references/Hexalith.EventStore \
  --require-remote references/Hexalith.EventStore \
  --format bundle
```

`--test-results` takes `FULL_PROJECT_NAME=PATH`, repeated once per root-owned test
project in the root solution, with a repository-relative artifact path. A required
project with no artifact is recorded as `NOT_RUN` and blocks; it is never silently
omitted, relabelled, or carried forward from an earlier pass.

The bundle contains one authoritative `document`, its exact rendered `markdown`,
and `markdown_sha256`. Insert that Markdown verbatim, then run
`--verify-record-sha256 <markdown_sha256> --format json`. Completion cannot advance
until this second mode confirms that the bytes in the record match the passing
bundle.

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
| `TEST_PROJECT_SCOPE_MISMATCH` | Declarations omit, duplicate, relabel, reuse, or add a project outside the root solution's root-owned test set | Declare exactly one matching artifact per authoritative project. |
| `TEST_RESULTS_EMPTY` | A parsed artifact contains zero tests | Run the project without an empty filter and emit a non-vacuous artifact. |
| `TEST_RESULTS_FAILED` | One or more tests failed | Fix the failures and emit a new artifact. |
| `TEST_SKIP_NOT_ALLOWED` | A skipped test has no exact versioned identity/reason allowance | Run it or add an approved, reasoned policy entry. |
| `TEST_BUILD_NOT_BOUND` | A TRX omits or reuses its test binary, the binary is outside the repository, its embedded `SourceRevisionId` differs from the candidate, or the TRX predates it | Clean-rebuild the committed candidate with its exact `SourceRevisionId`, then rerun every test and regenerate the record. |
| `FILE_LIST_DRIFT` | The record's list disagrees with the derived set, or the record carries more than one list | Replace the record's File List with the generated one. Never hand-edit either side into agreement. |
| `SUBMODULE_INTERNAL_PATH` | A path under a root-declared submodule appears in the record's File List | Remove it: it belongs to that repository's own record, and the gitlink belongs in the promotions section. |
| `CANDIDATE_NOT_FINAL` | The candidate is not an ancestor of HEAD, a non-output path changed after it, or any gitlink moved | Re-run against the committed head; only the story and sprint-status output commits may follow it. |
| `PROMOTION_GATE_NOT_PASS` | The embedded Story 6.7 checker document reports a result other than `pass` | Remediate the embedded checker's own blockers per `submodule-promotion-completion-gate.md`. |
| `BASELINE_NOT_TRUSTWORTHY` | The baseline is missing, `NO_VCS`, unresolvable, or not an ancestor of the candidate | Record a resolvable `baseline_commit` that is an ancestor of the candidate. |
| `RECORD_NOT_DERIVED` | No artifact was parsed, no candidate resolved, or no replaceable record section found | Supply the missing input. A run that derived nothing proves nothing and can never be read as a pass. |
| `RECORD_CONTENT_DRIFT` | The inserted block differs from its bundle digest, or a generated historical section is malformed | Insert the bundle Markdown verbatim and verify it before completion. |
| `WORKTREE_NOT_CLEAN` | Source-tree dirt remains outside record outputs and declared TRX artifacts | Commit story-owned work or remove unrelated dirt before measurement. |

| Warning | Condition |
| --- | --- |
| `UNUSED_TEST_SKIP_ALLOWANCE` | A versioned skipped-test exception was not exercised by the measured run |

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

## 6. Insert and verify the record verbatim

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

After insertion, pass the bundle's `markdown_sha256` to
`--verify-record-sha256`. This mode performs no test or Git remeasurement; it
proves that the final block is byte-identical to the one measurement bundle that
passed. An absent, duplicated, truncated, or edited marker span blocks as
`RECORD_CONTENT_DRIFT`.

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

## 9. Contract-bound v2 route

Sections 1-8 describe the v1 route. Passing the exact `--contract` option
selects an isolated v2 route instead. The generator dispatches on that exact
token before the legacy parser runs; an abbreviation such as `--contr` still
reaches the v1 parser, and every v1 invocation, document, and exit code is
unchanged.

```bash
uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py \
  --repository . \
  --contract _bmad-output/planning-artifacts/v9/story-contracts/7.1.json \
  --format bundle \
  --output-json docs/release-evidence/story-7.1-final-record-v2.json \
  --output-markdown docs/release-evidence/story-7.1-final-record-v2.md
```

### Accepted input

The route accepts exactly `--repository`, `--contract`, `--format`,
`--output-json`, and `--output-markdown`, each at most once, with no
prefix abbreviation. `--format` supports only `bundle`. The two output paths
must equal the contract's `finalRecord.paths`, because output paths are contract
facts rather than caller choices. Any option that would supply a count, path,
commit, gitlink, digest, exit, ledger, or verdict, such as `--candidate`,
`--passed`, `--summary`, `--test-results`, `--changed-path`, `--gitlink`, or
`--result`, is refused by name as `CALLER_AUTHORED_FACT` before anything is
derived.

The route requires the pinned `jsonschema` environment. It validates against
the tooling's own schema copies in `_bmad/schemas/`, never the evaluated
repository's: `story-final-record-v2.schema.json`,
`story-record-generator-failure-v1.schema.json`,
`v9-story-contract-v1.schema.json`, and `v9-authority-bundle-v1.schema.json`.

### Derivation

Every fact comes from Git objects of the committed candidate or from measured
JUnit result files:

1. **Candidate.** `HEAD` resolved to a commit. There is no `--candidate`
   option. The working tree must equal the candidate everywhere except for the
   two declared outputs and the declared JUnit result paths, detected without
   traversing submodules.
2. **Contract.** Read from the candidate blob at `--contract`, parsed as strict
   UTF-8 JSON (duplicate keys and non-finite numbers are rejected), required to
   carry `hexalith.conversations.story-contract.v1`, and validated against the
   closed story-contract schema. Scenario IDs must be unique and belong to the
   story, and `finalRecord.summary` must require and pass every scenario.
3. **Authority.** Epic, architecture, and planning candidate come from the
   contract. `bundleDigest` is recomputed from the candidate's
   `_bmad-output/planning-artifacts/v9-authority-bundle-v1.json` as the
   SHA-256 of one `<sha256>  <path>` LF line per ordinally sorted artifact
   row. The bundle's planning candidate must equal the contract's.
4. **Gitlinks.** Raw mode-`160000` entries of the candidate tree, sorted
   ordinally, must equal the candidate's root `.gitmodules` path set exactly.
   The final-record schema then pins the ten frozen paths in order.
5. **Scenarios.** Each pytest scenario command must have the shape
   `python3 -m pytest -q TARGET -k SELECTOR --junitxml=PATH`. The target must
   be committed at the candidate, and the JUnit path must lie outside every
   gitlink. The final scenario must be this generator's own invocation, with
   the same contract, format, and output paths.
6. **JUnit ledgers.** A result file must contain one `testsuites` root with
   exactly one direct `testsuite`, no DTD or entity declaration, and suite
   counters that equal its direct testcases. Each direct testcase becomes one
   ledger row: `<scenarioId>#<four-digit ordinal>`, subject
   `classname::name`, and `PASS` only when it has no direct `failure`, `error`,
   or `skipped` child. Every testcase must belong to the command's target
   module and contain its simple `-k` selector. A result file that predates the
   candidate's commit time is stale.
7. **Exit.** Each pytest scenario's exit is derived the way pytest reports
   it: `5` for no testcase, `1` for any failure or error, and `0` otherwise. A
   scenario passes only with a declared passing exit, a nonempty ledger, no
   skip, and no blocker. The self-invocation scenario carries the generator's
   own ordered assertion ledger.
8. **Summary.** Counted from the derived scenario results, and required to
   equal the contract's `finalRecord.summary` (`6/6/0/0/0/0` for Story 7.1).
   `faultInjection.results` stays empty, because no fault result is a measured
   input of this route.

### Outputs and digests

The JSON record is authoritative. It is rendered as `indent=2`, UTF-8, LF, with
a terminal newline. The Markdown is a deterministic projection of the same
record.

- **JSON content digest (self-excluding).** SHA-256 of the canonical JSON with
  `outputs.json.sha256`, `outputs.markdown.sha256`, and
  `renderedMarkdownSha256` all set to 64 zeros. It is stored in
  `outputs.json.sha256` and printed in the Markdown.
- **Markdown digest.** SHA-256 of the exact Markdown bytes. It is stored in both
  `outputs.markdown.sha256` and `renderedMarkdownSha256`.

Before anything is written, the route renders the pair twice and requires
identical bytes, validates the record against the final-record schema, and
re-derives every digest binding. It then replaces both outputs atomically,
restoring the first if the second replacement fails. Outputs are written only
on `PASS`; every other result leaves them unchanged. On `PASS`, stdout carries
the exact JSON record bytes. Otherwise stdout carries one
`hexalith.conversations.story-record-generator-failure.v1` document with stable
codes and generator-authored diagnostics. It never contains a traceback, a
partial record, or caller payload.

### Exit semantics

- Exit `0`: `PASS`. Both outputs were written, and stdout is the JSON record.
- Exit `1`: `FAIL`. A proven defect in the input or evidence was found, and the
  outputs are unchanged.
- Exit `2`: `BLOCKED`. The environment cannot support a trustworthy record, and
  the outputs are unchanged.

| Blocker | Exit | Condition |
| --- | --- | --- |
| `ARGUMENT_INVALID` | `1` | An unknown, repeated, valueless, empty, positional, or unsupported argument; a missing required option; a non-root `--repository`; or a contract path that is not a committed regular file |
| `CALLER_AUTHORED_FACT` | `1` | A fact-bearing option was supplied, or the output paths differ from `finalRecord.paths` |
| `INPUT_SCHEMA_INVALID` | `1` | The contract is malformed JSON, has an unknown schema identity, or violates its schema; or a result file is malformed, escapes the repository, or is not a single-suite JUnit document |
| `RECORD_NOT_DERIVED` | `1` | No committed candidate, no parsed result file, no raw gitlink path, or no derived assertion |
| `ASSERTION_LEDGER_EMPTY` | `1` | A scenario, or the whole run, yields no executed testcase |
| `AUTHORITY_BINDING_INVALID` | `1` | The V9 bundle is missing, malformed, unsorted, digest-drifted, or bound to another planning candidate |
| `GITLINK_INVENTORY_DRIFT` | `1` | Raw gitlinks and root `.gitmodules` differ, or `.gitmodules` declares a path outside `references/` |
| `WORKTREE_NOT_CLEAN` | `1` | The working tree differs from the candidate outside the declared outputs and results |
| `SCENARIO_COMMAND_UNSUPPORTED` | `1` | A scenario command has an unsupported shape, its target is not committed, or the self-invocation is missing or not final |
| `SCENARIO_RESULT_MISMATCH` | `1` | Testcases do not belong to the scenario's target and selector, a testcase identity repeats, two scenarios share a result path, or this invocation is not the declared self-invocation |
| `TEST_RESULTS_MISSING` | `1` | A declared result file does not exist, so the scenario was not run |
| `TEST_RESULTS_STALE` | `1` | A result file predates the candidate commit |
| `TEST_RESULTS_FAILED` | `1` | A testcase failed or errored, or the derived exit is not a declared passing exit |
| `TEST_SKIP_NOT_ALLOWED` | `1` | A testcase was skipped; the v2 route allows no skip |
| `TEST_COUNT_INCONSISTENT` | `1` | Suite counters disagree with the testcases the suite contains |
| `OUTPUT_PATH_INVALID` | `1` | An output lies below a gitlink, aliases an input, escapes the repository, or names a symlink or non-file |
| `OUTPUT_SCHEMA_INVALID` | `1` | The derived record violates the final-record schema |
| `RECORD_CONTENT_DRIFT` | `1` | Two renderings differ, a digest binding does not re-derive, or the installed bytes differ from the generated bytes |
| `GIT_UNAVAILABLE` | `2` | Git is not on `PATH` |
| `GIT_COMMAND_FAILED` | `2` | A Git command failed while deriving the record |
| `SCHEMA_UNAVAILABLE` | `2` | A tooling schema is missing, unreadable, or invalid |
| `SCHEMA_VALIDATOR_UNAVAILABLE` | `2` | `jsonschema` is not installed; use the pinned `uv run --frozen --no-sync` environment |
| `OUTPUT_WRITE_FAILED` | `2` | The outputs could not be written |
| `INTERNAL_ERROR` | `2` | An unexpected generator error occurred; only its exception type is reported |

Remediate the named condition and rerun the same command. Never hand-edit a
result file, a count, a path, or a digest into agreement.

### Operator procedure

1. Commit the story candidate: every executable, test, schema, and
   documentation change. `HEAD` is the candidate, and the working tree must be
   clean.
2. From the repository root, run each `scenarios[0]` through `scenarios[4]`
   `.command` from the story contract through `uv run --frozen --no-sync`, for
   example `uv run --frozen --no-sync python3 -m pytest -q ... --junitxml=...`.
   That pinned environment supplies `jsonschema`. Each command must exit `0`.
3. Leave the JUnit results uncommitted. They are written below the gitignored
   `artifacts/` directory and are read from there, not from Git.
4. Run the final generator scenario, the command at the top of this section,
   through `uv run --frozen --no-sync`. It must exit `0`. On any other exit,
   remediate and restart from step 1 if the candidate changed, otherwise from
   step 2.
5. Confirm that the two declared outputs are the only dirt, then commit exactly
   those outputs in a separate commit that follows the candidate.

## Ordered checklist (copy per story)

1. [ ] Every executable, test, and documentation change complete and saved.
2. [ ] Scoped commit created; committed `HEAD` resolved as the candidate.
3. [ ] Baseline read from frontmatter `baseline_commit` (or `baseline_revision`) and confirmed resolvable and an ancestor.
4. [ ] Candidate clean-rebuilt with `-t:Rebuild -p:SourceRevisionId=<candidate>` and every declared test project run against that build, each emitting its own TRX artifact.
5. [ ] Generator run once with `--format bundle`; nested `document.result` is `pass`, every `derived` field is true, and the build-manifest project set equals the test-result project set.
6. [ ] Every rendered TRX and test-binary digest is a full 64-hex SHA-256, and every build-manifest source revision equals the candidate.
7. [ ] Bundle field `markdown` inserted verbatim and unedited; `markdown_sha256` retained from that same bundle.
8. [ ] Frontmatter `file_list_commit` set to the immutable candidate revision the bundle measured.
9. [ ] Inserted block verified with `--verify-record-sha256 <markdown_sha256> --format json`; exit `0`, result `pass`.
10. [ ] Sprint-status comment references the generated record without restating any count, path total, promotion total, or commit.
11. [ ] No count, path, or commit anywhere in completion narrative was typed by hand.
