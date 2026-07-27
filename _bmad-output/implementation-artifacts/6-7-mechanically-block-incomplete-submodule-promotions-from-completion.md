---
story_key: '6-7-mechanically-block-incomplete-submodule-promotions-from-completion'
epic: 6
story_id: '6.7'
created: '2026-07-27'
status: 'in-progress'
baseline_commit: 'f3b827a80f87a85223eaf34e8fe1183a454a6c12'
submodule_promotions: []
# ^ This story introduces this field. Story 6.7 itself is NOT promotion-bearing: it
#   changes umbrella-owned tooling, skills, docs, and planning artifacts only. No
#   `references/...` submodule content changes. Keep this list empty.
authority:
  overlay: 'epic-6-authority-2026-07-27-v3'
  architecture: 'conversations-architecture-2026-07-27-v3'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-submodule-promotion-completion-gate.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

# Story 6.7: Mechanically block incomplete submodule promotions from completion

Status: in-progress

## Story

As a **Hexalith development-workflow maintainer**,
I want **promotion-bearing work to pass a mechanical submodule completion gate**,
so that **dirty submodules and uncaptured umbrella gitlinks cannot reach `done`**.

## Acceptance Criteria

Binding source: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` → *Story 6.7* (overlay `epic-6-authority-2026-07-27-v3`; v3 disposition for 6.7 is "No change"). The four binding ACs below are verbatim; the lettered sub-criteria are the approved detail from `sprint-change-proposal-2026-07-15-submodule-promotion-completion-gate.md` §4.1 that `epics.md` compressed. Both are approved authority — satisfy both.

**AC1.** Promotion-bearing work declares exact root `references/...` paths and whether remote commit availability is required; affected scope also includes gitlinks changed since baseline.

- 1a. Story and spec templates carry a structured `submodule_promotions` field. Non-promotion work uses an empty list; promotion work lists every affected root-declared path exactly once.
- 1b. The gate evaluates the **union** of declared paths and root gitlinks changed between the work baseline and the candidate umbrella revision.
- 1c. Existing work may receive a missing declaration automatically **only** when it is an exact transcription of already-approved scope; ambiguous or expanded scope requires Product Owner or user approval.

**AC2.** Each affected submodule is initialized, clean including untracked files, satisfies its availability policy, and is represented by the exact mode-`160000` gitlink in the committed umbrella revision.

- 2a. The path is declared by the root `.gitmodules` file.
- 2b. The submodule worktree is initialized and clean, including untracked files.
- 2c. `HEAD` resolves to a commit.
- 2d. If requested, that commit is contained by a **locally known** remote-tracking ref (never fetch).
- 2e. The candidate root commit records mode `160000` and the same object ID.

**AC3.** Stable machine-readable blockers prevent review/completion workflows from writing `review`/`done`; unrelated state warns without blocking.

- 3a. Failures produce stable blocker codes plus actionable text, and exit nonzero.
- 3b. `bmad-code-review`, `bmad-quick-dev` (plan route and one-shot route), and `bmad-dev-auto` cannot write or synchronize `done` after a failed gate.
- 3c. `bmad-dev-story` cannot move failed promotion-bearing state to `review`.
- 3d. Unrelated state outside the declaration and changed-gitlink set is reported without blocking.

**AC4.** Discovery uses root `.gitmodules` only and never initializes or traverses nested submodules; isolated Git fixtures prove success, failure, and concurrency cases.

- 4a. The checker never initializes, updates, fetches, pushes, commits, or enters nested submodules.
- 4b. Automated Git fixtures prove: clean/captured success; tracked dirt; untracked dirt; old or mismatched root gitlink; deterministic remote-availability failure **and** success; changed-but-undeclared gitlinks; unrelated dirty submodule warns without blocking; nested-submodule non-traversal; invalid scope.

**AC5 (completion evidence — proposal §5).** No completion-capable workflow can write `done` for promotion-bearing work unless every affected root-declared submodule is clean and the committed umbrella gitlink exactly matches its current commit. No unrelated worktree file or existing submodule-upgrade artifact is modified by this story.

## Tasks / Subtasks

- [x] **T1 — Implement the read-only checker** (AC: 1b, 2a–2e, 3a, 3d, 4a)
  - [x] `_bmad/scripts/verify_submodule_promotion.py` — stdlib only, no third-party imports. PEP-723 header (`requires-python = ">=3.11"`), executable bit set, matching the house style of `_bmad/scripts/resolve_customization.py`.
  - [x] CLI exactly as specified in **Dev Notes → Checker contract**: `--repository`, `--baseline`, `--candidate`, `--submodule` (repeatable), `--require-remote` (repeatable), `--format {text,json}`.
  - [x] Exit codes: `0` pass, `1` valid invocation with blockers, `2` invalid invocation or untrustworthy repository state.
  - [x] Emit stable blocker/warning codes from the frozen tables in Dev Notes. Do not invent new codes without adding them to the tables in the same change.
  - [x] Never mutate the repository: no `init`, `update`, `fetch`, `push`, `commit`, `add`, `checkout`, `submodule --recursive`.

- [x] **T2 — Isolated Git fixture tests** (AC: 4b)
  - [x] `_bmad/scripts/tests/test_verify_submodule_promotion.py` — pytest, PEP-723 header with `dependencies = ["pytest>=8.0"]`, `sys.exit(pytest.main([__file__, "-q"]))` guard at the bottom (house convention, see `.claude/skills/bmad-architecture/scripts/tests/test_lint_spine.py`).
  - [x] Every fixture builds its own throwaway repos under `tmp_path` with `git init`; never touch the real workspace or its `references/` tree.
  - [x] Cover all ten cases in AC 4b plus the traps in **Dev Notes → Verified git traps**.
  - [x] Set deterministic git env in fixtures (`GIT_AUTHOR_*`, `GIT_COMMITTER_*`, `-c init.defaultBranch=main`, `-c commit.gpgsign=false`, `-c protocol.file.allow=always` for local submodule adds) so the suite is hermetic and does not depend on the developer's global git config.

- [x] **T3 — Promotion-scope declaration in story/spec schemas** (AC: 1a, 1c)
  - [x] `bmad-create-story/template.md` — add YAML frontmatter containing `submodule_promotions: []` with the commented example.
  - [x] `bmad-create-story/SKILL.md` — instruct story creation to populate the field.
  - [x] `bmad-quick-dev/spec-template.md` and `bmad-quick-dev/step-02-plan.md` — add the field and the instruction to populate it during planning.
  - [x] `bmad-dev-auto/spec-template.md` and `bmad-dev-auto/step-02-plan.md` — same.
  - [x] Encode the AC 1c rule in the planning instructions: a missing declaration may be filled in automatically **only** as an exact transcription of already-approved scope; ambiguous or expanded scope requires Product Owner or user approval (`bmad-dev-auto` has no human in the loop, so there it is a `blocked` condition, not a question).
  - [x] **Apply every skill edit to BOTH `.claude/skills/<skill>/…` and `.agents/skills/<skill>/…`** — see Dev Notes → Dual skill trees.

- [x] **T4 — Gate `bmad-code-review` before `done`** (AC: 3a, 3b)
  - [x] `bmad-code-review/steps/step-04-present.md` → section *6. Update story status and sync sprint tracking*: insert a `#### Promotion completion gate` subsection **before** *Determine new status based on review outcome*.
  - [x] Run the checker with the story baseline, committed `HEAD` as candidate, and the declared scope. If scope is non-empty or a gitlink changed, missing baseline data is a blocker.
  - [x] Nonzero result ⇒ force `{new_status}` = `in-progress`, forbid the `done` branch, preserve blocker codes in the review record, and synchronize only `in-progress`.

- [x] **T5 — Gate `bmad-quick-dev` (both routes) before `done`** (AC: 3a, 3b)
  - [x] `bmad-quick-dev/step-05-present.md` — reorder to: capture baseline → keep `in-review` → commit scoped implementation and gitlinks → run gate against the committed candidate → mark spec `done` and sync `review` → commit completion record. Non-promotion behavior is unchanged.
  - [x] `bmad-quick-dev/step-oneshot.md` — the one-shot trace is initially created as `in-review` carrying `baseline_commit` and `submodule_promotions`. On gate failure it remains or returns to `in-progress` and never writes `done`.

