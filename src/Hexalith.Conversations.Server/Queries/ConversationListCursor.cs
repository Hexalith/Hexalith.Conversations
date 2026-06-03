// <copyright file="ConversationListCursor.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Client.Queries;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Conversation-specific inputs for the list continuation cursor produced and validated by the platform
/// <see cref="IQueryCursorCodec"/> (integrity/signing) plus <see cref="QueryCursorScope"/> (binding).
/// </summary>
/// <remarks>
/// <para>
/// This type owns only the domain-shaped pieces the SDK codec does not: the stable filter fingerprint, the
/// sort-version discriminator, the canonical <em>scope</em> string (tenant / caller / filter / sort), and the
/// opaque <em>position</em> payload (offset, issued-at, projection-generation token). The HMAC signing and
/// tamper detection that the retired hand-rolled cursor performed now belong entirely to the codec's
/// ASP.NET Core Data Protection layer.
/// </para>
/// <para>
/// <b>Why the projection-generation token rides in the position rather than the scope.</b> The scope must be
/// rebuilt identically on decode <em>before</em> any projection read so an integrity failure (tamper /
/// key rotation) and a tenant/caller/filter mismatch both fail closed with zero projection rows read — the
/// behavior the cursor fail-closed suite pins. The projection-generation token, however, is only knowable
/// <em>after</em> the projection read (it is derived from the returned rows), so it cannot participate in the
/// pre-read scope. It is therefore carried in the protected position and re-compared as a domain check after
/// the read — preserving the identical fail-closed outcome of the prior <c>DecodedCursor.Matches</c> check.
/// </para>
/// </remarks>
public static class ConversationListCursor
{
    /// <summary>
    /// Identifies the current sort order. Increment when the list ordering rule changes so cursors issued
    /// under the prior order fail closed instead of silently producing duplicated or skipped rows.
    /// </summary>
    public const int SortVersion = 1;

    /// <summary>
    /// The stable query-type discriminator the codec binds each cursor to. A cursor minted for the list query
    /// can never be replayed against another query type.
    /// </summary>
    public const string QueryType = "conversation-list";

    private static readonly JsonSerializerOptions s_positionOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Builds the canonical pre-read cursor scope binding tenant, caller, filter fingerprint, and sort
    /// version. Rebuilt identically on encode and decode so any change to a binding field yields
    /// <c>wrong-scope</c> from <see cref="IQueryCursorCodec.TryDecode"/>.
    /// </summary>
    /// <param name="tenantId">The trusted tenant binding.</param>
    /// <param name="callerPrincipalId">The caller principal identity.</param>
    /// <param name="filter">The exact-match tenant-scoped filter.</param>
    /// <returns>The canonical scope string.</returns>
    public static string BuildScope(TenantId tenantId, string callerPrincipalId, ConversationListFilterV1 filter)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(callerPrincipalId);
        ArgumentNullException.ThrowIfNull(filter);

        return QueryCursorScope.Create()
            .Add("tenant", tenantId.Value)
            .Add("caller", callerPrincipalId)
            .Add("filter", Fingerprint(filter))
            .Add("sort", SortVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Build();
    }

    /// <summary>
    /// Encodes the opaque cursor position: the page offset, the issue instant, and the projection-generation
    /// token re-checked after the projection read.
    /// </summary>
    /// <param name="offset">The page offset.</param>
    /// <param name="issuedAt">The instant the cursor was issued.</param>
    /// <param name="projectionGenerationToken">The projection-generation binding token.</param>
    /// <returns>The position string passed to <see cref="IQueryCursorCodec.Encode"/>.</returns>
    public static string EncodePosition(int offset, DateTimeOffset issuedAt, string projectionGenerationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionGenerationToken);

        return JsonSerializer.Serialize(
            new CursorPosition(offset, issuedAt.UtcTicks, projectionGenerationToken),
            s_positionOptions);
    }

    /// <summary>
    /// Parses an opaque cursor position previously produced by <see cref="EncodePosition"/>. A malformed or
    /// out-of-range position is rejected so a forged-but-decodable position still fails closed.
    /// </summary>
    /// <param name="position">The decoded position string from <see cref="IQueryCursorCodec.TryDecode"/>.</param>
    /// <param name="decoded">The parsed position when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the position is well-formed; otherwise <see langword="false"/>.</returns>
    public static bool TryParsePosition(string? position, out ConversationListCursorPosition decoded)
    {
        decoded = default;
        if (string.IsNullOrWhiteSpace(position))
        {
            return false;
        }

        try
        {
            CursorPosition? parsed = JsonSerializer.Deserialize<CursorPosition>(position, s_positionOptions);
            if (parsed is null
                || parsed.Offset < 0
                || string.IsNullOrEmpty(parsed.Generation))
            {
                return false;
            }

            decoded = new ConversationListCursorPosition(
                parsed.Offset,
                new DateTimeOffset(parsed.IssuedAtUtcTicks, TimeSpan.Zero),
                parsed.Generation);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Computes the stable filter fingerprint folded into the cursor scope. A change to any matched filter
    /// dimension yields a different fingerprint, so a cursor presented under a different filter set fails
    /// closed via <c>wrong-scope</c>.
    /// </summary>
    /// <param name="filter">The exact-match tenant-scoped filter.</param>
    /// <returns>An uppercase hex SHA-256 fingerprint.</returns>
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
            redactionState = filter.RedactionState?.Value,
            freshnessState = filter.FreshnessState?.Value,
            auditReadiness = filter.AuditReadiness?.Value,
            verificationState = filter.VerificationState?.Value,
            sortVersion = SortVersion,
        });
        byte[] bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private sealed record CursorPosition(int Offset, long IssuedAtUtcTicks, string Generation);
}

/// <summary>
/// The decoded conversation list cursor position: page offset, issue instant, and projection-generation
/// token re-checked after the projection read.
/// </summary>
/// <param name="Offset">The page offset.</param>
/// <param name="IssuedAt">The instant the cursor was issued.</param>
/// <param name="ProjectionGenerationToken">The projection-generation binding token.</param>
public readonly record struct ConversationListCursorPosition(
    int Offset,
    DateTimeOffset IssuedAt,
    string ProjectionGenerationToken);
