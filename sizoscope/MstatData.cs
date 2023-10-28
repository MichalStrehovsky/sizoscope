using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Runtime.InteropServices;

public partial class MstatData : IDisposable
{
    private readonly PEReader _peReader;
    private readonly MetadataReader _reader;
    private readonly Version _version;
    private readonly NameFormatter _formatter;

    private IntPtr _peImage;

    private readonly TypeReferenceHandle[] _primitiveTypeCodeToTypeRef;

    private int _typeSize;
    private int _methodSize;
    private int _fieldSize;
    private int _unownedFrozenObjectSize;
    private int _ownedFrozenObjectSize;
    private int _manifestResourceSize;

    public MetadataReader MetadataReader => _reader;
    public int Size => _typeSize
        + _methodSize
        + _fieldSize
        + _unownedFrozenObjectSize
        + _ownedFrozenObjectSize
        + _manifestResourceSize;

    public int UnownedFrozenObjectSize => _unownedFrozenObjectSize;

    ~MstatData() => Dispose(false);

    private unsafe MstatData(byte* peImage, int size)
        : this(new PEReader(peImage, size))
    {
        _peImage = (IntPtr)peImage;
    }

    private MstatData(PEReader peReader)
    {
        _peReader = peReader;
        _reader = _peReader.GetMetadataReader();
        _version = _reader.GetAssemblyDefinition().Version;
        _typeRefNameCache = new (string Namespace, string Name)[_reader.GetTableRowCount(TableIndex.TypeRef) + 1];
        _memberRefNameCache = new string[_reader.GetTableRowCount(TableIndex.MemberRef) + 1];
        _typeRefCache = new TypeRefRowCache[_reader.GetTableRowCount(TableIndex.TypeRef) + 1];
        _typeSpecCache = new TypeSpecRowCache[_reader.GetTableRowCount(TableIndex.TypeSpec) + 1];
        _memberRefCache = new MemberRefRowCache[_reader.GetTableRowCount(TableIndex.MemberRef) + 1];
        _methodSpecCache = new MethodSpecRowCache[_reader.GetTableRowCount(TableIndex.MethodSpec) + 1];
        _assemblyRefCache = new AssemblyRefRowCache[_reader.GetTableRowCount(TableIndex.AssemblyRef) + 1];
        _formatter = new NameFormatter(this);
        _primitiveTypeCodeToTypeRef = new TypeReferenceHandle[s_primitiveNames.Length];
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _peReader.Dispose();
            GC.SuppressFinalize(this);
        }

