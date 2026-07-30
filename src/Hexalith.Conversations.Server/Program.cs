// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.DataProtection;
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
builder.Services.AddConversationQueries(builder.Configuration);

WebApplication app = builder.Build();

// DAPR pub/sub delivery for the consumed Tenants domain events. Without these three calls the registered
// tenants consumer (AddConversationTenantAccess -> AddHexalithTenants) is never reachable: no
// /tenants/events route exists, the tenants.events topic is never announced to DAPR, and the local tenant
// access projection stays empty — so every authorized read fails closed forever. Mirrors the sibling
// Tenants host wiring (UseCloudEvents before endpoint mapping, MapSubscribeHandler for topic discovery).
app.UseCloudEvents();

app.UseEventStoreDomainService();

app.MapEventStoreDomainEvents();
app.MapSubscribeHandler();

app.Run();
