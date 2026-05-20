---
stepsCompleted:
  - step-01-load-context
  - step-02-define-thresholds
  - step-03-gather-evidence
  - step-04-evaluate-and-score
  - step-04e-aggregate-nfr
  - step-05-generate-report
  - step-e-refresh-state
lastStep: 'step-e-refresh-state'
lastSaved: '2026-05-19'
refreshedAt: '2026-05-19'
refreshNotes: 'Working-tree confirmation of Story 1.6 P22/P24 patches and incorporation of Story 1.11 spec hardening (commit 492055a). Test baseline re-run: 268/268 green.'
executionMode: 'sequential'
scope: 'Epic 1 foundation — tenant isolation (NFR16-21), reliability/idempotency (NFR22-27), data integrity & projection freshness (NFR38-48)'
releaseGate: 'MVP / Epic 1 done'
inputDocuments:
  - '_bmad/tea/config.yaml'
  - '_bmad-output/project-context.md'
  - '_bmad-output/planning-artifacts/prd.md (section: Non-Functional Requirements, lines 1321-1434)'
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
  - '_bmad-output/implementation-artifacts/1-1-set-up-initial-project-from-starter-template.md'
  - '_bmad-output/implementation-artifacts/1-2-define-conversation-identity-command-event-and-error-contracts.md'
  - '_bmad-output/implementation-artifacts/1-3-create-tenant-safe-conversation-aggregate.md'
  - '_bmad-output/implementation-artifacts/1-4-add-conversation-participants-with-stable-party-attribution.md'
  - '_bmad-output/implementation-artifacts/1-5-enforce-tenant-access-and-typed-fail-closed-rejections.md'
  - '_bmad-output/implementation-artifacts/1-6-add-idempotent-command-handling.md'
  - '_bmad-output/implementation-artifacts/1-6-idempotency-local-evidence.md'
  - '_bmad-output/implementation-artifacts/1-11-prove-replay-schema-versioning-and-projection-rebuild-behavior.md (commit 492055a hardening pulled in at refresh)'
  - '_bmad-output/test-artifacts/framework-setup-progress.md'
  - '_bmad-output/test-artifacts/automation-summary.md'
  - '.claude/skills/bmad-tea/resources/knowledge/adr-quality-readiness-checklist.md'
  - '.claude/skills/bmad-tea/resources/knowledge/test-quality.md'
  - '.claude/skills/bmad-tea/resources/knowledge/error-handling.md'
  - '.claude/skills/bmad-tea/resources/knowledge/ci-burn-in.md'
---

# Hexalith.Conversations — NFR Assessment

**Scope:** Epic 1 foundation NFRs against MVP / Epic 1-done release gate.
**Assessor:** Murat (Master Test Architect).
**Date:** 2026-05-19.

---

## Step 1 — Context, Scope, and Evidence Inventory

### 1.1 Scope of This Assessment

Epic 1 establishes the event-sourced foundation: tenant-safe conversation aggregate, participants, fail-closed tenant access, idempotent commands, projection rebuild, schema versioning, and published versioned domain events. The release gate is "Epic 1 done — foundation is shippable as the MVP substrate before Epic 2 governance lands."

In-scope NFRs (Epic-1 substantive):

| Cluster | NFRs in scope | Lifecycle stage |
|---|---|---|
| Tenant isolation & non-disclosure | NFR16, NFR17, NFR18, NFR19, NFR20*, NFR21* | Design review + automated tests |
| Reliability, idempotency & at-least-once | NFR22, NFR23, NFR24, NFR25, NFR26, NFR27 | Design review + automated tests (drills deferred) |
| Data integrity / event sourcing | NFR38, NFR39, NFR40, NFR41, NFR42, NFR43 | Automated tests + ADR |
| Projection freshness | NFR44, NFR45, NFR46, NFR47, NFR48 | Design review + automated tests |
| Measurement discipline (governing) | NFR1, NFR2, NFR3, NFR7, NFR8 | Design review |

`*` NFR20–21 (audit-paired governance, redaction non-leakage) are surfaced by Epic 2 stories; Epic 1 only needs to leave the contract surface and event flow ready, not enforce them. Carried as advisory.

Explicitly **out of scope for this run** (deferred to later assessments):

- Performance (NFR9-15) — no benchmark harness yet; would need k6/NBomber and a sized dataset.
- Security beyond tenant isolation (NFR62 release blockers other than tenant isolation, NFR65 audit-record access).
- Scalability/capacity (NFR30-37) — requires sized data + projection rebuild benchmarks.
- Integration/portability (NFR49-54) — requires Epic 4 contract package.
- Operability/observability (NFR55-61) — Epic 6.
- Compliance/release evidence (NFR62-68) — Epic 5.
- Accessibility (NFR69-77) — Epic 3 UI.

### 1.2 Implementation State (authoritative: `sprint-status.yaml`, 2026-05-19T18:30)

| Story | Title | Status | NFR relevance |
|---|---|---|---|
| 1.1 | Set up initial project from starter template | **done** | Scaffold; no NFR claim |
| 1.2 | Define conversation identity, command, event, and error contracts | **done** | NFR40 (schema/version on events), NFR41 (additive evolution example), NFR42 (temporal anchors), NFR19 (typed errors) |
| 1.3 | Create tenant-safe conversation aggregate | **done** | NFR16, NFR17 (positive cases) |
| 1.4 | Add conversation participants with stable Party attribution | **done** | NFR16, NFR42 (stable Party IDs) |
| 1.5 | Enforce tenant access and typed fail-closed rejections | **done** | NFR16, NFR17 (adversarial), NFR18, NFR19, NFR27 |
| 1.6 | Add idempotent command handling | **in-progress** (P22/P24 hardening landed in working tree on 2026-05-19; `ConversationAuditHandle`, `ConversationCommandSchemaValidation`, and `IdempotencyKeyMissing` typed code present; close-out blocked only by DEF1 handler wiring deferred to later stories) | NFR22 (core), NFR23 (partial), NFR40 (`idempotency_outcome_unknown` + `idempotency_key_missing` typed errors), NFR19 (server-derived audit handle) |
| 1.7 | Project conversation read models with freshness metadata | **ready-for-dev** | NFR44, NFR45, NFR46, NFR47, NFR48 (NOT YET BUILT) |
| 1.8 | Retrieve and list conversations by tenant business context | **ready-for-dev** | NFR18 read-side (NOT YET BUILT) |
| 1.9 | Resolve Parties and upstream references at read time | **ready-for-dev** | NFR19 hydration non-leakage (NOT YET BUILT) |
| 1.10 | Publish versioned conversation domain events | **ready-for-dev** | NFR23, NFR24, NFR40 publish-side (NOT YET BUILT) |
| 1.11 | Prove replay, schema versioning, and projection rebuild | **ready-for-dev** (advanced-elicitation hardening landed 2026-05-19 in commit 492055a — spec now requires explicit validation precedence, idempotency-records-not-replay-authority, side-channel equivalence, poison-quarantine bounded scope, fixed-fixture identifier discipline) | NFR38, NFR39, NFR41, NFR43 (NOT YET BUILT) |

**Net:** Epic 1 is **5 of 11 stories done, 1 in-progress, 5 not started**. Many of the in-scope NFRs above land in stories that haven't shipped code yet. The assessment will distinguish:

- ✅ **Covered with evidence** — story done, tests exist.
- ⚠️ **Design intent / partial** — story in progress or design committed but code missing.
- ⬜ **Not yet implemented** — story still in backlog; NFR cannot be graded.

### 1.3 Test Evidence Inventory

Backend-only .NET stack (xUnit v3, Shouldly, NSubstitute, Testcontainers per `framework-setup-progress.md`). Test projects:

- `tests/Hexalith.Conversations.Contracts.Tests` — contract metadata, identifier validation, forbidden public surface, serialization, sample contracts, participant contract, validation
- `tests/Hexalith.Conversations.Tests` — domain boundary, validation (create + command schema), aggregates (create + participant), state safety, idempotency fingerprint, idempotency store
- `tests/Hexalith.Conversations.Server.Tests` — tenant access guard / service / registration / handler integration, add-participant handler, projections accumulator, EventStore idempotency bridge, idempotent command executor
- `tests/Hexalith.Conversations.Client.Tests` — client boundary
- `tests/Hexalith.Conversations.IntegrationTests` — scaffold smoke, repository test context

No browser tests, no Playwright, no Pact yet, no performance harness, no chaos drills. **Evidence is exclusively unit + integration at the .NET layer.**

### 1.4 Knowledge Fragments Loaded

Core tier (always-load): `adr-quality-readiness-checklist`, `ci-burn-in`, `test-quality`, `error-handling`. Adapted from UI/Playwright-centric examples to backend .NET context. The 8-category × 29-criteria ADR checklist will be the structural backbone of the assessment, narrowed to Epic 1 NFR clusters.

Skipped (not applicable to backend-only stack at this stage): `playwright-config`, `playwright-cli`.

### 1.5 Open Inputs Pulled From PRD Governance NFRs

NFR1-NFR8 govern *how* each NFR is verified. They define the **measurement envelope** for any numeric assertion (NFR7), require pass/fail/waived/unknown-accepted classification (NFR2), and require classification of numeric targets into Release blocker / Validation target / Capacity discovery target before kickoff (NFR5). These act as the rubric for Step 2 thresholds — every in-scope NFR with a target needs (a) method, (b) environment class, (c) interpretation.

### 1.6 Confirmation Of Inputs

- PRD NFR section: ✅ loaded (77 NFRs, lines 1321–1434)
- Architecture: ✅ available, will pull specific sections during step 3 evidence pass (not loaded fully — avoid context bloat)
- Epic 1 implementation summaries: ✅ enumerated; story 1.1–1.6 + 1.11 inspected for headings
- Story files for 1.7-1.10: ✅ available; not yet read in detail since code is not landed
- Test inventory: ✅ enumerated (33 first-party `.cs` test files)
- Test framework: ✅ confirmed (.NET 10 / xUnit v3 / Shouldly / NSubstitute / Testcontainers)
- Performance/chaos/Pact harnesses: ❌ none yet — flagged for Step 2 threshold decisions
- Browser automation: not applicable (`tea_browser_automation: auto` resolves to N/A here)

**Step 1 complete.** Proceeding to Step 2 — define measurable thresholds for each in-scope NFR cluster.

---

## Step 2 — NFR Categories & Thresholds

Mapped to the ADR Quality Readiness Checklist's 8 categories. Only categories materially in scope for **Epic 1 + MVP gate** are expanded; out-of-scope categories are listed with their owning epic for traceability. Every threshold cites its PRD source and names verification artifact type per NFR1. UNKNOWN entries are surfaced as concerns for resolution.

### 2.1 Governing Measurement Rubric (NFR1, NFR2, NFR3, NFR7, NFR8)

Cross-cutting rules applied to every threshold below:

| Rule | Source | Application in this assessment |
|---|---|---|
| Every NFR identifies verification artifact + lifecycle stage | NFR1 | Each row below names artifact type (`xUnit`, `integration test`, `ADR`, `design review`, `operational drill`) |
| Every release-gated NFR has automated artifact + evidence file + status | NFR2 | Status uses `pass` / `fail` / `waived` / `unknown-accepted` per PRD vocabulary |
| Numeric targets need method + environment class + interpretation | NFR3 | Epic 1 in-scope NFRs are largely qualitative — no perf numerics graded here |
| Measurement envelope | NFR7 | N/A for qualitative gates; carried forward to Epic 5 / capacity work |
| Conformance evidence shape | NFR8 | Will be addressed by Story 5.6 (signed release artifact); flagged as MVP-gap |

### 2.2 Category 1 — Testability & Automation

