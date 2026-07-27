// <copyright file="ConversationsAppHostTopologyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Conversations.AppHost;
using Hexalith.EventStore.Aspire;

using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace Hexalith.Conversations.AppHost.Tests;

public sealed class ConversationsAppHostTopologyTest
{
    [Fact]
    public void ConversationsAppHostShouldBeMechanicallyNonShipping()
    {
        XDocument project = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Hexalith.Conversations.AppHost",
            "Hexalith.Conversations.AppHost.csproj"));

        string? isPackable = project.Descendants().Single(element => element.Name.LocalName == "IsPackable").Value;
        string? isPublishable = project.Descendants().Single(element => element.Name.LocalName == "IsPublishable").Value;

        isPackable.ShouldBe("false");
        isPublishable.ShouldBe("false");
    }

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
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.EventStore.Resource.Name.ShouldBe(ConversationsAppHostTopology.EventStoreResourceName);
        resources.ConversationsServer.Resource.Name.ShouldBe(ConversationsAppHostTopology.ConversationsResourceName);
        resources.AdminWeb.Resource.Name.ShouldBe(ConversationsAppHostTopology.AdminWebResourceName);
        resources.StateStore.Resource.Name.ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        resources.PubSub.Resource.Name.ShouldBe(ConversationsAppHostTopology.PubSubComponentName);
        resources.Security.ShouldNotBeNull();
        resources.Security!.Keycloak.Resource.Name.ShouldBe(HexalithEventStoreSecurityOptions.DefaultResourceName);

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

        string[] resourceNames = [.. builder.Resources.Select(static resource => resource.Name).Order(StringComparer.Ordinal)];
        resourceNames.ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    [Fact]
    public async Task ConversationsServerShouldUseSharedDaprSidecarAndWaitForEventStore()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        DaprSidecarOptions options = GetSidecarOptions(resources.ConversationsServer.Resource);
        options.AppId.ShouldBe(ConversationsAppHostTopology.ConversationsResourceName);
        options.EnableAppHealthCheck.ShouldBe(true);
        options.AppHealthCheckPath.ShouldBe("/alive");

        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext environmentContext = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resources.ConversationsServer.Resource,
            environment,
            TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resources.ConversationsServer.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(environmentContext);
        }

        environment["EventStore__DomainService__AppId"].ShouldBe(ConversationsAppHostTopology.ConversationsResourceName);
        environment["EventStore__DomainService__ServiceVersion"].ShouldBe("v1");

        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.StateStoreComponentName);
        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.PubSubComponentName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    [Fact]
    public void EventStoreShouldUseSharedSecurityWhenKeycloakIsEnabled()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.Security.ShouldNotBeNull();
        ResourceNamesReferencedBy(resources.EventStore.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.EventStore.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    [Fact]
    public void AdminWebShouldReferenceAndWaitForConversationsServer()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        ResourceNamesReferencedBy(resources.AdminWeb.Resource).ShouldContain(ConversationsAppHostTopology.ConversationsResourceName);
        ResourceNamesWaitedOnBy(resources.AdminWeb.Resource).ShouldContain(ConversationsAppHostTopology.ConversationsResourceName);
    }

    [Fact]
    public void AddConversationsShouldFailClosedAgainstNullBuilder()
        => Should.Throw<ArgumentNullException>(() => ConversationsAppHostTopology.AddConversations(null!));

    [Fact]
    public void AddConversationsShouldOmitSecurityWhenKeycloakIsDisabled()
    {
        IDistributedApplicationBuilder builder = CreateBuilder(enableKeycloak: false);

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.Security.ShouldBeNull();
        builder.Resources.Select(static resource => resource.Name).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesReferencedBy(resources.EventStore.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.EventStore.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    private static IDistributedApplicationBuilder CreateBuilder(bool enableKeycloak = true)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
        if (!enableKeycloak)
        {
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HexalithEventStoreSecurityOptions.DefaultEnableKeycloakConfigurationKey] = "false",
            });
        }

        return builder;
    }

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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
