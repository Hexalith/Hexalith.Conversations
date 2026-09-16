# V15 Authority Reconciliation

**Target:** `_bmad-output/planning-artifacts/architecture.md`, proposed V15 overlay  
**Authority inputs:** V13/V14 architecture rules; V15-V21 planning-authority JSON; `implementation-hold-v1.json`; Story 7.1 successor publisher; protected planning preflight  
**Evaluated repository:** `fd3dd58c3c10b512c79cc425f94d57c4d3400b20`  
**Date:** 2026-09-16  
**Mode:** reconciliation only; `architecture.md` was not edited

## Verdict

**FAIL — safe fail-closed state, but not a complete or publishable authority reconciliation.**

The proposed V15 overlay preserves every immutable V1-V14 byte and records the correct V14 block digest, V21 raw digest, V9 bundle digest, evaluated commit, checker blocker, and effective `ACTIVE` state. Its fail-closed interpretation is therefore safe.

It does not, however, satisfy the authority machinery it inherits. V21 was published in an earlier one-file commit, while V13 requires a new sidecar head and its pointer-bearing architecture amendment in the same commit. The proposed V15 append would adopt that already-published head after the fact while repeating the same atomic-publication rule. The protected workflow also has no marker-driven resolver and its pinned V21 verifier rejects every post-V21 gitlink change, every architecture amendment, and every verifier/successor-authority change. Consequently the overlay cannot truthfully claim that current-authority discovery and the Story 7.1 terminal transition are closed.

The only safe current execution answer remains:

> **Story 7.1 effective hold is `ACTIVE` at `fd3dd58c3c10b512c79cc425f94d57c4d3400b20`; V21's raw `LIFTED` field is publication-time history, not current authorization. Story 7.2 remains locked.**

## Reconciliation Matrix

| Check | Result | Evidence |
| --- | --- | --- |
| Append-only preservation | **PASS** | The worktree diff adds 286 lines after the V14 END marker and changes no prior line. `git diff --check` passes. |
| V14 predecessor binding in the V15 BEGIN marker | **PASS** | Exact V14 block is 3,873 bytes with SHA-256 `d33d977fda0776377684439bb7e78769a6b9a0279c293b8a08e44dfad8466dc5`, matching V15. |
| Selected V21 raw digest | **PASS** | Current V21 SHA-256 is `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`, matching both V15 markers and the current-state table. |
| V9 bundle raw digest | **PASS** | Current bundle SHA-256 is `8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3`, matching V15. |
| V15-V21 predecessor bytes | **PASS** | V16→V15, V17/V18 preserved-authority pins, V19→V18, V20→V19, and V21→V20 all match the checked-in raw SHA-256 values. |
| Evaluated commit/current result | **PASS** | The protected V21 effective-hold command returns `FAIL`, blocker `V21_DESCENDANT_GITLINK_DRIFT`, effective hold `ACTIVE`, exit 1 at the stated HEAD. |
| Fail-closed state semantics | **PASS** | `effective_hold()` maps missing history/artifacts and V21 validation exceptions to `ACTIVE`; the CLI exits nonzero on `FAIL`/`BLOCKED`; the workflow runs it under `set -euo pipefail`. |
| V13 atomic pointer publication | **FAIL** | V21 was added alone in commit `d297f96dc39b04008f391b404021899431585b6d`; the proposed V15 pointer is later and uncommitted. This is precisely the publication failure V13 defines. |
| Marker-driven current resolver in CI | **FAIL** | The workflow and publisher hard-code `V21_PATH`; neither parses the last complete architecture marker nor verifies its selected sidecar digest. |
| Point-in-time/currentness qualification | **PARTIAL** | V15 prose correctly qualifies V15-V21 externally, but the selected V21 document still exposes only raw `authorityEffect.implementationHold: LIFTED` and lacks `statusAsOf`, `recordedEffectAtCandidate`, and `effectiveStateAtEvaluatedHead`. |
| Story 7.1 terminal transition | **FAIL** | AD-4 states requirements for a future successor, but no successor authority/checker consumes the accepted-commit final record, retires V21's temporary restriction, or unlocks 7.2. |
| Compatibility with V13 operational-envelope gate | **FAIL** | No operational-envelope artifact exists, yet V17/V20/V21 recorded scoped hold lifts after V13 required a decision/defer/delegation before *any* hold-lift decision. V15 narrows the future gate without explicitly superseding or remediating that prior rule. |

## Findings

### RA-01 — Critical — The V15 pointer cannot retroactively satisfy V13's atomic-publication rule

