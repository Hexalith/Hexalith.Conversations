---
title: 'Publish an additive V24 Story 7.1 tooling successor'
type: 'bugfix'
created: '2026-09-21'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5a7234b922371b5d0a12085a444d93783263f278'
context:
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-7-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The accepted V23 request is an immutable exact-nine transaction, so correcting review-proven file-safety and verification gaps in a descendant produces `EVIDENCE_V23_REQUEST_DESCENDANT_SCOPE` or publisher-identity drift. Rewriting V23 would erase accepted evidence.

**Approach:** Publish one exact direct-child V24 tooling-correction transaction over V23. It authenticates the unchanged V23 publication first, then binds the corrected publisher and protected hosts through a new closed correction record without granting Story 7.1 execution.

## Boundaries & Constraints

**Always:** Preserve commit `5a7234b922371b5d0a12085a444d93783263f278`, its publisher digest, request, exact-nine manifest, V22 outcomes, and V23 BLOCKED/false semantics. Authenticate V24 before loading corrected Python; require one direct parent, exact mode-`100644` scope, self-excluding manifest and raw-gitlink equality, duplicate-safe JSON, a nonempty ledger, ACTIVE hold, and execution/release/push false. Reject ordinary V23 descendants unless they carry the exact V24 route.

**Never:** Amend V23, relax its verifier branch, publish an authority/marker, claim approval, change sprint/product/dependency/submodule/gitlink state, push, or claim protected-main acceptance. The existing protected-base host cannot recognize V24; required-check migration remains an external owner gate, not a bypass here.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Exact V24 correction | Direct child of V23 with the closed eight-path transaction | Evidence `PASS` with a nonempty ledger; authority remains non-executable | No approval or hold-lift claim |
| Historical V23 | Evaluate the immutable V23 request commit | Original publisher digest and BLOCKED/false request remain valid | No V24 reinterpretation |
| Unrouted descendant | Marker-free V23 descendant without the V24 record | Existing `EVIDENCE_V23_REQUEST_DESCENDANT_SCOPE` | `BLOCKED`, never fallback |
| V24 drift | Wrong topology, manifest, blob, schema, or gitlink | Stable V24 FAIL/BLOCKED result | No corrected publisher execution |
| Protected landing | Existing protected-base workflow evaluates V24 | Visible bootstrap blocker until owner coordinates a successor-aware trusted host | Never weaken or bypass the check |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/v24-story-7.1-entry-tooling-correction-v1.json` and `_bmad/schemas/v24-story-7.1-entry-tooling-correction-v1.schema.json` -- closed self-excluding contract generated last.
- `_bmad/scripts/publish_story_7_1_entry_authority.py` -- retain historical V23 validation, authenticate the exact V24 correction, and carry the reviewed no-follow, rollback-close, and visible-path fixes.
- `_bmad/scripts/resolve_current_planning_authority.py` -- keep the original V23 pin; authenticate the new V24 publisher pin before future V23 authority dispatch.
- `_bmad/scripts/verify_evidence_boundary.py` -- route V24 before the unchanged V23 request branch and independently verify both transactions.
- The three corresponding `_bmad/scripts/tests/test_*.py` modules -- prove review faults, historical parity, exact routing, and protected-base bootstrap visibility.
- `.github/workflows/planning-authority-preflight.yml` -- unchanged; protected-base materialization remains an external landing constraint.

## Tasks & Acceptance

**Execution:**
- [ ] V24 schema/record and publisher -- implement closed correction generation/validation, preserve V23 pins, and generate the record after the other seven blobs.
- [ ] Resolver/verifier -- add pinned V24 topology, schema, publisher, scope, result, and descendant checks without changing V23 rejection.
- [ ] Three test modules -- retain the reviewed safety regressions; add hard-link and raw duplicate production-route faults plus V23/V24 history, routing, tamper, and base-host bootstrap coverage.
- [ ] Exact transaction -- create one local direct-child commit containing only the eight declared mode-`100644` paths; exclude all spec, deferred-work, lifecycle, authority, marker, dependency, and gitlink changes.

**Acceptance Criteria:**
- Given immutable V23, when successor-aware hosts evaluate V24, then both lifecycle gates pass nonvacuously over exactly eight paths while execution remains false.
- Given V23 or any malformed/unrouted descendant, when evaluated, then its historical result or stable blocker is unchanged and corrected candidate code is never trusted first.
- Given the local V24 candidate, when handed off, then protected landing and required-check migration are explicitly unresolved external gates rather than claimed success.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

V24 is a correction overlay, not a replacement request. Hosts prove V23 first, then the V24 direct-child manifest and corrected-publisher pin; arbitrary descendants remain invalid.

## Verification

**Commands:**
- `uv run --frozen --no-sync python3 -m pytest -q -ra -o xfail_strict=true _bmad/scripts/tests/test_resolve_current_planning_authority.py _bmad/scripts/tests/test_verify_evidence_boundary.py _bmad/scripts/tests/test_publish_story_7_1_entry_authority.py` -- expected: exit 0, skip-free PASS.
- `python3 _bmad/scripts/verify_submodule_promotion.py --repository . --baseline 5a7234b922371b5d0a12085a444d93783263f278 --candidate HEAD` -- expected: PASS.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline 5a7234b922371b5d0a12085a444d93783263f278 --candidate HEAD` -- expected: PASS with exact eight paths and a nonempty ledger.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline e0b098fa1c056385e28ee8ac0efd0c55dfab324f --candidate 5a7234b922371b5d0a12085a444d93783263f278` -- expected: historical V23 request PASS with its original publisher digest.
- `git diff-tree --no-commit-id --name-only -r HEAD` and `git ls-tree -r HEAD -- <eight V24 paths>` -- expected: exact scope, mode `100644`, no gitlink.
- `npx --no-install commitlint --config commitlint.config.mjs --from 5a7234b922371b5d0a12085a444d93783263f278 --to HEAD --verbose` and `git diff --check` -- expected: accepted commit message and no whitespace errors.
