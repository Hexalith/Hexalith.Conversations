// <copyright file="ConversationRejectionTelemetryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

/// <summary>
/// Verifies <see cref="ConversationRejectionTelemetry"/> emits bounded-cardinality metrics and content-safe logs.
/// </summary>
public sealed class ConversationRejectionTelemetryTest
{
    private const string CorrelationId = "corr-safe-123";

    [Fact]
    public void RecordCommandRejection_EmitsCounterWithBoundedDimensions_NoConversationIdDimension()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.command.rejections", captured);

        telemetry.RecordCommandRejection(
            ConversationCommandRejectionClass.Validation,
            ConversationTenantAccessRequirement.Write,
            isRetryable: false,
            CorrelationId);

        captured.Count.ShouldBe(1);
        MeasurementRecord<long> record = captured[0];
        record.Value.ShouldBe(1L);
        record.TagValue("rejection_class").ShouldBe("validation");
        record.TagValue("operation_class").ShouldBe("write");
        record.TagValue("retryable").ShouldBe("false");
        record.Tags.Any(t => t.Key == "conversation_id" || t.Key == "tenant_id" || t.Key == "party_id")
            .ShouldBeFalse();
    }

    [Fact]
    public void RecordCommandRejection_LogMessageContainsOnlyBoundedFields_NoTenantOrPartyIds()
    {
        using FakeMeterFactory meterFactory = new();
        CapturingLogger<ConversationRejectionTelemetry> logger = new();
        ConversationRejectionTelemetry telemetry = new(meterFactory, logger);

        telemetry.RecordCommandRejection(
            ConversationCommandRejectionClass.Idempotency,
            ConversationTenantAccessRequirement.Write,
            isRetryable: false,
            CorrelationId);

        string message = logger.Messages.Single();
        message.ShouldContain("ConversationCommandRejected");
        message.ShouldContain("corr=");
        message.ShouldNotContain("tenant-");
        message.ShouldNotContain("party:");
        message.ShouldNotContain("conv:");
        message.ShouldNotContain("TenantId");
        message.ShouldNotContain("PartyId");
    }

    [Fact]
    public void RecordTenantDenial_EmitsCounterWithBoundedDimensions_NoTargetTenantValue()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.tenant.denials", captured);

        telemetry.RecordTenantDenial(
            ConversationTenantDenialClass.InsufficientAccess,
            ConversationTenantAccessRequirement.Governance,
            isRetryable: false,
            CorrelationId);

        captured.Count.ShouldBe(1);
        MeasurementRecord<long> record = captured[0];
        record.Value.ShouldBe(1L);
        record.TagValue("denial_class").ShouldBe("insufficientaccess");
        record.TagValue("operation_class").ShouldBe("governance");
        record.Tags.Any(t => t.Key == "tenant_id" || t.Key == "target_tenant").ShouldBeFalse();
    }

    [Fact]
    public void RecordTenantDenial_LogMessageContainsOnlyBoundedFields_NoCrosstenantData()
    {
        using FakeMeterFactory meterFactory = new();
        CapturingLogger<ConversationRejectionTelemetry> logger = new();
        ConversationRejectionTelemetry telemetry = new(meterFactory, logger);

        telemetry.RecordTenantDenial(
            ConversationTenantDenialClass.UnknownOrDisabled,
            ConversationTenantAccessRequirement.Read,
            isRetryable: false,
            CorrelationId);

        string message = logger.Messages.Single();
        message.ShouldContain("ConversationTenantDenied");
        message.ShouldContain("corr=");
        message.ShouldNotContain("tenant-");
        message.ShouldNotContain("party:");
        message.ShouldNotContain("TenantId");
    }

    [Fact]
    public void RecordPrivilegedAccessAttempt_EmitsCounterWithBoundedDimensions()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        List<MeasurementRecord<long>> captured = new();
        using MeterListener listener = StartListening<long>("conversations.privileged.access", captured);

        telemetry.RecordPrivilegedAccessAttempt(
            ConversationPrivilegedAccessClass.AuthorizedPrivilegedOperation,
            ConversationTenantAccessRequirement.Admin,
            CorrelationId);

        captured.Count.ShouldBe(1);
        MeasurementRecord<long> record = captured[0];
        record.Value.ShouldBe(1L);
        record.TagValue("access_class").ShouldBe("authorizedprivilegedoperation");
        record.TagValue("operation_class").ShouldBe("admin");
    }

    [Fact]
    public void RecordCommandRejection_NullOrEmptyCorrelationId_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordCommandRejection(
                ConversationCommandRejectionClass.Validation,
                ConversationTenantAccessRequirement.Write,
                isRetryable: false,
                correlationId: string.Empty));
    }

    [Fact]
    public void RecordCommandRejection_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordCommandRejection(
                ConversationCommandRejectionClass.None,
                ConversationTenantAccessRequirement.Write,
                isRetryable: false,
                CorrelationId));
    }

    [Fact]
    public void RecordTenantDenial_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordTenantDenial(
                ConversationTenantDenialClass.None,
                ConversationTenantAccessRequirement.Read,
                isRetryable: false,
                CorrelationId));
    }

    [Fact]
    public void RecordPrivilegedAccessAttempt_NoneClass_ThrowsArgumentException()
    {
        using FakeMeterFactory meterFactory = new();
        ConversationRejectionTelemetry telemetry = new(
            meterFactory,
            NullLogger<ConversationRejectionTelemetry>.Instance);

        Should.Throw<ArgumentException>(() =>
            telemetry.RecordPrivilegedAccessAttempt(
                ConversationPrivilegedAccessClass.None,
                ConversationTenantAccessRequirement.Admin,
                CorrelationId));
    }

    [Fact]
    public void AddConversationRejectionTelemetry_RegistersServiceCorrectly()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMetrics();
        services.AddConversationRejectionTelemetry();

        using ServiceProvider provider = services.BuildServiceProvider();
        IConversationRejectionTelemetry telemetry = provider.GetRequiredService<IConversationRejectionTelemetry>();
        telemetry.ShouldNotBeNull();
        telemetry.ShouldBeOfType<ConversationRejectionTelemetry>();
    }

    private static MeterListener StartListening<T>(
        string instrumentName,
        List<MeasurementRecord<T>> captured)
        where T : struct
    {
        MeterListener listener = new();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == instrumentName)
            {
                l.EnableMeasurementEvents(instrument, null);
            }
        };
        listener.SetMeasurementEventCallback<T>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == instrumentName)
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

    private sealed class FakeMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            Meter meter = new(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (Meter meter in _meters)
            {
                meter.Dispose();
            }

            _meters.Clear();
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
