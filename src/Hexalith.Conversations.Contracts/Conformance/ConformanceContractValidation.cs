// <copyright file="ConformanceContractValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Validates conformance contract fields without leaking protected content.
/// </summary>
/// <remarks>
/// Free-text fields reuse the canonical <see cref="ConversationError"/> disclosure blocklist so conformance
/// output cannot drift apart from the shared error guardrails. Tokens are additionally constrained to a
/// bounded closed character set whose forbidden characters (<c>:</c>, <c>\</c>, <c>/</c>) already exclude
/// prefixed identifiers and storage syntax, so legitimate machine identifiers such as <c>create-conversation</c>
/// and <c>FR73</c> remain valid while protected values cannot pass.
/// </remarks>
internal static class ConformanceContractValidation
{
    private const int MaxSafeTextLength = 512;
    private const int MaxSafeTokenLength = 128;

    internal static string RequiredSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaxSafeTextLength)
        {
            throw new ArgumentException("Value must be within the bounded content-safe length.", parameterName);
        }

        if (value.Any(char.IsControl))
        {
            throw new ArgumentException("Value must remain content-safe and cannot contain control characters.", parameterName);
        }

        ConversationError.EnsureContentSafe(value, parameterName);
        return value;
    }

    internal static string RequiredSafeToken(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaxSafeTokenLength || value.Any(static c => !IsTokenCharacter(c)))
        {
            throw new ArgumentException("Value must be a bounded content-safe identifier.", parameterName);
        }

        ConversationError.EnsureContentSafe(value, parameterName);
        return value;
    }

    internal static string? OptionalSafeToken(string? value, string parameterName)
        => value is null ? null : RequiredSafeToken(value, parameterName);

    internal static IReadOnlyList<string> RequiredMappingTokens(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one mapping identifier is required.", parameterName);
        }

        string[] mapped = values.Select(value => RequiredMappingToken(value, parameterName)).ToArray();
        return mapped.Distinct(StringComparer.Ordinal).Count() != mapped.Length
            ? throw new ArgumentException("Mapping identifiers must be unique.", parameterName)
            : mapped;
    }

    private static string RequiredMappingToken(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        // Mapping identifiers are closed traceability tokens (requirement ids like FR74, CORE precondition
        // ids, and release-gate categories such as release-gate-tenant-isolation). They are safe machine
        // identifiers, not free text. The bounded charset already excludes ':', '\\', and '/', so prefixed
        // protected identifiers and storage syntax cannot pass. The Story 4.4 lesson applies: do NOT run the
        // free-text disclosure blocklist over closed-vocabulary tokens, or it collides with legitimate
        // 'tenant-'/'party-' segments inside release-gate and precondition identifiers.
        return value.Length > MaxSafeTokenLength || value.Any(static c => !IsTokenCharacter(c))
            ? throw new ArgumentException("Mapping identifiers must be bounded content-safe machine tokens.", parameterName)
            : value;
    }

    internal static Uri RequiredDocumentationUri(Uri value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        return !value.IsAbsoluteUri || value.Scheme != Uri.UriSchemeHttps
            ? throw new ArgumentException("Conformance documentation pointers must use absolute HTTPS URIs.", parameterName)
            : value;
    }

    internal static DateTimeOffset RequiredUtcTimestamp(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timestamp must use UTC offset zero.");
        }

        return value.Year is < 2000 or > 9998
            ? throw new ArgumentOutOfRangeException(parameterName, "Timestamp must be within the plausible business range.")
            : value;
    }

    private static bool IsTokenCharacter(char value)
        => char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.';
}
