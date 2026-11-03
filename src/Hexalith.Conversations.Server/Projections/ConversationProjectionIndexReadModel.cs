// <copyright file="ConversationProjectionIndexReadModel.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Cross-conversation, per-tenant summary index persisted as one state-store value. The list boundary treats
/// these as candidates and verifies each candidate against its detail key before reporting it current.
/// </summary>
/// <remarks>
/// <para>
/// This is a Server-internal persistence shape; the persisted elements are the existing public
/// <see cref="ConversationSummaryProjectionV1"/> Contracts type, so the public contract-shape baseline is
/// unaffected. The index is maintained through <see cref="Hexalith.EventStore.Client.Projections.ReadModelWritePolicy"/>'s
/// idempotent reload-and-merge path (dedup by conversation identity, newest generation wins).
/// </para>
/// <para>
/// Version 2 (AD-7) makes this record the sole Conversations lifecycle/watermark record: the lifecycle and the
/// per-conversation watermarks share the index's single compare-and-swap with its entries, and only
/// <see cref="ConversationProjectionReadModelWriter"/> authors their transitions. A record without a schema
/// version is version 1. The version 2 members are internal persisted state, not public members.
/// </para>
/// </remarks>
public sealed class ConversationProjectionIndexReadModel
{
    /// <summary>The schema version of a lifecycle-bearing index.</summary>
    internal const int LifecycleSchemaVersion = 2;

    /// <summary>
    /// Gets the visible conversation summaries for the tenant scope.
    /// </summary>
    public IReadOnlyList<ConversationSummaryProjectionV1> Summaries { get; init; } = [];

    /// <summary>
    /// Gets the latest dispatch reference per conversation. A reference without a matching summary deliberately
    /// exposes pending work to fail-closed list readers before the detail write starts.
    /// </summary>
    public IReadOnlyDictionary<string, ConversationProjectionDispatchReference> Dispatches { get; init; }
        = new Dictionary<string, ConversationProjectionDispatchReference>(StringComparer.Ordinal);

    /// <summary>Gets the persisted schema version; zero (absent) and one both denote a version 1 index.</summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    internal int SchemaVersion { get; init; }

    /// <summary>Gets the version 2 lifecycle; absent from a version 1 index.</summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal ConversationProjectionIndexLifecycle? Lifecycle { get; init; }

    /// <summary>
    /// Gets the non-regressing per-conversation watermark: the highest aggregate sequence a completed generation
    /// of that conversation has published into this index, ordered ordinally by conversation identity.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal IReadOnlyDictionary<string, long>? Watermarks { get; init; }

    /// <summary>Gets a value indicating whether the record is a version 2 lifecycle record.</summary>
    internal bool IsLifecycleRecord => SchemaVersion == LifecycleSchemaVersion;
}
