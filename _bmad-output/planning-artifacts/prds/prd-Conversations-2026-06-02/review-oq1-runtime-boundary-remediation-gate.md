# OQ-1 AppHost Runtime-Boundary Remediation Gate

**Date:** 2026-09-17  
**Gate result:** PASS for the working-tree remediation  
**OQ-1 aggregate result:** BLOCKED  
**Release/acceptance effect:** None

## Scope

This gate reviews the test-harness-only AppHost runtime-boundary remediation, its three raw xUnit XML results, `APPHOST-RUNTIME-BOUNDARY-WORKTREE-001`, and the governing PRD/OQ-1 state. It does not approve a release, supersede committed OQ-1 technical evidence, activate a repository grant, or lift the implementation hold.

## Reviewer-Gate Results

| Check | Result | Evidence |
|---|---|---|
| Candidate binding | PASS | Both candidate-file SHA-256 values, their deterministic bundle digest, root base commit, and EventStore/Commons gitlinks recompute exactly. |
| Topology | PASS | The hash-bound raw XML records 8 passed, 0 failed in 0.875 seconds. |
| Runtime repeatability | PASS | Two separately hash-bound raw XML results each record 1 passed, 0 failed in 80.903 and 71.461 seconds. |
| Authentication boundary | PASS | A fresh test-only secret signing key is injected only into the test-launched EventStore; the protected unauthenticated probe must return HTTP 401. Production authentication files and fail-closed defaults are unchanged. |
| Dapr watcher remediation | PASS | The test-owned profile disables HotReload for both test-launched sidecars, the harness validates and asserts the attachment, and any pre-existing sidecar configuration causes a failure rather than an overwrite. Production AppHost composition is unchanged. |
| Prior review findings | PASS | The hard readiness budget, pre-existing-config guard, Dapr content/attachment assertions, negative authentication assertion, durable two-pass evidence, and exact dirty-tree source binding are all present. |
| Evidence boundary | PASS | `verify_evidence_boundary.py --repository . --baseline HEAD^ --candidate HEAD` passed for committed OQ-1 baseline `c0abd5cb73d420bb2f4b5d04827461ad82c4528c` and explicitly reported the remediation paths as uncommitted. The current remediation therefore remains working-tree evidence only. |
| Authority separation | PASS | `OQ1-TECHNICAL-EVIDENCE-001` remains BLOCKED and all three v1 grants remain `effective=false`; the working-tree record declares no supersession or activation. |
| PRD invariant: preservation | PASS | FR-20/SM-C1 remain PENDING until one approved, hash-bound manifest maps all seven closed categories to exact tests and requirements. Every v1 test and later approved addition remains in the accumulated, non-shrinking denominator. |
| PRD invariant: performance | PASS | SM-C2 remains FAILED under the universal rule that every identified command/read hot path must regress by no more than 5%. |
| PRD invariant: hold | PASS | The implementation hold remains ACTIVE; no waiver, release approval, substitute authority, or approver was inferred. |
| Memlog coverage | PASS | Every material harness, Dapr, readiness, evidence, and unchanged-governance action was recorded before the rerun. |

## Residual

The known `MSB3277` conflict between Microsoft.IdentityModel.Tokens 8.19.2 and 8.22.0 remains unresolved. It does not invalidate this narrow harness remediation, but it continues to block an unqualified OQ-1 compatibility claim and any successor grant activation.

## Decision

The working-tree AppHost remediation is reviewer-gate clean. OQ-1 remains fail-closed because the remediation has not been committed into a version-bound successor evidence record and the compatibility warning remains unresolved. FR-20/SM-C1, SM-C2, and the implementation hold retain their prior states without exception.
