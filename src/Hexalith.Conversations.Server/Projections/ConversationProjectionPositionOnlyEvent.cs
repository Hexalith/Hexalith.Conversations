// <copyright file="ConversationProjectionPositionOnlyEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Represents a durable event that advances the projection position without mutating conversation state.</summary>
/// <param name="Timestamp">The authoritative persisted event timestamp.</param>
internal sealed record ConversationProjectionPositionOnlyEvent(DateTimeOffset Timestamp);
