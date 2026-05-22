// <copyright file="GovernanceVerificationContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Governance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies governance verification public contracts remain closed, structured, and content safe.
/// </summary>
public sealed class GovernanceVerificationContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void GovernanceVerificationContractsShouldKeepStableCamelCaseJsonShape()
    {
        ConversationGovernanceVerificationScopeV1 scope = new(
            ContractSamples.Version,
            ConversationGovernanceVerificationScopeKind.Conversation,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.Zero));

        ConversationGovernanceVerificationCheckResultV1 check = new(
            ContractSamples.Version,
            ConversationGovernanceVerificationSuite.AuditPairing,
            "audit-pairing",
            ["AC1", "AC5"],
            ConversationGovernanceVerificationExecutionStatus.Completed,
            ConversationGovernanceVerificationFailureClassification.Passed,
            "Governed state has paired audit references.",
            ConversationGovernanceVerificationRemediation.None,
            new ConversationGovernanceVerificationEvidenceHandle("verification-proof-001"));

        ConversationGovernanceVerificationRunResultV1 result = new(
            ContractSamples.Version,
            scope,
            [ConversationGovernanceVerificationSuite.AuditPairing],
            new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.Zero),
            "correlation-001",
            ConversationGovernanceVerificationExecutionStatus.Completed,
            ConversationGovernanceVerificationFailureClassification.Passed,
            "Governance verification passed.",
            [check],
            ContractSamples.AuditEvidence);

        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"scope":{"schemaVersion":1,"scopeKind":"conversation","tenantId":"tenant:tenant-001","conversationId":"conv:conversation-001","requestedFromUtc":"2026-05-18T11:00:00+00:00","requestedToUtc":"2026-05-18T12:00:00+00:00"},"selectedSuites":["audit-pairing"],"generatedAtUtc":"2026-05-18T12:00:00+00:00","correlationId":"correlation-001","status":"completed","classification":"passed","safeSummary":"Governance verification passed.","checks":[{"schemaVersion":1,"suite":"audit-pairing","checkName":"audit-pairing","requirementMappings":["AC1","AC5"],"status":"completed","classification":"passed","safeDetail":"Governed state has paired audit references.","remediation":"none","evidence":{"value":"verification-proof-001"}}],"auditEvidence":{"handle":{"value":"audit-evidence-001"},"policyReference":"retention-policy-standard","capturedAt":"2026-05-18T12:00:00+00:00"},"auditNotRecordedReason":null}
            """,
            result);
    }

    [Theory]
    [InlineData("\"audit_pairing\"", typeof(ConversationGovernanceVerificationSuite))]
    [InlineData("\"raw-stream\"", typeof(ConversationGovernanceVerificationSuite))]
    [InlineData("\"governance_failed\"", typeof(ConversationGovernanceVerificationFailureClassification))]
    [InlineData("\"provider-payload\"", typeof(ConversationGovernanceVerificationFailureClassification))]
    [InlineData("\"read-eventstore\"", typeof(ConversationGovernanceVerificationRemediation))]
    public void GovernanceVerificationClosedVocabularyJsonShouldRejectUnsupportedValues(string json, Type targetType)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize(json, targetType, WebOptions));

    [Theory]
    [InlineData("raw message content")]
    [InlineData("EventStore stream name")]
    [InlineData("provider payload")]
    [InlineData("tenant:tenant-001")]
    [InlineData("Exception stack trace")]
    public void GovernanceVerificationSafeTextShouldRejectDisclosureTerms(string unsafeValue)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new ConversationGovernanceVerificationCheckResultV1(
            ContractSamples.Version,
            ConversationGovernanceVerificationSuite.AuditPairing,
            "audit-pairing",
            ["AC1"],
            ConversationGovernanceVerificationExecutionStatus.Failed,
            ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            unsafeValue,
            ConversationGovernanceVerificationRemediation.InspectGovernanceEvidence));

        exception.Message.ShouldNotContain(unsafeValue, Case.Insensitive);
    }

    [Fact]
    public void GovernanceVerificationMinimumVocabulariesShouldBeExplicit()
    {
        ConversationGovernanceVerificationSuite.All.Select(suite => suite.Value).ShouldBe(
            [
                "audit-pairing",
                "tenant-isolation",
                "redaction-replay",
                "projection-rebuild",
                "provider-portability",
                "schema-compatibility",
            ],
            ignoreOrder: false);

        ConversationGovernanceVerificationFailureClassification[] classifications =
        [
            ConversationGovernanceVerificationFailureClassification.Passed,
            ConversationGovernanceVerificationFailureClassification.GovernanceFailed,
            ConversationGovernanceVerificationFailureClassification.InfrastructureFailed,
            ConversationGovernanceVerificationFailureClassification.DependencyUnavailable,
            ConversationGovernanceVerificationFailureClassification.DataUnavailable,
            ConversationGovernanceVerificationFailureClassification.StaleProjection,
            ConversationGovernanceVerificationFailureClassification.UnsupportedVersion,
            ConversationGovernanceVerificationFailureClassification.UnauthorizedOrHidden,
            ConversationGovernanceVerificationFailureClassification.ExecutionFailed,
            ConversationGovernanceVerificationFailureClassification.NotApplicable,
        ];

        classifications.Select(classification => classification.Value).ShouldBeUnique();
    }

    [Fact]
    public void GovernanceVerificationRequestShouldRejectDuplicateSuites()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new ConversationGovernanceVerificationRequestV1(
            ContractSamples.Version,
            new ConversationGovernanceVerificationScopeV1(
                ContractSamples.Version,
                ConversationGovernanceVerificationScopeKind.Conversation,
                ContractSamples.Tenant,
                ContractSamples.Conversation),
            [ConversationGovernanceVerificationSuite.AuditPairing, ConversationGovernanceVerificationSuite.AuditPairing],
            "correlation-001"));

        exception.Message.ShouldContain("unique", Case.Insensitive);
    }

    [Fact]
    public void GovernanceVerificationScopeShouldRejectInvertedTimeWindow()
    {
        ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            new ConversationGovernanceVerificationScopeV1(
                ContractSamples.Version,
                ConversationGovernanceVerificationScopeKind.TimeWindow,
                ContractSamples.Tenant,
                RequestedFromUtc: new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.Zero),
                RequestedToUtc: new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero)));

        exception.Message.ShouldContain("must not end before", Case.Insensitive);
    }

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
