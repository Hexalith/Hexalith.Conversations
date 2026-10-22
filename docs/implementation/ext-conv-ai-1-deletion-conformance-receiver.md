# OQ-5.4-1 deletion conformance receiver

`tools/Hexalith.Conversations.DeletionConformance` is a separate SDK-hosted
qualification service. Its fixed scope is `ContractConformance`, `OQ-5.4-1`,
Evidence Level 4, with `agentsRuntimeEnabled=false`. It contains no
`Hexalith.Agents` reference and performs no protected deletion. It does not
establish Story 8.3, Level 5, dependency availability, installation or accepted
owner-target status.

The service uses `AddEventStoreDomainService` / `UseEventStoreDomainService`.
The SDK owns authentication, command/query endpoints, event transport, native
complete-stream verification and persistence. There is no local database,
custom bearer verifier, Dapr state-store client, or module AppHost.

## Required installed bindings

The service requires a non-Development environment, such as `Production`, and
refuses Development before activating the SDK. This keeps the SDK's existing
startup credential validator in its strict mode. The service also refuses
startup without complete `DeletionConformance` options and its separate
authority client identity. The SDK requires its application-channel token and
workload JWT trust contract. These settings are installation requirements, not
proof of enrollment:

| Setting | Required meaning |
| --- | --- |
| `DeletionConformance:Enabled` | Explicit `true` for this bounded owner qualification only. |
| `TenantId` | One canonical enrolled tenant, distinct from the foreign-tenant fixture. |
| `TargetVersion` | The full lowercase 40-character immutable installed receiver commit. |
| `ReceiverPrincipalId` | The separately enrolled receiver application/workload and authority client ID. Must equal `EventStore:DomainService:AppId` and `EventStore:Authentication:ClientId`. |
| `LookupPrincipalId` | A separately enrolled independent lookup workload. |
| `WorkerPrincipalId` | Exact current Conversations delivery actor copied by the authenticated gateway into command `UserId`. |
| `GatewayWorkloadId` | Enrolled EventStore workload allowed to invoke `/process`. |
| `StorageIdentity` | Independently verified installed EventStore storage identity; no secret or connection string. |
| `GatewayBaseAddress` | Exact HTTPS or loopback HTTP EventStore gateway origin. |
| `RetainedTargetVersions` | Optional explicit full historical targets retained for lost acknowledgement lookup after rollover. |

`ReceiverPrincipalId`, `LookupPrincipalId`, `WorkerPrincipalId` and
`GatewayWorkloadId` must be distinct. Each table row after `Enabled` is within
the `DeletionConformance` section. Configure the SDK's
`Authentication:Workload:Audience` to the receiver application ID and
`AllowedCallers` to the exact gateway and lookup workloads. Configure
`Authentication:WorkloadJwtBearer`, `APP_API_TOKEN`, and
`EventStore:Authentication` through independently enrolled installation inputs.
Only the `client_credentials` grant is accepted for the receiver's outbound
gateway reads. Private credentials and signing keys are never checked in.

The receiver's gateway credential needs current read permission on the exact
tenant's Conversations source and isolated
`conversation-deletion-conformance` receipt streams. Reads explicitly require
SDK native source proofs. Uncertified, incomplete, changed-head, unavailable or
foreign prefixes cannot produce an acknowledgement. The source worker needs
command permission only on that tenant's receipt domain and its source
publication scope. The lookup workload needs `domain-service:query` for this
receiver, with signed `eventstore:bound-tenant` and `eventstore:bound-domain`
claims equal to the installed tenant and `conversation-deletion-conformance`.
No Agents workload belongs in these grants.

The source owner still must install and independently verify the current worker,
approval issuer, source/catalogue/publication authority and trust anchors.
Configuring names in this service cannot supply those authorities. The service
does not interpret `CommandStateProof` or `VerifiedEffectiveEvents` as authority.

## Command and independent lookup protocol

Submit the ordinary EventStore gateway command with domain
`conversation-deletion-conformance`, aggregate ID equal to the original
`ConversationDeletionSignalId`, command type
`Hexalith.Conversations.DeletionConformance.AcceptConformanceDeletion`, and
payload `{ signal, deliveryAttemptId, targetVersion }`. The gateway must place
the installed current source worker actor in `UserId`. Only its authenticated
gateway workload can enter SDK `/process`. Admission independently reads the
complete Conversations stream and requires the exact approval event at
`SourceRevision` and its latest durable `Attempt` with the same attempt and
target. Missing approval, changed source fields, quarantine and stale attempts
cannot produce an acceptance.

