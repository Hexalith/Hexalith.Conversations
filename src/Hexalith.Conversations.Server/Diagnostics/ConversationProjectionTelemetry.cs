// <copyright file="ConversationProjectionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits bounded-cardinality metrics and content-safe structured logs for projection freshness and publication failures.
/// </summary>
public sealed class ConversationProjectionTelemetry : IConversationProjectionTelemetry
{
    private readonly Counter<long> _freshnessCounter;
    private readonly ILogger<ConversationProjectionTelemetry> _logger;
    private readonly Counter<long> _publicationFailureCounter;
    private readonly Counter<long> _rebuildCounter;

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

        Meter meter = meterFactory.Create("Hexalith.Conversations");
        _freshnessCounter = meter.CreateCounter<long>(
            "conversations.projection.freshness",
            description: "Number of projection freshness state observations by class and lag class");
        _rebuildCounter = meter.CreateCounter<long>(
            "conversations.projection.rebuild",
            description: "Number of projection rebuild progress observations by rebuild class");
        _publicationFailureCounter = meter.CreateCounter<long>(
            "conversations.publication.failures",
            description: "Number of publication failures by bounded failure class");
    }

    /// <inheritdoc />
    public void RecordProjectionFreshnessState(
        ConversationProjectionFreshnessClass freshnessClass,
        ConversationProjectionLagClass lagClass,
        string correlationId)
    {
        if (freshnessClass == ConversationProjectionFreshnessClass.None)
        {
            throw new ArgumentException("None is not a valid freshness class for telemetry signals.", nameof(freshnessClass));
        }

        if (lagClass == ConversationProjectionLagClass.None)
        {
            throw new ArgumentException("None is not a valid lag class for telemetry signals.", nameof(lagClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _freshnessCounter.Add(
            1,
            new KeyValuePair<string, object?>("freshness_class", freshnessClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("lag_class", lagClass.ToString().ToLowerInvariant()));

        _logger.LogInformation(
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
        if (rebuildClass == ConversationProjectionFreshnessClass.None)
        {
            throw new ArgumentException("None is not a valid rebuild class for telemetry signals.", nameof(rebuildClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _rebuildCounter.Add(
            1,
            new KeyValuePair<string, object?>("rebuild_class", rebuildClass.ToString().ToLowerInvariant()));

        _logger.LogInformation(
            "ConversationProjectionRebuild: rebuild={RebuildClass} corr={CorrelationId}",
            rebuildClass,
            correlationId);
    }

    /// <inheritdoc />
    public void RecordPublicationFailure(
        ConversationPublicationFailureClass failureClass,
        string correlationId)
    {
        if (failureClass == ConversationPublicationFailureClass.None)
        {
            throw new ArgumentException("None is not a valid failure class for telemetry signals.", nameof(failureClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _publicationFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("failure_class", failureClass.ToString().ToLowerInvariant()));

        _logger.LogInformation(
            "ConversationPublicationFailure: class={FailureClass} corr={CorrelationId}",
            failureClass,
            correlationId);
    }
}
