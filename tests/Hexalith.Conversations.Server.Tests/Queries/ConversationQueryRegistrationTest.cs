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
using Hexalith.EventStore.DomainService;
using Hexalith.Conversations.Server.Tests.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Queries;

/// <summary>
/// Verifies query-service registration remains fail-closed when optional review sources are not configured.
/// </summary>
public sealed class ConversationQueryRegistrationTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NativeSdkDiscoveryEnrollsAndDispatchesAllHandlersInBothCompositionOrders(bool queriesFirst)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        fixture.Guard.ForbidLegacyReads = true;
        fixture.Guard.ForbidWrites = true;
        QueryEnvelope query = ResolutionMetadataFixture.Query();
        IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        List<Claim> claims = [
            new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
            .. QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)),
        ];
        accessor.HttpContext!.User = new(new ClaimsIdentity(claims, "Workload"));
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddSingleton(accessor);
        builder.Services.AddSingleton<IReadModelStore>(fixture.Guard);
        builder.Services.AddSingleton<IConversationProjectionReadStore>(fixture.Reader(fixture.Guard));
        builder.Services.AddSingleton<IConversationTenantAccessService>(fixture.Access);
        builder.Services.AddSingleton<IConversationTenantProjectionSignal>(fixture.Access);
        builder.Services.AddSingleton<TimeProvider>(fixture.Time);
        if (queriesFirst) builder.Services.AddConversationQueries(_ => { });
        builder.AddEventStoreDomainService(typeof(ConversationQueryHandler).Assembly);
        if (!queriesFirst) builder.Services.AddConversationQueries(_ => { });
        Type[] expected = [.. typeof(ConversationQueryHandler).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IDomainQueryHandler).IsAssignableFrom(type))];
        var enrolled = builder.Services.Where(descriptor => descriptor.ServiceType == typeof(IDomainQueryHandler)).ToArray();
        enrolled.Length.ShouldBe(expected.Length);
        enrolled.Select(descriptor => descriptor.ImplementationType).ShouldBe(expected, ignoreOrder: true);
        enrolled.ShouldContain(descriptor => descriptor.ImplementationType == typeof(GetConversationResolutionMetadataDomainQueryHandler));
        expected.Length.ShouldBeGreaterThan(1);
        await using ServiceProvider provider = builder.Services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<IDomainQueryHandler>().Count().ShouldBe(expected.Length);
        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider, query, TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("metadata").GetProperty("projectId").GetString().ShouldBe("project-original");
        fixture.Access.Checks.ShouldBe(2);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
        fixture.Guard.ReadKeys.ShouldAllBe(key => key.StartsWith("projection:conversations-resolution", StringComparison.Ordinal)
            || key.StartsWith("projection:conversations-dispatch:", StringComparison.Ordinal));
    }

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
