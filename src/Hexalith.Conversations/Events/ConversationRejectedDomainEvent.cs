// <copyright file="ConversationRejectedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records a content-safe rejection of a conversation command as a durable EventStore event.
/// </summary>
/// <remarks>
/// This is the durable replay-side artifact. It carries the minimal machine-readable rejection
/// vocabulary required for tenant-safe replay and audit reconstruction:
/// <see cref="Code"/> (stable Story 1.2 <see cref="ConversationErrorCode"/>) and
/// <see cref="ReasonCode"/> (stable narrower reason within the code).
/// Caller-facing error envelopes such as <see cref="ConversationError"/> and
/// <c>ConversationErrorResult</c> wrap this with <c>Category</c>, <c>IsRetryable</c>,
/// <c>Documentation</c>, and audit handles at response time; that mapping is owned by the
/// command-dispatch pipeline (Story 1.5+) and the publication shape (Story 1.10).
/// </remarks>
/// <param name="code">The stable machine-readable rejection code. Must not be null.</param>
/// <param name="reasonCode">The stable machine-readable reason within the code. Must not be empty.</param>
/// <param name="schemaVersion">The schema version supplied by the command, when available.</param>
/// <param name="correlationId">The safe caller correlation identifier, when available.</param>
/// <param name="causationId">The safe caller causation identifier, when available.</param>
public sealed record ConversationRejectedDomainEvent(
    ConversationErrorCode Code,
    string ReasonCode,
    SchemaVersion? SchemaVersion = null,
    string? CorrelationId = null,
    string? CausationId = null) : IRejectionEvent
{
    /// <summary>
    /// Gets the stable machine-readable rejection code.
    /// </summary>
    public ConversationErrorCode Code { get; } = Code ?? throw new ArgumentNullException(nameof(Code));

    /// <summary>
    /// Gets the stable machine-readable reason within the code.
    /// </summary>
    public string ReasonCode { get; } = ValidateReasonCode(ReasonCode);

    private static string ValidateReasonCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
