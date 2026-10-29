---
title: '5.4 EXT-CONV-AI-1 opt-in deletion-runtime host composition'
type: 'feature'
created: '2026-10-29'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'acd8b0b59238ee7c84c49874339cc68ebfd6d008'
context:
  - '{project-root}/docs/runbooks/current-change-validation.md'
  - '{project-root}/docs/implementation/ext-conv-ai-1-deletion-conformance-receiver.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No Conversations host can compose `AddConversationDeletionRuntime`. The receiver adapter, the complete-source verifier adapter, the Live `identity` endpoint and container publishing do not exist. As a result, nothing is installable even after the independent authorities are delivered (gap analysis `owner-prerequisites-20261029-180858/installation-gap-analysis.md`).

**Approach:** Add the Conversations-side production pieces that need no independent authority, behind an explicit configuration opt-in on the existing Server host. The out-of-scope providers stay mandatory, so an enabled host refuses to start until they are installed. The owner chose "Source delivery first" on 2026-10-29. Parent spec `spec-5-4-ext-conv-ai-1-owner-qualification.md` stays in-progress.

## Boundaries & Constraints

**Always:**
- Opt-in is `Hexalith:Conversations:DeletionRuntime:Enabled`, default false. When false, the Server host is byte-for-byte compatible in behaviour: it does not opt in and keeps its Unavailable defaults.
- When enabled, the host validates complete configuration and lets `AddConversationDeletionRuntime` refuse a missing provider before listening.
- Gateway submission is admission only, so `SubmitAsync` never returns `Available`. Only an independent receiver lookup that returns an exact acknowledgement can.
- The receiver must report a target that is one of the configured full 40-hex `AcceptedTargetVersions`, and fixed scope `ContractConformance` / `OQ-5.4-1` / Level 4 / `agentsRuntimeEnabled=false`, with the configured principals and tenant. Otherwise the result is `Unavailable`.
- Credentials come only from SDK providers: the gateway authority transport and a separately configured lookup `JwtWorkloadAssertionIssuer`. Lookups require `CanBindResources` and bound tenant/domain. If they are absent, the lookup is `Unavailable`.
- Reuse the EventStore contracts `QueryEnvelope`, `QueryResult`, `SubmitCommandRequest` and `CompleteCommandSource`.
- Server references neither the receiver tool nor any Agents package.

