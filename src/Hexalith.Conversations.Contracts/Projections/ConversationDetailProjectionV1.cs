// <copyright file="ConversationDetailProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Tenant-scoped v1 conversation detail read model.
/// </summary>
/// <param name="schemaVersion">The public projection schema version.</param>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The tenant-scoped conversation identity.</param>
/// <param name="freshness">The server-computed freshness metadata.</param>
/// <param name="lifecycleState">The safe lifecycle state token.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="projectId">An optional stable project reference.</param>
/// <param name="folderId">An optional stable folder reference.</param>
/// <param name="providerCorrelation">Optional safe provider correlation metadata.</param>
/// <param name="participants">Stable participant references.</param>
/// <param name="messages">Visible timeline messages.</param>
/// <param name="fileReferences">Stable file references.</param>
/// <param name="attributes">Safe adopter metadata.</param>
public sealed record ConversationDetailProjectionV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshnessV1 Freshness,
    string LifecycleState,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    ProviderCorrelationMetadata? ProviderCorrelation = null,
    IReadOnlyList<ConversationParticipantProjectionV1>? Participants = null,
    IReadOnlyList<ConversationTimelineMessageProjectionV1>? Messages = null,
    IReadOnlyList<ConversationFileReferenceProjectionV1>? FileReferences = null,
    IReadOnlyDictionary<string, string>? Attributes = null)
{
    /// <summary>
    /// Gets the public projection schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>
    /// Gets the tenant-scoped conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>
    /// Gets the server-computed freshness metadata.
    /// </summary>
    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    /// <summary>
    /// Gets the safe lifecycle state token.
    /// </summary>
    public string LifecycleState { get; } = ValidateLifecycle(LifecycleState);

    /// <summary>
    /// Gets stable participant references.
    /// </summary>
    public IReadOnlyList<ConversationParticipantProjectionV1> Participants { get; } = ValidateList(Participants, nameof(Participants));

    /// <summary>
    /// Gets visible timeline messages.
    /// </summary>
    public IReadOnlyList<ConversationTimelineMessageProjectionV1> Messages { get; } = ValidateList(Messages, nameof(Messages));

    /// <summary>
    /// Gets stable file references.
    /// </summary>
    public IReadOnlyList<ConversationFileReferenceProjectionV1> FileReferences { get; } = ValidateList(FileReferences, nameof(FileReferences));

    /// <summary>
    /// Gets safe adopter metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; } = ValidateAttributes(Attributes);

    private static string ValidateLifecycle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value switch
        {
            "Initializing" or "Open" or "Closed" or "Archived" => value,
            _ => throw new ArgumentException("Unsupported conversation lifecycle state.", nameof(value)),
        };
    }

    private static IReadOnlyList<T> ValidateList<T>(IReadOnlyList<T>? values, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<T>();
        }

        return values.Any(value => value is null)
            ? throw new ArgumentException("Projection lists must not contain null elements.", parameterName)
            : values;
    }

    private static IReadOnlyDictionary<string, string> ValidateAttributes(IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        foreach (KeyValuePair<string, string> attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Key))
            {
                throw new ArgumentException("Projection attribute keys must be non-empty.", nameof(attributes));
            }

            if (attribute.Value is null)
            {
                throw new ArgumentException("Projection attribute values must not be null.", nameof(attributes));
            }
        }

        return attributes;
    }
}
