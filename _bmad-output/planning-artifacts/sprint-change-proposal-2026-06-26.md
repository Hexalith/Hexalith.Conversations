# Sprint Change Proposal - Initialize Conversations AppHost security through the EventStore Aspire extension

_Workflow: bmad-correct-course. Date: 2026-06-26. Mode: Batch. Author: Codex. Status: Approved and implemented._

> Trigger: `HexalithEventStoreSecurityExtensions to initialize the security service in aspire host`.
> `Hexalith.EventStore.Aspire` now owns the reusable security resource and JWT/OIDC wiring helpers, but
> the Conversations AppHost does not initialize that security resource.

---

## Section 1 - Issue Summary

**Problem.** `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` composes EventStore,
the Conversations server, and the admin web resource without calling
`builder.AddHexalithEventStoreSecurity()`. As a result, the local Aspire topology has no canonical
Keycloak-backed `security` resource and does not apply the shared JWT bearer configuration to the
EventStore gateway or Conversations server.

**Why this matters.**

- The architecture requires fail-closed security before aggregate load, command dispatch, projection
  read, admin action, verification, or background work.
- EventStore, FrontComposer, Projects, and Memories AppHosts already use
  `HexalithEventStoreSecurityExtensions` as the canonical local security composition path.
- Keeping Conversations without the shared security initialization lets the AppHost drift from the
  platform contract that Story 3.5 / FR-13 is meant to standardize.

**Issue type:** technical composition gap discovered during implementation. This is an AppHost/security
alignment correction, not a product-scope or UX change.

**Evidence.**

| Location | Current behavior |
|---|---|
| `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` | Adds EventStore and Conversations resources, but no `AddHexalithEventStoreSecurity()` call and no `WithJwtBearerSecurity(...)` wiring. |
| `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs` | Provides `AddHexalithEventStoreSecurity`, `WithJwtBearerSecurity`, `WithSecurityDependency`, `WithEventStoreClientCredentials`, and `WithOpenIdConnectSecurity`. |
| `Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs` | Uses the shared extension and wires EventStore/Tenants/Admin/Sample UI resources through it. |
| `Hexalith.FrontComposer/src/Hexalith.FrontComposer.AppHost/Program.cs` | Has already adopted the same extension through a parallel correct-course change. |

---

## Section 2 - Impact Analysis

### Epic Impact

No epic changes are required. The change reinforces the existing boilerplate-reduction plan:

- Epic 3 / Story 3.5: shared Aspire/Dapr domain-module hosting base.
- FR-13: AppHost/Aspire + Dapr topology expressed through shared hosting capability.
- FR-17: Conversations consumes in-scope promoted capabilities where available.
- FR-20 / NFR3: behavior preservation and fail-closed invariants remain the gate.

### Story Impact

No new story is required. Add this as an implementation task/change record under Story 3.5 or execute it
as a minor direct adjustment before Story 3.5 sign-off.

### Artifact Conflicts

| Artifact | Impact | Action |
|---|---|---|
| PRD | No scope change. Security and tenant isolation requirements are reinforced. | No PRD edit required. |
| Epics | Existing Story 3.5 already covers shared Aspire/Dapr hosting adoption. | No epic edit required. |
| Architecture | Aligns with Authentication & Security and Infrastructure/AppHost decisions. | No architecture edit required. |
| UX | No UI/UX behavior change. | No UX edit required. |
| AppHost code | Directly impacted. Add security resource initialization and JWT wiring. | Edit. |
| AppHost tests | Directly impacted. Topology tests should assert the `security` resource and service dependencies. | Edit. |
| Keycloak realm import | Directly impacted because the shared extension imports `./KeycloakRealms` when Keycloak is enabled. | Add local AppHost realm import asset. |

### Technical Impact

- Add `HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();`.
- Wire JWT bearer settings to EventStore and the Conversations server with `WithJwtBearerSecurity(security)`.
- Surface the security resource through `ConversationsAppHostResources` so tests and future AppHost code can inspect it.
- Keep `EnableKeycloak=false` behavior from the shared extension: no security resource and no JWT wiring when disabled.
- Do not pretend the current static admin web host is authenticated. It has no OIDC/security code today, so adding only JWT/OIDC environment variables there would not enforce access.
- Add a local `src/Hexalith.Conversations.AppHost/KeycloakRealms/hexalith-realm.json` import so the shared extension has a real realm path in tests and local runs.

