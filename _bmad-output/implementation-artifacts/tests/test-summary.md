# Test Automation Summary

## Story 4.3 Expose Typed Sanitized Errors and Remediation Guidance

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs` - Added descriptor coverage for every supported error code, retryability/catalog consistency, safe message/action fields, HTTPS documentation pointers, audit-handle allowance, closed action vocabulary serialization, and additive JSON tolerance.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs` - Extended unsafe free-text rejection to the new `SafeMessage` field and preserved serialized content-safety checks across curated error fixtures.
- [x] Senior review auto-fix: `tests/Hexalith.Conversations.Contracts.Tests/ForbiddenPublicSurfaceTest.cs` now rejects tenant, Party, conversation, provider-session/payload, business-reference, local-path, and exception markers across every protected `ConversationError` free-text field.
- [x] Senior review auto-fix: `tests/Hexalith.Conversations.Contracts.Tests/ConversationErrorCatalogTest.cs` now proves unsupported error code/category/action parser and JSON converter diagnostics do not echo raw protected values.
- [x] Senior review auto-fix: `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs` now proves invalid package versions return `send-semantic-package-version` while invalid schema versions keep `send-positive-integer-schema-version`.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSerializationTest.cs` and `ContractSamples.cs` - Updated representative wire fixtures and serialization samples for `clientAction`, `safeMessage`, catalog descriptors, and closed action vocabulary.
- [x] `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` - Added typed fallback coverage for unsupported schema before send, typed server error bodies, non-JSON 400/401/403/404/409/500 responses, timeout/unknown outcome, tenant denial fallback, idempotency conflict, and client-visible content-safety.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs` - Added command API coverage for malformed body, missing metadata, unauthenticated caller, missing tenant claim, tenant mismatch, route/body mismatch, handler-supplied idempotency conflict, handler-supplied audit unavailable, stale projection, participant/onboarding unavailable, provider-identity failures, and shared catalog action/message fields.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Error|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~Versioning|FullyQualifiedName~ContractSerialization"` - 49 passed.
- [x] Senior review auto-fix: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Error|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~Versioning|FullyQualifiedName~ContractSerialization"` - 50 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 23 passed.
- [x] Senior review regression: `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 23 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi"` - 13 passed after QA automation follow-up coverage.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~ConversationReadApi|FullyQualifiedName~Governance|FullyQualifiedName~Idempotency"` - 116 passed after QA automation follow-up coverage.
- [x] Senior review regression: `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~ConversationReadApi|FullyQualifiedName~Governance|FullyQualifiedName~Idempotency"` - 116 passed.
- [x] `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Contracts.1.0.0.nupkg`.
- [x] Senior review regression: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - passed.
- [x] `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Client.1.0.0.nupkg`. First parallel attempt collided on a shared contracts intermediate DLL lock; serial rerun passed.
- [x] Senior review regression: `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - exited successfully.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Contracts 278, Integration 8, Core 139, Server 386.
- [x] Senior review regression: `dotnet test Hexalith.Conversations.slnx` - all solution tests passed: Client 23, Contracts 279, Integration 8, Core 139, Server 390.

### Coverage
- Contracts now expose canonical typed remediation fields through `ConversationErrorClientAction`, `ConversationErrorDescriptor`, and `ConversationErrorCatalog` without replacing `ConversationErrorResult`.
- REST, client fallback, and compatibility checks now source category, retryability, safe action, safe message, documentation pointer, and audit-handle allowance from the shared catalog.
- Client and server tests prove raw non-JSON failures, malformed requests, authorization failures, idempotency conflicts, audit unavailable, stale projection, participant/onboarding unavailable, provider-identity failures, unsupported schemas, and unknown outcomes coarsen to bounded typed errors without leaking tenant IDs, provider/session details, route internals, raw exception text, or storage/infrastructure terms.
- Senior review coverage proves typed error free-text guards reject common protected identifier, provider, business-reference, local-path, and exception markers, and parser/converter diagnostics avoid echoing unsupported raw closed-vocabulary values.
- Compatibility remediation coverage now distinguishes invalid schema-version guidance from invalid package-version guidance.
- README and contract package docs now document compact adopter-facing error semantics at the contract/client level without adding raw HTTP fallback examples.

### Checklist Validation
- [x] Story 4.3 AC1-AC4 are covered by contract descriptor tests, client fallback tests, server command API tests, compatibility serialization tests, forbidden-surface scans, package validation, and full-solution regression tests.
- [x] The implementation keeps shared contracts plus `Hexalith.Conversations.Client` as the supported v1 path and does not add adopter-facing raw HTTP fallback guidance.
- [x] Summary includes targeted validation commands, package-validation evidence, full-solution results, and the transient pack-lock rerun note.

## Story 4.2 Provide Supported .NET Client Happy Path

### Generated Tests
- [x] `tests/Hexalith.Conversations.Client.Tests/ConversationClientTest.cs` - Added deterministic fake-transport coverage for create, append, and read request mapping; typed success/error mapping; current and non-current freshness outcomes; unsupported schema handling; duplicate replay; idempotency conflict; timeout/unknown outcome retry; non-seekable HTTP response content; tenant denial fallback; sanitized server errors; and DI typed-client registration.
- [x] `tests/Hexalith.Conversations.Client.Tests/ClientBoundaryTest.cs` - Extended client assembly boundary coverage for allowed Microsoft HTTP/DI references only and absence of raw HTTP fallback public surface.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationCommandApiTest.cs` - Added opt-in command API coverage for authorization metadata, create/append route shape, tenant binding, route/body conversation mismatch, typed idempotency conflict mapping, and content-safety.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` - Updated client package inventory coverage now that Story 4.2 intentionally adds supported client behavior.

### Validation
- [x] Readiness gates verified in `_bmad-output/implementation-artifacts/readiness-gates.md`: `Projection freshness blocking semantics` and `.NET client versus raw HTTP fallback policy` are `decided`.
- [x] Red phase: `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` failed before implementation because `ConversationClient`, `IConversationClient`, and DI references did not exist.
- [x] `dotnet test tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj` - 17 passed after review fix.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationReadApi|FullyQualifiedName~ConversationCommandApi|FullyQualifiedName~Idempotency"` - 56 passed.
- [x] `dotnet pack src/Hexalith.Conversations.Client/Hexalith.Conversations.Client.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Client.1.0.0.nupkg`.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed after review fix: Client 17, Contracts 273, Integration 8, Core 139, Server 382.