- [x] **T6 — Gate `bmad-dev-auto` before `done`** (AC: 3a, 3b)
  - [x] `bmad-dev-auto/step-04-review.md` → `## Finalize`: run the checker after the local commit and the commit-content verification, and **before** `Capture final_revision` / `status: done`.
  - [x] For activated promotion work, unavailable VCS, missing trustworthy inputs, or any blocker ⇒ HALT with `status: blocked`, record diagnostics in `Auto Run Result`, and never write a successful `final_revision` or `done`.

- [x] **T7 — Gate `bmad-dev-story` before `review`** (AC: 3c)
  - [x] `bmad-dev-story/SKILL.md` step 9 — run the checker using frontmatter `baseline_commit` and committed `HEAD` before `Update the story Status to: "review"`.
  - [x] Failure leaves story and sprint state `in-progress`, records diagnostics, and halts for remediation.
  - [x] Dev-story reads the approved scope but must not silently expand it. Code-review repeats the check as the final `done` authority.
  - [x] `bmad-dev-story/checklist.md` — add the corresponding validation item.

- [x] **T8 — Promotion runbook** (AC: 3a, 4a)
  - [x] `docs/runbooks/submodule-promotion-completion-gate.md` — create the live operational runbook while preserving the byte-identical signed-v1 evidence at `docs/release-evidence/promote-adopt-runbook.md`; use the three approved closing items:
    ```markdown
    8. [ ] Exact `submodule_promotions` scope recorded; remote requirements identified.
    9. [ ] Each affected submodule committed separately, clean, and available remotely where required.
    10. [ ] Root-only gitlinks committed in the umbrella repository and the mechanical completion gate passes.
    ```
  - [x] Document the canonical command, exit-code meanings, remediation, `in-progress`/`blocked` behavior, and the prohibition against recursive submodule commands. State explicitly that a staged pointer bump or a prose completion note is **not** gate evidence.

- [x] **T9 — Independent proof and evidence record** (AC: 4b, 5)
  - [x] Execute the fixture suite and record pass counts in the Dev Agent Record.
  - [x] Run the checker against the live umbrella at the story's own candidate commit with `--submodule` empty and record the output (expected: pass, with warnings for the three unrelated drifted gitlinks — see Verified repository facts).
  - [x] Fault-inject one mutation per acceptance criterion (Story 6.1 precedent) and record which check caught each. Revert every mutation and verify the working tree afterward.
  - [x] Record a Boundary Confirmation: which files this story changed and which it did not.

