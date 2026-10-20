using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionTenantStore : ITenantProjectionStore
{
    public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult<TenantLocalState?>(new() { TenantId = tenantId, Status = TenantStatus.Active,
            Members = new() { ["actor"] = TenantRole.TenantReader } });
    public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
        => throw new Xunit.Sdk.XunitException("Query attempted a tenant mutation.");
}
