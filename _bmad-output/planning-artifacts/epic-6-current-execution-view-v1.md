---
artifact: epic-6-current-execution-view-v1
generated: '2026-08-01'
generator_version: '1.0.0'
generation_command: 'python3 _bmad/scripts/generate_epic_6_current_execution_view.py'
source_epics: '_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md'
source_marker: 'EPIC-6-AUTHORITY-OVERLAY-AMENDMENT-V8:BEGIN'
overlay_version: 'epic-6-authority-2026-08-01-v8'
architecture_version: 'conversations-architecture-2026-08-01-v8'
status_source: '_bmad-output/implementation-artifacts/sprint-status.yaml'
source_epics_sha256: '37b85c3e6af62f8a5968480939783aa6bbb7558bebc61f57f4ebca1c44bd1908'
source_v8_block_sha256: '2b944155c2d893489a44feddcedfcc055d5c2df34020a465074b38f58bcbc353'
source_architecture_sha256: 'ced930531c6b0638dbf8253a0c766a146c66748f2f2ee13f64f4259ef9b667eb'
source_sprint_status_sha256: '3ef082f8b11a9eb9b33e11516e72ac4b7b43d0d817da7d9f86a532ffcc190ee1'
completed_story_6_2_record: '_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md'
completed_story_6_2_record_sha256: '1b87966f2b48d18c1f1d642e679febca26bbc591e8f270e6deb96393ea39034e'
completed_story_6_2_evidence:
  - path: 'docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json'
    sha256: 'd18d81286e171f4df1330677bc236ecdc388764ff58a0ed4be32111fdba31b76'
  - path: 'docs/release-evidence/projection-read-store-population-proof-v2.json'
    sha256: 'b4bcdb5b181be66780f251ad8a3b563b7554e34e0fb4d255d46a3a17addfaf7c'
  - path: 'docs/release-evidence/sm-c2-hot-path-baseline-v1.json'
    sha256: '4cb4e66744b26ead0a79a218aed662005f14007e8e103dd019836761569118ea'
  - path: 'docs/release-evidence/sm-c2-hot-path-post-v1.json'
    sha256: '5ce140bc0586e0dfc2acc6ab948624b88e47eb753decf6accf7dc3ead13ef3ef'
status: 'authority-correction-only-not-ready'
---

# Epic 6 Current Execution View

> **AUTHORITY CORRECTION ONLY — NOT READY.** This file is a deterministic,
> non-amending projection of the active v8 block. It does not authorize any
> remaining Epic 6 implementation. Work may start or resume only after
> mechanical v8 validation passes and a separate independent implementation-
> readiness assessment returns `READY`.

The append-only v8 block is authority; this projection exists to give an
implementer one complete, topologically ordered view. The source marker,
versions, hashes, generator identity, and status source above are validated.
Hand editing or semantic drift is a conformance failure.

## Completed Story 6.2 Retrospective Checkpoints

| Checkpoint | Boundary | Historical result | Immutable bindings |
| --- | --- | --- | --- |
| 6.2-H1 Baseline and authority | Frozen inventory, benchmark, ownership, and promotion declarations | Preserved from the immutable completed record. | 6.2-R, 6.2-E1, 6.2-E3 |
| 6.2-H2 Runtime and projection migration | Test-only hosting, platform surfaces, population path, and correctness lanes | Preserved from the immutable completed record. | 6.2-R, 6.2-E2 |
| 6.2-H3 Candidate evidence and closure | Candidate binding, generated record, promotion gate, and historical SM-C2 disposition | Preserved from the immutable completed record. | 6.2-R, 6.2-E2, 6.2-E4 |

### Immutable completed-history bindings

