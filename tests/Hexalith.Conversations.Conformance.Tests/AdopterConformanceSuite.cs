// <copyright file="AdopterConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Testing.Fixtures;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Runs the adopter-facing CORE conformance suite against the deterministic synthetic fixture and produces a
/// machine-readable, content-safe <see cref="ConformanceRunResultV1"/> suitable for CI consumption.
/// </summary>
/// <remarks>
/// The suite reuses existing CORE primitives rather than rebuilding them: Story 4.1
/// <see cref="ConversationContractCompatibility.Evaluate(ContractCompatibilityRequest)"/> for compatibility
/// discovery, Story 4.3 <see cref="ConversationErrorCatalog"/>/<see cref="ConversationError"/> for the error
/// envelope and typed failure cases, Story 4.4 <see cref="ConversationCorePreconditionCatalog"/> for governance
/// preconditions, the shared <see cref="ProjectionTrustState"/> trust/freshness vocabulary, and the Story 3.7
/// synthetic-fixture pattern via <see cref="ConversationConformanceCoreFixtures"/>. It is read-oriented and
/// side-effect-free: it appends no events, mutates no aggregate state, writes no production projection store,
/// persists no export artifact, and requires no nested submodule initialization.
/// </remarks>
public sealed class AdopterConformanceSuite
{
    private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");

    private readonly ConversationConformanceCoreSeedData _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdopterConformanceSuite"/> class.
    /// </summary>
    /// <param name="fixture">The synthetic CORE fixture data.</param>
    public AdopterConformanceSuite(ConversationConformanceCoreSeedData fixture)
        => _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

    /// <summary>
    /// Runs every CORE conformance check and aggregates the results into a machine-readable run result.
    /// </summary>
    /// <returns>The content-safe machine-readable conformance run result.</returns>
    public ConformanceRunResultV1 Run()
    {
        ConformanceCheckResultV1[] results =
        [
            CheckCreateConversation(),
            CheckAppendMessage(),
            CheckReadTimeline(),
            CheckTenantBinding(),
            CheckPartyIdentity(),
            CheckIdempotency(),
            CheckErrorEnvelope(),
            CheckProjectionFreshness(),
            CheckEventPublication(),
            CheckGovernancePrecondition(),
            CheckCompatibilityDiscovery(),
        ];

        bool anyFailure = results.Any(result => result.FailureClassification.IsFailure);
        bool anyDegraded = results.Any(result => result.Outcome.Equals(ConformanceOutcome.Degraded));

        ConformanceOutcome overallOutcome = anyFailure
            ? ConformanceOutcome.Blocked
            : anyDegraded
                ? ConformanceOutcome.Degraded
                : ConformanceOutcome.Ready;
        ConformanceFailureClassification overallClassification = anyFailure
            ? results.First(result => result.FailureClassification.IsFailure).FailureClassification
            : ConformanceFailureClassification.Conformant;

        return new ConformanceRunResultV1(
            SchemaVersion.Current,
            overallOutcome,
            overallClassification,
            anyFailure
                ? "One or more CORE conformance checks did not match the required contract behavior."
                : "All CORE conformance checks matched the required contract behavior against the synthetic fixture.",
            "adopter-core-conformance-v1",
            "conformance-runner",
            "correlation-conformance-run",
            _fixture.GeneratedAtUtc,
            results);
    }

    private ConformanceCheckResultV1 CheckCreateConversation()
    {
        ConversationSummaryProjectionV1 summary = _fixture.HappyPathSummary;
        bool conformant = summary.TenantId == _fixture.AuthorizedTenantId
            && summary.LifecycleState == "Open"
            && summary.BusinessReference is not null
            && summary.Freshness.AllowsTrustBearingDecision();

        return Conformant(
            ConformanceCheck.CreateConversation,
            "supported",
            conformant,
            ["FR73", "FR74"],
            ["supported-schema-versions", "idempotency-key-behavior"],
            ["release-gate-commands-queries-events"],
            "Create conversation produced a tenant scoped record with current evidence.",
            "conformance-create-conversation");
    }

    private ConformanceCheckResultV1 CheckAppendMessage()
    {
        ConversationDetailProjectionV1 detail = _fixture.HappyPathDetail;
        bool conformant = detail.Messages.Count >= 1
            && detail.Messages.All(message => message.AuthorPartyId is not null && !string.IsNullOrWhiteSpace(message.Text));

        return Conformant(
            ConformanceCheck.AppendMessage,
            "supported",
            conformant,
            ["FR73", "FR74"],
            ["idempotency-key-behavior"],
            ["release-gate-commands-queries-events"],
            "Append message produced a timeline message with stable Party attribution.",
            "conformance-append-message");
    }

    private ConformanceCheckResultV1 CheckReadTimeline()
    {
        ConversationDetailProjectionV1 detail = _fixture.HappyPathDetail;
        bool conformant = detail.TenantId == _fixture.AuthorizedTenantId
            && detail.Messages.Count >= 1
            && detail.Participants.Count >= 1
            && detail.Freshness.AllowsTrustBearingDecision();

        return Conformant(
            ConformanceCheck.ReadTimeline,
            "supported",
            conformant,
            ["FR73", "FR74"],
            ["projection-subscription-health"],
            ["release-gate-commands-queries-events", "release-gate-projection-freshness"],
            "Read timeline returned authorized participants and messages with current evidence.",
            "conformance-read-timeline");
    }

