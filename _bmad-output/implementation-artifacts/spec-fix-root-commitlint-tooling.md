---
title: 'Fix root commitlint tooling and complete pushall'
type: 'bugfix'
created: '2026-08-09'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '588381bb744ef2d81c0685e7f567c54f6dc37742'
submodule_promotions:
  - path: 'references/Hexalith.FrontComposer'
    require_remote: true
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The superproject cannot satisfy its mandatory pre-commit and pre-push validation because it has no owning-repository commitlint package, lockfile, or configuration. This left the already-synchronized `Hexalith.FrontComposer` gitlink advance uncommitted and prevented the superproject from being pushed last during `/pushall`.

**Approach:** Add a minimal, exact-pinned npm commitlint setup based on the strongest current `Hexalith.Builds` convention, validate both positive and negative commit-message cases, and then resume the guarded superproject commit/push with the pending gitlink included.

## Boundaries & Constraints

**Always:** Keep Node dependencies exact-pinned and reproducible through npm lockfile v3; extend `@commitlint/config-conventional`; set `defaultIgnores: false`; enforce the shared allowed-type list without `chore`; retain 200-character header/body limits; use the repository-local binary through `npx --no-install`; preserve all existing user and submodule changes; keep the final repository on `main`; validate the exact commit message before and after committing and validate the complete outgoing range before pushing; use only fast-forward synchronization and ordinary push semantics.

**Ask First:** Any need to change the intended FrontComposer gitlink target, introduce Husky or a new GitHub Actions workflow, modify shared instruction entry points, change dependency versions from the inspected `Hexalith.Builds` pins, resolve a remote divergence, or include an unexpected changed path.

**Never:** Initialize nested submodules; edit submodule contents; use recursive submodule updates; bypass hooks or commitlint; use unpinned/ad-hoc `npx` package acquisition; weaken commitlint rules; use `chore`; force-push; delete branches; rewrite history; or treat a failed npm, signature, commitlint, Git, or evidence-boundary check as success.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fresh root install | Tracked package manifest and lockfile, no `node_modules` | `npm ci` installs the exact locked commitlint toolchain | Stop before commit if install or lock consistency fails |
| Valid message | `build: sync local changes via /pushall` | Repository-local commitlint exits successfully | Do not commit until it passes |
| Forbidden type | `chore: bypass specific maintenance type` | Validation fails through `type-enum` | Treat an unexpected pass as a configuration failure |
| Git-generated/plain message | `Update subproject reference` | Validation fails because default ignores are disabled and the header is non-conventional | Treat an unexpected pass as a configuration failure |
| Pending gitlink | FrontComposer `0d0cf956…` to remote-reachable `a86176e9…` | Root commit records the exact new gitlink with the tooling files | Stop if the target changes, is dirty, or is not remote-reachable |
| Remote movement | `origin/main` changes before push | Re-fetch and require safe integration plus revalidation | Stop and report divergence/conflict; never force-push |

</frozen-after-approval>

## Code Map

- `package.json` -- new private root Node manifest; owns the exact commitlint CLI/config pins and compatible Node engine floor.
- `package-lock.json` -- new npm v3 lockfile generated from the exact root manifest; makes `npm ci` reproducible.
- `commitlint.config.mjs` -- new strict repository policy copied from the current Builds convention: conventional base, default ignores disabled, shared type allowlist, and 200-character limits.
- `_bmad-output/implementation-artifacts/spec-fix-root-commitlint-tooling.md` -- this workflow record; track its approved scope, execution status, and verification evidence with the change.
- `.gitignore:316` -- existing `node_modules/` exclusion; read-only evidence that no ignore change is needed.
- `references/Hexalith.Builds/package.json:40` -- read-only source for `@commitlint/cli` `21.2.1` and config `21.2.0` pins.
- `references/Hexalith.Builds/commitlint.config.mjs:1` -- read-only source for the policy shape; do not edit the submodule.
- `references/Hexalith.FrontComposer` -- pending gitlink only; target `a86176e9bca6721c459c513dbf0ca8249c67db03` was fast-forwarded, pushed, clean, and aligned with its `origin/main` by `/pushall`.
- `.github/workflows/planning-authority-preflight.yml` -- read-only current root CI; adding commitlint CI is outside this minimal blocker fix.

## Tasks & Acceptance

