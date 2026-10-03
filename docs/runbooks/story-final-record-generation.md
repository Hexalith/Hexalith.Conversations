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
described in section 9. Section 9 also describes how Story 7.3 makes every
governed completion route run that v2 route before `review` or `done`.

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

Story 7.3 rebinds these frozen surfaces one-for-one to the current routes and adds
the contract-bound v2 gate to each of them. See section 9, "Story 7.3
completion-route integration".

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
  closed by widening frozen acceptance scope. `bmad-dev-auto` is retired: Story
  7.3 governs its replacement, `bmad-build-auto/step-04-review.md`, for every
  story that has a v9 story contract (section 9).
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
prefix abbreviation. The separate `--verify-inserted-record` mode, described
under Story 7.3 below, accepts exactly `--repository`, `--contract`, and
`--verify-inserted-record`. `--format` supports only `bundle`. The two output paths
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
Contracts that declare acceptance-result scenarios additionally use
`v9-acceptance-result-v1.schema.json`.

### Derivation

Every fact comes from Git objects of the committed candidate or from measured
JUnit or acceptance-result evidence files:

1. **Candidate.** `HEAD` resolved to a commit. There is no `--candidate`
   option. The working tree must equal the candidate everywhere except for the
   two declared outputs and the declared evidence-result paths, detected without
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
   `python3 -m pytest -q TARGET -k SELECTOR --junitxml=PATH`. An
   acceptance-result scenario, used by Story 7.3, is described below. The target must
   be committed at the candidate, and the JUnit path must lie outside every
   gitlink. The final scenario must be this generator's own invocation, with
   the same contract, format, and output paths.
6. **Evidence ledgers.** A JUnit result file must contain one `testsuites` root with
   exactly one direct `testsuite`, no DTD or entity declaration, and suite
   counters that equal its direct testcases. Each direct testcase becomes one
   ledger row: `<scenarioId>#<four-digit ordinal>`, subject
   `classname::name`, and `PASS` only when it has no direct `failure`, `error`,
   or `skipped` child. Every testcase must belong to the command's target
   module and contain its simple `-k` selector. A result file that predates the
   candidate's commit time is stale. An acceptance result must validate as
   `hexalith.conversations.acceptance-result.v1`, bind the contract's story,
   scenario, exact command, and output path, and carry an ordered ledger whose
   IDs are exactly `<scenarioId>#<four-digit ordinal>`. A lifecycle-only
   successor commit is an accepted acceptance-result stamp: the result may name
   the derived candidate or a commit on the verified lifecycle-only path from
   that candidate to `HEAD`, while its input digests are still checked against
   the retained candidate's blobs. Its input paths and digests must bind the
   committed workflow surfaces required by the contract; duplicate ledger
   subjects are rejected.
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

### Story 7.2 measured inputs

Story 7.2 uses the same strict v2 command envelope with
`_bmad-output/planning-artifacts/v9/story-contracts/7.2.json` and the declared
`docs/release-evidence/story-7.2-final-record-v2.{json,md}` outputs. Run each
root-owned test project from the committed `Hexalith.Conversations.slnx`
separately and retain its TRX at
`artifacts/v9/7.2/test-results/<project-name>.trx`. The generator reads these
files directly, recomputes their counters from direct `UnitTestResult` rows,
applies the committed spec's skip policy, and sums all eight projects. Each TRX
must be newer than the newest bound source input, describe its named assembly,
and bind every result ID to that assembly's test definitions. Its executed
count must cover passing and failed result rows. The artifacts are ignored build
evidence; they are never caller supplied CLI facts.

The optional `measurements` record section is emitted for Story 7.2 only. It
contains the committed spec's ancestor baseline, the exact normalized
baseline-to-candidate root-owned changed paths, per-project result digests and
counts, summed counts, and the SHA-256 of the committed, independently verified
Story 7.1 JSON record. Gitlinks are bound separately by raw mode `160000` tree
entries and must match the root `.gitmodules` inventory. Paths beneath a
gitlink are never root-owned changed paths. Story 7.1's existing record bytes
and v1 behavior are unchanged.

After the ten contract selectors pass through pinned `uv`, run the declared
self-invocation:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py \
  --repository . \
  --contract _bmad-output/planning-artifacts/v9/story-contracts/7.2.json \
  --format bundle \
  --output-json docs/release-evidence/story-7.2-final-record-v2.json \
  --output-markdown docs/release-evidence/story-7.2-final-record-v2.md
```

The Story 7.2 result classes remain exit `0`/`PASS`, exit `1`/`FAIL`, and exit
`2`/`BLOCKED`; an environmental inability never counts as a passing scenario.
Specific fail codes are `TEST_RESULTS_MISSING`, `TEST_RESULTS_STALE`,
`TEST_FAILED`, `TEST_SKIPPED`, `TEST_NOT_RUN`, `SOURCE_TREE_DIRTY`,
`FILE_LIST_DRIFT`, `SUBMODULE_INTERNAL_PATH`, `GITLINK_SCOPE_MISMATCH`,
`GITLINK_DRIFT`, `BASELINE_NOT_TRUSTWORTHY`, and `CANDIDATE_NOT_FINAL`.
Rebuild or rerun a missing, stale, failed, skipped, or zero-run project; commit
source before generating; restore exact root gitlink scope and candidate state;
and regenerate both outputs from the same current candidate after correcting
the named condition. Do not edit counts, paths, or digests in the pair.

A committed Story 7.2 pair pins its candidate. An uncommitted pair does not pin
it. Committing only the pair is a record-only successor: a rerun keeps the
original candidate and reproduces the same bytes. Later commits may change only
the Story 7.2 spec's frontmatter `status` value, the corresponding Story 7.2
row in `sprint-status.yaml`, that file's `last_updated` date, and its
`# last_updated:` header comment date as lifecycle bookkeeping; a rerun still
reads measurements from the pinned candidate and reproduces the pair.
Their later working-tree mtimes do not make the candidate's test results stale.
Any other later commit, including a root gitlink bump or a history
rewrite that orphans the recorded candidate, makes every rerun stop with
`CANDIDATE_NOT_FINAL` while the old pair is present. To recover, retract the
superseded pair in its own commit (`git rm` both outputs), rebuild and rerun
all eight root test projects and the ten contract selectors against that new
`HEAD`, then regenerate and commit the new pair as a record-only successor.
Never restore the superseded pair; its candidate no longer describes the tree.

That recovery changes the recorded candidate and requires a separately authorized
successor task. It is not part of the 2026-09-27 completion repair: that repair
preserves the committed pair and its result artifacts, keeps the later EventStore
gitlink update, and reports `CANDIDATE_NOT_FINAL` at the current candidate without
retracting or regenerating historical evidence.

On 2026-09-29 the repository owner's waive-and-complete decision, recorded in
the Story 7.2 spec, authorizes this recovery for Story 7.2's completion. Here
the retraction of the pair pinned to `170ac9d2e8afa686e4203c44ad5bef9414d19a89`
does not get its own commit: it shares the completion candidate commit with that
decision's runbook and spec changes. Once both outputs are absent at `HEAD`, the
generator treats `HEAD` as the candidate. Before overwriting any result, archive
the existing results; the command refuses an existing destination:

