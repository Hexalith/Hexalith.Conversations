// <copyright file="ConversationSummaryProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Tenant-scoped v1 conversation summary read model.
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
/// <param name="participantPartyIds">Stable participant Party references.</param>
/// <param name="messageCount">The projected message count.</param>
/// <param name="fileReferenceCount">The projected file-reference count.</param>
/// <param name="providerCorrelation">Optional safe provider correlation metadata.</param>
public sealed record ConversationSummaryProjectionV1(
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
    ProviderCorrelationMetadata? ProviderCorrelation = null)
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
    /// Gets stable participant Party references.
    /// </summary>
    public IReadOnlyList<PartyId> ParticipantPartyIds { get; } = ValidateParticipants(ParticipantPartyIds);

    /// <summary>
    /// Gets the projected message count.
    /// </summary>
    public int MessageCount { get; } = ValidateCount(MessageCount, nameof(MessageCount));

    /// <summary>
    /// Gets the projected file-reference count.
    /// </summary>
    public int FileReferenceCount { get; } = ValidateCount(FileReferenceCount, nameof(FileReferenceCount));

    private static string ValidateLifecycle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value switch
        {
            "Initializing" or "Open" or "Closed" or "Archived" => value,
            _ => throw new ArgumentException("Unsupported conversation lifecycle state.", nameof(value)),
        };
    }

    private static IReadOnlyList<PartyId> ValidateParticipants(IReadOnlyList<PartyId>? participants)
    {
        if (participants is null || participants.Count == 0)
        {
            return Array.Empty<PartyId>();
        }

        return participants.Any(partyId => partyId is null)
            ? throw new ArgumentException("Participant Party references must not contain null elements.", nameof(participants))
            : participants;
    }

    private static int ValidateCount(int value, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
        return value;
    }
}
