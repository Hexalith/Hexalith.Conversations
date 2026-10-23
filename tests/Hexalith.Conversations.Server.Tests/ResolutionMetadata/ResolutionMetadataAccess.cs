using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionMetadataAccess : IConversationTenantAccessService, IConversationTenantProjectionSignal
{
    public ConversationTenantProjectionHealth Health { get; set; } = new(Version: 7, Watermark: "native-watermark-7");
    public List<(ConversationTenantAccessRequirement Requirement, TenantId? Tenant, string? Actor, TenantId? Route, TenantId? Aggregate, TenantId? Projection)> Arguments { get; } = [];
    public Func<ConversationTenantAccessDecision, ConversationTenantAccessDecision>? MutateDecision { get; set; }
    public string? AllowedTenant { get; set; }
    public int Checks { get; private set; }
    public Action<int>? OnCheck { get; set; }
    public Func<int, Task>? BeforeDecision { get; set; }
    public bool Denied { get; set; }
    public bool RetryableDenial { get; set; }
    public bool MissingDecisionEvidence { get; set; }
    public long? DecisionVersionOverride { get; set; }
    public string? DecisionWatermarkOverride { get; set; }
    public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(string tenantId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Health);

    public async ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId, string? callerPrincipalId, TenantId? routeTenantId = null, TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null, TenantId? projectionTenantId = null, TenantId? idempotencyTenantId = null,
        CancellationToken cancellationToken = default)
    {
        Arguments.Add((requirement, trustedTenantId, callerPrincipalId, routeTenantId, aggregateTenantId, projectionTenantId));
        Checks++;
        OnCheck?.Invoke(Checks);
        if (BeforeDecision is not null) await BeforeDecision(Checks);
        if (RetryableDenial) return ConversationTenantAccessDecision.Denied(requirement, trustedTenantId, callerPrincipalId,
            ConversationTenantAccessDenialReason.TenantAccessUnavailable, isRetryable: true);
        var decision = Denied || AllowedTenant is not null && trustedTenantId?.Value != AllowedTenant
            ? ConversationTenantAccessDecision.Denied(requirement, trustedTenantId, callerPrincipalId, ConversationTenantAccessDenialReason.MissingMember)
            : ConversationTenantAccessDecision.Allowed(requirement, trustedTenantId!, callerPrincipalId!,
                MissingDecisionEvidence ? null : DecisionVersionOverride ?? Health.Version,
                MissingDecisionEvidence ? null : DecisionWatermarkOverride ?? Health.Watermark);
        return MutateDecision?.Invoke(decision) ?? decision;
    }
}
