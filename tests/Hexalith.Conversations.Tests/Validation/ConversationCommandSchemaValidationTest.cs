// <copyright file="ConversationCommandSchemaValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Validation;

namespace Hexalith.Conversations.Tests.Validation;

/// <summary>
/// Verifies command-envelope validation shared by all public command shapes.
/// </summary>
public sealed class ConversationCommandSchemaValidationTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");

    /// <summary>
    /// P24: every command type requires an idempotency key at the command boundary.
    /// </summary>
    /// <param name="commandType">The command type under test.</param>
    /// <param name="idempotencyKey">The missing or malformed key.</param>
    [Theory]
    [InlineData("create", null)]
    [InlineData("append", "")]
    [InlineData("add-participant", " ")]
    [InlineData("attach-reference", "\t")]
    [InlineData("update-metadata", null)]
    [InlineData("close", "")]
    [InlineData("archive", "\r\n")]
    public void MissingIdempotencyKeyShouldReturnTypedRejectionForEveryCommandType(
        string commandType,
        string? idempotencyKey)
    {
        object command = Command(commandType, Metadata(idempotencyKey));

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.Code.ShouldBe(ConversationErrorCode.IdempotencyKeyMissing);
        rejection.ReasonCode.ShouldBe("idempotency_key_missing");
    }

    private static object Command(string commandType, ConversationCommandMetadata metadata)
        => commandType switch
        {
            "create" => new CreateConversationCommand(metadata),
            "append" => new AppendMessageCommand(metadata, Conversation, new MessageId("message-001"), Actor, "Hello"),
            "add-participant" => new AddParticipantCommand(
                metadata,
                Conversation,
                new PartyId("party-participant"),
                ParticipantType.Human,
                ParticipantRole.Member),
            "attach-reference" => new AttachFileReferenceCommand(metadata, Conversation, new FileId("file-001")),
            "update-metadata" => new UpdateConversationMetadataCommand(
                metadata,
                Conversation,
                "Case 123",
                new BusinessReference("crm", "case-123")),
            "close" => new CloseConversationCommand(metadata, Conversation, "resolved"),
            "archive" => new ArchiveConversationCommand(metadata, Conversation, "retained"),
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), commandType, "Unsupported command type."),
        };

    private static ConversationCommandMetadata Metadata(string? idempotencyKey)
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "caller-correlation-secret",
            "caller-causation-secret",
            idempotencyKey);
}