- **6.2-R:** [`_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md`](../implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md) — `sha256:1b87966f2b48d18c1f1d642e679febca26bbc591e8f270e6deb96393ea39034e`
- **6.2-E1:** [`docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json`](../../docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json) — `sha256:d18d81286e171f4df1330677bc236ecdc388764ff58a0ed4be32111fdba31b76`
- **6.2-E2:** [`docs/release-evidence/projection-read-store-population-proof-v2.json`](../../docs/release-evidence/projection-read-store-population-proof-v2.json) — `sha256:b4bcdb5b181be66780f251ad8a3b563b7554e34e0fb4d255d46a3a17addfaf7c`
- **6.2-E3:** [`docs/release-evidence/sm-c2-hot-path-baseline-v1.json`](../../docs/release-evidence/sm-c2-hot-path-baseline-v1.json) — `sha256:4cb4e66744b26ead0a79a218aed662005f14007e8e103dd019836761569118ea`
- **6.2-E4:** [`docs/release-evidence/sm-c2-hot-path-post-v1.json`](../../docs/release-evidence/sm-c2-hot-path-post-v1.json) — `sha256:5ce140bc0586e0dfc2acc6ab948624b88e47eb753decf6accf7dc3ead13ef3ef`

These checkpoints are navigation aids only. They are not new work items,
independent completion claims, or permission to rewrite/re-evaluate Story 6.2.

### Current Story Dispositions

| Story | Status | Current authority disposition |
| --- | --- | --- |
| 6.1 | done | Completed history; preserve record and evidence unchanged. |
| 6.2 | done | Completed history; preserve record, historical SM-C2 disposition, and evidence unchanged. |
| 6.3 | in-progress | Paused; resume only after readiness `READY`; 6.9, 6.10, and 6.12 gate completion. |
| 6.4 | backlog | Preservation-governance work; no product UI activation; 6.8 gates completion. |
| 6.5 | backlog | Three ordered checkpoints; 6.2 gates start and 6.8/6.10 gate completion. |
| 6.6 | backlog | Last; preserves independent assessment result and cannot use the v6 SM-C2 exception. |
| 6.7 | done | Completed history; preserve record and evidence unchanged. |
| 6.8 | in-progress | Paused; mechanical final-record owner for every later completion. |
| 6.9 | backlog | Oracle-tiering authority; gates 6.10 and contributes to 6.3/6.6. |
| 6.10 | backlog | Evidence-boundary helper; independent of 6.12; gates 6.3/6.5/6.6. |
| 6.11 | backlog | Universal four-row SM-C2 restoration; mandatory before 6.6. |
| 6.12 | ready-for-dev | Non-startable until readiness `READY` and 6.8 is done; gates 6.3/6.6. |

## Complete Effective Story Definitions

### Story 6.1: Rebaseline architecture and planning authority

**Status:** `done` — read-only completed history.

As a platform architect, I want architecture and epic authority reconciled to
the finalized PRD, so corrective implementation starts from one ownership and
decision model.

**Effective acceptance criteria (historical):**

1. Architecture distinguishes 20 initiative FRs from 104 Feature-FRs, 77
   Feature-NFRs, 52 UX decisions, and every UX acceptance criterion; preserves
   the accepted 13,289-LOC baseline; and defers only FR-16.
2. FR-10 through FR-15 have verified public platform landing zones; OQ-1
   through OQ-5 each have one resolved row; the canonical host pair is
   `AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()`.
3. A nonempty versioned hot-path inventory is frozen before baseline capture
   and records the PRD rule `post P95 <= 1.05 x baseline P95` under an identical
   reproducible envelope.
4. The module owns no reusable production AppHost, Aspire, ServiceDefaults, or
   equivalent runtime capability. The retained Conversations AppHost is only a
   non-packable, non-publishable local user/E2E test harness; platform deployment
   owns production composition.
5. The append-only authority preserves completed history, the full
   preservation denominator, the promotion-completion invariant, and signed v1
   evidence byte-for-byte.

**Direct dependencies:** none. The completed record remains authoritative and
is not changed by v8.

### Story 6.2: Migrate Conversations to platform-owned hosting

**Status:** `done` — read-only completed history.

As a Conversations maintainer, I want Conversations composed through public
platform capability while retaining only a module test harness, so the domain
module contains no reusable platform-owned hosting boilerplate.

