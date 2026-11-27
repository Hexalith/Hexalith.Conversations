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

        List<PartyId> partyIds = CollectDetailPartyIds(details);
        IReadOnlyList<ProjectId> projectIds = details.ProjectId is null ? [] : [details.ProjectId];
        IReadOnlyList<FolderId> folderIds = details.FolderId is null ? [] : [details.FolderId];
        List<FileId> fileIds = CollectDetailFileIds(details);

        IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>> partyResults =
            await HydrateOrUnavailableAsync(partyIds, ids => _directory.HydratePartiesAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>> projectResults =
            await HydrateOrUnavailableAsync(projectIds, ids => _directory.HydrateProjectsAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>> folderResults =
            await HydrateOrUnavailableAsync(folderIds, ids => _directory.HydrateFoldersAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>> fileResults =
            await HydrateOrUnavailableAsync(fileIds, ids => _directory.HydrateFilesAsync(context, ids, cancellationToken)).ConfigureAwait(false);

        PartyReferenceHydrationV1[] partyHydration = partyIds.Count == 0
            ? []
            : new PartyReferenceHydrationV1[partyIds.Count];
        for (int index = 0; index < partyIds.Count; index++)
        {
            PartyId id = partyIds[index];
            partyHydration[index] = ToPartyHydration(id, Resolve(id, partyResults));
        }

        FileReferenceHydrationV1[] fileHydration = fileIds.Count == 0
            ? []
            : new FileReferenceHydrationV1[fileIds.Count];
        for (int index = 0; index < fileIds.Count; index++)
        {
            FileId id = fileIds[index];
            fileHydration[index] = ToFileHydration(id, Resolve(id, fileResults));
        }

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
            partyHydration,
            details.ProjectId is null ? null : ToProjectHydration(details.ProjectId, Resolve(details.ProjectId, projectResults)),
            details.FolderId is null ? null : ToFolderHydration(details.FolderId, Resolve(details.FolderId, folderResults)),
            fileHydration,
            details.SensitivityMarks,
            details.Redactions,
            details.TrustPosture.WithParticipantResolution(WorstHydrationState(partyHydration)),
            details.EvidenceEntries);
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

        List<PartyId> partyIds = [];
        HashSet<PartyId> seenParties = [];
        List<ProjectId> projectIds = [];
        HashSet<ProjectId> seenProjects = [];
        List<FolderId> folderIds = [];
        HashSet<FolderId> seenFolders = [];
        for (int index = 0; index < summaries.Count; index++)
        {
            ConversationSummaryV1 summary = summaries[index];
            IReadOnlyList<PartyId> participants = summary.ParticipantPartyIds;
            for (int partyIndex = 0; partyIndex < participants.Count; partyIndex++)
            {
                PartyId partyId = participants[partyIndex];
                if (seenParties.Add(partyId))
                {
                    partyIds.Add(partyId);
                }
            }

            if (summary.ProjectId is { } projectId && seenProjects.Add(projectId))
            {
                projectIds.Add(projectId);
            }

            if (summary.FolderId is { } folderId && seenFolders.Add(folderId))
            {
                folderIds.Add(folderId);
            }
        }

        IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>> partyResults =
            await HydrateOrUnavailableAsync(partyIds, ids => _directory.HydratePartiesAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>> projectResults =
            await HydrateOrUnavailableAsync(projectIds, ids => _directory.HydrateProjectsAsync(context, ids, cancellationToken)).ConfigureAwait(false);
        IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>> folderResults =
            await HydrateOrUnavailableAsync(folderIds, ids => _directory.HydrateFoldersAsync(context, ids, cancellationToken)).ConfigureAwait(false);

        ConversationSummaryV1[] hydrated = new ConversationSummaryV1[summaries.Count];
        for (int index = 0; index < summaries.Count; index++)
        {
            ConversationSummaryV1 summary = summaries[index];
            IReadOnlyList<PartyId> participants = summary.ParticipantPartyIds;
            PartyReferenceHydrationV1[] partyHydration = participants.Count == 0
                ? []
                : new PartyReferenceHydrationV1[participants.Count];
            for (int partyIndex = 0; partyIndex < participants.Count; partyIndex++)
            {
                PartyId id = participants[partyIndex];
                partyHydration[partyIndex] = ToPartyHydration(id, Resolve(id, partyResults));
            }

            hydrated[index] = new ConversationSummaryV1(
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
                partyHydration,
                summary.ProjectId is null ? null : ToProjectHydration(summary.ProjectId, Resolve(summary.ProjectId, projectResults)),
                summary.FolderId is null ? null : ToFolderHydration(summary.FolderId, Resolve(summary.FolderId, folderResults)),
                summary.SearchTrustPreview.WithParticipantResolution(WorstHydrationState(partyHydration)));
        }

        return hydrated;
    }

    private static List<PartyId> CollectDetailPartyIds(ConversationDetailsV1 details)
    {
        List<PartyId> partyIds = [];
        HashSet<PartyId> seen = [];
        for (int index = 0; index < details.Participants.Count; index++)
        {
            PartyId id = details.Participants[index].ParticipantPartyId;
            if (seen.Add(id))
            {
                partyIds.Add(id);
            }
        }

        for (int index = 0; index < details.Messages.Count; index++)
        {
            PartyId id = details.Messages[index].AuthorPartyId;
            if (seen.Add(id))
            {
                partyIds.Add(id);
            }
        }

        return partyIds;
    }

    private static List<FileId> CollectDetailFileIds(ConversationDetailsV1 details)
    {
        List<FileId> fileIds = [];
        if (details.FileReferences.Count == 0)
        {
            return fileIds;
        }

        HashSet<FileId> seen = [];
        for (int index = 0; index < details.FileReferences.Count; index++)
        {
            FileId id = details.FileReferences[index].FileId;
            if (seen.Add(id))
            {
                fileIds.Add(id);
            }
        }

        return fileIds;
    }

    private static async ValueTask<IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>> HydrateOrUnavailableAsync<TReference>(
        IReadOnlyList<TReference> references,
        Func<IReadOnlyCollection<TReference>, ValueTask<IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>>> hydrate)
        where TReference : class
    {
        if (references.Count == 0)
        {
            return EmptyHydrationResults<TReference>.Instance;
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

    private static ProjectionTrustState WorstHydrationState(IReadOnlyList<PartyReferenceHydrationV1> hydration)
    {
        if (hydration.Count == 0)
        {
            return ProjectionTrustState.Current;
        }

        ProjectionTrustState worst = ProjectionTrustState.Current;
        foreach (PartyReferenceHydrationV1 item in hydration)
        {
            if (Priority(item.HydrationState) > Priority(worst))
            {
                worst = item.HydrationState;
            }
        }

        return worst;

        static int Priority(ProjectionTrustState state)
        {
            if (state == ProjectionTrustState.Forbidden) { return 6; }
            if (state == ProjectionTrustState.Unavailable) { return 5; }
            if (state == ProjectionTrustState.Rebuilding) { return 4; }
            if (state == ProjectionTrustState.Stale) { return 3; }
            if (state == ProjectionTrustState.Redacted) { return 2; }
            return 0;
        }
    }

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
