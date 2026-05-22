// <copyright file="GovernanceContractValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Governance;

internal static class GovernanceContractValidation
{
    private const int MaxSafeTextLength = 512;
    private const int MaxSafeTokenLength = 128;

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
        "tenant:",
        "customer:",
        "bearer",
        "secret",
    ];

    internal static T RequireNonNull<T>(T value, string parameterName)
        where T : class
        => value ?? throw new ArgumentNullException(parameterName);

    internal static string RequiredSafeText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaxSafeTextLength)
        {
            throw new ArgumentException("Value must be within the bounded content-safe length.", parameterName);
        }

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

        if (safe.Length > MaxSafeTokenLength)
        {
            throw new ArgumentException("Value must be within the bounded content-safe identifier length.", parameterName);
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
        if (value.Any(char.IsControl))
        {
            throw new ArgumentException("Value must remain content-safe and cannot contain control characters.", parameterName);
        }

        if (LooksLikePathOrLocation(value))
        {
            throw new ArgumentException("Value must remain content-safe and cannot contain storage or location syntax.", parameterName);
        }

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

    private static bool LooksLikePathOrLocation(string value)
        => value.Contains("://", StringComparison.Ordinal)
            || value.Contains('\\', StringComparison.Ordinal)
            || value.Contains('/', StringComparison.Ordinal)
            || value.Contains("..", StringComparison.Ordinal);
}
