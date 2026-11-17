using System.Text.Json;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.Conformance.Portable.Tests;

/// <summary>Preserves the reviewed exported Client API surface.</summary>
public sealed class ClientPublicApiConformanceTest
{
    /// <summary>The built Client exports exactly the reviewed type names.</summary>
    [Fact]
    public void BuiltClientExportsTheReviewedSurface()
    {
        string path = Path.Combine(RepositoryLocator.FindRepositoryRoot(), "docs", "release-evidence", "client-public-api-baseline-v1.json");
        using JsonDocument client = JsonDocument.Parse(RepositoryLocator.ReadAllText(path));
        string[] reviewed = client.RootElement.GetProperty("types").EnumerateArray().Select(type => type.GetProperty("name").GetString()!).Order(StringComparer.Ordinal).ToArray();
        typeof(ClientAssemblyMarker).Assembly.GetExportedTypes().Select(type => type.FullName!).Order(StringComparer.Ordinal)
            .ShouldBe(reviewed, Case.Sensitive, "PUBLIC_CONTRACT_WIDENED: the built Client exports differ from the reviewed baseline.");
    }
}
