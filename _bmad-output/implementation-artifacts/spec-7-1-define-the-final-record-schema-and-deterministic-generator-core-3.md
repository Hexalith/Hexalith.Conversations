---
title: 'Define the final-record schema and deterministic generator core'
type: 'feature'
created: '2026-09-20'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/v9/story-contracts/7.1.json'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The four closed Story 7.1 contracts exist, but the generator is v1-only and cannot execute the six candidate-bound scenarios or emit the authoritative JSON/Markdown pair. Current V22 authority also fails closed and forbids implementation.

**Approach:** After separate authority work makes the current resolver pass with Story 7.1 `EXECUTION_ALLOWED`, add an isolated v2 route that reuses hardened Git/evidence seams and preserves v1. Generate the final pair only from the committed story candidate and measured results.

## Boundaries & Constraints

**Always:** Before Story-owned edits, require the V22 resolver, current owner successor, holds, and operational-envelope gate to authorize the exact candidate; revalidate rather than trust historical V20/V21 lift fields. Derive candidates, paths, raw mode-`160000` root gitlinks, exits, digests, verdicts, and JUnit ledgers. For each direct testcase in the single direct pytest suite, emit `<scenarioId>#<four-digit ordinal>`, exact `classname::name`, and `PASS` only when no direct failure/error/skipped child exists. Preserve scenario order, deterministic UTF-8/LF output, the self-excluding JSON digest, Markdown-byte digests, and summary `6/6/0/0/0/0`.

**Never:** Repair/publish authority within 7.1; infer authorization from historical fields or sprint status; accept caller facts; weaken schemas; change v1 behavior, planning, sprint status, dependencies, product code, submodules, or gitlinks; traverse submodules; implement 7.2–7.4; or hand-author evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Entry gate | Current authority/candidate | Start only on current `PASS` plus `EXECUTION_ALLOWED` | Preserve files; report exact `FAIL`/`BLOCKED` |
| Bundle | Identical validated inputs | Byte-identical schema-valid JSON/Markdown | Stable content/digest blocker |
| Bad evidence | Malformed input, caller facts, or empty derivation | Closed failure; outputs unchanged | Exact input, argument, caller, or anti-vacuity blocker |

</frozen-after-approval>

## Code Map

- `_bmad/scripts/resolve_current_planning_authority.py` -- V22 preflight; current `HEAD` is `FAIL / CANDIDATE_GRAPH_DRIFT`, hold active.
- `_bmad/scripts/generate_story_record.py:225` -- reuse Git, containment, snapshot, commit, `.gitmodules`, raw-tree, and rendering helpers; dispatch exact `--contract` before the legacy parser at `:2688`.
- `_bmad/schemas/story-final-record-v2.schema.json:291` and `_bmad/schemas/story-record-generator-failure-v1.schema.json` -- complete embedded results and add the absent pre-identity failure contract.
- `_bmad/scripts/tests/test_generate_story_record.py:1458` -- retain schema tests; extend hermetic fixtures for all six scenarios, faults, restoration, and v1 regression.
- `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json:25` -- exact commands/order/outputs; V20 input inventory is historical until revalidated.
- `docs/runbooks/story-final-record-generation.md` and `docs/release-evidence/story-7.1-final-record-v2.{json,md}` -- operator contract and generated outputs.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad/scripts/resolve_current_planning_authority.py` -- stop without Story edits unless every current entry gate passes.
- [ ] `_bmad/schemas/story-final-record-v2.schema.json` and `_bmad/schemas/story-record-generator-failure-v1.schema.json` -- close complete result/failure shapes.
- [ ] `_bmad/scripts/generate_story_record.py` -- implement isolated v2 parsing, measured JUnit derivation, deterministic validation/rendering/digests, and atomic outputs while preserving v1.
- [ ] `_bmad/scripts/tests/test_generate_story_record.py` -- cover all frozen selectors, faults, anti-vacuity, restoration, deterministic reruns, and v1 regression.
- [ ] `docs/runbooks/story-final-record-generation.md` and `docs/release-evidence/story-7.1-final-record-v2.{json,md}` -- document behavior, then generate AC-06 from the committed candidate after AC-01–05 pass.

**Acceptance Criteria:**
- Given authority is missing, stale, failing, or non-executing, when entry runs, then Story files stay unchanged and distinct `FAIL`/`BLOCKED` remains visible.
- Given identical validated inputs, when v2 runs twice, then bytes match, validate, cross-bind digests, and include ten raw gitlinks in frozen order.
- Given malformed, caller-authored, empty, failing, skipped, or not-run evidence, when tested, then no pass is possible, its stable blocker appears, and fixtures restore byte-identically.
- Given AC-01–05 pass on one committed candidate, when AC-06 runs, then it embeds nonempty ordered ledgers and generates the authoritative pair with `6/6/0/0/0/0`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

V22 is live. Its historical candidate PASS keeps the hold active, while current `HEAD` fails graph validation. Authority repair and owner authorization are separate work.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate HEAD --check` -- expected before edits: exit 0, `PASS`, nonempty assertions, and current Story 7.1 execution authorization.
- Execute `scenarios[0]` through `scenarios[4]` `.command` values from `_bmad-output/planning-artifacts/v9/story-contracts/7.1.json` verbatim -- expected: five current, nonempty `PASS` JUnit files bound to one candidate.
- `uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py --repository . --contract _bmad-output/planning-artifacts/v9/story-contracts/7.1.json --format bundle --output-json docs/release-evidence/story-7.1-final-record-v2.json --output-markdown docs/release-evidence/story-7.1-final-record-v2.md` -- expected: exit 0 and deterministic `PASS` outputs only from the authorized committed candidate.
- `git diff --check` -- expected: no whitespace errors.
