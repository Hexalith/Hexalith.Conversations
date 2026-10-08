// <copyright file="ConversationGatewaySecurityTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.DomainService;

using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Probes the production sidecar-channel and workload policies on the running live host.</summary>
[Collection(ConversationGatewayLiveCollection.Name)]
public sealed class ConversationGatewaySecurityTests(ConversationGatewayLiveFixture fixture)
{
    /// <summary>Checks an actor discovery route rejects absent or incorrect channel credentials.</summary>
    [Fact]
    public async Task ActorDiscoveryShouldRequireItsOwnChannelToken()
    {
        fixture.RequireAvailable();
        using HttpClient client = new();
        foreach (string? token in new string?[] { null, "incorrect-channel-token" })
        {
            using HttpRequestMessage request = new(HttpMethod.Get, fixture.ApplicationHttpEndpoint + "/dapr/config");
            if (token is not null)
            {
                request.Headers.Add(DaprAppChannelToken.HeaderName, token);
            }

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using HttpRequestMessage allowed = fixture.CreateAppChannelRequest("/dapr/config");
        using HttpResponseMessage allowedResponse = await client.SendAsync(allowed, TestContext.Current.CancellationToken);
        allowedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Checks operational metadata requires an assertion scoped to the exact caller, audience, and operation.</summary>
    /// <param name="audience">The assertion's receiving audience, or current for this fixture.</param>
    /// <param name="operation">The assertion's granted operation.</param>
    /// <param name="callerMatches">Whether Dapr's attributed caller matches the signed workload.</param>
    [Theory]
    [InlineData("other-service", "domain-service:metadata", true)]
    [InlineData("current", "domain-service:query", true)]
    [InlineData("current", "domain-service:metadata", false)]
    public async Task MetadataShouldRejectMisboundWorkload(string audience, string operation, bool callerMatches)
    {
        fixture.RequireAvailable();
        using HttpClient client = new();
        using HttpRequestMessage missing = CreateMetadataRequest();
        using HttpResponseMessage missingResponse = await client.SendAsync(missing, TestContext.Current.CancellationToken);
        missingResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        IWorkloadAssertionIssuer issuer = fixture.Services.GetRequiredService<IWorkloadAssertionIssuer>();
        string? assertion = await issuer.IssueAsync(new WorkloadAssertionRequest(
            audience == "current" ? fixture.AppId : audience, operation), TestContext.Current.CancellationToken);
        assertion.ShouldNotBeNullOrWhiteSpace();
        using HttpRequestMessage wrong = CreateMetadataRequest();
        wrong.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        wrong.Headers.Add(EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName,
            callerMatches ? fixture.AppId : "untrusted-workload");
        using HttpResponseMessage wrongResponse = await client.SendAsync(wrong, TestContext.Current.CancellationToken);
        (wrongResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden).ShouldBeTrue();

        string? correct = await issuer.IssueAsync(new WorkloadAssertionRequest(fixture.AppId, "domain-service:metadata"),
            TestContext.Current.CancellationToken);
        correct.ShouldNotBeNullOrWhiteSpace();
        using HttpRequestMessage allowed = CreateMetadataRequest();
        allowed.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, correct);
        allowed.Headers.Add(EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName, fixture.AppId);
        using HttpResponseMessage allowedResponse = await client.SendAsync(allowed, TestContext.Current.CancellationToken);
        allowedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    /// <summary>Builds the actual POST metadata request used by the platform catalog refresher.</summary>
    /// <returns>A caller-owned request with the real channel credential and valid metadata payload.</returns>
    private HttpRequestMessage CreateMetadataRequest()
    {
        HttpRequestMessage request = fixture.CreateAppChannelRequest("/admin/operational-index-metadata", HttpMethod.Post);
        request.Content = JsonContent.Create(new AdminOperationalIndexMetadata.Request([ConversationProjectionHandler.ConversationDomain])
        {
            AppId = fixture.AppId,
            ServiceVersion = ConversationGatewayLiveFixture.ServiceVersion,
        });
        return request;
    }
}
