// <copyright file="ConversationBuyerAcceptanceDemoServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Testing.Fixtures;
using Hexalith.EventStore.Client.Queries;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Governance;

/// <summary>
/// Verifies the buyer acceptance demo runner stays read-oriented and content safe.
/// </summary>
public sealed class ConversationBuyerAcceptanceDemoServiceTest
{
    [Fact]
    public async Task DemoRunnerShouldProduceContentSafePassedEvidenceSummary()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        FakeProjectionReadStore store = new(seed);
        FakeTenantAccessService access = new(seed.AuthorizedTenantId);
        ConversationProjectionReadService projectionReadService = new(access, store);
        FakeTemporalEventSource temporalSource = new(seed);
        ConversationBuyerAcceptanceDemoService service = Service(access, store, projectionReadService, temporalSource);

        BuyerAcceptanceEvidenceSummaryV1 summary = await service.RunAsync(
            seed.Scenario,
            seed.AuthorizedTenantId,
            "runner-001",
            "runner-001",
            [seed.VerificationPass, seed.VerificationFailure],
            seed.PoisonTenantId,
            seed.PoisonProjection.Summary.ConversationId,
            TestContext.Current.CancellationToken);

        summary.StepResults
            .Where(step => step.Status != BuyerAcceptanceDemoExecutionStatus.Passed)
            .Select(step => $"{step.StepId}:{step.StepKind.Value}:{step.Status.Value}:{step.SafeSummary}")
            .ShouldBeEmpty();
        summary.Status.ShouldBe(BuyerAcceptanceDemoExecutionStatus.Passed);
        summary.StepResults.Count.ShouldBe(seed.Scenario.Steps.Count);
        summary.StepResults.ShouldAllBe(step => step.Status == BuyerAcceptanceDemoExecutionStatus.Passed);
        summary.StepResults.Select(step => step.StepId).ShouldBe(seed.Scenario.Steps.Select(step => step.StepId), ignoreOrder: false);
        summary.VerificationOutput.Select(output => output.Classification).ShouldContain(
            ConversationGovernanceVerificationFailureClassification.Passed);
        summary.VerificationOutput.Select(output => output.Classification).ShouldContain(
            ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        summary.EvidenceScope.ShouldBe(
            [BuyerAcceptanceEvidenceOwnership.Module, BuyerAcceptanceEvidenceOwnership.InheritedPlatformControl],
            ignoreOrder: false);
        summary.SafeSummary.ShouldBe("Buyer acceptance demo passed.");
        store.WriteAttempts.ShouldBe(0);
        temporalSource.Reads.ShouldBe(1);
        access.Calls.ShouldBeGreaterThan(0);

        string json = JsonSerializer.Serialize(summary);
        foreach (string sentinel in seed.PoisonSentinelValues)
        {
            json.ShouldNotContain(sentinel, Case.Insensitive);
        }

        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("provider payload", Case.Insensitive);
        json.ShouldNotContain("stack trace", Case.Insensitive);
    }

    [Fact]
    public async Task DemoRunnerShouldReportPartialWhenVerificationEvidenceIsMissing()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        FakeProjectionReadStore store = new(seed);
        FakeTenantAccessService access = new(seed.AuthorizedTenantId);
        ConversationProjectionReadService projectionReadService = new(access, store);
        ConversationBuyerAcceptanceDemoService service = Service(
            access,
            store,
            projectionReadService,
            new FakeTemporalEventSource(seed));

        BuyerAcceptanceEvidenceSummaryV1 summary = await service.RunAsync(
            seed.Scenario,
            seed.AuthorizedTenantId,
            "runner-001",
            "runner-001",
            [],
            seed.PoisonTenantId,
            seed.PoisonProjection.Summary.ConversationId,
            TestContext.Current.CancellationToken);

