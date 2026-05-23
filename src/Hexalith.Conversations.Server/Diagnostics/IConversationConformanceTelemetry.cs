// <copyright file="IConversationConformanceTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits content-safe, bounded-cardinality signals for conformance outcome observations.
/// </summary>
public interface IConversationConformanceTelemetry
{
    /// <summary>
    /// Records a conformance outcome signal with bounded dimensions and no sensitive content.
    /// </summary>
    /// <param name="statusClass">The bounded conformance status class.</param>
    /// <param name="safeGateId">The bounded safe gate identifier token from the approved vocabulary.</param>
    /// <param name="isBlocking">Whether this outcome blocks the release gate.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    void RecordConformanceOutcome(
        ConversationConformanceStatusClass statusClass,
        string safeGateId,
        bool isBlocking,
        string correlationId);
}
