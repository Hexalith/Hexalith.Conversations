// <copyright file="ConversationProjectionIndexHeader.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// The version 2 lifecycle header the index writer carries through one compare-and-swap transition.
/// </summary>
/// <param name="Lifecycle">The lifecycle written back; a contradictory persisted value is carried unchanged.</param>
/// <param name="Watermarks">The mutable, non-regressing per-conversation watermarks.</param>
internal readonly record struct ConversationProjectionIndexHeader(
    ConversationProjectionIndexLifecycle? Lifecycle,
    Dictionary<string, long> Watermarks);
