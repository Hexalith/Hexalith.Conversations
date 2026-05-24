// <copyright file="TelemetryValidationTestHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.Metrics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// A test <see cref="IMeterFactory"/> that creates real meters so the operational-telemetry validation suites
/// can drive the production telemetry classes and capture live emissions through a <see cref="MeterListener"/>.
/// </summary>
/// <remarks>
/// Mirrors the <c>FakeMeterFactory</c> used by the Server.Tests telemetry tests. It is duplicated here because
/// the Server.Tests internal helper assembly is not referenced by the Conformance.Tests project.
/// </remarks>
internal sealed class FakeMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = new();

    public Meter Create(MeterOptions options)
    {
        Meter meter = new(options);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (Meter meter in _meters)
        {
            meter.Dispose();
        }

        _meters.Clear();
    }
}

/// <summary>
/// An <see cref="ILogger{T}"/> that captures every formatted structured-log message so the validation suites
/// can assert the emitted log shapes exclude unsafe values.
/// </summary>
/// <typeparam name="T">The category type for the logger.</typeparam>
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