**Effective acceptance criteria (historical):**

1. The frozen SM-C2 baseline and candidate evidence were captured under the
   same versioned envelope. The v6 approved-cost/disclosure disposition is
   preserved as Story 6.2 completion context only and does not govern current
   release readiness.
2. `Hexalith.Conversations.AppHost` and its tests remain mechanically
   non-packable and non-publishable, limited to Conversations surfaces plus
   required platform dependencies, and never become production deployment
   composition.
3. Generic ServiceDefaults, Aspire, DAPR, publication, health, telemetry,
   projection/query, and subscription capability lives on approved public
   platform surfaces; Story 6.7 validated every promotion in scope.
4. The canonical named `IAsyncDomainProjectionHandler` route reuses the domain
   materializer and durably writes both tenant-scoped per-conversation and
   tenant-index read models through the shared write policy and store.
5. Immutable `projection-read-store-population-proof-v2` evidence binds the
   accepted append/replay path through production named dispatch, actual
   integration state-store end state, and production query results without
   calling the writer directly.
6. Focused integration evidence covers duplicate delivery, partial-write
   retry, tenant isolation, bounded failure, derived-state deletion, and full
   replay equivalence; DI resolution, mock calls, legacy projection output, and
   HTTP acceptance alone are insufficient.
7. Completion used the mechanical final-record path. The record, v2 proof,
   bound xUnit results, accepted baselines, and signed-v1 dependencies remain
   byte-identical.

**Direct dependencies:** completed Stories 6.1 and 6.7. No v8 work item may
reopen or re-evaluate this completed story.

### Story 6.3: Create the complete preservation traceability manifest

**Status:** `in-progress`, but paused by the global readiness hold.

As a release owner, I want a frozen, versioned preservation manifest with
complete requirement dispositions, so preservation claims are exact and
resistant to denominator drift.

**Effective acceptance criteria:**

1. The manifest covers all 20 initiative FRs, 104 Feature-FRs, 77 Feature-NFRs,
   52 UX decisions, every UX acceptance-criterion identifier, current controls,
   and preserved public contracts with zero gaps or duplicates.
2. Every obligation has evidence or named-owner approved non-activation with
   rationale; delivered-to-inactive and compatible changes include approval and
   compatibility evidence.
3. Source/build/test/baseline hashes, versioned mutation governance, and
   module/platform control separation are recorded and mechanically validated.
4. The manifest binds `conformance-oracle-tiering-decision-v2`, records every
   assertion's tier, and treats the portable tier's resolved-compile-surface
   test as evidence rather than author prose.
5. Projection proof is represented as an immutable predecessor chain: v2 is
   historical evidence, the Story 6.12 successor is the one approved current
   head, and historical evidence cannot stand in for a later candidate.
6. Completion binds v8, the exact UX preservation disposition identity, the
   current proof head, and a Story 6.8-generated final record at one compatible
   candidate.

**Direct dependencies:** Stories 6.9, 6.10, and 6.12 before completion; Story
6.8 governs the final record.

### Story 6.4: Repair UX provenance and preservation governance

**Status:** `backlog` and non-startable under the global readiness hold.

As a UX governance owner, I want the UX specification treated as a
preservation reference with reliable evidence mappings, so it constrains
behavior without silently authorizing UI delivery.

**Effective acceptance criteria:**

1. UX planning cites the canonical PRD and addendum and opens with a prominent
   preservation-only/non-activation banner. Historical Phase 0-3 language is
   labeled as future activation sequence, not the active Epic 6 plan.
2. The story produces exactly
   `docs/release-evidence/ux-preservation-disposition-v1.schema.json`,
   `docs/release-evidence/ux-preservation-disposition-v1.json`,
   `docs/release-evidence/ux-preservation-disposition-v1.md` as its
   deterministic projection, plus `UxPreservationDispositionValidationTest`
   in the conformance test project.