```bash
test ! -e artifacts/v9/7.2-superseded-170ac9d && cp -a artifacts/v9/7.2 artifacts/v9/7.2-superseded-170ac9d
```

Then regenerate the pair from that candidate as a record-only successor. After
regeneration, reproducing the `170ac9d` pair copies its results from the local,
gitignored `artifacts/v9/7.2-superseded-170ac9d` archive instead of
`artifacts/v9/7.2`; they exist nowhere else.

| Story 7.2 blocker | Exit | Condition |
| --- | --- | --- |
| `TEST_RESULTS_MISSING` | `1` | A root project TRX is absent, unreadable, names another assembly, or contains result IDs outside that assembly's definitions |
| `TEST_RESULTS_STALE` | `1` | A root project TRX predates the newest bound source input |
| `TEST_FAILED` | `1` | A root project TRX reports a failed test, has fewer executed tests than passing and failed result rows, or has counters that disagree with its result rows |
| `TEST_SKIPPED` | `1` | A root project TRX reports a skip that the committed spec's `allowed_skipped_tests` does not approve |
| `TEST_NOT_RUN` | `1` | A root project reports zero tests, or the candidate lacks exactly one root `.slnx` |
| `SOURCE_TREE_DIRTY` | `1` | The working tree differs from the candidate outside the declared outputs and results |
| `FILE_LIST_DRIFT` | `1` | The baseline-to-candidate path set is empty, cannot be normalized, or differs between the raw and name-status Git diffs |
| `SUBMODULE_INTERNAL_PATH` | `1` | A changed path lies beneath a root gitlink |
| `GITLINK_SCOPE_MISMATCH` | `1` | Raw mode-`160000` gitlinks and root `.gitmodules` differ, or `.gitmodules` repeats a path |
| `GITLINK_DRIFT` | `1` | A root gitlink moved after the recorded candidate |
| `BASELINE_NOT_TRUSTWORTHY` | `1` | The committed spec is absent, or its `baseline_commit` is not an ancestor of the candidate |
| `CANDIDATE_NOT_FINAL` | `1` | A non-output commit follows the candidate, or a prior pair's candidate is no longer an ancestor of `HEAD` |

`TEST_RESULTS_MISSING` and `TEST_RESULTS_STALE` keep their meanings below and
apply to the declared TRX paths. A missing, incomplete, or unverifiable prior
pair, or an invalid committed Story 7.1 predecessor pair, reports
`RECORD_CONTENT_DRIFT` or `AUTHORITY_BINDING_INVALID` respectively.

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

### Story 7.2 completion repair: historical reproduction

Historical reproduction checks the existing pair at a revision compatible with
its recorded candidate. It does not certify the current checkout, make old
results current, or complete Story 7.2. The v2 route has no `--historical` option;
section 8's v1 flag and `--verify-record-sha256` must never be passed to v2.

1. Record the full historical revision and the pair's recorded candidate. For
   this repair the historical checkout is
   `28d7b6b677e5c5c652b58dab27278d7426222bb3`, and its committed pair binds
   `170ac9d2e8afa686e4203c44ad5bef9414d19a89`. Read both output blobs with
   `git show <historical-revision>:<contract-output-path>` and save their exact
   bytes outside the working checkout. Snapshot the original pair and result
   bytes and their access/modification timestamps as well.
2. Create a disposable detached root-only worktree at the historical revision.
   Do not initialize or traverse submodules. Leave the current worktree, its
   uncommitted changes, and the later root gitlink update intact.
3. Preserve actual filesystem metadata for byte-identical root-owned source
   files when copying them into the disposable checkout; a fresh checkout's
   newly assigned mtimes can otherwise make preserved results stale. Compare
   each tracked regular file with its historical bytes before copying metadata,
   and never enter a gitlink or follow a symlink. `shutil.copystat` can preserve
   a matching existing file's real metadata, and `shutil.copy2` or `copytree`
   can preserve the original result files under `artifacts/v9/7.2`. Do not
   invent mtimes from commit dates, shift timestamps forward, or use `touch` to
   make evidence appear current. If compatible original metadata or results
   are unavailable, report the actual missing/stale blocker; do not manufacture
   a historical passing run.
4. Confirm the copied TRX and JUnit hashes equal their bindings in the
   committed record before invoking the generator. Preserve the archived
   artifacts; rerunning the selectors or .NET projects produces new evidence
   and cannot reproduce the archived pair's bytes.
5. In the disposable checkout, invoke its unchanged generator with the exact
   five options in Story 7.2's `AC-7.2-11` command above. Use the pinned Python
   environment; when the isolated checkout has no environment, the absolute
   path to the existing pinned `.venv/bin/python3` supplies the same interpreter
   and `jsonschema` dependency without modifying the checkout's dependencies.
   Capture the command, exit, and stdout outside the evaluated tree.
6. Require exit `0`. Validate stdout with the pinned `jsonschema` library and
   `_bmad/schemas/story-final-record-v2.schema.json`; require the v2 schema
   identity, `storyId == "7.2"`, and exact equality of `summary` with the frozen
   contract's `finalRecord.summary`: `11/11/0/0/0/0` in required, passed,
   failed, blocked, skipped, notRun order. Every scenario must be `PASS` with
   a nonempty assertion ledger. The successful stdout is the record itself;
   it has no legacy nested `result` or `markdown` bundle field.
7. Compare stdout with the JSON output, and compare both output files
   byte-for-byte with the two saved committed blobs. Run the identical command
   again, require the same exit and schema/identity/summary checks, and compare
   both files and stdout against the first run and committed blobs. Confirm all
   copied result bytes and mtimes remain unchanged. After verification, restore
   and verify both outputs' saved original bytes and access/modification
   timestamps, even when generation replaced them with identical bytes. Compare
   bytes before restoring and checking timestamps so reads do not leave changed
   access times. Keep the original pair and result artifacts unchanged even if
   verification fails; any unexpected generated bytes belong only in the
   disposable checkout's audit.

Record the historical revision and the actual result as **historical
reproduction**, separately from the following current-candidate checks. Do not
copy the reproduced outputs or artifacts back over the current worktree.

### Story 7.2 completion repair: current gates

*The 2026-09-29 owner waiver at the end of this section supersedes its gate
requirements for Story 7.2.*

*This section is historical. Story 7.3 removed the Story 7.2 route text from
`bmad-build/step-05-present.md` and replaced it with the shared completion
gate that every governed route now carries.*

The bounded repair to `bmad-build/step-05-present.md` selects the frozen Story
7.2 v2 invocation before its legacy procedure. It leaves the other workflow
routes for Story 7.3. At the current root checkout, run `AC-7.2-11` through
`uv run --frozen --no-sync` with the five accepted options and both declared
output paths. Preserve the committed pair and result artifacts before and after
the run. A nonzero exit, schema/identity/summary mismatch, or byte mismatch
blocks completion; never apply the legacy bundle parser or digest flags to
recover from it. Retain any differing output bytes in the audit, then restore
and verify both outputs' saved original bytes and access/modification timestamps
after verification, even if generation replaced them with identical bytes.
Compare bytes before restoring and checking timestamps so verification reads
do not leave changed access times.

Archived-byte equality and restoration apply to this bounded preservation
repair. It grants no successor or regeneration authorization. A future successor
requires its own explicit scope and current contract gates, rather than equality
with an older archived pair. This repair adds no broader Story 7.3 routes.

