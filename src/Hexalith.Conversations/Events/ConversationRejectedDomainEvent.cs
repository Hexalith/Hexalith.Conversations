// <copyright file="ConversationRejected.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records a content-safe rejection of a conversation command.
/// </summary>
/// <param name="code">The stable machine-readable rejection code.</param>
/// <param name="reasonCode">The stable machine-readable reason within the code.</param>
/// <param name="schemaVersion">The schema version supplied by the command, when available.</param>
/// <param name="correlationId">The safe caller correlation identifier, when available.</param>
/// <param name="causationId">The safe caller causation identifier, when available.</param>
public sealed record ConversationRejected(
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
