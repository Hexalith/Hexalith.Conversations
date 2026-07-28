// <copyright file="ConversationProjectionDispatchReference.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Links one tenant-index candidate to the dispatch ledger that proves its completed generation.</summary>
/// <param name="DispatchId">The stable dispatch identity.</param>
/// <param name="LastAppliedEventPosition">The generation position carried by the dispatch.</param>
public sealed record ConversationProjectionDispatchReference(
    string DispatchId,
    long LastAppliedEventPosition);
