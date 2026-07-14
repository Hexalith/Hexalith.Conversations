---
title: "Legacy root PRD reconciliation extraction"
source: "../../../archive/conversations-product-contract-2026-05-31.md"
target:
  - "prd.md"
  - "addendum.md"
extractionDate: "2026-07-14"
status: "source-extraction"
---

# Legacy root PRD reconciliation extraction

## Purpose and reconciliation rule

This file extracts material from the May 2026 root feature PRD that is not defined by the June/July 2026 boilerplate-reduction PRD or its addendum. It is not an instruction to add the legacy feature roadmap to the refactoring initiative.

The two documents have different intents:

- The legacy root PRD defines the externally observable **Hexalith.Conversations product/feature contract**.
- The current PRD defines an internal **refactoring initiative** whose explicit promise is to preserve the product contract while reducing boilerplate.

Therefore, carry legacy information forward as a namespaced **preserved product-contract baseline** (or an equivalent normative companion section), not as additional refactoring features. Any legacy release status, implementation choice, estimate, or unresolved assumption requires explicit revalidation before it is treated as current scope.

## Explicit conflicts requiring disposition

1. **Document identity and authority.** Current `prd.md` says it is a separate artifact from the feature PRD and links to `../../prd.md`. Archiving the root file without replacing that reference leaves the current PRD with no authoritative definition of “what Conversations does.” The baseline must be embedded, linked at its archive location, or replaced by another canonical feature-contract artifact.
2. **Requirement-ID collision.** The current PRD owns `FR-1` through `FR-20` for refactoring. The legacy PRD owns `FR1` through `FR104` for product behavior. Merging them without a namespace would silently corrupt traceability. Preserve legacy IDs under a namespace such as `Feature-FR1`…`Feature-FR104` and `Feature-NFR1`…`Feature-NFR77`; do not renumber them silently.
3. **Scope and audience.** Current scope is an internal developer-platform refactor with no customer-visible or UI/UX change; legacy scope includes business users, AI agents, compliance operators, SREs, adopter developers, operator UI, governance commands, and release commitments. These are compatible only if legacy content is treated as the behavior that must remain unchanged, not new work authorized by the refactor.
4. **Release state and assumptions.** Legacy content is a planning contract for v1/v1.1/vNext and contains working assumptions/defaults. Current content records measured implementation results dated July 2026 and assumes Conversations is greenfield/pre-release. The current documents do not establish which legacy roadmap items shipped. Preserve behavioral obligations and unresolved decisions, but do not silently claim that legacy phase assignments remain current.
5. **Hosting boundary.** Legacy/current planning language includes module-level Aspire/Dapr/ServiceDefaults hosting work. Repository instructions now state that a domain module must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project and must consume the domain-service SDK instead. Any carried-forward hosting statement must be rewritten as a platform dependency or shared SDK capability, not a Conversations-owned runtime project.

## Material product intent absent from the current PRD

The current PRD names release-gate behaviors but does not preserve the product thesis they protect.

- Conversations owns the durable, tenant-scoped, event-sourced **business record of AI-assisted exchanges** among humans, AI agents, and LLMs. It is not a chatbot, transcript table, provider session store, or LLM orchestration layer.
- The record belongs to the business and remains usable across tools and provider session lifetimes. Provider IDs are attribution metadata, never authority.
- The first adopter is the Hexalith chatbot; the buyer/acceptance authority is the Hexalith platform owner. A second adopter is evidence for the broader substrate claim, not a prerequisite for the basic product contract.
- The differentiating promise is **governance by construction**: fail-closed tenant isolation, paired audit events for governance mutations, idempotent behavior, deterministic replay/redaction behavior, time-correct evidence, and executable release evidence.
- Conversations links to upstream-owned Party, Project, Folder, and file identities by stable ID. Upstream modules own entity state and lifecycle orchestration; Conversations owns the conversation record and resolves current canonical references at read time.
- The qualitative value proposition is that an AI conversation is a durable business artifact comparable to a ticket, contract, or invoice: one memory that can be resumed and proved.

