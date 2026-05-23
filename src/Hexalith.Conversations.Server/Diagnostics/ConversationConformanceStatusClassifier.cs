// <copyright file="ConversationConformanceStatusClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Maps conformance outcomes, failure classifications, and gate statuses to bounded status classes.
/// </summary>
/// <remarks>
/// Classification takes precedence over outcome: non-<c>Conformant</c> classifications always override
/// the observed outcome. Only when <c>classification == Conformant</c> is the outcome consulted.
/// <c>Waived</c> is exclusively reachable via <see cref="ClassifyGate"/>; the check-level
/// <see cref="Classify"/> API cannot produce <c>Waived</c> because waiver state is a gate aggregation
/// decision, not a per-check classification.
/// </remarks>
public static class ConversationConformanceStatusClassifier
{
    /// <summary>
    /// Maps a conformance outcome and failure classification to a bounded status class.
    /// </summary>
    /// <param name="outcome">The observed conformance outcome.</param>
    /// <param name="classification">The failure classification for the check.</param>
    /// <returns>The bounded conformance status class.</returns>
    public static ConversationConformanceStatusClass Classify(
        ConformanceOutcome outcome,
        ConformanceFailureClassification classification)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(classification);

        if (classification.Equals(ConformanceFailureClassification.ProductInvariant))
        {
            return ConversationConformanceStatusClass.Fail;
        }

        if (classification.Equals(ConformanceFailureClassification.Infrastructure))
        {
            return ConversationConformanceStatusClass.InfrastructureFailure;
        }

        if (classification.Equals(ConformanceFailureClassification.UnavailableDependency))
        {
            return ConversationConformanceStatusClass.InfrastructureFailure;
        }

        if (classification.Equals(ConformanceFailureClassification.Execution))
        {
            return ConversationConformanceStatusClass.ExecutionFailure;
        }

        if (classification.Equals(ConformanceFailureClassification.Configuration))
        {
            return ConversationConformanceStatusClass.ExecutionFailure;
        }

        // classification == Conformant: consult outcome
        if (outcome.Equals(ConformanceOutcome.Ready))
        {
            return ConversationConformanceStatusClass.Pass;
        }

        if (outcome.Equals(ConformanceOutcome.Blocked))
        {
            // Conformant + Blocked = fail-closed observed correctly per contract
            return ConversationConformanceStatusClass.Pass;
        }

        if (outcome.Equals(ConformanceOutcome.Degraded))
        {
            return ConversationConformanceStatusClass.StaleEvidence;
        }

        // ConformanceOutcome.Unknown + Conformant
        return ConversationConformanceStatusClass.UnknownAccepted;
    }

    /// <summary>
    /// Maps a release gate status to a bounded status class.
    /// </summary>
    /// <param name="status">The release gate status.</param>
    /// <returns>The bounded conformance status class.</returns>
    public static ConversationConformanceStatusClass ClassifyGate(ReleaseGateStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Equals(ReleaseGateStatus.Pass))
        {
            return ConversationConformanceStatusClass.Pass;
        }

        if (status.Equals(ReleaseGateStatus.Fail))
        {
            return ConversationConformanceStatusClass.Fail;
        }

        if (status.Equals(ReleaseGateStatus.Waived))
        {
            return ConversationConformanceStatusClass.Waived;
        }

        // ReleaseGateStatus.UnknownAccepted
        return ConversationConformanceStatusClass.UnknownAccepted;
    }
}
