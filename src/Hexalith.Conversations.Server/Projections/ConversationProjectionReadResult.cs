// <copyright file="ConversationProjectionReadResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Carries a non-disclosing projection read outcome.
/// </summary>
/// <param name="FreshnessState">The public freshness state for the read outcome.</param>
/// <param name="ReasonCode">The safe public freshness reason code.</param>
/// <param name="Projection">The detail projection when safely visible.</param>
/// <param name="IsAvailableForTrustBearingActions">Whether dependent trust-bearing actions are enabled.</param>
public sealed record ConversationProjectionReadResult(
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode,
    ConversationDetailProjectionV1? Projection,
    bool IsAvailableForTrustBearingActions)
{
    /// <summary>
    /// Gets the public freshness state for the read outcome.
    /// </summary>
    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    /// <summary>
    /// Gets the safe public freshness reason code.
    /// </summary>
    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));
}
