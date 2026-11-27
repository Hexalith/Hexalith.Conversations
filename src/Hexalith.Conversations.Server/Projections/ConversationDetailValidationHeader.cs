// <copyright file="ConversationDetailValidationHeader.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Minimal header subset of <see cref="ConversationDetailProjectionV1"/> deserialized during list page
/// verification so full message timelines, participants, and evidence arrays are not allocated per row
/// while preserving fail-closed validation of required and recognized detail fields.
/// </summary>
/// <param name="SchemaVersion">The detail record's public projection schema version.</param>
/// <param name="TenantId">The detail record's tenant identity.</param>
/// <param name="ConversationId">The detail record's conversation identity.</param>
/// <param name="Freshness">The detail record's server-computed freshness metadata.</param>
/// <param name="LifecycleState">The detail record's safe lifecycle state token.</param>
[JsonConverter(typeof(ConversationDetailValidationHeaderConverter))]
internal sealed record ConversationDetailValidationHeader(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    ProjectionFreshnessV1 Freshness,
    string LifecycleState)
{
    /// <summary>Gets the detail record's public projection schema version.</summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>Gets the detail record's tenant identity.</summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>Gets the detail record's conversation identity.</summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>Gets the detail record's server-computed freshness metadata.</summary>
    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    /// <summary>Gets the detail record's safe lifecycle state token.</summary>
    public string LifecycleState { get; } = ValidateLifecycle(LifecycleState);

    /// <summary>Validates that a lifecycle state matches <see cref="ConversationDetailProjectionV1"/> rules.</summary>
    /// <param name="value">The candidate lifecycle state.</param>
    /// <returns>The validated lifecycle state.</returns>
    internal static string ValidateLifecycle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value switch
        {
            "Initializing" or "Open" or "Closed" or "Archived" => value,
            _ => throw new ArgumentException("Unsupported conversation lifecycle state.", nameof(value)),
        };
    }
}
