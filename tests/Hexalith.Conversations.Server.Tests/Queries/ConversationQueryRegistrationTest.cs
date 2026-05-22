// <copyright file="ConversationQueryRegistrationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Verifies query-service registration remains fail-closed when optional review sources are not configured.
/// </summary>
public sealed class ConversationQueryRegistrationTest
{
    [Fact]
    public void AddConversationQueriesShouldResolveHandlerWithoutConfiguredPrivilegedReviewSource()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConversationTenantAccessService>(new FakeTenantAccessService());
        services.AddSingleton<IConversationProjectionReadStore>(new FakeProjectionReadStore());
        services.AddConversationQueries(options =>
        {
            options.SigningKey = Enumerable.Repeat((byte)7, 32).ToArray();
            options.KeyId = "test-key";
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ConversationQueryHandler>().ShouldNotBeNull();
        provider.GetRequiredService<IPrivilegedOperationalJustificationReviewSource>()
            .ShouldNotBeNull();
    }

    private sealed class FakeTenantAccessService : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationTenantAccessDecision.Denied(
                requirement,
                trustedTenantId,
                callerPrincipalId,
                ConversationTenantAccessDenialReason.MissingMember));
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<ConversationProjectedReadModels?>(null);

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult((IReadOnlyList<ConversationSummaryProjectionV1>)[]);
    }
}
