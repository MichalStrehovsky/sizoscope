using System.Diagnostics;
using System.Reflection.Metadata;

partial class MstatData
{
    public static (MstatData Left, MstatData Right) Diff(MstatData left, MstatData right)
    {
        MstatData leftDiff = new MstatData(left._peReader);
        leftDiff._nameToNode = left._nameToNode;
        MstatData rightDiff = new MstatData(right._peReader);
        rightDiff._nameToNode = right._nameToNode;

        AddToDiff(left, right, leftDiff);
        AddToDiff(right, left, rightDiff);

        return (leftDiff, rightDiff);
    }

    private static void AddToDiff(MstatData left, MstatData right, MstatData result)
    {
        HashSet<MstatAssembly> rightAsms = new HashSet<MstatAssembly>(right.GetScopes());
        foreach (MstatAssembly leftAsm in left.GetScopes())
        {
            if (rightAsms.TryGetValue(leftAsm, out MstatAssembly rightAsm))
            {
                AddToDiff(leftAsm, rightAsm, result);
            }
            else
            {
                foreach (MstatTypeDefinition t in leftAsm.GetTypes())
                    AddToDiff(t, result);
            }
        }
    }

    private static void AddToDiff(MstatAssembly left, MstatAssembly right, MstatData result)
    {
        HashSet<MstatTypeDefinition> rightTypes = new HashSet<MstatTypeDefinition>(right.GetTypes());
        foreach (MstatTypeDefinition leftType in left.GetTypes())
        {
            if (rightTypes.TryGetValue(leftType, out MstatTypeDefinition rightType))
                AddToDiff(leftType, rightType, result);
            else
                AddToDiff(leftType, result);
        }
    }

    private static void AddToDiff(MstatTypeDefinition left, MstatTypeDefinition right, MstatData result)
    {
        HashSet<MstatMemberDefinition> rightMembers = new HashSet<MstatMemberDefinition>(right.GetMembers());
        foreach (MstatMemberDefinition leftMember in left.GetMembers())
        {
            if (rightMembers.TryGetValue(leftMember, out MstatMemberDefinition rightMember))
                AddToDiff(leftMember, rightMember, result);
            else
                AddToDiff(leftMember, result);
        }

        HashSet<MstatTypeDefinition> rightNestedTypes = new HashSet<MstatTypeDefinition>(right.GetNestedTypes());
        foreach (MstatTypeDefinition leftNestedType in left.GetNestedTypes())
        {
            if (rightNestedTypes.TryGetValue(leftNestedType, out MstatTypeDefinition rightType))
                AddToDiff(leftNestedType, rightType, result);
            else
                AddToDiff(leftNestedType, result);
        }

        HashSet<MstatTypeSpecification> rightTypeSpecs = new HashSet<MstatTypeSpecification>(right.GetTypeSpecifications());
        foreach (MstatTypeSpecification leftTypeSpec in left.GetTypeSpecifications())
        {
            if (rightTypeSpecs.TryGetValue(leftTypeSpec, out MstatTypeSpecification rightType))
                AddToDiff(leftTypeSpec, rightType, result);
            else
                AddToDiff(leftTypeSpec, result);
        }
    }

    private static void AddToDiff(MstatMemberDefinition left, MstatMemberDefinition right, MstatData result)
    {
        HashSet<MstatMethodSpecification> rightInstantiations = new HashSet<MstatMethodSpecification>(right.GetInstantiations());
        foreach (MstatMethodSpecification leftInstantiation in left.GetInstantiations())
        {
            if (!rightInstantiations.Contains(leftInstantiation))
                AddToDiff(leftInstantiation, result);
        }
    }

    private static void AddToDiff(MstatTypeSpecification left, MstatTypeSpecification right, MstatData result)
    {
        HashSet<MstatMemberDefinition> rightMembers = new HashSet<MstatMemberDefinition>(right.GetMembers());
        foreach (MstatMemberDefinition leftMember in left.GetMembers())
        {
            if (rightMembers.TryGetValue(leftMember, out MstatMemberDefinition rightMember))
                AddToDiff(leftMember, rightMember, result);
            else
                AddToDiff(leftMember, result);
        }
    }

