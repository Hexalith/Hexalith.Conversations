// <copyright file="TelemetryRedactionConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Drives the real <see cref="ConversationRejectionTelemetry"/>, <see cref="ConversationProjectionTelemetry"/>,
/// and <see cref="ConversationConformanceTelemetry"/> implementations through the operational scenario list,
/// capturing every live metric emission (via <see cref="MeterListener"/>) and structured log message so that
/// the validation suite can prove the emitted dimensions and logs exclude unsafe values.
/// </summary>
/// <remarks>
/// The suite is read-only against production state: it constructs the telemetry classes with a test
/// <see cref="IMeterFactory"/> and capturing loggers, then records signals for each scenario. It performs no
/// aggregate command dispatch, event appends, projection writes, governance mutations, or external calls.
/// Forbidden values are supplied ONLY as the typed safe inputs the APIs accept (the correlation-id parameter,
/// which is bound for ILogger but never emitted as a metric tag); the suite then proves they are absent from
/// every captured dimension value and every log message.
/// </remarks>
public sealed class TelemetryRedactionConformanceSuite
{
    /// <summary>
    /// Runs every operational scenario against the real telemetry surfaces and returns the captured signals.
    /// </summary>
    /// <returns>The captured metric measurements and log messages per telemetry surface.</returns>
    public TelemetryCaptureResult Run()
    {
        using FakeMeterFactory meterFactory = new();

        CapturingLogger<ConversationRejectionTelemetry> rejectionLogger = new();
        CapturingLogger<ConversationProjectionTelemetry> projectionLogger = new();
        CapturingLogger<ConversationConformanceTelemetry> conformanceLogger = new();

        ConversationRejectionTelemetry rejection = new(meterFactory, rejectionLogger);
        ConversationProjectionTelemetry projection = new(meterFactory, projectionLogger);
        ConversationConformanceTelemetry conformance = new(meterFactory, conformanceLogger);

        List<CapturedMeasurement> measurements = new();
        using MeterListener listener = StartListening("Hexalith.Conversations", measurements);

        foreach (TelemetryValidationScenario scenario in TelemetryDisclosureConformanceFixtures.Scenarios)
        {
            ExerciseScenario(scenario, rejection, projection, conformance);
        }

        List<string> logMessages = new();
        logMessages.AddRange(rejectionLogger.Messages);
        logMessages.AddRange(projectionLogger.Messages);
        logMessages.AddRange(conformanceLogger.Messages);

        return new TelemetryCaptureResult(measurements, logMessages);
    }

    /// <summary>
    /// Records the sentinel <c>None</c> guard behaviour for every telemetry method that defines one.
    /// </summary>
    /// <returns>The set of guard probes, each carrying the action that must throw.</returns>
    public static IReadOnlyList<NoneGuardProbe> NoneGuardProbes()
    {
        FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry rejection = new(meterFactory, NullLogger<ConversationRejectionTelemetry>.Instance);
        ConversationProjectionTelemetry projection = new(meterFactory, NullLogger<ConversationProjectionTelemetry>.Instance);
        ConversationConformanceTelemetry conformance = new(meterFactory, NullLogger<ConversationConformanceTelemetry>.Instance);

        const string corr = TelemetryDisclosureConformanceFixtures.SafeCorrelationId;
        const string gate = "tenant-isolation";

        return
        [
            new("command-rejection-none", () => rejection.RecordCommandRejection(
                ConversationCommandRejectionClass.None,
                ConversationTenantAccessRequirement.Write,
                false,
                corr)),
            new("tenant-denial-none", () => rejection.RecordTenantDenial(
                ConversationTenantDenialClass.None,
                ConversationTenantAccessRequirement.Read,
                false,
                corr)),
            new("privileged-access-none", () => rejection.RecordPrivilegedAccessAttempt(
                ConversationPrivilegedAccessClass.None,
                ConversationTenantAccessRequirement.Admin,
                corr)),
            new("freshness-none", () => projection.RecordProjectionFreshnessState(
                ConversationProjectionFreshnessClass.None,
                ConversationProjectionLagClass.WithinThreshold,
                corr)),
            new("lag-none", () => projection.RecordProjectionFreshnessState(
                ConversationProjectionFreshnessClass.Current,
                ConversationProjectionLagClass.None,
                corr)),
            new("rebuild-none", () => projection.RecordProjectionRebuildProgress(
                ConversationProjectionFreshnessClass.None,
                corr)),
            new("publication-none", () => projection.RecordPublicationFailure(
                ConversationPublicationFailureClass.None,
                corr)),
            new("conformance-none", () => conformance.RecordConformanceOutcome(
                ConversationConformanceStatusClass.None,
                gate,
                false,
                corr)),
        ];
    }

