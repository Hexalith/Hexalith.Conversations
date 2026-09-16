<!-- validation-run: 2026-09-16 current-run -->
# Adversarial PRD Review — Conversations Boilerplate Reduction

**Review date:** 2026-09-16  
**Lens:** adversarial/general — contradictions, authority, scope control, execution-state drift, verifiable gates, cross-repository ownership, and downstream architecture/story safety  
**Reviewed in full:** `prd.md`, `addendum.md`  
**Read-only corroboration:** cited repository evidence and current sprint status; no source artifact was changed

## Verdict

**REJECT AS A CURRENT RELEASE, ARCHITECTURE, OR STORY-GENERATION AUTHORITY.** The refactor intent is coherent, but the document's strongest claims are not. It declares a final PRD and an executed pilot while mandatory gates remain incomplete. The repository shows that the performance gate was materially weakened outside the PRD, FR-16 platform work crossed the declared scope boundary, landing-zone decisions remain nominally open after implementation, and the execution plan has expanded far beyond the snapshot presented here. This artifact can serve as historical intent only until its authority hierarchy, gate definitions, scope ledger, and present-state claims are reconciled.

**Severity counts:** 2 Critical · 7 High · 3 Medium · 1 Low

## Critical findings

### C-1 — The enforced SM-C2 rule contradicts the PRD's inviolable performance gate

**Exact PRD claims**

- §2, *Performance gate*: “post-refactor P95 command/read latency may be no more than 5% worse than the frozen reproducible pre-refactor baseline.”
- §7, *SM-C2*: “For every identified command/read hot path, post-refactor P95 latency must be no more than 5% worse.”
- §12, OQ-5: “SM-C2 permits at most a 5% post-refactor P95 regression.”

**Adversarial finding**

The post-refactor evidence applies a different rule. `docs/release-evidence/sm-c2-hot-path-post-v1.md` records HP-APPEND at **+20.01%** as “recorded, not gated,” HP-LIST at **+329.85%** as a pass against an approved-cost ceiling, and HP-OPEN at **+1760.88%** as a pass against another ceiling. Its companion JSON says the 5% rule applies only conditionally, with two paths ungated and two paths judged against post-change ceilings. The traceability manifest repeats that amended rule.

Either implementation evidence changed a normative PRD gate without updating the PRD, or the PRD's claim that SM-C2 is “authoritative” is false. A release can be called green while violating the stated gate by orders of magnitude.

**Impact**

- Release and hold-lift decisions can cite mutually incompatible authorities.
- Architecture may normalize an expensive design that the PRD forbids under the 5% envelope.
- Stories generated from the PRD will enforce a rule the current evidence system does not enforce.

**Concrete fix**

Choose one rule through explicit change control. Either restore the universal 5% gate and mark the current result failed, or amend SM-C2 in a new PRD version with the exact per-path rule, approver, rationale, expiry, successor story, and maximum ceilings. Bind the accepted rule's hash into the evidence schema and reject evidence produced under any other rule. Do not leave `status: final` while normative text and enforced gate disagree.

### C-2 — FR-20's “closed” preservation denominator does not contain the behaviors it claims to close

**Exact PRD claims**

- §4, *Conformance suite*: “in exactly the closed category list bound by FR-20: tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness, governance audit-pairing.”
- §6.5, FR-20: “The conformance-suite category list above is closed.”
- §6.5, FR-20: the baseline is “14 conformance suites, 214 tests, 100% pass.”
- §6.5, FR-20 says the traceability map has “`2.0.0-draft` / pending-prerequisites status” and uncovered §14 obligations are explicitly unguarded.

**Adversarial finding**

The cited `release-baseline-v1.json` contains 14 suite classes and 214 suite-class tests, but its suite list does **not** contain a projection-freshness suite or a governance-audit-pairing suite. It instead contains categories such as buyer acceptance, release scope, second adopter, telemetry cardinality, and telemetry redaction. The PRD equates two different denominators: a seven-category behavioral list and a fourteen-suite artifact. It then admits that the requirement traceability map is still draft and has pending prerequisites.

