using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

partial class MstatData
{
    // As we parse the file, we populate a sidecar cache with interesting observations
    // about rows of the metadata tables.
    // This cache has a 1:1 relationship with the file format. E.g. each row in the
    // MemberRef table has an entry in the `_memberRefCache` array. One can index
    // into the cache using the RID portion of the MemberRef token.
    //
    // The caches often form linked lists. E.g. all top-level types from a single
    // assemly form a linked list. E.g. TypeRefRowCache.NextTypeInScope contains the
    // metadata handle of the next type. Using the RID portion of the handle
    // we can find the next entry in the TypeRefRowCache.

    private readonly (string Namespace, string Name)[] _typeRefNameCache;
    private readonly string[] _memberRefNameCache;
    private readonly TypeRefRowCache[] _typeRefCache;
    private readonly TypeSpecRowCache[] _typeSpecCache;
    private readonly MemberRefRowCache[] _memberRefCache;
    private readonly MethodSpecRowCache[] _methodSpecCache;
    private readonly AssemblyRefRowCache[] _assemblyRefCache;

    private FrozenObjectHandle _firstUnownedFrozenObject;

    // These are not 1:1 with file format
    private ManifestResourceRowCache[] _manifestResourceCache;
    private FrozenObjectRowCache[] _frozenObjectCache;

    struct TypeRefRowCache
    {
        public int NodeId;
        public int HashCode;
        public int Size;
        public int AggregateSize;

        public TypeReferenceHandle FirstNestedType;
        public MemberReferenceHandle FirstMember;
        public TypeSpecificationHandle FirstTypeSpec;
        public FrozenObjectHandle FirstFrozenObject;
        public TypeReferenceHandle NextTypeInScope;

        public bool IsInitialized => NodeId != 0;

        public void Initialize(MstatData data, TypeReferenceHandle handle, out string @namespace, out string name)
        {
            Debug.Assert(!IsInitialized);

            TypeReference typeRef = data._reader.GetTypeReference(handle);

            name = data._reader.GetString(typeRef.Name);

            if (typeRef.ResolutionScope.Kind != HandleKind.TypeReference)
            {
                @namespace = string.Intern(data._reader.GetString(typeRef.Namespace));
                HashCode = System.HashCode.Combine(@namespace.GetHashCode(), @name.GetHashCode());
                ref AssemblyRefRowCache owningCache = ref data.GetRowCache((AssemblyReferenceHandle)typeRef.ResolutionScope);
                NextTypeInScope = owningCache.FirstTypeRef;
                owningCache.FirstTypeRef = handle;
            }
            else
            {
                @namespace = string.Empty;
                ref var owningCache = ref data.GetRowCache((TypeReferenceHandle)typeRef.ResolutionScope);
                NextTypeInScope = owningCache.FirstNestedType;
                owningCache.FirstNestedType = handle;
                HashCode = System.HashCode.Combine(owningCache.HashCode, @name.GetHashCode());
            }
            NodeId = -1;
        }

        internal void AddSize(MstatData data, TypeReferenceHandle handle, int size)
        {
            Debug.Assert(Unsafe.AreSame(ref this, ref Unsafe.Add(ref data._typeRefCache[0], MetadataTokens.GetRowNumber(handle))));
            
            AggregateSize += size;
            
            TypeReference typeRef = data._reader.GetTypeReference(handle);
            if (typeRef.ResolutionScope.Kind != HandleKind.TypeReference)
                data.GetRowCache((AssemblyReferenceHandle)typeRef.ResolutionScope).AddSize(size);
            else
                data.GetRowCache((TypeReferenceHandle)typeRef.ResolutionScope).AddSize(data, (TypeReferenceHandle)typeRef.ResolutionScope, size);
        }
    }

    struct TypeSpecRowCache
    {
        public int NodeId;
        public int HashCode;
        public int Size;
        public int AggregateSize;

        public TypeSpecificationHandle NextTypeSpec;
        public MemberReferenceHandle FirstMember;
        public FrozenObjectHandle FirstFrozenObject;

        public bool IsInitialized => NodeId != 0;

