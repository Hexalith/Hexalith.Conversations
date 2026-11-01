// <copyright file="ConversationsAppHostTopology.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.AppHost;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.EventStore.Aspire;

using Microsoft.Extensions.Configuration;

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
    /// The Conversations server resource name.
    /// </summary>
    public const string ConversationsResourceName = "conversations";

    /// <summary>
    /// The Conversations domain module's canonical Dapr app id.
    /// </summary>
    /// <remarks>
    /// EventStore routes the canonical <c>conversation</c> domain to the same app id by convention. The Aspire
    /// resource remains plural for display and dependency references, but the service-invocation identity must
    /// match that domain route.
    /// </remarks>
    public const string ConversationsDaprAppId = "conversation";

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
    /// The local realm client the Conversations tenant access uses as its EventStore gateway worker authority.
    /// </summary>
    /// <remarks>
    /// A dedicated confidential service-account client, so Conversations never borrows the EventStore workload
    /// identity. Its secret is a generated AppHost parameter injected into Keycloak, never a literal value.
    /// </remarks>
    public const string TenantAccessWorkerClientId = "conversations-tenant-access";

    /// <summary>The environment variable carrying the tenant-access gateway origin into the Conversations server.</summary>
    public const string TenantAccessGatewayBaseAddressVariable = "Hexalith__Conversations__TenantAccess__GatewayBaseAddress";

    /// <summary>The test-only switch selecting the prebuilt Builds-owned package gateway host.</summary>
    public const string TestingUseBuildsGatewayConfigurationKey = "Conversations:Testing:UseBuildsGateway";

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

        IResourceBuilder<ProjectResource> eventStoreProject = builder.Configuration.GetValue<bool>(TestingUseBuildsGatewayConfigurationKey)
            ? builder.AddProject<PrebuiltEventStoreGatewayProjectMetadata>(EventStoreResourceName).WithHttpEndpoint()
            : builder.AddHexalithEventStoreGatewayProject(EventStoreResourceName);
        HexalithEventStoreResources eventStoreResources = builder.AddHexalithEventStore(
            eventStoreProject,
            adminServer: null,
            adminUI: null);

        IResourceBuilder<ProjectResource> conversationsServer = builder
            .AddProject<Projects.Hexalith_Conversations_Server>(ConversationsResourceName)
            .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithEnvironment("EventStore__CommandStatus__StateStoreName", StateStoreComponentName)
            .WithEnvironment("EventStore__DataProtection__StateStoreName", StateStoreComponentName)
            .WithEnvironment("EventStore__DataProtection__PersistToStateStore", "true")
            .WithEnvironment("EventStore__Projections__ReadModelStateStoreName", StateStoreComponentName)
            .WithEnvironment("EventStore__Projections__CheckpointStateStoreName", StateStoreComponentName)
            .WithEnvironment("EventStore__ProjectionChanges__PubSubName", PubSubComponentName)
            .WithEnvironment("Tenants__PubSubName", PubSubComponentName)
            .WithEnvironment("Tenants__TopicName", "tenants.events")
            .WithEnvironment("Dapr__DeclarativeSubscriptions", "true")

            // The enrolled tenant access reads Tenants history through this gateway and refuses to start without it.
            .WithEnvironment(TenantAccessGatewayBaseAddressVariable, eventStoreResources.EventStore.GetEndpoint("http"))
            .WithHttpEndpoint();
        _ = conversationsServer.AddEventStoreDomainModule(eventStoreResources, ConversationsDaprAppId);
        _ = eventStoreResources.EventStore.WithGeneratedEventStoreAppChannelToken();
        _ = conversationsServer.WithGeneratedEventStoreAppChannelToken();

        if (security is not null)
        {
            bool persistent = bool.TryParse(builder.Configuration[
                HexalithEventStoreSecurityOptions.DefaultPersistentConfigurationKey]?.Trim(), out bool value) && value;
            IResourceBuilder<ParameterResource> workloadSecret = builder.AddParameter(
                "eventstore-workload-client-secret",
                new GenerateParameterDefault { MinLength = 32, Lower = true, Upper = true, Numeric = true, Special = false },
                secret: true,
                persist: persistent);
            _ = security.Keycloak.WithEnvironment("EVENTSTORE_WORKLOAD_CLIENT_SECRET", workloadSecret);
            _ = eventStoreResources.EventStore.WithEventStoreWorkloadClientCredentials(EventStoreResourceName, workloadSecret);
            _ = eventStoreResources.EventStore.WithJwtBearerSecurity(security);
            _ = conversationsServer.WithJwtBearerSecurity(security);

            // The tenant-access gateway worker authority: the same generated-secret plumbing as the EventStore
            // workload, for the dedicated realm client, with the client-credentials grant the enrolled host requires.
            IResourceBuilder<ParameterResource> tenantAccessSecret = builder.AddParameter(
                "conversations-tenant-access-client-secret",
                new GenerateParameterDefault { MinLength = 32, Lower = true, Upper = true, Numeric = true, Special = false },
                secret: true,
                persist: persistent);
            _ = security.Keycloak.WithEnvironment("CONVERSATIONS_TENANT_ACCESS_CLIENT_SECRET", tenantAccessSecret);
            _ = conversationsServer
                .WithEnvironment("EventStore__Authentication__Authority", security.RealmUrl)
                .WithEnvironment(
                    "EventStore__Authentication__TokenEndpoint",
                    ReferenceExpression.Create($"{security.RealmUrl}/protocol/openid-connect/token"))
                .WithEnvironment("EventStore__Authentication__GrantType", "client_credentials")
                .WithEnvironment("EventStore__Authentication__ClientId", TenantAccessWorkerClientId)
                .WithEnvironment("EventStore__Authentication__ClientSecret", tenantAccessSecret);
        }

        IResourceBuilder<ProjectResource> adminWeb = builder
            .AddProject<Projects.Hexalith_Conversations_Admin_Web>(AdminWebResourceName)
            .WithHttpEndpoint()
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
