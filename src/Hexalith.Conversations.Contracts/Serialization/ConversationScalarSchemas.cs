using System.Reflection;
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
            scalar = new() { ["type"] = "string", ["pattern"] = "^" + Regex.Escape(prefix) + @":(?=[\s\S]*\S)[\s\S]+$" };
        }
        else if (type == typeof(SchemaVersion))
        {
            scalar = new() { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = int.MaxValue, ["x-conversations-wire-format"] = "int32-number-token-without-fraction-exponent-or-quotes" };
        }
        else if (IsFamily(converter, typeof(ConversationStringValueJsonConverter<>)))
        {
            // These converters call the owner's closed Parse method. Read the same declared values,
            // never a duplicate spelling table or a permissive object representation.
            var canonical = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(property => property.PropertyType == type)
                .Select(property => JsonSerializer.SerializeToElement(property.GetValue(null), type, context.TypeInfo.Options).GetString()!);
            var lookupKeys = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(field => field.FieldType.IsGenericType && field.FieldType.GetGenericArguments().SequenceEqual(new[] { typeof(string), type }))
                .SelectMany(field => field.FieldType.GetProperty("Keys")?.GetValue(field.GetValue(null)) as IEnumerable<string> ?? []);
            var values = canonical.Concat(lookupKeys).Distinct(StringComparer.Ordinal).Where(value =>
            {
                // Respect the actual Parse/alias rules, rather than inferring wire spellings from CLR names.
                try { return JsonSerializer.Deserialize(JsonSerializer.Serialize(value), type, context.TypeInfo.Options) is not null; }
                catch (JsonException) { return false; }
            }).Order(StringComparer.Ordinal).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray();
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
