using System.Diagnostics;
using Hexalith.Conversations.DeletionConformance;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Executes the actual receiver host to verify startup refusal before an HTTP listener can open.</summary>
public sealed class DeletionConformanceStartupTests
{
    /// <summary>Complete receiver IDs and a client secret cannot bypass strict SDK trust configuration.</summary>
    [Theory]
    [InlineData("Development", "Deletion conformance qualification requires a non-Development environment.")]
    [InlineData("Production", "Domain-service internal credentials are not configured:")]
    public async Task PartiallyConfiguredHostNeverListens(string environment, string expectedRefusal)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        start.ArgumentList.Add(typeof(DeletionConformanceAggregate).Assembly.Location);
        foreach (string key in start.Environment.Keys.Where(static key => key.StartsWith("Authentication__", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("EventStore__", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("DeletionConformance__", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase)
            || key.Equals("DOTNET_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
            || key.Equals("APP_API_TOKEN", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            start.Environment.Remove(key);
        }
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        start.Environment["DeletionConformance__Enabled"] = "true";
        start.Environment["DeletionConformance__TenantId"] = "tenant-one";
        start.Environment["DeletionConformance__TargetVersion"] = new string('a', 40);
        start.Environment["DeletionConformance__ReceiverPrincipalId"] = "receiver";
        start.Environment["DeletionConformance__LookupPrincipalId"] = "receipt-verifier";
        start.Environment["DeletionConformance__WorkerPrincipalId"] = "source-worker";
        start.Environment["DeletionConformance__GatewayWorkloadId"] = "eventstore";
        start.Environment["DeletionConformance__StorageIdentity"] = "receipt-store";
        start.Environment["DeletionConformance__GatewayBaseAddress"] = "https://gateway.example/";
        start.Environment["EventStore__DomainService__AppId"] = "receiver";
        start.Environment["EventStore__Authentication__ClientId"] = "receiver";
        start.Environment["EventStore__Authentication__ClientSecret"] = "qualification-test-client-secret";
        using var process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> stderr = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
            await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
            string output = await stdout + await stderr;
            process.ExitCode.ShouldNotBe(0);
            output.ShouldContain(expectedRefusal);
            output.ShouldNotContain("Now listening on:");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            }
        }
    }
}
