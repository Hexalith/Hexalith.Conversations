// <copyright file="GovernanceContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies governance and audit contract semantics.
/// </summary>
public sealed class GovernanceContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Ensures governance metadata rejects missing or unsafe authority fields.
    /// </summary>
    /// <param name="value">The invalid value.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GovernanceMetadataShouldRequireAuthorityFields(string value)
    {
        Should.Throw<ArgumentException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            value,
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            value,
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            value));
    }

    /// <summary>
    /// Ensures tenant, conversation, actor, schema, and timestamp are explicit and validated.
    /// </summary>
    [Fact]
    public void GovernanceMetadataShouldRejectMissingScopeAndImplausibleTimestamp()
    {
        Should.Throw<ArgumentNullException>(() => new GovernanceOperationMetadata(
            null!,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentNullException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            null!,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentNullException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            null!,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentNullException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            null!,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        Should.Throw<ArgumentOutOfRangeException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            DateTimeOffset.MinValue,
            "correlation-001"));

        Should.Throw<ArgumentOutOfRangeException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            DateTimeOffset.MaxValue,
            "correlation-001"));

        Should.Throw<ArgumentOutOfRangeException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.FromHours(2)),
            "correlation-001"));
    }

    /// <summary>
    /// Ensures command metadata can be composed into governance metadata without copying idempotency as evidence.
    /// </summary>
    [Fact]
    public void GovernanceMetadataShouldMapFromCommandMetadataWithoutUsingIdempotencyAsEvidence()
    {
        GovernanceOperationMetadata metadata = GovernanceOperationMetadata.FromCommandMetadata(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp);

        metadata.SchemaVersion.ShouldBe(ContractSamples.CommandMetadata.SchemaVersion);
        metadata.TenantId.ShouldBe(ContractSamples.CommandMetadata.TenantId);
        metadata.ActorPartyId.ShouldBe(ContractSamples.CommandMetadata.ActorPartyId);
        metadata.ConversationId.ShouldBe(ContractSamples.Conversation);
        metadata.CorrelationId.ShouldBe(ContractSamples.CommandMetadata.CorrelationId);
        metadata.CausationId.ShouldBe(ContractSamples.CommandMetadata.CausationId);
    }

    /// <summary>
    /// Ensures governance free text rejects unsafe terms without echoing the unsafe value.
    /// </summary>
    /// <param name="unsafeValue">The unsafe value.</param>
    [Theory]
    [InlineData("raw message content")]
    [InlineData("audit sink unavailable")]
    [InlineData("EventStore position")]
    [InlineData("provider payload")]
    [InlineData("exception details")]
    [InlineData("token claim")]
    [InlineData("storage location")]
    [InlineData("provider sdk")]
    [InlineData("raw diagnostics")]
    [InlineData("handler projection")]
    public void GovernanceFreeTextShouldRejectUnsafeDisclosureTerms(string unsafeValue)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            unsafeValue,
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001"));

        exception.Message.ShouldNotContain(unsafeValue, Case.Insensitive);
    }

    /// <summary>
    /// Ensures malformed correlation, causation, and evidence references are rejected without tenant inference.
    /// </summary>
    [Fact]
    public void GovernanceReferencesShouldRejectMalformedOrUnsafeIdentifiers()
    {
        Should.Throw<ArgumentException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "bad correlation"));

        Should.Throw<ArgumentException>(() => new GovernanceOperationMetadata(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.Actor,
            "customer-request",
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp,
            "correlation-001",
            "raw-upstream-reference"));

        Should.Throw<ArgumentException>(() => new GovernanceAuditEvidenceReference(
            new AuditEvidenceHandle("audit-evidence-001"),
            "policy reference with spaces",
            ContractSamples.GovernanceTimestamp));

        Should.Throw<ArgumentException>(() => new AuditEvidenceHandle("storage-location-001"));
    }

    /// <summary>
    /// Ensures governance request and evidence contracts fail closed when required contract parts are absent.
    /// </summary>
    [Fact]
    public void GovernanceRequestAndEvidenceShouldRequireExplicitContractParts()
    {
        Should.Throw<ArgumentNullException>(() => new GovernanceRequest(
            null!,
            GovernanceOperationKind.SetRetentionPolicy,
            ContractSamples.GovernanceConversationTarget));

        Should.Throw<ArgumentNullException>(() => new GovernanceRequest(
            ContractSamples.GovernanceMetadata,
            null!,
            ContractSamples.GovernanceConversationTarget));

        Should.Throw<ArgumentNullException>(() => new GovernanceRequest(
            ContractSamples.GovernanceMetadata,
            GovernanceOperationKind.SetRetentionPolicy,
            null!));

        Should.Throw<ArgumentNullException>(() => new GovernanceAuditEvidence(
            null!,
            GovernanceOperationKind.SetRetentionPolicy,
            ContractSamples.GovernanceConversationTarget,
            GovernanceOutcome.Succeeded,
            ContractSamples.AuditEvidence));

        Should.Throw<ArgumentNullException>(() => new GovernanceAuditEvidence(
            ContractSamples.GovernanceMetadata,
            GovernanceOperationKind.SetRetentionPolicy,
            null!,
            GovernanceOutcome.Succeeded,
            ContractSamples.AuditEvidence));

        Should.Throw<ArgumentNullException>(() => new GovernanceAuditEvidence(
            ContractSamples.GovernanceMetadata,
            GovernanceOperationKind.SetRetentionPolicy,
            ContractSamples.GovernanceConversationTarget,
            null!,
            ContractSamples.AuditEvidence));

        Should.Throw<ArgumentNullException>(() => new GovernanceAuditEvidence(
            ContractSamples.GovernanceMetadata,
            GovernanceOperationKind.SetRetentionPolicy,
            ContractSamples.GovernanceConversationTarget,
            GovernanceOutcome.Succeeded,
            null!));

        Should.Throw<ArgumentNullException>(() => new GovernanceAuditEvidenceReference(
            null!,
            "retention-policy-standard",
            ContractSamples.GovernanceTimestamp));
    }

    /// <summary>
    /// Ensures target and audit evidence references reject unsafe values at the public boundary.
    /// </summary>
    [Fact]
    public void GovernanceTargetAndAuditReferenceShouldRejectUnsafeBoundaryValues()
    {
        Should.Throw<ArgumentException>(() => new GovernanceTarget(
            GovernedTargetKind.ContentSegment,
            SegmentReference: "storage:location-001"));

        Should.Throw<ArgumentException>(() => new GovernanceTarget(
            GovernedTargetKind.ContentSegment,
            SegmentReference: "segment reference"));

        Should.Throw<ArgumentException>(() => new AuditEvidenceHandle("audit sink 001"));

        Should.Throw<ArgumentOutOfRangeException>(() => new GovernanceAuditEvidenceReference(
            new AuditEvidenceHandle("audit-evidence-001"),
            "retention-policy-standard",
            DateTimeOffset.MinValue));

        Should.Throw<ArgumentOutOfRangeException>(() => new GovernanceAuditEvidenceReference(
            new AuditEvidenceHandle("audit-evidence-001"),
            "retention-policy-standard",
            new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.FromHours(1))));
    }

    /// <summary>
    /// Ensures malformed governance JSON cannot invent unsupported public vocabulary values.
    /// </summary>
    [Theory]
    [InlineData("\"EraseSourceEvent\"", typeof(GovernanceOperationKind))]
    [InlineData("\"ProviderPayload\"", typeof(GovernedTargetKind))]
    [InlineData("\"DestroyHistory\"", typeof(RetentionAction))]
    [InlineData("\"PersonalEmail\"", typeof(SensitivityCategory))]
    [InlineData("\"SourceEventDeletion\"", typeof(RedactionCategory))]
    [InlineData("\"StorageCommitted\"", typeof(GovernanceOutcome))]
    [InlineData("\"ReadHandlerLogs\"", typeof(GovernanceRemediation))]
    public void GovernanceClosedVocabularyJsonShouldRejectUnsupportedValues(string json, Type targetType)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize(json, targetType, WebOptions));

    /// <summary>
    /// Ensures governance metadata required fields are enforced during JSON deserialization.
    /// </summary>
    [Theory]
    [InlineData("""{"schemaVersion":1,"conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001"}""")]
    [InlineData("""{"schemaVersion":1,"tenantId":"tenant:tenant-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001"}""")]
    [InlineData("""{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001"}""")]
    [InlineData("""{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001"}""")]
    [InlineData("""{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001"}""")]
    [InlineData("""{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00"}""")]
    public void GovernanceMetadataJsonShouldRequireAuthorityFields(string json)
        => Should.Throw<ArgumentException>(() => JsonSerializer.Deserialize<GovernanceOperationMetadata>(json, WebOptions));

    /// <summary>
    /// Ensures legal-hold deferrals stay explicit as policy-blocked evidence, not silent success.
    /// </summary>
    [Fact]
    public void LegalHoldDeferralShouldSerializeAsPolicyBlockedEvidence()
    {
        GovernanceAuditEvidence evidence = ContractSamples.GovernanceEvidence(
            GovernanceOperationKind.DeferForLegalHold,
            GovernanceOutcome.PolicyBlocked);

        evidence.OperationKind.ShouldBe(GovernanceOperationKind.DeferForLegalHold);
        evidence.Outcome.ShouldBe(GovernanceOutcome.PolicyBlocked);
        evidence.Remediation.ShouldBe(GovernanceRemediation.WaitForLegalHoldRelease);

        string json = JsonSerializer.Serialize(evidence, WebOptions);
        json.ShouldContain("\"operationKind\":\"DeferForLegalHold\"", Case.Sensitive);
        json.ShouldContain("\"outcome\":\"PolicyBlocked\"", Case.Sensitive);
        json.ShouldContain("\"remediation\":\"WaitForLegalHoldRelease\"", Case.Sensitive);
        json.ShouldNotContain("\"outcome\":\"Succeeded\"", Case.Sensitive);
    }

    /// <summary>
    /// Ensures every governance mutation kind can be paired with each required public outcome state.
    /// </summary>
    [Fact]
    public void GovernanceMutationMatrixShouldHavePairedAuditEvidenceForEachOutcome()
    {
        GovernanceOperationKind[] operationKinds =
        [
            GovernanceOperationKind.SetRetentionPolicy,
            GovernanceOperationKind.ReplaceRetentionPolicy,
            GovernanceOperationKind.MarkContentSensitive,
            GovernanceOperationKind.RedactMessageContent,
            GovernanceOperationKind.ArchiveConversation,
            GovernanceOperationKind.LogicallyDeleteConversation,
            GovernanceOperationKind.DeferForLegalHold,
            GovernanceOperationKind.GovernAuditRecord,
            GovernanceOperationKind.RecordPrivilegedJustification,
        ];

        GovernanceOutcome[] outcomes =
        [
            GovernanceOutcome.Succeeded,
            GovernanceOutcome.Denied,
            GovernanceOutcome.AuditUnavailableFailed,
            GovernanceOutcome.PolicyBlocked,
        ];

        foreach (GovernanceOperationKind operationKind in operationKinds)
        {
            foreach (GovernanceOutcome outcome in outcomes)
            {
                GovernanceAuditEvidence evidence = ContractSamples.GovernanceEvidence(operationKind, outcome);

                evidence.OperationKind.ShouldBe(operationKind);
                evidence.Outcome.ShouldBe(outcome);
                evidence.Metadata.TenantId.ShouldBe(ContractSamples.Tenant);
                evidence.Metadata.ConversationId.ShouldBe(ContractSamples.Conversation);
                evidence.Metadata.ActorPartyId.ShouldBe(ContractSamples.Actor);
                evidence.AuditEvidence.ShouldNotBeNull();
            }
        }
    }

    /// <summary>
    /// Ensures governance state concepts remain explicit and do not imply source-event deletion.
    /// </summary>
    [Fact]
    public void GovernanceStateConceptsShouldDistinguishRetentionRedactionAndLegalHoldSemantics()
    {
        GovernanceStateConcept[] concepts =
        [
            GovernanceStateConcept.EventHistory,
            GovernanceStateConcept.DisplayedContent,
            GovernanceStateConcept.AuditRecord,
            GovernanceStateConcept.DerivedMaterialization,
            GovernanceStateConcept.Archival,
            GovernanceStateConcept.LogicalDeletion,
            GovernanceStateConcept.RetentionEnforcement,
            GovernanceStateConcept.LegalHoldDeferral,
        ];

        concepts.Select(concept => concept.Value).ShouldBeUnique();
        concepts.ShouldNotContain(concept => concept.Value.Contains("SourceEventDeletion", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures governance record ToString output stays content-safe and never echoes rationale text or policy details.
    /// </summary>
    [Fact]
    public void GovernanceRecordsShouldKeepToStringContentSafe()
    {
        const string rationaleSample = "customer-request";
        const string policySample = "retention-policy-standard";

        string metadataText = ContractSamples.GovernanceMetadata.ToString();
        metadataText.ShouldNotContain(rationaleSample, Case.Insensitive);
        metadataText.ShouldNotContain(policySample, Case.Insensitive);
        metadataText.ShouldContain("CorrelationId");

        string auditEvidenceText = ContractSamples.AuditEvidence.ToString();
        auditEvidenceText.ShouldNotContain(policySample, Case.Insensitive);

        string requestText = ContractSamples.GovernanceRequest.ToString();
        requestText.ShouldNotContain(rationaleSample, Case.Insensitive);
        requestText.ShouldNotContain(policySample, Case.Insensitive);

        string evidenceText = ContractSamples
            .GovernanceEvidence(GovernanceOperationKind.SetRetentionPolicy, GovernanceOutcome.Denied)
            .ToString();
        evidenceText.ShouldNotContain(rationaleSample, Case.Insensitive);
        evidenceText.ShouldNotContain(policySample, Case.Insensitive);

        string commandText = ContractSamples.RetentionCommand.ToString();
        commandText.ShouldNotContain(rationaleSample, Case.Insensitive);
        commandText.ShouldNotContain(policySample, Case.Insensitive);

        string retentionSetText = new RetentionPolicySet(
            ContractSamples.RetentionSetEventMetadata,
            policySample,
            rationaleSample,
            ContractSamples.AuditEvidence).ToString();
        retentionSetText.ShouldNotContain(rationaleSample, Case.Insensitive);
        retentionSetText.ShouldNotContain(policySample, Case.Insensitive);
    }

    /// <summary>
    /// Ensures retention command and event contracts reject unsafe rationale, policy, and audit values.
    /// </summary>
    [Theory]
    [InlineData("raw message content")]
    [InlineData("EventStore stream")]
    [InlineData("provider sdk")]
    [InlineData("token claim")]
    [InlineData("c:\\audit\\location")]
    [InlineData("https://audit.local/value")]
    public void RetentionContractsShouldRejectUnsafeValues(string unsafeValue)
    {
        Should.Throw<ArgumentException>(() => new SetConversationRetentionPolicyCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            "retention-policy-standard",
            unsafeValue,
            ContractSamples.GovernanceTimestamp));

        Should.Throw<ArgumentException>(() => new SetConversationRetentionPolicyCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            unsafeValue,
            "customer-request",
            ContractSamples.GovernanceTimestamp));
    }

    /// <summary>
    /// Ensures governance vocabularies are closed and reject unsupported caller strings.
    /// </summary>
    [Fact]
    public void GovernanceClosedVocabulariesShouldRejectUnsupportedValues()
    {
        Should.Throw<ArgumentException>(() => GovernanceOperationKind.Parse("EraseSourceEvent"));
        Should.Throw<ArgumentException>(() => GovernedTargetKind.Parse("ProviderPayload"));
        Should.Throw<ArgumentException>(() => GovernanceOutcome.Parse("StorageCommitted"));
        Should.Throw<ArgumentException>(() => GovernanceRemediation.Parse("ReadHandlerLogs"));
    }

    /// <summary>
    /// Ensures stable governance JSON fixtures remain content-safe and schema-versioned.
    /// </summary>
    [Fact]
    public void GovernanceFixturesShouldKeepStableCamelCaseJsonShapes()
    {
        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001","causationId":"causation-001"}
            """,
            ContractSamples.GovernanceMetadata);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001","causationId":"causation-001"},"operationKind":"SetRetentionPolicy","target":{"kind":"Conversation","messageId":null,"fileId":null,"partyId":null,"segmentReference":null},"retentionAction":"ApplyPolicy","sensitivityCategory":null,"redactionCategory":null,"archivalState":null,"legalHoldDeferral":null,"privilegedActionClass":null}
            """,
            ContractSamples.GovernanceRequest);

        AssertJsonEquivalent(
            """
            {"handle":{"value":"audit-evidence-001"},"policyReference":"retention-policy-standard","capturedAt":"2026-05-18T12:00:00+00:00"}
            """,
            ContractSamples.AuditEvidence);

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001","causationId":"causation-001"},"operationKind":"SetRetentionPolicy","target":{"kind":"Conversation","messageId":null,"fileId":null,"partyId":null,"segmentReference":null},"outcome":"Denied","auditEvidence":{"handle":{"value":"audit-evidence-001"},"policyReference":"retention-policy-standard","capturedAt":"2026-05-18T12:00:00+00:00"},"remediation":"ResubmitWithPolicyReference"}
            """,
            ContractSamples.GovernanceEvidence(GovernanceOperationKind.SetRetentionPolicy, GovernanceOutcome.Denied));

        AssertJsonEquivalent(
            """
            {"metadata":{"schemaVersion":1,"tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","actorPartyId":"party:party-actor","rationale":"customer-request","policyReference":"retention-policy-standard","operationTimestamp":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001","causationId":"causation-001"},"operationKind":"SetRetentionPolicy","target":{"kind":"Conversation","messageId":null,"fileId":null,"partyId":null,"segmentReference":null},"outcome":"AuditUnavailableFailed","auditEvidence":{"handle":{"value":"audit-evidence-001"},"policyReference":"retention-policy-standard","capturedAt":"2026-05-18T12:00:00+00:00"},"remediation":"RetryWhenAuditAvailable"}
            """,
            ContractSamples.GovernanceEvidence(GovernanceOperationKind.SetRetentionPolicy, GovernanceOutcome.AuditUnavailableFailed));
    }

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
