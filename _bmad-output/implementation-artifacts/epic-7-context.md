---
overlay_version: 'epic-6-authority-2026-08-18-v14'
architecture_version: 'conversations-architecture-2026-09-20-v22'
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

- Produce exactly one authoritative JSON final record per story and one digest-bound deterministic Markdown rendering. Derive counts, commits, repository-relative paths, root gitlinks, scenario results, and verdicts; caller-authored completion facts are prohibited.
- Bind each record to its planning authority and candidate, story candidate, baseline, frozen inventory, declared input and output digests, predecessor record digests, rollback boundary, scenario results, and every root gitlink.
- Required test evidence must be current, nonempty, passing, and free of failed, unapproved-skipped, or not-run checks. Missing or stale results, zero assertions, and environmental inability cannot pass; environmental inability is `BLOCKED`.
- Derive one exact changed-path set using slash-separated repository-relative paths. Reject `..`, backslashes, unrelated dirt, and paths below root-declared `references/` gitlinks. Never initialize, update, or traverse submodules to collect evidence.
- Resolve gitlinks only from raw root-tree mode-`160000` entries and require exact equality with the ordinal root `.gitmodules` inventory. Missing, extra, unresolved, non-gitlink, or moved entries block completion.
- Every governed review or done transition and generated workflow twin must invoke and verify the same generator before changing lifecycle state. `FAIL` or `BLOCKED` preserves the pre-review state.
- Historical verification is read-only. Preserve prior authority and evidence, accepted baselines, completed records, signed evidence, public value sets, and gitlink evidence as immutable point-in-time history. Every frozen fault mutation must produce its required blocker and restore fixtures byte-identically.
- The implementation hold is `ACTIVE`. V22 authorizes no Story 7.1 implementation, product or dependency change, submodule or gitlink change, sprint transition, release, push, or `EXECUTION_ALLOWED`. AR-15 completes only after repository-owner review, exact-candidate ordinary CI, owner fast-forward, and a post-merge resolver `PASS`; Story 7.1 still requires a separate owner-approved successor entering `EXECUTION_ALLOWED`.
- Story entry also requires the current workflow inventory, marker resolver, conformance gate, and production operational envelope gate to pass; current candidate-compatible predecessor, checkpoint, readiness, input-inventory, and story-contract bindings must be valid. Preservation, performance, and landing-zone blockers require closure or an explicit valid disposition. Until every entry gate passes, Epic 7 and Stories 7.1-7.4 remain `backlog`.

## Technical Decisions

- The governing pair is the immutable Epic V14 backlog overlay and Architecture V22. V22 is the last complete architecture marker; it supersedes only V16's pending-recovery state and inherits every other V16 decision and earlier invariant not explicitly replaced.
- Current authority is marker-driven. Follow the last complete architecture marker and run the committed current-authority resolver against the evaluated candidate; never infer live authority from filenames, a planning bundle, a raw sidecar `authorityEffect`, or a historical `LIFTED` value. Missing input, digest mismatch, incomplete history, nonzero exit, candidate drift, or gitlink drift resolves fail-closed to `ACTIVE`.
- V22 establishes the repository-pinned `current-planning-authority` route. The repository owner is the AR-15 trust root; the V21 `ci-trust` publisher and tests are historical only. GitHub rulesets, external validators, no-bypass proofs, check-run identity, and nonces are not part of the current recovery route.
- Story 7.1 follows the strict terminal state machine `ACTIVE -> EXECUTION_ALLOWED -> MERGE_CANDIDATE_VERIFIED -> ACCEPTED`. Merge proof binds the story candidate, protected merge tree and parents, admissible integration paths, and every root gitlink; terminal authority also binds the final-record digest and actual protected-main commit. `ACCEPTED` is immutable.
- Closed contracts govern story contracts, acceptance results, frozen inventories, and final records. JSON is authoritative, unexpected properties fail unless explicitly allowed, non-semantic ordering is ordinal, and inventory digests use NFC UTF-8 obligation IDs with one LF-terminated ID per line.
- Machine commands use exit `0` for `PASS`, `1` for `FAIL`, and `2` for `BLOCKED`. A passing final record requires every declared scenario to pass, with zero failed, blocked, skipped, or not-run scenarios.

## Cross-Story Dependencies

Completed Story 6.2 is the immutable hard predecessor; superseded Story 6.8 and its partial implementation are unaccepted inputs only. The Epic 7 chain is strict: 7.1 establishes the contracts and generator, 7.2 derives measured facts, 7.3 gates all completion transitions, and 7.4 proves read-only history and the fault matrix. Story 7.2 remains locked until Story 7.1 reaches `ACCEPTED` through a separate atomic terminal-authority publication; it cannot consume a raw final record, pull-request result, or historical lift field. Epic 7 exits only when Stories 7.1-7.4 are done at compatible accepted candidates.