These statements materially constrain what may be deleted as “plumbing.” They should appear in the current package as the reason the preservation gate exists.

## Actors and acceptance journeys absent from the current PRD

The current PRD contains developer refactoring journeys only. The following legacy actors define the observable behavior those developers must preserve:

| Actor / journey | Material preserved acceptance behavior |
|---|---|
| Maya, business user | Resume a multi-day conversation with participants, ordered messages, attachments, and business context intact; no cross-tenant enumeration; provider session expiry does not lose history; warm open target is P95 <= 500 ms under the defined envelope. |
| Atlas, AI agent | Recover full context after provider failure or provider switch; provider correlation IDs remain metadata; Party identity and both providers' attribution remain reconstructable. |
| Sarah, compliance operator | Find by tenant-scoped external identifier/date/business context; read attributed transcript and redactions; inspect inline audit trail; reconstruct prior state; copy citation-ready evidence; receive explicit migration-boundary/empty-state semantics; complete the core investigation within 90 seconds. |
| Diego, adopter developer | Integrate through typed contracts/.NET client without EventStore leakage; receive stable, sanitized typed errors; run adopter-facing conformance tests; preserve semver/deprecation expectations. |
| Marcus, SRE | During audit-sink degradation, governance writes fail closed while eligible non-governance work continues; verification is machine-readable and incident-runnable; privileged tenant-touching actions carry justification and are written into affected tenant audit trails. |
| Julian, platform owner | Accept/reject using concrete buyer evidence: self-serve seeded demo, signed conformance artifact, versioned manifest, and explicit waiver state; partial acceptance and downgrade triggers cannot be hidden. |
| Helen, security reviewer | Independently run adversarial tenant-isolation, stale/missing projection, audit-pairing, redaction-replay, and release-gate checks; distinguish module evidence from platform compliance controls. |
| Naomi, cross-product owner | Stable-ID indirection survives upstream lifecycle changes; Conversations does not own cross-module lifecycle orchestration in v1. |
| Daniel, operations leader | Recover an immutable, time-ordered, attributed record and governance state after harm. Conversations provides provable testimony, not prevention, AI-grounding remediation, or automatic legal hold. |

## Material scope and boundary information

### Product capability baseline

The product contract includes tenant-scoped conversation lifecycle, ordered messages, attributable participants, business references, event-sourced projections, governance/redaction/audit, operator evidence workflows, adopter contracts, compatibility evidence, and tenant-safe observability. Current refactor FR-20 refers to these only generically as “public contracts” and “release-gate behavior”; it does not enumerate them.

### Legacy release slicing that must not be silently adopted or discarded

- Legacy v1 floor: conversation aggregate; chatbot command/read subset; EventStore persistence; fail-closed tenant isolation; sensitive-data/redaction policy; code-level governance enforcement; typed contract/client surface; read-only operator Find/Read/time-travel view; conformance evidence; provider-portability proof; compatibility/deprecation behavior.
- Legacy v1.1 candidates: evidence-bundle export, operator retention editor, full upcasting framework, broader governance analyzer, full temporal property testing, remaining commands/projections, richer FrontComposer metadata, audit-pairing status endpoint, and a non-chatbot reference integration.
- Legacy vNext/anti-scope: semantic/vector memory, summaries, branching, multi-agent planning, attachment binary storage, provider orchestration, real-time collaboration/streaming UI, cryptographic erasure, full compliance automation/legal-hold orchestration, cross-module lifecycle orchestration, and cross-region replication.

The current PRD's “no feature or UI change” boundary means these phase lists should be retained only as provenance/current-state questions unless a separate delivery artifact confirms their status.

## Legacy functional requirements not defined in the current package

All entries below are material to the product baseline. A few are named in the current glossary or FR-20 gate, but none has an authoritative behavioral definition there. Preserve the legacy IDs with a feature namespace.

### Conversation lifecycle — Feature-FR1 through Feature-FR12