“100% of the manifest” proves only what the manifest actually contains. It cannot prove the declared closed behavior set when two named behaviors are not identifiable in that baseline and the requirements-to-tests map is not final.

**Impact**

- The no-regression gate can pass while projection freshness or governance audit pairing is unproved.
- Tests can be removed or misclassified without a finalized traceability authority.
- Architecture cannot distinguish preserved behavior from unguarded aspiration.

**Concrete fix**

Publish one authoritative preservation-denominator artifact enumerating exact test IDs, categories, requirement IDs, source commit, and hashes. Reconcile the seven declared categories against all 14 suites; add missing tests or mark the initiative failed/pending. Finalize and approve the traceability manifest with zero orphaned active obligations before lifting the hold or claiming FR-20/SM-C1 acceptance.

## High findings

### H-1 — “Final” and “executed” are stale readiness claims

**Exact PRD claims**

- Frontmatter: `status: final`, `updated: "2026-08-18"`.
- §2: “Phases 0–3 ... have executed as Epics 1–6 ... Epics 7–9 remain backlog.”
- §5.3 repeats: “Phases 0–3 have executed.”

**Adversarial finding**

Sprint status was updated on 2026-08-19 and includes not only Epics 7–9 but Epics **10–16** in backlog, multiple in-progress corrective actions, and an active global implementation hold. The PRD snapshot was obsolete one day after its update and is still presented under “Implementation Decision and Readiness Snapshot.” An as-of date can support historical reporting; it cannot make stale content the final chain-top readiness authority.

**Concrete fix**

Remove mutable execution state from the PRD or generate it from one current-status authority. Label this snapshot historical and not authoritative, link the current hold/decision chain, and block downstream generation unless the snapshot hash matches the current sprint/authority bundle.

### H-2 — OQ-1 remains open after the work it was required to gate has executed

**Exact PRD/addendum claims**

- §2: “the platform architect must resolve each FR-10 through FR-15 landing zone before its implementation story starts.”
- §12 leaves OQ-1 as an “Architecture dependency.”
- Addendum §B lists “Landing zone per promotion” under open architecture decisions.
- §5.3 says the promotion phase executed.

**Adversarial finding**

If FR-10 through FR-15 stories executed, their landing zones were necessarily chosen. Yet the chain-top documents still call the choice open and do not identify per-FR decisions. Either stories began in violation of their architecture precondition or the PRD/addendum failed to absorb the decisions. Across repositories, the PRD assigns capabilities to EventStore, Commons, or FrontComposer without binding the owning repository's approver, API/version decision, compatibility matrix, or release commitment.

**Concrete fix**

Publish a per-FR decision table for FR-10 through FR-15: chosen repository/package, rejected alternatives, owner/approver, decision artifact, implementing commit/gitlink, release vehicle, compatibility evidence, and rollback owner. Close OQ-1 if valid; otherwise mark affected stories nonconforming and reopen them.

### H-3 — FR-16 crossed the declared scope boundary through unconsumed platform work

**Exact PRD/addendum claims**

- §2: “FR-16 is deferred.”
- §5.2: “Promotions Conversations does not consume are cataloged as follow-on backlog, not built here.”
- FR-16: “The pilot does not add shared command/event metadata interfaces.”
- Addendum §F row 1: command/event metadata is “Backlog. Explicitly deferred from the pilot.”

**Adversarial finding**

Sprint status marks Story 3.7, “promote-adopt-compile-time-command-event-contract-metadata,” done. Its implementation record says shared EventStore command/event metadata and resolvers were added while direct Conversations DTO adoption was deferred. That is the forbidden state: a promotion Conversations did not consume was built inside the pilot. The platform repository absorbed public surface, tests, maintenance, and compatibility risk under a PRD that excluded it.

**Concrete fix**

