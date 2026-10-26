### Independent review iteration 6 — workflow limit reached

The fifth re-derivation passed its focused verification, but the next independent
review found remaining protocol omissions. The implementation is **not accepted
or complete**. Every finding below was graded before grouping; none matches an
unchanged prior finding closely enough to carry its previous disposition.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| R6B1 | high | bad_spec | `native_binary()` requires the source-stamped assembly and binds its bytes, but `verify_inserted()` permits only native TRX, receipts and the final pair in the publication commit. The new runtime publication test leaves `artifacts/native/evidence.dll` untracked. Including it violates the exact publication inventory; a fresh checkout cannot recover it from that publication. Define and derive the retained binary inventory. |
| R6B2 | medium | bad_spec | Native admission accepts only absolute codeBase paths beneath the current repository prefix. An unchanged accepted TRX from another checkout therefore fails PATH_INVALID after relocation, even with the matching binary at its relative location. Publication must define portable capture or explicitly bound relocation without weakening containment or modifying retained native bytes silently. |
| R6B3 | high | patch (pending; loopback limit) | Parent changed the three-future fault's prior horizon to 999 and refreshed restoration hashes; schema and semantics still accept observed horizon 14. The relation uses max(deliveries) instead of retaining the prior horizon. |
| R6B4 | high | bad_spec | `duplicateFutureState` and `conflictingFutureState` are only outcome strings. The merge schedule contains unique authoritative pending fingerprints but no identical repeated delivery or conflicting payload and persisted outcome; AC-16.1-03 explicitly requires both behaviors. Extend the measured schedules instead of treating labels as proof. |
| R6B5 | high | bad_spec | Parent relabelled the measured tenant and both Gapped native snapshots while leaving every Healthy restart observation unchanged; schema and semantics pass. Healthy snapshot/consumer shapes have no tenant or storage identity, so they cannot establish the same tenant's persisted authorized/denied decisions. |
| R6B6 | false | reject | The smaller creation-plus-participant history passes, but AC-16.2-02 requires deterministic canonical state for identical authoritative inputs, not a fixed minimum set of event types. The oracle derives complete domain values for that legitimate history and still requires multiple stable collection keys. Dropping events does not itself demonstrate incorrect replay; imposing the reviewer's fixed history would add an unstated requirement. |
| R6B7 | medium | patch (pending; loopback limit) | Parent reduced readiness to one attempt and one millisecond, including its diagnostic; both validators pass despite the independent three-attempt/1,000-ms controls. Require a reached retry/timeout terminal condition, not only upper bounds. |
| R6B8 | high | bad_spec | The only measured preflight diagnostic is captured during the Dapr collision. Unrelated occupancy exists in the before/after ledger, but `unrelatedCode: null` has no independently measured unrelated-only preflight result. AC-16.3-02 explicitly requires that negative control. |
| R6B9 | high | bad_spec | Parent retained recovered checkpoint three while both watermarks were zero; checks still claim newer-progress fences. There is no competing worker, lease/version expectation or persisted global-position/generation schedule proving protection of newer progress. Add that evidence; do not compare aggregate sequence numerically with EventStore global positions, which are different domains. |
| R6B10 | medium | patch (pending; loopback limit) | Actual opaque-ID constructors use .NET ThrowIfNullOrWhiteSpace. The native predicate accepts U+001C–U+001F, but Python strip in domain.text rejects them; the parent reproduced all four. Correct the existing predicate with cross-runtime boundary coverage. |
| R6E1 | medium | patch (pending; loopback limit) | Independently verified same opaque-ID whitespace rejection as R6B10 at domain.text/identifier. Native-valid serialized identifiers fail the current oracle. |
| R6V1 | medium | patch (pending; loopback limit) | Pre-verified regression gap: trusted-entry tests cover 16.1, qualified runtime/publication covers 16.2, and 16.3 cases either bypass entry in fixture scope or stop at missing entry. No test reaches its additional appHostOwnership requirement. Cover missing, unqualified and PASS ownership through the existing disposable trusted-entry mechanism. |

After individual grading, only R6B10/R6E1 share a root cause. Other roots remain
distinct. R6B1/R6B2/R6B4/R6B5/R6B8/R6B9 require non-frozen specification and protocol
corrections; the patch entries remain outstanding and are not deferred. The
counter advances from 5 to 6. The effective bmad-build step-04 instruction says:
**"If it exceeds 5, HALT and escalate to the human."** Consequently no sixth
re-derivation, code reversion, patch implementation or completion transition was
performed. The entire current implementation and its verification evidence remain
available for review. Continuing requires an explicit override of that workflow
limit, not another authorization of the already-approved prerequisite scope.

Review input: `/tmp/epic16-review6-zbl1gf6i/complete.diff` (9,799,008 bytes).
Parent counterexamples and this disposition are retained under
`module/validation/epic-16-prerequisite-implementation/review-iteration-6/`.
Tasks 2–5 are reopened for the unresolved protocol/correctness/coverage work;
task 1's actual CLI qualification and task 6's honest handoff remain recorded.
Story 16.1 remains draft/backlog, runtime hold ACTIVE, and accepted pins unchanged.

