using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Restricts command processing to the authenticated gateway, current source worker and exact installed target.</summary>
public sealed class DeletionConformanceAdmission(IOptions<DeletionConformanceOptions> options,
    DeletionConformanceAuthorization authorization, DeletionConformanceStreams streams) : IDomainServiceAdmissionStage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    /// <inheritdoc/>
    public string Name => "deletion-conformance-exact-source";

    /// <inheritdoc/>
    public async Task<DomainServiceAdmissionResult> EvaluateAsync(DomainServiceAdmissionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var current = options.Value;
        var envelope = context.Command;
        if (!current.IsConfigured() || envelope.Domain != DeletionConformanceContract.Domain
            || envelope.TenantId != current.TenantId || envelope.UserId != current.WorkerPrincipalId
            || envelope.CommandType != typeof(AcceptConformanceDeletion).FullName
            || !authorization.Authorize(current.GatewayWorkloadId, EventStoreWorkloadOperations.DomainServiceProcess, current.TenantId, false))
            return Reject(ConversationAgentsOutcome.Denied);
        try
        {
            var command = JsonSerializer.Deserialize<AcceptConformanceDeletion>(envelope.Payload, Json);
            if (command is null || !DeletionConformanceContract.ValidSignal(command.Signal)
                || command.Signal.TenantId.Value != current.TenantId || command.Signal.ConversationDeletionSignalId != envelope.AggregateId
                || command.TargetVersion != current.TargetVersion || string.IsNullOrWhiteSpace(command.DeliveryAttemptId)
                || command.DeliveryAttemptId.Length > 256) return Reject(ConversationAgentsOutcome.Denied);
            var outcome = await streams.VerifySourceAsync(command, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return outcome == ConversationAgentsOutcome.Available ? DomainServiceAdmissionResult.Accepted() : Reject(outcome);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return Reject(ConversationAgentsOutcome.Unavailable); }
    }

    private static DomainServiceAdmissionResult Reject(ConversationAgentsOutcome outcome)
        => DomainServiceAdmissionResult.Rejected([new ConformanceDeletionRejected(outcome)]);
}
