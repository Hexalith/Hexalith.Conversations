// <copyright file="BuyerAcceptanceDemoFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Provides deterministic synthetic data for the buyer acceptance walkthrough.
/// </summary>
public static class BuyerAcceptanceDemoFixtures
{
    /// <summary>
    /// Marks every client-observable fixture as synthetic demo data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-demo-data";

    private static readonly DateTimeOffset GeneratedAtUtc = new(2026, 5, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly TenantId AuthorizedTenant = new("demo-tenant");
    private static readonly TenantId PoisonTenant = new("poison-tenant");
    private static readonly PartyId Actor = new("party-demo-actor");
    private static readonly PartyId Participant = new("party-demo-participant");
    private static readonly BusinessReference Business = new("buyer-demo", "case-acceptance");
    private static readonly ProjectId Project = new("project-demo");
    private static readonly FolderId Folder = new("folder-demo");
    private static readonly AuditEvidenceHandle AuditHandle = new("audit-evidence-demo-001");
    private static readonly GovernanceAuditEvidenceReference AuditEvidence = new(
        AuditHandle,
        "buyer-acceptance-policy",
        GeneratedAtUtc);

    /// <summary>
    /// Creates deterministic synthetic demo data.
    /// </summary>
    /// <returns>The complete seeded acceptance data set.</returns>
    public static BuyerAcceptanceDemoSeedData Create()
    {
        BuyerAcceptanceDemoProjectionPair full = Projection(
            "full",
            BuyerAcceptanceDemoFixtureKind.FullTrust,
            BuyerAcceptanceDemoTrustState.Current,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified);
        BuyerAcceptanceDemoProjectionPair redacted = Projection(
            "redacted",
            BuyerAcceptanceDemoFixtureKind.Redacted,
            BuyerAcceptanceDemoTrustState.Redacted,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified,
            redacted: true);
        BuyerAcceptanceDemoProjectionPair stale = Projection(
            "stale",
            BuyerAcceptanceDemoFixtureKind.Stale,
            BuyerAcceptanceDemoTrustState.Stale,
            ProjectionTrustState.Stale,
            ConversationCitationAvailability.Unavailable,
            ConversationAuditReadinessState.Incomplete,
            ConversationVerificationState.Unknown);
        BuyerAcceptanceDemoProjectionPair missingCitation = Projection(
            "missing-citation",
            BuyerAcceptanceDemoFixtureKind.MissingCitation,
            BuyerAcceptanceDemoTrustState.Incomplete,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Unavailable,
            ConversationAuditReadinessState.Incomplete,
            ConversationVerificationState.Unknown,
            includeAuditEvidence: false);
        BuyerAcceptanceDemoProjectionPair unresolvedParticipant = Projection(
            "unresolved-participant",
            BuyerAcceptanceDemoFixtureKind.UnresolvedParticipant,
            BuyerAcceptanceDemoTrustState.Unavailable,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Unknown,
            participantState: ProjectionTrustState.Unavailable);
        BuyerAcceptanceDemoProjectionPair blockedCommand = Projection(
            "blocked-command",
            BuyerAcceptanceDemoFixtureKind.BlockedCommand,
            BuyerAcceptanceDemoTrustState.Current,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Unknown,
            includeBlockedCommand: true);

        BuyerAcceptanceDemoScenarioV1 scenario = Scenario(
            [
                full,
                redacted,
                stale,
                missingCitation,
                unresolvedParticipant,
                blockedCommand,
            ]);

        ConversationGovernanceVerificationRunResultV1 pass = Verification(
            ConversationGovernanceVerificationExecutionStatus.Completed,
            ConversationGovernanceVerificationFailureClassification.Passed,
            "Governance verification passed.",
            "Trusted scope matches derived records.");
        ConversationGovernanceVerificationRunResultV1 fail = Verification(
            ConversationGovernanceVerificationExecutionStatus.Failed,
            ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            "Governance verification did not pass.",
            "Governed state is missing paired audit references.");

        return new BuyerAcceptanceDemoSeedData(
            scenario,
            GeneratedAtUtc,
            AuthorizedTenant,
            PoisonTenant,
            [full, redacted, stale, missingCitation, unresolvedParticipant, blockedCommand],
            PoisonProjection(),
            pass,
            fail,
            ["POISON-SENTINEL-alpha", "POISON-SENTINEL-beta"]);
    }

    private static BuyerAcceptanceDemoScenarioV1 Scenario(IReadOnlyList<BuyerAcceptanceDemoProjectionPair> projections)
        => new(
            SchemaVersion.Current,
            AuthorizedTenant,
            "buyer-acceptance-demo-v1",
            SyntheticDataMarker,
            "Buyer acceptance demo",
            "correlation-buyer-demo",
            [
                .. projections.Select(pair => Fixture(pair.FixtureId, pair.FixtureKind, pair.TrustState, pair.Summary.ConversationId)),
                Fixture(
                    "fixture-verification-pass",
                    BuyerAcceptanceDemoFixtureKind.VerificationPass,
                    BuyerAcceptanceDemoTrustState.Current,
                    projections[0].Summary.ConversationId),
                Fixture(
                    "fixture-verification-failure",
                    BuyerAcceptanceDemoFixtureKind.VerificationFailure,
                    BuyerAcceptanceDemoTrustState.Failed,
                    projections[0].Summary.ConversationId),
                Fixture(
                    "fixture-cross-scope-denial",
                    BuyerAcceptanceDemoFixtureKind.CrossTenantPoison,
                    BuyerAcceptanceDemoTrustState.Hidden,
                    projections[0].Summary.ConversationId),
            ],
            [
                Step("step-find", BuyerAcceptanceDemoStepKind.Find, BuyerAcceptanceDemoFixtureKind.FullTrust, projections[0].Summary.ConversationId),
                Step("step-read", BuyerAcceptanceDemoStepKind.ReadDetail, BuyerAcceptanceDemoFixtureKind.FullTrust, projections[0].Summary.ConversationId),
                Step("step-redaction", BuyerAcceptanceDemoStepKind.RedactionAudit, BuyerAcceptanceDemoFixtureKind.Redacted, projections[1].Summary.ConversationId),
                Step("step-citation", BuyerAcceptanceDemoStepKind.CitationCopy, BuyerAcceptanceDemoFixtureKind.FullTrust, projections[0].Summary.ConversationId),
                Step("step-temporal", BuyerAcceptanceDemoStepKind.TemporalReconstruction, BuyerAcceptanceDemoFixtureKind.FullTrust, projections[0].Summary.ConversationId),
                Step("step-stale", BuyerAcceptanceDemoStepKind.ReadDetail, BuyerAcceptanceDemoFixtureKind.Stale, projections[2].Summary.ConversationId),
                Step("step-missing-citation", BuyerAcceptanceDemoStepKind.CitationCopy, BuyerAcceptanceDemoFixtureKind.MissingCitation, projections[3].Summary.ConversationId),
                Step("step-unresolved-participant", BuyerAcceptanceDemoStepKind.ReadDetail, BuyerAcceptanceDemoFixtureKind.UnresolvedParticipant, projections[4].Summary.ConversationId),
                Step("step-command-metadata", BuyerAcceptanceDemoStepKind.CommandMetadata, BuyerAcceptanceDemoFixtureKind.BlockedCommand, projections[5].Summary.ConversationId),
                Step("step-verification-pass", BuyerAcceptanceDemoStepKind.Verification, BuyerAcceptanceDemoFixtureKind.VerificationPass, projections[0].Summary.ConversationId),
                Step("step-verification-failure", BuyerAcceptanceDemoStepKind.Verification, BuyerAcceptanceDemoFixtureKind.VerificationFailure, projections[0].Summary.ConversationId),
                Step("step-cross-scope-denial", BuyerAcceptanceDemoStepKind.CrossTenantDenial, BuyerAcceptanceDemoFixtureKind.CrossTenantPoison, projections[0].Summary.ConversationId),
                Step("step-summary", BuyerAcceptanceDemoStepKind.EvidenceSummary, BuyerAcceptanceDemoFixtureKind.FullTrust, projections[0].Summary.ConversationId),
            ],
            ["AC1", "AC2", "AC3", "AC4", "AC5"]);

    private static BuyerAcceptanceDemoFixtureV1 Fixture(
        string fixtureId,
        BuyerAcceptanceDemoFixtureKind kind,
        BuyerAcceptanceDemoTrustState trustState,
        ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            AuthorizedTenant,
            fixtureId,
            kind,
            trustState,
            SyntheticDataMarker,
            "Synthetic governed conversation",
            "Continue with governed evidence.",
            conversationId,
            ["AC1", "AC2", "AC5"]);

    private static BuyerAcceptanceDemoStepV1 Step(
        string stepId,
        BuyerAcceptanceDemoStepKind stepKind,
        BuyerAcceptanceDemoFixtureKind fixtureKind,
        ConversationId conversationId)
        => new(
            SchemaVersion.Current,
            stepId,
            stepKind,
            fixtureKind,
            TrustStateFor(fixtureKind),
            SafeLabelFor(stepKind, fixtureKind),
            "Continue with governed evidence.",
            ["AC1", "AC2", "AC3", "AC4", "AC5"],
            conversationId,
            Business,
            "message:message-001",
            AuditHandle,
            TemporalCursor: stepKind == BuyerAcceptanceDemoStepKind.TemporalReconstruction
                ? "temporal:v1:pos:0000000003:projection:0000000100"
                : null,
            EvidenceHandles: [new ConversationGovernanceVerificationEvidenceHandle("verification-proof-demo")]);

    private static string SafeLabelFor(BuyerAcceptanceDemoStepKind stepKind, BuyerAcceptanceDemoFixtureKind fixtureKind)
    {
        if (stepKind == BuyerAcceptanceDemoStepKind.CrossTenantDenial)
        {
            return "Check cross scope denial";
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.Stale)
        {
            return "Check stale evidence handling";
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.MissingCitation)
        {
            return "Check incomplete citation";
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.BlockedCommand)
        {
            return "Check blocked command metadata";
        }

        return "Review governed evidence";
    }

    private static BuyerAcceptanceDemoTrustState TrustStateFor(BuyerAcceptanceDemoFixtureKind fixtureKind)
    {
        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.Redacted)
        {
            return BuyerAcceptanceDemoTrustState.Redacted;
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.Stale)
        {
            return BuyerAcceptanceDemoTrustState.Stale;
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.MissingCitation)
        {
            return BuyerAcceptanceDemoTrustState.Incomplete;
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.UnresolvedParticipant)
        {
            return BuyerAcceptanceDemoTrustState.Unavailable;
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.CrossTenantPoison)
        {
            return BuyerAcceptanceDemoTrustState.Hidden;
        }

        if (fixtureKind == BuyerAcceptanceDemoFixtureKind.VerificationFailure)
        {
            return BuyerAcceptanceDemoTrustState.Failed;
        }

        return BuyerAcceptanceDemoTrustState.Current;
    }

