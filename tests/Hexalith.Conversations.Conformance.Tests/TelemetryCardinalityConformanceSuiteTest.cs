// <copyright file="TelemetryCardinalityConformanceSuiteTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 6.8B — validates operational-telemetry cardinality gates. Proves every telemetry dimension is bounded
/// and approved: each closed-vocabulary enum has a small fixed member count (a cardinality budget), metric tag
/// KEYS are a fixed approved set per counter, the distinct tag-value set stays within the approved closed
/// vocabulary under high-cardinality invocation, and <c>gate_id</c> is the only string dimension and is
/// constrained to a bounded approved set.
/// </summary>
public sealed class TelemetryCardinalityConformanceSuiteTest
{
    private static readonly IReadOnlyList<CapturedMeasurement> Load =
        new TelemetryCardinalityConformanceSuite().RunHighCardinalityLoad();

    // ---- Cardinality budgets: each closed-vocabulary enum member count is small and fixed. ----

    [Fact]
    public void CommandRejectionClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationCommandRejectionClass>().Length.ShouldBe(9);

    [Fact]
    public void TenantDenialClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationTenantDenialClass>().Length.ShouldBe(6);

    [Fact]
    public void PrivilegedAccessClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationPrivilegedAccessClass>().Length.ShouldBe(3);

    [Fact]
    public void ProjectionFreshnessClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationProjectionFreshnessClass>().Length.ShouldBe(6);

    [Fact]
    public void ProjectionLagClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationProjectionLagClass>().Length.ShouldBe(5);

    [Fact]
    public void PublicationFailureClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationPublicationFailureClass>().Length.ShouldBe(6);

    [Fact]
    public void ConformanceStatusClassShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationConformanceStatusClass>().Length.ShouldBe(8);

    [Fact]
    public void TenantAccessRequirementShouldHaveFixedCardinalityBudget()
        => Enum.GetValues<ConversationTenantAccessRequirement>().Length.ShouldBe(4);

    [Fact]
    public void EveryClosedVocabularyEnumShouldStayWithinASmallBudgetCeiling()
    {
        // No telemetry dimension enum may grow into an unbounded set. A conservative ceiling guards against a
        // future change that would silently inflate metric cardinality.
        const int budgetCeiling = 16;

        Enum.GetValues<ConversationCommandRejectionClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationTenantDenialClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationPrivilegedAccessClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationProjectionFreshnessClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationProjectionLagClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationPublicationFailureClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationConformanceStatusClass>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
        Enum.GetValues<ConversationTenantAccessRequirement>().Length.ShouldBeLessThanOrEqualTo(budgetCeiling);
    }

    [Fact]
    public void ApprovedGateIdVocabularyShouldBeBoundedAndSmall()
    {
        TelemetryDisclosureConformanceFixtures.ApprovedGateIds.Count.ShouldBe(8);
        TelemetryDisclosureConformanceFixtures.ApprovedGateIds.Distinct().Count()
            .ShouldBe(TelemetryDisclosureConformanceFixtures.ApprovedGateIds.Count);
    }

    // ---- Tag KEY sets are a fixed approved set per counter. ----

    [Fact]
    public void EachCounterShouldOnlyEverEmitItsApprovedDimensionKeySet()
    {
        IEnumerable<IGrouping<string, CapturedMeasurement>> byCounter =
            Load.GroupBy(m => m.InstrumentName);

        foreach (IGrouping<string, CapturedMeasurement> counter in byCounter)
        {
            IReadOnlyList<string> approvedKeys =
                TelemetryDisclosureConformanceFixtures.ApprovedDimensionKeys[counter.Key];

            HashSet<string> observedKeys = counter
                .SelectMany(m => m.Tags.Select(tag => tag.Key))
                .ToHashSet(StringComparer.Ordinal);

            // The observed key set must equal (not merely be a subset of) the approved set — every approved
            // dimension is exercised, and no unapproved dimension ever appears.
            observedKeys.SetEquals(approvedKeys).ShouldBeTrue(
                $"Counter '{counter.Key}' observed dimension keys [{string.Join(", ", observedKeys.Order())}] "
                + $"do not match the approved set [{string.Join(", ", approvedKeys.Order())}].");
        }
    }

