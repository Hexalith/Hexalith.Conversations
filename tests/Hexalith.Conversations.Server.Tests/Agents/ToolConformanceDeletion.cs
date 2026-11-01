// <copyright file="ToolConformanceDeletion.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Mirror of the receiver tool's command and lookup payload, used only to read what the adapter sends.</summary>
internal sealed record ToolConformanceDeletion(ConversationDeletionSignal Signal, string DeliveryAttemptId, string TargetVersion);
