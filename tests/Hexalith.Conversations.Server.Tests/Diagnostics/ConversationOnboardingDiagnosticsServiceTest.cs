// <copyright file="ConversationOnboardingDiagnosticsServiceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies CORE onboarding diagnostics orchestration stays read-only, fail-closed, and content safe.
/// </summary>
public sealed class ConversationOnboardingDiagnosticsServiceTest
{
    private static readonly TenantId Tenant = new("tenant-alpha");
    private static readonly DateTimeOffset Generated = new(2026, 5, 23, 12, 0, 0, TimeSpan.Zero);

    // Closed-vocabulary tokens such as projection-subscription are safe machine identifiers, so this
    // scan targets protected-value leakage rather than the legitimate vocabulary tokens.
    private static readonly string[] ForbiddenFragments =
    [
        "tenant-alpha", "tenant:", "party:", "conv:", "conversation-", "provider-session",
        "provider payload", "provider response", "EventStore", "envelope", "SignalR",
        "Exception", "boom", "C:\\", "D:\\",
    ];

    [Fact]
    public async Task ReadyEnvironmentShouldReportAllChecksReady()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Ready);
        result.Checks.Select(check => check.Check).ShouldBe(OnboardingDiagnosticCheck.All, ignoreOrder: true);
        result.Checks.ShouldAllBe(check => check.Status == OnboardingDiagnosticStatus.Ready);
        result.Checks.ShouldAllBe(check => check.Error == null);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task MissingTenantContextShouldReturnHiddenUnknownWithoutDisclosure()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            trustedTenantId: null, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Unknown);
        result.Checks.Single().Check.ShouldBe(OnboardingDiagnosticCheck.TenantContext);
        result.Checks.Single().Status.ShouldBe(OnboardingDiagnosticStatus.Unknown);
        result.Checks.Single().Error!.Code.ShouldBe(ConversationErrorCode.TenantBindingMissing);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task DeniedAccessShouldBeSideChannelEquivalentToHiddenUnknown()
    {
        ConversationOnboardingDiagnosticsService denied = Service(NewBuilder().Ready().DenyAccess());

        OnboardingDiagnosticRunResultV1 result = await denied.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticRunResultV1 missing = await Service(NewBuilder().Ready()).RunAsync(
            trustedTenantId: null, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        // Denied and missing-context requests are externally indistinguishable: both unknown, one check.
        result.OverallStatus.ShouldBe(missing.OverallStatus);
        result.Checks.Count.ShouldBe(missing.Checks.Count);
        result.SafeSummary.ShouldBe(missing.SafeSummary);
        AssertContentSafe(result);
    }

    [Theory]
    [InlineData(ConversationTenantAccessDenialReason.TenantAccessStale)]
    [InlineData(ConversationTenantAccessDenialReason.TenantAccessGapDetected)]
    [InlineData(ConversationTenantAccessDenialReason.TenantAccessRolledBack)]
    [InlineData(ConversationTenantAccessDenialReason.TenantProjectionPoisoned)]
    [InlineData(ConversationTenantAccessDenialReason.TenantAccessUnavailable)]
    public async Task FreshnessOrAvailabilityAccessDenialShouldStaySideChannelEquivalentToHiddenUnknown(
        ConversationTenantAccessDenialReason reason)
    {
        // The production ConversationTenantAccessService fails closed on stale/gap/rollback/poisoned/unavailable
        // projection state before the orchestrator runs any check. Per the platform side-channel contract
        // (ConversationTenantAccessDecision.ToRejection), those outcomes must stay externally indistinguishable
        // from an unauthorized or missing-context request so freshness state cannot disclose tenant existence.
        ConversationOnboardingDiagnosticsService denied = Service(NewBuilder().Ready().DenyAccessWith(reason));

        OnboardingDiagnosticRunResultV1 result = await denied.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticRunResultV1 missing = await Service(NewBuilder().Ready()).RunAsync(
            trustedTenantId: null, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Unknown);
        result.OverallStatus.ShouldBe(missing.OverallStatus);
        result.Checks.Count.ShouldBe(missing.Checks.Count);
        result.Checks.Single().Check.ShouldBe(OnboardingDiagnosticCheck.TenantContext);
        result.SafeSummary.ShouldBe(missing.SafeSummary);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task StaleTenantProjectionShouldDegradeProjectionSubscriptionCheck()
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithProjectionHealth(new ConversationTenantProjectionHealth(IsStale: true)));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProjectionSubscription);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        check.Error!.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task ProjectionSubscriptionFailureShouldBlockClosed()
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithProjectionHealth(new ConversationTenantProjectionHealth(IsPoisoned: true)));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProjectionSubscription);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        check.Error!.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Blocked);
    }

    [Fact]
    public async Task AuditSinkUnavailableShouldDegradeAuditCheck()
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithAudit(ConversationGovernanceAuditStatus.AuditUnavailable));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.AuditAvailability);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        check.Error!.Code.ShouldBe(ConversationErrorCode.AuditSinkUnavailable);
        check.Error!.IsRetryable.ShouldBeTrue();
        AssertContentSafe(result);
    }

    [Fact]
    public async Task UnsupportedContractShouldBlockContractAndSchemaChecks()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            new ContractCompatibilityRequest(CommandSchemaVersion: "2"),
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 contract = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ContractVersion);
        OnboardingDiagnosticCheckResultV1 schema = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.SchemaCompatibility);

        contract.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        contract.Error!.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        schema.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task SchemaIncompatibilityFromInvalidVersionShouldBlock()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            new ContractCompatibilityRequest(EventSchemaVersion: "latest"),
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 schema = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.SchemaCompatibility);
        schema.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        schema.Error!.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
    }

    [Fact]
    public async Task MissingProviderConfigurationShouldBlockProviderCheck()
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithProviderConfiguration(present: false));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProviderConfiguration);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        check.Error!.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task PartiesIntegrationUnavailableShouldDegradeAndStayRetryable()
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithDirectory(ParticipantDirectoryValidationStatus.Unavailable));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.PartiesIntegration);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        check.Error!.Code.ShouldBe(ConversationErrorCode.ParticipantValidationUnavailable);
        check.Error!.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    public async Task ThrowingSignalsShouldFailClosedAndStayContentSafe()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready().Throwing());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProjectionSubscription).Status
            .ShouldBe(OnboardingDiagnosticStatus.Blocked);
        result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.AuditAvailability).Status
            .ShouldBe(OnboardingDiagnosticStatus.Degraded);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task DeprecatedContractVersionShouldDegradeWithoutBlocking()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            new ContractCompatibilityRequest(ContractsPackageVersion: "0.9.0"),
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 contract = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ContractVersion);
        OnboardingDiagnosticCheckResultV1 schema = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.SchemaCompatibility);

        // Deprecated is accepted but should surface as degraded, never blocked or silently ready.
        contract.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        contract.Error!.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        contract.RemediationGuidanceCode.ShouldBe("upgrade-to-active-v1");
        schema.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        AssertContentSafe(result);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public async Task InvalidSchemaVersionInputShouldBlockSchemaAndContractChecks(string commandSchemaVersion)
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            new ContractCompatibilityRequest(CommandSchemaVersion: commandSchemaVersion),
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 contract = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ContractVersion);
        OnboardingDiagnosticCheckResultV1 schema = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.SchemaCompatibility);

        contract.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        contract.Error!.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        schema.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        AssertContentSafe(result);
    }

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    public async Task ProjectionGapOrRollbackShouldDegradeProjectionSubscriptionCheck(
        bool isStale,
        bool hasGap,
        bool hasRollback,
        bool isPoisoned)
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithProjectionHealth(
                new ConversationTenantProjectionHealth(
                    IsStale: isStale,
                    HasGap: hasGap,
                    HasRollback: hasRollback,
                    IsPoisoned: isPoisoned)));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProjectionSubscription);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        check.Error!.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task NullProjectionHealthShouldBlockClosed()
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithNullProjectionHealth());

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProjectionSubscription);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        check.Error!.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        AssertContentSafe(result);
    }

    [Fact]
    public async Task BlockedCheckShouldDominateDegradedCheckInOverallStatus()
    {
        // Provider configuration missing (blocked) plus audit unavailable (degraded): blocked must win.
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready()
                .WithProviderConfiguration(present: false)
                .WithAudit(ConversationGovernanceAuditStatus.AuditUnavailable));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.ProviderConfiguration).Status
            .ShouldBe(OnboardingDiagnosticStatus.Blocked);
        result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.AuditAvailability).Status
            .ShouldBe(OnboardingDiagnosticStatus.Degraded);
        result.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Blocked);
        AssertContentSafe(result);
    }

    [Theory]
    [InlineData(ParticipantDirectoryValidationStatus.Inaccessible)]
    [InlineData(ParticipantDirectoryValidationStatus.Timeout)]
    [InlineData(ParticipantDirectoryValidationStatus.Error)]
    [InlineData(ParticipantDirectoryValidationStatus.Unknown)]
    public async Task NonValidPartyDirectoryStatusesShouldDegradeAndStayRetryable(
        ParticipantDirectoryValidationStatus status)
    {
        ConversationOnboardingDiagnosticsService service = Service(
            NewBuilder().Ready().WithDirectory(status));

        OnboardingDiagnosticRunResultV1 result = await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: TestContext.Current.CancellationToken);

        OnboardingDiagnosticCheckResultV1 check = result.Checks.Single(c => c.Check == OnboardingDiagnosticCheck.PartiesIntegration);
        check.Status.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        check.Error!.Code.ShouldBe(ConversationErrorCode.ParticipantValidationUnavailable);
        check.Error!.IsRetryable.ShouldBeTrue();
        AssertContentSafe(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankCorrelationIdShouldFailFastWithoutTouchingSignals(string correlationId)
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready().Throwing());

        await Should.ThrowAsync<ArgumentException>(async () => await service.RunAsync(
            Tenant, "caller-001", correlationId, Generated,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public async Task CanceledTokenShouldPropagateOperationCanceledFromSignals()
    {
        ConversationOnboardingDiagnosticsService service = Service(NewBuilder().Ready().Canceling());
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await service.RunAsync(
            Tenant, "caller-001", "correlation-001", Generated,
            cancellationToken: cts.Token).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public void DiagnosticsServiceShouldNotDependOnMutationExecutionBoundaries()
    {
        Type[] directDependencies =
        [
            .. typeof(ConversationOnboardingDiagnosticsService).GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType),
        ];

        directDependencies.ShouldNotContain(typeof(SetConversationRetentionPolicyCommandHandler));
        directDependencies.ShouldNotContain(typeof(IdempotentConversationCommandExecutor));
        directDependencies.ShouldNotContain(typeof(ConversationGovernanceAuditGate));
        directDependencies.ShouldNotContain(typeof(SetConversationRetentionPolicy));
    }

    [Fact]
    public void AddConversationOnboardingDiagnosticsShouldResolveServiceWithFailClosedDefaults()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IConversationTenantAccessService>(new FakeTenantAccessService(allowAccess: true));
        services.AddSingleton<IConversationTenantProjectionSignal>(
            new FakeProjectionSignal(ConversationTenantProjectionHealth.Healthy));
        services.AddConversationOnboardingDiagnostics();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ConversationOnboardingDiagnosticsService>().ShouldNotBeNull();
    }

    private static void AssertContentSafe(OnboardingDiagnosticRunResultV1 result)
    {
        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (string fragment in ForbiddenFragments)
        {
            json.ShouldNotContain(fragment, Case.Insensitive);
        }

        result.ToString().ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    private static ConversationOnboardingDiagnosticsService Service(Builder builder) => builder.Build();

    private static Builder NewBuilder() => new();

    private sealed class Builder
    {
        private bool _allowAccess;
        private ConversationTenantAccessDenialReason _denialReason = ConversationTenantAccessDenialReason.InsufficientRole;
        private ConversationTenantProjectionHealth? _health = ConversationTenantProjectionHealth.Healthy;
        private ConversationGovernanceAuditStatus _audit = ConversationGovernanceAuditStatus.Succeeded;
        private ParticipantDirectoryValidationStatus _directory = ParticipantDirectoryValidationStatus.Valid;
        private bool _providerPresent = true;
        private bool _throwing;
        private bool _canceling;

        public Builder Ready()
        {
            _allowAccess = true;
            return this;
        }

        public Builder DenyAccess()
        {
            _allowAccess = false;
            return this;
        }

        public Builder DenyAccessWith(ConversationTenantAccessDenialReason reason)
        {
            _allowAccess = false;
            _denialReason = reason;
            return this;
        }

        public Builder WithProjectionHealth(ConversationTenantProjectionHealth health)
        {
            _health = health;
            return this;
        }

        public Builder WithNullProjectionHealth()
        {
            _health = null;
            return this;
        }

        public Builder WithAudit(ConversationGovernanceAuditStatus audit)
        {
            _audit = audit;
            return this;
        }

        public Builder WithDirectory(ParticipantDirectoryValidationStatus directory)
        {
            _directory = directory;
            return this;
        }

        public Builder WithProviderConfiguration(bool present)
        {
            _providerPresent = present;
            return this;
        }

        public Builder Throwing()
        {
            _throwing = true;
            return this;
        }

        public Builder Canceling()
        {
            _canceling = true;
            return this;
        }

        public ConversationOnboardingDiagnosticsService Build()
            => new(
                new FakeTenantAccessService(_allowAccess, _denialReason),
                _canceling ? new CancelingProjectionSignal() : _throwing ? new ThrowingProjectionSignal() : new FakeProjectionSignal(_health),
                _throwing ? new ThrowingAuditSignal() : new FakeAuditSignal(_audit),
                _throwing ? new ThrowingDirectorySignal() : new FakeDirectorySignal(_directory),
                _throwing ? new ThrowingProviderSignal() : new FakeProviderSignal(_providerPresent));
    }

    private sealed class FakeTenantAccessService(
        bool allowAccess,
        ConversationTenantAccessDenialReason denialReason = ConversationTenantAccessDenialReason.InsufficientRole)
        : IConversationTenantAccessService
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
                    denialReason,
                    isRetryable: denialReason
                        is ConversationTenantAccessDenialReason.TenantAccessStale
                        or ConversationTenantAccessDenialReason.TenantAccessGapDetected
                        or ConversationTenantAccessDenialReason.TenantAccessRolledBack
                        or ConversationTenantAccessDenialReason.TenantAccessUnavailable));
    }

    private sealed class FakeProjectionSignal(ConversationTenantProjectionHealth? health) : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(health!);
    }

    private sealed class ThrowingProjectionSignal : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    private sealed class CancelingProjectionSignal : IConversationTenantProjectionSignal
    {
        public ValueTask<ConversationTenantProjectionHealth> GetProjectionHealthAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(ConversationTenantProjectionHealth.Healthy);
        }
    }

    private sealed class FakeAuditSignal(ConversationGovernanceAuditStatus status) : IConversationAuditAvailabilitySignal
    {
        public ValueTask<ConversationGovernanceAuditStatus> GetAuditAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(status);
    }

    private sealed class ThrowingAuditSignal : IConversationAuditAvailabilitySignal
    {
        public ValueTask<ConversationGovernanceAuditStatus> GetAuditAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    private sealed class FakeDirectorySignal(ParticipantDirectoryValidationStatus status) : IParticipantDirectoryAvailabilitySignal
    {
        public ValueTask<ParticipantDirectoryValidationStatus> GetDirectoryAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(status);
    }

    private sealed class ThrowingDirectorySignal : IParticipantDirectoryAvailabilitySignal
    {
        public ValueTask<ParticipantDirectoryValidationStatus> GetDirectoryAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    private sealed class FakeProviderSignal(bool present) : IConversationProviderConfigurationSignal
    {
        public ValueTask<bool> IsProviderConfigurationPresentAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(present);
    }

    private sealed class ThrowingProviderSignal : IConversationProviderConfigurationSignal
    {
        public ValueTask<bool> IsProviderConfigurationPresentAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }
}
