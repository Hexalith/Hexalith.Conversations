// <copyright file="ConversationGatewayRecordingLogger.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Records only the category identity when the production logger executes.</summary>
/// <param name="category">The actual production category.</param>
/// <param name="categories">The fixture's observed category identities.</param>
internal sealed class ConversationGatewayRecordingLogger(string category, ConcurrentDictionary<string, byte> categories) : ILogger
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel)) categories[category] = 0;
    }
}
