// <copyright file="ConversationQueryRegistrationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Conversations.Server.Tests.Projections;

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

        // Cursor integrity is now backed by ASP.NET Core Data Protection; no signing key / key id is bound.
        services.AddDataProtection();
        services.AddConversationQueries(options => options.MaxOffset = 100_000);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ConversationQueryHandler>().ShouldNotBeNull();
        provider.GetRequiredService<IQueryCursorCodec>().ShouldNotBeNull();
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

        public ValueTask<IReadOnlySet<string>> ValidatePageAsync(
            TenantId tenantId,
            ConversationProjectionIndexSnapshot snapshot,
            IReadOnlyList<ConversationSummaryProjectionV1> page,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ProjectionIndexSnapshotTestExtensions.NoInconsistentRows());

        public ValueTask<ConversationProjectionIndexSnapshot> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConversationProjectionIndexSnapshot.Empty);
    }
}
