// <copyright file="ConversationReplayEventBinding.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// The full binding a replay identity is bound to inside one tenant/conversation materialization (AD-8).
/// </summary>
/// <param name="EventType">The persisted event type discriminator.</param>
/// <param name="Sequence">The aggregate sequence the event was persisted at.</param>
/// <param name="SerializationFormat">The persisted serialization format.</param>
/// <param name="PayloadSha256">The lowercase-hex SHA-256 of the exact persisted payload bytes.</param>
/// <param name="SelectedUtcTicks">The UTC ticks of the selected replay time.</param>
/// <remarks>
/// The tenant and conversation members of the AD-8 binding are the materialization scope itself: an event whose
/// metadata names another scope is poisoned before duplicate tracking, so it never reaches this comparison.
/// Re-delivering an identity with an equal binding is a no-op; any difference is contradictory metadata.
/// </remarks>
internal readonly record struct ConversationReplayEventBinding(
    string EventType,
    long Sequence,
    string SerializationFormat,
    string PayloadSha256,
    long SelectedUtcTicks);
