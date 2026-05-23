// <copyright file="IConversationProjectionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits content-safe, bounded-cardinality signals for projection freshness and publication failures.
/// </summary>
public interface IConversationProjectionTelemetry
{
    /// <summary>
    /// Records a projection freshness state signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="freshnessClass">The bounded freshness class.</param>
    /// <param name="lagClass">The bounded lag class.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordProjectionFreshnessState(
        ConversationProjectionFreshnessClass freshnessClass,
        ConversationProjectionLagClass lagClass,
        string correlationId);

    /// <summary>
    /// Records a projection rebuild progress signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="rebuildClass">The bounded rebuild freshness class.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordProjectionRebuildProgress(
        ConversationProjectionFreshnessClass rebuildClass,
        string correlationId);

    /// <summary>
    /// Records a publication failure signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="failureClass">The bounded publication failure class.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordPublicationFailure(
        ConversationPublicationFailureClass failureClass,
        string correlationId);
}
