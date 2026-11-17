// <copyright file="PortableSurfaceVerification.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;

namespace Hexalith.Conversations.Conformance.Portable.Tests;

/// <summary>Invokes the read-only evaluated product dependency verifier with the current build configuration.</summary>
internal static class PortableSurfaceVerification
{
    /// <summary>Runs the product dependency verifier and requires a nonempty passing result.</summary>
    internal static void Verify(string script = "tests/compatibility/verify_portable_surface.py", string success = "PASS: portable compiled dependency surface")
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        string configuration = directory.Parent!.Name;
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("PORTABLE_PROJECT_MISSING: repository root unavailable.");
        ProcessStartInfo start = new("python3")
        {
            WorkingDirectory = directory!.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("--repository");
        start.ArgumentList.Add(".");
        start.ArgumentList.Add("--configuration");
        start.ArgumentList.Add(configuration);
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(240000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("RESOLVED_COMPILE_SURFACE_INVALID: evaluated tier verification timed out.");
        }

        process.ExitCode.ShouldBe(0, errors.GetAwaiter().GetResult());
        output.GetAwaiter().GetResult().ShouldContain(success);
    }
}
