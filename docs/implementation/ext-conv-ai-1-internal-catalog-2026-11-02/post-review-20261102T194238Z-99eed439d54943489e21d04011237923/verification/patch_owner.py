from pathlib import Path
r=Path('/home/administrator/projects/hexalith/conversations')
def write(n,s):
 p=r/n;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode())
def edit(n,a,b):
 p=r/n;s=p.read_text();assert a in s,(n,a);raw=p.read_bytes();v=s.replace(a,b);p.write_bytes(v.replace('\n','\r\n').encode() if b'\r\n' in raw else v.encode())
edit('src/Hexalith.Conversations.Contracts/Queries/ListConversationsPayload.cs','ConversationListFilterV1? Filter, ConversationPageRequest? Page','ConversationListFilterV1? Filter = null, ConversationPageRequest? Page = null')
edit('src/Hexalith.Conversations.Contracts/Queries/GetConversationPayload.cs','string? ConversationId);','string? ConversationId = null);')
write('src/Hexalith.Conversations.Contracts/Serialization/ConversationQuerySerialization.cs','''using System.Text.Json;

namespace Hexalith.Conversations.Contracts.Serialization;

/// <summary>Owner-internal strict JSON settings shared by ordinary query receivers and their catalog schemas.</summary>
internal static class ConversationQuerySerialization
{
    /// <summary>Gets the existing case-sensitive, strict-number ordinary query protocol.</summary>
    internal static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
''')
edit('src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs','using Hexalith.Conversations.Contracts.Queries;','using Hexalith.Conversations.Contracts.Queries;\nusing Hexalith.Conversations.Contracts.Serialization;')
edit('src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs','''    private static readonly JsonSerializerOptions s_queryJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };''','''    private static readonly JsonSerializerOptions s_queryJsonOptions = ConversationQuerySerialization.Options;''')
write('src/Hexalith.Conversations.Contracts/Serialization/ConversationScalarSchemas.cs','''using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.RegularExpressions;

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Serialization;

/// <summary>Schema projections of the existing Conversations scalar converter families and their owner vocabularies.</summary>
internal static class ConversationScalarSchemas
{
    /// <summary>Gets the owner converter transformation used by the SDK schema exporter.</summary>
    internal static JsonSchemaExporterOptions ExporterOptions { get; } = new() { TransformSchemaNode = Transform };

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode node)
    {
        Type type = context.TypeInfo.Type;
        Type converter = context.TypeInfo.Converter.GetType();
        JsonObject? scalar = null;
        if (IsFamily(converter, typeof(PrefixedIdentifierJsonConverter<>)))
        {
            string prefix = (string)converter.GetProperty("Prefix", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(context.TypeInfo.Converter)!;
            scalar = new() { ["type"] = "string", ["pattern"] = "^" + Regex.Escape(prefix) + @":(?=[\\s\\S]*\\S)[\\s\\S]+$" };
        }
        else if (type == typeof(SchemaVersion))
        {
            scalar = new() { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = int.MaxValue };
        }
        else if (IsFamily(converter, typeof(ConversationStringValueJsonConverter<>)))
        {
            // These converters call the owner's closed Parse method. Read the same declared values,
            // never a duplicate spelling table or a permissive object representation.
            var values = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(property => property.PropertyType == type)
                .Select(property => JsonSerializer.SerializeToNode(property.GetValue(null), type, context.TypeInfo.Options))
                .OrderBy(value => value!.GetValue<string>(), StringComparer.Ordinal).ToArray();
            if (values.Length == 0) throw new InvalidOperationException("The Conversations scalar vocabulary has no declared values: " + type.FullName);
            scalar = new() { ["type"] = "string", ["enum"] = new JsonArray(values) };
        }
        if (scalar is null) return node;
        // Custom reference converters permit null when the owning property is nullable. Preserve that
        // explicit shape while keeping required scalar properties from becoming unconstrained schemas.
        if (context.PropertyInfo?.IsGetNullable == true)
            return new JsonObject { ["anyOf"] = new JsonArray(scalar, new JsonObject { ["type"] = "null" }) };
        return scalar;
    }

    private static bool IsFamily(Type type, Type family)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
            if (current.IsGenericType && current.GetGenericTypeDefinition() == family) return true;
        return false;
    }
}
''')
edit('src/Hexalith.Conversations.Contracts/Routing/ConversationsCatalogDeclarations.cs','using Hexalith.Conversations.Contracts.Queries;','using Hexalith.Conversations.Contracts.Queries;\nusing Hexalith.Conversations.Contracts.Serialization;')
edit('src/Hexalith.Conversations.Contracts/Routing/ConversationsCatalogDeclarations.cs','        var operation = RouteOperationSchema.Create(route, request, result, "internal", "UI-only", EventStorePayloadSerialization.Options);','''        JsonSerializerOptions serializer = route.MessageType is "conversation-list" or "conversation-detail"
            ? ConversationQuerySerialization.Options : EventStorePayloadSerialization.Options;
        var operation = RouteOperationSchema.Create(route, request, result, "internal", "UI-only", serializer, ConversationScalarSchemas.ExporterOptions);''')