    private static void AddToDiff(MstatTypeDefinition t, MstatData result)
    {
        ref TypeRefRowCache cache = ref result.GetRowCache(t.Handle);
        cache.Size = t.Size;
        cache.NodeId = t.NodeId + RealNodeIdAddend;
        cache.AddSize(result, t.Handle, cache.Size);

        foreach (MstatTypeDefinition nested in t.GetNestedTypes())
            AddToDiff(nested, result);

        foreach (MstatTypeSpecification spec in t.GetTypeSpecifications())
            AddToDiff(spec, result);

        foreach (MstatMemberDefinition m in t.GetMembers())
            AddToDiff(m, result);
    }

    private static void AddToDiff(MstatTypeSpecification s, MstatData result)
    {
        ref TypeSpecRowCache cache = ref result.GetRowCache(s.Handle);
        cache.Size = s.Size;
        cache.NodeId = s.NodeId + RealNodeIdAddend;
        cache.AddSize(result, s.Handle, cache.Size);

        foreach (MstatMemberDefinition m in s.GetMembers())
            AddToDiff(m, result);
    }

    private static void AddToDiff(MstatMemberDefinition m, MstatData result)
    {
        ref MemberRefRowCache cache = ref result.GetRowCache(m.Handle);
        cache.Size = m.Size;
        cache.NodeId = m.NodeId + RealNodeIdAddend;
        cache.AddSize(result, m.Handle, cache.Size);

        foreach (MstatMethodSpecification s in m.GetInstantiations())
            AddToDiff(s, result);
    }

    private static void AddToDiff(MstatMethodSpecification s, MstatData result)
    {
        ref MethodSpecRowCache cache = ref result.GetRowCache(s.Handle);
        cache.Size = s.Size;
        cache.NodeId = s.NodeId + RealNodeIdAddend;
        cache.AddSize(result, s.Handle, cache.Size);
    }

    private struct SignatureEqualityComparer
    {
        private readonly MetadataReader _reader1;
        private BlobReader _blob1;
        private readonly MetadataReader _reader2;
        private BlobReader _blob2;

        private SignatureEqualityComparer(MetadataReader reader1, BlobReader blob1, MetadataReader reader2, BlobReader blob2)
            => (_reader1, _blob1, _reader2, _blob2) = (reader1, blob1, reader2, blob2);

        public static bool AreMethodSignaturesEqual(MetadataReader reader1, BlobReader blob1, MetadataReader reader2, BlobReader blob2)
            => new SignatureEqualityComparer(reader1, blob1, reader2, blob2).AreMethodSignaturesEqual();

        public static bool AreMethodSpecSignaturesEqual(MetadataReader reader1, BlobReader blob1, MetadataReader reader2, BlobReader blob2)
            => new SignatureEqualityComparer(reader1, blob1, reader2, blob2).AreMethodSpecSignaturesEqual();

        public static bool AreTypeSignaturesEqual(MetadataReader reader1, BlobReader blob1, MetadataReader reader2, BlobReader blob2)
            => new SignatureEqualityComparer(reader1, blob1, reader2, blob2).AreTypeSignaturesEqual();

        private bool AreMethodSignaturesEqual()
        {
            SignatureHeader header1 = _blob1.ReadSignatureHeader();
            SignatureHeader header2 = _blob2.ReadSignatureHeader();

            if (header1 != header2)
                return false;

            Debug.Assert(header1.Kind == SignatureKind.Method);

            if (header1.IsGeneric
                && _blob1.ReadCompressedInteger() != _blob2.ReadCompressedInteger())
                return false;

            int numParams = _blob1.ReadCompressedInteger();
            if (_blob2.ReadCompressedInteger() != numParams)
                return false;

            for (int i = 0; i < numParams + 1; i++)
                if (!AreTypeSignaturesEqual())
                    return false;

            return true;
        }

        private bool AreMethodSpecSignaturesEqual()
        {
            SignatureHeader header1 = _blob1.ReadSignatureHeader();
            SignatureHeader header2 = _blob2.ReadSignatureHeader();

            if (header1 != header2)
                return false;

            Debug.Assert(header1.Kind == SignatureKind.MethodSpecification);

            int numParams = _blob1.ReadCompressedInteger();
            if (_blob2.ReadCompressedInteger() != numParams)
                return false;

            for (int i = 0; i < numParams + 1; i++)
                if (!AreTypeSignaturesEqual())
                    return false;

            return true;
        }

