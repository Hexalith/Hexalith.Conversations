from pathlib import Path
root=Path.cwd()
def edit(p,f):
 path=root/p;old=path.read_bytes();text=old.decode().replace('\r\n','\n');text=f(text);path.write_bytes(text.replace('\n','\r\n').encode() if b'\r\n' in old else text.encode())
def metadata(s):
 s=s.replace('{method.Attributes}:{SignatureNames.Format','{method.Attributes}:{method.ImplAttributes}:{SignatureNames.Format')
 s=s.replace('type.GetInterfaceImplementations().Select(h => Names.Name(reader, reader.GetInterfaceImplementation(h).Interface))','type.GetInterfaceImplementations().Select(h => Interface(reader, h))')
 s=s.replace('assemblyName = name, assemblyVersion', 'assemblyName = name, assemblyFlags = (int)assembly.Flags, assemblyVersion')
 s=s.replace('api = new { kind = "conservative-ecma335-public-declarations-v1", types }','api = new { kind = "conservative-ecma335-public-declarations-v1", assemblyAttributes = ContractAttributes(reader, assembly.GetCustomAttributes()), moduleAttributes = ContractAttributes(reader, reader.GetModuleDefinition().GetCustomAttributes()), types }')
 s=s.replace('return AttributeName(reader, a) + ":" + Convert.ToHexString(reader.GetBlobBytes(a.Value));','return AttributeConstructor(reader, a) + ":" + Convert.ToHexString(reader.GetBlobBytes(a.Value));')
 at=s.index('    private static string AttributeName(')
 s=s[:at]+'''    /// <summary>Preserves per-interface contract annotations as well as the complete type.</summary>
    private static string Interface(MetadataReader reader, InterfaceImplementationHandle handle)
    {
        InterfaceImplementation implementation = reader.GetInterfaceImplementation(handle);
        return Names.Name(reader, implementation.Interface) + ":" + Attributes(reader, implementation.GetCustomAttributes());
    }

    /// <summary>Preserves the selected constructor, including local overloads and external ownership.</summary>
    private static string AttributeConstructor(MetadataReader reader, CustomAttribute attribute)
    {
        string name;
        MethodSignature<string> signature;
        if (attribute.Constructor.Kind == HandleKind.MemberReference)
        {
            MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            name = reader.GetString(constructor.Name);
            signature = constructor.DecodeMethodSignature(Names, (object?)null);
        }
        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
        {
            MethodDefinition constructor = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
            name = reader.GetString(constructor.Name);
            signature = constructor.DecodeSignature(Names, (object?)null);
        }
        else throw new InvalidDataException("Unsupported attribute constructor.");
        if (name != ".ctor" || !signature.Header.IsInstance || signature.Header.IsGeneric || signature.ReturnType != "Void")
            throw new InvalidDataException("Unsupported attribute construction contract.");
        return AttributeName(reader, attribute) + ":" + name + ":" + SignatureNames.Format(signature);
    }

    /// <summary>Excludes only separately verified source identity and expected build file-version metadata.</summary>
    private static string ContractAttributes(MetadataReader reader, CustomAttributeHandleCollection handles)
    {
        var retained = new List<string>();
        foreach (CustomAttributeHandle handle in handles)
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            string name = AttributeName(reader, attribute);
            // Only external framework attributes with these exact declarations are build stamps.
            bool external = attribute.Constructor.Kind == HandleKind.MemberReference
                && reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent.Kind == HandleKind.TypeReference;
            bool excluded = external && (name.EndsWith("]System.Reflection.AssemblyInformationalVersionAttribute", StringComparison.Ordinal)
                || name.EndsWith("]System.Reflection.AssemblyFileVersionAttribute", StringComparison.Ordinal));
            if (external && name.EndsWith("]System.Reflection.AssemblyMetadataAttribute", StringComparison.Ordinal))
            {
                BlobReader value = reader.GetBlobReader(attribute.Value);
                if (value.ReadUInt16() != 1) throw new BadImageFormatException();
                excluded = value.ReadSerializedString() == "SourceRevisionId";
            }
            if (!excluded) retained.Add(AttributeConstructor(reader, attribute) + ":" + Convert.ToHexString(reader.GetBlobBytes(attribute.Value)));
        }
        return string.Join(";", retained.Order(StringComparer.Ordinal));
    }
''' + s[at:]
 return s
