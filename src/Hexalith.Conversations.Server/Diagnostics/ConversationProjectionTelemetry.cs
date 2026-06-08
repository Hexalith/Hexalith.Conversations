// <copyright file="ConversationProjectionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Commons.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits bounded-cardinality metrics and content-safe structured logs for projection freshness and publication failures.
/// </summary>
public sealed class ConversationProjectionTelemetry : IConversationProjectionTelemetry
{
    private readonly BoundedTelemetryCounter _freshnessCounter;
    private readonly ILogger<ConversationProjectionTelemetry> _logger;
    private readonly BoundedTelemetryCounter _publicationFailureCounter;
    private readonly BoundedTelemetryCounter _rebuildCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionTelemetry"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory registered by OpenTelemetry ServiceDefaults.</param>
    /// <param name="logger">The structured logger.</param>
    public ConversationProjectionTelemetry(IMeterFactory meterFactory, ILogger<ConversationProjectionTelemetry> logger)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        BoundedTelemetryMeter meter = new(meterFactory, ConversationTelemetryDefinitions.MeterName);
        _freshnessCounter = meter.CreateCounter(ConversationTelemetryDefinitions.ProjectionFreshness);
        _rebuildCounter = meter.CreateCounter(ConversationTelemetryDefinitions.ProjectionRebuild);
        _publicationFailureCounter = meter.CreateCounter(ConversationTelemetryDefinitions.PublicationFailures);
    }

    /// <inheritdoc />
    public void RecordProjectionFreshnessState(
        ConversationProjectionFreshnessClass freshnessClass,
        ConversationProjectionLagClass lagClass,
        string correlationId)
    {
        BoundedMetricDimension freshnessDimension = BoundedMetricDimension.EnumToken("freshness_class", freshnessClass, nameof(freshnessClass));
        BoundedMetricDimension lagDimension = BoundedMetricDimension.EnumToken("lag_class", lagClass, nameof(lagClass));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _freshnessCounter.AddOne(freshnessDimension, lagDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationProjectionFreshness: freshness={FreshnessClass} lag={LagClass} corr={CorrelationId}",
            freshnessClass,
            lagClass,
            correlationId);
    }

    /// <inheritdoc />
    public void RecordProjectionRebuildProgress(
        ConversationProjectionFreshnessClass rebuildClass,
        string correlationId)
    {
        BoundedMetricDimension rebuildDimension = BoundedMetricDimension.EnumToken("rebuild_class", rebuildClass, nameof(rebuildClass));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _rebuildCounter.AddOne(rebuildDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationProjectionRebuild: rebuild={RebuildClass} corr={CorrelationId}",
            rebuildClass,
            correlationId);
    }

    /// <inheritdoc />
    public void RecordPublicationFailure(
        ConversationPublicationFailureClass failureClass,
        string correlationId)
    {
        BoundedMetricDimension failureDimension = BoundedMetricDimension.EnumToken("failure_class", failureClass, nameof(failureClass));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _publicationFailureCounter.AddOne(failureDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationPublicationFailure: class={FailureClass} corr={CorrelationId}",
            failureClass,
            correlationId);
    }
}
