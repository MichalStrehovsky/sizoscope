using System.Reflection.Metadata;
using static MstatData;

namespace sizoscope
{
    internal class TreeLogic
    {
        public static void RefreshTree(TreeView tree, MstatData data, Sorter sorter)
        {
            tree.BeginUpdate();
            tree.Nodes.Clear();

            if (data.UnownedFrozenObjectSize > 0)
            {
                tree.Nodes.Add(new TreeNode($"Frozen objects ({AsFileSize(data.UnownedFrozenObjectSize)})",
                        FrozenDataImageIndex, FrozenDataImageIndex,
                        new TreeNode[] { new TreeNode() })
                {
                    Tag = data,
                });
            }

            var asms = data.GetScopes();
            foreach (var asm in sorter.Sort(asms))
            {
                // Do not show for now. This is currently not possible to diff and just causes problems.
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;

                // Aggregate size can be zero if this is a diff
                if (asm.AggregateSize == 0)
                    continue;

                string name = $"{asm.Name} ({AsFileSize(asm.AggregateSize)})";
                TreeNode node = new TreeNode(name, 0, 0,
                    new TreeNode[] { new TreeNode() });
                node.Tag = asm;
                tree.Nodes.Add(node);
            }
            tree.EndUpdate();
        }

        const int FrozenDataImageIndex = 5;
        const int ResourceImageIndex = 6;

