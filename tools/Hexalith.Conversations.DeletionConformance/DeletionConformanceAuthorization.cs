using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.Http;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Narrows SDK-authenticated workload principals to this isolated domain and installed identity.</summary>
public sealed class DeletionConformanceAuthorization(IHttpContextAccessor http)
{
    /// <summary>Requires the SDK workload identity and its operation; wire fields never create identity.</summary>
    public bool Authorize(string principalId, string operation, string tenant, bool requireBindings)
    {
        var principal = http.HttpContext?.User;
        return principal?.Identities.Any(static identity => identity.IsAuthenticated
                && identity.AuthenticationType == EventStoreWorkloadAuthenticationDefaults.WorkloadScheme) == true
            && principal.FindAll(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType).Select(static claim => claim.Value).SequenceEqual([principalId])
            && principal.HasClaim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, operation)
            && (!requireBindings || principal.HasClaim(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType, tenant)
                && principal.HasClaim(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType, DeletionConformanceContract.Domain));
    }
}
