// <copyright file="ProjectionFreshness.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Describes the freshness and trust state of a public read contract.
/// </summary>
/// <param name="state">The public trust state.</param>
/// <param name="observedAt">The time at which the read state was observed.</param>
/// <param name="schemaVersion">The projection contract schema version.</param>
/// <param name="guidance">Optional safe developer guidance.</param>
public sealed record ProjectionFreshness(
    ProjectionTrustState State,
    DateTimeOffset ObservedAt,
    SchemaVersion SchemaVersion,
    string? Guidance = null);
