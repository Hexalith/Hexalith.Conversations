---
title: 'Publish the release-owner implementation-hold lift as V17 authority'
type: 'feature'
created: '2026-09-08'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '94dbb37694747e9dedee20591b84b5d6a69d19b3'
completion_scope: 'documentation-reconciliation'
publication_baseline_commit: '074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf'
implementation_commit: '82b91c10fe95f8ef7a7576d35199322481e359a7'
publication_commit: '8bd6789eae34a322e06156dd8e7e2fb0aded42d7'
reconciliation_baseline_commit: '5e4abc6f87692e634319c1d5612c91647ae6eae7'
submodule_promotions: []
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

## Reconciled Scope (2026-10-08)

The user authorized reconciliation with the existing V17 publication and current
validation policy. V17 was already published in the C1/C2 commits named above.
This run changes only this spec; it records that delivery rather than implementing
or republishing it. `status` tracks this documentation reconciliation. Completion
does not certify every original matrix fault, start a successor, satisfy the
readiness rerun, or grant current execution, release, or push authority.

The frozen section below remains byte-identical historical intent. Its missing-record
description and green-gate prerequisite describe the original development entry,
not a new implementation request. The reconciled tasks and acceptance below govern
this documentation correction. The original `baseline_commit` is retained as
planning provenance; the eight-path V17 transaction starts at
`publication_baseline_commit`, after the prerequisite work. Review the current
documentation diff from `reconciliation_baseline_commit`.

Use the [current-change policy](../../docs/runbooks/current-change-validation.md)
for this correction, as explicitly authorized by the user. This task did not
perform a fresh remote check of the policy's repository-wide activation.
The [evidence-boundary runbook](../../docs/runbooks/evidence-boundary-validation.md)
and verifier remain historical reproduction tools. A historical V17 `PASS` does
not turn a later `BLOCKED` result into a pass or change a current hold decision.
Preserve every authority, schema, publisher, test, and IR-0 record. Create no new
commits or publications, and do not push.

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** IR-0 assessed `READY` but records `hold_decision.state: absent` and `effective_hold: ACTIVE`, and states that `READY` does not lift the hold. `v11-story-7.1-schema-slice-v1.json` requires `holdRequirement.effectiveState: LIFTED` at `_bmad-output/planning-artifacts/implementation-hold-v1.json`, a record that has never existed, so `7.1-SCHEMAS`, Story 7.1, Epic 16 and all 30 successor rows stay blocked.

**Approach:** Follow the V15/V16 publication pattern — a recursively closed Draft 2020-12 schema, a deterministic git-blob-sourced publisher with fault tests, and an atomically written record plus a successor authority. Publish the release-owner decision as `LIFTED` and V17 as the successor to V16, unlocking `7.1-SCHEMAS` only.

**Human decisions (2026-09-08):**
- Release owner is recorded as `{owner: "Release owner", name: "Jerome — release-owner hold lift 2026-09-08", mechanism: "Repository-recorded independent release-owner decision; no cryptographic-signature claim"}`, mirroring `epic-6-completion-supersession-current-proof-decision-v1.json`.
- The lift is **candidate-bound with no calendar expiry**: it binds the IR-0 sha256, the V9 `planningCandidate` and `bundleDigest`, and goes stale on any drift. `expiry.calendarExpiry` is `null`.
- The readiness rerun IR-0 requires is a **separate follow-up**, declared as an unmet obligation in `nonClaims`. This spec does not perform or satisfy it.
- **Implementation is gated.** At `94dbb37` the evidence lane is red (`verify_evidence_boundary.py` → `FAIL`/`EVIDENCE_GATE_NOT_USED`; 25 failed / 282 passed). Do not begin implementation until that lane is green, because V17 may not claim `PASS` on a red gate and because the concurrent lifecycle-gate restoration edits the same verifier file.

## Boundaries & Constraints

**Always:** Recompute every declared hash from source bytes via `git show <candidate>:<path>`; derive modes from `git ls-tree`, never `os.stat`; compare the changed-path boundary by exact set equality reporting both missing and unexpected paths; derive gitlinks from raw mode `160000`; keep `PASS`/`FAIL`/`BLOCKED`/`not-applicable` distinct with a nonempty ledger and `skipsAllowed: false`; keep V9–V16 and IR-0 bytes byte-identical; keep the V9 predecessor chain intact (`fileSha256 8af7ba3b…f953ef3`, `bundleDigest 159eec0c…98ff4f055`, `planningCandidate 1e9a6112…3355e5`); publish additively.

