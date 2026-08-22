// <copyright file="PlanningToolingLifecycleAuthorityV16ValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Independently validates the durable V16 planning-tooling lifecycle authority.
/// </summary>
public sealed class PlanningToolingLifecycleAuthorityV16ValidationTest
{
    private const string AuthorityPath = "_bmad-output/planning-artifacts/v16-planning-tooling-lifecycle-authority-v1.json";
    private const string BaselineCommit = "08a4bdcc5a18067f8f93c777055d8097987a9da2";
    private const string Ir0Path = "_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-22-ir-0.md";
    private const string Ir0Sha256 = "862a880ca621c4f9b60328bc2f1ce353951d5ae7fcce811cffb6d050e8b122ad";
    private const string V15AuthorityPath = "_bmad-output/planning-artifacts/v15-planning-tooling-environment-authority-v1.json";
    private const string V15AuthoritySha256 = "bac4dc435bc200d2eb5b3601a794b20abe5afaa79dc51b79d4f9571a6f6a37ea";
    private const string V15BaselineCommit = "6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4";
    private const string V15CandidateCommit = "4586df9d35e1d50df401cd98cf62e4435d89007d";
    private const string V15PublicationCommit = BaselineCommit;
    private const string V9BundleDigest = "159eec0cb13d2af422c46e9490e51432495ea61c0d034832a502c9598ff4f055";
    private const string V9Candidate = "1e9a61126d3b7a55b514b7c7c8942d5af03355e5";
    private const string V9Path = "_bmad-output/planning-artifacts/v9-authority-bundle-v1.json";
    private const string V9Sha256 = "8af7ba3bdbc5efe80c9534463089013d8408b5aa0f291f3c00b3dcd36f953ef3";
    private const int ProcessTimeoutMilliseconds = 30_000;

    private static readonly string[] C1Paths =
    [
        ".github/workflows/planning-authority-preflight.yml",
        "_bmad-output/implementation-artifacts/spec-v15-update-planning-tooling-packages.md",
        "_bmad-output/implementation-artifacts/spec-v16-correct-planning-tooling-lifecycle-authority.md",
        "_bmad/schemas/v16-planning-tooling-lifecycle-authority-v1.schema.json",
        "_bmad/scripts/publish_v15_planning_tooling_environment.py",
        "_bmad/scripts/publish_v16_planning_tooling_lifecycle.py",
        "_bmad/scripts/tests/test_publish_v15_planning_tooling_environment.py",
        "_bmad/scripts/tests/test_publish_v16_planning_tooling_lifecycle.py",
        "_bmad/scripts/tests/test_verify_evidence_boundary.py",
        "_bmad/scripts/verify_evidence_boundary.py",
        "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingEnvironmentAuthorityV15ValidationTest.cs",
        "tests/Hexalith.Conversations.Conformance.Tests/PlanningToolingLifecycleAuthorityV16ValidationTest.cs",
    ];

    private static readonly string[] CombinedPaths = C1Paths.Append(AuthorityPath).Order(StringComparer.Ordinal).ToArray();

    private static readonly string[] V15C1Paths =
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

    private static readonly string[] V15CombinedPaths = V15C1Paths.Append(V15AuthorityPath).Order(StringComparer.Ordinal).ToArray();

    private static readonly string[] PackageNames =
    [
        "attrs", "colorama", "hexalith-conversations-planning", "iniconfig", "jsonschema",
        "jsonschema-specifications", "packaging", "pluggy", "pygments", "pytest", "referencing",
        "rpds-py", "typing-extensions",
    ];

