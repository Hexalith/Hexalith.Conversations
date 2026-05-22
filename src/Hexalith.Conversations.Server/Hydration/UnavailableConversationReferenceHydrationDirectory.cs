// <copyright file="UnavailableConversationReferenceHydrationDirectory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Safe default resolver used when no upstream reference adapter is registered.
/// </summary>
public sealed class UnavailableConversationReferenceHydrationDirectory : IConversationReferenceHydrationDirectory
{
    /// <summary>
    /// Gets the shared unavailable resolver.
    /// </summary>
    public static UnavailableConversationReferenceHydrationDirectory Instance { get; } = new();

    private UnavailableConversationReferenceHydrationDirectory()
    {
    }

    public ValueTask<IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>> HydratePartiesAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<PartyId> partyIds,
        CancellationToken cancellationToken = default)
        => Unavailable(partyIds);

    public ValueTask<IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>> HydrateProjectsAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<ProjectId> projectIds,
        CancellationToken cancellationToken = default)
        => Unavailable(projectIds);

    public ValueTask<IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>> HydrateFoldersAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<FolderId> folderIds,
        CancellationToken cancellationToken = default)
        => Unavailable(folderIds);

    public ValueTask<IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>> HydrateFilesAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<FileId> fileIds,
        CancellationToken cancellationToken = default)
        => Unavailable(fileIds);

    private static ValueTask<IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>> Unavailable<TReference>(
        IReadOnlyCollection<TReference> references)
        where TReference : class
        => ValueTask.FromResult((IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>)references
            .Distinct()
            .ToDictionary(
                reference => reference,
                reference => new ReferenceHydrationResult<TReference>(reference, ReferenceHydrationStatus.Unavailable)));
}