### Coverage
- The supported .NET client now posts v1 `CreateConversationCommand` and `AppendMessageCommand`, reads `ConversationDetailResult` from the existing read route, and returns typed success or `ConversationErrorResult` outcomes without exposing `HttpResponseMessage` or EventStore mechanics.
- Review fix hardened response deserialization for non-seekable HTTP content streams used by real transports.
- Idempotency metadata is preserved through command bodies and safe headers; duplicate replay and conflict behavior remain typed and caller-visible without using provider session IDs as durable identity.
- Freshness handling preserves `ConversationDetailResult` trust states; only `Current` + `current` + non-stale detail allows trust-bearing timeline use.
- A narrow opt-in `ConversationCommandApi` server extension was added for hosts/tests while `Program.cs` remains fail-closed.
- Raw HTTP fallback remains non-promotional: no public raw HTTP fallback API, examples, README snippets, or docs were added.

### Checklist Validation
- [x] Client, server API, boundary, package inventory, and raw-fallback-negative tests generated or updated.
- [x] Tests use xUnit, Shouldly, deterministic fake HTTP handlers, ASP.NET endpoint invocation, and existing contract DTOs.
- [x] Tests cover happy path plus unsupported schema, stale/rebuilding/unavailable/forbidden freshness, timeout retry, duplicate replay, idempotency conflict, tenant denial, sanitized errors, and dependency boundaries.
- [x] Summary includes validation commands, package-validation evidence, and full-solution results.

