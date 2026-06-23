// <copyright file="ConversationConformanceTelemetryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies <see cref="ConversationConformanceTelemetry"/> emits bounded-cardinality metrics and content-safe logs.
/// </summary>
public sealed class ConversationConformanceTelemetryTest
{
    private const string CorrelationId = "corr-safe-conformance-789";
    private const string SafeGateId = "tenant-isolation";

    [Fact]
    public void RecordConformanceOutcome_PassClass_EmitsBoundedCounterWithCorrectDimensions()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.conformance.outcomes", meterFactory, captured);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.Pass,
            SafeGateId,
            false,
            CorrelationId);

        captured.Count.ShouldBe(1);
        MeasurementRecord<long> record = captured[0];
        record.Value.ShouldBe(1L);
        record.TagValue("status_class").ShouldBe("pass");
        record.TagValue("gate_id").ShouldBe(SafeGateId);
        record.TagValue("blocking").ShouldBe("false");
        record.Tags.Any(t => t.Key == "tenant_id" || t.Key == "conversation_id").ShouldBeFalse();
    }

    [Fact]
    public void RecordConformanceOutcome_FailClass_EmitsBlockingTrueDimension()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.conformance.outcomes", meterFactory, captured);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.Fail,
            SafeGateId,
            true,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("status_class").ShouldBe("fail");
        captured[0].TagValue("blocking").ShouldBe("true");
    }

    [Fact]
    public void RecordConformanceOutcome_WaivedClass_EmitsBlockingFalseDimension()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.conformance.outcomes", meterFactory, captured);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.Waived,
            "audit-integrity",
            false,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("status_class").ShouldBe("waived");
        captured[0].TagValue("blocking").ShouldBe("false");
    }

    [Fact]
    public void RecordConformanceOutcome_InfrastructureFailure_EmitsBoundedCounter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.conformance.outcomes", meterFactory, captured);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.InfrastructureFailure,
            SafeGateId,
            false,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("status_class").ShouldBe("infrastructurefailure");
        captured[0].TagValue("blocking").ShouldBe("false");
    }

    [Fact]
    public void RecordConformanceOutcome_StaleEvidence_EmitsBoundedCounter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.conformance.outcomes", meterFactory, captured);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.StaleEvidence,
            SafeGateId,
            false,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("status_class").ShouldBe("staleevidence");
        captured[0].TagValue("blocking").ShouldBe("false");
    }

    [Fact]
    public void RecordConformanceOutcome_LogMessageContainsOnlyBoundedFields_NoTenantOrConversationIds()
    {
        using FakeMeterFactory meterFactory = new();
        CapturingLogger<ConversationConformanceTelemetry> logger = new();
        ConversationConformanceTelemetry telemetry = new(meterFactory, logger);

        telemetry.RecordConformanceOutcome(
            ConversationConformanceStatusClass.Pass,
            "audit-integrity",
            false,
            CorrelationId);

        string message = logger.Messages.Single();
        message.ShouldContain("ConversationConformanceOutcome");
        message.ShouldContain("corr=");
        message.ShouldNotContain("TenantId");
        message.ShouldNotContain("ConversationId");
        message.ShouldNotContain("tenant-");
        message.ShouldNotContain("party:");
    }

    [Fact]
    public void RecordConformanceOutcome_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordConformanceOutcome(
                ConversationConformanceStatusClass.None,
                SafeGateId,
                false,
                CorrelationId));
    }

    [Fact]
    public void RecordConformanceOutcome_EmptyGateId_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordConformanceOutcome(
                ConversationConformanceStatusClass.Pass,
                string.Empty,
                false,
                CorrelationId));
    }

    [Fact]
    public void RecordConformanceOutcome_EmptyCorrelationId_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationConformanceTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationConformanceTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordConformanceOutcome(
                ConversationConformanceStatusClass.Pass,
                SafeGateId,
                false,
                string.Empty));
    }

    [Fact]
    public void AddConversationConformanceTelemetry_RegistersServiceCorrectly()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMetrics();
        services.AddConversationConformanceTelemetry();

        using ServiceProvider provider = services.BuildServiceProvider();
        IConversationConformanceTelemetry telemetry = provider.GetRequiredService<IConversationConformanceTelemetry>();
        telemetry.ShouldNotBeNull();
        telemetry.ShouldBeOfType<ConversationConformanceTelemetry>();
    }

    private static MeterListener StartListening<T>(
        string instrumentName,
        FakeMeterFactory meterFactory,
        List<MeasurementRecord<T>> captured)
        where T : struct
    {
        MeterListener listener = new();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == instrumentName && meterFactory.Owns(instrument.Meter))
            {
                l.EnableMeasurementEvents(instrument, null);
            }
        };
        listener.SetMeasurementEventCallback<T>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == instrumentName && meterFactory.Owns(instrument.Meter))
            {
                captured.Add(new MeasurementRecord<T>(measurement, tags.ToArray()));
            }
        });
        listener.Start();
        return listener;
    }

    private sealed record MeasurementRecord<T>(T Value, KeyValuePair<string, object?>[] Tags)
        where T : struct
    {
        public string? TagValue(string key)
            => Tags.FirstOrDefault(t => t.Key == key).Value?.ToString();
    }
}
