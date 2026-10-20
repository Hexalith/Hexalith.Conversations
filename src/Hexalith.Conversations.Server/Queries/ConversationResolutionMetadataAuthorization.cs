using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Http;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>Classifies the narrow scoring purpose and binds current Read decisions to the exact target.</summary>
/// <param name="access">The existing owner Read authorization service.</param>
/// <param name="accessor">The SDK-verified request context.</param>
/// <param name="signal">Native permission health; absence conservatively prevents disclosure.</param>
/// <param name="native">The production native Tenant Read evidence transport; local health is insufficient for the strict handler.</param>
/// <param name="timeProvider">The clock for actual owner-check instants.</param>
public sealed class ConversationResolutionMetadataAuthorization(
    IConversationTenantAccessService access,
    IHttpContextAccessor accessor,
    IConversationTenantProjectionSignal? signal = null,
    TimeProvider? timeProvider = null,
    IConversationResolutionNativeEvidence? native = null)
{
    /// <summary>The fixed owner-classified purpose; callers cannot supply an inspection purpose or grant.</summary>
    public const string ScoringPurpose = ConversationResolutionScoringPolicy.Purpose;

    /// <summary>Whether the production native Tenant evidence path owns this policy decision.</summary>
    internal bool UsesNativeEvidence => native is not null;

    /// <summary>Requires SDK verification of this exact query and its original authority.</summary>
    internal bool IsVerifiedScoringQuery(QueryEnvelope query)
    {
        VerifiedQueryCaller? verified = VerifiedQueryCaller.Get(accessor.HttpContext);
        OriginatingQueryContext? origin = verified?.OriginatingContext;
        if (verified is null || !ReferenceEquals(verified.Query, query)
            || query.CallerAuthority?.Origin != "delegated-owner-query" || origin is null
            || query.Domain != "conversations" || query.QueryType != GetConversationResolutionMetadataQuery.QueryType
            || query.AuthenticatedWorkloadId != "projects" || !query.IsDelegated
            || query.OriginalActorId != query.UserId || origin.OriginalActorId != query.UserId
            || !Canonical(query.UserId) || string.IsNullOrWhiteSpace(query.TenantId) || string.IsNullOrWhiteSpace(query.AggregateId)
            || query.Payload is not { Length: <= 8192 }
            || query.IsGlobalAdmin || query.Paging is not null || (query.EntityId is not null && query.EntityId != query.AggregateId)
            || (origin.IsDelegated && !Canonical(origin.DelegationId))
            || (!origin.IsDelegated && origin.DelegationId is not null)
            || !Compatible(origin.Scopes, "projects.read") || !Compatible(origin.Audience, "hexalith-eventstore"))
        {
            return false;
        }

        try
        {
            using JsonDocument payload = JsonDocument.Parse(query.Payload);
            return payload.RootElement.ValueKind == JsonValueKind.Object && !payload.RootElement.EnumerateObject().Any();
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Checks Read for this resource and scoring purpose with matching native evidence around the decision.</summary>
    internal async ValueTask<(bool Denied, string? PermissionVersion, DateTimeOffset AuthorizedAt)> CheckAsync(
        TenantId tenant, ConversationId resource, string actor, string purpose, CancellationToken cancellationToken)
    {
        // Resource identity is fixed by verified routing, never by payload or a caller-supplied grant.
        if (purpose != ScoringPurpose || string.IsNullOrWhiteSpace(resource.Value))
        {
            return (true, null, default);
        }

        if (native is not null)
        {
            VerifiedQueryCaller? verified = VerifiedQueryCaller.Get(accessor.HttpContext);
            if (verified?.Query is not { } query || query.TenantId != tenant.Value || query.AggregateId != resource.Value
                || query.UserId != actor) return (true, null, default);
            var evidence = await native.ReadTenantAsync(query, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = (timeProvider ?? TimeProvider.System).GetUtcNow();
            if (evidence is null || evidence.TenantId != tenant.Value || evidence.PrincipalId != actor
                || evidence.VerifiedAt > now) return (false, null, now);
            return (!evidence.Allowed, evidence.Allowed ? JsonSerializer.Serialize(evidence.NativeProof, JsonSerializerOptions.Web) : null,
                evidence.VerifiedAt);
        }
        ConversationTenantProjectionHealth before = await GetHealthAsync(tenant.Value, cancellationToken)
            .ConfigureAwait(false);
        ConversationTenantAccessDecision decision = await access.CheckAccessAsync(
            ConversationTenantAccessRequirement.Read, tenant, actor, routeTenantId: tenant,
            aggregateTenantId: tenant, projectionTenantId: tenant, cancellationToken: cancellationToken).ConfigureAwait(false);
        DateTimeOffset checkedAt = (timeProvider ?? TimeProvider.System).GetUtcNow();
        ConversationTenantProjectionHealth after = await GetHealthAsync(tenant.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!decision.IsAllowed)
        {
            bool unavailable = decision.DenialReason is ConversationTenantAccessDenialReason.TenantAccessUnavailable
                or ConversationTenantAccessDenialReason.TenantAccessStale or ConversationTenantAccessDenialReason.TenantAccessGapDetected
                or ConversationTenantAccessDenialReason.TenantAccessRolledBack or ConversationTenantAccessDenialReason.TenantProjectionPoisoned
                or ConversationTenantAccessDenialReason.MalformedProjection;
            return (!unavailable, null, checkedAt);
        }

        if (decision.Requirement != ConversationTenantAccessRequirement.Read || decision.TenantId != tenant
            || decision.CallerPrincipalId != actor || !Current(before) || !Current(after) || before != after
            || decision.ProjectionVersion != before.Version || decision.ProjectionWatermark != before.Watermark)
        {
            return (false, null, checkedAt);
        }

        // This is a bounded encoding of the native tuple, not a revision allocator or authorization lease.
        return (false, JsonSerializer.Serialize(new object[] { before.Version!.Value, before.Watermark! }), checkedAt);
    }

    /// <summary>Confirms native permission evidence did not change during the last resource lookup.</summary>
    internal async ValueTask<bool> ConfirmVersionAsync(TenantId tenant, string expected, CancellationToken cancellationToken)
    {
        if (native is not null)
        {
            VerifiedQueryCaller? verified = VerifiedQueryCaller.Get(accessor.HttpContext);
            if (verified?.Query is not { } query || query.TenantId != tenant.Value) return false;
            var evidence = await native.ReadTenantAsync(query, cancellationToken).ConfigureAwait(false);
            return evidence is { Allowed: true } && evidence.TenantId == tenant.Value && evidence.PrincipalId == query.UserId
                && JsonSerializer.Serialize(evidence.NativeProof, JsonSerializerOptions.Web) == expected;
        }
        ConversationTenantProjectionHealth health = await GetHealthAsync(tenant.Value, cancellationToken)
            .ConfigureAwait(false);
        return Current(health)
            && JsonSerializer.Serialize(new object[] { health.Version!.Value, health.Watermark! }) == expected;
    }

    private ValueTask<ConversationTenantProjectionHealth> GetHealthAsync(string tenant, CancellationToken cancellationToken)
        => signal?.GetProjectionHealthAsync(tenant, cancellationToken)
            ?? ValueTask.FromResult(ConversationTenantProjectionHealth.Healthy);

    private static bool Canonical(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim();
    private static bool Compatible(IReadOnlyList<string>? values, string expected)
        => values is null || values.Count == 0 || values.Contains(expected, StringComparer.Ordinal);
    private static bool Current(ConversationTenantProjectionHealth? health)
        => health is { IsStale: false, HasGap: false, HasRollback: false, IsPoisoned: false, Version: >= 0 }
            && !string.IsNullOrWhiteSpace(health.Watermark) && health.Watermark.Length <= 1024;
}
