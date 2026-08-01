---
title: "Sprint Change Proposal — Preserve Story 6.2 Projection Proof and Add a Successor-Proof Follow-Up"
project: "Conversations"
date: "2026-08-01"
status: "approved-and-applied"
changeScope: "moderate"
mode: "batch"
trigger: "Two Story 6.2 projection-proof conformance assertions failed at the Story 6.3 baseline because completed candidate-bound evidence was compared to a later HEAD; Story 6.3's EventStore promotion exposed a third manifestation of the same time-basis defect."
affectedStories: ["6.2 (history preserved; no status or evidence rewrite)", "6.3 (pause completion and consume successor proof)", "6.6 (consume the proof chain head)", "6.12 (new follow-up)"]
approval: "approved by Jerome on 2026-08-01"
appliedAuthority: "epic-6-authority-2026-08-01-v7 / conversations-architecture-2026-08-01-v7"
---

# Sprint Change Proposal — Preserve Story 6.2 Projection Proof and Add a Successor-Proof Follow-Up

## 1. Issue Summary

### Problem statement

Story 6.2 completed with `projection-read-store-population-proof-v2` bound to
umbrella candidate `856ee997cd35eb1d432fcb288a75a7b5bf3c5b58` and EventStore
gitlink `e645901928eed9759e28e1086f23dc96875c3ac3`. The proof's platform
bindings are correct for that candidate; for example, the recorded SHA-256 of
`EventStoreDomainServiceExtensions.cs` is
`a297324ab709ce3fbc744a47640c326ebca13001ed4d479132f74154b0f334b1`,
which is exactly the hash of that file at EventStore commit `e6459019`.

The conformance validator nevertheless treats the completed proof as a
perpetual assertion about the current checkout. It compares the recorded
EventStore commit to `HEAD:references/Hexalith.EventStore`, rejects every root
gitlink or production-source change after the proof candidate, and hashes bound
source files from the current worktree. That time basis is incompatible with an
immutable completed record: legitimate later work can only make the old proof
red, and the only way to make the current checks green is to rewrite historical
evidence repeatedly.

That is the exact failure mode this correction must remove. Story 6.2 remains
`done`; its story record, v2 JSON/Markdown proof, three xUnit result artifacts,
signed-v1 bindings, and final-record history remain unchanged. Current release
assurance is restored through a new successor proof owned by a new approved
follow-up story, not by rebinding Story 6.2 after completion.

### Discovery and reproduced evidence

At Story 6.3's baseline, `e480c3f3176cdc3d911baf91eb3e7a8cd38874aa`,
the root EventStore gitlink was `4843b492dff7c16a4bc74db67509263f969c78c6`.
The Story 6.2 proof still correctly recorded its own candidate's EventStore
gitlink, `e6459019`. Two assertions therefore failed:

1. `ProofShouldBindExactProductionRouteKeysAndBoundedOutcomes` compared the
   historical `e6459019` to the later root gitlink at `HEAD`.
2. `RecordedPromotionCandidateShouldStillDescribeTheCurrentGitlinks` prohibited
   every root gitlink move after `856ee997`, even an approved later story's
   movement.

Story 6.3 then legitimately advanced EventStore to `e92ae66866d68842c3551b9709df5e81eb05b08c`
for shared projection-rebuild capability. That commit changes
`EventStoreDomainServiceExtensions.cs`, whose current hash is
`02db884cc54c4f7d4e4166cb6a5c104077a4b05b2172442d5d54d44b0d1ad615`.
The current focused run consequently shows a third failing assertion,
`ProofSourceAndSignedV1BindingsShouldRemainByteIdentical`, because it hashes the
current submodule worktree instead of the blob at the proof's recorded gitlink.

Executed current result:

```text
Hexalith.Conversations.Conformance.Tests
class: ProjectionReadStorePopulationProofValidationTest
Total: 8, Failed: 3, Skipped: 0, Not Run: 0
```

