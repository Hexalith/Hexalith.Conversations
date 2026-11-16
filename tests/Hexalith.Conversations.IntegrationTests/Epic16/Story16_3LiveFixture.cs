// <copyright file="Story16_3LiveFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.Conversations.IntegrationTests.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Integration;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// Owns a disposable Redis instance and the catalog-enrolled, authenticated EventStore/Conversations live hosts.
/// Tenant events cross the protected production HTTP consumer; no tenant-access substitution is installed.
/// </summary>
public sealed class Story16_3LiveFixture : IAsyncLifetime
{
    private readonly Story16_3RefusalAuthority _workerAuthority = new();
    private readonly string _container = "story163-" + Guid.NewGuid().ToString("N");
    private readonly ConcurrentDictionary<string, byte> _completed = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<Story16_3WireObservation> _wire = new();
    private readonly ConcurrentQueue<string> _stages = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private ConversationGatewayLiveFixture? _gateway;
    private int _redisPort;
    private bool _containerAttempted;
    private readonly string? _priorHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
    private readonly string? _priorGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT");

    /// <summary>The real member principal used by the tenant query gate.</summary>
    internal const string Caller = "party-gateway-actor";

    /// <summary>Gets the authenticated production topology.</summary>
    internal ConversationGatewayLiveFixture Gateway => _gateway ?? throw new InvalidOperationException("Fixture not initialized.");

    /// <summary>Gets the configured store failure control.</summary>
    internal Story16_3FailureStore FailureStore { get; private set; } = null!;

    /// <summary>Gets actual refused SDK worker authority requests.</summary>
    internal int RefusedAuthorityRequests => _workerAuthority.Requests;

    /// <summary>Gets the measured wire requests.</summary>
    internal IReadOnlyList<Story16_3WireObservation> Wire => _wire.ToArray();

    /// <summary>Gets bounded content-free stage observations for a failed native request.</summary>
    internal string Stages => string.Join("; ", _stages.TakeLast(20));

    /// <summary>Gets the production SDK read-model store independently of the failure decorator.</summary>
    internal IReadModelStore Store => Gateway.Services.GetRequiredService<DaprReadModelStore>();

