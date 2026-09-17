# OQ-1 / PRD Invariant Reviewer Gate — Rerun

## Overall verdict

**PASS.** The terminology and hash remediations are complete: no stale `TRX` reference remains in the PRD, addendum, aggregate, Owner record, technical-evidence record, or three grant records, and every reviewed SHA-256 binding resolves to the current file. The PRD correctly remains draft, OQ-1 remains evidence-blocked, and the Owner/OQ-1 records do not change the failed SM-C2 gate, active implementation hold, or pending/non-shrinkable FR-20/SM-C1 gate.

Finding counts: **critical 0 · high 0 · medium 0 · low 0**.

This is a reviewer-gate pass for the accuracy and internal consistency of the updated artifacts. It is not an OQ-1 technical pass, implementation-hold lift, preservation acceptance, performance waiver, release authorization, or PRD finalization.

## Required-invariant audit

| Invariant | Verdict | Evidence |
|---|---|---|
| No stale TRX terminology remains | **PASS** | A case-insensitive scan for the token `TRX` and `.trx` returned no matches across `prd.md`, `addendum.md`, `oq-1-landing-zone-approval-v1.json`, `oq-1-owner-authority-v1.json`, `oq-1-technical-evidence-v1.json`, and all three grant records. The addendum now says “ten hash-bound XML-encoded test-result runs” (§B), and the aggregate uses “XML test-result” / “XML-encoded test-result” consistently. |
| PRD remains draft and OQ-1 remains blocked | **PASS** | PRD frontmatter remains `status: draft` (`prd.md:1-5`). The aggregate remains `1.0.0-draft.3`, `status: evidence-blocked`, and `result: BLOCKED` (`oq-1-landing-zone-approval-v1.json:1-9`). |
| SM-C2 remains universal at no more than 5% regression for every identified command/read hot path | **PASS** | The readiness snapshot states the universal rule (`prd.md:29`). SM-C2 binds HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN and requires each identified path to be no more than 5% worse under the same reproducible envelope (`prd.md:362`). OQ-5 rejects evidence-local amended rules and waivers (`prd.md:401`). |
| Current SM-C2 evidence remains failed | **PASS** | The readiness snapshot says “FAILED on current evidence” (`prd.md:29`). SM-C2 records HP-APPEND at +20.01%, HP-LIST at +329.85%, and HP-OPEN at +1760.88% and expressly states that disclosures, cost ceilings, recorded-not-gated states, or evidence-local pass labels do not waive the universal rule (`prd.md:362`). |
| Implementation hold remains active | **PASS** | The readiness snapshot marks the hold **ACTIVE** and states that failed SM-C2 and pending FR-20/SM-C1 each independently prevent acceptance, release, successor-story authority, and hold lift (`prd.md:31-32`). The aggregate and technical-evidence non-claims also deny any hold lift. |
| FR-20/SM-C1 remain pending until all seven categories map to exact tests in one approved, hash-bound manifest | **PASS** | The readiness snapshot states the condition (`prd.md:28`). FR-20 names tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, and governance audit-pairing and requires exact fully qualified test IDs, requirement IDs, source/build identities, artifact hashes, approval evidence, and zero orphaned active obligations (`prd.md:333-346`). SM-C1 remains explicitly pending (`prd.md:361`). |
| Preservation denominator cannot shrink | **PASS** | FR-20 makes the v1 set and seven-category list immutable floors; later additions accumulate; deletion, replacement, merging, aggregation, reclassification, waiver, substitution, and shrinkage cannot establish acceptance (`prd.md:333-346`). SM-C1 repeats that successor evidence may only add coverage (`prd.md:361`). |
| OQ-1 records do not modify FR-20/SM-C1, SM-C2, or the hold | **PASS** | Aggregate scope excludes implementation-hold lift, FR-20/SM-C1 acceptance, and SM-C2 waiver/threshold change (`oq-1-landing-zone-approval-v1.json:47-54`); aggregate non-claims repeat those exclusions (`oq-1-landing-zone-approval-v1.json:411-417`). The Owner record is limited to FR-10 through FR-15 across EventStore, Commons, and Conversations and expressly disclaims hold lift, release, FR-20/SM-C1 approval, SM-C2 waiver, and FR-16 activation. PRD §13 says that authority establishes no authority for another gate (`prd.md:425`). |
| Pending grants cannot auto-activate | **PASS** | EventStore, Commons, and Conversations grants remain `issued-pending-evidence-closure`, `effective: false`, and unsuperseded. Each activation condition now states that immutable v1 never auto-activates and requires a hash-bound successor grant with explicit supersession, `effective=true`, and effective time. The aggregate assertion ledger keeps grant, release, compatibility, and rollback states BLOCKED. |
| OQ-1 technical evidence remains fail-closed | **PASS** | The technical-evidence record remains `result: BLOCKED`; it records 341 passing focused tests separately from one failed AppHost runtime boundary, the unresolved IdentityModel version conflict, absent package proof, and conditional rollback. Passing focused tests are not promoted into release or preservation acceptance. |

