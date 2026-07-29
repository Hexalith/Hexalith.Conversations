// <copyright file="ProjectionIndexSnapshotTestExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Builds index snapshots for test doubles that only care about which summaries a tenant holds.
/// </summary>
/// <remarks>
/// Fakes that stand in for the read store are not exercising cross-key verification — the real
/// <see cref="ConversationProjectionReadStore"/> tests own that. This produces a snapshot whose dispatch
/// references agree with their summaries, so such a fake models a tenant with no in-flight dispatch rather
/// than accidentally modelling a permanently inconsistent one.
/// </remarks>
internal static class ProjectionIndexSnapshotTestExtensions
{
    /// <summary>The dispatch identity a consistent test snapshot attributes its generations to.</summary>
    internal const string TestDispatchId = "test-dispatch";

    /// <summary>Builds a fully consistent snapshot from a candidate summary set.</summary>
    /// <param name="summaries">The candidate summaries the tenant index holds.</param>
    /// <returns>A snapshot with one matching dispatch reference per summary.</returns>
    internal static ConversationProjectionIndexSnapshot ToConsistentSnapshot(
        this IReadOnlyList<ConversationSummaryProjectionV1> summaries)
        => new()
        {
            Summaries = summaries,
            Dispatches = summaries.ToDictionary(
                static summary => summary.ConversationId.Value,
                static summary => new ConversationProjectionDispatchReference(
                    TestDispatchId,
                    summary.Freshness.LastAppliedEventPosition),
                StringComparer.Ordinal),
            HasIncompleteDispatch = false,
        };

    /// <summary>The verification result a consistent fake returns: nothing is withheld.</summary>
    /// <returns>An empty inconsistent-identifier set.</returns>
    internal static IReadOnlySet<string> NoInconsistentRows()
        => new HashSet<string>(StringComparer.Ordinal);
}
