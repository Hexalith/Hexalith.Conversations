// <copyright file="ConversationDetailsV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Tenant-scoped conversation detail returned by authorized retrieve queries.
/// </summary>
public sealed record ConversationDetailsV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshnessV1 Freshness,
    string LifecycleState,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    ConversationProviderCorrelationV1? ProviderCorrelation = null,
    IReadOnlyList<ConversationParticipantProjectionV1>? Participants = null,
    IReadOnlyList<ConversationTimelineMessageProjectionV1>? Messages = null,
    IReadOnlyList<ConversationFileReferenceProjectionV1>? FileReferences = null,
    string? GovernanceState = null,
    IReadOnlyDictionary<string, string>? Attributes = null)
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
    /// Gets stable participant references.
    /// </summary>
    public IReadOnlyList<ConversationParticipantProjectionV1> Participants { get; } = ValidateList(Participants, nameof(Participants));

    /// <summary>
    /// Gets ordered visible timeline metadata.
    /// </summary>
    public IReadOnlyList<ConversationTimelineMessageProjectionV1> Messages { get; } = ValidateList(Messages, nameof(Messages));

    /// <summary>
    /// Gets stable file references.
    /// </summary>
    public IReadOnlyList<ConversationFileReferenceProjectionV1> FileReferences { get; } = ValidateList(FileReferences, nameof(FileReferences));

    /// <summary>
    /// Gets safe adopter metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; } = Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Creates query details from an approved projection.
    /// </summary>
    /// <param name="projection">The source projection.</param>
    /// <returns>The public query details.</returns>
    public static ConversationDetailsV1 FromProjection(ConversationDetailProjectionV1 projection)
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
            ConversationProviderCorrelationV1.From(projection.ProviderCorrelation),
            projection.Participants,
            projection.Messages,
            projection.FileReferences,
            GovernanceState: "Unavailable",
            projection.Attributes);
    }

    private static IReadOnlyList<T> ValidateList<T>(IReadOnlyList<T>? values, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<T>();
        }

        return values.Any(value => value is null)
            ? throw new ArgumentException("Query lists must not contain null elements.", parameterName)
            : values;
    }
}
