// <copyright file="ConversationProjectionRebuildEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Unsigned local evidence produced by replay/rebuild proof tests.
/// </summary>
/// <param name="StoryKey">The story that produced the evidence.</param>
/// <param name="CoveredTestIds">The deterministic local proof identifiers.</param>
/// <param name="SchemaVersion">The domain event schema version under proof.</param>
/// <param name="ProjectionContractVersion">The projection contract version under proof.</param>
/// <param name="TenantId">The synthetic tenant scope.</param>
/// <param name="ConversationId">The synthetic conversation scope.</param>
/// <param name="RebuildStatus">The public rebuild trust state.</param>
/// <param name="Passed">A value indicating whether the proof row passed.</param>
/// <param name="SafeDiagnosticCode">The bounded safe diagnostic code.</param>
/// <param name="ProducedAt">The fixed evidence timestamp.</param>
/// <param name="Cursor">The normalized public projection cursor.</param>
public sealed record ConversationProjectionRebuildEvidence(
    string StoryKey,
    IReadOnlyList<string> CoveredTestIds,
    SchemaVersion SchemaVersion,
    SchemaVersion ProjectionContractVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionTrustState RebuildStatus,
    bool Passed,
    ProjectionFreshnessReasonCode SafeDiagnosticCode,
    DateTimeOffset ProducedAt,
    string Cursor)
{
    /// <summary>
    /// Gets the story that produced the evidence.
    /// </summary>
    public string StoryKey { get; } = ValidateRequired(StoryKey, nameof(StoryKey));

    /// <summary>
    /// Gets deterministic local proof identifiers.
    /// </summary>
    public IReadOnlyList<string> CoveredTestIds { get; } = ValidateTestIds(CoveredTestIds);

    /// <summary>
    /// Gets the bounded normalized public projection cursor.
    /// </summary>
    public string Cursor { get; } = ValidateRequired(Cursor, nameof(Cursor));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static IReadOnlyList<string> ValidateTestIds(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one covered test id is required.", nameof(values));
        }

        return values.ToArray();
    }
}
