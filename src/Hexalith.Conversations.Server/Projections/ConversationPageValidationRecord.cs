// <copyright file="ConversationPageValidationRecord.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// The fields needed to verify a conversation's generation against its index and dispatch ledger.
/// </summary>
/// <param name="Summary">The persisted conversation summary.</param>
/// <param name="Detail">The persisted detail header.</param>
/// <param name="DispatchId">The stable dispatch identity that produced this generation.</param>
[JsonConverter(typeof(ConversationPageValidationRecordConverter))]
internal sealed record ConversationPageValidationRecord(
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailValidationHeader Detail,
    string DispatchId)
{
    /// <summary>Extracts the validation fields from a fully deserialized conversation record.</summary>
    internal static ConversationPageValidationRecord FromFullRecord(ConversationProjectedReadModels models)
        => new(
            models.Summary,
            new ConversationDetailValidationHeader(
                models.Detail.SchemaVersion, models.Detail.TenantId, models.Detail.ConversationId,
                models.Detail.Freshness, models.Detail.LifecycleState),
            models.DispatchId);
}
