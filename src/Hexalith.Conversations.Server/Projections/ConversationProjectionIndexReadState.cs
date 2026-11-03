// <copyright file="ConversationProjectionIndexReadState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// How one tenant-index read maps the persisted lifecycle record, decided by the read store without writing.
/// </summary>
/// <remarks>
/// A contradictory or duplicated record is not a state: the read store throws
/// <see cref="ConversationProjectionConsistencyException"/> for it, mapped to <c>Unavailable</c>/<c>metadata_contradictory</c>.
/// </remarks>
internal enum ConversationProjectionIndexReadState
{
    /// <summary>A version 2 <c>Ready</c> index whose watermarks cover every entry: pages may report current.</summary>
    Ready = 0,

    /// <summary>A version 1 index or a version 2 <c>Rebuilding</c> index: <c>Rebuilding</c>/<c>rebuilding</c>.</summary>
    Rebuilding = 1,

    /// <summary>A lifecycle or source-position gap in a ready index: <c>Rebuilding</c>/<c>gap_detected</c>.</summary>
    GapDetected = 2,

    /// <summary>No index for an authorized tenant: <c>Unavailable</c>/<c>unavailable</c>.</summary>
    Missing = 3,
}