| # | Criterion | Threshold | Source | Verification artifact | Status target |
|---|---|---|---|---|---|
| 1.1 | Aggregate testable without EventStore runtime, Dapr, or HTTP | Pure command/state/event tests; no infra in unit lane | project-context.md "Do not mock inside aggregate logic" | `xUnit` unit (aggregates) | pass |
| 1.2 | Tenant access service testable without live Tenants projection | Service exercised against in-memory tenant projection fake | Story 1.5 "Existing Tenants and Parties Patterns to Reuse" | `xUnit` server tests | pass |
| 1.3 | Idempotency layer testable without real EventStore command status | `InMemoryConversationIdempotencyStore` + `EventStoreCommandStatusIdempotencyBridge` | 1-6-idempotency-local-evidence.md | `xUnit` server tests | pass |
| 1.4 | Deterministic data factories for command/event/state | `Hexalith.Conversations.Testing/Factories` exists, used by other tests | framework-setup-progress.md Step 3 | `xUnit` factories | pass |
| 1.5 | Sample valid + invalid command JSON for adopter consumption | Contract sample tests in `ContractSamples.cs` + `ContractValidationTest.cs` | Story 1.2 contracts | `xUnit` contract serialization | pass |

### 2.3 Category 2 — Test Data Strategy

| # | Criterion | Threshold | Source | Verification artifact | Status target |
|---|---|---|---|---|---|
| 2.1 | Multi-tenant test segregation | Every test that touches tenant scope binds an explicit `TenantId`; no cross-tenant leakage in fixtures | NFR16, NFR28 | `xUnit` server tests | pass |
| 2.2 | Synthetic data only, no real Party PII | Tests use generated GUIDs / synthetic identifiers — no upstream personal data | NFR58, project-context.md privacy rules | `xUnit` everywhere | pass |
| 2.3 | Parallel-safe test isolation | xUnit v3 default class-level isolation; no shared mutable state between tests | test-quality.md DoD | xUnit run with default parallelism | pass |
| 2.4 | Cleanup / teardown discipline | In-memory fakes drop scope after test; no Testcontainers cleanup gaps when DB integration arrives | Story 1.7 + Story 1.8 (not yet built) | `xUnit` + Testcontainers | **UNKNOWN** until 1.7/1.8 land |

### 2.4 Category 3 — Scalability & Availability (Reliability / Idempotency / At-Least-Once)

This category absorbs Epic 1's reliability NFRs (NFR22-NFR27).

| NFR | Criterion | Threshold | Source | Verification artifact | Status target |
|---|---|---|---|---|---|
| NFR22 | Duplicate / reordered / retried commands produce no divergent projections | Same logical input ⇒ same outcome metadata + no duplicate events | PRD line 1357 | `xUnit` idempotency tests + projection accumulator tests | pass |
| NFR23 | Pub/sub: at-least-once delivery tolerance | Test induced duplicates, reordering, replay; subscriber idempotent; dedup-window expiry exercised | PRD line 1358 | Integration test (when Story 1.10 lands) | **partial** — `EventStoreCommandStatusIdempotencyBridge` proves command-side; subscriber-side awaits 1.10 |
| NFR24 | Publication failure: retry / dead-letter / replay defined | Architecture decision recorded; behavior tested | PRD line 1359 | ADR + integration test | **UNKNOWN** — Story 1.10 not landed |
| NFR25 | Operational drills for sidecar restart, EventStore degradation, projection-rebuild crash, audit-sink degradation, redaction propagation | Drills run pre-GA | PRD line 1360 | Operational drill artifact | **UNKNOWN-accepted for MVP** (deferred per story 5.x ownership) |
| NFR26 | Failure-mode matrix documented | DAPR, EventStore, projections, pub/sub, tenant projection, audit sink × failure mode × retry × dead-letter × operator signal × recovery | PRD line 1361 | ADR / design doc | **UNKNOWN** — no matrix found in Epic 1 artifacts |
| NFR27 | Verification tooling distinguishes product invariant failures from infrastructure failures | Test naming + typed result categories | PRD line 1362, Story 1.5 typed errors | `xUnit` server tests | pass (for what exists) |
| NFR28 | RPO/RTO for event store / projection store / audit | Numeric targets defined | PRD line 1363 | ADR + drill | **UNKNOWN** — owned by Epic 5/6 |
| NFR29 | Backup restore + tenant-scoped recovery tested | Drill executed | PRD line 1364 | Operational drill artifact | **UNKNOWN-accepted for MVP** |

### 2.5 Category 5 — Security (Tenant Isolation Cluster)

The release-blocker zone for Epic 1.

| NFR | Criterion | Threshold | Source | Verification artifact | Status target |
|---|---|---|---|---|---|
| NFR16 | Tenant isolation failures are release blockers; missing/stale/ambiguous/mismatched/unknown tenant fails closed before aggregate or projection access | Every command and read path runs through `ConversationTenantAccessGuard` *before* aggregate load | PRD line 1348, Story 1.5 | `xUnit` server tests for guard + handler integration | pass (for command-side; read-side awaits Story 1.7/1.8) |
| NFR17 | Adversarial coverage: cross-tenant ID guessing, replayed cross-tenant commands, poisoned projection events, malformed metadata, mixed-tenant rebuild | Every adversarial vector has an explicit named test | PRD line 1349 | `xUnit` adversarial test suite | **partial** — guard tests cover ID guessing + denial; replay + poisoned events + rebuild adversarial cases await Stories 1.10 / 1.11 |
| NFR18 | Cross-tenant reads / writes / replay / rebuild / search / diagnostics / audit / admin fail closed with content-safe responses | Typed denials, no existence disclosure | PRD line 1350, Story 1.5 typed-error guidance | `xUnit` + integration | pass for command-side; read/rebuild/diagnostics awaits 1.7-1.11 |
| NFR19 | Error messages, logs, metrics, traces, diagnostics, conformance output must not leak tenant IDs / Party IDs / conversation existence / redacted content / provider payloads / cross-tenant business references | Tests assert error vocabulary and absence of leakable fields | PRD line 1351, Story 1.6 privacy/non-disclosure evidence | `xUnit` error vocabulary tests | pass for what's wired (Story 1.5 + 1.6) |
| NFR20 | Governance mutations fail closed when audit unavailable; no queued unaudited governance writes | Audit pairing contract enforced | PRD line 1352 | Epic 2 territory | **carry-forward** — not Epic 1 |
| NFR21 | Redacted content does not reappear in projections / search / audit views / caches / exports / temporal views / replay/rebuild output / logs / traces / errors / observability | All materialization surfaces honor redaction | PRD line 1353 | Epic 2 + 5 territory | **carry-forward** — not Epic 1 |

### 2.6 Category 8 — Deployability (Schema Evolution & Data Integrity)

Epic 1's data-integrity NFRs map here because they constrain what evolution is safe.

| NFR | Criterion | Threshold | Source | Verification artifact | Status target |
|---|---|---|---|---|---|
| NFR38 | v1 projections rebuildable from persisted event stream; same event history ⇒ functionally equivalent read model | Rebuild test reproduces projection state byte-equivalent (excluding non-deterministic metadata) | PRD line 1379 | Integration test (Story 1.11) | **UNKNOWN** — Story 1.11 not landed |
| NFR39 | Deterministic rebuild from same ordered event stream, excluding documented non-deterministic runtime metadata | Same input order ⇒ same output, allowed deltas explicitly listed | PRD line 1380 | Integration test (Story 1.11) | **UNKNOWN** — Story 1.11 not landed |
| NFR40 | Persisted + published events carry schema/version metadata; unsupported versions fail with typed documented errors | Every event has `schemaVersion`, every command validates supported range | PRD line 1381, Story 1.2 + 1.6 (`idempotency_outcome_unknown`) | `xUnit` contract + serialization tests | pass (for command-side); publish-side awaits 1.10 |
| NFR41 | Event schema evolution: one worked additive example | Worked example committed and tested | PRD line 1382 | Story 1.11 deliverable | **UNKNOWN** — Story 1.11 not landed |
| NFR42 | Temporal evidence anchors named (event position / projection version / timestamp / composite) | ADR or contract field clearly identifies authoritative anchor | PRD line 1383 | ADR + contract test | **UNKNOWN** — anchor decision not surfaced in Epic 1 artifacts |
| NFR43 | Temporal reconstruction deterministic enough that links resolve to same legally meaningful state | Determinism test asserts repeat-resolution stability | PRD line 1384 | Integration test (Story 1.11) | **UNKNOWN** — Story 1.11 not landed |

### 2.7 Category 6 — Monitorability / Debuggability (Projection Freshness Surface)

Epic 1's projection-freshness NFRs partially live here; full operability is Epic 6.

