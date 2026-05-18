// <copyright file="ConversationErrorCategory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Defines stable broad machine-readable Conversations error categories.
/// </summary>
[JsonConverter(typeof(ConversationErrorCategoryJsonConverter))]
public sealed record ConversationErrorCategory
{
    /// <summary>
    /// Gets the validation category.
    /// </summary>
    public static ConversationErrorCategory Validation { get; } = new("validation");

    /// <summary>
    /// Gets the authorization category.
    /// </summary>
    public static ConversationErrorCategory Authorization { get; } = new("authorization");

    /// <summary>
    /// Gets the conflict category.
    /// </summary>
    public static ConversationErrorCategory Conflict { get; } = new("conflict");

    /// <summary>
    /// Gets the freshness category.
    /// </summary>
    public static ConversationErrorCategory Freshness { get; } = new("freshness");

    /// <summary>
    /// Gets the audit category.
    /// </summary>
    public static ConversationErrorCategory Audit { get; } = new("audit");

    /// <summary>
    /// Gets the versioning category.
    /// </summary>
    public static ConversationErrorCategory Versioning { get; } = new("versioning");

    /// <summary>
    /// Gets the hidden target category.
    /// </summary>
    public static ConversationErrorCategory Hidden { get; } = new("hidden");

    private static readonly IReadOnlyDictionary<string, ConversationErrorCategory> KnownCategories =
        new[]
        {
            Validation,
            Authorization,
            Conflict,
            Freshness,
            Audit,
            Versioning,
            Hidden,
        }.ToDictionary(category => category.Value, StringComparer.Ordinal);

    private ConversationErrorCategory(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the machine-readable category value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported error category.
    /// </summary>
    /// <param name="value">The machine-readable category value.</param>
    /// <returns>The matching supported error category.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or unsupported.</exception>
    public static ConversationErrorCategory Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownCategories.TryGetValue(value, out ConversationErrorCategory? category)
            ? category
            : throw new ArgumentException($"Unsupported conversation error category '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
