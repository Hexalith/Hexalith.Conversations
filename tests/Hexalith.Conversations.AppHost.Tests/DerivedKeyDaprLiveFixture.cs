// <copyright file="DerivedKeyDaprLiveFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Dapr.Client;

using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Integration;

using Microsoft.Extensions.Configuration;

namespace Hexalith.Conversations.AppHost.Tests;

/// <summary>
/// Owns a disposable Redis container and live <c>daprd</c> sidecar backing <see cref="DaprReadModelStore"/>
/// for Story 12.3 (<c>AC-12.3-04</c>) real Dapr state-store fault tests.
/// </summary>
public sealed class DerivedKeyDaprLiveFixture : IAsyncLifetime
{
    private const int HealthTimeoutSeconds = 60;
    private readonly string _containerName = "story123-redis-" + Guid.NewGuid().ToString("N");
    private readonly string _appId = "story123-dapr-" + Guid.NewGuid().ToString("N")[..8];
    private readonly StringBuilder _daprLogs = new();
    private string? _componentsDirectory;
    private Process? _daprProcess;
    private int _redisPort;
    private int[] _daprPorts = [];
    private bool _containerStarted;

    /// <summary>Gets the configured Dapr Redis state-store component name.</summary>
    internal const string StateStoreName = "conversations-12-3-dapr-state";

