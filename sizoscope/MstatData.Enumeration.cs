using System.Collections;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

partial class MstatData
{
    public Enumerator<AssemblyReferenceHandle, MstatAssembly, MoveToNextScope> GetScopes()
        => new Enumerator<AssemblyReferenceHandle, MstatAssembly, MoveToNextScope>(this, MetadataTokens.AssemblyReferenceHandle(1));

    public Enumerator<FrozenObjectHandle, MstatFrozenObject, MoveToNextFrozenObject> GetFrozenObjects()
        => new Enumerator<FrozenObjectHandle, MstatFrozenObject, MoveToNextFrozenObject>(this, _firstUnownedFrozenObject);

    public interface ICanMoveNext<THandle, TRecord>
    {
        bool IsNil(THandle handle);
        THandle MoveNext(MstatData data, THandle current);
        TRecord GetCurrent(MstatData data, THandle handle);
    }

    public struct MoveToNextScope : ICanMoveNext<AssemblyReferenceHandle, MstatAssembly>
    {
        public bool IsNil(AssemblyReferenceHandle handle) => handle.IsNil;
        public MstatAssembly GetCurrent(MstatData data, AssemblyReferenceHandle handle) => new MstatAssembly(data, handle);
        public AssemblyReferenceHandle MoveNext(MstatData data, AssemblyReferenceHandle current)
            => MetadataTokens.GetRowNumber(current) < data.MetadataReader.AssemblyReferences.Count ?
            MetadataTokens.AssemblyReferenceHandle(MetadataTokens.GetRowNumber(current) + 1) : default;
    }

    public struct MoveToNextInScope : ICanMoveNext<TypeReferenceHandle, MstatTypeDefinition>
    {
        public bool IsNil(TypeReferenceHandle handle) => handle.IsNil;
        public MstatTypeDefinition GetCurrent(MstatData data, TypeReferenceHandle current) => new MstatTypeDefinition(data, current);
        public TypeReferenceHandle MoveNext(MstatData data, TypeReferenceHandle current) => data.GetRowCache(current).NextTypeInScope;
    }

    public struct MoveToNextSpecOfType : ICanMoveNext<TypeSpecificationHandle, MstatTypeSpecification>
    {
        public bool IsNil(TypeSpecificationHandle handle) => handle.IsNil;
        public MstatTypeSpecification GetCurrent(MstatData data, TypeSpecificationHandle handle) => new MstatTypeSpecification(data, handle);
        public TypeSpecificationHandle MoveNext(MstatData data, TypeSpecificationHandle current) => data.GetRowCache(current).NextTypeSpec;
    }

    public struct MoveToNextMemberOfType : ICanMoveNext<MemberReferenceHandle, MstatMemberDefinition>
    {
        public bool IsNil(MemberReferenceHandle handle) => handle.IsNil;
        public MstatMemberDefinition GetCurrent(MstatData data, MemberReferenceHandle handle) => new MstatMemberDefinition(data, handle);
        public MemberReferenceHandle MoveNext(MstatData data, MemberReferenceHandle current) => data.GetRowCache(current).NextMemberRef;
    }

    public struct MoveToNextSpecOfMethod : ICanMoveNext<MethodSpecificationHandle, MstatMethodSpecification>
    {
        public bool IsNil(MethodSpecificationHandle handle) => handle.IsNil;
        public MstatMethodSpecification GetCurrent(MstatData data, MethodSpecificationHandle handle) => new MstatMethodSpecification(data, handle);
        public MethodSpecificationHandle MoveNext(MstatData data, MethodSpecificationHandle current) => data.GetRowCache(current).NextMethodSpec;
    }

    public struct MoveToNextManifestResource : ICanMoveNext<ManifestResourceHandle, MstatManifestResource>
    {
        public bool IsNil(ManifestResourceHandle handle) => handle.IsNil;
        public MstatManifestResource GetCurrent(MstatData data, ManifestResourceHandle handle) => new MstatManifestResource(data, handle);
        public ManifestResourceHandle MoveNext(MstatData data, ManifestResourceHandle current) => data.GetRowCache(current).NextManifestResource;
    }

    public struct MoveToNextFrozenObject : ICanMoveNext<FrozenObjectHandle, MstatFrozenObject>
    {
        public bool IsNil(FrozenObjectHandle handle) => handle == 0;
        public MstatFrozenObject GetCurrent(MstatData data, FrozenObjectHandle handle) => new MstatFrozenObject(data, handle);
        public FrozenObjectHandle MoveNext(MstatData data, FrozenObjectHandle current) => data.GetRowCache(current).NextFrozenObject;
    }

    public struct Enumerator<THandle, TRecord, TNext> : IEnumerable<TRecord>, IEnumerator<TRecord> where TNext : struct, ICanMoveNext<THandle, TRecord>
    {
        private readonly MstatData _data;
        private readonly THandle _first;
        private THandle _current;

        public Enumerator(MstatData data, THandle first)
            => (_data, _first, _current) = (data, first, default);

        public TRecord Current => default(TNext).GetCurrent(_data, _current);

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (default(TNext).IsNil(_current))
            {
                if (default(TNext).IsNil(_first))
                    return false;
                _current = _first;
            }
            else
            {
                THandle next = default(TNext).MoveNext(_data, _current);
                if (default(TNext).IsNil(next))
                    return false;
                _current = next;
            }
            return true;
        }

        public void Reset() => _current = default;

        public Enumerator<THandle, TRecord, TNext> GetEnumerator() => this;

        IEnumerator<TRecord> IEnumerable<TRecord>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;

        void IDisposable.Dispose() { }
    }
}