V13 says that each new checkpoint-level sidecar authority and its pointer amendment are published in the same commit and that publishing either artifact alone is an authority-publication failure. V15 repeats that rule almost verbatim.

V21's actual publication commit, `d297f96dc39b04008f391b404021899431585b6d`, added only `_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json`. No architecture pointer landed with it. The proposed V15 diff adds only an architecture overlay after that publication. Its bytes and selected digest are correct, but chronology is part of the authority contract; digest equality cannot repair the missing atomic transaction.

V15 therefore cannot both:

1. claim it fixes current-authority discovery by selecting V21, and
2. retain the inherited/repeated rule that the selected new head and pointer must have been published together.

**Required correction:** either mint a true successor authority (for example, V22) and publish it atomically with the pointer-bearing architecture amendment, or explicitly define a one-time recovery/adoption transition that supersedes V13's publication rule for V15-V21 and explains why that exception is trustworthy. The first route is materially stronger. Do not describe V21's original publication as conforming.

### RA-02 — Critical — The protected V21 gate cannot admit the successor V15 says is required

The current publisher permits post-V21 changes only in `STORY_DESCENDANT_PATHS`: five Story 7.1 implementation files, five result XML files, and two final-record outputs. It rejects every descendant gitlink change. Neither `architecture.md`, the protected workflow, the publisher, nor a new successor-authority JSON path is permitted.

The current repository has already crossed that frozen boundary. Re-running the protected effective-hold check reports gitlink drift across EventStore, Folders, and FrontComposer commits through HEAD and exits 1. The workflow invokes both `v21 --check` and `v21 --effective-hold --check` under `set -euo pipefail`, using the protected-source copy of the same fixed V21 verifier.

This creates a transition deadlock:

- an architecture-only V15 publication remains a post-V21 unauthorized path and cannot make the preflight green;
- changing the workflow or verifier to understand a V22 successor is itself outside the V21 descendant allowlist;
- adding a V22 authority is also outside that allowlist; and
- rebinding current gitlinks is impossible because V21 rejects all descendant gitlink changes.

**Required correction:** define and implement a protected, release-owner-authorized bootstrap transaction for the successor. It must bind the exact merge candidate, exact allowed authority/tooling/pointer paths, exact root gitlinks, and source workflow identity, and remain fail-closed on any mismatch. Until that route exists, V15 is a design proposal, not a publishable `FINAL` authority.

### RA-03 — High — The total resolver exists only in prose

AD-3 says the last complete architecture marker selects one exact sidecar head, its digest is verified, and the selected sidecar's checker determines live execution state. The current workflow does not do that. It hard-codes `_bmad-output/planning-artifacts/v21-story-7.1-authority-correction-v1.json` and the `v21` CLI route. The current V9/V14 validators extract frozen overlay blocks but do not walk the final marker to a selected current head.

The result happens to agree with V15 today because both prose and code name V21. The next pointer change can fork them again, which is the exact divergence AD-3 says it prevents.

**Required correction:** add one marker-driven resolver used by CI and direct conformance tests. It must validate complete BEGIN/END pairing, predecessor block bytes/digest, sidecar path/digest, candidate binding, selected checker/tool identity, and fail-closed result semantics. The workflow should call that resolver rather than a fixed V21 route.

### RA-04 — High — Story 7.1's terminal transition is specified but not closed

AD-4 is directionally correct: it binds the protected merge candidate, post-integration re-evaluation, accepted-commit final record, retirement of V21's descendant restriction, and continued Story 7.2 lock. Those are the missing decisions identified by validation.

No committed mechanism implements those decisions:

- V21 validates an historical direct-child tooling/publication topology, not an exact protected-branch merge result;
- the workflow never consumes a candidate-matched final record as a terminal acceptance transition;
- no machine authority retires or replaces `STORY_DESCENDANT_PATHS` and the absolute descendant-gitlink ban;
- no authority unlocks Story 7.2 after accepted integration; and
- the V15 marker retains Epic V14 as the canonical epic authority, while AD-4 adds story-level entry/acceptance/successor rules without a paired epic/story-contract amendment.

The Deferred table itself concedes that a current-head successor is still required before Story 7.1 resumes. The scope sentence should therefore not say the transition is closed.

**Required correction:** publish the successor mechanism and paired epic/story authority, or change the V15 claim to “defines the requirements for closing the Story 7.1 transition” and keep the item explicitly open. `FINAL` is premature if “closed” remains the asserted outcome.

