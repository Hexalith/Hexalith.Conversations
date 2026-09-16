# V15 Reconciliation Against The 2026-09-16 Validation

- **Candidate:** `_bmad-output/planning-artifacts/architecture.md` through the V15 overlay
- **Inputs:** `validation-report-2026-09-16.md` and all four reviews under `reviews/validate-2026-09-16/`
- **Scope:** reconciliation only; this review does not amend the candidate
- **Verdict:** **NEEDS FIXES — V15 is fail-closed and materially closes the runtime decisions, but two inherited authority rules remain internally inconsistent and one verification obligation has no owned landing gate.**

## Executive Verdict

V15 correctly preserves V1-V14, selects `ACTIVE` as the current Story 7.1
state, and lands the missing Epic 16 decisions for atomic tenant mutation,
lifecycle/watermark ownership, replay-visible time, and tenant-gap recovery.
It also makes the vocabulary, AppHost, SPEC, conformance-tier, UI-version, and
accessibility items explicit owned deferrals rather than silently deciding
them.

The candidate is not yet a fully convergent current authority for three
reasons:

1. V15 points at the already-published V21 sidecar without publishing a
   successor sidecar in the same transaction, while V13 and AD-3 still say
   either artifact alone is an authority-publication failure. This repairs
   discoverability and produces the safe result, but does not repair the
   chain under its own publication rule.
2. AD-5 narrows V13's operational-envelope condition from **any** hold lift to
   Story 16.1 and product/runtime/release lifts, but V15 says inherited rules
   survive unless explicitly superseded and AD-5 never explicitly supersedes
   the broader V13 sentence or explains the planning-only exception. Both
   gates therefore remain live.
3. AD-9 decides the workflow route set, but V15 neither assigns nor gates the
   successor workflow inventory/current-head resolver test. The direct V9 test
   and publisher remain stale against the rule V15 declares current.

These are authority/publication and enforceability defects, not evidence that
V15 accidentally lifted a hold. Its explicit current result remains
fail-closed: Story 7.1 is `ACTIVE`.

## Findings That Did Not Fully Land

### RV-1 — HIGH — The pointer repair still violates the retained same-commit publication rule

**Source findings:** C1; rubric F1; reality F1; adversarial ADV-1; authority
AUTH-01/AUTH-02.

V15's Current Authority State and AD-3 successfully distinguish technical
architecture, immutable bundle evidence, recorded sidecar effect, and the
recomputed current hold. The exact V21 digest is correct, the current
`V21_DESCENDANT_GITLINK_DRIFT` result is recorded, and every failed or missing
check resolves to `ACTIVE`. This closes the unsafe precedence ambiguity.

The claimed chain repair is weaker than the input required. V13 says a new
checkpoint sidecar and its architecture pointer must be published in the same
commit and that either artifact alone is an authority-publication failure.
AD-3 repeats that rule. V15 now publishes only the late pointer to the existing
V21 artifact; it does not mint the successor authority requested by the
validation's update order. The Deferred table confirms that the current-head
successor remains future work. Thus V21 becomes discoverable, but the document
does not say how a pointer-only recovery can cure the publication failure it
continues to define.

**Required correction:** choose one explicit rule in V15:

- publish the current-head successor sidecar with the V15 pointer in the same
  transaction; or
- declare V15 a one-time fail-closed recovery amendment that ratifies V15-V21
  only as immutable historical predecessors, expressly supersedes the
  same-commit rule for this recovery case, and denies those records live effect
  until the next normally paired successor passes.

Keep the current effective hold `ACTIVE` either way. Do not describe V21 as a
valid current execution head merely because its checker can be invoked.

### RV-2 — HIGH — AD-5 does not explicitly supersede V13's broader operational-envelope precondition

**Source findings:** C2; rubric F2; adversarial ADV-6.

AD-5 names an owner, a counterpart path, required contents, a waiver signer,
and prospective entry gates. That is a valid late disposition of the missing
operational dimension. It also correctly refuses to turn the historic
V17/V20/V21 effects into current authorization.

However, V13's live rule says the operational envelope must be decided,
deferred with an owner, or delegated before **any hold-lift decision**. AD-5
replaces that with a gate before Story 16.1 and before any
product/runtime/release lift. V15's own inheritance sentence preserves every
invariant it does not explicitly supersede; AD-5 never says that it supersedes
V13's broader phrase. Calling the past lifts “planning-only” does not by itself
define the category or explain why full Story 7.1 execution was exempt.

The two rules therefore produce incompatible future entry decisions. A reader
following V13 blocks every lift until the counterpart exists; a reader
following AD-5 may permit a planning/story lift without it.

**Required correction:** explicitly state whether AD-5 supersedes the V13 Open
Dimension gate. If it does, define `planning-only` in terms of allowed outputs
and prohibited product/runtime/release effects, explain why Story 7.1 falls in
that category, and state that the amendment does not retroactively validate an
otherwise-invalid lift. If it does not, retain the broader gate and remove the
narrower implication.

### RV-3 — MEDIUM — The new workflow/currentness rules have no owned verification landing gate

**Source findings:** H2; reality F3; authority AUTH-04; rubric F7.

AD-9 makes the right architecture decision: the removed `bmad-dev-auto` and
`bmad-quick-dev` aliases are retired, and exact technology versions are
code-owned seed. AD-3 supplies the desired current-state resolver semantics.
This resolves what the route and resolver *should mean*.

The validation also found that the tracked V9 publisher and direct
`PlanningAuthorityV9ValidationTest` still require the deleted aliases, and
that no current-head test walks the selected pointer/predecessor chain and
asserts the recomputed hold. V15 does not assign those tool changes to an
owner or put them behind a revisit gate. AD-10's generic enforcement ledger is
not a substitute for the specifically required current-authority resolver.

