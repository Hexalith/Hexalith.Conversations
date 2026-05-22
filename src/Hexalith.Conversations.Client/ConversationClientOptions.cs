// <copyright file="ConversationClientOptions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Client;

/// <summary>
/// Configures the supported Conversations .NET client transport endpoint.
/// </summary>
public sealed record ConversationClientOptions
{
    /// <summary>
    /// Gets the Conversations API endpoint used by the typed client.
    /// </summary>
    public Uri? Endpoint { get; set; }
}