3. The JSON binds canonical source paths, versions, and hashes; inventories
   UX-DR1-52 and every UX acceptance-criterion identifier exactly once; and
   records `preserved-not-activated`, owner, rationale, evidence/control or
   explicit non-activation, historical provenance, compatibility, and
   disclosure-safety obligations for every item.
4. Historical story mappings remain labeled non-current provenance and cannot
   become implementation ownership. No inactive UX item points to a nonexistent
   current story.
5. Validation fails on missing, duplicate, unknown, unowned, unhashed,
   source-drifted, JSON/Markdown-drifted, reordered-without-regeneration, or
   activated-without-authority entries.
6. No production UI change or preserved-scope activation is authorized.

**Direct dependencies:** Story 6.1 for start and Story 6.8 for completion.

### Story 6.5: Correct the thin authoring template and reproduce SM-2

**Status:** `backlog` and non-startable under the global readiness hold.

As a domain-module author, I want a platform-hosted thin template with
reproducible authoring-cost evidence, so SM-2 measures only code a domain module
owns.

**Effective acceptance criteria:**

1. The template contains one non-packable, non-publishable module test AppHost
   for local user/E2E tests and no reusable module-owned Aspire library,
   ServiceDefaults facade, DAPR implementation, projection/query runtime,
   publication, health, telemetry, or subscription plumbing.
2. Checkpoint 6.5-A publishes corrected thin-module authoring guidance with
   ownership and prohibited-capability rules, versioned validation, and an
   explicit reviewer decision.
3. Checkpoint 6.5-B publishes a reproducible non-packable/non-publishable
   minimal fixture using live public platform APIs, with clean build/tests and
   an exact source inventory.
4. Checkpoint 6.5-C generates versioned SM-2 v2 evidence from frozen inclusion
   rules and the preserved baseline, including source paths, commands/tool
   versions, candidate identity, file/LOC evidence, confidence, and named
   acceptance.
5. The accepted 13,289-LOC SM-1 baseline remains unchanged; validators reject
   prohibited target ownership, vacuous evidence, and JSON/Markdown drift.
6. All three checkpoints pass at one compatible candidate. A checkpoint alone
   cannot complete the story.

**Direct dependencies:** Story 6.2 for start; Stories 6.8 and 6.10 before
completion.

### Story 6.6: Revalidate and issue superseding attestation

**Status:** `backlog`, last, and non-startable under the global readiness hold.

As a release owner, I want the corrected implementation independently
revalidated against the complete preservation contract, so a release decision
rests on current evidence rather than a prescribed verdict.

**Effective acceptance criteria:**

1. Every frozen row—HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN—has one usable,
   comparable candidate result and satisfies
   `post P95 <= 1.05 x baseline P95` under the identical reproducible envelope.
   The v6 ceiling/disclosure exception is not a current pass option.
2. The complete manifest passes; public contracts are equal or carry approved
   compatible-change evidence; topology, security, health, publication, admin
   composition, SM-1, reproducible SM-2, SM-3, and every preservation gate are
   evidenced.
3. The v2 attestation and supersession record preserve signed v1 evidence,
   consume accepted ADR 0003, bind the unchanged projection-proof predecessor
   plus its single approved current head, and rerun the head's functional gates.
4. Both conformance tiers run and are reported separately and summed; Story 6.8
   records for every predecessor and Story 6.10 evidence-boundary validation
   are current, non-vacuous, and green.
5. A fresh independent implementation-readiness assessment runs against the
   exact committed candidate and current authority/evidence identities. Its
   complete actual result is published unchanged; the assessor is not
   instructed or modified to return a particular verdict.
6. Release closure is a separate decision and remains blocked unless the
   preserved assessment result is `READY`. `NOT READY` or an incomplete
   assessment leaves Story 6.6 and Epic 6 open.

**Direct dependencies:** completion of Stories 6.3, 6.4, 6.5, 6.8, 6.9,
6.10, 6.11, and 6.12. This story always runs last.

### Story 6.7: Mechanically block incomplete submodule promotions from completion

**Status:** `done` — read-only completed history.

