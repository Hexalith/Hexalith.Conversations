using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Tests;

public sealed class EvidenceManifestTests
{
    [Fact]
    public void ManifestShouldRecomputeContainedFilesAndHashes()
    {
        using EvidenceFixture fixture = new();
        string file = fixture.Write("docs/proof.json", "{\"proof\":true}");
        EvidenceManifest manifest = new();
        manifest.Add("docs/proof.json", EvidenceFixture.Hash(file), "proof");
        AssertionLedger ledger = new();
        manifest.Verify(fixture.Root, manifest.ComputeSignablePayloadHash(), ledger).Passed.ShouldBeTrue();
        ledger.Results.Count.ShouldBe(2);
        manifest.Rows.Single().Role.ShouldBe("proof");

        byte[] original = File.ReadAllBytes(file);
        try
        {
            File.WriteAllText(file, "{\"proof\":false}");
            manifest.Verify(fixture.Root, manifest.ComputeSignablePayloadHash(), new()).Blockers
                .ShouldContain(row => row.Code == "EVIDENCE_HASH_MISMATCH");
        }
        finally { File.WriteAllBytes(file, original); }
        EvidenceFixture.Hash(file).ShouldBe(manifest.Digests["docs/proof.json"]);
        foreach (string path in new[] { "../proof.json", "/proof.json", "C:\\proof.json", "docs/../proof.json" })
        {
            EvidenceManifest invalid = new();
            invalid.Add(path, new string('a', 64), "proof");
            BoundaryEvaluation result = invalid.Verify(fixture.Root, invalid.ComputeSignablePayloadHash(), new());
            result.Passed.ShouldBeFalse();
            result.Blockers.ShouldContain(row => row.Code == "EVIDENCE_PATH_ESCAPE");
        }
        EvidenceManifest missing = new();
        missing.Add("absent.json", new string('a', 64), "proof");
        missing.Verify(fixture.Root, missing.ComputeSignablePayloadHash(), new()).Blockers
            .ShouldContain(row => row.Code == "EVIDENCE_FILE_MISSING");
    }

    [Fact]
    public void GeneratedAndBuildOutputsShouldBeRejected()
    {
        using EvidenceFixture fixture = new();
        foreach (string path in new[] { "bin/proof.json", "OBJ/proof.json", "docs/Generated/proof.json", "docs\\bIn\\proof.json" })
        {
            string file = fixture.Write(path.Replace('\\', '/'), "proof");
            EvidenceManifest manifest = new();
            manifest.Add(path, EvidenceFixture.Hash(file), "proof");
            BoundaryEvaluation result = manifest.Verify(fixture.Root, manifest.ComputeSignablePayloadHash(), new());
            result.Passed.ShouldBeFalse();
            result.Blockers.Single().Code.ShouldBe("EVIDENCE_OUTPUT_FORBIDDEN");
        }
        string allowed = fixture.Write("binary/generated-proof.json", "proof");
        EvidenceManifest clean = new();
        clean.Add("binary/generated-proof.json", EvidenceFixture.Hash(allowed), "proof");
        clean.Verify(fixture.Root, clean.ComputeSignablePayloadHash(), new()).Passed.ShouldBeTrue();
    }

    [Fact]
    public void SignablePayloadShouldBeRecomputed()
    {
        using EvidenceFixture fixture = new();
        EvidenceManifest manifest = new();
        string z = fixture.Write("z.json", "z");
        string a = fixture.Write("a.json", "a");
        manifest.Add("z.json", EvidenceFixture.Hash(z), "résultat");
        manifest.Add("a.json", EvidenceFixture.Hash(a), "proof");
        string payload = $"a.json\t{EvidenceFixture.Hash(a)}\tproof\nz.json\t{EvidenceFixture.Hash(z)}\trésultat\n";
        string canonical = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        manifest.ComputeSignablePayloadHash().ShouldBe(canonical);
        manifest.Verify(fixture.Root, canonical, new()).Passed.ShouldBeTrue();
        manifest.Verify(fixture.Root, new string('0', 64), new()).Blockers
            .ShouldContain(row => row.Code == "SIGNABLE_PAYLOAD_MISMATCH");
        EvidenceManifest reversed = new();
        reversed.Add("a.json", EvidenceFixture.Hash(a), "proof");
        reversed.Add("z.json", EvidenceFixture.Hash(z), "résultat");
        reversed.ComputeSignablePayloadHash().ShouldBe(canonical);
        reversed.ComputeSignablePayloadHash(SignablePayloadOrder.Declared)
            .ShouldNotBe(manifest.ComputeSignablePayloadHash(SignablePayloadOrder.Declared));
        File.WriteAllText(z, "changed");
        manifest.Verify(fixture.Root, canonical, new()).Blockers
            .ShouldContain(row => row.Code == "EVIDENCE_HASH_MISMATCH");

        // The v1 attestation predates canonical ordering. Its immutable signature uses declared order.
        using JsonDocument attestation = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(EvidenceFixture.FindRepositoryRoot(),
            "docs/release-evidence/success-metric-report-and-attestation-v1.json")));
        EvidenceManifest legacy = new();
        foreach (JsonElement row in attestation.RootElement.GetProperty("sourceArtifacts").EnumerateArray())
            legacy.Add(row.GetProperty("path").GetString()!, row.GetProperty("sha256").GetString()!, row.GetProperty("role").GetString()!);
        string signed = attestation.RootElement.GetProperty("attestation").GetProperty("signablePayloadHash").GetString()!;
        legacy.ComputeSignablePayloadHash(SignablePayloadOrder.Declared).ShouldBe(signed);
        legacy.ComputeSignablePayloadHash().ShouldNotBe(signed);
    }

    [Fact]
    public void SignedRowDelimitersAndMalformedDigestsShouldBeRejected()
    {
        foreach (char delimiter in new[] { '\t', '\r', '\n' })
        {
            Should.Throw<ArgumentException>(() => new EvidenceManifest().Add("proof" + delimiter, new string('a', 64), "proof"));
            Should.Throw<ArgumentException>(() => new EvidenceManifest().Add("proof", new string('a', 64), "proof" + delimiter));
        }
        Should.Throw<ArgumentException>(() => new EvidenceManifest().Add("proof", new string('A', 64), "proof"));
    }

    [Fact]
    public void SymbolicEvidencePathsShouldBeRejected()
    {
        using EvidenceFixture fixture = new();
        string file = fixture.Write("proof.txt", "proof");
        File.CreateSymbolicLink(Path.Combine(fixture.Root, "linked.txt"), file);
        Directory.CreateSymbolicLink(Path.Combine(fixture.Root, "linked-dir"), fixture.Root);
        foreach (string path in new[] { "linked.txt", "linked-dir/proof.txt" })
        {
            EvidenceManifest manifest = new();
            manifest.Add(path, EvidenceFixture.Hash(file), "proof");
            manifest.Verify(fixture.Root, manifest.ComputeSignablePayloadHash(), new()).Blockers.Single().Code
                .ShouldBe("EVIDENCE_PATH_ESCAPE");
        }
    }
}
