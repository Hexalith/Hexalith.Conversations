// <copyright file="PublicQueryCompatibilityTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.TestSupport;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Story 12.3 (<c>AC-12.3-05</c>) public query compatibility and contract snapshot preservation guard.
/// </summary>
public sealed class PublicQueryCompatibilityTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// <c>AC-12.3-05</c>: Proves the frozen public contract snapshot, query filters, ordering/cursor shapes,
    /// freshness vocabulary, forbidden/nonexistent equivalence, and wire response shapes are completely unchanged
    /// (<c>PUBLIC_QUERY_CONTRACT_DRIFT</c>).
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task DerivedKeyOptimizationShouldPreserveQueryContract()
    {
        // 1. Frozen public contract snapshot artifacts must remain byte-identical.
        string root = RepositoryLocator.FindRepositoryRoot();
        (string FileName, string ExpectedSha256)[] frozenSnapshots =
        [
            ("public-contract-shape-baseline-v1.json", "EBFC2F67E90ECC8A7734719C6E2673B6E8392AB2CAE9956A8E98B7BF769ACFCA"),
            ("public-contract-shape-story-9.2-v2.json", "0160A2BE428C681D22C014AFA109FC80EB97706FE6F47367AC571B9A9A7595F8"),
            ("public-contract-shape-story-6.4-v3.json", "EE61D0E210E5D43B9AE906E0ED422CD791D24722C5074C995BF14FEA3C5041EC"),
            ("public-contract-shape-story-6.4-v3-review.json", "721EED2082089F61A7B4105BFBCA263FF9E3136180CA49C2703CE1F887127C62"),
            ("public-contract-shape-bounded-read-v4.json", "4DE710B386FF63A2316C603E40556BC78B0F61BBF1635A419CF0D436A1353D74"),
        ];
        foreach ((string fileName, string expectedSha256) in frozenSnapshots)
        {
            byte[] bytes = RepositoryLocator.ReadAllBytes(Path.Combine(root, "docs", "release-evidence", fileName));
            Convert.ToHexString(SHA256.HashData(bytes))
                .ShouldBe(expectedSha256, $"PUBLIC_QUERY_CONTRACT_DRIFT: frozen snapshot {fileName} changed");
        }

        // 2. Live reflected Contracts public surface must match public-contract-shape-bounded-read-v4.json.
        string v4Path = Path.Combine(root, "docs", "release-evidence", "public-contract-shape-bounded-read-v4.json");
        using JsonDocument v4Doc = JsonDocument.Parse(RepositoryLocator.ReadAllBytes(v4Path));
        JsonElement v4Root = v4Doc.RootElement;
        int expectedTypeCount = v4Root.GetProperty("typeCount").GetInt32();
        JsonElement v4Types = v4Root.GetProperty("types");

        Assembly contractsAssembly = typeof(ListConversationsQuery).Assembly;
        Type[] exportedContractTypes =
        [
            .. contractsAssembly.GetExportedTypes()
                .Where(static t => (t.Namespace ?? string.Empty).StartsWith("Hexalith.Conversations.Contracts", StringComparison.Ordinal))
                .OrderBy(static t => t.Namespace, StringComparer.Ordinal)
                .ThenBy(static t => t.Name, StringComparer.Ordinal),
        ];

        exportedContractTypes.Length.ShouldBe(expectedTypeCount, "PUBLIC_QUERY_CONTRACT_DRIFT: exported public type count changed");

        // Verify kind, isAbstract, isSealed, constructors, property signatures and accessors, and methods across ALL exported contract types.
        JsonElement[] expectedTypes = [.. v4Types.EnumerateArray()];
        expectedTypes.Length.ShouldBe(exportedContractTypes.Length, "PUBLIC_QUERY_CONTRACT_DRIFT: snapshot type list length mismatch");
        for (int index = 0; index < exportedContractTypes.Length; index++)
        {
            Type actualType = exportedContractTypes[index];
            JsonElement expectedType = expectedTypes[index];
            string fullName = $"{actualType.Namespace}.{FormatSimpleName(actualType)}";

            (actualType.Namespace ?? string.Empty).ShouldBe(expectedType.GetProperty("namespace").GetString(), $"PUBLIC_QUERY_CONTRACT_DRIFT: namespace mismatch at {fullName}");
            FormatSimpleName(actualType).ShouldBe(expectedType.GetProperty("name").GetString(), $"PUBLIC_QUERY_CONTRACT_DRIFT: name mismatch at {fullName}");
            ClassifyKind(actualType).ShouldBe(expectedType.GetProperty("kind").GetString(), $"PUBLIC_QUERY_CONTRACT_DRIFT: kind mismatch on {fullName}");
            bool isAbstract = actualType is { IsAbstract: true, IsInterface: false } && !actualType.IsSealed;
            bool isSealed = actualType.IsSealed && !actualType.IsEnum && !actualType.IsValueType;
            isAbstract.ShouldBe(expectedType.GetProperty("isAbstract").GetBoolean(), $"PUBLIC_QUERY_CONTRACT_DRIFT: isAbstract mismatch on {fullName}");
            isSealed.ShouldBe(expectedType.GetProperty("isSealed").GetBoolean(), $"PUBLIC_QUERY_CONTRACT_DRIFT: isSealed mismatch on {fullName}");

            if (actualType.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(actualType);
                FormatTypeName(underlying).ShouldBe(
                    expectedType.GetProperty("enumUnderlyingType").GetString(),
                    $"PUBLIC_QUERY_CONTRACT_DRIFT: enum underlying type changed on {fullName}");
                string[] enumNames = Enum.GetNames(actualType);
                Array enumValues = Enum.GetValuesAsUnderlyingType(actualType);
                (string Name, string Value)[] actualEnumMembers = new (string Name, string Value)[enumNames.Length];
                for (int memberIndex = 0; memberIndex < enumNames.Length; memberIndex++)
                {
                    actualEnumMembers[memberIndex] = (
                        enumNames[memberIndex],
                        Convert.ToString(enumValues.GetValue(memberIndex), CultureInfo.InvariantCulture)!);
                }

                (string Name, string Value)[] expectedEnumMembers = expectedType.TryGetProperty("enumMembers", out JsonElement enumMembersEl)
                    ? [.. enumMembersEl.EnumerateArray().Select(static m => (m.GetProperty("name").GetString()!, m.GetProperty("value").GetString()!))]
                    : [];
                actualEnumMembers.ShouldBe(expectedEnumMembers, $"PUBLIC_QUERY_CONTRACT_DRIFT: enum members changed on {fullName}");
                continue;
            }

            const BindingFlags memberFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            (string Name, string Type, bool CanRead, bool CanWrite, bool IsStatic)[] actualProps =
            [
                .. actualType.GetProperties(memberFlags)
                    .Select(static p => (
                        p.Name,
                        FormatTypeName(p.PropertyType),
                        p.GetMethod is { IsPublic: true },
                        p.SetMethod is { IsPublic: true },
                        p.GetMethod?.IsStatic == true || p.SetMethod?.IsStatic == true))
                    .OrderBy(static p => p.Name, StringComparer.Ordinal),
            ];
            (string Name, string Type, bool CanRead, bool CanWrite, bool IsStatic)[] expectedProps =
                expectedType.TryGetProperty("properties", out JsonElement propsEl)
                ? [.. propsEl.EnumerateArray().Select(static p => (
                    p.GetProperty("name").GetString()!,
                    p.GetProperty("type").GetString()!,
                    p.GetProperty("canRead").GetBoolean(),
                    p.GetProperty("canWrite").GetBoolean(),
                    p.GetProperty("isStatic").GetBoolean()))]
                : [];
            actualProps.ShouldBe(expectedProps, $"PUBLIC_QUERY_CONTRACT_DRIFT: properties changed on {fullName}");

            string[] actualCtors =
            [
                .. actualType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(static c => FormatMemberSignature("(ctor)", null, c.GetParameters()))
                    .OrderBy(static s => s, StringComparer.Ordinal),
            ];
            string[] expectedCtors = expectedType.TryGetProperty("constructors", out JsonElement ctorsEl)
                ? [.. ctorsEl.EnumerateArray().Select(FormatExpectedSignature)]
                : [];
            actualCtors.ShouldBe(expectedCtors, $"PUBLIC_QUERY_CONTRACT_DRIFT: constructors changed on {fullName}");

            string[] actualMethods =
            [
                .. actualType.GetMethods(memberFlags)
                    .Where(static m => !m.IsSpecialName
                        && !m.Name.Contains('<', StringComparison.Ordinal)
                        && m.Name is not ("ToString" or "GetHashCode" or "Equals" or "GetType"))
                    .Select(static m => FormatMemberSignature(m.Name, FormatTypeName(m.ReturnType), m.GetParameters()))
                    .OrderBy(static s => s, StringComparer.Ordinal),
            ];
            string[] expectedMethods = expectedType.TryGetProperty("methods", out JsonElement methodsEl)
                ? [.. methodsEl.EnumerateArray().Select(FormatExpectedSignature)]
                : [];
            actualMethods.ShouldBe(expectedMethods, $"PUBLIC_QUERY_CONTRACT_DRIFT: methods changed on {fullName}");
        }

        // 3. Filters, ordering/cursor metadata, and validation rules remain preserved.
        DateTimeOffset from = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset to = new(2026, 5, 31, 23, 59, 59, TimeSpan.Zero);
        ConversationListFilterV1 fullFilter = new(
            ContractSamples.Business,
            ContractSamples.Project,
            ContractSamples.Folder,
            "Open",
            from,
            to,
            from.AddDays(5),
            ContractSamples.Participant,
            ProjectionTrustState.Redacted,
            ProjectionTrustState.Current,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified);
        ListConversationsQuery listQuery = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            "caller-contract",
            "corr-contract",
            fullFilter,
            new ConversationPageRequest(25, "opaque-cursor-v1"));
        listQuery.Page.PageSize.ShouldBe(25);
        listQuery.Page.ContinuationCursor.ShouldBe("opaque-cursor-v1");
        Should.Throw<ArgumentException>(() => new ConversationListFilterV1(ProjectedAtFrom: to, ProjectedAtTo: from));

        // 4. Freshness vocabulary remains closed and unchanged.
        ProjectionTrustState[] states =
        [
            ProjectionTrustState.Current,
            ProjectionTrustState.Stale,
            ProjectionTrustState.Rebuilding,
            ProjectionTrustState.Redacted,
            ProjectionTrustState.Unavailable,
            ProjectionTrustState.Forbidden,
        ];
        states.Select(static s => s.Value).Distinct(StringComparer.Ordinal).Count().ShouldBe(6);
        ProjectionFreshnessV1 freshness = ContractSamples.FreshnessV1;
        freshness.AllowsTrustBearingDecision().ShouldBeTrue();

        // 5. Forbidden and nonexistent outcomes must produce identical non-disclosing public response shapes.
        ConversationDetailResult hiddenForUnauthorizedTenant = ConversationDetailResult.Hidden(ContractSamples.Version);
        ConversationDetailResult explicitNonexistentDetail = new(
            ContractSamples.Version,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            Details: null,
            SafeNextAction: "The requested conversation is not available.");
        JsonSerializer.Serialize(hiddenForUnauthorizedTenant, WebOptions)
            .ShouldBe(JsonSerializer.Serialize(explicitNonexistentDetail, WebOptions));
        hiddenForUnauthorizedTenant.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        hiddenForUnauthorizedTenant.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Forbidden);
        hiddenForUnauthorizedTenant.Details.ShouldBeNull();

        ConversationListResult hiddenForTamperedCursor = ConversationListResult.Hidden(ContractSamples.Version);
        ConversationListResult explicitDeniedList = new(
            ContractSamples.Version,
            ProjectionTrustState.Forbidden,
            ProjectionFreshnessReasonCode.Forbidden,
            Conversations: [],
            Page: new ConversationPageMetadata(0, ContinuationCursor: null),
            SafeNextAction: "No accessible matches.");
        JsonSerializer.Serialize(hiddenForTamperedCursor, WebOptions)
            .ShouldBe(JsonSerializer.Serialize(explicitDeniedList, WebOptions));
        hiddenForTamperedCursor.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        hiddenForTamperedCursor.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.Forbidden);
        hiddenForTamperedCursor.Conversations.ShouldBeEmpty();
        hiddenForTamperedCursor.Page.ContinuationCursor.ShouldBeNull();

        // 6. Wire shapes for List and Open responses contain no internal optimization or infrastructure fields.
        ConversationSummaryV1 summary = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            freshness,
            "Open",
            "Contract Case",
            ContractSamples.Business,
            ContractSamples.Project,
            ContractSamples.Folder,
            [ContractSamples.Actor],
            MessageCount: 2,
            FileReferenceCount: 1);
        ConversationListResult listResult = new(
            ContractSamples.Version,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [summary],
            new ConversationPageMetadata(1, "opaque-next-cursor"),
            "Use the cursor only with the same tenant, caller, filters, and ordering.");
        string listJson = JsonSerializer.Serialize(listResult, WebOptions);

        ConversationDetailsV1 details = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            freshness,
            "Open",
            "Contract Case",
            ContractSamples.Business,
            ContractSamples.Project,
            ContractSamples.Folder,
            ConversationProviderCorrelationV1.From(ContractSamples.ProviderCorrelation),
            [new(ContractSamples.Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new(ContractSamples.Message, ContractSamples.Actor, "Hello", ContractSamples.EventMetadata.CommittedAt)],
            [new(ContractSamples.File, ContractSamples.Folder, ContractSamples.Message)],
            GovernanceState: "Current");
        ConversationDetailResult detailResult = ConversationDetailResult.Visible(
            ContractSamples.Version,
            details,
            "Current projection is available.");
        string detailJson = JsonSerializer.Serialize(detailResult, WebOptions);

        foreach (string forbidden in new[]
                 {
                     "generationToken",
                     "dispatchId",
                     "watermarks",
                     "replayStateHash",
                     "eventStore",
                     "dapr",
                     "redis",
                     "total",
                     "hasNext",
                 })
        {
            listJson.ShouldNotContain(forbidden, Case.Insensitive, $"PUBLIC_QUERY_CONTRACT_DRIFT: list leaked '{forbidden}'");
            detailJson.ShouldNotContain(forbidden, Case.Insensitive, $"PUBLIC_QUERY_CONTRACT_DRIFT: detail leaked '{forbidden}'");
        }

        await VerifyRuntimeQueriesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task VerifyRuntimeQueriesAsync(CancellationToken cancellationToken)
    {
        const string storeName = "statestore";
        const string member = "caller-contract-member";
        TenantId tenant = ContractSamples.Tenant;
        ConversationId ordinalFirst = new("contract-a");
        ConversationId ordinalSecond = new("contract-z");
        ConversationId newer = new("contract-newer");
        ProjectId project = ContractSamples.Project;
        FolderId folder = ContractSamples.Folder;
        BusinessReference business = ContractSamples.Business;
        DateTimeOffset tiedAt = ContractSamples.FreshnessV1.LastAppliedEventTimestamp;
        DateTimeOffset newerAt = tiedAt.AddHours(1);
        InMemoryReadModelStore store = new();

        ConversationSummaryProjectionV1 Summary(
            string id,
            DateTimeOffset time,
            ProjectId? projectId = null,
            FolderId? folderId = null,
            BusinessReference? businessReference = null,
            string lifecycle = "Open",
            PartyId? participant = null,
            bool stale = false,
            ProjectionTrustState? redaction = null,
            ConversationAuditReadinessState? auditReadiness = null,
            ConversationVerificationState? verification = null)
        {
            ProjectionFreshnessV1 freshness = new(
                ContractSamples.Version, "pos:0000000001", 1, time, time, TimeSpan.Zero, stale,
                stale ? ProjectionTrustState.Stale : ProjectionTrustState.Current,
                stale ? ProjectionFreshnessReasonCode.StaleThresholdExceeded : ProjectionFreshnessReasonCode.Current);
            return new ConversationSummaryProjectionV1(
                ContractSamples.Version, tenant, new ConversationId(id), freshness, lifecycle,
                BusinessReference: businessReference ?? business,
                ProjectId: projectId ?? project,
                FolderId: folderId ?? folder,
                ParticipantPartyIds: [participant ?? ContractSamples.Participant],
                SearchTrustPreview: new ConversationSearchTrustPreviewV1(
                    freshness.FreshnessState, freshness.ReasonCode, redaction ?? ProjectionTrustState.Redacted,
                    ProjectionTrustState.Current, ConversationCitationAvailability.Available,
                    auditReadiness ?? ConversationAuditReadinessState.Ready,
                    verification ?? ConversationVerificationState.Verified,
                    ConversationSearchMatchSource.Unknown, "Visible through authorized tenant scope."));
        }

        ConversationSummaryProjectionV1[] summaries =
        [
            Summary(ordinalSecond.Value, tiedAt),
            Summary("contract-other-project", tiedAt, projectId: new ProjectId("other-project")),
            Summary("contract-other-folder", tiedAt, folderId: new FolderId("other-folder")),
            Summary("contract-other-business-system", tiedAt, businessReference: new BusinessReference("other", business.Value)),
            Summary("contract-other-business-value", tiedAt, businessReference: new BusinessReference(business.System, "other")),
            Summary("contract-closed", tiedAt, lifecycle: "Closed"),
            Summary("contract-other-participant", tiedAt, participant: new PartyId("other-party")),
            Summary("contract-stale", tiedAt, stale: true),
            Summary("contract-unredacted", tiedAt, redaction: ProjectionTrustState.Current),
            Summary("contract-audit-incomplete", tiedAt, auditReadiness: ConversationAuditReadinessState.Incomplete),
            Summary("contract-unverified", tiedAt, verification: ConversationVerificationState.Unverified),
            Summary("contract-before-range", tiedAt.AddTicks(-1)),
            Summary("contract-after-range", newerAt.AddTicks(1)),
            Summary(ordinalFirst.Value, tiedAt),
            Summary(newer.Value, newerAt),
        ];
        string tenantSegment = EncodeSegment(tenant.Value);
        store.SeedRaw(storeName, $"projection:conversations-index:{tenantSegment}", new
        {
            SchemaVersion = 2,
            Lifecycle = 2,
            Summaries = summaries,
            Watermarks = summaries.ToDictionary(summary => summary.ConversationId.Value, _ => 1L, StringComparer.Ordinal),
            Dispatches = summaries.ToDictionary(
                summary => summary.ConversationId.Value,
                summary => new ConversationProjectionDispatchReference(summary.ConversationId.Value, 1),
                StringComparer.Ordinal),
        });
        foreach (ConversationSummaryProjectionV1 summary in summaries)
        {
            ConversationDetailProjectionV1 detail = new(
                summary.SchemaVersion, tenant, summary.ConversationId, summary.Freshness, summary.LifecycleState,
                BusinessReference: summary.BusinessReference, ProjectId: summary.ProjectId, FolderId: summary.FolderId);
            store.SeedRaw(
                storeName,
                $"projection:conversations:{tenantSegment}:{EncodeSegment(summary.ConversationId.Value)}",
                new ConversationProjectedReadModels(summary, detail) { DispatchId = summary.ConversationId.Value });
        }

        PublicQueryCompatibilityTenantAccess tenantAccess = new(tenant, member);
        ConversationProjectionReadStore readStore = new(store);
        ConversationQueryHandler handler = new(
            tenantAccess,
            readStore,
            new ConversationProjectionReadService(tenantAccess, readStore),
            new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Conversations.PublicQueryCompatibility"));
        ConversationListFilterV1 filter = new(
            BusinessReference: business, ProjectId: project, FolderId: folder, LifecycleState: "Open",
            ProjectedAtFrom: tiedAt, ProjectedAtTo: newerAt, ParticipantPartyId: ContractSamples.Participant,
            RedactionState: ProjectionTrustState.Redacted, FreshnessState: ProjectionTrustState.Current,
            AuditReadiness: ConversationAuditReadinessState.Ready, VerificationState: ConversationVerificationState.Verified);
        ConversationListResult first = await handler.ListAsync(
            new ListConversationsQuery(ContractSamples.Version, tenant, member, "contract-page-1", filter, new ConversationPageRequest(2)),
            cancellationToken);
        first.FreshnessState.ShouldBe(ProjectionTrustState.Current, "PUBLIC_QUERY_CONTRACT_DRIFT: first filtered page freshness");
        first.Conversations.Select(static row => row.ConversationId)
            .ShouldBe([newer, ordinalFirst], "PUBLIC_QUERY_CONTRACT_DRIFT: filtered ordering or ordinal tie-break");
        first.Page.ContinuationCursor.ShouldNotBeNullOrWhiteSpace("PUBLIC_QUERY_CONTRACT_DRIFT: first page cursor");

        ConversationListResult second = await handler.ListAsync(
            new ListConversationsQuery(ContractSamples.Version, tenant, member, "contract-page-2", filter,
                new ConversationPageRequest(2, first.Page.ContinuationCursor)),
            cancellationToken);
        second.FreshnessState.ShouldBe(ProjectionTrustState.Current, "PUBLIC_QUERY_CONTRACT_DRIFT: continuation freshness");
        second.Conversations.Select(static row => row.ConversationId)
            .ShouldBe([ordinalSecond], "PUBLIC_QUERY_CONTRACT_DRIFT: continuation rows");
        second.Page.ContinuationCursor.ShouldBeNull("PUBLIC_QUERY_CONTRACT_DRIFT: final page cursor");

        ConversationListResult recent = await handler.ListAsync(
            new ListConversationsQuery(ContractSamples.Version, tenant, member, "contract-recent",
                filter with { RecentActivityAfter = tiedAt }, new ConversationPageRequest(25)), cancellationToken);
        recent.Conversations.Select(static row => row.ConversationId)
            .ShouldBe([newer], "PUBLIC_QUERY_CONTRACT_DRIFT: recent activity bound is exclusive");

        ConversationDetailResult existing = await handler.GetAsync(
            new GetConversationQuery(ContractSamples.Version, tenant, member, "contract-existing", ordinalFirst), cancellationToken);
        existing.FreshnessState.ShouldBe(ProjectionTrustState.Current, "PUBLIC_QUERY_CONTRACT_DRIFT: existing detail");
        JsonElement firstJson = JsonSerializer.SerializeToElement(first, WebOptions);
        JsonElement existingJson = JsonSerializer.SerializeToElement(existing, WebOptions);
        AssertResponseProperties(firstJson, ["conversations", "freshnessState", "page", "reasonCode", "safeNextAction", "schemaVersion"]);
        AssertResponseProperties(firstJson.GetProperty("page"), ["continuationCursor", "returnedCount"]);
        AssertResponseProperties(firstJson.GetProperty("conversations")[0],
        [
            "businessReference", "conversationId", "fileReferenceCount", "folderHydration", "folderId", "freshness",
            "label", "lifecycleState", "messageCount", "participantPartyIds", "partyHydration", "projectHydration",
            "projectId", "providerCorrelation", "schemaVersion", "searchTrustPreview", "tenantId",
        ]);
        AssertResponseProperties(existingJson, ["details", "freshnessState", "reasonCode", "safeNextAction", "schemaVersion"]);
        AssertResponseProperties(existingJson.GetProperty("details"),
        [
            "attributes", "businessReference", "conversationId", "evidenceEntries", "fileHydration", "fileReferences",
            "folderHydration", "folderId", "freshness", "governanceState", "label", "lifecycleState", "messages",
            "participants", "partyHydration", "projectHydration", "projectId", "providerCorrelation", "redactions",
            "schemaVersion", "sensitivityMarks", "tenantId", "trustPosture",
        ]);
        ConversationDetailResult unauthorized = await handler.GetAsync(
            new GetConversationQuery(ContractSamples.Version, tenant, "caller-outsider", "contract-denied", ordinalFirst), cancellationToken);
        ConversationDetailResult nonexistent = await handler.GetAsync(
            new GetConversationQuery(ContractSamples.Version, tenant, member, "contract-missing", new ConversationId("contract-absent")),
            cancellationToken);
        unauthorized.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden, "PUBLIC_QUERY_CONTRACT_DRIFT: denied detail");
        JsonSerializer.Serialize(unauthorized, WebOptions)
            .ShouldBe(JsonSerializer.Serialize(nonexistent, WebOptions), "PUBLIC_QUERY_CONTRACT_DRIFT: denied and absent detail shapes");
    }

    private static void AssertResponseProperties(JsonElement value, string[] expected)
        => value.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ShouldBe(expected, Case.Sensitive, "PUBLIC_QUERY_CONTRACT_DRIFT: serialized response properties");

    private static string EncodeSegment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
            .Any(static m => string.Equals(m.Name, "<Clone>$", StringComparison.Ordinal));
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

    private static string FormatMemberSignature(string name, string? returnType, ParameterInfo[] parameters)
        => $"{name}({string.Join(",", parameters.Select(static p => $"{p.Name}:{FormatTypeName(p.ParameterType)}"))})->{returnType}";

    private static string FormatExpectedSignature(JsonElement member)
    {
        string name = member.GetProperty("name").GetString()!;
        string? returnType = member.TryGetProperty("returnType", out JsonElement ret) && ret.ValueKind == JsonValueKind.String
            ? ret.GetString()
            : null;
        string parameters = member.TryGetProperty("parameters", out JsonElement paramEl)
            ? string.Join(",", paramEl.EnumerateArray().Select(static p =>
                $"{p.GetProperty("name").GetString()}:{NormalizeLegacyParameterType(p.GetProperty("type").GetString()!)}"))
            : string.Empty;
        return $"{name}({parameters})->{returnType}";
    }

    private static string NormalizeLegacyParameterType(string name)
    {
        if (!name.EndsWith('&') || !name.Contains("[[", StringComparison.Ordinal))
        {
            return name;
        }

        try
        {
            Type? type = Type.GetType(
                name,
                static requested =>
                {
                    Assembly core = typeof(object).Assembly;
                    AssemblyName compatibleCore = core.GetName();
                    compatibleCore.Version = requested.Version;
                    if (requested.FullName == compatibleCore.FullName)
                    {
                        return core;
                    }

                    Assembly contracts = typeof(ListConversationsQuery).Assembly;
                    return requested.FullName == contracts.FullName ? contracts : null;
                },
                static (assembly, typeName, ignoreCase) => (assembly ?? typeof(object).Assembly).GetType(typeName, false, ignoreCase),
                throwOnError: false);
            return type is { IsByRef: true } ? FormatTypeName(type) : name;
        }
        catch (Exception ex) when (ex is ArgumentException or FileLoadException)
        {
            return name;
        }
    }

    private static string FormatTypeName(Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsByRef)
        {
            return FormatTypeName(type.GetElementType()!) + "&";
        }

        if (type.IsArray)
        {
            string rank = type.IsSZArray ? "[]" : type.GetArrayRank() == 1 ? "[*]" : "[" + new string(',', type.GetArrayRank() - 1) + "]";
            return FormatTypeName(type.GetElementType()!) + rank;
        }

        Type? nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
        {
            return FormatTypeName(nullableUnderlying) + "?";
        }

        if (type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            if (definition.DeclaringType is { } declaring)
            {
                Type[] arguments = type.GetGenericArguments();
                int inherited = declaring.GetGenericArguments().Length;
                if (declaring.IsGenericTypeDefinition)
                {
                    declaring = declaring.MakeGenericType(arguments[..inherited]);
                }

                string ownName = definition.Name.Split('`')[0];
                string ownArguments = string.Join(", ", arguments.Skip(inherited).Select(FormatTypeName));
                return FormatTypeName(declaring) + "+" + ownName + (ownArguments.Length == 0 ? string.Empty : $"<{ownArguments}>");
            }

            string baseName = (definition.FullName ?? definition.Name).Split('`')[0];
            string args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{baseName}<{args}>";
        }

        return type.FullName ?? type.Name;
    }
}
