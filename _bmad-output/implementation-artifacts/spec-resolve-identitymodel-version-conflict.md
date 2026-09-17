---
title: 'Resolve the IdentityModel assembly-version conflict'
type: 'bugfix'
created: '2026-09-17'
status: 'reviewed-awaiting-commit'
route: 'oneshot'
review_loop_iteration: 1
context:
  - 'docs/runbooks/evidence-boundary-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Conversations restores the Microsoft.IdentityModel family at 8.19.2 while its source-referenced EventStore ServiceDefaults assembly is compiled against 8.22.0, causing MSB3277 and blocking an unqualified OQ-1 compatibility claim.

**Approach:** Align the four affected non-packable Conversations consumers with the centrally authorized 8.22.0 IdentityModel family using narrowly scoped compatibility references. Validate the full restore/build graph, package metadata, and AppHost topology/runtime boundary without changing release or PRD gate states.

</frozen-after-approval>

## Implementation Notes

- Investigation reproduced MSB3277 in `Hexalith.Conversations.Server`: its primary NuGet asset was Microsoft.IdentityModel.Tokens 8.19.2 while `Hexalith.EventStore.ServiceDefaults.dll` referenced 8.22.0.
- Root central package authority already declares all seven IdentityModel packages at 8.22.0. The first candidate enabled root-wide transitive pinning, but review proved that it also promoted unrelated dependencies into three packable NuGet manifests, so that candidate was rejected.
- The final remediation adds the two root-centrally-versioned entry packages only to `Hexalith.Conversations.Server`, `Hexalith.Conversations.Server.Tests`, `Hexalith.Conversations.Conformance.Tests`, and `Hexalith.Conversations.IntegrationTests`. The resulting semantic graph diff changes only the seven IdentityModel-family versions from 8.19.2 to 8.22.0.
- The four packable Conversations projects build packages successfully and their extracted control/candidate nuspecs are byte-identical. The retained evidence records the exact pack commands, aggregate exits, package archive hashes, extracted-manifest hashes, and zero-byte diff. No submodule, package-version declaration, production authentication code, authority record, waiver, grant, or PRD acceptance state changed.
- A forced solution restore and serialized Debug and Release solution builds complete with 0 warnings and 0 errors; the reproduced MSB3277 is absent. The retained `.txt` build outputs include the success and warning/error totals and are not excluded by the repository log-file ignore rule.
- Post-build regression verification passed: 684/684 server tests, 8/8 rebuilt AppHost topology tests, two consecutive 1/1 real-process runtime-boundary runs, and 14/14 integration tests. The runtime lane also records 1/1 skipped under an explicit `env -u HEXALITH_RUN_APPHOST_BOUNDARY_TESTS` command. Every retained test run records its exact command, environment, process result, raw XML hash, and executed DLL hash; each enabled runtime pass also binds the EventStore DLL and restored asset graph it launched.
- Broad conformance verification remains failed at 456/472. The 16 retained failures include all three preservation-manifest validation tests plus authority, population-proof, final-record workflow, and planning-inventory checks. This failure is not converted into a waiver and blocks an unqualified overall-validation claim.
- `APPHOST-RUNTIME-BOUNDARY-WORKTREE-002` is deliberately working-tree-only. It supersedes v1 only within the provisional working-tree lane and does not supersede committed OQ-1 evidence, activate a grant, change FR-20/SM-C1, change SM-C2, or lift the implementation hold.

## Review Triage Log

Initial Blind Hunter finding floor: `N = min(floor(sqrt(1105.85) + 1), 10) = 10`; the reviewer reported 13 findings. No additional review layers were configured for this route.

