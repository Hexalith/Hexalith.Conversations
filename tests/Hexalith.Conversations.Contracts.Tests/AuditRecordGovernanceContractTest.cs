// <copyright file="AuditRecordGovernanceContractTest.cs" company="ITANEO">
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
/// Verifies governed audit-record public contracts.
/// </summary>
public sealed class AuditRecordGovernanceContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AuditRecordActionClassificationShouldBeClosedAndComplete()
    {
        AuditRecordActionClassification[] values =
        [
            AuditRecordActionClassification.Allowed,
            AuditRecordActionClassification.Denied,
            AuditRecordActionClassification.Redacted,
            AuditRecordActionClassification.Exported,
            AuditRecordActionClassification.SeparatelyLogged,
            AuditRecordActionClassification.PolicyBlocked,
        ];

        values.Select(value => value.Value).ShouldBeUnique();
        AuditRecordActionClassification.Parse("Allowed").ShouldBe(AuditRecordActionClassification.Allowed);
        Should.Throw<ArgumentException>(() => AuditRecordActionClassification.Parse("RawAuditSink"));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AuditRecordActionClassification>("\"StorageCommitted\"", WebOptions));
    }

    [Fact]
    public void AuditRecordTargetShouldUseAuditEvidenceHandleKey()
    {
        GovernanceTarget target = new(
            GovernedTargetKind.AuditRecord,
            AuditEvidenceHandle: new AuditEvidenceHandle("audit-evidence-001"));

        target.ToTargetKey().ShouldBe("audit:audit-evidence-001");
        target.SegmentReference.ShouldBeNull();
        target.ToTargetKey().ShouldNotStartWith("unsupported:", Case.Sensitive);
    }

    [Fact]
    public void AuditRecordTargetShouldRequireAuditEvidenceHandle()
    {
        GovernanceTarget target = new(GovernedTargetKind.AuditRecord);

        Should.Throw<InvalidOperationException>(target.ToTargetKey);
    }

    [Fact]
    public void AuditRecordQueryToStringShouldNotEchoCallerOrRawHandle()
    {
        GetConversationAuditRecordQuery query = new(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            "caller-secret-001",
            "correlation-001",
            ContractSamples.Conversation,
            "storage://raw-audit-location",
            AuditRecordActionClassification.Allowed);

        string text = query.ToString();

        text.ShouldContain(nameof(GetConversationAuditRecordQuery), Case.Sensitive);
        text.ShouldNotContain("caller-secret-001", Case.Sensitive);
        text.ShouldNotContain("storage", Case.Insensitive);
        text.ShouldNotContain("raw-audit-location", Case.Insensitive);
    }

    [Fact]
    public void AuditRecordContractsShouldSerializeStableSafeJsonShape()
    {
        GovernanceTarget target = new(
            GovernedTargetKind.AuditRecord,
            AuditEvidenceHandle: ContractSamples.AuditEvidence.Handle);
        AuditRecordPolicyTreatmentV1 treatment = new(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.AuditEvidence.Handle,
            ProjectionTrustState.Current,
            ProjectionTrustState.Redacted,
            AuditRecordActionClassification.Allowed,
            ExportEligible: false,
            SeparateLogRequired: true,
            "retention-policy-standard",
            "Use the returned audit handle as governed evidence.");
        ConversationAuditRecordDetailsV1 details = new(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            ContractSamples.GovernanceTimestamp,
            AuditRecordActionClassification.Allowed,
            GovernanceOutcome.Succeeded,
            "retention-policy-standard",
            "customer-request",
            target,
            ContractSamples.AuditEvidence,
            treatment,
            ContractSamples.FreshnessV1,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            "correlation-001",
            "causation-001");

        JsonNode? json = JsonNode.Parse(JsonSerializer.Serialize(details, WebOptions));

        json!["schemaVersion"]!.GetValue<int>().ShouldBe(1);
        json["actionClass"]!.GetValue<string>().ShouldBe("Allowed");
        json["governedTarget"]!["kind"]!.GetValue<string>().ShouldBe("AuditRecord");
        json["governedTarget"]!["auditEvidenceHandle"]!["value"]!.GetValue<string>().ShouldBe("audit-evidence-001");
        json.ToJsonString().ShouldNotContain("EventStore", Case.Insensitive);
        json.ToJsonString().ShouldNotContain("storage", Case.Insensitive);
        json.ToJsonString().ShouldNotContain("providerPayload", Case.Insensitive);
    }

    [Fact]
    public void AuditRecordContractsShouldKeepToStringContentSafe()
    {
        AuditRecordPolicyTreatmentV1 treatment = new(
            SchemaVersion.Current,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.AuditEvidence.Handle,
            ProjectionTrustState.Current,
            ProjectionTrustState.Current,
            AuditRecordActionClassification.Exported,
            ExportEligible: true,
            SeparateLogRequired: false,
            "retention-policy-standard",
            "Use this in-memory audit export response as governed evidence.");

        string text = treatment.ToString();

        text.ShouldNotContain("raw", Case.Insensitive);
        text.ShouldNotContain("EventStore", Case.Insensitive);
        text.ShouldNotContain("storage", Case.Insensitive);
    }

    [Fact]
    public void AuditRecordPublicContractsShouldNotExposeForbiddenSubstrateFields()
    {
        Type[] contractTypes =
        [
            typeof(GetConversationAuditRecordQuery),
            typeof(ConversationAuditRecordResult),
            typeof(ConversationAuditRecordDetailsV1),
            typeof(AuditRecordPolicyTreatmentV1),
        ];
        string[] forbidden =
        [
            "Sink",
            "Storage",
            "Stream",
            "PositionTopology",
            "Exception",
            "ProviderPayload",
            "MessageText",
            "RedactedText",
            "PartyPersonalData",
            "Raw",
            "Upstream",
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
}
