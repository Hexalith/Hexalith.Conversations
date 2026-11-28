// <copyright file="ConversationProjectionJsonValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Validates JSON tokens shared by the streaming projection readers.</summary>
internal static class ConversationProjectionJsonValidation
{
    /// <summary>Checks whether the persisted property binding matches the streaming readers.</summary>
    internal static bool UsesStreamingPropertyBinding(JsonSerializerOptions options)
        => options.PropertyNamingPolicy == JsonNamingPolicy.CamelCase
            && options.PropertyNameCaseInsensitive
            && options.Converters.Count == 0
            && options.ReferenceHandler is null
            && (options.TypeInfoResolver is null
                || (options.TypeInfoResolver.GetType() == typeof(DefaultJsonTypeInfoResolver)
                    && options.TypeInfoResolver is DefaultJsonTypeInfoResolver { Modifiers.Count: 0 }))
            && options.UnmappedMemberHandling == JsonUnmappedMemberHandling.Skip
            && !options.RespectRequiredConstructorParameters
            && !options.RespectNullableAnnotations
            && options.AllowDuplicateProperties;

    /// <summary>Compares a property name to its lowercase ASCII spelling without allocating on ordinary names.</summary>
    internal static bool PropertyNameEqualsIgnoreCase(ref Utf8JsonReader reader, ReadOnlySpan<byte> lowerAsciiName)
    {
        if (!reader.HasValueSequence && !reader.ValueIsEscaped)
        {
            ReadOnlySpan<byte> name = reader.ValueSpan;
            if (Ascii.EqualsIgnoreCase(name, lowerAsciiName))
            {
                return true;
            }

            if (Ascii.IsValid(name))
            {
                return false;
            }
        }

        Span<char> expected = stackalloc char[lowerAsciiName.Length];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = (char)lowerAsciiName[index];
        }

        return reader.GetString().AsSpan().Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Validates that a scalar can be decoded as a nullable string.</summary>
    internal static void ValidateNullableString(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a JSON string or null.");
        }

        ValidateStringEncoding(ref reader);
    }

    /// <summary>Validates string or property-name encoding, keeping unescaped ASCII allocation free.</summary>
    internal static void ValidateStringEncoding(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped || !Ascii.IsValid(reader.ValueSpan))
        {
            _ = reader.GetString();
        }
    }
}
