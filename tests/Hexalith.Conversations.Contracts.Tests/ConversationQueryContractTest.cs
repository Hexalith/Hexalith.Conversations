// <copyright file="ConversationQueryContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies public query contracts remain tenant-scoped and content-safe.
/// </summary>
public sealed class ConversationQueryContractTest
{
    /// <summary>
    /// Detail query results serialize as Conversations contracts with freshness and no infrastructure terms.
    /// </summary>
    [Fact]
    public void DetailResultShouldSerializeWithoutInfrastructureVocabulary()
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
            ConversationProviderCorrelationV1.From(ContractSamples.ProviderCorrelation),
            [new(ContractSamples.Participant, Participants.ParticipantType.Human, Participants.ParticipantRole.Member)],
            [new(ContractSamples.Message, ContractSamples.Actor, "Hello from the adopter.", ContractSamples.EventMetadata.CommittedAt)],
            [new(ContractSamples.File, ContractSamples.Folder, ContractSamples.Message)],
            GovernanceState: "Unavailable");

        ConversationDetailResult result = ConversationDetailResult.Visible(
            ContractSamples.Version,
            details,
            "Current projection is available.");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("freshness");
        json.ShouldContain("conversationId");
        json.ShouldContain("governanceState");
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("stream", Case.Insensitive);
        json.ShouldNotContain("snapshot", Case.Insensitive);
        json.ShouldNotContain("providerSessionReference", Case.Insensitive);
    }

    /// <summary>
    /// List contracts keep external business references distinct from stable identities.
    /// </summary>
    [Fact]
    public void ListQueryShouldKeepBusinessReferenceDistinctFromStableIdentifiers()
    {
        ConversationListFilterV1 filter = new(
            ContractSamples.Business,
            ContractSamples.Project,
            ContractSamples.Folder,
            "Open",
            DateFrom: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            DateTo: new DateTimeOffset(2026, 5, 31, 23, 59, 59, TimeSpan.Zero),
            RecentActivityAfter: new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
            ContractSamples.Participant);
        ListConversationsQuery query = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "caller-001",
            "correlation-001",
            filter,
            new ConversationPageRequest(25));

        query.Filter.ShouldNotBeNull();
        query.Filter.BusinessReference.ShouldBe(ContractSamples.Business);
        query.Filter.ProjectId.ShouldBe(ContractSamples.Project);
        query.Filter.FolderId.ShouldBe(ContractSamples.Folder);
    }

    /// <summary>
    /// Safe list results avoid totals and expose only accessible-result-relative metadata.
    /// </summary>
    [Fact]
    public void ListResultShouldExposePermissionSafePageMetadata()
    {
        ConversationListResult result = new(
            ContractSamples.Version,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [
                new ConversationSummaryV1(
                    ContractSamples.Version,
                    ContractSamples.Tenant,
                    ContractSamples.Conversation,
                    ContractSamples.FreshnessV1,
                    "Open",
                    "Case 123",
                    ContractSamples.Business,
                    ContractSamples.Project,
                    ContractSamples.Folder,
                    [ContractSamples.Actor],
                    MessageCount: 1,
                    FileReferenceCount: 0),
            ],
            new ConversationPageMetadata(1, "opaque-cursor"),
            "Use the cursor only with the same tenant, caller, filters, and ordering.");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("continuationCursor");
        json.ShouldNotContain("total", Case.Insensitive);
        json.ShouldNotContain("hasNext", Case.Insensitive);
        json.ShouldNotContain("providerSessionReference", Case.Insensitive);
    }
}
