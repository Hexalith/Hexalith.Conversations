# Story 7.2 completion resume — 2026-09-27

Story 7.2 remains **in-progress**. The historical final record reproduces byte for byte at the requested resume commit. Current-candidate verification and the separate Story 7.1 terminal-authority prerequisite do not pass. This audit grants no authority and preserves the existing implementation hold.

## Scope and immutable inputs

- Requested resume: `28d7b6b677e5c5c652b58dab27278d7426222bb3`.
- Main at investigation start: `30fffc2cd427e837c719b87cb33475e34a6fc8de`; clean working tree.
- Recorded Story 7.2 candidate: `170ac9d2e8afa686e4203c44ad5bef9414d19a89`.
- Frozen Story 7.2 baseline: `c69334cb13a981c9112ad687427b1f43fafc2988`.
- The later main commit changes only `references/Hexalith.EventStore`, from `3942057de12863d62cf779d85b7350aeed272211` to `02cf007c9860326aa03b32a78541a00b5717bd4a`. It is preserved.
- Repair scope: bmad-build step 05 and its operator guidance. The v1/v2 generator, schemas, contracts, prior records, authority publications, and gitlinks are unchanged. Other completion routes remain Story 7.3 work.

## Historical byte reproduction

Created a detached root-only worktree at the full requested resume commit; no submodules were initialized, updated, or traversed. For 3,640 byte-identical tracked files, copied the existing workspace filesystem metadata with `shutil.copystat`; zero tracked regular files differed in content. Copied `artifacts/v9/7.2` with `shutil.copytree`/`copy2`, retaining actual result bytes and timestamps. This avoids a fresh checkout timestamp falsely aging preserved test evidence; no timestamps were invented and no tests were rerun over archived results. The isolated root working tree was clean before generation.

All eight TRX SHA-256 values matched the committed record before invocation. A subsequent preservation check also verified all ten bound JUnit hashes: all 18 files retained identical bytes and original timestamps in the root checkout and isolated reproduction. From that isolated worktree, invoked the existing pinned Python environment twice:

```bash
/home/administrator/projects/hexalith/conversations/.venv/bin/python3 _bmad/scripts/generate_story_record.py \
  --repository . \
  --contract _bmad-output/planning-artifacts/v9/story-contracts/7.2.json \
  --format bundle \
  --output-json docs/release-evidence/story-7.2-final-record-v2.json \
  --output-markdown docs/release-evidence/story-7.2-final-record-v2.md
```

Both runs exited `0`. Both JSON and Markdown outputs equaled the raw `git show <resume-commit>:<path>` bytes; stdout also equaled the committed JSON bytes on both runs. The schema-valid record reports `11/11/0/0/0/0`, ten root gitlinks, and 2,026 executed/passed tests across eight projects, with zero failed or skipped tests. These are preserved historical measurements, not a new product-test run.

| Committed artifact | SHA-256 of exact file bytes |
| --- | --- |
| `docs/release-evidence/story-7.2-final-record-v2.json` | `15212a33103bf36ccefb3778853d3e4a8875d1c9e82a0c3d3104dae54e361ef5` |
| `docs/release-evidence/story-7.2-final-record-v2.md` | `0821f9b934506a87104279e67f51441ae3e9bbbad7a71cf3f3c1ab0786770e81` |

The JSON file hash above is the hash of the complete file, distinct from its embedded self-excluding content digest. Original main-worktree evidence and results were left unchanged.

## Current v2 gate

Before editing, ran:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py \
  --repository . \
  --contract _bmad-output/planning-artifacts/v9/story-contracts/7.2.json \
  --format bundle \
  --output-json docs/release-evidence/story-7.2-final-record-v2.json \
  --output-markdown docs/release-evidence/story-7.2-final-record-v2.md
```

At `30fffc2cd427e837c719b87cb33475e34a6fc8de`, exit `1` / `FAIL`: `CANDIDATE_NOT_FINAL`, `GITLINK_DRIFT`; diagnostic identifies `references/Hexalith.EventStore`. The committed pair pins the earlier candidate; the later gitlink change invalidates current completion. The generator preserved both outputs. No record retraction, regeneration, candidate-rule relaxation, or gitlink rollback was performed.

## Historical V24 blocker investigation

For **each** of the two full revisions below:

```text
28d7b6b677e5c5c652b58dab27278d7426222bb3
30fffc2cd427e837c719b87cb33475e34a6fc8de
```

Ran these commands with `<revision>` replaced by that full identifier:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py \
  --repository . --candidate <revision> \
  --baseline c69334cb13a981c9112ad687427b1f43fafc2988
uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py \
  --repository . --candidate <revision> --check
```

