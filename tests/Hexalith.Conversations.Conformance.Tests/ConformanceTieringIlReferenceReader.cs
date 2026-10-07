// <copyright file="ConformanceTieringIlReferenceReader.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Reads the compiled conformance assembly's IL to find the external types a test method reaches.
/// </summary>
/// <remarks>
/// This is the independent lower bound for the Story 9.1 source-closure bindings. Starting from one test method
/// and its declaring class's constructors, it follows every same-assembly method the IL names, every method of a
/// compiler-generated or instantiated same-assembly type (state machines, closures, virtual overrides), and every
/// static constructor of a same-assembly type it touches. Each external type reference in an operand, a member or
/// local signature, or a generic instantiation is recorded with its defining assembly.
/// </remarks>
internal sealed class ConformanceTieringIlReferenceReader : ISignatureTypeProvider<string, object?>, IDisposable
{
    private static readonly Dictionary<ushort, OperandType> OperandTypes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(code => unchecked((ushort)code.Value), code => code.OperandType);

    private readonly PEReader _peReader;
    private readonly MetadataReader _metadata;
    private readonly string _assemblyName;
    private HashSet<string> _references = new(StringComparer.Ordinal);
    private Queue<MethodDefinitionHandle> _pending = new();
    private HashSet<MethodDefinitionHandle> _visited = [];
    private HashSet<TypeDefinitionHandle> _expandedTypes = [];
    private List<TypeDefinitionHandle> _decodedDefinitions = [];
    private TypeDefinitionHandle _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConformanceTieringIlReferenceReader"/> class.
    /// </summary>
    /// <param name="assemblyPath">The compiled conformance test assembly.</param>
    internal ConformanceTieringIlReferenceReader(string assemblyPath)
    {
        _peReader = new PEReader(File.OpenRead(assemblyPath));
        _metadata = _peReader.GetMetadataReader();
        _assemblyName = _metadata.GetString(_metadata.GetAssemblyDefinition().Name);
    }

