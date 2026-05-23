// <copyright file="ConversationProjectionFreshnessClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Maps closed-vocabulary projection trust states and freshness reason codes to bounded signal classification enums.
/// </summary>
public static class ConversationProjectionFreshnessClassifier
{
    /// <summary>
    /// Maps a <see cref="ProjectionTrustState"/> to a <see cref="ConversationProjectionFreshnessClass"/>.
    /// </summary>
    /// <param name="state">The projection trust state.</param>
    /// <param name="reasonCode">The projection freshness reason code.</param>
    /// <returns>The bounded freshness class for telemetry signals.</returns>
    public static ConversationProjectionFreshnessClass Classify(ProjectionTrustState state, ProjectionFreshnessReasonCode reasonCode)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(reasonCode);

        if (state == ProjectionTrustState.Current || state == ProjectionTrustState.Redacted)
        {
            return ConversationProjectionFreshnessClass.Current;
        }

        if (state == ProjectionTrustState.Stale)
        {
            return ConversationProjectionFreshnessClass.Stale;
        }

        if (state == ProjectionTrustState.Rebuilding)
        {
            return ConversationProjectionFreshnessClass.Rebuilding;
        }

        // Forbidden collapses to Unavailable to prevent side-channel disclosure
        if (state == ProjectionTrustState.Unavailable || state == ProjectionTrustState.Forbidden)
        {
            return ConversationProjectionFreshnessClass.Unavailable;
        }

        return ConversationProjectionFreshnessClass.Unavailable;
    }

    /// <summary>
    /// Maps a <see cref="ProjectionFreshnessReasonCode"/> to a <see cref="ConversationProjectionLagClass"/>.
    /// </summary>
    /// <param name="reasonCode">The projection freshness reason code.</param>
    /// <returns>The bounded lag class for telemetry signals.</returns>
    public static ConversationProjectionLagClass ClassifyLag(ProjectionFreshnessReasonCode reasonCode)
    {
        ArgumentNullException.ThrowIfNull(reasonCode);

        if (reasonCode == ProjectionFreshnessReasonCode.Current
            || reasonCode == ProjectionFreshnessReasonCode.Rebuilding
            || reasonCode == ProjectionFreshnessReasonCode.Redacted)
        {
            return ConversationProjectionLagClass.WithinThreshold;
        }

        if (reasonCode == ProjectionFreshnessReasonCode.StaleThresholdExceeded)
        {
            return ConversationProjectionLagClass.ThresholdBreached;
        }

        if (reasonCode == ProjectionFreshnessReasonCode.GapDetected
            || reasonCode == ProjectionFreshnessReasonCode.OutOfOrderEvent
            || reasonCode == ProjectionFreshnessReasonCode.PoisonEvent
            || reasonCode == ProjectionFreshnessReasonCode.MetadataWriteFailed
            || reasonCode == ProjectionFreshnessReasonCode.MetadataContradictory
            || reasonCode == ProjectionFreshnessReasonCode.MixedGeneration)
        {
            return ConversationProjectionLagClass.CriticalLag;
        }

        if (reasonCode == ProjectionFreshnessReasonCode.Unavailable)
        {
            return ConversationProjectionLagClass.Unavailable;
        }

        // Forbidden — do not emit lag signal
        if (reasonCode == ProjectionFreshnessReasonCode.Forbidden)
        {
            return ConversationProjectionLagClass.None;
        }

        return ConversationProjectionLagClass.Unavailable;
    }
}