## Hash and record-integrity audit

- All seven aggregate `sourceBindings` matched the current files, including the updated addendum hash.
- All five aggregate `recordBindings` matched the current Owner, technical-evidence, and grant records.
- All ten XML test-result paths matched their SHA-256 values; their passed-test sum equals the recorded 341.
- The aggregate release-evidence binding and each grant's compatibility-evidence binding matched the current technical-evidence record.
- All six reviewed JSON records parsed successfully.
- The runtime result remains `FAIL`; no raw runtime artifact, environment identity, or hash binding is invented.

## Rubric snapshot

### Decision-readiness — adequate

The PRD makes the current decision state usable without pretending it is complete: the landing-zone decision and scoped Owner authority are recorded, while four technical blockers and the conditions for successor grants remain explicit. The draft/blocked state is the correct outcome for the available evidence.

### Substance over theater — strong

The critical gates are specific and evidenced: four named performance paths with measured regressions, seven named preservation categories, exact manifest requirements, non-shrink rules, versioned authority/grant records, and concrete OQ-1 blockers. Passing focused tests are separated from the failed runtime boundary.

### Strategic coherence — strong

OQ-1 resolves only the shared-module landing-zone and bounded authority question. It remains subordinate to behavior preservation and performance, so the refactor thesis and counter-metrics continue to form one coherent decision model.

### Done-ness clarity — adequate

FR-20/SM-C1 and SM-C2 have explicit pass conditions, and the OQ-1 records distinguish recorded authority, issued-but-ineffective grants, technical closure, successor-grant issuance, release, and rollback. The artifacts correctly communicate why the initiative is not done.

### Scope honesty — strong

Implementation, release, publication, rollback, FR-16, preservation, performance, and hold boundaries are explicit. The Owner record is self-attested, scoped, and does not claim external forge or package-registry permissions.

### Downstream usability — strong

The prior XML/TRX terminology drift is resolved. Stable IDs, current SHA-256 bindings, exact repository snapshots, explicit blocker states, and non-auto-activating grants make the record set mechanically extractable without silently widening authority.

### Shape fit — strong

The document's depth fits a brownfield, cross-repository, evidence-gated refactor with a preserved product-contract baseline. The readiness snapshot and decision register keep the current state visible despite the large normative baseline.

## Mechanical notes

- Refactoring IDs remain FR-1 through FR-20 with FR-16 explicitly deferred; preserved namespaces remain Feature-FR1 through Feature-FR104 and Feature-NFR1 through Feature-NFR77.
- UJ-1 through UJ-3 retain named protagonists and connect to the relevant FRs.
- Generic role labels remain responsibility descriptions rather than evidence of named authority; only Jérôme Piquot is named, from the recorded direct attestation.
- No gate contradiction or new finding was identified in this rerun.
- This review changed no PRD, addendum, memlog, OQ-1 record, grant, or evidence artifact.
