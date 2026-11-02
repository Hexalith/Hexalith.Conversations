// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Canonical Hexalith EventStore domain-service host (FR-3). The SDK owns all hosting, DAPR-endpoint,
// observability, and convention-discovery boilerplate; this module writes only its domain code plus this
// two-line host. The explicit-assemblies overload is used (never the calling-assembly one) so discovery
// targets the Conversations domain assembly — where ConversationAggregate lives — plus the Server boundary
// assembly, so future IDomainQueryHandler / IDomainProjectionHandler implementations (Stories 2.3 / 2.5)
// are discovered without re-touching this host.
builder.AddEventStoreDomainService(
    typeof(ConversationsAssemblyMarker).Assembly,
    typeof(ServerAssemblyMarker).Assembly);

// Register the conversation query boundary so the SDK-discovered IDomainQueryHandler adapter(s) (Story 2.3)
// can resolve the ConversationQueryHandler, its tenant-access gate, and the protected list-cursor codec
// (IQueryCursorCodec, Data Protection backed) the /query dispatch path constructs. The SDK assembly scan
// discovers the adapter type itself; this registers its dependency graph.
builder.Services.AddConversationTenantAccess(builder.Configuration);
builder.Services.AddConversationQueries(builder.Configuration);

// EXT-CONV-AI-1 deletion runtime: explicit opt-in only. Absent or false keeps this host's fail-closed Unavailable
// defaults unchanged. When enabled, the host validates its complete configuration and refuses to start until the
// independently installed authority, approval and publication providers are present.
bool deletionRuntimeEnabled = builder.Configuration.GetValue<bool>(ConversationDeletionRuntimeOptions.EnabledKey);
if (deletionRuntimeEnabled)
{
    builder.Services.AddConversationDeletionRuntimeHost(builder.Configuration);
}

builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(
    ConversationsCatalogDeclarations.Projections.Where(route => route.ProjectionType == "conversation").ToArray()));
builder.Services.AddEventStoreDevelopmentDomainCatalog(builder.Configuration,
    typeof(ConversationsCatalogDeclarations).Assembly, typeof(ConversationsCatalogAdapters).Assembly);

WebApplication app = builder.Build();

// DAPR pub/sub delivery for the consumed Tenants domain events is platform-owned: because
// AddConversationTenantAccess registers a consumer, UseEventStoreDomainService wires CloudEvents
// unwrapping and the protected /tenants/events POST route. The required enrolled declarative switch
// suppresses subscription discovery; Platform owns subscription/dead-letter/ACL configuration.
//
// The enrolled AddConversationTenantAccess path persists that projection in the configured read-model state
// store through the EventStore checkpointed writer (Story 16.1): one compare-and-swap record per tenant holds
// state, health and checkpoint, and only a healthy record authorizes. Tests prove that a restarted host and a
// second replica read the same record from a local Dapr/Redis store, with deliveries and Tenants history supplied
// in process; they do not prove Platform pub/sub delivery or the gateway history grant. The delivery that
// persists a gap attempts recovery immediately, and later deliveries retry it by reading the retained
// `system`/`tenants` history through the EventStore gateway under the platform scope `platform:tenants`.
// Platform must grant that read (`tenants:read`) and retry the tenants subscription without dead-lettering
// (module/platform-handoff.md).
app.UseEventStoreDomainService();
if (deletionRuntimeEnabled)
{
    app.MapConversationDeletionRuntimeHost();
}

app.Run();