**Never:** Rewrite or reissue any V1–V16 authority; register the hold record in the V9 bundle or its path sets; touch `sprint-status.yaml` row states, `src/`, `tests/`, `references/`, or `.gitmodules`; initialize nested submodules; set `releaseAuthorized` or `pushAuthorized` true; claim the readiness rerun; treat `BLOCKED` or a skip as a pass; push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Publish | Clean candidate containing both new artifacts | Record + V17 written atomically; `result: PASS`; `HOLD_DECISION_OK` line; exit 0 | — |
| `--check` re-run | Published candidate, unchanged bytes | Recomputed document equals committed bytes; exit 0 | `HOLD_AUTHORITY_DRIFT` on any mismatch |
| Predecessor drift | Any of the 7 carried authorities differs in bytes or mode | `HOLD_PREDECESSOR_DRIFT`, `FAIL`, exit 1 | Nonempty ledger; nothing written |
| Stale binding | IR-0 sha256, planningCandidate or bundleDigest differs | `HOLD_BINDING_STALE`, `FAIL` | Record must not evaluate to `LIFTED` |
| Unavailable history | Shallow clone, missing blob, git failure, timeout | `BLOCKED`, exit 2 | Never collapsed to `FAIL` or `PASS` |
| Closed schema | Unknown field, weak hash/path pattern, empty ledger | Rejected by `Draft202012Validator` | `HOLD_SCHEMA_INVALID`; `HOLD_SCHEMA_UNAVAILABLE`/`BLOCKED` if jsonschema absent |
| Path escape | Absolute, `..`, backslash or non-normalized path | `HOLD_PATH_ESCAPE`, `BLOCKED` | — |

</frozen-after-approval>

## Code Map

Already committed in C1, mirroring `publish_v16_planning_tooling_lifecycle.py` and its test:

- `_bmad/schemas/implementation-hold-v1.schema.json` -- closed record schema. `$id https://hexalith.com/schemas/implementation-hold-v1.schema.json`.
- `_bmad/schemas/v17-implementation-hold-decision-authority-v1.schema.json` -- closed successor-authority schema, same `$id` host form.
- `_bmad/scripts/publish_implementation_hold_decision.py` -- publisher. Error class `ImplementationHoldError(code, detail, state="FAIL")`, `HOLD_*` codes, success token `HOLD_DECISION_OK`.
- `_bmad/scripts/tests/test_publish_implementation_hold_decision.py` -- fault suite.

Already modified in C1:

- `_bmad/scripts/verify_evidence_boundary.py` -- contains the V17 constants, candidate-tree route ahead of V16, `validate_v17_scope()`, verification dispatch, and publisher check. Later successors take precedence when reproducing their candidates.
- `_bmad/scripts/tests/test_verify_evidence_boundary.py` -- V17 route and scope coverage.

Already published outputs (C2):

- `_bmad-output/planning-artifacts/implementation-hold-v1.json`
- `_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json`

Read-only roots of trust — never modified, only hashed: `v9-authority-bundle-v1.json`, `v12`/`v13`/`v14`/`v15`/`v16` authority JSONs, `implementation-readiness-report-2026-08-22-ir-0.md`, `v11-story-7.1-schema-slice-v1.json`.

The existing publisher uses the V16 JSON, Git, CLI, and per-file atomic-write conventions. Its fault tests load it by path and stage fixtures in shared temporary clones. The current correction's only editable path is `_bmad-output/implementation-artifacts/spec-v17-implementation-hold-decision-authority.md`.

## Tasks & Acceptance

**Execution:**

- [x] This spec -- record the actual C0/C1/C2 commit identities and compare C1's six paths and C2's two paths to the published inventories by exact set equality, including missing and unexpected paths; derive modes and gitlinks from Git.
- [x] This spec -- record deterministic publisher and historical V17 verifier results; independently recompute all seven carried hashes and modes at the publication baseline, C2, and reconciliation baseline.
- [x] This spec -- record the focused publisher and V17 routing test results and validate the two existing commit messages with the pinned commitlint CLI, retaining successful command evidence outside the repository.
- [x] This spec -- distinguish the original planning baseline and its broader diff from the publication baseline; explain the current-change policy and retain the current historical-verifier blocker honestly.
- [x] This spec -- define this file's review scope, record the complete observed changed-path inventory and concurrent work, confirm the frozen section is unchanged and links resolve, and run root-submodule inventory and whitespace checks. Confine this task's tracked write actions to this spec; leave historical artifacts, sprint states, and authorization flags untouched.

