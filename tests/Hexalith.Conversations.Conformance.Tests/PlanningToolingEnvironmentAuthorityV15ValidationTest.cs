// <copyright file="PlanningToolingEnvironmentAuthorityV15ValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Independently validates the candidate-bound V15 planning-tooling environment authority.
/// </summary>
public sealed partial class PlanningToolingEnvironmentAuthorityV15ValidationTest
{
    private const string AuthorityPath = "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json";
    private const string BaselineCommit = "6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4";
    private const string Ir0Path = "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md";
    private const string Ir0Sha256 = "862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad";
    private const string JsonschemaSdistSha256 = "0c26707e2efad8aa1bfc5b7ce170f3fccc2e4918ff85989ba9ffa9facb2be326";
    private const string JsonschemaWheelSha256 = "d489f15263b8d200f8387e64b4c3a75f06629559fb73deb8fdfb525f2dab50ce";
    private const string PytestSdistSha256 = "1088fbde8f2b49d95a549a195707afa7a76a3ce9bcadc26b6d71f0ffda5fe313";
    private const string PytestWheelSha256 = "37a86b45efb9a47a61a36449063e8e18d0cab3161329fc099eb21783169c4f0c";
    private const string V9BundleDigest = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055";
    private const string V9Candidate = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5";
    private const string V9Path = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json";
    private const string V9Sha256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3";

    private static readonly string[] C1Paths =
    [
        ".github/workflows/planning-authority-preflight.yml",
        "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
        "_bmad/schemas/v15-planning-tooling-environment-authority-v1.schema.json",
        "_bmad/scripts/publish_v15_planning_tooling_environment.py",
        "_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py",
        "_bmad/scripts/tests/test_verify_evidence_boundary.py",
        "_bmad/scripts/verify_evidence_boundary.py",
        "pyproject.toml",
        "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs",
        "uv.lock",
    ];

    private static readonly string[] CombinedPaths = C1Paths.Append(AuthorityPath).Order(StringComparer.Ordinal).ToArray();

    private static readonly string[] PackageNames =
    [
        "attrs",
        "colorama",
        "hexalith-conversations-planning",
        "iniconfig",
        "jsonschema",
        "jsonschema-specifications",
        "packaging",
        "pluggy",
        "pygments",
        "pytest",
        "referencing",
        "rpds-py",
        "typing-extensions",
    ];

