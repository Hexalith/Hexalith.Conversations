// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
using Hexalith.Conversations.Server;
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
builder.Services.AddConversationTenantAccess();

// The shared domain-service host (AddEventStoreDomainService) does not register a DaprClient, but the SDK
// persisted read-model store (DaprReadModelStore, registered by AddConversationQueries -> FR-5) resolves one.
// Register it here (mirroring the Tenants host); DAPR arrives transitively via Dapr.AspNetCore, so the Server
// takes no direct Dapr.Client reference. TryAdd semantics make this safe even if a future host already does it.
builder.Services.AddDaprClient();
builder.Services.AddConversationQueries(builder.Configuration);

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