**Execution:**
- [x] `package.json` -- add a private minimal manifest with Node `>=22.12.0` and exact commitlint dependencies -- establish an owning-repository pinned validator.
- [x] `package-lock.json` -- generate and verify lockfile v3 with scripts/audit/funding disabled during install -- make bootstrap deterministic without unrelated lifecycle behavior.
- [x] `commitlint.config.mjs` -- add the strict Builds-aligned policy -- enforce Conventional Commits, reject ignored defaults, and prohibit `chore`.
- [x] Root Git state -- verify and stage only the three tooling files, this workflow record, and the intended FrontComposer gitlink; validate and create the local commit; then post-validate it -- provide committed `HEAD` evidence for review without fetching or pushing during implementation.

**Acceptance Criteria:**
- Given a clean clone with supported Node, when `npm ci --ignore-scripts --no-audit --no-fund` runs, then the exact locked toolchain installs successfully.
- Given the repository configuration, when the approved `/pushall` message and the two known-bad examples are linted, then the valid message passes and both invalid messages fail.
- Given the pending FrontComposer pointer and new tooling, when the staged diff is inspected, then only those changes plus this workflow record are present and whitespace/conflict checks pass.
- Given the locally committed implementation, when the exact message and committed diff are checked, then post-commit commitlint passes, `HEAD` records the declared gitlink, and no fetch or push has occurred during implementation.

## Spec Change Log

- 2026-08-09: Added the root commitlint manifest, npm lockfile, strict policy, and implementation verification evidence.
- 2026-08-09: Declared the approved FrontComposer promotion and assigned the local pre-review commit to the coordinator so lifecycle gates can evaluate committed `HEAD`; preserved all tooling, path-boundary, and no-push constraints.

## Design Notes

The minimal three-file setup deliberately excludes Husky, semantic-release, and a new workflow. Those are independently reviewable enforcement improvements; the immediate defect is the absence of an owning-repository pinned validator required by the existing Git policy and `/pushall` procedure. The coordinator owns the local commit needed by review and retains all fetch, push, and final remote-state operations until mandatory review passes.

## Verification

**Commands:**
- `node --check commitlint.config.mjs` -- expected: configuration parses successfully.
- `npm ci --ignore-scripts --no-audit --no-fund` -- expected: exact lockfile installation succeeds.
- `npm audit signatures` -- expected: dependency signatures verify.
- `npx --no-install commitlint --edit <temporary-message-file> --verbose` -- expected: approved message passes; known-bad messages fail.
- `git diff --check`, `git diff --cached --name-status`, and `git diff --cached --check` -- expected: exact path boundary and no whitespace or conflict-marker errors.
- `npx --no-install commitlint --last --verbose` -- expected: the locally committed message passes before review.
- Post-review coordinator gate: fresh fetch, ff-only/divergence checks, outgoing-range validation, ordinary push, and final fetch/state checks must all succeed before completion.
- Evidence-boundary validation -- expected: `not-applicable`, because no planning-authority, evidence, reader, or governing workflow artifact changes.

**Implementation evidence (2026-08-09):**

- `node --check commitlint.config.mjs` -- PASS.
- `npm ci --ignore-scripts --no-audit --no-fund` -- PASS; installed 75 locked packages.
- `npm audit signatures` -- PASS; 75 package signatures and 9 attestations verified.
- Lockfile assertions -- PASS; lockfile v3, root Node engine, exact root dependency pins, and installed `@commitlint/cli` `21.2.1` / `@commitlint/config-conventional` `21.2.0` entries verified.
- `npx --no-install commitlint --edit <temporary-message-file> --verbose` -- PASS for `build: sync local changes via /pushall`; expected FAIL for `chore: bypass specific maintenance type` (`type-enum`) and `Update subproject reference` (`subject-empty`, `type-empty`).
- Root state boundary -- PASS; unstaged paths are exactly `package.json`, `package-lock.json`, `commitlint.config.mjs`, this workflow record, and the `references/Hexalith.FrontComposer` gitlink. The index remains unchanged.
- FrontComposer gitlink -- PASS; root moves from `0d0cf956e896817abc7a01f3e7f8f4d6cb753acd` to clean checkout and local `origin/main` target `a86176e9bca6721c459c513dbf0ca8249c67db03`.
- `git diff --check` plus untracked-file whitespace and conflict-marker checks -- PASS.
- Existing fetched-root alignment -- PASS; `git merge --ff-only origin/main` reported already up to date, divergence was `0 0`, and the branch remained `main`. A fresh fetch and repeat remain coordinator-only completion gates after review.
- Evidence-boundary validation -- `not-applicable`; the changed implementation record is not planning authority, signed/release evidence, an evidence reader, or a governing workflow artifact.
- The exact local commit and post-commit lint are the coordinator's immediate pre-review gates; fresh fetch, outgoing-range, push, and final-state gates remain pending mandatory review.
