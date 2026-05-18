// <copyright file="ContractValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies contract-level validation for non-empty metadata and unambiguous ranges.
/// </summary>
public sealed class ContractValidationTest
{
    /// <summary>
    /// Ensures version contracts reject impossible supported-version ranges.
    /// </summary>
    [Fact]
    public void VersionContractsShouldRejectMinimumVersionAboveActiveVersion()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ContractVersionInfo(
            "Conversations",
            new SchemaVersion(1),
            new SchemaVersion(2)));

        Should.Throw<ArgumentOutOfRangeException>(() => new UnsupportedSchemaVersion(
            new SchemaVersion(3),
            new SchemaVersion(1),
            new SchemaVersion(2)));
    }

    /// <summary>
    /// Ensures <see cref="UnsupportedSchemaVersion"/> rejects payloads whose requested version
    /// is actually supported (inside the [minimum, active] inclusive range).
    /// </summary>
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 1, 3)]
    [InlineData(3, 1, 3)]
    public void UnsupportedSchemaVersionShouldRejectInRangeRequest(int requested, int min, int active)
        => Should.Throw<ArgumentOutOfRangeException>(() => new UnsupportedSchemaVersion(
            new SchemaVersion(requested),
            new SchemaVersion(active),
            new SchemaVersion(min)));

    /// <summary>
    /// Ensures error result wrappers contain at least one non-null error.
    /// </summary>
    [Fact]
    public void ErrorResultShouldRequireAtLeastOneNonNullError()
    {
        Should.Throw<ArgumentException>(() => new ConversationErrorResult([]));
        Should.Throw<ArgumentException>(() => new ConversationErrorResult([null!]));
    }

    /// <summary>
    /// Ensures the error result snapshots its input so post-construction list mutation is invisible.
    /// </summary>
    [Fact]
    public void ErrorResultShouldSnapshotInputList()
    {
        ConversationError original = ContractSamples.SafeError(ConversationErrorCode.IdempotencyConflict);
        ConversationError replacement = ContractSamples.SafeError(ConversationErrorCode.TenantBindingMissing);

        List<ConversationError> input = [original];
        ConversationErrorResult result = new(input);

        input[0] = replacement;

        result.Errors.ShouldHaveSingleItem();
        result.Errors[0].ShouldBe(original);
    }

    /// <summary>
    /// Ensures public timestamps reject the default minimum value and out-of-range years.
    /// </summary>
    [Theory]
    [InlineData(0, 1, 1)] // MinValue
    [InlineData(1999, 12, 31)] // below business floor
    public void TimestampContractsShouldRejectImplausibleValues(int year, int month, int day)
    {
        DateTimeOffset stamp = year == 0
            ? DateTimeOffset.MinValue
            : new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);

        Should.Throw<ArgumentOutOfRangeException>(() => new ProjectionFreshness(
            ContractSamples.Freshness.State,
            stamp,
            ContractSamples.Version));

        Should.Throw<ArgumentOutOfRangeException>(() => new ConversationEventMetadata(
            ContractSamples.Version,
            "event-001",
            ConversationEventType.ConversationCreated,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            "correlation-001",
            stamp,
            ContractSamples.Actor));

        Should.Throw<ArgumentOutOfRangeException>(() => new ConversationMessageProjection(
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Message,
            ContractSamples.Actor,
            "Hello",
            stamp,
            ContractSamples.Freshness));
    }

    /// <summary>
    /// Ensures required metadata strings cannot be empty across every whitespace variant.
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("")]
    public void RequiredMetadataStringsShouldRejectWhitespace(string value)
    {
        Should.Throw<ArgumentException>(() => new ConversationCommandMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Actor,
            value));

        Should.Throw<ArgumentException>(() => new ConversationError(
            ContractSamples.Version,
            ConversationErrorCode.CommandValidationFailed,
            ConversationErrorCategory.Validation,
            false,
            value));
    }

    /// <summary>
    /// Ensures empty event identifiers are rejected at event metadata construction.
    /// </summary>
    [Fact]
    public void EventIdentifierShouldRejectEmpty()
    {
        Should.Throw<ArgumentException>(() => new ConversationEventMetadata(
            ContractSamples.Version,
            string.Empty,
            ConversationEventType.ConversationCreated,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            "correlation-001",
            ContractSamples.EventMetadata.CommittedAt,
            ContractSamples.Actor));
    }

    /// <summary>
    /// Ensures non-nullable identifier parameters reject null at envelope construction.
    /// </summary>
    [Fact]
    public void EnvelopeRecordsShouldRejectNullIdentifiers()
    {
        Should.Throw<ArgumentNullException>(() => new ConversationCommandMetadata(
            ContractSamples.Version,
            null!,
            ContractSamples.Actor,
            "correlation-001"));

        Should.Throw<ArgumentNullException>(() => new ConversationEventMetadata(
            ContractSamples.Version,
            "event-001",
            ConversationEventType.ConversationCreated,
            null!,
            ContractSamples.Conversation,
            "correlation-001",
            ContractSamples.EventMetadata.CommittedAt,
            ContractSamples.Actor));
    }

    /// <summary>
    /// Ensures optional reason codes are either absent or meaningful across every whitespace variant.
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("")]
    public void OptionalReasonCodesShouldRejectWhitespaceWhenProvided(string value)
    {
        Should.Throw<ArgumentException>(() => new CloseConversationCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            value));

        Should.Throw<ArgumentException>(() => new ArchiveConversationCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            value));
    }

    /// <summary>
    /// Ensures provider metadata names remain meaningful correlation metadata.
    /// </summary>
    [Fact]
    public void ProviderCorrelationMetadataShouldRequireProviderNameAndType()
    {
        Should.Throw<ArgumentException>(() => new ProviderCorrelationMetadata(
            string.Empty,
            "assistant",
            ContractSamples.Version));

        Should.Throw<ArgumentException>(() => new ProviderCorrelationMetadata(
            "provider-a",
            "\t",
            ContractSamples.Version));
    }

    /// <summary>
    /// Ensures provider correlation extension data rejects empty keys and null values.
    /// </summary>
    [Fact]
    public void ProviderCorrelationMetadataShouldRejectMalformedExtensionData()
    {
        Should.Throw<ArgumentException>(() => new ProviderCorrelationMetadata(
            "provider-a",
            "assistant",
            ContractSamples.Version,
            ExtensionData: new Dictionary<string, string>
            {
                [" "] = "value",
            }));

        Should.Throw<ArgumentException>(() => new ProviderCorrelationMetadata(
            "provider-a",
            "assistant",
            ContractSamples.Version,
            ExtensionData: new Dictionary<string, string?>
            {
                ["region"] = null,
            }! as IReadOnlyDictionary<string, string>));
    }

    /// <summary>
    /// Ensures participant lists are adopter-friendly when no participants are present.
    /// </summary>
    [Fact]
    public void ConversationSummaryProjectionShouldDefaultParticipantListToEmpty()
    {
        ConversationSummaryProjection projection = new(
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Freshness);

        projection.ParticipantPartyIds.ShouldBeEmpty();
    }

    /// <summary>
    /// Ensures conversation summary rejects participant lists containing null elements.
    /// </summary>
    [Fact]
    public void ConversationSummaryProjectionShouldRejectNullParticipantElements()
    {
        Should.Throw<ArgumentException>(() => new ConversationSummaryProjection(
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Freshness,
            ParticipantPartyIds: [null!]));
    }

    /// <summary>
    /// Closed-vocabulary types reject unknown values at <c>Parse</c>.
    /// </summary>
    [Fact]
    public void ClosedVocabularyParseShouldRejectUnknownValues()
    {
        Should.Throw<ArgumentException>(() => ConversationErrorCode.Parse("bogus_code"));
        Should.Throw<ArgumentException>(() => ConversationErrorCategory.Parse("bogus"));
        Should.Throw<ArgumentException>(() => ConversationEventType.Parse("BogusEvent"));
        Should.Throw<ArgumentException>(() => ConversationCommandType.Parse("BogusCommand"));
        Should.Throw<ArgumentException>(() => ProjectionTrustState.Parse("Bogus"));
    }
}
