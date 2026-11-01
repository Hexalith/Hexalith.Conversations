// <copyright file="Story16_1LiveSidecar.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Dapr.Client;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>
/// One live <c>daprd</c> process serving the shared Redis read-model state store for one Conversations replica.
/// </summary>
/// <remarks>
/// Every replica of the same application identity runs its own sidecar on isolated ports; the sidecars share the
/// state-store component and the application key prefix, exactly as two deployed replicas do.
/// </remarks>
internal sealed class Story16_1LiveSidecar : IAsyncDisposable
{
    private const int HealthTimeoutSeconds = 60;
    private readonly StringBuilder _standardError = new();
    private readonly string _appId;
    private readonly string _componentsDirectory;
    private Process? _process;

    /// <summary>
    /// Initializes a new instance of the <see cref="Story16_1LiveSidecar"/> class.
    /// </summary>
    /// <param name="appId">The shared application identity.</param>
    /// <param name="componentsDirectory">The directory holding the shared state-store component.</param>
    public Story16_1LiveSidecar(string appId, string componentsDirectory)
    {
        _appId = appId;
        _componentsDirectory = componentsDirectory;
    }

    /// <summary>Gets the sidecar HTTP endpoint.</summary>
    public string HttpEndpoint { get; private set; } = string.Empty;

    /// <summary>Gets the sidecar gRPC endpoint.</summary>
    public string GrpcEndpoint { get; private set; } = string.Empty;

    /// <summary>Gets the number of times this sidecar process was started.</summary>
    public int Starts { get; private set; }

    /// <summary>Starts (or restarts) the sidecar on fresh isolated ports and waits until it is healthy.</summary>
    /// <returns>A task.</returns>
    public async Task StartAsync()
    {
        await StopAsync().ConfigureAwait(false);
        int[] ports = GetAvailablePorts(4);
        HttpEndpoint = $"http://localhost:{ports[0]}";
        GrpcEndpoint = $"http://localhost:{ports[1]}";
        lock (_standardError)
        {
            _ = _standardError.Clear();
        }

        _process = new Process
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
        foreach (string argument in new[]
        {
            "--app-id", _appId,
            "--dapr-http-port", ports[0].ToString(CultureInfo.InvariantCulture),
            "--dapr-grpc-port", ports[1].ToString(CultureInfo.InvariantCulture),
            "--dapr-internal-grpc-port", ports[2].ToString(CultureInfo.InvariantCulture),
            "--metrics-port", ports[3].ToString(CultureInfo.InvariantCulture),
            "--resources-path", _componentsDirectory,
            "--placement-host-address", string.Empty,
            "--scheduler-host-address", string.Empty,
            "--log-level", "warn",
        })
        {
            _process.StartInfo.ArgumentList.Add(argument);
        }

        _process.ErrorDataReceived += (_, args) => Append(args.Data);
        _process.OutputDataReceived += (_, args) => Append(args.Data);
        _ = _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        Starts++;
        await WaitForHealthAsync().ConfigureAwait(false);
    }

    /// <summary>Creates a Dapr client bound to this sidecar.</summary>
    /// <returns>The client; the caller owns it.</returns>
    public DaprClient CreateClient()
        => new DaprClientBuilder().UseHttpEndpoint(HttpEndpoint).UseGrpcEndpoint(GrpcEndpoint).Build();

    /// <summary>Stops the sidecar process, when running.</summary>
    /// <returns>A task.</returns>
    public async Task StopAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private static int[] GetAvailablePorts(int count)
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

    private void Append(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_standardError)
        {
            _ = _standardError.AppendLine(line);
        }
    }

    private async Task WaitForHealthAsync()
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
        string? lastError = null;
        for (int attempt = 0; attempt < HealthTimeoutSeconds * 4; attempt++)
        {
            if (_process?.HasExited == true)
            {
                throw new InvalidOperationException($"daprd exited with code {_process.ExitCode}: {Tail()}");
            }

            try
            {
                using HttpResponseMessage response = await client
                    .GetAsync(new Uri($"{HttpEndpoint}/v1.0/healthz/outbound"))
                    .ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastError = $"HTTP {(int)response.StatusCode}";
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastError = exception.Message;
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"daprd did not become healthy: {lastError}. {Tail()}");
    }

    private string Tail()
    {
        lock (_standardError)
        {
            string text = _standardError.ToString();
            return text.Length <= 2000 ? text : text[^2000..];
        }
    }
}