    /// <summary>Gets the store options bound to the live Redis component.</summary>
    internal ConversationProjectionStoreOptions StoreOptions { get; } =
        ConversationProjectionStoreOptions.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConversationProjectionStoreOptions.ConfigurationKey] = StateStoreName,
            })
            .Build());

    /// <summary>Gets the live Dapr client.</summary>
    internal DaprClient Client { get; private set; } = null!;

    /// <summary>Gets the production <see cref="DaprReadModelStore"/> backed by live <c>daprd</c> + Redis.</summary>
    internal DaprReadModelStore Store { get; private set; } = null!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _redisPort = AllocatePorts(1)[0];
        try
        {
            _containerStarted = true;
            _ = await RunDockerAsync(["run", "--detach", "--rm", "--name", _containerName, "--publish", $"127.0.0.1:{_redisPort}:6379", "redis:7.4-alpine"], allowMissing: false)
                .ConfigureAwait(false);

            using CancellationTokenSource redisTimeout = new(TimeSpan.FromSeconds(30));
            while (!await DaprDiagnostics.IsRedisResponsiveAsync(_redisPort).ConfigureAwait(false))
            {
                await Task.Delay(100, redisTimeout.Token).ConfigureAwait(false);
            }

            _componentsDirectory = Path.Combine(Path.GetTempPath(), "story123-dapr-components-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(_componentsDirectory);
            string componentYaml = $$"""
                apiVersion: dapr.io/v1alpha1
                kind: Component
                metadata:
                  name: {{StateStoreName}}
                spec:
                  type: state.redis
                  version: v1
                  metadata:
                  - name: redisHost
                    value: 127.0.0.1:{{_redisPort}}
                  - name: redisPassword
                    value: ""
                  - name: actorStateStore
                    value: "false"
                """;
            await File.WriteAllTextAsync(Path.Combine(_componentsDirectory, "statestore.yaml"), componentYaml)
                .ConfigureAwait(false);

            _daprPorts = AllocatePorts(4);
            await StartSidecarAsync().ConfigureAwait(false);

            Client = new DaprClientBuilder()
                .UseHttpEndpoint($"http://127.0.0.1:{_daprPorts[0]}")
                .UseGrpcEndpoint($"http://127.0.0.1:{_daprPorts[1]}")
                .Build();
            Store = new DaprReadModelStore(Client);
        }
        catch
        {
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            Client?.Dispose();
        }
        finally
        {
            try
            {
                await StopSidecarAsync().ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    if (_componentsDirectory is not null && Directory.Exists(_componentsDirectory))
                    {
                        Directory.Delete(_componentsDirectory, recursive: true);
                    }
                }
                finally
                {
                    if (_containerStarted)
                    {
                        _ = await RunDockerAsync(["rm", "--force", _containerName], allowMissing: true).ConfigureAwait(false);
                        _containerStarted = false;
                    }
                }
            }
        }
    }

    /// <summary>Starts the sidecar on the original endpoints while retaining the backing Redis state.</summary>
    internal async Task StartSidecarAsync()
    {
        if (_daprProcess is { HasExited: false })
        {
            return;
        }

        Process sidecar = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ResolveDaprdPath(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
        _daprProcess = sidecar;
        foreach (string arg in new[]
                 {
                     "--app-id", _appId,
                     "--dapr-http-port", _daprPorts[0].ToString(CultureInfo.InvariantCulture),
                     "--dapr-grpc-port", _daprPorts[1].ToString(CultureInfo.InvariantCulture),
                     "--dapr-internal-grpc-port", _daprPorts[2].ToString(CultureInfo.InvariantCulture),
                     "--metrics-port", _daprPorts[3].ToString(CultureInfo.InvariantCulture),
                     "--resources-path", _componentsDirectory!,
                     "--placement-host-address", string.Empty,
                     "--scheduler-host-address", string.Empty,
                     "--log-level", "warn",
                 })
        {
            sidecar.StartInfo.ArgumentList.Add(arg);
        }

        sidecar.OutputDataReceived += (_, args) => AppendLog(args.Data);
        sidecar.ErrorDataReceived += (_, args) => AppendLog(args.Data);
        _ = sidecar.Start();
        sidecar.BeginOutputReadLine();
        sidecar.BeginErrorReadLine();
        await WaitForDaprHealthAsync(sidecar, $"http://127.0.0.1:{_daprPorts[0]}").ConfigureAwait(false);
    }

    /// <summary>Stops the live sidecar so requests through the production adapter observe a real outage.</summary>
    internal async Task StopSidecarAsync()
    {
        if (_daprProcess is not { } sidecar)
        {
            return;
        }

        try
        {
            if (!sidecar.HasExited)
            {
                sidecar.Kill(entireProcessTree: true);
                using CancellationTokenSource exitTimeout = new(TimeSpan.FromSeconds(10));
                await sidecar.WaitForExitAsync(exitTimeout.Token).ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited or failed to start.
        }
        finally
        {
            sidecar.Dispose();
            _daprProcess = null;
        }
    }

    private async Task WaitForDaprHealthAsync(Process sidecar, string httpEndpoint)
    {
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(3) };
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(HealthTimeoutSeconds));
        string? lastError = null;
        try
        {
            while (true)
            {
                if (sidecar.HasExited)
                {
                    throw new InvalidOperationException($"daprd exited with code {sidecar.ExitCode}: {LogTail()}");
                }

                try
                {
                    using HttpResponseMessage response = await http
                        .GetAsync(new Uri($"{httpEndpoint}/v1.0/healthz/outbound"), deadline.Token)
                        .ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        if (Store is not null)
                        {
                            // The reused gRPC channel can still be reconnecting after HTTP is healthy.
                            _ = await Store.GetAsync<object>(StateStoreName, "sidecar-readiness", deadline.Token)
                                .ConfigureAwait(false);
                        }

                        return;
                    }

                    lastError = $"HTTP {(int)response.StatusCode}";
                }
                catch (Exception ex) when (ex is HttpRequestException or Dapr.DaprException
                    || ex is TaskCanceledException && !deadline.IsCancellationRequested)
                {
                    lastError = ex.Message;
                }

                await Task.Delay(250, deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (deadline.IsCancellationRequested)
        {
            throw new InvalidOperationException($"daprd did not become healthy: {lastError}. {LogTail()}", ex);
        }
    }

    private static int[] AllocatePorts(int count)
    {
        TcpListener?[] listeners = new TcpListener?[count];
        int[] ports = new int[count];
        try
        {
            for (int index = 0; index < count; index++)
            {
                listeners[index] = new TcpListener(IPAddress.Loopback, 0);
                listeners[index]!.Start();
                ports[index] = ((IPEndPoint)listeners[index]!.LocalEndpoint).Port;
            }
        }
        finally
        {
            foreach (TcpListener? listener in listeners)
            {
                listener?.Stop();
            }
        }

        return ports;
    }

    private static string ResolveDaprdPath()
    {
        string candidate = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dapr",
            "bin",
            "daprd" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty));
        return File.Exists(candidate) ? candidate : "daprd";
    }

    private static async Task<string> RunDockerAsync(IReadOnlyList<string> arguments, bool allowMissing)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(45));
        ProcessStartInfo startInfo = new("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start docker.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited.
            }

            using CancellationTokenSource killWait = new(TimeSpan.FromSeconds(10));
            try
            {
                await process.WaitForExitAsync(killWait.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Bounded wait after terminating timed-out docker process before fixture cleanup.
            }

            throw;
        }

        string output = await stdout.ConfigureAwait(false);
        string error = await stderr.ConfigureAwait(false);
        if (allowMissing && process.ExitCode != 0 && error.Contains("No such container", StringComparison.OrdinalIgnoreCase))
        {
            return output;
        }

        process.ExitCode.ShouldBe(0, output + error);
        return output;
    }

    private void AppendLog(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_daprLogs)
        {
            _ = _daprLogs.AppendLine(line);
        }
    }

    private string LogTail()
    {
        lock (_daprLogs)
        {
            string text = _daprLogs.ToString();
            return text.Length <= 2000 ? text : text[^2000..];
        }
    }
}
