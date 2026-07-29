// <copyright file="ConversationGovernanceVerificationServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Governance;

/// <summary>
/// Verifies governance verification orchestration without introducing a write authority.
/// </summary>
public sealed class ConversationGovernanceVerificationServiceTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly TenantId OtherTenant = new("tenant-other");
    private static readonly ConversationId Conversation = new("conversation-alpha");
    private static readonly PartyId Actor = new("party-creator");
    private static readonly PartyId Participant = new("party-human");
    private static readonly MessageId Message = new("message-alpha");
    private static readonly DateTimeOffset Started = new(2026, 5, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 5, 22, 8, 0, 10, TimeSpan.Zero);
    private static readonly GovernanceAuditEvidenceReference AuditEvidence = new(
        new AuditEvidenceHandle("audit-evidence-verify"),
        "verification-policy-standard",
        Generated);
    private static readonly PrivilegedOperationalJustificationDetailsV1 VerifyJustification = new(
        SchemaVersion.Current,
        Tenant,
        Conversation,
        new GovernanceTarget(GovernedTargetKind.Conversation),
        Actor,
        PrivilegedOperationalActionClass.Verify,
        PrivilegedActionClass.OperationalOverride,
        "verification-policy-standard",
        "customer-request",
        Generated,
        GovernanceOutcome.Succeeded,
        AuditEvidence,
        ProjectionTrustState.Current,
        Freshness(ProjectionTrustState.Current),
        "Verification evidence is available.",
        "correlation-001",
        "causation-001");

    [Fact]
    public async Task PassingVerificationShouldReturnStructuredPassedResult()
    {
        ConversationProjectedReadModels models = Materialize(OrderedEvents());
        ConversationGovernanceVerificationService service = Service(models, ConversationTemporalEventSourceResult.Available(ReplayEvents()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request(ConversationGovernanceVerificationSuite.All),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.Passed);
        result.Status.ShouldBe(ConversationGovernanceVerificationExecutionStatus.Completed);
        result.AuditEvidence.ShouldBe(AuditEvidence);
        result.Checks.Select(check => check.Suite).ShouldBe(ConversationGovernanceVerificationSuite.All, ignoreOrder: false);
        result.Checks.ShouldAllBe(check => check.Classification == ConversationGovernanceVerificationFailureClassification.Passed);
        result.ToString().ShouldNotContain("tenant-other", Case.Insensitive);
        result.ToString().ShouldNotContain("stream-", Case.Insensitive);
        result.ToString().ShouldNotContain("Exception", Case.Insensitive);
    }

    [Fact]
    public async Task MissingAuditPairShouldBeGovernanceFailureNotInfrastructureFailure()
    {
        ConversationProjectedReadModels models = WithRedactions(
            Materialize(OrderedEvents()),
            new ConversationRedactionProjectionV1(
                new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
                RedactionCategory.ContentSuppression,
                "redaction-policy-standard",
                "customer-request",
                Actor,
                Generated,
                AuditEvidence: null,
                ProjectionTrustState.Current));
        ConversationGovernanceVerificationService service = Service(models, ConversationTemporalEventSourceResult.Available(ReplayEvents()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.AuditPairing]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        result.Checks.Single().Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        result.Checks.Single().SafeDetail.ShouldBe("Governed state is missing paired audit references.");
    }

    [Fact]
    public async Task RedactionReplayFailureShouldBeGovernanceFailureWithSafeDetail()
    {
        ConversationProjectedReadModels models = WithRedactions(
            Materialize(OrderedEvents()),
            new ConversationRedactionProjectionV1(
                new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
                RedactionCategory.ContentSuppression,
                "redaction-policy-standard",
                "customer-request",
                Actor,
                Generated,
                AuditEvidence,
                ProjectionTrustState.Current));
        ConversationGovernanceVerificationService service = Service(models, ConversationTemporalEventSourceResult.Available(ReplayEvents()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.RedactionReplay]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        result.Checks.Single().SafeDetail.ShouldBe("Redacted timeline entry was not placeholder safe.");
        result.Checks.Single().SafeDetail.ShouldNotContain("Hello", Case.Insensitive);
    }

    [Fact]
    public async Task ProjectionRebuildDisagreementShouldBeGovernanceFailure()
    {
        ConversationProjectedReadModels wrong = Materialize([Event(1, Created("event-create-001", 1, label: "Wrong"))]);
        ConversationGovernanceVerificationService service = Service(wrong, ConversationTemporalEventSourceResult.Available(ReplayEvents()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.ProjectionRebuild]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        result.Checks.Single().SafeDetail.ShouldBe("Rebuilt derived state disagrees with current read evidence.");
    }

    [Fact]
    public async Task UnsupportedSchemaShouldBeClassifiedSeparately()
    {
        ConversationGovernanceVerificationService service = Service(
            Materialize(OrderedEvents()),
            ConversationTemporalEventSourceResult.Available(
                [new ConversationReplayEventRecord(1, Created("event-create-001", 1, schemaVersion: new SchemaVersion(2)))]));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.SchemaCompatibility]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.UnsupportedVersion);
        result.Checks.Single().Remediation.ShouldBe(ConversationGovernanceVerificationRemediation.MigrateSchema);
    }

    [Fact]
    public async Task StaleProjectionShouldFailClosedBeforeTrustBearingChecks()
    {
        ConversationProjectedReadModels stale = Materialize(
            OrderedEvents(),
            freshnessState: ProjectionTrustState.Stale,
            reasonCode: ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        ConversationGovernanceVerificationService service = Service(stale, ConversationTemporalEventSourceResult.Available(ReplayEvents()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.AuditPairing]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.StaleProjection);
        result.Checks.Single().CheckName.ShouldBe("freshness-gate");
    }

    [Fact]
    public async Task MissingVerifyJustificationShouldBlockBeforeTenantEvidenceRead()
    {
        FakeProjectionReadStore store = new(Materialize(OrderedEvents()));
        ConversationGovernanceVerificationService service = Service(
            store,
            new StaticTemporalEventSource(ConversationTemporalEventSourceResult.Available(ReplayEvents())));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.AuditPairing]),
            Tenant,
            "caller-001",
            privilegedJustification: null,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden);
        result.Status.ShouldBe(ConversationGovernanceVerificationExecutionStatus.Blocked);
        result.Checks.Single().CheckName.ShouldBe("verify-justification");
        result.Checks.Single().Remediation.ShouldBe(ConversationGovernanceVerificationRemediation.ProvideVerifyJustification);
        result.AuditEvidence.ShouldBeNull();
        store.ReadCount.ShouldBe(0);
    }

    [Fact]
    public async Task NonVerifyPrivilegedJustificationShouldBlockBeforeTenantEvidenceRead()
    {
        FakeProjectionReadStore store = new(Materialize(OrderedEvents()));
        ConversationGovernanceVerificationService service = Service(
            store,
            new StaticTemporalEventSource(ConversationTemporalEventSourceResult.Available(ReplayEvents())));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.AuditPairing]),
            Tenant,
            "caller-001",
            Justification(PrivilegedOperationalActionClass.Read),
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden);
        result.Checks.Single().CheckName.ShouldBe("verify-justification");
        result.AuditEvidence.ShouldBe(AuditEvidence);
        store.ReadCount.ShouldBe(0);
    }

    [Fact]
    public async Task TenantWideScopeShouldReturnExplicitDeferredResultWithoutConversationRead()
    {
        FakeProjectionReadStore store = new(Materialize(OrderedEvents()));
        ConversationGovernanceVerificationService service = Service(
            store,
            new StaticTemporalEventSource(ConversationTemporalEventSourceResult.Available(ReplayEvents())));

        ConversationGovernanceVerificationRequestV1 request = new(
            SchemaVersion.Current,
            new ConversationGovernanceVerificationScopeV1(
                SchemaVersion.Current,
                ConversationGovernanceVerificationScopeKind.Tenant,
                Tenant),
            [ConversationGovernanceVerificationSuite.AuditPairing, ConversationGovernanceVerificationSuite.SchemaCompatibility],
            "correlation-001");

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            request,
            Tenant,
            "caller-001",
            privilegedJustification: null,
            Generated,
            localReadOnlyEvidenceOnly: true,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.NotApplicable);
        result.Status.ShouldBe(ConversationGovernanceVerificationExecutionStatus.Completed);
        result.AuditNotRecordedReason.ShouldBe("Local read only proof did not touch tenant data.");
        result.Checks.Select(check => check.CheckName).ShouldAllBe(checkName => checkName == "v1-scope-coverage");
        store.ReadCount.ShouldBe(0);
    }

    [Fact]
    public async Task LocalReadOnlyEvidenceOnlyShouldRecordExplicitAuditNotRecordedReason()
    {
        ConversationGovernanceVerificationService service = Service(
            Materialize(OrderedEvents()),
            ConversationTemporalEventSourceResult.Available(ReplayEvents()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.SchemaCompatibility]),
            Tenant,
            "caller-001",
            privilegedJustification: null,
            Generated,
            localReadOnlyEvidenceOnly: true,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.Passed);
        result.AuditEvidence.ShouldBeNull();
        result.AuditNotRecordedReason.ShouldBe("Local read only proof did not touch tenant data.");
    }

    [Fact]
    public async Task DependencyUnavailableAndThrownSourceShouldStayContentSafe()
    {
        ConversationGovernanceVerificationService unavailable = Service(
            Materialize(OrderedEvents()),
            ConversationTemporalEventSourceResult.Unavailable());
        ConversationGovernanceVerificationService throwing = Service(
            Materialize(OrderedEvents()),
            new ThrowingTemporalEventSource());

        ConversationGovernanceVerificationRunResultV1 unavailableResult = await unavailable.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.SchemaCompatibility]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);
        ConversationGovernanceVerificationRunResultV1 thrownResult = await throwing.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.SchemaCompatibility]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        unavailableResult.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.DependencyUnavailable);
        thrownResult.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.DependencyUnavailable);
        thrownResult.ToString().ShouldNotContain("boom", Case.Insensitive);
        thrownResult.ToString().ShouldNotContain("Exception", Case.Insensitive);
    }

    [Theory]
    [InlineData("rebuilding", "stale-projection")]
    [InlineData("outside-coverage", "data-unavailable")]
    public async Task TemporalSourceStatesShouldKeepStructuredFailureClassification(string sourceState, string classificationValue)
    {
        ConversationTemporalEventSourceResult source = sourceState == "rebuilding"
            ? ConversationTemporalEventSourceResult.Rebuilding()
            : ConversationTemporalEventSourceResult.OutsideCoverage();
        ConversationGovernanceVerificationService service = Service(Materialize(OrderedEvents()), source);

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.RedactionReplay]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.Value.ShouldBe(classificationValue);
        result.Checks.Single().SafeDetail.ShouldBe("Replay proof is unavailable.");
    }

    [Fact]
    public async Task UnauthorizedScopeShouldReturnHiddenResultWithoutInspectingTarget()
    {
        FakeProjectionReadStore store = new(Materialize(OrderedEvents()));
        ConversationGovernanceVerificationService service = Service(
            store,
            new StaticTemporalEventSource(ConversationTemporalEventSourceResult.Available(ReplayEvents())),
            allowAccess: false);

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.TenantIsolation]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden);
        result.SafeSummary.ShouldBe("Requested scope is hidden or unavailable.");
        store.ReadCount.ShouldBe(0);
    }

    [Fact]
    public async Task CrossTenantPoisonShouldNotEchoForeignTenant()
    {
        ConversationGovernanceVerificationService service = Service(
            Materialize(OrderedEvents()),
            ConversationTemporalEventSourceResult.Available(
                [new ConversationReplayEventRecord(1, Created("event-create-foreign", 1, tenantId: OtherTenant))]));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.TenantIsolation]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        result.Checks.Single().SafeDetail.ShouldBe("Cross scope evidence was hidden.");
        result.ToString().ShouldNotContain(OtherTenant.Value, Case.Insensitive);
    }

    [Fact]
    public async Task ProviderPortabilityShouldFailWhenProviderCorrelationMatchesConversationAuthority()
    {
        ConversationProjectionEventRecord[] poisonedEvents =
        [
            Event(1, Created("event-create-001", 1, providerSessionReference: Conversation.Value)),
            Event(2, ParticipantAdded("event-participant-001", 2)),
            Event(3, MessageAppended("event-message-001", 3)),
        ];
        ConversationGovernanceVerificationService service = Service(
            Materialize(poisonedEvents),
            ConversationTemporalEventSourceResult.Available(
                poisonedEvents.Select(e => new ConversationReplayEventRecord(e.Position, e.Event)).ToArray()));

        ConversationGovernanceVerificationRunResultV1 result = await service.VerifyAsync(
            Request([ConversationGovernanceVerificationSuite.ProviderPortability]),
            Tenant,
            "caller-001",
            VerifyJustification,
            Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Classification.ShouldBe(ConversationGovernanceVerificationFailureClassification.GovernanceFailed);
        result.Checks.Single().SafeDetail.ShouldBe("Provider correlation was treated as authority.");
        result.ToString().ShouldNotContain("provider-session", Case.Insensitive);
    }

    [Fact]
    public void VerificationServiceShouldNotDependOnMutationExecutionBoundaries()
    {
        Type[] directDependencies =
        [
            .. typeof(ConversationGovernanceVerificationService).GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType),
            .. typeof(ConversationGovernanceVerificationService)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Select(field => field.FieldType),
        ];

        directDependencies.ShouldNotContain(typeof(SetConversationRetentionPolicyCommandHandler));
        directDependencies.ShouldNotContain(typeof(MarkConversationContentSensitiveCommandHandler));
        directDependencies.ShouldNotContain(typeof(RedactMessageContentCommandHandler));
        directDependencies.ShouldNotContain(typeof(IdempotentConversationCommandExecutor));
        directDependencies.ShouldNotContain(typeof(ConversationGovernanceAuditGate));
        directDependencies.ShouldNotContain(typeof(SetConversationRetentionPolicy));
        directDependencies.ShouldNotContain(typeof(MarkConversationContentSensitive));
        directDependencies.ShouldNotContain(typeof(RedactMessageContent));
    }

    [Fact]
    public void AddConversationGovernanceVerificationShouldResolveServiceWithExistingBoundaries()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConversationTenantAccessService>(new FakeTenantAccessService(allowAccess: true));
        services.AddSingleton<IConversationProjectionReadStore>(new FakeProjectionReadStore(Materialize(OrderedEvents())));
        services.AddConversationGovernanceVerification();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ConversationGovernanceVerificationService>().ShouldNotBeNull();
    }

    private static ConversationGovernanceVerificationRequestV1 Request(
        IReadOnlyList<ConversationGovernanceVerificationSuite> suites)
        => new(
            SchemaVersion.Current,
            new ConversationGovernanceVerificationScopeV1(
                SchemaVersion.Current,
                ConversationGovernanceVerificationScopeKind.Conversation,
                Tenant,
                Conversation),
            suites,
            "correlation-001");

    private static PrivilegedOperationalJustificationDetailsV1 Justification(PrivilegedOperationalActionClass actionClass)
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            Actor,
            actionClass,
            PrivilegedActionClass.OperationalOverride,
            "verification-policy-standard",
            "customer-request",
            Generated,
            GovernanceOutcome.Succeeded,
            AuditEvidence,
            ProjectionTrustState.Current,
            Freshness(ProjectionTrustState.Current),
            "Verification evidence is available.",
            "correlation-001",
            "causation-001");

    private static ProjectionFreshnessV1 Freshness(ProjectionTrustState state)
        => new(
            SchemaVersion.Current,
            "pos:0000000003",
            3,
            Started.AddSeconds(3),
            Generated,
            TimeSpan.FromSeconds(7),
            IsStale: state == ProjectionTrustState.Stale,
            state,
            state == ProjectionTrustState.Current
                ? ProjectionFreshnessReasonCode.Current
                : ProjectionFreshnessReasonCode.StaleThresholdExceeded);

    private static ConversationGovernanceVerificationService Service(
        ConversationProjectedReadModels models,
        ConversationTemporalEventSourceResult temporalResult)
        => Service(new FakeProjectionReadStore(models), new StaticTemporalEventSource(temporalResult));

    private static ConversationGovernanceVerificationService Service(
        ConversationProjectedReadModels models,
        IConversationTemporalEventSource temporalSource)
        => Service(new FakeProjectionReadStore(models), temporalSource);

    private static ConversationGovernanceVerificationService Service(
        FakeProjectionReadStore store,
        IConversationTemporalEventSource temporalSource,
        bool allowAccess = true)
    {
        FakeTenantAccessService tenantAccess = new(allowAccess);
        ConversationProjectionReadService projectionReadService = new(tenantAccess, store);
        return new ConversationGovernanceVerificationService(
            tenantAccess,
            projectionReadService,
            store,
            temporalSource,
            new ConversationProjectionRebuildVerifier(new ConversationProjectionMaterializer()));
    }

    private static ConversationProjectedReadModels Materialize(
        IReadOnlyList<ConversationProjectionEventRecord> events,
        ProjectionTrustState? freshnessState = null,
        ProjectionFreshnessReasonCode? reasonCode = null)
    {
        ConversationProjectedReadModels models = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            events,
            Generated,
            TimeSpan.FromMinutes(5),
            isRebuilding: false);

        if (freshnessState is null || freshnessState == ProjectionTrustState.Current)
        {
            return models;
        }

        ProjectionFreshnessV1 freshness = new(
            SchemaVersion.Current,
            models.Detail.Freshness.ProjectionCursor,
            models.Detail.Freshness.LastAppliedEventPosition,
            models.Detail.Freshness.LastAppliedEventTimestamp,
            models.Detail.Freshness.ProjectionGeneratedAt,
            models.Detail.Freshness.LagDuration,
            IsStale: freshnessState == ProjectionTrustState.Stale,
            freshnessState,
            reasonCode ?? ProjectionFreshnessReasonCode.StaleThresholdExceeded);

        ConversationDetailProjectionV1 detail = Detail(models.Detail, freshness: freshness);
        ConversationSummaryProjectionV1 summary = new(
            models.Summary.SchemaVersion,
            models.Summary.TenantId,
            models.Summary.ConversationId,
            freshness,
            models.Summary.LifecycleState,
            models.Summary.Label,
            models.Summary.BusinessReference,
            models.Summary.ProjectId,
            models.Summary.FolderId,
            models.Summary.ParticipantPartyIds,
            models.Summary.MessageCount,
            models.Summary.FileReferenceCount,
            models.Summary.ProviderCorrelation,
            models.Summary.SearchTrustPreview);

        return new ConversationProjectedReadModels(summary, detail);
    }

    private static ConversationProjectedReadModels WithRedactions(
        ConversationProjectedReadModels models,
        params ConversationRedactionProjectionV1[] redactions)
        => new(models.Summary, Detail(models.Detail, redactions: redactions));

    private static ConversationDetailProjectionV1 Detail(
        ConversationDetailProjectionV1 source,
        ProjectionFreshnessV1? freshness = null,
        IReadOnlyList<ConversationRedactionProjectionV1>? redactions = null)
        => new(
            source.SchemaVersion,
            source.TenantId,
            source.ConversationId,
            freshness ?? source.Freshness,
            source.LifecycleState,
            source.Label,
            source.BusinessReference,
            source.ProjectId,
            source.FolderId,
            source.ProviderCorrelation,
            source.Participants,
            source.Messages,
            source.FileReferences,
            source.Attributes,
            source.ActiveRetentionPolicy,
            source.SensitivityMarks,
            redactions ?? source.Redactions,
            source.TrustPosture,
            source.EvidenceEntries);

    private static ConversationProjectionEventRecord[] OrderedEvents() =>
    [
        Event(1, Created("event-create-001", 1)),
        Event(2, ParticipantAdded("event-participant-001", 2)),
        Event(3, MessageAppended("event-message-001", 3)),
    ];

    private static ConversationReplayEventRecord[] ReplayEvents()
        => OrderedEvents().Select(e => new ConversationReplayEventRecord(e.Position, e.Event)).ToArray();

    private static ConversationProjectionEventRecord Event(long position, object e)
        => new(position, e);

    private static ConversationCreated Created(
        string eventId,
        long position,
        string label = "Case 123",
        SchemaVersion? schemaVersion = null,
        TenantId? tenantId = null,
        string? providerSessionReference = null)
        => new(
            Metadata(eventId, ConversationEventType.ConversationCreated, position, schemaVersion, tenantId),
            new BusinessReference("crm", "case-123"),
            null,
            null,
            label,
            providerSessionReference is null
                ? null
                : new ProviderCorrelationMetadata("contoso-ai", "assistant", SchemaVersion.Current, providerSessionReference));

    private static ParticipantAdded ParticipantAdded(string eventId, long position, TenantId? tenantId = null)
        => new(
            Metadata(eventId, ConversationEventType.ParticipantAdded, position, tenantId: tenantId),
            Participant,
            ParticipantType.Human,
            ParticipantRole.Member);

    private static MessageAppended MessageAppended(
        string eventId,
        long position,
        string providerSessionReference = "provider-session-001")
        => new(
            Metadata(eventId, ConversationEventType.MessageAppended, position),
            Message,
            Actor,
            "Hello",
            new ProviderCorrelationMetadata("contoso-ai", "assistant", SchemaVersion.Current, providerSessionReference));

    private static ConversationEventMetadata Metadata(
        string eventId,
        ConversationEventType eventType,
        long position,
        SchemaVersion? schemaVersion = null,
        TenantId? tenantId = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            eventId,
            eventType,
            tenantId ?? Tenant,
            Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private sealed class FakeTenantAccessService(bool allowAccess) : IConversationTenantAccessService
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
            => ValueTask.FromResult(allowAccess && trustedTenantId is not null && !string.IsNullOrWhiteSpace(callerPrincipalId)
                ? ConversationTenantAccessDecision.Allowed(requirement, trustedTenantId, callerPrincipalId)
                : ConversationTenantAccessDecision.Denied(
                    requirement,
                    trustedTenantId,
                    callerPrincipalId,
                    ConversationTenantAccessDenialReason.MissingTenant));
    }

    private sealed class FakeProjectionReadStore(ConversationProjectedReadModels models) : IConversationProjectionReadStore
    {
        public int ReadCount { get; private set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return ValueTask.FromResult<ConversationProjectedReadModels?>(models);
        }

        public ValueTask<IReadOnlySet<string>> ValidatePageAsync(
            TenantId tenantId,
            ConversationProjectionIndexSnapshot snapshot,
            IReadOnlyList<ConversationSummaryProjectionV1> page,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ProjectionIndexSnapshotTestExtensions.NoInconsistentRows());

        public ValueTask<ConversationProjectionIndexSnapshot> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(((IReadOnlyList<ConversationSummaryProjectionV1>)[models.Summary]).ToConsistentSnapshot());
    }

    private sealed class StaticTemporalEventSource(ConversationTemporalEventSourceResult result) : IConversationTemporalEventSource
    {
        public ValueTask<ConversationTemporalEventSourceResult> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(result);
    }

    private sealed class ThrowingTemporalEventSource : IConversationTemporalEventSource
    {
        public ValueTask<ConversationTemporalEventSourceResult> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }
}
