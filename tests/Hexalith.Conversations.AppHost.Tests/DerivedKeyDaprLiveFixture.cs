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

            int[] daprPorts = AllocatePorts(4);
            string httpEndpoint = $"http://127.0.0.1:{daprPorts[0]}";
            string grpcEndpoint = $"http://127.0.0.1:{daprPorts[1]}";

            _daprProcess = new Process
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
            foreach (string arg in new[]
                     {
                         "--app-id", _appId,
                         "--dapr-http-port", daprPorts[0].ToString(CultureInfo.InvariantCulture),
                         "--dapr-grpc-port", daprPorts[1].ToString(CultureInfo.InvariantCulture),
                         "--dapr-internal-grpc-port", daprPorts[2].ToString(CultureInfo.InvariantCulture),
                         "--metrics-port", daprPorts[3].ToString(CultureInfo.InvariantCulture),
                         "--resources-path", _componentsDirectory,
                         "--placement-host-address", string.Empty,
                         "--scheduler-host-address", string.Empty,
                         "--log-level", "warn",
                     })
            {
                _daprProcess.StartInfo.ArgumentList.Add(arg);
            }

            _daprProcess.OutputDataReceived += (_, args) => AppendLog(args.Data);
            _daprProcess.ErrorDataReceived += (_, args) => AppendLog(args.Data);
            _ = _daprProcess.Start();
            _daprProcess.BeginOutputReadLine();
            _daprProcess.BeginErrorReadLine();

            await WaitForDaprHealthAsync(httpEndpoint).ConfigureAwait(false);

            Client = new DaprClientBuilder()
                .UseHttpEndpoint(httpEndpoint)
                .UseGrpcEndpoint(grpcEndpoint)
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
                if (_daprProcess is not null)
                {
                    try
                    {
                        if (!_daprProcess.HasExited)
                        {
                            _daprProcess.Kill(entireProcessTree: true);
                            using CancellationTokenSource exitTimeout = new(TimeSpan.FromSeconds(10));
                            try
                            {
                                await _daprProcess.WaitForExitAsync(exitTimeout.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                // Bounded wait prevents fixture teardown from hanging if daprd ignores termination.
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process already exited.
                    }
                    finally
                    {
                        _daprProcess.Dispose();
                        _daprProcess = null;
                    }
                }
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

    private async Task WaitForDaprHealthAsync(string httpEndpoint)
    {
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(3) };
        string? lastError = null;
        for (int attempt = 0; attempt < HealthTimeoutSeconds * 4; attempt++)
        {
            if (_daprProcess?.HasExited == true)
            {
                throw new InvalidOperationException($"daprd exited with code {_daprProcess.ExitCode}: {LogTail()}");
            }

            try
            {
                using HttpResponseMessage response = await http
                    .GetAsync(new Uri($"{httpEndpoint}/v1.0/healthz/outbound"))
                    .ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastError = $"HTTP {(int)response.StatusCode}";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastError = ex.Message;
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"daprd did not become healthy: {lastError}. {LogTail()}");
    }

    private static int[] AllocatePorts(int count)
    {
        TcpListener[] listeners = new TcpListener[count];
        int[] ports = new int[count];
        for (int index = 0; index < count; index++)
        {
            listeners[index] = new TcpListener(IPAddress.Loopback, 0);
            listeners[index].Start();
            ports[index] = ((IPEndPoint)listeners[index].LocalEndpoint).Port;
        }

        foreach (TcpListener listener in listeners)
        {
            listener.Stop();
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
        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
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
