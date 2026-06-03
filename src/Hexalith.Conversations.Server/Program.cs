// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations;
using Hexalith.Conversations.Server;
using Hexalith.EventStore.DomainService;

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

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