edit('_bmad/scripts/epic16_native_helper/ProductMetadata.cs',metadata)
def program(s):
 at=s.index('    private static bool IsTestSignature(')
 s=s[:at]+'''    /// <summary>Recognizes resolvable Fact-derived attributes without loading their declarations.</summary>
    private static bool IsFactDerived(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle type = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => throw new InvalidDataException("Unsupported test attribute constructor."),
        };
        var visited = new HashSet<EntityHandle>();
        while (type.Kind == HandleKind.TypeDefinition)
        {
            if (!visited.Add(type)) throw new InvalidDataException("Cyclic attribute inheritance.");
            type = reader.GetTypeDefinition((TypeDefinitionHandle)type).BaseType;
        }
        if (type.Kind != HandleKind.TypeReference) throw new InvalidDataException("Unsupported attribute inheritance.");
        string name = TypeName(reader, type);
        if (name is "Xunit.FactAttribute" or "Xunit.TheoryAttribute") return true;
        // System attributes are sealed/non-Fact metadata. An unresolved external custom
        // attribute could derive Fact, so it cannot establish unambiguous discovery.
        if (!name.StartsWith("System.", StringComparison.Ordinal))
            throw new InvalidDataException("Unresolved external test attribute inheritance.");
        return false;
    }

''' + s[at:]
 s=s.replace('&& method.GetCustomAttributes().Count(attributeHandle => IsFact(reader, reader.GetCustomAttribute(attributeHandle))) == 1)', '&& method.GetCustomAttributes().Count(attributeHandle => IsFact(reader, reader.GetCustomAttribute(attributeHandle))) == 1\n                    && method.GetCustomAttributes().Count(attributeHandle => IsFactDerived(reader, reader.GetCustomAttribute(attributeHandle))) == 1)')
 return s
edit('_bmad/scripts/epic16_native_helper/Program.cs',program)
def products(s):
 s=s.replace("('assemblyName','culture','publicKey')","('assemblyName','culture','publicKey','assemblyFlags')")
 s=s.replace("    btypes=before['api']['types'];atypes=after['api']['types']", "    for key in ('assemblyAttributes', 'moduleAttributes'):\n        need(before['api'][key] == after['api'][key], 'UPSTREAM_API_DRIFT', subject)\n    btypes=before['api']['types'];atypes=after['api']['types']")
 return s
edit('_bmad/scripts/epic16_products.py',products)
for n in range(1,4):
 def launcher(s):
  start=s.index('    if not (error.name.startswith');end=s.index('    import json',start)
  return s[:start]+'''    declared = {
        'epic16_tooling', 'epic16_trust', 'epic16_measurements', 'epic16_native',
        'epic16_domain', 'epic16_lifecycle', 'epic16_schedules', 'epic16_capture',
        'epic16_faults', 'epic16_relationships', 'epic16_execution', 'epic16_products',
        'jsonschema', 'attrs', 'attr', 'referencing', 'rpds', 'jsonschema_specifications',
        'contained_reader', 'authoring_outputs', 'evidence_boundary_current',
        'generate_story_record', 'config_utils', 'verify_story_completion_workflows',
    }
    if error.name not in declared:
        raise
    import importlib.util
    if importlib.util.find_spec(error.name) is not None:
        raise
''' + s[end:]
 edit(f'_bmad/scripts/verify_story_16_{n}.py',launcher)
def lifecycle(s):
 s=s.replace("dispatch=[row['retiringDispatchId']] if erase and not worked else []", "resource_dispatch={state['conversationId']: [row['retiringDispatchId']] if erase and not worked else [] for state in states}\n        dispatch=[reference for state in states for reference in resource_dispatch[state['conversationId']]]")
 s=s.replace("component('dispatch',state['conversationId'],dispatch,", "component('dispatch',state['conversationId'],resource_dispatch[state['conversationId']],")
 return s
edit('_bmad/scripts/epic16_lifecycle.py',lifecycle)
