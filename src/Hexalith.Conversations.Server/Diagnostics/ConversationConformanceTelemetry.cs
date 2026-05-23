// <copyright file="ConversationConformanceTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits bounded-cardinality metrics and content-safe structured logs for conformance outcome observations.
/// </summary>
public sealed class ConversationConformanceTelemetry : IConversationConformanceTelemetry
{
    private readonly Counter<long> _conformanceCounter;
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

        Meter meter = meterFactory.Create("Hexalith.Conversations");
        _conformanceCounter = meter.CreateCounter<long>(
            "conversations.conformance.outcomes",
            description: "Number of conformance outcome observations by status class and gate");
    }

    /// <inheritdoc />
    public void RecordConformanceOutcome(
        ConversationConformanceStatusClass statusClass,
        string safeGateId,
        bool isBlocking,
        string correlationId)
    {
        if (statusClass == ConversationConformanceStatusClass.None)
        {
            throw new ArgumentException("None is not a valid status class for telemetry signals.", nameof(statusClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(safeGateId, nameof(safeGateId));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId, nameof(correlationId));

        _conformanceCounter.Add(
            1,
            new KeyValuePair<string, object?>("status_class", statusClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("gate_id", safeGateId),
            new KeyValuePair<string, object?>("blocking", isBlocking ? "true" : "false"));

        _logger.LogInformation(
            "ConversationConformanceOutcome: status={StatusClass} gate={GateId} blocking={IsBlocking} corr={CorrelationId}",
            statusClass,
            safeGateId,
            isBlocking,
            correlationId);
    }
}
