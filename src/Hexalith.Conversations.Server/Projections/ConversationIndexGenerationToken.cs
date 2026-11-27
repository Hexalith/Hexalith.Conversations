// <copyright file="ConversationIndexGenerationToken.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Computes the canonical, order-independent SHA-256 generation token over a tenant's conversation summaries
/// using a bounded streaming <see cref="IncrementalHash"/> so neither reads nor writes allocate full serialized
/// index buffers.
/// </summary>
internal static class ConversationIndexGenerationToken
{
    /// <summary>The token returned for an empty summary set.</summary>
    internal const string EmptyToken = "empty";

    /// <summary>
    /// Validates that a persisted generation token is a 64-character lowercase hexadecimal SHA-256 digest.
    /// </summary>
    /// <param name="token">The candidate token.</param>
    /// <returns><see langword="true"/> when the token is a valid 64-char lowercase hex string; otherwise <see langword="false"/>.</returns>
    internal static bool IsValidPersistedToken(string? token)
    {
        if (token is not { Length: 64 })
        {
            return false;
        }

        for (int index = 0; index < token.Length; index++)
        {
            char c = token[index];
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the supplied summaries are already ordered ordinally by <see cref="ConversationId.Value"/>.
    /// </summary>
    /// <param name="summaries">The summaries to inspect.</param>
    /// <returns><see langword="true"/> when already non-decreasing in ordinal order.</returns>
    internal static bool IsOrdered(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        for (int index = 1; index < summaries.Count; index++)
        {
            if (StringComparer.Ordinal.Compare(
                summaries[index - 1].ConversationId.Value,
                summaries[index].ConversationId.Value) > 0)
            {
                return false;
            }
        }

        return true;
    }

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        SkipValidation = true,
    };

    /// <summary>
    /// Computes the deterministic lowercase-hex SHA-256 generation token over all fields of the supplied summaries
    /// ordered ordinally by <see cref="ConversationId.Value"/>.
    /// </summary>
    /// <param name="summaries">The tenant-scoped conversation summaries.</param>
    /// <returns>The canonical generation token, or <see cref="EmptyToken"/> when empty.</returns>
    internal static string Compute(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        int count = summaries.Count;
        if (count == 0)
        {
            return EmptyToken;
        }

        IReadOnlyList<ConversationSummaryProjectionV1> ordered = EnsureOrdered(summaries);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using IncrementalHashBufferWriter buffer = new(hash);
        using Utf8JsonWriter writer = new(buffer, WriterOptions);

        writer.WriteStartArray();
        for (int index = 0; index < ordered.Count; index++)
        {
            WriteSummary(writer, ordered[index]);
            writer.Flush();
        }

        writer.WriteEndArray();
        writer.Flush();
        buffer.Flush();

        Span<byte> digest = stackalloc byte[32];
        _ = hash.TryGetHashAndReset(digest, out _);
        return Convert.ToHexStringLower(digest);
    }

    private static IReadOnlyList<ConversationSummaryProjectionV1> EnsureOrdered(
        IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        bool alreadyOrdered = true;
        for (int index = 1; index < summaries.Count; index++)
        {
            if (StringComparer.Ordinal.Compare(
                summaries[index - 1].ConversationId.Value,
                summaries[index].ConversationId.Value) > 0)
            {
                alreadyOrdered = false;
                break;
            }
        }

        if (alreadyOrdered)
        {
            return summaries;
        }

        ConversationSummaryProjectionV1[] sorted = new ConversationSummaryProjectionV1[summaries.Count];
        for (int index = 0; index < summaries.Count; index++)
        {
            sorted[index] = summaries[index];
        }

        Array.Sort(
            sorted,
            static (left, right) => StringComparer.Ordinal.Compare(left.ConversationId.Value, right.ConversationId.Value));
        return sorted;
    }

    private static void WriteSummary(Utf8JsonWriter writer, ConversationSummaryProjectionV1 summary)
    {
        writer.WriteStartObject();
        writer.WriteNumber("sv"u8, summary.SchemaVersion.Value);
        writer.WriteString("t"u8, summary.TenantId.Value);
        writer.WriteString("c"u8, summary.ConversationId.Value);
        WriteFreshness(writer, summary.Freshness);
        writer.WriteString("ls"u8, summary.LifecycleState);
        WriteNullableString(writer, "lb"u8, summary.Label);
        WriteBusinessReference(writer, summary.BusinessReference);
        WriteNullableString(writer, "p"u8, summary.ProjectId?.Value);
        WriteNullableString(writer, "f"u8, summary.FolderId?.Value);

        writer.WriteStartArray("pp"u8);
        IReadOnlyList<PartyId> participants = summary.ParticipantPartyIds;
        for (int index = 0; index < participants.Count; index++)
        {
            writer.WriteStringValue(participants[index].Value);
        }

        writer.WriteEndArray();
        writer.WriteNumber("mc"u8, summary.MessageCount);
        writer.WriteNumber("fc"u8, summary.FileReferenceCount);
        WriteProviderCorrelation(writer, summary.ProviderCorrelation);
        WriteSearchTrustPreview(writer, summary.SearchTrustPreview);
        writer.WriteEndObject();
    }

    private static void WriteFreshness(Utf8JsonWriter writer, ProjectionFreshnessV1 freshness)
    {
        writer.WriteStartObject("fr"u8);
        writer.WriteNumber("sv"u8, freshness.ProjectionContractSchemaVersion.Value);
        writer.WriteString("cur"u8, freshness.ProjectionCursor);
        writer.WriteNumber("pos"u8, freshness.LastAppliedEventPosition);
        writer.WriteNumber("et"u8, freshness.LastAppliedEventTimestamp.UtcTicks);
        writer.WriteNumber("eo"u8, freshness.LastAppliedEventTimestamp.Offset.Ticks);
        writer.WriteNumber("gt"u8, freshness.ProjectionGeneratedAt.UtcTicks);
        writer.WriteNumber("go"u8, freshness.ProjectionGeneratedAt.Offset.Ticks);
        if (freshness.LagDuration.HasValue)
        {
            writer.WriteNumber("lag"u8, freshness.LagDuration.Value.Ticks);
        }
        else
        {
            writer.WriteNull("lag"u8);
        }

        writer.WriteBoolean("st"u8, freshness.IsStale);
        writer.WriteString("fs"u8, freshness.FreshnessState.Value);
        writer.WriteString("rc"u8, freshness.ReasonCode.Value);
        writer.WriteEndObject();
    }

    private static void WriteBusinessReference(Utf8JsonWriter writer, BusinessReference? reference)
    {
        if (reference is null)
        {
            writer.WriteNull("br"u8);
            return;
        }

        writer.WriteStartObject("br"u8);
        writer.WriteString("s"u8, reference.System);
        writer.WriteString("v"u8, reference.Value);
        writer.WriteEndObject();
    }

    private static void WriteProviderCorrelation(Utf8JsonWriter writer, ProviderCorrelationMetadata? correlation)
    {
        if (correlation is null)
        {
            writer.WriteNull("pc"u8);
            return;
        }

        writer.WriteStartObject("pc"u8);
        writer.WriteString("pn"u8, correlation.ProviderName);
        writer.WriteString("pt"u8, correlation.ProviderType);
        writer.WriteNumber("msv"u8, correlation.MetadataSchemaVersion.Value);
        WriteNullableString(writer, "psr"u8, correlation.ProviderSessionReference);
        WriteNullableString(writer, "prr"u8, correlation.ProviderResponseReference);
        if (correlation.ExtensionData is null)
        {
            writer.WriteNull("ed"u8);
        }
        else
        {
            writer.WriteStartObject("ed"u8);
            if (correlation.ExtensionData.Count > 0)
            {
                KeyValuePair<string, string>[] entries = [.. correlation.ExtensionData];
                Array.Sort(entries, static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));
                for (int index = 0; index < entries.Length; index++)
                {
                    writer.WriteString(entries[index].Key, entries[index].Value);
                }
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteSearchTrustPreview(Utf8JsonWriter writer, ConversationSearchTrustPreviewV1 preview)
    {
        writer.WriteStartObject("stp"u8);
        writer.WriteString("fs"u8, preview.FreshnessState.Value);
        writer.WriteString("frc"u8, preview.FreshnessReasonCode.Value);
        writer.WriteString("rs"u8, preview.RedactionState.Value);
        writer.WriteString("prs"u8, preview.ParticipantResolutionState.Value);
        writer.WriteString("ca"u8, preview.CitationAvailability.Value);
        writer.WriteString("ar"u8, preview.AuditReadiness.Value);
        writer.WriteString("vs"u8, preview.VerificationState.Value);
        writer.WriteString("ms"u8, preview.MatchSource.Value);
        writer.WriteString("wv"u8, preview.WhyVisible);
        writer.WriteEndObject();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, ReadOnlySpan<byte> propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }
}