## Story 4.1 Publish Conversations Contract Package and Compatibility Metadata

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/Versioning/ContractCompatibilityMetadataTest.cs` - Added compatibility metadata coverage for active v1 command/projection/event/package discovery, closed status vocabulary serialization, supported/deprecated/unsupported/invalid checks, malformed and unsupported package-version inputs, additive JSON tolerance, typed safe failures, and forbidden content fragments.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractPackageInventoryTest.cs` - Added package inventory coverage that packs the contracts project and inspects `.nupkg`/`.nuspec` metadata and entries for adopter-safe contents.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization and forbidden-surface fixtures for compatibility status, package version, remediation, request, metadata, and result contracts.

### Validation
- [x] Red phase: targeted contract test filter failed before implementation because the new compatibility types did not exist.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization"` - 38 passed.
- [x] `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - produced `.artifacts/package-validation/Hexalith.Conversations.Contracts.1.0.0.nupkg`.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ContractPackageInventory"` - 2 passed.
- [x] QA follow-up: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractPackageInventory"` - 44 passed.
- [x] QA follow-up: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - completed successfully.
- [x] Senior review auto-fix: `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~Versioning|FullyQualifiedName~ContractMetadata|FullyQualifiedName~ContractsAssemblyBoundary|FullyQualifiedName~ForbiddenPublicSurface|FullyQualifiedName~ContractSerialization|FullyQualifiedName~ContractPackageInventory"` - 49 passed.
- [x] Senior review auto-fix: `dotnet pack src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj -c Release -o .artifacts/package-validation` - completed successfully.
- [x] `dotnet test Hexalith.Conversations.slnx` - all solution tests passed after senior review fixes: Contracts 273, Client 1, Integration 8, Core 139, Server 377.

### Coverage
- Compatibility metadata now exposes active v1 command, projection, event, contracts package, and .NET client package versions through contract-owned DTOs.
- Compatibility status is a closed vocabulary with JSON converter coverage for `supported`, `deprecated`, `unsupported`, and `invalid`.
- Compatibility checks return content-safe typed results for supported, deprecated package, unsupported schema/package, and malformed schema/package inputs.
- Senior review coverage enforces status/remediation/error invariants, non-null compatibility status, package-specific contracts/client version evaluation, and client package metadata alignment without adding client behavior.
- Package validation proves the contracts `.nupkg` includes adopter metadata and README guidance while excluding server, infrastructure, UI, test, and generated files.
- No server compatibility endpoint, client happy-path behavior, onboarding diagnostics, conformance package, release signing, or deprecation policy lifecycle was added.

### Checklist Validation
- [x] Contract, package inventory, serialization, and boundary tests generated.
- [x] Tests use xUnit, Shouldly, `System.Text.Json`, NuGet package inspection, and existing contract-sample safety patterns.
- [x] Tests cover happy path plus deprecated, unsupported, malformed, additive JSON, package inventory, dependency boundary, and content-safety scenarios.
- [x] Summary includes validation commands and package-validation evidence.