    private static readonly IReadOnlyDictionary<string, string> ImmutableAuthorityDigests =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [V9Path] = V9Sha256,
            ["_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json"] = "c082cde6923e9831eea768be6c547ca1ab87ed91244185b505bdf3ae1c116dcc",
            ["_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"] = "f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f",
            ["_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json"] = "e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f",
            [Ir0Path] = Ir0Sha256,
        };

    /// <summary>
    /// Recomputes the C1/C2 path, mode, hash, predecessor, package, IR-0, and anti-vacuity evidence.
    /// </summary>
    [Fact]
    public void V15AuthorityShouldBindExactEnvironmentAndTwoCommitBoundary()
    {
        JsonObject authority = ReadAuthority();
        ValidateClosedAuthority(authority);
        string candidate = RequiredString(authority, "candidateCommit");

        RunGitText("rev-parse", $"{candidate}^").ShouldBe(BaselineCommit);
        RunGitPaths("diff", "--name-only", "-z", BaselineCommit, candidate, "--").ShouldBe(C1Paths);
        RunGitText("rev-parse", "HEAD^").ShouldBe(candidate);
        RunGitPaths("diff", "--name-only", "-z", candidate, "HEAD", "--").ShouldBe([AuthorityPath]);
        RunGitPaths("diff", "--name-only", "-z", BaselineCommit, "HEAD", "--").ShouldBe(CombinedPaths);
        ChangedGitlinks(BaselineCommit, "HEAD").ShouldBeEmpty();

        JsonArray candidateFiles = RequiredArray(authority["candidateFiles"]);
        candidateFiles.Count.ShouldBe(C1Paths.Length);
        foreach (JsonObject row in candidateFiles.Select(RequiredObject))
        {
            string path = RequiredString(row, "path");
            string mode = ReadTreeMode(candidate, path);
            mode.ShouldBe("100644", path);
            RequiredString(row, "mode").ShouldBe(mode, path);
            RequiredString(row, "sha256").ShouldBe(Sha256(ReadGitBlob(candidate, path)), path);
        }

        foreach ((string path, string digest) in ImmutableAuthorityDigests)
        {
            Sha256(File.ReadAllBytes(RepositoryPath(path))).ShouldBe(digest, path);
        }

        string manifest = Encoding.UTF8.GetString(ReadGitBlob(candidate, "pyproject.toml"));
        manifest.ShouldContain("\"jsonschema==4.26.0\"");
        manifest.ShouldContain("\"pytest==9.1.1\"");
        manifest.ShouldNotContain("jsonschema==4.25.0");
        manifest.ShouldNotContain("pytest==8.4.1");
        string lockFile = Encoding.UTF8.GetString(ReadGitBlob(candidate, "uv.lock"));
        lockFile.ShouldContain(JsonschemaSdistSha256);
        lockFile.ShouldContain(JsonschemaWheelSha256);
        lockFile.ShouldContain(PytestSdistSha256);
        lockFile.ShouldContain(PytestWheelSha256);
        Regex.Matches(lockFile, "(?m)^name = ").Count.ShouldBe(13);

        string ir0 = File.ReadAllText(RepositoryPath(Ir0Path));
        ir0.ShouldContain("\nresult: READY\n");
        ir0.ShouldContain("\neffective_hold: ACTIVE\n");
    }

    /// <summary>
    /// Proves every named closed-field mutation is rejected without changing the committed authority.
    /// </summary>
    [Theory]
    [InlineData("version")]
    [InlineData("digest")]
    [InlineData("scope")]
    [InlineData("predecessor")]
    [InlineData("anti-vacuity")]
    public void V15AuthorityNamedFaultShouldBeRejected(string fault)
    {
        string authorityBefore = Sha256(File.ReadAllBytes(RepositoryPath(AuthorityPath)));
        JsonObject mutation = CloneAuthority();
        switch (fault)
        {
            case "version":
                RequiredObject(RequiredArray(RequiredObject(mutation["environment"])["packages"])[0])["version"] = "4.25.0";
                break;
            case "digest":
                RequiredObject(RequiredArray(mutation["candidateFiles"])[0])["sha256"] = new string('0', 64);
                break;
            case "scope":
                RequiredArray(RequiredObject(mutation["publication"])["combinedPaths"]).Add("unexpected.txt");
                break;
            case "predecessor":
                RequiredObject(mutation["predecessor"])["bundleDigest"] = new string('0', 64);
                break;
            case "anti-vacuity":
                RequiredArray(mutation["assertionLedger"]).Clear();
                break;
            default:
                throw new InvalidOperationException($"Unknown V15 fault '{fault}'.");
        }

        Should.Throw<InvalidDataException>(() => ValidateClosedAuthority(mutation));
        Sha256(File.ReadAllBytes(RepositoryPath(AuthorityPath))).ShouldBe(authorityBefore);
    }

    private static void ValidateClosedAuthority(JsonObject authority)
    {
        Require(RequiredString(authority, "schemaVersion") == "hexalith.conversations.v15-planning-tooling-environment-authority.v1", "schemaVersion");
        Require(RequiredString(authority, "authorityId") == "V15-PLANNING-TOOLING-ENVIRONMENT", "authorityId");
        Require(RequiredString(authority, "baselineCommit") == BaselineCommit, "baselineCommit");
        Require(Sha1Regex().IsMatch(RequiredString(authority, "candidateCommit")), "candidateCommit");

        JsonObject publication = RequiredObject(authority["publication"]);
        Require(ReadStrings(publication, "c1Paths").SequenceEqual(C1Paths, StringComparer.Ordinal), "c1Paths");
        Require(RequiredString(publication, "c2Path") == AuthorityPath, "c2Path");
        Require(ReadStrings(publication, "combinedPaths").SequenceEqual(CombinedPaths, StringComparer.Ordinal), "combinedPaths");
        Require(RequiredArray(publication["changedGitlinks"]).Count == 0, "changedGitlinks");

        JsonArray candidateFiles = RequiredArray(authority["candidateFiles"]);
        Require(candidateFiles.Count == C1Paths.Length, "candidateFiles count");
        string[] candidateFilePaths = candidateFiles.Select(RequiredObject).Select(row => RequiredString(row, "path")).ToArray();
        Require(candidateFilePaths.SequenceEqual(C1Paths, StringComparer.Ordinal), "candidateFiles paths");
        string candidate = RequiredString(authority, "candidateCommit");
        foreach (JsonObject row in candidateFiles.Select(RequiredObject))
        {
            string path = RequiredString(row, "path");
            Require(RequiredString(row, "mode") == "100644" && ReadTreeMode(candidate, path) == "100644", "candidate file mode");
            string digest = RequiredString(row, "sha256");
            Require(Sha256Regex().IsMatch(digest) && digest == Sha256(ReadGitBlob(candidate, path)), "candidate file sha256");
        }

        JsonObject environment = RequiredObject(authority["environment"]);
        Require(RequiredInt(environment, "packageCount") == 13, "packageCount");
        Require(ReadStrings(environment, "packageNames").SequenceEqual(PackageNames, StringComparer.Ordinal), "packageNames");
        JsonArray packages = RequiredArray(environment["packages"]);
        Require(packages.Count == 2, "packages");
        ValidatePackage(RequiredObject(packages[0]), "jsonschema", "4.26.0", JsonschemaSdistSha256, JsonschemaWheelSha256);
        ValidatePackage(RequiredObject(packages[1]), "pytest", "9.1.1", PytestSdistSha256, PytestWheelSha256);

        JsonObject predecessor = RequiredObject(authority["predecessor"]);
        Require(RequiredString(predecessor, "path") == V9Path, "predecessor path");
        Require(RequiredString(predecessor, "fileSha256") == V9Sha256, "predecessor file digest");
        Require(RequiredString(predecessor, "planningCandidate") == V9Candidate, "predecessor candidate");
        Require(RequiredString(predecessor, "bundleDigest") == V9BundleDigest, "predecessor bundle digest");

        JsonArray immutable = RequiredArray(authority["immutableAuthorities"]);
        Require(immutable.Count == ImmutableAuthorityDigests.Count, "immutable authority count");
        string[] immutablePaths = immutable.Select(RequiredObject).Select(row => RequiredString(row, "path")).ToArray();
        Require(immutablePaths.SequenceEqual(ImmutableAuthorityDigests.Keys, StringComparer.Ordinal), "immutable authority paths");
        foreach (JsonObject row in immutable.Select(RequiredObject))
        {
            string path = RequiredString(row, "path");
            Require(ImmutableAuthorityDigests.TryGetValue(path, out string? digest), "immutable authority path");
            Require(RequiredString(row, "sha256") == digest, "immutable authority digest");
            Require(RequiredString(row, "mode") == "100644", "immutable authority mode");
            Require(Sha256(File.ReadAllBytes(RepositoryPath(path))) == digest, "immutable authority live digest");
        }

        JsonObject ir0 = RequiredObject(authority["ir0Assessment"]);
        Require(RequiredString(ir0, "path") == Ir0Path, "IR-0 path");
        Require(RequiredString(ir0, "sha256") == Ir0Sha256, "IR-0 digest");
        Require(RequiredString(ir0, "result") == "READY", "IR-0 result");
        Require(RequiredString(ir0, "effectiveHold") == "ACTIVE", "IR-0 hold");
        Require(RequiredBool(ir0, "preserved"), "IR-0 preserved");

        JsonObject effect = RequiredObject(authority["authorityEffect"]);
        Require(RequiredString(effect, "implementationHold") == "ACTIVE", "implementation hold");
        Require(!RequiredBool(effect, "ir0AuthorizationChanged"), "IR-0 authorization change");
        Require(!RequiredBool(effect, "successorActivated"), "successor activation");
        Require(!RequiredBool(effect, "releaseAuthorized"), "release authorization");
        Require(!RequiredBool(effect, "pushAuthorized"), "push authorization");

        JsonObject semantics = RequiredObject(authority["resultSemantics"]);
        Require(ReadStrings(semantics, "states").SequenceEqual(["PASS", "FAIL", "BLOCKED", "not-applicable"], StringComparer.Ordinal), "result states");
        Require(RequiredBool(semantics, "ledgerRequired"), "ledger required");
        Require(!RequiredBool(semantics, "skipsAllowed"), "skips allowed");
        Require(RequiredString(authority, "result") == "PASS", "result");
        JsonArray ledger = RequiredArray(authority["assertionLedger"]);
        Require(ledger.Count > 0, "assertion ledger");
        Require(ledger.Select(RequiredObject).All(row => RequiredString(row, "state") == "PASS"), "assertion ledger state");
    }

    private static void ValidatePackage(JsonObject package, string name, string version, string sdist, string wheel)
    {
        Require(RequiredString(package, "name") == name, $"{name} name");
        Require(RequiredString(package, "version") == version, $"{name} version");
        Require(RequiredString(package, "registry") == "https://pypi.org/simple", $"{name} registry");
        Require(RequiredString(RequiredObject(package["sdist"]), "sha256") == sdist, $"{name} sdist");
        JsonArray wheels = RequiredArray(package["wheels"]);
        Require(wheels.Count == 1, $"{name} wheel count");
        Require(RequiredString(RequiredObject(wheels[0]), "sha256") == wheel, $"{name} wheel");
    }

    private static void Require(bool condition, string subject)
    {
        if (!condition)
        {
            throw new InvalidDataException($"V15 authority validation failed: {subject}.");
        }
    }

    private static JsonObject ReadAuthority()
        => RequiredObject(JsonNode.Parse(File.ReadAllText(RepositoryPath(AuthorityPath))));

    private static JsonObject CloneAuthority() => RequiredObject(ReadAuthority().DeepClone());

    private static JsonObject RequiredObject(JsonNode? node)
        => node as JsonObject ?? throw new InvalidDataException("Expected a JSON object.");

    private static JsonArray RequiredArray(JsonNode? node)
        => node as JsonArray ?? throw new InvalidDataException("Expected a JSON array.");

    private static string RequiredString(JsonObject value, string property)
        => value[property]?.GetValue<string>() ?? throw new InvalidDataException($"Expected string '{property}'.");

    private static int RequiredInt(JsonObject value, string property)
        => value[property]?.GetValue<int>() ?? throw new InvalidDataException($"Expected integer '{property}'.");

    private static bool RequiredBool(JsonObject value, string property)
        => value[property]?.GetValue<bool>() ?? throw new InvalidDataException($"Expected Boolean '{property}'.");

    private static string[] ReadStrings(JsonObject value, string property)
        => RequiredArray(value[property]).Select(item => item?.GetValue<string>() ?? throw new InvalidDataException($"Expected string in '{property}'.")).ToArray();

    private static string ReadTreeMode(string revision, string path)
        => RunGitText("ls-tree", revision, "--", path).Split(' ', 2, StringSplitOptions.None)[0];

    private static string[] RunGitPaths(params string[] arguments)
        => SplitNul(RunGitBytes(arguments))
            .Select(Encoding.UTF8.GetString)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static byte[] ReadGitBlob(string revision, string path) => RunGitBytes("show", $"{revision}:{path}");

    private static string RunGitText(params string[] arguments)
        => Encoding.UTF8.GetString(RunGitBytes(arguments)).TrimEnd('\r', '\n');

    private static byte[] RunGitBytes(params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = FindRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start Git.");
        using MemoryStream output = new();
        process.StandardOutput.BaseStream.CopyTo(output);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error);
        }

        return output.ToArray();
    }

    private static string[] ChangedGitlinks(string baseline, string candidate)
    {
        string[] records = SplitNul(RunGitBytes("diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--"))
            .Select(Encoding.UTF8.GetString)
            .ToArray();
        List<string> paths = [];
        for (int index = 0; index + 1 < records.Length; index += 2)
        {
            string[] fields = records[index].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length >= 5 && (fields[0] == ":160000" || fields[1] == "160000"))
            {
                paths.Add(records[index + 1]);
            }
        }

        return [.. paths.Order(StringComparer.Ordinal)];
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

    private static string RepositoryPath(string relativePath) => Path.Combine(FindRepositoryRoot(), relativePath);

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

    [GeneratedRegex("^[0-9a-f]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha1Regex();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
