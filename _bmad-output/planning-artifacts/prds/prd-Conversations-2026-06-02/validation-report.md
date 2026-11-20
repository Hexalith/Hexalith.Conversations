# Validation report — Conversations Boilerplate Reduction

**Document grade** Excellent

0 critical · 0 high · 0 medium · 0 low

**Reviewed** 2026-11-20T18:01:28+01:00

[PRD](prd.md) · [Addendum](addendum.md) · [Full rubric review](review-rubric.md)

## Overall verdict

The amended PRD closes all four findings. An engineer can now keep the required test harness, use the documented retry contract, and identify which shared capabilities already exist. Pilot acceptance remains open pending preservation approval, a passing performance benchmark and effective successor grants.

## Release evidence

FR-20 and SM-C1 await approval of the preservation evidence. The recorded SM-C2 benchmark exceeds the allowed 5% regression on three of the four hot paths. The failed performance gate and pending preservation approval continue to block pilot acceptance. [PRD §2](prd.md#2-implementation-decision-and-readiness-snapshot) · [Benchmark results](../../../../docs/release-evidence/sm-c2-hot-path-post-v1.json)

## Dimension verdicts

| Dimension | Verdict | Judgment |
| --- | --- | --- |
| Decision-readiness | Strong | Sections 2, 4 and 12 agree that the three immutable v1 grants remain ineffective. Each repository must issue a hash-bound successor that supersedes its v1 grant and records its effective state and time. The Conversations successor also depends on the EventStore and Commons successors, runtime and assembly-version evidence, and accepted rollback evidence. The November backlog gives owners work they can schedule under current policy. [EventStore grant](../../oq-1-eventstore-change-grant-v1.json) · [Commons grant](../../oq-1-commons-change-grant-v1.json) · [Conversations grant](../../oq-1-conversations-consumer-grant-v1.json) |
| Substance over theater | Strong | SM-2 counts 13 hand-authored fixture files, including its test AppHost project and source file. The draft qualifies the 77.59% reduction against an estimated 58-file baseline and explains why the LOC comparison is indicative. SM-1 continues to identify the missing named ratification for its own denominator. [Measured SM-2 evidence](../../../../docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md) |
| Strategic coherence | Strong | The pilot makes a domain module cheaper to author while preserving the behavior its consumers rely on. Sections 2.1 and 7 connect that goal to observable outcomes, including tenant-safe reads, retry behavior and per-path performance. Package qualification and the production client journey remain backlog work. [Sprint supplement](epics.md#current-sprint-supplement--20-november-2026) |
| Done-ness clarity | Strong | FR-3, FR-13, FR-18 and addendum §§A, D and F agree on hosting ownership. The platform provides production orchestration. Each module keeps exactly one repository-only test AppHost with packing and publishing disabled. The measured minimal module has its own fixture harness, whose two hand-authored files count toward SM-2. Story 9.3 still calls for verification through the supported deployed host. [Approved test AppHost criteria](../../../../docs/release-evidence/thin-authoring-guidance-v2.md#test-apphost) · [Fixture inventory](../../../../docs/release-evidence/thin-module-fixture-proof-v1.md#fixture-inventory) |
| Scope honesty | Strong | The draft limits new work to the pilot, retains FR-16 as deferred, and keeps fleet migration separate. The confirmed host exception, observed registrations and caller-supplied key contract provide no new release approval. Sections 2 and 12 retain the current-gitlink proof requirement, and §14 preserves the product contracts with their existing activation rules. [Current change policy](../../../../docs/runbooks/current-change-validation.md) |
| Downstream usability | Strong | Story 9.3 and addendum §B give create and append distinct caller-supplied keys and reuse each command’s original key and metadata on retry. The command validator rejects a missing key. Addendum §F records the existing shared tenant-access and HTTP registrations and identifies the remaining FR-11 parity and FR-12 compatibility evidence. These entries give developers a concrete integration starting point. [Integration guide](../../../../docs/integration-guide.md) · [Command validator](../../../../src/Hexalith.Conversations/Validation/ConversationCommandSchemaValidation.cs) · [Tenant registration](../../../../src/Hexalith.Conversations.Server/TenantAccess/ConversationTenantAccessServiceCollectionExtensions.cs) · [Client registration](../../../../src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs) |
| Shape fit | Strong | The readiness table and dated sprint supplement serve the immediate planning decision. The three named developer journeys, capability sections and stable requirement IDs support engineering handoff. The addendum carries implementation detail, and the preserved baseline remains available to release and conformance reviewers. |

## Findings by severity

No open documentation findings.

## Mechanical and source checks

- All 20 initiative FRs, 104 preserved Feature-FRs and 77 preserved Feature-NFRs have unique, contiguous definition IDs. UJ-1 through UJ-3 each name a protagonist.
- The five inline assumption tags map to §13. The preserved contract section is byte-identical to its pre-amendment version. The local Markdown links and anchors in the amended PRD and addendum resolve.
- The document grades Excellent across seven strong dimensions. Open findings total zero at every severity.
- The disposition of each November validation finding is recorded in the [four-finding reconciliation](reconcile-validation-2026-11-20.md).
- All ten November audit findings map to seven new and two existing backlog stories. The preserved product-contract section matches the committed baseline byte for byte. [Sprint proposal](../../sprint-change-proposal-2026-11-20.md) · [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml)
- Story 11.3 records 13 measured files against 58 estimated, a 77.59% reduction on the accepted estimate. The historical comparison has low confidence. [SM-2 measurement](../../../../docs/release-evidence/minimal-module-authoring-cost-sm2-v2.md)

## Review record

Working draft dated 2026-11-20, based on repository revision `698684f8e05c`. Reviewed PRD SHA-256 `9b89e9b36392af6d1e2bcc8e001f8d1db18c665005b661ea0d76a1d405cbefd0` and addendum SHA-256 `e258bec29df5223fc8051e9124bcb2b7fe665d71a6fa9b7235ba3713075e1b12`.

[Rubric](../../../../.agents/skills/bmad-prd/assets/prd-validation-checklist.md) · [Reviewer findings](review-rubric.md)