Boundary: exit `2` / `BLOCKED`, `EVIDENCE_V24_ROOT_GITLINK_DRIFT`, one nonempty blocked ledger row. Resolver: exit `2` / `BLOCKED`, `V24_ROOT_GITLINK_DRIFT`, one nonempty blocked ledger row. Both report “V23, V24, and evaluated gitlinks differ”, `effectiveHold: ACTIVE`, `implementationHold: ACTIVE`, and all four flags false (`ownerApprovalClaimed`, `releaseAuthorized`, `pushAuthorized`, `executionAllowed`). No trusted-host value was supplied or inferred from HEAD.

The complete assertion ledgers below were identical at both evaluated revisions; their command-to-revision bindings are the two explicit command/revision pairs above.

Boundary ledger:

```json
[
  {
    "id": "EVIDENCE_V24_ROOT_GITLINK_DRIFT",
    "subject": "evidence-boundary",
    "state": "BLOCKED",
    "message": "V23, V24, and evaluated gitlinks differ"
  }
]
```

Authority resolver ledger:

```json
[
  {
    "detail": "V23, V24, and evaluated gitlinks differ",
    "id": "V24_ROOT_GITLINK_DRIFT",
    "state": "BLOCKED",
    "subject": "current-authority-resolution"
  }
]
```

The check at `_bmad/scripts/verify_evidence_boundary.py:1263` compares the V23 request publication (`5a7234b922371b5d0a12085a444d93783263f278`), V24 correction publication (`20e2cdd2b37e6387055b241c7ee45fc54e836742`), and evaluated candidate's raw root gitlinks. The first two have identical maps with ten entries. At investigation-start main, eight object IDs differ:

| Root gitlink | V23 and V24 object ID | Investigation-start main object ID |
| --- | --- | --- |
| `references/Hexalith.Builds` | `2fba3497043fe5ffcfe4dc44c51a09eae9b950ab` | `0610f7837221c9859e280f06f333d7452a40f5b1` |
| `references/Hexalith.Commons` | `9f4809d37095e64e3732abc3e766535f9e836563` | `37455bbb258d30e3ee86aa4b09769ae6e5638f9f` |
| `references/Hexalith.EventStore` | `4bc61d9a60fae13a65f23963c1cb731065222f2a` | `02cf007c9860326aa03b32a78541a00b5717bd4a` |
| `references/Hexalith.Folders` | `b10971ae6e17d4fca2933908531f80a5f58b6e5a` | `18cf524bc4e6943b5141afb9a79b976abce5fb2b` |
| `references/Hexalith.FrontComposer` | `237b39e89cb50d8d37596f4da9ce3f6993260165` | `812de2fefc56950b26beb4babc6905cd08c2bc3e` |
| `references/Hexalith.Parties` | `14d249fde316b0002aec84351d7a7cdf953d1d30` | `c069af07af5f1e42bdfa1287705d943b8b4cadf7` |
| `references/Hexalith.Projects` | `ffa922d0f7a278da3bb02ee09769564df02d5f01` | `6bc515555cd207920dc7e357e2ce311ed11cb83e` |
| `references/Hexalith.Tenants` | `dfd84f4b9c93779f253672096fec5d717d6afc91` | `49ca5b46c6cab066b5a939a5551a27aa8db1e541` |

`Hexalith.AI.Tools` and `Hexalith.Memories` remain equal. The exact comparison explains the historical blocker; it is not a missing declaration or evidence that submodules must be initialized. `python3 scripts/check-root-submodules.py --repository .` exits `0`, `root submodules: PASS`, on current main. That current-tree result does not retroactively clear the frozen V24 comparison. The current-change runbook preserves V23–V29 history and forbids inventing another successor solely for routine path drift; this request explicitly requires reproducing the historical gate.

## Story 7.1 terminal authority

