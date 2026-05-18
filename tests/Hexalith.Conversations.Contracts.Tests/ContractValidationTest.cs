// <copyright file="ContractValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
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
    /// Ensures error result wrappers contain at least one non-null error.
    /// </summary>
    [Fact]
    public void ErrorResultShouldRequireAtLeastOneNonNullError()
    {
        Should.Throw<ArgumentException>(() => new ConversationErrorResult([]));
        Should.Throw<ArgumentException>(() => new ConversationErrorResult([null!]));
    }

    /// <summary>
    /// Ensures public timestamps reject the default minimum value.
    /// </summary>
    [Fact]
    public void TimestampContractsShouldRejectDefaultMinimumValue()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ProjectionFreshness(
            ContractSamples.Freshness.State,
            DateTimeOffset.MinValue,
            ContractSamples.Version));

        Should.Throw<ArgumentOutOfRangeException>(() => new ConversationEventMetadata(
            ContractSamples.Version,
            "event-001",
            ConversationEventType.ConversationCreated,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            "correlation-001",
            DateTimeOffset.MinValue));

        Should.Throw<ArgumentOutOfRangeException>(() => new ConversationMessageProjection(
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Message,
            ContractSamples.Actor,
            "Hello",
            DateTimeOffset.MinValue,
            ContractSamples.Freshness));
    }

    /// <summary>
    /// Ensures required metadata strings cannot be empty.
    /// </summary>
    [Fact]
    public void RequiredMetadataStringsShouldRejectWhitespace()
    {
        Should.Throw<ArgumentException>(() => new ConversationCommandMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Actor,
            " "));

        Should.Throw<ArgumentException>(() => new ConversationError(
            ContractSamples.Version,
            ConversationErrorCode.CommandValidationFailed,
            ConversationErrorCategory.Validation,
            false,
            "\t"));

        Should.Throw<ArgumentException>(() => new ConversationEventMetadata(
            ContractSamples.Version,
            string.Empty,
            ConversationEventType.ConversationCreated,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            "correlation-001",
            ContractSamples.EventMetadata.CommittedAt));
    }

    /// <summary>
    /// Ensures optional reason codes are either absent or meaningful.
    /// </summary>
    [Fact]
    public void OptionalReasonCodesShouldRejectWhitespaceWhenProvided()
    {
        Should.Throw<ArgumentException>(() => new CloseConversationCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            " "));

        Should.Throw<ArgumentException>(() => new ArchiveConversationCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            "\n"));
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
}
