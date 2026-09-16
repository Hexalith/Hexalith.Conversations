<!-- validation-run: 2026-09-16 final-post-editorial-rerun -->
# Adversarial PRD Review — Conversations Boilerplate Reduction

**Review date:** 2026-09-16

**Lens:** fail-closed performance, preservation-denominator integrity, authority provenance, scope/readiness contradictions, and regression against prior Critical/High findings

**Reviewed:** `prd.md`, `addendum.md`, and the cited current evidence artifacts

## Final rerun context and evidence boundary

This final rerun independently inspected the current working-tree PRD and addendum after editorial polish and the repair of their addendum section links. It followed `docs/runbooks/evidence-boundary-validation.md`, compared the documents with the tracked versions, and checked the cited baseline, performance, preservation-manifest, architecture, and hold records. No PRD, addendum, authority, signed evidence, or implementation file was changed by this reviewer.

The reviewed bytes are bound by independently recomputed SHA-256 values:

| Artifact | SHA-256 |
|---|---|
| `prd.md` | `1eea231c4c951eb6218a1b8912e52d41ff35fbd39f82ffd08d72dbffd78bb84c` |
| `addendum.md` | `fc35b13a12a42f3e46edf55b4194f6c6aa09a8b33ee2dc7eb3d18c36f09e660f` |
| `docs/release-evidence/consume-promote-keep-inventory-v1.json` | `20bbedb5d0aa1dcd35729aac6a8500f7cf75d8f0c0719b8e1364da5e19fa22b7` |

The inventory digest matches the addendum's declared source hash. The draft preservation manifest instead binds PRD hash `884981cefea501e5d6636b8f797581487a5d83cc65f8f4ef53879f3484a140f8`; comparison with the current digest confirms the disclosed stale binding. No successor approval is inferred from recomputing these hashes.

`python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline HEAD --candidate HEAD` exited 0 with `PASS`, a nonempty 23-entry assertion ledger, and zero blockers. Its committed baseline and candidate both resolve to `8c6248ec416666fdd4de0d1a54d450569ed63a4b`; `changedPaths` is empty and the dirty paths are reported separately. **This result checks committed-HEAD governance and reports the worktree; it does not mechanically validate the uncommitted PRD update's exact publication boundary, signatures, or release acceptance.** The historical `7.1-SCHEMAS` hold decision in its output does not override the current PRD's ACTIVE implementation hold. No new signable payload was created or certified. Candidate publication checks and fault-fixture execution are not claimed by this document review.

The post/baseline JSON P95 ratios were independently recalculated: HP-CREATE −50.14% passes, HP-APPEND +20.01% fails, HP-LIST +329.85% fails, and HP-OPEN +1760.88% fails under the universal ≤5% rule. Existing release-evidence files, architecture, and sprint-status files have no working-tree diff in this review.

## Verdict

**PASS FOR THE VALIDATION-DRIVEN PRD UPDATE; NOT A RELEASE OR HOLD-LIFT APPROVAL.** The updated package now states the requested dispositions without a remaining Critical, High, Medium, or Low document defect: SM-C2 is universal at no more than 5% P95 regression for every frozen hot path; the current result is failed; FR-20/SM-C1 remains pending; the seven categories and the complete 14-suite/214-test v1 denominator are immutable floors; and the implementation hold remains active. Missing approvals, authority chains, activation records, and evidence remain explicitly pending or nonconforming rather than inferred.

**Severity counts:** 0 Critical · 0 High · 0 Medium · 0 Low

This verdict does not say that the pilot, FR-20/SM-C1, SM-C2, SM-1, SM-2, FR-10–FR-15, §14 activation, release, or successor work is accepted. It says the PRD now represents their unresolved or failed state truthfully and fail-closed.

## Critical findings

None.

## High findings

None.

## Medium findings

None.

## Low findings

None.

## Final editorial triage

- **Resolved before verdict:** renamed addendum §§D/E headings initially left obsolete deep links in the PRD mapping tables. The current links at `prd.md:146,215-217` match the current headings. No finding remains open.
- **No semantic weakening found:** the rewritten purpose, readiness snapshot, Phase 3 status, FR-20 pending paragraph, and §14 disposition paragraph preserve their mandatory consequences. The §14 process/accessibility exclusions already existed in the tracked PRD and do not authorize removal of any frozen denominator test. Feature-NFR9 retains its 500 ms threshold and the same workload envelope after notation cleanup.
- **Retained evidence blockers, not new document defects:** failed SM-C2, the inconsistent benchmark fixture digest, the draft/stale preservation manifest, pending named approvals, and ACTIVE hold remain visible. Editorial acceptance neither repairs those artifacts nor grants their missing authority.