- **Feature-FR1:** Create a tenant-scoped conversation.
- **Feature-FR2:** Give it a stable tenant-scoped internal identity distinct from external IDs, provider IDs, labels, or thread names.
- **Feature-FR3:** Represent lifecycle states and permitted transitions, including active and archived/closed plus any approved reopening/sealing behavior.
- **Feature-FR4:** Append ordered messages.
- **Feature-FR5:** Add human, AI-agent, and LLM participants.
- **Feature-FR6:** Accept idempotent commands and return stable duplicate outcomes.
- **Feature-FR7:** Reject invalid, unauthorized, conflicting, duplicate, unsupported-version, or tenant-mismatched commands with documented typed failures.
- **Feature-FR8:** Retrieve the conversation with participants, ordered messages, attachment references, governance state, and freshness context.
- **Feature-FR9:** List tenant conversations by business context, external identifier, or recency.
- **Feature-FR10:** Update title/metadata when included in release scope.
- **Feature-FR11:** Close/archive when included in release scope.
- **Feature-FR12:** Preserve the complete record across provider expiry, restart, or failover.

### Participant attribution — Feature-FR13 through Feature-FR18

- **Feature-FR13:** Attribute each action to a stable Party identity.
- **Feature-FR14:** Model humans, AI agents, and LLMs as attributable participants.
- **Feature-FR15:** Keep provider correlation IDs as metadata, not source of truth.
- **Feature-FR16:** Keep provider payload metadata only as opaque, tenant-isolated, explicitly versioned extension data.
- **Feature-FR17:** Preserve multi-provider attribution across provider boundaries.
- **Feature-FR18:** Reconstruct who said or changed what, when, and in which tenant context.

### Business context and references — Feature-FR19 through Feature-FR25

- **Feature-FR19:** Attach file references without storing binaries in Conversations.
- **Feature-FR20:** Link projects/folders and other upstream entities by stable ID.
- **Feature-FR21:** Store external business identifiers for later tenant-scoped discovery.
- **Feature-FR22:** Distinguish external correlation keys from upstream domain references.
- **Feature-FR23:** Resolve Party, Project, Folder, and attachment references from upstream canonical state at read time.
- **Feature-FR24:** Keep the record readable and attributable when upstream entities change lifecycle state.
- **Feature-FR25:** Explain migration/coverage boundaries, including coverage start or handoff target.

### Tenant access and isolation — Feature-FR26 through Feature-FR32

- **Feature-FR26:** Require tenant context on every command, event, projection, query, pub/sub message, and audit record.
- **Feature-FR27:** Reject before aggregate/projection access when tenant binding is missing, malformed, stale, lagging, rolled back, ambiguous, mismatched, or unknown.
- **Feature-FR28:** Prevent cross-tenant enumeration and existence disclosure.
- **Feature-FR29:** Make unauthorized, nonexistent, and cross-tenant records indistinguishable unless policy explicitly allows disclosure.
- **Feature-FR30:** Return typed tenant-binding/isolation errors suitable for adopters.
- **Feature-FR31:** Attribute SRE/operator actions and write them into each affected tenant's audit trail.
- **Feature-FR32:** Publish tenant-aware events/notifications without leaking tenant metadata through topics, envelopes, IDs, errors, or negative results.

### Event sourcing, projections, and publication — Feature-FR33 through Feature-FR41

- **Feature-FR33:** Derive projections from ordered events.
- **Feature-FR34:** Expose replay position, projection version, or equivalent freshness metadata.
- **Feature-FR35:** Rebuild v1 projections to functionally equivalent read models for the same stream, scope, and contract version.
- **Feature-FR36:** Define current/stale/rebuilding/unavailable/tenant-hidden consistency states.
- **Feature-FR37:** Expose lag or documented freshness behavior for asynchronous reads.
- **Feature-FR38:** Publish meaningful domain events under the active contract.
- **Feature-FR39:** Carry explicit event schema/version metadata.
- **Feature-FR40:** Reject unsupported command/event/projection versions with typed failures.
- **Feature-FR41:** Define evolution, unsupported-version, migration, and upcaster boundaries.

