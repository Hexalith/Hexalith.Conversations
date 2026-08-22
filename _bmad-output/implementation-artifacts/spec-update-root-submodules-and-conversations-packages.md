---
title: 'Update root submodules and Conversations packages'
type: 'chore'
created: '2026-08-22'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '62f27c452b7ef8fb8d1f2a1c88e62e8c792b3893'
submodule_promotions:
  - path: 'references/Hexalith.AI.Tools'
    require_remote: true
  - path: 'references/Hexalith.EventStore'
    require_remote: true
  - path: 'references/Hexalith.Projects'
    require_remote: true
  - path: 'references/Hexalith.Folders'
    require_remote: true
  - path: 'references/Hexalith.Tenants'
    require_remote: true
  - path: 'references/Hexalith.FrontComposer'
    require_remote: true
  - path: 'references/Hexalith.Parties'
    require_remote: true
  - path: 'references/Hexalith.Memories'
    require_remote: true
  - path: 'references/Hexalith.Commons'
    require_remote: true
  - path: 'references/Hexalith.Builds'
    require_remote: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/docs/runbooks/submodule-promotion-completion-gate.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Conversations' recorded root gitlinks lag six clean upstream `main` heads, while its owned Node and Python verification packages have newer compatible stable releases. The July submodule-delivery spec is immutable historical evidence and cannot safely represent this current dependency refresh.

**Approach:** Additively update every root-declared submodule to the exact latest `origin/main` commit, refresh only Conversations-owned compatible stable packages and lockfiles, and validate the selected dependency graph without modifying dependency repositories.

## Boundaries & Constraints

**Always:** Preserve all unrelated worktree files; operate only on root-declared submodules without nested traversal; record full remote-resolvable SHAs; keep submodule worktrees clean; update generated lockfiles with their owning package manager; keep the AppHost SDK aligned with the imported Builds catalog; preserve distinct lifecycle evidence results.

**Ask First:** Modifying content or package catalogs inside a submodule; adopting Aspire 13.5's breaking stack; changing the .NET SDK feature band; reconciling or pushing the divergent root branch; changing repository secrets or remote history.

**Never:** Edit `spec-ship-breaking-submodule-updates.md` or frozen historical evidence; use recursive or `--remote` submodule commands; add local NuGet version overrides; downgrade intentional preview packages; weaken tests, warnings, evidence gates, or the active V14 hold; stage unrelated files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Outdated root gitlink | Clean submodule with newer `origin/main` | Parent gitlink records that exact full SHA | Block if dirty, non-fast-forward, or not remotely contained |
| Current root gitlink | Recorded SHA equals latest `origin/main` | Leave gitlink byte-identical and record verification | Fail on an unverified default branch or missing remote head |
| Owned package update | New compatible stable npm/Python release | Manifest and lockfile select the same release | Revert only the package update if locked install or focused tests fail |
| Incompatible latest package | Aspire 13.5.2 conflicts with Builds 13.4.6 | Retain the aligned 13.4.6 stack | Require a separately approved coordinated Builds/Aspire change |
| Dirty umbrella tree | Unrelated tracked or untracked files exist | Preserve and exclude them from the dependency boundary | Stop on overlap or unexpected staged paths |

</frozen-after-approval>

## Code Map

- `.gitmodules` -- authoritative inventory of the ten allowed root submodules; read-only.
- `references/Hexalith.{AI.Tools,EventStore,Projects,Folders,Tenants,FrontComposer,Parties,Memories,Commons,Builds}` -- exact root gitlinks; six require fast-forward pins and four require latest-head verification only.
- `Directory.Packages.props` -- versionless import of the selected Builds catalog; do not add local pins.
- `src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj:1` -- AppHost SDK must remain aligned with Builds' Aspire `13.4.6` selection.
- `package.json`, `package-lock.json` -- Conversations-owned commitlint versions and deterministic npm graph.
- `pyproject.toml`, `uv.lock` -- Conversations-owned evidence-verifier packages and deterministic Python graph.
- `spec-ship-breaking-submodule-updates.md` -- immutable historical evidence introduced by `8f8f14f`; read-only.

## Tasks & Acceptance

**Execution:**
- [ ] All ten `references/Hexalith.*` root gitlinks -- verify latest remote `main`; record the six fast-forward SHAs and leave the four already-current pointers unchanged.
- [ ] `package.json`, `package-lock.json` -- update both commitlint packages to `21.2.2` using npm's locked workflow.
- [ ] `pyproject.toml`, `uv.lock` -- update `jsonschema` to `4.26.0` and `pytest` to `9.1.1` using uv.
- [ ] `Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`, and the AppHost project -- verify the imported catalog remains authoritative and Aspire stays aligned at `13.4.6`; make no local NuGet override.
- [ ] This spec and final staged boundary -- retain the current baseline and exclude every pre-existing unrelated path.

**Acceptance Criteria:**
- Given the root `.gitmodules` inventory, when dependency refresh completes, then every initialized clean submodule HEAD equals the latest fetched `origin/main`, every changed parent entry is mode `160000`, and no nested submodule was traversed.
- Given the owned package manifests, when locked installs and focused checks run, then commitlint `21.2.2`, jsonschema `4.26.0`, and pytest `9.1.1` resolve without adding local NuGet overrides or breaking the aligned Aspire stack.
- Given the dirty umbrella worktree, when the final boundary is inspected, then only this spec, six gitlinks, and four owned package files belong to this change; all unrelated files remain untouched and unstaged.
- Given the active V14 hold, when this maintenance change is reviewed, then it claims neither hold lift, successor activation, IR-0 authorization, release approval, nor permission to push.

## Spec Change Log

## Design Notes

The latest Builds head already supplies current stable versions for eight of ten direct ordinary NuGet dependencies. Aspire.Hosting and Aspire.Hosting.Testing `13.5.2` are newer but require a coordinated breaking stack update; retaining `13.4.6` is the latest compatible selection for this bounded Conversations-only change. The historical July spec remains byte-identical because its digest is frozen by Epic 5 evidence.

## Verification

**Commands:**
- Per root path, `git -C <path> fetch origin main` then compare `git -C <path> rev-parse HEAD` with `git -C <path> rev-parse refs/remotes/origin/main` -- expected: exact equality and clean status without recursion.
- `npm ci && npm run commitlint -- --from HEAD --to HEAD` -- expected: locked install succeeds and repository-pinned commitlint executes.
- `uv sync --frozen && uv run --no-cache pytest _bmad/scripts/tests` -- expected: locked Python environment and planning/evidence tests pass.
- `dotnet restore Hexalith.Conversations.slnx && dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore -m:1` -- expected: restore and warning-free build pass with the selected submodule/package graph.
- Run each `tests/**/*.csproj` individually in Release -- expected: every configured project passes or records an exact environment blocker with focused fallback evidence.
- Run `verify_submodule_promotion.py` for all ten declared rows and `verify_evidence_boundary.py` against committed `HEAD` -- expected: promotion exit `0`; evidence `PASS` or `not-applicable` with a nonempty assertion ledger.