        private bool AreTypeSignaturesEqual()
        {
            SignatureTypeCode typeCode = _blob1.ReadSignatureTypeCode();
            if (_blob2.ReadSignatureTypeCode() != typeCode)
                return false;

            return typeCode switch
            {
                <= SignatureTypeCode.String or SignatureTypeCode.TypedReference or SignatureTypeCode.IntPtr or SignatureTypeCode.UIntPtr or SignatureTypeCode.Object => true,
                SignatureTypeCode.Pointer or SignatureTypeCode.ByReference or SignatureTypeCode.SZArray => AreTypeSignaturesEqual(),
                SignatureTypeCode.Array => AreArrayTypeSignaturesEqual(),
                SignatureTypeCode.GenericMethodParameter or SignatureTypeCode.GenericTypeParameter => _blob1.ReadCompressedInteger() == _blob2.ReadCompressedInteger(),
                SignatureTypeCode.GenericTypeInstance => AreGenericTypeInstancesEqual(),
                SignatureTypeCode.FunctionPointer => AreMethodSignaturesEqual(),
                SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier => AreTypeHandlesEqual() && AreTypeSignaturesEqual(),
                SignatureTypeCode.TypeHandle => AreTypeHandlesEqual(),
                _ => throw new Exception(typeCode.ToString()),
            };
        }

        private bool AreArrayTypeSignaturesEqual()
        {
            if (!AreTypeSignaturesEqual())
                return false;

            int rank = _blob1.ReadCompressedInteger();
            if (_blob2.ReadCompressedInteger() != rank)
                return false;

            int boundsCount = _blob1.ReadCompressedInteger();
            if (_blob2.ReadCompressedInteger() != boundsCount)
                return false;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < boundsCount; i++)
                    if (_blob1.ReadCompressedInteger() != _blob2.ReadCompressedInteger())
                        return false;
            }

            return true;
        }

        private bool AreGenericTypeInstancesEqual()
        {
            if (!AreTypeSignaturesEqual())
                return false;

            int numArguments = _blob1.ReadCompressedInteger();
            if (_blob2.ReadCompressedInteger() != numArguments)
                return false;

            for (int i = 0; i < numArguments; i++)
                if (!AreTypeSignaturesEqual())
                    return false;

            return true;
        }

        private bool AreTypeHandlesEqual()
        {
            EntityHandle handle1 = _blob1.ReadTypeHandle();
            EntityHandle handle2 = _blob2.ReadTypeHandle();

            if (handle1.Kind != handle2.Kind)
                return false;

            if (handle1.Kind == HandleKind.TypeReference)
                return TypeReferenceComparer.AreEqual(_reader1, (TypeReferenceHandle)handle1, _reader2, (TypeReferenceHandle)handle2);

            TypeSpecification typeSpec1 = _reader1.GetTypeSpecification((TypeSpecificationHandle)handle1);
            TypeSpecification typeSpec2 = _reader2.GetTypeSpecification((TypeSpecificationHandle)handle2);

            return new SignatureEqualityComparer(_reader1, _reader1.GetBlobReader(typeSpec1.Signature), _reader2, _reader2.GetBlobReader(typeSpec2.Signature))
                .AreTypeSignaturesEqual();
        }
    }

    private struct TypeReferenceComparer
    {
        public static bool AreEqual(MetadataReader reader1, TypeReferenceHandle handle1, MetadataReader reader2, TypeReferenceHandle handle2)
        {
            TypeReference typeRef1 = reader1.GetTypeReference(handle1);
            TypeReference typeRef2 = reader2.GetTypeReference(handle2);
            if (typeRef1.ResolutionScope.Kind != typeRef2.ResolutionScope.Kind)
                return false;

            string name = reader1.GetString(typeRef1.Name);
            if (!reader2.StringComparer.Equals(typeRef2.Name, name))
                return false;

            if (typeRef1.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                string ns = reader1.GetString(typeRef1.Namespace);
                if (!reader2.StringComparer.Equals(typeRef2.Namespace, ns))
                    return false;

                AssemblyReference asmRef1 = reader1.GetAssemblyReference((AssemblyReferenceHandle)typeRef1.ResolutionScope);
                AssemblyReference asmRef2 = reader2.GetAssemblyReference((AssemblyReferenceHandle)typeRef2.ResolutionScope);

                string asmName = reader1.GetString(asmRef1.Name);
                return reader2.StringComparer.Equals(asmRef2.Name, asmName);
            }

            return AreEqual(reader1, (TypeReferenceHandle)typeRef1.ResolutionScope, reader2, (TypeReferenceHandle)typeRef2.ResolutionScope);
        }
    }
}
