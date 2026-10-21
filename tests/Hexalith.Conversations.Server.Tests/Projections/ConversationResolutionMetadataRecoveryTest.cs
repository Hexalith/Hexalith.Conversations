using System.Text.Json;
using System.Text;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Projections;

public sealed class ConversationResolutionMetadataRecoveryTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static TenantId Tenant => ResolutionMetadataFixture.Tenant;
    private static ConversationId Conversation => ResolutionMetadataFixture.Conversation;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DistinctSecondCreationCannotReplaceOrRestoreClassificationAndOnlyPersistsObservedBarrier(bool afterLabelUpdate)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ProjectionRequest request = ResolutionMetadataFixture.Request();
        var created = JsonSerializer.Deserialize<ConversationCreated>(request.Events[0].Payload, JsonSerializerOptions.Web)!;
        List<ProjectionEventDto> events = [.. request.Events];
        if (afterLabelUpdate)
        {
            ConversationMetadataUpdated update = new(created.Metadata with
            {
                EventId = "event-descriptive-update", EventType = ConversationEventType.ConversationMetadataUpdated,
                OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1),
            }, Label: "changed label");
            events.Add(new(nameof(ConversationMetadataUpdated), JsonSerializer.SerializeToUtf8Bytes(update, JsonSerializerOptions.Web),
                "json", 2, ResolutionMetadataFixture.Started.AddSeconds(1), "correlation", "source-message-2"));
        }
        long position = events.Count + 1;
        ConversationCreated duplicate = created with
        {
            Metadata = created.Metadata with { EventId = "distinct-second-create", OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(position - 1) },
            Label = afterLabelUpdate ? "changed label" : created.Label,
            ResolutionScoringLabel = new(afterLabelUpdate ? "changed label" : created.Label,
                ConversationResolutionScoringPolicy.Purpose, ConversationResolutionScoringPolicy.Classification),
        };
        events.Add(new(nameof(ConversationCreated), JsonSerializer.SerializeToUtf8Bytes(duplicate, JsonSerializerOptions.Web),
            "json", position, ResolutionMetadataFixture.Started.AddSeconds(position - 1), "correlation", "source-second-create"));
        await AssertRejectedClassificationPreservesGenerationAsync(fixture, request with { Events = [.. events] });
    }

    [Theory]
    [InlineData("unsafe-value")]
    [InlineData("oversized-value")]
    [InlineData("unsupported-purpose")]
    [InlineData("unsupported-classification")]
    [InlineData("creation-mismatch")]
    [InlineData("creation-null-mismatch")]
    [InlineData("update-mismatch")]
    [InlineData("update-null-mismatch")]
    public async Task InvalidSerializedClassificationOrNativeLabelMismatchPreservesPriorGenerationAndObservedBarrier(string failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ProjectionRequest request = ResolutionMetadataFixture.Request(changedProject: true);
        if (failure.StartsWith("update-", StringComparison.Ordinal))
        {
            var created = JsonSerializer.Deserialize<ConversationCreated>(request.Events[0].Payload, JsonSerializerOptions.Web)!;
            ConversationMetadataUpdated updated = new(created.Metadata with
            {
                EventId = "invalid-classified-update", EventType = ConversationEventType.ConversationMetadataUpdated,
                OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1),
            }, Label: "new label")
            {
                ResolutionScoringLabel = new(failure == "update-null-mismatch" ? null : "different label",
                    ConversationResolutionScoringPolicy.Purpose, ConversationResolutionScoringPolicy.Classification),
            };
            request = request with { Events = [request.Events[0], new(nameof(ConversationMetadataUpdated),
                JsonSerializer.SerializeToUtf8Bytes(updated, JsonSerializerOptions.Web), "json", 2,
                ResolutionMetadataFixture.Started.AddSeconds(1), "correlation", "source-message-2")] };
        }
        else
        {
            JsonObject created = JsonNode.Parse(request.Events[0].Payload)!.AsObject();
            JsonNode classification = created["resolutionScoringLabel"]!;
            if (failure == "unsupported-purpose") classification["purpose"] = "inspection";
            else if (failure == "unsupported-classification") classification["classification"] = "caller-asserted";
            else classification["value"] = failure switch
            {
                "unsafe-value" => "secret value",
                "oversized-value" => new string('a', 161),
                "creation-null-mismatch" => null,
                _ => "different label",
            };
            request = request with { Events = [request.Events[0] with
                { Payload = JsonSerializer.SerializeToUtf8Bytes(created, JsonSerializerOptions.Web) }, request.Events[1]] };
        }
        await AssertRejectedClassificationPreservesGenerationAsync(fixture, request);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CompletedRedeliveryVerifiesAndRepairsMissingOrAlteredNativeWrapperWithoutBorrowingCompletion(bool altered, bool interruptRepair)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var originalMetadata = fixture.Metadata;
        var originalGeneration = fixture.Proof.Current;
        string detailKey = ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation);
        var detail = fixture.Store.Snapshot<ConversationProjectedReadModels>("statestore", detailKey)!;
        fixture.Store.SeedRaw("statestore", detailKey, detail with
        {
            ResolutionScoringLabel = altered ? new("altered label", ConversationResolutionScoringPolicy.Purpose,
                ConversationResolutionScoringPolicy.Classification) : null,
        });
        bool reachedRepair = false;
        fixture.Guard.BeforeSave = (type, _) =>
        {
            if (type == typeof(ConversationProjectedReadModels))
            {
                reachedRepair = true;
                if (interruptRepair) throw new IOException("interrupt native wrapper repair");
            }
            return Task.CompletedTask;
        };
        var first = await fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token);
        reachedRepair.ShouldBeTrue();
        if (interruptRepair)
        {
            first.Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
            fixture.Proof.Current!.IsPending.ShouldBeTrue();
            fixture.Metadata.ShouldBe(originalMetadata);
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
            fixture.Guard.BeforeSave = null;
            (await fixture.Handler(fixture.Guard).ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token))
                .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        }
        else first.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Store.Snapshot<ConversationProjectedReadModels>("statestore", detailKey)!.ResolutionScoringLabel.ShouldBe(detail.ResolutionScoringLabel);
        fixture.Metadata.ShouldBe(originalMetadata);
        fixture.Proof.Current.ShouldBe(originalGeneration);
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PriorImplicitFormatRefusesRedeliveryWithoutChangingStoredBytesAndLaterNativeGenerationRecovers(bool expiredLedger)
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest request = ResolutionMetadataFixture.Request(classifyLabel: false);
        (await fixture.Handler().ProjectAsync(request, "dispatch-implicit", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var implicitMetadata = fixture.Metadata with
        {
            Metadata = fixture.Metadata.Metadata with { SafeLabel = "Scoring label" }, IsLabelComplete = true,
            LabelClassification = ConversationResolutionScoringPolicy.Classification, LabelPurpose = null,
        };
        var implicitGeneration = ConversationResolutionGeneration.From(implicitMetadata, false);
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation), implicitMetadata);
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation), new ConversationResolutionMetadataProof(implicitGeneration));
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionFenceKey(Tenant, Conversation), new ConversationResolutionMetadataProof(implicitGeneration));
        if (expiredLedger) await fixture.EraseAsync(ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-implicit"));
        var before = await CaptureStoredGenerationAsync(fixture, "dispatch-implicit");
        int count = fixture.Store.Count;
        fixture.Guard.ForbidWrites = true;
        var refused = await fixture.Handler(fixture.Guard).ProjectAsync(request, "dispatch-implicit", Token);
        refused.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        refused.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.UnsupportedCapability);
        var rebuildRefusal = await Should.ThrowAsync<DomainProjectionRebuildRejectedException>(() =>
            fixture.Handler(fixture.Guard).PrepareRebuildAsync(request, "dispatch-implicit", Token));
        rebuildRefusal.ReasonCode.ShouldBe(ProjectionDispatchReasonCodes.UnsupportedCapability);
        (await CaptureStoredGenerationAsync(fixture, "dispatch-implicit")).ShouldBe(before);
        fixture.Store.Count.ShouldBe(count);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
        fixture.Proof.Barrier.ShouldBeNull();
        var unavailable = await fixture.DispatchAsync();
        unavailable.GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);

        fixture.Guard.ForbidWrites = false;
        fixture.Guard.ForbidLegacyReads = false;
        var created = JsonSerializer.Deserialize<ConversationCreated>(request.Events[0].Payload, JsonSerializerOptions.Web)!;
        ConversationMetadataUpdated classified = new(created.Metadata with
        {
            EventId = "supported-native-classification", EventType = ConversationEventType.ConversationMetadataUpdated,
            OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1),
        }, Label: created.Label)
        { ResolutionScoringLabel = new(created.Label, ConversationResolutionScoringPolicy.Purpose, ConversationResolutionScoringPolicy.Classification) };
        var next = request with { Events = [.. request.Events, new(nameof(ConversationMetadataUpdated),
            JsonSerializer.SerializeToUtf8Bytes(classified, JsonSerializerOptions.Web), "json", 2,
            ResolutionMetadataFixture.Started.AddSeconds(1), "correlation", "source-message-2")] };
        next = await ResolutionMetadataFixture.AdmitNativeAsync(next);
        (await fixture.Handler().ProjectAsync(next, "dispatch-native", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.LabelPurpose.ShouldBe(ConversationResolutionScoringPolicy.Purpose);
        fixture.Metadata.Metadata.SafeLabel.ShouldBe("Scoring label");
        fixture.Proof.PreviousCompleted.ShouldBe(implicitGeneration);
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
        next.Events[0].Payload.ShouldBe(request.Events[0].Payload);
    }

    private static async Task AssertRejectedClassificationPreservesGenerationAsync(ResolutionMetadataFixture fixture, ProjectionRequest invalid)
    {
        var originalMetadata = fixture.Metadata;
        var originalProof = fixture.Proof;
        var before = await CaptureStoredGenerationAsync(fixture, "dispatch-a");
        int count = fixture.Store.Count;
        var rejected = await fixture.Handler(fixture.Guard).ProjectAsync(invalid, "dispatch-invalid-classification", Token);
        rejected.Status.ShouldBe(ProjectionDispatchStatus.Failed);
        fixture.Metadata.ShouldBe(originalMetadata);
        fixture.Proof.Current.ShouldBe(originalProof.Current);
        fixture.Proof.PreviousCompleted.ShouldBe(originalProof.PreviousCompleted);
        fixture.Proof.Barrier.ShouldBe(new(invalid.Events.Max(item => item.SequenceNumber), "dispatch-invalid-classification"));
        var after = await CaptureStoredGenerationAsync(fixture, "dispatch-a");
        foreach (var pair in before.Where(pair => pair.Key != ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation)))
            after[pair.Key].ShouldBe(pair.Value);
        fixture.Store.Count.ShouldBe(count);
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        var unavailable = await fixture.DispatchAsync();
        unavailable.GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private static async Task<Dictionary<string, (string Json, bool Exists, string ETag)>> CaptureStoredGenerationAsync(
        ResolutionMetadataFixture fixture, string dispatchId)
    {
        string[] keys = [ConversationProjectionReadModelKeys.ResolutionMetadataKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.ResolutionFenceKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation),
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant), ConversationProjectionReadModelKeys.DispatchLedgerKey(dispatchId)];
        Dictionary<string, (string Json, bool Exists, string ETag)> snapshot = new(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            var observed = await fixture.Store.TryReadEtagAsync("statestore", key, Token);
            snapshot[key] = (fixture.Store.Snapshot<JsonObject>("statestore", key)?.ToJsonString() ?? "null", observed.Present, observed.Etag);
        }
        return snapshot;
    }

    [Fact]
    public async Task LargeValidTranscriptUsesLegacyCasAndOnlyBoundedMetadataRecoveryManifests()
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest request = ResolutionMetadataFixture.Request();
        ConversationCreated created = JsonSerializer.Deserialize<ConversationCreated>(request.Events[0].Payload, JsonSerializerOptions.Web)!;
        string transcript = new('x', 1_100_000);
        MessageAppended appended = new(created.Metadata with { EventType = ConversationEventType.MessageAppended,
            EventId = "message-event", OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1) },
            new("message-large"), new("actor"), transcript);
        request = request with { Events = [.. request.Events, new(nameof(MessageAppended),
            JsonSerializer.SerializeToUtf8Bytes(appended, JsonSerializerOptions.Web), "json", 2,
            ResolutionMetadataFixture.Started.AddSeconds(1), "correlation")] };
        int batches = 0;
        fixture.Guard.BeforeBatch = batch =>
        {
            batches++;
            batch.Scope.ProjectionType.ShouldBe("resolution-metadata");
            batch.Operations.ShouldAllBe(operation => operation.Key.Contains("conversations-resolution", StringComparison.Ordinal));
            batch.Operations.Sum(operation => operation.CanonicalValue.Length).ShouldBeLessThan(16_384);
            return Task.CompletedTask;
        };
        (await fixture.Handler(fixture.Guard).ProjectAsync(request, "dispatch-large", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        batches.ShouldBe(1);
        var detail = fixture.Store.Snapshot<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))!;
        JsonSerializer.SerializeToUtf8Bytes(detail).Length.ShouldBeGreaterThan(1_048_576);
        JsonSerializer.Serialize(detail).ShouldContain(transcript);
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
    }

    [Fact]
    public async Task GrowingTenantIndexBeyondManifestLimitDoesNotLimitLiveProjectionOrCoupleSiblingRecovery()
    {
        ResolutionMetadataFixture fixture = new();
        // These are valid previously persisted summaries. Every new production delivery must preserve them.
        ProjectionRequest seed = ResolutionMetadataFixture.Request();
        var models = new ConversationProjectionMaterializer().Project(Tenant, Conversation,
            [new(1, JsonSerializer.Deserialize<ConversationCreated>(seed.Events[0].Payload, JsonSerializerOptions.Web)!)],
            fixture.Time.Now, TimeSpan.FromMinutes(5));
        ConversationSummaryProjectionV1[] summaries = [.. Enumerable.Range(0, 1_500).Select(index => new ConversationSummaryProjectionV1(SchemaVersion.Current, Tenant, new($"existing-{index:D4}"),
            models.Summary.Freshness, models.Summary.LifecycleState, new('a', 160)))];
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.TenantIndexKey(Tenant),
            new ConversationProjectionIndexReadModel { Summaries = summaries });
        ConversationId sibling = new("conversation-sibling");
        bool interrupted = false;
        fixture.Store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (!interrupted && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 0)
            {
                interrupted = true;
                throw new IOException("interrupted first Conversation metadata batch");
            }
            return Task.CompletedTask;
        };
        (await fixture.Handler().ProjectAsync(seed, "dispatch-first-interrupted", Token)).Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        fixture.Store.BatchFaultHook = null;
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(conversationId: sibling), "dispatch-sibling", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, sibling, Token))!.ConversationId.ShouldBe(sibling);
        (await fixture.Handler().ProjectAsync(seed, "dispatch-first-interrupted", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var index = fixture.Store.Snapshot<ConversationProjectionIndexReadModel>("statestore",
            ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!;
        index.Summaries.Count.ShouldBe(1_502);
        JsonSerializer.SerializeToUtf8Bytes(index).Length.ShouldBeGreaterThan(1_048_576);
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(fixture.Metadata);
    }

    [Theory]
    [InlineData("decode")]
    [InlineData("gap")]
    [InlineData("schema")]
    [InlineData("materialization")]
    public async Task RejectedNewerInputPersistsOnlyObservedBarrierWhileOlderInvalidInputCannotPoisonWinner(string failure)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        ProjectionRequest invalid = ResolutionMetadataFixture.Request(changedProject: true);
        invalid = failure switch
        {
            "decode" => invalid with { Events = [.. invalid.Events[..1], invalid.Events[1] with { Payload = "{"u8.ToArray() }] },
            "gap" => invalid with { Events = [invalid.Events[0], invalid.Events[1] with { SequenceNumber = 3 }] },
            "schema" => invalid with { Events = [invalid.Events[0], invalid.Events[1] with
                { Payload = JsonSerializer.SerializeToUtf8Bytes(JsonSerializer.Deserialize<ConversationProjectChanged>(invalid.Events[1].Payload, JsonSerializerOptions.Web)!
                    with { Metadata = JsonSerializer.Deserialize<ConversationProjectChanged>(invalid.Events[1].Payload, JsonSerializerOptions.Web)!.Metadata with { SchemaVersion = new(2) } }, JsonSerializerOptions.Web) }] },
            _ => invalid with { Events = [invalid.Events[0], invalid.Events[1] with
                { Payload = JsonSerializer.SerializeToUtf8Bytes(JsonSerializer.Deserialize<ConversationProjectChanged>(invalid.Events[1].Payload, JsonSerializerOptions.Web)!
                    with { Metadata = JsonSerializer.Deserialize<ConversationProjectChanged>(invalid.Events[1].Payload, JsonSerializerOptions.Web)!.Metadata with { ConversationId = new("wrong-resource") } }, JsonSerializerOptions.Web) }] },
        };
        (await fixture.Handler().ProjectAsync(invalid, "dispatch-invalid", Token)).Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        fixture.Proof.Barrier.ShouldNotBeNull();
        fixture.Proof.Barrier.Position.ShouldBe(invalid.Events.Max(item => item.SequenceNumber));
        fixture.Proof.Current!.Freshness.ShouldBe(original.Freshness);
        fixture.Metadata.ShouldBe(original);
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        var unavailable = await fixture.DispatchAsync();
        unavailable.Success.ShouldBeTrue();
        unavailable.GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);

        fixture.Guard.ForbidWrites = false;
        fixture.Guard.ForbidLegacyReads = false;
        ProjectionRequest valid = ResolutionMetadataFixture.Request(changedProject: true);
        if (failure == "gap")
        {
            // Supply a contiguous position three to cover the observed gap barrier.
            var source = JsonSerializer.Deserialize<ConversationProjectChanged>(valid.Events[1].Payload, JsonSerializerOptions.Web)!;
            ConversationMetadataUpdated updated = new(source.Metadata with { EventId = "third-event",
                EventType = ConversationEventType.ConversationMetadataUpdated, OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(2) }, "Scoring label");
            valid = valid with { Events = [.. valid.Events, new(nameof(ConversationMetadataUpdated),
                JsonSerializer.SerializeToUtf8Bytes(updated, JsonSerializerOptions.Web), "json", 3,
                ResolutionMetadataFixture.Started.AddSeconds(2), "correlation", "source-message-3")] };
            valid = await ResolutionMetadataFixture.AdmitNativeAsync(valid);
        }
        (await fixture.Handler().ProjectAsync(valid, "dispatch-z-winner", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var winner = fixture.Metadata;
        ProjectionRequest old = ResolutionMetadataFixture.Request() with
        { Events = [ResolutionMetadataFixture.Request().Events[0] with { Payload = "{"u8.ToArray() }] };
        (await fixture.Handler().ProjectAsync(old, "dispatch-old-invalid", Token)).Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        fixture.Proof.Barrier.ShouldBeNull();
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(winner);
    }

    [Fact]
    public async Task PublicWriterInvalidatesCompletedProofBeforeTouchingRichComponentsAndOlderWriterDoesNotPoisonNewerProof()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        ProjectionRequest request = ResolutionMetadataFixture.Request(changedProject: true);
        var plan = await fixture.Handler().PrepareRebuildAsync(request, "dispatch-direct", Token);
        var detailOperation = plan.Operations.Single(operation => operation.Key == ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation));
        var direct = JsonSerializer.Deserialize<ConversationProjectedReadModels>(detailOperation.CanonicalValue.Span, JsonSerializerOptions.Web)!;
        fixture.Guard.BeforeSave = async (type, _) =>
        {
            if (type == typeof(ConversationProjectedReadModels))
            {
                fixture.Proof.Barrier.ShouldNotBeNull();
                await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                    await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
            }
        };
        await new ConversationProjectionReadModelWriter(fixture.Guard).PersistAsync(direct, Token);
        fixture.Metadata.ShouldBe(original);
        fixture.Proof.Barrier.ShouldNotBeNull();
        await fixture.ProjectAsync("dispatch-z-live", changedProject: true);
        var winner = fixture.Metadata;
        var old = direct with { DispatchId = "dispatch-a-loser" };
        await new ConversationProjectionReadModelWriter(fixture.Store).PersistAsync(old, Token);
        fixture.Proof.Barrier.ShouldBeNull();
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(winner);
    }

    [Theory]
    [InlineData("components")]
    [InlineData("metadata")]
    [InlineData("completion")]
    public async Task OverlappingEqualPositionDispatchesConvergeOnOrdinalWinnerEvenWhenLoserResumesLast(string boundary)
    {
        ResolutionMetadataFixture fixture = new();
        TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Guard.BeforeSave = async (type, _) =>
        {
            if (boundary == "components" && type == typeof(ConversationProjectedReadModels))
            {
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        fixture.Guard.BeforeBatch = async _ =>
        {
            if (boundary == "metadata")
            {
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        fixture.Guard.BeforeKeySave = async (key, value) =>
        {
            if (boundary == "completion" && key == ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation)
                && value is ConversationResolutionMetadataProof { Current.IsPending: false })
            {
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task<DomainProjectionHandlerResult> loser = fixture.Handler(fixture.Guard)
            .ProjectAsync(ResolutionMetadataFixture.Request(label: "loser"), "dispatch-a", Token);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        await fixture.ProjectAsync("dispatch-z", label: "winner");
        resume.TrySetResult();
        var result = await loser;
        result.Status.ShouldBeOneOf(ProjectionDispatchStatus.Completed, ProjectionDispatchStatus.Retryable);
        fixture.Proof.Current!.DispatchId.ShouldBe("dispatch-z");
        fixture.Proof.Current.IsPending.ShouldBeFalse();
        fixture.Metadata.Metadata.SafeLabel.ShouldBe("winner");
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(fixture.Metadata);
        var detail = fixture.Store.Snapshot<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))!;
        detail.DispatchId.ShouldBe("dispatch-z");
    }

    [Fact]
    public async Task PausedDirectWriterCannotReopenSameGenerationProofAndStrictlyNewerRetryConverges()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var plan = await fixture.Handler().PrepareRebuildAsync(ResolutionMetadataFixture.Request(changedProject: true), "dispatch-direct", Token);
        var operation = plan.Operations.Single(item => item.Key == ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation));
        var direct = JsonSerializer.Deserialize<ConversationProjectedReadModels>(operation.CanonicalValue.Span, JsonSerializerOptions.Web)!;
        // Legacy CAS permits an equal tuple, so these components could differ from a producer's same-tuple replay.
        direct = new(direct.Summary with { Label = "direct label" }, direct.Detail) { DispatchId = direct.DispatchId };
        TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Guard.BeforeSave = async (type, _) =>
        {
            if (type == typeof(ConversationProjectedReadModels))
            {
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task paused = new ConversationProjectionReadModelWriter(fixture.Guard).PersistAsync(direct, Token);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        var same = await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(changedProject: true), "dispatch-direct", Token);
        same.Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        fixture.Proof.Barrier.ShouldNotBeNull();
        fixture.Proof.Barrier.RequiresNewerGeneration.ShouldBeTrue();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        resume.TrySetResult();
        await paused;
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        await fixture.ProjectAsync("dispatch-z-newer", changedProject: true);
        fixture.Proof.Barrier.ShouldBeNull();
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token))!.Metadata.SafeLabel.ShouldBe("Scoring label");
        fixture.Store.Snapshot<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))!.Summary.Label.ShouldBe("Scoring label");
    }

    [Fact]
    public async Task TerminalLegacyIndexCompensationKeepsOuterProofPendingWhileLiveRichWriterCanResume()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var previous = fixture.Metadata;
        TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Guard.BeforeSave = async (type, _) =>
        {
            if (type == typeof(ConversationProjectedReadModels))
            {
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task<DomainProjectionHandlerResult> writer = fixture.Handler(fixture.Guard)
            .ProjectAsync(ResolutionMetadataFixture.Request(changedProject: true), "dispatch-paused-live", Token);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        (await new ConversationProjectionReadModelWriter(fixture.Store)
            .TryReconcileTerminalDispatchAsync(Tenant, Conversation, "dispatch-paused-live", Token)).ShouldBeTrue();
        (await new ConversationResolutionMetadataPersistence(fixture.Store)
            .TryRestoreTerminalAsync(Tenant, Conversation, "dispatch-paused-live", Token)).ShouldBeFalse();
        fixture.Metadata.ShouldBe(previous);
        fixture.Proof.Current!.IsPending.ShouldBeTrue();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        resume.TrySetResult();
        (await writer).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token))!.Metadata.ProjectId.ShouldBe("project-new");
    }

    public static IEnumerable<object[]> RebuildFaultBoundaries()
    {
        foreach (ReadModelBatchPhase phase in new[] { ReadModelBatchPhase.BeforeInstallOperation,
            ReadModelBatchPhase.AfterInstallOperation, ReadModelBatchPhase.BeforeCompaction, ReadModelBatchPhase.AfterCompaction })
            for (int ordinal = 0; ordinal < 6; ordinal++) yield return [phase, ordinal];
        foreach (ReadModelBatchPhase phase in new[] { ReadModelBatchPhase.MarkerPrepared,
            ReadModelBatchPhase.BeforeCommit, ReadModelBatchPhase.AfterCommit, ReadModelBatchPhase.BeforeReceipt })
            yield return [phase, -1];
    }

    [Theory]
    [MemberData(nameof(RebuildFaultBoundaries))]
    public async Task SdkProducedSixOperationImmutablePlanResumesCoherentlyAtEveryInstallCommitAndCompactionBoundary(
        ReadModelBatchPhase phase, int ordinal)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ReadModelBatch? produced = null;
        fixture.Guard.BeforeBatch = batch =>
        {
            produced = batch;
            batch.Operations.Count.ShouldBe(7);
            batch.RetainManifestForRecovery.ShouldBeFalse();
            return Task.CompletedTask;
        };
        bool faulted = false;
        fixture.Store.BatchFaultHook = (observed, index, _) =>
        {
            if (!faulted && observed == phase && (ordinal < 0 || index == ordinal))
            {
                faulted = true;
                throw new IOException("six-operation rebuild interruption");
            }
            return Task.CompletedTask;
        };
        _ = await RebuildAsync(fixture);
        faulted.ShouldBeTrue();
        var during = await fixture.DispatchAsync();
        if (during.Success && during.GetPayload().GetProperty("metadata").ValueKind != JsonValueKind.Null)
        {
            string project = during.GetPayload().GetProperty("metadata").GetProperty("projectId").GetString()!;
            project.ShouldBeOneOf("project-original", "project-new");
        }
        fixture.Store.BatchFaultHook = null;
        // The SDK retries its immutable produced plan with original ETags after process/store-adapter restart.
        // Replanning a batch against pending envelopes creates different ETags and is not native recovery.
        var restarted = new ResolutionGuardedStore(fixture.Store);
        var originalGeneratedAt = fixture.Time.Now;
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await restarted.ExecuteAsync(produced!, Token)).IsSuccess.ShouldBeTrue();
        var stable = await fixture.DispatchAsync();
        stable.Success.ShouldBeTrue();
        stable.GetPayload().GetProperty("metadata").GetProperty("projectId").GetString().ShouldBe("project-new");
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
        fixture.Metadata.Freshness.ProjectionGeneratedAt.ShouldBe(originalGeneratedAt);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FreshProductionDispatcherRetryWithChangedClockWithholdsPartialRebuildEvidence(bool hasPriorGeneration)
    {
        ResolutionMetadataFixture fixture = new();
        if (hasPriorGeneration) await fixture.ProjectAsync();
        ConversationResolutionMetadataReadModel? previous = hasPriorGeneration ? fixture.Metadata : null;
        bool faulted = false;
        fixture.Store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (!faulted && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 0)
            {
                faulted = true;
                throw new InvalidOperationException("production rebuild interrupted after first install");
            }
            return Task.CompletedTask;
        };
        (await RebuildAsync(fixture)).Outcomes.ShouldAllBe(outcome => outcome.Status != ProjectionDispatchStatus.Completed);
        faulted.ShouldBeTrue();
        fixture.Store.BatchFaultHook = null;
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        // This is an actual fresh native dispatcher/provider call using only the request and dispatch ID.
        // Native default rebuild markers do not retain the original plan in legacy-handler compositions.
        var retry = await RebuildAsync(fixture);
        retry.Outcomes.ShouldAllBe(outcome => outcome.Status == ProjectionDispatchStatus.Failed
            && outcome.ReasonCode == ProjectionDispatchReasonCodes.HandlerFailure);
        var query = await fixture.DispatchAsync();
        if (previous is null)
        {
            query.Success.ShouldBeFalse();
            query.ErrorMessage.ShouldBe("safe-denial");
        }
        else
        {
            query.Success.ShouldBeTrue();
            query.GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);
            fixture.Metadata.ShouldBe(previous);
            var proof = await fixture.Store.GetAsync<ConversationResolutionMetadataProof>("statestore",
                ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation), Token);
            proof.ETag.ShouldBeNullOrEmpty(); // Native prepared envelope, never a verified plain completed gate.
            proof.Value!.Current!.Matches(previous).ShouldBeTrue();
        }
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LongOpaqueOwnerIdsRetainExactModelsWithBoundedDistinctNativeMetadataTokens(bool unicode)
    {
        ResolutionMetadataFixture fixture = new();
        TenantId tenant = new(new string(unicode ? '界' : 't', unicode ? 256 : 4096));
        ConversationId conversation = new(new string(unicode ? '日' : 'c', unicode ? 256 : 4096));
        fixture.Guard.BeforeBatch = batch =>
        {
            Encoding.UTF8.GetByteCount(batch.Scope.TenantId).ShouldBeLessThanOrEqualTo(512);
            Encoding.UTF8.GetByteCount(batch.Scope.AggregateId).ShouldBeLessThanOrEqualTo(512);
            batch.Scope.TenantId.ShouldNotBe(tenant.Value);
            batch.Scope.AggregateId.ShouldNotBe(conversation.Value);
            batch.Operations.ShouldAllBe(operation => Encoding.UTF8.GetByteCount(operation.Key) <= 512);
            return Task.CompletedTask;
        };
        bool interrupted = false;
        fixture.Store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (!interrupted && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 1)
            {
                interrupted = true;
                throw new InvalidOperationException("interrupt bounded native metadata recovery");
            }
            return Task.CompletedTask;
        };
        ProjectionRequest request = ResolutionMetadataFixture.Request(tenantId: tenant, conversationId: conversation);
        (await fixture.Handler(fixture.Guard).ProjectAsync(request, "dispatch-long", Token)).Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        interrupted.ShouldBeTrue();
        fixture.Store.BatchFaultHook = null;
        (await fixture.Handler(fixture.Guard).ProjectAsync(request, "dispatch-long", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var model = fixture.Store.Snapshot<ConversationResolutionMetadataReadModel>("statestore",
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenant, conversation))!;
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(tenant, conversation, Token));
        model.TenantId.ShouldBe(tenant);
        model.ConversationId.ShouldBe(conversation);
        model.Metadata.ConversationId.ShouldBe(conversation.Value);
        ConversationId adjacent = new(conversation.Value + "other");
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(tenantId: tenant, conversationId: adjacent),
            "dispatch-adjacent", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Store.Snapshot<ConversationResolutionMetadataReadModel>("statestore",
            ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenant, adjacent))!.ConversationId.ShouldBe(adjacent);
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(tenant, adjacent, Token));
        (await fixture.Reader().ReadResolutionMetadataAsync(new(tenant.Value + "other"), conversation, Token)).ShouldBeNull();
        ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenant, adjacent)
            .ShouldNotBe(ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenant, conversation));
    }

    [Fact]
    public async Task SameGenerationRedeliveryCannotCompleteThroughInstalledNativeRebuildEnvelope()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var plan = await fixture.Handler().PrepareRebuildAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token);
        ReadModelBatch batch = new(new("statestore", Tenant.Value, "conversation", Conversation.Value,
            "conversation-read-model", "same-generation-rebuild"), plan.Operations);
        fixture.Store.BatchFaultHook = (phase, ordinal, _) => phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 0
            ? throw new InvalidOperationException("native proof envelope remains installed") : Task.CompletedTask;
        await Should.ThrowAsync<InvalidOperationException>(() => fixture.Store.ExecuteAsync(batch, Token));
        fixture.Store.BatchFaultHook = null;
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token))
            .Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        (await fixture.Store.ExecuteAsync(batch, Token)).IsSuccess.ShouldBeTrue();
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupersededDeliveryRequiresActualCompletedWinnerLedgerOrGenuinelyExpiredAbsence(bool expired)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync("dispatch-a");
        await fixture.ProjectAsync("dispatch-z", label: "winning label");
        var winner = fixture.Metadata;
        string key = ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-z");
        var ledger = fixture.Store.Snapshot<ConversationProjectionDispatchLedger>("statestore", key)!;
        if (expired) await fixture.EraseAsync(key);
        else fixture.Store.SeedRaw("statestore", key, ledger with { Status = ConversationProjectionDispatchStatus.Pending });
        var result = await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token);
        if (expired)
        {
            result.Status.ShouldBe(ProjectionDispatchStatus.Completed);
            (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(winner);
        }
        else
        {
            result.Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        }
        fixture.Metadata.ShouldBe(winner);
        fixture.Proof.Current!.Freshness.ShouldBe(winner.Freshness);
    }

    [Fact]
    public async Task UnchangedOldRetryAfterThreeGenerationsAndLedgerExpiryPreservesWinningNativeGeneration()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync("dispatch-a");
        await fixture.ProjectAsync("dispatch-b", changedProject: true);
        await fixture.ProjectAsync("dispatch-c", label: "third generation", changedProject: true);
        var winner = fixture.Metadata;
        await fixture.EraseAsync(ConversationProjectionReadModelKeys.DispatchLedgerKey("dispatch-a"));
        fixture.Time.Now = fixture.Time.Now.AddHours(2);
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "dispatch-a", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.ShouldBe(winner);
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(winner);
        fixture.Proof.Current!.Freshness.ShouldBe(winner.Freshness);
    }

    [Fact]
    public async Task RejectedEqualPositionOrdinalLoserCannotPoisonWinnerAndRejectedWinnerTupleCanBeRepaired()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync("dispatch-m");
        var original = fixture.Metadata;
        ProjectionRequest invalid = ResolutionMetadataFixture.Request() with
        { Events = [ResolutionMetadataFixture.Request().Events[0] with { Payload = "{"u8.ToArray() }] };
        (await fixture.Handler().ProjectAsync(invalid, "dispatch-a", Token)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        fixture.Proof.Barrier.ShouldBeNull();
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(original);
        (await fixture.Handler().ProjectAsync(invalid, "dispatch-z", Token)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        fixture.Proof.Barrier!.Position.ShouldBe(1);
        fixture.Proof.Barrier.DispatchId.ShouldBe("dispatch-z");
        fixture.Proof.Barrier.RequiresNewerGeneration.ShouldBeFalse();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token));
        await fixture.ProjectAsync("dispatch-z");
        fixture.Proof.Barrier.ShouldBeNull();
        fixture.Metadata.DispatchId.ShouldBe("dispatch-z");
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(fixture.Metadata);
    }

    [Theory]
    [InlineData(ReadModelBatchPhase.BeforeInstallOperation, 0)]
    [InlineData(ReadModelBatchPhase.AfterInstallOperation, 0)]
    [InlineData(ReadModelBatchPhase.BeforeCompaction, 0)]
    [InlineData(ReadModelBatchPhase.AfterCompaction, 0)]
    public async Task NativeRebuildOverlappingNewerLiveProducerPreservesWinnerOrWithholdsUntilConvergence(ReadModelBatchPhase phase, int ordinal)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool paused = false;
        fixture.Store.BatchFaultHook = async (observed, index, _) =>
        {
            if (!paused && observed == phase && index == ordinal)
            {
                paused = true;
                reached.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task<ProjectionDispatchResponse> rebuild = RebuildAsync(fixture);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        ProjectionRequest live = ResolutionMetadataFixture.Request(changedProject: true);
        var source = JsonSerializer.Deserialize<ConversationProjectChanged>(live.Events[1].Payload, JsonSerializerOptions.Web)!;
        ConversationMetadataUpdated updated = new(source.Metadata with { EventId = "live-third", EventType = ConversationEventType.ConversationMetadataUpdated,
            OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(2) }, "live winner")
        {
            ResolutionScoringLabel = new("live winner", ConversationResolutionScoringPolicy.Purpose,
                ConversationResolutionScoringPolicy.Classification),
        };
        live = live with { Events = [.. live.Events, new(nameof(ConversationMetadataUpdated), JsonSerializer.SerializeToUtf8Bytes(updated, JsonSerializerOptions.Web),
            "json", 3, ResolutionMetadataFixture.Started.AddSeconds(2), "correlation", "source-message-3")] };
        live = await ResolutionMetadataFixture.AdmitNativeAsync(live);
        var first = await fixture.Handler().ProjectAsync(live, "dispatch-live-winner", Token);
        try
        {
            var current = await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token);
            if (first.Status == ProjectionDispatchStatus.Completed) current.ShouldNotBeNull();
            if (current is not null) current.DispatchId.ShouldBe("dispatch-live-winner");
        }
        catch (ConversationProjectionConsistencyException)
        {
            first.Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        }
        resume.TrySetResult();
        _ = await rebuild;
        fixture.Store.BatchFaultHook = null;
        (await fixture.Handler().ProjectAsync(live, "dispatch-live-winner", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.Metadata.SafeLabel.ShouldBe("live winner");
        fixture.Metadata.LabelPurpose.ShouldBe(ConversationResolutionScoringPolicy.Purpose);
        fixture.Metadata.Freshness.LastAppliedEventPosition.ShouldBe(3);
        (await fixture.Reader().ReadResolutionMetadataAsync(Tenant, Conversation, Token)).ShouldBe(fixture.Metadata);
        fixture.Store.Snapshot<ConversationProjectedReadModels>("statestore", ConversationProjectionReadModelKeys.ConversationKey(Tenant, Conversation))!
            .DispatchId.ShouldBe("dispatch-live-winner");
        fixture.Store.Snapshot<ConversationProjectionIndexReadModel>("statestore", ConversationProjectionReadModelKeys.TenantIndexKey(Tenant))!
            .Dispatches[Conversation.Value].DispatchId.ShouldBe("dispatch-live-winner");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualNativeRawEnvelopeAndMarkerReadsRemainContentFreeWithoutWrites(bool rebuild)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ResolutionRawReadStore raw = new(fixture.Store);
        TaskCompletionSource installed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool paused = false;
        fixture.Store.BatchFaultHook = async (phase, ordinal, _) =>
        {
            if (!paused && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == (rebuild ? 0 : 1))
            {
                paused = true;
                installed.TrySetResult();
                await resume.Task.WaitAsync(Token);
            }
        };
        Task? writer = null;
        raw.AfterRawRead = async key =>
        {
            if (writer is null && key == ConversationProjectionReadModelKeys.ResolutionProofKey(Tenant, Conversation))
            {
                writer = rebuild ? RebuildAsync(fixture) : fixture.Handler().ProjectAsync(
                    ResolutionMetadataFixture.Request(changedProject: true), "dispatch-raw-live", Token);
                await installed.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            }
        };
        try
        {
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader(raw).ReadResolutionMetadataAsync(Tenant, Conversation, Token));
            raw.Observer.Reads.ShouldContain(read => read.Key.StartsWith("hxrmb:v1:marker:", StringComparison.Ordinal));
            raw.Observer.Reads.ShouldAllBe(read => read.Key.StartsWith("hxrmb:v1:marker:", StringComparison.Ordinal)
                || read.Key.StartsWith("projection:conversations-resolution:", StringComparison.Ordinal)
                || read.Key.StartsWith("projection:conversations-resolution-proof:", StringComparison.Ordinal)
                || read.Key.StartsWith("projection:conversations-dispatch:", StringComparison.Ordinal));
            foreach (var read in raw.Observer.Reads)
                if (read.Payload.Length > 0) AssertContentFreeJson(read.Payload);
            raw.Writes.ShouldBe(0);
            raw.Observer.Writes.ShouldBe(0);
        }
        finally
        {
            resume.TrySetResult();
            if (writer is not null) await writer;
        }
    }

    private static void AssertContentFreeJson(byte[] bytes)
    {
        using JsonDocument document = JsonDocument.Parse(bytes);
        Inspect(document.RootElement);
        static void Inspect(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    new[] { "detail", "summaries", "messages", "participants", "attributes", "fileReferences", "hydration", "text" }
                        .ShouldNotContain(property.Name);
                    Inspect(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray()) Inspect(item);
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                try
                {
                    byte[] decoded = Convert.FromBase64String(element.GetString()!);
                    using JsonDocument nested = JsonDocument.Parse(decoded);
                    Inspect(nested.RootElement);
                }
                catch (Exception exception) when (exception is FormatException or JsonException) { }
            }
        }
    }

    private static async Task<ProjectionDispatchResponse> RebuildAsync(ResolutionMetadataFixture fixture)
    {
        ProjectionRequest original = ResolutionMetadataFixture.Request(changedProject: true);
        var gateway = new ResolutionProjectionGateway(original);
        ProjectionRequest request = JsonSerializer.Deserialize<ProjectionRequest>(
            JsonSerializer.Serialize(original, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!;
        request.VerifiedNativeSourceProof.ShouldBeNull();
        request.NativeSourceProof.ShouldBe(gateway.Proof);
        await using ServiceProvider provider = new ServiceCollection().AddSingleton<IReadModelBatchStore>(fixture.Guard)
            .AddSingleton<IEventStoreGatewayClient>(gateway)
            .AddSingleton<IDomainProjectionHandler>(new ConversationProjectionHandler(new(), fixture.Time))
            .AddSingleton<IAsyncDomainProjectionHandler>(fixture.Handler()).BuildServiceProvider();
        string fingerprint = ProjectionRouteCatalogFingerprint.Compute("conversations-service", "v1",
            [new ProjectionDispatchRoute(ConversationProjectionHandler.ConversationDomain, ConversationAsyncProjectionHandler.ConversationReadModelProjectionType)]);
        return await DomainProjectionDispatcher.RebuildAsync(provider,
            new(request, [ConversationAsyncProjectionHandler.ConversationReadModelProjectionType],
                "rebuild-all-boundaries", fingerprint), new ProjectionDispatchOptions(),
            new DomainProjectionIdentityOptions { AppId = "conversations-service", ServiceVersion = "v1" }, Token);
    }
}
