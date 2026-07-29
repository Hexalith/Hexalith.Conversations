// <copyright file="ConversationAsyncProjectionHandlerTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Events;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>Exercises durable named dispatch and full-replay planning at the Conversations boundary.</summary>
public sealed class ConversationAsyncProjectionHandlerTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly DateTimeOffset Started = new(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AcceptedAppendShouldCompleteOnlyAfterBothExactKeysAreDurable()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-001",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
        ConversationProjectionIndexReadModel? index = store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));
        index.ShouldNotBeNull();
        index!.Summaries.ShouldHaveSingleItem().ConversationId.ShouldBe(Conversation);
        index.Dispatches[Conversation.Value].DispatchId.ShouldBe("dispatch-001");
        store.Snapshot<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-001"))!
            .Status.ShouldBe(ConversationProjectionDispatchStatus.Completed);
        store.LastTimeToLive.ShouldBe(ProjectionDispatchOptions.DefaultRedeliveryWindow);
    }

    [Fact]
    public async Task StableDuplicateShouldConvergeWithoutDuplicatingTheTenantIndex()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        DomainProjectionHandlerResult first = await handler.ProjectAsync(request, "dispatch-duplicate", TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectedReadModels> detailBefore = await store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionIndexReadModel> indexBefore = await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionDispatchLedger> ledgerBefore = await store.GetAsync<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-duplicate"),
            TestContext.Current.CancellationToken);

        DomainProjectionHandlerResult duplicate = await handler.ProjectAsync(request, "dispatch-duplicate", TestContext.Current.CancellationToken);

        first.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        duplicate.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Count.ShouldBe(3);
        (await store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            TestContext.Current.CancellationToken)).ETag.ShouldBe(detailBefore.ETag);
        (await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken)).ETag.ShouldBe(indexBefore.ETag);
        (await store.GetAsync<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-duplicate"),
            TestContext.Current.CancellationToken)).ETag.ShouldBe(ledgerBefore.ETag);
        store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!
            .Summaries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SecondWriteFailureShouldBeRetryableAndRetryShouldConverge()
    {
        InMemoryReadModelStore inner = new();
        FailCompletedIndexSaveOnceReadModelStore store = new(inner);
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        DomainProjectionHandlerResult partial = await handler.ProjectAsync(request, "dispatch-partial", TestContext.Current.CancellationToken);

        partial.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        partial.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.PartialRetry);
        inner.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
        ConversationProjectionIndexReadModel? pendingIndex = inner.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));
        pendingIndex.ShouldNotBeNull();
        pendingIndex!.Summaries.ShouldBeEmpty();
        pendingIndex.Dispatches[Conversation.Value].DispatchId.ShouldBe("dispatch-partial");
        inner.Snapshot<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-partial"))!
            .Status.ShouldBe(ConversationProjectionDispatchStatus.Pending);
        ConversationProjectionReadStore readStore = new(inner);

        // The detail key advanced while the index summary did not, so this conversation cannot prove a
        // completed generation and the detail read fails closed.
        _ = await Should.ThrowAsync<ConversationProjectionConsistencyException>(
            async () => await readStore.ReadAsync(Tenant, Conversation, TestContext.Current.CancellationToken));

        // Listing does not fail closed for the whole tenant. The pending dispatch is reported so no caller can
        // claim the page is current, but a conversation mid-write must not make its unrelated siblings
        // unreadable.
        ConversationProjectionIndexSnapshot pendingSnapshot = await readStore.ListAsync(
            Tenant,
            TestContext.Current.CancellationToken);
        pendingSnapshot.HasIncompleteDispatch.ShouldBeTrue();
        pendingSnapshot.Summaries.ShouldBeEmpty();

        DomainProjectionHandlerResult retried = await handler.ProjectAsync(request, "dispatch-partial", TestContext.Current.CancellationToken);

        retried.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        inner.Count.ShouldBe(3);
        inner.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!
            .Summaries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UnavailableStoreShouldReturnIndeterminateWithoutRawStorageDetail()
    {
        ConversationAsyncProjectionHandler handler = Handler(new UnavailableReadModelStore());

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-unavailable",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Indeterminate);
        result.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
        result.ReasonCode!.ShouldNotContain("backend");
    }

    [Fact]
    public async Task CrossTenantEventShouldFailWithoutWritingEitherTenantScope()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        TenantId foreignTenant = new("tenant-foreign");

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation, Created(foreignTenant, Conversation)),
            "dispatch-foreign",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        result.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
        store.Count.ShouldBe(0);
    }

    [Fact]
    public async Task UnsupportedEmptyAndMalformedInputsShouldFailWithoutAnyWrite()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest unsupported = Request(Tenant, Conversation) with { Domain = "unsupported" };
        ProjectionRequest empty = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            []);
        ProjectionRequest malformed = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [new ProjectionEventDto("UnknownEvent", [0x7B, 0x7D], "json", 1, Started, "correlation")]);

        (await handler.ProjectAsync(unsupported, "dispatch-unsupported", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Failed);
        (await handler.ProjectAsync(empty, "dispatch-empty", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Failed);
        (await handler.ProjectAsync(malformed, "dispatch-malformed", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Failed);
        store.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PersistedRejectionShouldAdvancePositionWithoutMutatingOrWritingARejectionOnlyStream()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ConversationRejectedDomainEvent rejection = new(
            ConversationErrorCode.CommandValidationFailed,
            "invalid-command");
        ProjectionEventDto rejectionDto = new(
            nameof(ConversationRejectedDomainEvent),
            JsonSerializer.SerializeToUtf8Bytes(rejection, JsonOptions),
            "json",
            1,
            Started,
            "correlation");
        ProjectionRequest rejectionOnly = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [rejectionDto]);

        DomainProjectionHandlerResult noOp = await handler.ProjectAsync(
            rejectionOnly,
            "dispatch-rejection-only",
            TestContext.Current.CancellationToken);

        noOp.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Count.ShouldBe(0, "a rejection-only history has no conversation state to publish");

        ProjectionRequest mixed = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [EventDto(Created(Tenant, Conversation), 1), rejectionDto with { SequenceNumber = 2 }]);
        DomainProjectionHandlerResult projected = await handler.ProjectAsync(
            mixed,
            "dispatch-created-then-rejected",
            TestContext.Current.CancellationToken);

        projected.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ConversationProjectedReadModels models = store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
        models.Summary.Label.ShouldBe("Production path conversation");
        models.Summary.Freshness.LastAppliedEventPosition.ShouldBe(2);
    }

    [Fact]
    public async Task FullReplayPlanShouldContainExactGenerationAndLedgerKeysWithoutMutatingLiveState()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);

        DomainProjectionRebuildPlan plan = await handler.PrepareRebuildAsync(
            Request(Tenant, Conversation),
            "rebuild-001",
            TestContext.Current.CancellationToken);

        plan.StoreName.ShouldBe(ConversationProjectionReadModelKeys.StateStoreName);
        plan.Operations.Select(operation => operation.Key).ShouldBe(
        [
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            ConversationProjectionReadModelKeys.DispatchLedgerKey("rebuild-001"),
        ]);
        plan.Operations.ShouldAllBe(operation => operation.Kind == ReadModelBatchOperationKind.Write);
        plan.Operations[2].TimeToLive.ShouldBe(ProjectionDispatchOptions.DefaultRedeliveryWindow);
        store.Count.ShouldBe(0, "rebuild preparation must remain side-effect free until coordinated promotion");
    }

    [Fact]
    public async Task NonCurrentMaterializationShouldFailWithoutPublishingDispatchState()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest gap = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [EventDto(Created(Tenant, Conversation), sequence: 2)]);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            gap,
            "dispatch-gap",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        store.Count.ShouldBe(0);
    }

    [Fact]
    public async Task EmptyRebuildHistoryShouldBeRejectedAsTerminalInput()
    {
        ConversationAsyncProjectionHandler handler = Handler(new InMemoryReadModelStore());
        ProjectionRequest empty = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            []);

        DomainProjectionRebuildRejectedException exception = await Should.ThrowAsync<DomainProjectionRebuildRejectedException>(
            () => handler.PrepareRebuildAsync(empty, "rebuild-empty", TestContext.Current.CancellationToken));

        exception.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
    }

    [Fact]
    public async Task PopulatedRebuildPlanShouldRetainValidSiblingAndUsePersistedEtags()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        await handler.ProjectAsync(
            Request(Tenant, ConversationB()),
            "dispatch-sibling",
            TestContext.Current.CancellationToken);
        await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-existing",
            TestContext.Current.CancellationToken);

        ReadModelEntry<ConversationProjectionIndexReadModel> validIndex = await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken);
        TenantId foreignTenant = new("tenant-foreign");
        ConversationId foreignConversation = new("conversation-foreign");
        ConversationProjectedReadModels foreign = new ConversationProjectionMaterializer().Project(
            foreignTenant,
            foreignConversation,
            [new ConversationProjectionEventRecord(1, Created(foreignTenant, foreignConversation))],
            Started.AddSeconds(2),
            TimeSpan.FromMinutes(5)) with
        {
            DispatchId = "dispatch-foreign-index",
        };
        Dictionary<string, ConversationProjectionDispatchReference> corruptedDispatches = new(
            validIndex.Value!.Dispatches,
            StringComparer.Ordinal)
        {
            [foreignConversation.Value] = new("dispatch-foreign-index", 1),
        };
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Summaries = [.. validIndex.Value.Summaries, validIndex.Value.Summaries[0], foreign.Summary],
                Dispatches = corruptedDispatches,
            });

        ReadModelEntry<ConversationProjectedReadModels> detailBefore = await store.GetAsync<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            TestContext.Current.CancellationToken);
        ReadModelEntry<ConversationProjectionIndexReadModel> indexBefore = await store.GetAsync<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            TestContext.Current.CancellationToken);

        DomainProjectionRebuildPlan plan = await handler.PrepareRebuildAsync(
            Request(Tenant, Conversation),
            "rebuild-populated",
            TestContext.Current.CancellationToken);

        ReadModelBatchOperation detailOperation = plan.Operations[0];
        ReadModelBatchOperation indexOperation = plan.Operations[1];
        detailOperation.Concurrency.ExpectedETag.ShouldBe(detailBefore.ETag);
        indexOperation.Concurrency.ExpectedETag.ShouldBe(indexBefore.ETag);
        plan.Operations[2].Concurrency.ShouldBe(ReadModelBatchConcurrency.CreateOnly);

        ConversationProjectionIndexReadModel rebuiltIndex = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(
            indexOperation.CanonicalValue.Span,
            JsonOptions)!;
        rebuiltIndex.Summaries.Select(summary => summary.ConversationId.Value).ShouldBe(
            [Conversation.Value, ConversationB().Value],
            ignoreOrder: true);
        rebuiltIndex.Dispatches.Keys.ShouldBe(
            [Conversation.Value, ConversationB().Value],
            ignoreOrder: true);
    }

    /// <summary>A rebuild operation identity cannot be reused for another tenant or conversation.</summary>
    [Fact]
    public async Task RebuildShouldRejectAnExistingLedgerWithDifferentIdentity()
    {
        InMemoryReadModelStore store = new();
        await store.SaveAsync(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("rebuild-reused"),
            new ConversationProjectionDispatchLedger(
                "rebuild-reused",
                "different-fingerprint",
                Tenant,
                ConversationB(),
                Started,
                ConversationProjectionDispatchStatus.Completed),
            TestContext.Current.CancellationToken);
        int keysBefore = store.Count;

        DomainProjectionRebuildRejectedException exception = await Should.ThrowAsync<DomainProjectionRebuildRejectedException>(
            () => Handler(store).PrepareRebuildAsync(
                Request(Tenant, Conversation),
                "rebuild-reused",
                TestContext.Current.CancellationToken));

        exception.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.DeliveryIdentityConflict);
        store.Count.ShouldBe(keysBefore, "rebuild preparation must not overwrite a conflicting operation ledger.");
    }

    /// <summary>
    /// A sibling accepted just before its first summary write is represented only by a dispatch reference and
    /// ledger. Rebuilding another conversation must retain that in-flight evidence.
    /// </summary>
    [Fact]
    public async Task RebuildShouldPreserveASummarylessPendingSiblingReference()
    {
        InMemoryReadModelStore store = new();
        const string siblingDispatch = "dispatch-sibling-pending";
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
                {
                    [ConversationB().Value] = new(siblingDispatch, 1),
                },
            });
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey(siblingDispatch),
            new ConversationProjectionDispatchLedger(
                siblingDispatch,
                "sibling-fingerprint",
                Tenant,
                ConversationB(),
                Started,
                ConversationProjectionDispatchStatus.Pending));

        DomainProjectionRebuildPlan plan = await Handler(store).PrepareRebuildAsync(
            Request(Tenant, Conversation),
            "rebuild-with-pending-sibling",
            TestContext.Current.CancellationToken);
        ConversationProjectionIndexReadModel rebuilt = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(
            plan.Operations[1].CanonicalValue.Span,
            JsonOptions)!;

        rebuilt.Dispatches[ConversationB().Value].DispatchId.ShouldBe(siblingDispatch);
        rebuilt.Summaries.ShouldNotContain(summary => summary.ConversationId == ConversationB());
    }

    [Fact]
    public async Task RebuildShouldBulkReadSummarylessSiblingLedgers()
    {
        InMemoryReadModelStore inner = new();
        const string siblingDispatch = "dispatch-sibling-bulk";
        inner.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
                {
                    [ConversationB().Value] = new(siblingDispatch, 1, IsPending: true),
                },
            });
        inner.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey(siblingDispatch),
            new ConversationProjectionDispatchLedger(
                siblingDispatch,
                "sibling-fingerprint",
                Tenant,
                ConversationB(),
                Started,
                ConversationProjectionDispatchStatus.Pending));
        RejectSiblingScalarReadsStore store = new(inner, siblingDispatch);

        DomainProjectionRebuildPlan plan = await Handler(store).PrepareRebuildAsync(
            Request(Tenant, Conversation),
            "rebuild-bulk-siblings",
            TestContext.Current.CancellationToken);

        store.BulkCalls.ShouldBe(1);
        ConversationProjectionIndexReadModel rebuilt = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(
            plan.Operations[1].CanonicalValue.Span,
            JsonOptions)!;
        rebuilt.Dispatches.ShouldContainKey(ConversationB().Value);
    }

    [Fact]
    public async Task CompletionTimeoutShouldReturnRetryableAfterDurableModelWrites()
    {
        InMemoryReadModelStore inner = new();
        StallCompletionLedgerReadStore store = new(inner);
        var options = new ProjectionDispatchOptions
        {
            RetryLeaseDuration = TimeSpan.FromMilliseconds(25),
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
            RetryMaxDelay = TimeSpan.FromMilliseconds(1),
            RedeliveryWindow = TimeSpan.FromMinutes(1),
        };
        ConversationAsyncProjectionHandler handler = new(
            new ConversationProjectionMaterializer(),
            new ConversationProjectionReadModelWriter(store),
            store,
            TimeProvider.System,
            store,
            Options.Create(options));

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-completion-timeout",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        result.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.PartialRetry);
        inner.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation)).ShouldNotBeNull();
    }

    [Fact]
    public async Task CompletionShouldRejectAReplacedLedgerIdentityWithoutCompletingIt()
    {
        InMemoryReadModelStore inner = new();
        SubstituteCompletionLedgerStore store = new(inner);
        ConversationAsyncProjectionHandler handler = Handler(store);

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-replaced-ledger",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        store.SubstitutedLedger.ShouldNotBeNull();
        store.SubstitutedLedger!.Status.ShouldBe(ConversationProjectionDispatchStatus.Pending);
    }

    private static ConversationAsyncProjectionHandler Handler(IReadModelStore store)
        => new(
            new ConversationProjectionMaterializer(),
            new ConversationProjectionReadModelWriter(store),
            store,
            new FixedTimeProvider(Started.AddSeconds(2)));

    private static ProjectionRequest Request(
        TenantId tenantId,
        ConversationId conversationId,
        ConversationCreated? created = null)
    {
        ConversationCreated @event = created ?? Created(tenantId, conversationId);
        return new ProjectionRequest(
            tenantId.Value,
            ConversationProjectionHandler.ConversationDomain,
            conversationId.Value,
            [EventDto(@event, sequence: 1)]);
    }

    /// <summary>
    /// A completed ledger whose read-model keys are gone must not be answered from the ledger alone: the
    /// generation is re-persisted so completion continues to mean "a reader can observe this".
    /// </summary>
    /// <remarks>
    /// The ledger is a third key family, so derived-state deletion or a store rollback can outlive the two keys
    /// it describes. Returning Completed there would claim a durable generation nothing can read, and the
    /// platform would have no reason to redeliver.
    /// </remarks>
    [Fact]
    public async Task CompletedLedgerWithoutDurableKeysShouldRePersistInsteadOfReportingAFalseCompletion()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ProjectionRequest request = Request(Tenant, Conversation);

        (await handler.ProjectAsync(request, "dispatch-survivor", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);

        await EraseAsync(store, ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation));
        await EraseAsync(store, ConversationProjectionReadModelKeys.TenantIndexKey(Tenant));
        store.Snapshot<ConversationProjectionDispatchLedger>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-survivor"))
            .ShouldNotBeNull("the surviving completed ledger is the premise of this test.");

        DomainProjectionHandlerResult redelivered = await handler.ProjectAsync(
            request,
            "dispatch-survivor",
            TestContext.Current.CancellationToken);

        redelivered.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))
            .ShouldNotBeNull("completion must mean the detail key is durable, not that the ledger says so.");
        store.Snapshot<ConversationProjectionIndexReadModel>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))
            .ShouldNotBeNull("completion must mean the tenant index is durable, not that the ledger says so.");
    }

    /// <summary>
    /// A completed ledger is not a shortcut around payload verification: matching key identities with the
    /// wrong materialized content must be repaired from the fingerprint-bound request.
    /// </summary>
    [Fact]
    public async Task CompletedLedgerWithAConflictingGenerationShouldRePersistExpectedContent()
    {
        InMemoryReadModelStore store = new();
        ProjectionRequest request = Request(Tenant, Conversation);
        ConversationAsyncProjectionHandler handler = Handler(store);
        (await handler.ProjectAsync(request, "dispatch-conflicting", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ConversationProjectedReadModels corrupt = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [new ConversationProjectionEventRecord(1, Created(Tenant, Conversation) with { Label = "Corrupted generation" })],
            Started.AddSeconds(2),
            TimeSpan.FromMinutes(5)) with
        {
            DispatchId = "dispatch-conflicting",
        };
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            corrupt);
        store.SeedRaw(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel
            {
                Summaries = [corrupt.Summary],
                Dispatches = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal)
                {
                    [Conversation.Value] = new("dispatch-conflicting", 1),
                },
            });

        DomainProjectionHandlerResult redelivered = await handler.ProjectAsync(
            request,
            "dispatch-conflicting",
            TestContext.Current.CancellationToken);

        redelivered.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))!
            .Summary.Label.ShouldBe("Production path conversation");
    }

    /// <summary>
    /// Replaying a conversation whose newest event is far older than the staleness threshold still persists.
    /// </summary>
    /// <remarks>
    /// Staleness describes how far behind a projection is; it is a read-time trust signal, not a reason to
    /// refuse a write. Gating persistence on it made full replay impossible for any conversation idle longer
    /// than the threshold, and made any projection outage longer than the threshold unrecoverable — the exact
    /// operation full replay exists to perform.
    /// </remarks>
    [Fact]
    public async Task ReplayOfAConversationOlderThanTheStalenessThresholdShouldStillPersist()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = new(
            new ConversationProjectionMaterializer(),
            new ConversationProjectionReadModelWriter(store),
            store,
            new FixedTimeProvider(Started.AddDays(30)));

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            Request(Tenant, Conversation),
            "dispatch-old-replay",
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(
            ProjectionDispatchStatus.Completed,
            "a 30-day-old event slice must still be projectable; refusing it would make replay impossible.");
        ConversationProjectedReadModels? persisted = store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation));
        persisted.ShouldNotBeNull();

        // The generation is persisted and honestly labelled stale — it is not passed off as current.
        persisted!.Detail.Freshness.IsStale.ShouldBeTrue();
        persisted.Detail.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// A stable dispatch identity reused for genuinely different input is rejected without writing anything.
    /// </summary>
    [Fact]
    public async Task ReusingADispatchIdentityForDifferentInputShouldFailWithoutWriting()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);

        (await handler.ProjectAsync(Request(Tenant, Conversation), "dispatch-reused", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        int keysAfterFirst = store.Count;

        DomainProjectionHandlerResult reused = await handler.ProjectAsync(
            Request(Tenant, ConversationB()),
            "dispatch-reused",
            TestContext.Current.CancellationToken);

        reused.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        reused.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.HandlerFailure);
        store.Count.ShouldBe(keysAfterFirst, "a rejected identity reuse must not write any key.");
        store.Snapshot<ConversationProjectedReadModels>(
            ConversationProjectionReadModelKeys.StateStoreName,
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, ConversationB()))
            .ShouldBeNull();
    }

    /// <summary>
    /// Redelivering the same work under the same identity is accepted even when incidental per-delivery
    /// metadata differs, because the fingerprint binds what is projected rather than how it was delivered.
    /// </summary>
    [Fact]
    public async Task RedeliveryWithDifferentCorrelationMetadataShouldStillConverge()
    {
        InMemoryReadModelStore store = new();
        ConversationAsyncProjectionHandler handler = Handler(store);
        ConversationCreated @event = Created(Tenant, Conversation);
        ProjectionRequest first = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [EventDto(@event, sequence: 1)]);
        ProjectionRequest redelivered = new(
            Tenant.Value,
            ConversationProjectionHandler.ConversationDomain,
            Conversation.Value,
            [
                new ProjectionEventDto(
                    nameof(ConversationCreated),
                    JsonSerializer.SerializeToUtf8Bytes(@event, JsonOptions),
                    "json",
                    1,
                    Started.AddMinutes(7),
                    "correlation-redelivered",
                    MessageId: "message-redelivered",
                    UserId: "user-redelivered",
                    GlobalPosition: 4242),
            ]);

        (await handler.ProjectAsync(first, "dispatch-redelivered", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);

        DomainProjectionHandlerResult second = await handler.ProjectAsync(
            redelivered,
            "dispatch-redelivered",
            TestContext.Current.CancellationToken);

        second.Status.ShouldBe(
            ProjectionDispatchStatus.Completed,
            "correlation, message, user and global-position values vary between deliveries of the same work; "
            + "treating them as identity reuse would fail a benign redelivery terminally.");
    }

    private static async Task EraseAsync(InMemoryReadModelStore store, string key)
    {
        (bool present, string etag) = await store.TryReadEtagAsync(
            ConversationProjectionReadModelKeys.StateStoreName,
            key,
            TestContext.Current.CancellationToken);
        present.ShouldBeTrue(key);
        (await store.TryEraseAsync(
            ConversationProjectionReadModelKeys.StateStoreName,
            key,
            etag,
            TestContext.Current.CancellationToken)).ShouldBeTrue(key);
    }

    private static ProjectionEventDto EventDto(ConversationCreated @event, long sequence)
        => new(
            nameof(ConversationCreated),
            JsonSerializer.SerializeToUtf8Bytes(@event, JsonOptions),
            "json",
            sequence,
            Started,
            "correlation-001");

    private static ConversationId ConversationB() => new("conversation-002");

    private static ConversationCreated Created(TenantId tenantId, ConversationId conversationId)
        => new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-created-001",
                ConversationEventType.ConversationCreated,
                tenantId,
                conversationId,
                "correlation-001",
                Started,
                Actor,
                "causation-001"),
            Label: "Production path conversation");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FailCompletedIndexSaveOnceReadModelStore(InMemoryReadModelStore inner) : IReadModelStore, IReadModelExpiringStore
    {
        private int _failed;

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.GetAsync<TValue>(storeName, key, cancellationToken);

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            if (value is ConversationProjectionIndexReadModel { Summaries.Count: > 0 }
                && Interlocked.Exchange(ref _failed, 1) == 0)
            {
                throw new InvalidOperationException("injected completed-index failure");
            }

            return inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
        }

        public Task<bool> TrySaveWithTimeToLiveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }

    private sealed class RejectSiblingScalarReadsStore(
        InMemoryReadModelStore inner,
        string siblingDispatchId) : IReadModelStore, IReadModelBulkStore
    {
        public int BulkCalls { get; private set; }

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            if (typeof(TValue) == typeof(ConversationProjectionDispatchLedger)
                && string.Equals(
                    key,
                    ConversationProjectionReadModelKeys.DispatchLedgerKey(siblingDispatchId),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Sibling ledgers must be read through the bounded bulk API.");
            }

            return inner.GetAsync<TValue>(storeName, key, cancellationToken);
        }

        public Task<IReadOnlyList<ReadModelBulkEntry<TValue>>> GetManyAsync<TValue>(
            string storeName,
            IReadOnlyList<string> keys,
            int parallelism,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            BulkCalls++;
            return inner.GetManyAsync<TValue>(storeName, keys, parallelism, cancellationToken);
        }

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);
    }

    private sealed class StallCompletionLedgerReadStore(InMemoryReadModelStore inner) : IReadModelStore, IReadModelExpiringStore
    {
        private int _ledgerReads;

        public async Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            if (typeof(TValue) == typeof(ConversationProjectionDispatchLedger)
                && Interlocked.Increment(ref _ledgerReads) > 1)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return await inner.GetAsync<TValue>(storeName, key, cancellationToken);
        }

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);

        public Task<bool> TrySaveWithTimeToLiveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }

    private sealed class SubstituteCompletionLedgerStore(InMemoryReadModelStore inner) : IReadModelStore, IReadModelExpiringStore
    {
        private int _ledgerReads;

        public ConversationProjectionDispatchLedger? SubstitutedLedger { get; private set; }

        public async Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            if (typeof(TValue) == typeof(ConversationProjectionDispatchLedger)
                && Interlocked.Increment(ref _ledgerReads) > 1)
            {
                SubstitutedLedger = new ConversationProjectionDispatchLedger(
                    "alien-dispatch",
                    "alien-fingerprint",
                    Tenant,
                    Conversation,
                    Started.AddHours(1),
                    ConversationProjectionDispatchStatus.Pending);
                inner.SeedRaw(storeName, key, SubstitutedLedger);
            }

            return await inner.GetAsync<TValue>(storeName, key, cancellationToken);
        }

        public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
            where TValue : class
            => inner.SaveAsync(storeName, key, value, cancellationToken);

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveAsync(storeName, key, value, etag, cancellationToken);

        public Task<bool> TrySaveWithTimeToLiveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default)
            where TValue : class
            => inner.TrySaveWithTimeToLiveAsync(storeName, key, value, etag, timeToLive, cancellationToken);
    }

    private sealed class UnavailableReadModelStore : IReadModelStore
    {
        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
            => throw new IOException("backend storage detail");

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
            => throw new IOException("backend storage detail");

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
            => throw new IOException("backend storage detail");
    }
}
