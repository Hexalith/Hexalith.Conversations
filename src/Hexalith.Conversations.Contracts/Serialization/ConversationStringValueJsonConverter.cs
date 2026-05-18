// <copyright file="ConversationStringValueJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Conversations.Contracts.Serialization;

internal abstract class ConversationStringValueJsonConverter<T> : JsonConverter<T>
    where T : notnull
{
    public sealed override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{typeToConvert.Name} must be encoded as a JSON string.");
        }

        return Create(reader.GetString() ?? throw new JsonException($"{typeToConvert.Name} cannot be null."));
    }

    public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(GetValue(value));

    protected abstract T Create(string value);

    protected abstract string GetValue(T value);
}
