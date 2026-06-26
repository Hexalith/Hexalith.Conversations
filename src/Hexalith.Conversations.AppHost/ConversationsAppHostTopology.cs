// <copyright file="ConversationsAppHostTopology.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.AppHost;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.Commons.Aspire;
using Hexalith.EventStore.Aspire;

/// <summary>
/// Composes the local Conversations Aspire topology.
/// </summary>
public static class ConversationsAppHostTopology
{
    private const string KeycloakRealmsDirectoryName = "KeycloakRealms";

    /// <summary>
    /// The local EventStore command-gateway resource name and Dapr app id.
    /// </summary>
    public const string EventStoreResourceName = "eventstore";

    /// <summary>
    /// The Conversations server resource name and Dapr app id.
    /// </summary>
    public const string ConversationsResourceName = "conversations";

    /// <summary>
    /// The Conversations admin web resource name.
    /// </summary>
    public const string AdminWebResourceName = "conversations-admin-web";

    /// <summary>
    /// The shared Dapr state-store component name.
    /// </summary>
    public const string StateStoreComponentName = "statestore";

    /// <summary>
    /// The shared Dapr pub/sub component name.
    /// </summary>
    public const string PubSubComponentName = "pubsub";

    /// <summary>
    /// Adds the local Conversations topology to an Aspire distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The composed local resource builders.</returns>
    public static ConversationsAppHostResources AddConversations(IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity(
            new HexalithEventStoreSecurityOptions
            {
                RealmImportPath = ResolveKeycloakRealmImportPath(builder.AppHostDirectory),
            });

        IResourceBuilder<ProjectResource> eventStoreProject = builder.AddHexalithEventStoreGatewayProject(EventStoreResourceName);
        HexalithEventStoreResources eventStoreResources = builder.AddHexalithEventStore(
            eventStoreProject,
            adminServer: null,
            adminUI: null);

        IResourceBuilder<ProjectResource> conversationsServer = builder.AddProject<Projects.Hexalith_Conversations_Server>(ConversationsResourceName);
        _ = conversationsServer.AddAspireDaprDomainModule(new AspireDaprDomainModuleOptions(ConversationsResourceName, AspireDaprInfrastructureMode.Shared)
        {
            SharedComponents = new AspireDaprSharedComponents(eventStoreResources.StateStore, eventStoreResources.PubSub),
            References = [eventStoreResources.EventStore],
            WaitFor = [eventStoreResources.EventStore],
        });

        if (security is not null)
        {
            _ = eventStoreResources.EventStore.WithJwtBearerSecurity(security);
            _ = conversationsServer.WithJwtBearerSecurity(security);
        }

        IResourceBuilder<ProjectResource> adminWeb = builder
            .AddProject<Projects.Hexalith_Conversations_Admin_Web>(AdminWebResourceName)
            .WithReference(conversationsServer)
            .WaitFor(conversationsServer);

        return new ConversationsAppHostResources(
            eventStoreResources.StateStore,
            eventStoreResources.PubSub,
            eventStoreResources.EventStore,
            conversationsServer,
            adminWeb,
            security);
    }

    private static string ResolveKeycloakRealmImportPath(string appHostDirectory)
    {
        string[] candidates =
        [
            Path.Combine(appHostDirectory, KeycloakRealmsDirectoryName),
            Path.Combine(Directory.GetCurrentDirectory(), KeycloakRealmsDirectoryName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Hexalith.Conversations.AppHost", KeycloakRealmsDirectoryName),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "Hexalith.Conversations.AppHost",
                KeycloakRealmsDirectoryName)),
        ];

        string? existingPath = candidates.FirstOrDefault(Directory.Exists);
        return existingPath ?? Path.Combine(appHostDirectory, KeycloakRealmsDirectoryName);
    }
}
