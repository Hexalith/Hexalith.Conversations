# V9 Execution Overlay — V8 Invariant Reconciliation

**Reviewed:** 2026-08-02  
**Target:** `_bmad-output/planning-artifacts/architecture.md`  
**Baseline:** `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`  
**Verdict:** **BLOCKED FOR V9 PUBLICATION; IMPLEMENTATION HOLD REMAINS ACTIVE.**

The overlay does not expressly weaken the EventStore authority, fail-closed tenant boundary, preservation denominators, no-UI boundary, SM-C2 gate, completed-history immutability, or platform/runtime ownership split. Its hold is stricter than v8 in requiring the same candidate and authority, an explicit release-owner hold lift, and reassessment after drift. However, the appended state is not mechanically coherent enough to become current v9 authority, and two inherited rules conflict or become ambiguous.

## Findings

### F-1 — Critical — The document still declares v8 as its machine-readable current authority

The overlay declares `conversations-architecture-2026-08-02-v9` at lines 1802-1811, but the document frontmatter still declares:

- `authorityVersion: conversations-architecture-2026-08-01-v8` (line 7);
- `rebaselinedAt: 2026-08-01` (line 6);
- `currentExecutionView: ...epic-6-current-execution-view-v1.md` (line 31);
- no v9 correction source or v2 current execution view.

This gives parsers and people two different answers about the canonical authority. It also contradicts the overlay's statement that the v1 view is immutable provenance and v2 is the current generated projection (lines 1984-1988). By the overlay's own gate (lines 1825-1830), complete v9 authority has therefore not been published and the hold cannot lift.

**Required disposition:** make the top-level authority metadata mechanically identify v9 and its canonical v2 companions while retaining v8/v1 as explicitly historical provenance. Do not treat the appended prose declaration alone as publication completion.

### F-2 — Critical — Appending v9 breaks the still-binding v8 view hash contract while v1 is declared immutable

V8 requires semantic or hash drift between the architecture and `epic-6-current-execution-view-v1.md` to fail validation (lines 251-258). The v9 overlay simultaneously says v1 remains immutable provenance (lines 1984-1988). At review time:

- current `architecture.md` SHA-256: `53d1df9be0102a947eef2a5f9adb2cd7d12c1554b00a60d264080fc565b69e12`;
- v1 view's `source_architecture_sha256`: `ced930531c6b0638dbf8253a0c766a146c66748f2f2ee13f64f4259ef9b667eb`.

The existing v8 conformance test computes the hash of the whole current architecture file, so the immutable v1 view and appended v9 architecture cannot both satisfy it. The overlay's generic statement that planning validators are v9 companions does not define how the v8 historical-prefix proof replaces the whole-file check.

**Required disposition:** the v9 validator must explicitly supersede the whole-file v8 hash assertion with an immutable, marker-bounded v8-prefix proof, while separately binding the complete v9 document and v2 view. Until that validator exists and passes, mechanical publication has failed and the hold remains active.

### F-3 — High — The explicit AppHost inheritance conflicts with the required repository baseline

V9 explicitly preserves `Hexalith.Conversations.AppHost` as a module-owned test harness (lines 1851-1853), inheriting the v8 ownership rule at lines 61-70 and 331-335. The required Hexalith baseline says a domain module **must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project** and places the AppHost in the platform/host repository (baseline lines 116-129 and 215-216).

Non-packable/non-publishable status does not remove the baseline's project-ownership prohibition. V9 therefore cannot truthfully claim both full v8 inheritance and baseline compliance.

**Required disposition:** treat this as a blocking architecture/baseline conflict. Resolve it through an expressly approved technical architecture or baseline amendment; an execution-only overlay is not authorized to choose a winner silently.

### F-4 — High — IR-0 does not fully carry forward v8 outcome-neutral readiness

V8 requires an independent `NOT READY` scenario, publication of the complete actual assessment result unchanged, and a prohibition on instructing or modifying the assessor to return `READY` (lines 303-314). V9 requires an "independent" IR-0 and blocks unless it returns `READY` (lines 1825-1830 and 1958-1965), but does not require preservation/publication of the actual non-`READY` result or expressly prohibit outcome steering. RG-15 gets explicit non-predetermination language; IR-0 does not.

The blanket sentence "Every v8 technical invariant remains binding" helps, but Story 6.6's execution definition is also expressly superseded (line 1887), leaving an avoidable ambiguity over whether these assessment mechanics survived as a global hold invariant.

**Required disposition:** state directly in IR-0 that the assessor is not instructed or modified to reach a verdict, the complete actual result is published unchanged, and `NOT READY`, incomplete, or `BLOCKED` evidence keeps the hold active. Carry a deterministic non-`READY` acceptance scenario into the v9 canonical contract.

### F-5 — High — "Successor implementation" is narrower than the declared global hold

V8 calls the hold global (lines 236-249). V9's operative sentence blocks only "successor implementation" (lines 1823-1825). The Publication Boundary currently blocks product/runtime changes (lines 1976-1982), and the supersession map covers known unfinished Story 6.x work, so no implementation is authorized by this document today. Nevertheless, the narrow operative phrase leaves room for an unmapped, renamed, maintenance-labelled, or newly introduced implementation unit to argue that it is not a "successor."

**Required disposition:** make the hold scope explicit: no initiative product, runtime, test-owned implementation, dependency/submodule promotion, or implementation-like partial work may start or resume outside completed immutable history until every lift condition is met. Planning-only publication work should be the sole named exception.

### F-6 — Medium — The inherited version rule is stale against the required DAPR baseline

The v8 architecture records Dapr client packages at `1.17.7` and directs alignment with sibling pins first (lines 932-938). V9 makes every v8 technical invariant binding and read-only (lines 1832-1835). The required repository baseline is DAPR `1.18+` (baseline line 32).

**Required disposition:** do not allow the v8 catch-all to freeze DAPR 1.17.7. Bind implementation to the repository baseline and central package management, or approve a separate technical amendment documenting a temporary exception. This does not authorize a package change during the planning-only publication.

## Explicitly Preserved, With No Reopening Found

- EventStore remains the only write-side/durable conversation-state authority; derived stores remain non-authoritative.
- Tenant access remains fail-closed across interactive, background, export, tool, and verification paths, with cross-tenant non-disclosure.
- The 20/104/77/52-plus-UX-AC denominators and FR-16-only deferral remain intact.
- UX remains preserved but not activated.
- SM-C2 remains a four-row, identical-envelope `post P95 <= 1.05 x baseline P95` gate with no substitute disposition.
- Epics 1-5 and Stories 6.1, 6.2, and 6.7 remain immutable completed history; partial successor input is not accepted evidence.
- Promotion, projection-proof lifecycle, conformance tiering, measured final records, evidence binding, audit/privacy/idempotency rules, and no product/runtime mutation from planning publication remain binding through the catch-all inheritance clause.

## Hold Conclusion

No implementation is authorized. The minimum lift sequence remains cumulative: complete coherent v9 publication; passing v9 mechanical validation; an outcome-neutral independent IR-0 returning `READY` for the exact same candidate, gitlinks, authority identities, and artifact digests; then an explicit release-owner hold-lift record. Any missing condition, non-`READY`/incomplete/`BLOCKED` result, or later drift restores the hold and requires both validator and IR-0 reruns.