---

## Section 3 - Recommended Approach

**Selected path: Option 1 - Direct Adjustment.** Effort **Low**, risk **Low**.

This is a narrow AppHost composition fix using an existing shared platform API. No rollback or MVP review
is justified.

**Rejected alternatives.**

- **Rollback:** not applicable; no completed feature needs undoing.
- **MVP review:** not applicable; no product scope changes.
- **Manual Keycloak wiring:** rejected because the shared extension is the platform source of truth and
  already carries `EnableKeycloak=false`, `KeycloakPersistent`, and persistent port behavior.

**Primary risk.** Enabling the shared extension starts a `security` resource by default in local Aspire.
Developers who do not want Keycloak must continue to set `EnableKeycloak=false`, matching EventStore and
FrontComposer behavior.

---

## Section 4 - Detailed Change Proposals

### 4a. `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs`

**OLD:**

```csharp
IResourceBuilder<ProjectResource> eventStoreProject = builder.AddHexalithEventStoreGatewayProject(EventStoreResourceName);
HexalithEventStoreResources eventStoreResources = builder.AddHexalithEventStore(
    eventStoreProject,
    adminServer: null,
    adminUI: null);

IResourceBuilder<ProjectResource> conversationsServer = builder.AddProject<Projects.Hexalith_Conversations_Server>(ConversationsResourceName);
```

**NEW:**

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();

IResourceBuilder<ProjectResource> eventStoreProject = builder.AddHexalithEventStoreGatewayProject(EventStoreResourceName);
HexalithEventStoreResources eventStoreResources = builder.AddHexalithEventStore(
    eventStoreProject,
    adminServer: null,
    adminUI: null);

IResourceBuilder<ProjectResource> conversationsServer = builder.AddProject<Projects.Hexalith_Conversations_Server>(ConversationsResourceName);

if (security is not null)
{
    _ = eventStoreResources.EventStore.WithJwtBearerSecurity(security);
    _ = conversationsServer.WithJwtBearerSecurity(security);
}
```

**Rationale:** Centralizes the security resource and JWT bearer environment contract in
`Hexalith.EventStore.Aspire`, matching the EventStore/FrontComposer pattern and avoiding a parallel
Conversations-only security path.

### 4b. `src/Hexalith.Conversations.AppHost/ConversationsAppHostResources.cs`

**OLD:**

```csharp
public sealed record ConversationsAppHostResources(
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<ProjectResource> EventStore,
    IResourceBuilder<ProjectResource> ConversationsServer,
    IResourceBuilder<ProjectResource> AdminWeb);
```

**NEW:**

```csharp
public sealed record ConversationsAppHostResources(
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<ProjectResource> EventStore,
    IResourceBuilder<ProjectResource> ConversationsServer,
    IResourceBuilder<ProjectResource> AdminWeb,
    HexalithEventStoreSecurityResources? Security);
