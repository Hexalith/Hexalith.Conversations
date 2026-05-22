# Test Automation Summary

## Story 3.4 Copy Citations and Stable Temporal Evidence Links

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationCitationContractTest.cs` - Added citation DTO/result serialization coverage for safe copied text, safe labels/accessibility text, audit-handle inclusion, temporal cursor metadata, and forbidden EventStore/provider/storage/personal-data/browser-selection vocabulary.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs` - Extended temporal anchor coverage for composite authoritative anchors carrying safe source position, projection cursor, projection version, and supporting timestamp while rejecting mismatched composite cursors.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for citation target, citation query, citation DTO, and citation result.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added citation query coverage for authorized safe DTO construction, redacted target placeholder/attribution output, missing audit-handle downgrade, denied/missing/stale/cross-tenant projection fail-closed behavior, no original message text, and tenant authorization/projection read boundaries.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs` - Updated deterministic temporal re-resolution assertions for composite authoritative anchors with projection cursor/version metadata and mismatched projection-version cursor fail-closed behavior.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added authorized route metadata and read API coverage for citation and temporal routes, trusted claim binding, malformed target/cursor hidden equivalence, strict malformed projection-cursor rejection, unsafe query-string value exclusion, and citation permission-downgrade clearing of clipboard/link metadata.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~TemporalReconstruction|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 30 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 72 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 183 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 678 passed.

### Coverage
- Citation copy now resolves through a Conversations-owned `ConversationCitationAccessService` after tenant/caller authorization and current projection freshness recheck; DTO output is built from governed evidence metadata rather than rendered/client text.
- Citation contracts expose schema, tenant, conversation, evidence id/kind, timestamp, actor PartyId, audit handle when ready, projection cursor/version, temporal cursor, safe copied text, safe labels, and safe next action without raw EventStore/provider/storage or original redacted content.
- Missing audit handles, missing/deleted evidence entries, redacted targets, stale projections, denied callers, cross-tenant projection poison, malformed targets, permission downgrades, and malformed temporal cursors fail closed with hidden/unavailable/rebuilding shapes rather than trusted citation output.
- Temporal reconstruction now returns a composite authoritative anchor containing safe source position plus projection cursor/version; timestamps remain supporting metadata only.
- HTTP surfaces remain read-only under the existing authorized `/api/v1/conversations` group. UI E2E is not applicable for Story 3.4 because there is still no Admin/FrontComposer project in this repository.

### Checklist Validation
- [x] API and backend E2E-style read-path tests generated for the implemented citation and stable temporal-link feature.
- [x] UI E2E tests assessed as not applicable because Story 3.4 is implemented as backend contracts/server/API read behavior in this repository.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, and existing fake-store patterns.
- [x] Tests cover happy path plus missing audit handle, malformed cursor, stale projection, redacted target, missing/deleted evidence, outside coverage, unsupported schema, cross-tenant link/projection poison, unauthorized-existing record, permission downgrade, and clipboard/browser/accessibility metadata safety.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes coverage metrics.

## Story 3.3 Inspect Redaction Attribution and Governance Audit Trail

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs` - Added redaction attribution contract coverage for safe JSON shape, safe labels/accessibility text, audit-handle linkage, missing-audit incomplete state, canonical placeholder enforcement, target-key consistency, visible-text consistency, readiness consistency, and forbidden original-content/provider/storage vocabulary.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixture coverage for `ConversationRedactionAttributionV1`.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added projection coverage proving redacted message evidence carries inline attribution, redaction evidence links to the same audit handle, governance evidence anchors expose safe detail metadata, chronological evidence ordering remains stable, and redacted content stays suppressed.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added route coverage proving audit-detail reads are under the authorized `/api/v1/conversations` group, bind tenant/caller only from trusted claims, ignore caller-supplied authority/action query data, return safe detail JSON, hide malformed handles without projection reads, hide missing trusted-tenant claims without projection reads, coarsen unexpected audit store failures to safe unavailable responses, and clear protected audit detail after a permission downgrade.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs` - Added gap coverage proving redaction audit records with missing audit anchors return hidden detail while preserving redacted placeholders, and unexpected audit source failures return content-safe unavailable results.
- [x] Existing `ConversationAuditRecordAccessServiceTest` and `ConversationQueryHandlerTest` coverage exercised independent audit authorization, stale/rebuilding/unavailable/cross-tenant/malformed handle states, redaction audit detail, and safe policy treatment.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationEvidence|FullyQualifiedName~Redaction|FullyQualifiedName~AuditRecord|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 32 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 94 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~Temporal|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationQueryRegistrationTest"` - 152 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 665 passed.

