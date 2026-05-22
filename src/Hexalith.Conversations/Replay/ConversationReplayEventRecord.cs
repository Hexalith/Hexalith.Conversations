// <copyright file="ConversationReplayEventRecord.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Replay;

/// <summary>
/// Carries a persisted conversation event and its ordered cursor for deterministic replay.
/// </summary>
/// <param name="Position">The positive ordered event position.</param>
/// <param name="Event">The persisted conversation event payload.</param>
public sealed record ConversationReplayEventRecord(long Position, object Event)
{
    /// <summary>
    /// Gets the positive ordered event position.
    /// </summary>
    public long Position { get; } = ValidatePosition(Position);

    /// <summary>
    /// Gets the persisted conversation event payload.
    /// </summary>
    public object Event { get; } = Event ?? throw new ArgumentNullException(nameof(Event));

    private static long ValidatePosition(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }
}
