// <copyright file="ConversationsAppHostRuntimeBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.Conversations.AppHost;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Aspire;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Exercises the retained AppHost through the running EventStore and Conversations production hosts.
/// </summary>
public sealed class ConversationsAppHostRuntimeBoundaryTest
{
    private const string SigningKey = "DevOnlySigningKey-AtLeast32Chars!";

    /// <summary>
    /// Starts the real AppHost and submits a command through EventStore to the Conversations production host.
    /// </summary>
    /// <remarks>
    /// Keycloak is disabled so the test isolates the EventStore/Conversations hosting boundary under review.
    /// </remarks>
    [Fact]
    public async Task RetainedAppHostShouldRunEventStoreAndConversationsProductionBoundary()
    {
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
        string tenantId = $"apphost-{Guid.NewGuid():N}";
        string conversationId = $"conversation-{Guid.NewGuid():N}";
        string messageId = Guid.NewGuid().ToString("N");
        string correlationId = Guid.NewGuid().ToString("N");
        eventStore.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken(tenantId));

        CreateConversation command = new(
            new CreateConversationCommand(
                new ConversationCommandMetadata(
                    SchemaVersion.Current,
                    new TenantId(tenantId),
                    new PartyId("apphost-boundary-actor"),
                    correlationId,
                    messageId,
                    $"idempotency-{messageId}"),
                Label: "AppHost production boundary"),
            new ConversationId(conversationId),
            DateTimeOffset.UtcNow,
            $"event-{messageId}");
        JsonElement payload = JsonSerializer.SerializeToElement(command, command.GetType());
        var request = new
        {
            MessageId = messageId,
            Tenant = tenantId,
            Domain = "conversation",
            AggregateId = conversationId,
            CommandType = nameof(CreateConversation),
            Payload = payload,
            CorrelationId = correlationId,
        };

        using HttpResponseMessage submission =
            await eventStore.PostAsJsonAsync("/api/v1/commands", request, timeout.Token);
        string submissionBody = await submission.Content.ReadAsStringAsync(timeout.Token);
        if (submission.StatusCode != HttpStatusCode.Accepted)
        {
            string resourceLogs = await ReadFailureLogsAsync(
                application,
                [correlationId, messageId, "error", "exception", "fail"],
                timeout.Token);
            submission.StatusCode.ShouldBe(
                HttpStatusCode.Accepted,
                $"{submissionBody}{Environment.NewLine}{resourceLogs}");
        }

        JsonElement status = await PollUntilTerminalAsync(eventStore, messageId, timeout.Token);
        status.GetProperty("status").GetString().ShouldBe("Completed");
        status.GetProperty("aggregateId").GetString().ShouldBe(conversationId);
        status.GetProperty("eventCount").GetInt32().ShouldBeGreaterThan(0);
    }

    private static async Task<JsonElement> PollUntilTerminalAsync(
        HttpClient client,
        string messageId,
        CancellationToken cancellationToken)
    {
        string? lastBody = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            using HttpResponseMessage response =
                await client.GetAsync($"/api/v1/commands/status/{messageId}", cancellationToken);
            lastBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                JsonElement status = JsonSerializer.Deserialize<JsonElement>(lastBody);
                string? state = status.GetProperty("status").GetString();
                if (state is "Completed" or "Rejected" or "PublishFailed" or "TimedOut")
                {
                    return status;
                }
            }
            else if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.StatusCode.ShouldBe(HttpStatusCode.OK, lastBody);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new TimeoutException($"Command {messageId} did not reach a terminal status. Last response: {lastBody}");
    }

    private static async Task<string> ReadFailureLogsAsync(
        DistributedApplication application,
        IReadOnlyList<string> filters,
        CancellationToken cancellationToken)
    {
        ResourceLoggerService resourceLogs = application.Services.GetRequiredService<ResourceLoggerService>();
        var all = new List<string>();
        var matching = new List<string>();
        foreach (string resourceName in new[]
                 {
                     ConversationsAppHostTopology.EventStoreResourceName,
                     ConversationsAppHostTopology.ConversationsResourceName,
                 })
        {
            application.ResourceNotifications.TryGetCurrentState(resourceName, out ResourceEvent? resourceEvent)
                .ShouldBeTrue($"Resource state for {resourceName} was unavailable while collecting diagnostics.");
            await foreach (IReadOnlyList<LogLine> batch in resourceLogs
                               .GetAllAsync(resourceEvent!.Resource)
                               .WithCancellation(cancellationToken))
            {
                foreach (LogLine line in batch)
                {
                    string decorated = $"[{resourceName}] {line.Content}";
                    all.Add(decorated);
                    if (filters.Any(filter => line.Content.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    {
                        matching.Add(decorated);
                    }
                }
            }
        }

        IReadOnlyList<string> selected = matching.Count == 0 ? all : matching;
        return selected.Count == 0
            ? "No EventStore/Conversations resource logs were captured."
            : string.Join(Environment.NewLine, selected.TakeLast(120));
    }

    private static string CreateAccessToken(string tenantId)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        string payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = "apphost-boundary-actor",
            iss = "hexalith-dev",
            aud = "hexalith-eventstore",
            nbf = now - 30,
            iat = now,
            exp = now + 600,
            tenants = new[] { tenantId },
            domains = new[] { "conversation" },
            permissions = new[] { "command:submit", "command:query" },
        }));
        string unsignedToken = $"{header}.{payload}";
        byte[] signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(SigningKey),
            Encoding.ASCII.GetBytes(unsignedToken));
        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