The observed count is therefore consistent: two failures existed when Story
6.3 began; its subsequent platform advance exposed a third assertion governed
by the same incorrect historical-versus-current comparison.

## 2. Impact Analysis

### Epic impact

Epic 6 remains viable and no completed epic or story is reopened. The epic needs
one additive authority amendment and one new corrective story, 6.12. The
binding sequence changes only to place the repair after the final-record
generator and before Story 6.3 may complete:

```text
6.1 -> 6.7 -> 6.2 -> 6.8 -> 6.12 -> 6.3 completion
```

Existing parallel constraints remain: 6.9 still precedes 6.3 and 6.6; 6.10 and
6.11 keep their approved scopes; 6.6 remains last.

### Story impact

- **Story 6.2:** remains `done`. No acceptance criterion, status, final record,
  proof byte, or recorded test result is changed. Its v2 proof is explicitly
  classified as point-in-time evidence for its recorded candidate.
- **Story 6.3:** stays `in-progress` and may not return to `review` or `done`
  while the projection-proof class is red. Its manifest must distinguish the
  immutable v2 historical proof from the current successor proof and bind the
  successor-chain head.
- **Story 6.6:** must hash-validate the immutable v2 predecessor and consume and
  rerun the latest approved successor proof for current readiness. It may not
  cite v2 alone as proof for a later release candidate.
- **Story 6.12:** new corrective follow-up. It repairs the validator's time
  basis, creates the successor proof and its lifecycle decision, and restores
  current release assurance without rewriting Story 6.2.

### Artifact conflicts

- **PRD:** no conflict and no modification. FR-20's versioned manifest,
  release-gate, and governed-change requirements support an additive successor.
- **Epics/planning authority:** append a v7 authority block; do not edit the
  frozen epic prefix or v1-v6 overlay bytes.
- **Architecture:** append the evidence-lifecycle rule and add a new ADR for
  candidate-bound versus current-readiness proof semantics. ADR 0003 remains
  accepted and unchanged as Story 6.2's population-proof decision.
- **UX:** no impact. No interface, journey, wireframe, interaction,
  accessibility, FrontComposer, or Fluent UI behavior changes.
- **Story records/release evidence:** Story 6.2 and every v2 proof artifact stay
  byte-identical. New v3 artifacts are additive and predecessor-linked.
- **Tests:** the Story 6.2 validator must validate historical bindings from Git
  objects at the recorded candidate/gitlink. A separate current-chain guard
  validates the latest proof against current in-scope projection dependencies.
- **Sprint tracking:** add Story 6.12 as `backlog`; after approval move it
  through implementation normally. Reconcile Story 6.3's spec frontmatter from
  `in-review` to `in-progress` while the red gate remains.
- **CI/deployment/IaC:** no topology or deployment change. The focused and full
  conformance lanes change only by adding proof-lifecycle validation.

### Technical impact

The implementation is evidence and test infrastructure, not production
behavior. It needs Git-object-safe hashing for root blobs and submodule blobs,
an explicit proof-chain contract, generated v3 JSON/Markdown plus fresh
machine-readable run evidence, fault injection, and the Story 6.8 final-record
path for Story 6.12 itself.

The critical distinction is:

| Question | Correct time basis |
| --- | --- |
| Was Story 6.2's v2 proof truthful? | Its recorded umbrella candidate and recorded submodule gitlinks |
| Is the current release candidate projection-safe? | The latest approved proof-chain head and its declared in-scope dependency set |
| Did unrelated platform work occur later? | Disclosed by planning/final-record controls, but not treated as mutation of v2 |

## 3. Recommended Approach

**Selected path:** Direct Adjustment with an additive follow-up story.
**Scope:** Moderate. **Effort:** Medium. **Risk:** Medium.

Append a v7 authority amendment and create Story 6.12, **Version projection
proofs without rewriting completed history**. Story 6.12 will preserve and
candidate-validate v2, add a successor lifecycle ADR/contract, generate v3
against the post-Story-6.3 platform state, and make the current release gate
follow the chain head.

