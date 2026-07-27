// <copyright file="ConversationGatewayLiveFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.Conversations;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.Server.Projections;
using Hexalith.EventStore.Testing.Integration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Hosts the Conversations domain service and the EventStore gateway behind one live <c>daprd</c> sidecar so
/// projection delivery crosses the real production boundary instead of an in-process dispatcher call.
/// </summary>
/// <remarks>
/// <para>
/// The single-host, single-app-id shape is the platform's own live-sidecar topology: the gateway's
/// <c>NamedProjectionDispatchCoordinator</c> reaches the domain service through DAPR service invocation, so
/// <c>project/v2</c> genuinely leaves the process through the sidecar and comes back in through the mapped
/// domain-service endpoint. Nothing here substitutes a projection seam.
/// </para>
/// <para>
/// The configured <c>IReadModelStore</c> is the platform's DAPR-backed adapter over a Redis state store, not an
/// in-memory fake, so ADR 0003's "configured integration state-store adapter" is the thing under assertion.
/// </para>
/// </remarks>
public sealed class ConversationGatewayLiveFixture : IAsyncLifetime
{
    /// <summary>The DAPR state-store component the Conversations read models are keyed into.</summary>
    public const string StateStoreName = "statestore";

    /// <summary>The domain-service version used by the static registration and the route catalog.</summary>
    public const string ServiceVersion = "v1";

    private const int RedisPort = 6379;
    private const int HealthTimeoutSeconds = 60;
    private const int WarmUpTimeoutSeconds = 45;

    private static readonly System.Text.Json.JsonSerializerOptions MetadataSerializerOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    private readonly StringBuilder _daprStandardError = new();
    private readonly StringBuilder _daprStandardOutput = new();
    private int _appPort;
    private string? _componentsDirectory;
    private int _daprGrpcPort;
    private int _daprHttpPort;
    private int _daprInternalGrpcPort;
    private int _daprMetricsPort;
    private Process? _daprProcess;
    private int _daprProfilePort;
    private string? _previousDaprGrpcPort;
    private string? _previousDaprHttpPort;
    private WebApplication? _testHost;

    /// <summary>Gets the isolated DAPR application identity used by this fixture run.</summary>
    public string AppId { get; } = $"conversations-gateway-{Guid.NewGuid():N}";

    /// <summary>Gets the isolated aggregate actor type name registered by this fixture run.</summary>
    public string AggregateActorTypeName { get; } = $"ConversationsAggregateActor{Guid.NewGuid():N}";

    /// <summary>Gets the DAPR HTTP endpoint of the fixture sidecar.</summary>
    public string DaprHttpEndpoint => $"http://localhost:{_daprHttpPort}";

    /// <summary>Gets the DAPR gRPC endpoint of the fixture sidecar.</summary>
    public string DaprGrpcEndpoint => $"http://localhost:{_daprGrpcPort}";

    /// <summary>
    /// Gets a value indicating whether the live gateway boundary started and is available to assert against.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the reason the live gateway boundary is unavailable, when it did not start.</summary>
    public string? SkipReason { get; private set; }

    /// <summary>
    /// Gets the named projection types the Conversations domain service advertised to the gateway.
    /// </summary>
    public IReadOnlyList<string> DiscoveredNamedProjectionTypes { get; private set; } = [];

    /// <summary>Gets the running host services (gateway plus Conversations domain service).</summary>
    public IServiceProvider Services => _testHost?.Services
        ?? throw new InvalidOperationException("The Conversations gateway live host has not started.");

    /// <summary>Creates an actor proxy factory bound to this fixture's sidecar.</summary>
    /// <returns>An actor proxy factory targeting the fixture sidecar HTTP endpoint.</returns>
    public IActorProxyFactory CreateActorProxyFactory()
        => new ActorProxyFactory(new ActorProxyOptions
        {
            HttpEndpoint = DaprHttpEndpoint,
            RequestTimeout = TimeSpan.FromSeconds(30),
        });

    /// <summary>Creates an aggregate actor proxy for one aggregate identity.</summary>
    /// <param name="tenantId">The tenant binding.</param>
    /// <param name="domain">The aggregate domain.</param>
    /// <param name="aggregateId">The aggregate identity.</param>
    /// <returns>The aggregate actor proxy served by this fixture's host.</returns>
    public IAggregateActor CreateAggregateActor(string tenantId, string domain, string aggregateId)
        => CreateActorProxyFactory().CreateActorProxy<IAggregateActor>(
            new ActorId($"{tenantId}:{domain}:{aggregateId}"),
            AggregateActorTypeName);