**Acceptance Criteria:**

- Given the existing C1/C2 and their actual C0, when their Git objects and publisher are checked, then the path sets are exactly six and two (eight combined), no raw mode `160000` changed, both documents reproduce, and all seven carried authorities retain their declared bytes and modes.
- Given the historical V17 publication, when the verifier evaluates C0 to C2 and focused tests run, then the verifier returns `PASS` with a nonempty ledger, all selected tests pass without skips, and the existing commit messages pass pinned commitlint.
- Given the reconciliation baseline and current policy, when the historical verifier returns `BLOCKED`, then this spec records its exact command, code, and exit state separately from V17's historical `PASS`, and makes no current hold-lift or readiness-rerun claim.
- Given the authorized documentation correction, when its selected diff is reviewed, then the review scope is this spec, this task's tracked write actions are confined to it, the complete observed changed-path inventory identifies concurrent work without inferring preservation from a path filter, the original baseline and frozen intent remain preserved, links and focused checks pass, and completion refers only to documentation reconciliation.

## Implementation Notes

- Original planning entry: `94dbb37694747e9dedee20591b84b5d6a69d19b3`.
- Publication C0: `074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf`.
- Implementation C1: `82b91c10fe95f8ef7a7576d35199322481e359a7`.
- Publication C2: `8bd6789eae34a322e06156dd8e7e2fb0aded42d7`.
- Documentation reconciliation baseline: `5e4abc6f87692e634319c1d5612c91647ae6eae7`.

C1 is C0's direct child and changes exactly the six code/schema/test paths in
V17's `publication.c1Paths`; the spec is outside that historical transaction.
C2 is C1's direct child and adds only the hold record and V17 authority. Their
combined changed-path set is the published eight-path inventory, with zero
changed gitlinks. The original planning-entry-to-C2 range instead contains
40 paths and six changed gitlinks from intervening work; it is not the V17
publication transaction. Preserve that distinction rather than rewriting history.

V17 records `implementationHold: LIFTED` for `7.1-SCHEMAS`, with release and push
authorization false and the readiness rerun unmet. That candidate-bound historical
decision is reproduced here. Later authority and effective-hold decisions retain
their own meaning; this correction grants no current permission to resume work.

During the audit, independent work advanced the repository to
`b3a813b0ba0020a558a3779e4e88b65612850e82`, including the initial V17 spec
reconciliation and V20/V21 policy changes. The reconciliation entry baseline
remains `5e4abc6f87692e634319c1d5612c91647ae6eae7`. Its complete range to that
observed tip has five paths; this task's declared review scope is this spec. Review the V17
documentation correction with the spec path selected, and review this audit's
remaining worktree delta against the observed tip. Do not describe the whole
entry-baseline-to-tip range as a one-file change.

Independent uncommitted corrections to
`_bmad-output/implementation-artifacts/spec-v21-review-current-policy-disposition.md`
and `docs/runbooks/v21-review-disposition.md` were also present during final
verification. They remain outside this task's write actions; the whole
worktree diff therefore contains more than this spec. This task's tracked
write actions targeted only the V17 spec. The selected diff defines review
scope; it does not establish authorship or prove the final bytes of concurrently
edited files were preserved. No whole-worktree before/after snapshot is claimed.

## Spec Change Log

- 2026-10-08: The user authorized reconciling the stale spec with the already
  published V17 and current validation policy. Retained the original baseline
  and frozen intent, recorded the actual transaction separately, replaced the
  new-implementation tasks with a documentation audit, and scoped completion to
  that correction. Keep all historical publication, authority, and IR-0 bytes.
- 2026-10-08: Completed the scoped audit: independently checked transaction
  paths, hashes and modes; reproduced the published outputs and historical
  verifier; measured 26 focused passing tests; validated the existing C1/C2
  messages; and retained the later historical-verifier blocker. Recorded
  concurrent advancement separately. At that handoff, status was `in-progress`
  pending independent workflow review; review subsequently moved it to
  `in-review`. This is historical handoff metadata, not a current status claim.
