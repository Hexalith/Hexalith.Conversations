# EXT-CONV-AI-1 runtime qualification

The six core seams use the existing `IConversationClient`, aggregate, authoritative EventStore source folds, tenant catalogue, and ordered deletion delivery pump. An external composition host may opt in with `AddConversationDeletionRuntime` and `MapConversationAgentParticipantApi`, after installing `IConversationAgentAuthority`, `IConversationDeletionApprovalVerifier`, `IConversationCommandSourceVerifier`, `IAuthoritativeEventStreamReader`, `ISourcePublicationNamespaceSource`, `ISourcePublicationIndexStore`, `IConversationDeletionReceiver`, and `IConversationClient`. It must supply exact `TenantId`, `WorkerPrincipalId`, `ServicePartyId`, and `NamespaceInstallationId` values. Registration rejects missing or known unavailable providers, but it does not prove enrollment or availability. The source namespace is `conversation/approved-deletion-v1` for that tenant and installation. The repository's thin domain-service host remains fail closed and does not opt in.

The route `POST /api/v1/conversations/{conversationId}/participants` uses the admitted EventStore gateway. Its 202 result means command admission; the caller must use the participant-state read to establish persistence. The handler rejects a foreign tenant or Party, changed type or role, and revoked authority before dispatch. General command routes are not mapped by this Agent-only endpoint extension. Once an external host explicitly composes the runtime, a scoped background worker runs bounded ordered dispatch passes for the exact installed namespace. A blocked pass retains its checkpoint and retries; receipt lookup remains independent of submission.

Accepted cohort `OQ-5.4-1` permits a separately installed and authenticated contract-conformance receiver for owner Level 4 evidence. The [receiver component and installation requirements](ext-conv-ai-1-deletion-conformance-receiver.md) implement an EventStore receipt domain and independent SDK query protocol without an Agents package or runtime path. Its source build is not an installed `IConversationDeletionReceiver` binding. The real Agents Story 8.3 receiver and `RQ-1` Level 5 remain separately gated.

## Live input and evidence

Use PowerShell 7.5 or later. JSON timestamp values are kept as strings so the exact UTC window survives parsing. Set `EXT_CONV_AI_1_LIVE_MANIFEST` to an installed JSON manifest path, or pass `-LiveManifest`. The verifier also reads the committed Agents dependency register and refuses a source target that is not accepted there as `Committed` or `Available`; a caller manifest cannot change the accepted target. The manifest has `schemaVersion: 1`, `targetCommit`, `tenantId`, `foreignTenantId`, `servicePartyId`, `workerPrincipalId`, `messageId`, `zeroAgentConversationId`, UTC `createdFromInclusive`/`createdToExclusive`, `namespaceInstallationId`, `receiverTargetVersion`, `rolloverReceiverTargetVersion`, `storageIdentity`, `receiverStorageIdentity`, `receiverPrincipalId`, and `lookupPrincipalId`. Both receiver targets are distinct full lowercase commit identifiers.

Its `bases` object names independent HTTPS targets `gateway`, `parties`, `receiver`, `control`, and `observer`. Its `credentialReferences` object names `service`, `human`, `approver`, `worker`, `party`, `receiver`, `control`, `observer`, and `receiverChannel`, each as an `env:VARIABLE_NAME` reference with a distinct installed value. The `receiver` assertion may instead use `file:/absolute/path`, reread before each query from a separately enrolled SDK credential provider's atomically published file. The runner never mints assertions. Secret values remain outside the manifest and evidence. Its `operations` object provides each request with `base`, `credential`, canonical case-sensitive `method` (`GET` or `POST`), relative `path`, and a contained `bodyFile` for POST. A manifest can select requests but cannot set expected results: the verifier asserts the C1–C4 typed outcomes and persisted facts in code.

Receiver operations use `base: receiver`, `credential: receiver`, `authentication: EventStoreWorkload`, `sidecarCredential: receiverChannel`, `method: POST`, and `path: query`. The receiver base is the independent lookup workload's authenticated Dapr invocation URL ending in `/v1.0/invoke/{receiver-app-id}/method/`. `receiver` references its lookup workload assertion; `receiverChannel` references its own sidecar API credential. The SDK receiver's application-channel token remains installed at its own sidecar. The runner sends the SDK workload assertion header and sidecar API header, without a public Bearer header. It decodes the SDK `QueryResult.payloadBytes` JSON and rejects failed, malformed or ambiguous query results. HTTP redirects are disabled.

