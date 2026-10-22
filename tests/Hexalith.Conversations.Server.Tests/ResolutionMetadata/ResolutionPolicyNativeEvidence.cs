using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionPolicyNativeEvidence(ResolutionMetadataFixture fixture,
    IConversationTenantAccessService access, IConversationTenantProjectionSignal signal) : IConversationResolutionNativeEvidence
{
    public async Task<TenantReadAuthorityResult?> ReadTenantAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        var before = await signal.GetProjectionHealthAsync(query.TenantId, cancellationToken);
        var decision = await access.CheckAccessAsync(ConversationTenantAccessRequirement.Read, ResolutionMetadataFixture.Tenant,
            query.UserId, ResolutionMetadataFixture.Tenant, aggregateTenantId: ResolutionMetadataFixture.Tenant,
            projectionTenantId: ResolutionMetadataFixture.Tenant, cancellationToken: cancellationToken);
        var after = await signal.GetProjectionHealthAsync(query.TenantId, cancellationToken);
        if (!decision.IsAllowed)
        {
            if (decision.IsRetryable) return null;
            var denied = await fixture.Native.ReadTenantAsync(query, cancellationToken);
            return denied is null ? null : denied with { Allowed = false };
        }
        if (!Current(before) || before != after || decision.ProjectionVersion != before.Version
            || decision.ProjectionWatermark != before.Watermark || decision.IsRetryable) return null;
        var evidence = await fixture.Native.ReadTenantAsync(query, cancellationToken);
        return evidence is null ? null : evidence with { Allowed = decision.IsAllowed };
    }

    public Task<ProjectionCurrentProof?> ReadProjectionAsync(ConversationResolutionMetadataReadModel metadata, CancellationToken cancellationToken)
        => fixture.Native.ReadProjectionAsync(metadata, cancellationToken);

    private static bool Current(ConversationTenantProjectionHealth value)
        => !value.IsStale && !value.HasGap && !value.HasRollback && !value.IsPoisoned && value.Version is >= 0
            && !string.IsNullOrWhiteSpace(value.Watermark);
}
