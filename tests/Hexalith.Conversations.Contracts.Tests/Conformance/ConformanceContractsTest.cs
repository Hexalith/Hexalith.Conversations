// <copyright file="ConformanceContractsTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies the adopter-facing conformance contracts expose closed vocabularies, traceable mappings,
/// failure-class invariants, content-safe free text, and stable web JSON.
/// </summary>
public sealed class ConformanceContractsTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/conformance");

    [Fact]
    public void ConformanceCheckVocabularyShouldCoverEveryCoreSurface()
    {
        string[] expected =
        [
            "create-conversation",
            "append-message",
            "read-timeline",
            "tenant-binding",
            "party-identity",
            "idempotency",
            "error-envelope",
            "projection-freshness",
            "event-publication",
            "governance-precondition",
            "compatibility-discovery",
        ];

        ConformanceCheck.All.Select(check => check.Value).ShouldBe(expected);
    }

    [Fact]
    public void ConformanceOutcomeShouldReuseSharedReadinessLanguageWithoutSynonyms()
    {
        ConformanceOutcome.All.Select(outcome => outcome.Value)
            .ShouldBe(["ready", "degraded", "blocked", "unknown"]);

        foreach (string synonym in new[] { "ok", "healthy", "pass-ish", "maybe", "pass", "fail" })
        {
            Should.Throw<ArgumentException>(() => ConformanceOutcome.Parse(synonym));
        }
    }

    [Fact]
    public void ConformanceFailureClassificationShouldDistinguishEveryFailureClass()
    {
        ConformanceFailureClassification.All.Select(value => value.Value)
            .ShouldBe(["conformant", "product-invariant", "infrastructure", "configuration", "unavailable-dependency", "execution"]);

        ConformanceFailureClassification.Conformant.IsFailure.ShouldBeFalse();
        ConformanceFailureClassification.ProductInvariant.IsFailure.ShouldBeTrue();
        ConformanceFailureClassification.Infrastructure.IsFailure.ShouldBeTrue();
        ConformanceFailureClassification.Configuration.IsFailure.ShouldBeTrue();
        ConformanceFailureClassification.UnavailableDependency.IsFailure.ShouldBeTrue();
        ConformanceFailureClassification.Execution.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("ready")]
    [InlineData("degraded")]
    [InlineData("blocked")]
    [InlineData("unknown")]
    public void ConformanceClosedVocabulariesShouldRejectStringContainingForbiddenCharacters(string _)
    {
        Should.Throw<ArgumentException>(() => ConformanceCheck.Parse("tenant:tenant-001"));
        Should.Throw<ArgumentException>(() => ConformanceOutcome.Parse("c:\\path"));
        Should.Throw<ArgumentException>(() => ConformanceFailureClassification.Parse("conv/hidden"));
    }

    [Fact]
    public void ConformantCheckResultMustNotCarryError()
        => Should.Throw<ArgumentException>(() => new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.CreateConversation,
            "supported",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            ["FR73"],
            ["supported-schema-versions"],
            ["release-gate-commands-queries-events"],
            "Create conversation accepted the supported request.",
            "none",
            Documentation,
            "conformance-create-conversation",
            ConversationErrorCatalog.CreateError(ConversationErrorCode.CommandValidationFailed, "correlation-001")));

    [Fact]
    public void NonConformantCheckResultMustCarryError()
        => Should.Throw<ArgumentException>(() => new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.CompatibilityDiscovery,
            "unsupported",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.ProductInvariant,
            ["FR74"],
            ["contract-compatibility"],
            ["release-gate-version-discovery"],
            "Unsupported version discovery returned a typed versioning error.",
            "use-supported-v1-package",
            Documentation,
            "conformance-compatibility-discovery"));

    [Fact]
    public void CheckResultMappingsMustBeUniqueAndNonEmpty()
    {
        Should.Throw<ArgumentException>(() => Result(["FR73", "FR73"], ["supported-schema-versions"], ["release-gate-commands-queries-events"]));
        Should.Throw<ArgumentException>(() => Result([], ["supported-schema-versions"], ["release-gate-commands-queries-events"]));
        Should.Throw<ArgumentException>(() => Result(["FR73"], [], ["release-gate-commands-queries-events"]));
        Should.Throw<ArgumentException>(() => Result(["FR73"], ["supported-schema-versions"], []));
    }

    [Fact]
    public void CheckResultFreeTextMustRejectProtectedValueDisclosure()
    {
        foreach (string unsafeText in new[] { "tenant-999", "party-hidden", "conversation-hidden", "EventStore envelope", "C:\\secret", "raw exception text" })
        {
            Should.Throw<ArgumentException>(() => new ConformanceCheckResultV1(
                SchemaVersion.Current,
                ConformanceCheck.CreateConversation,
                "supported",
                ConformanceOutcome.Ready,
                ConformanceFailureClassification.Conformant,
                ["FR73"],
                ["supported-schema-versions"],
                ["release-gate-commands-queries-events"],
                unsafeText,
                "none",
                Documentation,
                "conformance-create-conversation"));
        }
    }

    [Fact]
    public void CheckResultDocumentationMustUseHttps()
        => Should.Throw<ArgumentException>(() => new ConformanceCheckResultV1(
            SchemaVersion.Current,
            ConformanceCheck.CreateConversation,
            "supported",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            ["FR73"],
            ["supported-schema-versions"],
            ["release-gate-commands-queries-events"],
            "Create conversation accepted the supported request.",
            "none",
            new Uri("http://docs.hexalith.local/conversations"),
            "conformance-create-conversation"));

    [Fact]
    public void RunResultMustRequireAtLeastOneCheck()
        => Should.Throw<ArgumentException>(() => new ConformanceRunResultV1(
            SchemaVersion.Current,
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Empty run.",
            "adopter-core-conformance-v1",
            "conformance-runner",
            "correlation-conformance-001",
            new DateTimeOffset(2026, 5, 23, 9, 0, 0, TimeSpan.Zero),
            []));

    [Fact]
    public void RunResultMustRequireUtcTimestamp()
        => Should.Throw<ArgumentOutOfRangeException>(() => new ConformanceRunResultV1(
            SchemaVersion.Current,
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Run with non-UTC timestamp.",
            "adopter-core-conformance-v1",
            "conformance-runner",
            "correlation-conformance-001",
            new DateTimeOffset(2026, 5, 23, 9, 0, 0, TimeSpan.FromHours(2)),
            [ConformantResult()]));

    [Fact]
    public void RunResultShouldSerializeToStableCamelCaseWebJson()
    {
        ConformanceRunResultV1 run = new(
            SchemaVersion.Current,
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "All CORE conformance checks passed.",
            "adopter-core-conformance-v1",
            "conformance-runner",
            "correlation-conformance-001",
            new DateTimeOffset(2026, 5, 23, 9, 0, 0, TimeSpan.Zero),
            [ConformantResult()]);

        string json = JsonSerializer.Serialize(run, WebOptions);

        json.ShouldContain("\"overallOutcome\":\"ready\"");
        json.ShouldContain("\"overallClassification\":\"conformant\"");
        json.ShouldContain("\"suiteId\":\"adopter-core-conformance-v1\"");
        json.ShouldContain("\"check\":\"tenant-binding\"");
        json.ShouldContain("\"failureClassification\":\"conformant\"");
    }

    [Fact]
    public void RunResultShouldRoundTripWithWebDefaults()
    {
        ConformanceRunResultV1 run = new(
            SchemaVersion.Current,
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.ProductInvariant,
            "One CORE conformance check failed.",
            "adopter-core-conformance-v1",
            "conformance-runner",
            "correlation-conformance-001",
            new DateTimeOffset(2026, 5, 23, 9, 0, 0, TimeSpan.Zero),
            [NonConformantResult()]);

        string json = JsonSerializer.Serialize(run, WebOptions);
        ConformanceRunResultV1? deserialized = JsonSerializer.Deserialize<ConformanceRunResultV1>(json, WebOptions);

        deserialized.ShouldNotBeNull();
        deserialized!.OverallOutcome.ShouldBe(ConformanceOutcome.Blocked);
        deserialized.OverallClassification.ShouldBe(ConformanceFailureClassification.ProductInvariant);
        deserialized.Checks.Count.ShouldBe(1);
        deserialized.Checks[0].Error.ShouldNotBeNull();
    }

    [Fact]
    public void RunResultShouldTolerateAdditiveJsonProperties()
    {
        ConformanceRunResultV1 run = new(
            SchemaVersion.Current,
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "All CORE conformance checks passed.",
            "adopter-core-conformance-v1",
            "conformance-runner",
            "correlation-conformance-001",
            new DateTimeOffset(2026, 5, 23, 9, 0, 0, TimeSpan.Zero),
            [ConformantResult()]);

        JsonNode node = JsonNode.Parse(JsonSerializer.Serialize(run, WebOptions))!;
        node["futureField"] = "ignored-by-additive-tolerance";

        ConformanceRunResultV1? deserialized = JsonSerializer.Deserialize<ConformanceRunResultV1>(node.ToJsonString(), WebOptions);
        deserialized.ShouldNotBeNull();
    }

    [Fact]
    public void EveryConformanceCheckValueShouldRoundTripThroughParseAndJson()
    {
        foreach (ConformanceCheck check in ConformanceCheck.All)
        {
            ConformanceCheck.Parse(check.Value).ShouldBe(check);
            string json = JsonSerializer.Serialize(check, WebOptions);
            json.ShouldBe($"\"{check.Value}\"");
            JsonSerializer.Deserialize<ConformanceCheck>(json, WebOptions).ShouldBe(check);
        }
    }

    [Fact]
    public void EveryConformanceOutcomeValueShouldRoundTripThroughParseAndJson()
    {
        foreach (ConformanceOutcome outcome in ConformanceOutcome.All)
        {
            ConformanceOutcome.Parse(outcome.Value).ShouldBe(outcome);
            string json = JsonSerializer.Serialize(outcome, WebOptions);
            json.ShouldBe($"\"{outcome.Value}\"");
            JsonSerializer.Deserialize<ConformanceOutcome>(json, WebOptions).ShouldBe(outcome);
        }
    }

    [Fact]
    public void EveryConformanceFailureClassificationValueShouldRoundTripThroughParseAndJson()
    {
        foreach (ConformanceFailureClassification classification in ConformanceFailureClassification.All)
        {
            ConformanceFailureClassification.Parse(classification.Value).ShouldBe(classification);
            string json = JsonSerializer.Serialize(classification, WebOptions);
            json.ShouldBe($"\"{classification.Value}\"");
            JsonSerializer.Deserialize<ConformanceFailureClassification>(json, WebOptions).ShouldBe(classification);
            classification.IsFailure.ShouldBe(!classification.Equals(ConformanceFailureClassification.Conformant));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ready")]
    [InlineData("create_conversation")]
    [InlineData("create conversation")]
    [InlineData("unknown-vocabulary-value")]
    public void ConformanceVocabulariesShouldRejectMalformedOrUnknownValues(string? value)
    {
        Should.Throw<ArgumentException>(() => ConformanceCheck.Parse(value!));
        Should.Throw<ArgumentException>(() => ConformanceOutcome.Parse(value!));
        Should.Throw<ArgumentException>(() => ConformanceFailureClassification.Parse(value!));
    }

    [Fact]
    public void ConformanceVocabulariesShouldRejectOverlongTokens()
    {
        string overlong = new('a', 65);
        Should.Throw<ArgumentException>(() => ConformanceCheck.Parse(overlong));
        Should.Throw<ArgumentException>(() => ConformanceOutcome.Parse(overlong));
        Should.Throw<ArgumentException>(() => ConformanceFailureClassification.Parse(overlong));
    }

    [Fact]
    public void CheckResultScenarioCorrelationAndRemediationMustBeBoundedSafeTokens()
    {
        foreach (string unsafeToken in new[] { "scenario with space", "scenario/slash", "C:\\path", "tenant:001", new string('s', 129) })
        {
            // Scenario position.
            Should.Throw<ArgumentException>(() => Build(scenario: unsafeToken));

            // Remediation guidance code position.
            Should.Throw<ArgumentException>(() => Build(remediation: unsafeToken));

            // Correlation id position.
            Should.Throw<ArgumentException>(() => Build(correlation: unsafeToken));
        }
    }

    [Fact]
    public void CheckResultSafeMessageMustRejectControlCharactersAndOverlongText()
    {
        Should.Throw<ArgumentException>(() => Build(safeMessage: "Line one\nLine two"));
        Should.Throw<ArgumentException>(() => Build(safeMessage: new string('x', 513)));
    }

    [Fact]
    public void CheckResultMappingTokensMustPreserveReleaseGateSegmentsContainingTenantAndParty()
    {
        // Story 4.4 lesson regression: closed traceability tokens legitimately contain 'tenant-' and
        // 'party-' segments (release-gate-tenant-isolation, participant-identity, etc.). The mapping
        // validator must accept these closed machine identifiers without colliding with the free-text
        // disclosure blocklist, while the wire value preserves them verbatim.
        ConformanceCheckResultV1 result = new(
            SchemaVersion.Current,
            ConformanceCheck.TenantBinding,
            "supported",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            ["FR74"],
            ["participant-identity-validation", "projection-freshness"],
            ["release-gate-tenant-isolation"],
            "Tenant binding scoped the read to the authorized tenant.",
            "none",
            Documentation,
            "correlation-conformance-tenantbinding");

        result.ReleaseGateMappings.ShouldContain("release-gate-tenant-isolation");
        result.PreconditionMappings.ShouldContain("participant-identity-validation");

        string json = JsonSerializer.Serialize(result, WebOptions);
        json.ShouldContain("release-gate-tenant-isolation");
        json.ShouldContain("participant-identity-validation");
    }

    [Fact]
    public void CheckResultMappingTokensMustRejectStorageSyntaxAndPrefixedProtectedIdentifiers()
    {
        Should.Throw<ArgumentException>(() => Build(requirements: ["tenant:tenant-001"]));
        Should.Throw<ArgumentException>(() => Build(preconditions: ["projection/freshness"]));
        Should.Throw<ArgumentException>(() => Build(releaseGates: ["C:\\release-gate"]));
    }

    [Fact]
    public void CheckResultOptionalAuditHandleMustBeBoundedSafeTokenWhenSupplied()
    {
        Build(auditHandle: "audit-handle-conformance-001").AuditHandle.ShouldBe("audit-handle-conformance-001");
        Should.Throw<ArgumentException>(() => Build(auditHandle: "audit handle with space"));
        Should.Throw<ArgumentException>(() => Build(auditHandle: "C:\\audit"));
    }

    private static ConformanceCheckResultV1 Build(
        string scenario = "supported",
        string remediation = "none",
        string correlation = "conformance-create-conversation",
        string safeMessage = "Create conversation accepted the supported request.",
        IReadOnlyList<string>? requirements = null,
        IReadOnlyList<string>? preconditions = null,
        IReadOnlyList<string>? releaseGates = null,
        string? auditHandle = null)
        => new(
            SchemaVersion.Current,
            ConformanceCheck.CreateConversation,
            scenario,
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            requirements ?? ["FR73"],
            preconditions ?? ["supported-schema-versions"],
            releaseGates ?? ["release-gate-commands-queries-events"],
            safeMessage,
            remediation,
            Documentation,
            correlation,
            Error: null,
            AuditHandle: auditHandle);

    private static ConformanceCheckResultV1 Result(
        IReadOnlyList<string> requirements,
        IReadOnlyList<string> preconditions,
        IReadOnlyList<string> releaseGates)
        => new(
            SchemaVersion.Current,
            ConformanceCheck.CreateConversation,
            "supported",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            requirements,
            preconditions,
            releaseGates,
            "Create conversation accepted the supported request.",
            "none",
            Documentation,
            "conformance-create-conversation");

    private static ConformanceCheckResultV1 ConformantResult()
        => new(
            SchemaVersion.Current,
            ConformanceCheck.TenantBinding,
            "supported",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            ["FR74"],
            ["projection-freshness"],
            ["release-gate-tenant-isolation"],
            "Tenant binding scoped the read to the authorized tenant.",
            "none",
            Documentation,
            "correlation-conformance-tenantbinding");

    private static ConformanceCheckResultV1 NonConformantResult()
        => new(
            SchemaVersion.Current,
            ConformanceCheck.CompatibilityDiscovery,
            "unsupported",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.ProductInvariant,
            ["FR74"],
            ["contract-compatibility"],
            ["release-gate-version-discovery"],
            "Unsupported version discovery returned a typed versioning error.",
            "use-supported-v1-package",
            Documentation,
            "conformance-compatibility-discovery",
            ConversationErrorCatalog.CreateError(ConversationErrorCode.SchemaVersionUnsupported, "conformance-compatibility-discovery"));
}