- 2026-10-08: Review corrections recorded frozen-environment and source
  provenance, clarified review scope and concurrent-change evidence, and added
  a self-contained reproduction command. The supplementary documentation
  audit now accepts `in-progress`, `in-review`, and `done` and reports the
  actual frontmatter status. Frozen intent and historical publication remain unchanged.
- 2026-10-08: Completed independent review and the documentation reconciliation.
  All findings were triaged below; no work was deferred. Final audits passed
  with `status: done`. The historical matrix, readiness rerun, and execution
  decisions remain outside this completion scope.

## Review Triage Log

The three independent review layers evaluated the documentation correction.
The verification-gap reviewer reported no gaps. Each finding below was checked
against the source and evidence before corrections; changes remain confined to
this documentation artifact and its supplementary temporary audit helpers.

| Finding | Verdict | Evidence and disposition |
|---------|---------|--------------------------|
| Blind 1: stale status and audit guard | medium | The documented command reproduced exit 1 after status moved to `in-review`. The helper now accepts the three reconciliation lifecycle states and reports the actual state; handoff metadata is explicitly historical. Corrected and rechecked. |
| Blind 2: frozen environment provenance | low | Execution flags alone did not establish the installed environment. `uv sync --frozen --check` exited 0, checked 11 packages, and would make no changes. Recorded the independent evidence. |
| Blind 3: executed tooling identity | medium | Evaluated candidate IDs did not identify worktree tooling. All nine executing-tooling inputs match the named observed-tip blobs; the portable audit repeats that comparison. Recorded and verified. |
| Blind 4: selected diff overclaims ownership | medium | Filtering to the spec cannot establish authorship or unrelated-file preservation. The audit now reports the complete changed-path inventory separately, and the text limits preservation claims to write actions and measured snapshots. Corrected without touching concurrent work. |
| Blind 5: temporary-only reproduction | medium | A fresh checkout cannot recover session-only helper scripts. The spec now includes a self-contained standard-library/Git audit and direct Git-to-commitlint commands. The portable audit passed. |
| Blind 6: policy activation evidence | false | The user explicitly authorized applying the current policy to this correction; this task makes no repository-wide activation decision. Clarified that no fresh remote activation check is claimed. |
| Edge 1: review status breaks audit | medium | Independently confirmed the same failing guard as Blind 1. The corrected helper passed in `in-review`; final validation also checks the completion state. |

## Design Notes

**Carried set is 7, not 16.** Earlier authorities use a mix of byte-pinned overlay blocks in `epics.md`/`architecture.md` and standalone JSON sidecars, including V9 and V12–V14. V10 has no separate authority JSON file. `immutableAuthorities` was introduced at V15; the growth rule is "append the immediate predecessor authority JSON, keep IR-0 pinned last". Earlier authorities remain preserved transitively because the V9 bundle pins all 101 artifacts, including V11 at `14e95c44…a59da82d`.

**The record stays outside the bundle digest.** `publish_v9_planning_authority.py` raises `BUNDLE_INVENTORY_DRIFT` ("mutable gate or hold result") for any bundle inventory containing `implementation-hold-v1.json`. The existing V17 exclusion test verifies the hold record is absent from the V9 inventory and protected path sets. This correction changes none of those inventories.

**V13/V14 `nonClaims` are not a contradiction.** Both pin the literal string `"create implementation-hold-v1.json"` as something *they* do not do. V17 creating it is consistent; their bytes must remain untouched, asserted explicitly.

**Historical fail-closed default.** The V17-era design in `sprint-change-proposal-2026-08-04.md` required validator `PASS`, IR-0 `READY`, a valid `LIFTED` decision, and no later drift. Missing, stale or mismatched evidence evaluated to `ACTIVE`; no `ACTIVE_WITH_EXCEPTION` state existed in that contract. This historical description grants no current execution authority or new gate for this correction.

## Verification

Use frozen Python dependencies and explicit historical test selection. The
current directory lane excludes retired evidence suites; a directory-level pass
would not establish their coverage. Run the existing publisher suite and the
V17-specific routing tests, and report their measured counts. This audit does
not claim exhaustive fault coverage of the archived matrix.

Record exact commands and measured results for the publisher, historical C0/C2
verifier, current historical-verifier blocker, Git path/mode/hash checks, existing
C1/C2 commitlint, root-submodule inventory, and this spec's diff/link/frozen checks.

