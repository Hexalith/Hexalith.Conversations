using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Conversations.Server.Tests.Queries;

public sealed class ConversationNativeCurrentQueryTests
{
    [Theory]
    [InlineData("incarnation")]
    [InlineData("receipt")]
    [InlineData("catalog")]
    [InlineData("revoke-regrant")]
    [InlineData("revoke")]
    public async Task NativeChangesDuringFinalResourceIoWithholdAllMetadata(string change)
    {
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        fixture.Native.OnProjection = count =>
        {
            if (count != 2) return;
            if (change == "incarnation") fixture.Native.Incarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
            if (change == "receipt") fixture.Native.Receipt = new string('d', 64);
            if (change == "catalog") fixture.Native.Catalog = "changed-catalog";
            if (change == "revoke-regrant") fixture.Access.Health = new(Version: 9, Watermark: "native-revoke-regrant-prefix");
            if (change == "revoke") fixture.Access.Denied = true;
        };
        var result = await fixture.DispatchAsync();
        if (change == "revoke") result.ErrorMessage.ShouldBe("safe-denial");
        else result.GetPayload().Deserialize<ConversationResolutionMetadataResultV1>(JsonSerializerOptions.Web)!
            .Metadata.ShouldBeNull();
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("matching")]
    [InlineData("selection")]
    [InlineData("incarnation")]
    [InlineData("prefix")]
    [InlineData("head")]
    [InlineData("retention")]
    [InlineData("erased")]
    public async Task ProductionAdapterChecksExactProofThroughDelegatedSignedTransportAndIsolatedSelection(string change)
    {
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        var metadata = fixture.Metadata;
        string selectedKey = ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId);
        var selected = fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore", selectedKey)!;
        var source = metadata.NativeSourceProof!;
        source = change switch
        {
            "incarnation" => source with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" },
            "prefix" => source with { CumulativeDigest = new('c', 64) },
            "head" => source with { HeadMessageId = "replaced" },
            "retention" => source with { RetainedFloor = 2 },
            "erased" => source with { Erased = true },
            _ => source,
        };
        var proof = new Hexalith.EventStore.Contracts.Projections.ProjectionCurrentProof(source,
            new(selected.ProjectionOwner, "receipt", metadata.RequestFingerprint), "catalog");
        if (change == "selection") fixture.Store.SeedRaw("statestore", selectedKey,
            selected with { ProjectionOwner = selected.ProjectionOwner with { OwnerGeneration = new('d', 64) } });
        using var key = System.Security.Cryptography.RSA.Create(2048);
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:WorkloadIssuer:Workload"] = "conversations",
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
        }).Build();
        using var transport = new ResolutionDelegatedTransport(proof, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), fixture.Time.Now);
        var query = ResolutionMetadataFixture.Query();
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.Request.Headers[Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName] = "original-verified-assertion";
        var claims = Hexalith.EventStore.ServiceDefaults.Authentication.QueryCallerAuthorityBinding.Bind(query)
            .Select(pair => new System.Security.Claims.Claim(pair.Key, pair.Value)).ToList();
        claims.Add(new(Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"));
        claims.Add(new(Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadAuthenticationDefaults.OperationClaimType,
            Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadOperations.DomainServiceQuery));
        http.User = new(new System.Security.Claims.ClaimsIdentity(claims, "Workload"));
        var accessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = http };
        using var dapr = new Dapr.Client.DaprClientBuilder().Build();
        var client = new Hexalith.EventStore.DomainService.DelegatedOwnerQueryClient(dapr, transport, transport, accessor, configuration, fixture.Time);
        var guard = new ResolutionGuardedStore(fixture.Store) { ForbidLegacyReads = true, ForbidWrites = true };
        var evidence = new ConversationResolutionNativeEvidence(client, guard);
        using var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor>(accessor)
            .AddSingleton<Hexalith.EventStore.DomainService.IDomainQueryHandler>(new ResolutionDelegatedEvidenceHandler(evidence, metadata)).BuildServiceProvider();
        var result = await Hexalith.EventStore.DomainService.DomainQueryDispatcher.ExecuteAsync(services, query, TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        if (change == "matching") result.GetPayload().Deserialize<Hexalith.EventStore.Contracts.Projections.ProjectionCurrentProof>()!.ShouldBe(proof);
        else result.GetPayload().ValueKind.ShouldBe(JsonValueKind.Null);
        transport.Calls.ShouldBe(change == "selection" ? 0 : 1);
        guard.ForbiddenAttempts.ShouldBe(0);
        guard.ReadKeys.ShouldAllBe(value => value == selectedKey);
    }

    [Fact]
    public async Task LegacyBenignRedeliveryPreservesOriginalHeadAndRemainsUnproven()
    {
        var fixture = new ResolutionMetadataFixture();
        var legacy = JsonSerializer.Deserialize<Hexalith.EventStore.Contracts.Projections.ProjectionRequest>(
            JsonSerializer.Serialize(ResolutionMetadataFixture.Request() with { NativeSourceProof = null }))!;
        (await fixture.Handler().ProjectAsync(legacy, "dispatch-a", TestContext.Current.CancellationToken)).Status
            .ShouldBe(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed);
        var original = fixture.Metadata;
        var selected = fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(original.TenantId, original.ConversationId));
        var redelivery = legacy;
        redelivery = redelivery with { Events = [.. redelivery.Events.Select(item => item with { MessageId = "redelivery-transport-id" })] };
        (await fixture.Handler().ProjectAsync(redelivery, "dispatch-a", TestContext.Current.CancellationToken)).Status
            .ShouldBe(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed);
        fixture.Metadata.ShouldBe(original);
        fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(original.TenantId, original.ConversationId)).ShouldBe(selected);
        fixture.Metadata.SourceHeadMessageId.ShouldBe("source-message-1");
        fixture.Metadata.NativeSourceProof.ShouldBeNull();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(original.TenantId, original.ConversationId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProvenRedeliveryRejectsAlteredHeadIdentityAndPreservesOriginalGeneration()
    {
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        var request = ResolutionMetadataFixture.Request();
        var changed = request with { Events = [request.Events[0] with { MessageId = "different-message" }] };
        changed.VerifiedNativeSourceProof.ShouldBeNull();
        (await fixture.Handler().ProjectAsync(changed, "dispatch-a", TestContext.Current.CancellationToken)).Status
            .ShouldBe(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Failed);
        fixture.Metadata.ShouldBe(original);
        fixture.Proof.Current!.NativeSourceProof.ShouldBe(original.NativeSourceProof);
    }

    [Fact]
    public async Task MissingNativeAuthorityWithholdsBeforeProtectedLookup()
    {
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        fixture.Native.Available = false;
        (await fixture.DispatchAsync()).GetPayload().Deserialize<ConversationResolutionMetadataResultV1>(JsonSerializerOptions.Web)!
            .Metadata.ShouldBeNull();
        fixture.Guard.ReadKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task LegacyCoordinatedRebuildIsRefusedBeforeAnyReceiptOrRichManifestRead()
    {
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        var model = fixture.Metadata;
        var selection = ConversationResolutionNativeSelection.From(model, "actual-rebuild", rebuild: true)!;
        selection = selection with { ProjectionOwner = selection.ProjectionOwner with { ReceiptProjectionName = "rebuild" } };
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionSelectionKey(model.TenantId, model.ConversationId), selection);
        var evidence = new ConversationResolutionNativeEvidence(null!, fixture.Guard);
        fixture.Guard.ForbidLegacyReads = true; fixture.Guard.ForbidWrites = true;
        (await evidence.ReadProjectionAsync(model, TestContext.Current.CancellationToken)).ShouldBeNull();
        fixture.Guard.ReadKeys.ShouldBe([ConversationProjectionReadModelKeys.ResolutionSelectionKey(model.TenantId, model.ConversationId)]);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }
}
