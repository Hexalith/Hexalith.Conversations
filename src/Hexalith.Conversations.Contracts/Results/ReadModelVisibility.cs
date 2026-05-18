// <copyright file="ReadModelVisibility.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Results;

/// <summary>
/// Describes the expected read-model visibility caveat for an accepted command.
/// </summary>
/// <param name="state">The expected trust state after command acceptance.</param>
/// <param name="guidance">Optional safe developer guidance.</param>
public sealed record ReadModelVisibility(
    ProjectionTrustState State,
    string? Guidance = null);
