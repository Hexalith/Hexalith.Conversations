// <copyright file="PackageEnvironmentAuthorityV18ValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Independently validates the additive V18 package-environment authority.
/// </summary>
public sealed class PackageEnvironmentAuthorityV18ValidationTest
{
    private const string AuthorityPath = "_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json";
    private const string BaselineCommit = "b819a7c43a7024295abaabd418a74f5f64cb5af0";
    private const string BuildsPath = "references/Hexalith.Builds";
    private const string BuildsCatalogPath = "Props/Directory.Packages.props";
    private const int ProcessTimeoutMilliseconds = 30_000;

    private static readonly string[] C1Paths =
    [
        ".github/workflows/planning-authority-preflight.yml",
        "_bmad-output/implementation-artifacts/spec-update-all-packages.md",
        "_bmad/schemas/v18-package-environment-authority-v1.schema.json",
        "_bmad/scripts/publish_v18_package_environment_authority.py",
        "_bmad/scripts/tests/test_publish_v18_package_environment_authority.py",
        "global.json",
        "package-lock.json",
        BuildsPath,
        "tests/Hexalith.Conversations.Conformance.Tests/PackageEnvironmentAuthorityV18ValidationTest.cs",
        "tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs",
        "uv.lock",
    ];

    private static readonly string[] CandidateFilePaths = C1Paths.Where(path => path != BuildsPath).ToArray();
    private static readonly string[] CombinedPaths = C1Paths.Append(AuthorityPath).Order(StringComparer.Ordinal).ToArray();

    private static readonly string[] SourcePaths =
    [
        ".github/workflows/planning-authority-preflight.yml",
        "global.json",
        "package-lock.json",
        "package.json",
        "pyproject.toml",
        "uv.lock",
    ];

    private static readonly (string Name, string Version)[] PythonPackages =
    [
        ("attrs", "26.1.0"),
        ("colorama", "0.4.6"),
        ("hexalith-conversations-planning", "0.0.0"),
        ("iniconfig", "2.3.0"),
        ("jsonschema", "4.26.0"),
        ("jsonschema-specifications", "2025.9.1"),
        ("packaging", "26.3"),
        ("pluggy", "1.6.0"),
        ("pygments", "2.21.0"),
        ("pytest", "9.1.1"),
        ("referencing", "0.37.0"),
        ("rpds-py", "2026.6.3"),
        ("typing-extensions", "4.16.0"),
    ];

