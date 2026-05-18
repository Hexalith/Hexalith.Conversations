// <copyright file="ConversationErrorResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Errors;

/// <summary>
/// Wraps one or more content-safe Conversations errors.
/// </summary>
/// <param name="errors">The content-safe machine-readable errors.</param>
public sealed record ConversationErrorResult(IReadOnlyList<ConversationError> Errors);
