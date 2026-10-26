using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Hexalith.Conversations.Tooling;

/// <summary>Inspects owned product identity and conservative public metadata surfaces without execution.</summary>
internal static class ProductMetadata
{
    private static readonly SignatureNames Names = new();

    /// <summary>Writes the complete supported public declaration inventory and exact managed identity.</summary>
    internal static void Inspect(JsonElement request)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(request.GetProperty("assembly").GetString()!), false);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata || pe.PEHeaders.CorHeader is null) throw new BadImageFormatException();
        MetadataReader reader = pe.GetMetadataReader();
        if (!reader.IsAssembly) throw new BadImageFormatException();
        AssemblyDefinition assembly = reader.GetAssemblyDefinition();
        string name = reader.GetString(assembly.Name);
        string revision = request.GetProperty("revision").GetString()!;
        var versions = new List<string>();
        var revisions = new List<string>();
        foreach (CustomAttributeHandle handle in assembly.GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            string type = AttributeName(reader, attribute);
            bool informational = type.EndsWith("]System.Reflection.AssemblyInformationalVersionAttribute", StringComparison.Ordinal);
            bool metadata = type.EndsWith("]System.Reflection.AssemblyMetadataAttribute", StringComparison.Ordinal);
            if (!informational && !metadata) continue;
            BlobReader value = reader.GetBlobReader(attribute.Value);
            if (value.ReadUInt16() != 1) throw new BadImageFormatException();
            string key = value.ReadSerializedString() ?? "";
            if (informational) versions.Add(key);
            else if (key == "SourceRevisionId") revisions.Add(value.ReadSerializedString() ?? "");
        }
        if (versions.Count != 1 || !versions[0].Contains('+') || versions[0].Split('+')[^1].Split('.')[^1] != revision || revisions.Any(value => value != revision)) throw new InvalidDataException("Product source revision differs.");
        if (reader.ExportedTypes.Count != 0) throw new InvalidDataException("Forwarded/exported type compatibility is outside this inspector.");
        var types = new SortedDictionary<string, object>(StringComparer.Ordinal);
        var declaredTypes = new List<string>();
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string typeName = Names.Name(reader, handle);
            declaredTypes.Add(typeName);
            if (!Visible(reader, handle)) continue;
            var members = new List<string>();
            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                if (!Visible(method.Attributes)) continue;
                string parameters = string.Join(";", method.GetParameters().Select(h =>
                {
                    Parameter p = reader.GetParameter(h);
                    if (!p.GetMarshallingDescriptor().IsNil) throw new InvalidDataException("Parameter marshalling outside supported surface.");
                    return $"{p.SequenceNumber}:{reader.GetString(p.Name)}:{p.Attributes}:{Constant(reader, p.GetDefaultValue())}:{Attributes(reader, p.GetCustomAttributes())}";
                }));
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0) throw new InvalidDataException("Native imports outside supported surface.");
                members.Add($"method:{reader.GetString(method.Name)}:{method.Attributes}:{SignatureNames.Format(method.DecodeSignature(Names, (object?)null))}:{Generic(reader, method.GetGenericParameters())}:{parameters}:{Attributes(reader, method.GetCustomAttributes())}");
            }
            foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
            {
                FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.FieldAccessMask) is not (FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem)) continue;
                if (!field.GetMarshallingDescriptor().IsNil || field.GetRelativeVirtualAddress() != 0) throw new InvalidDataException("Native field layout outside supported surface.");
                members.Add($"field:{reader.GetString(field.Name)}:{field.Attributes}:{field.DecodeSignature(Names, (object?)null)}:{field.GetOffset()}:{Constant(reader, field.GetDefaultValue())}:{Attributes(reader, field.GetCustomAttributes())}");
            }
            foreach (PropertyDefinitionHandle propertyHandle in type.GetProperties())
            {
                PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                PropertyAccessors access = property.GetAccessors();
                if (!Visible(reader, access.Getter) && !Visible(reader, access.Setter)) continue;
                members.Add($"property:{reader.GetString(property.Name)}:{property.Attributes}:{SignatureNames.Format(property.DecodeSignature(Names, (object?)null))}:{Accessor(reader, access.Getter)}:{Accessor(reader, access.Setter)}:{Constant(reader, property.GetDefaultValue())}:{Attributes(reader, property.GetCustomAttributes())}");
            }
            foreach (EventDefinitionHandle eventHandle in type.GetEvents())
            {
                EventDefinition evt = reader.GetEventDefinition(eventHandle);
                EventAccessors access = evt.GetAccessors();
                if (!Visible(reader, access.Adder) && !Visible(reader, access.Remover)) continue;
                members.Add($"event:{reader.GetString(evt.Name)}:{evt.Attributes}:{Names.Name(reader, evt.Type)}:{Accessor(reader, access.Adder)}:{Accessor(reader, access.Remover)}:{Accessor(reader, access.Raiser)}:{Attributes(reader, evt.GetCustomAttributes())}");
            }
            TypeLayout layout = type.GetLayout();
            string header = $"{type.Attributes}:{Names.Name(reader, type.BaseType)}:{Generic(reader, type.GetGenericParameters())}:{layout.PackingSize}:{layout.Size}:{Attributes(reader, type.GetCustomAttributes())}";
            string[] interfaces = type.GetInterfaceImplementations().Select(h => Names.Name(reader, reader.GetInterfaceImplementation(h).Interface)).Order(StringComparer.Ordinal).ToArray();
            types.Add(typeName, new { header, interfaces, members = members.Order(StringComparer.Ordinal).ToArray() });
        }
        Console.Write(JsonSerializer.Serialize(new
        {
            assemblyName = name, assemblyVersion = assembly.Version.ToString(), culture = reader.GetString(assembly.Culture),
            publicKey = Convert.ToHexString(reader.GetBlobBytes(assembly.PublicKey)).ToLowerInvariant(),
            mvid = reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString(), sourceRevision = revision,
            informationalVersion = versions[0], declaredTypes = declaredTypes.Order(StringComparer.Ordinal).ToArray(),
            api = new { kind = "conservative-ecma335-public-declarations-v1", types },
        }));
    }

    private static bool Visible(MethodAttributes attributes) => (attributes & MethodAttributes.MemberAccessMask) is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;
    private static bool Visible(MetadataReader reader, MethodDefinitionHandle handle) => !handle.IsNil && Visible(reader.GetMethodDefinition(handle).Attributes);
    private static bool Visible(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        TypeAttributes access = type.Attributes & TypeAttributes.VisibilityMask;
        return access == TypeAttributes.Public || access is TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem && Visible(reader, type.GetDeclaringType());
    }
    private static string Accessor(MetadataReader reader, MethodDefinitionHandle handle) => handle.IsNil ? "absent" : $"{reader.GetString(reader.GetMethodDefinition(handle).Name)}:{reader.GetMethodDefinition(handle).Attributes}";
    private static string Constant(MetadataReader reader, ConstantHandle handle) => handle.IsNil ? "absent" : $"{reader.GetConstant(handle).TypeCode}:{Convert.ToHexString(reader.GetBlobBytes(reader.GetConstant(handle).Value))}";
    private static string Generic(MetadataReader reader, GenericParameterHandleCollection handles) => string.Join(";", handles.Select(h =>
    {
        GenericParameter p = reader.GetGenericParameter(h);
        return $"{p.Index}:{reader.GetString(p.Name)}:{p.Attributes}:{string.Join(',', p.GetConstraints().Select(c => Constraint(reader, c)).Order(StringComparer.Ordinal))}:{Attributes(reader, p.GetCustomAttributes())}";
    }));
    private static string Constraint(MetadataReader reader, GenericParameterConstraintHandle handle)
    {
        GenericParameterConstraint constraint = reader.GetGenericParameterConstraint(handle);
        return Names.Name(reader, constraint.Type) + ":" + Attributes(reader, constraint.GetCustomAttributes());
    }
    private static string AttributeName(MetadataReader reader, CustomAttribute attribute) => attribute.Constructor.Kind switch
    {
        HandleKind.MemberReference => Names.Name(reader, reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
        HandleKind.MethodDefinition => Names.Name(reader, reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()),
        _ => throw new InvalidDataException("Unsupported attribute constructor."),
    };
    private static string Attributes(MetadataReader reader, CustomAttributeHandleCollection handles) => string.Join(";", handles.Select(h =>
    {
        CustomAttribute a = reader.GetCustomAttribute(h);
        return AttributeName(reader, a) + ":" + Convert.ToHexString(reader.GetBlobBytes(a.Value));
    }).Order(StringComparer.Ordinal));
}
