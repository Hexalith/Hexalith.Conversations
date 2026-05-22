// <copyright file="ConversationEvidenceContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies governed conversation evidence read contracts.
/// </summary>
public sealed class ConversationEvidenceContractTest
{
    /// <summary>
    /// Detail responses expose trust posture and evidence records before timeline-shaped message data.
    /// </summary>
    [Fact]
    public void DetailShouldSerializeTrustPostureAndEvidenceBeforeMessages()
    {
        ConversationDetailsV1 details = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.FreshnessV1,
            "Open",
            "Case 123",
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(
                    ContractSamples.Message,
                    ContractSamples.Actor,
                    "Visible governed text.",
                    ContractSamples.EventMetadata.CommittedAt),
            ],
            TrustPosture: TrustPosture(),
            EvidenceEntries:
            [
                new ConversationEvidenceEntryV1(
                    "message:message-001",
                    "Message",
                    ContractSamples.Actor,
                    ContractSamples.EventMetadata.CommittedAt,
                    ProjectionTrustState.Current,
                    ConversationCitationAvailability.Available,
                    ConversationAuditReadinessState.Ready,
                    ProjectionTrustState.Current,
                    MessageId: ContractSamples.Message,
                    VisibleText: "Visible governed text."),
            ]);

        string json = JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.IndexOf("\"trustPosture\"", StringComparison.Ordinal).ShouldBeLessThan(
            json.IndexOf("\"messages\"", StringComparison.Ordinal));
        json.IndexOf("\"evidenceEntries\"", StringComparison.Ordinal).ShouldBeLessThan(
            json.IndexOf("\"messages\"", StringComparison.Ordinal));
        json.ShouldContain("\"evidenceCompletenessState\":\"Current\"");
        json.ShouldContain("\"commandEligibility\"");
        json.ShouldContain("\"availabilityState\":\"Unavailable\"");
        json.ShouldContain("\"kind\":\"Message\"");
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("stream", Case.Insensitive);
        json.ShouldNotContain("providerSessionReference", Case.Insensitive);
        json.ShouldNotContain("transcript", Case.Insensitive);
    }

    /// <summary>
    /// Older or partial detail projections default to explicit unavailable trust metadata.
    /// </summary>
    [Fact]
    public void DetailWithoutExplicitTrustPostureShouldDefaultToUnavailableMetadata()
    {
        ConversationDetailsV1 details = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.FreshnessV1,
            "Open");

        details.TrustPosture.EvidenceCompletenessState.ShouldBe(ProjectionTrustState.Unavailable);
        details.TrustPosture.ParticipantResolutionState.ShouldBe(ProjectionTrustState.Unavailable);
        details.TrustPosture.CitationAvailability.ShouldBe(ConversationCitationAvailability.Unavailable);
        details.TrustPosture.AuditReadiness.ShouldBe(ConversationAuditReadinessState.Unknown);
        details.TrustPosture.VerificationState.ShouldBe(ConversationVerificationState.Unknown);
        details.TrustPosture.CommandEligibility.ShouldNotBeEmpty();
        details.TrustPosture.CommandEligibility.ShouldAllBe(item => item.AvailabilityState == ProjectionTrustState.Unavailable);
        details.EvidenceEntries.ShouldBeEmpty();
    }

    /// <summary>
    /// Redaction attribution exposes only safe governed metadata and no original content fields.
    /// </summary>
    [Fact]
    public void RedactionAttributionShouldSerializeSafeInlineMetadataOnly()
    {
        ConversationRedactionAttributionV1 attribution = new(
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ContractSamples.RedactionMessageTarget,
            ContractSamples.RedactionMessageTarget.ToTargetKey(),
            ContractSamples.AuditEvidence,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            "[redacted]",
            "Redaction attribution",
            "Redacted evidence with governed attribution",
            "Open governed audit detail when authorized.");
        ConversationEvidenceEntryV1 entry = new(
            "message:message-001",
            "Message",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ProjectionTrustState.Redacted,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            MessageId: ContractSamples.Message,
            VisibleText: "[redacted]",
            PolicyReference: "redaction-policy-standard",
            GovernedTarget: ContractSamples.RedactionMessageTarget,
            RationaleClass: "customer-request",
            AuditEvidence: ContractSamples.AuditEvidence,
            SafeSummaryLabel: "Redacted message evidence",
            SafeDetailLabel: "Redaction audit detail",
            SafeAccessibilityLabel: "Redacted message evidence with governed attribution",
            SafeNextAction: "Open governed audit detail when authorized.",
            RedactionAttribution: attribution);

        string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        JsonNode parsed = JsonNode.Parse(json)!;

        parsed["redactionAttribution"]!["category"]!.GetValue<string>().ShouldBe("ContentSuppression");
        parsed["redactionAttribution"]!["policyReference"]!.GetValue<string>().ShouldBe("redaction-policy-standard");
        parsed["redactionAttribution"]!["reasonClass"]!.GetValue<string>().ShouldBe("customer-request");
        parsed["redactionAttribution"]!["actorPartyId"]!.GetValue<string>().ShouldBe("party:party-actor");
        parsed["redactionAttribution"]!["targetKey"]!.GetValue<string>().ShouldBe("message:message-001");
        parsed["redactionAttribution"]!["auditReadiness"]!.GetValue<string>().ShouldBe("Ready");
        parsed["visibleText"]!.GetValue<string>().ShouldBe("[redacted]");
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("original", Case.Insensitive);
        json.ShouldNotContain("redactedLength", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("storage", Case.Insensitive);
        json.ShouldNotContain("upstream", Case.Insensitive);
    }

    /// <summary>
    /// Missing redaction audit metadata remains explicit and non-ready.
    /// </summary>
    [Fact]
    public void RedactionAttributionWithoutAuditEvidenceShouldStayIncomplete()
    {
        ConversationRedactionAttributionV1 attribution = new(
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ContractSamples.RedactionMessageTarget,
            ContractSamples.RedactionMessageTarget.ToTargetKey(),
            AuditEvidence: null,
            ConversationAuditReadinessState.Incomplete,
            ProjectionTrustState.Redacted,
            "[redacted]",
            "Redaction attribution",
            "Redacted evidence with governed attribution",
            "Show incomplete audit detail state.");

        attribution.AuditEvidence.ShouldBeNull();
        attribution.AuditReadiness.ShouldBe(ConversationAuditReadinessState.Incomplete);
        attribution.AttributionState.ShouldBe(ProjectionTrustState.Redacted);
        attribution.SafeNextAction.ShouldBe("Show incomplete audit detail state.");
    }

    /// <summary>
    /// Redaction attribution target keys cannot drift from the governed target they describe.
    /// </summary>
    [Fact]
    public void RedactionAttributionShouldRejectMismatchedTargetKey()
    {
        Should.Throw<ArgumentException>(() => new ConversationRedactionAttributionV1(
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ContractSamples.RedactionMessageTarget,
            "message:other-message",
            ContractSamples.AuditEvidence,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            "[redacted]",
            "Redaction attribution",
            "Redacted evidence with governed attribution",
            "Open governed audit detail when authorized."));
    }

    /// <summary>
    /// Public redaction placeholders are canonical markers, not caller-supplied message text.
    /// </summary>
    [Fact]
    public void RedactionContractsShouldRejectNonCanonicalPlaceholderText()
    {
        Should.Throw<ArgumentException>(() => new ConversationRedactionAttributionV1(
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ContractSamples.RedactionMessageTarget,
            ContractSamples.RedactionMessageTarget.ToTargetKey(),
            ContractSamples.AuditEvidence,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            "Hello from the adopter.",
            "Redaction attribution",
            "Redacted evidence with governed attribution",
            "Open governed audit detail when authorized."));

        Should.Throw<ArgumentException>(() => new ConversationRedactionProjectionV1(
            ContractSamples.RedactionMessageTarget,
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ContractSamples.AuditEvidence,
            ProjectionTrustState.Redacted,
            "Hello from the adopter."));
    }

    /// <summary>
    /// Redacted evidence entries cannot carry mismatched visible text beside safe attribution metadata.
    /// </summary>
    [Fact]
    public void RedactedEvidenceEntryShouldRejectOriginalVisibleText()
    {
        ConversationRedactionAttributionV1 attribution = SafeAttribution();

        Should.Throw<ArgumentException>(() => new ConversationEvidenceEntryV1(
            "message:message-001",
            "Message",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ProjectionTrustState.Redacted,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            MessageId: ContractSamples.Message,
            VisibleText: "Hello from the adopter.",
            RedactionAttribution: attribution));
    }

    /// <summary>
    /// Missing redaction audit metadata must not be masked by a ready evidence-entry state.
    /// </summary>
    [Fact]
    public void RedactedEvidenceEntryShouldRequireMatchingAttributionReadiness()
    {
        ConversationRedactionAttributionV1 incompleteAttribution = SafeAttribution(
            includeAuditEvidence: false,
            readiness: ConversationAuditReadinessState.Incomplete,
            nextAction: "Show incomplete audit detail state.");

        Should.Throw<ArgumentException>(() => new ConversationEvidenceEntryV1(
            "message:message-001",
            "Message",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ProjectionTrustState.Redacted,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            MessageId: ContractSamples.Message,
            VisibleText: "[redacted]",
            RedactionAttribution: incompleteAttribution));
    }

    private static ConversationEvidenceTrustPostureV1 TrustPosture()
        => new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.FreshnessV1.ProjectionCursor,
            ContractSamples.FreshnessV1,
            ProjectionTrustState.Current,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified,
            [
                new ConversationCommandAvailabilityV1(
                    "read-governed-record",
                    ProjectionTrustState.Unavailable,
                    "conversations.read",
                    ProjectionTrustState.Current,
                    "read",
                    ProjectionTrustState.Current,
                    ConversationAuditReadinessState.Ready,
                    "Command execution is outside this read surface.",
                    ContractSamples.EventMetadata.CommittedAt),
            ]);

    private static ConversationRedactionAttributionV1 SafeAttribution(
        bool includeAuditEvidence = true,
        ConversationAuditReadinessState? readiness = null,
        string nextAction = "Open governed audit detail when authorized.")
        => new(
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            ContractSamples.RedactionMessageTarget,
            ContractSamples.RedactionMessageTarget.ToTargetKey(),
            includeAuditEvidence ? ContractSamples.AuditEvidence : null,
            readiness ?? ConversationAuditReadinessState.Ready,
            ProjectionTrustState.Redacted,
            "[redacted]",
            "Redaction attribution",
            "Redacted evidence with governed attribution",
            nextAction);
}