The required operation names are:

| Lane | Operations in execution order | Required independent facts |
| --- | --- | --- |
| Preflight | `identity`, `party`, `receiverIdentity` | Clean source commit equals authenticated runtime identity; exact tenant, namespace, receiver and storage; Parties confirms the immutable active Branch B Organization Party by tenant/id. The independent receiver reports fixed `ContractConformance` / `OQ-5.4-1` / Level 4 scope, disabled Agents runtime, and the exact separate receiver/lookup principals and storage. |
| C1 | `c1Membership`, `c1ParticipantReadAfterFirst`, `c1MembershipRetry`, `c1ParticipantRead`, `c1Post`, `c1MessageReadAfterFirst`, `c1PostRetry`, `c1MessageRead`, `c1RosterRead`, `c1Catalogue`, `c1ZeroAgentSource`, `c1Count`, `c1CurrentRead`, `c1Edit`, `c1EditedRead`, `c1Delete`, `c1DeletedRead` | Source revision is unchanged by exact retries; one AI member and one deterministic message with provenance remain. Current content, human edit and deletion state, and Facilitator are visible. A zero-Agent-Call Conversation is verified from source and present in the authenticated complete catalogue; count equals its active `[from,to)` fold. |
| C2 | `c2Approve`, `c2SourceBefore`, `c2Dispatch`, `c2SourceAfter`, `c2ReceiverLookup` | Approval creates one stable source signal. The source checkpoint advances only after an exact persisted receipt that equals independent receiver lookup and names a positive protected deletion revision. |
| C3 | `c3Approve`, `c3SourcePending`, `c3ReceiverOutage`, `c3SourceOutage`, `c3Restart`, `c3SourceRestart`, `c3TargetRollover`, `c3ReceiverIdentity`, `c3CurrentBinding`, `c3Retry`, `c3SourceAcknowledged`, `c3ReceiverLookup` | Outage is observed and restart retains the signal. Before retry, independent identity proves the successor's same fixed conformance scope at the pinned rollover target, and current source binding selects it. Retry yields an exact independently looked-up receipt at that target. |
| C4 | `c4CrossTenant`, `c4Revoked`, `c4DeletedRead`, `c4RemovedRead`, `c4PoisonSourceBefore`, `c4PoisonInject`, `c4PoisonSourceAfter`, `c4ReceiverLookup` | Foreign and revoked callers receive typed denial without content; deletion and principal removal remain distinct. Changed same-ID delivery conflicts, quarantines the original source entry, and leaves its checkpoint unadvanced. |

Every Live attempt writes `live-evidence.json` in a new run directory. An atomic flushed write records each pending operation before its HTTP request and records its status and response hash immediately afterward. Missing references or accepted targets produce `BLOCKED`; an observed mismatch produces `FAIL`, both with stable codes. The script contains no fallback from Local simulation to Live qualification. The installed manifest and its endpoint/credential references still require independent owner trust qualification before a passing command can establish availability.

## Frozen qualification inputs

Before any HTTP operation, the committed register must retain accepted `OQ-5.4-1`, complete Committed/Available protection, host and secrets targets, and the shared Platform commit required by the cohort. It must also accept an `EXT-CONV-AI-1` `RuntimeQualificationBinding` row. This is a proposed additional acceptance field; the existing commitment does not contain it and cannot authorize the changed verifier.

The row is a JSON object with `schemaVersion: 1`, `cohortId: OQ-5.4-1`, `manifestSha256`, and a `files` object. Each file entry contains a manifest-directory-relative `path` and lowercase `sha256`. Required entries are `configuration`, `plan`, `hostProfile`, `hostVerifierEvidence`, `sourceAnchor`, `partyAnchor`, `approvalAnchor`, `workerAnchor`, `receiverAnchor`, `lookupAnchor`, `observerAnchor`, and `controlAnchor`. Anchor files contain public trust material only. Hashes are recomputed, and files reached through symlinks or outside the manifest directory are refused.

