// <copyright file="TelemetryRedactionConformanceSuiteTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 6.8A — validates operational-telemetry redaction. Drives the real telemetry surfaces through every
/// operational scenario and proves that the emitted metric dimensions and structured-log shapes exclude unsafe
/// values, that metric dimensions only ever carry closed-vocabulary class values plus bounded booleans plus the
/// bounded gate id, and that the sentinel <c>None</c> enum value is rejected.
/// </summary>
public sealed class TelemetryRedactionConformanceSuiteTest
{
    private static readonly TelemetryCaptureResult Capture = TelemetryRedactionConformanceSuite.Run();

    [Fact]
    public void RunShouldEmitAtLeastOneMeasurementPerCounter()
    {
        IReadOnlyList<string> counters =
        [
            TelemetryDisclosureConformanceFixtures.CommandRejectionsCounter,
            TelemetryDisclosureConformanceFixtures.TenantDenialsCounter,
            TelemetryDisclosureConformanceFixtures.PrivilegedAccessCounter,
            TelemetryDisclosureConformanceFixtures.ProjectionFreshnessCounter,
            TelemetryDisclosureConformanceFixtures.ProjectionRebuildCounter,
            TelemetryDisclosureConformanceFixtures.PublicationFailuresCounter,
            TelemetryDisclosureConformanceFixtures.ConformanceOutcomesCounter,
        ];

        foreach (string counter in counters)
        {
            Capture.Measurements.Any(m => m.InstrumentName == counter)
                .ShouldBeTrue($"Validation run produced no live measurement for counter '{counter}'.");
        }
    }