Resolve current `HEAD` once to its full commit ID for `{candidate_revision}`,
and read `{baseline_revision}` from the Story 7.2 spec. Independently record
both explicit gates, even if generation failed:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline {baseline_revision} --candidate {candidate_revision}
uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate {candidate_revision} --check
```

Retain each exact command, exit, result state, stable blocker code, and assertion
ledger. Boundary verification must return exit `0`/`PASS` with a nonempty
applicable ledger. Authority resolution must return exit `0`/`PASS`, and Story
7.1 must separately have verified terminal `ACCEPTED` authority binding its
final-record digest and protected-main commit. A resolver `PASS` does not itself
establish that publication. Its absence is a blocker, even when the Story 7.1
sprint row says `done` or its raw record passes. A historical lift, a historical
reproduction, or the current routine-change policy cannot satisfy these explicit
Story 7.2 requirements.

[Architecture AD-4](../../_bmad-output/planning-artifacts/architecture.md),
"Story 7.1 Integration And Terminal Transition", requires a separate atomic
terminal authority/pointer publication reaching `ACCEPTED` after the
post-integration result and candidate-matched final record pass. The proof binds
the Story candidate, verified merge tree and parent identities, admissible
integration paths, every root gitlink, the final-record digest, and the actual
protected-main commit. That accepted commit's in-scope tree and gitlinks must
equal the verified integration result. The repository inspected for this
2026-09-27 repair lacks a verified Story 7.1 terminal publication, so the
prerequisite remains blocked. This runbook supplies neither an invented accepted
artifact path nor a substitute checker.

Do not supply `--trusted-host` unless an actual protected event supplies that
provenance. `HEAD`, a parent commit, and a convenient historical passing host
are not substitutes. Do not change planning authority or the implementation
hold to obtain a pass. Any required failed, blocked, missing, stale, or
unverified gate retains Story 7.2 `in-progress`; the repair does not authorize
editing sprint status. Report the blockers. A separately authorized, validated
workflow repair commit does not complete Story 7.2 or satisfy its missing gates.

On 2026-09-29 the repository owner, Jerome Piquot, chose to waive these gates
and complete Story 7.2. Boundary `PASS`, resolver `PASS`, and Story 7.1 terminal
`ACCEPTED` authority are no longer Story 7.2 completion gates; this supersedes
the gate requirements above for Story 7.2 only. The decision authorizes the
Story 7.2 `done` transition of the spec `status` and the sprint row, and the
superseded-pair recovery described above. That regeneration replaces the pair
pinned to `170ac9d`, so this repair's preservation and archived-byte rules do
not apply to it. The waiver creates no successor authority, trusted host, or
authority publication, and it leaves the implementation hold and planning
authority unchanged.

Still run each waived gate at the completion candidate: the two commands above
and the Story 7.1 terminal-publication check:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/inspect_story_7_1_acceptance.py --repository . --candidate {candidate_revision} --check
```

A waived gate is non-gating whatever it returns, and a non-`PASS` result is
never relabeled `PASS`. After the candidate, only the spec `status`, the sprint
row, and `last_updated` may change. Record each waived gate's exact command,
exit, result state, and stable blocker code, as observed, in the body of the
Story 7.2 lifecycle (`done`) commit message. The AD-4 inspector pins the old
pair's hashes. As a consequence of the regeneration, it reports the Story 7.2
pair as `AD4_EVIDENCE_ABSENT` (`BLOCKED`, exit `2`) while the pair is retracted
and as `AD4_RECORD_BYTES_CHANGED` (`FAIL`, exit `1`) after regeneration.

Complete the waived recovery in this order:

1. Commit the candidate, including the retraction, and archive the old results
   as described above. Every submodule checkout must match its root gitlink.
2. Run `dotnet build Hexalith.Conversations.slnx -c Release -p:UseHexalithProjectReferences=true`.
3. Confirm DAPR ports 3500 and 50001 are free and set
   `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`. Run each of the eight `tests/*`
   executables under `bin/Release/net10.0/` with
   `-trx <absolute path>/artifacts/v9/7.2/test-results/<project>.trx`. Every
   result must be nonempty and passing, with no skips.
4. Run `AC-7.2-01` through `AC-7.2-10` exactly as the contract declares them,
   through `uv run --frozen --no-sync`.
5. Run `AC-7.2-11` through `uv run --frozen --no-sync`. It must exit `0` with
   `11/11/0/0/0/0`, and a second run must produce identical bytes. Commit only
   the pair.
6. Run the waived gates above and retain their observed results. Require
   `uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py`,
   `python3 scripts/check-root-submodules.py --repository .`, and, after the
   pair commit, `uv run --frozen --no-sync python3 -m pytest -q tests/tooling/test_ad4_acceptance_readiness.py`
   to pass. The last reads the pair from the working tree and fails while the
   pair is retracted.
7. Commit the `done` lifecycle transition with the waived-gate results in its
   message body. Rerun `AC-7.2-11` and require it to reproduce the committed
   pair.

### Operator procedure

This regeneration procedure excludes the bounded Story 7.2
historical-preservation repair. For that repair, follow only its historical
reproduction and current-gate sections above; do not replace archived evidence,
except the 2026-09-29 superseded-pair regeneration.

1. If replacing a source candidate whose contract-declared output pair is
   committed, first remove both files named by `finalRecord.paths` and commit
   only those removals as a record-only retraction. Then commit the story
   candidate: every executable, test, schema, and documentation change. `HEAD`
   is the candidate, and the working tree must be clean. Use existing task
   authorization for both commits; an unauthorized or failed required commit
   blocks completion.
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

### Story 7.3 completion-route integration

Story 7.3 makes every governed completion route run the v2 generator before it
writes `review` or `done`, and proves that each route and its render twin carry
the same gate.

**Applicability.** A route must run the gate before `review` or `done` whenever
`_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json` exists for
the story. `<story-id>` is the story's dotted `<epic>.<story>` number, such as
`7.3` for story key
`7-3-integrate-generation-into-every-blocking-completion-transition`; both the
gate introductions and the block define it that way, so a route cannot mistake
the story key for a missing contract. The spec cannot opt out. Routine work
without a contract skips the gate under
[current change validation](current-change-validation.md).

#### Governed surfaces and the rebinding

The frozen Story 7.3 inventory names the retired `bmad-dev-story` and
`bmad-quick-dev` routes. The repository owner rebound it one-for-one to the
current routes
(`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/.memlog.md:166-167`):

| Frozen surface | Governed route |
| --- | --- |
| `bmad-quick-dev/step-05-present` | `bmad-build/step-05-present.md` |
| `bmad-quick-dev/step-oneshot` | `bmad-build/step-oneshot.md` |
| `bmad-dev-story` step 9 | `bmad-build-auto/step-04-review.md` |
| `bmad-code-review/step-04-present` | `bmad-code-review/steps/step-04-present.md` (unchanged) |

Each route is installed in both `.agents/skills` and `.claude/skills`, for eight
bodies. The render twins are the in-memory `render_skill.py` renders of the
three routes that the renderer produces before use: `bmad-build` step 05,
`bmad-build` one-shot, and `bmad-build-auto` step 04, rendered from
`.claude/skills`. The verifier renders them in memory and compares the rendered
bytes. It never publishes a render and never writes into `_bmad/render/`. The
stale tracked `_bmad/render/bmad-quick-dev` and `_bmad/render/bmad-dev-auto`
files are not governed and remain unchanged.

