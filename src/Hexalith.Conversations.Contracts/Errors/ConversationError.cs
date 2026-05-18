// <copyright file="ConversationError.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Describes a content-safe machine-readable Conversations failure.
/// </summary>
/// <param name="schemaVersion">The error schema version.</param>
/// <param name="code">The stable machine-readable error code.</param>
/// <param name="category">The broad machine-readable error category.</param>
/// <param name="isRetryable">A value indicating whether retry is meaningful.</param>
/// <param name="correlationId">The safe correlation identifier.</param>
/// <param name="auditHandle">An optional safe audit handle.</param>
/// <param name="documentation">An optional documentation pointer.</param>
/// <param name="safeFieldDiagnostics">Optional non-disclosing field diagnostics.</param>
/// <param name="developerGuidance">Optional safe developer guidance.</param>
public sealed record ConversationError(
    SchemaVersion SchemaVersion,
    ConversationErrorCode Code,
    ConversationErrorCategory Category,
    bool IsRetryable,
    string CorrelationId,
    string? AuditHandle = null,
    Uri? Documentation = null,
    IReadOnlyDictionary<string, string>? SafeFieldDiagnostics = null,
    string? DeveloperGuidance = null)
{
    /// <summary>
    /// Gets the safe correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = ValidateRequired(CorrelationId);

    /// <summary>
    /// Gets optional non-disclosing field diagnostics.
    /// </summary>
    public IReadOnlyDictionary<string, string>? SafeFieldDiagnostics { get; } = ValidateSafeDiagnostics(SafeFieldDiagnostics);

    /// <summary>
    /// Gets optional safe developer guidance.
    /// </summary>
    public string? DeveloperGuidance { get; } = ValidateSafeText(DeveloperGuidance, nameof(DeveloperGuidance));

    private static string ValidateRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    private static IReadOnlyDictionary<string, string>? ValidateSafeDiagnostics(IReadOnlyDictionary<string, string>? diagnostics)
    {
        if (diagnostics is null)
        {
            return null;
        }

        foreach (KeyValuePair<string, string> diagnostic in diagnostics)
        {
            ValidateSafeText(diagnostic.Key, nameof(SafeFieldDiagnostics));
            ValidateSafeText(diagnostic.Value, nameof(SafeFieldDiagnostics));
        }

        return diagnostics;
    }

    private static string? ValidateSafeText(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        string[] unsafeTerms =
        [
            "other-tenant",
            "exists",
            "redacted content",
            "provider-a",
            "storage",
        ];

        return unsafeTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase))
            ? throw new ArgumentException("Error details must remain content-safe.", parameterName)
            : value;
    }
}
