// <copyright file="ConversationPublicationService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Commons.Publication;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Non-static wrapper around <see cref="ConversationPublicationMapper"/> that emits bounded publication failure signals.
/// </summary>
public sealed class ConversationPublicationService(IConversationProjectionTelemetry? telemetry = null)
{
    private readonly IConversationProjectionTelemetry? _telemetry = telemetry;

    /// <summary>
    /// Maps a persisted candidate to a publishable public event or a bounded diagnostic, emitting a failure signal when rejected.
    /// </summary>
    /// <param name="persisted">The persisted candidate.</param>
    /// <param name="correlationId">An optional safe correlation identifier override.</param>
    /// <returns>The publication result.</returns>
    public ConversationPublicationResult TryMap(PersistedConversationEvent persisted, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(persisted);

        ConversationPublicationResult result = ConversationPublicationMapper.TryMap(persisted);

        if (_telemetry is not null)
        {
            PublicationFailureTelemetry.RecordRejected(
                result.IsPublished,
                result.Diagnostic,
                static diagnostic => ConversationPublicationFailureClassifier.Classify(diagnostic.Code),
                _telemetry.RecordPublicationFailure,
                correlationId);
        }

        return result;
    }
}
