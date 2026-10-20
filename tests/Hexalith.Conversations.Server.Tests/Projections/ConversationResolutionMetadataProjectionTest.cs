using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Projections;

public sealed class ConversationResolutionMetadataProjectionTest
{
    private static CancellationToken Token => TestContext.Current!.CancellationToken;
    private static TenantId Tenant => ResolutionMetadataFixture.Tenant;
    private static ConversationId Conversation => ResolutionMetadataFixture.Conversation;

    [Fact]
    public async Task ProductionDispatchPersistsIsolatedSubsetAndPermanentContentFreeProof()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var metadata = fixture.Metadata;
        var proof = fixture.Proof;
        proof.Current!.IsPending.ShouldBeFalse();
        proof.Current!.Matches(metadata).ShouldBeTrue();
        metadata.Metadata.ProjectId.ShouldBe("project-original");
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(metadata);
        string serialized = JsonSerializer.Serialize(proof);
        serialized.ShouldNotContain("Scoring label");
        serialized.ShouldNotContain("project-original");
        serialized.ShouldNotContain("actor");
    }

    [Fact]
    public async Task OpaqueMetadataKeysPreserveSpacesUnicodeAndSeparatorsWithoutAliasing()
    {
        ResolutionMetadataFixture fixture = new();
        TenantId tenant = new(" tenant: /日本 ");
        ConversationId conversation = new(" conversation: /日本 ");
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(tenantId: tenant, conversationId: conversation),
            "dispatch-opaque", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.Reader().ReadResolutionMetadataAsync(tenant, conversation, Token))!.Metadata.ConversationId.ShouldBe(conversation.Value);
        (await fixture.Reader().ReadResolutionMetadataAsync(tenant, new(conversation.Value.Trim()), Token)).ShouldBeNull();
    }

    [Fact]
    public async Task ReaderRacingACompletedGateCannotFetchRichRecoveryManifestThroughMetadata()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ReadModelBatch? isolatedBatch = null;
        bool inMetadata = false;
        TaskCompletionSource installed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Guard.BeforeBatch = batch =>
        {
            inMetadata = batch.Scope.ProjectionType == ConversationAsyncProjectionHandler.ConversationReadModelProjectionType;
            if (inMetadata)
            {
                isolatedBatch = batch;
                batch.Operations.Select(operation => operation.Key).ShouldBe([
                    ConversationProjectionReadModelKeys.ResolutionFenceKey(Tenant, Conversation),
                    ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation),
                    ConversationProjectionReadModelKeys.ResolutionSelectionKey(Tenant, Conversation)]);
            }
            else batch.Operations.ShouldAllBe(operation => operation.Key != ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation)
                && operation.Key != ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation));
            return Task.CompletedTask;
        };
        fixture.Store.BatchFaultHook = async (phase, ordinal, _) =>
        {
            if (inMetadata && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 1)
            {
                installed.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task<Hexalith.EventStore.DomainService.DomainProjectionHandlerResult>? writer = null;
        var readerGuard = new ResolutionGuardedStore(fixture.Store) { ForbidLegacyReads = true, ForbidWrites = true };
        readerGuard.AfterRead = async (type, _) =>
        {
            if (type == typeof(ConversationResolutionMetadataProof) && writer is null)
            {
                writer = fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(changedProject: true), "dispatch-race", Token);
                await installed.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            }
        };
        try
        {
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader(readerGuard).ReadResolutionMetadataAsync(Tenant, Conversation, Token));
            // Inspect the actual SDK marker read by metadata-envelope resolution. Its retained manifest has
            // only the classified subset and content-free fence, even in this worst-case gate race.
            using JsonDocument marker = fixture.Store.Snapshot<JsonDocument>("statestore", isolatedBatch!.Scope.ComputeMarkerKey())!;
            using JsonDocument manifest = JsonDocument.Parse(marker.RootElement.GetProperty("manifest").GetBytesFromBase64());
            foreach (JsonElement operation in manifest.RootElement.GetProperty("ops").EnumerateArray())
            {
                using JsonDocument value = JsonDocument.Parse(operation.GetProperty("val").GetBytesFromBase64());
                value.RootElement.TryGetProperty("detail", out _).ShouldBeFalse();
                value.RootElement.TryGetProperty("summaries", out _).ShouldBeFalse();
                value.RootElement.TryGetProperty("messages", out _).ShouldBeFalse();
            }
            readerGuard.ForbiddenAttempts.ShouldBe(0);
        }
        finally
        {
            resume.TrySetResult();
            if (writer is not null) (await writer).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        }
    }

    [Fact]
    public async Task LedgerExpiryAndRestartRecoverOriginalGenerationAndRejectIdentityReuse()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        var proof = fixture.Proof;
        await fixture.EraseAsync(ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-a"));
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(original);
        fixture.Time.Now = ResolutionMetadataFixture.Started.AddDays(1);
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.ShouldBe(original);
        fixture.Proof.Current!.Freshness.ShouldBe(proof.Current!.Freshness);
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request("changed payload"), "dispatch-a", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Failed);
        fixture.Metadata.ShouldBe(original);
    }

    [Theory]
    [InlineData("metadata-missing")]
    [InlineData("proof-missing")]
    [InlineData("pending")]
    [InlineData("digest")]
    [InlineData("tenant")]
    [InlineData("generation")]
    [InlineData("fingerprint")]
    [InlineData("ledger-pending")]
    [InlineData("ledger-mismatch")]
    public async Task PartialMalformedAndMixedGenerationsFailClosedWithoutRepair(string scenario)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation);
        string metadataKey = ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation);
        switch (scenario)
        {
            case "metadata-missing": await fixture.EraseAsync(metadataKey); break;
            case "proof-missing": await fixture.EraseAsync(proofKey); break;
            case "pending": fixture.Store.SeedRaw("statestore", proofKey, fixture.Proof with { Current = fixture.Proof.Current! with { IsPending = true } }); break;
            case "digest": fixture.Store.SeedRaw("statestore", metadataKey, fixture.Metadata with { Metadata = fixture.Metadata.Metadata with { ProjectId = "forged-project" } }); break;
            case "tenant": fixture.Store.SeedRaw("statestore", metadataKey, fixture.Metadata with { TenantId = new("foreign") }); break;
            case "generation": fixture.Store.SeedRaw("statestore", metadataKey, fixture.Metadata with { DispatchId = "foreign" }); break;
            case "fingerprint": fixture.Store.SeedRaw("statestore", metadataKey, fixture.Metadata with { RequestFingerprint = "malformed" }); break;
            default:
                string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-a");
                var ledger = fixture.Store.Snapshot<ConversationProjectionDispatchLedger>("statestore", ledgerKey)!;
                fixture.Store.SeedRaw("statestore", ledgerKey, scenario == "ledger-pending"
                    ? ledger with { Status = ConversationProjectionDispatchStatus.Pending }
                    : ledger with { RequestFingerprint = "other" });
                break;
        }

        fixture.Guard.ForbidLegacyReads = fixture.Guard.ForbidWrites = true;
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader(fixture.Guard).ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
        fixture.Guard.Reads.ShouldAllBe(type => type == typeof(ConversationResolutionMetadataReadModel)
            || type == typeof(ConversationResolutionMetadataProof) || type == typeof(ConversationProjectionDispatchLedger));
    }

    [Fact]
    public async Task ProofChangingDuringReadIsUnavailableEvenWhenMetadataMatchedTheFirstProof()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        fixture.Guard.AfterRead = (type, _) =>
        {
            if (type == typeof(ConversationResolutionMetadataReadModel))
                fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation), fixture.Proof);
            return Task.CompletedTask;
        };
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader(fixture.Guard).ReadResolutionMetadataAsync(Tenant, Conversation, Token));
    }

    public static IEnumerable<object[]> BatchInterruptionPoints()
    {
        foreach (string stage in new[] { ConversationAsyncProjectionHandler.ConversationReadModelProjectionType })
        {
            int count = 2;
            foreach (ReadModelBatchPhase phase in new[] { ReadModelBatchPhase.BeforeInstallOperation, ReadModelBatchPhase.AfterInstallOperation })
                for (int ordinal = 0; ordinal < count; ordinal++) yield return [stage, phase, ordinal];
            yield return [stage, ReadModelBatchPhase.BeforeCommit, -1];
            yield return [stage, ReadModelBatchPhase.AfterCommit, -1];
            for (int ordinal = 0; ordinal < count; ordinal++) yield return [stage, ReadModelBatchPhase.BeforeCompaction, ordinal];
            for (int ordinal = 0; ordinal < count - 1; ordinal++) yield return [stage, ReadModelBatchPhase.AfterCompaction, ordinal];
            yield return [stage, ReadModelBatchPhase.BeforeReceipt, -1];
            yield return [stage, ReadModelBatchPhase.RecoveryReceiptWritten, -1];
            yield return [stage, ReadModelBatchPhase.RecoveryManifestReclaimed, -1];
            yield return [stage, ReadModelBatchPhase.RecoveryAnchorCompacted, -1];
        }
    }

    [Theory]
    [InlineData("pending-proof", false)]
    [InlineData("pending-proof", true)]
    [InlineData("pending-fence", false)]
    [InlineData("pending-fence", true)]
    [InlineData("completed-fence", false)]
    [InlineData("completed-fence", true)]
    [InlineData("completed-proof", false)]
    [InlineData("completed-proof", true)]
    [InlineData("completed-ledger", false)]
    [InlineData("completed-ledger", true)]
    public async Task InterruptedOwnerWritesNeverReportFalseCompletionAndRetryPreservesTimestamp(string boundary, bool afterWrite)
    {
        ResolutionMetadataFixture fixture = new();
        bool faulted = false;
        Func<string, object, Task> fault = (key, value) =>
        {
            bool selected = value switch
            {
                ConversationResolutionMetadataProof proof => boundary == (proof.Current!.IsPending ? "pending-" : "completed-")
                    + (key.Contains("-fence:", StringComparison.Ordinal) ? "fence" : "proof"),
                ConversationProjectionDispatchLedger ledger => boundary == "completed-ledger" && ledger.Status == ConversationProjectionDispatchStatus.Completed,
                _ => false,
            };
            if (selected && !faulted)
            {
                faulted = true;
                throw new IOException("interrupted owner write");
            }

            return Task.CompletedTask;
        };
        if (afterWrite) fixture.Guard.AfterKeySave = fault;
        else fixture.Guard.BeforeKeySave = fault;
        var first = await fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-owner-interrupted", Token);
        faulted.ShouldBeTrue();
        first.Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        var ledger = fixture.Store.Snapshot<ConversationProjectionDispatchLedger>("statestore",
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-owner-interrupted"))!;
        DateTimeOffset original = ledger.ProjectionGeneratedAt;
        if (ledger.Status != ConversationProjectionDispatchStatus.Completed)
        {
            try
            {
                (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBeNull();
            }
            catch (ConversationProjectionConsistencyException) { }
        }

        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-owner-interrupted", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.Freshness.ProjectionGeneratedAt.ShouldBe(original);
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(BatchInterruptionPoints))]
    public async Task InterruptedCoordinatedComponentsWithholdOrConvergeAfterRestart(string stage, ReadModelBatchPhase phase, int ordinal)
    {
        ResolutionMetadataFixture fixture = new();
        bool armed = false;
        fixture.Guard.BeforeBatch = batch =>
        {
            armed = batch.Scope.ProjectionType == stage;
            return Task.CompletedTask;
        };
        bool faulted = false;
        fixture.Store.BatchFaultHook = (observed, index, _) =>
        {
            if (armed && observed == phase && (ordinal < 0 || index == ordinal) && !faulted)
            {
                faulted = true;
                throw new IOException("interrupted component batch");
            }

            return Task.CompletedTask;
        };
        var first = await fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-interrupted", Token);
        faulted.ShouldBeTrue();
        if (first.Status != ProjectionDispatchStatus.Completed)
        {
            var persistedProof = await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore",
                ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation), Token);
            persistedProof.Value!.Current!.IsPending.ShouldBeTrue();
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        }

        fixture.Store.BatchFaultHook = null;
        // Retry reuses durable manifest/generation state rather than manufacturing a new timestamp.
        var retry = await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-interrupted", Token);
        retry.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Proof.Current!.IsPending.ShouldBeFalse();
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(fixture.Metadata);
    }

    [Fact]
    public async Task RebuildAtomicallyPromotesMetadataProofAndLegacyGenerationWithExpectedEtags()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var old = fixture.Metadata;
        var plan = await fixture.Handler().PrepareRebuildAsync(ResolutionMetadataFixture.Request(changedProject: true), "rebuild-new", Token);
        plan.Operations.Count.ShouldBe(7);
        plan.Operations.Single(operation => operation.Key == ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation))
            .Concurrency.ShouldNotBe(ReadModelBatchConcurrency.CreateOnly);
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(old);
        var result = await fixture.Store.ExecuteAsync(new(new("statestore", Tenant.Value, "conversations", Conversation.Value,
            "conversation-read-model", "rebuild-new"), plan.Operations), Token);
        result.IsSuccess.ShouldBeTrue();
        fixture.Metadata.Metadata.ProjectId.ShouldBe("project-new");
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(fixture.Metadata);
    }

    [Fact]
    public async Task ProductionSdkRebuildWithLegacyCoexistenceKeepsEveryVisibleMarkerContentFree()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ReadModelBatch? captured = null;
        fixture.Guard.BeforeBatch = batch =>
        {
            captured = batch;
            batch.RetainManifestForRecovery.ShouldBeFalse();
            batch.OwnerSelection.ShouldBeNull();
            batch.OwnerSelections.ShouldBeNull();
            return Task.CompletedTask;
        };
        bool inspected = false;
        fixture.Store.BatchFaultHook = (phase, _, _) =>
        {
            if (phase == ReadModelBatchPhase.BeforeCommit && captured is not null)
            {
                inspected = true;
                using JsonDocument marker = fixture.Store.Snapshot<JsonDocument>("statestore", captured.Scope.ComputeMarkerKey())!;
                marker.RootElement.TryGetProperty("manifest", out _).ShouldBeFalse();
                marker.RootElement.TryGetProperty("manifestKey", out _).ShouldBeFalse();
                marker.RootElement.TryGetProperty("owners", out _).ShouldBeFalse();
                foreach (JsonElement descriptor in marker.RootElement.GetProperty("ops").EnumerateArray())
                    descriptor.TryGetProperty("val", out _).ShouldBeFalse();
            }

            return Task.CompletedTask;
        };
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IReadModelBatchStore>(fixture.Guard)
            .AddSingleton<IDomainProjectionHandler>(new ConversationProjectionHandler(new(), fixture.Time))
            .AddSingleton<IAsyncDomainProjectionHandler>(fixture.Handler())
            .BuildServiceProvider();
        string fingerprint = ProjectionRouteCatalogFingerprint.Compute("conversations-service", "v1",
            [new ProjectionDispatchRoute(ConversationProjectionHandler.ConversationDomain, ConversationAsyncProjectionHandler.ConversationReadModelProjectionType)]);
        ProjectionDispatchResponse response = await DomainProjectionDispatcher.RebuildAsync(provider,
            new(ResolutionMetadataFixture.Request(changedProject: true), [ConversationAsyncProjectionHandler.ConversationReadModelProjectionType],
                "rebuild-production", fingerprint), new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = "conversations-service", ServiceVersion = "v1" }, Token);
        inspected.ShouldBeTrue();
        response.Outcomes.ShouldAllBe(outcome => outcome.Status == ProjectionDispatchStatus.Completed);
        fixture.Metadata.Metadata.ProjectId.ShouldBe("project-new");
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
    }

    [Fact]
    public async Task CompetingDispatchesPreserveTheDeterministicWinner()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        await fixture.ProjectAsync("dispatch-z", changedProject: true);
        var winner = fixture.Metadata;
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.ShouldBe(winner);
        fixture.Proof.Current!.Matches(winner).ShouldBeTrue();
        fixture.Proof.PreviousCompleted!.DispatchId.ShouldBe("dispatch-a");
        await Should.ThrowAsync<Hexalith.EventStore.DomainService.DomainProjectionRebuildRejectedException>(() =>
            fixture.Handler().PrepareRebuildAsync(ResolutionMetadataFixture.Request(), "rebuild-old", Token));
        fixture.Metadata.ShouldBe(winner);
    }

    [Fact]
    public async Task TerminalPrewriteFailureKeepsPendingProofUntilSupportedRetryConverges()
    {
        ResolutionMetadataFixture fixture = new();
        fixture.Guard.BeforeSave = (type, _) => type == typeof(ConversationProjectedReadModels)
            ? throw new InvalidOperationException("terminal component admission failure") : Task.CompletedTask;
        (await fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-terminal", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        fixture.Proof.Current!.IsPending.ShouldBeTrue();
        (await fixture.Handler(fixture.Guard).ReconcileAsync(ResolutionMetadataFixture.Request(), "dispatch-terminal", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Retryable); // Guard deliberately does not opt into erasure.
        (await new ConversationProjectionReadModelWriter(fixture.Store).TryReconcileTerminalDispatchAsync(Tenant, Conversation, "dispatch-terminal", Token))
            .ShouldBeTrue();
        (await new ConversationResolutionMetadataPersistence(fixture.Store).TryRestoreTerminalAsync(Tenant, Conversation, "dispatch-terminal", Token))
            .ShouldBeFalse();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        fixture.Store.Snapshot<ConversationProjectionIndexReadModel>("statestore",
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!.Dispatches.ShouldBeEmpty();
        fixture.Store.Snapshot<ConversationResolutionMetadataReadModel>("statestore",
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation)).ShouldBeNull();
        await fixture.ProjectAsync("dispatch-terminal");
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
    }

    [Fact]
    public async Task TerminalCompensationRefusesAnAlreadyAdvancedMetadataGeneration()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        fixture.Guard.BeforeSave = (_, value) => value is ConversationResolutionMetadataProof { Current.IsPending: false }
            ? throw new InvalidOperationException("interrupted proof completion") : Task.CompletedTask;
        (await fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(changedProject: true), "dispatch-advanced", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        fixture.Metadata.DispatchId.ShouldBe("dispatch-advanced");
        (await new ConversationProjectionReadModelWriter(fixture.Store).TryReconcileTerminalDispatchAsync(Tenant, Conversation, "dispatch-advanced", Token))
            .ShouldBeFalse();
        fixture.Proof.Current!.IsPending.ShouldBeTrue();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        await fixture.ProjectAsync("dispatch-advanced", changedProject: true);
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
    }

    [Theory]
    [InlineData("metadata")]
    [InlineData("proof")]
    public async Task CompletedLedgerDoesNotHideMissingMetadataOrProofAndProjectionRetryRepairsIt(string missing)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        await fixture.EraseAsync(missing == "metadata"
            ? ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation)
            : ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation));
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        await fixture.ProjectAsync();
        fixture.Metadata.ShouldBe(original);
        fixture.Proof.Current!.Matches(original).ShouldBeTrue();
    }

    [Fact]
    public async Task RepairingACompletedSameGenerationFirstWithholdsTheOuterReadProof()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        await fixture.EraseAsync(ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation));
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Guard.BeforeSave = async (type, _) =>
        {
            if (type == typeof(ConversationProjectedReadModels))
            {
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task<DomainProjectionHandlerResult> repair = fixture.Handler(fixture.Guard)
            .ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token);
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            fixture.Proof.Current!.IsPending.ShouldBeTrue();
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        }
        finally
        {
            resume.TrySetResult();
            (await repair).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        }

        fixture.Metadata.ShouldBe(original);
        fixture.Proof.Current!.IsPending.ShouldBeFalse();
    }

}
