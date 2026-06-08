// <copyright file="ConversationConformanceTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Commons.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits bounded-cardinality metrics and content-safe structured logs for conformance outcome observations.
/// </summary>
public sealed class ConversationConformanceTelemetry : IConversationConformanceTelemetry
{
    private readonly BoundedTelemetryCounter _conformanceCounter;
    private readonly ILogger<ConversationConformanceTelemetry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationConformanceTelemetry"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory registered by OpenTelemetry ServiceDefaults.</param>
    /// <param name="logger">The structured logger.</param>
    public ConversationConformanceTelemetry(IMeterFactory meterFactory, ILogger<ConversationConformanceTelemetry> logger)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        BoundedTelemetryMeter meter = new(meterFactory, ConversationTelemetryDefinitions.MeterName);
        _conformanceCounter = meter.CreateCounter(ConversationTelemetryDefinitions.ConformanceOutcomes);
    }

    /// <inheritdoc />
    public void RecordConformanceOutcome(
        ConversationConformanceStatusClass statusClass,
        string safeGateId,
        bool isBlocking,
        string correlationId)
    {
        BoundedMetricDimension statusDimension = BoundedMetricDimension.EnumToken("status_class", statusClass, nameof(statusClass));
        BoundedMetricDimension gateDimension = BoundedMetricDimension.SafeToken("gate_id", safeGateId, nameof(safeGateId));
        BoundedMetricDimension blockingDimension = BoundedMetricDimension.BooleanToken("blocking", isBlocking);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId, nameof(correlationId));

        _conformanceCounter.AddOne(statusDimension, gateDimension, blockingDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationConformanceOutcome: status={StatusClass} gate={GateId} blocking={IsBlocking} corr={CorrelationId}",
            statusClass,
            safeGateId,
            isBlocking,
            correlationId);
    }
}
