# OQ-1 / PRD Invariant Reviewer Gate

## Overall verdict

**PASS WITH MINOR REMEDIATION.** The PRD remains a draft, the OQ-1 aggregate remains evidence-blocked, and the new self-attested Owner authority is confined to FR-10 through FR-15 across EventStore, Commons, and Conversations. It does not alter the universal SM-C2 rule, the failed performance result, the active implementation hold, or the pending/non-shrinkable FR-20/SM-C1 preservation gate.

Finding counts: **critical 0 · high 0 · medium 1 · low 0**.

## Required-invariant audit

| Invariant | Verdict | Evidence |
|---|---|---|
| SM-C2 remains universal at no more than 5% regression for every identified command/read hot path | **PASS** | PRD §2 states the universal rule (`prd.md:29`); SM-C2 names HP-CREATE, HP-APPEND, HP-LIST, and HP-OPEN and requires every row to satisfy the threshold (`prd.md:362`); OQ-5 repeats that no evidence-local amended rule or waiver is inferred (`prd.md:401`). |
| Current SM-C2 evidence is failed | **PASS** | PRD §2 marks the performance gate “FAILED on current evidence” (`prd.md:29`); SM-C2 records three failing rows and expressly rejects cost ceilings, disclosures, or evidence-local labels as waivers (`prd.md:362`). |
| Implementation hold remains active | **PASS** | The readiness snapshot marks the hold **ACTIVE** and states that either failed SM-C2 or pending FR-20/SM-C1 independently prevents acceptance, release, and successor-story authority (`prd.md:31-32`). The aggregate says it does not lift the hold (`oq-1-landing-zone-approval-v1.json:413-416`). |
| FR-20/SM-C1 remain pending until all seven categories map to exact tests in one approved, hash-bound manifest | **PASS** | The readiness snapshot states the condition (`prd.md:28`); FR-20 names all seven categories and requires exact fully-qualified test IDs, requirement IDs, source/build identities, hashes, approval evidence, and zero orphaned active obligations (`prd.md:333-346`); SM-C1 remains explicitly pending (`prd.md:361`). |
| Preservation denominator cannot shrink | **PASS** | FR-20 makes the v1 set a floor, carries additions forward, and prohibits deletion, replacement, aggregation, merging, reclassification, waiver, substitution, or shrinkage (`prd.md:333-346`). SM-C1 repeats that successor evidence may only add coverage (`prd.md:361`). |
| OQ-1 Owner records do not affect preservation/performance gates | **PASS** | Aggregate scope explicitly excludes hold lift, FR-20/SM-C1 acceptance, and SM-C2 waiver/threshold change (`oq-1-landing-zone-approval-v1.json:47-54`); its non-claims repeat all three boundaries (`oq-1-landing-zone-approval-v1.json:411-417`). PRD §13 limits the authority record to FR-10 through FR-15 and says it establishes no authority for another gate (`prd.md:425`). |
| Draft/not-final state is preserved | **PASS** | PRD frontmatter remains `status: draft` (`prd.md:1-5`); the OQ-1 aggregate is `1.0.0-draft.3`, `evidence-blocked`, and `BLOCKED` (`oq-1-landing-zone-approval-v1.json:1-9`). |
| No approver, waiver, owner, or authority is invented | **PASS** | The only named authority is Jérôme Piquot, grounded in the preserved statements “I Jérôme Piquot approve” and “I am the Owner, do the needed records”; the authority record labels itself self-attested, leaves the external identifier null, says external identity verification was not performed, and disclaims external permissions and all unrelated gates. Generic role labels elsewhere are explicitly described as responsibilities rather than proof of named authority (`prd.md:415-425`). No waiver is asserted. |
| Owner authority does not turn the grants into acceptance or release authority | **PASS** | The PRD describes all three grants as issued but ineffective (`prd.md:397,407-410`); the aggregate keeps grant, release, compatibility, and rollback assertions blocked (`oq-1-landing-zone-approval-v1.json:371-399`) and states that no implementation, cross-repository mutation, package publication, release, or successor work is authorized (`oq-1-landing-zone-approval-v1.json:411-417`). |

## Findings

### Medium

- **Evidence-file terminology is stale after the `.trx` to `.xml` rename** (Addendum §B; OQ-1 aggregate root observation and compatibility ledger) — The authoritative technical-evidence record now binds ten `.xml` files, but the addendum still says “ten hash-bound TRX runs” (`addendum.md:21`), the aggregate worktree note says “generated TRX record update” (`oq-1-landing-zone-approval-v1.json:124`), and the compatibility assertion says “Ten hash-bound TRX runs” (`oq-1-landing-zone-approval-v1.json:390-394`). This does not weaken a gate because the hashes and paths resolve and OQ-1 stays blocked, but it creates avoidable ambiguity for downstream evidence readers. *Fix:* replace those three TRX labels with “XML test-result files/runs”; after changing the addendum, refresh its hash in the aggregate `sourceBindings`, then revalidate every record binding.

## Rubric snapshot

### Decision-readiness — adequate

The decision surface is honest and actionable: OQ-1 distinguishes recorded Owner authority from ineffective grants, names four remaining technical blockers, and retains a blocked result. The PRD also separates historical work-performed claims from acceptance and release authority. The artifact is deliberately not ready to finalize, which is the correct decision state.

### Substance over theater — strong

The gate language is specific: four named hot paths with actual regression figures; seven named preservation categories; immutable denominator rules; exact evidence and authority boundaries; and concrete runtime, assembly, package, and rollback blockers. No pass is inferred from the 341 focused tests.

### Strategic coherence — strong

The Owner/OQ-1 update serves the platform-landing-zone decision while remaining subordinate to preservation and performance acceptance. The refactor thesis, scope boundaries, and counter-metrics continue to align.

### Done-ness clarity — adequate

FR-20/SM-C1 and SM-C2 are unusually explicit about their pass conditions. OQ-1 closure conditions are also listed. The remaining open conditions correctly prevent a final/accepted state rather than making done-ness ambiguous.

### Scope honesty — strong

Authority, implementation, release, package publication, rollback, FR-16, FR-20/SM-C1, and SM-C2 boundaries are all explicit. Historical claims are consistently quarantined from current approval.

### Downstream usability — adequate

Stable IDs, versioned records, SHA-256 bindings, repository snapshots, blocker states, and cross-references support extraction. The stale TRX terminology is the only material navigation/provenance blemish found in this pass.

### Shape fit — strong

The long form is justified by the brownfield, cross-repository, evidence-gated refactor and embedded preserved product-contract baseline. The readiness snapshot and decision register keep the current operational state discoverable despite the document's depth.

## Mechanical notes

- The aggregate's PRD, addendum, Owner-authority, technical-evidence, and three grant hashes matched the reviewed files at review time.
- The aggregate parsed as valid JSON.
- No contradiction was found between the PRD frontmatter, readiness snapshot, FR-20/SM-C1, SM-C2, OQ-1 register, aggregate assertion ledger, and aggregate non-claims.
- This review changed no PRD, addendum, OQ-1 record, evidence artifact, or memlog entry.