**Measured audit (2026-10-08).** Commands below ran from the repository root
with the frozen Python environment independently confirmed by
`uv sync --frozen --check`: exit 0, `Checked 11 packages`,
`Would make no changes`. The `--frozen --no-sync` execution flags alone do
not establish installed-environment provenance. External audit scripts, source snapshots,
commit-message files, and successful commitlint logs are retained in
`/tmp/conversations-v17-reconciliation-20261008-krycdovy/`; no evidence output
was added to the repository.

The worktree publisher, verifier, their two tests, both V17 schemas,
`_bmad/scripts/tests/conftest.py`, `pyproject.toml`, and `uv.lock` were each
compared byte-for-byte with their blobs at
`b3a813b0ba0020a558a3779e4e88b65612850e82`: **nine matches, exit 0**.
`source-provenance.json` retains their paths and SHA-256 values. These bytes
identify the executed worktree tooling separately from the evaluated C0/C2
and reconciliation candidates. The self-contained command below repeats this check.

**Git transaction and immutable sources.**
`python3 /tmp/conversations-v17-reconciliation-20261008-krycdovy/audit_git.py`
returned `PASS`, exit 0. It read the published inventory from the C2 blob,
checked single-parent ancestry using `git rev-list --parents -n 1`, compared
`git diff --name-only --no-renames -z` sets, and parsed
`git diff --raw --no-abbrev --no-renames -z` modes. C1 is C0's direct child;
C2 is C1's direct child. Every new path has mode `100644`; the two existing
verifier/test paths retain `100644`. Raw mode `160000` identified gitlinks.

| Range | Published inventory | Observed paths | Missing | Unexpected | Changed gitlinks |
|-------|---------------------|----------------|---------|------------|------------------|
| C0 to C1 | `publication.c1Paths` | 6 | `[]` | `[]` | `[]` |
| C1 to C2 | `publication.c2Paths` | 2 | `[]` | `[]` | `[]` |
| C0 to C2 | `publication.combinedPaths` | 8 | `[]` | `[]` | `[]` |

The six C1 paths are the schemas, publisher, publisher test, verifier, and
verifier test listed in Code Map. The two C2 paths are its published outputs.
All six `candidateFiles` hashes and modes independently match C1. For each
carried source below, the audit computed SHA-256 from `git show <revision>:<path>`
bytes and obtained its mode with `git ls-tree -z <revision> -- <path>` at C0,
C2, and the reconciliation baseline. All 21 observations match the declared
hash and mode `100644`. V16's six carried declarations match the corresponding
V17 entries; V17 adds V16 and retains IR-0 last.

| Carried source under `_bmad-output/planning-artifacts/` | SHA-256 at all three revisions |
|------------------------------------------------------|-------------------------------|
| `v9-authority-bundle-v1.json` | `8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3` |
| `v12-pre-ir0-remediation-authority-v1.json` | `c082cde6923e9831eea768be6c547ca1ab87ed91244185b505bdf3ae1c116dcc` |
| `v13-current-proof-authority-v1.json` | `f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f` |
| `v14-current-candidate-authority-v1.json` | `e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f` |
| `v15-planning-tooling-environment-authority-v1.json` | `bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea` |
| `v16-planning-tooling-lifecycle-authority-v1.json` | `5b71e6fbf8851f790af92f0a0d056d7f7e04b3b9749c3a2f3f4ef5f87312d45a` |
| `implementation-readiness-report-2026-08-22-ir-0.md` | `862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad` |

The V9 predecessor still has `planningCandidate`
`1e9a61126d3b7a55b514b7c7c8942d5af03355e5` and `bundleDigest`
`159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055`.
The original planning-entry-to-C2 range has 40 paths and six changed gitlinks:
EventStore, Folders, FrontComposer, Memories, Parties, and Projects. Those
intervening changes are outside the eight-path publication transaction.