        summary.Status.ShouldBe(BuyerAcceptanceDemoExecutionStatus.Partial);
        summary.StepResults
            .Where(step => step.Status == BuyerAcceptanceDemoExecutionStatus.Failed)
            .Select(step => step.StepId)
            .ShouldBe(["step-verification-pass", "step-verification-failure"], ignoreOrder: false);
        summary.SafeSummary.ShouldBe("Buyer acceptance demo did not pass.");
        store.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task DemoRunnerShouldReportPartialWhenCrossTenantProbeIsMissing()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        FakeProjectionReadStore store = new(seed);
        FakeTenantAccessService access = new(seed.AuthorizedTenantId);
        ConversationProjectionReadService projectionReadService = new(access, store);
        ConversationBuyerAcceptanceDemoService service = Service(
            access,
            store,
            projectionReadService,
            new FakeTemporalEventSource(seed));

        BuyerAcceptanceEvidenceSummaryV1 summary = await service.RunAsync(
            seed.Scenario,
            seed.AuthorizedTenantId,
            "runner-001",
            "runner-001",
            [seed.VerificationPass, seed.VerificationFailure],
            crossTenantProbeTenantId: null,
            crossTenantProbeConversationId: null,
            TestContext.Current.CancellationToken);

        summary.Status.ShouldBe(BuyerAcceptanceDemoExecutionStatus.Partial);
        summary.StepResults.Single(step => step.StepId == "step-cross-scope-denial").Status
            .ShouldBe(BuyerAcceptanceDemoExecutionStatus.Failed);
        JsonSerializer.Serialize(summary).ShouldNotContain("POISON-SENTINEL", Case.Insensitive);
        store.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task DemoRunnerShouldFailClosedWhenCallerAuthorityIsMissing()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        FakeProjectionReadStore store = new(seed);
        FakeTenantAccessService access = new(seed.AuthorizedTenantId);
        ConversationProjectionReadService projectionReadService = new(access, store);
        ConversationBuyerAcceptanceDemoService service = Service(
            access,
            store,
            projectionReadService,
            new FakeTemporalEventSource(seed));

        BuyerAcceptanceEvidenceSummaryV1 summary = await service.RunAsync(
            seed.Scenario,
            seed.AuthorizedTenantId,
            callerPrincipalId: null,
            "runner-001",
            [seed.VerificationPass, seed.VerificationFailure],
            seed.PoisonTenantId,
            seed.PoisonProjection.Summary.ConversationId,
            TestContext.Current.CancellationToken);

        summary.Status.ShouldBe(BuyerAcceptanceDemoExecutionStatus.Failed);
        summary.StepResults.ShouldAllBe(step => step.Status == BuyerAcceptanceDemoExecutionStatus.Failed);
        summary.VerificationOutput.ShouldBeEmpty();
        access.Calls.ShouldBe(0);
        store.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task DemoRunnerShouldIgnoreVerificationEvidenceOutsideScenarioScope()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        FakeProjectionReadStore store = new(seed);
        FakeTenantAccessService access = new(seed.AuthorizedTenantId);
        ConversationProjectionReadService projectionReadService = new(access, store);
        ConversationBuyerAcceptanceDemoService service = Service(
            access,
            store,
            projectionReadService,
            new FakeTemporalEventSource(seed));

        BuyerAcceptanceEvidenceSummaryV1 summary = await service.RunAsync(
            seed.Scenario,
            seed.AuthorizedTenantId,
            "runner-001",
            "runner-001",
            [VerificationFor(new TenantId("foreign-tenant"), seed.VerificationPass)],
            seed.PoisonTenantId,
            seed.PoisonProjection.Summary.ConversationId,
            TestContext.Current.CancellationToken);

