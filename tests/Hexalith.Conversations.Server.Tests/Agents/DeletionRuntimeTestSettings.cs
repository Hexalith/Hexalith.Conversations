// <copyright file="DeletionRuntimeTestSettings.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Identity;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Synthetic installation values for the opt-in deletion runtime host; never enrolled or qualified identities.</summary>
internal static class DeletionRuntimeTestSettings
{
    public const string Target = "0123456789abcdef0123456789abcdef01234567";
    public const string RolloverTarget = "89abcdef0123456789abcdef0123456789abcdef";
    public const string Worker = "conversations-worker";
    public const string Receiver = "conversations-deletion-conformance";
    public const string Lookup = "conversations-deletion-lookup";
    public const string Reader = "conversations-runtime-reader";
    public const string Platform = "eventstore";
    public const string Installation = "installation-a";
    public const string Storage = "conversations-storage";
    public const string ServiceParty = "conversations-dedicated-worker";
    public const string Gateway = "https://gateway.test/";
    public const string Sidecar = "http://127.0.0.1:3500";
    public const string SidecarToken = "sidecar-api-token";
    public const string WorkerToken = "worker-authority-token";
    public const string WorkerSecret = "worker-secret-value";
    public const string LookupSecret = "lookup-secret-value";
    public const string SigningKey = "deletion-runtime-test-signing-key-0123456789";

    /// <summary>Complete settings with a Development-only symmetric shared contract, for real-pipeline tests.</summary>
    public static Dictionary<string, string?> SymmetricConfiguration()
    {
        Dictionary<string, string?> settings = Configuration();
        settings.Remove("Authentication:JwtBearer:Authority");
        settings.Remove("Authentication:JwtBearer:AllowedAlgorithms:0");
        settings["Authentication:JwtBearer:SigningKey"] = SigningKey;
        return settings;
    }

    /// <summary>Signs a caller token with the Development symmetric contract.</summary>
    public static string CallerToken(string subject, string issuer = "https://authority.test/", IDictionary<string, object>? claims = null)
    {
        var all = new Dictionary<string, object>(claims ?? new Dictionary<string, object>(), StringComparer.Ordinal) { ["sub"] = subject };
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = "eventstore",
            Claims = all,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
    public const string TokenEndpoint = "https://authority.test/connect/token";

    public static ConversationDeletionRuntimeOptions Options() => new()
    {
        Enabled = true,
        TenantId = F.Tenant.Value,
        WorkerPrincipalId = Worker,
        ServicePartyId = ServiceParty,
        NamespaceInstallationId = Installation,
        StorageIdentity = Storage,
        GatewayBaseAddress = Gateway,
        PlatformWorkloadId = Platform,
        ReceiverPrincipalId = Receiver,
        LookupPrincipalId = Lookup,
        IdentityReaderPrincipalId = Reader,
        AcceptedTargetVersions = [Target, RolloverTarget],
    };

    public static Dictionary<string, string?> Configuration(bool enabled = true)
    {
        const string s = ConversationDeletionRuntimeOptions.SectionName;
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [ConversationProjectionStoreOptions.ConfigurationKey] = "deletion-runtime-state",
            ["EventStore:DataProtection:StateStoreName"] = "deletion-runtime-keys",
            ["EventStore:DataProtection:PersistToStateStore"] = "false",
            ["Tenants:PubSubName"] = "deletion-runtime-events",
            ["Tenants:TopicName"] = "deletion-runtime-tenants",
            ["Dapr:DeclarativeSubscriptions"] = "true",
        };
        if (!enabled)
        {
            return settings;
        }

        settings[s + ":Enabled"] = "true";
        settings[s + ":TenantId"] = F.Tenant.Value;
        settings[s + ":WorkerPrincipalId"] = Worker;
        settings[s + ":ServicePartyId"] = ServiceParty;
        settings[s + ":NamespaceInstallationId"] = Installation;
        settings[s + ":StorageIdentity"] = Storage;
        settings[s + ":GatewayBaseAddress"] = Gateway;
        settings[s + ":PlatformWorkloadId"] = Platform;
        settings[s + ":ReceiverPrincipalId"] = Receiver;
        settings[s + ":LookupPrincipalId"] = Lookup;
        settings[s + ":IdentityReaderPrincipalId"] = Reader;
        settings[s + ":AcceptedTargetVersions:0"] = Target;
        settings[s + ":AcceptedTargetVersions:1"] = RolloverTarget;
        settings["EventStore:Authentication:Authority"] = "https://authority.test/";
        settings["EventStore:Authentication:TokenEndpoint"] = TokenEndpoint;
        settings["EventStore:Authentication:ClientId"] = "worker-client";
        settings["EventStore:Authentication:ClientSecret"] = WorkerSecret;
        settings["Authentication:JwtBearer:Authority"] = "https://authority.test/";
        settings["Authentication:JwtBearer:Issuer"] = "https://authority.test/";
        settings["Authentication:JwtBearer:Audience"] = "eventstore";
        settings["Authentication:JwtBearer:AllowedAlgorithms:0"] = "RS256";
        settings[s + ":LookupIssuer:ClientId"] = "lookup-client";
        settings[s + ":LookupIssuer:ClientSecret"] = LookupSecret;
        settings[s + ":LookupIssuer:TokenEndpoint"] = TokenEndpoint;
        settings[s + ":LookupIssuer:EnableResourceBindingRequests"] = "true";
        settings["DAPR_HTTP_ENDPOINT"] = Sidecar;
        settings["DAPR_API_TOKEN"] = SidecarToken;
        return settings;
    }

    /// <summary>Approves one deletion on serialized local source events and projects its publication entry.</summary>
    public static async Task<ConversationDeletionDeliveryPumpFixture> ApprovedSourceAsync()
    {
        var f = new F();
        var before = await f.ReplayAsync();
        f.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"),
            F.Conversation, "independent-approval", before.SourceRevision, F.At.AddMinutes(1),
            F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
        var stream = (await f.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value),
            TestContext.Current.CancellationToken)).Stream!;
        return new(f, new(1, new ConversationDeletionPublicationProjector().Project(stream, TestContext.Current.CancellationToken).Single()));
    }

    public static ConversationDeletionSignal Signal(string id = "signal-alpha")
        => new(id, F.Tenant, F.Conversation, new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value).ActorId, 3,
            "independent-approval");

    public static string Evidence(ConversationDeletionSignal signal, string target)
        => ConformanceDeletionWireEvidencePrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            signal.ConversationDeletionSignalId + ":" + target)));

    public const string ConformanceDeletionWireEvidencePrefix = "oq-conformance-v1:";
}