**Deterministic publisher.** Both exact commands returned exit 0:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/publish_implementation_hold_decision.py --check --repository . --candidate 8bd6789eae34a322e06156dd8e7e2fb0aded42d7 --publication 8bd6789eae34a322e06156dd8e7e2fb0aded42d7
uv run --frozen --no-sync python3 _bmad/scripts/publish_implementation_hold_decision.py --check --repository . --candidate 5e4abc6f87692e634319c1d5612c91647ae6eae7 --publication 8bd6789eae34a322e06156dd8e7e2fb0aded42d7
```

Both emitted
`HOLD_DECISION_OK CANDIDATE=82b91c10fe95f8ef7a7576d35199322481e359a7 STATE=LIFTED UNLOCKS=7.1-SCHEMAS PATHS=8`.
The existing hold record and V17 authority reproduce byte-for-byte; their
SHA-256 values are respectively
`2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12` and
`1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50`.
The independent audit also found those bytes unchanged at C2, the
reconciliation baseline, the observed tip, and in the worktree. V17's stored
ledger has seven rows; the hold record has four, including the explicitly
`not-applicable` readiness-rerun row. This task ran no `--write` or publication.

**Historical verifier and current blocker.**

```bash
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf --candidate 8bd6789eae34a322e06156dd8e7e2fb0aded42d7
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 5e4abc6f87692e634319c1d5612c91647ae6eae7 --candidate 5e4abc6f87692e634319c1d5612c91647ae6eae7
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 5e4abc6f87692e634319c1d5612c91647ae6eae7 --candidate b3a813b0ba0020a558a3779e4e88b65612850e82
```

The C0-to-C2 command returned `PASS`, exit 0, with 23 assertion rows, the exact
eight-path changed set, and no changed gitlinks or blockers. This reproduces
the historical V17 route. The verifier envelope still reports `ACTIVE` and
false execution, owner-approval, release, and push flags; its successful
reproduction is not a current execution decision.

The two later commands each returned `BLOCKED`, exit 2, with one assertion
row and code `EVIDENCE_V24_ROOT_GITLINK_DRIFT`, message
`V23, V24, and evaluated gitlinks differ`. They report `ACTIVE` and all four
authority flags false. Their early-blocked envelopes have null baseline and
candidate fields; the exact evaluated revisions are recorded in the commands
above. This correction retains that blocker. Under the latest
[current-change policy](../../docs/runbooks/current-change-validation.md),
V19–V21 tooling and V23–V29 evidence enforcement are historical; this spec
explicitly requests reproduction, while routine new work uses focused current
checks. The policy changes no historical verdict or current hold decision.
The user's authorization applies the policy to this correction; this task
does not claim a fresh remote activation check.

**Focused tests.**

```bash
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_publish_implementation_hold_decision.py _bmad/scripts/tests/test_verify_evidence_boundary.py -k 'test_publish_implementation_hold_decision or v17 or multi_path_c2 or child_blocked_result_is_preserved'
uv run --frozen --no-sync python3 -m pytest --collect-only -q _bmad/scripts/tests/test_publish_implementation_hold_decision.py _bmad/scripts/tests/test_verify_evidence_boundary.py -k 'test_publish_implementation_hold_decision or v17 or multi_path_c2 or child_blocked_result_is_preserved'
```

The execution returned exit 0: **26 passed, 135 deselected**, with no failed,
skipped, xfailed, or xpassed tests, in 17.18 seconds. Explicit collection
identified 22 publisher cases and four verifier cases: both V17 route/inventory
tests, multi-path C2 compatibility, and preservation of a child `BLOCKED`
result. Deselection is the declared focused scope, not evidence that the
remaining 135 tests ran. These tests exercise existing publication, binding,
schema, path, history, and routing checks; they do not certify every original
matrix fault variant.

**Existing C1/C2 commit messages.** The pinned CLI reports
`@commitlint/cli@21.2.2`, matching the repository's package and lockfile pins.
The exact full messages were extracted with `git show -s --format=%B` for C1
and C2 into the external evidence directory. These commands each exited 0
with `found 0 problems, 0 warnings`:

```bash
./node_modules/.bin/commitlint --config commitlint.config.mjs --verbose < /tmp/conversations-v17-reconciliation-20261008-krycdovy/C1-commit-message.txt
./node_modules/.bin/commitlint --config commitlint.config.mjs --verbose < /tmp/conversations-v17-reconciliation-20261008-krycdovy/C2-commit-message.txt
```

`C1-commitlint.log`, `C2-commitlint.log`, and `commitlint-results.json` retain
successful command evidence. This task created no new commit message or commit.

To rerun commitlint without the session's temporary files, extract the exact
existing full messages directly from Git and pass them to the pinned CLI:

```bash
git show -s --format=%B 82b91c10fe95f8ef7a7576d35199322481e359a7 | ./node_modules/.bin/commitlint --config commitlint.config.mjs --verbose
git show -s --format=%B 8bd6789eae34a322e06156dd8e7e2fb0aded42d7 | ./node_modules/.bin/commitlint --config commitlint.config.mjs --verbose
```

**Documentation boundary and repository checks.**

```bash
python3 /tmp/conversations-v17-reconciliation-20261008-krycdovy/audit_documentation.py
python3 scripts/check-root-submodules.py --repository .
git diff --check
git diff 5e4abc6f87692e634319c1d5612c91647ae6eae7 -- _bmad-output/implementation-artifacts/spec-v17-implementation-hold-decision-authority.md
```

The supplementary documentation audit returns `PASS`, exit 0, and now reports
the actual frontmatter status, accepting `in-progress`, `in-review`, or `done`.
The path-selected diff contains exactly the spec, with missing and unexpected
sets both `[]`; this defines review scope rather than proving authorship. The
frozen block equals the reconciliation-baseline blob and pre-audit snapshot;
and all Markdown links and frontmatter context paths resolve. The frozen
block SHA-256 is
`0705e2d635085bbecd97953dde3076b861c4b827d7cf55cbe578fe343cb79c2b`.
The audit explicitly identifies the five-path entry-baseline-to-observed-tip
range as concurrent committed work and the separate uncommitted V21 spec and
runbook corrections as concurrent work reported in this session. Neither whole
range is claimed as spec-only. At review, the complete observed worktree
changed-path inventory was:

- `_bmad-output/implementation-artifacts/spec-v17-implementation-hold-decision-authority.md`
- `_bmad-output/implementation-artifacts/spec-v21-review-current-policy-disposition.md`
- `docs/runbooks/v21-review-disposition.md`

There were no observed untracked paths. The complete committed
reconciliation-baseline-to-observed-tip inventory was those three paths plus
`_bmad-output/implementation-artifacts/spec-publish-v20-story-7-1-release-owner-authority.md`
and `docs/runbooks/current-change-validation.md`. This task's patch actions
were confined to the V17 spec; Git-blob comparisons and its pre-audit snapshot
support the recorded immutable-source and frozen-block preservation. A selected
diff supplies no preservation proof for the concurrently edited V21 files.
Root inventory returned
`root submodules: PASS`, exit 0; whitespace validation exited 0. No historical
artifact, schema, publisher, test, IR-0 record, sprint row, gitlink, or
authorization flag was changed by this task's write actions. The readiness
rerun remains unmet; this task started no successor and made no push.

**Final reconciliation validation.** Both the portable command below and the
supplementary audit passed with `status: done`: 21 carried hash/mode observations,
nine tooling-blob matches, exact six/two/eight transaction sets, unchanged frozen
bytes, and resolved links/context. Root inventory and whitespace checks passed.
The two concurrent V21 files retained their SHA-256 values from the review
snapshot. Additional concurrent edits to the Story 9.2 spec and sprint-status
appeared during final validation; the audit reported them separately. This task
did not modify those paths. The final V17 documentation delta remains uncommitted.

**Self-contained reproduction.** Run the following command from a fresh
checkout with the named Git history available. It needs only Python's standard
library and Git. It checks the transaction and immutable sources, pins the
executed tooling to the observed tip, and checks frozen bytes, links, and
context paths. The complete changed-path inventory and selected review scope
are reported separately; a clean checkout is valid. Temporary scripts and
snapshots above are supplementary measured evidence, not prerequisites.

```bash
python3 - <<'PY'
from pathlib import Path
import hashlib, json, re, subprocess

