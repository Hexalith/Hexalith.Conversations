# Sprint Change Proposal - Record the Release-Owner Attestation Decision

**Date:** 2026-07-14
**Project:** Conversations
**Requested and approved by:** Jerome
**Workflow:** bmad-correct-course
**Mode:** Batch
**Status:** Approved and implemented

## 1. Issue Summary

Story 5.3 produced `success-metric-report-and-attestation-v1.{json,md}` as implementation-generated evidence with a `ready-for-signature` status and deliberately pending release-owner fields. Epic 5 then closed with a release-owner action to record the real decision without changing that evidence.

The triggering request is to complete that governance step. The source pair is hash-guarded, its validation test requires the implementation artifact to remain unsigned and pending, and the retrospective permits the decision to be recorded through a separate signed artifact. Editing the source pair would erase the implementation/release-owner boundary and invalidate the implementation evidence semantics.

Evidence at decision time:

- Source JSON SHA-256: `062ca0c7bc94279007077bda59eae867d21c12da2ffc0b59a0f389b99067e0fe`
- Source Markdown SHA-256: `aa7e52c11ce36fc2c9ea953e275c654e7f312016c990cb20be16666d87f9a2cd`
- Source commit: `c6670fac7347ecd7240f7bab7e5e23147c8dfc65`
- Signable payload hash: `d6c61737d3b937f1142f77f81c82eb7b13607a1d923c173095cb1ffef2f2fe73`
- Final recorded conformance: 384 / 384 passed with an empty public-contract-shape diff.

## 2. Impact Analysis

### Epic and Story Impact

Epic 5 and Story 5.3 remain complete. The correction closes the retrospective release-owner action; it does not reopen a story, add scope, change acceptance criteria, or alter sprint ordering.

### Artifact Impact

- **PRD:** No change. The decision follows the existing module-level evidence and approval boundaries.
- **Epics/stories:** No change. Story 5.3 intentionally delivered a signable, unsigned implementation artifact.
- **Architecture:** No change. The separate sidecar preserves the architecture's evidence-authority and compliance-separation rules.
- **UX:** No change. No rendered surface or workflow changes.
- **Implementation-generated evidence:** No change. Both Story 5.3 source files remain byte-identical.
- **Release-owner evidence:** Add a JSON authority and Markdown summary that bind the decision to the source hashes.
- **Sprint status:** No change because no epic or story is added, removed, or renumbered.

### Technical Impact

There is no runtime, public-contract, package, AppHost, source-code, test, generated-output, or submodule impact. The only technical risk is evidence drift; the decision sidecar mitigates it by binding the source file hashes, source commit, and signable payload hash and by declaring that drift invalidates the decision.

## 3. Recommended Approach

**Selected path:** Direct Adjustment.
**Scope:** Minor.
**Effort:** Low.
**Risk:** Low.

Create a separate release-owner decision pair under `docs/release-evidence/`. Record approval with the disclosed module-level residual risks, explicitly preserve unresolved OQ-2 target interpretation and projection proof, and acknowledge without approving inherited platform controls.

Rollback is not justified because the implementation evidence is valid and complete. MVP review is not applicable because this is a governance recording step, not a product-scope change.

## 4. Detailed Change Proposal

### Release-Owner Decision Artifact

Artifact: `docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.json`

OLD:

```text
No separate release-owner decision artifact exists. The implementation-generated attestation remains ready-for-signature with pending decision fields.
```

NEW:

```text
A signed release-owner sidecar binds to the implementation artifact hashes and records:
- release-owner decision: approved-with-recorded-residual-risks
- residual-risk acceptance: accepted-for-this-release-with-follow-up
- platform-control acknowledgement: acknowledged-not-approved
- signer: Jerome
- approval reference: this approved Sprint Change Proposal
- invalidation rule: any bound evidence drift requires a new decision
```

Rationale: A sidecar records the human governance decision without rewriting evidence generated and validated by the implementation agent.

### Human-Readable Summary

Artifact: `docs/release-evidence/success-metric-report-and-attestation-v1-release-owner-decision.md`

Add a navigable summary of the source hashes, decision, accepted residual risks, platform-control exclusion, and signature meaning. The JSON sidecar remains authoritative.

## 5. Implementation Handoff

**Classification:** Minor.
**Recipients:** Release owner and release reviewer/technical writer.

Responsibilities:

- Release owner: own the decision, accepted risks, and future invalidation/re-signing.
- Release reviewer/technical writer: keep release-facing claims aligned with this decision's exact scope.
- Developer/Architect/Quality follow-up owners: retain ownership of OQ-2, projection proof, and residual conformance coupling; this decision does not close those technical actions.

Success criteria:

- The release-owner JSON and Markdown decision records exist and agree.
- The source JSON/Markdown hashes remain unchanged.
- The decision binds to the current signable payload hash and source commit.
- No platform compliance or external security approval is claimed.
- No implementation, test, generated evidence, sprint-status, or submodule file is changed.

## 6. Change Navigation Checklist

### Trigger and Context

- [x] 1.1 Triggering story identified: Story 5.3 and Epic 5 retrospective action A1.
- [x] 1.2 Core problem defined: the release-owner decision is pending while implementation evidence must remain immutable.
- [x] 1.3 Evidence gathered: source artifact fields, hashes, validation invariants, residual risks, and retrospective completion condition.

### Epic Impact

- [x] 2.1 Epic 5 remains complete.
- [N/A] 2.2 No epic scope modification is required.
- [x] 2.3 Remaining epics and dependencies are unaffected.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No resequencing or priority change is needed.

### Artifact Conflict and Impact

- [x] 3.1 PRD checked; no conflict or change.
- [x] 3.2 Architecture checked; separate decision evidence follows its authority boundaries.
- [N/A] 3.3 UX checked; no UI impact.
- [x] 3.4 Release evidence and documentation boundary identified; only new release-owner records and this proposal are needed.

### Path Forward

- [x] 4.1 Direct Adjustment is viable with low effort and risk.
- [N/A] 4.2 Rollback is not justified.
- [N/A] 4.3 MVP review is not applicable.
- [x] 4.4 Direct Adjustment selected.

### Proposal and Handoff

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP is unaffected; action plan is bounded to decision evidence.
- [x] 5.5 Release-owner/reviewer and technical follow-up responsibilities defined.
- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked for source-hash and scope consistency.
- [x] 6.3 Jerome's request records approval to implement this bounded decision sidecar.
- [N/A] 6.4 Sprint status does not change because no epic/story topology changes.
- [x] 6.5 Handoff and invalidation criteria defined.

## 7. Approval and Completion

Jerome approved the bounded direct adjustment through the request to record the release-owner decision without changing implementation-generated evidence. The completed implementation uses a separate sidecar, preserves the source pair, accepts the disclosed module-level residual risks for this release with follow-up, and makes no inherited platform-control approval claim.
