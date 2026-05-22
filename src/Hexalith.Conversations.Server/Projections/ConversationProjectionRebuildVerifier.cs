// <copyright file="ConversationProjectionRebuildVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Deterministic local proof seam for rebuilding projections from ordered EventStore-derived events.
/// </summary>
public sealed class ConversationProjectionRebuildVerifier(ConversationProjectionMaterializer materializer)
{
    private const string StoryKey = "1-11-prove-replay-schema-versioning-and-projection-rebuild-behavior";
    private readonly ConversationProjectionMaterializer _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));

    /// <summary>
    /// Rebuilds projections from ordered event history and emits unsigned local evidence.
    /// </summary>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="conversationId">The conversation scope.</param>
    /// <param name="events">Ordered persisted events.</param>
    /// <param name="existing">Existing derived state, if any.</param>
    /// <param name="generatedAt">Fixed projection generation time.</param>
    /// <param name="staleAfter">Freshness threshold.</param>
    /// <param name="coveredTestIds">Covered local proof identifiers.</param>
    /// <returns>The rebuild result.</returns>
    public ConversationProjectionRebuildResult Rebuild(
        TenantId tenantId,
        ConversationId conversationId,
        IEnumerable<ConversationProjectionEventRecord> events,
        ConversationProjectedReadModels? existing,
        DateTimeOffset generatedAt,
        TimeSpan staleAfter,
        IReadOnlyList<string> coveredTestIds)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(coveredTestIds);

        ConversationProjectedReadModels rebuilt = _materializer.Project(
            tenantId,
            conversationId,
            events,
            generatedAt,
            staleAfter,
            isRebuilding: false);

        ProjectionTrustState disposition = existing is null
            ? ProjectionTrustState.Rebuilding
            : Equivalent(existing, rebuilt)
                ? ProjectionTrustState.Current
                : ProjectionTrustState.Stale;

        ProjectionFreshnessV1 freshness = rebuilt.Summary.Freshness;
        bool passed = freshness.AllowsTrustBearingDecision();
        ConversationProjectionRebuildEvidence evidence = new(
            StoryKey,
            coveredTestIds,
            SchemaVersion.Current,
            freshness.ProjectionContractSchemaVersion,
            tenantId,
            conversationId,
            freshness.FreshnessState,
            passed,
            freshness.ReasonCode,
            generatedAt,
            freshness.ProjectionCursor);

        return new ConversationProjectionRebuildResult(rebuilt, evidence, disposition);
    }

    private static bool Equivalent(ConversationProjectedReadModels first, ConversationProjectedReadModels second)
        => first.Summary.SchemaVersion == second.Summary.SchemaVersion
            && first.Summary.TenantId == second.Summary.TenantId
            && first.Summary.ConversationId == second.Summary.ConversationId
            && first.Summary.LifecycleState == second.Summary.LifecycleState
            && first.Summary.Label == second.Summary.Label
            && first.Summary.BusinessReference == second.Summary.BusinessReference
            && first.Summary.MessageCount == second.Summary.MessageCount
            && first.Summary.FileReferenceCount == second.Summary.FileReferenceCount
            && first.Summary.ParticipantPartyIds.SequenceEqual(second.Summary.ParticipantPartyIds)
            && first.Detail.Messages.SequenceEqual(second.Detail.Messages)
            && first.Detail.Participants.SequenceEqual(second.Detail.Participants)
            && first.Detail.FileReferences.SequenceEqual(second.Detail.FileReferences)
            && first.Detail.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal)
                .SequenceEqual(second.Detail.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal));
}
