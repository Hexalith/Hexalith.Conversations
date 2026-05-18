// <copyright file="ConversationIntValueJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Conversations.Contracts.Serialization;

internal abstract class ConversationIntValueJsonConverter<T> : JsonConverter<T>
    where T : notnull
{
    public sealed override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw new JsonException($"{typeToConvert.Name} must be encoded as a JSON integer (no fractional, exponent, or string values).");
        }

        try
        {
            return Create(value);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException($"{typeToConvert.Name} payload is out of range: {ex.Message}", ex);
        }
    }

    public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteNumberValue(GetValue(value));

    protected abstract T Create(int value);

    protected abstract int GetValue(T value);
}