### Coverage
- Governed evidence entries now carry server-owned safe audit target/link metadata and optional inline redaction attribution without original content, raw snippets, provider payloads, upstream Party details, EventStore topology, storage locations, or audit sink details.
- Redacted message entries and redaction evidence entries link to the same safe audit evidence reference when present; missing audit metadata remains explicit through incomplete readiness instead of becoming ready/current by default.
- Redaction placeholders are canonical `[redacted]` markers, redaction attribution target keys must match governed targets, and redacted evidence visible text plus audit readiness must stay consistent with attached attribution.
- Missing redaction audit anchors, missing trusted tenant claims, audit store failures, and permission-downgraded inline audit detail refreshes fail closed without retaining protected policy basis, audit evidence handles, raw failure terms, or detail payloads.
- Retention, sensitivity, and redaction evidence records expose stable target, actor, timestamp, policy basis, rationale class, trust state, audit readiness, safe labels, and next-action metadata for a future trust component without adding an Admin shell.
- Audit detail reads are exposed through the existing authorized read API/query boundary and continue to rely on tenant authorization, current projection freshness, malformed-handle hiding, policy-blocked shapes, and content-safe unavailable/rebuilding results.
- UI E2E tests are not applicable for Story 3.3 because this repository still has no Admin/FrontComposer project for this slice; the implemented scope is contracts, server projection/query behavior, API route, and safety tests.

### Checklist Validation
- [x] API and backend E2E-style read-path tests generated for the implemented redaction/audit inspection feature.
- [x] UI E2E tests assessed as not applicable because Story 3.3 is implemented as backend contracts/server/API read behavior in this repository.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, and existing fake-store patterns.
- [x] Tests cover happy path plus unauthorized audit, redacted evidence, missing audit anchor, stale projection, malformed handle, permission downgrade, accessibility-label contract, and safe hidden/unavailable states.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes coverage metrics.