### Governance and audit — Feature-FR42 through Feature-FR55

- **Feature-FR42:** Authorized systems set/replace retention policy with rationale.
- **Feature-FR43:** Authorized systems mark sensitive content.
- **Feature-FR44:** Authorized systems redact message content with actor, time, rationale, and policy attribution.
- **Feature-FR45:** Distinguish archive/logical deletion, retention, redaction, legal-hold deferral, and immutable audit/event history.
- **Feature-FR46:** Preserve audit events while redacting projected/displayed content.
- **Feature-FR47:** Pair every governance mutation with an audit event.
- **Feature-FR48:** Reject governance mutations if audit recording is unavailable.
- **Feature-FR49:** Permit non-governance activity during audit degradation only if it does not mutate governance state.
- **Feature-FR50:** Reconstruct message and governance state at a prior point in time.
- **Feature-FR51:** Make audit records citeable with stable IDs, timestamps, actor, tenant, conversation, and integrity metadata.
- **Feature-FR52:** Apply retention/redaction treatment to audit records themselves.
- **Feature-FR53:** Define allowed, denied, redacted, exported, and separately logged audit-record actions.
- **Feature-FR54:** Require structured justification for privileged operations touching tenant conversation data.
- **Feature-FR55:** Present privileged justification, actor, time, tenant, conversation, policy basis, and audit result coherently.

### Operator and compliance workflows — Feature-FR56 through Feature-FR69

- **Feature-FR56–FR57:** Find tenant conversations by external ID and narrow by date/business context.
- **Feature-FR58:** Read a reconstructed transcript with participants, messages, attachments, redactions, governance, tenant, policy, and freshness context.
- **Feature-FR59–FR60:** Inspect attributed inline redactions and the inline governance audit trail.
- **Feature-FR61:** View historical state.
- **Feature-FR62–FR63:** Copy citation-ready references and open stable temporal evidence links anchored by the contract.
- **Feature-FR64:** Ensure read-only workflows cannot mutate aggregate state.
- **Feature-FR65:** Classify and separately audit privileged mutations.
- **Feature-FR66–FR68:** Run governance verification by conversation/tenant/suite/window, return structured results for named invariants, and separate product failures from execution/infrastructure failures.
- **Feature-FR69:** Provide a seeded buyer demo covering redaction, time travel, citation copying, and cross-tenant denial.

### Consumer contracts and developer experience — Feature-FR70 through Feature-FR80

- **Feature-FR70–FR71:** Publish typed commands/projections/events/errors and a supported .NET client unless raw HTTP is explicitly accepted.
- **Feature-FR72:** Support a minimal create → append → read happy path.
- **Feature-FR73:** Supply adopter-runnable predeployment conformance tests.
- **Feature-FR74–FR75:** Document tenant/Party/idempotency/error/freshness/publication/governance semantics and expose active compatibility/version status.
- **Feature-FR76:** Preserve caller/composer/origin provenance metadata needed for attribution and composition.
- **Feature-FR77–FR79:** Provide actionable diagnostics, remediation guidance, and explicit CORE preconditions.
- **Feature-FR80:** Return sanitized typed errors with audit handle and documentation pointer without leaking inaccessible tenant, Party, conversation, content, provider payload, or references.

### Compatibility, evidence, and release gates — Feature-FR81 through Feature-FR94

- **Feature-FR81:** Publish additive/breaking/deprecation/minimum-version compatibility policy.
- **Feature-FR82–FR85:** Produce a signed conformance artifact; versioned release manifest; requirement/test traceability; named waiver process.
- **Feature-FR86:** Classify gate failures as blocking/non-blocking for isolation, audit, compatibility, rebuild, portability, documentation, and schemas.
- **Feature-FR87–FR91:** Verify adversarial tenant isolation, duplicate/reordered idempotency, redaction replay across all surfaces, provider-independent recoverability, and schema evolution with a worked additive example.
- **Feature-FR92–FR93:** Run executable command/query/event/error/version contract tests with at least one adopter-style CORE fixture.
- **Feature-FR94:** Separate Conversations module evidence from inherited platform compliance evidence.

