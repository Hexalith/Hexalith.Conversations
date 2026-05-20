// <copyright file="ConversationAuditHandle.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Creates opaque audit handles from server-side command boundary data.
/// </summary>
public static class ConversationAuditHandle
{
    /// <summary>
    /// Creates a stable non-disclosing handle for a command evaluation boundary.
    /// </summary>
    /// <param name="fingerprint">The scoped command fingerprint.</param>
    /// <param name="serverOperationId">The server-generated operation or event identity.</param>
    /// <returns>An opaque audit handle safe for idempotency records and replay payloads.</returns>
    public static string FromServerBoundary(
        ConversationCommandFingerprint fingerprint,
        string serverOperationId)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverOperationId);

        string material = string.Join(
            '\n',
            fingerprint.Scope.TenantId.Value,
            fingerprint.Scope.CommandType.Value,
            fingerprint.Scope.ScopeKind,
            fingerprint.Scope.SchemaVersion.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            serverOperationId);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return "audit-" + Convert.ToHexString(hash, 0, 16).ToLowerInvariant();
    }
}
