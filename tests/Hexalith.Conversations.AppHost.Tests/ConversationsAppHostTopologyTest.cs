// <copyright file="ConversationsAppHostTopologyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Conversations.AppHost;

namespace Hexalith.Conversations.AppHost.Tests;

public sealed class ConversationsAppHostTopologyTest
{
    [Fact]
    public void ConversationsAppHostShouldExposeStableResourceNames()
    {
        ConversationsAppHostTopology.EventStoreResourceName.ShouldBe("eventstore");
        ConversationsAppHostTopology.ConversationsResourceName.ShouldBe("conversations");
        ConversationsAppHostTopology.AdminWebResourceName.ShouldBe("conversations-admin-web");
        ConversationsAppHostTopology.StateStoreComponentName.ShouldBe("statestore");
        ConversationsAppHostTopology.PubSubComponentName.ShouldBe("pubsub");
    }

    [Fact]
    public void ConversationsAppHostShouldModelEventStoreServerAdminAndSharedDaprResources()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.EventStore.Resource.Name.ShouldBe(ConversationsAppHostTopology.EventStoreResourceName);
        resources.ConversationsServer.Resource.Name.ShouldBe(ConversationsAppHostTopology.ConversationsResourceName);
        resources.AdminWeb.Resource.Name.ShouldBe(ConversationsAppHostTopology.AdminWebResourceName);
        resources.StateStore.Resource.Name.ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        resources.PubSub.Resource.Name.ShouldBe(ConversationsAppHostTopology.PubSubComponentName);

        string[] projectNames = [.. builder.Resources.OfType<ProjectResource>().Select(static resource => resource.Name).Order(StringComparer.Ordinal)];
        projectNames.ShouldBe(
        [
            ConversationsAppHostTopology.ConversationsResourceName,
            ConversationsAppHostTopology.AdminWebResourceName,
            ConversationsAppHostTopology.EventStoreResourceName,
        ]);

        string[] componentNames = [.. builder.Resources.OfType<IDaprComponentResource>().Select(static resource => resource.Name).Order(StringComparer.Ordinal)];
        componentNames.ShouldBe(
        [
            ConversationsAppHostTopology.PubSubComponentName,
            ConversationsAppHostTopology.StateStoreComponentName,
        ]);
    }

    [Fact]
    public void ConversationsServerShouldUseSharedDaprSidecarAndWaitForEventStore()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        DaprSidecarOptions options = GetSidecarOptions(resources.ConversationsServer.Resource);
        options.AppId.ShouldBe(ConversationsAppHostTopology.ConversationsResourceName);

        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.StateStoreComponentName);
        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.PubSubComponentName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
    }

    [Fact]
    public void AdminWebShouldReferenceAndWaitForConversationsServer()
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        ResourceNamesReferencedBy(resources.AdminWeb.Resource).ShouldContain(ConversationsAppHostTopology.ConversationsResourceName);
        ResourceNamesWaitedOnBy(resources.AdminWeb.Resource).ShouldContain(ConversationsAppHostTopology.ConversationsResourceName);
    }

    [Fact]
    public void AddConversationsShouldFailClosedAgainstNullBuilder()
        => Should.Throw<ArgumentNullException>(() => ConversationsAppHostTopology.AddConversations(null!));

    private static DaprSidecarOptions GetSidecarOptions(ProjectResource resource)
        => resource.Annotations
            .OfType<DaprSidecarAnnotation>()
            .SelectMany(static sidecar => sidecar.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>())
            .Select(static annotation => annotation.Options)
            .Single();

    private static string[] ResourceNamesReferencedBySidecar(ProjectResource resource)
        => [.. resource.Annotations
            .OfType<DaprSidecarAnnotation>()
            .SelectMany(static annotation => annotation.Sidecar.Annotations.OfType<DaprComponentReferenceAnnotation>())
            .Select(static annotation => annotation.Component.Name)
            .Order(StringComparer.Ordinal)];

    private static string[] ResourceNamesReferencedBy(ProjectResource resource)
        => [.. resource.Annotations
            .OfType<ResourceRelationshipAnnotation>()
            .Select(static annotation => annotation.Resource.Name)
            .Order(StringComparer.Ordinal)];

    private static string[] ResourceNamesWaitedOnBy(ProjectResource resource)
        => [.. resource.Annotations
            .OfType<WaitAnnotation>()
            .Select(static annotation => annotation.Resource.Name)
            .Order(StringComparer.Ordinal)];
}
