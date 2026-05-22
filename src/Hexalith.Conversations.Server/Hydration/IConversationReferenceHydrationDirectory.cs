// <copyright file="IConversationReferenceHydrationDirectory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Resolves upstream reference display/status data behind a Conversations-owned read boundary.
/// </summary>
public interface IConversationReferenceHydrationDirectory
{
    /// <summary>
    /// Resolves Party references for a tenant-scoped read.
    /// </summary>
    ValueTask<IReadOnlyDictionary<PartyId, ReferenceHydrationResult<PartyId>>> HydratePartiesAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<PartyId> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves project references for a tenant-scoped read.
    /// </summary>
    ValueTask<IReadOnlyDictionary<ProjectId, ReferenceHydrationResult<ProjectId>>> HydrateProjectsAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<ProjectId> projectIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves folder references for a tenant-scoped read.
    /// </summary>
    ValueTask<IReadOnlyDictionary<FolderId, ReferenceHydrationResult<FolderId>>> HydrateFoldersAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<FolderId> folderIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves file references for a tenant-scoped read.
    /// </summary>
    ValueTask<IReadOnlyDictionary<FileId, ReferenceHydrationResult<FileId>>> HydrateFilesAsync(
        ConversationHydrationContext context,
        IReadOnlyCollection<FileId> fileIds,
        CancellationToken cancellationToken = default);
}
