// <copyright file="ConversationTemporalEventSourceResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Replay;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Carries a non-public temporal source read outcome.
/// </summary>
public sealed record ConversationTemporalEventSourceResult(
    ConversationTemporalEventSourceState State,
    IReadOnlyList<ConversationReplayEventRecord> Events,
    bool IsComplete)
{
    /// <summary>
    /// Creates an available temporal source result.
    /// </summary>
    public static ConversationTemporalEventSourceResult Available(
        IReadOnlyList<ConversationReplayEventRecord> events,
        bool isComplete = true)
        => new(ConversationTemporalEventSourceState.Available, events, isComplete);

    /// <summary>
    /// Creates a rebuilding temporal source result.
    /// </summary>
    public static ConversationTemporalEventSourceResult Rebuilding()
        => new(ConversationTemporalEventSourceState.Rebuilding, [], false);

    /// <summary>
    /// Creates an unavailable temporal source result.
    /// </summary>
    public static ConversationTemporalEventSourceResult Unavailable()
        => new(ConversationTemporalEventSourceState.Unavailable, [], false);

    /// <summary>
    /// Creates a retained-coverage miss result.
    /// </summary>
    public static ConversationTemporalEventSourceResult OutsideCoverage()
        => new(ConversationTemporalEventSourceState.OutsideCoverage, [], false);

    /// <summary>
    /// Gets the retained ordered events.
    /// </summary>
    public IReadOnlyList<ConversationReplayEventRecord> Events { get; } = ValidateEvents(Events);

    private static IReadOnlyList<ConversationReplayEventRecord> ValidateEvents(
        IReadOnlyList<ConversationReplayEventRecord>? events)
        => events is null || events.Any(e => e is null)
            ? throw new ArgumentException("Temporal events must be non-null.", nameof(events))
            : events;
}