As a Hexalith development-workflow maintainer, I want promotion-bearing work to
pass a mechanical submodule completion gate, so dirty submodules and uncaptured
umbrella gitlinks cannot reach `done`.

**Effective acceptance criteria (historical):**

1. Promotion-bearing work declares exact root `references/...` paths and
   availability policy; affected scope also includes gitlinks changed since the
   baseline.
2. Each affected submodule is initialized, clean including untracked files,
   satisfies its availability policy, and is represented by the exact raw
   mode-`160000` gitlink in the committed umbrella revision.
3. Stable blockers prevent review/completion; unrelated state warns without
   blocking; an empty or unevaluated scope cannot report a pass.
4. Discovery uses root `.gitmodules` only and never initializes or traverses
   nested submodules; isolated fixtures prove success, failure, displacement,
   and concurrency cases.

**Direct dependency:** completed Story 6.1. The completed record is not changed
by v8.

### Story 6.8: Generate the final story record mechanically from measured state

**Status:** `in-progress`, but paused by the global readiness hold.

As a workflow maintainer, I want final story records generated from measured
repository state, so completion facts cannot drift through hand-authored prose.

**Effective acceptance criteria:**

1. One generator emits a versioned document-and-Markdown bundle whose fields
   derive from machine-readable test results, the committed candidate path set,
   raw root gitlinks, and the embedded Story 6.7 promotion result. Counts,
   paths, and commits are not caller-authored.
2. The root `.slnx` defines required root-owned test projects; a missing, red,
   stale, skipped-without-exact-policy, or not-run result blocks. Totals are
   computed, not transcribed.
3. The file list is singular and exact. Source-tree dirt is blocked outside
   record outputs and declared TRX inputs; paths inside root submodules block
   and gitlink promotions appear only in their labeled section.
4. Candidate, test binary, submodule, and gitlink identities bind the final
   committed state. After the candidate only record-output paths may change and
   no gitlink may move.
5. All completion surfaces generate the same bundle, verify its inserted
   Markdown digest, and let blockers prevent `review` and `done`.
6. A pass requires nonempty derived scope and executed assertions; workflow
   invocation removal or displacement fails.
7. Read-only historical mode verifies closed records without mutating them or
   pretending to reconstruct an uncommitted former worktree.
8. Fault injection proves every guard can fail and restores every mutated
   fixture byte-identically.

**Direct dependency:** completed Story 6.2. Story 6.8 governs the final record
for every later completion.

### Story 6.9: Tier the conformance oracle and make the portable tier structural

**Status:** `backlog` and non-startable under the global readiness hold.

As a test-governance owner, I want the conformance oracle split by legitimate
binding, so consumer-portable assertions stay portable without weakening
module-internal checks.

**Effective acceptance criteria:**

1. Every conformance file binding a Server namespace is triaged into a
   versioned record: re-expressed against public Contracts, Client, or Testing
   surfaces at unchanged strength, or assigned to the module-internal tier with
   exact type and reason. Public contract widening is unavailable.
2. The portable tier has no non-packable module reference, proven from the
   resolved compile surface rather than project text.
3. No manifested test is removed, skipped, renamed away, or weakened; the
   executed total across both tiers is monotonic from a machine-readable
   pre-split result.
4. Reclassification of the three manifested denominator suites records named
   owner approval, rationale, and a versioned manifest update; FR-20 membership
   is unchanged.
5. A v2 disposition artifact supersedes v1 without editing v1.
6. Every tier is present in the solution and declared to the Story 6.8
   generator, so neither can be silently unrun.

**Direct dependency:** Story 6.1. Completion unlocks Story 6.10 and contributes
to Stories 6.3 and 6.6.

### Story 6.10: Consolidate the evidence-boundary validation pattern

**Status:** `backlog` and non-startable under the global readiness hold.

As a release-evidence maintainer, I want evidence validation consolidated
behind one enforced, non-shipping helper, so evidence cannot pass through
trusted declarations, incomplete diffs, unavailable history, or vacuous
assertions.

**Effective acceptance criteria:**