Move the EventStore metadata work into a separately authorized platform initiative, or approve a versioned PRD change activating the precise FR-16 subset. Exclude it from pilot acceptance/metrics and split Story 3.7's disposition between platform creation and Conversations adoption.

### H-4 — The template and SM-2 are called completed while mandatory evidence is provisional

**Exact PRD claims**

- FR-18 requires a recorded walkthrough at `thin-authoring-template-validation-v1.md` attached to pilot acceptance.
- FR-19 requires a reproducible fixture and measurement with commands, tool versions, commit/build identity, results, and named acceptance.
- §7 says SM-2 figures are “provisional” and “do not establish target attainment.”
- §5.3 says Phase 3 “Adopt & Prove” executed.

**Adversarial finding**

The document claims the prove phase executed while its primary metric says proof has not been established. Sprint status reinforces the gap: Epic 11 remains backlog to correct the template guidance, build the reproducible fixture, and produce authoritative SM-2 v2 evidence. UJ-3 and half the stated payoff depend on this proof.

**Concrete fix**

Separate “implementation executed” from “pilot accepted.” Mark FR-18, FR-19, SM-2, UJ-3, and Phase 3 proof pending until the fixture, reproducible measurement, walkthrough, and named acceptance exist. Do not call the template authoritative before Epic 11 or equivalent completes.

### H-5 — The authoritative SM-1 denominator lacks its required named acceptance

**Exact PRD/addendum claims**

- §7 requires “the same named approval discipline FR-20 demands for tests.”
- Addendum §A calls the inventory authoritative, then admits “no named acceptor; backfilling the named acceptance ... is an open follow-up.”

**Adversarial finding**

An artifact cannot be the accepted, protected denominator while lacking the identity required to make acceptance valid. `status: accepted` inside JSON is self-assertion, not approval. Backfilling a signer after implementation optimized against the denominator creates retrospective authority.

**Concrete fix**

Obtain a named ratification bound to the exact artifact hash and disclose that it is retrospective, or mark the denominator provisional and recalculate after approval. Do not mutate the historical record to simulate contemporaneous acceptance.

### H-6 — §14 is a normative contract catalog with no safe activation model

**Exact PRD claims**

- §0 calls §14 the “authoritative preserved product-contract baseline.”
- §14.1 says every Feature-FR and Feature-NFR is preserved, while “Preserved does not mean implemented, shipped, accepted, or scheduled.”
- §14.1 says every legacy item's delivery state is “open pending evidence or an explicit release decision.”
- §14.4 calls the v1 floor “Open / status unverified.”

**Adversarial finding**

Section 14 combines 104 functional and 77 non-functional requirements under a normative heading, then denies that implementation or activation is known. A downstream workflow sees requirement IDs and imperative statements; it has no per-requirement machine-safe state for implemented, preservation-gated, conditional, historical-only, active-release, or unguarded. The PRD tries to authorize a narrow refactor and preserve an unresolved roadmap in one artifact.

**Concrete fix**

Move §14 to a separately versioned contract catalog with explicit state per requirement and a signed release-activation manifest. Keep only the active preservation subset and hash in this refactor PRD. Tooling must consume activation state rather than infer it from prose.

### H-7 — Cross-repository authorization is asserted without defining who grants it

**Exact PRD claims**

- §5.1: “Coordinated changes into the relevant technical-module submodules (authorized for this initiative).”
- §9 says shared work “may edit sibling technical-module submodules.”
- §11 mitigates R3 with “additive-only changes; build the dependent modules in CI.”

**Adversarial finding**

No named owner for EventStore, Commons, or FrontComposer is bound to this authorization. No package release path, supported-consumer set, compatibility window, rollback authority, or cross-repo merge order is defined. A Conversations PRD cannot unilaterally grant ownership changes to technical repositories. “Build dependent modules” is not a gate without a closed consumer matrix, pinned revisions, commands, and required result.

**Concrete fix**

