// <copyright file="ConversationState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.State;

/// <summary>
/// Replay-only state for a tenant-scoped conversation.
/// </summary>
public sealed record ConversationState
{
    /// <summary>
    /// Gets a value indicating whether the conversation was created.
    /// </summary>
    public bool IsCreated { get; private set; }

    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public ConversationLifecycleState Lifecycle { get; private set; } = ConversationLifecycleState.NotCreated;

    /// <summary>
    /// Gets the tenant binding copied from persisted event data.
    /// </summary>
    public TenantId? TenantId { get; private set; }

    /// <summary>
    /// Gets the internal Conversations-owned identity copied from persisted event data.
    /// </summary>
    public ConversationId? ConversationId { get; private set; }

    /// <summary>
    /// Gets the creator Party attribution copied from persisted event data.
    /// </summary>
    public PartyId? CreatorPartyId { get; private set; }

    /// <summary>
    /// Gets the deterministic creation timestamp copied from persisted event data.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <summary>
    /// Gets the schema version copied from persisted event data.
    /// </summary>
    public SchemaVersion? SchemaVersion { get; private set; }

    /// <summary>
    /// Gets the correlation identifier copied from persisted event data.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Gets the causation identifier copied from persisted event data.
    /// </summary>
    public string? CausationId { get; private set; }

    /// <summary>
    /// Gets the idempotency key copied from command metadata when supplied.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// Gets the optional adopter-owned business reference.
    /// </summary>
    public BusinessReference? BusinessReference { get; private set; }

    /// <summary>
    /// Gets the optional stable project reference.
    /// </summary>
    public ProjectId? ProjectId { get; private set; }

    /// <summary>
    /// Gets the optional stable folder reference.
    /// </summary>
    public FolderId? FolderId { get; private set; }

    /// <summary>
    /// Gets the optional UI label that is not identity.
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// Gets optional provider correlation metadata that is never authority.
    /// </summary>
    public ProviderCorrelationMetadata? ProviderCorrelation { get; private set; }

    /// <summary>
    /// Applies a conversation-created event during deterministic replay.
    /// </summary>
    /// <param name="e">The conversation-created event.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the state has already been created. Duplicate <see cref="ConversationCreatedDomainEvent"/>
    /// entries indicate a corrupted or replayed stream and must surface as a deterministic invariant
    /// violation rather than silently overwriting tenant binding, creator attribution, or timestamps.
    /// </exception>
    public void Apply(ConversationCreatedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (IsCreated)
        {
            throw new InvalidOperationException(
                "ConversationCreatedDomainEvent applied to an already-created ConversationState. "
                + "Duplicate creation in event history violates the replay invariant.");
        }

        IsCreated = true;
        Lifecycle = ConversationLifecycleState.Open;
        TenantId = e.Metadata.TenantId;
        ConversationId = e.Metadata.ConversationId;
        CreatorPartyId = e.Metadata.ActorPartyId;
        CreatedAt = e.Metadata.CommittedAt;
        SchemaVersion = e.Metadata.SchemaVersion;
        CorrelationId = e.Metadata.CorrelationId;
        CausationId = e.Metadata.CausationId;
        BusinessReference = e.BusinessReference;
        ProjectId = e.ProjectId;
        FolderId = e.FolderId;
        Label = e.Label;
        IdempotencyKey = e.IdempotencyKey;
        ProviderCorrelation = e.ProviderCorrelation;
    }

    /// <summary>
    /// Applies a rejection event as a no-op during replay.
    /// </summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ConversationRejectedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
    }
}
