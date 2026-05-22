// <copyright file="PrivilegedOperationalJustificationContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies privileged operational justification contracts stay closed, structured, and content-safe.
/// </summary>
public sealed class PrivilegedOperationalJustificationContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PrivilegedOperationalActionClassShouldBeClosedAndComplete()
    {
        PrivilegedOperationalActionClass[] values =
        [
            PrivilegedOperationalActionClass.Read,
            PrivilegedOperationalActionClass.Rebuild,
            PrivilegedOperationalActionClass.Repair,
            PrivilegedOperationalActionClass.Export,
            PrivilegedOperationalActionClass.Verify,
            PrivilegedOperationalActionClass.VisibilityChange,
            PrivilegedOperationalActionClass.MetadataChange,
            PrivilegedOperationalActionClass.TenantDataTouch,
        ];

        values.Select(value => value.Value).ShouldBeUnique();
        PrivilegedOperationalActionClass.Parse("Read").ShouldBe(PrivilegedOperationalActionClass.Read);
        Should.Throw<ArgumentException>(() => PrivilegedOperationalActionClass.Parse("RawTranscriptDump"));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PrivilegedOperationalActionClass>("\"ProviderPayloadRead\"", WebOptions));
    }

    [Fact]
    public void PrivilegedJustificationShouldRequireStructuredAuthorityFields()
    {
        Should.Throw<ArgumentNullException>(() => new PrivilegedOperationalJustificationV1(
            null!,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            ContractSamples.Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentNullException>(() => new PrivilegedOperationalJustificationV1(
            SchemaVersion.Current,
            null!,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            ContractSamples.Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentException>(() => new PrivilegedOperationalJustificationV1(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            ContractSamples.Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "raw message content",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));
    }

    [Fact]
    public void PrivilegedJustificationCommandAndDetailsShouldSerializeStableSafeJsonShape()
    {
        RecordPrivilegedOperationalJustificationCommand command = new(Justification());
        PrivilegedOperationalJustificationDetailsV1 details = new(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            ContractSamples.Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            ContractSamples.GovernanceTimestamp,
            GovernanceOutcome.Succeeded,
            ContractSamples.AuditEvidence,
            ProjectionTrustState.Current,
            ContractSamples.FreshnessV1,
            "Use the returned audit handle as governed evidence.",
            "correlation-001",
            "causation-001");

        JsonNode? commandJson = JsonNode.Parse(JsonSerializer.Serialize(command, WebOptions));
        JsonNode? detailJson = JsonNode.Parse(JsonSerializer.Serialize(details, WebOptions));

        commandJson!["justification"]!["operationClass"]!.GetValue<string>().ShouldBe("Read");
        commandJson["justification"]!["privilegedActionClass"]!.GetValue<string>().ShouldBe("ComplianceReview");
        detailJson!["outcome"]!.GetValue<string>().ShouldBe("Succeeded");
        detailJson["auditEvidence"]!["handle"]!["value"]!.GetValue<string>().ShouldBe("audit-evidence-001");
        detailJson["visibilityState"]!.GetValue<string>().ShouldBe("Current");
        commandJson.ToJsonString().ShouldNotContain("EventStore", Case.Insensitive);
        commandJson.ToJsonString().ShouldNotContain("storage", Case.Insensitive);
        detailJson.ToJsonString().ShouldNotContain("providerPayload", Case.Insensitive);
    }

    [Fact]
    public void PrivilegedJustificationResultShouldKeepRedactedUnavailableAndMissingDistinct()
    {
        PrivilegedOperationalJustificationResult unavailable = PrivilegedOperationalJustificationResult.Unavailable(
            SchemaVersion.Current,
            ProjectionFreshnessReasonCode.Unavailable,
            "Retry after privileged-action evidence is available.");
        PrivilegedOperationalJustificationResult hidden = PrivilegedOperationalJustificationResult.Hidden(SchemaVersion.Current);

        unavailable.VisibilityState.ShouldBe(ProjectionTrustState.Unavailable);
        unavailable.Details.ShouldBeNull();
        hidden.VisibilityState.ShouldBe(ProjectionTrustState.Forbidden);
        hidden.Details.ShouldBeNull();
        unavailable.SafeNextAction.ShouldNotBe(hidden.SafeNextAction);
    }

    [Fact]
    public void PrivilegedJustificationContractsShouldKeepToStringContentSafe()
    {
        string text = Justification().ToString();

        text.ShouldNotContain("customer-request", Case.Insensitive);
        text.ShouldNotContain("privileged-review-policy", Case.Insensitive);
        text.ShouldNotContain("caller", Case.Insensitive);
        text.ShouldNotContain("storage", Case.Insensitive);
    }

    [Fact]
    public void PrivilegedPublicContractsShouldNotExposeForbiddenSubstrateFields()
    {
        Type[] contractTypes =
        [
            typeof(PrivilegedOperationalJustificationV1),
            typeof(RecordPrivilegedOperationalJustificationCommand),
            typeof(PrivilegedOperationalJustificationDetailsV1),
            typeof(PrivilegedOperationalJustificationResult),
            typeof(GetPrivilegedOperationalJustificationQuery),
        ];
        string[] forbidden =
        [
            "Sink",
            "Storage",
            "Stream",
            "Position",
            "Exception",
            "ProviderPayload",
            "MessageText",
            "RedactedText",
            "PartyPersonalData",
            "Raw",
            "Upstream",
            "Claim",
            "Token",
        ];

        foreach (Type type in contractTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                foreach (string term in forbidden)
                {
                    property.Name.ShouldNotContain(term, Case.Insensitive);
                }
            }
        }
    }

    private static PrivilegedOperationalJustificationV1 Justification()
        => new(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.Conversation),
            ContractSamples.Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            ContractSamples.GovernanceTimestamp,
            "correlation-001",
            "causation-001",
            ContractSamples.AuditEvidence);
}
