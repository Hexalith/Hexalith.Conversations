// <copyright file="ConversationProjectionRebuildResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Carries rebuilt read models, evidence, and disposition for any previous derived artifact.
/// </summary>
/// <param name="Models">The rebuilt read models.</param>
/// <param name="Evidence">The unsigned local verification evidence.</param>
/// <param name="ExistingArtifactDisposition">The disposition applied to previous derived state.</param>
public sealed record ConversationProjectionRebuildResult(
    ConversationProjectedReadModels Models,
    ConversationProjectionRebuildEvidence Evidence,
    ProjectionTrustState ExistingArtifactDisposition);