### Observability and operations — Feature-FR95 through Feature-FR99

- **Feature-FR95–FR99:** Observe sanitized command rejection reasons, projection lag/rebuild/availability, event-publication/contract failures, tenant-isolation denials/privileged attempts, and conformance/verification status for incidents and CI.

### Scope/lifecycle commitments — Feature-FR100 through Feature-FR104

- **Feature-FR100:** Classify capabilities per release as v1, v1.1, vNext, deferred, waived, or conditional.
- **Feature-FR101:** Expose the consequence of deferring substrate-defining capabilities.
- **Feature-FR102:** Support explicit buyer partial acceptance under the selected release deal.
- **Feature-FR103:** Track second-adopter status and downgrade-review milestones.
- **Feature-FR104:** Document ownership boundaries among Conversations, chatbot/provider, legal hold, attachments, identity, tenants, projects/folders, and upstream lifecycle.

## Legacy NFRs not defined in the current package

Current §8 contains five broad statements. The legacy PRD contains the measurable and operational detail needed to interpret “100% conformance” and “no material regression.” Preserve these under a feature-NFR namespace, while revalidating numeric release status.

### Evidence and waiver discipline — Feature-NFR1 through Feature-NFR8

- **Feature-NFR1–NFR3:** Every NFR names verification artifact/lifecycle stage; every gated NFR maps to automated evidence and pass/fail/waived/unknown-accepted status; numeric targets define method, environment, and pass/fail meaning.
- **Feature-NFR4–NFR6:** Unresolved capacity/latency numbers must be fixed or explicitly accepted by a named buyer; numeric targets have blocker/validation/discovery classification; misses require approver, expiry, compensating control, and customer-facing buyer acceptance.
- **Feature-NFR7–NFR8:** Define a shared measurement envelope and record environment, scale, tools, build hash, schema versions, timestamps, and manifest references in evidence.

### Performance — Feature-NFR9 through Feature-NFR15

- **Feature-NFR9:** Warm full-context open target: P95 <= 500 ms at <=500 messages, <=20 humans, <=5 AI agents, and 50 concurrent opens/sec/tenant.
- **Feature-NFR10–NFR11:** State what the latency includes and measure cold start separately.
- **Feature-NFR12–NFR13:** Complete defined operator investigations within 90 seconds and define backend/freshness/explainability thresholds that support that workflow.
- **Feature-NFR14–NFR15:** Benchmark append under duplicate/idempotent load and distinguish acceptance, persistence, audit, publication, and projection-visible milestones.

### Security and privacy — Feature-NFR16 through Feature-NFR21

- **Feature-NFR16–NFR18:** Tenant isolation is release-blocking and must fail closed across reads, writes, search, replay/rebuild, diagnostics, audit, and admin operations, including adversarial cases.
- **Feature-NFR19:** No sensitive/existence/tenant/provider leakage through errors or telemetry.
- **Feature-NFR20:** Audit-unavailable governance writes fail closed and cannot be queued unaudited.
- **Feature-NFR21:** Redacted content cannot reappear in projections, indexes, audit views, caches, exports, temporal/replay outputs, logs, traces, errors, or telemetry.

### Reliability, resilience, and recovery — Feature-NFR22 through Feature-NFR29

- **Feature-NFR22–NFR23:** Duplicate/reordered/retried commands and at-least-once delivery must not diverge state or duplicate business effects; test deduplication-window expiry.
- **Feature-NFR24–NFR27:** Define publication retry/dead-letter/replay/notification behavior; drill sidecar/store/rebuilder/lag/dead-letter/audit/redaction failures; maintain a failure-mode matrix; distinguish invariant failure from test infrastructure failure.
- **Feature-NFR28–NFR29:** Define RPO/RTO for events, projections, audit evidence, and replay configuration; test backup restore and tenant-scoped recovery.

### Scalability, capacity, and cost — Feature-NFR30 through Feature-NFR37