    [Fact]
    public void EveryApprovedCounterShouldBeExercisedUnderLoad()
    {
        IReadOnlyList<string> expectedCounters =
            TelemetryDisclosureConformanceFixtures.ApprovedDimensionKeys.Keys.ToArray();

        HashSet<string> observedCounters = Load.Select(m => m.InstrumentName).ToHashSet(StringComparer.Ordinal);

        foreach (string counter in expectedCounters)
        {
            observedCounters.ShouldContain(counter, $"Counter '{counter}' was never exercised under load.");
        }
    }

    // ---- Distinct emitted tag-value sets stay within the approved closed vocabulary under load. ----

    [Fact]
    public void DistinctDimensionValuesUnderLoadShouldStayWithinTheApprovedClosedVocabulary()
    {
        Dictionary<string, IReadOnlyList<string>> approvedByKey = BuildApprovedValuesByKey();

        foreach (string key in approvedByKey.Keys)
        {
            HashSet<string> observedValues = Load
                .SelectMany(m => m.Tags)
                .Where(tag => tag.Key == key)
                .Select(tag => tag.Value?.ToString() ?? string.Empty)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string value in observedValues)
            {
                approvedByKey[key].ShouldContain(
                    value,
                    $"Dimension '{key}' emitted value '{value}' outside the approved closed vocabulary under "
                    + "high-cardinality load.");
            }
        }
    }

    [Fact]
    public void DistinctDimensionValueCountPerKeyShouldNotExceedItsCardinalityBudget()
    {
        // Despite HighCardinalityIterations * many class permutations, the distinct value count for each
        // dimension is capped by the closed vocabulary size — proving raw ids / free text never inflate it.
        Dictionary<string, IReadOnlyList<string>> approvedByKey = BuildApprovedValuesByKey();

        foreach (string key in approvedByKey.Keys)
        {
            int distinctCount = Load
                .SelectMany(m => m.Tags)
                .Where(tag => tag.Key == key)
                .Select(tag => tag.Value?.ToString() ?? string.Empty)
                .Distinct(StringComparer.Ordinal)
                .Count();

            distinctCount.ShouldBeLessThanOrEqualTo(
                approvedByKey[key].Count,
                $"Dimension '{key}' produced {distinctCount} distinct values, exceeding its budget of "
                + $"{approvedByKey[key].Count}.");
        }
    }

    [Fact]
    public void HighCardinalityLoadShouldProduceManyMeasurementsButFewDistinctTagValues()
    {
        // Sanity: the load is genuinely high-volume (proving the cardinality bound is meaningful, not vacuous).
        Load.Count.ShouldBeGreaterThan(1000);

        int totalDistinctValues = Load
            .SelectMany(m => m.Tags)
            .Select(tag => $"{tag.Key}={tag.Value}")
            .Distinct(StringComparer.Ordinal)
            .Count();

        // Across thousands of emissions, the entire distinct (key=value) space stays tiny and bounded.
        totalDistinctValues.ShouldBeLessThanOrEqualTo(64);
    }

    // ---- gate_id is the only string dimension; it is constrained to a bounded approved set. ----

    [Fact]
    public void GateIdShouldBeTheOnlyDimensionCarryingValuesOutsideClassAndBooleanVocabularies()
    {
        Dictionary<string, IReadOnlyList<string>> classValues = BuildApprovedClassValuesByKey();
        string[] booleanKeys = ["retryable", "blocking"];

        HashSet<string> stringDimensionKeys = new(StringComparer.Ordinal);

        foreach (CapturedMeasurement measurement in Load)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                bool isClass = classValues.ContainsKey(tag.Key);
                bool isBoolean = booleanKeys.Contains(tag.Key);
                if (!isClass && !isBoolean)
                {
                    stringDimensionKeys.Add(tag.Key);
                }
            }
        }

        // The only dimension that is not a closed-vocabulary class and not a bounded boolean is gate_id.
        stringDimensionKeys.SetEquals(["gate_id"]).ShouldBeTrue(
            $"Unexpected free-string dimension(s) present: [{string.Join(", ", stringDimensionKeys.Order())}]. "
            + "gate_id must be the only such dimension.");
    }

    [Fact]
    public void EveryEmittedGateIdUnderLoadShouldBeWithinTheBoundedApprovedSet()
    {
        IEnumerable<string> emittedGateIds = Load
            .Where(m => m.InstrumentName == TelemetryDisclosureConformanceFixtures.ConformanceOutcomesCounter)
            .Select(m => m.TagValue("gate_id") ?? string.Empty);

        emittedGateIds.ShouldNotBeEmpty();

        foreach (string gateId in emittedGateIds.Distinct(StringComparer.Ordinal))
        {
            TelemetryCardinalityConformanceSuite.IsGateIdWithinApprovedBudget(gateId).ShouldBeTrue(
                $"Emitted gate_id '{gateId}' is outside the bounded approved set.");
        }
    }

    [Fact]
    public void CardinalityGateShouldRejectUnboundedOrRawGateIdValues()
    {
        // The gate is what protects gate_id from becoming an unbounded dimension. Prove it catches raw values.
        string[] unboundedOrRawCandidates =
        [
            "conv-7f3a-9911",
            "tenant-acme-prod",
            "party-7711",
            "raw-business-record-0099",
            "free text gate label",
            "gate-" + Guid.NewGuid().ToString("N"),
            string.Empty,
        ];

        foreach (string candidate in unboundedOrRawCandidates)
        {
            TelemetryCardinalityConformanceSuite.IsGateIdWithinApprovedBudget(candidate).ShouldBeFalse(
                $"Cardinality gate failed to reject unbounded/raw gate_id candidate '{candidate}'.");
        }
    }

    [Fact]
    public void CardinalityGateShouldAcceptEveryApprovedGateId()
    {
        foreach (string approved in TelemetryDisclosureConformanceFixtures.ApprovedGateIds)
        {
            TelemetryCardinalityConformanceSuite.IsGateIdWithinApprovedBudget(approved).ShouldBeTrue(
                $"Cardinality gate wrongly rejected approved gate_id '{approved}'.");
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildApprovedClassValuesByKey()
        => new(StringComparer.Ordinal)
        {
            ["rejection_class"] = TelemetryDisclosureConformanceFixtures.ExpectedRejectionClassTokens,
            ["denial_class"] = TelemetryDisclosureConformanceFixtures.ExpectedDenialClassTokens,
            ["access_class"] = TelemetryDisclosureConformanceFixtures.ExpectedAccessClassTokens,
            ["freshness_class"] = TelemetryDisclosureConformanceFixtures.ExpectedFreshnessClassTokens,
            ["lag_class"] = TelemetryDisclosureConformanceFixtures.ExpectedLagClassTokens,
            ["rebuild_class"] = TelemetryDisclosureConformanceFixtures.ExpectedFreshnessClassTokens,
            ["failure_class"] = TelemetryDisclosureConformanceFixtures.ExpectedPublicationFailureClassTokens,
            ["status_class"] = TelemetryDisclosureConformanceFixtures.ExpectedConformanceStatusClassTokens,
            ["operation_class"] = TelemetryDisclosureConformanceFixtures.ExpectedOperationClassTokens,
        };

    private static Dictionary<string, IReadOnlyList<string>> BuildApprovedValuesByKey()
    {
        Dictionary<string, IReadOnlyList<string>> values = BuildApprovedClassValuesByKey();
        values["retryable"] = TelemetryDisclosureConformanceFixtures.ApprovedBooleanValues;
        values["blocking"] = TelemetryDisclosureConformanceFixtures.ApprovedBooleanValues;
        values["gate_id"] = TelemetryDisclosureConformanceFixtures.ApprovedGateIds;
        return values;
    }
}
