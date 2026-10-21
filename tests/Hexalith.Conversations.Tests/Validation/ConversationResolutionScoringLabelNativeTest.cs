using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Validation;

using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Tests.Validation;

public sealed class ConversationResolutionScoringLabelNativeTest
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly ConversationId Conversation = new("conversation-scoring");

    [Fact]
    public void ClassifiedCreationPersistsExactPurposeAndLabelThroughSerializedNativeReplay()
    {
        CreateConversationCommand command = Command() with { ResolutionScoringLabel = Classify(" exact label ") };
        var result = CreateConversationBoundary.Dispatch(command, Conversation, CreatedAt, "event-created");
        result.IsSuccess.ShouldBeTrue();
        var created = result.Events.Single().ShouldBeOfType<ConversationCreatedDomainEvent>();
        var persisted = JsonSerializer.Deserialize<ConversationCreatedDomainEvent>(JsonSerializer.Serialize(created))!;
        ConversationState replayed = new();
        replayed.Apply(persisted);
        replayed.Label.ShouldBe(" exact label ");
        replayed.ResolutionScoringLabel.ShouldBe(command.ResolutionScoringLabel);
        replayed.ResolutionScoringLabel!.Purpose.ShouldBe(ConversationResolutionScoringLabel.SupportedPurpose);
        replayed.ResolutionScoringLabel.Classification.ShouldBe(ConversationResolutionScoringLabel.OwnerClassification);
    }

    [Fact]
    public void LegacyLabelsRemainUnclassifiedAndDescriptiveUpdatesRevokeClassification()
    {
        var legacy = CreateConversationBoundary.Dispatch(Command(), Conversation, CreatedAt, "event-legacy")
            .Events.Single().ShouldBeOfType<ConversationCreatedDomainEvent>();
        ConversationState state = new();
        state.Apply(legacy);
        state.Label.ShouldBe(" exact label ");
        state.ResolutionScoringLabel.ShouldBeNull();
        var classified = legacy with { ResolutionScoringLabel = Classify(legacy.Label) };
        ConversationState classifiedState = new();
        classifiedState.Apply(classified);
        classifiedState.Apply(new ConversationMetadataUpdated(classified.Metadata with
        {
            EventId = "event-label-changed", EventType = ConversationEventType.ConversationMetadataUpdated,
        }, Label: "new descriptive label"));
        classifiedState.Label.ShouldBe("new descriptive label");
        classifiedState.ResolutionScoringLabel.ShouldBeNull();
        var reclassified = new ConversationMetadataUpdated(classified.Metadata with
        {
            EventId = "event-label-reclassified", EventType = ConversationEventType.ConversationMetadataUpdated,
        }, Label: "new descriptive label") { ResolutionScoringLabel = Classify("new descriptive label") };
        var persisted = JsonSerializer.Deserialize<ConversationMetadataUpdated>(JsonSerializer.Serialize(reclassified))!;
        classifiedState.Apply(persisted);
        classifiedState.ResolutionScoringLabel.ShouldBe(reclassified.ResolutionScoringLabel);
    }

    [Fact]
    public void NativeClassificationCannotBeReusedForADifferentLabelOrIdempotentMeaning()
    {
        CreateConversationCommand legacy = Command();
        var classified = legacy with { ResolutionScoringLabel = Classify(legacy.Label) };
        var first = ConversationCommandFingerprint.Create(legacy, Conversation);
        var second = ConversationCommandFingerprint.Create(classified, Conversation);
        first.Scope.ShouldBe(second.Scope);
        first.PayloadFingerprint.ShouldNotBe(second.PayloadFingerprint);
        ConversationCommandFingerprint.Create(legacy with { ResolutionScoringLabel = null }, Conversation).ShouldBe(first);
        var mismatched = classified with { Label = "another label" };
        var rejected = CreateConversationBoundary.Dispatch(mismatched, Conversation, CreatedAt, "event-invalid");
        rejected.IsRejection.ShouldBeTrue();
        rejected.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>().ReasonCode.ShouldBe("scoring_label_mismatch");
    }

    [Fact]
    public void MetadataCommandClassificationIsBoundToItsNativeLabelAndIdempotentMeaning()
    {
        UpdateConversationMetadataCommand command = new(Command().Metadata, Conversation, Label: "label");
        var classified = command with { ResolutionScoringLabel = Classify("label") };
        ConversationCommandSchemaValidation.ValidateEnvelope(classified).ShouldBeNull();
        ConversationCommandFingerprint.Create(command, Conversation).PayloadFingerprint.ShouldNotBe(
            ConversationCommandFingerprint.Create(classified, Conversation).PayloadFingerprint);
        var mismatch = classified with { Label = "other label" };
        ConversationCommandSchemaValidation.ValidateEnvelope(mismatch)!.ReasonCode.ShouldBe("scoring_label_mismatch");
    }

    private static ConversationResolutionScoringLabel Classify(string? value)
        => new(value, ConversationResolutionScoringLabel.SupportedPurpose, ConversationResolutionScoringLabel.OwnerClassification);

    private static CreateConversationCommand Command()
        => new(new(SchemaVersion.Current, new TenantId("tenant-scoring"), new PartyId("actor-scoring"),
            "correlation-scoring", IdempotencyKey: "idempotency-scoring"), Label: " exact label ");
}