Add a cross-repo change charter signed by each repository owner. Define exact consumers at pinned gitlinks/package versions, required CI commands, acceptance owner per repo, release sequence, compatibility duration, and rollback procedure. Replace blanket authorization with per-capability approvals.

## Medium findings

### M-1 — “No external/customer-facing surface” conflicts with the affected contracts

**Exact PRD claims**

- §3 assumes “no external/customer-facing surface in scope.”
- §3.2 says end customers “should observe nothing.”
- §10 says public contracts are unchanged.
- §14 defines adopter packages, clients, errors, operator workflows, and chatbot adoption.

**Adversarial finding**

The refactor's direct user is a developer, but its affected surface is not internal-only. Public/adopter contracts, tenant isolation, operator workflows, and publication are in the preservation blast radius. Calling stakeholders non-users encourages underweighting adopter validation and customer-visible failure modes.

**Concrete fix**

Separate users of the refactor from stakeholders affected by preservation failure. Add adopter developers, release owners, operators, and tenant users to the latter set with review and acceptance responsibilities.

### M-2 — The no-synchronous-cross-service-call NFR has no closed verification artifact

**Exact PRD claim**

- §8 says SM-C2 cannot observe cross-process calls, so the invariant is verified by “structural evidence per identified hot path (dependency/call-boundary conformance check or trace-based assertion recorded with the SM-C2 evidence).”

**Adversarial finding**

“Structural check or trace-based assertion” leaves the oracle undecided. The PRD names neither a required artifact nor exact pass criteria while hosting and cross-key validation change. An implementer can select whichever evidence is easiest after the fact.

**Concrete fix**

Name one required verifier, exact hot-path boundaries, forbidden edges, command, artifact path/schema, and pass criteria. Bind it into the same release gate as SM-C2.

### M-3 — Decision roles are generic where gates require named authority

**Exact PRD claims**

- Owners include “platform architect,” “release owner,” “pilot acceptance owner,” and “product/platform owner.”
- FR-20 and SM-1 require explicit named-owner approval for denominator changes.
- §14.8 requires current named approval authority for legacy dispositions.

**Adversarial finding**

Role labels are insufficient for irreversible approvals. The PRD never binds current people or an authority registry to them. Evidence may be signed elsewhere, but the chain-top artifact gives no stable way to determine whether a signer held the role for the decision date and scope.

**Concrete fix**

Reference a versioned authority registry mapping each role to a named approver, effective dates, delegated scope, and supersession. Bind approval artifacts to both person and registry version.

## Low finding

### L-1 — Superseded inventory remains easy to misuse as active architecture input

**Exact addendum claims**

- The opening says figures are “first-pass and approximate — confirm during architecture.”
- §C calls its table “superseded provenance” and says dual labels are not accepted.
- The table nevertheless retains implementation-ready target capabilities and recommendations.

**Adversarial finding**

The warning is clear, but the presentation invites copy/paste into architecture or stories because the authoritative JSON is not rendered alongside it.

**Concrete fix**

Move the superseded table to a historical appendix or render the accepted inventory as primary. Add a machine-visible `superseded: true` banner and direct link/hash to the sole authority.

## Required correction order

1. Reconcile normative SM-C2 with the rule actually enforced; current results cannot be called a PRD pass.
2. Repair and approve the FR-20 denominator and traceability authority, including projection freshness and governance audit pairing.
3. Reclassify the PRD as historical until its execution snapshot and active hold/corrective backlog are synchronized.
4. Recover per-FR landing-zone/ownership decisions and resolve the FR-16 scope breach.
5. Separate the preserved product-contract catalog from refactor activation scope.
6. Finish or explicitly defer FR-18/FR-19/SM-2 before claiming the next-module payoff is proved.
7. Bind named decision authority and cross-repository ownership/compatibility gates.

## Gate recommendation

**Do not use this PRD to lift the implementation hold, authorize release, generate implementation stories, or certify the thin authoring template.** After the critical and high findings are corrected, rerun validation against the revised PRD, addendum, current sprint/authority bundle, and exact evidence hashes.