The configuration document binds the manifest's exact tenant, service Party, worker, namespace installation, receiver and lookup principals, and receiver storage. It also pins `attemptJournalDirectory` as a canonical absolute path on the retained owner journal store. The accepted host verifier evidence names its exact Platform target, `verificationMode: Live`, boolean `passPath: true`, and `profileSha256` equal to the pinned host profile. Component-only host evidence is insufficient. These locally checked, owner-accepted bytes pin prerequisites; they do not establish current runtime authority or turn a component verifier into a live pass path.

The plan has `schemaVersion: 1` and `bodyFiles`, a map from every unique POST body-file path to its SHA-256. Its key set must equal the manifest's POST body-file set exactly. Accepted input files and every POST body are checked before requests can execute. The runner sends the same captured body bytes whose hashes passed preflight.

After local prerequisites pass, an exclusive `{attemptJournalDirectory}/{target-set-sha256}.json` reservation is written before the first HTTP request. It binds the exact source/protection/Platform targets and frozen manifest, configuration, plan, profile and anchor hashes to the evidence path. Copying the same manifest or changing the artifacts destination cannot grant another attempt. Original reservations and run evidence are retained after failure, response loss or interruption. Missing prerequisites create refusal evidence without consuming a cohort attempt.

## Replacement owner packet status — 2026-10-22

The committed Agents register pins `87e8b4a6ec3224b20a560b0e83de857805925572`. Work began at `71fab44b7dd664699cedbbfc17ed0d7822e8bdb9`; the shared repository subsequently recorded most component changes at `6655e38743e57ffb85278278dde216663150bd01`. Review fixes remain outside that observed commit. The exact accepted command refuses at its target comparison with exit 1; no cohort executes. A complete replacement packet cannot yet name a reviewed clean successor and installed input hashes. It must retain the accepted `2026-10-08` integration date unless the owner explicitly changes it.

EXT-PROTECTION-1 is still Uncommitted. No enrolled service Party, current worker/machine binding, approval issuer, independent receiver installation or Live manifest has been verified. Current EventStore source explicitly rejects `CommandStateProof` / `VerifiedEffectiveEvents` at its router; those DTO fields are not usable authenticated source authority. No production publication namespace/operation authority implementation was found in the inspected EventStore/Platform source. The isolated Platform baseline also refuses on `NU1605` package downgrades. These require owner delivery; configuration strings and test fixtures cannot resolve them.

The 2026-10-22 detached evidence directory is `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261022-71fab44/`. It retains Local test results and exact-command refusal evidence. Receiver component tests use serialized local EventStore-source simulations, with a separate missing-configuration startup refusal. Neither is live Level 4 qualification. EXT-CONV-AI-1 remains Committed and unavailable; all C1–C4 persisted Live lanes remain unexecuted, and the complete immutable-target replacement acceptance packet remains blocked.

## Delivery checkpoint — 2026-10-25

The preceding 2026-10-22 status describes the historical working tree. The canonical source candidate observed for that checkpoint is `ed53473ea4c67aa10ffc46c9b951d9864fe263d4`. Before this documentation update, all 31 paths in the earlier reviewed-source inventory matched that commit's bytes. The receiver, qualification scripts, tests, owner documentation and spec also matched the earlier reviewed snapshot; only the CI workflow and unrelated deferred-work ledger differed. The pre-existing `references/Hexalith.Builds` working-tree changes were preserved. This source observation establishes neither an installed target nor replacement acceptance.

Fresh Local verification passed 2,247 tests across the five configured projects, including 31 receiver cases, with zero failures or skips. The repeated 37-case six-seam matrix is included in that total. Qualification-helper and synthetic actual-runner checks passed 45 cases. The receiver's fresh restore graph contains 52 libraries and no `Hexalith.Agents` library. These remain component and control-flow evidence. Native receipts, exact commands, source observations and the blocked replacement packet are retained at `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261025-ed53473/`.

That checkpoint left the installed Live manifest, authority and enrollment records, authenticated host delivery transport, independent receiver installation, and accepted configuration, plan, profile, and anchor hashes unverified. The committed Agents register still pinned the previous Conversations target, omitted its proposed `RuntimeQualificationBinding`, and left `EXT-PROTECTION-1` Uncommitted. The packet in `delivery-20261025-ed53473` therefore remained blocked on those inputs: it recorded the source candidate separately and left missing installed fields null.

