// <copyright file="BuyerAcceptanceDemoContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies buyer acceptance demo contracts remain deterministic, bounded, and content safe.
/// </summary>
public sealed class BuyerAcceptanceDemoContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DemoScenarioAndSummaryShouldSerializeStableCamelCaseShape()
    {
        BuyerAcceptanceDemoScenarioV1 scenario = Scenario();
        BuyerAcceptanceEvidenceSummaryV1 summary = Summary();

        string scenarioJson = JsonSerializer.Serialize(scenario, WebOptions);
        string summaryJson = JsonSerializer.Serialize(summary, WebOptions);

        JsonNode parsedScenario = JsonNode.Parse(scenarioJson)!;
        parsedScenario["schemaVersion"]!.GetValue<int>().ShouldBe(1);
        parsedScenario["scenarioId"]!.GetValue<string>().ShouldBe("buyer-acceptance-demo-v1");
        parsedScenario["syntheticDataMarker"]!.GetValue<string>().ShouldBe("synthetic-demo-data");
        parsedScenario["fixtures"]!.AsArray().Count.ShouldBe(2);
        parsedScenario["steps"]!.AsArray().Count.ShouldBe(2);
        scenarioJson.ShouldContain("\"stepKind\":\"find\"");
        scenarioJson.ShouldContain("\"fixtureKind\":\"full-trust\"");
        scenarioJson.ShouldContain("\"expectedTrustState\":\"current\"");
        scenarioJson.ShouldNotContain("poison-sentinel", Case.Insensitive);
        scenarioJson.ShouldNotContain("raw message", Case.Insensitive);

        JsonNode parsedSummary = JsonNode.Parse(summaryJson)!;
        parsedSummary["status"]!.GetValue<string>().ShouldBe("passed");
        parsedSummary["stepResults"]!.AsArray().Count.ShouldBe(2);
        summaryJson.ShouldContain("\"evidenceOwnership\":\"module\"");
        summaryJson.ShouldContain("\"evidenceScope\":[\"module\",\"inherited-platform-control\"]");
        summaryJson.ShouldContain("\"suite\":\"tenant-isolation\"");
        summaryJson.ShouldNotContain("EventStore", Case.Insensitive);
        summaryJson.ShouldNotContain("provider payload", Case.Insensitive);
    }

    [Theory]
    [InlineData("\"export-bundle\"", typeof(BuyerAcceptanceDemoStepKind))]
    [InlineData("\"cross_tenant\"", typeof(BuyerAcceptanceDemoStepKind))]
    [InlineData("\"production-data\"", typeof(BuyerAcceptanceDemoFixtureKind))]
    [InlineData("\"raw-stream\"", typeof(BuyerAcceptanceEvidenceOwnership))]
    [InlineData("\"warning\"", typeof(BuyerAcceptanceDemoExecutionStatus))]
    public void DemoClosedVocabularyJsonShouldRejectUnsupportedValues(string json, Type targetType)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize(json, targetType, WebOptions));

    [Theory]
    [InlineData("raw message content")]
    [InlineData("EventStore stream name")]
    [InlineData("provider payload")]
    [InlineData("tenant:tenant-001")]
    [InlineData("C:\\temp\\capture.txt")]
    public void DemoSafeTextShouldRejectDisclosureTerms(string unsafeValue)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new BuyerAcceptanceDemoStepV1(
            ContractSamples.Version,
            "step-unsafe",
            BuyerAcceptanceDemoStepKind.ReadDetail,
            BuyerAcceptanceDemoFixtureKind.FullTrust,
            BuyerAcceptanceDemoTrustState.Current,
            unsafeValue,
            "Continue with governed evidence.",
            ["AC1"]));

        exception.Message.ShouldNotContain(unsafeValue, Case.Insensitive);
    }

    [Fact]
    public void DemoScenarioShouldRejectDuplicateStepIds()
    {
        BuyerAcceptanceDemoStepV1 step = Step("step-find", BuyerAcceptanceDemoStepKind.Find);

        ArgumentException exception = Should.Throw<ArgumentException>(() => new BuyerAcceptanceDemoScenarioV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "buyer-acceptance-demo-v1",
            "synthetic-demo-data",
            "Buyer acceptance demo",
            "correlation-001",
            [Fixture("fixture-full", BuyerAcceptanceDemoFixtureKind.FullTrust)],
            [step, step],
            ["AC1"]));

        exception.Message.ShouldContain("unique", Case.Insensitive);
    }

    [Fact]
    public void DemoScenarioShouldRejectStepsForUndeclaredFixtureKinds()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new BuyerAcceptanceDemoScenarioV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "buyer-acceptance-demo-v1",
            "synthetic-demo-data",
            "Buyer acceptance demo",
            "correlation-001",
            [Fixture("fixture-full", BuyerAcceptanceDemoFixtureKind.FullTrust)],
            [Step("step-redaction", BuyerAcceptanceDemoStepKind.RedactionAudit, BuyerAcceptanceDemoFixtureKind.Redacted)],
            ["AC1"]));

        exception.Message.ShouldContain("declared fixture", Case.Insensitive);
    }

    [Fact]
    public void DemoStepShouldAcceptCompositeTemporalCursor()
    {
        BuyerAcceptanceDemoStepV1 step = new(
            ContractSamples.Version,
            "step-temporal",
            BuyerAcceptanceDemoStepKind.TemporalReconstruction,
            BuyerAcceptanceDemoFixtureKind.FullTrust,
            BuyerAcceptanceDemoTrustState.Current,
            "Review governed evidence",
            "Continue with governed evidence.",
            ["AC1"],
            ContractSamples.Conversation,
            TemporalCursor: "temporal:v1:pos:0000000003:projection:0000000100");

        step.TemporalCursor.ShouldBe("temporal:v1:pos:0000000003:projection:0000000100");
    }

    [Theory]
    [InlineData("temporal:v1:pos:0:projection:100")]
    [InlineData("temporal:v1:pos:3")]
    [InlineData("pos:3")]
    public void DemoStepShouldRejectMalformedTemporalCursor(string temporalCursor)
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new BuyerAcceptanceDemoStepV1(
            ContractSamples.Version,
            "step-temporal",
            BuyerAcceptanceDemoStepKind.TemporalReconstruction,
            BuyerAcceptanceDemoFixtureKind.FullTrust,
            BuyerAcceptanceDemoTrustState.Current,
            "Review governed evidence",
            "Continue with governed evidence.",
            ["AC1"],
            ContractSamples.Conversation,
            TemporalCursor: temporalCursor));

        exception.Message.ShouldContain("temporal", Case.Insensitive);
    }

    [Fact]
    public void EvidenceSummaryShouldRejectDuplicateRequirementMappings()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => new BuyerAcceptanceEvidenceSummaryV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "buyer-acceptance-demo-v1",
            "synthetic-demo-data",
            ContractSamples.GovernanceTimestamp,
            "runner-001",
            "correlation-001",
            BuyerAcceptanceDemoExecutionStatus.Passed,
            [StepResult("step-find", BuyerAcceptanceDemoStepKind.Find)],
            [],
            ["AC1", "AC1"],
            "Buyer acceptance demo passed.",
            [BuyerAcceptanceEvidenceOwnership.Module]));

        exception.Message.ShouldContain("unique", Case.Insensitive);
    }

    private static BuyerAcceptanceDemoScenarioV1 Scenario()
        => new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "buyer-acceptance-demo-v1",
            "synthetic-demo-data",
            "Buyer acceptance demo",
            "correlation-001",
            [
                Fixture("fixture-full", BuyerAcceptanceDemoFixtureKind.FullTrust),
                Fixture("fixture-denial", BuyerAcceptanceDemoFixtureKind.CrossTenantPoison),
            ],
            [
                Step("step-find", BuyerAcceptanceDemoStepKind.Find),
                Step("step-denial", BuyerAcceptanceDemoStepKind.CrossTenantDenial, BuyerAcceptanceDemoFixtureKind.CrossTenantPoison),
            ],
            ["AC1", "AC2", "AC3", "AC4", "AC5"]);

    private static BuyerAcceptanceEvidenceSummaryV1 Summary()
        => new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "buyer-acceptance-demo-v1",
            "synthetic-demo-data",
            ContractSamples.GovernanceTimestamp,
            "runner-001",
            "correlation-001",
            BuyerAcceptanceDemoExecutionStatus.Passed,
            [
                StepResult("step-find", BuyerAcceptanceDemoStepKind.Find),
                StepResult("step-denial", BuyerAcceptanceDemoStepKind.CrossTenantDenial, BuyerAcceptanceEvidenceOwnership.Module),
            ],
            [
                new BuyerAcceptanceVerificationSummaryV1(
                    ContractSamples.Version,
                    ConversationGovernanceVerificationSuite.TenantIsolation,
                    ConversationGovernanceVerificationExecutionStatus.Completed,
                    ConversationGovernanceVerificationFailureClassification.Passed,
                    "Trusted scope matches derived records.",
                    ConversationGovernanceVerificationRemediation.None,
                    ["AC1", "AC5"]),
            ],
            ["AC1", "AC2", "AC3", "AC4", "AC5"],
            "Buyer acceptance demo passed.",
            [BuyerAcceptanceEvidenceOwnership.Module, BuyerAcceptanceEvidenceOwnership.InheritedPlatformControl]);

    private static BuyerAcceptanceDemoFixtureV1 Fixture(string id, BuyerAcceptanceDemoFixtureKind kind)
        => new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            id,
            kind,
            BuyerAcceptanceDemoTrustState.Current,
            "synthetic-demo-data",
            "Synthetic governed conversation",
            "Continue with governed evidence.",
            ContractSamples.Conversation,
            ["AC1", "AC2"]);

    private static BuyerAcceptanceDemoStepV1 Step(
        string id,
        BuyerAcceptanceDemoStepKind kind,
        BuyerAcceptanceDemoFixtureKind? fixtureKind = null)
        => new(
            ContractSamples.Version,
            id,
            kind,
            fixtureKind ?? BuyerAcceptanceDemoFixtureKind.FullTrust,
            BuyerAcceptanceDemoTrustState.Current,
            "Review governed evidence",
            "Continue with governed evidence.",
            ["AC1", "AC5"],
            ContractSamples.Conversation,
            ContractSamples.Business,
            "message:message-001",
            ContractSamples.AuditEvidence.Handle,
            EvidenceHandles: [new ConversationGovernanceVerificationEvidenceHandle("verification-proof-001")]);

    private static BuyerAcceptanceEvidenceStepResultV1 StepResult(
        string id,
        BuyerAcceptanceDemoStepKind kind,
        BuyerAcceptanceEvidenceOwnership? ownership = null)
        => new(
            ContractSamples.Version,
            id,
            kind,
            BuyerAcceptanceDemoExecutionStatus.Passed,
            BuyerAcceptanceDemoTrustState.Current,
            ownership ?? BuyerAcceptanceEvidenceOwnership.Module,
            "Step passed.",
            "Continue with governed evidence.",
            ["AC1", "AC5"],
            [new ConversationGovernanceVerificationEvidenceHandle("verification-proof-001")]);
}
