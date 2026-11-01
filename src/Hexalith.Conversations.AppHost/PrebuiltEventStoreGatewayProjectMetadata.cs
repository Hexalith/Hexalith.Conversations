// <copyright file="PrebuiltEventStoreGatewayProjectMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.AppHost;

using System.Reflection;

using Aspire.Hosting;

/// <summary>Identifies the existing Builds-owned gateway host for the package-mode runtime test.</summary>
internal sealed class PrebuiltEventStoreGatewayProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath
    {
        get
        {
            string? root = typeof(ConversationsAppHostTopology).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "EnrolledBuildsRoot").Value;
            ArgumentException.ThrowIfNullOrWhiteSpace(root);
            string project = Path.GetFullPath(Path.Combine(root, "src", "hosts", "Hexalith.Builds.Module.EventStoreHost", "Hexalith.Builds.Module.EventStoreHost.csproj"));
            return File.Exists(project)
                ? project
                : throw new FileNotFoundException("The producer-selected Builds gateway host was not found.", project);
        }
    }

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
