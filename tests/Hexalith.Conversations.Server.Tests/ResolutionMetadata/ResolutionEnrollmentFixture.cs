using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Dapr.Client;

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

// Executes production endpoint delegates and registration over isolated SDK storage. Authentication
// middleware and installation/source-guard maintenance are deliberately outside this source fixture.
internal sealed class ResolutionEnrollmentFixture : IAsyncDisposable, IHttpRequestBodyDetectionFeature
{
    public const string Route = ConversationAsyncProjectionHandler.ConversationReadModelProjectionType;
    public WebApplication App { get; }
    public InMemoryDaprStateClient Backend { get; }
    public DaprReadModelStore Store { get; }
    public ResolutionTestTimeProvider Time { get; }
    public ProjectionRequest Input { get; }
    public ResolutionProjectionGateway Gateway { get; }
    public bool CanHaveBody => true;
    private readonly bool _enroll;
    private readonly Action<IServiceCollection>? _configure;
    private bool _disposed;

    public ResolutionEnrollmentFixture(bool enroll = true, Action<IServiceCollection>? configure = null,
        InMemoryDaprStateClient? backend = null, ResolutionTestTimeProvider? time = null, ProjectionRequest? originalInput = null)
    {
        _enroll = enroll; _configure = configure;
        Backend = backend ?? new();
        Time = time ?? new(ResolutionMetadataFixture.Started.AddSeconds(10));
        ProjectionRequest original = originalInput ?? ResolutionMetadataFixture.Request();
        var gateway = Gateway = new ResolutionProjectionGateway(original);
        Input = JsonSerializer.Deserialize<ProjectionRequest>(JsonSerializer.Serialize(original with
            { NativeSourceProof = gateway.Proof }, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!;
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<DaprClient>(Backend);
        builder.Services.AddSingleton<TimeProvider>(Time);
        builder.Services.AddSingleton<IEventStoreGatewayClient>(gateway.Client);
        builder.Services.AddSingleton<IOptions<ReadModelBatchOptions>>(Options.Create(new ReadModelBatchOptions { TimeProvider = Time }));
        builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(_ => { });
        builder.Services.Configure<DomainProjectionIdentityOptions>(options =>
        { options.AppId = "conversations-enrollment-fixture"; options.ServiceVersion = "v1"; });
        if (enroll)
        {
            builder.Services.AddConversationResolutionNativeProof();
            builder.Services.AddConversationResolutionNativeProof(); // repeated composition is idempotent
        }
        builder.Services.Configure<RouteCatalogHostOptions>(options => options.Identity = new(
            ResolutionRouteCatalog.AppId, ResolutionRouteCatalog.AppId,
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(typeof(ConversationAsyncProjectionHandler).FullName!))),
            Convert.ToHexStringLower(SHA256.HashData("isolated-projection-fixture"u8)),
            Convert.ToHexStringLower(SHA256.HashData("authenticated-service-endpoint-delegate"u8))));
        builder.Services.AddSingleton<ResolutionRouteCatalog>(services => new(services, Input.TenantId,
            () => services.GetRequiredService<EventStoreDomainServiceEndpointSource>().Endpoints!.DataSources.SelectMany(source => source.Endpoints)));
        builder.Services.AddSingleton<IRouteCatalogAuthority>(services => services.GetRequiredService<ResolutionRouteCatalog>());
        configure?.Invoke(builder.Services);
        App = builder.Build();
        App.MapEventStoreDomainProjectionEndpoints();
        App.MapEventStoreDomainProjectionMetadataEndpoint();
        Store = App.Services.GetRequiredService<DaprReadModelStore>();
    }

    public async Task<AdminOperationalIndexMetadata.Response> CatalogAsync(bool guarded = true)
        => await PostAsync<AdminOperationalIndexMetadata.Response>("/admin/operational-index-metadata",
            new AdminOperationalIndexMetadata.Request([ConversationProjectionHandler.ConversationDomain])
            { AppId = "conversations-enrollment-fixture", ServiceVersion = "v1", ProjectionSourceGuardVersion = guarded ? 1 : null });