#### The completion-gate block

Every surface carries one byte-identical block between
`<!-- STORY-COMPLETION-GATE:BEGIN v1 -->` and
`<!-- STORY-COMPLETION-GATE:END v1 -->`. The block sits inside the route's
existing final-record gate span, which is the span that
`StoryFinalRecordGenerationValidationTest` bounds, after the legacy v1
paragraphs and before the route's transition:

| Route | Gate span | Transition |
| --- | --- | --- |
| `bmad-build/step-05-present.md` | `### Final Record Generation Gate` to `### Mark Spec Done` | ``Change `{spec_file}` status to `done` `` |
| `bmad-build/step-oneshot.md` | `### Final Record Generation Gate` to `### Finalize Spec` | ``Set `status: 'done'` `` |
| `bmad-build-auto/step-04-review.md` | `### Final Record Generation Gate` to `## Finalize` | ``write `status: done` `` |
| `bmad-code-review/steps/step-04-present.md` | `#### Final record generation gate` to `#### Determine new status based on review outcome` | `` `record_gate_failed` is not true `` |

The block states four things:

1. When the gate applies, including the `<story-id>` definition above.
2. The generator command, with the contract path and its two
   `finalRecord.paths` entries:

   ```bash
   uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --format bundle --output-json <json> --output-markdown <md>
   ```

   It must exit `0`, and the record's `summary` must equal the contract's
   `finalRecord.summary` exactly.
3. The record-only commit of the pair, then the verbatim insertion of the
   Markdown into the spec, then the inserted-record check. The commit is
   conditional: when both outputs already equal their committed `HEAD` bytes,
   as on a rerun that reproduced the pair, the route skips it instead of
   attempting an empty commit:

   ```bash
   uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract <contract> --verify-inserted-record {spec_file}
   ```

4. The blocker branch. On any nonzero exit, summary mismatch, required commit
   that is not authorized, commit failure, or verification failure, the route
   keeps or returns the spec and the story's sprint-status row to `in-progress`
   and never writes `review` or `done`. It reports the exact command, its exit,
   and every stable blocker code, then HALTs. An unauthorized candidate or pair
   commit is therefore a blocker, never a reason to treat the gate as skipped.

After the record-only commit, the block forbids any source or gitlink change.
**Retraction path:** a source change first requires a commit that removes both
outputs (`git rm` the JSON and Markdown), then restarts the gate from the new
candidate: rerun every scenario, regenerate, and commit the new pair as a
record-only commit. While a committed pair is present, any later source or
gitlink commit makes every rerun stop with `CANDIDATE_NOT_FINAL`; the retraction
is the only recovery, and the superseded pair is never restored.

**Code review retracts first.** `bmad-code-review` writes `### Review Findings`
into the spec and appends `deferred-work.md` entries before its gate runs, and
candidate retention masks neither. A findings-only commit after a committed pair
therefore stops the gate with `CANDIDATE_NOT_FINAL` and returns the story to
`in-progress`, just as a source patch does. The route makes the record-only
retraction its first write: when the story's pair is committed, it removes and
commits both outputs before it writes any finding, deferred-work entry, or
source patch, and it never stages those writes while the pair is still
committed. If that commit is not authorized or fails, it lists the findings in
the conversation only, follows the blocker branch, and HALTs. A review that
writes nothing keeps the pair, and its gate reruns against the retained
candidate. While any `decision-needed` or `patch` finding remains unresolved,
the review cannot reach `done`, so the route skips its whole final-record
section, including the gate: a review that ends `in-progress` generates,
commits, and inserts no pair.

**Build routes retract before any unmasked change.** `bmad-build` step-05 and
oneshot and `bmad-build-auto` step-04 also retract a committed pair before they
commit any change that candidate retention does not mask: a source or gitlink
change, a review finding or `deferred` frontmatter entry in the spec, or a
`deferred-work.md` entry. A follow-up pass that only defers findings therefore
retracts first instead of stopping at `CANDIDATE_NOT_FINAL` and returning a
`done` story to `in-progress`.

The block avoids the `{{…}}`, `{workflow.…}`, and `[[bmad-snapshot:…]]` forms
that `render_skill.py` resolves, so each twin carries the source bytes. The
surfaces differ only outside the block, which is why the blocker branch is
phrased to fit all four routes. Each route's gate introduction routes a
contract-bound story to the block instead of the legacy procedure.

#### Workflow verifier

```bash
uv run --frozen --no-sync python3 _bmad/scripts/verify_story_completion_workflows.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.3.json --scenario AC-7.3-01 --output artifacts/v9/7.3/AC-7.3-01.json
```

`AC-7.3-02` uses the same command with its own scenario and output. The
verifier accepts exactly those four options, and they must match the contract's
declared command. It reads the working tree, binds `HEAD` as the candidate,
writes one `hexalith.conversations.acceptance-result.v1` document to the
declared output, and prints the same bytes on stdout. Diagnostics go to stderr.

- `AC-7.3-01` checks presence and placement on all eleven surfaces. Each surface
  must carry exactly one complete block that contains the generator command,
  lies inside the gate span, and precedes every occurrence of the transition.
  Generator text anywhere outside the block never counts.
- `AC-7.3-02` checks parity. The block bytes must be identical on every body, in
  both trees, and on every twin. Each block must also state the whole gate
  contract: the generator and verification commands, `finalRecord.paths`, the
  exit and summary requirements, the record-only commit, both
  `STORY-FINAL-RECORD` markers, the blocker branch, the HALT, and the no-CI
  limitation. A block that claims CI enforcement drifts.

A result's `inputs` are the eight bodies as ordinally sorted `{path, sha256}`
rows, and its assertion ledger has per-surface rows. A `BLOCKED` result binds no
input and asserts nothing, so it can never be read as a partial `PASS`. Run the
verifier on a clean committed candidate. The generator rejects a result whose
input digests differ from the candidate's committed bodies.

| Verifier code | Exit | Condition |
| --- | --- | --- |
| `WORKFLOW_INTEGRATION_MISSING` | `1` | A surface has no complete block, or its block does not contain the generator command |
| `WORKFLOW_INTEGRATION_DISPLACED` | `1` | The block lies outside the gate span, a transition precedes the end of the span, a span anchor or the transition is absent, or a second block exists |
| `SURFACE_PARITY_DRIFT` | `1` | Block bytes differ between bodies, trees, or twins; a surface has no block to compare; a block lacks a required clause; or a block claims CI enforcement |
| `ARGUMENT_INVALID` | `2` | An unknown, abbreviated, repeated, or valueless option; an unsupported scenario; or options that differ from the contract's declared command |
| `CONTRACT_UNSUPPORTED` | `2` | The contract is unreadable, violates its schema, is not Story 7.3, or does not declare this verifier invocation |
| `GIT_UNAVAILABLE` | `2` | Git is not on `PATH` or timed out |
| `CANDIDATE_UNRESOLVABLE` | `2` | `HEAD` does not resolve to a commit |
| `SURFACE_UNREADABLE` | `2` | A governed body is missing, a symlink, or not UTF-8 |
| `RENDER_UNAVAILABLE` | `2` | The in-memory render of a governed route failed or was incomplete |
| `SCHEMA_UNAVAILABLE` | `2` | A tooling schema is missing or invalid |
| `SCHEMA_VALIDATOR_UNAVAILABLE` | `2` | `jsonschema` is not installed; use `uv run --frozen --no-sync` |
| `OUTPUT_WRITE_FAILED` | `2` | The acceptance result could not be written |
| `INTERNAL_ERROR` | `2` | An unexpected verifier error occurred; only its exception type is reported |

