using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionRebuildQuerySession : IDisposable
{
    private readonly Dapr.Client.DaprClient _invocation = new Dapr.Client.DaprClientBuilder().Build();
    private readonly RSA _key = RSA.Create(2048);
    private readonly ResolutionEnrollmentFixture _fixture;
    private readonly HttpContextAccessor _accessor;
    private readonly DelegatedOwnerQueryClient _client;
    public ResolutionRebuildDelegatedTransport Transport { get; }

    public ResolutionRebuildQuerySession(ResolutionEnrollmentFixture fixture, string catalog)
    {
        _fixture = fixture;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:WorkloadIssuer:Workload"] = "conversations",
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(_key.ExportPkcs8PrivateKey()),
        }).Build();
        Transport = new(fixture, catalog, Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo()));
        var query = ResolutionMetadataFixture.Query();
        var http = new DefaultHttpContext();
        http.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName] = "original-verified-assertion";
        var claims = QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)).ToList();
        claims.Add(new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"));
        claims.Add(new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery));
        http.User = new(new ClaimsIdentity(claims, "Workload"));
        _accessor = new() { HttpContext = http };
        _client = new(_invocation, Transport, Transport, _accessor, configuration, fixture.Time);
    }

    public async Task<ProjectionCurrentProof?> ReadAsync(ConversationResolutionMetadataReadModel metadata)
    {
        var evidence = new ConversationResolutionNativeEvidence(_client, _fixture.Store);
        using var services = new ServiceCollection().AddSingleton<IHttpContextAccessor>(_accessor)
            .AddSingleton<IDomainQueryHandler>(new ResolutionDelegatedEvidenceHandler(evidence, metadata)).BuildServiceProvider();
        var result = await DomainQueryDispatcher.ExecuteAsync(services, ResolutionMetadataFixture.Query(), TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        return result.GetPayload().ValueKind == JsonValueKind.Null ? null
            : result.GetPayload().Deserialize<ProjectionCurrentProof>(JsonSerializerOptions.Web);
    }

    public async Task<QueryResult> ReadMetadataAsync()
    {
        var evidence = new ConversationResolutionNativeEvidence(_client, _fixture.Store);
        var authorization = new ConversationResolutionMetadataAuthorization(new ResolutionMetadataAccess(),
            _accessor, timeProvider: _fixture.Time, native: evidence);
        var handler = new GetConversationResolutionMetadataDomainQueryHandler(authorization,
            new ConversationProjectionReadStore(_fixture.Store), evidence);
        using var services = new ServiceCollection().AddSingleton<IHttpContextAccessor>(_accessor)
            .AddSingleton<IDomainQueryHandler>(handler).BuildServiceProvider();
        return await DomainQueryDispatcher.ExecuteAsync(services, ResolutionMetadataFixture.Query(), TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        Transport.Dispose();
        _invocation.Dispose();
        _key.Dispose();
    }
}
