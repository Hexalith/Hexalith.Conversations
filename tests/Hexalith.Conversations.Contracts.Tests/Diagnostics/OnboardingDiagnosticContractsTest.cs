// <copyright file="OnboardingDiagnosticContractsTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Diagnostics;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies CORE onboarding diagnostic and precondition contracts remain closed, structured, and content safe.
/// </summary>
public sealed class OnboardingDiagnosticContractsTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly Uri Documentation = new("https://docs.hexalith.local/conversations/contracts/v1/preconditions");

    [Fact]
    public void DiagnosticVocabulariesShouldBeExplicitAndBounded()
    {
        OnboardingDiagnosticCheck.All.Select(check => check.Value).ShouldBe(
            [
                "tenant-context",
                "contract-version",
                "provider-configuration",
                "projection-subscription",
                "schema-compatibility",
                "audit-availability",
                "parties-integration",
            ],
            ignoreOrder: false);

        OnboardingDiagnosticStatus.All.Select(status => status.Value).ShouldBe(
            [
                "ready",
                "degraded",
                "blocked",
                "unknown",
            ],
            ignoreOrder: false);
    }

    [Theory]
    [InlineData("\"tenant_context\"", typeof(OnboardingDiagnosticCheck))]
    [InlineData("\"raw-stream\"", typeof(OnboardingDiagnosticCheck))]
    [InlineData("\"ok\"", typeof(OnboardingDiagnosticStatus))]
    [InlineData("\"healthy\"", typeof(OnboardingDiagnosticStatus))]
    [InlineData("\"maybe\"", typeof(OnboardingDiagnosticStatus))]
    public void ClosedVocabularyJsonShouldRejectUnsupportedValues(string json, Type targetType)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize(json, targetType, WebOptions));

    [Fact]
    public void ReadyCheckMustNotCarryErrorAndNonReadyCheckMustCarryError()
    {
        Should.Throw<ArgumentException>(() => new OnboardingDiagnosticCheckResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.TenantContext,
            OnboardingDiagnosticStatus.Ready,
            "Tenant context is current.",
            "none",
            Documentation,
            ["AC2"],
            ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantBindingMissing, "correlation-001")));

        Should.Throw<ArgumentException>(() => new OnboardingDiagnosticCheckResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.AuditAvailability,
            OnboardingDiagnosticStatus.Blocked,
            "Audit recording is not available.",
            "retry-after-audit-available",
            Documentation,
            ["AC2"]));
    }

    [Fact]
    public void DiagnosticDocumentationPointersMustUseHttps()
        => Should.Throw<ArgumentException>(() => new OnboardingDiagnosticCheckResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.TenantContext,
            OnboardingDiagnosticStatus.Ready,
            "Tenant context is current.",
            "none",
            new Uri("http://docs.hexalith.local/conversations"),
            ["AC2"]));

    [Theory]
    [InlineData("raw upstream payload")]
    [InlineData("EventStore stream name")]
    [InlineData("provider payload")]
    [InlineData("tenant:tenant-001")]
    [InlineData("conversation-hidden")]
    [InlineData("NullReferenceException at boundary")]
    [InlineData("C:\\private\\path")]
    public void DiagnosticSafeTextShouldRejectDisclosureTerms(string unsafeValue)
        => Should.Throw<ArgumentException>(() => new OnboardingDiagnosticCheckResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.TenantContext,
            OnboardingDiagnosticStatus.Blocked,
            unsafeValue,
            "retry-later",
            Documentation,
            ["AC2"],
            ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantBindingMissing, "correlation-001")));

    [Fact]
    public void RunResultShouldKeepStableCamelCaseJsonShape()
    {
        OnboardingDiagnosticRunResultV1 result = new(
            SchemaVersion.Current,
            OnboardingDiagnosticStatus.Ready,
            "All CORE preconditions are ready.",
            "correlation-001",
            new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero),
            [
                new OnboardingDiagnosticCheckResultV1(
                    SchemaVersion.Current,
                    OnboardingDiagnosticCheck.TenantContext,
                    OnboardingDiagnosticStatus.Ready,
                    "Tenant context is current and access is allowed.",
                    "none",
                    Documentation,
                    ["AC2", "AC3"]),
            ]);

        AssertJsonEquivalent(
            """
            {"schemaVersion":1,"overallStatus":"ready","safeSummary":"All CORE preconditions are ready.","correlationId":"correlation-001","generatedAtUtc":"2026-05-23T12:00:00+00:00","checks":[{"schemaVersion":1,"check":"tenant-context","status":"ready","safeMessage":"Tenant context is current and access is allowed.","remediationGuidanceCode":"none","documentation":"https://docs.hexalith.local/conversations/contracts/v1/preconditions","requirementMappings":["AC2","AC3"],"error":null,"auditHandle":null}]}
            """,
            result);
    }

    [Fact]
    public void RunResultShouldRoundTripAdditiveJsonTolerantly()
    {
        OnboardingDiagnosticRunResultV1 result = new(
            SchemaVersion.Current,
            OnboardingDiagnosticStatus.Degraded,
            "Some CORE preconditions are degraded; review per-check remediation.",
            "correlation-001",
            new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero),
            [
                new OnboardingDiagnosticCheckResultV1(
                    SchemaVersion.Current,
                    OnboardingDiagnosticCheck.ProjectionSubscription,
                    OnboardingDiagnosticStatus.Degraded,
                    "Projection subscription is not current; retry after it is refreshed.",
                    "retry-after-projection-current",
                    Documentation,
                    ["AC2", "AC3"],
                    ConversationErrorCatalog.CreateError(ConversationErrorCode.TenantProjectionStale, "correlation-001")),
            ]);

        string json = JsonSerializer.Serialize(result, WebOptions);
        JsonObject augmented = JsonNode.Parse(json)!.AsObject();
        augmented["unknownAdditiveField"] = "ignored";

        OnboardingDiagnosticRunResultV1? roundTripped =
            JsonSerializer.Deserialize<OnboardingDiagnosticRunResultV1>(augmented.ToJsonString(), WebOptions);

        roundTripped.ShouldNotBeNull();
        roundTripped.OverallStatus.ShouldBe(OnboardingDiagnosticStatus.Degraded);
        roundTripped.Checks.Single().Error!.Code.ShouldBe(ConversationErrorCode.TenantProjectionStale);
    }

    [Fact]
    public void CorePreconditionCatalogShouldDocumentEveryRequiredPreconditionWithSafeFailureBehavior()
    {
        string[] expectedPreconditionIds =
        [
            "projection-freshness",
            "audit-sink-availability",
            "supported-schema-versions",
            "contract-compatibility",
            "participant-identity-validation",
            "idempotency-key-behavior",
            "projection-subscription-health",
            "required-configuration",
        ];

        ConversationCorePreconditionCatalog.All.Select(precondition => precondition.PreconditionId)
            .ShouldBe(expectedPreconditionIds, ignoreOrder: true);

        foreach (CorePreconditionV1 precondition in ConversationCorePreconditionCatalog.All)
        {
            // Only Current is trust-bearing; the catalog must not invent diagnostic-only synonyms.
            precondition.RequiredTrustState.ShouldBe(ProjectionTrustState.Current);
            precondition.SafeFailureBehavior.ShouldNotBeNullOrWhiteSpace();
            precondition.Documentation.Scheme.ShouldBe(Uri.UriSchemeHttps);

            // Every unmet error code must resolve in the shared catalog (no parallel error taxonomy).
            ConversationErrorCatalog.Get(precondition.UnmetErrorCode).ShouldNotBeNull();
        }
    }

    [Fact]
    public void DiagnosticFreeTextShouldNotLeakProtectedFragments()
    {
        // The closed-vocabulary check/status/precondition tokens are safe machine identifiers; this scan
        // targets the free-text disclosure surface (safe failure behavior) for protected-value leakage.
        string[] forbidden =
        [
            "EventStore", "envelope", "SignalR", "tenant:", "party:", "conv:",
            "provider-session", "provider payload", "provider response", "business reference",
            "Exception", "C:\\", "D:\\",
        ];

        foreach (CorePreconditionV1 precondition in ConversationCorePreconditionCatalog.All)
        {
            foreach (string fragment in forbidden)
            {
                precondition.SafeFailureBehavior.ShouldNotContain(fragment, Case.Insensitive);
            }
        }
    }

    [Fact]
    public void DiagnosticVocabularyParseShouldRoundTripEveryKnownValue()
    {
        foreach (OnboardingDiagnosticCheck check in OnboardingDiagnosticCheck.All)
        {
            OnboardingDiagnosticCheck.Parse(check.Value).ShouldBe(check);
        }

        foreach (OnboardingDiagnosticStatus status in OnboardingDiagnosticStatus.All)
        {
            OnboardingDiagnosticStatus.Parse(status.Value).ShouldBe(status);
        }
    }

    [Theory]
    [InlineData("tenant_context")]
    [InlineData("TENANT-CONTEXT")]
    [InlineData("unknown-check")]
    [InlineData("")]
    [InlineData("   ")]
    public void DiagnosticCheckParseShouldRejectUnsupportedValues(string value)
        => Should.Throw<ArgumentException>(() => OnboardingDiagnosticCheck.Parse(value));

    [Theory]
    [InlineData("ok")]
    [InlineData("healthy")]
    [InlineData("Ready")]
    [InlineData("")]
    public void DiagnosticStatusParseShouldRejectUnsupportedValues(string value)
        => Should.Throw<ArgumentException>(() => OnboardingDiagnosticStatus.Parse(value));

    [Fact]
    public void CheckResultShouldRejectUnsafeAuditHandleToken()
        => Should.Throw<ArgumentException>(() => new OnboardingDiagnosticCheckResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.AuditAvailability,
            OnboardingDiagnosticStatus.Degraded,
            "Audit recording is not available.",
            "retry-after-audit-available",
            Documentation,
            ["AC2"],
            ConversationErrorCatalog.CreateError(ConversationErrorCode.AuditSinkUnavailable, "correlation-001"),
            "audit handle with spaces"));

    [Fact]
    public void CheckResultShouldRequireAtLeastOneRequirementMapping()
        => Should.Throw<ArgumentException>(() => new OnboardingDiagnosticCheckResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.TenantContext,
            OnboardingDiagnosticStatus.Ready,
            "Tenant context is current.",
            "none",
            Documentation,
            []));

    [Fact]
    public void RunResultShouldRejectAtLeastOneCheck()
        => Should.Throw<ArgumentException>(() => new OnboardingDiagnosticRunResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticStatus.Ready,
            "All CORE preconditions are ready.",
            "correlation-001",
            new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero),
            []));

    [Theory]
    [InlineData(3)] // Non-zero UTC offset is rejected.
    public void RunResultShouldRejectNonUtcTimestamp(int offsetHours)
        => Should.Throw<ArgumentOutOfRangeException>(() => new OnboardingDiagnosticRunResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticStatus.Ready,
            "All CORE preconditions are ready.",
            "correlation-001",
            new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.FromHours(offsetHours)),
            [ReadyTenantCheck()]));

    [Fact]
    public void RunResultShouldRejectImplausibleTimestamp()
        => Should.Throw<ArgumentOutOfRangeException>(() => new OnboardingDiagnosticRunResultV1(
            SchemaVersion.Current,
            OnboardingDiagnosticStatus.Ready,
            "All CORE preconditions are ready.",
            "correlation-001",
            new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero),
            [ReadyTenantCheck()]));

    [Fact]
    public void CorePreconditionShouldRejectNonHttpsDocumentation()
        => Should.Throw<ArgumentException>(() => new CorePreconditionV1(
            SchemaVersion.Current,
            "projection-freshness",
            OnboardingDiagnosticCheck.TenantContext,
            ProjectionTrustState.Current,
            "Reads degrade and writes fail closed when stale.",
            ConversationErrorCode.TenantProjectionStale,
            new Uri("http://docs.hexalith.local/conversations")));

    [Fact]
    public void CorePreconditionShouldRejectUnsafeFailureBehaviorText()
        => Should.Throw<ArgumentException>(() => new CorePreconditionV1(
            SchemaVersion.Current,
            "projection-freshness",
            OnboardingDiagnosticCheck.TenantContext,
            ProjectionTrustState.Current,
            "EventStore stream snapshot leaked into safe failure text.",
            ConversationErrorCode.TenantProjectionStale,
            Documentation));

    [Fact]
    public void CorePreconditionCatalogGetShouldReturnDescriptorForKnownIdAndRejectUnknownId()
    {
        ConversationCorePreconditionCatalog.Get("projection-freshness").PreconditionId.ShouldBe("projection-freshness");
        Should.Throw<ArgumentException>(() => ConversationCorePreconditionCatalog.Get("not-a-precondition"));
    }

    private static OnboardingDiagnosticCheckResultV1 ReadyTenantCheck()
        => new(
            SchemaVersion.Current,
            OnboardingDiagnosticCheck.TenantContext,
            OnboardingDiagnosticStatus.Ready,
            "Tenant context is current and access is allowed.",
            "none",
            Documentation,
            ["AC2", "AC3"]);

    private static void AssertJsonEquivalent(string expected, object value)
    {
        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(value, value.GetType(), WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(value, value.GetType(), WebOptions));
    }
}
