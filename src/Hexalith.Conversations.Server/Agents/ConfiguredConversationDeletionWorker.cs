using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Client.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Checks an explicitly configured dedicated worker against independent current operation/Party authority.</summary>
/// <remarks>The authority must independently resolve current active Organization evidence from Parties. Configuration is not authority or enrollment.</remarks>
public sealed class ConfiguredConversationDeletionWorker
{
    private readonly ConversationDeletionWorkerRegistration? _registration;
    private readonly IConversationAgentAuthority _authority;
    private readonly TimeProvider _clock;

    /// <summary>Creates the disabled-by-default exact binding. A missing registration cannot authorize work.</summary>
    public ConfiguredConversationDeletionWorker(ConversationDeletionWorkerRegistration? registration, IConversationAgentAuthority authority, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (registration is not null && (registration.TenantId is null || registration.ServicePartyId is null
            || string.IsNullOrWhiteSpace(registration.AuthenticatedPrincipalId)))
        { throw new ArgumentException("Malformed worker registration.", nameof(registration)); }
        _registration = registration; _authority = authority; _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Resolves current exact Conversation authority for the dedicated service Party, without any human-role fallback.</summary>
    public async Task<ConversationAgentAuthorization?> AuthorizeAsync(TenantId tenant, ConversationId conversation,
        CancellationToken cancellationToken = default)
        => await ResolveAsync(tenant, conversation, "DeletionSource", cancellationToken).ConfigureAwait(false);

    /// <summary>Resolves current exact delivery-command authority for the enrolled worker; source-read permission alone is insufficient.</summary>
    public Task<ConversationAgentAuthorization?> AuthorizeDeliveryAsync(TenantId tenant, ConversationId conversation,
        CancellationToken cancellationToken = default) => ResolveAsync(tenant, conversation, "RecordConversationDeletionDelivery", cancellationToken);

    private async Task<ConversationAgentAuthorization?> ResolveAsync(TenantId tenant, ConversationId conversation, string operation,
        CancellationToken cancellationToken)
    {
        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), _clock, cancellationToken, _clock.GetTimestamp());
        try
        {
            deadline.ThrowIfCancellationRequested();
            if (_registration is null || _registration.TenantId != tenant) { return null; }
            ConversationAgentAuthorization result = await deadline.ReadAsync(token => _authority.AuthorizeAsync(_registration.AuthenticatedPrincipalId,
                tenant, conversation, operation, token)).ConfigureAwait(false);
            deadline.ThrowIfCancellationRequested();
            var admitted = ConversationAgentQueryService.CheckAuthority(result, _registration.AuthenticatedPrincipalId, tenant, true)
                == ConversationAgentsOutcome.Available && result.PartyId == _registration.ServicePartyId ? result : null;
            deadline.ThrowIfCancellationRequested(); return admitted;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsExpired) { return null; }
        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }
    }
}
