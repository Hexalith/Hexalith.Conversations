// <copyright file="ConversationsAppHostRuntimeBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Hexalith.Conversations.AppHost;
using Hexalith.EventStore.Aspire;

using System.Net;

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Exercises the retained AppHost through the running EventStore and Conversations production hosts.
/// </summary>
public sealed class ConversationsAppHostRuntimeBoundaryTest
{
    private const string RuntimeTestEnvironmentVariable = "HEXALITH_RUN_APPHOST_BOUNDARY_TESTS";

    /// <summary>
    /// Starts the real AppHost and proves both sides of the production Server/EventStore boundary become healthy.
    /// </summary>
    /// <remarks>
    /// This test is opt-in because it starts project processes, Dapr sidecars, and infrastructure containers.
    /// Set <c>HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true</c> in the dedicated runtime lane. Keycloak is disabled so
    /// the test isolates the EventStore/Conversations hosting boundary under review.
    /// </remarks>
    [Fact]
    public async Task RetainedAppHostShouldRunEventStoreAndConversationsProductionBoundary()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(RuntimeTestEnvironmentVariable),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Skip(
                $"Set {RuntimeTestEnvironmentVariable}=true to run the Docker/Dapr AppHost boundary test.");
        }

        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(5));
        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Conversations_AppHost>(
                [$"--{HexalithEventStoreSecurityOptions.DefaultEnableKeycloakConfigurationKey}=false"],
                timeout.Token);

        await using DistributedApplication application = await builder.BuildAsync(timeout.Token);
        await application.StartAsync(timeout.Token);

        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            ConversationsAppHostTopology.EventStoreResourceName,
            timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            ConversationsAppHostTopology.ConversationsResourceName,
            timeout.Token);

        using HttpClient eventStore = application.CreateHttpClient(
            ConversationsAppHostTopology.EventStoreResourceName,
            "http");
        using HttpClient conversations = application.CreateHttpClient(
            ConversationsAppHostTopology.ConversationsResourceName,
            "http");

        using HttpResponseMessage eventStoreHealth = await eventStore.GetAsync("/alive", timeout.Token);
        using HttpResponseMessage conversationsHealth = await conversations.GetAsync("/alive", timeout.Token);

        eventStoreHealth.StatusCode.ShouldBe(HttpStatusCode.OK);
        conversationsHealth.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