- **Feature-NFR30:** Set or explicitly accept unknowns for events/sec, concurrent conversations, write amplification, and concurrent tenant opens.
- **Feature-NFR31–NFR33:** Measure rebuilds at 1M/10M/100M events with tiered thresholds and support progress, resume, and tenant-safe cancellation/isolation.
- **Feature-NFR34–NFR35:** Define tenant-event lag and redaction-propagation SLOs with request behavior.
- **Feature-NFR36–NFR37:** Expose storage/write/rebuild/pub-sub/per-tenant cost indicators and set or accept cost thresholds.

### Data integrity and event sourcing — Feature-NFR38 through Feature-NFR43

- **Feature-NFR38–NFR39:** Rebuild equivalent deterministic projections/evidence from the same ordered event stream, excluding only documented non-persisted runtime metadata.
- **Feature-NFR40–NFR41:** Version persisted/published events, reject unsupported versions, and prove one additive evolution before GA.
- **Feature-NFR42–NFR43:** Define the authoritative temporal anchor and make temporal links resolve to the same legally meaningful state.

### Projection freshness — Feature-NFR44 through Feature-NFR48

- **Feature-NFR44–NFR46:** Use a consistent freshness shape across APIs/UI/diagnostics/verification and define current, stale, rebuilding, unavailable, and tenant-hidden semantics.
- **Feature-NFR47–NFR48:** Human surfaces distinguish normal/delayed/degraded/blocked/redacted/replaying/partial states and show last good state, completeness, scope, time, recommended action, and whether intervention is required.

### Integration and compatibility — Feature-NFR49 through Feature-NFR54

- **Feature-NFR49:** Executable compatibility tests cover commands, reads, events, errors, version discovery, and an adopter-style CORE fixture.
- **Feature-NFR50–NFR52:** Provider-portability tests strip/change provider IDs and cover contract, persistence, pub/sub, rebuild, and observability while preserving tenant/idempotency/order/audit/replay invariants.
- **Feature-NFR53–NFR54:** .NET client/contract errors match raw service semantics; FrontComposer metadata remains provenance, not coupling to one UI.

### Operability and observability — Feature-NFR55 through Feature-NFR61

- **Feature-NFR55–NFR58:** Observe named operational signals safely, with bounded cardinality and no high-cardinality content/identifier dimensions.
- **Feature-NFR59:** Governance/conformance output is machine-readable for CI/incidents.
- **Feature-NFR60–NFR61:** Privileged operations carry justification and are periodically reviewed; stale/unexplained access is an audit finding.

### Compliance, retention, and release evidence — Feature-NFR62 through Feature-NFR68

- **Feature-NFR62:** Isolation, audit integrity, redaction leakage, unsupported schemas, rebuild nondeterminism, and contract breakage automatically block release unless explicitly waived.
- **Feature-NFR63–NFR64:** Every release emits signed, traceable conformance evidence and separates module controls from inherited platform controls.
- **Feature-NFR65–NFR67:** Test audit-record access/export/redaction/tamper/privileged views; define retention/archive/delete/legal-hold across all materializations; make retention tenant-aware and evidenced.
- **Feature-NFR68:** Non-developer approvers can navigate summarized evidence while machine-readable artifacts remain authoritative.

### Accessibility and human trust — Feature-NFR69 through Feature-NFR77

- **Feature-NFR69–NFR74:** Operator/admin web surfaces meet WCAG 2.1 AA expectations, do not rely on color, support keyboard-only evidence workflows, and announce meaningful state changes to screen readers; verify automatically and manually.
- **Feature-NFR75:** Operators diagnose delayed/blocked projections and admins review failed release evidence correctly within 90 seconds without developer assistance.
- **Feature-NFR76–NFR77:** Fail-closed/degraded messages remain content-safe, identify class/operation/retryability/escalation, and clearly distinguish safe, stale, hidden, unavailable, and governance-blocked states without panic-inducing ambiguity.

## Constraints and qualitative requirements to preserve

