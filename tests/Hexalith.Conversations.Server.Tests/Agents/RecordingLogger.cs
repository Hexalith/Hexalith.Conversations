// <copyright file="RecordingLogger.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Records formatted log entries so tests can assert value-free reason codes.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, int EventId, string Message)> _entries = [];

    public IReadOnlyList<(LogLevel Level, int EventId, string Message)> Entries
    {
        get
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_entries)
        {
            _entries.Add((logLevel, eventId.Id, formatter(state, exception)));
        }
    }
}
