// <copyright file="ConversationGatewayRouteCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.Server.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Activates only this disposable live fixture's create command and named projection through the SDK's real Redis catalog authority.</summary>
/// <remarks>These declarations are isolated test inputs. They neither enroll the production module nor claim owner acceptance or deployed qualification.</remarks>
internal sealed class ConversationGatewayRouteCatalog : IDisposable
{
    private const string CatalogStore = "conversations-gateway-route-catalog";
    private const string PolicyVersion = "conversations-fixture-adr0001-v1";
    private static readonly RouteContractDeclaration CommandRoute = new(
        "conversations-fixture-create", "command", ConversationProjectionHandler.ConversationDomain,
        nameof(CreateConversation), null, ConversationGatewayLiveFixture.ServiceVersion, 1, "workload",
        EventStoreOperationIdentifiers.DomainServiceProcess, null, [],
        new(typeof(CreateConversation).FullName!, "clr-short-name-v1", [nameof(CreateConversation)], "domain-service", null, []));
    private static readonly RouteContractDeclaration ProjectionRoute = new(
        "conversations-fixture-read-model", "projection", ConversationProjectionHandler.ConversationDomain,
        null, ConversationAsyncProjectionHandler.ConversationReadModelProjectionType, ConversationGatewayLiveFixture.ServiceVersion,
        null, "workload", EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new(typeof(ConversationAsyncProjectionHandler).FullName!, "exact",
            [ConversationAsyncProjectionHandler.ConversationReadModelProjectionType], "domain-service", null,
            [ConversationAsyncProjectionHandler.ConversationReadModelProjectionType]));
    private static readonly RouteContractDeclaration[] Declarations = [CommandRoute, ProjectionRoute];
    private static readonly RouteOperationContract[] Operations =
    [
        RouteOperationSchema.Create(CommandRoute, typeof(CreateConversation), typeof(SubmitCommandResponse),
            "internal", "UI-only", JsonSerializerOptions.Web),
        RouteOperationSchema.Create(ProjectionRoute, typeof(ProjectionDispatchRequest), typeof(ProjectionDispatchResponse),
            "internal", "agent-eligible", JsonSerializerOptions.Web),
    ];
    private readonly string _appId;
    private readonly string _componentPath;
    private readonly string _directory;
    private readonly string _sidecar;
    private readonly HttpClient _metadataClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private WebApplication? _app;
    private string? _rootDigest;
    private long _epoch;

    /// <summary>Retains the exact component and deny-default ACL that the fixture sidecar will load.</summary>
    public ConversationGatewayRouteCatalog(string appId, string componentsDirectory, string sidecar)
    {
        _appId = appId;
        _sidecar = sidecar;
        _directory = Path.Combine(Path.GetTempPath(), "conversations-catalog-" + appId);
        Directory.CreateDirectory(_directory);
        _componentPath = Path.Combine(componentsDirectory, CatalogStore + ".yaml");
        File.WriteAllBytes(_componentPath, JsonSerializer.SerializeToUtf8Bytes(new
        {
            apiVersion = "dapr.io/v1alpha1", kind = "Component",
            metadata = new { name = CatalogStore, labels = new Dictionary<string, string>
                { [RouteCatalogAuthorityComponentPolicy.RoleLabel] = RouteCatalogAuthorityComponentPolicy.IsolatedRole } },
            spec = new { type = "state.redis", version = "v1", metadata = new[]
                { new { name = "redisHost", value = "localhost:6379" }, new { name = "redisPassword", value = "" },
                  new { name = "keyPrefix", value = "name" } } }, scopes = new[] { appId },
        }));
        File.WriteAllBytes(ConfigurationPath, JsonSerializer.SerializeToUtf8Bytes(new
        {
            apiVersion = "dapr.io/v1alpha1", kind = "Configuration", metadata = new { name = "conversations-gateway-fixture" },
            spec = new { accessControl = new { defaultAction = "deny", trustDomain = "public", policies = new[]
            {
                new { appId, @namespace = "default", trustDomain = "public", defaultAction = "deny",
                    operations = EventStoreDomainServiceRoutes.Operational.Select(route => new
                        { name = route.Route.TrimStart('/'), httpVerb = new[] { "POST" }, action = "allow" }).ToArray() },
            } } },
        }));
    }

    /// <summary>Gets the retained ACL passed to the actual sidecar's --config argument.</summary>
    public string ConfigurationPath => Path.Combine(_directory, "configuration.json");

