// <copyright file="ConversationCommandAvailabilityContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies governed command availability metadata remains explicit and fail-closed.
/// </summary>
public sealed class ConversationCommandAvailabilityContractTest
{
    /// <summary>
    /// Governed mutation metadata carries a separate action classification and fresh-server-recheck requirement.
    /// </summary>
    [Fact]
    public void GovernedMutationCommandShouldExposeClassificationAndFreshRecheckRequirement()
    {
        ConversationCommandAvailabilityV1 command = SafeCommand();

        command.ActionClassification.ShouldBe(ConversationCommandAvailabilityV1.GovernanceChangingActionClassification);
        command.RequiresFreshServerRecheck.ShouldBeTrue();
        command.AvailabilityState.ShouldBe(ProjectionTrustState.Unavailable);
        command.RequiredPermission.ShouldBe("conversations.governance");
        command.PreconditionState.ShouldBe(ProjectionTrustState.Current);
        command.RiskLevel.ShouldBe("governance");
        command.FreshnessRequirementState.ShouldBe(ProjectionTrustState.Current);
        command.AuditRequirement.ShouldBe(ConversationAuditReadinessState.Ready);
    }

    /// <summary>
    /// Missing command metadata produces a non-empty unavailable default instead of accidental no-authority.
    /// </summary>
    [Fact]
    public void MissingCommandMetadataShouldDefaultToUnavailableReadOnlyClassification()
    {
        ConversationEvidenceTrustPostureV1 posture = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.FreshnessV1.ProjectionCursor,
            ContractSamples.FreshnessV1,
            ProjectionTrustState.Current,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified);

        ConversationCommandAvailabilityV1 command = posture.CommandEligibility.Single();