This is preferred because it satisfies all four relevant invariants at once:

1. completed Story 6.2 history remains immutable;
2. the old proof is still mechanically verified rather than waived;
3. current readiness is backed by freshly executed evidence rather than a
   backlog note alone; and
4. future legitimate changes trigger a named successor-proof obligation
   instead of inviting silent in-place rebinding.

### Alternatives considered

- **Refresh the v2 JSON/Markdown and Story 6.2 record to current hashes.**
  Rejected. This silently rewrites completed evidence and destroys the ability
  to answer what Story 6.2 actually proved at completion.
- **Change the three assertions to stop checking drift and accept v2 forever.**
  Rejected. Candidate-aware historical validation is necessary but not
  sufficient for current release assurance.
- **Reopen Story 6.2.** Rejected. The defect is in the lifecycle semantics of
  the validator and later-proof ownership, not in the truth of the candidate
  evidence Story 6.2 completed with.
- **Rollback Story 6.3's submodule promotions.** Rejected. It would only hide
  the worktree-hash manifestation; the two baseline failures already existed at
  `e480c3f`, and the invalid perpetual-HEAD rule would fail again on the next
  legitimate promotion.
- **MVP/PRD reduction.** Rejected. No product capability or release objective
  needs to move.

### Timeline and sequencing impact

Story 6.3 pauses completion, but its implementation does not need to be
discarded. Story 6.8 must first provide the required generated completion path;
then Story 6.12 lands the repair and successor proof; Story 6.3 regenerates its
manifest against that chain head and resumes review. Story 6.6 remains the final
release revalidation and consumes the latest chain head.

## 4. Detailed Change Proposals

### 4.1 Story 6.2 completed record and v2 evidence

Story: `6.2 Migrate Conversations to platform-owned hosting`
Section: status, completion record, projection-proof artifacts

OLD:

```text
Story status: done
projection-read-store-population-proof-v2: candidate-bound completed evidence
```

NEW:

```text
No byte or status change.
Planning authority explicitly classifies projection-read-store-population-proof-v2
as immutable point-in-time evidence for candidate 856ee997 and EventStore e6459019.
```

Rationale: the evidence is truthful for its declared candidate. Later root
gitlink movement is not retroactive evidence tampering.

Explicitly protected byte set:

- `_bmad-output/implementation-artifacts/6-2-migrate-conversations-to-platform-owned-hosting.md`
- `docs/release-evidence/projection-read-store-population-proof-v2.json`
- `docs/release-evidence/projection-read-store-population-proof-v2.md`
- `docs/release-evidence/projection-read-store-population-proof-v2-deterministic.xunit.xml`
- `docs/release-evidence/projection-read-store-population-proof-v2-gateway.xunit.xml`
- `docs/release-evidence/projection-read-store-population-proof-v2-population.xunit.xml`
- all immutable signed-v1 artifacts referenced by the proof

### 4.2 New Story 6.12

Story: `6.12 Version projection proofs without rewriting completed history`
Section: new story definition

OLD:

```text
No Story 6.12 exists.
```

NEW:

