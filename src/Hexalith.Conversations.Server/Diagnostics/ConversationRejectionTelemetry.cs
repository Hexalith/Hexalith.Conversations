// <copyright file="ConversationRejectionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Commons.Diagnostics;
using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits bounded-cardinality metrics and content-safe structured logs for command rejections, tenant denials, and privileged access attempts.
/// </summary>
public sealed class ConversationRejectionTelemetry : IConversationRejectionTelemetry
{
    private readonly BoundedTelemetryCounter _commandRejectionCounter;
    private readonly ILogger<ConversationRejectionTelemetry> _logger;
    private readonly BoundedTelemetryCounter _privilegedAccessCounter;
    private readonly BoundedTelemetryCounter _tenantDenialCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationRejectionTelemetry"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory registered by OpenTelemetry ServiceDefaults.</param>
    /// <param name="logger">The structured logger.</param>
    public ConversationRejectionTelemetry(IMeterFactory meterFactory, ILogger<ConversationRejectionTelemetry> logger)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        BoundedTelemetryMeter meter = new(meterFactory, ConversationTelemetryDefinitions.MeterName);
        _commandRejectionCounter = meter.CreateCounter(ConversationTelemetryDefinitions.CommandRejections);
        _tenantDenialCounter = meter.CreateCounter(ConversationTelemetryDefinitions.TenantDenials);
        _privilegedAccessCounter = meter.CreateCounter(ConversationTelemetryDefinitions.PrivilegedAccess);
    }

    /// <inheritdoc />
    public void RecordCommandRejection(
        ConversationCommandRejectionClass rejectionClass,
        ConversationTenantAccessRequirement operationClass,
        bool isRetryable,
        string correlationId)
    {
        BoundedMetricDimension rejectionDimension = BoundedMetricDimension.EnumToken("rejection_class", rejectionClass, nameof(rejectionClass));
        BoundedMetricDimension operationDimension = BoundedMetricDimension.EnumToken("operation_class", operationClass, nameof(operationClass));
        BoundedMetricDimension retryableDimension = BoundedMetricDimension.BooleanToken("retryable", isRetryable);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _commandRejectionCounter.AddOne(rejectionDimension, operationDimension, retryableDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationCommandRejected: class={RejectionClass} operation={OperationClass} retryable={IsRetryable} corr={CorrelationId}",
            rejectionClass,
            operationClass,
            isRetryable,
            correlationId);
    }

    /// <inheritdoc />
    public void RecordTenantDenial(
        ConversationTenantDenialClass denialClass,
        ConversationTenantAccessRequirement operationClass,
        bool isRetryable,
        string correlationId)
    {
        BoundedMetricDimension denialDimension = BoundedMetricDimension.EnumToken("denial_class", denialClass, nameof(denialClass));
        BoundedMetricDimension operationDimension = BoundedMetricDimension.EnumToken("operation_class", operationClass, nameof(operationClass));
        BoundedMetricDimension retryableDimension = BoundedMetricDimension.BooleanToken("retryable", isRetryable);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _tenantDenialCounter.AddOne(denialDimension, operationDimension, retryableDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationTenantDenied: class={DenialClass} operation={OperationClass} retryable={IsRetryable} corr={CorrelationId}",
            denialClass,
            operationClass,
            isRetryable,
            correlationId);
    }

    /// <inheritdoc />
    public void RecordPrivilegedAccessAttempt(
        ConversationPrivilegedAccessClass accessClass,
        ConversationTenantAccessRequirement operationClass,
        string correlationId)
    {
        BoundedMetricDimension accessDimension = BoundedMetricDimension.EnumToken("access_class", accessClass, nameof(accessClass));
        BoundedMetricDimension operationDimension = BoundedMetricDimension.EnumToken("operation_class", operationClass, nameof(operationClass));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _privilegedAccessCounter.AddOne(accessDimension, operationDimension);

        BoundedTelemetryLog.Information(
            _logger,
            "ConversationPrivilegedAccess: class={AccessClass} operation={OperationClass} corr={CorrelationId}",
            accessClass,
            operationClass,
            correlationId);
    }
}
