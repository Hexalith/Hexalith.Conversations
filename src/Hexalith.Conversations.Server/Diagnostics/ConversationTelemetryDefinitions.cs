// <copyright file="ConversationTelemetryDefinitions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.Diagnostics;

namespace Hexalith.Conversations.Server.Diagnostics;

internal static class ConversationTelemetryDefinitions
{
    public const string MeterName = "Hexalith.Conversations";

    public static readonly BoundedTelemetryCounterDefinition CommandRejections = new(
        "conversations.command.rejections",
        "Number of command rejections by bounded reason class",
        "rejection_class",
        "operation_class",
        "retryable");

    public static readonly BoundedTelemetryCounterDefinition TenantDenials = new(
        "conversations.tenant.denials",
        "Number of tenant isolation denials by bounded denial class",
        "denial_class",
        "operation_class",
        "retryable");

    public static readonly BoundedTelemetryCounterDefinition PrivilegedAccess = new(
        "conversations.privileged.access",
        "Number of privileged access attempts by access class",
        "access_class",
        "operation_class");

    public static readonly BoundedTelemetryCounterDefinition ProjectionFreshness = new(
        "conversations.projection.freshness",
        "Number of projection freshness state observations by class and lag class",
        "freshness_class",
        "lag_class");

    public static readonly BoundedTelemetryCounterDefinition ProjectionRebuild = new(
        "conversations.projection.rebuild",
        "Number of projection rebuild progress observations by rebuild class",
        "rebuild_class");

    public static readonly BoundedTelemetryCounterDefinition PublicationFailures = new(
        "conversations.publication.failures",
        "Number of publication failures by bounded failure class",
        "failure_class");

    public static readonly BoundedTelemetryCounterDefinition ConformanceOutcomes = new(
        "conversations.conformance.outcomes",
        "Number of conformance outcome observations by status class and gate",
        "status_class",
        "gate_id",
        "blocking");
}