**Never:**
- Implement or stub `IConversationAgentAuthority`, `IConversationDeletionApprovalVerifier`, `ISourcePublicationNamespaceSource`, `ISourcePublicationIndexStore`, `ISourcePublicationAuthorityInstallationEvidence`, a binding signer, or a Parties endpoint.
- Mint tokens locally, accept `CommandStateProof` or `VerifiedEffectiveEvents`, or fall back from the worker identity to a caller identity (or the reverse).
- Deploy anything, edit the Agents register, or run a C1–C4 cohort.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Disabled | `Enabled` absent or false | Current Server composition unchanged | N/A |
| Enabled, provider missing | Out-of-scope providers absent | Startup throws naming the first missing provider; nothing listens | Process exits non-zero |
| Complete source | `CompleteCommandSourceContext.Current` matches request tenant/domain/aggregate, is unexpired, contiguous, not erased, and its proof head equals the last sequence | Authoritative stream with `NativeProof`, head and events | N/A |
| Source absent or mismatched | Null, foreign identity, expired, gap, erased, proof mismatch | `AuthoritativeStreamReadResult(null, reason)` | Admission rejects as `Unavailable` |
| Submit | Gateway answers 202 | `Unavailable` (pending); pump retries via lookup | Transport failure → `Unavailable` |
| Lookup | `QueryResult.success` with exact acknowledgement | `Available` + acknowledgement | Malformed, failed, unknown outcome or ambiguous payload → `Unavailable`; `Conflict` and `Absent` passed through |
| Identity | Authenticated configured reader | `{commit, tenantId, namespaceInstallationId, receiverTargetVersion, storageIdentity, authorityRevision}` | Non-reader 403. Missing commit, receiver target or authority revision 503 |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Server/Program.cs` -- thin host. Add a single opt-in call and mapping; keep the existing lines.
- `src/Hexalith.Conversations.Server/Agents/ConversationAgentServiceCollectionExtensions.cs:62-115` -- `AddConversationDeletionRuntime(catalogue, worker)` already rejects missing or Unavailable providers. Reuse it unchanged.
- Registrations built from options: `ConversationDeletionWorkerRegistration(TenantId, AuthenticatedPrincipalId=WorkerPrincipalId, ServicePartyId)` and `ConversationTenantCatalogueRegistration(new SourcePublicationScope(tenant, "conversation", "approved-deletion-v1", NamespaceInstallationId))`.
- `src/Hexalith.Conversations.Server/Agents/ConversationDeletionDeliveryPump.cs:35-141` -- call order: Current → Lookup(old) → Lookup(current) → Current → Submit. `AcknowledgeAsync` requires an exact acknowledgement. It uses `IConversationClient` with no HttpContext (background).
- `src/Hexalith.Conversations.Server/Agents/ConversationAgentAdmissionStage.cs:152-159,233-258` -- consumes `IConversationCommandSourceVerifier`. `MatchesCompleteState` compares with the source stream.
- `src/Hexalith.Conversations.Server/Api/ConversationAgentParticipantApiHandler.cs` -- submits through `IConversationClient` while handling an authenticated request.
- `src/Hexalith.Conversations.Client/ConversationClientServiceCollectionExtensions.cs` -- `AddHexalithConversationsClient(o => o.Endpoint)` returns an `IHttpClientBuilder` with no auth handler.
- SDK in `references/Hexalith.EventStore/src` (347d390e):
  - `CompleteCommandSourceContext.Current`; `AddEventStoreCompleteCommandSourceVerification(platformWorkloadId, ["conversation"])`.
  - `AddEventStoreGatewayClient` + `AddEventStoreGatewayAuthorityTransport(config)` (`EventStore:Authentication:*`; gateway UserId = token `sub`).
  - `AuthoritativeEventStreamReader` (native proof).
  - `JwtWorkloadAssertionIssuer` and `WorkloadAssertionIssuerOptions` (public; build a second instance by hand with a fixed `IOptionsMonitor`).
  - `Contracts/Queries/QueryEnvelope.cs` and `QueryResult.cs`.
- `tools/Hexalith.Conversations.DeletionConformance/` -- wire contract to mirror, not reference:
  - command `Hexalith.Conversations.DeletionConformance.AcceptConformanceDeletion{signal, deliveryAttemptId, targetVersion}` in domain `conversation-deletion-conformance`, aggregate = signal id;
  - query types `deletion-conformance-receipt` / `deletion-conformance-identity` (aggregate `identity`);
  - outcome is an enum number or name.
- `tests/Hexalith.Conversations.Server.Tests/Agents/` -- existing pump fixtures. Already enrolled in `eng/verify-ext-conv-ai-1.ps1:79` and the CI unit list.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Conversations.Server/Agents/CompleteSourceConversationCommandSourceVerifier.cs` -- map the admitted complete source to `AuthoritativeEventStream` per the matrix -- uses the SDK-authenticated prefix only.
- [x] `src/Hexalith.Conversations.Server/Agents/ConformanceConversationDeletionReceiver.cs` (+ options and mirrored wire records) -- `SubmitAsync` through the gateway client. `LookupAsync`/`CurrentTargetAsync` through the host's sidecar at `/v1.0/invoke/{AppId}/method/query`, with `dapr-api-token` and `X-Hexalith-Workload-Assertion` from the lookup issuer (audience = AppId, operation `domain-service:query`, bound tenant/domain), and `userId` = `LookupPrincipalId`. Redirects disabled.
- [x] `src/Hexalith.Conversations.Server/Agents/ConversationDeletionRuntimeHostExtensions.cs` -- `AddConversationDeletionRuntimeHost(IConfiguration)` / `MapConversationDeletionRuntimeHost()`. Bind and validate options (distinct principals, HTTPS or loopback gateway, full targets); register the adapters, SDK reader, gateway client, complete-source verification and `IConversationClient` with a handler that forwards the inbound Bearer when an HttpContext exists and otherwise uses the worker authority token; then call `AddConversationDeletionRuntime`.
- [x] `src/Hexalith.Conversations.Server/Agents/ConversationRuntimeIdentityEndpoint.cs` -- `GET api/v1/ext-conv-ai-1/runtime-identity`. The commit comes from the assembly informational version `+<40-hex>` suffix; `authorityRevision` comes from `AuthorizeAsync(worker, tenant, null, "DeletionSource")`.
- [x] `src/Hexalith.Conversations.Server/Program.cs` -- call the host extension only when `Enabled` is true.
- [x] `src/Hexalith.Conversations.Server/*.csproj`, `tools/Hexalith.Conversations.DeletionConformance/*.csproj` -- set `IsPublishable`, `EnableSdkContainerSupport`, `ContainerRepository` (`conversations`, `conversations-deletion-conformance`), `aspnet:10.0-alpine` and `ContainerUser app`, following the Platform TenantRegistry pattern.
- [x] `tests/Hexalith.Conversations.Server.Tests/Agents/` -- cover every matrix row with a stub HTTP handler and fake issuer and gateway, plus a real `WebApplicationFactory` refusal when enabled with providers missing.
- [x] `docs/implementation/ext-conv-ai-1-runtime-qualification.md` -- append a dated section listing the configuration keys and the remaining external providers. Do not rewrite history.