1. A non-packable `Hexalith.Conversations.TestSupport` project supplies
   `RepositoryLocator`, `GitFacts`, `EvidenceManifest`, `BoundaryAssertions`,
   and `AssertionLedger`, references no Conversations assembly, and does not
   alter either oracle tier's membership.
2. Its Git runner has bounded execution, concurrent stdout/stderr draining,
   explicit UTF-8 decoding, `core.quotepath=false`, unavailable-history
   handling, revision/diff resolution, raw modes, and historical blob hashing.
3. Manifest integrity is recomputed: repository-relative contained paths,
   existing files, canonical lowercase SHA-256, rejected generated/build
   output, recomputed signable payload, and a nonempty assertion ledger.
   Supersession allowlists cannot cover signed evidence.
4. Changed-file validation uses exact set equality. Gitlinks are detected from
   raw mode `160000`, never substring matching.
5. Unavailable history is an explicit skip, never a pass; zero executed
   assertions fail; roots of trust remain pinned in consuming test source.
6. `_bmad/scripts/verify_evidence_boundary.py` enforces blocker codes
   `EVIDENCE_HELPER_NOT_USED`, `ADHOC_GIT_RUNNER`,
   `ADHOC_REPOSITORY_ROOT`, `ADHOC_HASH_HELPER`,
   `EVIDENCE_ARTIFACT_UNVALIDATED`, `EXEMPTION_EXPIRED`,
   `SCOPE_NOT_EVALUATED`, and `BASELINE_NOT_PROVIDED`, while retaining warnings
   `EXEMPTION_ACTIVE` and `EVIDENCE_TEST_OUTSIDE_CONFORMANCE`.
7. The gate is mandatory in the five governed workflow bodies in both active
   agent trees and both generated quick-dev render twins; mirrored bodies stay
   equivalent and dev-story definition-of-done/checklist forbid ad-hoc
   equivalents.
8. All 24 approved baseline evidence readers plus any reader added before
   implementation migrate with zero day-one exemptions, unchanged assertion
   strength, pinned constants, and preserved counts. Projection-proof adoption
   does not absorb or weaken Story 6.12.
9. The runbook documents invariants, authoring, exemptions, and limitations;
   fault injection covers hashes, escaping paths, generated evidence, gitlinks,
   subset comparison, signed allowlisting, unavailable Git, removed workflow
   calls, and malformed authority markers.
10. Story 6.7's inherited gate-span coupling is repaired so adding the evidence
    gate cannot leave a displaced positive guard green.

**Direct dependencies:** Stories 6.8 and 6.9. Completion is required by Stories
6.3, 6.5, and 6.6. Story 6.10 is independent of Story 6.12.

### Story 6.11: Restore the universal SM-C2 gate without weakening projection correctness

**Status:** `backlog` and non-startable under the global readiness hold.

As a release owner, I want all frozen hot paths to have usable comparable
signal and remain within the PRD regression budget, so current readiness uses
one performance rule without weakening fail-closed behavior.

**Effective acceptance criteria:**

1. Before production implementation, an ADR defines per-conversation
   index-entry key families, derived-state ownership, write ordering,
   compatibility transition, rebuild/backfill, deletion, expiry, and rollback;
   EventStore remains the only write authority.
2. HP-LIST/HP-OPEN validation removes unnecessary full-index or per-row fan-out
   only where an explicit proof permits it. Missing, duplicate, stale,
   advanced, malformed, misfiled, pending, or inconsistent state remains fail
   closed and reads never repair durable state.
3. Tenant isolation, retries/idempotency, delayed/out-of-order delivery,
   equal-position conflict, deletion, replay, and interrupted rebuild remain
   deterministic and non-disclosing across every derived key family.
4. Public query contracts, filtering, ordering, cursors, freshness vocabulary,
   forbidden/nonexistent indistinguishability, and response shapes remain
   unchanged.
5. A versioned measurement-method decision fixes repetitions, raw-sample
   retention, warm/cold classification, environment controls, and a predeclared
   signal-quality rule for all four rows; it cannot change the PRD threshold or
   discard adverse samples after observation.
