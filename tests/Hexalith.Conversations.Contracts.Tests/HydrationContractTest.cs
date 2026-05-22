// <copyright file="HydrationContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies read-time hydration contracts expose only response-safe fields.
/// </summary>
public sealed class HydrationContractTest
{
    /// <summary>
    /// Hydrated detail responses carry stable references, safe state, and no upstream personal-data surface.
    /// </summary>
    [Fact]
    public void DetailHydrationShouldSerializeSafeAllowlistedFieldsOnly()
    {
        ConversationDetailsV1 details = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.FreshnessV1,
            "Open",
            "Case 123",
            ContractSamples.Business,
            ContractSamples.Project,
            ContractSamples.Folder,
            null,
            [new(ContractSamples.Participant, Participants.ParticipantType.Human, Participants.ParticipantRole.Member)],
            [new(ContractSamples.Message, ContractSamples.Actor, "Hello from the adopter.", ContractSamples.EventMetadata.CommittedAt)],
            [new(ContractSamples.File, ContractSamples.Folder, ContractSamples.Message)],
            "Unavailable",
            PartyHydration:
            [
                new PartyReferenceHydrationV1(
                    ContractSamples.Participant,
                    ProjectionTrustState.Current,
                    Resolved: true,
                    "Project participant",
                    "participant-token",
                    "Available"),
            ],
            ProjectHydration: new ProjectReferenceHydrationV1(
                ContractSamples.Project,
                ProjectionTrustState.Unavailable,
                Resolved: false,
                "Reference unavailable",
                "unavailable",
                "Unavailable"),
            FolderHydration: new FolderReferenceHydrationV1(
                ContractSamples.Folder,
                ProjectionTrustState.Redacted,
                Resolved: false,
                "Reference redacted",
                "redacted",
                "Redacted"),
            FileHydration:
            [
                new FileReferenceHydrationV1(
                    ContractSamples.File,
                    ProjectionTrustState.Forbidden,
                    Resolved: false,
                    "Reference unavailable",
                    "unavailable",
                    "Unavailable"),
            ]);

        string json = JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("partyHydration");
        json.ShouldContain("hydrationState");
        json.ShouldContain("safeLabel");
        json.ShouldContain("safeToken");
        json.ShouldContain("safeStatus");
        json.ShouldContain("party-participant");
        json.ShouldNotContain("email", Case.Insensitive);
        json.ShouldNotContain("phone", Case.Insensitive);
        json.ShouldNotContain("contact", Case.Insensitive);
        json.ShouldNotContain("person", Case.Insensitive);
        json.ShouldNotContain("organization", Case.Insensitive);
        json.ShouldNotContain("problem", Case.Insensitive);
        json.ShouldNotContain("adapterReason", Case.Insensitive);
        json.ShouldNotContain("internalReason", Case.Insensitive);
        json.ShouldNotContain("rawReason", Case.Insensitive);
    }
}