    private static readonly IReadOnlyDictionary<string, string> ImmutableAuthorityDigests =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json"] = "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea",
            ["_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json"] = "5b71e6fbf8851f790af92f0a0d056d7f7e04b3b9749c3a2f3f4ef5f87312d45a",
            ["_bmad-output/planning-artifacts/v17-implementation-hold-decision-authority-v1.json"] = "1444f76dad9495d4c17354a9f2f5d3ce9f456cfd254c66d4a6e77e5abf446e50",
            ["_bmad-output/planning-artifacts/implementation-hold-v1.json"] = "2c594075e8b212c7db05b00fa9bb3f1c626845437dc819c7f0e39460f5d80b12",
        };

    /// <summary>
    /// Recomputes the complete V18 C1/C2 package transaction from committed objects.
    /// </summary>
    [Fact]
    public void V18AuthorityShouldBindExactPackageEnvironmentAtAnyDescendant()
    {
        string head = RunGitText(FindRepositoryRoot(), "rev-parse", "HEAD");
        string publication = LocatePublication(head);
        RequireAncestor(publication, head);
        byte[] authorityBytes = ReadGitBlob(publication, AuthorityPath);
        ReadGitBlob(head, AuthorityPath).ShouldBe(authorityBytes);
        JsonObject authority = RequiredObject(JsonNode.Parse(authorityBytes));
        string candidate = ValidateClosedAuthority(authority);

        RequireSingleParent(candidate, BaselineCommit);
        RequireSingleParent(publication, candidate);
        RunGitPaths(BaselineCommit, candidate).ShouldBe(C1Paths);
        RunGitPaths(candidate, publication).ShouldBe([AuthorityPath]);
        RunGitPaths(BaselineCommit, publication).ShouldBe(CombinedPaths);
        ChangedGitlinks(BaselineCommit, candidate).ShouldBe([BuildsPath]);
        ChangedGitlinks(candidate, publication).ShouldBeEmpty();

        ReadGitBlob(candidate, "package.json").ShouldBe(ReadGitBlob(BaselineCommit, "package.json"));
        ReadGitBlob(candidate, "pyproject.toml").ShouldBe(ReadGitBlob(BaselineCommit, "pyproject.toml"));
        ReadPythonPackages(ReadGitBlob(candidate, "uv.lock")).ShouldBe(PythonPackages);
        ValidateNpmParity(candidate);
        ValidateToolchain(candidate);
        ValidateBuildsCatalog(candidate);
    }

    /// <summary>
    /// Proves closed properties, byte bindings, gitlinks, channels, and hold semantics reject faults.
    /// </summary>
    /// <param name="fault">The named isolated fault.</param>
    [Theory]
    [InlineData("unknown")]
    [InlineData("ledger")]
    [InlineData("hash")]
    [InlineData("gitlink")]
    [InlineData("toolchain")]
    [InlineData("hold")]
    public void V18AuthorityNamedClosedContractFaultShouldBeRejected(string fault)
    {
        JsonObject mutation = CloneAuthority();
        switch (fault)
        {
            case "unknown":
                mutation["unexpected"] = true;
                break;
            case "ledger":
                RequiredArray(mutation["assertionLedger"]).Clear();
                break;
            case "hash":
                RequiredObject(RequiredArray(mutation["sourceBindings"])[0])["sha256"] = new string('0', 64);
                break;
            case "gitlink":
                RequiredObject(RequiredArray(mutation["gitlinks"])[0])["mode"] = "100644";
                break;
            case "toolchain":
                RequiredObject(mutation["toolchain"])["communityToolkitAspireDapr"] = "13.5.0";
                break;
            case "hold":
                RequiredObject(mutation["authorityEffect"])["story71CandidateBindingResolved"] = true;
                break;
            default:
                throw new InvalidOperationException($"Unknown V18 fault '{fault}'.");
        }

        Should.Throw<InvalidDataException>(() => ValidateClosedAuthority(mutation));
    }

    private static string ValidateClosedAuthority(JsonObject authority)
    {
        RequireProperties(authority, "schemaVersion", "authorityId", "baselineCommit", "candidateCommit", "publication", "candidateFiles", "gitlinks", "sourceBindings", "directPins", "pythonEnvironment", "toolchain", "buildsCatalog", "immutableAuthorities", "authorityEffect", "resultSemantics", "result", "assertionLedger");
        Require(RequiredString(authority, "schemaVersion") == "hexalith.conversations.v18-package-environment-authority.v1", "schemaVersion");
        Require(RequiredString(authority, "authorityId") == "V18-PACKAGE-ENVIRONMENT-AUTHORITY", "authorityId");
        Require(RequiredString(authority, "baselineCommit") == BaselineCommit, "baselineCommit");
        string candidate = RequiredString(authority, "candidateCommit");
        Require(Regex.IsMatch(candidate, "^[0-9a-f]{40}$", RegexOptions.CultureInvariant), "candidateCommit");

        JsonObject publication = RequiredObject(authority["publication"]);
        RequireProperties(publication, "c1Paths", "c2Path", "combinedPaths", "changedGitlinks");
        Require(ReadStrings(publication, "c1Paths").SequenceEqual(C1Paths, StringComparer.Ordinal), "c1Paths");
        Require(RequiredString(publication, "c2Path") == AuthorityPath, "c2Path");
        Require(ReadStrings(publication, "combinedPaths").SequenceEqual(CombinedPaths, StringComparer.Ordinal), "combinedPaths");
        Require(ReadStrings(publication, "changedGitlinks").SequenceEqual([BuildsPath], StringComparer.Ordinal), "changedGitlinks");

        ValidateFileBindings(RequiredArray(authority["candidateFiles"]), CandidateFilePaths, candidate);
        ValidateFileBindings(RequiredArray(authority["sourceBindings"]), SourcePaths, candidate);
        ValidateGitlink(RequiredArray(authority["gitlinks"]), candidate);
        ValidateDirectPins(RequiredObject(authority["directPins"]));
        ValidatePythonEnvironment(RequiredObject(authority["pythonEnvironment"]));
        ValidateToolchainAuthority(RequiredObject(authority["toolchain"]));
        ValidateBuildsAuthority(RequiredObject(authority["buildsCatalog"]), candidate);
        ValidateImmutableAuthorities(RequiredArray(authority["immutableAuthorities"]), candidate);
        ValidateAuthorityEffect(RequiredObject(authority["authorityEffect"]));

        JsonObject semantics = RequiredObject(authority["resultSemantics"]);
        RequireProperties(semantics, "states", "ledgerRequired", "skipsAllowed");
        Require(ReadStrings(semantics, "states").SequenceEqual(["PASS", "FAIL", "BLOCKED", "not-applicable"], StringComparer.Ordinal), "result states");
        Require(RequiredBool(semantics, "ledgerRequired") && !RequiredBool(semantics, "skipsAllowed"), "result semantics");
        Require(RequiredString(authority, "result") == "PASS", "result");
        ValidateLedger(RequiredArray(authority["assertionLedger"]));
        return candidate;
    }

    private static void ValidateFileBindings(JsonArray bindings, string[] expectedPaths, string candidate)
    {
        Require(bindings.Count == expectedPaths.Length, "file binding count");
        Require(bindings.Select(RequiredObject).Select(row => RequiredString(row, "path")).SequenceEqual(expectedPaths, StringComparer.Ordinal), "file binding paths");
        foreach (JsonObject row in bindings.Select(RequiredObject))
        {
            RequireProperties(row, "path", "sha256", "mode");
            string path = RequiredString(row, "path");
            (string mode, string type, _) = ReadTreeRecord(candidate, path);
            Require(RequiredString(row, "mode") == "100644" && mode == "100644" && type == "blob", "file binding mode");
            Require(RequiredString(row, "sha256") == Sha256(ReadGitBlob(candidate, path)), "file binding digest");
        }
    }

    private static void ValidateGitlink(JsonArray gitlinks, string candidate)
    {
        Require(gitlinks.Count == 1, "gitlink count");
        JsonObject row = RequiredObject(gitlinks[0]);
        RequireProperties(row, "path", "mode", "baselineCommit", "candidateCommit");
        (string mode, string type, string commit) = ReadTreeRecord(candidate, BuildsPath);
        (_, _, string baselineBuilds) = ReadTreeRecord(BaselineCommit, BuildsPath);
        Require(RequiredString(row, "path") == BuildsPath && RequiredString(row, "mode") == "160000", "gitlink identity");
        Require(mode == "160000" && type == "commit" && RequiredString(row, "candidateCommit") == commit, "gitlink candidate");
        Require(RequiredString(row, "baselineCommit") == baselineBuilds && baselineBuilds != commit, "gitlink baseline");
    }

    private static void ValidateDirectPins(JsonObject directPins)
    {
        RequireProperties(directPins, "npm", "python", "unchangedFromBaseline");
        JsonObject npm = RequiredObject(directPins["npm"]);
        RequireProperties(npm, "@commitlint/cli", "@commitlint/config-conventional");
        Require(RequiredString(npm, "@commitlint/cli") == "21.2.2" && RequiredString(npm, "@commitlint/config-conventional") == "21.2.2", "npm direct pins");
        Require(ReadStrings(directPins, "python").SequenceEqual(["jsonschema==4.26.0", "pytest==9.1.1"], StringComparer.Ordinal), "Python direct pins");
        Require(RequiredBool(directPins, "unchangedFromBaseline"), "unchanged direct pins");
    }

    private static void ValidatePythonEnvironment(JsonObject environment)
    {
        RequireProperties(environment, "packageCount", "packages");
        Require(RequiredInt(environment, "packageCount") == PythonPackages.Length, "Python package count");
        JsonArray packages = RequiredArray(environment["packages"]);
        Require(packages.Count == PythonPackages.Length, "Python package rows");
        for (int index = 0; index < packages.Count; index++)
        {
            JsonObject row = RequiredObject(packages[index]);
            RequireProperties(row, "name", "version");
            Require(RequiredString(row, "name") == PythonPackages[index].Name && RequiredString(row, "version") == PythonPackages[index].Version, "Python package identity");
        }
    }

    private static void ValidateToolchainAuthority(JsonObject toolchain)
    {
        RequireProperties(toolchain, "dotnetSdk", "uv", "aspire", "communityToolkitAspireDapr", "microsoftNetTestSdk");
        Require(RequiredString(toolchain, "dotnetSdk") == "10.0.401", ".NET SDK");
        Require(RequiredString(toolchain, "uv") == "0.12.13", "uv");
        Require(RequiredString(toolchain, "aspire") == "13.5.3", "Aspire");
        Require(RequiredString(toolchain, "communityToolkitAspireDapr") == "13.5.1-beta.751", "CommunityToolkit Dapr");
        Require(RequiredString(toolchain, "microsoftNetTestSdk") == "18.10.0", "Microsoft.NET.Test.Sdk");
    }

    private static void ValidateBuildsAuthority(JsonObject catalog, string candidate)
    {
        RequireProperties(catalog, "catalogPath", "catalogSha256", "aspireVersion", "communityToolkitAspireDaprVersion", "microsoftNetTestSdkVersion");
        Require(RequiredString(catalog, "catalogPath") == BuildsCatalogPath, "Builds catalog path");
        (_, _, string buildsCommit) = ReadTreeRecord(candidate, BuildsPath);
        byte[] bytes = ReadBuildsBlob(buildsCommit, BuildsCatalogPath);
        Require(RequiredString(catalog, "catalogSha256") == Sha256(bytes), "Builds catalog digest");
        Require(RequiredString(catalog, "aspireVersion") == "13.5.3", "Builds Aspire version");
        Require(RequiredString(catalog, "communityToolkitAspireDaprVersion") == "13.5.1-beta.751", "Builds Toolkit Dapr version");
        Require(RequiredString(catalog, "microsoftNetTestSdkVersion") == "18.10.0", "Builds test SDK version");
    }

    private static void ValidateImmutableAuthorities(JsonArray immutable, string candidate)
    {
        Require(immutable.Count == ImmutableAuthorityDigests.Count, "immutable count");
        Require(immutable.Select(RequiredObject).Select(row => RequiredString(row, "path")).SequenceEqual(ImmutableAuthorityDigests.Keys, StringComparer.Ordinal), "immutable paths");
        foreach (JsonObject row in immutable.Select(RequiredObject))
        {
            RequireProperties(row, "path", "sha256", "mode");
            string path = RequiredString(row, "path");
            Require(ImmutableAuthorityDigests.TryGetValue(path, out string? digest), "immutable path");
            Require(RequiredString(row, "sha256") == digest && RequiredString(row, "mode") == "100644", "immutable authority binding");
            Require(Sha256(ReadGitBlob(candidate, path)) == digest, "immutable authority bytes");
        }
    }

    private static void ValidateAuthorityEffect(JsonObject effect)
    {
        RequireProperties(effect, "implementationHold", "story71CandidateBindingResolved", "historicalEvidenceRewritten", "releaseAuthorized", "pushAuthorized");
        Require(RequiredString(effect, "implementationHold") == "ACTIVE", "implementation hold");
        Require(!RequiredBool(effect, "story71CandidateBindingResolved"), "Story 7.1 binding");
        Require(!RequiredBool(effect, "historicalEvidenceRewritten"), "historical evidence");
        Require(!RequiredBool(effect, "releaseAuthorized") && !RequiredBool(effect, "pushAuthorized"), "release and push authority");
    }

    private static void ValidateLedger(JsonArray ledger)
    {
        string[] ids = ["V18-C1", "V18-C2", "V18-GITLINK", "V18-NPM", "V18-PYTHON", "V18-TOOLCHAIN", "V18-PREDECESSORS", "V18-HOLD"];
        Require(ledger.Count == ids.Length, "assertion ledger count");
        Require(ledger.Select(RequiredObject).Select(row => RequiredString(row, "id")).SequenceEqual(ids, StringComparer.Ordinal), "assertion ledger ids");
        Require(ledger.Select(RequiredObject).All(row => RequiredString(row, "state") == "PASS"), "assertion ledger states");
        foreach (JsonObject row in ledger.Select(RequiredObject))
        {
            string[] names = RequiredString(row, "id") == "V18-GITLINK" ? ["id", "subject", "state", "paths"] : ["id", "subject", "state"];
            RequireProperties(row, names);
        }
    }

    private static void ValidateNpmParity(string candidate)
    {
        JsonObject manifest = RequiredObject(JsonNode.Parse(ReadGitBlob(candidate, "package.json")));
        JsonObject lockFile = RequiredObject(JsonNode.Parse(ReadGitBlob(candidate, "package-lock.json")));
        JsonObject manifestPins = RequiredObject(manifest["devDependencies"]);
        JsonObject packages = RequiredObject(lockFile["packages"]);
        JsonObject lockPins = RequiredObject(RequiredObject(packages[""])["devDependencies"]);
        Require(JsonNode.DeepEquals(manifestPins, lockPins), "npm manifest-lock parity");
    }

    private static void ValidateToolchain(string candidate)
    {
        JsonObject globalJson = RequiredObject(JsonNode.Parse(ReadGitBlob(candidate, "global.json")));
        JsonObject sdk = RequiredObject(globalJson["sdk"]);
        Require(RequiredString(sdk, "version") == "10.0.401" && RequiredString(sdk, "rollForward") == "latestPatch", ".NET SDK source");
        string workflow = Encoding.UTF8.GetString(ReadGitBlob(candidate, ".github/workflows/planning-authority-preflight.yml"));
        Require(Regex.Matches(workflow, "uv==0\\.12\\.13", RegexOptions.CultureInvariant).Count == 1, "uv workflow pin");
        string appHost = Encoding.UTF8.GetString(ReadGitBlob(candidate, "src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj"));
        Require(appHost.Contains("Aspire.AppHost.Sdk/13.5.3", StringComparison.Ordinal), "AppHost SDK pin");
    }

    private static void ValidateBuildsCatalog(string candidate)
    {
        (_, _, string buildsCommit) = ReadTreeRecord(candidate, BuildsPath);
        string catalogText = Encoding.UTF8.GetString(ReadBuildsBlob(buildsCommit, BuildsCatalogPath)).TrimStart('\uFEFF');
        XDocument catalog = XDocument.Parse(catalogText);
        Dictionary<string, string> rows = catalog.Descendants("PackageVersion")
            .Where(row => row.Attribute("Include") is not null && row.Attribute("Version") is not null)
            .ToDictionary(row => row.Attribute("Include")!.Value, row => row.Attribute("Version")!.Value, StringComparer.OrdinalIgnoreCase);
        rows["Aspire.Hosting"].ShouldBe("13.5.3");
        rows["Aspire.Hosting.Testing"].ShouldBe("13.5.3");
        rows["CommunityToolkit.Aspire.Hosting.Dapr"].ShouldBe("13.5.1-beta.751");
        rows["Microsoft.Extensions.Http"].ShouldBe("10.0.12");
        rows["Microsoft.NET.Test.Sdk"].ShouldBe("18.10.0");
        rows["System.Text.Json"].ShouldBe("10.0.12");
        rows.Where(row => row.Value == "10.0.11").ShouldBeEmpty();
    }

    private static (string Name, string Version)[] ReadPythonPackages(byte[] bytes)
    {
        string[] blocks = Encoding.UTF8.GetString(bytes).Split("[[package]]", StringSplitOptions.RemoveEmptyEntries);
        return blocks
            .Select(block => (
                Name: Regex.Match(block, "(?m)^name = \"([^\"]+)\"$").Groups[1].Value,
                Version: Regex.Match(block, "(?m)^version = \"([^\"]+)\"$") is { Success: true } match ? match.Groups[1].Value : "0.0.0"))
            .Where(row => row.Name.Length > 0)
            .OrderBy(row => row.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonObject CloneAuthority()
    {
        string publication = LocatePublication(RunGitText(FindRepositoryRoot(), "rev-parse", "HEAD"));
        return RequiredObject(JsonNode.Parse(ReadGitBlob(publication, AuthorityPath))).DeepClone().AsObject();
    }

    private static string LocatePublication(string descendant)
    {
        string[] commits = RunGitText(FindRepositoryRoot(), "log", "--format=%H", "--diff-filter=A", descendant, "--", AuthorityPath)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Require(commits.Length == 1, "publication discovery");
        return commits[0];
    }

    private static void RequireSingleParent(string commit, string expected)
    {
        string[] record = RunGitText(FindRepositoryRoot(), "rev-list", "--parents", "-n", "1", commit).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Require(record.Length == 2 && record[0] == commit && record[1] == expected, "single parent");
    }

    private static void RequireAncestor(string ancestor, string descendant)
    {
        (int exitCode, _, _) = RunGitRaw(FindRepositoryRoot(), "merge-base", "--is-ancestor", ancestor, descendant);
        Require(exitCode == 0, "publication ancestry");
    }

    private static void RequireProperties(JsonObject value, params string[] names) => Require(value.Select(property => property.Key).Order(StringComparer.Ordinal).SequenceEqual(names.Order(StringComparer.Ordinal), StringComparer.Ordinal), "closed properties");

    private static void Require(bool condition, string subject)
    {
        if (!condition)
        {
            throw new InvalidDataException($"V18 authority validation failed: {subject}.");
        }
    }

    private static JsonObject RequiredObject(JsonNode? node) => node as JsonObject ?? throw new InvalidDataException("Expected a JSON object.");

    private static JsonArray RequiredArray(JsonNode? node) => node as JsonArray ?? throw new InvalidDataException("Expected a JSON array.");

    private static string RequiredString(JsonObject value, string property) => value[property]?.GetValue<string>() ?? throw new InvalidDataException($"Expected string '{property}'.");

    private static int RequiredInt(JsonObject value, string property) => value[property]?.GetValue<int>() ?? throw new InvalidDataException($"Expected integer '{property}'.");

    private static bool RequiredBool(JsonObject value, string property) => value[property]?.GetValue<bool>() ?? throw new InvalidDataException($"Expected Boolean '{property}'.");

    private static string[] ReadStrings(JsonObject value, string property) => RequiredArray(value[property]).Select(item => item?.GetValue<string>() ?? throw new InvalidDataException($"Expected string in '{property}'.")).ToArray();

    private static string[] RunGitPaths(string baseline, string candidate) => SplitNul(RunGitBytes(FindRepositoryRoot(), "diff", "--name-only", "-z", baseline, candidate, "--")).Select(Encoding.UTF8.GetString).Order(StringComparer.Ordinal).ToArray();

    private static byte[] ReadGitBlob(string revision, string path) => RunGitBytes(FindRepositoryRoot(), "show", $"{revision}:{path}");

    private static byte[] ReadBuildsBlob(string revision, string path) => RunGitBytes(Path.Combine(FindRepositoryRoot(), BuildsPath), "show", $"{revision}:{path}");

    private static (string Mode, string Type, string ObjectId) ReadTreeRecord(string revision, string path)
    {
        string value = RunGitText(FindRepositoryRoot(), "ls-tree", revision, "--", path);
        Match match = Regex.Match(value, "^([0-7]{6}) (blob|commit) ([0-9a-f]{40})\\t", RegexOptions.CultureInvariant);
        Require(match.Success, "tree record");
        return (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
    }

    private static string[] ChangedGitlinks(string baseline, string candidate)
    {
        string[] records = SplitNul(RunGitBytes(FindRepositoryRoot(), "diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--")).Select(Encoding.UTF8.GetString).ToArray();
        List<string> paths = [];
        for (int index = 0; index < records.Length; index += 2)
        {
            Require(index + 1 < records.Length, "raw gitlink record");
            string[] fields = records[index].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length >= 5 && (fields[0] == ":160000" || fields[1] == "160000"))
            {
                paths.Add(records[index + 1]);
            }
        }

        return [.. paths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    private static string RunGitText(string workingDirectory, params string[] arguments) => Encoding.UTF8.GetString(RunGitBytes(workingDirectory, arguments)).TrimEnd('\r', '\n');

    private static byte[] RunGitBytes(string workingDirectory, params string[] arguments)
    {
        (int exitCode, byte[] output, string error) = RunGitRaw(workingDirectory, arguments);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(error);
        }

        return output;
    }

    private static (int ExitCode, byte[] Output, string Error) RunGitRaw(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start Git.");
        Task<byte[]> outputTask = DrainAsync(process.StandardOutput.BaseStream);
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(ProcessTimeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Git exceeded {ProcessTimeoutMilliseconds} ms.");
        }

        if (!Task.WaitAll([outputTask, errorTask], ProcessTimeoutMilliseconds))
        {
            throw new TimeoutException("Git output drains did not complete.");
        }

        return (process.ExitCode, outputTask.GetAwaiter().GetResult(), errorTask.GetAwaiter().GetResult());
    }

    private static async Task<byte[]> DrainAsync(Stream stream)
    {
        using MemoryStream output = new();
        await stream.CopyToAsync(output).ConfigureAwait(false);
        return output.ToArray();
    }

    private static IEnumerable<byte[]> SplitNul(byte[] content)
    {
        int start = 0;
        for (int index = 0; index <= content.Length; index++)
        {
            if (index != content.Length && content[index] != 0)
            {
                continue;
            }

            if (index > start)
            {
                yield return content[start..index];
            }

            start = index + 1;
        }
    }

    private static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

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

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