- Fail closed before data access, not after a query has revealed existence.
- Tenant scoping is structural and persistent; privileged tools do not gain a hidden cross-tenant bypass.
- Governance audit pairing is enforced by code/runtime/test mechanisms, not reviewer procedure alone.
- Redaction preserves an immutable audit history while preventing redacted payload rematerialization anywhere user- or operator-visible.
- Event-sourced replay, schema evolution, and temporal evidence are product semantics, not merely implementation details.
- Provider portability is a tested recoverability property, not a provider abstraction claim.
- Public clients hide EventStore mechanics and use typed, sanitized, actionable failures.
- Stable-ID indirection preserves attribution across upstream lifecycle changes; upstream modules own current identity/entity state and orchestration.
- Operator evidence must be citeable and temporally stable, with visible freshness/degraded-state trust signals.
- The product promises honest records and evidence; it does not promise correct AI advice, harm prevention, chatbot orchestration, automatic legal hold, or full regulatory automation.
- Attachment binaries remain owned by `Hexalith.Folders`; tenant identity/roles remain owned by `Hexalith.Tenants`; Party identity remains owned by `Hexalith.Parties`; persistence/runtime plumbing comes from the platform SDK.

## Unresolved legacy questions requiring explicit close/supersede decisions

The current PRD does not explicitly dispose of these legacy questions. Defaults in the old file are not equivalent to approved current decisions.

### Product/governance questions

1. Does migrated/pre-UI-rollout event history contain sufficient attribution? If not, restrict the claim, backfill, or document the coverage boundary.
2. Is the signed conformance manifest plus named-waiver process an explicit release commitment?
3. Is Generate Evidence Bundle out of v1 and in v1.1, with only read-only Find/Read in v1?
4. What chatbot deadline constrains the work, and is chatbot release blocked on Conversations?
5. Who owns and must sign any public downgrade from “substrate backbone” framing? Legacy default/claim: Jerome.

### API/architecture questions

6. HTTP only or HTTP plus gRPC?
7. Consumer-supplied or service-derived idempotency key?
8. Exact status/retry semantics for stale tenant projections?
9. Pub/sub topic naming and whether the EventStore convention is sufficient?
10. Pull or push semantics for audit-pairing health status?

### Pre-kickoff/release questions

11. May v1 use raw HTTP if the .NET client misses GA?
12. Is the EventStore envelope stable/inherited or changed in this initiative?
13. Does another module consume Conversations events in the relevant release?
14. Is staffing sufficient for the old 16–18-week feature estimate, and is that estimate still relevant?
15. Is there a named second-adopter candidate and what evidence qualifies?
16. Is the Foundation Gate blocking/waiver definition ratified?
17. Are sensitive-data marking/redaction commands mandatory in the chatbot CORE path?

### Overlap with current open questions

- Current OQ-5 (“explicit hot-path budget or no regression?”) must reconcile legacy Feature-NFR9's concrete P95 envelope and Feature-NFR12's 90-second operator target. It cannot silently downgrade them to benchmark noise.
- Current OQ-3 (keep governance/temporal/hydration) must preserve their domain semantics even if generic orchestration is not promoted.
- Current OQ-1 (promotion landing zones) must honor the repository's platform-ownership rule and cannot create Conversations-owned hosting boilerplate.

## Minimal carry-forward recommendation

To archive the root PRD without losing its contract:

1. Add a clearly labeled “Preserved Conversations Product Contract Baseline” to the current package or a normative companion artifact.
2. Include the product intent, actor acceptance journeys, scope boundaries, `Feature-FR1`–`Feature-FR104`, `Feature-NFR1`–`Feature-NFR77`, and unresolved-question disposition table above.
3. State that the baseline constrains current FR-20/SM-C1 and does not expand refactoring scope.
4. Record every legacy item as `preserved`, `superseded by <artifact/decision>`, or `open`; never infer shipped state from the old v1/v1.1 labels.
5. Update the current PRD's link to the feature contract before moving the legacy file, and retain the archived original for provenance.