    public ProjectionDispatchRequest Dispatch(string fingerprint, string id = "enrolled-original", ProjectionRequest? input = null)
        => new(input ?? Input, [Route], id, fingerprint);

    public Task<ProjectionOwnerProofResult> OwnerAsync(string fingerprint, string? tenant = null,
        string? aggregate = null, string? route = null)
        => PostAsync<ProjectionOwnerProofResult>("/project/source-guard/owner-proof/v1", new ProjectionOwnerProofRequest(
            tenant ?? Input.TenantId, Input.Domain, aggregate ?? Input.AggregateId, route ?? Route, fingerprint));

    public async Task<T> PostAsync<T>(string path, object request, int expectedStatus = StatusCodes.Status200OK,
        Action<HttpContext>? configureContext = null)
    {
        using HttpResponseMessage response = await SendAsync(path, request, configureContext);
        ((int)response.StatusCode).ShouldBe(expectedStatus);
        return (await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions.Web,
            TestContext.Current.CancellationToken))!;
    }

    /// <summary>Requires an empty SDK refusal without executing any owner mutation.</summary>
    /// <param name="fingerprint">The requested projection capability fingerprint.</param>
    /// <param name="tenant">The requested tenant, or the original input tenant.</param>
    /// <param name="route">The requested projection route, or the registered route.</param>
    public async Task AssertOwnerRefusedAsync(string fingerprint, string? tenant = null, string? route = null)
    {
        int writes = Backend.WrittenKeys.Count;
        using HttpResponseMessage response = await SendAsync("/project/source-guard/owner-proof/v1", new ProjectionOwnerProofRequest(
            tenant ?? Input.TenantId, Input.Domain, Input.AggregateId, route ?? Route, fingerprint));
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        Backend.WrittenKeys.Count.ShouldBe(writes);
    }

    /// <summary>Executes the actual mapped endpoint delegate with exact committed generation and service bindings.</summary>
    /// <param name="path">The actual receiver route.</param>
    /// <param name="request">The receiver payload.</param>
    /// <param name="configureContext">An optional negative-control mutation of the bound context.</param>
    /// <returns>The receiver's actual status and response bytes.</returns>
    public async Task<HttpResponseMessage> SendAsync(string path, object request, Action<HttpContext>? configureContext = null)
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)App).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(item => item.RoutePattern.RawText == path);
        using IServiceScope scope = App.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(request, request.GetType(), JsonSerializerOptions.Web);
        context.Features.Set<IHttpRequestBodyDetectionFeature>(this);
        CommittedRouteCatalogSnapshot catalog = App.Services.GetRequiredService<ResolutionRouteCatalog>().Snapshot;
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(RouteCatalogSurfacePolicy.ClaimType, "service"),
            new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
        ], EventStoreWorkloadAuthenticationDefaults.WorkloadScheme));
        context.Request.Path = path;
        context.Request.Headers[RouteCatalogInvocationHandler.RootHeader] = catalog.RootDigest;
        context.Request.Headers[RouteCatalogInvocationHandler.EpochHeader] = catalog.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture);
        configureContext?.Invoke(context);
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
        context.Response.Body = new MemoryStream();
        context.RequestAborted = TestContext.Current.CancellationToken;
        await endpoint.RequestDelegate!(context);
        return new HttpResponseMessage((HttpStatusCode)context.Response.StatusCode)
        {
            Content = new ByteArrayContent(((MemoryStream)context.Response.Body).ToArray()),
        };
    }

    public async Task<ConversationResolutionMetadataReadModel> MetadataAsync()
        => (await Store.GetAsync<ConversationResolutionMetadataReadModel>("statestore",
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant,
                ResolutionMetadataFixture.Conversation), TestContext.Current.CancellationToken)).Value!;

    public async Task<ResolutionEnrollmentFixture> RestartAsync(DateTimeOffset now)
    {
        await DisposeAsync();
        // Only durable storage and original public input survive. Build a new container, handlers,
        // read-model adapter, gateway and clock; no admitted request or scoped service is retained.
        return new(_enroll, _configure, Backend, new(now), Input);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await App.DisposeAsync();
        Gateway.Dispose();
    }
}
