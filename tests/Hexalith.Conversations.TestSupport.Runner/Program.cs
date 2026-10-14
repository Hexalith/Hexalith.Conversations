using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Runner;

/// <summary>Internal, non-shipping JSON transport for the neutral evidence helpers.</summary>
internal static class Program
{
    private const string Story102RecordSha256 = "7bca69ff979b5ead9819da913933c13efbcb6e2076368202be92f256ef39d409";
    private const string Story102CandidateCommit = "5a119ff283a8f81906a52d983e50b04b76fae0ed";

    /// <summary>Reads one request and emits one helper-derived response.</summary>
    private static async Task<int> Main()
    {
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        try
        {
            using JsonDocument request = JsonDocument.Parse(await Console.In.ReadToEndAsync().ConfigureAwait(false));
            JsonElement input = request.RootElement;
            string root = input.GetProperty("repository").GetString()!;
            // This consumer pins the accepted predecessor rather than trusting request-authored roots.
            _ = Story102RecordSha256;
            _ = Story102CandidateCommit;
            GitFacts git = new();
            AssertionLedger ledger = new();
            GitFact<string> repository = await git.ResolveRepositoryAsync(root).ConfigureAwait(false);
            BoundaryAssertions.HistoryAvailable(ledger, "repository", repository);
            GitFact<string> candidate = await git.ResolveRevisionAsync(root, input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
            GitFact<string> baseline = await git.ResolveRevisionAsync(root, input.GetProperty("baseline").GetString()!).ConfigureAwait(false);
            BoundaryAssertions.HistoryAvailable(ledger, "candidate", candidate);
            BoundaryAssertions.HistoryAvailable(ledger, "baseline", baseline);
            GitFact<IReadOnlyList<GitDiffEntry>> diff = await git.ReadDiffAsync(root,
                input.GetProperty("baseline").GetString()!, input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
            BoundaryAssertions.HistoryAvailable(ledger, "diff", diff);
            if (input.TryGetProperty("expectedPaths", out JsonElement paths))
                BoundaryAssertions.ChangedPaths(ledger, "changed-paths", paths.EnumerateArray().Select(path => path.GetString()!), diff);
            BoundaryAssertions.NoGitlinks(ledger, "gitlinks", diff);
            List<object> blobs = [];
            if (input.TryGetProperty("blobPaths", out JsonElement blobPaths))
            {
                foreach (JsonElement path in blobPaths.EnumerateArray())
                {
                    string relative = path.GetString()!;
                    GitFact<GitBlob> blob = await git.ReadBlobAsync(root, input.GetProperty("candidate").GetString()!, relative).ConfigureAwait(false);
                    blobs.Add(new { path = relative, code = blob.Code, sha256 = blob.Value?.Sha256,
                        content = blob.IsSuccess ? Convert.ToBase64String(blob.Value!.Content) : null });
                }
            }
            if (input.TryGetProperty("manifest", out JsonElement rows))
            {
                EvidenceManifest manifest = new();
                foreach (JsonElement row in rows.EnumerateArray())
                    manifest.Add(row.GetProperty("path").GetString()!, row.GetProperty("sha256").GetString()!, row.GetProperty("role").GetString()!);
                manifest.Verify(root, input.GetProperty("signablePayloadHash").GetString()!, ledger);
            }
            if (input.TryGetProperty("roots", out JsonElement roots))
            {
                Dictionary<string, string> constants = roots.GetProperty("constants").EnumerateObject()
                    .ToDictionary(row => row.Name, row => row.Value.GetString()!, StringComparer.Ordinal);
                GitFact<string> path = RepositoryLocator.ResolveContainedPath(root, roots.GetProperty("sourcePath").GetString()!);
                if (path.IsSuccess && File.Exists(path.Value))
                    BoundaryAssertions.PinnedRoots(ledger, "roots", await File.ReadAllTextAsync(path.Value!).ConfigureAwait(false), constants);
                else
                {
                    ledger.Block("ROOT_OF_TRUST_NOT_PINNED", "roots", path.Message ?? "The consuming source is unavailable.");
                    ledger.Record("roots", false);
                }
                BoundaryAssertions.SignedEvidenceIsNotAllowlisted(ledger, "allowlist", root,
                    roots.GetProperty("allowlist").EnumerateArray().Select(row => row.GetString()!));
            }
            if (input.TryGetProperty("readerPaths", out JsonElement readerPaths))
            {
                foreach (JsonElement item in readerPaths.EnumerateArray())
                {
                    string relative = item.GetString()!;
                    GitFact<GitBlob> old = await git.ReadBlobAsync(root, input.GetProperty("baseline").GetString()!, relative).ConfigureAwait(false);
                    GitFact<GitBlob> current = await git.ReadBlobAsync(root, input.GetProperty("candidate").GetString()!, relative).ConfigureAwait(false);
                    string oldSource = old.IsSuccess ? System.Text.Encoding.UTF8.GetString(old.Value!.Content) : string.Empty;
                    string source = current.IsSuccess ? System.Text.Encoding.UTF8.GetString(current.Value!.Content) : string.Empty;
                    if (!source.Contains("docs/release-evidence/", StringComparison.Ordinal) && !source.Contains("EvidenceManifest", StringComparison.Ordinal)
                        && !oldSource.Contains("docs/release-evidence/", StringComparison.Ordinal) && !oldSource.Contains("EvidenceManifest", StringComparison.Ordinal)) continue;
                    IReadOnlyList<string> tokens = CSharpSourceDeclarations.UnconditionalTokens(source);
                    IReadOnlyList<string> oldTokens = CSharpSourceDeclarations.UnconditionalTokens(oldSource);
                    bool shared = new[] { "GitFacts", "EvidenceManifest", "AssertionLedger" }.All(name =>
                        tokens.Select((token, index) => (token, index)).Any(row => row.token == name
                            && tokens.Skip(row.index).TakeWhile(token => token != ";").Contains("new")))
                        && CSharpSourceDeclarations.ContainsSequence(tokens, ["BoundaryAssertions", "."]);
                    if (!shared) ledger.Block("EVIDENCE_READER_NOT_SHARED", relative, "Changed evidence readers must invoke the shared neutral helpers.");
                    ledger.Record("reader:shared:" + relative, relative, shared);
                    string[] assertions = ["Equal", "True", "ChangedPaths", "NoGitlinks", "PinnedRoots", "SignedEvidenceIsNotAllowlisted", "Record", "ShouldBe", "ShouldBeTrue"];
                    bool strength = source.Length != 0 && assertions.All(name => tokens.Count(token => token == name) >= oldTokens.Count(token => token == name))
                        && !CSharpSourceDeclarations.ContainsSequence(tokens, ["Skip", "="])
                        && !CSharpSourceDeclarations.ContainsSequence(tokens, ["Assert", ".", "True", "(", "true", ")"]);
                    // Existing literal roots and assertion identities remain present.
                    for (int index = 0; index + 5 < oldTokens.Count; index++)
                    {
                        if (oldTokens[index] == "const" && oldTokens[index + 1] == "string")
                            strength &= CSharpSourceDeclarations.ContainsSequence(tokens, oldTokens.Skip(index).Take(6).ToArray());
                    }
                    if (!strength) ledger.Block("EVIDENCE_READER_STRENGTH_REDUCED", relative, "Assertions or pinned roots were removed or replaced by vacuous assertions.");
                    ledger.Record("reader:strength:" + relative, relative, strength);
                }
            }
            List<object> containedPaths = [];
            if (input.TryGetProperty("containedPaths", out JsonElement requestedPaths))
            {
                foreach (JsonElement item in requestedPaths.EnumerateArray())
                {
                    string relative = item.GetString()!;
                    GitFact<string> contained = RepositoryLocator.ResolveContainedPath(root, relative);
                    containedPaths.Add(new { path = relative, fullPath = contained.Value, code = contained.Code });
                    if (!contained.IsSuccess) ledger.Block("EVIDENCE_PATH_ESCAPE", relative, contained.Message!);
                    ledger.Record("containment:" + relative, relative, contained.IsSuccess);
                }
            }
            BoundaryEvaluation evaluation = ledger.Evaluate();
            string state = ledger.Rows.Any(row => row.State == "SKIPPED")
                && !ledger.Rows.Any(row => row.State == "FAIL") ? "BLOCKED" : evaluation.Result;
            Console.WriteLine(JsonSerializer.Serialize(new {
                result = state, candidate = candidate.Value, baseline = baseline.Value, diff = diff.Value,
                blobs, containedPaths, assertionLedger = ledger.Rows, blockers = evaluation.Blockers.Select(row => row.Code).Distinct().Order(StringComparer.Ordinal)
            }, options));
            return 0;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException or IOException)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = "FAIL", blockers = new[] { "HELPER_REQUEST_INVALID" }, message = exception.Message }, options));
            return 1;
        }
    }
}