```

**Rationale:** Keeps topology tests and downstream composition honest about whether the security resource
exists.

### 4c. `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs`

Update the topology tests to assert:

- `resources.Security` is not null when Keycloak is enabled.
- The Aspire resource list includes `security`.
- EventStore references and waits for `security`.
- Conversations server references and waits for `security`.
- `EnableKeycloak=false` omits the `security` resource and leaves the rest of the topology intact.

### 4d. Verification plan

Required:

```sh
dotnet test tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj --tlp:off -v:minimal /nr:false
dotnet build src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj -c Release --tlp:off /nr:false
```

Recommended smoke checks if local prerequisites are available:

- Start the Conversations AppHost and confirm the Aspire dashboard shows `security` when Keycloak is enabled.
- Start with `EnableKeycloak=false` and confirm the stack omits `security` but keeps EventStore, Conversations, admin web, `statestore`, and `pubsub`.
- Confirm EventStore and Conversations server receive JWT bearer authority/issuer/audience environment settings when security is enabled.

---

## Section 5 - Implementation Handoff

**Scope classification: Minor.** One AppHost topology helper, one resource record, and topology tests are
affected. No PRD, epic, story, UX, domain, schema, public package API, or submodule edit is required.

**Route to:** Developer agent for direct implementation after approval.

**Implementation tasks:**

1. Initialize security in `ConversationsAppHostTopology.AddConversations(...)`.
2. Wire EventStore and Conversations server with `WithJwtBearerSecurity(security)` when enabled.
3. Add `Security` to `ConversationsAppHostResources`.
4. Update topology tests for the enabled and disabled security paths.
5. Add the local Keycloak realm import asset used by the shared extension.
6. Run the AppHost test project and Release AppHost build.

**Success criteria:**

- `ConversationsAppHostTopology` has a single security initialization path via
  `HexalithEventStoreSecurityExtensions`.
- EventStore and Conversations server reference and wait for the `security` resource when Keycloak is enabled.
- `EnableKeycloak=false` keeps the existing no-Keycloak local path available.
- AppHost topology tests pass.
- Release AppHost build passes with warnings-as-errors.

**Implementation evidence - completed 2026-06-26:**

- `src/Hexalith.Conversations.AppHost/ConversationsAppHostTopology.cs` now initializes security through
  `builder.AddHexalithEventStoreSecurity(...)`, resolves the local `KeycloakRealms` import path, and wires
  EventStore plus the Conversations server through `WithJwtBearerSecurity(security)`.
- `src/Hexalith.Conversations.AppHost/ConversationsAppHostResources.cs` now exposes the optional
  `HexalithEventStoreSecurityResources`.
- `src/Hexalith.Conversations.AppHost/KeycloakRealms/hexalith-realm.json` was added from the canonical
  EventStore AppHost realm import.
- `tests/Hexalith.Conversations.AppHost.Tests/ConversationsAppHostTopologyTest.cs` now covers the enabled
  security topology and the `EnableKeycloak=false` path.
- Verification:
  - `dotnet test tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj --tlp:off -v:minimal /nr:false` passed: 7 total, 0 failed.
  - `dotnet test tests/Hexalith.Conversations.AppHost.Tests/Hexalith.Conversations.AppHost.Tests.csproj -c Release --tlp:off -v:minimal /nr:false` passed: 7 total, 0 failed.
  - `dotnet build src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj -c Release --tlp:off /nr:false` passed with 0 warnings and 0 errors.

---

## Checklist Status - Change Navigation

- **1.1 Triggering story:** N/A. Direct AppHost composition correction requested by user; closest existing story is Epic 3 / Story 3.5.
- **1.2 Core problem:** Done. Conversations AppHost lacks shared security initialization.
- **1.3 Evidence:** Done. Topology code lacks `AddHexalithEventStoreSecurity`; EventStore/FrontComposer/Projects/Memories show the canonical pattern.
- **2.1 Current epic impact:** Done. Existing Epic 3 / Story 3.5 remains valid.
- **2.2 Epic-level changes:** N/A.
- **2.3 Future epic review:** Done. No future epic invalidated.
- **2.4 New epic need:** N/A.
- **2.5 Epic order/priority:** N/A.
- **3.1 PRD conflict:** N/A. Security requirements are reinforced.
- **3.2 Architecture conflict:** Done. Alignment with Authentication & Security / Infrastructure decisions.
- **3.3 UI/UX conflict:** N/A. No UI change.
- **3.4 Other artifacts:** Done. AppHost code and tests affected.
- **4.1 Direct Adjustment:** Viable, selected. Low effort, low risk.
- **4.2 Rollback:** Not viable / not applicable.
- **4.3 PRD MVP Review:** Not viable / not applicable.
- **4.4 Recommended path:** Done. Direct Adjustment.
- **5.1 Issue summary:** Done.
- **5.2 Impact and artifact needs:** Done.
- **5.3 Path forward:** Done.
- **5.4 MVP impact/action plan:** Done. MVP unaffected.
- **5.5 Agent handoff:** Done. Developer agent after approval.
- **6.1 Checklist completion:** Done for proposal stage; user approval pending.
- **6.2 Proposal accuracy:** Done.
- **6.3 User approval:** Done. User approved implementation on 2026-06-26.
- **6.4 Sprint status update:** N/A. No epic/story add/remove/renumbering.
- **6.5 Next steps:** Done. Routed to Developer agent and implemented directly.
