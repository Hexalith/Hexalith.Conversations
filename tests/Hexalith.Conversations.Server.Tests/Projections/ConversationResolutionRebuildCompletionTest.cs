using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

public sealed class ConversationResolutionRebuildCompletionTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static string SelectionKey => ConversationProjectionReadModelKeys.ResolutionSelectionKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
    private static string ProofKey => ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
    private static string MetadataKey => ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);

    private static ProjectionDispatchRequest Rebuild(ResolutionEnrollmentFixture fixture, string catalog)
        => fixture.Dispatch(catalog, fixture.Input.Events[^1].MessageId!, fixture.Input with
            { RebuildSourceHeadSequence = fixture.Input.Events[^1].SequenceNumber });

    private static async Task<Dictionary<string, string>> SnapshotAsync(ResolutionEnrollmentFixture fixture)
    {
        var values = new Dictionary<string, string>();
        foreach (string key in fixture.Backend.WrittenKeys.Distinct().Order(StringComparer.Ordinal))
        {
            var (value, etag) = await fixture.Backend.GetByteStateAndETagAsync("statestore", key, null, null!, Token);
            values[key] = etag + ":" + Convert.ToHexStringLower(SHA256.HashData(value.Span));
        }
        return values;
    }

    private static bool AllowedRead(string key) => key == ProofKey || key == MetadataKey || key == SelectionKey
        || key == ConversationProjectionReadModelKeys.ResolutionFenceKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation)
        || key.StartsWith("projection:conversations-dispatch:", StringComparison.Ordinal)
        || key.StartsWith("hxrmb:v1:completion:", StringComparison.Ordinal);

    [Fact]
    public async Task CompletedOriginalRebuildIsReadThroughActualSdkOwnerEndpointAndSignedDelegatedAdapter()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = Rebuild(fixture, catalog);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        string completion = fixture.Backend.WrittenKeys.Distinct().Single(key => key.StartsWith("hxrmb:v1:completion:", StringComparison.Ordinal));
        var completionValue = (await fixture.Store.GetAsync<JsonDocument>("statestore", completion, Token)).Value!;
        completionValue.RootElement.GetProperty("owners")[0].GetProperty("ownerGeneration").GetString().ShouldBe(metadata.Digest());
        completionValue.RootElement.GetRawText().ShouldNotContain("messages");
        var before = await SnapshotAsync(fixture);
        int mutations = fixture.Backend.WrittenKeys.Count, forbidden = 0;
        fixture.Backend.BeforeRead = key => { if (!AllowedRead(key)) { forbidden++; throw new Xunit.Sdk.XunitException("Forbidden rich read: " + key); } };
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Query attempted mutation.");
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        (await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant,
            ResolutionMetadataFixture.Conversation, Token)).ShouldBe(metadata);
        var proof = (await session.ReadAsync(metadata)).ShouldNotBeNull();
        proof.Source.ShouldBe(fixture.Input.NativeSourceProof);
        proof.Owner.Selection.OwnerGeneration.ShouldBe(metadata.Digest());
        proof.Owner.Selection.ReceiptProjectionName.ShouldBe(ReadModelRebuildCompletion.ReceiptProjectionName);
        session.Transport.Calls.ShouldBe(1);
        forbidden.ShouldBe(0);
        fixture.Backend.WrittenKeys.Count.ShouldBe(mutations);
        fixture.Backend.BeforeRead = null; fixture.Backend.BeforeWrite = null;
        (await SnapshotAsync(fixture)).ShouldBe(before);
        // Re-running the original operation after a clock change cannot produce a new generation/cut.
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        (await SnapshotAsync(fixture)).ShouldBe(before);
    }

    [Theory]
    [InlineData("prepared")]
    [InlineData("installed")]
    [InlineData("proof")]
    [InlineData("metadata")]
    [InlineData("detail")]
    [InlineData("index")]
    [InlineData("ledger")]
    [InlineData("fence")]
    [InlineData("selection")]
    [InlineData("receipt")]
    [InlineData("completion")]
    public async Task InterruptedActualSdkRebuildRefusesWithoutRichReadsAndRestartRecoversOriginalIdentity(string boundary)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = Rebuild(fixture, catalog);
        string target = boundary switch
        {
            "prepared" => ProofKey, "proof" => ProofKey, "metadata" => MetadataKey,
            "detail" => ConversationProjectionReadModelKeys.ConversationKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
            "index" => ConversationProjectionReadModelKeys.TenantIndexKey(ResolutionMetadataFixture.Tenant),
            "ledger" => ConversationProjectionReadModelKeys.DispatchLedgerKey(request.DispatchId),
            "fence" => ConversationProjectionReadModelKeys.ResolutionFenceKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
            "selection" => SelectionKey, _ => string.Empty,
        };
        int markerWrites = 0;
        void Stop(string key)
        {
            if (key == target || boundary == "completion" && key.StartsWith("hxrmb:v1:completion:", StringComparison.Ordinal)
                || boundary == "receipt" && key.StartsWith("hxrmb:v1:marker:", StringComparison.Ordinal)
                    && !key.EndsWith(":manifest", StringComparison.Ordinal) && ++markerWrites == 2)
                throw new InvalidOperationException("Interrupted rebuild at " + boundary);
        }
        if (boundary == "prepared") fixture.Backend.BeforeWrite = Stop;
        var staged = await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request);
        if (boundary == "prepared") staged.Outcomes[0].Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        else
        {
            staged.Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
            if (boundary != "installed")
            {
                fixture.Backend.BeforeWrite = Stop;
                (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request))
                    .Outcomes[0].Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
            }
        }
        fixture.Backend.BeforeWrite = null;
        var before = await SnapshotAsync(fixture);
        int forbidden = 0, mutations = fixture.Backend.WrittenKeys.Count;
        fixture.Backend.BeforeRead = key => { if (!AllowedRead(key)) { forbidden++; throw new Xunit.Sdk.XunitException("Forbidden rich read: " + key); } };
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Query attempted mutation.");
        (await fixture.OwnerAsync(catalog)).Proof.ShouldBeNull();
        try
        {
            // The local subset may already be compacted, but cannot disclose without the owner gate.
            var metadata = await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(
                ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token);
            if (metadata is not null)
            {
                using var session = new ResolutionRebuildQuerySession(fixture, catalog);
                (await session.ReadAsync(metadata)).ShouldBeNull();
            }
        }
        catch (ConversationProjectionConsistencyException) { }
        forbidden.ShouldBe(0);
        fixture.Backend.WrittenKeys.Count.ShouldBe(mutations);
        fixture.Backend.BeforeRead = null; fixture.Backend.BeforeWrite = null;
        (await SnapshotAsync(fixture)).ShouldBe(before);
        var originalProof = fixture.Input.NativeSourceProof;
        await using var restarted = await fixture.RestartAsync(fixture.Time.Now.AddHours(1));
        restarted.App.ShouldNotBeSameAs(fixture.App);
        restarted.App.Services.ShouldNotBeSameAs(fixture.App.Services);
        restarted.Store.ShouldNotBeSameAs(fixture.Store);
        restarted.Time.ShouldNotBeSameAs(fixture.Time);
        restarted.Backend.ShouldBeSameAs(fixture.Backend);
        restarted.Input.NativeSourceProof.ShouldBe(originalProof);
        (await restarted.CatalogAsync()).CatalogFingerprint.ShouldBe(catalog);
        (await SnapshotAsync(restarted)).ShouldBe(before);
        // The original public request crosses a fresh host/container and is deserialized again. Only
        // retained SDK storage can recover the original immutable operation/time/generation.
        (await restarted.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await restarted.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var recovered = await restarted.MetadataAsync();
        recovered.NativeSourceProof.ShouldBe(originalProof);
        recovered.Freshness.ProjectionGeneratedAt.ShouldBe(ResolutionMetadataFixture.Started.AddSeconds(10));
        var completed = await SnapshotAsync(restarted);
        (await restarted.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await SnapshotAsync(restarted)).ShouldBe(completed);
        restarted.Backend.BeforeRead = key => AllowedRead(key).ShouldBeTrue(key);
        restarted.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Recovered query attempted mutation.");
        using var recoveredSession = new ResolutionRebuildQuerySession(restarted, catalog);
        (await recoveredSession.ReadAsync(recovered)).ShouldNotBeNull().Source.ShouldBe(originalProof);
        restarted.Backend.BeforeRead = null; restarted.Backend.BeforeWrite = null;
        (await SnapshotAsync(restarted)).ShouldBe(completed);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("fingerprint")]
    [InlineData("scope")]
    [InlineData("batch")]
    [InlineData("owner")]
    [InlineData("selection")]
    [InlineData("native")]
    [InlineData("catalog")]
    [InlineData("source")]
    [InlineData("legacy")]
    [InlineData("arbitrary")]
    public async Task AlteredActualCompletionOrAuthorityRefusesThroughDelegatedAdapterWithoutFallback(string change)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = Rebuild(fixture, catalog);
        await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        string key = fixture.Backend.WrittenKeys.Distinct().Single(value => value.StartsWith("hxrmb:v1:completion:", StringComparison.Ordinal));
        var selected = (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", SelectionKey, Token)).Value!;
        if (change == "missing")
            (await fixture.Store.TryEraseAsync("statestore", key, (await fixture.Store.GetAsync<JsonDocument>("statestore", key, Token)).ETag!, Token)).ShouldBeTrue();
        else if (change is "fingerprint" or "scope" or "batch" or "owner" or "malformed")
        {
            var raw = (await fixture.Store.GetAsync<JsonObject>("statestore", key, Token)).Value!;
            if (change == "fingerprint") raw["fingerprint"] = "v1:" + new string('A', 43);
            if (change == "scope") raw["scopeHash"] = "wrong-scope";
            if (change == "batch") raw["batchId"] = "wrong-batch";
            if (change == "owner") raw["owners"]![0]!["ownerIdentity"] = "wrong-owner";
            if (change == "malformed") raw.Remove("version");
            await fixture.Store.SaveAsync("statestore", key, raw, Token);
        }
        else if (change is "selection" or "native" or "legacy" or "arbitrary")
        {
            selected = change switch
            {
                "selection" => selected with { ProjectionOwner = selected.ProjectionOwner with { OwnerGeneration = "wrong-generation" } },
                "native" => selected with { NativeSourceProof = selected.NativeSourceProof! with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" } },
                "legacy" => selected with { ProjectionOwner = selected.ProjectionOwner with { ReceiptProjectionName = "rebuild" } },
                _ => selected with { ProjectionOwner = selected.ProjectionOwner with { ReceiptProjectionName = "arbitrary-receipt" } },
            };
            await fixture.Store.SaveAsync("statestore", SelectionKey, selected, Token);
        }
        var before = await SnapshotAsync(fixture);
        int forbidden = 0;
        fixture.Backend.BeforeRead = value => { if (!AllowedRead(value)) { forbidden++; throw new Xunit.Sdk.XunitException("Forbidden rich read: " + value); } };
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Query attempted mutation.");
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        if (change == "catalog") session.Transport.Catalog = "changed-catalog";
        if (change == "source") session.Transport.SourceOverride = fixture.Gateway.Proof with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" };
        (await session.ReadAsync(metadata)).ShouldBeNull();
        if (change is "selection" or "native" or "legacy" or "arbitrary") session.Transport.Calls.ShouldBe(0);
        forbidden.ShouldBe(0);
        fixture.Backend.BeforeRead = null; fixture.Backend.BeforeWrite = null;
        (await SnapshotAsync(fixture)).ShouldBe(before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectRichWriterBarrierStillWithholdsACompletedRebuild(bool pending)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = Rebuild(fixture, catalog);
        await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request);
        await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request);
        var models = (await fixture.Store.GetAsync<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), Token)).Value!;
        var writer = new ConversationProjectionReadModelWriter(fixture.Store);
        if (pending) await writer.MarkPendingAsync(models, Token); else await writer.PersistAsync(models, Token);
        var before = await SnapshotAsync(fixture);
        fixture.Backend.BeforeRead = key => key.ShouldBe(ProofKey);
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Query attempted mutation.");
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        fixture.Backend.BeforeRead = null; fixture.Backend.BeforeWrite = null;
        (await SnapshotAsync(fixture)).ShouldBe(before);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HistoricalV1PreparedRebuildRefusesBeforeMarkerReadWithVirginOrPreviousMetadata(bool previous)
    {
        await using var template = new ResolutionEnrollmentFixture();
        var catalog = (await template.CatalogAsync()).CatalogFingerprint!;
        (await template.PostAsync<ProjectionDispatchResponse>("/project/v2", template.Dispatch(catalog))).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await template.MetadataAsync();
        await using var fixture = new ResolutionEnrollmentFixture();
        if (previous)
        {
            await fixture.CatalogAsync();
            (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", fixture.Dispatch(catalog))).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        }
        var selection = ConversationResolutionNativeSelection.From(metadata, "historical-rebuild", rebuild: true)!;
        selection = selection with { ProjectionOwner = selection.ProjectionOwner with { ReceiptProjectionName = "rebuild" } };
        var batch = new ReadModelBatch(new("statestore", fixture.Input.TenantId, fixture.Input.Domain,
            fixture.Input.AggregateId, "rebuild", "historical-rebuild"),
        [
            ReadModelBatchOperation.Write(ProofKey, new ConversationResolutionMetadataProof(ConversationResolutionGeneration.From(metadata, false)), ReadModelBatchConcurrency.LastWrite),
            ReadModelBatchOperation.Write(MetadataKey, metadata, ReadModelBatchConcurrency.LastWrite),
            ReadModelBatchOperation.Write(SelectionKey, selection, ReadModelBatchConcurrency.LastWrite),
            ReadModelBatchOperation.Write("historical-rich-output", new { Messages = "legacy-rich-input" }, ReadModelBatchConcurrency.LastWrite),
        ]) { OwnerSelections = [selection.ProjectionOwner] };
        (await fixture.Store.StageAsync(batch, Token)).Status.ShouldBe(ReadModelBatchStagingStatus.Prepared);
        var (raw, _) = await fixture.Backend.GetByteStateAndETagAsync("statestore", ProofKey, null, null!, Token);
        using (var envelope = JsonDocument.Parse(raw))
        {
            envelope.RootElement.GetProperty("$hxrmb").GetInt32().ShouldBe(1);
            (envelope.RootElement.GetProperty("prev").ValueKind != JsonValueKind.Null).ShouldBe(previous);
        }
        var before = await SnapshotAsync(fixture);
        int forbidden = 0;
        fixture.Backend.BeforeRead = key => { if (!AllowedRead(key)) { forbidden++; throw new Xunit.Sdk.XunitException("Historical read attempted rich hydration."); } };
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Historical read attempted mutation.");
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        (await new Hexalith.Conversations.Server.Queries.ConversationResolutionNativeEvidence(null!, fixture.Store)
            .ReadProjectionAsync(metadata, Token)).ShouldBeNull();
        forbidden.ShouldBe(0);
        fixture.Backend.BeforeRead = null; fixture.Backend.BeforeWrite = null;
        (await SnapshotAsync(fixture)).ShouldBe(before);
    }

    [Fact]
    public async Task UnsupportedStoreCannotFallBackToResolvingReadsOrObtainNativeEvidence()
    {
        var original = new ResolutionMetadataFixture();
        await original.ProjectAsync();
        var store = new ResolutionUnsupportedCompactedStore();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await new ConversationProjectionReadStore(store).ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        (await new Hexalith.Conversations.Server.Queries.ConversationResolutionNativeEvidence(null!, store)
            .ReadProjectionAsync(original.Metadata, Token)).ShouldBeNull();
        store.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("matching")]
    [InlineData("completion")]
    [InlineData("source")]
    [InlineData("catalog")]
    [InlineData("permission")]
    public async Task CompletedRebuildExecutesProductionMetadataQueryHandlerAndFinalAuthorityGates(string change)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = Rebuild(fixture, catalog);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        if (change == "completion")
        {
            string key = fixture.Backend.WrittenKeys.Distinct().Single(value => value.StartsWith("hxrmb:v1:completion:", StringComparison.Ordinal));
            var proof = (await fixture.Store.GetAsync<JsonObject>("statestore", key, Token)).Value!;
            proof["owners"]![0]!["messages"] = "unknown-nested-evidence";
            await fixture.Store.SaveAsync("statestore", key, proof, Token);
        }
        var before = await SnapshotAsync(fixture);
        int writes = fixture.Backend.WrittenKeys.Count, forbidden = 0;
        fixture.Backend.BeforeRead = key => { if (!AllowedRead(key)) { forbidden++; throw new Xunit.Sdk.XunitException("Metadata query attempted rich retrieval."); } };
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Metadata query attempted mutation.");
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        session.Transport.OnProjection = count =>
        {
            if (count != 2) return;
            if (change == "source") session.Transport.SourceOverride = fixture.Gateway.Proof with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" };
            if (change == "catalog") session.Transport.Catalog = "changed-catalog";
            if (change == "permission") session.Transport.TenantAllowed = false;
        };
        var query = await session.ReadMetadataAsync();
        if (change == "permission")
        {
            query.Success.ShouldBeFalse();
            query.ErrorMessage.ShouldBe("safe-denial");
        }
        else
        {
            query.Success.ShouldBeTrue();
            var result = query.GetPayload().Deserialize<ConversationResolutionMetadataResultV1>(JsonSerializerOptions.Web)!;
            if (change == "matching")
            {
                result.Metadata.ShouldBe(metadata.Metadata);
                result.Freshness.ShouldBe(metadata.Freshness);
                result.ResourceVersion.ShouldBe(JsonSerializer.Serialize(fixture.Input.NativeSourceProof, JsonSerializerOptions.Web));
                result.PermissionVersion.ShouldNotBeNullOrWhiteSpace();
                result.AuthorizedAt.ShouldBe(fixture.Time.Now);
            }
            else
            {
                result.Metadata.ShouldBeNull(); result.Freshness.ShouldBeNull();
                result.ResourceVersion.ShouldBeNull(); result.PermissionVersion.ShouldBeNull();
            }
        }
        session.Transport.Calls.ShouldBe(change == "completion" ? 1 : 2);
        session.Transport.TenantCalls.ShouldBeGreaterThanOrEqualTo(2);
        forbidden.ShouldBe(0);
        fixture.Backend.WrittenKeys.Count.ShouldBe(writes);
        fixture.Backend.BeforeRead = null; fixture.Backend.BeforeWrite = null;
        (await SnapshotAsync(fixture)).ShouldBe(before);
    }

}
