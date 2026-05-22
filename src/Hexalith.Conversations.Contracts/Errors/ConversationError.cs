// <copyright file="ConversationError.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Describes a content-safe machine-readable Conversations failure.
/// </summary>
/// <remarks>
/// The blocklist on free-text fields (<see cref="CorrelationId"/>, <see cref="AuditHandle"/>,
/// <see cref="DeveloperGuidance"/>, and <see cref="SafeFieldDiagnostics"/> entries) is best-effort
/// only. The primary non-disclosure mechanism is the closed-vocabulary <see cref="Code"/> and
/// <see cref="Category"/>.
/// </remarks>
/// <param name="schemaVersion">The error schema version.</param>
/// <param name="code">The stable machine-readable error code.</param>
/// <param name="category">The broad machine-readable error category.</param>
/// <param name="isRetryable">A value indicating whether retry is meaningful.</param>
/// <param name="correlationId">The safe correlation identifier.</param>
/// <param name="auditHandle">An optional safe audit handle.</param>
/// <param name="documentation">An optional documentation pointer.</param>
/// <param name="safeFieldDiagnostics">Optional non-disclosing field diagnostics.</param>
/// <param name="developerGuidance">Optional safe developer guidance.</param>
/// <param name="clientAction">Optional bounded adopter action.</param>
/// <param name="safeMessage">Optional safe adopter-facing message.</param>
public sealed record ConversationError(
    SchemaVersion SchemaVersion,
    ConversationErrorCode Code,
    ConversationErrorCategory Category,
    bool IsRetryable,
    string CorrelationId,
    string? AuditHandle = null,
    Uri? Documentation = null,
    IReadOnlyDictionary<string, string>? SafeFieldDiagnostics = null,
    string? DeveloperGuidance = null,
    ConversationErrorClientAction? ClientAction = null,
    string? SafeMessage = null)
{
    private static readonly string[] UnsafeTerms =
    [
        "other-tenant",
        "redacted content",
        "provider-a",
        "EventStore",
        "envelope",
        "stream",
        "snapshot",
        "sequence",
        "expected revision",
        "checkpoint",
        "SignalR",
        "projection topology",
        "handler",
        "dispatcher",
        "repository",
        "store",
        "aggregate identity",
        "raw upstream",
        "tenant:",
        "tenant-",
        "party:",
        "party-",
        "conv:",
        "conversation-",
        "provider-session",
        "provider response",
        "provider payload",
        "business reference",
        "case-",
        "raw exception",
        "exception",
        "C:\\",
        "D:\\",
    ];

    /// <summary>
    /// Gets the safe correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = EnsureSafeRequiredText(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Gets the optional safe audit handle.
    /// </summary>
    public string? AuditHandle { get; } = EnsureSafeOptionalText(AuditHandle, nameof(AuditHandle));

    /// <summary>
    /// Gets the optional non-disclosing field diagnostics.
    /// </summary>
    public IReadOnlyDictionary<string, string>? SafeFieldDiagnostics { get; } = EnsureSafeDiagnostics(SafeFieldDiagnostics);

    /// <summary>
    /// Gets the optional safe developer guidance.
    /// </summary>
    public string? DeveloperGuidance { get; } = EnsureSafeOptionalText(DeveloperGuidance, nameof(DeveloperGuidance));

    /// <summary>
    /// Gets the optional bounded adopter action.
    /// </summary>
    public ConversationErrorClientAction? ClientAction { get; } = ClientAction;

    /// <summary>
    /// Gets the optional safe adopter-facing message.
    /// </summary>
    public string? SafeMessage { get; } = EnsureSafeOptionalText(SafeMessage, nameof(SafeMessage));

    private static string EnsureSafeRequiredText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        EnsureContentSafe(value, parameterName);
        return value;
    }

    private static string? EnsureSafeOptionalText(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        EnsureContentSafe(value, parameterName);
        return value;
    }

    private static IReadOnlyDictionary<string, string>? EnsureSafeDiagnostics(IReadOnlyDictionary<string, string>? diagnostics)
    {
        if (diagnostics is null)
        {
            return null;
        }

        Dictionary<string, string> validated = new(diagnostics.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> diagnostic in diagnostics)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(diagnostic.Key, $"{nameof(SafeFieldDiagnostics)}.Key");
            ArgumentNullException.ThrowIfNull(diagnostic.Value, $"{nameof(SafeFieldDiagnostics)}.Value");
            EnsureContentSafe(diagnostic.Key, $"{nameof(SafeFieldDiagnostics)}.Key");
            EnsureContentSafe(diagnostic.Value, $"{nameof(SafeFieldDiagnostics)}.Value");
            validated.Add(diagnostic.Key, diagnostic.Value);
        }

        return validated;
    }

    internal static void EnsureContentSafe(string value, string parameterName)
    {
        foreach (string term in UnsafeTerms)
        {
            if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Error details must remain content-safe; the value contained the forbidden term fragment '{term}'.", parameterName);
            }
        }
    }
}
