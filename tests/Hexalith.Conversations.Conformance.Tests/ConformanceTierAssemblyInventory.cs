// <copyright file="ConformanceTierAssemblyInventory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Reads both declared tier metadata tables without loading module-internal runtime types.</summary>
internal static class ConformanceTierAssemblyInventory
{
    /// <summary>Returns the suite class names from both current tier binaries.</summary>
    /// <returns>The complete suite name inventory, retaining duplicate entries.</returns>
    internal static string[] SuiteClassNames()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        string framework = directory.Name;
        string configuration = directory.Parent!.Name;
        DirectoryInfo? root = directory;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Hexalith.Conversations.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            throw new DirectoryNotFoundException("TIER_PROJECT_MISSING: repository root unavailable.");
        }

        List<string> names = [];
        foreach (string name in new[] { "Hexalith.Conversations.Conformance.Portable.Tests", "Hexalith.Conversations.Conformance.Tests" })
        {
            using FileStream stream = File.OpenRead(Path.Combine(root.FullName, "tests", name, "bin", configuration, framework, name + ".dll"));
            using PEReader reader = new(stream);
            MetadataReader metadata = reader.GetMetadataReader();
            foreach (TypeDefinitionHandle handle in metadata.TypeDefinitions)
            {
                TypeDefinition type = metadata.GetTypeDefinition(handle);
                string typeName = metadata.GetString(type.Name);
                if (metadata.GetString(type.Namespace) == "Hexalith.Conversations.Conformance.Tests"
                    && typeName.EndsWith("ConformanceSuiteTest", StringComparison.Ordinal))
                {
                    names.Add(typeName);
                }
            }
        }

        names.Count.ShouldBe(names.Distinct(StringComparer.Ordinal).Count(), "ASSERTION_INVENTORY_DRIFT: a suite compiles in both tiers.");
        return names.ToArray();
    }
}
