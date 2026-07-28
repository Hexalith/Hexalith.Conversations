---
title: "Sprint Change Proposal — Close the Epic 5 Release-Owner Decision Ledger Entry"
project: "Conversations"
date: "2026-07-28"
status: "approved"
changeScope: "minor"
mode: "batch"
trigger: "bmad-correct-course invoked with the verbatim text of the open Epic 5 action item at sprint-status.yaml:223"
affectedArtifact: "_bmad-output/implementation-artifacts/sprint-status.yaml (one action-item status line plus one log comment)"
supersedesProposalScope: "none — closes the ledger entry for sprint-change-proposal-2026-07-14.md, which remains the authoritative decision record"
---

# Sprint Change Proposal — Close the Epic 5 Release-Owner Decision Ledger Entry

## 1. Issue Summary

### Problem statement

The requested action — *"Record the release-owner decision for
`success-metric-report-and-attestation-v1` without changing the
implementation-generated evidence"* — was already performed on 2026-07-14 and
its deliverables exist, are internally consistent, and are hash-pinned by live
conformance guards. The Epic 5 action item that tracks it is nevertheless still
`status: open` at `sprint-status.yaml:225`.

The defect is therefore in the ledger, not in the evidence. Nothing about the
release-owner decision needs to be recorded, re-recorded, or corrected. What
needs correcting is a tracking entry that says an obligation is outstanding when
it is not.

### How it was discovered

This `bmad-correct-course` run was invoked with the action item's text verbatim,
which is what an open ledger entry invites. Verifying the trigger before
proposing any edit surfaced the completed sidecar instead of a gap.

The same day's two sibling Epic 5 items were closed correctly — *"Make an
explicit OQ-2 decision for SM-1 and SM-2 target interpretation"* (`done`, via
`oq-2-target-interpretation-decision-v1`) and *"Create or approve an
architecture decision for projection read-store population proof versus
accepted deferral"* (`done`, via ADR-0003). This entry was missed in the same
pass.

### Evidence

All checks were executed in this session against the current working tree.

| Check | Command | Result |
| --- | --- | --- |
| Decision sidecar exists | `ls docs/release-evidence/` | `…-release-owner-decision.json` and `.md` present |
| Decision is signed | read of the JSON | `status: signed`, signer `Jerome`, `2026-07-14T12:17:38Z` |
| Bound source JSON unchanged | `sha256sum` | `062ca0c7…e0fe` — matches the bound value |
| Bound source Markdown unchanged | `sha256sum` | `aa7e52c1…a2cd` — matches the bound value |
| Bound source commit resolvable | `git cat-file -t c6670fac…` | `commit`, dated `Tue Jul 14 12:56:04 2026` |
| Implementation evidence still unsigned | read of the source pair | `status: ready-for-signature`, `releaseOwnerDecision: pending` |
| Approval reference exists | read of the proposal | `sprint-change-proposal-2026-07-14.md`, *Approved and implemented* |
| Decision pinned in test source | `SuccessMetricReportAndAttestationValidationTest.cs:45` | `ReleaseOwnerDecisionSha256 = 8091f6c2…4856` — matches disk |
| OQ-2 decision pinned in test source | same file, line 47 | `OqTwoDecisionSha256 = 06281924…3e06` — matches disk |
| Source commit pinned in test source | same file, line 39 | `SignedV1SourceCommit = c6670fac…` |
| Guards execute green | compiled xUnit v3 executable, `-class` filter, `-trx` | `Total: 16, Failed: 0, Skipped: 0, Not Run: 0` |
| Guards are non-vacuous | one-byte append to the decision JSON, re-run | `Failed: 2` — exactly the two binding assertions |
| Injection reversed cleanly | `git checkout --` + `sha256sum` + `git status` | back to `8091f6c2…4856`, working tree clean |