    private static BuyerAcceptanceDemoProjectionPair Projection(
        string suffix,
        BuyerAcceptanceDemoFixtureKind fixtureKind,
        BuyerAcceptanceDemoTrustState trustState,
        ProjectionTrustState freshnessState,
        ConversationCitationAvailability citationAvailability,
        ConversationAuditReadinessState auditReadiness,
        ConversationVerificationState verificationState,
        bool redacted = false,
        bool includeAuditEvidence = true,
        ProjectionTrustState? participantState = null,
        bool includeBlockedCommand = false)
    {
        ConversationId conversationId = new($"conversation-demo-{suffix}");
        MessageId messageId = new("message-001");
        BusinessReference businessReference = string.Equals(suffix, "full", StringComparison.Ordinal)
            ? Business
            : new BusinessReference("buyer-demo", $"case-{suffix}");
        ProjectionFreshnessV1 freshness = Freshness(suffix, freshnessState);
        ConversationEvidenceTrustPostureV1 posture = new(
            SchemaVersion.Current,
            AuthorizedTenant,
            conversationId,
            freshness.ProjectionCursor,
            freshness,
            redacted ? ProjectionTrustState.Redacted : freshnessState,
            participantState ?? ProjectionTrustState.Current,
            citationAvailability,
            auditReadiness,
            verificationState,
            includeBlockedCommand ? [BlockedCommand(freshness.ProjectionGeneratedAt)] : null);
        GovernanceTarget target = new(GovernedTargetKind.Message, MessageId: messageId);
        ConversationRedactionAttributionV1? attribution = redacted
            ? new ConversationRedactionAttributionV1(
                RedactionCategory.ContentSuppression,
                "buyer-acceptance-policy",
                "buyer-demo",
                Actor,
                GeneratedAtUtc,
                target,
                "message:message-001",
                AuditEvidence,
                ConversationAuditReadinessState.Ready,
                ProjectionTrustState.Redacted,
                "[redacted]",
                "Redacted demo evidence",
                "Copy redacted demo evidence",
                "Open governed audit detail when authorized.")
            : null;
        GovernanceAuditEvidenceReference? audit = includeAuditEvidence ? AuditEvidence : null;
        ConversationEvidenceEntryV1 evidence = new(
            "message:message-001",
            "Message",
            Actor,
            GeneratedAtUtc,
            redacted ? ProjectionTrustState.Redacted : freshnessState,
            citationAvailability,
            auditReadiness,
            redacted ? ProjectionTrustState.Redacted : freshnessState,
            MessageId: messageId,
            VisibleText: redacted ? "[redacted]" : "Synthetic governed message.",
            AuditEvidence: audit,
            SafeSummaryLabel: redacted ? "Redacted demo evidence" : "Demo message evidence",
            SafeAccessibilityLabel: redacted ? "Copy redacted demo evidence" : "Copy demo message evidence",
            SafeNextAction: "Open governed audit detail when authorized.",
            RedactionAttribution: attribution,
            SafeSourcePosition: 3);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            AuthorizedTenant,
            conversationId,
            freshness,
            "Open",
            $"Demo case {suffix}",
            businessReference,
            Project,
            Folder,
            [Actor, Participant],
            1,
            0,
            SearchTrustPreview: SearchTrust(freshness, citationAvailability, auditReadiness, verificationState));
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            AuthorizedTenant,
            conversationId,
            freshness,
            "Open",
            $"Demo case {suffix}",
            businessReference,
            Project,
            Folder,
            Participants: [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            Messages: [new ConversationTimelineMessageProjectionV1(messageId, Actor, redacted ? "[redacted]" : "Synthetic governed message.", GeneratedAtUtc)],
            TrustPosture: posture,
            EvidenceEntries: [evidence],
            Redactions: attribution is null
                ? []
                :
                [
                    new ConversationRedactionProjectionV1(
                        target,
                        RedactionCategory.ContentSuppression,
                        "buyer-acceptance-policy",
                        "buyer-demo",
                        Actor,
                        GeneratedAtUtc,
                        AuditEvidence,
                        ProjectionTrustState.Redacted,
                        "[redacted]"),
                ]);