**Acceptance Criteria:**
- Given the default configuration, when the Server starts, then its composition and existing test results are unchanged.
- Given test doubles for only the out-of-scope providers, when the host is enabled, then the pump delivers through the receiver adapter and advances only after an independently looked-up exact acknowledgement.
- Given the change, when the five-project Local verifier runs, then all suites pass with zero failures. The parent spec stays in-progress.

## Implementation Notes

- New in `src/Hexalith.Conversations.Server/Agents/`: `ConversationDeletionRuntimeOptions`, `ConformanceDeletionWire`, `CompleteSourceConversationCommandSourceVerifier`, `ConformanceConversationDeletionReceiver`, `ConversationRuntimeCallerTokenHandler`, `ConversationDeletionRuntimeHostExtensions` and `ConversationRuntimeIdentityEndpoint`. `Program.cs` adds two statements, both behind `Enabled`.
- Identity separation relies on the SDK `GatewayAuthorityAccessTokenHandler`, which only sets `Authorization ??=`. The caller handler clears or forwards the token first, so in background calls only the SDK attaches the worker token.
- The host had no Bearer caller scheme, so the opted-in group adds JwtBearer scheme `ConversationDeletionRuntimeCaller`. It validates against the shared `Authentication:JwtBearer` contract and maps the single `sub` to `NameIdentifier`, which is what the participant handler reads.
- `ProductionProgramShouldPerformExactlyTheComposedHostSequence` now lists the two opt-in statements and asserts that both stay behind the opt-in. Tests add `Microsoft.AspNetCore.Mvc.Testing`, whose version is centrally pinned, for the real-`Program` refusal.
- The verification `-c Release` publish is refused under source mapping (`HXW006`), and package preparation is refused (`HXW011`/`HXW010`). Container archives therefore come from a Debug source-mode `PublishContainer`. Nothing was pushed.
- An enabled host requires a platform-admitted complete source for every `conversation` command, and its gateway reader requires native proof. Both are intended under the opt-in.
- Observed but not changed: the existing admission `MatchesCompleteState` requires `PayloadVersion` to be null.
- Local verifier (scratch output): 2,372 tests passed, with zero failures or skips. New cases: 14 source-verifier, 54 receiver, 2 real-`Program` startup refusals and 23 host tests.
- Review patch round 1 applied all 16 patch findings. The first post-review Local run failed in two `PlatformEnrollmentMappingTest` cases (HXW006). The wire-contract test had added a Server.Tests → `tools/` receiver reference, which the mapping workspace does not copy. The test moved to `tests/Hexalith.Conversations.DeletionConformance.Tests`, which references Server with `Aliases="server"`; Server gained `InternalsVisibleTo` for it. Server still does not reference the receiver tool.
- Final Local verifier: 2,419 tests passed with zero failures or skips, and the 37-case six-seam run passed. Debug container archives were rebuilt and not pushed. They carry the baseline commit stamp from the working tree, so installation must rebuild from the clean commit. Receipts, including the failed run, are at `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/host-composition-20261029-acd8b0b/`.

