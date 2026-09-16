# Architecture Validation Report — 2026-09-16

- **Target:** `_bmad-output/planning-artifacts/architecture.md` (2,698 lines; 172,239 bytes; SHA-256 `f6c676868b42e0b1d0209ef3df7b4c4a518b9fdcc5dc875b734a6d378dca3666`)
- **Repository:** `main` at `fd3dd58c3c10b512c79cc425f94d57c4d3400b20`, aligned with `origin/main` before report generation
- **Intent:** Validate — no change to the architecture or authority artifacts
- **Lenses:** deterministic lint · good-spine rubric · reality/currentness · adversarial divergence · authority-chain/data-integrity
- **Full reviews:** [`review-rubric.md`](reviews/validate-2026-09-16/review-rubric.md) · [`review-reality-checked.md`](reviews/validate-2026-09-16/review-reality-checked.md) · [`review-adversarial.md`](reviews/validate-2026-09-16/review-adversarial.md) · [`review-authority-chain.md`](reviews/validate-2026-09-16/review-authority-chain.md)

## Gate verdict

**FAIL — do not use `architecture.md` as the current standalone build authority.** Its immutable V14 technical base is internally coherent: deterministic lint is clean, all V9–V14 overlay byte counts and digests reproduce, the V14 bundle and 38-node/61-edge graph agree, and the direct architecture conformance class passes 18/18. The current execution authority does not converge, however. The architecture resolver ends at V14/`ACTIVE`; committed authorities continue through V21; and the current V21 checker rejects `HEAD` with `V21_DESCENDANT_GITLINK_DRIFT`, restoring the effective Story 7.1 hold to **`ACTIVE`**. Treat the recorded V17/V20/V21 lifts as candidate-scoped historical facts, not current authorization.

The same review found two independent architecture-level blockers: the operational envelope was never dispositioned before scoped hold lifts, despite V13 requiring that before *any* lift, and Epic 16 still lacks the cross-replica tenant-projection mutation contract needed to prevent lost or regressed authorization state.

## Scoreboard

| Lens / check | Verdict | Critical | High | Medium | Low |
| --- | --- | ---: | ---: | ---: | ---: |
| Deterministic spine lint | PASS — 0 findings | 0 | 0 | 0 | 0 |
| Good-spine rubric | FAIL | 2 | 2 | 4 | 2 |
| Reality/currentness | FAIL as current authority | 1 | 3 | 2 | 1 |
| Adversarial divergence | FAIL | 2 | 4 | 1 | 0 |
| Authority-chain/data-integrity | FAIL | 1 | 2 | 1 | 0 |
| **Consolidated critical/high (deduplicated)** | **FAIL** | **3** | **9** | – | – |

The full reviews also contain **8 medium and 3 low reviewer findings before cross-lens deduplication**.

## Critical findings

### C1 — Current authority cannot be resolved deterministically, and V21 is invalid at `HEAD`

V13 requires every new `v<N>-*-authority` sidecar to be published with an appended architecture pointer (`architecture.md:2415-2430`). The last marker remains V14 and names `v14-current-candidate-authority-v1.json` with `hold=ACTIVE` (`architecture.md:2627,2698`), while committed execution authorities continue through V21. The installed V21 record says Story 7.1 is `LIFTED`, but its current checker returns `V21_DESCENDANT_GITLINK_DRIFT` and `effectiveHold: ACTIVE` after later EventStore, Folders, and FrontComposer gitlink changes.

Three plausible readers therefore disagree: the architecture resolver stops at V14/`ACTIVE`; a highest-filename reader selects V21/`LIFTED`; the required current checker returns `ACTIVE`. The safe effective state is **Story 7.1 hold `ACTIVE`** until a successor is bound to current `HEAD` and validates.

**Disposition:** discuss, then update. Append a pointer-bearing architecture overlay and successor authority; define one total resolver for architecture identity, planning candidate, point-in-time checkpoint state, and current scoped hold. Preserve V1–V14 bytes.

*Sources: rubric F1 · reality F1 · adversarial ADV-1 · authority AUTH-01/AUTH-02.*

### C2 — The operational-envelope precondition was bypassed

