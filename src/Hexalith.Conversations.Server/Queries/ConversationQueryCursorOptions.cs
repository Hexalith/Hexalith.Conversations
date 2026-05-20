// <copyright file="ConversationQueryCursorOptions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Configures the HMAC signing material and rotation identity used by <see cref="ConversationQueryCursor"/>.
/// </summary>
/// <remarks>
/// The host MUST provide a per-deployment signing secret. The constant-source key previously embedded in
/// the assembly does not constitute a tamper boundary because anyone with binary access can forge cursors.
/// Bind the secret through configuration (for example, <c>Hexalith:Conversations:Queries:Cursor:SigningKey</c>
/// as a base64 string) so it can be rotated without a code change. Cursors carry the <see cref="KeyId"/> they
/// were signed with so that future rotation can reject cursors signed by a retired key.
/// </remarks>
public sealed class ConversationQueryCursorOptions
{
    /// <summary>
    /// Gets or sets the HMAC-SHA256 signing key. The host must provide at least 32 bytes of cryptographically random material.
    /// </summary>
    public byte[] SigningKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the stable identifier of the current signing key. Bound into the cursor payload so the verifier can
    /// reject cursors signed by a retired key during rotation.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum cursor age. Defaults to 30 minutes; cursors older or future-dated beyond this window fail closed.
    /// </summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the maximum accepted page offset. Defends against forged cursors that would force unbounded skip scans.
    /// </summary>
    public int MaxOffset { get; set; } = 100_000;
}