| NFR | Criterion | Threshold | Source | Verification artifact | Status target |
|---|---|---|---|---|---|
| NFR44 | Projection freshness metadata consistent across consumer APIs / operator views / diagnostics / verification output | Same shape exposed everywhere | PRD line 1389 | Contract test + integration (Story 1.7) | **UNKNOWN** — Story 1.7 not landed |
| NFR45 | Standard freshness shape: `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, `lagDuration` (or documented equivalent) | Shape committed to public contract | PRD line 1390, Story 1.7 | Contract test | **UNKNOWN** — Story 1.7 not landed |
| NFR46 | Freshness semantics: `current` / `stale` / `rebuilding` / `unavailable` / `intentionally hidden by tenant isolation` | Vocabulary committed; exhaustive in tests | PRD line 1391 | Contract test | **UNKNOWN** — Story 1.7 not landed |
| NFR47 | Operator surfaces distinguish normal / delayed / degraded / blocked / redacted / replaying / partially rebuilt with tenant scope + timestamp + next action | UI / API state model | PRD line 1392 | Epic 3 + 6 | **carry-forward** — not Epic 1 |
| NFR48 | During lag / rebuild / replay / retry / dead-letter / audit-sink degradation: last-known-good + processing status + completeness signal + operator-action signal | Stable trust signals | PRD line 1393 | Epic 6 | **carry-forward** — not Epic 1 |

### 2.8 Out-of-Scope Categories (For Completeness)

| Category | NFR clusters | Owning Epic | Status here |
|---|---|---|---|
| Category 4 — Disaster Recovery | NFR28, NFR29 | Epic 5/6 | Out of scope, surface as MVP risk |
| Category 7 — QoS / QoE | NFR9-15 (perf), NFR47-48, NFR75-77 | Epic 3 + Epic 6 + perf workstream | Out of scope, no benchmark harness yet |

### 2.9 NFR Threshold Matrix Summary

| Cluster | NFRs | `pass` target reachable now | `UNKNOWN` until later story | `carry-forward` |
|---|---|---|---|---|
| Testability | 1.1–1.5 | 5 | 1 (2.4) | 0 |
| Reliability / Idempotency | NFR22–27 | 2 (NFR22, NFR27 partial) | 5 (NFR23-26, NFR28-29) | 0 |
| Security / Tenant isolation | NFR16–19 | 3 (command-side) | 1 (NFR17 adversarial breadth) | NFR20, NFR21 |
| Data integrity / schema evolution | NFR38–43 | 1 (NFR40 partial) | 5 (NFR38, NFR39, NFR41, NFR42, NFR43) | 0 |
| Projection freshness | NFR44–48 | 0 | 3 (NFR44, NFR45, NFR46) | NFR47, NFR48 |

**Read:** Roughly **11 of ~24 in-scope NFRs** are gradeable today (pass or partial-pass). **~10 are UNKNOWN** pending stories 1.7–1.11 / 1.6 close-out. **~3 are carry-forward** to Epic 2/3/6. This profile will drive a **CONCERNS** gate at minimum unless we narrow the gate to "what's actually done in 1.1–1.5 + 1.6 idempotency proof."

**Step 2 complete.** Proceeding to Step 3 — gather evidence against each threshold from the actual implementation and tests.

---

## Step 3 — Evidence Collection

Per Jerome's direction, scope is narrowed to **stories 1.1–1.5 done + 1.6 in-progress (local idempotency proof landed)**. UNKNOWN rows tied to stories 1.7–1.11 are carried forward as deferred — not graded as CONCERNS, since the implementing stories are simply not yet started.

### 3.1 Green-State Baseline

`dotnet test Hexalith.Conversations.slnx --no-restore` executed on 2026-05-19:

| Test project | Pass | Fail | Skip | Total | Duration |
|---|---|---|---|---|---|
| Hexalith.Conversations.Contracts.Tests | 77 | 0 | 0 | 77 | 383 ms |
| Hexalith.Conversations.Tests | 75 | 0 | 0 | 75 | 83 ms |
| Hexalith.Conversations.Server.Tests | 107 | 0 | 0 | 107 | 90 ms |
| Hexalith.Conversations.IntegrationTests | 8 | 0 | 0 | 8 | 74 ms |
| Hexalith.Conversations.Client.Tests | 1 | 0 | 0 | 1 | 29 ms |
| **Total** | **268** | **0** | **0** | **268** | **~660 ms** |

**Refresh re-run (2026-05-19, after Story 1.6 P22/P24 patches landed in working tree):** 268 / 0 / 0 / 268 / ~624 ms. Counts match the original baseline because the P22/P24 tests (`DuplicateReplayPayloadShouldExcludeCallerSuppliedCorrelationAndScope`, `MissingIdempotencyKeyShouldRejectBeforeTenantAccessAndIdempotencyLookup`, the 7-case `ConversationCommandSchemaValidationTest` theory, and the added `SameIdempotencyKeyUnderDifferentTenantShouldNotReplayStoredOutcome` / `SameKeyUnderDifferentCommandTypeShouldNotCollide` cases) were already counted in the initial baseline; the refresh confirms they remain green.

Quality signal: total suite under 1 second; well inside the test-quality.md DoD execution-time envelope. No skipped tests, no flakies in observed run. (Burn-in not exercised — a single clean run is the baseline; a follow-up burn-in N=10 would harden this evidence per `ci-burn-in.md`.)

### 3.2 Evidence Per NFR Cluster

#### Tenant Isolation (NFR16, NFR17, NFR18, NFR19)

**NFR16 — fail closed before aggregate or projection access**

| Evidence | What it proves |
|---|---|
| `ConversationTenantAccessGuardTest.RunAsyncShouldNotInvokeWriteDelegatesWhenAccessIsDenied` | On `Denied` decision, spy counters for aggregate-load / command-dispatch / event-append / projection-mutation / publication-metadata all = **0**. Order-of-operations is enforced — denial precedes protected work. |
| `RunAsyncShouldNotInvokeReadDelegatesWhenAccessIsDenied` | Same shape for the read path: projection-lookup / totals / pagination / Party-hydration / provider-metadata / existence-branch = **0** on denial. |
| `ConversationTenantAccessServiceTest.ShouldFailClosedBeforeStoreLookupForUnsafeInputs` | Missing/malformed inputs fail without even reading the tenant projection store — `store.GetCount = 0`. |
| `ConversationTenantAccessServiceTest.ShouldDenyTenantMismatchesBeforeStoreLookup` | Route/command tenant mismatch denied without store lookup. |

Verdict: **pass** for command-side and the read-side wrapper. Read consumers themselves (Stories 1.7–1.8) not landed.

**NFR17 — adversarial coverage (positive + negative)**

| Evidence | What it proves |
|---|---|
| `ConversationTenantAccessServiceTest.ShouldMapTenantRolesConservatively` `[Theory]` with 9 inline cases | Role × Requirement matrix: Reader can Read but not Write/Admin; Contributor Read+Write but not Admin; Owner all three. Denial reason = `InsufficientRole` when denied. |
| `ShouldFailClosedBeforeStoreLookupForUnsafeInputs` `[MemberData]` | Multiple missing/malformed input combinations exercised. |
| `EventStoreCommandStatusIdempotencyBridgeTest.BridgeNeverInventsConversationsOutcome` | Iterates **all** `CommandStatus` enum values; every status produces `RetryableUncertainty`, none invents a terminal Conversations outcome. Defends against new EventStore status values introducing unintended decisions. |
| Adversarial gap: cross-tenant ID guessing on read endpoints, replayed cross-tenant commands at the wire boundary, poisoned projection events, mixed-tenant rebuild | Read endpoints (1.7/1.8), publish-side (1.10), rebuild (1.11) not landed |

Verdict: **partial** — write-path adversarial proof is strong; read-path and replay/rebuild adversarial cases pending.

**NFR18 — content-safe denials**

| Evidence | What it proves |
|---|---|
| `ConversationTenantAccessGuardTest.RunAsyncShouldNotInvokeReadDelegatesWhenAccessIsDenied` | Read denial uses `ErrorCode = TenantIsolationViolation` (typed error); no existence disclosure. |
| `IdempotentConversationCommandExecutorTest.ConflictShouldRejectWithoutMutation` | Conflict returns `IdempotencyConflict` typed rejection with code `idempotency_conflict`; mutation count = 0. |

Verdict: **pass** for the command path. Search/admin/diagnostics paths are later epics.

**NFR19 — non-leakage of secrets/scope/payload**

| Evidence | What it proves |
|---|---|
| `ConversationStateSafetyTest.CreateEventAndStateMembersShouldNotExposeForbiddenPayloadTerms` | **Reflection-based** assertion that conversation events and state members do not include any of: `DisplayName`, `Email`, `Phone`, `PersonDetails`, `OrganizationDetails`, `Prompt`, `ResponsePayload`, `FileContent`, `FileMetadata`, `AccessToken`, `Claim`, `Authorization`, `Stream`, `Envelope`, `Snapshot`, `Sequence`, `ExpectedRevision`. Structural privacy enforced at the type system level — extends NFR19 from "logs/errors" to "events themselves." |
| `IdempotentConversationCommandExecutorTest.DuplicateReplayPayloadShouldExcludeCallerSuppliedCorrelationAndScope` | Replay payload contains `auditHandle` and explicitly does NOT contain `correlationId`, caller correlation secret, idempotency key, or tenant value. Backed by Story 1.6 patch P22: `ConversationAuditHandle.FromServerBoundary` produces a SHA-256 derived opaque handle from server-boundary inputs; `ConversationIdempotencyReplayResult.ResultPayload` serializes only `Category`, `SchemaVersion`, `CommandType`, `ConversationId`, `MessageId`, `ParticipantPartyId`, `FileId`, `RejectionCode`, `IsRetryable`, `AuditHandle` (caller `CorrelationId` and `TenantId` are explicitly dropped); `ConversationIdempotencyRecord.ToString()` scrubs `ScopeValue` and `Key` to `<redacted>`. |
| `ConversationCommandFingerprintTest.ProviderCorrelationChangesShouldNotChangeFingerprint` | `ToString()` does not include provider session vocabulary — non-leakage in logs/debug. |
| `ContractsAssemblyBoundaryTest.ContractsAssemblyShouldNotReferenceInfrastructureAssemblies` | Public contracts have **zero** references to Dapr, EventStore, Folders, FrontComposer, Parties, Projects, Tenants, AspNetCore, or System.Net.Http. Infrastructure vocabulary cannot leak through the public surface. |

Verdict: **pass** — exceeds the minimum bar; structural enforcement is stronger than the PRD requires.

**NFR20, NFR21** — Epic 2 governance / redaction. Carry-forward (no Epic 1 evidence claim).

#### Reliability / Idempotency / At-Least-Once (NFR22, NFR23, NFR27)

**NFR22 — duplicate / reordered / retried commands produce no divergent projections**

| Evidence | What it proves |
|---|---|
| `IdempotentConversationCommandExecutorTest.DuplicateCompletedOutcomeShouldReplayWithoutMutation` | Completed-state duplicate replays stored outcome; structural fields (category, tenant, command type, conversation, participant, rejection code, retryability) all match expected; mutation count = 0. (P18 review hardening: switched from reference equality to structural equality so the assertion proves downstream consumers see correct semantics.) |
| `PendingEquivalentSubmissionShouldReturnRetryableUncertaintyWithoutMutation` | Pending in-flight duplicate returns retryable uncertainty without running a second mutation. |
| `ConflictShouldRejectWithoutMutation` | Same scoped key with different command meaning returns `idempotency_conflict` rejection without invoking mutation. |
| `ConversationIdempotencyStoreTest.ConcurrentEquivalentReservationsShouldHaveSingleWinner` | **32 concurrent callers** synchronized on a `Barrier`, racing the lock at the same wall-clock instant: exactly 1 `Reserved`, 31 `RetryableUncertainty`. After completion, a duplicate reserve returns `Duplicate` with the stored outcome. (P7 review hardening: switched from sequential LINQ to `Task.Run` + barrier so the test actually proves atomicity rather than running serially.) |
| `DifferentPayloadShouldReturnConflictWithoutReplacingOutcome` | Conflict path does not replace stored outcome — terminal records are immutable. |
| `ConversationProjectionAccumulatorTest.DuplicateAndReorderedDeliveriesShouldNotCreateDuplicateReadModelRows` | Reordered + duplicated event delivery yields a snapshot with one participant, one message, one file, and 4 processed event IDs — set-by-stable-ID, not blind append. |
| `DuplicateTerminalLifecycleEventsShouldNotRegressState` | Archived cannot regress to Closed even if a late Closed event arrives; terminal lifecycle is idempotent. |
| `DuplicateMetadataUpdatesShouldRemainDeterministic` | Two accumulators applying the same metadata update reach the same snapshot, independent of dictionary order. |

Verdict: **pass** — exceptional coverage. The concurrent-barrier test is the kind of evidence that rarely shows up at Epic 1 stage.

**NFR23 — pub/sub at-least-once tolerance**

| Evidence | What it proves |
|---|---|
| `EventStoreCommandStatusIdempotencyBridgeTest.PendingAndTerminalStatusesProduceDistinguishableInternalReasonCodes` | Internal vocabulary distinguishes "in flight" vs "completed but replay required" — necessary for safe retries against an at-least-once command bus. |
| `MissingStatusReturnsContentSafeRetryableUncertainty` | Missing status ⇒ retry, not "duplicate" — fails open to retry rather than silently de-duping. |
| Subscriber-side dedupe + replay window: not yet built | Story 1.10 (publish versioned domain events) defines subscriber-visible behavior. |

Verdict: **partial** — command-side idempotency wraps both EventStore status and Conversations store correctly; subscriber-side at-least-once awaits Story 1.10.

**NFR27 — verification tooling distinguishes invariant failures from infrastructure**

| Evidence | What it proves |
|---|---|
| Tests use typed `ConversationErrorCode` values (`TenantIsolationViolation`, `IdempotencyConflict`, `IdempotencyOutcomeUnknown`) | Domain invariant failures produce typed errors distinct from infrastructure exceptions. |
| `EventStoreCommandStatusIdempotencyBridge` uses internal reason codes (`eventstore_command_status_pending`, `eventstore_command_status_missing`, `eventstore_terminal_replay_required`) | Infrastructure signal vocabulary is bounded and not leaked into public outcome. |

Verdict: **pass** for what's wired; broader conformance reporting is Epic 5.

**NFR24, NFR25, NFR26, NFR28, NFR29** — pub/sub failure handling, drills, RPO/RTO, failure-mode matrix. Deferred to Epic 5/6. **UNKNOWN-accepted for MVP** per PRD NFR2 / NFR6 (named approver and expiry required at GA; not at Epic 1 gate).

#### Data Integrity / Schema Versioning (NFR40, NFR42)

**NFR40 — schema/version metadata on every command and event; typed errors for unsupported versions**

| Evidence | What it proves |
|---|---|
| `ContractMetadataTest.CommandContractsShouldCarryRequiredMetadata` | All 7 mutating commands (`CreateConversationCommand`, `AppendMessageCommand`, `AddParticipantCommand`, `AttachFileReferenceCommand`, `UpdateConversationMetadataCommand`, `CloseConversationCommand`, `ArchiveConversationCommand`) carry a `Metadata` property whose shape is `SchemaVersion, TenantId, ActorPartyId, CorrelationId, CausationId, IdempotencyKey` — first three non-nullable, last two nullable. |
| `EventContractsShouldCarryRequiredMetadata` | Same for events (`ConversationCreated`, `MessageAppended`, `ParticipantAdded`, `FileReferenceAttached`, `ConversationMetadataUpdated`, …) |
| `ConversationCommandSchemaValidationTest.MissingIdempotencyKeyShouldReturnTypedRejectionForEveryCommandType` (7-case `[Theory]` across `CreateConversationCommand`, `AppendMessageCommand`, `AddParticipantCommand`, `AttachFileReferenceCommand`, `UpdateConversationMetadataCommand`, `CloseConversationCommand`, `ArchiveConversationCommand`) | Command schema validation enforced at boundary for **every** public command type. Backed by Story 1.6 patch P24: new typed error `ConversationErrorCode.IdempotencyKeyMissing` (closed-vocabulary parseable per `ContractValidationTest.ClosedVocabularyParseShouldRejectUnknownValues`) + shared `ConversationCommandSchemaValidation.ValidateEnvelope` that also rejects on `metadata_missing`, `tenant_binding_missing`, `schema_version_missing`, and `unsupported_schema_version` reason codes. Server-side enforcement: `AddParticipantCommandHandlerIdempotencyTest.MissingIdempotencyKeyShouldRejectBeforeTenantAccessAndIdempotencyLookup` asserts tenant-access invocation count = 0, idempotency-store reserve count = 0, state-load count = 0 — rejection precedes every protected operation. |
| `IdempotentConversationCommandExecutorTest`'s `idempotency_outcome_unknown` typed error path | Unsupported / uncertain state surfaces as a typed contract code, not a raw exception. |

Verdict: **pass** for the command + event surface. Published-event schema-version emission is Story 1.10 (deferred).

**NFR42 — temporal evidence anchors named**

| Evidence | What it proves |
|---|---|
| `ConversationAggregateCreateTest.ValidCreateShouldEmitOneConversationCreatedEvent` | Event carries `Metadata.EventId`, `Metadata.EventType`, `Metadata.CommittedAt`, `Metadata.CorrelationId`, `Metadata.CausationId`. Event ID and timestamp are both present and distinct. |
| `Metadata.ConversationId.Value.ShouldNotBe(created.ProviderCorrelation.ProviderSessionReference)` | Provider correlation is not authoritative identity — anchors are Conversations-owned. |

Verdict: **partial** — anchors exist in the event shape; the *contract-level decision* about which anchor is authoritative for time-travel resolution (event position vs. projection version vs. timestamp vs. composite) is owned by Story 1.11 and not yet recorded as an ADR.

**NFR38, NFR39, NFR41, NFR43** — projection rebuild determinism + worked additive schema-evolution example + temporal reconstruction. **UNKNOWN — Story 1.11 not landed.** Carried as the largest MVP gap.

#### Testability / Test Quality (Categories 1, 2 — for code that exists)

| Criterion | Evidence | Verdict |
|---|---|---|
| 1.1 Aggregate testable in isolation | `ConversationAggregateCreateTest`, `ConversationAggregateParticipantTest` exercise `ConversationAggregate.Handle(command, state)` with no infra | pass |
| 1.2 Tenant access testable with fake projection | `FakeTenantProjectionStore` exists in tests, used by `ConversationTenantAccessServiceTest` | pass |
| 1.3 Idempotency testable without EventStore runtime | `InMemoryConversationIdempotencyStore` + `EventStoreCommandStatusIdempotencyBridge` proves bridge interpretation against `CommandStatusRecord` test data | pass |
| 1.4 Deterministic factories | `Hexalith.Conversations.Testing.Factories` referenced by tests; samples like `ConversationTestIdsTest` exist | pass |
| 1.5 Sample valid + invalid contract requests | `ContractSamples.cs` + `ContractValidationTest.cs` + `ContractSerializationTest.cs` (77 passing tests in Contracts project) | pass |
| 2.1 Multi-tenant segregation in tests | Every test that runs the tenant access path uses explicit `TenantId("tenant-a")` / `TenantId("tenant-b")` (e.g. `ConversationTenantAccessServiceTest.OtherTenant`); no shared mutable state | pass |
| 2.2 No real Party PII | Tests use synthetic `PartyId("party-actor")` etc. | pass |
| 2.3 Parallel-safe isolation | xUnit v3 default class-level isolation in use; 268 tests in ~660 ms with no flake observed | pass |
| 2.4 Cleanup / Testcontainers discipline | In-memory fakes drop scope per test; no real Testcontainers usage yet — flagged for Story 1.7+ | partial / UNKNOWN |

#### Project Boundary Discipline (supports NFR19 and Story 1.1 scaffold contracts)

| Evidence | What it proves |
|---|---|
| `ContractsAssemblyBoundaryTest.ContractsAssemblyShouldNotReferenceInfrastructureAssemblies` | Contracts assembly has no references to Dapr, EventStore, Folders, FrontComposer, Parties, Projects, Tenants, AspNetCore, System.Net.Http. Public contracts cannot accidentally pull in infrastructure types. |
| `ContractsProjectFileShouldNotDeclareForbiddenReferences` | Same enforced at the `.csproj` level (XML inspection). |
| `ForbiddenPublicSurfaceTest`, `ContractsAssemblyBoundaryTest` (passing) | Public-surface area is policed. |
| `DomainBoundaryTest`, `DomainProjectBoundaryTest`, `ServerBoundaryTest`, `ClientBoundaryTest` | Cross-layer dependency direction enforced as tests, not conventions. |
| `ScaffoldSmokeTest` (Integration) | Solution file ⇄ disk consistency; project references obey scaffold direction; Contracts/Client/Domain/Server avoid forbidden infrastructure references. |

These boundary tests are not directly required by an Epic 1 NFR, but they back-stop NFR19 (no infrastructure vocabulary leakage) and the PRD's adopter-API design constraints.

### 3.3 Evidence Gaps (carried into Step 4 scoring)

| Gap | Owning story / artifact | NFR(s) impacted | Action |
|---|---|---|---|
| Projection rebuild determinism not proven | Story 1.11 | NFR38, NFR39, NFR43 | Defer; required before "Epic 1 done" gate |
| Worked additive schema-evolution example | Story 1.11 | NFR41 | Defer; required before GA per NFR41 |
| Temporal anchor authority decision (event position vs. projection version vs. composite) | Story 1.11 + ADR | NFR42 | Surface as ADR-needed; required for adopter portability |
| Published-event schema/version emission on the wire | Story 1.10 | NFR23, NFR24, NFR40 publish-side | Defer; subscriber-side replay/dedupe behavior not yet provable |
| Freshness metadata shape on read APIs | Story 1.7 | NFR44, NFR45, NFR46 | Defer; required before "Epic 1 done" gate |
| Tenant access fail-closed on read-side handlers | Stories 1.7, 1.8, 1.9 | NFR16, NFR17, NFR18 read-side | Defer; guard is built and tested, but production read paths not wired yet |
| Burn-in run (N=10 against changed specs) | CI pipeline (Epic 6 or pre-gate) | Reliability evidence breadth | Recommend: run `dotnet test` 10× in CI on PR for changed test files before declaring Epic 1 done |
| Cross-tenant adversarial replay on the published-event subscriber side | Story 1.10 + Story 5.5 conformance | NFR17, NFR18 | Defer to Epic 5 conformance work |
| RPO/RTO / DR drills / failure-mode matrix | Epic 5/6 | NFR24, NFR25, NFR26, NFR28, NFR29 | UNKNOWN-accepted for MVP per NFR2/NFR6 — needs named approver + expiry before GA |

**Step 3 complete.** Proceeding to Step 4 — score each cluster and form the gate decision.

---

## Step 4 — Domain Scoring (Sequential Mode)

Execution mode resolved to `sequential` (single assessor authors all four domain outputs). The workflow's 4-subagent structure is preserved as four sequential domain blocks. Each domain uses status vocabulary `PASS | PARTIAL | CONCERN | UNKNOWN-ACCEPTED | CARRY-FORWARD | FAIL` aligned with PRD NFR2 (`pass / fail / waived / unknown-accepted`) plus `partial` and `carry-forward` for nuance.

### 4.A Security Domain (Tenant Isolation Cluster)

In-scope NFRs: **NFR16, NFR17, NFR18, NFR19**. Carry-forward: **NFR20, NFR21** (Epic 2 governance/redaction).

```json
{
  "domain": "security",
  "risk_level": "LOW",
  "findings": [
    {
      "nfr": "NFR16",
      "title": "Tenant isolation fail-closed before aggregate or projection access",
      "status": "PASS",
      "evidence": [
        "ConversationTenantAccessGuardTest.RunAsyncShouldNotInvokeWriteDelegatesWhenAccessIsDenied — 5 protected operations gated, spy counts = 0",
        "ConversationTenantAccessGuardTest.RunAsyncShouldNotInvokeReadDelegatesWhenAccessIsDenied — 6 protected operations gated, spy counts = 0",
        "ConversationTenantAccessServiceTest.ShouldFailClosedBeforeStoreLookupForUnsafeInputs — store.GetCount = 0 on unsafe inputs"
      ],
      "scope_caveat": "Read-side handlers (Stories 1.7, 1.8, 1.9) not landed; guard is built, consumers will need to call through it."
    },
    {
      "nfr": "NFR17",
      "title": "Adversarial coverage: ID guessing, replay, poisoned events, mixed-tenant rebuild",
      "status": "PARTIAL",
      "evidence": [
        "ConversationTenantAccessServiceTest.ShouldMapTenantRolesConservatively — 9-case role/requirement matrix",
        "ShouldDenyTenantMismatchesBeforeStoreLookup — route vs command tenant mismatch denied",
        "EventStoreCommandStatusIdempotencyBridgeTest.BridgeNeverInventsConversationsOutcome — all enum values exhaustively checked"
      ],
      "gap": "Read-endpoint cross-tenant ID guessing, replayed cross-tenant published events, poisoned projection events, mixed-tenant rebuild — pending Stories 1.7-1.11 + Epic 5 conformance"
    },
    {
      "nfr": "NFR18",
      "title": "Cross-tenant operations fail closed with content-safe responses",
      "status": "PASS",
      "evidence": [
        "Typed ConversationErrorCode.TenantIsolationViolation returned on read denial; no existence disclosure",
        "Typed ConversationErrorCode.IdempotencyConflict returned without state mutation",
        "Story 1.5 typed-error guidance enforced in code"
      ],
      "scope_caveat": "Search / admin / diagnostics paths are Epic 3 / 5 / 6."
    },
    {
      "nfr": "NFR19",
      "title": "No leakage of tenant IDs, Party IDs, conversation existence, redacted content, provider payloads, cross-tenant business references in errors/logs/metrics/traces/diagnostics/conformance",
      "status": "PASS",
      "evidence": [
        "ConversationStateSafetyTest.CreateEventAndStateMembersShouldNotExposeForbiddenPayloadTerms — reflection-based block of DisplayName/Email/Phone/PersonDetails/Prompt/ResponsePayload/AccessToken/Claim/Stream/Envelope/Sequence/ExpectedRevision",
        "IdempotentConversationCommandExecutorTest.DuplicateReplayPayloadShouldExcludeCallerSuppliedCorrelationAndScope — payload excludes correlationId, idempotency key, tenant value",
        "ConversationCommandFingerprintTest.ProviderCorrelationChangesShouldNotChangeFingerprint — ToString does not leak provider session vocabulary",
        "ContractsAssemblyBoundaryTest — Contracts assembly has zero references to Dapr/EventStore/Folders/FrontComposer/Parties/Projects/Tenants/AspNetCore/HttpClient"
      ],
      "note": "Exceeds PRD requirement: structural enforcement at the type-system layer, not only at runtime logs."
    },
    {
      "nfr": "NFR20",
      "title": "Governance mutations fail closed when audit unavailable",
      "status": "CARRY-FORWARD",
      "owner": "Epic 2"
    },
    {
      "nfr": "NFR21",
      "title": "Redacted content does not reappear in projections / search / audit / caches / exports / temporal views / replay / logs / traces / errors",
      "status": "CARRY-FORWARD",
      "owner": "Epic 2 + Epic 5"
    }
  ],
  "compliance": {
    "NFR16_tenant_isolation_release_blocker": "PASS",
    "NFR17_adversarial_coverage": "PARTIAL",
    "NFR18_content_safe_denials": "PASS",
    "NFR19_non_leakage": "PASS"
  },
  "priority_actions": [
    "Extend tenant-isolation adversarial tests to read-side handlers in Story 1.7 + 1.8 (replicate Guard-test spy pattern for projection read paths).",
    "When Story 1.10 lands published events, add adversarial subscriber-side tests for replayed cross-tenant events.",
    "Keep ConversationStateSafetyTest forbidden-term list synchronized as new event types are added in Epic 2."
  ]
}
```

**Security verdict:** **PASS for the Epic 1 command-side surface.** Risk: **LOW** for what's in scope. Strong structural enforcement raises confidence above the bar the PRD sets.

### 4.B Performance Domain (Out of Scope)

In-scope NFRs: none. Out-of-scope NFRs deferred: NFR9, NFR10, NFR11, NFR12, NFR13, NFR14, NFR15.

```json
{
  "domain": "performance",
  "risk_level": "UNKNOWN",
  "findings": [
    {
      "nfr": "NFR9-NFR15 (open conversation P95, append latency, operator search 90s, etc.)",
      "status": "UNKNOWN-ACCEPTED",
      "rationale": "No benchmark harness yet. PRD NFR4 requires numeric thresholds before GA implementation; current phase is Epic 1 foundation. Test execution speed (~660 ms suite) is healthy but not equivalent to application latency.",
      "owner": "Pre-GA performance workstream"
    }
  ],
  "compliance": {
    "NFR9_open_p95_500ms": "UNKNOWN-ACCEPTED",
    "NFR11_cold_start_target": "UNKNOWN-ACCEPTED",
    "NFR14_append_latency": "UNKNOWN-ACCEPTED"
  },
  "priority_actions": [
    "Before GA: stand up a NBomber / k6 perf harness with the NFR7 measurement envelope (data volume, tenant count, concurrent opens/sec/tenant, projection state, cache state).",
    "Establish baseline P95 open-conversation latency once Story 1.7 read-model is in place.",
    "Classify each numeric target as Release-blocker / Validation-target / Capacity-discovery-target before kickoff (NFR5)."
  ]
}
```

**Performance verdict:** **UNKNOWN-ACCEPTED for MVP**. Not blocking Epic 1 completion. Must be converted to numeric thresholds with named approver + expiry before GA per NFR6.

### 4.C Reliability Domain (Idempotency / At-Least-Once / Resilience)

In-scope NFRs: **NFR22, NFR23, NFR27**. Deferred: NFR24, NFR25, NFR26, NFR28, NFR29.

```json
{
  "domain": "reliability",
  "risk_level": "LOW",
  "findings": [
    {
      "nfr": "NFR22",
      "title": "Duplicate / reordered / retried commands produce no divergent projections or duplicate business effects",
      "status": "PASS",
      "evidence": [
        "IdempotentConversationCommandExecutorTest — completed dupe replays outcome (mutation=0); conflict rejects (mutation=0); pending dupe returns retryable uncertainty (mutation=0)",
        "ConversationIdempotencyStoreTest.ConcurrentEquivalentReservationsShouldHaveSingleWinner — 32 callers race a barrier, exactly 1 Reserved + 31 RetryableUncertainty",
        "ConversationProjectionAccumulatorTest — duplicate + reordered participant/message/file events: snapshot has 1 of each; terminal lifecycle archived cannot regress to closed; metadata updates deterministic across dictionary order",
        "P7 + P18 + P22 + P26 code-review hardenings landed (barrier-synchronized concurrency proof; structural vs reference equality; payload non-disclosure assertions; externally-observable contracts vs enum-mirror)"
      ],
      "note": "This is exceptional Epic-1-stage evidence — concurrent barrier test is rare at this maturity."
    },
    {
      "nfr": "NFR23",
      "title": "Pub/sub at-least-once tolerance",
      "status": "PARTIAL",
      "evidence": [
        "EventStoreCommandStatusIdempotencyBridgeTest — command-side internal vocabulary distinguishes pending vs terminal-replay vs missing",
        "Bridge never invents Conversations outcome from EventStore status alone (defensive against future enum additions)"
      ],
      "gap": "Subscriber-side dedupe + replay window not exercised — owned by Story 1.10."
    },
    {
      "nfr": "NFR24",
      "title": "Pub/sub publication failure: retry / dead-letter / replay / subscriber notification",
      "status": "UNKNOWN",
      "owner": "Story 1.10 + ADR"
    },
    {
      "nfr": "NFR25",
      "title": "Operational drills: sidecar restart, EventStore degradation, projection-rebuilder crash, audit-sink degradation, redaction propagation failure",
      "status": "UNKNOWN-ACCEPTED",
      "owner": "Epic 5/6 pre-GA",
      "note": "Per PRD NFR6: requires named approver + expiry + compensating control if deferred past GA."
    },
    {
      "nfr": "NFR26",
      "title": "Failure-mode matrix (dep × failure × retry × dead-letter × operator signal × recovery)",
      "status": "UNKNOWN",
      "owner": "ADR — not landed in any Epic 1 artifact"
    },
    {
      "nfr": "NFR27",
      "title": "Verification tooling distinguishes invariant failures from infrastructure failures",
      "status": "PASS",
      "evidence": [
        "Typed ConversationErrorCode (TenantIsolationViolation, IdempotencyConflict, IdempotencyOutcomeUnknown) used for invariants",
        "Internal reason codes (eventstore_command_status_pending/missing, eventstore_terminal_replay_required) used for infrastructure signal",
        "Public outcome never exposes EventStore vocabulary"
      ]
    },
    {
      "nfr": "NFR28-NFR29",
      "title": "RPO/RTO + backup restore + tenant-scoped recovery",
      "status": "UNKNOWN-ACCEPTED",
      "owner": "Epic 5/6 pre-GA"
    }
  ],
  "compliance": {
    "NFR22_idempotency_no_divergence": "PASS",
    "NFR23_at_least_once_tolerance": "PARTIAL",
    "NFR27_invariant_vs_infrastructure_separation": "PASS",
    "NFR24_NFR25_NFR26_NFR28_NFR29_drills_and_RPO_RTO": "UNKNOWN-ACCEPTED"
  },
  "priority_actions": [
    "Land Story 1.10 published-event subscriber-side dedupe + replay tests to convert NFR23 from PARTIAL to PASS.",
    "Land Story 1.11 projection rebuild + schema-evolution example to unblock NFR38/39/41 (data-integrity cluster).",
    "Before GA: define failure-mode matrix (NFR26) as an ADR. The current code defensively handles many failure modes; documenting them is the next step.",
    "Before GA: schedule operational drills (NFR25) and define RPO/RTO (NFR28); attach named approver + expiry per NFR6 if deferred."
  ]
}
```

**Reliability verdict:** **PASS for command-side**, PARTIAL for the at-least-once subscriber surface. Risk: **LOW** for what's in scope. The dependency on Story 1.10 / 1.11 is real but explicitly accepted as deferred under the narrowed Epic 1 scope.

### 4.D Scalability Domain (Out of Scope)

In-scope NFRs: none. Out-of-scope NFRs deferred: NFR30-NFR37.

```json
{
  "domain": "scalability",
  "risk_level": "UNKNOWN",
  "findings": [
    {
      "nfr": "NFR30",
      "title": "Numeric targets or buyer-accepted unknowns for events/sec, concurrent conversations, write-amplification, concurrent opens/sec/tenant",
      "status": "UNKNOWN-ACCEPTED",
      "owner": "Pre-GA capacity workstream"
    },
    {
      "nfr": "NFR31, NFR32",
      "title": "Projection rebuild at 1M / 10M / 100M events",
      "status": "UNKNOWN",
      "owner": "Story 1.11 + capacity workstream",
      "note": "1M is MVP-required per NFR32 tiering; 10M pre-scale; 100M capacity evidence."
    },
    {
      "nfr": "NFR33",
      "title": "Long-running rebuilds: progress reporting, resumability, tenant-scoped cancellation",
      "status": "UNKNOWN",
      "owner": "Story 1.11"
    },
    {
      "nfr": "NFR34, NFR35",
      "title": "Tenant-events lag SLO + redaction-propagation SLO",
      "status": "UNKNOWN",
      "owner": "Epic 6 observability"
    },
    {
      "nfr": "NFR36, NFR37",
      "title": "Cost-relevant capacity indicators + numeric cost thresholds",
      "status": "UNKNOWN-ACCEPTED",
      "owner": "Pre-GA"
    }
  ],
  "compliance": {
    "NFR30_pre_kickoff_capacity_targets": "UNKNOWN-ACCEPTED",
    "NFR31_projection_rebuild_1M": "UNKNOWN",
    "NFR33_rebuild_resumability": "UNKNOWN"
  },
  "priority_actions": [
    "Land Story 1.11 projection rebuild proof at the 1M-event tier (NFR32 MVP requirement).",
    "Define capacity envelope (NFR7) with named tenant counts / concurrent conversation count / event rate before kickoff of any release-gated benchmark.",
    "Attach Story 1.11 progress/resumability behavior to ADR or operational runbook."
  ]
}
```

**Scalability verdict:** **UNKNOWN-ACCEPTED for MVP**, with the explicit understanding that **NFR31's 1M-event rebuild is an MVP requirement** that Story 1.11 must land before "Epic 1 done" can be declared.

---

## Step 4E — Aggregation

### 4E.1 Overall Risk Level

| Domain | Risk level | Rationale |
|---|---|---|
| Security | LOW | Strong structural + behavioral evidence for command-side; gaps are scope, not vulnerabilities |
| Performance | UNKNOWN | Deliberately out of scope; needs harness pre-GA |
| Reliability | LOW | Idempotency cluster is exceptional; subscriber-side gap is owned by Story 1.10 |
| Scalability | UNKNOWN | Deliberately out of scope; NFR31 1M-event rebuild required before "Epic 1 done" |

**Overall risk for current code (narrowed scope):** **LOW**.

**Overall risk for the "Epic 1 done" gate:** **MEDIUM**, driven by:

1. Six stories (1.6 in-progress, 1.7–1.11) still required to declare Epic 1 done.
2. Story 1.11 (rebuild proof) ties to MVP-required NFR31 + the data-integrity cluster (NFR38/39/41/43).
3. Story 1.10 (published events) ties to NFR23 / NFR24 / NFR40 publish-side.

### 4E.2 Compliance Summary

| PRD NFR cluster | Verdict | Story dependency |
|---|---|---|
| Tenant isolation (NFR16-19) | **PASS** for command-side; read-side awaits 1.7-1.9 | 1.7 / 1.8 / 1.9 |
| Reliability/idempotency (NFR22, NFR27) | **PASS** | none for what's in scope |
| At-least-once subscriber (NFR23) | **PARTIAL** | 1.10 |
| Drills / RPO/RTO / failure-mode matrix (NFR24-26, NFR28-29) | **UNKNOWN-ACCEPTED** for MVP | Epic 5/6 + named-waiver process |
| Schema/version metadata (NFR40) | **PASS** for command-side; publish-side awaits 1.10 | 1.10 |
| Temporal anchors (NFR42) | **PARTIAL** — present in events; authority not declared in ADR | 1.11 + ADR |
| Projection rebuild + schema evolution (NFR38, NFR39, NFR41, NFR43) | **UNKNOWN** — Story 1.11 not landed | 1.11 |
| Projection freshness (NFR44-46) | **UNKNOWN** — Story 1.7 not landed | 1.7 |
| Operator surfaces (NFR47-48) | **CARRY-FORWARD** | Epic 3 + 6 |
| Governance / redaction (NFR20-21) | **CARRY-FORWARD** | Epic 2 |
| Testability + boundary discipline | **PASS** | none |

### 4E.3 Cross-Domain Risks

| # | Domains | Risk | Impact |
|---|---|---|---|
| 1 | Security ↔ Data Integrity | Read-side tenant isolation (NFR16 read) and projection freshness (NFR44-46) both depend on Story 1.7. A bug in the freshness shape could conceivably leak the existence of a stale-but-real conversation across tenants if `lastAppliedEventPosition` is exposed without tenant scoping. | MEDIUM — surface when Story 1.7 is implemented; require structural test for tenant-scoping of every freshness field |
| 2 | Reliability ↔ Data Integrity | Story 1.10 publishes events to Dapr (at-least-once). If subscriber-side dedup isn't combined with projection-side idempotency (NFR22 — already proven), the system stays safe; if NFR22 ever weakens, NFR23 could cause projection divergence. | LOW today, watch the regression bar — extend `ConversationProjectionAccumulator` tests when Story 1.10 lands |
| 3 | Reliability ↔ Scalability | NFR31 (rebuild at 1M events) is BOTH a scalability NFR and a reliability evidence for replay determinism. Story 1.11 carries dual significance — it is the highest-leverage remaining MVP story. | HIGH leverage, not high risk — recommend prioritizing Story 1.11 |
| 4 | Performance ↔ Reliability | Idempotency lookup adds latency. Once Story 1.7 read APIs land, NFR9 (P95 ≤ 500 ms) will be measured *with* the idempotency hot path. Defensive design (in-memory store + atomic reserve) suggests the cost is small, but unmeasured. | LOW — flag for the future perf harness |

### 4E.4 Prioritized Action List

| Priority | Action | Owner | Blocks |
|---|---|---|---|
| URGENT-1 | Land Story 1.11 (replay, schema versioning, projection rebuild) | Dev | NFR31 (MVP-required), NFR38/39/41/43, NFR42 authority decision |
| URGENT-2 | Land Story 1.10 (publish versioned events) | Dev | NFR23 (subscriber-side), NFR24, NFR40 publish-side |
| URGENT-3 | Land Story 1.7 (read models + freshness) | Dev | NFR44-46, read-side tenant isolation |
| HIGH | When 1.7 ships, port the Guard-test spy pattern to read-side handlers to extend NFR16/17/18 to read path | Dev | NFR17 adversarial breadth |
| HIGH | When 1.10 ships, add subscriber-side replay + cross-tenant adversarial tests | Dev | NFR17, NFR23, NFR18 read/diagnostic path |
| MEDIUM | Author ADR declaring temporal-evidence authority anchor (event position vs projection version vs composite) | Architect | NFR42, adopter portability |
| MEDIUM | Author failure-mode matrix ADR (NFR26) | Architect | Pre-GA |
| MEDIUM | Add CI burn-in (N=10) on changed test files per PR | DevOps | Reliability evidence breadth |
| LOW | Maintain ConversationStateSafetyTest forbidden-term list as Epic 2 governance events are added | Dev (ongoing) | NFR19 regression guard |
| DEFER (Pre-GA) | Stand up NBomber/k6 perf harness with NFR7 envelope | Perf workstream | NFR9-15 |
| DEFER (Pre-GA) | Schedule operational drills + define RPO/RTO | Ops | NFR25, NFR28, NFR29 |

### 4E.5 Execution Report

- Execution mode: **sequential** (single assessor authored all 4 domain blocks; no subagent dispatch)
- Domains assessed: 4 / 4
- Cross-domain risks: 4 identified
- Priority actions: 11 (3 URGENT, 2 HIGH, 3 MEDIUM, 1 LOW, 2 DEFER)

**Step 4 complete.** Proceeding to Step 5 — final NFR report with gate decision and executive summary.

---

## Step 5 — Final NFR Report

### Executive Summary

**Feature:** Hexalith.Conversations — Epic 1 foundation (event-sourced conversation substrate).
**Assessment scope:** Stories 1.1–1.5 (done) + 1.6 local idempotency proof (in-progress), against the **MVP / Epic 1 done** release gate.
**Date:** 2026-05-19. **Assessor:** Murat.

**Verdict for shipped code (1.1–1.5 + 1.6 local proof): PASS ✅**
**Verdict for declaring "Epic 1 done" gate: CONCERNS ⚠️** — six stories must still land (1.6 close-out, 1.7, 1.8, 1.9, 1.10, 1.11). The data-integrity cluster (NFR38/39/41/43) is the largest open gap and depends entirely on Story 1.11.

**Headline counts (narrowed scope):**

- **8 PASS** — NFR16 command-side, NFR18 command-side, NFR19 (exceeds requirement), NFR22, NFR27, NFR40 command-side, testability, project-boundary discipline.
- **3 PARTIAL** — NFR17 (write-side complete, read-side pending), NFR23 (command-side complete, subscriber-side pending), NFR42 (anchors present, authority decision pending ADR).
- **7 UNKNOWN (deferred to later stories)** — NFR38, NFR39, NFR41, NFR43, NFR44–46. All blocked on stories 1.7 or 1.11.
- **5 UNKNOWN-ACCEPTED (deferred pre-GA)** — NFR24, NFR25, NFR26, NFR28, NFR29. Per PRD NFR2/NFR6, require named approver + expiry before GA.
- **2 CARRY-FORWARD** — NFR20, NFR21 (Epic 2 governance). Not Epic 1 territory.

**Blockers found in shipped code:** **0**.
**Critical adversarial gaps:** **0** for what's wired. Read-side adversarial coverage and subscriber-side cross-tenant replay are scheduled but unimplemented.

**Recommendation:** Continue Epic 1 build-out with these priorities — (1) Story 1.11 first (highest leverage: unblocks 5 NFRs and is MVP-required per NFR32 tiering), (2) Story 1.10 (unblocks NFR23 subscriber side + NFR40 publish side), (3) Story 1.7 (read-model freshness shape + read-side tenant isolation evidence). Defer perf/scalability/DR work to dedicated workstreams with named approvers per NFR6.

---

### Assessment By Category (ADR 8-Category Checklist Mapped to Epic 1 Scope)

| Category | Criteria met (in-scope subset) | PASS | PARTIAL | UNKNOWN/Deferred | Overall |
|---|---|---|---|---|---|
| 1. Testability & Automation | 5/5 | 5 | 0 | 0 | **PASS ✅** |
| 2. Test Data Strategy | 3/4 | 3 | 0 | 1 (Testcontainers cleanup pending Story 1.7) | **PASS ✅** |
| 3. Scalability & Availability (=Reliability/Idempotency in Epic 1) | 3/8 | 2 (NFR22, NFR27) | 1 (NFR23) | 5 (NFR24–26, NFR28–29) | **CONCERNS ⚠️** |
| 4. Disaster Recovery | 0/3 | 0 | 0 | 3 (UNKNOWN-ACCEPTED, deferred pre-GA) | **N/A — out of scope** |
| 5. Security (Tenant Isolation cluster) | 3/4 | 3 (NFR16, NFR18, NFR19) | 1 (NFR17) | 0 | **PASS ✅** |
| 6. Monitorability / Debuggability (Projection Freshness in Epic 1) | 0/3 | 0 | 0 | 3 (NFR44–46 pending Story 1.7) | **CONCERNS ⚠️** |
| 7. QoS / QoE | 0/4 | 0 | 0 | 4 (UNKNOWN-ACCEPTED, deferred pre-GA) | **N/A — out of scope** |
| 8. Deployability (Schema Evolution in Epic 1) | 1/6 | 1 (NFR40 command-side) | 1 (NFR42) | 4 (NFR38, NFR39, NFR41, NFR43 pending Story 1.11) | **CONCERNS ⚠️** |
| **Total (in-scope)** | **15/33** | **14** | **3** | **16** | **CONCERNS ⚠️ for Epic 1 done; PASS ✅ for shipped code** |

Note: Total counts depart from the literal ADR-checklist 29 because the scope mapping merges PRD NFR clusters (e.g., schema versioning under Category 8). The verdict honors the narrowed scope: PASS for code that exists, CONCERNS for the broader gate.

---

### Performance Assessment

**Out of scope.** No benchmark harness exists. Test execution time (~660 ms for 268 tests) is healthy but does not equate to application latency.

- **NFR9 / NFR11 / NFR14 (open-conversation P95, cold-start, append latency):** UNKNOWN-ACCEPTED. Threshold defined in PRD; measurement method not yet built.
- **Quick win:** Once Story 1.7 read API exists, capture baseline P95 with `dotnet-counters` to establish a starting point — does not require a full perf harness.

### Security Assessment

- **AuthN/AuthZ (tenant access):** **PASS ✅**. `ConversationTenantAccessService` enforces role mapping (Reader/Contributor/Owner × Read/Write/Admin) via 9-case `[Theory]`; tenant mismatches and unsafe inputs fail before projection lookup.
- **Data protection (non-leakage):** **PASS ✅** — exceeds requirement. Reflection-based forbidden-payload-term blocklist (`ConversationStateSafetyTest`) plus assembly-boundary tests (`ContractsAssemblyBoundaryTest`).
- **Input validation:** **PASS ✅**. `ConversationCommandSchemaValidationTest`, `CreateConversationBoundaryTest`, contract serialization tests cover validation.
- **API security (rate limiting, CORS, headers):** **N/A** — no public API surface yet. Carry-forward to Epic 4.
- **Secrets:** **N/A** — no production runtime; no secrets in the repo.
- **Compliance:** Module-level evidence pattern (NFR64) is owned by Epic 5; Epic 1 establishes the structural invariants the conformance pack will assert.

### Reliability Assessment

- **Idempotency (NFR22):** **PASS ✅** — exceptional evidence including 32-caller concurrent barrier proof of atomic reservation.
- **At-least-once tolerance (NFR23):** **PARTIAL** — command-side proven; subscriber-side awaits Story 1.10.
- **Fault tolerance (NFR27):** **PASS ✅** — typed errors distinguish invariant failures from infrastructure signal vocabulary.
- **CI burn-in (stability):** **CONCERNS ⚠️** — single clean run (268/268 in 660 ms) observed today; no automated N=10 burn-in in CI. **Quick win:** run `dotnet test` in a loop of 10 on PR for changed test files.
- **RTO / RPO (NFR28):** UNKNOWN-ACCEPTED. Deferred pre-GA per NFR6 (requires named approver + expiry).

### Maintainability Assessment

- **Test coverage:** Quantitative coverage% not measured this run. `coverlet.collector` is wired (per framework-setup-progress.md). **Action:** add `dotnet test --collect:"XPlat Code Coverage"` to nightly CI and publish lcov.
- **Code quality:** Project uses warnings-as-errors, nullable enabled, central package management. Story files show code-review hardening (P-numbered fixes landed in stories 1.5 + 1.6). **Status:** PASS qualitative.
- **Test quality (DoD):** **PASS ✅** — tests deterministic, isolated, under 1.5 min total, no skipped/flaky observed; explicit assertions; xUnit parallelism enabled.
- **Documentation:** PRD + architecture + per-story implementation summaries + project-context.md exist. **Gap:** ADR for temporal-evidence anchor (NFR42) not yet authored.

---

### Quick Wins

3 quick wins identified for immediate implementation:

1. **CI burn-in loop (Reliability)** — LOW effort. Add a GitHub Actions / Azure DevOps step that runs `dotnet test` on changed test files in a `for i in {1..10}` loop on PR. No code change; converts "single-run green" evidence to "10× green" evidence. Backs NFR22 evidence breadth.
2. **Coverage publication (Maintainability)** — LOW effort. Add `--collect:"XPlat Code Coverage"` and publish lcov. Establishes baseline for future regression tracking. No code change.
3. **Author NFR42 temporal-authority ADR (Data Integrity)** — MEDIUM effort, but unlocks Story 1.11 cleanly. Architect-day work. Names the authoritative anchor (event position vs projection version vs composite) so downstream conformance and adopter docs reference one source of truth.

---

### Recommended Actions

#### Immediate (Before declaring Epic 1 done) — HIGH Priority

1. **Land Story 1.11 (replay, schema versioning, projection rebuild)** — HIGH — Dev — multi-day
   - Implements the projection rebuilder + schema-evolution worked example.
   - Validates: NFR38 rebuild from event stream, NFR39 deterministic rebuild, NFR41 worked additive change, NFR43 temporal reconstruction.
   - **Pre-requisite:** ADR on temporal-evidence anchor (NFR42).
   - **Acceptance:** integration test demonstrates byte-equivalent rebuild for a representative event history; schema-version mismatch fails with typed error.

2. **Land Story 1.10 (publish versioned domain events)** — HIGH — Dev — multi-day
   - Wires the Dapr pub/sub publication path + subscriber-side test patterns.
   - Validates: NFR23 subscriber dedupe + replay window; NFR40 publish-side schema/version emission; NFR24 publication failure handling (retry/dead-letter/replay).
   - **Acceptance:** induced duplicates, reordering, and dead-letter replay all observable and content-safe.

3. **Land Story 1.7 (read models with freshness)** — HIGH — Dev — multi-day
   - Wires the read API with `lastAppliedEventPosition`, `lastAppliedEventTimestamp`, `projectionGeneratedAt`, `isStale`, `lagDuration` (or documented equivalent shape).
   - Validates: NFR44, NFR45, NFR46 freshness contract.
   - **Critical:** extend Guard spy pattern to the read-side handlers when this lands — currently NFR16/17/18 cover the write path only.

#### Short-term (Next Milestone) — MEDIUM Priority

4. **ADR: temporal-evidence anchor (NFR42)** — MEDIUM — Architect — 1 day. Names event position OR projection version OR composite as the authoritative anchor. Blocks 1.11 acceptance.
5. **ADR: failure-mode matrix (NFR26)** — MEDIUM — Architect — 2 days. Tabulates DAPR / EventStore / projections / pub/sub / tenant projection / audit sink × failure × retry × dead-letter × operator signal × recovery. Required pre-GA.
6. **CI burn-in N=10 on changed specs** — MEDIUM — DevOps — half-day. See Quick Win 1.
7. **Coverage publication via coverlet** — MEDIUM — DevOps — half-day. See Quick Win 2.

#### Long-term (Pre-GA backlog) — LOW Priority for now, REQUIRED before GA

8. **Perf harness with NFR7 envelope** — Owner: TBD perf workstream. NBomber or k6 against staging Aspire deployment with seeded data at 50/500/5000 conversations × 20 participants × 5 AI agents.
9. **DR drills (NFR25, NFR29)** — Owner: Ops. Required pre-GA.
10. **Define RPO/RTO (NFR28)** — Owner: Architect. Numeric target + restore drill.
11. **Capacity benchmarks (NFR31 1M / 10M / 100M events)** — Owner: Perf + Architect. NFR31 1M is **MVP-required** per NFR32 tiering — convert from "long-term" to "Immediate" if MVP scope tightens.

---

### Monitoring Hooks (Pre-GA — not Epic 1 territory but flagged here)

- [ ] Idempotency store hit-rate counter (dedupe-rate metric) — confirms NFR22 wired in production
- [ ] Tenant access denial counter by denial-reason — NFR55 / NFR98 operability
- [ ] Projection lag gauge per tenant — NFR45 freshness + NFR96 operability
- [ ] Pub/sub publication failure counter + dead-letter age gauge — NFR24 / NFR97

### Fail-Fast Mechanisms

- [ ] Tenant access guard already runs **before** aggregate load / projection lookup (NFR16 implemented)
- [ ] Idempotency reserve **before** mutation (NFR22 implemented)
- [ ] Schema-version validation **at boundary** (NFR40 command-side implemented)
- [ ] Audit-pairing enforcement on governance commands — owned by Epic 2

---

### Evidence Gaps (with owners and required-by milestone)

- [ ] **NFR38 / NFR39 / NFR41 / NFR43 — projection rebuild determinism + schema-evolution worked example**
  - **Owner:** Dev (Story 1.11)
  - **Required by:** Epic 1 done
  - **Suggested evidence:** integration test that replays a representative event history into two fresh projection stores and asserts byte-equivalent snapshots (NFR38/39); a `v1 → v1.1` additive change in one event with paired test (NFR41); temporal-cursor resolution test (NFR43).
  - **Spec hardening (commit 492055a, 2026-05-19):** Story 1.11 now explicitly requires (a) **validation precedence ordering** before replay (parse metadata → tenant/conversation identity → schema-version support → event-type support → ordering/duplicate semantics → payload application; earlier failures yield typed content-safe negatives and cannot upgrade projection/evidence state to trusted), (b) **idempotency records are NOT replay authority** — replay/rebuild correctness depends only on the ordered persisted event history, idempotency keys/conflict fingerprints/command-status records can explain but never authorize replay state, (c) **side-channel equivalence tests** for unauthorized/nonexistent/cross-tenant/tenant-hidden/unsupported-version/malformed/poisoned/quarantined cases — public response shape, freshness/trust vocabulary, counts, cursors, timing-bearing timestamps, and diagnostic-code classes must not distinguish existence/ownership, (d) **poison-quarantine bounded scope** — tests must prove poisoned/quarantined records cannot contaminate other tenants' projections or make unrelated conversations appear stale/unavailable/missing, (e) **fixed-fixture identifier discipline** — local evidence may use abstract ordered cursors / projection versions but must not serialize raw EventStore stream names, storage offsets, subscription names, raw provider IDs, Party display data, or real tenant/user identifiers. These hardened expectations also cover the read-side NFR18/NFR19 surface for the rebuild path.
  - **Impact:** Largest open gap. Blocks "Epic 1 done."

- [ ] **NFR44 / NFR45 / NFR46 — projection freshness contract**
  - **Owner:** Dev (Story 1.7)
  - **Required by:** Epic 1 done
  - **Suggested evidence:** contract test asserting freshness shape on read API; integration test exercising `current` / `stale` / `rebuilding` / `unavailable` / `hidden-by-tenant-isolation` semantics.

- [ ] **NFR23 subscriber-side, NFR24 publication failure, NFR40 publish-side**
  - **Owner:** Dev (Story 1.10)
  - **Required by:** Epic 1 done
  - **Suggested evidence:** integration test with Dapr Test Harness or in-process pub/sub fake exercising induced duplicates, reordering, dead-letter replay, schema-version emission.

- [ ] **NFR42 — temporal-evidence anchor authority**
  - **Owner:** Architect (ADR)
  - **Required by:** Story 1.11 acceptance
  - **Suggested evidence:** ADR file naming the authoritative anchor (event position vs projection version vs timestamp vs composite); contract test enforcing the choice.
  - **Spec hardening (commit 492055a, 2026-05-19):** Story 1.11's hardened tasks now require the ordering proof to explicitly name "stream identity, event identity, schema version, event position or equivalent ordered cursor, duplicate event identity handling, gap/reorder handling, and tie behavior when the branch lacks a stronger EventStore ordering primitive." Whichever anchor the ADR picks must satisfy all of those clauses.

- [ ] **NFR17 read-side adversarial coverage**
  - **Owner:** Dev (when Stories 1.7 / 1.8 land)
  - **Required by:** Epic 1 done
  - **Suggested evidence:** read-side Guard spy tests mirroring the write-side pattern; cross-tenant ID-guessing adversarial test on read endpoints.

- [ ] **CI burn-in run (N=10)**
  - **Owner:** DevOps
  - **Required by:** Epic 1 close-out
  - **Suggested evidence:** CI artifact log of 10 consecutive green runs on changed test files.

- [ ] **NFR25, NFR26, NFR28, NFR29 — drills / failure-mode matrix / RPO/RTO**
  - **Owner:** Ops + Architect
  - **Required by:** GA (waived for MVP per NFR6 if named approver + expiry attached)

---

### Gate YAML Snippet

```yaml
nfr_assessment:
  date: '2026-05-19'
  feature_name: 'Hexalith.Conversations Epic 1'
  scope: 'stories 1.1-1.5 done + 1.6 local idempotency proof'
  release_gate: 'MVP / Epic 1 done'
  adr_checklist_score: '15/33 in-scope criteria PASS or PARTIAL (the remaining 16 are deferred to later stories or pre-GA)'
  execution_mode: 'sequential'
  test_run:
    suite: 'Hexalith.Conversations.slnx'
    total: 268
    passed: 268
    failed: 0
    skipped: 0
    duration_ms: 659
  categories:
    testability_automation: 'PASS'
    test_data_strategy: 'PASS'
    scalability_availability: 'CONCERNS'
    disaster_recovery: 'OUT-OF-SCOPE'
    security: 'PASS'
    monitorability: 'CONCERNS'
    qos_qoe: 'OUT-OF-SCOPE'
    deployability: 'CONCERNS'
  overall_status_shipped_code: 'PASS'
  overall_status_epic_1_done_gate: 'CONCERNS'
  critical_issues: 0
  high_priority_issues: 3   # Stories 1.7, 1.10, 1.11 must land
  medium_priority_issues: 4 # ADRs + CI burn-in + coverage publication
  concerns: 3               # NFR17 read-side, NFR23 subscriber-side, NFR42 anchor authority
  blockers: false
  quick_wins: 3
  evidence_gaps: 7
  recommendations:
    - 'Land Story 1.11 (replay, schema versioning, projection rebuild) — highest leverage; unblocks NFR38/39/41/43 and MVP-required NFR31.'
    - 'Land Story 1.10 (publish versioned events) — unblocks NFR23 subscriber-side and NFR40 publish-side.'
    - 'Land Story 1.7 (read models + freshness) — unblocks NFR44/45/46 and read-side tenant-isolation evidence.'
    - 'Author ADR for temporal-evidence anchor (NFR42) before Story 1.11 acceptance.'
    - 'Add CI burn-in N=10 on changed test files per PR.'
    - 'Convert deferred drills / RPO/RTO / failure-mode matrix to named-approver waivers per NFR6 if not landing pre-GA.'