## Story 3.2 Governed Conversation Evidence Read

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationEvidenceContractTest.cs` - Added governed detail contract coverage for trust posture, evidence entries before message timeline data, fail-closed command eligibility defaults, explicit unavailable metadata defaults, and forbidden infrastructure/provider/session/transcript vocabulary checks.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for governed evidence trust posture, evidence entries, and command availability metadata.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added projection-owned trust posture/evidence-entry assertions, evidence kind coverage for messages/participants/attachments/freshness, chronological message evidence ordering, and redaction placeholder preservation in evidence entries.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadServiceTest.cs` - Added QA follow-up coverage proving stale, rebuilding, unavailable, and redacted detail projections do not become trust-bearing reads.
- [x] `tests/Hexalith.Conversations.Server.Tests/Hydration/ConversationReadHydrationServiceTest.cs` - Added detail participant-resolution downgrade coverage while preserving projection-owned evidence completeness and citation availability.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added governed detail query assertions for tenant scope, record identity, temporal cursor, command eligibility, missing citation metadata, partial evidence metadata, and evidence entry propagation after authorized projection read/hydration.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added existing detail route coverage proving authorized reads return trust posture, evidence entries, command eligibility, malformed route values fail hidden without projection reads, trusted claims are used instead of caller-supplied authority, and no unsafe EventStore/provider-session/transcript terms are exposed.
- [x] AI review follow-up - Added regression coverage for full evidence-entry chronology across participant/message/attachment/freshness records, forbidden participant-resolution aggregation precedence, and projection store exception coarsening for detail/list reads.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 20 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationReadHydrationServiceTest|FullyQualifiedName~ConversationReadApiTest"` - 93 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Temporal|FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryRegistrationTest"` - 148 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 653 passed.

### Coverage
- Detail projections and query details now expose source-owned trust posture with tenant scope, record identity, safe temporal cursor, projection freshness, evidence completeness, participant resolution, citation availability, audit readiness, verification state, and server-owned command availability metadata that defaults blocked/unavailable.
- Governed evidence entries now represent messages, participants, attachments, retention policy, sensitivity marks, redactions, and freshness metadata as evidence records rather than chat bubbles, preserving chronological evidence ordering and redacted placeholders.
- Server coverage verifies tenant/freshness denial ordering remains unchanged, non-current detail projections fail closed, projection store failures coarsen to unavailable, missing citation and partial evidence metadata remain explicit, detail hydration is response-scoped and only aggregates participant resolution, and the existing authorized API route returns the governed read shape without unsafe new routes or caller-supplied authority.

### Checklist Validation
- [x] API and backend E2E-style read-path tests generated for the implemented governed detail feature.
- [x] UI E2E tests assessed as not applicable because Story 3.2 is implemented as backend contracts/server/API read behavior in this repository.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, and existing fake-store patterns.
- [x] Tests cover happy path plus stale, rebuilding, unavailable, redacted, malformed route, missing citation, partial evidence, unresolved participant, and cross-tenant/hidden cases.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes coverage metrics.

## Story 3.1 Tenant-Scoped Find Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationQueryContractTest.cs` - Added Story 3.1 filter vocabulary validation, invalid date range rejection, search trust-preview JSON shape coverage, and assertions that list responses avoid totals, facets, autocomplete, recent-search, provider-session, and transcript surfaces.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixture coverage for Story 3.1 search trust-preview contracts and vocabularies.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added exact filter coverage for redaction state, freshness state, audit readiness, and verification state; safe no-accessible-matches shape coverage; trust-preview hydration behavior; and review regression coverage for non-current accessible matches beyond the continuation lookahead row.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added query-string binding coverage for Story 3.1 trust filters and fail-closed malformed date/closed-vocabulary filter handling without projection reads.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationQuery|FullyQualifiedName~ConversationSearch|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 17 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 117 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration|FullyQualifiedName~ConversationQueryRegistrationTest"` - 138 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 642 passed.

### Coverage
- Contracts now extend the existing tenant-scoped list-query path with closed, safe search filters for redaction, freshness, audit readiness, and verification state, without adding provider/session identifiers, broad transcript search, totals, facets, autocomplete, or recent-search metadata.
- Summary and projection contracts now expose compact search trust previews with freshness, redaction, participant resolution, citation availability, audit readiness, verification state, match source, and safe why-visible copy. Older projections default to non-assumptive trust metadata.
- Server/API coverage verifies tenant access still gates projection reads, poison rows are tenant-filtered before search, new filters apply only to tenant-scoped projection fields, malformed query values return hidden list shape, hydration updates participant resolution state after paging, non-current accessible matches downgrade list freshness even beyond the continuation lookahead row, and no accessible matches use safe empty copy.