Replacement `Committed` acceptance of the exact clean target, command and frozen input hashes must precede Live qualification. Passing persisted C1–C4 evidence is required for `Available`; it is not a prerequisite for proposing or accepting that replacement commitment. No Live command or refusal replay was run in that continuation, and no real cohort attempt was reserved. Installation and C1–C4 remain incomplete; Agents Story 8.3 and RQ-1 Level 5 remain gated.

## Installed delivery packet — 2026-10-25

The replacement packet is `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261025-8039d2a/replacement-acceptance-packet.json`. It names canonical target `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3`, replacement `TargetIntegrationDate` `2026-10-25`, and one exact Live command that pins that commit and a clean tree. `2026-10-08` stays the historical missed commitment on the unedited Agents register. The attempt journal is pinned at `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/attempt-journal` and holds no reservation.

No bases, credential references, Party or worker records, approval issuer, or twelve frozen input files were supplied. Those bindings and file hashes are null. The manifest names the same canonical target and leaves every unsupplied field null; its SHA-256 is the hash of those bytes. No host adapter was added, because no installed provider was supplied. No receiver was deployed. The source tree and the Debug conformance assembly are not an installed receiver, so they are not a restore-graph or principal-separation proof. `EXT-PROTECTION-1` remains Uncommitted, and no host Live `passPath` result was qualified. No cohort attempt was reserved. C1–C4 remain `NOT_RUN`. The packet status is `NotSubmitted`. It does not claim `Available`, and the Agents register was not edited. `delivery-20261022-71fab44` and `delivery-20261025-ed53473` were not rewritten.

Focused Debug builds of the server and deletion-conformance test projects succeeded with zero warnings. The conformance assembly passed 31 cases with zero failures or skips. That result is component evidence only.

## Opt-in host composition — 2026-10-29

The repository Server host can now compose `AddConversationDeletionRuntime`, but
only after an explicit opt-in. When `Hexalith:Conversations:DeletionRuntime:Enabled`
is absent or `false`, `Program.cs` skips both opt-in statements and the host keeps
its existing Unavailable defaults. When it is `true`,
`AddConversationDeletionRuntimeHost` validates the configuration below, registers
the Conversations-side adapters, and then calls `AddConversationDeletionRuntime`.
That call still refuses startup, naming the first missing provider, until the
independent providers are installed. `MapConversationDeletionRuntimeHost` maps
`GET api/v1/ext-conv-ai-1/runtime-identity` and the restricted participant route.
Both require a Bearer caller validated against the shared `Authentication:JwtBearer`
contract.

Settings in the `Hexalith:Conversations:DeletionRuntime` section:

| Key | Meaning |
| --- | --- |
| `Enabled` | Explicit `true` opt-in; absent or `false` leaves the host unchanged. |
| `TenantId` | The only installed tenant. |
| `WorkerPrincipalId` | Enrolled worker; the gateway token subject for background delivery. |
| `ServicePartyId` | Separately enrolled dedicated Conversations service Party. |
| `NamespaceInstallationId` | Installation of the `conversation/approved-deletion-v1` namespace. |
| `StorageIdentity` | Installed Conversations storage reference reported by runtime identity. |
| `GatewayBaseAddress` | EventStore gateway origin: HTTPS, or HTTP on loopback, with no path. |
| `PlatformWorkloadId` | EventStore platform workload bound by SDK complete-source admission for `conversation`. |
| `ReceiverPrincipalId` | Receiver principal; also its Dapr application id and the lookup assertion audience. |
| `LookupPrincipalId` | Independent lookup workload; the query `userId` and the lookup issuer workload. |
| `IdentityReaderPrincipalId` | The only token `sub` allowed to read the runtime identity. |
| `AcceptedTargetVersions` | Full lowercase 40-hex receiver targets, including any pinned rollover target. |
| `LookupIssuer:*` | Separate `WorkloadAssertionIssuerOptions` for lookups. `ClientId`, `ClientSecret` and `TokenEndpoint` are required, and `EnableResourceBindingRequests` must be `true`. |

`AcceptedTargetVersions` must keep every target that still has pending delivery
attempts. Removing one leaves those attempts unable to resolve a lost
acknowledgement.

