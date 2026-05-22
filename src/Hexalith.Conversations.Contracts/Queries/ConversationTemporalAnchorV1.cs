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
/// <param name="projectionVersion">The projection version that participates in a composite authoritative anchor.</param>
/// <param name="supportingTimestamp">A supporting display/correlation timestamp; never the authoritative legal anchor.</param>
public sealed record ConversationTemporalAnchorV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string AnchorKind,
    DateTimeOffset? Timestamp = null,
    long? SafeSourcePosition = null,
    string? ProjectionCursor = null,
    string? ContractCursor = null,
    long? ProjectionVersion = null,
    DateTimeOffset? SupportingTimestamp = null)
{
    public const string TimestampKind = "timestamp";
    public const string SafeSourcePositionKind = "safe_source_position";
    public const string ProjectionCursorKind = "projection_cursor";
    public const string ContractCursorKind = "contract_cursor";
    public const string CompositeCursorKind = "composite_cursor";

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
    public string? ProjectionCursor { get; } = ValidateOptionalCursor(
        AnchorKind,
        ProjectionCursor,
        ProjectionCursorKind,
        SafeSourcePosition,
        ProjectionVersion);

    /// <summary>
    /// Gets the safe contract-defined temporal cursor.
    /// </summary>
    public string? ContractCursor { get; } = ValidateOptionalCursor(
        AnchorKind,
        ContractCursor,
        ContractCursorKind,
        SafeSourcePosition,
        ProjectionVersion);

    /// <summary>
    /// Gets the projection version that participates in a composite authoritative anchor.
    /// </summary>
    public long? ProjectionVersion { get; } = ValidateProjectionVersion(AnchorKind, ProjectionVersion);

    /// <summary>
    /// Gets a supporting display/correlation timestamp; never the authoritative legal anchor.
    /// </summary>
    public DateTimeOffset? SupportingTimestamp { get; } = ValidateSupportingTimestamp(AnchorKind, SupportingTimestamp);

    private static string ValidateKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value switch
        {
            TimestampKind or SafeSourcePositionKind or ProjectionCursorKind or ContractCursorKind or CompositeCursorKind => value,
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
        bool required = kind is SafeSourcePositionKind or CompositeCursorKind;
        if (required && value is null)
        {
            throw new ArgumentException("Source-position anchors require a safe source position.", nameof(value));
        }

        if (!required && value is not null)
        {
            throw new ArgumentException("Only source-position anchors may carry a safe source position.", nameof(value));
        }

        if (value is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 1);
        }

        return value;
    }

    private static string? ValidateOptionalCursor(
        string kind,
        string? value,
        string cursorKind,
        long? safeSourcePosition,
        long? projectionVersion)
    {
        bool required = kind == cursorKind || kind == CompositeCursorKind;
        if (required && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cursor anchors require a cursor value.", nameof(value));
        }

        if (!required && value is not null)
        {
            throw new ArgumentException("Only the matching cursor anchor may carry this cursor value.", nameof(value));
        }

        if (kind == CompositeCursorKind
            && cursorKind == ContractCursorKind
            && !ContractCursorMatchesCompositeAnchor(value, safeSourcePosition, projectionVersion))
        {
            throw new ArgumentException("Composite temporal cursors must carry a valid safe position and projection version.", nameof(value));
        }

        return value;
    }

    private static long? ValidateProjectionVersion(string kind, long? value)
    {
        if (kind == CompositeCursorKind && value is null)
        {
            throw new ArgumentException("Composite temporal anchors require a projection version.", nameof(value));
        }

        if (kind != CompositeCursorKind && value is not null)
        {
            throw new ArgumentException("Only composite temporal anchors may carry a projection version.", nameof(value));
        }

        if (value is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 1);
        }

        return value;
    }

    private static DateTimeOffset? ValidateSupportingTimestamp(string kind, DateTimeOffset? value)
    {
        if (kind != CompositeCursorKind && value is not null)
        {
            throw new ArgumentException("Only composite temporal anchors may carry a supporting timestamp.", nameof(value));
        }

        if (value <= DateTimeOffset.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Supporting timestamp must be greater than DateTimeOffset.MinValue.");
        }

        return value;
    }

    private static bool ContractCursorMatchesCompositeAnchor(
        string? cursor,
        long? expectedPosition,
        long? expectedProjectionVersion)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        string[] parts = cursor.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 6
            && string.Equals(parts[0], "temporal", StringComparison.Ordinal)
            && string.Equals(parts[1], "v1", StringComparison.Ordinal)
            && string.Equals(parts[2], "pos", StringComparison.Ordinal)
            && long.TryParse(parts[3], out long parsedPosition)
            && parsedPosition > 0
            && (expectedPosition is null || expectedPosition == parsedPosition)
            && string.Equals(parts[4], "projection", StringComparison.Ordinal)
            && long.TryParse(parts[5], out long parsedProjectionVersion)
            && parsedProjectionVersion > 0
            && (expectedProjectionVersion is null || expectedProjectionVersion == parsedProjectionVersion);
    }
}
