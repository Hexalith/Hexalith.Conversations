# Deferred Work Ledger

Real findings that are not actionable in the story that surfaced them. Each entry records where it came from so a later sweep can verify it against the codebase before acting.

## Deferred from: code review of spec-6-1-rebaseline-architecture-and-planning-authority (2026-07-26)

- **The planning gate has no automated execution path.** Story 6.1's stated approach is to "enforce the resulting planning contract with focused conformance tests", but the repository has no `.github/workflows` directory and no pipeline definition anywhere outside `references/`. The story's Verification section lists hand-run binary invocations. Nothing causes `ArchitecturePlanningAuthorityValidationTest` (or the other 390 conformance facts) to run on any commit, and the artifacts it guards are markdown that later agents edit without triggering a build. Until this is wired into CI or a pre-commit hook, every authority assertion is advisory. Pre-existing repository condition, not caused by Story 6.1.

- **Nested submodule administrative metadata exists under a root submodule.** `.git/modules/references/Hexalith.FrontComposer/modules/Hexalith.AI.Tools` and `.../Hexalith.Builds` exist with mtimes 2026-07-14 22:34 and 2026-07-15 01:22. Both working trees are empty (0 entries), so the nested submodules are registered in git metadata but not checked out. The timestamps predate Story 6.1's implementation window, so this is not attributable to this pass, but it sits against the standing rule never to initialize or traverse nested submodules. A later sweep should confirm whether the metadata can be cleaned up without disturbing the FrontComposer checkout.

## Deferred from: code review of 6-7-mechanically-block-incomplete-submodule-promotions-from-completion (2026-07-27)

- **`bmad-dev-story` cannot detect an undeclared, uncommitted submodule promotion made during its own session.** It captures `baseline_commit` once at `ready-for-dev` and never runs `git commit` in any of its own steps; step 9 resolves "committed `HEAD`" as the gate candidate, so if nothing was committed, `HEAD` can equal `baseline_commit` and an undeclared/uncommitted promotion is invisible to the gate's blocking path. Deferred per Jerome's decision (2026-07-27 code review): requires both undeclared scope and leaving it uncommitted — narrow edge case, not worth changing the frozen four-workflow checker contract right now. Record as a known limitation in Dev Notes/runbook on a future pass.

- **`remote_contains()` has no staleness/shallow-clone signal for local remote-tracking refs.** `_bmad/scripts/verify_submodule_promotion.py` decides `REMOTE_COMMIT_UNAVAILABLE` purely from local `refs/remotes/*` containment, by design (never fetch). A force-pushed/rewritten remote, or a shallow submodule clone, can make a local ref falsely appear to contain (or not contain) a commit. Pre-existing tradeoff of the no-fetch-ever design, not a defect introduced incorrectly by this story.

- **Nested-gitlink dirt compensation in `submodule_dirt()` only covers the staged case.** An unstaged nested-submodule pointer change (checkout without `git add`) is invisible to both the status call and the `diff-index --cached` compensation. Nested submodules are policy-forbidden from ever being initialized in the first place, so this is defense-in-depth, not a live path today.

- **`ArchitecturePlanningAuthorityValidationTest.cs`'s new AppHost-qualification check is per-physical-line.** A future purely-cosmetic line-wrap of a qualifying phrase could produce a false conformance failure. Pre-existing test-authoring pattern in this file (substring/line-based checks), not unique to this story's addition.

- **`inspect_unrelated()` emits no warning at all for a fully-uninitialized unrelated root-declared submodule** (only initialized-but-dirty/drifted unrelated submodules warn). Not contradicted by the frozen warning-code table, but a silent blind spot worth a follow-up decision on whether uninitialized-unrelated should also warn.

- **Case-insensitive filesystem checks vs. case-sensitive git object matching.** A case-mismatched `--submodule` path could pass `own_worktree`'s filesystem check but fail the `git ls-tree`/`diff --raw` lookup with a misleading code. No Windows/macOS caller exists in this workspace today.