## Story 3.7 Provide Self-Serve Buyer Acceptance Demo

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/BuyerAcceptanceDemoContractTest.cs` - Added buyer acceptance scenario, fixture, step, evidence summary, verification summary, closed vocabulary, JSON shape, duplicate mapping, temporal cursor validation, undeclared fixture rejection, and content-safety coverage.
- [x] `tests/Hexalith.Conversations.Tests/Testing/BuyerAcceptanceDemoFixtureTest.cs` - Added deterministic synthetic fixture coverage for canonical trust states, synthetic marker, unique scenario steps, composite temporal cursor fixture, authorized projection data, verification pass/fail fixtures, and cross-tenant poison sentinel non-disclosure.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationBuyerAcceptanceDemoServiceTest.cs` - Added service runner coverage for full walkthrough summary, selected verification pass/fail output, out-of-scope verification filtering, temporal replay source wiring, module-vs-inherited evidence scope, cross-tenant denial, missing caller fail-closed behavior with verification summary suppression, missing/same-tenant probe partial outcomes, poison sentinel safety, DI registration, and mutation-boundary separation.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 18 passed after red phase and review fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 2 passed after red phase.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance"` - 8 passed after QA and review gap fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 78 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~BuyerAcceptance|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~ConversationGovernanceVerificationServiceTest|FullyQualifiedName~ConversationProjectionMaterializerTest"` - 121 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 52 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 145 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 774 passed.

### Coverage
- Buyer acceptance contracts expose deterministic scenario, fixture, step, evidence summary, selected verification summary, pass/fail status, requirement mappings, evidence handles, and module-vs-inherited ownership without open polymorphism.
- Synthetic fixtures cover full trust, redaction with audit evidence, stale evidence, missing citation/incomplete audit, unresolved participant hydration, blocked governance command metadata, verification pass/fail, and cross-tenant poison sentinels.
- The service runner composes existing read/query/projection and attached verification outputs, binds tenant/caller authority from the trusted boundary, fails closed without caller authority, and returns only an in-memory content-safe summary.
- No HTTP endpoint, UI shell, durable demo store, production seed store, export artifact, command handler, governance audit gate, or EventStore append path was added.

### Checklist Validation
- [x] Contract, fixture-builder, service, DI, and safety tests generated for Story 3.7.
- [x] API/demo-host route assessed as not necessary for current repo shape; service and tests provide the self-serve execution entry point without mutation semantics.
- [x] Tests use standard xUnit, Shouldly, DI resolution, deterministic fixture builders, fake in-memory read stores, and reflection safety-net checks.
- [x] Tests cover repeatability, safe fixture handling, selected verification output, out-of-scope verification filtering, canonical temporal cursor handling, content-safe evidence summary, module-vs-inherited evidence separation, missing caller/verification/probe partial or failed outcomes, same-tenant hidden read rejection for cross-tenant proof, cross-tenant poison non-disclosure, and mutation-boundary separation.

## Story 3.6 Run Governance Verification and Return Structured Results

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/GovernanceVerificationContractTest.cs` - Added contract coverage for stable verification JSON shape, closed verification scope/suite/status/classification/remediation vocabularies, safe diagnostic text rejection, required v1 suite/classification vocabulary, duplicate suite rejection, and inverted time-window rejection.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for verification vocabularies, scope, request, check result, run result, and evidence handle records.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/ConversationGovernanceVerificationServiceTest.cs` - Added service coverage for passing verification, missing audit pair, redaction replay failure, projection rebuild disagreement, unsupported schema, stale projection, missing/non-verify privileged justification, tenant-wide deferred scope, local read-only audit-not-recorded reason, dependency unavailable, retained-coverage data unavailable, rebuilding temporal evidence, thrown event-source failure, unauthorized scope, cross-tenant poison, provider correlation authority misuse, DI registration, and mutation-boundary separation.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ContractSerialization"` - 17 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationGovernanceVerification"` - 18 passed after senior review auto-fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ConversationQuery|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 66 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj --filter "FullyQualifiedName~ConversationReplayVerifier|FullyQualifiedName~ConversationAggregateRedaction|FullyQualifiedName~ConversationAggregateRetentionPolicy|FullyQualifiedName~ConversationAggregateSensitivity"` - 51 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~GovernanceVerification|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~ConversationProjectionRebuildVerifierTest|FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 125 passed after senior review auto-fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~ConversationPrivilegedOperationalJustificationService|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 133 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 746 passed after senior review auto-fixes.

### Coverage
- Verification contracts now expose tenant-safe request scope, suite selection, per-check results, run results, execution status, failure classification, evidence handles, and safe remediation without open polymorphism or raw infrastructure disclosure.
- Server verification runs through trusted tenant/caller authority, requires existing verify justification before touching tenant conversation data, blocks on non-current projection freshness, reuses replay and projection rebuild proof paths, and keeps verification output as derived evidence only.
- Check adapters distinguish governance failures from dependency, stale projection, data unavailable, unsupported version, unauthorized/hidden, and execution-style failures across audit pairing, tenant isolation, redaction replay, projection rebuild, provider portability, schema compatibility, missing/incorrect privileged verification justification, deferred tenant scope, and local read-only proof paths.
- No HTTP execution surface, worker, durable verification store, event append path, UI shell, evidence bundle export, or Story 3.7/3.8 scope was added.

