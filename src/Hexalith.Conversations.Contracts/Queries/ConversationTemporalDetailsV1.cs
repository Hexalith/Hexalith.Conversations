// <copyright file="ConversationTemporalDetailsV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Tenant-scoped conversation detail reconstructed at a safe temporal anchor.
/// </summary>
public sealed record ConversationTemporalDetailsV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    ConversationTemporalAnchorV1 TemporalAnchor,
    ConversationTemporalConfidenceV1 Confidence,
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
    ConversationRetentionPolicyProjectionV1? ActiveRetentionPolicy = null,
    string CurrentDisclosureState = "Applied",
    IReadOnlyDictionary<string, string>? Attributes = null,
    IReadOnlyList<ConversationSensitivityMarkProjectionV1>? SensitivityMarks = null,
    IReadOnlyList<ConversationRedactionProjectionV1>? Redactions = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public ConversationTemporalAnchorV1 TemporalAnchor { get; } =
        TemporalAnchor ?? throw new ArgumentNullException(nameof(TemporalAnchor));

    public ConversationTemporalConfidenceV1 Confidence { get; } =
        Confidence ?? throw new ArgumentNullException(nameof(Confidence));

    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    public string LifecycleState { get; } = ValidateRequired(LifecycleState, nameof(LifecycleState));

    public IReadOnlyList<ConversationParticipantProjectionV1> Participants { get; } =
        ValidateList(Participants, nameof(Participants));

    public IReadOnlyList<ConversationTimelineMessageProjectionV1> Messages { get; } =
        ValidateList(Messages, nameof(Messages));

    public IReadOnlyList<ConversationFileReferenceProjectionV1> FileReferences { get; } =
        ValidateList(FileReferences, nameof(FileReferences));

    public string CurrentDisclosureState { get; } = ValidateRequired(CurrentDisclosureState, nameof(CurrentDisclosureState));

    public IReadOnlyDictionary<string, string> Attributes { get; } =
        Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<ConversationSensitivityMarkProjectionV1> SensitivityMarks { get; } =
        ValidateList(SensitivityMarks, nameof(SensitivityMarks));

    public IReadOnlyList<ConversationRedactionProjectionV1> Redactions { get; } =
        ValidateList(Redactions, nameof(Redactions));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static IReadOnlyList<T> ValidateList<T>(IReadOnlyList<T>? values, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<T>();
        }

        return values.Any(value => value is null)
            ? throw new ArgumentException("Temporal detail lists must not contain null elements.", parameterName)
            : values;
    }
}