```markdown
### Story 6.12: Version projection proofs without rewriting completed history

As a release owner,
I want completed projection proofs validated at their recorded candidate and
current readiness represented by an explicit successor chain,
so that later approved platform work neither falsifies history nor inherits
stale assurance.

Acceptance criteria:

1. Story 6.2 remains done and the protected v2 byte set is unchanged. The v2
   validator reads root-owned blobs from umbrella candidate `856ee997...` and
   platform blobs from the root gitlinks recorded at that candidate; it proves
   every recorded hash, mode, gate result, and run binding at that time basis.
2. The perpetual-HEAD assertions are replaced. Historical validation never
   compares v2's recorded commit or hashes to the current worktree, and it does
   not prohibit later unrelated root gitlink or production-source movement.
3. ADR 0004 defines an immutable predecessor-linked projection-proof lifecycle:
   exact predecessor artifact hashes, one current chain head, exact changed
   dependency identities, named owner/rationale, and no in-place mutation.
4. `projection-read-store-population-proof-v3` is generated against the current
   candidate. It reruns the deterministic dispatcher, gateway/DAPR boundary,
   configured state-store end-state, query, deletion, and full-replay evidence;
   binds the current EventStore gitlink and all declared in-scope source/test
   blobs; and links to the unchanged v2 hashes.
5. The current-readiness guard follows the approved chain head and compares
   only declared projection-proof dependencies. In-scope drift without a
   successor fails with stable code `PROJECTION_PROOF_SUPERSESSION_REQUIRED`;
   unrelated root gitlink movement cannot invalidate historical proof.
6. Fault injection proves rejection of a changed v2 byte, wrong historical
   candidate/gitlink/blob, broken predecessor hash, duplicate or forked chain
   head, stale v3 binding, missing run, red/skipped/vacuous run, and undeclared
   in-scope drift. Every mutation is restored byte-identically.
7. Story 6.3 binds v2 as historical evidence and v3 as the current chain head.
   Story 6.6 consumes both, reruns v3's functional gates, and cannot cite v2
   alone for current readiness.
8. The focused projection-proof class, the Story 6.3 manifest validation class,
   and the full conformance project pass with zero failed/skipped/not-run tests;
   Story 6.12's final record is generated through Story 6.8.
```

Rationale: this is the smallest durable repair. It separates history validation
from current assurance and makes supersession additive and mechanical.

### 4.3 Projection proof validator

Artifact:
`tests/Hexalith.Conversations.Conformance.Tests/ProjectionReadStorePopulationProofValidationTest.cs`

OLD:

```csharp
promotion.GetProperty("commit").GetString()
    .ShouldBe(Git("rev-parse", "HEAD:references/Hexalith.EventStore"));

Git("diff", "--name-only", $"{candidate}..HEAD", "--", "references/")
    .ShouldBeEmpty("no root gitlink may move after the recorded promotion candidate");

ComputeSha256(fullPath)
    .ShouldBe(binding.GetProperty("sha256").GetString(), relativePath);
```

NEW:

```text
- Resolve the proof's recorded candidate.
- Read the EventStore gitlink and mode from that candidate's tree and compare
  them to the v2 promotion record.
- Hash root-owned bound blobs using `<candidate>:<path>`.
- Hash platform-bound blobs using `<recorded-submodule-commit>:<path>` in the
  declared root submodule.
- Keep current-worktree hashing only for the latest successor proof's declared
  current-dependency set.
- Replace the all-`references/` perpetual freeze with the versioned chain-head
  guard and stable successor-required diagnostic.
```

Rationale: the verifier must use the identity the evidence declares. A test
that silently substitutes `HEAD` is testing a different proposition.

### 4.4 Planning authority and architecture

Artifacts:

- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/epics.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/implementation-artifacts/epic-6-context.md`

OLD:

```text
Latest authority: v6.
Story 6.2 produces projection-read-store-population-proof-v2.
Story 6.6 consumes v2 for readiness.
No projection-proof evidence-lifecycle or successor-chain rule exists.
```

NEW:

```text
Append v7; do not edit v1-v6 bytes.

- Story 6.2 and v2 remain completed historical evidence.
- Story 6.12 owns candidate-aware historical validation and the additive v3
  successor.
- Story 6.3 records historical/current proof roles and binds the chain head.
- Story 6.6 validates the immutable predecessor and reruns the current head.
- ADR 0003 remains unchanged; ADR 0004 owns evidence lifecycle/supersession.
- Binding order adds `6.8 -> 6.12 -> 6.3 completion`; 6.6 remains last.
```

Rationale: an append-only authority amendment preserves every prior decision
while making the new ownership and completion gate explicit.

### 4.5 Story 6.3 status and acceptance

Artifact:
`_bmad-output/implementation-artifacts/spec-6-3-create-complete-preservation-traceability-manifest.md`

OLD:

```yaml
status: 'in-review'
```

NEW after proposal approval:

```yaml
status: 'in-progress'
```

Add to acceptance:

```text
Projection-population closure distinguishes immutable candidate-bound v2
history from the latest current-readiness successor. The manifest binds the
whole predecessor chain, identifies exactly one approved current head, and
fails if it treats historical evidence as proof for a later candidate.
```

Rationale: a story with a reproduced red release-gate class is not in review.
Its work is preserved; only its completion state and proof semantics are
corrected.

### 4.6 Sprint status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  6-11-make-cross-key-projection-validation-cheap-enough-to-re-gate-sm-c2: backlog
  epic-6-retrospective: optional
```

