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

using System.Diagnostics;
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
        // Eight minutes: the provenance prebuild now stamps both launchable configurations before the
        // AppHost starts, and the projection population poll runs after command completion.
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(8));
        string gatewayProjectPath = await BuildEventStoreGatewayWithProvenanceAsync(timeout.Token);
        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Conversations_AppHost>(
                [$"--{HexalithEventStoreSecurityOptions.DefaultEnableKeycloakConfigurationKey}=false"],
                timeout.Token);

        // The provenance stamp proves nothing unless the binary Aspire launches IS a stamped one: bind the
        // model's resolved project path to the prebuilt checkout, and require SuppressBuild so Aspire cannot
        // rebuild the gateway (which would overwrite the stamp) between the assertion and the launch.
        IResource eventStoreResource = builder.Resources.Single(resource =>
            string.Equals(resource.Name, ConversationsAppHostTopology.EventStoreResourceName, StringComparison.Ordinal));
        IProjectMetadata gatewayMetadata = eventStoreResource.Annotations.OfType<IProjectMetadata>().Single();
        Path.GetFullPath(gatewayMetadata.ProjectPath).ShouldBe(
            Path.GetFullPath(gatewayProjectPath),
            "Aspire must launch the same EventStore checkout the provenance prebuild stamped");
        object? suppressBuild = gatewayMetadata.GetType()
            .GetProperty("SuppressBuild")?
            .GetValue(gatewayMetadata);
        suppressBuild.ShouldBe(
            true,
            "the gateway project metadata must suppress Aspire's own build so the stamped binary is the launched binary");

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

        // Command completion alone proves the write boundary, not the projection boundary. The projected
        // read models must become queryable through the gateway's public query API — eventstore app to
        // conversation app across DAPR, with route discovery performed by the PLATFORM, not by test
        // scaffolding — so a cross-app catalog or read-store population regression cannot ship while
        // command status stays green.
        JsonElement detailPayload = await PollForProjectedReadModelAsync(
            eventStore,
            tenantId,
            conversationId,
            timeout.Token);
        ReadIdentifier(detailPayload.GetProperty("details").GetProperty("conversationId"))
            .ShouldBe(conversationId);

        (HttpStatusCode listStatus, string listBody) = await SubmitQueryAsync(
            eventStore,
            tenantId,
            conversationId,
            "conversation-list",
            timeout.Token);
        listStatus.ShouldBe(HttpStatusCode.OK, listBody);
        JsonElement listResponse = JsonSerializer.Deserialize<JsonElement>(listBody);
        listResponse.GetProperty("success").GetBoolean().ShouldBeTrue(listBody);
        JsonElement row = listResponse.GetProperty("payload").GetProperty("conversations")
            .EnumerateArray()
            .ShouldHaveSingleItem();
        ReadIdentifier(row.GetProperty("conversationId")).ShouldBe(conversationId);
    }

    private static string? ReadIdentifier(JsonElement identifier)
        => identifier.ValueKind == JsonValueKind.String
            ? identifier.GetString()
            : identifier.GetProperty("value").GetString();

    private static async Task<JsonElement> PollForProjectedReadModelAsync(
        HttpClient gateway,
        string tenantId,
        string conversationId,
        CancellationToken cancellationToken)
    {
        string? lastBody = null;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (HttpStatusCode statusCode, string body) = await SubmitQueryAsync(
                    gateway,
                    tenantId,
                    conversationId,
                    "conversation-detail",
                    cancellationToken);
                lastBody = body;
                if (statusCode == HttpStatusCode.OK)
                {
                    JsonElement response = JsonSerializer.Deserialize<JsonElement>(body);
                    if (response.TryGetProperty("success", out JsonElement success)
                        && success.GetBoolean()
                        && response.TryGetProperty("payload", out JsonElement payload)
                        && payload.TryGetProperty("details", out JsonElement details)
                        && details.ValueKind == JsonValueKind.Object)
                    {
                        return payload;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The projected conversation read model never became queryable through the gateway. Last response: {lastBody}");
        }
    }

    private static async Task<(HttpStatusCode StatusCode, string Body)> SubmitQueryAsync(
        HttpClient gateway,
        string tenantId,
        string conversationId,
        string queryType,
        CancellationToken cancellationToken)
    {
        var query = new
        {
            Tenant = tenantId,
            Domain = "conversations",
            AggregateId = conversationId,
            QueryType = queryType,
        };
        using HttpResponseMessage response = await gateway.PostAsJsonAsync("/api/v1/queries", query, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    private static async Task<JsonElement> PollUntilTerminalAsync(
        HttpClient client,
        string messageId,
        CancellationToken cancellationToken)
    {
        string? lastBody = null;

        // GetAsync and Task.Delay throw OperationCanceledException when the shared budget expires, so the
        // informative timeout diagnostic is raised from the catch — a bare while-condition check could never
        // reach it and the last response body would be lost.
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Command {messageId} did not reach a terminal status. Last response: {lastBody}");
        }
    }

    private static async Task<string> ReadFailureLogsAsync(
        DistributedApplication application,
        IReadOnlyList<string> filters,
        CancellationToken cancellationToken)
    {
        // Diagnostics must never replace or outlive the primary failure: log streams of still-running
        // resources do not complete, so collection is bounded by its own short window, and a missing
        // resource state degrades to a note instead of a second assertion failure.
        using CancellationTokenSource collection = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        collection.CancelAfter(TimeSpan.FromSeconds(15));
        ResourceLoggerService resourceLogs = application.Services.GetRequiredService<ResourceLoggerService>();
        var all = new List<string>();
        var matching = new List<string>();
        foreach (string resourceName in new[]
                 {
                     ConversationsAppHostTopology.EventStoreResourceName,
                     ConversationsAppHostTopology.ConversationsResourceName,
                 })
        {
            if (!application.ResourceNotifications.TryGetCurrentState(resourceName, out ResourceEvent? resourceEvent))
            {
                all.Add($"[{resourceName}] Resource state was unavailable while collecting diagnostics.");
                continue;
            }

            try
            {
                await foreach (IReadOnlyList<LogLine> batch in resourceLogs
                                   .GetAllAsync(resourceEvent!.Resource)
                                   .WithCancellation(collection.Token))
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
            catch (OperationCanceledException) when (collection.IsCancellationRequested)
            {
                // The bounded window elapsed; whatever was captured is enough for the diagnostic.
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
            domains = new[] { "conversation", "conversations" },
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

    private static async Task<string> BuildEventStoreGatewayWithProvenanceAsync(CancellationToken cancellationToken)
    {
        string repositoryRoot = FindRepositoryRoot();
        string eventStoreRoot = Path.Combine(repositoryRoot, "references", "Hexalith.EventStore");
        ProcessResult revision = await RunProcessAsync(
            "git",
            ["rev-parse", "HEAD"],
            eventStoreRoot,
            cancellationToken);
        revision.ExitCode.ShouldBe(0, revision.StandardError);
        string headRevision = revision.StandardOutput.Trim();
        headRevision.ShouldNotBeNullOrWhiteSpace();
        string sourceRevision = await ComputeWorkspaceRevisionAsync(
            eventStoreRoot,
            headRevision,
            cancellationToken);

        string projectPath = Path.Combine(
            eventStoreRoot,
            "src",
            "Hexalith.EventStore",
            "Hexalith.EventStore.csproj");

        // Aspire launches with --no-build (SuppressBuild, asserted at the call site) and dotnet run resolves
        // its own configuration, so the stamp must exist in EVERY configuration a launch could resolve —
        // stamping only the test's configuration would let a stale, unstamped binary from the other one run
        // while the assertion below stays green.
        foreach (string configuration in new HashSet<string>(
                     [FindTestBuildConfiguration(), "Debug", "Release"],
                     StringComparer.OrdinalIgnoreCase))
        {
            ProcessResult build = await RunProcessAsync(
                "dotnet",
                [
                    "build",
                    projectPath,
                    "--configuration",
                    configuration,
                    "-m:1",
                    $"-p:SourceRevisionId={sourceRevision}",
                    $"-p:InformationalVersion=1.0.0+{sourceRevision}",
                ],
                eventStoreRoot,
                cancellationToken);
            build.ExitCode.ShouldBe(
                0,
                $"The {configuration} EventStore gateway prebuild failed.{Environment.NewLine}"
                + $"{build.StandardOutput}{Environment.NewLine}{build.StandardError}");

            string gatewayAssembly = Path.Combine(
                eventStoreRoot,
                "src",
                "Hexalith.EventStore",
                "bin",
                configuration,
                "net10.0",
                "Hexalith.EventStore.dll");
            File.Exists(gatewayAssembly).ShouldBeTrue(gatewayAssembly);
            string? productVersion = FileVersionInfo.GetVersionInfo(gatewayAssembly).ProductVersion;
            productVersion.ShouldNotBeNullOrWhiteSpace();
            productVersion.ShouldContain(
                sourceRevision,
                Case.Insensitive,
                $"the {configuration} gateway binary Aspire may launch must carry the reviewed EventStore revision");
        }

        return projectPath;
    }

    private static async Task<string> ComputeWorkspaceRevisionAsync(
        string repositoryRoot,
        string headRevision,
        CancellationToken cancellationToken)
    {
        ProcessResult diff = await RunProcessAsync(
            "git",
            ["diff", "--binary", "--no-ext-diff", "HEAD", "--", "."],
            repositoryRoot,
            cancellationToken);
        diff.ExitCode.ShouldBe(0, diff.StandardError);
        ProcessResult untracked = await RunProcessAsync(
            "git",
            ["ls-files", "--others", "--exclude-standard", "-z"],
            repositoryRoot,
            cancellationToken);
        untracked.ExitCode.ShouldBe(0, untracked.StandardError);
        string[] untrackedPaths = untracked.StandardOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (diff.StandardOutput.Length == 0 && untrackedPaths.Length == 0)
        {
            return headRevision;
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(diff.StandardOutput));
        foreach (string relativePath in untrackedPaths)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(relativePath));
            hash.AppendData([0]);
            hash.AppendData(await File.ReadAllBytesAsync(
                Path.Combine(repositoryRoot, relativePath),
                cancellationToken));
        }

        string workspaceHash = Convert.ToHexStringLower(hash.GetHashAndReset())[..12];
        return $"{headRevision}.dirty.{workspaceHash}";
    }

    private static string FindTestBuildConfiguration()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory?.Parent is not null)
        {
            if (string.Equals(directory.Parent.Name, "bin", StringComparison.OrdinalIgnoreCase))
            {
                return directory.Name;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not infer the active test build configuration.");
    }

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

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return new(process.ExitCode, await standardOutput, await standardError);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