        if (_peImage != IntPtr.Zero)
            Marshal.FreeHGlobal(_peImage);
    }

    public static unsafe MstatData Read(string fileName)
    {
        using FileStream fs = File.OpenRead(fileName);
        int length = checked((int)fs.Length);
        byte* mem = (byte*)Marshal.AllocHGlobal(length);
        fs.Read(new Span<byte>(mem, length));
        var data = new MstatData(mem, length).Parse();

        data.TryLoadAssociatedDgmlFile(fileName);

        return data;
    }

    private TypeReferenceHandle GetTypeReferenceForSignatureTypeCode(SignatureTypeCode typeCode)
    {
        Debug.Assert(typeCode is <= SignatureTypeCode.String or (>= SignatureTypeCode.TypedReference and <= SignatureTypeCode.UIntPtr) or SignatureTypeCode.Object);

        int index;
        if (typeCode == SignatureTypeCode.Object)
            index = 0;
        else if (typeCode >= SignatureTypeCode.TypedReference)
            index = (int)typeCode - (int)SignatureTypeCode.TypedReference + (int)SignatureTypeCode.String;
        else
            index = (int)typeCode;

        string name = s_primitiveNames[index];
        if (!_primitiveTypeCodeToTypeRef[index].IsNil)
            return _primitiveTypeCodeToTypeRef[index];

        AssemblyReferenceHandle coreLibAsmRef = default;
        foreach (AssemblyReferenceHandle asmRefHandle in _reader.AssemblyReferences)
        {
            AssemblyReference asmRef = _reader.GetAssemblyReference(asmRefHandle);
            if (_reader.StringComparer.Equals(asmRef.Name, "System.Private.CoreLib"))
            {
                coreLibAsmRef = asmRefHandle;
                break;
            }
        }

        foreach (TypeReferenceHandle typeRefHandle in _reader.TypeReferences)
        {
            TypeReference typeRef = _reader.GetTypeReference(typeRefHandle);
            if (!coreLibAsmRef.IsNil && typeRef.ResolutionScope != coreLibAsmRef)
                continue;

            if (!_reader.StringComparer.Equals(typeRef.Name, name)
                || !_reader.StringComparer.Equals(typeRef.Namespace, "System"))
                continue;

            return _primitiveTypeCodeToTypeRef[index] = typeRefHandle;
        }

        throw new Exception($"{name} not found in TypeRefs");
    }

    private MstatData Parse()
    {
        ParseTypes();
        ParseMethods();

        if (_version >= new Version(2, 1))
        {
            ParseFrozenObjects();
            ParseManifestResources();
            ParseFields();
        }

#if DEBUG
        static void WalkType(MstatTypeDefinition type, ref int typeSize, ref int methodSize)
        {
            typeSize += type.Size;
            methodSize += WalkMembers(type.GetMembers());
            foreach (var nested in type.GetNestedTypes())
                WalkType(nested, ref typeSize, ref methodSize);
            foreach (var spec in type.GetTypeSpecifications())
            {
                typeSize += spec.Size;
                methodSize += WalkMembers(spec.GetMembers());
            }

            static int WalkMembers(Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> members)
            {
                int size = 0;
                foreach (var m in members)
                {
                    size += m.Size;
                    foreach (var s in m.GetInstantiations())
                        size += s.Size;
                }
                return size;
            }
        }
        int typeSize = 0;
        int memberSize = 0;
        foreach (var asm in GetScopes())
            foreach (var t in asm.GetTypes())
                WalkType(t, ref typeSize, ref memberSize);

        Debug.Assert(typeSize == _typeSize);
        Debug.Assert(memberSize == _methodSize + _fieldSize);
#endif

        return this;
    }

    private MethodDefinition GetGlobalMethod(string name)
    {
        TypeDefinition globalType = _reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(1));
        foreach (MethodDefinitionHandle methodHandle in globalType.GetMethods())
        {
            MethodDefinition method = _reader.GetMethodDefinition(methodHandle);
            if (_reader.StringComparer.Equals(method.Name, name))
                return method;
        }

        throw new Exception($"Global method {name} not found");
    }

    private void ParseTypes()
    {
        MethodBodyBlock body = _peReader.GetMethodBody(GetGlobalMethod("Types").RelativeVirtualAddress);
        BlobReader reader = body.GetILReader();

        int majorVersion = _version.Major;
        while (reader.RemainingBytes > 0)
        {
            EntityHandle typeToken = reader.ILReadLdToken();
            int size = reader.ILReadI4Constant();

            _typeSize += size;

            int nodeId = -1;
            if (majorVersion >= 2)
                nodeId = reader.ILReadI4Constant() + RealNodeIdAddend;

            if (typeToken.Kind == HandleKind.TypeReference)
            {
                ref TypeRefRowCache entry = ref GetRowCache((TypeReferenceHandle)typeToken);
                entry.NodeId = nodeId;
                entry.Size = size;
                entry.AddSize(this, (TypeReferenceHandle)typeToken, size);
            }
            else if (typeToken.Kind == HandleKind.TypeSpecification)
            {
                ref TypeSpecRowCache entry = ref GetRowCache((TypeSpecificationHandle)typeToken);
                entry.NodeId = nodeId;
                entry.Size = size;
                entry.AddSize(this, (TypeSpecificationHandle)typeToken, size);

            }
            else
            {
                throw new Exception($"Unexpected {typeToken.Kind}");
            }
        }
    }

    private void ParseMethods()
    {
        MethodBodyBlock body = _peReader.GetMethodBody(GetGlobalMethod("Methods").RelativeVirtualAddress);
        BlobReader reader = body.GetILReader();

        int majorVersion = _version.Major;
        while (reader.RemainingBytes > 0)
        {
            EntityHandle methodToken = reader.ILReadLdToken();
            int size = reader.ILReadI4Constant() + reader.ILReadI4Constant() + reader.ILReadI4Constant();
            int nodeId = -1;
            if (majorVersion >= 2)
                nodeId = reader.ILReadI4Constant() + RealNodeIdAddend;

            _methodSize += size;

            if (methodToken.Kind == HandleKind.MemberReference)
            {
                ref MemberRefRowCache entry = ref GetRowCache((MemberReferenceHandle)methodToken);
                entry.NodeId = nodeId;
                entry.Size = size;
                entry.AddSize(this, (MemberReferenceHandle)methodToken, size);
            }
            else if (methodToken.Kind == HandleKind.MethodSpecification)
            {
                ref MethodSpecRowCache entry = ref GetRowCache((MethodSpecificationHandle)methodToken);
                entry.NodeId = nodeId;
                entry.Size = size;
                entry.AddSize(this, (MethodSpecificationHandle)methodToken, size);
            }
            else
            {
                throw new Exception($"Unexpected {methodToken.Kind}");
            }
        }
    }

    private void ParseFields()
    {
        MethodBodyBlock body = _peReader.GetMethodBody(GetGlobalMethod("RvaFields").RelativeVirtualAddress);
        BlobReader reader = body.GetILReader();

        while (reader.RemainingBytes > 0)
        {
            EntityHandle fieldToken = reader.ILReadLdToken();
            int size = reader.ILReadI4Constant();
            int nodeId = reader.ILReadI4Constant() + RealNodeIdAddend;

            _fieldSize += size;

            Debug.Assert(fieldToken.Kind == HandleKind.MemberReference);
            ref MemberRefRowCache entry = ref GetRowCache((MemberReferenceHandle)fieldToken);
            entry.NodeId = nodeId;
            entry.Size = size;
            entry.AddSize(this, (MemberReferenceHandle)fieldToken, size);
        }
    }

    private void ParseFrozenObjects()
    {
        MethodBodyBlock body = _peReader.GetMethodBody(GetGlobalMethod("FrozenObjects").RelativeVirtualAddress);
        BlobReader reader = body.GetILReader();

        var frozenObjectRowCaches = new List<FrozenObjectRowCache>()
        {
            default
        };

        while (reader.RemainingBytes > 0)
        {
            FrozenObjectHandle current = (FrozenObjectHandle)frozenObjectRowCaches.Count;

            var entry = new FrozenObjectRowCache()
            {
                InstanceType = reader.ILReadLdToken(),
                Size = reader.ILReadI4Constant(),
                NodeId = reader.ILReadI4Constant(),
            };
            
            if (reader.ILTryReadLdToken(out EntityHandle owningType))
            {
                entry.OwningEntity = owningType;

                if (owningType.Kind == HandleKind.TypeReference)
                {
                    ref TypeRefRowCache typeRefCache = ref GetRowCache((TypeReferenceHandle)owningType);
                    typeRefCache.AddSize(this, (TypeReferenceHandle)owningType, entry.Size);
                    entry.NextFrozenObject = typeRefCache.FirstFrozenObject;
                    typeRefCache.FirstFrozenObject = current;
                }
                else
                {
                    ref TypeSpecRowCache typeSpecCache = ref GetRowCache((TypeSpecificationHandle)owningType);
                    typeSpecCache.AddSize(this, (TypeSpecificationHandle)owningType, entry.Size);
                    entry.NextFrozenObject = typeSpecCache.FirstFrozenObject;
                    typeSpecCache.FirstFrozenObject = current;
                }

                _ownedFrozenObjectSize += entry.Size;
            }
            else
            {
                _unownedFrozenObjectSize += entry.Size;

                entry.NextFrozenObject = _firstUnownedFrozenObject;
                _firstUnownedFrozenObject = current;
            }

            frozenObjectRowCaches.Add(entry);
        }

        _frozenObjectCache = frozenObjectRowCaches.ToArray();
    }

    private void ParseManifestResources()
    {
        MethodBodyBlock body = _peReader.GetMethodBody(GetGlobalMethod("ManifestResources").RelativeVirtualAddress);
        BlobReader reader = body.GetILReader();

        var manifestResourceRowCaches = new List<ManifestResourceRowCache>()
        {
            default
        };
        
        while (reader.RemainingBytes > 0)
        {
            var asmRefHandle = (AssemblyReferenceHandle)MetadataTokens.Handle(reader.ILReadI4Constant());
            string name = _reader.GetUserString(reader.ILReadString());
            int size = reader.ILReadI4Constant();

            ref AssemblyRefRowCache cache = ref GetRowCache(asmRefHandle);
            cache.AddSize(size);

            ManifestResourceHandle handle = MetadataTokens.ManifestResourceHandle(manifestResourceRowCaches.Count);

            manifestResourceRowCaches.Add(new ManifestResourceRowCache()
            {
                Name = name,
                Size = size,
                OwningAssembly = asmRefHandle,
                NextManifestResource = cache.FirstManifestResource,
            });

            cache.FirstManifestResource = handle;

            _manifestResourceSize += size;

        }

        _manifestResourceCache = manifestResourceRowCaches.ToArray();
    }

    private static readonly string[] s_primitiveNames = new string[]
        {
            "Object", "Void", "Boolean", "Char", "SByte", "Byte",
            "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
            "Single", "Double", "String", "TypedReference", "IntPtr", "UIntPtr"
        };

    private const int RealNodeIdAddend = 1;

    class NameFormatter
    {
        private readonly MstatData _data;

        public NameFormatter(MstatData data) => _data = data;

        public StringBuilder FormatMember(StringBuilder sb, MethodSpecificationHandle handle)
        {
            MetadataReader reader = _data.MetadataReader;
            MethodSpecification spec = reader.GetMethodSpecification(handle);
            MemberReference memberRef = reader.GetMemberReference((MemberReferenceHandle)spec.Method);
            BlobReader instReader = reader.GetBlobReader(spec.Signature);
            instReader.ReadSignatureHeader();
            
            string name = _data._memberRefNameCache[MetadataTokens.GetRowNumber(spec.Method)] ?? reader.GetString(memberRef.Name);
            sb.Append(name);

            sb.Append('<');
            for (int current = 0, count = instReader.ReadCompressedInteger(); current < count; current++)
            {
                if (current > 0)
                    sb.Append(", ");
                FormatName(sb, ref instReader, default);
            }
            sb.Append('>');

            BlobReader blobReader = reader.GetBlobReader(memberRef.Signature);
            SignatureHeader sigHeader = blobReader.ReadSignatureHeader();
            AppendMethodSignature(sb, blobReader, sigHeader, isGenericDefinition: false);

            return sb;
        }

        public StringBuilder FormatMember(StringBuilder sb, MemberReferenceHandle handle)
        {
            MetadataReader reader = _data.MetadataReader;
            MemberReference memberRef = reader.GetMemberReference(handle);
            string name = _data._memberRefNameCache[MetadataTokens.GetRowNumber(handle)] ?? reader.GetString(memberRef.Name);
            sb.Append(name);
            BlobReader blobReader = reader.GetBlobReader(memberRef.Signature);
            SignatureHeader sigHeader = blobReader.ReadSignatureHeader();

            if (sigHeader.Kind == SignatureKind.Method)
            {
                AppendMethodSignature(sb, blobReader, sigHeader, isGenericDefinition: true);
            }
            else
            {
                Debug.Assert(sigHeader.Kind == SignatureKind.Field);
                sb.Append(" : ");
                FormatName(sb, ref blobReader, default);
            }

            return sb;
        }

        private void AppendMethodSignature(StringBuilder sb, BlobReader reader, SignatureHeader header, bool isGenericDefinition)
        {
            Debug.Assert(header.Kind == SignatureKind.Method);

            if (header.IsGeneric)
            {
                int arity = reader.ReadCompressedInteger();
                if (isGenericDefinition)
                    sb.Append('<').Append(',', arity - 1).Append('>');
            }

            int paramCount = reader.ReadCompressedInteger();
            StringBuilder retType = FormatName(new StringBuilder(), ref reader, default);

            sb.Append('(');
            for (int i = 0; i < paramCount; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                FormatName(sb, ref reader, default);
            }
            sb.Append(')');

            sb.Append(" : ");
            sb.Append(retType);
        }

        public StringBuilder FormatName(StringBuilder sb, EntityHandle handle, FormatOptions options = FormatOptions.NamespaceQualify)
        {
            if (handle.Kind == HandleKind.TypeReference)
                return FormatName(sb, (TypeReferenceHandle)handle, options);
            return FormatName(sb, (TypeSpecificationHandle)handle, options);
        }

        public StringBuilder FormatName(StringBuilder sb, TypeReferenceHandle handle, FormatOptions options = FormatOptions.NamespaceQualify)
        {
            (string @namespace, string name) = _data._typeRefNameCache[MetadataTokens.GetRowNumber(handle)];

            if ((options & FormatOptions.NamespaceQualify) != 0)
            {
                if (@namespace == null)
                    @namespace = _data.MetadataReader.GetString(_data.MetadataReader.GetTypeReference(handle).Namespace);

                if (@namespace.Length == 0)
                {
                    EntityHandle resolutionScope = _data.MetadataReader.GetTypeReference(handle).ResolutionScope;
                    if (resolutionScope.Kind == HandleKind.TypeReference)
                    {
                        FormatName(sb, (TypeReferenceHandle)resolutionScope, options);
                        sb.Append('.');
                    }
                }
                else
                {
                    sb.Append(@namespace).Append('.');
                }
            }

            if (name == null)
                name = _data.MetadataReader.GetString(_data.MetadataReader.GetTypeReference(handle).Name);

            return sb.Append(name);
        }

        public StringBuilder FormatName(StringBuilder sb, TypeSpecificationHandle handle, FormatOptions options = FormatOptions.NamespaceQualify)
        {
            TypeSpecification spec = _data.MetadataReader.GetTypeSpecification(handle);
            BlobReader reader = _data.MetadataReader.GetBlobReader(spec.Signature);
            return FormatName(sb, ref reader, options);
        }

        private StringBuilder FormatName(StringBuilder sb, ref BlobReader reader, FormatOptions options = FormatOptions.NamespaceQualify)
        {
            SignatureTypeCode typeCode = reader.ReadSignatureTypeCode();
            switch (typeCode)
            {
                case <= SignatureTypeCode.String or (>= SignatureTypeCode.TypedReference and <= SignatureTypeCode.UIntPtr) or SignatureTypeCode.Object:
                    if ((options & FormatOptions.NamespaceQualify) != 0)
                        sb.Append("System.");
                    if (typeCode == SignatureTypeCode.Object)
                        sb.Append(s_primitiveNames[0]);
                    else if (typeCode >= SignatureTypeCode.TypedReference)
                        sb.Append(s_primitiveNames[(int)typeCode - (int)SignatureTypeCode.TypedReference + (int)SignatureTypeCode.String]);
                    else
                        sb.Append(s_primitiveNames[(int)typeCode]);
                    break;
                case SignatureTypeCode.TypeHandle:
                    FormatName(sb, (TypeReferenceHandle)reader.ReadTypeHandle(), options);
                    break;
                case SignatureTypeCode.GenericTypeInstance:
                    FormatName(sb, ref reader, options);
                    sb.Append('<');
                    int count = reader.ReadCompressedInteger();
                    for (int i = 0; i < count; i++)
                    {
                        if (i > 0)
                            sb.Append(", ");
                        FormatName(sb, ref reader, default);
                    }
                    sb.Append('>');
                    break;
                case SignatureTypeCode.SZArray:
                    FormatName(sb, ref reader, options);
                    sb.Append("[]");
                    break;
                case SignatureTypeCode.Pointer:
                    FormatName(sb, ref reader, options);
                    sb.Append('*');
                    break;
                case SignatureTypeCode.Array:
                    FormatName(sb, ref reader, options);
                    sb.Append('[');
                    sb.Append(new string(',', reader.ReadCompressedInteger()));
                    sb.Append(']');
                    for (int i = 0, boundsCount = reader.ReadCompressedInteger(); i < boundsCount; i++)
                        reader.ReadCompressedInteger();
                    for (int j = 0, lowerBoundsCount = reader.ReadCompressedInteger(); j < lowerBoundsCount; j++)
                        reader.ReadCompressedSignedInteger();
                    break;
                case SignatureTypeCode.GenericTypeParameter:
                    sb.Append('!').Append(reader.ReadCompressedInteger());
                    break;
                case SignatureTypeCode.GenericMethodParameter:
                    sb.Append("!!").Append(reader.ReadCompressedInteger());
                    break;
                case SignatureTypeCode.ByReference:
                    FormatName(sb, ref reader, options);
                    sb.Append('&');
                    break;
                case SignatureTypeCode.OptionalModifier:
                    sb.Append("[modopt ");
                    FormatName(sb, ref reader, default);
                    FormatName(sb, ref reader, options);
                    sb.Append(']');
                    break;
                case SignatureTypeCode.RequiredModifier:
                    sb.Append("[modreq ");
                    FormatName(sb, ref reader, default);
                    FormatName(sb, ref reader, options);
                    sb.Append(']');
                    break;
                case SignatureTypeCode.FunctionPointer:
                    if (reader.ReadSignatureHeader().IsGeneric)
                        for (int i = reader.ReadCompressedInteger(); i > 0; i--)
                            FormatName(sb, ref reader, default); // yolo
                    int numParams = reader.ReadCompressedInteger();
                    FormatName(sb, ref reader, default);
                    sb.Append("(*)");
                    sb.Append('(');
                    for (int i = numParams; i > 0; i--)
                        FormatName(sb, ref reader, default);
                    sb.Append(')');
                    break;

                default:
                    throw new Exception($"Unexpected {typeCode}");
            }

            return sb;
        }
    }

}