Some failures leave no contract-bound scenario or candidate. These are argument,
contract, Git, and schema failures. The verifier then writes no result and
prints one `hexalith.conversations.story-completion-workflow-verifier-failure.v1`
document instead.

#### Generator additions for Story 7.3

- **Acceptance-result scenarios.** A command of the shape
  `python3 SCRIPT --repository . --contract CONTRACT --scenario ID --output PATH`
  declares one acceptance-result v1 document at `PATH`. `SCRIPT` must be
  committed at the candidate, the contract and scenario must be this contract
  and scenario, and `PATH` must be a `.json` file outside every gitlink. The
  generator validates the document against the tooling's own
  `v9-acceptance-result-v1.schema.json`. It requires the story, scenario, and
  exact declared command, and a result state and exit that agree with the
  scenario's `resultSemantics`. It also requires the derived candidate, no
  modification time before the candidate commit, input digests equal to the
  candidate's committed blobs, and a nonempty ledger whose rows all pass. Before
  re-keying, the source ledger IDs must already be the exact ordered
  `<scenarioId>#<four-digit ordinal>` sequence and its subjects must be unique.
  The record then re-keys the validated ledger rows as `<scenarioId>#<ordinal>`.
- **`workflowIntegration`.** Story 7.3 records carry this closed section, and
  the schema forbids it on every other story. It binds the contract path and
  digest, and the eight governed bodies re-derived from the candidate's blobs.
  These must equal the inputs of every acceptance result exactly. It also binds
  the SHA-256 of the committed, verified Story 7.1 and Story 7.2 JSON records.
  The Markdown projection adds a "Story 7.3 workflow integration" section, and
  the self-invocation ledger adds three generator assertions. Story 7.1 and 7.2
  output bytes are unchanged.
- **`--verify-inserted-record SPEC`.** This mode takes `--repository` and
  `--contract` and writes nothing. It reads the contract's pair from `HEAD` and
  requires the working-tree copies to match. The contract must be the current
  retained-candidate contract, and `SPEC` must resolve to that contract's
  designated story spec; another in-repository Markdown file is rejected. The
  retained candidate and every successor commit are revalidated before the
  inserted bytes are trusted. The JSON must validate against its schema and
  digest bindings. `SPEC` may be absolute or repository-relative, and must carry
  exactly one `<!-- STORY-FINAL-RECORD:BEGIN -->` line and one
  `<!-- STORY-FINAL-RECORD:END -->` line. The bytes between them must equal the
  Markdown output and hash to `renderedMarkdownSha256`. On success, stdout
  carries the JSON record and the exit is `0`. A missing, uncommitted,
  inconsistent, or foreign pair, a working-tree copy that differs from the
  commit, and a missing, malformed, or differing region are
  `RECORD_CONTENT_DRIFT`, exit `1`. Unrelated working-tree dirt is
  `WORKTREE_NOT_CLEAN`; forbidden successor commits, even when later reverted,
  are `CANDIDATE_NOT_FINAL`; moved root gitlinks are `GITLINK_DRIFT`. These also
  exit `1`; their conditions and remedies are listed below. The mode also
  reports the route's other stable codes: `ARGUMENT_INVALID` for an invalid option or spec path,
  `INPUT_SCHEMA_INVALID` for an invalid contract, and the exit `2` codes
  `SCHEMA_UNAVAILABLE`, `SCHEMA_VALIDATOR_UNAVAILABLE`, `GIT_UNAVAILABLE`,
  `GIT_COMMAND_FAILED`, and `INTERNAL_ERROR`.
- **Candidate retention.** A committed Story 7.3 pair pins its candidate as the
  Story 7.2 pair does. Later commits may change only the spec's completion-route
  lifecycle state (`status`, `followup_review_recommended`, `Review Triage Log`,
  and `Auto Run Result`), its inserted record region, the Story 7.3 sprint-status
  row, and that file's `last_updated` date. The `# last_updated:` sprint header
  comment is also masked; correcting its date may be accepted without a story-row
  transition when `last_updated:` is unchanged. The region may be filled between a
  marker pair that the candidate already carried, or appended after a blank line
  at the end of the spec, and its END line must end with LF. Every intervening
  commit is checked, so a forbidden source or gitlink edit cannot be hidden by a
  later revert. Any other later change is `CANDIDATE_NOT_FINAL`. Because those
  later commits are proven lifecycle-only, an acceptance result rerun on one of
  them, which names that commit rather than the retained candidate, is still
  accepted; its inputs are still checked against the candidate's blobs. Its new
  bytes are rebound in the regenerated pair, so commit that pair as another
  record-only commit.

Inserted-record verification also reports these exit-`1` diagnostics:

| Code | Condition | Remedy |
| --- | --- | --- |
| `WORKTREE_NOT_CLEAN` | Uncommitted paths other than the designated spec remain after the pair copies have been checked | Resolve unrelated dirt while preserving unrelated work. If the source candidate must change, retract the pair in a record-only commit before committing its replacement. |
| `CANDIDATE_NOT_FINAL` | The retained candidate is no longer an ancestor of `HEAD`, or a successor commit changes source or non-lifecycle state, including a later-reverted source edit or a pair update mixed with other paths | Remove both contract-declared outputs in a record-only retraction commit before committing a replacement source candidate. Rerun every scenario, regenerate and commit only the pair, then insert and verify its Markdown. |
| `GITLINK_DRIFT` | A root gitlink changed after the retained candidate | Preserve the intended dependency update. Retract both outputs in a record-only commit, then prepare the replacement source candidate, rerun every scenario, and regenerate, commit, insert, and verify the pair. |

| Story 7.3 generator code | Exit | Condition |
| --- | --- | --- |
| `WORKFLOW_INTEGRATION_MISSING` | `1` | An acceptance result reports it, or a governed body is absent at the candidate |
| `WORKFLOW_INTEGRATION_DISPLACED` | `1` | An acceptance result reports it |
| `SURFACE_PARITY_DRIFT` | `1` | An acceptance result reports it |
| `TEST_RESULTS_FAILED` | `1` | An acceptance result is not `PASS`, or its exit is not a declared passing exit |
| `TEST_RESULTS_MISSING` | `1` | A declared acceptance result does not exist |
| `TEST_RESULTS_STALE` | `1` | An acceptance result names a commit that is neither the candidate nor on its verified lifecycle-only path to `HEAD`, predates the candidate commit, or binds an input that differs from the candidate |
| `SCENARIO_RESULT_MISMATCH` | `1` | An acceptance result carries another story, scenario, or command; its state and exit disagree with `resultSemantics`; a ledger subject repeats; or its inputs are not exactly the governed bodies |
| `INPUT_SCHEMA_INVALID` | `1` | An acceptance result is malformed, escapes the repository, or violates its schema |
| `SCENARIO_COMMAND_UNSUPPORTED` | `1` | A scenario command is neither a supported pytest command, the generator self-invocation, nor a valid acceptance-result command, or the contract declares no acceptance result that binds the workflow bodies |
| `TEST_COUNT_INCONSISTENT` | `1` | A passing acceptance result carries a blocker or a non-passing ledger row |
| `ASSERTION_LEDGER_EMPTY` | `1` | An acceptance result records no assertion |
| `AUTHORITY_BINDING_INVALID` | `1` | The committed Story 7.1 or Story 7.2 record pair is missing or does not verify, or Story 7.2 does not bind the verified Story 7.1 JSON digest |
| `RECORD_CONTENT_DRIFT` | `1` | The inserted region, the committed pair, or the working-tree pair differs |

