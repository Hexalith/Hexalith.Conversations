// <copyright file="TelemetryTestHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Server.Tests.Diagnostics;

internal sealed class FakeMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = new();

    public Meter Create(MeterOptions options)
    {
        Meter meter = new(options);
        _meters.Add(meter);
        return meter;
    }

    /// <summary>
    /// Returns whether the supplied meter was created by this factory. Used to scope the process-global
    /// <see cref="MeterListener"/> to this test's own instruments so that measurements emitted by sibling
    /// telemetry tests running in parallel (which share the same instrument names) are not captured here.
    /// </summary>
    /// <param name="meter">The meter reported by the listener.</param>
    /// <returns><c>true</c> when this factory created the meter; otherwise <c>false</c>.</returns>
    public bool Owns(Meter meter) => _meters.Contains(meter);

    public void Dispose()
    {
        foreach (Meter meter in _meters)
        {
            meter.Dispose();
        }

        _meters.Clear();
    }
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => NullLogger.Instance.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));
}