**Required correction:** add an owned deferred row or rule requiring a
successor workflow-inventory authority/checker and a current-head resolver test
before planning publication or Story 7.1 resumption. Preserve historical V9
validation at its bound candidate; do not make a historical publisher pretend
to validate the current route set.

## Critical/High Finding Coverage

| Validation item | V15 landing | Reconciliation |
| --- | --- | --- |
| C1 / ADV-1 / AUTH-01/02 — total current authority and safe hold | Current Authority State + AD-3 select V15, exact V21, protected recomputation, and `ACTIVE` on every non-pass | **Partial:** safe precedence lands; same-commit publication repair remains RV-1 |
| C2 / ADV-6 — operational envelope before hold lifts | AD-5 plus the Deferred table name the artifact, owner, scope, waiver signer, and gates | **Partial:** narrowed rule does not explicitly supersede V13; RV-2 |
| C3 / ADV-2 — atomic tenant mutation | AD-6 binds Tenants ownership, event identity, next-sequence rule, CAS/transaction, bounded attempts, mismatch/gap handling, fail-closed behavior, recovery, and concurrent conformance schedules | **Landed** |
| H1 / rubric F3 — Story 7.1 terminal/integration transition | AD-4 binds protected merge-candidate evaluation, integration paths, post-integration recheck, final-record consumption, descendant-policy retirement, and the Story 7.2 lock | **Landed as a fail-closed successor contract** |
| H2 / reality F3 — stale workflow aliases | AD-9 retires the aliases and selects `bmad-build` / `bmad-build-auto` | **Decision landed; executable verification ownership remains RV-3** |
| H3 / reality F4 — AppHost baseline conflict | Deferred table keeps the brownfield fixture non-authoritative and requires a baseline-owner exemption or relocation before Epic 12/Story 16.3 | **Landed as an explicit human-owned deferral; no silent exemption** |
| H4 / ADV-3 — lifecycle/watermark ownership | AD-7 selects the Conversations index v2, existing key, sole writer, shared CAS boundary, TenantCreated initialization, tombstone/rebuild behavior, v1 rebuild migration, and exact read mappings | **Landed** |
| H5 / ADV-4 — replay-visible time | AD-8 selects `ConversationEventMetadata.OccurredAt` for Conversations events and envelope `Timestamp` only for position-only events; it fixes normalization and the exact degraded mapping | **Landed**; `CommittedAt` is a legacy alias of `OccurredAt`, so this does not invent a competing timestamp |
| H6 / ADV-5 — missing vocabulary sidecar | Deferred table assigns Quality and preserves the pre-review blocker and closed public value set | **Landed as the safe deferral requested by the review** |
| H7 / rubric F4 — stale canonical SPEC | Deferred table assigns `bmad-spec` refresh before downstream planning and states that the old V9/UNBOUND/global-ACTIVE claims are historical | **Landed as an upstream correction gate; the architecture does not rewrite the SPEC** |
| H8 / AUTH-03 — missing `statusAsOf` and currentness qualifier | AD-3 requires successor fields and qualifies V15-V21 as immutable candidate-time records | **Landed for the spine; existing records remain immutable** |
| H9 / reality F2 / rubric F9 — stale exact versions | AD-9 delegates exact pins to `global.json`, the central catalog, and current package authority while preserving compatibility/alignment rules | **Landed** |

## Other Quiet Constraints

- **Tenant-gap recovery:** ADV-7 is not lost. AD-6 assigns authoritative replay
  and unsafe-state clearing to Tenants and expressly prevents Conversations or
  query paths from repairing it.
- **Current capability placement:** rubric F5/F8 land in the Current Capability
  And Boundary Map without reproducing a full source tree.
- **Public-boundary contradiction:** rubric F6 lands in AD-10, which scopes the
  prohibition to internal persistence concepts while retaining
  Conversations-owned public contracts.
- **Pattern enforceability:** rubric F7 lands as a Quality-owned ledger and an
  advisory-until-enforced rule. The specific authority/workflow checks still
  need RV-3's owned gate.
- **Conformance tier:** reality F5 is safely deferred to Epic 9 and explicitly
  classifies the current Server-referencing project as module-internal.
- **Fluent UI V5 and WCAG:** reality F6/F7 remain owned pre-activation choices;
  V15 does not convert an RC or a newer recommendation into an unapproved
  product requirement.
- **Human discoverability:** rubric F10 is materially improved by stable AD
  entries and compact capability/deferred tables, while append-only provenance
  remains intact.

## Scope And Overreach Check

No overreach beyond the confirmed spine-only deliverable was found. V15 changes
only `architecture.md`; it does not edit product source, dependencies,
submodules, gitlinks, the SPEC, UX scope, sidecar records, hold records, story
status, release evidence, or external systems. The named operational-envelope,
vocabulary, enforcement-ledger, SPEC, AppHost, conformance, and UI artifacts are
future owned deliverables with entry gates, not artifacts silently created by
this update.

AD-6 through AD-8 are appropriately architectural despite their specificity:
they fix cross-unit atomicity, ownership, persisted authority, and deterministic
time semantics that two independent builders could otherwise choose
incompatibly. They do not prescribe a full implementation tree.

## Reconciliation Gate

**Do not call the V15 update fully reconciled until RV-1 and RV-2 are corrected.**
RV-3 may remain deferred only if its owner and pre-publication/pre-resumption
gate are recorded in the spine. With those changes, the 2026-09-16
critical/high validation set is either decided or safely deferred, and the
spine remains within the user's architecture-only scope.
