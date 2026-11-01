using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

// The transport executes the actual enrolled SDK owner endpoint for every delegated read. Only the
// native gateway and authenticated HTTP boundary are fixture authorities, never seeded completion rows.
internal sealed class ResolutionRebuildDelegatedTransport(ResolutionEnrollmentFixture fixture,
    string catalog, string publicKey) : HttpMessageHandler, IHttpClientFactory, IWorkloadAssertionIssuer
{
    public bool CanBindResources => true;
    public int Calls { get; private set; }
    public int TenantCalls { get; private set; }
    public bool TenantAllowed { get; set; } = true;
    public Action<int>? OnProjection { get; set; }
    public string Catalog { get; set; } = catalog;
    public NativeSourceProof? SourceOverride { get; set; }
    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<string?>("source-fixture-workload-assertion");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = (await request.Content!.ReadFromJsonAsync<BoundOwnerQueryRequest>(cancellationToken))!;
        BoundOwnerQueryProof.Verify(body, request.Headers.GetValues(BoundOwnerQueryProof.HeaderName).Single(),
            "conversations", publicKey, fixture.Time.Now).ShouldBeTrue();
        body.PrincipalId.ShouldBe("actor");
        if (body.Query.QueryType == ResolveTenantReadAuthority.QueryType)
        {
            // Native Tenant authority is a fixture transport authority, never deployment/G-4 evidence.
            // The production SDK delegated client, native evidence adapter, authorization and metadata
            // handler below still execute their actual before/after authority checks.
            TenantCalls++;
            body.Query.Tenant.ShouldBe(fixture.Input.TenantId);
            body.Query.AggregateId.ShouldBe(fixture.Input.TenantId);
            var identity = new AggregateIdentity("system", "tenants", fixture.Input.TenantId);
            var source = NativeSourceProofCodec.CreateSeed(identity, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
            source = NativeSourceProofCodec.ExtendDigest(source, identity, 1, "fixture-tenant-read-1",
                Convert.ToHexStringLower(SHA256.HashData("fixture-native-tenant-read"u8)));
            var authority = new TenantReadAuthorityResult(fixture.Input.TenantId, body.PrincipalId,
                TenantAllowed, source, fixture.Time.Now);
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new SubmitQueryResponse("correlation",
                JsonSerializer.SerializeToElement(authority, JsonSerializerOptions.Web))) };
        }
        Calls++;
        OnProjection?.Invoke(Calls);
        body.Query.QueryType.ShouldBe(InspectProjectionCurrentQuery.QueryType);
        body.Query.Tenant.ShouldBe(fixture.Input.TenantId);
        body.Query.AggregateId.ShouldBe(fixture.Input.AggregateId);
        var expected = body.Query.Payload!.Value.Deserialize<InspectProjectionCurrentQuery>(JsonSerializerOptions.Web)!;
        using HttpResponseMessage ownerResponse = await fixture.SendAsync("/project/source-guard/owner-proof/v1",
            new ProjectionOwnerProofRequest(fixture.Input.TenantId, fixture.Input.Domain, fixture.Input.AggregateId,
                ResolutionEnrollmentFixture.Route, Catalog));
        if (!ownerResponse.IsSuccessStatusCode)
            return new(ownerResponse.StatusCode) { Content = new ByteArrayContent(await ownerResponse.Content.ReadAsByteArrayAsync(cancellationToken)) };
        var owner = (await ownerResponse.Content.ReadFromJsonAsync<ProjectionOwnerProofResult>(JsonSerializerOptions.Web, cancellationToken))!;
        if (owner.Proof is null || owner.Proof.Selection != expected.Selection)
            return new(HttpStatusCode.ServiceUnavailable);
        var current = new ProjectionCurrentProof(SourceOverride ?? fixture.Gateway.Proof, owner.Proof, Catalog);
        return new(HttpStatusCode.OK) { Content = JsonContent.Create(new SubmitQueryResponse("correlation",
            JsonSerializer.SerializeToElement(current, JsonSerializerOptions.Web))) };
    }
}
