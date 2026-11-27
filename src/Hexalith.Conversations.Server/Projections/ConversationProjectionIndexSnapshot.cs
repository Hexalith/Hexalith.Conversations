// <copyright file="ConversationProjectionIndexSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// One tenant-index read: the candidate summaries plus the dispatch references that describe which
/// generation each candidate belongs to.
/// </summary>
/// <remarks>
/// <para>
/// The list boundary reads the index exactly once and pages from this snapshot before verifying anything
/// against the detail keys. Verification is therefore proportional to the requested page rather than to the
/// tenant's conversation count, which is what keeps the no-N+1 read contract (NFR2) intact.
/// </para>
/// <para>
/// <see cref="HasIncompleteDispatch"/> is the tenant-scoped half of the cross-key guarantee: a dispatch
/// reference without a matching summary generation means an accepted conversation may not be represented in
/// <see cref="Summaries"/> at all. A caller that cannot see the missing row must not report the page as
/// current, but it may still return the rows it does hold — omission degrades freshness, it does not
/// invalidate unrelated conversations.
/// </para>
/// <para>
/// <see cref="ReadState"/> carries how the read store mapped the persisted lifecycle record (AD-7). A snapshot
/// built without one describes an initialized (<c>Ready</c>) tenant index.
/// </para>
/// </remarks>
public sealed record ConversationProjectionIndexSnapshot
{
    /// <summary>Gets the snapshot of an initialized (<c>Ready</c>) tenant index that holds no conversation.</summary>
    public static ConversationProjectionIndexSnapshot Empty { get; } = new();

    /// <summary>Gets the candidate summaries recorded in the tenant index.</summary>
    public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; init; } = [];

    /// <summary>Gets the latest dispatch reference per conversation identifier.</summary>
    public IReadOnlyDictionary<string, ConversationProjectionDispatchReference> Dispatches { get; init; }
        = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether any dispatch reference names a generation the index summaries do not
    /// yet reflect, so the tenant may be holding an accepted conversation that no page can show.
    /// </summary>
    public bool HasIncompleteDispatch { get; init; }

    /// <summary>Gets how the read store mapped the persisted lifecycle record; <c>Ready</c> by default.</summary>
    internal ConversationProjectionIndexReadState ReadState { get; init; }

    /// <summary>Gets the snapshot of an authorized tenant whose index record does not exist.</summary>
    internal static ConversationProjectionIndexSnapshot Missing { get; } = new() { ReadState = ConversationProjectionIndexReadState.Missing };
}
