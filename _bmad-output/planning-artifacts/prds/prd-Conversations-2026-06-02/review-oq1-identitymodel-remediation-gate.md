# OQ-1 IdentityModel Compatibility Remediation Gate

**Date:** 2026-09-17  
**Reviewer layer:** Blind Hunter  
**Gate result:** PASS for the narrow working-tree compatibility remediation  
**Broad conformance result:** FAIL (456/472)  
**OQ-1 aggregate result:** BLOCKED  
**Release/acceptance effect:** None

## Scope

This gate reviews the final IdentityModel version-alignment change, its package-graph and package-metadata comparisons, the rebuilt test assemblies, fresh raw test results, `APPHOST-RUNTIME-BOUNDARY-WORKTREE-002`, and the governing PRD/OQ-1 invariants. It does not approve a release, approve a successor preservation manifest, supersede committed OQ-1 technical evidence, activate a repository grant, or lift the implementation hold.

The final Blind Hunter rerun reviewed 1,387,098 bytes (`1,354.59 kB`) and computed `N = min(floor(sqrt(1354.59) + 1), 10) = 10`. It returned 14 findings: 12 safe findings were remediated and 2 remain explicit runtime-test residuals. The initial review's 13 findings are correctly classified as 10 closed and 3 retained. No additional review layers were configured for this route; the complete classifications are recorded in `spec-resolve-identitymodel-version-conflict.md`.

## Reviewer-Gate Results

| Check | Result | Evidence |
|---|---|---|
| Candidate binding | PASS | `APPHOST-RUNTIME-BOUNDARY-WORKTREE-002` SHA-256 is `1b573686f1846e0fa5ece97e42d31ae83faf255edd34cc34a911e8f17e4ac496`; all four candidate hashes and deterministic bundle digest `a5efbd1c012f50ad7732b0c3254a76c76ab8b2d9db0a3524bfdf97398060d455` recompute exactly. EventStore, Commons, Tenants, and Builds gitlinks are bound. |
| Restore | PASS | Forced solution restore completed with exit code 0 and has a hash-bound `.txt` output. |
| Package scope | PASS | Baseline/candidate semantic graphs for Server, Server.Tests, Conformance.Tests, and IntegrationTests change only the seven IdentityModel-family packages from 8.19.2 to 8.22.0. The candidate solution graph is retained, but no solution-wide baseline-confinement claim is made. |
| Packable metadata | PASS | Contracts, core, Testing, and Client all pack; exact commands and aggregate exit results are retained, all eight package archives are hash-bound, extracted control/candidate nuspec SHA-256 values are identical, and the retained diff is zero bytes. The rejected repository-wide transitive-pinning candidate is absent. |
| Build compatibility | PASS | The retained Debug and Release solution builds both say `Build succeeded`, `0 Warning(s)`, and `0 Error(s)`; neither MSB3277 nor IdentityModel 8.19.2 appears in either output. |
| Executed-binary binding | PASS | Every post-build test result binds the exact executed Debug DLL SHA-256 as well as the raw XML SHA-256. Each enabled runtime pass also binds the EventStore DLL and restored asset graph launched by that pass. |
| Server tests | PASS | 684 passed, 0 failed in 2.128 seconds. |
| AppHost topology | PASS | 8 passed, 0 failed in 0.622 seconds. |
| Runtime opt-in | PASS | An explicit `env -u HEXALITH_RUN_APPHOST_BOUNDARY_TESTS` run records one skip; ordinary test execution cannot start the lane implicitly. |
| Runtime repeatability | PASS | With explicit opt-in, two separately hash-bound runs each record 1 passed, 0 failed in 93.898 and 73.932 seconds. This proves two successful boundary executions, not two fresh cutover transitions; the stable marker permits the second activation to be idempotent. |
| Integration tests | PASS | 14 passed, 0 failed in 232.117 seconds. |
| Broad conformance | FAIL | 456 passed and 16 failed. The failures are retained, including all three PreservationTraceabilityManifest checks and the explicit `Directory.Build.props` approved-hash mismatch. |
| Review remediation | PASS | Durable restore/build evidence, narrow package alignment, replayable pack commands and archive hashes, unchanged nuspecs, Debug/Release builds, post-build AppHost and per-pass EventStore binding, explicit opt-out, accurate activation/repeatability claims, complete source gitlinks, per-resource diagnostic timeouts, provenance clarification, and exact Dapr profile comparison close every safe Blind Hunter finding. |
| Evidence boundary | PASS | `verify_evidence_boundary.py --repository . --baseline HEAD^ --candidate HEAD` passes for committed baseline `c0abd5cb73d420bb2f4b5d04827461ad82c4528c` and explicitly reports the current compatibility/runtime/evidence paths as uncommitted. |
| Authority separation | PASS | Committed OQ-1 technical evidence remains BLOCKED and all v1 grants remain ineffective. No successor manifest approval, approver, owner, waiver, or authority record was inferred. |
| PRD invariant: preservation | PASS | FR-20/SM-C1 remain PENDING until one approved, hash-bound manifest maps all seven preservation categories to exact tests and requirements. Every v1 test and later approved addition remains in the accumulated, non-shrinking denominator. |
| PRD invariant: performance | PASS | SM-C2 remains FAILED under the universal rule that every identified command/read hot path must regress by no more than 5%. |
| PRD invariant: hold | PASS | The implementation hold remains ACTIVE. |
| Memlog coverage | PASS | The rejected broad candidate, final narrow change, package/pack evidence, review remediations, post-build validation, retained failure, and unchanged governance state were appended chronologically. |

## Residuals

- The runtime harness still uses the shared `dapr init` state store and a stable test cutover marker. A safe isolation change requires a separately designed topology/lifecycle update.
- The real-process lane proves a missing bearer token is rejected with HTTP 401 but does not add the reviewer's proposed invalid-signature, issuer, audience, expiry, and algorithm matrix. That is a separate authentication-coverage expansion, not part of the narrow assembly-version fix.
- FR-20/SM-C1 still require an authorized successor preservation manifest that maps all seven preservation categories to exact tests in the full, non-shrinking denominator.
- These dependencies and residuals do not invalidate the narrow compatibility result, but they are not represented as closed or waived.

## Decision

The final working-tree IdentityModel remediation passes the narrow reviewer gate: it fixes MSB3277 without changing packable NuGet dependency metadata and passes the targeted post-build regression lanes. OQ-1 remains fail-closed because the source is uncommitted and broad conformance rejects the stale approved preservation-manifest binding. FR-20/SM-C1 remain PENDING, SM-C2 remains FAILED under the universal no-more-than-5-percent rule, and the implementation hold remains ACTIVE without exception.
