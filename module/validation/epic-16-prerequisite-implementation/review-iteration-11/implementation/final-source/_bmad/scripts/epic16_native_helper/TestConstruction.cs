using System.Reflection;
using System.Reflection.Metadata;

namespace Hexalith.Conversations.Tooling;

/// <summary>Resolves the pinned xUnit constructor-provider subset without loading evidence.</summary>
internal static class TestConstruction
{
    private static readonly SignatureNames Names = new();

    /// <summary>Validates local non-generic fixtures, inherited declarations and framework/default arguments.</summary>
    internal static void Validate(MetadataReader reader, TypeDefinitionHandle test)
    {
        var local = reader.TypeDefinitions.ToDictionary(h => Names.Name(reader, h), h => h, StringComparer.Ordinal);
        var assembly = new HashSet<string>(StringComparer.Ordinal);
        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute a = reader.GetCustomAttribute(handle);
            string name = AttributeName(reader, a);
            string? argument = GenericArgument(reader, name, "Xunit.AssemblyFixtureAttribute`1");
            if (argument is not null) assembly.Add(argument);
            else if (Framework(reader, name, "Xunit.AssemblyFixtureAttribute")) assembly.Add(LocalSerialized(reader, a, local));
        }
        var collection = new HashSet<string>(StringComparer.Ordinal);
        var classes = new HashSet<string>(StringComparer.Ordinal);
        TypeDefinitionHandle definition = default;
        var inheritedDeclarations = new HashSet<string>(StringComparer.Ordinal);
        int collectionTypes = 0;
        foreach (TypeDefinitionHandle current in Hierarchy(reader, test))
        {
            int declarations = 0;
            foreach (CustomAttributeHandle handle in reader.GetTypeDefinition(current).GetCustomAttributes())
            {
                CustomAttribute a = reader.GetCustomAttribute(handle);
                string name = AttributeName(reader, a);
                string? generic = GenericArgument(reader, name, "Xunit.CollectionAttribute`1");
                if (generic is not null)
                {
                    declarations++;
                    if (!inheritedDeclarations.Add(name)) continue;
                    if (++collectionTypes > 1) throw new InvalidDataException("Ambiguous inherited collection declarations.");
                    if (!local.TryGetValue(generic, out definition)) throw new InvalidDataException("External collection definition unsupported.");
                }
                else if (Framework(reader, name, "Xunit.CollectionAttribute"))
                {
                    declarations++;
                    if (!inheritedDeclarations.Add(name)) continue;
                    if (++collectionTypes > 1) throw new InvalidDataException("Ambiguous inherited collection declarations.");
                    string? value = FirstString(reader, a);
                    MethodSignature<string> signature = reader.GetMemberReference((MemberReferenceHandle)a.Constructor).DecodeMethodSignature(Names, (object?)null);
                    if (signature.ParameterTypes.Length != 1) throw new InvalidDataException("Collection constructor unsupported.");
                    if (signature.ParameterTypes[0] == "String")
                    {
                        TypeDefinitionHandle[] matches = reader.TypeDefinitions.Where(h => reader.GetTypeDefinition(h).GetCustomAttributes().Any(ah =>
                        {
                            CustomAttribute ca = reader.GetCustomAttribute(ah);
                            return Framework(reader, AttributeName(reader, ca), "Xunit.CollectionDefinitionAttribute") && FirstString(reader, ca) == value;
                        })).ToArray();
                        if (matches.Length != 1) throw new InvalidDataException("Named collection provider absent or ambiguous.");
                        definition = matches[0];
                    }
                    else
                    {
                        string key = LocalSerialized(reader, a, local);
                        definition = local[key];
                    }
                }
            }
            if (declarations > 1) throw new InvalidDataException("Ambiguous collection declaration.");
            // Attribute inheritance suppresses only the same concrete attribute type.
        }
        if (!definition.IsNil)
        {
            Interfaces(reader, definition, "Xunit.ICollectionFixture`1", collection);
            Interfaces(reader, definition, "Xunit.IClassFixture`1", classes);
        }
        Interfaces(reader, test, "Xunit.IClassFixture`1", classes);
        var forbidden = new HashSet<string>();
        Interfaces(reader, test, "Xunit.ICollectionFixture`1", forbidden);
        if (forbidden.Count != 0) throw new InvalidDataException("Collection fixtures belong on a collection definition.");
        ValidateFixtures(reader, local, assembly, new HashSet<string>());
        ValidateFixtures(reader, local, collection, assembly);
        ValidateFixtures(reader, local, classes, new HashSet<string>(assembly.Concat(collection)));
        Parameters(reader, test, new HashSet<string>(assembly.Concat(collection).Concat(classes)), true);
    }

    private static void ValidateFixtures(MetadataReader reader, Dictionary<string, TypeDefinitionHandle> local, HashSet<string> fixtures, HashSet<string> parents)
    {
        foreach (string name in fixtures)
        {
            if (!local.TryGetValue(name, out TypeDefinitionHandle handle)) throw new InvalidDataException("External or generic fixture provider is outside the supported metadata subset.");
            TypeDefinition type = reader.GetTypeDefinition(handle);
            if ((type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Interface)) != 0 || type.GetGenericParameters().Count != 0)
                throw new InvalidDataException("Fixture cannot be constructed by supported protocol.");
            Parameters(reader, handle, parents, false);
        }
    }

    private static void Parameters(MetadataReader reader, TypeDefinitionHandle handle, HashSet<string> fixtures, bool test)
    {
        MethodDefinition[] constructors = reader.GetTypeDefinition(handle).GetMethods().Select(reader.GetMethodDefinition)
            .Where(m => reader.GetString(m.Name) == ".ctor" && (m.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public).ToArray();
        if (constructors.Length != 1) throw new InvalidDataException("Exactly one public constructor required.");
        MethodDefinition ctor = constructors[0];
        MethodSignature<string> signature = ctor.DecodeSignature(Names, (object?)null);
        var parameters = ctor.GetParameters().Select(reader.GetParameter).Where(p => p.SequenceNumber > 0).ToDictionary(p => (int)p.SequenceNumber);
        for (int i = 0; i < signature.ParameterTypes.Length; i++)
        {
            string kind = Plain(signature.ParameterTypes[i]);
            bool supplied = fixtures.Contains(kind) || Framework(reader, kind, "Xunit.ITestContextAccessor")
                || test && Framework(reader, kind, "Xunit.ITestOutputHelper")
                || !test && Framework(reader, kind, "Xunit.Sdk.IMessageSink", "xunit.v3.common");
            if (test && parameters.TryGetValue(i + 1, out Parameter parameter))
            {
                supplied |= !parameter.GetDefaultValue().IsNil || (parameter.Attributes & ParameterAttributes.Optional) != 0
                    || kind.EndsWith("[]", StringComparison.Ordinal) && parameter.GetCustomAttributes().Any(h => AttributeName(reader, reader.GetCustomAttribute(h)).EndsWith("]System.ParamArrayAttribute", StringComparison.Ordinal));
            }
            if (!supplied) throw new InvalidDataException("Required constructor parameter has no supported provider.");
        }
    }

    private static IEnumerable<TypeDefinitionHandle> Hierarchy(MetadataReader reader, TypeDefinitionHandle start)
    {
        var visited = new HashSet<TypeDefinitionHandle>();
        while (!start.IsNil)
        {
            if (!visited.Add(start)) throw new InvalidDataException("Cyclic type hierarchy.");
            yield return start;
            EntityHandle parent = reader.GetTypeDefinition(start).BaseType;
            if (parent.Kind == HandleKind.TypeDefinition) start = (TypeDefinitionHandle)parent;
            else if (parent.IsNil || Names.Name(reader, parent).EndsWith("]System.Object", StringComparison.Ordinal)) yield break;
            else throw new InvalidDataException("External or generic fixture inheritance unsupported.");
        }
    }

    private static void Interfaces(MetadataReader reader, TypeDefinitionHandle type, string definition, HashSet<string> result)
    {
        foreach (TypeDefinitionHandle current in Hierarchy(reader, type))
            foreach (InterfaceImplementationHandle handle in reader.GetTypeDefinition(current).GetInterfaceImplementations())
            {
                string name = Names.Name(reader, reader.GetInterfaceImplementation(handle).Interface);
                string? value = GenericArgument(reader, name, definition);
                if (value is not null) result.Add(value);
            }
    }

    private static string? GenericArgument(MetadataReader reader, string name, string definition)
    {
        name = Plain(name);
        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            string prefix = Names.Name(reader, handle);
            if (Framework(reader, prefix, definition) && name.StartsWith(prefix + "<", StringComparison.Ordinal) && name.EndsWith('>'))
                return Plain(name[(prefix.Length + 1)..^1]);
        }
        return null;
    }

    private static string Plain(string name) => name.StartsWith("class ", StringComparison.Ordinal) ? name[6..]
        : name.StartsWith("valuetype ", StringComparison.Ordinal) ? name[10..] : name;

    private static bool Framework(MetadataReader reader, string name, string expected, string assemblyName = "xunit.v3.core")
    {
        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            TypeReference type = reader.GetTypeReference(handle);
            if (reader.GetString(type.Namespace) + "." + reader.GetString(type.Name) != expected || type.ResolutionScope.Kind != HandleKind.AssemblyReference) continue;
            AssemblyReference assembly = reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope);
            if (reader.GetString(assembly.Name) == assemblyName && assembly.Version == new Version(4, 0, 2, 0)
                && reader.GetString(assembly.Culture) == "" && (assembly.Flags & AssemblyFlags.PublicKey) == 0
                && Convert.ToHexString(reader.GetBlobBytes(assembly.PublicKeyOrToken)) == "8D05B1BB7A6FDB6C"
                && Plain(name) == Names.Name(reader, handle)) return true;
        }
        return false;
    }

    private static string AttributeName(MetadataReader reader, CustomAttribute attribute) => attribute.Constructor.Kind == HandleKind.MemberReference
        ? Names.Name(reader, reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent) : "";

    private static string? FirstString(MetadataReader reader, CustomAttribute attribute)
    {
        BlobReader value = reader.GetBlobReader(attribute.Value);
        if (value.ReadUInt16() != 1) throw new BadImageFormatException();
        MethodSignature<string> signature = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).DecodeMethodSignature(Names, (object?)null);
        return signature.ParameterTypes.Length == 0 ? null : value.ReadSerializedString();
    }

    private static string LocalSerialized(MetadataReader reader, CustomAttribute attribute, Dictionary<string, TypeDefinitionHandle> local)
    {
        string value = FirstString(reader, attribute) ?? throw new InvalidDataException("Missing fixture type.");
        // Compiler-produced local System.Type custom-attribute arguments are unqualified.
        // External assembly-qualified or constructed generic providers need separate metadata.
        if (!local.ContainsKey(value) && local.ContainsKey("." + value)) value = "." + value;
        if (!local.ContainsKey(value)) throw new InvalidDataException("Fixture type is not local supported metadata.");
        return value;
    }
}
