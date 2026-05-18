// <copyright file="PrefixedIdentifierJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Conversations.Contracts.Serialization;

/// <summary>
/// Base converter for typed identifiers serialized as URN-style prefixed strings.
/// The prefix prevents silent cross-type substitution between identifier families on the wire.
/// </summary>
internal abstract class PrefixedIdentifierJsonConverter<T> : JsonConverter<T>
    where T : notnull
{
    /// <summary>
    /// Gets the canonical wire prefix for this identifier family (without trailing colon).
    /// </summary>
    protected abstract string Prefix { get; }

    public sealed override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{typeToConvert.Name} must be encoded as a JSON string.");
        }

        string raw = reader.GetString() ?? throw new JsonException($"{typeToConvert.Name} cannot be null.");
        string expectedPrefix = $"{Prefix}:";
        if (!raw.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new JsonException($"{typeToConvert.Name} payload must start with the '{expectedPrefix}' prefix.");
        }

        string value = raw[expectedPrefix.Length..];
        try
        {
            return Create(value);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"{typeToConvert.Name} payload is malformed: {ex.Message}", ex);
        }
    }

    public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue($"{Prefix}:{GetValue(value)}");

    protected abstract T Create(string value);

    protected abstract string GetValue(T value);
}
