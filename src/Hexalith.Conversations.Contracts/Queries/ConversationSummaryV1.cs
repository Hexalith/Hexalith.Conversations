// <copyright file="ConversationSummaryV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Tenant-scoped conversation summary returned by authorized list queries.
/// </summary>
public sealed record ConversationSummaryV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshnessV1 Freshness,
    string LifecycleState,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    IReadOnlyList<PartyId>? ParticipantPartyIds = null,
    int MessageCount = 0,
    int FileReferenceCount = 0,
    ConversationProviderCorrelationV1? ProviderCorrelation = null,
    IReadOnlyList<PartyReferenceHydrationV1>? PartyHydration = null,
    ProjectReferenceHydrationV1? ProjectHydration = null,
    FolderReferenceHydrationV1? FolderHydration = null)
{
    /// <summary>
    /// Gets the public schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>
    /// Gets the Conversations-owned conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>
    /// Gets projection freshness context.
    /// </summary>
    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    /// <summary>
    /// Gets stable participant Party references.
    /// </summary>
    public IReadOnlyList<PartyId> ParticipantPartyIds { get; } = ValidateParticipants(ParticipantPartyIds);

    /// <summary>
    /// Gets response-scoped Party reference hydration.
    /// </summary>
    public IReadOnlyList<PartyReferenceHydrationV1> PartyHydration { get; } = ValidateHydration(PartyHydration, nameof(PartyHydration));

    /// <summary>
    /// Gets response-scoped project reference hydration.
    /// </summary>
    public ProjectReferenceHydrationV1? ProjectHydration { get; } = ProjectHydration;

    /// <summary>
    /// Gets response-scoped folder reference hydration.
    /// </summary>
    public FolderReferenceHydrationV1? FolderHydration { get; } = FolderHydration;

    /// <summary>
    /// Creates a query summary from an approved projection.
    /// </summary>
    /// <param name="projection">The source projection.</param>
    /// <returns>The public query summary.</returns>
    public static ConversationSummaryV1 FromProjection(ConversationSummaryProjectionV1 projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return new(
            projection.SchemaVersion,
            projection.TenantId,
            projection.ConversationId,
            projection.Freshness,
            projection.LifecycleState,
            projection.Label,
            projection.BusinessReference,
            projection.ProjectId,
            projection.FolderId,
            projection.ParticipantPartyIds,
            projection.MessageCount,
            projection.FileReferenceCount,
            ConversationProviderCorrelationV1.From(projection.ProviderCorrelation));
    }

    private static IReadOnlyList<PartyId> ValidateParticipants(IReadOnlyList<PartyId>? participants)
    {
        if (participants is null || participants.Count == 0)
        {
            return Array.Empty<PartyId>();
        }

        return participants.Any(participant => participant is null)
            ? throw new ArgumentException("Participant Party references must not contain null elements.", nameof(participants))
            : participants;
    }

    private static IReadOnlyList<T> ValidateHydration<T>(IReadOnlyList<T>? values, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<T>();
        }

        return values.Any(value => value is null)
            ? throw new ArgumentException("Hydration lists must not contain null elements.", parameterName)
            : values;
    }
}
