# Deferred Work Ledger

Real findings that are not actionable in the story that surfaced them. Each entry records where it came from so a later sweep can verify it against the codebase before acting.

## Deferred from: code review of spec-6-1-rebaseline-architecture-and-planning-authority (2026-07-26)

- **The planning gate has no automated execution path.** Story 6.1's stated approach is to "enforce the resulting planning contract with focused conformance tests", but the repository has no `.github/workflows` directory and no pipeline definition anywhere outside `references/`. The story's Verification section lists hand-run binary invocations. Nothing causes `ArchitecturePlanningAuthorityValidationTest` (or the other 390 conformance facts) to run on any commit, and the artifacts it guards are markdown that later agents edit without triggering a build. Until this is wired into CI or a pre-commit hook, every authority assertion is advisory. Pre-existing repository condition, not caused by Story 6.1.

- **Nested submodule administrative metadata exists under a root submodule.** `.git/modules/references/Hexalith.FrontComposer/modules/Hexalith.AI.Tools` and `.../Hexalith.Builds` exist with mtimes 2026-07-14 22:34 and 2026-07-15 01:22. Both working trees are empty (0 entries), so the nested submodules are registered in git metadata but not checked out. The timestamps predate Story 6.1's implementation window, so this is not attributable to this pass, but it sits against the standing rule never to initialize or traverse nested submodules. A later sweep should confirm whether the metadata can be cleaned up without disturbing the FrontComposer checkout.
