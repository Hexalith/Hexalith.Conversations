---
title: "2026-09-16 validation-update reconciliation"
sources:
  - "validation-report.md"
  - "user update signal dated 2026-09-16"
  - "prd.md"
  - "addendum.md"
  - "../../../archive/conversations-product-contract-2026-05-31.md"
targets:
  - "prd.md"
  - "addendum.md"
extractionDate: "2026-09-16"
status: "source-extraction"
---

# 2026-09-16 validation-update reconciliation

## Reconciliation rule

This extraction compares the current reviewer gate and the user's binding update signal with the PRD package and its archived product-contract source. It records constraints and gaps; it does not create approvals, owners, waivers, authority records, or implementation evidence. The archived contract is provenance, not proof of current decision authority.

## Source paths

- Current PRD: `prd.md`
- Technical addendum: `addendum.md`
- Current consolidated review: `validation-report.md`
- Current adversarial detail: `review-adversarial-general.md`
- Archived original product contract: `../../../archive/conversations-product-contract-2026-05-31.md`
- Current performance result: repo-root `docs/release-evidence/sm-c2-hot-path-post-v1.md` and `.json`
- Current hold evidence: repo-root `docs/release-evidence/epic-6-completion-supersession-current-proof-v1.md`
- Frozen preservation baseline: repo-root `docs/release-evidence/release-baseline-v1.json`
- Draft exact-test traceability: repo-root `docs/release-evidence/preservation-traceability-manifest-v2.json`

## Binding directives and confirmed constraints

1. **SM-C2 remains universal and inviolable.** Every identified command/read hot path must have post-refactor P95 latency no more than 5% worse than its frozen pre-refactor P95 under the same reproducible envelope. This confirms PRD §2, SM-C2, OQ-5, and the prior memlog decision; it rejects the evidence artifact's conditional/per-path amended rule.
2. **Current performance evidence is failed.** Under the universal rule, HP-APPEND (+20.01%), HP-LIST (+329.85%), and HP-OPEN (+1760.88%) fail. Ungating rows or substituting approved-cost ceilings cannot produce an SM-C2 pass. HP-CREATE's measured improvement does not cure the other failures.
3. **The implementation hold remains active.** No wording in the PRD, addendum, evidence, or this reconciliation lifts the hold or authorizes release, successor work, story generation, or template certification.
4. **FR-20 and SM-C1 are pending.** Acceptance requires an approved, hash-bound manifest that maps exact tests to all seven closed preservation categories: tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, and governance audit-pairing.
5. **The denominator must not shrink.** Missing category coverage is a failed/pending gate, not permission to remove a category, test, or obligation. The final authority must preserve the closed seven-category boundary, enumerate exact test IDs and requirement mappings, bind source/build identity and hashes, and have zero orphaned active obligations.
6. **The archived contract reinforces evidence discipline.** It requires a versioned release-specific conformance manifest with test identifiers, pass criteria, requirement traceability, build/environment identity, waiver state, and signed release evidence (Feature-FR83/84 and Feature-NFR8/62/63 in the reconciled namespace). Its named people and historical defaults do not establish present approval authority.

## Safe gaps to fix in the PRD package

- Change the package from a finalized/accepted posture to an in-update, held posture; label execution snapshots historical and distinguish work performed from work evidenced, accepted, failed, pending, or held.
- State plainly that the current SM-C2 result is **failed under the universal <=5% rule** and that alternate ceilings/ungated rows are non-authoritative for this PRD gate.
- State plainly that FR-20/SM-C1 are **pending**, because `preservation-traceability-manifest-v2.json` remains `2.0.0-draft` / `pending-prerequisites`; do not call the 14-suite/214-test baseline proof of all seven categories.
- Keep the implementation hold active and link its current evidence without implying that the evidence can lift the hold.
- Mark FR-18, FR-19, SM-2, UJ-3, and Phase 3 proof pending while the reproducible fixture, walkthrough, measurement, and valid acceptance evidence remain incomplete.
- Mark the SM-1 denominator provisional unless and until a valid ratification is bound to its exact hash; do not backfill a historical signer or present self-asserted `status: accepted` as approval.
- Distinguish direct refactor users from adopter, operator, release, and tenant stakeholders exposed to preservation failure.
- Do not treat the §8 structural-or-trace alternative as a closed verifier until one exact oracle, command, schema, path, and pass rule are authorized.
- Narrow the addendum's broad "first-pass and approximate" disclaimer to historical estimates/candidate mappings and make the superseded inventory machine-visibly historical.
- Keep §14 preservation obligations non-activating unless explicit current activation evidence exists; downstream generation remains blocked where machine-safe state is absent.

## Decision conflicts requiring real authority or evidence

These conflicts cannot be closed by editorial remediation. Surface them in this order, one at a time; absence of a record means pending/nonconforming, never inferred approval.

1. **OQ-1 / FR-10 through FR-15 landing zones.** Either supply the actual per-FR repository/package decision, approval record, implementing revision, release vehicle, compatibility evidence, and rollback authority, or mark affected work nonconforming/pending. No current complete record is present in the PRD package.
2. **FR-16 scope breach.** Story 3.7 created deferred, unconsumed platform metadata. A real decision must either place that work in a separately authorized platform initiative or version the PRD to activate the precise subset; until then it is outside pilot scope and excluded from pilot acceptance/metrics.
3. **SM-1 denominator authority.** Retrospective ratification, if pursued, must identify a valid current authority and bind the exact artifact hash while disclosing timing. Otherwise the denominator remains provisional.
4. **§14 activation authority.** A signed/versioned activation model with per-requirement states is absent. The archived contract's historical authority statements cannot be promoted to current approvals without revalidation.
5. **Cross-repository grants.** EventStore, Commons, and FrontComposer change authority, closed consumer coverage, release order, compatibility window, and rollback authority are not evidenced. The Conversations PRD cannot grant them unilaterally.
6. **Named authority registry.** Generic role labels do not prove who held delegated authority for a decision date and scope. No person, registry version, effective dates, or supersession data may be invented.
7. **Hold disposition.** Only an accepted independent decision based on the current hold evidence may propose a status transition; no such accepted decision is present in the reconciled sources.

## Reconciliation outcome

The user signal resolves both critical validation choices without changing scope: retain the universal SM-C2 rule and fail the current result; retain the full preservation boundary and hold FR-20/SM-C1 pending. The implementation hold therefore remains active. All safe documentary corrections may proceed, while the seven authority/evidence conflicts above remain explicit blockers rather than inferred decisions.
