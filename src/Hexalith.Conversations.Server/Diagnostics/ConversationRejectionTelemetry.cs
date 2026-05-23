// <copyright file="ConversationRejectionTelemetry.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Emits bounded-cardinality metrics and content-safe structured logs for command rejections, tenant denials, and privileged access attempts.
/// </summary>
public sealed class ConversationRejectionTelemetry : IConversationRejectionTelemetry
{
    private readonly Counter<long> _commandRejectionCounter;
    private readonly ILogger<ConversationRejectionTelemetry> _logger;
    private readonly Counter<long> _privilegedAccessCounter;
    private readonly Counter<long> _tenantDenialCounter;

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

        Meter meter = meterFactory.Create("Hexalith.Conversations");
        _commandRejectionCounter = meter.CreateCounter<long>(
            "conversations.command.rejections",
            description: "Number of command rejections by bounded reason class");
        _tenantDenialCounter = meter.CreateCounter<long>(
            "conversations.tenant.denials",
            description: "Number of tenant isolation denials by bounded denial class");
        _privilegedAccessCounter = meter.CreateCounter<long>(
            "conversations.privileged.access",
            description: "Number of privileged access attempts by access class");
    }

    /// <inheritdoc />
    public void RecordCommandRejection(
        ConversationCommandRejectionClass rejectionClass,
        ConversationTenantAccessRequirement operationClass,
        bool isRetryable,
        string correlationId)
    {
        if (rejectionClass == ConversationCommandRejectionClass.None)
        {
            throw new ArgumentException("None is not a valid rejection class for telemetry signals.", nameof(rejectionClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _commandRejectionCounter.Add(
            1,
            new KeyValuePair<string, object?>("rejection_class", rejectionClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("operation_class", operationClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("retryable", isRetryable ? "true" : "false"));

        _logger.LogInformation(
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
        if (denialClass == ConversationTenantDenialClass.None)
        {
            throw new ArgumentException("None is not a valid denial class for telemetry signals.", nameof(denialClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _tenantDenialCounter.Add(
            1,
            new KeyValuePair<string, object?>("denial_class", denialClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("operation_class", operationClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("retryable", isRetryable ? "true" : "false"));

        _logger.LogInformation(
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
        if (accessClass == ConversationPrivilegedAccessClass.None)
        {
            throw new ArgumentException("None is not a valid access class for telemetry signals.", nameof(accessClass));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        _privilegedAccessCounter.Add(
            1,
            new KeyValuePair<string, object?>("access_class", accessClass.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("operation_class", operationClass.ToString().ToLowerInvariant()));

        _logger.LogInformation(
            "ConversationPrivilegedAccess: class={AccessClass} operation={OperationClass} corr={CorrelationId}",
            accessClass,
            operationClass,
            correlationId);
    }
}
