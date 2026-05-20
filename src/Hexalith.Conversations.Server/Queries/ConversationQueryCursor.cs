// <copyright file="ConversationQueryCursor.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;

using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Encodes and validates opaque list continuation cursors.
/// </summary>
/// <remarks>
/// Cursors are HMAC-SHA256 signed with a per-deployment key supplied via <see cref="ConversationQueryCursorOptions"/>.
/// The payload binds tenant, caller principal, filter fingerprint, sort version, projection generation token, age, and
/// the signing key id. A mismatch on any field, including a future-dated <c>IssuedAt</c>, fails closed.
/// </remarks>
public sealed class ConversationQueryCursor
{
    /// <summary>
    /// Identifies the current sort order. Increment when the list ordering rule changes so cursors issued under
    /// the prior order fail closed instead of silently producing duplicated or skipped rows.
    /// </summary>
    public const int SortVersion = 1;

    private readonly byte[] _signingKey;
    private readonly string _keyId;
    private readonly TimeSpan _maxAge;
    private readonly int _maxOffset;

    public ConversationQueryCursor(IOptions<ConversationQueryCursorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ConversationQueryCursorOptions value = options.Value
            ?? throw new ArgumentException("Cursor options must be configured.", nameof(options));

        if (value.SigningKey is null || value.SigningKey.Length < 32)
        {
            throw new ArgumentException(
                "Cursor signing key must be configured with at least 32 bytes of random material.",
                nameof(options));
        }

        if (string.IsNullOrWhiteSpace(value.KeyId))
        {
            throw new ArgumentException("Cursor signing key id must be configured.", nameof(options));
        }

        if (value.MaxAge <= TimeSpan.Zero)
        {
            throw new ArgumentException("Cursor max age must be positive.", nameof(options));
        }

        if (value.MaxOffset < 1)
        {
            throw new ArgumentException("Cursor max offset must be positive.", nameof(options));
        }

        _signingKey = (byte[])value.SigningKey.Clone();
        _keyId = value.KeyId;
        _maxAge = value.MaxAge;
        _maxOffset = value.MaxOffset;
    }

    public string Encode(
        TenantId tenantId,
        string callerPrincipalId,
        ConversationListFilterV1 filter,
        int offset,
        string projectionGenerationToken,
        DateTimeOffset issuedAt)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(callerPrincipalId);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionGenerationToken);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        CursorPayload payload = new(
            1,
            _keyId,
            tenantId.Value,
            callerPrincipalId,
            Fingerprint(filter),
            SortVersion,
            projectionGenerationToken,
            offset,
            issuedAt.UtcDateTime);
        string payloadJson = JsonSerializer.Serialize(payload);
        string signature = Sign(payloadJson);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payloadJson}.{signature}"));
    }

    public string EncodeForTests(
        TenantId tenantId,
        string callerPrincipalId,
        ConversationListFilterV1 filter,
        int offset,
        string projectionGenerationToken,
        DateTimeOffset issuedAt)
        => Encode(tenantId, callerPrincipalId, filter, offset, projectionGenerationToken, issuedAt);

    public static string Fingerprint(ConversationListFilterV1 filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        string input = JsonSerializer.Serialize(new
        {
            businessSystem = filter.BusinessReference?.System,
            businessValue = filter.BusinessReference?.Value,
            project = filter.ProjectId?.Value,
            folder = filter.FolderId?.Value,
            lifecycle = filter.LifecycleState,
            projectedAtFrom = filter.ProjectedAtFrom?.UtcDateTime,
            projectedAtTo = filter.ProjectedAtTo?.UtcDateTime,
            recentActivityAfter = filter.RecentActivityAfter?.UtcDateTime,
            participant = filter.ParticipantPartyId?.Value,
            sortVersion = SortVersion,
        });
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    internal bool TryDecode(string? cursor, out DecodedCursor decoded)
    {
        decoded = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            string decodedText = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            int separator = decodedText.LastIndexOf('.');
            if (separator <= 0 || separator >= decodedText.Length - 1)
            {
                return false;
            }

            string payloadJson = decodedText[..separator];
            string signature = decodedText[(separator + 1)..];
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(Sign(payloadJson))))
            {
                return false;
            }

            CursorPayload? payload = JsonSerializer.Deserialize<CursorPayload>(payloadJson);
            if (payload is null
                || payload.Version != 1
                || payload.KeyId != _keyId
                || payload.SortVersion != SortVersion
                || payload.Offset < 0
                || payload.Offset > _maxOffset
                || string.IsNullOrEmpty(payload.TenantId)
                || string.IsNullOrEmpty(payload.CallerPrincipalId)
                || string.IsNullOrEmpty(payload.FilterFingerprint)
                || string.IsNullOrEmpty(payload.ProjectionGenerationToken))
            {
                return false;
            }

            decoded = new DecodedCursor(payload, _maxAge);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private string Sign(string payloadJson)
    {
        using HMACSHA256 hmac = new(_signingKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson)));
    }

    internal readonly record struct DecodedCursor(CursorPayload Payload, TimeSpan MaxAge)
    {
        public int Offset => Payload.Offset;

        public string ProjectionGenerationToken => Payload.ProjectionGenerationToken;

        public bool Matches(
            TenantId tenantId,
            string callerPrincipalId,
            ConversationListFilterV1 filter,
            string projectionGenerationToken,
            DateTimeOffset now)
        {
            if (Payload.TenantId != tenantId.Value
                || !string.Equals(Payload.CallerPrincipalId, callerPrincipalId, StringComparison.Ordinal)
                || Payload.FilterFingerprint != Fingerprint(filter)
                || !string.Equals(Payload.ProjectionGenerationToken, projectionGenerationToken, StringComparison.Ordinal))
            {
                return false;
            }

            DateTime issuedAtUtc = DateTime.SpecifyKind(Payload.IssuedAt, DateTimeKind.Utc);
            TimeSpan age = now.ToUniversalTime() - new DateTimeOffset(issuedAtUtc, TimeSpan.Zero);
            return age >= TimeSpan.Zero && age <= MaxAge;
        }
    }

    internal sealed record CursorPayload(
        int Version,
        string KeyId,
        string TenantId,
        string CallerPrincipalId,
        string FilterFingerprint,
        int SortVersion,
        string ProjectionGenerationToken,
        int Offset,
        DateTime IssuedAt);
}