V13 requires environment topology, module-relevant infrastructure/provider strategy, operational runbooks, and capacity-waiver ownership to be decided, deferred with a named owner, or delegated to a named counterpart artifact **before any hold-lift decision** (`architecture.md:2612-2618`). No such disposition artifact exists. V17 nevertheless lifted `7.1-SCHEMAS`, and V20/V21 lifted full Story 7.1 for their exact candidates.

The lifts are narrow, but the invariant says “any hold-lift decision.” Silent reinterpretation weakens an inherited rule and leaves future runtime work without a production parity contract.

**Disposition:** discuss. Either disposition the operational envelope and define how the late decision cures the scoped lifts, or append an explicit amendment narrowing the precondition to product/runtime or release work. Do not infer an exception from the lifts themselves.

*Sources: rubric F2 · adversarial ADV-6.*

### C3 — Durable tenant projection has no cross-replica atomic mutation contract

V14 places durable tenant projection storage behind `ITenantProjectionStore`, but the interface exposes only `GetAsync` and unconditional `SaveAsync`. The current handler's semaphore is process-local. Two replicas can read sequence `n-1`, apply `n` and `n+1`, then save in reverse order; a compliant last-write-wins provider can regress state or persist `n+1` without incorporating `n`. The architecture does not bind expected-version/CAS behavior, the event identity tuple, equal-sequence mismatch handling, retry limits, or the fail-closed terminal outcome.

This is authorization state: a lost disable transition is not a harmless projection lag.

**Disposition:** discuss, then update before Story 16.1 implementation/review. Bind one owner and one atomic mutation protocol, and expose enough contract surface plus conformance tests to enforce it.

*Source: adversarial ADV-2.*

## High findings

### H1 — Story 7.1 has no safe terminal/integration transition

V21 authorizes full Story 7.1 execution at its publication candidate, but the approved source leaves ordinary integration with accepted `main`, merge-candidate CI evaluation, and retirement of the temporary descendant-path restriction unresolved. The present `V21_DESCENDANT_GITLINK_DRIFT` failure is the concrete result.

**Disposition:** discuss. Publish owner-approved successor authority defining terminal state, protected-branch integration topology, merge-candidate evidence, and the rule that keeps Story 7.2 locked until acceptance.

### H2 — V14's workflow inventory is no longer executable

The architecture says deprecated `bmad-dev-auto` and `bmad-quick-dev` aliases must exist as single-hop forwarding routes. Current BMAD 6.12.0 removed four alias files. The direct `PlanningAuthorityV9ValidationTest` run fails 1/7 on missing `.agents/skills/bmad-dev-auto/SKILL.md`, while the V9 publisher at current `HEAD` also reports `CANDIDATE_SOURCE_DRIFT: pyproject.toml`.

**Disposition:** update. Publish a successor workflow-inventory authority or explicitly retire the alias invariant; add a current-head resolver test so frozen-byte tests cannot mask stale discovery.

### H3 — The test AppHost still conflicts with the effective Hexalith baseline

The architecture retains a non-packable, non-publishable module AppHost as a test fixture. The required Hexalith baseline says a domain module must not ship its own `*.AppHost` and places AppHost ownership in the platform/host repository. The architecture records this only as a working interpretation pending a baseline-owner ruling.

**Disposition:** discuss; do not autofix. Obtain the baseline owner's explicit exemption or relocate/remove the fixture through an additive architecture amendment.

### H4 — Lifecycle/watermark ownership and persistence shape are unspecified

Story 16.2 requires an event-fed lifecycle/watermark fact, but does not choose Tenants state, the conversation index, or a separate Conversations record as its single authority. Key grammar, atomicity with index writes, tombstone/rebuild behavior, and migration are undefined.

**Disposition:** update before affected implementation. Select one owner, versioned key/shape, initialization event, atomicity boundary, and exact mappings for never-used, initialized-empty, erased, corrupt, and unavailable states.

### H5 — Replay-visible time has multiple compliant sources and outcomes

Current code exposes both event-envelope timestamps and payload `CommittedAt`. V14 requires immutable event time but gives no precedence, normalization, conflict rule, or exact typed state/reason for absent or invalid time. Two deterministic implementations can still produce different replay hashes and freshness evidence.

**Disposition:** update. Bind a timestamp-precedence table per event class plus validation, precision/timezone normalization, conflict handling, and safe-state mapping.

### H6 — The load-bearing vocabulary sidecar is still absent

