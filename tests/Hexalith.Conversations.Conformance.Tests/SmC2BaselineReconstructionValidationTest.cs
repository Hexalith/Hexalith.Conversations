// <copyright file="SmC2BaselineReconstructionValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the SM-C2 baseline's reconstruction provenance against the repository itself.
/// </summary>
/// <remarks>
/// <para>
/// The versioned SM-C2 fixture did not exist at the declared source commit, so the baseline is a reconstruction.
/// AC1 allows that only when the reconstruction is evidenced. This guard checks the recorded claims against git
/// rather than against the artifact's own prose: the fixture really is absent at the declared source commit, the
/// measured production closure really is unchanged between that commit and the working revision, and the fixture
/// really does depend on nothing outside that closure.
/// </para>
/// <para>
/// When git history is unavailable the git-backed assertions are skipped rather than silently passing, and the
/// executed-check counter proves which comparisons actually ran. A zero-assertion green run is the failure mode
/// this shape exists to prevent.
/// </para>
/// </remarks>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class SmC2BaselineReconstructionValidationTest
{
    private const string BaselineJsonFileName = "sm-c2-hot-path-baseline-v1.json";
    private const string BaselineMarkdownFileName = "sm-c2-hot-path-baseline-v1.md";

    [Fact]
    public void BaselineShouldRecordAnAuditableReconstructionMethod()
    {
        using JsonDocument document = LoadEvidence(BaselineJsonFileName);
        JsonElement reconstruction = document.RootElement.GetProperty("reconstruction");
        JsonElement overlay = reconstruction.GetProperty("fixtureOverlay");

        reconstruction.GetProperty("required").GetBoolean().ShouldBeTrue();
        reconstruction.GetProperty("method").GetString()
            .ShouldBe("overlay-versioned-fixture-onto-preserved-source-commit");
        overlay.GetProperty("presentAtSourceCommit").GetBoolean().ShouldBeFalse();
        overlay.GetProperty("identicalInPostRun").GetBoolean().ShouldBeTrue();
        reconstruction.GetProperty("reason").GetString().ShouldNotBeNullOrWhiteSpace();
        reconstruction.GetProperty("equivalence").GetString().ShouldNotBeNullOrWhiteSpace();

        // The limitation must stay stated. A reconstruction that quietly drops it reads as a gate that could
        // have failed for this story, which it could not.
        reconstruction.GetProperty("residualLimitation").GetString()
            .ShouldNotBeNull()
            .ShouldContain("changed no source inside the measured closure", Case.Sensitive);

        // The declared overlay must be the file on disk, byte for byte.
        string fixturePath = Path.Combine(FindRepositoryRoot(), overlay.GetProperty("path").GetString()!);
        File.Exists(fixturePath).ShouldBeTrue(fixturePath);
        ComputeSha256(fixturePath).ShouldBe(overlay.GetProperty("sha256").GetString());
        document.RootElement.GetProperty("fixture").GetProperty("sha256").GetString()
            .ShouldBe(overlay.GetProperty("sha256").GetString());
    }

    [Fact]
    public void FixtureShouldDependOnlyOnTheDeclaredMeasuredClosure()
    {
        using JsonDocument document = LoadEvidence(BaselineJsonFileName);
        JsonElement reconstruction = document.RootElement.GetProperty("reconstruction");
        string[] declaredProjects =
        [
            .. reconstruction.GetProperty("measuredProductionClosure")
                .GetProperty("projects")
                .EnumerateArray()
                .Select(static project => project.GetString()!),
        ];

        declaredProjects.Order(StringComparer.Ordinal).ShouldBe(
            ["src/Hexalith.Conversations", "src/Hexalith.Conversations.Contracts"]);

        string fixturePath = Path.Combine(
            FindRepositoryRoot(),
            reconstruction.GetProperty("fixtureOverlay").GetProperty("path").GetString()!);
        string[] hexalithNamespaces =
        [
            .. File.ReadLines(fixturePath)
                .Select(static line => line.Trim())
                .Where(static line => line.StartsWith("using Hexalith.", StringComparison.Ordinal))
                .Select(static line => line["using ".Length..].TrimEnd(';')),
        ];

        hexalithNamespaces.ShouldNotBeEmpty(
            "the fixture scan found no Hexalith usings, so the closure claim would pass without being checked");

        // The measured closure is exactly the domain and contracts projects. A using that reached the Server,
        // Client, Admin, or platform assemblies would mean the baseline measures code the declared closure does
        // not cover, and the git equality check below would no longer justify the comparison.
        foreach (string usedNamespace in hexalithNamespaces)
        {
            usedNamespace.ShouldStartWith("Hexalith.Conversations", Case.Sensitive);
            foreach (string forbidden in new[]
                     {
                         "Hexalith.Conversations.Server",
                         "Hexalith.Conversations.Client",
                         "Hexalith.Conversations.Admin",
                         "Hexalith.Conversations.Testing",
                         "Hexalith.EventStore",
                         "Hexalith.Commons",
                     })
            {
                usedNamespace.StartsWith(forbidden, StringComparison.Ordinal).ShouldBeFalse(
                    $"'{usedNamespace}' is outside the declared measured production closure");
            }
        }
    }

    [Fact]
    public void GitShouldConfirmTheFixtureIsAbsentAtTheSourceCommitAndTheClosureIsUnchanged()
    {
        using JsonDocument document = LoadEvidence(BaselineJsonFileName);
        string sourceCommit = document.RootElement.GetProperty("sourceCommit").GetString()!;
        JsonElement reconstruction = document.RootElement.GetProperty("reconstruction");
        string fixtureRelativePath = reconstruction.GetProperty("fixtureOverlay").GetProperty("path").GetString()!;
        string[] closureProjects =
        [
            .. reconstruction.GetProperty("measuredProductionClosure")
                .GetProperty("projects")
                .EnumerateArray()
                .Select(static project => project.GetString()!),
        ];

        if (!TryRunGit(out _, "rev-parse", "--verify", $"{sourceCommit}^{{commit}}"))
        {
            Assert.Skip(
                $"The declared source commit {sourceCommit} is not resolvable in this checkout, so the "
                + "reconstruction claims cannot be verified against git here.");
        }

        int executedChecks = 0;

        // The fixture must be absent at the source commit: that absence is the whole reason a reconstruction
        // was permitted. If it were present, the artifact should have recorded a direct run instead.
        TryRunGit(out _, "cat-file", "-e", $"{sourceCommit}:{fixtureRelativePath}").ShouldBeFalse(
            "the fixture is present at the source commit, so the recorded reconstruction is not the honest method");
        executedChecks++;

        // The measured closure must be unchanged between the source commit and this revision, which is what
        // makes the overlaid baseline comparable to the post run.
        string[] arguments = ["diff", "--name-only", $"{sourceCommit}..HEAD", "--", .. closureProjects];
        TryRunGit(out string changed, arguments).ShouldBeTrue("the closure comparison could not be executed");
        string[] changedFiles =
        [
            .. changed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        ];
        changedFiles.ShouldBeEmpty(
            "the measured production closure changed since the declared source commit, so the reconstructed "
            + "baseline no longer measures the same sources as the post run");
        executedChecks++;

        reconstruction.GetProperty("measuredProductionClosure")
            .GetProperty("verification")
            .GetProperty("changedFileCount")
            .GetInt32()
            .ShouldBe(changedFiles.Length);
        executedChecks++;

        executedChecks.ShouldBe(3, "every git-backed reconstruction check must have executed");
    }

    [Fact]
    public void MarkdownShouldPresentTheReconstructionProvenance()
    {
        string markdown = File.ReadAllText(
            Path.Combine(ReleaseEvidenceDirectory(), BaselineMarkdownFileName));

        markdown.ShouldContain("## Reconstruction provenance", Case.Sensitive);
        markdown.ShouldContain("does **not** exist at source commit", Case.Sensitive);
        markdown.ShouldContain("overlay the versioned fixture", Case.Sensitive);
        markdown.ShouldContain("**0 changed files**", Case.Sensitive);
        markdown.ShouldContain("Residual limitation", Case.Sensitive);
    }

    private static bool TryRunGit(out string standardOutput, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepositoryRoot(),
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                standardOutput = string.Empty;
                return false;
            }

            standardOutput = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(milliseconds: 60_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            standardOutput = string.Empty;
            return false;
        }
    }

    private static string ComputeSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static JsonDocument LoadEvidence(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ReleaseEvidenceDirectory(), fileName)));

    private static string ReleaseEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "docs", "release-evidence");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