### Checklist Validation
- [x] Contract, service, and safety tests generated for the implemented verification workflow.
- [x] API/CLI boundary assessed as service-only for this story because the repository has no approved CLI/worker/Admin shell and adding an HTTP execution endpoint was optional.
- [x] Tests use standard xUnit, Shouldly, DI resolution, reflection safety-net checks, and existing fake-store patterns.
- [x] Tests cover happy path plus invariant failure, infrastructure/dependency failure, stale projection, retained-coverage data unavailable, missing audit pair, missing/non-verify verify justification, local read-only audit state, tenant-wide deferred scope, redaction replay failure, projection rebuild disagreement, unsupported schema, provider portability failure, cross-tenant poison, unauthorized scope, and release-gate suitability.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes validation commands and coverage metrics.

## Story 3.5 Preserve Read-Only Compliance Workflows and Safe Command Gates

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationCommandAvailabilityContractTest.cs` - Added fail-closed command availability contract coverage for explicit read-only/governance-changing classification, fresh server recheck requirements, missing-metadata defaults, available-governance gate validation, valid available-metadata recheck requirements, mandatory recheck on unavailable metadata, normalized safe vocabulary rejection, and stable JSON serialization.
- [x] `tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionMaterializerTest.cs` - Added projection state matrix coverage proving default governance command metadata remains unavailable, server-owned, audit/freshness annotated, and recheck-required across current, audit-ready, stale, rebuilding, unavailable, and unsupported-schema projections.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added read-boundary coverage proving projection-owned command metadata is preserved as advisory metadata, available metadata stays recheck-gated, stale projections clear protected detail state, and default missing command metadata remains unavailable/read-only/recheck-required.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added API coverage proving the authorized read group exposes GET routes only and ignores client-supplied command metadata/authority.
- [x] `tests/Hexalith.Conversations.Server.Tests/Governance/GovernanceAuditPairingSafetyNetTest.cs` - Added safety-net coverage proving read-only workspace boundaries do not directly depend on governance mutation handlers, the governance audit gate, or idempotent command execution.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCommandAvailability|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~ContractValidation|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 66 passed after senior review auto-fixes.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest|FullyQualifiedName~GovernanceAuditPairingSafetyNetTest|FullyQualifiedName~Privileged"` - 122 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationTenantAccess|FullyQualifiedName~SetConversationRetentionPolicyCommandHandlerTest|FullyQualifiedName~MarkConversationContentSensitiveCommandHandlerTest|FullyQualifiedName~RedactMessageContentCommandHandlerTest"` - 118 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 714 passed after senior review auto-fixes.

### Coverage
- Command availability metadata is now explicitly classified as read-only or governance-changing, requires fresh server recheck for every metadata instance, and rejects unsafe vocabulary tied to EventStore internals, provider payloads, browser/client state, route secrets, raw exceptions, and Party personal data, including separator/casing variants.
- Missing command metadata still produces non-empty unavailable defaults; projection-owned governance commands remain advisory from read workflows, and any available metadata must still carry required permission, precondition, risk, freshness, audit, blocked reason, classification, last-evaluated metadata, and a fresh server recheck requirement.
- Read APIs remain GET-only under the existing authorized `/api/v1/conversations` group and continue binding tenant/caller from trusted claims only.
- Stale or denied read transitions close protected detail/citation/audit/command fields rather than retaining clipboard-ready, temporal, audit, or command authority data.
- UI/component E2E was not applicable for Story 3.5 because there is still no `Hexalith.Conversations.Admin` or web project; the implemented scope is contracts, server projections/query behavior, read API, and safety tests.

