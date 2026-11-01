// <copyright file="ConversationGatewayRecordingLoggerProvider.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Observes executed production log categories without recording payload or credential values.</summary>
internal sealed class ConversationGatewayRecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, byte> _categories = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new ConversationGatewayRecordingLogger(categoryName, _categories);

    /// <summary>Reports whether the category actually emitted a log record.</summary>
    public bool HasCategory(string category) => _categories.ContainsKey(category);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
