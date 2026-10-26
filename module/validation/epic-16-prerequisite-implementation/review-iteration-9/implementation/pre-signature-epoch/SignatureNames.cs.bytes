using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Hexalith.Conversations.Tooling;

/// <summary>Canonical, non-executing ECMA-335 signature names for bounded API comparison.</summary>
internal sealed class SignatureNames : ISignatureTypeProvider<string, object?>
{
    /// <summary>Names an array including rank and explicit bounds.</summary>
    public string GetArrayType(string elementType, ArrayShape shape) => $"{elementType}[{shape.Rank};{string.Join(',', shape.Sizes)};{string.Join(',', shape.LowerBounds)}]";
    /// <summary>Names a managed reference.</summary>
    public string GetByReferenceType(string elementType) => elementType + "&";
    /// <summary>Names a function pointer signature.</summary>
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fn:" + Format(signature);
    /// <summary>Names a constructed generic type.</summary>
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType + "<" + string.Join(',', typeArguments) + ">";
    /// <summary>Names a generic method parameter.</summary>
    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
    /// <summary>Names a generic type parameter.</summary>
    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
    /// <summary>Retains required and optional custom modifiers.</summary>
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => $"{unmodifiedType} mod{(isRequired ? "req" : "opt")}({modifier})";
    /// <summary>Names a pinned type.</summary>
    public string GetPinnedType(string elementType) => elementType + " pinned";
    /// <summary>Names a pointer.</summary>
    public string GetPointerType(string elementType) => elementType + "*";
    /// <summary>Names a primitive metadata type.</summary>
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    /// <summary>Names a vector.</summary>
    public string GetSZArrayType(string elementType) => elementType + "[]";
    /// <summary>Names a defined type including its declaring type.</summary>
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        return type.IsNested ? GetTypeFromDefinition(reader, type.GetDeclaringType(), 0) + "+" + reader.GetString(type.Name)
            : reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    /// <summary>Names an external type with its assembly ownership.</summary>
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        TypeReference type = reader.GetTypeReference(handle);
        string scope = type.ResolutionScope.Kind switch
        {
            HandleKind.AssemblyReference => "[" + reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name) + "]",
            HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)type.ResolutionScope, 0) + "+",
            HandleKind.ModuleDefinition => string.Empty,
            _ => throw new InvalidDataException("Unsupported external type scope."),
        };
        return scope + reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    /// <summary>Decodes a constructed signature without loading a type.</summary>
    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    /// <summary>Names an arbitrary supported type handle.</summary>
    internal string Name(MetadataReader reader, EntityHandle handle) => handle.IsNil ? "" : handle.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
        HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
        HandleKind.TypeSpecification => GetTypeFromSpecification(reader, null, (TypeSpecificationHandle)handle, 0),
        _ => throw new InvalidDataException("Unsupported type handle."),
    };
    /// <summary>Retains calling convention, generic arity, return and parameter shape.</summary>
    internal static string Format(MethodSignature<string> signature) => $"{signature.Header.RawValue}:{signature.GenericParameterCount}:{signature.RequiredParameterCount}:{signature.ReturnType}({string.Join(',', signature.ParameterTypes)})";
}
