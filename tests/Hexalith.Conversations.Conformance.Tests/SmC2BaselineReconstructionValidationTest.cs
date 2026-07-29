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
/// test project and EventStore baseline are pinned, the overlays match the post run byte-for-byte, and the fixture
/// depends on nothing outside its declared measured closure.
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

        // The limitation must stay stated, while the production-path closure must make clear that this gate
        // can now expose the query/projection regressions the former toy workload could not observe.
        reconstruction.GetProperty("residualLimitation").GetString()
            .ShouldNotBeNull()
            .ShouldContain("can expose a Story 6.2 regression", Case.Sensitive);

        // The declared overlay must be the file on disk, byte for byte.
        string fixturePath = Path.Combine(FindRepositoryRoot(), overlay.GetProperty("path").GetString()!);
        File.Exists(fixturePath).ShouldBeTrue(fixturePath);
        ComputeSha256(fixturePath).ShouldBe(overlay.GetProperty("sha256").GetString());
        document.RootElement.GetProperty("fixture").GetProperty("sha256").GetString()
            .ShouldBe(overlay.GetProperty("sha256").GetString());

        JsonElement projectOverlay = reconstruction.GetProperty("projectOverlay");
        projectOverlay.GetProperty("presentAtSourceCommit").GetBoolean().ShouldBeTrue();
        projectOverlay.GetProperty("identicalInPostRun").GetBoolean().ShouldBeTrue();
        string projectPath = Path.Combine(FindRepositoryRoot(), projectOverlay.GetProperty("path").GetString()!);
        ComputeSha256(projectPath).ShouldBe(projectOverlay.GetProperty("sha256").GetString());
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
        [
            "references/Hexalith.EventStore/src/Hexalith.EventStore.Client",
            "references/Hexalith.EventStore/src/Hexalith.EventStore.Testing",
            "src/Hexalith.Conversations",
            "src/Hexalith.Conversations.Contracts",
            "src/Hexalith.Conversations.Server",
        ]);

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

        hexalithNamespaces.ShouldContain("Hexalith.Conversations.Server.Queries");
        hexalithNamespaces.ShouldContain("Hexalith.EventStore.Testing.Fakes");

        string[] allowedPrefixes = ["Hexalith.Conversations", "Hexalith.EventStore.Client", "Hexalith.EventStore.Testing"];
        foreach (string usedNamespace in hexalithNamespaces)
        {
            allowedPrefixes.Any(prefix => usedNamespace.StartsWith(prefix, StringComparison.Ordinal)).ShouldBeTrue(
                $"'{usedNamespace}' is outside the declared measured production closure");
        }
    }

    [Fact]
    public void GitShouldConfirmTheFixtureIsAbsentAndTheBaselineSubmoduleIsPinned()
    {
        using JsonDocument document = LoadEvidence(BaselineJsonFileName);
        string sourceCommit = document.RootElement.GetProperty("sourceCommit").GetString()!;
        JsonElement reconstruction = document.RootElement.GetProperty("reconstruction");
        string fixtureRelativePath = reconstruction.GetProperty("fixtureOverlay").GetProperty("path").GetString()!;
        string projectRelativePath = reconstruction.GetProperty("projectOverlay").GetProperty("path").GetString()!;
        string baselineEventStoreCommit = reconstruction.GetProperty("baselineEventStoreCommit").GetString()!;

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

        TryRunGit(out _, "cat-file", "-e", $"{sourceCommit}:{projectRelativePath}").ShouldBeTrue(
            "the project overlay claims to replace a file that was not present at the source commit");
        executedChecks++;

        TryRunGit(out string eventStoreCommit, "rev-parse", $"{sourceCommit}:references/Hexalith.EventStore")
            .ShouldBeTrue("the baseline EventStore gitlink could not be resolved");
        eventStoreCommit.Trim().ShouldBe(baselineEventStoreCommit);
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
        markdown.ShouldContain("byte-identical to the post run", Case.Sensitive);
        markdown.ShouldContain("baseline gitlink", Case.Sensitive);
        markdown.ShouldContain("Residual limitation", Case.Insensitive);
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