        public void Initialize(MstatData data, TypeSpecificationHandle handle)
        {
            TypeSpecification typeSpec = data._reader.GetTypeSpecification(handle);

            BlobReader reader = data._reader.GetBlobReader(typeSpec.Signature);
            TypeReferenceHandle declaringTypeRef = GetDeclaringTypeReference(data, reader);

            ref TypeRefRowCache typeRefRow = ref data.GetRowCache(declaringTypeRef);
            NextTypeSpec = typeRefRow.FirstTypeSpec;
            typeRefRow.FirstTypeSpec = handle;
            
            // TODO: compute hashcode as part of GetDeclaringTypeReference and make it better
            HashCode = typeRefRow.HashCode;

            NodeId = -1;
        }

        internal void AddSize(MstatData data, TypeSpecificationHandle handle, int size)
        {
            Debug.Assert(Unsafe.AreSame(ref this, ref Unsafe.Add(ref data._typeSpecCache[0], MetadataTokens.GetRowNumber(handle))));

            AggregateSize += size;

            TypeSpecification typeSpec = data._reader.GetTypeSpecification(handle);

            BlobReader reader = data._reader.GetBlobReader(typeSpec.Signature);
            TypeReferenceHandle declaringTypeRef = GetDeclaringTypeReference(data, reader);
            data.GetRowCache(declaringTypeRef).AddSize(data, declaringTypeRef, size);
        }

        private static TypeReferenceHandle GetDeclaringTypeReference(MstatData data, BlobReader reader)
        {
            SignatureTypeCode typeCode = reader.ReadSignatureTypeCode();
            return typeCode switch
            {
                SignatureTypeCode.GenericTypeInstance => GetDeclaringTypeReference(data, reader),
                SignatureTypeCode.SZArray or SignatureTypeCode.Array or SignatureTypeCode.Pointer or SignatureTypeCode.ByReference => GetDeclaringTypeReference(data, reader),
                SignatureTypeCode.TypeHandle => (TypeReferenceHandle)reader.ReadTypeHandle(),
                SignatureTypeCode.FunctionPointer => GetDeclaringTypeReferenceForFunctionPointer(data, reader),

                <= SignatureTypeCode.String or SignatureTypeCode.Object or (>= SignatureTypeCode.TypedReference and <= SignatureTypeCode.UIntPtr) => data.GetTypeReferenceForSignatureTypeCode(typeCode),

                _ => throw new Exception($"{typeCode} unexpected"),
            };

            static TypeReferenceHandle GetDeclaringTypeReferenceForFunctionPointer(MstatData data, BlobReader reader)
            {
                SignatureHeader header = reader.ReadSignatureHeader();
                if (header.IsGeneric)
                    reader.ReadCompressedInteger();
                reader.ReadCompressedInteger();
                return GetDeclaringTypeReference(data, reader);
            }
        }
    }

    struct MemberRefRowCache
    {
        public int NodeId;
        public int Size;
        public int AggregateSize;
        public int HashCode;

        public MemberReferenceHandle NextMemberRef;
        public MethodSpecificationHandle FirstMethodSpec;

        public bool IsInitialized => NodeId != 0;

        public void Initialize(MstatData data, MemberReferenceHandle handle, out string name)
        {
            MemberReference memberRef = data.MetadataReader.GetMemberReference(handle);

            name = data.MetadataReader.GetString(memberRef.Name);

            if (memberRef.Parent.Kind == HandleKind.TypeReference)
            {
                ref TypeRefRowCache parentCache = ref data.GetRowCache((TypeReferenceHandle)memberRef.Parent);
                NextMemberRef = parentCache.FirstMember;
                parentCache.FirstMember = handle;
                HashCode = System.HashCode.Combine(parentCache.HashCode, name.GetHashCode());
            }
            else
            {
                ref TypeSpecRowCache parentCache = ref data.GetRowCache((TypeSpecificationHandle)memberRef.Parent);
                NextMemberRef = parentCache.FirstMember;
                parentCache.FirstMember = handle;
                HashCode = System.HashCode.Combine(parentCache.HashCode, name.GetHashCode());
            }

            NodeId = -1;
        }

        internal void AddSize(MstatData data, MemberReferenceHandle handle, int size)
        {
            Debug.Assert(Unsafe.AreSame(ref this, ref Unsafe.Add(ref data._memberRefCache[0], MetadataTokens.GetRowNumber(handle))));

            AggregateSize += size;

            MemberReference memberRef = data.MetadataReader.GetMemberReference(handle);
            if (memberRef.Parent.Kind == HandleKind.TypeReference)
                data.GetRowCache((TypeReferenceHandle)memberRef.Parent).AddSize(data, (TypeReferenceHandle)memberRef.Parent, size);
            else
                data.GetRowCache((TypeSpecificationHandle)memberRef.Parent).AddSize(data, (TypeSpecificationHandle)memberRef.Parent, size);
        }
    }