## Story 2.8 Privileged Operational Justification Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/PrivilegedOperationalJustificationContractTest.cs` - Added privileged operation-class vocabulary, structured justification command/detail/result JSON shape, required-field validation, unsupported vocabulary rejection, `ToString()` safety, and forbidden substrate/personal-data field coverage.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for privileged justification vocabulary, command, query, details, and result contracts.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationPrivilegedOperationalJustificationServiceTest.cs` - Added server precondition coverage for approved privileged action, missing justification, unauthorized operator, governance-class authorization, stale/rebuilding freshness, cross-tenant projection poison, audit unavailable/unsafe/uncertain/policy-blocked paths, partial operation outcome, and no delegate execution before gates pass.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationPrivilegedJustificationReviewServiceTest.cs` - Added review-history coverage for authorized reviewer access, unauthorized non-disclosure, malformed handles, unavailable review source, stale review evidence, and explicit redacted/withheld fields.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryRegistrationTest.cs` - Added fail-closed query registration coverage proving the handler resolves with the default unavailable privileged review source when no durable source is configured.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs` - Updated audit-pairing inventory so `RecordPrivilegedJustification` is an implemented privileged audit boundary without making ordinary conversation commands audit-sink dependent.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 69 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest"` - 36 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 97 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ContractSerializationTest|FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 72 passed.
- [x] Review fix validation `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~ConversationQueryRegistrationTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~Projection"` - 119 passed.
- [x] Review fix validation `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Privileged|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 69 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 625 passed.

### Coverage
- Contracts now expose a closed privileged operation-class vocabulary, structured justification command, governed review query, coherent review details, and content-safe result states without raw conversation content, EventStore topology, storage paths, provider payloads, Party personal data, tokens, claims, or raw audit sink identifiers.
- Server coverage verifies tenant authorization occurs before protected evidence resolution, privileged reads/exports/verifications use Admin access, governance-changing metadata/visibility paths use Governance access, current freshness and audit evidence are required before executing privileged delegates, non-success/throwing delegate outcomes are audit-linked with content-safe diagnostics, and unsafe states fail closed without mutation/disclosure.
- Review coverage verifies authorized compliance reviewers receive coherent tenant-scoped records, while unauthorized, malformed, stale, unavailable, and redacted states remain explicit and non-disclosing.

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs` - Added Story 2.3 server-boundary tests for non-success audit statuses, tenant mismatch before audit proof, idempotency conflict before state load/audit, compatible duplicate replay, materially different same-key conflict, and sanitized replay payloads.

### E2E Tests
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added replay/materialization coverage for sensitivity-mark events from accepted public events through derived read state, plus unsupported-version downgrade behavior.
- [x] UI E2E tests are not applicable for Story 2.3 because this repository currently exposes backend contracts/server flows and no implemented UI workflow for sensitivity marking.

## Coverage
- API/application boundary: governance authorization, audit fail-closed behavior, tenant binding, idempotency conflict, duplicate replay, materially different same-key rejection, and sanitized retry-safe outcomes are covered.
- Projection/E2E-style workflow: accepted sensitivity events rebuild target-keyed read-model state with safe audit/trust metadata; unsupported-version sensitivity events do not upgrade projected trust.
- Existing Story 2.3 coverage remains in contract, aggregate, publication, projection accumulator, privacy, and serialization tests.
- UI features: 0/0 applicable for this backend-only story.

## Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` - 152 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-restore` - 124 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore` - 228 passed.
- [x] `dotnet test Hexalith.Conversations.slnx --no-restore` - 513 passed.

## Checklist Validation
- [x] API/application-boundary tests generated.
- [x] E2E-style replay/materialization tests generated for the backend workflow.
- [x] UI E2E tests assessed as not applicable because no UI exists.
- [x] Tests use standard xUnit and Shouldly APIs.
- [x] Tests cover happy path duplicate replay and critical error cases.
- [x] Tests use clear descriptions, no hardcoded waits, and no order dependency.
- [x] Summary includes coverage metrics and validation commands.

## Next Steps
- Keep the contract, domain, server, projection, and solution test lanes in CI for Story 2.3.

