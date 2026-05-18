// <copyright file="ConversationMetadataUpdated.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records that safe conversation metadata changed.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="label">An optional UI label that is not identity.</param>
/// <param name="businessReference">An optional adopter-owned business reference.</param>
/// <param name="attributes">Optional safe adopter metadata.</param>
public sealed record ConversationMetadataUpdated(
    ConversationEventMetadata Metadata,
    string? Label = null,
    BusinessReference? BusinessReference = null,
    IReadOnlyDictionary<string, string>? Attributes = null);
