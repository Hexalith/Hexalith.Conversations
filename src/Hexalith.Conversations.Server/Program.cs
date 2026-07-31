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
builder.Services.AddConversationQueries(builder.Configuration);

WebApplication app = builder.Build();

// DAPR pub/sub delivery for the consumed Tenants domain events is platform-owned: because
// AddConversationTenantAccess registers a consumer, UseEventStoreDomainService wires CloudEvents
// unwrapping, the /tenants/events route, and /dapr/subscribe. This module previously re-typed those three
// calls itself, which AC3 forbids; pass-10 decision D1 promoted them into the SDK.
//
// DISCLOSED (pass-10 decision D2): having that projection fed does not make it durable. It resolves to the
// platform default InMemoryTenantProjectionStore, so it is per-replica and lost on restart, and DAPR does
// not redeliver acked events. Conversations is SINGLE-REPLICA-ONLY until the deferred durable tenant
// access projection lands; a restart or a second replica leaves that instance denying legitimate callers.
app.UseEventStoreDomainService();

app.Run();