## Story 2.7 Audit Record Governance Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/AuditRecordGovernanceContractTest.cs` - Added audit-record action vocabulary, audit target key, missing-handle validation, JSON shape, query/contract `ToString()` safety, unsupported vocabulary rejection, and forbidden substrate field coverage.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added handler entry-point coverage proving `GetAuditRecordAsync()` returns citeable audit evidence through the governed query boundary.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationAuditRecordAccessServiceTest.cs` - Added server audit-record read/export coverage for allowed read, denied read, denied export, policy-blocked export, redacted/withheld details, stale/rebuilding projection, malformed handles, cross-tenant projection poison, source unavailability, rebuild preservation, outcome-only action blocking, and mutation attempts.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Existing projection/redaction replay coverage was exercised with Story 2.7 filters to prove rebuild and redaction behavior remains stable.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~GovernanceContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 63 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~AuditRecord|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest"` - 46 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~Projection"` - 69 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 595 passed.

### Coverage
- Contracts now expose closed audit-record action classes, safe audit-record target keys, missing-handle rejection, policy treatment metadata, governed audit review details, and content-safe read/export results without raw audit sink, storage, EventStore topology, provider payload, message text, redacted text, Party personal data, or raw upstream fields.
- Server coverage verifies tenant authorization occurs before handle parsing and projection reads; the query handler exposes the same governed audit-record boundary; unauthorized, malformed, cross-tenant, unavailable, stale, and rebuilding paths return non-disclosing results; allowed export is in-memory only; policy-blocked, outcome-only, and separate-log paths do not create unmanaged durable export surfaces.
- Rebuild/redaction coverage verifies derived audit views preserve citeable metadata while message redaction remains distinct from audit-record redaction and does not reintroduce suppressed message text.

## Story 2.6 Point-in-Time Governance Reconstruction Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs` - Added temporal anchor/result serialization, supported cursor form validation, forbidden substrate vocabulary checks, and safe hidden-result shape coverage.
- [x] `tests/Hexalith.Conversations.Tests/Replay/ConversationReplayVerifierTest.cs` - Added replay coverage proving retention, sensitivity, and redaction governance events replay deterministically with existing fail-closed replay protections preserved.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added redaction projection coverage proving redacted message text is replaced with a safe placeholder, redaction read state carries safe audit metadata, and prior redaction state suppresses later materialized message text.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs` - Added server temporal reconstruction coverage for timestamp anchors, safe-position cursors, projection cursors, contract cursors, malformed/cross-tenant cursor failure, projection rebuild, incomplete sources, source gaps, unsupported schema, and out-of-coverage behavior.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryContractTest|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 22 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifierTest|FullyQualifiedName~Temporal"` - 20 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~Temporal|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 53 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 572 passed.

### Coverage
- Contract coverage verifies temporal query/result DTOs expose Conversations-owned anchors, safe next actions, confidence/freshness metadata, and no EventStore stream/snapshot/raw substrate terms.
- Replay coverage verifies public/domain governance events are applied by `ConversationReplayVerifier` while existing tenant, conversation, schema, event-type, position, duplicate, malformed payload, unknown-event, and rejection no-op behavior remains covered.
- Server coverage verifies authorization and current disclosure projection checks happen before temporal evidence reads; timestamp, safe-position, projection-cursor, and contract-cursor anchors resolve to safe authoritative anchors; and unsafe cursor/source/projection states return hidden, unavailable, or rebuilding results without protected detail disclosure.
- Redaction coverage verifies current redaction policy suppresses historical message text, prior redaction state suppresses later materialized message text, and responses expose only placeholders, policy reason class, actor attribution, timestamp, and citeable audit handles.