### RA-05 — High — The operational-envelope rule is narrowed without explicit supersession or historical remediation

V13 requires the release owner to decide the operational envelope, defer it with a named owner, or delegate it to a named counterpart artifact **before any hold-lift decision**. The repository still has no `_bmad-output/planning-artifacts/production-operational-envelope-v1.md`.

V17 subsequently lifted `7.1-SCHEMAS`; V20 and V21 lifted Story 7.1 at their publication candidates. V15 now makes the operational envelope a hard gate for Story 16.1 and product/runtime/release hold lifts, and labels the earlier planning-only lifts historical. That is a sensible prospective policy, but it neither says the V13 “any hold-lift” clause is superseded nor records how the earlier nonconforming decisions were remediated.

Because V15 otherwise inherits every invariant it does not explicitly supersede, both rules remain live and conflict.

**Required correction:** explicitly supersede the V13 open-dimension timing sentence with the narrower rule and classify V17/V20/V21 as nonconforming point-in-time decisions that confer no present authority, or obtain a release-owner remediation accepting a named late disposition and its consequences. Do not imply the late architecture wording makes those earlier publications conforming when made.

### RA-06 — Medium — External qualification prevents unsafe use but does not meet V13's sidecar-shape requirement

The V15 current-state table and AD-3 correctly say that raw `authorityEffect` fields are point-in-time and that every nonvalidated current result is `ACTIVE`. This is a worthwhile fail-closed qualification and should remain.

V13 also requires regenerated point-in-time sidecars to carry `statusAsOf`. V15-V21 carry no such field; V21 also lacks the proposed split between `recordedEffectAtCandidate` and `effectiveStateAtEvaluatedHead`. V15 can qualify immutable history without rewriting it, but selecting V21 as the current head preserves the unsafe document shape for direct consumers.

**Required correction:** the atomic successor should carry `statusAsOf`, the recorded candidate/effect, the evaluated commit/state, and the exact checker/tool binding as separate fields. Keep V15-V21 immutable and label them historical inputs.

## Preserved Integrity And Correct Semantics

The following parts of V15 are reconciled and should be retained:

- V1-V14 remain byte-identical; V13 and V14 recompute to their recorded sizes and SHA-256 values.
- The V15 BEGIN marker has the full V13-required attribute shape, and the END marker repeats the required selected values.
- V21, V20, V19, V18, V17, V16, V15, the hold record, and the V9 bundle have the raw digests expected by their successors.
- V15 correctly separates the immutable V9 bundle from live execution state.
- V15 correctly states that V21's `LIFTED` value is historical at its candidate and that current HEAD evaluates to `ACTIVE`.
- V15 does not broaden a lift, start a story, mark Story 7.1 done, unlock Story 7.2, authorize a release, or authorize a push.
- AD-4's intended post-integration checks and AD-5's prospective operational ownership are sound architectural directions once their authority/publication gaps are closed.

## Reproduced Checks

| Command/check | Result |
| --- | --- |
| `git diff --check -- _bmad-output/planning-artifacts/architecture.md` | Exit 0; no whitespace defect. |
| Independent byte walk of architecture V13-V15 blocks | V13: 17,857 bytes / `c7d5c867…4605a`; V14: 3,873 bytes / `d33d977f…6dc5`; V15 is complete and terminal with trailing LF. |
| `sha256sum` over V15-V21, hold record, and V9 bundle | All recorded predecessor/selection digests match current bytes. |
| `uv run --frozen --no-sync python3 _bmad/scripts/publish_story_7_1_successor_authorities.py --repository . v21 --candidate HEAD --effective-hold --check` | Exit 1; `FAIL`; `V21_DESCENDANT_GITLINK_DRIFT`; effective hold `ACTIVE`. |
| `git show --stat d297f96dc39b04008f391b404021899431585b6d` | V21 publication changed exactly one file: the V21 JSON; no pointer amendment. |
| Static preflight/publisher reconciliation | Workflow is fixed to V21; current resolver does not parse the architecture marker; post-V21 allowlist excludes architecture/successor/tooling paths and all gitlink changes. |

## Handoff

Do not finalize or publish V15 as written. Keep its fail-closed current-state wording and verified digest bindings, but resolve RA-01 and RA-02 before calling the authority chain current, resolve RA-04 before claiming Story 7.1 has a terminal transition, and make the V13 operational-envelope supersession/remediation explicit. The next safe authority publication is an atomic successor transaction with a protected marker-driven resolver; until then, the only executable state is Story 7.1 hold `ACTIVE`.
