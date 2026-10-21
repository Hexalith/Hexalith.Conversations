using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Publication;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Projections;

public sealed class ConversationResolutionScoringLabelProjectionTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SafeLegacyLabelCannotBeClassifiedByProjectionOrMetadataQuery()
    {
        ResolutionMetadataFixture fixture = new();
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(classifyLabel: false), "legacy-label", Token))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.IsLabelComplete.ShouldBeFalse();
        fixture.Metadata.Metadata.SafeLabel.ShouldBeNull();
        fixture.Metadata.LabelClassification.ShouldBeNull();
        fixture.Metadata.LabelPurpose.ShouldBeNull();
        int count = fixture.Store.Count;
        var result = await fixture.DispatchAsync();
        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);
        fixture.Store.Count.ShouldBe(count);
    }

    [Fact]
    public async Task ExplicitNativeMetadataReclassificationAdmitsALegacyLabelWithoutRewritingHistory()
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest original = ResolutionMetadataFixture.Request(classifyLabel: false);
        (await fixture.Handler().ProjectAsync(original, "legacy-label", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.IsLabelComplete.ShouldBeFalse();
        var created = JsonSerializer.Deserialize<ConversationCreated>(original.Events[0].Payload, JsonSerializerOptions.Web)!;
        ConversationMetadataUpdated classified = new(created.Metadata with
        {
            EventId = "event-reclassified", EventType = ConversationEventType.ConversationMetadataUpdated,
            OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1),
        }, Label: created.Label)
        {
            ResolutionScoringLabel = new(created.Label, ConversationResolutionScoringLabel.SupportedPurpose,
                ConversationResolutionScoringLabel.OwnerClassification),
        };
        ProjectionRequest next = original with { Events = [.. original.Events,
            new(nameof(ConversationMetadataUpdated), JsonSerializer.SerializeToUtf8Bytes(classified, JsonSerializerOptions.Web),
                "json", 2, ResolutionMetadataFixture.Started.AddSeconds(1), "correlation", "source-message-2")] };
        (await fixture.Handler().ProjectAsync(next, "native-reclassified", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.IsLabelComplete.ShouldBeTrue();
        fixture.Metadata.Metadata.SafeLabel.ShouldBe(created.Label);
        fixture.Metadata.LabelPurpose.ShouldBe(ConversationResolutionScoringLabel.SupportedPurpose);
        next.Events[0].Payload.ShouldBe(original.Events[0].Payload);
        var result = await fixture.DispatchAsync();
        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("metadata").GetProperty("safeLabel").GetString().ShouldBe(created.Label);
    }

    [Fact]
    public async Task DescriptiveLabelUpdateInvalidatesNativeClassificationThroughNormalProjectionAndRebuild()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ProjectionRequest initial = ResolutionMetadataFixture.Request();
        var created = JsonSerializer.Deserialize<ConversationCreated>(initial.Events[0].Payload, JsonSerializerOptions.Web)!;
        var updated = new ConversationMetadataUpdated(created.Metadata with
        {
            EventId = "event-label-update", EventType = ConversationEventType.ConversationMetadataUpdated,
            OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1),
        }, Label: "new descriptive label");
        ProjectionRequest request = initial with { Events = [.. initial.Events,
            new(nameof(ConversationMetadataUpdated), JsonSerializer.SerializeToUtf8Bytes(updated, JsonSerializerOptions.Web),
                "json", 2, ResolutionMetadataFixture.Started.AddSeconds(1), "correlation", "source-message-2")] };
        (await fixture.Handler().ProjectAsync(request, "updated-label", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.IsLabelComplete.ShouldBeFalse();
        fixture.Metadata.LabelPurpose.ShouldBeNull();
        await RebuildAsync(fixture, request);
        fixture.Metadata.IsLabelComplete.ShouldBeFalse();
        fixture.Metadata.Metadata.SafeLabel.ShouldBeNull();
        fixture.Metadata.LabelClassification.ShouldBeNull();
    }

    [Fact]
    public async Task UnrelatedMetadataUpdatePreservesNativeClassificationAndAuthorizedSafeLabel()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ProjectionRequest original = ResolutionMetadataFixture.Request();
        var created = JsonSerializer.Deserialize<ConversationCreated>(original.Events[0].Payload, JsonSerializerOptions.Web)!;
        ConversationMetadataUpdated updated = new(created.Metadata with
        {
            EventId = "unrelated-metadata-update", EventType = ConversationEventType.ConversationMetadataUpdated,
            OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(1),
        }, BusinessReference: new("support", "case-123"), Attributes: new Dictionary<string, string> { ["category"] = "support" });
        ProjectionRequest next = original with { Events = [.. original.Events, new(nameof(ConversationMetadataUpdated),
            JsonSerializer.SerializeToUtf8Bytes(updated, JsonSerializerOptions.Web), "json", 2,
            ResolutionMetadataFixture.Started.AddSeconds(1), "correlation", "source-message-2")] };
        (await fixture.Handler().ProjectAsync(next, "unrelated-metadata", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.Metadata.SafeLabel.ShouldBe(created.Label);
        fixture.Metadata.LabelPurpose.ShouldBe(created.ResolutionScoringLabel!.Purpose);
        fixture.Metadata.LabelClassification.ShouldBe(created.ResolutionScoringLabel.Classification);
        fixture.Metadata.IsLabelComplete.ShouldBeTrue();
        var result = await fixture.DispatchAsync();
        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("metadata").GetProperty("safeLabel").GetString().ShouldBe(created.Label);
    }

    [Fact]
    public async Task NativeClassificationRebuildRetainsSourceTimestampAndImmutableRetryGenerationTime()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        fixture.Metadata.LabelPurpose.ShouldBe(ConversationResolutionScoringLabel.SupportedPurpose);
        ProjectionRequest request = ResolutionMetadataFixture.Request(changedProject: true);
        var original = JsonSerializer.Deserialize<ConversationCreated>(request.Events[0].Payload, JsonSerializerOptions.Web)!;
        ConversationCreatedDomainEvent durable = new(original.Metadata, original.BusinessReference, original.ProjectId,
            original.FolderId, original.Label, ProviderCorrelation: original.ProviderCorrelation)
        { ResolutionScoringLabel = original.ResolutionScoringLabel };
        var publication = ConversationPublicationMapper.TryMap(PersistedConversationEvent.Success(original.Metadata.TenantId, durable));
        publication.IsPublished.ShouldBeTrue();
        ConversationCreated published = publication.GetPublishedEvent<ConversationCreated>();
        published.ResolutionScoringLabel.ShouldBe(original.ResolutionScoringLabel);
        request = request with { Events = [request.Events[0] with
            { Payload = JsonSerializer.SerializeToUtf8Bytes(published, JsonSerializerOptions.Web) }, .. request.Events.Skip(1)] };
        DateTimeOffset initialGenerationTime = fixture.Metadata.Freshness.ProjectionGeneratedAt;
        fixture.Time.Now = fixture.Time.Now.AddMinutes(2);
        DateTimeOffset rebuildGenerationTime = fixture.Time.Now;
        ReadModelBatch? originalBatch = null;
        fixture.Guard.BeforeBatch = batch => { originalBatch = batch; return Task.CompletedTask; };
        await RebuildAsync(fixture, request);
        fixture.Metadata.Metadata.SafeLabel.ShouldBe("Scoring label");
        fixture.Metadata.LabelPurpose.ShouldBe(ConversationResolutionScoringLabel.SupportedPurpose);
        fixture.Metadata.LabelClassification.ShouldBe(ConversationResolutionScoringLabel.OwnerClassification);
        fixture.Metadata.Freshness.LastAppliedEventTimestamp.ShouldBe(ResolutionMetadataFixture.Started.AddSeconds(1));
        fixture.Metadata.Freshness.ProjectionGeneratedAt.ShouldBe(rebuildGenerationTime);
        fixture.Metadata.Freshness.ProjectionGeneratedAt.ShouldNotBe(initialGenerationTime);
        fixture.Metadata.Freshness.ProjectionGeneratedAt.ShouldNotBe(fixture.Metadata.Freshness.LastAppliedEventTimestamp);
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
        fixture.Time.Now = fixture.Time.Now.AddMinutes(1);
        fixture.Guard.BeforeBatch = null;
        (await fixture.Guard.ExecuteAsync(originalBatch!, Token)).IsSuccess.ShouldBeTrue();
        fixture.Metadata.Freshness.ProjectionGeneratedAt.ShouldBe(rebuildGenerationTime);
        fixture.Metadata.Freshness.LastAppliedEventTimestamp.ShouldBe(ResolutionMetadataFixture.Started.AddSeconds(1));
        fixture.Proof.Current!.Matches(fixture.Metadata).ShouldBeTrue();
    }

    private static async Task RebuildAsync(ResolutionMetadataFixture fixture, ProjectionRequest request)
    {
        await using ServiceProvider provider = new ServiceCollection().AddSingleton<IReadModelBatchStore>(fixture.Guard)
            .AddSingleton<IDomainProjectionHandler>(new ConversationProjectionHandler(new(), fixture.Time))
            .AddSingleton<IAsyncDomainProjectionHandler>(fixture.Handler()).BuildServiceProvider();
        string fingerprint = ProjectionRouteCatalogFingerprint.Compute("conversations-service", "v1",
            [new ProjectionDispatchRoute(ConversationProjectionHandler.ConversationDomain,
                ConversationAsyncProjectionHandler.ConversationReadModelProjectionType)]);
        var result = await DomainProjectionDispatcher.RebuildAsync(provider,
            new(request, [ConversationAsyncProjectionHandler.ConversationReadModelProjectionType], "zz-rebuild-scoring-label", fingerprint),
            new ProjectionDispatchOptions(), new DomainProjectionIdentityOptions { AppId = "conversations-service", ServiceVersion = "v1" }, Token);
        result.Outcomes.ShouldAllBe(outcome => outcome.Status == ProjectionDispatchStatus.Completed);
    }
}
