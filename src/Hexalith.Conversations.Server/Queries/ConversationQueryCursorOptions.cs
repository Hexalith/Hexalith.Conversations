// <copyright file="ConversationQueryCursorOptions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Configures the domain-policy bounds the conversation list cursor re-applies after the platform
/// <see cref="Hexalith.EventStore.Client.Queries.IQueryCursorCodec"/> validates a continuation cursor.
/// </summary>
/// <remarks>
/// Cursor integrity (signing, tamper detection, cross-purpose isolation) now belongs entirely to the codec's
/// ASP.NET Core Data Protection layer, so no signing key or key id lives here. The codec has no wall-clock
/// lifetime and no offset bound, so the handler re-applies these two policy values after a successful decode:
/// an oversized offset, an expired cursor, or a future-dated cursor fails closed exactly as the hand-rolled
/// codec did.
/// </remarks>
public sealed class ConversationQueryCursorOptions
{
    /// <summary>
    /// Gets or sets the maximum cursor age. Defaults to 30 minutes; cursors older or future-dated beyond this window fail closed.
    /// </summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the maximum accepted page offset. Defends against forged cursors that would force unbounded skip scans.
    /// </summary>
    public int MaxOffset { get; set; } = 100_000;
}
