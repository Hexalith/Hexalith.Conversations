// <copyright file="ConformanceDeletionWire.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Mirrors the separately installed conformance receiver's wire contract; the receiver tool is never referenced.</summary>
internal static class ConformanceDeletionWire
{
    /// <summary>The receiver command type, an exact CLR full name in the receiver's assembly.</summary>
    public const string CommandType = "Hexalith.Conversations.DeletionConformance.AcceptConformanceDeletion";

    /// <summary>The independent receipt query type.</summary>
    public const string ReceiptQueryType = "deletion-conformance-receipt";

    /// <summary>The independent identity query type.</summary>
    public const string IdentityQueryType = "deletion-conformance-identity";

    /// <summary>The fixed identity query aggregate.</summary>
    public const string IdentityAggregateId = "identity";

    /// <summary>The fixed conformance receiver kind.</summary>
    public const string ReceiverKind = "ContractConformance";

    /// <summary>The only qualification scope the receiver may report.</summary>
    public const string QualificationScope = "OQ-5.4-1";

    /// <summary>The only evidence level the receiver may report.</summary>
    public const int EvidenceLevel = 4;

    /// <summary>The content-free receipt evidence prefix.</summary>
    public const string EvidencePrefix = "oq-conformance-v1:";

    /// <summary>The command and receipt-lookup payload fields.</summary>
    public static readonly IReadOnlyList<string> RequestFields = ["signal", "deliveryAttemptId", "targetVersion"];

    /// <summary>The exact identity payload fields.</summary>
    public static readonly IReadOnlyList<string> IdentityFields =
    [
        "schemaVersion", "receiverKind", "qualificationScope", "evidenceLevel", "agentsRuntimeEnabled", "tenantId", "targetVersion",
        "storageIdentity", "receiverPrincipalId", "lookupPrincipalId",
    ];

    /// <summary>The receipt payload fields.</summary>
    public static readonly IReadOnlyList<string> ReceiptFields = ["outcome", "acknowledgement"];

    /// <summary>The exact acknowledgement fields.</summary>
    public static readonly IReadOnlyList<string> AcknowledgementFields =
        ["signalId", "sourceRevision", "protectedDeletionRevision", "targetVersion", "evidence"];
}

/// <summary>Mirrors <c>AcceptConformanceDeletion</c> and <c>DeletionConformanceLookup</c>: <c>{ signal, deliveryAttemptId, targetVersion }</c>.</summary>
/// <param name="Signal">The original immutable source publication.</param>
/// <param name="DeliveryAttemptId">The durable source delivery attempt.</param>
/// <param name="TargetVersion">The exact receiver target.</param>
internal sealed record ConformanceDeletionRequest(ConversationDeletionSignal Signal, string DeliveryAttemptId, string TargetVersion);