Gateway admission and `/process` event production remain pending. They are never
receipt confirmation. EventStore persists `ConformanceDeletionAccepted`; the
source may acknowledge only after an independent authenticated query confirms
that event's positive receipt revision.

Both query operations use the existing SDK `POST /query` endpoint on the separate
receiver origin. Send `X-Hexalith-Workload-Assertion` issued for the receiving
application and `domain-service:query`, through its authenticated application
channel. The query envelope has `tenantId` set to the installed tenant, `domain`
set to `conversation-deletion-conformance`, `userId` set to
`LookupPrincipalId`, and a fresh `correlationId`. `isGlobalAdmin` and `isDelegated`
must be false. `payload` is the SDK envelope's base64 encoding of UTF-8 JSON
bytes, rather than an embedded JSON object.

| Operation | `queryType` | `aggregateId` | Decoded payload |
| --- | --- | --- | --- |
| Identity | `deletion-conformance-identity` | `identity` | Empty byte array (`payload: ""`). |
| Receipt | `deletion-conformance-receipt` | Original signal ID | `{ signal, deliveryAttemptId, targetVersion }`. |

Identity returns SDK `QueryResult` containing these fixed payload fields:
`schemaVersion=1`, `receiverKind="ContractConformance"`,
`qualificationScope="OQ-5.4-1"`, `evidenceLevel=4`,
`agentsRuntimeEnabled=false`, and installed `tenantId`, `targetVersion`,
`storageIdentity`, `receiverPrincipalId`, `lookupPrincipalId`. Identity accepts
no expectation payload. The lookup service derives these fields from its own
installed configuration, never from caller-selected expected scope.

Receipt returns SDK `QueryResult` with a `DeletionConformanceResult` payload:
`outcome` uses the existing `ConversationAgentsOutcome` values, and
`acknowledgement` is populated only for `Available`. The acknowledgement is the
original `ConversationDeletionAcknowledgement`, including its immutable source
revision, positive persisted receipt event position, target and content-free
`oq-conformance-v1:` evidence reference. The evidence reference is not a
signature or a transferable capability; authenticated independent lookup is
required. Complete empty streams return `Absent`; incomplete/unavailable reads
return `Unavailable`; changed same-id bindings return `Conflict`; unauthorized
requests disclose no receipt.

An exact retry with a new transport attempt emits no second acceptance. Restart
replays receipt events. Typed rejection events retain the original receipt's
position. A target rollover must retain the old exact service/storage and add
the old full target to `RetainedTargetVersions` before independent lookup can
resolve a lost acknowledgement; it never relabels an old receipt.

## Build and installation handoff

For local source verification, build and execute the test project individually:

```bash
dotnet build tests/Hexalith.Conversations.DeletionConformance.Tests/Hexalith.Conversations.DeletionConformance.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
dotnet tests/Hexalith.Conversations.DeletionConformance.Tests/bin/Debug/net10.0/Hexalith.Conversations.DeletionConformance.Tests.dll -xml /tmp/deletion-conformance-tests.xml
```

After independently accepted installation inputs exist, prepare a separately
deployed local Debug process artifact with the SDK publish path:

```bash
dotnet publish tools/Hexalith.Conversations.DeletionConformance/Hexalith.Conversations.DeletionConformance.csproj -c Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -o /tmp/conversations-deletion-conformance
```

The platform owner installs that artifact under its own application identity and
sidecar, binds the protected configuration above, and runs
`DOTNET_ENVIRONMENT=Production dotnet Hexalith.Conversations.DeletionConformance.dll` from the installed
directory. CI shipping uses the repository's Release/package-reference policy.
The platform owns the process, sidecar, authenticated route, EventStore domain
registration and persistent store. Building or publishing this artifact does
not perform any of those installation steps or authorize a cohort.

No receiver is installed by this source change. No enrolled credentials, current
source authority, protection commitment or replacement owner acceptance was
available during implementation. OQ prerequisites must pass before any C1–C4
cohort; otherwise only the pinned owner's refusal command may run.
