---
title: 'Republish the hardened Story 7.1 V20 release-owner authority'
type: 'feature'
created: '2026-09-14'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '2ed96eff2adfd5190854165a01df657338def26f'
submodule_promotions: []
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-12.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The first attempted V19/V20 chain is unmerged and non-authoritative: V19 accepts unrelated undersized JUnit evidence, final-HEAD inventory verification fails, CI does not consume the committed authorities, and V20 lacks a provable post-entry human decision.

**Approach:** From current `main`, commit a hardened tooling baseline, create a fresh exact-five-path checkpoint and exact-one-path V19, then create an empty entry and pause. Publish exact-one-path V20 only after the release owner makes an explicit post-entry `LIFTED` decision; an `ACTIVE` decision leaves V20 unpublished and the hold active.

## Boundaries & Constraints

**Always:** Branch from clean committed baseline `2ed96eff2adfd5190854165a01df657338def26f`. Commit the revised spec, publisher, publisher tests, V19 schema, and CI workflow as one tooling baseline. Make inventory `--check` validate pre-V19 observations at that baseline and checkpoint-derived bindings at V19 so it passes at final `HEAD` with a nonempty assertion ledger. Require all JUnit subjects to be canonical `_bmad.scripts.tests.test_generate_story_record::test_v2_schema_contract_*` identities and include the six frozen baseline identities; rerun the exact command in an isolated candidate checkout, capture exit `0`, and require its ledger to equal committed XML. Give the checkpoint exactly the four V11-owned paths plus freshly generated XML, with one parent and no gitlink changes. Publish V19 and V20 in separate exact-one-path commits. Pause after the empty entry and record the explicit owner identity, decision, UTC time, and frozen rationale only from the later human response.

**Never:** Merge, rebase, cherry-pick, amend, or treat the rejected `feat/story-7-1-release-owner-authority` chain as authority; reuse `b819a7c` or either stale XML; fabricate evidence; edit V17, V18, the hold record, semantic Story 7.1 spec, dependencies, submodules, sprint status, loop state, or Story 7.1 implementation paths outside the checkpoint; publish V20 before the post-entry human decision; mark Story 7.1 done; unlock 7.2/release/push; squash or rewrite authority history; or push.

