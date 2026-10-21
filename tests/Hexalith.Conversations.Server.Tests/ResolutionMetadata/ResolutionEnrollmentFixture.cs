using System.Text.Json;

using Dapr.Client;

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

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
    public InMemoryDaprStateClient Backend { get; } = new();
    public DaprReadModelStore Store { get; }
    public ResolutionTestTimeProvider Time { get; } = new(ResolutionMetadataFixture.Started.AddSeconds(10));
    public ProjectionRequest Input { get; }
    public ResolutionProjectionGateway Gateway { get; }
    public bool CanHaveBody => true;

    public ResolutionEnrollmentFixture(bool enroll = true, Action<IServiceCollection>? configure = null)
    {
        ProjectionRequest original = ResolutionMetadataFixture.Request();
        var gateway = Gateway = new ResolutionProjectionGateway(original);
        Input = JsonSerializer.Deserialize<ProjectionRequest>(JsonSerializer.Serialize(original with
            { NativeSourceProof = gateway.Proof }, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!;
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<DaprClient>(Backend);
        builder.Services.AddSingleton<TimeProvider>(Time);
        builder.Services.AddSingleton<IEventStoreGatewayClient>(gateway);
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

    public async Task<T> PostAsync<T>(string path, object request, int expectedStatus = StatusCodes.Status200OK)
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)App).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(item => item.RoutePattern.RawText == path);
        using IServiceScope scope = App.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(request, request.GetType(), JsonSerializerOptions.Web);
        context.Features.Set<IHttpRequestBodyDetectionFeature>(this);
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
        context.Response.Body = new MemoryStream();
        context.RequestAborted = TestContext.Current.CancellationToken;
        await endpoint.RequestDelegate!(context);
        context.Response.StatusCode.ShouldBe(expectedStatus);
        context.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<T>(context.Response.Body, JsonSerializerOptions.Web,
            TestContext.Current.CancellationToken))!;
    }

    public async Task<ConversationResolutionMetadataReadModel> MetadataAsync()
        => (await Store.GetAsync<ConversationResolutionMetadataReadModel>("statestore",
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant,
                ResolutionMetadataFixture.Conversation), TestContext.Current.CancellationToken)).Value!;

    public ValueTask DisposeAsync() => App.DisposeAsync();
}
