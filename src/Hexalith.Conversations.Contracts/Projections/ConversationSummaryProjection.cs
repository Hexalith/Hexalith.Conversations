// <copyright file="ConversationSummaryProjection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Minimal adopter-facing conversation summary contract.
/// </summary>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="freshness">The freshness and trust state.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="participantPartyIds">Stable participant Party references; null is normalized to empty.</param>
public sealed record ConversationSummaryProjection(
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshness Freshness,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    IReadOnlyList<PartyId>? ParticipantPartyIds = null)
{
    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the tenant-scoped conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the freshness and trust state.
    /// </summary>
    public ProjectionFreshness Freshness { get; } = RequireNonNull(Freshness, nameof(Freshness));

    /// <summary>
    /// Gets stable participant Party references. Null inputs normalize to empty.
    /// </summary>
    public IReadOnlyList<PartyId> ParticipantPartyIds { get; } = ValidateParticipantPartyIds(ParticipantPartyIds);

    private static T RequireNonNull<T>(T value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    private static IReadOnlyList<PartyId> ValidateParticipantPartyIds(IReadOnlyList<PartyId>? participants)
    {
        if (participants is null || participants.Count == 0)
        {
            return Array.Empty<PartyId>();
        }

        return participants.Any(party => party is null)
            ? throw new ArgumentException("Participant Party references must not contain null elements.", nameof(participants))
            : participants;
    }
}