    private static void ExerciseScenario(
        TelemetryValidationScenario scenario,
        ConversationRejectionTelemetry rejection,
        ConversationProjectionTelemetry projection,
        ConversationConformanceTelemetry conformance)
    {
        // Each scenario passes a forbidden-value-laden correlation id (the only free-text-shaped parameter the
        // APIs accept) so the suite can prove redaction: the forbidden content must never reach a metric tag.
        // The bounded enum/boolean/gate arguments come exclusively from the closed vocabularies.
        const string corr = TelemetryDisclosureConformanceFixtures.SafeCorrelationId;

        switch (scenario)
        {
            case TelemetryValidationScenario.NormalOperations:
                rejection.RecordPrivilegedAccessAttempt(
                    ConversationPrivilegedAccessClass.AuthorizedPrivilegedOperation,
                    ConversationTenantAccessRequirement.Read,
                    corr);
                projection.RecordProjectionFreshnessState(
                    ConversationProjectionFreshnessClass.Current,
                    ConversationProjectionLagClass.WithinThreshold,
                    corr);
                conformance.RecordConformanceOutcome(
                    ConversationConformanceStatusClass.Pass,
                    "tenant-isolation",
                    false,
                    corr);
                break;

            case TelemetryValidationScenario.RedactionEvent:
                conformance.RecordConformanceOutcome(
                    ConversationConformanceStatusClass.Pass,
                    "redaction-non-leakage",
                    false,
                    corr);
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.PolicyRejection,
                    ConversationTenantAccessRequirement.Governance,
                    false,
                    corr);
                break;

            case TelemetryValidationScenario.CrossTenantDenial:
                rejection.RecordTenantDenial(
                    ConversationTenantDenialClass.InsufficientAccess,
                    ConversationTenantAccessRequirement.Read,
                    false,
                    corr);
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.TenantIsolation,
                    ConversationTenantAccessRequirement.Write,
                    false,
                    corr);
                projection.RecordPublicationFailure(
                    ConversationPublicationFailureClass.TenantViolation,
                    corr);
                break;