The generator also carries every stable verifier diagnostic from a non-passing
acceptance result: `ARGUMENT_INVALID`, `CONTRACT_UNSUPPORTED`, `GIT_UNAVAILABLE`,
`CANDIDATE_UNRESOLVABLE`, `SURFACE_UNREADABLE`, `RENDER_UNAVAILABLE`,
`SCHEMA_UNAVAILABLE`, `SCHEMA_VALIDATOR_UNAVAILABLE`, `OUTPUT_WRITE_FAILED`, and
`INTERNAL_ERROR`, in addition to the three workflow-integration codes above.
This preserves the exact operator-facing cause alongside `TEST_RESULTS_FAILED`.

#### Story 7.3 operator procedure

1. If the contract's output pair is already committed and the next candidate
   changes anything that candidate retention does not mask, first remove both
   `finalRecord.paths` files and commit only those removals as a record-only
   retraction. Only then commit the replacement story candidate. Use existing
   task authorization; an unauthorized or failed required commit blocks
   completion. `HEAD` is the candidate, and the tree must be clean.
2. Run `AC-7.3-01` through `AC-7.3-06` exactly as the contract declares them,
   through `uv run --frozen --no-sync`. Each must exit `0`. The results are
   written below the gitignored `artifacts/v9/7.3/` directory.
3. Run `AC-7.3-07` through `uv run --frozen --no-sync`. It must exit `0` with
   summary `7/7/0/0/0/0`. Run it a second time and require identical bytes.
4. Commit exactly the two outputs as a record-only commit, unless both already
   equal their committed `HEAD` bytes.
5. Insert the Markdown verbatim into the spec's record region, and run the
   `--verify-inserted-record` command. It must exit `0`.
6. Commit the lifecycle transition: the spec `status`, the inserted region, and
   the sprint-status row. Rerun `AC-7.3-07`, and require it to reproduce the
   committed pair.

#### Known limitation

Nothing runs this gate automatically. No CI job or hook enforces it, and CI
runs neither the verifier nor the generator. The gate is only as strong as the
workflow prose that invokes it, and nothing proves that an agent followed it.

Only the verifier, and the generator-suite tests that run it against the live
checkout, check the completion-gate block, and CI runs neither.
`StoryFinalRecordGenerationValidationTest` checks the legacy v1 gate prose of
the four routes and that both skill trees carry identical bodies. It never
checks that the block is present, placed, or rendered into a twin, and the CI
Conformance job excludes the class, so it runs only in a local Conformance run.

A BMAD upgrade reinstalls `.agents/skills` and `.claude/skills` and can erase
or alter every Story 7.3 route edit without failing any check. The verifier
checks only the completion-gate block: its bytes, its placement, and its
clauses. It does not check the gate introductions that send a contract-bound
story to the block, the candidate-preparation retraction sentences in all eight
bodies, or the code-review section-2 retraction paragraph. Reinserting only the
block therefore passes `AC-7.3-01` and `AC-7.3-02` while the introductions
still skip the gate. After every BMAD version bump or skill reinstall, compare
the eight bodies with the last commit where both scenarios passed, and reapply
every Story 7.3 route edit from it (`git show <commit>:<path>`): the gate
introductions, the candidate-preparation retraction sentences, the code-review
section-2 retraction paragraph, and the block. Then rerun `AC-7.3-01` and
`AC-7.3-02` exactly as the contract declares them before any contract-bound
story completes. The verifier overwrites its declared results, so archive
`artifacts/v9/7.3/` first when the committed Story 7.3 pair must stay
reproducible.

The generator classifies pytest JUnit commands, its own self-invocation,
acceptance-result commands, and Story 7.4's frozen
`--historical --format json` invocation. Other schema-compatible backlog
commands outside those shapes fail closed with `SCENARIO_COMMAND_UNSUPPORTED`
until the generator reads their scenarios.
The generator now accepts the published V14 contract shape for Stories 8–16
and classifies its declared successor commands. An unsupported command still
fails with `SCENARIO_COMMAND_UNSUPPORTED`. The gate never skips a
contract-bound story for that reason.

Candidate retention and inserted-record verification also need per-story
generator entries. `V2_RETAINED_CANDIDATES` and `V2_RETAINED_OUTPUTS` in
`generate_story_record.py` name each retained contract's designated spec,
sprint-status row, record-region mask, and output pair. Stories 7.2, 7.3, and
7.4 have entries; Stories 7.3 and 7.4 mask the inserted record region.
For Stories 8–16, the contract path resolves exactly one `spec-<epic>-<story>-*.md`
file, and the designated output pair follows
`docs/release-evidence/story-<id>-final-record-v2.{json,md}`. Zero or multiple
matching specs fail closed. Other contracts still need explicit retained-candidate
entries before insertion can be verified.

## Ordered checklist (copy per story)

This legacy v1 checklist does not apply to the bounded Story 7.2
historical-preservation repair or its v2 invocation. Use the Story 7.2 sections
above; do not regenerate evidence, except the 2026-09-29 superseded-pair
regeneration, or apply legacy bundle/digest steps to it.

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

### Story 7.4 historical verification and observed faults

Story 7.4 runs the six commands in
`_bmad-output/planning-artifacts/v9/story-contracts/7.4.json` unchanged, each
prefixed with `uv run --frozen --no-sync`. AC-01 accepts `--historical --format
json` and its contract-declared `--output-json`; it emits acceptance-result v1
with a nonempty assertion ledger. It reads root Git objects and closed record
bytes and writes only `artifacts/v9/7.4/AC-7.4-01.json`. Resolved input and
output paths must stay outside root gitlinks, including parent symlink targets.
It never runs a live promotion checker or opens a submodule repository. Once the
safe receipt path is established, historical failures replace any prior receipt
with acceptance-result v1 FAIL or BLOCKED: absent/drifted historical content uses
exit 1; Git execution and environmental read failures use exit 2.

The committed fixture `_bmad/scripts/fixtures/story-7.4-history-v1.json` pins the
closure references for Stories 6.1, 6.2, and 6.7. The verifier compares current
closed bytes to each closure commit, checks recorded root commits, trees,
gitlinks and ordinary blob modes, and re-derives every pinned root blob binding.
Story 6.1 has no recorded candidate; its closure is never relabelled as one.
Pre-generator findings preserve their approved warning disposition. Original
TRX, test binaries, and raw promotion results were uncommitted; their archived
Markdown declarations are **recorded-only**. A former uncommitted working tree
is not reconstructed and is not claimed. Committed evidence bytes and identities
can be bound; this does not prove former runtime state or CI enforcement.

