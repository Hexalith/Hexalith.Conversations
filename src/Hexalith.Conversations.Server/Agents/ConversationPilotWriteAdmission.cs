using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Establishes the human Party and checks the current tenant projection for ordinary pilot writes.</summary>
/// <param name="configuration">Operator-provisioned bindings keyed by exact tenant and authenticated subject.</param>
/// <param name="access">The domain owner's current tenant write gate.</param>
public sealed class ConversationPilotWriteAdmission(IConfiguration configuration, IConversationTenantAccessService access)
{
    /// <summary>Rejects missing, contradictory or unavailable authority before processing.</summary>
    /// <param name="principal">The principal authenticated by the gateway.</param>
    /// <param name="metadata">The requested tenant and Party.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The command owner's admission result.</returns>
    public async Task<DomainServiceAdmissionResult> AuthorizeAsync(string principal, ConversationCommandMetadata metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var matches = configuration.GetSection("Conversations:Pilot:HumanPartyBindings").GetChildren()
                .Where(entry => entry["TenantId"] == metadata.TenantId.Value && entry["PrincipalId"] == principal)
                .Take(2).ToArray();
            if (matches.Length != 1 || matches[0]["PartyId"] != metadata.ActorPartyId.Value)
                return DomainServiceAdmissionResult.Rejected([new ConversationWriteDenied()]);

            var decision = await access.CheckAccessAsync(ConversationTenantAccessRequirement.Write, metadata.TenantId,
                principal, commandTenantId: metadata.TenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (decision.IsAllowed && decision.Requirement == ConversationTenantAccessRequirement.Write
                && decision.TenantId == metadata.TenantId && decision.CallerPrincipalId == principal)
                return DomainServiceAdmissionResult.Accepted();
            if (decision.IsRetryable) throw new ConversationWriteAuthorityUnavailableException();
            return DomainServiceAdmissionResult.Rejected([new ConversationWriteDenied()]);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not ConversationWriteAuthorityUnavailableException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new ConversationWriteAuthorityUnavailableException();
        }
    }
}
