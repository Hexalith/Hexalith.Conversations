// <copyright file="ConversationState.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Immutable;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;

namespace Hexalith.Conversations.State;

/// <summary>
/// Replay-only state for a tenant-scoped conversation.
/// </summary>
/// <remarks>
/// Declared as a sealed class (not a record) because the state carries identity through a
/// rebuilt-on-each-Apply <see cref="ImmutableArray{T}"/> of participants. Record value-equality
/// would be misleading over private collection fields, and the synthesized <c>with</c> copy
/// constructor would share a mutable backing list across clones.
/// </remarks>
public sealed class ConversationState
{
    private ImmutableArray<ConversationParticipant> _participants = ImmutableArray<ConversationParticipant>.Empty;

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
    /// Gets the deterministic timestamp of the most recently applied event, when any.
    /// </summary>
    public DateTimeOffset? LastEventAt { get; private set; }

    /// <summary>
    /// Gets optional provider correlation metadata that is never authority.
    /// </summary>
    public ProviderCorrelationMetadata? ProviderCorrelation { get; private set; }

    /// <summary>
    /// Gets the replayed participant membership as an immutable snapshot.
    /// </summary>
    public IReadOnlyList<ConversationParticipant> Participants => _participants;

    /// <summary>
    /// Determines whether a participant membership already exists.
    /// </summary>
    /// <param name="partyId">The stable Party reference.</param>
    /// <param name="participantType">The participant type.</param>
    /// <param name="participantRole">The participant role.</param>
    /// <returns><see langword="true" /> when matching membership exists.</returns>
    public bool HasParticipant(PartyId partyId, ParticipantType participantType, ParticipantRole participantRole)
        => _participants.Any(p => p.PartyId == partyId
            && p.ParticipantType == participantType
            && p.ParticipantRole == participantRole);

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
        LastEventAt = e.Metadata.CommittedAt;
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
    /// Applies a participant-added event during deterministic replay.
    /// </summary>
    /// <remarks>
    /// Replay is idempotent: a duplicate event in the stream (snapshot pointer not advanced,
    /// dispatcher retry) is treated as a no-op so the aggregate remains loadable. Command-time
    /// duplicate detection is the authority for rejecting duplicate membership and lives in the
    /// aggregate validation, not here.
    /// </remarks>
    /// <param name="e">The participant-added event.</param>
    public void Apply(ParticipantAddedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (HasParticipant(e.ParticipantPartyId, e.ParticipantType, e.ParticipantRole))
        {
            return;
        }

        _participants = _participants.Add(new ConversationParticipant(
            e.ParticipantPartyId,
            e.ParticipantType,
            e.ParticipantRole,
            e.AddedAt,
            e.Metadata.ActorPartyId));
        LastEventAt = e.Metadata.CommittedAt;
    }

    /// <summary>
    /// Applies a rejection event as a no-op during replay.
    /// </summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ConversationRejectedDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
    }

    /// <summary>
    /// Test-only seam for setting <see cref="Lifecycle"/> directly. Exposed to
    /// <c>Hexalith.Conversations.Tests</c> via <c>InternalsVisibleTo</c>.
    /// </summary>
    /// <remarks>
    /// Production code must never call this. Lifecycle transitions in production must flow through
    /// the corresponding <c>Apply</c> overload for an emitted domain event. This seam exists so
    /// participant tests can set up Closed/Archived states without depending on close/archive
    /// commands, which are owned by a future story.
    /// </remarks>
    /// <param name="lifecycle">The lifecycle state to force.</param>
    internal void ForceLifecycleForTests(ConversationLifecycleState lifecycle) => Lifecycle = lifecycle;
}
