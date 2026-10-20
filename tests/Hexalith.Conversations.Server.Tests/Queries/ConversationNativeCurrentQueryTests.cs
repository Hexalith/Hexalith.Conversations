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

    [Fact]
    public async Task BenignRedeliveryPreservesTheOriginalPersistedHeadIdentityAndNativeReceipt()
    {
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        var selected = fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(original.TenantId, original.ConversationId));
        var redelivery = ResolutionMetadataFixture.Request();
        redelivery = redelivery with { Events = [.. redelivery.Events.Select(item => item with { MessageId = "redelivery-transport-id" })] };
        (await fixture.Handler().ProjectAsync(redelivery, "dispatch-a", TestContext.Current.CancellationToken)).Status
            .ShouldBe(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed);
        fixture.Metadata.ShouldBe(original);
        fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(original.TenantId, original.ConversationId)).ShouldBe(selected);
        fixture.Metadata.SourceHeadMessageId.ShouldBe("source-message-1");
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
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionSelectionKey(model.TenantId, model.ConversationId), selection);
        var evidence = new ConversationResolutionNativeEvidence(null!, fixture.Guard);
        fixture.Guard.ForbidLegacyReads = true; fixture.Guard.ForbidWrites = true;
        (await evidence.ReadProjectionAsync(model, TestContext.Current.CancellationToken)).ShouldBeNull();
        fixture.Guard.ReadKeys.ShouldBe([ConversationProjectionReadModelKeys.ResolutionSelectionKey(model.TenantId, model.ConversationId)]);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }
}