        public static void BeforeExpand(TreeNode node, Sorter sorter)
        {
            if (node.FirstNode.Tag != null)
                return;

            node.Nodes.Clear();

            const int instantiationsImageIndex = 4;

            if (node.Tag is MstatData mstat && node.ImageIndex == FrozenDataImageIndex)
            {
                foreach (var frozenObj in sorter.Sort(mstat.GetFrozenObjects()))
                {
                    node.Nodes.Add(new TreeNode($"Instance of {frozenObj} ({AsFileSize(frozenObj.Size)})", FrozenDataImageIndex, FrozenDataImageIndex)
                    {
                        Tag = frozenObj.NodeId,
                    });
                }
            }
            else if (node.Tag is MstatAssembly resourceAssembly && node.ImageIndex == ResourceImageIndex)
            {
                foreach (var res in sorter.Sort(resourceAssembly.GetManifestResources().Select(r => (r.Name, r.Size))))
                {
                    string name = $"{res.Name} ({AsFileSize(res.AggregateSize)})";
                    var newNode = new TreeNode(name, ResourceImageIndex, ResourceImageIndex);
                    node.Nodes.Add(newNode);
                }
            }
            else if (node.Tag is MstatAssembly asm)
            {
                if (asm.GetManifestResources().MoveNext())
                {
                    string name = "Resources";
                    var newNode = new TreeNode(name, ResourceImageIndex, ResourceImageIndex,
                        new TreeNode[] { new TreeNode() });
                    newNode.Tag = asm;
                    node.Nodes.Add(newNode);
                }

                var namespacesAndSizes = asm.GetTypes()
                    .Where(t => t.Namespace.Length > 0)
                    .GroupBy(t => t.Namespace)
                    .Select(g => (Name: g.Key, AggregateSize: g.Sum(t => t.AggregateSize)));
                foreach (var ns in sorter.Sort(namespacesAndSizes))
                {
                    string name = $"{ns.Name} ({AsFileSize(ns.AggregateSize)})";
                    var newNode = new TreeNode(name, 1, 1,
                        new TreeNode[] { new TreeNode() });
                    newNode.Tag = (asm, ns.Name);
                    node.Nodes.Add(newNode);
                }

                AppendTypes(sorter, node, asm.GetTypes(), d => d.Namespace.Length == 0);
            }
            else if (node.Tag is (MstatAssembly a, string ns))
            {
                AppendTypes(sorter, node, a.GetTypes(), d => d.Namespace == ns);
            }
            else if (node.Tag is MstatTypeDefinition frozenDef && node.ImageIndex == FrozenDataImageIndex)
            {
                foreach (var frozenObj in sorter.Sort(frozenDef.GetFrozenObjects()))
                {
                    node.Nodes.Add(new TreeNode($"Instance of {frozenObj} ({AsFileSize(frozenObj.Size)})", FrozenDataImageIndex, FrozenDataImageIndex)
                    {
                        Tag = frozenObj.NodeId,
                    });
                }
            }
            else if (node.Tag is MstatTypeSpecification frozenSpec && node.ImageIndex == FrozenDataImageIndex)
            {
                foreach (var frozenObj in sorter.Sort(frozenSpec.GetFrozenObjects()))
                {
                    node.Nodes.Add(new TreeNode($"Instance of {frozenObj} ({AsFileSize(frozenObj.Size)})", FrozenDataImageIndex, FrozenDataImageIndex)
                    {
                        Tag = frozenObj.NodeId,
                    });
                }
            }
            else if (node.Tag is MstatTypeDefinition genericDef && node.ImageIndex == instantiationsImageIndex)
            {
                foreach (var inst in sorter.Sort(genericDef.GetTypeSpecifications()))
                {
                    string name = $"{inst} ({AsFileSize(inst.AggregateSize)})";
                    var newNode = new TreeNode(name, 2, 2);
                    newNode.Tag = inst;

                    if (inst.GetMembers().MoveNext() || inst.GetFrozenObjects().MoveNext())
                        newNode.Nodes.Add(new TreeNode());

                    node.Nodes.Add(newNode);
                }
            }
            else if (node.Tag is MstatTypeSpecification spec)
            {
                if (spec.GetFrozenObjects().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Frozen objects",
                        FrozenDataImageIndex, FrozenDataImageIndex,
                        new TreeNode[] { new TreeNode() });
                    newNode.Tag = spec;
                    node.Nodes.Add(newNode);
                }
                AppendMembers(sorter, node, spec.GetMembers());
            }
            else if (node.Tag is MstatTypeDefinition def)
            {
                if (def.GetFrozenObjects().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Frozen objects",
                        FrozenDataImageIndex, FrozenDataImageIndex,
                        new TreeNode[] { new TreeNode() });
                    newNode.Tag = def;
                    node.Nodes.Add(newNode);
                }
                if (def.GetTypeSpecifications().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Instantiations",
                        instantiationsImageIndex, instantiationsImageIndex,
                        new TreeNode[] { new TreeNode() });
                    newNode.Tag = def;
                    node.Nodes.Add(newNode);
                }
                AppendTypes(sorter, node, def.GetNestedTypes(), x => true);
                AppendMembers(sorter, node, def.GetMembers());
            }
            else if (node.Tag is MstatMemberDefinition memberDef)
            {
                foreach (var inst in memberDef.GetInstantiations())
                {
                    string name = $"{inst} ({AsFileSize(inst.Size)})";
                    var newNode = new TreeNode(name, 3, 3);
                    newNode.Tag = inst;
                    node.Nodes.Add(newNode);
                }
            }

            static void AppendTypes(Sorter sorter, TreeNode node, Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> list, Func<MstatTypeDefinition, bool> filter)
            {
                foreach (var t in sorter.Sort(list.Where(filter)))
                {
                    string name = $"{t.Name} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, 2, 2,
                        new TreeNode[] { new TreeNode() });
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }

            static void AppendMembers(Sorter sorter, TreeNode node, Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> list)
            {
                foreach (var t in sorter.Sort(list))
                {
                    string name = $"{t} ({AsFileSize(t.AggregateSize)})";
                    int imageIndex = t.IsField ? 7 : 3;
                    var n = new TreeNode(name, imageIndex, imageIndex);
                    if (t.GetInstantiations().Any())
                        n.Nodes.Add(new TreeNode());
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }
        }

        public static string AsFileSize(int size)
            => Math.Abs(size) switch
            {
                < 1024 => $"{size:F0} B",
                < 1024 * 1024 => $"{size / 1024f:F1} kB",
                _ => $"{size / (1024f * 1024f):F1} MB",
            };

        public class Sorter
        {
            private readonly string _key;

            private Sorter(string key) => _key = key;

            public IEnumerable<MstatAssembly> Sort(IEnumerable<MstatAssembly> asms)
                => _key == "Name" ? asms.OrderBy(a => a.Name) : asms.OrderByDescending(a => a.AggregateSize);
            public IEnumerable<(string Name, int AggregateSize)> Sort(IEnumerable<(string Name, int AggregateSize)> ns)
                => _key == "Name" ? ns.OrderBy(n => n.Name) : ns.OrderByDescending(n => n.AggregateSize);

            public IEnumerable<MstatTypeSpecification> Sort(IEnumerable<MstatTypeSpecification> specs)
                => _key == "Name" ? specs.OrderBy(s => s.ToString()) : specs.OrderByDescending(s => s.AggregateSize);

            public IEnumerable<MstatTypeDefinition> Sort(IEnumerable<MstatTypeDefinition> types)
                => _key == "Name" ? types.OrderBy(t => t.Name) : types.OrderByDescending(t => t.AggregateSize);

            public IEnumerable<MstatMemberDefinition> Sort(IEnumerable<MstatMemberDefinition> members)
                => _key == "Name" ? members.OrderBy(t => t.Name) : members.OrderByDescending(t => t.AggregateSize);
            public IEnumerable<MstatFrozenObject> Sort(IEnumerable<MstatFrozenObject> frozenObjects)
                => _key == "Name" ? frozenObjects.OrderBy(t => t.ToString()) : frozenObjects.OrderByDescending(t => t.Size);

            public IEnumerable<(string Name, int Size, int Id)> Sort(IEnumerable<(string Name, int Size, int Id)> members)
                => _key == "Name" ? members.OrderBy(t => t.Name) : members.OrderByDescending(t => t.Size);

            public static Sorter ByName() => new Sorter("Name");
            public static Sorter BySize() => new Sorter("Size");

            public override string ToString() => $"Sort by {_key}";
        }
    }
}