root = Path(subprocess.check_output(
    ['git', 'rev-parse', '--show-toplevel'], text=True).strip())
def git(*args):
    return subprocess.check_output(['git', *args], cwd=root)
def blob(revision, path):
    return git('show', f'{revision}:{path}')
def mode(revision, path):
    header, observed = git('ls-tree', '-z', revision, '--', path).rstrip(b'\0').split(b'\t')
    assert observed.decode() == path
    return header.split()[0].decode()
def paths(*args):
    return [p.decode() for p in git(*args).split(b'\0') if p]
def digest(data):
    return hashlib.sha256(data).hexdigest()
def frozen(data):
    begin = data.index(b'<frozen-after-approval')
    end = data.index(b'</frozen-after-approval>') + len(b'</frozen-after-approval>')
    return data[begin:end]

c0 = '074c5b7afb95dfb6365d62a9afa93b4ef75e6fcf'
c1 = '82b91c10fe95f8ef7a7576d35199322481e359a7'
c2 = '8bd6789eae34a322e06156dd8e7e2fb0aded42d7'
entry = '5e4abc6f87692e634319c1d5612c91647ae6eae7'
tooling = 'b3a813b0ba0020a558a3779e4e88b65612850e82'
spec = '_bmad-output/implementation-artifacts/spec-v17-implementation-hold-decision-authority.md'
authority_path = '_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json'
authority_bytes = blob(c2, authority_path)
assert digest(authority_bytes) == '1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50'
authority = json.loads(authority_bytes)
for child, parent in ((c1, c0), (c2, c1)):
    assert git('rev-list', '--parents', '-n', '1', child).decode().split() == [child, parent]
