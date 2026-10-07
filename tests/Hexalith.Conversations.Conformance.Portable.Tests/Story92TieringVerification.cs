// <copyright file="Story92TieringVerification.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;

namespace Hexalith.Conversations.Conformance.Tests.Story92;

/// <summary>Invokes the read-only evaluated tier verifier with the current build configuration.</summary>
internal static class Story92TieringVerification
{
    /// <summary>Runs a verifier mode and requires a nonempty passing result.</summary>
    /// <param name="mode">The exact verifier mode flag.</param>
    internal static void Verify(string mode)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        string configuration = directory.Parent!.Name;
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("TIER_PROJECT_MISSING: repository root unavailable.");
        ProcessStartInfo start = new("python3")
        {
            WorkingDirectory = directory!.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("_bmad/scripts/verify_conformance_tiering.py");
        start.ArgumentList.Add("--repository");
        start.ArgumentList.Add(".");
        start.ArgumentList.Add("--configuration");
        start.ArgumentList.Add(configuration);
        start.ArgumentList.Add(mode);
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(240000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("RESOLVED_COMPILE_SURFACE_INVALID: evaluated tier verification timed out.");
        }

        process.ExitCode.ShouldBe(0, errors.GetAwaiter().GetResult());
        output.GetAwaiter().GetResult().ShouldContain("PASS: Story 9.2");
    }
}
