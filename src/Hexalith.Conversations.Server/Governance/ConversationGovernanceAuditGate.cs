// <copyright file="ConversationGovernanceAuditGate.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Enforces fail-closed handling around governance audit recording.
/// </summary>
internal static class ConversationGovernanceAuditGate
{
    public static async ValueTask<ConversationGovernanceAuditResult> RecordRequiredAsync(
        Func<CancellationToken, ValueTask<ConversationGovernanceAuditResult>> recordAuditAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recordAuditAsync);

        try
        {
            return await recordAuditAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return ConversationGovernanceAuditResult.AuditUnavailable();
        }
    }
}
