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
    ConversationProjectionDispatchStatus Status);

/// <summary>Durable completion states for a Conversations projection dispatch.</summary>
public enum ConversationProjectionDispatchStatus
{
    /// <summary>The dispatch is visible only as incomplete work.</summary>
    Pending = 0,

    /// <summary>Both projection keys were persisted for this stable dispatch identity.</summary>
    Completed = 1,
}
