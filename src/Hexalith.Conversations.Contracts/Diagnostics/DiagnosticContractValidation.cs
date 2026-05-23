// <copyright file="DiagnosticContractValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Contracts.Diagnostics;

/// <summary>
/// Validates onboarding diagnostic contract fields without leaking protected content.
/// </summary>
/// <remarks>
/// Free-text fields reuse the canonical <see cref="ConversationError"/> disclosure blocklist so diagnostics
/// cannot drift apart from the shared error guardrails. Tokens are additionally constrained to a bounded
/// closed character set whose forbidden characters (<c>:</c>, <c>\</c>, <c>/</c>) already exclude prefixed
/// identifiers and storage syntax, so legitimate machine vocabulary such as <c>projection-subscription</c>
/// and <c>party-identity-validation</c> remains valid while protected values cannot pass.
/// </remarks>
internal static class DiagnosticContractValidation
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

        // Reuse the canonical ConversationError disclosure blocklist for free-text.
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

        // The token charset already excludes ':', '\\', and '/', so prefixed identifiers and storage
        // syntax cannot pass; the canonical free-text guard catches any remaining disclosure markers.
        ConversationError.EnsureContentSafe(value, parameterName);
        return value;
    }

    internal static IReadOnlyList<string> RequiredRequirementMappings(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one requirement mapping is required.", parameterName);
        }

        return values.Select(value => RequiredSafeToken(value, parameterName)).ToArray();
    }

    internal static Uri RequiredDocumentationUri(Uri value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (!value.IsAbsoluteUri || value.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Diagnostic documentation pointers must use absolute HTTPS URIs.", parameterName);
        }

        return value;
    }

    private static bool IsTokenCharacter(char value)
        => char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.';
}
