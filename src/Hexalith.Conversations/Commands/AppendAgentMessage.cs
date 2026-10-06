// <copyright file="AppendAgentMessage.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Commands;

/// <summary>Pure restricted AppendAgentMessage intent; admission supplies current authority.</summary>
/// <param name="PublicCommand">The portable owner contract.</param>
/// <param name="PostedAt">Deterministic occurrence time.</param>
/// <param name="EventId">Deterministic source event identity.</param>
public sealed record AppendAgentMessage(AppendMessageCommand PublicCommand, DateTimeOffset PostedAt, string EventId);
