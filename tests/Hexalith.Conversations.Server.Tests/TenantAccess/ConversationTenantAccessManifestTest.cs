// <copyright file="ConversationTenantAccessManifestTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Server.TenantAccess;

using Microsoft.Extensions.Configuration;

namespace Hexalith.Conversations.Server.Tests.TenantAccess;

/// <summary>
/// Ties the module manifest to the configuration the enrolled tenant access requires at startup.
/// </summary>
public sealed class ConversationTenantAccessManifestTest
{
    private const string ClientSecretKey = ConversationEventStoreGatewayRegistration.AuthoritySection + ":ClientSecret";

    /// <summary>
    /// Every key the enrolled host refuses to start without is a declared manifest input, and the worker client
    /// secret is declared as a secret that <c>integration.secrets</c> maps to the same key.
    /// </summary>
    [Fact]
    public void ManifestDeclaresEveryKeyTheEnrolledHostRequiresAtStartup()
    {
        string path = Path.Combine(FindRepositoryRoot(), "module", "hexalith.module-manifest.v2.json");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement module = manifest.RootElement.GetProperty("modules")[0];
        JsonElement[] inputs = [.. module.GetProperty("runtime").GetProperty("extensions").EnumerateArray()
            .Where(static extension => extension.TryGetProperty("configurationInputs", out _))
            .SelectMany(static extension => extension.GetProperty("configurationInputs").EnumerateArray())];

        // The required keys come from the host's own startup checks, so a new requirement fails here until declared.
        IReadOnlyList<string> authority = ConversationEventStoreGatewayRegistration.AuthorityFailures(new ConfigurationBuilder().Build());
        authority.ShouldContain(ClientSecretKey);
        string[] required =
        [
            ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey,
            .. authority,
            ConversationEventStoreGatewayRegistration.AuthoritySection + ":GrantType",
        ];

        foreach (string key in required)
        {
            JsonElement input = inputs.Where(input => input.GetProperty("key").GetString() == key).ShouldHaveSingleItem(key);
            input.GetProperty("source").GetString().ShouldBe(key == ClientSecretKey ? "secret" : "configuration", key);
        }

        string secretName = inputs.Single(input => input.GetProperty("key").GetString() == ClientSecretKey)
            .GetProperty("reference").GetString().ShouldNotBeNull();
        module.GetProperty("integration").GetProperty("secrets").EnumerateArray()
            .Where(secret => secret.GetProperty("name").GetString() == secretName)
            .ShouldHaveSingleItem()
            .GetProperty("configurationKey").GetString().ShouldBe(ClientSecretKey);

        JsonElement integration = module.GetProperty("integration");
        JsonElement identity = integration.GetProperty("identityNeeds").EnumerateArray()
            .Where(item => item.GetProperty("name").GetString() == "tenant-access-history-reader")
            .ShouldHaveSingleItem();
        identity.GetProperty("audience").GetString().ShouldBe("hexalith-eventstore");
        identity.GetProperty("applicationPrincipal").GetBoolean().ShouldBeTrue();
        identity.GetProperty("claims").EnumerateArray().Select(static claim => claim.GetString()).ShouldBe(["eventstore:permission"]);

        JsonElement[] egress = [.. integration.GetProperty("egress").EnumerateArray()];
        JsonElement gatewayRead = egress.Where(item => item.GetProperty("name").GetString() == "tenant-access-gateway-stream-read")
            .ShouldHaveSingleItem();
        string gatewayReadDestination = gatewayRead.GetProperty("destination").GetString().ShouldNotBeNull();
        gatewayReadDestination.ShouldContain(ConversationTenantAccessServiceCollectionExtensions.GatewayBaseAddressKey + "/api/v1/streams/read");
        gatewayReadDestination.ShouldContain("scope platform:tenants");
        gatewayReadDestination.ShouldContain("permission tenants:read");
        JsonElement workerToken = egress.Where(item => item.GetProperty("name").GetString() == "tenant-access-worker-token")
            .ShouldHaveSingleItem();
        workerToken.GetProperty("destination").GetString().ShouldNotBeNull().ShouldContain(
            ConversationEventStoreGatewayRegistration.AuthoritySection + ":TokenEndpoint");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "module", "hexalith.module-manifest.v2.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The repository root with the module manifest was not found.");
    }
}