### Checklist Validation
- [x] API and backend read-path safety tests generated for command-gate behavior.
- [x] UI E2E tests assessed as not applicable because no UI exists in this repository for this story.
- [x] Tests use standard xUnit, Shouldly, ASP.NET endpoint invocation, reflection safety-net checks, and existing fake-store patterns.
- [x] Tests cover fail-closed defaults, blocked command metadata, available command metadata, mandatory recheck metadata, normalized safe command fields, stale projection clearing, client-supplied metadata rejection, read-only route shape, and mutation-boundary separation.
- [x] Tests are independent, use no sleeps or hardcoded waits, and the summary includes validation commands and coverage metrics.

## Story 3.4 Copy Citations and Stable Temporal Evidence Links

### Generated Tests
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ConversationCitationContractTest.cs` - Added citation DTO/result serialization coverage for safe copied text, safe labels/accessibility text, audit-handle inclusion, temporal cursor metadata, forbidden EventStore/provider/storage/personal-data/browser-selection vocabulary, unsafe citation DTO construction, and unsafe evidence-entry target rejection.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/TemporalReconstructionContractTest.cs` - Extended temporal anchor coverage for composite authoritative anchors carrying safe source position, projection cursor, projection version, and supporting timestamp while rejecting mismatched composite cursors.
- [x] `tests/Hexalith.Conversations.Contracts.Tests/ContractSamples.cs` - Added serialization fixtures for citation target, citation query, citation DTO, and citation result.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationQueryHandlerTest.cs` - Added citation query coverage for authorized safe DTO construction, redacted target placeholder/attribution output, missing audit-handle downgrade, denied/missing/stale/cross-tenant projection fail-closed behavior, future source-position gap handling, no original message text, and tenant authorization/projection read boundaries.
- [x] `tests/Hexalith.Conversations.Server.Tests/Queries/ConversationTemporalReconstructionServiceTest.cs` - Updated deterministic temporal re-resolution assertions for composite authoritative anchors with projection cursor/version metadata and mismatched projection-version cursor fail-closed behavior.
- [x] `tests/Hexalith.Conversations.Server.Tests/Api/ConversationReadApiTest.cs` - Added authorized route metadata and read API coverage for citation and temporal routes, trusted claim binding, malformed target/cursor hidden equivalence, strict malformed projection-cursor rejection, unsafe query-string value exclusion, and citation permission-downgrade clearing of clipboard/link metadata.

### Validation
- [x] `dotnet test tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationEvidence|FullyQualifiedName~TemporalReconstruction|FullyQualifiedName~ForbiddenPublicSurfaceTest"` - 37 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationCitation|FullyQualifiedName~ConversationTemporalReconstructionServiceTest|FullyQualifiedName~ConversationQueryHandlerTest|FullyQualifiedName~ConversationReadApiTest"` - 74 passed.
- [x] `dotnet test tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj --filter "FullyQualifiedName~ConversationProjectionMaterializerTest|FullyQualifiedName~ConversationProjectionReadServiceTest|FullyQualifiedName~ConversationAuditRecordAccessServiceTest|FullyQualifiedName~TenantAccess|FullyQualifiedName~Hydration"` - 183 passed.
- [x] `dotnet test Hexalith.Conversations.slnx` - 687 passed.

### Coverage
- Citation copy now resolves through a Conversations-owned `ConversationCitationAccessService` after tenant/caller authorization and current projection freshness recheck; DTO output is built from governed evidence metadata rather than rendered/client text.
- Citation contracts expose schema, tenant, conversation, evidence id/kind, timestamp, actor PartyId, audit handle when ready, projection cursor/version, temporal cursor, safe copied text, safe labels, and safe next action without raw EventStore/provider/storage or original redacted content.
- Missing audit handles, missing/deleted evidence entries, redacted targets, stale projections, denied callers, cross-tenant projection poison, malformed targets, future source-position gaps, permission downgrades, and malformed temporal cursors fail closed with hidden/unavailable/rebuilding shapes rather than trusted citation output.
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