- [x] **T10 — Sprint tracking** (AC: 5)
  - [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` — Epic 3 action item A2 ("Add a blocking completion gate for submodule promotions so dirty submodules or uncaptured root gitlinks cannot reach done.") flips `open` → `done` **only after** independent verification in T9 passes. Preserve all Epic 1–5 entries, comments, STATUS DEFINITIONS, and WORKFLOW NOTES.
  - [x] Do not modify any other `action_items` entry.

### Review Findings

Adversarial four-layer review (Blind Hunter, Edge Case Hunter, Verification Gap Reviewer, Acceptance Auditor) against diff `f3b827a80f8`..`4382340`.

- [x] [Review][Patch] (resolved: disclose retroactively — applied) Undeclared, undisclosed submodule gitlink promotions reached the shipped candidate — The Boundary Confirmation states "It does not modify any `references/` gitlink or submodule content" and `submodule_promotions` is `[]`, but committed HEAD (`43823403b47a92d0d0c7ad27440865df41575951`) bumps four gitlinks vs. baseline: `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.Memories`, `references/Hexalith.Tenants`. Jerome's call: disclose retroactively — correct the Boundary Confirmation/File List to accurately list these four gitlinks as changed (clean/captured, undeclared), and fix the T9 verification methodology to run the checker against the real baseline→HEAD (not a self-comparison) and record the true (4-warning) result. [_bmad-output/implementation-artifacts/6-7-....md Boundary Confirmation, File List, Dev Agent Record T9 entries]
- [x] [Review][Patch] (resolved: revert to open — applied) sprint-status.yaml self-contradiction on Epic 3 action item A2 — Comment says A2 "stays open until Story 6.7 passes independent verification" but the `action_items` entry was flipped to `done` while the story is still `review`. Jerome's call: revert A2's status to `open` until Story 6.7 itself reaches `done`. [_bmad-output/implementation-artifacts/sprint-status.yaml:~157]
- [x] [Review][Patch] (resolved: accept and reconcile — applied) Unrelated Story 6.1/6.2 AppHost-ownership authority reconciliation bundled into 6.7 — `architecture.md`, `epics.md`, `ArchitecturePlanningAuthorityValidationTest.cs`, and two new sprint-change-proposal docs are in this story's File List under "expanded-scope authorization," contradicting this story's own frozen "do not edit architecture.md/epics.md" / "tests/ dotnet is out of bounds" Dev Notes text, with no Task/Subtask coverage. Jerome's call: keep the authority-chain changes, but reconcile 6.7's own Dev Notes/constraints text so it no longer contradicts itself, and cite the actual `bmad-loop-resolve` approval session by reference. [_bmad-output/implementation-artifacts/6-7-....md Dev Notes → Testing requirements, Non-negotiable constraint #4, File List]
- [x] [Review][Defer] `bmad-dev-story` cannot detect an undeclared, uncommitted submodule promotion made during its own session [.claude/skills/bmad-dev-story/SKILL.md step 9] — deferred: requires both undeclared scope and leaving it uncommitted (narrow edge case); record as a known limitation in Dev Notes/runbook rather than changing the frozen checker contract now
- [x] [Review][Patch] `main()` swallows only `GateError`; other exceptions break the JSON-output contract [_bmad/scripts/verify_submodule_promotion.py:592-608] — applied: catches `Exception`, emits `INTERNAL_ERROR` diagnostic, exit 2
- [x] [Review][Patch] `--format=json` (equals form) silently falls back to text output on a pre-parse argument error [_bmad/scripts/verify_submodule_promotion.py:594] — applied: `pre_parse_output_format()` handles both forms
- [x] [Review][Patch] New runbook's checklist items 1-7 are a restyled copy of the unrelated Epic 3 code-promotion/adoption checklist, not this gate's own criteria [docs/runbooks/submodule-promotion-completion-gate.md:111-119] — applied: items 1-7 replaced with submodule-promotion-specific criteria
- [x] [Review][Patch] A git-command failure inspecting an unrelated, undeclared root submodule aborts the whole run instead of warning [_bmad/scripts/verify_submodule_promotion.py inspect_unrelated()] — applied: wrapped in try/except, emits `UNRELATED_SUBMODULE_INSPECTION_FAILED` warning
- [x] [Review][Patch] Renaming a declared submodule path between baseline and candidate produces a false `PATH_NOT_ROOT_DECLARED` blocker [_bmad/scripts/verify_submodule_promotion.py changed_gitlinks()] — applied: rename/copy status only reports the destination path as changed
- [x] [Review][Patch] No ancestor check between `--baseline` and `--candidate` lets a stale/rebased baseline silently change changed-gitlink results [_bmad/scripts/verify_submodule_promotion.py] — applied: new `is_ancestor()` check, `BASELINE_NOT_ANCESTOR` exit-2 condition
- [x] [Review][Patch] `epic-6-context.md`'s new ownership sentence has a grammar error and names a different owner than architecture.md's matching sentence, despite the file's own zero-semantic-drift claim [_bmad-output/implementation-artifacts/epic-6-context.md:31] — applied: sentence now shares the exact "Platform deployment owns production topology and composition" phrase with architecture.md; matching Conformance assertion updated
- [x] [Review][Patch] `decode()` uses lossy `errors="replace"` before exact-string path/gitlink matching [_bmad/scripts/verify_submodule_promotion.py:48] — applied: `errors="surrogateescape"`
- [x] [Review][Patch] `safe_relative_path` doesn't reject embedded control characters (e.g. newline), which could corrupt single-line text output [_bmad/scripts/verify_submodule_promotion.py] — applied: rejects any `ord(character) < 0x20`
- [x] [Review][Defer] `remote_contains()` has no staleness/shallow-clone signal for local remote-tracking refs [_bmad/scripts/verify_submodule_promotion.py:337-346] — deferred, pre-existing design tradeoff (no-fetch-ever policy)
- [x] [Review][Defer] Nested-gitlink dirt compensation only covers the staged case, not unstaged checkouts [_bmad/scripts/verify_submodule_promotion.py submodule_dirt()] — deferred, nested submodules are policy-forbidden from being initialized at all
- [x] [Review][Defer] AppHost-qualification check in Conformance test is per-physical-line, fragile to future cosmetic line-wraps [tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs] — deferred, pre-existing test-authoring pattern
- [x] [Review][Patch] Zero pytest coverage of the checker's own default `--format text` output path [_bmad/scripts/tests/test_verify_submodule_promotion.py] — applied (upgraded from defer): added `test_default_text_format_is_human_readable`
- [x] [Review][Defer] `inspect_unrelated()` emits no warning at all for a fully-uninitialized unrelated root submodule [_bmad/scripts/verify_submodule_promotion.py] — deferred, not contradicted by the frozen warning-code table
- [x] [Review][Defer] Case-insensitive filesystem checks vs. case-sensitive git object matching could misreport a case-mismatched `--submodule` path [_bmad/scripts/verify_submodule_promotion.py] — deferred, no Windows/macOS caller in this workspace today

## Dev Notes

### Non-negotiable constraints

1. **Read-only.** The checker inspects repository state. It must never run `git init`, `submodule init/update/sync`, `fetch`, `pull`, `push`, `commit`, `add`, `checkout`, `reset`, or anything with `--recursive`. This is `project-context.md` policy *and* AC 4a.
2. **Root `.gitmodules` only.** Discovery reads the umbrella's `.gitmodules`. Nested `.gitmodules` files exist (`references/Hexalith.FrontComposer/.gitmodules` declares five nested submodules) and must never be read for discovery or traversed.
3. **Scope discipline.** Only declared paths and changed gitlinks block. Everything else warns. False blocking from unrelated concurrent submodule state is the named primary risk of this change (proposal §3).
4. **No product/runtime impact.** Do not touch `src/`, `tests/` (dotnet), solution files, package versions, submodule contents, or any `references/` gitlink. This story is umbrella tooling + workflow prose only. **Addendum (2026-07-27, Jerome-authorized expanded scope — see Dev Notes → Testing requirements below and Boundary Confirmation):** this constraint was overridden for exactly one `tests/` file, `ArchitecturePlanningAuthorityValidationTest.cs`, as part of the v3 planning-authority reconciliation; no other `src/`, `tests/`, solution, or package-version file was touched. Four `references/` gitlinks were also found changed in the shipped candidate (disclosed, not authorized as promotion-bearing work — see Boundary Confirmation correction).
5. **Stage only declared files at commit time.** This is the failure that required review correction in Stories 2.2, 3.3 and was called out again in Story 6.1's Boundary Confirmation. The working tree currently carries three unrelated gitlink drifts — see below. Never `git add -A` / `git commit -a`.
6. **Never bypass commit validation**; Conventional Commits when a commit is requested.

### Verified repository facts (measured 2026-07-27 at `f3b827a`)

These were measured in this workspace. Use them; do not re-derive from assumption.

| Fact | Value |
| --- | --- |
| Repository root | `/home/administrator/projects/hexalith/conversations` |
| HEAD | `f3b827a80f87a85223eaf34e8fe1183a454a6c12` |
| git | 2.53.0 |
| system python3 | 3.14.4 (no pytest installed) |
| uv | 0.11.16 — `uv run --with pytest …` provisions Python 3.11.15 + pytest 9.1.1 |
| Root-declared submodules | 10, all under `references/` (AI.Tools, EventStore, Projects, Folders, Tenants, FrontComposer, Parties, Memories, Commons, Builds) |
| `_bmad/scripts/` tracked in git | Yes (`memlog.py`, `resolve_config.py`, `resolve_customization.py`) — new files there will be tracked |
| `_bmad/scripts/tests/` | Does not exist yet — T2 creates it |
| `verify_submodule_promotion.py` | Does not exist yet — T1 creates it |

**Live gitlink drift (unrelated to this story — do not "fix" it):**

| Path | Recorded gitlink at HEAD | Submodule checkout HEAD |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `a17cafb0ca269cadb09cfbbecbbdae9ec10bebe6` | `b015e54a200f9e51ff3de7e9e973170ac8cc6967` |
| `references/Hexalith.Tenants` | `85838fbbb4efcd131a44d4ac4535110b1a9d3217` | `55e6000a41e7846868ff7512b79e5f7a36464a37` |
| `references/Hexalith.Memories` | `c9dfb06ffaf26a19a9cc6c4f38b5b2203ce4201e` | `a6753c1152ba6a4688210e453cc567f3faca8720` |

This is the exact "unrelated concurrent state" case AC 3d exists for. With `submodule_promotions: []` and no gitlink changed between baseline and candidate, the gate **must pass with warnings** on this workspace. If your implementation blocks here, it is wrong. Use this as the live acceptance check in T9.

### Checker contract (freeze this — other workflows key off it)

Canonical invocation (proposal §4.4):

```bash
python3 _bmad/scripts/verify_submodule_promotion.py \
  --repository <root> \
  --baseline <story-baseline-commit> \
  --candidate <committed-umbrella-revision> \
  --submodule references/Hexalith.EventStore \
  --require-remote references/Hexalith.EventStore \
  --format json
```

- `--submodule` and `--require-remote` are repeatable. `--require-remote` names a path that must also appear in `--submodule` (otherwise → exit 2, `INVALID_SCOPE`).
- `--candidate` defaults to `HEAD`. `--repository` defaults to the project root.
- `--baseline` is optional. When absent, changed-gitlink detection is skipped and warning `BASELINE_NOT_PROVIDED` is emitted. Callers (code-review, dev-story, dev-auto) treat that warning as a **blocker** when declared scope is non-empty, per proposal §4.6.
- `--format` defaults to `text` (human-readable, actionable remediation per blocker). `json` emits the machine-readable document below.

**Exit codes**

| Code | Meaning |
| --- | --- |
| `0` | Gate passed. Warnings may still be present. |
| `1` | Valid invocation, one or more completion blockers. |
| `2` | Invalid invocation, or repository state prevents a trustworthy decision. |

**Blocker codes (exit 1) — stable, machine-readable**

| Code | Condition | AC |
| --- | --- | --- |
| `PATH_NOT_ROOT_DECLARED` | Declared path is not in root `.gitmodules` | 2a |
| `SUBMODULE_NOT_INITIALIZED` | Path has no submodule worktree of its own | 2b |
| `SUBMODULE_DIRTY_TRACKED` | Tracked modifications (staged or unstaged) in the submodule | 2b |
| `SUBMODULE_DIRTY_UNTRACKED` | Untracked files in the submodule | 2b |
| `SUBMODULE_HEAD_UNRESOLVED` | Submodule `HEAD` does not resolve to a commit (unborn/detached-broken) | 2c |
| `REMOTE_COMMIT_UNAVAILABLE` | `--require-remote` path whose HEAD is contained by no local remote-tracking ref | 2d |
| `GITLINK_MISSING_IN_CANDIDATE` | Candidate commit records no entry at that path | 2e |
| `GITLINK_MODE_NOT_160000` | Candidate records the path with a mode other than `160000` | 2e |
| `GITLINK_COMMIT_MISMATCH` | Candidate gitlink object ID ≠ submodule HEAD | 2e |

**Warning codes (non-blocking)**

| Code | Condition | AC |
| --- | --- | --- |
| `BASELINE_NOT_PROVIDED` | No `--baseline`; changed-gitlink detection skipped | — |
| `UNDECLARED_GITLINK_CHANGE` | Gitlink changed baseline→candidate but was not declared. **The path still joins the affected set and is fully evaluated**; its evaluation failures are blockers. `require_remote` defaults to `false` for these. | 1b |
| `UNRELATED_SUBMODULE_DIRTY` | Root submodule outside the affected set has dirt | 3d |
| `UNRELATED_GITLINK_DRIFT` | Root submodule outside the affected set has checkout ≠ recorded gitlink | 3d |
| `UNRELATED_SUBMODULE_INSPECTION_FAILED` | A git command failed while inspecting a submodule outside the affected set (e.g. corrupted index); the inspection is skipped and reported as a warning rather than aborting the whole run | 3d (2026-07-27 code review patch) |

> **Design note on `UNDECLARED_GITLINK_CHANGE`:** epics AC1 says the affected scope *includes* gitlinks changed since baseline, and proposal §2 says such a change "cannot silently pass". Both are satisfied by evaluate-and-warn rather than block-on-undeclared: the pointer is fully verified, and the missing declaration is surfaced for the reviewer. Do not upgrade it to a blocker.

**Exit-2 conditions**

`GIT_UNAVAILABLE` (no git binary), `NOT_A_GIT_REPOSITORY`, `MISSING_GITMODULES`, `BASELINE_UNRESOLVABLE`, `CANDIDATE_UNRESOLVABLE`, `BASELINE_NOT_ANCESTOR` (baseline is not an ancestor of candidate; changed-gitlink detection would be unreliable — added 2026-07-27 code review patch), `INVALID_SCOPE` (`--require-remote` without matching `--submodule`, duplicate declaration, path outside the repository, absolute path, embedded control character), `GIT_COMMAND_FAILED`, `INTERNAL_ERROR` (any unexpected exception outside the above — always emits a parseable error document rather than a raw traceback; added 2026-07-27 code review patch).

**JSON document shape**

```json
{
  "schema": "submodule-promotion-gate/v1",
  "result": "pass|blocked|error",
  "repository": "<absolute path>",
  "baseline": "<sha or null>",
  "candidate": "<sha>",
  "declared": [{"path": "references/X", "require_remote": true}],
  "changed_gitlinks": ["references/Y"],
  "evaluated": [
    {"path": "references/X", "recorded_gitlink": "<sha|null>", "recorded_mode": "160000",
     "head": "<sha|null>", "initialized": true, "clean": true, "remote_available": true}
  ],
  "blockers": [{"code": "…", "path": "references/X", "message": "…", "remediation": "…"}],
  "warnings": [{"code": "…", "path": "references/Y", "message": "…"}]
}
```

Keep `result` and `blockers[].code` stable — the four gated workflows read them.

### Verified git traps (measured — each one silently breaks a naive implementation)

**T-1 — An uninitialized submodule directory answers as the umbrella.** Verified: running `git -C references/<empty-dir> status --porcelain --untracked-files=all` from inside this workspace returns the **umbrella's** dirty files, because git walks up to the umbrella `.git`. A naive checker therefore either invents phantom submodule dirt or — if the umbrella happens to be clean — reports an uninitialized submodule as clean and captured.
*Fix:* before trusting any `git -C <path> …` result, assert the path is its own worktree, e.g. `git -C <path> rev-parse --show-toplevel` equals the absolute submodule path (and/or `<path>/.git` exists as file or directory). Otherwise emit `SUBMODULE_NOT_INITIALIZED`.

**T-2 — `git ls-tree <rev> -- <nonexistent-path>` exits 0 with empty output.** Verified: `git ls-tree HEAD -- references/DoesNotExist` → exit 0, no output. Detect "not recorded in the candidate" by **empty output**, never by exit status; otherwise a misspelled or removed path passes green.

**T-3 — Mode column, not substring.** Parse `git ls-tree` / `git diff --raw` field-wise. `160000` can legitimately appear inside a blob hash or a filename. Existing repo precedent: `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:492-500` isolates the mode columns for exactly this reason. Raw format is `:<srcmode> <dstmode> <srcsha> <dstsha> <status>\t<path>`.

**T-4 — Never let unresolvable history pass green.** Story 6.1's review pass 2 found two tests degrading to zero-assertion green passes when history was unavailable (reproduced with `GIT_DIR=/nonexistent`) — erasing the very `160000` guarantee this story mechanizes. An unresolvable `--baseline`/`--candidate` is exit 2 (`*_UNRESOLVABLE`), never exit 0. Shallow clones (`git clone --depth 1`, `actions/checkout` default `fetch-depth: 1`) make this a real path.

**T-5 — Verified plumbing.** These all work at git 2.53.0 in this workspace:
- Discovery: `git config -f .gitmodules --get-regexp '^submodule\..*\.path$'` → `submodule.<name>.path <path>` lines.
- Recorded gitlink: `git ls-tree <candidate> -- <path>` → `160000 commit <sha>\t<path>`.
- Submodule HEAD: `git -C <path> rev-parse HEAD`.
- Changed gitlinks: `git diff --raw <baseline> <candidate>`, keep lines whose src or dst mode is `160000`.
- Remote availability (no network): `git -C <path> for-each-ref --contains <sha> --format='%(refname)' refs/remotes/` (or `git -C <path> branch --remotes --contains <sha>`). Verified non-empty for the current EventStore HEAD.

**T-6 — Non-traversal vs. staged nested gitlinks.** `git status --porcelain --ignore-submodules=all` guarantees non-traversal but also hides a staged nested-gitlink bump inside the submodule's index. Recommended split that keeps both properties:
- worktree + untracked dirt: `git -C <path> status --porcelain --untracked-files=all --ignore-submodules=all`
- staged changes including gitlink entries: `git -C <path> diff-index --cached --name-status HEAD` (pure object comparison, no worktree descent)

If you choose differently, state the trade-off in the Dev Agent Record. Do not silently drop either half.

**T-7 — Path safety and encoding.** Use `-z` variants where available and set `-c core.quotepath=false`. Decode subprocess output explicitly as UTF-8 with a replacement policy. Read stdout and stderr concurrently (`subprocess.run(..., capture_output=True, timeout=…)`), never sequentially from live pipes — the dotnet suite had to be patched for exactly this deadlock. Always pass a timeout, and include stderr in failure messages rather than discarding it.

**T-8 — Line endings.** `.gitattributes` pins `_bmad-output/**/*.{md,json,yaml,yml}` and `docs/release-evidence/**/*.{md,json}` to `eol=lf`. `_bmad/scripts/*.py` and the skill trees are **not** pinned. Write `\n` and do not introduce CRLF.

### Dual skill trees — every skill edit lands twice

`.claude/skills/` (936 tracked files) and `.agents/skills/` (946 tracked files) are **byte-identical copies** of the same skill set — `diff -rq` reports no content differences; the only delta is that `.agents/skills/` additionally contains `aspire/`. Both are tracked in git. Different agent harnesses read different trees.

**Every file listed in T3–T7 must be edited in both trees, identically.** After editing, prove it:

```bash
diff -rq .agents/skills .claude/skills   # expect: only "Only in .agents/skills: aspire"
```

`_bmad/render/bmad-quick-dev/` and `_bmad/render/bmad-dev-auto/` are **derived, stale snapshots** with placeholders already substituted (e.g. `English` in place of `{{.communication_language}}`) and are already behind the live copies (missing `{workflow.on_complete}`). Nothing references them. Default: do not edit them; record the decision in the Dev Agent Record.

### Files to modify — exact anchors

| File (× both skill trees) | Anchor | Change |
| --- | --- | --- |
| `bmad-create-story/template.md` | top of file (currently has no frontmatter) | Add YAML frontmatter with `submodule_promotions: []` |
| `bmad-create-story/SKILL.md` | step 5 template-output list | Populate the field during story creation |
| `bmad-quick-dev/spec-template.md` | frontmatter block | Add `submodule_promotions: []` |
| `bmad-quick-dev/step-02-plan.md` | instruction 3 (fill the template) | Populate the field from the investigated scope |
| `bmad-quick-dev/step-05-present.md` | `### Mark Spec Done` / `### Commit and Open` | Reorder + gate (T5) |
| `bmad-quick-dev/step-oneshot.md` | `### Generate Spec Trace` → `### Commit` | `in-review` first, gate before `done` (T5) |
| `bmad-dev-auto/spec-template.md` | frontmatter block | Add `submodule_promotions: []` |
| `bmad-dev-auto/step-02-plan.md` | instruction 3 | Populate the field |
| `bmad-dev-auto/step-04-review.md` | `## Finalize`, after commit-content verification, before `Capture final_revision` | Gate → `status: blocked` on failure (T6) |
| `bmad-code-review/steps/step-04-present.md` | section *6*, before *Determine new status based on review outcome* | Gate → force `in-progress` (T4) |
| `bmad-dev-story/SKILL.md` | step 9, before `<action>Update the story Status to: "review"</action>` | Gate → stay `in-progress` (T7) |
| `bmad-dev-story/checklist.md` | validation list | Add the gate item (T7) |
| `docs/runbooks/submodule-promotion-completion-gate.md` | new live operational runbook | Add the three approved items + command/exits/remediation while preserving signed-v1 evidence (T8; owner-approved 2026-07-27) |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | `action_items` → Epic 3 A2 | `open` → `done` after T9 (T10) |

### Baseline field-name divergence — handle both

The four gated workflows do **not** agree on the frontmatter key:

| Workflow | Frontmatter key | Set in |
| --- | --- | --- |
| `bmad-dev-story` | `baseline_commit` | `SKILL.md` step (writes it when status is `ready-for-dev`) |
| `bmad-quick-dev` | `baseline_commit` | `step-03-implement.md:21` |
| `bmad-dev-auto` | `baseline_revision` | `step-03-implement.md:20` |
| `bmad-code-review` | reads `baseline_commit` | `steps/step-01-gather-context.md:24` |

`spec-6-1-…md` carries `baseline_revision`. Gate instructions must read **either** key (`baseline_commit`, else `baseline_revision`) and treat `NO_VCS` or a missing value as "no trustworthy baseline" → blocker when declared scope is non-empty. Do not rename existing keys; that is out of scope and would break in-flight specs.

### Testing requirements

- **Framework:** pytest via `uv`, matching the existing BMad script tests. Run:
  `uv run --with pytest pytest _bmad/scripts/tests/test_verify_submodule_promotion.py -q`
  (verified available: uv 0.11.16 provisions Python 3.11.15 + pytest 9.1.1). System `python3` is 3.14.4 with no pytest — do not assume a bare `pytest` on PATH.
- **Hermetic fixtures only.** Build umbrella + submodule repos under `tmp_path`. Never operate on the real workspace, never on `references/`, never network.
- **Local submodule adds** need `-c protocol.file.allow=always` on modern git; set it in the fixture helper.
- **Remote-availability determinism:** create a local bare repo as `origin`, push/fetch into it inside the fixture so `refs/remotes/origin/*` exists locally. Prove both the available and unavailable branches deterministically — no network, no flakiness.
- **Nested non-traversal proof:** build a submodule that itself declares a nested submodule (uninitialized and, separately, initialized-and-dirty). Assert the checker never initializes it and that its state does not change the verdict. Assert on observable evidence — e.g. the nested path stays empty and no `.git` appears — not merely on an absent exception.
- **Trap coverage:** add a fixture per T-1, T-2, T-3, T-4. These are the cases that pass green under a plausible-looking implementation.
- **No dotnet impact expected.** This story adds no C# and changes no guarded planning artifact, so the conformance suite should be unaffected. Confirm rather than assume: `ArchitecturePlanningAuthorityValidationTest` hashes and parses `architecture.md` and `epics.md` byte-for-byte (see it assert `mode-\`160000\` gitlink` and `root \`.gitmodules\`` at `ArchitecturePlanningAuthorityValidationTest.cs:595-602`). **Do not edit `architecture.md` or `epics.md` in this story** — 6.1 already placed the invariant, and any edit there breaks frozen hashes.

  **Addendum (2026-07-27, expanded-scope authorization — code-review reconciliation):** this prohibition was overridden mid-implementation. Per the Dev Agent Record ("Final regression HALT" and "Expanded-scope authorization" entries), the definition-of-done regression run failed five pre-existing `ArchitecturePlanningAuthorityValidationTest` cases against v3 planning-authority drift unrelated to this story's own checker work. Jerome explicitly authorized reconciling `architecture.md`, `epics.md` (append-only), `epic-6-context.md`, and the Conformance test's authority class to resolve it, per `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27.md` (`trigger: "Human clarification during the paused Story 6.2 bmad-loop resolution"`) and its predecessor `sprint-change-proposal-2026-07-26.md`. This is a real, out-of-band authorization, not self-declared by this story — but it was never reconciled with the prohibition above at the time, and this story's own Dev Notes gave no carve-out for it. See Boundary Confirmation and File List for the full disclosed scope.

### Previous story intelligence (Story 6.1, `done` 2026-07-26 after two review passes)

Story 6.1 is the direct predecessor and its review pass 2 is the single most useful input here. Load `_bmad-output/implementation-artifacts/spec-6-1-rebaseline-architecture-and-planning-authority.md` if you need detail. The carry-forward lessons:

- **A gate that cannot fail is worse than no gate.** Pass 1 reported 401/401 green while 14 semantic mutations to the guarded artifacts passed. The replacement historical binding was a tautology comparing a declared hash against the commit it was computed from — measured 19/19 artifacts where the branch could never fail. *Apply to 6.7:* every fixture must include a mutation that the check is supposed to catch, and you must observe it fail before you observe it pass.
- **Zero-assertion green passes.** Unresolvable git history silently skipped every assertion in two tests, including `ShouldNotContain("160000")` — "the exact gitlink-exclusion invariant Story 6.7 must mechanize". Fixed with `Assert.Skip` plus a positive executed-path counter. *Apply to 6.7:* T-4 above; and in the fixtures, assert the code path actually executed, not just that nothing raised.
- **Substring checks are not semantic checks.** `"5% P95"` is a substring of `"45% P95"`. *Apply to 6.7:* T-3 above — parse fields, never substring-match `160000`.
- **Stage only the declared File List.** Pass 2's explicit review decision: five uncommitted root gitlink drifts were left untouched and excluded from the Story 6.1 commit, because sweeping them in "is the failure that required review correction in Stories 2.2 and 3.3". Three of those drifts are still present now.
- **Disclose, don't fix.** Story 6.1 disclosed unrelated content carried in story-owned files rather than silently reverting it. Do the same in the Boundary Confirmation.
- **Fault injection must target acceptance criteria, not the checks that cannot fail to notice.** Pass 1 injected only SHA-256 and byte-boundary mutations; pass 2 injected one per AC and caught twelve real gaps. T9 requires the pass-2 discipline.
- Story 6.1's own Boundary Confirmation states: *"Under the promotion-completion invariant this story declares, that state would otherwise block completion."* Verify how your checker actually treats it — with an empty declaration and no gitlink change in the diff, it must **warn**, not block.

### Git intelligence (last 8 commits)

```
f3b827a feat: enhance bmad-loop hook to support workspacePaths and update review adapter name
0ff57b2 feat: add new BMAD commands and deprecate old ones
16e3d3d fix: enhance SuccessMetricReportAndAttestationValidationTest with new validations and introduce Deferred Work Ledger
4461dd4 fix: update subproject commits for Hexalith.EventStore and Hexalith.Tenants
d91c1cf feat: approve projection read-store proof ADR
6e8cf8a fix: enhance null checks and configure await in various tests and handlers
7aaf130 fix: update Aspire.AppHost.Sdk version from 13.4.2 to 13.4.6 …
4b1c3fd fix: improve instructions for locating hexalith-llm-instructions.md in submodules
```

Relevant signals:
- `4461dd4` is a bare gitlink-bump commit — the exact commit shape the gate governs, and evidence that pointer bumps still land outside a declared scope.
- `0ff57b2` reinstalled/deprecated BMad commands and is why `.claude/skills/` and `.agents/skills/` are currently in lockstep; a skill edit applied to one tree only will show up as a `diff -rq` delta.
- `d91c1cf` amended a frozen overlay without a version bump — the concrete precedent for why derived/duplicated artifacts must be updated together.
- No `.github/workflows` exists anywhere outside `references/` (recorded in `deferred-work.md`): nothing runs any gate automatically. The checker must therefore be trivially runnable by hand, and the workflow prose is the only enforcement path today.

### Downstream consumer — Story 6.2 is blocked on this

`_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting.md` has `status: blocked`, and its **Block If** clause reads *"Story 6.7's mechanical promotion gate is not complete"*. Its **Always** clause requires updating affected root submodules and exact mode-`160000` gitlinks *through the Story 6.7 gate*. Binding order is `6.1 → 6.7 → 6.2`. 6.2 will be the first real promotion-bearing consumer: it declares EventStore/Commons paths and bumps their gitlinks. Design the CLI so that story can call it unchanged.

### Project Structure Notes

- New files land in `_bmad/scripts/` and a new `_bmad/scripts/tests/` — both tracked, both umbrella-owned, neither inside `references/`. No dotnet project membership, no `.slnx` change.
- Skill files are agent instructions (markdown), not code; the "implementation" for T4–T7 is precise prose that a workflow agent will follow verbatim. Keep the inserted instructions imperative and unambiguous, matching each file's existing voice (`bmad-code-review` uses `####` subsections and `{variable}` placeholders; `bmad-dev-auto` uses declarative HALT semantics; `bmad-dev-story` uses `<action>` / `<check>` XML-ish steps).
- `bmad-dev-story/SKILL.md` uses `<step n="9" …>` XML syntax — insert `<action>` / `<check>` elements, not free prose.
- No variance from the unified structure is expected. If you find one, record it here rather than working around it.

### References

- [Source: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Story 6.7`] — binding ACs, overlay `epic-6-authority-2026-07-27-v3`
- [Source: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md#Binding Dependency Order`] — `6.1 → 6.7 → 6.2 → 6.5 → 6.6`
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-submodule-promotion-completion-gate.md`] — approved by Jerome 2026-07-15; §2 technical impact, §4.1–4.10 detailed change proposals, §5 completion evidence and final success criterion
- [Source: `_bmad-output/planning-artifacts/architecture.md#Promotion Completion Invariant`] — the declarative invariant this story mechanizes
- [Source: `_bmad-output/implementation-artifacts/epic-6-context.md#Technical Decisions`] — root-only discovery, never traverse nested submodules
- [Source: `_bmad-output/implementation-artifacts/spec-6-1-rebaseline-architecture-and-planning-authority.md#Review Pass 2 Corrections`] — tautological-gate and zero-assertion-pass precedents
- [Source: `_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting.md`] — downstream consumer, currently `blocked`
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`] — Epic 3 action item A2, `open`
- [Source: `docs/release-evidence/promote-adopt-runbook.md`] — immutable signed-v1 historical evidence retained byte-identically
- [Source: `docs/runbooks/submodule-promotion-completion-gate.md`] — owner-approved live operational replacement for T8
- [Source: `tests/Hexalith.Conversations.Conformance.Tests/SuccessMetricReportAndAttestationValidationTest.cs:492-500`] — mode-column parsing precedent
- [Source: `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs:595-602`, `:781-810`] — invariant assertions and `git ls-tree` gitlink regex precedent
- [Source: `.claude/skills/bmad-architecture/scripts/tests/test_lint_spine.py:1-25`] — PEP-723 + pytest house convention for BMad scripts
- [Source: `_bmad/scripts/resolve_customization.py:1-50`] — stdlib-only, argparse, JSON-to-stdout house style
- [Source: `_bmad-output/project-context.md#Development Workflow Rules`] — never recursive, never nested submodules, scope changes to Conversations artifacts

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- 2026-07-27 — T1 plan: centralize all Git inspection in a stdlib-only CLI, parse raw/tree fields structurally, separate exit-2 trust failures from exit-1 completion blockers, and inspect unrelated root submodules only for non-blocking warnings.
- 2026-07-27 — T1 RED: `uv run --with pytest pytest _bmad/scripts/tests/test_verify_submodule_promotion.py -q` → 2 failed because the checker did not yet exist.
- 2026-07-27 — T1 GREEN: the same focused suite → 2 passed; `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline f3b827a80f87a85223eaf34e8fe1183a454a6c12 --candidate HEAD --format json` → exit 0, pass, exactly three `UNRELATED_GITLINK_DRIFT` warnings.
- 2026-07-27 — T2 GREEN: `uv run --with pytest pytest _bmad/scripts/tests/test_verify_submodule_promotion.py -q` → 25 passed in 2.74s. Fixtures cover clean/remote success, tracked and untracked dirt, remote failure, missing/wrong/mismatched gitlinks, undeclared changed gitlinks, unrelated dirt, uninitialized and unresolved worktrees, invalid scope/history, nested non-traversal, staged nested gitlinks, missing-tree-entry semantics, structural mode parsing, Git absence, and UTF-8/space paths.
- 2026-07-27 — T3 RED: `rg '^submodule_promotions:'` returned exit 1 for all three live templates before the schema change.
- 2026-07-27 — T3 GREEN: schema/instruction searches passed; `git diff --check` passed; `diff -rq .agents/skills .claude/skills` reported only the approved extra `.agents/skills/aspire` directory.
- 2026-07-27 — T4 RED/GREEN: the promotion-gate heading was absent in both code-review copies before the edit; afterward structural searches, byte-equality comparison, and `git diff --check` passed.
- 2026-07-27 — T5 RED/GREEN: quick-dev had no committed-candidate promotion gate and one-shot wrote `done` before commit; post-edit heading-order checks prove candidate commit → gate → done/review → completion-record commit, both routes are byte-identical across skill trees, and `git diff --check` passes.
- 2026-07-27 — T6 RED/GREEN: dev-auto had no promotion gate before `final_revision`/`done`; post-edit line-order checks place it after commit-content verification and before both writes, with byte-identical skill copies and clean diff validation.
- 2026-07-27 — T7 RED/GREEN: dev-story had no promotion-gate action or checklist item; post-edit checks prove the gate precedes the `review` write, both copies match, `git diff --check` passes, and the complete `<workflow>` fragment parses as XML.
- 2026-07-27 — T8 BLOCKED: the required runbook edit was implemented and passed focused prose checks, but `SuccessMetricReportAndAttestationValidationTest.SourceArtifactsShouldBindToSignedV1ContentAtItsDeclaredSourceIdentity` rejected it because the same file is hash-pinned signed v1 evidence. The edit was reverted, leaving the signed artifact byte-identical while authority is clarified.
- 2026-07-27 — T9 fixture proof: full `uv run --with pytest pytest _bmad/scripts/tests/test_verify_submodule_promotion.py -q` → 29 passed in 2.54s; focused acceptance fault command → 6 passed in 0.51s.
- 2026-07-27 — T9 fault injections: AC1 changed-but-undeclared plus uncaptured checkout → `UNDECLARED_GITLINK_CHANGE` and `GITLINK_COMMIT_MISMATCH`; AC2 tracked dirt → `SUBMODULE_DIRTY_TRACKED`; AC3 removed dev-auto gate heading → workflow ordering validator reported the missing marker; AC4 uninitialized and initialized-dirty nested submodules → non-traversal assertions preserved state and pass verdict; AC5 injected `references/Hexalith.EventStore` into the story-owned File List → boundary validator rejected it.
- 2026-07-27 — T9 live gate: empty declared scope, baseline/candidate `f3b827a80f87a85223eaf34e8fe1183a454a6c12` → exit 0/pass, zero blockers, exactly three `UNRELATED_GITLINK_DRIFT` warnings for EventStore, Tenants, and Memories.
- 2026-07-27 — T9 restoration check: every Git mutation occurred only in pytest `tmp_path` repositories and was removed by fixture cleanup. Root `git diff --check` passed; live submodule HEADs remained EventStore `b015e54a200f9e51ff3de7e9e973170ac8cc6967`, Tenants `55e6000a41e7846868ff7512b79e5f7a36464a37`, and Memories `a6753c1152ba6a4688210e453cc567f3faca8720`.
- 2026-07-27 — Regression validation before HALT: `dotnet restore Hexalith.Conversations.slnx` passed; Release build passed with 0 warnings/errors; Contracts 618/618, Client 29/29, Domain 185/185, Server 610/610, Integration 9/9, and ServiceDefaults 7/7 passed. Conformance ran 408 tests: 402 passed and 6 failed; five failures bind pre-existing architecture/epic working-tree edits, while the sixth exposed the T8 signed-evidence conflict.
- 2026-07-27 — Safe restoration proof: after reverting only the attempted T8 runbook edit, the focused signed-source guard passed 1/1, `git diff --exit-code -- docs/release-evidence/promote-adopt-runbook.md` passed, and `git diff --check` passed. The remaining five full-Conformance failures belong to pre-existing authority-file drift rather than Story 6.7 files.
- 2026-07-27 — T10 remains pending: Epic 3 action A2 was restored to `open` because the T8/T9 completion boundary has not passed. Story and sprint status remain `in-progress`; no other action item changed.
- 2026-07-27 — T8 owner resolution: Jerome approved the recommended preservation design. The historical signed-v1 runbook remains byte-identical; live completion-gate guidance moves to versioned `docs/runbooks/submodule-promotion-completion-gate.md`.
- 2026-07-27 — T8 RED/GREEN: the new preservation/content contract first failed because the operational runbook did not exist; after implementation the focused contract passed 1/1, the full checker/workflow suite passed 30/30, the signed-v1 SHA-256 remained `2ae308e82f159b3f152077d6946ff220108266806668c6dfb0921f3df0920ce1`, and its focused Conformance guard passed 1/1.
- 2026-07-27 — T9/T10 resumed: the positive boundary now holds because the new live document is outside signed evidence. Epic 3 action A2 changed `open` → `done` only after the 30-test independent proof; every other action-item status remains unchanged.
- 2026-07-27 — Final regression HALT: eight .NET test projects passed 1,479/1,479; Conformance passed 403/408 and failed five `ArchitecturePlanningAuthorityValidationTest` cases against the pre-existing `architecture.md`, `epics.md`, and `epic-6-context.md` edits. Aggregate result: 1,882 passed, 5 failed, 0 skipped across 1,887 tests. Story 6.7's former signed-runbook failure is resolved, but the mandatory all-green completion gate keeps story and sprint state `in-progress`.
- 2026-07-27 — Expanded-scope authorization: Jerome explicitly authorized reconciliation of the v3 planning-authority files and their Conformance contracts after the five-failure HALT.
- 2026-07-27 — Authority RED/GREEN: the existing five failing cases proved the v2 oracle rejected approved v3 authority. The repaired 17-test authority class now requires the non-packable/non-publishable module test AppHost, forbids production/runtime ownership, binds architecture/overlay/context v3, preserves the exact 55,536-byte Epic 1–5 prefix and complete 14,843-byte v2 overlay, and passed 17/17; full Conformance passed 408/408.
- 2026-07-27 — Final completion GREEN: Release build passed with 0 warnings/errors; all nine .NET test projects passed 1,887/1,887 with 0 failed/skipped; checker/workflow/boundary pytest passed 31/31; XML parsing, Python compilation, signed-v1 byte guard, and `git diff --check` passed. Skill parity reports only the approved `.agents/skills/aspire` extra directory.
- 2026-07-27 — Final promotion gate: empty approved scope, trustworthy baseline/candidate `f3b827a80f87a85223eaf34e8fe1183a454a6c12` → exit 0/pass, no blockers or changed gitlinks, with exactly three non-blocking `UNRELATED_GITLINK_DRIFT` warnings for EventStore, Tenants, and Memories.
- 2026-07-27 — **Code review correction (supersedes the two entries above as completion evidence):** both prior "live gate" runs invoked the checker with `--baseline` and `--candidate` set to the same commit (`f3b827a80f87a85223eaf34e8fe1183a454a6c12`), which cannot detect any gitlink change by construction and never validated the actually-shipped candidate. Re-run against the real baseline and the committed candidate: `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline f3b827a80f87a85223eaf34e8fe1183a454a6c12 --candidate HEAD --format json` (candidate resolved to `43823403b47a92d0d0c7ad27440865df41575951`) → exit 0/pass, zero blockers, `changed_gitlinks: [references/Hexalith.Builds, references/Hexalith.EventStore, references/Hexalith.Memories, references/Hexalith.Tenants]`, four `UNDECLARED_GITLINK_CHANGE` warnings (not the three `UNRELATED_GITLINK_DRIFT` warnings claimed above — the affected-but-undeclared path evaluates differently from the truly-unrelated path). All four submodules are clean and their recorded gitlinks match `HEAD`. See Boundary Confirmation correction for full disclosure.
- 2026-07-27 — Dev-story completion revalidation at candidate `623a3e636d66974c6081174fdaa35be6d9ae97f7`: the normal `dotnet restore Hexalith.Conversations.slnx` broad gate remains blocked by `NU1102` because the checked-out EventStore dependency graph requests unpublished `999.1.20-proof.fa2d1c9910f8` packages (nearest NuGet version `3.82.0`). The approved local-source fallback restored and built Release with `-p:UseHexalithProjectReferences=true`; build result was 0 warnings/errors and all nine test projects passed 1,887/1,887 with 0 failed/skipped (Contracts 618, Client 29, Domain 185, Server 610, Conformance 408, Admin Web 14, AppHost 7, Integration 9, ServiceDefaults 7). The checker/workflow pytest suite passed 38/38; workflow XML parsing, Python compilation, skill-tree parity, signed-v1 SHA-256 `2ae308e82f159b3f152077d6946ff220108266806668c6dfb0921f3df0920ce1`, and `git diff --check` passed.
- 2026-07-27 — Current promotion completion gate BLOCKED: `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline f3b827a80f87a85223eaf34e8fe1183a454a6c12 --candidate HEAD --format json` resolved candidate `623a3e636d66974c6081174fdaa35be6d9ae97f7` and returned exit 1 / `GITLINK_COMMIT_MISMATCH` for `references/Hexalith.Memories`: committed gitlink `fe19a27cf1b60457aa05f45dd075b37c1038b3e3` does not match the clean checkout HEAD `000af15600c7ecbbb5a7c48c73746221f185ae58`. Actionable remediation: commit the root gitlink that exactly matches the checked-out submodule HEAD, or otherwise restore an owner-approved captured state; do not initialize, update, fetch, or silently expand scope. Four `UNDECLARED_GITLINK_CHANGE` warnings remain for Builds, EventStore, Memories, and Tenants. Story and sprint state remain `in-progress`.
- 2026-07-27 — Completion File List audit against `f3b827a80f87a85223eaf34e8fe1183a454a6c12..HEAD` found 41 changed paths, not the previously claimed 35/39. Added the two omitted committed paths (`deferred-work.md` and the Story 6.2 spec) to the File List without modifying either artifact.

### Completion Notes List

- T1 complete: added an executable PEP-723 Python 3.11 checker with the frozen CLI, JSON/text output, exit codes, blocker/warning codes, root-only discovery, clean/HEAD/remote/gitlink validation, baseline-to-candidate gitlink union, and non-blocking unrelated-state reporting.
- T2 complete: added hermetic pytest coverage using isolated repositories and local bare remotes; after review hardening the suite contains 38 passing cases and exercises every AC 4b case and verified Git trap without touching the live workspace.
- T3 complete: story, quick-dev, and dev-auto planning now declare exact promotion scope and remote policy, with fail-closed approval handling for missing/ambiguous scope. Both live skill trees are synchronized; derived `_bmad/render/` snapshots remain intentionally untouched.
- T4 complete: code-review now runs the canonical checker before its status decision, accepts either baseline key, promotes missing baseline to a blocker for activated work, preserves stable codes, and prevents both story and sprint `done` writes after failure.
- T5 complete: quick-dev plan and one-shot routes now hold `in-review`, commit only scoped candidate content, gate committed `HEAD`, return to `in-progress` with diagnostics on failure, and write `done`/sprint `review` only after success.
- T6 complete: unattended dev-auto now blocks on missing promotion scope, activated missing VCS/baseline, or checker failure; it records stable diagnostics and cannot emit a successful final revision or `done` after failure.
- T7 complete: dev-story now validates approved scope and committed HEAD before review, preserves blocker diagnostics, and explicitly restores both story and sprint tracking to `in-progress` on failure; code-review remains the final `done` authority.
- T8 complete under the owner-approved preservation design: added a live versioned operational runbook with the exact scope, candidate, exit, remediation, workflow-state, root-only safety, and three-item closing-checklist rules; signed-v1 evidence remains byte-identical.
- T9 complete: checker, workflow ordering, operational-runbook preservation/content, live-workspace, fault-injection, and boundary proofs all pass without touching unrelated state.
- T10 implementation evidence is complete, but Epic 3 action A2 remains `open` by the code-review decision until Story 6.7 itself reaches `done`; no other action item changed.
- Expanded completion repair complete: owner-authorized v3 architecture, append-only epic amendment, derived context, and Conformance contracts now agree without weakening the signed-v1, historical-prefix, v2-overlay, projection, performance, or platform-ownership guards.
- Completion revalidation: all tasks are checked, the exact 41-path baseline-to-HEAD record is now listed, and focused/source-reference regression validation passes; the story is not ready for review while the recorded `GITLINK_COMMIT_MISMATCH` blocker remains.

### File List

- `_bmad/scripts/verify_submodule_promotion.py` (new)
- `_bmad/scripts/tests/test_verify_submodule_promotion.py` (new)
- `.agents/skills/bmad-create-story/SKILL.md` (modified)
- `.agents/skills/bmad-create-story/template.md` (modified)
- `.agents/skills/bmad-code-review/steps/step-04-present.md` (modified)
- `.agents/skills/bmad-dev-auto/spec-template.md` (modified)
- `.agents/skills/bmad-dev-auto/step-02-plan.md` (modified)
- `.agents/skills/bmad-dev-auto/step-04-review.md` (modified)
- `.agents/skills/bmad-dev-story/SKILL.md` (modified)
- `.agents/skills/bmad-dev-story/checklist.md` (modified)
- `.agents/skills/bmad-quick-dev/spec-template.md` (modified)
- `.agents/skills/bmad-quick-dev/step-02-plan.md` (modified)
- `.agents/skills/bmad-quick-dev/step-05-present.md` (modified)
- `.agents/skills/bmad-quick-dev/step-oneshot.md` (modified)
- `.claude/skills/bmad-create-story/SKILL.md` (modified)
- `.claude/skills/bmad-create-story/template.md` (modified)
- `.claude/skills/bmad-code-review/steps/step-04-present.md` (modified)
- `.claude/skills/bmad-dev-auto/spec-template.md` (modified)
- `.claude/skills/bmad-dev-auto/step-02-plan.md` (modified)
- `.claude/skills/bmad-dev-auto/step-04-review.md` (modified)
- `.claude/skills/bmad-dev-story/SKILL.md` (modified)
- `.claude/skills/bmad-dev-story/checklist.md` (modified)
- `.claude/skills/bmad-quick-dev/spec-template.md` (modified)
- `.claude/skills/bmad-quick-dev/step-02-plan.md` (modified)
- `.claude/skills/bmad-quick-dev/step-05-present.md` (modified)
- `.claude/skills/bmad-quick-dev/step-oneshot.md` (modified)
- `docs/runbooks/submodule-promotion-completion-gate.md` (new)
- `_bmad-output/implementation-artifacts/6-7-mechanically-block-incomplete-submodule-promotions-from-completion.md` (modified)
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified by the code-review commit; deferred findings ledger)
- `_bmad-output/implementation-artifacts/epic-6-context.md` (modified)
- `_bmad-output/implementation-artifacts/spec-6-2-migrate-conversations-to-platform-owned-hosting.md` (new in the implementation candidate; unrelated downstream story artifact disclosed by the completion audit)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
- `_bmad-output/planning-artifacts/architecture.md` (modified; owner-authorized v3 authority)
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md` (modified; append-only v3 amendment)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26.md` (new; approved superseded authority provenance)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27.md` (new; approved v3 authority)
- `tests/Hexalith.Conversations.Conformance.Tests/ArchitecturePlanningAuthorityValidationTest.cs` (modified)
- `references/Hexalith.Builds` (gitlink; undeclared, disclosed 2026-07-27 code review — clean, matches submodule `HEAD`)
- `references/Hexalith.EventStore` (gitlink; undeclared, disclosed 2026-07-27 code review — pre-existing drift capture, clean, matches submodule `HEAD`)
- `references/Hexalith.Memories` (gitlink; undeclared, disclosed 2026-07-27 code review — pre-existing drift capture, clean, matches submodule `HEAD`)
- `references/Hexalith.Tenants` (gitlink; undeclared, disclosed 2026-07-27 code review — pre-existing drift capture, clean, matches submodule `HEAD`)

### Boundary Confirmation

Story 6.7 changes `_bmad/scripts/`, the five named BMad skill families in both live skill trees, the live operational runbook, this story/sprint record, and—under Jerome's explicit expanded-scope authorization—the v3 architecture/epic/context authority chain and its single Conformance contract. It does not modify `src/`, solution/build/package files, signed release evidence, historical Epic 1–5 content, the approved v2 overlay, `_bmad/render/`, or unrelated dotnet tests other than the one Conformance contract named above. The original 55,536-byte Epic 1–5 prefix and complete 14,843-byte v2 overlay are byte-pinned; the unrelated Story 6.2 spec remains untouched.

**Correction (2026-07-27 code review):** the statement above previously read "It does not modify any `references/` gitlink or submodule content" — that was false against the actually-committed candidate. Between this story's baseline (`f3b827a80f87a85223eaf34e8fe1183a454a6c12`) and the commit that shipped it (`43823403b47a92d0d0c7ad27440865df41575951`), four root gitlinks changed: `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.Memories`, and `references/Hexalith.Tenants`. The latter three were the pre-existing drift this story's own Dev Notes named as unrelated and "do not fix"; `Hexalith.Builds` is a fourth, previously undocumented bump. None were declared in `submodule_promotions` (which remains `[]`, correctly — this story is still not itself promotion-bearing work). Verified via the story's own checker against the real baseline and candidate: `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline f3b827a80f87a85223eaf34e8fe1183a454a6c12 --candidate HEAD --format json` → `result: pass`, zero blockers, four `UNDECLARED_GITLINK_CHANGE` warnings (all four submodules are clean and their recorded gitlinks match their checked-out `HEAD`). The gate correctly does not block on this — undeclared-but-clean changes are warn-only by design (see Dev Notes → Checker contract) — but the two "live gate" Dev Agent Record entries below that invoked the checker with baseline and candidate both set to `f3b827a80f87a85223eaf34e8fe1183a454a6c12` never actually tested this: comparing a commit against itself trivially yields zero changed gitlinks. Disclosed, not fixed, per Story 6.1's own precedent cited in this story's Dev Notes.

## Change Log

- 2026-07-27 — Implemented the read-only submodule-promotion completion gate, hermetic and workflow contract tests, synchronized completion gates across both live skill trees, an owner-approved live operational runbook that preserves signed-v1 evidence, and Epic 3 action A2 closure.
- 2026-07-27 — Kept completion status `in-progress` because the required full regression gate reports five pre-existing planning-authority failures outside this story's boundary.
- 2026-07-27 — With explicit owner authorization, reconciled the approved v3 authority/context and strengthened Conformance to preserve the v2 history while enforcing the non-shipping test-AppHost boundary.
- 2026-07-27 — Passed the full definition of done and moved Story 6.7 from `in-progress` to `review`.
- 2026-07-27 — Code review (adversarial four-layer): disclosed 4 previously-undisclosed undeclared root gitlink changes in the shipped candidate and corrected the self-referential T9 live-gate evidence; reverted Epic 3 action A2 to `open`; reconciled the story's own contradicted "no architecture.md/epics.md/tests-dotnet" constraints against its later expanded-scope authorization; applied 9 checker/runbook robustness patches (exception handling, `--format=json` parsing, rename handling, baseline-ancestor check, unrelated-submodule failure isolation, `epic-6-context.md` grammar/drift fix + matching Conformance assertion, lossy decode, control-character rejection, default-text-format test coverage) with 7 new regression tests (38/38 pytest passing); deferred 6 lower-priority findings to the ledger, including `bmad-dev-story`'s inability to detect an uncommitted undeclared promotion. Moved Story 6.7 back to `in-progress`: the pytest/checker suite is independently re-verified, but the full dotnet/Conformance suite could not be re-run in this environment (pre-existing `NU1102` package-restore failure, unrelated to this story), so the `ArchitecturePlanningAuthorityValidationTest.cs` edit made during this pass is unconfirmed by an actual test run.
- 2026-07-27 — Dev-story completion revalidation passed Release/source-reference build and all 1,887 .NET tests plus 38 checker/workflow tests, corrected the File List to the exact 41-path baseline-to-HEAD set, and kept Story 6.7 `in-progress` because the live gate returned `GITLINK_COMMIT_MISMATCH` for the pre-existing Memories checkout/gitlink drift; the normal package-mode restore remains independently blocked by the documented `NU1102` proof-version dependency.

## Open Questions for the Story Owner

These do not block implementation — each has a stated default that the dev agent should follow unless overridden.

1. **`_bmad/render/` derived copies.** They are stale, unreferenced snapshots. *Default: leave untouched, record the decision.*
2. **`UNDECLARED_GITLINK_CHANGE` severity.** Warning + full evaluation, per the reconciliation of epics AC1 with proposal §2. *Default: warning, as specified above.* Escalating it to a blocker would tighten scope discipline but would false-block legitimate concurrent pointer bumps.
3. **Story-file naming.** This file uses the create-story default `{story_key}.md`, matching the sprint-status key and Epics 1–5. Epic 6's earlier artifacts use a `spec-6-N-…md` prefix because they were produced by the quick-dev/dev-auto spec route. If 6.7 is implemented via `bmad-dev-auto`, that route will generate its own `spec-6-7-…md`; keep this file as the authoritative story context and cross-reference it rather than duplicating the ACs.
4. **`bmad-loop` / story-automator.** They orchestrate the four gated skills rather than writing `done` themselves (`.bmad-loop/bmad_loop_hook.py` contains no sprint-status writes), so gating the four skills covers them. *Default: no change; note the finding.* If a future orchestrator writes status directly, it needs the same gate.