NEW after proposal approval:

```yaml
  6-11-make-cross-key-projection-validation-cheap-enough-to-re-gate-sm-c2: backlog
  6-12-version-projection-proofs-without-rewriting-completed-history: backlog
  epic-6-retrospective: optional
```

Append a dated log comment citing this approved proposal, the two baseline
failures, the third current manifestation, the protected v2 byte set, and the
`6.8 -> 6.12 -> 6.3 completion` sequence.

Rationale: the follow-up must be visible and approved in the backlog before any
validator or evidence byte changes.

## 5. Implementation Handoff

**Classification:** Moderate — backlog reorganization plus developer and
quality-review work.

**Recipients:** Product Owner / release owner, Developer, and Quality reviewer.

### Responsibilities

- **Jerome / release owner:** approve or revise this proposal and the new Story
  6.12 scope. Approval authorizes the additive v7 planning amendment and backlog
  entry; it does not pre-approve generated v3 evidence before review.
- **Product Owner:** publish Story 6.12 and the v7 overlay atomically across
  epics, architecture, Epic 6 context, Story 6.3 acceptance, and sprint status.
- **Developer:** implement candidate-aware Git-object validation, ADR 0004, the
  generated v3 successor proof, and focused fault-injection coverage. Do not
  modify any protected v2 or signed-v1 byte.
- **Quality reviewer:** independently verify the protected-byte hashes,
  candidate and submodule blob resolution, chain uniqueness, current in-scope
  drift behavior, unrelated-gitlink non-interference, executed run evidence,
  and full conformance result.
- **Story 6.3 owner:** regenerate the preservation manifest after Story 6.12 and
  bind the historical/current roles without hand-editing generated output.
- **Story 6.6 owner:** consume and rerun the latest proof-chain head while
  retaining v2 as an immutable predecessor.

### Success criteria

1. The protected Story 6.2/v2 byte set is byte-identical before and after the
   repair.
2. The v2 validator passes when the current checkout differs but its recorded
   candidate and gitlinks remain resolvable and hash-correct.
3. Corrupting any recorded v2 blob or identity makes historical validation red.
4. A generated v3 proof links to v2 by full hash and proves the current
   projection path with fresh passing machine-readable runs.
5. In-scope post-v3 drift fails with
   `PROJECTION_PROOF_SUPERSESSION_REQUIRED`; unrelated gitlink movement does
   not.
6. Story 6.3's manifest names exactly one current proof-chain head and does not
   represent v2 as current readiness.
7. Focused proof and manifest classes and full Conformance pass with zero
   failed, skipped, or not-run tests.
8. Story 6.12 reaches `done` only through the generated final-record gate; Story
   6.3 returns to review only afterwards.

## 6. Change Navigation Checklist

### 1 — Trigger and Context

- [x] 1.1 Triggering story identified: Story 6.3 exposed two Story 6.2
  projection-proof failures at baseline `e480c3f`.
- [x] 1.2 Core problem categorized: failed evidence-lifecycle approach. A
  completed candidate-bound proof is incorrectly validated as a perpetual HEAD
  snapshot.
- [x] 1.3 Evidence gathered: recorded/candidate/current gitlinks, candidate and
  current source hashes, exact failing assertions, current 8-test focused run,
  Git history, planning authority, Story 6.2 record, Story 6.3 spec, and sprint
  state.

### 2 — Epic Impact