    private ConformanceCheckResultV1 CheckTenantBinding()
    {
        // Tenant binding is the CORE behavior that proves cross-tenant isolation, so the conformance check
        // exercises the AC4 cross-tenant scenario directly: an authorized read must stay scoped to the
        // authorized tenant, AND a cross-tenant request must collapse to the hidden/unavailable side-channel
        // shape (aggregate_not_found / HideOrRefresh, non-retryable) so an unauthorized caller cannot
        // distinguish a forbidden tenant/conversation from a nonexistent one. The conformant observation is
        // therefore the hidden 'unknown' outcome carrying the typed cross-tenant error, never a disclosure.
        ConversationDetailProjectionV1 detail = _fixture.HappyPathDetail;
        ConversationError denial = _fixture.CrossTenantDenialFailure.Error;
        bool authorizedScoped = detail.TenantId == _fixture.AuthorizedTenantId
            && detail.TenantId != _fixture.PoisonTenantId;
        bool hiddenSideChannel = denial.Code == ConversationErrorCode.AggregateNotFound
            && denial.Category == ConversationErrorCategory.Hidden
            && denial.ClientAction == ConversationErrorClientAction.HideOrRefresh
            && !denial.IsRetryable;
        bool conformant = authorizedScoped && hiddenSideChannel;

        return TypedFailureCheck(
            ConformanceCheck.TenantBinding,
            _fixture.CrossTenantDenialFailure.Scenario,
            conformant ? ConformanceOutcome.Unknown : ConformanceOutcome.Blocked,
            conformant,
            ["FR74"],
            ["projection-freshness", "participant-identity-validation"],
            ["release-gate-tenant-isolation"],
            "Tenant binding scoped the authorized read and collapsed the cross-tenant request to the hidden side-channel-equivalent shape without revealing existence.",
            "hide-or-refresh",
            denial);
    }

    private ConformanceCheckResultV1 CheckPartyIdentity()
    {
        ConversationDetailProjectionV1 detail = _fixture.HappyPathDetail;
        bool conformant = detail.Participants.Count >= 1
            && detail.Participants.All(participant => participant.ParticipantPartyId is not null);

        return Conformant(
            ConformanceCheck.PartyIdentity,
            "supported",
            conformant,
            ["FR74"],
            ["participant-identity-validation"],
            ["release-gate-tenant-isolation"],
            "Party identity is represented by stable Party references rather than provider-only identity.",
            "conformance-partyidentity");
    }

    private ConformanceCheckResultV1 CheckIdempotency()
    {
        // A duplicate command with a changed payload must surface a non-retryable idempotency conflict.
        ConversationError error = _fixture.IdempotencyConflictFailure.Error;
        bool conformant = error.Code == ConversationErrorCode.IdempotencyConflict
            && !error.IsRetryable
            && error.ClientAction == ConversationErrorClientAction.UseNewIdempotencyKey;

        return TypedFailureCheck(
            ConformanceCheck.Idempotency,
            _fixture.IdempotencyConflictFailure.Scenario,
            conformant ? ConformanceOutcome.Blocked : ConformanceOutcome.Unknown,
            conformant,
            ["FR74"],
            ["idempotency-key-behavior"],
            ["release-gate-idempotent-commands"],
            "Duplicate command with a changed payload surfaced a non-retryable idempotency conflict.",
            "use-new-idempotency-key",
            error);
    }

    private ConformanceCheckResultV1 CheckErrorEnvelope()
    {
        ConversationError error = _fixture.SanitizedErrorFailure.Error;
        ConversationErrorDescriptor descriptor = ConversationErrorCatalog.Get(error.Code);
        bool conformant = error.Category == descriptor.Category
            && error.IsRetryable == descriptor.IsRetryable
            && error.Documentation is not null
            && error.SafeMessage is not null;

        return TypedFailureCheck(
            ConformanceCheck.ErrorEnvelope,
            _fixture.SanitizedErrorFailure.Scenario,
            conformant ? ConformanceOutcome.Blocked : ConformanceOutcome.Unknown,
            conformant,
            ["FR74"],
            ["required-configuration"],
            ["release-gate-error-envelope"],
            "Failure surfaced the shared typed error contract with stable code, category, and safe message.",
            "inspect-typed-error-contract",
            error);
    }