6. HP-CREATE and HP-APPEND obtain usable comparable signal under the same
   frozen envelope; missing or unusable signal fails.
7. HP-LIST and HP-OPEN use the preserved Story 6.2 baseline fixture and satisfy
   the universal gate with every correctness test green; performance work may
   not weaken or reclassify correctness.
8. Unit, integration, and real DAPR state-store lanes fault-inject partial
   writes, latency, unavailable stores, poison records, retries, concurrency,
   tenant collisions, and replay.
9. One candidate-bound additive evidence set records every baseline/candidate
   raw sample, environment fact, calculation, signal verdict, and exact
   code/test identity for all four rows; JSON is authoritative and Markdown is
   deterministic.
10. Story 6.11 reaches `done` only when every frozen row satisfies
    `post P95 <= 1.05 x baseline P95` and every correctness gate is green. Any
    miss, unusable signal, red/skip/not-run/vacuous test, or stale binding keeps
    the story incomplete and release closure blocked.

**Direct dependency:** completed Story 6.2. Story 6.11 is independent of
Stories 6.10 and 6.12 and is mandatory before Story 6.6.

### Story 6.12: Version projection proofs without rewriting completed history

**Status:** `ready-for-dev`, but non-startable under the global readiness hold
and its existing Story 6.8 entry gate.

As a release owner, I want completed projection proofs validated at their
recorded candidate and current readiness represented by an explicit successor
chain, so approved later work neither falsifies history nor inherits stale
assurance.

**Effective acceptance criteria:**

1. Story 6.2 remains `done`; its record, v2 JSON/Markdown, three bound xUnit
   results, generated final record, and signed-v1 dependencies remain
   byte-identical. Historical validation reads root and submodule blobs from
   the recorded candidate/gitlinks and proves every bound hash, mode, gate, and
   run identity at that time basis.
2. Historical validation does not compare v2 to the current worktree or forbid
   later unrelated movement; mutation or unresolvable recorded Git objects
   still fail.
3. ADR 0004 defines an immutable predecessor-linked lifecycle with full
   predecessor hashes, exactly one approved current head, exact changed
   dependencies, named owner/rationale, and no in-place evidence mutation.
4. Generated `projection-read-store-population-proof-v3` reruns deterministic
   dispatch, gateway/DAPR, configured state-store, production query, deletion,
   and replay evidence against the current candidate and links unchanged v2.
5. The current guard compares only declared proof dependencies; undeclared
   in-scope drift fails `PROJECTION_PROOF_SUPERSESSION_REQUIRED`, while
   unrelated gitlink movement does not invalidate history.
6. Fault injection rejects changed v2 bytes, wrong historical identities,
   broken predecessor hashes, duplicate/forked heads, stale v3, missing/red/
   skipped/vacuous runs, and undeclared drift, restoring fixtures exactly.
7. Story 6.3 binds v2 as history and v3 as current; Story 6.6 consumes both and
   reruns v3. V2 alone cannot prove current readiness.
8. Focused proof, manifest, and full Conformance lanes pass without failed,
   skipped, or not-run tests; Story 6.8 generates the final record.

**Internal checkpoints:**

| Checkpoint | Criteria | Review and rollback boundary |
| --- | --- | --- |
| 6.12-A Historical validity and lifecycle contract | AC1-AC3 | Protected-byte inventory, candidate-aware historical validation, ADR 0004, and a closed successor-chain schema; no v3 current-head claim. |
| 6.12-B Successor generation and current guard | AC4-AC5 | Deterministic v3 projection, fresh functional lanes, exact approval, one current head, and drift guard; may be discarded without changing v2 history. |
| 6.12-C Fault injection, manifest handoff, and closure | AC6-AC8 | Mutation matrix, Story 6.3/6.6 handoff, full conformance, and Story 6.8-generated final record. |

Checkpoint success does not advance the story to `done`; all eight criteria
must pass at one compatible final candidate.

