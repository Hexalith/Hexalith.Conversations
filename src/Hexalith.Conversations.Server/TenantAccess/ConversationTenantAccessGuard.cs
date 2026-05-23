// <copyright file="ConversationTenantAccessGuard.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Diagnostics;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// Runs protected conversation operations only after tenant access is allowed.
/// </summary>
public static class ConversationTenantAccessGuard
{
    /// <summary>
    /// Checks tenant access, then invokes the protected operation only when allowed.
    /// </summary>
    /// <typeparam name="TResult">The guarded operation result type.</typeparam>
    /// <param name="accessService">The tenant access service.</param>
    /// <param name="requirement">The access requirement.</param>
    /// <param name="trustedTenantId">The trusted request tenant binding.</param>
    /// <param name="callerPrincipalId">The caller principal id.</param>
    /// <param name="deniedResult">Maps a denied decision to the caller's result type.</param>
    /// <param name="protectedOperation">The protected operation to invoke only after allow.</param>
    /// <param name="routeTenantId">The route tenant binding when present.</param>
    /// <param name="commandTenantId">The command body tenant binding when present.</param>
    /// <param name="aggregateTenantId">The aggregate tenant binding when available.</param>
    /// <param name="projectionTenantId">The projection key tenant binding when available.</param>
    /// <param name="idempotencyTenantId">The idempotency tenant binding when available.</param>
    /// <param name="telemetry">The optional rejection telemetry — when supplied, emits bounded signals on denial.</param>
    /// <param name="correlationId">The safe correlation identifier for telemetry — required when <paramref name="telemetry"/> is supplied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The denied result or the protected operation result.</returns>
    public static async ValueTask<TResult> RunAsync<TResult>(
        IConversationTenantAccessService accessService,
        ConversationTenantAccessRequirement requirement,
        TenantId? trustedTenantId,
        string? callerPrincipalId,
        Func<ConversationTenantAccessDecision, TResult> deniedResult,
        Func<CancellationToken, ValueTask<TResult>> protectedOperation,
        TenantId? routeTenantId = null,
        TenantId? commandTenantId = null,
        TenantId? aggregateTenantId = null,
        TenantId? projectionTenantId = null,
        TenantId? idempotencyTenantId = null,
        IConversationRejectionTelemetry? telemetry = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accessService);
        ArgumentNullException.ThrowIfNull(deniedResult);
        ArgumentNullException.ThrowIfNull(protectedOperation);

        ConversationTenantAccessDecision decision = await accessService
            .CheckAccessAsync(
                requirement,
                trustedTenantId,
                callerPrincipalId,
                routeTenantId,
                commandTenantId,
                aggregateTenantId,
                projectionTenantId,
                idempotencyTenantId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            if (telemetry is not null && !string.IsNullOrWhiteSpace(correlationId))
            {
                ConversationTenantDenialClass denialClass =
                    ConversationCommandRejectionClassifier.Classify(decision.DenialReason);

                if (denialClass != ConversationTenantDenialClass.None)
                {
                    telemetry.RecordTenantDenial(denialClass, decision.Requirement, decision.IsRetryable, correlationId);
                }

                if (decision.DenialReason is ConversationTenantAccessDenialReason.MissingTenant
                    or ConversationTenantAccessDenialReason.MalformedTenant)
                {
                    telemetry.RecordCommandRejection(
                        ConversationCommandRejectionClass.TenantBinding,
                        decision.Requirement,
                        decision.IsRetryable,
                        correlationId);
                }
            }

            return deniedResult(decision);
        }

        // Honor late cancellation between the access decision and the protected operation
        // so an abandoned request cannot trigger downstream infrastructure work.
        cancellationToken.ThrowIfCancellationRequested();

        return await protectedOperation(cancellationToken).ConfigureAwait(false);
    }
}
