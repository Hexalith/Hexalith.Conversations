---
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-19-v16'
---

# Epic 7 Context: Reliable Mechanical Completion Records

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Provide deterministic, candidate-bound completion records whose test counts, exact changed-path set, candidate identity, submodule condition, root gitlink state, and verdict are derived from machine results and Git objects rather than copied by a caller. This prevents review or completion with vacuous scope, stale or incomplete tests, dirty or moved dependencies, displaced workflow integration, or rewritten historical evidence.

## Stories

- Story 7.1: Define the final-record schema and deterministic generator core
- Story 7.2: Derive test, path, candidate, submodule, and gitlink facts
- Story 7.3: Integrate generation into every blocking completion transition
- Story 7.4: Verify historical mode and required fault-injection blockers

## Requirements & Constraints

- Produce exactly one authoritative JSON final record per story and one digest-bound deterministic Markdown rendering. Derive counts, commits, paths, gitlinks, scenario results, and verdicts; caller-authored completion facts are prohibited.
- Bind each record to the planning authority and candidate, story candidate, baseline, frozen inventory, declared input/output digests, predecessor record digests, rollback boundary, scenario results, and every root gitlink.
- Required test evidence must be current, nonempty, passing, and free of failed, unapproved-skipped, or not-run checks. Missing or stale results, zero assertions, and environmental inability cannot pass; environmental inability is `BLOCKED`.
- Changed paths must be one exact repository-relative, slash-separated set with no `..`, backslashes, unrelated dirt, or path below a root `references/` gitlink. Never initialize, update, or traverse submodules for evidence.
- Resolve gitlinks only from raw mode-`160000` root-tree entries and require exact equality with the ordinal root `.gitmodules` inventory. Missing, extra, unresolved, non-gitlink, or moved entries block completion.
- Every governed review/done transition and generated workflow twin must invoke and verify the same generator before changing lifecycle state. `FAIL` or `BLOCKED` preserves the pre-review state.
- Historical verification is read-only. Preserve V1-V21 authority and evidence, accepted baselines, completed records, signed evidence, public value sets, and gitlink evidence as immutable point-in-time history. Prove all frozen fault mutations produce their required blockers and restore fixtures byte-identically.
- The implementation hold is `ACTIVE`; discovery and context compilation grant no implementation, review, completion, release, or push authority. Story 7.1 may enter implementation only when the current-state resolver passes without drift, the repository owner approves the exact one-commit AR-15 V22 parent/candidate tuple, `main` fast-forwards to that candidate, and the resolver passes again on committed `main`; the current workflow inventory/resolver/conformance gate and production operational envelope gate must also pass, and a later owner-approved AD-4 successor must enter `EXECUTION_ALLOWED` for the exact merge candidate.
- That successor must also bind current, compatible Story 6.2, `7.1-SCHEMAS`, IR-0, V19 checkpoint, V20 input-inventory, and Story 7.1 evidence; independently close or validly disposition FR-20/SM-C1, SM-C2, and OQ-1; and retain exactly one sprint row for Epic 7 and each Story 7.1-7.4. Until all gates pass, all five lifecycle rows remain `backlog`.

## Technical Decisions

- The governing pair is the immutable Epic V14 backlog overlay plus Architecture V16. V16 is the last complete architecture marker, selects `v21-story-7.1-authority-correction-v1.json` at SHA-256 `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`, inherits every earlier invariant it does not explicitly supersede, and replaces V15's AR-15 joint ownership, blocked status, deferred action, GitHub-ruleset, external-validator, no-bypass, check-run, and nonce mechanism. The repository owner is the sole AR-15 approval role.
- Live execution state is never inferred from filenames, the planning bundle, or a raw sidecar `authorityEffect`. Follow the last complete marker, verify its selected sidecar digest, and run the repository-pinned current-state checker at the evaluated commit. Missing input, digest mismatch, incomplete history, nonzero exit, `FAIL`, `BLOCKED`, candidate drift, or gitlink drift resolves fail-closed to `ACTIVE`.
- V21's recorded lift is historical at its publication candidate, not live authorization. At planning status commit `2c6a4af775eaa969deb4bbbfc294be1c472afd7d`, the designated diagnostic reports `V21_DESCENDANT_GITLINK_DRIFT`. At V16 publication, AR-15 is `PENDING_V22_RECOVERY`; the V22 recovery artifacts, generic resolver, and production operational envelope are absent. Live state must be recomputed by the selected resolver. FR-20/SM-C1 remains pending, SM-C2 failed, and OQ-1 blocked.
- AR-15 recovery is a repository-owner-approved, repository-validated atomic V22 transaction run locally and in ordinary CI. The candidate is one commit directly after the approved current-`main` parent and integrates only by fast-forward. The new route does not invoke GitHub rulesets, an external validator service, no-bypass proof, external check identity, or a nonce; the old V21 `ci-trust` publisher/tests are frozen historical verification. V22 must preserve `ACTIVE`, changes no product, dependency, submodule, story status, release, or push state, and cannot itself authorize Story 7.1. Any later planning-authority publication must use the current route inventory and generic marker resolver with its Quality conformance gate.
- Story 7.1 uses the strict terminal state machine `ACTIVE -> EXECUTION_ALLOWED -> MERGE_CANDIDATE_VERIFIED -> ACCEPTED`. Its merge proof binds the story candidate, protected merge tree and parents, admissible integration paths, and every root gitlink; terminal authority additionally binds the final-record digest and actual protected-main commit. `ACCEPTED` is immutable.
- Closed contracts are story-contract v1, acceptance-result v1, frozen-inventory v1, and story-final-record v2. JSON is authoritative; unknown properties fail unless explicitly allowed. Non-semantic ordering is ordinal, and inventory digests use NFC UTF-8 obligation IDs with one LF-terminated ID per line.
- Machine commands use exit `0` for `PASS`, `1` for `FAIL`, and `2` for `BLOCKED`; required direct-test environment failures remain blocked and no-test execution fails. A passing record requires every declared scenario to pass, with `required=passed` and zero failed, blocked, skipped, or not-run scenarios.

## Cross-Story Dependencies

Completed Story 6.2 is the immutable hard predecessor; superseded Story 6.8 and its partial implementation are unaccepted inputs only. The Epic 7 chain is strict: 7.1 establishes the contracts and generator, 7.2 derives measured facts, 7.3 gates all completion transitions, and 7.4 proves read-only history and the fault matrix. Story 7.2 remains locked until Story 7.1 reaches V15 AD-4 `ACCEPTED` through a separate atomic terminal-authority/pointer publication; it cannot consume a raw final record, pull-request result, or V21 `LIFTED` field. Epic 7 exits only when Stories 7.1-7.4 are done at compatible accepted candidates.
