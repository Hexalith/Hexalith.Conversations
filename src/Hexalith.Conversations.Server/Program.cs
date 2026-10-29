// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
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

WebApplication app = builder.Build();

// DAPR pub/sub delivery for the consumed Tenants domain events is platform-owned: because
// AddConversationTenantAccess registers a consumer, UseEventStoreDomainService wires CloudEvents
// unwrapping and the protected /tenants/events POST route. The required enrolled declarative switch
// suppresses subscription discovery; Platform owns subscription/dead-letter/ACL configuration.
//
// DISCLOSED (pass-10 decision D2): having that projection fed does not make it durable. It resolves to the
// platform default InMemoryTenantProjectionStore, so it is per-replica and lost on restart, and DAPR does
// not redeliver acked events. Conversations is SINGLE-REPLICA-ONLY until the deferred durable tenant
// access projection lands; a restart or a second replica leaves that instance denying legitimate callers.
app.UseEventStoreDomainService();
if (deletionRuntimeEnabled)
{
    app.MapConversationDeletionRuntimeHost();
}

app.Run();
