// <copyright file="ConversationEvidenceContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

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
}
