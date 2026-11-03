// <copyright file="ConversationProjectionPositionOnlyEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Represents a durable event that advances the projection position without mutating conversation state.</summary>
/// <param name="MessageId">The persisted envelope message identity, which is this event's replay identity.</param>
/// <param name="Timestamp">The persisted envelope timestamp, which is this event's selected replay time.</param>
/// <remarks>
/// The decoder carries both values exactly as persisted. A blank identity or a default/invalid time is not
/// repaired here: the materializer rejects it before duplicate suppression as contradictory metadata.
/// </remarks>
internal sealed record ConversationProjectionPositionOnlyEvent(string MessageId, DateTimeOffset Timestamp);