The fault injection is recorded because a hash comparison that has never been
observed failing is not proof of a binding. Under mutation,
`SignedReleaseOwnerDecisionShouldStillBindTheImmutableV1ReportAndSourceIdentity`
("The signed release-owner decision must remain byte-identical to the pinned
record") and
`HistoricalEvidenceBindingsShouldMatchAndRemainPointInTimeEvidence` both failed,
then both passed again after byte-identical restoration.

Environment note recorded rather than glossed: the first Release build of the
Conformance test project failed with two transient `CopyRefAssembly` file-copy
errors under `references/Hexalith.EventStore/src/Hexalith.EventStore.Client`,
caused by concurrent access rather than by source state. Two immediate re-runs
built with 0 warnings and 0 errors. The full solution suite was **not** run in
this session, and this proposal makes no claim about it: `sprint-status.yaml`
already records Story 6.2 as in-progress with conformance at 417/418 pending
evidence regeneration, which is unrelated to this ledger entry.

## 2. Impact Analysis

### Epic impact

None. Epic 5 is `done` and its three stories remain `done`. This proposal adds,
removes, renumbers, resequences and reprioritizes nothing.

### Story impact

None. Story 5.3 delivered a deliberately unsigned, signable artifact; that
remains true and is asserted by
`SuccessMetricReportAndAttestationValidationTest.cs:445`
(`releaseOwnerDecision` must read `pending`).

Story 6.6 (`6-6-revalidate-and-issue-superseding-attestation`, `backlog`) retains
sole ownership of the superseding v2 attestation and its own release-owner
decision. This proposal does not touch, pre-empt, or partially perform that
work.

### Artifact conflicts

- **PRD:** no change. No requirement, success metric, or MVP boundary moves.
- **Epics/stories:** no change.
- **Architecture:** no change. ADR-0003 already governs the v1→v2 boundary.
- **UX:** no change. No rendered surface is involved.
- **Implementation-generated evidence:** no change, by construction. The source
  pair is byte-identical before and after this run.
- **Release-owner evidence:** no change. The 2026-07-14 sidecar stands as
  written.
- **Sprint status:** one action-item `status` value and one appended log
  comment.

### Technical impact

No runtime, public-contract, package, AppHost, test, generated-output, or
submodule impact.

The one technical risk worth naming is the risk of the *rejected* path: issuing
a new or amended v1 decision record would change the bytes of
`…-release-owner-decision.json` and break three live guards —
`SuccessMetricReportAndAttestationValidationTest.cs:131`,
`OqTwoTargetInterpretationDecisionValidationTest.cs:24`, and
`ProjectionReadStorePopulationProofValidationTest.cs:268` — which is precisely
the drift-detection those guards exist to provide. The proposed path changes no
guarded byte.

## 3. Recommended Approach

**Selected path:** Direct Adjustment.
**Scope:** Minor. **Effort:** Low. **Risk:** Low.

Close the Epic 5 action item as `done` and record why, citing the delivered
artifacts, their bound hashes, the source commit, the pinning test constants,
and the Story 6.6 boundary for anything superseding.

Alternatives considered and rejected:

- **Issue a new v1 release-owner decision.** Rejected. There is nothing to
  decide that has not been decided; the bound evidence has not drifted, so the
  sidecar's own invalidation rule has not fired. Re-signing would break three
  hash-pinned guards and would encroach on Story 6.6.
- **Rollback.** Not applicable. No work needs undoing.
- **PRD MVP review.** Not applicable. This is a tracking correction with no
  scope, requirement, or goal implication.
- **Leave the entry open.** Rejected as the default. An entry that reads `open`
  against completed, guarded work misreports release state and will keep
  re-triggering correction runs like this one.

## 4. Detailed Change Proposal

### Sprint status ledger

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`
Section: `action_items`, Epic 5 entry at lines 222–225.

OLD:

```yaml
  - epic: 5
    action: "Record the release-owner decision for success-metric-report-and-attestation-v1 without changing the implementation-generated evidence."
    owner: "Release owner"
    status: open
```

NEW:

```yaml
  - epic: 5
    action: "Record the release-owner decision for success-metric-report-and-attestation-v1 without changing the implementation-generated evidence."
    owner: "Release owner"
    status: done
```

Rationale: the obligation was discharged on 2026-07-14 through
`sprint-change-proposal-2026-07-14.md`. Its deliverables exist, remain
byte-identical to their pinned values, and are enforced by executable guards
proven capable of failing.

### Sprint status log comment

Artifact: same file, chronological log block below `last_updated:`.

OLD:

```text
No log entry records that the Epic 5 release-owner decision action item was verified as already discharged.
```

NEW:

```text
# epic-5 action item "Record the release-owner decision for success-metric-report-and-attestation-v1
# ..." closed open -> done on 2026-07-28 through approved
# sprint-change-proposal-2026-07-28-release-owner-decision-ledger-closure.md (correct-course,
# verification only — NO evidence, code, test or submodule byte changed). [full text in the applied file]
```

Rationale: the ledger's convention is a dated narrative entry per state change.
The comment carries the verification so a future reader does not have to re-run
it.

### Explicitly unchanged

The following are named here so the boundary is auditable, and none of them is
modified by this proposal:

| Artifact | State |
| --- | --- |
| `success-metric-report-and-attestation-v1.json` | unchanged, `062ca0c7…e0fe` |
| `success-metric-report-and-attestation-v1.md` | unchanged, `aa7e52c1…a2cd` |
| `…-release-owner-decision.json` | unchanged, `8091f6c2…4856` |
| `…-release-owner-decision.md` | unchanged, `a73077c0…9e0b` |
| `oq-2-target-interpretation-decision-v1.json` | unchanged, `06281924…3e06` |
| Conformance test sources | unchanged |
| Root gitlinks / submodules | unchanged; none declared, none touched |

## 5. Implementation Handoff

**Classification:** Minor — direct implementation.
**Recipient:** Developer agent (this session), on Jerome's approval.

Responsibilities:

- **Release owner (Jerome):** approve the closure. Retains ownership of the
  2026-07-14 decision and of any future re-signing if bound evidence drifts.
- **Developer:** apply the two `sprint-status.yaml` edits and nothing else.
- **Story 6.6 owner:** unchanged obligation to revalidate and issue the
  superseding v2 attestation with its own release-owner decision. This closure
  does not discharge, weaken, or pre-approve that.

Success criteria:

1. `sprint-status.yaml:225` reads `status: done`.
2. A dated log comment records the verification and cites the bound hashes,
   source commit, pinning constants, and the Story 6.6 boundary.
3. `git status --porcelain` shows exactly one modified file: `sprint-status.yaml`.
4. All six evidence hashes listed in §4 are unchanged after the edit.
5. The two decision-pinning test classes still pass 16/16.

### What this closure does not claim

- It does not claim OQ-2 was resolved *inside* the v1 decision. The v1 record
  carries OQ-2 as `unconfirmed`; `oq-2-target-interpretation-decision-v1`
  resolved it prospectively on 2026-07-14 without rewriting history.
- It does not claim projection read-store population was proven *inside* the v1
  decision. The v1 record carries it as deferred; ADR-0003 explicitly refuses to
  carry that deferral forward as Epic 6 authority.
- It does not claim the residual `Conformance.Tests -> Server` coupling is
  resolved. That Epic 5 action item remains `open` and is out of scope here.
- It makes no platform-compliance, security-certification, or external-audit
  claim of any kind.

## 6. Change Navigation Checklist

### 1 — Trigger and Context

- [x] 1.1 Triggering item identified: Epic 5 action item, `sprint-status.yaml:222-225`, owner *Release owner*.
- [x] 1.2 Core problem defined and categorized: *misunderstanding of current state* — a tracking record contradicts delivered, guarded evidence.
- [x] 1.3 Evidence gathered: hashes, source commit, pinned test constants, 16/16 green run, and a reversed fault injection.

### 2 — Epic Impact

- [x] 2.1 Epic 5 remains `done` and completable as planned.
- [N/A] 2.2 No epic-level scope, criteria, addition, or removal.
- [x] 2.3 Remaining epics reviewed; Epic 6 unaffected, Story 6.6 boundary preserved.
- [N/A] 2.4 No epic invalidated; no new epic needed.
- [N/A] 2.5 No resequencing or reprioritization.

### 3 — Artifact Conflict and Impact

- [x] 3.1 PRD checked; no conflict, no change.
- [x] 3.2 Architecture checked; ADR-0003 already owns the v1→v2 boundary.
- [N/A] 3.3 UX checked; no interface, flow, or accessibility impact.
- [x] 3.4 Secondary artifacts checked; only the sprint-status ledger requires an edit. No CI, IaC, deployment, or test-strategy impact.

### 4 — Path Forward

- [x] 4.1 Direct Adjustment viable — effort Low, risk Low.
- [N/A] 4.2 Rollback not applicable; nothing to undo.
- [N/A] 4.3 MVP review not applicable; no scope or goal implication.
- [x] 4.4 Direct Adjustment selected; alternatives documented in §3 with rejection reasons.

### 5 — Proposal Components

- [x] 5.1 Issue summary written.
- [x] 5.2 Epic and artifact impacts documented, including the explicitly-unchanged set.
- [x] 5.3 Recommended path and rejected alternatives presented.
- [x] 5.4 MVP unaffected; action plan bounded to two ledger edits.
- [x] 5.5 Handoff plan defined with per-role responsibilities.

### 6 — Final Review and Handoff

- [x] 6.1 All applicable checklist sections addressed.
- [x] 6.2 Proposal checked for hash, line-reference, and scope consistency.
- [x] 6.3 Explicit approval from Jerome — granted 2026-07-28, unconditional.
- [x] 6.4 Sprint-status update defined; it changes an action-item status only, not epic or story topology.
- [x] 6.5 Handoff, success criteria, and non-claims defined.

## 7. Approval and Completion

Jerome approved this proposal unconditionally on 2026-07-28. Both
`sprint-status.yaml` edits from §4 are applied and verified.

Post-implementation verification:

| Success criterion | Result |
| --- | --- |
| 1. Action item reads `done` | Confirmed by YAML parse: `epic 5 \| status: done \| owner: Release owner` |
| 2. Dated log comment records the verification | Added below `last_updated:`, citing hashes, source commit, pinning constants, the executed guard run, the fault injection, and the Story 6.6 boundary |
| 3. Exactly one file modified | `git status --porcelain` → ` M sprint-status.yaml` plus this untracked proposal |
| 4. Six evidence hashes unchanged | `062ca0c7…e0fe`, `aa7e52c1…a2cd`, `8091f6c2…4856`, `a73077c0…9e0b`, `06281924…3e06` — all as bound |
| 5. Guards still green | `Total: 16, executed 16, passed 16, failed 0, error 0, skipped 0, not run 0` |

The ledger file parses as valid YAML. Four action items remain unfinished across
the whole ledger and are untouched by this closure — three Epic 5 items still
`open` (the `Conformance.Tests -> Server` coupling decision, promotion of the
Story 5.3 evidence-boundary validation pattern, and release-facing documentation
alignment) plus the Epic 3 mechanical final-record item, which stays
`in-progress` until Story 6.8 is done.

Nothing was staged, committed, or pushed. No submodule was initialized, updated,
or entered.
