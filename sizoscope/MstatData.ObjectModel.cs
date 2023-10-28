using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

partial class MstatData
{
    public struct MstatTypeDefinition : IEquatable<MstatTypeDefinition>
    {
        private readonly MstatData _data;
        public readonly TypeReferenceHandle Handle;

        public MstatTypeDefinition(MstatData data, TypeReferenceHandle handle)
            => (_data, Handle) = (data, handle);

        public string Name => _data._typeRefNameCache[MetadataTokens.GetRowNumber(Handle)].Name ?? throw new Exception();
        public string Namespace => _data._typeRefNameCache[MetadataTokens.GetRowNumber(Handle)].Namespace ?? throw new Exception();
        public int Size => _data.GetRowCache(Handle).Size;
        public int NodeId => _data.GetRowCache(Handle).NodeId - RealNodeIdAddend;
        public int AggregateSize => _data.GetRowCache(Handle).AggregateSize;
        public Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> GetNestedTypes()
            => new Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope>(_data, _data.GetRowCache(Handle).FirstNestedType);
        public Enumerator<TypeSpecificationHandle, MstatTypeSpecification, MoveToNextSpecOfType> GetTypeSpecifications()
            => new Enumerator<TypeSpecificationHandle, MstatTypeSpecification, MoveToNextSpecOfType>(_data, _data.GetRowCache(Handle).FirstTypeSpec);
        public Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> GetMembers()
            => new Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType>(_data, _data.GetRowCache(Handle).FirstMember);
        public Enumerator<FrozenObjectHandle, MstatFrozenObject, MoveToNextFrozenObject> GetFrozenObjects()
            => new Enumerator<FrozenObjectHandle, MstatFrozenObject, MoveToNextFrozenObject>(_data, _data.GetRowCache(Handle).FirstFrozenObject);

        public override string ToString() => ToString(FormatOptions.NamespaceQualify);
        public string ToString(FormatOptions options) => _data._formatter.FormatName(new StringBuilder(), Handle, options).ToString();

        public override int GetHashCode() => _data.GetRowCache(Handle).HashCode;

