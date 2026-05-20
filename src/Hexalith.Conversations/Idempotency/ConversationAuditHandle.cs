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

        // P48 review fix (2026-05-20): length-prefixed canonical encoding prevents newline/delimiter injection in any input
        // (TenantId, ScopeValue, IdempotencyKey, serverOperationId) from producing identical hash material across distinct
        // scope tuples. Mirrors the length-prefix discipline in ConversationPayloadFingerprint.FromParts.
        StringBuilder material = new();
        AppendPart(material, fingerprint.Scope.TenantId.Value);
        AppendPart(material, fingerprint.Scope.CommandType.Value);
        AppendPart(material, fingerprint.Scope.ScopeKind);
        AppendPart(material, fingerprint.Scope.ScopeValue);
        AppendPart(material, fingerprint.Scope.IdempotencyKey);
        AppendPart(material, fingerprint.Scope.SchemaVersion.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendPart(material, serverOperationId);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString()));
        return "audit-" + Convert.ToHexString(hash, 0, 16).ToLowerInvariant();
    }

    private static void AppendPart(StringBuilder material, string value)
    {
        material
            .Append(value.Length)
            .Append(':')
            .Append(value)
            .Append('\n');
    }
}
