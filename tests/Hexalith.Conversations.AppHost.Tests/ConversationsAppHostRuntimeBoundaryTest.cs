// <copyright file="ConversationsAppHostRuntimeBoundaryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Conversations.AppHost;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Aspire;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
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
    /// <summary>
    /// Gets a value indicating whether the real-infrastructure runtime lane was explicitly enabled.
    /// </summary>
    public static bool RuntimeBoundaryTestsEnabled
        => string.Equals(
            Environment.GetEnvironmentVariable("HEXALITH_RUN_APPHOST_BOUNDARY_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase)
        || (string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.Ordinal)
            && string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Starts the real AppHost and proves an un-enrolled catalog fails closed before command admission.
    /// </summary>
    /// <remarks>
    /// Keycloak is disabled so the test isolates the EventStore/Conversations hosting boundary under review.
    /// </remarks>
    [Fact(
        Skip = "Set HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true to run the real AppHost boundary lane.",
        SkipUnless = nameof(RuntimeBoundaryTestsEnabled))]
    public async Task RetainedAppHostShouldRefuseCommandsWithoutCatalogAuthority()
    {
        // Keep owner-host compilation separate from the actual runtime boundary budget.
        using CancellationTokenSource prebuildTimeout = new(TimeSpan.FromMinutes(6));
        string gatewayProjectPath;
        string referenceMode = ReadEnrollmentMetadata("EnrolledReferenceMode");

        try
        {
            gatewayProjectPath = await BuildEventStoreGatewayWithProvenanceAsync(prebuildTimeout.Token);
        }
        catch (OperationCanceledException) when (prebuildTimeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The EventStore gateway provenance prebuild exceeded its own 6-minute budget. This is a build "
                + "machine problem, not a production-boundary failure: the AppHost was never started.");
        }

        // The real production hosts and sidecars receive their full runtime readiness budget.
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(8));
        List<string> arguments = [$"--{HexalithEventStoreSecurityOptions.DefaultEnableKeycloakConfigurationKey}=false"];
        if (referenceMode == "package")
        {
            arguments.Add($"--{ConversationsAppHostTopology.TestingUseBuildsGatewayConfigurationKey}=true");
        }

        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Conversations_AppHost>(
                [.. arguments],
                timeout.Token);

        string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        IResourceBuilder<ParameterResource> signingKeyParameter = builder.AddParameter(
            "eventstore-boundary-signing-key",
            () => signingKey,
            secret: true);

        // The provenance stamp proves nothing unless the binary Aspire launches IS a stamped one: bind the
        // model's resolved project path to the prebuilt checkout, and require SuppressBuild so Aspire cannot
        // rebuild the gateway (which would overwrite the stamp) between the assertion and the launch.
        ProjectResource eventStoreResource = builder.Resources.OfType<ProjectResource>().Single(resource =>
            string.Equals(resource.Name, ConversationsAppHostTopology.EventStoreResourceName, StringComparison.Ordinal));
        _ = builder.CreateResourceBuilder(eventStoreResource)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
            .WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
            .WithEnvironment("Authentication__JwtBearer__Authority", string.Empty)
            .WithEnvironment("Authentication__JwtBearer__Issuer", "hexalith-dev")
            .WithEnvironment(
                "Authentication__JwtBearer__Audience",
                HexalithEventStoreSecurityOptions.DefaultAudience)
            .WithEnvironment(
                "Authentication__JwtBearer__ValidAudiences__0",
                HexalithEventStoreSecurityOptions.DefaultAudience)
            .WithEnvironment("Authentication__JwtBearer__AllowedAlgorithms__0", "HS256")
            .WithEnvironment("Authentication__JwtBearer__SigningKey", signingKeyParameter)
            .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
            .WithEnvironment("Authentication__JwtBearer__AllowInsecureSymmetricKey", "true")
            .WithEnvironment("Authentication__WorkloadIssuer__Workload", ConversationsAppHostTopology.EventStoreResourceName);
        ProjectResource conversationsResource = builder.Resources.OfType<ProjectResource>().Single(resource =>
            string.Equals(resource.Name, ConversationsAppHostTopology.ConversationsResourceName, StringComparison.Ordinal));
        _ = builder.CreateResourceBuilder(conversationsResource)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
            .WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
            .WithEnvironment("Authentication__JwtBearer__Authority", string.Empty)
            .WithEnvironment("Authentication__JwtBearer__Issuer", "hexalith-dev")
            .WithEnvironment("Authentication__JwtBearer__Audience", ConversationsAppHostTopology.ConversationsDaprAppId)
            .WithEnvironment("Authentication__JwtBearer__SigningKey", signingKeyParameter)
            .WithEnvironment("Authentication__JwtBearer__AllowInsecureSymmetricKey", "true")
            .WithEnvironment("Authentication__Workload__Audience", ConversationsAppHostTopology.ConversationsDaprAppId)
            .WithEnvironment("Authentication__Workload__AllowedCallers__0", ConversationsAppHostTopology.EventStoreResourceName);
        ConfigureRuntimeBoundarySidecars(builder);

        IProjectMetadata gatewayMetadata = eventStoreResource.Annotations.OfType<IProjectMetadata>().Single();
        Path.GetFullPath(gatewayMetadata.ProjectPath).ShouldBe(
            Path.GetFullPath(gatewayProjectPath),
            "Aspire must launch the same owner gateway project the provenance prebuild stamped");
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
        await WaitForEndpointReadyAsync(eventStore, "/alive", timeout.Token);
        using (HttpResponseMessage unauthenticated = await eventStore.GetAsync(
                   "/api/v1/commands/status/auth-boundary-probe",
                   timeout.Token))
        {
            unauthenticated.StatusCode.ShouldBe(
                HttpStatusCode.Unauthorized,
                "the runtime harness must preserve EventStore's fail-closed authentication boundary");
        }
        await AssertConversationsWorkloadDeniedAsync(builder, application, conversationsResource, timeout.Token);

        string tenantId = $"apphost-{Guid.NewGuid():N}";
        string conversationId = $"conversation-{Guid.NewGuid():N}";
        string messageId = Guid.NewGuid().ToString("N");
        string correlationId = Guid.NewGuid().ToString("N");
        eventStore.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken(tenantId, signingKey));

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

        using HttpResponseMessage submission = await eventStore.PostAsJsonAsync("/api/v1/commands", request, timeout.Token);
        string submissionBody = await submission.Content.ReadAsStringAsync(timeout.Token);
        if (submission.StatusCode != HttpStatusCode.ServiceUnavailable)
        {
            string resourceLogs = await ReadFailureLogsAsync(
                application,
                [correlationId, messageId, "error", "exception", "fail"],
                timeout.Token);
            submission.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable, submissionBody + Environment.NewLine + resourceLogs);
        }

        // The retained production hosts are not enrolled in a live route catalog.
        // The current SDK rejects this request before admission or domain dispatch;
        // an arbitrary gateway failure must never satisfy this boundary assertion.
        submission.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        JsonElement problem = JsonSerializer.Deserialize<JsonElement>(submissionBody);
        problem.GetProperty("status").GetInt32().ShouldBe(503);
        problem.GetProperty("title").GetString().ShouldBe("Service Unavailable");
        problem.GetProperty("detail").GetString().ShouldBe("Catalog authority cannot safely admit this operation.");
        problem.GetProperty("type").GetString().ShouldBe("https://hexalith.io/problems/service-unavailable");

        using HttpResponseMessage status = await eventStore.GetAsync($"/api/v1/commands/status/{messageId}", timeout.Token);
        string statusBody = await status.Content.ReadAsStringAsync(timeout.Token);
        status.StatusCode.ShouldBe(HttpStatusCode.NotFound, statusBody);
        status.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        JsonElement missingStatus = JsonSerializer.Deserialize<JsonElement>(statusBody);
        missingStatus.GetProperty("status").GetInt32().ShouldBe(404);
        missingStatus.GetProperty("title").GetString().ShouldBe("Not Found");
        missingStatus.GetProperty("type").GetString().ShouldBe("https://hexalith.io/problems/command-status-not-found");
        missingStatus.GetProperty("detail").GetString().ShouldBe($"No command status found for message or correlation identifier '{messageId}'.");
        await AssertCommandStateAbsentAsync(application, eventStoreResource, tenantId, messageId, timeout.Token);
    }

    private static async Task AssertConversationsWorkloadDeniedAsync(
        IDistributedApplicationTestingBuilder builder,
        DistributedApplication application,
        ProjectResource resource,
        CancellationToken cancellationToken)
    {
        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext context = new(builder.ExecutionContext, resource, environment, cancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        object configuredToken = environment[DaprAppChannelToken.ConfigurationKey];
        string? token = configuredToken is IValueProvider provider
            ? await provider.GetValueAsync(cancellationToken)
            : configuredToken.ToString();
        token.ShouldNotBeNullOrWhiteSpace();
        using HttpClient conversations = application.CreateHttpClient(resource.Name, "http");
        using HttpRequestMessage request = new(HttpMethod.Post, "/process");
        request.Headers.Add(DaprAppChannelToken.HeaderName, token);
        request.Content = JsonContent.Create(new { });
        using HttpResponseMessage response = await conversations.SendAsync(request, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a valid sidecar channel alone must never grant workload authority");
    }

    private static async Task AssertCommandStateAbsentAsync(
        DistributedApplication application,
        ProjectResource resource,
        string tenantId,
        string messageId,
        CancellationToken cancellationToken)
    {
        IDaprSidecarResource sidecar = resource.Annotations.OfType<DaprSidecarAnnotation>().Single().Sidecar;
        using HttpClient dapr = application.CreateHttpClient(sidecar.Name + "-cli", "http");
        using HttpResponseMessage response = await dapr.GetAsync("/v1.0/metadata", cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument metadata = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        metadata.RootElement.GetProperty("id").GetString().ShouldBe(ConversationsAppHostTopology.EventStoreResourceName);
        metadata.RootElement.GetProperty("components").EnumerateArray()
            .Single(component => component.GetProperty("name").GetString() == ConversationsAppHostTopology.StateStoreComponentName)
            .GetProperty("type").GetString().ShouldBe("state.redis");
        foreach (string key in new[] { CommandStatusConstants.BuildKey(tenantId, messageId), CommandArchiveConstants.BuildKey(tenantId, messageId) })
        {
            using HttpResponseMessage state = await dapr.GetAsync(
                $"/v1.0/state/{ConversationsAppHostTopology.StateStoreComponentName}/{Uri.EscapeDataString(key)}?consistency=strong",
                cancellationToken);
            state.StatusCode.ShouldBe(HttpStatusCode.NoContent, "the healthy state store must contain neither command status nor archive for a pre-admission refusal");
        }
    }

    private static async Task WaitForEndpointReadyAsync(
        HttpClient client,
        string path,
        CancellationToken cancellationToken)
    {
        string? lastDiagnostic = null;
        using CancellationTokenSource readinessTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readinessTimeout.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            while (true)
            {
                try
                {
                    using HttpResponseMessage response = await client.GetAsync(path, readinessTimeout.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    string body = await response.Content.ReadAsStringAsync(readinessTimeout.Token);
                    lastDiagnostic = $"Status={(int)response.StatusCode} ({response.StatusCode}); Body={body}";
                }
                catch (HttpRequestException exception)
                {
                    lastDiagnostic = exception.Message;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), readinessTimeout.Token);
            }
        }
        catch (OperationCanceledException) when (readinessTimeout.IsCancellationRequested)
        {
            string budget = cancellationToken.IsCancellationRequested
                ? "the runtime-lane budget expired"
                : "the 90-second endpoint-readiness budget expired";
            throw new TimeoutException(
                $"The EventStore endpoint did not become ready before {budget}. Last result: {lastDiagnostic}");
        }
    }

    private static void ConfigureRuntimeBoundarySidecars(IDistributedApplicationTestingBuilder builder)
    {
        string configPath = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Hexalith.Conversations.AppHost.Tests",
            "DaprComponents",
            "runtime-boundary.yaml");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("The runtime-boundary Dapr configuration was not found.", configPath);
        }

        const string expectedConfig = """
            apiVersion: dapr.io/v1alpha1
            kind: Configuration
            metadata:
              name: conversations-runtime-boundary
            spec:
              features:
                - name: HotReload
                  enabled: false

            """;
        string normalizedConfig = File.ReadAllText(configPath).Replace("\r\n", "\n", StringComparison.Ordinal);
        normalizedConfig.ShouldBe(
            expectedConfig.Replace("\r\n", "\n", StringComparison.Ordinal),
            "the runtime-boundary profile must be the exact reviewed HotReload-disabled configuration");

        string[] projectNames =
        [
            ConversationsAppHostTopology.EventStoreResourceName,
            ConversationsAppHostTopology.ConversationsResourceName,
        ];
        foreach (string projectName in projectNames)
        {
            ProjectResource project = builder.Resources.OfType<ProjectResource>().Single(resource =>
                string.Equals(resource.Name, projectName, StringComparison.Ordinal));
            DaprSidecarAnnotation sidecar = project.Annotations.OfType<DaprSidecarAnnotation>().Single();
            DaprSidecarOptionsAnnotation options = sidecar.Sidecar.Annotations
                .OfType<DaprSidecarOptionsAnnotation>()
                .Single();
            if (!string.IsNullOrWhiteSpace(options.Options.Config))
            {
                throw new InvalidOperationException(
                    $"Dapr sidecar '{sidecar.Sidecar.Name}' already has configuration '{options.Options.Config}'. "
                    + "The runtime-boundary harness refuses to replace it silently.");
            }

            _ = sidecar.Sidecar.Annotations.Remove(options);
            sidecar.Sidecar.Annotations.Add(
                new DaprSidecarOptionsAnnotation(options.Options with { Config = configPath }));
            sidecar.Sidecar.Annotations
                .OfType<DaprSidecarOptionsAnnotation>()
                .Single()
                .Options
                .Config
                .ShouldBe(configPath);
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

            using CancellationTokenSource collection = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            collection.CancelAfter(TimeSpan.FromSeconds(15));
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

    private static string CreateAccessToken(string tenantId, string signingKey)
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
            Encoding.UTF8.GetBytes(signingKey),
            Encoding.ASCII.GetBytes(unsignedToken));
        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static async Task<string> BuildEventStoreGatewayWithProvenanceAsync(CancellationToken cancellationToken)
    {
        string referenceMode = ReadEnrollmentMetadata("EnrolledReferenceMode");
        (string ownerRoot, string projectPath, string assemblyName, string ownerIdentity) = SelectGatewayHost(referenceMode);
        ProcessResult revision = await RunProcessAsync(
            "git",
            ["rev-parse", "HEAD"],
            ownerRoot,
            cancellationToken);
        revision.ExitCode.ShouldBe(0, revision.StandardError);
        string headRevision = revision.StandardOutput.Trim();
        headRevision.ShouldNotBeNullOrWhiteSpace();
        string ownerRevision = await ComputeWorkspaceRevisionAsync(
            ownerRoot,
            headRevision,
            cancellationToken);
        string sdkVersion = ReadEnrollmentMetadata("EnrolledEventStoreVersion");
        string sourceRevision = $"{ownerIdentity}.{ownerRevision}.sdk.{sdkVersion}";

        // The producer selects the only configuration that this test and its
        // launched children may use; an ambient fallback must not build another mode.
        string? configuration = Environment.GetEnvironmentVariable("Configuration");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HEXALITH_SOURCE_MAPPING_HASH"))
            || configuration != (referenceMode == "source" ? "Debug" : "Release"))
        {
            throw new InvalidOperationException("Gateway preparation requires the complete producer-selected build environment.");
        }

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
            ownerRoot,
            cancellationToken);
        build.ExitCode.ShouldBe(
            0,
            $"The {configuration} EventStore gateway prebuild failed.{Environment.NewLine}"
            + $"{build.StandardOutput}{Environment.NewLine}{build.StandardError}");

        string gatewayAssembly = Path.Combine(
            Path.GetDirectoryName(projectPath)!,
            "bin",
            configuration,
            "net10.0",
            assemblyName + ".dll");
        File.Exists(gatewayAssembly).ShouldBeTrue(gatewayAssembly);
        string? productVersion = FileVersionInfo.GetVersionInfo(gatewayAssembly).ProductVersion;
        productVersion.ShouldNotBeNullOrWhiteSpace();
        productVersion.ShouldContain(
            sourceRevision,
            Case.Insensitive,
            $"the {configuration} gateway binary Aspire may launch must carry its actual {ownerIdentity} revision and selected SDK identity");
        if (referenceMode == "package")
        {
            using JsonDocument dependencies = JsonDocument.Parse(await File.ReadAllTextAsync(
                Path.ChangeExtension(gatewayAssembly, ".deps.json"), cancellationToken));
            dependencies.RootElement.GetProperty("libraries").GetProperty($"Hexalith.EventStore.Gateway/{sdkVersion}")
                .GetProperty("type").GetString().ShouldBe("package", "the Builds gateway host must consume the selected published SDK");
        }

        return projectPath;
    }

    private static (string OwnerRoot, string ProjectPath, string AssemblyName, string OwnerIdentity) SelectGatewayHost(string referenceMode)
    {
        if (referenceMode == "source")
        {
            string ownerRoot = ReadEnrollmentMetadata("EnrolledEventStoreRoot");
            return (ownerRoot, Path.Combine(ownerRoot, "src", "Hexalith.EventStore", "Hexalith.EventStore.csproj"), "Hexalith.EventStore", "Hexalith.EventStore");
        }

        if (referenceMode == "package")
        {
            string ownerRoot = ReadEnrollmentMetadata("EnrolledBuildsRoot");
            return (ownerRoot, Path.Combine(ownerRoot, "src", "hosts", "Hexalith.Builds.Module.EventStoreHost", "Hexalith.Builds.Module.EventStoreHost.csproj"), "Hexalith.Builds.Module.EventStoreHost", "Hexalith.Builds");
        }

        throw new InvalidOperationException($"An explicit producer-selected reference mode is required; observed '{referenceMode}'.");
    }

    private static string ReadEnrollmentMetadata(string key)
    {
        string? value = typeof(ConversationsAppHostRuntimeBoundaryTest).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key).Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
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
