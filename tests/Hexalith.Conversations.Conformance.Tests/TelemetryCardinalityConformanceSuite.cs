// <copyright file="TelemetryCardinalityConformanceSuite.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Drives the real telemetry surfaces under many high-cardinality invocations across every operational
/// scenario, capturing live metric emissions so the cardinality validation suite can prove that the distinct
/// observed tag-value set per dimension never exceeds the approved closed vocabulary, and that <c>gate_id</c>
/// stays within the bounded approved set.
/// </summary>
/// <remarks>
/// This harness deliberately invokes the telemetry methods with every closed-vocabulary class value many times
/// (and across many correlation ids, which are never emitted as dimensions) to stress the cardinality of the
/// emitted dimensions. It is read-only against production state — no command dispatch, no event appends, no
/// projection writes, no governance mutation, no external calls.
/// </remarks>
public sealed class TelemetryCardinalityConformanceSuite
{
    /// <summary>The number of high-cardinality iterations applied per closed-vocabulary value.</summary>
    public const int HighCardinalityIterations = 50;

    /// <summary>
    /// Runs every closed-vocabulary value many times across all surfaces and returns the captured signals.
    /// </summary>
    /// <returns>The captured metric measurements grouped under all instruments.</returns>
    public static IReadOnlyList<CapturedMeasurement> RunHighCardinalityLoad()
    {
        using FakeMeterFactory meterFactory = new();

        ConversationRejectionTelemetry rejection = new(
            meterFactory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ConversationRejectionTelemetry>.Instance);
        ConversationProjectionTelemetry projection = new(
            meterFactory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ConversationProjectionTelemetry>.Instance);
        ConversationConformanceTelemetry conformance = new(
            meterFactory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ConversationConformanceTelemetry>.Instance);

        List<CapturedMeasurement> measurements = new();
        using MeterListener listener = StartListening("Hexalith.Conversations", measurements);

        ConversationCommandRejectionClass[] rejectionClasses = NonNoneValues(ConversationCommandRejectionClass.None);
        ConversationTenantDenialClass[] denialClasses = NonNoneValues(ConversationTenantDenialClass.None);
        ConversationPrivilegedAccessClass[] accessClasses = NonNoneValues(ConversationPrivilegedAccessClass.None);
        ConversationProjectionFreshnessClass[] freshnessClasses = NonNoneValues(ConversationProjectionFreshnessClass.None);
        ConversationProjectionLagClass[] lagClasses = NonNoneValues(ConversationProjectionLagClass.None);
        ConversationPublicationFailureClass[] failureClasses = NonNoneValues(ConversationPublicationFailureClass.None);
        ConversationConformanceStatusClass[] statusClasses = NonNoneValues(ConversationConformanceStatusClass.None);
        ConversationTenantAccessRequirement[] operationClasses = Enum.GetValues<ConversationTenantAccessRequirement>();

        // Many iterations, varying a per-call unique correlation id (never a metric dimension) and cycling
        // every closed-vocabulary value, to prove the emitted dimension-value set stays bounded.
        for (int iteration = 0; iteration < HighCardinalityIterations; iteration++)
        {
            string corr = $"corr-load-{iteration:D4}";
            bool retryable = iteration % 2 == 0;
            ConversationTenantAccessRequirement op = operationClasses[iteration % operationClasses.Length];

            foreach (ConversationCommandRejectionClass rejectionClass in rejectionClasses)
            {
                rejection.RecordCommandRejection(rejectionClass, op, retryable, corr);
            }

            foreach (ConversationTenantDenialClass denialClass in denialClasses)
            {
                rejection.RecordTenantDenial(denialClass, op, retryable, corr);
            }

            foreach (ConversationPrivilegedAccessClass accessClass in accessClasses)
            {
                rejection.RecordPrivilegedAccessAttempt(accessClass, op, corr);
            }

            foreach (ConversationProjectionFreshnessClass freshnessClass in freshnessClasses)
            {
                foreach (ConversationProjectionLagClass lagClass in lagClasses)
                {
                    projection.RecordProjectionFreshnessState(freshnessClass, lagClass, corr);
                }

                projection.RecordProjectionRebuildProgress(freshnessClass, corr);
            }

            foreach (ConversationPublicationFailureClass failureClass in failureClasses)
            {
                projection.RecordPublicationFailure(failureClass, corr);
            }

            foreach (ConversationConformanceStatusClass statusClass in statusClasses)
            {
                // gate_id cycles through the bounded approved set only — never a raw id.
                string gate = TelemetryDisclosureConformanceFixtures.ApprovedGateIds[
                    iteration % TelemetryDisclosureConformanceFixtures.ApprovedGateIds.Count];
                conformance.RecordConformanceOutcome(statusClass, gate, retryable, corr);
            }
        }

        return measurements;
    }

    /// <summary>
    /// Evaluates the cardinality gate that must reject an unbounded/raw <c>gate_id</c> value.
    /// </summary>
    /// <param name="candidateGateId">The gate id to test against the bounded approved vocabulary.</param>
    /// <returns><see langword="true"/> when the candidate is approved; otherwise <see langword="false"/>.</returns>
    public static bool IsGateIdWithinApprovedBudget(string candidateGateId)
        => TelemetryDisclosureConformanceFixtures.ApprovedGateIds.Contains(candidateGateId, StringComparer.Ordinal);

    private static TEnum[] NonNoneValues<TEnum>(TEnum none)
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().Where(value => !value.Equals(none)).ToArray();

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