## Spec Change Log

## Review Triage Log

Review pass 1 (2026-10-29): blind, edge-case and verification-gap layers over the change-only diff, with pre-existing working-tree edits excluded.

| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| VG1 participant route `sub`→`NameIdentifier` never exercised | medium / patch | Pre-verified. Only the identity route is pipeline-tested; handler-level tests hand-build `NameIdentifier`. Add a real-pipeline participant test, including a token that carries its own `nameidentifier`. |
| VG2 `requireNativeProof: true` only type-checked | medium / patch | Pre-verified. No composed-host stream read. Assert `RequireNativeProof` on the outgoing read and that a proof-less page is non-authoritative. |
| VG3 composed lookup issuer identity untested | medium / patch | Pre-verified. Every functional test replaces it with `FakeLookupIssuer`. Issue a real Development assertion and assert caller = `LookupPrincipalId` and `CanBindResources`. |
| VG4 several startup refusal rules unasserted | medium / patch | Pre-verified. GrantType, LookupIssuer Workload and host-ClientId equality, and several option keys have no rows. Convert to a theory. |
| B1 no documented provider installation path | low / patch | The real `Program` can only refuse until compiled adapters are registered before the opt-in call. The doc must say so. |
| B2 stale remarks on the Program-sequence test | low / patch | The remarks still say no unit test runs `Program.cs`; `ConversationDeletionRuntimeHostStartupTests` now does. The `ComposeLikeProgram` binding weakness is rejected as low: the real-`Program` startup tests cover the enabled path. |
| B3 required lookup/Dapr inputs not validated at startup | medium / patch | `LookupIssuer` `ClientId`, `ClientSecret`, `TokenEndpoint`, `EnableResourceBindingRequests` and the sidecar endpoint or port plus `DAPR_API_TOKEN` are never required. The host starts and is permanently `Unavailable`, contradicting the doc's refuse-by-key claim. |
| B4 / EC5 sidecar accepts plain HTTP to any host | medium / patch | `TryGetSidecar` allows non-loopback `http` (cleartext `dapr-api-token` and assertion) and silently drops a path. Require HTTPS or loopback HTTP and an empty path. |
| B5 no diagnostics for stuck delivery | medium / patch | `ConversationDeletionDispatchWorker` is the only Agents logger. The adapter collapses every cause to `Unavailable` silently. Log stable reason codes only, never values. |
| B6 non-exact authenticated receipt downgraded to `Unavailable` | medium / patch | `ConversationDeletionDeliveryPump.AcknowledgeAsync` quarantines a non-exact acknowledgement, but the adapter pre-empts it, so an integrity fault retries forever without poison metadata. The parent's frozen "same-id changed fields conflict / no-skip quarantine" settles the fix: map a well-formed non-exact `Available` receipt to `Conflict`. Malformed or ambiguous answers stay `Unavailable`. |
| B7 / EC8 nested acknowledgement unknown members ignored | low / rejected | The exact fields are still compared, and an extra member changes no accepted value. Unlikely in practice, and the fix adds a guard. The hard-coded length 82 is cosmetic. |
| B8 mirrored wire contract can drift from the tool | medium / patch | Tool and adapter literals are duplicated in tests, so a rename stays green. Add a contract test comparing the Server's mirrored constants and shapes with the tool's types, without a runtime reference. |
| B9 identity remark claims nothing comes from configuration | low / patch | Tenant, namespace and storage are installed references from options. Correct the remark. Cache-Control and record placement are rejected as low. |
| B10 no proof the build stamps a commit | false | The built `Hexalith.Conversations.Server.dll` carries `1.0.0+acd8b0b59238ee7c84c49874339cc68ebfd6d008`. |
| B11 identity choice by `HttpContext` presence | false | Only the hosted-worker pump and the authenticated participant handler use `IConversationClient`; no in-request non-Bearer path exists. |
| B12 verifier branch tests missing | medium / patch | `VerifiedEffectiveEvents`, empty prefix, over-limit events, `ValidUntil <= ObservedAt`, default `ObservedAt`, null payload, non-conversation domain and null command are untested. Assert the specific `FailureReason`. |
| B13 no host-level rollover test | low / rejected | Existing pump tests cover old-target lookup and rollover. The adapter accepting both configured targets is covered by `CurrentTargetRequiresTheFixedConformanceIdentity`. |
| B14 container conventions | false | This follows the Platform TenantRegistry pattern the spec mandates. The isolated installer pod spec sets a numeric `runAsUser`. |
| B15 doc verification not reproducible | low / patch | Results live only in scratch. Retain the receipts in a detached evidence directory and cite it. |
| B16 Bearer forwarding trust boundary undocumented | low / patch | Callers are validated against the shared gateway contract and their token is forwarded. Document this. |
| EC1 non-boolean opt-in crashes | false | The `ConfigurationBinder` conversion error names the exact key and refuses before listening, which is fail-closed. |
| EC2 `PlatformWorkloadId` 201–256 chars | low / patch | The SDK bound is 200, but options allow 256, so the SDK throws an unrelated message. Bound the key at 200. |
| EC3 worker token URIs validated only lazily | low / rejected | The pump fails closed (`Unavailable`). The fix would duplicate SDK validation rules. |
| EC4 every conversation command needs a complete source when enabled | maybe-false / defer | Unverified high. It depends on whether the installed `ICompleteCommandSourceAuthority.Requires` covers every conversation aggregate this host processes. Settle it at installation. |
| EC6 dropping a pending target from `AcceptedTargetVersions` stalls delivery | low / patch | Operator-induced. Document that accepted targets must retain any target with pending attempts. |
| EC7 invalid UTF-8 escapes the adapter as `InvalidOperationException` | low / patch | Callers catch it, but the matrix requires the adapter itself to return `Unavailable`. Add it to the catch filters. |
| EC9 caller JwtBearer metadata backchannel follows redirects | low / patch | `JwtBearerAuthenticationContract` sets no backchannel, which contradicts "redirects disabled on every outbound client". Set a redirect-free backchannel handler. |