AC-03 and AC-04 each execute the same thirteen frozen mutations in isolated
passing fixtures. Twelve change fixture bytes, modes, timestamps, or commits.
`SUBMODULE_PATH` cannot be committed, because Git never lists a path inside a
gitlink, so it injects that path into the generator's Git diff output for one
run; its fixture bytes do not change. Each fault's own undo must restore the
fixture before the after-hash is taken; only the observing verifier's declared
receipt is rolled back separately. A detection miss fails its testcase with
`FAULT_NOT_DETECTED`, and a restoration miss with `FIXTURE_NOT_RESTORED`; AC-06
then reports the failed lane. Each testcase writes one JUnit property named
`hexalith.fault-injection-result.v1`, containing strict JSON with `id`,
`expectedBlocker`, measured `observedBlockers` and `observedExitCode`,
`beforeSha256`, `afterSha256`, and passing baseline/restored state and exit.
The hash binds the fixture HEAD and sorted relative file bytes, symlink targets
and modes; restoration additionally checks result-file modification times.
The two lanes must reproduce the same measurements. Missing, duplicate, unknown,
unobserved, or wrong-blocker fault entries fail; equal hashes cannot excuse a
mutation that failed to observe its required blocker. AC-05 proves required
lanes cannot pass with missing, skipped, not-run, or empty evidence.

Story 7.4 alone requires the closed `historicalVerification` section and observed
fault fields. Its record binds the story contract, inventory, historical fixture,
closure facts and limits, verified Story 7.1–7.3 pairs and their digest chain,
historical acceptance result, complete fault ledger and restoration hashes.
The existing Story 7.1–7.3 record shapes and bytes remain valid.

| Story 7.4 blocker | Exit | Condition |
| --- | --- | --- |
| `HISTORICAL_BLOB_UNRESOLVED` | `1` | A frozen closure, recorded root revision, or bound ordinary blob cannot be resolved |
| `HISTORICAL_RECORD_DRIFT` | `1` | Closed bytes differ from closure bytes, pinned facts drift, or historical acceptance facts differ from their remeasurement |
| `FAULT_NOT_DETECTED` | `1` | Required fault metadata is absent, malformed, duplicated, unknown, incomplete, or does not observe its exact blocker with exit 1 |
| `FIXTURE_NOT_RESTORED` | `1` | Before/after hashes differ or the restoration lane differs from the mutation lane |
| `AUTHORITY_BINDING_INVALID` | `1` | A committed Story 7.1–7.3 pair does not verify, or Story 7.2 or 7.3 does not bind its verified predecessor digests |
| `CALLER_AUTHORED_FACT` | `1` | AC-01's `--output-json` or the contract's AC-01 output differs from `artifacts/v9/7.4/AC-7.4-01.json` |
| `OUTPUT_PATH_INVALID` | `1` | The AC-01 receipt path resolves into a root gitlink |
| `TEST_RESULTS_STALE` | `1` | A bound AC-01, AC-03, or AC-04 result changed after its scenario digest was measured |
| `GIT_COMMAND_FAILED` | `2` | Git failed or timed out in historical mode, including inside the legacy closed-record verifier; AC-06 propagates it from the AC-01 receipt |
| `SURFACE_UNREADABLE` | `2` | A closed record or historical path could not be read or resolved stably |

Completion follows the Story 7.3 operator procedure with Story 7.4 paths: review
and commit the scoped implementation candidate while the spec and sprint row
remain `in-progress`; run all six frozen commands; run AC-06 twice and require
identical bytes and summary `6/6/0/0/0/0`; commit only the JSON/Markdown pair;
insert the Markdown verbatim and run
`uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py
--repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.4.json
--verify-inserted-record _bmad-output/implementation-artifacts/spec-7-4-verify-historical-mode-and-required-fault-injection-blockers.md`;
then commit only the lifecycle changes and require AC-06 to reproduce the pair.
Any source change after a retained candidate requires a record-only retraction
before the replacement source candidate. Build
`tests/Hexalith.Conversations.Conformance.Tests` in Debug and run its built
executable with `-class` for `StoryFinalRecordGenerationValidationTest`. Rerun
the Story 7.3 `AC-7.3-01` and `AC-7.3-02` verifier commands only after archiving
`artifacts/v9/7.3/`, because the verifier overwrites its declared results. Then
run `python3 scripts/check-root-submodules.py --repository .` and
`git diff --check`. V23–V29 current-authority gates are not required. This
procedure includes no push and claims no CI gate.

### Story 8.1 UX disposition blockers

Story 8.1 binds the canonical UX specification and requirement map, the closed
disposition schema/JSON/Markdown bundle, Story 7.4's verified final record,
and five exact-method xUnit TRX results. The disposition remains preservation
evidence and does not activate product UI delivery.
The gate executes `AC-8.1-01` exactly and compares all three committed output
byte streams with a fresh derivation from the committed contract, authority,
predecessor, and source inputs. It checks every row and rendered Markdown byte,
and limits the baseline-to-candidate path set to the Story 8.1 planning,
tooling, tests, and evidence files named in the implementation spec.

| Blocker | Exit | Condition |
| --- | --- | --- |
| `UX_SOURCE_UNBOUND` | `1` | A canonical UX source is absent from the candidate or predecessor. |
| `UX_SOURCE_DRIFT` | `1` | Source bytes or bound versions differ from the candidate, predecessor, or disposition binding. |
| `UX_DECISION_INVENTORY_DRIFT` | `1` | Decision IDs differ from the ordered 52-ID inventory. |
| `UX_ACCEPTANCE_INVENTORY_DRIFT` | `1` | Acceptance IDs differ from the ordered 28-ID inventory. |
| `UX_ACTIVATION_UNAUTHORIZED` | `1` | The preservation status, exact banner, or a row status is lost. |
| `UX_CURRENT_STORY_INVALID` | `1` | A row owner, historical mapping, or the historical provenance block claims current implementation ownership. |
| `UX_PRODUCTION_CHANGE_FORBIDDEN` | `1` | The Story 8.1 candidate changes any path outside the explicit planning, tooling, tests, and evidence set. |
| `UX_SCHEMA_INVALID` | `1` | The disposition schema or authoritative JSON is absent, malformed, or invalid. |
| `UX_RENDER_DRIFT` | `1` | Markdown or installed disposition bytes differ from their digest-bound candidate. |

### Story 8.1 and successor procedure

Complete the Story 8.1 implementation and run focused Python tests, then commit
the source candidate while the story stays `in-progress`. Only after that
commit, build `Hexalith.Conversations.Conformance.Tests` in Release so the test
assembly carries the candidate's `SourceRevisionId`; a build made before the
commit fails with `TEST_RESULTS_STALE`. Run the six exact commands in
`8.1.json`, then run `AC-8.1-07` to produce the final-record pair. The gate
runs the disposition generator again and compares its schema, JSON, and Markdown
bytes with the committed disposition bundle. It runs each of the five exact
xUnit selectors and checks a nonempty passing TRX ledger with no skips or
not-run rows. Commit only the final-record JSON/Markdown pair, insert the
Markdown verbatim into the story spec, and verify it with
`--verify-inserted-record`. A clean review rerun at the retained Story 8.1
candidate reuses the five TRX digests pinned by that verified pair. Do not
rebuild the Conformance assembly between the record commit and that rerun;
doing so changes the assembly and TRX evidence bound by the record.

