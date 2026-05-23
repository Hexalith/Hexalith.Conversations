// <copyright file="ConversationOnboardingDiagnosticsService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Orchestrates read-only CORE onboarding diagnostics from existing trusted tenant, projection, audit,
/// Parties, and contract-compatibility signals.
/// </summary>
/// <remarks>
/// Modeled on <see cref="ConversationGovernanceVerificationService"/>: a server-owned service that fails
/// closed on missing trusted tenant/caller authority before any tenant data is touched, returns one typed
/// run result composed of per-check results with machine-readable codes, statuses mapped to the shared
/// trust/freshness vocabulary, and bounded remediation, and never leaks protected detail. Failing, degraded,
/// or unknown checks reuse the shared <see cref="ConversationErrorCatalog"/> rather than a parallel envelope.
/// </remarks>
public sealed class ConversationOnboardingDiagnosticsService
{
    private static readonly Uri PreconditionDocumentation =
        new("https://docs.hexalith.local/conversations/contracts/v1/preconditions", UriKind.Absolute);

    private readonly IConversationTenantAccessService _tenantAccessService;
    private readonly IConversationTenantProjectionSignal _projectionSignal;
    private readonly IConversationAuditAvailabilitySignal _auditAvailabilitySignal;
    private readonly IParticipantDirectoryAvailabilitySignal _directoryAvailabilitySignal;
    private readonly IConversationProviderConfigurationSignal _providerConfigurationSignal;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationOnboardingDiagnosticsService"/> class.
    /// </summary>
    public ConversationOnboardingDiagnosticsService(
        IConversationTenantAccessService tenantAccessService,
        IConversationTenantProjectionSignal projectionSignal,
        IConversationAuditAvailabilitySignal auditAvailabilitySignal,
        IParticipantDirectoryAvailabilitySignal directoryAvailabilitySignal,
        IConversationProviderConfigurationSignal providerConfigurationSignal)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _projectionSignal = projectionSignal ?? throw new ArgumentNullException(nameof(projectionSignal));
        _auditAvailabilitySignal = auditAvailabilitySignal ?? throw new ArgumentNullException(nameof(auditAvailabilitySignal));
        _directoryAvailabilitySignal = directoryAvailabilitySignal ?? throw new ArgumentNullException(nameof(directoryAvailabilitySignal));
        _providerConfigurationSignal = providerConfigurationSignal ?? throw new ArgumentNullException(nameof(providerConfigurationSignal));
    }

    /// <summary>
    /// Runs onboarding diagnostics after binding tenant and caller authority from the trusted server boundary.
    /// </summary>
    /// <param name="trustedTenantId">The trusted request tenant binding.</param>
    /// <param name="callerPrincipalId">The trusted caller principal identifier.</param>
    /// <param name="correlationId">The safe correlation identifier.</param>
    /// <param name="generatedAtUtc">The UTC timestamp for the run.</param>
    /// <param name="compatibilityRequest">The adopter-supplied schema/contract versions to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A single content-safe diagnostic run result.</returns>
    public async ValueTask<OnboardingDiagnosticRunResultV1> RunAsync(
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        string correlationId,
        DateTimeOffset generatedAtUtc,
        ContractCompatibilityRequest? compatibilityRequest = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        // Fail closed on missing trusted tenant/caller authority before any tenant data is touched.
        // Denied/missing/cross-tenant requests collapse to a single hidden-equivalent unknown result
        // so they cannot reveal whether a protected tenant exists.
        if (trustedTenantId is null || string.IsNullOrWhiteSpace(callerPrincipalId))
        {
            return Hidden(correlationId, generatedAtUtc);
        }

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Read,
                trustedTenantId,
                callerPrincipalId,
                routeTenantId: trustedTenantId,
                projectionTenantId: trustedTenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return Hidden(correlationId, generatedAtUtc);
        }

        OnboardingDiagnosticCheckResultV1 tenantContext = EvaluateTenantContext(correlationId);
        OnboardingDiagnosticCheckResultV1 contractVersion = EvaluateContractVersion(correlationId, compatibilityRequest);
        OnboardingDiagnosticCheckResultV1 schemaCompatibility = EvaluateSchemaCompatibility(correlationId, compatibilityRequest);
        OnboardingDiagnosticCheckResultV1 projectionSubscription = await EvaluateProjectionSubscriptionAsync(
            trustedTenantId, correlationId, cancellationToken).ConfigureAwait(false);
        OnboardingDiagnosticCheckResultV1 auditAvailability = await EvaluateAuditAvailabilityAsync(
            trustedTenantId, correlationId, cancellationToken).ConfigureAwait(false);
        OnboardingDiagnosticCheckResultV1 partiesIntegration = await EvaluatePartiesIntegrationAsync(
            trustedTenantId, correlationId, cancellationToken).ConfigureAwait(false);
        OnboardingDiagnosticCheckResultV1 providerConfiguration = await EvaluateProviderConfigurationAsync(
            trustedTenantId, correlationId, cancellationToken).ConfigureAwait(false);

        OnboardingDiagnosticCheckResultV1[] checks =
        [
            tenantContext,
            contractVersion,
            providerConfiguration,
            projectionSubscription,
            schemaCompatibility,
            auditAvailability,
            partiesIntegration,
        ];

        return BuildResult(correlationId, generatedAtUtc, checks);
    }

    private static OnboardingDiagnosticCheckResultV1 EvaluateTenantContext(string correlationId)
        => Ready(
            OnboardingDiagnosticCheck.TenantContext,
            "Tenant context is current and access is allowed.",
            ["AC2", "AC3"]);

    private static OnboardingDiagnosticCheckResultV1 EvaluateContractVersion(
        string correlationId,
        ContractCompatibilityRequest? compatibilityRequest)
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(
            compatibilityRequest ?? new ContractCompatibilityRequest());

        return CompatibilityCheck(OnboardingDiagnosticCheck.ContractVersion, correlationId, result, ["AC2", "AC3"]);
    }

    private static OnboardingDiagnosticCheckResultV1 EvaluateSchemaCompatibility(
        string correlationId,
        ContractCompatibilityRequest? compatibilityRequest)
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(
            compatibilityRequest ?? new ContractCompatibilityRequest());

        return CompatibilityCheck(OnboardingDiagnosticCheck.SchemaCompatibility, correlationId, result, ["AC2", "AC3"]);
    }

    private static OnboardingDiagnosticCheckResultV1 CompatibilityCheck(
        OnboardingDiagnosticCheck check,
        string correlationId,
        ContractCompatibilityResult result,
        IReadOnlyList<string> requirementMappings)
    {
        if (result.Status == ContractCompatibilityStatus.Supported)
        {
            return Ready(check, "Requested contract and schema versions are supported.", requirementMappings);
        }

        if (result.Status == ContractCompatibilityStatus.Deprecated)
        {
            // Deprecated is still accepted; surface it as degraded with the versioning catalog code.
            ConversationError deprecatedError = ConversationErrorCatalog.CreateError(
                ConversationErrorCode.SchemaVersionUnsupported,
                correlationId,
                developerGuidance: "Upgrade to the active v1 contracts and client package.");
            return Blocked(
                check,
                OnboardingDiagnosticStatus.Degraded,
                "Requested versions are deprecated; upgrade to the active v1 contracts.",
                "upgrade-to-active-v1",
                deprecatedError,
                requirementMappings);
        }

        ConversationError error = result.Error ?? ConversationErrorCatalog.CreateError(
            ConversationErrorCode.SchemaVersionUnsupported,
            correlationId,
            developerGuidance: "Use supported Conversations contract and client versions.");

        return Blocked(
            check,
            OnboardingDiagnosticStatus.Blocked,
            "Requested contract or schema version is not supported.",
            "use-supported-version",
            error,
            requirementMappings);
    }

    private async ValueTask<OnboardingDiagnosticCheckResultV1> EvaluateProjectionSubscriptionAsync(
        TenantId tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ConversationTenantProjectionHealth? health;
        try
        {
            health = await _projectionSignal.GetProjectionHealthAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Blocked(
                OnboardingDiagnosticCheck.ProjectionSubscription,
                OnboardingDiagnosticStatus.Blocked,
                "Projection subscription health could not be read; failing closed.",
                "retry-after-projection-current",
                ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantProjectionStale, correlationId),
                ["AC2", "AC3"]);
        }

        if (health is null || health.IsPoisoned)
        {
            return Blocked(
                OnboardingDiagnosticCheck.ProjectionSubscription,
                OnboardingDiagnosticStatus.Blocked,
                "Projection subscription is unavailable; reads cannot be trust-bearing.",
                "retry-after-projection-current",
                ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantProjectionStale, correlationId),
                ["AC2", "AC3"]);
        }

        if (health.HasGap || health.HasRollback || health.IsStale)
        {
            // Stale/rebuilding/gap/rollback map to the Stale/Rebuilding trust language: a bounded
            // degraded status with safe retry remediation, never trust-bearing.
            return Blocked(
                OnboardingDiagnosticCheck.ProjectionSubscription,
                OnboardingDiagnosticStatus.Degraded,
                "Projection subscription is not current; retry after it is refreshed.",
                "retry-after-projection-current",
                ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantProjectionStale, correlationId),
                ["AC2", "AC3"]);
        }

        return Ready(
            OnboardingDiagnosticCheck.ProjectionSubscription,
            "Projection subscription is current.",
            ["AC2", "AC3"]);
    }

    private async ValueTask<OnboardingDiagnosticCheckResultV1> EvaluateAuditAvailabilityAsync(
        TenantId tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ConversationGovernanceAuditStatus status;
        try
        {
            status = await _auditAvailabilitySignal.GetAuditAvailabilityAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            status = ConversationGovernanceAuditStatus.AuditUnavailable;
        }

        if (status == ConversationGovernanceAuditStatus.Succeeded)
        {
            return Ready(
                OnboardingDiagnosticCheck.AuditAvailability,
                "Audit recording is available.",
                ["AC2", "AC3"]);
        }

        return Blocked(
            OnboardingDiagnosticCheck.AuditAvailability,
            OnboardingDiagnosticStatus.Degraded,
            "Audit recording is not currently available; retry later.",
            "retry-after-audit-available",
            ConversationErrorCatalog.CreateError(ConversationErrorCode.AuditSinkUnavailable, correlationId),
            ["AC2", "AC3"]);
    }

    private async ValueTask<OnboardingDiagnosticCheckResultV1> EvaluatePartiesIntegrationAsync(
        TenantId tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ParticipantDirectoryValidationStatus status;
        try
        {
            status = await _directoryAvailabilitySignal.GetDirectoryAvailabilityAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            status = ParticipantDirectoryValidationStatus.Unavailable;
        }

        if (status == ParticipantDirectoryValidationStatus.Valid)
        {
            return Ready(
                OnboardingDiagnosticCheck.PartiesIntegration,
                "Party validation is available.",
                ["AC2", "AC3"]);
        }

        return Blocked(
            OnboardingDiagnosticCheck.PartiesIntegration,
            OnboardingDiagnosticStatus.Degraded,
            "Participant identity validation is not currently available; retry later.",
            "retry-after-participant-validation-available",
            ConversationErrorCatalog.CreateError(ConversationErrorCode.ParticipantValidationUnavailable, correlationId),
            ["AC2", "AC3"]);
    }

    private async ValueTask<OnboardingDiagnosticCheckResultV1> EvaluateProviderConfigurationAsync(
        TenantId tenantId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        bool present;
        try
        {
            present = await _providerConfigurationSignal.IsProviderConfigurationPresentAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            present = false;
        }

        if (present)
        {
            return Ready(
                OnboardingDiagnosticCheck.ProviderConfiguration,
                "Required provider configuration is present.",
                ["AC2", "AC3"]);
        }

        return Blocked(
            OnboardingDiagnosticCheck.ProviderConfiguration,
            OnboardingDiagnosticStatus.Blocked,
            "Required provider configuration is missing.",
            "supply-required-configuration",
            ConversationErrorCatalog.CreateError(
                ConversationErrorCode.CommandValidationFailed,
                correlationId,
                developerGuidance: "Supply the required Conversations provider configuration."),
            ["AC2", "AC3"]);
    }

    private static OnboardingDiagnosticCheckResultV1 Ready(
        OnboardingDiagnosticCheck check,
        string safeMessage,
        IReadOnlyList<string> requirementMappings)
        => new(
            SchemaVersion.Current,
            check,
            OnboardingDiagnosticStatus.Ready,
            safeMessage,
            "none",
            PreconditionDocumentation,
            requirementMappings);

    private static OnboardingDiagnosticCheckResultV1 Blocked(
        OnboardingDiagnosticCheck check,
        OnboardingDiagnosticStatus status,
        string safeMessage,
        string remediationGuidanceCode,
        ConversationError error,
        IReadOnlyList<string> requirementMappings)
        => new(
            SchemaVersion.Current,
            check,
            status,
            safeMessage,
            remediationGuidanceCode,
            error.Documentation ?? PreconditionDocumentation,
            requirementMappings,
            error,
            error.AuditHandle);

    private static OnboardingDiagnosticRunResultV1 Hidden(string correlationId, DateTimeOffset generatedAtUtc)
    {
        // Side-channel equivalence: a denied/missing/cross-tenant request returns a single unknown
        // tenant-context check that does not reveal whether a protected tenant exists.
        OnboardingDiagnosticCheckResultV1 hidden = new(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.TenantContext,
            OnboardingDiagnosticStatus.Unknown,
            "Diagnostics are unavailable for the supplied context.",
            "provide-authenticated-context",
            PreconditionDocumentation,
            ["AC2", "AC3"],
            ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantBindingMissing, correlationId));

        return new OnboardingDiagnosticRunResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticStatus.Unknown,
            "Diagnostics are unavailable for the supplied context.",
            correlationId,
            generatedAtUtc,
            [hidden]);
    }

    private static OnboardingDiagnosticRunResultV1 BuildResult(
        string correlationId,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<OnboardingDiagnosticCheckResultV1> checks)
    {
        OnboardingDiagnosticStatus overall = OverallStatus(checks);
        return new OnboardingDiagnosticRunResultV1(
            SchemaVersion.Current,
            overall,
            SummaryFor(overall),
            correlationId,
            generatedAtUtc,
            checks);
    }

    private static OnboardingDiagnosticStatus OverallStatus(IReadOnlyList<OnboardingDiagnosticCheckResultV1> checks)
    {
        // Priority ordering keeps the aggregate honest: any blocked precondition blocks the run, any
        // unknown leaves it unknown, any degraded leaves it degraded, otherwise it is ready.
        if (checks.Any(check => check.Status == OnboardingDiagnosticStatus.Blocked))
        {
            return OnboardingDiagnosticStatus.Blocked;
        }

        if (checks.Any(check => check.Status == OnboardingDiagnosticStatus.Unknown))
        {
            return OnboardingDiagnosticStatus.Unknown;
        }

        return checks.Any(check => check.Status == OnboardingDiagnosticStatus.Degraded)
            ? OnboardingDiagnosticStatus.Degraded
            : OnboardingDiagnosticStatus.Ready;
    }

    private static string SummaryFor(OnboardingDiagnosticStatus status)
    {
        if (status == OnboardingDiagnosticStatus.Ready)
        {
            return "All CORE preconditions are ready.";
        }

        if (status == OnboardingDiagnosticStatus.Degraded)
        {
            return "Some CORE preconditions are degraded; review per-check remediation.";
        }

        return status == OnboardingDiagnosticStatus.Blocked
            ? "Some CORE preconditions are not met; review per-check remediation."
            : "Diagnostics are unavailable for the supplied context.";
    }
}
