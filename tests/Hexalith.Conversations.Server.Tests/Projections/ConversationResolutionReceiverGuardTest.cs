using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;

using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>Exercises the SDK receiver guard against persisted owner state.</summary>
public sealed class ConversationResolutionReceiverGuardTest
{
    /// <summary>Refuses uncommitted authority before any persisted owner state changes.</summary>
    /// <param name="fault">The single catalog or caller binding to invalidate.</param>
    [Theory]
    [InlineData("missing-root")]
    [InlineData("wrong-root")]
    [InlineData("missing-epoch")]
    [InlineData("wrong-epoch")]
    [InlineData("missing-surface")]
    [InlineData("wrong-surface")]
    [InlineData("missing-host")]
    [InlineData("wrong-target")]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-capability")]
    [InlineData("wrong-projection")]
    public async Task ReceiverRefusesUncommittedAuthorityWithoutChangingPersistedState(string fault)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        string fingerprint = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var original = fixture.Dispatch(fingerprint);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", original)).Outcomes[0].Status
            .ShouldBe(ProjectionDispatchStatus.Completed);
        var before = await SnapshotAsync();
        int writes = fixture.Backend.WrittenKeys.Count;
        ProjectionDispatchRequest request = original with { DispatchId = "refused-followup" };
        if (fault == "wrong-tenant") request = request with { Request = request.Request with { TenantId = "other-tenant" } };
        if (fault == "wrong-capability") request = request with { CatalogFingerprint = new string('f', 64) };
        if (fault == "wrong-projection") request = request with { ProjectionTypes = ["uncommitted-projection"] };
        RouteCatalogHostOptions options = fixture.App.Services.GetRequiredService<IOptions<RouteCatalogHostOptions>>().Value;
        if (fault == "missing-host") options.Identity = null;
        if (fault == "wrong-target") options.Identity = options.Identity! with { AppId = "other-receiver" };
        using HttpResponseMessage response = await fixture.SendAsync("/project/v2", request, context =>
        {
            if (fault == "missing-root") context.Request.Headers.Remove(RouteCatalogInvocationHandler.RootHeader);
            if (fault == "wrong-root") context.Request.Headers[RouteCatalogInvocationHandler.RootHeader] = new string('f', 64);
            if (fault == "missing-epoch") context.Request.Headers.Remove(RouteCatalogInvocationHandler.EpochHeader);
            if (fault == "wrong-epoch") context.Request.Headers[RouteCatalogInvocationHandler.EpochHeader] = "999";
            if (fault == "missing-surface") context.User = new ClaimsPrincipal(new ClaimsIdentity([], "workload"));
            if (fault == "wrong-surface") context.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(RouteCatalogSurfacePolicy.ClaimType, "agent"),
            ], "workload"));
        });
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        fixture.Backend.WrittenKeys.Count.ShouldBe(writes);
        (await SnapshotAsync()).ShouldBe(before);

        async Task<Dictionary<string, string>> SnapshotAsync()
        {
            Dictionary<string, string> result = [];
            foreach (string key in fixture.Backend.WrittenKeys.Distinct().Order(StringComparer.Ordinal))
            {
                var (bytes, etag) = await fixture.Backend.GetByteStateAndETagAsync("statestore", key, null, null!, TestContext.Current.CancellationToken);
                result[key] = etag + ":" + Convert.ToHexStringLower(SHA256.HashData(bytes.Span));
            }
            return result;
        }
    }
}
