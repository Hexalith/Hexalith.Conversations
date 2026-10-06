// <copyright file="PublicClientApiSnapshotTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.Conversations.Client;

namespace Hexalith.Conversations.Client.Tests;

/// <summary>
/// Compares the built Client's exported types and declared public CLR members with the reviewed inventory.
/// </summary>
public sealed class PublicClientApiSnapshotTest
{
    private const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Rejects an unreviewed Client API change without rewriting the committed baseline.
    /// </summary>
    [Fact]
    public void CurrentClientApiShouldMatchReviewedBaselineWithoutWriting()
    {
        string snapshot = BuildSnapshot();

        // Retain the observed surface for review even when the comparison fails. This is build output,
        // never the committed baseline: the normal test run cannot bless its own changed API.
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "client-public-api-current.json"), snapshot);

        string baselinePath = Path.Combine(FindRepositoryRoot(), "docs", "release-evidence", "client-public-api-baseline-v1.json");
        snapshot.ShouldBe(
            File.ReadAllText(baselinePath),
            "The Client public API differs from its reviewed baseline. Review client-public-api-current.json "
            + "in the test output directory and the API diff before explicitly updating the baseline.");
    }

    private static string BuildSnapshot()
    {
        Assembly assembly = typeof(ClientAssemblyMarker).Assembly;
        NullabilityInfoContext nullability = new();
        var types = assembly.GetExportedTypes()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => new
            {
                Name = type.FullName,
                Attributes = type.Attributes.ToString(),
                ApiAttributes = DescribeApiAttributes(type.GetCustomAttributesData()),
                BaseType = type.BaseType?.ToString(),
                Interfaces = type.GetInterfaces().Select(item => item.ToString()).Order(StringComparer.Ordinal).ToArray(),
                GenericConstraints = DescribeGenericConstraints(type.GetGenericArguments()),
                Members = type.GetMembers(PublicMembers)
                    .Select(member => DescribeMember(member, nullability))
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
            })
            .ToArray();

        return JsonSerializer.Serialize(
            new { Assembly = assembly.GetName().Name, Types = types },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true,
                NewLine = "\n",
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }) + "\n";
    }

    private static string DescribeMember(MemberInfo member, NullabilityInfoContext nullability)
        => (member switch
        {
            MethodBase method => $"{member.MemberType}: {method}; {method.Attributes}; "
                + $"parameters=[{string.Join("; ", method.GetParameters().Select(parameter => DescribeParameter(parameter, nullability)))}]"
                + (method is MethodInfo info
                    ? $"; returns={DescribeNullability(nullability.Create(info.ReturnParameter))}; "
                        + $"{DescribeModifiers(info.ReturnParameter.GetRequiredCustomModifiers(), info.ReturnParameter.GetOptionalCustomModifiers())}; "
                        + $"returnAttributes=[{DescribeApiAttributes(info.ReturnParameter.GetCustomAttributesData())}]; "
                        + $"generics=[{DescribeGenericConstraints(info.GetGenericArguments())}]"
                    : string.Empty),
            PropertyInfo property => $"Property: {property}; {DescribeNullability(nullability.Create(property))}",
            FieldInfo field => $"Field: {field}; {field.Attributes}; {DescribeNullability(nullability.Create(field))}; "
                + DescribeModifiers(field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers())
                + (field.IsLiteral ? $"; constant={JsonSerializer.Serialize(field.GetRawConstantValue())}" : string.Empty),
            EventInfo eventInfo => $"Event: {eventInfo}; {DescribeNullability(nullability.Create(eventInfo))}",
            Type nested => $"NestedType: {nested.FullName}; {nested.Attributes}",
            _ => throw new InvalidOperationException($"Unsupported public member: {member.MemberType}: {member.Name}"),
        }) + $"; apiAttributes=[{DescribeApiAttributes(member.GetCustomAttributesData())}]";

    private static string DescribeParameter(ParameterInfo parameter, NullabilityInfoContext nullability)
        => $"{parameter.Name}: {parameter.Attributes}; {DescribeNullability(nullability.Create(parameter))}; "
            + $"hasDefault={parameter.HasDefaultValue}; default={(parameter.HasDefaultValue ? JsonSerializer.Serialize(parameter.DefaultValue) : "none")}; "
            + $"{DescribeModifiers(parameter.GetRequiredCustomModifiers(), parameter.GetOptionalCustomModifiers())}; "
            + $"apiAttributes=[{DescribeApiAttributes(parameter.GetCustomAttributesData())}]";

    private static string DescribeModifiers(Type[] required, Type[] optional)
        => $"modreq=[{string.Join(",", required.Select(type => type.ToString()))}]; "
            + $"modopt=[{string.Join(",", optional.Select(type => type.ToString()))}]";

    private static string DescribeApiAttributes(IEnumerable<CustomAttributeData> attributes)
        => string.Join("; ", attributes
            .Where(attribute => attribute.AttributeType == typeof(ObsoleteAttribute)
                || attribute.AttributeType == typeof(ParamArrayAttribute)
                || attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis"
                || attribute.AttributeType.Namespace == "System.Runtime.CompilerServices"
                    && attribute.AttributeType.Name is not ("CompilerGeneratedAttribute" or "AsyncStateMachineAttribute"
                        or "IteratorStateMachineAttribute" or "AsyncIteratorStateMachineAttribute"
                        or "NullableAttribute" or "NullableContextAttribute" or "NullablePublicOnlyAttribute"))
            .Select(attribute => attribute.ToString())
            .Order(StringComparer.Ordinal));

    private static string DescribeNullability(NullabilityInfo info)
        => $"{info.ReadState}/{info.WriteState}"
            + (info.GenericTypeArguments.Length > 0 ? $"<{string.Join(",", info.GenericTypeArguments.Select(DescribeNullability))}>" : string.Empty)
            + (info.ElementType is { } element ? $"[{DescribeNullability(element)}]" : string.Empty);

    private static string DescribeGenericConstraints(Type[] arguments)
        => string.Join("; ", arguments.Where(argument => argument.IsGenericParameter).Select(argument =>
            $"{argument.Name}: {argument.GenericParameterAttributes}; "
            + string.Join(",", argument.GetGenericParameterConstraints().Select(constraint => constraint.ToString()).Order(StringComparer.Ordinal))));

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

        throw new DirectoryNotFoundException("Could not find the Conversations repository root.");
    }
}