DC-2 delegates redaction/hydration condition mappings and per-surface indistinguishability to `_bmad-output/planning-artifacts/conversations-vocabulary-v1.json`, but the file is absent from the V14 candidate and bundle. The review blocker prevents unsafe completion but leaves affected stories unable to converge.

**Disposition:** defer under the existing blocker, then update. Publish and bundle the promised sidecar without widening the shipped public value set.

### H7 — The canonical spec package still describes V9/`UNBOUND`/global `ACTIVE`

`_bmad-output/specs/spec-Conversations/SPEC.md` says its package is the complete canonical contract, but it still instructs consumers to resolve the final V9 marker, `PC=UNBOUND`, and a global active hold. V14 has a bound planning candidate, and later scoped authorities govern Story 7.1.

**Disposition:** discuss/update upstream. Refresh the spec through `bmad-spec`, preserving stable capability identity and distinguishing immutable architecture from current execution authority.

### H8 — Point-in-time sidecars omit the required currentness qualifier

V13 requires regenerated point-in-time sidecars to carry `statusAsOf`; none of V15–V21 does. Their status-shaped fields are therefore easy to misread as live, which is exactly what happens when V21's publication-time `LIFTED` field is read without its current checker.

**Disposition:** update successor schemas. Separate `recordedEffectAtCandidate` from recomputed `effectiveStateAtEvaluatedHead`; qualify immutable existing records through the successor instead of rewriting them.

### H9 — Exact technology versions in the architecture are stale seed

The latest architecture snapshot names Aspire 13.4.6 and Dapr 1.18.5. Current tracked authority and code use SDK 10.0.401, Aspire 13.5.3, and Dapr 1.18.7. The durable sibling-pin alignment rule remains valid; the numbers are dated evidence, not current guidance.

**Disposition:** update with a dated pointer to V18/current central package catalog, or explicitly classify exact versions as code-owned seed.

## Mechanical and repository evidence

| Check | Result |
| --- | --- |
| `lint_spine.py` via temporary `ARCHITECTURE-SPINE.md` symlink | PASS, 0 findings |
| V9–V14 independent marker byte/digest walk | PASS; all prefix/block pins and terminal-LF grammar match |
| `ArchitecturePlanningAuthorityValidationTest` | PASS, 18/18 |
| `PlanningAuthorityV9ValidationTest` | FAIL, 1/7 — missing `bmad-dev-auto` alias |
| V14, V15, V16, V17, V18, V19, V20 checkers | PASS |
| V9 publisher against current `HEAD` | FAIL — `CANDIDATE_SOURCE_DRIFT: pyproject.toml` |
| V21 current checker | FAIL — `V21_DESCENDANT_GITLINK_DRIFT`; effective hold `ACTIVE` |
| Focused Debug build of conformance project | PASS with 2 `Microsoft.IdentityModel.Tokens` version-conflict warnings |

## Verified strengths

- The append-only V9–V14 chain is byte-intact; no in-place architecture rewrite was detected.
- The V14 bundle, graph, epic authority, architecture block, and sidecar bindings agree at their publication candidate.
- V13's DC-1…DC-11 closures remain materially useful: SM-C2, shared trust vocabulary, record cleanliness, graph composition, public temporal anchor, audit degradation, key grammar, ADR namespace, tier strength, AppHost interpretation, and ceiling retirement are explicitly governed.
- Core technology choices remain available and fit the codebase: .NET 10, Aspire, Dapr, EventStore domain-service seams, `.slnx`, central package management, and the named test stack.
- The current machinery fails closed. No unsafe Story 7.1 lift should be inferred from this validation.

## Recommended update order

1. **Restore one authority resolver:** append the current-head pointer/successor authority and keep Story 7.1 `ACTIVE` until it validates.
2. **Resolve inherited preconditions:** disposition or explicitly narrow the operational envelope rule.
3. **Close Epic 16 runtime contracts:** atomic tenant mutation, lifecycle/watermark ownership, replay-time precedence, and recovery ownership.
4. **Repair planning tooling authority:** replace/retire the removed aliases and add a current-head discovery test.
5. **Refresh companions:** update the canonical spec and point exact package versions to V18/current central pins.

No architecture or authority artifact was changed by this validation. Offer: roll these findings into an append-only `bmad-architecture` Update run.