    /// <summary>Skips the calling test when the live gateway boundary could not start.</summary>
    public void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            Assert.Skip(SkipReason ?? DaprTestPrerequisites.SkipReason);
        }
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        int[] ports = GetAvailablePorts(6);
        _appPort = ports[0];
        _daprHttpPort = ports[1];
        _daprGrpcPort = ports[2];
        _daprInternalGrpcPort = ports[3];
        _daprMetricsPort = ports[4];
        _daprProfilePort = ports[5];

        // The DAPR actor runtime discovers its sidecar through these variables, and the fixture allocates
        // random ports so parallel runs cannot collide.
        _previousDaprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
        _previousDaprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT");
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", _daprHttpPort.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", _daprGrpcPort.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (!DaprTestPrerequisites.IsAvailable)
        {
            SkipReason = DaprTestPrerequisites.SkipReason;
            RestoreDaprPortEnvironment();
            return;
        }

        try
        {
            _componentsDirectory = CreateComponentFiles();
            await StartTestHostAsync().ConfigureAwait(false);
            await VerifyAppListeningAsync().ConfigureAwait(false);
            StartDaprSidecar();
            await WaitForDaprHealthAsync().ConfigureAwait(false);

            // Placement dissemination and actor registration complete asynchronously after the sidecar
            // reports healthy; asserting before that makes the first real actor call fail open.
            await Task.Delay(2000).ConfigureAwait(false);
            await VerifyAppListeningAsync().ConfigureAwait(false);
            await WarmUpActorRuntimeAsync().ConfigureAwait(false);

            await ActivateProjectionDeliveryWriterProtocolAsync().ConfigureAwait(false);
            await DiscoverNamedProjectionRoutesAsync().ConfigureAwait(false);
            IsAvailable = true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or SocketException)
        {
            SkipReason = "The Conversations live gateway boundary could not start: "
                + DaprDiagnostics.ToSupportSafeDiagnostic(exception.Message);
            await DisposeResourcesAsync().ConfigureAwait(false);
            RestoreDaprPortEnvironment();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeResourcesAsync().ConfigureAwait(false);
        RestoreDaprPortEnvironment();
    }

    private static int[] GetAvailablePorts(int count)
    {
        TcpListener[] listeners = new TcpListener[count];
        int[] ports = new int[count];
        for (int index = 0; index < count; index++)
        {
            listeners[index] = new TcpListener(IPAddress.Loopback, 0);
            listeners[index].Start();
            ports[index] = ((IPEndPoint)listeners[index].LocalEndpoint).Port;
        }

        for (int index = 0; index < count; index++)
        {
            listeners[index].Stop();
        }

        return ports;
    }

    private static string ResolveDaprdPath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string candidate = Path.Combine(home, ".dapr", "bin", "daprd" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty));
        return File.Exists(candidate)
            ? candidate
            : OperatingSystem.IsWindows() ? "daprd.exe" : "daprd";
    }

    private static string TailString(string value, int maxCharacters)
        => string.IsNullOrEmpty(value) || value.Length <= maxCharacters ? value : "..." + value[^maxCharacters..];

    private string CreateComponentFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dapr-conversations-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);

        string stateStore = $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: {StateStoreName}
            spec:
              type: state.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "localhost:{RedisPort}"
                - name: redisPassword
                  value: ""
                - name: actorStateStore
                  value: "true"
            scopes:
              - {AppId}
            """;

        string pubSub = $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: pubsub
            spec:
              type: pubsub.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "localhost:{RedisPort}"
                - name: redisPassword
                  value: ""
                - name: enableDeadLetter
                  value: "true"
            scopes:
              - {AppId}
            """;

        File.WriteAllText(Path.Combine(directory, "statestore.yaml"), stateStore);
        File.WriteAllText(Path.Combine(directory, "pubsub.yaml"), pubSub);
        return directory;
    }

    private async Task StartTestHostAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
        });

        string httpPort = _daprHttpPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string grpcPort = _daprGrpcPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        builder.Configuration["DAPR_HTTP_PORT"] = httpPort;
        builder.Configuration["DAPR_GRPC_PORT"] = grpcPort;
        builder.Configuration["Dapr:HttpPort"] = httpPort;
        builder.Configuration["Dapr:GrpcPort"] = grpcPort;
        builder.Configuration["EventStore:Actors:AggregateActorTypeName"] = AggregateActorTypeName;
        builder.Configuration["EventStore:DomainService:AppId"] = AppId;
        builder.Configuration["EventStore:DomainService:ServiceVersion"] = ServiceVersion;

        // The retry worker is driven explicitly by the tests; a background tick would race the assertions.
        builder.Configuration["EventStore:ProjectionDispatch:RetryWorkerInterval"] = "00:10:00";

        // The sidecar, actor runtime, and gateway are all in-process here, so request logging would bury the
        // assertion output without adding diagnostic value.
        builder.Configuration["Logging:LogLevel:Default"] = "Warning";

        _ = builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(
            _appPort,
            listen => listen.Protocols = HttpProtocols.Http1));

        // Registered before the SDK so the canonical registration keeps a host-supplied client authoritative.
        _ = builder.Services.AddSingleton(new DaprClientBuilder()
            .UseHttpEndpoint(DaprHttpEndpoint)
            .UseGrpcEndpoint(DaprGrpcEndpoint)
            .Build());

        // The production Conversations host, verbatim: the canonical two-line domain-service registration over
        // the domain and server boundary assemblies.
        _ = builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        _ = builder.Services.AddConversationQueries(options => options.MaxOffset = 100_000);
        _ = builder.Services.AddDataProtection();

        // Tenant admission is the one deliberately substituted seam: it needs a live Tenants projection, is
        // orthogonal to projection delivery, and gates only the read side. Recorded in the v2 proof evidence.
        _ = builder.Services.AddSingleton<IConversationTenantAccessService>(new AllowConfiguredTenantAccessService());

        // The platform gateway that owns the production dispatch path under proof: the actor runtime, the
        // projection update orchestrator, and the named projection dispatch coordinator.
        _ = builder.Services.AddEventStoreServer(builder.Configuration);

        // The gateway-side command substrate the aggregate actor resolves. These are the platform's production
        // DAPR implementations, registered individually rather than through AddEventStore(): that entry point
        // also composes the gateway's REST/OpenAPI surface, which is outside the projection boundary under
        // proof and drags an unrelated Microsoft.OpenApi binding into this lane.
        _ = builder.Services.AddOptions<CommandStatusOptions>().BindConfiguration("EventStore:CommandStatus");
        _ = builder.Services.AddOptions<CommandCorrelationIndexOptions>().BindConfiguration("EventStore:CommandCorrelationIndex");
        _ = builder.Services.AddSingleton<ICommandStatusStore, DaprCommandStatusStore>();
        _ = builder.Services.AddSingleton<ICommandCorrelationIndex, DaprCommandCorrelationIndex>();
        _ = builder.Services.AddSingleton<ICommandArchiveStore, DaprCommandArchiveStore>();

        _ = builder.Services.Configure<DomainServiceOptions>(options =>
            options.Registrations[$"*|{ConversationProjectionHandler.ConversationDomain}|{ServiceVersion}"] =
                new DomainServiceRegistration(
                    AppId,
                    "process",
                    "*",
                    ConversationProjectionHandler.ConversationDomain,
                    ServiceVersion));

        _testHost = builder.Build();
        _ = _testHost.MapActorsHandlers();

        // Maps /process, /project, /project/v2, the rebuild routes, and the operational-index metadata the
        // gateway's catalog refresher reads. This is the production mapping, not a test-local shim.
        _ = _testHost.UseEventStoreDomainService();
        _ = _testHost.MapGet("/healthz", () => Microsoft.AspNetCore.Http.Results.Ok("healthy"));

        await _testHost.StartAsync().ConfigureAwait(false);

        IServer server = _testHost.Services.GetRequiredService<IServer>();
        ICollection<string>? addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null || addresses.Count == 0)
        {
            throw new InvalidOperationException($"Kestrel did not bind any address. Expected port {_appPort}.");
        }
    }

    private void StartDaprSidecar()
    {
        _daprProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ResolveDaprdPath(),
                Arguments = string.Join(
                    ' ',
                    "--app-id",
                    AppId,
                    "--app-port",
                    _appPort,
                    "--app-protocol",
                    "http",
                    "--app-channel-address",
                    "127.0.0.1",
                    "--dapr-http-port",
                    _daprHttpPort,
                    "--dapr-grpc-port",
                    _daprGrpcPort,
                    "--dapr-internal-grpc-port",
                    _daprInternalGrpcPort,
                    "--metrics-port",
                    _daprMetricsPort,
                    "--profile-port",
                    _daprProfilePort,
                    "--resources-path",
                    $"\"{_componentsDirectory}\"",
                    "--log-level",
                    "info",
                    "--placement-host-address",
                    $"localhost:{DaprLocalEndpoints.PlacementPort}",
                    "--scheduler-host-address",
                    $"localhost:{DaprLocalEndpoints.SchedulerPort}"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        _daprProcess.OutputDataReceived += (_, args) => Append(_daprStandardOutput, args.Data);
        _daprProcess.ErrorDataReceived += (_, args) => Append(_daprStandardError, args.Data);

        _ = _daprProcess.Start();
        _daprProcess.BeginOutputReadLine();
        _daprProcess.BeginErrorReadLine();

        if (_daprProcess.HasExited)
        {
            throw new InvalidOperationException(
                $"daprd exited immediately with code {_daprProcess.ExitCode}. stderr: {Capture(_daprStandardError)}");
        }
    }

    private static void Append(StringBuilder buffer, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (buffer)
        {
            _ = buffer.AppendLine(line);
        }
    }

    private static string Capture(StringBuilder buffer)
    {
        lock (buffer)
        {
            return buffer.ToString();
        }
    }

    private async Task WaitForDaprHealthAsync()
    {
        using HttpClient client = new();
        string healthUrl = $"{DaprHttpEndpoint}/v1.0/healthz/outbound";
        string? lastError = null;
        for (int attempt = 0; attempt < HealthTimeoutSeconds; attempt++)
        {
            if (_daprProcess?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"daprd exited with code {_daprProcess.ExitCode} during the health check. "
                    + $"stderr: {TailString(Capture(_daprStandardError), 2000)}");
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync(new Uri(healthUrl)).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastError = $"HTTP {(int)response.StatusCode}";
            }
            catch (HttpRequestException exception)
            {
                lastError = exception.Message;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"The DAPR sidecar did not become healthy within {HealthTimeoutSeconds} seconds. "
            + $"Last error: {lastError ?? "(none)"}. "
            + $"stderr: {TailString(Capture(_daprStandardError), 2000)}");
    }

    private async Task VerifyAppListeningAsync()
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
        string healthUrl = $"http://127.0.0.1:{_appPort}/healthz";
        string? lastError = null;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(new Uri(healthUrl)).ConfigureAwait(false);
                return;
            }
            catch (HttpRequestException exception)
            {
                lastError = exception.Message;
            }
            catch (TaskCanceledException)
            {
                lastError = "The request timed out.";
            }

            await Task.Delay(200).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"The live gateway host did not answer on http://127.0.0.1:{_appPort}. Last error: {lastError}.");
    }

    private async Task WarmUpActorRuntimeAsync()
    {
        IETagActor proxy = CreateActorProxyFactory().CreateActorProxy<IETagActor>(
            new ActorId($"warmup:{Guid.NewGuid():N}"),
            ETagActor.ETagActorTypeName);

        Stopwatch stopwatch = Stopwatch.StartNew();
        Exception? lastError = null;
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(WarmUpTimeoutSeconds))
        {
            try
            {
                string seeded = await proxy.RegenerateAsync().ConfigureAwait(false);
                string? readBack = await proxy.GetCurrentETagAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(readBack) && string.Equals(readBack, seeded, StringComparison.Ordinal))
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastError = exception;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"The DAPR actor runtime did not warm up within {WarmUpTimeoutSeconds} seconds. "
            + $"Last error: {lastError?.Message ?? "(the round trip returned an inconsistent ETag)"}.");
    }

    /// <summary>
    /// Performs the platform's store-global projection-delivery writer-protocol activation.
    /// </summary>
    /// <remarks>
    /// Named v2 delivery admits nothing until this marker exists: the idempotency coordinator answers
    /// <c>delivery_state_unavailable</c> while <c>WriterProtocolV2Active</c> is false. In a deployment an
    /// operator performs it once through the gateway's authorized admin endpoint. The activation contract
    /// (<c>IProjectionDeliveryCutover</c> and its request record) is internal to
    /// <c>Hexalith.EventStore.Server</c>, so this reaches the registered platform implementation reflectively
    /// rather than re-implementing the marker write or bypassing its precondition validation. The gateway is
    /// still the component that decides whether activation is legal.
    /// </remarks>
    private async Task ActivateProjectionDeliveryWriterProtocolAsync()
    {
        System.Reflection.Assembly serverAssembly = typeof(IProjectionUpdateOrchestrator).Assembly;
        Type cutoverContract = serverAssembly.GetType(
            "Hexalith.EventStore.Server.Projections.IProjectionDeliveryCutover",
            throwOnError: true)!;
        Type requestContract = serverAssembly.GetType(
            "Hexalith.EventStore.Server.Projections.ProjectionDeliveryCutoverRequest",
            throwOnError: true)!;

        object cutover = Services.GetRequiredService(cutoverContract);
        object request = Activator.CreateInstance(
            requestContract,
            $"conversations-gateway-live-{AppId}",
            "disposable-live-fixture-store",
            true,
            true,
            true)!;

        Task activation = (Task)cutoverContract
            .GetMethod("ActivateAsync")!
            .Invoke(cutover, [request, CancellationToken.None])!;
        await activation.ConfigureAwait(false);

        object? status = activation.GetType().GetProperty("Result")?.GetValue(activation);
        if (!string.Equals(status?.ToString(), "Activated", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The projection delivery writer protocol could not be activated for the live gateway lane: {status}.");
        }
    }

    /// <summary>
    /// Discovers the Conversations named projection routes the way the gateway does in production.
    /// </summary>
    /// <remarks>
    /// The domain service advertises its named routes on <c>/admin/operational-index-metadata</c>, and answering
    /// that call is also what registers the domain-side dispatch catalog, so the fingerprint both sides agree on
    /// is produced by the product rather than by this fixture. The gateway-side catalog is then filled from the
    /// advertised binding. Nothing about the route is hard-coded here: a Conversations handler that stopped
    /// advertising <c>conversation-read-model</c> would leave the catalog empty and fail the lane.
    /// </remarks>
    private async Task DiscoverNamedProjectionRoutesAsync()
    {
        // Same shape the gateway uses: a DAPR service-invocation request issued through an HttpClient, so the
        // call leaves the process through the sidecar exactly as the platform's own catalog refresher does.
        using HttpRequestMessage metadataRequest = Services
            .GetRequiredService<DaprClient>()
            .CreateInvokeMethodRequest(
                AppId,
                "admin/operational-index-metadata",
                new AdminOperationalIndexMetadata.Request([ConversationProjectionHandler.ConversationDomain])
                {
                    AppId = AppId,
                    ServiceVersion = ServiceVersion,
                });
        using HttpResponseMessage metadataResponse = await Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient()
            .SendAsync(metadataRequest)
            .ConfigureAwait(false);
        _ = metadataResponse.EnsureSuccessStatusCode();

        AdminOperationalIndexMetadata.Response response = (await metadataResponse.Content
            .ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(MetadataSerializerOptions)
            .ConfigureAwait(false))
            ?? throw new InvalidOperationException("The operational-index metadata response could not be read.");

        if (response.CatalogFingerprint is null
            || response.DispatchCapability is null
            || response.DispatchVersion is null
            || response.AppId is null
            || response.ServiceVersion is null)
        {
            throw new InvalidOperationException(
                "The Conversations domain service did not advertise a named projection dispatch binding.");
        }

        NamedProjectionRouteCatalogEntry[] entries =
        [
            .. response.Domains
                .Where(static domain => domain.NamedProjectionTypes is { Count: > 0 })
                .Select(domain => new NamedProjectionRouteCatalogEntry(
                    response.AppId,
                    response.ServiceVersion,
                    domain.Domain,
                    response.DispatchVersion.Value,
                    response.DispatchCapability,
                    response.CatalogFingerprint,
                    domain.NamedProjectionTypes!)),
        ];

        if (entries.Length == 0)
        {
            throw new InvalidOperationException(
                "The Conversations domain service advertised no named projection types.");
        }

        DiscoveredNamedProjectionTypes = [.. entries.SelectMany(static entry => entry.ProjectionTypes)];
        Services.GetRequiredService<INamedProjectionRouteCatalog>().Upsert(entries);
    }

    private async ValueTask DisposeResourcesAsync()
    {
        if (_daprProcess is not null && !_daprProcess.HasExited)
        {
            _daprProcess.Kill(entireProcessTree: true);
            await _daprProcess.WaitForExitAsync().ConfigureAwait(false);
        }

        _daprProcess?.Dispose();
        _daprProcess = null;

        if (_testHost is not null)
        {
            await _testHost.StopAsync().ConfigureAwait(false);
            await _testHost.DisposeAsync().ConfigureAwait(false);
            _testHost = null;
        }

        if (_componentsDirectory is not null && Directory.Exists(_componentsDirectory))
        {
            try
            {
                Directory.Delete(_componentsDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup of a temporary component directory.
            }
        }

        _componentsDirectory = null;
    }

    private void RestoreDaprPortEnvironment()
    {
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", _previousDaprHttpPort);
        Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", _previousDaprGrpcPort);
    }

    private sealed class AllowConfiguredTenantAccessService : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                trustedTenantId is not null
                && routeTenantId == trustedTenantId
                && !string.IsNullOrWhiteSpace(callerPrincipalId)
                    ? ConversationTenantAccessDecision.Allowed(requirement, trustedTenantId, callerPrincipalId)
                    : ConversationTenantAccessDecision.Denied(
                        requirement,
                        trustedTenantId,
                        callerPrincipalId,
                        ConversationTenantAccessDenialReason.TenantMismatch));
    }
}
