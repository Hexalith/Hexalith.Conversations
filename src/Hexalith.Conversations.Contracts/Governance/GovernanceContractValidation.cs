// <copyright file="GovernanceContractValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Governance;

internal static class GovernanceContractValidation
{
    private static readonly string[] UnsafeTerms =
    [
        "raw message content",
        "audit sink",
        "eventstore",
        "provider payload",
        "provider sdk",
        "upstream",
        "exception",
        "token",
        "claim",
        "storage",
        "storage location",
        "stream",
        "revision",
        "diagnostic",
        "handler",
        "projection",
        "runtime",
        "sdk",
        "server runtime",
    ];

    internal static T RequireNonNull<T>(T value, string parameterName)
        where T : class
        => value ?? throw new ArgumentNullException(parameterName);

    internal static string RequiredSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        EnsureContentSafe(value, parameterName);
        return value;
    }

    internal static string RequiredSafeToken(string value, string parameterName)
    {
        string safe = RequiredSafeText(value, parameterName);
        if (safe.Any(static c => !IsTokenCharacter(c)))
        {
            throw new ArgumentException("Value must be a bounded content-safe identifier.", parameterName);
        }

        return safe;
    }

    internal static string? OptionalSafeToken(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        return RequiredSafeToken(value, parameterName);
    }

    internal static DateTimeOffset RequiredUtcTimestamp(DateTimeOffset value, string parameterName)
    {
        if (value <= DateTimeOffset.MinValue || value >= DateTimeOffset.MaxValue)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timestamp must be within the plausible audited business range.");
        }

        if (value.Year < 2000 || value.Year > 9998)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timestamp must be within the plausible audited business range.");
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timestamp must use UTC offset zero.");
        }

        return value;
    }

    private static void EnsureContentSafe(string value, string parameterName)
    {
        foreach (string term in UnsafeTerms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Value must remain content-safe and cannot contain reserved disclosure terms.", parameterName);
            }
        }
    }

    private static bool IsTokenCharacter(char value)
        => char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.' or ':';
}
