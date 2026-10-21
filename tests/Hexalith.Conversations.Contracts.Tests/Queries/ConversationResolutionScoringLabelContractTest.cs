using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Serialization;

using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Queries;

public sealed class ConversationResolutionScoringLabelContractTest
{
    [Theory]
    [InlineData(null)]
    [InlineData(" exact label ")]
    public void ClassifiedCreationCommandPreservesSourceGeneratedShapeAndOriginalConstructor(string? label)
    {
        var value = Classify(label);
        CreateConversationCommand legacy = new(ContractSamples.CommandMetadata, Label: label);
        AssertSourceGeneratedRoundTrip(legacy, legacy with { ResolutionScoringLabel = value }, value,
            ConversationsJsonContext.Default.CreateConversationCommand,
            [typeof(ConversationCommandMetadata), typeof(BusinessReference), typeof(ProjectId), typeof(FolderId),
                typeof(string), typeof(ProviderCorrelationMetadata), typeof(CallerMetadata)]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" exact label ")]
    public void ClassifiedMetadataCommandPreservesSourceGeneratedShapeAndOriginalConstructor(string? label)
    {
        var value = Classify(label);
        UpdateConversationMetadataCommand legacy = new(ContractSamples.CommandMetadata, ContractSamples.Conversation, Label: label);
        AssertSourceGeneratedRoundTrip(legacy, legacy with { ResolutionScoringLabel = value }, value,
            ConversationsJsonContext.Default.UpdateConversationMetadataCommand,
            [typeof(ConversationCommandMetadata), typeof(ConversationId), typeof(string), typeof(BusinessReference),
                typeof(IReadOnlyDictionary<string, string>), typeof(CallerMetadata)]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" exact label ")]
    public void ClassifiedCreationEventPreservesSourceGeneratedShapeAndOriginalConstructor(string? label)
    {
        var value = Classify(label);
        ConversationCreated legacy = new(ContractSamples.EventMetadata, Label: label);
        AssertSourceGeneratedRoundTrip(legacy, legacy with { ResolutionScoringLabel = value }, value,
            ConversationsJsonContext.Default.ConversationCreated,
            [typeof(ConversationEventMetadata), typeof(BusinessReference), typeof(ProjectId), typeof(FolderId),
                typeof(string), typeof(ProviderCorrelationMetadata)]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" exact label ")]
    public void ClassifiedMetadataEventPreservesSourceGeneratedShapeAndOriginalConstructor(string? label)
    {
        var value = Classify(label);
        ConversationMetadataUpdated legacy = new(ContractSamples.EventMetadata with
            { EventType = ConversationEventType.ConversationMetadataUpdated }, Label: label);
        AssertSourceGeneratedRoundTrip(legacy, legacy with { ResolutionScoringLabel = value }, value,
            ConversationsJsonContext.Default.ConversationMetadataUpdated,
            [typeof(ConversationEventMetadata), typeof(string), typeof(BusinessReference), typeof(IReadOnlyDictionary<string, string>)]);
    }

    private static ConversationResolutionScoringLabel Classify(string? label)
        => new(label, ConversationResolutionScoringLabel.SupportedPurpose, ConversationResolutionScoringLabel.OwnerClassification);

    private static void AssertSourceGeneratedRoundTrip<T>(T legacy, T classified,
        ConversationResolutionScoringLabel value, JsonTypeInfo<T> typeInfo, Type[] constructorSignature)
    {
        JsonElement before = JsonSerializer.SerializeToElement(legacy, typeInfo);
        before.TryGetProperty("resolutionScoringLabel", out _).ShouldBeFalse();
        JsonElement json = JsonSerializer.SerializeToElement(classified, typeInfo);
        var restored = JsonSerializer.Deserialize(json, typeInfo)!;
        JsonElement.DeepEquals(json, JsonSerializer.SerializeToElement(restored, typeInfo)).ShouldBeTrue();
        json.GetProperty("resolutionScoringLabel").Deserialize(ConversationsJsonContext.Default.ConversationResolutionScoringLabel)
            .ShouldBe(value);
        json.GetProperty("label").GetString().ShouldBe(value.Value);
        typeof(T).GetConstructor(constructorSignature).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("secret value")]
    [InlineData("https://private.example/data")]
    [InlineData("line\nbreak")]
    [InlineData("   ")]
    public void UnsafeLabelsCannotAcquireClassification(string label)
        => Should.Throw<ArgumentException>(() => new ConversationResolutionScoringLabel(label,
            ConversationResolutionScoringLabel.SupportedPurpose, ConversationResolutionScoringLabel.OwnerClassification));

    [Fact]
    public void UnsupportedPurposeClassificationAndOversizedLabelsAreRejectedWithoutTruncation()
    {
        Should.Throw<ArgumentException>(() => new ConversationResolutionScoringLabel("label", "inspection",
            ConversationResolutionScoringLabel.OwnerClassification));
        Should.Throw<ArgumentException>(() => new ConversationResolutionScoringLabel("label",
            ConversationResolutionScoringLabel.SupportedPurpose, "caller-asserted"));
        Should.Throw<ArgumentException>(() => new ConversationResolutionScoringLabel(new string('a', 161),
            ConversationResolutionScoringLabel.SupportedPurpose, ConversationResolutionScoringLabel.OwnerClassification));
        new ConversationResolutionScoringLabel(new string('a', 160), ConversationResolutionScoringLabel.SupportedPurpose,
            ConversationResolutionScoringLabel.OwnerClassification).Value!.Length.ShouldBe(160);
    }
}