        public bool Equals(MstatTypeDefinition other)
        {
            if (_data == other._data)
                return Handle == other.Handle;

            if (Name != other.Name
                || Namespace != other.Namespace)
                return false;

            TypeReference thisTypeRef = this._data.MetadataReader.GetTypeReference(Handle);
            TypeReference otherTypeRef = other._data.MetadataReader.GetTypeReference(other.Handle);
            if (thisTypeRef.ResolutionScope.Kind != otherTypeRef.ResolutionScope.Kind)
                return false;

            if (thisTypeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                string thisAsmName = this._data.MetadataReader.GetString(this._data.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)thisTypeRef.ResolutionScope).Name);
                return other._data.MetadataReader.StringComparer.Equals(other._data.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)otherTypeRef.ResolutionScope).Name, thisAsmName);
            }
            else
            {
                return new MstatTypeDefinition(_data, (TypeReferenceHandle)thisTypeRef.ResolutionScope).Equals(
                    new MstatTypeDefinition(other._data, (TypeReferenceHandle)otherTypeRef.ResolutionScope));
            }
        }
    }

    public struct MstatTypeSpecification : IEquatable<MstatTypeSpecification>
    {
        private readonly MstatData _data;
        public readonly TypeSpecificationHandle Handle;

        public MstatTypeSpecification(MstatData data, TypeSpecificationHandle handle)
            => (_data, Handle) = (data, handle);

        public int Size => _data.GetRowCache(Handle).Size;
        public int AggregateSize => _data.GetRowCache(Handle).AggregateSize;
        public int NodeId => _data.GetRowCache(Handle).NodeId - RealNodeIdAddend;

        public Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> GetMembers()
            => new Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType>(_data, _data.GetRowCache(Handle).FirstMember);

        public Enumerator<FrozenObjectHandle, MstatFrozenObject, MoveToNextFrozenObject> GetFrozenObjects()
            => new Enumerator<FrozenObjectHandle, MstatFrozenObject, MoveToNextFrozenObject>(_data, _data.GetRowCache(Handle).FirstFrozenObject);

        public override string ToString() => ToString(FormatOptions.NamespaceQualify);
        public string ToString(FormatOptions options) => _data._formatter.FormatName(new StringBuilder(), Handle, options).ToString();

        public override int GetHashCode() => _data.GetRowCache(Handle).HashCode;

        public bool Equals(MstatTypeSpecification other)
        {
            if (_data == other._data)
                return Handle == other.Handle;

            BlobReader thisTypeSpecBlob = _data.MetadataReader.GetBlobReader(_data.MetadataReader.GetTypeSpecification(Handle).Signature);
            BlobReader otherTypeSpecBlob = other._data.MetadataReader.GetBlobReader(other._data.MetadataReader.GetTypeSpecification(other.Handle).Signature);

            return SignatureEqualityComparer.AreTypeSignaturesEqual(_data.MetadataReader, thisTypeSpecBlob, other._data.MetadataReader, otherTypeSpecBlob);
        }
    }

    public struct MstatMemberDefinition : IEquatable<MstatMemberDefinition>
    {
        private readonly MstatData _data;
        public readonly MemberReferenceHandle Handle;

        public MstatMemberDefinition(MstatData data, MemberReferenceHandle handle)
            => (_data, Handle) = (data, handle);

        public int Size => _data.GetRowCache(Handle).Size;
        public int AggregateSize => _data.GetRowCache(Handle).AggregateSize;
        public string Name => _data._memberRefNameCache[MetadataTokens.GetRowNumber(Handle)];
        public int NodeId => _data.GetRowCache(Handle).NodeId - RealNodeIdAddend;

        public bool IsField
        {
            get
            {
                MemberReference memberRef = _data.MetadataReader.GetMemberReference(Handle);
                return _data.MetadataReader.GetBlobReader(memberRef.Signature).ReadSignatureHeader().Kind == SignatureKind.Field;
            }
        }

        public MstatTypeDefinition OwnerAsDef
        {
            get
            {
                MemberReference memberRef = _data.MetadataReader.GetMemberReference(Handle);
                if (memberRef.Parent.Kind == HandleKind.TypeReference)
                    return new MstatTypeDefinition(_data, (TypeReferenceHandle)memberRef.Parent);
                return default;
            }
        }

        public MstatTypeSpecification OwnerAsSpec
        {
            get
            {
                MemberReference memberRef = _data.MetadataReader.GetMemberReference(Handle);
                if (memberRef.Parent.Kind == HandleKind.TypeSpecification)
                    return new MstatTypeSpecification(_data, (TypeSpecificationHandle)memberRef.Parent);
                return default;
            }
        }

        public Enumerator<MethodSpecificationHandle, MstatMethodSpecification, MoveToNextSpecOfMethod> GetInstantiations()
            => new Enumerator<MethodSpecificationHandle, MstatMethodSpecification, MoveToNextSpecOfMethod>(_data, _data.GetRowCache(Handle).FirstMethodSpec);

        public override string ToString() => _data._formatter.FormatMember(new StringBuilder(), Handle).ToString();
        
        public string ToQualifiedString()
        {
            var sb = new StringBuilder();
            MstatTypeDefinition ownerAsDef = OwnerAsDef;
            if (!ownerAsDef.Handle.IsNil)
                _data._formatter.FormatName(sb, ownerAsDef.Handle, FormatOptions.NamespaceQualify);
            else
                _data._formatter.FormatName(sb, OwnerAsSpec.Handle, FormatOptions.NamespaceQualify);
            sb.Append('.');
            return _data._formatter.FormatMember(sb, Handle).ToString();
        }

        public override int GetHashCode() => _data.GetRowCache(Handle).HashCode;

        public bool Equals(MstatMemberDefinition other)
        {
            if (_data == other._data)
                return Handle == other.Handle;

            MemberReference thisMemberRef = _data.MetadataReader.GetMemberReference(Handle);
            MemberReference otherMemberRef = other._data.MetadataReader.GetMemberReference(other.Handle);

            if (thisMemberRef.Parent.Kind != otherMemberRef.Parent.Kind || Name != other.Name)
                return false;

            BlobReader thisSigBlob = _data.MetadataReader.GetBlobReader(thisMemberRef.Signature);
            BlobReader otherSigBlob = other._data.MetadataReader.GetBlobReader(otherMemberRef.Signature);

            // TODO: is this legit?
            //if (thisSigBlob.RemainingBytes != otherSigBlob.RemainingBytes)
            //    return false;

            if (thisMemberRef.Parent.Kind == HandleKind.TypeReference)
            {
                if (!this.OwnerAsDef.Equals(other.OwnerAsDef))
                    return false;
            }
            else
            {
                if (!this.OwnerAsSpec.Equals(other.OwnerAsSpec))
                    return false;
            }

            return SignatureEqualityComparer.AreMethodSignaturesEqual(
                _data.MetadataReader, thisSigBlob, other._data.MetadataReader, otherSigBlob);
        }
    }

    public struct MstatMethodSpecification : IEquatable<MstatMethodSpecification>
    {
        private readonly MstatData _data;
        public readonly MethodSpecificationHandle Handle;

        public MstatMethodSpecification(MstatData data, MethodSpecificationHandle handle)
            => (_data, Handle) = (data, handle);

        public int Size => _data.GetRowCache(Handle).Size;
        public int NodeId => _data.GetRowCache(Handle).NodeId - RealNodeIdAddend;

        public override string ToString() => _data._formatter.FormatMember(new StringBuilder(), Handle).ToString();

        public override int GetHashCode() => _data.GetRowCache(Handle).HashCode;

        public bool Equals(MstatMethodSpecification other)
        {
            if (_data == other._data)
                return Handle == other.Handle;

            MethodSpecification thisMethodSpec = _data.MetadataReader.GetMethodSpecification(Handle);
            MethodSpecification otherMethodSpec = other._data.MetadataReader.GetMethodSpecification(other.Handle);

            if (!new MstatMemberDefinition(_data, (MemberReferenceHandle)thisMethodSpec.Method)
                .Equals(new MstatMemberDefinition(other._data, (MemberReferenceHandle)otherMethodSpec.Method)))
                return false;

            return SignatureEqualityComparer.AreMethodSpecSignaturesEqual(
                _data.MetadataReader, _data.MetadataReader.GetBlobReader(thisMethodSpec.Signature),
                other._data.MetadataReader, other._data.MetadataReader.GetBlobReader(otherMethodSpec.Signature));
        }
    }

    public struct MstatAssembly : IEquatable<MstatAssembly>
    {
        private readonly MstatData _data;
        private readonly AssemblyReferenceHandle _handle;

        public AssemblyReferenceHandle Handle => _handle;

        public string Name => _data.MetadataReader.GetString(_data.MetadataReader.GetAssemblyReference(_handle).Name);

        public MstatAssembly(MstatData data, AssemblyReferenceHandle handle)
            => (_data, _handle) = (data, handle);

        public int AggregateSize => _data.GetRowCache(_handle).AggregateSize;

        public Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> GetTypes()
            => new Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope>(_data, _data.GetRowCache(_handle).FirstTypeRef);

        public Enumerator<ManifestResourceHandle, MstatManifestResource, MoveToNextManifestResource> GetManifestResources()
            => new Enumerator<ManifestResourceHandle, MstatManifestResource, MoveToNextManifestResource>(_data, _data.GetRowCache(_handle).FirstManifestResource);

        public override int GetHashCode() => Name.GetHashCode();

        public bool Equals(MstatAssembly other)
        {
            if (_data == other._data)
                return _handle == other._handle;

            return Name == other.Name;
        }
    }

    public struct MstatManifestResource : IEquatable<MstatManifestResource>
    {
        private readonly MstatData _data;
        private readonly ManifestResourceHandle _handle;

        public ManifestResourceHandle Handle => _handle;
        public string Name => _data.GetRowCache(_handle).Name;
        public int Size => _data.GetRowCache(_handle).Size;
        public MstatAssembly Assembly => new MstatAssembly(_data, _data.GetRowCache(_handle).OwningAssembly);

        public MstatManifestResource(MstatData data, ManifestResourceHandle handle)
            => (_data, _handle) = (data, handle);

        public bool Equals(MstatManifestResource other)
        {
            if (_data == other._data)
                return _handle == other._handle;

            return Assembly.Equals(other.Assembly) &&
                Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    public struct MstatFrozenObject : IEquatable<MstatFrozenObject>
    {
        private readonly MstatData _data;
        private readonly FrozenObjectHandle _handle;

        public MstatFrozenObject(MstatData data, FrozenObjectHandle handle)
            => (_data, _handle) = (data, handle);

        public FrozenObjectHandle Handle => _handle;

        public int Size => _data.GetRowCache(_handle).Size;

        public int NodeId => _data.GetRowCache(_handle).NodeId;

        public EntityHandle OwningEntity => _data.GetRowCache(_handle).OwningEntity;

        public EntityHandle InstanceType => _data.GetRowCache(_handle).InstanceType;

        public override string ToString()
        {
            EntityHandle type = _data.GetRowCache(_handle).InstanceType;
            return _data._formatter.FormatName(new StringBuilder(), type).ToString();
        }

        public override int GetHashCode()
        {
            return _data.GetNameForId(NodeId).GetHashCode();
        }

        public bool Equals(MstatFrozenObject other)
        {
            if (_data == other._data)
                return NodeId == other.NodeId;

            return this._data.GetNameForId(this.NodeId)
                == other._data.GetNameForId(other.NodeId);
        }
    }

}