        return new BuyerAcceptanceDemoProjectionPair($"fixture-{suffix}", fixtureKind, trustState, summary, detail);
    }

    private static ConversationCommandAvailabilityV1 BlockedCommand(DateTimeOffset evaluatedAt)
        => new(
            "set-retention-policy",
            ProjectionTrustState.Unavailable,
            "conversations.governance",
            ProjectionTrustState.Current,
            "governance",
            ProjectionTrustState.Current,
            ConversationAuditReadinessState.Ready,
            "Action requires current evidence and audit readiness.",
            evaluatedAt,
            ConversationCommandAvailabilityV1.GovernanceChangingActionClassification);

    private static ConversationSearchTrustPreviewV1 SearchTrust(
        ProjectionFreshnessV1 freshness,
        ConversationCitationAvailability citationAvailability,
        ConversationAuditReadinessState auditReadiness,
        ConversationVerificationState verificationState)
        => new(
            freshness.FreshnessState,
            freshness.ReasonCode,
            freshness.FreshnessState,
            ProjectionTrustState.Current,
            citationAvailability,
            auditReadiness,
            verificationState,
            ConversationSearchMatchSource.BusinessReference,
            "Visible through authorized scope and matched business reference.");

    private static ProjectionFreshnessV1 Freshness(string suffix, ProjectionTrustState state)
        => new(
            SchemaVersion.Current,
            $"pos:demo-{suffix}",
            100,
            GeneratedAtUtc.AddSeconds(-1),
            GeneratedAtUtc,
            TimeSpan.FromSeconds(1),
            IsStale: state == ProjectionTrustState.Stale,
            state,
            state == ProjectionTrustState.Current
                ? ProjectionFreshnessReasonCode.Current
                : ProjectionFreshnessReasonCode.StaleThresholdExceeded);

    private static ConversationGovernanceVerificationRunResultV1 Verification(
        ConversationGovernanceVerificationExecutionStatus status,
        ConversationGovernanceVerificationFailureClassification classification,
        string summary,
        string detail)
        => new(
            SchemaVersion.Current,
            new ConversationGovernanceVerificationScopeV1(
                SchemaVersion.Current,
                ConversationGovernanceVerificationScopeKind.Conversation,
                AuthorizedTenant,
                new ConversationId("conversation-demo-full")),
            [ConversationGovernanceVerificationSuite.TenantIsolation],
            GeneratedAtUtc,
            "correlation-buyer-demo",
            status,
            classification,
            summary,
            [
                new ConversationGovernanceVerificationCheckResultV1(
                    SchemaVersion.Current,
                    ConversationGovernanceVerificationSuite.TenantIsolation,
                    "tenant-isolation",
                    ["AC1", "AC5"],
                    status,
                    classification,
                    detail,
                    classification == ConversationGovernanceVerificationFailureClassification.Passed
                        ? ConversationGovernanceVerificationRemediation.None
                        : ConversationGovernanceVerificationRemediation.InspectGovernanceEvidence,
                    new ConversationGovernanceVerificationEvidenceHandle("verification-proof-demo")),
            ]);

    private static BuyerAcceptanceDemoProjectionPair PoisonProjection()
    {
        ConversationId conversationId = new("conversation-poison");
        ProjectionFreshnessV1 freshness = new(
            SchemaVersion.Current,
            "pos:poison",
            666,
            GeneratedAtUtc.AddSeconds(-1),
            GeneratedAtUtc,
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            PoisonTenant,
            conversationId,
            freshness,
            "Open",
            "POISON-SENTINEL-alpha",
            Business,
            ParticipantPartyIds: [new PartyId("party-poison")],
            MessageCount: 1);
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            PoisonTenant,
            conversationId,
            freshness,
            "Open",
            "POISON-SENTINEL-beta",
            Business,
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(
                    new MessageId("message-poison"),
                    new PartyId("party-poison"),
                    "POISON-SENTINEL-alpha",
                    GeneratedAtUtc),
            ]);

        return new BuyerAcceptanceDemoProjectionPair(
            "fixture-poison",
            BuyerAcceptanceDemoFixtureKind.CrossTenantPoison,
            BuyerAcceptanceDemoTrustState.Hidden,
            summary,
            detail);
    }
}

/// <summary>
/// Carries deterministic synthetic acceptance data for tests or demo-only host composition.
/// </summary>
public sealed record BuyerAcceptanceDemoSeedData(
    BuyerAcceptanceDemoScenarioV1 Scenario,
    DateTimeOffset GeneratedAtUtc,
    TenantId AuthorizedTenantId,
    TenantId PoisonTenantId,
    IReadOnlyList<BuyerAcceptanceDemoProjectionPair> AuthorizedProjections,
    BuyerAcceptanceDemoProjectionPair PoisonProjection,
    ConversationGovernanceVerificationRunResultV1 VerificationPass,
    ConversationGovernanceVerificationRunResultV1 VerificationFailure,
    IReadOnlyList<string> PoisonSentinelValues);

/// <summary>
/// Carries one synthetic summary/detail projection pair without depending on server storage types.
/// </summary>
public sealed record BuyerAcceptanceDemoProjectionPair(
    string FixtureId,
    BuyerAcceptanceDemoFixtureKind FixtureKind,
    BuyerAcceptanceDemoTrustState TrustState,
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailProjectionV1 Detail);