    /// <summary>Records a completed scenario; disposal rejects a partial selection.</summary>
    /// <remarks>
    /// xUnit never creates this collection fixture when no test is selected, so disposal cannot observe an empty
    /// selection; the native runner's separate empty-selection control rejects that case.
    /// </remarks>
    internal void Complete(string scenario)
    {
        _completed.TryAdd(scenario, 0).ShouldBeTrue();
        TestContext.Current.SendDiagnosticMessage($"Story 16.3 {scenario} completed; authenticated production projection requests={_wire.Count(item => item.Status == 200 && item.ChannelAuthenticated && item.WorkloadAuthenticated)}.");
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        _redisPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        try
        {
            await _workerAuthority.StartAsync();
            // Every state family, including actor and catalog state, lives in this disposable instance.
            _containerAttempted = true;
            await DockerAsync("run", "--detach", "--rm", "--name", _container, "--publish",
                $"127.0.0.1:{_redisPort}:6379", "redis:7.4-alpine");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!await DaprDiagnostics.IsRedisResponsiveAsync(_redisPort))
                await Task.Delay(100, timeout.Token);
            _gateway = new ConversationGatewayLiveFixture(_redisPort, ConfigureHost, ObserveWire,
                requireBoundaryCensus: false, includeProgress: true);
            await _gateway.InitializeAsync();
            Gateway.RequireAvailable();
            string redisImage = await DockerAsync("inspect", "--format", "{{.Image}}", _container);
            var metadata = await _http.GetFromJsonAsync<JsonElement>(Gateway.DaprHttpEndpoint + "/v1.0/metadata");
            TestContext.Current.SendDiagnosticMessage(JsonSerializer.Serialize(new
            {
                fixture = "Story16_3", appId = Gateway.AppId, catalogRoot = Gateway.CatalogRootDigest,
                stateStore = ConversationGatewayLiveFixture.StateStoreName,
                daprRuntime = metadata.GetProperty("runtimeVersion").GetString(), redisImage = redisImage.Trim(),
                dotnetRuntime = Environment.Version.ToString(), tenantAdmission = "enrolled-durable",
            }));
        }
        catch
        {
            await DisposeResourcesAsync();
            throw;
        }
    }

    private void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Configuration["Tenants:PubSubName"] = "conversations-gateway-test-events";
        builder.Configuration["Tenants:TopicName"] = "tenants.events";
        builder.Configuration["Dapr:DeclarativeSubscriptions"] = "true";
        builder.Configuration[ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey] = _workerAuthority.BaseAddress + "/";
        builder.Configuration["EventStore:Authentication:Authority"] = _workerAuthority.BaseAddress + "/";
        builder.Configuration["EventStore:Authentication:TokenEndpoint"] = _workerAuthority.BaseAddress + "/connect/token";
        builder.Configuration["EventStore:Authentication:ClientId"] = "story163-worker";
        builder.Configuration["EventStore:Authentication:ClientSecret"] = Guid.NewGuid().ToString("N");
        builder.Services.RemoveAll<IConversationTenantAccessService>();
        builder.Services.AddConversationTenantAccess(builder.Configuration);
        // Keep every other SDK persistence capability (batches, expiry and conditional erase) unchanged.
        builder.Services.RemoveAll<IReadModelStore>();
        builder.Services.AddSingleton<IReadModelStore>(provider =>
            FailureStore = new Story16_3FailureStore(provider.GetRequiredService<DaprReadModelStore>()));
    }

    private void ObserveWire(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            string path = context.Request.Path.Value ?? string.Empty;
            if (path is not ("/project/v2" or "/project/v2/reconcile")) { await next(context); return; }
            _stages.Enqueue(path + " received");
            context.Request.EnableBuffering();
            var request = await JsonSerializer.DeserializeAsync<ProjectionDispatchRequest>(context.Request.Body,
                JsonSerializerOptions.Web, context.RequestAborted);
            context.Request.Body.Position = 0;
            _stages.Enqueue(path + " body read");
            Stream output = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await next(context);
                string outcome = "empty";
                if (buffer.Length > 0)
                {
                    using var document = JsonDocument.Parse(buffer.ToArray());
                    var root = document.RootElement;
                    outcome = root.ValueKind == JsonValueKind.String ? root.GetString()!
                        : root.TryGetProperty("outcomes", out var outcomes)
                            ? string.Join(",", outcomes.EnumerateArray().Select(item =>
                                item.GetProperty("status").ToString() + ":" + (item.TryGetProperty("reasonCode", out var reason) ? reason.ToString() : string.Empty))) : "unclassified";
                }
                _stages.Enqueue(path + " completed HTTP " + context.Response.StatusCode + " " + outcome);
                if (request is not null) _wire.Enqueue(new(path, context.Response.StatusCode,
                    context.Request.Headers.ContainsKey(DaprAppChannelToken.HeaderName),
                    context.Request.Headers.ContainsKey(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName), request, outcome));
                buffer.Position = 0;
                await buffer.CopyToAsync(output, context.RequestAborted);
            }
            catch (Exception error) { _stages.Enqueue(path + " exception " + error.GetType().Name); throw; }
            finally { context.Response.Body = output; }
        });
    }

    /// <summary>Creates a tenant and its actual durable membership through the production consumer route.</summary>
    internal async Task AdmitTenantAsync(string tenant, CancellationToken token)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await DeliverAsync(new TenantCreated(tenant, "Story 16.3", null, now), tenant, 1, now, token);
        await DeliverAsync(new UserAddedToTenant(tenant, Caller, TenantRole.TenantContributor), tenant, 2, now, token);
    }

    private async Task DeliverAsync<T>(T payload, string tenant, long sequence, DateTimeOffset timestamp, CancellationToken token)
        where T : IEventPayload
    {
        var envelope = new EventStoreDomainEventEnvelope(UniqueIdHelper.GenerateSortableUniqueStringId(), tenant, "system",
            payload.GetType().FullName!, sequence, timestamp, UniqueIdHelper.GenerateSortableUniqueStringId(), "json",
            JsonSerializer.SerializeToUtf8Bytes(payload)) { Domain = "tenants", UserId = "tenants-producer" };
        using var request = Gateway.CreateAppChannelRequest("/tenants/events", HttpMethod.Post);
        request.Content = JsonContent.Create(envelope);
        using var response = await _http.SendAsync(request, token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(token));
    }

    /// <summary>Sends a signed operation request to the production receiver without bypassing its policies.</summary>
    internal async Task<HttpResponseMessage> SendAsync<T>(string path, string operation, T payload, CancellationToken token,
        bool authenticate = true)
    {
        using var request = Gateway.CreateAppChannelRequest(path, HttpMethod.Post);
        request.Content = JsonContent.Create(payload);
        if (authenticate)
        {
            string? assertion = await Gateway.Services.GetRequiredService<IWorkloadAssertionIssuer>()
                .IssueAsync(new(Gateway.AppId, operation), token);
            assertion.ShouldNotBeNullOrWhiteSpace();
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName, Gateway.AppId);
        }
        return await _http.SendAsync(request, token);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        string[] directories = [.. Gateway.ScratchDirectories];
        int[] ports = [new Uri(Gateway.DaprHttpEndpoint).Port, new Uri(Gateway.DaprGrpcEndpoint).Port,
            new Uri(Gateway.ApplicationHttpEndpoint).Port, new Uri(_workerAuthority.BaseAddress).Port, _redisPort];
        await DisposeResourcesAsync();
        foreach (string directory in directories) Directory.Exists(directory).ShouldBeFalse("fixture scratch must be removed");
        foreach (int port in ports) (await DaprDiagnostics.IsPortReachableAsync("127.0.0.1", port)).ShouldBeFalse("fixture listeners must be released");
        Environment.GetEnvironmentVariable("DAPR_HTTP_PORT").ShouldBe(_priorHttpPort);
        Environment.GetEnvironmentVariable("DAPR_GRPC_PORT").ShouldBe(_priorGrpcPort);
        TestContext.Current.SendDiagnosticMessage("Story 16.3 cleanup: private Redis removed, listeners released, component/catalog files removed, Dapr environment restored.");
        _completed.Keys.Order(StringComparer.Ordinal).SequenceEqual(new[] { "Scenario01", "Scenario02", "Scenario03", "Scenario04", "Scenario05" }).ShouldBeTrue(
            "the live proof must execute all five native scenarios; a filtered selection cannot pass");
    }

    private async Task DisposeResourcesAsync()
    {
        try
        {
            if (_gateway is not null) await _gateway.DisposeAsync();
        }
        finally
        {
            try
            {
                _http.Dispose();
                await _workerAuthority.DisposeAsync();
            }
            finally
            {
                if (_containerAttempted)
                {
                    await RunDockerAsync(["rm", "--force", _container], allowMissingContainer: true);
                    _containerAttempted = false;
                }
            }
        }
    }

    private static Task<string> DockerAsync(params string[] arguments)
        => RunDockerAsync(arguments, allowMissingContainer: false);

    private static async Task<string> RunDockerAsync(IReadOnlyList<string> arguments, bool allowMissingContainer)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Docker could not start.");
        // Readers must drain after timeout, instead of inheriting its cancelled token.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(timeout.Token); }
        catch
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            finally
            {
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(stdout, stderr);
            }
            throw;
        }
        string output = await stdout;
        string error = await stderr;
        if (allowMissingContainer && process.ExitCode == 1 && error.Contains("No such container", StringComparison.OrdinalIgnoreCase))
            return output;
        process.ExitCode.ShouldBe(0, output + error);
        return output;
    }

    /// <summary>Derives the production detail key's public wire representation.</summary>
    internal static string DetailKey(string tenant, string conversation) => $"projection:conversations:{Encode(tenant)}:{Encode(conversation)}";

    /// <summary>Derives the production tenant index key's public wire representation.</summary>
    internal static string IndexKey(string tenant) => $"projection:conversations-index:{Encode(tenant)}";

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