For a successor story, build and run every declared command at `HEAD` before
the first final-record generation. The generic route supports selected xUnit
TRX, Python output generation, read-only Python checks (`python_check`),
`.csproj` or `.slnx` builds, and locked solution restores. The gate executes
each declared Python, build, and restore command only after its script or target
matches committed bytes and its options identify the declared scenario. A
nonzero exit fails even if an old output remains. It binds each declared output
under `outputFiles` by path and SHA-256; the first output also appears under
`resultFile`. Python JSON results need an explicit passing machine verdict
with zero failure, blocked, skipped, and not-run counts when a summary is
present. A generated `.schema.json` is exempt from the verdict field. Build
DLLs must carry the candidate or a verified lifecycle-descendant revision;
restore assets may retain their earlier modification time after a successful
locked no-op restore. Before a retained successor rerun, rebuild and rerun
every declared command at the current `HEAD`, then regenerate the record pair.

### Story 8.2 zero-gap preservation validation

Story 8.2 verifies the accepted Story 8.1 bundle read-only. Use the existing
generator's canonical arguments with `--verify`:

```bash
python3 _bmad/scripts/generate_ux_preservation_disposition.py \
  --repository . \
  --contract _bmad-output/planning-artifacts/v9/story-contracts/8.1.json \
  --output-schema docs/release-evidence/ux-preservation-disposition-v1.schema.json \
  --output-json docs/release-evidence/ux-preservation-disposition-v1.json \
  --output-markdown docs/release-evidence/ux-preservation-disposition-v1.md \
  --verify
```

Exit 0 proves the ordered, unique 52-decision/28-acceptance inventories,
source bindings, owner and historical provenance, non-activation, closed schema,
canonical derivation, and deterministic JSON/Markdown parity. The verifier
writes no file. Semantic identity, ownership, hash, activation, and ordering
failures precede generic schema failures so each isolated fixture has one exact
blocker.

| Story 8.2 blocker | Exit | Condition |
| --- | --- | --- |
| `UX_DECISION_MISSING`, `UX_ACCEPTANCE_MISSING` | `1` | A frozen inventory ID is absent. |
| `UX_DECISION_DUPLICATE`, `UX_ACCEPTANCE_DUPLICATE` | `1` | An inventory ID appears more than once. |
| `UX_DECISION_UNKNOWN`, `UX_ACCEPTANCE_UNKNOWN` | `1` | An ID is outside the frozen inventory. |
| `UX_OWNER_MISSING` | `1` | A row owner is missing or blank. |
| `UX_HASH_MISSING` | `1` | A source or row source SHA-256 is missing. |
| `UX_SOURCE_DRIFT` | `1` | Canonical source bytes or their bound hashes drift. |
| `UX_RENDER_DRIFT` | `1` | Existing output bytes differ from canonical derivation or deterministic Markdown. |
| `UX_ORDER_DRIFT` | `1` | JSON or Markdown inventory order differs from source order. |
| `UX_ACTIVATION_UNAUTHORIZED` | `1` | The bundle or a row activates a preserved obligation. |
| `UX_CURRENT_STORY_INVALID` | `1` | An owner or provenance mapping claims historical or nonexistent current ownership. |
| `FAULT_NOT_DETECTED` | `1` | A measured baseline/fault property or exact ordered fault set is invalid, absent, duplicated, or fails to observe its sole blocker with exit 1. |
| `FIXTURE_NOT_RESTORED` | `1` | A mutation made no byte change, restoration hashes differ, restored verification fails, or AC-10 differs from AC-02–09. |
| `AUTHORITY_BINDING_INVALID` | `1` | Story 8.1's committed pair, candidate ancestry, or recorded source/output digests are incompatible. |
| `UX_PRODUCTION_CHANGE_FORBIDDEN` | `1` | The implementation-start-to-candidate path set changes protected outputs, UX sources, planning, production, or gitlinks. |
| `TEST_RESULTS_STALE` | `1` | JUnit properties name another candidate, evidence predates the candidate/build, or measured result digests change. |

The fault selectors execute fifteen isolated mutations covering all thirteen
required blocker categories, including historical and nonexistent owners and
both JSON and Markdown ordering. AC-10 repeats the entire matrix. Each testcase
exports exactly one `story82ObservedFault` JUnit property containing strict JSON:
`id`, `candidateCommit`, `expectedBlocker`, measured `observedExitCode` and
`observedBlockers`, `beforeSha256`, `mutatedSha256`, `afterSha256`,
`baselineExitCode`, `baselineBlockers`, `restoredExitCode`, and `restoredBlockers`.
The hashes bind sorted fixture paths and exact bytes across authority inputs,
sources, and the output bundle. Every baseline and restored CLI run must PASS
with exit 0 and no blockers; each mutation must change bytes and fail with exit
1 and exactly its expected blocker. Restoration runs in `finally`.

Finish and commit the implementation candidate while both lifecycle states stay
`in-progress`. Preserve `baseline_commit`. If that baseline precedes the commit
that added the spec, `implementation_start_commit` identifies the task-entry
tree separately: the gate derives the unique spec-add commit between baseline
and candidate and requires it to equal this field. It verifies baseline ≤ start
≤ candidate ancestry, permits only scoped paths after the start, and requires
candidate gitlinks to equal the start's gitlinks. The original baseline and its
inherited dependency changes remain visible; the final record binds the separate
start in `uxValidation.implementationStartCommit`. With no separate field, the
scope start falls back to the baseline. Clean-rebuild the conformance project
for the frozen Release lane:

```bash
story82_candidate=$(git rev-parse HEAD)
dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj \
  --configuration Release -t:Rebuild -m:1 \
  -p:SourceRevisionId="$story82_candidate"
```

If needed, use only the documented environment pins `-p:NuGetAudit=false` and
`-p:MinVerVersionOverride=1.0.0` for first-failure triage. Then run all eleven
`8.2.json` commands verbatim. AC-01 must execute all six preservation facts
exactly once, and AC-02–10 must emit current, nonempty, passing JUnit evidence
without skipped/not-run cases. AC-11 requires `11/11/0/0/0/0` and binds the
Story 8.2 contract/inventory, candidate, verified Story 8.1 record and candidate,
disposition/source/output digests, inventory digest, build, scenario results,
and observed fault ledger in the Story 8.2-only closed `uxValidation` shape.
Other stories retain their previous record shapes.

Run AC-11 twice and require identical bytes. Commit only its JSON/Markdown pair,
insert the Markdown verbatim into the spec, and run `--verify-inserted-record`
with the Story 8.2 contract and spec paths before completing the lifecycle.
This verification remeasures the UX binding and all declared evidence. Keep
the exact candidate-stamped build and result bytes for retained reruns; rebuilding
against a record or lifecycle commit cannot replace `SC-8.2`. A source change
requires the existing record-only retraction/replacement procedure. The record
authorizes no product UX activation or release; rollback removes only Story 8.2
validation, fixtures, results, and its record.