```

---

### Related Artifacts

- **PRD:** `_bmad-output/planning-artifacts/prd.md` (NFR section: lines 1321–1434)
- **Architecture:** `_bmad-output/planning-artifacts/architecture.md`
- **Epics:** `_bmad-output/planning-artifacts/epics.md`
- **Sprint status:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
- **Story 1.1–1.11 implementation summaries:** `_bmad-output/implementation-artifacts/1-*.md`
- **Idempotency local evidence:** `_bmad-output/implementation-artifacts/1-6-idempotency-local-evidence.md`
- **Project context (binding rules):** `_bmad-output/project-context.md`
- **Test framework:** `_bmad-output/test-artifacts/framework-setup-progress.md`
- **Test automation summary:** `_bmad-output/test-artifacts/automation-summary.md`
- **Test directory:** `tests/Hexalith.Conversations.*.Tests/`

---

### Checklist Validation

| Validation item | Status |
|---|---|
| Prerequisites: implementation accessible for evaluation | ✅ source + tests on disk |
| Prerequisites: evidence sources available | ✅ 33 test files + 268 passing tests + implementation summaries |
| Prerequisites: NFR categories determined | ✅ 8 ADR categories mapped to PRD NFR clusters |
| Prerequisites: knowledge base loaded | ✅ adr-quality-readiness-checklist, test-quality, error-handling, ci-burn-in |
| Context: PRD loaded | ✅ NFR section lines 1321-1434 |
| Context: Story files loaded | ✅ 11 stories enumerated; 1.1-1.6 + 1.11 inspected |
| Context: tech-spec.md loaded | N/A — does not exist for this project |
| Thresholds: defined or UNKNOWN | ✅ All in-scope NFRs have a threshold or explicit UNKNOWN |
| Thresholds: no guessing | ✅ Every PASS cites a specific test; every UNKNOWN cites the owning story |
| Evidence: performance | UNKNOWN-ACCEPTED (out of scope) |
| Evidence: security | ✅ tenant access tests + non-leakage tests + assembly boundary tests |
| Evidence: reliability | ✅ idempotency tests + projection accumulator tests + bridge tests |
| Evidence: maintainability | ✅ 268 passing tests in 660 ms; warnings-as-errors; central package management |
| Status classification: deterministic | ✅ Every cluster verdict tied to a specific test artifact or owning story |
| Quick wins: identified | ✅ 3 quick wins listed |
| Recommended actions: specific + prioritized + owners | ✅ 11 actions with priority and owner |
| Evidence gaps: documented with owners + deadline | ✅ 7 gaps each with owning story and required-by milestone |
| Final report: file created at `_bmad-output/test-artifacts/nfr-assessment.md` | ✅ this file |
| Gate YAML snippet | ✅ above |
| CLI sessions cleaned up | N/A — no Playwright sessions used (backend stack) |

---

### Completion Summary

- **Overall NFR status for shipped code:** PASS ✅
- **Overall NFR status for "Epic 1 done" gate:** CONCERNS ⚠️ (six stories must still land)
- **Critical blockers in shipped code:** 0
- **Waivers needed for MVP:** 5 UNKNOWN-ACCEPTED items (NFR24, NFR25, NFR26, NFR28, NFR29) — each needs named approver + expiry per NFR6 before GA; for MVP, the named-waiver process from PRD NFR85 covers them.
- **Highest-leverage next move:** **Story 1.11** (data-integrity proof; unblocks NFR38/39/41/43 + MVP-required NFR31 1M-event rebuild).
- **Next recommended workflow:** Once stories 1.7, 1.10, 1.11 are dev-complete and tests are green, re-run **`bmad-testarch-nfr`** in **Edit (E) mode** to grade the UNKNOWN entries and re-evaluate the gate. Alternatively, run **`bmad-testarch-trace`** to build a traceability matrix between FRs/NFRs and the now-existing test suite.

**Generated:** 2026-05-19. **Workflow:** `bmad-testarch-nfr` (sequential execution mode).

---

## Refresh Log

### 2026-05-19 — Refresh pass (Edit mode) — Murat

**Trigger:** Working-tree progress on Story 1.6 P22/P24 patches and Story 1.11 advanced-elicitation hardening (commit `492055a`) since the original report was generated earlier today.

**Refresh scope:** Confirm working-tree state against the assessment's claims; fold in the 1.11 spec hardening; re-verify the test baseline; tighten evidence-row provenance for P22 and P24.

**Working-tree state confirmed (uncommitted on `main` at refresh time):**

- `src/Hexalith.Conversations/Idempotency/ConversationAuditHandle.cs` — NEW (P22). `FromServerBoundary(fingerprint, serverOperationId)` produces a SHA-256-derived `audit-<hex>` opaque handle from tenant ID + command type + scope kind + schema version + server operation ID.
- `src/Hexalith.Conversations/Idempotency/ConversationIdempotencyOutcome.cs` — `AuditHandle` property added; `NoOp` and `RetryableUncertainty` factories accept an optional `auditHandle`.
- `src/Hexalith.Conversations/Idempotency/ConversationIdempotencyRecord.cs` — `ToString()` now redacts `ScopeValue` and `Key` to literal `<redacted>`.
- `src/Hexalith.Conversations/Idempotency/ConversationIdempotencyReplayResult.cs` — `ResultPayload` now serializes only `Category`, `SchemaVersion`, `CommandType`, `ConversationId`, `MessageId`, `ParticipantPartyId`, `FileId`, `RejectionCode`, `IsRetryable`, `AuditHandle`. Caller `CorrelationId` and `TenantId` are dropped.
- `src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs` — NEW (P24). `ValidateEnvelope(object?)` accepts every public command type (`CreateConversationCommand`, `AppendMessageCommand`, `AddParticipantCommand`, `AttachFileReferenceCommand`, `UpdateConversationMetadataCommand`, `CloseConversationCommand`, `ArchiveConversationCommand`) and returns typed `ConversationRejectedDomainEvent` for `command_missing`, `metadata_missing`, `tenant_binding_missing`, `schema_version_missing`, `unsupported_schema_version`, `idempotency_key_missing`.
- `src/Hexalith.Conversations.Contracts/Errors/ConversationErrorCode.cs` — `IdempotencyKeyMissing` constant added to the closed vocabulary (with `KnownCodes` parse coverage and `IsRetryable` taxonomy unchanged).
- `src/Hexalith.Conversations.Server/CommandHandlers/AddParticipantCommandHandler.cs` — wired to the shared schema validation and the server-derived audit handle.
- `src/Hexalith.Conversations/Validation/AddParticipantValidation.cs` & `CreateConversationValidation.cs` — refactored to delegate envelope checks to the shared validator.
- `tests/Hexalith.Conversations.Tests/Validation/ConversationCommandSchemaValidationTest.cs` — NEW. 7-case `[Theory]` proves `IdempotencyKeyMissing` rejection for every public command type.
- `tests/Hexalith.Conversations.Server.Tests/Idempotency/AddParticipantCommandHandlerIdempotencyTest.cs` — `MissingIdempotencyKeyShouldRejectBeforeTenantAccessAndIdempotencyLookup` added; asserts tenant-access invocation = 0, idempotency reserve = 0, state-load = 0.
- `tests/Hexalith.Conversations.Server.Tests/Idempotency/IdempotentConversationCommandExecutorTest.cs` — `DuplicateReplayPayloadShouldExcludeCallerSuppliedCorrelationAndScope` added; asserts the audit-handle replacement and explicit absence of `correlationId` / caller secret / idempotency key / tenant value in the replay payload.
- `tests/Hexalith.Conversations.Tests/Idempotency/ConversationIdempotencyStoreTest.cs` — `SameIdempotencyKeyUnderDifferentTenantShouldNotReplayStoredOutcome` (P19) and `SameKeyUnderDifferentCommandTypeShouldNotCollide` (P20) added.
- `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` & `ContractValidationTest.cs` — extended to cover `IdempotencyKeyMissing` parse + classification.

**Story 1.11 spec hardening absorbed (commit `492055a`, 2026-05-19T17:27):**

The commit added AC 8 (side-channel non-disclosure) and new tasks for:

- Explicit validation precedence (metadata → tenant/conversation identity → schema-version → event-type → ordering/duplicate → payload). Earlier failures cannot upgrade derived state to trusted.
- Idempotency artifacts are NOT replay authority — they may explain command submission but cannot drive replay/rebuild correctness.
- Bounded poison/quarantine scope — tests must prove a poisoned/quarantined record cannot contaminate other tenants' projections or make unrelated conversations look stale/unavailable/missing.
- Side-channel equivalence tests for unauthorized, nonexistent, cross-tenant, tenant-hidden, unsupported-version, malformed, poisoned, quarantined cases. Public response shape, freshness/trust vocabulary, counts, cursors, timing-sensitive timestamps, and diagnostic-code classes must not distinguish existence/ownership.
- Fixed synthetic fixture identifiers only — local evidence may use abstract ordered cursors / projection versions but must not serialize raw EventStore stream names, storage offsets, subscription names, raw provider IDs, Party display data, or real tenant/user identifiers.
- "Validation precedence" explicit ordering before replay can trust state.

This hardening **strengthens but does not alter** the NFR38/39/41/42/43 verdicts — they remain UNKNOWN until Story 1.11 lands. When it does land, the hardened spec means it will also produce read-side NFR18/NFR19 side-channel-equivalence evidence as a side benefit.

**Test baseline re-verification (2026-05-19, refresh time):**

```
Hexalith.Conversations.Client.Tests        — 1/1 passed (26 ms)
Hexalith.Conversations.Contracts.Tests     — 77/77 passed (356 ms)
Hexalith.Conversations.IntegrationTests    — 8/8 passed (79 ms)
Hexalith.Conversations.Tests               — 75/75 passed (81 ms)
Hexalith.Conversations.Server.Tests        — 107/107 passed (82 ms)
Total                                       — 268/268 passed, 0 skipped, ~624 ms wall-clock
```

Counts identical to the original baseline because the P22/P24 tests were already counted there; the refresh confirms no regression and slightly faster total wall-clock (~624 ms vs ~660 ms).

**Verdicts after refresh — unchanged:**

- Shipped code (Stories 1.1–1.5 done + 1.6 local idempotency proof now incl. P22/P24): **PASS ✅**.
- "Epic 1 done" gate: **CONCERNS ⚠️** — six stories (1.6 close-out modulo DEF1 handler wiring, 1.7, 1.8, 1.9, 1.10, 1.11) must still land.
- Critical blockers in shipped code: **0**.
- Highest-leverage next move: still **Story 1.11**.

**Sections refreshed in this pass:**

- Frontmatter (added `step-e-refresh-state`, `refreshedAt`, `refreshNotes`; pinned commit `492055a` reference).
- Section 1.2 — Story 1.6 row qualified with "P22/P24 in working tree"; Story 1.11 row qualified with the hardening commit reference.
- Section 3.1 — Refresh re-run row added.
- Section 3.2 — NFR19 and NFR40 evidence rows enriched with P22 and P24 provenance.
- Section 5 (Evidence Gaps) — NFR38/39/41/43 + NFR42 entries enriched with the 1.11 hardening expectations.

**Sections deliberately NOT refreshed:**

- Gate YAML snippet — counts and categories unchanged.
- Section 4 domain scoring — unchanged (no new test surface beyond what was already counted).
- Section 4E aggregation / cross-domain risks — unchanged.
- Sprint status authoritative source still shows Story 1.6 as `in-progress` (P22/P24 are working-tree, not committed); Story 1.11 still `ready-for-dev`. No status flips.

<!-- Powered by BMAD-CORE™ -->

