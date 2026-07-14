// <copyright file="PublicContractShapeSnapshotGenerationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.1 (AC2) — captures the deterministic, diffable public contract-shape snapshot of the
/// <c>Hexalith.Conversations.Contracts</c> assembly and writes it to <c>docs/release-evidence/</c>.
/// </summary>
/// <remarks>
/// This mirrors <see cref="ReleaseConformanceArtifactGenerationTest"/>: reflect the exported public surface,
/// emit a stably-ordered JSON artifact, then re-read and re-validate it in the same pass so the committed file
/// is always a faithful round-trip. Story 5.1 re-runs this generator and diffs its output byte-for-byte against
/// the committed baseline; non-determinism here would manifest as false contract drift, so ordering is enforced
/// everywhere (types sorted by namespace then name; every member collection sorted by a stable signature key).
/// </remarks>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class PublicContractShapeSnapshotGenerationTest
{
    private const string ContractsAssemblyName = "Hexalith.Conversations.Contracts";
    private const string ContractsNamespacePrefix = "Hexalith.Conversations.Contracts";

    private static readonly JsonSerializerOptions SnapshotOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Generic type names (`IReadOnlyList<T>`) are pervasive here; relaxed encoding keeps the committed
        // evidence human-readable without affecting determinism (the encoder output is itself deterministic).
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Object/record auto-generated members that obscure real contract-shape diffs. Deterministic but pure noise.
    private static readonly HashSet<string> SuppressedMethodNames =
    [
        "ToString",
        "GetHashCode",
        "Equals",
        "GetType",
    ];

    // The six release-gate behavior areas AC2 requires the snapshot to provably cover, each mapped to the
    // public Contracts namespaces/types that back its adopter-facing envelope (Dev Notes mapping).
    private static readonly ReleaseGateAreaCoverage[] ReleaseGateAreas =
    [
        new(
            "tenant-isolation",
            ["Hexalith.Conversations.Contracts.Identifiers", "Hexalith.Conversations.Contracts.Errors"],
            "TenantId plus tenant-scoped command/query fields and tenant-denial error codes."),
        new(
            "governance-audit",
            ["Hexalith.Conversations.Contracts.Governance"],
            "Governance commands/results and audit evidence envelopes."),
        new(
            "idempotency",
            ["Hexalith.Conversations.Contracts.Commands", "Hexalith.Conversations.Contracts.Results"],
            "Command metadata, command-accepted results, and idempotency error codes."),
        new(
            "redaction",
            ["Hexalith.Conversations.Contracts.Events", "Hexalith.Conversations.Contracts.Governance"],
            "Redaction domain events plus redaction command/result and projection envelopes."),
        new(
            "projection-freshness",
            ["Hexalith.Conversations.Contracts.Projections", "Hexalith.Conversations.Contracts.TrustStates"],
            "Projection freshness shapes and projection trust state."),
        new(
            "contract-validation",
            ["Hexalith.Conversations.Contracts.Versioning", "Hexalith.Conversations.Contracts.Conformance", "Hexalith.Conversations.Contracts.Errors"],
            "Schema versioning, conformance contract types, and the closed error-code vocabulary."),
    ];

    [Fact]
    public void SnapshotShouldCaptureExportedPublicTypesDeterministically()
    {
        PublicContractShapeSnapshotV1 first = BuildSnapshot();
        PublicContractShapeSnapshotV1 second = BuildSnapshot();

        first.Types.ShouldNotBeEmpty();
        JsonSerializer.Serialize(first, SnapshotOptions)
            .ShouldBe(JsonSerializer.Serialize(second, SnapshotOptions));
    }

    [Fact]
    public void CurrentSnapshotShouldMatchCommittedBaselineWithoutWriting()
    {
        string root = FindRepositoryRoot();
        string baselinePath = Path.Combine(root, "docs", "release-evidence", "public-contract-shape-baseline-v1.json");
        string committedBaseline = File.ReadAllText(baselinePath);
        string currentSnapshot = JsonSerializer.Serialize(BuildSnapshot(), SnapshotOptions);

        currentSnapshot.ShouldBe(
            committedBaseline,
            "The full live public contract shape differs from the immutable Story 1.1 baseline. "
            + "Review the member-level diff and obtain explicit approval instead of regenerating the baseline.");
    }

    [Fact]
    public void SnapshotShouldCoverAllSixReleaseGateBehaviorAreas()
    {
        PublicContractShapeSnapshotV1 snapshot = BuildSnapshot();
        HashSet<string> capturedNamespaces = snapshot.Types.Select(t => t.Namespace).ToHashSet();

        snapshot.ReleaseGateAreaCoverage.Select(a => a.Area)
            .ShouldBe(["tenant-isolation", "governance-audit", "idempotency", "redaction", "projection-freshness", "contract-validation"]);

        foreach (ReleaseGateAreaCoverage area in snapshot.ReleaseGateAreaCoverage)
        {
            foreach (string ns in area.Namespaces)
            {
                capturedNamespaces.ShouldContain(ns, $"Release-gate area '{area.Area}' references namespace '{ns}' that the snapshot did not capture.");
            }
        }
    }

    [Fact]
    public void SnapshotShouldBeContentSafe()
    {
        // The captured surface must contain ONLY public Conversations contract type/member names — never
        // substrate mechanics or host paths. Mirrors the existing release-evidence content-safety scans.
        // The scan targets the reflected `Types` payload (the surface that could leak substrate names); the
        // self-describing header is human-authored prose that legitimately uses the word "snapshot" (as does
        // the story/AC text), so it is intentionally not part of the scanned surface.
        string[] forbidden =
        [
            "EventStore",
            "snapshot",
            "SignalR",
            "dispatcher",
            "repository",
            "provider payload",
            "raw exception",
            "C:\\",
            "D:\\",
        ];

        string typesJson = JsonSerializer.Serialize(BuildSnapshot().Types, SnapshotOptions);

        foreach (string fragment in forbidden)
        {
            typesJson.ShouldNotContain(fragment, Case.Insensitive, $"Captured contract surface must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void GenerateAndSaveContractShapeSnapshotFile()
    {
        // Generates the committed FR-20 / Story 5.1 baseline artifact at docs/release-evidence/ and
        // re-reads + re-validates it in the same pass so the committed file always round-trips.
        PublicContractShapeSnapshotV1 snapshot = BuildSnapshot();
        string json = JsonSerializer.Serialize(snapshot, SnapshotOptions);

        string root = FindRepositoryRoot();
        string dir = Path.Combine(root, "docs", "release-evidence");
        string path = Path.Combine(dir, "public-contract-shape-baseline-v1.json");

        Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);

        string readBack = File.ReadAllText(path);
        PublicContractShapeSnapshotV1? parsed = JsonSerializer.Deserialize<PublicContractShapeSnapshotV1>(readBack, SnapshotOptions);
        parsed.ShouldNotBeNull();
        parsed!.Types.Count.ShouldBe(snapshot.Types.Count);
        parsed.Assembly.ShouldBe(ContractsAssemblyName);

        // Determinism guard: re-serializing the round-tripped artifact reproduces the committed bytes exactly.
        JsonSerializer.Serialize(parsed, SnapshotOptions).ShouldBe(json);
    }

    private static PublicContractShapeSnapshotV1 BuildSnapshot()
    {
        Assembly assembly = typeof(ConformanceRunResultV1).Assembly;
        assembly.GetName().Name.ShouldBe(ContractsAssemblyName);

        List<PublicTypeShape> types = assembly.GetExportedTypes()
            .Where(t => (t.Namespace ?? string.Empty).StartsWith(ContractsNamespacePrefix, StringComparison.Ordinal))
            .Select(DescribeType)
            .OrderBy(t => t.Namespace, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        return new PublicContractShapeSnapshotV1
        {
            ArtifactKind = "public-contract-shape-baseline",
            Version = "v1",
            Assembly = ContractsAssemblyName,
            Description =
                "Deterministic snapshot of every exported public type and member of the "
                + "Hexalith.Conversations.Contracts assembly. FR-20 / Story 5.1 behavior-preservation baseline: "
                + "Story 5.1 re-runs the generator and diffs its output against this committed file. "
                + "Regenerate with: dotnet test tests/Hexalith.Conversations.Conformance.Tests "
                + "--filter \"FullyQualifiedName~PublicContractShapeSnapshotGenerationTest\". "
                + "Captured by reflection over the built assembly; types and members are sorted for byte-stable diffing. "
                + "Note: the Conversations closed vocabularies (e.g. ConversationErrorCode) use the smart-enum record "
                + "pattern, so their members appear as static properties rather than CLR enum members.",
            GeneratedBy = $"{nameof(PublicContractShapeSnapshotGenerationTest)}.{nameof(GenerateAndSaveContractShapeSnapshotFile)}",
            ReleaseGateAreaCoverage = ReleaseGateAreas,
            TypeCount = types.Count,
            Types = types,
        };
    }

    private static PublicTypeShape DescribeType(Type type)
    {
        string kind = ClassifyKind(type);

        IReadOnlyList<EnumMemberShape> enumMembers = [];
        IReadOnlyList<PropertyShape> properties = [];
        IReadOnlyList<FieldShape> fields = [];
        IReadOnlyList<MethodShape> constructors = [];
        IReadOnlyList<MethodShape> methods = [];

        if (type.IsEnum)
        {
            enumMembers = DescribeEnumMembers(type);
        }
        else
        {
            const BindingFlags memberFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            properties = type.GetProperties(memberFlags)
                .Select(p => new PropertyShape(
                    p.Name,
                    FormatTypeName(p.PropertyType),
                    p.GetMethod is { IsPublic: true },
                    p.SetMethod is { IsPublic: true },
                    p.GetMethod?.IsStatic == true || p.SetMethod?.IsStatic == true))
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            fields = type.GetFields(memberFlags)
                .Where(f => !f.IsSpecialName && !f.Name.Contains('<', StringComparison.Ordinal))
                .Select(f => new FieldShape(f.Name, FormatTypeName(f.FieldType), f.IsLiteral, f.IsStatic))
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .ToList();

            constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(c => new MethodShape("(ctor)", null, DescribeParameters(c.GetParameters())))
                .OrderBy(SignatureKey, StringComparer.Ordinal)
                .ToList();

            methods = type.GetMethods(memberFlags)
                .Where(m => !m.IsSpecialName
                    && !m.Name.Contains('<', StringComparison.Ordinal)
                    && !SuppressedMethodNames.Contains(m.Name))
                .Select(m => new MethodShape(m.Name, FormatTypeName(m.ReturnType), DescribeParameters(m.GetParameters())))
                .OrderBy(SignatureKey, StringComparer.Ordinal)
                .ToList();
        }

        IReadOnlyList<string> interfaces = type.GetInterfaces()
            .Select(FormatTypeName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        return new PublicTypeShape
        {
            Namespace = type.Namespace ?? string.Empty,
            Name = FormatSimpleName(type),
            Kind = kind,
            IsAbstract = type is { IsAbstract: true, IsInterface: false } && !type.IsSealed,
            IsSealed = type.IsSealed && !type.IsEnum && !type.IsValueType,
            BaseType = type.BaseType is { } bt && bt != typeof(object) && bt != typeof(ValueType) && bt != typeof(Enum)
                ? FormatTypeName(bt)
                : null,
            Interfaces = interfaces.Count > 0 ? interfaces : null,
            EnumUnderlyingType = type.IsEnum ? FormatTypeName(type.GetEnumUnderlyingType()) : null,
            EnumMembers = enumMembers.Count > 0 ? enumMembers : null,
            Properties = properties.Count > 0 ? properties : null,
            Fields = fields.Count > 0 ? fields : null,
            Constructors = constructors.Count > 0 ? constructors : null,
            Methods = methods.Count > 0 ? methods : null,
        };
    }

    private static IReadOnlyList<EnumMemberShape> DescribeEnumMembers(Type enumType)
    {
        string[] names = Enum.GetNames(enumType);
        Array values = Enum.GetValuesAsUnderlyingType(enumType);

        return names
            .Select((name, i) => new EnumMemberShape(name, Convert.ToString(values.GetValue(i), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty))
            .OrderBy(m => m.Value, StringComparer.Ordinal)
            .ThenBy(m => m.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<ParameterShape> DescribeParameters(ParameterInfo[] parameters)
        => parameters.Select(p => new ParameterShape(p.Name ?? string.Empty, FormatTypeName(p.ParameterType))).ToList();

    private static string SignatureKey(MethodShape method)
        => $"{method.Name}({string.Join(",", method.Parameters.Select(p => p.Type))})->{method.ReturnType}";

    private static string ClassifyKind(Type type)
    {
        if (type.IsEnum)
        {
            return "enum";
        }

        if (type.IsInterface)
        {
            return "interface";
        }

        bool isRecord = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Any(m => string.Equals(m.Name, "<Clone>$", StringComparison.Ordinal));

        if (type.IsValueType)
        {
            return isRecord ? "record struct" : "struct";
        }

        return isRecord ? "record" : "class";
    }

    private static string FormatSimpleName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string baseName = type.Name.Split('`')[0];
        string args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
        return $"{baseName}<{args}>";
    }

    private static string FormatTypeName(Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsArray)
        {
            return FormatTypeName(type.GetElementType()!) + "[]";
        }

        Type? nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
        {
            return FormatTypeName(nullableUnderlying) + "?";
        }

        if (type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            string baseName = (definition.FullName ?? definition.Name).Split('`')[0];
            string args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{baseName}<{args}>";
        }

        return type.FullName ?? type.Name;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    /// <summary>Root of the public contract-shape snapshot artifact.</summary>
    internal sealed record PublicContractShapeSnapshotV1
    {
        public required string ArtifactKind { get; init; }

        public required string Version { get; init; }

        public required string Assembly { get; init; }

        public required string Description { get; init; }

        public required string GeneratedBy { get; init; }

        public required IReadOnlyList<ReleaseGateAreaCoverage> ReleaseGateAreaCoverage { get; init; }

        public required int TypeCount { get; init; }

        public required IReadOnlyList<PublicTypeShape> Types { get; init; }
    }

    /// <summary>Maps a release-gate behavior area to the public namespaces that back its envelope.</summary>
    internal sealed record ReleaseGateAreaCoverage(string Area, IReadOnlyList<string> Namespaces, string Note);

    /// <summary>Deterministic description of one exported public type.</summary>
    internal sealed record PublicTypeShape
    {
        public required string Namespace { get; init; }

        public required string Name { get; init; }

        public required string Kind { get; init; }

        public bool IsAbstract { get; init; }

        public bool IsSealed { get; init; }

        public string? BaseType { get; init; }

        public IReadOnlyList<string>? Interfaces { get; init; }

        public string? EnumUnderlyingType { get; init; }

        public IReadOnlyList<EnumMemberShape>? EnumMembers { get; init; }

        public IReadOnlyList<PropertyShape>? Properties { get; init; }

        public IReadOnlyList<FieldShape>? Fields { get; init; }

        public IReadOnlyList<MethodShape>? Constructors { get; init; }

        public IReadOnlyList<MethodShape>? Methods { get; init; }
    }

    internal sealed record EnumMemberShape(string Name, string Value);

    internal sealed record PropertyShape(string Name, string Type, bool CanRead, bool CanWrite, bool IsStatic);

    internal sealed record FieldShape(string Name, string Type, bool IsConstant, bool IsStatic);

    internal sealed record MethodShape(string Name, string? ReturnType, IReadOnlyList<ParameterShape> Parameters);

    internal sealed record ParameterShape(string Name, string Type);
}
