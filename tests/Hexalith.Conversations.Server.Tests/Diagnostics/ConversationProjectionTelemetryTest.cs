// <copyright file="ConversationProjectionTelemetryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies <see cref="ConversationProjectionTelemetry"/> emits bounded-cardinality metrics and content-safe logs.
/// </summary>
public sealed class ConversationProjectionTelemetryTest
{
    private const string CorrelationId = "corr-safe-456";

    [Fact]
    public void RecordProjectionFreshnessState_CurrentWithinThreshold_EmitsBoundedCounterWithBothDimensions()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.projection.freshness", meterFactory, captured);

        telemetry.RecordProjectionFreshnessState(
            ConversationProjectionFreshnessClass.Current,
            ConversationProjectionLagClass.WithinThreshold,
            CorrelationId);

        captured.Count.ShouldBe(1);
        MeasurementRecord<long> record = captured[0];
        record.Value.ShouldBe(1L);
        record.TagValue("freshness_class").ShouldBe("current");
        record.TagValue("lag_class").ShouldBe("withinthreshold");
        record.Tags.Any(t => t.Key == "tenant_id" || t.Key == "conversation_id").ShouldBeFalse();
    }

    [Fact]
    public void RecordProjectionFreshnessState_Stale_EmitsBoundedCounterWithStaleClass()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.projection.freshness", meterFactory, captured);

        telemetry.RecordProjectionFreshnessState(
            ConversationProjectionFreshnessClass.Stale,
            ConversationProjectionLagClass.ThresholdBreached,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("freshness_class").ShouldBe("stale");
        captured[0].TagValue("lag_class").ShouldBe("thresholdbreached");
    }

    [Fact]
    public void RecordProjectionFreshnessState_Rebuilding_EmitsBoundedCounterWithRebuildingClass()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.projection.freshness", meterFactory, captured);

        telemetry.RecordProjectionFreshnessState(
            ConversationProjectionFreshnessClass.Rebuilding,
            ConversationProjectionLagClass.WithinThreshold,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("freshness_class").ShouldBe("rebuilding");
    }

    [Fact]
    public void RecordProjectionFreshnessState_LogMessageContainsOnlyBoundedFields_NoTenantOrConversationIds()
    {
        using FakeMeterFactory meterFactory = new();
        CapturingLogger<ConversationProjectionTelemetry> logger = new();
        ConversationProjectionTelemetry telemetry = new(meterFactory, logger);

        telemetry.RecordProjectionFreshnessState(
            ConversationProjectionFreshnessClass.Stale,
            ConversationProjectionLagClass.ThresholdBreached,
            CorrelationId);

        string message = logger.Messages.Single();
        message.ShouldContain("ConversationProjectionFreshness");
        message.ShouldContain("corr=");
        message.ShouldNotContain("TenantId");
        message.ShouldNotContain("ConversationId");
        message.ShouldNotContain("tenant-");
        message.ShouldNotContain("party:");
    }

    [Fact]
    public void RecordProjectionRebuildProgress_Rebuilding_EmitsBoundedCounter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.projection.rebuild", meterFactory, captured);

        telemetry.RecordProjectionRebuildProgress(
            ConversationProjectionFreshnessClass.Rebuilding,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("rebuild_class").ShouldBe("rebuilding");
    }

    [Fact]
    public void RecordProjectionRebuildProgress_LogMessageContainsStableEventName()
    {
        using FakeMeterFactory meterFactory = new();
        CapturingLogger<ConversationProjectionTelemetry> logger = new();
        ConversationProjectionTelemetry telemetry = new(meterFactory, logger);

        telemetry.RecordProjectionRebuildProgress(
            ConversationProjectionFreshnessClass.Rebuilding,
            CorrelationId);

        string message = logger.Messages.Single();
        message.ShouldContain("ConversationProjectionRebuild");
        message.ShouldContain("corr=");
        message.ShouldNotContain("TenantId");
        message.ShouldNotContain("ConversationId");
    }

    [Fact]
    public void RecordProjectionRebuildProgress_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordProjectionRebuildProgress(
                ConversationProjectionFreshnessClass.None,
                CorrelationId));
    }

    [Fact]
    public void RecordPublicationFailure_UnsupportedSchema_EmitsBoundedCounter()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.publication.failures", meterFactory, captured);

        telemetry.RecordPublicationFailure(
            ConversationPublicationFailureClass.UnsupportedSchema,
            CorrelationId);

        captured.Count.ShouldBe(1);
        captured[0].TagValue("failure_class").ShouldBe("unsupportedschema");
    }

    [Fact]
    public void RecordPublicationFailure_LogMessageContainsOnlyBoundedFields()
    {
        using FakeMeterFactory meterFactory = new();
        CapturingLogger<ConversationProjectionTelemetry> logger = new();
        ConversationProjectionTelemetry telemetry = new(meterFactory, logger);

        telemetry.RecordPublicationFailure(
            ConversationPublicationFailureClass.TenantViolation,
            CorrelationId);

        string message = logger.Messages.Single();
        message.ShouldContain("ConversationPublicationFailure");
        message.ShouldContain("corr=");
        message.ShouldNotContain("TenantId");
        message.ShouldNotContain("ConversationId");
    }

    [Fact]
    public void RecordPublicationFailure_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordPublicationFailure(
                ConversationPublicationFailureClass.None,
                CorrelationId));
    }

    [Fact]
    public void RecordProjectionFreshnessState_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordProjectionFreshnessState(
                ConversationProjectionFreshnessClass.None,
                ConversationProjectionLagClass.WithinThreshold,
                CorrelationId));
    }

    [Fact]
    public void RecordProjectionFreshnessState_NoneLagClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationProjectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationProjectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordProjectionFreshnessState(
                ConversationProjectionFreshnessClass.Current,
                ConversationProjectionLagClass.None,
                CorrelationId));
    }

    [Fact]
    public void AddConversationProjectionTelemetry_RegistersServiceCorrectly()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMetrics();
        services.AddConversationProjectionTelemetry();

        using ServiceProvider provider = services.BuildServiceProvider();
        IConversationProjectionTelemetry telemetry = provider.GetRequiredService<IConversationProjectionTelemetry>();
        telemetry.ShouldNotBeNull();
        telemetry.ShouldBeOfType<ConversationProjectionTelemetry>();
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