    /// <summary>
    /// Returns every external type reference reachable from one test method, as <c>Assembly|Namespace.Type</c>.
    /// </summary>
    /// <param name="testMethod">The reflected test method.</param>
    /// <returns>The sorted external type references.</returns>
    internal IReadOnlyList<string> ReachableReferences(MethodInfo testMethod)
    {
        ArgumentNullException.ThrowIfNull(testMethod);
        _references = new(StringComparer.Ordinal);
        _pending = new();
        _visited = [];
        _expandedTypes = [];
        _decodedDefinitions = [];
        MethodDefinitionHandle start = (MethodDefinitionHandle)MetadataTokens.EntityHandle(testMethod.MetadataToken);
        _owner = _metadata.GetMethodDefinition(start).GetDeclaringType();
        Enqueue(start);
        foreach (MethodDefinitionHandle handle in _metadata.GetTypeDefinition(_owner).GetMethods())
        {
            string name = _metadata.GetString(_metadata.GetMethodDefinition(handle).Name);
            if (name is ".ctor" or ".cctor")
            {
                Enqueue(handle);
            }
        }

        while (_pending.Count > 0)
        {
            Visit(_pending.Dequeue());
        }

        return _references.Order(StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc/>
    public void Dispose() => _peReader.Dispose();

    /// <inheritdoc/>
    public string GetArrayType(string elementType, ArrayShape shape) => elementType;

    /// <inheritdoc/>
    public string GetByReferenceType(string elementType) => elementType;

    /// <inheritdoc/>
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";

    /// <inheritdoc/>
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType;

    /// <inheritdoc/>
    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;

    /// <inheritdoc/>
    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;

    /// <inheritdoc/>
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

    /// <inheritdoc/>
    public string GetPinnedType(string elementType) => elementType;

    /// <inheritdoc/>
    public string GetPointerType(string elementType) => elementType;

    /// <inheritdoc/>
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

    /// <inheritdoc/>
    public string GetSZArrayType(string elementType) => elementType;

    /// <inheritdoc/>
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        _decodedDefinitions.Add(handle);
        ExpandIfImplicit(handle);
        return _metadata.GetString(_metadata.GetTypeDefinition(handle).Name);
    }

    /// <inheritdoc/>
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => RecordReference(handle);

    /// <inheritdoc/>
    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => _metadata.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    private void Enqueue(MethodDefinitionHandle handle)
    {
        if (_visited.Add(handle))
        {
            _pending.Enqueue(handle);
            TypeDefinitionHandle owner = _metadata.GetMethodDefinition(handle).GetDeclaringType();
            ExpandIfImplicit(owner);
            EnqueueStaticConstructor(owner);
        }
    }

    private void EnqueueStaticConstructor(TypeDefinitionHandle owner)
    {
        foreach (MethodDefinitionHandle handle in _metadata.GetTypeDefinition(owner).GetMethods())
        {
            if (_metadata.GetString(_metadata.GetMethodDefinition(handle).Name) == ".cctor")
            {
                Enqueue(handle);
            }
        }
    }

    private void ExpandIfImplicit(TypeDefinitionHandle handle)
    {
        TypeDefinition type = _metadata.GetTypeDefinition(handle);
        if (_metadata.GetString(type.Name).StartsWith('<'))
        {
            ExpandType(handle);
        }
    }

    private void ExpandType(TypeDefinitionHandle handle)
    {
        if (handle == _owner || !_expandedTypes.Add(handle))
        {
            return;
        }

        foreach (MethodDefinitionHandle method in _metadata.GetTypeDefinition(handle).GetMethods())
        {
            Enqueue(method);
        }
    }

    private void Visit(MethodDefinitionHandle handle)
    {
        MethodDefinition method = _metadata.GetMethodDefinition(handle);
        _ = method.DecodeSignature(this, null);
        if (method.RelativeVirtualAddress == 0)
        {
            return;
        }

        MethodBodyBlock body = _peReader.GetMethodBody(method.RelativeVirtualAddress);
        if (!body.LocalSignature.IsNil)
        {
            _ = _metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(this, null);
        }

        ImmutableArray<byte> il = body.GetILContent();
        int offset = 0;
        while (offset < il.Length)
        {
            ushort code = il[offset++];
            if (code == 0xFE)
            {
                code = (ushort)(0xFE00 | il[offset++]);
            }

            if (!OperandTypes.TryGetValue(code, out OperandType operand))
            {
                throw new InvalidOperationException($"Unknown IL opcode 0x{code:X4} in {_metadata.GetString(method.Name)}.");
            }

            switch (operand)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    offset += 1;
                    break;
                case OperandType.InlineVar:
                    offset += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    offset += 8;
                    break;
                case OperandType.InlineSwitch:
                    int count = BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(offset, 4));
                    offset += 4 + (4 * count);
                    break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    VisitToken(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(offset, 4)));
                    offset += 4;
                    break;
                default:
                    offset += 4;
                    break;
            }
        }
    }

    private void VisitToken(int token)
    {
        EntityHandle handle = MetadataTokens.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
                MethodDefinitionHandle target = (MethodDefinitionHandle)handle;
                MethodDefinition definition = _metadata.GetMethodDefinition(target);
                if (_metadata.GetString(definition.Name) == ".ctor")
                {
                    // An instantiated same-assembly type may be called through any virtual override.
                    ExpandType(definition.GetDeclaringType());
                }

                Enqueue(target);
                break;
            case HandleKind.MethodSpecification:
                MethodSpecification specification = _metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                _ = specification.DecodeSignature(this, null);
                VisitToken(MetadataTokens.GetToken(specification.Method));
                break;
            case HandleKind.MemberReference:
                VisitMemberReference((MemberReferenceHandle)handle);
                break;
            case HandleKind.FieldDefinition:
                FieldDefinition field = _metadata.GetFieldDefinition((FieldDefinitionHandle)handle);
                _ = field.DecodeSignature(this, null);
                TypeDefinitionHandle owner = field.GetDeclaringType();
                ExpandIfImplicit(owner);
                EnqueueStaticConstructor(owner);
                break;
            case HandleKind.TypeDefinition:
                ExpandIfImplicit((TypeDefinitionHandle)handle);
                EnqueueStaticConstructor((TypeDefinitionHandle)handle);
                break;
            case HandleKind.TypeReference:
                _ = RecordReference((TypeReferenceHandle)handle);
                break;
            case HandleKind.TypeSpecification:
                _ = GetTypeFromSpecification(_metadata, null, (TypeSpecificationHandle)handle, 0);
                break;
            case HandleKind.StandaloneSignature:
                StandaloneSignature signature = _metadata.GetStandaloneSignature((StandaloneSignatureHandle)handle);
                if (signature.GetKind() == StandaloneSignatureKind.Method)
                {
                    _ = signature.DecodeMethodSignature(this, null);
                }

                break;
        }
    }

    private void VisitMemberReference(MemberReferenceHandle handle)
    {
        MemberReference member = _metadata.GetMemberReference(handle);
        if (member.GetKind() == MemberReferenceKind.Method)
        {
            _ = member.DecodeMethodSignature(this, null);
        }
        else
        {
            _ = member.DecodeFieldSignature(this, null);
        }

        EntityHandle parent = member.Parent;
        switch (parent.Kind)
        {
            case HandleKind.TypeReference:
                _ = RecordReference((TypeReferenceHandle)parent);
                break;
            case HandleKind.TypeSpecification:
                _decodedDefinitions = [];
                _ = GetTypeFromSpecification(_metadata, null, (TypeSpecificationHandle)parent, 0);
                if (_metadata.GetString(member.Name) == ".ctor" && _decodedDefinitions.Count > 0)
                {
                    ExpandType(_decodedDefinitions[0]);
                }

                break;
            case HandleKind.TypeDefinition:
                TypeDefinitionHandle owner = (TypeDefinitionHandle)parent;
                if (_metadata.GetString(member.Name) == ".ctor")
                {
                    ExpandType(owner);
                }

                ExpandIfImplicit(owner);
                EnqueueStaticConstructor(owner);
                break;
            case HandleKind.MethodDefinition:
                Enqueue((MethodDefinitionHandle)parent);
                break;
        }
    }

    private string RecordReference(TypeReferenceHandle handle)
    {
        TypeReference reference = _metadata.GetTypeReference(handle);
        TypeReference outermost = reference;
        while (outermost.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            outermost = _metadata.GetTypeReference((TypeReferenceHandle)outermost.ResolutionScope);
        }

        string fullName = _metadata.GetString(outermost.Namespace) + "." + _metadata.GetString(outermost.Name);
        if (outermost.ResolutionScope.Kind == HandleKind.AssemblyReference)
        {
            AssemblyReference assembly = _metadata.GetAssemblyReference((AssemblyReferenceHandle)outermost.ResolutionScope);
            string assemblyName = _metadata.GetString(assembly.Name);
            if (!string.Equals(assemblyName, _assemblyName, StringComparison.Ordinal))
            {
                _ = _references.Add(assemblyName + "|" + fullName);
            }
        }

        return fullName;
    }
}