    [Fact]
    public void EveryMeasurementShouldCarryOnlyApprovedDimensionKeys()
    {
        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            IReadOnlyList<string> approvedKeys =
                TelemetryDisclosureConformanceFixtures.ApprovedDimensionKeys[measurement.InstrumentName];

            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                approvedKeys.ShouldContain(
                    tag.Key,
                    $"Surface '{measurement.InstrumentName}' emitted unapproved dimension key '{tag.Key}'.");
            }
        }
    }

    [Fact]
    public void NoMeasurementDimensionShouldEverCarryAForbiddenValue()
    {
        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                string value = tag.Value?.ToString() ?? string.Empty;
                foreach (ForbiddenValueFixture forbidden in TelemetryDisclosureConformanceFixtures.ForbiddenValues)
                {
                    value.ShouldNotContain(
                        forbidden.Value,
                        Case.Insensitive,
                        $"Surface '{measurement.InstrumentName}' dimension '{tag.Key}' leaked forbidden value class "
                        + $"'{forbidden.ValueClass}' (fixture '{forbidden.Value}').");
                }
            }
        }
    }

    [Fact]
    public void NoMeasurementDimensionShouldCarryRawIdentifierShapes()
    {
        // Raw conversation/Party/provider/file id shapes must never appear as dimension values. The dimensions
        // are closed-vocabulary tokens only, so none of these prefixes/markers may occur.
        string[] rawIdentifierMarkers =
        [
            "conv-",
            "conversation-",
            "tenant-acme",
            "party-",
            "principal-",
            "file-",
            "blob-",
            "bizrec-",
            "sk-live-",
            "@",
        ];

        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                string value = tag.Value?.ToString() ?? string.Empty;
                foreach (string marker in rawIdentifierMarkers)
                {
                    value.ShouldNotContain(
                        marker,
                        Case.Insensitive,
                        $"Surface '{measurement.InstrumentName}' dimension '{tag.Key}' carried raw identifier "
                        + $"marker '{marker}' (value '{value}').");
                }
            }
        }
    }

    [Fact]
    public void ClassDimensionsShouldOnlyCarryClosedVocabularyTokens()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> approvedByKey = BuildApprovedClassValues();

        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                if (!approvedByKey.TryGetValue(tag.Key, out IReadOnlyList<string>? approved))
                {
                    continue;
                }

                string value = tag.Value?.ToString() ?? string.Empty;
                approved.ShouldContain(
                    value,
                    $"Surface '{measurement.InstrumentName}' dimension '{tag.Key}' carried value '{value}' "
                    + "outside its closed vocabulary.");
            }
        }
    }

    [Fact]
    public void BooleanDimensionsShouldOnlyCarryBoundedTrueOrFalseTokens()
    {
        string[] booleanKeys = ["retryable", "blocking"];

        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                if (!booleanKeys.Contains(tag.Key))
                {
                    continue;
                }

                string value = tag.Value?.ToString() ?? string.Empty;
                TelemetryDisclosureConformanceFixtures.ApprovedBooleanValues.ShouldContain(
                    value,
                    $"Surface '{measurement.InstrumentName}' boolean dimension '{tag.Key}' carried '{value}'.");
            }
        }
    }

    [Fact]
    public void GateIdDimensionShouldOnlyCarryApprovedBoundedGateIds()
    {
        IEnumerable<CapturedMeasurement> conformanceMeasurements = Capture.Measurements
            .Where(m => m.InstrumentName == TelemetryDisclosureConformanceFixtures.ConformanceOutcomesCounter);

        conformanceMeasurements.ShouldNotBeEmpty();

        foreach (CapturedMeasurement measurement in conformanceMeasurements)
        {
            string? gateId = measurement.TagValue("gate_id");
            gateId.ShouldNotBeNull();
            TelemetryDisclosureConformanceFixtures.ApprovedGateIds.ShouldContain(
                gateId!,
                $"Conformance outcome emitted gate_id '{gateId}' outside the bounded approved set.");
        }
    }

    [Fact]
    public void GateIdShouldBeTheOnlyStringDimensionOutsideTheClassAndBooleanVocabularies()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> approvedClassValues = BuildApprovedClassValues();
        string[] booleanKeys = ["retryable", "blocking"];

        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                bool isClassDimension = approvedClassValues.ContainsKey(tag.Key);
                bool isBooleanDimension = booleanKeys.Contains(tag.Key);
                bool isGateId = tag.Key == "gate_id";

                (isClassDimension || isBooleanDimension || isGateId).ShouldBeTrue(
                    $"Surface '{measurement.InstrumentName}' emitted dimension '{tag.Key}', which is neither a "
                    + "closed-vocabulary class, a bounded boolean, nor the bounded gate_id.");
            }
        }
    }

    [Fact]
    public void NoStructuredLogMessageShouldEverCarryAForbiddenValue()
    {
        Capture.LogMessages.ShouldNotBeEmpty();

        foreach (string message in Capture.LogMessages)
        {
            foreach (ForbiddenValueFixture forbidden in TelemetryDisclosureConformanceFixtures.ForbiddenValues)
            {
                message.ShouldNotContain(
                    forbidden.Value,
                    Case.Insensitive,
                    $"Structured log leaked forbidden value class '{forbidden.ValueClass}': '{message}'.");
            }
        }
    }

    [Fact]
    public void StructuredLogMessagesShouldNotCarryTenantOrPartyOrConversationIdShapes()
    {
        string[] forbiddenLogMarkers =
        [
            "TenantId",
            "ConversationId",
            "PartyId",
            "tenant-acme",
            "party-",
            "conv-",
            "principal-",
        ];

        foreach (string message in Capture.LogMessages)
        {
            foreach (string marker in forbiddenLogMarkers)
            {
                message.ShouldNotContain(
                    marker,
                    Case.Sensitive,
                    $"Structured log carried forbidden identifier marker '{marker}': '{message}'.");
            }
        }
    }

    [Fact]
    public void EveryTelemetrySurfaceShouldRejectTheSentinelNoneValue()
    {
        IReadOnlyList<NoneGuardProbe> probes = TelemetryRedactionConformanceSuite.NoneGuardProbes();

        // Eight None guards exist: command rejection, tenant denial, privileged access, freshness, lag, rebuild,
        // publication, and conformance.
        probes.Count.ShouldBe(8);

        foreach (NoneGuardProbe probe in probes)
        {
            Should.Throw<ArgumentException>(
                probe.Invoke,
                $"Telemetry surface '{probe.Surface}' did not reject the sentinel None value.");
        }
    }

    [Fact]
    public void NoneSentinelGuardShouldPreventEmissionOfANoneDimensionValue()
    {
        // A None guard that throws BEFORE Counter.Add means no measurement can ever carry a 'none' class token.
        foreach (CapturedMeasurement measurement in Capture.Measurements)
        {
            foreach (KeyValuePair<string, object?> tag in measurement.Tags)
            {
                string value = tag.Value?.ToString() ?? string.Empty;
                value.ShouldNotBe(
                    "none",
                    $"Surface '{measurement.InstrumentName}' dimension '{tag.Key}' emitted the sentinel 'none'.");
            }
        }
    }

    [Fact]
    public void FixtureForbiddenValuesShouldCoverEveryRequiredDisclosureClass()
    {
        // Guard the fixture itself: the redaction proof is only meaningful if all required forbidden value
        // classes are present in the scan set.
        string[] requiredClasses =
        [
            "conversation-content",
            "user-free-text",
            "raw-business-record-id",
            "prompt-fragment",
            "unbounded-fault-string",
            "provider-payload",
            "redacted-content",
            "unauthorized-identifier",
            "cross-tenant-party-detail",
        ];

        IReadOnlyList<string> coveredClasses = TelemetryDisclosureConformanceFixtures.ForbiddenValues
            .Select(f => f.ValueClass)
            .ToArray();

        foreach (string required in requiredClasses)
        {
            coveredClasses.ShouldContain(required);
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildApprovedClassValues()
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
}
