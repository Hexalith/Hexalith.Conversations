// <copyright file="ConversationProjectionDispatchReference.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Links one tenant-index candidate to the dispatch ledger that proves its completed generation.</summary>
/// <param name="DispatchId">The stable dispatch identity.</param>
/// <param name="LastAppliedEventPosition">The generation position carried by the dispatch.</param>
/// <param name="IsPending">Whether the reference is an incomplete pre-write marker.</param>
/// <param name="PreviousDispatchId">The completed reference displaced by this pending marker, when any.</param>
/// <param name="PreviousLastAppliedEventPosition">The displaced completed generation position, when any.</param>
public sealed record ConversationProjectionDispatchReference(
    string DispatchId,
    long LastAppliedEventPosition,
    bool IsPending = false,
    string? PreviousDispatchId = null,
    long? PreviousLastAppliedEventPosition = null);
