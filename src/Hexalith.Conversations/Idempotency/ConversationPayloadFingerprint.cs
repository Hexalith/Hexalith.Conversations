// <copyright file="ConversationPayloadFingerprint.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Stores a bounded hash of canonical command meaning.
/// </summary>
/// <param name="Algorithm">The fingerprint algorithm identifier.</param>
/// <param name="Value">The lowercase hexadecimal hash value.</param>
public sealed record ConversationPayloadFingerprint(string Algorithm, string Value)
{
    /// <summary>
    /// Gets the supported fingerprint algorithm identifier.
    /// </summary>
    public const string Sha256Algorithm = "sha256";

    /// <summary>
    /// Gets the fingerprint algorithm identifier.
    /// </summary>
    public string Algorithm { get; } = ValidateRequired(Algorithm, nameof(Algorithm));

    /// <summary>
    /// Gets the lowercase hexadecimal hash value.
    /// </summary>
    public string Value { get; } = ValidateRequired(Value, nameof(Value));

    /// <summary>
    /// Creates a fingerprint from canonical key/value parts.
    /// </summary>
    /// <param name="parts">The canonical key/value parts.</param>
    /// <returns>The bounded fingerprint.</returns>
    public static ConversationPayloadFingerprint FromParts(IEnumerable<KeyValuePair<string, string?>> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        StringBuilder canonical = new();
        foreach (KeyValuePair<string, string?> part in parts.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            string value = NormalizeSafeText(part.Key, part.Value ?? string.Empty);
            canonical
                .Append(part.Key.Length)
                .Append(':')
                .Append(part.Key)
                .Append('=')
                .Append(value.Length)
                .Append(':')
                .Append(value)
                .Append('\n');
        }

        byte[] bytes = Encoding.UTF8.GetBytes(canonical.ToString());
        byte[] hash = SHA256.HashData(bytes);
        return new ConversationPayloadFingerprint(Sha256Algorithm, Convert.ToHexString(hash).ToLowerInvariant());
    }

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static string NormalizeSafeText(string key, string value)
        => IsSafeFreeTextField(key)
            ? value.Normalize(NormalizationForm.FormC)
            : value;

    private static bool IsSafeFreeTextField(string key)
        => key == "label"
            || key == "business.value"
            || key == "text"
            || key.StartsWith("attribute.", StringComparison.Ordinal);
}
