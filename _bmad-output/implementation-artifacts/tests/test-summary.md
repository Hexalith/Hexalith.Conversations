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
