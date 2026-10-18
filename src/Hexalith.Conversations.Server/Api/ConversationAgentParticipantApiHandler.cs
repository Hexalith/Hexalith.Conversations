using System.Security.Claims;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Server.Agents;

namespace Hexalith.Conversations.Server.Api;

/// <summary>Forwards the restricted participant route through the admitted EventStore gateway.</summary>
/// <remarks>The gateway admission result is pending until an independent source read confirms persistence.</remarks>
public sealed class ConversationAgentParticipantApiHandler(
    IConversationAgentAuthority authority,
    IConversationClient client,
    IHttpContextAccessor contextAccessor) : IConversationCommandApiHandler
{
    /// <inheritdoc />
    public async ValueTask<ConversationCommandApiOutcome<ConversationAgentCommandResult>> AddParticipantAsync(
        AddParticipantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        string? principal = contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? tenant = contextAccessor.HttpContext?.User.FindFirstValue(ConversationReadApi.TenantIdClaimType);
        if (string.IsNullOrWhiteSpace(principal) || tenant != command.Metadata.TenantId.Value)
        {
            return Outcome(ConversationAgentsOutcome.Denied);
        }
        if (command.OperationTimestamp is not { } occurred || occurred.Offset != TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(command.Metadata.IdempotencyKey) || command.Metadata.SchemaVersion.Value != 1)
        {
            return Outcome(ConversationAgentsOutcome.Invalid);
        }

        ConversationAgentAuthorization admitted;
        try
        {
            admitted = await authority.AuthorizeAsync(principal, command.Metadata.TenantId,
                command.ConversationId, "AddAgentParticipant", cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Outcome(ConversationAgentsOutcome.Unavailable);
        }

        ConversationAgentsOutcome access = ConversationAgentQueryService.CheckAuthority(
            admitted, principal, command.Metadata.TenantId, requireOrganization: true);
        if (access != ConversationAgentsOutcome.Available)
        {
            return Outcome(access);
        }
        if (admitted.PartyId != command.Metadata.ActorPartyId || admitted.PartyId != command.ParticipantPartyId)
        {
            return Outcome(ConversationAgentsOutcome.Denied);
        }
        if (command.ParticipantType != ParticipantType.AiAgent || command.ParticipantRole != ParticipantRole.Member)
        {
            return Outcome(ConversationAgentsOutcome.Conflict);
        }

        ConversationAgentCommandResult accepted;
        try
        {
            accepted = await client.AddParticipantAsync(command, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!Enum.IsDefined(accepted.Outcome))
            {
                return Outcome(ConversationAgentsOutcome.Unavailable);
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Outcome(ConversationAgentsOutcome.Unavailable);
        }

        try
        {
            ConversationAgentAuthorization current = await authority.AuthorizeAsync(principal, command.Metadata.TenantId,
                command.ConversationId, "AddAgentParticipant", cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (current != admitted)
            {
                return Outcome(ConversationAgentsOutcome.Unavailable);
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Outcome(ConversationAgentsOutcome.Unavailable);
        }

        return ConversationCommandApiOutcome<ConversationAgentCommandResult>.Success(
            accepted with { Persisted = false }, Status(accepted.Outcome));
    }

    /// <inheritdoc />
    public ValueTask<ConversationCommandApiOutcome<ConversationCreatedResult>> CreateConversationAsync(
        CreateConversationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ValueTask.FromResult(Unavailable<ConversationCreatedResult>(command.Metadata.CorrelationId));
    }

    /// <inheritdoc />
    public ValueTask<ConversationCommandApiOutcome<ConversationCommandAcceptedResult>> AppendMessageAsync(
        AppendMessageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ValueTask.FromResult(Unavailable<ConversationCommandAcceptedResult>(command.Metadata.CorrelationId));
    }

    /// <inheritdoc />
    public ValueTask<ConversationCommandApiOutcome<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(
        ReassignConversationProjectCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ValueTask.FromResult(Unavailable<ConversationCommandAcceptedResult>(command.Metadata.CorrelationId));
    }

    private static ConversationCommandApiOutcome<ConversationAgentCommandResult> Outcome(ConversationAgentsOutcome outcome)
        => ConversationCommandApiOutcome<ConversationAgentCommandResult>.Success(new(outcome), Status(outcome));

    private static int Status(ConversationAgentsOutcome outcome) => outcome switch
    {
        ConversationAgentsOutcome.Available => StatusCodes.Status202Accepted,
        ConversationAgentsOutcome.Denied => StatusCodes.Status403Forbidden,
        ConversationAgentsOutcome.Invalid => StatusCodes.Status400BadRequest,
        ConversationAgentsOutcome.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status503ServiceUnavailable,
    };

    private static ConversationCommandApiOutcome<T> Unavailable<T>(string correlationId) where T : class
        => ConversationCommandApiOutcome<T>.Failure(new([
            ConversationErrorCatalog.CreateError(ConversationErrorCode.ParticipantValidationUnavailable, correlationId),
        ]), StatusCodes.Status503ServiceUnavailable);
}
