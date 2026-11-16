// <copyright file="SmC2MeasurementMethodValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Executes the predeclared SM-C2 method independently of Python, using files only.</summary>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class SmC2MeasurementMethodValidationTest
{
    private const string MethodPath = "docs/release-evidence/sm-c2-measurement-method-v2.json";
    private const string PayloadSha256 = "7d6b2d7a6049a80eda5a578ddf097fa5a0eb0a910327dc9227791f2080174bff";
    private static readonly string[] _rows = ["HP-APPEND", "HP-CREATE", "HP-LIST", "HP-OPEN"];
    private static readonly int[] _repetitions = [400, 1200, 200, 200];
    private static readonly string[] _seeds = ["0584078bda73ada5", "fd015e1f7c52f628", "5bb56fc70826a8e7", "00debde9af1485b1"];
    private static readonly decimal[] _halfWidths = [8.276225m, 0.255475m, 133.2389m, 48.67435m];

    /// <summary>Pins the frozen envelope and reproduces every exact bootstrap self-check without making a verdict.</summary>
    [Fact]
    public void MethodShouldBePredeclaredAndDeterministic()
    {
        // The native runner changes its working directory; private-output runs supply the discovery origin.
        string root = RepositoryLocator.FindRepositoryRoot(Environment.GetEnvironmentVariable("SMC2_METHOD_REPOSITORY_ROOT"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, MethodPath)));
        JsonElement method = document.RootElement;
        method.EnumerateObject().Count().ShouldBe(14);
        method.GetProperty("schemaVersion").GetString().ShouldBe("hexalith.conversations.sm-c2-measurement-method.v2");
        method.GetProperty("digests").GetProperty("signablePayloadSha256").GetString().ShouldBe(PayloadSha256);
        using MemoryStream canonical = new();
        using (Utf8JsonWriter writer = new(canonical, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            WriteCanonical(writer, method, root: true);
        }

        Convert.ToHexStringLower(SHA256.HashData(canonical.ToArray())).ShouldBe(PayloadSha256);
        JsonElement[] rows = [.. method.GetProperty("rows").EnumerateArray()];
        rows.Select(static row => row.GetProperty("hotPathId").GetString()).ShouldBe(_rows);
        foreach (JsonElement row in rows)
        {
            row.GetProperty("threshold").GetProperty("factor").GetString().ShouldBe("1.05");
            row.GetProperty("threshold").GetProperty("requiresUsableSignal").GetBoolean().ShouldBeTrue();
            row.GetProperty("threshold").GetProperty("rule").GetString().ShouldBe("postP95 <= 1.05 * baselineP95");
        }

        JsonElement repetitions = method.GetProperty("repetitions");
        repetitions.GetProperty("sessionsPerSide").GetInt32().ShouldBe(4);
        repetitions.GetProperty("operationsPerSample").GetInt32().ShouldBe(2000);
        repetitions.GetProperty("sessionOrder").EnumerateArray().Select(static value => value.GetString())
            .ShouldBe(["B", "C", "B", "C", "B", "C", "B", "C"]);
        repetitions.GetProperty("adaptive").GetBoolean().ShouldBeFalse();
        repetitions.GetProperty("dropSession").GetBoolean().ShouldBeFalse();
        repetitions.GetProperty("rerunSession").GetBoolean().ShouldBeFalse();
        repetitions.GetProperty("failedSession").GetString().ShouldBe("void-entire-pair");
        method.GetProperty("warmup").GetProperty("repetitionsPerSession").GetInt32().ShouldBe(5);
        method.GetProperty("warmup").GetProperty("includedInRawSamples").GetBoolean().ShouldBeFalse();
        JsonElement retention = method.GetProperty("retention");
        retention.GetProperty("retainAllRawSamples").GetBoolean().ShouldBeTrue();
        retention.GetProperty("outlierRule").GetString().ShouldBe("none");
        retention.GetProperty("trimming").GetString().ShouldBe("none");
        foreach (string field in new[] { "sampleDeletion", "adaptiveStopping", "rerunSelection" })
        {
            retention.GetProperty(field).GetBoolean().ShouldBeFalse();
        }

        method.GetProperty("classification").GetProperty("measured").GetString().ShouldBe("warm in-process production path");
        method.GetProperty("classification").GetProperty("coldSubstitution").GetBoolean().ShouldBeFalse();
        JsonElement environment = method.GetProperty("environment");
        environment.GetProperty("sdk").GetString().ShouldBe("11.0.100");
        environment.GetProperty("runtime").GetString().ShouldBe("11.0.0");
        environment.GetProperty("configuration").GetString().ShouldBe("Release");
        environment.GetProperty("concurrency").GetInt32().ShouldBe(1);
        environment.GetProperty("host").GetProperty("sameHostForBothSides").GetBoolean().ShouldBeTrue();
        environment.GetProperty("host").GetProperty("recordBeforeObservation").EnumerateArray().Select(static value => value.GetString())
            .ShouldBe(["kernel", "cpuModel", "logicalProcessors"]);
        environment.GetProperty("runnerFlags").EnumerateArray().Select(static value => value.GetString())
            .ShouldBe(["-parallel", "none", "-maxThreads", "1"]);
        environment.GetProperty("forbiddenOverridePrefixes").EnumerateArray().Select(static value => value.GetString())
            .ShouldBe(["DOTNET_", "COMPlus_"]);
        environment.GetProperty("forbiddenConcurrentProcesses").EnumerateArray().Select(static value => value.GetString())
            .ShouldBe(["dotnet", "Aspire", "daprd"]);
        environment.GetProperty("captureBeforeObservation").GetBoolean().ShouldBeTrue();
        environment.GetProperty("driftVoidsPair").GetBoolean().ShouldBeTrue();
        method.GetProperty("comparability").GetProperty("envelopeRevision").GetString().ShouldBe("sm-c2-envelope-v2");
        method.GetProperty("comparability").GetProperty("remeasureBothSidesAsPair").GetBoolean().ShouldBeTrue();
        method.GetProperty("comparability").GetProperty("baselineSourceCommit").GetString()
            .ShouldBe("29def441408becfbbbdc5c59b9af14a7717cb21f");
        method.GetProperty("comparability").GetProperty("baselineEventStoreCommit").GetString()
            .ShouldBe("b2d3402552fbadf529c220fcc739da9d06d285fe");
        method.GetProperty("comparability").GetProperty("fixtureBasisSha256").GetString()
            .ShouldBe("f02c4b5d103c591656d4d78181269ef3981e9af6f757200632d13a4038697eff");
        method.GetProperty("percentile").GetProperty("method").GetString().ShouldBe("nearest-rank");
        method.GetProperty("percentile").GetProperty("level").GetString().ShouldBe("0.95");
        method.GetProperty("percentile").GetProperty("interpolation").GetBoolean().ShouldBeFalse();
        JsonElement signal = method.GetProperty("signalQuality");
        signal.GetProperty("method").GetString().ShouldBe("percentile-bootstrap");
        signal.GetProperty("resamples").GetInt32().ShouldBe(10000);
        signal.GetProperty("prng").GetString().ShouldBe("SplitMix64");
        signal.GetProperty("lowerLevel").GetString().ShouldBe("0.025");
        signal.GetProperty("upperLevel").GetString().ShouldBe("0.975");
        signal.GetProperty("maximumHalfWidthFactor").GetString().ShouldBe("0.05");
        signal.GetProperty("usableRule").GetString().ShouldBe("halfWidth <= 0.05 * baselineP95");
        JsonElement checks = method.GetProperty("selfChecks");
        checks.GetProperty("purpose").GetString().ShouldBe("algorithm conformance, not a verdict");
        using JsonDocument baseline = ReadBoundSamples(root, checks.GetProperty("baseline"));
        using JsonDocument candidate = ReadBoundSamples(root, checks.GetProperty("candidate"));
        JsonElement[] vectors = [.. checks.GetProperty("vectors").EnumerateArray()];
        vectors.Select(static row => row.GetProperty("hotPathId").GetString()).ShouldBe(_rows);
        for (int i = 0; i < _rows.Length; i++)
        {
            string row = _rows[i];
            repetitions.GetProperty("perSide").GetProperty(row).GetInt32().ShouldBe(_repetitions[i]);
            repetitions.GetProperty("perSession").GetProperty(row).GetInt32().ShouldBe(_repetitions[i] / 4);
            decimal[] baseSamples = Samples(baseline.RootElement, row);
            decimal[] candidateSamples = Samples(candidate.RootElement, row);
            JsonElement vector = vectors[i];
            vector.GetProperty("baselineCount").GetInt32().ShouldBe(baseSamples.Length);
            vector.GetProperty("candidateCount").GetInt32().ShouldBe(candidateSamples.Length);
            baseSamples.Length.ShouldBe(30);
            candidateSamples.Length.ShouldBe(30);
            Rank(baseSamples, 95, 100).ShouldBe(Number(vector, "baselineP95Microseconds"));
            Rank(candidateSamples, 95, 100).ShouldBe(Number(vector, "candidateP95Microseconds"));
            byte[] seed = SHA256.HashData(Encoding.UTF8.GetBytes($"sm-c2-measurement-method-v2|{row}"))[..8];
            Convert.ToHexStringLower(seed).ShouldBe(_seeds[i]);
            vector.GetProperty("seedHex").GetString().ShouldBe(_seeds[i]);
            ulong state = BinaryPrimitives.ReadUInt64BigEndian(seed);
            decimal[] differences = new decimal[10000];
            for (int b = 0; b < differences.Length; b++)
            {
                decimal[] sampledBase = Draw(baseSamples, ref state);
                decimal[] sampledCandidate = Draw(candidateSamples, ref state);
                differences[b] = Rank(sampledCandidate, 95, 100) - Rank(sampledBase, 95, 100);
            }

            decimal lo = Rank(differences, 25, 1000);
            decimal hi = Rank(differences, 975, 1000);
            lo.ShouldBe(Number(vector, "lowerDifferenceMicroseconds"));
            hi.ShouldBe(Number(vector, "upperDifferenceMicroseconds"));
            ((hi - lo) / 2).ShouldBe(Number(vector, "halfWidthMicroseconds"));
            ((hi - lo) / 2).ShouldBe(_halfWidths[i]);
        }

        // Small probes distinguish nearest-rank from interpolation and exercise exact rank boundaries.
        Rank([1, 2, 3, 4], 95, 100).ShouldBe(4);
        Rank([4, 1, 3, 2], 25, 100).ShouldBe(1);
        Rank([1, 2, 3, 4], 50, 100).ShouldBe(2);
    }

    /// <summary>Reads immutable raw samples and independently recomputes their bound file digest.</summary>
    private static JsonDocument ReadBoundSamples(string root, JsonElement binding)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(root, binding.GetProperty("path").GetString()!));
        Convert.ToHexStringLower(SHA256.HashData(bytes)).ShouldBe(binding.GetProperty("sha256").GetString());
        return JsonDocument.Parse(bytes);
    }

    /// <summary>Selects every retained raw observation for the named row, without deleting or trimming.</summary>
    private static decimal[] Samples(JsonElement evidence, string row) =>
        [.. evidence.GetProperty("rows").EnumerateArray().Single(value => value.GetProperty("hotPathId").GetString() == row)
            .GetProperty("rawMicrosecondsPerOperation").EnumerateArray().Select(static value => value.GetDecimal())];

    /// <summary>Reads an exact decimal algorithm result, avoiding binary floating-point rounding.</summary>
    private static decimal Number(JsonElement vector, string field) => decimal.Parse(vector.GetProperty(field).GetString()!, CultureInfo.InvariantCulture);

    /// <summary>Computes the non-interpolated nearest rank for a rational percentile level.</summary>
    private static decimal Rank(decimal[] values, int numerator, int denominator) =>
        values.Order().ElementAt(((numerator * values.Length) + denominator - 1) / denominator - 1);

    /// <summary>Draws exactly n independent indices, preserving PRNG state between both sides and resamples.</summary>
    private static decimal[] Draw(decimal[] samples, ref ulong state)
    {
        decimal[] result = new decimal[samples.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = samples[Next(ref state) % (ulong)samples.Length];
        }

        return result;
    }

    /// <summary>Advances SplitMix64 with explicitly wrapping unsigned 64-bit arithmetic.</summary>
    private static ulong Next(ref ulong state)
    {
        unchecked
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong value = state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }

    /// <summary>Recomputes canonical signable JSON, sorting every object and excluding approval and digests.</summary>
    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value, bool root = false)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (JsonProperty property in value.EnumerateObject().OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                if (root && property.Name is "approval" or "digests")
                {
                    continue;
                }

                writer.WritePropertyName(property.Name);
                WriteCanonical(writer, property.Value);
            }

            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (JsonElement item in value.EnumerateArray())
            {
                WriteCanonical(writer, item);
            }

            writer.WriteEndArray();
        }
        else
        {
            value.WriteTo(writer);
        }
    }
}
