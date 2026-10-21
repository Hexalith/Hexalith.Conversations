using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionDelegatedTransport(ProjectionCurrentProof proof, string publicKey, DateTimeOffset now)
    : HttpMessageHandler, IHttpClientFactory, IWorkloadAssertionIssuer
{
    public bool CanBindResources => true;
    public int Calls { get; private set; }
    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<string?>("source-fixture-workload-assertion");
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        BoundOwnerQueryRequest body = (await request.Content!.ReadFromJsonAsync<BoundOwnerQueryRequest>(cancellationToken))!;
        BoundOwnerQueryProof.Verify(body, request.Headers.GetValues(BoundOwnerQueryProof.HeaderName).Single(),
            "conversations", publicKey, now).ShouldBeTrue();
        body.PrincipalId.ShouldBe("actor");
        body.Query.QueryType.ShouldBe(InspectProjectionCurrentQuery.QueryType);
        body.Query.AggregateId.ShouldBe(proof.Owner.Selection.AggregateId);
        return new(HttpStatusCode.OK) { Content = JsonContent.Create(new SubmitQueryResponse("correlation",
            JsonSerializer.SerializeToElement(proof, JsonSerializerOptions.Web))) };
    }
}
