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
    IReadOnlyDictionary<string, string>? Attributes = null,
    IReadOnlyList<PartyReferenceHydrationV1>? PartyHydration = null,
    ProjectReferenceHydrationV1? ProjectHydration = null,
    FolderReferenceHydrationV1? FolderHydration = null,
    IReadOnlyList<FileReferenceHydrationV1>? FileHydration = null,
    IReadOnlyList<ConversationSensitivityMarkProjectionV1>? SensitivityMarks = null)
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
    /// Gets derived sensitivity state for authorized reads.
    /// </summary>
    public IReadOnlyList<ConversationSensitivityMarkProjectionV1> SensitivityMarks { get; } =
        ValidateList(SensitivityMarks, nameof(SensitivityMarks));

    /// <summary>
    /// Gets safe adopter metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; } = Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets response-scoped Party reference hydration.
    /// </summary>
    public IReadOnlyList<PartyReferenceHydrationV1> PartyHydration { get; } = ValidateList(PartyHydration, nameof(PartyHydration));

    /// <summary>
    /// Gets response-scoped project reference hydration.
    /// </summary>
    public ProjectReferenceHydrationV1? ProjectHydration { get; } = ProjectHydration;

    /// <summary>
    /// Gets response-scoped folder reference hydration.
    /// </summary>
    public FolderReferenceHydrationV1? FolderHydration { get; } = FolderHydration;

    /// <summary>
    /// Gets response-scoped file reference hydration.
    /// </summary>
    public IReadOnlyList<FileReferenceHydrationV1> FileHydration { get; } = ValidateList(FileHydration, nameof(FileHydration));

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
            Attributes: projection.Attributes,
            SensitivityMarks: projection.SensitivityMarks);
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
