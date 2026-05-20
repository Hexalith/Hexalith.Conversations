// <copyright file="ConversationQueryCursor.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Encodes and validates opaque list continuation cursors.
/// </summary>
internal static class ConversationQueryCursor
{
    private static readonly byte[] SigningKey = Encoding.UTF8.GetBytes("Hexalith.Conversations.QueryCursor.V1");
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    public static string Encode(
        TenantId tenantId,
        string callerPrincipalId,
        ConversationListFilterV1 filter,
        int offset,
        DateTimeOffset issuedAt)
    {
        CursorPayload payload = new(
            1,
            tenantId.Value,
            callerPrincipalId,
            Fingerprint(filter),
            offset,
            issuedAt.UtcDateTime);
        string payloadJson = JsonSerializer.Serialize(payload);
        string signature = Sign(payloadJson);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payloadJson}.{signature}"));
    }

    public static string EncodeForTests(
        TenantId tenantId,
        string callerPrincipalId,
        ConversationListFilterV1 filter,
        int offset,
        DateTimeOffset issuedAt)
        => Encode(tenantId, callerPrincipalId, filter, offset, issuedAt);

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
            dateFrom = filter.DateFrom?.UtcDateTime,
            dateTo = filter.DateTo?.UtcDateTime,
            recentActivityAfter = filter.RecentActivityAfter?.UtcDateTime,
            participant = filter.ParticipantPartyId?.Value,
        });
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public static bool TryDecode(string? cursor, out DecodedCursor decoded)
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
            if (payload is null || payload.Version != 1 || payload.Offset < 0)
            {
                return false;
            }

            decoded = new DecodedCursor(payload);
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
    }

    private static string Sign(string payloadJson)
    {
        using HMACSHA256 hmac = new(SigningKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson)));
    }

    internal readonly record struct DecodedCursor(CursorPayload Payload)
    {
        public int Offset => Payload.Offset;

        public bool Matches(TenantId tenantId, string callerPrincipalId, ConversationListFilterV1 filter, DateTimeOffset now)
            => Payload.TenantId == tenantId.Value
                && string.Equals(Payload.CallerPrincipalId, callerPrincipalId, StringComparison.Ordinal)
                && Payload.FilterFingerprint == Fingerprint(filter)
                && now.ToUniversalTime() - new DateTimeOffset(Payload.IssuedAt, TimeSpan.Zero) <= MaxAge;
    }

    internal sealed record CursorPayload(
        int Version,
        string TenantId,
        string CallerPrincipalId,
        string FilterFingerprint,
        int Offset,
        DateTime IssuedAt);
}