edit('src/Hexalith.Conversations.Contracts/Routing/ConversationsCatalogDeclarations.cs','            schema["additionalProperties"] = false;','            schema["type"] = "object";\n            schema["additionalProperties"] = false;')
# Guard only demonstrated structural fields, before any semantic lambda dereferences them.
edit('src/Hexalith.Conversations.EventStoreExtensions/ConversationsCatalogAdapters.cs','        payload => new Dictionary<string, object?> { ["intent-sha256"] = CanonicalIntentPayload.Sha256(semantic(payload)) });','''        payload =>
        {
            ValidateRequiredPayload(payload);
            return new Dictionary<string, object?> { ["intent-sha256"] = CanonicalIntentPayload.Sha256(semantic(payload)) };
        });

    private static void ValidateRequiredPayload<T>(T payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        object request = payload switch
        {
            CreateConversation command => Require(command.PublicCommand), AddParticipant command => Require(command.PublicCommand),
            ReassignConversationProject command => Require(command.PublicCommand), SetConversationRetentionPolicy command => Require(command.PublicCommand),
            MarkConversationContentSensitive command => Require(command.PublicCommand), RedactMessageContent command => Require(command.PublicCommand),
            AddAgentParticipant command => Require(command.PublicCommand), RemoveAgentParticipant command => Require(command.PublicCommand),
            AppendAgentMessage command => Require(command.PublicCommand), ApproveConversationDeletion command => Require(command.PublicCommand),
            RecordConversationDeletionDelivery command => Require(command.PublicCommand),
            EditConversationMessageCommand command => command, DeleteConversationMessageCommand command => command,
            _ => throw new ArgumentException("Unsupported Conversations command payload.", nameof(payload)),
        };
        ConversationCommandMetadata? metadata = request switch
        {
            CreateConversationCommand command => command.Metadata, AddParticipantCommand command => command.Metadata,
            ReassignConversationProjectCommand command => command.Metadata,
            Contracts.Governance.SetConversationRetentionPolicyCommand command => command.Metadata,
            Contracts.Governance.MarkConversationContentSensitiveCommand command => command.Metadata,
            Contracts.Governance.RedactMessageContentCommand command => command.Metadata,
            RemoveAgentParticipantCommand command => command.Metadata, AppendMessageCommand command => command.Metadata,
            ApproveConversationDeletionCommand command => command.Metadata, RecordConversationDeletionDeliveryCommand command => command.Metadata,
            EditConversationMessageCommand command => command.Metadata, DeleteConversationMessageCommand command => command.Metadata,
            _ => throw new ArgumentException("Unsupported Conversations public command.", nameof(payload)),
        };
        ArgumentNullException.ThrowIfNull(metadata, "Metadata");
    }

    private static T Require<T>(T? value) where T : class
        => value ?? throw new ArgumentNullException("PublicCommand");''')
edit('scripts/release_state.py','must contain exactly eight assets','must contain exactly {len(expected)} assets')
edit('scripts/release_state.py','Poll NuGet, then require an exact GitHub tag/release and eight assets.','Poll NuGet, then require the exact GitHub tag/release and declared package/symbol assets.')
edit('tests/tooling/test_release_tooling.py','with self.assertRaisesRegex(ValueError, "exactly eight assets"):','with self.assertRaisesRegex(ValueError, f"exactly {len(release_contract.expected_asset_names(\'1.0.0\'))} assets"):')