        command.ActionName.ShouldBe("read-governed-record");
        command.ActionClassification.ShouldBe(ConversationCommandAvailabilityV1.ReadOnlyActionClassification);
        command.RequiresFreshServerRecheck.ShouldBeTrue();
        command.AvailabilityState.ShouldBe(ProjectionTrustState.Unavailable);
        command.BlockedReason.ShouldContain("unavailable");
    }

    /// <summary>
    /// Available governance-changing metadata is impossible without current preconditions, audit readiness, and recheck.
    /// </summary>
    [Theory]
    [InlineData("precondition")]
    [InlineData("freshness")]
    [InlineData("audit")]
    [InlineData("recheck")]
    public void AvailableGovernanceCommandShouldRequireAllServerSideGates(string failingGate)
    {
        Should.Throw<ArgumentException>(() => SafeCommand(
            availability: ProjectionTrustState.Current,
            precondition: failingGate == "precondition" ? ProjectionTrustState.Stale : ProjectionTrustState.Current,
            freshness: failingGate == "freshness" ? ProjectionTrustState.Stale : ProjectionTrustState.Current,
            auditReadiness: failingGate == "audit" ? ConversationAuditReadinessState.Unavailable : ConversationAuditReadinessState.Ready,
            requiresFreshServerRecheck: failingGate != "recheck"));
    }

    /// <summary>
    /// Unavailable command metadata is still a command handoff contract and must not opt out of server recheck.
    /// </summary>
    [Fact]
    public void UnavailableGovernanceCommandShouldStillRequireFreshServerRecheck()
    {
        Should.Throw<ArgumentException>(() => SafeCommand(requiresFreshServerRecheck: false));
    }

    /// <summary>
    /// Available governance-changing metadata remains advisory and must carry every server-side recheck gate.
    /// </summary>
    [Fact]
    public void AvailableGovernanceCommandShouldRemainAdvisoryAndRequireServerRecheck()
    {
        ConversationCommandAvailabilityV1 command = SafeCommand(availability: ProjectionTrustState.Current);

        command.AvailabilityState.ShouldBe(ProjectionTrustState.Current);
        command.ActionClassification.ShouldBe(ConversationCommandAvailabilityV1.GovernanceChangingActionClassification);
        command.RequiresFreshServerRecheck.ShouldBeTrue();
        command.PreconditionState.ShouldBe(ProjectionTrustState.Current);
        command.FreshnessRequirementState.ShouldBe(ProjectionTrustState.Current);
        command.AuditRequirement.ShouldBe(ConversationAuditReadinessState.Ready);
        command.RequiredPermission.ShouldBe("conversations.governance");
    }

    /// <summary>
    /// Command metadata rejects unsafe vocabulary from infrastructure, routes, clients, provider payloads, and raw failures.
    /// </summary>
    [Theory]
    [InlineData("actionName", "EventStore-stream")]
    [InlineData("actionName", "set-retention-policy?tenantId=tenant-evil")]
    [InlineData("requiredPermission", "conversations.providerPayload")]
    [InlineData("riskLevel", "route-secret")]
    [InlineData("blockedReason", "Raw exception from EventStore stream.")]
    [InlineData("blockedReason", "Use browser-selected value from local storage.")]
    [InlineData("actionClassification", "client-side-optional")]
    [InlineData("blockedReason", "Raw-exception from Event Store details.")]
    [InlineData("blockedReason", "Use browser selected provider-payload.")]
    [InlineData("blockedReason", "Party-personal data was present.")]
    [InlineData("blockedReason", "Hidden field supplied client state.")]
    public void CommandMetadataShouldRejectUnsafeVocabulary(string field, string unsafeValue)
    {
        Should.Throw<ArgumentException>(() => SafeCommand(
            actionName: field == "actionName" ? unsafeValue : "set-retention-policy",
            requiredPermission: field == "requiredPermission" ? unsafeValue : "conversations.governance",
            riskLevel: field == "riskLevel" ? unsafeValue : "governance",
            blockedReason: field == "blockedReason" ? unsafeValue : "Command execution requires a fresh server recheck.",
            actionClassification: field == "actionClassification"
                ? unsafeValue
                : ConversationCommandAvailabilityV1.GovernanceChangingActionClassification));
    }

    /// <summary>
    /// Serialized metadata remains additive and content-safe for future UI renderers.
    /// </summary>
    [Fact]
    public void CommandMetadataShouldSerializeStableSafeFields()
    {
        string json = JsonSerializer.Serialize(SafeCommand(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        JsonNode parsed = JsonNode.Parse(json)!;

        parsed["actionName"]!.GetValue<string>().ShouldBe("set-retention-policy");
        parsed["actionClassification"]!.GetValue<string>().ShouldBe("governance-changing");
        parsed["requiresFreshServerRecheck"]!.GetValue<bool>().ShouldBeTrue();
        json.IndexOf("\"actionClassification\"", StringComparison.Ordinal).ShouldBeLessThan(
            json.IndexOf("\"requiresFreshServerRecheck\"", StringComparison.Ordinal));
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("stream", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("tenant-evil", Case.Insensitive);
    }

    private static ConversationCommandAvailabilityV1 SafeCommand(
        string actionName = "set-retention-policy",
        ProjectionTrustState? availability = null,
        string requiredPermission = "conversations.governance",
        ProjectionTrustState? precondition = null,
        string riskLevel = "governance",
        ProjectionTrustState? freshness = null,
        ConversationAuditReadinessState? auditReadiness = null,
        string blockedReason = "Command execution requires a fresh server recheck.",
        string actionClassification = ConversationCommandAvailabilityV1.GovernanceChangingActionClassification,
        bool requiresFreshServerRecheck = true)
        => new(
            actionName,
            availability ?? ProjectionTrustState.Unavailable,
            requiredPermission,
            precondition ?? ProjectionTrustState.Current,
            riskLevel,
            freshness ?? ProjectionTrustState.Current,
            auditReadiness ?? ConversationAuditReadinessState.Ready,
            blockedReason,
            ContractSamples.EventMetadata.CommittedAt,
            actionClassification,
            requiresFreshServerRecheck);
}