1. **Ignored `.log` evidence — actionable, patched.** The final solution build is retained as `solution-build.txt`, which is not excluded by `*.log`.
2. **Build output lacked summary — actionable, patched.** The retained build output now includes `Build succeeded`, `0 Warning(s)`, and `0 Error(s)`.
3. **Restore was not retained — actionable, patched.** A forced solution restore has a hash-bound `.txt` output and exit result.
4. **One-project package graph — actionable, patched.** The evidence binds the solution-wide candidate graph plus baseline/candidate semantic graphs for all four affected projects.
5. **Repository-wide pinning broadened package metadata — actionable, patched.** The broad property was removed. Narrow references now change only the seven IdentityModel versions, and all four packable nuspecs have a zero-byte diff.
6. **Test commands were not replayable — actionable, patched.** Each validation run records its command, filters, environment, configuration, and process exit code.
7. **Tests predated the retained build — actionable, patched.** Every validation was rerun after the final build, with executed DLL hashes bound alongside raw XML.
8. **Approved preservation-manifest hash mismatch — blocking authority dependency, retained.** A successor manifest requires explicit approval; none was invented. Broad conformance therefore remains failed and OQ-1 remains blocked.
9. **Runtime lane lacked opt-in enforcement — actionable, patched.** xUnit v3 conditional skip now requires `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true`, with a retained disabled-path result.
10. **Returned gateway revision appeared discarded — actionable clarification, patched.** The unused returned value was removed. The computed dirty-tree revision remains internal to the provenance prebuild and is asserted on every launchable gateway binary; the stable store marker is intentionally a separate idempotency identity.
11. **Shared Dapr state-store marker — residual, not safely patched in this compatibility lane.** Isolation requires a separately designed topology and lifecycle change; the current limitation is explicit in v2 evidence.
12. **Expanded invalid-token matrix — residual, outside this narrow compatibility change.** The retained runtime lane proves fail-closed missing-token behavior; a broader authentication matrix was not inferred as part of the version-alignment fix.
13. **Dapr YAML substring check — actionable, patched.** The harness now compares the normalized file to the complete canonical reviewed document.

Initial-review result: 10 findings closed and 3 retained dependencies/residuals (approved successor manifest, shared state-store isolation, and expanded authentication matrix).

## Reviewer-Gate Rerun

The final Blind Hunter rerun reviewed 1,387,098 bytes (`1,354.59 kB`) and therefore retained `N = min(floor(sqrt(1354.59) + 1), 10) = 10`. It reported 14 findings; 12 safe findings are closed below and 2 remain explicit residuals. No additional review layers were configured.

1. **Stale review-size arithmetic — actionable, patched.** The spec and gate now record the rerun snapshot and calculation.
2. **Release build absent — actionable, patched.** A serialized Release solution build is retained with exit 0, 0 warnings, 0 errors, and no MSB3277.
3. **Launched EventStore binary not bound per runtime pass — actionable, patched.** Each pass now retains and binds the launched EventStore DLL and `project.assets.json` hashes.
4. **Opt-out environment only implicit — actionable, patched.** The retained command explicitly clears `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS` with `env -u`.
5. **Activation-response wording overclaimed — actionable, patched.** The evidence now claims only the asserted HTTP 200 activation status, not an exact response body.
6. **Shared Dapr state and stable marker — residual, retained.** The harness remains non-hermetic; isolation needs a separately designed topology/lifecycle change.
7. **Second activation may be idempotent — actionable clarification, patched.** The evidence proves two successful boundary executions, not two fresh cutover transitions.
8. **Invalid-token matrix incomplete — residual, retained.** The narrow lane still proves missing-token HTTP 401 only; no extra authorization coverage is inferred.
9. **Incomplete source gitlink binding — actionable, patched.** EventStore, Commons, Tenants, and Builds gitlinks are all bound.
10. **Concatenated manifests mislabeled as XML documents — actionable, patched.** The four-manifest concatenations now use `.txt` paths.
11. **Pack evidence not fully replayable — actionable, patched.** Exact commands, aggregate process exits, package archive hashes, hash-list files, and extracted-manifest hashes are retained.
12. **Solution-wide baseline overclaim — actionable, patched.** The semantic baseline claim is limited to the four affected projects; the evidence makes no solution-wide baseline-confinement claim.
13. **Shared failure-log cancellation budget — actionable, patched.** Each resource now receives its own bounded 15-second diagnostic window.
14. **Initial triage count inaccurate — actionable, patched.** The first review is correctly recorded as 10 closed and 3 retained, and a chronological correction is appended to the memlog.

All safe findings from both review passes are closed. The specification remains `reviewed-awaiting-commit` because the candidate is uncommitted and an approved, hash-bound successor preservation manifest mapping all seven categories to exact tests is still required.