- [x] 2.1 Epic 6 remains completable; no completed work needs rollback.
- [x] 2.2 Additive v7 amendment and Story 6.12 approved by Jerome and published.
- [x] 2.3 Remaining Epic 6 stories reviewed: 6.3 and 6.6 consume the correction;
  6.4, 6.5, 6.9, 6.10, and 6.11 retain their scopes.
- [x] 2.4 No epic is invalidated and no new epic is needed; one corrective story
  is sufficient.
- [x] 2.5 Sequence amended narrowly; 6.6 remains last.

### 3 — Artifact Conflict and Impact

- [x] 3.1 PRD checked; no goal, requirement, or MVP scope change.
- [x] 3.2 Architecture checked; ADR 0003 remains valid, while proof lifecycle
  requires additive ADR 0004 and a v7 architecture projection.
- [N/A] 3.3 UX checked; no UI/UX or accessibility impact.
- [x] 3.4 Tests, release evidence, story records, manifest generation,
  final-record generation, and sprint tracking impacts are specified. No
  deployment, IaC, or runtime source change is required.

### 4 — Path Forward

- [x] 4.1 Direct Adjustment viable — effort Medium, risk Medium.
- [x] 4.2 Rollback evaluated and rejected — it does not remove the baseline
  failures and would discard legitimate Story 6.3 work.
- [x] 4.3 MVP review evaluated and rejected — the product scope remains
  achievable.
- [x] 4.4 Direct Adjustment plus additive follow-up selected; rationale and
  alternatives are documented in section 3.

### 5 — Proposal Components

- [x] 5.1 Issue summary includes discovery context and reproduced evidence.
- [x] 5.2 Epic, story, artifact, and technical impacts are explicit.
- [x] 5.3 Recommended path includes trade-offs and rejected alternatives.
- [x] 5.4 MVP is unaffected; action plan and sequence are defined.
- [x] 5.5 Handoff owners, responsibilities, and success criteria are defined.

### 6 — Final Review and Handoff

- [x] 6.1 All applicable checklist sections are addressed and applied planning
  changes are recorded in section 7.
- [x] 6.2 Proposal checked against current Git identities, proof bindings,
  Story 6.2/6.3 state, v6 authority, and the focused executable result.
- [x] 6.3 Jerome approved the complete proposal on 2026-08-01 without revision.
- [x] 6.4 Story 6.12 and the dated sprint-status log entry are applied.
- [x] 6.5 Handoff roles, sequence, non-claims, and done criteria are unambiguous.

## 7. Approval, Application, and Handoff

Jerome approved this proposal without revision on 2026-08-01.

The approved planning correction is applied:

1. append-only authority `epic-6-authority-2026-08-01-v7` is published after
   the closed v6 marker;
2. architecture projects the decision as
   `conversations-architecture-2026-08-01-v7`;
3. Epic 6 developer context is regenerated to v7;
4. Story 6.12 is added to sprint status as `backlog`;
5. Story 6.3 is reconciled to `in-progress` and carries the v7 proof-chain
   acceptance; and
6. the planning-authority conformance guard binds the v7 append-only chain and
   Story 6.12's load-bearing prohibitions.

Validation executed after application:

| Check | Result |
| --- | --- |
| Conformance project Release build with local project references | passed, 0 warnings and 0 errors |
| `ArchitecturePlanningAuthorityValidationTest` | 17/17 passed, 0 failed/skipped/not-run |
| Working-tree whitespace check | passed |
| Story 6.2/v2 protected byte set | unchanged; no protected path appears in the diff |

The projection-proof implementation gate intentionally remains red pending
Story 6.12. This approval does not pre-approve a v3 proof and does not make a
backlog disposition stand in for executed evidence. The Developer handoff is:
author ADR 0004, implement candidate-aware historical verification, produce
and validate the additive v3 successor, then return Story 6.3 to review only
after the focused and full conformance gates pass.

No Story 6.2 record, v2 evidence, signed-v1 evidence, production source, public
contract, package, or submodule content was modified.