    struct MethodSpecRowCache
    {
        public int NodeId;
        public int Size;
        public int HashCode;

        public MethodSpecificationHandle NextMethodSpec;

        public bool IsInitialized => NodeId != 0;

        public void Initialize(MstatData data, MethodSpecificationHandle handle)
        {
            MethodSpecification methodSpec = data.MetadataReader.GetMethodSpecification(handle);
            ref MemberRefRowCache parentCache = ref data.GetRowCache((MemberReferenceHandle)methodSpec.Method);
            NextMethodSpec = parentCache.FirstMethodSpec;
            parentCache.FirstMethodSpec = handle;
            
            // TODO: better hashcode
            HashCode = parentCache.HashCode;

            NodeId = -1;
        }

        internal void AddSize(MstatData data, MethodSpecificationHandle handle, int size)
        {
            Debug.Assert(Unsafe.AreSame(ref this, ref Unsafe.Add(ref data._methodSpecCache[0], MetadataTokens.GetRowNumber(handle))));

            MethodSpecification methodSpec = data.MetadataReader.GetMethodSpecification(handle);
            data.GetRowCache((MemberReferenceHandle)methodSpec.Method).AddSize(data, (MemberReferenceHandle)methodSpec.Method, size);
        }
    }

    struct AssemblyRefRowCache
    {
        public int AggregateSize;

        public TypeReferenceHandle FirstTypeRef;
        public ManifestResourceHandle FirstManifestResource;

        internal void AddSize(int size) => AggregateSize += size;
    }

    struct ManifestResourceRowCache
    {
        public int Size;
        public string Name;
        public AssemblyReferenceHandle OwningAssembly;

        public ManifestResourceHandle NextManifestResource;
    }

    struct FrozenObjectRowCache
    {
        public int Size;
        public int NodeId;
        public EntityHandle InstanceType;
        public EntityHandle OwningEntity;

        public FrozenObjectHandle NextFrozenObject;
    }

    public enum FrozenObjectHandle { }

    private ref TypeRefRowCache GetRowCache(TypeReferenceHandle handle)
    {
        ref TypeRefRowCache result = ref _typeRefCache[MetadataTokens.GetRowNumber(handle)];
        if (!result.IsInitialized)
        {
            result.Initialize(this, handle, out string @namespace, out string name);
            _typeRefNameCache[MetadataTokens.GetRowNumber(handle)] = (@namespace, name);
        }
        return ref result;
    }

    private ref TypeSpecRowCache GetRowCache(TypeSpecificationHandle handle)
    {
        ref TypeSpecRowCache result = ref _typeSpecCache[MetadataTokens.GetRowNumber(handle)];
        if (!result.IsInitialized)
            result.Initialize(this, handle);
        return ref result;
    }

    private ref MemberRefRowCache GetRowCache(MemberReferenceHandle handle)
    {
        ref MemberRefRowCache result = ref _memberRefCache[MetadataTokens.GetRowNumber(handle)];
        if (!result.IsInitialized)
        {
            result.Initialize(this, handle, out string name);
            _memberRefNameCache[MetadataTokens.GetRowNumber(handle)] = name;
        }
        return ref result;
    }

    private ref MethodSpecRowCache GetRowCache(MethodSpecificationHandle handle)
    {
        ref MethodSpecRowCache result = ref _methodSpecCache[MetadataTokens.GetRowNumber(handle)];
        if (!result.IsInitialized)
            result.Initialize(this, handle);
        return ref result;
    }

    private ref AssemblyRefRowCache GetRowCache(AssemblyReferenceHandle handle)
    {
        return ref _assemblyRefCache[MetadataTokens.GetRowNumber(handle)];
    }

    private ref ManifestResourceRowCache GetRowCache(ManifestResourceHandle handle)
    {
        return ref _manifestResourceCache[MetadataTokens.GetRowNumber(handle)];
    }

    private ref FrozenObjectRowCache GetRowCache(FrozenObjectHandle handle)
    {
        return ref _frozenObjectCache[(int)handle];
    }
}