**Direct dependency:** Story 6.8. Story 6.12 is independent of Stories 6.10
and 6.11 and precedes completion of Stories 6.3 and 6.6.

### Topological Dependency Plan

| Gate or wave | Work | Entry condition | Completion unlocks |
| --- | --- | --- | --- |
| Authority Gate | Publish and validate comprehensive v8; then rerun readiness separately | Approved comprehensive correction | Remaining work only if the independent result is `READY` |
| Completed spine | 6.1 -> 6.7 -> 6.2 | Immutable historical fact | Existing prerequisites satisfied |
| Wave 1 | Resume 6.8; execute 6.4, 6.5-A/B, 6.9, and 6.11 | Readiness `READY` plus local prerequisites | 6.8/6.9 unlock 6.10; 6.8 unlocks 6.12 |
| Wave 2 | 6.10 and 6.12 in parallel; finish 6.5-C when its gates permit | Direct predecessors done | Completion paths for 6.3/6.5/6.6 |
| Wave 3 | Complete 6.3, 6.4, and 6.5 | Exact dependencies and evidence pass | Capstone eligibility |
| Wave 4 | 6.6 only | Every predecessor done and universal SM-C2 green | Independent assessment and possible Epic 6 closure |

Direct dependency edges:

```text
6.1 -> 6.7
6.7 -> 6.2
6.2 -> 6.8
6.1 -> 6.4
6.2 -> 6.5
6.8 -> completion of 6.4
6.8 -> completion of 6.5
6.1 -> 6.9
6.8 -> 6.10
6.9 -> 6.10
6.8 -> 6.12
6.9 -> completion of 6.3
6.10 -> completion of 6.3
6.12 -> completion of 6.3
6.10 -> completion of 6.5
6.2 -> 6.11
6.3 -> 6.6
6.4 -> 6.6
6.5 -> 6.6
6.8 -> 6.6
6.9 -> 6.6
6.10 -> 6.6
6.11 -> 6.6
6.12 -> 6.6
```

The graph is acyclic. Stories 6.10, 6.11, and 6.12 are mutually independent
after their stated predecessors, although each must preserve compatible edits
on shared validation surfaces.

### High-Risk BDD Scenario Catalogue

```gherkin
Scenario: Cross-tenant derived key is presented during an authorized read
  Given tenant A is authorized and an otherwise valid record is stored under tenant B's key
  When the list or detail query validates the derived state
  Then the query fails closed without disclosing tenant B existence or content

Scenario: Evidence content changes after candidate binding
  Given a generated evidence artifact is bound by path, mode, hash, candidate, and test binary
  When any bound byte or identity changes
  Then validation fails with a stable blocker and no stale evidence is reused

Scenario: Historical proof is valid but the current dependency set drifted
  Given v2 validates at its recorded candidate and an approved current head exists
  When an in-scope current dependency changes without an approved successor
  Then current readiness fails with PROJECTION_PROOF_SUPERSESSION_REQUIRED
  And historical v2 validity remains unchanged

Scenario: A required test is skipped or the assertion ledger is empty
  Given an evidence lane is required by a story completion gate
  When the result is skipped, not run, missing, stale, or records zero executed assertions
  Then the gate fails and cannot be reported as not-applicable or passing

Scenario: A frozen SM-C2 row has unusable signal or exceeds the threshold
  Given its baseline and candidate use the frozen identical envelope
  When signal quality is unusable or post P95 exceeds 1.05 times baseline P95
  Then Story 6.11 remains incomplete and Story 6.6 cannot close

Scenario: Readiness returns NOT READY
  Given Story 6.6 executes an independent assessment and preserves the complete report
  When the result is NOT READY
  Then the report remains unchanged and release closure stays blocked
```

## Completion Gate

Authority validation proves only that the v8 planning set is complete,
append-only, internally consistent, acyclic, metric-consistent, UX-preservation
safe, and projection-equivalent. It does not implement any story and does not
run or predetermine the separate implementation-readiness assessment.
