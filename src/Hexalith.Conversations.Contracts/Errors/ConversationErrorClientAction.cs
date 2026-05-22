// <copyright file="ConversationErrorClientAction.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Defines bounded adopter actions for typed Conversations errors.
/// </summary>
[JsonConverter(typeof(ConversationErrorClientActionJsonConverter))]
public sealed record ConversationErrorClientAction
{
    /// <summary>
    /// Gets the action for supplying authenticated context.
    /// </summary>
    public static ConversationErrorClientAction ProvideContext { get; } = new("provide-context");

    /// <summary>
    /// Gets the action for checking caller access.
    /// </summary>
    public static ConversationErrorClientAction CheckAccess { get; } = new("check-access");

    /// <summary>
    /// Gets the action for retrying after a safe delay or dependency recovery.
    /// </summary>
    public static ConversationErrorClientAction RetryLater { get; } = new("retry-later");

    /// <summary>
    /// Gets the action for supplying required audit evidence.
    /// </summary>
    public static ConversationErrorClientAction ProvideAuditEvidence { get; } = new("provide-audit-evidence");

    /// <summary>
    /// Gets the action for sending a changed command with a new idempotency key.
    /// </summary>
    public static ConversationErrorClientAction UseNewIdempotencyKey { get; } = new("use-new-idempotency-key");

    /// <summary>
    /// Gets the action for retrying the same command metadata.
    /// </summary>
    public static ConversationErrorClientAction RetrySameRequest { get; } = new("retry-same-request");

    /// <summary>
    /// Gets the action for supplying an idempotency key.
    /// </summary>
    public static ConversationErrorClientAction ProvideIdempotencyKey { get; } = new("provide-idempotency-key");

    /// <summary>
    /// Gets the action for hiding or refreshing the unavailable target view.
    /// </summary>
    public static ConversationErrorClientAction HideOrRefresh { get; } = new("hide-or-refresh");

    /// <summary>
    /// Gets the action for using supported contract and client package versions.
    /// </summary>
    public static ConversationErrorClientAction UseSupportedVersion { get; } = new("use-supported-version");

    /// <summary>
    /// Gets the action for correcting safe request metadata or fields.
    /// </summary>
    public static ConversationErrorClientAction CorrectRequest { get; } = new("correct-request");

    /// <summary>
    /// Gets the action for aligning the request context to the authenticated context.
    /// </summary>
    public static ConversationErrorClientAction AlignContext { get; } = new("align-context");

    /// <summary>
    /// Gets the action for using a Conversations Party identity.
    /// </summary>
    public static ConversationErrorClientAction UsePartyIdentity { get; } = new("use-party-identity");

    private static readonly IReadOnlyDictionary<string, ConversationErrorClientAction> KnownActions =
        new[]
        {
            ProvideContext,
            CheckAccess,
            RetryLater,
            ProvideAuditEvidence,
            UseNewIdempotencyKey,
            RetrySameRequest,
            ProvideIdempotencyKey,
            HideOrRefresh,
            UseSupportedVersion,
            CorrectRequest,
            AlignContext,
            UsePartyIdentity,
        }.ToDictionary(action => action.Value, StringComparer.Ordinal);

    private ConversationErrorClientAction(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the machine-readable action value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported client action.
    /// </summary>
    /// <param name="value">The machine-readable action value.</param>
    /// <returns>The matching supported action.</returns>
    public static ConversationErrorClientAction Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownActions.TryGetValue(value, out ConversationErrorClientAction? action)
            ? action
            : throw new ArgumentException("Unsupported conversation error client action.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