## Story 2.5 Audit Pairing Enforcement Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRetentionPolicyTest.cs` - Added aggregate coverage proving mismatched retention audit evidence fails with `AuditPairingRequired` / `audit_pairing_mismatch` before retention mutation events.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateSensitivityTest.cs` - Added aggregate coverage proving mismatched sensitivity audit evidence fails with `AuditPairingRequired` / `audit_pairing_mismatch` before sensitivity mutation events.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/SetConversationRetentionPolicyCommandHandlerTest.cs` - Added retention handler coverage for audit-service exceptions, closed-state pre-audit rejection, and mismatched returned audit evidence without mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/MarkConversationContentSensitiveCommandHandlerTest.cs` - Added sensitivity handler coverage for audit-service exceptions, invalid target pre-audit rejection, compatible duplicate no-op before duplicate audit, and mismatched returned audit evidence without mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added redaction handler coverage proving audit-service exceptions map to fail-closed `audit_unavailable` without mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs` - Added explicit release-gate inventory for implemented governance mutation handlers, aggregate commands, domain mutation events, and operation kinds; future vocabulary remains prepared but unimplemented; review tightened coverage so audited aggregate commands and non-governance command paths must remain explicit.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter FullyQualifiedName~Governance` - completed; no tests currently match this aggregate-project filter.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationAggregateRetentionPolicyTest|FullyQualifiedName~ConversationAggregateSensitivityTest|FullyQualifiedName~ConversationAggregateRedactionTest"` - 31 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter FullyQualifiedName~TenantAccess` - 125 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter FullyQualifiedName~GovernanceAuditPairingSafetyNetTest` - 3 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~TenantAccess|FullyQualifiedName~Governance"` - 128 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - 156 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 556 passed.

## Story 2.4 Redaction Evidence

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/RedactionContractTest.cs` - Added redaction command/event/result JSON and content-safety coverage for message and opaque content-segment targets.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs` - Added aggregate redaction success, replay, duplicate/no-op, conflict, audit-pairing, target validation, and no-event rejection coverage.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added tenant/governance authorization-before-load, audit fail-closed, tenant mismatch, idempotency conflict, and successful mutation coverage.
- [x] `tests/Hexalith.Conversations.Server.Tests/Publication/ConversationPublicationMapperTest.cs` - Added public redaction event publication mapping coverage.
- [x] `tests/Hexalith.Conversations.Tests/Idempotency/ConversationCommandFingerprintTest.cs` - Added redaction command fingerprint scope coverage using canonical safe target/policy/rationale/category metadata.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added QA automation follow-up coverage for unsupported schema rejection before tenant/idempotency/load/audit disclosure, stale state-load coarsening before audit, and completed duplicate replay without state load or duplicate audit evidence.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs` - Added QA automation gap coverage proving existing sensitivity marks do not block separately audited redaction intent or mutate replay state before event persistence.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added QA automation gap coverage proving already-sensitive targets still require and use the redaction audit gate before mutation.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/RedactionContractTest.cs` - Added QA automation gap coverage for documented redaction result round trips: success, denied, audit unavailable, policy blocked, unsupported target, already-redacted duplicate, and idempotency conflict.
- [x] `tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateRedactionTest.cs` - Added review regression coverage for mismatched audit evidence failing closed before redaction mutation.
- [x] `tests/Hexalith.Conversations.Server.Tests/TenantAccess/RedactMessageContentCommandHandlerTest.cs` - Added review regression coverage for invalid targets before audit side effects, compatible duplicate no-op before audit, and mismatched audit evidence rejection before mutation.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj` - 155 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj` - 132 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj` - 237 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 533 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-build --no-restore` - 155 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-build --no-restore` - 132 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-build --no-restore` - 237 passed.
- [ ] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-restore -p:DisableMsCoverageReferencedPathMaps=true` - blocked before test execution because the sandbox denied writing the generated Coverlet source-root mapping file under `tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0`.
- [ ] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore` - blocked before test execution because the sandbox denied writing the generated Microsoft CodeCoverage source-root mapping file under `tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0`.
- [ ] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore -p:DisableMsCoverageReferencedPathMaps=true` - blocked before test execution because the sandbox then denied writing the generated Coverlet source-root mapping file under `tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0`.
- [ ] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-restore -p:OutputPath=...` - blocked before test execution because the sandbox denied creating the alternate output directory.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --no-build --no-restore` - 155 passed; this validates the existing compiled assembly only and does not compile the new QA follow-up tests.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --no-build --no-restore` - 132 passed; this validates the existing compiled assembly only and does not compile the new QA follow-up tests.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --no-build --no-restore` - 237 passed; this validates the existing compiled assembly only and does not compile the new QA follow-up tests.
