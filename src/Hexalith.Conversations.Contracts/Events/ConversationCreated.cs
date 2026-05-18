// <copyright file="ConversationCreated.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that a conversation was created.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="projectId">An optional stable project reference.</param>
/// <param name="folderId">An optional stable folder reference.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="providerCorrelation">Optional provider correlation metadata.</param>
public sealed record ConversationCreated(
    ConversationEventMetadata Metadata,
    BusinessReference? BusinessReference = null,
    ProjectId? ProjectId = null,
    FolderId? FolderId = null,
    string? Label = null,
    ProviderCorrelationMetadata? ProviderCorrelation = null);
