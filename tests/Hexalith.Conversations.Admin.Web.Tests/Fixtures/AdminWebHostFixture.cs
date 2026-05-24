// <copyright file="AdminWebHostFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Hexalith.Conversations.Admin.Web.Tests.Fixtures;

/// <summary>
/// Starts the rendered Admin Web host on a loopback port for browser evidence tests.
/// </summary>
public sealed class AdminWebHostFixture : IAsyncLifetime
{
    private const int MaxStartAttempts = 3;

    private readonly StringBuilder _output = new();
    private readonly object _outputLock = new();
    private Process? _process;

    public string BaseAddress { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        string assembly = typeof(global::Program).Assembly.Location;
        string? workingDirectory = Path.GetDirectoryName(assembly);
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new InvalidOperationException("Cannot resolve Admin Web output directory.");
        }

        // GetFreePort releases the loopback port before the child binds it, so a parallel
        // process can win the race. Retry with a freshly allocated port when the host exits
        // before becoming healthy instead of failing the whole lane on a transient bind race.
        for (int attempt = 1; ; attempt++)
        {
            int port = GetFreePort();
            BaseAddress = $"http://127.0.0.1:{port}";

            ProcessStartInfo startInfo = new("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory,
            };
            startInfo.ArgumentList.Add(assembly);
            startInfo.Environment["ASPNETCORE_URLS"] = BaseAddress;
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Admin Web host.");
            _process.OutputDataReceived += (_, args) => Append(args.Data);
            _process.ErrorDataReceived += (_, args) => Append(args.Data);
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            try
            {
                await WaitForHealthAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception) when (attempt < MaxStartAttempts)
            {
                await TerminateProcessAsync().ConfigureAwait(false);
            }
        }
    }

    public ValueTask DisposeAsync() => new(TerminateProcessAsync());

    private static int GetFreePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task TerminateProcessAsync()
    {
        Process? process = _process;
        _process = null;
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the HasExited check and Kill; nothing left to stop.
        }
        finally
        {
            process.Dispose();
        }
    }

    private async Task WaitForHealthAsync()
    {
        using HttpClient client = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        while (!timeout.IsCancellationRequested)
        {
            if (_process?.HasExited == true)
            {
                throw new InvalidOperationException($"Admin Web host exited before health check passed. Output: {SafeOutput()}");
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync(
                    $"{BaseAddress}/health",
                    timeout.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token).ConfigureAwait(false);
        }

        throw new TimeoutException($"Admin Web host did not become healthy. Output: {SafeOutput()}");
    }

    private void Append(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        // stdout and stderr callbacks fire on separate threads; serialize appends because
        // StringBuilder is not thread-safe.
        lock (_outputLock)
        {
            _output.AppendLine(line);
        }
    }

    private string SafeOutput()
    {
        lock (_outputLock)
        {
            return _output.ToString();
        }
    }
}
