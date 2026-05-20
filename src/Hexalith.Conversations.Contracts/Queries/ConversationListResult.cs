// <copyright file="ConversationListResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries a content-safe conversation list outcome.
/// </summary>
public sealed record ConversationListResult(
    SchemaVersion SchemaVersion,
    ProjectionTrustState FreshnessState,
    ProjectionFreshnessReasonCode ReasonCode,
    IReadOnlyList<ConversationSummaryV1> Conversations,
    ConversationPageMetadata Page,
    string SafeNextAction)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the public freshness state.
    /// </summary>
    public ProjectionTrustState FreshnessState { get; } = FreshnessState ?? throw new ArgumentNullException(nameof(FreshnessState));

    /// <summary>
    /// Gets the public freshness reason code.
    /// </summary>
    public ProjectionFreshnessReasonCode ReasonCode { get; } = ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    /// <summary>
    /// Gets accessible conversation summaries.
    /// </summary>
    public IReadOnlyList<ConversationSummaryV1> Conversations { get; } = ValidateConversations(Conversations);

    /// <summary>
    /// Gets permission-safe page metadata.
    /// </summary>
    public ConversationPageMetadata Page { get; } = Page ?? throw new ArgumentNullException(nameof(Page));

    /// <summary>
    /// Gets safe next-action metadata.
    /// </summary>
    public string SafeNextAction { get; } = ValidateRequired(SafeNextAction, nameof(SafeNextAction));

    /// <summary>
    /// Creates a content-safe hidden list result.
    /// </summary>
    public static ConversationListResult Hidden(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            [],
            new ConversationPageMetadata(0),
            "No accessible conversations are available.");

    /// <summary>
    /// Creates a content-safe unavailable list result.
    /// </summary>
    public static ConversationListResult Unavailable(SchemaVersion schemaVersion)
        => new(
            schemaVersion,
            ProjectionTrustState.Unavailable,
            ProjectionFreshnessReasonCode.Unavailable,
            [],
            new ConversationPageMetadata(0),
            "Retry after the read model is available.");

    private static IReadOnlyList<ConversationSummaryV1> ValidateConversations(IReadOnlyList<ConversationSummaryV1>? conversations)
    {
        if (conversations is null || conversations.Count == 0)
        {
            return Array.Empty<ConversationSummaryV1>();
        }

        return conversations.Any(conversation => conversation is null)
            ? throw new ArgumentException("Conversation list results must not contain null elements.", nameof(conversations))
            : conversations;
    }

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
