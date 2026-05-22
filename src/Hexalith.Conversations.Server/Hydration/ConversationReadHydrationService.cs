// <copyright file="ConversationReadHydrationService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Composes response-scoped reference hydration for projection-backed conversation reads.
/// </summary>
public sealed class ConversationReadHydrationService
{
    private readonly IConversationReferenceHydrationDirectory _directory;

    public ConversationReadHydrationService(IConversationReferenceHydrationDirectory? directory = null)
    {
        _directory = directory ?? UnavailableConversationReferenceHydrationDirectory.Instance;
    }

    /// <summary>
    /// Hydrates one detail response after tenant access and projection checks have succeeded.
    /// </summary>
    public async ValueTask<ConversationDetailsV1> HydrateDetailAsync(
        ConversationDetailsV1 details,
        ConversationHydrationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<PartyId> partyIds = details.Participants
            .Select(participant => participant.ParticipantPartyId)
            .Concat(details.Messages.Select(message => message.AuthorPartyId))
            .Distinct()
            .ToList();
        IReadOnlyList<ProjectId> projectIds = details.ProjectId is null ? [] : [details.ProjectId];
        IReadOnlyList<FolderId> folderIds = details.FolderId is null ? [] : [details.FolderId];
        IReadOnlyList<FileId> fileIds = details.FileReferences
            .Select(file => file.FileId)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>> partyResults =
            await HydrateOrUnavailableAsync(partyIds, ids => _directory.HydratePartiesAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>> projectResults =
            await HydrateOrUnavailableAsync(projectIds, ids => _directory.HydrateProjectsAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>> folderResults =
            await HydrateOrUnavailableAsync(folderIds, ids => _directory.HydrateFoldersAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>> fileResults =
            await HydrateOrUnavailableAsync(fileIds, ids => _directory.HydrateFilesAsync(context, ids, cancellationToken)).ConfigureAwait(false);

        return new ConversationDetailsV1(
            details.SchemaVersion,
            details.TenantId,
            details.ConversationId,
            details.Freshness,
            details.LifecycleState,
            details.Label,
            details.BusinessReference,
            details.ProjectId,
            details.FolderId,
            details.ProviderCorrelation,
            details.Participants,
            details.Messages,
            details.FileReferences,
            details.GovernanceState,
            details.Attributes,
            partyIds.Select(id => ToPartyHydration(id, Resolve(id, partyResults))).ToList(),
            details.ProjectId is null ? null : ToProjectHydration(details.ProjectId, Resolve(details.ProjectId, projectResults)),
            details.FolderId is null ? null : ToFolderHydration(details.FolderId, Resolve(details.FolderId, folderResults)),
            fileIds.Select(id => ToFileHydration(id, Resolve(id, fileResults))).ToList());
    }

    /// <summary>
    /// Hydrates list page summaries without changing list authorization, ordering, cursors, or filters.
    /// </summary>
    public async ValueTask<IReadOnlyList<ConversationSummaryV1>> HydrateSummariesAsync(
        IReadOnlyList<ConversationSummaryV1> summaries,
        ConversationHydrationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        ArgumentNullException.ThrowIfNull(context);

        if (summaries.Count == 0)
        {
            return summaries;
        }

        IReadOnlyList<PartyId> partyIds = summaries
            .SelectMany(summary => summary.ParticipantPartyIds)
            .Distinct()
            .ToList();
        IReadOnlyList<ProjectId> projectIds = summaries
            .Select(summary => summary.ProjectId)
            .Where(project => project is not null)
            .Cast<ProjectId>()
            .Distinct()
            .ToList();
        IReadOnlyList<FolderId> folderIds = summaries
            .Select(summary => summary.FolderId)
            .Where(folder => folder is not null)
            .Cast<FolderId>()
            .Distinct()
            .ToList();

        IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>> partyResults =
            await HydrateOrUnavailableAsync(partyIds, ids => _directory.HydratePartiesAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>> projectResults =
            await HydrateOrUnavailableAsync(projectIds, ids => _directory.HydrateProjectsAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>> folderResults =
            await HydrateOrUnavailableAsync(folderIds, ids => _directory.HydrateFoldersAsync(context, ids, cancellationToken)).ConfigureAwait(false);

        return summaries
            .Select(summary => new ConversationSummaryV1(
                summary.SchemaVersion,
                summary.TenantId,
                summary.ConversationId,
                summary.Freshness,
                summary.LifecycleState,
                summary.Label,
                summary.BusinessReference,
                summary.ProjectId,
                summary.FolderId,
                summary.ParticipantPartyIds,
                summary.MessageCount,
                summary.FileReferenceCount,
                summary.ProviderCorrelation,
                summary.ParticipantPartyIds.Select(id => ToPartyHydration(id, Resolve(id, partyResults))).ToList(),
                summary.ProjectId is null ? null : ToProjectHydration(summary.ProjectId, Resolve(summary.ProjectId, projectResults)),
                summary.FolderId is null ? null : ToFolderHydration(summary.FolderId, Resolve(summary.FolderId, folderResults))))
            .ToList();
    }

    private static async ValueTask<IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>> HydrateOrUnavailableAsync<TReference>(
        IReadOnlyList<TReference> references,
        Func<IReadOnlyCollection<TReference>, ValueTask<IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>>> hydrate)
        where TReference : class
    {
        if (references.Count == 0)
        {
            return new Dictionary<TReference, ReferenceHydrationResult<TReference>>();
        }

        try
        {
            return await hydrate(references).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // AC 3/4: Map any non-cancellation adapter failure (timeout, throttling, transport, custom
            // adapter exceptions, etc.) to safe Unavailable hydration so the entire read does not crash
            // and raw upstream problem details never reach the public response.
            return Unavailable(references);
        }
    }

    private static ReferenceHydrationResult<TReference> Resolve<TReference>(
        TReference reference,
        IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>> results)
        where TReference : class
        => results.TryGetValue(reference, out ReferenceHydrationResult<TReference>? result)
            ? result
            : new ReferenceHydrationResult<TReference>(reference, ReferenceHydrationStatus.Unavailable);

    private static IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>> Unavailable<TReference>(
        IReadOnlyList<TReference> references)
        where TReference : class
        => references.ToDictionary(
            reference => reference,
            reference => new ReferenceHydrationResult<TReference>(reference, ReferenceHydrationStatus.Unavailable));

    private static PartyReferenceHydrationV1 ToPartyHydration(
        PartyId id,
        ReferenceHydrationResult<PartyId> result)
    {
        (ProjectionTrustState state, bool resolved) = Map(result.Status);
        return new(
            id,
            state,
            resolved,
            resolved ? SafeOrFallback(result.SafeLabel, "Participant unavailable") : LabelFallback(state, "Participant unavailable"),
            resolved ? SafeOrFallback(result.SafeToken, "participant") : TokenFallback(state),
            resolved ? SafeOrFallback(result.SafeStatus, StatusFallback(state)) : StatusFallback(state));
    }

    private static ProjectReferenceHydrationV1 ToProjectHydration(
        ProjectId id,
        ReferenceHydrationResult<ProjectId> result)
    {
        (ProjectionTrustState state, bool resolved) = Map(result.Status);
        return new(
            id,
            state,
            resolved,
            resolved ? SafeOrFallback(result.SafeLabel, "Reference unavailable") : LabelFallback(state, "Reference unavailable"),
            resolved ? SafeOrFallback(result.SafeToken, "reference") : TokenFallback(state),
            resolved ? SafeOrFallback(result.SafeStatus, StatusFallback(state)) : StatusFallback(state));
    }

    private static FolderReferenceHydrationV1 ToFolderHydration(
        FolderId id,
        ReferenceHydrationResult<FolderId> result)
    {
        (ProjectionTrustState state, bool resolved) = Map(result.Status);
        return new(
            id,
            state,
            resolved,
            resolved ? SafeOrFallback(result.SafeLabel, "Reference unavailable") : LabelFallback(state, "Reference unavailable"),
            resolved ? SafeOrFallback(result.SafeToken, "reference") : TokenFallback(state),
            resolved ? SafeOrFallback(result.SafeStatus, StatusFallback(state)) : StatusFallback(state));
    }

    private static FileReferenceHydrationV1 ToFileHydration(
        FileId id,
        ReferenceHydrationResult<FileId> result)
    {
        (ProjectionTrustState state, bool resolved) = Map(result.Status);
        return new(
            id,
            state,
            resolved,
            resolved ? SafeOrFallback(result.SafeLabel, "Reference unavailable") : LabelFallback(state, "Reference unavailable"),
            resolved ? SafeOrFallback(result.SafeToken, "reference") : TokenFallback(state),
            resolved ? SafeOrFallback(result.SafeStatus, StatusFallback(state)) : StatusFallback(state));
    }

    private static (ProjectionTrustState State, bool Resolved) Map(ReferenceHydrationStatus status)
        => status switch
        {
            ReferenceHydrationStatus.Current => (ProjectionTrustState.Current, true),
            ReferenceHydrationStatus.Stale => (ProjectionTrustState.Stale, true),
            ReferenceHydrationStatus.Rebuilding => (ProjectionTrustState.Rebuilding, false),
            ReferenceHydrationStatus.Redacted or ReferenceHydrationStatus.PolicyFiltered or ReferenceHydrationStatus.Erased
                => (ProjectionTrustState.Redacted, false),
            ReferenceHydrationStatus.Unavailable or ReferenceHydrationStatus.Timeout or ReferenceHydrationStatus.Throttled
                => (ProjectionTrustState.Unavailable, false),
            _ => (ProjectionTrustState.Forbidden, false),
        };

    private static string SafeOrFallback(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string LabelFallback(ProjectionTrustState state, string unavailable)
        => state == ProjectionTrustState.Redacted ? "Reference redacted" : unavailable;

    private static string TokenFallback(ProjectionTrustState state)
        => state == ProjectionTrustState.Redacted ? "redacted" : "unavailable";

    private static string StatusFallback(ProjectionTrustState state)
        => state == ProjectionTrustState.Redacted
            ? "Redacted"
            : state == ProjectionTrustState.Stale
                ? "Stale"
                : state == ProjectionTrustState.Rebuilding
                    ? "Rebuilding"
                    : "Unavailable";
}