        summary.Status.ShouldBe(BuyerAcceptanceDemoExecutionStatus.Partial);
        summary.VerificationOutput.ShouldBeEmpty();
        summary.StepResults
            .Where(step => step.Status == BuyerAcceptanceDemoExecutionStatus.Failed)
            .Select(step => step.StepId)
            .ShouldBe(["step-verification-pass", "step-verification-failure"], ignoreOrder: false);
    }

    [Fact]
    public async Task DemoRunnerShouldNotTreatSameTenantHiddenReadAsCrossTenantDenial()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        FakeProjectionReadStore store = new(seed);
        FakeTenantAccessService access = new(seed.AuthorizedTenantId);
        ConversationProjectionReadService projectionReadService = new(access, store);
        ConversationBuyerAcceptanceDemoService service = Service(
            access,
            store,
            projectionReadService,
            new FakeTemporalEventSource(seed));

        BuyerAcceptanceEvidenceSummaryV1 summary = await service.RunAsync(
            seed.Scenario,
            seed.AuthorizedTenantId,
            "runner-001",
            "runner-001",
            [seed.VerificationPass, seed.VerificationFailure],
            seed.AuthorizedTenantId,
            new ConversationId("conversation-demo-missing"),
            TestContext.Current.CancellationToken);

        summary.Status.ShouldBe(BuyerAcceptanceDemoExecutionStatus.Partial);
        summary.StepResults.Single(step => step.StepId == "step-cross-scope-denial").Status
            .ShouldBe(BuyerAcceptanceDemoExecutionStatus.Failed);
        store.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    public void DemoRunnerShouldNotDependOnMutationExecutionBoundaries()
    {
        Type[] directDependencies =
        [
            .. typeof(ConversationBuyerAcceptanceDemoService).GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType),
            .. typeof(ConversationBuyerAcceptanceDemoService)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Select(field => field.FieldType),
        ];

        directDependencies.ShouldNotContain(typeof(SetConversationRetentionPolicyCommandHandler));
        directDependencies.ShouldNotContain(typeof(MarkConversationContentSensitiveCommandHandler));
        directDependencies.ShouldNotContain(typeof(RedactMessageContentCommandHandler));
        directDependencies.ShouldNotContain(typeof(IdempotentConversationCommandExecutor));
        directDependencies.ShouldNotContain(typeof(ConversationGovernanceAuditGate));
    }

    [Fact]
    public void AddConversationBuyerAcceptanceDemoShouldResolveService()
    {
        BuyerAcceptanceDemoSeedData seed = BuyerAcceptanceDemoFixtures.Create();
        ServiceCollection services = new();
        services.AddSingleton<IConversationTenantAccessService>(new FakeTenantAccessService(seed.AuthorizedTenantId));
        services.AddSingleton<IConversationProjectionReadStore>(new FakeProjectionReadStore(seed));
        services.AddDataProtection();
        services.AddConversationQueries(options => options.MaxOffset = 100_000);
        services.AddConversationGovernanceVerification();
        services.AddConversationBuyerAcceptanceDemo();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ConversationBuyerAcceptanceDemoService>().ShouldNotBeNull();
    }

    private static ConversationBuyerAcceptanceDemoService Service(
        IConversationTenantAccessService access,
        IConversationProjectionReadStore store,
        ConversationProjectionReadService projectionReadService,
        IConversationTemporalEventSource? temporalEventSource = null)
    {
        ConversationQueryHandler queryHandler = new(
            access,
            store,
            projectionReadService,
            new QueryCursorCodec(
                new EphemeralDataProtectionProvider(),
                ConversationQueryServiceCollectionExtensions.CursorCodecPurpose),
            temporalReconstructionService: new ConversationTemporalReconstructionService(
                access,
                projectionReadService,
                temporalEventSource ?? new UnavailableConversationTemporalEventSource()));

        return new ConversationBuyerAcceptanceDemoService(queryHandler, projectionReadService, new FakeTimeProvider());
    }

    private static ConversationGovernanceVerificationRunResultV1 VerificationFor(
        TenantId tenantId,
        ConversationGovernanceVerificationRunResultV1 source)
        => new(
            source.SchemaVersion,
            new ConversationGovernanceVerificationScopeV1(
                source.Scope.SchemaVersion,
                source.Scope.ScopeKind,
                tenantId,
                source.Scope.ConversationId),
            source.SelectedSuites,
            source.GeneratedAtUtc,
            source.CorrelationId,
            source.Status,
            source.Classification,
            source.SafeSummary,
            source.Checks,
            source.AuditEvidence,
            source.AuditNotRecordedReason);

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        private readonly Dictionary<ConversationId, ConversationProjectedReadModels> _authorized;
        private readonly BuyerAcceptanceDemoSeedData _seed;

        public FakeProjectionReadStore(BuyerAcceptanceDemoSeedData seed)
        {
            _seed = seed;
            _authorized = seed.AuthorizedProjections.ToDictionary(
                pair => pair.Summary.ConversationId,
                pair => new ConversationProjectedReadModels(pair.Summary, pair.Detail));
        }

        public int WriteAttempts { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            if (tenantId == _seed.PoisonTenantId && conversationId == _seed.PoisonProjection.Summary.ConversationId)
            {
                return ValueTask.FromResult<ConversationProjectedReadModels?>(
                    new ConversationProjectedReadModels(_seed.PoisonProjection.Summary, _seed.PoisonProjection.Detail));
            }

            return ValueTask.FromResult(_authorized.GetValueOrDefault(conversationId));
        }

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<ConversationSummaryProjectionV1>>(
                [_authorized.Values.Single(model => model.Summary.ConversationId.Value == "conversation-demo-full").Summary]);
    }

    private sealed class FakeTenantAccessService(TenantId authorizedTenantId) : IConversationTenantAccessService
    {
        public int Calls { get; private set; }

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
        {
            Calls++;
            bool allowed = trustedTenantId == authorizedTenantId
                && (routeTenantId is null || routeTenantId == authorizedTenantId)
                && (projectionTenantId is null || projectionTenantId == authorizedTenantId)
                && !string.IsNullOrWhiteSpace(callerPrincipalId);

            return ValueTask.FromResult(allowed
                ? ConversationTenantAccessDecision.Allowed(requirement, authorizedTenantId, callerPrincipalId!)
                : ConversationTenantAccessDecision.Denied(
                    requirement,
                    trustedTenantId,
                    callerPrincipalId,
                    ConversationTenantAccessDenialReason.MissingMember));
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 5, 22, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeTemporalEventSource(BuyerAcceptanceDemoSeedData seed) : IConversationTemporalEventSource
    {
        public int Reads { get; private set; }

        public ValueTask<ConversationTemporalEventSourceResult> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(ConversationTemporalEventSourceResult.Available(
                [
                    new ConversationReplayEventRecord(
                        1,
                        new ConversationCreated(
                            Metadata("event-create-001", ConversationEventType.ConversationCreated, 1, conversationId),
                            new BusinessReference("buyer-demo", "case-acceptance"),
                            new ProjectId("project-demo"),
                            new FolderId("folder-demo"),
                            "Demo case full")),
                    new ConversationReplayEventRecord(
                        2,
                        new ParticipantAdded(
                            Metadata("event-participant-001", ConversationEventType.ParticipantAdded, 2, conversationId),
                            new PartyId("party-demo-participant"),
                            ParticipantType.Human,
                            ParticipantRole.Member)),
                    new ConversationReplayEventRecord(
                        3,
                        new MessageAppended(
                            Metadata("event-message-001", ConversationEventType.MessageAppended, 3, conversationId),
                            new MessageId("message-001"),
                            new PartyId("party-demo-actor"),
                            "Synthetic governed message.")),
                ]));
        }

        private ConversationEventMetadata Metadata(
            string eventId,
            ConversationEventType eventType,
            long position,
            ConversationId conversationId)
            => new(
                SchemaVersion.Current,
                eventId,
                eventType,
                seed.AuthorizedTenantId,
                conversationId,
                "correlation-buyer-demo",
                new DateTimeOffset(2026, 5, 22, 9, 0, 0, TimeSpan.Zero).AddSeconds(position),
                new PartyId("party-demo-actor"),
                "causation-buyer-demo");
    }
}