The worker, receiver, lookup and reader principals and the platform workload must
all be distinct, and `PlatformWorkloadId` is at most 200 characters. The host also
requires `EventStore:Authentication` (`Authority`, `TokenEndpoint`, `ClientId`,
`ClientSecret`, `client_credentials` grant) for the worker's SDK gateway authority
transport, `Authentication:JwtBearer` for inbound callers, and `DAPR_HTTP_ENDPOINT`
or `DAPR_HTTP_PORT` plus `DAPR_API_TOKEN` for its own sidecar. The sidecar endpoint
must be HTTPS, or HTTP to a loopback host, with no path. The lookup issuer client
must differ from the worker and host issuer clients. An incomplete value refuses
startup by key name, without echoing values.

The out-of-scope providers are compiled adapters. The installation registers them
in the service collection before it calls `AddConversationDeletionRuntimeHost`;
configuration cannot supply them.

Inbound callers are validated against the shared `Authentication:JwtBearer` gateway
contract, and their Bearer token is forwarded to the gateway unchanged. That is a
deliberate trust boundary: this host accepts only tokens the gateway itself would
accept, and it never substitutes the worker identity for a caller.

The in-scope adapters are:

- `CompleteSourceConversationCommandSourceVerifier`, which maps only the
  SDK-admitted `CompleteCommandSourceContext.Current` prefix. It refuses an absent,
  foreign, expired, gapped, erased or proof-mismatched source, and any request that
  carries `CommandStateProof` or `VerifiedEffectiveEvents`.
- The SDK `AuthoritativeEventStreamReader`, with native proof, over the gateway client.
- `ConformanceConversationDeletionReceiver`. It submits through the gateway client
  and treats a 202 as pending admission, never as `Available`. It looks up the
  current target and receipts through this host's sidecar at
  `/v1.0/invoke/{ReceiverPrincipalId}/method/query`, with `dapr-api-token` and a
  bound lookup assertion. Only an exact acknowledgement is `Available`. A
  well-formed receipt whose same-id fields differ is `Conflict`, which the pump
  quarantines. Malformed, failed, unknown or ambiguous answers are `Unavailable`.
  Each unavailable cause logs one value-free reason code.
- `IConversationClient` on the gateway. Inside a request it forwards the inbound
  Bearer and fails without one. In the background it uses the worker authority
  token.

Redirects are disabled on every outbound client.

Still external, and mandatory before an enabled host starts:
`IConversationAgentAuthority`, `IConversationDeletionApprovalVerifier`,
`ISourcePublicationNamespaceSource` and `ISourcePublicationIndexStore`. Lookups
also need a binding-capable lookup issuer, which is an enrolled authority profile
with `EnableResourceBindingRequests`. Until one exists, current-target and receipt
lookups are `Unavailable`, so delivery stays pending and runtime identity answers
503. That is intended fail-closed behaviour. Still not delivered: the platform
complete-source binding, `ISourcePublicationAuthorityInstallationEvidence`, a binding
signer, the Parties endpoint, an installed receiver, and the Live manifest. This
change installs, enrolls or qualifies nothing, and it does not edit the Agents
register or run a cohort.

The Server and receiver projects now publish containers `conversations` and
`conversations-deletion-conformance` on `aspnet:10.0-alpine` as user `app`.
Nothing was pushed.

Verification for this change used the producer source/Debug mapping from
`delivery-20261029-acd8b0b/prepare-source.stdout.log`. The first five-project Local
run passed 2,372 tests. After the review fixes, one run failed: two
`PlatformEnrollmentMappingTest` cases rejected a test reference to the receiver
tool. The wire-contract test therefore moved to the receiver's own test project.
The final Local run passed 2,419 tests with zero failures or skips, and its 37-case
six-seam run also passed. The new cases cover every matrix row, including:

- a real `WebApplicationFactory` refusal when the host is enabled without installed providers;
- a real-pipeline participant call;
- the composed native-proof reader and the real lookup issuer;
- quarantine of a mismatched receipt;
- a pump that advances only after an independently looked-up exact acknowledgement.

These tests use doubles for the out-of-scope providers and a stubbed gateway and
sidecar, so they are component evidence only.

`PublishContainer` archives were produced and not pushed. A Release publish is
refused under the source mapping (`HXW006`), so the archives come from a Debug
source-mode build of a working tree. They carry the baseline commit stamp and must
be rebuilt from the clean committed target before installation.

All receipts, including the failed run, are retained at
`/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/host-composition-20261029-acd8b0b/`.
The parent owner-qualification spec stays in progress.
