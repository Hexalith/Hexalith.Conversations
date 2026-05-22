// <copyright file="ConversationTemporalAnchorV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Identifies a Conversations-owned temporal reconstruction anchor without exposing storage topology.
/// </summary>
/// <param name="schemaVersion">The anchor contract schema version.</param>
/// <param name="tenantId">The tenant binding carried by the anchor.</param>
/// <param name="conversationId">The conversation binding carried by the anchor.</param>
/// <param name="anchorKind">The supported anchor kind.</param>
/// <param name="timestamp">The requested committed-at timestamp for timestamp anchors.</param>
/// <param name="safeSourcePosition">The positive Conversations-owned source position.</param>
/// <param name="projectionCursor">The safe projection cursor value.</param>
/// <param name="contractCursor">The safe contract-defined temporal cursor.</param>
public sealed record ConversationTemporalAnchorV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string AnchorKind,
    DateTimeOffset? Timestamp = null,
    long? SafeSourcePosition = null,
    string? ProjectionCursor = null,
    string? ContractCursor = null)
{
    public const string TimestampKind = "timestamp";
    public const string SafeSourcePositionKind = "safe_source_position";
    public const string ProjectionCursorKind = "projection_cursor";
    public const string ContractCursorKind = "contract_cursor";

    /// <summary>
    /// Gets the anchor contract schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the tenant binding carried by the anchor.
    /// </summary>
    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    /// <summary>
    /// Gets the conversation binding carried by the anchor.
    /// </summary>
    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    /// <summary>
    /// Gets the supported anchor kind.
    /// </summary>
    public string AnchorKind { get; } = ValidateKind(AnchorKind);

    /// <summary>
    /// Gets the requested committed-at timestamp for timestamp anchors.
    /// </summary>
    public DateTimeOffset? Timestamp { get; } = ValidateTimestamp(AnchorKind, Timestamp);

    /// <summary>
    /// Gets the positive Conversations-owned source position.
    /// </summary>
    public long? SafeSourcePosition { get; } = ValidatePosition(AnchorKind, SafeSourcePosition);

    /// <summary>
    /// Gets the safe projection cursor value.
    /// </summary>
    public string? ProjectionCursor { get; } = ValidateOptionalCursor(AnchorKind, ProjectionCursor, ProjectionCursorKind);

    /// <summary>
    /// Gets the safe contract-defined temporal cursor.
    /// </summary>
    public string? ContractCursor { get; } = ValidateOptionalCursor(AnchorKind, ContractCursor, ContractCursorKind);

    private static string ValidateKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value switch
        {
            TimestampKind or SafeSourcePositionKind or ProjectionCursorKind or ContractCursorKind => value,
            _ => throw new ArgumentException("Unsupported temporal anchor kind.", nameof(value)),
        };
    }

    private static DateTimeOffset? ValidateTimestamp(string kind, DateTimeOffset? value)
    {
        if (kind == TimestampKind && value is null)
        {
            throw new ArgumentException("Timestamp anchors require a timestamp.", nameof(value));
        }

        if (kind != TimestampKind && value is not null)
        {
            throw new ArgumentException("Only timestamp anchors may carry a timestamp.", nameof(value));
        }

        return value;
    }

    private static long? ValidatePosition(string kind, long? value)
    {
        if (kind == SafeSourcePositionKind && value is null)
        {
            throw new ArgumentException("Source-position anchors require a safe source position.", nameof(value));
        }

        if (kind != SafeSourcePositionKind && value is not null)
        {
            throw new ArgumentException("Only source-position anchors may carry a safe source position.", nameof(value));
        }

        if (value is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 1);
        }

        return value;
    }

    private static string? ValidateOptionalCursor(string kind, string? value, string cursorKind)
    {
        bool required = kind == cursorKind;
        if (required && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cursor anchors require a cursor value.", nameof(value));
        }

        if (!required && value is not null)
        {
            throw new ArgumentException("Only the matching cursor anchor may carry this cursor value.", nameof(value));
        }

        return value;
    }
}
