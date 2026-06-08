// <copyright file="ConversationTelemetryGuardsTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies the thin Conversations telemetry wrappers preserve their constructor guards, correlation-id
/// guards, gate-id disclosure safety, and the stable <c>Hexalith.Conversations</c> meter name after adopting
/// the shared bounded-telemetry helper.
/// </summary>
public sealed class ConversationTelemetryGuardsTest
{
    private const string CorrelationId = "corr-safe-guard-001";
    private const string MeterName = "Hexalith.Conversations";

    [Fact]
    public void Constructors_NullMeterFactoryOrLogger_ThrowArgumentNullException()
    {
        using FakeMeterFactory meterFactory = new();

        _ = Should.Throw<ArgumentNullException>(() =>
            new ConversationProjectionTelemetry(null!, NullLogger<ConversationProjectionTelemetry>.Instance));
        _ = Should.Throw<ArgumentNullException>(() =>
            new ConversationProjectionTelemetry(meterFactory, null!));

        _ = Should.Throw<ArgumentNullException>(() =>
            new ConversationRejectionTelemetry(null!, NullLogger<ConversationRejectionTelemetry>.Instance));
        _ = Should.Throw<ArgumentNullException>(() =>
            new ConversationRejectionTelemetry(meterFactory, null!));

        _ = Should.Throw<ArgumentNullException>(() =>
            new ConversationConformanceTelemetry(null!, NullLogger<ConversationConformanceTelemetry>.Instance));
        _ = Should.Throw<ArgumentNullException>(() =>
            new ConversationConformanceTelemetry(meterFactory, null!));
    }

    [Fact]
    public void ProjectionTelemetry_EmptyCorrelationId_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        _ = Should.Throw<ArgumentException>(() =>
            telemetry.RecordProjectionFreshnessState(
                ConversationProjectionFreshnessClass.Current,
                ConversationProjectionLagClass.WithinThreshold,
                string.Empty));
        _ = Should.Throw<ArgumentException>(() =>
            telemetry.RecordProjectionRebuildProgress(
                ConversationProjectionFreshnessClass.Rebuilding,
                string.Empty));
        _ = Should.Throw<ArgumentException>(() =>
            telemetry.RecordPublicationFailure(
                ConversationPublicationFailureClass.UnsupportedSchema,
                string.Empty));
    }

    [Fact]
    public void RejectionTelemetry_EmptyCorrelationId_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        _ = Should.Throw<ArgumentException>(() =>
            telemetry.RecordTenantDenial(
                ConversationTenantDenialClass.InsufficientAccess,
                ConversationTenantAccessRequirement.Read,
                isRetryable: false,
                correlationId: string.Empty));
        _ = Should.Throw<ArgumentException>(() =>
            telemetry.RecordPrivilegedAccessAttempt(
                ConversationPrivilegedAccessClass.AuthorizedPrivilegedOperation,
                ConversationTenantAccessRequirement.Admin,
                correlationId: string.Empty));
    }

    [Fact]
    public void ConformanceTelemetry_GateIdWithControlCharacter_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        _ = Should.Throw<ArgumentException>(() =>
            telemetry.RecordConformanceOutcome(
                ConversationConformanceStatusClass.Pass,
                "tenant\nisolation",
                isBlocking: false,
                CorrelationId));
    }

    [Fact]
    public void ProjectionFreshness_EmittedOnStableConversationsMeter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        List<string> meterNames = [];
        using MeterListener listener = StartListeningForMeterName("conversations.projection.freshness", meterNames);

        telemetry.RecordProjectionFreshnessState(
            ConversationProjectionFreshnessClass.Current,
            ConversationProjectionLagClass.WithinThreshold,
            CorrelationId);

        meterNames.Single().ShouldBe(MeterName);
    }

    [Fact]
    public void CommandRejection_EmittedOnStableConversationsMeter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        List<string> meterNames = [];
        using MeterListener listener = StartListeningForMeterName("conversations.command.rejections", meterNames);

        telemetry.RecordCommandRejection(
            ConversationCommandRejectionClass.Validation,
            ConversationTenantAccessRequirement.Write,
            isRetryable: false,
            CorrelationId);

        meterNames.Single().ShouldBe(MeterName);
    }

    [Fact]
    public void ConformanceOutcome_EmittedOnStableConversationsMeter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        List<string> meterNames = [];
        using MeterListener listener = StartListeningForMeterName("conversations.conformance.outcomes", meterNames);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.Pass,
            "tenant-isolation",
            isBlocking: false,
            CorrelationId);

        meterNames.Single().ShouldBe(MeterName);
    }

    private static MeterListener StartListeningForMeterName(string instrumentName, List<string> meterNames)
    {
        MeterListener listener = new();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == instrumentName)
            {
                l.EnableMeasurementEvents(instrument, null);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
        {
            if (instrument.Name == instrumentName)
            {
                meterNames.Add(instrument.Meter.Name);
            }
        });
        listener.Start();
        return listener;
    }
}
