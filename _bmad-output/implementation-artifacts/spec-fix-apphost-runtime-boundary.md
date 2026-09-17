---
title: 'Repair the AppHost runtime-boundary evidence lane'
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

**Problem:** The retained AppHost runtime-boundary test cannot start EventStore after its JWT configuration became fail-closed, while a heavily consumed host-wide inotify budget can independently prevent Dapr sidecars from starting. This leaves the runtime evidence lane failed without demonstrating a defect in the production boundary.

**Approach:** Make the harness supply a fresh test-only symmetric JWT configuration through a secret Aspire parameter and use the same key for its signed tokens. Give both test-launched Dapr sidecars a test-owned configuration that disables component hot reload, preserving production topology, authentication validation, and every end-to-end boundary assertion.

</frozen-after-approval>

## Implementation Notes

- Added a fresh 384-bit signing key per runtime-boundary execution and exposed it to EventStore through a secret Aspire parameter; both harness-issued JWT types now use that same key.
- Added a test-owned Dapr configuration with component hot reload disabled and applied it to the EventStore and Conversations sidecars before `BuildAsync`; production AppHost composition is unchanged.
- Preserved the production JWT fail-closed validator, real-process startup, HTTP 200 cutover-status assertion, command, projection, query, and provenance assertions.
- The Dapr options annotation property is init-only at compile time, so the harness follows the integration package's established remove-and-replace annotation pattern while preserving every pre-existing option.
- The first real-process run proved both planned startup fixes but exposed that Aspire's resource state becomes healthy before EventStore accepts HTTP. Added a bounded `/alive` probe before the idempotent cutover request so transport startup is distinguished from a cutover refusal; the HTTP 200 cutover-status assertion remains unchanged.
- Provisional verification before review passed twice in 79.987 seconds and 78.804 seconds; those runs are retained only as implementation chronology and are not the final evidence set.
- Blind Hunter review identified six actionable audit points. The harness now enforces a hard 90-second endpoint-readiness budget, rejects rather than overwrites any future pre-existing Dapr config, asserts both the HotReload profile content and attached sidecar model, and proves an unauthenticated protected request receives 401. Fresh post-review evidence will replace the provisional pre-review result.
- Post-review compilation showed this repository's Shouldly version lacks the string `ShouldContain` overload with a custom message; the profile assertion now uses ordinal `Contains(...).ShouldBeTrue(...)` without weakening the condition.
- Final v1 post-review verification: the test project builds; all eight AppHost topology tests passed in 0.875 seconds; the full runtime-boundary class passed twice in 80.903 seconds and 71.461 seconds. All three raw xUnit XML results and the two exact candidate-file hashes are bound by `apphost-runtime-boundary-working-tree-evidence-v1.json`; because the marker and state store are stable, these are two successful boundary executions rather than proof of two fresh cutover transitions.
- The evidence record is deliberately working-tree-only. It does not supersede OQ1-TECHNICAL-EVIDENCE-001, activate any repository grant, release the implementation hold, change FR-20/SM-C1, or change SM-C2. A committed-source successor and the remaining compatibility evidence are still required.
- The additive working-tree successor `APPHOST-RUNTIME-BOUNDARY-WORKTREE-002` now closes the prior MSB3277 observation by binding narrowly scoped compatibility references for the four affected non-packable consumers, all seven IdentityModel 8.22.0 resolutions, unchanged packable nuspecs, a warning-free solution build, and fresh passing server/topology/runtime/integration results. It also retains the 456/472 conformance failure and leaves the committed evidence, grants, PRD states, and implementation hold unchanged.

## Review Triage Log

1. **Hard readiness deadline — actionable, patched.** The endpoint probe previously inherited only the outer eight-minute token. It now has its own linked 90-second deadline and reports which budget expired.
2. **Existing Dapr configuration — actionable, patched.** The harness now fails if either sidecar already has a configuration instead of overwriting a future production-owned value.
3. **Dapr profile attachment — actionable, patched.** The test reads and validates the exact HotReload-disabled YAML and then asserts that both sidecar models reference that file.
4. **Authentication negative path — actionable, patched.** The runtime lane now proves the protected status endpoint returns HTTP 401 without a bearer token before exercising authenticated operations.
5. **Durable final runs — actionable, patched.** Topology and two post-review runtime runs are retained as three separately hashed XML artifacts; repeatability does not imply two fresh cutover transitions.
6. **Dirty-tree source binding — actionable, patched.** The working-tree evidence record binds the exact runtime test and Dapr profile hashes plus their deterministic bundle digest, while explicitly refusing commit-bound or release-evidence status.
7. **Runtime opt-in and exact profile validation — actionable, patched in the successor review.** The real-infrastructure lane now conditionally skips unless explicitly enabled, and the Dapr profile must match the complete canonical reviewed document rather than a substring.

All six findings are closed in the candidate. The final reviewer-gate rerun passed with no blocking remediation finding. The spec is `reviewed-awaiting-commit` because repository instructions do not authorize Codex to commit. The later IdentityModel remediation closes MSB3277 in the working tree only; broad conformance and committed-source successor evidence still block an unqualified OQ-1 claim.