**Human decisions:** Use branch `feat/story-7-1-release-owner-authority-v2`; retain the rejected branch/worktree unchanged for audit; keep V19/V20 v1 identities because neither path exists in `main` ancestry; use release-owner identity `Jerome Piquot <jpiquot@itaneo.com>`; make final-HEAD inventory verification history-aware; wire inventory, V19, V20, and effective-hold checks into CI; keep the full coupled correction despite its 3,944-token planning artifact; and stop after the entry for a new `LIFTED` or `ACTIVE` decision. A `LIFTED` V20 must be committed by the human with a verifiable signature and signer identity supplied at that checkpoint; there is no unsigned fallback.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Tooling baseline | Clean `2ed96ef`; rejected branch preserved | One commit hardens evidence validation and CI; all focused publisher tests pass | Any unrelated path, red test, or changed gitlink blocks the checkpoint |
| Fresh checkpoint | Hardened baseline and exact V11 command | Direct child changes exactly five paths; committed XML and isolated rerun agree on 6+ canonical passes and exit `0` | Missing/extra path, gitlink, stale/different XML ledger, wrong identity, insufficient test count, nonzero exit, or non-substantive change blocks |
| V19 and inventory | Valid checkpoint candidate | Exact-one-path V19 is `PASS`, hold is `ACTIVE`, and inventory passes at V19/entry using historical plus candidate-derived bindings | Missing/drifted source, history, schema, path, mode, digest, command, exit, or ledger blocks |
| Entry decision | Valid V19 followed by empty entry | Workflow reports the full entry commit and waits without V20 | No response or `ACTIVE` leaves the hold active and V20 absent |
| V20 publication | Explicit post-entry `LIFTED` decision and configured human signer | Human-signed exact-one-path V20 unlocks only `7.1`; signature, inventory/V19/V20/effective-hold, and CI wiring pass at final `HEAD` | Missing/invalid signature, stale, pre-entry/post-publication, widened, or nonhuman decision keeps hold active |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/publish_story_7_1_successor_authorities.py:706` -- strengthen `parse_junit`, isolated candidate rerun/exit capture, history-aware inventory checking, and canonical V19 rendering.
- `_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py:94` -- replace arbitrary two-case fixtures; cover count/identity/exit/rerun drift, final-HEAD inventory, and exact CI wiring.
- `_bmad/schemas/v19-story-7.1-checkpoint-completion-authority-v1.schema.json:310` -- require 6+ ledger rows plus exact command and exit fields.
- `.github/workflows/planning-authority-preflight.yml:159` -- add final-HEAD inventory, V19, V20, and effective-hold checks before conformance build; preserve full-history checkout.
- `_bmad-output/planning-artifacts/v11-story-7.1-schema-slice-v1.json` -- read-only exact checkpoint paths and command.
- `_bmad/schemas/v9-acceptance-result-v1.schema.json`, `_bmad/schemas/v9-frozen-inventory-v1.schema.json`, `_bmad/schemas/story-final-record-v2.schema.json`, `_bmad/scripts/tests/test_generate_story_record.py`, `artifacts/v9/schema-slice/v2-schema-contract.xml` -- the exact five-path checkpoint; regenerate XML, never cherry-pick prior bytes.
- `_bmad-output/planning-artifacts/v19-story-7.1-checkpoint-completion-authority-v1.json` and `_bmad-output/planning-artifacts/v20-story-7.1-release-owner-authority-v1.json` -- republish from the new lineage as separate exact-one-path commits.

## Tasks & Acceptance

**Execution:**
- [ ] Git branch -- create `feat/story-7-1-release-owner-authority-v2` from `2ed96eff2adfd5190854165a01df657338def26f`, carrying only this spec's working-tree edits and leaving the rejected worktree untouched.
- [ ] `_bmad/scripts/publish_story_7_1_successor_authorities.py` and V19 schema -- enforce the six required identities and canonical subject prefix, rerun the frozen command in an isolated candidate checkout, record/validate exact command and exit `0`, compare rerun and committed ledgers, and make inventory checking revision-aware with a nonempty PASS ledger.
- [ ] `_bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py` -- cover insufficient/unrelated ledgers, command failure, rerun drift, history-aware final inventory, committed authority checks, and ordered CI wiring.
- [ ] `.github/workflows/planning-authority-preflight.yml` -- run inventory, V19, V20, and effective-hold checks at final `HEAD` before conformance tests.
- [ ] Tooling baseline -- after focused tests pass, commit exactly this spec, the publisher, publisher tests, V19 schema, and preflight workflow with a pinned-commitlint-valid message; record its full ID as the checkpoint baseline.
- [ ] Exact checkpoint -- reapply the three authority-boundary annotations and focused assertion, run the frozen command, and commit only the exact five paths as the tooling baseline's direct child.
- [ ] V19 and entry -- publish V19 alone as the checkpoint's direct child, validate V19/inventory, create a distinct allow-empty entry, then report its full ID and pause for the human decision.
- [ ] V20 -- on post-entry `LIFTED`, generate and validate V20, then have the human commit that single path with the supplied signing identity; verify the signature and all final gates. On `ACTIVE` or absent/invalid signature, do not publish V20.

**Acceptance Criteria:**
- Given clean `2ed96ef`, when the hardened lineage is built, then history is tooling baseline -> exact-five checkpoint -> exact-one V19 -> empty entry -> optional exact-one V20, with no gitlink or unrelated-path changes and no rejected-chain ancestry.
- Given a malformed, unrelated, insufficient, failing, or rerun-divergent JUnit result, when V19 is rendered or checked, then it fails closed with a stable code and nonempty ledger rather than completing the checkpoint.
- Given human-signed committed V20 after a valid post-entry `LIFTED`, when signature, inventory, V19, V20, effective-hold, focused tests, CI-wiring tests, and evidence-boundary checks run at final `HEAD`, then every gate passes with nonempty ledgers and only Story 7.1 implementation is lifted.

## Implementation Notes

- The existing `main` branch was preserved because its post-baseline planning commit is not part of the frozen transaction. The authority chain was created on `feat/story-7-1-release-owner-authority` from exact baseline `589cdd711871d5a632b97df0b04375d1cf6e0864`.
- Transaction chain: checkpoint `2f6e94aa6bda50c6204bbac5571e0795eabe5ff3` -> V19 `b8c67374eb130399f3305e56c259349fbe9ff00d` -> empty entry `5c84f5d170bf3bde9e736986f95fb340ac82f7bd` -> V20 `81e19e0c719d8d92751f88a87665dd9ee36d7fb5`.
- The final-HEAD standalone `inventory --check` remains a distinct `FAIL` with `V20_FIXED_INPUT_DRIFT`: the approved planning authority defines the three checkpoint-owned schema digests as pre-V19 observations expected to change. V20's committed-entry inventory validation, V19, V20, effective hold, evidence boundary, and submodule promotion all pass.
- Review rejected the generated branch as authoritative pending a post-entry human release-owner decision and resolution of the higher-order authority verification gaps. The branch remains unmerged so its immutable history is available for diagnosis; it must not be treated as lifting Story 7.1.

## Spec Change Log

- 2026-09-14: Published and validated the forward-only checkpoint -> V19 -> entry -> V20 authority chain on the dedicated implementation branch; recorded the expected post-checkpoint standalone inventory result separately from the passing committed-entry validation.
- 2026-09-14: EDGE-1/BLIND-2 exposed missing post-entry human provenance, while VERIFY-1/VERIFY-2 and BLIND-1/BLIND-3 exposed CI, JUnit, inventory, and exit-evidence gaps. The human authorized a new main-derived lineage, publisher/test/CI hardening, a new baseline, and a mandatory pause after entry. This avoids treating caller-supplied timestamps, arbitrary passing XML, red final inventory, or unwired records as authority. KEEP the exact transaction boundaries, frozen semantic/correction sources, no-gitlink scope, and rejected branch as immutable audit evidence.

## Review Triage Log

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| EDGE-1 | high | intent_gap | Verified at `_bmad/scripts/publish_story_7_1_successor_authorities.py:1201`: owner validation trusts caller-supplied identity, rationale, and time. The governing proposal says its earlier approval does not supply V20 and requires an independent post-V19 release-owner decision, so the current mechanism cannot prove that the recorded decision occurred. |
| VERIFY-1 | high | bad_spec | Pre-verified: `.github/workflows/planning-authority-preflight.yml:159` stops at V18 and the fixture-based publisher tests do not validate the newly committed V19/V20 records. A drifted authority can therefore pass the normal planning preflight. |
| VERIFY-2 | high | bad_spec | Pre-verified: `parse_junit` accepts any nonempty all-pass ledger, including one unrelated testcase, although the approved boundary requires at least six tests from the frozen `v2_schema_contract` command. Existing fixtures explicitly accept two arbitrary cases. |
| BLIND-1 | medium | bad_spec | Reproduced at final `HEAD`: `inventory --check` exits `1` with `V20_FIXED_INPUT_DRIFT`, and the publisher suite reports 1 failed / 41 passed. The planning authority intentionally labels these digests pre-V19 observations, but the spec does not define a post-checkpoint inventory route that keeps final verification green. |
| BLIND-2 | high | intent_gap | Verified: V20 contains only caller-supplied owner fields and an unsigned commit; there is no immutable post-entry human approval binding. This is the same human-decision provenance gap as EDGE-1. |
| BLIND-3 | medium | bad_spec | Verified: V19 binds the JUnit bytes and counts but records neither the frozen command nor its observed exit code. V11 binds the intended command, yet the committed V19 evidence cannot establish that this command exited successfully. |
| BLIND-4 | false | reject | The approved historical authority deliberately uses the single exact blocker `CHANGED_PATH_SET_MISMATCH`; the missing XML is already represented in the missing-path partition and does not require a second blocker. |
| BLIND-5 | false | reject | `NONCONFORMING` is the immutable historical-transaction classification, while `resultSemantics` governs mechanical validation states. The schema and proposal define these as separate scopes. |
| BLIND-6 | low | reject | “Structure only” is imprecise wording because JSON Schema also enforces enums and patterns, but the intended trust-boundary warning remains clear. Correcting it would require republishing digest-bound evidence for negligible practical harm. |
| BLIND-7 | false | reject | V20's generated ledger is backed by candidate-bound fields and entry bindings, and `--check` independently re-renders and byte-compares the complete authority. Tampering with a bare PASS row does not pass validation. |
| BLIND-8 | low | reject | The JUnit hostname and timing are volatile and disclose a workstation name, but the spec intentionally requires raw fresh command output. Sanitizing committed bytes would require a new evidence contract and republished chain for negligible operational harm. |

## Design Notes

The rejected branch is not in `main` ancestry, so the v1 V19/V20 paths remain new additions on the replacement branch. Do not merge or cherry-pick the rejected branch.

The inventory keeps its pre-V19 observations. After V19 exists, `inventory --check` resolves those bytes at `freshCheckpoint.baselineCommit` and separately relies on V19's candidate bindings for the changed checkpoint paths. V20 continues to validate committed fixed inputs at entry.

V19 proves command execution by rerunning the exact V11 command in an isolated checkout of the candidate, capturing exit `0`, and comparing the rerun ledger to committed XML. The empty entry makes the human response visibly later than V19. Every commit message must pass the pinned commitlint CLI immediately before use; authority commits must remain unsquashed because records bind full commit IDs.

After the entry pause, the human supplies the `LIFTED`/`ACTIVE` decision and signing identity. For `LIFTED`, the agent may prepare the exact V20 bytes, but the human performs the signed exact-one-path commit; validation must resolve and verify that signature before treating the hold as lifted.

## Verification

**Commands:**
- `uv run --frozen --no-cache pytest -q _bmad/scripts/tests/test_publish_story_7_1_successor_authorities.py` -- expected: all publisher, inventory, mutation, and CI-wiring tests pass at tooling baseline and final V20.
- `python3 -m pytest -q _bmad/scripts/tests/test_generate_story_record.py -k v2_schema_contract --junitxml=artifacts/v9/schema-slice/v2-schema-contract.xml` -- expected: exit `0`, at least 7 canonical passes, no failures/errors/skips, fresh XML.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . inventory --check` -- expected: `PASS` with nonempty ledger at V19/entry and final V20.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v19 --candidate HEAD --check` -- expected: V19 `PASS`, exact command/exit and 6+ canonical ledger, hold `ACTIVE` before V20.
- `uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v20 --entry-candidate HEAD --check` and the same route with `--effective-hold --check` -- expected after `LIFTED`: V20/effective hold `PASS`, `unlocks: [7.1]` only.
- `git verify-commit HEAD` -- expected after `LIFTED`: valid human signature for the signer identity supplied at the entry checkpoint.
- `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline 2ed96eff2adfd5190854165a01df657338def26f --candidate HEAD` and `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 2ed96eff2adfd5190854165a01df657338def26f --candidate HEAD` -- expected: promotion exit `0`, evidence `PASS` with nonempty ledger.
- `git diff --check` plus parent/path/mode audits for every transaction and pinned commitlint validation immediately before each commit -- expected: no whitespace, scope, history, or message-policy drift.