for base, tip, key, count in ((c0, c1, 'c1Paths', 6),
                             (c1, c2, 'c2Paths', 2),
                             (c0, c2, 'combinedPaths', 8)):
    expected = authority['publication'][key]
    observed = paths('diff', '--name-only', '--no-renames', '-z', base, tip)
    missing, unexpected = sorted(set(expected) - set(observed)), sorted(set(observed) - set(expected))
    print(key, 'missing=', missing, 'unexpected=', unexpected)
    assert len(expected) == len(set(expected)) == len(observed) == len(set(observed)) == count
    assert not missing and not unexpected
    fields = git('diff', '--raw', '--no-abbrev', '--no-renames', '-z', base, tip).split(b'\0')
    assert len(fields) == 2 * count + 1 and fields[-1] == b''
    raw_paths, gitlinks = [], []
    for index in range(0, len(fields) - 1, 2):
        old_mode, new_mode = fields[index].decode().split()[:2]
        old_mode = old_mode.removeprefix(':')
        path = fields[index + 1].decode()
        raw_paths.append(path)
        if '160000' in (old_mode, new_mode):
            gitlinks.append(path)
        assert old_mode in ('000000', '100644') and new_mode == mode(tip, path) == '100644'
    assert set(raw_paths) == set(observed) and not gitlinks
    print(key, 'rawModes=100644', 'changedGitlinks=', gitlinks)
assert len(authority['immutableAuthorities']) == 7
for row in authority['immutableAuthorities']:
    for revision in (c0, c2, entry):
        assert digest(blob(revision, row['path'])) == row['sha256']
        assert mode(revision, row['path']) == row['mode'] == '100644'
sources = authority['publication']['c1Paths'] + [
    '_bmad/scripts/tests/conftest.py', 'pyproject.toml', 'uv.lock']
assert len(sources) == len(set(sources)) == 9
for path in sources:
    assert (root / path).read_bytes() == blob(tooling, path), path
print('carriedHashModeChecks=21', 'sourceBlobMatches=9', 'tooling=', tooling)

current = (root / spec).read_bytes()
assert frozen(current) == frozen(blob(entry, spec))
assert digest(frozen(current)) == '0705e2d635085bbecd97953dde3076b861c4b827d7cf55cbe578fe343cb79c2b'
text = current.decode()
frontmatter = text.split('---', 2)[1]
status = re.search(r"^status: ['\"]?(in-progress|in-review|done)['\"]?$", frontmatter, re.M)
assert status
assert "baseline_commit: '94dbb37694747e9dedee20591b84b5d6a69d19b3'" in frontmatter
assert f"reconciliation_baseline_commit: '{entry}'" in frontmatter
prose = re.sub(r'(?ms)^```.*?^```[ \t]*$', '', text)
links = re.findall(r'\[[^\]]+\]\(([^)]+)\)', prose)
for target in links:
    assert not target.startswith('/')
    resolved = (root / spec).parent.joinpath(target.split('#', 1)[0]).resolve()
    assert resolved.is_relative_to(root) and resolved.is_file(), target
context = re.findall(r"^  - '\{project-root\}/([^']+)'$", frontmatter, re.M)
assert len(context) == 3 and all((root / path).is_file() for path in context)
changed = paths('diff', '--name-only', '--no-renames', '-z', 'HEAD')
untracked = paths('ls-files', '--others', '--exclude-standard', '-z')
print(json.dumps({'result': 'PASS', 'status': status.group(1),
    'observedChangedPaths': sorted(set(changed)), 'observedUntrackedPaths': sorted(set(untracked)),
    'reviewScopePaths': sorted(set(changed) & {spec}),
    'resolvedMarkdownLinks': links, 'resolvedContextPaths': context}, indent=2))
PY
```
