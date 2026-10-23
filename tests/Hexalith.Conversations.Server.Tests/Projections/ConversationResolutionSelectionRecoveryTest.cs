using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>Verifies producer-owned recovery of durable native metadata selection and receipt evidence.</summary>
public sealed class ConversationResolutionSelectionRecoveryTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Repairs selected-owner corruption while preserving the original native generation across process loss.</summary>
    /// <param name="corruption">The missing or corrupt selected-owner evidence.</param>
    /// <param name="interruptRepair">Whether the first repair is interrupted and resumed in a fresh container.</param>
    [Theory]
    [InlineData("missing", false)]
    [InlineData("generation", false)]
    [InlineData("native-proof", false)]
    [InlineData("projection-version", false)]
    [InlineData("batch", false)]
    [InlineData("receipt", false)]
    [InlineData("owner", false)]
    [InlineData("head-sequence", false)]
    [InlineData("head-message", false)]
    [InlineData("owner-version", false)]
    [InlineData("freshness", false)]
    [InlineData("null-owner", false)]
    [InlineData("missing", true)]
    [InlineData("batch", true)]
    public async Task CompletedRedeliveryRepairsSelectedOwnerWithoutChangingOriginalGeneration(string corruption, bool interruptRepair)
    {
        await using var initialFixture = new ResolutionEnrollmentFixture();
        ResolutionEnrollmentFixture fixture = initialFixture;
        string catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = fixture.Dispatch(catalog);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        string selectionKey = ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId);
        string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(metadata.TenantId, metadata.ConversationId);
        string fenceKey = ConversationProjectionReadModelKeys.ResolutionFenceKey(metadata.TenantId, metadata.ConversationId);
        string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(request.DispatchId);
        var originalProof = (await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore", proofKey, Token)).Value!;
        var originalFence = (await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore", fenceKey, Token)).Value!;
        var originalLedger = (await fixture.Store.GetAsync<ConversationProjectionDispatchLedger>("statestore", ledgerKey, Token)).Value!;
        var originalSelection = await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token);
        var detail = (await fixture.Store.GetAsync<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(metadata.TenantId, metadata.ConversationId), Token)).Value!;
        var index = (await fixture.Store.GetAsync<ConversationProjectionIndexReadModel>("statestore",
            ConversationProjectionReadModelKeys.TenantIndexKey(metadata.TenantId), Token)).Value!;
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        (await session.ReadAsync(metadata)).ShouldNotBeNull();
        await AssertMetadataAvailableAsync(session, metadata, fixture.Time.Now);

        if (corruption == "missing")
        {
            (await fixture.Store.TryEraseAsync("statestore", selectionKey, originalSelection.ETag!, Token)).ShouldBeTrue();
        }
        else
        {
            ConversationResolutionNativeSelection selected = originalSelection.Value!;
            ConversationResolutionNativeSelection altered = corruption switch
            {
                "generation" => selected with { ProjectionOwner = selected.ProjectionOwner with { OwnerGeneration = "altered" } },
                "native-proof" => selected with { NativeSourceProof = null },
                "projection-version" => selected with { ProjectionVersion = "altered" },
                "batch" => selected with { ProjectionOwner = selected.ProjectionOwner with { BatchId = "altered-batch" } },
                "receipt" => selected with { ProjectionOwner = selected.ProjectionOwner with { ReceiptProjectionName = "rebuild" } },
                "owner" => selected with { ProjectionOwner = selected.ProjectionOwner with { OwnerIdentity = "other-conversation" } },
                "head-sequence" => selected with { ProjectionOwner = selected.ProjectionOwner with { AppliedSequence = 2 } },
                "head-message" => selected with { ProjectionOwner = selected.ProjectionOwner with { MessageId = "other-message" } },
                "owner-version" => selected with { ProjectionOwner = selected.ProjectionOwner with { Version = 2 } },
                "freshness" => selected with { ProjectionOwner = selected.ProjectionOwner with { FreshnessVersion = "altered" } },
                _ => selected with { ProjectionOwner = null! },
            };
            await fixture.Store.SaveAsync("statestore", selectionKey, altered, Token);
        }

        int readsOnlyWrites = fixture.Backend.WrittenKeys.Count;
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Selected-owner read attempted repair.");
        (await session.ReadAsync(metadata)).ShouldBeNull();
        await AssertMetadataUnavailableAsync(session);
        fixture.Backend.WrittenKeys.Count.ShouldBe(readsOnlyWrites);
        fixture.Backend.BeforeWrite = null;

        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        if (interruptRepair)
        {
            fixture.Backend.BeforeWrite = key =>
            {
                if (key == selectionKey) throw new InvalidOperationException("Interrupted selected-owner repair.");
            };
            (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request))
                .Outcomes[0].Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
            (await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore", proofKey, Token))
                .Value!.Current!.IsPending.ShouldBeTrue();
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(metadata.TenantId, metadata.ConversationId, Token));
            (await fixture.OwnerAsync(catalog)).Proof.ShouldBeNull();
            await AssertMetadataUnavailableAsync(session);
            fixture.Backend.BeforeWrite = null;
        }

        await using var restarted = interruptRepair ? await fixture.RestartAsync(fixture.Time.Now.AddHours(1)) : null;
        if (restarted is not null)
        {
            restarted.Backend.ShouldBeSameAs(fixture.Backend);
            restarted.App.ShouldNotBeSameAs(fixture.App);
            restarted.Store.ShouldNotBeSameAs(fixture.Store);
            restarted.Time.ShouldNotBeSameAs(fixture.Time);
            (await restarted.CatalogAsync()).CatalogFingerprint.ShouldBe(catalog);
            fixture = restarted;
        }
        using var recoveredSession = new ResolutionRebuildQuerySession(fixture, catalog);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        var repaired = await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token);
        repaired.ETag.ShouldNotBeNullOrEmpty();
        repaired.ETag.ShouldNotBe(originalSelection.ETag);
        var repairedSelection = repaired.Value.ShouldNotBeNull();
        repairedSelection.NativeSourceProof.ShouldBe(metadata.NativeSourceProof);
        repairedSelection.ProjectionVersion.ShouldBe(metadata.Freshness.ProjectionCursor);
        repairedSelection.ProjectionOwner.ShouldBe(originalSelection.Value!.ProjectionOwner with { BatchId = repairedSelection.ProjectionOwner.BatchId });
        repairedSelection.ProjectionOwner.BatchId.ShouldNotBeNullOrWhiteSpace();
        (await fixture.OwnerAsync(catalog)).Proof.ShouldNotBeNull().Selection.ShouldBe(repairedSelection.ProjectionOwner);
        (await recoveredSession.ReadAsync(metadata)).ShouldNotBeNull().Source.ShouldBe(metadata.NativeSourceProof);
        await AssertMetadataAvailableAsync(recoveredSession, metadata, fixture.Time.Now);
        (await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore", proofKey, Token))
            .Value!.Current.ShouldBe(originalProof.Current);
        (await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore", fenceKey, Token))
            .Value!.Current.ShouldBe(originalFence.Current);
        (await fixture.Store.GetAsync<ConversationProjectionDispatchLedger>("statestore", ledgerKey, Token)).Value.ShouldBe(originalLedger);
        JsonElement.DeepEquals(JsonSerializer.SerializeToElement((await fixture.Store.GetAsync<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(metadata.TenantId, metadata.ConversationId), Token)).Value),
            JsonSerializer.SerializeToElement(detail)).ShouldBeTrue();
        JsonElement.DeepEquals(JsonSerializer.SerializeToElement((await fixture.Store.GetAsync<ConversationProjectionIndexReadModel>("statestore",
            ConversationProjectionReadModelKeys.TenantIndexKey(metadata.TenantId), Token)).Value),
            JsonSerializer.SerializeToElement(index)).ShouldBeTrue();
        (await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(metadata.TenantId, metadata.ConversationId, Token))
            .ShouldBe(metadata);

        int completedWrites = fixture.Backend.WrittenKeys.Count;
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Completed repaired redelivery attempted mutation.");
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Backend.WrittenKeys.Count.ShouldBe(completedWrites);
    }

    /// <summary>Detects an unchanged selected value whose ETag changes between receipt validation and confirmation.</summary>
    [Fact]
    public async Task SelectionEtagInterleavingReopensProofAndRepairsBeforeReportingCompletion()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        string catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = fixture.Dispatch(catalog);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        string selectionKey = ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId);
        string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(metadata.TenantId, metadata.ConversationId);
        string detailKey = ConversationProjectionReadModelKeys.ConversationKey(metadata.TenantId, metadata.ConversationId);
        var selected = (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token)).Value!;
        string receiptKey = new ReadModelBatchScope("statestore", metadata.TenantId.Value,
            ConversationProjectionHandler.ConversationDomain, metadata.ConversationId.Value,
            ConversationAsyncProjectionHandler.ConversationReadModelProjectionType, selected.ProjectionOwner.BatchId).ComputeMarkerKey();
        bool receiptRead = false, awaitingConfirmation = false, interleaved = false, reopenedProof = false;
        fixture.Backend.BeforeRead = key =>
        {
            if (key == receiptKey) receiptRead = true;
            // Recovery probes happen before the rich detail verification. Arm only after the initial
            // selection and its actual completed receipt have already been checked by durability.
            if (receiptRead && key == detailKey) awaitingConfirmation = true;
            if (awaitingConfirmation && key == selectionKey && !interleaved)
            {
                interleaved = true;
                fixture.Backend.BeforeRead = null;
                // The value remains valid. Only a changed ETag can detect this interleaving.
                fixture.Store.SaveAsync("statestore", selectionKey, selected, Token).GetAwaiter().GetResult();
            }
        };
        fixture.Backend.BeforeWrite = key =>
        {
            if (interleaved && key == proofKey) reopenedProof = true;
        };
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        interleaved.ShouldBeTrue();
        reopenedProof.ShouldBeTrue();
        fixture.Backend.BeforeRead = null;
        fixture.Backend.BeforeWrite = null;
        var repaired = (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token)).Value!;
        repaired.ProjectionOwner.BatchId.ShouldNotBe(selected.ProjectionOwner.BatchId);
        repaired.ProjectionOwner.ShouldBe(selected.ProjectionOwner with { BatchId = repaired.ProjectionOwner.BatchId });
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        (await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore", proofKey, Token))
            .Value!.Current!.IsPending.ShouldBeFalse();
        (await fixture.OwnerAsync(catalog)).Proof.ShouldNotBeNull().Selection.ShouldBe(repaired.ProjectionOwner);
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        await AssertMetadataAvailableAsync(session, metadata, fixture.Time.Now);
    }

    /// <summary>Repairs a lost or corrupt selected owner created by an accepted native rebuild.</summary>
    /// <param name="corruption">The selected-owner deletion or batch identity corruption.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("batch")]
    public async Task RebuildOriginSelectionRepairPreservesGenerationAndPublishesValidReplacementReceipt(string corruption)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        string catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = fixture.Dispatch(catalog, fixture.Input.Events[^1].MessageId!,
            fixture.Input with { RebuildSourceHeadSequence = fixture.Input.Events[^1].SequenceNumber });
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        string selectionKey = ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId);
        var selected = await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token);
        var originalSelection = selected.Value.ShouldNotBeNull();
        originalSelection.ProjectionOwner.ReceiptProjectionName.ShouldBe(ReadModelRebuildCompletion.ReceiptProjectionName);
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        await AssertMetadataAvailableAsync(session, metadata, fixture.Time.Now);
        if (corruption == "missing")
            (await fixture.Store.TryEraseAsync("statestore", selectionKey, selected.ETag!, Token)).ShouldBeTrue();
        else
            await fixture.Store.SaveAsync("statestore", selectionKey, originalSelection with
                { ProjectionOwner = originalSelection.ProjectionOwner with { BatchId = "wrong-rebuild-batch" } }, Token);
        int readOnlyWrites = fixture.Backend.WrittenKeys.Count;
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Rebuild-origin query attempted repair.");
        (await fixture.OwnerAsync(catalog)).Proof.ShouldBeNull();
        await AssertMetadataUnavailableAsync(session);
        fixture.Backend.WrittenKeys.Count.ShouldBe(readOnlyWrites);
        fixture.Backend.BeforeWrite = null;
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        var repaired = (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token)).Value!;
        repaired.NativeSourceProof.ShouldBe(metadata.NativeSourceProof);
        repaired.ProjectionOwner.ReceiptProjectionName.ShouldBeNull();
        repaired.ProjectionOwner.ShouldBe(originalSelection.ProjectionOwner with
        {
            BatchId = repaired.ProjectionOwner.BatchId,
            ReceiptProjectionName = null,
        });
        repaired.ProjectionOwner.BatchId.ShouldNotBe(originalSelection.ProjectionOwner.BatchId);
        (await fixture.OwnerAsync(catalog)).Proof.ShouldNotBeNull().Selection.ShouldBe(repaired.ProjectionOwner);
        await AssertMetadataAvailableAsync(session, metadata, fixture.Time.Now);
        (await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(metadata.TenantId, metadata.ConversationId, Token))
            .ShouldBe(metadata);
    }

    /// <summary>Repairs actual receipt loss or corruption even while the selected-owner row stays intact.</summary>
    /// <param name="missing">Whether the original completed receipt is deleted instead of corrupted.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedRedeliveryRepairsActualReceiptWithoutChangingOriginalMetadata(bool missing)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        string catalog = (await fixture.CatalogAsync()).CatalogFingerprint!;
        var request = fixture.Dispatch(catalog);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        string selectionKey = ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId);
        var selected = await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token);
        var originalSelection = selected.Value.ShouldNotBeNull();
        string receiptKey = new ReadModelBatchScope("statestore", metadata.TenantId.Value,
            ConversationProjectionHandler.ConversationDomain, metadata.ConversationId.Value,
            ConversationAsyncProjectionHandler.ConversationReadModelProjectionType, originalSelection.ProjectionOwner.BatchId).ComputeMarkerKey();
        var (bytes, etag) = await fixture.Backend.GetByteStateAndETagAsync("statestore", receiptKey, null, null!, Token);
        bytes.IsEmpty.ShouldBeFalse();
        using var session = new ResolutionRebuildQuerySession(fixture, catalog);
        await AssertMetadataAvailableAsync(session, metadata, fixture.Time.Now);
        if (missing)
            (await fixture.Store.TryEraseAsync("statestore", receiptKey, etag, Token)).ShouldBeTrue();
        else
        {
            var receipt = JsonSerializer.Deserialize<JsonObject>(bytes.Span, JsonSerializerOptions.Web)!;
            receipt["batch"] = "wrong-receipt-batch";
            await fixture.Store.SaveAsync("statestore", receiptKey, receipt, Token);
        }
        (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token)).ShouldBe(selected);
        int readOnlyWrites = fixture.Backend.WrittenKeys.Count;
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Receipt query attempted repair.");
        (await fixture.OwnerAsync(catalog)).Proof.ShouldBeNull();
        await AssertMetadataUnavailableAsync(session);
        fixture.Backend.WrittenKeys.Count.ShouldBe(readOnlyWrites);
        fixture.Backend.BeforeWrite = null;
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        var repaired = (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", selectionKey, Token)).Value!;
        repaired.ProjectionOwner.ShouldBe(originalSelection.ProjectionOwner with { BatchId = repaired.ProjectionOwner.BatchId });
        repaired.ProjectionOwner.BatchId.ShouldNotBe(originalSelection.ProjectionOwner.BatchId);
        repaired.NativeSourceProof.ShouldBe(metadata.NativeSourceProof);
        string replacementKey = new ReadModelBatchScope("statestore", metadata.TenantId.Value,
            ConversationProjectionHandler.ConversationDomain, metadata.ConversationId.Value,
            ConversationAsyncProjectionHandler.ConversationReadModelProjectionType, repaired.ProjectionOwner.BatchId).ComputeMarkerKey();
        var replacement = await fixture.Backend.GetByteStateAndETagAsync("statestore", replacementKey, null, null!, Token);
        replacement.Item1.IsEmpty.ShouldBeFalse();
        (await fixture.OwnerAsync(catalog)).Proof.ShouldNotBeNull().Selection.ShouldBe(repaired.ProjectionOwner);
        await AssertMetadataAvailableAsync(session, metadata, fixture.Time.Now);
        (await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(metadata.TenantId, metadata.ConversationId, Token))
            .ShouldBe(metadata);
    }

    private static async Task AssertMetadataAvailableAsync(ResolutionRebuildQuerySession session,
        ConversationResolutionMetadataReadModel metadata, DateTimeOffset authorizedAt)
    {
        var query = await session.ReadMetadataAsync();
        query.Success.ShouldBeTrue();
        var result = query.GetPayload().Deserialize<ConversationResolutionMetadataResultV1>(JsonSerializerOptions.Web)!;
        result.Metadata.ShouldBe(metadata.Metadata);
        result.Freshness.ShouldBe(metadata.Freshness);
        result.ResourceVersion.ShouldBe(JsonSerializer.Serialize(metadata.NativeSourceProof, JsonSerializerOptions.Web));
        result.PermissionVersion.ShouldNotBeNullOrWhiteSpace();
        result.AuthorizedAt.ShouldBe(authorizedAt);
    }

    private static async Task AssertMetadataUnavailableAsync(ResolutionRebuildQuerySession session)
    {
        var query = await session.ReadMetadataAsync();
        query.Success.ShouldBeTrue();
        var result = query.GetPayload().Deserialize<ConversationResolutionMetadataResultV1>(JsonSerializerOptions.Web)!;
        result.Metadata.ShouldBeNull();
        result.Freshness.ShouldBeNull();
        result.ResourceVersion.ShouldBeNull();
        result.PermissionVersion.ShouldBeNull();
        result.AuthorizedAt.ShouldBeNull();
    }
}
