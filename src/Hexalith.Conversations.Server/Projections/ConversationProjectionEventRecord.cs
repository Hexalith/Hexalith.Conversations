// <copyright file="ConversationProjectionEventRecord.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Carries a public conversation event and its safe source position for projection materialization.
/// </summary>
/// <param name="Position">The positive source position used for gap detection.</param>
/// <param name="Event">The public conversation event contract.</param>
public sealed record ConversationProjectionEventRecord(long Position, object Event)
{
    /// <summary>
    /// Gets the positive source position used for gap detection.
    /// </summary>
    public long Position { get; } = ValidatePosition(Position);

    /// <summary>
    /// Gets the public conversation event contract.
    /// </summary>
    public object Event { get; } = Event ?? throw new ArgumentNullException(nameof(Event));

    private static long ValidatePosition(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }
}
