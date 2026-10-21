// <copyright file="ConversationProjectionDispatchLedger.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Internal durable state for one stable named-projection dispatch identity.</summary>
public sealed record ConversationProjectionDispatchLedger(
    string DispatchId,
    string RequestFingerprint,
    TenantId TenantId,
    ConversationId ConversationId,
    DateTimeOffset ProjectionGeneratedAt,
    ConversationProjectionDispatchStatus Status)
{
    /// <summary>Gets the first durable admission decision; omitted by legacy ledger records.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ConversationProjectionNativeAdmission? NativeAdmission { get; init; }
}
