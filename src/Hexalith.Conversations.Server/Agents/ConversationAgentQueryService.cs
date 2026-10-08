// <copyright file="ConversationAgentQueryService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.State;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Current source reads gated before lookup and reconfirmed after awaited source reads.</summary>
/// <param name="authority">Current operation scope and immutable Party authority.</param>
/// <param name="sources">Complete exact source replay.</param>
/// <param name="catalogue">Independently authenticated complete tenant catalogue.</param>
public sealed class ConversationAgentQueryService(IConversationAgentAuthority authority,
    ConversationAgentSourceReader sources, IConversationTenantCatalogue catalogue)
{
    /// <summary>Reads current membership, content, roster, accessibility or exact message.</summary>
    /// <param name="principal">Authenticated service principal.</param>
    /// <param name="query">Exact tenant/source request.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed current result.</returns>
    public async Task<ConversationAgentReadResult> ReadAsync(string principal, ConversationAgentReadQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var admitted = await AuthorizeSafelyAsync(principal, query.TenantId, query.ConversationId, "AgentRead", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        ConversationAgentsOutcome outcome = CheckAuthority(admitted, principal, query.TenantId, requireOrganization: true);
        if (outcome != ConversationAgentsOutcome.Available)
        {
            return Hidden(query, outcome);
        }
        var read = await sources.ReadAsync(query.TenantId, query.ConversationId, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var current = await AuthorizeSafelyAsync(principal, query.TenantId, query.ConversationId, "AgentRead", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        outcome = CheckAuthority(current, principal, query.TenantId, requireOrganization: true);
        if (outcome != ConversationAgentsOutcome.Available)
        {
            return Hidden(query, outcome);
        }
        if (admitted != current || read is null)
        {
            return Hidden(query, ConversationAgentsOutcome.Unavailable);
        }
        var (source, state) = read.Value;
        if (!state.IsCreated)
        {
            return new(ConversationAgentsOutcome.Absent, query.TenantId, query.ConversationId, source.Head, source.ObservedAt, source.ObservationId);
        }
        if (state.IsDeleted)
        {
            return new(ConversationAgentsOutcome.ConversationDeleted, query.TenantId, query.ConversationId, source.Head, source.ObservedAt, source.ObservationId);
        }
        if (state.WasAgentRemoved(current.PartyId!))
        {
            return new(ConversationAgentsOutcome.PrincipalRemovedFromConversation, query.TenantId, query.ConversationId, source.Head, source.ObservedAt, source.ObservationId);
        }
        bool present = state.HasParticipant(current.PartyId!, Contracts.Participants.ParticipantType.AiAgent,
            Contracts.Participants.ParticipantRole.Member);
        if (!present)
        {
            return query.ParticipantStateOnly ? new(ConversationAgentsOutcome.Absent, query.TenantId, query.ConversationId, source.Head, source.ObservedAt, source.ObservationId) : Hidden(query, ConversationAgentsOutcome.Denied);
        }
        if (query.ParticipantStateOnly || query.AccessibilityOnly)
        {
            return new(ConversationAgentsOutcome.Available, query.TenantId, query.ConversationId, source.Head,
            source.ObservedAt, source.ObservationId, AgentParticipantPresent: true);
        }
        var messages = state.Messages.Where(m => query.MessageId is null || m.MessageId == query.MessageId)
            .Select(m => Visible(state, m)).ToArray();
        if (query.MessageId is not null && messages.Length == 0)
        {
            return new(ConversationAgentsOutcome.Absent, query.TenantId, query.ConversationId, source.Head,
            source.ObservedAt, source.ObservationId, AgentParticipantPresent: true);
        }
        var participants = state.Participants.Select(p => new ConversationParticipantProjectionV1(
            p.PartyId, p.ParticipantType, p.ParticipantRole, p.AddedAt)).ToArray();
        return new(ConversationAgentsOutcome.Available, query.TenantId, query.ConversationId, source.Head,
            source.ObservedAt, source.ObservationId, participants, messages, true);
    }

    /// <summary>Counts active source Conversations without granting unjoined content access.</summary>
    /// <param name="principal">Authenticated service principal.</param>
    /// <param name="query">Owner-accepted half-open UTC creation window; currently open/undeleted Conversations, including zero Agent Calls.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Complete denominator or Unavailable, never inferred zero.</returns>
    public async Task<ConversationActiveCountResult> CountAsync(string principal, ConversationActiveCountQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var admitted = await AuthorizeSafelyAsync(principal, query.TenantId, null, "AgentCount", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = CheckAuthority(admitted, principal, query.TenantId, true);
        if (outcome != ConversationAgentsOutcome.Available)
        {
            return new(outcome);
        }
        if (query.CreatedFromInclusive.Offset != TimeSpan.Zero || query.CreatedToExclusive.Offset != TimeSpan.Zero
            || query.CreatedFromInclusive >= query.CreatedToExclusive)
        {
            return new(ConversationAgentsOutcome.Invalid);
        }
        var result = await ReadCatalogueSafelyAsync(query, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var current = await AuthorizeSafelyAsync(principal, query.TenantId, null, "AgentCount", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        outcome = CheckAuthority(current, principal, query.TenantId, true);
        if (outcome != ConversationAgentsOutcome.Available)
        {
            return new(outcome);
        }
        if (admitted != current || result is null || result.Outcome != ConversationAgentsOutcome.Available || !result.Complete
            || result.Entries is null || string.IsNullOrWhiteSpace(result.Checkpoint) || result.ObservedAt is null || result.ObservedAt == DateTimeOffset.MinValue
            || result.Entries.Any(e => e is null || e.TenantId != query.TenantId || e.ConversationId is null || e.CreatedAt == default)
            || result.Entries.Select(e => e.ConversationId).Distinct().Count() != result.Entries.Count)
        {
            return new(ConversationAgentsOutcome.Unavailable);
        }
        long count = result.Entries.LongCount(e => e.Active && e.CreatedAt >= query.CreatedFromInclusive && e.CreatedAt < query.CreatedToExclusive);
        return new(ConversationAgentsOutcome.Available, count, result.Checkpoint, result.ObservedAt,
            query.TenantId, query.CreatedFromInclusive, query.CreatedToExclusive);
    }

    /// <summary>Resolves immutable source revision and receiver acknowledgement after lost delivery.</summary>
    /// <param name="principal">Authenticated source worker.</param>
    /// <param name="query">Exact source lookup.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Safe durable delivery state.</returns>
    public async Task<ConversationDeletionSourceResult> DeletionSourceAsync(string principal, ConversationDeletionSourceQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var admitted = await AuthorizeSafelyAsync(principal, query.TenantId, query.ConversationId, "DeletionSource", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = CheckAuthority(admitted, principal, query.TenantId, false);
        if (outcome != ConversationAgentsOutcome.Available)
        {
            return new(outcome);
        }
        var read = await sources.ReadAsync(query.TenantId, query.ConversationId, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var current = await AuthorizeSafelyAsync(principal, query.TenantId, query.ConversationId, "DeletionSource", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        outcome = CheckAuthority(current, principal, query.TenantId, false);
        if (outcome != ConversationAgentsOutcome.Available)
        {
            return new(outcome);
        }
        if (admitted != current || read is null)
        {
            return new(ConversationAgentsOutcome.Unavailable);
        }
        var result = read.Value.State.DeletionSource;
        if (result.Signal is { } signal && ((query.SourceRevision is not null && query.SourceRevision != signal.SourceRevision)
            || (query.SignalId is not null && query.SignalId != signal.ConversationDeletionSignalId)))
        {
            return new(ConversationAgentsOutcome.Conflict);
        }
        return result;
    }

    /// <summary>Checks exact authenticated evidence shape.</summary>
    /// <param name="result">Provider evidence.</param>
    /// <param name="principal">Authenticated identity.</param>
    /// <param name="tenant">Exact tenant.</param>
    /// <param name="requireOrganization">Whether this operation is an Agent service operation.</param>
    /// <returns>Available only for exact current evidence.</returns>
    public static ConversationAgentsOutcome CheckAuthority(ConversationAgentAuthorization? result,
        string principal, TenantId tenant, bool requireOrganization)
    {
        if (result is null)
        {
            return ConversationAgentsOutcome.Unavailable;
        }
        if (result.Outcome != ConversationAgentsOutcome.Available)
        {
            return result.Outcome == ConversationAgentsOutcome.Denied ? ConversationAgentsOutcome.Denied : ConversationAgentsOutcome.Unavailable;
        }
        return string.IsNullOrWhiteSpace(principal) || result.PrincipalId != principal || result.TenantId != tenant
            || string.IsNullOrWhiteSpace(result.AuthorityRevision)
            || (requireOrganization && (!result.OrganizationIdentityConfirmed || result.PartyId is null))
            ? ConversationAgentsOutcome.Denied : ConversationAgentsOutcome.Available;
    }

    private async Task<ConversationAgentAuthorization> AuthorizeSafelyAsync(string principal, TenantId tenant,
        ConversationId? conversation, string operation, CancellationToken cancellationToken)
    {
        try
        {
            return await authority.AuthorizeAsync(principal, tenant, conversation, operation, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new(ConversationAgentsOutcome.Unavailable);
        }
    }

    private async Task<ConversationTenantCatalogueResult> ReadCatalogueSafelyAsync(ConversationActiveCountQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await catalogue.ReadAsync(query, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new(ConversationAgentsOutcome.Unavailable);
        }
    }

    private static ConversationAgentReadResult Hidden(ConversationAgentReadQuery query, ConversationAgentsOutcome outcome)
        => new(outcome, query.TenantId, query.ConversationId);

    private static ConversationAgentMessage Visible(ConversationState state, ConversationMessage message)
    {
        bool redacted = state.Redactions.Any(r => r.Target.Kind == Contracts.Governance.GovernedTargetKind.Conversation
            || r.Target.MessageId == message.MessageId || r.Target.Kind == Contracts.Governance.GovernedTargetKind.ContentSegment);
        return new(message.MessageId, message.AuthorPartyId, message.Deleted || redacted ? null : message.Text,
            message.CreatedAt, message.EditedAt, message.Deleted, redacted, message.Provenance);
    }
}