            case TelemetryValidationScenario.ProviderFault:
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.Infrastructure,
                    ConversationTenantAccessRequirement.Write,
                    true,
                    corr);
                projection.RecordPublicationFailure(
                    ConversationPublicationFailureClass.TransientFailure,
                    corr);
                conformance.RecordConformanceOutcome(
                    ConversationConformanceStatusClass.InfrastructureFailure,
                    "provider-portability",
                    false,
                    corr);
                break;

            case TelemetryValidationScenario.MalformedMetadata:
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.Validation,
                    ConversationTenantAccessRequirement.Write,
                    false,
                    corr);
                rejection.RecordTenantDenial(
                    ConversationTenantDenialClass.ContextMismatch,
                    ConversationTenantAccessRequirement.Write,
                    false,
                    corr);
                break;

            case TelemetryValidationScenario.PrivilegedAccess:
                rejection.RecordPrivilegedAccessAttempt(
                    ConversationPrivilegedAccessClass.UnauthorizedPrivilegedAttempt,
                    ConversationTenantAccessRequirement.Admin,
                    corr);
                rejection.RecordPrivilegedAccessAttempt(
                    ConversationPrivilegedAccessClass.AuthorizedPrivilegedOperation,
                    ConversationTenantAccessRequirement.Governance,
                    corr);
                break;

            case TelemetryValidationScenario.StaleProjection:
                projection.RecordProjectionFreshnessState(
                    ConversationProjectionFreshnessClass.Stale,
                    ConversationProjectionLagClass.ThresholdBreached,
                    corr);
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.TenantProjectionUnavailable,
                    ConversationTenantAccessRequirement.Read,
                    true,
                    corr);
                break;

            case TelemetryValidationScenario.AuditUnavailable:
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.AuditUnavailable,
                    ConversationTenantAccessRequirement.Governance,
                    true,
                    corr);
                conformance.RecordConformanceOutcome(
                    ConversationConformanceStatusClass.InfrastructureFailure,
                    "audit-integrity",
                    false,
                    corr);
                break;

            case TelemetryValidationScenario.DuplicateCommand:
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.Idempotency,
                    ConversationTenantAccessRequirement.Write,
                    false,
                    corr);
                break;

            case TelemetryValidationScenario.ProjectionLag:
                projection.RecordProjectionFreshnessState(
                    ConversationProjectionFreshnessClass.Current,
                    ConversationProjectionLagClass.CriticalLag,
                    corr);
                break;

            case TelemetryValidationScenario.RebuildState:
                projection.RecordProjectionRebuildProgress(
                    ConversationProjectionFreshnessClass.Rebuilding,
                    corr);
                projection.RecordProjectionRebuildProgress(
                    ConversationProjectionFreshnessClass.PartiallyRebuilt,
                    corr);
                break;

            case TelemetryValidationScenario.SubscriberFailure:
                projection.RecordPublicationFailure(
                    ConversationPublicationFailureClass.UnsupportedSchema,
                    corr);
                projection.RecordPublicationFailure(
                    ConversationPublicationFailureClass.DeadLettered,
                    corr);
                projection.RecordPublicationFailure(
                    ConversationPublicationFailureClass.ReplayRequired,
                    corr);
                break;

            case TelemetryValidationScenario.ConfigurationGap:
                conformance.RecordConformanceOutcome(
                    ConversationConformanceStatusClass.ExecutionFailure,
                    "contract-compatibility",
                    false,
                    corr);
                rejection.RecordCommandRejection(
                    ConversationCommandRejectionClass.PolicyRejection,
                    ConversationTenantAccessRequirement.Admin,
                    false,
                    corr);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unhandled validation scenario.");
        }
    }

    private static MeterListener StartListening(string meterName, List<CapturedMeasurement> captured)
    {
        MeterListener listener = new();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                l.EnableMeasurementEvents(instrument, null);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                captured.Add(new CapturedMeasurement(instrument.Name, measurement, tags.ToArray()));
            }
        });
        listener.Start();
        return listener;
    }
}

/// <summary>
/// Carries a single live metric measurement captured from a real telemetry surface.
/// </summary>
/// <param name="InstrumentName">The emitting counter name.</param>
/// <param name="Value">The measurement value.</param>
/// <param name="Tags">The captured metric dimensions.</param>
public sealed record CapturedMeasurement(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags)
{
    /// <summary>Gets the string value of a captured tag, or <see langword="null"/> when absent.</summary>
    /// <param name="key">The dimension key.</param>
    /// <returns>The dimension value as a string.</returns>
    public string? TagValue(string key)
        => Tags.FirstOrDefault(tag => tag.Key == key).Value?.ToString();
}

/// <summary>
/// Aggregates the captured metric measurements and structured log messages produced by a validation run.
/// </summary>
/// <param name="Measurements">Every captured metric measurement across all surfaces.</param>
/// <param name="LogMessages">Every captured structured log message across all surfaces.</param>
public sealed record TelemetryCaptureResult(
    IReadOnlyList<CapturedMeasurement> Measurements,
    IReadOnlyList<string> LogMessages);

/// <summary>
/// Carries a single sentinel <c>None</c> guard probe whose action must throw <see cref="ArgumentException"/>.
/// </summary>
/// <param name="Surface">The bounded machine name of the probed telemetry surface.</param>
/// <param name="Invoke">The action that supplies the sentinel and must throw.</param>
public sealed record NoneGuardProbe(string Surface, Action Invoke);
