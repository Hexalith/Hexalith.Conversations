// <copyright file="CreateConversationCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Requests creation of a tenant-scoped conversation.
/// </summary>
/// <param name="metadata">The command metadata.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="projectId">An optional stable project reference.</param>
/// <param name="folderId">An optional stable folder reference.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata.</param>
/// <param name="callerMetadata">Optional bounded, content-safe caller provenance metadata.</param>
public sealed record CreateConversationCommand(
    ConversationCommandMetadata Metadata,
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    string? Label = null,
    ProviderCorrelationMetadata? ProviderCorrelation = null,
    CallerMetadata? CallerMetadata = null)
{
    /// <summary>Gets the explicit native classification of Label for the fixed internal scoring purpose.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConversationResolutionScoringLabel? ResolutionScoringLabel { get; init; }
}
