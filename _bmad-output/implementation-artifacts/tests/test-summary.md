# Test Automation Summary

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