## Adversarial falsification checks

### Universal SM-C2 and current failure — PASS

- The readiness table calls the gate **FAILED** and applies the ≤5% rule to every identified command/read hot path; it explicitly rejects approved-cost ceilings and recorded-but-ungated rows as waivers (`prd.md:29`).
- SM-C2 freezes the nonempty four-row inventory—HP-CREATE, HP-APPEND, HP-LIST, HP-OPEN—and reports HP-APPEND +20.01%, HP-LIST +329.85%, and HP-OPEN +1760.88% as failures (`prd.md:362`). It also says an evidence-local pass label cannot replace the universal threshold.
- OQ-5 repeats that new comparable evidence must pass every row and that no evidence-local amended rule or waiver is inferred (`prd.md:401`).
- This treatment is adversarially necessary: the cited JSON still contains the weaker conditional comparison rule (`docs/release-evidence/sm-c2-hot-path-post-v1.json:29`), labels HP-LIST and HP-OPEN as passes against cost ceilings (`:151-159`, `:195-203`), and reports overall `result: pass` (`:206-208`). The PRD does not adopt those labels. Under the JSON's own universal `publishedRuleResult`, three of four rows fail (`:108-115`, `:151-159`, `:195-203`).
- The evidence also has a provenance mismatch: the Markdown sidecar reports fixture SHA-256 `4838a5a1…` (`docs/release-evidence/sm-c2-hot-path-post-v1.md:5-8`), while the authoritative JSON reports `1a43bacc…` (`docs/release-evidence/sm-c2-hot-path-post-v1.json:8-11`). The PRD records this as an additional blocker and does not invent a preferred value or repair (`prd.md:362`).

No wording elsewhere in the PRD creates a per-path exception, correctness-cost waiver, recorded-only escape, empty-inventory escape, or substitute absolute target for SM-C2.

### Active implementation hold — PASS

- The hold is **ACTIVE**, and failed SM-C2 plus pending FR-20/SM-C1 each independently block hold lift, pilot acceptance, release, and successor authorization (`prd.md:31`). The historical execution snapshot is expressly not acceptance (`prd.md:32`, `:108-110`).
- The current hold evidence says `Implementation hold: ACTIVE`, `Release authorized: false`, and `Independent decision required: true` (`docs/release-evidence/epic-6-completion-supersession-current-proof-v1.md:3-8`); it says the artifact cannot itself lift the hold or authorize a successor/release (`:18-20`). Current sprint status also says the global hold remains active (`_bmad-output/implementation-artifacts/sprint-status.yaml:39-42`).
- The PRD records no lift, waiver, approver, or substitute authority (`prd.md:31`).

No acceptance, execution, architecture, or historical-evidence sentence elsewhere overrides this disposition.

### FR-20/SM-C1 exact-test preservation floor — PASS

- The PRD treats `release-baseline-v1.json` as an immutable input and explicitly says its 14-suite/214-test list is not, by itself, proof of all seven categories (`prd.md:28`). The evidence confirms exactly 14 suites and 214 suite-class tests (`docs/release-evidence/release-baseline-v1.json:15-34`).
- FR-20 is **PENDING** until one approved, hash-bound manifest retains every v1 denominator test and maps all seven categories to exact fully qualified test IDs, requirement IDs, source/build identity, hashes, approval evidence, and zero orphaned active obligations (`prd.md:333-335`).
- The current manifest is correctly rejected as acceptance evidence: it self-identifies as `2.0.0-draft` and `pending-prerequisites` (`docs/release-evidence/preservation-traceability-manifest-v2.json:1-6`), and its bound PRD hash is stale relative to this update (`:44-47`; acknowledged at `prd.md:344,434`).
- The denominator cannot be shrunk or swapped. Every original v1 test remains; additions accumulate; removals, replacement, reclassification, aggregation, merge, waiver, or substitution cannot establish acceptance (`prd.md:340-343`, `:346`, `:361`). The seven categories are an immutable floor, while future versions may only add categories or exact tests (`prd.md:343`, `:346`).
- Plumbing-only deletion language cannot reach the frozen denominator: only a deleted test not in the frozen manifest can use the justification ledger (`prd.md:343`).

No path remains to pass FR-20/SM-C1 by preserving only suite counts, omitting projection freshness or governance audit-pairing, renaming categories, replacing original tests, or approving a smaller denominator.

### Authority and waiver provenance — PASS

