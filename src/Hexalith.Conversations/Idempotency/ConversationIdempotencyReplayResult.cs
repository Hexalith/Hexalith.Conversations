// <copyright file="ConversationIdempotencyReplayResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Represents an idempotent duplicate replay with no new domain events.
/// </summary>
/// <param name="outcome">The stored logical outcome.</param>
public sealed record ConversationIdempotencyReplayResult(ConversationIdempotencyOutcome outcome)
    : DomainResult(Array.Empty<IEventPayload>())
{
    /// <summary>
    /// Gets the stored logical outcome.
    /// </summary>
    public ConversationIdempotencyOutcome Outcome { get; init; } =
        outcome ?? throw new ArgumentNullException(nameof(outcome));

    /// <inheritdoc />
    public override string? ResultPayload
        => JsonSerializer.Serialize(
            new
            {
                Outcome.Category,
                Outcome.SchemaVersion,
                Outcome.CommandType,
                Outcome.ConversationId,
                Outcome.MessageId,
                Outcome.ParticipantPartyId,
                Outcome.FileId,
                Outcome.RejectionCode,
                Outcome.IsRetryable,
                Outcome.AuditHandle,
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
