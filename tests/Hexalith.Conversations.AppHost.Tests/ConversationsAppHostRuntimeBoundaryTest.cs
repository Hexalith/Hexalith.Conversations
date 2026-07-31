// <copyright file="ConversationsAppHostRuntimeBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Hexalith.Commons.UniqueIds;

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
        // Separate budgets. The provenance prebuild stamps BOTH launchable configurations and can dominate a
        // cold machine; sharing one token with the AppHost launch and the projection poll meant a slow
        // prebuild surfaced either as a bare OperationCanceledException from Aspire startup or as a
        // projection-boundary timeout, with nothing distinguishing "the prebuild ate the budget" from "the
        // production boundary is broken" (pass-10 review).
        using CancellationTokenSource prebuildTimeout = new(TimeSpan.FromMinutes(6));
        string gatewayProjectPath;
        string gatewayRevision;

        try
        {
            (gatewayProjectPath, gatewayRevision) =
                await BuildEventStoreGatewayWithProvenanceAsync(prebuildTimeout.Token);
        }
        catch (OperationCanceledException) when (prebuildTimeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The EventStore gateway provenance prebuild exceeded its own 6-minute budget. This is a build "
                + "machine problem, not a production-boundary failure: the AppHost was never started.");
        }

        // Eight minutes for the runtime lane, started only after the prebuild so the boundary under proof
        // always gets its full window: the AppHost starts, and the projection population poll runs after
        // command completion.
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(8));
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

        // Named-projection dispatch is refused with delivery_state_unavailable until the store-global v2
        // writer protocol has been cut over — the documented operator maintenance action. Perform it through
        // the production admin endpoint before the first command so the dispatch under proof is admitted.
        await ActivateProjectionDeliveryAsync(eventStore, HarnessCutoverCommit, timeout.Token);

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

        // Aspire reporting both resources healthy does not mean the gateway can invoke the domain service
        // through DAPR yet: the sidecar's app registration settles independently, and until it does the
        // gateway's standard resilience pipeline exhausts its attempt and total-request timeouts and returns
        // 500. Measured, not assumed — this lane failed on roughly one run in four with exactly that
        // signature, and the gateway logs showed Standard-AttemptTimeout followed by
        // Standard-TotalRequestTimeout with no domain-side error at all.
        //
        // Retrying is safe rather than a mask: the command carries a fixed idempotency key and a fixed
        // MessageId, so a redelivery is the platform's own idempotent path. Only 5xx is retried. A 400 or a
        // 403 is a real rejection and fails on the first response, so a genuine refusal can never be
        // retried into a timeout, and the attempt count is reported so a slow start is never mistaken for a
        // broken boundary.
        HttpStatusCode submissionStatus;
        string submissionBody;
        int attempts = 0;
        DateTimeOffset readinessDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(120);
        while (true)
        {
            attempts++;
            using HttpResponseMessage submission =
                await eventStore.PostAsJsonAsync("/api/v1/commands", request, timeout.Token);
            submissionStatus = submission.StatusCode;
            submissionBody = await submission.Content.ReadAsStringAsync(timeout.Token);
            if (submissionStatus != HttpStatusCode.InternalServerError
                && submissionStatus != HttpStatusCode.BadGateway
                && submissionStatus != HttpStatusCode.ServiceUnavailable
                && submissionStatus != HttpStatusCode.GatewayTimeout)
            {
                break;
            }

            if (DateTimeOffset.UtcNow >= readinessDeadline)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
        }

        if (submissionStatus != HttpStatusCode.Accepted)
        {
            string resourceLogs = await ReadFailureLogsAsync(
                application,
                [correlationId, messageId, "error", "exception", "fail"],
                timeout.Token);
            submissionStatus.ShouldBe(
                HttpStatusCode.Accepted,
                $"command submission was still not accepted after {attempts} attempt(s) across the "
                + $"120-second DAPR invocation-readiness window.{Environment.NewLine}{submissionBody}"
                + $"{Environment.NewLine}{resourceLogs}");
        }

        JsonElement status = await PollUntilTerminalAsync(eventStore, messageId, timeout.Token);
        status.GetProperty("status").GetString().ShouldBe("Completed");
        status.GetProperty("aggregateId").GetString().ShouldBe(conversationId);
        status.GetProperty("eventCount").GetInt32().ShouldBeGreaterThan(0);

        // Command completion alone proves the write boundary, not the projection boundary: the projected
        // read models must land in the REAL Redis state store through the cross-app eventstore -> conversation
        // dispatch and become servable by the production query seam. The gateway's /api/v1/queries handler
        // route cannot carry this proof today — the AppHost defines no DomainServiceOptions registration for
        // the conversations domain, so handler-query routing to the module is structurally unresolvable — so
        // the assertion targets the module's own production /query endpoint, the same seam DAPR service
        // invocation reaches.
        using HttpClient conversations = application.CreateHttpClient(
            ConversationsAppHostTopology.ConversationsResourceName,
            "http");

        // The read side fails closed without a Tenants projection, and the AppHost composes no Tenants
        // module. Feed the projection through the module's own production /tenants/events subscription
        // endpoint — byte-for-byte the delivery the DAPR sidecar performs — so tenant admission is decided
        // by the real event-fed projection, not by a substituted access service.
        await SeedTenantAccessProjectionAsync(conversations, tenantId, timeout.Token);

        JsonElement detailResult = await PollForProjectedReadModelAsync(
            conversations,
            tenantId,
            conversationId,
            timeout.Token);
        string expectedConversationId = JsonSerializer.SerializeToElement(new ConversationId(conversationId)).GetString()!;
        ReadIdentifier(detailResult.GetProperty("details").GetProperty("conversationId"))
            .ShouldBe(expectedConversationId);

        JsonElement listResult = await SubmitQueryAsync(
            conversations,
            tenantId,
            conversationId,
            "conversation-list",
            timeout.Token);
        listResult.GetProperty("freshnessState").GetString().ShouldBe("Current");
        JsonElement row = listResult.GetProperty("conversations")
            .EnumerateArray()
            .ShouldHaveSingleItem();
        ReadIdentifier(row.GetProperty("conversationId")).ShouldBe(expectedConversationId);
    }

    private static string? ReadIdentifier(JsonElement identifier)
        => identifier.ValueKind == JsonValueKind.String
            ? identifier.GetString()
            : identifier.GetProperty("value").GetString();

    /// <summary>
    /// The stable cutover identity this harness attests with.
    /// </summary>
    /// <remarks>
    /// The writer-protocol marker is store-global and written once for the lifetime of the store; the
    /// endpoint returns 200 for a re-activation at the same commit and 409 for a different one. Attesting
    /// with the gateway revision made the attested value move with the EventStore worktree, so the run after
    /// any gitlink advance hit its own previous marker and was refused — the local DAPR Redis is the shared
    /// `dapr init` instance with `keyPrefix: none`, so the marker outlives the run that wrote it. Measured,
    /// not inferred: the stored marker held `cutoverCommit e4618d91` from the pass-10 gitlink while the tree
    /// was at `e6459019`. A stable harness identity makes re-activation genuinely idempotent and keeps 409
    /// meaning what the endpoint documents — a *different* marker, i.e. a real operator cutover, which this
    /// proof must not run against.
    /// </remarks>
    private const string HarnessCutoverCommit = "hexalith-conversations-apphost-boundary-harness";

    private static async Task ActivateProjectionDeliveryAsync(
        HttpClient eventStore,
        string gatewayRevision,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage activation = new(
            HttpMethod.Post,
            "/api/v1/admin/projections/delivery-writer-protocol/activate");
        activation.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateAdminAccessToken());
        activation.Content = JsonContent.Create(new
        {
            CutoverCommit = gatewayRevision,
            BackupReference = "apphost-boundary-preflight",
            WritersQuiesced = true,
            RetryWorkersQuiesced = true,
            DowngradeProhibitedAcknowledged = true,
        });
        using HttpResponseMessage response = await eventStore.SendAsync(activation, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        // 200 is the only success: ProjectionDeliveryCutoverStatus.Activated already covers "activated, or
        // was already active for THIS exact commit". 409 is documented as a DIFFERENT marker being present,
        // so it means the activation was genuinely refused and the boundary proof would then run against a
        // protocol state it never established. Accepting it read a refusal as success (pass-10 review).
        // With the stable harness identity above, a 409 can no longer be self-inflicted by a gitlink
        // advance: it reports a marker this harness did not write, which is a real conflict.
        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            $"the delivery-writer-protocol activation must succeed for CutoverCommit {gatewayRevision}; a 409 "
            + $"reports a different marker already present, not an idempotent re-activation. Body: {body}");
    }

    private static string CreateAdminAccessToken()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        string payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = "apphost-boundary-operator",
            iss = "hexalith-dev",
            aud = "hexalith-eventstore",
            nbf = now - 30,
            iat = now,
            exp = now + 600,
            global_admin = true,
        }));
        string unsignedToken = $"{header}.{payload}";
        byte[] signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(SigningKey),
            Encoding.ASCII.GetBytes(unsignedToken));
        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    private static async Task SeedTenantAccessProjectionAsync(
        HttpClient conversations,
        string tenantId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string correlationId = Guid.NewGuid().ToString("N");
        // The consumer's envelope validation requires ULID message ids, not GUIDs.
        var tenantCreated = new
        {
            MessageId = UniqueIdHelper.GenerateSortableUniqueStringId(),
            AggregateId = tenantId,
            TenantId = tenantId,
            EventTypeName = "Hexalith.Tenants.Contracts.Events.TenantCreated",
            SequenceNumber = 1L,
            Timestamp = now,
            CorrelationId = correlationId,
            SerializationFormat = "json",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                TenantId = tenantId,
                Name = "AppHost boundary tenant",
                Description = (string?)null,
                CreatedAt = now,
            }),
        };
        var userAdded = new
        {
            MessageId = UniqueIdHelper.GenerateSortableUniqueStringId(),
            AggregateId = tenantId,
            TenantId = tenantId,
            EventTypeName = "Hexalith.Tenants.Contracts.Events.UserAddedToTenant",
            SequenceNumber = 2L,
            Timestamp = now,
            CorrelationId = correlationId,
            SerializationFormat = "json",
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                TenantId = tenantId,
                UserId = "apphost-boundary-actor",
                Role = 3, // TenantRole.TenantReader
            }),
        };

        foreach (object envelope in new[] { tenantCreated, userAdded })
        {
            using HttpResponseMessage delivery = await conversations.PostAsJsonAsync(
                "/tenants/events",
                envelope,
                cancellationToken);
            string deliveryBody = await delivery.Content.ReadAsStringAsync(cancellationToken);

            // OK is necessary but NOT sufficient: MapProcessingResult returns Results.Ok() for
            // SkippedUnknownEventType, SkippedNoHandlers, SkippedAggregateMismatch and FailedInvalidPayload,
            // so a renamed event type, a drifted payload shape, or Role = 3 ceasing to mean TenantReader all
            // pass this assertion. The effect is verified below so the failure names its own cause instead of
            // surfacing minutes later as an unattributed projection-boundary timeout (pass-10 review).
            delivery.StatusCode.ShouldBe(HttpStatusCode.OK, deliveryBody);
        }
    }

    private static async Task<JsonElement> PollForProjectedReadModelAsync(
        HttpClient conversations,
        string tenantId,
        string conversationId,
        CancellationToken cancellationToken)
    {
        string? lastResult = null;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                JsonElement result = await SubmitQueryAsync(
                    conversations,
                    tenantId,
                    conversationId,
                    "conversation-detail",
                    cancellationToken);
                lastResult = result.GetRawText();
                if (result.TryGetProperty("details", out JsonElement details)
                    && details.ValueKind == JsonValueKind.Object
                    && result.TryGetProperty("freshnessState", out JsonElement freshness)
                    && string.Equals(freshness.GetString(), "Current", StringComparison.Ordinal))
                {
                    return result;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The projected conversation read model never became queryable through the production query seam. Last result: {lastResult}");
        }
    }

    private static async Task<JsonElement> SubmitQueryAsync(
        HttpClient conversations,
        string tenantId,
        string conversationId,
        string queryType,
        CancellationToken cancellationToken)
    {
        var envelope = new
        {
            tenantId,
            domain = "conversations",
            aggregateId = conversationId,
            queryType,
            payload = Array.Empty<byte>(),
            correlationId = Guid.NewGuid().ToString("N"),
            userId = "apphost-boundary-actor",
        };
        using HttpResponseMessage response = await conversations.PostAsJsonAsync("/query", envelope, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        JsonElement result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("success").GetBoolean().ShouldBeTrue(body);
        byte[] payloadBytes = result.GetProperty("payloadBytes").GetBytesFromBase64();
        return JsonSerializer.Deserialize<JsonElement>(payloadBytes);
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

    private static async Task<(string ProjectPath, string SourceRevision)> BuildEventStoreGatewayWithProvenanceAsync(CancellationToken cancellationToken)
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

        return (projectPath, sourceRevision);
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