Terminal `ACCEPTED` authority could **not** be established. Architecture AD-4 (`architecture.md:2888`) requires an atomic terminal authority/pointer publication binding the accepted main commit, verified integration tree and gitlinks, and final-record digest. `epics.md:384` and the Story 7.2 spec keep that prerequisite separate from a raw passing record or `done` row.

Examined 111 committed JSON documents under `_bmad-output/planning-artifacts` and `docs/release-evidence` at investigation-start main. The only exact `ACCEPTED` values occur in the Epic 6 supersession contract enums and Epic 6 current-proof decision, not Story 7.1 terminal authority. This content inventory is supporting evidence, not an authority resolver. The resolver's blocked result above is the decisive failure to establish authority. The committed V29 record still says `implementationHold: ACTIVE` with all four authority flags false; V21's historical `LIFTED` field is not current authorization.

No terminal transition is made. Story 7.1's existing lifecycle and records, Story 7.2's `in-progress` state, and the active hold remain preserved.

## Repair verification and review

The initial full generator regression run reported 170 passes and one failure in `test_both_skill_trees_stay_byte_identical_for_every_changed_file`. Synchronizing the same step-05 repair to its Claude twin fixed that failure; the focused test then passed. The final full run after all review corrections passed:

```text
Command: uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py
........................................................................ [ 42%]
........................................................................ [ 84%]
...........................                                              [100%]
171 passed in 66.53s (0:01:06)

Exit: 0
```

Focused parity command:

```bash
uv run --frozen --no-sync python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py::test_both_skill_trees_stay_byte_identical_for_every_changed_file
```

Final focused result: exit `0`, `1 passed in 0.10s`. No archived JUnit or TRX path was used for these regression runs.

Rendered using the repository's renderer, then inspected the resulting step rather than relying only on source text:

```bash
uv run --no-cache /home/administrator/projects/hexalith/conversations/_bmad/scripts/render_skill.py --project-root /home/administrator/projects/hexalith/conversations --skill /home/administrator/projects/hexalith/conversations/.agents/skills/bmad-build
```

```text
PASS: rendered v2 command exactly matches AC-7.2-11 under pinned uv; route and terminal-gate ordering verified; configuration/snapshot references resolved; tracked twins byte-identical.
Rendered step: /home/administrator/projects/hexalith/conversations/_bmad/render/bmad-build/conversations-f0fc90326eb5/37926d32263d5fa3ef98/step-05-present.md
Rendered step SHA-256: c90e906aea2309fc3cab2f40f19f6778cf0130021a0e2a3685a9d9dd2da47486
Source twin SHA-256: 79d420e6604e2c91b0117ac9a99a52a3da5b1c2fa20e5e1571e13c7d3aa58cb7
```

The source step's command was compared to the contract after shell line-continuation normalization. Both output schema/identity/summary checks, independent terminal gates, and the legacy-route exclusion were inspected. Root-submodule validation and `git diff --check` passed. Both Story 7.1/7.2 committed pairs, all planning artifacts, sprint status, and the 18 bound result files remain unchanged; the pair's internal digest cross-bindings also verify.

Three independent review lenses completed. Edge-case and verification-gap reviewers found no issues. Seven blind-review documentation findings were corrected: timestamp restoration, exclusion of generic regeneration instructions, durable regression/render/ledger evidence, complete AD-4 bindings, and the scope of archived-byte comparison. The per-finding triage is retained in the Story 7.2 spec. These repairs do not resolve or waive the current-candidate, V24, or predecessor-authority blockers.

## Commit-message validation

The exact candidate message was validated with installed `@commitlint/cli` **21.2.2**, matching the repository pin:

```text
fix(workflow): route story 7.2 completion through v2 contract

Preserve the historical record and active hold; keep Story 7.2 in-progress while current candidate and predecessor authority gates remain blocked.
```

```text
Command: node_modules/.bin/commitlint --config commitlint.config.mjs --edit /tmp/story-7-2-completion-6tqeteal/commit-message.txt --verbose
Pinned CLI: 21.2.2
⧗   --- input ---
fix(workflow): route story 7.2 completion through v2 contract

Preserve the historical record and active hold; keep Story 7.2 in-progress while current candidate and predecessor authority gates remain blocked.
✔   found 0 problems, 0 warnings
Exit: 0
```

The commit is local only; no push is authorized or performed.