## Design Notes

Background and request paths never share an identity source. A request without a Bearer token fails; it does not fall back to the worker. The SDK `/query` route requires both the sidecar app token and the assertion, so the adapter calls through its own sidecar rather than the receiver directly. A production binding-capable issuer does not exist yet, so lookups stay `Unavailable` in production until one is installed. That is intended fail-closed behaviour, not a defect.

## Verification

Builds need the producer mapping environment. Its prepare and export commands are in `docs/implementation/ext-conv-ai-1-runtime-qualification.md`, section "Owner-qualification continuation — 2026-10-29". Reuse `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261029-acd8b0b/prepare-source.stdout.log`, and write new outputs under scratch or a new evidence directory, never into existing evidence. Always pass `-nodeReuse:false`, because concurrent sessions make builds hang. Run built xUnit v3 test assemblies directly, as the Local verifier does.

**Commands:**
- `dotnet build tests/Hexalith.Conversations.Server.Tests -nodeReuse:false` with that environment -- expected: 0 errors.
- `pwsh -NoProfile -File eng/verify-ext-conv-ai-1.ps1 -Mode Local -ArtifactsPath <new dir>` with the same environment -- expected: all five suites pass, zero failures.
- `dotnet publish src/Hexalith.Conversations.Server -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=<scratch>.tar.gz` -- expected: archive produced (no push).
