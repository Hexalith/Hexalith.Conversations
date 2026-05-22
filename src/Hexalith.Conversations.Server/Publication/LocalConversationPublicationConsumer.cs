// <copyright file="LocalConversationPublicationConsumer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Small deterministic consumer used to document default idempotency semantics.
/// </summary>
public sealed class LocalConversationPublicationConsumer(TenantId tenantId)
{
    private readonly HashSet<string> _appliedIdentities = new(StringComparer.Ordinal);
    private readonly HashSet<string> _participantIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the number of applied effects.
    /// </summary>
    public int AppliedEffectCount { get; private set; }

    /// <summary>
    /// Gets the participant identifiers applied by the consumer.
    /// </summary>
    public IReadOnlyList<string> ParticipantIds => _participantIds.Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Applies a public event if its default idempotency identity has not been seen.
    /// </summary>
    /// <param name="e">The public event.</param>
    /// <returns><c>true</c> when a new effect was applied.</returns>
    public bool TryApply(object e)
    {
        ConversationEventMetadata? metadata = ConversationPublicationMetadata.GetMetadata(e);
        if (metadata is null || !tenantId.Equals(metadata.TenantId) || metadata.SchemaVersion.Value != SchemaVersion.Current.Value)
        {
            return false;
        }

        if (!_appliedIdentities.Add(metadata.DeduplicationKey))
        {
            return false;
        }

        if (e is ParticipantAdded participant)
        {
            _participantIds.Add(participant.ParticipantPartyId.Value);
        }

        AppliedEffectCount++;
        return true;
    }
}
