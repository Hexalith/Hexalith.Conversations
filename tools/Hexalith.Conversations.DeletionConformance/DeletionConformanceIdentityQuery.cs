using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Reports the installed service's fixed qualification scope under independent SDK workload authentication.</summary>
public sealed class DeletionConformanceIdentityQuery(IOptions<DeletionConformanceOptions> options,
    DeletionConformanceAuthorization authorization) : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => DeletionConformanceContract.Domain;
    /// <inheritdoc/>
    public string QueryType => "deletion-conformance-identity";
    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = options.Value;
        if (!current.IsConfigured() || query.Domain != Domain || query.QueryType != QueryType || query.TenantId != current.TenantId
            || query.AggregateId != "identity" || query.UserId != current.LookupPrincipalId || query.Payload.Length != 0
            || query.IsGlobalAdmin || query.IsDelegated
            || !authorization.Authorize(current.LookupPrincipalId, EventStoreWorkloadOperations.DomainServiceQuery, query.TenantId, true))
            return Task.FromResult(QueryResult.Failure("Denied"));
        return Task.FromResult(QueryResult.FromPayload(JsonSerializer.SerializeToElement(new
        {
            schemaVersion = 1, receiverKind = "ContractConformance", qualificationScope = DeletionConformanceContract.QualificationScope,
            evidenceLevel = 4, agentsRuntimeEnabled = false, tenantId = current.TenantId, targetVersion = current.TargetVersion,
            storageIdentity = current.StorageIdentity, receiverPrincipalId = current.ReceiverPrincipalId, lookupPrincipalId = current.LookupPrincipalId,
        })));
    }
}