- Cross-repository work is explicitly not authorized by this PRD and requires a real versioned repository-specific grant before acceptance (`prd.md:87`, `:374`).
- FR-10–FR-15 remain nonconforming until actual owning-repository approval, implementation/release identity, compatibility evidence, and rollback authority exist (`prd.md:30`, `:397`, `:407`; `addendum.md:17-21`).
- Story 3.7's unconsumed FR-16 platform work is explicitly out of scope/nonconforming, excluded from pilot acceptance and metrics, and not retroactively authorized (`prd.md:95`, `:283-287`; `addendum.md:109-115`).
- SM-1 uses a frozen value but remains pending exact-hash named ratification because the inventory merely self-records `accepted` and contains no acceptor (`prd.md:16`, `:118-123`, `:353`, `:408`; `addendum.md:7-11`).
- Generic role labels are declared responsibilities, not proof of a named person's authority; gates stay pending until a versioned authority record exists (`prd.md:425`). Historical names and waiver concepts in §14 are explicitly non-activating (`prd.md:434`, `:749-763`).

No new approver, owner, waiver, signer, or authority record is invented by the update.

## Prior Critical/High finding disposition

| Prior finding | Current disposition | Exact evidence |
|---|---|---|
| C-1 — SM-C2 rule/evidence contradiction | **Fail-closed resolved** | Universal rule and failed current result at `prd.md:29,362,401`; evidence-local alternate rule expressly rejected. |
| C-2 — Seven-category claim not proven by 14-suite baseline | **Fail-closed resolved** | FR-20/SM-C1 pending at `prd.md:28,333-345,361`; draft manifest not accepted. |
| H-1 — Final/executed readiness drift | **Resolved** | Frontmatter is `status: draft` (`prd.md:1-5`); execution is historical work performed, not acceptance (`:32,108-110`). |
| H-2 — OQ-1 open after implementation | **Truthfully nonconforming** | Technical mapping is recorded, but acceptance is pending the actual authority/release/compatibility/rollback chain (`prd.md:30,397,407`; `addendum.md:17-21`). |
| H-3 — FR-16 scope breach | **Truthfully nonconforming** | Story 3.7 work is out of scope, earns no pilot credit, and requires separate authority (`prd.md:95,283-287`; `addendum.md:115`). |
| H-4 — FR-18/FR-19/SM-2 called complete | **Fail-closed resolved** | UJ-3, FR-18, FR-19, SM-2, and Phase 3 proof are pending (`prd.md:59,110,305-323,354`). |
| H-5 — SM-1 denominator lacked named acceptance | **Fail-closed resolved** | Frozen denominator value is separated from pending named acceptance and exact-hash ratification (`prd.md:16,118-123,353,408`; `addendum.md:9`). |
| H-6 — §14 lacked safe activation state | **Fail-closed resolved** | Machine-safe activation remains pending; no downstream inference is permitted without an approved hash-current state/activation manifest (`prd.md:409,434`). |
| H-7 — Cross-repository authority asserted without grants | **Fail-closed resolved** | The PRD grants no authority and requires repository-specific grants (`prd.md:87,374,410`). |

## Collateral contradiction check

No collateral Critical/High contradiction was found after remediation. In particular:

- `status: draft` is consistent with the unresolved gates.
- FR-16's normative deferral now coexists honestly with the recorded Story 3.7 breach by labeling the work out-of-scope/nonconforming rather than pretending it did not occur.
- The reconstructed SM-C2 baseline's later root commit is explicitly explained as the pre-change point for the Epic 6 hosting migration (`prd.md:337`), closing the former commit-binding ambiguity.
- The SM-C2 Markdown/JSON fixture-hash mismatch is disclosed as an unresolved evidence blocker rather than silently reconciled (`prd.md:362`).
- The superseded inventory is machine-marked and hash-points to the sole frozen denominator artifact (`addendum.md:3-5`), while the addendum distinguishes the frozen value from pending acceptance (`:7-11`).
- The no-synchronous-cross-service-call invariant remains pending until one closed verifier is authorized; it cannot contribute a passing gate (`prd.md:367,411`).
- External/customer-facing feature delivery remains out of scope, while external/adopter and tenant-visible behavior remains inside the preservation blast radius (`prd.md:37,417`).

## Gate recommendation

The PRD update may proceed to human conflict resolution, but **must remain draft and held**. The first remaining real decision conflict is OQ-1: supply the actual FR-10–FR-15 owning-repository approval/release/compatibility/rollback records, or retain the current nonconforming disposition. Nothing in this review authorizes release, hold lift, successor implementation, retrospective approval, or denominator change.