    /// <summary>Registers the SDK observer, real authority backend, and the accepted bounded create-command fingerprint policy.</summary>
    public void Configure(WebApplicationBuilder builder)
    {
        builder.Configuration["DAPR_HTTP_ENDPOINT"] = _sidecar;
        builder.Configuration["EventStore:RouteCatalog:StateStore"] = CatalogStore;
        builder.Configuration["EventStore:RouteCatalog:StateKey"] = "fixture-" + _appId;
        const string prefix = "EventStore:RouteCatalog:DevelopmentBootstrap:";
        builder.Configuration[prefix + "HostId"] = _appId;
        builder.Configuration[prefix + "RequiredHostIds:0"] = _appId;
        builder.Configuration[prefix + "ComponentPath"] = _componentPath;
        builder.Configuration[prefix + "AccessControlPath"] = ConfigurationPath;
        builder.Configuration[prefix + "AuthorityRole"] = RouteCatalogAuthorityComponentPolicy.IsolatedRole;
        builder.Services.Configure<RouteCatalogHostOptions>(options =>
        {
            options.Profile = "Development";
            options.DevelopmentFixture = true;
        });
        builder.Services.AddSingleton<IIdempotencyIntentAdapter>(CreateCommandAdapter());
        const string admission = "EventStore:IdempotencyAdmission:";
        builder.Configuration[admission + "Enabled"] = "true";
        builder.Configuration[admission + "ActiveDigestKeyVersion"] = "fixture-v1";
        builder.Configuration[admission + "DigestKeySource"] = "Configuration";
        builder.Configuration[admission + "DigestKeySecretStoreName"] = "fixture-local-keys";
        builder.Configuration[admission + "DigestKeySecretName"] = "conversations-fixture-intent";
        builder.Configuration[admission + "DigestKeySecretGeneration"] = "fixture-v1";
        builder.Configuration[admission + "DigestKeys:fixture-v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        builder.Services.AddEventStoreRouteCatalogObservation(ObserveAsync);
    }

    /// <summary>Adapts the accepted ADR 0001 scoped semantic fingerprint without transport timestamps or event identities.</summary>
    internal static IDeclaredIdempotencyIntentAdapter CreateCommandAdapter()
    {
        var policy = new CommandIntentDeclaration(CommandRoute, "conversations-fixture-create", "create-conversation",
            PolicyVersion, new("conversations-fixture-create-intent", 1,
                [new("target", "string"), new("semantic-fingerprint", "string"), new("public-tenant", "string"),
                 new("public-command-type", "string"), new("scope-kind", "string"), new("scope-value", "string"),
                 new("schema-version", "int64"), new("public-idempotency-key", "string")]),
            IdempotencyReplayRetentionTier.Mutation, false);
        return new DeclaredIdempotencyIntentAdapter<CreateConversation>(policy, command =>
        {
            var fingerprint = ConversationCommandFingerprint.Create(command.PublicCommand,
                command.ConversationId ?? throw new InvalidOperationException("The fixture create command requires its allocated conversation identity."));
            return new Dictionary<string, object?>
            {
                ["semantic-fingerprint"] = fingerprint.PayloadFingerprint.Value,
                ["public-tenant"] = fingerprint.Scope.TenantId.Value,
                ["public-command-type"] = fingerprint.Scope.CommandType.Value,
                ["scope-kind"] = fingerprint.Scope.ScopeKind,
                ["scope-value"] = fingerprint.Scope.ScopeValue,
                ["schema-version"] = (long)fingerprint.Scope.SchemaVersion.Value,
                ["public-idempotency-key"] = fingerprint.Scope.IdempotencyKey,
            };
        });
    }

    /// <summary>Binds the observer to the actual mapped application after its receivers have been installed.</summary>
    public void Attach(WebApplication app) => _app = app;

    /// <summary>Prepares, attests and commits a real generation after the sidecar and isolated Redis state are healthy.</summary>
    public async Task ActivateAsync(CancellationToken token = default)
    {
        var app = _app ?? throw new InvalidOperationException("The live catalog has no mapped host.");
        var actual = await ObserveAsync(app.Services, token).ConfigureAwait(false);
        var identity = ObservedRouteCatalogHost.Identity(actual);
        app.Services.GetRequiredService<IOptions<RouteCatalogHostOptions>>().Value.Identity = identity;
        var catalog = VersionedRouteCatalogBuilder.Build(new(1, "Development", Declarations, Operations,
            actual.Targets.Select(target => new RouteCompositionBinding(target.RouteId, target.AppId, target.Method,
                target.ContractVersion, target.IsRuntimeOverride, target.ResolverTenantPattern)).ToArray(), actual.AdmissionPolicies!));
        var candidate = new RouteCatalogActivation("fixture-" + _appId, RouteCatalogCodec.Encode(catalog),
            RouteCatalogCodec.ComputeRootDigest(catalog), null, [identity], "Development-fixture", "",
            actual.AuxiliaryOperations, actual.ProjectionCapabilities);
        candidate = candidate with { TrustProof = RouteCatalogActivationBinding.ComputeDigest(candidate) };
        var authority = app.Services.GetRequiredService<IRouteCatalogAuthority>();
        await authority.PrepareAsync(candidate, token).ConfigureAwait(false);
        await authority.ReadyAsync(candidate.AttemptId, token).ConfigureAwait(false);
        await authority.CommitAsync(candidate.AttemptId, token).ConfigureAwait(false);
        var readback = await authority.ReadbackAsync(token).ConfigureAwait(false);
        if (!readback.Serving || readback.RootDigest != candidate.RootDigest || readback.Generation != 1)
            throw new InvalidOperationException("The live Redis authority did not retain the exact prepared generation.");
        _rootDigest = readback.RootDigest;
        _epoch = readback.Epoch;
    }

    /// <summary>Binds direct authenticated trust-boundary probes to the exact committed live generation.</summary>
    public void Bind(HttpRequestMessage request)
    {
        if (_rootDigest is null) throw new InvalidOperationException("The live catalog is not committed.");
        request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, _rootDigest);
        request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, _epoch.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _metadataClient.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private async Task<RouteCatalogHostObservation> ObserveAsync(IServiceProvider provider, CancellationToken token)
    {
        var app = _app ?? throw new InvalidOperationException("The catalog observer requires the actual mapped application.");
        using var response = await _metadataClient.GetAsync(_sidecar + "/v1.0/metadata", token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var metadata = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false));
        var component = metadata.RootElement.GetProperty("components").EnumerateArray()
            .SingleOrDefault(item => item.GetProperty("name").GetString() == CatalogStore);
        if (metadata.RootElement.GetProperty("id").GetString() != _appId || component.ValueKind != JsonValueKind.Object
            || component.GetProperty("type").GetString() != "state.redis")
            throw new RouteCatalogUnavailableException("The running fixture sidecar did not load its dedicated Redis authority component.");
        using var scope = provider.CreateScope();
        var local = scope.ServiceProvider;
        var domain = local.GetRequiredService<DiscoveryResult>().Aggregates.Single(item => item.DomainName == CommandRoute.Domain);
        if (domain.Type != typeof(ConversationAggregate) || !domain.Type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(method => method.Name == "Handle" && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(CreateConversation)))
            throw new RouteCatalogUnavailableException("The fixture create declaration differs from the actual registered aggregate payload.");
        var named = local.GetServices<IAsyncDomainProjectionHandler>().Single(handler => handler.Domain == ProjectionRoute.Domain
            && handler.ProjectionType == ProjectionRoute.ProjectionType);
        if (named.GetType() != typeof(ConversationAsyncProjectionHandler))
            throw new RouteCatalogUnavailableException("The fixture named projection differs from its actual registered handler.");
        var identity = local.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value;
        if (identity.AppId != _appId || identity.ServiceVersion != ConversationGatewayLiveFixture.ServiceVersion)
            throw new RouteCatalogUnavailableException("The actual domain identity differs from the fixture's independent sidecar identity.");
        var registration = local.GetRequiredService<IOptions<DomainServiceOptions>>().Value.Registrations
            .Single(item => item.Key == "*|" + CommandRoute.Domain + "|" + identity.ServiceVersion).Value;
        if (registration.AppId != identity.AppId || registration.MethodName != "process")
            throw new RouteCatalogUnavailableException("The actual gateway resolver differs from the fixture command receiver.");
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        var targets = Declarations.Select(declaration =>
        {
            string method = declaration.Kind == "command" ? "process" : "project/v2";
            var endpoint = endpoints.OfType<RouteEndpoint>().Single(item => item.RoutePattern.RawText == "/" + method);
            var canonical = EventStoreDomainServiceRoutes.Operational.Single(route => route.Route == endpoint.RoutePattern.RawText);
            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                || endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.SequenceEqual(["POST"]) != true
                || !endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(policy => policy.Policy == canonical.Policy)
                || canonical.Operation != declaration.Operation)
                throw new RouteCatalogUnavailableException("The actual endpoint no longer matches the declared method and workload policy.");
            return new RouteCatalogEntry(declaration.RouteId, 1, RouteCatalogCodec.ComputeDeclarationDigest(declaration),
                identity.AppId, method, identity.ServiceVersion, declaration.CredentialKind, canonical.Operation, false,
                registration.TenantId, Operations.Single(operation => operation.RouteId == declaration.RouteId).Digest);
        }).ToArray();
        var admission = await RouteCatalogAdmissionObservation.CaptureAsync(local, identity.AppId, token).ConfigureAwait(false);
        var auxiliary = RouteCatalogDomainObservation.Auxiliary(local, endpoints).Where(operation =>
            operation.Domain == CommandRoute.Domain && EventStoreDomainServiceRoutes.Operational.Any(route =>
                route.Route.TrimStart('/') == operation.Method)).ToArray();
        var actual = await DevelopmentRouteCatalogInputs.CaptureAsync(app.Configuration, identity.AppId, Declarations, targets,
            admission, RouteCatalogDomainObservation.Capabilities(local), auxiliary, token).ConfigureAwait(false);
        return actual with
        {
            AuthorityCoordinates = provider.GetRequiredService<IRouteCatalogStateStore>().Coordinates,
            Operations = Operations,
            TopologyDocuments = [.. actual.TopologyDocuments,
                JsonSerializer.SerializeToUtf8Bytes(new
                    { appId = _appId, componentName = component.GetProperty("name").GetString(), componentType = component.GetProperty("type").GetString() }),
                JsonSerializer.SerializeToUtf8Bytes(new[] { domain.Type.Assembly, named.GetType().Assembly }.Distinct().ToDictionary(
                    assembly => assembly.FullName!, assembly => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))), StringComparer.Ordinal))],
        };
    }
}