    private ConformanceCheckResultV1 CheckProjectionFreshness()
    {
        ProjectionFreshnessV1 freshness = _fixture.StaleDetail.Freshness;
        // Only Current is trust-bearing; a stale projection must be reported as degraded (non-trust-bearing).
        bool conformant = freshness.FreshnessState == ProjectionTrustState.Stale
            && freshness.IsStale
            && !freshness.AllowsTrustBearingDecision();

        return new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.ProjectionFreshness,
            "projection-lag",
            conformant ? ConformanceOutcome.Degraded : ConformanceOutcome.Unknown,
            conformant ? ConformanceFailureClassification.Conformant : ConformanceFailureClassification.ProductInvariant,
            ["FR74"],
            ["projection-freshness", "projection-subscription-health"],
            ["release-gate-projection-freshness"],
            "Stale projection was reported as a non-trust-bearing degraded state using the shared trust/freshness vocabulary.",
            conformant ? "retry-after-projection-current" : "inspect-projection-freshness",
            Documentation,
            "conformance-projection-freshness",
            ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantProjectionStale, "conformance-projection-freshness"));
    }

    private ConformanceCheckResultV1 CheckEventPublication()
    {
        // Published event schema versions must remain supported by the active contract.
        ContractCompatibilityResult evaluation = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(EventSchemaVersion: SchemaVersion.Current.Value.ToString()));
        bool conformant = evaluation.Status == ContractCompatibilityStatus.Supported && evaluation.Error is null;

        return Conformant(
            ConformanceCheck.EventPublication,
            "supported",
            conformant,
            ["FR74"],
            ["supported-schema-versions"],
            ["release-gate-event-schema-evolution"],
            "Published event schema version is supported by the active contract.",
            "conformance-event-publication");
    }

    private ConformanceCheckResultV1 CheckGovernancePrecondition()
    {
        // Every documented CORE precondition must require trust-bearing Current state and a typed unmet error.
        bool conformant = ConversationCorePreconditionCatalog.All.Count > 0
            && ConversationCorePreconditionCatalog.All.All(precondition =>
                precondition.RequiredTrustState == ProjectionTrustState.Current
                && precondition.UnmetErrorCode is not null);

        return Conformant(
            ConformanceCheck.GovernancePrecondition,
            "supported",
            conformant,
            ["FR74"],
            ["audit-sink-availability", "required-configuration"],
            ["release-gate-tenant-isolation"],
            "Documented CORE governance preconditions require current trust-bearing state and a typed unmet error.",
            "conformance-governance-precondition");
    }

    private ConformanceCheckResultV1 CheckCompatibilityDiscovery()
    {
        // An unsupported version must produce a typed versioning error and bounded remediation rather than
        // being processed under an incompatible contract.
        ContractCompatibilityResult evaluation = ConversationContractCompatibility.Evaluate(
            new ContractCompatibilityRequest(CommandSchemaVersion: (SchemaVersion.Current.Value + 1).ToString()));
        bool conformant = evaluation.Status == ContractCompatibilityStatus.Unsupported
            && evaluation.Error is not null
            && evaluation.Error.Code == ConversationErrorCode.SchemaVersionUnsupported
            && evaluation.Remediations.Count >= 1;

        return TypedFailureCheck(
            ConformanceCheck.CompatibilityDiscovery,
            "unsupported",
            conformant ? ConformanceOutcome.Blocked : ConformanceOutcome.Unknown,
            conformant,
            ["FR74"],
            ["contract-compatibility", "supported-schema-versions"],
            ["release-gate-version-discovery"],
            "Unsupported version discovery returned a typed versioning error and bounded remediation.",
            "use-supported-v1-package",
            conformant
                ? evaluation.Error!
                : ConversationErrorCatalog.CreateError(ConversationErrorCode.SchemaVersionUnsupported, "conformance-compatibility-discovery"));
    }

    private static ConformanceCheckResultV1 Conformant(
        ConformanceCheck check,
        string scenario,
        bool conformant,
        IReadOnlyList<string> requirements,
        IReadOnlyList<string> preconditions,
        IReadOnlyList<string> releaseGates,
        string safeMessage,
        string correlationId)
        => new(
            SchemaVersion.Current,
            check,
            scenario,
            conformant ? ConformanceOutcome.Ready : ConformanceOutcome.Blocked,
            conformant ? ConformanceFailureClassification.Conformant : ConformanceFailureClassification.ProductInvariant,
            requirements,
            preconditions,
            releaseGates,
            safeMessage,
            conformant ? "none" : "inspect-conformance-failure",
            Documentation,
            correlationId,
            conformant ? null : ConversationErrorCatalog.CreateError(ConversationErrorCode.CommandValidationFailed, correlationId));

    private static ConformanceCheckResultV1 TypedFailureCheck(
        ConformanceCheck check,
        string scenario,
        ConformanceOutcome expectedOutcome,
        bool conformant,
        IReadOnlyList<string> requirements,
        IReadOnlyList<string> preconditions,
        IReadOnlyList<string> releaseGates,
        string safeMessage,
        string remediationGuidanceCode,
        ConversationError error)
        => new(
            SchemaVersion.Current,
            check,
            scenario,
            expectedOutcome,
            conformant ? ConformanceFailureClassification.Conformant : ConformanceFailureClassification.ProductInvariant,
            requirements,
            preconditions,
            releaseGates,
            safeMessage,
            conformant ? remediationGuidanceCode : "inspect-conformance-failure",
            Documentation,
            $"correlation-{scenario}",
            error);
}