static class ILReadingExtensions
{
    public static int ILReadI4Constant(this ref BlobReader @this)
    {
        var opcode = (ILOpCode)@this.ReadByte();
        return opcode switch
        {
            ILOpCode.Ldc_i4 => @this.ReadInt32(),
            ILOpCode.Ldc_i4_s => @this.ReadSByte(),
            ILOpCode.Ldc_i4_m1 => -1,
            >= ILOpCode.Ldc_i4_0 and <= ILOpCode.Ldc_i4_8 => (int)opcode - (int)ILOpCode.Ldc_i4_0,
            _ => throw Unexpected(opcode)
        };
    }

    public static EntityHandle ILReadLdToken(this ref BlobReader @this)
    {
        var opcode = (ILOpCode)@this.ReadByte();
        if (opcode != ILOpCode.Ldtoken)
            throw Unexpected(opcode);
        return MetadataTokens.EntityHandle(@this.ReadInt32());
    }

    public static UserStringHandle ILReadString(this ref BlobReader @this)
    {
        var opcode = (ILOpCode)@this.ReadByte();
        if (opcode != ILOpCode.Ldstr)
            throw Unexpected(opcode);
        return MetadataTokens.UserStringHandle(@this.ReadInt32());
    }

    public static bool ILTryReadLdToken(this ref BlobReader @this, out EntityHandle token)
    {
        var opcode = (ILOpCode)@this.ReadByte();
        if (opcode != ILOpCode.Ldtoken)
        {
            token = default;
            return false;
        }
        token = MetadataTokens.EntityHandle(@this.ReadInt32());
        return true;
    }

    private static Exception Unexpected(ILOpCode opcode) => new Exception($"Unexpected opcode {opcode}");
}

public enum FormatOptions
{
    NamespaceQualify = 1,
}