    private static readonly IReadOnlyDictionary<string, string> ImmutableAuthorityDigests =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [V9Path] = V9Sha256,
            ["_bmad-output/planning-artifacts/v12-pre-ir0-remediation-authority-v1.json"] = "c082cde6923e9831eea768be6c547ca1ab87ed91244185b505bdf3ae1c116dcc",
            ["_bmad-output/planning-artifacts/v13-current-proof-authority-v1.json"] = "f2f02115502d42d6e74f1e34351eeda1e1d778b35e2dee485821ac53e448138f",
            ["_bmad-output/planning-artifacts/v14-current-candidate-authority-v1.json"] = "e96c34dfdf7f2cd8619b75abc42aad40ab0d8606d3ab798bf2b9b58fac83da7f",
            [V15AuthorityPath] = V15AuthoritySha256,
            [Ir0Path] = Ir0Sha256,
        };

    /// <summary>
    /// Recomputes V16 C1/C2 and all inherited authority from committed objects at HEAD.
    /// </summary>
    [Fact]
    public void V16AuthorityShouldBindExactLifecycleAtAnyDescendant()
    {
        string head = RunGitText("rev-parse", "HEAD");
        string publication = LocatePublication(head);
        RequireAncestor(publication, head);
        byte[] authorityBytes = ReadGitBlob(publication, AuthorityPath);
        ReadGitBlob(head, AuthorityPath).ShouldBe(authorityBytes);
        JsonObject authority = RequiredObject(JsonNode.Parse(authorityBytes));
        string candidate = ValidateClosedAuthority(authority);

        RequireSingleParent(candidate, BaselineCommit);
        RequireSingleParent(publication, candidate);
        RunGitPaths("diff", "--name-only", "-z", BaselineCommit, candidate, "--").ShouldBe(C1Paths);
        RunGitPaths("diff", "--name-only", "-z", candidate, publication, "--").ShouldBe([AuthorityPath]);
        RunGitPaths("diff", "--name-only", "-z", BaselineCommit, publication, "--").ShouldBe(CombinedPaths);
        ChangedGitlinks(BaselineCommit, publication).ShouldBeEmpty();

        RequireSingleParent(V15CandidateCommit, V15BaselineCommit);
        RequireSingleParent(V15PublicationCommit, V15CandidateCommit);
        RunGitPaths("diff", "--name-only", "-z", V15BaselineCommit, V15CandidateCommit, "--").ShouldBe(V15C1Paths);
        RunGitPaths("diff", "--name-only", "-z", V15CandidateCommit, V15PublicationCommit, "--").ShouldBe([V15AuthorityPath]);
        ChangedGitlinks(V15BaselineCommit, V15PublicationCommit).ShouldBeEmpty();
        Sha256(ReadGitBlob(V15PublicationCommit, V15AuthorityPath)).ShouldBe(V15AuthoritySha256);

        foreach ((string path, string digest) in ImmutableAuthorityDigests)
        {
            Sha256(ReadGitBlob(head, path)).ShouldBe(digest, path);
        }
    }

    /// <summary>
    /// Proves closed properties, URLs, hashes, V15 identity, and nonempty strong ledgers are enforced.
    /// </summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData("url")]
    [InlineData("ledger")]
    [InlineData("hash")]
    [InlineData("v15")]
    public void V16AuthorityNamedClosedContractFaultShouldBeRejected(string fault)
    {
        JsonObject mutation = CloneAuthority();
        switch (fault)
        {
            case "unknown":
                mutation["unexpected"] = true;
                break;
            case "url":
                RequiredObject(RequiredObject(RequiredArray(RequiredObject(mutation["environment"])["packages"])[0])["sdist"])["url"] = "https://example.invalid";
                break;
            case "ledger":
                RequiredArray(mutation["assertionLedger"]).Clear();
                break;
            case "hash":
                RequiredObject(RequiredArray(mutation["immutableAuthorities"])[0])["sha256"] = new string('0', 64);
                break;
            case "v15":
                RequiredObject(mutation["v15Publication"])["publicationCommit"] = V15CandidateCommit;
                break;
            default:
                throw new InvalidOperationException($"Unknown V16 fault '{fault}'.");
        }

        Should.Throw<InvalidDataException>(() => ValidateClosedAuthority(mutation));
    }

    private static string ValidateClosedAuthority(JsonObject authority)
    {
        RequireProperties(authority, "schemaVersion", "authorityId", "baselineCommit", "candidateCommit", "publication", "candidateFiles", "v15Publication", "environment", "predecessor", "immutableAuthorities", "ir0Assessment", "authorityEffect", "resultSemantics", "result", "assertionLedger");
        Require(RequiredString(authority, "schemaVersion") == "hexalith.conversations.v16-planning-tooling-lifecycle-authority.v1", "schemaVersion");
        Require(RequiredString(authority, "authorityId") == "V16-PLANNING-TOOLING-LIFECYCLE", "authorityId");
        Require(RequiredString(authority, "baselineCommit") == BaselineCommit, "baselineCommit");
        string candidate = RequiredString(authority, "candidateCommit");
        Require(candidate.Length == 40 && candidate.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'), "candidateCommit");

        JsonObject publication = RequiredObject(authority["publication"]);
        RequireProperties(publication, "c1Paths", "c2Path", "combinedPaths", "changedGitlinks");
        Require(ReadStrings(publication, "c1Paths").SequenceEqual(C1Paths, StringComparer.Ordinal), "c1Paths");
        Require(RequiredString(publication, "c2Path") == AuthorityPath, "c2Path");
        Require(ReadStrings(publication, "combinedPaths").SequenceEqual(CombinedPaths, StringComparer.Ordinal), "combinedPaths");
        Require(RequiredArray(publication["changedGitlinks"]).Count == 0, "changedGitlinks");

        JsonArray candidateFiles = RequiredArray(authority["candidateFiles"]);
        Require(candidateFiles.Count == C1Paths.Length, "candidateFiles count");
        Require(candidateFiles.Select(RequiredObject).Select(row => RequiredString(row, "path")).SequenceEqual(C1Paths, StringComparer.Ordinal), "candidateFiles paths");
        foreach (JsonObject row in candidateFiles.Select(RequiredObject))
        {
            RequireProperties(row, "path", "sha256", "mode");
            string path = RequiredString(row, "path");
            Require(RequiredString(row, "mode") == "100644" && ReadTreeMode(candidate, path) == "100644", "candidate file mode");
            Require(RequiredString(row, "sha256") == Sha256(ReadGitBlob(candidate, path)), "candidate file digest");
        }

        ValidateV15Publication(RequiredObject(authority["v15Publication"]));
        ValidateEnvironment(RequiredObject(authority["environment"]));
        JsonObject predecessor = RequiredObject(authority["predecessor"]);
        RequireProperties(predecessor, "path", "fileSha256", "planningCandidate", "bundleDigest");
        Require(RequiredString(predecessor, "path") == V9Path && RequiredString(predecessor, "fileSha256") == V9Sha256, "predecessor file");
        Require(RequiredString(predecessor, "planningCandidate") == V9Candidate && RequiredString(predecessor, "bundleDigest") == V9BundleDigest, "predecessor identity");

        JsonArray immutable = RequiredArray(authority["immutableAuthorities"]);
        Require(immutable.Count == ImmutableAuthorityDigests.Count, "immutable count");
        Require(immutable.Select(RequiredObject).Select(row => RequiredString(row, "path")).SequenceEqual(ImmutableAuthorityDigests.Keys, StringComparer.Ordinal), "immutable paths");
        foreach (JsonObject row in immutable.Select(RequiredObject))
        {
            RequireProperties(row, "path", "sha256", "mode");
            string path = RequiredString(row, "path");
            Require(ImmutableAuthorityDigests.TryGetValue(path, out string? digest), "immutable path");
            Require(RequiredString(row, "sha256") == digest && RequiredString(row, "mode") == "100644", "immutable binding");
        }

        JsonObject ir0 = RequiredObject(authority["ir0Assessment"]);
        RequireProperties(ir0, "path", "sha256", "result", "effectiveHold", "preserved");
        Require(RequiredString(ir0, "path") == Ir0Path && RequiredString(ir0, "sha256") == Ir0Sha256, "IR-0 identity");
        Require(RequiredString(ir0, "result") == "READY" && RequiredString(ir0, "effectiveHold") == "ACTIVE" && RequiredBool(ir0, "preserved"), "IR-0 state");

        JsonObject effect = RequiredObject(authority["authorityEffect"]);
        RequireProperties(effect, "implementationHold", "ir0AuthorizationChanged", "successorActivated", "releaseAuthorized", "pushAuthorized");
        Require(RequiredString(effect, "implementationHold") == "ACTIVE", "implementation hold");
        Require(!RequiredBool(effect, "ir0AuthorizationChanged") && !RequiredBool(effect, "successorActivated") && !RequiredBool(effect, "releaseAuthorized") && !RequiredBool(effect, "pushAuthorized"), "authority effect");
        JsonObject semantics = RequiredObject(authority["resultSemantics"]);
        RequireProperties(semantics, "states", "ledgerRequired", "skipsAllowed");
        Require(ReadStrings(semantics, "states").SequenceEqual(["PASS", "FAIL", "BLOCKED", "not-applicable"], StringComparer.Ordinal), "result states");
        Require(RequiredBool(semantics, "ledgerRequired") && !RequiredBool(semantics, "skipsAllowed"), "result semantics");
        Require(RequiredString(authority, "result") == "PASS", "result");
        ValidateLedger(RequiredArray(authority["assertionLedger"]));
        return candidate;
    }

    private static void ValidateV15Publication(JsonObject publication)
    {
        RequireProperties(publication, "baselineCommit", "candidateCommit", "publicationCommit", "path", "sha256", "c1Paths", "combinedPaths");
        Require(RequiredString(publication, "baselineCommit") == V15BaselineCommit, "V15 baseline");
        Require(RequiredString(publication, "candidateCommit") == V15CandidateCommit, "V15 candidate");
        Require(RequiredString(publication, "publicationCommit") == V15PublicationCommit, "V15 publication");
        Require(RequiredString(publication, "path") == V15AuthorityPath && RequiredString(publication, "sha256") == V15AuthoritySha256, "V15 artifact");
        Require(ReadStrings(publication, "c1Paths").SequenceEqual(V15C1Paths, StringComparer.Ordinal), "V15 C1 paths");
        Require(ReadStrings(publication, "combinedPaths").SequenceEqual(V15CombinedPaths, StringComparer.Ordinal), "V15 combined paths");
    }

    private static void ValidateEnvironment(JsonObject environment)
    {
        RequireProperties(environment, "packageCount", "packageNames", "packages");
        Require(RequiredInt(environment, "packageCount") == 13, "packageCount");
        Require(ReadStrings(environment, "packageNames").SequenceEqual(PackageNames, StringComparer.Ordinal), "packageNames");
        JsonArray packages = RequiredArray(environment["packages"]);
        Require(packages.Count == 2, "packages");
        ValidatePackage(RequiredObject(packages[0]), "jsonschema", "4.26.0", "https://files.pythonhosted.org/packages/b3/fc/e067678238fa451312d4c62bf6e6cf5ec56375422aee02f9cb5f909b3047/jsonschema-4.26.0.tar.gz", "0c26707e2efad8aa1bfc5b7ce170f3fccc2e4918ff85989ba9ffa9facb2be326", "https://files.pythonhosted.org/packages/69/90/f63fb5873511e014207a475e2bb4e8b2e570d655b00ac19a9a0ca0a385ee/jsonschema-4.26.0-py3-none-any.whl", "d489f15263b8d200f8387e64b4c3a75f06629559fb73deb8fdfb525f2dab50ce");
        ValidatePackage(RequiredObject(packages[1]), "pytest", "9.1.1", "https://files.pythonhosted.org/packages/e4/47/b9efed96c114afcfa3c9d3fe98a76a1d14c74a9e266d397cf6eb64be5e01/pytest-9.1.1.tar.gz", "1088fbde8f2b49d95a549a195707afa7a76a3ce9bcadc26b6d71f0ffda5fe313", "https://files.pythonhosted.org/packages/24/25/1de2678b631f5a49215c6c96fff41ba892b0a34df68d6d80292b1b48aa7f/pytest-9.1.1-py3-none-any.whl", "37a86b45efb9a47a61a36449063e8e18d0cab3161329fc099eb21783169c4f0c");
    }

    private static void ValidatePackage(JsonObject package, string name, string version, string sdistUrl, string sdistHash, string wheelUrl, string wheelHash)
    {
        RequireProperties(package, "name", "version", "registry", "sdist", "wheels");
        Require(RequiredString(package, "name") == name && RequiredString(package, "version") == version && RequiredString(package, "registry") == "https://pypi.org/simple", $"{name} identity");
        JsonObject sdist = RequiredObject(package["sdist"]);
        RequireProperties(sdist, "url", "sha256");
        Require(RequiredString(sdist, "url") == sdistUrl && RequiredString(sdist, "sha256") == sdistHash, $"{name} sdist");
        JsonArray wheels = RequiredArray(package["wheels"]);
        Require(wheels.Count == 1, $"{name} wheel count");
        JsonObject wheel = RequiredObject(wheels[0]);
        RequireProperties(wheel, "url", "sha256");
        Require(RequiredString(wheel, "url") == wheelUrl && RequiredString(wheel, "sha256") == wheelHash, $"{name} wheel");
    }

    private static void ValidateLedger(JsonArray ledger)
    {
        string[] ids = ["V16-C1", "V16-C2", "V16-GITLINKS", "V16-V15", "V16-ENVIRONMENT", "V16-PREDECESSORS", "V16-LIFECYCLE"];
        string[] subjects = ["single-parent-exact-twelve-path-c1", "single-parent-authority-only-c2", "raw-mode-160000-changed-set", "immutable-original-v15-transaction", "exact-thirteen-package-environment", "immutable-v9-v15-and-ir0-identities", "active-hold-no-release-or-push-authority"];
        Require(ledger.Count == ids.Length, "assertion ledger count");
        for (int index = 0; index < ledger.Count; index++)
        {
            JsonObject row = RequiredObject(ledger[index]);
            RequireProperties(row, index == 2 ? ["id", "subject", "state", "paths"] : ["id", "subject", "state"]);
            Require(RequiredString(row, "id") == ids[index] && RequiredString(row, "subject") == subjects[index] && RequiredString(row, "state") == "PASS", "assertion ledger row");
            if (index == 2)
            {
                Require(RequiredArray(row["paths"]).Count == 0, "assertion gitlink paths");
            }
        }
    }

    private static JsonObject CloneAuthority()
    {
        string publication = LocatePublication(RunGitText("rev-parse", "HEAD"));
        return RequiredObject(JsonNode.Parse(ReadGitBlob(publication, AuthorityPath))).DeepClone().AsObject();
    }

    private static string LocatePublication(string descendant)
    {
        string[] commits = RunGitText("log", "--format=%H", "--diff-filter=A", descendant, "--", AuthorityPath).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Require(commits.Length == 1, "publication discovery");
        return commits[0];
    }

    private static void RequireSingleParent(string commit, string expected)
    {
        string[] record = RunGitText("rev-list", "--parents", "-n", "1", commit).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Require(record.Length == 2 && record[0] == commit && record[1] == expected, "single parent");
    }

    private static void RequireAncestor(string ancestor, string descendant)
    {
        (int exitCode, _, _) = RunGitRaw("merge-base", "--is-ancestor", ancestor, descendant);
        Require(exitCode == 0, "publication ancestry");
    }

    private static void RequireProperties(JsonObject value, params string[] names) => Require(value.Select(property => property.Key).Order(StringComparer.Ordinal).SequenceEqual(names.Order(StringComparer.Ordinal), StringComparer.Ordinal), "closed properties");

    private static void Require(bool condition, string subject)
    {
        if (!condition)
        {
            throw new InvalidDataException($"V16 authority validation failed: {subject}.");
        }
    }

    private static JsonObject RequiredObject(JsonNode? node) => node as JsonObject ?? throw new InvalidDataException("Expected a JSON object.");

    private static JsonArray RequiredArray(JsonNode? node) => node as JsonArray ?? throw new InvalidDataException("Expected a JSON array.");

    private static string RequiredString(JsonObject value, string property) => value[property]?.GetValue<string>() ?? throw new InvalidDataException($"Expected string '{property}'.");

    private static int RequiredInt(JsonObject value, string property) => value[property]?.GetValue<int>() ?? throw new InvalidDataException($"Expected integer '{property}'.");

    private static bool RequiredBool(JsonObject value, string property) => value[property]?.GetValue<bool>() ?? throw new InvalidDataException($"Expected Boolean '{property}'.");

    private static string[] ReadStrings(JsonObject value, string property) => RequiredArray(value[property]).Select(item => item?.GetValue<string>() ?? throw new InvalidDataException($"Expected string in '{property}'.")).ToArray();

    private static string ReadTreeMode(string revision, string path) => RunGitText("ls-tree", revision, "--", path).Split(' ', 2, StringSplitOptions.None)[0];

    private static string[] RunGitPaths(params string[] arguments) => SplitNul(RunGitBytes(arguments)).Select(Encoding.UTF8.GetString).Order(StringComparer.Ordinal).ToArray();

    private static byte[] ReadGitBlob(string revision, string path) => RunGitBytes("show", $"{revision}:{path}");

    private static string RunGitText(params string[] arguments) => Encoding.UTF8.GetString(RunGitBytes(arguments)).TrimEnd('\r', '\n');

    private static byte[] RunGitBytes(params string[] arguments)
    {
        (int exitCode, byte[] output, string error) = RunGitRaw(arguments);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(error);
        }

        return output;
    }

    private static (int ExitCode, byte[] Output, string Error) RunGitRaw(params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            WorkingDirectory = FindRepositoryRoot(),
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

    private static string[] ChangedGitlinks(string baseline, string candidate)
    {
        string[] records = SplitNul(RunGitBytes("diff", "--raw", "--no-abbrev", "--no-renames", "-z", baseline, candidate, "--")).Select(Encoding.UTF8.GetString).ToArray();
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
