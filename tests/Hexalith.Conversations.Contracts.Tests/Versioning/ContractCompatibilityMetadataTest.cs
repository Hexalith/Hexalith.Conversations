// <copyright file="ContractCompatibilityMetadataTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Versioning;

/// <summary>
/// Verifies adopter-facing contract compatibility metadata and safe version checks.
/// </summary>
public sealed class ContractCompatibilityMetadataTest
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CurrentCompatibilityMetadataShouldExposeSupportedPackageAndContractVersions()
    {
        ContractCompatibilityMetadata metadata = ConversationContractCompatibility.Current;

        metadata.SchemaVersion.ShouldBe(SchemaVersion.Current);
        metadata.Status.ShouldBe(ContractCompatibilityStatus.Supported);
        metadata.CommandContracts.ContractName.ShouldBe("commands");
        metadata.ProjectionContracts.ContractName.ShouldBe("projections");
        metadata.EventContracts.ContractName.ShouldBe("events");
        metadata.CommandContracts.ActiveSchemaVersion.ShouldBe(SchemaVersion.Current);
        metadata.ProjectionContracts.ActiveSchemaVersion.ShouldBe(SchemaVersion.Current);
        metadata.EventContracts.ActiveSchemaVersion.ShouldBe(SchemaVersion.Current);
        metadata.ContractsPackage.PackageId.ShouldBe("Hexalith.Conversations.Contracts");
        metadata.ClientPackage.PackageId.ShouldBe("Hexalith.Conversations.Client");
        metadata.ContractsPackage.Version.ShouldBe("1.0.0");
        metadata.ClientPackage.Version.ShouldBe("1.0.0");
        metadata.Remediations.ShouldBeEmpty();
    }

    [Fact]
    public void ContractVersionInfoShouldRejectNullCompatibilityStatus()
        => Should.Throw<ArgumentNullException>(() => new ContractVersionInfo(
            "commands",
            SchemaVersion.Current,
            SchemaVersion.Current)
        {
            Status = null!,
        });

    [Fact]
    public void CompatibilityMetadataShouldRejectStatusRemediationMismatches()
    {
        ContractCompatibilityRemediation remediation = new(
            "upgrade-to-active-v1",
            new Uri("https://docs.hexalith.local/conversations/contracts/v1/compatibility"));

        Should.Throw<ArgumentException>(() => new ContractCompatibilityMetadata(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Supported,
            ConversationContractCompatibility.Current.CommandContracts,
            ConversationContractCompatibility.Current.ProjectionContracts,
            ConversationContractCompatibility.Current.EventContracts,
            ConversationContractCompatibility.Current.ContractsPackage,
            ConversationContractCompatibility.Current.ClientPackage,
            [remediation]));

        Should.Throw<ArgumentException>(() => new ContractCompatibilityMetadata(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Unsupported,
            ConversationContractCompatibility.Current.CommandContracts,
            ConversationContractCompatibility.Current.ProjectionContracts,
            ConversationContractCompatibility.Current.EventContracts,
            ConversationContractCompatibility.Current.ContractsPackage,
            ConversationContractCompatibility.Current.ClientPackage,
            []));
    }

    [Fact]
    public void CompatibilityResultShouldRejectUnsafeStatusRemediationAndErrorCombinations()
    {
        ContractCompatibilityRemediation remediation = new(
            "use-supported-v1-package",
            new Uri("https://docs.hexalith.local/conversations/contracts/v1/compatibility"));
        ConversationError error = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(CommandSchemaVersion: "2")).Error!;

        Should.Throw<ArgumentException>(() => new ContractCompatibilityResult(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Supported,
            ConversationContractCompatibility.Current,
            [remediation]));

        Should.Throw<ArgumentException>(() => new ContractCompatibilityResult(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Unsupported,
            ConversationContractCompatibility.Current,
            []));

        Should.Throw<ArgumentException>(() => new ContractCompatibilityResult(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Unsupported,
            ConversationContractCompatibility.Current,
            [remediation]));

        Should.Throw<ArgumentException>(() => new ContractCompatibilityResult(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Deprecated,
            ConversationContractCompatibility.Current,
            [remediation],
            error));
    }

    [Fact]
    public void CompatibilityResultShouldSnapshotRemediationInput()
    {
        ContractCompatibilityRemediation remediation = new(
            "upgrade-to-active-v1",
            new Uri("https://docs.hexalith.local/conversations/contracts/v1/compatibility"));
        List<ContractCompatibilityRemediation> input = [remediation];

        ContractCompatibilityResult result = new(
            SchemaVersion.Current,
            ContractCompatibilityStatus.Deprecated,
            ConversationContractCompatibility.Current,
            input);

        input.Clear();

        result.Remediations.ShouldHaveSingleItem();
    }

    [Fact]
    public void CompatibilityStatusShouldSerializeAsClosedVocabulary()
    {
        JsonSerializer.Serialize(ContractCompatibilityStatus.Supported, Options).ShouldBe("\"supported\"");
        JsonSerializer.Serialize(ContractCompatibilityStatus.Deprecated, Options).ShouldBe("\"deprecated\"");
        JsonSerializer.Serialize(ContractCompatibilityStatus.Unsupported, Options).ShouldBe("\"unsupported\"");
        JsonSerializer.Serialize(ContractCompatibilityStatus.Invalid, Options).ShouldBe("\"invalid\"");

        JsonSerializer.Deserialize<ContractCompatibilityStatus>("\"supported\"", Options)
            .ShouldBe(ContractCompatibilityStatus.Supported);
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ContractCompatibilityStatus>("\"SUPPORTED\"", Options));
    }

    [Fact]
    public void CurrentVersionsShouldEvaluateAsSupported()
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            CommandSchemaVersion: "1",
            ProjectionSchemaVersion: "1",
            EventSchemaVersion: "1",
            ContractsPackageVersion: "1.0.0",
            ClientPackageVersion: "1.0.0"));

        result.Status.ShouldBe(ContractCompatibilityStatus.Supported);
        result.Error.ShouldBeNull();
        result.Remediations.ShouldBeEmpty();
    }

    [Fact]
    public void DeprecatedPackageVersionsShouldReturnSafeRemediation()
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            ContractsPackageVersion: "0.9.0",
            ClientPackageVersion: "0.9.0"));

        result.Status.ShouldBe(ContractCompatibilityStatus.Deprecated);
        result.Error.ShouldBeNull();
        result.Remediations.Select(r => r.GuidanceCode).ShouldContain("upgrade-to-active-v1");
        result.Remediations.All(r => r.DocumentationUri.IsAbsoluteUri).ShouldBeTrue();
    }

    [Fact]
    public void UnsupportedSchemaVersionsShouldReturnTypedCompatibilityResult()
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            CommandSchemaVersion: "2"));

        result.Status.ShouldBe(ContractCompatibilityStatus.Unsupported);
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        result.Error.Category.ShouldBe(ConversationErrorCategory.Versioning);
        result.Error.SafeFieldDiagnostics.ShouldNotBeNull();
        result.Error.SafeFieldDiagnostics.Keys.ShouldContain("commandSchemaVersion");
        result.Error.DeveloperGuidance.ShouldBe("Use the active v1 contracts package and client package.");
    }

    [Fact]
    public void UnsupportedPackageVersionsShouldReturnTypedCompatibilityResult()
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            ContractsPackageVersion: "2.0.0",
            ClientPackageVersion: "2.0.0"));

        result.Status.ShouldBe(ContractCompatibilityStatus.Unsupported);
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        result.Error.Category.ShouldBe(ConversationErrorCategory.Versioning);
        result.Error.SafeFieldDiagnostics.ShouldNotBeNull();
        result.Error.SafeFieldDiagnostics.Keys.ShouldContain("contractsPackageVersion");
        result.Error.SafeFieldDiagnostics.Keys.ShouldContain("clientPackageVersion");
        result.Error.SafeFieldDiagnostics.Values.ShouldNotContain("2.0.0");
        result.Remediations.Select(r => r.GuidanceCode).ShouldContain("use-supported-v1-package");
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.0")]
    [InlineData("latest")]
    public void MalformedSchemaVersionsShouldReturnInvalidCompatibilityResult(string requestedVersion)
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            EventSchemaVersion: requestedVersion));

        result.Status.ShouldBe(ContractCompatibilityStatus.Invalid);
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        result.Error.SafeFieldDiagnostics.ShouldNotBeNull();
        result.Error.SafeFieldDiagnostics.Keys.ShouldContain("eventSchemaVersion");
        result.Error.SafeFieldDiagnostics.Values.ShouldNotContain(requestedVersion);
        result.Remediations.Select(r => r.GuidanceCode).ShouldContain("send-positive-integer-schema-version");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("latest")]
    public void MalformedPackageVersionsShouldReturnInvalidCompatibilityResult(string requestedVersion)
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            ContractsPackageVersion: requestedVersion));

        result.Status.ShouldBe(ContractCompatibilityStatus.Invalid);
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        result.Error.SafeFieldDiagnostics.ShouldNotBeNull();
        result.Error.SafeFieldDiagnostics.Keys.ShouldContain("contractsPackageVersion");
        result.Error.SafeFieldDiagnostics.Values.ShouldNotContain(requestedVersion);
        result.Remediations.ShouldNotBeEmpty();
        result.Remediations.Select(r => r.GuidanceCode).ShouldContain("send-semantic-package-version");
        result.Remediations.All(r => r.DocumentationUri.IsAbsoluteUri).ShouldBeTrue();
    }

    [Fact]
    public void AdditiveCompatibilityMetadataFieldsShouldBeIgnoredByContractDeserialization()
    {
        string json = """
            {
              "schemaVersion": 1,
              "status": "supported",
              "commandContracts": {
                "contractName": "commands",
                "activeSchemaVersion": 1,
                "minimumSupportedSchemaVersion": 1,
                "status": "supported"
              },
              "projectionContracts": {
                "contractName": "projections",
                "activeSchemaVersion": 1,
                "minimumSupportedSchemaVersion": 1,
                "status": "supported"
              },
              "eventContracts": {
                "contractName": "events",
                "activeSchemaVersion": 1,
                "minimumSupportedSchemaVersion": 1,
                "status": "supported"
              },
              "contractsPackage": {
                "packageId": "Hexalith.Conversations.Contracts",
                "version": "1.0.0"
              },
              "clientPackage": {
                "packageId": "Hexalith.Conversations.Client",
                "version": "1.0.0"
              },
              "remediations": [],
              "additiveV1Field": "ignored"
            }
            """;

        ContractCompatibilityMetadata? metadata = JsonSerializer.Deserialize<ContractCompatibilityMetadata>(json, Options);

        metadata.ShouldNotBeNull();
        metadata.Status.ShouldBe(ContractCompatibilityStatus.Supported);
        metadata.ContractsPackage.PackageId.ShouldBe("Hexalith.Conversations.Contracts");
    }

    [Fact]
    public void CompatibilityMetadataJsonShouldRemainContentSafe()
    {
        ContractCompatibilityResult result = ConversationContractCompatibility.Evaluate(new ContractCompatibilityRequest(
            CommandSchemaVersion: "2",
            ContractsPackageVersion: "2.0.0"));

        string json = JsonSerializer.Serialize(result, Options);

        string[] forbiddenFragments =
        [
            "EventStore",
            "stream",
            "snapshot",
            "envelope",
            "SignalR",
            "subscription",
            "server route",
            "displayName",
            "email",
            "phone",
            "provider payload",
            "raw exception",
            "D:\\",
            "C:\\",
        ];

        foreach (string forbidden in forbiddenFragments)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }
}
